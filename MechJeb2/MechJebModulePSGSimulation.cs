/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

extern alias JetBrainsAnnotations;
using System;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLibBindings.PSG;
using UnityEngine;
using static MechJebLib.Utils.Statics;
using static System.Math;

#nullable enable

namespace MuMech
{
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

        public int SuccessfulConverges;
        public int LastLmStatus;
        public int MaxLmIterations;
        public int LastLmIterations;

        public string? LastFailureMessage;
        public double Staleness;
        public double LastInfeasibility;
        private double _lastTime;

        public MechJebModulePSGSimulation(MechJebCore core) : base(core) { }

        private Ascent? _ascent;

        // The solver's own copy of the last converged solution, used to warm start the next solve; guidance flies a separate
        // DeepCopy(). The worker thread reads it while a job runs, so the main thread only touches it between jobs.
        private Solution? _solution;

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        protected override void OnModuleEnabled()
        {
            Debug.Log("Enabling PSG Simulation");
            SuccessfulConverges = LastLmStatus = MaxLmIterations = 0;
            LastLmStatus = LastLmIterations = 0;
            Staleness = LastInfeasibility = _lastTime = 0;
            LastFailureMessage = null;
            _ascent = null;
            _solution = null;
            // FIXME: should throw and disable on no logicalplan or flightplan
        }

        protected override void OnModuleDisabled()
        {
            Debug.Log("Disabling PSG Simulation");
            // a job that is still running keeps reading _solution, so the next enable must not hand it to another job
            _ascent = null;
            _solution = null;
        }

        public override void OnStart(PartModule.StartState state) => GameEvents.onStageActivate.Add(HandleStageEvent);

        public override void OnDestroy() => GameEvents.onStageActivate.Remove(HandleStageEvent);

        private void HandleStageEvent(int data) => _blockOptimizerUntilTime = VesselState.Time + OptimizerPauseTime;

        public void RequestFlightPlanUpdate()
        {
            // the flight plan is frozen in flight
            if (Enabled)
                return;
            FlightPlan = LogicalPlan == null ? null : FlightPlan.FromLogicalPlan(LogicalPlan.Plan, Core.StageStats.VacStats, Core.StageStats.AtmoStats, MinDeltaV);
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
                    Core.Guidance.SetSolution(psg.Solution.DeepCopy());
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
            double lan = _ascentSettings.LaunchingToPlane || _ascentSettings.LaunchingToMatchLan
                ? Core.Target.TargetOrbit.LAN
                : (double)LAN;

            bool attachAltFlag = !_optimizeStageFlag || BurnoutAltitudeFlag;

            double fpa = FPA;

            // initialize the first time to avoid initial large staleness values
            if (_lastTime == 0)
                _lastTime = VesselState.Time;

            Staleness = VesselState.Time - _lastTime;

            if (_ascent is { IsRunning: true })
                return;

            GatherException();

            HandleDoneTask();

            MarkReady();

            if (FlightPlan == null)
                return;

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
                    return;
            }

            // check for readiness (not terminal guidance and not finished)
            if (!Core.Guidance.IsReady())
                return;

            bool vesselOutsideAtmosphere = !MainBody.atmosphere || VesselState.AltitudeASL > MainBody.RealMaxAtmosphereAltitude();

            // suspend guidance for long coast phases out of the atmosphere
            if (Core.Guidance.IsCoasting() && Core.Guidance.CurrentPhaseTgo > 20 && vesselOutsideAtmosphere)
                return;

            if (Core.Guidance.Solution != null)
            {
                // check for prestaging as the current stage gets low
                if (Core.Guidance.CurrentPhaseTgo < PreStageTime)
                {
                    _blockOptimizerUntilTime = VesselState.Time + OptimizerPauseTime;
                    return;
                }
            }

            if (_blockOptimizerUntilTime > VesselState.Time)
                return;

            try
            {
                Ascent.AscentBuilder ascentBuilder = Ascent.Builder()
                   .Initial(Core.StageStats.VacR, Core.StageStats.VacV, Core.StageStats.VacU, Core.StageStats.VacT
                      , MainBody.gravParameter, MainBody.Radius)
                   .SetTarget(peR, apR, attR, Deg2Rad(Inc), Deg2Rad(lan), ArgP, fpa, attachAltFlag, lanflag, ArgPFlag);

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
                    double qAlphaMax = LimitQa;
                    double qMax = Core.Thrust.LimitDynamicPressure ? Core.Thrust.MaxDynamicPressure.Val : 0.0;
                    V3 w = 2 * PI / MainBody.rotationPeriod * V3.northpole;

                    ascentBuilder.AerodynamicConstants(cd, aRef, rho0, qAlphaMax, qMax, h0, w);
                }

                if (_solution != null && Core.Guidance.Solution != null)
                {
                    // guidance slides its copy's T0 forward while waiting on the pad, and the warm start has to begin at liftoff
                    _solution.T0 = Core.Guidance.Solution.T0;
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
                    return;

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
                                    stage.KSPStage, stage.MJStage, stage.Unguided, stage.AllowShutdown, massContinuity, ispCurrent, minThrottle);
                                massContinuity = false;
                                break;
                            }
                        case StageType.COAST_DURING:
                            if (i != startingIndex)
                                massContinuity = true; // this coast and next burn should be mass continuity.
                            ascentBuilder.AddCoast(fuelStats.StartMass * 1000, fuelStats.EndMass * 1000, mint, maxt, stage.KSPStage, stage.MJStage, stage.Unguided, massContinuity);
                            break;
                        case StageType.COAST_BEFORE:
                        case StageType.COAST_DEDICATED:
                            ascentBuilder.AddCoast(fuelStats.StartMass * 1000, fuelStats.StartMass * 1000, mint, maxt, stage.KSPStage, stage.MJStage, stage.Unguided, massContinuity);
                            break;
                        case StageType.COAST_AFTER:
                            ascentBuilder.AddCoast(fuelStats.EndMass * 1000, fuelStats.EndMass * 1000, mint, maxt, stage.KSPStage, stage.MJStage, stage.Unguided, massContinuity);
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
                _blockOptimizerUntilTime = VesselState.Time + 1;
                return;
            }

            // TODO: wire up Cancellation and Timeouts
            if (!_ascent.TryStartJob())
                throw new Exception("[MechJebModulePSGSimulation] could not start optimizer job");

            _blockOptimizerUntilTime = VesselState.Time + 1;
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
