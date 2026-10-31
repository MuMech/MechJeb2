using System.Collections.Generic;
using KodeKit;
using KodeUI;
using KSP.Localization;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleNodeEditor ("Maneuver Node Editor") in KodeKit, row for row with its WindowGUI.
    /// The module keeps the state (selected node, edited components, deltas, snap); both UIs use its helpers.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleNodeEditor))]
    public class NodeEditorView : KodeView
    {
        readonly MechJebModuleNodeEditor ed;

        public NodeEditorView(MechJebModuleNodeEditor ed) : base(ed) => this.ed = ed;

        protected override float MinWidth => 300;

        List<ManeuverNode> Nodes => ed.Vessel.patchedConicSolver != null ? ed.Vessel.patchedConicSolver.maneuverNodes : null;
        int Count => Nodes?.Count ?? 0;
        int NodeIndex => Nodes?.IndexOf(ed.node) ?? -1;

        static readonly string[] relativityModeStrings = { "0", "1", "2", "3", "4" };

        // cached labels, so the bindings don't format a string every frame
        readonly string[] nodeLabels = new string[16];
        readonly string[] modeLabels = new string[5];

        string NodeLabel(int i)
        {
            // bindings also run while the panel is hidden; the label's cell is sized from its first text, so never ""
            if (i < 0) i = 0;
            if (i >= nodeLabels.Length) return "Maneuver node #" + (i + 1);
            return nodeLabels[i] ?? (nodeLabels[i] = "Maneuver node #" + (i + 1));
        }

        string ModeLabel()
        {
            int m = (int)ed.Vessel.patchedConicRenderer.relativityMode;
            if (m < 0 || m >= modeLabels.Length) return Localizer.Format("#MechJeb_NodeEd_Label9", ed.Vessel.patchedConicRenderer.relativityMode.ToString());
            return modeLabels[m] ?? (modeLabels[m] = Localizer.Format("#MechJeb_NodeEd_Label9", ed.Vessel.patchedConicRenderer.relativityMode.ToString()));
        }

        // the node choice WindowGUI made before drawing
        protected override void BeforeSync()
        {
            List<ManeuverNode> nodes = Nodes;
            if (nodes == null || nodes.Count == 0) return;
            ed.SelectNode(nodes.Count == 1 || !nodes.Contains(ed.node) ? nodes[0] : ed.node);
        }

        protected override void BuildContents(KodeWindow w)
        {
            if (KodeWindow.ModernLayout)
            {
                BuildModern(w);
                return;
            }

            UIObject r = w.Root;

            // no node
            Layout none = KodeWindow.Panel(r);
            KodeWindow.Label(none, "#MechJeb_NodeEd_Label1"); //"No maneuver nodes to edit."
            RelativityModeSelect(w, none);
            w.Visible(none.gameObject, () => Count == 0);

            Layout edit = KodeWindow.Panel(r);
            w.Visible(edit.gameObject, () => Count > 0);

            // node selector + merge, only with several nodes
            Layout selector = KodeWindow.Panel(edit);
            w.ArrowSelector(selector, () => NodeIndex, i => ed.SelectNode(Nodes[i]), () => Count, () => NodeLabel(NodeIndex));
            UIButton merge = KodeWindow.Button(selector, "#MechJeb_NodeEd_button1", () => ed.MergeNext(NodeIndex)); //"Merge next node"
            w.Visible(merge.gameObject, () => Count > 1 && NodeIndex < Count - 1);
            w.Visible(selector.gameObject, () => Count > 1);

            ComponentRow(w, edit, "#MechJeb_NodeEd_Label2", () => ed.prograde, v => ed.prograde = v, () => ed.progradeDelta); //"Prograde:"
            ComponentRow(w, edit, "#MechJeb_NodeEd_Label3", () => ed.radialPlus, v => ed.radialPlus = v, () => ed.radialPlusDelta); //"Radial+:"
            ComponentRow(w, edit, "#MechJeb_NodeEd_Label4", () => ed.normalPlus, v => ed.normalPlus = v, () => ed.normalPlusDelta); //"Normal+:"

            Layout deltas = KodeWindow.Row(edit);
            KodeWindow.Label(deltas, "#MechJeb_NodeEd_Label5", true); //"Set delta to:"
            string[] deltaLabels = { "0.01", "0.1", "1", "10", "100" };
            double[] deltaValues = { 0.01, 0.1, 1, 10, 100 };
            for (int i = 0; i < deltaValues.Length; i++)
            {
                double v = deltaValues[i];
                KodeWindow.Button(deltas, deltaLabels[i], () => ed.progradeDelta = ed.radialPlusDelta = ed.normalPlusDelta = v, true);
            }

            KodeWindow.Button(edit, "#MechJeb_NodeEd_button2", ed.UpdateNodeFromFields); //"Update"

            Layout shift = KodeWindow.Row(edit);
            KodeWindow.Label(shift, "#MechJeb_NodeEd_Label6", true); //"Shift time"
            KodeWindow.Button(shift, "-o", () => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT - ed.node.patch.period));
            KodeWindow.Button(shift, "-", () => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT - ed.timeOffset));
            EditUi.Field(w, shift, () => ed.timeOffset, 100);
            KodeWindow.Button(shift, "+", () => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT + ed.timeOffset));
            KodeWindow.Button(shift, "+o", () => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT + ed.node.patch.period));

            Layout snap = KodeWindow.Row(edit);
            KodeWindow.Button(snap, "#MechJeb_NodeEd_button3", ed.SnapNode, true); //"Snap node to"
            w.ArrowSelector(snap, () => (int)ed.snap, i => ed.snap = (MechJebModuleNodeEditor.Snap)i, MechJebModuleNodeEditor.numSnaps,
                () => ed.snapStrings[(int)ed.snap]);

            RelativityModeSelect(w, edit);

            ExecuteRows(w, edit, false);
        }

        // the node executor: Execute next node / Execute all nodes (with more than one node) / Abort, Auto-warp
        void ExecuteRows(KodeWindow w, UIObject parent, bool flexible)
        {
            MechJebCore core = ed.Core;
            NodeUi.ExecuteButtons(w, parent, core, ed, "#MechJeb_NodeEd_button4", "#MechJeb_NodeEd_button5", "#MechJeb_NodeEd_button6",
                () => true, () => Count > 1, flexible); //"Execute next node", "Execute all nodes", "Abort node execution"
            UnityEngine.UI.Toggle autowarp = NodeUi.AutowarpToggle(w, parent, core, "#MechJeb_NodeEd_checkbox1"); //"Auto-warp"
            w.Visible(autowarp, () => core.Node != null);
        }

        /// <summary>label [value] m/s [-] [delta] [+] m/s; -/+ repeat every 0.2 s while held, like LimitedRepeatButtoon.</summary>
        void ComponentRow(KodeWindow w, UIObject parent, string label, System.Func<EditableDouble> value, System.Action<EditableDouble> setValue,
            System.Func<EditableDouble> delta)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, label);
            EditUi.Field(w, row, value, 60);
            KodeWindow.Label(row, "m/s");
            KodeWindow.RepeatButton(row, "-", () =>
            {
                setValue(value() - delta());
                ed.UpdateNodeFromFields();
            });
            EditUi.Field(w, row, delta, 50);
            KodeWindow.RepeatButton(row, "+", () =>
            {
                setValue(value() + delta());
                ed.UpdateNodeFromFields();
            });
            KodeWindow.Label(row, "m/s");
        }

        // ---- modern layout: a node dropdown, steppers in a property grid with one step choice, grouped sections ----

        string[] nodeEntries = new string[0];

        string[] NodeEntries()
        {
            int n = Count;
            if (nodeEntries.Length == n) return nodeEntries;
            nodeEntries = new string[n];
            for (int i = 0; i < n; i++) nodeEntries[i] = NodeLabel(i);
            return nodeEntries;
        }

        static readonly string[] stepLabels = { "0.01", "0.1", "1", "10", "100" };
        static readonly double[] stepValues = { 0.01, 0.1, 1, 10, 100 };

        int StepIndex()
        {
            for (int i = 0; i < stepValues.Length; i++)
                if (System.Math.Abs(ed.progradeDelta.Val - stepValues[i]) < 1e-9) return i;
            return -1;
        }

        void BuildModern(KodeWindow w)
        {
            UIObject r = w.Root;

            Layout none = KodeWindow.Panel(r);
            KodeWindow.Label(none, "#MechJeb_NodeEd_Label1"); //"No maneuver nodes to edit."
            ModernRelativity(w, none);
            w.Visible(none.gameObject, () => Count == 0);

            Layout edit = KodeWindow.Panel(r);
            w.Visible(edit.gameObject, () => Count > 0);

            Layout selector = KodeWindow.Row(edit);
            new ComboBox(w, selector, () => NodeIndex, i => ed.SelectNode(Nodes[i]), NodeEntries);
            UIButton merge = KodeWindow.Button(selector, "#MechJeb_NodeEd_button1", () => ed.MergeNext(NodeIndex)); //"Merge next node"
            w.Visible(merge.gameObject, () => Count > 1 && NodeIndex < Count - 1);
            w.Visible(selector.gameObject, () => Count > 1);

            KodeWindow.Header(edit, "#MechJeb_NewUI_Burn"); //Burn
            KodeWindow.PropertyGrid grid = w.Properties(edit);
            grid.FlexibleColumn = 0; // label, stepper, unit: the labels take the room, the steppers line up
            StepperRow(w, grid, "#MechJeb_NodeEd_Label2", () => ed.prograde, v => ed.prograde = v, () => ed.progradeDelta); //"Prograde:"
            StepperRow(w, grid, "#MechJeb_NodeEd_Label3", () => ed.radialPlus, v => ed.radialPlus = v, () => ed.radialPlusDelta); //"Radial+:"
            StepperRow(w, grid, "#MechJeb_NodeEd_Label4", () => ed.normalPlus, v => ed.normalPlus = v, () => ed.normalPlusDelta); //"Normal+:"

            // one step for the three axes, chosen once
            Layout steps = KodeWindow.Row(edit);
            KodeWindow.Label(steps, "#MechJeb_NewUI_Step"); //Step
            w.Segmented(steps, stepLabels, StepIndex, i => ed.progradeDelta = ed.radialPlusDelta = ed.normalPlusDelta = stepValues[i], ChoiceStyle.Joined);
            KodeWindow.Label(steps, "m/s");
            KodeWindow.Button(edit, "#MechJeb_NodeEd_button2", ed.UpdateNodeFromFields, ButtonRole.Normal, true); //"Update"

            KodeWindow.Header(edit, "#MechJeb_NewUI_Time"); //Time
            Layout shift = KodeWindow.Row(edit);
            KodeWindow.Label(shift, "#MechJeb_NodeEd_Label6", true); //"Shift time"
            KodeWindow.Button(shift, "-o", () => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT - ed.node.patch.period));
            // the field is the shift; − and + move the node by it
            w.Stepper(shift, () => ed.timeOffset.Text, s => ed.timeOffset.Text = s, d => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT + d),
                () => ed.timeOffset, 90);
            KodeWindow.Button(shift, "+o", () => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT + ed.node.patch.period));

            Layout snap = KodeWindow.Row(edit);
            KodeWindow.Button(snap, "#MechJeb_NodeEd_button3", ed.SnapNode); //"Snap node to"
            new ComboBox(w, snap, () => (int)ed.snap, i => ed.snap = (MechJebModuleNodeEditor.Snap)i, () => ed.snapStrings);

            KodeWindow.Header(edit, "#MechJeb_NewUI_Display"); //Display
            ModernRelativity(w, edit);

            KodeWindow.Header(edit, "#MechJeb_NewUI_Execute"); //Execute
            ExecuteRows(w, edit, true);
        }

        /// <summary>label [−][value][+] m/s, the step being the axis's delta; − and + repeat while held.</summary>
        void StepperRow(KodeWindow w, KodeWindow.PropertyGrid grid, string label, System.Func<EditableDouble> value,
            System.Action<EditableDouble> setValue, System.Func<EditableDouble> delta)
        {
            Layout row = grid.Row();
            KodeWindow.Label(row, label);
            w.Stepper(row, () => value().Text, s => value().Text = s, d =>
            {
                setValue(value() + d);
                ed.UpdateNodeFromFields();
            }, () => delta(), 70, () => value().IsParsed, true);
            KodeWindow.Label(row, "m/s");
        }

        void ModernRelativity(KodeWindow w, UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, "#MechJeb_NodeEd_Label8"); //"Conics mode:"
            w.Segmented(row, relativityModeStrings, () => (int)ed.Vessel.patchedConicRenderer.relativityMode,
                i => ed.Vessel.patchedConicRenderer.relativityMode = (PatchRendering.RelativityMode)i);
            UIText mode = w.BoundLabel(parent, ModeLabel, true); //"Current mode: <<1>>"
            KodeWindow.ToneText(mode, KodeWindow.Tone.Dim);
        }

        void RelativityModeSelect(KodeWindow w, UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, "#MechJeb_NodeEd_Label8"); //"Conics mode:"
            w.SelectionGrid(row, relativityModeStrings, () => (int)ed.Vessel.patchedConicRenderer.relativityMode,
                i => ed.Vessel.patchedConicRenderer.relativityMode = (PatchRendering.RelativityMode)i);
            w.BoundLabel(parent, ModeLabel, true); //"Current mode: <<1>>"
        }

    }
}
