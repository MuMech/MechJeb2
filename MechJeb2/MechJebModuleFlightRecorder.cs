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
    public class MechJebModuleFlightRecorder : DisplayModule
    {
        public MechJebModuleFlightRecorder(MechJebCore core) : base(core) { priority = 2000; }

        public struct record
        {
            public double timeSinceMark;
            public double altitudeASL;
            public double downRange;
            public double speedSurface;
            public double speedOrbital;
            public double acceleration;
            public double Q;


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
            Q
        }

        public struct graphState
        {
            public double minimum;
            public double maximum;
            public bool display;

            public double scale
            {
                get { return (maximum - minimum) / backgroundTexture.height; }
            }
        }

        public record[] history = new record[3000];

        public int historyIdx = -1;

        [Persistent(pass = (int)Pass.Global)]
        public bool downrange = true;

        public bool ascentPath = false;

        static Texture2D backgroundTexture = new Texture2D(1024, 512);
        private CelestialBody oldMainBody;
        private int typeCount = Enum.GetValues(typeof(recordType)).Length;
        private double[] maximums;
        private double[] minimums;
        private graphState[] graphStates;
        private double lastMaximumAltitude;
        private double precision = 0.2;

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
            if (historyIdx > 0)
            {
                history = new record[1000];
                maximums = new double[typeCount];
                minimums = new double[typeCount];
                graphStates = new graphState[typeCount];
            }
            historyIdx = 0;
        }

        public override void OnStart(PartModule.StartState state)
        {
            this.users.Add(this); //flight recorder should always run.

            maximums = new double[typeCount];
            minimums = new double[typeCount];
            graphStates = new graphState[typeCount];
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
            dragLosses += vesselState.deltaT * vesselState.drag;

            maxDragGees = Math.Max(maxDragGees, vesselState.drag / 9.81);

            double circularPeriod = 2 * Math.PI * vesselState.radius / OrbitalManeuverCalculator.CircularOrbitSpeed(mainBody, vesselState.radius);
            double angleTraversed = (vesselState.longitude - markLongitude) + 360 * (vesselState.time - markUT) / part.vessel.mainBody.rotationPeriod;
            phaseAngleFromMark = MuUtils.ClampDegrees360(360 * (vesselState.time - markUT) / circularPeriod - angleTraversed);

            int oldHistoryIdx = historyIdx;

            historyIdx = Mathf.Min(Mathf.FloorToInt((float)(timeSinceMark / precision)), history.Length);

            if (historyIdx != oldHistoryIdx && historyIdx < history.Length)
            {
                history[historyIdx].timeSinceMark = timeSinceMark;
                history[historyIdx].altitudeASL = vesselState.altitudeASL;
                history[historyIdx].downRange = GroundDistanceFromMark();
                history[historyIdx].speedSurface = vesselState.speedSurface;
                history[historyIdx].speedOrbital = vesselState.speedOrbital;
                history[historyIdx].acceleration = vessel.geeForce;
                history[historyIdx].Q = vesselState.dynamicPressure;

                UpdateScale();
            }
        }

        public override void Drive(FlightCtrlState s)
        {
            deltaVExpended += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle);
            steeringLosses += vesselState.deltaT * vesselState.ThrustAccel(s.mainThrottle) * (1 - Vector3d.Dot(vesselState.surfaceVelocity.normalized, vesselState.forward));
        }

        protected override void WindowGUI(int windowID)
        {
            DefaultAscentPath path = (DefaultAscentPath)core.GetComputerModule<MechJebModuleAscentAutopilot>().ascentPath;

            if (oldMainBody != mainBody || lastMaximumAltitude != graphStates[(int)recordType.AltitudeASL].maximum)
            {
                UpdateScale();
                lastMaximumAltitude = graphStates[(int)recordType.AltitudeASL].maximum;
                MechJebModuleAscentPathEditor.UpdateAtmoTexture(backgroundTexture, mainBody, lastMaximumAltitude);
                oldMainBody = mainBody;
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Mark", GUILayout.ExpandWidth(false)))
                Mark();

            if (GUILayout.Button(downrange ? "Downrange" : "Time", GUILayout.ExpandWidth(false)))
            {
                downrange = !downrange;
            }

            GUILayout.Label("Capacity " + (100 * (historyIdx + 1) / (float)history.Length).ToString("F1") + "%", GUILayout.ExpandWidth(false));

            GUILayout.Label("Size " + (8 * typeCount * history.Length >> 10) + "kB", GUILayout.ExpandWidth(false));

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            //ascentPath = GUILayout.Toggle(ascentPath, "Ascent path", GUILayout.ExpandWidth(false));
            graphStates[(int)recordType.AltitudeASL].display = GUILayout.Toggle(graphStates[(int)recordType.AltitudeASL].display, "Altitude", GUILayout.ExpandWidth(false));
            graphStates[(int)recordType.Acceleration].display = GUILayout.Toggle(graphStates[(int)recordType.Acceleration].display, "Acceleration", GUILayout.ExpandWidth(false));
            graphStates[(int)recordType.SpeedSurface].display = GUILayout.Toggle(graphStates[(int)recordType.SpeedSurface].display, "Surface speed", GUILayout.ExpandWidth(false));
            graphStates[(int)recordType.SpeedOrbital].display = GUILayout.Toggle(graphStates[(int)recordType.SpeedOrbital].display, "Orbital speed", GUILayout.ExpandWidth(false));
            graphStates[(int)recordType.Q].display = GUILayout.Toggle(graphStates[(int)recordType.Q].display, "Q", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();


            GUILayout.Box(backgroundTexture);

            if (Event.current.type == EventType.Repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                r.xMin += GUI.skin.box.margin.left;
                r.yMin += GUI.skin.box.margin.top;

                r.xMax -= GUI.skin.box.margin.right;
                r.yMax -= GUI.skin.box.margin.bottom;

                double hScale = downrange ? (maximums[(int)recordType.DownRange] - minimums[(int)recordType.DownRange]) / backgroundTexture.width : precision;

                if (graphStates[(int)recordType.AltitudeASL].display)
                    DrawnPath(r, recordType.AltitudeASL, hScale, graphStates[(int)recordType.AltitudeASL].scale, downrange, Color.white);
                if (graphStates[(int)recordType.Acceleration].display)
                    DrawnPath(r, recordType.Acceleration, hScale, graphStates[(int)recordType.Acceleration].scale, downrange, Color.red);
                if (graphStates[(int)recordType.SpeedSurface].display)
                    DrawnPath(r, recordType.SpeedSurface, hScale, graphStates[(int)recordType.SpeedSurface].scale, downrange, Color.yellow);
                if (graphStates[(int)recordType.SpeedOrbital].display)
                    DrawnPath(r, recordType.SpeedOrbital, hScale, graphStates[(int)recordType.SpeedOrbital].scale, downrange, Color.magenta);
                if (graphStates[(int)recordType.Q].display)
                    DrawnPath(r, recordType.Q, hScale, graphStates[(int)recordType.Q].scale, downrange, Color.cyan);

                // Fix : the scales are different so the result is not usefull
                //if (ascentPath)
                //    MechJebModuleAscentPathEditor.DrawnPath(r, (float)hScale, (float)graphStates[(int)recordType.AltitudeASL].scale, path, Color.gray);
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private void DrawnPath(Rect r, recordType type, double scaleX, double scaleY, bool downRange, Color color)
        {
            Vector2 p1 = new Vector2(r.xMin, r.yMax);
            Vector2 p2 = new Vector2();

            if (history.Length <= 2 || historyIdx == 0)
                return;

            int t = 1;

            p1 = new Vector2(r.xMin + (float)(history[0].downRange / scaleX), r.yMax - (float)(history[0][type] / scaleY));

            while (t <= historyIdx && t < history.Length)
            {
                var rec = history[t];
                p2.x = r.xMin + (float)((downRange ? rec.downRange : rec.timeSinceMark) / scaleX);
                p2.y = r.yMax - (float)(rec[type] / scaleY);

                if (r.Contains(p2) && (p1 - p2).sqrMagnitude >= 1.0 || t < 2)
                {
                    Drawing.DrawLine(p1, p2, color, 2, true);
                    p1.x = p2.x;
                    p1.y = p2.y;
                }

                t++;
            }
        }

        // TODO : not do the full scale update as often
        private void UpdateScale()
        {
            for (int t = 0; t < typeCount; t++)
            {
                maximums[t] = Double.MinValue;
                minimums[t] = Double.MaxValue;
            }

            maximums[(int)recordType.AltitudeASL] = Math.Max(30000.0, mainBody.RealMaxAtmosphereAltitude());
            maximums[(int)recordType.DownRange] = 1000;

            for (int i = 0; i <= historyIdx; i++)
            {
                record r = history[i];
                for (int t = 0; t < typeCount; t++)
                {
                    maximums[t] = Math.Max(maximums[t], r[(recordType)t]);
                    minimums[t] = Math.Min(minimums[t], r[(recordType)t]);
                }
            }

            for (int t = 0; t < typeCount; t++)
            {
                if (graphStates[t].maximum < maximums[t])
                    graphStates[t].maximum = maximums[t] + Math.Abs(maximums[t] * 0.2);

                if (graphStates[t].minimum > minimums[t])
                    graphStates[t].minimum = minimums[t] - Math.Abs(minimums[t] * 0.2);
            }
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(400), GUILayout.Height(300) };
        }

        public override string GetName()
        {
            return "Flight Recorder";
        }
    }
}
