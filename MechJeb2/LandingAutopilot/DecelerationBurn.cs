using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    namespace Landing
    {
        public class DecelerationBurn : AutopilotStep
        {
            public DecelerationBurn(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep OnFixedUpdate()
            {
                if (vesselState.altitudeASL < core.landing.DecelerationEndAltitude() + 5)
                {
                    core.warp.MinimumWarp();

                    if (core.landing.UseAtmosphereToBrake())
                        return new FinalDescent(core);
                    else
                        return new KillHorizontalVelocity(core);

                }

                double decelerationStartTime = (core.landing.prediction.trajectory.Any() ? core.landing.prediction.trajectory.First().UT : vesselState.time);
                if (decelerationStartTime - vesselState.time > 5)
                {
                    core.thrust.targetThrottle = 0;

                    status = Localizer.Format("#MechJeb_LandingGuidance_Status4");//"Warping to start of braking burn."

                    //warp to deceleration start
                    Vector3d decelerationStartAttitude = -orbit.SwappedOrbitalVelocityAtUT(decelerationStartTime);
                    decelerationStartAttitude += mainBody.getRFrmVel(orbit.SwappedAbsolutePositionAtUT(decelerationStartTime));
                    decelerationStartAttitude = decelerationStartAttitude.normalized;
                    core.attitude.attitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, core.landing);
                    bool warpReady = core.attitude.attitudeAngleFromTarget() < 5;

                    if (warpReady && core.node.autowarp)
                        core.warp.WarpToUT(decelerationStartTime - 5);
                    else if (!MuUtils.PhysicsRunning())
                        core.warp.MinimumWarp();
                    return this;
                }

                Vector3d desiredThrustVector = -vesselState.surfaceVelocity.normalized;

                Vector3d courseCorrection = core.landing.ComputeCourseCorrection(false);
                double correctionAngle = courseCorrection.magnitude / (2.0 * vesselState.limitedMaxThrustAccel);
                correctionAngle = Math.Min(0.1, correctionAngle);
                desiredThrustVector = (desiredThrustVector + correctionAngle * courseCorrection.normalized).normalized;

                if (Vector3d.Dot(vesselState.surfaceVelocity, vesselState.up) > 0
                    || Vector3d.Dot(vesselState.forward, desiredThrustVector) < 0.75)
                {
                    core.thrust.targetThrottle = 0;
                    status = Localizer.Format("#MechJeb_LandingGuidance_Status5");//"Braking"
                }
                else
                {
                    double controlledSpeed = vesselState.speedSurface * Math.Sign(Vector3d.Dot(vesselState.surfaceVelocity, vesselState.up)); //positive if we are ascending, negative if descending
                    double desiredSpeed = -core.landing.MaxAllowedSpeed();
                    double desiredSpeedAfterDt = -core.landing.MaxAllowedSpeedAfterDt(vesselState.deltaT);
                    double minAccel = -vesselState.localg * Math.Abs(Vector3d.Dot(vesselState.surfaceVelocity.normalized, vesselState.up));
                    double maxAccel = vesselState.maxThrustAccel * Vector3d.Dot(vesselState.forward, -vesselState.surfaceVelocity.normalized) - vesselState.localg * Math.Abs(Vector3d.Dot(vesselState.surfaceVelocity.normalized, vesselState.up));
                    const double speedCorrectionTimeConstant = 0.3;
                    double speedError = desiredSpeed - controlledSpeed;
                    double desiredAccel = speedError / speedCorrectionTimeConstant + (desiredSpeedAfterDt - desiredSpeed) / vesselState.deltaT;
                    if (maxAccel - minAccel > 0) core.thrust.targetThrottle = Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0F, 1.0F);
                    else core.thrust.targetThrottle = 0;
                    status = Localizer.Format("#MechJeb_LandingGuidance_Status6", Math.Abs(desiredSpeed).ToString("F1"));//"Braking: target speed = " +  + " m/s"
                }

                core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, core.landing);

                return this;
            }
        }
    }
}
