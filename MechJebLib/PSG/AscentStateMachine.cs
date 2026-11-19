/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Text;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    /// <summary>What the PSG ascent autopilot is doing (MechJebModulePSGAutopilot.State, kOS STATE).</summary>
    public enum PSGState
    {
        PRELAUNCH,       // the countdown and the solar panels, before the autopilot flies
        VERTICAL_ASCENT, // on the pad awaiting liftoff, then straight up to the pitch start height
        PITCH_PROGRAM,   // pitching over at the pitch rate until it meets the guidance pitch
        BURNING,         // flying a burn of the Solution
        JETTISON,        // dropping a cut burn, before a fixed upper stack or after a COAST_AFTER coast: staged until its engines are gone
        COASTING,        // the Solution's coast
        TERMINAL,        // the end of a precise shutdown last stage: cut on the target's angular momentum
        TERMINAL_RCS,    // the RCS trim after the engines are cut
        FINISHED         // done, or disengaged
    }

    // what the transitions of a tick tell the rest of MechJeb
    [Flags]
    public enum AscentEvents
    {
        NONE = 0,

        // the coast started: PSGSimulation.CoastStarted, and the throttle off
        COAST_STARTED = 1,

        // the coast ended: PSGSimulation.CoastEnded and MarkCoastDone
        COAST_ENDED = 2,

        // terminal guidance started: PSGSimulation.TerminalStarted
        TERMINAL_STARTED = 4,

        // the ascent is over: PSGSimulation.GuidanceFinished, the throttle off, and the autopilot disengaged
        FINISHED = 8
    }

    // the screen messages about the RCS (PSGFeedback)
    [Flags]
    public enum AscentMessages { NONE = 0, RCS_COAST = 1, RCS_TRIM = 2, RCS_OFF = 4 }

    public enum AscentAttitude
    {
        // leave the attitude alone
        NONE,

        // the attitude controller lets go of the axes (on the pad)
        OFF,

        // straight up, rolled to Heading
        VERTICAL,

        // Pitch and Heading, AoA limited
        PITCH_HEADING,

        // the inertial Direction, AoA limited
        INERTIAL
    }

    public enum AscentThrottle
    {
        // leave the throttle alone
        NONE,

        // the guidance throttle, ThrottleValue
        GUIDED,

        // throttle off
        OFF,

        // throttle off and the RCS translating forward
        RCS_FORWARD
    }

    public enum AscentStaging
    {
        NONE,

        // stage with the staging controller's limit and delays (asked every tick while it is wanted)
        STAGE,

        // stage now
        IMMEDIATE
    }

    /// <summary>
    ///     What the vessel and the rest of MechJeb look like to the ascent this tick.  Thrust and mass are in any units where
    ///     thrust / mass is an acceleration (kN and t in KSP).  Positions, velocities and the acceleration are in the Solution's
    ///     frame (WorldToV3Rotated).
    /// </summary>
    public struct AscentInputs
    {
        // the Solution's clock (MET), which stays at zero until liftoff
        public double Met;

        // the universal time, for the timers
        public double Time;

        // the length of a physics tick
        public double Dt;

        // the vessel's current KSP stage
        public int CurrentStage;

        // on the ground (the same test as PSGSimulation's, so guidance and the optimizer agree about the pad)
        public bool Grounded;

        // off the pad and not landed
        public bool LiftedOff;

        public double AltitudeBottom;

        public bool HasAtmosphere;

        public double Mass;

        // the engines' thrust available (with the engines out and the RCS group on, the RCS thrust along the vessel's axis)
        public double ThrustAvailable;

        // the engines' minimum throttle, for the guidance throttle
        public double MinThrottle;

        // the RCS thrust available forward
        public double RCSThrustUp;

        // at least 10 % of the RCS thrust pushes the vessel forward (the RCS group off counts too)
        public bool ForwardRCS;

        // the RCS action group is on
        public bool RCSGroup;

        public V3 R;
        public V3 V;
        public V3 A;

        // the stage stats, top-down (index 0 is the top of the rocket), and the time of their run
        public List<FuelStats> VacStats;
        public List<FuelStats> AtmoStats;
        public double          VacT;

        // the persistentIds of the vessel's parts
        public HashSet<uint> PartIds;

        // the countdown's prelaunch is done (its result from the last tick that drove it)
        public bool PrelaunchDone;

        // a launch countdown is running (it stages at T-0): on the pad the vessel waits in PRELAUNCH until it ends
        public bool CountdownActive;

        public double PitchStartHeight;

        // degrees per second
        public double PitchRate;

        // how long before the end of the coast the RCS settles the propellant
        public double UllageLeadTime;

        // the KSP stage the plan's spin-up is done in, -1 without one
        public int SpinupStage;
    }

    /// <summary>
    ///     What the ascent asks the vessel and the rest of MechJeb to do this tick.  The transitions' part is applied first, then
    ///     the tick's.
    /// </summary>
    public struct AscentCommand
    {
        // set by the transitions
        public AscentEvents   Events;
        public AscentMessages Messages;

        // turn the RCS action group on
        public bool RCSGroupOn;

        // set by the state's tick: drive the countdown's prelaunch, which sets the next tick's PrelaunchDone
        public bool DrivePrelaunch;

        // the solar panels and the antennas for the ascent
        public bool DriveAscentParts;

        // the autostage limit to request, -1 to leave it
        public int AutostageLimit;

        public AscentAttitude Attitude;

        // degrees: Heading for VERTICAL and PITCH_HEADING, Pitch for PITCH_HEADING
        public double Pitch;
        public double Heading;

        // INERTIAL, in the Solution's frame
        public V3 Direction;

        public AscentThrottle Throttle;
        public double         ThrottleValue;

        public AscentStaging Staging;

        // drop out of time warp
        public bool MinimumWarp;

        // spin up for the coast's staging
        public bool Spinup;

        public static AscentCommand None => new AscentCommand { AutostageLimit = -1 };
    }

    /// <summary>How a burn of the Solution ends: see the table in the AscentStateMachine doc.</summary>
    internal enum BurnEnd
    {
        COAST,     // the last burn flown before the coast: cut at the coast's start
        JETTISON,  // cut at its planned end with propellant left, before a fixed upper stack, and staged off
        DEPLETION, // burns out, into its residuals, and autostaging drops it
        TERMINAL,  // the last stage, with a precise shutdown: cut on the target's angular momentum
        BURNOUT    // the last stage, without one: burns until it is out
    }

    /// <summary>
    ///     The state machine of the PSG ascent autopilot (MechJebModulePSGAutopilot).
    ///     Flies the Solution the PSGSimulation manager gives it from the PSG Optimizer.
    ///     Only supports a single midflight coast phase (burn-coast-burn).
    ///     Supports unguided stages and fixed burntime stages.
    ///     Will attempt to precisely shutdown before a coast or before a fixed burntime stack.
    ///     If the last stage can be shutdown will enter TERMINAL guidance to precisely hit an energy objective.
    ///     Will use RCS in TERMINAL_RCS to more precisely hit that energy objective.
    ///     The autopilot gathers the inputs, calls Step once per physics tick, and applies the command.
    /// </summary>
    /// <remarks>
    ///     How each burn of the Solution ends is decided in one place, BurnEndOf:
    ///
    ///     BurnEnd    the burn                             it ends                               then
    ///     ---------  -----------------------------------  ------------------------------------  ------------------------------------
    ///     COAST      the last one flown before the        at the coast's start, however much    COASTING (CoastDue), which does with
    ///                coast                                it has left, or burned out in its     its KSP stage what the coast's kind
    ///                                                     own KSP stage                         says (below)
    ///     JETTISON   one the optimizer ends with          at its planned end                    JETTISON stages until its engines
    ///                propellant left, before a burn in                                          leave the vessel (JettisonDue,
    ///                a lower KSP stage (a fixed upper                                           UpdateJettison); engines that burn on
    ///                stack)                                                                     in the next burn aren't cut (BurnsOn)
    ///     DEPLETION  any other before the last stage      burns out, into its residuals (a     autostaging drops it, and the stage
    ///                                                     cut it can't make burns on too)       stats show it gone (ReconcileStages)
    ///     TERMINAL   the last stage, with a precise       TERMINAL from TERMINAL_GUIDANCE_TIME  FINISHED, through TERMINAL_RCS when
    ///                shutdown                             before its end, cut a tick ahead of   the RCS group is on
    ///                                                     the target's angular momentum
    ///     BURNOUT    the last stage, without one          burns out                             FINISHED: gone from the stage stats,
    ///                                                                                           or spent (TerminalSpentNow)
    ///
    ///     The coast's kind says what happens to the KSP stage of the burn before it:
    ///
    ///     COAST_BEFORE, COAST_DEDICATED  COASTING stages it off into the coast's KSP stage (TickCoasting)
    ///     COAST_DURING                   the coast is in it, and its engines light again after the coast
    ///     COAST_AFTER                    the coast carries it, and JETTISON drops it as the coast ends (CarriedBurn)
    ///
    ///     Any burn can run dry before its end, which the stage stats show (ReconcileStages): guidance moves on to the next
    ///     one, or into the coast if it was the burn before it.  Burns before a coast only end at it from the last burn flown
    ///     before it, and only in its own KSP stage (OnStageOf).  The last stage can run dry short of the target, burning or
    ///     in terminal guidance (TerminalSpentNow).  The cuts on the clock are made on the tick nearest their time (Reached),
    ///     out of time warp (CutNear).
    ///
    ///     Guidance never flies a phase the Solution skips (Solution.Skip): one the optimizer shrank to nothing, or a pruned
    ///     stage after the last stage.  A coast it skips is no coast (Solution.CoastIndex), and the burns around it end as
    ///     though it weren't there (Solution.NextPhase): a coast-during shrunk to nothing, with the burn before it, is the one
    ///     burn after it.
    /// </remarks>
    public class AscentStateMachine
    {
        // terminal guidance starts this many seconds before the end of the last stage
        private const double TERMINAL_GUIDANCE_TIME = 10;

        // the steering holds still this many seconds before the end of a phase, through the staging or the burnout
        // (residuals may make it less)
        private const double STEERING_LOCK_TIME = 2;

        // a stage with less delta-v than this in m/s left has burned out
        private const double BURNED_OUT_DELTAV = 1;

        // time warp drops this many seconds before a cut on the clock (CutNear), as it does for terminal guidance
        private const double CUT_WARP_TIME = 10;

        // a last stage that hasn't lit this many seconds after the vessel staged into it, past its end, failed to ignite
        // (TerminalSpentNow): until then it may be waiting for its propellant to settle
        private const double IGNITION_TIMEOUT = 10;

        private const double MIN_PITCH_RATE = 0.1; // degrees per second

        // what guidance commands, for the UI and kOS
        public double Pitch           { get; private set; }
        public double Heading         { get; private set; }
        public double Tgo             { get; private set; }
        public double Vgo             { get; private set; }
        public double CurrentPhaseTgo { get; private set; }

        // The autopilot owns this and can change the FlightPlan to reconcile stages.
        public Solution? Solution { get; private set; }

        private PSGState _state = PSGState.PRELAUNCH;

        public PSGState State => _state;

        /// <summary>
        ///     kOS GUIDANCESTATUS: State in the status names of the guidance controller this autopilot replaced, which launch scripts
        ///     wait on.  ENABLED until there is a Solution and INITIALIZED while it holds on the pad with one (PRELAUNCH, through
        ///     the countdown), then BURNING from the ignition on the pad on, through the vertical ascent and the pitch program.
        ///     JETTISON was TERMINAL_STAGING.
        /// </summary>
        public string GuidanceStatus
        {
            get
            {
                switch (_state)
                {
                    case PSGState.PRELAUNCH:    return Solution == null ? "ENABLED" : "INITIALIZED";
                    case PSGState.BURNING:      return "BURNING";
                    case PSGState.JETTISON:     return "TERMINAL_STAGING";
                    case PSGState.COASTING:     return "COASTING";
                    case PSGState.TERMINAL:     return "TERMINAL";
                    case PSGState.TERMINAL_RCS: return "TERMINAL_RCS";
                    case PSGState.FINISHED:     return "FINISHED";
                    default:                    return Solution == null ? "ENABLED" : "BURNING";
                }
            }
        }

        public bool IsCoasting => _state == PSGState.COASTING;

        public bool IsTerminal => _state == PSGState.TERMINAL || _state == PSGState.TERMINAL_RCS;

        public bool IsFinished => _state == PSGState.FINISHED;

        // flying the Solution, after the pitch program
        public bool IsGuided => _state == PSGState.BURNING || _state == PSGState.JETTISON || IsCoasting || IsTerminal;

        /// <summary>Set when the coast starts, and kept after it: the Solution's burns before the coast are behind the vessel.</summary>
        public bool HasCoasted { get; private set; }

        /// <summary>The UT the coast started.</summary>
        public double CoastStartTime { get; private set; }

        // this tick's inputs, and the command being built
        private AscentInputs  _in;
        private AscentCommand _cmd = AscentCommand.None;

        // the flight plan indices of the stages guidance cut and saw jettisoned (their engines gone from the vessel), which the
        // stage stats may not show gone yet; the indices are those of every Solution, since the stage list is frozen in flight
        private readonly HashSet<int> _handedOff = new HashSet<int>();

        // the persistentIds of the engine parts a jettison has to see leave the vessel (JettisonEnginesAttached)
        private readonly HashSet<uint> _jettisonParts = new HashSet<uint>();

        // the KSP stage JETTISON last saw, to tell a stage it fired
        private int _jettisonStage;

        // the stage stats the Solution's flight plan was last reconciled with (NaN to reconcile again)
        private double _reconciledVacT = double.NaN;

        private double _pitchStartTime;

        // the last stage has had no thrust to give since then (NaN while it has), and _terminalSpent once that has lasted a second
        private double _spentSince = double.NaN;
        private bool   _terminalSpent;

        // the vessel's KSP stage, the UT it staged into it, and whether it has had thrust since (UpdateLit)
        private int    _litStage = int.MinValue;
        private double _stagedAt;
        private bool   _lit;

        /// <summary>
        ///     Engages the ascent: the guidance reset (ResetGuidance) and PRELAUNCH.
        /// </summary>
        public void Start()
        {
            ResetGuidance();
            _cmd = AscentCommand.None;
            TransitionTo(PSGState.PRELAUNCH);
        }

        /// <summary>
        ///     The state of an enabled autopilot with nothing to fly yet, in the state it is in.  UpdateDisplayVariables doesn't run
        ///     without a Solution, so the values it publishes have to be cleared here or the UI and kOS would keep showing the old ones.
        /// </summary>
        public void ResetGuidance()
        {
            Solution = null;
            HasCoasted = false;
            _handedOff.Clear();
            _reconciledVacT = double.NaN;
            _spentSince = double.NaN;
            _terminalSpent = false;
            Tgo = Vgo = CurrentPhaseTgo = 0;
            Pitch = Heading = 0;
            _inertial = V3.zero;
        }

        /// <summary>The autopilot was disengaged: no Solution, FINISHED (no transition).</summary>
        public void Disengage()
        {
            Solution = null;
            _state = PSGState.FINISHED;
        }

        /// <summary>Ends the ascent at once (FINISHED, with its transition), with this tick's command.</summary>
        public AscentCommand Finish()
        {
            _cmd = AscentCommand.None;
            TransitionTo(PSGState.FINISHED);
            return _cmd;
        }

        /// <summary>
        ///     Takes a DeNormalized() Solution to fly (guidance flies in SI units and MET seconds).  False if it was ignored: terminal
        ///     guidance flies to the end of the Solution it started with.
        /// </summary>
        public bool SetSolution(Solution solution)
        {
            Check.False(solution.Normalized, "guidance needs a DeNormalized() Solution");

            // PSGSimulation cancels the solve in progress when the coast starts and when it ends (CoastStarted, CoastEnded), so a
            // solution that arrives during the coast starts with it, and one that arrives after it has no coast
            CheckDebug.True(!IsCoasting || solution.CoastPhase(0));
            CheckDebug.True(!HasCoasted || IsCoasting || solution.CoastIndex() < 0);

            // terminal guidance flies to the end of the Solution it started with (PSGSimulation drops the solve in progress when
            // it starts, so this is only a solve that was done before)
            if (IsTerminal || IsFinished)
                return false;

            Solution = solution;

            // its flight plan is as old as its solve: reconcile it with the stage stats now
            _reconciledVacT = double.NaN;

            return true;
        }

        /// <summary>One physics tick: where the vessel is in the Solution, the state, and what to do.</summary>
        public AscentCommand Step(in AscentInputs inputs)
        {
            _in = inputs;
            _cmd = AscentCommand.None;

            ReconcileStages();

            UpdateSpent();

            UpdateJettison();

            MaybeRotateLaunchSite();

            UpdateState();

            UpdateSteering();

            UpdateDisplayVariables();

            TickState();

            return _cmd;
        }

        /*
         * The state machine
         */

        private PSGState DetermineState(PSGState desired)
        {
            if (desired == PSGState.FINISHED)
                return desired;

            // the vessel waits on the pad until the countdown is over and there is a Solution to fly, and waits again when a
            // countdown started on the pad drops the Solution (PSGSimulation.FlushSolution)
            if (desired == PSGState.PRELAUNCH && _in.PrelaunchDone && !LaunchHeld())
                desired = PSGState.VERTICAL_ASCENT;
            else if (desired == PSGState.VERTICAL_ASCENT && LaunchHeld())
                desired = PSGState.PRELAUNCH;
            if (desired == PSGState.VERTICAL_ASCENT && _in.LiftedOff && _in.AltitudeBottom > _in.PitchStartHeight)
                desired = PSGState.PITCH_PROGRAM;
            // not in the tick the pitch program is entered: its pitch starts at 90° from the time OnEnterPitchProgram records,
            // which a cascade from the vertical ascent would skip, handing over to guidance at once
            if (desired == PSGState.PITCH_PROGRAM && _state == PSGState.PITCH_PROGRAM && GuidanceTransition())
                desired = PSGState.BURNING;

            // the rest flies the Solution
            if (Solution == null || desired == PSGState.PRELAUNCH)
                return desired;

            // the coast can come before the pitch program has met guidance (in a vacuum it never runs)
            if ((desired == PSGState.VERTICAL_ASCENT || desired == PSGState.PITCH_PROGRAM || desired == PSGState.BURNING) && CoastDue())
                desired = PSGState.COASTING;

            // a coast lasts a tick at least, so that it is started and ended (a zero-length coast is a cut at its start)
            if (desired == PSGState.COASTING && _state == PSGState.COASTING && CoastOver())
                desired = PSGState.BURNING;

            // JETTISON drops a burn cut before a fixed upper stack from BURNING only (one that ends before the pitch program hands
            // over to guidance is dropped once it has), and the burn a COAST_AFTER coast carried as the coast ends, before the next
            // burn throttles up and relights it
            if (desired == PSGState.BURNING && (_state == PSGState.BURNING || _state == PSGState.COASTING) && JettisonIndex() >= 0)
                desired = PSGState.JETTISON;

            // its engines are gone (UpdateJettison has marked it done)
            if (desired == PSGState.JETTISON && JettisonIndex() < 0)
                desired = PSGState.BURNING;

            if (desired == PSGState.BURNING)
            {
                int p = CurrentPhase(false);
                if (p < 0)
                    desired = PSGState.FINISHED; // nothing left to burn
                else if (TerminalDue(p))
                    desired = PSGState.TERMINAL;
                else if (p == Solution.TerminalIndex() && _terminalSpent)
                    desired = PSGState.FINISHED; // the last stage burned out
            }

            if (desired == PSGState.TERMINAL)
            {
                bool rcs = WillDoRCS();
                // run dry, or the last stage is gone from the stage stats (burned out short of the target)
                if (_terminalSpent || CurrentPhase(false) < 0)
                    desired = PSGState.FINISHED;
                // stopping one tick short is more accurate for rockets without RCS, but sometimes we overshoot with only one tick
                else if (TerminalConditionsMet(rcs ? 2 : 1))
                    desired = rcs ? PSGState.TERMINAL_RCS : PSGState.FINISHED;
            }
            else if (desired == PSGState.TERMINAL_RCS)
            {
                if (!_in.RCSGroup || _terminalSpent || TerminalConditionsMet(1))
                    desired = PSGState.FINISHED;
            }

            return desired;
        }

        private void UpdateState()
        {
            PSGState desired = DetermineState(_state);
            if (desired != _state)
                TransitionTo(desired);
        }

        private void TransitionTo(PSGState next)
        {
            PSGState previous = _state;

            if (previous != next)
                Print($"[AscentStateMachine] {previous} -> {next} at MET {_in.Met:F2}");

            switch (previous)
            {
                case PSGState.COASTING:
                    if (next != PSGState.COASTING) OnExitCoasting();
                    break;
            }

            _state = next;

            // a solve in progress would move the target that terminal guidance flies to: once, as terminal guidance starts (in
            // TERMINAL, or TERMINAL_RCS when a cascade from BURNING skips TERMINAL)
            if (IsTerminal && !(previous == PSGState.TERMINAL || previous == PSGState.TERMINAL_RCS))
                _cmd.Events |= AscentEvents.TERMINAL_STARTED;

            switch (next)
            {
                case PSGState.PRELAUNCH:       OnEnterPrelaunch(); break;
                case PSGState.VERTICAL_ASCENT: OnEnterVerticalAscent(); break;
                case PSGState.PITCH_PROGRAM:   OnEnterPitchProgram(); break;
                case PSGState.BURNING:         OnEnterBurning(); break;
                case PSGState.JETTISON:        OnEnterJettison(); break;
                case PSGState.COASTING:        OnEnterCoasting(); break;
                case PSGState.TERMINAL:        OnEnterTerminal(); break;
                case PSGState.TERMINAL_RCS:    OnEnterTerminalRCS(); break;
                case PSGState.FINISHED:        OnEnterFinished(); break;
            }
        }

        private void TickState()
        {
            if (_state != PSGState.PRELAUNCH && _state != PSGState.FINISHED)
            {
                _cmd.DriveAscentParts = true;
                UpdateAutostageLimit();
            }

            switch (_state)
            {
                case PSGState.PRELAUNCH:       TickPrelaunch(); break;
                case PSGState.VERTICAL_ASCENT: TickVerticalAscent(); break;
                case PSGState.PITCH_PROGRAM:   TickPitchProgram(); break;
                case PSGState.BURNING:         TickBurning(); break;
                case PSGState.JETTISON:        TickJettison(); break;
                case PSGState.COASTING:        TickCoasting(); break;
                case PSGState.TERMINAL:        TickTerminal(); break;
                case PSGState.TERMINAL_RCS:    TickTerminalRCS(); break;
                case PSGState.FINISHED:        TickFinished(); break;
            }
        }

        // the autopilot keeps PrelaunchDone, which it clears as it engages and when the Solution is dropped on the pad
        private void OnEnterPrelaunch() { }

        // The countdown's prelaunch runs until it is done, then the vessel waits (LaunchHeld) with the throttle off and the axes
        // let go, as the prelaunch has them.  Engaged in flight, the prelaunch is skipped and nothing is cut: a throttle cut for
        // a tick could cost a RealFuels engine its ignition.
        private void TickPrelaunch()
        {
            _cmd.DrivePrelaunch = !_in.PrelaunchDone;

            if (!_in.Grounded)
                return;

            _cmd.Throttle = AscentThrottle.OFF;
            _cmd.Attitude = AscentAttitude.OFF;
        }

        // on the pad, the launch waits for the countdown, which stages at T-0, and for a Solution to fly
        private bool LaunchHeld() => _in.Grounded && (_in.CountdownActive || Solution == null);

        private void OnEnterVerticalAscent() { }

        private void TickVerticalAscent()
        {
            //during the vertical ascent we just thrust straight up at max throttle
            _cmd.Attitude = AscentAttitude.VERTICAL;
            _cmd.Heading = Heading;

            ThrottleGuided();
        }

        private void OnEnterPitchProgram() => _pitchStartTime = _in.Met;

        private void TickPitchProgram()
        {
            _cmd.Attitude = AscentAttitude.PITCH_HEADING;
            _cmd.Heading = Heading;
            _cmd.Pitch = PitchProgramPitch();

            ThrottleGuided();
        }

        private void OnEnterBurning() { }

        private void TickBurning()
        {
            _cmd.MinimumWarp = CutNear();
            SteerGuided();
            ThrottleGuided();
        }

        /// <summary>The KSP stage of the burn JETTISON drops, for the log and the status.</summary>
        public int JettisonStage()
        {
            int index = JettisonIndex();
            return index >= 0 ? Solution!.FlightPlan!.Stages[index].KSPStage : -1;
        }

        private void OnEnterJettison()
        {
            _jettisonStage = _in.CurrentStage;
            Print($"[AscentStateMachine] jettisoning the burn of KSP stage {JettisonStage()} from KSP stage {_in.CurrentStage}");
        }

        // Stages until the cut burn's engines are gone (UpdateJettison ends it): a fairing or an escape tower staged between the
        // burn and its decoupler is staged off too.  Autostaging won't drop an engine with propellant left.
        private void TickJettison()
        {
            // locked past the end of a burn cut before a fixed upper stack, and the next burn's after a COAST_AFTER coast
            SteerGuided();

            if (Solution == null)
            {
                _cmd.Throttle = AscentThrottle.OFF;
                return;
            }

            // The throttle is off, so the cut engines don't burn on until they are gone.  Engines of the burn that burn on in the
            // next one (a sustainer the boosters burn with) fly at the next burn's throttle instead: a cut would shut them down,
            // and a RealFuels engine might not light again.
            int index = JettisonIndex();
            if (index >= 0 && BurnsOn(index))
                ThrottleGuided(NextBurn());
            else
                _cmd.Throttle = AscentThrottle.OFF;

            // one stage at a time: a stage fired last tick gets this one for its parts to leave the vessel
            if (_in.CurrentStage != _jettisonStage)
            {
                _jettisonStage = _in.CurrentStage;
                return;
            }

            // never at or past the stage autostaging stops at (UpdateJettison gives up there): that would be the payload
            if (_in.CurrentStage <= AutostageFloor())
                return;

            // KSP refuses a stage during its staging cooldown, so this goes on until it is staged
            _cmd.Staging = AscentStaging.IMMEDIATE;
        }

        private void OnEnterCoasting()
        {
            // a solve started before the coast plans the burn before it, so it must never arrive now (and the throttle off)
            _cmd.Events |= AscentEvents.COAST_STARTED;

            CoastStartTime = _in.Time;
            HasCoasted = true;

            // force RCS on at the state transition
            if (!_in.RCSGroup)
            {
                _cmd.RCSGroupOn = true;
                _cmd.Messages |= AscentMessages.RCS_COAST;
            }
        }

        private void TickCoasting()
        {
            _cmd.MinimumWarp = CutNear();
            SteerGuided();

            _cmd.Throttle = AscentThrottle.OFF;

            if (Solution == null)
                return;

            int coast = CurrentPhase(true);
            if (coast < 0)
                return;

            // The coast starts in its KSP stage: the burn before a coast before the next burn (or a dedicated one) is staged off,
            // which autostaging won't do while it still has propellant (residuals, or a booster cut at the coast's start).  A
            // fairing or an escape tower jettisoned in the coast takes the vessel past that stage, which doesn't end the coast.
            // (A burn cut before a fixed upper stack has no such stage to stage to, and a COAST_AFTER coast is in the stage of
            // the burn it carries: JETTISON drops those.)
            if (_in.CurrentStage > StageOf(coast))
                _cmd.Staging = AscentStaging.STAGE;

            if (Solution.Tgo(_in.Met, coast) < _in.UllageLeadTime)
                _cmd.Throttle = AscentThrottle.RCS_FORWARD;

            // the plan's spin-up rides on its coast: the vessel spins up while it coasts, and staging waits for the spin
            if (_in.CurrentStage == _in.SpinupStage)
                _cmd.Spinup = true;
        }

        private void OnExitCoasting()
        {
            // a solve started during the coast still has it ahead, which would start it again
            _cmd.Events |= AscentEvents.COAST_ENDED;
        }

        private void OnEnterTerminal()
        {
            if (_in.ForwardRCS && !_in.RCSGroup)
                _cmd.Messages |= AscentMessages.RCS_OFF;
        }

        private void TickTerminal()
        {
            _cmd.MinimumWarp = true;
            SteerGuided();
            ThrottleGuided();
        }

        // only with the RCS group on (WillDoRCS)
        private void OnEnterTerminalRCS()
        {
            Print("[AscentStateMachine] transition to RCS terminal guidance.");
            _cmd.Messages |= AscentMessages.RCS_TRIM;
        }

        private void TickTerminalRCS()
        {
            _cmd.MinimumWarp = true;
            SteerGuided();
            _cmd.Throttle = AscentThrottle.RCS_FORWARD;
        }

        private void OnEnterFinished()
        {
            if (Solution != null)
            {
                Print($"[AscentStateMachine] finished {Solution.Tgo(_in.Met):F1} s from the end of the Solution.");
                // the stages guidance found left to fly: one done early points at the stage stats
                Print($"[AscentStateMachine] {Solution.FlightPlan}");
            }

            // a cascade from BURNING through TERMINAL skips TERMINAL_STARTED, and a solve in progress is of no use now; the throttle
            // off, and no user is left to keep the autopilot engaged
            _cmd.Events |= AscentEvents.FINISHED;
        }

        private void TickFinished() { }

        /*
         * Where the vessel is in the Solution
         */

        // Reconciles the Solution's flight plan with the stage stats, when they are new.  A stage it can't match on the engines it
        // burns is gone, and with it every stage below it (FlightPlan.ReconcileStages).  A stage with propellant left for under
        // BURNED_OUT_DELTAV has burned out: unlike the simulation, which stops planning a stage with less than MinDeltaV left,
        // guidance flies a stage until it is out.  Stale stats (right after staging) only ever show a stage as still there.
        private void ReconcileStages()
        {
            FlightPlan? plan = Solution?.FlightPlan;
            if (plan == null)
                return;

            List<FuelStats> vacStats = _in.VacStats;

            // a failed stage stats run publishes no rows, which would make every stage look gone
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (vacStats.Count == 0 || _in.VacT == _reconciledVacT)
                return;

            _reconciledVacT = _in.VacT;

            int done = DoneStages(plan);
            plan.ReconcileStages(vacStats, _in.AtmoStats, BURNED_OUT_DELTAV);

            // a stage done is what moves guidance on, so say what it was matched against: a stage gone from the stage stats
            // ahead of its time (one that vanishes from them, or burns out early) shows up here
            if (DoneStages(plan) != done)
                LogReconcile(plan, vacStats);
        }

        private static int DoneStages(FlightPlan plan)
        {
            int n = 0;
            foreach (FlightPlanStage stage in plan.Stages)
                if (stage.Done)
                    n++;
            return n;
        }

        private void LogReconcile(FlightPlan plan, List<FuelStats> vacStats)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[AscentStateMachine] stage done at MET {_in.Met:F2} in KSP stage {_in.CurrentStage}");
            for (int i = 0; i < plan.Stages.Count; i++)
                sb.AppendLine($"  plan {i}: {plan.Stages[i]} engines: {Signatures(plan.Stages[i].PlannedSignatures)}");
            for (int i = 0; i < vacStats.Count; i++)
            {
                FuelStats s = vacStats[i];
                sb.AppendLine($"  stats {i}: kspStage: {s.KSPStage} dV: {s.DeltaV:F1} dt: {s.DeltaTime:F1} thrust: {s.Thrust:F1} " +
                    $"mass: {s.StartMass:F3} -> {s.EndMass:F3} rcs: {s.RcsThrust:F2} engines: {Signatures(s.EngineSignatures)}");
            }

            Print(sb.ToString());
        }

        private static string Signatures(IReadOnlyList<EngineSignature> signatures)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < signatures.Count; i++)
                sb.Append(i > 0 ? " " : "").Append(signatures[i].PartPersistentID).Append(':').Append(signatures[i].ModuleIndex);
            return sb.Append(']').ToString();
        }

        // gone from the stage stats, or cut and seen jettisoned by guidance itself (UpdateJettison)
        private bool PhaseDone(int p)
        {
            int index = Solution!.FlightPlanIndex(p);
            FlightPlan? plan = Solution.FlightPlan;
            if (index < 0 || plan == null)
                return false;

            return plan.Stages[index].Done || _handedOff.Contains(index);
        }

        // the KSP stage of phase p now (the reconciled one)
        private int StageOf(int p)
        {
            Solution solution = Solution!;
            return solution.FlightPlan != null && solution.FlightPlanIndex(p) >= 0
                ? solution.FlightPlan.Stages[solution.FlightPlanIndex(p)].KSPStage
                : solution.KSPStage(p);
        }

        // The phase the vessel is flying: the coast while coasting, else the first burn of the Solution's burns before the coast
        // (before it) or after it (after it), up to the last stage, whose stage is still there, or -1 once none is.  Phases the
        // Solution skips are never flown, and a coast it skips is no coast (Solution.CoastIndex).
        private int CurrentPhase(bool coasting)
        {
            Solution solution = Solution!;
            int coast = solution.CoastIndex();

            if (coasting)
                return coast;

            int first = 0;
            int last = solution.TerminalIndex();

            if (coast >= 0)
            {
                if (HasCoasted)
                    first = coast + 1;
                else
                    last = coast - 1;
            }

            for (int p = first; p <= last; p++)
                if (!solution.Skip(p) && !solution.CoastPhase(p) && !PhaseDone(p))
                    return p;

            return -1;
        }

        // The coast starts as soon as the burns before it are gone, or in the last burn flown before it: at its planned start,
        // cutting that burn however much it has left (it would overshoot), or when it has burned out.  Not while the vessel is
        // still in an earlier burn (a booster burning on past its planned end), whose stage TickCoasting would stage off with its
        // propellant.
        private bool CoastDue()
        {
            if (HasCoasted || _in.Grounded)
                return false;

            int coast = Solution!.CoastIndex();
            if (coast < 0)
                return false;

            int p = CurrentPhase(false);
            if (p < 0)
                return true;

            if (BurnEndOf(p) != BurnEnd.COAST)
                return false;

            return Reached(Solution.StartTime(coast)) || (OnStageOf(p) && NoThrust(_in.ThrustAvailable) && CurrentStageEmpty());
        }

        private bool CoastOver()
        {
            int coast = CurrentPhase(true);
            return coast < 0 || Reached(Solution!.EndTime(coast));
        }

        // MET t is reached on the tick nearest it, or any after it: cutting on the first tick at or past it would be up to a tick
        // late
        private bool Reached(double t) => t - _in.Met < 0.5 * _in.Dt;

        // A cut on the clock is CUT_WARP_TIME away or less: the coast's start in the burn before it, its end in the coast, or the
        // end of a burn JETTISON drops.  Time warp would make it late, and the coast's ullage needs physics.
        private bool CutNear()
        {
            if (Solution == null)
                return false;

            double t;
            if (IsCoasting)
            {
                int coast = CurrentPhase(true);
                if (coast < 0)
                    return false;
                t = Solution.EndTime(coast);
            }
            else
            {
                int p = CurrentPhase(false);
                if (p >= 0 && BurnEndOf(p) == BurnEnd.COAST)
                    t = Solution.StartTime(Solution.CoastIndex());
                else if (JettisonCut(p))
                    t = Solution.EndTime(p);
                else
                    return false;
            }

            return t - _in.Met <= CUT_WARP_TIME;
        }

        // How burn phase p ends: see the table in the class doc.
        internal BurnEnd BurnEndOf(int p)
        {
            Solution solution = Solution!;

            if (p == solution.TerminalIndex())
                return solution.Phases[p].PreciseShutdown ? BurnEnd.TERMINAL : BurnEnd.BURNOUT;

            // the next phase guidance flies, past the ones the Solution skips
            int next = solution.NextPhase(p);
            int coast = solution.CoastIndex();

            if (!HasCoasted && coast >= 0 && next == coast)
                return BurnEnd.COAST;

            // a cut needs a burn in a lower KSP stage after it, for JETTISON to stage to, and a flight plan stage to mark done
            // (_handedOff); one that can't be made burns on
            if (solution.Phases[p].PropellantLeft && next >= 0 && !solution.CoastPhase(next) && solution.FlightPlanIndex(p) >= 0 &&
                StageOf(next) < StageOf(p))
                return BurnEnd.JETTISON;

            return BurnEnd.DEPLETION;
        }

        // terminal guidance flies the end of a last stage with a precise shutdown
        private bool TerminalDue(int p)
        {
            if (_in.Grounded || BurnEndOf(p) != BurnEnd.TERMINAL)
                return false;

            return Solution!.Tgo(_in.Met, p) <= TERMINAL_GUIDANCE_TIME;
        }

        // The target is the angular momentum at the end of the Solution, which the vessel's passes in the next tick (or two
        // with the RCS trim to come).
        // XXX: this is only applicable for orbit raising / ascents.
        private bool TerminalConditionsMet(int ticks)
        {
            // bit of a hack to predict velocity + position in the next tick or two
            // FIXME: what exactly does KSP do to integrate over timesteps?
            V3 a0 = _in.A;

            double dt = ticks * _in.Dt;
            V3 v1 = _in.V + a0 * dt;
            V3 x1 = _in.R + _in.V * dt + 0.5 * a0 * dt * dt;

            (V3 rT, V3 vT) = Solution!.TerminalStateVectors();

            double hf = V3.Cross(x1, v1).magnitude;
            double hT = V3.Cross(rT, vT).magnitude;

            if (hf <= hT)
                return false;

            Print($"[AscentStateMachine] terminal conditions met -- hf: {hf} hT: {hT}");
            return true;
        }

        // A burn the optimizer ends with propellant left, before another burn in a lower KSP stage (a fixed upper stack), is cut at
        // its planned end and jettisoned (JETTISON): autostaging won't drop an engine with propellant left.  The burn before a
        // coast is cut by the coast instead, and COASTING stages into the coast's KSP stage (TickCoasting).  Here there is no such
        // stage to stage to: the next burn's is its ignition, which is left to autostaging (ullage), so JETTISON watches the
        // burn's engines leave instead.  A COAST_AFTER coast is the exception, which carries the burn before it in the burn's own
        // KSP stage: JETTISON drops that burn as the coast ends (CarriedBurn).
        private bool JettisonDue(int p) => JettisonCut(p) && Reached(Solution!.EndTime(p));

        // phase p is a burn JETTISON drops at its end
        private bool JettisonCut(int p) => p >= 0 && !_in.Grounded && BurnEndOf(p) == BurnEnd.JETTISON;

        // After a COAST_AFTER coast, the flight plan index of the burn the coast carried to the next burn, or -1: throttling up for
        // the next burn would relight it, and autostaging won't drop it with propellant left (a cut, or residuals).  A Solution
        // made in the coast starts at the coast, so the burn is found in the flight plan, whose stage list is frozen.
        private int CarriedBurn()
        {
            FlightPlan? plan = Solution!.FlightPlan;
            if (!HasCoasted || plan == null)
                return -1;

            int index = plan.CoastAfterBurnIndex();
            return _handedOff.Contains(index) ? -1 : index;
        }

        // the flight plan index of the burn JETTISON drops if its engines are still on, or -1: a burn cut before a fixed upper stack
        // (JettisonDue), or the burn a COAST_AFTER coast carried (CarriedBurn)
        private int JettisonCandidate()
        {
            int p = CurrentPhase(false);
            return JettisonDue(p) ? Solution!.FlightPlanIndex(p) : CarriedBurn();
        }

        // the flight plan index of the burn JETTISON drops while its engines are on, or -1
        private int JettisonIndex()
        {
            int index = JettisonCandidate();
            return index >= 0 && JettisonEnginesAttached(index) ? index : -1;
        }

        // The engines of flight plan stage index that leave the vessel with it are still on it (FlightPlan.PartsJettisonedWith).
        // The stage stats would show them gone only after a fresh run, so this looks at the vessel's parts.  With none to look
        // for (all of them burn on) a stage fired is enough.
        private bool JettisonEnginesAttached(int index)
        {
            FlightPlan plan = Solution!.FlightPlan!;

            plan.PartsJettisonedWith(index, _jettisonParts);

            if (_jettisonParts.Count == 0)
                return _in.CurrentStage >= plan.Stages[index].KSPStage;

            foreach (uint id in _jettisonParts)
                if (_in.PartIds.Contains(id))
                    return true;

            return false;
        }

        // Engines of flight plan stage index burn on in a later burn: a sustainer the boosters burn with (or one fed by drop
        // tanks), which JETTISON must not cut.
        private bool BurnsOn(int index)
        {
            FlightPlan plan = Solution!.FlightPlan!;

            plan.PartsJettisonedWith(index, _jettisonParts);

            foreach (EngineSignature s in plan.Stages[index].PlannedSignatures)
                if (!_jettisonParts.Contains(s.PartPersistentID))
                    return true;

            return false;
        }

        // The phase of the burn after the one JETTISON drops: after a burn cut before a fixed upper stack, the next one; after a
        // COAST_AFTER coast the vessel is in it already.
        private int NextBurn()
        {
            int p = CurrentPhase(false);
            return JettisonDue(p) ? Solution!.NextPhase(p) : p;
        }

        // Before UpdateState, in BURNING and JETTISON: a burn JETTISON drops whose engines are gone is done, which moves
        // CurrentPhase on past a burn cut before a fixed upper stack (one already staged off by hand or by autostaging is never
        // cut).  At the stage autostaging stops at nothing more is staged, and the next burn flies with the engines still on.  Not
        // in COASTING, whose autostaging stops at the coast's own stage.
        private void UpdateJettison()
        {
            if (Solution == null || !(_state == PSGState.BURNING || _state == PSGState.JETTISON))
                return;

            int index = JettisonCandidate();
            if (index < 0)
                return;

            int stage = Solution.FlightPlan!.Stages[index].KSPStage;
            if (!JettisonEnginesAttached(index))
                Print($"[AscentStateMachine] jettisoned the burn of KSP stage {stage} at MET {_in.Met:F2} in KSP stage {_in.CurrentStage}");
            else if (_in.CurrentStage <= AutostageFloor())
                Print($"[AscentStateMachine] [WARNING] can't jettison the burn of KSP stage {stage}: its engines are still on in KSP " +
                    $"stage {_in.CurrentStage}, where autostaging stops");
            else
                return;

            _handedOff.Add(index);
        }

        // the last stage has no thrust left to give for a second: see TerminalSpentNow
        private void UpdateSpent()
        {
            UpdateLit();

            if (!TerminalSpentNow())
            {
                _spentSince = double.NaN;
                _terminalSpent = false;
                return;
            }

            if (double.IsNaN(_spentSince))
                _spentSince = _in.Time;

            _terminalSpent = _in.Time - _spentSince >= 1;
        }

        // Whether the vessel has had thrust since it staged into its KSP stage: a stage staged into has not lit until then.
        private void UpdateLit()
        {
            if (_in.CurrentStage != _litStage)
            {
                _litStage = _in.CurrentStage;
                _stagedAt = _in.Time;
                _lit = false;
            }

            if (!NoThrust(_in.ThrustAvailable))
                _lit = true;
        }

        // On the last stage with no thrust left to give: the engines are out (a flamed-out engine adds nothing to ThrustAvailable,
        // which also counts the RCS when the engines are out and the RCS group is on), or in the RCS trim the RCS is.  A RealFuels
        // engine that has not lit yet (MechJeb holding its ignition until the propellant settles) has no ThrustAvailable either,
        // but still has its propellant: in a burn the stage stats must say it is empty too, until the end of the burn.  Past it
        // no thrust is the end with propellant left too, once it has lit (a flameout or an engine failure), while an engine still
        // burning goes on into its residuals.  One that hasn't lit since the vessel staged into it may still be waiting for its
        // propellant to settle (a stage late on its staging delays reaches its end unlit), until IGNITION_TIMEOUT says it failed
        // to ignite.  A short of delta-v rocket runs dry here before the target, burning or in terminal guidance.
        private bool TerminalSpentNow()
        {
            if (Solution == null || _in.Grounded || !(_state == PSGState.BURNING || IsTerminal))
                return false;

            // the engines are cut in the RCS trim, and one with no relights may be gone from the stage stats
            if (_state == PSGState.TERMINAL_RCS)
                return NoThrust(_in.RCSThrustUp);

            int terminal = Solution.TerminalIndex();
            if (CurrentPhase(false) != terminal || !OnStageOf(terminal) || !NoThrust(_in.ThrustAvailable))
                return false;

            return CurrentStageEmpty() ||
                (_in.Met >= Solution.EndTime(terminal) && (_lit || _in.Time - _stagedAt >= IGNITION_TIMEOUT));
        }

        // The vessel is in phase p's KSP stage, or past it (a fairing staged off in the burn).  A booster below it that burned
        // out waits in its own stage, empty and with no thrust, until autostaging drops it (its pre-delay, or the RCS settling the
        // next stage's propellant): that is not the end of phase p.
        private bool OnStageOf(int p) => _in.CurrentStage <= StageOf(p);

        // Thrust in kN too small to matter: under 1 mm/s² on this vessel.  Not an exact zero: with the engines out and the RCS group
        // on, ThrustAvailable is the RCS thrust along the vessel's axis, which on a rocket with no RCS pushing forward is rounding
        // noise of 1e-7 kN rather than 0.
        private bool NoThrust(double thrust) => thrust < 1e-3 * _in.Mass;

        // under 1 m/s left in the current stage by the stage stats; not empty while they have nothing for it
        private bool CurrentStageEmpty()
        {
            List<FuelStats> vac = _in.VacStats;
            bool found = false;
            double dv = 0;
            for (int i = 0; i < vac.Count; i++)
                if (vac[i].KSPStage == _in.CurrentStage)
                {
                    found = true;
                    dv += vac[i].DeltaV;
                }

            return found && dv < 1;
        }

        private bool WillDoRCS() => _in.ForwardRCS && _in.RCSGroup;

        // Turns a Solution computed on the ground with the planet's rotation: to the launch site while the vessel waits on the pad,
        // then to the vessel after liftoff until the first Solution computed in flight replaces it (which is never turned).
        private void MaybeRotateLaunchSite() => Solution?.RotateToLaunchSite(_in.R);

        // Updates Tgo/Vgo/CurrentPhaseTgo/Pitch/Heading -- "display" only and could be moved.  It runs after UpdateState, so the
        // tick that ends the ascent is already FINISHED here: the values stay those of the last guided tick, not the Solution
        // read past its end (the RCS trim runs past it), which the UI and kOS would go on showing after the autopilot disengages.
        private void UpdateDisplayVariables()
        {
            if (Solution == null || IsFinished)
                return;

            // the Solution's clock is MET, which stays at zero until liftoff, so on the ground this flies its first point
            int p = CurrentPhase(IsCoasting);

            // frozen from the engine cut on: the RCS trim runs past the end of the Solution
            if (_state != PSGState.TERMINAL_RCS)
            {
                Tgo = Solution.Tgo(_in.Met);
                Vgo = Solution.Vgo(_in.Met);
                if (p >= 0)
                    CurrentPhaseTgo = Solution.Tgo(_in.Met, p);
            }

            // no burn left to fly: Pitch and Heading keep their last values
            if (p < 0)
                return;

            (Pitch, Heading) = GuidancePitchHeading(p);
        }

        // the pitch and heading of phase p's guidance now, in degrees
        private (double pitch, double heading) GuidancePitchHeading(int p)
        {
            (V3 inertial, _) = Solution!.InertialGuidance(p, _in.Met, _in.MinThrottle);

            (double pitch, double heading) = Astro.ECIToPitchHeading(_in.R, inertial);

            return (Rad2Deg(pitch), Rad2Deg(heading));
        }

        // The autostaging stops at the coast's KSP stage while the coast is ahead or under way, and at the last stage's after it.
        // Stopping at the top of the rocket matters: when terminal guidance cuts the engines for the RCS trim, autostaging would
        // stage off a spent engine with no relights, and with it the RCS the insertion stage still needs.
        private void UpdateAutostageLimit()
        {
            if (Solution == null)
                return;

            _cmd.AutostageLimit = AutostageFloor();
        }

        // the KSP stage autostaging stops at (see UpdateAutostageLimit)
        private int AutostageFloor()
        {
            int coast = Solution!.CoastIndex();
            return coast >= 0 && (IsCoasting || !HasCoasted) ? StageOf(coast) : StageOf(Solution!.TerminalIndex());
        }

        /// <summary>The pitch program's pitch now, in degrees.</summary>
        public double PitchProgramPitch()
        {
            double dt = _in.Met - _pitchStartTime;
            // a rate of 0 or less would hold the pitch at 90 and never hand over to guidance in an atmosphere
            double theta = dt * Math.Max(_in.PitchRate, MIN_PITCH_RATE);
            // not below the horizon while waiting for guidance (no solution yet, or none at all)
            return Math.Max(90 - theta, 0);
        }

        // The pitch program hands over to guidance where it meets the guidance pitch, and at once in a vacuum.  This tick's guidance
        // pitch: Pitch is last tick's (UpdateDisplayVariables runs after UpdateState), or another Solution's.
        private bool GuidanceTransition()
        {
            if (Solution == null)
                return false;

            if (!_in.HasAtmosphere)
                return true;

            int p = CurrentPhase(false);
            return PitchProgramPitch() <= (p >= 0 ? GuidancePitchHeading(p).pitch : Pitch);
        }

        private V3 _inertial = V3.zero; // the guided steering (UpdateSteering), which SteerGuided flies

        // The guided steering, kept from the pad on (not only in the guided states) so the lock always has a direction to hold: it
        // follows the Solution until STEERING_LOCK_TIME before the end of the phase, then holds still through the staging or the
        // burnout.  With none yet (a Solution that arrives, or an engage that starts, inside the lock) it takes the Solution's.
        private void UpdateSteering()
        {
            if (Solution == null)
                return;

            int p = CurrentPhase(IsCoasting);
            if (p >= 0 && (_in.Grounded || _inertial == V3.zero || Solution.Tgo(_in.Met, p) > STEERING_LOCK_TIME))
                (_inertial, _) = Solution.InertialGuidance(p, _in.Met, _in.MinThrottle);
        }

        private void SteerGuided()
        {
            if (Solution == null)
                return;

            _cmd.Attitude = AscentAttitude.INERTIAL;
            _cmd.Direction = _inertial;
        }

        private void ThrottleGuided()
        {
            if (Solution == null)
                return;

            ThrottleGuided(CurrentPhase(false));
        }

        // the guided throttle of phase p
        private void ThrottleGuided(int p)
        {
            if (Solution == null)
                return;

            _cmd.Throttle = AscentThrottle.GUIDED;

            // full throttle with no burn left to fly (only the vertical ascent and the pitch program get here without one: the
            // guided states finish)
            if (p < 0)
            {
                _cmd.ThrottleValue = 1.0;
                return;
            }

            (_, double throttle) = Solution.InertialGuidance(p, _in.Met, _in.MinThrottle);

            _cmd.ThrottleValue = throttle;
        }
    }
}
