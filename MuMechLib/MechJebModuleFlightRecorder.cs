using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

        [Persistent(pass=(int)Pass.Local)]
        [ValueInfoItem("Mark UT", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)]
        public double markUT = 0;

        [ValueInfoItem("Time since mark", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)]
        public double timeSinceMark = 0;
        
        [Persistent(pass = (int)Pass.Local)]
        [ValueInfoItem("ΔV expended", InfoItem.Category.Recorder, format = "F0", units = "m/s")]
        public double deltaVExpended = 0;

        [Persistent(pass = (int)Pass.Local)]
        [ValueInfoItem("Drag losses", InfoItem.Category.Recorder, format = "F0", units = "m/s")]
        public double dragLosses = 0;

        [Persistent(pass = (int)Pass.Local)]
        [ValueInfoItem("Gravity losses", InfoItem.Category.Recorder, format = "F0", units = "m/s")]
        public double gravityLosses = 0;

        [Persistent(pass = (int)Pass.Local)]
        [ValueInfoItem("Steering losses", InfoItem.Category.Recorder, format = "F0", units = "m/s")]
        public double steeringLosses = 0;

        [ValueInfoItem("Phase angle from mark", InfoItem.Category.Recorder, format = "F2", units = "º")]
        public double phaseAngleFromMark = 0;

        [Persistent(pass = (int)Pass.Local)]
        double markLongitude = 0;

        [Persistent(pass = (int)Pass.Local)]
        [ValueInfoItem("Max drag gees", InfoItem.Category.Recorder, format = "F2")]
        public double maxDragGees = 0;

        [ActionInfoItem("MARK", InfoItem.Category.Recorder)]
        public void Mark()
        {
            markUT = vesselState.time;
            deltaVExpended = dragLosses = gravityLosses = steeringLosses = 0;
            phaseAngleFromMark = 0;
            markLongitude = vesselState.longitude;
            maxDragGees = 0;
        }

        public override void OnStart(PartModule.StartState state)
        {
            this.enabled = true; //flight recorder should always run.
        }
        
        public override void OnFixedUpdate()
        {
            if (markUT == 0) Mark();

            timeSinceMark = vesselState.time - markUT;

            if (vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                Mark(); //keep resetting stats until we launch
                return;
            }

            gravityLosses += vesselState.deltaT * Vector3d.Dot(-vesselState.velocityVesselSurfaceUnit, vesselState.gravityForce);
            gravityLosses -= vesselState.deltaT * Vector3d.Dot(vesselState.velocityVesselSurfaceUnit, vesselState.up * vesselState.radius * Math.Pow(2 * Math.PI / part.vessel.mainBody.rotationPeriod, 2));
            double dragAccel = mainBody.DragAccel(vesselState.CoM, vesselState.velocityVesselOrbit, vesselState.massDrag / vesselState.mass).magnitude;
            dragLosses += vesselState.deltaT * dragAccel;

            maxDragGees = Math.Max(maxDragGees, dragAccel / 9.81);

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
