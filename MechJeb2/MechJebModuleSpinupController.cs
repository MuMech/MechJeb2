/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

namespace MuMech
{
    public class MechJebModuleSpinupController : ComputerModule
    {
        public int    ActivationStage     = -1;
        public double RollAngularVelocity = 0;

        public MechJebModuleSpinupController(MechJebCore core) : base(core)
        {
        }

        private bool   _stabilizing = true;
        private double _startTime;

        public override void OnModuleEnabled()
        {
            _stabilizing = true;
            _startTime   = 0;
        }

        public override void OnModuleDisabled()
        {
            core.attitude.SetOmegaTarget(roll: double.NaN);
            core.attitude.SetActuationControl(true, true);
            core.staging.autostageLimitInternal = -1;
            core.attitude.users.Remove(this);
            base.OnModuleDisabled();
        }

        public override void Drive(FlightCtrlState s)
        {
            if (vessel.currentStage < ActivationStage)
            {
                enabled = false;
                return;
            }

            if (vessel.currentStage > ActivationStage)
            {
                // wait for at least a second after stabilizing to ensure that if we get kicked by
                // aerodynamics on staging that we don't think we're stable due to the very first tick.
                _startTime = vesselState.time + 1.0;
                return;
            }

            if (vesselState.time < _startTime)
                return;

            core.staging.autostageLimitInternal = vessel.currentStage;

            if (!vessel.ActionGroups[KSPActionGroup.RCS])
                vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            if (_stabilizing && (core.attitude.attitudeAngleFromTarget() > 1.0 || core.vessel.angularVelocity.magnitude > 0.001))
            {
                return;
            }

            _stabilizing = false;

            core.attitude.users.Add(this);
            core.attitude.SetOmegaTarget(roll: RollAngularVelocity);
            core.attitude.SetActuationControl(false, false);
        }
    }
}
