using System;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleRendezvousAutopilot : ComputerModule
    {

        public MechJebModuleRendezvousAutopilot(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble desiredDistance = 100;
        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble maxPhasingOrbits = 5;
        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble maxClosingSpeed = 100;

        public string status = "";

        public override void OnModuleEnabled()
        {
            vessel.RemoveAllManeuverNodes();
            if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
        }

        public override void OnModuleDisabled()
        {
            core.node.Abort(); //make sure we turn off node executor if we get disabled suddenly
        }

        public override void Drive(FlightCtrlState s)
        {
            if (!core.target.NormalTargetExists)
            {
                users.Clear();
                return;
            }

            core.node.autowarp = core.node.autowarp && core.target.Distance > 1000;

            //If we get within the target distance and then next maneuver node is still
            //far in the future, delete it and we will create a new one to match velocities immediately.
            //This can often happen because the target vessel's orbit shifts slightly when it is unpacked.
            if (core.target.Distance < desiredDistance
                && vessel.patchedConicSolver.maneuverNodes.Count > 0
                && vessel.patchedConicSolver.maneuverNodes[0].UT > vesselState.time + 1)
            {
                vessel.RemoveAllManeuverNodes();
            }

            if (vessel.patchedConicSolver.maneuverNodes.Count > 0)
            {
                //If we have plotted a maneuver, execute it.
                if (!core.node.enabled) core.node.ExecuteAllNodes(this);
            }
            else if (core.target.Distance < desiredDistance * 1.05 + 2
                     && core.target.RelativeVelocity.magnitude < 1)
            {
                //finished
                users.Clear();
                core.thrust.ThrustOff();
                status = Localizer.Format("#MechJeb_RZauto_statu1");//"Successful rendezvous"
            }
            else if (core.target.Distance < desiredDistance * 1.05 + 2)
            {
                //We are within the target distance: match velocities
                double UT = vesselState.time;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.TargetOrbit);
                vessel.PlaceManeuverNode(orbit, dV, UT);
                status = Localizer.Format("#MechJeb_RZauto_statu2", desiredDistance.ToString());//"Within " +  + "m: matching velocities."
            }
            else if (core.target.Distance < vesselState.radius / 25)
            {
                if (orbit.NextClosestApproachDistance(core.target.TargetOrbit, vesselState.time) < desiredDistance
                    && orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time) < vesselState.time + 150)
                {
                    //We're close to the target, and on a course that will take us closer. Kill relvel at closest approach
                    double UT = orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time);
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.TargetOrbit);

                    //adjust burn time so as to come to rest at the desired distance from the target:
                    double approachDistance = orbit.Separation(core.target.TargetOrbit, UT);
                    double approachSpeed = (orbit.SwappedOrbitalVelocityAtUT(UT) - core.target.TargetOrbit.SwappedOrbitalVelocityAtUT(UT)).magnitude;
                    if (approachDistance < desiredDistance)
                    {
                        UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - approachDistance * approachDistance)) / approachSpeed;
                    }

                    //if coming in hot, stop early to avoid crashing:
                    if (approachSpeed > 10) UT -= 1;

                    vessel.PlaceManeuverNode(orbit, dV, UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu3");//"Planning to match velocities at closest approach."
                }
                else
                {
                    //We're not far from the target. Close the distance
                    double closingSpeed = core.target.Distance / 100;
                    if (closingSpeed > maxClosingSpeed) closingSpeed = maxClosingSpeed;
                    double closingTime = core.target.Distance / closingSpeed;

                    double UT = vesselState.time + 15;
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.target.TargetOrbit, closingTime);
                    vessel.PlaceManeuverNode(orbit, dV, UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu4");//"Close to target: plotting intercept"
                }
            }
            else if (orbit.NextClosestApproachDistance(core.target.TargetOrbit, vesselState.time) < core.target.TargetOrbit.semiMajorAxis / 25)
            {
                //We're not close to the target, but we're on an approximate intercept course.
                //Kill relative velocities at closest approach
                double UT = orbit.NextClosestApproachTime(core.target.TargetOrbit, vesselState.time);
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.target.TargetOrbit);

                //adjust burn time so as to come to rest at the desired distance from the target:
                double approachDistance = (orbit.SwappedAbsolutePositionAtUT(UT) - core.target.TargetOrbit.SwappedAbsolutePositionAtUT(UT)).magnitude;
                double approachSpeed = (orbit.SwappedOrbitalVelocityAtUT(UT) - core.target.TargetOrbit.SwappedOrbitalVelocityAtUT(UT)).magnitude;
                if (approachDistance < desiredDistance)
                {
                    UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - approachDistance * approachDistance)) / approachSpeed;
                }

                //if coming in hot, stop early to avoid crashing:
                if (approachSpeed > 10) UT -= 1;

                vessel.PlaceManeuverNode(orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu5");//"On intercept course. Planning to match velocities at closest approach."
            }
            else if (orbit.RelativeInclination(core.target.TargetOrbit) < 0.05 && orbit.eccentricity < 0.05)
            {
                //We're not on an intercept course, but we have a circular orbit in the right plane.

                double hohmannUT;
                Vector3d hohmannDV = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, core.target.TargetOrbit, vesselState.time, out hohmannUT);

                double numPhasingOrbits = (hohmannUT - vesselState.time) / orbit.period;

                double actualMaxPhasingOrbits = Math.Max(maxPhasingOrbits, 5); // ignore input values that are unreasonably small

                if (numPhasingOrbits < actualMaxPhasingOrbits)
                {
                    //It won't be too long until the intercept window. Plot a Hohmann transfer intercept.
                    vessel.PlaceManeuverNode(orbit, hohmannDV, hohmannUT);

                    status = Localizer.Format("#MechJeb_RZauto_statu6", numPhasingOrbits.ToString("F2"));//"Planning Hohmann transfer for intercept after " +  + " phasing orbits."
                }
                else
                {
                    //We are in a circular orbit in the right plane, but we aren't phasing quickly enough. Move to a better phasing orbit
                    double axisRatio = Math.Pow(1 + 1.25 / actualMaxPhasingOrbits, 2.0 / 3.0);
                    double lowPhasingRadius = core.target.TargetOrbit.semiMajorAxis / axisRatio;
                    double highPhasingRadius = core.target.TargetOrbit.semiMajorAxis * axisRatio;

                    bool useLowPhasingRadius = (lowPhasingRadius > mainBody.Radius + mainBody.RealMaxAtmosphereAltitude() + 3000) && (orbit.semiMajorAxis < core.target.TargetOrbit.semiMajorAxis);
                    double phasingOrbitRadius = (useLowPhasingRadius ? lowPhasingRadius : highPhasingRadius);

                    if (orbit.ApR < phasingOrbitRadius)
                    {
                        double UT1 = vesselState.time + 15;
                        Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(orbit, UT1, phasingOrbitRadius);
                        vessel.PlaceManeuverNode(orbit, dV1, UT1);
                        Orbit transferOrbit = vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                        double UT2 = transferOrbit.NextApoapsisTime(UT1);
                        Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                        vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                    }
                    else if (orbit.PeR > phasingOrbitRadius)
                    {
                        double UT1 = vesselState.time + 15;
                        Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, UT1, phasingOrbitRadius);
                        vessel.PlaceManeuverNode(orbit, dV1, UT1);
                        Orbit transferOrbit = vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                        double UT2 = transferOrbit.NextPeriapsisTime(UT1);
                        Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                        vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                    }
                    else
                    {
                        double UT = orbit.NextTimeOfRadius(vesselState.time, phasingOrbitRadius);
                        Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);
                        vessel.PlaceManeuverNode(orbit, dV, UT);
                    }

                    status = Localizer.Format("#MechJeb_RZauto_statu7", numPhasingOrbits.ToString("F1"),maxPhasingOrbits.text,MuUtils.ToSI(phasingOrbitRadius - mainBody.Radius, 0));//"Next intercept window would be <<1>> orbits away, which is more than the maximum of <<2>> phasing orbits. Increasing phasing rate by establishing new phasing orbit at <<3>>m
                }
            }
            else if (orbit.RelativeInclination(core.target.TargetOrbit) < 0.05)
            {
                //We're not on an intercept course. We're in the right plane, but our orbit isn't circular. Circularize.

                bool circularizeAtPe;
                if (orbit.eccentricity > 1) circularizeAtPe = true;
                else circularizeAtPe = Math.Abs(orbit.PeR - core.target.TargetOrbit.semiMajorAxis) < Math.Abs(orbit.ApR - core.target.TargetOrbit.semiMajorAxis);

                double UT;
                if (circularizeAtPe) UT = Math.Max(vesselState.time, orbit.NextPeriapsisTime(vesselState.time));
                else UT = orbit.NextApoapsisTime(vesselState.time);

                Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);
                vessel.PlaceManeuverNode(orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu8");//"Circularizing."
            }
            else
            {
                //We're not on an intercept course, and we're not in the right plane. Match planes
                bool ascending;
                if (orbit.eccentricity < 1)
                {
                    if (orbit.TimeOfAscendingNode(core.target.TargetOrbit, vesselState.time) < orbit.TimeOfDescendingNode(core.target.TargetOrbit, vesselState.time))
                    {
                        ascending = true;
                    }
                    else
                    {
                        ascending = false;
                    }
                }
                else
                {
                    if (orbit.AscendingNodeExists(core.target.TargetOrbit))
                    {
                        ascending = true;
                    }
                    else
                    {
                        ascending = false;
                    }
                }

                double UT;
                Vector3d dV;
                if (ascending) dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, core.target.TargetOrbit, vesselState.time, out UT);
                else dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, core.target.TargetOrbit, vesselState.time, out UT);

                vessel.PlaceManeuverNode(orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu9");//"Matching planes."
            }
        }
    }
}
