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
                // the deorbit burn hands over at full throttle: without a prediction nothing else would close it, and the burn runs on
                if (!Core.Landing.PredictionReady)
                {
                    Core.Thrust.ThrustOff();
                    return this;
                }

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
                    Core.Thrust.ThrustOff();
                    if (Core.Landing.RCSAdjustment)
                        Core.RCS.Enabled = true;
                    return new CoastToDeceleration(Core);
                }

                // If we're off course, but already too low, skip the course correction
                if (VesselState.AltitudeASL < Core.Landing.DecelerationEndAltitude() + 5)
                {
                    return new DecelerationBurn(Core);
                }


                // If a parachute has already been deployed then we will not be able to control attitude anyway, so move back to the coast to deceleration step.
                if (VesselState.ParachuteDeployed)
                {
                    Core.Thrust.ThrustOff();
                    return new CoastToDeceleration(Core);
                }

                // We are not in .90 anymore. Turning while under drag is a bad idea
                if (VesselState.DragAcceleration > 0.1)
                {
                    return new CoastToDeceleration(Core);
                }

                // A vessel that has dropped its engines can't correct anything, and turning towards the burn takes its heat shield off the airflow
                if (VesselState.LimitedMaxThrustAcceleration <= 0)
                {
                    Core.Thrust.ThrustOff();
                    return new CoastToDeceleration(Core);
                }

                Vector3d deltaV = Core.Landing.ComputeCourseCorrection(true);

                // it can stop the landing
                if (!Core.Landing.Enabled) return null;

                Status = Localizer.Format("#MechJeb_LandingGuidance_Status3",
                    deltaV.magnitude.ToString("F1")); //"Performing course correction of about " +  + " m/s"

                Core.Attitude.AttitudeTo(deltaV.normalized, AttitudeReference.INERTIAL, Core.Landing);

                if (Core.Attitude.AttitudeAngleFromTarget() < 2)
                    _courseCorrectionBurning = true;
                else if (Core.Attitude.AttitudeAngleFromTarget() > 30)
                    _courseCorrectionBurning = false;

                if (_courseCorrectionBurning)
                {
                    const double TIME_CONSTANT = 2.0;
                    Core.Thrust.ThrustForDv(deltaV.magnitude, TIME_CONSTANT);
                }
                else
                {
                    Core.Thrust.ThrustOff();
                }

                return this;
            }
        }
    }
}
