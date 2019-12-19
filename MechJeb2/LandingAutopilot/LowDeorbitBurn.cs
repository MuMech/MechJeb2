using System;
using UnityEngine;
using KSP.Localization;

// FIXME: use a maneuver node

namespace MuMech
{
    namespace Landing
    {
        public class LowDeorbitBurn : AutopilotStep
        {
            bool deorbitBurnTriggered = false;
            double lowDeorbitBurnMaxThrottle = 0;

            bool lowDeorbitEndConditionSet = false;
            bool lowDeorbitEndOnLandingSiteNearer = false;

            const double lowDeorbitBurnTriggerFactor = 2;

            public LowDeorbitBurn(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (deorbitBurnTriggered && core.attitude.attitudeAngleFromTarget() < 5)
                {
                    core.thrust.targetThrottle = Mathf.Clamp01((float)lowDeorbitBurnMaxThrottle);
                }
                else
                {
                    core.thrust.targetThrottle = 0;
                }

                return this;
            }

            public override AutopilotStep OnFixedUpdate()
            {
                //Decide when we will start the deorbit burn:
                double stoppingDistance = Math.Pow(vesselState.speedSurfaceHorizontal, 2) / (2 * vesselState.limitedMaxThrustAccel);
                double triggerDistance = lowDeorbitBurnTriggerFactor * stoppingDistance;
                double heightAboveTarget = vesselState.altitudeASL - core.landing.DecelerationEndAltitude();
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

                if (deorbitBurnTriggered) status = Localizer.Format("#MechJeb_LandingGuidance_Status11");//"Executing low deorbit burn"
                else status = Localizer.Format("#MechJeb_LandingGuidance_Status12");//"Moving to low deorbit burn point"

                //Warp toward deorbit burn if it hasn't been triggerd yet:
                if (!deorbitBurnTriggered && core.node.autowarp && rangeToTarget > 2 * triggerDistance) core.warp.WarpRegularAtRate((float)(orbit.period / 6));
                if (rangeToTarget < triggerDistance && !MuUtils.PhysicsRunning()) core.warp.MinimumWarp();

                //By default, thrust straight back at max throttle
                Vector3d thrustDirection = -vesselState.surfaceVelocity.normalized;
                lowDeorbitBurnMaxThrottle = 1;

                //If we are burning, we watch the predicted landing site and switch to the braking
                //burn when the predicted landing site crosses the target. We also use the predictions
                //to steer the predicted landing site toward the target
                if (deorbitBurnTriggered && core.landing.PredictionReady)
                {
                    //angle slightly left or right to fix any cross-range error in the predicted landing site:
                    Vector3d horizontalToLandingSite = Vector3d.Exclude(vesselState.up, core.landing.LandingSite - vesselState.CoM).normalized;
                    Vector3d horizontalToTarget = Vector3d.Exclude(vesselState.up, core.target.GetPositionTargetPosition() - vesselState.CoM).normalized;
                    const double angleGain = 4;
                    Vector3d angleCorrection = angleGain * (horizontalToTarget - horizontalToLandingSite);
                    if (angleCorrection.magnitude > 0.1) angleCorrection *= 0.1 / angleCorrection.magnitude;
                    thrustDirection = (thrustDirection + angleCorrection).normalized;

                    double rangeToLandingSite = Vector3d.Exclude(vesselState.up, core.landing.LandingSite - vesselState.CoM).magnitude;
                    double maxAllowedSpeed = core.landing.MaxAllowedSpeed();

                    if (!lowDeorbitEndConditionSet && Vector3d.Distance(core.landing.LandingSite, vesselState.CoM) < mainBody.Radius + vesselState.altitudeASL)
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
                                core.thrust.targetThrottle = 0;
                                return new DecelerationBurn(core);
                            }

                            double maxAllowedSpeedAfterDt = core.landing.MaxAllowedSpeedAfterDt(vesselState.deltaT);
                            double speedAfterDt = vesselState.speedSurface + vesselState.deltaT * Vector3d.Dot(vesselState.gravityForce, vesselState.surfaceVelocity.normalized);
                            double throttleToMaintainLandingSite;
                            if (vesselState.speedSurface < maxAllowedSpeed) throttleToMaintainLandingSite = 0;
                            else throttleToMaintainLandingSite = (speedAfterDt - maxAllowedSpeedAfterDt) / (vesselState.deltaT * vesselState.maxThrustAccel);

                            lowDeorbitBurnMaxThrottle = throttleToMaintainLandingSite + 1 * (rangeToLandingSite / rangeToTarget - 1) + 0.2;
                        }
                        else
                        {
                            if (lowDeorbitEndConditionSet && lowDeorbitEndOnLandingSiteNearer)
                            {
                                core.thrust.targetThrottle = 0;
                                return new DecelerationBurn(core);
                            }
                            else
                            {
                                lowDeorbitBurnMaxThrottle = 0;
                                status = Localizer.Format("#MechJeb_LandingGuidance_Status13");//"Deorbit burn complete: waiting for the right moment to start braking"
                            }
                        }
                    }
                }

                core.attitude.attitudeTo(thrustDirection, AttitudeReference.INERTIAL, core.landing);

                return this;
            }
        }
    }
}
