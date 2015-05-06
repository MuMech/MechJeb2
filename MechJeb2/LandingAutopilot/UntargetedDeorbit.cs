using System;
using UnityEngine;

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
                if (orbit.PeA < -0.1 * mainBody.Radius)
                {
                    core.thrust.targetThrottle = 0;
                    return new FinalDescent(core);
                }

                core.attitude.attitudeTo(Vector3d.back, AttitudeReference.ORBIT_HORIZONTAL, core.landing);
                if (core.attitude.attitudeAngleFromTarget() < 5)
                    core.thrust.targetThrottle = 1;
                else
                    core.thrust.targetThrottle = 0;

                status = "Doing deorbit burn.";

                return this;
            }
        }
    }
}
