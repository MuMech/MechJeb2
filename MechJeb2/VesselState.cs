﻿using System;
using System.Collections.Generic;
using System.Linq;
using Smooth.Pools;
using UnityEngine;
using System.Reflection;

namespace MuMech
{
    public class VesselState
    {
        public static bool isLoadedProceduralFairing = false;
        public static bool isLoadedRealFuels = false;
        // RealFuels.ModuleEngineRF propellantStatus field to call via reflection
        private static FieldInfo RFPropStatusField;
        // stableUllage is always true without RealFuels installed
        public bool stableUllage { get { return this.einfo.stableUllage; } }

        private Vessel vesselRef = null;

        private EngineInfo einfo = new EngineInfo();
        private IntakeInfo iinfo = new IntakeInfo();
        public readonly List<EngineWrapper> enginesWrappers = new List<EngineWrapper>();

        [ValueInfoItem("Universal Time", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)]
        public double time;            //planetarium time
        public double deltaT;          //TimeWarp.fixedDeltaTime

        public Vector3d CoM;
        //Matrix3x3f inertiaTensor = new Matrix3x3f();
        public Vector3d MoI; //Diagonal components of the inertia tensor (almost always the dominant components)
        public Vector3d calcMoI; //Internal calculation of MOI vs. stock
        
        public bool useStockRCSTorque = false;
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
        
        public MovingAverage3d angularVelocityAvg = new MovingAverage3d(5);

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
        
        public Vector3d CoT;
        public Vector3d DoT;
        public double CoTScalar;        


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

        public Vector3d CoL;
        public double CoLScalar;


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
        [ValueInfoItem("Dynamic pressure", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "Pa")]
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

        public Vector6 rcsThrustAvailable = new Vector6(); // thrust available from RCS thrusters
        public Vector6 rcsTorqueAvailable = new Vector6(); // torque available from RCS thrusters 

        // Total torque
        public Vector3d torqueAvailable;
        
        public Vector3d torqueReactionSpeed;

        // Torque from different components
        public Vector6 torqueReactionWheel = new Vector6();  // torque available from Reaction wheels
        public Vector6 torqueRcs = new Vector6();            // torque available from RCS from stock code (not working properly ATM)
        public Vector6 torqueControlSurface = new Vector6(); // torque available from Aerodynamic control surfaces
        public Vector6 torqueGimbal = new Vector6();         // torque available from Gimbaled engines
        public Vector6 torqueOthers = new Vector6();         // torque available from Mostly FAR

        // Variable part of torque related to differential throttle
        public Vector3d torqueDiffThrottle;

        // List of parachutes
        public List<ModuleParachute> parachutes = new List<ModuleParachute>();

        public bool parachuteDeployed;

        // Resource information keyed by resource Id.
        public Dictionary<int, ResourceInfo> resources = new Dictionary<int, ResourceInfo>();

        public CelestialBody mainBody;

        // A convenient debug message to display in the UI
        public static string message;
        [GeneralInfoItem("Debug String", InfoItem.Category.Misc, showInEditor = true)]
        public void DebugString()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(message);
            GUILayout.EndVertical();
        }

        // Callbacks for external module
        public delegate void VesselStatePartExtension(Part p);
        public delegate void VesselStatePartModuleExtension(PartModule pm);

        private Dictionary<ModuleEngines, ModuleGimbal> engines = new Dictionary<ModuleEngines, ModuleGimbal>();

        public List<VesselStatePartExtension> vesselStatePartExtensions = new List<VesselStatePartExtension>();
        public List<VesselStatePartModuleExtension> vesselStatePartModuleExtensions = new List<VesselStatePartModuleExtension>();
        public delegate double DTerminalVelocity();

        static VesselState()
        {
            isLoadedProceduralFairing = isAssemblyLoaded("ProceduralFairings");
            isLoadedRealFuels = isAssemblyLoaded("RealFuels");
            if (isLoadedRealFuels)
            {
                RFPropStatusField = getFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "propellantStatus");
                if (RFPropStatusField == null)
                {
                    Debug.Log("BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no propellantStatus field, disabling RF");
                    isLoadedRealFuels = false;
                }
            }
        }

        static bool isAssemblyLoaded(string assemblyName)
        {
            foreach (AssemblyLoader.LoadedAssembly assembly in AssemblyLoader.loadedAssemblies)
            {
                try
                {
                    if (assembly.assembly.GetName().Name == assemblyName)
                        return true;
                }
                catch (InvalidOperationException)
                {
                    // Silently drop exception generated by users who manage to put assembly that 
                    // can't load for reasons (missing deps most of the time)
                }
            }
            return false;
        }

        static FieldInfo getFieldByReflection(String assemblyString, String className, String fieldName) {
                string assemblyName = "";

                foreach (AssemblyLoader.LoadedAssembly loaded in AssemblyLoader.loadedAssemblies)
                {
                    if (loaded.assembly.GetName().Name == assemblyString)
                    {
                        assemblyName = loaded.assembly.FullName;
                    }
                }

                if (assemblyName == "")
                {
                    return null;
                }

                Type type = Type.GetType("RealFuels.ModuleEnginesRF, " + assemblyName);

                if (type == null)
                {
                    return null;
                }

                return type.GetField("propellantStatus");
        }


        public VesselState()
        {
            TerminalVelocityCall = TerminalVelocityStockKSP;
        }

        //public static bool SupportsGimbalExtension<T>() where T : PartModule
        //{
        //    return gimbalExtDict.ContainsKey(typeof(T));
        //}
        //
        //public static void AddGimbalExtension<T>(GimbalExt gimbalExtension) where T : PartModule
        //{
        //    gimbalExtDict[typeof(T)] = gimbalExtension;
        //}

        public bool Update(Vessel vessel)
        {
            if (vessel.rootPart.rb == null) return false; //if we try to update before rigidbodies exist we spam the console with NullPointerExceptions.

            TestStuff(vessel);

            UpdateVelocityAndCoM(vessel);

            UpdateBasicInfo(vessel);

            UpdateRCSThrustAndTorque(vessel);

            enginesWrappers.Clear();

            einfo.Update(CoM, vessel);
            iinfo.Update();
            AnalyzeParts(vessel, einfo, iinfo);

            UpdateResourceRequirements(einfo, iinfo);

            ToggleRCSThrust(vessel);

            UpdateMoIAndAngularMom(vessel);
            return true;
        }
        
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

            //message = "\nPools :\n" +
            //          SimulatedVessel.PoolSize + " SimulatedVessel\n" +
            //          SimulatedPart.PoolSize +                  " SimulatedPart\n" +
            //          SimulatedParachute.PoolSize +             " SimulatedParachute\n" +
            //          ListPool<AbsoluteVector>.Instance.Size +  " AbsoluteVector\n" +
            //          ReentrySimulation.PoolSize +              " ReentrySimulation\n" +
            //          ReentrySimulation.Result.PoolSize + " Result\n" +
            //          SimulatedPart.DragCubePool.Instance.Size + " DragCubePool\n" +
            //          FuelNode.PoolSize + " FuelNode\n";

            //ListPool<AbsoluteVector>.Instance.
        }


        // TODO memo for later. egg found out that vessel.pos is actually 1 frame in the future while vessel.obt_vel is not.
        // This should have changed in 1.1
        // This most likely has some impact on the code.

        
        void UpdateVelocityAndCoM(Vessel vessel)
        {
            mass = vessel.totalMass;
            CoM = vessel.CoMD;
            orbitalVelocity = vessel.velocityD + vessel.orbit.GetRotFrameVel(vessel.orbit.referenceBody).xzy;
        }

        // Calculate a bunch of simple quantities each frame.
        void UpdateBasicInfo(Vessel vessel)
        {
            time = Planetarium.GetUniversalTime();
            deltaT = TimeWarp.fixedDeltaTime;

            //CoM = °;
            up = (CoM - vessel.mainBody.position).normalized;

            Rigidbody rigidBody = vessel.rootPart.rb;
            if (rigidBody != null) rootPartPos = rigidBody.position;

            north = vessel.north;
            east = vessel.east;
            forward = vessel.GetTransform().up;
            rotationSurface = Quaternion.LookRotation(north, up);
            rotationVesselSurface = Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * rotationSurface);

            surfaceVelocity = orbitalVelocity - vessel.mainBody.getRFrmVel(CoM);

            velocityMainBodySurface = rotationSurface * surfaceVelocity;

            horizontalOrbit = Vector3d.Exclude(up, orbitalVelocity).normalized;
            horizontalSurface = Vector3d.Exclude(up, surfaceVelocity).normalized;

            angularVelocity = Quaternion.Inverse(vessel.GetTransform().rotation) * vessel.rootPart.rb.angularVelocity;

            radialPlusSurface = Vector3d.Exclude(surfaceVelocity, up).normalized;
            radialPlus = Vector3d.Exclude(orbitalVelocity, up).normalized;
            normalPlusSurface = -Vector3d.Cross(radialPlusSurface, surfaceVelocity.normalized);
            normalPlus = -Vector3d.Cross(radialPlus, orbitalVelocity.normalized);

            mach = vessel.mach;

            gravityForce = FlightGlobals.getGeeForceAtPosition(CoM); // TODO vessel.gravityForPos or vessel.gravityTrue
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

            if (!vessel.LandedOrSplashed)
            {
                orbitLAN.value = vessel.orbit.LAN;
            }
            else
            {
                orbitLAN.value = -(vessel.transform.position - vessel.mainBody.transform.position).AngleInPlane(Planetarium.Zup.Z, Planetarium.Zup.X);
                orbitTimeToAp.value = 0;
            }

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
            rcsThrustAvailable.Reset();
            rcsTorqueAvailable.Reset();
            torqueRcs.Reset();

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

            Vector3d movingCoM = vessel.CurrentCoM;

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];
                for (int m = 0; m < p.Modules.Count; m++)
                {
                    ModuleRCS rcs = p.Modules[m] as ModuleRCS;

                    if (rcs == null)
                        continue;
                    
                    Vector3 pos;
                    Vector3 neg;
                    rcs.GetPotentialTorque(out pos, out neg);

                    torqueRcs.Add(pos);
                    torqueRcs.Add(neg);


                    if (!p.ShieldedFromAirstream && rcs.rcsEnabled && rcs.isEnabled && !rcs.isJustForShow)
                    {
                        Vector3 attitudeControl = new Vector3(rcs.enablePitch ? 1 : 0, rcs.enableRoll ? 1 : 0, rcs.enableYaw ? 1 : 0);
                        Vector3 translationControl = new Vector3(rcs.enableX ? 1 : 0f, rcs.enableZ ? 1 : 0, rcs.enableY ? 1 : 0);
                        // rcsTorqueAvailable:
                        for (int j = 0; j < rcs.thrusterTransforms.Count; j++)
                        {
                            Transform t = rcs.thrusterTransforms[j];
                            Vector3d thrusterPosition = t.position - movingCoM;
                            Vector3d thrustDirection = rcs.useZaxis ? -t.forward : -t.up;
                            
                            float power = rcs.thrusterPower;
                            
                            if (FlightInputHandler.fetch.precisionMode)
                            {
                                if (rcs.useLever)
                                {
                                    float lever = rcs.GetLeverDistance(t, thrustDirection, movingCoM);
                                    if (lever > 1)
                                    {
                                        power = power / lever;
                                    }
                                }
                                else
                                {
                                    power *= rcs.precisionFactor;
                                }
                            }

                            Vector3d thrusterThrust = thrustDirection * power;
                            // This is a cheap hack to get rcsTorque with the RCS balancer active.
                            if (!rcsbal.enabled)
                            {
                                rcsThrustAvailable.Add(Vector3.Scale(vessel.GetTransform().InverseTransformDirection(thrusterThrust), translationControl));
                            }
                            Vector3d thrusterTorque = Vector3.Cross(thrusterPosition, thrusterThrust);

                            // Convert in vessel local coordinate
                            rcsTorqueAvailable.Add(Vector3.Scale(vessel.GetTransform().InverseTransformDirection(thrusterTorque), attitudeControl));
                        }
                    }
                }
            }
        }


        [GeneralInfoItem("RCS Translation", InfoItem.Category.Vessel, showInEditor = true)]
        public void RCSTranslation()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("RCS Translation");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(rcsThrustAvailable.positive), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Neg", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(rcsThrustAvailable.negative), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("RCS Torque", InfoItem.Category.Vessel, showInEditor = true)]
        public void RCSTorque()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("RCS Torque");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(torqueRcs.positive), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Neg", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(torqueRcs.negative), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        // Loop over all the parts in the vessel and calculate some things.
        void AnalyzeParts(Vessel vessel, EngineInfo einfo, IntakeInfo iinfo)
        {
            parachutes.Clear();
            parachuteDeployed = false;

            torqueAvailable = Vector3d.zero;

            //ctrlTorqueAvailablePos = new Vector3();
            //ctrlTorqueAvailableNeg = new Vector3();

            Vector6 torqueReactionSpeed6 = new Vector6();

            torqueReactionWheel.Reset();
            torqueControlSurface.Reset();
            torqueGimbal.Reset();
            torqueOthers.Reset();

            pureDragV = Vector3d.zero;
            pureLiftV = Vector3d.zero;

            dragCoef = 0;
            areaDrag = 0;

            CoL = Vector3d.zero;
            CoLScalar = 0;

            CoT = Vector3d.zero;
            DoT = Vector3d.zero;
            CoTScalar = 0;

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];

                Vector3d partPureLift = Vector3.zero;
                Vector3d partPureDrag = -p.dragVectorDir * p.dragScalar; 

                if (!p.hasLiftModule)
                {
                    Vector3 bodyLift = p.transform.rotation * (p.bodyLiftScalar * p.DragCubes.LiftForce);
                    partPureLift = Vector3.ProjectOnPlane(bodyLift, -p.dragVectorDir);
                    
                    double liftScale = bodyLift.magnitude;
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

                engines.Clear();

                for (int m = 0; m < p.Modules.Count; m++)
                {
                    PartModule pm = p.Modules[m];
                    if (!pm.isEnabled)
                    {
                        continue;
                    }
                    
                    ModuleLiftingSurface ls = pm as ModuleLiftingSurface;
                    if (ls != null)
                    {
                        partPureLift += ls.liftForce;
                        partPureDrag += ls.dragForce;
                    }

                    ModuleReactionWheel rw = pm as ModuleReactionWheel;
                    if (rw != null)
                    {
                        Vector3 pos;
                        Vector3 neg;
                        rw.GetPotentialTorque(out pos, out neg);

                        torqueReactionWheel.Add(pos);
                        torqueReactionWheel.Add(neg);
                    }
                    else if (pm is ModuleEngines)
                    {
                        var moduleEngines = pm as ModuleEngines;

                        if (!engines.ContainsKey(moduleEngines))
                            engines.Add(moduleEngines, null);
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
                    else if (pm is ModuleControlSurface) // also does ModuleAeroSurface
                    {
                        ModuleControlSurface cs = (pm as ModuleControlSurface);

                        //if (p.ShieldedFromAirstream || cs.deploy)
                        //    continue;

                        Vector3 ctrlTorquePos;
                        Vector3 ctrlTorqueNeg;

                        cs.GetPotentialTorque(out ctrlTorquePos, out ctrlTorqueNeg);

                        torqueControlSurface.Add(ctrlTorquePos);
                        torqueControlSurface.Add(ctrlTorqueNeg);

                        torqueReactionSpeed6.Add(Mathf.Abs(cs.ctrlSurfaceRange) / cs.actuatorSpeed * Vector3d.Max(ctrlTorquePos.Abs(), ctrlTorqueNeg.Abs()));
                    }
                    else if (pm is ModuleGimbal)
                    {
                        ModuleGimbal g = (pm as ModuleGimbal);

                        if (g.engineMultsList == null)
                            g.CreateEngineList();

                        for (int j = 0; j < g.engineMultsList.Count; j++)
                        {
                            var engs = g.engineMultsList[j];
                            for (int k = 0; k < engs.Count; k++)
                            {
                                engines[engs[k].Key] = g;
                            }
                        }

                        Vector3 pos;
                        Vector3 neg;
                        g.GetPotentialTorque(out pos, out neg);

                        torqueGimbal.Add(pos);
                        torqueGimbal.Add(neg);
                        
                        if (g.useGimbalResponseSpeed)
                            torqueReactionSpeed6.Add((Mathf.Abs(g.gimbalRange) / g.gimbalResponseSpeed) * Vector3d.Max(pos.Abs(), neg.Abs()));
                    }
                    else if (pm is ModuleRCS)
                    {
                        // Already handled earlier. Prevent the generic ITorqueProvider to catch it
                    }
                    else if (pm is ITorqueProvider) // All mod that supports it. Including FAR
                    {
                        ITorqueProvider tp = pm as ITorqueProvider;
                    
                        Vector3 pos;
                        Vector3 neg;
                        tp.GetPotentialTorque(out pos, out neg);

                        torqueOthers.Add(pos);
                        torqueOthers.Add(neg);
                    }

                    for (int index = 0; index < vesselStatePartModuleExtensions.Count; index++)
                    {
                        VesselStatePartModuleExtension vspme = vesselStatePartModuleExtensions[index];
                        vspme(pm);
                    }
                }

                foreach (KeyValuePair<ModuleEngines, ModuleGimbal> engine in engines)
                {
                    einfo.AddNewEngine(engine.Key, engine.Value, enginesWrappers, ref CoT, ref DoT, ref CoTScalar);
                    if (isLoadedRealFuels && RFPropStatusField != null)
                    {
                        einfo.CheckUllageStatus(engine.Key);
                    }
                }

                pureDragV += partPureDrag;
                pureLiftV += partPureLift;

                Vector3d partAeroForce = partPureDrag + partPureLift;

                Vector3d partDrag = Vector3d.Project(partAeroForce, -surfaceVelocity);
                Vector3d partLift = partAeroForce - partDrag;
                
                double partLiftScalar = partLift.magnitude;

                if (p.rb != null && partLiftScalar > 0.01)
                {
                    CoLScalar += partLiftScalar;
                    CoL += ((Vector3d)p.rb.worldCenterOfMass + (Vector3d)(p.partTransform.rotation * p.CoLOffset)) * partLiftScalar;
                }
            }
            
            torqueAvailable += Vector3d.Max(torqueReactionWheel.positive, torqueReactionWheel.negative);
            
            torqueAvailable += Vector3d.Max(torqueRcs.positive, torqueRcs.negative); // The stock RCS value is wrong in 1.1.3 (x2)
            //torqueAvailable += Vector3d.Max(rcsTorqueAvailable.positive, rcsTorqueAvailable.negative);

            torqueAvailable += Vector3d.Max(torqueControlSurface.positive, torqueControlSurface.negative);
            
            torqueAvailable += Vector3d.Max(torqueGimbal.positive, torqueGimbal.negative);

            torqueAvailable += Vector3d.Max(torqueOthers.positive, torqueOthers.negative); // Mostly FAR

            torqueDiffThrottle = Vector3d.Max(einfo.torqueDiffThrottle.positive, einfo.torqueDiffThrottle.negative);
            torqueDiffThrottle.y = 0;

            if (torqueAvailable.sqrMagnitude > 0)
            {
                torqueReactionSpeed = Vector3d.Max(torqueReactionSpeed6.positive, torqueReactionSpeed6.negative);
                torqueReactionSpeed.Scale(torqueAvailable.InvertNoNaN());
            }
            else
            {
                torqueReactionSpeed = Vector3d.zero;
            }

            thrustVectorMaxThrottle = einfo.thrustMax;
            thrustVectorMinThrottle = einfo.thrustMin;
            thrustVectorLastFrame = einfo.thrustCurrent;
            
            if (CoTScalar > 0)
                CoT = CoT / CoTScalar;
            DoT = DoT.normalized;
            
            if (CoLScalar > 0)
                CoL = CoL / CoLScalar;

            pureDragV = pureDragV / mass;
            pureLiftV = pureLiftV / mass;

            pureDrag = pureDragV.magnitude;

            pureLift = pureLiftV.magnitude;
            
            Vector3d force = pureDragV + pureLiftV;
            Vector3d liftDir = -Vector3d.Cross(vessel.transform.right, -surfaceVelocity.normalized);

            // Drag is the part (pureDrag + PureLift) applied opposite of the surface vel
            drag = Vector3d.Dot(force, -surfaceVelocity.normalized);
            // DragUp is the part (pureDrag + PureLift) applied in the "Up" direction
            dragUp = Vector3d.Dot(pureDragV, up);
            // Lift is the part (pureDrag + PureLift) applied in the "Lift" direction
            lift = Vector3d.Dot(force, liftDir);
            // LiftUp is the part (pureDrag + PureLift) applied in the "Up" direction
            liftUp = Vector3d.Dot(force, up);

            maxEngineResponseTime = einfo.maxResponseTime;
        }

        [GeneralInfoItem("Torque", InfoItem.Category.Vessel, showInEditor = true)]
        public void TorqueCompare()
        {
            var reactionTorque = Vector3d.Max(torqueReactionWheel.positive, torqueReactionWheel.negative);
            var rcsTorque = Vector3d.Max(rcsTorqueAvailable.positive, rcsTorqueAvailable.negative);
            var controlTorque = Vector3d.Max(torqueControlSurface.positive, torqueControlSurface.negative);
            var gimbalTorque = Vector3d.Max(torqueGimbal.positive, torqueGimbal.negative);
            var diffTorque = Vector3d.Max(einfo.torqueDiffThrottle.positive, einfo.torqueDiffThrottle.negative);
            diffTorque.y = 0;
            var othersTorque = Vector3d.Max(torqueOthers.positive, torqueOthers.negative);

            GUILayout.Label("Torque sources", GuiUtils.LabelNoWrap);
            GUILayout.BeginHorizontal();
            // col 1 MJs
            GUILayout.BeginVertical();
            GUILayout.Label("ReactionWheel", GuiUtils.LabelNoWrap);
            GUILayout.Label("RCS", GuiUtils.LabelNoWrap);
            GUILayout.Label("ControlSurface", GuiUtils.LabelNoWrap);
            GUILayout.Label("Gimbal", GuiUtils.LabelNoWrap);
            GUILayout.Label("Diff Throttle", GuiUtils.LabelNoWrap);
            GUILayout.Label("Others (FAR)", GuiUtils.LabelNoWrap);
            GUILayout.EndVertical();

            // col 2 Stocks
            GUILayout.BeginVertical();
            GUILayout.Label(MuUtils.PrettyPrint(reactionTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.Label(MuUtils.PrettyPrint(rcsTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.Label(MuUtils.PrettyPrint(controlTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.Label(MuUtils.PrettyPrint(gimbalTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.Label(MuUtils.PrettyPrint(diffTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.Label(MuUtils.PrettyPrint(othersTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
        }


        void UpdateResourceRequirements(EngineInfo einfo, IntakeInfo iinfo)
        {
            // Convert the resource information from the einfo and iinfo format
            // to the more useful ResourceInfo format.
            ResourceInfo.Release(resources.Values);
            resources.Clear();
            foreach (var info in einfo.resourceRequired)
            {
                int id = info.Key;
                var req = info.Value;
                resources[id] = ResourceInfo.Borrow(
                        PartResourceLibrary.Instance.GetDefinition(id),
                        req.requiredLastFrame,
                        req.requiredAtMaxThrottle,
                        iinfo.getIntakes(id),
                        vesselRef);
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

        private static readonly Vector3[] unitVectors = { new Vector3(1, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 1) };

        // KSP's calculation of the vessel's moment of inertia is broken.
        // This function is somewhat expensive :(
        // Maybe it can be optimized more.
        void UpdateMoIAndAngularMom(Vessel vessel)
        {
            // stock code + fix
            Matrix4x4 tensor = Matrix4x4.zero;
            Matrix4x4 partTensor = Matrix4x4.identity;
            Matrix4x4 inertiaMatrix = Matrix4x4.identity;
            Matrix4x4 productMatrix = Matrix4x4.identity;

            //invQuat = QuaternionD.Inverse(vessel.ReferenceTransform.rotation * Quaternion.Euler(-90, 0, 0)); // stock
            QuaternionD invQuat = QuaternionD.Inverse(vessel.ReferenceTransform.rotation); // fix the stock bug
            for (int i = 0; i < vessel.parts.Count; ++i)
            {
                Part part = vessel.parts[i];
                if (part.rb != null)
                {
                    KSPUtil.ToDiagonalMatrix2(part.rb.inertiaTensor, ref partTensor);

                    Quaternion rot = (Quaternion)invQuat * part.transform.rotation * part.rb.inertiaTensorRotation;
                    Quaternion inv = Quaternion.Inverse(rot);

                    Matrix4x4 rotMatrix = Matrix4x4.TRS(Vector3.zero, rot, Vector3.one);
                    Matrix4x4 invMatrix = Matrix4x4.TRS(Vector3.zero, inv, Vector3.one);
                    
                    KSPUtil.Add(ref tensor, rotMatrix * partTensor * invMatrix);
                    Vector3 position = vessel.vesselTransform.InverseTransformDirection(part.rb.position - vessel.CoMD);

                    KSPUtil.ToDiagonalMatrix2(part.rb.mass * position.sqrMagnitude, ref inertiaMatrix);
                    KSPUtil.Add(ref tensor, inertiaMatrix);

                    KSPUtil.OuterProduct2(position, -part.rb.mass * position, ref productMatrix);
                    KSPUtil.Add(ref tensor, productMatrix);
                }
            }
            //MoI = vessel.MOI = KSPUtil.Diag(tensor);
            MoI = KSPUtil.Diag(tensor);
            angularMomentum = Vector3d.zero;
            angularMomentum.x = (float)(MoI.x * vessel.angularVelocity.x);
            angularMomentum.y = (float)(MoI.y * vessel.angularVelocity.y);
            angularMomentum.z = (float)(MoI.z * vessel.angularVelocity.z);

            angularVelocityAvg.value = angularVelocity;
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
            if (vessel == null || vessel.rootPart.rb == null) return 0;
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

        // Used during the vesselState constructor; distilled to other
        // variables later.
        public class EngineInfo
        {
            public Vector3d thrustCurrent = new Vector3d(); // thrust at throttle achieved last frame
            public Vector3d thrustMax = new Vector3d(); // thrust at full throttle
            public Vector3d thrustMin = new Vector3d(); // thrust at zero throttle
            public double maxResponseTime = 0;
            public Vector6 torqueDiffThrottle = new Vector6();
            // stableUllage is always true without RealFuels installed
            public bool stableUllage = true;

            public struct FuelRequirement
            {
                public double requiredLastFrame;
                public double requiredAtMaxThrottle;
            }
            public Dictionary<int, FuelRequirement> resourceRequired = new Dictionary<int, FuelRequirement>();

            private Vector3d CoM;
            private float atmP0; // pressure now
            private float atmP1; // pressure after one timestep
            private Queue<Quaternion> rotSave = new Queue<Quaternion>();

            public void Update(Vector3d c, Vessel vessel)
            {
                thrustCurrent = Vector3d.zero;
                thrustMax = Vector3d.zero;
                thrustMin = Vector3d.zero;
                maxResponseTime = 0;

                torqueDiffThrottle.Reset();

                resourceRequired.Clear();

                stableUllage = true;

                CoM = c;

                atmP0 = (float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres);
                float alt1 = (float)(vessel.altitude + TimeWarp.fixedDeltaTime * vessel.verticalSpeed);
                atmP1 = (float)(FlightGlobals.getStaticPressure(alt1) * PhysicsGlobals.KpaToAtmospheres);
            }

            public void CheckUllageStatus(ModuleEngines e)
            {
                // we report stable ullage for an unstable engine which is throttled up, so we let RF kill it
                // instead of having MJ throttle it down.
                if ((!e.EngineIgnited) || (!e.isEnabled) || (e.requestedThrottle > 0.0F))
                {
                    return;
                }

                String propellantStatus;

                try
                {
                    propellantStatus = RFPropStatusField.GetValue(e) as String;
                }
                catch (ArgumentException)
                {
                    // This exception happens when users are using RealFuels, but ullage is disabled.
                    Debug.Log("ArgumentError thrown while getting propellantStatus from RealFuels, ullage integration disabled");
                    RFPropStatusField = null;
                    return;
                }

                if (propellantStatus == null)
                {
                    Debug.Log("BUG: getting propellantStatus from RealFuels casted to null, ullage status likely broken");
                    return;
                }

                if ((propellantStatus != "Nominal") && (propellantStatus != "Very Stable"))
                {
                    stableUllage = false;
                }
            }

            public void AddNewEngine(ModuleEngines e, ModuleGimbal gimbal, List<EngineWrapper> enginesWrappers, ref Vector3d CoT, ref Vector3d DoT, ref double CoTScalar)
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

                if (e.isOperational)
                {
                    Part p = e.part;

                    float thrustLimiter = e.thrustPercentage / 100f;

                    double maxThrust = e.maxFuelFlow * e.flowMultiplier * Isp * e.g;
                    double minThrust = e.minFuelFlow * e.flowMultiplier * Isp * e.g;

                    // RealFuels engines reports as operational even when they are shutdown
                    if (e.finalThrust == 0f && minThrust > 0f)
                        minThrust = maxThrust = 0;

                    //MechJebCore.print(maxThrust.ToString("F2") + " " + minThrust.ToString("F2") + " " + e.minFuelFlow.ToString("F2") + " " + e.maxFuelFlow.ToString("F2") + " " + e.flowMultiplier.ToString("F2") + " " + Isp.ToString("F2") + " " + thrustLimiter.ToString("F3"));

                    double eMaxThrust = minThrust + (maxThrust - minThrust) * thrustLimiter;
                    double eMinThrust = e.throttleLocked ? eMaxThrust : minThrust;
                    double eCurrentThrust = e.finalThrust;

                    rotSave.Clear();
                    
                    // Used for Diff Throttle
                    Vector3d constantForce = Vector3d.zero;
                    Vector3d maxVariableForce = Vector3d.zero;
                    Vector3d constantTorque = Vector3d.zero;
                    Vector3d maxVariableTorque = Vector3d.zero;
                    double currentMaxThrust = maxThrust;
                    double currentMinThrust = minThrust;

                    if (e.throttleLocked)
                    {
                        currentMaxThrust *= thrustLimiter;
                        currentMinThrust = currentMaxThrust;
                    }

                    // Reset gimbals to default rotation
                    if (gimbal != null && !gimbal.gimbalLock)
                    {
                        rotSave.Clear();
                        for (int i = 0; i < gimbal.gimbalTransforms.Count; i++)
                        {
                            Transform gimbalTransform = gimbal.gimbalTransforms[i];
                            rotSave.Enqueue(gimbalTransform.localRotation);
                            gimbalTransform.localRotation = gimbal.initRots[i];
                        }
                    }

                    for (int i = 0; i < e.thrustTransforms.Count; i++)
                    {
                        var transform = e.thrustTransforms[i];
                        // The rotation makes a +z vector point in the direction that molecules are ejected
                        // from the engine.  The resulting thrust force is in the opposite direction.
                        Vector3d thrustDirectionVector = -transform.forward;
                        
                        double cosineLosses = Vector3d.Dot(thrustDirectionVector, e.part.vessel.GetTransform().up);
                        var thrustTransformMultiplier = e.thrustTransformMultipliers[i];
                        var tCurrentThrust = eCurrentThrust * thrustTransformMultiplier;

                        thrustCurrent += tCurrentThrust * cosineLosses * thrustDirectionVector;
                        thrustMax += eMaxThrust * cosineLosses * thrustDirectionVector * thrustTransformMultiplier;
                        thrustMin += eMinThrust * cosineLosses * thrustDirectionVector * thrustTransformMultiplier;
                        
                        CoT += tCurrentThrust * (Vector3d)transform.position;
                        DoT -= tCurrentThrust * thrustDirectionVector;
                        CoTScalar += tCurrentThrust;
                        
                        Quaternion inverseVesselRot = e.part.vessel.ReferenceTransform.rotation.Inverse();
                        Vector3d thrust_dir = inverseVesselRot * thrustDirectionVector;
                        Vector3d pos = inverseVesselRot * (transform.position - CoM);

                        maxVariableForce += (currentMaxThrust - currentMinThrust) * thrust_dir * thrustTransformMultiplier;
                        constantForce += currentMinThrust * thrust_dir * thrustTransformMultiplier;
                        maxVariableTorque += (currentMaxThrust - currentMinThrust) * thrustTransformMultiplier * Vector3d.Cross(pos, thrust_dir);
                        constantTorque += currentMinThrust * thrustTransformMultiplier * Vector3d.Cross(pos, thrust_dir);

                        if (!e.throttleLocked)
                        {
                            torqueDiffThrottle.Add(Vector3d.Cross(pos, thrust_dir) * (float)(maxThrust - minThrust) * thrustTransformMultiplier);
                        }
                    }

                    enginesWrappers.Add(new EngineWrapper(e, constantForce, maxVariableForce, constantTorque, maxVariableTorque));

                    // Restore gimbals rotation
                    if (gimbal != null && !gimbal.gimbalLock)
                    {
                        for (int i = 0; i < gimbal.gimbalTransforms.Count; i++)
                        {
                            gimbal.gimbalTransforms[i].localRotation = rotSave.Dequeue();
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
            public readonly Dictionary<int, List<ModuleResourceIntake>> allIntakes = new Dictionary<int, List<ModuleResourceIntake>>();

            public void Update()
            {
                foreach (List<ModuleResourceIntake> intakes in allIntakes.Values)
                {
                    ListPool<ModuleResourceIntake>.Instance.Release(intakes);
                }
                allIntakes.Clear();
            }

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
                    thelist = ListPool<ModuleResourceIntake>.Instance.Borrow();
                    allIntakes[id] = thelist;
                }
                thelist.Add(intake);
            }

            private static readonly List<ModuleResourceIntake> empty = new List<ModuleResourceIntake>();
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
                    for (int i = 0; i < intakes.Count; i++)
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
            public List<IntakeData> intakes = new List<IntakeData>();

            public struct IntakeData
            {
                public IntakeData(ModuleResourceIntake intake, double predictedMassFlow)
                {
                    this.intake = intake;
                    this.predictedMassFlow = predictedMassFlow;
                }
                public ModuleResourceIntake intake;
                public double predictedMassFlow; // min kg/s this timestep or next
            }

            private static readonly Pool<ResourceInfo> pool = new Pool<ResourceInfo>(Create, Reset);

            public static int PoolSize
            {
                get { return pool.Size; }
            }

            private static ResourceInfo Create()
            {
                return new ResourceInfo();
            }

            public virtual void Release()
            {
                pool.Release(this);
            }

            public static void Release(Dictionary<int, ResourceInfo>.ValueCollection objList)
            {
                foreach (ResourceInfo resourceInfo in objList)
                {
                    resourceInfo.Release();
                }
            }

            private static void Reset(ResourceInfo obj)
            {
                obj.required = 0;
                obj.requiredAtMaxThrottle = 0;
                obj.intakeAvailable = 0;
                obj.intakes.Clear();
            }

            private ResourceInfo()
            {
            }

            public static ResourceInfo Borrow(PartResourceDefinition r, double req /* u per deltaT */, double atMax /* u per s */, List<ModuleResourceIntake> modules, Vessel vessel)
            {
                ResourceInfo resourceInfo = pool.Borrow();
                resourceInfo.Init(r, req /* u per deltaT */, atMax /* u per s */, modules, vessel);
                return resourceInfo;
            }

            private void Init(PartResourceDefinition r, double req /* u per deltaT */, double atMax /* u per s */, List<ModuleResourceIntake> modules, Vessel vessel)
            {
                definition = r;
                double density = definition.density * 1000; // kg per unit (density is in T per unit)
                float dT = TimeWarp.fixedDeltaTime;
                required = req * density / dT;
                requiredAtMaxThrottle = atMax * density;

                // For each intake, we want to know the min of what will (or can) be provided either now or at the end of the timestep.
                // 0 means now, 1 means next timestep
                Vector3d v0 = vessel.srf_velocity;
                Vector3d v1 = v0 + dT * vessel.acceleration;
                Vector3d v0norm = v0.normalized;
                Vector3d v1norm = v1.normalized;
                double v0mag = v0.magnitude;
                double v1mag = v1.magnitude;

                float alt1 = (float)(vessel.altitude + dT * vessel.verticalSpeed);

                double staticPressure1 = vessel.staticPressurekPa;
                double staticPressure2 = FlightGlobals.getStaticPressure(alt1);
                
                // As with thrust, here too we should get the static pressure at the intake, not at the center of mass.
                double atmDensity0 = FlightGlobals.getAtmDensity(staticPressure1, vessel.externalTemperature);
                double atmDensity1 = FlightGlobals.getAtmDensity(staticPressure2, FlightGlobals.getExternalTemperature(alt1));

                double v0speedOfSound = vessel.mainBody.GetSpeedOfSound(staticPressure1, atmDensity0);
                double v1speedOfSound = vessel.mainBody.GetSpeedOfSound(staticPressure2, atmDensity1);

                float v0mach = v0speedOfSound > 0 ? (float)(v0.magnitude / v0speedOfSound) : 0;
                float v1mach = v1speedOfSound > 0 ? (float)(v1.magnitude / v1speedOfSound) : 0;

                intakes.Clear();
                int idx = 0;
                for (int index = 0; index < modules.Count; index++)
                {
                    var intake = modules[index];
                    Vector3d intakeFwd0 = intake.part.FindModelTransform(intake.intakeTransformName).forward; // TODO : replace with the new public field
                    Vector3d intakeFwd1;
                    {
                        // Rotate the intake by the angular velocity for one timestep, in case the ship is spinning.
                        // Going through the Unity vector classes is about as many lines as hammering it out by hand.
                        Vector3 rot = dT * vessel.angularVelocity;
                        intakeFwd1 = Quaternion.AngleAxis((float)(MathExtensions.Rad2Deg * rot.magnitude), rot) * intakeFwd0;
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

                    double mass0 = massProvided(v0mag, v0norm, atmDensity0, staticPressure1, v0mach, intake, intakeFwd0);
                    double mass1 = massProvided(v1mag, v1norm, atmDensity1, staticPressure2, v1mach, intake, intakeFwd1);
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
                    
                    intakes.Add(new IntakeData(intake, mass));
                    
                    idx++;
                }
            }

            // Return the number of kg of resource provided per second under certain conditions.
            // We use kg since the numbers are typically small.
            private double massProvided(double vesselSpeed, Vector3d normVesselSpeed, double atmDensity, double staticPressure, float mach,
                    ModuleResourceIntake intake, Vector3d intakeFwd)
            {
                if ((intake.checkForOxygen && !FlightGlobals.currentMainBody.atmosphereContainsOxygen) || staticPressure < intake.kPaThreshold) // TODO : add the new test (the bool and maybe the attach node ?)
                {
                    return 0;
                }

                // This is adapted from code shared by Amram at:
                // http://forum.kerbalspaceprogram.com/showthread.php?34288-Maching-Bird-Challeng?p=440505
                // Seems to be accurate for 0.18.2 anyway.
                double intakeSpeed = intake.intakeSpeed; // airspeed when the intake isn't moving

                double aoa = Vector3d.Dot(normVesselSpeed, intakeFwd);
                if (aoa < 0) { aoa = 0; }
                else if (aoa > 1) { aoa = 1; }

                double finalSpeed = intakeSpeed + aoa * vesselSpeed;

                double airVolume = finalSpeed * intake.area * intake.unitScalar * intake.machCurve.Evaluate(mach);
                double airmass = atmDensity * airVolume; // tonnes per second

                // TODO: limit by the amount the intake can store
                return airmass * 1000;
            }
        }


        public class EngineWrapper
        {
            public readonly ModuleEngines engine;

            public float thrustRatio
            {
                get
                {
                    return engine.thrustPercentage / 100;
                }
                set
                {
                    engine.thrustPercentage = value * 100;
                }
            }

            private Vector3d _constantForce;
            private Vector3d _maxVariableForce;
            private Vector3d _constantTorque;
            private Vector3d _maxVariableTorque;

            public Vector3d constantForce { get { return _constantForce; } }
            public Vector3d maxVariableForce { get { return _maxVariableForce; } }
            public Vector3d constantTorque { get { return _constantTorque; } }
            public Vector3d maxVariableTorque { get { return _maxVariableTorque; } }

            public EngineWrapper(ModuleEngines module, Vector3d constantForce, Vector3d maxVariableForce, Vector3d constantTorque, Vector3d maxVariableTorque)
            {
                engine = module;
                _constantForce = constantForce;
                _maxVariableForce = maxVariableForce;
                _constantTorque = constantTorque;
                _maxVariableTorque = maxVariableTorque;
            }
        }


    }
}
