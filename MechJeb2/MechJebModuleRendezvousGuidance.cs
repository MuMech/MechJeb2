extern alias JetBrainsAnnotations;
using System.Linq;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleRendezvousGuidance : DisplayModule
    {
        public MechJebModuleRendezvousGuidance(MechJebCore core) : base(core) { }

        internal readonly EditableDoubleMult phasingOrbitAltitude = new EditableDoubleMult(200000, 1000);

        // The maneuver planning actions, shared by the IMGUI window and the new UI view.

        internal void AlignPlanes()
        {
            double UT;
            Vector3d dV;
            if (Orbit.AscendingNodeExists(Core.Target.TargetOrbit))
            {
                dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(Orbit, Core.Target.TargetOrbit, VesselState.Time, out UT);
            }
            else
            {
                dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(Orbit, Core.Target.TargetOrbit, VesselState.Time, out UT);
            }

            Vessel.RemoveAllManeuverNodes();
            Vessel.PlaceManeuverNode(Orbit, dV, UT);
        }

        internal void EstablishNewOrbit()
        {
            const double leadTime = 30;
            double phasingOrbitRadius = phasingOrbitAltitude + MainBody.Radius;

            Vessel.RemoveAllManeuverNodes();
            if (Orbit.ApR < phasingOrbitRadius)
            {
                double UT1 = VesselState.Time + leadTime;
                Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(Orbit, UT1, phasingOrbitRadius);
                Vessel.PlaceManeuverNode(Orbit, dV1, UT1);
                Orbit transferOrbit = Vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                double UT2 = transferOrbit.NextApoapsisTime(UT1);
                Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                Vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
            }
            else if (Orbit.PeR > phasingOrbitRadius)
            {
                double UT1 = VesselState.Time + leadTime;
                Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(Orbit, UT1, phasingOrbitRadius);
                Vessel.PlaceManeuverNode(Orbit, dV1, UT1);
                Orbit transferOrbit = Vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                double UT2 = transferOrbit.NextPeriapsisTime(UT1);
                Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                Vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
            }
            else
            {
                double UT = Orbit.NextTimeOfRadius(VesselState.Time, phasingOrbitRadius);
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(Orbit, UT);
                Vessel.PlaceManeuverNode(Orbit, dV, UT);
            }
        }

        internal void InterceptWithHohmann()
        {
            (Vector3d dV, double UT, _, _) =
                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(Orbit, Core.Target.TargetOrbit, VesselState.Time, coplanar: false);
            Vessel.RemoveAllManeuverNodes();
            Vessel.PlaceManeuverNode(Orbit, dV, UT);
        }

        internal void MatchVelocitiesAtClosestApproach()
        {
            double UT = Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, VesselState.Time);
            Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, UT, Core.Target.TargetOrbit);
            Vessel.RemoveAllManeuverNodes();
            Vessel.PlaceManeuverNode(Orbit, dV, UT);
        }

        internal void GetCloser()
        {
            double UT = VesselState.Time;
            (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(Orbit, UT, Core.Target.TargetOrbit, 100, 10);
            Vessel.RemoveAllManeuverNodes();
            Vessel.PlaceManeuverNode(Orbit, dV, UT);
        }

        public override string GetName() => Localizer.Format("#MechJeb_RZplan_title"); //"Rendezvous Planner"

        public override string IconName() => "Rendezvous Planner";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
