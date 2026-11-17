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

namespace MuMech.MechJebKos
{
    // Shared base for the ADDONS:MECHJEB:MANEUVERPLANNER:<operation> bindings: one maneuver planner Operation each.
    //
    // Every binding owns its own Operation instance, so a script's settings are its own: the planner window keeps one (static)
    // instance of each operation for every vessel, and kOS never touches those. The settings live as long as the kOS processor.
    //
    // MAKENODES / CHANGELASTNODE are the window's "Create a new" / "Change the last" maneuver node: they plan from the orbit after
    // the last node (or before the last node, which they then replace) and place the nodes. A plan that fails throws with
    // MechJeb's message, as the window shows it; a plan that succeeds with a warning leaves it in WARNING.
    [KOSNomenclature("MechJebOperation")]
    public abstract class OperationBinding<TOp> : Structure where TOp : Operation, new()
    {
        private readonly Func<MechJebCore?> _core;
        private readonly SharedObjects      _shared;
        private          string             _warning = "";

        protected readonly TOp Op = new TOp();

        protected OperationBinding(Func<MechJebCore?> core, SharedObjects shared)
        {
            _core   = core;
            _shared = shared;
            RegisterInitializer(InitializeSuffixes);
        }

        // the Core deliberately re-evaluates on every call since the core can update dynamically
        protected MechJebCore Core => _core() ?? throw new KOSException("MechJeb is not available on this vessel.");

        protected MechJebModuleManeuverPlanner Planner =>
            Core.GetComputerModule<MechJebModuleManeuverPlanner>() ??
            throw new KOSException($"MechJeb module {nameof(MechJebModuleManeuverPlanner)} is not available on this vessel.");

        private void InitializeSuffixes()
        {
            AddSuffix("NAME", new Suffix<StringValue>(() => Op.GetName(),
                "MechJeb's name for this maneuver, as the maneuver planner lists it."));
            AddSuffix("MAKENODES", new NoArgsSuffix<ListValue>(() => Plan(false),
                "Plan this maneuver from the orbit after the last maneuver node and add its node(s); returns them as a list of nodes."));
            AddSuffix("CHANGELASTNODE", new NoArgsSuffix<ListValue>(() => Plan(true),
                "Plan this maneuver from the orbit before the last maneuver node and replace that node with its node(s); returns them as a list of nodes."));
            AddSuffix("WARNING", new Suffix<StringValue>(() => _warning,
                "The warning MechJeb gave with the last nodes this made (\"\" for none)."));

            TimeSelector? timing = Op.Timing;
            if (timing != null)
            {
                AddSuffix("TIMEREF", new SetSuffix<StringValue>(() => timing.TimeReference.ToString(), value => SetTimeReference(timing, value),
                    "When to burn, one of TIMEREFS (APOAPSIS, PERIAPSIS, X_FROM_NOW, ALTITUDE, ...)."));
                AddSuffix("TIMEREFS", new Suffix<ListValue>(
                    () => new ListValue(timing.AllowedTimeReferences.Select(r => (Structure)new StringValue(r.ToString()))),
                    "The TIMEREF values this maneuver offers."));
                AddSuffix("LEADTIME", new SetSuffix<ScalarValue>(() => timing.LeadTime.Val, value => timing.LeadTime.Val = value,
                    "Seconds from the start of the orbit planned on (now, or the last node) to the burn, for TIMEREF X_FROM_NOW."));
                AddSuffix("TIMEALTITUDE", new SetSuffix<ScalarValue>(() => timing.CircularizeAltitude.Val,
                    value => timing.CircularizeAltitude.Val = value,
                    "Altitude (m) at which to burn, for TIMEREF ALTITUDE."));
            }

            InitializeOperationSuffixes();
        }

        protected abstract void InitializeOperationSuffixes();

        /// <summary>
        /// Anything the operation needs from the binding before it plans from <paramref name="o"/> at <paramref name="ut"/>, or a
        /// KOSException for a plan it can't make.
        /// </summary>
        protected virtual void BeforePlan(Orbit o, double ut, bool replaceLast) { }

        private ListValue Plan(bool replaceLast)
        {
            MechJebCore core = Core;
            MechJebModuleManeuverPlanner planner = Planner;
            Vessel vessel = core.vessel;

            if (planner.TechLocked)
                throw new KOSLowTechException("use the MechJeb maneuver planner", "the tech node that unlocks it");
            if (!Career.CanMakeNodes(out string reason))
                throw new KOSLowTechException("use maneuver nodes", reason);
            if (vessel.patchedConicSolver == null)
                throw new KOSSituationallyInvalidException(
                    "A KSP limitation makes it impossible to access the maneuver nodes of this vessel at this time. (perhaps it's not the active vessel?)");
            if (replaceLast && planner.PlannerNodeCount() == 0)
                throw new KOSException("There is no maneuver node to change.");

            // a SET TARGET earlier in this physics tick may not have reached MechJeb yet
            core.Target.SyncWithVesselTarget();

            planner.GetOrbitAndUT(!replaceLast, out Orbit o, out double ut);
            BeforePlan(o, ut, replaceLast);

            ManeuverNode? replaced = replaceLast ? planner.LastPlannerNode() : null;
            List<ManeuverNode>? nodes = planner.PlaceNodes(Op, o, ut, replaceLast);
            if (nodes == null)
            {
                string? message = Op.GetErrorMessage();
                throw new KOSException(string.IsNullOrEmpty(message) ? $"MechJeb could not plan \"{Op.GetName()}\"." : message);
            }

            // a kOS node for the replaced node would now point at a node that is gone
            if (replaced != null)
                _shared.NodeLookup.Remove(replaced);

            _warning = Op.GetErrorMessage() ?? "";
            return new ListValue(nodes.Select(n => (Structure)Node.FromExisting(vessel, n, _shared)));
        }

        private static void SetTimeReference(TimeSelector timing, StringValue value)
        {
            string name = value.ToString().Trim();
            // Enum.TryParse also accepts numbers ("7"), so only accept the names.
            if (name.Length > 0 && !char.IsDigit(name[0]) && name[0] != '-' && name[0] != '+' &&
                Enum.TryParse(name, true, out TimeReference timeReference) && timing.TrySetTimeReference(timeReference))
                return;

            throw new KOSException($"Invalid TIMEREF '{value}'. Expected one of {string.Join(", ", timing.AllowedTimeReferences)}.");
        }
    }
}
