/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using MechJebLib.PVG;
using static MechJebLib.Utils.Statics;
using UnityEngine;

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
    ///     - optimizer watchdog timeout
    ///     - threading
    ///     - coasts and other options
    ///     - report failures
    ///     - handle failures by rebootstrapping with infinite upper stage, current Pv and zero Pr
    ///     - relay statistics back to the UI (DONE)
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

        private bool IsUnguided(int s) // MEGA-FIXME: hardcoded
        {
            return s <= 2;
        }

        public void SetTarget(double peR, double apR, double attR, double inclination, double lan, bool attachAltFlag, bool lanflag)
        {
            if (_ascentSettings.SpinupStage < 0)
                core.spinup.users.Remove(this);
            else if (vessel.currentStage > _ascentSettings.SpinupStage)
                core.spinup.users.Add(this);

            core.spinup.ActivationStage     = _ascentSettings.SpinupStage;
            core.spinup.RollAngularVelocity = _ascentSettings.SpinupAngularVelocity;

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

            Ascent.AscentBuilder ascent = Ascent.Builder()
                .Initial(vesselState.orbitalPosition.WorldToV3(), vesselState.orbitalVelocity.WorldToV3(), vesselState.forward.WorldToV3(),
                    vesselState.time, mainBody.gravParameter)
                .SetTarget(peR, apR, attR, Deg2Rad(inclination), Deg2Rad(lan), attachAltFlag, lanflag);

            if (core.guidance.Solution != null)
                ascent.OldSolution(core.guidance.Solution);

            for (int i = core.stageStats.vacStats.Length - 1; i >= _ascentSettings.LastStage; i--)
            {
                FuelFlowSimulation.FuelStats fuelStats = core.stageStats.vacStats[i];

                // skip sep motors.  we already avoid running if the bottom stage has burned below this margin.
                if (fuelStats.DeltaV < _ascentSettings.MinDeltaV)
                    continue;

                bool optimizeTime = _ascentSettings.OptimizeStage == i;

                bool unguided = IsUnguided(i);

                if (i == 2)
                {
                    double ct = 60;
                    
                    if (i+1 == vessel.currentStage && core.guidance.IsCoasting())
                        ct -= vesselState.time - core.guidance.StartCoast;

                    ascent.AddCoast(fuelStats.StartMass * 1000, ct, i + 1);
                }

                ascent.AddStageUsingBurnTime(fuelStats.StartMass * 1000, fuelStats.MaxThrust * 1000, fuelStats.Isp, fuelStats.DeltaTime, i,
                    optimizeTime,
                    unguided);
            }

            ascent.FixedBurnTime(_ascentSettings.FixedBurntime);

            Optimizer pvg = ascent.Build().Run();

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

            _blockOptimizerUntilTime = vesselState.time + 1;
        }
    }
}
