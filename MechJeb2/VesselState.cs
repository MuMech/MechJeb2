using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KSP.Localization;
using MechJebLibBindings;
using Smooth.Pools;
using UnityEngine;

namespace MuMech
{
    public class VesselState
    {
        public static bool isLoadedPrincipia;
        public static bool isLoadedProceduralFairing;

        public static bool isLoadedRealFuels;

        // RealFuels.ModuleEngineRF ullageSet field to call via reflection
        public static FieldInfo RFullageSetField;

        // RealFuels.ModuleEngineRF ignitions field to call via reflection
        public static FieldInfo RFignitionsField;

        // RealFuels.ModuleEngineRF ignited field to call via reflection
        public static FieldInfo RFignitedField;

        // RealFuels.ModuleEngineRF ullage field to call via reflection
        public static FieldInfo RFullageField;

        // RealFuels.Ullage.UllageSet GetUllageStability method to call via reflection
        public static MethodInfo RFGetUllageStabilityMethod;

        // lowestUllage is always VeryStable without RealFuels installed
        public double lowestUllage => einfo.lowestUllage;

        public static bool isLoadedFAR;

        private delegate double FARVesselDelegate(Vessel v);

        private static FARVesselDelegate FARVesselDragCoeff;
        private static FARVesselDelegate FARVesselRefArea;
        private static FARVesselDelegate FARVesselTermVelEst;
        private static FARVesselDelegate FARVesselDynPres;

        private delegate void FARCalculateVesselAeroForcesDelegate(Vessel vessel, out Vector3 aeroForce, out Vector3 aeroTorque,
            Vector3 velocityWorldVector, double altitude);

        private static FARCalculateVesselAeroForcesDelegate FARCalculateVesselAeroForces;

        private Vessel vesselRef;

        private readonly EngineInfo          einfo           = new EngineInfo();
        private readonly IntakeInfo          iinfo           = new IntakeInfo();
        public readonly  List<EngineWrapper> enginesWrappers = new List<EngineWrapper>();

        [ValueInfoItem("#MechJeb_UniversalTime", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)] //Universal Time
        public double time;                                                                                //planetarium time

        public double deltaT; //TimeWarp.fixedDeltaTime

        public Vector3d CoM;
        public Vector3d MoI; //Diagonal components of the inertia tensor (almost always the dominant components)

        public Vector3d up;
        public Vector3d north;
        public Vector3d east;
        public Vector3d forward;           //the direction the vessel is pointing
        public Vector3d horizontalOrbit;   //unit vector in the direction of horizontal component of orbit velocity
        public Vector3d horizontalSurface; //unit vector in the direction of horizontal component of surface velocity
        public Vector3d rootPartPos;

        public Quaternion rotationSurface;
        public Quaternion rotationVesselSurface;

        public Vector3d velocityMainBodySurface;

        public Vector3d orbitalVelocity;
        public Vector3d orbitalPosition;
        public Vector3d surfaceVelocity;

        public Vector3d angularVelocity;
        public Vector3d angularMomentum;

        public Vector3d radialPlus;        //unit vector in the plane of up and velocityVesselOrbit and perpendicular to velocityVesselOrbit
        public Vector3d radialPlusSurface; //unit vector in the plane of up and velocityVesselSurface and perpendicular to velocityVesselSurface
        public Vector3d normalPlus;        //unit vector perpendicular to up and velocityVesselOrbit
        public Vector3d normalPlusSurface; //unit vector perpendicular to up and velocityVesselSurface

        public Vector3d gravityForce;

        [ValueInfoItem("#MechJeb_LocalGravity", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "m/s²")] //Local gravity
        public double localg;                                                                                       //magnitude of gravityForce

        //How about changing these so we store the instantaneous values and *also*
        //the smoothed MovingAverages? Sometimes we need the instantaneous value.
        [ValueInfoItem("#MechJeb_OrbitalSpeed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")] //Orbital speed
        public readonly MovingAverage speedOrbital = new MovingAverage();

        [ValueInfoItem("#MechJeb_SurfaceSpeed", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s")] //Surface speed
        public readonly MovingAverage speedSurface = new MovingAverage();

        [ValueInfoItem("#MechJeb_VerticalSpeed", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s")] //Vertical speed
        public readonly MovingAverage speedVertical = new MovingAverage();

        [ValueInfoItem("#MechJeb_SurfaceHorizontalSpeed", InfoItem.Category.Surface, format = ValueInfoItem.SI,
            units = "m/s")] //Surface horizontal speed
        public readonly MovingAverage speedSurfaceHorizontal = new MovingAverage();

        [ValueInfoItem("#MechJeb_OrbitHorizontalSpeed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")] //Orbit horizontal speed
        public double speedOrbitHorizontal;

        [ValueInfoItem("#MechJeb_Heading", InfoItem.Category.Surface, format = "F1", units = "º")] //Heading
        public readonly MovingAverage vesselHeading = new MovingAverage();

        [ValueInfoItem("#MechJeb_Pitch", InfoItem.Category.Surface, format = "F1", units = "º")] //Pitch
        public readonly MovingAverage vesselPitch = new MovingAverage();

        [ValueInfoItem("#MechJeb_Roll", InfoItem.Category.Surface, format = "F1", units = "º")] //Roll
        public readonly MovingAverage vesselRoll = new MovingAverage();

        [ValueInfoItem("#MechJeb_Altitude_ASL", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, units = "m")] //Altitude (ASL)
        public readonly MovingAverage altitudeASL = new MovingAverage();

        [ValueInfoItem("#MechJeb_Altitude_true", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, units = "m")] //Altitude (true)
        public readonly MovingAverage altitudeTrue = new MovingAverage();

        [ValueInfoItem("#MechJeb_SurfaceAltitudeASL", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 4,
            units = "m")] //Surface altitude ASL
        public double surfaceAltitudeASL;

        [ValueInfoItem("#MechJeb_Apoapsis", InfoItem.Category.Orbit, units = "m", format = ValueInfoItem.SI, siSigFigs = 6,
            category = InfoItem.Category.Orbit)] //Apoapsis
        public readonly MovingAverage orbitApA = new MovingAverage();

        [ValueInfoItem("#MechJeb_Periapsis", InfoItem.Category.Orbit, units = "m", format = ValueInfoItem.SI, siSigFigs = 6,
            category = InfoItem.Category.Orbit)] //Periapsis
        public readonly MovingAverage orbitPeA = new MovingAverage();

        [ValueInfoItem("#MechJeb_OrbitalPeriod", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 2,
            category = InfoItem.Category.Orbit)] //Orbital period
        public readonly MovingAverage orbitPeriod = new MovingAverage();

        [ValueInfoItem("#MechJeb_TimeToApoapsis", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 1)] //Time to apoapsis
        public readonly MovingAverage orbitTimeToAp = new MovingAverage();

        [ValueInfoItem("#MechJeb_TimeToPeriapsis", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 1)] //Time to periapsis
        public readonly MovingAverage orbitTimeToPe = new MovingAverage();

        [ValueInfoItem("#MechJeb_LAN", InfoItem.Category.Orbit, format = ValueInfoItem.ANGLE)] //LAN
        public readonly MovingAverage orbitLAN = new MovingAverage();

        [ValueInfoItem("#MechJeb_ArgumentOfPeriapsis", InfoItem.Category.Orbit, format = "F1", units = "º")] //Argument of periapsis
        public readonly MovingAverage orbitArgumentOfPeriapsis = new MovingAverage();

        [ValueInfoItem("#MechJeb_Inclination", InfoItem.Category.Orbit, format = "F3", units = "º")] //Inclination
        public readonly MovingAverage orbitInclination = new MovingAverage();

        [ValueInfoItem("#MechJeb_Eccentricity", InfoItem.Category.Orbit, format = "F3")] //Eccentricity
        public readonly MovingAverage orbitEccentricity = new MovingAverage();

        [ValueInfoItem("#MechJeb_SemiMajorAxis", InfoItem.Category.Orbit, format = ValueInfoItem.SI, siSigFigs = 6, units = "m")] //Semi-major axis
        public readonly MovingAverage orbitSemiMajorAxis = new MovingAverage();

        [ValueInfoItem("Celestial Longitude", InfoItem.Category.Orbit, format = "F3")]
        public readonly MovingAverage celestialLongitude = new MovingAverage();

        [ValueInfoItem("#MechJeb_Latitude", InfoItem.Category.Surface, format = ValueInfoItem.ANGLE_NS)] //Latitude
        public readonly MovingAverage latitude = new MovingAverage();

        [ValueInfoItem("#MechJeb_Longitude", InfoItem.Category.Surface, format = ValueInfoItem.ANGLE_EW)] //Longitude
        public readonly MovingAverage longitude = new MovingAverage();

        [ValueInfoItem("#MechJeb_AngleOfAttack", InfoItem.Category.Misc, format = "F2", units = "º")] //Angle of Attack
        public readonly MovingAverage AoA = new MovingAverage();

        [ValueInfoItem("#MechJeb_AngleOfSideslip", InfoItem.Category.Misc, format = "F2", units = "º")] //Angle of Sideslip
        public readonly MovingAverage AoS = new MovingAverage();

        [ValueInfoItem("#MechJeb_DisplacementAngle", InfoItem.Category.Misc, format = "F2", units = "º")] //Displacement Angle
        public readonly MovingAverage displacementAngle = new MovingAverage();

        public readonly MovingAverage3d angularVelocityAvg = new MovingAverage3d(5);

        // instantaneous values
        public double currentPitch => rotationVesselSurface.eulerAngles.x > 180
            ? 360.0 - rotationVesselSurface.eulerAngles.x
            : -rotationVesselSurface.eulerAngles.x;

        public double currentRoll => rotationVesselSurface.eulerAngles.z > 180
            ? rotationVesselSurface.eulerAngles.z - 360.0
            : rotationVesselSurface.eulerAngles.z;

        public double currentHeading => rotationVesselSurface.eulerAngles.y;

        public double radius; //distance from planet center

        public double mass;

        // Thrust is a vector.  These are in the same frame of reference as forward and other vectors.
        public Vector3d thrustVectorLastFrame;
        public Vector3d thrustVectorMaxThrottle;
        public Vector3d thrustVectorMinThrottle;

        // Thrust in the forward direction (for historical reasons).
        public double thrustAvailable => Vector3d.Dot(thrustVectorMaxThrottle, forward);
        public double thrustMinimum   => Vector3d.Dot(thrustVectorMinThrottle, forward);
        public double thrustCurrent   => Vector3d.Dot(thrustVectorLastFrame, forward);

        // Forward direction of thrust (CoT-CoM).normalized
        // FIXME: this is Vector3d.zero if throttle is zero!
        public Vector3d thrustForward;

        // Acceleration in the forward direction, for when dividing by mass is too complicated.
        public double maxThrustAccel     => thrustAvailable / mass;
        public double minThrustAccel     => thrustMinimum / mass;
        public double currentThrustAccel => thrustCurrent / mass;

        public double maxEngineResponseTime;

        public bool rcsThrust;

        /* the current throttle limit, this may include transient condition such as limiting to zero due to unstable propellants in RF */
        public float throttleLimit = 1;

        /* the fixed throttle limit (i.e. user limited in the GUI), does not include transient conditions as limiting to zero due to unstable propellants in RF */
        public float  throttleFixedLimit = 1;
        public double limitedMaxThrustAccel => maxThrustAccel * throttleFixedLimit + minThrustAccel * (1 - throttleFixedLimit);

        public Vector3d CoT;
        public Vector3d DoT;
        public double   CoTScalar;

        public Vector3d pureDragV;

        [ValueInfoItem("#MechJeb_PureDrag", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")] //Pure Drag
        public double pureDrag;

        public Vector3d pureLiftV;

        [ValueInfoItem("#MechJeb_PureLift", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")] //Pure Lift
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
        public double   CoLScalar;

        [ValueInfoItem("#MechJeb_Mach", InfoItem.Category.Vessel, format = "F2")] //Mach
        public double mach;

        [ValueInfoItem("#MechJeb_SpeedOfSound", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s")] //Speed of sound
        public double speedOfSound;

        [ValueInfoItem("#MechJeb_DragCoefficient", InfoItem.Category.Vessel, format = "F2")] //Drag Coefficient
        public double dragCoef;

        // Product of the drag surface area, drag coefficient and the physic multiplers
        public double areaDrag;

        public double atmosphericDensity;

        [ValueInfoItem("#MechJeb_AtmosphereDensity", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "g/m³")] //Atmosphere density
        public double atmosphericDensityGrams;

        [ValueInfoItem("#MechJeb_MaxDynamicPressure", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "Pa")] //Max dynamic pressure
        public double maxDynamicPressure;

        [ValueInfoItem("#MechJeb_DynamicPressure", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "Pa")] //Dynamic pressure
        public double dynamicPressure;

        [ValueInfoItem("#MechJeb_IntakeAir", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")] //Intake air
        public double intakeAir;

        [ValueInfoItem("#MechJeb_IntakeAirAllIntakes", InfoItem.Category.Vessel, format = ValueInfoItem.SI,
            units = "kg/s")] //Intake air (all intakes open)
        public double intakeAirAllIntakes;

        [ValueInfoItem("#MechJeb_IntakeAirNeeded", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")] //Intake air needed
        public double intakeAirNeeded;

        [ValueInfoItem("#MechJeb_intakeAirAtMax", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")] //Intake air needed (max)
        public double intakeAirAtMax;

        [ValueInfoItem("#MechJeb_AngleToPrograde", InfoItem.Category.Orbit, format = "F2", units = "º")] //Angle to prograde
        public double angleToPrograde;

        [ValueInfoItem("#MechJeb_AerothermalFlux", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "W/m²")] //Aerothermal flux
        public double freeMolecularAerothermalFlux;

        public readonly Vector6 rcsThrustAvailable = new Vector6(); // thrust available from RCS thrusters

        public readonly Vector6 rcsTorqueAvailable = new Vector6(); // torque available from RCS thrusters

        // Total torque
        public Vector3d torqueAvailable;

        public Vector3d torqueReactionSpeed;

        // Torque from different components
        public readonly Vector6 torqueReactionWheel = new Vector6(); // torque available from Reaction wheels

        //public Vector6 torqueRcs = new Vector6();            // torque available from RCS from stock code (not working properly ATM)
        public readonly Vector6 torqueControlSurface = new Vector6(); // torque available from Aerodynamic control surfaces
        public readonly Vector6 torqueGimbal         = new Vector6(); // torque available from Gimbaled engines
        public readonly Vector6 torqueOthers         = new Vector6(); // torque available from Mostly FAR

        // Variable part of torque related to differential throttle
        public Vector3d torqueDiffThrottle;

        // List of parachutes
        public readonly List<ModuleParachute> parachutes = new List<ModuleParachute>();

        public bool parachuteDeployed;

        // Resource information keyed by resource Id.
        public readonly Dictionary<int, ResourceInfo> resources = new Dictionary<int, ResourceInfo>();

        public CelestialBody mainBody;

        // A convenient debug message to display in the UI
        public static string message;

        [GeneralInfoItem("#MechJeb_DebugString", InfoItem.Category.Misc, showInEditor = true)] //Debug String
        public void DebugString()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(message);
            GUILayout.EndVertical();
        }

        // Callbacks for external module
        public delegate void VesselStatePartExtension(Part p);

        public delegate void VesselStatePartModuleExtension(PartModule pm);

        private readonly Dictionary<ModuleEngines, ModuleGimbal> engines = new Dictionary<ModuleEngines, ModuleGimbal>();

        public readonly List<VesselStatePartExtension>       vesselStatePartExtensions       = new List<VesselStatePartExtension>();
        public readonly List<VesselStatePartModuleExtension> vesselStatePartModuleExtensions = new List<VesselStatePartModuleExtension>();

        public delegate double DTerminalVelocity();

        private static bool reflectionInitDone;

        public void InitReflection()
        {
            FARVesselDragCoeff        = null;
            FARVesselRefArea          = null;
            FARVesselTermVelEst       = null;
            FARVesselDynPres          = null;
            isLoadedProceduralFairing = ReflectionUtils.IsAssemblyLoaded("ProceduralFairings");
            isLoadedRealFuels         = ReflectionUtils.IsAssemblyLoaded("RealFuels");
            isLoadedPrincipia         = ReflectionUtils.IsAssemblyLoaded("principia.ksp_plugin_adapter");
            if (isLoadedRealFuels)
            {
                Debug.Log("MechJeb: RealFuels Assembly is loaded");
                RFullageSetField = ReflectionUtils.GetFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "ullageSet");
                if (RFullageSetField == null)
                {
                    Debug.Log("MechJeb BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no ullageSet field, disabling RF");
                    isLoadedRealFuels = false;
                }

                RFGetUllageStabilityMethod = ReflectionUtils.GetMethodByReflection("RealFuels", "RealFuels.Ullage.UllageSet", "GetUllageStability",
                    BindingFlags.Public | BindingFlags.Instance);
                if (RFGetUllageStabilityMethod == null)
                {
                    Debug.Log("MechJeb BUG: RealFuels loaded, but RealFuels.Ullage.UllageSet has no GetUllageStability method, disabling RF");
                    isLoadedRealFuels = false;
                }

                RFignitionsField = ReflectionUtils.GetFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "ignitions");
                if (RFignitionsField == null)
                {
                    Debug.Log("MechJeb BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no ignitions field, disabling RF");
                    isLoadedRealFuels = false;
                }

                RFignitedField = ReflectionUtils.GetFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "ignited",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                if (RFignitedField == null)
                {
                    Debug.Log("MechJeb BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no ignited field, disabling RF");
                    isLoadedRealFuels = false;
                }

                RFullageField = ReflectionUtils.GetFieldByReflection("RealFuels", "RealFuels.ModuleEnginesRF", "ullage");
                if (RFullageField == null)
                {
                    Debug.Log("MechJeb BUG: RealFuels loaded, but RealFuels.ModuleEnginesRF has no ullage field, disabling RF");
                    isLoadedRealFuels = false;
                }

                if (isLoadedRealFuels)
                {
                    Debug.Log("MechJeb: RealFuels Assembly is wired up properly");
                }
            }

            isLoadedFAR = ReflectionUtils.IsAssemblyLoaded("FerramAerospaceResearch");
            if (isLoadedFAR)
            {
                var farNames = new List<string> { "VesselDragCoeff", "VesselRefArea", "VesselTermVelEst", "VesselDynPres" };
                foreach (string name in farNames)
                {
                    MethodInfo methodInfo = ReflectionUtils.GetMethodByReflection(
                        "FerramAerospaceResearch",
                        "FerramAerospaceResearch.FARAPI",
                        name,
                        BindingFlags.Public | BindingFlags.Static,
                        new[] { typeof(Vessel) }
                    );
                    if (methodInfo == null)
                    {
                        Debug.Log("MJ BUG: FAR loaded, but FerramAerospaceResearch.FARAPI has no " + name + " method. Disabling FAR");
                        isLoadedFAR = false;
                    }
                    else
                    {
                        typeof(VesselState).GetField("FAR" + name, BindingFlags.NonPublic | BindingFlags.Static).SetValue(null,
                            (FARVesselDelegate)Delegate.CreateDelegate(typeof(FARVesselDelegate), methodInfo));
                    }
                }

                MethodInfo FARCalculateVesselAeroForcesMethodInfo = ReflectionUtils.GetMethodByReflection(
                    "FerramAerospaceResearch",
                    "FerramAerospaceResearch.FARAPI",
                    "CalculateVesselAeroForces",
                    BindingFlags.Public | BindingFlags.Static,
                    new[] { typeof(Vessel), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(Vector3), typeof(double) }
                );
                if (FARCalculateVesselAeroForcesMethodInfo == null)
                {
                    Debug.Log("MJ BUG: FAR loaded, but FerramAerospaceResearch.FARAPI has no CalculateVesselAeroForces method, disabling FAR");
                    isLoadedFAR = false;
                }
                else
                {
                    FARCalculateVesselAeroForces =
                        (FARCalculateVesselAeroForcesDelegate)Delegate.CreateDelegate(typeof(FARCalculateVesselAeroForcesDelegate),
                            FARCalculateVesselAeroForcesMethodInfo);
                }
            }

            reflectionInitDone = true;
        }

        public VesselState()
        {
            if (!reflectionInitDone && HighLogic.LoadedSceneIsGame)
                InitReflection();
            if (isLoadedFAR)
            {
                TerminalVelocityCall = TerminalVelocityFAR;
            }
            else
            {
                TerminalVelocityCall = TerminalVelocityStockKSP;
            }
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
            if (time == Planetarium.GetUniversalTime())
                return true;

            if (vessel.rootPart.rb == null)
                return false; //if we try to update before rigidbodies exist we spam the console with NullPointerExceptions.

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

        private void UpdateVelocityAndCoM(Vessel vessel)
        {
            mass            = vessel.totalMass;
            CoM             = vessel.CoMD;
            orbitalVelocity = vessel.obt_velocity;
            orbitalPosition = CoM - vessel.mainBody.position;
        }

        // Calculate a bunch of simple quantities each frame.
        private void UpdateBasicInfo(Vessel vessel)
        {
            time   = Planetarium.GetUniversalTime();
            deltaT = TimeWarp.fixedDeltaTime;

            //CoM = °;
            up = orbitalPosition.normalized;

            Rigidbody rigidBody = vessel.rootPart.rb;
            if (rigidBody != null) rootPartPos = rigidBody.position;

            north           = vessel.north;
            east            = vessel.east;
            forward         = vessel.GetTransform().up;
            rotationSurface = Quaternion.LookRotation(north, up);
            rotationVesselSurface =
                Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(vessel.GetTransform().rotation) * rotationSurface);

            surfaceVelocity = orbitalVelocity - vessel.mainBody.getRFrmVel(CoM);

            velocityMainBodySurface = rotationSurface * surfaceVelocity;

            horizontalOrbit   = Vector3d.Exclude(up, orbitalVelocity).normalized;
            horizontalSurface = Vector3d.Exclude(up, surfaceVelocity).normalized;

            angularVelocity = vessel.angularVelocity;

            radialPlusSurface = Vector3d.Exclude(surfaceVelocity, up).normalized;
            radialPlus        = Vector3d.Exclude(orbitalVelocity, up).normalized;
            normalPlusSurface = -Vector3d.Cross(radialPlusSurface, surfaceVelocity.normalized);
            normalPlus        = -Vector3d.Cross(radialPlus, orbitalVelocity.normalized);

            mach = vessel.mach;

            gravityForce = FlightGlobals.getGeeForceAtPosition(CoM); // TODO vessel.gravityForPos or vessel.gravityTrue
            localg       = gravityForce.magnitude;

            speedOrbital.Value  = orbitalVelocity.magnitude;
            speedSurface.Value  = surfaceVelocity.magnitude;
            speedVertical.Value = Vector3d.Dot(surfaceVelocity, up);
            speedSurfaceHorizontal.Value =
                Vector3d.Exclude(up, surfaceVelocity).magnitude; //(velocityVesselSurface - (speedVertical * up)).magnitude;
            speedOrbitHorizontal = (orbitalVelocity - speedVertical * up).magnitude;

            // Angle of Attack, angle between surface velocity and the ship-nose vector (KSP "up" vector) in the plane that has no ship-right/left in it
            var srfProj = Vector3.ProjectOnPlane(surfaceVelocity.normalized, vessel.ReferenceTransform.right);
            double tmpAoA = UtilMath.Rad2Deg * Math.Atan2(Vector3.Dot(srfProj.normalized, vessel.ReferenceTransform.forward),
                Vector3.Dot(srfProj.normalized, vessel.ReferenceTransform.up));
            AoA.Value = double.IsNaN(tmpAoA) || speedSurface.Value < 0.01 ? 0 : tmpAoA;

            // Angle of Sideslip, angle between surface velocity and the ship-nose vector (KSP "up" vector) in the plane that has no ship-top/bottom in it (KSP "forward"/"back")
            srfProj = Vector3.ProjectOnPlane(surfaceVelocity.normalized, vessel.ReferenceTransform.forward);
            double tmpAoS = UtilMath.Rad2Deg * Math.Atan2(Vector3.Dot(srfProj.normalized, vessel.ReferenceTransform.right),
                Vector3.Dot(srfProj.normalized, vessel.ReferenceTransform.up));
            AoS.Value = double.IsNaN(tmpAoS) || speedSurface.Value < 0.01 ? 0 : tmpAoS;

            // Displacement Angle, angle between surface velocity and the ship-nose vector (KSP "up" vector) -- ignores roll of the craft (0 to 180 degrees)
            double tempAoD = UtilMath.Rad2Deg *
                             Math.Acos(MuUtils.Clamp(Vector3.Dot(vessel.ReferenceTransform.up, surfaceVelocity.normalized), -1, 1));
            displacementAngle.Value = double.IsNaN(tempAoD) || speedSurface.Value < 0.01 ? 0 : tempAoD;

            vesselHeading.Value = currentHeading;
            vesselPitch.Value   = currentPitch;
            vesselRoll.Value    = currentRoll;

            altitudeASL.Value = vessel.mainBody.GetAltitude(CoM);

            surfaceAltitudeASL = vessel.mainBody.pqsController != null ? vessel.pqsAltitude : 0d;
            if (vessel.mainBody.ocean && surfaceAltitudeASL < 0) surfaceAltitudeASL = 0;
            altitudeTrue.Value = altitudeASL - surfaceAltitudeASL;

            // altitudeBottom will be recomputed if someone requests it.
            altitudeBottomIsCurrent = false;

            double atmosphericPressure = FlightGlobals.getStaticPressure(altitudeASL, vessel.mainBody);
            //if (atmosphericPressure < vessel.mainBody.atmosphereMultiplier * 1e-6) atmosphericPressure = 0;
            double temperature = FlightGlobals.getExternalTemperature(altitudeASL);
            atmosphericDensity      = FlightGlobals.getAtmDensity(atmosphericPressure, temperature);
            atmosphericDensityGrams = atmosphericDensity * 1000;
            if (isLoadedFAR)
            {
                dynamicPressure = FARVesselDynPres(vessel) * 1000;
            }
            else
            {
                dynamicPressure = vessel.dynamicPressurekPa * 1000;
            }

            if (dynamicPressure > maxDynamicPressure)
                maxDynamicPressure = dynamicPressure;
            freeMolecularAerothermalFlux = 0.5 * atmosphericDensity * speedSurface * speedSurface * speedSurface;


            speedOfSound = vessel.speedOfSound;

            orbitApA.Value      = vessel.orbit.ApA;
            orbitPeA.Value      = vessel.orbit.PeA;
            orbitPeriod.Value   = vessel.orbit.period;
            orbitTimeToAp.Value = vessel.orbit.timeToAp;
            orbitTimeToPe.Value = vessel.orbit.timeToPe;

            orbitLAN.Value = vessel.orbit.LAN;

            orbitArgumentOfPeriapsis.Value = vessel.orbit.argumentOfPeriapsis;
            orbitInclination.Value         = vessel.orbit.inclination;
            orbitEccentricity.Value        = vessel.orbit.eccentricity;
            orbitSemiMajorAxis.Value       = vessel.orbit.semiMajorAxis;
            celestialLongitude.Value       = Planetarium.right.AngleInPlane(-Planetarium.up, orbitalPosition);
            latitude.Value                 = vessel.mainBody.GetLatitude(CoM);
            longitude.Value                = MuUtils.ClampDegrees180(vessel.mainBody.GetLongitude(CoM));

            if (vessel.mainBody != Planetarium.fetch.Sun)
            {
                Vector3d prograde = vessel.mainBody.orbit.getOrbitalVelocityAtUT(time).xzy;
                Vector3d normal = vessel.mainBody.orbit.GetOrbitNormal().xzy;
                angleToPrograde = MuUtils.ClampDegrees360((vessel.orbit.inclination > 90 || vessel.orbit.inclination < -90 ? 1 : -1) *
                                                          orbitalPosition.AngleInPlane(normal, prograde));
            }
            else
            {
                angleToPrograde = 0;
            }

            mainBody = vessel.mainBody;

            radius = orbitalPosition.magnitude;

            vesselRef = vessel;
        }

        private void UpdateRCSThrustAndTorque(Vessel vessel)
        {
            rcsThrustAvailable.Reset();
            rcsTorqueAvailable.Reset();

            //torqueRcs.Reset();

            if (!vessel.ActionGroups[KSPActionGroup.RCS])
                return;

            MechJebModuleRCSBalancer rcsbal = vessel.GetMasterMechJeb().Rcsbal;
            if (rcsbal.Enabled)
            {
                Vector3d rot = Vector3d.zero;
                for (int i = 0; i < Vector6.Values.Length; i++)
                {
                    Vector6.Direction dir6 = Vector6.Values[i];
                    Vector3d dir = Vector6.Directions[(int)dir6];
                    rcsbal.GetThrottles(dir, out double[] throttles, out List<RCSSolver.Thruster> thrusters);
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
                    var rcs = p.Modules[m] as ModuleRCS;

                    if (rcs == null)
                        continue;

                    //Vector3 pos;
                    //Vector3 neg;
                    //rcs.GetPotentialTorque(out pos, out neg);

                    //torqueRcs.Add(pos);
                    //torqueRcs.Add(neg);

                    //if (rcsbal.enabled)
                    //    continue;

                    if (!p.ShieldedFromAirstream && rcs.rcsEnabled && rcs.isEnabled && !rcs.isJustForShow && !rcs.flameout && rcs.rcs_active)
                    {
                        var attitudeControl = new Vector3(rcs.enablePitch ? 1 : 0, rcs.enableRoll ? 1 : 0, rcs.enableYaw ? 1 : 0);

                        var translationControl = new Vector3(rcs.enableX ? 1 : 0f, rcs.enableZ ? 1 : 0, rcs.enableY ? 1 : 0);
                        for (int j = 0; j < rcs.thrusterTransforms.Count; j++)
                        {
                            Transform t = rcs.thrusterTransforms[j];

                            // Borrowed from kOS:  As of KSP 1.11.x, RCS parts now use part variants.  To prevent
                            // counting torque as if the superset of all variant nozzles were present, the ones not
                            // currently active have to be culled out here, since KSP isn't culling them out itself when
                            // it populates ModuleRCS.thrusterTransforms:
                            if (!t.gameObject.activeInHierarchy)
                                continue;

                            Vector3d thrusterPosition = t.position - movingCoM;

                            Vector3d thrustDirection = rcs.useZaxis ? -t.forward : -t.up;

                            float power = rcs.thrusterPower * rcs.thrustPercentage * 0.01f;

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
                            if (!rcsbal.Enabled)
                            {
                                rcsThrustAvailable.Add(Vector3.Scale(vessel.GetTransform().InverseTransformDirection(thrusterThrust),
                                    translationControl));
                            }

                            Vector3d thrusterTorque = Vector3.Cross(thrusterPosition, thrusterThrust);

                            // Convert in vessel local coordinate
                            rcsTorqueAvailable.Add(Vector3.Scale(vessel.GetTransform().InverseTransformDirection(thrusterTorque), attitudeControl));
                            //rcsThrustAvailable.Add(Vector3.Scale(vessel.GetTransform().InverseTransformDirection(thrusterThrust), translationControl));
                        }
                    }
                }
            }
        }

        [GeneralInfoItem("#MechJeb_RCSTranslation", InfoItem.Category.Vessel, showInEditor = true)] //RCS Translation
        public void RCSTranslation()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_RCSTranslation")); //"RCS Translation"
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos", GUILayout.ExpandWidth(true)); //
            GUILayout.Label(MuUtils.PrettyPrint(rcsThrustAvailable.Positive), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Neg", GUILayout.ExpandWidth(true)); //
            GUILayout.Label(MuUtils.PrettyPrint(rcsThrustAvailable.Negative), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_RCSTorque", InfoItem.Category.Vessel, showInEditor = true)] //RCS Torque
        public void RCSTorque()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_RCSTorque")); //"RCS Torque"
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(rcsTorqueAvailable.Positive), GUILayout.ExpandWidth(false));
            //GUILayout.Label(MuUtils.PrettyPrint(torqueRcs.positive), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Neg", GUILayout.ExpandWidth(true));
            GUILayout.Label(MuUtils.PrettyPrint(rcsTorqueAvailable.Negative), GUILayout.ExpandWidth(false));
            //GUILayout.Label(MuUtils.PrettyPrint(torqueRcs.negative), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        //FARCalculateVesselAeroForces(vessel,out farForce,out farTorque,surfaceVelocity,altitudeASL);
        private       Vector3  lastFarForce, lastFarTorque;
        private       Vector3d lastSurfaceVelocity;
        private       double   lastAltitudeASL;
        private       float    lastAoA;
        private const double   _dVelocitySqrThreshold    = 100;
        private const double   _dVelocitySqrMinThreshold = 1;
        private const double   _dAltitudeThreshold       = 300;
        private const float    _fAoAThreshold            = 2;

        private void CalculateVesselAeroForcesWithCache(Vessel v, out Vector3 farForce, out Vector3 farTorque, Vector3d surfaceVelocity,
            double altitudeASL)
        {
            float AoA = Vector3.Angle(v.rootPart.transform.TransformDirection(Vector3.up), surfaceVelocity);
            if ((lastSurfaceVelocity - surfaceVelocity).sqrMagnitude > _dVelocitySqrThreshold
                || Math.Abs(altitudeASL - lastAltitudeASL) > _dAltitudeThreshold
                || (surfaceVelocity.sqrMagnitude > _dVelocitySqrMinThreshold && AoA - lastAoA > _fAoAThreshold))
            {
                FARCalculateVesselAeroForces(v, out farForce, out farTorque, surfaceVelocity, altitudeASL);
                lastSurfaceVelocity = surfaceVelocity;
                lastAltitudeASL     = altitudeASL;
                lastAoA             = AoA;
                lastFarForce        = farForce;
                lastFarTorque       = farTorque;
            }
            else
            {
                farForce  = lastFarForce;
                farTorque = lastFarTorque;
            }
        }

        // Loop over all the parts in the vessel and calculate some things.
        private void AnalyzeParts(Vessel vessel, EngineInfo einfo, IntakeInfo iinfo)
        {
            parachutes.Clear();
            parachuteDeployed = false;

            torqueAvailable = Vector3d.zero;

            var torqueReactionSpeed6 = new Vector6();

            torqueReactionWheel.Reset();
            torqueControlSurface.Reset();
            torqueGimbal.Reset();
            torqueOthers.Reset();

            pureDragV = Vector3d.zero;
            pureLiftV = Vector3d.zero;

            if (isLoadedFAR)
            {
                dragCoef = FARVesselDragCoeff(vessel);
                areaDrag = FARVesselRefArea(vessel) * dragCoef * PhysicsGlobals.DragMultiplier;
            }
            else
            {
                dragCoef = 0;
                areaDrag = 0;
            }

            CoL       = Vector3d.zero;
            CoLScalar = 0;

            CoT           = Vector3d.zero;
            DoT           = Vector3d.zero;
            CoTScalar     = 0;
            thrustForward = Vector3d.zero;

            for (int i = 0; i < vessel.parts.Count; i++)
            {
                Part p = vessel.parts[i];

                Vector3d partPureLift = Vector3.zero;
                Vector3d partPureDrag = -p.dragVectorDir * p.dragScalar;

                if (!p.hasLiftModule)
                {
                    Vector3 bodyLift = p.transform.rotation * (p.bodyLiftScalar * p.DragCubes.LiftForce);
                    partPureLift = Vector3.ProjectOnPlane(bodyLift, -p.dragVectorDir);

                    //double liftScale = bodyLift.magnitude;
                }

                //#warning while this works for real time it does not help for simulations. Need to get a coef even while in vacum
                //if (p.dynamicPressurekPa > 0 && PhysicsGlobals.DragMultiplier > 0)
                //    dragCoef += p.simDragScalar / (p.dynamicPressurekPa * PhysicsGlobals.DragMultiplier);

                if (!isLoadedFAR)
                {
                    dragCoef += p.DragCubes.DragCoeff;
                    areaDrag += p.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier * PhysicsGlobals.DragMultiplier;
                }

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

                    var ls = pm as ModuleLiftingSurface;
                    if (ls != null)
                    {
                        partPureLift += ls.liftForce;
                        partPureDrag += ls.dragForce;
                    }

                    var rw = pm as ModuleReactionWheel;
                    if (rw != null)
                    {
                        rw.GetPotentialTorque(out Vector3 pos, out Vector3 neg);

                        // GetPotentialTorque reports the same value for pos & neg on ModuleReactionWheel
                        torqueReactionWheel.Add(pos);
                        torqueReactionWheel.Add(-neg);
                    }
                    else if (pm is ModuleEngines)
                    {
                        var moduleEngines = pm as ModuleEngines;

                        engines.TryAdd(moduleEngines, null);
                    }
                    else if (pm is ModuleResourceIntake)
                    {
                        iinfo.addIntake(pm as ModuleResourceIntake);
                    }
                    else if (pm is ModuleParachute)
                    {
                        var parachute = pm as ModuleParachute;

                        parachutes.Add(parachute);
                        if (parachute.deploymentState == ModuleParachute.deploymentStates.DEPLOYED ||
                            parachute.deploymentState == ModuleParachute.deploymentStates.SEMIDEPLOYED)
                        {
                            parachuteDeployed = true;
                        }
                    }
                    else if (pm is ModuleControlSurface) // also does ModuleAeroSurface
                    {
                        var cs = pm as ModuleControlSurface;

                        //if (p.ShieldedFromAirstream || cs.deploy)
                        //    continue;

                        cs.GetPotentialTorque(out Vector3 ctrlTorquePos, out Vector3 ctrlTorqueNeg);

                        torqueControlSurface.Add(ctrlTorquePos);
                        torqueControlSurface.Add(ctrlTorqueNeg);

                        torqueReactionSpeed6.Add(Mathf.Abs(cs.ctrlSurfaceRange) / cs.actuatorSpeed *
                                                 Vector3d.Max(ctrlTorquePos.Abs(), ctrlTorqueNeg.Abs()));
                    }
                    else if (pm is ModuleGimbal)
                    {
                        var g = pm as ModuleGimbal;

                        if (g.engineMultsList == null)
                            g.CreateEngineList();

                        for (int j = 0; j < g.engineMultsList.Count; j++)
                        {
                            List<KeyValuePair<ModuleEngines, float>> engs = g.engineMultsList[j];
                            for (int k = 0; k < engs.Count; k++)
                            {
                                engines[engs[k].Key] = g;
                            }
                        }

                        g.GetPotentialTorque(out Vector3 pos, out Vector3 neg);

                        // GetPotentialTorque reports the same value for pos & neg on ModuleGimbal

                        torqueGimbal.Add(pos);
                        torqueGimbal.Add(-neg);

                        if (g.useGimbalResponseSpeed)
                            torqueReactionSpeed6.Add(Mathf.Abs(g.gimbalRange) / g.gimbalResponseSpeed * Vector3d.Max(pos.Abs(), neg.Abs()));
                    }
                    else if (pm is ModuleRCS)
                    {
                        // Already handled earlier. Prevent the generic ITorqueProvider to catch it
                    }
                    else if (pm is ITorqueProvider) // All mod that supports it. Including FAR
                    {
                        var tp = pm as ITorqueProvider;

                        tp.GetPotentialTorque(out Vector3 pos, out Vector3 neg);

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
                    if (isLoadedRealFuels && RFullageSetField != null && RFignitionsField != null && RFullageField != null)
                    {
                        einfo.CheckUllageStatus(engine.Key);
                    }
                }

                pureDragV += partPureDrag;
                pureLiftV += partPureLift;

                Vector3d partAeroForce = partPureDrag + partPureLift;

                var partDrag = Vector3d.Project(partAeroForce, -surfaceVelocity);
                Vector3d partLift = partAeroForce - partDrag;

                double partLiftScalar = partLift.magnitude;

                if (p.rb != null && partLiftScalar > 0.01)
                {
                    CoLScalar += partLiftScalar;
                    CoL       += ((Vector3d)p.rb.worldCenterOfMass + (Vector3d)(p.partTransform.rotation * p.CoLOffset)) * partLiftScalar;
                }
            }

            torqueAvailable += Vector3d.Max(torqueReactionWheel.Positive, torqueReactionWheel.Negative);

            //torqueAvailable += Vector3d.Max(torqueRcs.positive, torqueRcs.negative);

            torqueAvailable += Vector3d.Max(rcsTorqueAvailable.Positive, rcsTorqueAvailable.Negative);

            torqueAvailable += Vector3d.Max(torqueControlSurface.Positive, torqueControlSurface.Negative);

            torqueAvailable += Vector3d.Max(torqueGimbal.Positive, torqueGimbal.Negative);

            torqueAvailable += Vector3d.Max(torqueOthers.Positive, torqueOthers.Negative); // Mostly FAR

            torqueDiffThrottle   = Vector3d.Max(einfo.torqueDiffThrottle.Positive, einfo.torqueDiffThrottle.Negative);
            torqueDiffThrottle.y = 0;

            if (torqueAvailable.sqrMagnitude > 0)
            {
                torqueReactionSpeed = Vector3d.Max(torqueReactionSpeed6.Positive, torqueReactionSpeed6.Negative);
                torqueReactionSpeed.Scale(torqueAvailable.InvertNoNaN());
            }
            else
            {
                torqueReactionSpeed = Vector3d.zero;
            }

            thrustVectorMaxThrottle = einfo.thrustMax;
            thrustVectorMinThrottle = einfo.thrustMin;
            thrustVectorLastFrame   = einfo.thrustCurrent;

            if (CoTScalar > 0)
            {
                CoT           = CoT / CoTScalar;
                thrustForward = (CoM - CoT).normalized;
            }

            DoT = DoT.normalized;

            if (CoLScalar > 0)
                CoL = CoL / CoLScalar;

            Vector3d liftDir = -Vector3d.Cross(vessel.transform.right, -surfaceVelocity.normalized);

            if (isLoadedFAR && !vessel.packed && surfaceVelocity != Vector3d.zero)
            {
                Vector3 farTorque = Vector3.zero;
                CalculateVesselAeroForcesWithCache(vessel, out Vector3 farForce, out farTorque, surfaceVelocity, altitudeASL);

                Vector3d farDragVector = Vector3d.Dot(farForce, -surfaceVelocity.normalized) * -surfaceVelocity.normalized;
                drag      = farDragVector.magnitude / mass;
                dragUp    = Vector3d.Dot(farDragVector, up) / mass;
                pureDragV = farDragVector;
                pureDrag  = drag;

                Vector3d farLiftVector = Vector3d.Dot(farForce, liftDir) * liftDir;
                lift      = farLiftVector.magnitude / mass;
                liftUp    = Vector3d.Dot(farForce, up) / mass; // Use farForce instead of farLiftVector to match code for stock aero
                pureLiftV = farLiftVector;
                pureLift  = lift;
            }
            else
            {
                pureDragV = pureDragV / mass;
                pureLiftV = pureLiftV / mass;

                pureDrag = pureDragV.magnitude;

                pureLift = pureLiftV.magnitude;

                Vector3d force = pureDragV + pureLiftV;
                // Drag is the part (pureDrag + PureLift) applied opposite of the surface vel
                drag = Vector3d.Dot(force, -surfaceVelocity.normalized);
                // DragUp is the part (pureDrag + PureLift) applied in the "Up" direction
                dragUp = Vector3d.Dot(pureDragV, up);
                // Lift is the part (pureDrag + PureLift) applied in the "Lift" direction
                lift = Vector3d.Dot(force, liftDir);
                // LiftUp is the part (pureDrag + PureLift) applied in the "Up" direction
                liftUp = Vector3d.Dot(force, up);
            }

            maxEngineResponseTime = einfo.maxResponseTime;
        }

        [GeneralInfoItem("#MechJeb_Torque", InfoItem.Category.Vessel, showInEditor = true)] //Torque
        public void TorqueCompare()
        {
            var reactionTorque = Vector3d.Max(torqueReactionWheel.Positive, torqueReactionWheel.Negative);
            //var rcsTorque = Vector3d.Max(torqueRcs.positive, torqueRcs.negative);

            var rcsTorqueMJ = Vector3d.Max(rcsTorqueAvailable.Positive, rcsTorqueAvailable.Negative);

            var controlTorque = Vector3d.Max(torqueControlSurface.Positive, torqueControlSurface.Negative);
            var gimbalTorque = Vector3d.Max(torqueGimbal.Positive, torqueGimbal.Negative);
            var diffTorque = Vector3d.Max(einfo.torqueDiffThrottle.Positive, einfo.torqueDiffThrottle.Negative);
            diffTorque.y = 0;
            var othersTorque = Vector3d.Max(torqueOthers.Positive, torqueOthers.Negative);

            GUILayout.Label("Torque sources", GuiUtils.LabelNoWrap);
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            GUILayout.Label("ReactionWheel", GuiUtils.LabelNoWrap);

            GUILayout.Label("RCS", GuiUtils.LabelNoWrap);
            //GUILayout.Label("RCS MJ", GuiUtils.LabelNoWrap);

            GUILayout.Label("ControlSurface", GuiUtils.LabelNoWrap);
            GUILayout.Label("Gimbal", GuiUtils.LabelNoWrap);
            GUILayout.Label("Diff Throttle", GuiUtils.LabelNoWrap);
            GUILayout.Label("Others (FAR)", GuiUtils.LabelNoWrap);
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            GUILayout.Label(MuUtils.PrettyPrint(reactionTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            //GUILayout.Label(MuUtils.PrettyPrint(rcsTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));

            GUILayout.Label(MuUtils.PrettyPrint(rcsTorqueMJ), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));

            GUILayout.Label(MuUtils.PrettyPrint(controlTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.Label(MuUtils.PrettyPrint(gimbalTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.Label(MuUtils.PrettyPrint(diffTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.Label(MuUtils.PrettyPrint(othersTorque), GuiUtils.LabelNoWrap, GUILayout.ExpandWidth(false));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void UpdateResourceRequirements(EngineInfo einfo, IntakeInfo iinfo)
        {
            // Convert the resource information from the einfo and iinfo format
            // to the more useful ResourceInfo format.
            ResourceInfo.Release(resources.Values);
            resources.Clear();
            foreach (KeyValuePair<int, EngineInfo.FuelRequirement> info in einfo.resourceRequired)
            {
                int id = info.Key;
                EngineInfo.FuelRequirement req = info.Value;
                resources[id] = ResourceInfo.Borrow(
                    PartResourceLibrary.Instance.GetDefinition(id),
                    req.requiredLastFrame,
                    req.requiredAtMaxThrottle,
                    iinfo.getIntakes(id),
                    vesselRef);
            }

            int intakeAirId = PartResourceLibrary.Instance.GetDefinition("IntakeAir").id;
            intakeAir           = 0;
            intakeAirNeeded     = 0;
            intakeAirAtMax      = 0;
            intakeAirAllIntakes = 0;
            if (resources.ContainsKey(intakeAirId))
            {
                intakeAir           = resources[intakeAirId].intakeProvided;
                intakeAirAllIntakes = resources[intakeAirId].intakeAvailable;
                intakeAirNeeded     = resources[intakeAirId].required;
                intakeAirAtMax      = resources[intakeAirId].requiredAtMaxThrottle;
            }
        }

        // Decide whether to control the RCS thrusters from the main throttle
        private void ToggleRCSThrust(Vessel vessel)
        {
            if (thrustVectorMaxThrottle.magnitude == 0 && vessel.ActionGroups[KSPActionGroup.RCS])
            {
                rcsThrust               =  true;
                thrustVectorMaxThrottle += forward * rcsThrustAvailable.Down;
            }
            else
            {
                rcsThrust = false;
            }
        }

        private void UpdateMoIAndAngularMom(Vessel vessel)
        {
            MoI                      = vessel.MOI;
            angularMomentum          = vessel.angularMomentum;
            angularVelocityAvg.Value = angularVelocity;
        }

        [ValueInfoItem("#MechJeb_TerminalVelocity", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s")] //Terminal velocity
        public double TerminalVelocity() => TerminalVelocityCall();

        public readonly DTerminalVelocity TerminalVelocityCall;

        public double TerminalVelocityStockKSP()
        {
            if (mainBody == null || altitudeASL > mainBody.RealMaxAtmosphereAltitude()) return double.PositiveInfinity;

            return Math.Sqrt(2000 * mass * localg / (areaDrag * vesselRef.atmDensity));
        }

        public double TerminalVelocityFAR() => FARVesselTermVelEst(vesselRef);

        public double ThrustAccel(double throttle) => (1.0 - throttle) * minThrustAccel + throttle * maxThrustAccel;

        public double HeadingFromDirection(Vector3d dir) =>
            MuUtils.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(dir, east), Vector3d.Dot(dir, north)));

        // Altitude of bottom of craft, only calculated when requested because it is a bit expensive
        private bool   altitudeBottomIsCurrent;
        private double _altitudeBottom;

        [ValueInfoItem("#MechJeb_Altitude_bottom", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6,
            units = "m")] //Altitude (bottom)
        public double altitudeBottom
        {
            get
            {
                if (!altitudeBottomIsCurrent)
                {
                    _altitudeBottom         = ComputeVesselBottomAltitude(vesselRef);
                    altitudeBottomIsCurrent = true;
                }

                return _altitudeBottom;
            }
        }

        private double ComputeVesselBottomAltitude(Vessel vessel)
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
            public Vector3d thrustCurrent; // thrust at throttle achieved last frame
            public Vector3d thrustMax;     // thrust at full throttle
            public Vector3d thrustMin;     // thrust at zero throttle
            public double   maxResponseTime;

            public readonly Vector6 torqueDiffThrottle = new Vector6();

            // lowestUllage is always VeryStable without RealFuels installed
            public double lowestUllage;

            public struct FuelRequirement
            {
                public double requiredLastFrame;
                public double requiredAtMaxThrottle;
            }

            public readonly Dictionary<int, FuelRequirement> resourceRequired = new Dictionary<int, FuelRequirement>();

            private          Vector3d CoM;
            private          float    atmP0; // pressure now
            private          float    atmP1; // pressure after one timestep
            private readonly Queue    rotSave = new Queue();

            public void Update(Vector3d c, Vessel vessel)
            {
                thrustCurrent   = Vector3d.zero;
                thrustMax       = Vector3d.zero;
                thrustMin       = Vector3d.zero;
                maxResponseTime = 0;

                torqueDiffThrottle.Reset();

                resourceRequired.Clear();

                lowestUllage = 1.0;

                CoM = c;

                atmP0 = (float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres);
                float alt1 = (float)(vessel.altitude + TimeWarp.fixedDeltaTime * vessel.verticalSpeed);
                atmP1 = (float)(FlightGlobals.getStaticPressure(alt1) * PhysicsGlobals.KpaToAtmospheres);
            }

            public void CheckUllageStatus(ModuleEngines e)
            {
                // we report stable ullage for an unstable engine which is throttled up, so we let RF kill it
                // instead of having MJ throttle it down.
                if (e.getFlameoutState || !e.EngineIgnited || !e.isEnabled || e.requestedThrottle > 0.0F)
                {
                    return;
                }

                bool? ullage;
                try
                {
                    ullage = RFullageField.GetValue(e) as bool?;
                }
                catch (ArgumentException e1)
                {
                    Debug.Log("MechJeb BUG ArgumentError thrown while getting ullage from RealFuels, ullage integration disabled: " + e1.Message);
                    RFullageField = null;
                    return;
                }

                if (ullage == null)
                {
                    Debug.Log("MechJeb BUG: getting ullage from RealFuels casted to null, ullage status likely broken");
                    return;
                }

                if (ullage == false)
                {
                    return;
                }

                /* ullage is 'stable' if the engine has no ignitions left */
                int? ignitions;
                try
                {
                    ignitions = RFignitionsField.GetValue(e) as int?;
                }
                catch (ArgumentException e2)
                {
                    Debug.Log("MechJeb BUG ArgumentError thrown while getting ignitions from RealFuels, ullage integration disabled: " + e2.Message);
                    RFignitionsField = null;
                    return;
                }

                if (ignitions == null)
                {
                    Debug.Log("MechJeb BUG: getting ignitions from RealFuels casted to null, ullage status likely broken");
                    return;
                }

                /* -1 => infinite ignitions;  0 => no ignitions left;  1+ => ignitions remaining */
                if (ignitions == 0)
                {
                    return;
                }

                // finally we have an ignitable engine (that isn't already ignited), so check its propellant status

                // need to call RFullageSet to get the UllageSet then call GetUllageStability on that.
                // then need to get all the constants off of UllageSimulator
                double propellantStability;

                try
                {
                    object ullageSet = RFullageSetField.GetValue(e);
                    if (ullageSet == null)
                    {
                        Debug.Log("MechJeb BUG: getting propellantStatus from RealFuels casted to null, ullage status likely broken");
                        return;
                    }

                    try
                    {
                        propellantStability = (double)RFGetUllageStabilityMethod.Invoke(ullageSet, Array.Empty<object>());
                    }
                    catch (Exception e4)
                    {
                        Debug.Log("MechJeb BUG Exception thrown while calling GetUllageStability from RealFuels, ullage integration disabled: " +
                                  e4.Message);
                        RFullageSetField = null;
                        return;
                    }
                }
                catch (Exception e3)
                {
                    Debug.Log("MechJeb BUG Exception thrown while getting ullageSet from RealFuels, ullage integration disabled: " + e3.Message);
                    RFullageSetField = null;
                    return;
                }

                if (propellantStability < lowestUllage)
                {
                    lowestUllage = propellantStability;
                }
            }

            public void AddNewEngine(ModuleEngines e, ModuleGimbal gimbal, List<EngineWrapper> enginesWrappers, ref Vector3d CoT, ref Vector3d DoT,
                ref double CoTScalar)
            {
                // FIXME: shouldn't we gather statistics on unignited engines???
                if (!e.EngineIgnited || !e.isEnabled)
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
                    Propellant propellant = e.propellants[i];
                    double maxreq = e.maxFuelFlow * propellant.ratio;
                    addResource(propellant.id, propellant.currentRequirement, maxreq);
                }

                if (e.isOperational)
                {
                    float thrustLimiter = e.thrustPercentage / 100f;

                    double maxThrust = e.maxFuelFlow * e.flowMultiplier * Isp * e.g;
                    double minThrust = e.minFuelFlow * e.flowMultiplier * Isp * e.g;

                    // RealFuels engines reports as operational even when they are shutdown
                    // REMOVED: this definitively screws up the 1kN thruster in RO/RF and sets minThrust/maxThrust
                    // to zero when the engine is just throttled down -- which screws up suicide burn calcs, etc.
                    // if (e.finalThrust == 0f && minThrust > 0f)
                    //    minThrust = maxThrust = 0;

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
                        currentMinThrust =  currentMaxThrust;
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
                        Transform transform = e.thrustTransforms[i];
                        // The rotation makes a +z vector point in the direction that molecules are ejected
                        // from the engine.  The resulting thrust force is in the opposite direction.
                        Vector3d thrustDirectionVector = -transform.forward;

                        double cosineLosses = Vector3d.Dot(thrustDirectionVector, e.part.vessel.GetTransform().up);
                        float thrustTransformMultiplier = e.thrustTransformMultipliers[i];
                        double tCurrentThrust = eCurrentThrust * thrustTransformMultiplier;

                        thrustCurrent += tCurrentThrust * cosineLosses * thrustDirectionVector;
                        thrustMax     += eMaxThrust * cosineLosses * thrustDirectionVector * thrustTransformMultiplier;
                        thrustMin     += eMinThrust * cosineLosses * thrustDirectionVector * thrustTransformMultiplier;

                        CoT       += tCurrentThrust * (Vector3d)transform.position;
                        DoT       -= tCurrentThrust * thrustDirectionVector;
                        CoTScalar += tCurrentThrust;

                        Quaternion inverseVesselRot = e.part.vessel.ReferenceTransform.rotation.Inverse();
                        Vector3d thrust_dir = inverseVesselRot * thrustDirectionVector;
                        Vector3d pos = inverseVesselRot * (transform.position - CoM);

                        maxVariableForce  += (currentMaxThrust - currentMinThrust) * thrust_dir * thrustTransformMultiplier;
                        constantForce     += currentMinThrust * thrust_dir * thrustTransformMultiplier;
                        maxVariableTorque += (currentMaxThrust - currentMinThrust) * thrustTransformMultiplier * Vector3d.Cross(pos, thrust_dir);
                        constantTorque    += currentMinThrust * thrustTransformMultiplier * Vector3d.Cross(pos, thrust_dir);

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
                            gimbal.gimbalTransforms[i].localRotation = (Quaternion)rotSave.Dequeue();
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
                if (resourceRequired.TryGetValue(id, out FuelRequirement value))
                {
                    req = value;
                }
                else
                {
                    req                  = new FuelRequirement();
                    resourceRequired[id] = req;
                }

                req.requiredLastFrame     += current;
                req.requiredAtMaxThrottle += max;
            }
        }

        // Used during the vesselState constructor; distilled to other variables later.
        private class IntakeInfo
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
                if (allIntakes.TryGetValue(id, out List<ModuleResourceIntake> allIntake))
                {
                    thelist = allIntake;
                }
                else
                {
                    thelist        = ListPool<ModuleResourceIntake>.Instance.Borrow();
                    allIntakes[id] = thelist;
                }

                thelist.Add(intake);
            }

            private static readonly List<ModuleResourceIntake> empty = new List<ModuleResourceIntake>();

            public List<ModuleResourceIntake> getIntakes(int id)
            {
                if (allIntakes.TryGetValue(id, out List<ModuleResourceIntake> intakes))
                {
                    return intakes;
                }

                return empty;
            }
        }

        // Stored.
        public class ResourceInfo
        {
            public PartResourceDefinition definition;

            // We use kg/s rather than the more common T/s because these numbers tend to be small.
            // One debate I've had is whether to use mass/s or unit/s.  Dunno...

            public double required;              // kg/s
            public double requiredAtMaxThrottle; // kg/s
            public double intakeAvailable;       // kg/s

            public double intakeProvided
            {
                // kg/s for currently-open intakes
                get
                {
                    double sum = 0;
                    for (int i = 0; i < intakes.Count; i++)
                    {
                        IntakeData intakeData = intakes[i];
                        if (intakeData.intake.intakeEnabled)
                        {
                            sum += intakeData.predictedMassFlow;
                        }
                    }

                    return sum;
                }
            }

            public readonly List<IntakeData> intakes = new List<IntakeData>();

            public struct IntakeData
            {
                public IntakeData(ModuleResourceIntake intake, double predictedMassFlow)
                {
                    this.intake            = intake;
                    this.predictedMassFlow = predictedMassFlow;
                }

                public readonly ModuleResourceIntake intake;
                public readonly double               predictedMassFlow; // min kg/s this timestep or next
            }

            private static readonly Pool<ResourceInfo> pool = new Pool<ResourceInfo>(Create, Reset);

            public static int PoolSize => pool.Size;

            private static ResourceInfo Create() => new ResourceInfo();

            public virtual void Release() => pool.Release(this);

            public static void Release(Dictionary<int, ResourceInfo>.ValueCollection objList)
            {
                foreach (ResourceInfo resourceInfo in objList)
                {
                    resourceInfo.Release();
                }
            }

            private static void Reset(ResourceInfo obj)
            {
                obj.required              = 0;
                obj.requiredAtMaxThrottle = 0;
                obj.intakeAvailable       = 0;
                obj.intakes.Clear();
            }

            private ResourceInfo()
            {
            }

            public static ResourceInfo Borrow(PartResourceDefinition r, double req /* u per deltaT */, double atMax /* u per s */,
                List<ModuleResourceIntake> modules, Vessel vessel)
            {
                ResourceInfo resourceInfo = pool.Borrow();
                resourceInfo.Init(r, req /* u per deltaT */, atMax /* u per s */, modules, vessel);
                return resourceInfo;
            }

            private void Init(PartResourceDefinition r, double req /* u per deltaT */, double atMax /* u per s */, List<ModuleResourceIntake> modules,
                Vessel vessel)
            {
                definition = r;
                double density = definition.density * 1000; // kg per unit (density is in T per unit)
                float dT = TimeWarp.fixedDeltaTime;
                required              = req * density / dT;
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
                    ModuleResourceIntake intake = modules[index];
                    Transform intakeTransform = intake.intakeTransform;
                    if (intakeTransform == null)
                        continue;
                    Vector3d intakeFwd0 = intakeTransform.forward; // TODO : replace with the new public field
                    Vector3d intakeFwd1;
                    {
                        // Rotate the intake by the angular velocity for one timestep, in case the ship is spinning.
                        // Going through the Unity vector classes is about as many lines as hammering it out by hand.
                        Vector3 rot = dT * vessel.angularVelocity;
                        intakeFwd1 = Quaternion.AngleAxis(Mathf.Rad2Deg * rot.magnitude, rot) * intakeFwd0;
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
                    mass     = Math.Min(mass, capacity);

                    intakes.Add(new IntakeData(intake, mass));

                    idx++;
                }
            }

            // Return the number of kg of resource provided per second under certain conditions.
            // We use kg since the numbers are typically small.
            private double massProvided(double vesselSpeed, Vector3d normVesselSpeed, double atmDensity, double staticPressure, float mach,
                ModuleResourceIntake intake, Vector3d intakeFwd)
            {
                if ((intake.checkForOxygen && !FlightGlobals.currentMainBody.atmosphereContainsOxygen) ||
                    staticPressure < intake.kPaThreshold) // TODO : add the new test (the bool and maybe the attach node ?)
                {
                    return 0;
                }

                // This is adapted from code shared by Amram at:
                // http://forum.kerbalspaceprogram.com/showthread.php?34288-Maching-Bird-Challeng?p=440505
                // Seems to be accurate for 0.18.2 anyway.
                double intakeSpeed = intake.intakeSpeed; // airspeed when the intake isn't moving

                double aoa = Vector3d.Dot(normVesselSpeed, intakeFwd);
                if (aoa < 0) { aoa      = 0; }
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
                get => engine.thrustPercentage / 100;
                set => engine.thrustPercentage = value * 100;
            }

            public Vector3d constantForce { get; }

            public Vector3d maxVariableForce { get; }

            public Vector3d constantTorque { get; }

            public Vector3d maxVariableTorque { get; }

            public EngineWrapper(ModuleEngines module, Vector3d constantForce, Vector3d maxVariableForce, Vector3d constantTorque,
                Vector3d maxVariableTorque)
            {
                engine                 = module;
                this.constantForce     = constantForce;
                this.maxVariableForce  = maxVariableForce;
                this.constantTorque    = constantTorque;
                this.maxVariableTorque = maxVariableTorque;
            }
        }
    }
}
