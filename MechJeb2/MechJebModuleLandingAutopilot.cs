using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleLandingAutopilot : ComputerModule
    {

        //public interface:
        public void LandAtPositionTarget(object controller)
        {
            users.Add(controller);

            predictor.users.Add(this);
            vessel.RemoveAllManeuverNodes(); //for the benefit of the landing predictions module

            deployedGears = false;
            deorbitBurnTriggered = false;
            lowDeorbitEndConditionSet = false;
            planeChangeTriggered = false;

            // Create a new parachute plan
            this.parachutePlan = new ParachutePlan(this);
            this.parachutePlan.StartPlanning();

            if (orbit.PeA < 0) landStep = LandStep.COURSE_CORRECTIONS; 
            else if (UseLowDeorbitStrategy()) landStep = LandStep.PLANE_CHANGE;
            else landStep = LandStep.DEORBIT_BURN;
        }

        public void LandUntargeted(object controller)
        {
            users.Add(controller);

            deployedGears = false;

            // Create a new parachute plan
            this.parachutePlan = new ParachutePlan(this);
            this.parachutePlan.StartPlanning();

            landStep = LandStep.UNTARGETED_DEORBIT;
        }

        public void StopLanding()
        {
            this.users.Clear();
        }

        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public EditableDouble touchdownSpeed = 0.5;

        public string status = "";

        //Internal state:
        enum LandStep
        {
            PLANE_CHANGE, LOW_DEORBIT_BURN, DEORBIT_BURN, COURSE_CORRECTIONS, COAST_TO_DECELERATION,
            DECELERATING, KILLING_HORIZONTAL_VELOCITY, FINAL_DESCENT, UNTARGETED_DEORBIT, OFF
        }
        LandStep landStep = LandStep.OFF;

        IDescentSpeedPolicy descentSpeedPolicy;

        bool planeChangeTriggered = false;
        double planeChangeDVLeft = 0;
        bool deorbitBurnTriggered = false;
        double lowDeorbitBurnMaxThrottle;
        bool lowDeorbitEndConditionSet = false;
        bool lowDeorbitEndOnLandingSiteNearer = false;
        bool deployedGears = false;

        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public bool deployGears = true;
        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public EditableInt limitGearsStage = 0;
        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public bool deployChutes = true;
        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public EditableInt limitChutesStage = 0;

        const double lowDeorbitBurnTriggerFactor = 2;

        ParachutePlan parachutePlan = null;  // This is used to adjest the height at which the parachutes semi deploy as a means of targeting the landing in an atmosphere where it is not possible to control atitude to perform corse correction burns.
        
        //Landing prediction data:
        MechJebModuleLandingPredictions predictor;
        ReentrySimulation.Result prediction;
        ReentrySimulation.Result errorPrediction;
        bool warpReady = false;
        bool PredictionReady //We shouldn't do any autopilot stuff until this is true
        {
            get
            {
                // Check that there is a prediction and that it is a landing prediction.
                if (prediction == null)
                {
                    return false;
                }
                else if (prediction.outcome != ReentrySimulation.Outcome.LANDED)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        bool ErrorPredictionReady // We shouldn't try to use an ErrorPrediction until this is true.
        {
            get
            {
                return (errorPrediction != null) &&
                       (errorPrediction.outcome == ReentrySimulation.Outcome.LANDED);
            }
        }
        double LandingAltitude //the altitude above sea level of the terrain at the landing site
        {
            get
            {
                if (PredictionReady)
                {
                    // Although we know the landingASL as it is in the prediciton, we suspect that it might sometimes be incorrect. So to check we will calculate it again here, and if the two differ log an error. It seems that this terrain ASL calls when made from the simulatiuon thread are regularly incorrect, but are OK when made from this thread. At the time of writting (KSP0.23) there seem to be several other things going wrong witht he terrain system, such as visual glitches as we as the occasional exceptions being thrown when calls to the CelestialBody object are made. I suspect a bug or some sort - for now this hack improves the landing results.
                    {
                        double checkASL = prediction.body.TerrainAltitude(prediction.endPosition.latitude, prediction.endPosition.longitude);
                        if (checkASL != prediction.endASL)
                        {
                            // I know that this check is not required as we might as well always make the asignment. However this allows for some debug monitoring of how often this is occuring.
                            prediction.endASL = checkASL;
                        }
                    }

                    return prediction.endASL;
                }
                else
                {
                    return 0;
                }
            }
        }
        Vector3d LandingSite //the current position of the landing site
        {
            get
            {
                return mainBody.GetWorldSurfacePosition(prediction.endPosition.latitude,
                                                        prediction.endPosition.longitude, LandingAltitude);
            }
        }
        Vector3d RotatedLandingSite //the position where the landing site will be when we land at it
        {
            get { return prediction.WorldEndPosition(); }
        }

        public string ParachuteControlInfo
        {
            get
            {
                if (this.ParachutesDeployable())
                {
                    string retVal = "'Chute Multiplier: " + this.parachutePlan.Multiplier.ToString("F7");
                    retVal += "\nMultiplier Quality: " + this.parachutePlan.MultiplierQuality.ToString("F1") +"%";
                    retVal += "\nUsing " + this.parachutePlan.MultiplierDataAmount + " predictions";

                    return ( retVal );
                }
                else
                {
                    return null;
                }
            }
        }

        public override void OnModuleEnabled()
        {
            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            predictor.users.Remove(this);
            predictor.descentSpeedPolicy = null;
            core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            landStep = LandStep.OFF;
            status = "Off";
        }

        public override void Drive(FlightCtrlState s)
        {
            if (landStep == LandStep.OFF) return;

            // If the latest prediction is a landing, aerobrake or no-reentry prediciton then keep it. However if it is any other sort or result it is not much use to us, so do not bother!
            {
                ReentrySimulation.Result result = predictor.GetResult();
                if (null != result)
                {
                    if (result.outcome != ReentrySimulation.Outcome.ERROR && result.outcome != ReentrySimulation.Outcome.TIMED_OUT)
                    {
                        this.prediction = result;
                    }
                }
            }
            {
                ReentrySimulation.Result result = predictor.GetErrorResult();
                if (null != result)
                {
                    if (result.outcome != ReentrySimulation.Outcome.ERROR && result.outcome != ReentrySimulation.Outcome.TIMED_OUT)
                    {
                        this.errorPrediction = result;
                    }
                }
            }

            descentSpeedPolicy = PickDescentSpeedPolicy();

            predictor.descentSpeedPolicy = PickDescentSpeedPolicy(); //create a separate IDescentSpeedPolicy object for the simulation
            predictor.decelEndAltitudeASL = DecelerationEndAltitude();
            predictor.parachuteSemiDeployMultiplier = this.parachutePlan.Multiplier;

            // Consider lowering the langing gear
            {
                double minalt = Math.Min(vesselState.altitudeBottom, Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue));
                if (deployGears && !deployedGears && (minalt < 1000)) DeployLandingGears();
            }

            if (!PredictionReady && landStep != LandStep.DEORBIT_BURN && landStep != LandStep.PLANE_CHANGE && landStep != LandStep.LOW_DEORBIT_BURN
                && landStep != LandStep.UNTARGETED_DEORBIT && landStep != LandStep.FINAL_DESCENT) return;

            switch (landStep)
            {
                case LandStep.UNTARGETED_DEORBIT:
                    DriveUntargetedDeorbit(s);
                    break;

                case LandStep.PLANE_CHANGE:
                    DrivePlaneChange(s);
                    break;

                case LandStep.LOW_DEORBIT_BURN:
                    DriveLowDeorbitBurn(s);
                    break;

                case LandStep.DEORBIT_BURN:
                    DriveDeorbitBurn(s);
                    break;

                case LandStep.COURSE_CORRECTIONS:
                    DriveCourseCorrections(s);
                    break;

                case LandStep.KILLING_HORIZONTAL_VELOCITY:
                    DriveKillHorizontalVelocity(s);
                    break;

                case LandStep.FINAL_DESCENT:
                    DriveUntargetedLanding(s);
                    break;

                default:
                    break;
            }

        }

        public override void OnFixedUpdate()
        {
            try
            {
                switch (landStep)
                {
                    case LandStep.PLANE_CHANGE:
                        FixedUpdatePlaneChange();
                        break;

                    case LandStep.LOW_DEORBIT_BURN:
                        FixedUpdateLowDeorbitBurn();
                        break;

                    case LandStep.DEORBIT_BURN:
                        FixedUpdateDeorbitBurn();
                        break;

                    case LandStep.COAST_TO_DECELERATION:
                        FixedUpdateCoastToDeceleration();
                        break;

                    case LandStep.DECELERATING:
                        FixedUpdateDecelerationBurn();
                        break;

                    default:
                        break;
                }
                DeployParachutes();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        void DriveUntargetedDeorbit(FlightCtrlState s)
        {
            if (orbit.PeA < -0.1 * mainBody.Radius)
            {
                core.thrust.targetThrottle = 0;
                landStep = LandStep.FINAL_DESCENT;
                return;
            }

            core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT_HORIZONTAL, this);
            if (core.attitude.attitudeAngleFromTarget() < 5) core.thrust.targetThrottle = 1;
            else core.thrust.targetThrottle = 0;

            status = "Doing deorbit burn.";
        }

        void FixedUpdatePlaneChange()
        {
            Vector3d targetRadialVector = mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0);
            Vector3d currentRadialVector = vesselState.CoM - mainBody.position;
            double angleToTarget = Vector3d.Angle(targetRadialVector, currentRadialVector);
            bool approaching = Vector3d.Dot(targetRadialVector - currentRadialVector, vessel.obt_velocity) > 0;

            if (!planeChangeTriggered && approaching && (angleToTarget > 80) && (angleToTarget < 90))
            {
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp(true);
                planeChangeTriggered = true;
            }

            if (planeChangeTriggered)
            {
                Vector3d horizontalToTarget = ComputePlaneChange();
                Vector3d finalVelocity = Quaternion.FromToRotation(vesselState.horizontalOrbit, horizontalToTarget) * vessel.obt_velocity;

                Vector3d deltaV = finalVelocity - vessel.obt_velocity;
                //burn normal+ or normal- to avoid dropping the Pe:
                Vector3d burnDir = Vector3d.Exclude(vesselState.up, Vector3d.Exclude(vessel.obt_velocity, deltaV));
                planeChangeDVLeft = Math.PI / 180 * Vector3d.Angle(finalVelocity, vessel.obt_velocity) * vesselState.speedOrbitHorizontal;
                core.attitude.attitudeTo(burnDir, AttitudeReference.INERTIAL, this);
                if (planeChangeDVLeft < 0.1F)
                {
                    landStep = LandStep.LOW_DEORBIT_BURN;
                }

                status = "Executing low orbit plane change of about " + planeChangeDVLeft.ToString("F0") + " m/s";
            }
            else
            {
                if (core.node.autowarp) core.warp.WarpRegularAtRate((float)(orbit.period / 6));
                status = "Moving to low orbit plane change burn point";
            }
        }

        //Could make this an iterative procedure for improved accuracy
        Vector3d ComputePlaneChange()
        {
            Vector3d targetRadialVector = mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0);
            Vector3d currentRadialVector = vesselState.CoM - mainBody.position;
            double angleToTarget = Vector3d.Angle(targetRadialVector, currentRadialVector);
            //this calculation seems like it might be be working right:
            double timeToTarget = orbit.TimeOfTrueAnomaly(orbit.trueAnomaly + angleToTarget, vesselState.time) - vesselState.time;
            double planetRotationAngle = 360 * timeToTarget / mainBody.rotationPeriod;
            Quaternion planetRotation = Quaternion.AngleAxis((float)planetRotationAngle, mainBody.angularVelocity);
            Vector3d targetRadialVectorOnFlyover = planetRotation * targetRadialVector;
            Vector3d horizontalToTarget = Vector3d.Exclude(vesselState.up, targetRadialVectorOnFlyover - currentRadialVector).normalized;
            return horizontalToTarget;
        }

        void DrivePlaneChange(FlightCtrlState s)
        {
            if (planeChangeTriggered && core.attitude.attitudeAngleFromTarget() < 2)
            {
                core.thrust.targetThrottle = Mathf.Clamp01((float)(planeChangeDVLeft / (2 * vesselState.maxThrustAccel)));
            }
            else
            {
                core.thrust.targetThrottle = 0;
            }
        }

        void FixedUpdateLowDeorbitBurn()
        {
            //Decide when we will start the deorbit burn:
            double stoppingDistance = Math.Pow(vesselState.speedSurfaceHorizontal, 2) / (2 * vesselState.limitedMaxThrustAccel);
            double triggerDistance = lowDeorbitBurnTriggerFactor * stoppingDistance;
            double heightAboveTarget = vesselState.altitudeASL - DecelerationEndAltitude();
            if (triggerDistance < heightAboveTarget)
            {
                triggerDistance = heightAboveTarget;
            }

            //See if it's time to start the deorbit burn:
            double rangeToTarget = Vector3d.Exclude(vesselState.up, core.target.GetPositionTargetPosition() - vesselState.CoM).magnitude;

            if (!deorbitBurnTriggered && rangeToTarget < triggerDistance)
            {
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp(true);
                deorbitBurnTriggered = true;
            }

            if (deorbitBurnTriggered) status = "Executing low deorbit burn";
            else status = "Moving to low deorbit burn point";

            //Warp toward deorbit burn if it hasn't been triggerd yet:
            if (!deorbitBurnTriggered && core.node.autowarp && rangeToTarget > 2 * triggerDistance) core.warp.WarpRegularAtRate((float)(orbit.period / 6));
            if (rangeToTarget < triggerDistance && !MuUtils.PhysicsRunning()) core.warp.MinimumWarp();

            //By default, thrust straight back at max throttle
            Vector3d thrustDirection = -vessel.srf_velocity.normalized;
            lowDeorbitBurnMaxThrottle = 1;

            //If we are burning, we watch the predicted landing site and switch to the braking
            //burn when the predicted landing site crosses the target. We also use the predictions
            //to steer the predicted landing site toward the target
            if (deorbitBurnTriggered && PredictionReady)
            {
                //angle slightly left or right to fix any cross-range error in the predicted landing site:
                Vector3d horizontalToLandingSite = Vector3d.Exclude(vesselState.up, LandingSite - vesselState.CoM).normalized;
                Vector3d horizontalToTarget = Vector3d.Exclude(vesselState.up, core.target.GetPositionTargetPosition() - vesselState.CoM).normalized;
                const double angleGain = 4;
                Vector3d angleCorrection = angleGain * (horizontalToTarget - horizontalToLandingSite);
                if (angleCorrection.magnitude > 0.1) angleCorrection *= 0.1 / angleCorrection.magnitude;
                thrustDirection = (thrustDirection + angleCorrection).normalized;

                double rangeToLandingSite = Vector3d.Exclude(vesselState.up, LandingSite - vesselState.CoM).magnitude;
                double maxAllowedSpeed = MaxAllowedSpeed();

                if (!lowDeorbitEndConditionSet && Vector3d.Distance(LandingSite, vesselState.CoM) < mainBody.Radius + vesselState.altitudeASL)
                {
                    lowDeorbitEndOnLandingSiteNearer = rangeToLandingSite > rangeToTarget;
                    lowDeorbitEndConditionSet = true;
                }

                lowDeorbitBurnMaxThrottle = 1;

                if (orbit.PeA < 0)
                {
                    if (rangeToLandingSite > rangeToTarget)
                    {
                        if (lowDeorbitEndConditionSet && !lowDeorbitEndOnLandingSiteNearer)
                        {
                            landStep = LandStep.DECELERATING;
                            core.thrust.targetThrottle = 0;
                        }

                        double maxAllowedSpeedAfterDt = MaxAllowedSpeedAfterDt(vesselState.deltaT);
                        double speedAfterDt = vesselState.speedSurface + vesselState.deltaT * Vector3d.Dot(vesselState.gravityForce, vessel.srf_velocity.normalized);
                        double throttleToMaintainLandingSite;
                        if (vesselState.speedSurface < maxAllowedSpeed) throttleToMaintainLandingSite = 0;
                        else throttleToMaintainLandingSite = (speedAfterDt - maxAllowedSpeedAfterDt) / (vesselState.deltaT * vesselState.maxThrustAccel);

                        lowDeorbitBurnMaxThrottle = throttleToMaintainLandingSite + 1 * (rangeToLandingSite / rangeToTarget - 1) + 0.2;
                    }
                    else
                    {
                        if (lowDeorbitEndConditionSet && lowDeorbitEndOnLandingSiteNearer)
                        {
                            landStep = LandStep.DECELERATING;
                            core.thrust.targetThrottle = 0;
                        }
                        else
                        {
                            lowDeorbitBurnMaxThrottle = 0;
                            status = "Deorbit burn complete: waiting for the right moment to start braking";
                        }
                    }
                }
            }

            core.attitude.attitudeTo(thrustDirection, AttitudeReference.INERTIAL, this);
        }

        void DriveLowDeorbitBurn(FlightCtrlState s)
        {
            if (deorbitBurnTriggered && core.attitude.attitudeAngleFromTarget() < 5)
            {
                core.thrust.targetThrottle = Mathf.Clamp01((float)lowDeorbitBurnMaxThrottle);
            }
            else
            {
                core.thrust.targetThrottle = 0;
            }
        }

        void FixedUpdateDeorbitBurn()
        {
            //if we don't want to deorbit but we're already on a reentry trajectory, we can't wait until the ideal point 
            //in the orbit to deorbt; we already have deorbited.
            if (part.vessel.orbit.ApA < part.vessel.mainBody.RealMaxAtmosphereAltitude())
            {
                landStep = LandStep.COURSE_CORRECTIONS;
                core.thrust.targetThrottle = 0;
                return;
            }

            //We aim for a trajectory that 
            // a) has the same vertical speed as our current trajectory
            // b) has a horizontal speed that will give it a periapsis of -10% of the body's radius
            // c) has a heading that points toward where the target will be at the end of free-fall, accounting for planetary rotation
            Vector3d horizontalDV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, vesselState.time, 0.9 * mainBody.Radius); //Imagine we are going to deorbit now. Find the burn that would lower our periapsis to -10% of the planet's radius
            Orbit forwardDeorbitTrajectory = orbit.PerturbedOrbit(vesselState.time, horizontalDV);                                     //Compute the orbit that would put us on
            double freefallTime = forwardDeorbitTrajectory.NextTimeOfRadius(vesselState.time, mainBody.Radius) - vesselState.time;     //Find how long that orbit would take to impact the ground
            double planetRotationDuringFreefall = 360 * freefallTime / mainBody.rotationPeriod;                                        //Find how many degrees the planet will rotate during that time
            Vector3d currentTargetRadialVector = mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0); //Find the current vector from the planet center to the target landing site
            Quaternion freefallPlanetRotation = Quaternion.AngleAxis((float)planetRotationDuringFreefall, mainBody.angularVelocity);   //Construct a quaternion representing the rotation of the planet found above
            Vector3d freefallEndTargetRadialVector = freefallPlanetRotation * currentTargetRadialVector;                               //Use this quaternion to find what the vector from the planet center to the target will be when we hit the ground
            Vector3d freefallEndTargetPosition = mainBody.position + freefallEndTargetRadialVector;                                    //Then find the actual position of the target at that time
            Vector3d freefallEndHorizontalToTarget = Vector3d.Exclude(vesselState.up, freefallEndTargetPosition - vesselState.CoM).normalized; //Find a horizontal unit vector that points toward where the target will be when we hit the ground
            Vector3d currentHorizontalVelocity = Vector3d.Exclude(vesselState.up, vessel.obt_velocity); //Find our current horizontal velocity
            double finalHorizontalSpeed = (currentHorizontalVelocity + horizontalDV).magnitude;                     //Find the desired horizontal speed after the deorbit burn
            Vector3d finalHorizontalVelocity = finalHorizontalSpeed * freefallEndHorizontalToTarget;                //Combine the desired speed and direction to get the desired velocity after the deorbi burn

            //Compute the angle between the location of the target at the end of freefall and the normal to our orbit:
            Vector3d currentRadialVector = vesselState.CoM - part.vessel.mainBody.position;
            double targetAngleToOrbitNormal = Vector3d.Angle(orbit.SwappedOrbitNormal(), freefallEndTargetRadialVector);
            targetAngleToOrbitNormal = Math.Min(targetAngleToOrbitNormal, 180 - targetAngleToOrbitNormal);

            double targetAheadAngle = Vector3d.Angle(currentRadialVector, freefallEndTargetRadialVector); //How far ahead the target is, in degrees
            double planeChangeAngle = Vector3d.Angle(currentHorizontalVelocity, freefallEndHorizontalToTarget); //The plane change required to get onto the deorbit trajectory, in degrees

            //If the target is basically almost normal to our orbit, it doesn't matter when we deorbit; might as well do it now
            //Otherwise, wait until the target is ahead
            if (targetAngleToOrbitNormal < 10
                || (targetAheadAngle < 90 && targetAheadAngle > 60 && planeChangeAngle < 90))
            {
                deorbitBurnTriggered = true;
            }

            if (deorbitBurnTriggered)
            {
            	if (!MuUtils.PhysicsRunning()) { core.warp.MinimumWarp(); } //get out of warp

                Vector3d deltaV = finalHorizontalVelocity - currentHorizontalVelocity;
                core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, this);

                if (deltaV.magnitude < 2.0) landStep = LandStep.COURSE_CORRECTIONS;

                status = "Doing high deorbit burn";
            }
            else
            {
                core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT, this);
                if (core.node.autowarp) core.warp.WarpRegularAtRate((float)(orbit.period / 10));

                status = "Moving to high deorbit burn point";
            }
        }

        void DriveDeorbitBurn(FlightCtrlState s)
        {
            if (deorbitBurnTriggered && core.attitude.attitudeAngleFromTarget() < 5) core.thrust.targetThrottle = 1.0F;
            else core.thrust.targetThrottle = 0;
        }

        //Estimate the delta-V of the correction burn that would be required to put us on
        //course for the target
        public Vector3d ComputeCourseCorrection(bool allowPrograde)
        {
            //actualLandingPosition is the predicted actual landing position
            Vector3d actualLandingPosition = RotatedLandingSite - mainBody.position;

            //orbitLandingPosition is the point where our current orbit intersects the planet
            double endRadius = mainBody.Radius + DecelerationEndAltitude() - 100;
            Vector3d orbitLandingPosition;
            if (orbit.PeR < endRadius)
                orbitLandingPosition = orbit.SwappedRelativePositionAtUT(orbit.NextTimeOfRadius(vesselState.time, endRadius));
            else
                orbitLandingPosition = orbit.SwappedRelativePositionAtUT(orbit.NextPeriapsisTime(vesselState.time));

            //convertOrbitToActual is a rotation that rotates orbitLandingPosition on actualLandingPosition
            Quaternion convertOrbitToActual = Quaternion.FromToRotation(orbitLandingPosition, actualLandingPosition);

            //Consider the effect small changes in the velocity in each of these three directions
            Vector3d[] perturbationDirections = { vessel.srf_velocity.normalized, vesselState.radialPlusSurface, vesselState.normalPlusSurface };

            //Compute the effect burns in these directions would
            //have on the landing position, where we approximate the landing position as the place
            //the perturbed orbit would intersect the planet.
            Vector3d[] deltas = new Vector3d[3];
            for (int i = 0; i < 3; i++)
            {
                const double perturbationDeltaV = 1; //warning: hard experience shows that setting this too low leads to bewildering bugs due to finite precision of Orbit functions
                Orbit perturbedOrbit = orbit.PerturbedOrbit(vesselState.time, perturbationDeltaV * perturbationDirections[i]); //compute the perturbed orbit
                double perturbedLandingTime;
                if (perturbedOrbit.PeR < endRadius) perturbedLandingTime = perturbedOrbit.NextTimeOfRadius(vesselState.time, endRadius);
                else perturbedLandingTime = perturbedOrbit.NextPeriapsisTime(vesselState.time);
                Vector3d perturbedLandingPosition = perturbedOrbit.SwappedRelativePositionAtUT(perturbedLandingTime); //find where it hits the planet
                Vector3d landingDelta = perturbedLandingPosition - orbitLandingPosition; //find the difference between that and the original orbit's intersection point
                landingDelta = convertOrbitToActual * landingDelta; //rotate that difference vector so that we can now think of it as starting at the actual landing position
                landingDelta = Vector3d.Exclude(actualLandingPosition, landingDelta); //project the difference vector onto the plane tangent to the actual landing position
                deltas[i] = landingDelta / perturbationDeltaV; //normalize by the delta-V considered, so that deltas now has units of meters per (meter/second) [i.e., seconds]
            }

            //Now deltas stores the predicted offsets in landing position produced by each of the three perturbations. 
            //We now figure out the offset we actually want

            //First we compute the target landing position. We have to convert the latitude and longitude of the target
            //into a position. We can't just get the current position of those coordinates, because the planet will
            //rotate during the descent, so we have to account for that.
            Vector3d desiredLandingPosition = mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0);
            float bodyRotationAngleDuringDescent = (float)(360 * (prediction.endUT - vesselState.time) / mainBody.rotationPeriod);
            Quaternion bodyRotationDuringFall = Quaternion.AngleAxis(bodyRotationAngleDuringDescent, mainBody.angularVelocity.normalized);
            desiredLandingPosition = bodyRotationDuringFall * desiredLandingPosition;

            Vector3d desiredDelta = desiredLandingPosition - actualLandingPosition;
            desiredDelta = Vector3d.Exclude(actualLandingPosition, desiredDelta);

            //Now desiredDelta gives the desired change in our actual landing position (projected onto a plane
            //tangent to the actual landing position).

            Vector3d downrangeDirection;
            Vector3d downrangeDelta;
            if (allowPrograde)
            {
                //Construct the linear combination of the prograde and radial+ perturbations 
                //that produces the largest effect on the landing position. The Math.Sign is to
                //detect and handle the case where radial+ burns actually bring the landing sign closer
                //(e.g. when we are traveling close to straight up)
                downrangeDirection = (deltas[0].magnitude * perturbationDirections[0]
                    + Math.Sign(Vector3d.Dot(deltas[0], deltas[1])) * deltas[1].magnitude * perturbationDirections[1]).normalized;

                downrangeDelta = Vector3d.Dot(downrangeDirection, perturbationDirections[0]) * deltas[0]
                                 + Vector3d.Dot(downrangeDirection, perturbationDirections[1]) * deltas[1];
            }
            else
            {
                //If we aren't allowed to burn prograde, downrange component of the landing
                //position has to be controlled by radial+/- burns:
                downrangeDirection = perturbationDirections[1];
                downrangeDelta = deltas[1];
            }

            //Now solve a 2x2 system of linear equations to determine the linear combination
            //of perturbationDirection01 and normal+ that will give the desired offset in the
            //predicted landing position.
            Matrix2x2 A = new Matrix2x2(
                downrangeDelta.sqrMagnitude, Vector3d.Dot(downrangeDelta, deltas[2]),
                Vector3d.Dot(downrangeDelta, deltas[2]), deltas[2].sqrMagnitude
            );

            Vector2d b = new Vector2d(Vector3d.Dot(desiredDelta, downrangeDelta), Vector3d.Dot(desiredDelta, deltas[2]));

            Vector2d coeffs = A.inverse() * b;

            Vector3d courseCorrection = coeffs.x * downrangeDirection + coeffs.y * perturbationDirections[2];

            return courseCorrection;
        }

        void DriveCourseCorrections(FlightCtrlState s)
        {
            // If the atomospheric drag is at least 100mm/s2 then start trying to target the overshoot using the parachutes
            if (mainBody.DragAccel(vesselState.CoM, vessel.obt_velocity, vesselState.massDrag / vesselState.mass).magnitude > 0.1)
            {
                if (ParachutesDeployable())
                {
                    ControlParachutes();
                }
            }

            double currentError = Vector3d.Distance(core.target.GetPositionTargetPosition(), LandingSite);

            if (currentError < 150)
            {
                core.thrust.targetThrottle = 0;
                landStep = LandStep.COAST_TO_DECELERATION;
                return;
            }

            // If a parachute has already been deployed then we will not be able to control attitude anyway, so move back to the coast to deceleration step.
            if (vesselState.parachuteDeployed)
            {
                core.thrust.targetThrottle = 0;
                landStep = LandStep.COAST_TO_DECELERATION;
                return;
            }

            Vector3d deltaV = ComputeCourseCorrection(true);

            status = "Performing course correction of about " + deltaV.magnitude.ToString("F1") + " m/s";

            core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, this);

            if (core.attitude.attitudeAngleFromTarget() < 5)
            {
                const double timeConstant = 2.0;
                core.thrust.ThrustForDV(deltaV.magnitude, timeConstant);
            }
            else
            {
                core.thrust.targetThrottle = 0;
            }
        }

        void FixedUpdateCoastToDeceleration()
        {
            core.thrust.targetThrottle = 0;

            // If the atmospheric drag is has started to act on the vessel then we are in a position to start considering when to deploy the parachutes.
            if (mainBody.DragAccel(vesselState.CoM, vessel.obt_velocity, vesselState.massDrag / vesselState.mass).magnitude > 0.10)
            {
                if(ParachutesDeployable())
                {
                    ControlParachutes();
                }
            }

            double maxAllowedSpeed = MaxAllowedSpeed();
            if (vesselState.speedSurface > 0.9 * maxAllowedSpeed)
            {
                core.warp.MinimumWarp();
                landStep = LandStep.DECELERATING;
                return;
            }

            double currentError = Vector3d.Distance(core.target.GetPositionTargetPosition(), LandingSite);
            if (currentError > 600)
            {
                if (!vesselState.parachuteDeployed) // However if there is already a parachute deployed, then do not bother trying to correct the course as we will not have any attitude control anyway.
                {
                    core.warp.MinimumWarp();
                    landStep = LandStep.COURSE_CORRECTIONS;
                    return;
                }
            }

            // Sometimes (on bodies with a thick atmosphere) there is no need for a decleration burn. Check for this so that it is possible to transition into the final decent step.
            if ((vesselState.altitudeASL < DecelerationEndAltitude() + 5) && UseAtmosphereToBrake())
            {
                landStep = LandStep.FINAL_DESCENT;
                core.warp.MinimumWarp();
                return;
            }
                        
            if (core.attitude.attitudeAngleFromTarget() < 1) { warpReady = true; } // less warp start warp stop jumping
            if (core.attitude.attitudeAngleFromTarget() > 5) { warpReady = false; } // hopefully

            if (PredictionReady)
            {
                double decelerationStartTime = (prediction.trajectory.Any() ? prediction.trajectory.First().UT : vesselState.time);
                Vector3d decelerationStartAttitude = -orbit.SwappedOrbitalVelocityAtUT(decelerationStartTime);
                decelerationStartAttitude += mainBody.getRFrmVel(orbit.SwappedAbsolutePositionAtUT(decelerationStartTime));
                decelerationStartAttitude = decelerationStartAttitude.normalized;
                core.attitude.attitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, this);
            }

            //Warp at a rate no higher than the rate that would have us impacting the ground 10 seconds from now:
            if (warpReady && core.node.autowarp) core.warp.WarpRegularAtRate((float)(vesselState.altitudeASL / (10 * Math.Abs(vesselState.speedVertical))));
            else core.warp.MinimumWarp();

            status = "Coasting toward deceleration burn";
        }

        void FixedUpdateDecelerationBurn()
        {
            if (vesselState.altitudeASL < DecelerationEndAltitude() + 5)
            {
                if (UseAtmosphereToBrake()) landStep = LandStep.FINAL_DESCENT;
                else landStep = LandStep.KILLING_HORIZONTAL_VELOCITY;
                core.warp.MinimumWarp();
                return;
            }

            double decelerationStartTime = (prediction.trajectory.Any() ? prediction.trajectory.First().UT : vesselState.time);
            if (decelerationStartTime - vesselState.time > 5)
            {
                core.thrust.targetThrottle = 0;

                status = "Warping to start of braking burn.";

                //warp to deceleration start
                Vector3d decelerationStartAttitude = -orbit.SwappedOrbitalVelocityAtUT(decelerationStartTime);
                decelerationStartAttitude += mainBody.getRFrmVel(orbit.SwappedAbsolutePositionAtUT(decelerationStartTime));
                decelerationStartAttitude = decelerationStartAttitude.normalized;
                core.attitude.attitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, this);
                bool warpReady = core.attitude.attitudeAngleFromTarget() < 5;

                if (warpReady && core.node.autowarp) core.warp.WarpToUT(decelerationStartTime - 5);
                else if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
                return;
            }

            Vector3d desiredThrustVector = -vessel.srf_velocity.normalized;

            Vector3d courseCorrection = ComputeCourseCorrection(false);
            double correctionAngle = courseCorrection.magnitude / (2.0 * vesselState.limitedMaxThrustAccel);
            correctionAngle = Math.Min(0.1, correctionAngle);
            desiredThrustVector = (desiredThrustVector + correctionAngle * courseCorrection.normalized).normalized;

            if (Vector3d.Dot(vessel.srf_velocity, vesselState.up) > 0
                     || Vector3d.Dot(vesselState.forward, desiredThrustVector) < 0.75)
            {
                core.thrust.targetThrottle = 0;
                status = "Braking";
            }
            else
            {
                double controlledSpeed = vesselState.speedSurface * Math.Sign(Vector3d.Dot(vessel.srf_velocity, vesselState.up)); //positive if we are ascending, negative if descending
                double desiredSpeed = -MaxAllowedSpeed();
                double desiredSpeedAfterDt = -MaxAllowedSpeedAfterDt(vesselState.deltaT);
                double minAccel = -vesselState.localg * Math.Abs(Vector3d.Dot(vessel.srf_velocity.normalized, vesselState.up));
                double maxAccel = vesselState.maxThrustAccel * Vector3d.Dot(vesselState.forward, -vessel.srf_velocity.normalized) - vesselState.localg * Math.Abs(Vector3d.Dot(vessel.srf_velocity.normalized, vesselState.up));
                const double speedCorrectionTimeConstant = 0.3;
                double speedError = desiredSpeed - controlledSpeed;
                double desiredAccel = speedError / speedCorrectionTimeConstant + (desiredSpeedAfterDt - desiredSpeed) / vesselState.deltaT;
                if (maxAccel - minAccel > 0) core.thrust.targetThrottle = Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0F, 1.0F);
                else core.thrust.targetThrottle = 0;
                status = "Braking: target speed = " + Math.Abs(desiredSpeed).ToString("F1") + " m/s";
            }

            core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, this);
        }

        // TODO I think that this function could be better rewritten to much more agressively kill the horizontal velocity. At present on low gravity bodies such as Bop, the craft will hover and slowly drift sideways, loosing the prescion of the landing. 
        public void DriveKillHorizontalVelocity(FlightCtrlState s)
        {
            Vector3d horizontalPointingDirection = Vector3d.Exclude(vesselState.up, vesselState.forward).normalized;
            if (Vector3d.Dot(horizontalPointingDirection, vessel.srf_velocity) > 0)
            {
                core.thrust.targetThrottle = 0;
                core.attitude.attitudeTo(Vector3.up, AttitudeReference.SURFACE_NORTH, this);
                landStep = LandStep.FINAL_DESCENT;
                return;
            }

            //control thrust to control vertical speed:
            const double desiredSpeed = 0; //hover until horizontal velocity is killed
            double controlledSpeed = Vector3d.Dot(vessel.srf_velocity, vesselState.up);
            double speedError = desiredSpeed - controlledSpeed;
            const double speedCorrectionTimeConstant = 1.0;
            double desiredAccel = speedError / speedCorrectionTimeConstant;
            double minAccel = -vesselState.localg;
            double maxAccel = -vesselState.localg + Vector3d.Dot(vesselState.forward, vesselState.up) * vesselState.maxThrustAccel;
            if (maxAccel - minAccel > 0)
            {
                core.thrust.targetThrottle = Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0F, 1.0F);
            }
            else
            {
                core.thrust.targetThrottle = 0;
            }

            //angle up and slightly away from vertical:
            Vector3d desiredThrustVector = (vesselState.up + 0.2 * horizontalPointingDirection).normalized;

            core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, this);

            status = "Killing horizontal velocity before final descent";
        }

        public void DriveUntargetedLanding(FlightCtrlState s)
        {
            if (vessel.LandedOrSplashed)
            {
                core.thrust.ThrustOff();
                core.thrust.users.Remove(this);
                StopLanding();
                return;
            }

            // TODO perhaps we should pop the parachutes at this point, or at least consider it depending on the altitude.

            double minalt = Math.Min(vesselState.altitudeBottom, Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue));
              
            if (vesselState.limitedMaxThrustAccel < vesselState.gravityForce.magnitude)
            {
                //if we have TWR < 1, just try as hard as we can to decelerate:
                //(we need this special case because otherwise the calculations spit out NaN's)
                core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
                core.thrust.trans_kill_h = true;
                core.thrust.trans_spd_act = 0;
            }
            else if (minalt > 300)
            {
                if ((vessel.srf_velocity.magnitude > 5) && (Vector3d.Angle(vessel.srf_velocity, vesselState.up) < 80))
                {
                    //if we have positive vertical velocity, point up and don't thrust:
                    core.attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, null);
                    core.thrust.tmode = MechJebModuleThrustController.TMode.DIRECT;
                    core.thrust.trans_spd_act = 0;
                }
                else if ((vessel.srf_velocity.magnitude > 5) && (Vector3d.Angle(vesselState.forward, -vessel.srf_velocity) > 45))
                {
                    //if we're not facing approximately retrograde, turn to point retrograde and don't thrust:
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                    core.thrust.tmode = MechJebModuleThrustController.TMode.DIRECT;
                    core.thrust.trans_spd_act = 0;
                }
                else
                {
                    //if we're above 300m, point retrograde and control surface velocity:
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                    
                    core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_SURFACE;
                    
                    //core.thrust.trans_spd_act = (float)Math.Sqrt((vesselState.maxThrustAccel - vesselState.gravityForce.magnitude) * 2 * minalt) * 0.90F;
                    Vector3d estimatedLandingPosition = vesselState.CoM + vessel.srf_velocity.sqrMagnitude / (2 * vesselState.limitedMaxThrustAccel) * vessel.srf_velocity.normalized;
                    double terrainRadius = mainBody.Radius + mainBody.TerrainAltitude(estimatedLandingPosition);
                    IDescentSpeedPolicy aggressivePolicy = new GravityTurnDescentSpeedPolicy(terrainRadius, mainBody.GeeASL * 9.81, vesselState.limitedMaxThrustAccel);
                    core.thrust.trans_spd_act = (float)(aggressivePolicy.MaxAllowedSpeed(vesselState.CoM - mainBody.position, vessel.srf_velocity));
                }
            }
            else
            {
                //last 300 meters:
                core.thrust.trans_spd_act = -Mathf.Lerp(0, (float)Math.Sqrt((vesselState.limitedMaxThrustAccel - vesselState.localg) * 2 * 300) * 0.90F, (float)minalt / 300);

                //take into account desired landing speed:
                core.thrust.trans_spd_act = (float)Math.Min(-touchdownSpeed, core.thrust.trans_spd_act);

//                core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
//                core.thrust.trans_kill_h = true;
                
//                if (Math.Abs(Vector3d.Angle(-vessel.srf_velocity, vesselState.up)) < 10)
                if (vesselState.speedSurfaceHorizontal < 5)
                {
                    //if we're falling more or less straight down, control vertical speed and 
                    //kill horizontal velocity
                    core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
                    core.thrust.trans_kill_h = true;
                }
                else
                {
                    //if we're falling at a significant angle from vertical, our vertical speed might be
                    //quite small but we might still need to decelerate. Control the total speed instead
                    //by thrusting directly retrograde
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                    core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_SURFACE;
                    core.thrust.trans_spd_act *= -1;
                }
            }

            status = "Final descent: " + vesselState.altitudeBottom.ToString("F0") + "m above terrain";
        }

        public void ControlParachutes()
        {
            // Firstly - do we have a parchute plan? If not then we had better get one quick!
            if (null == parachutePlan)
            {
                parachutePlan = new ParachutePlan(this);
            }

            // Are there any deployable parachutes? If not then there is no point in us being here. Lets switch to cruising to the deceleration burn instead.
            if (!ParachutesDeployable())
            {
                predictor.runErrorSimulations = false;
                parachutePlan.ClearData();
                return;
            }
            else
            {
                predictor.runErrorSimulations = true;
            }

            // Is there an error prediction available? If so add that into the mix
            if (this.ErrorPredictionReady)
            {
                parachutePlan.AddResult(errorPrediction);
            }

            // Has the Landing prediction been updated? If so then we can use the result to refine our parachute plan.
            if (this.PredictionReady)
            {
                parachutePlan.AddResult(prediction);
            }
        }

        void DeployParachutes()
        {
            if (vesselState.mainBody.atmosphere && deployChutes)
            {
                foreach (ModuleParachute p in vesselState.parachutes)
                {
                    // what is the ASL at which we should deploy this parachute? It is the actual deployment height above the surface + the ASL of the predicted landing point.
                    double LandingSiteASL = LandingAltitude;
                    double ParachuteDeployAboveGroundAtLandingSite = p.deployAltitude * this.parachutePlan.Multiplier;

                    double ASLDeployAltitude = ParachuteDeployAboveGroundAtLandingSite + LandingSiteASL;

                    if (p.part.inverseStage >= limitChutesStage && p.deploymentState == ModuleParachute.deploymentStates.STOWED && ASLDeployAltitude > vesselState.altitudeASL)
                    {
                        p.Deploy();
                        // Debug.Log("Deploying parachute " + p.name + " at " + ASLDeployAltitude + ". (" + LandingSiteASL + " + " + ParachuteDeployAboveGroundAtLandingSite +")");
                    }
                }
            }
        }

        // This methods works out if there are any parachutes that are capable of being deployed
        bool ParachutesDeployable()
        {
            if (!vesselState.mainBody.atmosphere) return false;
            if (!deployChutes) return false;

            foreach (ModuleParachute p in vesselState.parachutes)
            {
                if (p.part.inverseStage >= limitChutesStage && p.deploymentState == ModuleParachute.deploymentStates.STOWED)
                {
                    return true;
                }
            }

            return false;
        }

        // This methods works out if there are any parachutes that have already been deployed (or semi deployed)
        bool ParachutesDeployed()
        {
            return vesselState.parachuteDeployed;
        }


        void DeployLandingGears()
        {
            //new-style landing legs are activated by an event:
            //vessel.rootPart.SendEvent("LowerLeg");

            //old-style landings legs are activated on part activation:
            foreach (Part p in vessel.parts)
            {
                if (p.HasModule<ModuleLandingLeg>()) 
                    if ( p.inverseStage >= limitGearsStage )
                        foreach (ModuleLandingLeg l in p.FindModulesImplementing<ModuleLandingLeg>())
                            l.LowerLeg();

                if (p is LandingLeg)
                {
                    LandingLeg l = (LandingLeg)p;
                    if (l.legState == LandingLeg.LegStates.RETRACTED)
                    {
                        l.DeployOnActivate = true;
                        l.force_activate();
                    }
                }
            }
            deployedGears = true;
        }

        IDescentSpeedPolicy PickDescentSpeedPolicy()
        {
            if (UseAtmosphereToBrake())
            {
                return new PWBCoastDescentSpeedPolicy(mainBody.Radius + DecelerationEndAltitude()); // TODO this decent policy has been changed for experimentation.
            }

            return new SafeDescentSpeedPolicy(mainBody.Radius + DecelerationEndAltitude(), mainBody.GeeASL * 9.81, vesselState.limitedMaxThrustAccel);
        }

        double DecelerationEndAltitude()
        {
            if (UseAtmosphereToBrake())
            {
                // if the atmosphere is thick, deceleration (meaning freefall through the atmosphere)
                // should end a safe height above the landing site in order to allow braking from terminal velocity
                double landingSiteDragLength = mainBody.DragLength(LandingAltitude, (vesselState.massDrag + ParachuteAddedDragMass()) / vesselState.mass);

                return 2 * landingSiteDragLength + LandingAltitude;
            }
            else
            {
                //if the atmosphere is thin, the deceleration burn should end
                //500 meters above the landing site to allow for a controlled final descent
                return 500 + LandingAltitude;
            }
        }

        //On planets with thick enough atmospheres, we shouldn't do a deceleration burn. Rather,
        //we should let the atmosphere decelerate us and only burn during the final descent to
        //ensure a safe touchdown speed. How do we tell if the atmosphere is thick enough? We check
        //to see if there is an altitude within the atmosphere for which the characteristic distance
        //over which drag slows the ship is smaller than the altitude above the terrain. If so, we can
        //expect to get slowed to near terminal velocity before impacting the ground. 
        public bool UseAtmosphereToBrake()
        {
            //The air density goes like exp(-h/(scale height)), so the drag length goes like exp(+h/(scale height)).
            //Some math shows that if (scale height) > e * (surface drag length) then 
            //there is an altitude at which (altitude) > (drag length at that altitude).
            double seaLevelDragLength = mainBody.DragLength(0, (vesselState.massDrag + ParachuteAddedDragMass()) / vesselState.mass);
            return (1000 * mainBody.atmosphereScaleHeight > 2.71828 * seaLevelDragLength);
        }

        // This is not the exact number, but it's good enough for our use
        public double ParachuteAddedDragMass()
        {
            double addedDragMass = 0;
            if (vesselState.mainBody.atmosphere && deployChutes)
            {
                foreach (ModuleParachute p in vesselState.parachutes)
                {
                    if (p.part.inverseStage >= limitChutesStage)
                        switch (p.deploymentState)
                        {
                            case ModuleParachute.deploymentStates.STOWED:
                            case ModuleParachute.deploymentStates.ACTIVE:
                                addedDragMass += p.part.mass * p.fullyDeployedDrag - p.part.mass * p.stowedDrag;
                                break;
                            case ModuleParachute.deploymentStates.SEMIDEPLOYED:
                                addedDragMass += p.part.mass * p.fullyDeployedDrag - p.part.mass * p.semiDeployedDrag;
                                break;
                        }

                }
            }
            return addedDragMass;
        }






        bool UseLowDeorbitStrategy()
        {
            if (mainBody.atmosphere) return false;

            double periapsisSpeed = orbit.SwappedOrbitalVelocityAtUT(orbit.NextPeriapsisTime(vesselState.time)).magnitude;
            double stoppingDistance = Math.Pow(periapsisSpeed, 2) / (2 * vesselState.limitedMaxThrustAccel);

            return orbit.PeA < 2 * stoppingDistance + mainBody.Radius / 4;
        }

        double MaxAllowedSpeed()
        {
            return descentSpeedPolicy.MaxAllowedSpeed(vesselState.CoM - mainBody.position, vessel.srf_velocity);
        }

        double MaxAllowedSpeedAfterDt(double dt)
        {
            return descentSpeedPolicy.MaxAllowedSpeed(vesselState.CoM + vessel.obt_velocity * dt - mainBody.position,
                vessel.srf_velocity + dt * vesselState.gravityForce);
        }

        public override void OnStart(PartModule.StartState state)
        {
            predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();
        }

        public MechJebModuleLandingAutopilot(MechJebCore core) : base(core) { }
    }

    //A descent speed policy that gives the max safe speed if our entire velocity were straight down
    class SafeDescentSpeedPolicy : IDescentSpeedPolicy
    {
        double terrainRadius;
        double g;
        double thrust;

        public SafeDescentSpeedPolicy(double terrainRadius, double g, double thrust)
        {
            this.terrainRadius = terrainRadius;
            this.g = g;
            this.thrust = thrust;
        }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel)
        {
            double altitude = pos.magnitude - terrainRadius;
            return 0.9 * Math.Sqrt(2 * (thrust - g) * altitude);
        }
    }

    class CoastDescentSpeedPolicy : IDescentSpeedPolicy
    {
        double endCoastRadius;
        public CoastDescentSpeedPolicy(double endCoastRadius)
        {
            this.endCoastRadius = endCoastRadius;
        }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel)
        {
            if (pos.magnitude > endCoastRadius) return double.MaxValue;
            else return 0; 
        }
    }

    class PWBCoastDescentSpeedPolicy : IDescentSpeedPolicy
    {
        double endCoastRadius;
        public PWBCoastDescentSpeedPolicy(double endCoastRadius)
        {
            this.endCoastRadius = endCoastRadius;
        }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel)
        {
            if (pos.magnitude > endCoastRadius) return double.MaxValue;
            else return 1; //  It is a bit silly to set this at 0 because then the simulation times out as it never reaches the ground.
        }
    }

    class GravityTurnDescentSpeedPolicy : IDescentSpeedPolicy
    {
        double terrainRadius;
        double g;
        double thrust;

        public GravityTurnDescentSpeedPolicy(double terrainRadius, double g, double thrust)
        {
            this.terrainRadius = terrainRadius;
            this.g = g;
            this.thrust = thrust;
        }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel)
        {
            //do a binary search for the max speed that avoids death
            double maxFallDistance = pos.magnitude - terrainRadius;

            double lowerBound = 0;
            double upperBound = 1.1 * vel.magnitude;

            while (upperBound - lowerBound > 0.1)
            {
                double test = (upperBound + lowerBound) / 2;
                if (GravityTurnFallDistance(pos, test * vel.normalized) < maxFallDistance) lowerBound = test;
                else upperBound = test;
            }
            return 0.95 * ((upperBound + lowerBound) / 2);
        }

        public double GravityTurnFallDistance(Vector3d x, Vector3d v)
        {
            double startRadius = x.magnitude;

            const int steps = 10;
            for (int i = 0; i < steps; i++)
            {
                Vector3d gVec = -g * x.normalized;
                Vector3d thrustVec = -thrust * v.normalized;
                double dt = (1.0 / (steps - i)) * (v.magnitude / thrust);
                Vector3d newV = v + dt * (thrustVec + gVec);
                x += dt * (v + newV) / 2;
                v = newV;
            }

            double endRadius = x.magnitude;

            endRadius -= v.sqrMagnitude / (2 * (thrust - g));

            return startRadius - endRadius;
        }
    }

    // Class to hold all the information about the planned deployment of parachutes.
    class ParachutePlan
    {
        // We use a linear regresion to calculate the best place to deploy based on previous predictions
        LinearRegression regression;
        ReentrySimulation.Result lastResult; //  store the last result so that we can check if any new result is actually a new one, or the same one again.
        ReentrySimulation.Result lastErrorResult; //  store the last error result so that we can check if any new error result is actually a new one, or the same one again.
        CelestialBody body;
        MechJebModuleLandingAutopilot autoPilot;
        bool parachutePresent = false;
        double maxSemiDeployHeight;
        double minSemiDeployHeight;
        double maxMultiplier;
        double currentMultiplier;  // This is multiplied with the parachutes fulldeploy height to give the height at which the parachutes will be semideployed.
        double correlation; // This is the correlation coefficent of the dataset, and is used to tell if the data set is providing helpful information or not. It is exposed outside the class as a "quality percentage" where -1 -> 100% and 0 or more -> 0%
        const int dataSetSize = 40;

        public double Multiplier
        {
            get {return currentMultiplier;}
        }

        public int MultiplierDataAmount
        {
            get { return this.regression.dataSetSize; }
        }

        public double MultiplierQuality
        {
            get 
            { 
                double corr = this.correlation;
                if (Double.IsNaN(corr)) return 0;
                return Math.Max(0, corr * -100); 
            }
        }
        
        // Incorporates a new simulation result into the simulation data set and calculate a new semi deployment multiplier. If the data set has a poor correlation, then it might just leave the mutiplier. If the correlation becomes positive then it will clear the dataset and start again.
        public void AddResult(ReentrySimulation.Result newResult)
        {
            // if this result is the same as the old result, then it is not new!
            if (newResult.multiplierHasError)
            {
                if (lastErrorResult != null)
                {
                    if (newResult.id == lastErrorResult.id) { return; }
                }
                lastErrorResult = newResult;
            }
            else
            {
                if (lastResult != null)
                {
                    if (newResult.id == lastResult.id) { return; }
                }
                lastResult = newResult;
            }

            // What was the overshoot for this new result?
            double overshoot = newResult.GetOvershoot(this.autoPilot.core.target.targetLatitude, this.autoPilot.core.target.targetLongitude);

            // Debug.Log("overshoot: " + overshoot + " multiplier: " + newResult.parachuteMultiplier + " hasError:" + newResult.multiplierHasError);

            // Add the new result to the linear regression
            regression.Add(overshoot, newResult.parachuteMultiplier);

            // What is the correlation coefficent of the data. If it is weak a correlation then we will dismiss the dataset and use it to change the current multiplier
            correlation = regression.CorrelationCoefficent;
            if (correlation > -0.2) // TODO this is the best value to test for non-correlation?
            {
                // If the correlation is less that 0 then we will give up controlling the parachutes and throw away the dataset. Also check that we have got several bits of data, just in case we get two datapoints that are badly correlated.
                if (correlation > 0 && this.regression.dataSetSize > 5)
                {
                    ClearData();
                    // Debug.Log("Giving up control of the parachutes as the data does not correlate: " + correlation);
                }
                else
                {
                    // Debug.Log("Ignoring the simulation dataset because the correlation is not significant enough: " + correlation);
                }
            }
            else
            {
                // How much data is there? If just one datapoint then we need to slightly vary the multplier to avoid doing exactly the same multiplier again and getting a divide by zero!. If there is just two then we will not update the multiplier as we can't conclude much from two points of data!  
                int dataSetSize = regression.dataSetSize;
                if (dataSetSize == 1)
                {
                    this.currentMultiplier *= 0.99999;
                }
                else if (dataSetSize == 2)
                {
                    // Doing nothing
                }
                else
                {
                    // Use the linear regression to give us a new prediciton for when to open the parachutes
                    try
                    {
                        this.currentMultiplier = regression.yIntercept;
                    }
                    catch (Exception)
                    {
                        // If there is not enough data then we expect an exception. However we need to vary the multiplier everso slightly so that we get different data in order to start generating data. This should never happen as we have already checked the size of the dataset.
                        this.currentMultiplier *= 0.99999;
                    }
                }

                // Impose sensible limits on the multiplier
                if (this.currentMultiplier < 1) { this.currentMultiplier = 1; }
                if (this.currentMultiplier > this.maxMultiplier) { this.currentMultiplier = this.maxMultiplier; }
            }

            return;
        }

        public ParachutePlan(MechJebModuleLandingAutopilot _autopliot)
        {
            // Create the linear regression for storing previous prediction results
            this.regression = new LinearRegression(dataSetSize); // Store the previous however many predictions

            // Take a reference to the landing autopilot module that we are working for.
            this.autoPilot = _autopliot;

            // Take a note of which body this parachute plan is for. If we go to a different body, we will need a new plan!
            this.body = _autopliot.vessel.orbit.referenceBody;
         }

        // Throw away any old data, and create a new empty dataset
        public void ClearData()
        {
            // Create the linear regression for storing previous prediction results
            this.regression = new LinearRegression(dataSetSize); // Stored the previous 20 predictions
        }

        public void StartPlanning()
        {
            // what is the highest point at which we could semi deploy? - look at all the parachutes in the craft, and consider the lowest semi deployment pressure.
            float minSemiDeployPressure = 0; 
            float maxFullDeployHeight = 0;
            parachutePresent = false; // First assume that there are no parachutes.

            // TODO should we check if each of these parachutes is withing the staging limit?
            foreach (ModuleParachute p in autoPilot.vesselState.parachutes)
            {
                if (p.minAirPressureToOpen > minSemiDeployPressure) // Although this is called "minSemiDeployPressure" we want to find the largest value for each of our parachutes. This can be used to calculate the corresponding height, and hence a height at which we can be guarenteed that all our parachutes will deploy if asked to.
                {
                    minSemiDeployPressure = p.minAirPressureToOpen;
                }
                if (p.deployAltitude > maxFullDeployHeight)
                {
                    maxFullDeployHeight = p.deployAltitude;
                }

                parachutePresent = true;
            }

            // If parachtes are present on the craft then work out the max / min semideployment heights and the starting value.
            if(true == parachutePresent)
            {
                // TODO is there benefit in running an intial simulation to calculate the height at which the ratio between vertical and horizontal velocity would be the best for being able to deply the chutes to control the landing site?

                // At what ASL height does the reference body have this pressure?
                maxSemiDeployHeight = (this.body.atmosphereScaleHeight *1000) * -1 * Math.Log(minSemiDeployPressure / this.body.atmosphereMultiplier);
                
                // We have to have semi deployed by the time we fully deploy.
                minSemiDeployHeight = maxFullDeployHeight;

                maxMultiplier = maxSemiDeployHeight / minSemiDeployHeight;

                // Set the inital mutiplier to be the mid point.
                currentMultiplier = maxMultiplier / 2;
            }
        }
    }

    // A class to hold a set of x,y data and perform linear regression analysis on it
    class LinearRegression
    {
        double[] x;
        double[] y;
        int maxDataPoints;
        int dataPointCount;
        int currentDataPoint;
        double sumX;
        double sumY;
        double sumXX;
        double sumXY;
        double sumYY;

        public LinearRegression(int _maxDataPoints)
        {
            this.maxDataPoints = _maxDataPoints;
            this.dataPointCount = 0;
            this.currentDataPoint = -1;

            this.x = new double[_maxDataPoints];
            this.y = new double[_maxDataPoints];
        }

        // Add a new data point. It might be that this will replace an old data point, or it might be that this is the first or second datapoints
        public void Add(double _x, double _y)
        {
            currentDataPoint++;
            dataPointCount++;

            // wrap back to the begining if we have got the end of the array
            if (currentDataPoint >= maxDataPoints)
            {
                currentDataPoint = 0;
            }

            // If we have maxed out the number of data points then we need to remove the old values from the running totals
            if (dataPointCount > maxDataPoints)
            {
                dataPointCount = maxDataPoints;
            }

            x[currentDataPoint] = _x;
            y[currentDataPoint] = _y;

            // Calculate the new totals
            sumX = 0;
            sumY = 0;
            sumXX = 0;
            sumXY = 0;
            sumYY = 0;

            for (int i = 0; i < dataPointCount; i++)
            {
                double thisx = x[i];
                double thisy = y[i];

                sumX += thisx;
                sumXX += thisx * thisx;
                sumY += thisy;
                sumYY += thisy * thisy;
                sumXY += thisx * thisy;
            }
        }

        public double slope
        {
            get
            {
                // Require at least half the datapoints in the datset 
                if(this.dataPointCount <2)
                {
                    throw new Exception("Not enough data to calculate trend line");
                }

                double result = (sumXY - ((sumX*sumY)/dataPointCount)) / (sumXX - ((sumX* sumX)/dataPointCount));

                return result;
            }
        }

        public double yIntercept
        {
            get
            {
                double result = (sumY/dataPointCount) - (slope * (sumX / dataPointCount));

                return result;
            }
        }

        public int dataSetSize
        {
            get
            {
                return this.dataPointCount;
            }
        }

        // Calculation the Pearson product-moment correlation coefficient
        public double CorrelationCoefficent
        {
            get
            {
                double stdX = Math.Sqrt(sumXX / dataPointCount - sumX * sumX / dataPointCount / dataPointCount);
                double stdY = Math.Sqrt(sumYY / dataPointCount - sumY * sumY / dataPointCount / dataPointCount);
                double covariance = (sumXY / dataPointCount - sumX * sumY / dataPointCount / dataPointCount);

                return covariance / stdX / stdY;
            }
        }

    }
}
