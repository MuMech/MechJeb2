using MuMech.Landing;
using UnityEngine;

namespace MuMech
{
    public class MechJebModulePoweredDescentGuidance : AutopilotModule
    {
        private PDGGuidanceLoop _guidanceStep;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public double PlanThrottle = 0.95;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public double MinThrottle = 0.375;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public double TargetClearance = 100.0;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public double TerminalHandoverDownrange = 1000.0;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public double TerminalGlideConstraint = 15.0;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public bool UseApolloTerminal = false;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public bool PulseThrottleMode = false;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public double PulsePeriod = 1.0;

        public MechJebModulePoweredDescentGuidance(MechJebCore core)
            : base(core)
        {
        }

        public bool Running
        {
            get { return _guidanceStep != null; }
        }

        public string PdgStatus
        {
            get
            {
                if (_guidanceStep == null) return "Idle";
                return _guidanceStep.Status;
            }
        }

        public PDGGuidanceLoop CurrentGuidanceStep
        {
            get { return _guidanceStep; }
        }

        public void StartTargetedLanding(object controller)
        {
            if (Vessel == null) return;
            if (Vessel.LandedOrSplashed) return;
            if (!Core.Target.PositionTargetExists) return;

            StartGuidance(controller, true);
        }

        public void StartUntargetedLanding(object controller)
        {
            if (Vessel == null) return;
            if (Vessel.LandedOrSplashed) return;

            StartGuidance(controller, false);
        }

        private void StartGuidance(object controller, bool landAtTarget)
        {
            StopGuidanceInternal(false);

            Users.Add(controller);

            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);

            Vessel.RemoveAllManeuverNodes();

            _guidanceStep = new PDGGuidanceLoop(Core, this)
            {
                LandAtTarget = landAtTarget,
                UseApolloTerminal = UseApolloTerminal,
                TargetClearance = TargetClearance,
                PlanThrottle = PlanThrottle,
                MinThrottle = MinThrottle,
                TerminalHandoverDownrange = TerminalHandoverDownrange,
                TerminalGlideConstraint = TerminalGlideConstraint,
                PulseThrottleMode = PulseThrottleMode,
                PulsePeriod = PulsePeriod
            };
        }

        public void StopGuidance()
        {
            StopGuidanceInternal(true);
        }

        private void StopGuidanceInternal(bool clearUsers)
        {
            if (clearUsers)
                Users.Clear();

            Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Attitude.Users.Remove(this);
            Core.Attitude.attitudeDeactivate();

            _guidanceStep = null;
        }

        public override void OnFixedUpdate()
        {
            if (_guidanceStep == null) return;

            AutopilotStep nextStep = _guidanceStep.OnFixedUpdate();

            if (nextStep == null)
                StopGuidance();
        }

        public override void Drive(FlightCtrlState s)
        {
            if (_guidanceStep == null) return;

            _guidanceStep.Drive(s);
        }

        protected override void OnModuleEnabled()
        {
            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
        }

        protected override void OnModuleDisabled()
        {
            StopGuidanceInternal(false);
        }
    }
}