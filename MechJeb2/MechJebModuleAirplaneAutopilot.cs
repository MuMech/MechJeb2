using System.Linq;
using MuMech.AttitudeControllers;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAirplaneAutopilot : ComputerModule
    {
        public bool HeadingHoldEnabled = false, AltitudeHoldEnabled = false, VertSpeedHoldEnabled = false, RollHoldEnabled = false, SpeedHoldEnabled = false;

        [Persistent (pass = (int)Pass.Global)]
        public double AltitudeTarget = 0, HeadingTarget = 90, RollTarget = 0, SpeedTarget = 0, VertSpeedTarget = 0, VertSpeedMax = 10, RollMax = 5;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble AccKp = 0.5, AccKi = 0.5, AccKd = 0.005;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble VerKp = 1, VerKi = 0.1, VerKd = 0.01;

        private double VerPIDScale = 1;


        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble RolKp = 0.2, RolKi = 0.01, RolKd = 0.001;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble YawKp = 2, YawKi = 0.01, YawKd = 0.001;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble YawLimit = 0.5;

        public PIDController AccelerationPIDController, VertSpeedPIDController, RollPIDController, YawPIDController;

        public MechJebModuleAirplaneAutopilot (MechJebCore core) : base (core)
        {
            
        }

        public override void OnStart (PartModule.StartState state)
        {
            AccelerationPIDController = new PIDController (AccKp, AccKi, AccKd);
            VertSpeedPIDController = new PIDController (VerKp, VerKi, VerKd);
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
            VertSpeedPIDController.Reset ();
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
                VertSpeedPIDController.Reset ();
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

        double fixVertSpeed (double deltaAltitude)
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
            VerPIDScale = (0.0015885994 * vessel.totalMass + 0.0031753299) * fixPIDfromSpeed (vesselState.speedSurface);
            VertSpeedPIDController.Kp = VerKp * VerPIDScale;
            VertSpeedPIDController.Ki = VerKi * VerPIDScale;
            VertSpeedPIDController.Kd = VerKd * VerPIDScale;
            AccelerationPIDController.Kp = AccKp;
            AccelerationPIDController.Ki = AccKi;
            AccelerationPIDController.Kd = AccKd;
            RollPIDController.Kp = RolKp;
            RollPIDController.Ki = RolKi;
            RollPIDController.Kd = RolKd;
            YawPIDController.Kp = YawKp;
            YawPIDController.Ki = YawKi;
            YawPIDController.Kd = YawKd;
        }

        public double a_err, v_err, cur_acc, _spd;
        public double RealVertSpeedTarget, RealRollTarget, RealAccelerationTarget;

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
            if (AltitudeHoldEnabled)
                RealVertSpeedTarget = MuUtils.Clamp (fixVertSpeed (AltitudeTarget - vesselState.altitudeASL), -VertSpeedMax, VertSpeedMax);
            else
                RealVertSpeedTarget = VertSpeedTarget;

            //VertSpeedHold
            if (VertSpeedHoldEnabled) {
                double vertspd = vesselState.speedVertical;
                v_err = RealVertSpeedTarget - vertspd;
                VertSpeedPIDController.intAccum = MuUtils.Clamp (VertSpeedPIDController.intAccum, -1 / (VerKi * VerPIDScale), 1 / (VerKi * VerPIDScale));
                double p_act = VertSpeedPIDController.Compute (v_err);
                //Debug.Log (p_act);
                if (double.IsNaN (p_act)) {
                    VertSpeedPIDController.Reset ();
                } else {
                    s.pitch = Mathf.Clamp ((float)p_act, -1, 1);
                }
            }

            //HeadingHold
            double curHeading = vesselState.HeadingFromDirection (vesselState.forward);
            if (HeadingHoldEnabled) {
                double toturn = MuUtils.ClampDegrees180 (HeadingTarget - curHeading);
                RealRollTarget = MuUtils.Clamp (toturn * 2, -RollMax, RollMax);
            } else {
                RealRollTarget = RollTarget;
            }

            if (RollHoldEnabled) {
                double roll_err = MuUtils.ClampDegrees180 (vesselState.vesselRoll + RealRollTarget);
                RollPIDController.intAccum = MuUtils.Clamp (RollPIDController.intAccum, -100 / AccKi, 100 / AccKi);
                double roll_act = RollPIDController.Compute (roll_err);
                s.roll = Mathf.Clamp ((float)roll_act / 100, -1, 1);
            }

            if (HeadingHoldEnabled) {
                double yaw_err = MuUtils.ClampDegrees180 (HeadingTarget - curHeading);
                YawPIDController.intAccum = MuUtils.Clamp (YawPIDController.intAccum, -YawLimit * 100 / AccKi, YawLimit * 100 / AccKi);
                double yaw_act = YawPIDController.Compute (yaw_err);
                s.yaw = (float)MuUtils.Clamp (yaw_act / 100, -YawLimit, +YawLimit);
            }
        }
    }
}
