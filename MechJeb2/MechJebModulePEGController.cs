using System;
using UnityEngine;
using System.Collections.Generic;

/*
 * PEG algorithm references:
 *
 * - https://ntrs.nasa.gov/archive/nasa/casi.ntrs.nasa.gov/19760024151.pdf (Langston1976)
 * - https://ntrs.nasa.gov/archive/nasa/casi.ntrs.nasa.gov/19740004402.pdf (Brand1973)
 * - https://ntrs.nasa.gov/search.jsp?R=19790048206 (Mchenry1979)
 * - https://arc.aiaa.org/doi/abs/10.2514/6.1977-1051 (Jaggers1977)
 * - https://ntrs.nasa.gov/search.jsp?R=19760020204 (Jaggers1976)
 * - https://ntrs.nasa.gov/search.jsp?R=19740024190 (Jaggers1974)
 * - i've found the Jaggers thrust integrals aren't stable
 *
 * For future inspiration:
 *
 * - https://arc.aiaa.org/doi/abs/10.2514/6.2012-4843 (Ping Lu's updated PEG)
 *
 */

/*
 *  Higher Priority / Nearer Term TODO list:
 *
 *  - Full RCS burn execution
 *  - Buttons in maneuver planner to execute nodes with/without PEG and with/without RCS
 *  - Dumb PEG-7 style burn of exact deltav in given direction
 *  - Button to finish up remaining fraction of dV in ascent/burn with RCS (normal burn followed by PEG-7-style RCS burn)
 *  - better thrust integrals?
 *  - manual entry of coast phase (probably based on kerbal-stage rather than final-stage since final-stage may change if we e.g. eat into TLI)
 *
 *  Medium Priority / Medium Term TODO list:
 *
 *  - landings and rendezvous
 *     - engine throttling
 *     - lambert solver integration
 *  - injection into orbits at other than the periapsis
 *  - matching planes with contract orbits
 *  - direct ascent to Lunar intercept
 *  - J^2 fixes for Principia
 *
 *  Wishlist for PEG Nirvana:
 *
 *  - throttling down core engine asymmetrically until booster sep (Delta IV Heavy)
 *  - constant accelleration phase through throttle down (space shuttle style g-limiting)
 *  - timed stage-and-a-half booster separation (Atlas I/II)
 *  - PEG for landing
 *  - launch to free-return around Moon (with and without N-body Principia)
 *  - direct ascent to interplanetary trajectories, 'cuz why the hell not?
 */

namespace MuMech
{
    public enum PegStatus { ENABLED, INITIALIZING, INITIALIZED, CONVERGED, STAGING, TERMINAL, FINISHED, FAILED };
    public enum TargetMode { PERIAPSIS, ORBIT, VGO };
    public enum IncMode { FIXED_LAN, FREE_LAN };

    public class MechJebModulePEGController : ComputerModule
    {
        public MechJebModulePEGController(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pegInterval = new EditableDouble(0.01);

        public Vector3d lambda;
        public Vector3d lambdaDot;
        public double t_lambda;
        public Vector3d primer;
        public Vector3d iF { get { return primer.normalized; } }
        public double phi { get { return lambdaDot.magnitude * K; } }
        public double primerMag { get { return primer.magnitude; } }
        public double pitch;
        public double heading;

        public PegStatus status;

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

        public override void OnModuleEnabled()
        {
            status = PegStatus.ENABLED;
            core.AddToPostDrawQueue(DrawCSE);
        }

        public override void OnModuleDisabled()
        {
        }

        public void AssertStart()
        {
            if (status == PegStatus.ENABLED )
                Reset();
        }

        public override void OnFixedUpdate()
        {
            if ( !enabled || status == PegStatus.ENABLED )
                return;

            if ( status == PegStatus.FINISHED )
            {
                Done();
                return;
            }

            // FIXME: we need to add liveStats to vacStats and atmoStats from MechJebModuleStageStats so we don't have to force liveSLT here
            stats.liveSLT = true;
            stats.RequestUpdate(this, true);

            if ( status == PegStatus.FAILED )
                Reset();

            if (tmode == TargetMode.VGO)
                converge_vgo();
            else
                converge();

            update_pitch_and_heading();
        }

        // state for next iteration
        public double tgo;          // time to burnout
        public double tgo_prev;     // time for last full converge
        public Vector3d vgo;        // velocity remaining to be gained
        private Vector3d rd;        // burnout position vector
        private Vector3d rgrav;
        private Vector3d rbias;
        int last_stage;
        // following for graphing + stats
        public Vector3d vd;
        private Vector3d rp;
        private Vector3d vp;
        double deltaTcoast;

        private double last_PEG;    // this is the last PEG update time
        public double K;

        public List<StageInfo> stages = new List<StageInfo>();

        /*
         * TARGET APIs
         */

        // private
        public IncMode imode;
        public TargetMode tmode;

        // target burnout radius
        private double rdval;
        // target burnout velocity
        private double vdval;
        // target burnout angle
        private double gamma;
        // tangent plane of desired orbit (opposite of the orbit normal)
        public Vector3d iy;
        // unit vector of rd
        private Vector3d ix;
        // unit vector downrange
        private Vector3d iz;
        // inclination target for FREE_LAN
        private double incval;
        // linear terminal velocity targets
        private Orbit target_orbit;


        // does its own initialization and is idempotent
        public void TargetNode(ManeuverNode node)
        {
            if ( !isStable() && !(status == PegStatus.FINISHED) )
            {
                imode = IncMode.FIXED_LAN;
                tmode = TargetMode.ORBIT;
                target_orbit = node.nextPatch;
                vgo = node.GetBurnVector(orbit);
            }
            iy = -target_orbit.SwappedOrbitNormal();
        }

        /* meta state for consumers that means "is iF usable?" (or pitch/heading) */
        public bool isStable()
        {
            return status == PegStatus.CONVERGED || status == PegStatus.TERMINAL || status == PegStatus.STAGING;
        }

        /* normal pre-states but not usefully converged */
        public bool isInitializing()
        {
            return status == PegStatus.ENABLED || status == PegStatus.INITIALIZING || status == PegStatus.INITIALIZED;
        }

        public void TargetPeInsertMatchPlane(double PeA, double ApA, Vector3d tangent)
        {
            imode = IncMode.FIXED_LAN;
            iy = -tangent.normalized;
            SetRdvalVdval(PeA, ApA);
        }

        public void TargetPeInsertMatchOrbitPlane(double PeA, double ApA, Orbit o)
        {
            TargetPeInsertMatchPlane(PeA, ApA, o.SwappedOrbitNormal());
        }

        public void TargetVgo(Vector3d vgo)
        {
            tmode = TargetMode.VGO;
            this.vgo = vgo;
            this.vgain = vgo.magnitude;
            lambda = vgo.normalized;
            lambdaDot = Vector3d.zero;
            t_lambda = vesselState.time;
        }

        public void TargetPeInsertMatchInc(double PeA, double ApA, double inc)
        {
            /* imode = IncMode.FREE_LAN;
            if (incval != inc)
            { */
                incval = inc;
                SetPlaneFromInclination(inc);  /* updating every time would defeat PEG's corrector */
            /* } */
            SetRdvalVdval(PeA, ApA);
        }

        /* converts PeA + ApA into rdval/vdval for periapsis insertion.
           - handles hyperbolic orbits
           - remaps ApA < PeA onto circular orbits */
        private void SetRdvalVdval(double PeA, double ApA)
        {
            tmode = TargetMode.PERIAPSIS;
            double PeR = mainBody.Radius + PeA;
            double ApR = mainBody.Radius + ApA;

            rdval = PeR;

            double sma = (PeR + ApR) / 2;

            /* remap nonsense ApAs onto circular orbits */
            if ( ApA >= 0 && ApA < PeA )
                sma = PeR;

            vdval = Math.Sqrt( mainBody.gravParameter * ( 2 / PeR - 1 / sma ) );
            gamma = 0;  /* periapsis */
        }

        /* this will produce an inclination plane based strictly off of the current vessel position, it is somewhat far from optimal,
           but is a useful first guess to seed the predictor (not sure if the below-latitude stuff is strictly necessary, but this is
           code i first used to attempt to directly set iy, but which performs poorly) */
        private void SetPlaneFromInclination(double inc)
        {
            double desiredHeading = UtilMath.Deg2Rad * OrbitalManeuverCalculator.HeadingForInclination(inc, vesselState.latitude);
            Vector3d desiredHeadingVector = Math.Sin(desiredHeading) * vesselState.east + Math.Cos(desiredHeading) * vesselState.north;
            Vector3d tangent = Vector3d.Cross(vesselState.orbitalPosition, desiredHeadingVector).normalized;
            if ( Math.Abs(inc) < Math.Abs(vesselState.latitude) )
            {
                if (Vector3.Angle(tangent, Planetarium.up) < 90)
                    tangent = Vector3.RotateTowards(Planetarium.up, tangent, (float)(inc * UtilMath.Deg2Rad), 10.0f);
                else
                    tangent = Vector3.RotateTowards(-Planetarium.up, tangent, (float)(inc * UtilMath.Deg2Rad), 10.0f);
            }
            iy = -tangent;
        }

        private double last_call;        // this is the last call to converge
        private int last_stage_count;    // num stages we had last time
        private double last_stage_time;  // last time we staged

        // debugging
        public double vgain;

        // sort of like PEG-7.  vgain is set to the initial magnitude of vgo and it counts down (decrementing vector math is
        // KSP is going to be complicated by the rotating world axes and vectors from multiple ticks ago not being in the same basis, #FML)
        private void converge_vgo()
        {
            double dt = vesselState.time - last_call;
            double dV_atom = ( vessel.acceleration_immediate - vessel.graviticAcceleration ).magnitude * dt;

            if ( last_call != 0 )
            {
                vgain -= dV_atom;
                tgo = vgain * dV_atom;  // constant thrust approximation
            }

            UpdateStages();

            if ( vgain < 0 )
                Done();

            last_call = vesselState.time;
        }

        private void converge()
        {
            double dt = vesselState.time - last_call;
            Vector3d dV_atom = ( vessel.acceleration_immediate - vessel.graviticAcceleration ) * dt;

            if ( last_call != 0 )
            {
                tgo -= dt;
                vgo -= dV_atom;
            }

            UpdateStages();

            bool has_rcs = vessel.hasEnabledRCSModules() && vessel.ActionGroups[KSPActionGroup.RCS];
            int tickstop = 1;

            // due to increasing accelleration due to high constant thrust we stop at 2 * tick rather than 1 * tick to always stop before
            if (has_rcs)
                tickstop = 2;

            if ( tgo < ( tickstop * TimeWarp.fixedDeltaTime ) && last_call != 0 )
            {
                if ( has_rcs )
                {
                    // finish remaining vgo on RCS
                    vessel.ctrlState.Z = -1.0F;
                    core.thrust.ThrustOff();
                    // we have arrived near enough and all we can do is trim out velocity
                    rp = vesselState.orbitalPosition;
                    vp = vesselState.orbitalVelocity;
                    iy = - orbit.SwappedOrbitNormal();
                    vgo = Vector3d.zero;
                    // kind of abusing the corrector, but this should work to share code
                    corrector();
                    TargetVgo(vgo);
                    return;
                }
                Done();
                return;
            }

            if ( core.thrust.targetThrottle > 0 || vessel.ctrlState.Z != 0.0f )
                last_call = vesselState.time;

            if ( last_stage_count > stages.Count )
                last_stage_time = vesselState.time;

            last_stage_count = stages.Count;
            /* 3 second quiet time after staging */
            if ( vesselState.time < last_stage_time + 3.0 )
            {
                status = PegStatus.STAGING;
                return;
            }

            if ( status == PegStatus.TERMINAL )
                return;

            // only do active guidance every pegInterval
            if ( (vesselState.time - last_PEG) < pegInterval && core.thrust.targetThrottle > 0 )
                return;

            // skipping active guidance for last 10 seconds is neccessary due to thrust integral instability
            if ( status == PegStatus.CONVERGED && tgo < 10 )
            {
                status = PegStatus.TERMINAL;
                return;
            }

            bool converged = false;

            try {
                for(int i = 0; i < 20 && !converged && status != PegStatus.FAILED; i++)
                    converged = update();
            }
            catch
            {
                status = PegStatus.FAILED;
            }

            if (!converged)
                status = PegStatus.FAILED;

            if (vgo.magnitude == 0.0)
                status = PegStatus.FAILED;

            if (status != PegStatus.FAILED)
                status = PegStatus.CONVERGED;

            last_PEG = vesselState.time;
        }

        /* extract pitch and heading off of iF to avoid continuously recomputing on every call */
        private void update_pitch_and_heading()
        {
            double z = vesselState.time - t_lambda;
            // this more complicated implementation from Jaggers seems to offer no practical advantage as far as I can tell
            /* double sinTheta = Vector3d.Dot(lambda, vesselState.up);
            double w = Math.Sqrt( mainBody.gravParameter / ( vesselState.radius * vesselState.radius * vesselState.radius ) );
            primer = lambda * Math.Cos(w * z) + lambdaDot / w * Math.Sin(w * z)  + 3.0 * w * z / 2.0 * sinTheta * Math.Sin(w * z) * vesselState.up; */
            primer = lambda + lambdaDot * z;
            pitch = 90.0 - Vector3d.Angle(iF, vesselState.up);
            Vector3d headingDir = iF - Vector3d.Project(iF, vesselState.up);
            heading = UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(headingDir, vesselState.east), Vector3d.Dot(headingDir, vesselState.north));
        }

        private bool update()
        {
            Vector3d r = vesselState.orbitalPosition;
            double rm = vesselState.orbitalPosition.magnitude;
            Vector3d v = vesselState.orbitalVelocity;
            double gm = mainBody.gravParameter;

            if ( status == PegStatus.INITIALIZING )
            {
                rp = r + v * tgo;
                vp = v;
                t_lambda = vesselState.time;
                rbias = Vector3d.zero;
                vgo = v.normalized;
                tgo = 1;
                rgrav = -vesselState.orbitalPosition * Math.Sqrt( gm / ( rm * rm * rm ) ) / 2.0;
                corrector();
            }

            // compute cumulative tgo + tgo's
            tgo = 0;
            for(int i = 0; i <= last_stage; i++)
            {
                stages[i].tgo1 = tgo;
                tgo += stages[i].dt;
                stages[i].tgo = tgo;
            }

            // total thrust integrals
            double J, L, S, Q, H, P;
            L = J = S = Q = H = P = 0.0;

            for(int i = 0; i <= last_stage; i++)
            {
                stages[i].Si = - stages[i].Li * ( stages[i].tau - stages[i].dt ) + stages[i].ve * stages[i].dt;
                stages[i].Ji = stages[i].Li * stages[i].tgo - stages[i].Si;
                stages[i].Qi = stages[i].Si * ( stages[i].tau + stages[i].tgo1 ) - stages[i].ve * stages[i].dt * stages[i].dt / 2.0;
                stages[i].Pi = stages[i].Qi * ( stages[i].tau + stages[i].tgo1 ) - stages[i].ve * stages[i].dt * stages[i].dt / 2.0 * ( stages[i].tgo1 + stages[i].dt / 3.0);

                stages[i].Hi = stages[i].Ji * stages[i].tgo - stages[i].Qi;
                stages[i].Si += L * stages[i].dt;
                stages[i].Qi += J * stages[i].dt;
                stages[i].Pi += H * stages[i].dt;
                L += stages[i].Li;
                S += stages[i].Si;
                H += stages[i].Hi;
                J += stages[i].Ji;
                Q += stages[i].Qi;
                P += stages[i].Pi;
            }

            K = J / L;
            double QP = Q - S * K;

            /*
            double ldm = lambdaDot.magnitude;
            if (ldm < 1e-6)
                ldm = 1e-6;

            double Kp = tgo / 2;
            double theta = ldm * Kp;
            double f1 = Math.Sin(theta) / theta;
            double f2 = 3.0 * ( f1 - Math.Cos(theta) ) / ( theta * theta );
            double Lp = f1 * L;
            double Sp = f1 * S;
            double D = L * Kp - S;
            double Jp = f2 * D;
            double Qp = ( - L * Kp * Kp / 3.0 + D * Kp ) * f2;
            double delta = ldm * ( K - tgo / 2.0);
            double F1 = f1 * Math.Cos( delta );
            double F2 = f2 * Math.Cos( delta );
            double F3 = F1 * ( 1.0 - theta * delta / 3.0 );
            double LT = F1 * L;
            double QT = F2 * ( Q - S * K );
            double ST = F3 * S;
            */

            // steering
            lambda = vgo.normalized;

            if ( status != PegStatus.INITIALIZING )
                rgrav = tgo * tgo / ( tgo_prev * tgo_prev ) * rgrav;

            Vector3d rgo = rd - ( r + v * tgo + rgrav ) + rbias;

            // from Jaggers 1977
            /* this saves about 10 dV to orbit but seems to cost in targetting accuracy
            if ( imode == IncMode.FREE_LAN && tgo > 40 && isStable() )
            {
                Vector3d ip = Vector3d.Cross(lambda, iy).normalized;
                double Q1 = Vector3d.Dot(rgo - Vector3d.Dot(lambda, rgo) * lambda, ip);
                rgo = S * lambda + Q1 * ip;
            }
            */

            if ( status != PegStatus.CONVERGED || tgo > 40 || last_stage > 0 )
            {
                // orthogonality:  S = Vector3d.Dot(lambda, rgo)
                //rgo = rgo + ( ST - Vector3d.Dot(lambda, rgo) ) * lambda;
                rgo = rgo + ( S - Vector3d.Dot(lambda, rgo) ) * lambda;
                if ( QP == 0 )
                    lambdaDot = Vector3d.zero;
                else
                    // this comes from rgo = ST * lambda + QT * lambdaDot (simplified rthrust)
                    lambdaDot = ( rgo - S * lambda ) / QP;
            }

            double ldm = lambdaDot.magnitude;
            if (ldm < 1e-6)
                ldm = 1e-6;

            double phiMax = 45.0 * UtilMath.Deg2Rad;

            // try to clamp lambdaDot to something reasonable if we start burns with low tgo
            if ( status != PegStatus.CONVERGED )
            {
                double clamp = 4.5 * Math.Pow(10, ( tgo - 40 ) / 40 ) * UtilMath.Deg2Rad;
                if (clamp < phiMax)
                    phiMax = clamp;
            }

            // always allow up to at least the schuler frequency clamp
            double schuler = K * 0.35 * Math.Sqrt( gm / ( rm * rm * rm ) );
            if (phiMax < schuler)
                phiMax = schuler;

            if ( lambdaDot.magnitude > phiMax / K )
            {
                ldm = phiMax / K;
                lambdaDot = lambdaDot.normalized * ldm;
                //rgo = ST * lambda + QT * lambdaDot;
                rgo = S * lambda + QP * lambdaDot;
            }

            t_lambda = vesselState.time + K;

/*
            Kp = tgo / 2;
            theta = ldm * Kp;
            f1 = Math.Sin(theta) / theta;
            f2 = 3.0 * ( f1 - Math.Cos(theta) ) / ( theta * theta );
            Lp = f1 * L;
            Sp = f1 * S;
            D = L * Kp - S;
            Jp = f2 * D;
            Qp = ( - L * Kp * Kp / 3.0 + D * Kp ) * f2;
            delta = ldm * ( K - tgo / 2.0);
            F1 = f1 * Math.Cos( delta );
            F2 = f2 * Math.Cos( delta );
            F3 = F1 * ( 1.0 - theta * delta / 3.0 );
            LT = F1 * L;
            QT = F2 * ( Q - S * K );
            ST = F3 * S;

            Debug.Log("theta = " + (theta * UtilMath.Rad2Deg) + " delta = " + (delta * UtilMath.Rad2Deg) + " f1: " + f1 + " f2: " + f2 + " F1: " + F1 + " F2: " + F2 + " F3: " + F3);
            Debug.Log("LT = " + LT + " QT = " + QT + " ST = " + ST);
            */

//                vthrust = LT * lambda;
//                rthrust = ST * lambda + QT * lambdaDot;

            Vector3d vthrust, rthrust;
            vthrust = lambda * ( L - ldm * ldm * ( H - J * K ) / 2.0 );
            rthrust = lambda * ( S - ldm * ldm * ( P - 2.0 * Q * K + S * K * K ) / 2.0 ) + QP * lambdaDot;
            rbias = rgo - rthrust;

            Vector3d rc1 = r - rthrust / 10.0 - vthrust * tgo / 30.0;
            Vector3d vc1 = v + 1.2 * rthrust / tgo - vthrust/10.0;

            Vector3d rc2, vc2;

            if ( Planetarium.FrameIsRotating() )
                // this routine has no inverse rotation issues but has problems with parabolic/hyperbolic orbits
                ConicStateUtils.CSE(mainBody.gravParameter, rc1, vc1, tgo, out rc2, out vc2);
            else
                // this routine has inverse rotation issues, but should work fine with parabolic/hyperbolic orbits
                CSEKSP(rc1, vc1, tgo, out rc2, out vc2);

            // debugging routine, always correct, very slow
            //CSESimple(rc1, vc1, tgo, out rc2, out vc2);

            Vector3d vgrav = vc2 - vc1;
            rgrav = rc2 - rc1 - vc1 * tgo;

            rp = r + v * tgo + rgrav + rthrust;
            vp = v + vgo + vgrav;

            // corrector
            Vector3d vmiss = corrector();

            tgo_prev = tgo;

            if ( status == PegStatus.INITIALIZING )
                status = PegStatus.INITIALIZED;

            return Math.Abs(vmiss.magnitude) < 0.01 * Math.Abs(vgo.magnitude);
        }

        private Vector3d corrector()
        {
            rp = rp - Vector3d.Dot(rp, iy) * iy;
            ix = rp.normalized;
            Vector3d iz = Vector3d.Cross(ix, iy);

            if (tmode == TargetMode.ORBIT)
            {
                /* FIXME: very unlikely to work on ascents / below inv rotation threshold */
                Vector3d target_orbit_periapsis = target_orbit.getRelativePositionFromTrueAnomaly(0).xzy;
                double ta = Vector3.Angle(target_orbit_periapsis, rp) * UtilMath.Deg2Rad;
                if ( Vector3d.Dot(Vector3d.Cross(target_orbit_periapsis, rp), -iy) < 0 )
                    ta = -ta;
                vd = target_orbit.getOrbitalVelocityAtTrueAnomaly(ta).xzy;
                rd = target_orbit.getRelativePositionFromTrueAnomaly(ta).xzy;
            }
            else
            {
                rd = rdval * ix;
                vd = vdval * ( Math.Sin(gamma) * ix + Math.Cos(gamma) * iz );
            }

            if ( status == PegStatus.CONVERGED && tgo < 40 && last_stage == 0 )
                rd = rp;

            Vector3d vmiss = vp - vd;
            vgo = vgo - 1.0 * vmiss;

            if ( imode == IncMode.FREE_LAN && tgo > 40 && isStable() )
            {
                // FIXME: doesn't look like this works without the rgo corrections (which affect ix here?) but those break accurate targetting...
                // correct iy to fixed inc with free LAN
                double d = Vector3d.Dot( -Planetarium.up, ix );
                double SE = - 0.5 * ( Vector3d.Dot( -Planetarium.up, iy) + Math.Cos(incval * UtilMath.Deg2Rad) ) * Vector3d.Dot( -Planetarium.up, iz ) / (1 - d * d);
                iy = ( iy * Math.Sqrt( 1 - SE * SE ) + SE * iz ).normalized;
            }

            return vmiss;
        }

        List<Vector3d> CSEPoints = new List<Vector3d>();

        private void DrawCSE()
        {
            if (enabled && status != PegStatus.FINISHED)
            {
                var r = orbit.getRelativePositionAtUT(vesselState.time).xzy;
                var p = mainBody.position;
                Vector3d vpos = vessel.CoM + (vesselState.orbitalVelocity - Krakensbane.GetFrameVelocity() - vessel.orbit.GetRotFrameVel(vessel.orbit.referenceBody).xzy) * Time.fixedDeltaTime;
                GLUtils.DrawPath(mainBody, new List<Vector3d> { p, p + r }, Color.red, true, false, false);
                GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + vgo }, Color.green, true, false, false);
                GLUtils.DrawPath(mainBody, new List<Vector3d> { rd + p, rd + p + ( vd * 100 ) }, Color.green, true, false, false);
                GLUtils.DrawPath(mainBody, new List<Vector3d> { rp + p, rp + p + ( vp * 100 ) }, Color.red, true, false, false);
                GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + iF * 100 }, Color.red, true, false, false);
                GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + lambda * 100 }, Color.blue, true, false, false);
                GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + lambdaDot * 100 }, Color.cyan, true, false, false);
                // GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + rthrust }, Color.magenta, true, false, false);
                // GLUtils.DrawPath(mainBody, CSEPoints, Color.red, true, false, false);
            }
        }

        Orbit CSEorbit = new Orbit();

        /* FIXME: this still doesn't quite work due to the inverse rotation problem -- still wiggles at 145km on ascents, particularly polar ones */
        private void CSEKSP(Vector3d r0, Vector3d v0, double t, out Vector3d rf, out Vector3d vf)
        {
            Vector3d rot = orbit.GetRotFrameVelAtPos(mainBody, r0.xzy);
            CSEorbit.UpdateFromStateVectors(r0.xzy, v0.xzy + rot, mainBody, vesselState.time);
            CSEorbit.GetOrbitalStateVectorsAtUT(vesselState.time, out rf, out vf);
            CSEorbit.GetOrbitalStateVectorsAtUT(vesselState.time + t, out rf, out vf);

            rot = CSEorbit.GetRotFrameVelAtPos(mainBody, rf);

            rf = rf.xzy;
            vf = (vf - rot).xzy;
        }

        /* stupidly simple, but expensive, and you get graphs */
        private void CSESimple(Vector3d r0, Vector3d v0, double t, out Vector3d rf, out Vector3d vf)
        {
            CSEPoints.Clear();
            int N = Convert.ToInt32( 2.0 * t );
            if (N < 150)
                N = 150;
            vf = v0;
            rf = r0;
            double dt = t/N;
            for(int i = 0; i < N; i++)
            {
                double rfm = rf.magnitude;
                vf = vf - dt * mainBody.gravParameter * rf / (rfm * rfm * rfm );
                rf = rf + dt * vf;
                if (i % 10 == 0) {
                    CSEPoints.Add(rf + mainBody.position);
                }
            }
        }

        private void Done()
        {
            users.Clear();
            core.thrust.ThrustOff();
            vessel.ctrlState.X = vessel.ctrlState.Y = vessel.ctrlState.Z = 0.0f;
            status = PegStatus.FINISHED;
        }

        public void Reset()
        {
            // lambda and lambdaDot are deliberately not cleared here
            if ( imode == IncMode.FREE_LAN )
                SetPlaneFromInclination(incval);
            status = PegStatus.INITIALIZING;
            stages = new List<StageInfo>();
            tgo_prev = 0.0;
            tgo = 1.0;
            last_PEG = 0.0;
            last_call = 0.0;
            last_stage_count = 0;
            rd = Vector3d.zero;
            rgrav = Vector3d.zero;
        }

        private int MatchInOldStageList(int i)
        {
            // some paranoia
            if ( i > (vacStats.Length - 1) )
                return -1;

            // it may later match, but zero dV is useless
            if ( vacStats[i].deltaV <= 0 )
                return -1;

            // this is used to copy stages from the oldlist to the newlist and reduce garbage
            for ( int j = 0; j < stages.Count; j++ )
            {
                if ( stages[j].kspStage == i && stages[j].PartsListMatch(vacStats[i].parts) )
                {
                    return j;
                }
            }
            return -1;
        }

        public void SynchStats()
        {
            for (int i = 0; i < stages.Count; i++ )
            {
                // only live atmostats for the bottom stage, which is inaccurate, but this
                // is abusing an algorithm that can't fly properly in the atmosphere anyway
                FuelFlowSimulation.Stats[] mjstats = ( i == 0 ) ? atmoStats : vacStats;

                int k = stages[i].kspStage;
                stages[i].parts = mjstats[k].parts;
                stages[i].ve = mjstats[k].isp * 9.80665;
                stages[i].thrust = mjstats[k].startThrust;
                stages[i].dt = mjstats[k].deltaTime;
                stages[i].Li = mjstats[k].deltaV;
                stages[i].mass = mjstats[k].startMass;
                stages[i].a0 = stages[i].thrust / stages[i].mass;
                stages[i].tau = stages[i].ve / stages[i].a0;
                stages[i].mdot = stages[i].thrust / stages[i].ve;
            }
        }

        private void UpdateStages()
        {
            List<StageInfo>newlist = new List<StageInfo>();

            for ( int i = vacStats.Length-1; i >= 0; i-- )
            {
                int j;
                if ( ( j = MatchInOldStageList(i) ) > 0 )
                {
                    newlist.Add(stages[j]);
                    stages.RemoveAt(j);
                    continue;
                }
                if ( vacStats[i].deltaV > 0 )
                {
                    StageInfo stage = new StageInfo();
                    stage.kspStage =  i;
                    newlist.Add(stage);
                }
            }

            stages = newlist;

            SynchStats();

            // find out how many stages we really need and clean up the Li (dV) and dt of the upper stage
            double vgo_temp_mag = vgo.magnitude;
            for(int i = 0; i < stages.Count; i++)
            {
                last_stage = i;
                if ( stages[i].Li > vgo_temp_mag || i == stages.Count - 1 )
                {
                    stages[i].Li = vgo_temp_mag;
                    stages[i].dt = stages[i].tau * ( 1 - Math.Exp(-stages[i].Li/stages[i].ve) );
                    break;
                }
                else
                {
                  vgo_temp_mag -= stages[i].Li;
                }
            }

            // zero out all the upper stages
            for(int i = last_stage + 1; i < stages.Count; i++)
            {
                stages[i].Li = stages[i].Ji = stages[i].Si = 0;
                stages[i].Qi = stages[i].Pi = stages[i].Hi = 0;
                stages[i].dt = 0;
            }

        }

        public class StageInfo
        {
            // vehicle data
            public double mdot;
            public double ve;
            public double thrust;
            public double mass;
            public double a0;
            public double tau;
            public double dt;

            // integrals
            public double Li;      // delta-V (first integral)
            public double Si;
            public double Hi;
            public double Ji;
            public double Qi;
            public double Pi;

            // cumulative dt
            public double tgo1;    // tgo of i-1th stage (tgo of previous stage / tgo at start of this stage)
            public double tgo;     // tgo at the end of the stage (sum of dt of this stage and all previous)

            // tracking
            public List<Part> parts;
            public int kspStage;

            public bool PartsListMatch(List<Part> other)
            {
                for(int i = 0; i < parts.Count; i++)
                {
                    if ( !other.Contains(parts[i]) )
                        return false;
                }
                for(int i = 0; i < other.Count; i++)
                {
                    if ( !parts.Contains(other[i]) )
                        return false;
                }
                return true;
            }

            public override string ToString()
            {
                return "kspstage = " + kspStage +
                       " a0 = " + a0 +
                       " mdot = " + mdot +
                       " ve = " + ve +
                       " thrust = " + thrust +
                       " mass = " + mass +
                       " tau = " + tau +
                       " tgo1 = " + tgo1 +
                       " dt = " + dt +
                       " tgo = " + tgo +
                       " Li = " + Li +
                       " Si = " + Si +
                       " Ji = " + Ji +
                       " Qi = " + Qi;
            }
        }

        private void log_stages()
        {
            Debug.Log("num stages = " + stages.Count);
            for ( int i = 0; i < stages.Count; i++ )
            {
                Debug.Log(stages[i]);
            }
        }
    }
}
