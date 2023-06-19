using KSP.Localization;
using UnityEngine;

// FIXME: use a maneuver node

namespace MuMech
{
    namespace Landing
    {
        public class PlaneChange : AutopilotStep
        {
            private bool   _planeChangeTriggered;
            private double _planeChangeDVLeft;

            public PlaneChange(MechJebCore core) : base(core)
            {
            }

            //Could make this an iterative procedure for improved accuracy
            private Vector3d ComputePlaneChange()
            {
                Vector3d targetRadialVector =
                    Core.vessel.mainBody.GetWorldSurfacePosition(Core.Target.targetLatitude, Core.Target.targetLongitude, 0) - MainBody.position;
                Vector3d currentRadialVector = Core.VesselState.CoM - Core.vessel.mainBody.position;
                double angleToTarget = Vector3d.Angle(targetRadialVector, currentRadialVector);
                //this calculation seems like it might be be working right:
                double timeToTarget = Orbit.TimeOfTrueAnomaly(Core.vessel.orbit.trueAnomaly * UtilMath.Rad2Deg + angleToTarget, VesselState.time) -
                                      VesselState.time;
                double planetRotationAngle = 360 * timeToTarget / MainBody.rotationPeriod;
                var planetRotation = Quaternion.AngleAxis((float)planetRotationAngle, MainBody.angularVelocity);
                Vector3d targetRadialVectorOnFlyover = planetRotation * targetRadialVector;
                Vector3d horizontalToTarget = Vector3d.Exclude(VesselState.up, targetRadialVectorOnFlyover - currentRadialVector).normalized;
                return horizontalToTarget;
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (_planeChangeTriggered && Core.Attitude.attitudeAngleFromTarget() < 2)
                {
                    Core.Thrust.targetThrottle = Mathf.Clamp01((float)(_planeChangeDVLeft / (2 * Core.VesselState.maxThrustAccel)));
                }
                else
                {
                    Core.Thrust.targetThrottle = 0;
                }

                return this;
            }

            public override AutopilotStep OnFixedUpdate()
            {
                Vector3d targetRadialVector = MainBody.GetWorldSurfacePosition(Core.Target.targetLatitude, Core.Target.targetLongitude, 0) -
                                              MainBody.position;
                Vector3d currentRadialVector = VesselState.CoM - MainBody.position;
                double angleToTarget = Vector3d.Angle(targetRadialVector, currentRadialVector);
                bool approaching = Vector3d.Dot(targetRadialVector - currentRadialVector, VesselState.orbitalVelocity) > 0;

                if (!_planeChangeTriggered && approaching && angleToTarget > 80 && angleToTarget < 90)
                {
                    if (!MuUtils.PhysicsRunning()) Core.Warp.MinimumWarp(true);
                    _planeChangeTriggered = true;
                }

                if (_planeChangeTriggered)
                {
                    Vector3d horizontalToTarget = ComputePlaneChange();
                    Vector3d finalVelocity = Quaternion.FromToRotation(VesselState.horizontalOrbit, horizontalToTarget) * VesselState.orbitalVelocity;

                    Vector3d deltaV = finalVelocity - VesselState.orbitalVelocity;
                    //burn normal+ or normal- to avoid dropping the Pe:
                    var burnDir = Vector3d.Exclude(VesselState.up, Vector3d.Exclude(VesselState.orbitalVelocity, deltaV));
                    _planeChangeDVLeft = UtilMath.Deg2Rad * Vector3d.Angle(finalVelocity, VesselState.orbitalVelocity) *
                                        VesselState.speedOrbitHorizontal;
                    Core.Attitude.attitudeTo(burnDir, AttitudeReference.INERTIAL, Core.Landing);
                    Status = Localizer.Format("#MechJeb_LandingGuidance_Status14",
                        _planeChangeDVLeft.ToString("F0")); //"Executing low orbit plane change of about " +  + " m/s"

                    if (_planeChangeDVLeft < 0.1F)
                    {
                        return new LowDeorbitBurn(Core);
                    }
                }
                else
                {
                    if (Core.Node.autowarp) Core.Warp.WarpRegularAtRate((float)(Orbit.period / 6));
                    Status = Localizer.Format("#MechJeb_LandingGuidance_Status15"); //"Moving to low orbit plane change burn point"
                }

                return this;
            }
        }
    }
}
