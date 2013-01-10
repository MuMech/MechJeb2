using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleLandingAutopilot : DisplayModule
    {
        public MechJebModuleLandingAutopilot(MechJebCore core) : base(core) { }

        public double touchdownSpeed = 0.5;

        bool deployedGears = false;

        enum LandStep { OFF, FINAL_DESCENT, KILLING_HORIZONTAL_VELOCITY, DECELERATING, COURSE_CORRECTIONS }
        LandStep landStep;

        MechJebModuleLandingPredictions predictions;

        public override void OnStart(PartModule.StartState state)
        {
            predictions = core.GetComputerModule<MechJebModuleLandingPredictions>();
        }

        public override void OnModuleEnabled()
        {
            core.attitude.enabled = true;
            core.thrust.enabled = true;
            deployedGears = false;
        }

        public override void OnModuleDisabled()
        {
            core.attitude.enabled = false;
            core.thrust.enabled = false;
        }

        
        public override void Drive(FlightCtrlState s)
        {
            predictions.descentSpeedPolicy = PickDescentSpeedPolicy();

            switch (landStep)
            {
                case LandStep.COURSE_CORRECTIONS:
                    DriveCourseCorrections(s);
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

                case LandStep.OFF:
                    break;
            }
        
        }

        protected override void FlightWindowGUI(int windowID)
        {
            if (predictions.result != null && predictions.result.outcome == ReentrySimulation.Outcome.LANDED) GLUtils.DrawMapViewGroundMarker(mainBody, predictions.result.endPosition.latitude, predictions.result.endPosition.longitude, Color.blue, 60);

            GUILayout.BeginVertical();

            if (GUILayout.Button("Pick target")) core.target.PickPositionTargetOnMap();

            predictions.enabled = GUILayout.Toggle(predictions.enabled, "Predictions enabled");

            if (GUILayout.Button("Land at target")) landStep = LandStep.COURSE_CORRECTIONS;
            if (GUILayout.Button("Stop landing")) landStep = LandStep.OFF;

            if (predictions.result != null && predictions.result.outcome == ReentrySimulation.Outcome.LANDED)
            {
                double error = Vector3d.Distance(mainBody.GetRelSurfacePosition(predictions.result.endPosition.latitude, predictions.result.endPosition.longitude, 0),
                                                 mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0));
                GUILayout.Label("Landing position error = " + error.ToString("F0") + "m");
            }
            
            GUILayout.EndVertical();

            GUI.DragWindow();
        }


        //Estimate the delta-V of the correction burn that would be required to put us on
        //course for the target
        public Vector3d ComputeCourseCorrection(bool allowPrograde)
        {
            if (!predictions.enabled) return Vector3d.zero;
            if (predictions.result.outcome != ReentrySimulation.Outcome.LANDED) return Vector3d.zero;

            //actualLandingPosition is the predicted actual landing position
            Vector3d actualLandingPosition = predictions.result.referenceFrame.WorldPositionAtCurrentTime(predictions.result.endPosition) - mainBody.position;

            //orbitLandingPosition is the point where our current orbit intersects the planet
            Vector3d orbitLandingPosition = orbit.SwappedRelativePositionAtUT(orbit.NextTimeOfRadius(vesselState.time, mainBody.Radius));

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
                Vector3d perturbedLandingPosition = perturbedOrbit.SwappedRelativePositionAtUT(perturbedOrbit.NextTimeOfRadius(vesselState.time, mainBody.Radius)); //find where it hits the planet
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
            float bodyRotationAngleDuringDescent = (float)(360*(predictions.result.endUT - vesselState.time)/mainBody.rotationPeriod);
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

            Vector3d velocityCorrection = coeffs.x * downrangeDirection + coeffs.y * perturbationDirections[2];

            return velocityCorrection;
        }

        void DriveCourseCorrections(FlightCtrlState s)
        {
            Vector3d deltaV = ComputeCourseCorrection(true);

            Debug.Log("deltaV.magnitude = " + deltaV.magnitude);

            if (deltaV.magnitude < 0.2) landStep = LandStep.DECELERATING; 

            core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, this);
            s.mainThrottle = (float)(deltaV.magnitude / (10.0 *vesselState.maxThrustAccel));
        }

        void DriveDecelerationBurn(FlightCtrlState s)
        {
            Vector3d desiredThrustVector = -vesselState.velocityVesselSurfaceUnit;

            Vector3d courseCorrection = ComputeCourseCorrection(false);
            double correctionAngle = courseCorrection.magnitude / (5.0 * vesselState.maxThrustAccel);
            correctionAngle = Math.Min(0.1, correctionAngle);
            desiredThrustVector = (desiredThrustVector + correctionAngle * courseCorrection.normalized).normalized;

            if (Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up) > 0
                     || Vector3d.Dot(vesselState.forward, desiredThrustVector) < 0.75)
            {
                s.mainThrottle = 0;
            }
            else
            {
                double controlledSpeed = vesselState.speedSurface * Math.Sign(Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.up)); //positive if we are ascending, negative if descending
                IDescentSpeedPolicy policy = PickDescentSpeedPolicy();
                double desiredSpeed = -policy.MaxAllowedSpeed(vesselState.CoM - mainBody.position, vesselState.velocityVesselSurface);
                double desiredSpeedAfterDt = -predictions.descentSpeedPolicy.MaxAllowedSpeed(vesselState.CoM + vesselState.velocityVesselOrbit * vesselState.deltaT - mainBody.position, vesselState.velocityVesselSurface);
                double minAccel = -vesselState.localg * Math.Abs(Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.up));
                double maxAccel = vesselState.maxThrustAccel * Vector3d.Dot(vesselState.forward, -vesselState.velocityVesselSurfaceUnit) - vesselState.localg * Math.Abs(Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.up));
                double speedCorrectionTimeConstant = 0.3;
                double speedError = desiredSpeed - controlledSpeed;
                double desiredAccel = speedError / speedCorrectionTimeConstant + (desiredSpeedAfterDt - desiredSpeed) / vesselState.deltaT;
                if (maxAccel - minAccel > 0) s.mainThrottle = Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0F, 1.0F);
                else s.mainThrottle = 0;
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
        
        }

        public void DriveUntargetedLanding(FlightCtrlState s)
        {
            if (vessel.LandedOrSplashed)
            {
                s.mainThrottle = 0;
                enabled = false;
                return;
            }

            double minalt = Math.Min(vesselState.altitudeBottom, Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue));

            if (!deployedGears && (minalt < 1000)) DeployLandingGears();

            if (Vector3d.Angle(vesselState.velocityVesselSurface, vesselState.up) < 80)
            {
                //if we have positive vertical velocity, point up and don't thrust:
                core.attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, null);
                core.thrust.tmode = MechJebModuleThrustController.TMode.DIRECT;
                core.thrust.trans_spd_act = 0;
            }
            else if (Vector3d.Angle(vesselState.forward, -vesselState.velocityVesselSurface) > 20)
            {
                //if we're not facing approximately retrograde, turn to point retrograde and don't thrust:
                core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                core.thrust.tmode = MechJebModuleThrustController.TMode.DIRECT;
                core.thrust.trans_spd_act = 0;
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

                core.thrust.trans_spd_act = (float)Math.Sqrt((vesselState.maxThrustAccel - vesselState.gravityForce.magnitude) * 2 * minalt) * 0.90F;
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
        }


        IDescentSpeedPolicy PickDescentSpeedPolicy()
        {
            double terrainRadius = 0;
            if (predictions.result != null && predictions.result.outcome == ReentrySimulation.Outcome.LANDED)
            {
                terrainRadius = mainBody.Radius + MuUtils.TerrainAltitude(mainBody, predictions.result.endPosition.latitude, predictions.result.endPosition.longitude);
            }
            return new SafeDescentSpeedPolicy(terrainRadius, mainBody.GeeASL * 9.81, vesselState.maxThrustAccel);
        }

        //deploy landing gears:
        void DeployLandingGears()
        {
            vessel.rootPart.SendEvent("LowerLeg");
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
            if (Vector3d.Dot(pos, vel) > 0) return double.MaxValue;

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
