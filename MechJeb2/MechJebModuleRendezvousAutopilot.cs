extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using KSP.Localization;
using MechJebLib.Functions;
using MechJebLib.Primitives;
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

        //The earliest time for the middle of a burn of this size: the node executor centers a burn on its node, and only starts it
        //once the vessel points the right way, so anything sooner starts late.
        public double EarliestBurnUT(double dV) => VesselState.Time + Math.Max(15, Core.Node.FlipTime() + Core.Node.BurnLeadTime(dV));

        //When to center the burn that kills the relative velocity at a closest approach, to come to rest at the desired distance from the target.
        public double BurnUTToStopAtDesiredDistance(double closestApproachUT)
        {
            double UT = closestApproachUT;
            double approachDistance = Orbit.Separation(Core.Target.TargetOrbit, UT);
            double approachSpeed = (Orbit.WorldOrbitalVelocityAtUT(UT) - Core.Target.TargetOrbit.WorldOrbitalVelocityAtUT(UT)).magnitude;
            if (approachDistance < desiredDistance)
            {
                UT -= Math.Sqrt(Math.Abs(desiredDistance * desiredDistance - approachDistance * approachDistance)) / approachSpeed;
            }

            //if coming in hot, stop early to avoid crashing:
            if (approachSpeed > 10) UT -= 1;

            return UT;
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

        //The time to close a distance at the closing speed limit, and at least long enough to turn around and stop at the end. A
        //zero-revolution intercept of about an orbit or more has only a huge, near radial solution, so this stops at a quarter of
        //an orbit, and is faster than the limit when it does.
        public double ClosingTime(double distance)
        {
            double closingSpeed = Math.Max(0.01, Math.Min(distance / 100, maxClosingSpeed));
            return Math.Min(Math.Max(distance / closingSpeed, 1.5 * Core.Node.FlipTime()), Orbit.period / 4);
        }

        //The cost of a Hohmann transfer between two circular orbits, and the lead angle of the target for it
        private static (double cost, double lead) Hohmann(double mu, double radius1, double radius2)
        {
            (double dV1, double dV2, _, double lead) = Astro.HohmannTransferParameters(mu, new V3(radius1, 0, 0), new V3(radius2, 0, 0));
            return (Math.Abs(dV1) + Math.Abs(dV2), lead);
        }

        //The radius of the orbit around the target's that brings the next window within maxOrbits, or NaN if it isn't a safe one.
        //Below the target the phase angle closes, and above it it opens, so the two have opposite shares of a cycle left to go.
        private double PhasingOrbitRadius(double maxOrbits)
        {
            double targetRadius = Core.Target.TargetOrbit.semiMajorAxis;
            (_, double lead) = Hohmann(MainBody.gravParameter, Orbit.semiMajorAxis, targetRadius);
            double closing = Clamp2Pi(Deg2Rad(Orbit.PhaseAngle(Core.Target.TargetOrbit, VesselState.Time)) - lead) / TAU;

            bool low = Orbit.semiMajorAxis < targetRadius;
            double radius = PhasingOrbitRadius(low, low ? closing : 1 - closing, maxOrbits);
            if (MainBody.IsSafeOrbitRadius(radius)) return radius;

            radius = PhasingOrbitRadius(!low, low ? 1 - closing : closing, maxOrbits);
            return MainBody.IsSafeOrbitRadius(radius) ? radius : double.NaN;
        }

        //The full size at a whole cycle to go gets there in 0.8 maxOrbits. A smaller share needs a smaller, cheaper orbit.
        private double PhasingOrbitRadius(bool low, double cycleToGo, double maxOrbits)
        {
            double axisRatio = Math.Pow(1 + 1.25 * Math.Max(cycleToGo, 0.02) / maxOrbits, 2.0 / 3.0);
            return low ? Core.Target.TargetOrbit.semiMajorAxis / axisRatio : Core.Target.TargetOrbit.semiMajorAxis * axisRatio;
        }

        //Orbits of nearly the same size have a transfer at any time, which can cost far more than going to a phasing orbit and back.
        private bool TooCostly(double cost, double phasingOrbitRadius)
        {
            double mu = MainBody.gravParameter;
            if (cost <= 2 * Hohmann(mu, Orbit.semiMajorAxis, Core.Target.TargetOrbit.semiMajorAxis).cost)
                return false;
            return cost > 2 * Hohmann(mu, Orbit.semiMajorAxis, phasingOrbitRadius).cost;
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

            double closestApproachUT = Orbit.MinimumSeparationTime(Core.Target.TargetOrbit, VesselState.Time);
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
                    Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, closestApproachUT, Core.Target.TargetOrbit);
                    Vessel.PlaceManeuverNode(Orbit, dV, BurnUTToStopAtDesiredDistance(closestApproachUT));

                    status = Localizer.Format("#MechJeb_RZauto_statu3"); //"Planning to match velocities at closest approach."
                }
                else
                {
                    //We're not far from the target. Close the distance
                    double UT = VesselState.Time + 15;

                    (Vector3d dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(Orbit, UT, Core.Target.TargetOrbit, closingTime);
                    if (UT < EarliestBurnUT(dV.magnitude))
                    {
                        UT = EarliestBurnUT(dV.magnitude);
                        (dV, _) = OrbitalManeuverCalculator.DeltaVToInterceptAtTime(Orbit, UT, Core.Target.TargetOrbit, closingTime);
                    }

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
                Vector3d dV = OrbitalManeuverCalculator.DeltaVToMatchVelocities(Orbit, closestApproachUT, Core.Target.TargetOrbit);
                Vessel.PlaceManeuverNode(Orbit, dV, BurnUTToStopAtDesiredDistance(closestApproachUT));

                status = Localizer.Format("#MechJeb_RZauto_statu5"); //"On intercept course. Planning to match velocities at closest approach."
            }
            else if (Orbit.RelativeInclination(Core.Target.TargetOrbit) < 0.05 && Orbit.eccentricity < 0.05)
            {
                //We're not on an intercept course, but we have a circular orbit in the right plane.

                (Vector3d hohmannDV, double hohmannUT, Vector3d arrivalDV, _) =
                    OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(Orbit, Core.Target.TargetOrbit, VesselState.Time, coplanar: false);

                double numPhasingOrbits = (hohmannUT - VesselState.Time) / Orbit.period;

                double actualMaxPhasingOrbits = Math.Max(maxPhasingOrbits, 5); // ignore input values that are unreasonably small

                double phasingOrbitRadius = PhasingOrbitRadius(actualMaxPhasingOrbits);

                //With no safe phasing orbit we can only wait for the window, however far it is.
                if (double.IsNaN(phasingOrbitRadius)
                    || (numPhasingOrbits < actualMaxPhasingOrbits && !TooCostly(hohmannDV.magnitude + arrivalDV.magnitude, phasingOrbitRadius)))
                {
                    //It won't be too long until the intercept window. Plot a Hohmann transfer intercept.
                    //If it opens too soon to start the burn on time, take it as early as we can.
                    double earliestUT = EarliestBurnUT(hohmannDV.magnitude);
                    if (hohmannUT < earliestUT)
                    {
                        (hohmannDV, hohmannUT, _, _) = OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(Orbit, Core.Target.TargetOrbit, earliestUT,
                            fixedTime: true, coplanar: false);
                    }

                    Vessel.PlaceManeuverNode(Orbit, hohmannDV, hohmannUT);

                    status = Localizer.Format("#MechJeb_RZauto_statu6",
                        numPhasingOrbits.ToString("F2")); //"Planning Hohmann transfer for intercept after " +  + " phasing orbits."
                }
                else
                {
                    //We are in a circular orbit in the right plane, but we aren't phasing quickly enough. Move to a better phasing orbit
                    List<ManeuverParameters> maneuvers = OrbitalManeuverCalculator.ManeuversToCircularOrbit(Orbit, VesselState.Time + 15, phasingOrbitRadius);
                    double earliestUT = EarliestBurnUT(maneuvers[0].dV.magnitude);
                    if (maneuvers[0].UT < earliestUT)
                        maneuvers = OrbitalManeuverCalculator.ManeuversToCircularOrbit(Orbit, earliestUT, phasingOrbitRadius);

                    foreach (ManeuverParameters maneuver in maneuvers)
                        Vessel.PlaceManeuverNode(Orbit, maneuver.dV, maneuver.UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu7", numPhasingOrbits.ToString("F1"), maxPhasingOrbits.Text,
                        (phasingOrbitRadius - MainBody.Radius)
                       .ToSI(0)); //"Next intercept window would be <<1>> orbits away, which is more than the maximum of <<2>> phasing orbits. Increasing phasing rate by establishing new phasing orbit at <<3>>m
                }
            }
            else if (Orbit.RelativeInclination(Core.Target.TargetOrbit) < 0.05)
            {
                //We're not on an intercept course. We're in the right plane, but our orbit isn't circular. Circularize.

                double safeRadius = MainBody.SafeOrbitRadius();

                bool circularizeAtPe;
                if (Orbit.eccentricity > 1) circularizeAtPe = true;
                else
                    circularizeAtPe = Orbit.PeR >= safeRadius && Math.Abs(Orbit.PeR - Core.Target.TargetOrbit.semiMajorAxis) <
                        Math.Abs(Orbit.ApR - Core.Target.TargetOrbit.semiMajorAxis);

                double UT;
                if (circularizeAtPe) UT = Math.Max(VesselState.Time, Orbit.NextPeriapsisTime(VesselState.Time));
                else UT = Orbit.NextApoapsisTime(VesselState.Time);

                Vector3d dV;
                if (Orbit.Radius(UT) >= safeRadius) dV = OrbitalManeuverCalculator.DeltaVToCircularize(Orbit, UT);
                else if (VesselState.Radius >= safeRadius)
                {
                    //A hyperbola coming in too low: the circular orbit would be in the atmosphere or the terrain, so raise it first
                    UT = VesselState.Time + 15;
                    dV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(Orbit, UT, safeRadius);
                }
                else
                {
                    status = Localizer.Format("#MechJeb_RZauto_orbitTooLow"); //"The orbit is too low to rendezvous from."
                    ScreenMessages.PostScreenMessage(status, 5);
                    Users.Clear();
                    return;
                }

                Vessel.PlaceManeuverNode(Orbit, dV, UT);

                status = Localizer.Format("#MechJeb_RZauto_statu8"); //"Circularizing."
            }
            else
            {
                //We're not on an intercept course, and we're not in the right plane. Match planes
                bool nodeAhead = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanes(Orbit, Core.Target.TargetOrbit, VesselState.Time, out Vector3d dV, out double UT);

                //A node too close to start the burn on time: take the next one.
                if (nodeAhead && UT < EarliestBurnUT(dV.magnitude))
                    nodeAhead = OrbitalManeuverCalculator.DeltaVAndTimeToMatchPlanes(Orbit, Core.Target.TargetOrbit, EarliestBurnUT(dV.magnitude), out dV, out UT);

                if (nodeAhead)
                {
                    Vessel.PlaceManeuverNode(Orbit, dV, UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu9"); //"Matching planes."
                }
                else
                {
                    //Past both nodes of a hyperbola: only a closed orbit will reach another one
                    UT = VesselState.Time + 15;
                    Vessel.PlaceManeuverNode(Orbit, OrbitalManeuverCalculator.DeltaVToCircularize(Orbit, UT), UT);

                    status = Localizer.Format("#MechJeb_RZauto_statu8"); //"Circularizing."
                }
            }
        }
    }
}
