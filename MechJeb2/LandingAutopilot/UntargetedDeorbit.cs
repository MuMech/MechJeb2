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
                    Core.thrust.targetThrottle = 0;
                    return new FinalDescent(Core);
                }

                Core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT_HORIZONTAL, Core.landing);
                Core.thrust.targetThrottle = Core.attitude.attitudeAngleFromTarget() < 5 ? 1 : 0;

                Status = Localizer.Format("#MechJeb_LandingGuidance_Status16"); //"Doing deorbit burn."

                return this;
            }
        }
    }
}
