using System;
using System.Linq;
using KSP.Localization;
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
                if (!Core.Landing.PredictionReady)
                    return this;

                Vector3d deltaV = Core.Landing.ComputeCourseCorrection(true);

                if (!Core.Landing.RCSAdjustment) return this;

                if (deltaV.magnitude > 3)
                    Core.RCS.enabled = true;
                else if (deltaV.magnitude < 0.01)
                    Core.RCS.enabled = false;

                if (Core.RCS.enabled)
                    Core.RCS.SetWorldVelocityError(deltaV);

                return this;
            }

            private bool _warpReady;

            public override AutopilotStep OnFixedUpdate()
            {
                Core.Thrust.targetThrottle = 0;

                // If the atmospheric drag is has started to act on the vessel then we are in a position to start considering when to deploy the parachutes.
                if (Core.Landing.DeployChutes)
                {
                    if (Core.Landing.ParachutesDeployable())
                    {
                        Core.Landing.ControlParachutes();
                    }
                }

                double maxAllowedSpeed = Core.Landing.MaxAllowedSpeed();
                if (VesselState.speedSurface > 0.9 * maxAllowedSpeed)
                {
                    Core.Warp.MinimumWarp();
                    if (Core.Landing.RCSAdjustment)
                        Core.RCS.enabled = false;
                    return new DecelerationBurn(Core);
                }

                Status = Localizer.Format("#MechJeb_LandingGuidance_Status1"); //"Coasting toward deceleration burn"

                if (Core.Landing.LandAtTarget)
                {
                    double currentError = Vector3d.Distance(Core.Target.GetPositionTargetPosition(), Core.Landing.LandingSite);
                    if (currentError > 1000)
                    {
                        if (!VesselState.parachuteDeployed &&
                            VesselState.drag <=
                            0.1) // However if there is already a parachute deployed or drag is high, then do not bother trying to correct the course as we will not have any attitude control anyway.
                        {
                            Core.Warp.MinimumWarp();
                            if (Core.Landing.RCSAdjustment)
                                Core.RCS.enabled = false;
                            return new CourseCorrection(Core);
                        }
                    }
                    else
                    {
                        Vector3d deltaV = Core.Landing.ComputeCourseCorrection(true);
                        Status += "\n" + Localizer.Format("#MechJeb_LandingGuidance_Status2",
                            deltaV.magnitude.ToString("F3")); //"Course correction DV: " +  + " m/s"
                    }
                }

                // If we're already low, skip directly to the Deceleration burn
                if (VesselState.altitudeASL < Core.Landing.DecelerationEndAltitude() + 5)
                {
                    Core.Warp.MinimumWarp();
                    if (Core.Landing.RCSAdjustment)
                        Core.RCS.enabled = false;
                    return new DecelerationBurn(Core);
                }

                if (Core.Attitude.attitudeAngleFromTarget() < 1) { _warpReady = true; } // less warp start warp stop jumping

                if (Core.Attitude.attitudeAngleFromTarget() > 5) { _warpReady = false; } // hopefully

                if (Core.Landing.PredictionReady)
                {
                    if (VesselState.drag < 0.01)
                    {
                        double decelerationStartTime = Core.Landing.Prediction.Trajectory.Any()
                            ? Core.Landing.Prediction.Trajectory.First().UT
                            : VesselState.time;
                        Vector3d decelerationStartAttitude = -Orbit.WorldOrbitalVelocityAtUT(decelerationStartTime);
                        decelerationStartAttitude += MainBody.getRFrmVel(Orbit.WorldPositionAtUT(decelerationStartTime));
                        decelerationStartAttitude =  decelerationStartAttitude.normalized;
                        Core.Attitude.attitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, Core.Landing);
                    }
                    else
                    {
                        Core.Attitude.attitudeTo(Vector3.back, AttitudeReference.SURFACE_VELOCITY, Core.Landing);
                    }
                }

                //Warp at a rate no higher than the rate that would have us impacting the ground 10 seconds from now:
                if (_warpReady && Core.Node.autowarp)
                {
                    // Make sure if we're hovering that we don't go straight into too fast of a warp
                    // (g * 5 is average velocity falling for 10 seconds from a hover)
                    double velocityGuess = Math.Max(Math.Abs(VesselState.speedVertical), VesselState.localg * 5);
                    Core.Warp.WarpRegularAtRate((float)(VesselState.altitudeASL / (10 * velocityGuess)));
                }
                else
                {
                    Core.Warp.MinimumWarp();
                }

                return this;
            }
        }
    }
}
