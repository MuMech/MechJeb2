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
            if (!Target.Exists())
            {
                this.enabled = false;
                return;
            }

            if (part.vessel.patchedConicSolver.maneuverNodes.Any())
            {
                //execute maneuver node
            }
            else if (Target.Distance(part.vessel) < 100 && Target.RelativeVelocity(part.vessel).magnitude < 1)
            {
                //finished
                this.enabled = false;
                status = "Successful rendezvous";
            }
            else if (Target.Distance(part.vessel) < 100)
            {
                double UT = vesselState.time;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(part.vessel.orbit, UT, Target.Orbit());
                PlaceManeuverNode(part.vessel.orbit, dV, UT);
                status = "Within 100m: matching velocities.";
            }
            else if (Target.Distance(part.vessel) < vesselState.radius / 100)
            {
                //We're not far from the target. Close the distance and then kill relative velocity
                double closingSpeed = Target.Distance(part.vessel) / 100;
                if (closingSpeed > 100) closingSpeed = 100;
                double closingTime = Target.Distance(part.vessel) / closingSpeed;

                //Burn to intercept the target
                double UT = vesselState.time + 15;
                double interceptUT = UT + closingTime;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(part.vessel.orbit, UT, Target.Orbit(), interceptUT);
                PlaceManeuverNode(part.vessel.orbit, dV, UT);

                //Then kill relative velocities at interceptUT
                Orbit closingOrbit = part.vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToMatchVelocities(closingOrbit, interceptUT, Target.Orbit());
                PlaceManeuverNode(closingOrbit, dV2, interceptUT);

                status = "Within " + (vesselState.radius / 100).ToString("F0") + "m: plotting intercept over " + closingTime.ToString("F0") + "s";
            }
            else if (part.vessel.orbit.NextClosestApproachDistance(Target.Orbit(), vesselState.time) < Target.Orbit().semiMajorAxis / 100)
            {
                //We're not close to the target, but we're on an approximate intercept course. 
                //Kill relative velocities at closest approach
                double UT = part.vessel.orbit.NextClosestApproachTime(Target.Orbit(), vesselState.time);
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(part.vessel.orbit, UT, Target.Orbit());
                PlaceManeuverNode(part.vessel.orbit, dV, UT);

                status = "Closest approach distance is < " + (Target.Orbit().semiMajorAxis/100).ToString("F0") + "m. Planning to kill relvel at closest approach.";
            }
            else if (part.vessel.orbit.RelativeInclination(Target.Orbit()) < 0.05 && part.vessel.orbit.eccentricity < 0.05)
            {
                //We're not on an intercept course, but we have a circular orbit in the right plane.
                //Plot a Hohmann transfer intercept.
                double UT;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(part.vessel.orbit, Target.Orbit(), vesselState.time, out UT);
                PlaceManeuverNode(part.vessel.orbit, dV, UT);

                status = "Planning Hohmann transfer for intercept.";
            }
            else if (part.vessel.orbit.RelativeInclination(Target.Orbit()) < 0.05)
            {
                //We're not on an intercept course. We're in the right plane, but our orbit isn't circular. Circularize.
                double UT = vesselState.time + 15;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(part.vessel.orbit, UT);
                PlaceManeuverNode(part.vessel.orbit, dV, UT);

                status = "Circularizing.";
            }
            else
            {
                //We're not on an intercept course, and we're not in the right plane. Match planes
                double UT;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(part.vessel.orbit, Target.Orbit(), vesselState.time, out UT);
                PlaceManeuverNode(part.vessel.orbit, dV, UT);

                status = "Matching planes.";
            }
        }


        //input dV should be in world coordinates
        public void PlaceManeuverNode(Orbit patch, Vector3d dV, double UT)
        {
            //convert a dV in world coordinates into the coordinate system of the maneuver node,
            //which uses (x, y, z) = (radial+, normal-, prograde)
            Vector3d nodeDV = new Vector3d(Vector3d.Dot(patch.RadialPlus(UT), dV),
                                           Vector3d.Dot(-patch.NormalPlus(UT), dV),
                                           Vector3d.Dot(patch.Prograde(UT), dV));
            ManeuverNode mn = part.vessel.patchedConicSolver.AddManeuverNode(UT);
            mn.OnGizmoUpdated(nodeDV, UT);
        }

    }
}
