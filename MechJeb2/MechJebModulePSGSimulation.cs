/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Utils;
using MechJebLibBindings.PSG;
using UnityEngine;
using static MechJebLib.Utils.Statics;
using static System.Math;

#nullable enable

namespace MuMech
{
    /// <summary>What the PSG optimizer is doing, or why it is not solving (MechJebModulePSGSimulation.State).</summary>
    public enum OptimizerState
    {
        READY,           // between two solves
        SOLVING,         // a solve is running
        NO_PLAN,         // no flight plan, or nothing left in it to plan
        STAGE_ENDING,    // the current stage is about to end: the solution is frozen through staging
        AFTER_STAGING,   // a short wait after staging
        AFTER_PHASE,     // the same wait after a phase ended without staging (the start or end of a coast during a stage)
        ON_PAD,          // either wait before liftoff: the stage not lit yet has no phase in the solution, so its time to go reads 0
        VERTICAL_ASCENT, // after liftoff, the vertical ascent to the pitch start height: no re-solve until guidance takes over
        PITCH_PROGRAM,   // the pitch program, pitching over to the guidance pitch of the solution it has: no re-solve until guidance takes over
        COAST_PAUSE,     // a long coast above the atmosphere, once a solve started there has converged: resumes 20 s before the burn
        TERMINAL,        // terminal guidance flies the end of the burn on the frozen solution
        UNGUIDED_ONLY,   // only unguided stages are left, which the optimizer can't steer
        FINISHED,        // guidance is done
        RETRYING         // the problem could not be built (a setting PSG refuses): tried again every second
    }

    /// <summary>
    ///     This class isolates a bunch of glue between MJ and the PSG optimizer that I don't
    ///     yet understand how to write correctly.
    /// </summary>
    public class MechJebModulePSGSimulation : ComputerModule
    {
        [Persistent(pass = (int)Pass.TYPE)]
        public PersistedLogicalPlan? LogicalPlan;

        // set once the plan is edited in the PSG Stages window: the saved LogicalPlan is then kept. Without it the plan is the
        // default one (PlanEditor.DefaultPlan), made again from the stage stats every frame, so it follows the vessel's changes
        [Persistent(pass = (int)Pass.TYPE)]
        public bool PlanEdited;

        /// <summary>
        ///     The plan is set on the ground and locked from the moment the ascent is engaged or the vessel leaves the ground,
        ///     until it is back on the ground: staging changes under the autopilot in flight (fairings, an escape tower, engine
        ///     failures, stages burning out a little early or late), and the flight plan made on the ground is what it reconciles
        ///     with them.
        /// </summary>
        public bool PlanLocked => Enabled || (Vessel != null && Vessel.VesselOffGround());

        // This is generated from the LogicalPlan, and is not user-editable while the module is enabled
        public FlightPlan? FlightPlan;

        /// <summary>The plan's coast, from the FlightPlan: its kind and the KSP stage the vessel is in during it, or (BURN, -1) without one.</summary>
        public (StageType Kind, int KSPStage) PlannedCoast()
        {
            if (FlightPlan == null) return (StageType.BURN, -1);
            foreach (FlightPlanStage s in FlightPlan.Stages)
                if (s.StageType != StageType.BURN)
                    return (s.StageType, s.KSPStage);
            return (StageType.BURN, -1);
        }

        /// <summary>
        ///     The KSP stage the vessel spins up in, during the plan's coast (a guided coast with Spinup set), or -1: the spin-up
        ///     controller holds the staging until the vessel spins.
        /// </summary>
        public int PlannedSpinupStage()
        {
            if (LogicalPlan == null) return -1;
            foreach (LogicalPlanStage s in LogicalPlan.Plan.Stages)
                if (s.StageType != StageType.BURN)
                    return s.Spinup && !s.Unguided ? PlannedCoast().KSPStage : -1;
            return -1;
        }

        /// <summary>The lowest KSP stage the plan burns (its last burn), or 0 without a plan.</summary>
        public int PlannedLastKSPStage()
        {
            if (FlightPlan == null || FlightPlan.Stages.Count == 0) return 0;
            int last = int.MaxValue;
            foreach (FlightPlanStage s in FlightPlan.Stages)
                if (s.StageType == StageType.BURN && s.KSPStage < last)
                    last = s.KSPStage;
            return last == int.MaxValue ? 0 : last;
        }

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult PeriapsisAltitude = new EditableDoubleMult(145000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult ApoapsisAltitude = new EditableDoubleMult(0, 1000);

        /// <summary>The target inclination: one for both ascent types, AscentSettings.DesiredInclination.</summary>
        public EditableDouble Inc => Core.AscentSettings.DesiredInclination;

        // the PSG inclination saved before it was shared, under its old name: read once (AscentSettings.ShareInclinationIfNeeded)
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL), name = "Inc")]
        internal readonly EditableDouble SavedPsgInclination = new EditableDouble(double.NaN);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble LAN = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult ArgP = new EditableDoubleMult(0, Math.PI / 180);

        // with all fixed burn times this is the burnout altitude and is always used (AttachAltFlag is ignored)
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult BurnoutAltitude = new EditableDoubleMult(145000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult FPA = new EditableDoubleMult(0, Math.PI / 180);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool BurnoutAltitudeFlag = false;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool ArgPFlag = false;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble Cd = new EditableDouble(0.5);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble Aref = new EditableDouble(0.0);

        /// <summary>
        ///     Cd and Aref are worked out from the vessel (AeroEstimate) and written into their fields before each solve, instead of
        ///     the values typed in.
        /// </summary>
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool AutoAero = true;

        /// <summary>The vessel's drag area as AutoAero fills Cd and Aref with it, worked out by UpdateAutoAero.</summary>
        public readonly PSGAeroEstimate AeroEstimate = new PSGAeroEstimate();

        private float _nextAeroEstimate;

        /// <summary>
        ///     With AutoAero, works out the vessel's drag area (at most twice a second) and fills Cd and Aref with it. It is worked
        ///     out on the ground and stays once the vessel is airborne. The PSG Settings window calls it too, so the fields show the
        ///     estimate before the ascent is engaged.
        /// </summary>
        public void UpdateAutoAero()
        {
            if (!AutoAero || Time.unscaledTime < _nextAeroEstimate)
                return;
            _nextAeroEstimate = Time.unscaledTime + 0.5f;

            Solution? plan = Core.PSGAutopilot.Solution ?? Core.PSGAutopilot.PadSolution;
            if (!AeroEstimate.Update(Vessel, plan))
                return;

            Cd.Val = Round(AeroEstimate.Cd, 3);
            Aref.Val = Round(AeroEstimate.Aref, 3);
        }

        // A setup saved with an Aref typed in and no AutoAero value keeps that Aref and its Cd.
        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            bool autoSaved = (type != null && type.HasValue(nameof(AutoAero))) || (global != null && global.HasValue(nameof(AutoAero)));
            if (!autoSaved && Aref > 0)
                AutoAero = false;
        }

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble LimitQa = new EditableDouble(2000);

        /// <summary>
        ///     The Qα limit the optimizer plans with and the autopilot flies to, in Pa-rad: 0 (no limit) for a setting of 0 or
        ///     less, and at most 10000.
        /// </summary>
        public double QAlphaLimit => LimitQa > 0 ? Min(LimitQa, 10000) : 0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MaxCoast = 450;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MinCoast = 0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MinDeltaV = 40;

        // deliberately not in the UI or in global, edit the ship file with an editor (do not set to appx zero)
        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableDouble OptimizerPauseTime = 5;

        // deliberately not in the UI or in global, edit the ship file with an editor (do not set to appx zero)
        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableDouble PreStageTime = 10;

        private double _blockOptimizerUntilTime;

        // why _blockOptimizerUntilTime is set: the second between two solves is routine, the others are pauses worth showing
        private OptimizerState _blockReason = OptimizerState.READY;

        /// <summary>What the optimizer is doing, or why it is not solving, as of the last physics frame.</summary>
        public OptimizerState State { get; private set; } = OptimizerState.READY;

        /// <summary>
        ///     For STAGE_ENDING, the seconds left in the stage; for AFTER_STAGING and RETRYING, the seconds left in the pause; for
        ///     COAST_PAUSE, the seconds until it ends (20 s before the burn).
        /// </summary>
        public double StateSeconds { get; private set; }

        /// <summary>
        ///     Whether this coast is solved above the atmosphere: a solve started in the coast above the atmosphere, once the
        ///     shutdown and the staging into the coast had settled, has converged and reached guidance.  The optimizer then pauses
        ///     until 20 s before the burn (COAST_PAUSE, StateSeconds to go), the stretch guidance can warp through.  Cleared when
        ///     the coast starts or ends, and by staging, whose dispersions the next solve then picks up.
        /// </summary>
        public bool VacuumCoastSolved { get; private set; }

        // whether the running job was started in the coast above the atmosphere, staged into the coast and after the last staging
        private bool _vacuumCoastJob;

        public int SuccessfulConverges;
        public int LastLmStatus;
        public int MaxLmIterations;
        public int LastLmIterations;

        public string? LastFailureMessage;

        /// <summary>
        ///     How much delta-v, in m/s, the last converged solution needs beyond what the rocket has (Solution.DeltaVShortfall), or
        ///     zero.  Guidance still flies that solution, so the rocket runs out of propellant before the target.
        /// </summary>
        public double DeltaVShortfall;

        /// <summary>
        ///     For a rocket with only fixed burn times and a burnout altitude above the atmosphere, the periapsis altitude in m of the
        ///     orbit the last converged solution ends on when that periapsis is under the atmosphere (or the surface on an airless body),
        ///     else NaN.  Such a rocket maximizes its
        ///     energy instead of aiming at an orbit, so running short shows up here and not as a DeltaVShortfall.
        /// </summary>
        public double SuborbitalPeriapsis = double.NaN;

        public double Staleness;
        public double LastInfeasibility;

        /// <summary>
        ///     While a first solve runs, where it is, for the Ascent window: "computing the ascent, step 2 of 5, adding the unguided
        ///     stages: 1m 12s, iteration 143, error 3.1e-02". Null otherwise, and for in-flight re-solves.
        /// </summary>
        public string? ProgressDetail;

        /// <summary>The same, short, for the autopilot status: "computing the ascent, step 2 of 5 (1m 12s)".</summary>
        public string? ProgressShort;

        private int _progressPass, _progressSeconds, _progressIterations;

        // rebuilt when the step, the whole second or the iteration count changes, so about once a second or per iteration
        private void UpdateProgressText(SolveProgress p)
        {
            if (p.Pass == 0)
            {
                ProgressDetail = ProgressShort = null;
                return;
            }

            int seconds = (int)p.PassSeconds;
            if (ProgressDetail != null && p.Pass == _progressPass && seconds == _progressSeconds && p.Iterations == _progressIterations)
                return;

            _progressPass       = p.Pass;
            _progressSeconds    = seconds;
            _progressIterations = p.Iterations;

            (ProgressShort, ProgressDetail) = PSGFeedback.ProgressText(p);
        }
        private double _lastTime;

        public MechJebModulePSGSimulation(MechJebCore core) : base(core) { }

        private Ascent? _ascent;

        // The solver's own copy of the last converged solution, in normalized units to warm start the next solve; guidance flies
        // a separate DeNormalized() copy in SI units. The worker thread reads it while a job runs, so the main thread only touches
        // it between jobs.
        private Solution? _solution;

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        protected override void OnModuleEnabled()
        {
            Debug.Log("Enabling PSG Simulation");
            ResetCounters();
            _ascent = null;
            _solution = null;
            // FIXME: should throw and disable on no logicalplan or flightplan
        }

        protected override void OnModuleDisabled()
        {
            Debug.Log("Disabling PSG Simulation");
            CancelJob();
            // a cancelled job can still be reading _solution until it stops, so the next enable must not hand it to another job
            _solution = null;
        }

        public override void OnStart(PartModule.StartState state) => GameEvents.onStageActivate.Add(HandleStageEvent);

        public override void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(HandleStageEvent);
            CancelJob();
        }

        // Stops a running solve without blocking the main thread: the job throws at its next constraint evaluation, and since
        // _ascent is dropped nothing reads its result.
        private void CancelJob()
        {
            _ascent?.Cancel();
            _ascent = null;
            _vacuumCoastJob = false;
        }

        private void ResetCounters()
        {
            SuccessfulConverges = LastLmStatus = MaxLmIterations = 0;
            LastLmStatus = LastLmIterations = 0;
            Staleness = LastInfeasibility = _lastTime = 0;
            LastFailureMessage = null;
            SetState(OptimizerState.READY);
            DeltaVShortfall = 0;
            SuborbitalPeriapsis = double.NaN;
            VacuumCoastSolved = false;
        }

        private void CheckReach(Solution solution)
        {
            DeltaVShortfall = solution.DeltaVShortfall();

            SuborbitalPeriapsis = double.NaN;
            if (_optimizeStageFlag)
                return;

            // the target of such a rocket is its burnout altitude: one inside the atmosphere is a suborbital flight on purpose (a
            // sounding rocket), with no orbit to fall short of
            double floor = MainBody.atmosphere ? MainBody.atmosphereDepth : 0;
            if (BurnoutAltitude < floor)
                return;

            (V3 rf, V3 vf) = solution.TerminalStateVectors();
            (double sma, double ecc, _, _, _, _, _) = Astro.KeplerianFromStateVectors(MainBody.gravParameter, rf, vf);
            double pe = Astro.PeriapsisFromKeplerian(sma, ecc) - MainBody.Radius;
            if (pe < floor)
                SuborbitalPeriapsis = pe;
        }

        /// <summary>
        ///     Forgets everything solved so far, including the solve in progress and what guidance is flying. A timed launch calls
        ///     this because the launch site will have rotated away and the target has changed, so none of it applies any more.
        /// </summary>
        public void FlushSolution()
        {
            CancelJob();
            // a cancelled job can still be reading _solution until it stops, so the next job must not be handed it
            _solution = null;
            ResetCounters();
            Core.PSGAutopilot.FlushSolution();
        }

        /// <summary>
        ///     Guidance calls this when the coast starts.  A solve started before it plans the burn before the coast, so it is
        ///     dropped.  The next one waits OptimizerPauseTime for the shutdown to settle (a staging into the coast waits again),
        ///     and the first to converge that started above the atmosphere solves the coast (VacuumCoastSolved).
        /// </summary>
        public void CoastStarted()
        {
            CancelSolve("the coast started");
            VacuumCoastSolved = false;
            // a burn that burned out ahead of the coast's start may have had no pre-staging freeze, so without this an engine still
            // tailing off could start the coast's solve
            Block(OptimizerPauseTime, OptimizerState.AFTER_PHASE);
        }

        /// <summary>
        ///     Guidance calls this when the coast ends.  A solve started during the coast still has it ahead, which would start
        ///     it again, so it is dropped.
        /// </summary>
        public void CoastEnded()
        {
            CancelSolve("the coast ended");
            VacuumCoastSolved = false;
        }

        /// <summary>
        ///     Guidance calls this when terminal guidance starts.  It flies to the end of the solution it has, which a solve still in
        ///     progress would move, so that solve is dropped (no new one starts until terminal guidance is over).
        /// </summary>
        public void TerminalStarted() => CancelSolve("terminal guidance started");

        /// <summary>Guidance calls this when it finishes, when no solution it is given is flown any more.</summary>
        public void GuidanceFinished() => CancelSolve("guidance finished");

        // Drops the solve in progress, keeping the last converged solution to warm start the next solve: a solve started before
        // the coast starts or ends plans a part of the flight that has already happened (a burn before the coast, or a coast
        // that is over), and must never reach guidance.
        private void CancelSolve(string why)
        {
            if (_ascent is { IsReady: false })
                Debug.Log($"[MechJebModulePSGSimulation] dropped the solve in progress: {why}");

            // Unlike FlushSolution this keeps _solution, or the next solve in flight would start from nothing.  The cancelled
            // job reads it only while it transcribes its warm start, in its first milliseconds, and the next job can't start
            // until a second after this one did (Block at TryStartJob), so the two never read it at once.
            CancelJob();
        }

        private void HandleStageEvent(int data)
        {
            Block(OptimizerPauseTime, OptimizerState.AFTER_STAGING);

            // a staging in the coast (into it, or a fairing) moves the vessel: the first solve after the wait solves the coast again
            VacuumCoastSolved = _vacuumCoastJob = false;
        }

        private void Block(double seconds, OptimizerState reason)
        {
            _blockOptimizerUntilTime = VesselState.Time + seconds;
            _blockReason = reason;
        }

        private void SetState(OptimizerState state, double seconds = 0)
        {
            State = state;
            StateSeconds = seconds;
        }

        public void RequestFlightPlanUpdate()
        {
            // once there is one, the flight plan is kept while the plan is locked
            if (PlanLocked && FlightPlan != null)
                return;
            FlightPlan = LogicalPlan == null ? null : FlightPlan.FromLogicalPlan(LogicalPlan.Plan, Core.StageStats.VacStats, Core.StageStats.AtmoStats, MinDeltaV);
        }

        /// <summary>
        ///     Keeps the logical plan current, then the flight plan built from it: the default plan follows the stage stats, an
        ///     edited one stays as edited, and neither changes while the plan is locked. The Ascent window calls this every physics
        ///     frame while it is open with PSG selected, and the PSG ascent autopilot until there is a flight plan, so launches
        ///     don't depend on that window being open (kOS launches never open it). Returns whether there were stages to plan.
        /// </summary>
        public bool UpdateLogicalPlan()
        {
            List<FuelStats> vacStats = Core.StageStats.VacStats;

            if (vacStats.Count == 0)
                return false;

            if (PlanLocked && FlightPlan != null)
                return true;

            if (!PlanEdited || LogicalPlan == null)
                LogicalPlan = new PersistedLogicalPlan(PlanEditor.DefaultPlan(vacStats, MinDeltaV));

            RequestFlightPlanUpdate();

            return true;
        }

        /// <summary>Keeps plan as the vessel's plan, edited. Refused while the plan is locked.</summary>
        public bool SetEditedPlan(LogicalPlan plan)
        {
            if (PlanLocked)
                return false;

            LogicalPlan = new PersistedLogicalPlan(plan);
            PlanEdited  = true;
            RequestFlightPlanUpdate();
            return true;
        }

        /// <summary>Drops the edited plan for the default one, every burn guided and optimized with no coast. Refused while locked.</summary>
        public bool ResetPlan()
        {
            if (PlanLocked)
                return false;

            PlanEdited = false;
            UpdateLogicalPlan();
            return true;
        }

        private void HandleDoneTask()
        {
            if (!(_ascent is { IsCompleted: true }))
                return;

            try
            {
                Optimizer? psg = _ascent.GetOptimizer();
                if (psg == null)
                    return;

                LastLmStatus = psg.TerminationType;
                LastLmIterations = psg.Iterations;
                LastInfeasibility = psg.PrimalFeasibility;

                if (LastLmIterations > MaxLmIterations)
                    MaxLmIterations = LastLmIterations;

                if (psg.Success() && psg.Solution != null)
                {
                    _solution = psg.Solution;
                    Solution flown = psg.Solution.DeNormalized();
                    CheckReach(flown);
                    Core.PSGAutopilot.SetSolution(flown);

                    // the coast's start and end cancel the job, so a job started in the coast lands in it
                    CheckDebug.True(!_vacuumCoastJob || Core.PSGAutopilot.IsCoasting);
                    if (_vacuumCoastJob && !VacuumCoastSolved)
                    {
                        VacuumCoastSolved = true;
                        Debug.Log($"[MechJebModulePSGSimulation] solved the coast above the atmosphere, {flown.Tgo(VesselState.MET, 0):F1} s to the burn");
                    }

                    SuccessfulConverges += 1;
                    _lastTime = VesselState.Time;
                    Staleness = 0;
                    LastFailureMessage = null;
                }
                else
                {
                    Debug.Log("failed guidance, znorm: " + psg.PrimalFeasibility);
                }
            }
            finally
            {
                _ascent.TryMarkReady();
            }
        }

        private void GatherException()
        {
            if (!(_ascent is { IsFaulted: true }))
                return;

            LastFailureMessage = _ascent.Exception?.Message;

            if (_ascent.Exception != null)
                Debug.Log(_ascent.Exception);
        }

        // A launch into a target's plane, or to its LAN, flies the LAN the countdown timed it for: the target can be cleared,
        // destroyed or recovered since, or the player can target something else to look ahead, and the launch keeps its plane.
        private double TargetLan() => Core.LaunchCountdown.TargetLan;

        private bool _optimizeStageFlag => !LogicalPlan?.AllFixedBurnTimes() ?? true;

        private void MarkReady()
        {
            if (!(_ascent is { IsStopped: true }))
                return;

            if (!_ascent.TryMarkReady())
                throw new Exception("[MechJebModulePSGSimulation] could not mark job as ready");
        }

        public override void OnFixedUpdate()
        {
            Core.StageStats.RequestUpdate();

            double peR = MainBody.Radius + PeriapsisAltitude;
            double apR = MainBody.Radius + ApoapsisAltitude;
            if (ApoapsisAltitude < 0)
                apR = ApoapsisAltitude;
            double attR = MainBody.Radius + BurnoutAltitude;

            bool lanflag = _ascentSettings.LaunchingToPlane || _ascentSettings.LaunchingToMatchLan || _ascentSettings.LaunchingToLan;
            double lan = _ascentSettings.LaunchingToPlane || _ascentSettings.LaunchingToMatchLan ? TargetLan() : (double)LAN;

            bool attachAltFlag = !_optimizeStageFlag || BurnoutAltitudeFlag;

            double fpa = FPA;

            // initialize the first time to avoid initial large staleness values
            if (_lastTime == 0)
                _lastTime = VesselState.Time;

            Staleness = VesselState.Time - _lastTime;

            if (_ascent is { IsRunning: true })
            {
                SetState(OptimizerState.SOLVING);
                UpdateProgressText(_ascent.GetProgress());
                return;
            }

            ProgressDetail = ProgressShort = null;

            GatherException();

            HandleDoneTask();

            MarkReady();

            if (FlightPlan == null)
            {
                SetState(OptimizerState.NO_PLAN);
                return;
            }

            // reconcile the flight plan stages with the VacStats, using the EngineSignatures
            FlightPlan.ReconcileStages(Core.StageStats.VacStats, Core.StageStats.AtmoStats, MinDeltaV);

            if (_optimizeStageFlag)
            {
                // Except if we're using AttR for a fixed time rocket;
                // If 0 < apR < peR then circularize at peR (apR < 0 means hyperbolic orbit)
                if (apR > 0 && apR < peR)
                    apR = peR;
                // Clamp the AttR between peR and apR
                if (attR < peR)
                    attR = peR;
                if (apR > 0 && attR > apR)
                    attR = apR;
            }

            if (Vessel.VesselOffGround())
            {
                // if we have only inertially guided stages we can't run the optimizer
                if (FlightPlan.AllUnguidedStages())
                {
                    // also true once no stage is left at all, as in terminal guidance on the last stage: name that instead
                    SetState(Core.PSGAutopilot.IsTerminal ? OptimizerState.TERMINAL
                        : Core.PSGAutopilot.IsFinished ? OptimizerState.FINISHED
                        : UnguidedBurnLeft() ? OptimizerState.UNGUIDED_ONLY : OptimizerState.NO_PLAN);
                    return;
                }
            }

            // not in terminal guidance, which flies the end of a frozen solution, and not finished
            if (Core.PSGAutopilot.IsTerminal || Core.PSGAutopilot.IsFinished)
            {
                SetState(Core.PSGAutopilot.IsTerminal ? OptimizerState.TERMINAL : OptimizerState.FINISHED);
                return;
            }

            bool vesselOutsideAtmosphere = !MainBody.atmosphere || VesselState.AltitudeASL > MainBody.RealMaxAtmosphereAltitude();

            // Above the atmosphere the coast follows its orbit: once one solve started there has converged (VacuumCoastSolved) the
            // solves pause until 20 s before the burn.  Until then they run, after the waits, so the coast's length is solved from
            // where the shutdown, the staging into the coast and the atmosphere left the vessel.
            if (VacuumCoastSolved && Core.PSGAutopilot.IsCoasting && Core.PSGAutopilot.CurrentPhaseTgo > 20 && vesselOutsideAtmosphere)
            {
                SetState(OptimizerState.COAST_PAUSE, Core.PSGAutopilot.CurrentPhaseTgo - 20);
                return;
            }

            if (Core.PSGAutopilot.Solution != null)
            {
                // check for prestaging as the current stage gets low
                if (Core.PSGAutopilot.CurrentPhaseTgo < PreStageTime)
                {
                    Block(OptimizerPauseTime, OptimizerState.STAGE_ENDING);
                    SetState(Vessel.VesselOffGround() ? OptimizerState.STAGE_ENDING : OptimizerState.ON_PAD, Core.PSGAutopilot.CurrentPhaseTgo);
                    return;
                }
            }

            // After liftoff the vertical ascent and the pitch program fly a fixed program, and the pitch program hands over to
            // guidance where it meets the solution's pitch: a re-solve from the climb would move that pitch.  Without a solution
            // yet the solves go on, or guidance would never take over.
            if (Vessel.VesselOffGround() && Core.PSGAutopilot.Solution != null)
            {
                switch (Core.PSGAutopilot.State)
                {
                    case PSGState.VERTICAL_ASCENT:
                        SetState(OptimizerState.VERTICAL_ASCENT);
                        return;
                    case PSGState.PITCH_PROGRAM:
                        SetState(OptimizerState.PITCH_PROGRAM);
                        return;
                }
            }

            if (_blockOptimizerUntilTime > VesselState.Time)
            {
                // the routine second between two solves is not a pause. A staging event names its own wait (HandleStageEvent): a
                // pre-staging freeze that outlasts its phase without one is the end of a phase that needed no staging
                OptimizerState reason = _blockReason == OptimizerState.STAGE_ENDING ? OptimizerState.AFTER_PHASE : _blockReason;
                if (!Vessel.VesselOffGround() && (reason == OptimizerState.AFTER_PHASE || reason == OptimizerState.AFTER_STAGING))
                    reason = OptimizerState.ON_PAD;
                SetState(reason, _blockOptimizerUntilTime - VesselState.Time);
                return;
            }

            // The PSG clock is MET.  The stage stats snapshot is a little older than now, and one taken before liftoff is at MET
            // zero, so a solution computed on the ground always starts at liftoff.
            double met0 = Max(VesselState.MET - (VesselState.Time - Core.StageStats.VacT), 0);

            bool vacuumCoastJob;

            try
            {
                Ascent.AscentBuilder ascentBuilder = Ascent.Builder()
                   .Initial(Core.StageStats.VacR, Core.StageStats.VacV, Core.StageStats.VacU, met0
                      , MainBody.gravParameter, MainBody.Radius)
                   .SetTarget(peR, apR, attR, Deg2Rad(Inc), Deg2Rad(lan), ArgP, fpa, attachAltFlag, lanflag, ArgPFlag)
                   .FlightPlan(FlightPlan);

                // On the ground guidance flies straight up at full throttle to the pitch start height before the pitch program,
                // so the solve starts at the top of that rise, which the solution keeps in front of its first phase.  After
                // liftoff the solves are paused until guidance takes over, past the rise.
                if (!Vessel.VesselOffGround())
                    ascentBuilder.VerticalRise(Max(_ascentSettings.PitchStartHeight - VesselState.AltitudeBottom, 0));

                // note that we have to solve the atmospheric solution even when flying in vacuum, or the optimizer may decide to dip back
                // into the atmosphere again and burn up the rocket.
                if (MainBody.atmosphere)
                {
                    // This very crudely fits an exponential between "sea" level and 15% of the way to space to find rho0 and h0
                    double r1 = MainBody.atmosphereDepth * 0.15;

                    double rho0 = MainBody.atmDensityASL;
                    double rho1 = MainBody.GetDensity(MainBody.GetPressure(r1), MainBody.GetTemperature(r1));

                    double h0 = r1 / Log(rho0 / rho1);
                    UpdateAutoAero();
                    double cd = Cd;
                    double aRef = Aref;
                    double qAlphaMax = QAlphaLimit;
                    double qMax = Core.AscentSettings.ThrustLimits.LimitDynamicPressure ? Core.AscentSettings.ThrustLimits.MaxDynamicPressure.Val : 0.0;
                    V3 w = 2 * PI / MainBody.rotationPeriod * V3.northpole;

                    ascentBuilder.AerodynamicConstants(cd, aRef, rho0, qAlphaMax, qMax, h0, w);
                }

                if (_solution != null)
                {
                    // the launch site has turned with the planet since a solution computed on the ground was solved, so turn it
                    // onto this problem's initial position before warm starting from it (only a solution computed on the ground
                    // is turned)
                    _solution.RotateToLaunchSite(Core.StageStats.VacR);
                    ascentBuilder.OldSolution(_solution);
                }

                bool massContinuity = false;

                double mint = MinCoast;
                double maxt = MaxCoast;

                if (Core.PSGAutopilot.IsCoasting)
                {
                    mint = Max(mint - (VesselState.Time - Core.PSGAutopilot.CoastStartTime), 0);
                    maxt = Max(maxt - (VesselState.Time - Core.PSGAutopilot.CoastStartTime), 0);
                }

                int startingIndex = FindStartingIndex();

                if (startingIndex == -1)
                {
                    SetState(OptimizerState.NO_PLAN);
                    return;
                }

                // While coasting the starting index is the coast's.  Guidance stages into a coast before the burn (or a dedicated
                // one) only after the autostaging delays, so a solve started before that stage doesn't solve the coast.
                vacuumCoastJob = Core.PSGAutopilot.IsCoasting && vesselOutsideAtmosphere &&
                    Vessel.currentStage <= FlightPlan.Stages[startingIndex].KSPStage;

                // walk the rocket from the bottom up
                // NOTE: if we ever reversed the order the AscentBuilder takes stages this API gets cleaner
                for (int i = startingIndex; i >= 0 ; i--)
                {
                    FlightPlanStage stage = FlightPlan.Stages[i];
                    if (stage.Done)
                        continue;

                    FuelStats fuelStats = stage.FuelStats;

                    switch (stage.StageType)
                    {
                        case StageType.BURN:
                            {
                                double minThrottle = fuelStats.MinThrust / fuelStats.MaxThrust;

                                double ispCurrent = stage.AtmoISP;

                                ascentBuilder.AddStage(fuelStats.StartMass * 1000, fuelStats.EndMass * 1000, fuelStats.MaxThrust * 1000, fuelStats.Isp,
                                    stage.KSPStage, stage.MJStage, stage.Unguided, stage.AllowShutdown, massContinuity, ispCurrent, minThrottle, flightPlanIndex: i);
                                massContinuity = false;
                                break;
                            }
                        case StageType.COAST_DURING:
                            if (i != startingIndex)
                                massContinuity = true; // this coast and next burn should be mass continuity.
                            ascentBuilder.AddCoast(fuelStats.StartMass * 1000, fuelStats.EndMass * 1000, mint, maxt, stage.KSPStage, stage.MJStage, stage.Unguided, massContinuity,
                                flightPlanIndex: i);
                            break;
                        case StageType.COAST_BEFORE:
                        case StageType.COAST_DEDICATED:
                            ascentBuilder.AddCoast(fuelStats.StartMass * 1000, fuelStats.StartMass * 1000, mint, maxt, stage.KSPStage, stage.MJStage, stage.Unguided, massContinuity,
                                flightPlanIndex: i);
                            break;
                        case StageType.COAST_AFTER:
                            ascentBuilder.AddCoast(fuelStats.EndMass * 1000, fuelStats.EndMass * 1000, mint, maxt, stage.KSPStage, stage.MJStage, stage.Unguided, massContinuity,
                                flightPlanIndex: i);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _ascent = ascentBuilder.Build();
            }
            catch (Exception e)
            {
                // the builder runs synchronously, so a failure (e.g. a Check on an invalid user setting) would otherwise
                // escape OnFixedUpdate on every tick without being reported in the UI or backing off.
                LastFailureMessage = e.Message;
                Debug.Log(e);
                Block(1, OptimizerState.RETRYING);
                SetState(OptimizerState.RETRYING, 1);
                return;
            }

            if (!_ascent.TryStartJob())
                throw new Exception("[MechJebModulePSGSimulation] could not start optimizer job");

            _vacuumCoastJob = vacuumCoastJob;

            Block(1, OptimizerState.READY);
            SetState(OptimizerState.SOLVING);
        }

        private bool UnguidedBurnLeft()
        {
            foreach (FlightPlanStage stage in FlightPlan!.Stages)
                if (!stage.Done && stage is { StageType: StageType.BURN, Unguided: true })
                    return true;
            return false;
        }

        private int FindStartingIndex()
        {
            if (FlightPlan == null || FlightPlan.Stages.Count == 0)
                return -1;

            bool foundCoast = false;

            for (int i = FlightPlan.Stages.Count-1; i >= 0; i--)
            {
                FlightPlanStage stage = FlightPlan.Stages[i];

                // if we're coasting just return the index of the coast
                if (Core.PSGAutopilot.IsCoasting)
                {
                    if (stage.StageType == StageType.BURN)
                        continue;
                    return i;
                }

                // if we're past coasting, skip past the coast
                if (Core.PSGAutopilot.HasCoasted && !foundCoast)
                {
                    if (stage.StageType != StageType.BURN)
                        foundCoast = true;
                    continue;
                }

                // skip any remaining burned stages
                if (stage.Done)
                    continue;

                return i;
            }

            return -1;
        }

        public void MarkCoastDone()
        {
            if (FlightPlan == null || FlightPlan.Stages.Count == 0)
                return;

            foreach (FlightPlanStage stage in FlightPlan.Stages)
            {
                if (stage.StageType == StageType.BURN) continue;

                stage.Done = true;
                return;
            }
        }
    }
}
