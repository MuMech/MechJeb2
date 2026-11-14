/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:RENDEZVOUSAUTOPILOT - the rendezvous autopilot (MechJebModuleRendezvousAutopilot). The options are the Rendezvous
    // Autopilot window's own, shared by every vessel: what a script sets shows there, and the other way round.
    //
    // It brings this vessel to rest DESIREDDISTANCE from the target, a vessel in orbit in the same sphere of influence, by planning
    // maneuver nodes one at a time and executing them with the node executor. It removes the nodes there are when it starts, and it
    // needs this to be the active vessel. Kerboscript can't catch the exception ENGAGE() throws, so check PROBLEM first.
    //
    //   SET r TO ADDONS:MECHJEB:RENDEZVOUSAUTOPILOT.
    //   ADDONS:MECHJEB:TARGET:SELECT(VESSEL("Station")).
    //   SET r:DESIREDDISTANCE TO 100.
    //   IF r:PROBLEM = "" {
    //     r:ENGAGE().
    //     WAIT UNTIL NOT r:ENABLED.
    //     PRINT r:RESULT.                  // ARRIVED, or why it stopped (see MESSAGE)
    //   } ELSE PRINT r:PROBLEM.
    //
    // A run goes on after the program that started it ends, until it arrives, stops or DISENGAGE().
    [KOSNomenclature("MechJebRendezvousAutopilot")]
    public class RendezvousAutopilotBinding : ComputerModuleBinding<MechJebModuleRendezvousAutopilot>
    {
        private readonly AttitudeBinding _attitude;

        public RendezvousAutopilotBinding(Func<MechJebCore?> core, AttitudeBinding attitude) : base(core)
        {
            _attitude = attitude;
        }

        private MechJebModuleRendezvousAutopilotWindow Window =>
            Core.GetComputerModule<MechJebModuleRendezvousAutopilotWindow>() ??
            throw new KOSException($"MechJeb module {nameof(MechJebModuleRendezvousAutopilotWindow)} is not available on this vessel.");

        private MechJebModuleNodeExecutor NodeExecutor =>
            Core.Node ?? throw new KOSException($"MechJeb module {nameof(MechJebModuleNodeExecutor)} is not available on this vessel.");

        protected override void InitializeSuffixes()
        {
            // --- engage ---
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Module.Enabled, value =>
                {
                    if (value) Engage();
                    else Disengage();
                },
                "Whether the rendezvous autopilot runs: set true to engage it, false to stop it."));
            AddSuffix("ENGAGE", new NoArgsVoidSuffix(Engage,
                "Start the rendezvous with the target. Throws for the reason PROBLEM gives."));
            AddSuffix("DISENGAGE", new NoArgsVoidSuffix(Disengage,
                "Stop the rendezvous autopilot, also when the window or another processor started it, and the node executor it runs."));
            AddSuffix("PROBLEM", new Suffix<StringValue>(() => Refusal()?.Message ?? "",
                "Why ENGAGE() would throw now (no target, a target in another sphere of influence, ...), or \"\" when it wouldn't."));

            // --- options ---
            Setting("DESIREDDISTANCE", ap => ap.desiredDistance, 0, false, " m",
                "The distance (m) from the target to stop at.");
            Setting("MAXPHASINGORBITS", ap => ap.maxPhasingOrbits, 5, true, "",
                "The most orbits to wait for a Hohmann transfer window before moving to a faster phasing orbit (at least 5).");
            Setting("MAXCLOSINGSPEED", ap => ap.maxClosingSpeed, 0, false, " m/s",
                "The speed limit (m/s) closing in on a target nearby. Exceeded when it would take more than a quarter of an orbit.");
            AddSuffix("AUTOWARP", new SetSuffix<BooleanValue>(() => NodeExecutor.Autowarp, value => NodeExecutor.Autowarp = value,
                "Time-warp to the burns: the window's Auto-warp, which is NODEEXECUTOR:AUTOWARP."));

            // --- state ---
            AddSuffix("STATUS", new Suffix<StringValue>(() => Module.status,
                "The status text the window shows, which stays after a run: where it got to, or why it stopped."));
            AddSuffix("PHASE", new Suffix<StringValue>(() => Module.Phase.ToString(),
                "What the last node it planned is for: NONE (none yet), MATCH_PLANES, CIRCULARIZE, PHASING_ORBIT, HOHMANN, " +
                "MATCH_AT_CLOSEST_APPROACH, CLOSE_IN or MATCH_VELOCITIES."));
            AddSuffix("RESULT", new Suffix<StringValue>(() => Module.StopReason.ToString(),
                "How the last run ended: NONE while it runs (or before the first), ARRIVED, DISENGAGED, or a problem with MESSAGE: " +
                "NO_TARGET, TARGET_IS_BODY, OTHER_SOI, TARGET_LANDED, VESSEL_LANDED, ORBIT_TOO_LOW, PLANNING_FAILED."));
            AddSuffix("MESSAGE", new Suffix<StringValue>(() => Module.StopMessage,
                "The message the last run stopped with (\"\" for none)."));
        }

        private void Setting(string name, Func<MechJebModuleRendezvousAutopilot, EditableDouble> field, double min, bool orEqual, string unit,
            string description) =>
            AddSuffix(name, new SetSuffix<ScalarValue>(() => field(Module).Val, value =>
            {
                double d = KosArgs.Finite(value, name);
                if (orEqual ? d < min : d <= min)
                    throw new KOSException(orEqual ? $"{name} must be at least {min}{unit}." : $"{name} must be more than {min}{unit}.");
                field(Module).Val = d;
            }, description));

        // ---- engage ----

        // Why it can't start, or null. Built but not thrown, so PROBLEM and ENGAGE() agree.
        private KOSException? Refusal()
        {
            MechJebModuleRendezvousAutopilot ap = Module;
            // the autopilot has no tech of its own: the window's unlocks it
            if (Window.TechLocked)
                return new KOSLowTechException("use the MechJeb rendezvous autopilot", "the tech node that unlocks it");
            // it plans maneuver nodes and executes them
            if (!Career.CanMakeNodes(out string reason))
                return new KOSLowTechException("use maneuver nodes", reason);
            if (Core.vessel.patchedConicSolver == null)
                return new KOSSituationallyInvalidException(
                    "A KSP limitation makes it impossible to access the maneuver nodes of this vessel at this time. (perhaps it's not the active vessel?)");

            // a SET TARGET earlier in this physics tick may not have reached MechJeb yet
            Core.Target.SyncWithVesselTarget();
            // else it ends on its first tick
            string? problem = ap.Problem(out _);
            return problem == null ? null : new KOSException(problem);
        }

        private void Engage()
        {
            KOSException? refusal = Refusal();
            if (refusal != null)
                throw refusal;

            // the node executor steers: this processor's ATTITUDE claim would hold its last attitude after it ends
            _attitude.Release();
            Module.Users.Add(this);
        }

        // as the autopilot ends itself, whoever started it
        private void Disengage() => Module.Users.Clear();
    }
}
