using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleWarpController : ComputerModule
    {
        public MechJebModuleWarpController(MechJebCore core)
            : base(core)
        {
            WarpPaused = false;
            priority = 100;
            enabled = true;
        }

        double warpIncreaseAttemptTime = 0;

        private int lastAskedIndex = 0;

        public double warpToUT { get; private set; }
        public bool WarpPaused { get; private set; }

        [Persistent(pass = (int)Pass.Global)]
        public bool activateSASOnWarp = true;

        [Persistent(pass = (int)Pass.Global)]
        public bool useQuickWarp = false;

        public void useQuickWarpInfoItem()
        {
            useQuickWarp = GUILayout.Toggle(useQuickWarp, Localizer.Format("#MechJeb_WarpHelper_checkbox1"));//"Quick warp"
        }

        [GeneralInfoItem("#MechJeb_MJWarpControl", InfoItem.Category.Misc)]//MJ Warp Control
        public void ControlWarpButton()
        {
            if (WarpPaused && GUILayout.Button(Localizer.Format("#MechJeb_WarpHelper_button3")))//"Resume MJ Warp"
            {
                ResumeWarp();
            }
            if (!WarpPaused && GUILayout.Button(Localizer.Format("#MechJeb_WarpHelper_button4")))//"Pause MJ Warp"
            {
                PauseWarp();
            }
        }

        public override void OnUpdate()
        {
            if (!WarpPaused && lastAskedIndex > 0 && lastAskedIndex != TimeWarp.CurrentRateIndex)
            {
                // Rate limited by the altitude so we should not care
                if (!vessel.LandedOrSplashed && TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRateIndex == TimeWarp.fetch.GetMaxRateForAltitude(vessel.altitude, vessel.mainBody))
                    return;

                //print("Warppause : lastAskedIndex=" + lastAskedIndex + " CurrentRateIndex=" + TimeWarp.CurrentRateIndex + " WarpMode=" + TimeWarp.WarpMode + " MaxCurrentRate=" + TimeWarp.fetch.GetMaxRateForAltitude(vessel.altitude, vessel.mainBody));
                WarpPaused = false;
                //PauseWarp();

                //ScreenMessages.PostScreenMessage("MJ : Warp canceled by user or an other mod");
            }
        }

        public override void OnFixedUpdate() {
            if (warpToUT > 0)
                WarpToUT(warpToUT);
        }

        private void PauseWarp()
        {
            WarpPaused = true;

            if (activateSASOnWarp && TimeWarp.CurrentRateIndex == 0)
                part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
        }

        private void ResumeWarp()
        {
            if (!WarpPaused)
                return;

            WarpPaused = false;
            SetTimeWarpRate(lastAskedIndex, false);
        }

        // Turn SAS on during regular warp for compatibility with PersistentRotation
        private void SetTimeWarpRate(int rateIndex, bool instant)
        {
            if (rateIndex != TimeWarp.CurrentRateIndex)
            {
                if (activateSASOnWarp && TimeWarp.WarpMode == TimeWarp.Modes.HIGH && TimeWarp.CurrentRateIndex == 0)
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);

                lastAskedIndex = rateIndex;
                if (WarpPaused)
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#MechJeb_WarpHelper_scrmsg"));//"MJ : Warp paused - resume in the Warp Helper menu"
                }
                else
                {
                    TimeWarp.SetRate(rateIndex, instant);
                }

                if (activateSASOnWarp && rateIndex == 0)
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            }
        }

        public void WarpToUT(double UT, double maxRate = -1)
        {
            if (UT <= vesselState.time) {
                warpToUT = 0.0;
                return;
            }

            if (maxRate < 0)
                maxRate = TimeWarp.fetch.warpRates[TimeWarp.fetch.warpRates.Length - 1];

            double desiredRate;
            if (useQuickWarp) {
                desiredRate = 1;
                if (orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL && orbit.EndUT < UT) {
                    for(int i=0; i<TimeWarp.fetch.warpRates.Length; i++){
                        if (i * Time.fixedDeltaTime * TimeWarp.fetch.warpRates[i] <= orbit.EndUT - vesselState.time)
                            desiredRate = TimeWarp.fetch.warpRates[i] + 0.1;
                        else break;
                    }
                }
                else{
                    for(int i=0; i<TimeWarp.fetch.warpRates.Length; i++){
                        if (i * Time.fixedDeltaTime * TimeWarp.fetch.warpRates[i] <= UT - vesselState.time)
                            desiredRate = TimeWarp.fetch.warpRates[i] + 0.1;
                        else break;
                    }
                }
            }
            else desiredRate = 1.0 * (UT - (vesselState.time + Time.fixedDeltaTime * (float)TimeWarp.CurrentRateIndex));
            
            desiredRate = MuUtils.Clamp(desiredRate, 1, maxRate);

            if (!vessel.LandedOrSplashed &&
               vesselState.altitudeASL < TimeWarp.fetch.GetAltitudeLimit(1, mainBody))
            {
                //too low to use any regular warp rates. Use physics warp at a max of x2:
                WarpPhysicsAtRate((float)Math.Min(desiredRate, 2));
            }
            else
            {
                WarpRegularAtRate((float)desiredRate,useQuickWarp,useQuickWarp);
            }
            warpToUT = UT;
        }

        //warp at the highest regular warp rate that is <= maxRate
        public void WarpRegularAtRate(float maxRate, bool instantOnIncrease = false, bool instantOnDecrease = true)
        {
            if (!CheckRegularWarp()) return;

            if (TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex] > maxRate)
            {
                DecreaseRegularWarp(instantOnDecrease);
            }
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Length && TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex + 1] <= maxRate)
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
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.physicsWarpRates.Length && TimeWarp.fetch.physicsWarpRates[TimeWarp.CurrentRateIndex + 1] <= maxRate)
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

        private bool IncreaseRegularWarp(bool instant = false)
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

        private bool IncreasePhysicsWarp(bool instant = false)
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

        private bool DecreaseRegularWarp(bool instant = false)
        {
            if (!CheckRegularWarp()) return false;

            if (TimeWarp.CurrentRateIndex == 0) return false; //already at minimum warp

            SetTimeWarpRate(TimeWarp.CurrentRateIndex - 1, instant);
            return true;
        }

        private bool DecreasePhysicsWarp(bool instant = false)
        {
            if (!CheckPhysicsWarp()) return false;

            if (TimeWarp.CurrentRateIndex == 0) return false; //already at minimum warp

            SetTimeWarpRate(TimeWarp.CurrentRateIndex - 1, instant);
            return true;
        }

        public bool MinimumWarp(bool instant = false)
        {
            warpToUT = 0.0;
            if (TimeWarp.CurrentRateIndex == 0) return false; //Somehow setting TimeWarp.SetRate to 0 when already at 0 causes unexpected rapid separation (Kraken)
            SetTimeWarpRate(0, instant);
            return true;
        }
    }
}
