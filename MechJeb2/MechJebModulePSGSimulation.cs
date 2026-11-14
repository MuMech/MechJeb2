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
        READY,         // between two solves
        SOLVING,       // a solve is running
        NO_PLAN,       // no flight plan, or nothing left in it to plan
        STAGE_ENDING,  // the current stage is about to end: the solution is frozen through staging
        AFTER_STAGING, // a short wait after staging
        AFTER_PHASE,   // the same wait after a phase ended without staging (the end of a coast during a stage)
        ON_PAD,        // either wait before liftoff: the stage not lit yet has no phase in the solution, so its time to go reads 0
        COAST_PAUSE,   // a long coast above the atmosphere: resumes 20 s before the burn
        TERMINAL,      // terminal guidance flies the end of the burn on the frozen solution
        UNGUIDED_ONLY, // only unguided stages are left, which the optimizer can't steer
        FINISHED,      // guidance is done
        RETRYING       // the problem could not be built (a setting PSG refuses): tried again every second
    }

    /// <summary>
    ///     This class isolates a bunch of glue between MJ and the PSG optimizer that I don't
    ///     yet understand how to write correctly.
    /// </summary>
    public class MechJebModulePSGSimulation : ComputerModule
    {
        [Persistent(pass = (int)Pass.TYPE)]
        public PersistedLogicalPlan? LogicalPlan;

        // This is generated from the LogicalPlan, and is not user-editable while the module is enabled
        public FlightPlan? FlightPlan;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult PeriapsisAltitude = new EditableDoubleMult(145000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult ApoapsisAltitude = new EditableDoubleMult(0, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble Inc = new EditableDouble(0.0);

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

            string time = GuiUtils.TimeToDHMS(seconds);
            string step = $"computing the ascent, step {p.Pass} of {Max(p.Pass, p.PassCount)}";
            ProgressShort = $"{step} ({time})";
            ProgressDetail = p.Iterations > 0 && !double.IsNaN(p.Infeasibility)
                ? $"{step}, {p.Name}: {time}, iteration {p.Iterations}, error {p.Infeasibility:0.0e+00}"
                : $"{step}, {p.Name}: {time}";
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
            Core.Guidance.FlushSolution();
        }

        private void HandleStageEvent(int data) => Block(OptimizerPauseTime, OptimizerState.AFTER_STAGING);

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
            // the flight plan is frozen in flight, once there is one
            if (Enabled && FlightPlan != null)
                return;
            FlightPlan = LogicalPlan == null ? null : FlightPlan.FromLogicalPlan(LogicalPlan.Plan, Core.StageStats.VacStats, Core.StageStats.AtmoStats, MinDeltaV);
        }

        /// <summary>
        ///     Builds the logical plan from the stage stats and the ascent settings, then the flight plan from it (unless that is
        ///     frozen). The Ascent window calls this every physics frame while it is open with PSG selected, and the PSG ascent
        ///     autopilot until there is a flight plan, so launches don't depend on that window being open (kOS launches never
        ///     open it). Returns whether there were stages to plan.
        /// </summary>
        public bool UpdateLogicalPlan(MechJebModuleAscentSettings settings)
        {
            List<FuelStats> vacStats = Core.StageStats.VacStats;

            if (vacStats.Count == 0)
                return false;

            LogicalPlan = new PersistedLogicalPlan(MechJebLib.PSG.LogicalPlan.GenerateLogicalPlan(vacStats, settings.UnguidedStages, settings.FixedStages,
                settings.LastStage, MinDeltaV, settings.CoastStage, settings.CoastLocation));

            RequestFlightPlanUpdate();

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
                    Core.Guidance.SetSolution(flown);
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
                    SetState(!Core.Guidance.IsReady() ? Core.Guidance.IsTerminal() ? OptimizerState.TERMINAL : OptimizerState.FINISHED
                        : UnguidedBurnLeft() ? OptimizerState.UNGUIDED_ONLY : OptimizerState.NO_PLAN);
                    return;
                }
            }

            // check for readiness (not terminal guidance and not finished)
            if (!Core.Guidance.IsReady())
            {
                SetState(Core.Guidance.IsTerminal() ? OptimizerState.TERMINAL : OptimizerState.FINISHED);
                return;
            }

            bool vesselOutsideAtmosphere = !MainBody.atmosphere || VesselState.AltitudeASL > MainBody.RealMaxAtmosphereAltitude();

            // suspend guidance for long coast phases out of the atmosphere
            if (Core.Guidance.IsCoasting() && Core.Guidance.CurrentPhaseTgo > 20 && vesselOutsideAtmosphere)
            {
                SetState(OptimizerState.COAST_PAUSE, Core.Guidance.CurrentPhaseTgo - 20);
                return;
            }

            if (Core.Guidance.Solution != null)
            {
                // check for prestaging as the current stage gets low
                if (Core.Guidance.CurrentPhaseTgo < PreStageTime)
                {
                    Block(OptimizerPauseTime, OptimizerState.STAGE_ENDING);
                    SetState(Vessel.VesselOffGround() ? OptimizerState.STAGE_ENDING : OptimizerState.ON_PAD, Core.Guidance.CurrentPhaseTgo);
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

            try
            {
                Ascent.AscentBuilder ascentBuilder = Ascent.Builder()
                   .Initial(Core.StageStats.VacR, Core.StageStats.VacV, Core.StageStats.VacU, met0
                      , MainBody.gravParameter, MainBody.Radius)
                   .SetTarget(peR, apR, attR, Deg2Rad(Inc), Deg2Rad(lan), ArgP, fpa, attachAltFlag, lanflag, ArgPFlag)
                   .FlightPlan(FlightPlan);

                // note that we have to solve the atmospheric solution even when flying in vacuum, or the optimizer may decide to dip back
                // into the atmosphere again and burn up the rocket.
                if (MainBody.atmosphere)
                {
                    // This very crudely fits an exponential between "sea" level and 15% of the way to space to find rho0 and h0
                    double r1 = MainBody.atmosphereDepth * 0.15;

                    double rho0 = MainBody.atmDensityASL;
                    double rho1 = MainBody.GetDensity(MainBody.GetPressure(r1), MainBody.GetTemperature(r1));

                    double h0 = r1 / Log(rho0 / rho1);
                    double cd = Cd;
                    double aRef = Aref;
                    double qAlphaMax = QAlphaLimit;
                    double qMax = Core.Thrust.LimitDynamicPressure ? Core.Thrust.MaxDynamicPressure.Val : 0.0;
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

                if (Core.Guidance.IsCoasting())
                {
                    mint = Max(mint - (VesselState.Time - Core.Guidance.StartCoast), 0);
                    maxt = Max(maxt - (VesselState.Time - Core.Guidance.StartCoast), 0);
                }

                int startingIndex = FindStartingIndex();

                if (startingIndex == -1)
                {
                    SetState(OptimizerState.NO_PLAN);
                    return;
                }

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
                if (Core.Guidance.IsCoasting())
                {
                    if (stage.StageType == StageType.BURN)
                        continue;
                    return i;
                }

                // if we're past coasting, skip past the coast
                if (Core.Guidance.hasCoasted && !foundCoast)
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
