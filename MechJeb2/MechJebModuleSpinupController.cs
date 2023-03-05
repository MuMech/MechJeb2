/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSpinupController : ComputerModule
    {
        private enum SpinupState { INITIALIZED, STARTING, STABILIZING, SPINUP, FINISHED  }

        public double RollAngularVelocity = 0;

        public MechJebModuleSpinupController(MechJebCore core) : base(core)
        {
        }

        private SpinupState _state;
        private double      _startTime;

        public override void OnModuleEnabled()
        {
            _state     = SpinupState.INITIALIZED;
            _startTime = Math.Max(vesselState.time, _startTime);
            core.attitude.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            _state = SpinupState.FINISHED;
            core.attitude.SetOmegaTarget(roll: double.NaN);
            core.attitude.SetActuationControl(true, true);
            // FIXME: this might overwrite someone else, but the only other consumer so far is the GuidanceController
            core.staging.autostageLimitInternal = 0;
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
            _startTime = vesselState.time + 1.0;
        }

        public override void Drive(FlightCtrlState s)
        {
            if (_state == SpinupState.INITIALIZED)
                return;

            core.staging.autostageLimitInternal = vessel.currentStage;
            
            if (vesselState.time < _startTime)
                return;

            if (_state == SpinupState.STARTING)
                _state = SpinupState.STABILIZING;
            
            if (vessel.angularVelocityD.y / RollAngularVelocity >= 0.99)
                enabled = false;

            if (!vessel.ActionGroups[KSPActionGroup.RCS])
                vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            if (_state == SpinupState.STABILIZING && (core.attitude.attitudeAngleFromTarget() > 1.0 || core.vessel.angularVelocity.magnitude > 0.001))
                return;

            _state = SpinupState.SPINUP;

            core.attitude.SetOmegaTarget(roll: RollAngularVelocity);
            core.attitude.SetActuationControl(false, false);
        }

        public void AssertStart()
        {
            if (_state == SpinupState.INITIALIZED)
                _state = SpinupState.STARTING;
        }
    }
}
