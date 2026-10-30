using System;
using UnityEngine;

namespace MuMech
{
    public class AutopilotModule : ComputerModule
    {
        protected AutopilotModule(MechJebCore core) : base(core)
        {
        }

        public override void Drive(FlightCtrlState s)
        {
            if (CurrentStep == null) return;

            try
            {
                CurrentStep = CurrentStep.Drive(s);
                _driveFailures = 0;
            }
            catch (Exception ex)
            {
                StepFailed(ex, ref _driveFailures);
            }
        }

        public override void OnFixedUpdate()
        {
            if (CurrentStep == null) return;

            try
            {
                CurrentStep = CurrentStep.OnFixedUpdate();
                _updateFailures = 0;
            }
            catch (Exception ex)
            {
                StepFailed(ex, ref _updateFailures);
            }
        }

        // A step that throws stays the current step with the last attitude, throttle and warp requests in force, so a step
        // that keeps throwing stops the autopilot (about a second of physics ticks) rather than being retried forever.
        private const int MAX_STEP_FAILURES = 50;
        private int _driveFailures, _updateFailures;

        private void StepFailed(Exception ex, ref int failures)
        {
            if (failures++ == 0)
            {
                Debug.LogError($"[MechJeb] {GetType().Name}: step {CurrentStep?.GetType().Name} threw (logged once while it keeps throwing):");
                Debug.LogException(ex);
            }

            if (failures < MAX_STEP_FAILURES)
                return;

            Debug.LogError($"[MechJeb] {GetType().Name}: stopped after {failures} exceptions in a row from step {CurrentStep?.GetType().Name}");
            _driveFailures = _updateFailures = 0;
            StopAfterStepFailures();
        }

        /// <summary>Stops the autopilot when its step keeps throwing; an autopilot overrides it to release what it holds.</summary>
        protected virtual void StopAfterStepFailures()
        {
            SetStep(null);
            Users.Clear();
        }

        protected void SetStep(AutopilotStep step) => CurrentStep = step;

        public string Status => CurrentStep == null ? "Off" : CurrentStep.Status;

        protected bool Active => CurrentStep != null;

        public AutopilotStep CurrentStep { get; private set; }
    }

    public class AutopilotStep
    {
        protected readonly MechJebCore Core;

        //conveniences:
        protected VesselState   VesselState => Core.VesselState;
        protected Vessel        Vessel      => Core.part.vessel;
        protected CelestialBody MainBody    => Core.part.vessel.mainBody;
        protected Orbit         Orbit       => Core.part.vessel.orbit;

        protected AutopilotStep(MechJebCore core)
        {
            Core = core;
        }

        public virtual AutopilotStep Drive(FlightCtrlState s) => this;
        public virtual AutopilotStep OnFixedUpdate()          => this;
        public         string        Status                   { get; protected set; }
    }
}
