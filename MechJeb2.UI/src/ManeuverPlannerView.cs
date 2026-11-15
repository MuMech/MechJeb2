using System;
using KodeKit;
using KodeUI;
using KSP.Localization;
using MechJebLibBindings;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleManeuverPlanner ("Maneuver Planner") in KodeKit. The module keeps the state
    /// (selected operation, create/change mode) and the node creation; each Operation's parameter rows are built once into their own
    /// panel by OperationPanels (another mod's operation, which has no builder, gets a note that its settings can't be shown),
    /// and the selected operation's panel is shown.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleManeuverPlanner))]
    public class ManeuverPlannerView : KodeView
    {
        readonly MechJebModuleManeuverPlanner planner;

        public ManeuverPlannerView(MechJebModuleManeuverPlanner planner) : base(planner) => this.planner = planner;

        protected override float MinWidth => 300;

        MechJebCore Core => planner.Core;
        bool HasVessel => planner.Vessel != null && planner.Vessel.patchedConicSolver != null;
        bool HasNode => Core.Node != null;

        // the node executor, as IMGUI's planner shows it: "all" with more than one node (or under Principia, which hides them)
        void ExecuteButtons(KodeWindow w, UIObject parent) =>
            NodeUi.ExecuteButtons(w, parent, Core, planner, "#MechJeb_Maneu_button4", "#MechJeb_Maneu_button5", "#MechJeb_Maneu_button6",
                () => planner.PlannerNodeCount() > 0,
                () => planner.PlannerNodeCount() > 0 && (HasVessel && planner.Vessel.patchedConicSolver.maneuverNodes.Count > 1 || ReflectionUtils.IsLoadedPrincipia),
                true, ButtonRole.Normal, "#MechJeb_NewUI_NoNodeCreateAbove"); //Execute next node, Execute all nodes, Abort node execution
        // "No maneuver node: create one above."

        bool AnyNode => HasVessel && planner.Vessel.patchedConicSolver.maneuverNodes.Count > 0;

        // before the bindings: forced "create" mode without nodes, and the selected operation's per-frame work
        protected override void BeforeSync()
        {
            if (planner.PlannerNodeCount() == 0) planner.CreateNodeMode = true;
            if (!HasVessel) return;
            // no catch: KodeView logs a BeforeSync failure once and the bindings still run
            planner.GetOrbitAndUT(out Orbit o, out double ut);
            planner.CurrentOperation.ViewUpdate(o, ut, Core.Target);
        }

        // ManeuverPlanner's bindings must not throw without a node executor
        static readonly EditableDouble noLeadTime = new EditableDouble(3);

        EditableDouble LeadTime => HasNode ? Core.Node.LeadTime : noLeadTime;

        // ---- A grouped operation dropdown, an action bar, a lead time stepper ----

        static readonly string[] groupKeys = { "#MechJeb_NewUI_OpOrbit", "#MechJeb_NewUI_OpTarget", "#MechJeb_NewUI_OpPlanets" };

        /// <summary>The group of an operation: 0 orbit shape, 1 target, 2 planets and moons.</summary>
        static int Group(Operation op)
        {
            switch (op)
            {
                case OperationGeneric _:
                case OperationLambert _:
                case OperationKillRelVel _:
                case OperationPlane _:
                case OperationCourseCorrection _:
                    return 1;
                case OperationAdvancedTransfer _:
                case OperationMoonReturn _:
                    return 2;
                default:
                    return 0;
            }
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;

            // with nodes: add a node or change the last one; a new node goes after the last one
            string[] modes = { Localizer.Format("#MechJeb_NewUI_NodeNew"), Localizer.Format("#MechJeb_NewUI_NodeChangeLast") }; //New node, Change the last node
            Layout mode = KodeWindow.Row(r);
            w.Segmented(mode, modes, () => planner.CreateNodeMode ? 0 : 1, i => planner.CreateNodeMode = i == 0);
            w.Visible(mode.gameObject, () => planner.PlannerNodeCount() > 0);
            UIText after = KodeWindow.Paragraph(r, "#MechJeb_NewUI_NodeGoesAfterLast"); //The new node goes after the last one.
            KodeWindow.ToneText(after, KodeWindow.Tone.Dim);
            w.Visible(after.gameObject, () => planner.PlannerNodeCount() > 0 && planner.CreateNodeMode);

            // the operations sorted by group (stable), so each group is listed once under its header
            Operation[] operations = planner.Operations;
            var order = new int[operations.Length];
            int n = 0;
            for (int g = 0; g < groupKeys.Length; g++)
                for (int i = 0; i < operations.Length; i++)
                    if (Group(operations[i]) == g) order[n++] = i;
            var names = new string[order.Length];
            var position = new int[order.Length];
            for (int k = 0; k < order.Length; k++)
            {
                names[k] = planner.OperationNames[order[k]];
                position[order[k]] = k;
            }
            string[] groups = { Localizer.Format(groupKeys[0]), Localizer.Format(groupKeys[1]), Localizer.Format(groupKeys[2]) };
            var combo = new ComboBox(w, r, () => position[planner.OperationId], k => planner.OperationId = order[k], () => names)
            {
                Group = k => groups[Group(operations[order[k]])]
            };

            OperationPanelsFor(w, r);

            Layout problem = w.StatusBar(r, () => KodeWindow.Tone.Warn);
            OpUi.DynamicParagraph(w, problem, () => planner.CurrentOperation.GetErrorMessage() ?? "");
            w.Visible(problem.gameObject, () => !string.IsNullOrEmpty(planner.CurrentOperation.GetErrorMessage()));

            // destructive on the left, the main action last on the right
            KodeWindow.ActionBarRow bar = KodeWindow.ActionBar(r);
            UIButton removeAll = bar.Danger("#MechJeb_Maneu_button3", () => planner.Vessel.RemoveAllManeuverNodes()); //Remove ALL nodes
            GuidanceUi.Enable(w, removeAll, () => AnyNode);
            UIButton createExecute = bar.Normal("#MechJeb_Maneu_button2", () => CreateNodes(true)); //"Create and execute"
            w.Visible(createExecute.gameObject, () => HasNode);
            bar.Primary("#MechJeb_Maneu_button1", () => CreateNodes(false)); //"Create node"

            Layout exec = KodeWindow.Panel(r);
            w.Visible(exec.gameObject, () => HasNode);
            KodeWindow.Header(exec, "#MechJeb_NewUI_Execute"); //Execute
            ExecuteButtons(w, exec);

            NodeUi.AutowarpToggle(w, exec, Core, "#MechJeb_Maneu_Autowarp"); //"Auto-warp"
            UnityEngine.UI.Toggle rcs = w.SwitchRow(exec, "#MechJeb_NewUI_RcsBurn", () => HasNode && Core.Node.RCSOnly, v => //RCS burn
            {
                if (HasNode) Core.Node.RCSOnly = v;
            });
            Tooltips.Add(rcs.transform.parent.gameObject, "#MechJeb_NewUI_RcsBurn_tooltip");
            w.SwitchRow(exec, "#MechJeb_SmartASS_button35", () => HasNode && Core.Node.KillRollRotation, v =>
            {
                if (HasNode) Core.Node.KillRollRotation = v;
            });

            Layout lead = KodeWindow.Row(exec);
            KodeWindow.Label(lead, "#MechJeb_Maneu_Lead_time", true); //Lead time:
            w.Stepper(lead, () => LeadTime.Text, s =>
            {
                if (HasNode) Core.Node.LeadTime.Text = s;
            }, d => LeadTime.Val += d, () => 1, 45, () => LeadTime.IsParsed);
            KodeWindow.Label(lead, "s");
            UIButton reset = KodeWindow.Button(lead, "#MechJeb_NewUI_Reset", () => LeadTime.Val = 3); //Reset
            Tooltips.Add(reset.gameObject, "#MechJeb_NewUI_LeadTimeReset_tooltip"); //Back to the default lead time, 3 s.
        }

        /// <summary>
        /// One panel per operation, the selected one shown. Each is built the first time it is selected: opening the planner
        /// builds one of its 18 operations, not all of them.
        /// </summary>
        void OperationPanelsFor(KodeWindow w, UIObject parent)
        {
            Operation[] operations = planner.Operations;
            for (int i = 0; i < operations.Length; i++)
            {
                int index = i;
                Operation op = operations[i];
                w.LazyPanel(parent, () => planner.OperationId == index, panel =>
                {
                    var ctx = new OpContext { W = w, Panel = panel, Op = op, Planner = planner };
                    if (OperationPanels.Build(ctx)) return;
                    // another mod's operation: its parameters are drawn by an IMGUI method this UI doesn't run
                    KodeWindow.Paragraph(panel, "#MechJeb_NewUI_OpNoPanel"); //"This operation comes from another mod: its settings can't be shown here, it uses their current values"
                });
            }
        }

        // the Create node / Create and execute buttons
        void CreateNodes(bool execute)
        {
            if (!HasVessel) return;
            planner.GetOrbitAndUT(out Orbit o, out double ut);
            planner.CreateNodes(execute, o, ut);
        }
    }
}
