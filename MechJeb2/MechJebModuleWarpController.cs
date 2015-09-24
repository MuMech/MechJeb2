using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleWarpController : ComputerModule
    {
        public MechJebModuleWarpController(MechJebCore core)
            : base(core)
        {
            priority = 100;
        }

        double warpIncreaseAttemptTime = 0;


        // Turn SAS on during regular warp for compatibility with PersistentRotation 
        void SetTimeWarpRate(int rateIndex, bool instant)
        {
            if (rateIndex != TimeWarp.CurrentRateIndex)
            {
                if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRateIndex == 0)
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                TimeWarp.SetRate(rateIndex, instant);
                if (rateIndex == 0)
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            }
        }

        public void WarpToUT(double UT, double maxRate = 100000)
        {
            double desiredRate = 1.0 * (UT - (vesselState.time + Time.fixedDeltaTime * (float)TimeWarp.CurrentRateIndex));
            desiredRate = MuUtils.Clamp(desiredRate, 1, maxRate);

            if (!vessel.LandedOrSplashed &&
               vesselState.altitudeASL < TimeWarp.fetch.GetAltitudeLimit(1, mainBody))
            {
                //too low to use any regular warp rates. Use physics warp at a max of x2:
                WarpPhysicsAtRate((float)Math.Min(desiredRate, 2));
            }
            else
            {
                WarpRegularAtRate((float)desiredRate);
            }
        }

        //warp at the highest regular warp rate that is <= maxRate
        public void WarpRegularAtRate(float maxRate, bool instantOnIncrease = false, bool instantOnDecrease = true)
        {
            if (!CheckRegularWarp()) return;

            if (TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex] > maxRate)
            {
                DecreaseRegularWarp(instantOnDecrease);
            }
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Count() && TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex + 1] <= maxRate)
            {
                IncreaseRegularWarp(instantOnIncrease);
            }
        }

        //warp at the highest regular warp rate that is <= maxRate
        public void WarpPhysicsAtRate(float maxRate, bool instantOnIncrease = false, bool instantOnDecrease = true)
        {
            if (!CheckPhysicsWarp()) return;

            if (TimeWarp.fetch.physicsWarpRates[TimeWarp.CurrentRateIndex] > maxRate)
            {
                DecreasePhysicsWarp(instantOnDecrease);
            }
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.physicsWarpRates.Count() && TimeWarp.fetch.physicsWarpRates[TimeWarp.CurrentRateIndex + 1] <= maxRate)
            {
                IncreasePhysicsWarp(instantOnIncrease);
            }
        }

        //If the warp mode is regular warp, returns true
        //If the warp mode is physics warp, switches it to regular warp and returns false
        private bool CheckRegularWarp()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH)
            {
                double instantAltitudeASL = (vesselState.CoM - mainBody.position).magnitude - mainBody.Radius;
                if (instantAltitudeASL > mainBody.RealMaxAtmosphereAltitude())
                {
                    TimeWarp.fetch.Mode = TimeWarp.Modes.HIGH;
                    SetTimeWarpRate(0, true);
                }
                return false;
            }
            return true;
        }

        //If the warp mode is physics warp, returns true
        //If the warp mode is regular warp, switches it to physics warp and returns false
        private bool CheckPhysicsWarp()
        {
            if (TimeWarp.WarpMode != TimeWarp.Modes.LOW)
            {
                TimeWarp.fetch.Mode = TimeWarp.Modes.LOW;
                SetTimeWarpRate(0, true);
                return false;
            }
            return true;
        }

        public bool IncreaseRegularWarp(bool instant = false)
        {
            if (!CheckRegularWarp()) return false; //make sure we are in regular warp

            //do a bunch of checks to see if we can increase the warp rate:
            if (TimeWarp.CurrentRateIndex + 1 == TimeWarp.fetch.warpRates.Length) return false; //already at max warp
            if (!vessel.LandedOrSplashed)
            {
                double instantAltitudeASL = (vesselState.CoM - mainBody.position).magnitude - mainBody.Radius;
                if (TimeWarp.fetch.GetAltitudeLimit(TimeWarp.CurrentRateIndex + 1, mainBody) > instantAltitudeASL) return false;
                //altitude too low to increase warp
            }
            if (TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex] != TimeWarp.CurrentRate) return false; //most recent warp change is not yet complete
            if (vesselState.time - warpIncreaseAttemptTime < 2) return false; //we increased warp too recently

            warpIncreaseAttemptTime = vesselState.time;
            SetTimeWarpRate(TimeWarp.CurrentRateIndex + 1, instant);
            return true;
        }

        public bool IncreasePhysicsWarp(bool instant = false)
        {
            if (!CheckPhysicsWarp()) return false; //make sure we are in regular warp

            //do a bunch of checks to see if we can increase the warp rate:
            if (TimeWarp.CurrentRateIndex + 1 == TimeWarp.fetch.physicsWarpRates.Length) return false; //already at max warp
            if (TimeWarp.fetch.physicsWarpRates[TimeWarp.CurrentRateIndex] != TimeWarp.CurrentRate) return false; //most recent warp change is not yet complete
            if (vesselState.time - warpIncreaseAttemptTime < 2) return false; //we increased warp too recently

            warpIncreaseAttemptTime = vesselState.time;
            SetTimeWarpRate(TimeWarp.CurrentRateIndex + 1, instant);
            return true;
        }

        public bool DecreaseRegularWarp(bool instant = false)
        {
            if (!CheckRegularWarp()) return false;

            if (TimeWarp.CurrentRateIndex == 0) return false; //already at minimum warp

            SetTimeWarpRate(TimeWarp.CurrentRateIndex - 1, instant);
            return true;
        }

        public bool DecreasePhysicsWarp(bool instant = false)
        {
            if (!CheckPhysicsWarp()) return false;

            if (TimeWarp.CurrentRateIndex == 0) return false; //already at minimum warp

            SetTimeWarpRate(TimeWarp.CurrentRateIndex - 1, instant);
            return true;
        }

        public bool MinimumWarp(bool instant = false)
        {
            if (TimeWarp.CurrentRateIndex == 0) return false; //Somehow setting TimeWarp.SetRate to 0 when already at 0 causes unexpected rapid separation (Kraken)
            SetTimeWarpRate(0, instant);
            return true;
        }
    }
}
