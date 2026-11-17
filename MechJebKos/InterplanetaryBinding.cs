/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:MANEUVERPLANNER:INTERPLANETARY - the "advanced transfer to another planet" (porkchop) maneuver.
    //
    // It takes two steps, like the window: COMPUTE() starts a background grid search of departure times and transfer durations
    // (the porkchop plot) and MAKENODES() solves the ejection burn for the chosen point of it. Between them, wait for FINISHED
    // and pick a point: the lowest delta-V one by default, or SELECTASAP() / SELECT(departure, duration) in PORKCHOP mode.
    // LIMITED_TIME mode always takes the lowest delta-V transfer arriving within MAXARRIVALTIME.
    //
    // MODE, the porkchop window, MAXARRIVALTIME and INCLUDECAPTUREBURN take effect at the next COMPUTE(); PERIAPSIS and
    // INCLINATION at MAKENODES(). The window bounds and MAXARRIVALTIME read NaN until set: COMPUTE() then uses the window's
    // defaults (1.5 synodic periods of departures, transfers up to twice the Hohmann time).
    //
    //   SET xfer TO ADDONS:MECHJEB:MANEUVERPLANNER:INTERPLANETARY.
    //   xfer:COMPUTE(). WAIT UNTIL xfer:FINISHED.
    //   IF NOT xfer:FAILED { xfer:SELECTASAP(). xfer:MAKENODES(). }
    [KOSNomenclature("MechJebInterplanetary")]
    public class InterplanetaryBinding : OperationBinding<OperationAdvancedTransfer>
    {
        private OperationAdvancedTransfer.Mode _mode = OperationAdvancedTransfer.Mode.PORKCHOP;
        private double? _minDepartureTime;
        private double? _maxDepartureTime;
        private double? _minTransferTime;
        private double? _maxTransferTime;
        private double? _maxArrivalTime;
        private double? _periapsis; // m

        public InterplanetaryBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            // --- the grid search (taking effect at COMPUTE) ---
            AddSuffix("MODE", new SetSuffix<StringValue>(() => _mode.ToString(), value => _mode = ParseMode(value),
                "PORKCHOP (pick any point of the departure/duration grid) or LIMITED_TIME (cheapest arrival within MAXARRIVALTIME). Takes effect at the next COMPUTE()."));
            AddSuffix("DEPARTUREMIN", new SetSuffix<ScalarValue>(() => _minDepartureTime ?? double.NaN, value => _minDepartureTime = Finite(value, "DEPARTUREMIN"),
                "PORKCHOP: earliest departure UT of the grid (no earlier than the start of the orbit planned on); NaN for the default."));
            AddSuffix("DEPARTUREMAX", new SetSuffix<ScalarValue>(() => _maxDepartureTime ?? double.NaN, value => _maxDepartureTime = Finite(value, "DEPARTUREMAX"),
                "PORKCHOP: latest departure UT of the grid; NaN for the default."));
            AddSuffix("DURATIONMIN", new SetSuffix<ScalarValue>(() => _minTransferTime ?? double.NaN, value => _minTransferTime = Finite(value, "DURATIONMIN"),
                "PORKCHOP: shortest transfer duration (s) of the grid, an hour or more; NaN for the default."));
            AddSuffix("DURATIONMAX", new SetSuffix<ScalarValue>(() => _maxTransferTime ?? double.NaN, value => _maxTransferTime = Finite(value, "DURATIONMAX"),
                "PORKCHOP: longest transfer duration (s) of the grid; NaN for the default."));
            AddSuffix("MAXARRIVALTIME", new SetSuffix<ScalarValue>(() => _maxArrivalTime ?? double.NaN, value => _maxArrivalTime = Finite(value, "MAXARRIVALTIME"),
                "LIMITED_TIME: arrive at most this many seconds after the start of the orbit planned on; NaN for the default."));
            AddSuffix("RESETWINDOW", new NoArgsVoidSuffix(ResetWindow,
                "Go back to the default porkchop window and MAXARRIVALTIME (at the next COMPUTE())."));
            AddSuffix("INCLUDECAPTUREBURN", new SetSuffix<BooleanValue>(() => Op.IncludeCaptureBurn, value => Op.IncludeCaptureBurn = value,
                "Rank the grid by ejection plus capture delta-V rather than ejection alone (no capture node is made). Takes effect at the next COMPUTE()."));

            // --- the burn (taking effect at MAKENODES) ---
            AddSuffix("PERIAPSIS", new SetSuffix<ScalarValue>(() => _periapsis ?? DefaultPeriapsis(), value => _periapsis = Finite(value, "PERIAPSIS"),
                "Arrival periapsis altitude (m) at a celestial target; defaults to 10 km above its atmosphere, or 100 km (NaN for a target that is not a body, where it does not apply)."));
            AddSuffix("INCLINATIONFLAG", new SetSuffix<BooleanValue>(() => Op.TargetInclinationFlag, value => Op.TargetInclinationFlag = value,
                "Also target INCLINATION for the arrival orbit."));
            AddSuffix("INCLINATION", new SetSuffix<ScalarValue>(() => Op.TargetInclination.Val, value => Op.TargetInclination.Val = value,
                "Arrival orbit inclination in degrees, used when INCLINATIONFLAG is true."));

            // --- computing ---
            AddSuffix("COMPUTE", new NoArgsVoidSuffix(Compute,
                "Start the grid search from the orbit after the last maneuver node (as MAKENODES plans from), for the current target."));
            AddSuffix("COMPUTING", new Suffix<BooleanValue>(() => Op.IsComputing,
                "True while the grid search started by COMPUTE() runs."));
            AddSuffix("FINISHED", new Suffix<BooleanValue>(() => Op.IsFinished,
                "True once the grid search is done (check FAILED)."));
            AddSuffix("FAILED", new Suffix<BooleanValue>(() => Op.HasFailed,
                "True when the finished grid search found no transfer at all."));
            AddSuffix("PROGRESS", new Suffix<ScalarValue>(() => Op.Progress,
                "Percent done of the grid search."));

            // --- choosing the transfer ---
            AddSuffix("SELECTLOWESTDV", new NoArgsVoidSuffix(() => RequireGrid(true).SelectLowestDv(),
                "PORKCHOP: choose the lowest delta-V point of the grid (the default)."));
            AddSuffix("SELECTASAP", new NoArgsVoidSuffix(() => RequireGrid(true).SelectAsap(),
                "PORKCHOP: choose the lowest delta-V transfer of the earliest departure."));
            AddSuffix("SELECT", new TwoArgsSuffix<ScalarValue, ScalarValue>(Select,
                "PORKCHOP: choose the grid point nearest a departure UT and a transfer duration (s); it must have a transfer."));
            AddSuffix("SELECTEDDEPARTURE", new Suffix<ScalarValue>(() => Selection().departureTime,
                "UT of the chosen departure (NaN before the grid is done)."));
            AddSuffix("SELECTEDDURATION", new Suffix<ScalarValue>(() => Selection().duration,
                "Transfer duration (s) of the chosen point (NaN before the grid is done)."));
            AddSuffix("SELECTEDDELTAV", new Suffix<ScalarValue>(() => Selection().deltaV,
                "The grid's delta-V estimate (m/s) at the chosen point; the burn MAKENODES solves differs somewhat (NaN before the grid is done)."));
        }

        private static OperationAdvancedTransfer.Mode ParseMode(StringValue value)
        {
            string name = value.ToString().Trim();
            // Enum.TryParse also accepts numbers ("1"), so only accept the names.
            if (name.Length > 0 && !char.IsDigit(name[0]) && name[0] != '-' && name[0] != '+' &&
                Enum.TryParse(name, true, out OperationAdvancedTransfer.Mode mode) && Enum.IsDefined(typeof(OperationAdvancedTransfer.Mode), mode))
                return mode;
            throw new KOSException($"Invalid interplanetary MODE '{value}'. Expected PORKCHOP or LIMITED_TIME.");
        }

        // NaN clears the setting back to its default
        private static double? Finite(ScalarValue value, string suffix)
        {
            double d = value.GetDoubleValue();
            if (double.IsNaN(d))
                return null;
            if (double.IsInfinity(d))
                throw new KOSException($"{suffix} must be a finite number.");
            return d;
        }

        private void ResetWindow()
        {
            _minDepartureTime = _maxDepartureTime = _minTransferTime = _maxTransferTime = _maxArrivalTime = null;
        }

        private double DefaultPeriapsis() =>
            Core.Target.Target is CelestialBody body ? OperationAdvancedTransfer.DefaultPeriapsisKm(body) * 1000 : double.NaN;

        private void Compute()
        {
            MechJebCore core = Core;
            MechJebModuleManeuverPlanner planner = Planner;
            if (planner.TechLocked)
                throw new KOSLowTechException("use the MechJeb maneuver planner", "the tech node that unlocks it");

            core.Target.SyncWithVesselTarget();
            planner.GetOrbitAndUT(true, out Orbit o, out double ut);

            Op.SelectionMode = _mode;
            string? error = Op.Compute(o, ut, core.Target, _minDepartureTime, _maxDepartureTime, _minTransferTime, _maxTransferTime, _maxArrivalTime);
            if (error != null)
                throw new KOSException(error);
        }

        // the finished grid, or throws; porkchop: and one that has points to choose from
        private OperationAdvancedTransfer RequireGrid(bool porkchop)
        {
            if (!Op.HasWorker)
                throw new KOSException("Call COMPUTE() first, then wait for FINISHED.");
            if (!Op.IsFinished)
                throw new KOSException($"The interplanetary grid search is still running ({Op.Progress}%); wait for FINISHED.");
            if (Op.HasFailed)
                throw new KOSException("The interplanetary grid search found no transfer; try another window or MODE.");
            if (porkchop && !Op.IsPorkchopMode)
                throw new KOSException("Choosing a point needs MODE PORKCHOP (at the last COMPUTE()); LIMITED_TIME always takes the cheapest.");
            return Op;
        }

        private void Select(ScalarValue departureTime, ScalarValue duration)
        {
            if (!RequireGrid(true).SelectTimes(departureTime, duration))
                throw new KOSException($"No transfer at departure UT {departureTime} with duration {duration} s: it is outside the grid or has no solution.");
        }

        private (double departureTime, double duration, double deltaV) Selection() =>
            Op.TryGetSelection(out double departureTime, out double duration, out double deltaV)
                ? (departureTime, duration, deltaV)
                : (double.NaN, double.NaN, double.NaN);

        protected override void BeforePlan(Orbit o, double ut, bool replaceLast)
        {
            // the grid was computed for the orbit after the last node; replacing that node plans from the orbit before it
            if (replaceLast)
                throw new KOSException("CHANGELASTNODE is not supported for INTERPLANETARY: remove the node, then COMPUTE() and MAKENODES().");

            RequireGrid(false);
            if (Op.IsStale(o, Core.Target.TargetOrbit))
                throw new KOSException("The orbit or the target changed since COMPUTE(); call COMPUTE() again.");

            // a vessel or asteroid target has no periapsis to aim for (the solver intercepts it whatever this is)
            double periapsis = _periapsis ?? DefaultPeriapsis();
            Op.PeriapsisHeight.Val = double.IsNaN(periapsis) ? 0 : periapsis / 1000;
        }
    }
}
