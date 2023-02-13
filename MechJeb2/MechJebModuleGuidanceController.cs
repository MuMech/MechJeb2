/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System.Collections.Generic;
using MechJebLib.PVG;
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

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble UllageLeadTime = 20;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool ShouldDrawTrajectory = true;

        private MechJebModuleAscentSettings _ascentSettings => core.ascentSettings;
        
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
                core.AddToPostDrawQueue(DrawTrajetory);
            }
        }

        public override void OnModuleEnabled()
        {
            Status = PVGStatus.ENABLED;
            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
            core.spinup.users.Add(this);
            Solution        = null;
            _allowExecution = false;
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            if (!core.rssMode)
                core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            core.staging.users.Remove(this);
            core.spinup.users.Remove(this);
            Solution = null;
            Status   = PVGStatus.FINISHED;
        }

        private bool _allowExecution;

        // we wait until we get a signal to allow execution to start
        public void AssertStart(bool allow_execution = true)
        {
            _allowExecution = allow_execution;
        }

        public override void OnFixedUpdate()
        {
            UpdatePitchAndHeading();

            if (!HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("MechJebModuleGuidanceController [BUG]: PVG enabled in non-flight mode.  How does this happen?");
                Done();
            }

            if (!enabled || Status == PVGStatus.ENABLED)
                return;

            if (Status == PVGStatus.FINISHED)
            {
                Done();
                return;
            }

            HandleTerminal();

            HandleSpinup();

            HandleThrottle();

            DrawTrajetory();
        }

        private bool WillDoRCSButNotYet()
        {
            // We might have wonky transforms and have a tiny bit of fore RCS, so require at least 10% of the max RCS thrust to be
            // in the pointy direction (which should be "up" / y-axis per KSP/Unity semantics).
            bool hasRCS = vessel.hasEnabledRCSModules() &&
                          vesselState.rcsThrustAvailable.up > 0.1 * vesselState.rcsThrustAvailable.MaxMagnitude();

            return hasRCS && Status != PVGStatus.TERMINAL_RCS;
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
            if (vessel.currentStage < _ascentSettings.LastStage)
            {
                Done();
                return;
            }

            // this handles termination of thrust for final stages of "fixed" burntime rockets (due to residuals Tgo may go less than zero so we
            // wait for natural termination of thrust).   no support for RCS terminal trim.
            if (_ascentSettings.OptimizeStage < 0 && vessel.currentStage <= _ascentSettings.LastStage && Solution.Tgo(vesselState.time) <= 0 &&
                vesselState.thrustAvailable == 0)
            {
                Done();
                return;
            }

            // TERMINAL_STAGING is set on a non-upper stage optimized stage, once we are no longer in the optimized stage
            // then we have staged, so we need to reset that condition.  Otherwise we need to wait for staging.
            if (Status == PVGStatus.TERMINAL_STAGING)
            {
                if (vessel.currentStage == _ascentSettings.OptimizeStage)
                    return;
                
                Status = PVGStatus.BURNING;
            }
            
            // We should either be in an non-upper stage optimized stage, or we should be within 10 seconds of the whole
            // burntime in order to enter terminal guidance.
            if (vessel.currentStage != _ascentSettings.OptimizeStage && Solution.Tgo(vesselState.time) > 10)
                return;
            
            int solutionIndex = Solution.IndexForKSPStage(vessel.currentStage);
            if (solutionIndex < 0)
            {
                Done();
                return;
            }

            // Only enter terminal guidance within 10 seconds of the current stage
            if (Solution.Tgo(vesselState.time, solutionIndex) > 10)
                return;

            if (Status != PVGStatus.TERMINAL_RCS)
                Status = PVGStatus.TERMINAL;

            core.warp.MinimumWarp();

            if (Status == PVGStatus.TERMINAL_RCS && !vessel.ActionGroups[KSPActionGroup.RCS]) // if someone manually disables RCS
            {
                Debug.Log("[MechJebModuleGuidanceController] terminating guidance due to manual deactivation of RCS.");
                TerminalDone();
                return;
            }

            // stopping one tick short is more accurate for rockets without RCS, but sometimes we overshoot with only one tick
            int ticks = WillDoRCSButNotYet() ? 2 : 1;

            // bit of a hack to predict velocity + position in the next tick or two
            // FIXME: what exactly does KSP do to integrate over timesteps?
            Vector3d a0 = vessel.acceleration_immediate;

            double dt = ticks * TimeWarp.fixedDeltaTime;
            Vector3d v1 = vesselState.orbitalVelocity + a0 * dt;
            Vector3d x1 = vesselState.orbitalPosition + vesselState.orbitalVelocity * dt + 0.5 * a0 * dt * dt;

            if (Solution.TerminalGuidanceSatisfied(x1.WorldToV3(), v1.WorldToV3(), solutionIndex))
            {
                if (WillDoRCSButNotYet())
                {
                    Debug.Log("[MechJebModuleGuidanceController] transition to RCS terminal guidance.");
                    Status = PVGStatus.TERMINAL_RCS;
                    if (!vessel.ActionGroups[KSPActionGroup.RCS])
                        vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
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
            if (vessel.currentStage != _ascentSettings.SpinupStage)
                return;

            core.spinup.AssertStart();
            core.spinup.RollAngularVelocity = _ascentSettings.SpinupAngularVelocity;
        }

        public bool IsTerminal()
        {
            return Status == PVGStatus.TERMINAL_RCS || Status == PVGStatus.TERMINAL_STAGING || Status == PVGStatus.TERMINAL;
        }

        /* is guidance usable? */
        public bool IsStable()
        {
            return IsNormal() || IsTerminal();
        }

        // either ENABLED and waiting for a Solution, or executing a solution "normally" (not terminal, not failed)
        public bool IsReady()
        {
            return Status == PVGStatus.ENABLED || IsNormal();
        }

        // not TERMINAL guidance or TERMINAL_RCS -- when we should be running the optimizer
        public bool IsNormal()
        {
            return Status == PVGStatus.INITIALIZED || Status == PVGStatus.BURNING || Status == PVGStatus.COASTING;
        }

        public bool IsCoasting()
        {
            return Status == PVGStatus.COASTING;
        }

        private bool IsThrustOn()
        {
            return IsBurning() || IsTerminal();
        }

        private bool IsBurning()
        {
            return Status == PVGStatus.BURNING;
        }

        /* normal pre-states but not usefully converged */
        public bool IsInitializing()
        {
            return Status == PVGStatus.ENABLED || Status == PVGStatus.INITIALIZED;
        }

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

            int coastStage = Solution.CoastStage();
            
            Debug.Log($"coast stage: {coastStage} current stage: {vessel.currentStage} will coast: {Solution.WillCoast(vesselState.time)}");

            if (coastStage >= 0 && vessel.currentStage == coastStage && Solution.WillCoast(vesselState.time))
                core.staging.autostageLimitInternal = coastStage;
            else
                core.staging.autostageLimitInternal = 0;

            if (Solution.Coast(vesselState.time))
            {
                if (!IsCoasting())
                {
                    StartCoast = vesselState.time;
                    // force RCS on at the state transition
                    if (!vessel.ActionGroups[KSPActionGroup.RCS])
                        vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                }

                Status = PVGStatus.COASTING;

                if (Solution.StageTimeLeft(vesselState.time) < UllageLeadTime)
                    RCSOn();
                
                ThrustOff();
                return;
            }

            ThrottleOn();

            Status = PVGStatus.BURNING;
        }

        private bool IsGrounded()
        {
            return vessel.situation == Vessel.Situations.LANDED ||
                   vessel.situation == Vessel.Situations.PRELAUNCH ||
                   vessel.situation == Vessel.Situations.SPLASHED;
        }

        private void UpdatePitchAndHeading()
        {
            if (Solution == null)
                return;

            // if we're not flying yet, continuously update the t0 of the solution
            if (IsGrounded())
                Solution.T0 = vesselState.time;

            if (Status != PVGStatus.TERMINAL_RCS)
            {
                (double pitch, double heading) = Solution.PitchAndHeading(vesselState.time);
                Pitch                          = Rad2Deg(pitch);
                Heading                        = Rad2Deg(heading);
                Tgo                            = Solution.Tgo(vesselState.time);
                VGO                            = Solution.Vgo(vesselState.time);
            }
            /* else leave pitch and heading at the last values, also stop updating vgo/tgo */
        }

        private readonly List<Vector3d> _trajectory = new List<Vector3d>();
        private readonly Orbit          _finalOrbit = new Orbit();

        private void DrawTrajetory()
        {
            if (Solution == null)
                return;

            if (!enabled)
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

                _trajectory.Add(Solution.R(t).V3ToWorld() + mainBody.position);
            }

            GLUtils.DrawPath(mainBody, _trajectory, Color.red, MapView.MapIsEnabled);

            Vector3d rf = Planetarium.fetch.rotation * Solution.R(Solution.Tf).ToVector3d().xzy;
            Vector3d vf = Planetarium.fetch.rotation * Solution.V(Solution.Tf).ToVector3d().xzy;

            _finalOrbit.UpdateFromStateVectors(rf.xzy, vf.xzy, mainBody, Solution.Tf);

            GLUtils.DrawOrbit(_finalOrbit, Color.yellow);
        }

        private void ThrottleOn()
        {
            core.thrust.targetThrottle = 1.0F;
        }

        private void RCSOn()
        {
            core.thrust.ThrustOff();
            vessel.ctrlState.Z = -1.0F;
        }

        private void ThrustOff()
        {
            core.thrust.ThrustOff();
        }

        private void TerminalDone()
        {
            if (Solution == null)
            {
                Done();
                return;
            }

            // if we still have a coast to do in this stage, start the coast
            if (vessel.currentStage == Solution.CoastStage() && Solution.WillCoast(vesselState.time))
            {
                ThrustOff();
                Status = PVGStatus.COASTING;
                return;
            }
            
            // if we have more un-optimized upper stages to burn, stage and use the TERMINAL_STAGING state
            if (Solution.TerminalStage() != vessel.currentStage)
            {
                core.staging.Stage();
                Status = PVGStatus.TERMINAL_STAGING;
                return;
            }
  
            // otherwise we just have normal termination
            Done();
        }

        private void Done()
        {
            users.Clear();
            ThrustOff();
            Status   = PVGStatus.FINISHED;
            Solution = null;
            enabled  = false;
        }

        public void SetSolution(Solution solution)
        {
            Solution = solution;
            if (Status == PVGStatus.ENABLED)
                Status = PVGStatus.INITIALIZED;
        }
    }
}
