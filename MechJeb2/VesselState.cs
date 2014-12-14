using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class VesselState
    {
        private Vessel vesselRef = null;

        [ValueInfoItem("Universal Time", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)]
        public double time;            //planetarium time
        public double deltaT;          //TimeWarp.fixedDeltaTime

        public Vector3d CoM;
        Matrix3x3f inertiaTensor = new Matrix3x3f();
        public Vector3d MoI; //Diagonal components of the inertia tensor (almost always the dominant components)
        public Vector3d up;
        public Vector3d north;
        public Vector3d east;
        public Vector3d forward;      //the direction the vessel is pointing
        public Vector3d horizontalOrbit;   //unit vector in the direction of horizontal component of orbit velocity
        public Vector3d horizontalSurface; //unit vector in the direction of horizontal component of surface velocity
        public Vector3d rootPartPos;

        public Quaternion rotationSurface;
        public Quaternion rotationVesselSurface;

        public Vector3d velocityMainBodySurface;

        public Vector3d angularVelocity;
        public Vector3d angularMomentum;

        public Vector3d radialPlus;   //unit vector in the plane of up and velocityVesselOrbit and perpendicular to velocityVesselOrbit 
        public Vector3d radialPlusSurface; //unit vector in the plane of up and velocityVesselSurface and perpendicular to velocityVesselSurface
        public Vector3d normalPlus;    //unit vector perpendicular to up and velocityVesselOrbit
        public Vector3d normalPlusSurface;  //unit vector perpendicular to up and velocityVesselSurface

        public Vector3d gravityForce;
        [ValueInfoItem("Local gravity", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "m/s²")]
        public double localg;             //magnitude of gravityForce

        //How about changing these so we store the instantaneous values and *also*
        //the smoothed MovingAverages? Sometimes we need the instantaneous value.
        [ValueInfoItem("Orbital speed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")]
        public MovingAverage speedOrbital = new MovingAverage();
        [ValueInfoItem("Surface speed", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s")]
        public MovingAverage speedSurface = new MovingAverage();
        [ValueInfoItem("Vertical speed", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s")]
        public MovingAverage speedVertical = new MovingAverage();
        [ValueInfoItem("Surface horizontal speed", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s")]
        public MovingAverage speedSurfaceHorizontal = new MovingAverage();
        [ValueInfoItem("Orbit horizontal speed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")]
        public double speedOrbitHorizontal;
        [ValueInfoItem("Heading", InfoItem.Category.Surface, format = "F1", units = "º")]
        public MovingAverage vesselHeading = new MovingAverage();
        [ValueInfoItem("Pitch", InfoItem.Category.Surface, format = "F1", units = "º")]
        public MovingAverage vesselPitch = new MovingAverage();
        [ValueInfoItem("Roll", InfoItem.Category.Surface, format = "F1", units = "º")]
        public MovingAverage vesselRoll = new MovingAverage();
        [ValueInfoItem("Altitude (ASL)", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = -1, units = "m")]
        public MovingAverage altitudeASL = new MovingAverage();
        [ValueInfoItem("Altitude (true)", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = 0, units = "m")]
        public MovingAverage altitudeTrue = new MovingAverage();
        [ValueInfoItem("Surface altitude ASL", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 4, siMaxPrecision = 0, units = "m")]
        double surfaceAltitudeASL;

        [ValueInfoItem("Apoapsis", InfoItem.Category.Orbit, units = "m", format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = 0, category = InfoItem.Category.Orbit)]
        public MovingAverage orbitApA = new MovingAverage();
        [ValueInfoItem("Periapsis", InfoItem.Category.Orbit, units = "m", format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = 0, category = InfoItem.Category.Orbit)]
        public MovingAverage orbitPeA = new MovingAverage();
        [ValueInfoItem("Orbital period", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 2, category = InfoItem.Category.Orbit)]
        public MovingAverage orbitPeriod = new MovingAverage();
        [ValueInfoItem("Time to apoapsis", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 1)]
        public MovingAverage orbitTimeToAp = new MovingAverage();
        [ValueInfoItem("Time to periapsis", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 1)]
        public MovingAverage orbitTimeToPe = new MovingAverage();
        [ValueInfoItem("LAN", InfoItem.Category.Orbit, format = ValueInfoItem.ANGLE)]
        public MovingAverage orbitLAN = new MovingAverage();
        [ValueInfoItem("Argument of periapsis", InfoItem.Category.Orbit, format = "F1", units = "º")]
        public MovingAverage orbitArgumentOfPeriapsis = new MovingAverage();
        [ValueInfoItem("Inclination", InfoItem.Category.Orbit, format = "F3", units = "º")]
        public MovingAverage orbitInclination = new MovingAverage();
        [ValueInfoItem("Eccentricity", InfoItem.Category.Orbit, format = "F3")]
        public MovingAverage orbitEccentricity = new MovingAverage();
        [ValueInfoItem("Semi-major axis", InfoItem.Category.Orbit, format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = 0, units = "m")]
        public MovingAverage orbitSemiMajorAxis = new MovingAverage();
        [ValueInfoItem("Latitude", InfoItem.Category.Surface, format = ValueInfoItem.ANGLE_NS)]
        public MovingAverage latitude = new MovingAverage();
        [ValueInfoItem("Longitude", InfoItem.Category.Surface, format = ValueInfoItem.ANGLE_EW)]
        public MovingAverage longitude = new MovingAverage();
        [ValueInfoItem("Angle of Attack", InfoItem.Category.Misc, format = "F2", units = "º")]
        public MovingAverage AoA = new MovingAverage();
        [ValueInfoItem("Angle of Sideslip", InfoItem.Category.Misc, format = "F2", units = "º")]
        public MovingAverage AoS = new MovingAverage();

        public double radius;  //distance from planet center

        public double mass;

        // Thrust is a vector.  These are in the same frame of reference as forward and other vectors.
        public Vector3d thrustVectorLastFrame = new Vector3d();
        public Vector3d thrustVectorMaxThrottle = new Vector3d();
        public Vector3d thrustVectorMinThrottle = new Vector3d();

        // Thrust in the forward direction (for historical reasons).
        public double thrustAvailable { get { return Vector3d.Dot(thrustVectorMaxThrottle, forward); } }
        public double thrustMinimum { get { return Vector3d.Dot(thrustVectorMinThrottle, forward); } }
        public double thrustCurrent { get { return Vector3d.Dot(thrustVectorLastFrame, forward); } }

        // Acceleration in the forward direction, for when dividing by mass is too complicated.
        public double maxThrustAccel { get { return thrustAvailable / mass; } }
        public double minThrustAccel { get { return thrustMinimum / mass; } }
        public double currentThrustAccel { get { return thrustCurrent / mass; } }

        public double maxEngineResponseTime = 0;

        public bool rcsThrust = false;
        public float throttleLimit = 1;
        public double limitedMaxThrustAccel { get { return maxThrustAccel * throttleLimit + minThrustAccel * (1 - throttleLimit); } }
        // Total base torque (including torque from SRB)
        public Vector3d torqueAvailable;
        // Variable part of torque related to throttle
        public Vector3d torqueFromEngine;
        public double massDrag;
        public double atmosphericDensity;
        [ValueInfoItem("Atmosphere density", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "g/m³")]
        public double atmosphericDensityGrams;
        [ValueInfoItem("Dynamic pressure", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "pa")]
        public double dynamicPressure;
        [ValueInfoItem("Intake air", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")]
        public double intakeAir;
        [ValueInfoItem("Intake air (all intakes open)", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")]
        public double intakeAirAllIntakes;
        [ValueInfoItem("Intake air needed", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")]
        public double intakeAirNeeded;
        [ValueInfoItem("Intake air needed (max)", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")]
        public double intakeAirAtMax;
        [ValueInfoItem("Angle to prograde", InfoItem.Category.Orbit, format = "F2", units = "º")]
        public double angleToPrograde;

        public Vector6 rcsThrustAvailable; // thrust available from RCS thrusters
        public Vector6 rcsTorqueAvailable; // torque available from RCS thrusters

        public Vector6 ctrlTorqueAvailable; // torque available from control surfaces

        // List of parachutes
        public List<ModuleParachute> parachutes;

        public bool parachuteDeployed;
        
        // Resource information keyed by resource Id.
        public Dictionary<int, ResourceInfo> resources;

        public CelestialBody mainBody;

        // Callbacks for external module
        public delegate void VesselStatePartExtension(Part p);
        public delegate void VesselStatePartModuleExtension(PartModule pm);

        public delegate bool GimbalExtIsValid(PartModule p);
        public delegate Vector3d GimbalExtTorqueVector(PartModule p, int i, Vector3d CoM);
        public delegate Quaternion GimbalExtInitialRot(PartModule p, Transform engineTransform, int i);

        public struct GimbalExt
        {
            public GimbalExtIsValid isValid;
            public GimbalExtInitialRot initialRot;
            public GimbalExtTorqueVector torqueVector;
        }

        private static Dictionary<System.Type, GimbalExt> gimbalExtDict;

        public List<VesselStatePartExtension> vesselStatePartExtensions = new List<VesselStatePartExtension>();
        public List<VesselStatePartModuleExtension> vesselStatePartModuleExtensions = new List<VesselStatePartModuleExtension>();
        public delegate double DTerminalVelocity();

        static VesselState()
        {
            gimbalExtDict = new Dictionary<System.Type, GimbalExt>();
            GimbalExt nullGimbal = new GimbalExt() { isValid = nullGimbalIsValid, initialRot = nullGimbalInitialRot, torqueVector = nullGimbalTorqueVector };
            GimbalExt stockGimbal = new GimbalExt() { isValid = stockGimbalIsValid, initialRot = stockGimbalInitialRot, torqueVector = stockGimbalTorqueVector };
            gimbalExtDict.Add(typeof(object), nullGimbal);
            gimbalExtDict.Add(typeof(ModuleGimbal), stockGimbal);
        }

        public VesselState()
        {
            TerminalVelocityCall = TerminalVelocityStockKSP;
        }

        public void Update(Vessel vessel)
        {
            if (vessel.rigidbody == null) return; //if we try to update before rigidbodies exist we spam the console with NullPointerExceptions.

            UpdateBasicInfo(vessel);

            UpdateRCSThrustAndTorque(vessel);

            EngineInfo einfo = new EngineInfo(CoM);
            IntakeInfo iinfo = new IntakeInfo();
            AnalyzeParts(vessel, einfo, iinfo);

            UpdateResourceRequirements(einfo, iinfo);

            ToggleRCSThrust(vessel);

            UpdateMoIAndAngularMom(vessel);
        }

        
        // Calculate a bunch of simple quantities each frame.
        void UpdateBasicInfo(Vessel vessel)
        {
            time = Planetarium.GetUniversalTime();
            deltaT = TimeWarp.fixedDeltaTime;

            CoM = vessel.findWorldCenterOfMass();
            up = (CoM - vessel.mainBody.position).normalized;

            Rigidbody rigidBody = vessel.rootPart.rigidbody;
            if (rigidBody != null) rootPartPos = rigidBody.position;

            north = Vector3d.Exclude(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized;
            east = vessel.mainBody.getRFrmVel(CoM).normalized;
            forward = vessel.GetTransform().up;
            rotationSurface = Quaternion.LookRotation(north, up);
            rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * rotationSurface);

            // Angle of attack, angle between surface velocity and the vessel's "up" vector
            // Originally from ferram4's FAR
            Vector3 tmpVec = vessel.ReferenceTransform.up      * Vector3.Dot(vessel.ReferenceTransform.up,      vessel.srf_velocity.normalized)
                           + vessel.ReferenceTransform.forward * Vector3.Dot(vessel.ReferenceTransform.forward, vessel.srf_velocity.normalized);   //velocity vector projected onto a plane that divides the airplane into left and right halves
            double tmpAoA = 180.0/Math.PI * Math.Asin(Vector3.Dot(tmpVec.normalized, vessel.ReferenceTransform.forward));
            if (double.IsNaN(tmpAoA))
                AoA.value = 0;
            else
                AoA.value = tmpAoA;

            // Angle of Sideslip, angle between surface velocity and the vessel's "right" vector
            // Originally from ferram4's FAR
            tmpVec = vessel.ReferenceTransform.up    * Vector3.Dot(vessel.ReferenceTransform.up,    vessel.srf_velocity.normalized) 
                   + vessel.ReferenceTransform.right * Vector3.Dot(vessel.ReferenceTransform.right, vessel.srf_velocity.normalized);     //velocity vector projected onto the vehicle-horizontal plane
            double tempAoS = 180.0/Math.PI * Math.Asin(Vector3.Dot(tmpVec.normalized, vessel.ReferenceTransform.right));
            if (double.IsNaN(tempAoS))
                AoS.value = 0;
            else
                AoS.value= tempAoS;

            velocityMainBodySurface = rotationSurface * vessel.srf_velocity;

            horizontalOrbit = Vector3d.Exclude(up, vessel.obt_velocity).normalized;
            horizontalSurface = Vector3d.Exclude(up, vessel.srf_velocity).normalized;

            angularVelocity = Quaternion.Inverse(vessel.GetTransform().rotation) * vessel.rigidbody.angularVelocity;

            radialPlusSurface = Vector3d.Exclude(vessel.srf_velocity, up).normalized;
            radialPlus = Vector3d.Exclude(vessel.obt_velocity, up).normalized;
            normalPlusSurface = -Vector3d.Cross(radialPlusSurface, vessel.srf_velocity.normalized);
            normalPlus = -Vector3d.Cross(radialPlus, vessel.obt_velocity.normalized);

            gravityForce = FlightGlobals.getGeeForceAtPosition(CoM);
            localg = gravityForce.magnitude;

            speedOrbital.value = vessel.obt_velocity.magnitude;
            speedSurface.value = vessel.srf_velocity.magnitude;
            speedVertical.value = Vector3d.Dot(vessel.srf_velocity, up);
            speedSurfaceHorizontal.value = Vector3d.Exclude(up, vessel.srf_velocity).magnitude; //(velocityVesselSurface - (speedVertical * up)).magnitude;
            speedOrbitHorizontal = (vessel.obt_velocity - (speedVertical * up)).magnitude;

            vesselHeading.value = rotationVesselSurface.eulerAngles.y;
            vesselPitch.value = (rotationVesselSurface.eulerAngles.x > 180) ? (360.0 - rotationVesselSurface.eulerAngles.x) : -rotationVesselSurface.eulerAngles.x;
            vesselRoll.value = (rotationVesselSurface.eulerAngles.z > 180) ? (rotationVesselSurface.eulerAngles.z - 360.0) : rotationVesselSurface.eulerAngles.z;

            altitudeASL.value = vessel.mainBody.GetAltitude(CoM);

            surfaceAltitudeASL = vessel.mainBody.pqsController != null ? vessel.pqsAltitude : 0d;
            if (vessel.mainBody.ocean && surfaceAltitudeASL < 0) surfaceAltitudeASL = 0;
            altitudeTrue.value = altitudeASL - surfaceAltitudeASL;

            // altitudeBottom will be recomputed if someone requests it.
            altitudeBottomIsCurrent = false;

            double atmosphericPressure = FlightGlobals.getStaticPressure(altitudeASL, vessel.mainBody);
            if (atmosphericPressure < vessel.mainBody.atmosphereMultiplier * 1e-6) atmosphericPressure = 0;
            atmosphericDensity = FlightGlobals.getAtmDensity(atmosphericPressure);
            atmosphericDensityGrams = atmosphericDensity * 1000;
            dynamicPressure = 0.5 * vessel.atmDensity * vessel.srf_velocity.sqrMagnitude;

            orbitApA.value = vessel.orbit.ApA;
            orbitPeA.value = vessel.orbit.PeA;
            orbitPeriod.value = vessel.orbit.period;
            orbitTimeToAp.value = vessel.orbit.timeToAp;
            if (vessel.orbit.eccentricity < 1) orbitTimeToPe.value = vessel.orbit.timeToPe;
            else orbitTimeToPe.value = -vessel.orbit.meanAnomaly / (2 * Math.PI / vessel.orbit.period);
            orbitLAN.value = vessel.orbit.LAN;
            orbitArgumentOfPeriapsis.value = vessel.orbit.argumentOfPeriapsis;
            orbitInclination.value = vessel.orbit.inclination;
            orbitEccentricity.value = vessel.orbit.eccentricity;
            orbitSemiMajorAxis.value = vessel.orbit.semiMajorAxis;
            latitude.value = vessel.mainBody.GetLatitude(CoM);
            longitude.value = MuUtils.ClampDegrees180(vessel.mainBody.GetLongitude(CoM));

            if (vessel.mainBody != Planetarium.fetch.Sun)
            {
                Vector3d delta = vessel.mainBody.getPositionAtUT(Planetarium.GetUniversalTime() + 1) - vessel.mainBody.getPositionAtUT(Planetarium.GetUniversalTime() - 1);
                Vector3d plUp = Vector3d.Cross(vessel.mainBody.getPositionAtUT(Planetarium.GetUniversalTime()) - vessel.mainBody.referenceBody.getPositionAtUT(Planetarium.GetUniversalTime()), vessel.mainBody.getPositionAtUT(Planetarium.GetUniversalTime() + vessel.mainBody.orbit.period / 4) - vessel.mainBody.referenceBody.getPositionAtUT(Planetarium.GetUniversalTime() + vessel.mainBody.orbit.period / 4)).normalized;
                angleToPrograde = MuUtils.ClampDegrees360((((vessel.orbit.inclination > 90) || (vessel.orbit.inclination < -90)) ? 1 : -1) * ((Vector3)up).AngleInPlane(plUp, delta));
            }
            else
            {
                angleToPrograde = 0;
            }

            mainBody = vessel.mainBody;

            radius = (CoM - vessel.mainBody.position).magnitude;

            vesselRef = vessel;
        }

        void UpdateRCSThrustAndTorque(Vessel vessel)
        {
            rcsThrustAvailable = new Vector6();
            rcsTorqueAvailable = new Vector6();

            if (!vessel.ActionGroups[KSPActionGroup.RCS]) return;

            var rcsbal = vessel.GetMasterMechJeb().rcsbal;
            if (rcsbal.enabled)
            {
                Vector3d rot = Vector3d.zero;
                foreach (Vector6.Direction dir6 in Enum.GetValues(typeof(Vector6.Direction)))
                {
                    Vector3d dir = Vector6.directions[dir6];
                    double[] throttles;
                    List<RCSSolver.Thruster> thrusters;
                    rcsbal.GetThrottles(dir, out throttles, out thrusters);
                    if (throttles != null)
                    {
                        for (int i = 0; i < throttles.Length; i++)
                        {
                            if (throttles[i] > 0)
                            {
                                Vector3d force = thrusters[i].GetThrust(dir, rot);
                                rcsThrustAvailable.Add(vessel.GetTransform().InverseTransformDirection(dir * Vector3d.Dot(force * throttles[i], dir)));
                                // Are we missing an rcsTorqueAvailable calculation here?
                            }
                        }
                    }
                }
            }
            else // !rcsbal.enabled
            {
                foreach (Part p in vessel.parts)
                {
                    foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                    {
                        double maxT = pm.thrusterPower;
                        Vector3d partPosition = p.Rigidbody.worldCenterOfMass - CoM;

                        if ((pm.isEnabled) && (!pm.isJustForShow))
                        {
                            foreach (Transform t in pm.thrusterTransforms)
                            {
                                Vector3d thrusterThrust = vessel.GetTransform().InverseTransformDirection(-t.up.normalized) * pm.thrusterPower;
                                rcsThrustAvailable.Add(thrusterThrust);
                                Vector3d thrusterTorque = Vector3.Cross(vessel.GetTransform().InverseTransformDirection(partPosition), thrusterThrust);
                                rcsTorqueAvailable.Add(thrusterTorque);
                            }
                        }
                    }
                }
            }
        }

        // Loop over all the parts in the vessel and calculate some things.
        void AnalyzeParts(Vessel vessel, EngineInfo einfo, IntakeInfo iinfo)
        {
            mass = 0;
            massDrag = 0;

            parachutes = new List<ModuleParachute>();
            parachuteDeployed = false;

            torqueAvailable = Vector3d.zero;
            torqueFromEngine = Vector3d.zero;
            ctrlTorqueAvailable = new Vector6();

            foreach (Part p in vessel.parts)
            {
                if (p.IsPhysicallySignificant())
                {
                    double partMass = p.TotalMass();
                    mass += partMass;
                    massDrag += partMass * p.maximum_drag;
                }

                if (p is ControlSurface) // legacy. Remove this if and when it's no longer important to support mods that use ControlSurface
                {
                    Vector3d partPosition = p.Rigidbody.worldCenterOfMass - CoM;
                    ControlSurface cs = (p as ControlSurface);
                    Vector3d airSpeed = vessel.srf_velocity + Vector3.Cross(cs.Rigidbody.angularVelocity, cs.transform.position - cs.Rigidbody.position);
                    // Air Speed is velocityVesselSurface
                    // AddForceAtPosition seems to need the airspeed vector rotated with the flap rotation x its surface
                    Quaternion airSpeedRot = Quaternion.AngleAxis(cs.ctrlSurfaceRange * cs.ctrlSurfaceArea, cs.transform.rotation * cs.pivotAxis);
                    Vector3 ctrlTroquePos = vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, cs.getLiftVector(airSpeedRot * airSpeed)));
                    Vector3 ctrlTroqueNeg = vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, cs.getLiftVector(Quaternion.Inverse(airSpeedRot) * airSpeed)));
                    ctrlTorqueAvailable.Add(ctrlTroquePos);
                    ctrlTorqueAvailable.Add(ctrlTroqueNeg);
                }

                foreach (VesselStatePartExtension vspe in vesselStatePartExtensions)
                {
                    vspe(p);
                }

                foreach (PartModule pm in p.Modules)
                {
                    if (!pm.isEnabled) continue;

                    if (pm is ModuleReactionWheel)
                    {
                        ModuleReactionWheel rw = (ModuleReactionWheel)pm;
                        // I had to remove the test for active in .23 since the new ressource system reply to the RW that 
                        // there is no energy available when the RW do tiny adjustement.
                        // I replaceed it with a test that check if there is electricity anywhere on the ship. 
                        // Let's hope we don't get reaction wheel that use something else
                        //if (rw.wheelState == ModuleReactionWheel.WheelState.Active && !rw.stateString.Contains("Not enough"))
                        if (rw.wheelState == ModuleReactionWheel.WheelState.Active && vessel.HasElectricCharge())
                            torqueAvailable += new Vector3d(rw.PitchTorque, rw.RollTorque, rw.YawTorque);
                    }
                    else if (pm is ModuleEngines)
                    {
                        einfo.AddNewEngine(pm as ModuleEngines, p.Rigidbody.worldCenterOfMass - CoM);
                    }
                    else if (pm is ModuleEnginesFX)
                    {
                        einfo.AddNewEngine(pm as ModuleEnginesFX, p.Rigidbody.worldCenterOfMass - CoM);
                    }
                    else if (pm is ModuleResourceIntake)
                    {
                        iinfo.addIntake(pm as ModuleResourceIntake);
                    }
                    else if (pm is ModuleParachute)
                    {
                        ModuleParachute parachute = pm as ModuleParachute;
                        parachutes.Add(parachute);
                        if (parachute.deploymentState == ModuleParachute.deploymentStates.DEPLOYED || parachute.deploymentState == ModuleParachute.deploymentStates.SEMIDEPLOYED)
                        {
                            parachuteDeployed = true;
                        }
                    }
                    else if (pm is ModuleControlSurface)
                    {
                        // TODO : Tweakable for ignorePitch / ignoreYaw  / ignoreRoll 
                        ModuleControlSurface cs = (pm as ModuleControlSurface);
                        Vector3d partPosition = p.Rigidbody.worldCenterOfMass - CoM;

                        Vector3d airSpeed = vessel.srf_velocity + Vector3.Cross(cs.part.Rigidbody.angularVelocity, cs.transform.position - cs.part.Rigidbody.position);

                        Quaternion airSpeedRot = Quaternion.AngleAxis(cs.ctrlSurfaceRange * cs.ctrlSurfaceArea, cs.transform.rotation * Vector3.right);

                        Vector3 ctrlTroquePos = vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, cs.getLiftVector(airSpeedRot * airSpeed)));
                        Vector3 ctrlTroqueNeg = vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, cs.getLiftVector(Quaternion.Inverse(airSpeedRot) * airSpeed)));
                        ctrlTorqueAvailable.Add(ctrlTroquePos);
                        ctrlTorqueAvailable.Add(ctrlTroqueNeg);
                    }

                    foreach (VesselStatePartModuleExtension vspme in vesselStatePartModuleExtensions)
                    {
                        vspme(pm);
                    }
                }
            }

            torqueAvailable += Vector3d.Max(rcsTorqueAvailable.positive, rcsTorqueAvailable.negative); // Should we use Max or Min ?
            torqueAvailable += Vector3d.Max(ctrlTorqueAvailable.positive, ctrlTorqueAvailable.negative); // Should we use Max or Min ?            
            torqueAvailable += Vector3d.Max(einfo.torqueEngineAvailable.positive, einfo.torqueEngineAvailable.negative);

            // TODO: add torqueEngineThrottle if differential throttle is active

            torqueFromEngine += Vector3d.Max(einfo.torqueEngineVariable.positive, einfo.torqueEngineVariable.negative);

            thrustVectorMaxThrottle = einfo.thrustMax;
            thrustVectorMinThrottle = einfo.thrustMin;
            thrustVectorLastFrame = einfo.thrustCurrent;
            
            maxEngineResponseTime = einfo.maxResponseTime;
        }

        void UpdateResourceRequirements(EngineInfo einfo, IntakeInfo iinfo)
        {
            // Convert the resource information from the einfo and iinfo format
            // to the more useful ResourceInfo format.
            resources = new Dictionary<int, ResourceInfo>();
            foreach (var info in einfo.resourceRequired)
            {
                int id = info.Key;
                var req = info.Value;
                resources[id] = new ResourceInfo(
                        PartResourceLibrary.Instance.GetDefinition(id),
                        req.requiredLastFrame,
                        req.requiredAtMaxThrottle,
                        iinfo.getIntakes(id));
            }

            int intakeAirId = PartResourceLibrary.Instance.GetDefinition("IntakeAir").id;
            intakeAir = 0;
            intakeAirNeeded = 0;
            intakeAirAtMax = 0;
            intakeAirAllIntakes = 0;
            if (resources.ContainsKey(intakeAirId))
            {
                intakeAir = resources[intakeAirId].intakeProvided;
                intakeAirAllIntakes = resources[intakeAirId].intakeAvailable;
                intakeAirNeeded = resources[intakeAirId].required;
                intakeAirAtMax = resources[intakeAirId].requiredAtMaxThrottle;
            }
        }

        // Decide whether to control the RCS thrusters from the main throttle
        void ToggleRCSThrust(Vessel vessel)
        {
            if (thrustVectorMaxThrottle.magnitude == 0 && vessel.ActionGroups[KSPActionGroup.RCS])
            {
                rcsThrust = true;
                thrustVectorMaxThrottle += (Vector3d)(vessel.transform.up) * rcsThrustAvailable.down;
            }
            else
            {
                rcsThrust = false;
            }
        }

        // KSP's calculation of the vessel's moment of inertia is broken.
        // This function is somewhat expensive :(
        // Maybe it can be optimized more.
        void UpdateMoIAndAngularMom(Vessel vessel)
        {
            inertiaTensor = new Matrix3x3f();

            Transform vesselTransform = vessel.GetTransform();
            Quaternion inverseVesselRotation = Quaternion.Inverse(vesselTransform.rotation);

            Vector3[] unitVectors = { new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1) };

            foreach (Part p in vessel.parts)
            {
                Rigidbody rigidbody = p.Rigidbody;
                if (rigidbody == null) continue;

                //Compute the contributions to the vessel inertia tensor due to the part inertia tensor
                Vector3 principalMoments = rigidbody.inertiaTensor;
                Quaternion princAxesRot = inverseVesselRotation * p.transform.rotation * rigidbody.inertiaTensorRotation;
                Quaternion invPrincAxesRot = Quaternion.Inverse(princAxesRot);

                for (int j = 0; j < 3; j++)
                {
                    Vector3 partInertiaTensorTimesjHat = princAxesRot * Vector3.Scale(principalMoments, invPrincAxesRot * unitVectors[j]);
                    for (int i = 0; i < 3; i++)
                    {
                        inertiaTensor[i, j] += Vector3.Dot(unitVectors[i], partInertiaTensorTimesjHat);
                    }
                }

                //Compute the contributions to the vessel inertia tensor due to the part mass and position
                float partMass = p.TotalMass();
                Vector3 partPosition = vesselTransform.InverseTransformDirection(rigidbody.worldCenterOfMass - CoM);

                for (int i = 0; i < 3; i++)
                {
                    inertiaTensor[i, i] += partMass * partPosition.sqrMagnitude;
                    
                    for (int j = 0; j < 3; j++)
                    {
                        inertiaTensor[i, j] += -partMass * partPosition[i] * partPosition[j];
                    }
                }
            }

            MoI = new Vector3d(inertiaTensor[0, 0], inertiaTensor[1, 1], inertiaTensor[2, 2]);
            angularMomentum = inertiaTensor * angularVelocity;
        }

        [ValueInfoItem("Terminal velocity", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s")]
        public double TerminalVelocity()
        {
            return TerminalVelocityCall();
        }
        
        public DTerminalVelocity TerminalVelocityCall;
                      
        public double TerminalVelocityStockKSP()
        {
            if (mainBody == null || altitudeASL > mainBody.RealMaxAtmosphereAltitude()) return double.PositiveInfinity;

            double airDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(CoM, mainBody));
            return Math.Sqrt(2 * localg * mass / (massDrag * FlightGlobals.DragMultiplier * airDensity));
        }

        public double ThrustAccel(double throttle)
        {
            return (1.0 - throttle) * minThrustAccel + throttle * maxThrustAccel;
        }

        public double HeadingFromDirection(Vector3d dir)
        {
            return MuUtils.ClampDegrees360(180 / Math.PI * Math.Atan2(Vector3d.Dot(dir, east), Vector3d.Dot(dir, north)));
        }

        // Altitude of bottom of craft, only calculated when requested because it is a bit expensive
        private bool altitudeBottomIsCurrent = false;
        private double _altitudeBottom;
        [ValueInfoItem("Altitude (bottom)", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = 0, units = "m")]
        public double altitudeBottom
        {
            get
            {
                if (!altitudeBottomIsCurrent)
                {
                    _altitudeBottom = ComputeVesselBottomAltitude(vesselRef);
                    altitudeBottomIsCurrent = true;
                }
                return _altitudeBottom;
            }
        }

        double ComputeVesselBottomAltitude(Vessel vessel)
        {
            double ret = altitudeTrue;
            foreach (Part p in vessel.parts)
            {
                if (p.collider != null)
                {
                    /*Vector3d bottomPoint = p.collider.ClosestPointOnBounds(vesselmainBody.position);
                    double partBottomAlt = vesselmainBody.GetAltitude(bottomPoint) - surfaceAltitudeASL;
                    _altitudeBottom = Math.Max(0, Math.Min(_altitudeBottom, partBottomAlt));*/
                    Bounds bounds = p.collider.bounds;
                    Vector3 extents = bounds.extents;
                    float partRadius = Mathf.Max(extents[0], Mathf.Max(extents[1], extents[2]));
                    double partAltitudeBottom = vessel.mainBody.GetAltitude(bounds.center) - partRadius - surfaceAltitudeASL;
                    partAltitudeBottom = Math.Max(0, partAltitudeBottom);
                    if (partAltitudeBottom < ret) ret = partAltitudeBottom;
                }
            }
            return ret;
        }

        internal static GimbalExt getGimbalExt(Part p, out PartModule pm)
        {
            foreach (PartModule m in p.Modules)
            {
                GimbalExt gimbal;
                if (gimbalExtDict.TryGetValue(m.GetType(), out gimbal) && gimbal.isValid(m))
                {
                    pm = m;
                    return gimbal;
                }
            }
            pm = null;
            return gimbalExtDict[typeof(object)];
        }

        // The delgates implentation for the null gimbal ( no gimbal present)
        private static bool nullGimbalIsValid(PartModule p)
        {
            return true;
        }

        private static Vector3d nullGimbalTorqueVector(PartModule p, int i, Vector3d CoM)
        {
            return Vector3d.zero;
        }

        private static Quaternion nullGimbalInitialRot(PartModule p, Transform engineTransform, int i)
        {
            return engineTransform.rotation;
        }

        // The delegate implementation for the stock gimbal
        private static bool stockGimbalIsValid(PartModule p)
        {
            ModuleGimbal gimbal = p as ModuleGimbal;
            return gimbal.initRots.Count() > 0;
        }

        private static Vector3d stockGimbalTorqueVector(PartModule p, int i, Vector3d CoM)
        {
            ModuleGimbal gimbal = p as ModuleGimbal;
            Vector3d torque = Vector3d.zero;

            if (gimbal.gimbalLock)
                return Vector3d.zero;

            // Edge case where multiple gimbals defined, clamp to the last one as an easy fix.
            i = Math.Min(gimbal.gimbalTransforms.Count - 1, i);

            Vector3d position = gimbal.gimbalTransforms[i].position - CoM;
            double distance = position.magnitude;
            double radius = Vector3.Exclude(Vector3.Project(position, p.vessel.ReferenceTransform.up), position).magnitude;

            torque.x = Math.Sin(Math.Abs(gimbal.gimbalRange) * Math.PI / 180d) * distance;
            torque.z = Math.Sin(Math.Abs(gimbal.gimbalRange) * Math.PI / 180d) * distance;

            // The "(e.part.vessel.rb_velocity * Time.fixedDeltaTime)" makes no sense to me but that's how the game does it...
            Vector3d position2 = position + (p.vessel.rb_velocity * Time.fixedDeltaTime);
            Vector3d radialAxis = Vector3.Exclude(Vector3.Project(position2, p.vessel.ReferenceTransform.up), position2);
            if (radialAxis.sqrMagnitude > 0.01f)
            {
                torque.y = Math.Sin(Math.Abs(gimbal.gimbalRange) * Math.PI / 180d) * radius;
            }

            return torque;
        }

        private static Quaternion stockGimbalInitialRot(PartModule p, Transform engineTransform, int i)
        {
            ModuleGimbal gimbal = p as ModuleGimbal;

            // Edge case where multiple gimbals defined, clamp to the last one as an easy fix.
            i = Math.Min(gimbal.gimbalTransforms.Count - 1, i);

            // Save the current local rot
            Quaternion save = gimbal.gimbalTransforms[i].localRotation;
            // Apply the default rot and let unity compute the world rot
            gimbal.gimbalTransforms[i].localRotation = gimbal.initRots[i];
            Quaternion initRot = engineTransform.rotation;
            // Restore the current local rot
            gimbal.gimbalTransforms[i].localRotation = save;
            return initRot;
        }

        // Used during the vesselState constructor; distilled to other
        // variables later.
        public class EngineInfo
        {
            public Vector3d thrustCurrent = new Vector3d(); // thrust at throttle achieved last frame
            public Vector3d thrustMax = new Vector3d(); // thrust at full throttle
            public Vector3d thrustMin = new Vector3d(); // thrust at zero throttle
            public double maxResponseTime = 0;

            public Vector6 torqueEngineAvailable = new Vector6();
            public Vector6 torqueEngineVariable = new Vector6();
            public Vector3d torqueEngineThrottle = new Vector3d();

            public class FuelRequirement
            {
                public double requiredLastFrame = 0;
                public double requiredAtMaxThrottle = 0;
            }
            public Dictionary<int, FuelRequirement> resourceRequired = new Dictionary<int, FuelRequirement>();

            Vector3d CoM;
            float atmP0; // pressure now
            float atmP1; // pressure after one timestep

            public EngineInfo(Vector3d c)
            {
                CoM = c;
                atmP0 = (float)FlightGlobals.getStaticPressure();  // TODO : more FlightGlobals call to remove
                float alt1 = (float)(FlightGlobals.ship_altitude + TimeWarp.fixedDeltaTime * FlightGlobals.ship_verticalSpeed);
                atmP1 = (float)FlightGlobals.getStaticPressure(alt1);
            }

            public void AddNewEngine(ModuleEngines e, Vector3d partPosition)
            {
                if ((!e.EngineIgnited) || (!e.isEnabled))
                {
                    return;
                }

                // Compute the resource requirement at full thrust.
                //   mdot = maxthrust / (Isp * g0) in tonnes per second
                //   udot = mdot / mixdensity in units per second of propellant per ratio unit
                //   udot * ratio_i : units per second of propellant i
                // Choose the worse Isp between now and after one timestep.
                // TODO: actually, pressure should be for the engine part, not for the spacecraft.
                // The pressure can easily vary by 1% from top to bottom of a spacecraft.
                // We'd need to compute the position of the part, which seems like a pain.
                float Isp0 = e.atmosphereCurve.Evaluate(atmP0);
                float Isp1 = e.atmosphereCurve.Evaluate(atmP1);
                double Isp = Math.Min(Isp0, Isp1);
                double udot = e.maxThrust / (Isp * e.g * e.mixtureDensity); // Tavert Issue #163
                foreach (var propellant in e.propellants)
                {
                    double maxreq = udot * propellant.ratio;
                    addResource(propellant.id, propellant.currentRequirement, maxreq);
                }

                if (!e.getFlameoutState)
                {
                    Part p = e.part;

                    double usableFraction = 1;
                    if (e.useVelocityCurve)
                    {
                        usableFraction *= e.velocityCurve.Evaluate((float)(e.part.vessel.orbit.GetVel() - e.part.vessel.mainBody.getRFrmVel(CoM)).magnitude);
                    }

                    float maxThrust = e.maxThrust / (float)e.thrustTransforms.Count;
                    float minThrust = e.minThrust / (float)e.thrustTransforms.Count;

                    double eMaxThrust = minThrust + (maxThrust - minThrust) * e.thrustPercentage / 100f;
                    double eMinThrust = e.throttleLocked ? eMaxThrust : minThrust;
                    double eCurrentThrust = usableFraction * (eMaxThrust * e.currentThrottle + eMinThrust * (1 - e.currentThrottle));

                    for (int i = 0; i < e.thrustTransforms.Count; i++)
                    {
                        PartModule gimbal;
                        GimbalExt gimbalExt = VesselState.getGimbalExt(p, out gimbal);

                        // The rotation makes a +z vector point in the direction that molecules are ejected
                        // from the engine.  The resulting thrust force is in the opposite direction.
                        // This gives us the thrust direction at rest state fro gimbaled engines
                        Vector3d thrustDirectionVector = gimbalExt.initialRot(gimbal, e.thrustTransforms[i], i) * Vector3d.back;
                        // This one would give us the current thrust direction including current gimbal
                        // Not sure which one is the best one to use.
                        //thrustDirectionVector = e.thrustTransforms[i].rotation * Vector3d.back;

                        double cosineLosses = Vector3d.Dot(thrustDirectionVector, e.part.vessel.GetTransform().up);

                        thrustCurrent += eCurrentThrust * cosineLosses * thrustDirectionVector;
                        thrustMax += eMaxThrust * cosineLosses * thrustDirectionVector;
                        thrustMin += eMinThrust * cosineLosses * thrustDirectionVector;

                        Vector3d torque = gimbalExt.torqueVector(gimbal, i, CoM);

                        torqueEngineAvailable.Add(torque * eMinThrust);
                        torqueEngineVariable.Add(torque * (eMaxThrust - eMinThrust));
                        if (!e.throttleLocked)
                        {
                            torqueEngineThrottle += Vector3d.Cross(partPosition, thrustDirectionVector) * (maxThrust - minThrust); // TODO: check
                        }
                    }

                    if (e.useEngineResponseTime)
                    {
                        double responseTime = 1.0 / Math.Min(e.engineAccelerationSpeed, e.engineDecelerationSpeed);
                        if (responseTime > maxResponseTime) maxResponseTime = responseTime;
                    }
                }
            }

            // Support for the new ModuleEnginesFX - lack of common interface between the 2 engins type is not fun
            // I can't even just copy  ModuleEngines to a ModuleEnginesFX and use the same function since some field are readonly
            public void AddNewEngine(ModuleEnginesFX e, Vector3d partPosition)
            {
                if ((!e.EngineIgnited) || (!e.isEnabled))
                {
                    return;
                }

                // Compute the resource requirement at full thrust.
                //   mdot = maxthrust / (Isp * g0) in tonnes per second
                //   udot = mdot / mixdensity in units per second of propellant per ratio unit
                //   udot * ratio_i : units per second of propellant i
                // Choose the worse Isp between now and after one timestep.
                // TODO: actually, pressure should be for the engine part, not for the spacecraft.
                // The pressure can easily vary by 1% from top to bottom of a spacecraft.
                // We'd need to compute the position of the part, which seems like a pain.
                float Isp0 = e.atmosphereCurve.Evaluate(atmP0);
                float Isp1 = e.atmosphereCurve.Evaluate(atmP1);
                double Isp = Math.Min(Isp0, Isp1);
                double udot = e.maxThrust / (Isp * e.g * e.mixtureDensity); // Tavert Issue #163
                foreach (var propellant in e.propellants)
                {
                    double maxreq = udot * propellant.ratio;
                    addResource(propellant.id, propellant.currentRequirement, maxreq);
                }

                if (!e.getFlameoutState)
                {
                    Part p = e.part;

                    double usableFraction = 1;
                    if (e.useVelocityCurve)
                    {
                        usableFraction *= e.velocityCurve.Evaluate((float)(e.part.vessel.orbit.GetVel() - e.part.vessel.mainBody.getRFrmVel(CoM)).magnitude);
                    }

                    float maxThrust = e.maxThrust / (float)e.thrustTransforms.Count;
                    float minThrust = e.minThrust / (float)e.thrustTransforms.Count;

                    double eMaxThrust = minThrust + (maxThrust - minThrust) * e.thrustPercentage / 100f;
                    double eMinThrust = e.throttleLocked ? eMaxThrust : minThrust;
                    double eCurrentThrust = usableFraction * (eMaxThrust * e.currentThrottle + eMinThrust * (1 - e.currentThrottle));

                    for (int i = 0; i < e.thrustTransforms.Count; i++)
                    {
                        PartModule gimbal;
                        GimbalExt gimbalExt = VesselState.getGimbalExt(p, out gimbal);

                        // The rotation makes a +z vector point in the direction that molecules are ejected
                        // from the engine.  The resulting thrust force is in the opposite direction.
                        // This gives us the thrust direction at rest state fro gimbaled engines
                        Vector3d thrustDirectionVector = gimbalExt.initialRot(gimbal, e.thrustTransforms[i], i) * Vector3d.back;
                        // This one would give us the current thrust direction including current gimbal
                        // Not sure which one is the best one to use.
                        //thrustDirectionVector = e.thrustTransforms[i].rotation * Vector3d.back;

                        double cosineLosses = Vector3d.Dot(thrustDirectionVector, e.part.vessel.GetTransform().up);

                        thrustCurrent += eCurrentThrust * cosineLosses * thrustDirectionVector;
                        thrustMax += eMaxThrust * cosineLosses * thrustDirectionVector;
                        thrustMin += eMinThrust * cosineLosses * thrustDirectionVector;

                        Vector3d torque = gimbalExt.torqueVector(gimbal, i, CoM);

                        torqueEngineAvailable.Add(torque * eMinThrust);
                        torqueEngineVariable.Add(torque * (eMaxThrust - eMinThrust));
                        if (!e.throttleLocked)
                        {
                            torqueEngineThrottle += Vector3d.Cross(partPosition, thrustDirectionVector) * (maxThrust - minThrust); // TODO: check
                        }
                    }

                    if (e.useEngineResponseTime)
                    {
                        double responseTime = 1.0 / Math.Min(e.engineAccelerationSpeed, e.engineDecelerationSpeed);
                        if (responseTime > maxResponseTime) maxResponseTime = responseTime;
                    }
                }
            }

            private void addResource(int id, double current /* u/sec */, double max /* u/sec */)
            {
                FuelRequirement req;
                if (resourceRequired.ContainsKey(id))
                {
                    req = resourceRequired[id];
                }
                else
                {
                    req = new FuelRequirement();
                    resourceRequired[id] = req;
                }

                req.requiredLastFrame += current;
                req.requiredAtMaxThrottle += max;
            }
        }

        // Used during the vesselState constructor; distilled to other variables later.
        class IntakeInfo
        {
            public Dictionary<int, List<ModuleResourceIntake>> allIntakes = new Dictionary<int, List<ModuleResourceIntake>>();

            public void addIntake(ModuleResourceIntake intake)
            {
                // TODO: figure out how much airflow we have, how much we could have,
                // drag, etc etc.
                List<ModuleResourceIntake> thelist;
                int id = PartResourceLibrary.Instance.GetDefinition(intake.resourceName).id;
                if (allIntakes.ContainsKey(id))
                {
                    thelist = allIntakes[id];
                }
                else
                {
                    thelist = new List<ModuleResourceIntake>();
                    allIntakes[id] = thelist;
                }
                thelist.Add(intake);
            }

            static List<ModuleResourceIntake> empty = new List<ModuleResourceIntake>();
            public List<ModuleResourceIntake> getIntakes(int id)
            {
                if (allIntakes.ContainsKey(id))
                {
                    return allIntakes[id];
                }
                else
                {
                    return empty;
                }
            }
        }

        // Stored.
        public class ResourceInfo
        {
            public PartResourceDefinition definition;

            // We use kg/s rather than the more common T/s because these numbers tend to be small.
            // One debate I've had is whether to use mass/s or unit/s.  Dunno...

            public double required = 0;              // kg/s
            public double requiredAtMaxThrottle = 0; // kg/s
            public double intakeAvailable = 0;       // kg/s
            public double intakeProvided
            {           // kg/s for currently-open intakes
                get
                {
                    double sum = 0;
                    foreach (var intakeData in intakes)
                    {
                        if (intakeData.intake.intakeEnabled)
                        {
                            sum += intakeData.predictedMassFlow;
                        }
                    }
                    return sum;
                }
            }
            public IntakeData[] intakes;

            public struct IntakeData
            {
                public ModuleResourceIntake intake;
                public double predictedMassFlow; // min kg/s this timestep or next
            }

            // Return the number of kg of resource provided per second under certain conditions.
            // We use kg since the numbers are typically small.
            private double massProvided(double vesselSpeed, Vector3d vesselFwd, double atmDensity,
                    ModuleResourceIntake intake, Vector3d intakeFwd)
            {
                if (intake.checkForOxygen && !FlightGlobals.currentMainBody.atmosphereContainsOxygen)
                {
                    return 0;
                }

                // This is adapted from code shared by Amram at:
                // http://forum.kerbalspaceprogram.com/showthread.php?34288-Maching-Bird-Challeng?p=440505
                // Seems to be accurate for 0.18.2 anyway.
                double intakeSpeed = intake.maxIntakeSpeed; // airspeed when the intake isn't moving

                double aoa = Vector3d.Dot(vesselFwd, intakeFwd);
                if (aoa < 0) { aoa = 0; }
                else if (aoa > 1) { aoa = 1; }

                double finalSpeed;
                if (aoa <= intake.aoaThreshold)
                {
                    finalSpeed = intakeSpeed;
                }
                else
                {
                    // This is labeled as a bug for double-counting intakeSpeed.
                    // It also double-counts unitScalar...
                    double airSpeedGUI = vesselSpeed + intakeSpeed;
                    double airSpeed = airSpeedGUI * intake.unitScalar;
                    finalSpeed = aoa * (airSpeed + intakeSpeed);
                }
                double airVolume = finalSpeed * intake.area * intake.unitScalar;
                double airmass = atmDensity * airVolume; // tonnes per second

                // TODO: limit by the amount the intake can store
                return airmass * 1000;
            }

            public ResourceInfo(PartResourceDefinition r, double req /* u per deltaT */, double atMax /* u per s */, List<ModuleResourceIntake> modules)
            {
                definition = r;
                double density = definition.density * 1000; // kg per unit (density is in T per unit)
                double dT = TimeWarp.fixedDeltaTime;
                required = req * density / dT;
                requiredAtMaxThrottle = atMax * density;

                // For each intake, we want to know the min of what will (or can) be provided either now or at the end of the timestep.
                // 0 means now, 1 means next timestep
                Vector3d v0 = FlightGlobals.ship_srfVelocity;
                Vector3d v1 = v0 + dT * FlightGlobals.ship_acceleration;
                Vector3d v0norm = v0.normalized;
                Vector3d v1norm = v1.normalized;
                double v0mag = v0.magnitude;
                double v1mag = v1.magnitude;

                // As with thrust, here too we should get the static pressure at the intake, not at the center of mass.
                double atmDensity0 = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure());
                float alt1 = (float)(FlightGlobals.ship_altitude
                        + dT * FlightGlobals.ship_verticalSpeed);
                double atmDensity1 = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(alt1));

                intakes = new IntakeData[modules.Count];
                int idx = 0;
                foreach (var intake in modules)
                {
                    Vector3d intakeFwd0 = intake.part.FindModelTransform(intake.intakeTransformName).forward;
                    Vector3d intakeFwd1;
                    {
                        // Rotate the intake by the angular velocity for one timestep, in case the ship is spinning.
                        // Going through the Unity vector classes is about as many lines as hammering it out by hand.
                        Vector3d rot = dT * FlightGlobals.ship_angularVelocity;
                        intakeFwd1 = Quaternion.AngleAxis((float)(Math.PI / 180 * rot.magnitude), rot) * intakeFwd0;
                        /*Vector3d cos;
                        Vector3d sin;
                        for(int i = 0; i < 3; ++i) {
                            cos[i] = Math.Cos (rot[i]);
                            sin[i] = Math.Sin (rot[i]);
                        }
                        intakeFwd1[0]
                            = intakeFwd0[0] * cos[1] * cos[2]
                            + intakeFwd0[1] * (sin[0]*sin[1]*cos[2] - cos[0]*sin[2])
                            + intakeFwd0[2] * (sin[0]*sin[2] + cos[0]*sin[1]);

                        intakeFwd1[1]
                            = intakeFwd0[0] * cos[1] * sin[2]
                            + intakeFwd0[1] * (cos[0]*cos[1] + sin[0]*sin[1]*sin[2])
                            + intakeFwd0[2] * (cos[0]*sin[1]*sin[2] - sin[0]*cos[2]);

                        intakeFwd1[2]
                            = intakeFwd0[0] * (-sin[1])
                            + intakeFwd0[1] * sin[0] * cos[1]
                            + intakeFwd0[2] * cos[0] * cos[1];*/
                    }

                    double mass0 = massProvided(v0mag, v0norm, atmDensity0, intake, intakeFwd0);
                    double mass1 = massProvided(v1mag, v1norm, atmDensity1, intake, intakeFwd1);
                    double mass = Math.Min(mass0, mass1);

                    // Also, we can't have more airflow than what fits in the resource tank of the intake part.
                    double capacity = 0;
                    foreach (PartResource tank in intake.part.Resources)
                    {
                        if (tank.info.id == definition.id)
                        {
                            capacity += tank.maxAmount; // units per timestep
                        }
                    }
                    capacity = capacity * density / dT; // convert to kg/s
                    mass = Math.Min(mass, capacity);

                    intakes[idx].intake = intake;
                    intakes[idx].predictedMassFlow = mass;
                    intakeAvailable += mass;
                    idx++;
                }
            }
        }
    }
}
