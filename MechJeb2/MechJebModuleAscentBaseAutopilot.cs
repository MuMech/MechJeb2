using System;
using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;

namespace MuMech
{
    public abstract class MechJebModuleAscentBaseAutopilot : ComputerModule
    {
        protected MechJebModuleAscentBaseAutopilot(MechJebCore core) : base(core) { }

        public string Status = "";

        protected MechJebModuleAscentSettings AscentSettings => Core.AscentSettings;

        public  bool   TimedLaunch;
        private double _launchTime;

        public double CurrentMaxAoA;

        // useful to track time since launch event (works around bugs with physics starting early and vessel.launchTime being wildly off)
        // FIXME?: any time we lift off from a rock this should probably be set to that time?
        private double _launchStarted;

        public double TMinus => _launchTime - VesselState.time;

        //internal state:
        private enum AscentMode { PRELAUNCH, ASCEND, CIRCULARIZE }

        private AscentMode _mode;
        private bool       _placedCircularizeNode;
        private double     _lastTMinus = 999;

        // wiring for launchStarted
        private void OnLaunch(EventReport report)
        {
            _launchStarted = VesselState.time;
            Debug.Log("[MechJebModuleAscentAutopilot] LaunchStarted = " + _launchStarted);
        }

        // wiring for launchStarted
        public override void OnStart(PartModule.StartState state)
        {
            _launchStarted = -1;
            GameEvents.onLaunch.Add(OnLaunch);
        }

        private void FixupLaunchStart()
        {
            // continuously update the launchStarted time if we're sitting on the ground on the water anywhere (once we are not, then we've launched)
            if (Vessel.situation == Vessel.Situations.LANDED || Vessel.situation == Vessel.Situations.PRELAUNCH ||
                Vessel.situation == Vessel.Situations.SPLASHED)
                _launchStarted = VesselState.time;
        }

        // various events can cause launchStarted to be before or after vessel.launchTime, but the most recent one is so far always the most accurate
        // (physics wobbles can start vessel.launchTime (KSP's zero MET) early, while staging before engaging the autopilot can cause launchStarted to happen early)
        // this will only be valid AFTER launching
        protected double MET => VesselState.time - (_launchStarted > Vessel.launchTime ? _launchStarted : Vessel.launchTime);

        protected override void OnModuleEnabled()
        {
            _mode = AscentMode.PRELAUNCH;

            _placedCircularizeNode = false;

            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
            if (AscentSettings.Autostage) Core.Staging.Users.Add(this);

            Status = Localizer.Format("#MechJeb_Ascent_status1"); //"Pre Launch"
        }

        protected override void OnModuleDisabled()
        {
            Core.Attitude.attitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.Users.Remove(this);

            if (_placedCircularizeNode) Core.Node.Abort();

            Status = Localizer.Format("#MechJeb_Ascent_status2"); //"Off"
        }

        public void StartCountdown(double time)
        {
            if (AscentSettings.OverrideWarpToPlane)
            {
                TimedLaunch = false;
                _launchTime = VesselState.time;
                _lastTMinus = 0;
            }
            else
            {
                TimedLaunch = true;
                _launchTime = time;
                _lastTMinus = 999;
            }
        }

        public override void OnFixedUpdate()
        {
            if (AscentSettings.AscentType == AscentType.PVG)
                Core.StageStats.RequestUpdate();

            FixupLaunchStart();
            if (TimedLaunch)
            {
                if (TMinus < 3 * VesselState.deltaT || (TMinus > 10.0 && _lastTMinus < 1.0))
                {
                    if (Enabled && VesselState.thrustAvailable < 10E-4) // only stage if we have no engines active
                        StageManager.ActivateNextStage();
                    TimedLaunchHook(); // let ascentPath modules do stuff edge triggered on launch starting
                    TimedLaunch = false;
                }
                else
                {
                    if (Core.Node.autowarp)
                        Core.Warp.WarpToUT(_launchTime - AscentSettings.WarpCountDown);
                }

                _lastTMinus = TMinus;
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            AscentSettings.LimitingAoA = false;

            switch (_mode)
            {
                case AscentMode.PRELAUNCH:
                    DrivePrelaunch();
                    break;

                case AscentMode.ASCEND:
                    DriveAscent();
                    break;

                case AscentMode.CIRCULARIZE:
                    DriveCircularizationBurn();
                    break;
            }
        }

        private void DriveDeployableComponents()
        {
            if (AscentSettings.AutodeploySolarPanels)
            {
                if (VesselState.altitudeASL > MainBody.RealMaxAtmosphereAltitude())
                {
                    Core.Solarpanel.ExtendAll();
                }
                else
                {
                    Core.Solarpanel.RetractAll();
                }
            }

            if (AscentSettings.AutoDeployAntennas)
            {
                if (VesselState.altitudeASL > MainBody.RealMaxAtmosphereAltitude())
                    Core.AntennaControl.ExtendAll();
                else
                    Core.AntennaControl.RetractAll();
            }
        }

        private void DrivePrelaunch()
        {
            if (Vessel.LiftedOff() && !Vessel.Landed)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status4"); //"Vessel is not landed, skipping pre-launch"
                _mode  = AscentMode.ASCEND;
                return;
            }

            Debug.Log("prelaunch killing throttle");
            Core.Thrust.ThrustOff();

            Core.Attitude.SetAxisControl(false, false, false);

            if (TimedLaunch && TMinus > 10.0)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status1"); //"Pre Launch"
                return;
            }

            if (AscentSettings.AutodeploySolarPanels && MainBody.atmosphere)
            {
                Core.Solarpanel.RetractAll();
                if (Core.Solarpanel.AllRetracted())
                {
                    Debug.Log("Prelaunch -> Ascend");
                    _mode = AscentMode.ASCEND;
                }
                else
                {
                    Status = Localizer.Format("#MechJeb_Ascent_status5"); //"Retracting solar panels"
                }
            }
            else
            {
                _mode = AscentMode.ASCEND;
            }
        }

        private void DriveAscent()
        {
            if (TimedLaunch)
            {
                Debug.Log("Awaiting Liftoff");
                Status = Localizer.Format("#MechJeb_Ascent_status6"); //"Awaiting liftoff"
                // kill the optimizer if it is running.
                Core.Guidance.Enabled = false;

                Core.Attitude.SetAxisControl(false, false, false);
                return;
            }

            DriveDeployableComponents();

            if (DriveAscent2())
            {
                if (GameSettings.VERBOSE_DEBUG_LOG) { Debug.Log("Remaining in Ascent"); }
            }
            else
            {
                if (GameSettings.VERBOSE_DEBUG_LOG) { Debug.Log("Ascend -> Circularize"); }

                _mode = AscentMode.CIRCULARIZE;
            }
        }

        private void DriveCircularizationBurn()
        {
            if (!Vessel.patchedConicsUnlocked() || AscentSettings.SkipCircularization)
            {
                Users.Clear();
                return;
            }

            DriveDeployableComponents();

            if (_placedCircularizeNode)
            {
                if (Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                {
                    MechJebModuleFlightRecorder recorder = Core.GetComputerModule<MechJebModuleFlightRecorder>();
                    if (recorder != null) AscentSettings.LaunchPhaseAngle.val    = recorder.phaseAngleFromMark;
                    if (recorder != null) AscentSettings.LaunchLANDifference.val = VesselState.orbitLAN - recorder.markLAN;

                    //finished circularize
                    Users.Clear();
                    return;
                }
            }
            else
            {
                //place circularization node
                Vessel.RemoveAllManeuverNodes();
                double ut = Orbit.NextApoapsisTime(VesselState.time);
                //During the circularization burn, try to correct any inclination errors because it's better to combine the two burns.
                //  For example, if you're about to do a 1500 m/s circularization burn, if you combine a 200 m/s inclination correction
                //  into it, you actually only spend 1513 m/s to execute combined manuver.  Mechjeb should also do correction burns before
                //  this if possible, and this can't correct all errors... but it's better then nothing.
                //   (A better version of this should try to match inclination & LAN if target is specified)
                // FIXME? this inclination correction is unlikely to be at tha AN/DN and will throw the LAN off with anything other than high
                // TWR launches from equatorial launch sites -- should probably be made optional (or clip it if the correction is too large).
                Vector3d inclinationCorrection =
                    OrbitalManeuverCalculator.DeltaVToChangeInclination(Orbit, ut, Math.Abs(AscentSettings.DesiredInclination));
                Vector3d smaCorrection = OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(Orbit.PerturbedOrbit(ut, inclinationCorrection), ut,
                    AscentSettings.DesiredOrbitAltitude + MainBody.Radius);
                Vector3d dV = inclinationCorrection + smaCorrection;
                Vessel.PlaceManeuverNode(Orbit, dV, ut);
                _placedCircularizeNode = true;

                Core.Node.ExecuteOneNode(this);
            }

            Status = Localizer.Format(Core.Node.burnTriggered ? "#MechJeb_Ascent_status7" : "#MechJeb_Ascent_status8");
        }

        protected abstract bool DriveAscent2();

        protected virtual void TimedLaunchHook()
        {
            // triggered when timed launches start the actual launch
        }

        //data used by ThrottleToRaiseApoapsis
        private          float         _raiseApoapsisLastThrottle;
        private          double        _raiseApoapsisLastApR;
        private          double        _raiseApoapsisLastUT;
        private readonly MovingAverage _raiseApoapsisRatePerThrottle = new MovingAverage(3);

        //gives a throttle setting that reduces as we approach the desired apoapsis
        //so that we can precisely match the desired apoapsis instead of overshooting it
        protected float ThrottleToRaiseApoapsis(double currentApR, double finalApR)
        {
            float desiredThrottle;

            if (currentApR > finalApR + 5.0)
            {
                desiredThrottle = 0.0F; //done, throttle down
            }
            else if (_raiseApoapsisLastUT > VesselState.time - 1)
            {
                //reduce throttle as apoapsis nears target
                double instantRatePerThrottle =
                    (Orbit.ApR - _raiseApoapsisLastApR) / ((VesselState.time - _raiseApoapsisLastUT) * _raiseApoapsisLastThrottle);
                instantRatePerThrottle              = Math.Max(1.0, instantRatePerThrottle); //avoid problems from negative rates
                _raiseApoapsisRatePerThrottle.Value = instantRatePerThrottle;
                double desiredApRate = (finalApR - currentApR) / 1.0;
                desiredThrottle = Mathf.Clamp((float)(desiredApRate / _raiseApoapsisRatePerThrottle), 0.05F, 1.0F);
            }
            else
            {
                desiredThrottle = 1.0F; //no recent data point; just use max thrust.
            }

            //record data for next frame
            _raiseApoapsisLastThrottle = desiredThrottle;
            _raiseApoapsisLastApR      = Orbit.ApR;
            _raiseApoapsisLastUT       = VesselState.time;

            return desiredThrottle;
        }

        protected double SrfvelPitch() => 90.0 - Vector3d.Angle(VesselState.surfaceVelocity, VesselState.up);

        protected double SrfvelHeading() => VesselState.HeadingFromDirection(VesselState.surfaceVelocity.ProjectOnPlane(VesselState.up));

        // this provides ground track heading based on desired inclination and is what most consumers should call
        protected void AttitudeTo(double desiredPitch)
        {
            double desiredHeading = OrbitalManeuverCalculator.HeadingForLaunchInclination(Vessel.orbit, AscentSettings.DesiredInclination);
            AttitudeTo(desiredPitch, desiredHeading);
        }

        // provides AoA limiting and roll control
        // provides no ground tracking and should only be called by autopilots like PVG that deeply know what they're doing with yaw control
        // (possibly should be moved into the attitude controller, but right now it collaborates too heavily with the ascent autopilot)
        //
        protected void AttitudeTo(double desiredPitch, double desiredHeading)
        {
            /*
            Vector6 rcs = vesselState.rcsThrustAvailable;

            // FIXME?  should this be up/down and not forward/back?  seems wrong?  why was i using down before for the ullage direction?
            bool has_rcs = vessel.hasEnabledRCSModules() && vessel.ActionGroups[KSPActionGroup.RCS] && ( rcs.left > 0.01 ) && ( rcs.right > 0.01 ) && ( rcs.forward > 0.01 ) && ( rcs.back > 0.01 );

            if ( (vesselState.thrustCurrent / vesselState.thrustAvailable < 0.50) && !has_rcs )
            {
                // if engines are spooled up at less than 50% and we have no RCS in the stage, do not issue any guidance commands yet
                return;
            }
            */

            Vector3d desiredHeadingVector = Math.Sin(desiredHeading * UtilMath.Deg2Rad) * VesselState.east +
                                            Math.Cos(desiredHeading * UtilMath.Deg2Rad) * VesselState.north;

            Vector3d desiredThrustVector = Math.Cos(desiredPitch * UtilMath.Deg2Rad) * desiredHeadingVector
                                           + Math.Sin(desiredPitch * UtilMath.Deg2Rad) * VesselState.up;

            desiredThrustVector = desiredThrustVector.normalized;

            /* old style AoA limiter */
            if (AscentSettings.LimitAoA && !AscentSettings.LimitQaEnabled)
            {
                float fade = VesselState.dynamicPressure < AscentSettings.AOALimitFadeoutPressure
                    ? (float)(AscentSettings.AOALimitFadeoutPressure / VesselState.dynamicPressure)
                    : 1;
                CurrentMaxAoA = Math.Min(fade * AscentSettings.MaxAoA, 180d);
                AscentSettings.LimitingAoA = Vessel.altitude < MainBody.atmosphereDepth &&
                                             Vector3.Angle(VesselState.surfaceVelocity, desiredThrustVector) > CurrentMaxAoA;

                if (AscentSettings.LimitingAoA)
                {
                    desiredThrustVector = Vector3.RotateTowards(VesselState.surfaceVelocity, desiredThrustVector,
                        (float)(CurrentMaxAoA * Mathf.Deg2Rad), 1).normalized;
                }
            }

            /* AoA limiter for PVG */
            if (AscentSettings.LimitQaEnabled)
            {
                double lim = MuUtils.Clamp(AscentSettings.LimitQa, 0, 10000);
                AscentSettings.LimitingAoA =
                    VesselState.dynamicPressure * Vector3.Angle(VesselState.surfaceVelocity, desiredThrustVector) * UtilMath.Deg2Rad > lim;
                if (AscentSettings.LimitingAoA)
                {
                    CurrentMaxAoA = lim / VesselState.dynamicPressure * UtilMath.Rad2Deg;
                    desiredThrustVector = Vector3.RotateTowards(VesselState.surfaceVelocity, desiredThrustVector,
                        (float)(CurrentMaxAoA * UtilMath.Deg2Rad), 1).normalized;
                }
            }

            bool liftedOff = Vessel.LiftedOff() && !Vessel.Landed && VesselState.altitudeBottom > 5;

            double pitch = 90 - Vector3d.Angle(desiredThrustVector, VesselState.up);

            double hdg;

            if (pitch > 89.9)
            {
                hdg = desiredHeading;
            }
            else
            {
                hdg = MuUtils.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(desiredThrustVector, VesselState.east),
                    Vector3d.Dot(desiredThrustVector, VesselState.north)));
            }

            if (AscentSettings.ForceRoll)
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                Core.Attitude.attitudeTo(hdg, pitch, desiredPitch == 90.0 ? AscentSettings.VerticalRoll : AscentSettings.TurnRoll, this,
                    liftedOff, liftedOff, liftedOff && VesselState.altitudeBottom > AscentSettings.RollAltitude, true);
            }
            else
            {
                Core.Attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL_COT, this);
            }

            Core.Attitude.SetAxisControl(liftedOff, liftedOff, liftedOff && VesselState.altitudeBottom > AscentSettings.RollAltitude);
        }
    }
}
