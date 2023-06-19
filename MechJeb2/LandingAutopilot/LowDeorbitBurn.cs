using System;
using KSP.Localization;
using UnityEngine;

// FIXME: use a maneuver node

namespace MuMech
{
    namespace Landing
    {
        public class LowDeorbitBurn : AutopilotStep
        {
            private bool   _deorbitBurnTriggered;
            private double _lowDeorbitBurnMaxThrottle;

            private bool _lowDeorbitEndConditionSet;
            private bool _lowDeorbitEndOnLandingSiteNearer;

            private const double LOW_DEORBIT_BURN_TRIGGER_FACTOR = 2;

            public LowDeorbitBurn(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (_deorbitBurnTriggered && Core.Attitude.attitudeAngleFromTarget() < 5)
                {
                    Core.Thrust.targetThrottle = Mathf.Clamp01((float)_lowDeorbitBurnMaxThrottle);
                }
                else
                {
                    Core.Thrust.targetThrottle = 0;
                }

                return this;
            }

            public override AutopilotStep OnFixedUpdate()
            {
                //Decide when we will start the deorbit burn:
                double stoppingDistance = Math.Pow(VesselState.speedSurfaceHorizontal, 2) / (2 * VesselState.limitedMaxThrustAccel);
                double triggerDistance = LOW_DEORBIT_BURN_TRIGGER_FACTOR * stoppingDistance;
                double heightAboveTarget = VesselState.altitudeASL - Core.Landing.DecelerationEndAltitude();
                if (triggerDistance < heightAboveTarget)
                {
                    triggerDistance = heightAboveTarget;
                }

                //See if it's time to start the deorbit burn:
                double rangeToTarget = Vector3d.Exclude(VesselState.up, Core.Target.GetPositionTargetPosition() - VesselState.CoM).magnitude;

                if (!_deorbitBurnTriggered && rangeToTarget < triggerDistance)
                {
                    if (!MuUtils.PhysicsRunning()) Core.Warp.MinimumWarp(true);
                    _deorbitBurnTriggered = true;
                }

                Status = Localizer.Format(_deorbitBurnTriggered ? "#MechJeb_LandingGuidance_Status11" : //"Executing low deorbit burn"
                    "#MechJeb_LandingGuidance_Status12");                                               //"Moving to low deorbit burn point"

                //Warp toward deorbit burn if it hasn't been triggerd yet:
                if (!_deorbitBurnTriggered && Core.Node.autowarp && rangeToTarget > 2 * triggerDistance)
                    Core.Warp.WarpRegularAtRate((float)(Orbit.period / 6));
                if (rangeToTarget < triggerDistance && !MuUtils.PhysicsRunning()) Core.Warp.MinimumWarp();

                //By default, thrust straight back at max throttle
                Vector3d thrustDirection = -VesselState.surfaceVelocity.normalized;
                _lowDeorbitBurnMaxThrottle = 1;

                //If we are burning, we watch the predicted landing site and switch to the braking
                //burn when the predicted landing site crosses the target. We also use the predictions
                //to steer the predicted landing site toward the target
                if (_deorbitBurnTriggered && Core.Landing.PredictionReady)
                {
                    //angle slightly left or right to fix any cross-range error in the predicted landing site:
                    Vector3d horizontalToLandingSite = Vector3d.Exclude(VesselState.up, Core.Landing.LandingSite - VesselState.CoM).normalized;
                    Vector3d horizontalToTarget =
                        Vector3d.Exclude(VesselState.up, Core.Target.GetPositionTargetPosition() - VesselState.CoM).normalized;
                    const double ANGLE_GAIN = 4;
                    Vector3d angleCorrection = ANGLE_GAIN * (horizontalToTarget - horizontalToLandingSite);
                    if (angleCorrection.magnitude > 0.1) angleCorrection *= 0.1 / angleCorrection.magnitude;
                    thrustDirection = (thrustDirection + angleCorrection).normalized;

                    double rangeToLandingSite = Vector3d.Exclude(VesselState.up, Core.Landing.LandingSite - VesselState.CoM).magnitude;
                    double maxAllowedSpeed = Core.Landing.MaxAllowedSpeed();

                    if (!_lowDeorbitEndConditionSet &&
                        Vector3d.Distance(Core.Landing.LandingSite, VesselState.CoM) < MainBody.Radius + VesselState.altitudeASL)
                    {
                        _lowDeorbitEndOnLandingSiteNearer = rangeToLandingSite > rangeToTarget;
                        _lowDeorbitEndConditionSet        = true;
                    }

                    _lowDeorbitBurnMaxThrottle = 1;

                    if (Orbit.PeA < 0)
                    {
                        if (rangeToLandingSite > rangeToTarget)
                        {
                            if (_lowDeorbitEndConditionSet && !_lowDeorbitEndOnLandingSiteNearer)
                            {
                                Core.Thrust.targetThrottle = 0;
                                return new DecelerationBurn(Core);
                            }

                            double maxAllowedSpeedAfterDt = Core.Landing.MaxAllowedSpeedAfterDt(VesselState.deltaT);
                            double speedAfterDt = VesselState.speedSurface +
                                                  VesselState.deltaT * Vector3d.Dot(VesselState.gravityForce, VesselState.surfaceVelocity.normalized);
                            double throttleToMaintainLandingSite;
                            if (VesselState.speedSurface < maxAllowedSpeed) throttleToMaintainLandingSite = 0;
                            else
                                throttleToMaintainLandingSite =
                                    (speedAfterDt - maxAllowedSpeedAfterDt) / (VesselState.deltaT * VesselState.maxThrustAccel);

                            _lowDeorbitBurnMaxThrottle = throttleToMaintainLandingSite + 1 * (rangeToLandingSite / rangeToTarget - 1) + 0.2;
                        }
                        else
                        {
                            if (_lowDeorbitEndConditionSet && _lowDeorbitEndOnLandingSiteNearer)
                            {
                                Core.Thrust.targetThrottle = 0;
                                return new DecelerationBurn(Core);
                            }

                            _lowDeorbitBurnMaxThrottle = 0;
                            Status = Localizer.Format(
                                "#MechJeb_LandingGuidance_Status13"); //"Deorbit burn complete: waiting for the right moment to start braking"
                        }
                    }
                }

                Core.Attitude.attitudeTo(thrustDirection, AttitudeReference.INERTIAL, Core.Landing);

                return this;
            }
        }
    }
}
