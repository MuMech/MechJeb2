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

                // it can stop the landing
                if (!Core.Landing.Enabled) return null;

                if (!Core.Landing.RCSAdjustment) return this;

                if (deltaV.magnitude > 3)
                    Core.RCS.Enabled = true;
                else if (deltaV.magnitude < 0.01)
                    Core.RCS.Enabled = false;

                if (Core.RCS.Enabled)
                    Core.RCS.SetWorldVelocityError(deltaV);

                return this;
            }

            private bool _warpReady;

            public override AutopilotStep OnFixedUpdate()
            {
                Core.Thrust.ThrustOff();

                // If the atmospheric drag is has started to act on the vessel then we are in a position to start considering when to deploy the parachutes.
                if (Core.Landing.DeployChutes)
                {
                    if (Core.Landing.ParachutesDeployable())
                    {
                        Core.Landing.ControlParachutes();
                    }
                }

                double maxAllowedSpeed = Core.Landing.MaxAllowedSpeed();
                if (VesselState.SpeedSurface > 0.9 * maxAllowedSpeed)
                {
                    Core.Warp.MinimumWarp();
                    if (Core.Landing.RCSAdjustment)
                        Core.RCS.Enabled = false;
                    return new DecelerationBurn(Core);
                }

                Status = Localizer.Format("#MechJeb_LandingGuidance_Status1"); //"Coasting toward deceleration burn"

                // without a landing prediction there is nothing to correct towards
                if (Core.Landing.LandAtTarget && Core.Landing.PredictionReady)
                {
                    double currentError = Vector3d.Distance(Core.Target.GetPositionTargetPosition(), Core.Landing.LandingSite);
                    if (currentError > 1000)
                    {
                        if (!VesselState.ParachuteDeployed && VesselState.LimitedMaxThrustAcceleration > 0 &&
                            VesselState.DragAcceleration <=
                            0.1) // However if there is already a parachute deployed or drag is high, then do not bother trying to correct the course as we will not have any attitude control anyway.
                        {
                            Core.Warp.MinimumWarp();
                            if (Core.Landing.RCSAdjustment)
                                Core.RCS.Enabled = false;
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
                if (VesselState.AltitudeASL < Core.Landing.DecelerationEndAltitude() + 5)
                {
                    Core.Warp.MinimumWarp();
                    if (Core.Landing.RCSAdjustment)
                        Core.RCS.Enabled = false;
                    return new DecelerationBurn(Core);
                }

                if (Core.Attitude.AttitudeAngleFromTarget() < 1) { _warpReady = true; } // less warp start warp stop jumping

                if (Core.Attitude.AttitudeAngleFromTarget() > 5) { _warpReady = false; } // hopefully

                // in the air the attitude needs no prediction: a long entry may have none for a while
                if (VesselState.DragAcceleration >= 0.01 || Core.Landing.PredictionReady)
                {
                    if (VesselState.DragAcceleration < 0.01)
                    {
                        double decelerationStartTime = Core.Landing.Prediction.Trajectory.Any()
                            ? Core.Landing.Prediction.Trajectory.First().UT
                            : VesselState.Time;
                        Vector3d entryPosition = Orbit.WorldPositionAtUT(decelerationStartTime);
                        Vector3d decelerationStartAttitude = -Orbit.WorldOrbitalVelocityAtUT(decelerationStartTime);
                        decelerationStartAttitude += MainBody.getRFrmVel(entryPosition);
                        decelerationStartAttitude = decelerationStartAttitude.normalized;
                        // turned in vacuum to the attitude it will meet the air with
                        if (Core.Landing.EntryAttitudeIsDefault)
                            Core.Attitude.AttitudeTo(decelerationStartAttitude, AttitudeReference.INERTIAL, Core.Landing);
                        else
                            Core.Landing.EntryAttitudeTo(-decelerationStartAttitude, entryPosition - MainBody.position);
                    }
                    else if (Core.Landing.EntryAttitudeIsDefault || VesselState.ParachuteDeployed)
                    {
                        Core.Attitude.AttitudeTo(Vector3.back, AttitudeReference.SURFACE_VELOCITY, Core.Landing);
                    }
                    else
                    {
                        // the entry attitude until the parachutes open or the deceleration burn starts, as the simulation assumes
                        Core.Landing.EntryAttitudeTo(VesselState.SurfaceVelocity, VesselState.Up);
                    }
                }

                //Warp at a rate no higher than the rate that would have us impacting the ground 10 seconds from now:
                if (_warpReady && Core.Landing.Autowarp && !Core.Landing.NearJettison)
                {
                    // Make sure if we're hovering that we don't go straight into too fast of a warp
                    // (g * 5 is average velocity falling for 10 seconds from a hover)
                    double velocityGuess = Math.Max(Math.Abs(VesselState.SpeedVertical), VesselState.LocalGravity * 5);
                    Core.Warp.WarpRegularAtRate((float)(VesselState.AltitudeASL / (10 * velocityGuess)));
                }
                else if (Core.Warp.MechJebWarping)
                {
                    // MechJeb's own warp ends; with Auto-warp off the player may warp by hand
                    Core.Warp.MinimumWarp();
                }

                return this;
            }
        }
    }
}
