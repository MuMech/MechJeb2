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
    //Todo: make records persistent
    class MechJebModuleFlightRecorder : ComputerModule
    {
        public MechJebModuleFlightRecorder(MechJebCore core) : base(core) { priority = 2000; }

        [ValueInfoItem(name="Mark UT", time=true)]
        public double markUT;
        [ValueInfoItem(name="Time since mark", time=true)]
        public double timeSinceMark;
        [ValueInfoItem(name="ΔV expended", units= "m/s")]
        public double deltaVExpended;
        [ValueInfoItem(name="Drag losses", units = "m/s")]
        public double dragLosses;
        [ValueInfoItem(name="Gravity losses", units = "m/s")]
        public double gravityLosses;
        [ValueInfoItem(name="Steering losses", units="m/s")]
        public double steeringLosses;
        [ValueInfoItem(name="Phase angle from mark", units="º")]
        public double phaseAngleFromMark;
        double markLongitude;

        [ActionInfoItem(name="MARK")]
        public void Mark()
        {
            markUT = vesselState.time;
            deltaVExpended = dragLosses = gravityLosses = steeringLosses = 0;
            phaseAngleFromMark = 0;
            markLongitude = vesselState.longitude;
        }

        public override void OnStart(PartModule.StartState state)
        {
            Mark();
            this.enabled = true; //flight recorder should always run.
        }

        public override void OnFixedUpdate()
        {
            timeSinceMark = vesselState.time - markUT;

            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                Mark(); //keep resetting stats until we launch
                return;
            }

            gravityLosses += vesselState.deltaT * Vector3d.Dot(-vesselState.velocityVesselSurfaceUnit, vesselState.gravityForce);
            gravityLosses -= vesselState.deltaT * Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.up * vesselState.radius * Math.Pow(2 * Math.PI / part.vessel.mainBody.rotationPeriod, 2));
            dragLosses += vesselState.deltaT * mainBody.DragAccel(vesselState.CoM, vesselState.velocityVesselOrbit, vesselState.massDrag / vesselState.mass).magnitude;

            double circularPeriod = 2 * Math.PI * vesselState.radius / OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, vesselState.radius);
            double angleTraversed = (vesselState.longitude - markLongitude) + 360 * (vesselState.time - markUT) / part.vessel.mainBody.rotationPeriod;
            phaseAngleFromMark = MuUtils.ClampDegrees360(360 * (vesselState.time - markUT) / circularPeriod - angleTraversed);

        }

        public override void Drive(FlightCtrlState s)
        {
            deltaVExpended += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle);
            steeringLosses += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle) * (1 - Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.forward));
        }
    }
}
