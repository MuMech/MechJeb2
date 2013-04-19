﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public enum AttitudeReference
    {
        INERTIAL,          //world coordinate system.
        ORBIT,             //forward = prograde, left = normal plus, up = radial plus
        ORBIT_HORIZONTAL,  //forward = surface projection of orbit velocity, up = surface normal
        SURFACE_NORTH,     //forward = north, left = west, up = surface normal
        SURFACE_VELOCITY,  //forward = surface frame vessel velocity, up = perpendicular component of surface normal
        TARGET,            //forward = toward target, up = perpendicular component of vessel heading
        RELATIVE_VELOCITY, //forward = toward relative velocity direction, up = tbd
        TARGET_ORIENTATION,//forward = direction target is facing, up = target up
        MANEUVER_NODE      //forward = next maneuver node direction, up = tbd
    }

    public class MechJebModuleAttitudeController : ComputerModule
    {
        public double stress;

        public PIDControllerV pid;
        public Vector3d lastAct = Vector3d.zero;
        private double lastResetRoll = 0;

        [ToggleInfoItem("Use SAS if available", InfoItem.Category.Vessel), Persistent(pass = (int)Pass.Local)]
        public bool useSAS = true;

        [Persistent(pass = (int)Pass.Global)]
        public double Kp = 10000;
        [Persistent(pass = (int)Pass.Global)]
        public double Ki = 0;
        [Persistent(pass = (int)Pass.Global)]
        public double Kd = 800;
        [Persistent(pass = (int)Pass.Global)]
        public double Ki_limit = 1000000;
        [Persistent(pass = (int)Pass.Global)]
        public double Tf = 0.1;
        [Persistent(pass = (int)Pass.Global)]
        public double drive_factor = 100000;
        [ValueInfoItem("Steering error", InfoItem.Category.Vessel, format = "F1", units = "º")]
        public MovingAverage steeringError = new MovingAverage();

        public bool attitudeKILLROT = false;

        protected bool attitudeChanged = false;

        protected AttitudeReference _oldAttitudeReference = AttitudeReference.INERTIAL;
        protected AttitudeReference _attitudeReference = AttitudeReference.INERTIAL;
        public AttitudeReference attitudeReference
        {
            get
            {
                return _attitudeReference;
            }
            set
            {
                if (_attitudeReference != value)
                {
                    _oldAttitudeReference = _attitudeReference;
                    _attitudeReference = value;
                    attitudeChanged = true;
                }
            }
        }

        protected Quaternion _oldAttitudeTarget = Quaternion.identity;
        protected Quaternion _lastAttitudeTarget = Quaternion.identity;
        protected Quaternion _attitudeTarget = Quaternion.identity;
        public Quaternion attitudeTarget
        {
            get
            {
                return _attitudeTarget;
            }
            set
            {
                if (Math.Abs(Vector3d.Angle(_lastAttitudeTarget * Vector3d.forward, value * Vector3d.forward)) > 10)
                {
                    _oldAttitudeTarget = _attitudeTarget;
                    _lastAttitudeTarget = value;
                    attitudeChanged = true;
                }
                _attitudeTarget = value;
            }
        }

        protected bool _attitudeRollMatters = false;
        public bool attitudeRollMatters
        {
            get
            {
                return _attitudeRollMatters;
            }
        }

        public double attitudeError;

        public MechJebModuleAttitudeController(MechJebCore core)
            : base(core)
        {
            priority = 800;
        }

        public override void OnStart(PartModule.StartState state)
        {
            pid = new PIDControllerV(Kp, Ki, Kd, Ki_limit, -Ki_limit);

            base.OnStart(state);
        }

        public Quaternion attitudeGetReferenceRotation(AttitudeReference reference)
        {
            Vector3 fwd, up;
            Quaternion rotRef = Quaternion.identity;

            if (core.target.Target == null && (reference == AttitudeReference.TARGET || reference == AttitudeReference.TARGET_ORIENTATION || reference == AttitudeReference.RELATIVE_VELOCITY))
            {
                attitudeDeactivate();
                return rotRef;
            }

            if ((reference == AttitudeReference.MANEUVER_NODE) && (vessel.patchedConicSolver.maneuverNodes.Count == 0))
            {
                attitudeDeactivate();
                return rotRef;
            }

            switch (reference)
            {
                case AttitudeReference.ORBIT:
                    rotRef = Quaternion.LookRotation(vesselState.velocityVesselOrbitUnit, vesselState.up);
                    break;
                case AttitudeReference.ORBIT_HORIZONTAL:
                    rotRef = Quaternion.LookRotation(Vector3d.Exclude(vesselState.up, vesselState.velocityVesselOrbitUnit), vesselState.up);
                    break;
                case AttitudeReference.SURFACE_NORTH:
                    rotRef = vesselState.rotationSurface;
                    break;
                case AttitudeReference.SURFACE_VELOCITY:
                    rotRef = Quaternion.LookRotation(vesselState.velocityVesselSurfaceUnit, vesselState.up);
                    break;
                case AttitudeReference.TARGET:
                    fwd = (core.target.Position - vessel.GetTransform().position).normalized;
                    up = Vector3d.Cross(fwd, vesselState.normalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    break;
                case AttitudeReference.RELATIVE_VELOCITY:
                    fwd = core.target.RelativeVelocity.normalized;
                    up = Vector3d.Cross(fwd, vesselState.normalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    break;
                case AttitudeReference.TARGET_ORIENTATION:
                    Transform targetTransform = core.target.Transform;
                    if (core.target.Target is ModuleDockingNode)
                    {
                        rotRef = Quaternion.LookRotation(targetTransform.forward, targetTransform.up);
                    }
                    else
                    {
                        rotRef = Quaternion.LookRotation(targetTransform.up, targetTransform.right);
                    }
                    break;
                case AttitudeReference.MANEUVER_NODE:
                    fwd = vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(orbit);
                    up = Vector3d.Cross(fwd, vesselState.normalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    break;
            }
            return rotRef;
        }

        public Vector3d attitudeWorldToReference(Vector3d vector, AttitudeReference reference)
        {
            return Quaternion.Inverse(attitudeGetReferenceRotation(reference)) * vector;
        }

        public Vector3d attitudeReferenceToWorld(Vector3d vector, AttitudeReference reference)
        {
            return attitudeGetReferenceRotation(reference) * vector;
        }

        public bool attitudeTo(Quaternion attitude, AttitudeReference reference, object controller)
        {
            users.Add(controller);
            attitudeReference = reference;
            attitudeTarget = attitude;
            _attitudeRollMatters = true;

            return true;
        }

        public bool attitudeTo(Vector3d direction, AttitudeReference reference, object controller)
        {
            bool ok = false;
            double ang_diff = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, attitudeGetReferenceRotation(reference) * direction));
            Vector3 up, dir = direction;

            if (!enabled || (ang_diff > 45))
            {
                up = attitudeWorldToReference(-vessel.GetTransform().forward, reference);
            }
            else
            {
                up = attitudeWorldToReference(attitudeReferenceToWorld(attitudeTarget * Vector3d.up, attitudeReference), reference);
            }
            Vector3.OrthoNormalize(ref dir, ref up);
            ok = attitudeTo(Quaternion.LookRotation(dir, up), reference, controller);
            if (ok)
            {
                _attitudeRollMatters = false;
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool attitudeTo(double heading, double pitch, double roll, object controller)
        {
            Quaternion attitude = Quaternion.AngleAxis((float)heading, Vector3.up) * Quaternion.AngleAxis(-(float)pitch, Vector3.right) * Quaternion.AngleAxis(-(float)roll, Vector3.forward);
            return attitudeTo(attitude, AttitudeReference.SURFACE_NORTH, controller);
        }

        public bool attitudeDeactivate()
        {
            users.Clear();
            attitudeChanged = true;

            return true;
        }

        //angle in degrees between the vessel's current pointing direction and the attitude target, ignoring roll
        public double attitudeAngleFromTarget()
        {
            return enabled ? Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, vesselState.forward)) : 0;
        }

        public override void OnFixedUpdate()
        {
            steeringError.value = attitudeError = attitudeAngleFromTarget();
        }

        public override void OnUpdate()
        {
            if (attitudeChanged)
            {
                if (attitudeReference != AttitudeReference.INERTIAL)
                {
                    attitudeKILLROT = false;
                }
                pid.Reset();

                attitudeChanged = false;
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            // Used in the killRot activation calculation and drive_limit calculation
            double precision = Math.Max(0.5, Math.Min(10.0, (vesselState.torquePYAvailable + vesselState.torqueThrustPYAvailable * s.mainThrottle) * 20.0 / vesselState.MoI.magnitude));

            // Reset the PID controller during roll to keep pitch and yaw errors
            // from accumulating on the wrong axis.
            double rollDelta = Mathf.Abs((float)(vesselState.vesselRoll - lastResetRoll));
            if (rollDelta > 180) rollDelta = 360 - rollDelta;
            if (rollDelta > 5)
            {
                pid.Reset();
                lastResetRoll = vesselState.vesselRoll;
            }

            // Direction we want to be facing
            Quaternion target = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget;
            Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * target);

            Vector3d deltaEuler = new Vector3d(
                                                    (delta.eulerAngles.x > 180) ? (delta.eulerAngles.x - 360.0F) : delta.eulerAngles.x,
                                                    -((delta.eulerAngles.y > 180) ? (delta.eulerAngles.y - 360.0F) : delta.eulerAngles.y),
                                                    (delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z
                                                );

            Vector3d torque = new Vector3d(
                                                    vesselState.torquePYAvailable + vesselState.torqueThrustPYAvailable * s.mainThrottle,
                                                    vesselState.torqueRAvailable,
                                                    vesselState.torquePYAvailable + vesselState.torqueThrustPYAvailable * s.mainThrottle
                                            );

            Vector3d inertia = Vector3d.Scale(
                                                    vesselState.angularMomentum.Sign(),
                                                    Vector3d.Scale(
                                                        Vector3d.Scale(vesselState.angularMomentum, vesselState.angularMomentum),
                                                        Vector3d.Scale(torque, vesselState.MoI).Invert()
                                                    )
                                                );

            Vector3d err = deltaEuler * Math.PI / 180.0F;
            err += inertia.Reorder(132);
            err.Scale(Vector3d.Scale(vesselState.MoI, torque.Invert()));

            Vector3d act = pid.Compute(err);

            float drive_limit = Mathf.Clamp01((float)(err.magnitude * drive_factor / precision));

            act.x = Mathf.Clamp((float)act.x, drive_limit * -1, drive_limit);
            act.y = Mathf.Clamp((float)act.y, drive_limit * -1, drive_limit);
            act.z = Mathf.Clamp((float)act.z, drive_limit * -1, drive_limit);

            act = lastAct + (act - lastAct) * (TimeWarp.fixedDeltaTime / Tf);

            SetFlightCtrlState(act, deltaEuler, s, precision, drive_limit);

            act = new Vector3d(s.pitch, s.yaw, s.roll);
            lastAct = act;

            stress = Math.Abs(act.x) + Math.Abs(act.y) + Math.Abs(act.z);
        }

        private void SetFlightCtrlState(Vector3d act, Vector3d deltaEuler, FlightCtrlState s, double precision, float drive_limit)
        {
            bool userCommandingPitchYaw = (Mathfx.Approx(s.pitch, s.pitchTrim, 0.1F) ? false : true) || (Mathfx.Approx(s.yaw, s.yawTrim, 0.1F) ? false : true);
            bool userCommandingRoll = (Mathfx.Approx(s.roll, s.rollTrim, 0.1F) ? false : true);

            if (userCommandingPitchYaw || userCommandingRoll)
            {
                pid.Reset();
                if (useSAS)
                {
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
                }
                if (attitudeKILLROT)
                {
                    attitudeTo(Quaternion.LookRotation(vessel.GetTransform().up, -vessel.GetTransform().forward), AttitudeReference.INERTIAL, null);
                }
            }
            else
            {
                double int_error = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, vesselState.forward));
                if (useSAS)
                {
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, (int_error < precision / 10.0) && (Math.Abs(deltaEuler.y) < Math.Min(0.5, precision / 2.0)));
                }
            }

            if (!attitudeRollMatters)
            {
                attitudeTo(Quaternion.LookRotation(attitudeTarget * Vector3d.forward, attitudeWorldToReference(-vessel.GetTransform().forward, attitudeReference)), attitudeReference, null);
                _attitudeRollMatters = false;
            }

            if (!userCommandingRoll)
            {
                if (!double.IsNaN(act.z)) s.roll = Mathf.Clamp((float)(act.z), -drive_limit, drive_limit);
                if (Math.Abs(s.roll) < 0.05)
                {
                    s.roll = 0;
                }
            }

            if (!userCommandingPitchYaw)
            {
                if (!double.IsNaN(act.x)) s.pitch = Mathf.Clamp((float)(act.x), -drive_limit, drive_limit);
                if (!double.IsNaN(act.y)) s.yaw = Mathf.Clamp((float)(act.y), -drive_limit, drive_limit);
                if (Math.Abs(s.pitch) < 0.05)
                {
                    s.pitch = 0;
                }
                if (Math.Abs(s.yaw) < 0.05)
                {
                    s.yaw = 0;
                }
            }
        }
    }
}
