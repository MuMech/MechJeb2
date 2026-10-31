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

        /// <summary>Move the node to the selected snap point.</summary>
        internal void SnapNode()
        {
            Orbit o = node.patch;
            double UT = node.UT;
            switch (snap)
            {
                case Snap.PERIAPSIS:
                    UT = o.NextPeriapsisTime(o.eccentricity < 1 ? UT - o.period / 2 : UT);
                    break;

                case Snap.APOAPSIS:
                    if (o.eccentricity < 1) UT = o.NextApoapsisTime(UT - o.period / 2);
                    break;

                case Snap.EQ_ASCENDING:
                    if (o.AscendingNodeEquatorialExists()) UT = o.TimeOfAscendingNodeEquatorial(UT - o.period / 2);
                    break;

                case Snap.EQ_DESCENDING:
                    if (o.DescendingNodeEquatorialExists()) UT = o.TimeOfDescendingNodeEquatorial(UT - o.period / 2);
                    break;

                case Snap.REL_ASCENDING:
                    if (Core.Target.NormalTargetExists && Core.Target.TargetOrbit.referenceBody == o.referenceBody)
                    {
                        if (o.AscendingNodeExists(Core.Target.TargetOrbit))
                            UT = o.TimeOfAscendingNode(Core.Target.TargetOrbit, UT - o.period / 2);
                    }

                    break;

                case Snap.REL_DESCENDING:
                    if (Core.Target.NormalTargetExists && Core.Target.TargetOrbit.referenceBody == o.referenceBody)
                    {
                        if (o.DescendingNodeExists(Core.Target.TargetOrbit))
                            UT = o.TimeOfDescendingNode(Core.Target.TargetOrbit, UT - o.period / 2);
                    }

                    break;
            }

            node.UpdateNode(node.DeltaV, UT);
        }

        private static float nextClick;

        private static bool LimitedRepeatButtoon(string text)
        {
            if (GUILayout.RepeatButton(text, GuiUtils.LayoutNoExpandWidth) && nextClick < Time.time)
            {
                nextClick = Time.time + 0.2f;
                return true;
            }

            return false;
        }

        private static readonly string[] relativityModeStrings = { "0", "1", "2", "3", "4" };

        public override string GetName() => Localizer.Format("#MechJeb_NodeEd_title"); //"Maneuver Node Editor"

        public override string IconName() => "Maneuver Node Editor";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();

        public MechJebModuleNodeEditor(MechJebCore core) : base(core) { }
    }
}
