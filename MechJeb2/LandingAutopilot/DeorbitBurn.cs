using System;
using UnityEngine;
using KSP.Localization;

// FIXME: use a maneuver node

namespace MuMech
{
    namespace Landing
    {
        public class DeorbitBurn : AutopilotStep
        {
            bool deorbitBurnTriggered = false;

            public DeorbitBurn(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (deorbitBurnTriggered && core.attitude.attitudeAngleFromTarget() < 5)
                    core.thrust.targetThrottle = 1.0F;
                else
                    core.thrust.targetThrottle = 0;

                return this;
            }

            public override AutopilotStep OnFixedUpdate()
            {
                //if we don't want to deorbit but we're already on a reentry trajectory, we can't wait until the ideal point 
                //in the orbit to deorbt; we already have deorbited.
                if (orbit.ApA < mainBody.RealMaxAtmosphereAltitude())
                {
                    core.thrust.targetThrottle = 0;
                    return new CourseCorrection(core);
                }

                //We aim for a trajectory that 
                // a) has the same vertical speed as our current trajectory
                // b) has a horizontal speed that will give it a periapsis of -10% of the body's radius
                // c) has a heading that points toward where the target will be at the end of free-fall, accounting for planetary rotation
                Vector3d horizontalDV = OrbitalManeuverCalculator.DeltaVToChangePeriapsis(orbit, vesselState.time, 0.9 * mainBody.Radius); //Imagine we are going to deorbit now. Find the burn that would lower our periapsis to -10% of the planet's radius
                Orbit forwardDeorbitTrajectory = orbit.PerturbedOrbit(vesselState.time, horizontalDV);                                     //Compute the orbit that would put us on
                double freefallTime = forwardDeorbitTrajectory.NextTimeOfRadius(vesselState.time, mainBody.Radius) - vesselState.time;     //Find how long that orbit would take to impact the ground
                double planetRotationDuringFreefall = 360 * freefallTime / mainBody.rotationPeriod;                                        //Find how many degrees the planet will rotate during that time
                Vector3d currentTargetRadialVector = mainBody.GetWorldSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0)-mainBody.position; //Find the current vector from the planet center to the target landing site
                Quaternion freefallPlanetRotation = Quaternion.AngleAxis((float)planetRotationDuringFreefall, mainBody.angularVelocity);   //Construct a quaternion representing the rotation of the planet found above
                Vector3d freefallEndTargetRadialVector = freefallPlanetRotation * currentTargetRadialVector;                               //Use this quaternion to find what the vector from the planet center to the target will be when we hit the ground
                Vector3d freefallEndTargetPosition = mainBody.position + freefallEndTargetRadialVector;                                    //Then find the actual position of the target at that time
                Vector3d freefallEndHorizontalToTarget = Vector3d.Exclude(vesselState.up, freefallEndTargetPosition - vesselState.CoM).normalized; //Find a horizontal unit vector that points toward where the target will be when we hit the ground
                Vector3d currentHorizontalVelocity = Vector3d.Exclude(vesselState.up, vesselState.orbitalVelocity); //Find our current horizontal velocity
                double finalHorizontalSpeed = (currentHorizontalVelocity + horizontalDV).magnitude;                     //Find the desired horizontal speed after the deorbit burn
                Vector3d finalHorizontalVelocity = finalHorizontalSpeed * freefallEndHorizontalToTarget;                //Combine the desired speed and direction to get the desired velocity after the deorbi burn

                //Compute the angle between the location of the target at the end of freefall and the normal to our orbit:
                Vector3d currentRadialVector = vesselState.CoM - mainBody.position;
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
                    core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, core.landing);

                    if (deltaV.magnitude < 2.0)
                    {
                        return new CourseCorrection(core);
                    }

                    status = Localizer.Format("#MechJeb_LandingGuidance_Status7");//"Doing high deorbit burn"
                }
                else
                {
                    core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT, core.landing);
                    if (core.node.autowarp) core.warp.WarpRegularAtRate((float)(orbit.period / 10));

                    status = Localizer.Format("#MechJeb_LandingGuidance_Status8");//"Moving to high deorbit burn point"
                }

                return this;
            }
        }
    }
}
