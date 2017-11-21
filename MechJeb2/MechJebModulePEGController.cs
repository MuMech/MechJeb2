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
 *  MAYBE/RESEARCH list:
 *
 *  - should vd = vp before projecting vp into the iy plane?
 *  - can the Jaggers1977 tweaks to the corrector be fixed, will they give better terminal guidance or anything?
 *  - what is up with the Jaggers1977 rgo fixes to the iy corrector not appearing to do anything useful at all?
 *    (oh looks like 90 degree inclination launches got all wiggly again, need to see if the rgo fixes fix them?)
 *
 *  Higher Priority / Nearer Term TODO list:
 *
 *  - external delta-v mode and hooking PEG up to the NodeExecutor
 *  - fixing the angular momentum cutoff to be smarter (requirement for NodeExecutor)
 *  - "FREE" inclination mode (in-plane maneuvers)
 *  - manual entry of coast phase (probably based on kerbal-stage rather than final-stage since final-stage may change if we e.g. eat into TLI)
 *
 *  Medium Priority / Medium Term TODO list:
 *
 *  - linear terminal velocity constraints and engine throttling?  (landings and rendezvous?)
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
        public EditableDouble pegInterval = new EditableDouble(1);
        // these values deliberately do not persist
        public EditableDouble terminalGuidanceTime = new EditableDouble(40);
        public EditableDouble vmissGain = new EditableDouble(1.0);

        public Vector3d lambda;
        public Vector3d lambdaDot;
        public double t_lambda;
        public Vector3d primer;
        public Vector3d iF { get { return primer.normalized; } }
        public double phi { get { return lambdaDot.magnitude * K; } }
        public double primerMag { get { return primer.magnitude; } }
        public double pitch;
        public double heading;
        public double lambdaHeading;

        /* FIXME: status enum? */
        public bool initialized;
        public bool converged;
        public bool finished;
        public bool terminalGuidance;
        public bool failed;

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

        public override void OnModuleEnabled()
        {
            Reset();
            core.AddToPostDrawQueue(DrawCSE);
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

            if ( finished )
            {
                core.thrust.targetThrottle = 0.0F;
            }
        }

        // state for next iteration
        public double tgo;          // time to burnout
        public double tgo_prev;     // time for last full converge
        public Vector3d vgo;        // velocity remaining to be gained
        private Vector3d rd;        // burnout position vector
        private Vector3d vprev;     // value of v on prior iteration
        private Vector3d rgrav;
        private Vector3d rbias;
        // following for graphing + stats
        private Vector3d vd;
        private Vector3d rp;
        private Vector3d vp;

        private double last_PEG;    // this is the last PEG update time
        private double last_call;   // this is the last call to converge
        public double K;
        public double Fv;

        public List<StageInfo> stages = new List<StageInfo>();

        /*
         * TARGET APIs
         */

        // private
        private IncMode imode;
        private TargetMode tmode;

        // target burnout radius
        private double rdval;
        // target burnout velocity
        private double vdval;
        // target burnout angle
        private double gamma;
        // target orbit
        private Orbit targetOrbit;
        // tangent plane (minus orbit normal for PEG)
        public Vector3d iy;
        // inclination target for FREE_LAN
        private double incval;

        private enum IncMode { FIXED_LAN, FREE_LAN, FREE };
        private enum TargetMode { PERIAPSIS, ORBIT };

        // must be called after one of TargetPe* for now
        public void AscentInit()
        {
            if (!initialized)
            {
                // rd initialized to rdval-length vector 20 degrees downrange from r
                rd = QuaternionD.AngleAxis(20, -iy) * vesselState.up * rdval;
                // Dv initialized to rdval-length vector perpendicular to rd, minus current v
                vgo = Vector3d.Cross(-iy, rd).normalized * vdval - vesselState.orbitalVelocity;
            }
        }

        // does its own initialization and is idempotent
        public void TargetNode(ManeuverNode node)
        {
            if (!initialized)
            {
                vgo = node.GetBurnVector(orbit);
                TargetOrbit(node.nextPatch);
                rd = node.nextPatch.SwappedRelativePositionAtUT(node.UT);
            }
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

        // matches oribital parameters, does not rendezvous, will have issues below the inv rotation threshold
        public void TargetOrbit(Orbit o)
        {
            imode = IncMode.FIXED_LAN;
            iy = -o.SwappedOrbitNormal().normalized;
            tmode = TargetMode.ORBIT;
            targetOrbit = o;
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

        private void converge()
        {
            if ( last_call != 0 )
                tgo -= vesselState.time - last_call;

            last_call = vesselState.time;

            if (tgo < TimeWarp.fixedDeltaTime)
            {
                finished = true;
                return;
            }

            if ( (vesselState.time - last_PEG) < pegInterval)
                return;

            // skip active guidance entirely for last 2 seconds
            if (terminalGuidance && tgo < 2)
                return;

            converged = false;

            if (terminalGuidance)
            {
                update();
                converged = true;
            }
            else
            {
                for(int i = 0; i < 20 && !converged; i++)
                {
                    update();
                }
            }

            if (!converged)
                failed = true;

            if (vgo.magnitude == 0.0)
                failed = true;

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
            double z = vesselState.time - t_lambda;
            // this more complicated implementation from Jaggers seems to offer no practical advantage as far as I can tell
            // double sinTheta = Vector3d.Dot(lambda, vesselState.up);
            // double w = Math.Sqrt( mainBody.gravParameter / ( vesselState.radius * vesselState.radius * vesselState.radius ) );
            // primer = lambda * Math.Cos(w * z) + lambdaDot / w * Math.Sin(w * z)  + 3.0 * w * z / 2.0 * sinTheta * Math.Sin(w * z) * vesselState.up;
            primer = lambda + lambdaDot * z;
            pitch = 90.0 - Vector3d.Angle(iF, vesselState.up);
            Vector3d headingDir = iF - Vector3d.Project(iF, vesselState.up);
            heading = UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(headingDir, vesselState.east), Vector3d.Dot(headingDir, vesselState.north));
            // lambdaHeading is useful for ascents if iF winds up over the zenith pointing in the wrong direction
            Vector3d lambdaDir = lambda - Vector3d.Project(lambda, vesselState.up);
            lambdaHeading = UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(lambdaDir, vesselState.east), Vector3d.Dot(lambdaDir, vesselState.north));
        }

        private void update()
        {
            failed = true;

            Vector3d r = vesselState.orbitalPosition;
            double rm = vesselState.orbitalPosition.magnitude;
            Vector3d v = vesselState.orbitalVelocity;

            double gm = mainBody.gravParameter;

            if (!initialized)
            {
                lambda = v.normalized;
                tgo = 1;
                vgo = v.normalized * 1;
                lambdaDot = Vector3d.up;
                rp = r + v * tgo;
                vp = v;
            }
            else
            {
                Vector3d dvsensed = v - vprev;
                vgo = vgo - dvsensed;
                vprev = v;
            }

            if (vgo == Vector3d.zero)
                vgo = v.normalized;

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
            double tgo2 = 0;
            for(int i = 0; i <= last_stage; i++)
            {
                stages[i].tgo1 = tgo2;
                tgo2 += stages[i].dt;
                stages[i].tgo = tgo2;
            }

            if (!terminalGuidance || tgo > 2)
            {
                /* use calculated tgo off of vgo + stage analysis */
                /* (this also sneakily initializes tgo to 1 on the first-pass) */
                tgo = ( tgo2 > 0 ) ? tgo2 : 1;
            }

            // total thrust integrals
            double J, L, S, Q, H, P;
            L = J = S = Q = H = P = 0.0;

            for(int i = 0; i <= last_stage; i++)
            {
                stages[i].updateIntegrals();
            }

            for(int i = 0; i <= last_stage; i++)
            {
                double HA = stages[i].Ji * stages[i].tgo - stages[i].Qi;
                double SA = stages[i].Si + L * stages[i].dt;
                double QA = stages[i].Qi + J * stages[i].dt;
                double PA = stages[i].Pi + H * stages[i].dt;
                L += stages[i].Li;
                S += SA;
                H += HA;
                J += stages[i].Ji;
                Q += QA;
                P += PA;
            }
            log_stages();


            Debug.Log("L = " + L + " J = " + J + " S = " + S + " Q = " + Q );

            K = J / L;
            double QT = Q - S * K;

            // steering
            if (!terminalGuidance || tgo > 2)
                lambda = vgo.normalized;

            if (!initialized)
            {
                rgrav = -vesselState.orbitalPosition * Math.Sqrt( gm / ( rm * rm * rm ) ) / 2.0;
            }
            else
            {
                rgrav = tgo * tgo / ( tgo_prev * tgo_prev ) * rgrav;
            }

            Vector3d rgo = rd - ( r + v * tgo + rgrav ) + rbias;

            // from Jaggers 1977, not clear if you still should orthogonolize this (below) or not
            // also not clear if it should be frozen during terminalGuidance or maybe only the last few seconds?
            if ( imode == IncMode.FREE_LAN && !terminalGuidance )
            {
                Vector3d ip = Vector3d.Cross(lambda, iy).normalized;
                double Q1 = Vector3d.Dot(rgo - Vector3d.Dot(lambda, rgo) * lambda, ip);
                rgo = S * lambda + Q1 * ip;
            }

            rgo = rgo + ( S - Vector3d.Dot(lambda, rgo) ) * lambda;

            double phiMax = 45.0 * UtilMath.Deg2Rad;

            /*
               if (tmode == TargetMode.ORBIT)
               {
               var rad = vesselState.radius;
               double lambdaDot_xz = 0.35 * Math.Sqrt( gm / ( rad * rad * rad ) );
               lambdaDot = lambdaDot_xz * ( Vector3d.Cross( lambda, iy ) );
               }
               else
               { */
            if (!terminalGuidance)
                lambdaDot = ( rgo - S * lambda ) / QT;
            /*
               }
               */

            double ldm = lambdaDot.magnitude;

            if ( lambdaDot.magnitude > phiMax / K )
            {
                ldm = phiMax / K;
                lambdaDot = lambdaDot.normalized * ldm;
                rgo = S * lambda + QT * lambdaDot;
            }

            t_lambda = vesselState.time + Math.Tan( ldm * K ) / ldm;

            Vector3d vthrust = lambda * ( L - ldm * ldm * ( H - J * K ) / 2.0 );
            Vector3d rthrust = lambda * ( S - ldm * ldm * ( P - 2.0 * Q * K + S * K * K ) / 2.0 ) + QT * lambdaDot;

            rbias = rgo - rthrust;

            // going faster than the speed of light or doing burns the length of the Oort cloud are not supported...
            if (!vthrust.magnitude.IsFinite() || !rthrust.magnitude.IsFinite() || (vthrust.magnitude > 1E10) || (rthrust.magnitude > 1E16))
            {
                Fail();
                return;
            }

            Vector3d rc1 = r - rthrust / 10.0 - vthrust * tgo / 30.0;
            Vector3d vc1 = v + 1.2 * rthrust / tgo - vthrust/10.0;

            Vector3d rc2, vc2;

            //CSEKSP(rc1, vc1, tgo, out rc2, out vc2);
            ConicStateUtils.CSE(mainBody.gravParameter, rc1, vc1, tgo, out rc2, out vc2);
            //CSESimple(rc1, vc1, tgo, out rc2, out vc2);

            Vector3d vgrav = vc2 - vc1;
            rgrav = rc2 - rc1 - vc1 * tgo;

            rp = r + v * tgo + rgrav + rthrust;
            vp = v + vgo + vgrav;

            // corrector

            vmissGain = MuUtils.Clamp(vmissGain, 0.01, 0.99);

            rp = rp - Vector3d.Dot(rp, iy) * iy;
            Vector3d ix = (rp - Vector3d.Dot(iy, rp) * iy).normalized;
            if (!terminalGuidance)
                rd = rdval * ix;
            else
                rd = rp;
            Vector3d iz = Vector3d.Cross(ix, iy);
            vd = vdval * ( Math.Sin(gamma) * ix + Math.Cos(gamma) * iz );

            Vector3d vmiss = vd - vp;
            vgo = vgo + vmissGain * vmiss;

            if ( imode == IncMode.FREE_LAN && !terminalGuidance )
            {
                // correct iy to fixed inc with free LAN
                double d = Vector3d.Dot( -Planetarium.up, ix );
                double SE = - 0.5 * ( Vector3d.Dot( -Planetarium.up, iy) + Math.Cos(incval * UtilMath.Deg2Rad) ) * Vector3d.Dot( -Planetarium.up, iz ) / (1 - d * d);
                iy = ( iy * Math.Sqrt( 1 - SE * SE ) + SE * iz ).normalized;
            }

            // housekeeping
            initialized = true;

            if ( Math.Abs(vmiss.magnitude) < 0.01 * Math.Abs(vgo.magnitude) )
            {
                //Debug.Log("converged!");
                converged = true;
            }
            else
            {
                //Debug.Log("miss = " + ( vmiss.magnitude / vgo.magnitude ) + " [ " + vmiss + " ] ");
                converged = false;
            }

            tgo_prev = tgo;

            failed = false;
        }

        List<Vector3d> CSEPoints = new List<Vector3d>();

        private void DrawCSE()
        {
            var p = mainBody.position;
            var vpos = mainBody.position + vesselState.orbitalPosition;
            GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + vgo }, Color.green, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { rd + p, rd + p + ( vd * 100 ) }, Color.green, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { rp + p, rp + p + ( vp * 100 ) }, Color.red, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + iF * 100 }, Color.red, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + lambda * 101 }, Color.blue, true, false, false);
            GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + lambdaDot * 100 }, Color.cyan, true, false, false);
            // GLUtils.DrawPath(mainBody, new List<Vector3d> { vpos, vpos + rthrust }, Color.magenta, true, false, false);
            // GLUtils.DrawPath(mainBody, CSEPoints, Color.red, true, false, false);
        }

        Orbit CSEorbit = new Orbit();

        /* FIXME: this still doesn't quite work due to the inverse rotation problem -- still wiggles at 145km on ascents, particularly polar ones */
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
            finished = false;
            stages = new List<StageInfo>();
            tgo_prev = 0.0;
            tgo = 1.0;
            last_PEG = 0.0;
            last_call = 0.0;
            rd = Vector3d.zero;
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
                stages[i].thrust = mjstats[k].startThrust;
                stages[i].dt = mjstats[k].deltaTime;
                stages[i].Li = mjstats[k].deltaV;
                if ( i == 0 )
                    // mechjeb's staging can get confused sometimes
                    stages[i].mass = vesselState.mass;
                else
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

            // does the first pass of updating the integrals
            public void updateIntegrals()
            {
                Si = - Li * ( tau - dt ) + ve * dt;
                Ji = Li * tgo - Si;
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
                       " Ji = " + Ji;
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
