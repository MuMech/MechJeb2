using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace MuMech
{
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
        public double trans_land_touchdown_speed = 0.5;
        public bool limitToTerminalVelocity = false;
        public bool limitToPreventOverheats = false;


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

                if (trans_land)
                {
                    if (vessel.LandedOrSplashed)
                    {
                        tmode = TMode.OFF;
                        trans_land = false;
                    }
                    else
                    {
                        double minalt = Math.Min(vesselState.altitudeBottom, Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue));

                        if (Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up) > 0)
                        {
                            //if we have positive vertical velocity, point up and don't thrust:
                            core.attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, null);
                            tmode = TMode.DIRECT;
                            trans_spd_act = 0;
                        }
                        else if (Vector3d.Angle(vesselState.forward, -vesselState.velocityVesselSurface) > 90)
                        {
                            //if we're not facing approximately retrograde, turn to point retrograde and don't thrust:
                            core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                            tmode = TMode.DIRECT;
                            trans_spd_act = 0;
                        }
                        else if (vesselState.maxThrustAccel < vesselState.gravityForce.magnitude)
                        {
                            //if we have TWR < 1, just try as hard as we can to decelerate:
                            //(we need this special case because otherwise the calculations spit out NaN's)
                            tmode = TMode.KEEP_VERTICAL;
                            trans_kill_h = true;
                            trans_spd_act = 0;
                        }
                        else if (minalt > 200)
                        {
                            //if we're above 200m, point retrograde and control surface velocity:
                            core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);

                            tmode = TMode.KEEP_SURFACE;
                            trans_spd_act = (float)Math.Sqrt((vesselState.maxThrustAccel - vesselState.gravityForce.magnitude) * 2 * minalt) * 0.90F;
                        }
                        else
                        {
                            //last 200 meters:
                            trans_spd_act = -Mathf.Lerp(0, (float)Math.Sqrt((vesselState.maxThrustAccel - vesselState.gravityForce.magnitude) * 2 * 200) * 0.90F, (float)minalt / 200);

                            //take into account desired landing speed:
                            trans_spd_act = (float)Math.Min(-trans_land_touchdown_speed, trans_spd_act);

                            if (Math.Abs(Vector3d.Angle(-vesselState.velocityVesselSurface, vesselState.up)) < 10)
                            {
                                //if we're falling more or less straight down, control vertical speed and 
                                //kill horizontal velocity
                                tmode = TMode.KEEP_VERTICAL;
                                trans_kill_h = true;
                            }
                            else
                            {
                                //if we're falling at a significant angle from vertical, our vertical speed might be
                                //quite small but we might still need to decelerate. Control the total speed instead
                                //by thrusting directly retrograde
                                core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                                tmode = TMode.KEEP_SURFACE;
                                trans_spd_act *= -1;
                            }
                        }

                        //deploy landing gears:
                        if (!trans_land_gears && (minalt < 1000))
                        {
                            vessel.rootPart.SendEvent("LowerLeg");
                            foreach (Part p in vessel.parts)
                            {
                                if (p is LandingLeg)
                                {
                                    LandingLeg l = (LandingLeg)p;
                                    if (l.legState == LandingLeg.LegStates.RETRACTED)
                                    {
                                        l.DeployOnActivate = true;
                                        l.force_activate();
                                    }
                                }
                            }
                            trans_land_gears = true;
                        }
                    }
                }

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
                            Vector3 hsdir = Vector3.Exclude(vesselState.up, orbit.GetVel() - mainBody.getRFrmVel(vesselState.CoM));
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
                s.mainThrottle = Mathf.Min(s.mainThrottle, terminalVelocityThrottle());
            }

            if (limitToPreventOverheats)
            {
                s.mainThrottle = Mathf.Min(s.mainThrottle, temperatureSafetyThrottle());
            }
        }

        float terminalVelocityThrottle()
        {
            if (vesselState.altitudeASL > mainBody.maxAtmosphereAltitude) return 1.0F;

            double velocityRatio = Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up) / vesselState.TerminalVelocity();

            if (velocityRatio < 1.0) return 1.0F; //full throttle if under terminal velocity

            //throttle down quickly as we exceed terminal velocity:
            double falloff = 15.0;
            return Mathf.Clamp((float)(1.0 - falloff * (velocityRatio - 1.0)), 0.0F, 1.0F);
        }


        //limit the throttle if something is close to overheating
        float temperatureSafetyThrottle()
        {
            float maxTempRatio = vessel.parts.Max(p => p.temperature / p.maxTemp);

            //reduce throttle as the max temp. ratio approaches 1 within the safety margin
            float tempSafetyMargin = 0.05f;
            if (maxTempRatio < 1 - tempSafetyMargin) return 1.0F;
            else return (1 - maxTempRatio) / tempSafetyMargin;
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
