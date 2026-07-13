/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLibBindings;
using UnityEngine;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MuMech
{
    public enum PSGStatus { ENABLED, INITIALIZED, BURNING, COASTING, TERMINAL, TERMINAL_RCS, TERMINAL_STAGING, FINISHED }

    /// <summary>
    ///     The guidance controller for PSG (responsible for taking a Solution from PSG and flying it)
    /// </summary>
    public class MechJebModuleGuidanceController : ComputerModule
    {
        public MechJebModuleGuidanceController(MechJebCore core) : base(core) { }

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble UllageLeadTime = 20;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool ShouldDrawTrajectory = true;

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        public double Pitch;
        public double Heading;
        private V3 _inertial = V3.zero; // inertial in right-handed non-rotating
        private double _throttle;
        public Vector3d Inertial = Vector3d.zero; // inertial in rotating coordinates
        public double Tgo;
        public double Vgo;
        public double StartCoast;
        public bool hasCoasted;

        public Solution? Solution;

        public PSGStatus Status = PSGStatus.ENABLED;

        public override void OnStart(PartModule.StartState state)
        {
            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
            {
                Core.AddToPostDrawQueue(DrawTrajectory);
            }
        }

        protected override void OnModuleEnabled()
        {
            Debug.Log("MechJebModuleGuidanceController: Enabled");
            Status = PSGStatus.ENABLED;
            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
            Core.Spinup.Users.Add(this);
            Solution = null;
            _allowExecution = false;
            hasCoasted = false;
        }

        protected override void OnModuleDisabled()
        {
            Debug.Log("MechJebModuleGuidanceController: Disabled");
            Core.Attitude.attitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.Users.Remove(this);
            Core.Spinup.Users.Remove(this);
            Solution = null;
            Status = PSGStatus.FINISHED;
        }

        private bool _allowExecution;

        // we wait until we get a signal to allow execution to start
        public void AssertStart(bool allowExecution = true) => _allowExecution = allowExecution;

        public override void OnFixedUpdate()
        {
            UpdatePitchAndHeading();

            if (!HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("MechJebModuleGuidanceController [BUG]: PSG enabled in non-flight mode.  How does this happen?");
                Done();
            }

            if (!Enabled || Status == PSGStatus.ENABLED)
                return;

            if (Status == PSGStatus.FINISHED)
            {
                Done();
                return;
            }

            // RO/RF: if we are spooling up engines, then flush the PID integrators to prevent windup
            if (Status == PSGStatus.BURNING || Status == PSGStatus.TERMINAL)
                if (VesselState.ThrustCurrent < VesselState.ThrustMinimum * 0.98)
                    Core.Attitude.Controller.Reset();

            HandleTerminal();

            HandleSpinup();

            HandleThrottle();

            DrawTrajectory();
        }

        private bool WillDoRCSButNotYet()
        {
            if (Solution == null)
                return false;

            // We might have wonky transforms and have a tiny bit of fore RCS, so require at least 10% of the max RCS thrust to be
            // in the pointy direction (which should be "up" / y-axis per KSP/Unity semantics).
            bool hasRCS = Vessel.hasEnabledRCSModules() &&
                VesselState.RCSThrustAvailable.Up > 0.1 * VesselState.RCSThrustAvailable.MaxMagnitude();

            return hasRCS && Status != PSGStatus.TERMINAL_RCS && Vessel.currentStage == Solution.TerminalKSPStage();
        }

        private void HandleTerminal()
        {
            if (Solution == null)
                return;

            if (!IsThrustOn())
                return;

            if (IsGrounded())
                return;

            // if we've gone past the last stage we need to just stop
            if (Vessel.currentStage < Solution.TerminalKSPStage())
            {
                Done();
                return;
            }

            /*
             * FIXME: consider liquid booster()s + unguided beehive cluster
             *
             * 1. we should enter terminal guidance on the top liquid booster and shut it down precisely and suspend the optimizer
             *    there because we CANNOT use the residuals because we're about to loose our degrees of freedom.  but the optimizer should
             *    have burned this stage to completion.  we also don't want to use TERMINAL_RCS here.
             * 2. we should still enter terminal guidance on the top aerobee stage and precisely shut it down to actually hit the target as
             *    best as possible, and this burntime should have been tuned by the optimizer.  we do want to use TERMINAL_RCS here.
             *
             * Right now we only support one OptimizeKSPStage() and really the SolutionBuilder needs to know to slap a PreciseShutdown flag
             * onto the last liquid booster segment (even though the optimizer didn't optimize it) and likely could use the same flag for
             * precise shutdown of engines before a coast.  Although maybe there should be two different flags (for precise shutdown with
             * and without RCS cleanup).
             */

            // this handles termination of thrust for final stages of "fixed" burntime rockets (due to residuals Tgo may go less than zero so we
            // wait for natural termination of thrust).   no support for RCS terminal trim.
            if (Solution.OptimizeKSPStage() < 0 && Vessel.currentStage <= Solution.TerminalKSPStage() && Solution.Tgo(VesselState.Time) <= 0 &&
                VesselState.ThrustAvailable == 0)
            {
                Done();
                return;
            }

            // TERMINAL_STAGING is set on a non-upper stage optimized stage, once we are no longer in the optimized stage
            // then we have staged, so we need to reset that condition.  Otherwise we need to wait for staging.
            if (Status == PSGStatus.TERMINAL_STAGING)
            {
                if (Vessel.currentStage == Solution.OptimizeKSPStage())
                    return;

                Status = PSGStatus.BURNING;
            }

            // we need to have an optimizable stage to run terminal guidance
            if (Vessel.currentStage != Solution.OptimizeKSPStage())
                return;

            // we need to be within 10 seconds of the end of the stage to run terminal guidance
            if (Solution.TgoForKSPStage(VesselState.Time, Vessel.currentStage) > 10)
                return;

            // if we are in a coast then don't enter terminal guidance
            if (IsCoasting())
                return;

            // if we are coast-during then prevent coasting if we will be coasting this stage (first burn of burn-coast-burn)
            if (_ascentSettings.CoastLocation == 0 && Vessel.currentStage == Solution.CoastKSPStage() && Solution.WillCoast(VesselState.Time))
                return;

            if (Status != PSGStatus.TERMINAL_RCS)
                Status = PSGStatus.TERMINAL;

            Core.Warp.MinimumWarp();

            if (Status == PSGStatus.TERMINAL_RCS && !Vessel.ActionGroups[KSPActionGroup.RCS]) // if someone manually disables RCS
            {
                Debug.Log("[MechJebModuleGuidanceController] terminating guidance due to manual deactivation of RCS.");
                TerminalDone();
                return;
            }

            // stopping one tick short is more accurate for rockets without RCS, but sometimes we overshoot with only one tick
            int ticks = WillDoRCSButNotYet() ? 2 : 1;

            // bit of a hack to predict velocity + position in the next tick or two
            // FIXME: what exactly does KSP do to integrate over timesteps?
            Vector3d a0 = Vessel.acceleration_immediate;

            double dt = ticks * TimeWarp.fixedDeltaTime;
            Vector3d v1 = VesselState.OrbitalVelocity + a0 * dt;
            Vector3d x1 = VesselState.OrbitalPosition + VesselState.OrbitalVelocity * dt + 0.5 * a0 * dt * dt;

            bool shouldEndTerminal = false;

            // this handles ending TERMINAL guidance (but not TERMINAL_RCS) due to thrust fault in the last stage
            if (Status == PSGStatus.TERMINAL && VesselState.ThrustCurrent == 0 && Vessel.currentStage < Solution.TerminalKSPStage())
            {
                Debug.Log("[MechJebModuleGuidanceController] no thrust in last stage.");
                shouldEndTerminal = true;
            }

            if (Solution.TerminalGuidanceSatisfied(x1.WorldToV3Rotated(), v1.WorldToV3Rotated(), VesselState.Time))
                shouldEndTerminal = true;

            if (shouldEndTerminal)
            {
                if (WillDoRCSButNotYet())
                {
                    Debug.Log("[MechJebModuleGuidanceController] transition to RCS terminal guidance.");
                    Status = PSGStatus.TERMINAL_RCS;
                    if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                        Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                }
                else
                {
                    Debug.Log("[MechJebModuleGuidanceController] terminal guidance completed.");
                    TerminalDone();
                }
            }
        }

        private void HandleSpinup()
        {
            if (Vessel.currentStage != _ascentSettings.SpinupStage)
                return;

            Core.Spinup.AssertStart();
            Core.Spinup.RollAngularVelocity = _ascentSettings.SpinupAngularVelocity;
        }

        public bool IsTerminal() => Status == PSGStatus.TERMINAL_RCS || Status == PSGStatus.TERMINAL_STAGING || Status == PSGStatus.TERMINAL;

        /* is guidance usable? */
        public bool IsStable() => IsNormal() || IsTerminal();

        // either ENABLED and waiting for a Solution, or executing a solution "normally" (not terminal, not failed)
        public bool IsReady() => Status == PSGStatus.ENABLED || IsNormal();

        // not TERMINAL guidance or TERMINAL_RCS -- when we should be running the optimizer
        public bool IsNormal() => Status == PSGStatus.INITIALIZED || Status == PSGStatus.BURNING || Status == PSGStatus.COASTING;

        public bool IsCoasting() => Status == PSGStatus.COASTING;

        private bool IsThrustOn() => IsBurning() || IsTerminal();

        private bool IsBurning() => Status == PSGStatus.BURNING;

        /* normal pre-states but not usefully converged */
        public bool IsInitializing() => Status == PSGStatus.ENABLED || Status == PSGStatus.INITIALIZED;

        private void HandleThrottle()
        {
            if (Solution == null)
                return;

            if (!_allowExecution)
                return;

            if (Status == PSGStatus.TERMINAL_RCS)
            {
                RCSOn();
                return;
            }

            if (Status == PSGStatus.TERMINAL || Status == PSGStatus.TERMINAL_STAGING)
            {
                ThrottleOn();
                return;
            }

            int coastStage = Solution.CoastKSPStage();

            //Debug.Log($"coast stage: {coastStage} current stage: {Vessel.currentStage} will coast: {Solution.WillCoast(VesselState.time)}");


            // Stop autostaging if we need to do a coast.  Also, we need to affirmatively set the termination
            // of autostaging to the top of the rocket.  If we don't, then when we cut the engines and do the
            // RCS trim, autostaging will stage off the spent engine if there's no relights.  This is unwanted
            // since the insertion stage may still have RCS which is necessary to complete the mission.
            if (coastStage >= 0 && Vessel.currentStage >= coastStage && Solution.WillCoast(VesselState.Time))
                Core.Staging.AutoStageLimitRequest(coastStage, this);
            else
                Core.Staging.AutoStageLimitRequest(Solution.TerminalKSPStage(), this);

            if (Solution.Coast(VesselState.Time))
            {
                if (!IsCoasting())
                    DoCoast();

                if (Solution.StageTimeLeft(VesselState.Time) < UllageLeadTime)
                    RCSOn();

                ThrustOff();
            }
            else
            {
                ThrottleOn();

                Status = PSGStatus.BURNING;
            }
        }

        private bool IsGrounded() =>
            Vessel.situation == Vessel.Situations.LANDED ||
            Vessel.situation == Vessel.Situations.PRELAUNCH ||
            Vessel.situation == Vessel.Situations.SPLASHED;

        private void UpdatePitchAndHeading()
        {
            if (Solution == null)
                return;

            // if we're not flying yet, continuously update the t0 of the solution
            if (IsGrounded())
                Solution.T0 = VesselState.Time;

            if (Status != PSGStatus.TERMINAL_RCS)
            {
                // don't update vgo/tgo if we're in RCS
                Tgo = Solution.Tgo(VesselState.Time);
                Vgo = Solution.Vgo(VesselState.Time);
            }

            V3 r0 = VesselState.OrbitalPosition.WorldToV3Rotated();

            // lock the inertial heading at tgo < 2.0 for any staging event or terminal burnout
            // (2 seconds is to hopefully allow for variance due to residuals, it may be less)
            int idx = Solution.IndexForKSPStage(Vessel.currentStage, Core.Guidance.IsCoasting());
            if (IsGrounded() || Solution.Tgo(VesselState.Time, idx) > 2.0)
                (_inertial, _throttle) = Solution.InertialGuidance(VesselState.Time);

            (double pitch, double heading) = Astro.ECIToPitchHeading(r0, _inertial);

            Inertial = _inertial.V3ToWorldRotated();
            Pitch = Rad2Deg(pitch);
            Heading = Rad2Deg(heading);
        }

        private readonly List<Vector3d> _trajectory = new List<Vector3d>();
        private readonly Orbit _finalOrbit = new Orbit();

        private void DrawTrajectory()
        {
            if (Solution == null)
                return;

            if (!Enabled)
                return;

            if (!MapView.MapIsEnabled)
                return;

            if (!ShouldDrawTrajectory)
                return;

            const int SEGMENTS = 50;

            _trajectory.Clear();
            double dt = (Solution.Tf - Solution.T0) / SEGMENTS;

            for (int i = 0; i <= SEGMENTS; i++)
            {
                double t = Solution.T0 + dt * i;

                _trajectory.Add(Solution.R(t).V3ToWorldRotated() + MainBody.position);
            }

            GLUtils.DrawPath(MainBody, _trajectory, Color.red, MapView.MapIsEnabled);

            Vector3d rf = Planetarium.fetch.rotation * Solution.R(Solution.Tf).ToVector3d().xzy;
            Vector3d vf = Planetarium.fetch.rotation * Solution.V(Solution.Tf).ToVector3d().xzy;

            _finalOrbit.UpdateFromStateVectors(rf.xzy, vf.xzy, MainBody, Solution.Tf);

            GLUtils.DrawOrbit(_finalOrbit, Color.yellow);
        }

        private void ThrottleOn() => Core.Thrust.TargetThrottle = _throttle > 0 ? (float)_throttle : 1.0f;

        private void RCSOn()
        {
            Core.Thrust.ThrustOff();
            Vessel.ctrlState.Z = -1.0F;
        }

        private void ThrustOff() => Core.Thrust.ThrustOff();

        private void TerminalDone()
        {
            if (Solution == null)
            {
                Done();
                return;
            }

            // if we still have a coast to do in this stage, start the coast
            if (Vessel.currentStage == Solution.CoastKSPStage() && Solution.WillCoast(VesselState.Time))
            {
                ThrustOff();
                DoCoast();
                return;
            }

            // if we have more un-optimized upper stages to burn, stage and use the TERMINAL_STAGING state
            if (Solution.TerminalKSPStage() != Vessel.currentStage && Solution.OptimizeKSPStage() == Vessel.currentStage)
            {
                ThrustOff();
                Core.Staging.ImmediateStage();
                Status = PSGStatus.TERMINAL_STAGING;
                return;
            }

            // otherwise we just have normal termination
            Done();
        }

        private void Done()
        {
            Users.Clear();
            ThrustOff();
            Status = PSGStatus.FINISHED;
            Solution = null;
            Enabled = false;
        }

        private void DoCoast()
        {
            StartCoast = VesselState.Time;
            // force RCS on at the state transition
            if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            Status = PSGStatus.COASTING;

            hasCoasted = true;
        }

        public void SetSolution(Solution solution)
        {
            Solution = solution;
            if (Status == PSGStatus.ENABLED)
                Status = PSGStatus.INITIALIZED;
        }
    }
}
