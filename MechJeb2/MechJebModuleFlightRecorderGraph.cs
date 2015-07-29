using System;
using UnityEngine;
using Object = UnityEngine.Object;
using recordType = MuMech.MechJebModuleFlightRecorder.recordType;


namespace MuMech
{
    //A class to record flight data, currently deltaV and time
    //TODO: add launch phase angle measurement
    //TODO: decide whether to keep separate "total" and "since mark" records
    //TODO: record RCS dV expended?
    //TODO: make records persistent
    public class MechJebModuleFlightRecorderGraph : DisplayModule
    {

        public struct graphState
        {
            public double minimum;
            public double maximum;
            public bool display;

            public void Reset()
            {
                minimum = 0;
                maximum = 0;
            }
        }

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

        public bool ascentPath = false;

        static Texture2D backgroundTexture = new Texture2D(1, 512);
        private CelestialBody oldMainBody;
        private static readonly int typeCount = Enum.GetValues(typeof(recordType)).Length;

        private graphState[] graphStates;
        private double lastMaximumAltitude;
        private double precision = 0.2;

        private int width = 512;
        private int height = 256;

        private bool paused = false;

        private MechJebModuleFlightRecorder recorder;

        public MechJebModuleFlightRecorderGraph(MechJebCore core)
            : base(core)
        {
            priority = 2000;
            graphStates = new graphState[typeCount];
        }

        public override void OnStart(PartModule.StartState state)
        {
            width = 128 * hSize;
            height = 128 * vSize;
            recorder = core.GetComputerModule<MechJebModuleFlightRecorder>();
            ResetScale();
        }

        protected override void WindowGUI(int windowID)
        {
            //DefaultAscentPath path = (DefaultAscentPath)core.GetComputerModule<MechJebModuleAscentAutopilot>().ascentPath;

            if (oldMainBody != mainBody || lastMaximumAltitude != graphStates[(int)recordType.AltitudeASL].maximum || height != backgroundTexture.height)
            {
                UpdateScale();
                lastMaximumAltitude = graphStates[(int)recordType.AltitudeASL].maximum;

                if (height != backgroundTexture.height)
                {
                    Object.Destroy(backgroundTexture);
                    backgroundTexture = new Texture2D(1, height);
                }

                MechJebModuleAscentPathEditor.UpdateAtmoTexture(backgroundTexture, vessel.mainBody, lastMaximumAltitude, realAtmo);
                oldMainBody = mainBody;
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(paused ? "Resume" : "Pause", GUILayout.ExpandWidth(false)))
            {
                paused = !paused;
            }

            if (GUILayout.Button(downrange ? "Downrange" : "Time", GUILayout.ExpandWidth(false)))
            {
                downrange = !downrange;
            }

            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                hSize--;
            }

            GUILayout.Label(width.ToString(), GUILayout.ExpandWidth(false));

            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                hSize++;
            }

            GUILayout.Label("x", GUILayout.ExpandWidth(false));

            if (GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                vSize--;
            }

            GUILayout.Label(height.ToString(), GUILayout.ExpandWidth(false));

            if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                vSize++;
            }

            hSize = Mathf.Clamp(hSize, 1, 20);
            vSize = Mathf.Clamp(vSize, 1, 10);

            bool oldRrealAtmo = realAtmo;

            realAtmo = GUILayout.Toggle(realAtmo, "Real Atmo", GUILayout.ExpandWidth(false));

            if (oldRrealAtmo != realAtmo)
                MechJebModuleAscentPathEditor.UpdateAtmoTexture(backgroundTexture, vessel.mainBody, lastMaximumAltitude, realAtmo);

            GUILayout.Label("Capacity " + (100 * (recorder.historyIdx) / (float)recorder.history.Length).ToString("F1") + "%", GUILayout.ExpandWidth(false));

            GUILayout.Label("Size " + (8 * typeCount * recorder.history.Length >> 10).ToString() + "kB", GUILayout.ExpandWidth(false));

            GUILayout.Label("AoA " + graphStates[(int)recordType.AoA].minimum.ToString("F2") + " " + graphStates[(int)recordType.AoA].maximum.ToString("F2"), GUILayout.ExpandWidth(false));

            GUILayout.Label("Time " + recorder.timeSinceMark.ToString("F0"), GUILayout.ExpandWidth(false));

            GUILayout.Label("Downrange " + MuUtils.ToSI(recorder.history[recorder.historyIdx].downRange) + "m", GUILayout.ExpandWidth(false));

            GUILayout.Label("", GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Mark", GUILayout.ExpandWidth(false)))
            {
                ResetScale(); // TODO : should check something else to catch Mark calls from other code
                recorder.Mark();
            }

            if (GUILayout.Button("Rst", GUILayout.ExpandWidth(false)))
            {
                ResetScale();

            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            Color color = GUI.color;

            //ascentPath = GUILayout.Toggle(ascentPath, "Ascent path", GUILayout.ExpandWidth(false));
            stages = GUILayout.Toggle(stages, "Stages", GUILayout.ExpandWidth(false));
            GUI.color = Color.white;
            graphStates[(int)recordType.AltitudeASL].display = GUILayout.Toggle(graphStates[(int)recordType.AltitudeASL].display, "Altitude", GUILayout.ExpandWidth(false));

            GUI.color = Color.grey;
            graphStates[(int)recordType.AltitudeTrue].display = GUILayout.Toggle(graphStates[(int)recordType.AltitudeTrue].display, "True Altitude", GUILayout.ExpandWidth(false));

            GUI.color = Color.red;
            graphStates[(int)recordType.Acceleration].display = GUILayout.Toggle(graphStates[(int)recordType.Acceleration].display, "Acceleration", GUILayout.ExpandWidth(false));
            GUI.color = Color.yellow;
            graphStates[(int)recordType.SpeedSurface].display = GUILayout.Toggle(graphStates[(int)recordType.SpeedSurface].display, "Surface speed", GUILayout.ExpandWidth(false));
            GUI.color = Color.magenta;
            graphStates[(int)recordType.SpeedOrbital].display = GUILayout.Toggle(graphStates[(int)recordType.SpeedOrbital].display, "Orbital speed", GUILayout.ExpandWidth(false));
            GUI.color = Color.cyan;
            graphStates[(int)recordType.Q].display = GUILayout.Toggle(graphStates[(int)recordType.Q].display, "Q", GUILayout.ExpandWidth(false));

            GUI.color = Color.green;
            graphStates[(int)recordType.AoA].display = GUILayout.Toggle(graphStates[(int)recordType.AoA].display, "AoA", GUILayout.ExpandWidth(false));

            GUI.color = XKCDColors.GreenTeal;
            graphStates[(int)recordType.Pitch].display = GUILayout.Toggle(graphStates[(int)recordType.Pitch].display, "Pitch", GUILayout.ExpandWidth(false));

            GUI.color = XKCDColors.CandyPink;
            graphStates[(int)recordType.Mass].display = GUILayout.Toggle(graphStates[(int)recordType.Mass].display, "Mass", GUILayout.ExpandWidth(false));

            GUI.color = color;

            GUILayout.EndHorizontal();

            GUILayout.Box(Texture2D.blackTexture, GUILayout.Width(width), GUILayout.Height(height));

            if (Event.current.type == EventType.Repaint)
            {
                Rect r = GUILayoutUtility.GetLastRect();

                UpdateScale();

                GUI.DrawTexture(r, backgroundTexture, ScaleMode.StretchToFill);

                //r.xMin += GUI.skin.box.margin.left;
                //r.yMin += GUI.skin.box.margin.top;
                //
                //r.xMax -= GUI.skin.box.margin.right;
                //r.yMax -= GUI.skin.box.margin.bottom;
                double maxDownRange = Math.Max(recorder.maximums[(int)recordType.DownRange], 500);
                double hScale = downrange ? (maxDownRange - recorder.minimums[(int)recordType.DownRange]) / width : precision;

                if (stages)
                    DrawnStages(r, hScale, downrange);

                if (graphStates[(int)recordType.AltitudeASL].display)
                    DrawnPath(r, recordType.AltitudeASL, hScale, downrange, Color.white);
                if (graphStates[(int)recordType.AltitudeTrue].display)
                    DrawnPath(r, recordType.AltitudeTrue, hScale, downrange, Color.grey);
                if (graphStates[(int)recordType.Acceleration].display)
                    DrawnPath(r, recordType.Acceleration, hScale, downrange, Color.red);
                if (graphStates[(int)recordType.SpeedSurface].display)
                    DrawnPath(r, recordType.SpeedSurface, hScale, downrange, Color.yellow);
                if (graphStates[(int)recordType.SpeedOrbital].display)
                    DrawnPath(r, recordType.SpeedOrbital, hScale, downrange, Color.magenta);
                if (graphStates[(int)recordType.Q].display)
                    DrawnPath(r, recordType.Q, hScale, downrange, Color.cyan);
                if (graphStates[(int)recordType.AoA].display)
                    DrawnPath(r, recordType.AoA, hScale, downrange, Color.green);

                if (graphStates[(int)recordType.Pitch].display)
                    DrawnPath(r, recordType.Pitch, hScale, downrange, XKCDColors.GreenTeal);

                if (graphStates[(int)recordType.Mass].display)
                    DrawnPath(r, recordType.Mass, hScale, downrange, XKCDColors.CandyPink);

                // Fix : the scales are different so the result is not usefull
                //if (ascentPath)
                //    MechJebModuleAscentPathEditor.DrawnPath(r, (float)hScale, (float)graphStates[(int)recordType.AltitudeASL].scale, path, Color.gray);

                width = 128 * hSize;
                height = 128 * vSize;
            }
            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private void DrawnPath(Rect r, recordType type, double scaleX, bool downRange, Color color)
        {
            if (recorder.history.Length <= 2 || recorder.historyIdx == 0)
                return;

            graphState graphState = graphStates[(int)type];

            double scaleY = (graphState.maximum - graphState.minimum) / height;

            float yBase = r.yMax + (float)(graphState.minimum / scaleY);

            Vector2 p1 = new Vector2(r.xMin + (float)((downRange ? recorder.history[0].downRange : recorder.history[0].timeSinceMark) / scaleX), yBase - (float)(recorder.history[0][type] / scaleY));
            Vector2 p2 = new Vector2();

            int t = 1;
            while (t <= recorder.historyIdx && t < recorder.history.Length)
            {
                var rec = recorder.history[t];
                p2.x = r.xMin + (float)((downRange ? rec.downRange : rec.timeSinceMark) / scaleX);
                p2.y = yBase - (float)(rec[type] / scaleY);

                // skip 0 length line but always drawn the first 2 points
                if (r.Contains(p2) && ((p1 - p2).sqrMagnitude >= 1.0 || t < 2))
                {
                    Drawing.DrawLine(p1, p2, color, 2, true);

                    p1.x = p2.x;
                    p1.y = p2.y;
                }
                t++;
            }
        }

        private void DrawnStages(Rect r, double scaleX, bool downRange)
        {
            if (recorder.history.Length <= 2 || recorder.historyIdx == 0)
                return;

            int lastStage = recorder.history[0].currentStage;

            Vector2 p1 = new Vector2(0, r.yMin);
            Vector2 p2 = new Vector2(0, r.yMax);

            int t = 1;
            while (t <= recorder.historyIdx && t < recorder.history.Length)
            {
                var rec = recorder.history[t];
                if (rec.currentStage != lastStage)
                {
                    lastStage = rec.currentStage;
                    p1.x = r.xMin + (float)((downRange ? rec.downRange : rec.timeSinceMark) / scaleX);
                    p2.x = p1.x;

                    if (r.Contains(p1))
                    {
                        Drawing.DrawLine(p1, p2, new Color(0.2f, 0.2f, 0.2f), 1, false);
                    }
                }
                t++;
            }
        }

        private void ResetScale()
        {
            // Avoid min = max and set sane minimums
            graphStates[(int)recordType.AltitudeASL].minimum = 0;
            graphStates[(int)recordType.DownRange].minimum = 0;
            graphStates[(int)recordType.Acceleration].minimum = 0;
            graphStates[(int)recordType.SpeedOrbital].minimum = 0;
            graphStates[(int)recordType.SpeedSurface].minimum = 0;
            graphStates[(int)recordType.Q].minimum = 0;
            graphStates[(int)recordType.AoA].minimum = -5;
            graphStates[(int)recordType.AltitudeTrue].minimum = 0;
            graphStates[(int)recordType.Pitch].minimum = 0;
            graphStates[(int)recordType.Mass].minimum = 0;

            graphStates[(int)recordType.AltitudeASL].maximum = mainBody != null && mainBody.atmosphere ? mainBody.RealMaxAtmosphereAltitude() : 10000.0;
            graphStates[(int)recordType.DownRange].maximum = 500;
            graphStates[(int)recordType.Acceleration].maximum = 2;
            graphStates[(int)recordType.SpeedOrbital].maximum = 300;
            graphStates[(int)recordType.SpeedSurface].maximum = 300;
            graphStates[(int)recordType.Q].maximum = 1000;
            graphStates[(int)recordType.AoA].maximum = 5;
            graphStates[(int)recordType.AltitudeTrue].maximum = 100;
            graphStates[(int)recordType.Pitch].maximum = 90;
            graphStates[(int)recordType.Mass].maximum = 5;
        }

        private void UpdateScale()
        {
            if (recorder.historyIdx == 0)
                ResetScale();

            for (int t = 0; t < typeCount; t++)
            {
                if (graphStates[t].maximum < recorder.maximums[t])
                    graphStates[t].maximum = recorder.maximums[t] + Math.Abs(recorder.maximums[t] * 0.2);

                if (graphStates[t].minimum > recorder.minimums[t])
                    graphStates[t].minimum = recorder.minimums[t] - Math.Abs(recorder.minimums[t] * 0.2);
            }

            double AoA = Math.Max(Math.Abs(graphStates[(int)recordType.AoA].minimum), Math.Abs(graphStates[(int)recordType.AoA].maximum));

            graphStates[(int)recordType.AoA].minimum = -AoA;
            graphStates[(int)recordType.AoA].maximum = AoA;
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
