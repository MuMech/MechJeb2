using System;
using UnityEngine;

// FIXME: use a maneuver node

namespace MuMech
{
    namespace Landing
    {
        public class PlaneChange : AutopilotStep
        {
            bool planeChangeTriggered = false;
            double planeChangeDVLeft;

            public PlaneChange(MechJebCore core) : base(core)
            {
            }

            //Could make this an iterative procedure for improved accuracy
            Vector3d ComputePlaneChange()
            {
                Vector3d targetRadialVector = core.vessel.mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0);
                Vector3d currentRadialVector = core.vesselState.CoM - core.vessel.mainBody.position;
                double angleToTarget = Vector3d.Angle(targetRadialVector, currentRadialVector);
                //this calculation seems like it might be be working right:
                double timeToTarget = orbit.TimeOfTrueAnomaly(core.vessel.orbit.trueAnomaly + angleToTarget, vesselState.time) - vesselState.time;
                double planetRotationAngle = 360 * timeToTarget / mainBody.rotationPeriod;
                Quaternion planetRotation = Quaternion.AngleAxis((float)planetRotationAngle, mainBody.angularVelocity);
                Vector3d targetRadialVectorOnFlyover = planetRotation * targetRadialVector;
                Vector3d horizontalToTarget = Vector3d.Exclude(vesselState.up, targetRadialVectorOnFlyover - currentRadialVector).normalized;
                return horizontalToTarget;
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (planeChangeTriggered && core.attitude.attitudeAngleFromTarget() < 2)
                {
                    core.thrust.targetThrottle = Mathf.Clamp01((float)(planeChangeDVLeft / (2 * core.vesselState.maxThrustAccel)));
                }
                else
                {
                    core.thrust.targetThrottle = 0;
                }
                return this;
            }

            public override AutopilotStep OnFixedUpdate()
            {
                Vector3d targetRadialVector = mainBody.GetRelSurfacePosition(core.target.targetLatitude, core.target.targetLongitude, 0);
                Vector3d currentRadialVector = vesselState.CoM - mainBody.position;
                double angleToTarget = Vector3d.Angle(targetRadialVector, currentRadialVector);
                bool approaching = Vector3d.Dot(targetRadialVector - currentRadialVector, vesselState.orbitalVelocity) > 0;

                if (!planeChangeTriggered && approaching && (angleToTarget > 80) && (angleToTarget < 90))
                {
                    if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp(true);
                    planeChangeTriggered = true;
                }

                if (planeChangeTriggered)
                {
                    Vector3d horizontalToTarget = ComputePlaneChange();
                    Vector3d finalVelocity = Quaternion.FromToRotation(vesselState.horizontalOrbit, horizontalToTarget) * vesselState.orbitalVelocity;

                    Vector3d deltaV = finalVelocity - vesselState.orbitalVelocity;
                    //burn normal+ or normal- to avoid dropping the Pe:
                    Vector3d burnDir = Vector3d.Exclude(vesselState.up, Vector3d.Exclude(vesselState.orbitalVelocity, deltaV));
                    planeChangeDVLeft = Math.PI / 180 * Vector3d.Angle(finalVelocity, vesselState.orbitalVelocity) * vesselState.speedOrbitHorizontal;
                    core.attitude.attitudeTo(burnDir, AttitudeReference.INERTIAL, core.landing);
                    status = "Executing low orbit plane change of about " + planeChangeDVLeft.ToString("F0") + " m/s";

                    if (planeChangeDVLeft < 0.1F)
                    {
                        return new LowDeorbitBurn(core);
                    }
                }
                else
                {
                    if (core.node.autowarp) core.warp.WarpRegularAtRate((float)(orbit.period / 6));
                    status = "Moving to low orbit plane change burn point";
                }

                return this;
            }
        }
    }
}
