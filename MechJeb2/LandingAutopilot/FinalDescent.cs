using System;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    namespace Landing
    {
        public class FinalDescent : AutopilotStep
        {
            private IDescentSpeedPolicy _aggressivePolicy;

            public FinalDescent(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep OnFixedUpdate()
            {
                return this;
                /*double minalt = Math.Min(VesselState.altitudeBottom, Math.Min(VesselState.altitudeASL, VesselState.altitudeTrue));

                if (!Core.Node.Autowarp || _aggressivePolicy == null) return this;

                double maxVel = 1.02 * _aggressivePolicy.MaxAllowedSpeed(VesselState.CoM - MainBody.position, VesselState.surfaceVelocity);

                double diffPercent = (maxVel / VesselState.speedSurface - 1) * 100;

                if (minalt > 200 && diffPercent > 0 && Vector3d.Angle(VesselState.forward, -VesselState.surfaceVelocity) < 45)
                    Core.Warp.WarpRegularAtRate((float)(diffPercent * diffPercent * diffPercent));
                else
                    Core.Warp.MinimumWarp(true);

                return this;*/
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (Vessel.LandedOrSplashed)
                {
                    Core.Landing.StopLanding();
                    return null;
                }

                // TODO perhaps we should pop the parachutes at this point, or at least consider it depending on the altitude.

                double minalt = Math.Min(VesselState.altitudeBottom, Math.Min(VesselState.altitudeASL, VesselState.altitudeTrue));

                if (VesselState.limitedMaxThrustAccel < VesselState.gravityForce.magnitude)
                {
                    // if we have TWR < 1, just try as hard as we can to decelerate:
                    // (we need this special case because otherwise the calculations spit out NaN's)
                    Core.Thrust.Tmode       = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
                    Core.Thrust.TransKillH  = true;
                    Core.Thrust.TransSpdAct = 0;
                }
                else if (minalt > 200)
                {
                    if (VesselState.surfaceVelocity.magnitude > 5 && Vector3d.Angle(VesselState.surfaceVelocity, VesselState.up) < 80)
                    {
                        // if we have positive vertical velocity, point up and don't thrust:
                        Core.Attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, null);
                        Core.Thrust.Tmode       = MechJebModuleThrustController.TMode.DIRECT;
                        Core.Thrust.TransSpdAct = 0;
                    }
                    else if (VesselState.surfaceVelocity.magnitude > 5 && Vector3d.Angle(VesselState.forward, -VesselState.surfaceVelocity) > 45)
                    {
                        // if we're not facing approximately retrograde, turn to point retrograde and don't thrust:
                        Core.Attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                        Core.Thrust.Tmode       = MechJebModuleThrustController.TMode.DIRECT;
                        Core.Thrust.TransSpdAct = 0;
                    }
                    else
                    {
                        //if we're above 200m, point retrograde and control surface velocity:
                        Core.Attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);

                        Core.Thrust.Tmode = MechJebModuleThrustController.TMode.KEEP_SURFACE;

                        //core.thrust.trans_spd_act = (float)Math.Sqrt((vesselState.maxThrustAccel - vesselState.gravityForce.magnitude) * 2 * minalt) * 0.90F;
                        Vector3d estimatedLandingPosition = VesselState.CoM + VesselState.surfaceVelocity.sqrMagnitude /
                            (2 * VesselState.limitedMaxThrustAccel) * VesselState.surfaceVelocity.normalized;
                        double terrainRadius = MainBody.Radius + MainBody.TerrainAltitude(estimatedLandingPosition);
                        _aggressivePolicy =
                            new GravityTurnDescentSpeedPolicy(terrainRadius, MainBody.GeeASL * 9.81,
                                VesselState.limitedMaxThrustAccel); // this constant policy creation is wastefull...
                        Core.Thrust.TransSpdAct =
                            (float)_aggressivePolicy.MaxAllowedSpeed(VesselState.CoM - MainBody.position, VesselState.surfaceVelocity);
                    }
                }
                else
                {
                    // last 200 meters:
                    Core.Thrust.TransSpdAct = -Mathf.Lerp(0,
                        (float)Math.Sqrt((VesselState.limitedMaxThrustAccel - VesselState.localg) * 2 * 200) * 0.90F, (float)minalt / 200);

                    // take into account desired landing speed:
                    Core.Thrust.TransSpdAct = (float)Math.Min(-Core.Landing.TouchdownSpeed, Core.Thrust.TransSpdAct);

//                    core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
//                    core.thrust.trans_kill_h = true;

//                    if (Math.Abs(Vector3d.Angle(-vessel.surfaceVelocity, vesselState.up)) < 10)
                    if (VesselState.speedSurfaceHorizontal < 5)
                    {
                        // if we're falling more or less straight down, control vertical speed and
                        // kill horizontal velocity
                        Core.Thrust.Tmode      = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
                        Core.Thrust.TransKillH = true;
                    }
                    else
                    {
                        // if we're falling at a significant angle from vertical, our vertical speed might be
                        // quite small but we might still need to decelerate. Control the total speed instead
                        // by thrusting directly retrograde
                        Core.Attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                        Core.Thrust.Tmode       =  MechJebModuleThrustController.TMode.KEEP_SURFACE;
                        Core.Thrust.TransSpdAct *= -1;
                    }
                }

                Status = Localizer.Format("#MechJeb_LandingGuidance_Status9",
                    VesselState.altitudeBottom.ToString("F0")); //"Final descent: " +  + "m above terrain"

                // ComputeCourseCorrection doesn't work close to the ground
                /* if (core.landing.landAtTarget)
                {
                    core.rcs.enabled = true;
                    Vector3d deltaV = core.landing.ComputeCourseCorrection(false);
                    core.rcs.SetWorldVelocityError(deltaV);

                    status += "\nDV: " + deltaV.magnitude.ToString("F3") + " m/s";
                } */

                return this;
            }
        }
    }
}
