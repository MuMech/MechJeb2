extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleNodeEditor : DisplayModule
    {
        // internal: also edited by MechJeb2.UI's window
        internal EditableDouble prograde = 0;
        internal EditableDouble radialPlus = 0;
        internal EditableDouble normalPlus = 0;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble progradeDelta = 0;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble radialPlusDelta = 0;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble normalPlusDelta = 0;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableTime timeOffset = 0;

        internal ManeuverNode node;
        private ManeuverGizmo gizmo;

        internal enum Snap { PERIAPSIS, APOAPSIS, REL_ASCENDING, REL_DESCENDING, EQ_ASCENDING, EQ_DESCENDING }

        internal static readonly int numSnaps = Enum.GetNames(typeof(Snap)).Length;
        internal Snap snap = Snap.PERIAPSIS;

        internal readonly string[] snapStrings = { Localizer.Format("#MechJeb_NodeEd_Snap1"), Localizer.Format("#MechJeb_NodeEd_Snap2"), Localizer.Format("#MechJeb_NodeEd_Snap3"), Localizer.Format("#MechJeb_NodeEd_Snap4"), Localizer.Format("#MechJeb_NodeEd_Snap5"), Localizer.Format("#MechJeb_NodeEd_Snap6") }; //"periapsis""apoapsis""AN with target""DN with target""equatorial AN""equatorial DN"

        private void GizmoUpdateHandler(Vector3d dV, double UT)
        {
            prograde = dV.z;
            radialPlus = dV.x;
            normalPlus = dV.y;
        }

        internal void MergeNext(int index)
        {
            ManeuverNode cur = Vessel.patchedConicSolver.maneuverNodes[index];
            ManeuverNode next = Vessel.patchedConicSolver.maneuverNodes[index + 1];

            double newUT = (cur.UT + next.UT) / 2;
            cur.UpdateNode(cur.patch.DeltaVToManeuverNodeCoordinates(newUT, cur.WorldDeltaV() + next.WorldDeltaV()), newUT);
            next.RemoveSelf();
        }

        /// <summary>Make `newNode` the edited node: reload the fields when it changed and follow its gizmo.</summary>
        internal void SelectNode(ManeuverNode newNode)
        {
            ManeuverNode oldNode = node;
            node = newNode;

            if (node != oldNode)
            {
                prograde = node.DeltaV.z;
                radialPlus = node.DeltaV.x;
                normalPlus = node.DeltaV.y;
            }

            if (gizmo != node.attachedGizmo)
            {
                if (gizmo != null) gizmo.OnGizmoUpdated -= GizmoUpdateHandler;
                gizmo = node.attachedGizmo;
                if (gizmo != null) gizmo.OnGizmoUpdated += GizmoUpdateHandler;
            }
        }

        /// <summary>Write the three edited components to the node.</summary>
        internal void UpdateNodeFromFields() => node.UpdateNode(new Vector3d(radialPlus, normalPlus, prograde), node.UT);

        /// <summary>Move the node by `dt` seconds. A move to a non-finite time or into the past is refused: the stock game misbehaves with either.</summary>
        internal void ShiftNode(double dt) => MoveNode(node.UT + dt);

        /// <summary>Move the node by `orbits` periods of its orbit. A hyperbolic orbit has no period, so the node stays.</summary>
        internal void ShiftNodeOrbits(int orbits)
        {
            if (node.patch.eccentricity < 1)
                ShiftNode(orbits * node.patch.period);
        }

        private void MoveNode(double UT)
        {
            if (UT.IsFinite() && UT >= Planetarium.GetUniversalTime())
                node.UpdateNode(node.DeltaV, UT);
        }

        /// <summary>Move the node to the selected snap point: the one nearest the node on an ellipse, and never one in the past.</summary>
        internal void SnapNode()
        {
            Orbit o = node.patch;
            double UT = node.UT;
            // a hyperbolic orbit has no period, so it has only one of each point to search for
            double searchFrom = o.eccentricity < 1 ? Math.Max(UT - o.period / 2, Planetarium.GetUniversalTime()) : Planetarium.GetUniversalTime();
            switch (snap)
            {
                case Snap.PERIAPSIS:
                    UT = o.NextPeriapsisTime(searchFrom);
                    break;

                case Snap.APOAPSIS:
                    if (o.eccentricity < 1) UT = o.NextApoapsisTime(searchFrom);
                    break;

                case Snap.EQ_ASCENDING:
                    if (o.AscendingNodeEquatorialExists()) UT = o.TimeOfAscendingNodeEquatorial(searchFrom);
                    break;

                case Snap.EQ_DESCENDING:
                    if (o.DescendingNodeEquatorialExists()) UT = o.TimeOfDescendingNodeEquatorial(searchFrom);
                    break;

                case Snap.REL_ASCENDING:
                    if (Core.Target.NormalTargetExists && Core.Target.TargetOrbit.referenceBody == o.referenceBody)
                    {
                        if (o.AscendingNodeExists(Core.Target.TargetOrbit))
                            UT = o.TimeOfAscendingNode(Core.Target.TargetOrbit, searchFrom);
                    }

                    break;

                case Snap.REL_DESCENDING:
                    if (Core.Target.NormalTargetExists && Core.Target.TargetOrbit.referenceBody == o.referenceBody)
                    {
                        if (o.DescendingNodeExists(Core.Target.TargetOrbit))
                            UT = o.TimeOfDescendingNode(Core.Target.TargetOrbit, searchFrom);
                    }

                    break;
            }

            MoveNode(UT);
        }

        private static readonly string[] relativityModeStrings = { "0", "1", "2", "3", "4" };

        public override string GetName() => Localizer.Format("#MechJeb_NodeEd_title"); //"Maneuver Node Editor"

        public override string IconName() => "Maneuver Node Editor";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();

        public MechJebModuleNodeEditor(MechJebCore core) : base(core) { }
    }
}
