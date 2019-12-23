using KSP.Localization;
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
                if (core.landing.deployChutes)
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
                    if (core.landing.rcsAdjustment)
                        core.rcs.enabled = true;
                    return new CoastToDeceleration(core);
                }

                // If we're off course, but already too low, skip the course correction
                if (vesselState.altitudeASL < core.landing.DecelerationEndAltitude() + 5)
                {
                    return new DecelerationBurn(core);
                }


                // If a parachute has already been deployed then we will not be able to control attitude anyway, so move back to the coast to deceleration step.
                if (vesselState.parachuteDeployed)
                {
                    core.thrust.targetThrottle = 0;
                    return new CoastToDeceleration(core);
                }

                // We are not in .90 anymore. Turning while under drag is a bad idea
                if (vesselState.drag > 0.1)
                {
                    return new CoastToDeceleration(core);
                }

                Vector3d deltaV = core.landing.ComputeCourseCorrection(true);

                status = Localizer.Format("#MechJeb_LandingGuidance_Status3", deltaV.magnitude.ToString("F1"));//"Performing course correction of about " +  + " m/s"

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
