using System;
using UnityEngine;
using KSP.UI.Screens;
using System.Collections.Generic;
using System.Reflection;

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
 *  - UX overhaul
 *    - bring back stock node executor and allow switching
 *    - embed PEG details into maneuver planner when PEG is doing the node executor
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
    public enum PegStatus { ENABLED, INITIALIZING, CONVERGED, BURNING, COASTING, BURNING_STAGING, COASTING_STAGING, TERMINAL, TERMINAL_RCS, FINISHED, FAILED };

    public class MechJebModulePEGController : ComputerModule
    {
        public MechJebModulePEGController(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble pegInterval = new EditableDouble(0.01);

        // these variables will persist even if Reset() completely blows away the solution, so that pitch+heading will still be stable
        // until a new solution is found.
        public Vector3d lambda;
        public Vector3d lambdaDot;
        public double t_lambda;
        public Vector3d iF;
        public double pitch;
        public double heading;
        public double tgo;
        public double vgo;

        // this is a public setting to control autowarping
        public bool autowarp = false;

        public PontryaginBase.Solution solution { get { return ( p != null ) ? p.solution : null; } }
        public List<PontryaginBase.Arc> arcs { get { return ( solution != null) ? p.solution.arcs : null; } }

        public PegStatus status;
        public PegStatus oldstatus;

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

        public double last_stage_time = 0.0;
        public double last_optimizer_time = 0.0;

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onStageActivate.Add(handleStageEvent);
        }

        public override void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(handleStageEvent);
        }

        private void handleStageEvent(int data)
        {
            last_stage_time = vesselState.time;
        }

        public override void OnModuleEnabled()
        {
            // coast phases are deliberately not reset in Reset() so we never get a completed coast phase again after whacking Reset()
            status = PegStatus.ENABLED;
            // core.AddToPostDrawQueue(DrawCSE);
            core.attitude.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.users.Remove(this);
            status = PegStatus.FINISHED;
            p.KillThread();
            p = null;
        }

        public bool allow_execution = false;

        // ENABLED just means the module is enabled, by calling Reset here we transition from ENABLED to INITIALIZING
        //
        // by setting allow_execution here we allow moving to COASTING or BURNING from INITIALIZED
        // FIXME: we don't seem to need allow_execution = false now?
        public void AssertStart(bool allow_execution = true)
        {
            if (status == PegStatus.ENABLED )
                Reset();
            this.allow_execution = allow_execution;
        }

        public override void OnFixedUpdate()
        {
            pegInterval = MuUtils.Clamp(pegInterval, 0.10, 1.00);

            update_pitch_and_heading();

            if ( isLoadedPrincipia )
            {
                // Debug.Log("FOUND PRINCIPIA!!!");
            }

            if ( !HighLogic.LoadedSceneIsFlight )
            {
                Debug.Log("MechJebModulePEGController [BUG]: PEG enabled in non-flight mode.  How does this happen?");
                Done();
            }

            if ( !enabled || status == PegStatus.ENABLED )
                return;

            if ( status == PegStatus.FINISHED )
            {
                Done();
                return;
            }

            if ( status == PegStatus.FAILED )
                Reset();

            bool has_rcs = vessel.hasEnabledRCSModules() && vessel.ActionGroups[KSPActionGroup.RCS] && ( vesselState.rcsThrustAvailable.up > 0.01 );

            if (p != null && p.solution != null && tgo < TimeWarp.fixedDeltaTime)
            {
                if (has_rcs)
                {
                    status = PegStatus.TERMINAL_RCS;
                }
                else
                {
                    Done();
                    return;
                }
            }

            if ( p != null && p.solution != null && tgo < 10 )
            {
                Vector3d dVleft = p.solution.vf() - vesselState.orbitalVelocity;
                double angle = Math.Acos(Vector3d.Dot(dVleft/dVleft.magnitude, vesselState.forward))*UtilMath.Rad2Deg;
                Debug.Log("dVleft = " + dVleft + " angle = " + angle);
            }

            if ( status == PegStatus.TERMINAL_RCS )
            {
                Vector3d dVleft = p.solution.vf() - vesselState.orbitalVelocity;
                double angle = Math.Acos(Vector3d.Dot(dVleft/dVleft.magnitude, vesselState.forward))*UtilMath.Rad2Deg;
                if (angle > 45)
                {
                    Done();
                    return;
                }
            }


            handle_throttle();

            handle_vacstats();

            handle_staging();

            converge();

        }

        /*
         * TARGET APIs
         */

        public void TargetNode(ManeuverNode node, double burntime)
        {
            if (p == null || isCoasting())
            {
                PontryaginNode solver = p as PontryaginNode;
                if (solver == null)
                    solver = new PontryaginNode(mu: mainBody.gravParameter, r0: vesselState.orbitalPosition, v0: vesselState.orbitalVelocity, pv0: node.GetBurnVector(orbit).normalized, pr0: Vector3d.zero, dV: node.GetBurnVector(orbit).magnitude, bt: burntime);
                solver.intercept(node.nextPatch);
                p = solver;
            }
        }

        // FIXME: convert v0m/r0m to vTm/rTm because its terminal not initial
        /* converts PeA + ApA into r0m/v0m for periapsis insertion.
           - handles hyperbolic orbits
           - remaps ApA < PeA onto circular orbits */
        private void ConvertToRadVel(double PeA, double ApA, out double r0m, out double v0m, out double sma)
        {
            double PeR = mainBody.Radius + PeA;
            double ApR = mainBody.Radius + ApA;

            r0m = PeR;

            sma = (PeR + ApR) / 2;

            /* remap nonsense ApAs onto circular orbits */
            if ( ApA >= 0 && ApA < PeA )
                sma = PeR;

            v0m = Math.Sqrt( mainBody.gravParameter * ( 2 / PeR - 1 / sma ) );
        }

        double old_v0m;
        double old_r0m;
        double old_inc;

        public void TargetPeInsertMatchOrbitPlane(double PeA, double ApA, Orbit o)
        {
            bool doupdate = false;

            double v0m, r0m, sma;

            ConvertToRadVel(PeA, ApA, out r0m, out v0m, out sma);

            if (r0m != old_r0m || v0m != old_v0m)
                doupdate = true;

            if (p == null || doupdate)
            {
                if (p != null)
                    p.KillThread();

                lambdaDot = Vector3d.zero;
                double desiredHeading = OrbitalManeuverCalculator.HeadingForInclination(o.inclination, vesselState.latitude);
                Vector3d desiredHeadingVector = Math.Sin(desiredHeading * UtilMath.Deg2Rad) * vesselState.east + Math.Cos(desiredHeading * UtilMath.Deg2Rad) * vesselState.north;
                Vector3d desiredThrustVector = Math.Cos(45 * UtilMath.Deg2Rad) * desiredHeadingVector + Math.Sin(45 * UtilMath.Deg2Rad) * vesselState.up;  /* 45 pitch guess */
                lambda = desiredThrustVector;
                PontryaginLaunch solver = new PontryaginLaunch(mu: mainBody.gravParameter, r0: vesselState.orbitalPosition, v0: vesselState.orbitalVelocity, pv0: lambda.normalized, pr0: Vector3d.zero, dV: v0m);
                QuaternionD rot = Quaternion.Inverse(Planetarium.fetch.rotation);
                Debug.Log("o.h = " + -o.h.xzy);
                Vector3d pos, vel;
                o.GetOrbitalStateVectorsAtUT(vesselState.time, out pos, out vel);
                Debug.Log("r x v = " + Vector3d.Cross(rot * pos.xzy, rot * vel.xzy));
                double hTm = v0m * r0m; // FIXME: gamma
                Debug.Log("hTm = " + hTm + " sma = " + sma);
                solver.flightangle5constraint(r0m, v0m, 0, o.h.normalized * hTm);
                p = solver;
            }

            old_v0m = v0m;
            old_r0m = r0m;
        }

        public void TargetPeInsertMatchInc(double PeA, double ApA, double inc)
        {
            bool doupdate = false;

            double v0m, r0m, sma;

            ConvertToRadVel(PeA, ApA, out r0m, out v0m, out sma);

            if (r0m != old_r0m || v0m != old_v0m || inc != old_inc)
                doupdate = true;

            if (p == null || doupdate)
            {
                if (p != null)
                    p.KillThread();

                lambdaDot = Vector3d.zero;
                double desiredHeading = OrbitalManeuverCalculator.HeadingForInclination(inc, vesselState.latitude);
                Vector3d desiredHeadingVector = Math.Sin(desiredHeading * UtilMath.Deg2Rad) * vesselState.east + Math.Cos(desiredHeading * UtilMath.Deg2Rad) * vesselState.north;
                Vector3d desiredThrustVector = Math.Cos(45 * UtilMath.Deg2Rad) * desiredHeadingVector + Math.Sin(45 * UtilMath.Deg2Rad) * vesselState.up;  /* 45 pitch guess */
                lambda = desiredThrustVector;
                PontryaginLaunch solver = new PontryaginLaunch(mu: mainBody.gravParameter, r0: vesselState.orbitalPosition, v0: vesselState.orbitalVelocity, pv0: lambda.normalized, pr0: Vector3d.zero, dV: v0m);
                solver.flightangle4constraint(r0m, v0m, 0, inc * UtilMath.Deg2Rad);
                p = solver;
            }

            old_v0m = v0m;
            old_r0m = r0m;
            old_inc = inc;
        }

        /* meta state for consumers that means "is iF usable?" (or pitch/heading) */
        public bool isStable()
        {
            return isNormal() || isTerminalGuidance();
        }

        // not TERMINAL guidance or TERMINAL_RCS
        public bool isNormal()
        {
            return status == PegStatus.CONVERGED || status == PegStatus.BURNING || status == PegStatus.BURNING_STAGING || status == PegStatus.COASTING || status == PegStatus.COASTING_STAGING;
        }

        public bool isCoasting()
        {
            return status == PegStatus.COASTING || status == PegStatus.COASTING_STAGING;
        }

        public bool isStaging()
        {
            return status == PegStatus.BURNING_STAGING || status == PegStatus.COASTING_STAGING;
        }

        public bool isTerminalGuidance()
        {
            return status == PegStatus.TERMINAL || status == PegStatus.TERMINAL_RCS;
        }

        /* normal pre-states but not usefully converged */
        public bool isInitializing()
        {
            return status == PegStatus.ENABLED || status == PegStatus.INITIALIZING;
        }

        private PontryaginBase p;

        private void handle_staging()
        {
            if (p == null || p.solution == null)
                return;

            if (isStaging())
                return;

            PontryaginBase.Arc current_arc = null;

            int i;

            for(i = 0; i < p.solution.arcs.Count; i++)
            {
                current_arc = p.solution.arcs[i];

                if ( ( current_arc.thrust != 0 && current_arc.ksp_stage > StageManager.CurrentStage) || (p.solution.tgo(vesselState.time, i) <= 0) )
                {
                    current_arc.done = true;
                }
                else
                {
                    break;
                }
            }

            /*
            if (i == p.solution.arcs.Count && current_arc.thrust == 0)
            {
                current_arc.done = true;
            }
            */
        }

        private void handle_vacstats()
        {
            if (p == null)
                return;

            stats.RequestUpdate(this, true);

            List<int>kspstages = new List<int>();

            for ( int i = vacStats.Length-1; i >= 0; i-- )
                if ( vacStats[i].deltaV > 20 ) // FIXME: tweakable
                    kspstages.Add(i);

            if (p != null)
                p.SynchStages(kspstages, vacStats, vesselState.mass, vesselState.thrustCurrent, vesselState.time);
        }

        private void converge()
        {
            if (p == null)
            {
                status = PegStatus.INITIALIZING;
                return;
            }

            if (p.solution == null)
            {
                /* we have a solver but no solution */
                status = PegStatus.INITIALIZING;
            }
            else
            {
                /* we have a solver and have a valid solution */
                if ( isTerminalGuidance() )
                    return;

                /* hardcoded 1 second of terminal guidance */
                if ( tgo < 1 )
                {
                    status = PegStatus.TERMINAL;
                    return;
                }
            }

            if ( (vesselState.time - last_optimizer_time) < pegInterval )
                return;

            if ( isStaging() )
                return;

            // for last 10 seconds of coast phase don't recompute (FIXME: can this go lower?  it was a workaround for a bug)
            if ( p.solution != null && p.solution.arc(vesselState.time).thrust == 0 && p.solution.current_tgo(vesselState.time) < 10 )
                return;

            p.UpdatePosition(vesselState.orbitalPosition, vesselState.orbitalVelocity, lambda, lambdaDot, tgo, vgo);

            p.threadStart(vesselState.time);
            //if ( p.threadStart(vesselState.time) )
                //Debug.Log("MechJeb: started optimizer thread");

            if (status == PegStatus.INITIALIZING && p.solution != null)
                status = PegStatus.CONVERGED;

            last_optimizer_time = vesselState.time;
        }

        private int last_burning_stage;
        private bool last_burning_stage_complete;
        private double last_coasting_time = 0.0;

        // if we're transitioning from a complete thrust phase to a coast, wait for staging, otherwise
        // just go off of whatever the solution says for the current time.
        private bool actuallyCoasting()
        {
            PontryaginBase.Arc current_arc = p.solution.arc(vesselState.time);

            if ( last_burning_stage_complete && last_burning_stage <= StageManager.CurrentStage )
                return false;
            return current_arc.thrust == 0;
        }

        private void handle_throttle()
        {
            if ( p == null || p.solution == null )
                return;

            if ( !allow_execution )
                return;

            if ( status == PegStatus.TERMINAL_RCS )
            {
                RCSOn();
                return;
            }

            PontryaginBase.Arc current_arc = p.solution.arc(vesselState.time);

            if ( current_arc.thrust != 0 )
            {
                last_burning_stage = current_arc.ksp_stage;
                last_burning_stage_complete = current_arc.complete_burn;
            }

            if ( actuallyCoasting() )
            {
                if ( !isTerminalGuidance() )
                {
                    if (vesselState.time < last_stage_time + 4)
                        status = PegStatus.COASTING_STAGING;
                    else
                        status = PegStatus.COASTING;
                }
                last_coasting_time = vesselState.time;

                core.staging.autostageLimitInternal = last_burning_stage - 1;
                ThrustOff();
            }
            else
            {
                if ( !isTerminalGuidance() )
                {
                    if ((vesselState.time < last_stage_time + 4) || (vesselState.time < last_coasting_time + 4))
                        status = PegStatus.BURNING_STAGING;
                    else
                        status = PegStatus.BURNING;
                }

                if (core.staging.autostageLimitInternal > 0)
                {
                    core.staging.autostageLimitInternal = 0;
                }
                else
                {
                    ThrustOn();
                }
            }
        }

        /* extract pitch and heading off of iF to avoid continuously recomputing on every call */
        private void update_pitch_and_heading()
        {
            // FIXME: if we have no solution update off of lambda + lambdaDot + last update time
            if (p == null || p.solution == null)
                return;

            // if we're not flying yet, continuously update the t0 of the solution
            if ( vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH || vessel.situation == Vessel.Situations.SPLASHED )
                p.solution.t0 = vesselState.time;

            if ( status == PegStatus.TERMINAL_RCS )
            {
                /* leave pitch, heading and lambda at the last values, also stop updating vgo/tgo */
                lambdaDot = Vector3d.zero;
            }
            else
            {
                lambda = p.solution.pv(vesselState.time);
                lambdaDot = p.solution.pr(vesselState.time);
                iF = lambda.normalized;
                p.solution.pitch_and_heading(vesselState.time, ref pitch, ref heading);
                tgo = p.solution.tgo(vesselState.time);
                vgo = p.solution.vgo(vesselState.time);
            }
        }

        private void ThrustOn()
        {
            core.thrust.targetThrottle = 1.0F;
            vessel.ctrlState.X = vessel.ctrlState.Y = vessel.ctrlState.Z = 0.0f;
        }

        private void RCSOn()
        {
            core.thrust.ThrustOff();
            vessel.ctrlState.Z = -1.0F;
        }

        private void ThrustOff()
        {
            core.thrust.ThrustOff();
            vessel.ctrlState.X = vessel.ctrlState.Y = vessel.ctrlState.Z = 0.0f;
        }

        private void Done()
        {
            users.Clear();
            ThrustOff();
            status = PegStatus.FINISHED;
            enabled = false;
        }

        public void Reset()
        {
            // lambda and lambdaDot are deliberately not cleared here
            p.KillThread();
            p = null;
            status = PegStatus.INITIALIZING;
            last_stage_time = 0.0;
            last_optimizer_time = 0.0;
            last_coasting_time = 0.0;
            autowarp = false;
            if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
        }

        public static bool isLoadedPrincipia = false;
        public static MethodInfo principiaEGNPCDOF;

        static MechJebModulePEGController()
        {
            isLoadedPrincipia = ReflectionUtils.isAssemblyLoaded("ksp_plugin_adapter");
            if (isLoadedPrincipia)
            {
                principiaEGNPCDOF = ReflectionUtils.getMethodByReflection("ksp_plugin_adapter", "principia.ksp_plugin_adapter.Interface", "ExternalGetNearestPlannedCoastDegreesOfFreedom", BindingFlags.NonPublic | BindingFlags.Static);
                if (principiaEGNPCDOF == null)
                {
                    // Debug.Log("failed to find ExternalGetNearestPlannedCoastDegreesOfFreedom");
                    isLoadedPrincipia = false;
                    return;
                }
            }
        }
    }
}
