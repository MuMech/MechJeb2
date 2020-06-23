using System;
using System.Linq;
using MuMech.AttitudeControllers;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAirplaneAutopilot : ComputerModule
    {
        [Persistent(pass = (int)Pass.Local)]
        public bool HeadingHoldEnabled = false, AltitudeHoldEnabled = false, VertSpeedHoldEnabled = false, RollHoldEnabled = false, SpeedHoldEnabled = false;

        [Persistent (pass = (int)Pass.Local)]
        public double AltitudeTarget = 0, HeadingTarget = 90, RollTarget = 0, SpeedTarget = 0, VertSpeedTarget = 0;

        [Persistent(pass = (int)Pass.Local)]
        public double BankAngle = 30;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble AccKp = 0.5, AccKi = 0.5, AccKd = 0.005;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble PitKp = 2, PitKi = 0.5, PitKd = 0.001;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble RolKp = 0.5, RolKi = 0.08, RolKd = 0.75;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble YawKp = 0.25, YawKi = 0.3, YawKd = 0.75;

        [Persistent (pass = (int)Pass.Local)]
        public EditableDouble YawLimit = 10, RollLimit = 45, PitchDownLimit = 15, PitchUpLimit = 25;

        public PIDController AccelerationPIDController, PitchPIDController, RollPIDController, YawPIDController;

        public double a_err, cur_acc, _spd;
        public double curr_yaw;
        public double pitch_err, roll_err, yaw_err;
        public double pitch_act, roll_act, yaw_act;
        public double RealVertSpeedTarget, RealPitchTarget, RealRollTarget, RealYawTarget, RealAccelerationTarget;

        public MechJebModuleAirplaneAutopilot (MechJebCore core) : base (core)
        {
            
        }

        public override void OnStart (PartModule.StartState state)
        {
            AccelerationPIDController = new PIDController (AccKp, AccKi, AccKd);
            PitchPIDController = new PIDController(PitKp, PitKi, PitKd);
            RollPIDController = new PIDController (RolKp, RolKi, RolKd);
            YawPIDController = new PIDController (YawKp, YawKi, YawKd);
        }

        public override void OnModuleEnabled ()
        {
            if (AltitudeHoldEnabled)
                EnableAltitudeHold ();
            if (SpeedHoldEnabled)
                EnableSpeedHold ();
        }

        public override void OnModuleDisabled ()
        {
            //core.attitude.attitudeDeactivate ();
            if (SpeedHoldEnabled) {
                core.thrust.users.Remove (this);
            }
        }

        public void EnableHeadingHold ()
        {
            HeadingHoldEnabled = true;
            RollHoldEnabled = true;
            YawPIDController.Reset ();
            RollPIDController.Reset ();
        }

        public void DisableHeadingHold ()
        {
            HeadingHoldEnabled = false;
            RollHoldEnabled = false;
        }

        public void EnableSpeedHold ()
        {
            if (!enabled)
                return;
            _spd = vesselState.speedSurface;
            SpeedHoldEnabled = true;
            core.thrust.users.Add (this);
            AccelerationPIDController.Reset ();
        }

        public void DisableSpeedHold ()
        {
            if (!enabled)
                return;
            SpeedHoldEnabled = false;
            core.thrust.users.Remove (this);
        }

        public void EnableVertSpeedHold ()
        {
            if (!enabled)
                return;
            VertSpeedHoldEnabled = true;
            PitchPIDController.Reset();
        }

        public void DisableVertSpeedHold ()
        {
            if (!enabled)
                return;
            VertSpeedHoldEnabled = false;
        }

        public void EnableAltitudeHold ()
        {
            if (!enabled)
                return;
            AltitudeHoldEnabled = true;
            if (VertSpeedHoldEnabled) {
                PitchPIDController.Reset();
            } else {
                EnableVertSpeedHold ();
            }
        }

        public void DisableAltitudeHold ()
        {
            if (!enabled)
                return;
            AltitudeHoldEnabled = false;
            DisableVertSpeedHold ();
        }

        private double convertAltitudeToVerticalSpeed (double deltaAltitude)
        {
            double reference = 5;
            if (reference < 2)
                reference = 2;
            if (deltaAltitude > reference || deltaAltitude < -reference)
                return deltaAltitude / reference;
            else if (deltaAltitude > 0.1)
                return deltaAltitude * deltaAltitude / (reference * reference);
            else if (deltaAltitude > -0.1)
                return 0;
            else
                return -deltaAltitude * deltaAltitude / (reference * reference);
        }

        private double fixPIDfromSpeed (double speed)
        {
            if (speed > 600)
                return 0.5;
            else
                return speed * -0.0015 + 1.5;
        }

        void UpdatePID ()
        {
            AccelerationPIDController.Kp = AccKp;
            AccelerationPIDController.Ki = AccKi;
            AccelerationPIDController.Kd = AccKd;
            PitchPIDController.Kp = PitKp;
            PitchPIDController.Ki = PitKi;
            PitchPIDController.Kd = PitKd;
            RollPIDController.Kp = RolKp;
            RollPIDController.Ki = RolKi;
            RollPIDController.Kd = RolKd;
            YawPIDController.Kp = YawKp;
            YawPIDController.Ki = YawKi;
            YawPIDController.Kd = YawKd;
        }

        public override void Drive (FlightCtrlState s)
        {
            UpdatePID ();

            //SpeedHold (set AccelerationTarget automatically to hold speed)
            if (SpeedHoldEnabled) {
                double spd = vesselState.speedSurface;
                cur_acc = (spd - _spd) / Time.fixedDeltaTime;
                _spd = spd;
                RealAccelerationTarget = (SpeedTarget - spd) / 4;
                a_err = (RealAccelerationTarget - cur_acc);
                AccelerationPIDController.intAccum = MuUtils.Clamp (AccelerationPIDController.intAccum, -1 / AccKi, 1 / AccKi);
                double t_act = AccelerationPIDController.Compute (a_err);
                if (!double.IsNaN (t_act)) {
                    core.thrust.targetThrottle = (float)MuUtils.Clamp (t_act, 0, 1);
                } else {
                    core.thrust.targetThrottle = 0.0f;
                    AccelerationPIDController.Reset ();
                }
            }

            //AltitudeHold (set VertSpeed automatically to hold altitude)
            if (AltitudeHoldEnabled) {
                RealVertSpeedTarget = convertAltitudeToVerticalSpeed(AltitudeTarget - vesselState.altitudeASL);
                RealVertSpeedTarget = UtilMath.Clamp(RealVertSpeedTarget, -VertSpeedTarget, VertSpeedTarget);
            }
            else
                RealVertSpeedTarget = VertSpeedTarget;

            pitch_err = roll_err = yaw_err = 0;
            pitch_act = roll_act = yaw_act = 0;

            //VertSpeedHold
            if (VertSpeedHoldEnabled) {
                // NOTE: 60-to-1 rule:
                // deltaAltitude = 2 * PI * r * deltaPitch / 360
                // Vvertical = 2 * PI * TAS * deltaPitch / 360
                // deltaPitch = Vvertical / Vhorizontal * 180 / PI
                double deltaVertSpeed = RealVertSpeedTarget - vesselState.speedVertical;
                double adjustment = 180 / vesselState.speedSurface * deltaVertSpeed / Math.PI;
                RealPitchTarget = vesselState.vesselPitch + adjustment;

                RealPitchTarget = UtilMath.Clamp(RealPitchTarget, -PitchDownLimit, PitchUpLimit);
                pitch_err = MuUtils.ClampDegrees180(RealPitchTarget - vesselState.vesselPitch);

                PitchPIDController.intAccum = UtilMath.Clamp(PitchPIDController.intAccum, -100 / PitKi , 100 / PitKi);
                pitch_act = PitchPIDController.Compute (pitch_err) / 100;

                //Debug.Log (p_act);
                if (double.IsNaN (pitch_act)) {
                    PitchPIDController.Reset ();
                } else {
                    s.pitch = Mathf.Clamp ((float)pitch_act, -1, 1);
                }
            }

            //HeadingHold
            double curFlightPath = vesselState.HeadingFromDirection (vesselState.forward);
            // NOTE: we can not use vesselState.vesselHeading here because it interpolates headings internally
            //       i.e. turning from 1° to 359° will end up as (1+359)/2 = 180°
            //       see class MovingAverage for more details
            curr_yaw = vesselState.currentHeading - curFlightPath;

            if (HeadingHoldEnabled) {
                double toturn = MuUtils.ClampDegrees180 (HeadingTarget - curFlightPath);

                if (Math.Abs(toturn) < 0.2) {
                    // yaw for small adjustments
                    RealYawTarget = MuUtils.Clamp(toturn * 2, -YawLimit, YawLimit);
                    RealRollTarget = 0;
                } else {
                    // roll for large adjustments
                    RealYawTarget = 0;
                    RealRollTarget = MuUtils.Clamp (toturn * 2, -RollLimit, RollLimit);
                }
            } else {
                RealRollTarget = RollTarget;
                RealYawTarget = 0;
            }

            if (RollHoldEnabled) {
                RealRollTarget = UtilMath.Clamp(RealRollTarget, -BankAngle, BankAngle);
                RealRollTarget = UtilMath.Clamp(RealRollTarget, -RollLimit, RollLimit);
                roll_err = MuUtils.ClampDegrees180(RealRollTarget - -vesselState.currentRoll);

                RollPIDController.intAccum = MuUtils.Clamp (RollPIDController.intAccum, -100 / RolKi, 100 / RolKi);
                roll_act = RollPIDController.Compute (roll_err) / 100;

                if (double.IsNaN(roll_act))
                    RollPIDController.Reset();
                else
                    s.roll = Mathf.Clamp((float)roll_act, -1, 1);
            }

            if (HeadingHoldEnabled) {
                RealYawTarget = UtilMath.Clamp(RealYawTarget, -YawLimit, YawLimit);
                yaw_err = MuUtils.ClampDegrees180 (RealYawTarget - curr_yaw);
                
                YawPIDController.intAccum = MuUtils.Clamp (YawPIDController.intAccum, -100 / YawKi, 100 / YawKi);
                yaw_act = YawPIDController.Compute (yaw_err) / 100;

                if (double.IsNaN(yaw_act))
                    YawPIDController.Reset();
                else
                    s.yaw = Mathf.Clamp((float)yaw_act, -1, 1);
            }
        }
    }
}
