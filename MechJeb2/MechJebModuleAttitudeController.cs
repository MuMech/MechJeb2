using System;
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
        MANEUVER_NODE,     //forward = next maneuver node direction, up = tbd
        SUN,               //forward = orbit velocity of the parent body orbiting the sun, up = radial plus of that orbit
        SURFACE_HORIZONTAL,//forward = surface velocity horizontal component, up = surface normal
    }

    public class MechJebModuleAttitudeController : ComputerModule
    {
        public PIDControllerV3 pid;
        public Vector3d lastAct = Vector3d.zero;
        public Vector3d pidAction;  //info
        public Vector3d error;  //info
        protected float timeCount = 0;
        protected Part lastReferencePart;

        public bool RCS_auto = false;
        public bool attitudeRCScontrol = true;

        [Persistent(pass = (int)Pass.Global)]
        public bool Tf_autoTune = true;

        [Persistent(pass = (int)Pass.Global)]
        public double Tf = 0; // not used any more but kept so it loads from old configs
        public Vector3d TfV = new Vector3d(0.3, 0.3, 0.3);
        [Persistent(pass = (int)Pass.Global)]
        private Vector3 TfVec = new Vector3(0.3f, 0.3f, 0.3f);  // use the serialize since Vector3d does not
        [Persistent(pass = (int)Pass.Global)]
        public double TfMin = 0.1;
        [Persistent(pass = (int)Pass.Global)]
        public double TfMax = 0.5;
        [Persistent(pass = (int)Pass.Global)]
        public double kpFactor = 3;
        [Persistent(pass = (int)Pass.Global)]
        public double kiFactor = 6;
        [Persistent(pass = (int)Pass.Global)]
        public double kdFactor = 0.5;

        [Persistent(pass = (int)Pass.Global)]
        [ValueInfoItem("Steering error", InfoItem.Category.Vessel, format = "F1", units = "º")]
        public MovingAverage steeringError = new MovingAverage();

        [Persistent(pass = (int)Pass.Global)]
        public static bool useCoMVelocity = true;

        public bool attitudeKILLROT = false;

        protected bool attitudeChanged = false;

        protected AttitudeReference _attitudeReference = AttitudeReference.INERTIAL;

        protected Vector3d _axisControl = Vector3d.one;

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
                    AxisControl(true, true, true);
                    attitudeChanged = true;
                }
                _attitudeTarget = value;
            }
        }

        protected Quaternion _requestedAttitude = Quaternion.identity;
        public Quaternion RequestedAttitude
        {
            get { return _requestedAttitude; }
        }

        public bool attitudeRollMatters
        {
            get
            {
                return _axisControl.z > 0;
            }
        }

        public Vector3d AxisState
        {
            get { return new Vector3d(_axisControl.x, _axisControl.y, _axisControl.z); }
        }


        [Persistent(pass = (int)Pass.Global | (int)Pass.Type), ToggleInfoItem("Use stock SAS", InfoItem.Category.Vessel)]
        public bool useSAS = false;

        protected Quaternion lastSAS = new Quaternion();

        public double attitudeError;
        
        public Vector3d torque;
        public Vector3d inertia;

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
            pid = new PIDControllerV3(Vector3d.zero, Vector3d.zero, Vector3d.zero, 1, -1);
            setPIDParameters();
            lastAct = Vector3d.zero;
            base.OnStart(state);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            if (Tf > 0)
            {
                TfV = new Vector3d(Tf, Tf, Tf);
                Tf = 0;
            }
            else
            {
                TfV = TfVec;
            }
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            TfVec = TfV;
            base.OnSave(local, type, global);
        }

        public void tuneTf(Vector3d torque)
        {
            Vector3d ratio = new Vector3d(
                torque.x != 0 ? vesselState.MoI.x / torque.x : 0,
                torque.z != 0 ? vesselState.MoI.z / torque.z : 0,   //y <=> z
                torque.y != 0 ? vesselState.MoI.y / torque.y : 0    //z <=> y
                );

            TfV = 0.05 * ratio;

            Vector3d delayFactor = Vector3d.one + 2 * vesselState.torqueReactionSpeed;

            TfV.Scale(delayFactor);


            TfV = TfV.Clamp(2.0 * TimeWarp.fixedDeltaTime, 1.0);
            TfV = TfV.Clamp(TfMin, TfMax);

            //Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
            //Tf = Mathf.Clamp((float)Tf, (float)TfMin, (float)TfMax);
        }

        public void setPIDParameters()
        {
            Vector3d invTf = TfV.Invert();
            pid.Kd = kdFactor * invTf;

            pid.Kp = (1 / (kpFactor * Math.Sqrt(2))) * pid.Kd;
            pid.Kp.Scale(invTf);

            pid.Ki = (1 / (kiFactor * Math.Sqrt(2))) * pid.Kp;
            pid.Ki.Scale(invTf);

            pid.intAccum = pid.intAccum.Clamp(-5, 5);
        }

        public void ResetConfig()
        {
            TfMin = 0.1;
            TfMax = 0.5;
            kpFactor = 3;
            kiFactor = 6;
            kdFactor = 0.5;
        }

        public void AxisControl(bool pitch, bool yaw, bool roll)
        {
            _axisControl.x = pitch ? 1 : 0;
            _axisControl.y = yaw ? 1 : 0;
            _axisControl.z = roll ? 1 : 0;
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
                    rotRef = Quaternion.LookRotation(vesselState.orbitalVelocity.normalized, vesselState.up);
                    break;
                case AttitudeReference.ORBIT_HORIZONTAL:
                    rotRef = Quaternion.LookRotation(Vector3d.Exclude(vesselState.up, vesselState.orbitalVelocity.normalized), vesselState.up);
                    break;
                case AttitudeReference.SURFACE_NORTH:
                    rotRef = vesselState.rotationSurface;
                    break;
                case AttitudeReference.SURFACE_VELOCITY:
                    rotRef = Quaternion.LookRotation(vesselState.surfaceVelocity.normalized, vesselState.up);
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
                    if (core.target.CanAlign)
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
                case AttitudeReference.SUN:
                    Orbit baseOrbit = vessel.mainBody == Planetarium.fetch.Sun ? vessel.orbit : orbit.TopParentOrbit();
                    up = vesselState.CoM - Planetarium.fetch.Sun.transform.position;
                    fwd = Vector3d.Cross(-baseOrbit.GetOrbitNormal().Reorder(132).normalized, up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    break;
                case AttitudeReference.SURFACE_HORIZONTAL:
                    rotRef = Quaternion.LookRotation(Vector3d.Exclude(vesselState.up, vesselState.surfaceVelocity.normalized), vesselState.up);
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
            AxisControl(true, true, true);
            return true;
        }

        public bool attitudeTo(Vector3d direction, AttitudeReference reference, object controller)
        {
            //double ang_diff = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, attitudeGetReferenceRotation(reference) * direction));

            Vector3 up, dir = direction;

            if (!enabled)
            {
                up = attitudeWorldToReference(-vessel.GetTransform().forward, reference);
            }
            else
            {
                up = attitudeWorldToReference(attitudeReferenceToWorld(attitudeTarget * Vector3d.up, attitudeReference), reference);
            }
            Vector3.OrthoNormalize(ref dir, ref up);
            attitudeTo(Quaternion.LookRotation(dir, up), reference, controller);
            AxisControl(true, true, false);
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

            if (useSAS)
                return;

            torque = vesselState.torqueAvailable + vesselState.torqueFromEngine ;
            if (core.thrust.differentialThrottleSuccess)
                torque += vesselState.torqueFromDiffThrottle * vessel.ctrlState.mainThrottle / 2.0;

            inertia = Vector3d.Scale(
                vesselState.angularMomentum.Sign(),
                Vector3d.Scale(
                    Vector3d.Scale(vesselState.angularMomentum, vesselState.angularMomentum),
                    Vector3d.Scale(torque, vesselState.MoI).Invert()
                    )
                );
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
        }

        public override void Drive(FlightCtrlState s)
        {
            if (useSAS)
            {
                _requestedAttitude = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Quaternion.Euler(90, 0, 0);
                if (!part.vessel.ActionGroups[KSPActionGroup.SAS])
                {
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                    part.vessel.Autopilot.SAS.LockHeading(_requestedAttitude);
                    lastSAS = _requestedAttitude;
                }
                else if (Quaternion.Angle(lastSAS, _requestedAttitude) > 10)
                {
                    part.vessel.Autopilot.SAS.LockHeading(_requestedAttitude);
                    lastSAS = _requestedAttitude;
                }
                else
                {
                    part.vessel.Autopilot.SAS.LockHeading(_requestedAttitude, true);
                }

                core.thrust.differentialThrottleDemandedTorque = Vector3d.zero;
            }
            else
            {
                // Direction we want to be facing
                _requestedAttitude = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget;
                Transform vesselTransform = vessel.ReferenceTransform;
                Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vesselTransform.rotation) * _requestedAttitude);

                Vector3d deltaEuler = delta.DeltaEuler();
                
                // ( MoI / available torque ) factor:
                Vector3d NormFactor = Vector3d.Scale(vesselState.MoI, torque.Invert()).Reorder(132);

                // Find out the real shorter way to turn were we wan to.
                // Thanks to HoneyFox
                Vector3d tgtLocalUp = vesselTransform.transform.rotation.Inverse() * _requestedAttitude * Vector3d.forward;
                Vector3d curLocalUp = Vector3d.up;

                double turnAngle = Math.Abs(Vector3d.Angle(curLocalUp, tgtLocalUp));
                Vector2d rotDirection = new Vector2d(tgtLocalUp.x, tgtLocalUp.z);
                rotDirection = rotDirection.normalized * turnAngle / 180.0;

                // And the lowest roll
                // Thanks to Crzyrndm
                Vector3 normVec = Vector3.Cross(_requestedAttitude * Vector3.forward, vesselTransform.up);
                Quaternion targetDeRotated = Quaternion.AngleAxis((float)turnAngle, normVec) * _requestedAttitude;
                float rollError = Vector3.Angle(vesselTransform.right, targetDeRotated * Vector3.right) * Math.Sign(Vector3.Dot(targetDeRotated * Vector3.right, vesselTransform.forward));

                error = new Vector3d(
                    -rotDirection.y * Math.PI,
                    rotDirection.x * Math.PI,
                    rollError * Mathf.Deg2Rad
                    );

                error.Scale(_axisControl);

                Vector3d err = error + inertia.Reorder(132) / 2d;
                err = new Vector3d(
                    Math.Max(-Math.PI, Math.Min(Math.PI, err.x)),
                    Math.Max(-Math.PI, Math.Min(Math.PI, err.y)),
                    Math.Max(-Math.PI, Math.Min(Math.PI, err.z)));

                err.Scale(NormFactor);

                // angular velocity:
                Vector3d omega;
                omega.x = vessel.angularVelocity.x;
                omega.y = vessel.angularVelocity.z; // y <=> z
                omega.z = vessel.angularVelocity.y; // z <=> y
                omega.Scale(NormFactor);

                if (Tf_autoTune)
                    tuneTf(torque);
                setPIDParameters();

                pidAction = pid.Compute(err, omega);

                // low pass filter,  wf = 1/Tf:
                Vector3d act = lastAct;
                act.x += (pidAction.x - lastAct.x) * (1.0 / ((TfV.x / TimeWarp.fixedDeltaTime) + 1.0));
                act.y += (pidAction.y - lastAct.y) * (1.0 / ((TfV.y / TimeWarp.fixedDeltaTime) + 1.0));
                act.z += (pidAction.z - lastAct.z) * (1.0 / ((TfV.z / TimeWarp.fixedDeltaTime) + 1.0));
                lastAct = act;

                SetFlightCtrlState(act, deltaEuler, s, 1);

                act = new Vector3d(s.pitch, s.yaw, s.roll);

                // Feed the control torque to the differential throttle
                if (core.thrust.differentialThrottleSuccess)
                    core.thrust.differentialThrottleDemandedTorque = -Vector3d.Scale(act.xzy, vesselState.torqueFromDiffThrottle * vessel.ctrlState.mainThrottle);
            }
        }

        private void SetFlightCtrlState(Vector3d act, Vector3d deltaEuler, FlightCtrlState s, float drive_limit)
        {
            bool userCommandingPitchYaw = (Mathfx.Approx(s.pitch, s.pitchTrim, 0.1F) ? false : true) || (Mathfx.Approx(s.yaw, s.yawTrim, 0.1F) ? false : true);
            bool userCommandingRoll = (Mathfx.Approx(s.roll, s.rollTrim, 0.1F) ? false : true);

            // Disable the new SAS so it won't interfere. But enable it while in timewarp for compatibility with PersistentRotation
            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH || TimeWarp.CurrentRateIndex == 0)
                part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);


            if (attitudeKILLROT)
            {
                if (lastReferencePart != vessel.GetReferenceTransformPart() || userCommandingPitchYaw || userCommandingRoll)
                {
                    attitudeTo(Quaternion.LookRotation(vessel.GetTransform().up, -vessel.GetTransform().forward), AttitudeReference.INERTIAL, null);
                    lastReferencePart = vessel.GetReferenceTransformPart();
                }
            }
            if (userCommandingPitchYaw || userCommandingRoll)
            {
                pid.Reset();
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
                        if (attitudeRCScontrol && core.rcs.users.Count == 0)
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