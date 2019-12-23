using System;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    namespace Landing
    {
        public class CoastToDeceleration : AutopilotStep
        {
            public CoastToDeceleration(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (!core.landing.PredictionReady)
                    return this;

                Vector3d deltaV = core.landing.ComputeCourseCorrection(true);

                if (core.landing.rcsAdjustment)
                {
                    if (deltaV.magnitude > 3)
                        core.rcs.enabled = true;
                    else if (deltaV.magnitude < 0.01)
                        core.rcs.enabled = false;

                    if (core.rcs.enabled)
                        core.rcs.SetWorldVelocityError(deltaV);
                }

                return this;
            }

            bool warpReady;

            public override AutopilotStep OnFixedUpdate()
            {
                core.thrust.targetThrottle = 0;

                // If the atmospheric drag is has started to act on the vessel then we are in a position to start considering when to deploy the parachutes.
                if (core.landing.deployChutes)
                {
                    if (core.landing.ParachutesDeployable())
                    {
                        core.landing.ControlParachutes();
                    }
                }

                double maxAllowedSpeed = core.landing.MaxAllowedSpeed();
                if (vesselState.speedSurface > 0.9 * maxAllowedSpeed)
                {
                    core.warp.MinimumWarp();
                    if (core.landing.rcsAdjustment)
                        core.rcs.enabled = false;
                    return new DecelerationBurn(core);
                }

                status = Localizer.Format("#MechJeb_LandingGuidance_Status1");//"Coasting toward deceleration burn"

                if (core.landing.landAtTarget)
                {
                    double currentError = Vector3d.Distance(core.target.GetPositionTargetPosition(), core.landing.LandingSite);
                    if (currentError > 1000)
                    {
                        if (!vesselState.parachuteDeployed && vesselState.drag <= 0.1) // However if there is already a parachute deployed or drag is high, then do not bother trying to correct the course as we will not have any attitude control anyway.
                        {
                            core.warp.MinimumWarp();
                            if (core.landing.rcsAdjustment)
                                core.rcs.enabled = false;
                            return new CourseCorrection(core);
                        }
                    }
                    else
                    {
                        Vector3d deltaV = core.landing.ComputeCourseCorrection(true);
                        status += "\n" + Localizer.Format("#MechJeb_LandingGuidance_Status2",deltaV.magnitude.ToString("F3"));//"Course correction DV: " +  + " m/s"
                    }
                }

                // If we're already low, skip directly to the Deceleration burn
                if (vesselState.altitudeASL < core.landing.DecelerationEndAltitude() + 5)
                {
                    core.warp.MinimumWarp();
                    if (core.landing.rcsAdjustment)
                        core.rcs.enabled = false;
                    return new DecelerationBurn(core);
                }

                if (core.attitude.attitudeAngleFromTarget() < 1) { warpReady = true; } // less warp start warp stop jumping
                if (core.attitude.attitudeAngleFromTarget() > 5) { warpReady = false; } // hopefully

                if (core.landing.PredictionReady)
                {
                    if (vesselState.drag < 0.01)
                    {
                        double decelerationStartTime = (core.landing.prediction.trajectory.Any() ? core.landing.prediction.trajectory.First().UT : vesselState.time);
                        Vector3d decelerationStartAttitude = -orbit.SwappedOrbitalVelocityAtUT(decelerationStartTime);
                        decelerationStartAttitude += mainBody.getRFrmVel(orbit.SwappedAbsolutePositionAtUT(decelerationStartTime));
                        decelerationStartAttitude = decelerationStartAttitude.normalized;
                        core.attitude.attitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, core.landing);
                    }
                    else
                    {
                        core.attitude.attitudeTo(Vector3.back, AttitudeReference.SURFACE_VELOCITY, core.landing);
                    }
                }

                //Warp at a rate no higher than the rate that would have us impacting the ground 10 seconds from now:
                if (warpReady && core.node.autowarp)
                {
                    // Make sure if we're hovering that we don't go straight into too fast of a warp
                    // (g * 5 is average velocity falling for 10 seconds from a hover)
                    double velocityGuess = Math.Max(Math.Abs(vesselState.speedVertical), vesselState.localg * 5);
                    core.warp.WarpRegularAtRate((float)(vesselState.altitudeASL / (10 * velocityGuess)));
                }
                else
                {
                    core.warp.MinimumWarp();
                }

                return this;
            }
        }
    }
}
