using System;
using KodeKit;
using KodeUI;
using KSP.Localization;
using MechJebLibBindings;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleManeuverPlanner ("Maneuver Planner") in KodeKit, row for row with its WindowGUI. The module keeps the state
    /// (selected operation, create/change mode) and the node creation; each Operation's parameter rows are built once into their own
    /// panel by OperationPanels (an operation without a builder falls back to its DoParametersGUI through ImguiBlock),
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

        // what WindowGUI did before drawing: forced "create" mode without nodes, and the selected operation's drawing side effects
        protected override void BeforeSync()
        {
            if (planner.PlannerNodeCount() == 0) planner.CreateNodeMode = true;
            if (!HasVessel) return;
            try
            {
                planner.GetOrbitAndUT(out Orbit o, out double ut);
                planner.CurrentOperation.ViewUpdate(o, ut, Core.Target);
            }
            catch (Exception)
            {
            } // like WindowGUI around DoParametersGUI
        }

        // ManeuverPlanner's bindings must not throw without a node executor
        static readonly EditableDouble noLeadTime = new EditableDouble(3);

        EditableDouble LeadTime => HasNode ? Core.Node.LeadTime : noLeadTime;

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            UIObject r = w.Root;

            // "Create a new" / "Change the last" [maneuver node to:], or just "Create a new maneuver node to:"
            Layout mode = KodeWindow.Row(r);
            string shownMode = Localizer.Format("#MechJeb_Maneu_createNodeBtn01");
            UIButton modeButton = KodeWindow.Button(mode, shownMode, () => planner.CreateNodeMode = !planner.CreateNodeMode);
            KodeWindow.Label(mode, "#MechJeb_Maneu_createlab1", true); //"maneuver node to:"
            bool lastCreate = true;
            w.AddSync(() =>
            {
                bool create = planner.CreateNodeMode;
                if (create == lastCreate) return;
                lastCreate = create;
                modeButton.Text(Localizer.Format(create ? "#MechJeb_Maneu_createNodeBtn01" : "#MechJeb_Maneu_createNodeBtn02"));
            });
            w.Visible(mode.gameObject, () => planner.PlannerNodeCount() > 0);
            UIText noNode = KodeWindow.Label(r, "#MechJeb_Maneu_createlab2"); //"Create a new maneuver node to:"
            w.Visible(noNode.gameObject, () => planner.PlannerNodeCount() == 0);

            new ComboBox(w, r, () => planner.OperationId, i => planner.OperationId = i, () => planner.OperationNames);

            // one panel per operation
            Operation[] operations = planner.Operations;
            for (int i = 0; i < operations.Length; i++)
            {
                int index = i;
                var ctx = new OpContext { W = w, Panel = KodeWindow.Panel(r), Op = operations[i], Planner = planner };
                if (!OperationPanels.Build(ctx))
                {
                    Operation op = operations[i];
                    new ImguiBlock(ctx.Panel, () =>
                    {
                        if (!HasVessel) return;
                        try
                        {
                            planner.GetOrbitAndUT(out Orbit o, out double ut);
                            op.DoParametersGUI(o, ut, Core.Target);
                        }
                        catch (Exception)
                        {
                        }
                    });
                }

                w.Visible(ctx.Panel.gameObject, () => planner.OperationId == index);
            }

            UIText after = KodeWindow.Label(r, "#MechJeb_Maneu_createlab3"); //"after the last maneuver node."
            w.Visible(after.gameObject, () => planner.PlannerNodeCount() > 0);

            Layout create = KodeWindow.Row(r);
            KodeWindow.Button(create, "#MechJeb_Maneu_button1", () => CreateNodes(false), true); //"Create node"
            UIButton createExecute = KodeWindow.Button(create, "#MechJeb_Maneu_button2", () => CreateNodes(true), true); //"Create and execute"
            w.Visible(createExecute.gameObject, () => HasNode);

            // the operation's error / warning message
            UIText error = OpUi.DynamicParagraph(w, r, () => planner.CurrentOperation.GetErrorMessage() ?? "", true);
            w.Visible(error.gameObject, () => !string.IsNullOrEmpty(planner.CurrentOperation.GetErrorMessage()));

            KodeWindow.Button(r, "#MechJeb_Maneu_button3", () => planner.Vessel.RemoveAllManeuverNodes(), true); //Remove ALL nodes

            // node executor
            UIButton execOne = KodeWindow.Button(r, "#MechJeb_Maneu_button4", () => Core.Node.ExecuteOneNode(planner), true); //Execute next node
            w.Visible(execOne.gameObject, () => HasNode && planner.PlannerNodeCount() > 0 && !Core.Node.Enabled);
            UIButton execAll = KodeWindow.Button(r, "#MechJeb_Maneu_button5", () => Core.Node.ExecuteAllNodes(planner), true); //Execute all nodes
            w.Visible(execAll.gameObject,
                () => HasNode && planner.PlannerNodeCount() > 0 && !Core.Node.Enabled &&
                    (HasVessel && planner.Vessel.patchedConicSolver.maneuverNodes.Count > 1 || ReflectionUtils.IsLoadedPrincipia));
            UIButton abort = KodeWindow.Button(r, "#MechJeb_Maneu_button6", () => Core.Node.Abort(), true); //Abort node execution
            w.Visible(abort.gameObject, () => HasNode && Core.Node.Enabled);

            Layout options = KodeWindow.Row(r);
            w.Toggle(options, "#MechJeb_Maneu_Autowarp", () => HasNode && Core.Node.Autowarp, v => //"Auto-warp"
            {
                if (HasNode) Core.Node.Autowarp = v;
            });
            w.Toggle(options, "RCS Burn", () => HasNode && Core.Node.RCSOnly, v =>
            {
                if (HasNode) Core.Node.RCSOnly = v;
            });
            w.Toggle(options, "Kill Rotation", () => HasNode && Core.Node.KillRollRotation, v =>
            {
                if (HasNode) Core.Node.KillRollRotation = v;
            });
            w.Visible(options.gameObject, () => HasNode);

            Layout lead = KodeWindow.Row(r);
            KodeWindow.Label(lead, "#MechJeb_Maneu_Lead_time"); //Lead time:
            EditFields.Field(w, lead, () => LeadTime.Text, s =>
            {
                if (HasNode) Core.Node.LeadTime.Text = s;
            }, 35, () => LeadTime.IsParsed);
            KodeWindow.Label(lead, "s");
            KodeWindow.Button(lead, "+", () => LeadTime.Val += 1);
            KodeWindow.Button(lead, "-", () => LeadTime.Val -= 1);
            KodeWindow.Button(lead, "R", () => LeadTime.Val = 3);
            w.Visible(lead.gameObject, () => HasNode);
        }

        // ---- modern layout: a grouped operation dropdown, an action bar, a lead time stepper ----

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

        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            string[] modes = { Localizer.Format("#MechJeb_Maneu_createNodeBtn01"), Localizer.Format("#MechJeb_Maneu_createNodeBtn02") };
            Layout mode = KodeWindow.Row(r);
            w.Segmented(mode, modes, () => planner.CreateNodeMode ? 0 : 1, i => planner.CreateNodeMode = i == 0);
            KodeWindow.Label(mode, "#MechJeb_Maneu_createlab1"); //"maneuver node to:"
            w.Visible(mode.gameObject, () => planner.PlannerNodeCount() > 0);
            UIText noNode = KodeWindow.Label(r, "#MechJeb_Maneu_createlab2"); //"Create a new maneuver node to:"
            w.Visible(noNode.gameObject, () => planner.PlannerNodeCount() == 0);

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

            for (int i = 0; i < operations.Length; i++)
            {
                int index = i;
                var ctx = new OpContext { W = w, Panel = KodeWindow.Panel(r), Op = operations[i], Planner = planner };
                if (!OperationPanels.Build(ctx))
                {
                    Operation op = operations[i];
                    new ImguiBlock(ctx.Panel, () =>
                    {
                        if (!HasVessel) return;
                        try
                        {
                            planner.GetOrbitAndUT(out Orbit o, out double ut);
                            op.DoParametersGUI(o, ut, Core.Target);
                        }
                        catch (Exception)
                        {
                        }
                    });
                }

                w.Visible(ctx.Panel.gameObject, () => planner.OperationId == index);
            }

            UIText after = KodeWindow.Label(r, "#MechJeb_Maneu_createlab3"); //"after the last maneuver node."
            w.Visible(after.gameObject, () => planner.PlannerNodeCount() > 0);

            Layout problem = w.StatusBar(r, () => KodeWindow.Tone.Warn);
            OpUi.DynamicParagraph(w, problem, () => planner.CurrentOperation.GetErrorMessage() ?? "");
            w.Visible(problem.gameObject, () => !string.IsNullOrEmpty(planner.CurrentOperation.GetErrorMessage()));

            // destructive on the left, the main action last on the right
            KodeWindow.ActionBarRow bar = KodeWindow.ActionBar(r);
            bar.Danger("#MechJeb_Maneu_button3", () => planner.Vessel.RemoveAllManeuverNodes()); //Remove ALL nodes
            UIButton createExecute = bar.Normal("#MechJeb_Maneu_button2", () => CreateNodes(true)); //"Create and execute"
            w.Visible(createExecute.gameObject, () => HasNode);
            bar.Primary("#MechJeb_Maneu_button1", () => CreateNodes(false)); //"Create node"

            Layout exec = KodeWindow.Panel(r);
            w.Visible(exec.gameObject, () => HasNode);
            KodeWindow.Header(exec, "#MechJeb_NewUI_Execute"); //Execute
            UIButton execOne = KodeWindow.Button(exec, "#MechJeb_Maneu_button4", () => Core.Node.ExecuteOneNode(planner), ButtonRole.Primary, true); //Execute next node
            w.Visible(execOne.gameObject, () => HasNode && planner.PlannerNodeCount() > 0 && !Core.Node.Enabled);
            UIButton execAll = KodeWindow.Button(exec, "#MechJeb_Maneu_button5", () => Core.Node.ExecuteAllNodes(planner), ButtonRole.Normal, true); //Execute all nodes
            w.Visible(execAll.gameObject,
                () => HasNode && planner.PlannerNodeCount() > 0 && !Core.Node.Enabled &&
                    (HasVessel && planner.Vessel.patchedConicSolver.maneuverNodes.Count > 1 || ReflectionUtils.IsLoadedPrincipia));
            UIButton abort = KodeWindow.Button(exec, "#MechJeb_Maneu_button6", () => Core.Node.Abort(), ButtonRole.Danger, true); //Abort node execution
            w.Visible(abort.gameObject, () => HasNode && Core.Node.Enabled);

            w.SwitchRow(exec, "#MechJeb_Maneu_Autowarp", () => HasNode && Core.Node.Autowarp, v => //"Auto-warp"
            {
                if (HasNode) Core.Node.Autowarp = v;
            });
            w.SwitchRow(exec, "RCS Burn", () => HasNode && Core.Node.RCSOnly, v =>
            {
                if (HasNode) Core.Node.RCSOnly = v;
            });
            w.SwitchRow(exec, "Kill Rotation", () => HasNode && Core.Node.KillRollRotation, v =>
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
            KodeWindow.Button(lead, "R", () => LeadTime.Val = 3); // back to the default 3 s
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
