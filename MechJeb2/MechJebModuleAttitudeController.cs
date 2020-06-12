using System;
using System.Collections.Generic;
using MuMech.AttitudeControllers;
using UnityEngine;

namespace MuMech
{
    public enum AttitudeReference
    {
        INERTIAL,          //world coordinate system.
        INERTIAL_COT,      //world coordinate system fixed for CoT offset.
        ORBIT,             //forward = prograde, left = normal plus, up = radial plus
        ORBIT_HORIZONTAL,  //forward = surface projection of orbit velocity, up = surface normal
        SURFACE_NORTH,     //forward = north, left = west, up = surface normal
        SURFACE_NORTH_COT, //forward = north, left = west, up = surface normal, fixed for CoT offset
        SURFACE_VELOCITY,  //forward = surface frame vessel velocity, up = perpendicular component of surface normal
        TARGET,            //forward = toward target, up = perpendicular component of vessel heading
        RELATIVE_VELOCITY, //forward = toward relative velocity direction, up = tbd
        TARGET_ORIENTATION,//forward = direction target is facing, up = target up
        MANEUVER_NODE,     //forward = next maneuver node direction, up = tbd
        MANEUVER_NODE_COT, //forward = next maneuver node direction, up = tbd, fixed for CoT offset
        SUN,               //forward = orbit velocity of the parent body orbiting the sun, up = radial plus of that orbit
        SURFACE_HORIZONTAL,//forward = surface velocity horizontal component, up = surface normal
    }

    public class MechJebModuleAttitudeController : ComputerModule
    {
        protected float timeCount = 0;
        protected Part lastReferencePart;

        public bool RCS_auto = false;
        public bool attitudeRCScontrol = true;


        [Persistent(pass = (int)Pass.Global)]
        [ValueInfoItem("#MechJeb_SteeringError", InfoItem.Category.Vessel, format = "F1", units = "º")]//Steering error
        public MovingAverage steeringError = new MovingAverage();

        public bool attitudeKILLROT = false;

        protected bool attitudeChanged = false;

        protected AttitudeReference _attitudeReference = AttitudeReference.INERTIAL;

        protected Vector3d _axisControl = Vector3d.one;

        public BaseAttitudeController Controller { get; protected set; }
        public List<BaseAttitudeController> controllers = new List<BaseAttitudeController>();

        [Persistent(pass = (int)Pass.Global)]
        public int activeController = 2;


        public void SetActiveController(int i)
        {
            activeController = i;
            Controller = controllers[activeController];
            Controller.OnStart();
        }

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

        protected Quaternion _attitudeTarget = Quaternion.identity;
        public Quaternion attitudeTarget
        {
            get
            {
                return _attitudeTarget;
            }
            set
            {
                if (Math.Abs(Vector3d.Angle(_attitudeTarget * Vector3d.forward, value * Vector3d.forward)) > 10)
                {
                    AxisControl(true, true, true);
                    attitudeChanged = true;
                }
                _attitudeTarget = value;
            }
        }

        private Quaternion _requestedAttitude = Quaternion.identity;
        public Quaternion RequestedAttitude
        {
            get { return _requestedAttitude; }
        }

        public bool attitudeRollMatters
        {
            get
            {
                return _axisControl.y > 0;
            }
        }

        public Vector3d AxisState
        {
            get { return new Vector3d(_axisControl.x, _axisControl.y, _axisControl.z); }
        }


        //[Persistent(pass = (int)Pass.Global | (int)Pass.Type), ToggleInfoItem("Use stock SAS", InfoItem.Category.Vessel)]
        // Disable the use of Stock SAS for now
        public bool useSAS = false;

        protected Quaternion lastSAS = new Quaternion();

        public double attitudeError;

        public Vector3d torque;
        public Vector3d inertia;

        public MechJebModuleAttitudeController(MechJebCore core)
            : base(core)
        {
            priority = 800;

            controllers.Add(new MJAttitudeController(this));
            controllers.Add(new KosAttitudeController(this));
            controllers.Add(new HybridController(this));


            Controller = new HybridController(this);

        }

        public override void OnModuleDisabled()
        {
            if (useSAS)
            {
                part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
            }
            Controller.OnModuleDisabled();
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            foreach (BaseAttitudeController attitudeController in controllers)
            {
                attitudeController.OnLoad(local, type, global);
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            SetActiveController(activeController);
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);
            foreach (BaseAttitudeController attitudeController in controllers)
            {
                attitudeController.OnSave(local, type, global);
            }
        }

        public void AxisControl(bool pitch, bool yaw, bool roll)
        {
            _axisControl.x = pitch ? 1 : 0;
            _axisControl.y = roll ? 1 : 0;
            _axisControl.z = yaw ? 1 : 0;
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
                case AttitudeReference.INERTIAL_COT:
                    rotRef = Quaternion.FromToRotation(vesselState.thrustForward, vesselState.forward) * rotRef;
                    break;
                case AttitudeReference.ORBIT:
                    rotRef = Quaternion.LookRotation(vesselState.orbitalVelocity.normalized, vesselState.up);
                    break;
                case AttitudeReference.ORBIT_HORIZONTAL:
                    rotRef = Quaternion.LookRotation(Vector3d.Exclude(vesselState.up, vesselState.orbitalVelocity.normalized), vesselState.up);
                    break;
                case AttitudeReference.SURFACE_NORTH:
                    rotRef = vesselState.rotationSurface;
                    break;
                case AttitudeReference.SURFACE_NORTH_COT:
                    rotRef = vesselState.rotationSurface;
                    rotRef = Quaternion.FromToRotation(vesselState.thrustForward, vesselState.forward) * rotRef;
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
                case AttitudeReference.MANEUVER_NODE_COT:
                    fwd = vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(orbit);
                    up = Vector3d.Cross(fwd, vesselState.normalPlus);
                    Vector3.OrthoNormalize(ref fwd, ref up);
                    rotRef = Quaternion.LookRotation(fwd, up);
                    rotRef = Quaternion.FromToRotation(vesselState.thrustForward, vesselState.forward) * rotRef;
                    break;
                case AttitudeReference.SUN:
                    Orbit baseOrbit = vessel.mainBody == Planetarium.fetch.Sun ? vessel.orbit : orbit.TopParentOrbit();
                    up = vesselState.CoM - Planetarium.fetch.Sun.transform.position;
                    fwd = Vector3d.Cross(-baseOrbit.GetOrbitNormal().xzy.normalized, up);
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

        public bool attitudeTo(Quaternion attitude, AttitudeReference reference, object controller, bool AxisCtrlPitch=true, bool AxisCtrlYaw=true, bool AxisCtrlRoll=true)
        {
            users.Add(controller);
            attitudeReference = reference;
            attitudeTarget = attitude;
            AxisControl(AxisCtrlPitch, AxisCtrlYaw, AxisCtrlRoll);
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

        public bool attitudeTo(double heading, double pitch, double roll, object controller, bool AxisCtrlPitch=true, bool AxisCtrlYaw=true, bool AxisCtrlRoll=true, bool fixCOT=false)
        {
            Quaternion attitude = Quaternion.AngleAxis((float)heading, Vector3.up) * Quaternion.AngleAxis(-(float)pitch, Vector3.right) * Quaternion.AngleAxis(-(float)roll, Vector3.forward);
            if (fixCOT)
                return attitudeTo(attitude, AttitudeReference.SURFACE_NORTH_COT, controller, AxisCtrlPitch, AxisCtrlYaw, AxisCtrlRoll);
            else
                return attitudeTo(attitude, AttitudeReference.SURFACE_NORTH, controller, AxisCtrlPitch, AxisCtrlYaw, AxisCtrlRoll);
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

            torque = vesselState.torqueAvailable;
            if (core.thrust.differentialThrottleSuccess == MechJebModuleThrustController.DifferentialThrottleStatus.Success)
                torque += vesselState.torqueDiffThrottle * vessel.ctrlState.mainThrottle / 2.0;



            // Inertia is a bad name. It's the "angular distance to stop"
            inertia = 0.5 * Vector3d.Scale(
                vesselState.angularMomentum.Sign(),
                Vector3d.Scale(
                    Vector3d.Scale(vesselState.angularMomentum, vesselState.angularMomentum),
                    Vector3d.Scale(torque, vesselState.MoI).InvertNoNaN()
                    )
                );
            Controller.OnFixedUpdate();
        }

        public override void OnUpdate()
        {
            if (attitudeChanged)
            {
                if (attitudeReference != AttitudeReference.INERTIAL && attitudeReference != AttitudeReference.INERTIAL_COT)
                {
                    attitudeKILLROT = false;
                }
                // TODO : Do we really need to reset the controller each time the requested attitude changes ?
                Controller.Reset();
                //lastAct = Vector3d.zero;

                attitudeChanged = false;
            }
            Controller.OnUpdate();
        }

        public override void Drive(FlightCtrlState s)
        {
            // Direction we want to be facing
            _requestedAttitude = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget;


            if (useSAS)
            {
                // TODO : This most likely require some love to use all the new SAS magic

                _requestedAttitude = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Quaternion.Euler(90, 0, 0);
                if (!part.vessel.ActionGroups[KSPActionGroup.SAS])
                {
                    part.vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
                    part.vessel.Autopilot.SAS.SetTargetOrientation(_requestedAttitude * Vector3.up, false);
                    lastSAS = _requestedAttitude;
                }
                else if (Quaternion.Angle(lastSAS, _requestedAttitude) > 10)
                {
                    part.vessel.Autopilot.SAS.SetTargetOrientation(_requestedAttitude * Vector3.up, false);
                    lastSAS = _requestedAttitude;
                }
                else
                {
                    part.vessel.Autopilot.SAS.SetTargetOrientation(_requestedAttitude * Vector3.up, true);
                }

                core.thrust.differentialThrottleDemandedTorque = Vector3d.zero;
            }
            else
            {
                Vector3d act;
                Vector3d deltaEuler;
                Controller.DrivePre(s, out act, out deltaEuler);

                SetFlightCtrlState(act, deltaEuler, s, 1);

                act = new Vector3d(s.pitch, s.roll, s.yaw);

                Controller.DrivePost(s);

                // Feed the control torque to the differential throttle
                if (core.thrust.differentialThrottleSuccess == MechJebModuleThrustController.DifferentialThrottleStatus.Success)
                    core.thrust.differentialThrottleDemandedTorque = -Vector3d.Scale(act, vesselState.torqueDiffThrottle * vessel.ctrlState.mainThrottle);
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
                Controller.Reset();
            }

            if (!userCommandingRoll)
            {
                if (!double.IsNaN(act.y)) s.roll = Mathf.Clamp((float)(act.y), -drive_limit, drive_limit);
            }

            if (!userCommandingPitchYaw)
            {
                if (!double.IsNaN(act.x)) s.pitch = Mathf.Clamp((float)(act.x), -drive_limit, drive_limit);
                if (!double.IsNaN(act.z)) s.yaw = Mathf.Clamp((float)(act.z), -drive_limit, drive_limit);
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
                if (RCS_auto && ((absErr.x > 3.0) || (absErr.y > 3.0) || (absErr.z > 3.0)) && core.thrust.limiter != MechJebModuleThrustController.LimitMode.UnstableIgnition)
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
