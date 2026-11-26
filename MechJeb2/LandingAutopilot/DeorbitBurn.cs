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
                if (_deorbitBurnTriggered && Core.Attitude.AttitudeAngleFromTarget() < 5)
                    Core.Thrust.TargetThrottle = 1.0F;
                else
                    Core.Thrust.ThrustOff();

                return this;
            }

            public override AutopilotStep OnFixedUpdate()
            {
                //if we don't want to deorbit but we're already on a reentry trajectory, we can't wait until the ideal point
                //in the orbit to deorbt; we already have deorbited.
                if (Orbit.ApA < MainBody.RealMaxAtmosphereAltitude())
                {
                    Core.Thrust.ThrustOff();
                    return new CourseCorrection(Core);
                }

                //We aim for a trajectory that
                // a) has the same vertical speed as our current trajectory
                // b) has a horizontal speed that will give it the entry periapsis (by default -10% of the body's radius)
                // c) has a heading that points toward where the target will be at the end of free-fall, accounting for planetary rotation
                Vector3d horizontalDV =
                    OrbitalManeuverCalculator.DeltaVToChangePeriapsis(Orbit, VesselState.Time,
                        Core.Landing.EntryPeriapsisRadius()); //Imagine we are going to deorbit now. Find the burn that would lower our periapsis to the entry periapsis
                Orbit forwardDeorbitTrajectory = Orbit.PerturbedOrbit(VesselState.Time, horizontalDV); //Compute the orbit that would put us on
                //Find how long that orbit would take to impact the ground; a periapsis above the ground (a shallow entry) never reaches it,
                //and the drag brings the vessel down near the periapsis
                double freefallTime =
                    (forwardDeorbitTrajectory.PeR < MainBody.Radius
                        ? forwardDeorbitTrajectory.NextTimeOfRadius(VesselState.Time, MainBody.Radius)
                        : forwardDeorbitTrajectory.NextPeriapsisTime(VesselState.Time)) -
                    VesselState.Time;
                double planetRotationDuringFreefall =
                    360 * freefallTime / MainBody.rotationPeriod; //Find how many degrees the planet will rotate during that time
                Vector3d currentTargetRadialVector = MainBody.GetWorldSurfacePosition(Core.Target.targetLatitude, Core.Target.targetLongitude, 0) -
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
                    Vector3d.Exclude(VesselState.Up, freefallEndTargetPosition - VesselState.CoM)
                       .normalized; //Find a horizontal unit vector that points toward where the target will be when we hit the ground
                var currentHorizontalVelocity = Vector3d.Exclude(VesselState.Up, VesselState.OrbitalVelocity); //Find our current horizontal velocity
                double finalHorizontalSpeed =
                    (currentHorizontalVelocity + horizontalDV).magnitude; //Find the desired horizontal speed after the deorbit burn

                //Compute the angle between the location of the target at the end of freefall and the normal to our orbit:
                Vector3d currentRadialVector = VesselState.CoM - MainBody.position;
                double targetAngleToOrbitNormal = Vector3d.Angle(Orbit.OrbitNormal(), freefallEndTargetRadialVector);
                targetAngleToOrbitNormal = Math.Min(targetAngleToOrbitNormal, 180 - targetAngleToOrbitNormal);

                //How far the new orbit carries the vessel (to the ground, or to the periapsis when that is above it: a shallow entry) and
                //how far ahead the target is, along the orbit
                Vector3d freefallEndRadialVector = forwardDeorbitTrajectory.WorldPositionAtUT(VesselState.Time + freefallTime) - MainBody.position;
                double travel = AngleAlongOrbit(currentRadialVector, freefallEndRadialVector, currentHorizontalVelocity);
                double ahead = AngleAlongOrbit(currentRadialVector, freefallEndTargetRadialVector, currentHorizontalVelocity);
                double off = (ahead - travel + 540) % 360 - 180;

                //Within 30 degrees of the antipode the horizontal direction to the target has almost no length and can point sideways:
                //steering along it changes the plane by tens of degrees. Keep the orbit's plane there; course correction takes the rest.
                bool nearAntipode = ahead > 150 && ahead < 210;
                Vector3d heading = nearAntipode ? currentHorizontalVelocity.normalized : freefallEndHorizontalToTarget;
                Vector3d finalHorizontalVelocity = finalHorizontalSpeed * heading; //The desired velocity after the deorbit burn

                double planeChangeAngle =
                    Vector3d.Angle(currentHorizontalVelocity, heading); //The plane change required to get onto the deorbit trajectory, in degrees

                //If the target is basically almost normal to our orbit, it doesn't matter when we deorbit; might as well do it now
                //Otherwise, wait until the target is as far ahead as the new orbit carries the vessel, within 15 degrees. The steep default
                //entry hits the ground about a quarter of an orbit after the burn.

                if (targetAngleToOrbitNormal < 10 || (Math.Abs(off) < 15 && planeChangeAngle < 90))
                {
                    _deorbitBurnTriggered = true;
                }

                if (_deorbitBurnTriggered)
                {
                    if (!MuUtils.PhysicsRunning()) { Core.Warp.MinimumWarp(); } //get out of warp

                    Vector3d deltaV = finalHorizontalVelocity - currentHorizontalVelocity;
                    Core.Attitude.AttitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, Core.Landing);

                    if (deltaV.magnitude < 2.0)
                    {
                        return new CourseCorrection(Core);
                    }

                    Status = Localizer.Format("#MechJeb_LandingGuidance_Status7"); //"Doing high deorbit burn"
                }
                else
                {
                    Core.Attitude.AttitudeTo(Vector3d.back, AttitudeReference.ORBIT, Core.Landing);
                    if (Core.Landing.Autowarp) Core.Warp.WarpRegularAtRate((float)(Orbit.period / 10));

                    Status = Localizer.Format("#MechJeb_LandingGuidance_Status8"); //"Moving to high deorbit burn point"
                }

                return this;
            }

            // the angle from one radial vector to another in the direction of travel, 0 to 360 degrees
            private static double AngleAlongOrbit(Vector3d from, Vector3d to, Vector3d velocity)
            {
                Vector3d normal = Vector3d.Cross(from, velocity).normalized;
                double angle = Math.Atan2(Vector3d.Dot(Vector3d.Cross(from.normalized, to.normalized), normal), Vector3d.Dot(from.normalized, to.normalized)) *
                    UtilMath.Rad2Deg;
                return angle < 0 ? angle + 360 : angle;
            }
        }
    }
}
