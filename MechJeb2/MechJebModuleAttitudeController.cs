using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using MuMech.AttitudeControllers;
using UnityEngine;

namespace MuMech
{
    public enum AttitudeReference
    {
        INERTIAL,           //world coordinate system.
        INERTIAL_COT,       //world coordinate system fixed for CoT offset.
        ORBIT,              //forward = prograde, left = normal plus, up = radial plus
        ORBIT_HORIZONTAL,   //forward = surface projection of orbit velocity, up = surface normal
        SURFACE_NORTH,      //forward = north, left = west, up = surface normal
        SURFACE_NORTH_COT,  //forward = north, left = west, up = surface normal, fixed for CoT offset
        SURFACE_VELOCITY,   //forward = surface frame vessel velocity, up = perpendicular component of surface normal
        TARGET,             //forward = toward target, up = perpendicular component of vessel heading
        RELATIVE_VELOCITY,  //forward = toward relative velocity direction, up = tbd
        TARGET_ORIENTATION, //forward = direction target is facing, up = target up
        MANEUVER_NODE,      //forward = next maneuver node direction, up = tbd
        MANEUVER_NODE_COT,  //forward = next maneuver node direction, up = tbd, fixed for CoT offset
        SUN,                //forward = orbit velocity of the parent body orbiting the sun, up = radial plus of that orbit
        SURFACE_HORIZONTAL  //forward = surface velocity horizontal component, up = surface normal
    }

    [UsedImplicitly]
    public class MechJebModuleAttitudeController : ComputerModule
    {
        private float timeCount;
        private Part  lastReferencePart;

        public           bool RCS_auto           = false;
        private readonly bool attitudeRCScontrol = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        [ValueInfoItem("#MechJeb_SteeringError", InfoItem.Category.Vessel, format = "F1", units = "º")]
        //Steering error
        public readonly MovingAverage steeringError = new MovingAverage();

        public bool attitudeKILLROT;

        private bool attitudeChanged;

        private AttitudeReference _attitudeReference = AttitudeReference.INERTIAL;

        public Vector3d AxisControl      { get; private set; } = Vector3d.one;
        public Vector3d ActuationControl { get; private set; } = Vector3d.one;
        public Vector3d OmegaTarget      { get; private set; } = new Vector3d(double.NaN, double.NaN, double.NaN);

        public           BaseAttitudeController       Controller { get; private set; }
        private readonly List<BaseAttitudeController> _controllers = new List<BaseAttitudeController>();

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int activeController = 3;

        public void SetActiveController(int i)
        {
            activeController = i;
            Controller       = _controllers[activeController];
            Controller.OnStart();
        }

        protected override void OnModuleEnabled()
        {
            timeCount = 50;
            SetAxisControl(true, true, true);
            SetActuationControl();
            SetOmegaTarget();
            Controller.OnModuleEnabled();
        }

        public AttitudeReference attitudeReference
        {
            get => _attitudeReference;
            private set
            {
                if (_attitudeReference == value) return;

                _attitudeReference = value;
                attitudeChanged    = true;
            }
        }

        private Quaternion _attitudeTarget = Quaternion.identity;

        public Quaternion attitudeTarget
        {
            get => _attitudeTarget;
            private set
            {
                if (Math.Abs(Vector3d.Angle(_attitudeTarget * Vector3d.forward, value * Vector3d.forward)) > 10)
                {
                    SetAxisControl(true, true, true);
                    attitudeChanged = true;
                }

                _attitudeTarget = value;
            }
        }

        public Quaternion RequestedAttitude { get; private set; } = Quaternion.identity;

        //[Persistent(pass = (int)Pass.Global | (int)Pass.Type), ToggleInfoItem("Use stock SAS", InfoItem.Category.Vessel)]
        // Disable the use of Stock SAS for now
        private readonly bool useSAS = false;

        private Quaternion lastSAS;

        public double attitudeError;

        public Vector3d torque;
        public Vector3d inertia;

        public MechJebModuleAttitudeController(MechJebCore core)
            : base(core)
        {
            Priority = 800;

            _controllers.Add(new MJAttitudeController(this));
            _controllers.Add(new KosAttitudeController(this));
            _controllers.Add(new HybridController(this));
            _controllers.Add(new BetterController(this));


            Controller = new BetterController(this);
        }

        protected override void OnModuleDisabled()
        {
            if (useSAS) Part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            Controller.OnModuleDisabled();
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            foreach (BaseAttitudeController attitudeController in _controllers) attitudeController.OnLoad(local, type, global);
        }

        public override void OnStart(PartModule.StartState state) => SetActiveController(activeController);

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

        public Quaternion attitudeGetReferenceRotation(AttitudeReference reference)
        {
            Vector3 fwd, up;
            Quaternion rotRef = Quaternion.identity;

            if (Core.Target.Target == null && (reference == AttitudeReference.TARGET || reference == AttitudeReference.TARGET_ORIENTATION ||
                                               reference == AttitudeReference.RELATIVE_VELOCITY))
            {
                attitudeDeactivate();
                return rotRef;
            }

            if ((reference == AttitudeReference.MANEUVER_NODE || reference == AttitudeReference.MANEUVER_NODE_COT) &&
                Vessel.patchedConicSolver.maneuverNodes.Count == 0)
            {
                attitudeDeactivate();
                return rotRef;
            }

            Vector3d thrustForward = VesselState.thrustForward;

            // the off-axis thrust modifications get into a fight with the differential throttle so do not use them when diffthrottle is used
            if (Core.Thrust.DifferentialThrottle)
                thrustForward = VesselState.forward;

            switch (reference)
            {
                case AttitudeReference.INERTIAL_COT:
                    rotRef = Quaternion.FromToRotation(thrustForward, VesselState.forward);
                    break;
                case AttitudeReference.ORBIT:
                    rotRef = Quaternion.LookRotation(VesselState.orbitalVelocity.normalized, VesselState.up);
                    break;
                case AttitudeReference.ORBIT_HORIZONTAL:
                    rotRef = Quaternion.LookRotation(Vector3d.Exclude(VesselState.up, VesselState.orbitalVelocity.normalized), VesselState.up);
                    break;
                case AttitudeReference.SURFACE_NORTH:
                    rotRef = VesselState.rotationSurface;
                    break;
                case AttitudeReference.SURFACE_NORTH_COT:
                    rotRef = VesselState.rotationSurface;
                    rotRef = Quaternion.FromToRotation(thrustForward, VesselState.forward) * rotRef;
                    break;
                case AttitudeReference.SURFACE_VELOCITY:
                    rotRef = Quaternion.LookRotation(VesselState.surfaceVelocity.normalized, VesselState.up);
                    break;
                case AttitudeReference.TARGET:
                    fwd = (Core.Target.Position - Vessel.GetTransform().position).normalized;
                    up  = Vector3d.Cross(fwd, VesselState.normalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    break;
                case AttitudeReference.RELATIVE_VELOCITY:
                    fwd = Core.Target.RelativeVelocity.normalized;
                    up  = Vector3d.Cross(fwd, VesselState.normalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    break;
                case AttitudeReference.TARGET_ORIENTATION:
                    Transform targetTransform = Core.Target.Transform;
                    Vector3 targetUp = targetTransform.up;
                    rotRef = Core.Target.CanAlign
                        ? Quaternion.LookRotation(targetTransform.forward, targetUp)
                        : Quaternion.LookRotation(targetUp, targetTransform.right);
                    break;
                case AttitudeReference.MANEUVER_NODE:
                    fwd = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit);
                    up  = Vector3d.Cross(fwd, VesselState.normalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    break;
                case AttitudeReference.MANEUVER_NODE_COT:
                    fwd = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit);
                    up  = Vector3d.Cross(fwd, VesselState.normalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    rotRef = Quaternion.FromToRotation(thrustForward, VesselState.forward) * rotRef;
                    break;
                case AttitudeReference.SUN:
                    Orbit baseOrbit = Vessel.mainBody == Planetarium.fetch.Sun ? Vessel.orbit : Orbit.TopParentOrbit();
                    up     = VesselState.CoM - Planetarium.fetch.Sun.transform.position;
                    fwd    = Vector3d.Cross(-baseOrbit.GetOrbitNormal().xzy.normalized, up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    break;
                case AttitudeReference.SURFACE_HORIZONTAL:
                    rotRef = Quaternion.LookRotation(Vector3d.Exclude(VesselState.up, VesselState.surfaceVelocity.normalized), VesselState.up);
                    break;
            }

            return rotRef;
        }

        private Vector3d attitudeWorldToReference(Vector3d vector, AttitudeReference reference) =>
            Quaternion.Inverse(attitudeGetReferenceRotation(reference)) * vector;

        private Vector3d attitudeReferenceToWorld(Vector3d vector, AttitudeReference reference) => attitudeGetReferenceRotation(reference) * vector;

        public void attitudeTo(Quaternion attitude, AttitudeReference reference, object controller, bool AxisCtrlPitch = true,
            bool AxisCtrlYaw = true, bool AxisCtrlRoll = true)
        {
            Users.Add(controller);
            attitudeReference = reference;
            attitudeTarget    = attitude;
            SetAxisControl(AxisCtrlPitch, AxisCtrlYaw, AxisCtrlRoll);
        }

        public void attitudeTo(Vector3d direction, AttitudeReference reference, object controller)
        {
            //double ang_diff = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, attitudeGetReferenceRotation(reference) * direction));

            Vector3 up, dir = direction;

            if (!Enabled)
                up = attitudeWorldToReference(-Vessel.GetTransform().forward, reference);
            else
                up = attitudeWorldToReference(attitudeReferenceToWorld(attitudeTarget * Vector3d.up, reference), reference);
            Vector3.OrthoNormalize(ref dir, ref up);
            attitudeTo(Quaternion.LookRotation(dir, up), reference, controller);
            SetAxisControl(true, true, false);
        }

        public void attitudeTo(double heading, double pitch, double roll, object controller, bool AxisCtrlPitch = true, bool AxisCtrlYaw = true,
            bool AxisCtrlRoll = true, bool fixCOT = false)
        {
            Quaternion attitude = Quaternion.AngleAxis((float)heading, Vector3.up) * Quaternion.AngleAxis(-(float)pitch, Vector3.right) *
                                  Quaternion.AngleAxis(-(float)roll, Vector3.forward);
            AttitudeReference reference = fixCOT ? AttitudeReference.SURFACE_NORTH_COT : AttitudeReference.SURFACE_NORTH;
            attitudeTo(attitude, reference, controller, AxisCtrlPitch,
                AxisCtrlYaw, AxisCtrlRoll);
        }

        public void attitudeDeactivate()
        {
            Users.Clear();
            attitudeChanged = true;
        }

        //angle in degrees between the vessel's current pointing direction and the attitude target, ignoring roll
        public double attitudeAngleFromTarget() =>
            Enabled
                ? Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, VesselState.forward))
                : 0;

        public Vector3d targetAttitude()
        {
            if (Enabled)
                return attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward;

            return Vector3d.zero;
        }

        public override void OnFixedUpdate()
        {
            steeringError.Value = attitudeError = attitudeAngleFromTarget();

            if (useSAS)
                return;

            torque = VesselState.torqueAvailable;
            if (Core.Thrust.DifferentialThrottle &&
                Core.Thrust.DifferentialThrottleSuccess == MechJebModuleThrustController.DifferentialThrottleStatus.SUCCESS)
                torque += VesselState.torqueDiffThrottle * Vessel.ctrlState.mainThrottle / 2.0;

            // Inertia is a bad name. It's the "angular distance to stop"
            inertia = 0.5 * Vector3d.Scale(
                VesselState.angularMomentum.Sign(),
                Vector3d.Scale(
                    Vector3d.Scale(VesselState.angularMomentum, VesselState.angularMomentum),
                    Vector3d.Scale(torque, VesselState.MoI).InvertNoNaN()
                )
            );
            Controller.OnFixedUpdate();
        }

        public override void OnUpdate()
        {
            if (attitudeChanged)
            {
                if (attitudeReference != AttitudeReference.INERTIAL && attitudeReference != AttitudeReference.INERTIAL_COT) attitudeKILLROT = false;

                Controller.Reset();

                attitudeChanged = false;
            }

            Controller.OnUpdate();
        }

        public override void Drive(FlightCtrlState s)
        {
            // Direction we want to be facing
            RequestedAttitude = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget;

            if (useSAS)
            {
                // TODO : This most likely require some love to use all the new SAS magic

                RequestedAttitude = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Quaternion.Euler(90, 0, 0);
                if (!Part.vessel.ActionGroups[KSPActionGroup.SAS])
                {
                    Part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                    Part.vessel.Autopilot.SAS.SetTargetOrientation(RequestedAttitude * Vector3.up, false);
                    lastSAS = RequestedAttitude;
                }
                else if (Quaternion.Angle(lastSAS, RequestedAttitude) > 10)
                {
                    Part.vessel.Autopilot.SAS.SetTargetOrientation(RequestedAttitude * Vector3.up, false);
                    lastSAS = RequestedAttitude;
                }
                else
                {
                    Part.vessel.Autopilot.SAS.SetTargetOrientation(RequestedAttitude * Vector3.up, true);
                }

                Core.Thrust.DifferentialThrottleDemandedTorque = Vector3d.zero;
            }
            else
            {
                Controller.DrivePre(s, out Vector3d act, out Vector3d deltaEuler);

                act.Scale(ActuationControl);

                SetFlightCtrlState(act, deltaEuler, s, 1);

                act = new Vector3d(s.pitch, s.roll, s.yaw);

                // Feed the control torque to the differential throttle
                if (Core.Thrust.DifferentialThrottleSuccess == MechJebModuleThrustController.DifferentialThrottleStatus.SUCCESS)
                    Core.Thrust.DifferentialThrottleDemandedTorque =
                        -Vector3d.Scale(act, VesselState.torqueDiffThrottle * Vessel.ctrlState.mainThrottle);
            }
        }

        private void SetFlightCtrlState(Vector3d act, Vector3d deltaEuler, FlightCtrlState s, float drive_limit)
        {
            bool userCommandingPitch = !Mathfx.Approx(s.pitch, s.pitchTrim, 0.1F);
            bool userCommandingYaw = !Mathfx.Approx(s.yaw, s.yawTrim, 0.1F);
            bool userCommandingRoll = !Mathfx.Approx(s.roll, s.rollTrim, 0.1F);

            // Disable the new SAS so it won't interfere. But enable it while in timewarp for compatibility with PersistentRotation
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH || TimeWarp.CurrentRateIndex == 0)
                Part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);

            if (attitudeKILLROT)
                if (lastReferencePart != Vessel.GetReferenceTransformPart() || userCommandingPitch || userCommandingYaw || userCommandingRoll)
                {
                    attitudeTo(Quaternion.LookRotation(Vessel.GetTransform().up, -Vessel.GetTransform().forward), AttitudeReference.INERTIAL, null);
                    lastReferencePart = Vessel.GetReferenceTransformPart();
                }

            if (userCommandingPitch)
                Controller.Reset(0);

            if (userCommandingRoll)
                Controller.Reset(1);

            if (userCommandingYaw)
                Controller.Reset(2);

            if (!userCommandingRoll)
                if (!double.IsNaN(act.y))
                    s.roll = Mathf.Clamp((float)act.y, -drive_limit, drive_limit);

            if (!userCommandingPitch && !userCommandingYaw)
            {
                if (!double.IsNaN(act.x)) s.pitch = Mathf.Clamp((float)act.x, -drive_limit, drive_limit);
                if (!double.IsNaN(act.z)) s.yaw   = Mathf.Clamp((float)act.z, -drive_limit, drive_limit);
            }

            // RCS and SAS control:
            Vector3d absErr; // Absolute error (exag º)
            absErr.x = Math.Abs(deltaEuler.x);
            absErr.y = Math.Abs(deltaEuler.y);
            absErr.z = Math.Abs(deltaEuler.z);

            if (absErr.x < 0.4 && absErr.y < 0.4 && absErr.z < 0.4)
            {
                if (timeCount < 50)
                {
                    timeCount++;
                }
                else
                {
                    if (RCS_auto)
                        if (attitudeRCScontrol && Core.RCS.Users.Count == 0)
                            Part.vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
                }
            }
            else if (absErr.x > 1.0 || absErr.y > 1.0 || absErr.z > 1.0)
            {
                timeCount = 0;
                if (RCS_auto && (absErr.x > 3.0 || absErr.y > 3.0 || absErr.z > 3.0) &&
                    Core.Thrust.Limiter != MechJebModuleThrustController.LimitMode.UNSTABLE_IGNITION)
                    if (attitudeRCScontrol)
                        Part.vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
            }
        } // end of SetFlightCtrlState
    }
}
