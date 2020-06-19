using System;
using System.Linq;
using MuMech.AttitudeControllers;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAirplaneAutopilot : ComputerModule
    {
        public bool HeadingHoldEnabled = false, AltitudeHoldEnabled = false, VertSpeedHoldEnabled = false, RollHoldEnabled = false, SpeedHoldEnabled = false;

        [Persistent (pass = (int)Pass.Global)]
        public double AltitudeTarget = 0, HeadingTarget = 90, RollTarget = 0, SpeedTarget = 0, VertSpeedTarget = 0;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble AccKp = 0.5, AccKi = 0.5, AccKd = 0.005;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble VerKp = 1, VerKi = 0.1, VerKd = 0.01;

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble PitKp = 2, PitKi = 0.5, PitKd = 0.001;

        private double VerPIDScale = 1;


        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble RolKp = 0.2, RolKi = 0.01, RolKd = 0.001;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble YawKp = 2, YawKi = 0.01, YawKd = 0.001;

        [Persistent (pass = (int)Pass.Global)]
        public EditableDouble YawLimit = 45, RollLimit = 5, PitchDownLimit = 10, PitchUpLimit = 25;

        public PIDController AccelerationPIDController, VertSpeedPIDController, PitchPIDController, RollPIDController, YawPIDController;

        public MechJebModuleAirplaneAutopilot (MechJebCore core) : base (core)
        {
            
        }

        public override void OnStart (PartModule.StartState state)
        {
            AccelerationPIDController = new PIDController (AccKp, AccKi, AccKd);
            VertSpeedPIDController = new PIDController (VerKp, VerKi, VerKd);
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
            VertSpeedPIDController.Reset ();
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
                VertSpeedPIDController.Reset ();
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

        public double a_err, cur_acc, _spd;
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
                RealVertSpeedTarget = fixVertSpeed(AltitudeTarget - vesselState.altitudeASL);
            else
                RealVertSpeedTarget = VertSpeedTarget;

            //VertSpeedHold
            if (VertSpeedHoldEnabled) {
                double pitch_err = 0;
                double deltaVertSpeed = RealVertSpeedTarget - vesselState.speedVertical;

                if (deltaVertSpeed > 0)
                {
                    double remainingPitchRange = (PitchUpLimit - vesselState.vesselPitch) * .9;

                    if (remainingPitchRange > 0)
                    {
                        double sc = UtilMath.Clamp(Math.Abs(deltaVertSpeed / RealVertSpeedTarget), 0, 1);
                        double exp = (Math.Log(remainingPitchRange + 1) - Math.Log(1)) * sc;
                        pitch_err = Math.Exp(exp) - 1;
                    }
                }
                else
                {
                    double remainingPitchRange = (vesselState.vesselPitch - -PitchDownLimit) * .9;

                    if (remainingPitchRange > 0)
                    {
                        double sc = UtilMath.Clamp(Math.Abs(deltaVertSpeed / RealVertSpeedTarget), 0, 1);
                        double exp = (Math.Log(remainingPitchRange + 1) - Math.Log(1)) * sc;
                        pitch_err = -(Math.Exp(exp) - 1);
                    }
                }

                // above pitch up limit
                if (PitchUpLimit < vesselState.vesselPitch)
                    pitch_err = UtilMath.Min(pitch_err, PitchUpLimit - vesselState.vesselPitch);
                // below pitch down limit
                else if (-PitchDownLimit > vesselState.vesselPitch)
                    pitch_err = UtilMath.Max(pitch_err, -PitchDownLimit - vesselState.vesselPitch);

                //PitchPIDController.intAccum = MuUtils.Clamp (PitchPIDController.intAccum, -1 / (VerKi * VerPIDScale), 1 / (VerKi * VerPIDScale));
                PitchPIDController.intAccum = UtilMath.Clamp(PitchPIDController.intAccum, -100 / PitKi , 100 / PitKi);
                double p_act = PitchPIDController.Compute (pitch_err);

                print("init: " + PitchPIDController.intAccum + " err: " + pitch_err + " act: " + p_act / 100);
                //Debug.Log (p_act);
                if (double.IsNaN (p_act)) {
                    PitchPIDController.Reset ();
                } else {
                    s.pitch = Mathf.Clamp ((float)p_act / 100, -1, 1);
                }
            }

            //HeadingHold
            double curHeading = vesselState.HeadingFromDirection (vesselState.forward);
            if (HeadingHoldEnabled) {
                double toturn = MuUtils.ClampDegrees180 (HeadingTarget - curHeading);
                RealRollTarget = MuUtils.Clamp (toturn * 2, -RollLimit, RollLimit);
            } else {
                RealRollTarget = RollTarget;
            }

            if (RollHoldEnabled) {
                double roll_err = MuUtils.ClampDegrees180 (vesselState.vesselRoll + RealRollTarget);
                RollPIDController.intAccum = MuUtils.Clamp (RollPIDController.intAccum, -100 / RolKi, 100 / RolKi);
                double roll_act = RollPIDController.Compute (roll_err);
                s.roll = Mathf.Clamp ((float)roll_act / 100, -1, 1);
            }

            if (HeadingHoldEnabled) {
                float YawActLimit = 0.5f;
                double yaw_err = MuUtils.ClampDegrees180 (HeadingTarget - curHeading);
                yaw_err = UtilMath.Clamp(yaw_err, -YawLimit, YawLimit);
                YawPIDController.intAccum = MuUtils.Clamp (YawPIDController.intAccum, -YawActLimit * 100 / YawKi, YawActLimit * 100 / YawKi);
                double yaw_act = YawPIDController.Compute (yaw_err);
                s.yaw = Mathf.Clamp((float)yaw_act / 100, -YawActLimit, +YawActLimit);
            }
        }

        private double calculateSteeringError(double current, double target, double minLimit, double maxLimit)
        {
            double margin = 0.1;
            double error = 0;
            double delta = target - current;
            double remainingRange = delta > 0 ? (maxLimit - current) * (1 - margin) : (current - minLimit) * (1 - margin);

            if (remainingRange > 0)
            {
                double sc = UtilMath.Clamp(Math.Abs(delta / target), 0, 1);
                double exp = (Math.Log(remainingRange + 1) - Math.Log(1)) * sc;
                error = Math.Exp(exp) - 1;

                if (delta < 0)
                    error = -error;
            }

            // corrective steering back into operational limits [minLimit, maxLimit]
            if (current > maxLimit)
                error = UtilMath.Min(error, (maxLimit - current) * (1 + margin));
            else if (current < minLimit)
                error = UtilMath.Max(error, minLimit - current);

            return error;
        }
    }
}
