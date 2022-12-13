/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Threading.Tasks;
using MechJebLib.Maths;
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

        private MechJebModuleAscentSettings _ascentSettings => core.ascentSettings;

        private Task? _task;

        private Ascent? _ascent;

        public override void OnModuleEnabled()
        {
            SuccessfulConverges = LastLmStatus     = MaxLmIterations = 0;
            LastLmStatus        = LastLmIterations = 0;
            Staleness           = LastZnorm        = _lastTime = 0;
        }

        public override void OnModuleDisabled()
        {
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
            _blockOptimizerUntilTime = vesselState.time + _ascentSettings.OptimizerPauseTime;
        }

        private bool VesselOffGround()
        {
            return vessel.situation != Vessel.Situations.LANDED && vessel.situation != Vessel.Situations.PRELAUNCH &&
                   vessel.situation != Vessel.Situations.SPLASHED;
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
                    core.guidance.SetSolution(pvg.GetSolution());
                    SuccessfulConverges += 1;
                    _lastTime           =  vesselState.time;
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
                _lastTime = vesselState.time;
            
            Staleness = vesselState.time - _lastTime;

            GatherException();
            
            HandleDoneTask();

            if (_task is { IsCompleted: false })
            {
                return;
            }

            // clamp the AttR
            if (attR < peR)
                attR = peR;
            if (attR > apR && apR > peR)
                attR = apR;

            if (VesselOffGround())
            {
                bool hasGuided = false;

                for (int i = core.stageStats.vacStats.Length - 1; i >= _ascentSettings.LastStage; i--)
                {
                    double dv = core.stageStats.vacStats[i].DeltaV;

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

            if (_blockOptimizerUntilTime > vesselState.time)
                return;

            // check for readiness (not terminal guidance and not finished)
            if (!core.guidance.IsReady())
                return;

            if (core.guidance.Solution != null)
            {
                int solutionIndex = core.guidance.Solution.IndexForKSPStage(vessel.currentStage);

                if (solutionIndex >= 0)
                {
                    // check for prestaging as the current stage gets low
                    if (core.guidance.Solution?.Tgo(vesselState.time, solutionIndex) < _ascentSettings.PreStageTime)
                    {
                        _blockOptimizerUntilTime = vesselState.time + _ascentSettings.OptimizerPauseTime;
                        return;
                    }
                }
            }

            Ascent.AscentBuilder ascentBuilder = Ascent.Builder()
                .Initial(vesselState.orbitalPosition.WorldToV3(), vesselState.orbitalVelocity.WorldToV3(), vesselState.forward.WorldToV3(),
                    vesselState.time, mainBody.gravParameter, mainBody.Radius)
                .SetTarget(peR, apR, attR, Deg2Rad(inclination), Deg2Rad(lan), fpa, attachAltFlag, lanflag)
                .TerminalConditions(Functions.HmagFromApsides(mainBody.gravParameter, peR, apR));

            if (core.guidance.Solution != null)
                ascentBuilder.OldSolution(core.guidance.Solution);

            bool optimizedStageFound = false;

            for (int i = core.stageStats.vacStats.Length - 1; i >= _ascentSettings.LastStage; i--)
            {
                FuelFlowSimulation.FuelStats fuelStats = core.stageStats.vacStats[i];

                if ((i == _ascentSettings.CoastStage && _ascentSettings.CoastBeforeFlag) || (i == _ascentSettings.CoastStage - 1 && !_ascentSettings.CoastBeforeFlag))
                {
                    double ct = _ascentSettings.FixedCoastLength;
                    double maxt = _ascentSettings.MaxCoast;
                    double mint = _ascentSettings.MinCoast;

                    if (i == vessel.currentStage && core.guidance.IsCoasting())
                    {
                        ct   = Math.Max(ct - vesselState.time - core.guidance.StartCoast, 0);
                        maxt = Math.Max(maxt - vesselState.time - core.guidance.StartCoast, 0);
                        mint = Math.Max(mint - vesselState.time - core.guidance.StartCoast, 0);
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

            _blockOptimizerUntilTime = vesselState.time + 1;
        }
    }
}
