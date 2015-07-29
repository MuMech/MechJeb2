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

        public Vector3d orbitalVelocity;
        public Vector3d surfaceVelocity;

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
        [ValueInfoItem("Altitude (true)", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = -1, units = "m")]
        public MovingAverage altitudeTrue = new MovingAverage();
        [ValueInfoItem("Surface altitude ASL", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 4, siMaxPrecision = -1, units = "m")]
        public double surfaceAltitudeASL;

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
        // Variable part of torque related to throttle from engine gimbal
        public Vector3d torqueFromEngine;
        // Variable part of torque related to differential throttle
        public Vector3d torqueFromDiffThrottle;

        //public double massDrag;


        public Vector3d pureDragV;
        [ValueInfoItem("Pure Drag", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")]
        public double pureDrag;

        public Vector3d pureLiftV;
        [ValueInfoItem("Pure Lift", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")]
        public double pureLift;

        // Drag is the force (pureDrag + PureLift) applied opposite of the surface vel
        public double drag;
        // Drag is the force (pureDrag + PureLift) applied in the "Up" direction
        public double dragUp;
        // Lift is the force (pureDrag + PureLift) applied in the "Lift" direction
        public double lift;
        // Lift is the force (pureDrag + PureLift) applied in the "Up" direction
        public double liftUp;


        [ValueInfoItem("Mach", InfoItem.Category.Vessel, format = "F2")]
        public double mach;

        [ValueInfoItem("Speed of sound", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s")]
        public double speedOfSound;

        [ValueInfoItem("Drag Coefficient", InfoItem.Category.Vessel, format = "F2")]
        public double dragCoef;

        // Product of the drag surface area, drag coefficient and the physic multiplers
        public double areaDrag;

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

        // torque available from control surfaces
        public Vector3 ctrlTorqueAvailablePos;
        public Vector3 ctrlTorqueAvailableNeg;

        public Vector3d torqueReactionSpeed;

        // List of parachutes
        public List<ModuleParachute> parachutes;

        public bool parachuteDeployed;

        // Resource information keyed by resource Id.
        public Dictionary<int, ResourceInfo> resources = new Dictionary<int, ResourceInfo>();

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

        public static bool SupportsGimbalExtension<T>() where T : PartModule
        {
            return gimbalExtDict.ContainsKey(typeof(T));
        }

        public static void AddGimbalExtension<T>(GimbalExt gimbalExtension) where T : PartModule
        {
            gimbalExtDict[typeof(T)] = gimbalExtension;
        }

        public void Update(Vessel vessel)
        {
            if (vessel.rigidbody == null) return; //if we try to update before rigidbodies exist we spam the console with NullPointerExceptions.

            TestStuff(vessel);

            UpdateVelocityAndCoM(vessel);

            UpdateBasicInfo(vessel);

            UpdateRCSThrustAndTorque(vessel);

            EngineInfo einfo = new EngineInfo(CoM);
            IntakeInfo iinfo = new IntakeInfo();
            AnalyzeParts(vessel, einfo, iinfo);

            UpdateResourceRequirements(einfo, iinfo);

            ToggleRCSThrust(vessel);

            UpdateMoIAndAngularMom(vessel);
        }


        public DragCubeList cube = new DragCubeList();

        private void TestStuff(Vessel vessel)
        {
            //int partCount = vessel.parts.Count;
            //for (int index = 0; index < partCount; ++index)
            //{
            //    if (!vessel.parts[index].DragCubes.None)
            //        vessel.parts[index].DragCubes.SetDragWeights();
            //}
            //for (int index = 0; index < partCount; ++index)
            //{
            //    if (!vessel.parts[index].DragCubes.None)
            //        vessel.parts[index].DragCubes.SetPartOcclusion();
            //}

            //for (int index = 0; index < partCount; ++index)
            //{
            //    Part part = vessel.parts[index];
            //    if (!part.DragCubes.None)
            //        part.DragCubes.SetDrag(part.dragVectorDirLocal, 0.1f);
            //}

            //cube = new DragCubeList();
            //cube.ClearCubes();

            //for (int index = 0; index < partCount; ++index)
            //{
            //    Part part = vessel.parts[index];
            //    if (!part.DragCubes.None)
            //    {
            //        for (int face = 0; face < 6; face++)
            //        {
            //            //cube.WeightedArea[face] += part.DragCubes.WeightedArea[face];
            //            cube.WeightedDrag[face] += part.DragCubes.WeightedDrag[face];
            //            cube.AreaOccluded[face] += part.DragCubes.AreaOccluded[face];
            //        }
            //    }
            //}
            //
            //cube.SetDrag(vessel.srf_velocity, (float)vessel.mach);
            //
            //double dragScale = cube.AreaDrag * PhysicsGlobals.DragCubeMultiplier;


            //SimulatedVessel simVessel = SimulatedVessel.New(vessel);

            //MechJebCore.print("KPA " + vessel.dynamicPressurekPa.ToString("F9"));

            //Vector3 localVel = vessel.GetTransform().InverseTransformDirection( vessel.srf_velocity );
            //Vector3 localVel = vessel.GetTransform().InverseTransformDirection( vessel.rigidbody.velocity + Krakensbane.GetFrameVelocity());

            //MechJebCore.print(MuUtils.PrettyPrint(localVel));

            //Vector3 simDrag = simVessel.Drag(localVel,
            //    (float)(0.0005 * vessel.atmDensity * vessel.srf_velocity.sqrMagnitude),
            //    (float)vessel.mach);
            //
            //
            //Vector3 simLift = simVessel.Lift(vessel.rigidbody.velocity + Krakensbane.GetFrameVelocity(),
            //    (float)(0.0005 * vessel.atmDensity * vessel.srf_velocity.sqrMagnitude),
            //    (float)vessel.mach);
            //
            //dragScalar = simDrag.magnitude;
            //
            //liftScalar = simLift.magnitude;

            //double exposedArea = 0;
            //double skinExposedArea = 0;
            //double radiativeArea = 0;
            //foreach (Part part in vessel.Parts)
            //{
            //    exposedArea += part.exposedArea;
            //    skinExposedArea += part.skinExposedArea;
            //    radiativeArea += part.radiativeArea;
            //    //MechJebCore.print(part.name + " " + part.exposedArea.ToString("F4") + " " + part.skinExposedArea.ToString("F4"));
            //}
            //MechJebCore.print(exposedArea.ToString("F2") + " " + skinExposedArea.ToString("F2") + " " + radiativeArea.ToString("F2"));

        }


        // TODO memo for later. egg found out that vessel.pos is actually 1 frame in the future while vessel.obt_vel is not.
        // This most likely has some impact on the code.

        // Calculate velocity at the CoM, and the CoM
        // This should no be slower than calling vessel.findWorldCenterOfMass() since
        // the KSP call does the same thing.
        void UpdateVelocityAndCoM(Vessel vessel)
        {
            CoM = Vector3d.zero;
            orbitalVelocity = Vector3d.zero;

            mass = 0;

            torqueAvailable = Vector3d.zero;
            torqueFromEngine = Vector3d.zero;

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                if (p.rb != null)
                {
                    mass += p.rb.mass;

                    CoM = CoM + (p.rb.worldCenterOfMass * p.rb.mass);

                    orbitalVelocity = orbitalVelocity + p.rb.velocity * p.rb.mass;
                }
            }
            CoM = CoM / mass;
            orbitalVelocity = orbitalVelocity / mass + Krakensbane.GetFrameVelocity() + vessel.orbit.GetRotFrameVel(vessel.orbit.referenceBody).xzy;

            if (!MechJebModuleAttitudeController.useCoMVelocity || vessel.packed)
                orbitalVelocity = vessel.obt_velocity;
        }

        // Calculate a bunch of simple quantities each frame.
        void UpdateBasicInfo(Vessel vessel)
        {
            time = Planetarium.GetUniversalTime();
            deltaT = TimeWarp.fixedDeltaTime;

            //CoM = vessel.findWorldCenterOfMass();
            up = (CoM - vessel.mainBody.position).normalized;

            Rigidbody rigidBody = vessel.rootPart.rigidbody;
            if (rigidBody != null) rootPartPos = rigidBody.position;

            north = Vector3d.Exclude(up, (vessel.mainBody.position + vessel.mainBody.transform.up * (float)vessel.mainBody.Radius) - CoM).normalized;
            east = vessel.mainBody.getRFrmVel(CoM).normalized;
            forward = vessel.GetTransform().up;
            rotationSurface = Quaternion.LookRotation(north, up);
            rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * rotationSurface);

            surfaceVelocity = orbitalVelocity - vessel.mainBody.getRFrmVel(CoM);

            velocityMainBodySurface = rotationSurface * surfaceVelocity;

            horizontalOrbit = Vector3d.Exclude(up, orbitalVelocity).normalized;
            horizontalSurface = Vector3d.Exclude(up, surfaceVelocity).normalized;

            angularVelocity = Quaternion.Inverse(vessel.GetTransform().rotation) * vessel.rigidbody.angularVelocity;

            radialPlusSurface = Vector3d.Exclude(surfaceVelocity, up).normalized;
            radialPlus = Vector3d.Exclude(orbitalVelocity, up).normalized;
            normalPlusSurface = -Vector3d.Cross(radialPlusSurface, surfaceVelocity.normalized);
            normalPlus = -Vector3d.Cross(radialPlus, orbitalVelocity.normalized);

            mach = vessel.mach;

            gravityForce = FlightGlobals.getGeeForceAtPosition(CoM);
            localg = gravityForce.magnitude;

            speedOrbital.value = orbitalVelocity.magnitude;
            speedSurface.value = surfaceVelocity.magnitude;
            speedVertical.value = Vector3d.Dot(surfaceVelocity, up);
            speedSurfaceHorizontal.value = Vector3d.Exclude(up, surfaceVelocity).magnitude; //(velocityVesselSurface - (speedVertical * up)).magnitude;
            speedOrbitHorizontal = (orbitalVelocity - (speedVertical * up)).magnitude;

            // Angle of attack, angle between surface velocity and the vessel's "up" vector
            // Originally from ferram4's FAR
            Vector3 tmpVec = vessel.ReferenceTransform.up * Vector3.Dot(vessel.ReferenceTransform.up, surfaceVelocity.normalized)
                           + vessel.ReferenceTransform.forward * Vector3.Dot(vessel.ReferenceTransform.forward, surfaceVelocity.normalized);   //velocity vector projected onto a plane that divides the airplane into left and right halves
            double tmpAoA = 180.0 / Math.PI * Math.Asin(Vector3.Dot(tmpVec.normalized, vessel.ReferenceTransform.forward));
            AoA.value = double.IsNaN(tmpAoA) || speedSurface.value < 0.01 ? 0 : tmpAoA;

            // Angle of Sideslip, angle between surface velocity and the vessel's "right" vector
            // Originally from ferram4's FAR
            tmpVec = vessel.ReferenceTransform.up * Vector3.Dot(vessel.ReferenceTransform.up, surfaceVelocity.normalized)
                   + vessel.ReferenceTransform.right * Vector3.Dot(vessel.ReferenceTransform.right, surfaceVelocity.normalized);     //velocity vector projected onto the vehicle-horizontal plane
            double tempAoS = 180.0 / Math.PI * Math.Asin(Vector3.Dot(tmpVec.normalized, vessel.ReferenceTransform.right));
            AoS.value = double.IsNaN(tempAoS) || speedSurface.value < 0.01 ? 0 : tempAoS;

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
            //if (atmosphericPressure < vessel.mainBody.atmosphereMultiplier * 1e-6) atmosphericPressure = 0;
            double temperature = FlightGlobals.getExternalTemperature(altitudeASL);
            atmosphericDensity = FlightGlobals.getAtmDensity(atmosphericPressure, temperature);
            atmosphericDensityGrams = atmosphericDensity * 1000;
            dynamicPressure = vessel.dynamicPressurekPa * 1000;

            speedOfSound = vessel.speedOfSound;

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
                for (int i = 0; i < Vector6.Values.Length; i++)
                {
                    Vector6.Direction dir6 = Vector6.Values[i];
                    Vector3d dir = Vector6.directions[(int)dir6];
                    double[] throttles;
                    List<RCSSolver.Thruster> thrusters;
                    rcsbal.GetThrottles(dir, out throttles, out thrusters);
                    if (throttles != null)
                    {
                        for (int j = 0; j < throttles.Length; j++)
                        {
                            if (throttles[j] > 0)
                            {
                                Vector3d force = thrusters[j].GetThrust(dir, rot);
                                rcsThrustAvailable.Add(
                                    vessel.GetTransform().InverseTransformDirection(dir * Vector3d.Dot(force * throttles[j], dir)));
                                // Are we missing an rcsTorqueAvailable calculation here?
                            }
                        }
                    }
                }
            }

            Vector3d movingCoM = CoM + (vessel.rb_velocity * Time.fixedDeltaTime);

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                for (int m = 0; m < p.Modules.Count; m++)
                {
                    PartModule mod = p.Modules[m];

                    //if (mod.GetType() != typeof(ModuleRCS)) // ignore derived type. ModuleRCSFX is handled in an ext
                    //    continue;


                    if (!(mod is ModuleRCS))
                        continue;

                    ModuleRCS rcs = (ModuleRCS)mod;
                    if (!p.ShieldedFromAirstream && rcs.rcsEnabled && rcs.isEnabled && !rcs.isJustForShow)
                    {
                        // rcsTorqueAvailable:
                        for (int j = 0; j < rcs.thrusterTransforms.Count; j++)
                        {
                            Transform t = rcs.thrusterTransforms[j];
                            Vector3d thrusterPosition = t.position - movingCoM;

                            float power = rcs.thrusterPower;

                            if (FlightInputHandler.fetch.precisionMode)
                            {
                                float lever = rcs.GetLeverDistance(-t.up, thrusterPosition);
                                if (lever > 1)
                                {
                                    power = power / lever;
                                }
                            }

                            Vector3d thrusterThrust = -t.up * power;
                            // This is a cheap hack to get rcsTorque with the RCS balancer active.
                            if (!rcsbal.enabled)
                            {
                                rcsThrustAvailable.Add(vessel.GetTransform().InverseTransformDirection(thrusterThrust));
                            }
                            Vector3d thrusterTorque = Vector3.Cross(thrusterPosition, thrusterThrust);
                            // Convert in vessel local coordinate
                            rcsTorqueAvailable.Add(vessel.GetTransform().InverseTransformDirection(thrusterTorque));
                        }
                    }
                }
            }
        }

        // Loop over all the parts in the vessel and calculate some things.
        void AnalyzeParts(Vessel vessel, EngineInfo einfo, IntakeInfo iinfo)
        {
            parachutes = new List<ModuleParachute>();
            parachuteDeployed = false;

            torqueAvailable = Vector3d.zero;
            torqueFromEngine = Vector3d.zero;

            ctrlTorqueAvailablePos = new Vector3();
            ctrlTorqueAvailableNeg = new Vector3();

            torqueReactionSpeed = new Vector3();

            pureDragV = Vector3d.zero;
            pureLiftV = Vector3d.zero;

            dragCoef = 0;
            areaDrag = 0;


            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];

                pureDragV += -p.dragVectorDir * p.dragScalar;

                if (!p.hasLiftModule)
                {
                    Vector3 bodyLift = p.transform.rotation * (p.bodyLiftScalar * p.DragCubes.LiftForce);
                    bodyLift = Vector3.ProjectOnPlane(bodyLift, -p.dragVectorDir);
                    pureLiftV += bodyLift;
                }

                //#warning while this works for real time it does not help for simulations. Need to get a coef even while in vacum
                //if (p.dynamicPressurekPa > 0 && PhysicsGlobals.DragMultiplier > 0)
                //    dragCoef += p.simDragScalar / (p.dynamicPressurekPa * PhysicsGlobals.DragMultiplier);

                dragCoef += p.DragCubes.DragCoeff;
                areaDrag += p.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier * PhysicsGlobals.DragMultiplier;

                for (int index = 0; index < vesselStatePartExtensions.Count; index++)
                {
                    VesselStatePartExtension vspe = vesselStatePartExtensions[index];
                    vspe(p);
                }

                for (int m = 0; m < p.Modules.Count; m++)
                {
                    PartModule pm = p.Modules[m];
                    if (!pm.isEnabled)
                    {
                        continue;
                    }

                    if (pm is ModuleReactionWheel)
                    {
                        ModuleReactionWheel rw = (ModuleReactionWheel)pm;
                        if (rw.wheelState == ModuleReactionWheel.WheelState.Active && rw.operational)
                        {
                            torqueAvailable += new Vector3d(rw.PitchTorque, rw.RollTorque, rw.YawTorque);
                        }
                    }
                    else if (pm is ModuleEngines)
                    {
                        einfo.AddNewEngine(pm as ModuleEngines, p.transform.position - CoM);
                    }
                    else if (pm is ModuleResourceIntake)
                    {
                        iinfo.addIntake(pm as ModuleResourceIntake);
                    }
                    else if (pm is ModuleParachute)
                    {
                        ModuleParachute parachute = pm as ModuleParachute;

                        parachutes.Add(parachute);
                        if (parachute.deploymentState == ModuleParachute.deploymentStates.DEPLOYED ||
                            parachute.deploymentState == ModuleParachute.deploymentStates.SEMIDEPLOYED)
                        {
                            parachuteDeployed = true;
                        }
                    }
                    else if (pm is ModuleAeroSurface)
                    {
                        // TODO ...
                    }
                    else if (pm is ModuleControlSurface)
                    {
                        ModuleControlSurface cs = (pm as ModuleControlSurface);

                        if (p.ShieldedFromAirstream || cs.deploy)
                            continue;

                        pureLiftV += cs.liftForce;
                        pureDragV += cs.dragForce;

                        Vector3d partPosition = p.Rigidbody.worldCenterOfMass - CoM;

                        // Build a vector that show if the surface is left/right forward/back up/down of the CoM.
                        Vector3 relpos = vessel.transform.InverseTransformDirection(partPosition);
                        float inverted = relpos.y > 0.01 ? -1 : 1;
                        relpos.x = cs.ignorePitch ? 0 : inverted * (relpos.x < 0.01 ? -1 : 1);
                        relpos.y = cs.ignoreRoll ? 0 : inverted;
                        relpos.z = cs.ignoreYaw ? 0 : inverted * (relpos.z < 0.01 ? -1 : 1);

                        Vector3 velocity = p.Rigidbody.GetPointVelocity(cs.transform.position) + Krakensbane.GetFrameVelocityV3f();

                        Vector3 nVel;
                        Vector3 liftVector;
                        float liftDot;
                        float absDot;
                        cs.SetupCoefficients(velocity, p.atmDensity, out nVel, out liftVector, out liftDot, out absDot);

                        Quaternion maxRotation = Quaternion.AngleAxis(cs.ctrlSurfaceRange, cs.transform.rotation * Vector3.right);

                        double dynPressurePa = p.dynamicPressurekPa * 1000;

                        float mach = (float)p.machNumber;

                        Vector3 posDeflection = maxRotation * liftVector;
                        float liftDotPos = Vector3.Dot(nVel, posDeflection);
                        absDot = Mathf.Abs(liftDotPos);

                        Vector3 liftForcePos = cs.GetLiftVector(posDeflection, liftDotPos, absDot, dynPressurePa, mach) * cs.ctrlSurfaceArea;
                        Vector3 ctrlTorquePos = Vector3.Scale(vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, liftForcePos)), relpos);

                        Vector3 negsDeflection = Quaternion.Inverse(maxRotation) * liftVector;
                        float liftDotNeg = Vector3.Dot(nVel, negsDeflection);
                        absDot = Mathf.Abs(liftDotPos);
                        Vector3 liftForceNeg = cs.GetLiftVector(negsDeflection, liftDotNeg, absDot, dynPressurePa, mach) * cs.ctrlSurfaceArea;
                        Vector3 ctrlTorqueNeg = Vector3.Scale(vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, liftForceNeg)), relpos);

                        ctrlTorqueAvailablePos += ctrlTorquePos;
                        ctrlTorqueAvailableNeg += ctrlTorqueNeg;

                        torqueReactionSpeed += (Mathf.Abs(cs.ctrlSurfaceRange) / cs.actuatorSpeed) * new Vector3(
                            (Mathf.Abs(ctrlTorquePos.x) + Mathf.Abs(ctrlTorqueNeg.x)) / 2f,
                            (Mathf.Abs(ctrlTorquePos.y) + Mathf.Abs(ctrlTorqueNeg.y)) / 2f,
                            (Mathf.Abs(ctrlTorquePos.z) + Mathf.Abs(ctrlTorqueNeg.z)) / 2f);

                    }
                    else if (pm is ModuleLiftingSurface)
                    {
                        ModuleLiftingSurface liftingSurface = (ModuleLiftingSurface)pm;
                        pureLiftV += liftingSurface.liftForce;
                        pureDragV += liftingSurface.dragForce;
                    }

                    for (int index = 0; index < vesselStatePartModuleExtensions.Count; index++)
                    {
                        VesselStatePartModuleExtension vspme = vesselStatePartModuleExtensions[index];
                        vspme(pm);
                    }
                }
            }


            torqueAvailable += new Vector3(
                (Mathf.Abs(ctrlTorqueAvailablePos.x) + Mathf.Abs(ctrlTorqueAvailableNeg.x)) / 2f,
                (Mathf.Abs(ctrlTorqueAvailablePos.y) + Mathf.Abs(ctrlTorqueAvailableNeg.y)) / 2f,
                (Mathf.Abs(ctrlTorqueAvailablePos.z) + Mathf.Abs(ctrlTorqueAvailableNeg.z)) / 2f);

            if (torqueAvailable.sqrMagnitude > 0)
                torqueReactionSpeed.Scale(torqueAvailable.Invert());


            torqueAvailable += Vector3d.Max(rcsTorqueAvailable.positive, rcsTorqueAvailable.negative); // Should we use Max or Min ?

            torqueAvailable += Vector3d.Max(einfo.torqueEngineAvailable.positive, einfo.torqueEngineAvailable.negative);

            torqueFromDiffThrottle = Vector3d.Max(einfo.torqueDiffThrottle.positive, einfo.torqueDiffThrottle.negative);
            torqueFromDiffThrottle.y = 0;

            torqueFromEngine += Vector3d.Max(einfo.torqueEngineVariable.positive, einfo.torqueEngineVariable.negative);




            //MechJebCore.print(" thrustMax "  +einfo.thrustMax);

            thrustVectorMaxThrottle = einfo.thrustMax;
            thrustVectorMinThrottle = einfo.thrustMin;
            thrustVectorLastFrame = einfo.thrustCurrent;

            pureDragV = pureDragV / mass;
            pureLiftV = pureLiftV / mass;

            pureDrag = pureDragV.magnitude;

            pureLift = pureLiftV.magnitude;


            Vector3d force = pureDragV + pureLiftV;
            Vector3d liftDir = -Vector3d.Cross(vessel.transform.right, -surfaceVelocity.normalized);

            // Drag is the part (pureDrag + PureLift) applied opposite of the surface vel
            drag = Vector3d.Dot(force, -surfaceVelocity.normalized);
            // Drag is the part (pureDrag + PureLift) applied in the "Up" direction
            dragUp = Vector3d.Dot(pureDragV, up);
            // Lift is the part (pureDrag + PureLift) applied in the "Lift" direction
            lift = Vector3d.Dot(force, liftDir);
            // Lift is the part (pureDrag + PureLift) applied in the "Up" direction
            liftUp = Vector3d.Dot(force, up);

            maxEngineResponseTime = einfo.maxResponseTime;
        }

        void UpdateResourceRequirements(EngineInfo einfo, IntakeInfo iinfo)
        {
            // Convert the resource information from the einfo and iinfo format
            // to the more useful ResourceInfo format.
            resources.Clear();
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

            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part p = vessel.parts[index];
                Rigidbody rigidbody = p.Rigidbody;
                if (rigidbody == null)
                {
                    continue;
                }

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
                float partMass = rigidbody.mass;
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

            return Math.Sqrt((2000 * mass * localg) / (areaDrag * vesselRef.atmDensity));
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
            if (vessel == null || vessel.rigidbody == null) return 0;
            double ret = altitudeTrue;
            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
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
                    if (partAltitudeBottom < ret)
                    {
                        ret = partAltitudeBottom;
                    }
                }
            }
            return ret;
        }

        internal static GimbalExt getGimbalExt(Part p, out PartModule pm)
        {
            for (int i = 0; i < p.Modules.Count; i++)
            {
                PartModule m = p.Modules[i];
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
            return gimbal.initRots.Any();
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
            double radius = Vector3.ProjectOnPlane(position, Vector3.Project(position, p.vessel.ReferenceTransform.up)).magnitude;

            torque.x = Math.Sin(Math.Abs(gimbal.gimbalRange) * Math.PI / 180d) * distance;
            torque.z = Math.Sin(Math.Abs(gimbal.gimbalRange) * Math.PI / 180d) * distance;

            // The "(e.part.vessel.rb_velocity * Time.fixedDeltaTime)" makes no sense to me but that's how the game does it...
            Vector3d position2 = position + (p.vessel.rb_velocity * Time.fixedDeltaTime);
            Vector3d radialAxis = Vector3.ProjectOnPlane(position2, Vector3.Project(position2, p.vessel.ReferenceTransform.up));
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
            public Vector6 torqueDiffThrottle = new Vector6();

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
                atmP0 = (float)(FlightGlobals.getStaticPressure() * PhysicsGlobals.KpaToAtmospheres);  // TODO : more FlightGlobals call to remove
                float alt1 = (float)(FlightGlobals.ship_altitude + TimeWarp.fixedDeltaTime * FlightGlobals.ship_verticalSpeed);
                atmP1 = (float)(FlightGlobals.getStaticPressure(alt1) * PhysicsGlobals.KpaToAtmospheres);
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
                float Isp0 = e.atmosphereCurve.Evaluate(atmP0);
                float Isp1 = e.atmosphereCurve.Evaluate(atmP1);
                float Isp = Mathf.Min(Isp0, Isp1);

                for (int i = 0; i < e.propellants.Count; i++)
                {
                    var propellant = e.propellants[i];
                    double maxreq = e.maxFuelFlow * propellant.ratio;
                    addResource(propellant.id, propellant.currentRequirement, maxreq);
                }

                if (!e.getFlameoutState)
                {
                    Part p = e.part;

                    float thrustLimiter = e.thrustPercentage / 100f;

                    float maxThrust = e.maxFuelFlow * e.flowMultiplier * Isp * e.g / e.thrustTransforms.Count;
                    float minThrust = e.minFuelFlow * e.flowMultiplier * Isp * e.g / e.thrustTransforms.Count;

                    //MechJebCore.print(maxThrust.ToString("F2") + " " + minThrust.ToString("F2") + " " + e.minFuelFlow.ToString("F2") + " " + e.maxFuelFlow.ToString("F2") + " " + e.flowMultiplier.ToString("F2") + " " + Isp.ToString("F2") + " " + thrustLimiter.ToString("F3"));

                    double eMaxThrust = minThrust + (maxThrust - minThrust) * thrustLimiter;
                    double eMinThrust = e.throttleLocked ? eMaxThrust : minThrust;
                    // currentThrottle include the thrustLimiter
                    //double eCurrentThrust = usableFraction * (eMaxThrust * e.currentThrottle / thrustLimiter + eMinThrust * (1 - e.currentThrottle / thrustLimiter));
                    double eCurrentThrust = e.resultingThrust / e.thrustTransforms.Count;


                    //MechJebCore.print(eMinThrust.ToString("F2") + " " + eMaxThrust.ToString("F2") + " " + eCurrentThrust.ToString("F2"));

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

                        //MechJebCore.print(cosineLosses.ToString("F2") + " " + MuUtils.PrettyPrint(thrustDirectionVector));

                        Vector3d torque = gimbalExt.torqueVector(gimbal, i, CoM);

                        torqueEngineAvailable.Add(torque * eMinThrust);
                        torqueEngineVariable.Add(torque * (eCurrentThrust - eMinThrust));
                        if (!e.throttleLocked)
                        {
                            // TODO : use eCurrentThrust instead of maxThrust and change the relevant code in MechJebModuleThrustController for the Differential throttle
                            torqueDiffThrottle.Add(e.vessel.transform.rotation.Inverse() * Vector3d.Cross(partPosition, thrustDirectionVector) * (maxThrust - minThrust));
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
                    for (int i = 0; i < intakes.Length; i++)
                    {
                        var intakeData = intakes[i];
                        if (intakeData.intake.intakeEnabled)
                        {
                            sum += intakeData.predictedMassFlow;
                        }
                    }
                    return sum;
                }
            }
            public IntakeData[] intakes = new IntakeData[0];

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
                double atmDensity0 = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(), FlightGlobals.getExternalTemperature());
                float alt1 = (float)(FlightGlobals.ship_altitude + dT * FlightGlobals.ship_verticalSpeed);
                double atmDensity1 = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(alt1), FlightGlobals.getExternalTemperature(alt1));

                intakes = new IntakeData[modules.Count];
                int idx = 0;
                for (int index = 0; index < modules.Count; index++)
                {
                    var intake = modules[index];
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
                    for (int i = 0; i < intake.part.Resources.Count; i++)
                    {
                        PartResource tank = intake.part.Resources[i];
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
