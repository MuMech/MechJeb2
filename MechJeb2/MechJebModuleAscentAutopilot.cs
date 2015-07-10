using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //Todo: -reimplement measurement of LPA
    //      -Figure out exactly how throttle-limiting should work and interact
    //       with the global throttle-limit option
    public class MechJebModuleAscentAutopilot : ComputerModule
    {
        public MechJebModuleAscentAutopilot(MechJebCore core) : base(core) { }

        public string status = "";

        //input parameters:
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public IAscentPath ascentPath = new DefaultAscentPath();
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult desiredOrbitAltitude = new EditableDoubleMult(100000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public double desiredInclination = 0.0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool autoThrottle = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool correctiveSteering = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool forceRoll = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble verticalRoll = new EditableDouble(0);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble turnRoll = new EditableDouble(0);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool autodeploySolarPanels = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool _autostage = true;
        public bool autostage
        {
            get { return _autostage; }
            set
            {
                bool changed = (value != _autostage);
                _autostage = value;
                if (changed)
                {
                    if (_autostage && this.enabled) core.staging.users.Add(this);
                    if (!_autostage) core.staging.users.Remove(this);
                }
            }
        }


        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool limitAoA = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble maxAoA = 5;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult aoALimitFadeoutPressure = new EditableDoubleMult(2500);
        public bool limitingAoA = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble launchPhaseAngle = 0;

        [Persistent(pass = (int)(Pass.Global))]
        public EditableInt warpCountDown = 11;

        public bool timedLaunch = false;
        public double launchTime = 0;

        public double currentMaxAoA = 0;

        public double tMinus
        {
            get { return launchTime - vesselState.time; }
        }

        //internal state:
        enum AscentMode { RETRACT_SOLAR_PANELS, VERTICAL_ASCENT, GRAVITY_TURN, COAST_TO_APOAPSIS, CIRCULARIZE };
        AscentMode mode;
        bool placedCircularizeNode = false;
        private double lastTMinus = 999;


        public override void OnModuleEnabled()
        {
            if (autodeploySolarPanels && mainBody.atmosphere)
                mode = AscentMode.RETRACT_SOLAR_PANELS;
            else
                mode = AscentMode.VERTICAL_ASCENT;

            placedCircularizeNode = false;

            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
            if (autostage) core.staging.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            core.staging.users.Remove(this);

            if (placedCircularizeNode) core.node.Abort();

            status = "Off";
        }

        public void StartCountdown(double time)
        {
            timedLaunch = true;
            launchTime = time;
            lastTMinus = 999;
        }

        public override void OnFixedUpdate()
        {
            if (timedLaunch)
            {
                if (tMinus < 3 * vesselState.deltaT || (tMinus > 10.0 && lastTMinus < 1.0))
                {
                    if (enabled && vesselState.thrustAvailable < 10E-4) // only stage if we have no engines active
                        Staging.ActivateNextStage();
                    timedLaunch = false;
                }
                else
                {
                    if (core.node.autowarp)
                        core.warp.WarpToUT(launchTime - warpCountDown);
                }
                lastTMinus = tMinus;
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            limitingAoA = false;
            switch (mode)
            {
                case AscentMode.RETRACT_SOLAR_PANELS:
                    DriveRetractSolarPanels(s);
                    break;

                case AscentMode.VERTICAL_ASCENT:
                    DriveVerticalAscent(s);
                    break;

                case AscentMode.GRAVITY_TURN:
                    DriveGravityTurn(s);
                    break;

                case AscentMode.COAST_TO_APOAPSIS:
                    DriveCoastToApoapsis(s);
                    break;

                case AscentMode.CIRCULARIZE:
                    DriveCircularizationBurn(s);
                    break;
            }
        }

        void DriveRetractSolarPanels(FlightCtrlState s)
        {
            if (autoThrottle) core.thrust.targetThrottle = 0.0f;

            if (timedLaunch && tMinus > 10.0)
            {
                status = "Awaiting liftoff";
                return;
            }

            status = "Retracting solar panels";
            core.solarpanel.RetractAll();
            if (core.solarpanel.AllRetracted())
                mode = AscentMode.VERTICAL_ASCENT;
        }

        void DriveVerticalAscent(FlightCtrlState s)
        {
            if (timedLaunch)
            {
                status = "Awaiting liftoff";
                return;
            }

            if (!ascentPath.IsVerticalAscent(vesselState.altitudeASL, vesselState.speedSurface)) mode = AscentMode.GRAVITY_TURN;
            if (autoThrottle && orbit.ApA > desiredOrbitAltitude) mode = AscentMode.COAST_TO_APOAPSIS;

            //during the vertical ascent we just thrust straight up at max throttle
            if (forceRoll && vesselState.altitudeTrue > 50)
            { // pre-align roll unless correctiveSteering is active as it would just interfere with that
                core.attitude.attitudeTo(90 - desiredInclination, 90, verticalRoll, this);
            }
            else
            {
                core.attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, this);
            }
            if (autoThrottle) core.thrust.targetThrottle = 1.0F;

            if (!vessel.LiftedOff()) status = "Awaiting liftoff";
            else status = "Vertical ascent";
        }

        //data used by ThrottleToRaiseApoapsis
        float raiseApoapsisLastThrottle = 0;
        double raiseApoapsisLastApR = 0;
        double raiseApoapsisLastUT = 0;
        MovingAverage raiseApoapsisRatePerThrottle = new MovingAverage(3, 0);

        //gives a throttle setting that reduces as we approach the desired apoapsis
        //so that we can precisely match the desired apoapsis instead of overshooting it
        float ThrottleToRaiseApoapsis(double currentApR, double finalApR)
        {
            float desiredThrottle;

            if (currentApR > finalApR + 5.0)
            {
                desiredThrottle = 0.0F; //done, throttle down
            }
            else if (orbit.ApA < mainBody.RealMaxAtmosphereAltitude())
            {
                desiredThrottle = 1.0F; //throttle hard to escape atmosphere
            }
            else if (raiseApoapsisLastUT > vesselState.time - 1)
            {
                //reduce throttle as apoapsis nears target
                double instantRatePerThrottle = (orbit.ApR - raiseApoapsisLastApR) / ((vesselState.time - raiseApoapsisLastUT) * raiseApoapsisLastThrottle);
                instantRatePerThrottle = Math.Max(1.0, instantRatePerThrottle); //avoid problems from negative rates
                raiseApoapsisRatePerThrottle.value = instantRatePerThrottle;
                double desiredApRate = (finalApR - currentApR) / 1.0;
                desiredThrottle = Mathf.Clamp((float)(desiredApRate / raiseApoapsisRatePerThrottle), 0.05F, 1.0F);
            }
            else
            {
                desiredThrottle = 1.0F; //no recent data point; just use max thrust.
            }

            //record data for next frame
            raiseApoapsisLastThrottle = desiredThrottle;
            raiseApoapsisLastApR = orbit.ApR;
            raiseApoapsisLastUT = vesselState.time;

            return desiredThrottle;
        }

        void DriveGravityTurn(FlightCtrlState s)
        {
            //stop the gravity turn when our apoapsis reaches the desired altitude
            if (autoThrottle && orbit.ApA > desiredOrbitAltitude)
            {
                mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (ascentPath.IsVerticalAscent(vesselState.altitudeASL, vesselState.speedSurface))
            {
                mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            if (autoThrottle)
            {
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, desiredOrbitAltitude + mainBody.Radius);
                if (core.thrust.targetThrottle < 1.0F)
                {
                    //when we are bringing down the throttle to make the apoapsis accurate, we're liable to point in weird
                    //directions because thrust goes down and so "difficulty" goes up. so just burn prograde
                    core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.ORBIT, this);
                    status = "Fine tuning apoapsis";
                    return;
                }
            }

            //transition gradually from the rotating to the non-rotating reference frame. this calculation ensures that
            //when our maximum possible apoapsis, given our orbital energy, is desiredOrbitalRadius, then we are
            //fully in the non-rotating reference frame and thus doing the correct calculations to get the right inclination
            double GM = mainBody.gravParameter;
            double potentialDifferenceWithApoapsis = GM / vesselState.radius - GM / (mainBody.Radius + desiredOrbitAltitude);
            double verticalSpeedForDesiredApoapsis = Math.Sqrt(2 * potentialDifferenceWithApoapsis);
            double referenceFrameBlend = Mathf.Clamp((float)(vesselState.speedOrbital / verticalSpeedForDesiredApoapsis), 0.0F, 1.0F);

            Vector3d actualVelocityUnit = ((1 - referenceFrameBlend) * vesselState.surfaceVelocity.normalized
                                               + referenceFrameBlend * vesselState.orbitalVelocity.normalized).normalized;

            double desiredHeading = Math.PI / 180 * OrbitalManeuverCalculator.HeadingForInclination(desiredInclination, vesselState.latitude);
            Vector3d desiredHeadingVector = Math.Sin(desiredHeading) * vesselState.east + Math.Cos(desiredHeading) * vesselState.north;
            double desiredFlightPathAngle = ascentPath.FlightPathAngle(vesselState.altitudeASL, vesselState.speedSurface);

            Vector3d desiredVelocityUnit = Math.Cos(desiredFlightPathAngle * Math.PI / 180) * desiredHeadingVector
                                         + Math.Sin(desiredFlightPathAngle * Math.PI / 180) * vesselState.up;

            Vector3d desiredThrustVector = desiredVelocityUnit;

            if (correctiveSteering)
            {
                Vector3d velocityError = (desiredVelocityUnit - actualVelocityUnit);

                const double Kp = 5.0; //control gain

                //"difficulty" scales the controller gain to account for the difficulty of changing a large velocity vector given our current thrust
                double difficulty = vesselState.surfaceVelocity.magnitude / (50 + 10 * vesselState.ThrustAccel(core.thrust.targetThrottle));
                if (difficulty > 5) difficulty = 5;

                if (vesselState.limitedMaxThrustAccel == 0) difficulty = 1.0; //so we don't freak out over having no thrust between stages

                Vector3d steerOffset = Kp * difficulty * velocityError;

                //limit the amount of steering to 10 degrees. Furthermore, never steer to a FPA of > 90 (that is, never lean backward)
                double maxOffset = 10 * Math.PI / 180;
                if (desiredFlightPathAngle > 80) maxOffset = (90 - desiredFlightPathAngle) * Math.PI / 180;
                if (steerOffset.magnitude > maxOffset) steerOffset = maxOffset * steerOffset.normalized;

                desiredThrustVector += steerOffset;
            }

            desiredThrustVector = desiredThrustVector.normalized;

            if (limitAoA)
            {
                float fade = vesselState.dynamicPressure < aoALimitFadeoutPressure ? (float)(aoALimitFadeoutPressure / vesselState.dynamicPressure) : 1;
                currentMaxAoA = Math.Min(fade * maxAoA, 180d);
                limitingAoA = vessel.altitude < mainBody.atmosphereDepth && Vector3.Angle(vesselState.surfaceVelocity, desiredThrustVector) > currentMaxAoA;

                if (limitingAoA)
                {
                    desiredThrustVector = Vector3.RotateTowards(vesselState.surfaceVelocity, desiredThrustVector, (float)(currentMaxAoA * Mathf.Deg2Rad), 1).normalized;
                }
            }

            if (forceRoll && Vector3.Angle(vesselState.up, vesselState.forward) > 7 && core.attitude.attitudeError < 5)
            {
                var pitch = 90 - Vector3.Angle(vesselState.up, desiredThrustVector);
                var hdg = core.rover.HeadingToPos(vessel.CoM, vessel.CoM + desiredThrustVector);
                core.attitude.attitudeTo(hdg, pitch, turnRoll, this);
            }
            else
            {
                core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, this);
            }

            status = "Gravity turn";
        }

        void DriveCoastToApoapsis(FlightCtrlState s)
        {
            core.thrust.targetThrottle = 0;

            double circularSpeed = OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, orbit.ApR);
            double apoapsisSpeed = orbit.SwappedOrbitalVelocityAtUT(orbit.NextApoapsisTime(vesselState.time)).magnitude;
            double circularizeBurnTime = (circularSpeed - apoapsisSpeed) / vesselState.limitedMaxThrustAccel;

            //Once we get above the atmosphere, plan and execute the circularization maneuver.
            //For orbits near the edge of the atmosphere, we can't wait until we break the atmosphere
            //to start the burn, so we also compare the timeToAp with the expected circularization burn time.
            //if ((vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
            //    || (vesselState.limitedMaxThrustAccel > 0 && orbit.timeToAp < circularizeBurnTime / 1.8))

            // Sarbian : removed the special case for now. Some ship where turning while still in atmosphere

            if (vesselState.altitudeASL > mainBody.RealMaxAtmosphereAltitude())
            {
                if (autodeploySolarPanels)
                    core.solarpanel.ExtendAll();

                mode = AscentMode.CIRCULARIZE;
                core.warp.MinimumWarp();
                return;
            }

            //if our apoapsis has fallen too far, resume the gravity turn
            if (orbit.ApA < desiredOrbitAltitude - 1000.0)
            {
                mode = AscentMode.GRAVITY_TURN;
                core.warp.MinimumWarp();
                return;
            }

            //point prograde and thrust gently if our apoapsis falls below the target
            //core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.ORBIT, this);

            // Actually I have a better idea: Don't initiate orientation changes when there's a chance that our main engine
            // might reignite. There won't be enough control authority to counteract that much momentum change.
            // - Starwaster
            core.thrust.targetThrottle = 0;

            double desiredHeading = Math.PI / 180 * OrbitalManeuverCalculator.HeadingForInclination(desiredInclination, vesselState.latitude);
            Vector3d desiredHeadingVector = Math.Sin(desiredHeading) * vesselState.east + Math.Cos(desiredHeading) * vesselState.north;
            double desiredFlightPathAngle = ascentPath.FlightPathAngle(vesselState.altitudeASL, vesselState.speedSurface);

            Vector3d desiredThrustVector = Math.Cos(desiredFlightPathAngle * Math.PI / 180) * desiredHeadingVector
                + Math.Sin(desiredFlightPathAngle * Math.PI / 180) * vesselState.up;


            core.attitude.attitudeTo(desiredThrustVector.normalized, AttitudeReference.INERTIAL, this);
            if (autoThrottle && orbit.ApA < desiredOrbitAltitude)
            {
                core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.INERTIAL, this);
                core.thrust.targetThrottle = ThrottleToRaiseApoapsis(orbit.ApR, desiredOrbitAltitude + mainBody.Radius);
            }

            if (core.node.autowarp)
            {
                //warp at x2 physical warp:
                core.warp.WarpPhysicsAtRate(2);
            }

            status = "Coasting to edge of atmosphere";
        }

        void DriveCircularizationBurn(FlightCtrlState s)
        {
            if (!vessel.patchedConicsUnlocked())
            {
                this.users.Clear();
                return;
            }

            if (placedCircularizeNode)
            {
                if (!vessel.patchedConicSolver.maneuverNodes.Any())
                {
                    MechJebModuleFlightRecorder recorder = core.GetComputerModule<MechJebModuleFlightRecorder>();
                    if (recorder != null) launchPhaseAngle = recorder.phaseAngleFromMark;

                    //finished circularize
                    this.users.Clear();
                    return;
                }
            }
            else
            {
                //place circularization node
                vessel.RemoveAllManeuverNodes();
                double UT = orbit.NextApoapsisTime(vesselState.time);
                //Vector3d dV = OrbitalManeuverCalculator.DeltaVToCircularize(orbit, UT);
                Vector3d dV = OrbitalManeuverCalculator.DeltaVForSemiMajorAxis(orbit, UT, desiredOrbitAltitude + mainBody.Radius);
                vessel.PlaceManeuverNode(orbit, dV, UT);
                placedCircularizeNode = true;

                core.node.ExecuteOneNode(this);
            }

            if (core.node.burnTriggered) status = "Circularizing";
            else status = "Coasting to circularization burn";
        }
    }

    //An IAscentPath describes the desired gravity turn path
    public interface IAscentPath
    {
        //altitude at which to stop going straight up
        bool IsVerticalAscent(double altitude, double velocity);

        //controls the ascent path
        double FlightPathAngle(double altitude, double velocity);
    }

    public class DefaultAscentPath : IAscentPath
    {
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult turnStartAltitude = new EditableDoubleMult(500, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult turnStartVelocity = new EditableDoubleMult(100);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult turnEndAltitude = new EditableDoubleMult(60000, 1000);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble turnEndAngle = 0;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult turnShapeExponent = new EditableDoubleMult(0.4, 0.01);
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool autoPath = true;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public float autoTurnPerc = 0.05f;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public float autoTurnSpdFactor = 18.5f;

        private double actualTurnStart = 0;

        public double autoTurnStartAltitude
        {
            get
            {
                var vessel = FlightGlobals.ActiveVessel;
                return (vessel.mainBody.atmosphere ? vessel.mainBody.RealMaxAtmosphereAltitude() * autoTurnPerc : vessel.terrainAltitude + 25);
            }
        }

        public double autoTurnStartVelocity
        {
            get
            {
                var vessel = FlightGlobals.ActiveVessel;
                return vessel.mainBody.atmosphere ? autoTurnSpdFactor * autoTurnSpdFactor * autoTurnSpdFactor * 0.015625f : double.PositiveInfinity;
            }
        }

        public double autoTurnEndAltitude
        {
            get
            {
                var vessel = FlightGlobals.ActiveVessel;
                var targetAlt = vessel.GetMasterMechJeb().GetComputerModule<MechJebModuleAscentAutopilot>().desiredOrbitAltitude;
                if (vessel.mainBody.atmosphere)
                {
                    return Math.Min(vessel.mainBody.RealMaxAtmosphereAltitude() * 0.85, targetAlt);
                }
                else
                {
                    return Math.Min(30000, targetAlt * 0.85);
                }
            }
        }

        public double VerticalAscentEnd()
        {
            return autoPath ? autoTurnStartAltitude : turnStartAltitude;
        }

        public double SpeedAscentEnd()
        {
            return autoPath ? autoTurnStartVelocity : turnStartVelocity;
        }

        public bool IsVerticalAscent(double altitude, double velocity)
        {
            actualTurnStart = Math.Min(actualTurnStart, autoTurnStartAltitude);
            if (altitude < VerticalAscentEnd() && velocity < SpeedAscentEnd())
            {
                actualTurnStart = Math.Max(actualTurnStart, altitude);
                return true;
            }
            return false;
        }

        public double FlightPathAngle(double altitude, double velocity)
        {
            var turnEnd = (autoPath ? autoTurnEndAltitude : turnEndAltitude);

            if (IsVerticalAscent(altitude, velocity)) return 90.0;

            if (altitude > turnEnd) return turnEndAngle;

            return Mathf.Clamp((float)(90.0 - Math.Pow((altitude - actualTurnStart) / (turnEnd - actualTurnStart), turnShapeExponent) * (90.0 - turnEndAngle)), 0.01F, 89.99F);
        }
    }

    public static class LaunchTiming
    {
        //Computes the time until the phase angle between the launchpad and the target equals the given angle.
        //The convention used is that phase angle is the angle measured starting at the target and going east until
        //you get to the launchpad.
        //The time returned will not be exactly accurate unless the target is in an exactly circular orbit. However,
        //the time returned will go to exactly zero when the desired phase angle is reached.
        public static double TimeToPhaseAngle(double phaseAngle, CelestialBody launchBody, double launchLongitude, Orbit target)
        {
            double launchpadAngularRate = 360 / launchBody.rotationPeriod;
            double targetAngularRate = 360.0 / target.period;
            if (Vector3d.Dot(-target.GetOrbitNormal().Reorder(132).normalized, launchBody.angularVelocity) < 0) targetAngularRate *= -1; //retrograde target

            Vector3d currentLaunchpadDirection = launchBody.GetSurfaceNVector(0, launchLongitude);
            Vector3d currentTargetDirection = target.SwappedRelativePositionAtUT(Planetarium.GetUniversalTime());
            currentTargetDirection = Vector3d.Exclude(launchBody.angularVelocity, currentTargetDirection);

            double currentPhaseAngle = Math.Abs(Vector3d.Angle(currentLaunchpadDirection, currentTargetDirection));
            if (Vector3d.Dot(Vector3d.Cross(currentTargetDirection, currentLaunchpadDirection), launchBody.angularVelocity) < 0)
            {
                currentPhaseAngle = 360 - currentPhaseAngle;
            }

            double phaseAngleRate = launchpadAngularRate - targetAngularRate;

            double phaseAngleDifference = MuUtils.ClampDegrees360(phaseAngle - currentPhaseAngle);

            if (phaseAngleRate < 0)
            {
                phaseAngleRate *= -1;
                phaseAngleDifference = 360 - phaseAngleDifference;
            }


            return phaseAngleDifference / phaseAngleRate;
        }

        //Computes the time required for the given launch location to rotate under the target orbital plane.
        //If the latitude is too high for the launch location to ever actually rotate under the target plane,
        //returns the time of closest approach to the target plane.
        //I have a wonderful proof of this formula which this comment is too short to contain.
        public static double TimeToPlane(CelestialBody launchBody, double launchLatitude, double launchLongitude, Orbit target)
        {
            double inc = Math.Abs(Vector3d.Angle(-target.GetOrbitNormal().Reorder(132).normalized, launchBody.angularVelocity));
            Vector3d b = Vector3d.Exclude(launchBody.angularVelocity, -target.GetOrbitNormal().Reorder(132).normalized).normalized; // I don't understand the sign here, but this seems to work
            b *= launchBody.Radius * Math.Sin(Math.PI / 180 * launchLatitude) / Math.Tan(Math.PI / 180 * inc);
            Vector3d c = Vector3d.Cross(-target.GetOrbitNormal().Reorder(132).normalized, launchBody.angularVelocity).normalized;
            double cMagnitudeSquared = Math.Pow(launchBody.Radius * Math.Cos(Math.PI / 180 * launchLatitude), 2) - b.sqrMagnitude;
            if (cMagnitudeSquared < 0) cMagnitudeSquared = 0;
            c *= Math.Sqrt(cMagnitudeSquared);
            Vector3d a1 = b + c;
            Vector3d a2 = b - c;

            Vector3d longitudeVector = launchBody.GetSurfaceNVector(0, launchLongitude);

            double angle1 = Math.Abs(Vector3d.Angle(longitudeVector, a1));
            if (Vector3d.Dot(Vector3d.Cross(longitudeVector, a1), launchBody.angularVelocity) < 0) angle1 = 360 - angle1;
            double angle2 = Math.Abs(Vector3d.Angle(longitudeVector, a2));
            if (Vector3d.Dot(Vector3d.Cross(longitudeVector, a2), launchBody.angularVelocity) < 0) angle2 = 360 - angle2;

            double angle = Math.Min(angle1, angle2);
            return (angle / 360) * launchBody.rotationPeriod;
        }
    }
}
