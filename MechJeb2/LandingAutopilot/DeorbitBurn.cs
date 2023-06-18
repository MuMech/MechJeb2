using System;
using KSP.Localization;
using UnityEngine;

// FIXME: use a maneuver node

namespace MuMech
{
    namespace Landing
    {
        public class DeorbitBurn : AutopilotStep
        {
            private bool _deorbitBurnTriggered;

            public DeorbitBurn(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (_deorbitBurnTriggered && Core.attitude.attitudeAngleFromTarget() < 5)
                    Core.thrust.targetThrottle = 1.0F;
                else
                    Core.thrust.targetThrottle = 0;

                return this;
            }

            public override AutopilotStep OnFixedUpdate()
            {
                //if we don't want to deorbit but we're already on a reentry trajectory, we can't wait until the ideal point
                //in the orbit to deorbt; we already have deorbited.
                if (Orbit.ApA < MainBody.RealMaxAtmosphereAltitude())
                {
                    Core.thrust.targetThrottle = 0;
                    return new CourseCorrection(Core);
                }

                //We aim for a trajectory that
                // a) has the same vertical speed as our current trajectory
                // b) has a horizontal speed that will give it a periapsis of -10% of the body's radius
                // c) has a heading that points toward where the target will be at the end of free-fall, accounting for planetary rotation
                Vector3d horizontalDV =
                    OrbitalManeuverCalculator.DeltaVToChangePeriapsis(Orbit, VesselState.time,
                        0.9 * MainBody
                            .Radius); //Imagine we are going to deorbit now. Find the burn that would lower our periapsis to -10% of the planet's radius
                Orbit forwardDeorbitTrajectory = Orbit.PerturbedOrbit(VesselState.time, horizontalDV); //Compute the orbit that would put us on
                double freefallTime =
                    forwardDeorbitTrajectory.NextTimeOfRadius(VesselState.time, MainBody.Radius) -
                    VesselState.time; //Find how long that orbit would take to impact the ground
                double planetRotationDuringFreefall =
                    360 * freefallTime / MainBody.rotationPeriod; //Find how many degrees the planet will rotate during that time
                Vector3d currentTargetRadialVector = MainBody.GetWorldSurfacePosition(Core.target.targetLatitude, Core.target.targetLongitude, 0) -
                                                     MainBody.position; //Find the current vector from the planet center to the target landing site
                var freefallPlanetRotation =
                    Quaternion.AngleAxis((float)planetRotationDuringFreefall,
                        MainBody.angularVelocity); //Construct a quaternion representing the rotation of the planet found above
                Vector3d
                    freefallEndTargetRadialVector =
                        freefallPlanetRotation *
                        currentTargetRadialVector; //Use this quaternion to find what the vector from the planet center to the target will be when we hit the ground
                Vector3d freefallEndTargetPosition =
                    MainBody.position + freefallEndTargetRadialVector; //Then find the actual position of the target at that time
                Vector3d freefallEndHorizontalToTarget =
                    Vector3d.Exclude(VesselState.up, freefallEndTargetPosition - VesselState.CoM)
                        .normalized; //Find a horizontal unit vector that points toward where the target will be when we hit the ground
                var currentHorizontalVelocity = Vector3d.Exclude(VesselState.up, VesselState.orbitalVelocity); //Find our current horizontal velocity
                double finalHorizontalSpeed =
                    (currentHorizontalVelocity + horizontalDV).magnitude; //Find the desired horizontal speed after the deorbit burn
                Vector3d
                    finalHorizontalVelocity =
                        finalHorizontalSpeed *
                        freefallEndHorizontalToTarget; //Combine the desired speed and direction to get the desired velocity after the deorbi burn

                //Compute the angle between the location of the target at the end of freefall and the normal to our orbit:
                Vector3d currentRadialVector = VesselState.CoM - MainBody.position;
                double targetAngleToOrbitNormal = Vector3d.Angle(Orbit.OrbitNormal(), freefallEndTargetRadialVector);
                targetAngleToOrbitNormal = Math.Min(targetAngleToOrbitNormal, 180 - targetAngleToOrbitNormal);

                double targetAheadAngle =
                    Vector3d.Angle(currentRadialVector, freefallEndTargetRadialVector); //How far ahead the target is, in degrees
                double planeChangeAngle =
                    Vector3d.Angle(currentHorizontalVelocity,
                        freefallEndHorizontalToTarget); //The plane change required to get onto the deorbit trajectory, in degrees

                //If the target is basically almost normal to our orbit, it doesn't matter when we deorbit; might as well do it now
                //Otherwise, wait until the target is ahead
                if (targetAngleToOrbitNormal < 10
                    || (targetAheadAngle < 90 && targetAheadAngle > 60 && planeChangeAngle < 90))
                {
                    _deorbitBurnTriggered = true;
                }

                if (_deorbitBurnTriggered)
                {
                    if (!MuUtils.PhysicsRunning()) { Core.warp.MinimumWarp(); } //get out of warp

                    Vector3d deltaV = finalHorizontalVelocity - currentHorizontalVelocity;
                    Core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, Core.landing);

                    if (deltaV.magnitude < 2.0)
                    {
                        return new CourseCorrection(Core);
                    }

                    Status = Localizer.Format("#MechJeb_LandingGuidance_Status7"); //"Doing high deorbit burn"
                }
                else
                {
                    Core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT, Core.landing);
                    if (Core.node.autowarp) Core.warp.WarpRegularAtRate((float)(Orbit.period / 10));

                    Status = Localizer.Format("#MechJeb_LandingGuidance_Status8"); //"Moving to high deorbit burn point"
                }

                return this;
            }
        }
    }
}
