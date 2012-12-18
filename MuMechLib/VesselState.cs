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
        public double time;            //planetarium time
        public double deltaT;          //TimeWarp.fixedDeltaTime

        public Vector3d CoM;
        public Vector3d MoI;
        public Vector3d up;
        public Vector3d north;
        public Vector3d east;
        public Vector3d forward;      //the direction the vessel is pointing

        public Quaternion rotationSurface;
        public Quaternion rotationVesselSurface;

        public Vector3d velocityMainBodySurface;
        public Vector3d velocityVesselSurface;
        public Vector3d velocityVesselSurfaceUnit;
        public Vector3d velocityVesselOrbit;
        public Vector3d velocityVesselOrbitUnit;

        public Vector3d vectorToTarget;
        public Vector3d relativeVelocityToTarget;
        public double distanceToTarget;

        public Vector3d angularVelocity;
        public Vector3d angularMomentum;

        public Vector3d upNormalToVelSurface; //unit vector in the plane of up and velocityVesselSurface and perpendicular to velocityVesselSurface
        public Vector3d upNormalToVelOrbit;   //unit vector in the plane of up and velocityVesselOrbit and perpendicular to velocityVesselOrbit 
        public Vector3d leftSurface;  //unit vector perpendicular to up and velocityVesselSurface
        public Vector3d leftOrbit;    //unit vector perpendicular to up and velocityVesselOrbit

        public Vector3d gravityForce;
        public double localg;             //magnitude of gravityForce

        //How about changing these so we store the instantaneous values and *also*
        //the smoothed MovingAverages? Sometimes we need the instantaneous value.
        public MovingAverage speedOrbital = new MovingAverage();
        public MovingAverage speedSurface = new MovingAverage();
        public MovingAverage speedVertical = new MovingAverage();
        public MovingAverage speedHorizontal = new MovingAverage();
        public MovingAverage vesselHeading = new MovingAverage();
        public MovingAverage vesselPitch = new MovingAverage();
        public MovingAverage vesselRoll = new MovingAverage();
        public MovingAverage altitudeASL = new MovingAverage();
        public MovingAverage altitudeTrue = new MovingAverage();
        public double altitudeBottom = 0;
        public MovingAverage orbitApA = new MovingAverage();
        public MovingAverage orbitPeA = new MovingAverage();
        public MovingAverage orbitPeriod = new MovingAverage();
        public MovingAverage orbitTimeToAp = new MovingAverage();
        public MovingAverage orbitTimeToPe = new MovingAverage();
        public MovingAverage orbitLAN = new MovingAverage();
        public MovingAverage orbitArgumentOfPeriapsis = new MovingAverage();
        public MovingAverage orbitInclination = new MovingAverage();
        public MovingAverage orbitEccentricity = new MovingAverage();
        public MovingAverage orbitSemiMajorAxis = new MovingAverage();
        public MovingAverage latitude = new MovingAverage();
        public MovingAverage longitude = new MovingAverage();

        public double radius;  //distance from planet center

        public double mass;
        public double thrustAvailable;
        public double thrustMinimum;
        public double maxThrustAccel;      //thrustAvailable / mass
        public double minThrustAccel;      //some engines (particularly SRBs) have a minimum thrust so this may be nonzero
        public double torqueRAvailable;
        public double torquePYAvailable;
        public double torqueThrustPYAvailable;
        public double massDrag;
        public double atmosphericDensity;
        public double angleToPrograde;

        CelestialBody mainBody;

        public void Update(Vessel vessel)
        {
            time = Planetarium.GetUniversalTime();
            deltaT = TimeWarp.fixedDeltaTime;

            CoM = vessel.findWorldCenterOfMass();
            up = (CoM - vessel.mainBody.position).normalized;

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

            if (FlightGlobals.fetch.VesselTarget != null)
            {
                vectorToTarget = FlightGlobals.fetch.VesselTarget.GetTransform().position - vessel.transform.position;
                relativeVelocityToTarget = velocityVesselOrbit - FlightGlobals.fetch.VesselTarget.GetObtVelocity();
                distanceToTarget = vectorToTarget.magnitude;
            }
            else
            {
                vectorToTarget = Vector3d.zero;
                relativeVelocityToTarget = Vector3d.zero;
                distanceToTarget = 0;
            }

            angularVelocity = Quaternion.Inverse(vessel.GetTransform().rotation) * vessel.rigidbody.angularVelocity;

            upNormalToVelSurface = Vector3d.Exclude(velocityVesselSurfaceUnit, up).normalized;
            upNormalToVelOrbit = Vector3d.Exclude(velocityVesselOrbit, up).normalized;
            leftSurface = -Vector3d.Cross(upNormalToVelSurface, velocityVesselSurfaceUnit);
            leftOrbit = -Vector3d.Cross(upNormalToVelOrbit, velocityVesselOrbitUnit); ;

            gravityForce = FlightGlobals.getGeeForceAtPosition(CoM);
            localg = gravityForce.magnitude;

            speedOrbital.value = velocityVesselOrbit.magnitude;
            speedSurface.value = velocityVesselSurface.magnitude;
            speedVertical.value = Vector3d.Dot(velocityVesselSurface, up);
            speedHorizontal.value = (velocityVesselSurface - (speedVertical * up)).magnitude;

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

            atmosphericDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(altitudeASL, vessel.mainBody));

            orbitApA.value = vessel.orbit.ApA;
            orbitPeA.value = vessel.orbit.PeA;
            orbitPeriod.value = vessel.orbit.period;
            orbitTimeToAp.value = vessel.orbit.timeToAp;
            if (vessel.orbit.eccentricity < 1) orbitTimeToPe.value = vessel.orbit.timeToPe;
            else orbitTimeToPe.value = -vessel.orbit.meanAnomaly / (2 * Math.PI / vessel.orbit.period); //orbit.timeToPe is bugged for ecc > 1 and timewarp > 2x
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

            radius = (CoM - vessel.mainBody.position).magnitude;

            mass = thrustAvailable = thrustMinimum = massDrag = torqueRAvailable = torquePYAvailable = torqueThrustPYAvailable = 0;
            MoI = vessel.findLocalMOI(CoM);
            foreach (Part p in vessel.parts)
            {
                if (p.physicalSignificance != Part.PhysicalSignificance.NONE)
                {
                    double partMass = p.TotalMass();
                    mass += partMass;
                    massDrag += partMass * p.maximum_drag;
                }

                MoI += p.Rigidbody.inertiaTensor;
                if (((p.State == PartStates.ACTIVE) || ((Staging.CurrentStage > Staging.lastStage) && (p.inverseStage == Staging.lastStage))) && ((p is LiquidEngine) || (p is LiquidFuelEngine) || (p is SolidRocket) || (p is AtmosphericEngine) || p.Modules.Contains("ModuleEngines")))
                {
                    if (p is LiquidEngine && p.EngineHasFuel())
                    {
                        double usableFraction = Vector3d.Dot((p.transform.rotation * ((LiquidEngine)p).thrustVector).normalized, forward);
                        thrustAvailable += ((LiquidEngine)p).maxThrust * usableFraction;
                        thrustMinimum += ((LiquidEngine)p).minThrust * usableFraction;
                        if (((LiquidEngine)p).thrustVectoringCapable)
                        {
                            torqueThrustPYAvailable += Math.Sin(Math.Abs(((LiquidEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                        }
                    }
                    else if (p is LiquidFuelEngine && p.EngineHasFuel())
                    {
                        double usableFraction = Vector3d.Dot((p.transform.rotation * ((LiquidFuelEngine)p).thrustVector).normalized, forward);
                        thrustAvailable += ((LiquidFuelEngine)p).maxThrust * usableFraction;
                        thrustMinimum += ((LiquidFuelEngine)p).minThrust * usableFraction;
                        if (((LiquidFuelEngine)p).thrustVectoringCapable)
                        {
                            torqueThrustPYAvailable += Math.Sin(Math.Abs(((LiquidFuelEngine)p).gimbalRange) * Math.PI / 180) * ((LiquidFuelEngine)p).maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                        }
                    }
                    else if (p is SolidRocket && !p.ActivatesEvenIfDisconnected)
                    {
                        double usableFraction = Vector3d.Dot((p.transform.rotation * ((SolidRocket)p).thrustVector).normalized, forward);
                        thrustAvailable += ((SolidRocket)p).thrust * usableFraction;
                        thrustMinimum += ((SolidRocket)p).thrust * usableFraction;
                    }
                    else if (p is AtmosphericEngine && p.EngineHasFuel())
                    {
                        double usableFraction = Vector3d.Dot((p.transform.rotation * ((AtmosphericEngine)p).thrustVector).normalized, forward);
                        thrustAvailable += ((AtmosphericEngine)p).maximumEnginePower * ((AtmosphericEngine)p).totalEfficiency * usableFraction;
                        if (((AtmosphericEngine)p).thrustVectoringCapable)
                        {
                            torqueThrustPYAvailable += Math.Sin(Math.Abs(((AtmosphericEngine)p).gimbalRange) * Math.PI / 180) * ((AtmosphericEngine)p).maximumEnginePower * ((AtmosphericEngine)p).totalEfficiency * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                        }
                    }
                    else if (p.Modules.Contains("ModuleEngines"))
                    {
                        foreach (PartModule pm in p.Modules)
                        {
                            if ((pm is ModuleEngines) && (pm.isEnabled) && p.EngineHasFuel())
                            {
                                ModuleEngines e = (ModuleEngines)pm;
                                double usableFraction = 1; // Vector3d.Dot((p.transform.rotation * e.thrustTransform.forward).normalized, forward); // TODO: Fix usableFraction
                                thrustAvailable += e.maxThrust * usableFraction;

                                if (e.throttleLocked) thrustMinimum += e.maxThrust * usableFraction;
                                else thrustMinimum += e.minThrust * usableFraction;

                                if (p.Modules.OfType<ModuleGimbal>().Count() > 0)
                                {
                                    torqueThrustPYAvailable += Math.Sin(Math.Abs(p.Modules.OfType<ModuleGimbal>().First().gimbalRange) * Math.PI / 180) * e.maxThrust * (p.Rigidbody.worldCenterOfMass - CoM).magnitude; // TODO: close enough?
                                }
                            }
                        }
                    }
                }
                if ((!FlightInputHandler.RCSLock) && (p is RCSModule))
                {
                    double maxT = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        if (((RCSModule)p).thrustVectors[i] != Vector3.zero)
                        {
                            maxT = Math.Max(maxT, ((RCSModule)p).thrusterPowers[i]);
                        }
                    }
                    // torqueRAvailable += maxT;
                    torquePYAvailable += maxT * (p.Rigidbody.worldCenterOfMass - CoM).magnitude;
                }
                if (p is CommandPod)
                {
                    torqueRAvailable += Math.Abs(((CommandPod)p).rotPower);
                    torquePYAvailable += Math.Abs(((CommandPod)p).rotPower);
                }
            }

            angularMomentum = new Vector3d(angularVelocity.x * MoI.x, angularVelocity.y * MoI.y, angularVelocity.z * MoI.z);

            maxThrustAccel = thrustAvailable / mass;
            minThrustAccel = thrustMinimum / mass;

            mainBody = vessel.mainBody;
        }

        public double TerminalVelocity()
        {
            if (altitudeASL > mainBody.maxAtmosphereAltitude) return Double.MaxValue;

            double airDensity = FlightGlobals.getAtmDensity(FlightGlobals.getStaticPressure(CoM, mainBody));
            return Math.Sqrt(2 * localg * mass / (massDrag * FlightGlobals.DragMultiplier * airDensity));
        }

        public double ThrustAccel(double throttle)
        {
            return (1.0 - throttle) * minThrustAccel + throttle * maxThrustAccel;
        }

    }
}
