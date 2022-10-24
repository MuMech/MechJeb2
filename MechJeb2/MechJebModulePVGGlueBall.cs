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
    ///     TODO:
    ///     - Gather old solution, desired target and physical stages (DONE)
    ///     - Build and feed to Ascent optimizer (DONE)
    ///     - Shove new solution back to the guidance controller (DONE)
    ///     - Throttle new requests for a second (DONE)
    ///     - Don't run when the controller is in terminal guidance (DONE)
    ///     - Don't run immediately after staging (DONE)
    ///     - Needs to be properly enabled by the PVG ascent state machine (DONE)
    ///     - Should it control the guidance module?
    ///     FUTURE:
    ///     - optimizer watchdog timeout (DONE)
    ///     - threading (DONE)
    ///     - coasts and other options
    ///     - report failures in the UI
    ///     - handle failures by rebootstrapping with infinite upper stage, current Pv and zero Pr
    ///     - relay statistics back to the UI (DONE)
    ///     - fix staeleness in the UI
    ///     - remove inertial stage hardcoding (DONE)
    /// </summary>
    public class MechJebModulePVGGlueBall : ComputerModule
    {
        private double _blockOptimizerUntilTime;

        public int    SuccessfulConverges;
        public int    LastLmStatus;
        public int    MaxLmIterations;
        public int    LastLmIterations;
        public double Staleness;
        public double LastZnorm;

        public MechJebModulePVGGlueBall(MechJebCore core) : base(core) { }

        private MechJebModuleAscentSettings _ascentSettings => core.ascentSettings;

        private Task? _task;

        private Ascent? _ascent;

        public override void OnModuleEnabled()
        {
            SuccessfulConverges = LastLmStatus     = MaxLmIterations = 0;
            LastLmStatus        = LastLmIterations = 0;
            Staleness           = LastZnorm        = 0;
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
            return _ascentSettings.UnguidedStages.val.Contains(s);
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

        public void SetTarget(double peR, double apR, double attR, double inclination, double lan, bool attachAltFlag, bool lanflag)
        {
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

                for (int i = core.stageStats.vacStats.Length - 1; i >= _ascentSettings.LastStage; i--)
                {
                    double dv = core.stageStats.vacStats[i].DeltaV;
                    double dt = core.stageStats.vacStats[i].DeltaTime;

                    // skip the zero length stages
                    if (dv == 0)
                        continue;

                    if (dv > _ascentSettings.MinDeltaV && dt > _ascentSettings.PreStageTime)
                        break;

                    // or else we need to pause guidance
                    _blockOptimizerUntilTime = vesselState.time + _ascentSettings.OptimizerPauseTime;
                    break;
                }
            }

            if (_blockOptimizerUntilTime > vesselState.time)
                return;

            if (!core.guidance.IsReady())
            {
                return;
            }

            // terminal guidance check
            if (core.guidance.IsStable() && core.guidance.Tgo < 10)
                return;

            Ascent.AscentBuilder ascentBuilder = Ascent.Builder()
                .Initial(vesselState.orbitalPosition.WorldToV3(), vesselState.orbitalVelocity.WorldToV3(), vesselState.forward.WorldToV3(),
                    vesselState.time, mainBody.gravParameter, mainBody.Radius)
                .SetTarget(peR, apR, attR, Deg2Rad(inclination), Deg2Rad(lan), attachAltFlag, lanflag)
                .TerminalConditions(Functions.HmagFromApsides(mainBody.gravParameter, peR, apR));

            if (core.guidance.Solution != null)
                ascentBuilder.OldSolution(core.guidance.Solution);

            bool optimizedStageFound = false;

            for (int i = core.stageStats.vacStats.Length - 1; i >= _ascentSettings.LastStage; i--)
            {
                FuelFlowSimulation.FuelStats fuelStats = core.stageStats.vacStats[i];

                if (i == _ascentSettings.CoastStage)
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
                        ascentBuilder.AddFixedCoast(fuelStats.StartMass * 1000, ct, i);
                    }
                    else
                    {
                        ascentBuilder.AddOptimizedCoast(fuelStats.StartMass * 1000, mint, maxt, i);
                    }
                }

                // skip sep motors.  we already avoid running if the bottom stage has burned below this margin.
                if (fuelStats.DeltaV < _ascentSettings.MinDeltaV)
                    continue;

                bool optimizeTime = _ascentSettings.OptimizeStage == i;

                if (optimizeTime)
                    optimizedStageFound = true;

                bool unguided = IsUnguided(i);

                ascentBuilder.AddStageUsingBurnTime(fuelStats.StartMass * 1000, fuelStats.MaxThrust * 1000, fuelStats.Isp, fuelStats.DeltaTime, i,
                    optimizeTime, unguided);
            }

            ascentBuilder.FixedBurnTime(!optimizedStageFound); // FIXME: can ascentbuilder just figure this out?

            _ascent = ascentBuilder.Build();

            Debug.Log("firing off new task");
            _task = new Task(_ascent.Run);
            _task.Start();

            _blockOptimizerUntilTime = vesselState.time + 1;
        }
    }
}
