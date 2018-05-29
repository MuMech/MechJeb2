using System;
using UnityEngine;

namespace MuMech
{
    namespace Landing
    {
        public class FinalDescent : AutopilotStep
        {

            private IDescentSpeedPolicy aggressivePolicy;
            private float thrustAt200Meters;
            private bool forceVerticalMode = false;

            public FinalDescent(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep OnFixedUpdate()
            {
                double minalt = Math.Min(vesselState.altitudeBottom, Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue));

                if (core.node.autowarp && aggressivePolicy != null)
                {
                    double maxVel = 1.02 * aggressivePolicy.MaxAllowedSpeed(vesselState.CoM - mainBody.position, vesselState.surfaceVelocity);

                    double diffPercent = ((maxVel / vesselState.speedSurface) - 1) * 100;
                    
                    if (minalt > 200  && diffPercent > 0 && (Vector3d.Angle(vesselState.forward, -vesselState.surfaceVelocity) < 45))
                        core.warp.WarpRegularAtRate((float)(diffPercent * diffPercent * diffPercent));
                    else
                        core.warp.MinimumWarp(true);
                    
                }
                return this;
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (vessel.LandedOrSplashed)
                {
                    core.landing.StopLanding();
                    return null;
                }

                // TODO perhaps we should pop the parachutes at this point, or at least consider it depending on the altitude.

                double minalt = Math.Min(vesselState.altitudeBottom, Math.Min(vesselState.altitudeASL, vesselState.altitudeTrue));

                if (vesselState.limitedMaxThrustAccel < vesselState.gravityForce.magnitude)
                {
                    // if we have TWR < 1, just try as hard as we can to decelerate:
                    // (we need this special case because otherwise the calculations spit out NaN's)
                    core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
                    core.thrust.trans_kill_h = true;
                    core.thrust.trans_spd_act = 0;
                }
                else if (minalt > 200)
                {
                    if ((vesselState.surfaceVelocity.magnitude > 5) && (Vector3d.Angle(vesselState.surfaceVelocity, vesselState.up) < 80))
                    {
                        // if we have positive vertical velocity, point up and don't thrust:
                        core.attitude.attitudeTo(Vector3d.up, AttitudeReference.SURFACE_NORTH, null);
                        core.thrust.tmode = MechJebModuleThrustController.TMode.DIRECT;
                        core.thrust.trans_spd_act = 0;
                    }
                    else if ((vesselState.surfaceVelocity.magnitude > 5) && (Vector3d.Angle(vesselState.forward, -vesselState.surfaceVelocity) > 45))
                    {
                        // if we're not facing approximately retrograde, turn to point retrograde and don't thrust:
                        core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                        core.thrust.tmode = MechJebModuleThrustController.TMode.DIRECT;
                        core.thrust.trans_spd_act = 0;
                    }
                    else
                    {
                        //if we're above 200m, point retrograde and control surface velocity:
                        core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);

                        core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_SURFACE;

                        //core.thrust.trans_spd_act = (float)Math.Sqrt((vesselState.maxThrustAccel - vesselState.gravityForce.magnitude) * 2 * minalt) * 0.90F;
                        Vector3d estimatedLandingPosition = vesselState.CoM + vesselState.surfaceVelocity.sqrMagnitude / (2 * vesselState.limitedMaxThrustAccel) * vesselState.surfaceVelocity.normalized;
                        double terrainRadius = mainBody.Radius + mainBody.TerrainAltitude(estimatedLandingPosition);
                        aggressivePolicy = new GravityTurnDescentSpeedPolicy(terrainRadius, mainBody.GeeASL * 9.81, vesselState.limitedMaxThrustAccel);  // this constant policy creation is wastefull...
                        core.thrust.trans_spd_act = (float)(aggressivePolicy.MaxAllowedSpeed(vesselState.CoM - mainBody.position, vesselState.surfaceVelocity));
                        thrustAt200Meters = (float)vesselState.limitedMaxThrustAccel; // this gets updated until we are below 200 meters
                    }
                }
                else
                {
                    float previous_trans_spd_act = Math.Abs(core.thrust.trans_spd_act); // avoid issues going from KEEP_SURFACE mode to KEEP_VERTICAL mode

                    // Last 200 meters, at this point the rocket has a TWR that will rise rapidly, so be sure to use the same policy based on an older
                    // TWR. Otherwise we would suddenly get a large jump in desired speed leading to a landing that is too fast.
                    core.thrust.trans_spd_act = Mathf.Lerp(0, (float)Math.Sqrt((thrustAt200Meters - vesselState.localg) * 2 * 200) * 0.90F, (float)minalt / 200);

                    // Sometimes during the descent we end up going up a bit due to overthrusting during breaking, avoid thrusting up even more and destabilizing the rocket
                    core.thrust.trans_spd_act = (float)Math.Min(previous_trans_spd_act, core.thrust.trans_spd_act);

                    // take into account desired landing speed:
                    core.thrust.trans_spd_act = (float)Math.Max(core.landing.touchdownSpeed, core.thrust.trans_spd_act);

                    // Prevent that we switch back from Vertical mode to KeepSurface mode
                    // When that happens the rocket will start tilting and end up falling over
                    if (vesselState.speedSurfaceHorizontal < 5)
                    {
                        forceVerticalMode = true;
                    }

                    if (forceVerticalMode)
                    {
                        // if we're falling more or less straight down, control vertical speed and 
                        // kill horizontal velocity
                        core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_VERTICAL;
                        core.thrust.trans_spd_act *= -1;
                        core.thrust.trans_kill_h = false; // rockets are long, and will fall over if you attempt to do any manouvers to kill that last bit of horizontal speed
                        if (core.landing.rcsAdjustment) // If allowed, use RCS to stablize the rocket
                            core.rcs.enabled = true;
                        // Turn on SAS because we are likely to have a bit of horizontal speed left that needs to stabilize
                        // Use SmartASS, because Mechjeb doesn't expect SAS to be used (i.e. it is automatically turned off again)
                        core.EngageSmartASSControl(MechJebModuleSmartASS.Mode.SURFACE, MechJebModuleSmartASS.Target.VERTICAL_PLUS, true);
                    }
                    else
                    {
                        // if we're falling at a significant angle from vertical, our vertical speed might be
                        // quite small but we might still need to decelerate. Control the total speed instead
                        // by thrusting directly retrograde
                        core.attitude.attitudeTo(Vector3d.back, AttitudeReference.SURFACE_VELOCITY, null);
                        core.thrust.tmode = MechJebModuleThrustController.TMode.KEEP_SURFACE;
                    }
                }

                status = "Final descent: " + vesselState.altitudeBottom.ToString("F0") + "m above terrain";

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
