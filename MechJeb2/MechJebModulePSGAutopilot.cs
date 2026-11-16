/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Text;
using KSP.Localization;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Utils;
using MechJebLibBindings;
using UnityEngine;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MuMech
{
    /// <summary>What the PSG ascent autopilot is doing (MechJebModulePSGAutopilot.State, kOS GUIDANCESTATUS).</summary>
    public enum PSGState
    {
        PRELAUNCH,       // the countdown and the solar panels, before the autopilot flies
        VERTICAL_ASCENT, // on the pad awaiting liftoff, then straight up to the pitch start height
        PITCH_PROGRAM,   // pitching over at the pitch rate until it meets the guidance pitch
        BURNING,         // flying a burn of the Solution
        COASTING,        // the Solution's coast
        TERMINAL,        // the end of a precise shutdown last stage: cut on the target's angular momentum
        TERMINAL_RCS,    // the RCS trim after the engines are cut
        FINISHED         // done, or disengaged
    }

    /// <summary>
    ///     Autopilot for PSG Launches.
    ///     Flies the Solution the PSGSimulation manager gives it from the PSG Optimizer.
    ///     Only supports a single midflight coast phase (burn-coast-burn).
    ///     Supports unguided stages and fixed burntime stages.
    ///     Will attempt to precisely shutdown before a coast or before a fixed burntime stack.
    ///     If the last stage can be shutdown will enter TERMINAL guidance is precisely hit an energy objective.
    ///     Will use RCS in TERMINAL_RCS to more precisely hit that energy objective.
    ///     Residuals may cause stages to shutdown earlier than planned.
    ///     Allows boosters to overburn into residuals, if not before a coast/fixed stages or at the end of the burn.
    /// </summary>
    public class MechJebModulePSGAutopilot : ComputerModule, IAscentAutopilot
    {
        public MechJebModulePSGAutopilot(MechJebCore core) : base(core) { }

        // the settings below were saved under the guidance controller this module replaced
        public override string FormerName => "MechJebModuleGuidanceController";

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble UllageLeadTime = 20;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool ShouldDrawTrajectory = true;

        public AscentStatus Status { get; } = new AscentStatus();

        UserPool IAscentAutopilot.Users => Users;

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        private MechJebModuleLaunchCountdown _countdown => Core.LaunchCountdown;

        // terminal guidance starts this many seconds before the end of the last stage
        private const double TERMINAL_GUIDANCE_TIME = 10;

        // the steering holds still this many seconds before the end of a phase, through the staging or the burnout
        // (residuals may make it less)
        private const double STEERING_LOCK_TIME = 2;

        // a stage with less delta-v than this in m/s left has burned out
        private const double BURNED_OUT_DELTAV = 1;

        private const double MIN_PITCH_RATE = 0.1; // degrees per second

        // what guidance commands, for the UI and kOS
        public double Pitch;
        public double Heading;
        public Vector3d Inertial = Vector3d.zero; // inertial in rotating coordinates
        public double Tgo;
        public double Vgo;
        public double CurrentPhaseTgo;

        private V3 _inertial = V3.zero; // inertial in right-handed non-rotating
        private double _throttle;

        // The PSGAutopilot owns this and can change the FlightPlan to reconcile stages.
        public Solution? Solution;

        // The last ground solution on the pad, for the PSG Solution window
        public Solution? PadSolution;

        // what the vessel flew since the autopilot was engaged, for the PSG Solution window
        public readonly PSGFlownTrack FlownTrack = new PSGFlownTrack();

        private PSGState _state = PSGState.PRELAUNCH;

        public PSGState State => _state;

        public bool HasSolution => Solution != null;

        public bool IsCoasting => _state == PSGState.COASTING;

        public bool IsTerminal => _state == PSGState.TERMINAL || _state == PSGState.TERMINAL_RCS;

        public bool IsFinished => _state == PSGState.FINISHED;

        // flying the Solution, after the pitch program
        private bool IsGuided => _state == PSGState.BURNING || IsCoasting || IsTerminal;

        /// <summary>Set when the coast starts, and kept after it: the Solution's burns before the coast are behind the vessel.</summary>
        public bool HasCoasted { get; private set; }

        /// <summary>The UT the coast started.</summary>
        public double CoastStartTime { get; private set; }

        // what guidance does with RCS (PSGFeedback), said once at each change
        private ScreenMessage _rcsCoastMessage = null!, _rcsTrimMessage = null!, _rcsOffMessage = null!;

        // the flight plan indices of the stages guidance cut and staged off itself, which the stage stats may not show gone yet;
        // the indices are those of every Solution, since the stage list is frozen in flight
        private readonly HashSet<int> _handedOff = new HashSet<int>();

        // the stage stats the Solution's flight plan was last reconciled with (NaN to reconcile again)
        private double _reconciledVacT = double.NaN;

        // the countdown holds the throttle
        private bool _allowThrottle;

        // Countdown.DrivePrelaunch is done
        private bool _prelaunchDone;

        private double _pitchStartTime;

        // the last stage has had no thrust to give since then (NaN while it has), and _terminalSpent once that has lasted a second
        private double _spentSince = double.NaN;
        private bool _terminalSpent;

        private string? _awaitingComputing, _awaitingProgress, _awaitingProgressFrom;
        private string? _lockStatus, _lockStatusId;

        public override void OnStart(PartModule.StartState state)
        {
            _rcsCoastMessage = PSGFeedback.RcsCoastMessage();
            _rcsTrimMessage  = PSGFeedback.RcsTrimMessage();
            _rcsOffMessage   = PSGFeedback.RcsOffMessage();

            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
                Core.AddToPostDrawQueue(DrawTrajectory);
        }

        protected override void OnModuleEnabled()
        {
            Debug.Log("[MechJebModulePSGAutopilot] enabled");

            _ascentSettings.ApplyOwnValues(this, true);

            // Each engage plans from the current settings and stages: the flight plan is made again, so the plan of an earlier
            // engage, with the stages it marked done, is not flown again. The simulation, which freezes the plan, is disabled here.
            Core.PSGSimulation.FlightPlan = null;

            ResetGuidance();
            FlownTrack.Clear();

            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
            Core.Spinup.Users.Add(this);
            if (_ascentSettings.Autostage) Core.Staging.Users.Add(this);
            _countdown.Users.Add(this);

            Status.Set(Localizer.Format("#MechJeb_Ascent_status1")); //"Pre Launch"

            TransitionTo(PSGState.PRELAUNCH);
        }

        protected override void OnModuleDisabled()
        {
            Debug.Log("[MechJebModulePSGAutopilot] disabled");

            Core.Attitude.AttitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.Users.Remove(this);
            Core.Staging.AutoStageLimitRemove(this);
            Core.Spinup.Users.Remove(this);
            _countdown.Users.Remove(this);
            _ascentSettings.ReleaseOwnValues(this);

            _countdown.Abort();

            Core.PSGSimulation.Users.Remove(this);

            Solution = null;
            _state = PSGState.FINISHED;

            Status.Set(Localizer.Format("#MechJeb_Ascent_status2")); //"Off"
        }

        // The state of an enabled autopilot with nothing to fly yet. UpdateGuidance doesn't run without a Solution, so the values
        // it publishes have to be cleared here or the UI and kOS would keep showing the old ones.
        private void ResetGuidance()
        {
            Solution = null;
            PadSolution = null;
            _allowThrottle = false;
            _prelaunchDone = false;
            HasCoasted = false;
            _handedOff.Clear();
            _reconciledVacT = double.NaN;
            _spentSince = double.NaN;
            _terminalSpent = false;
            Tgo = Vgo = CurrentPhaseTgo = 0;
            Pitch = Heading = 0;
            _inertial = V3.zero;
            Inertial = Vector3d.zero;
            _throttle = 0;
        }

        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("[MechJebModulePSGAutopilot] [BUG]: PSG enabled in non-flight mode.  How does this happen?");
                TransitionTo(PSGState.FINISHED);
                return;
            }

            UpdateFlightPlan();

            UpdateCountdownGate();

            ReconcileStages();

            UpdateSpent();

            UpdateState();

            UpdateGuidance();

            TickState();

            if (Solution != null && !IsGrounded())
                FlownTrack.Record(Core);

            // the steering doesn't reach the vessel while another mod has taken MechJeb's controls (DeactivateControl: RP-1 sets
            // it when the avionics on board can't support the vessel, RemoteTech without a connection)
            if (IsGuided && Core.DeactivateControl)
                SetLockStatus();
        }

        // the guidance needs a flight plan, which nothing else makes unless the Ascent window is open
        private void UpdateFlightPlan()
        {
            if (Core.PSGSimulation.FlightPlan != null)
                return;

            Core.StageStats.RequestUpdate();
            Core.PSGSimulation.UpdateLogicalPlan();
        }

        // the optimizer starts late in a countdown (or at once without one), and the throttle waits for the countdown to end
        private void UpdateCountdownGate()
        {
            if (_countdown.Active)
            {
                if (_countdown.TMinus <= Math.Max(_ascentSettings.WarpCountDown, 0))
                    Core.PSGSimulation.Users.Add(this);
                else
                    Core.PSGSimulation.Users.Remove(this);
                _allowThrottle = false;
            }
            else
            {
                Core.PSGSimulation.Users.Add(this);
                _allowThrottle = true;
            }
        }

        /*
         * The state machine
         */

        private PSGState DetermineState(PSGState desired)
        {
            if (desired == PSGState.FINISHED)
                return desired;

            if (desired == PSGState.PRELAUNCH && _prelaunchDone)
                desired = PSGState.VERTICAL_ASCENT;
            if (desired == PSGState.VERTICAL_ASCENT && LiftedOff() && VesselState.AltitudeBottom > _ascentSettings.PitchStartHeight)
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
                if (!Vessel.ActionGroups[KSPActionGroup.RCS] || _terminalSpent || TerminalConditionsMet(1))
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
                Debug.Log($"[MechJebModulePSGAutopilot] {previous} -> {next} at MET {VesselState.MET:F2}");

            switch (previous)
            {
                case PSGState.COASTING:
                    if (next != PSGState.COASTING) OnExitCoasting();
                    break;
            }

            _state = next;

            switch (next)
            {
                case PSGState.PRELAUNCH:       OnEnterPrelaunch(); break;
                case PSGState.VERTICAL_ASCENT: OnEnterVerticalAscent(); break;
                case PSGState.PITCH_PROGRAM:   OnEnterPitchProgram(); break;
                case PSGState.BURNING:         OnEnterBurning(); break;
                case PSGState.COASTING:        OnEnterCoasting(); break;
                case PSGState.TERMINAL:        OnEnterTerminal(); break;
                case PSGState.TERMINAL_RCS:    OnEnterTerminalRCS(); break;
                case PSGState.FINISHED:        OnEnterFinished(); break;
            }
        }

        private void TickState()
        {
            _ascentSettings.LimitingAoA = false;

            if (_state != PSGState.PRELAUNCH && _state != PSGState.FINISHED)
            {
                if (_countdown.HoldForLiftoff(Status))
                    return;

                Core.Solarpanel.DriveForAscent();
                Core.AntennaControl.DriveForAscent();
                UpdateAutostageLimit();
            }

            switch (_state)
            {
                case PSGState.PRELAUNCH:       TickPrelaunch(); break;
                case PSGState.VERTICAL_ASCENT: TickVerticalAscent(); break;
                case PSGState.PITCH_PROGRAM:   TickPitchProgram(); break;
                case PSGState.BURNING:         TickBurning(); break;
                case PSGState.COASTING:        TickCoasting(); break;
                case PSGState.TERMINAL:        TickTerminal(); break;
                case PSGState.TERMINAL_RCS:    TickTerminalRCS(); break;
                case PSGState.FINISHED:        TickFinished(); break;
            }
        }

        private void OnEnterPrelaunch() => _prelaunchDone = false;

        private void TickPrelaunch() => _prelaunchDone = _countdown.DrivePrelaunch(Status);

        private void OnEnterVerticalAscent() { }

        private void TickVerticalAscent()
        {
            //during the vertical ascent we just thrust straight up at max throttle
            Core.Attitude.AttitudeToVerticalLaunch(Heading,
                _ascentSettings.ForceRoll && VesselState.AltitudeBottom > _ascentSettings.RollAltitude, _ascentSettings.VerticalRoll, this);

            ThrottleGuided();

            if (!LiftedOff())
            {
                // the first solve can take minutes on a slow machine: say so, or the pad looks stuck
                if (Solution == null)
                    Status.Set(AwaitingComputing()); //"Awaiting liftoff: computing the ascent…"
                else
                    Status.Set(Localizer.Format("#MechJeb_Ascent_status12")); //"Awaiting liftoff"
                return;
            }

            double dh = _ascentSettings.PitchStartHeight - VesselState.AltitudeBottom;
            Status.Set($"Vertical ascent {dh:F2}m to go");
        }

        private void OnEnterPitchProgram() => _pitchStartTime = VesselState.MET;

        private void TickPitchProgram()
        {
            double pitch = PitchProgramPitch();

            // without a solution the guidance pitch is 0, so "N° to guidance" would be the pitch itself: say what is missing instead,
            // since the pitch program goes on down to the horizon
            if (Solution == null)
                Status.Set($"No guidance yet, pitch program at {pitch:F0}°. " +
                    PSGFeedback.OptimizerStateText(Core.PSGSimulation.State, Core.PSGSimulation.StateSeconds, true, false,
                        Core.PSGSimulation.LastFailureMessage != null ? Core.PSGSimulation.Staleness : 0),
                    AscentStatusLevel.WARNING);
            else
                Status.Set(Localizer.Format("#MechJeb_Ascent_status15", $"{pitch - Pitch:F}")); //Pitch program <<1>>° to guidance

            Core.Attitude.LimitedAttitudeTo(Heading, pitch, _ascentSettings.ForceRoll, _ascentSettings.TurnRoll, ApplyAoALimiter, this);

            ThrottleGuided();
        }

        private void OnEnterBurning() { }

        private void TickBurning()
        {
            SteerGuided();

            // a burn the optimizer ended with propellant left is cut and staged off, then the next one flies
            if (HandOff())
                return;

            ThrottleGuided();
        }

        private void OnEnterCoasting()
        {
            // a solve started before the coast plans the burn before it, so it must never arrive now
            Core.PSGSimulation.CoastStarted();

            CoastStartTime = VesselState.Time;
            HasCoasted = true;

            ThrustOff();

            // force RCS on at the state transition
            if (!Vessel.ActionGroups[KSPActionGroup.RCS])
            {
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                ScreenMessages.PostScreenMessage(_rcsCoastMessage);
            }
        }

        private void TickCoasting()
        {
            SteerGuided();

            ThrustOff();

            int coast = CurrentPhase(true);
            if (coast < 0)
                return;

            // The coast starts in its KSP stage: the burn before a coast before the next burn (or a dedicated one) is staged off,
            // which autostaging won't do while it still has propellant (residuals, or a booster cut at the coast's start).  A
            // fairing or an escape tower jettisoned in the coast takes the vessel past that stage, which doesn't end the coast.
            if (Vessel.currentStage > StageOf(coast))
                Core.Staging.Stage();

            if (Solution!.Tgo(VesselState.MET, coast) < UllageLeadTime)
                RCSOn();

            // the plan's spin-up rides on its coast: the vessel spins up while it coasts, and staging waits for the spin
            if (Vessel.currentStage == Core.PSGSimulation.PlannedSpinupStage())
            {
                Core.Spinup.AssertStart();
                Core.Spinup.RollAngularVelocity = _ascentSettings.SpinupAngularVelocity;
            }
        }

        private void OnExitCoasting()
        {
            // a solve started during the coast still has it ahead, which would start it again
            Core.PSGSimulation.CoastEnded();
            Core.PSGSimulation.MarkCoastDone();
        }

        private void OnEnterTerminal()
        {
            // a solve in progress would move the target that terminal guidance flies to
            Core.PSGSimulation.TerminalStarted();

            if (HasForwardRCS() && !Vessel.ActionGroups[KSPActionGroup.RCS])
                ScreenMessages.PostScreenMessage(_rcsOffMessage);
        }

        private void TickTerminal()
        {
            Core.Warp.MinimumWarp();
            SteerGuided();
            ThrottleGuided();
        }

        private void OnEnterTerminalRCS()
        {
            Debug.Log("[MechJebModulePSGAutopilot] transition to RCS terminal guidance.");
            ScreenMessages.PostScreenMessage(_rcsTrimMessage);
            if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
        }

        private void TickTerminalRCS()
        {
            Core.Warp.MinimumWarp();
            SteerGuided();
            RCSOn();
        }

        private void OnEnterFinished()
        {
            if (Solution != null)
            {
                Debug.Log($"[MechJebModulePSGAutopilot] finished {Solution.Tgo(VesselState.MET):F1} s from the end of the Solution.");
                // the stages guidance found left to fly: one done early points at the stage stats
                Debug.Log($"[MechJebModulePSGAutopilot] {Solution.FlightPlan}");
            }

            ThrustOff();
            Users.Clear(); // no user is left to keep it engaged
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

            List<FuelStats> vacStats = Core.StageStats.VacStats;

            // a failed stage stats run publishes no rows, which would make every stage look gone
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (vacStats.Count == 0 || Core.StageStats.VacT == _reconciledVacT)
                return;

            _reconciledVacT = Core.StageStats.VacT;

            int done = DoneStages(plan);
            plan.ReconcileStages(vacStats, Core.StageStats.AtmoStats, BURNED_OUT_DELTAV);

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
            sb.AppendLine($"[MechJebModulePSGAutopilot] stage done at MET {VesselState.MET:F2} in KSP stage {Vessel.currentStage}");
            for (int i = 0; i < plan.Stages.Count; i++)
                sb.AppendLine($"  plan {i}: {plan.Stages[i]} engines: {Signatures(plan.Stages[i].PlannedSignatures)}");
            for (int i = 0; i < vacStats.Count; i++)
            {
                FuelStats s = vacStats[i];
                sb.AppendLine($"  stats {i}: kspStage: {s.KSPStage} dV: {s.DeltaV:F1} dt: {s.DeltaTime:F1} thrust: {s.Thrust:F1} " +
                    $"mass: {s.StartMass:F3} -> {s.EndMass:F3} rcs: {s.RcsThrust:F2} engines: {Signatures(s.EngineSignatures)}");
            }

            Debug.Log(sb.ToString());
        }

        private static string Signatures(IReadOnlyList<EngineSignature> signatures)
        {
            var sb = new StringBuilder("[");
            for (int i = 0; i < signatures.Count; i++)
                sb.Append(i > 0 ? " " : "").Append(signatures[i].PartPersistentID).Append(':').Append(signatures[i].ModuleIndex);
            return sb.Append(']').ToString();
        }

        // gone from the stage stats, or cut and staged off by guidance itself
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

        // The coast, if it comes before the last stage: a coast after it is pruned with the stages after the last stage, which
        // the optimizer gives no burn time.
        private int LiveCoastIndex()
        {
            int coast = Solution!.CoastIndex();
            return coast >= 0 && coast < Solution.TerminalIndex() ? coast : -1;
        }

        // The phase the vessel is flying: the coast while coasting, else the first burn of the Solution's burns before the coast
        // (before it) or after it (after it), up to the last stage, whose stage is still there, or -1 once none is.
        private int CurrentPhase(bool coasting)
        {
            Solution solution = Solution!;
            int coast = LiveCoastIndex();

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
                if (!solution.CoastPhase(p) && !PhaseDone(p))
                    return p;

            return -1;
        }

        // The coast starts at its planned start, cutting the burn before it however much it has left (it would overshoot), or
        // as soon as the burns before it are gone or the last of them has burned out.
        private bool CoastDue()
        {
            if (HasCoasted || IsGrounded())
                return false;

            int coast = LiveCoastIndex();
            if (coast < 0)
                return false;

            if (VesselState.MET >= Solution!.StartTime(coast))
                return true;

            int p = CurrentPhase(false);
            return p < 0 || (p == coast - 1 && NoThrust(VesselState.ThrustAvailable) && CurrentStageEmpty());
        }

        private bool CoastOver()
        {
            int coast = CurrentPhase(true);
            return coast < 0 || VesselState.MET >= Solution!.EndTime(coast);
        }

        // terminal guidance flies the end of a last stage with a precise shutdown
        private bool TerminalDue(int p)
        {
            Solution solution = Solution!;
            if (p != solution.TerminalIndex() || !solution.Phases[p].PreciseShutdown || IsGrounded())
                return false;

            return solution.Tgo(VesselState.MET, p) <= TERMINAL_GUIDANCE_TIME;
        }

        // The target is the angular momentum at the end of the Solution, which the vessel's passes in the next tick (or two
        // with the RCS trim to come).
        // XXX: this is only applicable for orbit raising / ascents.
        private bool TerminalConditionsMet(int ticks)
        {
            // bit of a hack to predict velocity + position in the next tick or two
            // FIXME: what exactly does KSP do to integrate over timesteps?
            Vector3d a0 = Vessel.acceleration_immediate;

            double dt = ticks * TimeWarp.fixedDeltaTime;
            Vector3d v1 = VesselState.OrbitalVelocity + a0 * dt;
            Vector3d x1 = VesselState.OrbitalPosition + VesselState.OrbitalVelocity * dt + 0.5 * a0 * dt * dt;

            (V3 rT, V3 vT) = Solution!.TerminalStateVectors();

            double hf = V3.Cross(x1.WorldToV3Rotated(), v1.WorldToV3Rotated()).magnitude;
            double hT = V3.Cross(rT, vT).magnitude;

            if (hf <= hT)
                return false;

            Debug.Log($"[MechJebModulePSGAutopilot] terminal conditions met -- hf: {hf} hT: {hT}");
            return true;
        }

        // A burn the optimizer ends with propellant left, before another burn in a lower KSP stage, is cut at its planned end
        // and staged off: autostaging won't drop an engine with propellant left.  True while it is being staged off.
        private bool HandOff()
        {
            Solution solution = Solution!;
            int p = CurrentPhase(false);

            // the burn before the coast is cut by the coast, and the last stage by terminal guidance or burning out
            if (p < 0 || p >= solution.TerminalIndex() || solution.CoastPhase(p + 1) || !solution.Phases[p].PropellantLeft)
                return false;

            if (VesselState.MET < solution.EndTime(p) || StageOf(p + 1) >= StageOf(p))
                return false;

            // staged: the stage stats will show it gone, and until then it is done
            if (Vessel.currentStage < StageOf(p))
            {
                _handedOff.Add(solution.FlightPlanIndex(p));
                return false;
            }

            ThrustOff();

            // KSP refuses a stage during its staging cooldown, so this goes on until it is staged
            Core.Staging.ImmediateStage();

            return true;
        }

        // the last stage has no thrust left to give for a second: see TerminalSpentNow
        private void UpdateSpent()
        {
            if (!TerminalSpentNow())
            {
                _spentSince = double.NaN;
                _terminalSpent = false;
                return;
            }

            if (double.IsNaN(_spentSince))
                _spentSince = VesselState.Time;

            _terminalSpent = VesselState.Time - _spentSince >= 1;
        }

        // On the last stage with no thrust left to give: the engines are out (a flamed-out engine adds nothing to ThrustAvailable,
        // which also counts the RCS when the engines are out and the RCS group is on), or in the RCS trim the RCS is.  A RealFuels
        // engine that has not lit yet (MechJeb holding its ignition until the propellant settles) has no ThrustAvailable either,
        // but still has its propellant: in a burn the stage stats must say it is empty too.  A short of delta-v rocket runs dry
        // here before the target, burning or in terminal guidance.
        private bool TerminalSpentNow()
        {
            if (Solution == null || IsGrounded() || !(_state == PSGState.BURNING || IsTerminal))
                return false;

            // the engines are cut in the RCS trim, and one with no relights may be gone from the stage stats
            if (_state == PSGState.TERMINAL_RCS)
                return NoThrust(VesselState.RCSThrustAvailable.Up);

            if (CurrentPhase(false) != Solution.TerminalIndex())
                return false;

            return NoThrust(VesselState.ThrustAvailable) && CurrentStageEmpty();
        }

        // Thrust in kN too small to matter: under 1 mm/s² on this vessel.  Not an exact zero: with the engines out and the RCS group
        // on, ThrustAvailable is the RCS thrust along the vessel's axis, which on a rocket with no RCS pushing forward is rounding
        // noise of 1e-7 kN rather than 0.
        private bool NoThrust(double thrust) => thrust < 1e-3 * VesselState.Mass;

        // under 1 m/s left in the current stage by the stage stats; not empty while they have nothing for it
        private bool CurrentStageEmpty()
        {
            List<FuelStats> vac = Core.StageStats.VacStats;
            bool found = false;
            double dv = 0;
            for (int i = 0; i < vac.Count; i++)
                if (vac[i].KSPStage == Vessel.currentStage)
                {
                    found = true;
                    dv += vac[i].DeltaV;
                }

            return found && dv < 1;
        }

        // We might have wonky transforms and have a tiny bit of fore RCS, so require at least 10% of the max RCS thrust to be in the
        // pointy direction (which should be "up" / y-axis per KSP/Unity semantics). VesselState counts the RCS thrust with the group
        // off too: an ascent flown with RCS off ends without the RCS trim, as it always has.
        private bool HasForwardRCS() =>
            Vessel.hasEnabledRCSModules() && VesselState.RCSThrustAvailable.Up > 0.1 * VesselState.RCSThrustAvailable.MaxMagnitude();

        private bool WillDoRCS() => HasForwardRCS() && Vessel.ActionGroups[KSPActionGroup.RCS];

        private bool IsGrounded() =>
            Vessel.situation == Vessel.Situations.LANDED ||
            Vessel.situation == Vessel.Situations.PRELAUNCH ||
            Vessel.situation == Vessel.Situations.SPLASHED;

        private bool LiftedOff() => Vessel.LiftedOff() && !Vessel.Landed;

        /*
         * Guidance
         */

        private void UpdateGuidance()
        {
            if (Solution == null)
                return;

            V3 r0 = VesselState.OrbitalPosition.WorldToV3Rotated();

            // the launch site turns with the planet while we wait on the ground, so turn the Solution with it (only a Solution
            // computed on the ground is turned)
            Solution.RotateToLaunchSite(r0);

            // the Solution's clock is MET, which stays at zero until liftoff, so on the ground this flies its first point
            int p = CurrentPhase(IsCoasting);

            if (_state != PSGState.TERMINAL_RCS) // don't update vgo/tgo if we're in RCS
            {
                Tgo = Solution.Tgo(VesselState.MET);
                Vgo = Solution.Vgo(VesselState.MET);
                if (p >= 0)
                    CurrentPhaseTgo = Solution.Tgo(VesselState.MET, p);
            }

            // lock the inertial heading near the end of the phase for any staging event or terminal burnout
            if (p >= 0 && (IsGrounded() || Solution.Tgo(VesselState.MET, p) > STEERING_LOCK_TIME))
                (_inertial, _throttle) = Solution.InertialGuidance(p, VesselState.MET, VesselState.MinThrottle);

            (double pitch, double heading) = Astro.ECIToPitchHeading(r0, _inertial);

            Inertial = _inertial.V3ToWorldRotated();
            Pitch = Rad2Deg(pitch);
            Heading = Rad2Deg(heading);
        }

        // The autostaging stops at the coast's KSP stage while the coast is ahead or under way, and at the last stage's after it.
        // Stopping at the top of the rocket matters: when terminal guidance cuts the engines for the RCS trim, autostaging would
        // stage off a spent engine with no relights, and with it the RCS the insertion stage still needs.
        private void UpdateAutostageLimit()
        {
            if (Solution == null || !_allowThrottle)
                return;

            int coast = LiveCoastIndex();
            int stage = coast >= 0 && (IsCoasting || !HasCoasted) ? StageOf(coast) : StageOf(Solution.TerminalIndex());

            Core.Staging.AutoStageLimitRequest(stage, this);
        }

        private double PitchProgramPitch()
        {
            double dt = VesselState.MET - _pitchStartTime;
            // a rate of 0 or less would hold the pitch at 90 and never hand over to guidance in an atmosphere
            double theta = dt * Math.Max(_ascentSettings.PitchRate, MIN_PITCH_RATE);
            // not below the horizon while waiting for guidance (no solution yet, or none at all)
            return Math.Max(90 - theta, 0);
        }

        // the pitch program hands over to guidance where it meets the guidance pitch, and at once in a vacuum
        private bool GuidanceTransition()
        {
            if (Solution == null)
                return false;

            return !MainBody.atmosphere || PitchProgramPitch() <= Pitch;
        }

        private void SteerGuided()
        {
            double ang = Vector3d.Angle(Inertial, VesselState.Forward);
            AscentStatusLevel severity = ang > 5 ? AscentStatusLevel.ERROR : ang > 1 ? AscentStatusLevel.WARNING : AscentStatusLevel.NORMAL;
            Status.Set($"Stable Guidance: {ang:F}° deviation", severity);
            Core.Attitude.LimitedAttitudeTo(Inertial, _ascentSettings.ForceRoll, _ascentSettings.TurnRoll, ApplyAoALimiter, this);
        }

        private void ThrottleGuided()
        {
            if (Solution == null || !_allowThrottle)
                return;

            // RO/RF: if we are spooling up engines, then flush the PID integrators to prevent windup
            if (VesselState.ThrustCurrent < VesselState.ThrustMinimum * 0.98)
                Core.Attitude.Controller.Reset();

            Core.Thrust.TargetThrottle = _throttle > 0 ? (float)_throttle : 1.0f;
        }

        private void RCSOn()
        {
            Core.Thrust.ThrustOff();
            Vessel.ctrlState.Z = -1.0F;
        }

        private void ThrustOff() => Core.Thrust.ThrustOff();

        private Vector3d ApplyAoALimiter(Vector3d desiredThrustVector)
        {
            double lim = Core.PSGSimulation.QAlphaLimit;
            if (lim <= 0)
                return desiredThrustVector;

            _ascentSettings.LimitingAoA = VesselState.DynamicPressure * Vector3.Angle(VesselState.SurfaceVelocity, desiredThrustVector) * UtilMath.Deg2Rad > lim;
            if (_ascentSettings.LimitingAoA)
            {
                _ascentSettings.CurrentMaxAoA = lim / VesselState.DynamicPressure * UtilMath.Rad2Deg;
                desiredThrustVector = MathExtensions.RotateTowards(VesselState.SurfaceVelocity, desiredThrustVector, (float)(_ascentSettings.CurrentMaxAoA * UtilMath.Deg2Rad), 1).normalized;
            }

            return desiredThrustVector;
        }

        /*
         * Solutions
         */

        // guidance flies in SI units and MET seconds, so this takes a DeNormalized() Solution
        public void SetSolution(Solution solution)
        {
            Check.False(solution.Normalized, "guidance needs a DeNormalized() Solution");

            // PSGSimulation cancels the solve in progress when the coast starts and when it ends (CoastStarted, CoastEnded), so a
            // solution that arrives during the coast starts with it, and one that arrives after it has no coast
            CheckDebug.True(!IsCoasting || solution.CoastPhase(0));
            CheckDebug.True(!HasCoasted || IsCoasting || solution.CoastIndex() < 0);

            // terminal guidance flies to the end of the Solution it started with (PSGSimulation drops the solve in progress when
            // it starts, so this is only a solve that was done before)
            if (IsTerminal || IsFinished)
                return;

            Solution = solution;
            if (solution.T0 <= 0)
                PadSolution = solution;

            // its flight plan is as old as its solve: reconcile it with the stage stats now
            _reconciledVacT = double.NaN;
        }

        /// <summary>
        ///     Drops a Solution that no longer applies and goes back to waiting for one. Only for use on the ground, the timed launch
        ///     buttons and kOS suffixes require a landed vessel. Does nothing while disabled, so a countdown started before engaging
        ///     leaves the throttle alone.
        /// </summary>
        public void FlushSolution()
        {
            if (!Enabled)
                return;

            bool prelaunchDone = _prelaunchDone;
            ResetGuidance();
            _prelaunchDone = prelaunchDone;

            // engaging without a countdown leaves the throttle up on the pad, and once the autopilot has left prelaunch nothing
            // else lowers it before T-0
            ThrustOff();
        }

        /*
         * Status and drawing
         */

        // "Awaiting liftoff: computing the ascent, step 2 of 5 (1m 12s)", rebuilt only when the progress text changes
        private string AwaitingComputing()
        {
            string? progress = Core.PSGSimulation.ProgressShort;
            if (progress == null)
                return _awaitingComputing ??= Localizer.Format("#MechJeb_Ascent_status12") + ": computing the ascent…"; //"Awaiting liftoff"

            if (!ReferenceEquals(progress, _awaitingProgressFrom) || _awaitingProgress == null)
            {
                _awaitingProgressFrom = progress;
                _awaitingProgress     = Localizer.Format("#MechJeb_Ascent_status12") + ": " + progress;
            }

            return _awaitingProgress;
        }

        // says why MechJeb can't fly the vessel while another mod has taken its controls; the string is rebuilt only when the lock
        // named changes
        private void SetLockStatus()
        {
            string? lockId = PSGFeedback.SteeringLockId();
            if (lockId != _lockStatusId || _lockStatus == null)
            {
                _lockStatusId = lockId;
                _lockStatus   = PSGFeedback.LockStatus(lockId);
            }

            Status.Set(_lockStatus, AscentStatusLevel.ERROR);
        }

        private readonly List<Vector3d> _trajectory = new List<Vector3d>();
        private readonly Orbit _finalOrbit = new Orbit();

        private void DrawTrajectory()
        {
            if (Solution == null)
                return;

            if (!Enabled)
                return;

            if (!MapView.MapIsEnabled)
                return;

            if (!ShouldDrawTrajectory)
                return;

            const int SEGMENTS = 50;

            _trajectory.Clear();
            double dt = (Solution.Tf - Solution.T0) / SEGMENTS;

            for (int i = 0; i <= SEGMENTS; i++)
            {
                double t = Solution.T0 + dt * i;

                _trajectory.Add(Solution.R(t).V3ToWorldRotated() + MainBody.position);
            }

            GLUtils.DrawPath(MainBody, _trajectory, Color.red, MapView.MapIsEnabled);

            Vector3d rf = Planetarium.fetch.rotation * Solution.R(Solution.Tf).ToVector3d().xzy;
            Vector3d vf = Planetarium.fetch.rotation * Solution.V(Solution.Tf).ToVector3d().xzy;

            // Tf is an MET, the orbit wants the UT of the end of the trajectory
            _finalOrbit.UpdateFromStateVectors(rf.xzy, vf.xzy, MainBody, VesselState.Time + Solution.Tgo(VesselState.MET));

            GLUtils.DrawOrbit(_finalOrbit, Color.yellow);
        }
    }
}
