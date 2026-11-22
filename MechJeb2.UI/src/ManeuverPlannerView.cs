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
    /// With the Settings toggle on (off by default), an operation that opts in (<see cref="Operation.Previewable"/>) previews its burn
    /// while it is hovered in the list: <see cref="Preview"/> says when, the planner's <see cref="ManeuverPreview"/> draws the ghost
    /// orbit on the map, and the row's card shows its Δv and time. No node is placed and the selection doesn't change.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleManeuverPlanner))]
    public class ManeuverPlannerView : KodeView
    {
        readonly MechJebModuleManeuverPlanner planner;

        public ManeuverPlannerView(MechJebModuleManeuverPlanner planner) : base(planner) => this.planner = planner;

        protected override float MinWidth => 300;

        /// <summary>When a hovered operation previews its burn (an index into the list's entries); null before the window is built.</summary>
        public KodeKit.Headless.PreviewModel<int> Preview { get; private set; }

        /// <summary>The operation list (null before the window is built).</summary>
        public ComboBox OperationList { get; private set; }

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
            if (planner.PlannerNodeCount() == 0 && planner.Mode != MechJebModuleManeuverPlanner.NodeMode.New)
                planner.Mode = MechJebModuleManeuverPlanner.NodeMode.New;
            // a folded "Create a maneuver" has no operation on screen to update
            if (!HasVessel || !planner.showCreation) return;
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

            // the node creation folds away (folded by default under Principia, which plans its own maneuvers); the fold is saved
            Layout create = w.Section(r, "#MechJeb_NewUI_CreateManeuver", planner.showCreation, c => BuildCreation(w, c)); //Create a maneuver
            w.AddSync(() =>
            {
                bool open = create.gameObject.activeSelf;
                if (planner.showCreation != open) planner.showCreation = open;
            });

            // the node executor's settings, tagged: an undo or redo of them waits while the executor flies a node
            using (w.Commits.Tagging(ExecutorTag))
                BuildExecute(w, r);
            w.Undo.Guard = t => t.Tag != ExecutorTag || !HasNode || !Core.Node.Enabled;
            w.UndoButtons();
        }

        const string ExecutorTag = "executor";

        // the create buttons' texts by node mode: create, replace the last node, add to it
        static readonly string[] createKeys = { "#MechJeb_Maneu_button1", "#MechJeb_NewUI_NodeReplace", "#MechJeb_NewUI_NodeAdd" };
        //Create node, Replace node, Add to node
        static readonly string[] createExecuteKeys = { "#MechJeb_Maneu_button2", "#MechJeb_NewUI_NodeReplaceExecute", "#MechJeb_NewUI_NodeAddExecute" };
        //Create and execute, Replace and execute, Add and execute
        static readonly string[] hintKeys = { "#MechJeb_NewUI_NodeGoesAfterLast", "#MechJeb_NewUI_NodeReplacesLast", "#MechJeb_NewUI_NodeAddedToLast" };
        //Computed after the last node. / Computed after the node before the last, which is removed. / Computed after the last node, then added to it.

        void BuildCreation(KodeWindow w, UIObject r)
        {
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
            // each operation's English name for the filter: its type's, without "Operation" (OperationCourseCorrection: CourseCorrection)
            var opKeys = new string[order.Length];
            for (int k = 0; k < order.Length; k++)
            {
                string type = operations[order[k]].GetType().Name;
                opKeys[k] = type.StartsWith("Operation", System.StringComparison.Ordinal) ? type.Substring("Operation".Length) : type;
            }
            var combo = new ComboBox(w, r, () => position[planner.OperationId], k => planner.OperationId = order[k], () => names, name: "#MechJeb_NewUI_Label_Operation") //Operation
            {
                Group = k => groups[Group(operations[order[k]])],
                Filter = true,
                Keys = () => opKeys
            };

            OperationList = combo;
            PreviewOnHover(w, combo, operations, order, position);

            OperationPanelsFor(w, r);

            Layout problem = w.StatusBar(r, () => KodeWindow.Tone.Warn);
            OpUi.DynamicParagraph(w, problem, Problem);
            w.Visible(problem.gameObject, () => Problem().Length > 0);

            // with nodes: what Create does with them, just above the buttons, and where the burn is computed from
            string[] modes =
            {
                Localizer.Format("#MechJeb_NewUI_NodeNew"), Localizer.Format("#MechJeb_NewUI_NodeChangeLast"), Localizer.Format("#MechJeb_NewUI_NodeAddToLast")
            }; //New node, Replace last, Add to last
            Layout mode = KodeWindow.Row(r);
            w.Segmented(mode, modes, () => (int)planner.Mode, i => planner.Mode = (MechJebModuleManeuverPlanner.NodeMode)i, label: "#MechJeb_NewUI_Label_CreateAs"); //Create as
            w.Visible(mode.gameObject, () => planner.PlannerNodeCount() > 0);
            for (int i = 0; i < hintKeys.Length; i++)
            {
                int m = i;
                UIText hint = KodeWindow.Paragraph(r, hintKeys[i]);
                KodeWindow.ToneText(hint, KodeWindow.Tone.Dim);
                w.Visible(hint.gameObject, () => planner.PlannerNodeCount() > 0 && (int)planner.Mode == m);
            }

            // destructive on the left, the main action last on the right; the create buttons say what they do to the nodes
            KodeWindow.ActionBarRow bar = KodeWindow.ActionBar(r);
            UIButton removeAll = bar.Danger("#MechJeb_Maneu_button3", () => planner.Vessel.RemoveAllManeuverNodes()); //Remove ALL nodes
            GuidanceUi.Enable(w, removeAll, () => AnyNode);
            UIButton createExecute = bar.Normal(createExecuteKeys[0], () => CreateNodes(true));
            w.Visible(createExecute.gameObject, () => HasNode);
            Tooltips.Add(createExecute.gameObject, "#MechJeb_NewUI_NodeThenExecute_tooltip"); //Then executes the next node.
            UIButton create = bar.Primary(createKeys[0], () => CreateNodes(false));
            int shown = 0;
            w.AddSync(() =>
            {
                int m = planner.PlannerNodeCount() > 0 ? (int)planner.Mode : 0;
                if (m == shown) return;
                shown = m;
                create.Text(Localizer.Format(createKeys[m]));
                createExecute.Text(Localizer.Format(createExecuteKeys[m]));
            });
            // each button as wide as its longest label, so the window keeps its width when the choice changes
            WidestLabel(w, create, createKeys);
            WidestLabel(w, createExecute, createExecuteKeys);
        }

        /// <summary>
        /// The hovered operation's preview: the list reports its hovered entry to a <see cref="KodeKit.Headless.PreviewModel{T}"/>, which
        /// starts the planner's ghost after its debounce and removes it on a leave, Escape, a pick or the list closing. The list's own
        /// pick is the only setter; the model takes none.
        /// </summary>
        void PreviewOnHover(KodeWindow w, ComboBox combo, Operation[] operations, int[] order, int[] position)
        {
            var preview = new KodeKit.Headless.PreviewModel<int>(() => position[planner.OperationId], k => operations[order[k]].Previewable);
            Preview = preview;
            combo.EntryHovered += (k, source) => preview.Hover(k, source, UnityEngine.Time.unscaledTime);
            combo.EntryLeft += preview.Leave;
            combo.Model.Closed += preview.Revert;
            combo.Picked += (k, source) => preview.Commit(source);
            preview.Previewed += k => planner.Preview.Show(operations[order[k]]);
            preview.Reverted += planner.Preview.Clear;
            combo.EntryCard = (k, card) => PreviewCard(operations[order[k]], card);
            w.AddSync(() =>
            {
                preview.Enabled = Core.Settings != null && Core.Settings.UiPlannerPreview;
                preview.Tick(UnityEngine.Time.unscaledTime);
            });
        }

        // the hovered operation's card: its Δv and when it burns, live from the preview (a card opened at once, the pointer moving on from
        // another row's card, fills in once the debounced preview is computed); why there is no burn, in the Δv row; nothing for an
        // operation without a preview, or with the setting off
        void PreviewCard(Operation op, HoverCard card)
        {
            if (!op.Previewable || Core.Settings == null || !Core.Settings.UiPlannerPreview) return;
            ManeuverPreview p = planner.Preview;
            card.Title(op.GetName());
            card.Text("#MechJeb_NewUI_PreviewSettings", true); //A preview with its current settings: no node is placed.
            card.Value("#MechJeb_NewUI_PreviewDeltaV", ct => //Δv
            {
                if (p.Operation != op) ct.Append("...");
                else if (!p.Valid) ct.Append(string.IsNullOrEmpty(p.Error) ? "-" : p.Error);
                else
                {
                    ct.Append(p.DeltaV, 1).Append(" m/s");
                    if (!string.IsNullOrEmpty(p.Error)) ct.Append(" (").Append(p.Error).Append(')');
                }
            });
            card.Value("#MechJeb_NewUI_PreviewBurnIn", ct => //Burn in
            {
                if (p.Operation == op && p.Valid) ct.AppendDHMS(System.Math.Max(0, p.BurnUT - Planetarium.GetUniversalTime()), 0);
                else ct.Append("-");
            });
            if (!MapView.MapIsEnabled) card.Text("#MechJeb_NewUI_PreviewOpenMap", true); //Its orbit shows dashed in the map view.
        }

        // the button's text gets the width of its longest label, measured again when the theme changes the font or its size
        static void WidestLabel(KodeWindow w, UIButton b, string[] keys)
        {
            TMPro.TMP_Text t = b.ChildText.tmpText;
            UnityEngine.UI.LayoutElement le = b.ChildText.GetComponent<UnityEngine.UI.LayoutElement>() ?? b.ChildText.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            float size = -1;
            TMPro.TMP_FontAsset font = null;
            w.AddSync(() =>
            {
                if (t.fontSize == size && t.font == font) return;
                size = t.fontSize;
                font = t.font;
                float widest = 0;
                for (int i = 0; i < keys.Length; i++)
                    widest = UnityEngine.Mathf.Max(widest, t.GetPreferredValues(Localizer.Format(keys[i])).x);
                le.minWidth = UnityEngine.Mathf.Ceil(widest);
            });
        }

        // why Add to the last node did nothing, then the operation's error or warning
        string Problem()
        {
            string error = planner.CurrentOperation.GetErrorMessage();
            string add = planner.AddError;
            if (string.IsNullOrEmpty(add)) return error ?? "";
            return string.IsNullOrEmpty(error) ? add : add + "\n" + error;
        }

        void BuildExecute(KodeWindow w, UIObject r)
        {
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
