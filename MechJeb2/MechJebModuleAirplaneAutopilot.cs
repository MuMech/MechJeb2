using System.Linq;
using MuMech.AttitudeControllers;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAirplaneAutopilot : ComputerModule
    {
        public bool HeadingHoldEnabled = false, AltitudeHoldEnabled = false, VertSpeedHoldEnabled = false, RollHoldEnabled = false, SpeedHoldEnabled = false;

        [Persistent (pass = (int)Pass.Global)]
        public double AltitudeTarget = 0, HeadingTarget = 90, RollTarget = 0, SpeedTarget = 0, VertSpeedTarget = 0, VertSpeedMax = 10, RollMax = 30;

        private double pitch, heading, roll;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble AccKp = 1, AccKi = 0.1, AccKd = 0.01;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble VerKp = 1, VerKi = 0.1, VerKd = 0.01;

        double _spd = 0;

        public PIDController AccelerationPIDController, VertSpeedPIDController;

        public MechJebModuleAirplaneAutopilot (MechJebCore core) : base (core)
        {
            
        }

        public override void OnModuleEnabled ()
        {
            AccelerationPIDController = new PIDController (AccKp, AccKi, AccKd);
            VertSpeedPIDController = new PIDController (VerKp, VerKi, VerKd);
            core.attitude.users.Add (this);
            if (AltitudeHoldEnabled)
                EnableAltitudeHold ();
            if (SpeedHoldEnabled)
                EnableSpeedHold ();
        }

        public override void OnModuleDisabled ()
        {
            core.attitude.attitudeDeactivate ();
            if (SpeedHoldEnabled) {
                core.thrust.users.Remove (this);
            }
        }

        public void EnableHeadingHold ()
        {
            HeadingHoldEnabled = true;
            RollHoldEnabled = true;
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
            if (VertSpeedHoldEnabled)
                VertSpeedPIDController.Reset ();
            else
                EnableVertSpeedHold ();
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

        public void UpdatePID ()
        {
            VertSpeedPIDController.Kp = VerKp;
            VertSpeedPIDController.Ki = VerKi;
            VertSpeedPIDController.Kd = VerKd;
            AccelerationPIDController.Kp = AccKp;
            AccelerationPIDController.Ki = AccKi;
            AccelerationPIDController.Kd = AccKd;
        }

        public double a_err, v_err, exp_act, cur_acc;
        public double RealVertSpeedTarget, RealRollTarget, RealAccelerationTarget;

        public override void OnFixedUpdate ()
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
                VertSpeedPIDController.intAccum = MuUtils.Clamp (VertSpeedPIDController.intAccum, -60 / AccKi, 60 / AccKi);
                exp_act = Mathf.Asin (Mathf.Clamp ((float)(RealVertSpeedTarget / vesselState.speedSurface), -1, 1)) * UtilMath.Rad2Deg;
                double p_act = exp_act + VertSpeedPIDController.Compute (v_err);
                if (!double.IsNaN (p_act)) {
                    pitch = MuUtils.Clamp (p_act, -60, 60);
                } else {
                    pitch = vesselState.vesselPitch;
                    VertSpeedPIDController.Reset ();
                }
            } else {
                pitch = vesselState.vesselPitch;
            }

            //HeadingHold
            double curHeading = vesselState.HeadingFromDirection (vesselState.forward);
            if (HeadingHoldEnabled) {
                double toturn = MuUtils.ClampDegrees180 (HeadingTarget - curHeading);
                RealRollTarget = MuUtils.Clamp (toturn * 2, -RollMax, RollMax);
                heading = MuUtils.ClampDegrees360 (curHeading + RealRollTarget / 10);
                if (-3 < toturn && toturn < 3)
                    heading = HeadingTarget;
            } else {
                RealRollTarget = RollTarget;
                heading = MuUtils.ClampDegrees360 (curHeading + RealRollTarget / 10);
            }
            if (RollHoldEnabled) {
                roll = RealRollTarget;
            } else {
                roll = vesselState.vesselPitch;
            }


            core.attitude.attitudeTo (heading, pitch, roll, this, AltitudeHoldEnabled || VertSpeedHoldEnabled, HeadingHoldEnabled, RollHoldEnabled && vessel.situation != Vessel.Situations.LANDED && vessel.situation != Vessel.Situations.PRELAUNCH);
        }
    }
}
