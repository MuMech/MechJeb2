#nullable enable

using MechJebLib.Control;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleHoverslamAutopilot : ComputerModule
    {
        public MechJebModuleHoverslamAutopilot(MechJebCore core) : base(core)
        {
        }

        // TODO: have the button say "disengage" when it is engaged
        [ActionInfoItem("#MechJeb_HoverslamEngage", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamEngage_tooltip")] //Engage hoverslam
        public void ToggleEnabled() => Enabled = !Enabled;

        [EditableInfoItem("#MechJeb_HoverslamIgnitionLead", InfoItem.Category.Hoverslam, width = 50, rightLabel = "s", expandWidth = true, tooltip = "#MechJeb_HoverslamIgnitionLead_tooltip")] //Ignition lead
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))]
        public readonly EditableDouble IgnitionLead = new EditableDouble(0.0);

        [EditableInfoItem("#MechJeb_HoverslamTouchdownSpeed", InfoItem.Category.Hoverslam, width = 50, rightLabel = "m/s", expandWidth = true, tooltip = "#MechJeb_HoverslamTouchdownSpeed_tooltip")]
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))] //Touchdown speed
        public readonly EditableDouble TouchdownSpeed = new EditableDouble(0);

        [ToggleInfoItem("#MechJeb_HoverslamAutoWarp", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamAutoWarp_tooltip")]
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))] //Hoverslam auto-warp
        public bool AutoWarp = true;

        [ToggleInfoItem("#MechJeb_HoverslamHoldUpright", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamHoldUpright_tooltip")]
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))] //Hold upright after touchdown
        public bool HoldUpright;

        [ValueInfoItem("#MechJeb_HoverslamState", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamState_tooltip")] //Hoverslam state
        public string HoverslamState => !Enabled ? "Disabled" : _state.ToString();

        private const double DEFAULT_MIN_ON_TIME = 0.50;

        [EditableInfoItem("#MechJeb_HoverslamPWMPulseWidth", InfoItem.Category.Hoverslam, width = 50, rightLabel = "s", expandWidth = true, tooltip = "#MechJeb_HoverslamPWMTimeWidth_tooltip")]
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))] //PWM time width
        public readonly EditableDouble PWMPulseWidth = new EditableDouble(DEFAULT_MIN_ON_TIME);

        protected override void OnModuleEnabled()
        {
            Reset();
            Core.Thrust.Users.Add(this);
            Core.Attitude.Users.Add(this);
            TransitionTo(DetermineState(State.Align));
        }

        protected override void OnModuleDisabled()
        {
            Reset();
            Core.Warp.MinimumWarp(true);
            Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            Core.Attitude.Users.Remove(this);
        }

        private void Reset() => _lastAdjV = new Vector3d(double.NaN, double.NaN, double.NaN);

        public override void OnFixedUpdate()
        {
            UpdateAdjV();
            UpdateState();
            TickState();
            _lastAdjV = _adjV;
        }

        private enum State { Init, Align, Coast, Burn, Vertical, Finished }

        private State _state;
        private Vector3d _lastAdjV;
        private Vector3d _adjV;

        private void UpdateAdjV() => _adjV = VesselState.surfaceVelocity + Core.Hoverslam.FinalDescentSpeed * VesselState.up;

        private bool AlignedForBurn() => SafeAcos(Vector3d.Dot(VesselState.forward, Core.Hoverslam.IgnitionAttitude)) < Deg2Rad(1) && Vessel.angularVelocity.magnitude < 0.001;

        private State DetermineState(State desired)
        {
            if (!IsFinite(Core.Hoverslam.IgnitionUT))
                return State.Init;

            if (desired == State.Init)
                desired = State.Align;
            if (desired == State.Align && AlignedForBurn())
                desired = State.Coast;
            double countdown = Core.Hoverslam.IgnitionCountdown;
            if ((desired == State.Coast || desired == State.Align) && IsFinite(countdown) && countdown <= Time.fixedDeltaTime + IgnitionLead)
                desired = State.Burn;
            if (desired == State.Burn && !double.IsNaN(_lastAdjV[0]) && Vector3d.Angle(_lastAdjV, _adjV) > 10.0)
                desired = State.Vertical;
            if (desired == State.Burn || desired == State.Vertical)
            {
                if (Vector3d.Dot(VesselState.surfaceVelocity, VesselState.up) >= 0.0)
                    desired = State.Finished;
                if (!Vessel.VesselOffGround())
                    desired = State.Finished;
            }

            return desired;
        }

        private void UpdateState()
        {
            State desired = DetermineState(_state);
            if (desired != _state) TransitionTo(desired);
        }

        private void TransitionTo(State next)
        {
            _state = next;
            switch (next)
            {
                case State.Init:     OnEnterInit(); break;
                case State.Align:    OnEnterAligning(); break;
                case State.Coast:    OnEnterCoast(); break;
                case State.Burn:     OnEnterBurn(); break;
                case State.Vertical: OnEnterFinalDescent(); break;
                case State.Finished: OnEnterFinished(); break;
            }
        }

        private void TickState()
        {
            switch (_state)
            {
                case State.Init:     TickInit(); break;
                case State.Align:    TickAligning(); break;
                case State.Coast:    TickCoast(); break;
                case State.Burn:     TickBurn(); break;
                case State.Vertical: TickFinalDescent(); break;
                case State.Finished: TickFinished(); break;
            }
        }

        private void OnEnterInit() => Core.Thrust.ThrustOff();

        private void TickInit()
        {
        }

        private void OnEnterAligning() => Core.Thrust.ThrustOff();

        private void TickAligning()
        {
            Core.Attitude.attitudeTo(Core.Hoverslam.IgnitionAttitude, AttitudeReference.INERTIAL_COT, this);
            if (!MuUtils.PhysicsRunning() && VesselState.time + TimeWarp.fixedDeltaTime > Core.Hoverslam.IgnitionUT)
                Core.Warp.MinimumWarp(true);
        }

        private void OnEnterCoast() => Core.Thrust.ThrustOff();

        private void TickCoast()
        {
            Core.Attitude.attitudeTo(Core.Hoverslam.IgnitionAttitude, AttitudeReference.INERTIAL_COT, this);
            if (AutoWarp)
                Core.Warp.WarpToUT(Core.Hoverslam.IgnitionUT);
            else if (!MuUtils.PhysicsRunning() && VesselState.time + TimeWarp.fixedDeltaTime > Core.Hoverslam.IgnitionUT)
                Core.Warp.MinimumWarp(true);
        }

        private void OnEnterBurn() => Core.Thrust.TargetThrottle = 1.0f;

        private void TickBurn() => Core.Attitude.attitudeTo(-_adjV, AttitudeReference.INERTIAL_COT, this);

        private void OnEnterFinalDescent()
        {
            _pwm.Reset();
            Core.Attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, this);
        }

        private readonly DeltaSigmaThrottleModulator _pwm = new DeltaSigmaThrottleModulator(0.02, DEFAULT_MIN_ON_TIME);

        private void TickFinalDescent()
        {
            if (Vector3d.Dot(VesselState.surfaceVelocity, VesselState.up) >= -1.0)
                Core.Attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, this);

            double v2 = VesselState.surfaceVelocity.sqrMagnitude;
            double g = Vessel.graviticAcceleration.magnitude;
            double h = VesselState.altitudeBottom;
            double vf = TouchdownSpeed;

            double accel = g + 0.5 * (v2 - vf * vf) / h;

            _pwm.MinOnTime = PWMPulseWidth;
            _pwm.MinOffTime = TimeWarp.fixedDeltaTime;
            Core.Thrust.TargetThrottle = _pwm.ThrottleCommand(accel, VesselState.minThrustAccel, VesselState.maxThrustAccel, TimeWarp.fixedDeltaTime);
        }

        private void OnEnterFinished()
        {
            if (HoldUpright)
            {
                Core.SmartASS.mode = MechJebModuleSmartASS.Mode.SURFACE;
                Core.SmartASS.target = MechJebModuleSmartASS.Target.VERTICAL_PLUS;
                Core.SmartASS.Engage();
            }

            Enabled = false;
        }

        private void TickFinished() { }
    }
}
