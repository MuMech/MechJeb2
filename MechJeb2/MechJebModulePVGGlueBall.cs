/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Threading.Tasks;
using MechJebLib.Core;
using MechJebLib.PVG;
using UnityEngine;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MuMech
{
    /// <summary>
    ///     This class isolates a bunch of glue between MJ and the PVG optimizer that I don't
    ///     yet understand how to write correctly.
    /// </summary>
    public class MechJebModulePVGGlueBall : ComputerModule
    {
        private double _blockOptimizerUntilTime;

        public int SuccessfulConverges;
        public int LastLmStatus;
        public int MaxLmIterations;
        public int LastLmIterations;

        public  Exception? Exception;
        public  double     Staleness;
        public  double     LastZnorm;
        private double     _lastTime;

        public MechJebModulePVGGlueBall(MechJebCore core) : base(core) { }

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        private Task? _task;

        private Ascent? _ascent;

        protected override void OnModuleEnabled()
        {
            SuccessfulConverges = LastLmStatus     = MaxLmIterations = 0;
            LastLmStatus        = LastLmIterations = 0;
            Staleness           = LastZnorm        = _lastTime = 0;
        }

        protected override void OnModuleDisabled()
        {
        }

        public override void OnFixedUpdate()
        {
            Core.StageStats.RequestUpdate(this);
        }

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onStageActivate.Add(HandleStageEvent);
        }

        public override void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(HandleStageEvent);
        }

        private void HandleStageEvent(int data)
        {
            _blockOptimizerUntilTime = VesselState.time + _ascentSettings.OptimizerPauseTime;
        }

        private bool VesselOffGround()
        {
            return Vessel.situation != Vessel.Situations.LANDED && Vessel.situation != Vessel.Situations.PRELAUNCH &&
                   Vessel.situation != Vessel.Situations.SPLASHED;
        }

        private bool IsUnguided(int s)
        {
            return _ascentSettings.UnguidedStages.Contains(s);
        }

        // FIXME: maybe this could be a callback to the Task?
        private void HandleDoneTask()
        {
            if (!(_task is { IsCompleted: true }) || _ascent == null)
                return;

            Optimizer? pvg = _ascent.GetOptimizer();

            if (pvg != null)
            {
                if (pvg.Success())
                {
                    Core.Guidance.SetSolution(pvg.GetSolution());
                    SuccessfulConverges += 1;
                    _lastTime           =  VesselState.time;
                    Staleness           =  0;
                }
                else
                {
                    Debug.Log("failed guidance, znorm: " + pvg.Znorm);
                }

                LastLmStatus     = pvg.LmStatus;
                LastLmIterations = pvg.LmIterations;
                LastZnorm        = pvg.Znorm;

                if (LastLmIterations > MaxLmIterations)
                    MaxLmIterations = LastLmIterations;
            }

            _task = null;
        }

        private void GatherException()
        {
            if (!(_task is { IsCompleted: true }))
                return;

            Exception = _task.Exception?.InnerException;

            Debug.Log(Exception);
        }

        public void SetTarget(double peR, double apR, double attR, double inclination, double lan, double fpa, bool attachAltFlag, bool lanflag)
        {
            // initialize the first time we hit SetTarget to avoid initial large staleness values
            if (_lastTime == 0)
                _lastTime = VesselState.time;

            Staleness = VesselState.time - _lastTime;

            GatherException();

            HandleDoneTask();

            if (_task is { IsCompleted: false })
            {
                Debug.Log("active PVG task not completed");
                return;
            }

            if (_ascentSettings.OptimizeStageFlag)
            {
                // clamp the AttR between peR and apR but not if we're using AttR for a fixed time rocket
                if (attR < peR)
                    attR = peR;
                if (attR > apR && apR > peR)
                    attR = apR;
            }

            if (VesselOffGround())
            {
                bool hasGuided = false;

                for (int i = Core.StageStats.vacStats.Length - 1; i >= _ascentSettings.LastStage; i--)
                {
                    double dv = Core.StageStats.vacStats[i].DeltaV;

                    // skip the zero length stages
                    if (dv == 0)
                        continue;

                    if (!IsUnguided(i))
                        hasGuided = true;
                }

                // if we have only inertially guided stages we can't run the optimizer
                if (!hasGuided)
                    return;
            }

            // check for readiness (not terminal guidance and not finished)
            if (!Core.Guidance.IsReady())
                return;

            if (Core.Guidance.Solution != null)
            {
                int solutionIndex = Core.Guidance.Solution.IndexForKSPStage(Vessel.currentStage, Core.Guidance.IsCoasting());

                if (solutionIndex >= 0)
                {
                    // check for prestaging as the current stage gets low
                    if (Core.Guidance.Solution?.Tgo(VesselState.time, solutionIndex) < _ascentSettings.PreStageTime)
                    {
                        _blockOptimizerUntilTime = VesselState.time + _ascentSettings.OptimizerPauseTime;
                        return;
                    }
                }
            }

            if (_blockOptimizerUntilTime > VesselState.time)
                return;

            Ascent.AscentBuilder ascentBuilder = Ascent.Builder()
                .Initial(VesselState.orbitalPosition.WorldToV3Rotated(), VesselState.orbitalVelocity.WorldToV3Rotated(),
                    VesselState.forward.WorldToV3Rotated(),
                    VesselState.time, MainBody.gravParameter, MainBody.Radius)
                .SetTarget(peR, apR, attR, Deg2Rad(inclination), Deg2Rad(lan), fpa, attachAltFlag, lanflag)
                .TerminalConditions(Maths.HmagFromApsides(MainBody.gravParameter, peR, apR));

            if (Core.Guidance.Solution != null)
                ascentBuilder.OldSolution(Core.Guidance.Solution);

            bool optimizedStageFound = false;

            for (int i = Core.StageStats.vacStats.Length - 1; i >= _ascentSettings.LastStage; i--)
            {
                FuelFlowSimulation.FuelStats fuelStats = Core.StageStats.vacStats[i];

                if (!Core.Guidance.HasGoodSolutionWithNoFutureCoast())
                {
                    if ((i == _ascentSettings.CoastStage && _ascentSettings.CoastBeforeFlag) ||
                        (i == _ascentSettings.CoastStage - 1 && !_ascentSettings.CoastBeforeFlag))
                    {
                        double ct = _ascentSettings.FixedCoastLength;
                        double maxt = _ascentSettings.MaxCoast;
                        double mint = _ascentSettings.MinCoast;

                        if (i == Vessel.currentStage && Core.Guidance.IsCoasting())
                        {
                            ct   = Math.Max(ct - (VesselState.time - Core.Guidance.StartCoast), 0);
                            maxt = Math.Max(maxt - (VesselState.time - Core.Guidance.StartCoast), 0);
                            mint = Math.Max(mint - (VesselState.time - Core.Guidance.StartCoast), 0);
                        }

                        if (_ascentSettings.FixedCoast)
                        {
                            ascentBuilder.AddFixedCoast(fuelStats.StartMass * 1000, ct, _ascentSettings.CoastStage);
                        }
                        else
                        {
                            ascentBuilder.AddOptimizedCoast(fuelStats.StartMass * 1000, mint, maxt, _ascentSettings.CoastStage);
                        }
                    }
                }

                // skip sep motors.  we already avoid running if the bottom stage has burned below this margin.
                if (fuelStats.DeltaV < _ascentSettings.MinDeltaV)
                    continue;

                bool optimizeTime = _ascentSettings.OptimizeStage == i;

                if (optimizeTime)
                    optimizedStageFound = true;

                bool unguided = IsUnguided(i);

                ascentBuilder.AddStageUsingFinalMass(fuelStats.StartMass * 1000, fuelStats.EndMass * 1000, fuelStats.Isp, fuelStats.DeltaTime, i,
                    optimizeTime, unguided);
            }

            ascentBuilder.FixedBurnTime(!optimizedStageFound); // FIXME: can ascentbuilder just figure this out?

            _ascent = ascentBuilder.Build();

            _task = new Task(_ascent.Run);
            _task.Start();

            _blockOptimizerUntilTime = VesselState.time + 1;
        }
    }
}
