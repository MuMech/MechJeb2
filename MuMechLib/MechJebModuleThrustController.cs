using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace MuMech
{
    //ThrustController should be enabled/disabled through .users, not .enabled.
    public class MechJebModuleThrustController : ComputerModule
    {
        public MechJebModuleThrustController(MechJebCore core)
            : base(core)
        {
            priority = 200;
        }


        //turn these into properties? implement settings saving
        public float trans_spd_act = 0;
        public float trans_prev_thrust = 0;
        public bool trans_kill_h = false;
        public bool trans_land = false;
        public bool trans_land_gears = false;
        [ToggleInfoItem(name="Limit throttle to keep below terminal velocity")]
        public bool limitToTerminalVelocity = false;
        [ToggleInfoItem(name="Limit throttle to prevent overheats")]
        public bool limitToPreventOverheats = false;
        public bool limitAcceleration = false;
        public double maxAcceleration = double.MaxValue;


        private bool tmode_changed = false;

        private double t_integral = 0;
        private double t_prev_err = 0;

        public float t_Kp = 0.05F;
        public float t_Ki = 0.000001F;
        public float t_Kd = 0.05F;


        public enum TMode
        {
            OFF,
            KEEP_ORBITAL,
            KEEP_SURFACE,
            KEEP_VERTICAL,
            DIRECT
        }

        private TMode prev_tmode = TMode.OFF;
        private TMode _tmode = TMode.OFF;
        public TMode tmode
        {
            get
            {
                return _tmode;
            }
            set
            {
                if (_tmode != value)
                {
                    prev_tmode = _tmode;
                    _tmode = value;
                    tmode_changed = true;
                }
            }
        }



        public override void Drive(FlightCtrlState s)
        {
            if ((tmode != TMode.OFF) && (vesselState.thrustAvailable > 0))
            {
                double spd = 0;

                switch (tmode)
                {
                    case TMode.KEEP_ORBITAL:
                        spd = vesselState.speedOrbital;
                        break;
                    case TMode.KEEP_SURFACE:
                        spd = vesselState.speedSurface;
                        break;
                    case TMode.KEEP_VERTICAL:
                        spd = vesselState.speedVertical;
                        Vector3d rot = Vector3d.up;
                        if (trans_kill_h)
                        {
                            Vector3 hsdir = Vector3.Exclude(vesselState.up, vesselState.velocityVesselSurface);
                            Vector3 dir = -hsdir + vesselState.up * Math.Max(Math.Abs(spd), 20 * mainBody.GeeASL);
                            if ((Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue) > 5000) && (hsdir.magnitude > Math.Max(Math.Abs(spd), 100 * mainBody.GeeASL) * 2))
                            {
                                tmode = TMode.DIRECT;
                                trans_spd_act = 100;
                                rot = -hsdir;
                            }
                            else
                            {
                                if (spd > 0)
                                {
                                    rot = vesselState.up;
                                }
                                else
                                {
                                    rot = dir.normalized;
                                }
                            }
                            core.attitude.attitudeTo(rot, AttitudeReference.INERTIAL, null);
                        }
                        break;
                }

                double t_err = (trans_spd_act - spd) / vesselState.maxThrustAccel;
                if ((tmode == TMode.KEEP_ORBITAL && Vector3d.Dot(vesselState.forward, vesselState.velocityVesselOrbit) < 0) ||
                   (tmode == TMode.KEEP_SURFACE && Vector3d.Dot(vesselState.forward, vesselState.velocityVesselSurface) < 0))
                {
                    //allow thrust to declerate 
                    t_err *= -1;
                }
                t_integral += t_err * TimeWarp.fixedDeltaTime;
                double t_deriv = (t_err - t_prev_err) / TimeWarp.fixedDeltaTime;
                double t_act = (t_Kp * t_err) + (t_Ki * t_integral) + (t_Kd * t_deriv);
                t_prev_err = t_err;

                if ((tmode != TMode.KEEP_VERTICAL)
                    || !trans_kill_h
                    || (core.attitude.attitudeError < 2)
                    || ((Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue) < 1000) && (core.attitude.attitudeError < 90)))
                {
                    if (tmode == TMode.DIRECT)
                    {
                        trans_prev_thrust = s.mainThrottle = trans_spd_act / 100.0F;
                    }
                    else
                    {
                        trans_prev_thrust = s.mainThrottle = Mathf.Clamp(trans_prev_thrust + (float)t_act, 0, 1.0F);
                    }
                }
                else
                {
                    if ((core.attitude.attitudeError >= 2) && (vesselState.torqueThrustPYAvailable > vesselState.torquePYAvailable * 10))
                    {
                        trans_prev_thrust = s.mainThrottle = 0.1F;
                    }
                    else
                    {
                        trans_prev_thrust = s.mainThrottle = 0;
                    }
                }
            }


            if (limitToTerminalVelocity)
            {
                s.mainThrottle = Mathf.Min(s.mainThrottle, TerminalVelocityThrottle());
            }

            if (limitToPreventOverheats)
            {
                s.mainThrottle = Mathf.Min(s.mainThrottle, TemperatureSafetyThrottle());
            }

            if (limitAcceleration)
            {
                s.mainThrottle = Mathf.Min(s.mainThrottle, AccelerationLimitedThrottle());
            }
        }

        //A throttle setting that throttles down when the vertical velocity of the ship exceeds terminal velocity
        float TerminalVelocityThrottle()
        {
            if (vesselState.altitudeASL > mainBody.maxAtmosphereAltitude) return 1.0F;

            double velocityRatio = Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up) / vesselState.TerminalVelocity();

            if (velocityRatio < 1.0) return 1.0F; //full throttle if under terminal velocity

            //throttle down quickly as we exceed terminal velocity:
            double falloff = 15.0;
            return Mathf.Clamp((float)(1.0 - falloff * (velocityRatio - 1.0)), 0.0F, 1.0F);
        }


        //a throttle setting that throttles down if something is close to overheating
        float TemperatureSafetyThrottle()
        {
            float maxTempRatio = vessel.parts.Max(p => p.temperature / p.maxTemp);

            //reduce throttle as the max temp. ratio approaches 1 within the safety margin
            float tempSafetyMargin = 0.05f;
            if (maxTempRatio < 1 - tempSafetyMargin) return 1.0F;
            else return (1 - maxTempRatio) / tempSafetyMargin;
        }
        
        //The throttle setting that will give an acceleration of maxAcceleration
        float AccelerationLimitedThrottle()
        {
            double throttleForMaxAccel = (maxAcceleration - vesselState.minThrustAccel) / (vesselState.maxThrustAccel - vesselState.minThrustAccel);
            return Mathf.Clamp((float)throttleForMaxAccel, 0, 1);
        }

        public override void OnUpdate()
        {
            if (tmode_changed)
            {
                if (trans_kill_h && (tmode == TMode.OFF))
                {
                    core.attitude.attitudeDeactivate(null);
                    trans_land = false;
                }
                t_integral = 0;
                t_prev_err = 0;
                tmode_changed = false;
                FlightInputHandler.SetNeutralControls();
            }
        }
    }
}
