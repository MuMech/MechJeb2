extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleWarpController : ComputerModule
    {
        public MechJebModuleWarpController(MechJebCore core)
            : base(core)
        {
            WarpPaused = false;
            Priority = 100;
            Enabled = true;
        }

        private double warpIncreaseAttemptTime;

        private int lastAskedIndex;
        private float _lastPausedMessageTime = float.NegativeInfinity;

        public double warpToUT   { get; private set; }
        public bool   WarpPaused { get; private set; }

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool activateSASOnWarp = true;

        [UsedImplicitly, Persistent(pass = (int)Pass.GLOBAL)]
        public bool useQuickWarp;

        [GeneralInfoItem("#MechJeb_MJWarpControl", InfoItem.Category.Misc)] //MJ Warp Control
        public void ControlWarpButton() { }

        // set while MechJeb itself changes the rate, so the rate change event can tell its changes from anyone else's
        private bool _settingRate;

        // The warp someone else stopped: the player's stop key, or a mod such as Kerbal Alarm Clock, set the rate to 1x while MechJeb
        // was warping to this UT. The autopilots ask again every tick, so AutopilotWarpToUT raises no warp until that time has
        // passed, the player warps by hand, or a new warp is started with ClearStop. WarpToUT, what other mods call, is such a
        // new warp.
        private double _stoppedUT = double.NaN;

        /// <summary>True while MechJeb's autopilots hold off warping because someone else stopped its warp.</summary>
        public bool StoppedByOther => !double.IsNaN(_stoppedUT);

        /// <summary>Lets MechJeb warp again after someone else stopped it, for a warp the player starts anew.</summary>
        public void ClearStop() => _stoppedUT = double.NaN;

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onTimeWarpRateChanged.Add(OnTimeWarpRateChanged);
            base.OnStart(state);
        }

        public override void OnDestroy()
        {
            GameEvents.onTimeWarpRateChanged.Remove(OnTimeWarpRateChanged);
            base.OnDestroy();
        }

        // KSP fires this from inside TimeWarp.SetRate, so MechJeb's own changes are the ones made while _settingRate is set
        private void OnTimeWarpRateChanged()
        {
            if (_settingRate)
                return;

            if (TimeWarp.CurrentRateIndex == 0)
            {
                if (warpToUT > 0)
                {
                    _stoppedUT = warpToUT;
                    warpToUT   = 0.0;
                }
            }
            else
            {
                // the player warps again: MechJeb manages the warp again
                _stoppedUT = double.NaN;
            }
        }

        public override void OnFixedUpdate()
        {
            if (warpToUT > 0)
                AutopilotWarpToUT(warpToUT);
        }

        internal void PauseWarp()
        {
            WarpPaused = true;

            if (activateSASOnWarp && TimeWarp.CurrentRateIndex == 0)
                Part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
        }

        internal void ResumeWarp()
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
                    Part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);

                lastAskedIndex = rateIndex;
                // Pausing stops MJ from raising the warp. Slowing down is always allowed, so autopilots that need to
                // stop warping (node executor, landing) still work.
                if (WarpPaused && rateIndex >= TimeWarp.CurrentRateIndex)
                {
                    // the autopilots ask every tick, do not flood the screen
                    if (Time.realtimeSinceStartup - _lastPausedMessageTime > 5f)
                    {
                        _lastPausedMessageTime = Time.realtimeSinceStartup;
                        ScreenMessages.PostScreenMessage(
                            Localizer.Format("#MechJeb_WarpHelper_scrmsg")); //"MJ : Warp paused - resume in the Warp Helper menu"
                    }
                }
                else
                {
                    _settingRate = true;
                    try { TimeWarp.SetRate(rateIndex, instant); }
                    finally { _settingRate = false; }
                }

                if (activateSASOnWarp && rateIndex == 0)
                    Part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            }
        }

        /// <summary>
        ///     Warps to that UT, as a new request: it warps even after someone else stopped MechJeb's last warp. For other mods (kOS's
        ///     WARPTOUT, RasterPropMonitor's warp buttons), which call it once for each warp the player asks for. The warp then goes on
        ///     by itself until that UT, or until someone else stops it. MechJeb's autopilots, which ask every tick, call
        ///     <see cref="AutopilotWarpToUT" /> instead.
        /// </summary>
        public void WarpToUT(double UT, double maxRate = -1)
        {
            ClearStop();
            AutopilotWarpToUT(UT, maxRate);
        }

        /// <summary>
        ///     Warps to that UT for an autopilot that asks every tick. After someone else stopped the warp (the player, or a mod such as
        ///     Kerbal Alarm Clock), it raises no warp until the stopped warp's UT has passed, the player warps by hand, or
        ///     <see cref="ClearStop" /> is called for a new warp.
        /// </summary>
        public void AutopilotWarpToUT(double UT, double maxRate = -1)
        {
            if (UT <= VesselState.Time)
            {
                warpToUT = 0.0;
                return;
            }

            if (StoppedByOther)
            {
                if (VesselState.Time < _stoppedUT)
                    return;
                _stoppedUT = double.NaN;
            }

            if (maxRate < 0)
                maxRate = TimeWarp.fetch.warpRates[TimeWarp.fetch.warpRates.Length - 1];

            double desiredRate;
            if (useQuickWarp)
            {
                desiredRate = 1;
                if (Orbit.patchEndTransition != Orbit.PatchTransitionType.FINAL && Orbit.EndUT < UT)
                {
                    for (int i = 0; i < TimeWarp.fetch.warpRates.Length; i++)
                    {
                        if (i * Time.fixedDeltaTime * TimeWarp.fetch.warpRates[i] <= Orbit.EndUT - VesselState.Time)
                            desiredRate = TimeWarp.fetch.warpRates[i] + 0.1;
                        else break;
                    }
                }
                else
                {
                    for (int i = 0; i < TimeWarp.fetch.warpRates.Length; i++)
                    {
                        if (i * Time.fixedDeltaTime * TimeWarp.fetch.warpRates[i] <= UT - VesselState.Time)
                            desiredRate = TimeWarp.fetch.warpRates[i] + 0.1;
                        else break;
                    }
                }
            }
            else desiredRate = 1.0 * (UT - (VesselState.Time + Time.fixedDeltaTime * TimeWarp.CurrentRateIndex));

            desiredRate = Clamp(desiredRate, 1, maxRate);

            if (!Vessel.LandedOrSplashed &&
                VesselState.AltitudeASL < TimeWarp.fetch.GetAltitudeLimit(1, MainBody))
            {
                //too low to use any regular warp rates. Use physics warp at a max of x2:
                WarpPhysicsAtRate((float)Math.Min(desiredRate, 2));
            }
            else
            {
                WarpRegularAtRate((float)desiredRate, useQuickWarp, useQuickWarp);
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
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.warpRates.Length &&
                     TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex + 1] <= maxRate)
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
            else if (TimeWarp.CurrentRateIndex + 1 < TimeWarp.fetch.physicsWarpRates.Length &&
                     TimeWarp.fetch.physicsWarpRates[TimeWarp.CurrentRateIndex + 1] <= maxRate)
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
                double instantAltitudeASL = (VesselState.CoM - MainBody.position).magnitude - MainBody.Radius;
                if (instantAltitudeASL > MainBody.RealMaxAtmosphereAltitude())
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
            if (!Vessel.LandedOrSplashed)
            {
                double instantAltitudeASL = (VesselState.CoM - MainBody.position).magnitude - MainBody.Radius;
                if (TimeWarp.fetch.GetAltitudeLimit(TimeWarp.CurrentRateIndex + 1, MainBody) > instantAltitudeASL) return false;
                //altitude too low to increase warp
            }

            if (TimeWarp.fetch.warpRates[TimeWarp.CurrentRateIndex] != TimeWarp.CurrentRate)
                return false; //most recent warp change is not yet complete
            if (VesselState.Time - warpIncreaseAttemptTime < 2) return false; //we increased warp too recently

            warpIncreaseAttemptTime = VesselState.Time;
            SetTimeWarpRate(TimeWarp.CurrentRateIndex + 1, instant);
            return true;
        }

        private bool IncreasePhysicsWarp(bool instant = false)
        {
            if (!CheckPhysicsWarp()) return false; //make sure we are in regular warp

            //do a bunch of checks to see if we can increase the warp rate:
            if (TimeWarp.CurrentRateIndex + 1 == TimeWarp.fetch.physicsWarpRates.Length) return false; //already at max warp
            if (TimeWarp.fetch.physicsWarpRates[TimeWarp.CurrentRateIndex] != TimeWarp.CurrentRate)
                return false; //most recent warp change is not yet complete
            if (VesselState.Time - warpIncreaseAttemptTime < 2) return false; //we increased warp too recently

            warpIncreaseAttemptTime = VesselState.Time;
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
            if (TimeWarp.CurrentRateIndex == 0)
                return false; //Somehow setting TimeWarp.SetRate to 0 when already at 0 causes unexpected rapid separation (Kraken)
            SetTimeWarpRate(0, instant);
            return true;
        }
    }
}
