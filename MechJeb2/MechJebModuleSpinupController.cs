/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

extern alias JetBrainsAnnotations;
using System;

namespace MuMech
{
    public class MechJebModuleSpinupController : ComputerModule
    {
        private enum SpinupState { INITIALIZED, STARTING, STABILIZING, SPINUP, FINISHED }

        public double RollAngularVelocity = 0;

        public MechJebModuleSpinupController(MechJebCore core) : base(core)
        {
        }

        private SpinupState _state;
        private double      _startTime;

        protected override void OnModuleEnabled()
        {
            _state     = SpinupState.INITIALIZED;
            _startTime = Math.Max(VesselState.time, _startTime);
            Core.Attitude.Users.Add(this);
        }

        protected override void OnModuleDisabled()
        {
            _state = SpinupState.FINISHED;
            Core.Attitude.SetOmegaTarget(roll: double.NaN);
            Core.Attitude.SetActuationControl();
            Core.Staging.AutoStageLimitRemove(this);
            Core.Attitude.Users.Remove(this);
            base.OnModuleDisabled();
        }

        public override void OnStart(PartModule.StartState state) => GameEvents.onStageActivate.Add(HandleStageEvent);

        public override void OnDestroy() => GameEvents.onStageActivate.Remove(HandleStageEvent);

        private void HandleStageEvent(int data) =>
            // wait a second to enable after staging because aerodynamics may kick us,
            // but on the first tick we may think we are stable
            _startTime = VesselState.time + 1.0;

        public override void Drive(FlightCtrlState s)
        {
            if (_state == SpinupState.INITIALIZED)
                return;

            Core.Staging.AutoStageLimitRequest(Vessel.currentStage, this);

            if (VesselState.time < _startTime)
                return;

            if (_state == SpinupState.STARTING)
                _state = SpinupState.STABILIZING;

            if (Vessel.angularVelocityD.y / RollAngularVelocity >= 0.99)
                Enabled = false;

            if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            if (_state == SpinupState.STABILIZING && (Core.Attitude.attitudeAngleFromTarget() > 1.0 || Core.vessel.angularVelocity.magnitude > 0.001))
                return;

            _state = SpinupState.SPINUP;

            Core.Attitude.SetOmegaTarget(roll: RollAngularVelocity);
            Core.Attitude.SetActuationControl(false, false);
        }

        public void AssertStart()
        {
            if (_state == SpinupState.INITIALIZED)
                _state = SpinupState.STARTING;
        }
    }
}
