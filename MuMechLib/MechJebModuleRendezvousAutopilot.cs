using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{
    public class MechJebModuleRendezvousAutopilot : ComputerModule
    {

        public MechJebModuleRendezvousAutopilot(MechJebCore core) : base(core) { }

        public string status = "";

        public override void Drive(FlightCtrlState s)
        {
            if (!core.target.NormalTargetExists)
            {
                this.enabled = false;
                return;
            }

            core.node.autowarp = core.target.Distance > 1000; //don't warp when close to target, because warping introduces small perturbations

            if (vessel.patchedConicSolver.maneuverNodes.Any())
            {
                if (!core.node.enabled) core.node.ExecuteAllNodes();
            }
            else if (core.target.Distance < 100 && core.target.RelativeVelocity.magnitude < 1)
            {
                //finished
                this.enabled = false;
                s.mainThrottle = 0;
                status = "Successful rendezvous";
            }
            else if (core.target.Distance < 100)
            {
                double UT = vesselState.time;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.Orbit);
                vessel.PlaceManeuverNode(orbit, dV, UT);
                status = "Within 100m: matching velocities.";

                core.node.leadFraction = 0.5;
            }
            else if (core.target.Distance < vesselState.radius / 50)
            {
                if (orbit.NextClosestApproachDistance(core.target.Orbit, vesselState.time) < 100
                    && orbit.NextClosestApproachTime(core.target.Orbit, vesselState.time) < vesselState.time + 150)
                {
                    //We're close to the target, and on a course that will take us closer. Kill relvel at closest approach
                    double UT = orbit.NextClosestApproachTime(core.target.Orbit, vesselState.time);
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.Orbit);
                    vessel.PlaceManeuverNode(orbit, dV, UT);
                    core.node.leadFraction = 0.5;

                    status = "Planning to match velocities at closest approach.";
                }
                else
                {
                    //We're not far from the target. Close the distance
                    double closingSpeed = core.target.Distance / 100;
                    if (closingSpeed > 100) closingSpeed = 100;
                    double closingTime = core.target.Distance / closingSpeed;

                    double UT = vesselState.time + 15;
                    double interceptUT = UT + closingTime;
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.target.Orbit, interceptUT, 10);
                    vessel.PlaceManeuverNode(orbit, dV, UT);

                    status = "Close to target: plotting intercept over " + closingTime.ToString("F0") + "s";
                }
            }
            else if (orbit.NextClosestApproachDistance(core.target.Orbit, vesselState.time) < core.target.Orbit.semiMajorAxis / 50)
            {
                //We're not close to the target, but we're on an approximate intercept course. 
                //Kill relative velocities at closest approach
                double UT = orbit.NextClosestApproachTime(core.target.Orbit, vesselState.time);
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.Orbit);
                vessel.PlaceManeuverNode(orbit, dV, UT);

                core.node.leadFraction = 0.5;

                status = "On intercept course. Planning to match velocities at closest approach.";
            }
            else if (orbit.RelativeInclination(core.target.Orbit) < 0.05 && orbit.eccentricity < 0.05)
            {
                //We're not on an intercept course, but we have a circular orbit in the right plane.
                //Plot a Hohmann transfer intercept.
                double UT;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, core.target.Orbit, vesselState.time, out UT);
                vessel.PlaceManeuverNode(orbit, dV, UT);

                core.node.leadFraction = 1;

                status = "Planning Hohmann transfer for intercept.";
            }
            else if (orbit.RelativeInclination(core.target.Orbit) < 0.05)
            {
                //We're not on an intercept course. We're in the right plane, but our orbit isn't circular. Circularize.

                bool circularizeAtPe;
                if (orbit.eccentricity > 1) circularizeAtPe = true;
                else circularizeAtPe = Math.Abs(orbit.PeR - core.target.Orbit.semiMajorAxis) < Math.Abs(orbit.ApR - core.target.Orbit.semiMajorAxis);

                double UT;
                if (circularizeAtPe) UT = Math.Max(vesselState.time, orbit.NextPeriapsisTime(vesselState.time));
                else UT = orbit.NextApoapsisTime(vesselState.time);

                Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);
                vessel.PlaceManeuverNode(orbit, dV, UT);

                core.node.leadFraction = 1;

                status = "Circularizing.";
            }
            else
            {
                //We're not on an intercept course, and we're not in the right plane. Match planes
                double UT;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, core.target.Orbit, vesselState.time, out UT);
                vessel.PlaceManeuverNode(orbit, dV, UT);

                core.node.leadFraction = 0.5;

                status = "Matching planes.";
            }
        }

    }
}
