using System.Collections.Generic;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class OperationGeneric : Operation
    {
        private static readonly string _name = Localizer.Format("#MechJeb_Hohm_title");
        public override         string GetName() => _name;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Capture = true;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool PlanCapture = true;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Rendezvous = true;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble LagTime = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableTime MinDepartureUT = 0;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableTime MaxDepartureUT = 0;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Coplanar;

        private static readonly TimeReference[] _timeReferences =
        {
            TimeReference.COMPUTED, TimeReference.PERIAPSIS, TimeReference.APOAPSIS, TimeReference.X_FROM_NOW, TimeReference.ALTITUDE,
            TimeReference.EQ_DESCENDING, TimeReference.EQ_ASCENDING, TimeReference.REL_NEAREST_AD, TimeReference.REL_ASCENDING,
            TimeReference.REL_DESCENDING, TimeReference.CLOSEST_APPROACH
        };

        private static readonly TimeSelector _timeSelector = new TimeSelector(_timeReferences);

        public override void DoParametersGUI(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            Capture =
                !GUILayout.Toggle(!Capture, Localizer.Format("#MechJeb_Hohm_intercept_only")); //no capture burn (impact/flyby)
            if (Capture)
                PlanCapture = GUILayout.Toggle(PlanCapture, "Plan insertion burn");
            Coplanar = GUILayout.Toggle(Coplanar, Localizer.Format("#MechJeb_Hohm_simpleTransfer")); //coplanar maneuver
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(Rendezvous, "Rendezvous"))
                Rendezvous = true;
            if (GUILayout.Toggle(!Rendezvous, "Transfer"))
                Rendezvous = false;
            GUILayout.EndHorizontal();
            if (Rendezvous)
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Hohm_Label1"), LagTime, "sec"); //fractional target period offset

            _timeSelector.DoChooseTimeGUI();
        }

        protected override List<ManeuverParameters> MakeNodesImpl(Orbit o, double universalTime, MechJebModuleTargetController target)
        {
            if (!target.NormalTargetExists)
                throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception1")); //must select a target for the bi-impulsive transfer.

            if (o.referenceBody != target.TargetOrbit.referenceBody)
                throw
                    new OperationException(
                        Localizer.Format("#MechJeb_Hohm_Exception2")); //target for bi-impulsive transfer must be in the same sphere of influence.

            bool anExists = o.AscendingNodeExists(target.TargetOrbit);
            bool dnExists = o.DescendingNodeExists(target.TargetOrbit);

            switch (_timeSelector.TimeReference)
            {
                case TimeReference.REL_ASCENDING when !anExists:
                    throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception3")); //ascending node with target doesn't exist.
                case TimeReference.REL_DESCENDING when !dnExists:
                    throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception4")); //descending node with target doesn't exist.
                case TimeReference.REL_NEAREST_AD when !(anExists || dnExists):
                    throw new OperationException(
                        Localizer.Format("#MechJeb_Hohm_Exception5")); //neither ascending nor descending node with target exists.
            }

            double ut = _timeSelector.ComputeManeuverTime(o, universalTime, target);

            if (target.Target is CelestialBody && Capture && PlanCapture)
                ErrorMessage =
                    "Insertion burn to a celestial with an SOI is not supported by this maneuver.  A Transfer-to-Moon maneuver needs to be written to properly support this case.";

            Orbit targetOrbit = target.TargetOrbit;

            double lagTime = Rendezvous ? LagTime.val : 0;

            (Vector3d dV1, double ut1, Vector3d dV2, double ut2) =
                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, targetOrbit, ut, lagTime, Coplanar, Rendezvous, Capture);

            if (Capture && PlanCapture)
                return new List<ManeuverParameters> { new ManeuverParameters(dV1, ut1), new ManeuverParameters(dV2, ut2) };
            return new List<ManeuverParameters> { new ManeuverParameters(dV1, ut1) };
        }
    }
}
