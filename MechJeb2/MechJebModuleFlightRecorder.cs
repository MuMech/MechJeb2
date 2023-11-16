extern alias JetBrainsAnnotations;
using System;
using System.IO;
using JetBrainsAnnotations::JetBrains.Annotations;

namespace MuMech
{
    //A class to record flight data, currently deltaV and time
    //TODO: make records persistent
    [UsedImplicitly]
    public class MechJebModuleFlightRecorder : ComputerModule
    {
        // TODO : this is already nearly an array so use a list and allow to add any generic ValueInfoItem
        public struct RecordStruct
        {
            public double TimeSinceMark;
            public int    CurrentStage;
            public double AltitudeASL;
            public double DownRange;
            public double SpeedSurface;
            public double SpeedOrbital;
            public double Acceleration;
            public double Q;
            public double AoA;
            public double AoS;
            public double AoD;
            public double AltitudeTrue;
            public double Pitch;
            public double Mass;
            public double GravityLosses;
            public double DragLosses;
            public double SteeringLosses;
            public double DeltaVExpended;

            public double this[RecordType type]
            {
                get
                {
                    switch (type)
                    {
                        case RecordType.TIME_SINCE_MARK:
                            return TimeSinceMark;
                        case RecordType.CURRENT_STAGE:
                            return CurrentStage;
                        case RecordType.ALTITUDE_ASL:
                            return AltitudeASL;
                        case RecordType.DOWN_RANGE:
                            return DownRange;
                        case RecordType.SPEED_SURFACE:
                            return SpeedSurface;
                        case RecordType.SPEED_ORBITAL:
                            return SpeedOrbital;
                        case RecordType.MASS:
                            return Mass;
                        case RecordType.ACCELERATION:
                            return Acceleration;
                        case RecordType.Q:
                            return Q;
                        case RecordType.AO_A:
                            return AoA;
                        case RecordType.AO_S:
                            return AoS;
                        case RecordType.AO_D:
                            return AoD;
                        case RecordType.ALTITUDE_TRUE:
                            return AltitudeTrue;
                        case RecordType.PITCH:
                            return Pitch;
                        case RecordType.GRAVITY_LOSSES:
                            return GravityLosses;
                        case RecordType.DRAG_LOSSES:
                            return DragLosses;
                        case RecordType.STEERING_LOSSES:
                            return SteeringLosses;
                        case RecordType.DELTA_V_EXPENDED:
                            return DeltaVExpended;
                        default:
                            return 0;
                    }
                }
            }
        }

        public enum RecordType
        {
            TIME_SINCE_MARK,
            CURRENT_STAGE,
            ALTITUDE_ASL,
            DOWN_RANGE,
            SPEED_SURFACE,
            SPEED_ORBITAL,
            MASS,
            ACCELERATION,
            Q,
            AO_A,
            AO_S,
            AO_D,
            ALTITUDE_TRUE,
            PITCH,
            GRAVITY_LOSSES,
            DRAG_LOSSES,
            STEERING_LOSSES,
            DELTA_V_EXPENDED
        }

        public RecordStruct[] History = new RecordStruct[1];

        public int HistoryIdx = -1;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly int HistorySize = 3000;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly double Precision = 0.2;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Downrange = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool RealAtmo = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Stages = false;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int HSize = 4;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int VSize = 2;

        private static readonly int _typeCount = Enum.GetValues(typeof(RecordType)).Length;

        public readonly double[] Maximums;
        public readonly double[] Minimums;

        private readonly bool _paused = false;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_MarkUT", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)] //Mark UT
        public double MarkUT;

        [ValueInfoItem("#MechJeb_TimeSinceMark", InfoItem.Category.Recorder, format = ValueInfoItem.TIME)] //Time since mark
        public double TimeSinceMark;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_DVExpended", InfoItem.Category.Recorder, format = "F1", units = "m/s")] //ΔV expended
        public double DeltaVExpended;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_DragLosses", InfoItem.Category.Recorder, format = "F1", units = "m/s")] //Drag losses
        public double DragLosses;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_GravityLosses", InfoItem.Category.Recorder, format = "F1", units = "m/s")] //Gravity losses
        public double GravityLosses;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_SteeringLosses", InfoItem.Category.Recorder, format = "F1", units = "m/s")] //Steering losses
        public double SteeringLosses;

        [ValueInfoItem("#MechJeb_PhaseAngleFromMark", InfoItem.Category.Recorder, format = "F2", units = "º")] //Phase angle from mark
        public double PhaseAngleFromMark;

        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_MarkLAN", InfoItem.Category.Recorder, format = ValueInfoItem.ANGLE_EW)] //Mark LAN
        public double MarkLAN;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_MarkLatitude", InfoItem.Category.Recorder, format = ValueInfoItem.ANGLE_NS)] //Mark latitude
        public double MarkLatitude;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_MarkLongitude", InfoItem.Category.Recorder, format = ValueInfoItem.ANGLE_EW)] //Mark longitude
        public double MarkLongitude;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_MarkAltitudeASL", InfoItem.Category.Recorder, format = ValueInfoItem.SI, units = "m")] //Mark altitude ASL
        public double MarkAltitude;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        public int MarkBodyIndex = 1;

        [ValueInfoItem("#MechJeb_MarkBody", InfoItem.Category.Recorder)] //Mark body
        public string MarkBody() => FlightGlobals.Bodies[MarkBodyIndex].bodyName;

        [ValueInfoItem("#MechJeb_DistanceFromMark", InfoItem.Category.Recorder, format = ValueInfoItem.SI, units = "m")] //Distance from mark
        public double DistanceFromMark() =>
            Vector3d.Distance(VesselState.CoM,
                FlightGlobals.Bodies[MarkBodyIndex].GetWorldSurfacePosition(MarkLatitude, MarkLongitude, MarkAltitude) -
                FlightGlobals.Bodies[MarkBodyIndex].position);

        [UsedImplicitly]
        [ValueInfoItem("#MechJeb_DownrangeDistance", InfoItem.Category.Recorder, format = ValueInfoItem.SI, units = "m")] //Downrange distance
        public double GroundDistanceFromMark()
        {
            CelestialBody markBody = FlightGlobals.Bodies[MarkBodyIndex];
            Vector3d markVector = markBody.GetSurfaceNVector(MarkLatitude, MarkLongitude);
            Vector3d vesselVector = VesselState.CoM - markBody.transform.position;
            return markBody.Radius * Vector3d.Angle(markVector, vesselVector) * UtilMath.Deg2Rad;
        }

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.LOCAL)]
        [ValueInfoItem("#MechJeb_MaxDragGees", InfoItem.Category.Recorder, format = "F2")] //Max drag gees
        public double MaxDragGees;

        [ActionInfoItem("MARK", InfoItem.Category.Recorder)]
        public void Mark()
        {
            MarkUT             = VesselState.time;
            DeltaVExpended     = DragLosses = GravityLosses = SteeringLosses = 0;
            PhaseAngleFromMark = 0;
            MarkLatitude       = VesselState.latitude;
            MarkLongitude      = VesselState.longitude;
            MarkLAN            = VesselState.orbitLAN;
            MarkAltitude       = VesselState.altitudeASL;
            MarkBodyIndex      = FlightGlobals.Bodies.IndexOf(MainBody);
            MaxDragGees        = 0;
            TimeSinceMark      = 0;
            for (int t = 0; t < Maximums.Length; t++)
            {
                Minimums[t] = double.MaxValue;
                Maximums[t] = double.MinValue;
            }

            HistoryIdx = 0;
            Record(HistoryIdx);
        }

        public MechJebModuleFlightRecorder(MechJebCore core)
            : base(core)
        {
            Priority = 2000;
            Maximums = new double[_typeCount];
            Minimums = new double[_typeCount];
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (History.Length != HistorySize)
                History = new RecordStruct[HistorySize];
            Users.Add(this); //flight recorder should always run.
        }

        private double _lastRecordTime;

        public override void OnFixedUpdate()
        {
            if (MarkUT == 0) Mark();

            TimeSinceMark = VesselState.time - MarkUT;

            if (Vessel.situation == Vessel.Situations.PRELAUNCH)
            {
                Mark(); //keep resetting stats until we launch
                return;
            }

            GravityLosses  += VesselState.deltaT * Vector3d.Dot(-VesselState.orbitalVelocity.normalized, VesselState.gravityForce);
            DragLosses     += VesselState.deltaT * VesselState.drag;
            DeltaVExpended += VesselState.deltaT * VesselState.currentThrustAccel;
            SteeringLosses += VesselState.deltaT * VesselState.currentThrustAccel *
                              (1 - Vector3d.Dot(VesselState.orbitalVelocity.normalized, VesselState.forward));

            MaxDragGees = Math.Max(MaxDragGees, VesselState.drag / 9.81);

            double circularPeriod = Orbit.CircularOrbitPeriod();
            double angleTraversed = VesselState.longitude - MarkLongitude + 360 * (VesselState.time - MarkUT) / Part.vessel.mainBody.rotationPeriod;
            PhaseAngleFromMark = MuUtils.ClampDegrees360(360 * (VesselState.time - MarkUT) / circularPeriod - angleTraversed);

            if (_paused)
                return;

            //int oldHistoryIdx = historyIdx;

            //historyIdx = Mathf.Min(Mathf.FloorToInt((float)(timeSinceMark / precision)), history.Length - 1);

            if (VesselState.time >= _lastRecordTime + Precision && HistoryIdx < History.Length - 1)
            {
                _lastRecordTime = VesselState.time;
                HistoryIdx++;
                Record(HistoryIdx);
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
            History[idx].TimeSinceMark  = TimeSinceMark;
            History[idx].AltitudeASL    = VesselState.altitudeASL;
            History[idx].DownRange      = GroundDistanceFromMark();
            History[idx].SpeedSurface   = VesselState.speedSurface;
            History[idx].SpeedOrbital   = VesselState.speedOrbital;
            History[idx].Acceleration   = Vessel.geeForce;
            History[idx].Q              = VesselState.dynamicPressure;
            History[idx].AltitudeTrue   = VesselState.altitudeTrue;
            History[idx].Pitch          = VesselState.vesselPitch;
            History[idx].Mass           = VesselState.mass;
            History[idx].GravityLosses  = GravityLosses;
            History[idx].DragLosses     = DragLosses;
            History[idx].SteeringLosses = SteeringLosses;
            History[idx].DeltaVExpended = DeltaVExpended;

            if (TimeWarp.WarpMode != TimeWarp.Modes.HIGH)
            {
                History[idx].CurrentStage = Vessel.currentStage;
            }
            else
            {
                History[idx].CurrentStage = idx > 0 ? History[idx - 1].CurrentStage : Vessel.currentStage;
            }

            History[idx].AoA = VesselState.AoA;
            History[idx].AoS = VesselState.AoS;
            History[idx].AoD = VesselState.displacementAngle;
            for (int t = 0; t < _typeCount; t++)
            {
                double current = History[idx][(RecordType)t];
                Minimums[t] = Math.Min(Minimums[t], current);
                Maximums[t] = Math.Max(Maximums[t], current);
            }
        }

        public void DumpCsv()
        {
            string exportPath = KSPUtil.ApplicationRootPath + "/GameData/MechJeb2/Export/";

            if (!Directory.Exists(exportPath))
                Directory.CreateDirectory(exportPath);

            string vesselName = Vessel != null ? string.Join("_", Vessel.vesselName.Split(Path.GetInvalidFileNameChars())) : "";

            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            string path = exportPath + vesselName + "_" + timestamp + ".csv";
            using (var writer = new StreamWriter(path))
            {
                writer.WriteLine(string.Join(",", Enum.GetNames(typeof(RecordType))));

                for (int idx = 0; idx <= HistoryIdx; idx++)
                {
                    RecordStruct r = History[idx];
                    writer.Write(r[0]);
                    for (int i = 1; i < _typeCount; i++)
                    {
                        writer.Write(',');
                        writer.Write(r[(RecordType)i]);
                    }

                    writer.WriteLine();
                }
            }

            ScreenMessages.PostScreenMessage("Exported as\n" + path, 5);
        }
    }
}
