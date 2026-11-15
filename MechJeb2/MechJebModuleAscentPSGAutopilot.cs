extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

/*
 * Optimized launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentPSGAutopilot : ComputerModule, IAscentAutopilot
    {
        public MechJebModuleAscentPSGAutopilot(MechJebCore core) : base(core)
        {
        }

        public AscentStatus Status { get; } = new AscentStatus();

        UserPool IAscentAutopilot.Users => Users;

        private MechJebModuleAscentSettings AscentSettings => Core.AscentSettings;

        private MechJebModuleLaunchCountdown Countdown => Core.LaunchCountdown;

        // the player's throttle and electric limits, off while PSG flies and given back when it stops
        private bool _savedLimitThrottle, _savedElectricThrottle, _limitsSaved;

        protected override void OnModuleEnabled()
        {
            Debug.Log("Enabling PSG Ascent Autopilot");
            _mode = AscentMode.PRELAUNCH;

            _savedLimitThrottle    = Core.Thrust.LimitThrottle;
            _savedElectricThrottle = Core.Thrust.ElectricThrottle;
            _limitsSaved           = true;
            AscentSettings.ForcePsgThrustLimits();

            // Each engage plans from the current settings and stages: Drive rebuilds the flight plan, so the plan of an earlier
            // engage, with the stages it marked done, is not flown again. The simulation, which freezes the plan, is disabled here.
            Core.PSGSimulation.FlightPlan = null;

            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
            if (AscentSettings.Autostage) Core.Staging.Users.Add(this);
            Countdown.Users.Add(this);
            Core.Guidance.Users.Add(this);
            Core.Guidance.CascadeDisable(this); // disable this module if guidance is disabled

            Status.Set(Localizer.Format("#MechJeb_Ascent_status1")); //"Pre Launch"
        }

        protected override void OnModuleDisabled()
        {
            Debug.Log("Disabling PSG Ascent Autopilot");
            Core.Attitude.AttitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.Users.Remove(this);
            Countdown.Users.Remove(this);
            if (_limitsSaved)
            {
                Core.Thrust.LimitThrottle    = _savedLimitThrottle;
                Core.Thrust.ElectricThrottle = _savedElectricThrottle;
                _limitsSaved                 = false;
            }

            Countdown.Abort();

            Status.Set(Localizer.Format("#MechJeb_Ascent_status2")); //"Off"

            Core.Guidance.Users.Remove(this);
            Core.PSGSimulation.Users.Remove(this);
        }

        private enum AscentMode
        {
            PRELAUNCH,
            VERTICAL_ASCENT,
            PITCHPROGRAM,
            GUIDANCE
        }

        private AscentMode _mode;

        public override void Drive(FlightCtrlState s)
        {
            AscentSettings.ForcePsgThrustLimits();

            // the guidance needs a flight plan, which nothing else makes unless the Ascent window is open
            if (Core.PSGSimulation.FlightPlan == null)
            {
                Core.StageStats.RequestUpdate();
                Core.PSGSimulation.UpdateLogicalPlan(AscentSettings);
            }

            if (Countdown.Active)
            {
                if (Countdown.TMinus <= AscentSettings.WarpCountDown)
                {
                    Core.PSGSimulation.Users.Add(this);
                    Core.Guidance.AssertStart(false);
                }
                else
                {
                    Core.PSGSimulation.Users.Remove(this);
                }
            }
            else
            {
                Core.PSGSimulation.Users.Add(this);
                Core.Guidance.AssertStart();
            }

            AscentSettings.LimitingAoA = false;

            if (_mode == AscentMode.PRELAUNCH)
            {
                if (Countdown.DrivePrelaunch(Status)) _mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            if (Countdown.HoldForLiftoff(Status)) return;

            Core.Solarpanel.DriveForAscent();
            Core.AntennaControl.DriveForAscent();

            // TODO: at some point we probably still need to:
            // 1. model vertical rise and pitchover in the solver
            // 2. freeze recomputation after launch
            // 3. fly through maxQ using attitude-as-function-of-velocity (rather than time)
            //    -- this should still handle thrust-fault failures by pitching up as we're not making enough velocity for our downrange
            //       and should wind up riding Q-alpha limits
            // 4. pick up closed-loop on the other side, after maxQ
            switch (_mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent();
                    break;

                case AscentMode.PITCHPROGRAM:
                    DrivePitchProgram();
                    break;

                case AscentMode.GUIDANCE:
                    DriveGuidance();
                    break;
            }
        }

        private double _pitchStartTime;

        private string _awaitingComputing;

        private void DriveVerticalAscent()
        {
            //during the vertical ascent we just thrust straight up at max throttle
            Core.Attitude.AttitudeToVerticalLaunch(Core.Guidance.Heading,
                AscentSettings.ForceRoll && VesselState.AltitudeBottom > AscentSettings.RollAltitude, AscentSettings.VerticalRoll, this);

            bool liftedOff = Vessel.LiftedOff() && !Vessel.Landed;

            if (!liftedOff)
            {
                // the first solve can take minutes on a slow machine: say so, or the pad looks stuck
                if (Core.Guidance.Solution == null)
                    Status.Set(_awaitingComputing ??= Localizer.Format("#MechJeb_Ascent_status12") + ": computing the ascent…"); //"Awaiting liftoff"
                else
                    Status.Set(Localizer.Format("#MechJeb_Ascent_status12")); //"Awaiting liftoff"
                return;
            }

            if (VesselState.AltitudeBottom > AscentSettings.PitchStartHeight)
            {
                _mode = AscentMode.PITCHPROGRAM;
                _pitchStartTime = VesselState.MET;
                return;
            }

            double dh = AscentSettings.PitchStartHeight - VesselState.AltitudeBottom;
            Status.Set($"Vertical ascent {dh:F2}m to go");
        }

        private void DrivePitchProgram()
        {
            double dt = VesselState.MET - _pitchStartTime;
            double theta = dt * AscentSettings.PitchRate;
            // not below the horizon while waiting for guidance (no solution yet, or none at all)
            double pitch = Math.Max(90 - theta, 0);

            // without a solution the guidance pitch is 0, so "N° to guidance" would be the pitch itself: say what is missing instead,
            // since the pitch program goes on down to the horizon
            if (Core.Guidance.Solution == null)
                Status.Set($"No guidance yet, pitch program at {pitch:F0}°. " +
                    MechJebModuleAscentMenu.OptimizerStateText(Core.PSGSimulation.State, Core.PSGSimulation.StateSeconds, true, false,
                        Core.PSGSimulation.LastFailureMessage != null ? Core.PSGSimulation.Staleness : 0),
                    AscentStatusLevel.WARNING);
            else
                Status.Set(Localizer.Format("#MechJeb_Ascent_status15", $"{pitch - Core.Guidance.Pitch:F}")); //Pitch program <<1>>° to guidance

            if (CheckForGuidanceTransition(pitch))
            {
                _mode = AscentMode.GUIDANCE;
                return;
            }

            Core.Attitude.LimitedAttitudeTo(Core.Guidance.Heading, pitch, AscentSettings.ForceRoll, AscentSettings.TurnRoll, ApplyAoALimiter, this);
        }

        private bool CheckForGuidanceTransition(double pitch)
        {
            if (!MainBody.atmosphere) return true;

            if (pitch <= Core.Guidance.Pitch && Core.Guidance.IsStable()) return true;

            return false;
        }

        private void DriveGuidance()
        {
            if (Core.Guidance.Status == PSGStatus.FINISHED)
            {
                Users.Clear();
                return;
            }

            // another mod has locked the steering without taking MechJeb's controls: the steering does nothing, and a spin-up never
            // reaches its rate, so it holds autostaging for good
            if (SetLockStatus())
                return;

            if (!Core.Guidance.IsStable())
            {
                double pitch = Math.Min(Math.Min(90, SrfvelPitch()), VesselState.Pitch);
                Core.Attitude.LimitedAttitudeTo(SrfvelHeading(), pitch, AscentSettings.ForceRoll, AscentSettings.TurnRoll, ApplyAoALimiter, this);
                Status.Set(Localizer.Format("#MechJeb_Ascent_status16"), AscentStatusLevel.WARNING); //"WARNING: Unstable Guidance"
            }
            else
            {
                double ang = Vector3d.Angle(Core.Guidance.Inertial, VesselState.Forward);
                AscentStatusLevel severity = ang > 5 ? AscentStatusLevel.ERROR : ang > 1 ? AscentStatusLevel.WARNING : AscentStatusLevel.NORMAL;
                Status.Set($"Stable Guidance: {ang:F}° deviation", severity);
                Core.Attitude.LimitedAttitudeTo(Core.Guidance.Inertial, AscentSettings.ForceRoll, AscentSettings.TurnRoll, ApplyAoALimiter, this);
            }
        }

        private string  _lockStatus, _lockStatusId;
        private bool    _lockStatusSpinup, _lockStatusTaken;

        // Drive doesn't run while another mod has taken MechJeb's controls (DeactivateControl: RP-1 sets it when the avionics on
        // board can't support the vessel, RemoteTech without a connection), so the status would freeze on its last value
        public override void OnFixedUpdate()
        {
            if (_mode == AscentMode.GUIDANCE && Core.DeactivateControl)
                SetLockStatus();
        }

        // says why MechJeb can't fly the vessel when another mod has taken its controls or locked the steering, and returns
        // whether it did; the string is rebuilt only when the reason changes
        private bool SetLockStatus()
        {
            bool taken = Core.DeactivateControl;
            string lockId = SteeringLockId();
            if (!taken && lockId == null)
                return false;

            bool spinupHeld = !taken && Core.Spinup.Enabled && Vessel.currentStage == AscentSettings.SpinupStage;
            if (lockId != _lockStatusId || spinupHeld != _lockStatusSpinup || taken != _lockStatusTaken || _lockStatus == null)
            {
                _lockStatusId     = lockId;
                _lockStatusSpinup = spinupHeld;
                _lockStatusTaken  = taken;
                string who = lockId ?? "another mod";
                _lockStatus = spinupHeld
                    ? $"Controls locked by {who}: the spin-up can't start, so staging is held"
                    : $"Controls locked by {who}: MechJeb can't steer or throttle";
            }

            Status.Set(_lockStatus, AscentStatusLevel.ERROR);
            return true;
        }

        /// <summary>
        ///     The id of an input lock that blocks steering on the vessel, or null (MechJeb sets none: its own lock only blocks clicks). RP-1's is
        ///     "RP0ControlLocker". Walks the lock dictionary with its struct enumerator, so no garbage.
        /// </summary>
        internal static string SteeringLockId()
        {
            if (!InputLockManager.IsLocked(ControlTypes.PITCH))
                return null;

            foreach (KeyValuePair<string, ulong> entry in InputLockManager.lockStack)
            {
                if ((entry.Value & (ulong)ControlTypes.PITCH) != 0)
                    return entry.Key;
            }

            return null;
        }

        private Vector3d ApplyAoALimiter(Vector3d desiredThrustVector)
        {
            double lim = Core.PSGSimulation.QAlphaLimit;
            if (lim <= 0)
                return desiredThrustVector;

            AscentSettings.LimitingAoA = VesselState.DynamicPressure * Vector3.Angle(VesselState.SurfaceVelocity, desiredThrustVector) * UtilMath.Deg2Rad > lim;
            if (AscentSettings.LimitingAoA)
            {
                AscentSettings.CurrentMaxAoA = lim / VesselState.DynamicPressure * UtilMath.Rad2Deg;
                desiredThrustVector = MathExtensions.RotateTowards(VesselState.SurfaceVelocity, desiredThrustVector, (float)(AscentSettings.CurrentMaxAoA * UtilMath.Deg2Rad), 1).normalized;
            }

            return desiredThrustVector;
        }

        private double SrfvelPitch() => 90.0 - Vector3d.Angle(VesselState.SurfaceVelocity, VesselState.Up);

        private double SrfvelHeading() => VesselState.HeadingFromDirection(VesselState.SurfaceVelocity.ProjectOnPlane(VesselState.Up));
    }
}
