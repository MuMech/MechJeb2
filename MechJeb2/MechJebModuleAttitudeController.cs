extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using MuMech.AttitudeControllers;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public enum AttitudeReference
    {
        INERTIAL, //world coordinate system.
        INERTIAL_COT, //world coordinate system fixed for CoT offset.
        ORBIT, //forward = prograde, left = normal plus, up = radial plus
        ORBIT_HORIZONTAL, //forward = surface projection of orbit velocity, up = surface normal
        SURFACE_NORTH, //forward = north, left = west, up = surface normal
        SURFACE_NORTH_COT, //forward = north, left = west, up = surface normal, fixed for CoT offset
        SURFACE_VELOCITY, //forward = surface frame vessel velocity, up = perpendicular component of surface normal
        TARGET, //forward = toward target, up = perpendicular component of vessel heading
        RELATIVE_VELOCITY, //forward = toward relative velocity direction, up = tbd
        TARGET_ORIENTATION, //forward = direction target is facing, up = target up
        MANEUVER_NODE, //forward = next maneuver node direction, up = tbd
        MANEUVER_NODE_COT, //forward = next maneuver node direction, up = tbd, fixed for CoT offset
        SUN, //forward = orbit velocity of the parent body orbiting the sun, up = radial plus of that orbit
        SURFACE_HORIZONTAL //forward = surface velocity horizontal component, up = surface normal
    }

    [UsedImplicitly]
    public class MechJebModuleAttitudeController : ComputerModule
    {
        private float _timeCount;
        private Part _lastReferencePart;

        public bool RCSAuto = false;
        private readonly bool _attitudeRCSControl = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        [ValueInfoItem("#MechJeb_SteeringError", InfoItem.Category.Vessel, format = "F1", units = "º")]
        //Steering error
        public readonly MovingAverage SteeringError = new MovingAverage();

        public bool AttitudeKillRot;

        private bool _attitudeChanged;

        private AttitudeReference _attitudeReference = AttitudeReference.INERTIAL;

        public Vector3d AxisControl      { get; private set; } = Vector3d.one;
        public Vector3d ActuationControl { get; private set; } = Vector3d.one;
        public Vector3d OmegaTarget      { get; private set; } = new Vector3d(double.NaN, double.NaN, double.NaN);

        public BaseAttitudeController Controller { get; private set; }
        private readonly List<BaseAttitudeController> _controllers = new List<BaseAttitudeController>();

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int ActiveController = 3;

        public void SetActiveController(int i)
        {
            ActiveController = i;
            Controller = _controllers[ActiveController];
            Controller.OnStart();
        }

        protected override void OnModuleEnabled()
        {
            _timeCount = 50;
            SetAxisControl(true, true, true);
            SetActuationControl();
            SetOmegaTarget();
            Controller.OnModuleEnabled();
        }

        public AttitudeReference AttitudeReference
        {
            get => _attitudeReference;
            private set
            {
                if (_attitudeReference == value) return;

                _attitudeReference = value;
                _attitudeChanged = true;
            }
        }

        private QuaternionD _attitudeTarget = QuaternionD.identity;

        public QuaternionD AttitudeTarget
        {
            get => _attitudeTarget;
            private set
            {
                if (Math.Abs(Vector3d.Angle(_attitudeTarget * Vector3d.forward, value * Vector3d.forward)) > 10)
                {
                    SetAxisControl(true, true, true);
                    _attitudeChanged = true;
                }

                _attitudeTarget = value;
            }
        }

        public QuaternionD RequestedAttitude { get; private set; } = QuaternionD.identity;

        public double AttitudeError;

        public Vector3d Torque;
        public Vector3d Inertia;

        public MechJebModuleAttitudeController(MechJebCore core)
            : base(core)
        {
            Priority = 800;

            _controllers.Add(new MJAttitudeController(this));
            _controllers.Add(new KosAttitudeController(this));
            _controllers.Add(new HybridController(this));
            _controllers.Add(new BetterController(this));
            _controllers.Add(new LQRController(this));

            Controller = new BetterController(this);
        }

        protected override void OnModuleDisabled() => Controller.OnModuleDisabled();

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            foreach (BaseAttitudeController attitudeController in _controllers) attitudeController.OnLoad(local, type, global);
        }

        public override void OnStart(PartModule.StartState state) => SetActiveController(ActiveController);

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);
            foreach (BaseAttitudeController attitudeController in _controllers) attitudeController.OnSave(local, type, global);
        }

        public void SetAxisControl(bool pitch, bool yaw, bool roll) => AxisControl = new Vector3d(pitch ? 1 : 0, roll ? 1 : 0, yaw ? 1 : 0);

        public void SetActuationControl(bool pitch = true, bool yaw = true, bool roll = true) =>
            ActuationControl = new Vector3d(pitch ? 1 : 0, roll ? 1 : 0, yaw ? 1 : 0);

        public void SetOmegaTarget(double pitch = double.NaN, double yaw = double.NaN, double roll = double.NaN) =>
            OmegaTarget = new Vector3d(pitch, roll, yaw);

        public QuaternionD AttitudeGetReferenceRotation(AttitudeReference reference)
        {
            Vector3 fwd, up;
            QuaternionD rotRef = QuaternionD.identity;

            if (Core.Target.Target == null && (reference == AttitudeReference.TARGET || reference == AttitudeReference.TARGET_ORIENTATION ||
                    reference == AttitudeReference.RELATIVE_VELOCITY))
            {
                AttitudeDeactivate();
                return rotRef;
            }

            if ((reference == AttitudeReference.MANEUVER_NODE || reference == AttitudeReference.MANEUVER_NODE_COT) &&
                Vessel.patchedConicSolver.maneuverNodes.Count == 0)
            {
                AttitudeDeactivate();
                return rotRef;
            }

            Vector3d thrustForward = VesselState.ThrustForward;

            // the off-axis thrust modifications get into a fight with the differential throttle so do not use them when diff throttle is used
            if (Core.Thrust.DifferentialThrottle)
                thrustForward = VesselState.Forward;

            switch (reference)
            {
                case AttitudeReference.INERTIAL_COT:
                    rotRef = MathExtensions.FromToRotation(thrustForward, VesselState.Forward);
                    break;
                case AttitudeReference.ORBIT:
                    rotRef = QuaternionD.LookRotation(VesselState.OrbitalVelocity.normalized, VesselState.Up);
                    break;
                case AttitudeReference.ORBIT_HORIZONTAL:
                    rotRef = QuaternionD.LookRotation(Vector3d.Exclude(VesselState.Up, VesselState.OrbitalVelocity.normalized), VesselState.Up);
                    break;
                case AttitudeReference.SURFACE_NORTH:
                    rotRef = VesselState.RotationSurface;
                    break;
                case AttitudeReference.SURFACE_NORTH_COT:
                    rotRef = VesselState.RotationSurface;
                    rotRef = MathExtensions.FromToRotation(thrustForward, VesselState.Forward) * rotRef;
                    break;
                case AttitudeReference.SURFACE_VELOCITY:
                    rotRef = QuaternionD.LookRotation(VesselState.SurfaceVelocity.normalized, VesselState.Up);
                    break;
                case AttitudeReference.TARGET:
                    fwd = (Core.Target.Position - Vessel.GetTransform().position).normalized;
                    up = Vector3d.Cross(fwd, VesselState.NormalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = QuaternionD.LookRotation(fwd, up);
                    break;
                case AttitudeReference.RELATIVE_VELOCITY:
                    fwd = Core.Target.RelativeVelocity.normalized;
                    up = Vector3d.Cross(fwd, VesselState.NormalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = QuaternionD.LookRotation(fwd, up);
                    break;
                case AttitudeReference.TARGET_ORIENTATION:
                    Transform targetTransform = Core.Target.Transform;
                    Vector3 targetUp = targetTransform.up;
                    rotRef = Core.Target.CanAlign
                        ? QuaternionD.LookRotation(targetTransform.forward, targetUp)
                        : QuaternionD.LookRotation(targetUp, targetTransform.right);
                    break;
                case AttitudeReference.MANEUVER_NODE:
                    fwd = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit);
                    up = Vector3d.Cross(fwd, VesselState.NormalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = QuaternionD.LookRotation(fwd, up);
                    break;
                case AttitudeReference.MANEUVER_NODE_COT:
                    fwd = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit);
                    up = Vector3d.Cross(fwd, VesselState.NormalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = QuaternionD.LookRotation(fwd, up);
                    rotRef = MathExtensions.FromToRotation(thrustForward, VesselState.Forward) * rotRef;
                    break;
                case AttitudeReference.SUN:
                    Orbit baseOrbit = Vessel.mainBody == Planetarium.fetch.Sun ? Vessel.orbit : Orbit.TopParentOrbit();
                    up = VesselState.CoM - Planetarium.fetch.Sun.transform.position;
                    fwd = Vector3d.Cross(-baseOrbit.GetOrbitNormal().xzy.normalized, up);
                    rotRef = QuaternionD.LookRotation(fwd, up);
                    break;
                case AttitudeReference.SURFACE_HORIZONTAL:
                    rotRef = QuaternionD.LookRotation(Vector3d.Exclude(VesselState.Up, VesselState.SurfaceVelocity.normalized), VesselState.Up);
                    break;
            }

            return rotRef;
        }

        private Vector3d AttitudeWorldToReference(Vector3d vector, AttitudeReference reference) =>
            QuaternionD.Inverse(AttitudeGetReferenceRotation(reference)) * vector;

        private Vector3d AttitudeReferenceToWorld(Vector3d vector, AttitudeReference reference) => AttitudeGetReferenceRotation(reference) * vector;

        public void AttitudeTo(QuaternionD attitude, AttitudeReference reference, object controller, bool axisCtrlPitch = true,
            bool axisCtrlYaw = true, bool axisCtrlRoll = true)
        {
            Users.Add(controller);
            AttitudeReference = reference;
            AttitudeTarget = attitude;
            SetOmegaTarget();
            SetAxisControl(axisCtrlPitch, axisCtrlYaw, axisCtrlRoll);
        }

        public void AttitudeTo(Vector3d direction, AttitudeReference reference, object controller, bool killRollRotation = true)
        {
            //double ang_diff = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, attitudeGetReferenceRotation(reference) * direction));

            Vector3 dir = direction;

            Vector3 up = !Enabled ? AttitudeWorldToReference(-Vessel.GetTransform().forward, reference) : AttitudeWorldToReference(AttitudeReferenceToWorld(AttitudeTarget * Vector3d.up, reference), reference);
            Vector3.OrthoNormalize(ref dir, ref up);
            AttitudeTo(QuaternionD.LookRotation(dir, up), reference, controller, axisCtrlRoll: killRollRotation);
            if (killRollRotation) { SetOmegaTarget(roll: 0); }
        }

        public void AttitudeTo(double heading, double pitch, double roll, object controller, bool axisCtrlPitch = true, bool axisCtrlYaw = true,
            bool axisCtrlRoll = true, bool fixCOT = false)
        {
            QuaternionD attitude = QuaternionD.AngleAxis((float)heading, Vector3.up) * QuaternionD.AngleAxis(-(float)pitch, Vector3.right) *
                QuaternionD.AngleAxis(-(float)roll, Vector3.forward);
            AttitudeReference reference = fixCOT ? AttitudeReference.SURFACE_NORTH_COT : AttitudeReference.SURFACE_NORTH;
            AttitudeTo(attitude, reference, controller, axisCtrlPitch,
                axisCtrlYaw, axisCtrlRoll);
        }

        // points straight up at the given heading, actuation and control stay off until liftoff
        public void AttitudeToVerticalLaunch(double heading, bool forceRoll, double roll, object controller)
        {
            AttitudeTo(heading, 90, roll, controller, fixCOT: true);
            bool liftedOff = Vessel.LiftedOff() && !Vessel.Landed;

            SetActuationControl(liftedOff, liftedOff, liftedOff);
            SetAxisControl(liftedOff, liftedOff, liftedOff && forceRoll);
        }

        // pitch/heading variant of LimitedAttitudeTo
        public void LimitedAttitudeTo(double heading, double pitch, bool forceRoll, double roll, Func<Vector3d, Vector3d> aoaLimiter,
            object controller)
        {
            Vector3d headingVector = Math.Sin(heading * UtilMath.Deg2Rad) * VesselState.East +
                Math.Cos(heading * UtilMath.Deg2Rad) * VesselState.North;

            Vector3d direction = Math.Cos(pitch * UtilMath.Deg2Rad) * headingVector
                + Math.Sin(pitch * UtilMath.Deg2Rad) * VesselState.Up;

            LimitedAttitudeTo(direction, forceRoll, roll, aoaLimiter, controller);
        }

        // applies the AoA limiter to the direction, then holds the given roll if forceRoll is set
        public void LimitedAttitudeTo(Vector3d direction, bool forceRoll, double roll, Func<Vector3d, Vector3d> aoaLimiter, object controller)
        {
            direction = aoaLimiter(direction.normalized);

            // calculate pitch and heading after applying AoA limiter
            double pitch = 90 - Vector3d.Angle(direction, VesselState.Up);
            double heading = Clamp360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(direction, VesselState.East),
                Vector3d.Dot(direction, VesselState.North)));

            if (forceRoll)
                AttitudeTo(heading, pitch, roll, controller, fixCOT: true);
            else
                AttitudeTo(direction, AttitudeReference.INERTIAL_COT, controller);

            SetActuationControl();
            SetAxisControl(true, true, forceRoll);
        }

        public void AttitudeDeactivate()
        {
            Users.Clear();
            _attitudeChanged = true;
        }

        //angle in degrees between the vessel's current pointing direction and the attitude target, ignoring roll
        public double AttitudeAngleFromTarget() =>
            Enabled
                ? Math.Abs(Vector3d.Angle(AttitudeGetReferenceRotation(AttitudeReference) * AttitudeTarget * Vector3d.forward, VesselState.Forward))
                : 0;

        public Vector3d TargetAttitude()
        {
            if (Enabled)
                return AttitudeGetReferenceRotation(AttitudeReference) * AttitudeTarget * Vector3d.forward;

            return Vector3d.zero;
        }

        public override void OnFixedUpdate()
        {
            SteeringError.Value = AttitudeError = AttitudeAngleFromTarget();

            Torque = VesselState.TorqueAvailable;
            if (Core.Thrust.DifferentialThrottle &&
                Core.Thrust.DifferentialThrottleSuccess == MechJebModuleThrustController.DifferentialThrottleStatus.SUCCESS)
                Torque += VesselState.TorqueDifferentialThrottle * Vessel.ctrlState.mainThrottle / 2.0;

            // Inertia is a bad name. It's the "angular distance to stop"
            Inertia = 0.5 * Vector3d.Scale(
                VesselState.AngularMomentum.Sign(),
                Vector3d.Scale(
                    Vector3d.Scale(VesselState.AngularMomentum, VesselState.AngularMomentum),
                    Vector3d.Scale(Torque, VesselState.MoI).InvertNoNaN()
                )
            );
            Controller.OnFixedUpdate();
        }

        public override void OnUpdate()
        {
            if (_attitudeChanged)
            {
                if (AttitudeReference != AttitudeReference.INERTIAL && AttitudeReference != AttitudeReference.INERTIAL_COT) AttitudeKillRot = false;

                if (!(Controller is BetterController) && !(Controller is LQRController))
                    Controller.Reset();

                _attitudeChanged = false;
            }

            Controller.OnUpdate();
        }

        public override void Drive(FlightCtrlState s)
        {
            // Direction we want to be facing
            RequestedAttitude = AttitudeGetReferenceRotation(AttitudeReference) * AttitudeTarget;

            Controller.DrivePre(s, out Vector3d act, out Vector3d deltaEuler);

            act.Scale(ActuationControl);

            SetFlightCtrlState(act, deltaEuler, s, 1);

            act = new Vector3d(s.pitch, s.roll, s.yaw);

            // Feed the control torque to the differential throttle.  The halving must match the diff throttle torque that
            // OnFixedUpdate() adds to the torque the controllers normalize act against.
            if (Core.Thrust.DifferentialThrottleSuccess == MechJebModuleThrustController.DifferentialThrottleStatus.SUCCESS)
                Core.Thrust.DifferentialThrottleDemandedTorque =
                    -Vector3d.Scale(act, VesselState.TorqueDifferentialThrottle * Vessel.ctrlState.mainThrottle / 2.0);
        }

        private void SetFlightCtrlState(Vector3d act, Vector3d deltaEuler, FlightCtrlState s, float driveLimit)
        {
            bool userCommandingPitch = !Mathfx.Approx(s.pitch, s.pitchTrim, 0.1F);
            bool userCommandingYaw = !Mathfx.Approx(s.yaw, s.yawTrim, 0.1F);
            bool userCommandingRoll = !Mathfx.Approx(s.roll, s.rollTrim, 0.1F);

            // Disable the new SAS so it won't interfere. But enable it while in time warp for compatibility with PersistentRotation
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH || TimeWarp.CurrentRateIndex == 0)
                Part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);

            if (AttitudeKillRot)
                if (_lastReferencePart != Vessel.GetReferenceTransformPart() || userCommandingPitch || userCommandingYaw || userCommandingRoll)
                {
                    AttitudeTo(QuaternionD.LookRotation(Vessel.GetTransform().up, -Vessel.GetTransform().forward), AttitudeReference.INERTIAL, null);
                    _lastReferencePart = Vessel.GetReferenceTransformPart();
                }

            if (userCommandingPitch)
                Controller.Reset(0);

            if (userCommandingRoll)
                Controller.Reset(1);

            if (userCommandingYaw)
                Controller.Reset(2);

            if (!userCommandingRoll)
                if (!double.IsNaN(act.y))
                    s.roll = Mathf.Clamp((float)act.y, -driveLimit, driveLimit);

            if (!userCommandingPitch && !userCommandingYaw)
            {
                if (!double.IsNaN(act.x)) s.pitch = Mathf.Clamp((float)act.x, -driveLimit, driveLimit);
                if (!double.IsNaN(act.z)) s.yaw = Mathf.Clamp((float)act.z, -driveLimit, driveLimit);
            }

            // RCS and SAS control:
            Vector3d absErr; // Absolute error (º)
            absErr.x = Math.Abs(deltaEuler.x);
            absErr.y = Math.Abs(deltaEuler.y);
            absErr.z = Math.Abs(deltaEuler.z);

            if (absErr.x < 0.4 && absErr.y < 0.4 && absErr.z < 0.4)
            {
                if (_timeCount < 50)
                {
                    _timeCount++;
                }
                else
                {
                    if (RCSAuto)
                        if (_attitudeRCSControl && Core.RCS.Users.Count == 0)
                            Part.vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
                }
            }
            else if (absErr.x > 1.0 || absErr.y > 1.0 || absErr.z > 1.0)
            {
                _timeCount = 0;
                if (RCSAuto && (absErr.x > 3.0 || absErr.y > 3.0 || absErr.z > 3.0) &&
                    Core.Thrust.Limiter != MechJebModuleThrustController.LimitMode.UNSTABLE_IGNITION)
                    if (_attitudeRCSControl)
                        Part.vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
            }
        } // end of SetFlightCtrlState
    }
}
