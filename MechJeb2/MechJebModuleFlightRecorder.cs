using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //A class to record flight data, currently deltaV and time
    //TODO: add launch phase angle measurement
    //TODO: decide whether to keep separate "total" and "since mark" records
    //TODO: record RCS dV expended?
    //TODO: make records persistent
    public class MechJebModuleFlightRecorder : ComputerModule
    {
        public MechJebModuleFlightRecorder(MechJebCore core) : base(core) { priority = 2000; }

        [Persistent(pass = (int)Pass.Local)]
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
        [ValueInfoItem("Mark latitude", InfoItem.Category.Recorder, format = ValueInfoItem.ANGLE_NS)]
        public double markLatitude = 0;

        [Persistent(pass = (int)Pass.Local)]
        [ValueInfoItem("Mark longitude", InfoItem.Category.Recorder, format = ValueInfoItem.ANGLE_EW)]
        public double markLongitude = 0;

        [Persistent(pass = (int)Pass.Local)]
        [ValueInfoItem("Mark altitude ASL", InfoItem.Category.Recorder, format = ValueInfoItem.SI, units = "m")]
        public double markAltitude = 0;

        [Persistent(pass = (int)Pass.Local)]
        public int markBodyIndex = 1;

        [ValueInfoItem("Mark body", InfoItem.Category.Recorder)]
        public string MarkBody() { return FlightGlobals.Bodies[markBodyIndex].bodyName; }

        [ValueInfoItem("Distance from mark", InfoItem.Category.Recorder, format = ValueInfoItem.SI, units = "m")]
        public double DistanceFromMark()
        {
            return Vector3d.Distance(vesselState.CoM, FlightGlobals.Bodies[markBodyIndex].GetWorldSurfacePosition(markLatitude, markLongitude, markAltitude));
        }

        [ValueInfoItem("Downrange distance", InfoItem.Category.Recorder, format = ValueInfoItem.SI, units = "m")]
        public double GroundDistanceFromMark()
        {
            CelestialBody markBody = FlightGlobals.Bodies[markBodyIndex];
            Vector3d markVector = markBody.GetSurfaceNVector(markLatitude, markLongitude);
            Vector3d vesselVector = vesselState.CoM - markBody.transform.position;
            return markBody.Radius * Vector3d.Angle(markVector, vesselVector) * Math.PI / 180;
        }

        [Persistent(pass = (int)Pass.Local)]
        [ValueInfoItem("Max drag gees", InfoItem.Category.Recorder, format = "F2")]
        public double maxDragGees = 0;

        [ActionInfoItem("MARK", InfoItem.Category.Recorder)]
        public void Mark()
        {
            markUT = vesselState.time;
            deltaVExpended = dragLosses = gravityLosses = steeringLosses = 0;
            phaseAngleFromMark = 0;
            markLatitude = vesselState.latitude;
            markLongitude = vesselState.longitude;
            markAltitude = vesselState.altitudeASL;
            markBodyIndex = FlightGlobals.Bodies.IndexOf(mainBody);
            maxDragGees = 0;
        }

        public override void OnStart(PartModule.StartState state)
        {
            this.users.Add(this); //flight recorder should always run.
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

            gravityLosses += vesselState.deltaT * Vector3d.Dot(-vesselState.surfaceVelocity.normalized, vesselState.gravityForce);
            gravityLosses -= vesselState.deltaT * Vector3d.Dot(vesselState.surfaceVelocity.normalized, vesselState.up * vesselState.radius * Math.Pow(2 * Math.PI / part.vessel.mainBody.rotationPeriod, 2));
            double dragAccel = mainBody.DragAccel(vesselState.CoM, vesselState.orbitalVelocity, vesselState.massDrag / vesselState.mass).magnitude;
            dragLosses += vesselState.deltaT * dragAccel;

            maxDragGees = Math.Max(maxDragGees, dragAccel / 9.81);

            double circularPeriod = 2 * Math.PI * vesselState.radius / OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, vesselState.radius);
            double angleTraversed = (vesselState.longitude - markLongitude) + 360 * (vesselState.time - markUT) / part.vessel.mainBody.rotationPeriod;
            phaseAngleFromMark = MuUtils.ClampDegrees360(360 * (vesselState.time - markUT) / circularPeriod - angleTraversed);
        }

        public override void Drive(FlightCtrlState s)
        {
            deltaVExpended += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle);
            steeringLosses += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle) * (1 - Vector3d.Dot(vesselState.surfaceVelocity.normalized, vesselState.forward));
        }
    }
}
