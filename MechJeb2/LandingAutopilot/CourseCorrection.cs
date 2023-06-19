using KSP.Localization;

namespace MuMech
{
    namespace Landing
    {
        public class CourseCorrection : AutopilotStep
        {
            private bool _courseCorrectionBurning;

            public CourseCorrection(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (!Core.Landing.PredictionReady)
                    return this;

                // If the atomospheric drag is at least 100mm/s2 then start trying to target the overshoot using the parachutes
                if (Core.Landing.DeployChutes)
                {
                    if (Core.Landing.ParachutesDeployable())
                    {
                        Core.Landing.ControlParachutes();
                    }
                }

                double currentError = Vector3d.Distance(Core.Target.GetPositionTargetPosition(), Core.Landing.LandingSite);

                if (currentError < 150)
                {
                    Core.Thrust.targetThrottle = 0;
                    if (Core.Landing.RCSAdjustment)
                        Core.RCS.enabled = true;
                    return new CoastToDeceleration(Core);
                }

                // If we're off course, but already too low, skip the course correction
                if (VesselState.altitudeASL < Core.Landing.DecelerationEndAltitude() + 5)
                {
                    return new DecelerationBurn(Core);
                }


                // If a parachute has already been deployed then we will not be able to control attitude anyway, so move back to the coast to deceleration step.
                if (VesselState.parachuteDeployed)
                {
                    Core.Thrust.targetThrottle = 0;
                    return new CoastToDeceleration(Core);
                }

                // We are not in .90 anymore. Turning while under drag is a bad idea
                if (VesselState.drag > 0.1)
                {
                    return new CoastToDeceleration(Core);
                }

                Vector3d deltaV = Core.Landing.ComputeCourseCorrection(true);

                Status = Localizer.Format("#MechJeb_LandingGuidance_Status3",
                    deltaV.magnitude.ToString("F1")); //"Performing course correction of about " +  + " m/s"

                Core.Attitude.attitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, Core.Landing);

                if (Core.Attitude.attitudeAngleFromTarget() < 2)
                    _courseCorrectionBurning = true;
                else if (Core.Attitude.attitudeAngleFromTarget() > 30)
                    _courseCorrectionBurning = false;

                if (_courseCorrectionBurning)
                {
                    const double TIME_CONSTANT = 2.0;
                    Core.Thrust.ThrustForDV(deltaV.magnitude, TIME_CONSTANT);
                }
                else
                {
                    Core.Thrust.targetThrottle = 0;
                }

                return this;
            }
        }
    }
}
