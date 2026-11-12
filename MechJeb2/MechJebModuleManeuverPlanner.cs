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

        // Creation or replacement mode
        private bool _createNode = true;

        // ---- shared with the new UI (MechJeb2.UI.ManeuverPlannerView) ----

        internal Operation[] Operations => _operation;
        internal string[] OperationNames => _operationNames;

        internal int OperationId
        {
            get => _operationId = Mathf.Clamp(_operationId, 0, _operation.Length - 1);
            set => _operationId = Mathf.Clamp(value, 0, _operation.Length - 1);
        }

        internal Operation CurrentOperation => _operation[OperationId];

        internal bool CreateNodeMode
        {
            get => _createNode;
            set => _createNode = value;
        }

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
        internal void GetOrbitAndUT(out Orbit o, out double ut) => GetOrbitAndUT(_createNode, out o, out ut);

        /// <summary>
        /// GetOrbitAndUT for a given mode rather than the window's: after the last node to create new ones (<paramref name="createNode"/>),
        /// after the node before it to change the last one.
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
            // the operation has an error to show: leave the nodes alone, and don't execute the ones that were already there
            if (PlaceNodes(CurrentOperation, o, ut, !_createNode) == null)
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

        public List<ManeuverNode> GetManeuverNodes()
        {
            MechJebModuleLandingPredictions predictor = Core.GetComputerModule<MechJebModuleLandingPredictions>();
            if (predictor == null) return Vessel.patchedConicSolver.maneuverNodes;
            return Vessel.patchedConicSolver.maneuverNodes.Where(n => n != predictor.aerobrakeNode).ToList();
        }

        public override string GetName() => Localizer.Format("#MechJeb_Maneuver_Planner_title"); //Maneuver Planner

        public override string IconName() => "Maneuver Planner";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
