using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using KSP.Localization;
using MechJebLibBindings;
using Smooth.Pools;
using UnityEngine;
using static MechJebLib.Utils.Statics;

#nullable enable

namespace MuMech
{
    public class VesselState
    {
        // Callbacks for external module
        public delegate void VesselStatePartExtension(Part p);

        public delegate void VesselStatePartModuleExtension(PartModule pm);

        private const double D_VELOCITY_SQR_THRESHOLD = 100;
        private const double D_VELOCITY_SQR_MIN_THRESHOLD = 1;
        private const double D_ALTITUDE_THRESHOLD = 300;

        private const float F_AO_A_THRESHOLD = 2;

        // RealFuels reflection
        public static readonly ReflectionUtils.ClassContext RFModuleEnginesRFType;
        public static readonly ReflectionUtils.FieldContext RFullageSetField;
        public static readonly ReflectionUtils.FieldContext RFignitionsField;
        public static readonly ReflectionUtils.FieldContext RFignitedField;
        public static readonly ReflectionUtils.FieldContext RFullageField;
        public static readonly ReflectionUtils.MethodContext RFGetUllageStabilityMethod;

        public static bool IsRealFuelsCorrectlyInitialized;

        public delegate double DoubleDelegate();

        public delegate double DoubleVesselDelegate(Vessel v);

        public delegate void CalculateVesselAeroForcesDelegate(Vessel vessel, out Vector3 aeroForce, out Vector3 aeroTorque, Vector3 velocityWorldVector, double altitude);

        public static ReflectionUtils.DelegateContext<DoubleVesselDelegate> FARVesselDragCoeff;
        public static ReflectionUtils.DelegateContext<DoubleVesselDelegate> FARVesselRefArea;
        public static ReflectionUtils.DelegateContext<DoubleVesselDelegate> FARVesselTermVelEst;
        public static ReflectionUtils.DelegateContext<DoubleVesselDelegate> FARVesselDynPres;
        public static ReflectionUtils.DelegateContext<CalculateVesselAeroForcesDelegate> FARCalculateVesselAeroForces;

        public static bool IsFARCorrectlyInitialized;

        // A convenient debug message to display in the UI
        public static string? Message;

        private readonly EngineInfo _einfo = new EngineInfo();

        private readonly Dictionary<ModuleEngines, ModuleGimbal?> _engines = new Dictionary<ModuleEngines, ModuleGimbal?>();
        private readonly IntakeInfo _iinfo = new IntakeInfo();
        public readonly List<EngineWrapper> EngineWrappers = new List<EngineWrapper>();

        // List of parachutes
        public readonly List<ModuleParachute> Parachutes = new List<ModuleParachute>();

        public readonly Vector6 RCSThrustAvailable = new Vector6(); // thrust available from RCS thrusters

        public readonly Vector6 RCSTorqueAvailable = new Vector6(); // torque available from RCS thrusters

        // Resource information keyed by resource Id.
        public readonly Dictionary<int, ResourceInfo> Resources = new Dictionary<int, ResourceInfo>();

        //public Vector6 torqueRcs = new Vector6();            // torque available from RCS from stock code (not working properly ATM)
        public readonly Vector6 TorqueControlSurface = new Vector6(); // torque available from Aerodynamic control surfaces
        public readonly Vector6 TorqueGimbal = new Vector6(); // torque available from Gimbaled engines
        public readonly Vector6 TorqueOthers = new Vector6(); // torque available from Mostly FAR

        // Torque from different components
        public readonly Vector6 TorqueReactionWheel = new Vector6(); // torque available from Reaction wheels

        public readonly List<VesselStatePartExtension> VesselStatePartExtensions = new List<VesselStatePartExtension>();
        public readonly List<VesselStatePartModuleExtension> VesselStatePartModuleExtensions = new List<VesselStatePartModuleExtension>();
        private double _altitudeBottom;

        // Altitude of bottom of craft, only calculated when requested because it is a bit expensive
        private bool _altitudeBottomIsCurrent;
        private double _lastAltitudeAsl;
        private float _lastAoA;

        //FARCalculateVesselAeroForces(vessel,out farForce,out farTorque,surfaceVelocity,altitudeASL);
        private Vector3 _lastFarForce;
        private Vector3d _lastSurfaceVelocity;

        private Vessel _vessel => _core.vessel;

        [ValueInfoItem("#MechJeb_Altitude_ASL", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, units = "m")] //Altitude (ASL)
        public double AltitudeASL;

        [ValueInfoItem("#MechJeb_Altitude_true", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, units = "m")] //Altitude (true)
        public double AltitudeTrue;

        [ValueInfoItem("#MechJeb_AngleToPrograde", InfoItem.Category.Orbit, format = "F2", units = "º")] //Angle to prograde
        public double AngleToPrograde;

        public Vector3d AngularMomentum;

        public Vector3d AngularVelocity;

        [ValueInfoItem("#MechJeb_AngleOfAttack", InfoItem.Category.Misc, format = "F2", units = "º")] //Angle of Attack
        public double AoA;

        [ValueInfoItem("#MechJeb_DisplacementAngle", InfoItem.Category.Misc, format = "F2", units = "º")] //Displacement Angle
        public double AoD;

        [ValueInfoItem("#MechJeb_AngleOfSideslip", InfoItem.Category.Misc, format = "F2", units = "º")] //Angle of Sideslip
        public double AoS;

        // Product of the drag surface area, drag coefficient and the physics multipliers
        [ValueInfoItem("Area Drag", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m²")] //Area Drag
        public double AreaDrag;

        public double AtmosphericDensity;

        [ValueInfoItem("#MechJeb_AtmosphereDensity", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "g/m³")] //Atmosphere density
        public double AtmosphericDensityInGrams;

        [ValueInfoItem("Celestial Longitude", InfoItem.Category.Orbit, format = "F3")]
        public double CelestialLongitude;

        public Vector3d CoL;
        public double CoLMagnitude;

        public Vector3d CoM;

        public Vector3d CoT;
        public double CoTMagnitude;

        public double DeltaT; //TimeWarp.fixedDeltaTime
        public Vector3d DoT;

        [ValueInfoItem("Drag Acceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")] //Drag Acceleration
        public double DragAcceleration; // wind relative drag acceleration

        [ValueInfoItem("Drag Force", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kN")] //Drag Force
        public double DragForce; // wind relative drag force

        [ValueInfoItem("#MechJeb_DragCoefficient", InfoItem.Category.Vessel, format = "F2")] //Drag Coefficient
        public double DragCoefficient;

        [ValueInfoItem("#MechJeb_DynamicPressure", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "Pa")] //Dynamic pressure
        public double DynamicPressure;

        public Vector3d East;
        public Vector3d Forward; //the direction the vessel is pointing

        [ValueInfoItem("#MechJeb_AerothermalFlux", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "W/m²")] //Aerothermal flux
        public double FreeMolecularAerothermalFlux;

        public Vector3d GravityForce;

        [ValueInfoItem("#MechJeb_Heading", InfoItem.Category.Surface, format = "F1", units = "º")] //Heading
        public double Heading;

        public Vector3d HorizontalOrbit; //unit vector in the direction of horizontal component of orbit velocity
        public Vector3d HorizontalSurface; //unit vector in the direction of horizontal component of surface velocity

        [ValueInfoItem("#MechJeb_IntakeAir", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")] //Intake air
        public double IntakeAir;

        [ValueInfoItem("#MechJeb_IntakeAirAllIntakes", InfoItem.Category.Vessel, format = ValueInfoItem.SI,
            units = "kg/s")]
        //Intake air (all intakes open)
        public double IntakeAirAllIntakes;

        [ValueInfoItem("#MechJeb_intakeAirAtMax", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")] //Intake air needed (max)
        public double IntakeAirAtMax;

        [ValueInfoItem("#MechJeb_IntakeAirNeeded", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")] //Intake air needed
        public double IntakeAirNeeded;

        [ValueInfoItem("#MechJeb_Latitude", InfoItem.Category.Surface, format = ValueInfoItem.ANGLE_NS)] //Latitude
        public double Latitude;

        [ValueInfoItem("Lift Acceleration", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")] //Lift Acceleration
        public double LiftAcceleration; // wind relative Lift acceleration

        [ValueInfoItem("Lift Force", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kN")] //Lift Force
        public double LiftForce; // wind relative Lift force

        [ValueInfoItem("#MechJeb_LocalGravity", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "m/s²")] //Local gravity
        public double LocalGravity; //magnitude of gravityForce

        [ValueInfoItem("#MechJeb_Longitude", InfoItem.Category.Surface, format = ValueInfoItem.ANGLE_EW)] //Longitude
        public double Longitude;

        [ValueInfoItem("#MechJeb_Mach", InfoItem.Category.Vessel, format = "F2")] //Mach
        public double Mach;

        public CelestialBody? MainBody;

        public double Mass;

        [ValueInfoItem("#MechJeb_MaxDynamicPressure", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "Pa")] //Max dynamic pressure
        public double MaxDynamicPressure;

        public double MaxEngineResponseTime;
        public Vector3d MoI; //Diagonal components of the inertia tensor (almost always the dominant components)
        public Vector3d NormalPlus; //unit vector perpendicular to up and velocityVesselOrbit
        public Vector3d NormalPlusSurface; //unit vector perpendicular to up and velocityVesselSurface
        public Vector3d North;
        public Vector3d OrbitalPosition;

        public Vector3d OrbitalVelocity;

        [ValueInfoItem("#MechJeb_Apoapsis", InfoItem.Category.Orbit, units = "m", format = ValueInfoItem.SI, siSigFigs = 6, category = InfoItem.Category.Orbit)] //Apoapsis
        public double OrbitApA;

        [ValueInfoItem("#MechJeb_ArgumentOfPeriapsis", InfoItem.Category.Orbit, format = "F1", units = "º")] //Argument of periapsis
        public double OrbitArgumentOfPeriapsis;

        [ValueInfoItem("#MechJeb_Eccentricity", InfoItem.Category.Orbit, format = "F3")] //Eccentricity
        public double OrbitEccentricity;

        [ValueInfoItem("#MechJeb_Inclination", InfoItem.Category.Orbit, format = "F3", units = "º")] //Inclination
        public double OrbitInclination;

        [ValueInfoItem("#MechJeb_LAN", InfoItem.Category.Orbit, format = ValueInfoItem.ANGLE)] //LAN
        public double OrbitLAN;

        [ValueInfoItem("#MechJeb_Periapsis", InfoItem.Category.Orbit, units = "m", format = ValueInfoItem.SI, siSigFigs = 6, category = InfoItem.Category.Orbit)] //Periapsis
        public double OrbitPeA;

        [ValueInfoItem("#MechJeb_OrbitalPeriod", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 2, category = InfoItem.Category.Orbit)] //Orbital period
        public double OrbitPeriod;

        [ValueInfoItem("#MechJeb_SemiMajorAxis", InfoItem.Category.Orbit, format = ValueInfoItem.SI, siSigFigs = 6, units = "m")] //Semi-major axis
        public double OrbitSemiMajorAxis;

        [ValueInfoItem("#MechJeb_TimeToApoapsis", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 1)] //Time to apoapsis
        public double OrbitTimeToAp;

        [ValueInfoItem("#MechJeb_TimeToPeriapsis", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, timeDecimalPlaces = 1)] //Time to periapsis
        public double OrbitTimeToPe;

        public bool ParachuteDeployed;

        [ValueInfoItem("#MechJeb_Pitch", InfoItem.Category.Surface, format = "F1", units = "º")] //Pitch
        public double Pitch;

        [ValueInfoItem("#MechJeb_PureDrag", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")] //Pure Drag
        public double PureDrag;

        public Vector3d PureDragVector;

        [ValueInfoItem("#MechJeb_PureLift", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s²")] //Pure Lift
        public double PureLift;

        public Vector3d PureLiftVector;

        public Vector3d RadialPlus; //unit vector in the plane of up and velocityVesselOrbit and perpendicular to velocityVesselOrbit
        public Vector3d RadialPlusSurface; //unit vector in the plane of up and velocityVesselSurface and perpendicular to velocityVesselSurface

        public double Radius; //distance from planet center

        public bool RCSThrust;

        [ValueInfoItem("#MechJeb_Roll", InfoItem.Category.Surface, format = "F1", units = "º")] //Roll
        public double Roll;

        public Vector3d RootPartPosition;

        public Quaternion RotationSurface;
        public Quaternion RotationVesselSurface;

        [ValueInfoItem("#MechJeb_SpeedOfSound", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s")] //Speed of sound
        public double SpeedOfSound;

        //How about changing these so we store the instantaneous values and *also*
        //the smoothed MovingAverages? Sometimes we need the instantaneous value.
        [ValueInfoItem("#MechJeb_OrbitalSpeed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")] //Orbital speed
        public double SpeedOrbital;

        [ValueInfoItem("#MechJeb_OrbitHorizontalSpeed", InfoItem.Category.Orbit, format = ValueInfoItem.SI, units = "m/s")] //Orbit horizontal speed
        public double SpeedOrbitalHorizontal;

        [ValueInfoItem("#MechJeb_SurfaceSpeed", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s")] //Surface speed
        public double SpeedSurface;

        [ValueInfoItem("#MechJeb_SurfaceHorizontalSpeed", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s")] //Surface horizontal speed
        public double SpeedSurfaceHorizontal;

        [ValueInfoItem("#MechJeb_VerticalSpeed", InfoItem.Category.Surface, format = ValueInfoItem.SI, units = "m/s")] //Vertical speed
        public double SpeedVertical;

        [ValueInfoItem("#MechJeb_SurfaceAltitudeASL", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 4, units = "m")] //Surface altitude ASL
        public double SurfaceAltitudeASL;

        public Vector3d SurfaceVelocity;

        /* the fixed throttle limit (i.e. user limited in the GUI), does not include transient conditions as limiting to zero due to unstable propellants in RF */
        public float ThrottleFixedLimit = 1;

        /* the current throttle limit, this may include transient condition such as limiting to zero due to unstable propellants in RF */
        public float ThrottleLimit = 1;

        // Forward direction of thrust (CoT-CoM).normalized
        // FIXME: this is Vector3d.zero if throttle is zero!
        public Vector3d ThrustForward;

        // Thrust is a vector.  These are in the same frame of reference as forward and other vectors.
        public Vector3d ThrustVectorLastFrame;
        public Vector3d ThrustVectorMaxThrottle;
        public Vector3d ThrustVectorMinThrottle;

        [ValueInfoItem("#MechJeb_UniversalTime", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)] //Universal Time
        public double Time; //planetarium time

        // Total torque
        public Vector3d TorqueAvailable;

        // Variable part of torque related to differential throttle
        public Vector3d TorqueDifferentialThrottle;

        public Vector3d TorqueReactionSpeed; // FIXME: probably buggy + needs to be removed (but used in MJAttitudeController)

        // model as a first order IIR low pass filter with alpha = torqueResponseSpeed * timestep
        // 50 is no filter at 0.02 sec
        // typical values are 16-50 with 8 or 4 possible.
        public Vector3d TorqueResponseSpeed;

        public Vector3d TorqueWeightedExponentialResponseDelay; // Exposed for debugging.
        public Vector3d TorqueWeightedLinearResponseDelay; // Exposed for debugging.

        public Vector3d Up;

        public Vector3d VelocityMainBodySurface;
        private readonly MechJebCore _core;


        public VesselState(MechJebCore core)
        {
            _core = core;
        }

        static VesselState()
        {
            /*
             * RealFuels Reflection
             */

            RFModuleEnginesRFType = ReflectionUtils.Assembly("RealFuels").Class("RealFuels.ModuleEnginesRF");
            RFullageSetField = ReflectionUtils.Assembly("RealFuels").Class("RealFuels.ModuleEnginesRF").Field("ullageSet");
            RFignitionsField = ReflectionUtils.Assembly("RealFuels").Class("RealFuels.ModuleEnginesRF").Field("ignitions");
            RFignitedField = ReflectionUtils.Assembly("RealFuels").Class("RealFuels.ModuleEnginesRF").Field("ignited", BindingFlags.NonPublic | BindingFlags.Instance);
            RFullageField = ReflectionUtils.Assembly("RealFuels").Class("RealFuels.ModuleEnginesRF").Field("ullage");
            RFGetUllageStabilityMethod = ReflectionUtils.Assembly("RealFuels").Class("RealFuels.Ullage.UllageSet").Method("GetUllageStability", BindingFlags.Public | BindingFlags.Instance);

            ValidateRealFuels();

            /*
             * FAR Reflection
             */

            FARVesselDragCoeff = ReflectionUtils.Assembly("FerramAerospaceResearch").Class("FerramAerospaceResearch.FARAPI")
               .Method("VesselDragCoeff", BindingFlags.Public | BindingFlags.Static, new[] { typeof(Vessel) })
               .StaticDelegate<DoubleVesselDelegate>();
            FARVesselRefArea = ReflectionUtils.Assembly("FerramAerospaceResearch").Class("FerramAerospaceResearch.FARAPI")
               .Method("VesselRefArea", BindingFlags.Public | BindingFlags.Static, new[] { typeof(Vessel) })
               .StaticDelegate<DoubleVesselDelegate>();
            FARVesselTermVelEst = ReflectionUtils.Assembly("FerramAerospaceResearch").Class("FerramAerospaceResearch.FARAPI")
               .Method("VesselTermVelEst", BindingFlags.Public | BindingFlags.Static, new[] { typeof(Vessel) })
               .StaticDelegate<DoubleVesselDelegate>();
            FARVesselDynPres = ReflectionUtils.Assembly("FerramAerospaceResearch").Class("FerramAerospaceResearch.FARAPI")
               .Method("VesselDynPres", BindingFlags.Public | BindingFlags.Static, new[] { typeof(Vessel) })
               .StaticDelegate<DoubleVesselDelegate>();
            FARCalculateVesselAeroForces = ReflectionUtils.Assembly("FerramAerospaceResearch").Class("FerramAerospaceResearch.FARAPI")
               .Method("CalculateVesselAeroForces", BindingFlags.Public | BindingFlags.Static, new[] { typeof(Vessel), typeof(Vector3).MakeByRefType(), typeof(Vector3).MakeByRefType(), typeof(Vector3), typeof(double) })
               .StaticDelegate<CalculateVesselAeroForcesDelegate>();

            ValidateFAR();
        }

        private static void ValidateRealFuels()
        {
            IsRealFuelsCorrectlyInitialized = ReflectionUtils.IsLoadedRealFuels && RFModuleEnginesRFType.IsValid && RFullageSetField.IsValid && RFGetUllageStabilityMethod.IsValid
                && RFignitionsField.IsValid && RFignitedField.IsValid && RFullageField.IsValid;

            if (!ReflectionUtils.IsLoadedRealFuels)
            {
                Debug.Log("MechJeb: RealFuels Assembly is not available, prior messages are ignorable.");
                return;
            }

            if (IsRealFuelsCorrectlyInitialized)
                Debug.Log("MechJeb: RealFuels Assembly is wired up properly.");
            else
                Debug.Log("MechJeb ERROR: RealFuels integration in VesselState failed to initialize correctly, THIS IS A BUG.");
        }

        private static void ValidateFAR()
        {
            IsFARCorrectlyInitialized = ReflectionUtils.IsLoadedFAR && FARVesselDragCoeff.IsValid && FARVesselRefArea.IsValid && FARVesselTermVelEst.IsValid
                && FARVesselDynPres.IsValid && FARCalculateVesselAeroForces.IsValid;

            if (!ReflectionUtils.IsLoadedFAR)
            {
                Debug.Log("MechJeb: FAR Assembly is not available, prior messages are ignorable.");
                return;
            }

            if (IsFARCorrectlyInitialized)
                Debug.Log("MechJeb: FAR Assembly is wired up properly.");
            else
                Debug.Log("MechJeb ERROR: FAR integration in VesselState failed to initialize correctly, THIS IS A BUG.");
        }

        // lowestUllage is always VeryStable without RealFuels installed
        public double LowestUllage => _einfo.LowestUllage;

        // Thrust in the forward direction (for historical reasons).
        public double ThrustAvailable => Vector3d.Dot(ThrustVectorMaxThrottle, Forward);
        public double ThrustMinimum   => Vector3d.Dot(ThrustVectorMinThrottle, Forward);
        public double ThrustCurrent   => Vector3d.Dot(ThrustVectorLastFrame, Forward);

        // Acceleration in the forward direction, for when dividing by mass is too complicated.
        public double MaxThrustAcceleration        => ThrustAvailable / Mass;
        public double MinThrustAcceleration        => ThrustMinimum / Mass;
        public double CurrentThrustAcceleration    => ThrustCurrent / Mass;
        public double LimitedMaxThrustAcceleration => MaxThrustAcceleration * ThrottleFixedLimit + MinThrustAcceleration * (1 - ThrottleFixedLimit);

        [ValueInfoItem("#MechJeb_Altitude_bottom", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6,
            units = "m")] //Altitude (bottom)
        public double AltitudeBottom
        {
            get
            {
                if (_altitudeBottomIsCurrent) return _altitudeBottom;

                _altitudeBottom = ComputeVesselBottomAltitude();
                _altitudeBottomIsCurrent = true;

                return _altitudeBottom;
            }
        }

        [GeneralInfoItem("#MechJeb_DebugString", InfoItem.Category.Misc, showInEditor = true)] //Debug String
        public void DebugString()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Message);
            GUILayout.EndVertical();
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
        public bool Update()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (Time == Planetarium.GetUniversalTime())
                return true;

            if (_vessel.rootPart.rb == null)
                return false; //if we try to update before rigidbodies exist we spam the console with NullPointerExceptions.

            UpdateVelocityAndCoM();

            UpdateBasicInfo();

            UpdateRCSThrustAndTorque();

            EngineWrappers.Clear();

            _einfo.Update(CoM, _vessel);
            _iinfo.Update();
            AnalyzeParts(_einfo, _iinfo);

            UpdateResourceRequirements(_einfo, _iinfo);

            ToggleRCSThrust();

            UpdateMoIAndAngularMom();

            return true;
        }

        // TODO memo for later. egg found out that vessel.pos is actually 1 frame in the future while vessel.obt_vel is not.
        // This should have changed in 1.1
        // This most likely has some impact on the code.

        private void UpdateVelocityAndCoM()
        {
            Mass = _vessel.totalMass;
            CoM = _vessel.CoMD;
            OrbitalVelocity = _vessel.obt_velocity;
            OrbitalPosition = CoM - _vessel.mainBody.position;
        }

        // Calculate a bunch of simple quantities each frame.
        private void UpdateBasicInfo()
        {
            Time = Planetarium.GetUniversalTime();
            DeltaT = TimeWarp.fixedDeltaTime;

            //CoM = °;
            Up = OrbitalPosition.normalized;

            Rigidbody rigidBody = _vessel.rootPart.rb;
            if (rigidBody != null) RootPartPosition = rigidBody.position;

            North = _vessel.north;
            East = _vessel.east;
            Forward = _vessel.GetTransform().up;
            RotationSurface = Quaternion.LookRotation(North, Up);
            RotationVesselSurface =
                Quaternion.Inverse(Quaternion.Euler(90, 0, 0) * Quaternion.Inverse(_vessel.GetTransform().rotation) * RotationSurface);

            SurfaceVelocity = OrbitalVelocity - _vessel.mainBody.getRFrmVel(CoM);

            VelocityMainBodySurface = RotationSurface * SurfaceVelocity;

            HorizontalOrbit = Vector3d.Exclude(Up, OrbitalVelocity).normalized;
            HorizontalSurface = Vector3d.Exclude(Up, SurfaceVelocity).normalized;

            AngularVelocity = _vessel.angularVelocity;

            RadialPlusSurface = Vector3d.Exclude(SurfaceVelocity, Up).normalized;
            RadialPlus = Vector3d.Exclude(OrbitalVelocity, Up).normalized;
            NormalPlusSurface = -Vector3d.Cross(RadialPlusSurface, SurfaceVelocity.normalized);
            NormalPlus = -Vector3d.Cross(RadialPlus, OrbitalVelocity.normalized);

            Mach = _vessel.mach;

            GravityForce = FlightGlobals.getGeeForceAtPosition(CoM); // TODO vessel.gravityForPos or vessel.gravityTrue
            LocalGravity = GravityForce.magnitude;

            SpeedOrbital = OrbitalVelocity.magnitude;
            SpeedSurface = SurfaceVelocity.magnitude;
            SpeedVertical = Vector3d.Dot(SurfaceVelocity, Up);
            SpeedSurfaceHorizontal =
                Vector3d.Exclude(Up, SurfaceVelocity).magnitude; //(velocityVesselSurface - (speedVertical * up)).magnitude;
            SpeedOrbitalHorizontal = (OrbitalVelocity - SpeedVertical * Up).magnitude;

            // Angle of Attack, angle between surface velocity and the ship-nose vector (KSP "up" vector) in the plane that has no ship-right/left in it
            var srfProj = Vector3.ProjectOnPlane(SurfaceVelocity.normalized, _vessel.ReferenceTransform.right);
            double tmpAoA = UtilMath.Rad2Deg * Math.Atan2(Vector3.Dot(srfProj.normalized, _vessel.ReferenceTransform.forward),
                Vector3.Dot(srfProj.normalized, _vessel.ReferenceTransform.up));
            AoA = double.IsNaN(tmpAoA) || SpeedSurface < 0.01 ? 0 : tmpAoA;

            // Angle of Sideslip, angle between surface velocity and the ship-nose vector (KSP "up" vector) in the plane that has no ship-top/bottom in it (KSP "forward"/"back")
            srfProj = Vector3.ProjectOnPlane(SurfaceVelocity.normalized, _vessel.ReferenceTransform.forward);
            double tmpAoS = UtilMath.Rad2Deg * Math.Atan2(Vector3.Dot(srfProj.normalized, _vessel.ReferenceTransform.right),
                Vector3.Dot(srfProj.normalized, _vessel.ReferenceTransform.up));
            AoS = double.IsNaN(tmpAoS) || SpeedSurface < 0.01 ? 0 : tmpAoS;

            // Displacement Angle, angle between surface velocity and the ship-nose vector (KSP "up" vector) -- ignores roll of the craft (0 to 180 degrees)
            double tempAoD = UtilMath.Rad2Deg *
                Math.Acos(Clamp(Vector3.Dot(_vessel.ReferenceTransform.up, SurfaceVelocity.normalized), -1, 1));
            AoD = double.IsNaN(tempAoD) || SpeedSurface < 0.01 ? 0 : tempAoD;

            Heading = RotationVesselSurface.eulerAngles.y;
            Pitch = RotationVesselSurface.eulerAngles.x > 180
                ? 360.0 - RotationVesselSurface.eulerAngles.x
                : -RotationVesselSurface.eulerAngles.x;
            Roll = RotationVesselSurface.eulerAngles.z > 180
                ? RotationVesselSurface.eulerAngles.z - 360.0
                : RotationVesselSurface.eulerAngles.z;

            AltitudeASL = _vessel.mainBody.GetAltitude(CoM);

            SurfaceAltitudeASL = _vessel.mainBody.pqsController != null ? _vessel.pqsAltitude : 0d;
            if (_vessel.mainBody.ocean && SurfaceAltitudeASL < 0) SurfaceAltitudeASL = 0;
            AltitudeTrue = AltitudeASL - SurfaceAltitudeASL;

            // altitudeBottom will be recomputed if someone requests it.
            _altitudeBottomIsCurrent = false;

            double atmosphericPressure = FlightGlobals.getStaticPressure(AltitudeASL, _vessel.mainBody);
            //if (atmosphericPressure < vessel.mainBody.atmosphereMultiplier * 1e-6) atmosphericPressure = 0;
            double temperature = FlightGlobals.getExternalTemperature(AltitudeASL);
            AtmosphericDensity = FlightGlobals.getAtmDensity(atmosphericPressure, temperature);
            AtmosphericDensityInGrams = AtmosphericDensity * 1000;

            DynamicPressure = GetDynamicPressure();

            if (DynamicPressure > MaxDynamicPressure)
                MaxDynamicPressure = DynamicPressure;
            FreeMolecularAerothermalFlux = 0.5 * AtmosphericDensity * SpeedSurface * SpeedSurface * SpeedSurface;


            SpeedOfSound = _vessel.speedOfSound;

            OrbitApA = _vessel.orbit.ApA;
            OrbitPeA = _vessel.orbit.PeA;
            OrbitPeriod = _vessel.orbit.period;
            OrbitTimeToAp = _vessel.orbit.timeToAp;
            OrbitTimeToPe = _vessel.orbit.timeToPe;

            OrbitLAN = _vessel.orbit.LAN;

            OrbitArgumentOfPeriapsis = _vessel.orbit.argumentOfPeriapsis;
            OrbitInclination = _vessel.orbit.inclination;
            OrbitEccentricity = _vessel.orbit.eccentricity;
            OrbitSemiMajorAxis = _vessel.orbit.semiMajorAxis;
            CelestialLongitude = Planetarium.right.AngleInPlane(-Planetarium.up, OrbitalPosition);
            Latitude = _vessel.mainBody.GetLatitude(CoM);
            Longitude = MuUtils.ClampDegrees180(_vessel.mainBody.GetLongitude(CoM));

            if (_vessel.mainBody != Planetarium.fetch.Sun)
            {
                Vector3d prograde = _vessel.mainBody.orbit.getOrbitalVelocityAtUT(Time).xzy;
                Vector3d normal = _vessel.mainBody.orbit.GetOrbitNormal().xzy;
                AngleToPrograde = MuUtils.ClampDegrees360((_vessel.orbit.inclination > 90 || _vessel.orbit.inclination < -90 ? 1 : -1) *
                    OrbitalPosition.AngleInPlane(normal, prograde));
            }
            else
            {
                AngleToPrograde = 0;
            }

            MainBody = _vessel.mainBody;

            Radius = OrbitalPosition.magnitude;
        }

        private double GetDynamicPressure()
        {
            if (ReflectionUtils.IsLoadedFAR)
            {
                return FARVesselDynPres.Call(_vessel) * 1000;
            }

            return _vessel.dynamicPressurekPa * 1000;
        }

        private void UpdateRCSThrustAndTorque()
        {
            RCSThrustAvailable.Reset();
            RCSTorqueAvailable.Reset();

            //torqueRcs.Reset();

            if (!_vessel.ActionGroups[KSPActionGroup.RCS])
                return;

            MechJebModuleRCSBalancer rcsbal = _vessel.GetMasterMechJeb().Rcsbal;
            if (rcsbal.Enabled)
            {
                Vector3d rot = Vector3d.zero;
                foreach (Vector6.Direction dir6 in Vector6.Values)
                {
                    Vector3d dir = Vector6.Directions[(int)dir6];
                    rcsbal.GetThrottles(dir, out double[] throttles, out List<RCSSolver.Thruster> thrusters);
                    if (throttles == null) continue;

                    for (int j = 0; j < throttles.Length; j++)
                    {
                        if (throttles[j] <= 0) continue;

                        Vector3d force = thrusters[j].GetThrust(dir, rot);
                        RCSThrustAvailable.Add(
                            _vessel.GetTransform().InverseTransformDirection(dir * Vector3d.Dot(force * throttles[j], dir)));
                        // Are we missing an rcsTorqueAvailable calculation here?
                    }
                }
            }

            Vector3d movingCoM = _vessel.CurrentCoM;

            foreach (Part p in _vessel.parts)
            {
                foreach (PartModule m in p.Modules)
                {
                    var rcs = m as ModuleRCS;

                    if (rcs == null)
                        continue;

                    //Vector3 pos;
                    //Vector3 neg;
                    //rcs.GetPotentialTorque(out pos, out neg);

                    //torqueRcs.Add(pos);
                    //torqueRcs.Add(neg);

                    //if (rcsbal.enabled)
                    //    continue;

                    if (p.ShieldedFromAirstream || !rcs.rcsEnabled || !rcs.isEnabled || rcs.isJustForShow || rcs.flameout || !rcs.rcs_active)
                        continue;

                    var attitudeControl = new Vector3(rcs.enablePitch ? 1 : 0, rcs.enableRoll ? 1 : 0, rcs.enableYaw ? 1 : 0);

                    var translationControl = new Vector3(rcs.enableX ? 1 : 0f, rcs.enableZ ? 1 : 0, rcs.enableY ? 1 : 0);
                    foreach (Transform t in rcs.thrusterTransforms)
                    {
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
                                    power /= lever;
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
                            RCSThrustAvailable.Add(Vector3.Scale(_vessel.GetTransform().InverseTransformDirection(thrusterThrust),
                                translationControl));
                        }

                        Vector3d thrusterTorque = Vector3.Cross(thrusterPosition, thrusterThrust);

                        // Convert in vessel local coordinate
                        RCSTorqueAvailable.Add(Vector3.Scale(_vessel.GetTransform().InverseTransformDirection(thrusterTorque), attitudeControl));
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
            GUILayout.Label("Pos", GuiUtils.LayoutExpandWidth); //
            GUILayout.Label(MuUtils.PrettyPrint(RCSThrustAvailable.Positive), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Neg", GuiUtils.LayoutExpandWidth); //
            GUILayout.Label(MuUtils.PrettyPrint(RCSThrustAvailable.Negative), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_RCSTorque", InfoItem.Category.Vessel, showInEditor = true)] //RCS Torque
        public void RCSTorque()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_RCSTorque")); //"RCS Torque"
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pos", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(RCSTorqueAvailable.Positive), GuiUtils.LayoutNoExpandWidth);
            //GUILayout.Label(MuUtils.PrettyPrint(torqueRcs.positive), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Neg", GuiUtils.LayoutExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(RCSTorqueAvailable.Negative), GuiUtils.LayoutNoExpandWidth);
            //GUILayout.Label(MuUtils.PrettyPrint(torqueRcs.negative), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void CalculateVesselAeroForcesWithCache(Vessel v, out Vector3 farForce, Vector3d surfaceVelocity,
            double altitudeASL)
        {
            float aoa = Vector3.Angle(v.rootPart.transform.TransformDirection(Vector3.up), surfaceVelocity);
            if ((_lastSurfaceVelocity - surfaceVelocity).sqrMagnitude > D_VELOCITY_SQR_THRESHOLD
                || Math.Abs(altitudeASL - _lastAltitudeAsl) > D_ALTITUDE_THRESHOLD
                || (surfaceVelocity.sqrMagnitude > D_VELOCITY_SQR_MIN_THRESHOLD && aoa - _lastAoA > F_AO_A_THRESHOLD))
            {
                FARCalculateVesselAeroForces.Call(v, out farForce, out _, surfaceVelocity, altitudeASL);
                _lastSurfaceVelocity = surfaceVelocity;
                _lastAltitudeAsl = altitudeASL;
                _lastAoA = aoa;
                _lastFarForce = farForce;
            }
            else
            {
                farForce = _lastFarForce;
            }
        }

        // Loop over all the parts in the vessel and calculate some things.
        private void AnalyzeParts(EngineInfo einfo, IntakeInfo iinfo)
        {
            Parachutes.Clear();
            ParachuteDeployed = false;

            TorqueAvailable = Vector3d.zero;

            var torqueWeightedExponentialResponseDelay6 = new Vector6();
            var torqueWeightedLinearResponseDelay6 = new Vector6();
            var torqueReactionSpeed6 = new Vector6(); // FIXME: probably buggy + remove.

            TorqueReactionWheel.Reset();
            TorqueControlSurface.Reset();
            TorqueGimbal.Reset();
            TorqueOthers.Reset();

            PureDragVector = Vector3d.zero;
            PureLiftVector = Vector3d.zero;

            if (ReflectionUtils.IsLoadedFAR)
            {
                DragCoefficient = FARVesselDragCoeff.Call(_vessel);
                AreaDrag = FARVesselRefArea.Call(_vessel) * DragCoefficient * PhysicsGlobals.DragMultiplier;
            }
            else
            {
                DragCoefficient = 0;
                AreaDrag = 0;
            }

            CoL = Vector3d.zero;
            CoLMagnitude = 0;

            CoT = Vector3d.zero;
            DoT = Vector3d.zero;
            CoTMagnitude = 0;
            ThrustForward = Vector3d.zero;

            foreach (Part p in _vessel.parts)
            {
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

                if (!ReflectionUtils.IsLoadedFAR)
                {
                    DragCoefficient += p.DragCubes.DragCoeff;
                    AreaDrag += p.DragCubes.AreaDrag * PhysicsGlobals.DragCubeMultiplier * PhysicsGlobals.DragMultiplier;
                }

                foreach (VesselStatePartExtension vspe in VesselStatePartExtensions)
                {
                    vspe(p);
                }

                _engines.Clear();

                foreach (PartModule pm in p.Modules)
                {
                    if (!pm.isEnabled)
                        continue;

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
                        TorqueReactionWheel.Add(pos);
                        TorqueReactionWheel.Add(-neg);
                    }
                    else
                        switch (pm)
                        {
                            case ModuleEngines engine:
                                {
                                    if (!_engines.ContainsKey(engine))
                                        _engines.Add(engine, null);
                                    break;
                                }
                            case ModuleResourceIntake intake:
                                iinfo.AddIntake(intake);
                                break;
                            case ModuleParachute parachute:
                                {
                                    Parachutes.Add(parachute);
                                    if (parachute.deploymentState == ModuleParachute.deploymentStates.DEPLOYED ||
                                        parachute.deploymentState == ModuleParachute.deploymentStates.SEMIDEPLOYED)
                                    {
                                        ParachuteDeployed = true;
                                    }

                                    break;
                                }
                            // also does ModuleAeroSurface
                            case ModuleControlSurface surface:
                                {
                                    //if (p.ShieldedFromAirstream || cs.deploy)
                                    //    continue;

                                    surface.GetPotentialTorque(out Vector3 ctrlTorquePos, out Vector3 ctrlTorqueNeg);

                                    TorqueControlSurface.Add(ctrlTorquePos);
                                    TorqueControlSurface.Add(ctrlTorqueNeg);

                                    if (surface.useExponentialSpeed)
                                    {
                                        float effectiveActuatorDelay = (float)Clamp(50 - surface.actuatorSpeed / surface.ctrlSurfaceRange, 0, 50);

                                        torqueWeightedExponentialResponseDelay6.Positive = effectiveActuatorDelay * ctrlTorquePos.Abs();
                                        torqueWeightedExponentialResponseDelay6.Negative = effectiveActuatorDelay * ctrlTorqueNeg.Abs();
                                    }
                                    else
                                    {
                                        // 10 is a fudge factor for the difference in response time between a linear ramp and an exponential curve.
                                        // XXX: this should probably be tweakable
                                        float effectiveActuatorDelay = (float)Clamp(50 - 10 * surface.actuatorSpeed / surface.ctrlSurfaceRange, 0, 50);

                                        torqueWeightedLinearResponseDelay6.Positive = effectiveActuatorDelay * ctrlTorquePos.Abs();
                                        torqueWeightedLinearResponseDelay6.Negative = effectiveActuatorDelay * ctrlTorqueNeg.Abs();
                                    }

                                    torqueReactionSpeed6.Add(Mathf.Abs(surface.ctrlSurfaceRange) / surface.actuatorSpeed *
                                        Vector3d.Max(ctrlTorquePos.Abs(), ctrlTorqueNeg.Abs()));
                                    break;
                                }
                            case ModuleGimbal gimbal:
                                {
                                    if (gimbal.engineMultsList == null)
                                        gimbal.CreateEngineList();

                                    // ReSharper disable once NullableWarningSuppressionIsUsed
                                    foreach (List<KeyValuePair<ModuleEngines, float>> engs in gimbal.engineMultsList!)
                                    {
                                        foreach (KeyValuePair<ModuleEngines, float> t in engs)
                                        {
                                            _engines[t.Key] = gimbal;
                                        }
                                    }

                                    gimbal.GetPotentialTorque(out Vector3 pos, out Vector3 neg);

                                    // GetPotentialTorque reports the same value for pos & neg on ModuleGimbal

                                    TorqueGimbal.Add(pos);
                                    TorqueGimbal.Add(-neg);

                                    float effectiveGimbalDelay = (float)Clamp(50 - gimbal.gimbalResponseSpeed, 0, 50);

                                    if (gimbal.useGimbalResponseSpeed)
                                    {
                                        torqueWeightedExponentialResponseDelay6.Positive += effectiveGimbalDelay * pos.Abs();
                                        torqueWeightedExponentialResponseDelay6.Negative += effectiveGimbalDelay * neg.Abs();

                                        torqueReactionSpeed6.Add(Mathf.Abs(gimbal.gimbalRange) / gimbal.gimbalResponseSpeed * Vector3d.Max(pos.Abs(), neg.Abs()));
                                    }

                                    break;
                                }
                            case ModuleRCS _:
                                // Already handled earlier. Prevent the generic ITorqueProvider to catch it
                                break;
                            // All mod that supports it. Including FAR
                            case ITorqueProvider provider:
                                {
                                    provider.GetPotentialTorque(out Vector3 pos, out Vector3 neg);

                                    TorqueOthers.Add(pos);
                                    TorqueOthers.Add(neg);
                                    break;
                                }
                        }

                    foreach (VesselStatePartModuleExtension vspme in VesselStatePartModuleExtensions)
                        vspme(pm);
                }

                foreach (KeyValuePair<ModuleEngines, ModuleGimbal?> engine in _engines)
                {
                    einfo.AddNewEngine(engine.Key, engine.Value, EngineWrappers, ref CoT, ref DoT, ref CoTMagnitude);
                    einfo.CheckUllageStatus(engine.Key);
                }

                PureDragVector += partPureDrag;
                PureLiftVector += partPureLift;

                Vector3d partAeroForce = partPureDrag + partPureLift;

                var partDrag = Vector3d.Project(partAeroForce, -SurfaceVelocity);
                Vector3d partLift = partAeroForce - partDrag;

                double partLiftScalar = partLift.magnitude;

                if (p.rb != null && partLiftScalar > 0.01)
                {
                    CoLMagnitude += partLiftScalar;
                    CoL += ((Vector3d)p.rb.worldCenterOfMass + (Vector3d)(p.partTransform.rotation * p.CoLOffset)) * partLiftScalar;
                }
            }

            TorqueAvailable += Vector3d.Max(TorqueReactionWheel.Positive, TorqueReactionWheel.Negative);

            //torqueAvailable += Vector3d.Max(torqueRcs.positive, torqueRcs.negative);

            TorqueAvailable += Vector3d.Max(RCSTorqueAvailable.Positive, RCSTorqueAvailable.Negative);

            TorqueAvailable += Vector3d.Max(TorqueControlSurface.Positive, TorqueControlSurface.Negative);

            TorqueAvailable += Vector3d.Max(TorqueGimbal.Positive, TorqueGimbal.Negative);

            TorqueAvailable += Vector3d.Max(TorqueOthers.Positive, TorqueOthers.Negative); // Mostly FAR

            TorqueDifferentialThrottle = Vector3d.Max(einfo.TorqueDifferentialThrottle.Positive, einfo.TorqueDifferentialThrottle.Negative);
            TorqueDifferentialThrottle.y = 0;

            if (TorqueAvailable.sqrMagnitude > 0)
            {
                TorqueReactionSpeed = Vector3d.Max(torqueReactionSpeed6.Positive, torqueReactionSpeed6.Negative);
                TorqueReactionSpeed.Scale(TorqueAvailable.InvertNoNaN());

                TorqueWeightedExponentialResponseDelay = Vector3d.Max(torqueWeightedExponentialResponseDelay6.Positive, torqueWeightedExponentialResponseDelay6.Negative);
                TorqueWeightedLinearResponseDelay = Vector3d.Max(torqueWeightedLinearResponseDelay6.Positive, torqueWeightedLinearResponseDelay6.Negative);
                // XXX: this is a little bogus, but we should be underestimating the response time of the linear filter, but
                // we should be preserving the right gain margin and noise sensitivity.
                Vector3d torqueResponseDelay = TorqueWeightedExponentialResponseDelay + TorqueWeightedLinearResponseDelay;
                torqueResponseDelay.Scale(TorqueAvailable.InvertNoNaN());
                TorqueResponseSpeed = new Vector3(50, 50, 50) - torqueResponseDelay;
            }
            else
            {
                TorqueReactionSpeed = Vector3d.zero;
                TorqueResponseSpeed = new Vector3(50, 50, 50);
            }

            ThrustVectorMaxThrottle = einfo.ThrustMax;
            ThrustVectorMinThrottle = einfo.ThrustMin;
            ThrustVectorLastFrame = einfo.ThrustCurrent;

            if (CoTMagnitude > 0)
            {
                CoT /= CoTMagnitude;
                ThrustForward = (CoM - CoT).normalized;
                // In certain circumstances, like hotstaging, the CoM of the Vessel can be behind the CoT before
                // decoupling and while dragging the previous stage.  In that case thrustForward can wind up pointing
                // backwards, which is not what we want.  If that happens, we just set it to the forward vector.
                if (Vector3d.Dot(ThrustForward, Forward) < 0)
                    ThrustForward = Forward;
            }

            DoT = DoT.normalized;

            if (CoLMagnitude > 0)
                CoL /= CoLMagnitude;

            Vector3d liftDir = -Vector3d.Cross(_vessel.transform.right, -SurfaceVelocity.normalized);

            if (ReflectionUtils.IsLoadedFAR && !_vessel.packed && SurfaceVelocity != Vector3d.zero)
            {
                CalculateVesselAeroForcesWithCache(_vessel, out Vector3 farForce, SurfaceVelocity, AltitudeASL);

                Vector3d farDragVector = Vector3d.Dot(farForce, -SurfaceVelocity.normalized) * -SurfaceVelocity.normalized;
                DragForce = farDragVector.magnitude;
                DragAcceleration = farDragVector.magnitude / Mass;
                PureDragVector = farDragVector / Mass;
                PureDrag = DragAcceleration;

                Vector3d farLiftVector = Vector3d.Dot(farForce, liftDir) * liftDir;
                LiftForce = farLiftVector.magnitude;
                LiftAcceleration = farLiftVector.magnitude / Mass;
                PureLiftVector = farLiftVector / Mass;
                PureLift = LiftAcceleration;
            }
            else
            {
                Vector3d force = PureDragVector + PureLiftVector;

                PureDragVector /= Mass;
                PureLiftVector /= Mass;
                PureDrag = PureDragVector.magnitude;
                PureLift = PureLiftVector.magnitude;

                // Drag is the part (pureDrag + PureLift) applied opposite of the surface vel
                DragForce = Vector3d.Dot(force, -SurfaceVelocity.normalized);
                DragAcceleration = DragForce / Mass;
                // Lift is the part (pureDrag + PureLift) applied in the "Lift" direction
                LiftForce = Vector3d.Dot(force, liftDir);
                LiftAcceleration = LiftForce / Mass;
            }

            MaxEngineResponseTime = einfo.MaxResponseTime;
        }

        [GeneralInfoItem("#MechJeb_Torque", InfoItem.Category.Vessel, showInEditor = true)] //Torque
        public void TorqueCompare()
        {
            var reactionTorque = Vector3d.Max(TorqueReactionWheel.Positive, TorqueReactionWheel.Negative);
            //var rcsTorque = Vector3d.Max(torqueRcs.positive, torqueRcs.negative);

            var rcsTorqueMJ = Vector3d.Max(RCSTorqueAvailable.Positive, RCSTorqueAvailable.Negative);

            var controlTorque = Vector3d.Max(TorqueControlSurface.Positive, TorqueControlSurface.Negative);
            var gimbalTorque = Vector3d.Max(TorqueGimbal.Positive, TorqueGimbal.Negative);
            var diffTorque = Vector3d.Max(_einfo.TorqueDifferentialThrottle.Positive, _einfo.TorqueDifferentialThrottle.Negative);
            diffTorque.y = 0;
            var othersTorque = Vector3d.Max(TorqueOthers.Positive, TorqueOthers.Negative);

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
            GUILayout.Label(MuUtils.PrettyPrint(reactionTorque), GuiUtils.LabelNoWrap, GuiUtils.LayoutNoExpandWidth);
            //GUILayout.Label(MuUtils.PrettyPrint(rcsTorque), GuiUtils.LabelNoWrap, GuiUtils.LayoutNoExpandWidth);

            GUILayout.Label(MuUtils.PrettyPrint(rcsTorqueMJ), GuiUtils.LabelNoWrap, GuiUtils.LayoutNoExpandWidth);

            GUILayout.Label(MuUtils.PrettyPrint(controlTorque), GuiUtils.LabelNoWrap, GuiUtils.LayoutNoExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(gimbalTorque), GuiUtils.LabelNoWrap, GuiUtils.LayoutNoExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(diffTorque), GuiUtils.LabelNoWrap, GuiUtils.LayoutNoExpandWidth);
            GUILayout.Label(MuUtils.PrettyPrint(othersTorque), GuiUtils.LabelNoWrap, GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void UpdateResourceRequirements(EngineInfo einfo, IntakeInfo iinfo)
        {
            // Convert the resource information from the einfo and iinfo format
            // to the more useful ResourceInfo format.
            ResourceInfo.Release(Resources.Values);
            Resources.Clear();
            foreach (KeyValuePair<int, EngineInfo.FuelRequirement> info in einfo.ResourceRequired)
            {
                int id = info.Key;
                EngineInfo.FuelRequirement req = info.Value;
                Resources[id] = ResourceInfo.Borrow(
                    PartResourceLibrary.Instance.GetDefinition(id),
                    req.RequiredLastFrame,
                    req.RequiredAtMaxThrottle,
                    iinfo.GetIntakes(id),
                    _vessel);
            }

            int intakeAirId = PartResourceLibrary.Instance.GetDefinition("IntakeAir").id;
            IntakeAir = 0;
            IntakeAirNeeded = 0;
            IntakeAirAtMax = 0;
            IntakeAirAllIntakes = 0;
            if (Resources.ContainsKey(intakeAirId))
            {
                IntakeAir = Resources[intakeAirId].IntakeProvided;
                IntakeAirAllIntakes = Resources[intakeAirId].IntakeAvailable;
                IntakeAirNeeded = Resources[intakeAirId].Required;
                IntakeAirAtMax = Resources[intakeAirId].RequiredAtMaxThrottle;
            }
        }

        // Decide whether to control the RCS thrusters from the main throttle
        private void ToggleRCSThrust()
        {
            if (ThrustVectorMaxThrottle.magnitude == 0 && _vessel.ActionGroups[KSPActionGroup.RCS])
            {
                RCSThrust = true;
                ThrustVectorMaxThrottle += Forward * RCSThrustAvailable.Down;
            }
            else
            {
                RCSThrust = false;
            }
        }

        private void UpdateMoIAndAngularMom()
        {
            MoI = _vessel.MOI;
            AngularMomentum = _vessel.angularMomentum;
        }

        [ValueInfoItem("#MechJeb_TerminalVelocity", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s")] //Terminal velocity
        public double TerminalVelocity()
        {
            if (ReflectionUtils.IsLoadedFAR)
                return FARVesselTermVelEst.Call(_vessel);

            if (MainBody == null || AltitudeASL > MainBody.RealMaxAtmosphereAltitude())
                return double.PositiveInfinity;

            return Math.Sqrt(2000 * Mass * LocalGravity / (AreaDrag * _vessel.atmDensity));
        }

        public double ThrustAccel(double throttle) => (1.0 - throttle) * MinThrustAcceleration + throttle * MaxThrustAcceleration;

        public double HeadingFromDirection(Vector3d dir) =>
            MuUtils.ClampDegrees360(UtilMath.Rad2Deg * Math.Atan2(Vector3d.Dot(dir, East), Vector3d.Dot(dir, North)));

        private double ComputeVesselBottomAltitude()
        {
            if (_vessel == null || _vessel.rootPart.rb == null) return 0;
            double ret = AltitudeTrue;
            foreach (Part p in _vessel.parts)
            {
                if (p.collider == null) continue;

                /*Vector3d bottomPoint = p.collider.ClosestPointOnBounds(vesselmainBody.position);
                    double partBottomAlt = vesselmainBody.GetAltitude(bottomPoint) - surfaceAltitudeASL;
                    _altitudeBottom = Math.Max(0, Math.Min(_altitudeBottom, partBottomAlt));*/
                Bounds bounds = p.collider.bounds;
                Vector3 extents = bounds.extents;
                float partRadius = Mathf.Max(extents[0], Mathf.Max(extents[1], extents[2]));
                double partAltitudeBottom = _vessel.mainBody.GetAltitude(bounds.center) - partRadius - SurfaceAltitudeASL;
                partAltitudeBottom = Math.Max(0, partAltitudeBottom);
                if (partAltitudeBottom < ret)
                    ret = partAltitudeBottom;
            }

            return ret;
        }

        // Used during the vesselState constructor; distilled to other
        // variables later.
        public class EngineInfo
        {
            private readonly Queue _rotSave = new Queue();

            public readonly Dictionary<int, FuelRequirement> ResourceRequired = new Dictionary<int, FuelRequirement>();

            public readonly Vector6 TorqueDifferentialThrottle = new Vector6();
            private float _atmP0; // pressure now
            private float _atmP1; // pressure after one timestep

            private Vector3d _com;

            // lowestUllage is always VeryStable without RealFuels installed
            // Don't wait for Update, set it now so we don't invite a race condition.
            public double LowestUllage = 1.0;
            public double MaxResponseTime;
            public Vector3d ThrustCurrent; // thrust at throttle achieved last frame
            public Vector3d ThrustMax; // thrust at full throttle
            public Vector3d ThrustMin; // thrust at zero throttle

            public void Update(Vector3d c, Vessel vessel)
            {
                ThrustCurrent = Vector3d.zero;
                ThrustMax = Vector3d.zero;
                ThrustMin = Vector3d.zero;
                MaxResponseTime = 0;

                TorqueDifferentialThrottle.Reset();

                ResourceRequired.Clear();

                LowestUllage = 1.0;

                _com = c;

                _atmP0 = (float)(vessel.staticPressurekPa * PhysicsGlobals.KpaToAtmospheres);
                float alt1 = (float)(vessel.altitude + TimeWarp.fixedDeltaTime * vessel.verticalSpeed);
                _atmP1 = (float)(FlightGlobals.getStaticPressure(alt1) * PhysicsGlobals.KpaToAtmospheres);
            }

            public void CheckUllageStatus(ModuleEngines e)
            {
                if (!IsRealFuelsCorrectlyInitialized)
                    return;

                // we report stable ullage for an unstable engine which is throttled up, so we let RF kill it
                // instead of having MJ throttle it down.
                if (e.getFlameoutState || !e.EngineIgnited || !e.isEnabled || e.requestedThrottle > 0.0F)
                    return;

                if (!RFModuleEnginesRFType.IsInstance(e))
                    return;

                bool ullage = RFullageField.GetValue<bool>(e);
                if (!ullage)
                    return;

                /* ullage is 'stable' if the engine has no ignitions left */
                int ignitions = RFignitionsField.GetValue<int>(e);

                /* -1 => infinite ignitions;  0 => no ignitions left;  1+ => ignitions remaining */
                if (ignitions == 0)
                    return;

                // finally we have an ignitable engine (that isn't already ignited), so check its propellant status

                // need to call RFullageSet to get the UllageSet then call GetUllageStability on that.
                object ullageSet = RFullageSetField.GetValue<object>(e);

                double propellantStability = (double)RFGetUllageStabilityMethod.Invoke(ullageSet, Array.Empty<object>());

                if (propellantStability < LowestUllage)
                    LowestUllage = propellantStability;
            }

            public void AddNewEngine(ModuleEngines e, ModuleGimbal? gimbal, List<EngineWrapper> enginesWrappers, ref Vector3d cot, ref Vector3d dot,
                ref double coTScalar)
            {
                // FIXME: shouldn't we gather statistics on unignited engines???
                if (!e.EngineIgnited || !e.isEnabled)
                    return;

                // Compute the resource requirement at full thrust.
                //   mdot = maxthrust / (Isp * g0) in tonnes per second
                //   udot = mdot / mixdensity in units per second of propellant per ratio unit
                //   udot * ratio_i : units per second of propellant i
                // Choose the worse Isp between now and after one timestep.
                float isp0 = e.atmosphereCurve.Evaluate(_atmP0);
                float isp1 = e.atmosphereCurve.Evaluate(_atmP1);
                float isp = Mathf.Min(isp0, isp1);

                foreach (Propellant propellant in e.propellants)
                {
                    double maxreq = e.maxFuelFlow * propellant.ratio;
                    AddResource(propellant.id, propellant.currentRequirement, maxreq);
                }

                if (!e.isOperational) return;

                float thrustLimiter = e.thrustPercentage / 100f;

                double maxThrust = e.maxFuelFlow * e.flowMultiplier * isp * e.g;
                double minThrust = e.minFuelFlow * e.flowMultiplier * isp * e.g;

                // RealFuels engines reports as operational even when they are shutdown
                // REMOVED: this definitively screws up the 1kN thruster in RO/RF and sets minThrust/maxThrust
                // to zero when the engine is just throttled down -- which screws up suicide burn calcs, etc.
                // if (e.finalThrust == 0f && minThrust > 0f)
                //    minThrust = maxThrust = 0;

                //MechJebCore.print(maxThrust.ToString("F2") + " " + minThrust.ToString("F2") + " " + e.minFuelFlow.ToString("F2") + " " + e.maxFuelFlow.ToString("F2") + " " + e.flowMultiplier.ToString("F2") + " " + Isp.ToString("F2") + " " + thrustLimiter.ToString("F3"));

                double eMaxThrust = minThrust + (maxThrust - minThrust) * thrustLimiter;
                double eMinThrust = e.throttleLocked ? eMaxThrust : minThrust;
                double eCurrentThrust = e.finalThrust;

                _rotSave.Clear();

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
                    _rotSave.Clear();
                    for (int i = 0; i < gimbal.gimbalTransforms.Count; i++)
                    {
                        Transform gimbalTransform = gimbal.gimbalTransforms[i];
                        _rotSave.Enqueue(gimbalTransform.localRotation);
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

                    ThrustCurrent += tCurrentThrust * cosineLosses * thrustDirectionVector;
                    ThrustMax += eMaxThrust * cosineLosses * thrustDirectionVector * thrustTransformMultiplier;
                    ThrustMin += eMinThrust * cosineLosses * thrustDirectionVector * thrustTransformMultiplier;

                    cot += tCurrentThrust * (Vector3d)transform.position;
                    dot -= tCurrentThrust * thrustDirectionVector;
                    coTScalar += tCurrentThrust;

                    Quaternion inverseVesselRot = e.part.vessel.ReferenceTransform.rotation.Inverse();
                    Vector3d thrustDir = inverseVesselRot * thrustDirectionVector;
                    Vector3d pos = inverseVesselRot * (transform.position - _com);

                    maxVariableForce += (currentMaxThrust - currentMinThrust) * thrustDir * thrustTransformMultiplier;
                    constantForce += currentMinThrust * thrustDir * thrustTransformMultiplier;
                    maxVariableTorque += (currentMaxThrust - currentMinThrust) * thrustTransformMultiplier * Vector3d.Cross(pos, thrustDir);
                    constantTorque += currentMinThrust * thrustTransformMultiplier * Vector3d.Cross(pos, thrustDir);

                    if (!e.throttleLocked)
                    {
                        TorqueDifferentialThrottle.Add(Vector3d.Cross(pos, thrustDir) * (float)(maxThrust - minThrust) * thrustTransformMultiplier);
                    }
                }

                enginesWrappers.Add(new EngineWrapper(e, maxVariableForce, constantTorque, maxVariableTorque));

                // Restore gimbals rotation
                if (gimbal != null && !gimbal.gimbalLock)
                {
                    foreach (Transform t in gimbal.gimbalTransforms)
                        t.localRotation = (Quaternion)_rotSave.Dequeue();
                }

                if (e.useEngineResponseTime)
                {
                    double responseTime = 1.0 / Math.Min(e.engineAccelerationSpeed, e.engineDecelerationSpeed);
                    if (responseTime > MaxResponseTime) MaxResponseTime = responseTime;
                }
            }

            private void AddResource(int id, double current /* u/sec */, double max /* u/sec */)
            {
                FuelRequirement req;
                if (ResourceRequired.TryGetValue(id, out FuelRequirement value))
                {
                    req = value;
                }
                else
                {
                    req = new FuelRequirement();
                    ResourceRequired[id] = req;
                }

                req.RequiredLastFrame += current;
                req.RequiredAtMaxThrottle += max;
            }

            public struct FuelRequirement
            {
                public double RequiredLastFrame;
                public double RequiredAtMaxThrottle;
            }
        }

        // Used during the vesselState constructor; distilled to other variables later.
        private class IntakeInfo
        {
            private static readonly List<ModuleResourceIntake> _empty = new List<ModuleResourceIntake>();
            private readonly Dictionary<int, List<ModuleResourceIntake>> _allIntakes = new Dictionary<int, List<ModuleResourceIntake>>();

            public void Update()
            {
                foreach (List<ModuleResourceIntake> intakes in _allIntakes.Values)
                {
                    ListPool<ModuleResourceIntake>.Instance.Release(intakes);
                }

                _allIntakes.Clear();
            }

            public void AddIntake(ModuleResourceIntake intake)
            {
                // TODO: figure out how much airflow we have, how much we could have,
                // drag, etc etc.
                List<ModuleResourceIntake> thelist;
                int id = PartResourceLibrary.Instance.GetDefinition(intake.resourceName).id;
                if (_allIntakes.TryGetValue(id, out List<ModuleResourceIntake> allIntake))
                {
                    thelist = allIntake;
                }
                else
                {
                    thelist = ListPool<ModuleResourceIntake>.Instance.Borrow();
                    _allIntakes[id] = thelist;
                }

                thelist.Add(intake);
            }

            public List<ModuleResourceIntake> GetIntakes(int id) => _allIntakes.TryGetValue(id, out List<ModuleResourceIntake> intakes) ? intakes : _empty;
        }

        // Stored.
        public sealed class ResourceInfo
        {
            private static readonly Pool<ResourceInfo> _pool = new Pool<ResourceInfo>(Create, Reset);

            public readonly List<IntakeData> Intakes = new List<IntakeData>();
            public double IntakeAvailable; // kg/s

            // We use kg/s rather than the more common T/s because these numbers tend to be small.
            // One debate I've had is whether to use mass/s or unit/s.  Dunno...

            public double Required; // kg/s
            public double RequiredAtMaxThrottle; // kg/s

            private ResourceInfo()
            {
            }

            public double IntakeProvided
            {
                // kg/s for currently-open intakes
                get
                {
                    double sum = 0;
                    foreach (IntakeData intakeData in Intakes)
                    {
                        if (intakeData.Intake.intakeEnabled)
                            sum += intakeData.PredictedMassFlow;
                    }

                    return sum;
                }
            }

            public static int PoolSize => _pool.Size;

            private static ResourceInfo Create() => new ResourceInfo();

            public void Release() => _pool.Release(this);

            public static void Release(Dictionary<int, ResourceInfo>.ValueCollection objList)
            {
                foreach (ResourceInfo resourceInfo in objList)
                {
                    resourceInfo.Release();
                }
            }

            private static void Reset(ResourceInfo obj)
            {
                obj.Required = 0;
                obj.RequiredAtMaxThrottle = 0;
                obj.IntakeAvailable = 0;
                obj.Intakes.Clear();
            }

            public static ResourceInfo Borrow(PartResourceDefinition r, double req /* u per deltaT */, double atMax /* u per s */,
                List<ModuleResourceIntake> modules, Vessel vessel)
            {
                ResourceInfo resourceInfo = _pool.Borrow();
                resourceInfo.Init(r, req /* u per deltaT */, atMax /* u per s */, modules, vessel);
                return resourceInfo;
            }

            private void Init(PartResourceDefinition r, double req /* u per deltaT */, double atMax /* u per s */, List<ModuleResourceIntake> modules,
                Vessel vessel)
            {
                double density = r.density * 1000; // kg per unit (density is in T per unit)
                float dT = TimeWarp.fixedDeltaTime;
                Required = req * density / dT;
                RequiredAtMaxThrottle = atMax * density;

                // For each intake, we want to know the min of what will (or can) be provided either now or at the end of the timestep.
                // 0 means now, 1 means next timestep
                Vector3d v0 = vessel.srf_velocity;
                Vector3d v1 = v0 + dT * vessel.acceleration;
                Vector3d v0Unit = v0.normalized;
                Vector3d v1Unit = v1.normalized;
                double v0Mag = v0.magnitude;
                double v1Mag = v1.magnitude;

                float alt1 = (float)(vessel.altitude + dT * vessel.verticalSpeed);

                double staticPressure1 = vessel.staticPressurekPa;
                double staticPressure2 = FlightGlobals.getStaticPressure(alt1);

                // As with thrust, here too we should get the static pressure at the intake, not at the center of mass.
                double atmDensity0 = FlightGlobals.getAtmDensity(staticPressure1, vessel.externalTemperature);
                double atmDensity1 = FlightGlobals.getAtmDensity(staticPressure2, FlightGlobals.getExternalTemperature(alt1));

                double v0SpeedOfSound = vessel.mainBody.GetSpeedOfSound(staticPressure1, atmDensity0);
                double v1SpeedOfSound = vessel.mainBody.GetSpeedOfSound(staticPressure2, atmDensity1);

                float v0Mach = v0SpeedOfSound > 0 ? (float)(v0.magnitude / v0SpeedOfSound) : 0;
                float v1Mach = v1SpeedOfSound > 0 ? (float)(v1.magnitude / v1SpeedOfSound) : 0;

                Intakes.Clear();
                foreach (ModuleResourceIntake intake in modules)
                {
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

                    double mass0 = MassProvided(v0Mag, v0Unit, atmDensity0, staticPressure1, v0Mach, intake, intakeFwd0);
                    double mass1 = MassProvided(v1Mag, v1Unit, atmDensity1, staticPressure2, v1Mach, intake, intakeFwd1);
                    double mass = Math.Min(mass0, mass1);

                    // Also, we can't have more airflow than what fits in the resource tank of the intake part.
                    double capacity = 0;
                    foreach (PartResource tank in intake.part.Resources)
                    {
                        if (tank.info.id == r.id)
                            capacity += tank.maxAmount; // units per timestep
                    }

                    capacity = capacity * density / dT; // convert to kg/s
                    mass = Math.Min(mass, capacity);

                    Intakes.Add(new IntakeData(intake, mass));
                }
            }

            // Return the number of kg of resource provided per second under certain conditions.
            // We use kg since the numbers are typically small.
            private double MassProvided(double vesselSpeed, Vector3d normVesselSpeed, double atmDensity, double staticPressure, float mach,
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
                if (aoa < 0) { aoa = 0; }
                else if (aoa > 1) { aoa = 1; }

                double finalSpeed = intakeSpeed + aoa * vesselSpeed;

                double airVolume = finalSpeed * intake.area * intake.unitScalar * intake.machCurve.Evaluate(mach);
                double airmass = atmDensity * airVolume; // tonnes per second

                // TODO: limit by the amount the intake can store
                return airmass * 1000;
            }

            public struct IntakeData
            {
                public IntakeData(ModuleResourceIntake intake, double predictedMassFlow)
                {
                    Intake = intake;
                    PredictedMassFlow = predictedMassFlow;
                }

                public readonly ModuleResourceIntake Intake;
                public readonly double PredictedMassFlow; // min kg/s this timestep or next
            }
        }

        public class EngineWrapper
        {
            public readonly ModuleEngines Engine;

            public EngineWrapper(ModuleEngines module, Vector3d maxVariableForce, Vector3d constantTorque,
                Vector3d maxVariableTorque)
            {
                Engine = module;
                MaxVariableForce = maxVariableForce;
                ConstantTorque = constantTorque;
                MaxVariableTorque = maxVariableTorque;
            }

            public float ThrustRatio
            {
                get => Engine.thrustPercentage / 100;
                set => Engine.thrustPercentage = value * 100;
            }

            public Vector3d MaxVariableForce { get; }

            public Vector3d ConstantTorque { get; }

            public Vector3d MaxVariableTorque { get; }
        }
    }
}
