using KSP.Localization;

namespace MuMech
{
    namespace Landing
    {
        public class UntargetedDeorbit : AutopilotStep
        {
            public UntargetedDeorbit(MechJebCore core) : base(core)
            {
            }

            public override AutopilotStep Drive(FlightCtrlState s)
            {
                if (Orbit.PeA < -0.1 * MainBody.Radius)
                {
                    Core.Thrust.ThrustOff();
                    return new FinalDescent(Core);
                }

                Core.Attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT_HORIZONTAL, Core.Landing);
                if (Core.Attitude.attitudeAngleFromTarget() < 5)
                    Core.Thrust.TargetThrottle = 1;
                else
                    Core.Thrust.ThrustOff();

                Status = Localizer.Format("#MechJeb_LandingGuidance_Status16"); //"Doing deorbit burn."

                return this;
            }
        }
    }
}
