/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Linq;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed;
using kOS.Suffixed.Part;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:DOCKINGAUTOPILOT - the docking autopilot (MechJebModuleDockingAutopilot) and its window's port selector
    // (MechJebModuleDockingGuidance). The options are the window's own: what a script sets shows there, and the other way round.
    //
    // It docks with the target: a docking port, or a vessel, which it only approaches, stopping where docking would start
    // (STOPPED_SHORT). Control the vessel from a docking port first. KSP turns a port target back into its vessel beyond 200 m from
    // the active vessel; a port chosen with SELECTPORT is remembered, and the autopilot targets it again inside 190 m.
    //
    //   SET d TO ADDONS:MECHJEB:DOCKINGAUTOPILOT.
    //   SHIP:DOCKINGPORTS[0]:CONTROLFROM().
    //   ADDONS:MECHJEB:TARGET:SELECT(VESSEL("Station")).
    //   d:SELECTPORT(d:PORTS[0]).
    //   SET d:SPEEDLIMIT TO 0.5.
    //   d:ENGAGE().
    //   WAIT UNTIL NOT d:ENABLED.
    //   PRINT d:RESULT.                    // DOCKED, or why it stopped (see MESSAGE)
    //
    // A run goes on after the program that started it ends, until it docks, stops or DISENGAGE().
    [KOSNomenclature("MechJebDockingAutopilot")]
    public class DockingAutopilotBinding : ComputerModuleBinding<MechJebModuleDockingAutopilot>
    {
        private readonly SharedObjects  _shared;
        private readonly AttitudeBinding _attitude;

        public DockingAutopilotBinding(Func<MechJebCore?> core, SharedObjects shared, AttitudeBinding attitude) : base(core)
        {
            _shared   = shared;
            _attitude = attitude;
        }

        private MechJebModuleDockingGuidance Guidance =>
            Core.GetComputerModule<MechJebModuleDockingGuidance>() ??
            throw new KOSException($"MechJeb module {nameof(MechJebModuleDockingGuidance)} is not available on this vessel.");

        protected override void InitializeSuffixes()
        {
            // --- engage ---
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Module.Enabled, value =>
                {
                    if (value) Engage();
                    else Disengage();
                },
                "Whether the docking autopilot runs: set true to engage it, false to stop it."));
            AddSuffix("ENGAGE", new NoArgsVoidSuffix(Engage,
                "Start docking with the target (a docking port, or a vessel to approach)."));
            AddSuffix("DISENGAGE", new NoArgsVoidSuffix(Disengage,
                "Stop the docking autopilot, also when the window or another processor started it."));

            // --- the target port ---
            AddSuffix("PORTS", new Suffix<ListValue>(Ports,
                "The target vessel's docking ports the port this vessel is controlled from can dock with and that aren't docked (as the " +
                "window's selector). Empty without a target vessel, while it's out of physics range, or while this vessel isn't " +
                "controlled from a docking port."));
            AddSuffix("SELECTPORT", new OneArgsSuffix<Structure>(SelectPort,
                "Target one of PORTS to dock with, and remember it while KSP has its vessel as the target (beyond 200 m). Not while ENABLED."));
            AddSuffix("HASPORT", new Suffix<BooleanValue>(() => CurrentPort() != null,
                "True when there is a port to dock with: the target, or the port remembered while its vessel is the target."));
            AddSuffix("PORT", new Suffix<Structure>(() =>
                {
                    ModuleDockingNode port = CurrentPort() ?? throw new KOSException("There is no target docking port.");
                    return PartValueFactory.Construct(port.part, _shared);
                },
                "The port to dock with (see HASPORT)."));

            // --- options ---
            Distance("SPEEDLIMIT", ap => ap.speedLimit, 0, "m/s",
                "Speed limit relative to the target in m/s (0: no limit).");
            Option("OVERRIDESAFEDISTANCE", ap => ap.overrideSafeDistance, (ap, v) => ap.overrideSafeDistance = v,
                "Use SAFEDISTANCE instead of the measured one.");
            Distance("SAFEDISTANCE", ap => ap.overridenSafeDistance, 0, "m",
                "How far (m) past the target the route passes, with OVERRIDESAFEDISTANCE.");
            Option("OVERRIDESTARTDISTANCE", ap => ap.overrideTargetSize, (ap, v) => ap.overrideTargetSize = v,
                "Use STARTDISTANCE instead of the measured one.");
            Distance("STARTDISTANCE", ap => ap.overridenTargetSize, 10, "m",
                "How far (m) in front of the port the final approach starts, with OVERRIDESTARTDISTANCE.");
            Option("STOPONCONTACT", ap => ap.stopOnContact, (ap, v) => ap.stopOnContact = v,
                "Stop when the vessel touches the target anywhere but at the ports.");
            Option("FORCEROLL", ap => ap.forceRol, (ap, v) => ap.forceRol = v,
                "Hold ROLL relative to the target port while docking.");
            AddSuffix("ROLL", new SetSuffix<ScalarValue>(() => Module.rol.Val, value => Module.rol.Val = KosArgs.Finite(value, "ROLL"),
                "Roll (degrees) relative to the target port, with FORCEROLL."));

            // --- state ---
            AddSuffix("STEP", new Suffix<StringValue>(() => Module.dockingStep.ToString(),
                "The step: INIT, BACKING_UP, MOVING_TO_START, DOCKING, WRONG_SIDE_BACKING_UP, WRONG_SIDE_LATERAL, WRONG_SIDE_SWITCHSIDE, " +
                "LEAVING_TARGET, or OFF."));
            AddSuffix("STATUS", new Suffix<StringValue>(() => Module.status,
                "The status text the window shows."));
            AddSuffix("DISTANCE", new Suffix<ScalarValue>(() => Module.zSep,
                "Distance (m) along the docking axis from the target, positive in front of it (updated while ENABLED)."));
            AddSuffix("LATERALDISTANCE", new Suffix<ScalarValue>(() => Module.lateralSep.magnitude,
                "Distance (m) from the docking axis (updated while ENABLED)."));
            AddSuffix("CURRENTSAFEDISTANCE", new Suffix<ScalarValue>(() => Module.safeDistance,
                "The safe distance (m) in use: SAFEDISTANCE, or the measured one (updated while ENABLED)."));
            AddSuffix("CURRENTSTARTDISTANCE", new Suffix<ScalarValue>(() => Module.targetSize,
                "The start distance (m) in use: STARTDISTANCE, or the measured one (updated while ENABLED)."));
            AddSuffix("RESULT", new Suffix<StringValue>(() => Module.StopReason.ToString(),
                "How the last run ended: NONE while it runs (or before the first), DOCKED, AT_PORT (the ports met, the magnets finish " +
                "it), DISENGAGED, TARGET_LOST, or a problem with MESSAGE: STOPPED_SHORT, CONTACT, STUCK, GAVE_UP, NO_CAPTURE, SPINNING, " +
                "PORT_CLOSED, INCOMPATIBLE, CONTROL_LOST, NOT_A_VESSEL."));
            AddSuffix("MESSAGE", new Suffix<StringValue>(() => Module.StopMessage,
                "The message the last run stopped with (\"\" for none)."));
        }

        private void Distance(string name, Func<MechJebModuleDockingAutopilot, EditableDouble> field, double min, string unit, string description) =>
            AddSuffix(name, new SetSuffix<ScalarValue>(() => field(Module).Val, value =>
            {
                double d = KosArgs.Finite(value, name);
                if (d < min)
                    throw new KOSException($"{name} must be at least {min} {unit}.");
                field(Module).Val = d;
            }, description));

        private void Option(string name, Func<MechJebModuleDockingAutopilot, bool> get, Action<MechJebModuleDockingAutopilot, bool> set,
            string description) =>
            AddSuffix(name, new SetSuffix<BooleanValue>(() => get(Module), value => set(Module, value), description));

        // ---- engage ----

        private void Engage()
        {
            MechJebModuleDockingAutopilot ap = Module;
            // the autopilot has no tech of its own: the window's unlocks it
            if (Guidance.TechLocked)
                throw new KOSLowTechException("use the MechJeb docking autopilot", "the tech node that unlocks it");

            // a SET TARGET earlier in this physics tick may not have reached MechJeb yet
            MechJebModuleTargetController target = Core.Target;
            target.SyncWithVesselTarget();
            // else it ends on its first tick
            if (!target.NormalTargetExists || target.Target.GetVessel() == null)
                throw new KOSException("The docking autopilot needs a target vessel or docking port.");

            // the autopilot steers: this processor's ATTITUDE claim would hold its last attitude after it ends
            _attitude.Release();
            ap.Users.Add(this);
        }

        // as the autopilot ends itself, whoever started it
        private void Disengage() => Module.Users.Clear();

        // ---- the target port ----

        private ListValue Ports()
        {
            MechJebModuleDockingGuidance guidance = Guidance;
            Core.Target.SyncWithVesselTarget();
            guidance.UpdateTargetPortSelector();
            return new ListValue(guidance.targetPorts.Select(p => (Structure)PartValueFactory.Construct(p.part, _shared)));
        }

        private void SelectPort(Structure value)
        {
            if (!(value is IKOSTargetable targetable) || !(targetable.Target is ModuleDockingNode port))
                throw new KOSException($"SELECTPORT takes a docking port, not a {value.KOSName}.");
            if (port == null || port.vessel == null)
                throw new KOSException($"{value} no longer exists.");

            MechJebModuleDockingAutopilot ap = Module;
            Vessel vessel = Core.vessel;
            if (ap.Enabled)
                throw new KOSException("The port can't change while the docking autopilot runs: DISENGAGE() first.");
            if (port.vessel == vessel)
                throw new KOSException("A ship cannot dock with a port of its own.");

            ModuleDockingNode referencePort = vessel.GetReferenceDockingPort();
            if (referencePort == null)
                throw new KOSException("Control the vessel from a docking port first (CONTROLFROM() on one of its ports).");
            if (!MechJebModuleDockingGuidance.IsSelectableTargetPort(referencePort, port))
                throw new KOSException($"{value} is docked, or can't dock with the port this vessel is controlled from.");

            Guidance.SelectTargetPort(port);
        }

        // the target, or the port the autopilot (or the selector) remembers while KSP has its vessel as the target
        private ModuleDockingNode? CurrentPort()
        {
            MechJebModuleTargetController target = Core.Target;
            target.SyncWithVesselTarget();
            if (!target.NormalTargetExists)
                return null;
            if (target.Target is ModuleDockingNode port)
                return port;

            Vessel targetVessel = target.Target.GetVessel();
            if (targetVessel == null)
                return null;

            ModuleDockingNode? wanted = Module.WantedPort;
            if (wanted == null || wanted.vessel != targetVessel)
                wanted = Guidance.selectedTargetPort;
            return wanted != null && wanted.vessel == targetVessel ? wanted : null;
        }
    }
}
