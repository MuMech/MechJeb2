using System.Collections.Generic;
using KodeKit;
using KodeKit.Headless;
using KodeUI;
using KSP.Localization;
using UnityEngine;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleNodeEditor ("Maneuver Node Editor") in KodeKit.
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

        // the conics modes by name, in PatchRendering.RelativityMode's order
        static readonly string[] relativityModeKeys =
        {
            "#MechJeb_NewUI_ConicsLocal", "#MechJeb_NewUI_ConicsSoiEntry", "#MechJeb_NewUI_ConicsSoiExit", "#MechJeb_NewUI_ConicsRelative",
            "#MechJeb_NewUI_ConicsDynamic"
        };

        // cached labels, so the bindings don't format a string every frame
        readonly string[] nodeLabels = new string[16];

        string NodeLabel(int i)
        {
            // bindings also run while the panel is hidden; the label's cell is sized from its first text, so never ""
            if (i < 0) i = 0;
            if (i >= nodeLabels.Length) return "Maneuver node #" + (i + 1);
            return nodeLabels[i] ?? (nodeLabels[i] = "Maneuver node #" + (i + 1));
        }

        // the node choice, made before the bindings
        protected override void BeforeSync()
        {
            List<ManeuverNode> nodes = Nodes;
            if (nodes == null || nodes.Count == 0) return;
            ed.SelectNode(nodes.Count == 1 || !nodes.Contains(ed.node) ? nodes[0] : ed.node);
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

        // ---- A node dropdown, steppers in a property grid with one step choice, grouped sections ----

        string[] nodeEntries = new string[0];
        ManeuverNode[] listedNodes = new ManeuverNode[0];

        // a new array when the nodes change, not only their count: the labels are by position, so a node removed and another
        // added in the same frame give the same texts, and an open drop-down list must still close
        string[] NodeEntries()
        {
            List<ManeuverNode> nodes = Nodes;
            int n = nodes?.Count ?? 0;
            bool same = listedNodes.Length == n;
            for (int i = 0; same && i < n; i++) same = nodes[i] == listedNodes[i];
            if (same) return nodeEntries;
            listedNodes = n == 0 ? new ManeuverNode[0] : nodes.ToArray();
            nodeEntries = new string[n];
            for (int i = 0; i < n; i++) nodeEntries[i] = NodeLabel(i);
            return nodeEntries;
        }

        static readonly string[] stepLabels = { "0.01", "0.1", "1", "10", "100" };
        static readonly double[] stepValues = { 0.01, 0.1, 1, 10, 100 };

        // the step of the three axes; a saved step of 0 (the old default) steps by 1
        double Step() => ed.progradeDelta.Val > 0 ? ed.progradeDelta.Val : 1;

        int StepIndex()
        {
            for (int i = 0; i < stepValues.Length; i++)
                if (System.Math.Abs(Step() - stepValues[i]) < 1e-9) return i;
            return -1;
        }

        /// <summary>
        /// What an undo of the node editor restores: the edited node's burn and time. The Δv and time fields write texts, and the
        /// edit is the node: typing is no undo step until the typed value reaches the node (a commit of the field's target, at the
        /// end of the edit). A node that was executed or deleted, one no longer selected, or a time now in the past, isn't restored:
        /// undo never re-plans a burn the vessel has made, nor writes a node the fields don't show. The stack restores a state only
        /// while the node is still as the change left it, so a gizmo drag or a stock node edit since makes the undo stale.
        /// </summary>
        sealed class NodeState : IUndoState
        {
            readonly NodeEditorView view;

            public NodeState(NodeEditorView view) => this.view = view;

            sealed class Shot
            {
                public ManeuverNode Node;
                public Vector3d DeltaV;
                public double UT;

                public override bool Equals(object o) => o is Shot s && s.Node == Node && s.DeltaV == DeltaV && s.UT == UT;

                public override int GetHashCode() => UT.GetHashCode();
            }

            public object Take()
            {
                ManeuverNode n = view.ed.node;
                if (n == null || view.Nodes == null || !view.Nodes.Contains(n)) return null;
                return new Shot { Node = n, DeltaV = n.DeltaV, UT = n.UT };
            }

            public bool Restore(object state)
            {
                if (!(state is Shot s)) return false;
                List<ManeuverNode> nodes = view.Nodes;
                if (nodes == null || !nodes.Contains(s.Node)) return false;
                // another node selected since: the fields show that node, and an undo or redo mustn't write this one under them
                if (view.ed.node != s.Node) return false;
                if (double.IsNaN(s.UT) || double.IsInfinity(s.UT) || s.UT <= Planetarium.GetUniversalTime()) return false;
                s.Node.UpdateNode(s.DeltaV, s.UT);
                view.ed.SelectNode(s.Node);
                // the fields show the restored burn, also when they held a typed value the node never had (SelectNode reloads them
                // only when the burn differs from what they last loaded)
                view.ed.prograde = s.DeltaV.z;
                view.ed.radialPlus = s.DeltaV.x;
                view.ed.normalPlus = s.DeltaV.y;
                return true;
            }
        }

        NodeState nodeState;

        // an edit of the node: its undo restores the node's burn and time
        void UndoesTheNode(KodeWindow w, UIObject control)
        {
            CommitTarget t = w.Commits.Find(control);
            if (t != null) t.State = nodeState ??= new NodeState(this);
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            nodeState = null;

            Layout none = KodeWindow.Panel(r);
            KodeWindow.ToneText(KodeWindow.Paragraph(none, "#MechJeb_NodeEd_Label1"), KodeWindow.Tone.Dim); //"No maneuver node: create one in the Maneuver Planner."
            Relativity(w, none);
            w.Visible(none.gameObject, () => Count == 0);

            Layout edit = KodeWindow.Panel(r);
            w.Visible(edit.gameObject, () => Count > 0);

            Layout selector = KodeWindow.Row(edit);
            new ComboBox(w, selector, () => NodeIndex, i => ed.SelectNode(Nodes[i]), NodeEntries, name: "#MechJeb_NewUI_Label_Node"); //Node
            UIButton merge = KodeWindow.Button(selector, "#MechJeb_NodeEd_button1", () => ed.MergeNext(NodeIndex)); //"Merge next node"
            w.Visible(merge.gameObject, () => Count > 1 && NodeIndex < Count - 1);
            w.Visible(selector.gameObject, () => Count > 1);

            KodeWindow.Header(edit, "#MechJeb_NewUI_Burn"); //Burn
            KodeWindow.PropertyGrid grid = w.Properties(edit);
            grid.FlexibleColumn = 0; // label, stepper, unit: the labels take the room, the steppers line up
            StepperRow(w, grid, "#MechJeb_NewUI_Prograde", () => ed.prograde, v => ed.prograde = v); //Prograde
            StepperRow(w, grid, "#MechJeb_NewUI_RadialOut", () => ed.radialPlus, v => ed.radialPlus = v); //Radial out
            StepperRow(w, grid, "#MechJeb_NewUI_Normal", () => ed.normalPlus, v => ed.normalPlus = v); //Normal

            // one step for the three axes, chosen once
            Layout steps = KodeWindow.Row(edit);
            KodeWindow.Label(steps, "#MechJeb_NewUI_Step"); //Step
            w.Segmented(steps, stepLabels, StepIndex, i => ed.progradeDelta = ed.radialPlusDelta = ed.normalPlusDelta = stepValues[i], ChoiceStyle.Joined);
            KodeWindow.Label(steps, "m/s");

            KodeWindow.Header(edit, "#MechJeb_NewUI_Time"); //Time
            Layout shift = KodeWindow.Row(edit);
            KodeWindow.Label(shift, "#MechJeb_NodeEd_Label6", true); //"Shift time"
            CommitTarget<string> time = null;
            UIButton earlier = KodeWindow.Button(shift, "-o", () => time.Apply(-1, ed.ShiftNodeOrbits, KodeWindow.Pointer));
            Tooltips.Add(earlier.gameObject, "#MechJeb_NewUI_OrbitEarlier_tooltip"); //Move the node one orbit earlier.
            // the field is the shift; − and + move the node by it
            Layout shiftStepper = w.Stepper(shift, () => ed.timeOffset.Text, s => ed.timeOffset.Text = s, ed.ShiftNode,
                () => ed.timeOffset, 90, label: "#MechJeb_NodeEd_Label6"); //"Shift time"
            UndoesTheNode(w, shiftStepper);
            // the orbit shifts and the snap are edits of the node's time too: commits of the shift's target, undone with it
            time = (CommitTarget<string>)w.Commits.Find(shiftStepper);
            UIButton later = KodeWindow.Button(shift, "+o", () => time.Apply(1, ed.ShiftNodeOrbits, KodeWindow.Pointer));
            Tooltips.Add(later.gameObject, "#MechJeb_NewUI_OrbitLater_tooltip"); //Move the node one orbit later.

            Layout snap = KodeWindow.Row(edit);
            KodeWindow.Button(snap, "#MechJeb_NodeEd_button3", () => time.Apply(0, _ => ed.SnapNode(), KodeWindow.Pointer)); //"Snap node to"
            new ComboBox(w, snap, () => (int)ed.snap, i => ed.snap = (MechJebModuleNodeEditor.Snap)i, () => ed.snapStrings, name: "#MechJeb_NodeEd_button3"); //"Snap node to"

            KodeWindow.Header(edit, "#MechJeb_NewUI_Display"); //Display
            Relativity(w, edit);

            KodeWindow.Header(edit, "#MechJeb_NewUI_Execute"); //Execute
            ExecuteRows(w, edit, true);

            // undo and redo of this window's edits, held while the node executor flies a node: it reads the node and the settings
            w.Undo.Guard = _ => ed.Core.Node == null || !ed.Core.Node.Enabled;
            w.UndoButtons();
        }

        /// <summary>
        /// label [−][value][+] m/s; − and + repeat while held. − and + apply at once, a typed value on Enter or when the field
        /// loses focus.
        /// </summary>
        void StepperRow(KodeWindow w, KodeWindow.PropertyGrid grid, string label, System.Func<EditableDouble> value,
            System.Action<EditableDouble> setValue)
        {
            Layout row = grid.Row();
            KodeWindow.Label(row, label);
            Layout stepper = w.Stepper(row, () => value().Text, s => value().Text = s, d =>
            {
                setValue(value() + d);
                ed.UpdateNodeFromFields();
            }, Step, 70, () => value().IsParsed, true);
            UndoesTheNode(w, stepper);
            // the typed value reaches the node when the edit ends: a commit of the stepper's target, so the undo stack sees the node's
            // state before and after it (the typing itself changed only the field's text, and is no undo step)
            var target = (CommitTarget<string>)w.Commits.Find(stepper);
            stepper.GetComponentInChildren<TMPro.TMP_InputField>().onEndEdit.AddListener(_ =>
            {
                if (Count > 0 && ed.node != null && value().IsParsed)
                    target.Apply(value().Text, typed => ed.UpdateNodeFromFields(), KodeWindow.Source(ChangeSource.Keyboard));
            });
            KodeWindow.Label(row, "m/s");
        }

        void Relativity(KodeWindow w, UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            KodeWindow.Label(row, "#MechJeb_NewUI_ConicsMode", true); //Conics mode
            var names = new string[relativityModeKeys.Length];
            for (int i = 0; i < names.Length; i++) names[i] = Localizer.Format(relativityModeKeys[i]);
            new ComboBox(w, row, () => (int)ed.Vessel.patchedConicRenderer.relativityMode,
                i => ed.Vessel.patchedConicRenderer.relativityMode = (PatchRendering.RelativityMode)i, () => names);
        }

    }
}
