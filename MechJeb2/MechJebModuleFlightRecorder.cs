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
        // TODO : this is already nearly an array so use a list and allow to add any generic ValueInfoItem
        public struct record
        {
            public double timeSinceMark;
            public double altitudeASL;
            public double downRange;
            public double speedSurface;
            public double speedOrbital;
            public double acceleration;
            public double Q;
            public double AoA;
            public double altitudeTrue;
            public double pitch;
            public double mass;
            public int currentStage;

            public double this[recordType type]
            {
                get
                {
                    switch (type)
                    {
                        case recordType.TimeSinceMark:
                            return timeSinceMark;
                        case recordType.AltitudeASL:
                            return altitudeASL;
                        case recordType.DownRange:
                            return downRange;
                        case recordType.SpeedSurface:
                            return speedSurface;
                        case recordType.SpeedOrbital:
                            return speedOrbital;
                        case recordType.Acceleration:
                            return acceleration;
                        case recordType.Q:
                            return Q;
                        case recordType.AoA:
                            return AoA;
                        case recordType.AltitudeTrue:
                            return altitudeTrue;
                        case recordType.Pitch:
                            return pitch;
                        case recordType.Mass:
                            return mass;
                        default:
                            return 0;
                    }
                }
            }
        }

        public enum recordType
        {
            TimeSinceMark,
            AltitudeASL,
            DownRange,
            SpeedSurface,
            SpeedOrbital,
            Acceleration,
            Q,
            AoA,
            AltitudeTrue,
            Pitch,
            Mass
        }

        public record[] history = new record[3000];

        public int historyIdx = -1;

        [Persistent(pass = (int)Pass.Global)]
        public bool downrange = true;

        [Persistent(pass = (int)Pass.Global)]
        public bool realAtmo = false;

        [Persistent(pass = (int)Pass.Global)]
        public bool stages = false;

        [Persistent(pass = (int)Pass.Global)]
        public int hSize = 4;

        [Persistent(pass = (int)Pass.Global)]
        public int vSize = 2;

        private static readonly int typeCount = Enum.GetValues(typeof(recordType)).Length;

        public double[] maximums;
        public double[] minimums;

        private double precision = 0.2;

        private bool paused = false;

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
            timeSinceMark = 0;
            for (int t = 0; t < maximums.Length; t++)
            {
                minimums[t] = Double.MaxValue;
                maximums[t] = Double.MinValue;
            }
            historyIdx = 0;
            Record(historyIdx);
        }

        public MechJebModuleFlightRecorder(MechJebCore core)
            : base(core)
        {
            priority = 2000;
            maximums = new double[typeCount];
            minimums = new double[typeCount];
        }

        public override void OnStart(PartModule.StartState state)
        {
            this.users.Add(this); //flight recorder should always run.
        }

        private double lastRecordTime = 0;

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
            dragLosses += vesselState.deltaT * vesselState.drag;

            maxDragGees = Math.Max(maxDragGees, vesselState.drag / 9.81);

            double circularPeriod = 2 * Math.PI * vesselState.radius / OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, vesselState.radius);
            double angleTraversed = (vesselState.longitude - markLongitude) + 360 * (vesselState.time - markUT) / part.vessel.mainBody.rotationPeriod;
            phaseAngleFromMark = MuUtils.ClampDegrees360(360 * (vesselState.time - markUT) / circularPeriod - angleTraversed);

            if (paused)
                return;

            //int oldHistoryIdx = historyIdx;

            //historyIdx = Mathf.Min(Mathf.FloorToInt((float)(timeSinceMark / precision)), history.Length - 1);

            if (vesselState.time >= (lastRecordTime + precision) && historyIdx < history.Length - 1)
            {
                lastRecordTime = vesselState.time;
                historyIdx++;
                Record(historyIdx);
                //if (TimeWarp.WarpMode == TimeWarp.Modes.HIGH)
                //    print("WRP " + historyIdx + " " + history[historyIdx].downRange.ToString("F0") + " " + history[historyIdx].AoA.ToString("F2"));
                //else
                //{
                //    print("STD " + historyIdx + " " + history[historyIdx].downRange.ToString("F0") + " " + history[historyIdx].AoA.ToString("F2"));
                //}
            }
        }

        private void Record(int idx)
        {
            history[idx].timeSinceMark = timeSinceMark;
            history[idx].altitudeASL = vesselState.altitudeASL;
            history[idx].downRange = GroundDistanceFromMark();
            history[idx].speedSurface = vesselState.speedSurface;
            history[idx].speedOrbital = vesselState.speedOrbital;
            history[idx].acceleration = vessel.geeForce;
            history[idx].Q = vesselState.dynamicPressure;
            history[idx].altitudeTrue = vesselState.altitudeTrue;
            history[idx].pitch = vesselState.vesselPitch;
            history[idx].mass = vesselState.mass;

            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH)
            {
                history[idx].currentStage = vessel.currentStage;
                history[idx].AoA = vesselState.AoA;
            }
            else
            {
                history[idx].currentStage = idx > 0 ? history[idx - 1].currentStage : Staging.CurrentStage;
                history[idx].AoA = idx > 0 ? history[idx - 1].AoA : 0;
            }
            for (int t = 0; t < typeCount; t++)
            {
                double current = history[idx][(recordType)t];
                minimums[t] = Math.Min(minimums[t], current);
                maximums[t] = Math.Max(maximums[t], current);
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            deltaVExpended += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle);
            steeringLosses += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle) * (1 - Vector3d.Dot(vesselState.surfaceVelocity.normalized, vesselState.forward));
        }

        // TODO : not do the full scale update as often
        private void UpdateMinMax()
        {
            for (int t = 0; t < typeCount; t++)
            {
                minimums[t] = Double.MaxValue;
                maximums[t] = Double.MinValue;
            }

            for (int i = 0; i <= historyIdx; i++)
            {
                record r = history[i];
                for (int t = 0; t < typeCount; t++)
                {
                    minimums[t] = Math.Min(minimums[t], r[(recordType)t]);
                    maximums[t] = Math.Max(maximums[t], r[(recordType)t]);
                }
            }
        }

    }
}
