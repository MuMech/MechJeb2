using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    namespace Landing
    {
        public class KillHorizontalVelocity : AutopilotStep
        {
            public KillHorizontalVelocity(MechJebCore core) : base(core)
            {
            }

            // TODO I think that this function could be better rewritten to much more agressively kill the horizontal velocity. At present on low gravity bodies such as Bop, the craft will hover and slowly drift sideways, loosing the prescion of the landing. 
            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (!Core.Landing.PredictionReady)
                    return this;

                Vector3d horizontalPointingDirection = Vector3d.Exclude(VesselState.Up, VesselState.Forward).normalized;
                if (Vector3d.Dot(horizontalPointingDirection, VesselState.SurfaceVelocity) > 0)
                {
                    Core.Thrust.RequestActiveThrottle(0.0f);
                    Core.Attitude.attitudeTo(Vector3.up, AttitudeReference.SURFACE_NORTH, Core.Landing);
                    return new FinalDescent(Core);
                }

                //control thrust to control vertical speed:
                const double DESIRED_SPEED = 0; //hover until horizontal velocity is killed
                double controlledSpeed = Vector3d.Dot(VesselState.SurfaceVelocity, VesselState.Up);
                double speedError = DESIRED_SPEED - controlledSpeed;
                const double SPEED_CORRECTION_TIME_CONSTANT = 1.0;
                double desiredAccel = speedError / SPEED_CORRECTION_TIME_CONSTANT;
                double minAccel = -VesselState.LocalGravity;
                double maxAccel = -VesselState.LocalGravity + Vector3d.Dot(VesselState.Forward, VesselState.Up) * VesselState.MaxThrustAcceleration;
                if (maxAccel - minAccel > 0)
                {
                    Core.Thrust.RequestActiveThrottle(Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0f, 1.0f));
                }
                else
                {
                    Core.Thrust.RequestActiveThrottle(0.0f);
                }

                //angle up and slightly away from vertical:
                Vector3d desiredThrustVector = (VesselState.Up + 0.2 * horizontalPointingDirection).normalized;

                Core.Attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, Core.Landing);

                Status = Localizer.Format("#MechJeb_LandingGuidance_Status10"); //"Killing horizontal velocity before final descent"

                return this;
            }
        }
    }
}
