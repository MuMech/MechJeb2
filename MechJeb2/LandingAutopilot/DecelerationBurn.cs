using System;
using System.Linq;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    namespace Landing
    {
        public class DecelerationBurn : AutopilotStep
        {
            private bool _decelerationBurnTriggered;
            public DecelerationBurn(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep OnFixedUpdate()
            {
                if (VesselState.altitudeASL < Core.Landing.DecelerationEndAltitude() + 5)
                {
                    Core.Warp.MinimumWarp();

                    if (Core.Landing.UseAtmosphereToBrake())
                        return new FinalDescent(Core);
                    return new KillHorizontalVelocity(Core);
                }

                double decelerationStartTime =
                    Core.Landing.Prediction.Trajectory.Any() ? Core.Landing.Prediction.Trajectory.First().UT : VesselState.time;
                if (decelerationStartTime - VesselState.time > 5 && !_decelerationBurnTriggered)
                {
                    Core.Thrust.ThrustOff();

                    Status = Localizer.Format("#MechJeb_LandingGuidance_Status4"); //"Warping to start of braking burn."

                    //warp to deceleration start
                    Vector3d decelerationStartAttitude = -Orbit.WorldOrbitalVelocityAtUT(decelerationStartTime);
                    decelerationStartAttitude += MainBody.getRFrmVel(Orbit.WorldPositionAtUT(decelerationStartTime));
                    decelerationStartAttitude = decelerationStartAttitude.normalized;
                    Core.Attitude.attitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, Core.Landing);
                    bool warpReady = Core.Attitude.attitudeAngleFromTarget() < 5 && Core.vessel.angularVelocity.magnitude < 0.001;

                    if (warpReady && Core.Node.Autowarp)
                        Core.Warp.WarpToUT(decelerationStartTime - 5);
                    
                    else if (!MuUtils.PhysicsRunning())
                        Core.Warp.MinimumWarp();
                    return this;
                }

                if (!_decelerationBurnTriggered)
                    _decelerationBurnTriggered = true;

                Vector3d desiredThrustVector = -VesselState.surfaceVelocity.normalized;

                Vector3d courseCorrection = Core.Landing.ComputeCourseCorrection(false);
                double correctionAngle = courseCorrection.magnitude / (2.0 * VesselState.limitedMaxThrustAccel);
                correctionAngle     = Math.Min(0.1, correctionAngle);
                desiredThrustVector = (desiredThrustVector + correctionAngle * courseCorrection.normalized).normalized;

                if (Vector3d.Dot(VesselState.surfaceVelocity, VesselState.up) > 0
                    || Vector3d.Dot(VesselState.forward, desiredThrustVector) < 0.75)
                {
                    Core.Thrust.RequestActiveThrottle(0.0f);
                    Status                     = Localizer.Format("#MechJeb_LandingGuidance_Status5"); //"Braking"
                }
                else
                {
                    double controlledSpeed =
                        VesselState.speedSurface *
                        Math.Sign(Vector3d.Dot(VesselState.surfaceVelocity, VesselState.up)); //positive if we are ascending, negative if descending
                    double desiredSpeed = -Core.Landing.MaxAllowedSpeed();
                    double desiredSpeedAfterDt = -Core.Landing.MaxAllowedSpeedAfterDt(VesselState.deltaT);
                    double minAccel = -VesselState.localg * Math.Abs(Vector3d.Dot(VesselState.surfaceVelocity.normalized, VesselState.up));
                    double maxAccel = VesselState.maxThrustAccel * Vector3d.Dot(VesselState.forward, -VesselState.surfaceVelocity.normalized) -
                                      VesselState.localg * Math.Abs(Vector3d.Dot(VesselState.surfaceVelocity.normalized, VesselState.up));
                    const double SPEED_CORRECTION_TIME_CONSTANT = 0.3;
                    double speedError = desiredSpeed - controlledSpeed;
                    double desiredAccel = speedError / SPEED_CORRECTION_TIME_CONSTANT + (desiredSpeedAfterDt - desiredSpeed) / VesselState.deltaT;
                    if (maxAccel - minAccel > 0)
                        Core.Thrust.RequestActiveThrottle(Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0f, 1.0f));
                    else Core.Thrust.RequestActiveThrottle(0);
                    Status = Localizer.Format("#MechJeb_LandingGuidance_Status6",
                        desiredSpeed >= double.MaxValue ? "∞" : Math.Abs(desiredSpeed).ToString("F1")); //"Braking: target speed = " +  + " m/s"
                }

                Core.Attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, Core.Landing);

                return this;
            }
        }
    }
}
