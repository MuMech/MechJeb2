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
        int NodeIndex => Nodes.IndexOf(ed.node);

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
            EditFields.Field(w, shift, () => ed.timeOffset.Text, s => ed.timeOffset.Text = s, 100);
            KodeWindow.Button(shift, "+", () => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT + ed.timeOffset));
            KodeWindow.Button(shift, "+o", () => ed.node.UpdateNode(ed.node.DeltaV, ed.node.UT + ed.node.patch.period));

            Layout snap = KodeWindow.Row(edit);
            KodeWindow.Button(snap, "#MechJeb_NodeEd_button3", ed.SnapNode, true); //"Snap node to"
            w.ArrowSelector(snap, () => (int)ed.snap, i => ed.snap = (MechJebModuleNodeEditor.Snap)i, MechJebModuleNodeEditor.numSnaps,
                () => ed.snapStrings[(int)ed.snap]);

            RelativityModeSelect(w, edit);

            // node executor
            MechJebCore core = ed.Core;
            bool HasNode() => core.Node != null;
            UIButton execOne = KodeWindow.Button(edit, "#MechJeb_NodeEd_button4", () => core.Node.ExecuteOneNode(ed)); //"Execute next node"
            w.Visible(execOne.gameObject, () => HasNode() && !core.Node.Enabled);
            UIButton execAll = KodeWindow.Button(edit, "#MechJeb_NodeEd_button5", () => core.Node.ExecuteAllNodes(ed)); //"Execute all nodes"
            w.Visible(execAll.gameObject, () => HasNode() && !core.Node.Enabled && Count > 1);
            UIButton abort = KodeWindow.Button(edit, "#MechJeb_NodeEd_button6", () => core.Node.Abort()); //"Abort node execution"
            w.Visible(abort.gameObject, () => HasNode() && core.Node.Enabled);
            UnityEngine.UI.Toggle autowarp = w.Toggle(edit, "#MechJeb_NodeEd_checkbox1", () => core.Node.Autowarp, v => core.Node.Autowarp = v); //"Auto-warp"
            w.Visible(autowarp.transform.parent.gameObject, HasNode);
        }

        /// <summary>label [value] m/s [-] [delta] [+] m/s; -/+ repeat every 0.2 s while held, like LimitedRepeatButtoon.</summary>
        void ComponentRow(KodeWindow w, UIObject parent, string label, System.Func<EditableDouble> value, System.Action<EditableDouble> setValue,
            System.Func<EditableDouble> delta)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, label);
            EditFields.Field(w, row, () => value().Text, s => value().Text = s, 60, () => value().IsParsed);
            KodeWindow.Label(row, "m/s");
            KodeWindow.RepeatButton(row, "-", () =>
            {
                setValue(value() - delta());
                ed.UpdateNodeFromFields();
            });
            EditFields.Field(w, row, () => delta().Text, s => delta().Text = s, 50, () => delta().IsParsed);
            KodeWindow.RepeatButton(row, "+", () =>
            {
                setValue(value() + delta());
                ed.UpdateNodeFromFields();
            });
            KodeWindow.Label(row, "m/s");
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
