using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuMech
{
    //A class to record flight data, currently deltaV and time
    //Todo: add launch phase angle measurement
    //Todo: decide whether to keep separate "total" and "since mark" records
    //Todo: record RCS dV expended?
    class MechJebModuleFlightRecorder : ComputerModule
    {
        public MechJebModuleFlightRecorder(MechJebCore core) : base(core) { priority = 2000; }

        public double markUT;
        public double deltaVExpended;
        public double dragLosses;
        public double gravityLosses;
        public double steeringLosses;

        public void Mark()
        {
            markUT = vesselState.time;
            deltaVExpended = dragLosses = gravityLosses = steeringLosses = 0;
        }

        public override void OnStart(PartModule.StartState state)
        {
            Mark();
        }

        public override void Drive(FlightCtrlState s)
        {
            deltaVExpended += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle);
            gravityLosses += vesselState.deltaT * Vector3d.Dot(-vesselState.velocityVesselSurfaceUnit, vesselState.gravityForce);
            gravityLosses -= vesselState.deltaT * Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.up * vesselState.radius * Math.Pow(2 * Math.PI / part.vessel.mainBody.rotationPeriod, 2));
            steeringLosses += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle) * (1 - Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.forward));
            dragLosses += vesselState.deltaT * mainBody.DragAccel(vesselState.CoM, vesselState.velocityVesselOrbit, vesselState.massDrag / vesselState.mass).magnitude;
        }
    }
}
