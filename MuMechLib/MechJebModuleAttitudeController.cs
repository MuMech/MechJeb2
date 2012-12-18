using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAttitudeController : ComputerModule
    {
        public MechJebModuleAttitudeController(MechJebCore core)
            : base(core)
        {
            priority = 800;
        }

        public float stress;

        public float Kp = 120.0F; //not sure what Kp is exactly, but it is effectly a multiplyer for the acting force, at Kp = 10000 the force was clamped at either -1 or +1 the majority of the time
        public double drive_factor = 450.0F;


        private Vector3d integral = Vector3d.zero;
        private Vector3d prev_err = Vector3d.zero;
        private Vector3d k_integral = Vector3d.zero;
        private Vector3d k_prev_err = Vector3d.zero;


        //Variables for drive
        float inertiaFactor = 1.8F; //level of effect inertia has
        float attitudeErrorMinAct = 1.0F; //sets the floor of the attitude error being modified by the error percentage
        float attitudeErrorMaxAct = 3.0F; //sets the ceiling of the attitude error being modified by the error percentage
        float reduceZfactor = 18.0F; //reduces z by this factor, z seems to act far too high in relation to x and y
        float activationDelay = 3.0F; //lessens the act for this many seconds, control goes from 0 to full using a squared formula

        //Array for averaging out the act value - size of array determintes how many frames to average out
        private Vector3d[] a_avg_act = new Vector3d[2];


        //Rapid toggle/resting variables
        private int restFrameTolerance = 90; //How many frames to use for detecting the rapid toggle
        private int[] rapidToggleX = { 0, 0, 0, 0 }; //Array size determines how many toggles required to trigger the axis rest, init array to 0s
        private int[] rapidToggleY = { 0, 0, 0, 0 }; //Negative values indicate the vector was at the minimum threshold
        private int[] rapidToggleZ = { 0, 0, 0, 0 };
        private int rapidRestX = 0; //How long the axis should rest for if rapid toggle is detected
        private int rapidRestY = 0;
        private int rapidRestZ = 0;
        private int rapidToggleRestFactor = 8; //Factor the axis is divided by if the rapid toggle resting is activated


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


        public bool attitudeKILLROT = false;

        protected bool attitudeChanged = false;
        protected bool _attitudeActive = false;
        public bool attitudeActive
        {
            get
            {
                return _attitudeActive;
            }
        }

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



        public Quaternion attitudeGetReferenceRotation(AttitudeReference reference)
        {
            Vector3 fwd, up;
            Quaternion rotRef = Quaternion.identity;
            if (FlightGlobals.fetch.VesselTarget == null &&
                (reference == AttitudeReference.TARGET || reference == AttitudeReference.TARGET_ORIENTATION || reference == AttitudeReference.RELATIVE_VELOCITY))
            {
                //in target-relative mode, but there's no target:
                attitudeDeactivate(null);
                return rotRef;
            }
            if ((reference == AttitudeReference.MANEUVER_NODE) && (part.vessel.patchedConicSolver.maneuverNodes.Count == 0))
            {
                attitudeDeactivate(null);
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
                /*                case AttitudeReference.TARGET:
                                    fwd = (targetPosition() - part.vessel.GetTransform().position).normalized;
                                    up = Vector3d.Cross(fwd, vesselState.leftOrbit);
                                    Vector3.OrthoNormalize(ref fwd, ref up);
                                    rotRef = Quaternion.LookRotation(fwd, up);
                                    break;
                                case AttitudeReference.RELATIVE_VELOCITY:
                                    fwd = relativeVelocityToTarget().normalized;
                                    up = Vector3d.Cross(fwd, vesselState.leftOrbit);
                                    Vector3.OrthoNormalize(ref fwd, ref up);
                                    rotRef = Quaternion.LookRotation(fwd, up);
                                    break;
                                case AttitudeReference.TARGET_ORIENTATION:
                                    Transform targetTransform = FlightGlobals.fetch.VesselTarget.GetTransform();
                                    if (FlightGlobals.fetch.VesselTarget is ModuleDockingNode)
                                    {
                                        rotRef = Quaternion.LookRotation(targetTransform.forward, targetTransform.up);
                                    }
                                    else
                                    {
                                        rotRef = Quaternion.LookRotation(targetTransform.up, targetTransform.right);
                                    }
                                    break;*/
                case AttitudeReference.MANEUVER_NODE:
                    fwd = part.vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(part.vessel.orbit);
                    up = Vector3d.Cross(fwd, vesselState.leftOrbit);
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

        public bool attitudeTo(Quaternion attitude, AttitudeReference reference, ComputerModule controller)
        {
            /*            if ((controlModule != null) && (controller != null) && (controlModule != controller))
                        {
                            return false;
                        }*/

            attitudeReference = reference;
            attitudeTarget = attitude;
            _attitudeActive = true;
            _attitudeRollMatters = true;

            return true;
        }

        public bool attitudeTo(Vector3d direction, AttitudeReference reference, ComputerModule controller)
        {
            bool ok = false;
            double ang_diff = Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, attitudeGetReferenceRotation(reference) * direction));
            Vector3 up, dir = direction;
            if (!attitudeActive || (ang_diff > 45))
            {
                up = attitudeWorldToReference(-part.vessel.GetTransform().forward, reference);
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

        public bool attitudeTo(double heading, double pitch, double roll, ComputerModule controller)
        {
            Quaternion attitude = Quaternion.AngleAxis((float)heading, Vector3.up) * Quaternion.AngleAxis(-(float)pitch, Vector3.right) * Quaternion.AngleAxis(-(float)roll, Vector3.forward);
            return attitudeTo(attitude, AttitudeReference.SURFACE_NORTH, controller);
        }

        public bool attitudeDeactivate(ComputerModule controller)
        {
            /*            if ((controlModule != null) && (controller != null) && (controlModule != controller))
                        {
                            return false;
                        }*/

            _attitudeActive = false;
            attitudeChanged = true;

            return true;
        }

        //angle in degrees between the vessel's current pointing direction and the attitude target, ignoring roll
        public double attitudeAngleFromTarget()
        {
            return attitudeError;
        }



        public override void OnFixedUpdate()
        {
            attitudeError = attitudeActive ? Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, vesselState.forward)) : 0;
        }

        public override void OnUpdate()
        {
            if (attitudeChanged)
            {
                if (attitudeReference != AttitudeReference.INERTIAL)
                {
                    attitudeKILLROT = false;
                }
                integral = Vector3.zero;
                prev_err = Vector3.zero;

                attitudeChanged = false;

                /*                foreach (ComputerModule module in modules)
                                {
                                    module.onAttitudeChange(_oldAttitudeReference, _oldAttitudeTarget, attitudeReference, attitudeTarget);
                                }*/
            }

        }

        public override void drive(FlightCtrlState s)
        {
            if (attitudeActive)
            {
                // Used in the killRot activation calculation and drive_limit calculation
                double precision = Math.Max(0.5, Math.Min(10.0, (vesselState.torquePYAvailable + vesselState.torqueThrustPYAvailable * s.mainThrottle) * 20.0 / vesselState.MoI.magnitude));

                // Direction we want to be facing
                Quaternion target = attitudeGetReferenceRotation(attitudeReference) * attitudeTarget;
                Quaternion delta = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(part.vessel.GetTransform().rotation) * target);

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
                                                      vesselState.angularMomentum.Sign() * inertiaFactor,
                                                      Vector3d.Scale(
                                                          Vector3d.Scale(vesselState.angularMomentum, vesselState.angularMomentum),
                                                          Vector3d.Scale(torque, vesselState.MoI).Invert()
                                                      )
                                                 );

                // Determine the current level of error, this is then used to determine what act to apply
                Vector3d err = deltaEuler * Math.PI / 180.0F;
                err += inertia.Reorder(132);
                err.Scale(Vector3d.Scale(vesselState.MoI, torque.Invert()));
                prev_err = err;

                // Make sure we are taking into account the correct timeframe we are working with
                integral += err * TimeWarp.fixedDeltaTime;

                // The inital value of act
                // Act is ultimately what determines what the pitch, yaw and roll will be set to
                // We make alterations to act between here and passing it into the pod controls
                Vector3d act = Mathf.Clamp((float)attitudeError, attitudeErrorMinAct, attitudeErrorMaxAct) * Kp * err;
                //+ Ki * integral + Kd * deriv; //Ki and Kd are always 0 so they have been commented out

                // The maximum value the controls may be
                float drive_limit = Mathf.Clamp01((float)(err.magnitude * drive_factor / precision));

                // Reduce z by reduceZfactor, z seems to act far too high in relation to x and y
                act.z = act.z / reduceZfactor;

                // Reduce all 3 axis to a maximum of drive_limit
                act.x = Mathf.Clamp((float)act.x, drive_limit * -1, drive_limit);
                act.y = Mathf.Clamp((float)act.y, drive_limit * -1, drive_limit);
                act.z = Mathf.Clamp((float)act.z, drive_limit * -1, drive_limit);

                // Met is the time in seconds from take off
                double met = vesselState.time - part.vessel.launchTime;

                // Reduce effects of controls after launch and returns them gradually
                // This helps to reduce large wobbles experienced in the first few seconds
                if (met < activationDelay)
                {
                    act = act * ((met / activationDelay) * (met / activationDelay));
                }

                // Averages out the act with the last few results, determined by the size of the a_avg_act array
                act = averageVector3d(a_avg_act, act);

                // Looks for rapid toggling of each axis and if found, then rest that axis for a while
                // This helps prevents wobbles from getting larger
                rapidRestX = restForPeriod(rapidToggleX, act.x, rapidRestX);
                rapidRestY = restForPeriod(rapidToggleY, act.y, rapidRestY);
                rapidRestZ = restForPeriod(rapidToggleZ, act.z, rapidRestZ);

                // Reduce axis by rapidToggleRestFactor if rapid toggle rest has been triggered
                if (rapidRestX > 0) act.x = act.x / rapidToggleRestFactor;
                if (rapidRestY > 0) act.y = act.y / rapidToggleRestFactor;
                if (rapidRestZ > 0) act.z = act.z / rapidToggleRestFactor;

                // Sets the SetFlightCtrlState for pitch, yaw and roll
                SetFlightCtrlState(act, deltaEuler, s, precision, drive_limit);

                //Works out the stress
                stress = Mathf.Min(Mathf.Abs((float)(act.x)), 1.0F) + Mathf.Min(Mathf.Abs((float)(act.y)), 1.0F) + Mathf.Min(Mathf.Abs((float)(act.z)), 1.0F);
            }
        }

        // Sets the SetFlightCtrlState for pitch, yaw and roll
        private void SetFlightCtrlState(Vector3d act, Vector3d deltaEuler, FlightCtrlState s, double precision, float drive_limit)
        {
            // Is the user inputting pitch, yaw, roll
            bool userCommandingPitchYaw = (Mathfx.Approx(s.pitch, 0, 0.1F) ? false : true) || (Mathfx.Approx(s.yaw, 0, 0.1F) ? false : true);
            bool userCommandingRoll = (Mathfx.Approx(s.roll, 0, 0.1F) ? false : true);

            if (userCommandingPitchYaw || userCommandingRoll)
            {
                s.killRot = false;
                if (attitudeKILLROT)
                {
                    attitudeTo(Quaternion.LookRotation(part.vessel.GetTransform().up, -part.vessel.GetTransform().forward), AttitudeReference.INERTIAL, null);
                }
            }
            else
            {
                //Determine amount of error
                double int_error = attitudeActive ? Math.Abs(Vector3d.Angle(attitudeGetReferenceRotation(attitudeReference) * attitudeTarget * Vector3d.forward, vesselState.forward)) : 0;

                //Determine if we should have killRot on or not
                s.killRot = (int_error < precision / 10.0) && (Math.Abs(deltaEuler.y) < Math.Min(0.5, precision / 2.0));
            }

            if (userCommandingRoll)
            {
                prev_err = new Vector3d(prev_err.x, prev_err.y, 0);
                integral = new Vector3d(integral.x, integral.y, 0);
                if (!attitudeRollMatters)
                {
                    //human and computer roll
                    attitudeTo(Quaternion.LookRotation(attitudeTarget * Vector3d.forward, attitudeWorldToReference(-part.vessel.GetTransform().forward, attitudeReference)), attitudeReference, null);
                    _attitudeRollMatters = false;
                }
            }
            else
            {
                //computer roll only
                if (!double.IsNaN(act.z)) s.roll = Mathf.Clamp((float)(s.roll + act.z), -drive_limit, drive_limit);
            }

            if (userCommandingPitchYaw)
            {
                //human and computer yaw and pitch
                prev_err = new Vector3d(0, 0, prev_err.z);
                integral = new Vector3d(0, 0, integral.z);
            }
            else
            {
                //computer pitch and yaw only
                if (!double.IsNaN(act.x)) s.pitch = Mathf.Clamp((float)(s.pitch + act.x), -drive_limit, drive_limit);
                if (!double.IsNaN(act.y)) s.yaw = Mathf.Clamp((float)(s.yaw + act.y), -drive_limit, drive_limit);
            }

        }

        // Looks for rapid toggling of each axis and if found, then rest that axis for a while
        // This prevents wobbles from getting larger
        private int restForPeriod(int[] restArray, double vector, int resting)
        {
            int n = restArray.Length - 1; //Last elemet in the array, useful in loops and inseting element to the end
            int insertPos = -1; //Position to insert a vector into
            int vectorSign = Mathf.Clamp(Mathf.RoundToInt((float)vector), -1, 1); //Is our vector a negative, 0 or positive
            float threshold = 0.95F; //Vector must be above this to class as hitting the upper limit
            bool aboveThreshold = Mathf.Abs((float)vector) > threshold; //Determines if the input vector is above the threshold

            // Decrease our resting count so we don't rest forever
            if (resting > 0) resting--;

            // Move all values in restArray towards 0 by 1, effectly taking 1 frame off the count
            // Negative values indicate the vector was at the minimum threshold
            for (int i = n; i >= 0; i--) restArray[i] = restArray[i] - Mathf.Clamp(restArray[i], -1, 1);

            // See if the oldest value has reached 0, if it has move all values 1 to the left
            if (restArray[0] == 0 && restArray[1] != 0)
            {
                for (int i = 0; i < n - 1; i++) restArray[i] = restArray[i + 1]; //shuffle everything down to the left
                restArray[n] = 0; //item n has just been shifted to the left, so reset value to 0
            }

            // Find our position to insert the vector sign, insertPos will be -1 if no empty position is found
            for (int i = n; i >= 0; i--)
            {
                if (restArray[i] == 0) insertPos = i;
            }

            // If we found a valid insert position, and the sign is different to the last sign, then insert it
            if (
                aboveThreshold && ( // First make sure we are above the threshold

                    // If in position 0, the sign is always different
                    (insertPos == 0) ||

                    // If in position 1 to n-1, then make sure the previous sign is different
                // We don't want to rest if the axis is simply at the max for a while such as it may be for hard turns
                    (insertPos > 0 && vectorSign != Mathf.Clamp(restArray[insertPos - 1], -1, 1))
                )
               ) // end if aboveThreshold
            {
                // Insert restFrameTolerance and the vectors sign
                restArray[insertPos] = restFrameTolerance * vectorSign;
            }

            // Determine if the array is full, we are above the threshold, and the sign is different to the last
            if (aboveThreshold && insertPos == -1 && vectorSign != Mathf.Clamp(restArray[n], -1, 1))
            {
                // Array is full, remove oldest value to make room for new value
                for (int i = 0; i < n - 1; i++)
                {
                    restArray[i] = restArray[i + 1];
                }
                // Insert new value
                restArray[n] = restFrameTolerance * vectorSign;

                // Sets the axis to rest for the length of time 3/4 of the frame difference between the first item and last item
                resting = (int)Math.Ceiling(((Math.Abs(restArray[n]) - Math.Abs(restArray[0])) / 4.0) * 3.0);
            }

            // Returns number of frames to rest for, or 0 for no rest
            return resting;
        }

        // Averages out the act with the last few results, determined by the size of the vectorArray array
        private Vector3d averageVector3d(Vector3d[] vectorArray, Vector3d newVector)
        {
            double x = 0.0, y = 0.0, z = 0.0;
            int n = vectorArray.Length;
            int k = 0;

            // Loop through the array to determine average
            // Give more weight to newer items and less weight to older items
            for (int i = 0; i < n; i++)
            {
                k += i + 1;
                if (i < n - 1) { vectorArray[i] = vectorArray[i + 1]; }
                else { vectorArray[i] = newVector; }
                x += vectorArray[i].x * (i + 1);
                y += vectorArray[i].y * (i + 1);
                z += vectorArray[i].z * (i + 1);
            }
            return new Vector3d(x / k, y / k, z / k);
        }


    }
}
