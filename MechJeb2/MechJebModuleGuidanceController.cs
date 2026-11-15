/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

extern alias JetBrainsAnnotations;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Utils;
using MechJebLibBindings;
using UnityEngine;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MuMech
{
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
        public double CurrentPhaseTgo;

        // the decisions: which part of the Solution is flown, when burns end, when to coast, stage and finish
        private readonly GuidanceStateMachine _machine = new GuidanceStateMachine();

        // the universal time the last coast started
        public double StartCoast => _machine.StartCoast;

        // a coast has been started since guidance was engaged
        public bool hasCoasted => _machine.HasCoasted;

        // guidance's private copy (the solver warm starts from its own), main thread only.  Its clock is MET (VesselState.MET).
        public Solution? Solution => _machine.Solution;

        public PSGStatus Status => _machine.Status;

        // what guidance does with RCS (PSGFeedback), said once at each change
        private ScreenMessage _rcsCoastMessage = null!, _rcsTrimMessage = null!, _rcsOffMessage = null!;

        // what the vessel flew since guidance was engaged, for the PSG Solution window
        public readonly PSGFlownTrack FlownTrack = new PSGFlownTrack();

        // the last Solution computed on the ground (it starts at MET zero), the one the vessel lifted off with, kept after
        // Solution moves on, for the PSG Solution window
        public Solution? PadSolution;

        public override void OnStart(PartModule.StartState state)
        {
            _rcsCoastMessage = PSGFeedback.RcsCoastMessage();
            _rcsTrimMessage  = PSGFeedback.RcsTrimMessage();
            _rcsOffMessage   = PSGFeedback.RcsOffMessage();

            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
            {
                Core.AddToPostDrawQueue(DrawTrajectory);
            }
        }

        protected override void OnModuleEnabled()
        {
            Debug.Log("MechJebModuleGuidanceController: Enabled");
            ResetToAwaitingSolution();
            FlownTrack.Clear();
            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
            Core.Spinup.Users.Add(this);
        }

        // The state of an enabled controller with nothing to fly yet. UpdatePitchAndHeading doesn't run without a Solution,
        // so the values it publishes have to be cleared here or the UI and kOS would keep showing the old ones.
        private void ResetToAwaitingSolution()
        {
            _machine.Reset();
            PadSolution = null;
            _allowExecution = false;
            Tgo = Vgo = CurrentPhaseTgo = 0;
            Pitch = Heading = 0;
            _inertial = V3.zero;
            Inertial = Vector3d.zero;
            _throttle = 0;
        }

        protected override void OnModuleDisabled()
        {
            Debug.Log("MechJebModuleGuidanceController: Disabled");
            Core.Attitude.AttitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.AutoStageLimitRemove(this);
            Core.Spinup.Users.Remove(this);
            _machine.Finish();
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

            if (Solution != null && !IsGrounded())
                FlownTrack.Record(Core);

            // RO/RF: if we are spooling up engines, then flush the PID integrators to prevent windup
            if (Status == PSGStatus.BURNING || Status == PSGStatus.TERMINAL)
                if (VesselState.ThrustCurrent < VesselState.ThrustMinimum * 0.98)
                    Core.Attitude.Controller.Reset();

            bool wasCoasting = _machine.IsCoasting();

            GuidanceCommand cmd = _machine.Step(GatherInputs());

            if (cmd.Finished)
            {
                Done();
                return;
            }

            HandleSpinup();

            Apply(cmd);

            // a solve started before the coast plans the burn before it, and one started during the coast still has it ahead:
            // neither must arrive once the coast has started or ended
            if (!wasCoasting && _machine.IsCoasting())
                Core.PSGSimulation.CoastStarted();
            else if (wasCoasting && !_machine.IsCoasting())
                Core.PSGSimulation.CoastEnded();
        }

        private GuidanceInputs GatherInputs() =>
            new GuidanceInputs
            {
                Met             = VesselState.MET,
                Time            = VesselState.Time,
                Dt              = TimeWarp.fixedDeltaTime,
                KSPStage        = Vessel.currentStage,
                Grounded        = IsGrounded(),
                AllowExecution  = _allowExecution,
                Mass            = VesselState.Mass,
                ThrustAvailable = VesselState.ThrustAvailable,
                RCSThrustUp     = VesselState.RCSThrustAvailable.Up,
                ForwardRCS      = HasForwardRCS(),
                RCSGroup        = Vessel.ActionGroups[KSPActionGroup.RCS],
                StageDeltaV     = CurrentStageDeltaV(),
                R               = VesselState.OrbitalPosition.WorldToV3Rotated(),
                V               = VesselState.OrbitalVelocity.WorldToV3Rotated(),
                A               = Vessel.acceleration_immediate.WorldToV3Rotated(),
                UllageLeadTime  = UllageLeadTime,
                CoastDuring     = Core.PSGSimulation.PlannedCoast().Kind == StageType.COAST_DURING
            };

        private void Apply(in GuidanceCommand cmd)
        {
            if ((cmd.Messages & GuidanceMessages.RCS_OFF) != 0)
                ScreenMessages.PostScreenMessage(_rcsOffMessage);
            if ((cmd.Messages & GuidanceMessages.RCS_TRIM) != 0)
                ScreenMessages.PostScreenMessage(_rcsTrimMessage);
            if ((cmd.Messages & GuidanceMessages.RCS_COAST) != 0)
                ScreenMessages.PostScreenMessage(_rcsCoastMessage);

            if (cmd.RCSGroupOn && !Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            if (cmd.MinimumWarp)
                Core.Warp.MinimumWarp();

            if (cmd.AutoStageLimit >= 0)
                Core.Staging.AutoStageLimitRequest(cmd.AutoStageLimit, this);

            switch (cmd.Throttle)
            {
                case GuidanceThrottle.SOLUTION:
                    ThrottleOn();
                    break;
                case GuidanceThrottle.OFF:
                    ThrustOff();
                    break;
                case GuidanceThrottle.RCS_FORWARD:
                    RCSOn();
                    break;
            }

            switch (cmd.Staging)
            {
                case GuidanceStaging.STAGE:
                    Core.Staging.Stage();
                    break;
                case GuidanceStaging.IMMEDIATE:
                    Core.Staging.ImmediateStage();
                    break;
            }

            if (cmd.MarkCoastDone)
                Core.PSGSimulation.MarkCoastDone();
        }

        // at least 10 % of the RCS thrust pushes the vessel forward (the RCS group off counts too)
        private bool HasForwardRCS() =>
            Vessel.hasEnabledRCSModules() && VesselState.RCSThrustAvailable.Up > 0.1 * VesselState.RCSThrustAvailable.MaxMagnitude();

        // the delta-v left in the current stage by the stage stats, NaN while they have nothing for it
        private double CurrentStageDeltaV()
        {
            List<FuelStats> vac = Core.StageStats.VacStats;
            bool found = false;
            double dv = 0;
            for (int i = 0; i < vac.Count; i++)
                if (vac[i].KSPStage == Vessel.currentStage)
                {
                    found = true;
                    dv += vac[i].DeltaV;
                }

            return found ? dv : double.NaN;
        }

        // the plan's spin-up rides on its coast: the vessel spins up while it coasts, and staging waits for the spin
        private void HandleSpinup()
        {
            if (!IsCoasting() || Vessel.currentStage != Core.PSGSimulation.PlannedSpinupStage())
                return;

            Core.Spinup.AssertStart();
            Core.Spinup.RollAngularVelocity = _ascentSettings.SpinupAngularVelocity;
        }

        public bool IsTerminal() => _machine.IsTerminal();

        /* is guidance usable? */
        public bool IsStable() => IsNormal() || IsTerminal();

        // either ENABLED and waiting for a Solution, or executing a solution "normally" (not terminal, not failed)
        public bool IsReady() => Status == PSGStatus.ENABLED || IsNormal();

        // not TERMINAL guidance or TERMINAL_RCS -- when we should be running the optimizer
        public bool IsNormal() => _machine.IsNormal();

        public bool IsCoasting() => _machine.IsCoasting();

        /* normal pre-states but not usefully converged */
        public bool IsInitializing() => Status == PSGStatus.ENABLED || Status == PSGStatus.INITIALIZED;

        private bool IsGrounded() =>
            Vessel.situation == Vessel.Situations.LANDED ||
            Vessel.situation == Vessel.Situations.PRELAUNCH ||
            Vessel.situation == Vessel.Situations.SPLASHED;

        private void UpdatePitchAndHeading()
        {
            if (Solution == null)
                return;

            V3 r0 = VesselState.OrbitalPosition.WorldToV3Rotated();

            // the launch site turns with the planet while we wait on the ground, so turn the Solution with it (only a Solution
            // computed on the ground is turned)
            Solution.RotateToLaunchSite(r0);

            // the Solution's clock is MET, which stays at zero until liftoff, so on the ground this flies its first point
            int idx = Solution.IndexForKSPStage(Vessel.currentStage, Core.Guidance.IsCoasting());

            if (!_machine.IsRCSTrim()) // don't update vgo/tgo if we're in RCS
            {
                Tgo = Solution.Tgo(VesselState.MET);
                Vgo = Solution.Vgo(VesselState.MET);
                if (idx >= 0)
                    CurrentPhaseTgo = Solution.Tgo(VesselState.MET, idx);
            }

            // lock the inertial heading at tgo < 2.0 for any staging event or terminal burnout
            // (2 seconds is to hopefully allow for variance due to residuals, it may be less)
            if (IsGrounded() || (idx >= 0 && Solution.Tgo(VesselState.MET, idx) > 2.0))
                (_inertial, _throttle) = Solution.InertialGuidance(VesselState.MET, VesselState.MinThrottle);

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

            // Tf is an MET, the orbit wants the UT of the end of the trajectory
            _finalOrbit.UpdateFromStateVectors(rf.xzy, vf.xzy, MainBody, VesselState.Time + Solution.Tgo(VesselState.MET));

            GLUtils.DrawOrbit(_finalOrbit, Color.yellow);
        }

        private void ThrottleOn() => Core.Thrust.TargetThrottle = _throttle > 0 ? (float)_throttle : 1.0f;

        private void RCSOn()
        {
            Core.Thrust.ThrustOff();
            Vessel.ctrlState.Z = -1.0F;
        }

        private void ThrustOff() => Core.Thrust.ThrustOff();

        private void Done()
        {
            Users.Clear();
            ThrustOff();
            _machine.Finish();
            Enabled = false;
        }

        // guidance flies in SI units and MET seconds, so this takes a DeNormalized() Solution
        public void SetSolution(Solution solution)
        {
            Check.False(solution.Normalized, "guidance needs a DeNormalized() Solution");

            // PSGSimulation cancels the solve in progress when the coast starts and when it ends (CoastStarted, CoastEnded), so a
            // solution that arrives during the coast starts with it, and one that arrives after it has no coast
            CheckDebug.True(!IsCoasting() || solution.CoastPhase(0));
            CheckDebug.True(!hasCoasted || IsCoasting() || solution.CoastKSPStage() < 0);

            _machine.SetSolution(solution);
            if (solution.T0 <= 0)
                PadSolution = solution;
        }

        /// <summary>
        ///     Drops a Solution that no longer applies and goes back to waiting for one. Only for use on the ground, the timed launch
        ///     buttons and kOS suffixes require a landed vessel. Does nothing while disabled, so a countdown started before engaging
        ///     keeps the FINISHED status and leaves the throttle alone.
        /// </summary>
        public void FlushSolution()
        {
            if (!Enabled)
                return;

            ResetToAwaitingSolution();

            // engaging without a countdown leaves us BURNING with the throttle up on the pad, and once the autopilot has left
            // prelaunch nothing else lowers it before T-0
            ThrustOff();
        }
    }
}
