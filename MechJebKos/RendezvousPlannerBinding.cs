/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Linq;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using KSP.Localization;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:RENDEZVOUSPLANNER - the Rendezvous Planner window (MechJebModuleRendezvousGuidance): its maneuvers, which
    // replace the maneuver nodes and return the new ones, and its readouts, as numbers. PHASINGALTITUDE is the window's own.
    //
    // They need a target in this vessel's sphere of influence, and this to be the active vessel. Kerboscript can't catch the
    // exception a maneuver throws, so check PROBLEM first: a maneuver can still throw for a reason of its own (no node with the
    // target's plane ahead, a target too far to get closer, ...).
    //
    //   SET p TO ADDONS:MECHJEB:RENDEZVOUSPLANNER.
    //   IF p:PROBLEM = "" AND p:RELATIVEINCLINATION > 0.05 {
    //     p:ALIGNPLANES().
    //     ADDONS:MECHJEB:NODEEXECUTOR:EXECUTEONENODE().
    //   }
    [KOSNomenclature("MechJebRendezvousPlanner")]
    public class RendezvousPlannerBinding : ComputerModuleBinding<MechJebModuleRendezvousGuidance>
    {
        private readonly SharedObjects _shared;

        // the closest approach search marches a whole orbit, so the readouts share one each physics tick
        private Orbit? _approachTarget;
        private double _approachSearchedAt = double.NaN;
        private double _approachTime;

        public RendezvousPlannerBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core)
        {
            _shared = shared;
        }

        protected override void InitializeSuffixes()
        {
            // --- maneuvers ---
            Maneuver("ALIGNPLANES", p => p.AlignPlanes(),
                "Match planes with the target at the next node ahead with its plane.");
            Maneuver("ESTABLISHORBIT", p => p.EstablishNewOrbit(),
                "Move to a circular phasing orbit at PHASINGALTITUDE.");
            Maneuver("INTERCEPTHOHMANN", p => p.InterceptWithHohmann(),
                "Intercept the target with a Hohmann transfer at the next window.");
            Maneuver("MATCHVELOCITIES", p => p.MatchVelocitiesAtClosestApproach(),
                "Match velocities at the closest approach, stopping at the rendezvous autopilot's DESIREDDISTANCE from the target.");
            Maneuver("GETCLOSER", p => p.GetCloser(),
                "Intercept a target nearby (the rendezvous autopilot's close-in range), at its MAXCLOSINGSPEED.");
            AddSuffix("PROBLEM", new Suffix<StringValue>(() => Refusal()?.Message ?? "",
                "Why the maneuvers would throw now (no target, a target in another sphere of influence, the rendezvous autopilot " +
                "running, ...), or \"\" when they wouldn't for that."));

            // --- the phasing orbit ---
            AddSuffix("PHASINGALTITUDE", new SetSuffix<ScalarValue>(() => Module.phasingOrbitAltitude.Val, value =>
                {
                    double altitude = KosArgs.Finite(value, "PHASINGALTITUDE");
                    if (altitude <= 0)
                        throw new KOSException("PHASINGALTITUDE must be more than 0 m.");
                    Module.phasingOrbitAltitude.Val = altitude;
                },
                "The altitude (m) of the orbit ESTABLISHORBIT moves to."));
            AddSuffix("PHASINGORBITSAFE", new Suffix<BooleanValue>(() => Module.PhasingOrbitIsSafe,
                "False when PHASINGALTITUDE meets the atmosphere, the terrain, a moon or the edge of the sphere of influence (the " +
                "window only warns)."));

            // --- readouts ---
            AddSuffix("ONTHEGROUND", new Suffix<BooleanValue>(() =>
                {
                    Core.Target.SyncWithVesselTarget();
                    return Module.OnTheGround;
                },
                "True when the target or this vessel is on the ground, where the maneuvers make no sense (the window only warns)."));
            AddSuffix("CLOSESTAPPROACHTIME", new Suffix<ScalarValue>(() => ClosestApproachTime(),
                "The universal time (s) of the next closest approach to the target."));
            AddSuffix("CLOSESTAPPROACHDISTANCE", new Suffix<ScalarValue>(() =>
                    Module.Orbit.Separation(TargetOrbit(), ClosestApproachTime()),
                "The distance (m) from the target at the next closest approach."));
            AddSuffix("CLOSESTAPPROACHSPEED", new Suffix<ScalarValue>(() =>
                    OrbitalManeuverCalculator.DeltaVToMatchVelocities(Module.Orbit, ClosestApproachTime(), TargetOrbit()).magnitude,
                "The speed (m/s) relative to the target at the next closest approach."));
            AddSuffix("RELATIVEINCLINATION", new Suffix<ScalarValue>(() => Module.Orbit.RelativeInclination(TargetOrbit()),
                "The angle (degrees) between this vessel's orbit and the target's."));
        }

        private void Maneuver(string name, Func<MechJebModuleRendezvousGuidance, string?> plan, string description) =>
            AddSuffix(name, new NoArgsSuffix<ListValue>(() => Plan(plan),
                description + " Replaces the maneuver nodes, and returns the new ones as a list of nodes."));

        // ---- the window's gate ----

        // Why the maneuvers can't run, or null. Built but not thrown, so PROBLEM and the maneuvers agree.
        private KOSException? Refusal()
        {
            MechJebModuleRendezvousGuidance planner = Module;
            if (planner.TechLocked)
                return new KOSLowTechException("use the MechJeb rendezvous planner", "the tech node that unlocks it");
            if (!Career.CanMakeNodes(out string reason))
                return new KOSLowTechException("use maneuver nodes", reason);
            if (Core.vessel.patchedConicSolver == null)
                return new KOSSituationallyInvalidException(
                    "A KSP limitation makes it impossible to access the maneuver nodes of this vessel at this time. (perhaps it's not the active vessel?)");
            // it would replace them on its next tick, or have the node executor burn a node that is gone
            if (Core.GetComputerModule<MechJebModuleRendezvousAutopilot>()?.Enabled == true)
                return new KOSException("The rendezvous autopilot plans the maneuver nodes while it runs: RENDEZVOUSAUTOPILOT:DISENGAGE() first.");
            return TargetRefusal();
        }

        // what the window needs to show its maneuvers and readouts: a target in this vessel's sphere of influence
        private KOSException? TargetRefusal()
        {
            MechJebModuleTargetController target = Core.Target;
            // a SET TARGET earlier in this physics tick may not have reached MechJeb yet
            target.SyncWithVesselTarget();
            if (!target.NormalTargetExists)
                return new KOSException(Localizer.Format("#MechJeb_RZplan_label1")); //"Select a target to rendezvous with."
            if (target.TargetOrbit.referenceBody != Module.Orbit.referenceBody)
                return new KOSException(Localizer.Format("#MechJeb_RZplan_label2")); //"Rendezvous target must be in the same sphere of influence."
            return null;
        }

        private Orbit TargetOrbit()
        {
            KOSException? refusal = TargetRefusal();
            if (refusal != null)
                throw refusal;
            return Core.Target.TargetOrbit;
        }

        // ---- maneuvers ----

        private ListValue Plan(Func<MechJebModuleRendezvousGuidance, string?> plan)
        {
            KOSException? refusal = Refusal();
            if (refusal != null)
                throw refusal;

            MechJebModuleRendezvousGuidance planner = Module;
            Vessel vessel = Core.vessel;
            var before = new List<ManeuverNode>(vessel.patchedConicSolver.maneuverNodes);

            string? problem;
            try
            {
                problem = plan(planner);
            }
            catch (Exception e) when (!(e is KOSException))
            {
                // as the window: the message is all a script sees
                UnityEngine.Debug.LogException(e);
                throw new KOSException(Localizer.Format("#MechJeb_RZplan_failed", e.Message)); //"That burn can't be planned: <<1>>"
            }
            finally
            {
                // a kOS node for a node the plan removed would now point at a node that is gone
                List<ManeuverNode> after = vessel.patchedConicSolver.maneuverNodes;
                foreach (ManeuverNode node in before)
                    if (!after.Contains(node))
                        _shared.NodeLookup.Remove(node);
            }

            if (problem != null)
                throw new KOSException(problem);

            return new ListValue(vessel.patchedConicSolver.maneuverNodes.Select(n => (Structure)Node.FromExisting(vessel, n, _shared)));
        }

        // ---- readouts ----

        private double ClosestApproachTime()
        {
            MechJebModuleRendezvousGuidance planner = Module;
            Orbit target = TargetOrbit();
            double now = planner.VesselState.Time;
            if (!ReferenceEquals(target, _approachTarget) || now != _approachSearchedAt)
            {
                _approachTarget = target;
                _approachSearchedAt = now;
                _approachTime = planner.Orbit.MinimumSeparationTime(target, now);
            }

            return _approachTime;
        }
    }
}
