using KSP.Localization;
using KSP.UI.Screens;
using UnityEngine;

namespace MuMech
{
    public abstract class MechJebModuleAscentBaseAutopilot : ComputerModule
    {
        protected MechJebModuleAscentBaseAutopilot(MechJebCore core) : base(core) { }

        public enum StatusLevel { NORMAL, WARNING, ERROR }

        public string Status { get; private set; } = "";

        public StatusLevel StatusSeverity { get; private set; }

        protected void SetStatus(string status, StatusLevel severity = StatusLevel.NORMAL)
        {
            Status = status;
            StatusSeverity = severity;
        }

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

            SetStatus(Localizer.Format("#MechJeb_Ascent_status1")); //"Pre Launch"
        }

        protected override void OnModuleDisabled()
        {
            Core.Attitude.AttitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.Users.Remove(this);

            SetStatus(Localizer.Format("#MechJeb_Ascent_status2")); //"Off"
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
                SetStatus(Localizer.Format("#MechJeb_Ascent_status4")); //"Vessel is not landed, skipping pre-launch"
                _mode = AscentMode.ASCEND;
                return;
            }

            Core.Thrust.ThrustOff();

            Core.Attitude.SetAxisControl(false, false, false);

            if (TimedLaunch && TMinus > 10.0)
            {
                SetStatus(Localizer.Format("#MechJeb_Ascent_status1")); //"Pre Launch"
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
                    SetStatus(Localizer.Format("#MechJeb_Ascent_status5")); //"Retracting solar panels"
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
                SetStatus(Localizer.Format("#MechJeb_Ascent_status6")); //"Awaiting liftoff"

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
    }
}
