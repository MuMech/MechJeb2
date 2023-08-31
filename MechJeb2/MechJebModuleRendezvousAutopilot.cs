using System;
using JetBrains.Annotations;
using KSP.Localization;
using static MechJebLib.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleRendezvousAutopilot : ComputerModule
    {
        public MechJebModuleRendezvousAutopilot(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble desiredDistance = 100;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble maxPhasingOrbits = 5;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble maxClosingSpeed = 100;

        public string status = "";

        protected override void OnModuleEnabled()
        {
            Vessel.RemoveAllManeuverNodes();
            if (!MuUtils.PhysicsRunning()) Core.Warp.MinimumWarp();
        }

        protected override void OnModuleDisabled()
        {
            Core.Node.Abort(); //make sure we turn off node executor if we get disabled suddenly
        }

        public override void Drive(FlightCtrlState s)
        {
            if (!Core.Target.NormalTargetExists)
            {
                Users.Clear();
                return;
            }

            Core.Node.autowarp = Core.Node.autowarp && Core.Target.Distance > 1000;

            //If we get within the target distance and then next maneuver node is still
            //far in the future, delete it and we will create a new one to match velocities immediately.
            //This can often happen because the target vessel's orbit shifts slightly when it is unpacked.
            if (Core.Target.Distance < desiredDistance
                && Vessel.patchedConicSolver.maneuverNodes.Count > 0
                && Vessel.patchedConicSolver.maneuverNodes[0].UT > VesselState.time + 1)
            {
                Vessel.RemoveAllManeuverNodes();
            }

            if (Vessel.patchedConicSolver.maneuverNodes.Count > 0)
            {
                //If we have plotted a maneuver, execute it.
                if (!Core.Node.Enabled) Core.Node.ExecuteAllNodes(this);
            }
            else if (Core.Target.Distance < desiredDistance * 1.05 + 2
                     && Core.Target.RelativeVelocity.magnitude < 1)
            {
                //finished
                Users.Clear();
                Core.Thrust.ThrustOff();
                status = Localizer.Format("#MechJeb_RZauto_statu1"); //"Successful rendezvous"
            }
            else if (Core.Target.Distance < desiredDistance * 1.05 + 2)
            {
                //We are within the target distance: match velocities
                double UT = VesselState.time;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, UT, Core.Target.TargetOrbit);
                Vessel.PlaceManeuverNode(Orbit, dV, UT);
                status = Localizer.Format("#MechJeb_RZauto_statu2", desiredDistance.ToString()); //"Within " +  + "m: matching velocities."
            }
            else if (Core.Target.Distance < VesselState.radius / 25)
            {
                if (Orbit.NextClosestApproachDistance(Core.Target.TargetOrbit, VesselState.time) < desiredDistance
                    && Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, VesselState.time) < VesselState.time + 150)
                {
                    //We're close to the target, and on a course that will take us closer. Kill relvel at closest approach
                    double UT = Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, VesselState.time);
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, UT, Core.Target.TargetOrbit);

                    //adjust burn time so as to come to rest at the desired distance from the target:
                    double approachDistance = Orbit.Separation(Core.Target.TargetOrbit, UT);
                    double approachSpeed = (Orbit.WorldOrbitalVelocityAtUT(UT) - Core.Target.TargetOrbit.WorldOrbitalVelocityAtUT(UT)).magnitude;
                    if (approachDistance < desiredDistance)
                    {
                        UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - approachDistance * approachDistance)) / approachSpeed;
                    }

                    //if coming in hot, stop early to avoid crashing:
                    if (approachSpeed > 10) UT -= 1;

                    Vessel.PlaceManeuverNode(Orbit, dV, UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu3"); //"Planning to match velocities at closest approach."
                }
                else
                {
                    //We're not far from the target. Close the distance
                    double closingSpeed = Core.Target.Distance / 100;
                    if (closingSpeed > maxClosingSpeed) closingSpeed = maxClosingSpeed;
                    closingSpeed = Math.Max(0.01, closingSpeed);
                    double closingTime = Core.Target.Distance / closingSpeed;
                    double UT = VesselState.time + 15;

                    (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(Orbit, UT, Core.Target.TargetOrbit, closingTime);
                    Vessel.PlaceManeuverNode(Orbit, dV, UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu4"); //"Close to target: plotting intercept"
                }
            }
            else if (Orbit.NextClosestApproachDistance(Core.Target.TargetOrbit, VesselState.time) < Core.Target.TargetOrbit.semiMajorAxis / 25)
            {
                //We're not close to the target, but we're on an approximate intercept course.
                //Kill relative velocities at closest approach
                double UT = Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, VesselState.time);
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, UT, Core.Target.TargetOrbit);

                //adjust burn time so as to come to rest at the desired distance from the target:
                double approachDistance = (Orbit.WorldPositionAtUT(UT) - Core.Target.TargetOrbit.WorldPositionAtUT(UT)).magnitude;
                double approachSpeed = (Orbit.WorldOrbitalVelocityAtUT(UT) - Core.Target.TargetOrbit.WorldOrbitalVelocityAtUT(UT)).magnitude;
                if (approachDistance < desiredDistance)
                {
                    UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - approachDistance * approachDistance)) / approachSpeed;
                }

                //if coming in hot, stop early to avoid crashing:
                if (approachSpeed > 10) UT -= 1;

                Vessel.PlaceManeuverNode(Orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu5"); //"On intercept course. Planning to match velocities at closest approach."
            }
            else if (Orbit.RelativeInclination(Core.Target.TargetOrbit) < 0.05 && Orbit.eccentricity < 0.05)
            {
                //We're not on an intercept course, but we have a circular orbit in the right plane.

                double hohmannUT;
                Vector3d hohmannDV =
                    OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(Orbit, Core.Target.TargetOrbit, VesselState.time, out hohmannUT);

                double numPhasingOrbits = (hohmannUT - VesselState.time) / Orbit.period;

                double actualMaxPhasingOrbits = Math.Max(maxPhasingOrbits, 5); // ignore input values that are unreasonably small

                if (numPhasingOrbits < actualMaxPhasingOrbits)
                {
                    //It won't be too long until the intercept window. Plot a Hohmann transfer intercept.
                    Vessel.PlaceManeuverNode(Orbit, hohmannDV, hohmannUT);

                    status = Localizer.Format("#MechJeb_RZauto_statu6",
                        numPhasingOrbits.ToString("F2")); //"Planning Hohmann transfer for intercept after " +  + " phasing orbits."
                }
                else
                {
                    //We are in a circular orbit in the right plane, but we aren't phasing quickly enough. Move to a better phasing orbit
                    double axisRatio = Math.Pow(1 + 1.25 / actualMaxPhasingOrbits, 2.0 / 3.0);
                    double lowPhasingRadius = Core.Target.TargetOrbit.semiMajorAxis / axisRatio;
                    double highPhasingRadius = Core.Target.TargetOrbit.semiMajorAxis * axisRatio;

                    bool useLowPhasingRadius = lowPhasingRadius > MainBody.Radius + MainBody.RealMaxAtmosphereAltitude() + 3000 &&
                                               Orbit.semiMajorAxis < Core.Target.TargetOrbit.semiMajorAxis;
                    double phasingOrbitRadius = useLowPhasingRadius ? lowPhasingRadius : highPhasingRadius;

                    if (Orbit.ApR < phasingOrbitRadius)
                    {
                        double UT1 = VesselState.time + 15;
                        Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(Orbit, UT1, phasingOrbitRadius);
                        Vessel.PlaceManeuverNode(Orbit, dV1, UT1);
                        Orbit transferOrbit = Vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                        double UT2 = transferOrbit.NextApoapsisTime(UT1);
                        Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                        Vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                    }
                    else if (Orbit.PeR > phasingOrbitRadius)
                    {
                        double UT1 = VesselState.time + 15;
                        Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(Orbit, UT1, phasingOrbitRadius);
                        Vessel.PlaceManeuverNode(Orbit, dV1, UT1);
                        Orbit transferOrbit = Vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                        double UT2 = transferOrbit.NextPeriapsisTime(UT1);
                        Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                        Vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                    }
                    else
                    {
                        double UT = Orbit.NextTimeOfRadius(VesselState.time, phasingOrbitRadius);
                        Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(Orbit, UT);
                        Vessel.PlaceManeuverNode(Orbit, dV, UT);
                    }

                    status = Localizer.Format("#MechJeb_RZauto_statu7", numPhasingOrbits.ToString("F1"), maxPhasingOrbits.text,
                        (phasingOrbitRadius - MainBody.Radius)
                        .ToSI(0)); //"Next intercept window would be <<1>> orbits away, which is more than the maximum of <<2>> phasing orbits. Increasing phasing rate by establishing new phasing orbit at <<3>>m
                }
            }
            else if (Orbit.RelativeInclination(Core.Target.TargetOrbit) < 0.05)
            {
                //We're not on an intercept course. We're in the right plane, but our orbit isn't circular. Circularize.

                bool circularizeAtPe;
                if (Orbit.eccentricity > 1) circularizeAtPe = true;
                else
                    circularizeAtPe = Math.Abs(Orbit.PeR - Core.Target.TargetOrbit.semiMajorAxis) <
                                      Math.Abs(Orbit.ApR - Core.Target.TargetOrbit.semiMajorAxis);

                double UT;
                if (circularizeAtPe) UT = Math.Max(VesselState.time, Orbit.NextPeriapsisTime(VesselState.time));
                else UT                 = Orbit.NextApoapsisTime(VesselState.time);

                Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(Orbit, UT);
                Vessel.PlaceManeuverNode(Orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu8"); //"Circularizing."
            }
            else
            {
                //We're not on an intercept course, and we're not in the right plane. Match planes
                bool ascending;
                if (Orbit.eccentricity < 1)
                {
                    if (Orbit.TimeOfAscendingNode(Core.Target.TargetOrbit, VesselState.time) <
                        Orbit.TimeOfDescendingNode(Core.Target.TargetOrbit, VesselState.time))
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
                    if (Orbit.AscendingNodeExists(Core.Target.TargetOrbit))
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
                    dV  = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(Orbit, Core.Target.TargetOrbit, VesselState.time, out UT);
                else dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(Orbit, Core.Target.TargetOrbit, VesselState.time, out UT);

                Vessel.PlaceManeuverNode(Orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu9"); //"Matching planes."
            }
        }
    }
}
