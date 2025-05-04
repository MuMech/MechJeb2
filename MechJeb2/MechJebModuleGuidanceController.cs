/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using MechJebLib.PVG;
using MechJebLibBindings;
using UnityEngine;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MuMech
{
    public enum PVGStatus { ENABLED, INITIALIZED, BURNING, COASTING, TERMINAL, TERMINAL_RCS, TERMINAL_STAGING, FINISHED }

    /// <summary>
    ///     The guidance controller for PVG (responsible for taking a Solution from PVG and flying it)
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
        public double Tgo;
        public double VGO;
        public double StartCoast;

        public Solution? Solution;

        public PVGStatus Status = PVGStatus.ENABLED;

        public override void OnStart(PartModule.StartState state)
        {
            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
            {
                Core.AddToPostDrawQueue(DrawTrajetory);
            }
        }

        protected override void OnModuleEnabled()
        {
            Status = PVGStatus.ENABLED;
            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
            Core.Spinup.Users.Add(this);
            Solution        = null;
            _allowExecution = false;
        }

        protected override void OnModuleDisabled()
        {
            Core.Attitude.attitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.Users.Remove(this);
            Core.Spinup.Users.Remove(this);
            Solution = null;
            Status   = PVGStatus.FINISHED;
        }

        private bool _allowExecution;

        // we wait until we get a signal to allow execution to start
        public void AssertStart(bool allowExecution = true)
        {
            _allowExecution = allowExecution;
        }

        public override void OnFixedUpdate()
        {
            UpdatePitchAndHeading();

            if (!HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("MechJebModuleGuidanceController [BUG]: PVG enabled in non-flight mode.  How does this happen?");
                Done();
            }

            if (!Enabled || Status == PVGStatus.ENABLED)
                return;

            if (Status == PVGStatus.FINISHED)
            {
                Done();
                return;
            }

            // RO/RF: if we are spooling up engines, then flush the PID integrators to prevent windup
            if (Status == PVGStatus.BURNING || Status == PVGStatus.TERMINAL)
                if (VesselState.thrustCurrent < VesselState.thrustMinimum*0.98)
                    Core.Attitude.Controller.Reset();

            HandleTerminal();

            HandleSpinup();

            HandleThrottle();

            DrawTrajetory();
        }

        private bool WillDoRCSButNotYet()
        {
            // We might have wonky transforms and have a tiny bit of fore RCS, so require at least 10% of the max RCS thrust to be
            // in the pointy direction (which should be "up" / y-axis per KSP/Unity semantics).
            bool hasRCS = Vessel.hasEnabledRCSModules() &&
                          VesselState.rcsThrustAvailable.Up > 0.1 * VesselState.rcsThrustAvailable.MaxMagnitude();

            return hasRCS && Status != PVGStatus.TERMINAL_RCS && Vessel.currentStage == _ascentSettings.LastStage;
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
            if (Vessel.currentStage < _ascentSettings.LastStage)
            {
                Done();
                return;
            }

            // this handles termination of thrust for final stages of "fixed" burntime rockets (due to residuals Tgo may go less than zero so we
            // wait for natural termination of thrust).   no support for RCS terminal trim.
            if (_ascentSettings.OptimizeStage < 0 && Vessel.currentStage <= _ascentSettings.LastStage && Solution.Tgo(VesselState.time) <= 0 &&
                VesselState.thrustAvailable == 0)
            {
                Done();
                return;
            }

            // TERMINAL_STAGING is set on a non-upper stage optimized stage, once we are no longer in the optimized stage
            // then we have staged, so we need to reset that condition.  Otherwise we need to wait for staging.
            if (Status == PVGStatus.TERMINAL_STAGING)
            {
                if (Vessel.currentStage == _ascentSettings.OptimizeStage)
                    return;

                Status = PVGStatus.BURNING;
            }

            // We should either be in an non-upper stage optimized stage, or we should be within 10 seconds of the whole
            // burntime in order to enter terminal guidance.
            if (Vessel.currentStage != _ascentSettings.OptimizeStage && Solution.Tgo(VesselState.time) > 10)
                return;

            // The includeCoast: false flag here is to skip a coast which is in the past in the Solution when
            // we are ending the coast and the optimizer hasn't run the solution, but CoastBefore is set so
            // that both the coast and burn have the same KSPStage.  So we want the index of the current burn
            // and not the index of the first matching KSPStage in the Solution which is the coast.  Might
            // also consider modifying APIs like IndexForKSPStage to omit stages which are in the past -- but
            // I have concerns about that with residuals where you may currently be in a burning stage which
            // is in the "past" in the Solution but you're burning down residuals and you don't know when
            // the stage will actually run out (assuming it isn't a burn before a coast or an optimized burntime
            // so that we burn past the end of the stage and into whatever residuals are available).
            int solutionIndex = Solution.IndexForKSPStage(Vessel.currentStage, Core.Guidance.IsCoasting());
            if (solutionIndex < 0)
                return;

            // Only enter terminal guidance within 10 seconds of the current stage
            if (Solution.Tgo(VesselState.time, solutionIndex) > 10)
                return;

            if (Status != PVGStatus.TERMINAL_RCS)
                Status = PVGStatus.TERMINAL;

            Core.Warp.MinimumWarp();

            if (Status == PVGStatus.TERMINAL_RCS && !Vessel.ActionGroups[KSPActionGroup.RCS]) // if someone manually disables RCS
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
            Vector3d v1 = VesselState.orbitalVelocity + a0 * dt;
            Vector3d x1 = VesselState.orbitalPosition + VesselState.orbitalVelocity * dt + 0.5 * a0 * dt * dt;

            bool shouldEndTerminal = false;

            // this handles ending TERMINAL guidance (but not TERMINAL_RCS) due to thrust fault in the last stage
            if (Status == PVGStatus.TERMINAL && VesselState.thrustCurrent == 0 && Vessel.currentStage < _ascentSettings.LastStage.Val)
            {
                Debug.Log("[MechJebModuleGuidanceController] no thrust in last stage.");
                shouldEndTerminal = true;
            }

            if (Solution.TerminalGuidanceSatisfied(x1.WorldToV3Rotated(), v1.WorldToV3Rotated(), solutionIndex))
                shouldEndTerminal = true;

            if (shouldEndTerminal)
            {
                if (WillDoRCSButNotYet())
                {
                    Debug.Log("[MechJebModuleGuidanceController] transition to RCS terminal guidance.");
                    Status = PVGStatus.TERMINAL_RCS;
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

        public bool IsTerminal() => Status == PVGStatus.TERMINAL_RCS || Status == PVGStatus.TERMINAL_STAGING || Status == PVGStatus.TERMINAL;

        /* is guidance usable? */
        public bool IsStable() => IsNormal() || IsTerminal();

        // either ENABLED and waiting for a Solution, or executing a solution "normally" (not terminal, not failed)
        public bool IsReady() => Status == PVGStatus.ENABLED || IsNormal();

        // not TERMINAL guidance or TERMINAL_RCS -- when we should be running the optimizer
        public bool IsNormal() => Status == PVGStatus.INITIALIZED || Status == PVGStatus.BURNING || Status == PVGStatus.COASTING;

        public bool IsCoasting() => Status == PVGStatus.COASTING;

        private bool IsThrustOn() => IsBurning() || IsTerminal();

        private bool IsBurning() => Status == PVGStatus.BURNING;

        /* normal pre-states but not usefully converged */
        public bool IsInitializing() => Status == PVGStatus.ENABLED || Status == PVGStatus.INITIALIZED;

        private void HandleThrottle()
        {
            if (Solution == null)
                return;

            if (!_allowExecution)
                return;

            if (Status == PVGStatus.TERMINAL_RCS)
            {
                RCSOn();
                return;
            }

            if (Status == PVGStatus.TERMINAL || Status == PVGStatus.TERMINAL_STAGING)
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
            if (coastStage >= 0 && Vessel.currentStage >= coastStage && Solution.WillCoast(VesselState.time))
                Core.Staging.AutoStageLimitRequest(coastStage, this);
            else
                Core.Staging.AutoStageLimitRequest(Solution.TerminalStage(), this);

            if (Solution.Coast(VesselState.time))
            {
                if (!IsCoasting())
                {
                    DoCoast();
                }

                if (Solution.StageTimeLeft(VesselState.time) < UllageLeadTime)
                    RCSOn();

                ThrustOff();
            }
            else
            {
                ThrottleOn();

                Status = PVGStatus.BURNING;
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
                Solution.T0 = VesselState.time;

            if (Status != PVGStatus.TERMINAL_RCS)
            {
                (double pitch, double heading) = Solution.PitchAndHeading(VesselState.time);
                Pitch                          = Rad2Deg(pitch);
                Heading                        = Rad2Deg(heading);
                Tgo                            = Solution.Tgo(VesselState.time);
                VGO                            = Solution.Vgo(VesselState.time);
            }
            /* else leave pitch and heading at the last values, also stop updating vgo/tgo */
        }

        private readonly List<Vector3d> _trajectory = new List<Vector3d>();
        private readonly Orbit          _finalOrbit = new Orbit();

        private void DrawTrajetory()
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

        private void ThrottleOn() => Core.Thrust.TargetThrottle = 1.0F;

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
            if (Vessel.currentStage == Solution.CoastKSPStage() && Solution.WillCoast(VesselState.time))
            {
                ThrustOff();
                DoCoast();
                return;
            }

            // if we have more un-optimized upper stages to burn, stage and use the TERMINAL_STAGING state
            if (Solution.TerminalStage() != Vessel.currentStage && _ascentSettings.OptimizeStage == Vessel.currentStage)
            {
                ThrustOff();
                Core.Staging.ImmediateStage();
                Status = PVGStatus.TERMINAL_STAGING;
                return;
            }

            // otherwise we just have normal termination
            Done();
        }

        private void Done()
        {
            Users.Clear();
            ThrustOff();
            Status   = PVGStatus.FINISHED;
            Solution = null;
            Enabled  = false;
        }

        private void DoCoast()
        {
            StartCoast = VesselState.time;
            // force RCS on at the state transition
            if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            Status = PVGStatus.COASTING;
        }

        public void SetSolution(Solution solution)
        {
            Solution = solution;
            if (Status == PVGStatus.ENABLED)
                Status = PVGStatus.INITIALIZED;
        }

        // This API is necessary so that we know that there's no future coast on the trajectory so
        // we entirely suppress adding the coast in the glueball.  If there is no solution, then we're
        // bootstrapping so we want to add a coast if we need one, so we return false here.
        public bool HasGoodSolutionWithNoFutureCoast()
        {
            if (Solution == null)
                return false;

            return !Solution.WillCoast(VesselState.time);
        }
    }
}
