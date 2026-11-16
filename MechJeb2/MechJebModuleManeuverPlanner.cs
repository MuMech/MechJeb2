extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using MechJebLibBindings;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleManeuverPlanner : DisplayModule
    {
        public MechJebModuleManeuverPlanner(MechJebCore core) : base(core) { }

        // Keep all Operation objects so parameters are saved
        private static readonly Operation[] _operation = Operation.GetAvailableOperations();

        private readonly string[] _operationNames =
            new List<Operation>(_operation).ConvertAll(x => x.GetName()).ToArray();

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public int _operationId;

        /// <summary>What Create node does with the planner's nodes.</summary>
        internal enum NodeMode { New, ChangeLast, AddToLast }

        private NodeMode _mode = NodeMode.New;

        // the new UI's "Create a maneuver" fold: open, except under Principia, which plans its own maneuvers
        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public bool showCreation = !ReflectionUtils.IsLoadedPrincipia;

        // why the last Add to the last node did nothing, for the operation it was tried with
        private string _addError;
        private Operation _addErrorOp;

        // the share of the orbital period an added burn may be away from the last node; a hyperbolic orbit allows a minute
        private const double ADD_MAX_PERIOD_SHARE = 0.02;
        private const double ADD_MAX_SECONDS_HYPERBOLIC = 60;

        // ---- shared with the new UI (MechJeb2.UI.ManeuverPlannerView) ----

        internal Operation[] Operations => _operation;
        internal string[] OperationNames => _operationNames;

        internal int OperationId
        {
            get => _operationId = Mathf.Clamp(_operationId, 0, _operation.Length - 1);
            set => _operationId = Mathf.Clamp(value, 0, _operation.Length - 1);
        }

        internal Operation CurrentOperation => _operation[OperationId];

        internal NodeMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                _addError = null;
            }
        }

        /// <summary>Why the last Add to the last node with the selected operation did nothing, or null.</summary>
        internal string AddError => _addErrorOp == CurrentOperation ? _addError : null;

        private MechJebModuleLandingPredictions _predictor;

        // Vessel.patchedConicSolver.maneuverNodes without the aerobrake node, without allocating (a view calls this every frame)
        private List<ManeuverNode> RawNodes => Vessel != null && Vessel.patchedConicSolver != null ? Vessel.patchedConicSolver.maneuverNodes : null;

        private ManeuverNode AerobrakeNode
        {
            get
            {
                _predictor ??= Core.GetComputerModule<MechJebModuleLandingPredictions>();
                return _predictor?.aerobrakeNode;
            }
        }

        internal int PlannerNodeCount()
        {
            List<ManeuverNode> nodes = RawNodes;
            if (nodes == null) return 0;
            ManeuverNode skip = AerobrakeNode;
            int n = 0;
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i] != skip) n++;
            return n;
        }

        internal ManeuverNode LastPlannerNode() => PlannerNodeFromEnd(0);

        // the planner node fromEnd places before the last one (0: the last), skipping the aerobrake node
        private ManeuverNode PlannerNodeFromEnd(int fromEnd)
        {
            List<ManeuverNode> nodes = RawNodes;
            if (nodes == null) return null;
            ManeuverNode skip = AerobrakeNode;
            for (int i = nodes.Count - 1; i >= 0; i--)
                if (nodes[i] != skip && fromEnd-- == 0)
                    return nodes[i];
            return null;
        }

        /// <summary>
        /// The orbit and universal time the next maneuver is computed from: after the last node when there is one, or, when
        /// changing the last node, after the node before it.
        /// </summary>
        internal void GetOrbitAndUT(out Orbit o, out double ut) => GetOrbitAndUT(_mode != NodeMode.ChangeLast, out o, out ut);

        /// <summary>
        /// GetOrbitAndUT for a given mode rather than the window's: after the last node to create new ones or add to the last one
        /// (<paramref name="createNode"/>), after the node before it to change the last one.
        /// </summary>
        internal void GetOrbitAndUT(bool createNode, out Orbit o, out double ut)
        {
            ut = VesselState.Time;
            o = Orbit;
            ManeuverNode from = PlannerNodeFromEnd(createNode ? 0 : 1);
            if (from != null)
            {
                ut = from.UT;
                o = from.nextPatch;
            }
        }

        /// <summary>The Create node / Create and execute buttons.</summary>
        internal void CreateNodes(bool execute, Orbit o, double ut)
        {
            _addError = null;
            // the operation has an error to show: leave the nodes alone, and don't execute the ones that were already there
            if (_mode == NodeMode.AddToLast ? !AddToLastNode(CurrentOperation, o, ut) : PlaceNodes(CurrentOperation, o, ut, _mode == NodeMode.ChangeLast) == null)
                return;

            if (execute && Core.Node != null)
                Core.Node.ExecuteOneNode(this);
        }

        /// <summary>
        /// Make <paramref name="op"/>'s nodes from orbit <paramref name="o"/> at <paramref name="ut"/> (see GetOrbitAndUT), and place
        /// them, after removing the last planner node when <paramref name="replaceLast"/>. Null, with the nodes left alone, when the
        /// operation fails: its GetErrorMessage says why.
        /// </summary>
        internal List<ManeuverNode> PlaceNodes(Operation op, Orbit o, double ut, bool replaceLast)
        {
            List<ManeuverParameters> nodeList = op.MakeNodes(o, ut, Core.Target);
            if (nodeList == null)
                return null;

            if (replaceLast)
                LastPlannerNode()?.RemoveSelf();

            var placed = new List<ManeuverNode>(nodeList.Count);
            for (int i = 0; i < nodeList.Count; i++)
                placed.Add(Vessel.PlaceManeuverNode(o, nodeList[i].dV, nodeList[i].UT));
            return placed;
        }

        /// <summary>
        /// Add <paramref name="op"/>'s burn, made from the orbit after the last planner node (see GetOrbitAndUT), to that node:
        /// one node at the last node's time with the sum of both burns. False, with the nodes left alone, when the operation
        /// fails (its GetErrorMessage says why), makes more than one burn, or puts its burn too far from the last node (AddError
        /// says why).
        /// </summary>
        internal bool AddToLastNode(Operation op, Orbit o, double ut)
        {
            ManeuverNode last = LastPlannerNode();
            if (last == null)
                return PlaceNodes(op, o, ut, false) != null;

            List<ManeuverParameters> nodeList = op.MakeNodes(o, ut, Core.Target);
            if (nodeList == null)
                return false;

            _addErrorOp = op;
            if (nodeList.Count != 1)
            {
                _addError = Localizer.Format("#MechJeb_NewUI_AddOneBurnOnly"); //Add to the last node takes a maneuver of one burn.
                return false;
            }

            Orbit after = last.nextPatch;
            double maxGap = after.eccentricity < 1 ? ADD_MAX_PERIOD_SHARE * after.period : ADD_MAX_SECONDS_HYPERBOLIC;
            double gap = Math.Abs(nodeList[0].UT - last.UT);
            if (gap > maxGap)
            {
                _addError = Localizer.Format("#MechJeb_NewUI_AddTooFar", GuiUtils.TimeToDHMS(gap), GuiUtils.TimeToDHMS(maxGap));
                //This burn is <<1>> from the last node, more than <<2>>: create it as a new node.
                return false;
            }

            // both burns are impulses at the last node's time, so their world vectors add
            Vector3d sum = last.WorldDeltaV() + nodeList[0].dV;
            last.UpdateNode(last.patch.DeltaVToManeuverNodeCoordinates(last.UT, sum), last.UT);
            return true;
        }

        public List<ManeuverNode> GetManeuverNodes()
        {
            MechJebModuleLandingPredictions predictor = Core.GetComputerModule<MechJebModuleLandingPredictions>();
            if (predictor == null) return Vessel.patchedConicSolver.maneuverNodes;
            return Vessel.patchedConicSolver.maneuverNodes.Where(n => n != predictor.aerobrakeNode).ToList();
        }

        // the node executor flying a node
        protected override bool AutopilotEngaged => Core.Node != null && Core.Node.Enabled;

        public override string GetName() => Localizer.Format("#MechJeb_Maneuver_Planner_title"); //Maneuver Planner

        public override string IconName() => "Maneuver Planner";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
