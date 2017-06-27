using System;
using KSP.UI.Screens;
using UnityEngine;

/*
 * Atlas/Centaur-style PEG launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentPEG : MechJebModuleAscentBase
    {
        public MechJebModuleAscentPEG(MechJebCore core) : base(core) { }

        /* default pitch program here works decently at SLT of about 1.4 */
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult pitchStartTime = new EditableDoubleMult(10);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult pitchRate = new EditableDoubleMult(0.75);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult pitchEndTime = new EditableDoubleMult(55);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult desiredApoapsis = new EditableDoubleMult(100000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult terminalGuidanceSecs = new EditableDoubleMult(10);

        /* this deliberately does not persist, it is for emergencies only */
        public EditableDoubleMult pitchBias = new EditableDoubleMult(0);

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }

        public override void OnModuleEnabled()
        {
            mode = AscentMode.VERTICAL_ASCENT;
        }

        public override void OnModuleDisabled()
        {
        }

        enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GRAVITY_TURN, EXIT };
        AscentMode mode;

        /* guidancePitchAngle -- output from the guidance algorithm, not 'manual' pitch */
        public double guidancePitch { get { return Math.Asin(A + C) * UtilMath.Rad2Deg; } }

        /* current MJ stage index */
        private int last_stage;
        /* current exhaust velocity */
        private double v_e;
        /* time to burn the entire vehicle */
        private double tau;
        /* current acceleration */
        private double a0;
        /* tangential velocity at burnout */
        private double vT;
        /* radius at burnout */
        private double rT;
        /* radius now */
        private double r;
        /* gravParameter */
        private double GM;

        /* ending radial velocity */
        double rdT;
        /* current radial velocity */
        double rd;
        /* r: this is in the radial direction (altitude to gain) */
        double dr;
        /* rdot: also in the radial direction (upwards velocity to lose) */
        double drd;

        /* angular velocity at burnout */
        double wT;
        /* current angular velocity */
        double w;
        /* mean radius */
        double rbar;
        /* angular momentum at burnout */
        double hT;
        /* angular momentum */
        double h;
        /* angular momentum to gain */
        double dh;
        /* acceleration at burnout */
        double aT;
        /* current gravity + centrifugal force term */
        double C;
        /* gravity + centrifugal force at burnout */
        double CT;

        public double sma() {
            if ( desiredApoapsis > autopilot.desiredOrbitAltitude )
                return (autopilot.desiredOrbitAltitude + 2 * mainBody.Radius + desiredApoapsis) / 2;
            else
                return autopilot.desiredOrbitAltitude + mainBody.Radius;
        }

        private void update_rocket_stats() {
            /* sometimes the last stage in MJ has 0.0 dV and we have to search back for the actively burning stage */
            for(int i = vacStats.Length - 1; i >= 0; i--)
            {
                if ( vacStats[i].deltaV > 0 )
                {
                    last_stage = i;
                    break;
                }
            }

            GM = mainBody.gravParameter;
            v_e = vacStats[last_stage].isp * 9.80665;
            a0 = vesselState.currentThrustAccel;
            tau = v_e / a0;
            rT = autopilot.desiredOrbitAltitude + mainBody.Radius;
            vT = Math.Sqrt(GM * (2/rT - 1/sma()));  /* FIXME: assumes periapsis insertion */
            r = mainBody.position.magnitude;

            rdT = 0;  /* FIXME: assumes periapsis insertion */
            rd = vesselState.speedVertical;

            wT = vT / rT;
            w = Vector3.Cross(mainBody.position, vessel.obt_velocity).magnitude / (r * r);
            rbar = ( rT + r ) / 2.0D;
            hT = rT * vT;  /* FIXME: assumes periapsis insertion */
            h = Vector3.Cross(mainBody.position, vessel.obt_velocity).magnitude;
            dh = hT - h;
            aT = a0 / ( 1.0D - T / tau );

            C = (GM / (r * r) - w * w * r ) / a0;
            CT = (GM / (rT * rT) - wT * wT * rT ) / aT;

            Debug.Log("GM = " + GM + " v_e = " + v_e + " tau = " + tau + "vT = " + vT);
            Debug.Log("aT = " + aT + " rT = " + rT + " rdT = " + rdT + " wT = " + wT + " hT = " + hT);
            Debug.Log("a0 = " + a0 + " r = " + r + " rd = " + rd + " w = " + w + " h = " + h + " rbar = " + rbar);
            Debug.Log("C = " + C + " CT = " + CT);
        }

        private void peg_solve()
        {

            double b0 = -v_e * Math.Log(1.0D - T/tau);
            double b1 = b0 * tau - v_e * T;
            double c0 = b0 * T - b1;
            double c1 = c0 * tau - v_e * T * T / 2.0D;

            double d = b0 * c1 - b1 * c0;

            A = ( c1 * ( rdT - rd ) - b1 * ( rT - r - rd * T ) ) / d;
            B = ( -c0 * ( rdT - rd ) + b0 * ( rT - r - rd * T ) ) / d;
        }

        /* steering constants */
        private double A;
        private double B;
        /* time to burnout */
        public double T;
        /* dV to add */
        public double dV;

        private void peg_estimate(double dt, bool debug = false)
        {
            /* update old guidance */
            A = A + B * dt;
            B = B;
            T = T - dt;

            aT = a0 / ( 1.0D - T / tau );
            CT = (GM / (rT * rT) - wT * wT * rT ) / aT;

            /* current sin pitch  */
            double f_r = A + C;
            /* sin pitch at burnout */
            double f_rT = A + B * T + CT;
            /* appx rate of sin pitch */
            double fd_r = ( f_rT - f_r ) / T;

            /* placeholder for future yaw term */
            double f_h = 0.0D;
            /* placeholder for future yaw rate term */
            double fd_h = 0.0D;

            /* cos pitch */
            double f_th = 1.0D - f_r * f_r / 2.0D - f_h * f_h / 2.0D;
            /* cos pitch rate */
            double fd_th = - ( f_r * fd_r + f_h * fd_h );
            /* cos pitch accel */
            double fdd_th = - ( fd_r * fd_r + fd_h * fd_h ) / 2.0D;

            if (debug) {
               Debug.Log("f_th = " + f_th + " fd_th = " + fd_th + " fdd_th = " + fdd_th);
               Debug.Log("f_r = " + f_r + " f_rT = " + f_rT + " fd_r = " + fd_r);
            }

            /* updated estimate of dV to burn */
            dV = ( dh / rbar + v_e * T * ( fd_th + fdd_th * tau ) + fdd_th * v_e * T * T / 2.0D ) / ( f_th + fd_th * tau + fdd_th * tau * tau );

            /* updated estimate of T */
            T = tau * ( 1 - Math.Exp( - dV / v_e ) );
        }

        private bool bad_dV()
        {
            return dV <= 0.0D;
        }

        private bool bad_pitch()
        {
            return Double.IsNaN(guidancePitch);
        }

        private bool bad_guidance()
        {
            return Double.IsNaN(T) || Double.IsInfinity(T) || T <= 0.0D || Double.IsNaN(A) || Double.IsInfinity(A) || Double.IsNaN(B) || Double.IsInfinity(B);
        }

        public bool guidanceEnabled = true;
        public bool saneGuidance = false;
        public int convergenceSteps;
        public bool terminalGuidance = false;

        private void converge(double dt, bool initialize = false)
        {
            if (initialize || bad_guidance() || bad_pitch() || bad_dV() || !saneGuidance)
            {
                T = 120.0D;
                A = 0.0D;
                B = 0.0D;
                dt = 0.0;
            }

            double startingT = T;
            double startingA = A;
            double startingB = B;

            bool stable = false;

            if (T < terminalGuidanceSecs)
            {
                peg_estimate(dt);
                terminalGuidance = true;
                stable = true; /* terminal guidance is always considered stable */
            }
            else
            {
                for(convergenceSteps = 1; convergenceSteps <= 250; convergenceSteps++) {
                    double oldT = T;
                    if (convergenceSteps == 0)
                        peg_estimate(dt);
                    else
                        peg_estimate(0);

                    peg_solve();

                    if ( Math.Abs(T - oldT) < 0.01 ) {
                        stable = true;
                        break;
                    }
                }
                terminalGuidance = false;
            }

            Debug.Log("pitch = " + Math.Asin(A+C) + " dV = " + dV + " cycles = " + convergenceSteps);

            if (!stable || bad_guidance() || bad_dV() || bad_pitch())
            {
                /* FIXME: probably shouldn't scribble over globals then restore them if they're bad --
                   should scribble in local vars and then set them if they're good. */
                A = startingA;
                B = startingB;
                T = startingT;
                saneGuidance = false;
            }
            else
            {
                saneGuidance = true;
            }
        }

        double last_time = 0.0D;

        public override bool DriveAscent(FlightCtrlState s)
        {
            stats.RequestUpdate(this);
            update_rocket_stats();

            if (last_time != 0.0D)
                converge(vesselState.time - last_time);
            else
                converge(0);

            last_time = vesselState.time;

            switch (mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent(s);
                    break;

                case AscentMode.INITIATE_TURN:
                    DriveInitiateTurn(s);
                    break;

                case AscentMode.GRAVITY_TURN:
                    DriveGravityTurn(s);
                    break;
            }

            if (mode == AscentMode.EXIT)
                return false;
            else
                return true;
        }

        double ascentStartTime = 0.0D;

        void DriveVerticalAscent(FlightCtrlState s)
        {
            if (ascentStartTime > 0.0D && (vesselState.time - ascentStartTime ) > pitchStartTime)
                mode = AscentMode.INITIATE_TURN;

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90);

            core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && vesselState.altitudeBottom > 50);

            if (autopilot.autoThrottle) core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff() || vessel.Landed) {
                status = "Awaiting liftoff";
            }
            else
            {
                if (ascentStartTime == 0.0D)
                    ascentStartTime = vesselState.time;
                double dt = pitchStartTime - ( vesselState.time - ascentStartTime );
                status = String.Format("Vertical ascent {0:F2} s", dt);
            }
        }

        void DriveInitiateTurn(FlightCtrlState s)
        {
            if ((vesselState.time - ascentStartTime ) > pitchEndTime)
            {
                mode = AscentMode.GRAVITY_TURN;
                return;
            }

            double dt = vesselState.time - ascentStartTime - pitchStartTime;
            double theta = dt * pitchRate;
            attitudeTo(Math.Min(90, 90 - theta + pitchBias));

            status = String.Format("Pitch program {0:F2} s", pitchEndTime - pitchStartTime - dt);
        }

        void DriveGravityTurn(FlightCtrlState s)
        {
            if (h >= hT)
            {
                status = "Angular momentum target achieved";
                core.thrust.targetThrottle = 0.0F;
                mode = AscentMode.EXIT;
                return;
            }

            if (saneGuidance && guidanceEnabled) {
                if (terminalGuidance)
                    status = "Locked Terminal Guidance";
                else
                    status = "Stable PEG Guidance";

                attitudeTo(guidancePitch);
            }
            else
            {
                // srfvelPitch == zero AoA
                status = "Unguided Gravity Turn";
                attitudeTo(Math.Min(90, srfvelPitch() + pitchBias));
            }
        }
    }
}
