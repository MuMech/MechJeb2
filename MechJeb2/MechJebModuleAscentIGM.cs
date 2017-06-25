using System;
using KSP.UI.Screens;
using UnityEngine;

/*
 * Apollo-style IGM launches for RSS/RO
 */

namespace MuMech
{
    public class MechJebModuleAscentIGM : MechJebModuleAscentBase
    {
        public MechJebModuleAscentIGM(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnStartAltitude = new EditableDoubleMult(500, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnStartVelocity = new EditableDoubleMult(50);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
            public EditableDoubleMult turnStartPitch = new EditableDoubleMult(25);


        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }


        public override void OnModuleEnabled()
        {
            mode = AscentMode.VERTICAL_ASCENT;
        }

        public override void OnModuleDisabled()
        {
        }

        public bool IsVerticalAscent(double altitude, double velocity)
        {
            if (altitude < turnStartAltitude && velocity < turnStartVelocity)
            {
                return true;
            }
            return false;
        }

        enum AscentMode { VERTICAL_ASCENT, INITIATE_TURN, GRAVITY_TURN, EXIT };
        AscentMode mode;

        /* current MJ stage index */
        private int last_stage;
        /* current exhaust velocity */
        private double v_e;
        /* time to burn the entire vehicle */
        private double tau;
        /* current acceleration */
        private double a0;
        /* tangential velocity at burnout FIXME: circular for now */
        private double vT;
        /* radius at burnout */
        private double rT;
        /* radius now */
        private double r;
        /* gravParameter */
        private double GM;

        /* ending radial velocity (FIXME: circular for now) */
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
        /* angular momentum at burnout FIXME: circular for now */
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

        private void update_rocket_stats() {
            /* sometimes the last stage in MJ has 0.0 dV and we have to search back for the actively burning stage */
            for(int i = vacStats.Length - 1; i >= 0; i--)
            {
                if ( vacStats[i].deltaV > 0.0D )
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
            vT = Math.Sqrt(GM/rT);  /* FIXME: assumes circular */
            r = mainBody.position.magnitude;

            rdT = 0;  /* FIXME: assumes circular */
            rd = vesselState.speedVertical;

            wT = vT / rT;
            w = Vector3.Cross(mainBody.position, vessel.obt_velocity).magnitude / (r * r);
            rbar = ( rT + r ) / 2.0D;
            hT = rT * vT;  /* FIXME: assumes circular */
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
        private double T;
        /* dV to add */
        private double dV;

        private void peg_estimate(double dt, bool debug = false)
        {
            double oldA = A;
            double oldB = B;
            double oldT = T;

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
            return Double.IsNaN(Math.Asin(A+C));
        }

        private bool bad_guidance()
        {
            return Double.IsNaN(T) || Double.IsInfinity(T) || T <= 0.0D || Double.IsNaN(A) || Double.IsInfinity(A) || Double.IsNaN(B) || Double.IsInfinity(B);
        }

        private bool sane_guidance = false;

        private void converge(double dt, bool initialize = false)
        {
            if (initialize || bad_guidance() || bad_pitch() || bad_dV() || !sane_guidance)
            {
                T = 120.0D;
                A = 0.0D;
                B = 0.0D;
                dt = 0.0;
            }

            double startingT = T;
            double startingA = A;
            double startingB = B;

            bool converged = false;
            int i;
            for(i = 0; i < 50; i++) {
                double oldT = T;
                if (i == 0)
                    peg_estimate(dt);
                else
                    peg_estimate(0);
                peg_solve();
                if ( Math.Abs(T - oldT) < 0.1 ) {
                    converged = true;
                    break;
                }
            }

            Debug.Log("pitch = " + Math.Asin(A+C) + " dV = " + dV + " cycles = " + i);

            if (!converged || bad_guidance() || bad_dV() || bad_pitch())
            {
                /* FIXME: probably shouldn't scribble over globals then restore them if they're bad */
                A = startingA;
                B = startingB;
                T = startingT;
                sane_guidance = false;
            }
            else
            {
                sane_guidance = true;
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

        void DriveVerticalAscent(FlightCtrlState s)
        {
            if (!IsVerticalAscent(vesselState.altitudeASL, vesselState.speedSurface)) mode = AscentMode.INITIATE_TURN;

            //during the vertical ascent we just thrust straight up at max throttle
            attitudeTo(90);

            core.attitude.AxisControl(!vessel.Landed, !vessel.Landed, !vessel.Landed && vesselState.altitudeBottom > 50);

            if (autopilot.autoThrottle) core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff() || vessel.Landed) status = "Awaiting liftoff";
            else status = "Vertical ascent";
        }

        void DriveInitiateTurn(FlightCtrlState s)
        {
            if ((90 - turnStartPitch) >= srfvelPitch())
            {
                mode = AscentMode.GRAVITY_TURN;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (IsVerticalAscent(vesselState.altitudeASL, vesselState.speedSurface))
            {
                mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            attitudeTo(90 - turnStartPitch);

            status = "Initiate gravity turn";
        }

        void DriveGravityTurn(FlightCtrlState s)
        {
            if (h >= hT)
            {
                core.thrust.targetThrottle = 0.0F;
                mode = AscentMode.EXIT;
                return;
            }

            if (sane_guidance) {
                attitudeTo(Math.Asin(A + C) * UtilMath.Rad2Deg);
            }
            else
            {
                // srfvelPitch == zero AoA
                attitudeTo(srfvelPitch());
            }

            status = "Gravity turn";
        }
    }
}
