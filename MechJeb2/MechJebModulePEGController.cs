using System;
using UnityEngine;
using KSP.UI.Screens;
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
 *  - Buttons in maneuver planner to execute nodes with/without PEG and with/without RCS
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
    public enum PegStatus { ENABLED, INITIALIZING, INITIALIZED, SLEWING, CONVERGED, STAGING, TERMINAL, TERMINAL_RCS, FINISHED, FAILED, COASTING };
    public enum TargetMode { PERIAPSIS, ORBIT };
    public enum IncMode { FIXED_LAN };

    public class MechJebModulePEGController : ComputerModule
    {
        public MechJebModulePEGController(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pegInterval = new EditableDouble(0.01);

        public Vector3d lambda;
        public Vector3d lambdaDot;
        public double t_lambda;
        public Vector3d oldlambda;
        public Vector3d oldlambdaDot;
        public double oldt_lambda;
        public Vector3d primer;
        public Vector3d iF { get { return primer.normalized; } }
        public double phi { get { return lambdaDot.magnitude * K; } }
        public double primerMag { get { return primer.magnitude; } }
        public double pitch;
        public double heading;

        public PegStatus status;
        public PegStatus oldstatus;

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

        // should only be called once per burn/ascent
        public override void OnModuleEnabled()
        {
            // coast phases are deliberately not reset in Reset() so we never get a completed coast phase again after whacking Reset()
            coastFinished = false;
            coastDone = 0.0;
            SetCoast(0.0, -1);
            status = PegStatus.ENABLED;
            core.AddToPostDrawQueue(DrawCSE);
            core.attitude.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.users.Remove(this);
        }

        // peg can now control thrust in converge_vgo() but consumers may want it to not burn
        private bool suppressBurning;
        // this can be hammered on repetetively, peg will start updating data.
        public void AssertStart(bool suppressBurning = false)
        {
            this.suppressBurning = suppressBurning;
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

            if (isTerminalGuidance())
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

        private double last_PEG;    // this is the last PEG update time
        public double K;

        public List<StageInfo> stages = new List<StageInfo>();

        /*
         * TARGET APIs
         */

        // private
        public IncMode imode;
        public TargetMode tmode;

        public double coastSecs = 0.0;
        public int coastAfterStage = -1;
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
        // inclination target for manual targetting with inclination
        private double incval;
        private double leadingAngleval;
        private Orbit incOrbit;
        // linear terminal velocity targets
        private Orbit target_orbit;

        // can be hammered on repetetively, we remember how much coast we've done and if we've completed the coast
        public void SetCoast(double secs, int stage)
        {
            if (isTerminalGuidance())
                return;

            coastSecs = secs;
            coastAfterStage = stage;
        }

        public void ClearCoast()
        {
            if (isTerminalGuidance())
                return;
            coastSecs = 0.0;
            coastAfterStage = 0;
        }

        // does its own initialization and is idempotent
        public void TargetNode(ManeuverNode node, bool force_vgo = false)
        {
            target_orbit = node.nextPatch;
            iy = -target_orbit.SwappedOrbitNormal();
            imode = IncMode.FIXED_LAN;
            tmode = TargetMode.ORBIT;
            if ( (!isStable() && status != PegStatus.FINISHED) || force_vgo )
            {
                // do not update vgo if we're stable (and not warping in the node executor or something like that
                vgo = node.GetBurnVector(orbit);
            }
        }

        /* meta state for consumers that means "is iF usable?" (or pitch/heading) */
        public bool isStable()
        {
            return status == PegStatus.CONVERGED || status == PegStatus.STAGING || status == PegStatus.COASTING || isTerminalGuidance();
        }

        public bool isTerminalGuidance()
        {
            return status == PegStatus.SLEWING || status == PegStatus.TERMINAL || status == PegStatus.TERMINAL_RCS;
        }

        /* normal pre-states but not usefully converged */
        public bool isInitializing()
        {
            return status == PegStatus.ENABLED || status == PegStatus.INITIALIZING || status == PegStatus.INITIALIZED;
        }

        public void TargetPeInsertMatchPlane(double PeA, double ApA, Vector3d tangent)
        {
            // update iy every tick to fix world rotation issues
            iy = -tangent.normalized;
            imode = IncMode.FIXED_LAN;
            if (isTerminalGuidance())
                return;
            SetRdvalVdval(PeA, ApA);
        }

        public void TargetPeInsertMatchOrbitPlane(double PeA, double ApA, Orbit o)
        {
            TargetPeInsertMatchPlane(PeA, ApA, o.SwappedOrbitNormal());
        }

        public void TargetRcs(Vector3d vgo)
        {
            status = PegStatus.SLEWING;
            this.vgo = vgo;
            lambda = vgo.normalized;
            lambdaDot = Vector3d.zero;
            t_lambda = vesselState.time;
        }

        public void TargetVgo(Vector3d vgo)
        {
            status = PegStatus.TERMINAL;
            this.vgo = vgo;
            lambda = vgo.normalized;
            lambdaDot = Vector3d.zero;
            t_lambda = vesselState.time;
        }

        public void TargetPeInsertMatchInc(double PeA, double ApA, double inc, double leadingAngle)
        {
            if (incval != inc || leadingAngleval != leadingAngle || incOrbit == null || !vessel.LiftedOff() || vessel.Landed)
            {
                incval = inc;
                leadingAngleval = leadingAngle;
                // we use this orbit solely to be able to pull off the normal at later ticks, which will then be adjusted for
                // world coordinate rotation in future frames (awful KSP rotating coordinate system).  the Ap and Pe do not matter
                // and we could not orient it correctly since the location of the Pe insertion is dependent upon thd downrange
                // termination of the burn and we do not have accurate enough predictions of that point.
                incOrbit = OrbitFromInclination(inc, leadingAngle);
            }
            TargetPeInsertMatchOrbitPlane(PeA, ApA, incOrbit);
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

        // this is an orbit which is useful only in that it stores an orbit normal and we can retrieve that later
        private Orbit OrbitFromInclination(double inc, double leadingAngle)
        {
            Vector3d tangent = OrbitNormalFromInclination(inc).normalized;
            tangent = QuaternionD.AngleAxis(-leadingAngle, Planetarium.up) * tangent;
            // on earth this gives a 185x185 orbit, but it doesn't matter.
            Vector3d pos = Vector3d.Cross(tangent, vesselState.orbitalPosition).normalized * 6556000.0;
            Vector3d vel = Vector3d.Cross(tangent, pos).normalized * 77974.0;
            Orbit o = new Orbit();
            o.UpdateFromStateVectors(pos.xzy, vel.xzy, mainBody, vesselState.time);
            return o;
        }

        // this takes the current position of the vessel and produces an orbit normal for creating a target
        private Vector3d OrbitNormalFromInclination(double inc)
        {
            double desiredHeading = UtilMath.Deg2Rad * OrbitalManeuverCalculator.HeadingForInclination(inc, vesselState.latitude);
            Vector3d desiredHeadingVector = Math.Sin(desiredHeading) * vesselState.east + Math.Cos(desiredHeading) * vesselState.north;
            Vector3d tangent = Vector3d.Cross(vesselState.orbitalPosition, desiredHeadingVector).normalized;
            if ( Math.Abs(inc) < Math.Abs(vesselState.latitude) )
            {
                if (Vector3.Angle(tangent, Planetarium.up) < 90) // can't i figure this out from the quadrant of inc?
                {
                    tangent = Vector3.RotateTowards(Planetarium.up, tangent, (float)(inc * UtilMath.Deg2Rad), 0.0f);
                }
                else
                {
                    tangent = Vector3.RotateTowards(-Planetarium.up, tangent, (float)(inc * UtilMath.Deg2Rad), 0.0f);
                }
            }
            return tangent;
        }

        private double last_call;        // this is the last call to converge
        private int last_stage_count;    // num stages we had last time
        private double last_stage_time;  // last time we staged

        private void converge_vgo()
        {
            /* we handle attitude directly here, because ascents are $COMPLICATED we do not in converge() */
            core.attitude.attitudeTo(iF, AttitudeReference.INERTIAL, this);
            if ( core.attitude.attitudeAngleFromTarget() > 1 && status == PegStatus.SLEWING )
            {
                vessel.ctrlState.Z = 0.0F;
                core.thrust.ThrustOff();
                return;
            }

            if ( status == PegStatus.SLEWING ) {
                status = PegStatus.TERMINAL_RCS;
            }

            // only use rcs for trim if its enabled and we have more than 10N of thrust
            bool has_rcs = vessel.hasEnabledRCSModules() && vessel.ActionGroups[KSPActionGroup.RCS] && ( vesselState.rcsThrustAvailable.down > 0.01 );

            double dt = vesselState.time - last_call;
            Vector3d dV_atom = ( vessel.acceleration_immediate - vessel.graviticAcceleration ) * dt;

            if ( last_call != 0 )
                vgo -= dV_atom;

            double vgo_forward = Vector3d.Dot(vgo, vesselState.forward);

            Vector3d next_dV_atom;
            if (status == PegStatus.TERMINAL_RCS)
                next_dV_atom = vesselState.rcsThrustAvailable.down / vesselState.mass * vesselState.forward * dt;
            else
                next_dV_atom = vesselState.maxThrustAccel * vesselState.forward * dt;

            tgo = vgo_forward / Vector3d.Dot(next_dV_atom, vesselState.forward) * TimeWarp.fixedDeltaTime;

            int tickstop = 1;
            // due to increasing accelleration due to high constant thrust we stop at 2 * tick rather than 1 * tick to always stop before
            if (has_rcs && status == PegStatus.TERMINAL)
                tickstop = 2;

            if ( tgo < ( tickstop * TimeWarp.fixedDeltaTime ) && last_call != 0 )
            {
                Debug.Log("finishing burn due to tgo < tick limit, vgo = " + vgo.magnitude + " tgo = " + tgo);
                if ( has_rcs && status == PegStatus.TERMINAL )
                {
                    Debug.Log("switching to RCS trim burn");
                    // finish remaining vgo on RCS
                    core.thrust.ThrustOff();
                    // we have arrived near enough and all we can do is trim out velocity
                    rp = vesselState.orbitalPosition;
                    vp = vesselState.orbitalVelocity;
                    iy = - orbit.SwappedOrbitNormal();
                    vgo = Vector3d.zero;
                    // kind of abusing the corrector, but this should work to share code
                    corrector();
                    TargetRcs(vgo);
                    return;
                }
                Done();
                return;
            }

            if (!suppressBurning)
            {
                if ( status == PegStatus.TERMINAL_RCS )
                {
                    core.thrust.ThrustOff();
                    vessel.ctrlState.Z = -1.0F;
                }
                else
                {
                    core.thrust.targetThrottle = 1.0F;
                    vessel.ctrlState.Z = 0.0F;
                }
            }

            UpdateStages();


            last_call = vesselState.time;
        }

        private double coastDone;
        private bool coastFinished;
        private double coastRemain;

        private void converge()
        {
            oldstatus = status;
            oldlambda = lambda;
            oldlambdaDot = lambdaDot;
            oldt_lambda = t_lambda;

            double dt = vesselState.time - last_call;
            Vector3d dV_atom = ( vessel.acceleration_immediate - vessel.graviticAcceleration ) * dt;

            if ( last_call != 0 )
            {
                tgo -= dt;
                vgo -= dV_atom;
            }

            coastRemain = coastSecs - coastDone;

            if ( StageManager.CurrentStage < coastAfterStage && coastRemain > 0 )
            {
                status = PegStatus.COASTING;
                core.thrust.ThrustOff();
                coastDone += dt;
            }
            else
            {
                if ( status == PegStatus.COASTING )
                {
                    status = PegStatus.CONVERGED;
                    coastFinished = true;
                    core.thrust.targetThrottle = 1.0F;
                }
            }

            UpdateStages();

            last_call = vesselState.time;

            if ( last_stage_count > stages.Count && status != PegStatus.COASTING )
                last_stage_time = vesselState.time;

            last_stage_count = stages.Count;
            /* 3 second quiet time after staging */
            if ( vesselState.time < last_stage_time + 3.0 )
            {
                status = PegStatus.STAGING;
                return;
            }

            // only do active guidance every pegInterval
            if ( (vesselState.time - last_PEG) < pegInterval && core.thrust.targetThrottle > 0 )
                return;

            // skipping active guidance for last 10 seconds is neccessary due to thrust integral instability
            if ( status == PegStatus.CONVERGED && tgo < 10 && status != PegStatus.COASTING )
            {
                // go to VGO directly with same vgo/tgo/lambda/lambdaDot/t_lambda
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

            if (status != PegStatus.FAILED && status != PegStatus.COASTING)
                status = PegStatus.CONVERGED;

            // restore saved guidance if we might have screwed it up
            if (!isStable())
            {
                lambda = oldlambda;
                lambdaDot = oldlambdaDot;
                t_lambda = oldt_lambda;
            }

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
                if ( stages[i].kspStage == -1 )
                {
                    // handle coast phase
                    S += L * stages[i].dt;
                    Q += J * stages[i].dt;
                    P += H * stages[i].dt;
                    continue;
                }

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

            // K = tgo - S / L;  // J / L;
            K = J / L;
            double QP = Q - S * K;

            double ldm = lambdaDot.magnitude;

            /*
            double Kp = tgo / 2;
            double theta = ldm * Kp;
            double f1;
            if (theta == 0.0)
                f1 = 1.0;
            else
                f1 = Math.Sin(theta) / theta;
            double f2;
            if (theta == 0.0)
                f2 = 1.0;
            else
                f2 = 3.0 * ( f1 - Math.Cos(theta) ) / ( theta * theta );
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
                    // this comes from rgo = S * lambda + QP * lambdaDot (simplified rthrust)
                    lambdaDot = ( rgo - S * lambda ) / QP;
            }

            ldm = lambdaDot.magnitude;

            // large values of phimax get weird even though they converge
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
/*
            if ( theta > phiMax )
            {
                ldm = phiMax / Kp;
                lambdaDot = lambdaDot.normalized * ldm;
                rgo = ST * lambda + QT * lambdaDot;
            }
            */

            if ( lambdaDot.magnitude > phiMax / K )
            {
                ldm = phiMax / K;
                lambdaDot = lambdaDot.normalized * ldm;
                //rgo = ST * lambda + QT * lambdaDot;
                rgo = S * lambda + QP * lambdaDot;
            }

            t_lambda = vesselState.time + K;

            /*
            // Kp = tgo / 2;
            theta = ldm * Kp;
            if (theta == 0.0)
                f1 = 1.0;
            else
                f1 = Math.Sin(theta) / theta;
            if (theta == 0.0)
                f2 = 1.0;
            else
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

            Vector3d vthrust, rthrust;

//                vthrust = LT * lambda;
//                rthrust = ST * lambda + QT * lambdaDot;

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

            /*
            if ( imode == IncMode.FREE_LAN && tgo > 40 && isStable() )
            {
                // FIXME: doesn't look like this works without the rgo corrections (which affect ix here?) but those break accurate targetting...
                // correct iy to fixed inc with free LAN
                double d = Vector3d.Dot( -Planetarium.up, ix );
                double SE = - 0.5 * ( Vector3d.Dot( -Planetarium.up, iy) + Math.Cos(incval * UtilMath.Deg2Rad) ) * Vector3d.Dot( -Planetarium.up, iz ) / (1 - d * d);
                iy = ( iy * Math.Sqrt( 1 - SE * SE ) + SE * iz ).normalized;
            }
            */

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
            /* if ( imode == IncMode.FREE_LAN )
                SetPlaneFromInclination(incval); */
            coastDone = 0.0;
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

        public void SynchStats()
        {
            for (int i = 0; i < stages.Count; i++ )
            {
                if (stages[i].kspStage == -1 )
                    continue;

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
                if ( i == coastAfterStage - 1 && coastRemain > 0 )
                {
                    StageInfo stage = new StageInfo();
                    stage.kspStage = -1;
                    stage.dt = coastRemain;
                    stage.Li = 0;
                    newlist.Add(stage);
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
                if (stages[i].kspStage == -1)
                    continue;

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
