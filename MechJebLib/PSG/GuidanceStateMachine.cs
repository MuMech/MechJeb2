/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public enum PSGStatus { ENABLED, INITIALIZED, BURNING, COASTING, TERMINAL, TERMINAL_RCS, TERMINAL_STAGING, FINISHED }

    // what guidance does with the main throttle and the RCS this tick
    public enum GuidanceThrottle
    {
        // leave the throttle alone
        NONE,

        // the Solution's throttle
        SOLUTION,

        // throttle off
        OFF,

        // throttle off and RCS translating forward
        RCS_FORWARD
    }

    public enum GuidanceStaging
    {
        NONE,

        // stage with the staging controller's delays (asked every tick while it is wanted)
        STAGE,

        // stage now
        IMMEDIATE
    }

    [Flags]
    public enum GuidanceMessages { NONE = 0, RCS_COAST = 1, RCS_TRIM = 2, RCS_OFF = 4 }

    /// <summary>
    ///     What the vessel looks like to guidance this tick. Thrust and mass are in any units where thrust / mass is an
    ///     acceleration (kN and t in KSP). Positions, velocities and the acceleration are in the Solution's frame.
    /// </summary>
    public struct GuidanceInputs
    {
        // the Solution's clock (MET)
        public double Met;

        // the universal time, for the timers
        public double Time;

        // the length of a physics tick
        public double Dt;

        // the vessel's current KSP stage
        public int KSPStage;

        public bool Grounded;

        // the autopilot let guidance fly (AssertStart)
        public bool AllowExecution;

        public double Mass;

        // the engines' thrust available (with the engines out and the RCS group on, the RCS thrust along the vessel's axis)
        public double ThrustAvailable;

        // the RCS thrust available forward
        public double RCSThrustUp;

        // at least 10 % of the RCS thrust pushes the vessel forward (the RCS group off counts too)
        public bool ForwardRCS;

        // the RCS action group is on
        public bool RCSGroup;

        // the delta-v left in the current KSP stage by the stage stats, NaN while they have nothing for it
        public double StageDeltaV;

        public V3 R;
        public V3 V;
        public V3 A;

        // how long before the end of a coast the RCS settles the propellant
        public double UllageLeadTime;

        // the coast is a coast-during (inside the burn of one KSP stage)
        public bool CoastDuring;
    }

    /// <summary>
    ///     What guidance asks the vessel to do this tick.
    /// </summary>
    public struct GuidanceCommand
    {
        public GuidanceThrottle Throttle;

        // the autostage limit to hold, -1 to leave it
        public int AutoStageLimit;

        public GuidanceStaging Staging;

        // turn the RCS action group on
        public bool RCSGroupOn;

        // drop out of time warp
        public bool MinimumWarp;

        // a coast is behind: the flight plan must mark it done
        public bool MarkCoastDone;

        public GuidanceMessages Messages;

        // guidance is over: disengage
        public bool Finished;

        public static GuidanceCommand None => new GuidanceCommand { AutoStageLimit = -1 };
    }

    /// <summary>
    ///     The decisions of the PSG guidance controller: which part of the Solution the vessel is flying, when a burn ends, when
    ///     to coast, stage and finish. The KSP side gathers the inputs, calls Step once per physics tick, and applies the command.
    /// </summary>
    public class GuidanceStateMachine
    {
        public PSGStatus Status { get; private set; } = PSGStatus.ENABLED;

        public Solution? Solution { get; private set; }

        // a coast has been started since guidance was engaged
        public bool HasCoasted { get; private set; }

        // the universal time the last coast started
        public double StartCoast { get; private set; }

        private double _spentSince = double.NaN;

        public bool IsTerminal() => Status == PSGStatus.TERMINAL_RCS || Status == PSGStatus.TERMINAL_STAGING || Status == PSGStatus.TERMINAL;

        // not terminal guidance and not finished: when the optimizer should run
        public bool IsNormal() => Status == PSGStatus.INITIALIZED || Status == PSGStatus.BURNING || Status == PSGStatus.COASTING;

        public bool IsCoasting() => Status == PSGStatus.COASTING;

        public bool IsRCSTrim() => Status == PSGStatus.TERMINAL_RCS;

        // the state of an enabled controller with nothing to fly yet
        public void Reset()
        {
            Status = PSGStatus.ENABLED;
            Solution = null;
            HasCoasted = false;
            StartCoast = 0;
            _spentSince = double.NaN;
        }

        public void SetSolution(Solution solution)
        {
            Solution = solution;
            if (Status == PSGStatus.ENABLED)
                Status = PSGStatus.INITIALIZED;
        }

        public void Finish()
        {
            Status = PSGStatus.FINISHED;
            Solution = null;
        }

        public GuidanceCommand Step(in GuidanceInputs inputs)
        {
            GuidanceCommand cmd = GuidanceCommand.None;

            if (Status == PSGStatus.ENABLED)
                return cmd;

            if (Status == PSGStatus.FINISHED)
            {
                cmd.Finished = true;
                return cmd;
            }

            HandleTerminal(inputs, ref cmd);

            if (cmd.Finished)
            {
                Finish();
                return cmd;
            }

            HandleThrottle(inputs, ref cmd);

            return cmd;
        }

        private bool IsThrustOn() => Status == PSGStatus.BURNING || IsTerminal();

        // under 1 mm/s² on this vessel
        private static bool NoThrust(double thrust, in GuidanceInputs inputs) => thrust < 1e-3 * inputs.Mass;

        // under 1 m/s left in the current stage by the stage stats; not empty while they have nothing for it
        private static bool StageEmpty(in GuidanceInputs inputs) => inputs.StageDeltaV < 1;

        private bool WillDoRCSButNotYet(in GuidanceInputs inputs) =>
            inputs.ForwardRCS && inputs.RCSGroup && Status != PSGStatus.TERMINAL_RCS && inputs.KSPStage == Solution!.TerminalKSPStage();

        private void HandleTerminal(in GuidanceInputs inputs, ref GuidanceCommand cmd)
        {
            if (Solution == null)
                return;

            if (!IsThrustOn())
            {
                _spentSince = double.NaN;
                return;
            }

            if (inputs.Grounded)
                return;

            // past the last stage
            if (inputs.KSPStage < Solution.TerminalKSPStage())
            {
                cmd.Finished = true;
                return;
            }

            if (TerminalStageSpent(inputs))
            {
                Print($"[GuidanceStateMachine] the last stage ran dry {Solution.Tgo(inputs.Met):F1} s before the end of the burn.");
                cmd.Finished = true;
                return;
            }

            // the end of a fixed burn time last stage, which never gets terminal guidance
            if (Solution.OptimizeKSPStage() != Solution.TerminalKSPStage() && inputs.KSPStage <= Solution.TerminalKSPStage() &&
                Solution.Tgo(inputs.Met) <= 0 && NoThrust(inputs.ThrustAvailable, inputs))
            {
                cmd.Finished = true;
                return;
            }

            if (Status == PSGStatus.TERMINAL_STAGING)
            {
                if (inputs.KSPStage == Solution.OptimizeKSPStage())
                    return;

                Status = PSGStatus.BURNING;
            }

            if (inputs.KSPStage != Solution.OptimizeKSPStage())
                return;

            if (Solution.TgoForKSPStage(inputs.Met, inputs.KSPStage) > 10)
                return;

            if (IsCoasting())
                return;

            // the first burn of a coast-during burn-coast-burn
            if (inputs.CoastDuring && inputs.KSPStage == Solution.CoastKSPStage() && Solution.WillCoast(inputs.Met))
                return;

            if (Status != PSGStatus.TERMINAL_RCS)
            {
                if (Status != PSGStatus.TERMINAL && inputs.ForwardRCS && !inputs.RCSGroup && inputs.KSPStage == Solution.TerminalKSPStage())
                    cmd.Messages |= GuidanceMessages.RCS_OFF;
                Status = PSGStatus.TERMINAL;
            }

            cmd.MinimumWarp = true;

            if (Status == PSGStatus.TERMINAL_RCS && !inputs.RCSGroup)
            {
                Print("[GuidanceStateMachine] terminating guidance due to manual deactivation of RCS.");
                TerminalDone(inputs, ref cmd);
                return;
            }

            // stopping one tick short is more accurate for rockets without RCS, but sometimes we overshoot with only one tick
            int ticks = WillDoRCSButNotYet(inputs) ? 2 : 1;

            double dt = ticks * inputs.Dt;
            V3 v1 = inputs.V + inputs.A * dt;
            V3 x1 = inputs.R + inputs.V * dt + 0.5 * inputs.A * dt * dt;

            if (!Solution.TerminalGuidanceSatisfied(x1, v1, inputs.Met, inputs.KSPStage))
                return;

            if (WillDoRCSButNotYet(inputs))
            {
                Print("[GuidanceStateMachine] transition to RCS terminal guidance.");
                cmd.Messages |= GuidanceMessages.RCS_TRIM;
                Status = PSGStatus.TERMINAL_RCS;
                cmd.RCSGroupOn = true;
            }
            else
            {
                Print("[GuidanceStateMachine] terminal guidance completed.");
                TerminalDone(inputs, ref cmd);
            }
        }

        // On the terminal stage with no thrust left to give, for a second
        private bool TerminalStageSpent(in GuidanceInputs inputs)
        {
            // a RealFuels engine that has not lit yet has no ThrustAvailable either, but still has its propellant: in a burn the
            // stage stats must say it is empty too
            bool spent = inputs.KSPStage == Solution!.TerminalKSPStage() &&
                (Status == PSGStatus.TERMINAL_RCS
                    ? NoThrust(inputs.RCSThrustUp, inputs)
                    : NoThrust(inputs.ThrustAvailable, inputs) && StageEmpty(inputs));

            if (!spent)
            {
                _spentSince = double.NaN;
                return false;
            }

            if (double.IsNaN(_spentSince))
                _spentSince = inputs.Time;

            return inputs.Time - _spentSince >= 1;
        }

        private void HandleThrottle(in GuidanceInputs inputs, ref GuidanceCommand cmd)
        {
            if (Solution == null)
                return;

            if (!inputs.AllowExecution)
                return;

            if (Status == PSGStatus.TERMINAL_RCS)
            {
                cmd.Throttle = GuidanceThrottle.RCS_FORWARD;
                return;
            }

            if (Status == PSGStatus.TERMINAL || Status == PSGStatus.TERMINAL_STAGING)
            {
                cmd.Throttle = GuidanceThrottle.SOLUTION;
                return;
            }

            int coastStage = Solution.CoastKSPStage();

            // hold autostaging at the coast's stage while the coast is ahead, else at the top of the rocket, so that the RCS trim
            // keeps a spent insertion stage
            if (coastStage >= 0 && inputs.KSPStage >= coastStage && Solution.WillCoast(inputs.Met))
                cmd.AutoStageLimit = coastStage;
            else
                cmd.AutoStageLimit = Solution.TerminalKSPStage();

            // terminal guidance may have shut down the burn before the coast slightly ahead of the (frozen) solution's planned
            // coast start, so stay in the coast until the solution's coast is behind us
            bool solutionCoast = Solution.Coast(inputs.Met);

            if (solutionCoast || (IsCoasting() && Solution.WillCoast(inputs.Met)))
            {
                if (!IsCoasting())
                    DoCoast(inputs, ref cmd);

                cmd.Throttle = solutionCoast && Solution.StageTimeLeft(inputs.Met) < inputs.UllageLeadTime
                    ? GuidanceThrottle.RCS_FORWARD
                    : GuidanceThrottle.OFF;

                // a relightable booster below a coast-before/dedicated coast is not dropped by autostaging
                if (inputs.KSPStage > coastStage)
                    cmd.Staging = GuidanceStaging.STAGE;
            }
            else
            {
                if (HasCoasted)
                    cmd.MarkCoastDone = true;

                cmd.Throttle = GuidanceThrottle.SOLUTION;

                Status = PSGStatus.BURNING;
            }
        }

        private void TerminalDone(in GuidanceInputs inputs, ref GuidanceCommand cmd)
        {
            // the burn just shut down is followed by the coast
            if (Solution!.CoastFollows(inputs.Met))
            {
                cmd.Throttle = GuidanceThrottle.OFF;
                DoCoast(inputs, ref cmd);
                return;
            }

            // more un-optimized upper stages to burn
            if (Solution.TerminalKSPStage() != inputs.KSPStage && Solution.OptimizeKSPStage() == inputs.KSPStage)
            {
                cmd.Throttle = GuidanceThrottle.OFF;
                cmd.Staging = GuidanceStaging.IMMEDIATE;
                Status = PSGStatus.TERMINAL_STAGING;
                return;
            }

            cmd.Finished = true;
        }

        private void DoCoast(in GuidanceInputs inputs, ref GuidanceCommand cmd)
        {
            StartCoast = inputs.Time;
            if (!inputs.RCSGroup)
            {
                cmd.RCSGroupOn = true;
                cmd.Messages |= GuidanceMessages.RCS_COAST;
            }

            Status = PSGStatus.COASTING;
            HasCoasted = true;
        }
    }
}
