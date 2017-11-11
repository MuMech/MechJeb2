using System;
using UnityEngine;
using System.Collections.Generic;

/*
 * PEG algorithm, mostly from:
 *
 * https://ntrs.nasa.gov/search.jsp?R=19760020204
 *
 * Jaggers 1976 is mostly referencing and builds atop the implementation in:
 *
 * https://ntrs.nasa.gov/search.jsp?R=19790048206
 *
 * Some additional tweaks from:
 *
 * https://arc.aiaa.org/doi/abs/10.2514/6.1977-1051 (very different thrust integral / gravity integral formulation which is not used here)
 *
 * Related earlier work for background:
 *
 * https://ntrs.nasa.gov/search.jsp?R=19740024190
 *
 */

/*
 *  MAYBE/RESEARCH list:
 *
 *  - setting for linear tangent / linear angle to switch between clipping omega to 0.00001 or not?
 *  - allow tweaking the corrector vmiss gain?
 *  - should vd = vp before projecting vp into the iy plane?
 *  - when omega is clipped should the magnitude of lambdaDot be adjusted?
 *  - what should phimax be clipped to?
 *  - can the Jaggers1977 tweaks to the corrector be fixed, will they give better terminal guidance or anything?
 *  - f2 and f3 tweaks from the Jaggers papers?
 *
 *  Higher Priority / Nearer Term TODO list:
 *
 *  - the iy corrector for free-lan mode from Jaggers1977 seems to not work well for inclinations below the launch latitude
 *  - the iy corrector seems to have lambda/lambdaDot orthogonality issues?
 *  - external delta-v mode and hooking PEG up to the NodeExecutor
 *  - fixing the angular momentum cutoff to be smarter (requirement for NodeExecutor)
 *  - "FREE" inclination mode (in-plane maneuvers -- requirement for NodeExecutor)
 *  - manual entry of coast phase (probably based on kerbal-stage rather than final-stage since final-stage may change if we e.g. eat into TLI)
 *
 *  Medium Priority / Medium Term TODO list:
 *
 *  - injection into orbits at other than the periapsis
 *  - matching planes with contract orbits
 *  - launch to rendevous with space-stations (engine throttling?)
 *  - Lambert-driven end conditions
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
    public class MechJebModulePEGController : ComputerModule
    {
        public MechJebModulePEGController(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble terminalGuidanceTime = new EditableDouble(10);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pegInterval = new EditableDouble(1);

        public Vector3d lambda;
        public Vector3d lambdaDot;
        public double t_lambda;
        public Vector3d iF;
        public double phi { get { return omega * K; } }
        public double primerMag { get { return ( lambda + lambdaDot * ( vesselState.time - t_lambda ) ).magnitude; } }  /* FIXME: incorrect */
        public double pitch;
        public double heading;

        /* FIXME: status enum? */
        public bool initialized;
        public bool converged;
        public bool finished;
        public bool terminalGuidance;
        public bool failed;

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

        private System.Diagnostics.Stopwatch CSEtimer = new System.Diagnostics.Stopwatch();

        public override void OnModuleEnabled()
        {
            Reset();
            core.AddToPostDrawQueue(DrawCSE);
            finished = false;
        }

        public override void OnModuleDisabled()
        {
        }

        public override void OnFixedUpdate()
        {
            if (finished) /* i told you i was done */
                return;

            // FIXME: we need to add liveStats to vacStats and atmoStats from MechJebModuleStageStats so we don't have to force liveSLT here
            stats.liveSLT = true;
            stats.RequestUpdate(this, true);
            if (enabled)
               converge();

            update_pitch_and_heading();

            if ( vessel.orbit.h.magnitude > rdval * vdval )
            {
                core.thrust.targetThrottle = 0.0F;
                finished = true;
            }
        }

        // state for next iteration
        public double tgo;          // time to burnout
        public Vector3d vgo;        // velocity remaining to be gained
        private Vector3d rd;        // burnout position vector
        private double f1;
        public double omega;
        private Vector3d vprev;     // value of v on prior iteration
        private Vector3d rgrav;
        private Vector3d rgo;  // FIXME: temp for graphing

        private double last_PEG;    // this is the last PEG update time
        public double K;

        public List<StageInfo> stages = new List<StageInfo>();


        /*
         * TARGET APIs
         */

        // private
        private IncMode imode;

        // target burnout radius
        private double rdval;
        // target burnout velocity
        private double vdval;
        // target burnout angle
        private double gamma;
        // tangent plane (minus orbit normal for PEG)
        public Vector3d iy;
        // inclination target for FREE_LAN
        private double incval;

        private enum IncMode { FIXED_LAN, FREE_LAN, FREE };

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

        public void TargetPeInsertMatchInc(double PeA, double ApA, double inc)
        {
            imode = IncMode.FREE_LAN;
            if (incval != inc)
            {
                incval = inc;
                SetPlaneFromInclination(inc);  /* updating every time would defeat PEG's corrector */
            }
            SetRdvalVdval(PeA, ApA);
        }

        public void TargetPeInsertFree(double PeA, double ApA) /* FIXME: implement */
        {
            imode = IncMode.FREE;
            SetRdvalVdval(PeA, ApA);
        }

        /* converts PeA + ApA into rdval/vdval for periapsis insertion.
           - handles hyperbolic orbits
           - remaps ApA < PeA onto circular orbits */
        private void SetRdvalVdval(double PeA, double ApA)
        {
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

        private void converge()
        {
            if ( (vesselState.time - last_PEG) < pegInterval)
                return;

            if (converged)
            {
                update();
            }
            else
            {
                for(int i = 0; i < 200 && !converged; i++)
                    update();
                if (!converged)
                    failed = true;
            }
            if (vgo.magnitude == 0.0)
            {
                failed = true;
                converged = false;
            }
            if (converged && !failed && tgo < terminalGuidanceTime)
                terminalGuidance = true;

            if (!converged || failed)
            {
                converged = false;
                failed = true;
            }
            last_PEG = vesselState.time;
        }

        /* extract pitch and heading off of iF to avoid continuously recomputing on every call */
        private void update_pitch_and_heading()
        {
            double sinTheta = Vector3d.Dot(lambda, vesselState.up);
            double z = vesselState.time - t_lambda;
            double w = Math.Sqrt( mainBody.gravParameter / ( vesselState.radius * vesselState.radius * vesselState.radius ) );
            iF = ( lambda * Math.Cos(w * z) + lambdaDot / w * Math.Sin(w * z)  + 3.0 * w * z / 2.0 * sinTheta * Math.Sin(w * z) * vesselState.up ).normalized;
            pitch = 90.0 - Vector3d.Angle(iF, vesselState.up);
            Vector3d headingDir = iF - Vector3d.Project(iF, vesselState.up);
            heading = UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(headingDir, vesselState.east), Vector3d.Dot(headingDir, vesselState.north));
        }

        private void update()
        {
            failed = true;

            Vector3d r = vesselState.orbitalPosition;
            Vector3d v = vesselState.orbitalVelocity;

            double gm = mainBody.gravParameter;

            // value of tgo from previous iteration
            double tgo_prev = 0;

            if (!initialized)
            {
                CSEtimer.Reset();
                f1 = 1;
                omega = 0.00001;
                // rd initialized to rdval-length vector 20 degrees downrange from r
                rd = QuaternionD.AngleAxis(20, -iy) * vesselState.up * rdval;
                // vgo initialized to rdval-length vector perpendicular to rd, minus current v
                vgo = Vector3d.Cross(-iy, rd).normalized * vdval - v;
                tgo = 1;
            }
            else
            {
                tgo_prev = tgo;
                Vector3d dvsensed = v - vprev;
                vgo = vgo - dvsensed;
                vprev = v;
            }

            // need accurate stage information before thrust integrals, Li is just dV so we read it from MJ
            UpdateStages();

            // find out how many stages we really need and clean up the Li (dV) and dt of the upper stage
            double vgo_temp_mag = vgo.magnitude;
            int last_stage = 0;
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

            // compute cumulative tgo's
            tgo = 0;
            for(int i = 0; i <= last_stage; i++)
            {
                stages[i].tgo1 = tgo;
                tgo += stages[i].dt;
                stages[i].tgo = tgo;
            }

            // total thrust integrals
            double J, L, S, Q;
            L = J = S = Q = 0;

            for(int i = 0; i <= last_stage; i++)
            {
                stages[i].updateIntegrals();
            }

            for(int i = 0; i <= last_stage; i++)
            {
                double Sp;
                if ( (last_stage == 0 && tgo > terminalGuidanceTime) || ( i < last_stage) )
                {
                    Sp = - stages[i].Li * ( stages[i].tau - stages[i].dt ) + stages[i].ve * stages[i].dt;
                    Q += J * stages[i].dt + Sp * ( stages[i].tau + stages[i].tgo1 ) - stages[i].ve * stages[i].dt * stages[i].dt / 2.0;
                }
                else
                {
                    Sp  = stages[i].Li * stages[i].dt / 2.0;
                    Q += J * stages[i].dt + Sp * ( stages[i].dt / 3.0 + stages[i].tgo1 );
                }
                S += Sp + L * stages[i].dt;
                L += stages[i].Li;
                J += stages[i].Li * stages[i].tgo - Sp;
            }

            K = J/L;
            Q = Q-S*K;

            t_lambda = vesselState.time + Math.Tan( omega * K ) / omega;

            // steering
            lambda = vgo.normalized;

            if (!initialized)
            {
                var rad = vesselState.radius;
                rgrav = -r * Math.Sqrt( gm / ( rad * rad * rad ) ) / 2.0;
            }
            else
            {
                rgrav = tgo * tgo / ( tgo_prev * tgo_prev ) * rgrav;
            }

            if (!terminalGuidance)
            {
                double phiMax = 30.0 * UtilMath.Deg2Rad;

                lambdaDot = ( rgo - S * lambda ) / Q;
                omega = lambdaDot.magnitude;
                if ( omega * K > phiMax )
                    omega = phiMax / K;
                if ( omega < 0.00001 )
                    omega = 0.00001;
            }

            rgo = rd - ( r + v * tgo + rgrav );

            /*if ( imode == IncMode.FREE_LAN )
            {
                Vector3d ip = Vector3d.Cross(lambda, iy).normalized;
                double A1 = Vector3d.Dot(rgo - Vector3d.Dot(lambda, rgo) * lambda, ip);
                rgo = S * lambda + A1 * ip;
            }
            else
            { */
                rgo = rgo + ( S - Vector3d.Dot(lambda, rgo) ) * lambda;
            /*} */
            S = f1 * S;

            f1 = ( 2.0 / ( omega * tgo ) ) * Math.Sin( omega * tgo / 2.0 );
            Vector3d rthrust = rgo;
            Vector3d vthrust = f1 * vgo;

            // going faster than the speed of light or doing burns the length of the Oort cloud are not supported...
            if (!vthrust.magnitude.IsFinite() || !rthrust.magnitude.IsFinite() || (vthrust.magnitude > 1E10) || (rthrust.magnitude > 1E16))
            {
                Fail();
                return;
            }

            // BLOCK7 - CSE gravity averaging

            Vector3d rc1 = r - rthrust / 10.0 - vthrust * tgo / 30.0;
            Vector3d vc1 = v + 1.2 * rthrust / tgo - vthrust/10.0;

            if (!vc1.magnitude.IsFinite() || !rc1.magnitude.IsFinite() || !tgo.IsFinite())
            {
                Fail();
                return;
            }

            Vector3d rc2, vc2;

            CSEtimer.Start();
            //CSEKSP(rc1, vc1, tgo, out rc2, out vc2);
            ConicStateUtils.CSE(mainBody.gravParameter, rc1, vc1, tgo, out rc2, out vc2);
            //CSESimple(rc1, vc1, tgo, out rc2, out vc2);
            CSEtimer.Stop();


            Vector3d vgrav = vc2 - vc1;
            rgrav = rc2 - rc1 - vc1 * tgo;

            Vector3d rp = r + v * tgo + rgrav + rthrust;
            Vector3d vp = v + vthrust + vgrav;

            // corrector

            rp = rp - Vector3d.Dot(rp, iy) * iy;
            if (terminalGuidance)
            {
                rd = rp;
            }
            else
            {
                rd = rdval * rp.normalized;
            }
            Vector3d ix = rd.normalized;
            Vector3d iz = Vector3d.Cross(ix, iy);
            Vector3d vd = vdval * ( Math.Sin(gamma) * ix + Math.Cos(gamma) * iz );
            Vector3d vmiss = vd - vp;
            Vector3d vgo_new = vgo + 0.5 * vmiss;  // gain of 1.0 causes PEG to flail on ascents

            // from Jaggers 1977 - but this tends to cause PEG to flail, maybe 0.05 is either too big or too small for ascents?
            /* Vector3d lambdav = vgo_new.normalized;
            double Fv = Vector3d.Dot(lambdav, vd - v) / Vector3d.Dot(lambda, vp - v ) - 1.0;
            if ( Math.Abs(Fv) < 0.05 )
                vgo = vgo * ( 1 + Fv );
            else */
                vgo = vgo_new;

            if ( imode == IncMode.FREE_LAN && !terminalGuidance )
            {
                /* correct iy to fixed inc with free LAN */
                double d = Vector3d.Dot( -Planetarium.up, ix );
                double SE = - 0.5 * ( Vector3d.Dot( -Planetarium.up, iy) + Math.Cos(incval * UtilMath.Deg2Rad) ) * Vector3d.Dot( -Planetarium.up, iz ) / (1 - d * d);
                iy = ( iy * Math.Sqrt( 1 - SE * SE ) + SE * iz ).normalized;
            }

            // housekeeping
            initialized = true;

            if ( Math.Abs(vmiss.magnitude) < 0.001 * Math.Abs(vgo.magnitude) )
                converged = true;

            failed = false;
        }

        List<Vector3d> CSEPoints = new List<Vector3d>();

        private void DrawCSE()
        {
            var p = mainBody.position;
            GLUtils.DrawPath(mainBody, new List<Vector3d> { Vector3d.zero, vgo }, Color.green, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { Vector3d.zero, iF * 100 }, Color.red, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { Vector3d.zero, lambda * 101 }, Color.blue, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { Vector3d.zero, lambdaDot * 100 }, Color.cyan, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { Vector3d.zero, rgo }, Color.magenta, true, false, false);
            // GLUtils.DrawPath(mainBody, CSEPoints, Color.red, true, false, false);
        }

        Orbit CSEorbit = new Orbit();

        private void CSEKSP(Vector3d r0, Vector3d v0, double t, out Vector3d rf, out Vector3d vf)
        {
            Vector3d rot = orbit.GetRotFrameVelAtPos(mainBody, r0.xzy);
            CSEorbit.UpdateFromStateVectors(r0.xzy, v0.xzy + rot, mainBody, vesselState.time);
            CSEorbit.GetOrbitalStateVectorsAtUT(vesselState.time, out rf, out vf);
            CSEorbit.GetOrbitalStateVectorsAtUT(vesselState.time + t, out rf, out vf);

            rot = orbit.GetRotFrameVelAtPos(mainBody, rf);

            rf = rf.xzy;
            vf = (vf - rot).xzy;
        }

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
            finished = true;
        }

        private void Fail()
        {
            failed = true;
            Reset();
        }

        public void Reset()
        {
            // failed is deliberately not set to false here
            // lambda and lambdaDot are deliberately not cleared here
            if ( imode == IncMode.FREE_LAN )
                SetPlaneFromInclination(incval);
            terminalGuidance = false;
            initialized = false;
            converged = false;
            stages = new List<StageInfo>();
            tgo = 0.0;
            rd = Vector3d.zero;
            vgo = Vector3d.zero;
            vprev = Vector3d.zero;
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
                stages[i].a0 = mjstats[k].startThrust / mjstats[k].startMass;
                stages[i].thrust = mjstats[k].startThrust;
                stages[i].dt = mjstats[k].deltaTime;
                stages[i].Li = mjstats[k].deltaV;
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
        }

        public class StageInfo
        {
            // vehicle data
            public double mdot;
            public double ve;
            public double thrust;
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

            // does the first pass of updating the integrals
            public void updateIntegrals()
            {
                Si = - Li * ( tau - dt ) + ve * dt;
                Ji = - Si + Li * tgo;
                Qi = Si * ( tau + tgo1 ) - ve * dt * dt / 2.0;
                Pi = Qi * ( tau + tgo1 ) - ve * dt * dt / 2.0 * ( dt / 3.0 + tgo1 );
            }

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
                String ret = "kspstage = " + kspStage + "\n" +
                       "a0 = " + a0 + "\n" +
                       "mdot = " + mdot + "\n" +
                       "ve = " + ve + "\n" +
                       "thrust = " + thrust + "\n" +
                       "tau = " + tau + "\n" +
                       "dt = " + dt + "\n" +
                       "Li = " + Li + "\n" +
                       "Parts = ";

                for(int i=0; i < parts.Count; i++)
                {
                    ret += parts[i];
                }
                ret += "\n";
                return ret;
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
