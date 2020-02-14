using UnityEngine;
using KSP.Localization;

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
                if (!core.landing.PredictionReady)
                    return this;

                Vector3d horizontalPointingDirection = Vector3d.Exclude(vesselState.up, vesselState.forward).normalized;
                if (Vector3d.Dot(horizontalPointingDirection, vesselState.surfaceVelocity) > 0)
                {
                    core.thrust.targetThrottle = 0;
                    core.attitude.attitudeTo(Vector3.up, AttitudeReference.SURFACE_NORTH, core.landing);
                    return new FinalDescent(core);
                }

                //control thrust to control vertical speed:
                const double desiredSpeed = 0; //hover until horizontal velocity is killed
                double controlledSpeed = Vector3d.Dot(vesselState.surfaceVelocity, vesselState.up);
                double speedError = desiredSpeed - controlledSpeed;
                const double speedCorrectionTimeConstant = 1.0;
                double desiredAccel = speedError / speedCorrectionTimeConstant;
                double minAccel = -vesselState.localg;
                double maxAccel = -vesselState.localg + Vector3d.Dot(vesselState.forward, vesselState.up) * vesselState.maxThrustAccel;
                if (maxAccel - minAccel > 0)
                {
                    core.thrust.targetThrottle = Mathf.Clamp((float)((desiredAccel - minAccel) / (maxAccel - minAccel)), 0.0F, 1.0F);
                }
                else
                {
                    core.thrust.targetThrottle = 0;
                }

                //angle up and slightly away from vertical:
                Vector3d desiredThrustVector = (vesselState.up + 0.2 * horizontalPointingDirection).normalized;

                core.attitude.attitudeTo(desiredThrustVector, AttitudeReference.INERTIAL, core.landing);

                status = Localizer.Format("#MechJeb_LandingGuidance_Status10");//"Killing horizontal velocity before final descent"

                return this;
            }
        }
    }
}
