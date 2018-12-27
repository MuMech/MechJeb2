using System;
using UnityEngine;
using Object = UnityEngine.Object;
using recordType = MuMech.MechJebModuleFlightRecorder.recordType;


namespace MuMech
{
    public class MechJebModuleFlightRecorderGraph : DisplayModule
    {
        private const int ScaleTicks = 11;

        public struct graphState
        {
            public double minimum;
            public double maximum;
            public string[] labels;
            public double[] labelsPos;
            public int labelsActive;
            public bool display;

            public void Reset()
            {
                minimum = 0;
                maximum = 0;
                labels = new string[ScaleTicks];
                labelsPos = new double[ScaleTicks];

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

        [Persistent(pass = (int)Pass.Global)]
        public bool autoScale = true;

        [Persistent(pass = (int)Pass.Global)]
        public int timeScale = 0;

        [Persistent(pass = (int)Pass.Global)]
        public int downrangeScale = 0;

        [Persistent(pass = (int)Pass.Global)]
        public int scaleIdx = 0;

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

        private float hPos = 0;

        private bool follow = true;

        private MechJebModuleFlightRecorder recorder;

        public MechJebModuleFlightRecorderGraph(MechJebCore core)
            : base(core)
        {
            priority = 2000;
            graphStates = new graphState[typeCount];
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

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

                MechJebModuleAscentClassicMenu.UpdateAtmoTexture(backgroundTexture, vessel.mainBody, lastMaximumAltitude, realAtmo);
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

            //GUILayout.Label("Size " + (8 * typeCount * recorder.history.Length >> 10).ToString() + "kB", GUILayout.ExpandWidth(false));

            GUILayout.Label("Time " + GuiUtils.TimeToDHMS(recorder.timeSinceMark), GUILayout.ExpandWidth(false));

            GUILayout.Label("Downrange " + MuUtils.ToSI(recorder.history[recorder.historyIdx].downRange, -2) + "m", GUILayout.ExpandWidth(false));

            //GUILayout.Label("", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Mark", GUILayout.ExpandWidth(false)))
            {
                ResetScale(); // TODO : should check something else to catch Mark calls from other code
                recorder.Mark();
            }

            if (GUILayout.Button("Reset Scale", GUILayout.ExpandWidth(false)))
            {
                ResetScale();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            autoScale = GUILayout.Toggle(autoScale, "Auto Scale", GUILayout.ExpandWidth(false));

            if (!autoScale && GUILayout.Button("-", GUILayout.ExpandWidth(false)))
            {
                if (downrange)
                    downrangeScale--;
                else
                    timeScale--;
            }

            float maxX = (float)(downrange ? recorder.maximums[(int)recordType.DownRange] : recorder.maximums[(int)recordType.TimeSinceMark]);

            double maxXScaled = (downrange ? maxX : maxX / precision) / width;
            double autoScaleX = Math.Max(Math.Ceiling(Math.Log(maxXScaled, 2)), 0);
            double manualScaleX = downrange ? downrangeScale : timeScale;
            double activeScaleX = autoScale ? autoScaleX : manualScaleX;

            double scaleX = downrange ? Math.Pow(2, activeScaleX) : precision * Math.Pow(2, activeScaleX);

            GUILayout.Label((downrange ? MuUtils.ToSI(scaleX, -1, 2) + "m/px" : GuiUtils.TimeToDHMS(scaleX, 1) + "/px"), GUILayout.ExpandWidth(false));

            if (!autoScale && GUILayout.Button("+", GUILayout.ExpandWidth(false)))
            {
                if (downrange)
                    downrangeScale++;
                else
                    timeScale++;
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

            timeScale = Mathf.Clamp(timeScale, 0, 20);
            downrangeScale = Mathf.Clamp(downrangeScale, 0, 20);
            hSize = Mathf.Clamp(hSize, 1, 20);
            vSize = Mathf.Clamp(vSize, 1, 10);

            bool oldRealAtmo = realAtmo;

            realAtmo = GUILayout.Toggle(realAtmo, "Real Atmo", GUILayout.ExpandWidth(false));

            if (oldRealAtmo != realAtmo)
                MechJebModuleAscentClassicMenu.UpdateAtmoTexture(backgroundTexture, vessel.mainBody, lastMaximumAltitude, realAtmo);

            //GUILayout.Label("", GUILayout.ExpandWidth(true));
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("CSV", GUILayout.ExpandWidth(false)))
            {
                recorder.DumpCSV();
            }

            GUILayout.Label("Storage: " + (100 * (recorder.historyIdx) / (float)recorder.history.Length).ToString("F1") + "%", GUILayout.ExpandWidth(false));

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

            GUI.color = Color.green;
            graphStates[(int)recordType.AoS].display = GUILayout.Toggle(graphStates[(int)recordType.AoS].display, "AoS", GUILayout.ExpandWidth(false));

            GUI.color = Color.green;
            graphStates[(int)recordType.AoD].display = GUILayout.Toggle(graphStates[(int)recordType.AoD].display, "AoD", GUILayout.ExpandWidth(false));

            GUI.color = XKCDColors.GreenTeal;
            graphStates[(int)recordType.Pitch].display = GUILayout.Toggle(graphStates[(int)recordType.Pitch].display, "Pitch", GUILayout.ExpandWidth(false));

            GUI.color = XKCDColors.CandyPink;
            graphStates[(int)recordType.Mass].display = GUILayout.Toggle(graphStates[(int)recordType.Mass].display, "Mass", GUILayout.ExpandWidth(false));

            GUI.color = color;

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.Space(50);

            GUILayout.Box(Texture2D.blackTexture, GUILayout.Width(width), GUILayout.Height(height));
            Rect r = GUILayoutUtility.GetLastRect();

            DrawScaleLabels(r);

            GUILayout.BeginVertical();

            //GUILayout.Label("X " + MuUtils.ToSI(hPos, -1, 2) + " " + MuUtils.ToSI(hPos + scaleX * width, -1, 2), GUILayout.Width(110));

            color = GUI.color;

            if (!graphStates[scaleIdx].display)
            {
                int newIdx = 0;
                while (newIdx < typeCount && !graphStates[newIdx].display)
                {
                    newIdx++;
                }
                if (newIdx == typeCount)
                    newIdx = 0;
                scaleIdx = newIdx;
            }

            if (graphStates[(int)recordType.AltitudeASL].display)
            {
                GUI.color = Color.white;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AltitudeASL, "ASL " + MuUtils.ToSI(graphStates[(int)recordType.AltitudeASL].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AltitudeASL].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AltitudeASL, "ASL", GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.AltitudeASL;
            }

            if (graphStates[(int)recordType.AltitudeTrue].display)
            {
                GUI.color = Color.grey;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AltitudeTrue, "AGL " + MuUtils.ToSI(graphStates[(int)recordType.AltitudeTrue].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AltitudeTrue].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AltitudeTrue, "AGL", GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.AltitudeTrue;
            }
            if (graphStates[(int)recordType.Acceleration].display)
            {
                GUI.color = Color.red;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.Acceleration, "Acc " + MuUtils.ToSI(graphStates[(int)recordType.Acceleration].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.Acceleration].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.Acceleration, "Acc" , GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.Acceleration;
            }
            if (graphStates[(int)recordType.SpeedSurface].display)
            {
                GUI.color = Color.yellow;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.SpeedSurface, "SrfVel " + MuUtils.ToSI(graphStates[(int)recordType.SpeedSurface].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.SpeedSurface].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.SpeedSurface, "SrfVel" , GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.SpeedSurface;
            }
            if (graphStates[(int)recordType.SpeedOrbital].display)
            {
                GUI.color = Color.magenta;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.SpeedOrbital, "ObtVel " + MuUtils.ToSI(graphStates[(int)recordType.SpeedOrbital].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.SpeedOrbital].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.SpeedOrbital, "ObtVel", GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.SpeedOrbital;
            }
            if (graphStates[(int)recordType.Q].display)
            {
                GUI.color = Color.cyan;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.Q, "Q " + MuUtils.ToSI(graphStates[(int)recordType.Q].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.Q].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.Q, "Q", GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.Q;
            }
            if (graphStates[(int)recordType.AoA].display)
            {
                GUI.color = Color.green;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AoA, "AoA " + MuUtils.ToSI(graphStates[(int)recordType.AoA].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AoA].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AoA, "AoA", GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.AoA;
            }
            if (graphStates[(int)recordType.AoS].display)
            {
                GUI.color = Color.green;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AoS, "AoS " + MuUtils.ToSI(graphStates[(int)recordType.AoS].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AoS].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AoS, "AoS", GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.AoS;
            }
            if (graphStates[(int)recordType.AoD].display)
            {
                GUI.color = Color.green;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.AoD, "AoD " + MuUtils.ToSI(graphStates[(int)recordType.AoD].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.AoD].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.AoD, "AoD", GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.AoD;
            }
            if (graphStates[(int)recordType.Pitch].display)
            {
                GUI.color = XKCDColors.GreenTeal;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.Pitch, "Pitch " + MuUtils.ToSI(graphStates[(int)recordType.Pitch].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.Pitch].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.Pitch, "Pitch" , GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.Pitch;
            }
            if (graphStates[(int)recordType.Mass].display)
            {
                GUI.color = XKCDColors.CandyPink;
                //if (GUILayout.Toggle(scaleIdx == (int)recordType.Mass, "Mass " + MuUtils.ToSI(graphStates[(int)recordType.Mass].minimum, -1, 3) + " " + MuUtils.ToSI(graphStates[(int)recordType.Mass].maximum, -1, 3), GUILayout.ExpandWidth(true)))
                if (GUILayout.Toggle(scaleIdx == (int)recordType.Mass, "Mass", GUILayout.ExpandWidth(true)))
                    scaleIdx = (int)recordType.Mass;
            }

            GUI.color = color;

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            float visibleX = (float)(width * scaleX);
            float rightValue = Mathf.Max(visibleX, maxX);

            if (follow)
                hPos = rightValue - visibleX;
            hPos = GUILayout.HorizontalScrollbar(hPos, visibleX, 0, rightValue);
            follow = GUILayout.Toggle(follow, "", GUILayout.ExpandWidth(false));

            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
            {
                UpdateScale();

                if (graphStates[(int)recordType.AltitudeASL].display || graphStates[(int)recordType.AltitudeTrue].display)
                    GUI.DrawTexture(r, backgroundTexture, ScaleMode.StretchToFill);

                if (stages)
                    DrawnStages(r, scaleX, downrange);

                if (graphStates[(int)recordType.AltitudeASL].display)
                    DrawnPath(r, recordType.AltitudeASL, hPos, scaleX, downrange, Color.white);
                if (graphStates[(int)recordType.AltitudeTrue].display)
                    DrawnPath(r, recordType.AltitudeTrue, hPos, scaleX, downrange, Color.grey);
                if (graphStates[(int)recordType.Acceleration].display)
                    DrawnPath(r, recordType.Acceleration, hPos, scaleX, downrange, Color.red);
                if (graphStates[(int)recordType.SpeedSurface].display)
                    DrawnPath(r, recordType.SpeedSurface, hPos, scaleX, downrange, Color.yellow);
                if (graphStates[(int)recordType.SpeedOrbital].display)
                    DrawnPath(r, recordType.SpeedOrbital, hPos, scaleX, downrange, Color.magenta);
                if (graphStates[(int)recordType.Q].display)
                    DrawnPath(r, recordType.Q, hPos, scaleX, downrange, Color.cyan);
                if (graphStates[(int)recordType.AoA].display)
                    DrawnPath(r, recordType.AoA, hPos, scaleX, downrange, Color.green);
                if (graphStates[(int)recordType.AoS].display)
                    DrawnPath(r, recordType.AoS, hPos, scaleX, downrange, Color.green);
                if (graphStates[(int)recordType.AoD].display)
                    DrawnPath(r, recordType.AoD, hPos, scaleX, downrange, Color.green);
                if (graphStates[(int)recordType.Pitch].display)
                    DrawnPath(r, recordType.Pitch, hPos, scaleX, downrange, XKCDColors.GreenTeal);
                if (graphStates[(int)recordType.Mass].display)
                    DrawnPath(r, recordType.Mass, hPos, scaleX, downrange, XKCDColors.CandyPink);

                // Fix : the scales are different so the result is not useful
                //if (ascentPath)
                //    MechJebModuleAscentPathEditor.DrawnPath(r, (float)hScale, (float)graphStates[(int)recordType.AltitudeASL].scale, path, Color.gray);

                width = 128 * hSize;
                height = 128 * vSize;
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        private void DrawScaleLabels(Rect r)
        {
            if (scaleIdx == 0)
                return;

            const int w = 80;
            const int h = 20;
            GUIStyle centeredStyle = new GUIStyle(GUI.skin.label) {alignment = TextAnchor.MiddleRight};

            graphState state = graphStates[scaleIdx];
            if (state.labels == null)
                return;

            int count = state.labelsActive;
            double invScaleY = height / (state.maximum - state.minimum);
            float yBase = r.yMax + (float) (state.minimum * invScaleY);
            for (int i = 0; i < count; i++)
            {
                GUI.Label(new Rect(r.xMin - w, yBase - (float)(invScaleY * state.labelsPos[i]) - h * 0.5f, w, h), state.labels[i], centeredStyle);
            }
        }

        private void DrawnPath(Rect r, recordType type, float minimum, double scaleX, bool downRange, Color color)
        {
            if (recorder.history.Length <= 2 || recorder.historyIdx == 0)
                return;

            graphState graphState = graphStates[(int)type];

            double scaleY = (graphState.maximum - graphState.minimum) / height;

            double invScaleX = 1 / scaleX;
            double invScaleY = 1 / scaleY;

            float xBase = (float) (r.xMin - (minimum * invScaleX));
            float yBase = r.yMax + (float)(graphState.minimum * invScaleY);

            int t = 0;
            while (t < recorder.historyIdx && t < recorder.history.Length &&
                (xBase + (float)((downRange ? recorder.history[t].downRange : recorder.history[t].timeSinceMark) * invScaleX)) <= r.xMin)
            {
                t++;
            }

            Vector2 p1 = new Vector2(xBase + (float)((downRange ? recorder.history[t].downRange : recorder.history[t].timeSinceMark) * invScaleX), yBase - (float)(recorder.history[t][type] * invScaleY));
            Vector2 p2 = new Vector2();

            while (t <= recorder.historyIdx && t < recorder.history.Length)
            {
                var rec = recorder.history[t];
                p2.x = xBase + (float)((downRange ? rec.downRange : rec.timeSinceMark) * invScaleX);
                p2.y = yBase - (float)(rec[type] * invScaleY);

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

        private void UpdateScale()
        {
            if (recorder.historyIdx == 0)
                ResetScale();

            for (int t = 0; t < typeCount; t++)
            {
                bool change = false;

                if (graphStates[t].maximum < recorder.maximums[t])
                {
                    change = true;
                    graphStates[t].maximum = recorder.maximums[t] + Math.Abs(recorder.maximums[t] * 0.2);
                }

                if (graphStates[t].minimum > recorder.minimums[t])
                {
                    change = true;
                    graphStates[t].minimum = recorder.minimums[t] - Math.Abs(recorder.minimums[t] * 0.2);
                }

                if (graphStates[t].labels == null)
                {
                    change = true;
                    graphStates[t].labels = new string[ScaleTicks];
                    graphStates[t].labelsPos = new double[ScaleTicks];
                }

                if (change)
                {
                    double maximum = graphStates[t].maximum;
                    double minimum = graphStates[t].minimum;
                    double range = heckbertNiceNum(maximum - minimum, false);
                    double step = heckbertNiceNum(range / (ScaleTicks - 1), true);

                    minimum = Math.Floor(minimum / step) * step;
                    maximum = Math.Ceiling(maximum / step) * step;
                    int digit = (int)Math.Max(-Math.Floor(Math.Log10(step)), 0);

                    double currX = minimum;
                    int i = 0;
                    while (currX <= maximum + 0.5 * step)
                    {
                        graphStates[t].labels[i] = currX.ToString("F" + digit);
                        graphStates[t].labelsPos[i] = currX;
                        currX += step;
                        i++;
                    }

                    graphStates[t].labelsActive = i;
                    graphStates[t].minimum = minimum;
                    graphStates[t].maximum = maximum;
                }
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
            graphStates[(int)recordType.AoS].minimum = -5;
            graphStates[(int)recordType.AoD].minimum = 0; // is never negative
            graphStates[(int)recordType.AltitudeTrue].minimum = 0;
            graphStates[(int)recordType.Pitch].minimum = 0;
            graphStates[(int)recordType.Mass].minimum = 0;
            graphStates[(int)recordType.GravityLosses].minimum = 0;
            graphStates[(int)recordType.DragLosses].minimum = 0;
            graphStates[(int)recordType.SteeringLosses].minimum = 0;
            graphStates[(int)recordType.DeltaVExpended].minimum = 0;

            graphStates[(int)recordType.AltitudeASL].maximum = mainBody != null && mainBody.atmosphere ? mainBody.RealMaxAtmosphereAltitude() : 10000.0;
            graphStates[(int)recordType.DownRange].maximum = 500;
            graphStates[(int)recordType.Acceleration].maximum = 2;
            graphStates[(int)recordType.SpeedOrbital].maximum = 300;
            graphStates[(int)recordType.SpeedSurface].maximum = 300;
            graphStates[(int)recordType.Q].maximum = 1000;
            graphStates[(int)recordType.AoA].maximum = 5;
            graphStates[(int)recordType.AoS].maximum = 5;
            graphStates[(int)recordType.AoD].maximum = 5;
            graphStates[(int)recordType.AltitudeTrue].maximum = 100;
            graphStates[(int)recordType.Pitch].maximum = 90;
            graphStates[(int)recordType.Mass].maximum = 5;
            graphStates[(int)recordType.GravityLosses].maximum = 100;
            graphStates[(int)recordType.DragLosses].maximum = 100;
            graphStates[(int)recordType.SteeringLosses].maximum = 100;
            graphStates[(int)recordType.DeltaVExpended].maximum = 100;
        }

        private double heckbertNiceNum(double x, bool round)
        {
            int exp = (int)Math.Log10(x);
            double f = x / (Math.Pow(10.0, exp));
            double nf = 1;

            if (round)
            {
                if (f < 1.5)
                    nf = 1;
                else if (f < 3)
                    nf = 2;
                else if (f < 7)
                    nf = 5;
                else
                    nf = 10;
            }
            else
            {
                if (f <= 1)
                    nf = 1;
                else if (f <= 2)
                    nf = 2;
                else if (f <= 5)
                    nf = 5;
                else
                    nf = 10;
            }
            return nf * Math.Pow(10.0, exp);

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
