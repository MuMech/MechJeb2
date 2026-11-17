/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.PSG;
using static System.Math;

namespace MuMech
{
    /// <summary>
    ///     What PSG tells the player about the ascent: the optimizer's state, the reach warning, why a solve failed, the first
    ///     solve's progress, a lock on the controls, and what guidance does with RCS. The modules keep the figures and cache the
    ///     strings; the words are here.
    /// </summary>
    internal static class PSGFeedback
    {
        /// <summary>"Optimizer: …": what the PSG optimizer is doing, or why it is not solving, so that a climbing staleness reads as a pause.</summary>
        /// <param name="failingFor">
        ///     the plan's age while the re-solves fail (a failure is kept until the next success), else 0: guidance keeps flying the
        ///     last plan, and its age says how stale it is
        /// </param>
        internal static string? OptimizerStateText(OptimizerState state, double seconds, bool noSolution, bool coasting = false, double failingFor = 0)
        {
            switch (state)
            {
                case OptimizerState.SOLVING:
                case OptimizerState.READY:
                    if (noSolution) return failingFor > 0 ? "Optimizer: no plan yet, the solves are failing" : "Optimizer: computing the ascent…";
                    // a re-solve or two can fail and the next succeed: only a plan older than a few seconds is worth a line
                    return failingFor > 3 ? $"Optimizer: re-solves failing, flying the plan from {GuiUtils.TimeToDHMS(failingFor)} ago" : "Optimizer: running";
                case OptimizerState.NO_PLAN:
                    return "Optimizer: no flight plan, no stages to optimize";
                case OptimizerState.STAGE_ENDING:
                    // the freeze comes before the end of each phase: a burn's end or, in a coast, the burn after it
                    return coasting
                        ? $"Optimizer: paused, the burn starts in {Ceiling(seconds):F0} s"
                        : $"Optimizer: paused, the stage ends in {Ceiling(seconds):F0} s";
                case OptimizerState.AFTER_STAGING:
                    return $"Optimizer: paused after staging, {Ceiling(seconds):F0} s";
                case OptimizerState.ON_PAD:
                    return "Optimizer: paused until liftoff";
                case OptimizerState.AFTER_PHASE:
                    return $"Optimizer: paused while the next phase starts, {Ceiling(seconds):F0} s";
                case OptimizerState.COAST_PAUSE:
                    return $"Optimizer: paused for the coast, resumes in {GuiUtils.TimeToDHMS(seconds)}";
                case OptimizerState.TERMINAL:
                    return "Optimizer: paused, terminal guidance";
                case OptimizerState.UNGUIDED_ONLY:
                    return "Optimizer: paused, only unguided stages left";
                case OptimizerState.FINISHED:
                    return "Optimizer: guidance finished";
                case OptimizerState.RETRYING:
                    return "Optimizer: can't plan with these settings, retrying";
                default:
                    return null;
            }
        }

        /// <summary>
        ///     Why the PSG plan can't reach orbit with this rocket, in words, or null. The burn lines round a shortfall under 2.5 m/s
        ///     to zero as noise, so this does too.
        /// </summary>
        /// <param name="shortfall">MechJebModulePSGSimulation.DeltaVShortfall, m/s</param>
        /// <param name="suborbitalPeriapsis">MechJebModulePSGSimulation.SuborbitalPeriapsis, m, or NaN</param>
        /// <param name="atmosphere">whether the body has an atmosphere</param>
        internal static string? ReachWarning(double shortfall, double suborbitalPeriapsis, bool atmosphere)
        {
            if (shortfall >= 2.5)
                return $"Not enough Δv: the rocket is {shortfall:F0} m/s short of the target orbit.";

            if (!double.IsNaN(suborbitalPeriapsis))
                return $"Not enough Δv for orbit: the planned orbit's periapsis is {suborbitalPeriapsis / 1000:F0} km, " +
                    (atmosphere ? "inside the atmosphere." : "under the surface.");

            return null;
        }

        /// <summary>How far in degrees the target inclination is below the launch latitude (negative when it is not).</summary>
        internal static double InclinationBelowLatitude(double latitude, double inclination)
        {
            inclination = Abs(inclination);
            return Abs(latitude) - (inclination < 90.0 ? inclination : 180.0 - inclination);
        }

        /// <summary>
        ///     A known reason for "Target unreachable", to follow the failure message, or empty: the same test as the window's red
        ///     "inc below current latitude" line.
        /// </summary>
        internal static string UnreachableCause(string? failure, double belowLatitude)
        {
            if (failure == null || failure.IndexOf("unreachable", StringComparison.OrdinalIgnoreCase) < 0)
                return string.Empty;

            return belowLatitude > 2.001 ? $" (the target inclination is {belowLatitude:F1}° below the launch latitude)" : string.Empty;
        }

        /// <summary>
        ///     The first solve's progress: short, for the autopilot status ("computing the ascent, step 2 of 5 (1m 12s)"), and
        ///     detailed, for the Ascent window ("computing the ascent, step 2 of 5, adding the unguided stages: 1m 12s, iteration
        ///     143, error 3.1e-02"). Both null before the first pass.
        /// </summary>
        internal static (string? shortText, string? detail) ProgressText(SolveProgress p)
        {
            if (p.Pass == 0)
                return (null, null);

            string time = GuiUtils.TimeToDHMS((int)p.PassSeconds);
            string step = $"computing the ascent, step {p.Pass} of {Max(p.Pass, p.PassCount)}";
            string detail = p.Iterations > 0 && !double.IsNaN(p.Infeasibility)
                ? $"{step}, {p.Name}: {time}, iteration {p.Iterations}, error {p.Infeasibility:0.0e+00}"
                : $"{step}, {p.Name}: {time}";

            return ($"{step} ({time})", detail);
        }

        /// <summary>The autopilot's status when another mod has taken MechJeb's controls (DeactivateControl).</summary>
        /// <param name="lockId">the input lock that mod put on the steering, to name it, or null</param>
        internal static string LockStatus(string? lockId) => $"Controls locked by {lockId ?? "another mod"}: MechJeb can't steer or throttle";

        // KSP's own lock while its window is out of focus: it blocks the keyboard and joystick, not MechJeb, which steers through
        // OnFlyByWire, so it never names the mod that took the controls
        private const string FOCUS_LOCK = "flightDriver_ApplicationFocus";

        /// <summary>
        ///     The id of an input lock that blocks steering on the vessel, or null, to name the mod that took MechJeb's controls: an
        ///     input lock alone only blocks the player's input. RP-1's is "RP0ControlLocker". MechJeb's own lock only blocks clicks,
        ///     and KSP's focus lock is skipped. Walks the lock dictionary with its struct enumerator, so no garbage.
        /// </summary>
        internal static string? SteeringLockId()
        {
            if (!InputLockManager.IsLocked(ControlTypes.PITCH))
                return null;

            foreach (KeyValuePair<string, ulong> entry in InputLockManager.lockStack)
            {
                if ((entry.Value & (ulong)ControlTypes.PITCH) != 0 && entry.Key != FOCUS_LOCK)
                    return entry.Key;
            }

            return null;
        }

        // what guidance does with RCS, said once at each change: it turns RCS on for a coast, and trims the end of the last burn with
        // RCS only when the RCS group is already on, which otherwise looks like chance
        internal static ScreenMessage RcsCoastMessage() => Message("PSG coast, RCS on to hold attitude and settle propellant");

        internal static ScreenMessage RcsTrimMessage() => Message("PSG finishing the burn with RCS");

        internal static ScreenMessage RcsOffMessage() => Message("RCS is off, so PSG won't trim the end of the burn with RCS");

        private static ScreenMessage Message(string text) =>
            new ScreenMessage($"<color=orange>[MechJeb]: {text}</color>", 5f, ScreenMessageStyle.UPPER_CENTER);
    }
}
