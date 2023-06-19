using System;
using JetBrains.Annotations;
using KSP.Localization;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    [UsedImplicitly]
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
            if (!MuUtils.PhysicsRunning()) core.Warp.MinimumWarp();
        }

        public override void OnModuleDisabled()
        {
            core.Node.Abort(); //make sure we turn off node executor if we get disabled suddenly
        }

        public override void Drive(FlightCtrlState s)
        {
            if (!core.Target.NormalTargetExists)
            {
                users.Clear();
                return;
            }

            core.Node.autowarp = core.Node.autowarp && core.Target.Distance > 1000;

            //If we get within the target distance and then next maneuver node is still
            //far in the future, delete it and we will create a new one to match velocities immediately.
            //This can often happen because the target vessel's orbit shifts slightly when it is unpacked.
            if (core.Target.Distance < desiredDistance
                && vessel.patchedConicSolver.maneuverNodes.Count > 0
                && vessel.patchedConicSolver.maneuverNodes[0].UT > vesselState.time + 1)
            {
                vessel.RemoveAllManeuverNodes();
            }

            if (vessel.patchedConicSolver.maneuverNodes.Count > 0)
            {
                //If we have plotted a maneuver, execute it.
                if (!core.Node.enabled) core.Node.ExecuteAllNodes(this);
            }
            else if (core.Target.Distance < desiredDistance * 1.05 + 2
                     && core.Target.RelativeVelocity.magnitude < 1)
            {
                //finished
                users.Clear();
                core.Thrust.ThrustOff();
                status = Localizer.Format("#MechJeb_RZauto_statu1"); //"Successful rendezvous"
            }
            else if (core.Target.Distance < desiredDistance * 1.05 + 2)
            {
                //We are within the target distance: match velocities
                double UT = vesselState.time;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.Target.TargetOrbit);
                vessel.PlaceManeuverNode(orbit, dV, UT);
                status = Localizer.Format("#MechJeb_RZauto_statu2", desiredDistance.ToString()); //"Within " +  + "m: matching velocities."
            }
            else if (core.Target.Distance < vesselState.radius / 25)
            {
                if (orbit.NextClosestApproachDistance(core.Target.TargetOrbit, vesselState.time) < desiredDistance
                    && orbit.NextClosestApproachTime(core.Target.TargetOrbit, vesselState.time) < vesselState.time + 150)
                {
                    //We're close to the target, and on a course that will take us closer. Kill relvel at closest approach
                    double UT = orbit.NextClosestApproachTime(core.Target.TargetOrbit, vesselState.time);
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.Target.TargetOrbit);

                    //adjust burn time so as to come to rest at the desired distance from the target:
                    double approachDistance = orbit.Separation(core.Target.TargetOrbit, UT);
                    double approachSpeed = (orbit.WorldOrbitalVelocityAtUT(UT) - core.Target.TargetOrbit.WorldOrbitalVelocityAtUT(UT)).magnitude;
                    if (approachDistance < desiredDistance)
                    {
                        UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - approachDistance * approachDistance)) / approachSpeed;
                    }

                    //if coming in hot, stop early to avoid crashing:
                    if (approachSpeed > 10) UT -= 1;

                    vessel.PlaceManeuverNode(orbit, dV, UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu3"); //"Planning to match velocities at closest approach."
                }
                else
                {
                    //We're not far from the target. Close the distance
                    double closingSpeed = core.Target.Distance / 100;
                    if (closingSpeed > maxClosingSpeed) closingSpeed = maxClosingSpeed;
                    closingSpeed = Math.Max(0.01, closingSpeed);
                    double closingTime = core.Target.Distance / closingSpeed;
                    double UT = vesselState.time + 15;

                    (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(orbit, UT, core.Target.TargetOrbit, closingTime);
                    vessel.PlaceManeuverNode(orbit, dV, UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu4"); //"Close to target: plotting intercept"
                }
            }
            else if (orbit.NextClosestApproachDistance(core.Target.TargetOrbit, vesselState.time) < core.Target.TargetOrbit.semiMajorAxis / 25)
            {
                //We're not close to the target, but we're on an approximate intercept course.
                //Kill relative velocities at closest approach
                double UT = orbit.NextClosestApproachTime(core.Target.TargetOrbit, vesselState.time);
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(orbit, UT, core.Target.TargetOrbit);

                //adjust burn time so as to come to rest at the desired distance from the target:
                double approachDistance = (orbit.WorldPositionAtUT(UT) - core.Target.TargetOrbit.WorldPositionAtUT(UT)).magnitude;
                double approachSpeed = (orbit.WorldOrbitalVelocityAtUT(UT) - core.Target.TargetOrbit.WorldOrbitalVelocityAtUT(UT)).magnitude;
                if (approachDistance < desiredDistance)
                {
                    UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - approachDistance * approachDistance)) / approachSpeed;
                }

                //if coming in hot, stop early to avoid crashing:
                if (approachSpeed > 10) UT -= 1;

                vessel.PlaceManeuverNode(orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu5"); //"On intercept course. Planning to match velocities at closest approach."
            }
            else if (orbit.RelativeInclination(core.Target.TargetOrbit) < 0.05 && orbit.eccentricity < 0.05)
            {
                //We're not on an intercept course, but we have a circular orbit in the right plane.

                double hohmannUT;
                Vector3d hohmannDV =
                    OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(orbit, core.Target.TargetOrbit, vesselState.time, out hohmannUT);

                double numPhasingOrbits = (hohmannUT - vesselState.time) / orbit.period;

                double actualMaxPhasingOrbits = Math.Max(maxPhasingOrbits, 5); // ignore input values that are unreasonably small

                if (numPhasingOrbits < actualMaxPhasingOrbits)
                {
                    //It won't be too long until the intercept window. Plot a Hohmann transfer intercept.
                    vessel.PlaceManeuverNode(orbit, hohmannDV, hohmannUT);

                    status = Localizer.Format("#MechJeb_RZauto_statu6",
                        numPhasingOrbits.ToString("F2")); //"Planning Hohmann transfer for intercept after " +  + " phasing orbits."
                }
                else
                {
                    //We are in a circular orbit in the right plane, but we aren't phasing quickly enough. Move to a better phasing orbit
                    double axisRatio = Math.Pow(1 + 1.25 / actualMaxPhasingOrbits, 2.0 / 3.0);
                    double lowPhasingRadius = core.Target.TargetOrbit.semiMajorAxis / axisRatio;
                    double highPhasingRadius = core.Target.TargetOrbit.semiMajorAxis * axisRatio;

                    bool useLowPhasingRadius = lowPhasingRadius > mainBody.Radius + mainBody.RealMaxAtmosphereAltitude() + 3000 &&
                                               orbit.semiMajorAxis < core.Target.TargetOrbit.semiMajorAxis;
                    double phasingOrbitRadius = useLowPhasingRadius ? lowPhasingRadius : highPhasingRadius;

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

                    status = Localizer.Format("#MechJeb_RZauto_statu7", numPhasingOrbits.ToString("F1"), maxPhasingOrbits.text,
                        (phasingOrbitRadius - mainBody.Radius)
                        .ToSI(0)); //"Next intercept window would be <<1>> orbits away, which is more than the maximum of <<2>> phasing orbits. Increasing phasing rate by establishing new phasing orbit at <<3>>m
                }
            }
            else if (orbit.RelativeInclination(core.Target.TargetOrbit) < 0.05)
            {
                //We're not on an intercept course. We're in the right plane, but our orbit isn't circular. Circularize.

                bool circularizeAtPe;
                if (orbit.eccentricity > 1) circularizeAtPe = true;
                else
                    circularizeAtPe = Math.Abs(orbit.PeR - core.Target.TargetOrbit.semiMajorAxis) <
                                      Math.Abs(orbit.ApR - core.Target.TargetOrbit.semiMajorAxis);

                double UT;
                if (circularizeAtPe) UT = Math.Max(vesselState.time, orbit.NextPeriapsisTime(vesselState.time));
                else UT                 = orbit.NextApoapsisTime(vesselState.time);

                Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);
                vessel.PlaceManeuverNode(orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu8"); //"Circularizing."
            }
            else
            {
                //We're not on an intercept course, and we're not in the right plane. Match planes
                bool ascending;
                if (orbit.eccentricity < 1)
                {
                    if (orbit.TimeOfAscendingNode(core.Target.TargetOrbit, vesselState.time) <
                        orbit.TimeOfDescendingNode(core.Target.TargetOrbit, vesselState.time))
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
                    if (orbit.AscendingNodeExists(core.Target.TargetOrbit))
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
                if (ascending)
                    dV  = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(orbit, core.Target.TargetOrbit, vesselState.time, out UT);
                else dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(orbit, core.Target.TargetOrbit, vesselState.time, out UT);

                vessel.PlaceManeuverNode(orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu9"); //"Matching planes."
            }
        }
    }
}
