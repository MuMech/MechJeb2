extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
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
        public bool MatchOrbit = false;

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
            bool isCelestialTarget = target.Target is CelestialBody;

            // two-burn capture vs single-burn intercept/flyby
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(Capture, isCelestialTarget
                    ? Localizer.Format("#MechJeb_Hohm_transfer") //Transfer
                    : Localizer.Format("#MechJeb_Hohm_rendezvous"))) //Rendezvous
                Capture = true;
            if (GUILayout.Toggle(!Capture, isCelestialTarget
                    ? Localizer.Format("#MechJeb_Hohm_flyby") //Flyby / Impact
                    : Localizer.Format("#MechJeb_Hohm_intercept"))) //Intercept
                Capture = false;
            GUILayout.EndHorizontal();

            // match the target's orbit rather than intercepting the target itself
            if (Capture)
            {
                GUILayout.BeginHorizontal();
                MatchOrbit = GUILayout.Toggle(MatchOrbit, Localizer.Format("#MechJeb_Hohm_matchOrbit")); //Match orbit
                GUILayout.EndHorizontal();
            }

            // arrival delay offsets the intercept point (e.g. 10 seconds behind a station, or half a moon's period)
            if (Capture && !MatchOrbit)
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Hohm_arrivalDelay"), LagTime, "sec"); //Arrival delay

            // optionally create a maneuver node for the arrival burn
            // (not offered for a direct transfer to a celestial without match orbit or arrival delay)
            if (Capture && (!isCelestialTarget || MatchOrbit || LagTime.Val != 0))
                PlanCapture = GUILayout.Toggle(PlanCapture, Localizer.Format("#MechJeb_Hohm_createArrivalNode")); //Create arrival node
            else
                PlanCapture = false;

            // coplanar restricts the transfer to the parking orbit plane (user can add a mid-course correction)
            Coplanar = GUILayout.Toggle(Coplanar, Localizer.Format("#MechJeb_Hohm_simpleTransfer")); //Coplanar only
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

            Orbit targetOrbit = target.TargetOrbit;

            double lagTime = !MatchOrbit ? LagTime.Val : 0;

            bool fixedTime = false;

            if (_timeSelector.TimeReference != TimeReference.COMPUTED)
            {
                bool anExists = o.AscendingNodeExists(target.TargetOrbit);
                bool dnExists = o.DescendingNodeExists(target.TargetOrbit);

                if (_timeSelector.TimeReference == TimeReference.REL_ASCENDING && !anExists)
                    throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception3")); //ascending node with target doesn't exist.

                if (_timeSelector.TimeReference == TimeReference.REL_DESCENDING && !dnExists)
                    throw new OperationException(Localizer.Format("#MechJeb_Hohm_Exception4")); //descending node with target doesn't exist.

                if (_timeSelector.TimeReference == TimeReference.REL_NEAREST_AD && !(anExists || dnExists))
                    throw new OperationException(
                        Localizer.Format("#MechJeb_Hohm_Exception5")); //neither ascending nor descending node with target exists.

                universalTime = _timeSelector.ComputeManeuverTime(o, universalTime, target);
                fixedTime     = true;
            }

            (Vector3d dV1, double ut1, Vector3d dV2, double ut2) =
                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, targetOrbit, universalTime, lagTime, fixedTime, Coplanar, Capture && !MatchOrbit,
                    Capture);

            if (Capture && PlanCapture)
                return new List<ManeuverParameters> { new ManeuverParameters(dV1, ut1), new ManeuverParameters(dV2, ut2) };
            return new List<ManeuverParameters> { new ManeuverParameters(dV1, ut1) };
        }
    }
}
