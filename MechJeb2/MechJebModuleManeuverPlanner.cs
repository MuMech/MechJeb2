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
        internal void GetOrbitAndUT(out Orbit o, out double ut)
        {
            ut = VesselState.Time;
            o = Orbit;
            ManeuverNode from = PlannerNodeFromEnd(_createNode ? 0 : 1);
            if (from != null)
            {
                ut = from.UT;
                o = from.nextPatch;
            }
        }

        /// <summary>The Create node / Create and execute buttons.</summary>
        internal void CreateNodes(bool execute, Orbit o, double ut)
        {
            List<ManeuverParameters> nodeList = CurrentOperation.MakeNodes(o, ut, Core.Target);

            // the operation has an error to show: leave the nodes alone, and don't execute the ones that were already there
            if (nodeList == null)
                return;

            if (!_createNode)
                LastPlannerNode().RemoveSelf();
            for (int i = 0; i < nodeList.Count; i++)
            {
                Vessel.PlaceManeuverNode(o, nodeList[i].dV, nodeList[i].UT);
            }

            if (execute && Core.Node != null)
                Core.Node.ExecuteOneNode(this);
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
