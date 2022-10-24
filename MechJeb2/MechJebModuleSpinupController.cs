/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;

namespace MuMech
{
    public class MechJebModuleSpinupController : ComputerModule
    {
        public double RollAngularVelocity = 0;

        public MechJebModuleSpinupController(MechJebCore core) : base(core)
        {
        }

        private bool   _stabilizing = true;
        private double _startTime;

        public override void OnModuleEnabled()
        {
            _stabilizing = true;
            _startTime   = Math.Max(vesselState.time, _startTime);
            core.attitude.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.SetOmegaTarget(roll: double.NaN);
            core.attitude.SetActuationControl(true, true);
            // FIXME: this might overwrite someone else, but the only other consumer so far is the GuidanceController
            core.staging.autostageLimitInternal = -1;
            core.attitude.users.Remove(this);
            base.OnModuleDisabled();
        }
        
        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onStageActivate.Add(HandleStageEvent);
        }

        public override void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(HandleStageEvent);
        }

        private void HandleStageEvent(int data)
        {
            // wait a second to enable after staging because aerodynamics may kick us,
            // but on the first tick we may think we are stable
            if (!enabled || _stabilizing)
                _startTime = vesselState.time + 1.0;
        }

        public override void Drive(FlightCtrlState s)
        {
            if (vesselState.time < _startTime)
                return;

            core.staging.autostageLimitInternal = vessel.currentStage;

            if (!vessel.ActionGroups[KSPActionGroup.RCS])
                vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            if (_stabilizing && (core.attitude.attitudeAngleFromTarget() > 1.0 || core.vessel.angularVelocity.magnitude > 0.001))
                return;

            _stabilizing = false;

            core.attitude.SetOmegaTarget(roll: RollAngularVelocity);
            core.attitude.SetActuationControl(false, false);
        }
    }
}
