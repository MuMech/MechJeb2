using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{
    public class StagingController : ComputerModule
    {
        public StagingController(MechJebCore core)
            : base(core)
        {
            priority = 1000;
        }

        private bool autoStage = false;

        public void setAutoStage(bool autoStage)
        {
            this.autoStage = autoStage;
        }


        public override void OnFixedUpdate()
        {
            /*            if (!part.vessel.isActiveVessel) return;

                        //if autostage enabled, and if we are not waiting on the pad, and if there are stages left,
                        //and if we are allowed to continue staging, and if we didn't just fire the previous stage
                        if (autoStage && liftedOff && Staging.CurrentStage > 0 && Staging.CurrentStage > autoStageLimit
                            && vesselState.time - lastStageTime > autoStageDelay)
                        {
                            //don't decouple active or idle engines or tanks
                            if (!ARUtils.inverseStageDecouplesActiveOrIdleEngineOrTank(Staging.CurrentStage - 1, part.vessel))
                            {
                                //only fire decouplers to drop deactivated engines or tanks
                                if (!ARUtils.inverseStageFiresDecoupler(Staging.CurrentStage - 1, part.vessel)
                                    || ARUtils.inverseStageDecouplesDeactivatedEngineOrTank(Staging.CurrentStage - 1, part.vessel))
                                {
                                    if (ARUtils.inverseStageFiresDecoupler(Staging.CurrentStage - 1, part.vessel))
                                    {
                                        //if we decouple things, delay the next stage a bit to avoid exploding the debris
                                        lastStageTime = vesselState.time;
                                    }

                                    Staging.ActivateNextStage();
                                }
                            }
                        }*/
        }

    }
}
