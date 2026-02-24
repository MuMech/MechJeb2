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
            bool isCelestialTarget = target.Target is CelestialBody;

            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(Capture, isCelestialTarget ? "Transfer" : "Rendezvous")) // two-burn Hohmann transfer with capture
                Capture = true;
            if (GUILayout.Toggle(!Capture, isCelestialTarget ? "Flyby / Impact" : "Intercept")) // single-burn intercept/flyby/impact transfer
                Capture = false;
            GUILayout.EndHorizontal();

            // are we trying to hit the target (transfer to celestial / rendezvous with ship) or just match an orbit
            if (Capture)
            {
                GUILayout.BeginHorizontal();
                Rendezvous = !GUILayout.Toggle(!Rendezvous, "Match orbit");
                GUILayout.EndHorizontal();
            }

            // arrival offset is for doing a transfer to e.g. 10 seconds behind a space station, or half the moon's period behind the moon
            if (Capture && Rendezvous)
                GuiUtils.SimpleTextBox(Localizer.Format("Arrival delay"), LagTime, "sec");

            // if we are planning a capture node (doing the math), do we also plot the maneuver node
            // (for a simple transfer to a Moon we don't allow this, without Match orbit or Arrival delay)
            if (Capture && (!isCelestialTarget || !Rendezvous || LagTime.Val != 0))
                PlanCapture = GUILayout.Toggle(PlanCapture, "Create arrival node");
            else
                PlanCapture = false;

            // coplanar transfer is for doing in-plane maneuver to the radius of the e.g. Moon and then the user does a MCC to intercept with a lower cost
            Coplanar = GUILayout.Toggle(Coplanar, "Coplanar only");
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

            double lagTime = Rendezvous ? LagTime.Val : 0;

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
                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(o, targetOrbit, universalTime, Rendezvous ? lagTime : 0, fixedTime, Coplanar, Capture && Rendezvous,
                    Capture);

            if (Capture && PlanCapture)
                return new List<ManeuverParameters> { new ManeuverParameters(dV1, ut1), new ManeuverParameters(dV2, ut2) };
            return new List<ManeuverParameters> { new ManeuverParameters(dV1, ut1) };
        }
    }
}
