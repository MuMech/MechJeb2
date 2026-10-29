using System;
using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public abstract class MechJebModuleAscentBaseAutopilot : ComputerModule
    {
        protected MechJebModuleAscentBaseAutopilot(MechJebCore core) : base(core) { }

        public string Status = "";

        protected MechJebModuleAscentSettings AscentSettings => Core.AscentSettings;

        public bool TimedLaunch;
        private double _launchTime;

        public double CurrentMaxAoA;

        public double TMinus => _launchTime - VesselState.Time;

        //internal state:
        private enum AscentMode { PRELAUNCH, ASCEND }

        private AscentMode _mode;
        private double _lastTMinus = 999;

        protected override void OnModuleEnabled()
        {
            _mode = AscentMode.PRELAUNCH;

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

            Status = Localizer.Format("#MechJeb_Ascent_status2"); //"Off"
        }

        public void StartCountdown(double time)
        {
            if (AscentSettings.OverrideWarpToPlane)
            {
                TimedLaunch = false;
                _launchTime = VesselState.Time;
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
            if (TimedLaunch)
            {
                if (TMinus < 3 * VesselState.DeltaT || (TMinus > 10.0 && _lastTMinus < 1.0))
                {
                    if (Enabled && VesselState.ThrustAvailable < 10E-4) // only stage if we have no engines active
                        StageManager.ActivateNextStage();
                    TimedLaunch = false;
                }
                else
                {
                    if (Core.Node.Autowarp)
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
            }
        }

        private void DriveDeployableComponents()
        {
            if (AscentSettings.AutoDeploySolarPanels)
            {
                if (VesselState.AltitudeASL > MainBody.RealMaxAtmosphereAltitude())
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
                if (VesselState.AltitudeASL > MainBody.RealMaxAtmosphereAltitude())
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
                _mode = AscentMode.ASCEND;
                return;
            }

            Core.Thrust.ThrustOff();

            Core.Attitude.SetAxisControl(false, false, false);

            if (TimedLaunch && TMinus > 10.0)
            {
                Status = Localizer.Format("#MechJeb_Ascent_status1"); //"Pre Launch"
                return;
            }

            if (AscentSettings.AutoDeploySolarPanels && MainBody.atmosphere)
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
                Status = Localizer.Format("#MechJeb_Ascent_status6"); //"Awaiting liftoff"

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
                if (GameSettings.VERBOSE_DEBUG_LOG) { Debug.Log("Ascend -> Done"); }

                Users.Clear();
            }
        }

        protected abstract bool DriveAscent2();

        protected double SrfvelPitch() => 90.0 - Vector3d.Angle(VesselState.SurfaceVelocity, VesselState.Up);

        protected double SrfvelHeading() => VesselState.HeadingFromDirection(VesselState.SurfaceVelocity.ProjectOnPlane(VesselState.Up));

        protected void AttitudeTo(double desiredPitch, double desiredHeading)
        {
            Vector3d desiredHeadingVector = Math.Sin(desiredHeading * UtilMath.Deg2Rad) * VesselState.East +
                Math.Cos(desiredHeading * UtilMath.Deg2Rad) * VesselState.North;

            Vector3d desiredThrustVector = Math.Cos(desiredPitch * UtilMath.Deg2Rad) * desiredHeadingVector
                + Math.Sin(desiredPitch * UtilMath.Deg2Rad) * VesselState.Up;

            AttitudeTo(desiredThrustVector);
        }

        // provides AoA limiting and roll control
        protected void AttitudeTo(Vector3d desiredThrustVector)
        {
            desiredThrustVector = ApplyAoALimiter(desiredThrustVector.normalized);

            // calculate pitch and heading after applying AoA limiter
            double pitch = 90 - Vector3d.Angle(desiredThrustVector, VesselState.Up);
            double hdg = Clamp360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(desiredThrustVector, VesselState.East),
                Vector3d.Dot(desiredThrustVector, VesselState.North)));

            if (AscentSettings.ForceRoll)
                Core.Attitude.attitudeTo(hdg, pitch, AscentSettings.TurnRoll, this, fixCOT: true);
            else
                Core.Attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL_COT, this);

            Core.Attitude.SetActuationControl();
            Core.Attitude.SetAxisControl(true, true, AscentSettings.ForceRoll);
        }

        protected abstract Vector3d ApplyAoALimiter(Vector3d desiredThrustVector);
    }
}
