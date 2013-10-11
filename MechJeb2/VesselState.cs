using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //Copied verbatim from old VesselState. Should this class be reorganized at all?

    public class VesselState
    {
        [ValueInfoItem("Universal Time", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)]
        public double time;            //planetarium time
        public double deltaT;          //TimeWarp.fixedDeltaTime

        public Vector3d CoM;
        Matrix3x3 inertiaTensor = new Matrix3x3();
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
        public Vector3d velocityVesselSurface;
        public Vector3d velocityVesselSurfaceUnit;
        public Vector3d velocityVesselOrbit;
        public Vector3d velocityVesselOrbitUnit;

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
        [ValueInfoItem("Altitude (bottom)", InfoItem.Category.Surface, format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = 0, units = "m")]
        public double altitudeBottom = 0;
        [ValueInfoItem("Apoapsis", InfoItem.Category.Orbit, units = "m", format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = 0, category = InfoItem.Category.Orbit)]
        public MovingAverage orbitApA = new MovingAverage();
        [ValueInfoItem("Periapsis", InfoItem.Category.Orbit, units = "m", format = ValueInfoItem.SI, siSigFigs = 6, siMaxPrecision = 0, category = InfoItem.Category.Orbit)]
        public MovingAverage orbitPeA = new MovingAverage();
        [ValueInfoItem("Orbital period", InfoItem.Category.Orbit, format = ValueInfoItem.TIME, category = InfoItem.Category.Orbit)]
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

        public double radius;  //distance from planet center

        public double mass;
        public double thrustAvailable;
        public double thrustMinimum;
        public double maxThrustAccel; //thrustAvailable / mass
        public float throttleLimit = 1;
        public double limitedMaxThrustAccel { get { return maxThrustAccel * throttleLimit; } }
        public double minThrustAccel;      //some engines (particularly SRBs) have a minimum thrust so this may be nonzero
        public Vector3d torqueAvailable;
        public double torqueThrustPYAvailable;
        public double massDrag;
        public double atmosphericDensity;
        [ValueInfoItem("Atmosphere density", InfoItem.Category.Misc, format = ValueInfoItem.SI, units = "g/m³")]
        public double atmosphericDensityGrams;
        [ValueInfoItem("Intake air", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")]
        public double intakeAir;
        [ValueInfoItem("Intake air needed", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")]
        public double intakeAirNeeded;
        [ValueInfoItem("Intake air needed (max)", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "kg/s")]
        public double intakeAirAtMax;
        [ValueInfoItem("Angle to prograde", InfoItem.Category.Orbit, format = "F2", units = "º")]
        public double angleToPrograde;

        public Vector6 rcsThrustAvailable;
        public Vector6 rcsTorqueAvailable;

        public Vector6 ctrlTorqueAvailable;


        // Resource information keyed by resource Id.
        public Dictionary<int, ResourceInfo> resources;

        public CelestialBody mainBody;

        public void Update(Vessel vessel)
        {
            if (vessel.rigidbody == null) return; //if we try to update before rigidbodies exist we spam the console with NullPointerExceptions.
            //if (vessel.packed) return;

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

            velocityVesselOrbit = vessel.orbit.GetVel();
            velocityVesselOrbitUnit = velocityVesselOrbit.normalized;
            velocityVesselSurface = velocityVesselOrbit - vessel.mainBody.getRFrmVel(CoM);
            velocityVesselSurfaceUnit = velocityVesselSurface.normalized;
            velocityMainBodySurface = rotationSurface * velocityVesselSurface;

            horizontalOrbit = Vector3d.Exclude(up, velocityVesselOrbit).normalized;
            horizontalSurface = Vector3d.Exclude(up, velocityVesselSurface).normalized;

            angularVelocity = Quaternion.Inverse(vessel.GetTransform().rotation) * vessel.rigidbody.angularVelocity;

            radialPlusSurface = Vector3d.Exclude(velocityVesselSurface, up).normalized;
            radialPlus = Vector3d.Exclude(velocityVesselOrbit, up).normalized;
            normalPlusSurface = -Vector3d.Cross(radialPlusSurface, velocityVesselSurfaceUnit);
            normalPlus = -Vector3d.Cross(radialPlus, velocityVesselOrbitUnit);

            gravityForce = FlightGlobals.getGeeForceAtPosition(CoM);
            localg = gravityForce.magnitude;

            speedOrbital.value = velocityVesselOrbit.magnitude;
            speedSurface.value = velocityVesselSurface.magnitude;
            speedVertical.value = Vector3d.Dot(velocityVesselSurface, up);
            speedSurfaceHorizontal.value = (velocityVesselSurface - (speedVertical * up)).magnitude;
            speedOrbitHorizontal = (velocityVesselOrbit - (speedVertical * up)).magnitude;

            vesselHeading.value = rotationVesselSurface.eulerAngles.y;
            vesselPitch.value = (rotationVesselSurface.eulerAngles.x > 180) ? (360.0 - rotationVesselSurface.eulerAngles.x) : -rotationVesselSurface.eulerAngles.x;
            vesselRoll.value = (rotationVesselSurface.eulerAngles.z > 180) ? (rotationVesselSurface.eulerAngles.z - 360.0) : rotationVesselSurface.eulerAngles.z;

            altitudeASL.value = vessel.mainBody.GetAltitude(CoM);
            RaycastHit sfc;
            if (Physics.Raycast(CoM, -up, out sfc, (float)altitudeASL + 10000.0F, 1 << 15))
            {
                altitudeTrue.value = sfc.distance;
            }
            else if (vessel.mainBody.pqsController != null)
            {
                // from here: http://kerbalspaceprogram.com/forum/index.php?topic=10324.msg161923#msg161923
                altitudeTrue.value = vessel.mainBody.GetAltitude(CoM) - (vessel.mainBody.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(vessel.mainBody.GetLongitude(CoM), Vector3d.down) * QuaternionD.AngleAxis(vessel.mainBody.GetLatitude(CoM), Vector3d.forward) * Vector3d.right) - vessel.mainBody.pqsController.radius);
            }
            else
            {
                altitudeTrue.value = vessel.mainBody.GetAltitude(CoM);
            }

            double surfaceAltitudeASL = altitudeASL - altitudeTrue;
            altitudeBottom = altitudeTrue;
            foreach (Part p in vessel.parts)
            {
                if (p.collider != null)
                {
                    Vector3d bottomPoint = p.collider.ClosestPointOnBounds(vessel.mainBody.position);
                    double partBottomAlt = vessel.mainBody.GetAltitude(bottomPoint) - surfaceAltitudeASL;
                    altitudeBottom = Math.Max(0, Math.Min(altitudeBottom, partBottomAlt));
                }
            }

            double atmosphericPressure = FlightGlobals.getStaticPressure(altitudeASL, vessel.mainBody);
            if (atmosphericPressure < vessel.mainBody.atmosphereMultiplier * 1e-6) atmosphericPressure = 0;
            atmosphericDensity = FlightGlobals.getAtmDensity(atmosphericPressure);
            atmosphericDensityGrams = atmosphericDensity * 1000;

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

            mass = thrustAvailable = thrustMinimum = massDrag = torqueThrustPYAvailable = 0;
            torqueAvailable = new Vector3d();
            rcsThrustAvailable = new Vector6();
            rcsTorqueAvailable = new Vector6();
            ctrlTorqueAvailable = new Vector6();

            EngineInfo einfo = new EngineInfo(forward, CoM);
            IntakeInfo iinfo = new IntakeInfo();

            var rcsbal = vessel.GetMasterMechJeb().rcsbal;
            if (vessel.ActionGroups[KSPActionGroup.RCS] && rcsbal.enabled)
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
                                rcsThrustAvailable.Add(dir * Vector3d.Dot(force * throttles[i], dir));
                            }
                        }
                    }
                }
            }

            foreach (Part p in vessel.parts)
            {
                if (p.physicalSignificance != Part.PhysicalSignificance.NONE)
                {
                    double partMass = p.TotalMass();
                    mass += partMass;
                    massDrag += partMass * p.maximum_drag;
                }

                if (vessel.ActionGroups[KSPActionGroup.RCS] && !rcsbal.enabled)
                {
                    foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                    {
                        double maxT = pm.thrusterPower;
                        Vector3d partPosition = p.Rigidbody.worldCenterOfMass - CoM;

                        if ((pm.isEnabled) && (!pm.isJustForShow))
                        {
                            foreach (Transform t in pm.thrusterTransforms)
                            {
                                Vector3d thrusterThrust = -t.up * pm.thrusterPower;
                                rcsThrustAvailable.Add(thrusterThrust);
                                Vector3d thrusterTorque = vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, thrusterThrust));
                                rcsTorqueAvailable.Add(thrusterTorque);
                            }
                        }
                    }
                }

                if (p is ControlSurface)
                {
                    Vector3d partPosition = p.Rigidbody.worldCenterOfMass - CoM;
                    ControlSurface cs = (p as ControlSurface);
                    // Air Speed is velocityVesselSurface
                    // AddForceAtPosition seems to need the airspeed vector rotated with the flap rotation x its surface
                    Quaternion airSpeedRot = Quaternion.AngleAxis(cs.ctrlSurfaceRange * cs.ctrlSurfaceArea, cs.transform.rotation * cs.pivotAxis);
                    Vector3 ctrlTroquePos =  vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, cs.getLiftVector( airSpeedRot * velocityVesselSurface )));
                    Vector3 ctrlTroqueNeg =  vessel.GetTransform().InverseTransformDirection(Vector3.Cross(partPosition, cs.getLiftVector( Quaternion.Inverse(airSpeedRot) * velocityVesselSurface )));
                    ctrlTorqueAvailable.Add(ctrlTroquePos);
                    ctrlTorqueAvailable.Add(ctrlTroqueNeg);
                }

                if (p is CommandPod)
                {
                    torqueAvailable += Vector3d.one * Math.Abs(((CommandPod)p).rotPower);
                }

                foreach (PartModule pm in p.Modules)
                {
                    if (!pm.isEnabled) continue;

                    if (pm is ModuleReactionWheel)
                    {
                        ModuleReactionWheel rw = (ModuleReactionWheel)pm;
                        // It seems a RW with no electricity is still "Active" so we need to test for something else...
                        if (rw.wheelState == ModuleReactionWheel.WheelState.Active && !rw.stateString.Contains("Not enough"))
                            torqueAvailable += new Vector3d(rw.PitchTorque, rw.RollTorque, rw.YawTorque);
                    }

                    if (pm is ModuleEngines)
                    {
                        einfo.AddNewEngine(pm as ModuleEngines);
                    }
                    else if (pm is ModuleResourceIntake)
                    {
                        iinfo.addIntake(pm as ModuleResourceIntake);
                    }
                }
            }

            torqueAvailable += Vector3d.Max(rcsTorqueAvailable.positive, rcsTorqueAvailable.negative); // Should we use Max or Min ?
            torqueAvailable += Vector3d.Max(ctrlTorqueAvailable.positive, ctrlTorqueAvailable.negative); // Should we use Max or Min ?            

            thrustAvailable += einfo.thrustAvailable;
            thrustMinimum += einfo.thrustMinimum;
            torqueThrustPYAvailable += einfo.torqueThrustPYAvailable;

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
            if (resources.ContainsKey(intakeAirId))
            {
                intakeAir = resources[intakeAirId].intakeProvided;
                intakeAirNeeded = resources[intakeAirId].required;
                intakeAirAtMax = resources[intakeAirId].requiredAtMaxThrottle;
            }

            angularMomentum = new Vector3d(angularVelocity.x * MoI.x, angularVelocity.y * MoI.y, angularVelocity.z * MoI.z);

            maxThrustAccel = thrustAvailable / mass;
            minThrustAccel = thrustMinimum / mass;

            inertiaTensor = new Matrix3x3();
            foreach (Part p in vessel.parts)
            {
                if (p.Rigidbody == null) continue;

                //Compute the contributions to the vessel inertia tensor due to the part inertia tensor
                Vector3d principalMoments = p.Rigidbody.inertiaTensor;
                Quaternion princAxesRot = Quaternion.Inverse(vessel.GetTransform().rotation) * p.transform.rotation * p.Rigidbody.inertiaTensorRotation;
                Quaternion invPrincAxesRot = Quaternion.Inverse(princAxesRot);

                for (int i = 0; i < 3; i++)
                {
                    Vector3d iHat = Vector3d.zero;
                    iHat[i] = 1;
                    for (int j = 0; j < 3; j++)
                    {
                        Vector3d jHat = Vector3d.zero;
                        jHat[j] = 1;
                        inertiaTensor[i, j] += Vector3d.Dot(iHat, princAxesRot * Vector3d.Scale(principalMoments, invPrincAxesRot * jHat));
                    }
                }

                //Compute the contributions to the vessel inertia tensor due to the part mass and position
                double partMass = p.TotalMass();
                Vector3 partPosition = vessel.GetTransform().InverseTransformDirection(p.Rigidbody.worldCenterOfMass - CoM);

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

        //probably this should call a more general terminal velocity method
        [ValueInfoItem("Terminal velocity", InfoItem.Category.Vessel, format = ValueInfoItem.SI, units = "m/s")]
        public double TerminalVelocity()
        {
            if (altitudeASL > mainBody.RealMaxAtmosphereAltitude()) return double.PositiveInfinity;

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

        // Used during the vesselState constructor; distilled to other
        // variables later.
        public class EngineInfo
        {
            public double thrustAvailable = 0;
            public double thrustMinimum = 0;
            public double torqueThrustPYAvailable = 0;

            public class FuelRequirement
            {
                public double requiredLastFrame = 0;
                public double requiredAtMaxThrottle = 0;
            }
            public Dictionary<int, FuelRequirement> resourceRequired = new Dictionary<int, FuelRequirement>();

            Vector3d forward;
            Vector3d CoM;
            float atmP0; // pressure now
            float atmP1; // pressure after one timestep

            public EngineInfo(Vector3d fwd, Vector3d c)
            {
                forward = fwd;
                CoM = c;
                atmP0 = (float)FlightGlobals.getStaticPressure();
                float alt1 = (float)(FlightGlobals.ship_altitude + TimeWarp.fixedDeltaTime * FlightGlobals.ship_verticalSpeed);
                atmP1 = (float)FlightGlobals.getStaticPressure(alt1);
            }

            public void AddNewEngine(ModuleEngines e)
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
                //double udot = e.maxThrust / (Isp * 9.81 * e.mixtureDensity);
                double udot = e.maxThrust / (Isp * 9.82 * e.mixtureDensity); // Tavert Issue #163
                foreach (var propellant in e.propellants)
                {
                    double maxreq = udot * propellant.ratio;
                    addResource(propellant.id, propellant.currentRequirement, maxreq);
                }

                if (!e.getFlameoutState)
                {
                    double usableFraction = 1; // Vector3d.Dot((p.transform.rotation * e.thrustTransform.forward).normalized, forward); // TODO: Fix usableFraction
                    thrustAvailable += e.maxThrust * usableFraction;

                    if (e.throttleLocked) thrustMinimum += e.maxThrust * usableFraction;
                    else thrustMinimum += e.minThrust * usableFraction;

                    Part p = e.part;
                    ModuleGimbal gimbal = p.Modules.OfType<ModuleGimbal>().FirstOrDefault();
                    if (gimbal != null && !gimbal.gimbalLock)
                    {
                        double gimbalRange = gimbal.gimbalRange;
                        torqueThrustPYAvailable += Math.Sin(Math.Abs(gimbalRange) * Math.PI / 180) * e.maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude; // TODO: close enough?
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
                    idx++;
                }
            }
        }
    }
}