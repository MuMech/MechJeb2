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
    ///     - optimizer watchdog timeout
    ///     - threading
    ///     - coasts and other options
    ///     - reset guidance
    ///     - relay statistics back to the UI (DONE)
    /// </summary>
    public class MechJebModulePVGGlueBall : ComputerModule
    {
        private double _blockOptimizerUntilTime = 0.0;

        public  int    SuccessfulConverges;
        public  int    LastLmStatus;
        public  int    MaxLmIterations;
        public  int    LastLmIterations;
        public  double Staleness;
        public  double LastZnorm;

        public MechJebModulePVGGlueBall(MechJebCore core) : base(core) { }

        public override void OnModuleEnabled()
        {
            SuccessfulConverges = LastLmStatus         = MaxLmIterations = 0;
            LastLmStatus        = LastLmIterations = 0;
            Staleness           = LastZnorm            = 0;
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
            _blockOptimizerUntilTime = vesselState.time + 5;
        }

        private bool VesselOffGround()
        {
            return vessel.situation != Vessel.Situations.LANDED && vessel.situation != Vessel.Situations.PRELAUNCH &&
                   vessel.situation != Vessel.Situations.SPLASHED;
        }

        public void SetTarget(double peR, double apR, double attR, double inclination, double lan, bool attachAltFlag, bool lanflag)
        {
            // clamp the AttR
            if (attR < peR)
                attR = peR;
            if (attR > apR && apR > peR)
                attR = apR;
            
            if (VesselOffGround())
            {
                for (int i = core.stageStats.vacStats.Length - 1; i >= 0; i--)
                {
                    double dv = core.stageStats.vacStats[i].DeltaV;
                    if (dv == 0)
                        continue;
                    if (dv > 20)
                        break;
                    // as long as we have a next stage which is less than 20 dv block running
                    // more simulations for at least 5 seconds.
                    _blockOptimizerUntilTime = vesselState.time + 5;
                }
            }

            if (_blockOptimizerUntilTime > vesselState.time)
                return;
            
            if (!core.guidance.IsReady())
            {
                return;
            }
            
            Ascent.AscentBuilder ascent = Ascent.Builder()
                .Initial(vesselState.orbitalPosition.WorldToV3(), vesselState.orbitalVelocity.WorldToV3(), vesselState.time, mainBody.gravParameter)
                .SetTarget(peR, apR, attR, Deg2Rad(inclination), Deg2Rad(lan), attachAltFlag, lanflag);

            if (core.guidance.Solution != null)
                ascent.OldSolution(core.guidance.Solution);

            for (int i = core.stageStats.vacStats.Length - 1; i >= 0; i--)
            {
                FuelFlowSimulation.FuelStats fuelStats = core.stageStats.vacStats[i];

                // skip sep motors.  we already avoid running if the bottom stage has burned below this margin.
                if (fuelStats.DeltaV < 20)
                    continue;

                ascent.AddStageUsingBurnTime(fuelStats.StartMass * 1000, fuelStats.MaxThrust * 1000, fuelStats.Isp, fuelStats.DeltaTime);
            }

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
