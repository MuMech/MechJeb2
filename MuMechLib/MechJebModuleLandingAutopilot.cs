using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleLandingAutopilot : ComputerModule
    {

        //public interface:
        public void LandAtPositionTarget()
        {
            this.enabled = true;

            predictor.users.Add(this);
            vessel.RemoveAllManeuverNodes(); //for the benefit of the landing predictions module

            deployedGears = false;
            deorbitBurnTriggered = false;
            lowDeorbitEndConditionSet = false;
            planeChangeTriggered = false;

            if (orbit.PeA < 0) landStep = LandStep.DECELERATING;
            else if (UseLowDeorbitStrategy()) landStep = LandStep.PLANE_CHANGE;
            else landStep = LandStep.DEORBIT_BURN;
        }

        public void LandUntargeted()
        {
            this.enabled = true;

            deployedGears = false;

            landStep = LandStep.UNTARGETED_DEORBIT;
        }

        public void StopLanding()
        {
            this.enabled = false;
        }

        [Persistent(pass = (int)(Pass.Local | Pass.Type | Pass.Global))]
        public double touchdownSpeed = 0.5;
        [Persistent(pass = (int)Pass.Global)]
        public bool autowarp = true;

        public string status = "";       


        //Internal state:
        enum LandStep { PLANE_CHANGE, LOW_DEORBIT_BURN, DEORBIT_BURN, COURSE_CORRECTIONS, COAST_TO_DECELERATION, 
            DECELERATING, KILLING_HORIZONTAL_VELOCITY, FINAL_DESCENT, UNTARGETED_DEORBIT, OFF }
        LandStep landStep = LandStep.OFF;

        IDescentSpeedPolicy descentSpeedPolicy;

        bool planeChangeTriggered = false;
        double planeChangeDVLeft = 0;
        bool deorbitBurnTriggered = false;
        double lowDeorbitBurnMaxThrottle;
        bool lowDeorbitEndConditionSet = false;
        bool lowDeorbitEndOnLandingSiteNearer = false;
        bool deployedGears = false;

        double lowDeorbitBurnTriggerFactor = 2;

        //Landing prediction data:
        MechJebModuleLandingPredictions predictor;
        ReentrySimulation.Result prediction;
        bool PredictionReady //We shouldn't do any autopilot stuff until this is true
        {
            get { return (prediction != null) && 
                         (prediction.outcome == ReentrySimulation.Outcome.LANDED); }
        }
        double LandingAltitude //the altitude above sea level of the terrain at the landing site
        {
            get 
            {
                if (PredictionReady) return mainBody.TerrainAltitude(prediction.endPosition.latitude, prediction.endPosition.longitude);
                else return 0;
            }
        }
        Vector3d LandingSite //the current position of the landing site
        {
            get { return mainBody.GetWorldSurfacePosition(prediction.endPosition.latitude, 
                                                          prediction.endPosition.longitude, LandingAltitude); }
        }
        Vector3d RotatedLandingSite //the position where the landing site will be when we land at it
        {
            get { return prediction.WorldEndPosition(); }
        }


        public override void OnModuleEnabled()
        {
            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.users.Remove(this);
            core.thrust.users.Remove(this);
            predictor.users.Remove(this);
            predictor.descentSpeedPolicy = null;
            core.KillThrottle();
            landStep = LandStep.OFF;
            status = "Off";
        }



        public override void Drive(FlightCtrlState s)
        {
            if (landStep == LandStep.OFF) return;

            descentSpeedPolicy = PickDescentSpeedPolicy();

            predictor.descentSpeedPolicy = PickDescentSpeedPolicy(); //create a separate IDescentSpeedPolicy object for the simulation
            predictor.endAltitudeASL = DecelerationEndAltitude();
            
            prediction = predictor.GetResult(); //grab a reference to the current result, in case a new one comes in while we're doing stuff

            if (prediction == null) Debug.Log("prediction is null");
            else Debug.Log("prediction.outcme = " + prediction.outcome);

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

                case LandStep.COAST_TO_DECELERATION:
                    DriveCoastToDeceleration(s);
                    break;

                case LandStep.DECELERATING:
                    DriveDecelerationBurn(s);
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

                default:
                    break;
            }
        }

        void DriveUntargetedDeorbit(FlightCtrlState s)
        {
            if (orbit.PeA < -0.1 * mainBody.Radius)
            {
                s.mainThrottle = 0;
                landStep = LandStep.FINAL_DESCENT;
                return;
            }

            core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT_HORIZONTAL, this);
            if (core.attitude.attitudeAngleFromTarget() < 5) s.mainThrottle = 1;
            else s.mainThrottle = 0;

            status = "Doing deorbit burn.";
        }

        void FixedUpdatePlaneChange()
        {
            Vector3d targetRadialVector = mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0);
            Vector3d currentRadialVector = vesselState.CoM - mainBody.position;
            double angleToTarget = Vector3d.Angle(targetRadialVector, currentRadialVector);
            bool approaching = Vector3d.Dot(targetRadialVector - currentRadialVector, vesselState.velocityVesselOrbit) > 0;

            Debug.Log("angleToTarget = " + angleToTarget + "; approaching = " + approaching);

            if (!planeChangeTriggered && approaching && (angleToTarget > 80) && (angleToTarget < 90))
            {
                Debug.Log("triggering plane change");
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp(true);
                planeChangeTriggered = true;
            }

            if (planeChangeTriggered)
            {
                Vector3d horizontalToTarget = ComputePlaneChange();
                Vector3d finalVelocity = Quaternion.FromToRotation(vesselState.horizontalOrbit, horizontalToTarget) * vesselState.velocityVesselOrbit;

                Vector3d deltaV = finalVelocity - vesselState.velocityVesselOrbit;
                //burn normal+ or normal- to avoid dropping the Pe:
                Vector3d burnDir = Vector3d.Exclude(vesselState.up, Vector3d.Exclude(vesselState.velocityVesselOrbit, deltaV));
                planeChangeDVLeft = Math.PI / 180 * Vector3d.Angle(finalVelocity, vesselState.velocityVesselOrbit) * vesselState.speedOrbitHorizontal;
                core.attitude.attitudeTo(burnDir, AttitudeReference.INERTIAL, this);
                if (planeChangeDVLeft < 0.1F)
                {
                    landStep = LandStep.LOW_DEORBIT_BURN;
                }

                status = "Executing low orbit plane change of about " + planeChangeDVLeft.ToString("F0") + " m/s";
            }
            else
            {
                if (autowarp) core.warp.WarpRegularAtRate((float)(orbit.period / 60));
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
            Debug.Log("orbit.trueAnomaly = " + orbit.trueAnomaly);
            Debug.Log("time for 3.6 degrees = " + (orbit.TimeOfTrueAnomaly(orbit.trueAnomaly + 3.6, vesselState.time) - vesselState.time));
            Debug.Log("orbit.period = " + orbit.period);
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
                s.mainThrottle = Mathf.Clamp01((float)(planeChangeDVLeft / (2 * vesselState.maxThrustAccel)));
            }
            else
            {
                s.mainThrottle = 0.0F;
            }
        }

        void FixedUpdateLowDeorbitBurn()
        {
            //Decide when we will start the deorbit burn:
            double stoppingDistance = Math.Pow(vesselState.speedSurfaceHorizontal, 2) / (2 * vesselState.maxThrustAccel);
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

            Debug.Log("range to target   = " + rangeToTarget);
            Debug.Log("trigger = " + triggerDistance);

            if (deorbitBurnTriggered) status = "Executing low deorbit burn";
            else status = "Moving to low deorbit burn point";
            
            //Warp toward deorbit burn if it hasn't been triggerd yet:
            if (!deorbitBurnTriggered && autowarp && rangeToTarget > 2 * triggerDistance) core.warp.WarpRegularAtRate((float)(orbit.period / 60));
            if (rangeToTarget < 2 * triggerDistance && !MuUtils.PhysicsRunning()) core.warp.MinimumWarp();

            //By default, thrust straight back at max throttle
            Vector3d thrustDirection = -vesselState.velocityVesselSurfaceUnit;
            lowDeorbitBurnMaxThrottle = 1;

            //If we are burning, we watch the predicted landing site and switch to the braking
            //burn when the predicted landing site crosses the target. We also use the predictions
            //to steer the predicted landing site toward the target
            if (deorbitBurnTriggered && PredictionReady)
            {
                //angle slightly left or right to fix any cross-range error in the predicted landing site:
                Vector3d horizontalToLandingSite = Vector3d.Exclude(vesselState.up, LandingSite - vesselState.CoM).normalized;
                Vector3d horizontalToTarget = Vector3d.Exclude(vesselState.up, core.target.GetPositionTargetPosition() - vesselState.CoM).normalized;
                double angleGain = 4;
                Vector3d angleCorrection = angleGain * (horizontalToTarget - horizontalToLandingSite);
                if(angleCorrection.magnitude > 0.1) angleCorrection *= 0.1 / angleCorrection.magnitude;
                thrustDirection = (thrustDirection + angleCorrection).normalized;

                Debug.Log("Angle to correct = " + Vector3d.Angle(horizontalToTarget, horizontalToLandingSite));


                double rangeToLandingSite = Vector3d.Exclude(vesselState.up, LandingSite - vesselState.CoM).magnitude;
                double maxAllowedSpeed = MaxAllowedSpeed();

                if (!lowDeorbitEndConditionSet)
                {
                    lowDeorbitEndOnLandingSiteNearer = rangeToLandingSite > rangeToTarget;
                    lowDeorbitEndConditionSet = true;
                    Debug.Log("NOW SETTING lowDeorbitEndOnLandingSiteNearer = " + lowDeorbitEndOnLandingSiteNearer);
                }

                if (rangeToLandingSite > rangeToTarget)
                {
                    if (!lowDeorbitEndOnLandingSiteNearer)
                    {
                        Debug.Log("Switching to decelerating because landing site is far");
                        landStep = LandStep.DECELERATING;
                    }

                    double maxAllowedSpeedAfterDt = MaxAllowedSpeedAfterDt(vesselState.deltaT);
                    double speedAfterDt = vesselState.speedSurface + vesselState.deltaT * Vector3d.Dot(vesselState.gravityForce, vesselState.velocityVesselSurfaceUnit); 
                    double throttleToMaintainLandingSite;
                    if (vesselState.speedSurface < maxAllowedSpeed) throttleToMaintainLandingSite = 0;
                    else throttleToMaintainLandingSite = (speedAfterDt - maxAllowedSpeedAfterDt) / (vesselState.deltaT * vesselState.maxThrustAccel);

                    lowDeorbitBurnMaxThrottle = throttleToMaintainLandingSite + 1 * (rangeToLandingSite / rangeToTarget - 1) + 0.2;
                }
                else
                {
                    if(lowDeorbitEndOnLandingSiteNearer) 
                    {
                        Debug.Log("Switching to decelerating because landing site is near");
                        lowDeorbitBurnMaxThrottle = 1;
                        landStep = LandStep.DECELERATING;
                    }
                    else
                    {
                        lowDeorbitBurnMaxThrottle = 0;
                        status = "Deorbit burn complete: waiting for the right moment to start braking";
                    }
                }
            }

            Debug.Log("final max throttle = " + lowDeorbitBurnMaxThrottle);

            core.attitude.attitudeTo(thrustDirection, AttitudeReference.INERTIAL, this);

        }

        void DriveLowDeorbitBurn(FlightCtrlState s)
        {
            if (deorbitBurnTriggered && core.attitude.attitudeAngleFromTarget() < 5)
            {
                s.mainThrottle = Mathf.Clamp01((float)lowDeorbitBurnMaxThrottle);
            }
            else
            {
                s.mainThrottle = 0.0F;
            }
            Debug.Log("low deorbit applying throttle " + s.mainThrottle);
        }


        void FixedUpdateDeorbitBurn()
        {
            //if we don't want to deorbit but we're already on a reentry trajectory, we can't wait until the ideal point 
            //in the orbit to deorbt; we already have deorbited.
            if (part.vessel.orbit.ApA < part.vessel.mainBody.RealMaxAtmosphereAltitude())
            {
                landStep = LandStep.COURSE_CORRECTIONS;
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
            Vector3d currentHorizontalVelocity = Vector3d.Exclude(vesselState.up, vesselState.velocityVesselOrbit); //Find our current horizontal velocity
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
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp(true); //get out of warp

                Vector3d deltaV = finalHorizontalVelocity - currentHorizontalVelocity;
                core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, this);

                if (deltaV.magnitude < 2.0) landStep = LandStep.COURSE_CORRECTIONS;

                status = "Doing high deorbit burn";
            }
            else
            {
                core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT, this);
                core.warp.WarpRegularAtRate((float)(orbit.period / 60));

                status = "Moving to high deorbit burn point";
            }
        }

        void DriveDeorbitBurn(FlightCtrlState s)
        {
            if (deorbitBurnTriggered && core.attitude.attitudeAngleFromTarget() < 5) s.mainThrottle = 1.0F;
            else s.mainThrottle = 0.0F;
        }


        //Estimate the delta-V of the correction burn that would be required to put us on
        //course for the target
        public Vector3d ComputeCourseCorrection(bool allowPrograde)
        {
            //actualLandingPosition is the predicted actual landing position
            Vector3d actualLandingPosition = RotatedLandingSite - mainBody.position;

            //orbitLandingPosition is the point where our current orbit intersects the planet
            double endRadius = mainBody.Radius + DecelerationEndAltitude() - 100;
            Vector3d orbitLandingPosition = orbit.SwappedRelativePositionAtUT(orbit.NextTimeOfRadius(vesselState.time, endRadius));

            //convertOrbitToActual is a rotation that rotates orbitLandingPosition on actualLandingPosition
            Quaternion convertOrbitToActual = Quaternion.FromToRotation(orbitLandingPosition, actualLandingPosition);

            //Consider the effect small changes in the velocity in each of these three directions
            Vector3d[] perturbationDirections = { vesselState.velocityVesselSurfaceUnit, vesselState.radialPlusSurface, vesselState.normalPlusSurface };

            //Compute the effect burns in these directions would
            //have on the landing position, where we approximate the landing position as the place
            //the perturbed orbit would intersect the planet.
            Vector3d[] deltas = new Vector3d[3];
            for (int i = 0; i < 3; i++)
            {
                double perturbationDeltaV = 0.1;
                Orbit perturbedOrbit = orbit.PerturbedOrbit(vesselState.time, perturbationDeltaV * perturbationDirections[i]); //compute the perturbed orbit
                Vector3d perturbedLandingPosition = perturbedOrbit.SwappedRelativePositionAtUT(perturbedOrbit.NextTimeOfRadius(vesselState.time, endRadius)); //find where it hits the planet
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
            float bodyRotationAngleDuringDescent = (float)(360*(prediction.endUT - vesselState.time)/mainBody.rotationPeriod);
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
                //(e.g. when we are travelling close to straight up)
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
            double currentError = Vector3d.Distance(core.target.GetPositionTargetPosition(), LandingSite);

            if (currentError < 300)
            {
                s.mainThrottle = 0;
                landStep = LandStep.COAST_TO_DECELERATION;
                return;
            }

            Vector3d deltaV = ComputeCourseCorrection(true);

            status = "Performing course correction of about " + deltaV.magnitude.ToString("F1") + " m/s";

            core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, this);

            if (core.attitude.attitudeAngleFromTarget() < 5)
            {
                s.mainThrottle = Mathf.Clamp01((float)(deltaV.magnitude / (10.0 * vesselState.maxThrustAccel)));
            }
            else
            {
                s.mainThrottle = 0;
            }
        }

        void FixedUpdateCoastToDeceleration()
        {
            double maxAllowedSpeed = MaxAllowedSpeed();
            Debug.Log("maxAllowedSpeed = " + maxAllowedSpeed);
            if (vesselState.speedSurface > 0.9 * maxAllowedSpeed)
            {
                core.warp.MinimumWarp(true);
                landStep = LandStep.DECELERATING;
                return;
            }

            double currentError = Vector3d.Distance(core.target.GetPositionTargetPosition(), LandingSite);
            if (currentError > 600)
            {
                core.warp.MinimumWarp(true);
                landStep = LandStep.COURSE_CORRECTIONS;
                return;
            }

            bool warpReady = true;
            if (PredictionReady)
            {
                double decelerationStartTime = prediction.trajectory.First().UT;
                Vector3d decelerationStartAttitude = -orbit.SwappedOrbitalVelocityAtUT(decelerationStartTime).normalized;
                core.attitude.attitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, this);
                warpReady = core.attitude.attitudeAngleFromTarget() < 5;
            }

            //Warp at a rate no higher than the rate that would have us impacting the ground 10 seconds from now:
            Debug.Log("Trying to warp at x" + (float)(vesselState.altitudeASL / (10 * Math.Abs(vesselState.speedVertical))));
            if (warpReady) core.warp.WarpRegularAtRate((float)(vesselState.altitudeASL / (10 * Math.Abs(vesselState.speedVertical))));
            else core.warp.MinimumWarp();

            status = "Coasting toward deceleration burn";
        }

        void DriveCoastToDeceleration(FlightCtrlState s)
        {
            s.mainThrottle = 0;
        }

        void DriveDecelerationBurn(FlightCtrlState s)
        {
            if (vesselState.altitudeASL < DecelerationEndAltitude() + 5)
            {
                landStep = LandStep.KILLING_HORIZONTAL_VELOCITY;
                return;
            }

            Vector3d desiredThrustVector = -vesselState.velocityVesselSurfaceUnit;

            Vector3d courseCorrection = ComputeCourseCorrection(false);
            Debug.Log("course correction dv = " + courseCorrection.magnitude);
            double correctionAngle = courseCorrection.magnitude / (2.0 * vesselState.maxThrustAccel);
            correctionAngle = Math.Min(0.1, correctionAngle);
            desiredThrustVector = (desiredThrustVector + correctionAngle * courseCorrection.normalized).normalized;

            if (Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up) > 0
                     || Vector3d.Dot(vesselState.forward, desiredThrustVector) < 0.75)
            {
                s.mainThrottle = 0;
                status = "Braking";
            }
            else
            {
                double controlledSpeed = vesselState.speedSurface * Math.Sign(Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up)); //positive if we are ascending, negative if descending
                double desiredSpeed = -MaxAllowedSpeed();
                double desiredSpeedAfterDt = -MaxAllowedSpeedAfterDt(vesselState.deltaT);
                double minAccel = -vesselState.localg * Math.Abs(Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.up));
                double maxAccel = vesselState.maxThrustAccel * Vector3d.Dot(vesselState.forward, -vesselState.velocityVesselSurfaceUnit) - vesselState.localg * Math.Abs(Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.up));
                double speedCorrectionTimeConstant = 0.3;
                double speedError = desiredSpeed - controlledSpeed;
                double desiredAccel = speedError / speedCorrectionTimeConstant + (desiredSpeedAfterDt - desiredSpeed) / vesselState.deltaT;
                if (maxAccel - minAccel > 0) s.mainThrottle = Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0F, 1.0F);
                else s.mainThrottle = 0;
                status = "Braking: target speed = " + Math.Abs(desiredSpeed).ToString("F1") + " m/s";
            }

            core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, this);

        }

        public void DriveKillHorizontalVelocity(FlightCtrlState s)
        {
            Vector3d horizontalPointingDirection = Vector3d.Exclude(vesselState.up, vesselState.forward).normalized;
            if (Vector3d.Dot(horizontalPointingDirection, vesselState.velocityVesselSurface) > 0)
            {
                s.mainThrottle = 0;
                core.attitude.attitudeTo(Vector3.up, AttitudeReference.SURFACE_NORTH, this);
                landStep = LandStep.FINAL_DESCENT;
                return;
            }

            //control thrust to control vertical speed:
            double desiredSpeed = 0; //hover until horizontal velocity is killed
            double controlledSpeed = Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up);
            double speedError = desiredSpeed - controlledSpeed;
            double speedCorrectionTimeConstant = 1.0;
            double desiredAccel = speedError / speedCorrectionTimeConstant;
            double minAccel = -vesselState.localg;
            double maxAccel = -vesselState.localg + Vector3d.Dot(vesselState.forward, vesselState.up) * vesselState.maxThrustAccel;
            if (maxAccel - minAccel > 0)
            {
                s.mainThrottle = Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0F, 1.0F);
            }
            else
            {
                s.mainThrottle = 0;
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
                s.mainThrottle = 0;
                StopLanding();
                return;
            }

            double minalt = Math.Min(vesselState.altitudeBottom, Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue));

            if (!deployedGears && (minalt < 1000)) DeployLandingGears();

            if (Vector3d.Angle(vesselState.velocityVesselSurface, vesselState.up) < 80)
            {
                //if we have positive vertical velocity, point up and don't thrust:
                core.attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, null);
                core.thrust.tmode = MechJebModuleThrustController.TMode.OFF;
            }
            else if (Vector3d.Angle(vesselState.forward, -vesselState.velocityVesselSurface) > 20)
            {
                //if we're not facing approximately retrograde, turn to point retrograde and don't thrust:
                core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                core.thrust.tmode = MechJebModuleThrustController.TMode.OFF;
            }
            else if (vesselState.maxThrustAccel < vesselState.gravityForce.magnitude)
            {
                //if we have TWR < 1, just try as hard as we can to decelerate:
                //(we need this special case because otherwise the calculations spit out NaN's)
                core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
                core.thrust.trans_kill_h = true;
                core.thrust.trans_spd_act = 0;
            }
            else if (minalt > 200)
            {
                //if we're above 200m, point retrograde and control surface velocity:
                core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);

                core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_SURFACE;

                //core.thrust.trans_spd_act = (float)Math.Sqrt((vesselState.maxThrustAccel - vesselState.gravityForce.magnitude) * 2 * minalt) * 0.90F;
                Vector3d estimatedLandingPosition = vesselState.CoM + vesselState.velocityVesselSurface.sqrMagnitude / (2 * vesselState.maxThrustAccel) * vesselState.velocityVesselSurfaceUnit;
                double terrainRadius = mainBody.Radius + mainBody.TerrainAltitude(estimatedLandingPosition);
                IDescentSpeedPolicy aggressivePolicy = new GravityTurnDescentSpeedPolicy(terrainRadius, mainBody.GeeASL * 9.81, vesselState.maxThrustAccel);
                core.thrust.trans_spd_act = (float)(aggressivePolicy.MaxAllowedSpeed(vesselState.CoM - mainBody.position, vesselState.velocityVesselSurface));
            }
            else
            {
                //last 200 meters:
                core.thrust.trans_spd_act = -Mathf.Lerp(0, (float)Math.Sqrt((vesselState.maxThrustAccel - vesselState.gravityForce.magnitude) * 2 * 200) * 0.90F, (float)minalt / 200);

                //take into account desired landing speed:
                core.thrust.trans_spd_act = (float)Math.Min(-touchdownSpeed, core.thrust.trans_spd_act);

                if (Math.Abs(Vector3d.Angle(-vesselState.velocityVesselSurface, vesselState.up)) < 10)
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


        void DeployLandingGears()
        {
            //new-style landing legs are activated by an event:
            vessel.rootPart.SendEvent("LowerLeg");

            //old-style landings legs are actived on part activation:
            foreach (Part p in vessel.parts)
            {
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
            if (UseAtmosphereToBrake()) return new CoastDescentSpeedPolicy();

            return new SafeDescentSpeedPolicy(mainBody.Radius + DecelerationEndAltitude(), mainBody.GeeASL * 9.81, vesselState.maxThrustAccel);
        }

        double DecelerationEndAltitude()
        {
            if (UseAtmosphereToBrake())
            {
                //if the atmosphere is thick, deceleration (meaning freefall through the atmosphere)
                //should end a safe height above the landing site in order to allow braking from terminal velocity
                double landingSiteDragLength = mainBody.DragLength(LandingAltitude, vesselState.massDrag / vesselState.mass);
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
        bool UseAtmosphereToBrake()
        {
            //The air density goes like exp(-h/(scale height)), so the drag length goes like exp(+h/(scale height)).
            //Some math shows that if (scale height) > e * (surface drag length) then 
            //there is an altitude at which (altitude) > (drag length at that altitude).
            double seaLevelDragLength = mainBody.DragLength(0, vesselState.massDrag / vesselState.mass);
            return (1000 * mainBody.atmosphereScaleHeight > 2.71828 * seaLevelDragLength);
        }

        bool UseLowDeorbitStrategy()
        {
            if (mainBody.atmosphere) return false;

            double periapsisSpeed = orbit.SwappedOrbitalVelocityAtUT(orbit.NextPeriapsisTime(vesselState.time)).magnitude;
            double stoppingDistance = Math.Pow(periapsisSpeed, 2) / (2 * vesselState.maxThrustAccel);

            Debug.Log("periapsisSpeed = " + periapsisSpeed);
            Debug.Log("stoppingDistance = " + stoppingDistance);

            return orbit.PeA < 2 * stoppingDistance + mainBody.Radius / 4;
        }


        double MaxAllowedSpeed()
        {
            return descentSpeedPolicy.MaxAllowedSpeed(vesselState.CoM - mainBody.position, vesselState.velocityVesselSurface);                
        }

        double MaxAllowedSpeedAfterDt(double dt)
        {
            return descentSpeedPolicy.MaxAllowedSpeed(vesselState.CoM + vesselState.velocityVesselOrbit *  dt - mainBody.position, 
                vesselState.velocityVesselSurface + dt * vesselState.gravityForce);                    
        }

        public override void OnStart(PartModule.StartState state)
        {
            predictor = core.GetComputerModule<MechJebModuleLandingPredictions>();
        }

        public MechJebModuleLandingAutopilot(MechJebCore core) : base(core) { }






    }






    class CoastDescentSpeedPolicy : IDescentSpeedPolicy
    {
        public CoastDescentSpeedPolicy() { }

        public double MaxAllowedSpeed(Vector3d pos, Vector3d vel) { return double.MaxValue; }
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

            int steps = 10;
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

}
