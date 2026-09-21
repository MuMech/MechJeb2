extern alias JetBrainsAnnotations;
using System;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;
using Object = UnityEngine.Object;
using RecordType = MuMech.MechJebModuleFlightRecorder.RecordType;

namespace MuMech
{
    public class MechJebModuleFlightRecorderGraph : DisplayModule
    {
        private const int ScaleTicks = 11;
        private const string DISPLAYED_GRAPHS_NODE = "displayedGraphs";

        public struct graphState
        {
            public double   minimum;
            public double   maximum;
            public string[] labels;
            public double[] labelsPos;
            public int      labelsActive;
        }

        // One row per graph the user can tick, in on-screen order. NewRow breaks the checkbox row.
        private readonly struct GraphInfo
        {
            public readonly RecordType Type;
            public readonly Color      Color;
            public readonly string     Label;      // checkbox label (localization tag, or a literal)
            public readonly string     ScaleLabel; // label in the scale picker
            public readonly bool       NewRow;

            public GraphInfo(RecordType type, Color color, string label, string scaleLabel, bool newRow = false)
            {
                Type       = type;
                Color      = color;
                Label      = label;
                ScaleLabel = scaleLabel;
                NewRow     = newRow;
            }
        }

        // Blue, Navy, Teal, Magenta, Purple all have poor contrast on black or against other colors here
        // Maybe some of them could be lightened up, but many of the lighter variants are already in this list.
        private static readonly GraphInfo[] graphInfos =
        {
            new GraphInfo(RecordType.ALTITUDE_ASL,     XKCDColors.White,      "#MechJeb_Flightrecord_checkbox4", "#MechJeb_Flightrecord_checkbox18"),                // Altitude / ASL
            new GraphInfo(RecordType.ALTITUDE_TRUE,    XKCDColors.Grey,       "#MechJeb_Flightrecord_checkbox5", "#MechJeb_Flightrecord_checkbox19"),                // True Altitude / AGL
            new GraphInfo(RecordType.ACCELERATION,     XKCDColors.LightRed,   "#MechJeb_Flightrecord_checkbox6", "#MechJeb_Flightrecord_checkbox20"),                // Acceleration / Acc
            new GraphInfo(RecordType.SPEED_SURFACE,    XKCDColors.Yellow,     "#MechJeb_Flightrecord_checkbox7", "#MechJeb_Flightrecord_checkbox21"),                // Surface speed / SrfVel
            new GraphInfo(RecordType.SPEED_ORBITAL,    XKCDColors.Apricot,    "#MechJeb_Flightrecord_checkbox8", "#MechJeb_Flightrecord_checkbox22"),                // Orbital speed / ObtVel
            new GraphInfo(RecordType.MASS,             XKCDColors.Pink,       "#MechJeb_Flightrecord_checkbox9", "#MechJeb_Flightrecord_checkbox23"),                // Mass
            new GraphInfo(RecordType.Q,                XKCDColors.Cyan,       "#MechJeb_Flightrecord_checkbox10", "#MechJeb_Flightrecord_checkbox24", newRow: true), // Q
            new GraphInfo(RecordType.AO_A,             XKCDColors.Lavender,   "#MechJeb_Flightrecord_checkbox11", "#MechJeb_Flightrecord_checkbox25"),               // AoA
            new GraphInfo(RecordType.AO_S,             XKCDColors.Lime,       "#MechJeb_Flightrecord_checkbox12", "#MechJeb_Flightrecord_checkbox26"),               // AoS
            new GraphInfo(RecordType.AO_D,             XKCDColors.Orange,     "#MechJeb_Flightrecord_checkbox13", "#MechJeb_Flightrecord_checkbox27"),               // AoD
            new GraphInfo(RecordType.PITCH,            XKCDColors.Mint,       "#MechJeb_Flightrecord_checkbox14", "#MechJeb_Flightrecord_checkbox28"),               // Pitch
            new GraphInfo(RecordType.DELTA_V_EXPENDED, XKCDColors.Beige,      "ΔV",                              "ΔV"),                                              // ΔV
            new GraphInfo(RecordType.GRAVITY_LOSSES,   XKCDColors.Green,      "#MechJeb_Flightrecord_checkbox15", "#MechJeb_Flightrecord_checkbox29"),               // Gravity Loss
            new GraphInfo(RecordType.DRAG_LOSSES,      XKCDColors.LightBrown, "#MechJeb_Flightrecord_checkbox16", "#MechJeb_Flightrecord_checkbox30"),               // Drag Loss
            new GraphInfo(RecordType.STEERING_LOSSES,  XKCDColors.Cerise,     "#MechJeb_Flightrecord_checkbox17", "#MechJeb_Flightrecord_checkbox31"),               // Steering Loss
        };

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool downrange = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool realAtmo;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool stages;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int hSize = 4;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int vSize = 2;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool autoScale = true;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int timeScale;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int downrangeScale;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public int scaleIdx;

        public bool ascentPath = false;

        private static Texture2D backgroundTexture;
        private CelestialBody oldMainBody;
        private static readonly int typeCount = Enum.GetValues(typeof(RecordType)).Length;

        private readonly graphState[] graphStates;

        // Which graphs the user has ticked, indexed by (int)RecordType. Persisted by name; see OnSave/OnLoad.
        private readonly bool[] displayed;

        private double lastMaximumAltitude;
        private readonly double precision = 0.2;

        private int width = 512;
        private int height = 256;

        private bool paused;

        private float hPos;

        private bool follow = true;

        private MechJebModuleFlightRecorder recorder;

        public MechJebModuleFlightRecorderGraph(MechJebCore core)
            : base(core)
        {
            Priority = 2000;
            graphStates = new graphState[typeCount];
            displayed   = new bool[typeCount];
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);

            if (global == null)
                return;

            ConfigNode node = global.AddNode(DISPLAYED_GRAPHS_NODE);

            foreach (GraphInfo g in graphInfos)
                node.AddValue(g.Type.ToString(), displayed[(int)g.Type]);
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (global == null || !global.HasNode(DISPLAYED_GRAPHS_NODE))
                return;

            ConfigNode node = global.GetNode(DISPLAYED_GRAPHS_NODE);

            // Keyed by name so that reordering or extending RecordType cannot scramble the saved flags.
            // Anything unrecognized or malformed is skipped rather than thrown: an exception here would
            // cost the module every one of its settings, since MechJebCore catches OnLoad per module.
            for (int i = 0; i < node.values.Count; i++)
            {
                ConfigNode.Value v = node.values[i];

                if (Enum.TryParse(v.name, out RecordType recordType) && Enum.IsDefined(typeof(RecordType), recordType) &&
                    bool.TryParse(v.value, out bool display))
                    displayed[(int)recordType] = display;
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsEditor)
                return;

            width = 128 * hSize;
            height = 128 * vSize;
            recorder = Core.GetComputerModule<MechJebModuleFlightRecorder>();
            ResetScale();
        }

        protected override void WindowGUI(int windowID)
        {
            //DefaultAscentPath path = (DefaultAscentPath)core.GetComputerModule<MechJebModuleAscentAutopilot>().ascentPath;

            if (oldMainBody != MainBody || lastMaximumAltitude != graphStates[(int)RecordType.ALTITUDE_ASL].maximum ||
                height != backgroundTexture.height)
            {
                UpdateScale();
                lastMaximumAltitude = graphStates[(int)RecordType.ALTITUDE_ASL].maximum;

                if (backgroundTexture == null || height != backgroundTexture.height)
                {
                    Object.Destroy(backgroundTexture);
                    backgroundTexture = new Texture2D(1, height);
                }

                MechJebModuleAscentClassicPathMenu.UpdateAtmoTexture(backgroundTexture, Vessel.mainBody, lastMaximumAltitude, realAtmo);
                oldMainBody = MainBody;
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(paused ? Localizer.Format("#MechJeb_Flightrecord_Button1_1") : Localizer.Format("#MechJeb_Flightrecord_Button1_2"),
                    GuiUtils.LayoutNoExpandWidth)) //"Resume""Pause"
            {
                paused = !paused;
            }

            if (GUILayout.Button(
                    downrange ? Localizer.Format("#MechJeb_Flightrecord_Button2_1") : Localizer.Format("#MechJeb_Flightrecord_Button2_2"),
                    GuiUtils.LayoutNoExpandWidth)) //"Downrange""Time"
            {
                downrange = !downrange;
            }

            //GUILayout.Label("Size " + (8 * typeCount * recorder.history.Length >> 10).ToString() + "kB", GuiUtils.LayoutNoExpandWidth);

            GUILayout.Label(Localizer.Format("#MechJeb_Flightrecord_Label1", GuiUtils.TimeToDHMS(recorder.TimeSinceMark)),
                GuiUtils.LayoutNoExpandWidth); //Time <<1>>

            GUILayout.Label(Localizer.Format("#MechJeb_Flightrecord_Label2", recorder.History[recorder.HistoryIdx].DownRange.ToSI()) + "m",
                GuiUtils.LayoutNoExpandWidth); //Downrange <<1>>

            //GUILayout.Label("", GuiUtils.LayoutExpandWidth);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Localizer.Format("#MechJeb_Flightrecord_Button3"), GuiUtils.LayoutNoExpandWidth)) //"Mark"
            {
                ResetScale(); // TODO : should check something else to catch Mark calls from other code
                recorder.Mark();
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_Flightrecord_Button4"), GuiUtils.LayoutNoExpandWidth)) //"Reset Scale"
            {
                ResetScale();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            autoScale = GUILayout.Toggle(autoScale, Localizer.Format("#MechJeb_Flightrecord_checkbox1"), GuiUtils.LayoutNoExpandWidth); //Auto Scale

            if (!autoScale && GUILayout.Button("-", GuiUtils.LayoutNoExpandWidth))
            {
                if (downrange)
                    downrangeScale--;
                else
                    timeScale--;
            }

            float maxX = (float)(downrange
                ? recorder.Maximums[(int)RecordType.DOWN_RANGE]
                : recorder.Maximums[(int)RecordType.TIME_SINCE_MARK]);

            double maxXScaled = (downrange ? maxX : maxX / precision) / width;
            double autoScaleX = Math.Max(Math.Ceiling(Math.Log(maxXScaled, 2)), 0);
            double manualScaleX = downrange ? downrangeScale : timeScale;
            double activeScaleX = autoScale ? autoScaleX : manualScaleX;

            double scaleX = downrange ? Math.Pow(2, activeScaleX) : precision * Math.Pow(2, activeScaleX);

            GUILayout.Label(downrange ? scaleX.ToSI(2) + "m/px" : GuiUtils.TimeToDHMS(scaleX, 1) + "/px", GuiUtils.LayoutNoExpandWidth);

            if (!autoScale && GUILayout.Button("+", GuiUtils.LayoutNoExpandWidth))
            {
                if (downrange)
                    downrangeScale++;
                else
                    timeScale++;
            }

            if (GUILayout.Button("-", GuiUtils.LayoutNoExpandWidth))
            {
                hSize--;
            }

            GUILayout.Label(width.ToString(), GuiUtils.LayoutNoExpandWidth);

            if (GUILayout.Button("+", GuiUtils.LayoutNoExpandWidth))
            {
                hSize++;
            }

            GUILayout.Label("x", GuiUtils.LayoutNoExpandWidth);

            if (GUILayout.Button("-", GuiUtils.LayoutNoExpandWidth))
            {
                vSize--;
            }

            GUILayout.Label(height.ToString(), GuiUtils.LayoutNoExpandWidth);

            if (GUILayout.Button("+", GuiUtils.LayoutNoExpandWidth))
            {
                vSize++;
            }

            timeScale = Mathf.Clamp(timeScale, 0, 20);
            downrangeScale = Mathf.Clamp(downrangeScale, 0, 20);
            hSize = Mathf.Clamp(hSize, 1, 20);
            vSize = Mathf.Clamp(vSize, 1, 10);

            bool oldRealAtmo = realAtmo;

            realAtmo = GUILayout.Toggle(realAtmo, Localizer.Format("#MechJeb_Flightrecord_checkbox2"), GuiUtils.LayoutNoExpandWidth); //Real Atmo

            if (oldRealAtmo != realAtmo)
                MechJebModuleAscentClassicPathMenu.UpdateAtmoTexture(backgroundTexture, Vessel.mainBody, lastMaximumAltitude, realAtmo);

            //GUILayout.Label("", GuiUtils.LayoutExpandWidth);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(Localizer.Format("#MechJeb_Flightrecord_Button5"), GuiUtils.LayoutNoExpandWidth)) //CSV
            {
                recorder.DumpCsv();
            }

            GUILayout.Label(
                Localizer.Format("#MechJeb_Flightrecord_Label3", (100 * recorder.HistoryIdx / (float)recorder.History.Length).ToString("F1")),
                GuiUtils.LayoutNoExpandWidth); //Storage: <<1>> %

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            Color color = GUI.color;

            //ascentPath = GUILayout.Toggle(ascentPath, "Ascent path", GuiUtils.LayoutNoExpandWidth);
            stages = GUILayout.Toggle(stages, Localizer.Format("#MechJeb_Flightrecord_checkbox3"), GuiUtils.LayoutNoExpandWidth); //"Stages"

            foreach (GraphInfo g in graphInfos)
            {
                if (g.NewRow)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                GUI.color = g.Color;
                displayed[(int)g.Type] =
                    GUILayout.Toggle(displayed[(int)g.Type], Localizer.Format(g.Label), GuiUtils.LayoutNoExpandWidth);
            }

            GUI.color = color;

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            GUILayout.BeginHorizontal();

            GUILayout.Space(50);

            GUILayout.Box(Texture2D.blackTexture, GuiUtils.LayoutWidth(width), GUILayout.Height(height));
            Rect r = GUILayoutUtility.GetLastRect();

            DrawScaleLabels(r);

            GUILayout.BeginVertical();

            //GUILayout.Label("X " + hPos.ToSI(2) + " " + (hPos+scaleX*width).ToSI(2), GuiUtils.LayoutWidth(110));

            color = GUI.color;

            if (!displayed[scaleIdx])
            {
                int newIdx = 0;
                while (newIdx < typeCount && !displayed[newIdx])
                {
                    newIdx++;
                }

                if (newIdx == typeCount)
                    newIdx = 0;
                scaleIdx = newIdx;
            }

            foreach (GraphInfo g in graphInfos)
            {
                if (!displayed[(int)g.Type])
                    continue;

                GUI.color = g.Color;
                if (GUILayout.Toggle(scaleIdx == (int)g.Type, Localizer.Format(g.ScaleLabel), GuiUtils.LayoutExpandWidth))
                    scaleIdx = (int)g.Type;
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
            follow = GUILayout.Toggle(follow, "", GuiUtils.LayoutNoExpandWidth);

            GUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
            {
                UpdateScale();

                if (displayed[(int)RecordType.ALTITUDE_ASL] || displayed[(int)RecordType.ALTITUDE_TRUE])
                    GUI.DrawTexture(r, backgroundTexture, ScaleMode.StretchToFill);

                if (stages)
                    DrawnStages(r, scaleX, downrange);

                foreach (GraphInfo g in graphInfos)
                {
                    if (displayed[(int)g.Type])
                        DrawnPath(r, g.Type, hPos, scaleX, downrange, g.Color);
                }

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

            graphState state = graphStates[scaleIdx];
            if (state.labels == null)
                return;

            int count = state.labelsActive;
            double invScaleY = height / (state.maximum - state.minimum);
            float yBase = r.yMax + (float)(state.minimum * invScaleY);
            for (int i = 0; i < count; i++)
            {
                GUI.Label(new Rect(r.xMin - w, yBase - (float)(invScaleY * state.labelsPos[i]) - h * 0.5f, w, h), state.labels[i],
                    GuiUtils.MiddleRightLabel);
            }
        }

        private void DrawnPath(Rect r, RecordType type, float minimum, double scaleX, bool downRange, Color color)
        {
            if (recorder.History.Length <= 2 || recorder.HistoryIdx == 0)
                return;

            graphState graphState = graphStates[(int)type];

            double scaleY = (graphState.maximum - graphState.minimum) / height;

            double invScaleX = 1 / scaleX;
            double invScaleY = 1 / scaleY;

            float xBase = (float)(r.xMin - minimum * invScaleX);
            float yBase = r.yMax + (float)(graphState.minimum * invScaleY);

            int t = 0;
            while (t < recorder.HistoryIdx && t < recorder.History.Length &&
                   xBase + (float)((downRange ? recorder.History[t].DownRange : recorder.History[t].TimeSinceMark) * invScaleX) <= r.xMin)
            {
                t++;
            }

            var p1 = new Vector2(xBase + (float)((downRange ? recorder.History[t].DownRange : recorder.History[t].TimeSinceMark) * invScaleX),
                yBase - (float)(recorder.History[t][type] * invScaleY));
            var p2 = new Vector2();

            while (t <= recorder.HistoryIdx && t < recorder.History.Length)
            {
                MechJebModuleFlightRecorder.RecordStruct rec = recorder.History[t];
                p2.x = xBase + (float)((downRange ? rec.DownRange : rec.TimeSinceMark) * invScaleX);
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
            if (recorder.History.Length <= 2 || recorder.HistoryIdx == 0)
                return;

            int lastStage = recorder.History[0].CurrentStage;

            var p1 = new Vector2(0, r.yMin);
            var p2 = new Vector2(0, r.yMax);

            int t = 1;
            while (t <= recorder.HistoryIdx && t < recorder.History.Length)
            {
                MechJebModuleFlightRecorder.RecordStruct rec = recorder.History[t];
                if (rec.CurrentStage != lastStage)
                {
                    lastStage = rec.CurrentStage;
                    p1.x = r.xMin + (float)((downRange ? rec.DownRange : rec.TimeSinceMark) / scaleX);
                    p2.x = p1.x;

                    if (r.Contains(p1))
                    {
                        Drawing.DrawLine(p1, p2, new Color(0.5f, 0.5f, 0.5f), 1, false);
                    }
                }

                t++;
            }
        }

        private void UpdateScale()
        {
            if (recorder.HistoryIdx == 0)
                ResetScale();

            for (int t = 0; t < typeCount; t++)
            {
                bool change = false;

                if (graphStates[t].maximum < recorder.Maximums[t])
                {
                    change = true;
                    graphStates[t].maximum = recorder.Maximums[t] + Math.Abs(recorder.Maximums[t] * 0.2);
                }

                if (graphStates[t].minimum > recorder.Minimums[t])
                {
                    change = true;
                    graphStates[t].minimum = recorder.Minimums[t] - Math.Abs(recorder.Minimums[t] * 0.2);
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
            graphStates[(int)RecordType.ALTITUDE_ASL].minimum = 0;
            graphStates[(int)RecordType.DOWN_RANGE].minimum = 0;
            graphStates[(int)RecordType.ACCELERATION].minimum = 0;
            graphStates[(int)RecordType.SPEED_SURFACE].minimum = 0;
            graphStates[(int)RecordType.SPEED_ORBITAL].minimum = 0;
            graphStates[(int)RecordType.MASS].minimum = 0;
            graphStates[(int)RecordType.Q].minimum = 0;
            graphStates[(int)RecordType.AO_A].minimum = -5;
            graphStates[(int)RecordType.AO_S].minimum = -5;
            graphStates[(int)RecordType.AO_D].minimum = 0; // is never negative
            graphStates[(int)RecordType.ALTITUDE_TRUE].minimum = 0;
            graphStates[(int)RecordType.PITCH].minimum = 0;
            graphStates[(int)RecordType.DELTA_V_EXPENDED].minimum = 0;
            graphStates[(int)RecordType.GRAVITY_LOSSES].minimum = 0;
            graphStates[(int)RecordType.DRAG_LOSSES].minimum = 0;
            graphStates[(int)RecordType.STEERING_LOSSES].minimum = 0;

            graphStates[(int)RecordType.ALTITUDE_ASL].maximum =
                MainBody != null && MainBody.atmosphere ? MainBody.RealMaxAtmosphereAltitude() : 10000.0;
            graphStates[(int)RecordType.DOWN_RANGE].maximum = 500;
            graphStates[(int)RecordType.ACCELERATION].maximum = 2;
            graphStates[(int)RecordType.SPEED_SURFACE].maximum = 300;
            graphStates[(int)RecordType.SPEED_ORBITAL].maximum = 300;
            graphStates[(int)RecordType.MASS].maximum = 5;
            graphStates[(int)RecordType.Q].maximum = 1000;
            graphStates[(int)RecordType.AO_A].maximum = 5;
            graphStates[(int)RecordType.AO_S].maximum = 5;
            graphStates[(int)RecordType.AO_D].maximum = 5;
            graphStates[(int)RecordType.ALTITUDE_TRUE].maximum = 100;
            graphStates[(int)RecordType.PITCH].maximum = 90;
            graphStates[(int)RecordType.DELTA_V_EXPENDED].maximum = 100;
            graphStates[(int)RecordType.GRAVITY_LOSSES].maximum = 100;
            graphStates[(int)RecordType.DRAG_LOSSES].maximum = 100;
            graphStates[(int)RecordType.STEERING_LOSSES].maximum = 100;
        }

        private double heckbertNiceNum(double x, bool round)
        {
            int exp = (int)Math.Log10(x);
            double f = x / Math.Pow(10.0, exp);
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

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(400), GUILayout.Height(300) };

        public override string GetName() => Localizer.Format("#MechJeb_Flightrecord_title"); //"Flight Recorder"

        public override string IconName() => "Flight Recorder";
    }
}
