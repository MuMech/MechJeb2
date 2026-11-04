extern alias JetBrainsAnnotations;
using System.Collections.Generic;
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

        private MechJebModuleRendezvousAutopilot Autopilot => Core.GetComputerModule<MechJebModuleRendezvousAutopilot>();

        // Only a warning: the autopilot refuses this, but a player planning by hand may know better.
        internal bool OnTheGround
        {
            get
            {
                Vessel target = Core.Target.Target?.GetVessel();
                return Vessel.LandedOrSplashed || (target != null && target.LandedOrSplashed);
            }
        }

        // The maneuver planning actions, called by the view.

        internal void AlignPlanes()
        {
            if (!OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanes(Orbit, Core.Target.TargetOrbit, VesselState.Time, out Vector3d dV, out double UT))
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_RZplan_noNode"), 5);
                return;
            }

            Vessel.RemoveAllManeuverNodes();
            Vessel.PlaceManeuverNode(Orbit, dV, UT);
        }

        internal void EstablishNewOrbit()
        {
            const double leadTime = 30;
            double phasingOrbitRadius = phasingOrbitAltitude + MainBody.Radius;

            if (phasingOrbitRadius >= MainBody.sphereOfInfluence)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_RZplan_outsideSOI"), 5);
                return;
            }

            List<ManeuverParameters> maneuvers = OrbitalManeuverCalculator.ManeuversToCircularOrbit(Orbit, VesselState.Time + leadTime, phasingOrbitRadius);
            double earliestUT = Autopilot.EarliestBurnUT(maneuvers[0].dV.magnitude);
            if (maneuvers[0].UT < earliestUT)
                maneuvers = OrbitalManeuverCalculator.ManeuversToCircularOrbit(Orbit, earliestUT, phasingOrbitRadius);

            Vessel.RemoveAllManeuverNodes();
            foreach (ManeuverParameters maneuver in maneuvers)
                Vessel.PlaceManeuverNode(Orbit, maneuver.dV, maneuver.UT);
        }

        // Only a warning: a player can know that an orbit under the highest peak is clear where it goes.
        internal bool PhasingOrbitIsSafe => MainBody.IsSafeOrbitRadius(phasingOrbitAltitude + MainBody.Radius);

        internal void InterceptWithHohmann()
        {
            (Vector3d dV, double UT, _, _) =
                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(Orbit, Core.Target.TargetOrbit, VesselState.Time, coplanar: false);

            double earliestUT = Autopilot.EarliestBurnUT(dV.magnitude);
            if (UT < earliestUT)
            {
                (dV, UT, _, _) = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(Orbit, Core.Target.TargetOrbit, earliestUT,
                    fixedTime: true, coplanar: false);
            }

            Vessel.RemoveAllManeuverNodes();
            Vessel.PlaceManeuverNode(Orbit, dV, UT);
        }

        internal void MatchVelocitiesAtClosestApproach()
        {
            double closestApproachUT = Orbit.MinimumSeparationTime(Core.Target.TargetOrbit, VesselState.Time);
            Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, closestApproachUT, Core.Target.TargetOrbit);
            Vessel.RemoveAllManeuverNodes();

            // A burn centered on the closest approach would end at it, which can be inside the target. Use the autopilot's stand-off.
            Vessel.PlaceManeuverNode(Orbit, dV, Autopilot.BurnUTToStopAtDesiredDistance(closestApproachUT));
        }

        internal void GetCloser()
        {
            // the range of the autopilot's close-in phase: from farther away this costs about a tenth of the distance in m/s
            double distance = Core.Target.Distance;
            if (distance >= VesselState.Radius / 25)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_RZplan_tooFar"), 5);
                return;
            }

            double closingTime = Autopilot.ClosingTime(distance);
            (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(Orbit, VesselState.Time, Core.Target.TargetOrbit, closingTime, 10);
            double UT = Autopilot.EarliestBurnUT(dV.magnitude);
            (dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(Orbit, UT, Core.Target.TargetOrbit, closingTime, 10);
            Vessel.RemoveAllManeuverNodes();
            Vessel.PlaceManeuverNode(Orbit, dV, UT);
        }

        public override string GetName() => Localizer.Format("#MechJeb_RZplan_title"); //"Rendezvous Planner"

        public override string IconName() => "Rendezvous Planner";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
