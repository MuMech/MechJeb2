/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using KSP.Localization;
using MechJebLib.PSG;
using MechJebLibBindings;
using UnityEngine;

#nullable enable

namespace MuMech
{
    /// <summary>
    ///     Autopilot for PSG Launches: the KSP side of AscentStateMachine (MechJebLib), which flies the Solution the PSGSimulation
    ///     manager gives it from the PSG Optimizer.  Each physics tick this gathers the vessel's state into AscentInputs, steps
    ///     the state machine, and applies the AscentCommand it returns: the attitude, throttle, staging and the rest, and what
    ///     the PSGSimulation is told.  The status line, the countdown and the trajectory drawing are here too.
    /// </summary>
    public class MechJebModulePSGAutopilot : ComputerModule, IAscentAutopilot
    {
        public MechJebModulePSGAutopilot(MechJebCore core) : base(core) { }

        // the settings below were saved under the guidance controller this module replaced
        public override string FormerName => "MechJebModuleGuidanceController";

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble UllageLeadTime = 20;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool ShouldDrawTrajectory = true;

        public AscentStatus Status { get; } = new AscentStatus();

        UserPool IAscentAutopilot.Users => Users;

        private MechJebModuleAscentSettings _ascentSettings => Core.AscentSettings;

        private MechJebModuleLaunchCountdown _countdown => Core.LaunchCountdown;

        // the decisions: where the vessel is in the Solution, the state, and what to do each tick
        private readonly AscentStateMachine _machine = new AscentStateMachine();

        // what guidance commands, for the UI and kOS
        public double Pitch           => _machine.Pitch;
        public double Heading         => _machine.Heading;
        public double Tgo             => _machine.Tgo;
        public double Vgo             => _machine.Vgo;
        public double CurrentPhaseTgo => _machine.CurrentPhaseTgo;

        // The state machine owns this and can change the FlightPlan to reconcile stages.
        public Solution? Solution => _machine.Solution;

        // The last ground solution on the pad, for the PSG Solution window
        public Solution? PadSolution;

        // what the vessel flew since the autopilot was engaged, for the PSG Solution window
        public readonly PSGFlownTrack FlownTrack = new PSGFlownTrack();

        public PSGState State => _machine.State;

        /// <summary>kOS GUIDANCESTATUS: see AscentStateMachine.GuidanceStatus.</summary>
        public string GuidanceStatus => _machine.GuidanceStatus;

        public bool HasSolution => Solution != null;

        public bool IsCoasting => _machine.IsCoasting;

        public bool IsTerminal => _machine.IsTerminal;

        public bool IsFinished => _machine.IsFinished;

        /// <summary>Set when the coast starts, and kept after it: the Solution's burns before the coast are behind the vessel.</summary>
        public bool HasCoasted => _machine.HasCoasted;

        /// <summary>The UT the coast started.</summary>
        public double CoastStartTime => _machine.CoastStartTime;

        // what guidance does with RCS (PSGFeedback), said once at each change
        private ScreenMessage _rcsCoastMessage = null!, _rcsTrimMessage = null!, _rcsOffMessage = null!;

        // the countdown holds the throttle
        private bool _allowThrottle;

        // Countdown.DrivePrelaunch is done
        private bool _prelaunchDone;

        // the persistentIds of the vessel's parts (AscentInputs.PartIds), refreshed when its part count changes
        private readonly HashSet<uint> _partIds = new HashSet<uint>();
        private Vessel? _partIdsVessel;
        private int _partIdsCount = -1;

        private string? _awaitingComputing, _awaitingProgress, _awaitingProgressFrom;
        private string? _lockStatus, _lockStatusId;

        public override void OnStart(PartModule.StartState state)
        {
            _rcsCoastMessage = PSGFeedback.RcsCoastMessage();
            _rcsTrimMessage  = PSGFeedback.RcsTrimMessage();
            _rcsOffMessage   = PSGFeedback.RcsOffMessage();

            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
                Core.AddToPostDrawQueue(DrawTrajectory);
        }

        protected override void OnModuleEnabled()
        {
            Debug.Log("[MechJebModulePSGAutopilot] enabled");

            _ascentSettings.ApplyOwnValues(this, true);

            // Each engage plans from the current settings and stages: the flight plan is made again, so the plan of an earlier
            // engage, with the stages it marked done, is not flown again. The simulation, which freezes the plan, is disabled here.
            Core.PSGSimulation.FlightPlan = null;

            ResetGuidance();
            FlownTrack.Clear();

            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
            Core.Spinup.Users.Add(this);
            if (_ascentSettings.Autostage) Core.Staging.Users.Add(this);
            _countdown.Users.Add(this);

            Status.Set(Localizer.Format("#MechJeb_Ascent_status1")); //"Pre Launch"

            _machine.Start();
        }

        protected override void OnModuleDisabled()
        {
            Debug.Log("[MechJebModulePSGAutopilot] disabled");

            Core.Attitude.AttitudeDeactivate();
            if (!Core.RssMode)
                Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Staging.Users.Remove(this);
            Core.Staging.AutoStageLimitRemove(this);
            Core.Spinup.Users.Remove(this);
            _countdown.Users.Remove(this);
            _ascentSettings.ReleaseOwnValues(this);

            _countdown.Abort();

            Core.PSGSimulation.Users.Remove(this);

            _machine.Disengage();
            _rcsForward = false;

            Status.Set(Localizer.Format("#MechJeb_Ascent_status2")); //"Off"
        }

        // The state of an enabled autopilot with nothing to fly yet (AscentStateMachine.ResetGuidance), and the countdown's.
        private void ResetGuidance()
        {
            _machine.ResetGuidance();
            PadSolution = null;
            _allowThrottle = false;
            _prelaunchDone = false;
        }

        public override void OnFixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
            {
                Debug.Log("[MechJebModulePSGAutopilot] [BUG]: PSG enabled in non-flight mode.  How does this happen?");
                ApplyTransition(_machine.Finish());
                return;
            }

            UpdateFlightPlan();

            UpdateCountdownGate();

            AscentCommand cmd = _machine.Step(GatherInputs());

            Apply(cmd);

            if (Solution != null && !IsGrounded())
                FlownTrack.Record(Core);

            // the steering doesn't reach the vessel while another mod has taken MechJeb's controls (DeactivateControl: RP-1 sets
            // it when the avionics on board can't support the vessel, RemoteTech without a connection)
            if (_machine.IsGuided && Core.DeactivateControl)
                SetLockStatus();
        }

        // the guidance needs a flight plan, which nothing else makes unless the Ascent window is open
        private void UpdateFlightPlan()
        {
            if (Core.PSGSimulation.FlightPlan != null)
                return;

            Core.StageStats.RequestUpdate();
            Core.PSGSimulation.UpdateLogicalPlan();
        }

        // the optimizer starts late in a countdown (or at once without one), and the throttle waits for the countdown to end
        private void UpdateCountdownGate()
        {
            if (_countdown.Active)
            {
                if (_countdown.TMinus <= Math.Max(_ascentSettings.WarpCountDown, 0))
                    Core.PSGSimulation.Users.Add(this);
                else
                    Core.PSGSimulation.Users.Remove(this);
                _allowThrottle = false;
            }
            else
            {
                Core.PSGSimulation.Users.Add(this);
                _allowThrottle = true;
            }
        }

        /*
         * The state machine's inputs and commands
         */

        private AscentInputs GatherInputs()
        {
            UpdatePartIds();

            return new AscentInputs
            {
                Met              = VesselState.MET,
                Time             = VesselState.Time,
                Dt               = TimeWarp.fixedDeltaTime,
                CurrentStage     = Vessel.currentStage,
                Grounded         = IsGrounded(),
                LiftedOff        = LiftedOff(),
                AltitudeBottom   = VesselState.AltitudeBottom,
                HasAtmosphere    = MainBody.atmosphere,
                Mass             = VesselState.Mass,
                ThrustAvailable  = VesselState.ThrustAvailable,
                MinThrottle      = VesselState.MinThrottle,
                RCSThrustUp      = VesselState.RCSThrustAvailable.Up,
                ForwardRCS       = HasForwardRCS(),
                RCSGroup         = Vessel.ActionGroups[KSPActionGroup.RCS],
                R                = VesselState.OrbitalPosition.WorldToV3Rotated(),
                V                = VesselState.OrbitalVelocity.WorldToV3Rotated(),
                A                = Vessel.acceleration_immediate.WorldToV3Rotated(),
                VacStats         = Core.StageStats.VacStats,
                AtmoStats        = Core.StageStats.AtmoStats,
                VacT             = Core.StageStats.VacT,
                PartIds          = _partIds,
                PrelaunchDone    = _prelaunchDone,
                AllowThrottle    = _allowThrottle,
                PitchStartHeight = _ascentSettings.PitchStartHeight,
                PitchRate        = _ascentSettings.PitchRate,
                UllageLeadTime   = UllageLeadTime,
                SpinupStage      = Core.PSGSimulation.PlannedSpinupStage()
            };
        }

        // parts only leave the vessel in an ascent, so a part count that hasn't changed is the same parts
        private void UpdatePartIds()
        {
            if (Vessel == _partIdsVessel && Vessel.parts.Count == _partIdsCount)
                return;

            _partIdsVessel = Vessel;
            _partIdsCount  = Vessel.parts.Count;

            _partIds.Clear();
            foreach (Part part in Vessel.parts)
                _partIds.Add(part.persistentId);
        }

        // the transitions' part of the command, then (unless the countdown holds for liftoff) the tick's
        private void Apply(in AscentCommand cmd)
        {
            // the forward RCS is asked for again each tick it is wanted (ApplyThrottle)
            _rcsForward = false;

            ApplyTransition(cmd);

            _ascentSettings.LimitingAoA = false;

            if (cmd.CheckLiftoffHold && _countdown.HoldForLiftoff(Status))
                return;

            if (cmd.DriveAscentParts)
            {
                Core.Solarpanel.DriveForAscent();
                Core.AntennaControl.DriveForAscent();
            }

            if (cmd.AutostageLimit >= 0)
                Core.Staging.AutoStageLimitRequest(cmd.AutostageLimit, this);

            if (cmd.DrivePrelaunch)
                _prelaunchDone = _countdown.DrivePrelaunch(Status);

            if (cmd.MinimumWarp)
                Core.Warp.MinimumWarp();

            ApplyAttitude(cmd);

            ApplyThrottle(cmd);

            UpdateStatus();

            switch (cmd.Staging)
            {
                case AscentStaging.STAGE:
                    Core.Staging.Stage();
                    break;
                case AscentStaging.IMMEDIATE:
                    Core.Staging.ImmediateStage();
                    break;
            }

            if (cmd.Spinup)
            {
                Core.Spinup.AssertStart();
                Core.Spinup.RollAngularVelocity = _ascentSettings.SpinupAngularVelocity;
            }
        }

        private void ApplyTransition(in AscentCommand cmd)
        {
            // a solve started during the coast still has it ahead, which would start it again
            if ((cmd.Events & AscentEvents.COAST_ENDED) != 0)
            {
                Core.PSGSimulation.CoastEnded();
                Core.PSGSimulation.MarkCoastDone();
            }

            // a solve started before the coast plans the burn before it, so it must never arrive now
            if ((cmd.Events & AscentEvents.COAST_STARTED) != 0)
            {
                Core.PSGSimulation.CoastStarted();
                ThrustOff();
            }

            // a solve in progress would move the target that terminal guidance flies to
            if ((cmd.Events & AscentEvents.TERMINAL_STARTED) != 0)
                Core.PSGSimulation.TerminalStarted();

            if ((cmd.Messages & AscentMessages.RCS_COAST) != 0)
                ScreenMessages.PostScreenMessage(_rcsCoastMessage);
            if ((cmd.Messages & AscentMessages.RCS_TRIM) != 0)
                ScreenMessages.PostScreenMessage(_rcsTrimMessage);
            if ((cmd.Messages & AscentMessages.RCS_OFF) != 0)
                ScreenMessages.PostScreenMessage(_rcsOffMessage);

            if (cmd.RCSGroupOn && !Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            // a solve in progress is of no use now
            if ((cmd.Events & AscentEvents.FINISHED) != 0)
            {
                Core.PSGSimulation.GuidanceFinished();
                ThrustOff();
                Users.Clear(); // no user is left to keep it engaged
            }
        }

        private void ApplyAttitude(in AscentCommand cmd)
        {
            switch (cmd.Attitude)
            {
                case AscentAttitude.VERTICAL:
                    //during the vertical ascent we just thrust straight up at max throttle
                    Core.Attitude.AttitudeToVerticalLaunch(cmd.Heading,
                        _ascentSettings.ForceRoll && VesselState.AltitudeBottom > _ascentSettings.RollAltitude, _ascentSettings.VerticalRoll,
                        this);
                    break;
                case AscentAttitude.PITCH_HEADING:
                    Core.Attitude.LimitedAttitudeTo(cmd.Heading, cmd.Pitch, _ascentSettings.ForceRoll, _ascentSettings.TurnRoll, ApplyAoALimiter,
                        this);
                    break;
                case AscentAttitude.INERTIAL:
                    Vector3d inertial = cmd.Direction.V3ToWorldRotated();
                    double ang = Vector3d.Angle(inertial, VesselState.Forward);
                    AscentStatusLevel severity = ang > 5 ? AscentStatusLevel.ERROR : ang > 1 ? AscentStatusLevel.WARNING : AscentStatusLevel.NORMAL;
                    Status.Set($"Stable Guidance: {ang:F}° deviation", severity);
                    Core.Attitude.LimitedAttitudeTo(inertial, _ascentSettings.ForceRoll, _ascentSettings.TurnRoll, ApplyAoALimiter, this);
                    break;
            }
        }

        private void ApplyThrottle(in AscentCommand cmd)
        {
            switch (cmd.Throttle)
            {
                case AscentThrottle.GUIDED:
                    // RO/RF: if we are spooling up engines, then flush the PID integrators to prevent windup
                    if (VesselState.ThrustCurrent < VesselState.ThrustMinimum * 0.98)
                        Core.Attitude.Controller.Reset();

                    Core.Thrust.TargetThrottle = (float)cmd.ThrottleValue;
                    break;
                case AscentThrottle.OFF:
                    ThrustOff();
                    break;
                case AscentThrottle.RCS_FORWARD:
                    RCSOn();
                    break;
            }
        }

        // the status line of the states that say more than the guidance deviation (ApplyAttitude)
        private void UpdateStatus()
        {
            switch (State)
            {
                case PSGState.VERTICAL_ASCENT:
                    if (!LiftedOff())
                    {
                        // the first solve can take minutes on a slow machine: say so, or the pad looks stuck
                        if (Solution == null)
                            Status.Set(AwaitingComputing()); //"Awaiting liftoff: computing the ascent…"
                        else
                            Status.Set(Localizer.Format("#MechJeb_Ascent_status12")); //"Awaiting liftoff"
                        break;
                    }

                    double dh = _ascentSettings.PitchStartHeight - VesselState.AltitudeBottom;
                    Status.Set($"Vertical ascent {dh:F2}m to go");
                    break;
                case PSGState.PITCH_PROGRAM:
                    double pitch = _machine.PitchProgramPitch();

                    // without a solution the guidance pitch is 0, so "N° to guidance" would be the pitch itself: say what is missing
                    // instead, since the pitch program goes on down to the horizon
                    if (Solution == null)
                        Status.Set($"No guidance yet, pitch program at {pitch:F0}°. " +
                            PSGFeedback.OptimizerStateText(Core.PSGSimulation.State, Core.PSGSimulation.StateSeconds, true, false,
                                Core.PSGSimulation.LastFailureMessage != null ? Core.PSGSimulation.Staleness : 0),
                            AscentStatusLevel.WARNING);
                    else
                        Status.Set(Localizer.Format("#MechJeb_Ascent_status15", $"{pitch - Pitch:F}")); //Pitch program <<1>>° to guidance
                    break;
                case PSGState.JETTISON:
                    if (Solution != null)
                        Status.Set($"Jettisoning stage {_machine.JettisonStage()}");
                    break;
            }
        }

        // We might have wonky transforms and have a tiny bit of fore RCS, so require at least 10% of the max RCS thrust to be in the
        // pointy direction (which should be "up" / y-axis per KSP/Unity semantics). VesselState counts the RCS thrust with the group
        // off too: an ascent flown with RCS off ends without the RCS trim, as it always has.
        private bool HasForwardRCS() =>
            Vessel.hasEnabledRCSModules() && VesselState.RCSThrustAvailable.Up > 0.1 * VesselState.RCSThrustAvailable.MaxMagnitude();

        // the same test as PSGSimulation's, so guidance and the optimizer agree about the pad
        private bool IsGrounded() => !Vessel.VesselOffGround();

        private bool LiftedOff() => Vessel.LiftedOff() && !Vessel.Landed;

        // Set by RCSOn for this tick: Drive writes the forward RCS command into the fly-by-wire state.  Writing Vessel.ctrlState
        // from OnFixedUpdate isn't enough: FlightInputHandler copies the player's input over it in its own FixedUpdate, so
        // depending on the script order the coast's ullage and the TERMINAL_RCS trim would not fire.
        private bool _rcsForward;

        private void RCSOn()
        {
            Core.Thrust.ThrustOff();
            _rcsForward = true;
        }

        public override void Drive(FlightCtrlState s)
        {
            if (_rcsForward)
                s.Z = -1.0F;
        }

        private void ThrustOff() => Core.Thrust.ThrustOff();

        private Vector3d ApplyAoALimiter(Vector3d desiredThrustVector)
        {
            double lim = Core.PSGSimulation.QAlphaLimit;
            if (lim <= 0)
                return desiredThrustVector;

            _ascentSettings.LimitingAoA = VesselState.DynamicPressure * Vector3.Angle(VesselState.SurfaceVelocity, desiredThrustVector) * UtilMath.Deg2Rad > lim;
            if (_ascentSettings.LimitingAoA)
            {
                _ascentSettings.CurrentMaxAoA = lim / VesselState.DynamicPressure * UtilMath.Rad2Deg;
                desiredThrustVector = MathExtensions.RotateTowards(VesselState.SurfaceVelocity, desiredThrustVector, (float)(_ascentSettings.CurrentMaxAoA * UtilMath.Deg2Rad), 1).normalized;
            }

            return desiredThrustVector;
        }

        /*
         * Solutions
         */

        // guidance flies in SI units and MET seconds, so this takes a DeNormalized() Solution
        public void SetSolution(Solution solution)
        {
            if (_machine.SetSolution(solution) && solution.T0 <= 0)
                PadSolution = solution;
        }

        /// <summary>
        ///     Drops a Solution that no longer applies and goes back to waiting for one. Only for use on the ground, the timed launch
        ///     buttons and kOS suffixes require a landed vessel. Does nothing while disabled, so a countdown started before engaging
        ///     leaves the throttle alone.
        /// </summary>
        public void FlushSolution()
        {
            if (!Enabled)
                return;

            bool prelaunchDone = _prelaunchDone;
            ResetGuidance();
            _prelaunchDone = prelaunchDone;

            // engaging without a countdown leaves the throttle up on the pad, and once the autopilot has left prelaunch nothing
            // else lowers it before T-0
            ThrustOff();
        }

        /*
         * Status and drawing
         */

        // "Awaiting liftoff: computing the ascent, step 2 of 5 (1m 12s)", rebuilt only when the progress text changes
        private string AwaitingComputing()
        {
            string? progress = Core.PSGSimulation.ProgressShort;
            if (progress == null)
                return _awaitingComputing ??= Localizer.Format("#MechJeb_Ascent_status12") + ": computing the ascent…"; //"Awaiting liftoff"

            if (!ReferenceEquals(progress, _awaitingProgressFrom) || _awaitingProgress == null)
            {
                _awaitingProgressFrom = progress;
                _awaitingProgress     = Localizer.Format("#MechJeb_Ascent_status12") + ": " + progress;
            }

            return _awaitingProgress;
        }

        // says why MechJeb can't fly the vessel while another mod has taken its controls; the string is rebuilt only when the lock
        // named changes
        private void SetLockStatus()
        {
            string? lockId = PSGFeedback.SteeringLockId();
            if (lockId != _lockStatusId || _lockStatus == null)
            {
                _lockStatusId = lockId;
                _lockStatus   = PSGFeedback.LockStatus(lockId);
            }

            Status.Set(_lockStatus, AscentStatusLevel.ERROR);
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
    }
}
