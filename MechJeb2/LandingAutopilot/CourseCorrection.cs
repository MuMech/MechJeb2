using System;
using UnityEngine;

namespace MuMech
{
    namespace Landing
    {
        public class CourseCorrection : AutopilotStep
        {
            bool courseCorrectionBurning = false;

            public CourseCorrection(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (!core.landing.PredictionReady)
                    return this;

                // If the atomospheric drag is at least 100mm/s2 then start trying to target the overshoot using the parachutes
                if (mainBody.DragAccel(vesselState.CoM, vesselState.orbitalVelocity, vesselState.massDrag / vesselState.mass).magnitude > 0.1)
                {
                    if (core.landing.ParachutesDeployable())
                    {
                        core.landing.ControlParachutes();
                    }
                }

                double currentError = Vector3d.Distance(core.target.GetPositionTargetPosition(), core.landing.LandingSite);

                if (currentError < 150)
                {
                    core.thrust.targetThrottle = 0;
                    core.rcs.enabled = core.landing.rcsAdjustment;
                    return new CoastToDeceleration(core);
                }

                // If a parachute has already been deployed then we will not be able to control attitude anyway, so move back to the coast to deceleration step.
                if (vesselState.parachuteDeployed)
                {
                    core.thrust.targetThrottle = 0;
                    return new CoastToDeceleration(core);
                }

                Vector3d deltaV = core.landing.ComputeCourseCorrection(true);

                status = "Performing course correction of about " + deltaV.magnitude.ToString("F1") + " m/s";

                core.attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, core.landing);

                if (core.attitude.attitudeAngleFromTarget() < 2)
                    courseCorrectionBurning = true;
                else if (core.attitude.attitudeAngleFromTarget() > 30)
                    courseCorrectionBurning = false;

                if (courseCorrectionBurning)
                {
                    const double timeConstant = 2.0;
                    core.thrust.ThrustForDV(deltaV.magnitude, timeConstant);
                }
                else
                {
                    core.thrust.targetThrottle = 0;
                }

                return this;
            }
        }
    }
}
