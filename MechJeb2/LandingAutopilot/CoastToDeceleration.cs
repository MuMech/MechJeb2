using System;
using System.Linq;
using UnityEngine;

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
                    else if (deltaV.magnitude < 0.1)
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
                if (mainBody.DragAccel(vesselState.CoM, vesselState.orbitalVelocity, vesselState.massDrag / vesselState.mass).magnitude > 0.10)
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
                    core.rcs.enabled = false;
                    return new DecelerationBurn(core);
                }

                status = "Coasting toward deceleration burn";

                if (core.landing.landAtTarget)
                {
                    double currentError = Vector3d.Distance(core.target.GetPositionTargetPosition(), core.landing.LandingSite);
                    if (currentError > 1000)
                    {
                        if (!vesselState.parachuteDeployed) // However if there is already a parachute deployed, then do not bother trying to correct the course as we will not have any attitude control anyway.
                        {
                            core.warp.MinimumWarp();
                            core.rcs.enabled = false;
                            return new CourseCorrection(core);
                        }
                    }
                    else
                    {
                        Vector3d deltaV = core.landing.ComputeCourseCorrection(true);
                        status += "\nCourse correction DV: " + deltaV.magnitude.ToString("F3") + " m/s";
                    }
                }

                // Sometimes (on bodies with a thick atmosphere) there is no need for a decleration burn. Check for this so that it is possible to transition into the final decent step.
                if ((vesselState.altitudeASL < core.landing.DecelerationEndAltitude() + 5) && core.landing.UseAtmosphereToBrake())
                {
                    core.warp.MinimumWarp();
                    return new FinalDescent(core);
                }

                if (core.attitude.attitudeAngleFromTarget() < 1) { warpReady = true; } // less warp start warp stop jumping
                if (core.attitude.attitudeAngleFromTarget() > 5) { warpReady = false; } // hopefully

                if (core.landing.PredictionReady)
                {
                    double decelerationStartTime = (core.landing.prediction.trajectory.Any() ? core.landing.prediction.trajectory.First().UT : vesselState.time);
                    Vector3d decelerationStartAttitude = -orbit.SwappedOrbitalVelocityAtUT(decelerationStartTime);
                    decelerationStartAttitude += mainBody.getRFrmVel(orbit.SwappedAbsolutePositionAtUT(decelerationStartTime));
                    decelerationStartAttitude = decelerationStartAttitude.normalized;
                    core.attitude.attitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, core.landing);
                }

                //Warp at a rate no higher than the rate that would have us impacting the ground 10 seconds from now:
                if (warpReady && core.node.autowarp)
                    core.warp.WarpRegularAtRate((float)(vesselState.altitudeASL / (10 * Math.Abs(vesselState.speedVertical))));
                else
                    core.warp.MinimumWarp();

                return this;
            }
        }
    }
}
