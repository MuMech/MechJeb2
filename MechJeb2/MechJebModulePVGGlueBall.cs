using UnityEngine;
using MechJebLib.PVG;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MuMech
{
    /// <summary>
    ///     This class isolates a bunch of glue between MJ and the PVG optimizer that I don't
    ///     yet understand how to write correctly.
    ///     TODO:
    ///     - Gather old solution, desired target and physical stages
    ///     - Build and feed to Ascent optimizer
    ///     - Shoven new solution back to the guidance controller
    ///     - Throttle new requests for a second
    ///     FUTURE:
    ///     - threading
    ///     - coasts and other options
    ///     - reset guidance
    ///     - relay statistics back to the UI
    /// </summary>
    public class MechJebModulePVGGlueBall : ComputerModule
    {
        private double _lastSuccessTime = 0;
        
        public MechJebModulePVGGlueBall(MechJebCore core) : base(core) { }

        private bool VesselOffGround()
        {
            return vessel.situation != Vessel.Situations.LANDED && vessel.situation != Vessel.Situations.PRELAUNCH &&
                   vessel.situation != Vessel.Situations.SPLASHED;
        }
        
        public void SetTarget(double peR, double apR, double attR, double inclination, double lan, bool attachAltFlag, bool lanflag, double coastLen)
        {
            if (vesselState.time - _lastSuccessTime < 1.0)
                return;

            // this ensures we have a burning stage with some deltaV to bother with running the optimizer
            // this avoids sep motors, nearly burned out stages and stages burning more than is expected due to residuals
            // FIXME: should this margin be larger for unlucky residuals?
            if (core.stageStats.vacStats[core.stageStats.vacStats.Length - 1].DeltaV < 20 && VesselOffGround())
                return;
            
            Ascent.AscentBuilder ascent = Ascent.Builder()
                .Initial(vesselState.orbitalPosition.WorldToV3(), vesselState.orbitalVelocity.WorldToV3(), vesselState.time, mainBody.gravParameter)
                .SetTarget(peR, apR, attR, Deg2Rad(inclination), Deg2Rad(lan), attachAltFlag, lanflag, coastLen);

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
            }
            else
            {
                Debug.Log("failed guidance, znorm: " + pvg.Znorm);
            }

            _lastSuccessTime = vesselState.time;
        }
    }
}
