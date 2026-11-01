extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleRendezvousAutopilot : ComputerModule
    {
        public MechJebModuleRendezvousAutopilot(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble desiredDistance = 100;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble maxPhasingOrbits = 5;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble maxClosingSpeed = 100;

        public string status = "";

        //The physics ticks in a row that planning may fail before we give up: a failure can pass as the orbits move on.
        private const int MAX_FAILED_TICKS = 50;
        private int _failedTicks;

        protected override void OnModuleEnabled()
        {
            status = "";
            _failedTicks = 0;
            if (Vessel.patchedConicsUnlocked() && Vessel.patchedConicSolver.maneuverNodes.Count > 0)
                ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_RZauto_nodesRemoved"), 5);
            Vessel.RemoveAllManeuverNodes();
            if (!MuUtils.PhysicsRunning()) Core.Warp.MinimumWarp();
        }

        protected override void OnModuleDisabled()
        {
            Core.Node.AutowarpInhibited = false;
            Core.Node.Abort(); //make sure we turn off node executor if we get disabled suddenly
        }

        //Why the autopilot can't run for this target and vessel, or null if it can.
        private string Problem()
        {
            if (!Core.Target.NormalTargetExists) return Localizer.Format("#MechJeb_RZauto_label1"); //"Select a target to rendezvous with."
            if (Core.Target.Target is CelestialBody) return Localizer.Format("#MechJeb_RZauto_noBody"); //"The autopilot can't rendezvous with a body."
            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody) return Localizer.Format("#MechJeb_RZauto_label2"); //"Rendezvous target must be in the same sphere of influence."

            Vessel target = Core.Target.Target.GetVessel();
            if (target != null && target.LandedOrSplashed) return Localizer.Format("#MechJeb_RZauto_landedTarget"); //"The autopilot needs a target in orbit."
            if (Vessel.LandedOrSplashed || Vessel.situation == Vessel.Situations.PRELAUNCH) return Localizer.Format("#MechJeb_RZauto_landedVessel"); //"The autopilot can't run on the ground."

            return null;
        }

        //The window may be closed, so the status alone isn't enough.
        private void Stop(string reason)
        {
            status = reason;
            ScreenMessages.PostScreenMessage(reason, 5);
            Users.Clear();
        }

        //This one also runs during time warp, where Drive doesn't.
        public override void OnFixedUpdate()
        {
            string problem = Problem();
            if (problem != null) Stop(problem);
        }

        //The time to close a distance at the closing speed limit. A zero-revolution intercept of about an orbit or more has only
        //a huge, near radial solution, so this stops at a quarter of an orbit, and is faster than the limit when it does.
        public double ClosingTime(double distance)
        {
            double closingSpeed = Math.Max(0.01, Math.Min(distance / 100, maxClosingSpeed));
            return Math.Min(distance / closingSpeed, Orbit.period / 4);
        }

        public override void Drive(FlightCtrlState s)
        {
            string problem = Problem();
            if (problem != null)
            {
                Stop(problem);
                return;
            }

            try
            {
                Rendezvous();
                _failedTicks = 0;
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                //a plan that failed half way can leave a node that makes no sense alone
                if (!Core.Node.Enabled) Vessel.RemoveAllManeuverNodes();

                if (++_failedTicks >= MAX_FAILED_TICKS) Stop(Localizer.Format("#MechJeb_RZauto_failed", e.Message)); //"Stopped, the next burn can't be planned: <<1>>"
            }
        }

        private void Rendezvous()
        {
            //The burns close to the target are better watched in real time
            Core.Node.AutowarpInhibited = Core.Target.Distance <= 1000;

            //If we get within the target distance and then next maneuver node is still
            //far in the future, delete it and we will create a new one to match velocities immediately.
            //This can often happen because the target vessel's orbit shifts slightly when it is unpacked.
            if (Core.Target.Distance < desiredDistance
                && Vessel.patchedConicSolver.maneuverNodes.Count > 0
                && Vessel.patchedConicSolver.maneuverNodes[0].UT > VesselState.Time + 1)
            {
                Vessel.RemoveAllManeuverNodes();
                Core.Node.Abort(); //it would carry on with the deleted node's direction
            }

            if (Vessel.patchedConicSolver.maneuverNodes.Count > 0)
            {
                //If we have plotted a maneuver, execute it.
                if (!Core.Node.Enabled) Core.Node.ExecuteAllNodes(this);
                return;
            }

            double closestApproachUT = Orbit.NextClosestApproachTime(Core.Target.TargetOrbit, VesselState.Time);
            double closestApproachDistance = Orbit.Separation(Core.Target.TargetOrbit, closestApproachUT);

            if (Core.Target.Distance < desiredDistance * 1.05 + 2
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
                double UT = VesselState.Time;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, UT, Core.Target.TargetOrbit);
                Vessel.PlaceManeuverNode(Orbit, dV, UT);
                status = Localizer.Format("#MechJeb_RZauto_statu2", desiredDistance.ToString()); //"Within " +  + "m: matching velocities."
            }
            else if (Core.Target.Distance < VesselState.Radius / 25)
            {
                double closingTime = ClosingTime(Core.Target.Distance);

                //The margin keeps the intercept we plotted from looking too slow, so that it doesn't get plotted again.
                if (closestApproachDistance < desiredDistance
                    && closestApproachUT < VesselState.Time + Math.Max(150, closingTime * 1.25))
                {
                    //We're close to the target, and on a course that will take us closer. Kill relvel at closest approach
                    double UT = closestApproachUT;
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, UT, Core.Target.TargetOrbit);

                    //adjust burn time so as to come to rest at the desired distance from the target:
                    double approachSpeed = (Orbit.WorldOrbitalVelocityAtUT(UT) - Core.Target.TargetOrbit.WorldOrbitalVelocityAtUT(UT)).magnitude;
                    if (closestApproachDistance < desiredDistance)
                    {
                        UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - closestApproachDistance * closestApproachDistance)) / approachSpeed;
                    }

                    //if coming in hot, stop early to avoid crashing:
                    if (approachSpeed > 10) UT -= 1;

                    Vessel.PlaceManeuverNode(Orbit, dV, UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu3"); //"Planning to match velocities at closest approach."
                }
                else
                {
                    //We're not far from the target. Close the distance
                    double UT = VesselState.Time + 15;

                    (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(Orbit, UT, Core.Target.TargetOrbit, closingTime);
                    Vessel.PlaceManeuverNode(Orbit, dV, UT);

                    //The time limit in ClosingTime can leave us above the closing speed limit: say so, or the setting looks ignored.
                    status = Core.Target.Distance / closingTime > maxClosingSpeed * 1.001
                        ? Localizer.Format("#MechJeb_RZauto_statu10") //"Close to target: plotting intercept above the max closing velocity, which would take too long"
                        : Localizer.Format("#MechJeb_RZauto_statu4"); //"Close to target: plotting intercept"
                }
            }
            else if (closestApproachDistance < Core.Target.TargetOrbit.semiMajorAxis / 25)
            {
                //We're not close to the target, but we're on an approximate intercept course.
                //Kill relative velocities at closest approach
                double UT = closestApproachUT;
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, UT, Core.Target.TargetOrbit);

                //adjust burn time so as to come to rest at the desired distance from the target:
                double approachSpeed = (Orbit.WorldOrbitalVelocityAtUT(UT) - Core.Target.TargetOrbit.WorldOrbitalVelocityAtUT(UT)).magnitude;
                if (closestApproachDistance < desiredDistance)
                {
                    UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - closestApproachDistance * closestApproachDistance)) / approachSpeed;
                }

                //if coming in hot, stop early to avoid crashing:
                if (approachSpeed > 10) UT -= 1;

                Vessel.PlaceManeuverNode(Orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu5"); //"On intercept course. Planning to match velocities at closest approach."
            }
            else if (Orbit.RelativeInclination(Core.Target.TargetOrbit) < 0.05 && Orbit.eccentricity < 0.05)
            {
                //We're not on an intercept course, but we have a circular orbit in the right plane.

                (Vector3d hohmannDV, double hohmannUT, _, _) =
                    OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(Orbit, Core.Target.TargetOrbit, VesselState.Time, coplanar: false);

                double numPhasingOrbits = (hohmannUT - VesselState.Time) / Orbit.period;

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
                        double UT1 = VesselState.Time + 15;
                        Vector3d dV1 = OrbitalManeuverCalculator.DeltaVToChangeApoapsis(Orbit, UT1, phasingOrbitRadius);
                        Vessel.PlaceManeuverNode(Orbit, dV1, UT1);
                        Orbit transferOrbit = Vessel.patchedConicSolver.maneuverNodes[0].nextPatch;
                        double UT2 = transferOrbit.NextApoapsisTime(UT1);
                        Vector3d dV2 = OrbitalManeuverCalculator.DeltaVToCircularize(transferOrbit, UT2);
                        Vessel.PlaceManeuverNode(transferOrbit, dV2, UT2);
                    }
                    else if (Orbit.PeR > phasingOrbitRadius)
                    {
                        double UT1 = VesselState.Time + 15;
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

                    status = Localizer.Format("#MechJeb_RZauto_statu7", numPhasingOrbits.ToString("F1"), maxPhasingOrbits.Text,
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
                if (circularizeAtPe) UT = Math.Max(VesselState.Time, Orbit.NextPeriapsisTime(VesselState.Time));
                else UT = Orbit.NextApoapsisTime(VesselState.Time);

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
                    if (Orbit.TimeOfAscendingNode(Core.Target.TargetOrbit, VesselState.Time) <
                        Orbit.TimeOfDescendingNode(Core.Target.TargetOrbit, VesselState.Time))
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
                    dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesAscending(Orbit, Core.Target.TargetOrbit, VesselState.Time, out UT);
                else dV = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanesDescending(Orbit, Core.Target.TargetOrbit, VesselState.Time, out UT);

                Vessel.PlaceManeuverNode(Orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu9"); //"Matching planes."
            }
        }
    }
}
