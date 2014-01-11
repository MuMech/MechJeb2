using System;
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
        public PIDControllerV2 pid;
        public Vector3d lastAct = Vector3d.zero;
        public Vector3d pidAction;  //info
        protected float timeCount = 0;

        public bool RCS_auto = false;
        public bool attitudeRCScontrol = true;

        [Persistent(pass = (int)Pass.Global)]
        public bool Tf_autoTune = true;

        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public double Tf = 0.3;

        [Persistent(pass = (int)Pass.Global)]
        [ValueInfoItem("Steering error", InfoItem.Category.Vessel, format = "F1", units = "º")]
        public MovingAverage steeringError = new MovingAverage();

        public bool attitudeKILLROT = false;

        protected bool attitudeChanged = false;

        protected AttitudeReference _oldAttitudeReference = AttitudeReference.INERTIAL;
        protected AttitudeReference _attitudeReference = AttitudeReference.INERTIAL;
        
        public override void OnModuleEnabled()
        {
            timeCount = 50;
        }
        
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

        [Persistent(pass = (int)Pass.Global | (int)Pass.Type), ToggleInfoItem("Use stock SAS", InfoItem.Category.Vessel)]
        public bool useSAS = false;

        protected Quaternion lastSAS = new Quaternion();

        public double attitudeError;

        public MechJebModuleAttitudeController(MechJebCore core)
            : base(core)
        {
            priority = 800;
        }

        public override void OnModuleDisabled()
        {
            if (useSAS)
            {
                part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            }
            base.OnModuleDisabled();
        }

        public override void OnStart(PartModule.StartState state)
        {            
            pid = new PIDControllerV2(0, 0, 0, 1, -1);
            setPIDParameters();
            lastAct = Vector3d.zero;
            base.OnStart(state);
        }

        public void tuneTf()
        {
            Vector3d torque = new Vector3d(
                                    vesselState.torqueAvailable.x + vesselState.torqueThrustPYAvailable * vessel.ctrlState.mainThrottle,
                                    vesselState.torqueAvailable.y,
                                    vesselState.torqueAvailable.z + vesselState.torqueThrustPYAvailable * vessel.ctrlState.mainThrottle
                                          );

            Vector3d ratio = new Vector3d(
                                    torque.x != 0 ? vesselState.MoI.x / torque.x : 0,
                                    torque.y != 0 ? vesselState.MoI.y / torque.y : 0,
                                    torque.z != 0 ? vesselState.MoI.z / torque.z : 0
                );

            Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);

            setPIDParameters();
        }

        public void setPIDParameters()
        {
            pid.Kd = 0.53 / Tf;
            pid.Kp = pid.Kd / (3 * Math.Sqrt(2) * Tf);
            pid.Ki = pid.Kp / (12 * Math.Sqrt(2) * Tf);
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
            //double ang_diff = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(reference) * direction, vesselState.forward));
            double ang_diff = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, attitudeGetReferenceRotation(reference) * direction));
            
            Vector3 up, dir = direction;

            // TODO : Fix that so it does not roll when it should not. Current fix is a "hack" that set required roll to 0 if !attitudeRollMatters

            if (!enabled || (ang_diff > 45))
            {
                up = attitudeWorldToReference(-vessel.GetTransform().forward, reference);
            }
            else
            {
                up = attitudeWorldToReference(attitudeReferenceToWorld(attitudeTarget * Vector3d.up, attitudeReference), reference);
            }
            Vector3.OrthoNormalize(ref dir, ref up);
            attitudeTo(Quaternion.LookRotation(dir, up), reference, controller);
            _attitudeRollMatters = false;
            return true;
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
                lastAct = Vector3d.zero;

                attitudeChanged = false;
            }

            if (Tf_autoTune)
                tuneTf();

        }

        public override void Drive(FlightCtrlState s)
        {
            if (useSAS)
            {
                Quaternion target = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Quaternion.Euler(90, 0, 0);
                if (!part.vessel.ActionGroups[KSPActionGroup.SAS])
                {
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                    part.vessel.VesselSAS.LockHeading(target);
                    lastSAS = target;
                }
                else if (Quaternion.Angle(lastSAS, target) > 10)
                {
                    part.vessel.VesselSAS.LockHeading(target);
                    lastSAS = target;
                }
                else
                {
                    part.vessel.VesselSAS.LockHeading(target, true);
                }
            }
            else
            {
                // Direction we want to be facing
                Quaternion target = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget;
                Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * target);

                Vector3d deltaEuler = new Vector3d(
                                                        (delta.eulerAngles.x > 180) ? (delta.eulerAngles.x - 360.0F) : delta.eulerAngles.x,
                                                        -((delta.eulerAngles.y > 180) ? (delta.eulerAngles.y - 360.0F) : delta.eulerAngles.y),
                                                        (delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z
                                                    );

                Vector3d torque = new Vector3d(
                                                        vesselState.torqueAvailable.x + vesselState.torqueThrustPYAvailable * s.mainThrottle,
                                                        vesselState.torqueAvailable.y,
                                                        vesselState.torqueAvailable.z + vesselState.torqueThrustPYAvailable * s.mainThrottle
                                                );

                Vector3d inertia = Vector3d.Scale(
                                                        vesselState.angularMomentum.Sign(),
                                                        Vector3d.Scale(
                                                            Vector3d.Scale(vesselState.angularMomentum, vesselState.angularMomentum),
                                                            Vector3d.Scale(torque, vesselState.MoI).Invert()
                                                        )
                                                    );

                // ( MoI / avaiable torque ) factor:
                Vector3d NormFactor = Vector3d.Scale(vesselState.MoI, torque.Invert()).Reorder(132);
                
                // Find out the real shorter way to turn were we wan to.
                // Thanks to HoneyFox

                Vector3d tgtLocalUp = vessel.ReferenceTransform.transform.rotation.Inverse() * target * Vector3d.forward;
                Vector3d curLocalUp = Vector3d.up;

                double turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
                Vector2d rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
                rotDirection = rotDirection.normalized * turnAngle / 180.0f;

                Vector3d err = new Vector3d(
                                                -rotDirection.y * Math.PI,
                                                rotDirection.x * Math.PI,
                                                attitudeRollMatters?((delta.eulerAngles.z > 180) ? (delta.eulerAngles.z - 360.0F) : delta.eulerAngles.z) * Math.PI / 180.0F : 0F
                                            );

                err += inertia.Reorder(132) / 2;
                err = new Vector3d(Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                                   Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                                   Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));
                err.Scale(NormFactor);

                // angular velocity:
                Vector3d omega;
                omega.x = vessel.angularVelocity.x;
                omega.y = vessel.angularVelocity.z; // y <=> z 
                omega.z = vessel.angularVelocity.y; // z <=> y 
                omega.Scale(NormFactor);

                pidAction = pid.Compute(err, omega);

                // low pass filter,  wf = 1/Tf:
                Vector3d act = lastAct + (pidAction - lastAct) * (1 / ((Tf / TimeWarp.fixedDeltaTime) + 1));
                lastAct = act;

                SetFlightCtrlState(act, deltaEuler, s, 1);
                act = new Vector3d(s.pitch, s.yaw, s.roll);
            }
        }

        private void SetFlightCtrlState(Vector3d act, Vector3d deltaEuler, FlightCtrlState s, float drive_limit)
        {
            bool userCommandingPitchYaw = (Mathfx.Approx(s.pitch, s.pitchTrim, 0.1F) ? false : true) || (Mathfx.Approx(s.yaw, s.yawTrim, 0.1F) ? false : true);
            bool userCommandingRoll = (Mathfx.Approx(s.roll, s.rollTrim, 0.1F) ? false : true);

            // Disable the new SAS so it won't interfere. 
            // Todo : enable it when it's a good idea or the user had it enabled before
            part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);

            if (userCommandingPitchYaw || userCommandingRoll)
            {
                pid.Reset();
                
                if (attitudeKILLROT)
                {
                    attitudeTo(Quaternion.LookRotation(vessel.GetTransform().up, -vessel.GetTransform().forward), AttitudeReference.INERTIAL, null);
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
            }

            if (!userCommandingPitchYaw)
            {
                if (!double.IsNaN(act.x)) s.pitch = Mathf.Clamp((float)(act.x), -drive_limit, drive_limit);
                if (!double.IsNaN(act.y)) s.yaw = Mathf.Clamp((float)(act.y), -drive_limit, drive_limit);
            }

            // RCS and SAS control:
            Vector3d absErr;            // Absolute error (exag º)
            absErr.x = Math.Abs(deltaEuler.x);
            absErr.y = Math.Abs(deltaEuler.y);
            absErr.z = Math.Abs(deltaEuler.z);

            if ((absErr.x < 0.4) && (absErr.y < 0.4) && (absErr.z < 0.4))
            {
                if (timeCount < 50)
                {
                    timeCount++;
                }
                else
                {
                    if (RCS_auto)
                    {
                        if (attitudeRCScontrol && core.rcs.users.Count==0)
                        {
                            part.vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
                        }
                    }
                }
            }
            else if ((absErr.x > 1.0) || (absErr.y > 1.0) || (absErr.z > 1.0))
            {
                timeCount = 0;
                if (RCS_auto && ((absErr.x > 3.0) || (absErr.y > 3.0) || (absErr.z > 3.0)))
                {
                    if (attitudeRCScontrol)
                    {
                        part.vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                    }
                }
            }
        } // end of SetFlightCtrlState 
    }
}
