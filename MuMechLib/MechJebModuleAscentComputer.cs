using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //Todo: reimplement measurement of LPA and launch to rendezvous
    //      implement launch-to-plane
    public class MechJebModuleAscentComputer : ComputerModule
    {
        public MechJebModuleAscentComputer(MechJebCore core) : base(core) { }


        ////////////////////////////////////
        // GLOBAL DATA /////////////////////
        ////////////////////////////////////

        //autopilot modes
        public enum AscentMode { PRELAUNCH, COUNTING_DOWN, VERTICAL_ASCENT, GRAVITY_TURN, COAST_TO_APOAPSIS, CIRCULARIZE, DISENGAGED };
        String[] modeStrings = new String[] { "Awaiting liftoff", "Counting down", "Vertical ascent", "Gravity turn", "Coasting to apoapsis", "Circularizing", "Disengaged" };
        public AscentMode mode = AscentMode.DISENGAGED;

        //input parameters:
        double gravityTurnStartAltitude = 10000.0;
        double gravityTurnEndAltitude = 70000.0;
        double gravityTurnEndPitch = 0.0;
        double gravityTurnShapeExponent = 0.4;
        double desiredOrbitAltitude = 100000.0;
        double desiredInclination = 0.0;
        bool autoWarpToApoapsis = true;
        bool seizeThrottle = true;


        ////////////////////////////////////////
        // ASCENT PATH /////////////////////////
        ////////////////////////////////////////

        //controls the ascent path
        private double FlightPathAngle(double altitude)
        {
            if (altitude < gravityTurnStartAltitude) return 90.0;

            if (altitude > gravityTurnEndAltitude) return gravityTurnEndPitch;

            return Mathf.Clamp((float)(90.0 - Math.Pow((altitude - gravityTurnStartAltitude) / (gravityTurnEndAltitude - gravityTurnStartAltitude), gravityTurnShapeExponent) * (90.0 - gravityTurnEndPitch)), 0.01F, 89.99F);
        }



        ///////////////////////////////////////
        // FLYING THE ROCKET //////////////////
        ///////////////////////////////////////

        //called every frame or so; this is where we manipulate the flight controls
        public override void drive(FlightCtrlState s)
        {
            if (mode == AscentMode.DISENGAGED) return;

            //detect liftoff:
            if (mode == AscentMode.PRELAUNCH && Staging.CurrentStage <= Staging.lastStage) mode = AscentMode.VERTICAL_ASCENT;

            switch (mode)
            {
                case AscentMode.VERTICAL_ASCENT:
                    driveVerticalAscent(s);
                    break;

                case AscentMode.GRAVITY_TURN:
                    driveGravityTurn(s);
                    break;

                case AscentMode.COAST_TO_APOAPSIS:
                    driveCoastToApoapsis(s);
                    break;

                case AscentMode.CIRCULARIZE:
                    driveCircularizationBurn(s);
                    break;
            }

            if (seizeThrottle)
            {
                core.thrust.limitToPreventOverheats = true;
                core.thrust.limitToTerminalVelocity = true;
            }
        }




        ///////////////////////////////////////
        // VERTICAL ASCENT ////////////////////
        ///////////////////////////////////////

        void driveVerticalAscent(FlightCtrlState s)
        {
            //during the vertical ascent we just thrust straight up at max throttle
            if (seizeThrottle) s.mainThrottle = 1.0F;
            if (vesselState.altitudeASL > gravityTurnStartAltitude) mode = AscentMode.GRAVITY_TURN;
            if (seizeThrottle && part.vessel.orbit.ApA > desiredOrbitAltitude)
            {
                //lastAccelerationTime = vesselState.time;
                mode = AscentMode.COAST_TO_APOAPSIS;
            }

            core.attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, this);
        }

        ///////////////////////////////////////
        // GRAVITY TURN ///////////////////////
        ///////////////////////////////////////

        //gives a throttle setting that reduces as we approach the desired apoapsis
        //so that we can precisely match the desired apoapsis instead of overshooting it
        float throttleToRaiseApoapsis(double currentApoapsisRadius, double finalApoapsisRadius)
        {
            if (currentApoapsisRadius > finalApoapsisRadius + 5.0) return 0.0F;
            else if (part.vessel.orbit.ApA < part.vessel.mainBody.maxAtmosphereAltitude) return 1.0F; //throttle hard to escape atmosphere

            double GM = part.vessel.mainBody.gravParameter;

            double potentialDifference = GM / currentApoapsisRadius - GM / finalApoapsisRadius;
            double finalKineticEnergy = 0.5 * vesselState.speedOrbital * vesselState.speedOrbital + potentialDifference;
            double speedDifference = Math.Sqrt(2 * finalKineticEnergy) - vesselState.speedOrbital;

            if (speedDifference > 100) return 1.0F;

            return Mathf.Clamp((float)(speedDifference / (1.0 * vesselState.maxThrustAccel)), 0.05F, 1.0F); //1 second time constant for throttle reduction
        }


        void driveGravityTurn(FlightCtrlState s)
        {
            //stop the gravity turn when our apoapsis reaches the desired altitude
            if (seizeThrottle && part.vessel.orbit.ApA > desiredOrbitAltitude)
            {
                mode = AscentMode.COAST_TO_APOAPSIS;
                return;
            }

            //if we've fallen below the turn start altitude, go back to vertical ascent
            if (vesselState.altitudeASL < gravityTurnStartAltitude)
            {
                mode = AscentMode.VERTICAL_ASCENT;
                return;
            }

            if (seizeThrottle)
            {
                s.mainThrottle = throttleToRaiseApoapsis(part.vessel.orbit.ApR, desiredOrbitAltitude + part.vessel.mainBody.Radius);
                if (s.mainThrottle < 1.0F)
                {
                    //when we are bringing down the throttle to make the apoapsis accurate, we're liable to point in weird
                    //directions because thrust goes down and so "difficulty" goes up. so just burn prograde
                    core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.ORBIT, this);
                    return;
                }
            }


            //transition gradually from the rotating to the non-rotating reference frame. this calculation ensures that
            //when our maximum possible apoapsis, given our orbital energy, is desiredOrbitalRadius, then we are
            //fully in the non-rotating reference frame and thus doing the correct calculations to get the right inclination
            double GM = part.vessel.mainBody.gravParameter;
            double potentialDifferenceWithApoapsis = GM / vesselState.radius - GM / (part.vessel.mainBody.Radius + desiredOrbitAltitude);
            double verticalSpeedForDesiredApoapsis = Math.Sqrt(2 * potentialDifferenceWithApoapsis);
            double referenceFrameBlend = Mathf.Clamp((float)(vesselState.speedOrbital / verticalSpeedForDesiredApoapsis), 0.0F, 1.0F);

            Vector3d actualVelocityUnit = ((1 - referenceFrameBlend) * vesselState.velocityVesselSurfaceUnit
                                               + referenceFrameBlend * vesselState.velocityVesselOrbitUnit).normalized;

            double desiredHeading = OrbitalManeuverCalculator.HeadingForInclination(desiredInclination, vesselState.latitude);
            Vector3d desiredHeadingVector = Math.Sin(desiredHeading) * vesselState.east + Math.Cos(desiredHeading) * vesselState.north;
            double desiredFlightPathAngle = FlightPathAngle(vesselState.altitudeASL);

            Vector3d desiredVelocityUnit = Math.Cos(desiredFlightPathAngle * Math.PI / 180) * desiredHeadingVector
                                         + Math.Sin(desiredFlightPathAngle * Math.PI / 180) * vesselState.up;

            Vector3d velocityError = (desiredVelocityUnit - actualVelocityUnit);

            const double Kp = 5.0; //control gain

            //"difficulty" scales the controller gain to account for the difficulty of changing a large velocity vector given our current thrust
            double difficulty = vesselState.velocityVesselSurface.magnitude / (50 + 10 * vesselState.ThrustAccel(s.mainThrottle));
            if (difficulty > 5) difficulty = 5;

            if (vesselState.maxThrustAccel == 0) difficulty = 1.0; //so we don't freak out over having no thrust between stages

            Vector3d desiredThrustVector = desiredVelocityUnit;
            Vector3d steerOffset = Kp * difficulty * velocityError;

            //limit the amount of steering to 10 degrees. Furthemore, never steer to a FPA of > 90 (that is, never lean backward)
            double maxOffset = 10 * Math.PI / 180;
            if (desiredFlightPathAngle > 80) maxOffset = (90 - desiredFlightPathAngle) * Math.PI / 180;
            if (steerOffset.magnitude > maxOffset) steerOffset = maxOffset * steerOffset.normalized;

            desiredThrustVector += steerOffset;
            desiredThrustVector = desiredThrustVector.normalized;

            core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, this);
        }

        ///////////////////////////////////////
        // COAST AND CIRCULARIZATION //////////
        ///////////////////////////////////////

        //move to a CelestialBody extensions class?
        double gAtRadius(double radius)
        {
            return FlightGlobals.getGeeForceAtPosition(part.vessel.mainBody.position + radius * vesselState.up).magnitude;
        }

        double thrustPitchForZeroVerticalAcc(double orbitRadius, double currentHorizontalSpeed, double thrustAcceleration, double currentVerticalSpeed)
        {
            double centrifugalAcceleration = currentHorizontalSpeed * currentHorizontalSpeed / orbitRadius;
            double gravityAcceleration = gAtRadius(orbitRadius);
            double downwardAcceleration = gravityAcceleration - centrifugalAcceleration;
            downwardAcceleration -= currentVerticalSpeed / 1.0; //1.0 = 1 second time constant for killing existing vertical speed
            if (Math.Abs(downwardAcceleration) > thrustAcceleration) return 0; //we're screwed, just burn horizontal
            return Math.Asin(downwardAcceleration / thrustAcceleration);
        }

        double expectedSpeedAtApoapsis()
        {
            // (1/2)(orbitSpeed)^2 - g(r)*r = constant
            double currentTotalEnergy = 0.5 * vesselState.speedOrbital * vesselState.speedOrbital - vesselState.localg * vesselState.radius;
            double gAtApoapsis = gAtRadius(part.vessel.orbit.ApR);
            double potentialEnergyAtApoapsis = -gAtApoapsis * part.vessel.orbit.ApR;
            double kineticEnergyAtApoapsis = currentTotalEnergy - potentialEnergyAtApoapsis;
            return Math.Sqrt(2 * kineticEnergyAtApoapsis);
        }

        double circularizationBurnThrottle(double currentSpeed, double finalSpeed)
        {
            if (vesselState.maxThrustAccel == 0)
            {
                //then we are powerless
                return 0;
            }
            double desiredThrustAccel = (finalSpeed - currentSpeed) / 1.0; //1 second time constant for throttling down
            float throttleCommand = (float)((desiredThrustAccel - vesselState.minThrustAccel) / (vesselState.maxThrustAccel - vesselState.minThrustAccel));
            return Mathf.Clamp(throttleCommand, 0.01F, 1.0F);
        }

        void driveCoastToApoapsis(FlightCtrlState s)
        {
            s.mainThrottle = 0.0F;

            //if we have started to descend, i.e. reached apoapsis, circularize
            if (Vector3d.Dot(vesselState.velocityVesselOrbit, vesselState.up) < 0)
            {
                mode = AscentMode.CIRCULARIZE;
                core.warp.MinimumWarp();
                return;
            }

            //if our apoapsis has fallen too far, resume the gravity turn
            if (part.vessel.orbit.ApA < desiredOrbitAltitude - 1000.0)
            {
                mode = AscentMode.GRAVITY_TURN;
                core.warp.MinimumWarp();
                return;
            }

            //if our apoapsis has fallen below the target, raise it back up
            if (part.vessel.orbit.ApA < desiredOrbitAltitude - 10)
            {
                if (MuUtils.PhysicsRunning())
                {
                    //if physics is running, steer prograde and throttle up
                    core.attitude.attitudeTo(Vector3.forward, AttitudeReference.ORBIT, this);

                    if (core.attitude.attitudeAngleFromTarget() < 15)
                    {
                        s.mainThrottle = throttleToRaiseApoapsis(part.vessel.orbit.ApR, desiredOrbitAltitude + part.vessel.mainBody.Radius);
                    }
                    else
                    {
                        s.mainThrottle = 0.0F;
                    }
                }
                else
                {
                    //if physics is not running, switch to physics warp x2 so we can throttle up
                    if (autoWarpToApoapsis) core.warp.WarpPhysicsAtRate(2);
                    else core.warp.MinimumWarp();
                }
                return;
            }
            
            //apoapsis is high enough

            if (autoWarpToApoapsis)
            {
                //If we're allowed to autowarp, do so
                //This if statement tries to squash a bug where the AP increases warp just as it passes Ap,
                //which slightly messes up the start of the circularization burn
                if (part.vessel.orbit.timeToAp > 10 && (part.vessel.orbit.period - part.vessel.orbit.timeToAp) > 10)
                {
                    core.warp.WarpToUT(vesselState.time + part.vessel.orbit.timeToAp - 30);
                }
            }

            //If we are out of the atmosphere and have a high enough apoapsis and aren't near apoapsis, play dead to 
            //prevent acceleration from interfering with time warp. Otherwise point in the direction that we will
            //want to point at the start of the circularization burn
            if (autoWarpToApoapsis
                && vesselState.altitudeASL > part.vessel.mainBody.maxAtmosphereAltitude
                && part.vessel.orbit.timeToAp > 30)
            {
                //don't steer
                core.attitude.attitudeDeactivate(this);
            }
            else
            {
                double expectedApoapsisThrottle = circularizationBurnThrottle(expectedSpeedAtApoapsis(), OrbitalManeuverCalculator.CircularOrbitSpeed(part.vessel.mainBody, part.vessel.orbit.ApR));
                double desiredThrustPitch = thrustPitchForZeroVerticalAcc(part.vessel.orbit.ApR, expectedSpeedAtApoapsis(),
                                                                          vesselState.ThrustAccel(expectedApoapsisThrottle), 0);
                Vector3d groundHeading = Vector3d.Exclude(vesselState.up, part.vessel.orbit.GetVel()).normalized;
                Vector3d desiredThrustVector = (Math.Cos(desiredThrustPitch) * groundHeading + Math.Sin(desiredThrustPitch) * vesselState.up);
                core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, this);
                return;
            }
        }



        void driveCircularizationBurn(FlightCtrlState s)
        {
            if (part.vessel.orbit.ApA < desiredOrbitAltitude - 1000.0)
            {
                mode = AscentMode.GRAVITY_TURN;
                core.attitude.attitudeTo(Vector3.forward, AttitudeReference.ORBIT, this);
                return;
            }

            double orbitalRadius = (vesselState.CoM - part.vessel.mainBody.position).magnitude;
            double circularSpeed = Math.Sqrt(orbitalRadius * vesselState.localg);

            //we start the circularization burn at apoapsis, and the orbit is about as circular as it is going to get when the 
            //periapsis has moved closer to us than the apoapsis
            if (Math.Min(part.vessel.orbit.timeToPe, part.vessel.orbit.period - part.vessel.orbit.timeToPe) <
                Math.Min(part.vessel.orbit.timeToAp, part.vessel.orbit.period - part.vessel.orbit.timeToAp)
                || vesselState.speedOrbital > circularSpeed + 1.0) //makes sure we don't keep burning forever if we are somehow way off course
            {
                s.mainThrottle = 0.0F;
                mode = AscentMode.DISENGAGED;
                return;
            }

            //During circularization we think in the *non-rotating* frame, but allow ourselves to discuss centrifugal acceleration
            //as the tendency for the altitude to rise because of our large horizontal velocity.
            //To get a circular orbit we need to reach orbital horizontal speed while simultaneously getting zero vertical speed.
            //We compute the thrust vector needed to get zero vertical acceleration, taking into account gravity and centrifugal
            //acceleration. If we currently have a nonzero vertical velocity we then adjust the thrust vector to bring that to zero

            s.mainThrottle = (float)circularizationBurnThrottle(vesselState.speedOrbital, circularSpeed);

            double thrustAcceleration = vesselState.ThrustAccel(s.mainThrottle);

            Vector3d groundHeading = Vector3d.Exclude(vesselState.up, vesselState.velocityVesselOrbit).normalized;
            double horizontalSpeed = Vector3d.Dot(groundHeading, vesselState.velocityVesselOrbit);
            double verticalSpeed = Vector3d.Dot(vesselState.up, vesselState.velocityVesselOrbit);

            //test the following modification: pitch = 0 if throttle < 1
            double desiredThrustPitch = thrustPitchForZeroVerticalAcc(orbitalRadius, horizontalSpeed, thrustAcceleration, verticalSpeed);



            //Vector3d desiredThrustVector = Math.Cos(desiredThrustPitch) * groundHeading + Math.Sin(desiredThrustPitch) * vesselState.up;
            Vector3d desiredThrustVector = Math.Cos(desiredThrustPitch) * Vector3d.forward + Math.Sin(desiredThrustPitch) * Vector3d.up;
            core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.ORBIT_HORIZONTAL, this);
        }

    }
}
