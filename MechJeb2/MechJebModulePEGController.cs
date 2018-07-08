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
 *    - move most of the ascent buttons into the main ascent menu.
 *    - hide things like corrective steering when using PEG that do not apply.
 *    - consider drop down for different kinds of PEG targets in the UX
 *    - debug vector toggle button somewhere
 *    - PEG details hidden by default
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
    public enum PegStatus { ENABLED, INITIALIZING, INITIALIZED, SLEWING, CONVERGED, STAGING, TERMINAL, TERMINAL_RCS, FINISHED, FAILED, COASTING };

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
        public Vector3d iF;
        public double pitch;
        public double heading;
        public double tgo;
        public double vgo;

        public PegStatus status;
        public PegStatus oldstatus;

        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] vacStats { get { return stats.vacStats; } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

        // should only be called once per burn/ascent
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
            p = null;
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

            converge();

            if (p != null && p.solution != null && tgo < TimeWarp.fixedDeltaTime)
                Done();
        }

        // state for next iteration
        private double last_PEG;    // this is the last PEG update time

        /*
         * TARGET APIs
         */

        public bool coasting;

        public void TargetNode(ManeuverNode node, bool force_vgo = false)
        {
            if (p == null)
            {
                coasting = true;
                lambda = node.GetBurnVector(orbit).normalized;
                lambdaDot = Vector3d.zero;
                /*
                PontryaginNode solver = new PontryaginNode(mu: mainBody.gravParameter, r0: vesselState.orbitalPosition, v0: vesselState.orbitalVelocity, pv0: lambda.normalized, pr0: Vector3d.zero, dV: v0m);
                solver.intercept(orbit, node.nextPatch, coasting);
                p = solver;
                */
            }
        }

        public void TargetPeInsertMatchPlane(double PeA, double ApA, Vector3d tangent)
        {
            throw new Exception("FIXME");
        }

        public void TargetPeInsertMatchOrbitPlane(double PeA, double ApA, Orbit o)
        {
            throw new Exception("FIXME");
        }

        double v0m;
        double r0m;
        double inc;

        public void TargetPeInsertMatchInc(double PeA, double ApA, double inc)
        {
            bool doupdate = false;

            if (r0m != this.r0m || v0m != this.v0m || inc != this.inc)
            {
                this.r0m = PeA + mainBody.Radius;
                this.v0m = Math.Sqrt(mainBody.gravParameter/r0m);
                this.inc = inc;
                doupdate = true;
            }

            if (p == null || doupdate)
            {
                if (p != null)
                    p.KillThread();

                /* this guesses the costate */
                Debug.Log("v0m = " + v0m);
                Debug.Log("r0m = " + r0m);
                Debug.Log("inc = " + inc);
                lambdaDot = Vector3d.zero;
                double desiredHeading = OrbitalManeuverCalculator.HeadingForInclination(inc, vesselState.latitude);
                Vector3d desiredHeadingVector = Math.Sin(desiredHeading * UtilMath.Deg2Rad) * vesselState.east + Math.Cos(desiredHeading * UtilMath.Deg2Rad) * vesselState.north;
                Vector3d desiredThrustVector = Math.Cos(45 * UtilMath.Deg2Rad) * desiredHeadingVector + Math.Sin(45 * UtilMath.Deg2Rad) * vesselState.up;  /* 45 pitch guess */
                lambda = desiredThrustVector;
                Debug.Log("r0 = " + vesselState.orbitalPosition + " v0 = " + vesselState.orbitalVelocity);
                Debug.Log("desiredHeading = " + desiredHeading + " desiredHeadingVector = " + desiredHeadingVector + " desiredThrustVector = " + desiredThrustVector);
                PontryaginLaunch solver = new PontryaginLaunch(mu: mainBody.gravParameter, r0: vesselState.orbitalPosition, v0: vesselState.orbitalVelocity, pv0: lambda.normalized, pr0: Vector3d.zero, dV: v0m);
                solver.flightangle4constraint(r0m, v0m, 0, inc * UtilMath.Deg2Rad);
                p = solver;
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

        private double last_call;        // this is the last call to converge
        private PontryaginBase p;

        private void converge()
        {
            if (p == null)
                return;

            if (p != null && p.solution != null && tgo < 2)
            {
                status = PegStatus.TERMINAL;
                return;
            }

            // FIXME: we need to add liveStats to vacStats and atmoStats from MechJebModuleStageStats so we don't have to force liveSLT here
            stats.liveSLT = true;
            stats.RequestUpdate(this, true);

            UpdateStages();

            p.UpdatePosition(vesselState.orbitalPosition, vesselState.orbitalVelocity, lambda, lambdaDot, tgo, vgo);

            if ( p.threadStart(vesselState.time) )
                Debug.Log("started thread");

            last_call = vesselState.time;

            if (p.solution == null)
                status = PegStatus.INITIALIZING;
            else
                status = PegStatus.CONVERGED;

            last_PEG = vesselState.time;
        }

        /* extract pitch and heading off of iF to avoid continuously recomputing on every call */
        private void update_pitch_and_heading()
        {
            if (p == null || p.solution == null)
                return;

            if ( vessel.situation == Vessel.Situations.LANDED || vessel.situation == Vessel.Situations.PRELAUNCH || vessel.situation == Vessel.Situations.SPLASHED )
                p.solution.t0 = vesselState.time;

            lambda = p.solution.pv(vesselState.time);
            lambdaDot = p.solution.pr(vesselState.time);
            iF = lambda.normalized;
            p.solution.pitch_and_heading(vesselState.time, ref pitch, ref heading);
            tgo = p.solution.tgo(vesselState.time);
            vgo = p.solution.vgo(vesselState.time);
        }

        private void Done()
        {
            users.Clear();
            core.thrust.ThrustOff();
            vessel.ctrlState.X = vessel.ctrlState.Y = vessel.ctrlState.Z = 0.0f;
            status = PegStatus.FINISHED;
            enabled = false;
        }

        public void Reset()
        {
            // lambda and lambdaDot are deliberately not cleared here
            p = null;
            status = PegStatus.INITIALIZING;
            last_PEG = 0.0;
            last_call = 0.0;
        }

        private void UpdateStages()
        {
            List<int>kspstages = new List<int>();

            for ( int i = vacStats.Length-1; i >= 0; i-- )
                if ( vacStats[i].deltaV > 20 )
                    kspstages.Add(i);

            if (p != null)
                p.SynchStages(kspstages, vacStats, vesselState.mass);
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
