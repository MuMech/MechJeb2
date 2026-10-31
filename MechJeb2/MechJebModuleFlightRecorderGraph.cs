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
        internal const int ScaleTicks = 11;
        private const string DISPLAYED_GRAPHS_NODE = "displayedGraphs";

        public struct graphState
        {
            public double minimum;
            public double maximum;
            public string[] labels;
            public double[] labelsPos;
            public int labelsActive;
        }

        // One row per graph the user can tick, in on-screen order. NewRow breaks the checkbox row.
        internal readonly struct GraphInfo
        {
            public readonly RecordType Type;
            public readonly Color Color;
            public readonly string Label; // checkbox label (localization tag, or a literal)
            public readonly string ScaleLabel; // label in the scale picker
            public readonly bool NewRow;

            public GraphInfo(RecordType type, Color color, string label, string scaleLabel, bool newRow = false)
            {
                Type = type;
                Color = color;
                Label = label;
                ScaleLabel = scaleLabel;
                NewRow = newRow;
            }
        }

        // Blue, Navy, Teal, Magenta, Purple all have poor contrast on black or against other colors here
        // Maybe some of them could be lightened up, but many of the lighter variants are already in this list.
        internal static readonly GraphInfo[] graphInfos =
        {
            new GraphInfo(RecordType.ALTITUDE_ASL, XKCDColors.White, "#MechJeb_Flightrecord_checkbox4", "#MechJeb_Flightrecord_checkbox18"), // Altitude / ASL
            new GraphInfo(RecordType.ALTITUDE_TRUE, XKCDColors.Grey, "#MechJeb_Flightrecord_checkbox5", "#MechJeb_Flightrecord_checkbox19"), // True Altitude / AGL
            new GraphInfo(RecordType.ACCELERATION, XKCDColors.LightRed, "#MechJeb_Flightrecord_checkbox6", "#MechJeb_Flightrecord_checkbox20"), // Acceleration / Acc
            new GraphInfo(RecordType.SPEED_SURFACE, XKCDColors.Yellow, "#MechJeb_Flightrecord_checkbox7", "#MechJeb_Flightrecord_checkbox21"), // Surface speed / SrfVel
            new GraphInfo(RecordType.SPEED_ORBITAL, XKCDColors.Apricot, "#MechJeb_Flightrecord_checkbox8", "#MechJeb_Flightrecord_checkbox22"), // Orbital speed / ObtVel
            new GraphInfo(RecordType.MASS, XKCDColors.Pink, "#MechJeb_Flightrecord_checkbox9", "#MechJeb_Flightrecord_checkbox23"), // Mass
            new GraphInfo(RecordType.Q, XKCDColors.Cyan, "#MechJeb_Flightrecord_checkbox10", "#MechJeb_Flightrecord_checkbox24", true), // Q
            new GraphInfo(RecordType.AO_A, XKCDColors.Lavender, "#MechJeb_Flightrecord_checkbox11", "#MechJeb_Flightrecord_checkbox25"), // AoA
            new GraphInfo(RecordType.AO_S, XKCDColors.Lime, "#MechJeb_Flightrecord_checkbox12", "#MechJeb_Flightrecord_checkbox26"), // AoS
            new GraphInfo(RecordType.AO_D, XKCDColors.Orange, "#MechJeb_Flightrecord_checkbox13", "#MechJeb_Flightrecord_checkbox27"), // AoD
            new GraphInfo(RecordType.PITCH, XKCDColors.Mint, "#MechJeb_Flightrecord_checkbox14", "#MechJeb_Flightrecord_checkbox28"), // Pitch
            new GraphInfo(RecordType.DELTA_V_EXPENDED, XKCDColors.Beige, "ΔV", "ΔV"), // ΔV
            new GraphInfo(RecordType.GRAVITY_LOSSES, XKCDColors.Green, "#MechJeb_Flightrecord_checkbox15", "#MechJeb_Flightrecord_checkbox29"), // Gravity Loss
            new GraphInfo(RecordType.DRAG_LOSSES, XKCDColors.LightBrown, "#MechJeb_Flightrecord_checkbox16", "#MechJeb_Flightrecord_checkbox30"), // Drag Loss
            new GraphInfo(RecordType.STEERING_LOSSES, XKCDColors.Cerise, "#MechJeb_Flightrecord_checkbox17", "#MechJeb_Flightrecord_checkbox31") // Steering Loss
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

        internal static Texture2D backgroundTexture;
        private CelestialBody oldMainBody;
        internal static readonly int typeCount = Enum.GetValues(typeof(RecordType)).Length;

        internal readonly graphState[] graphStates;

        // Which graphs the user has ticked, indexed by (int)RecordType. Persisted by name; see OnSave/OnLoad.
        internal readonly bool[] displayed;

        internal double lastMaximumAltitude;
        private readonly double precision = 0.2;

        internal int width = 512;
        internal int height = 256;

        internal bool paused;



        internal MechJebModuleFlightRecorder recorder;

        public MechJebModuleFlightRecorderGraph(MechJebCore core)
            : base(core)
        {
            Priority = 2000;
            graphStates = new graphState[typeCount];
            displayed = new bool[typeCount];
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

        // ---- shared by the IMGUI window and the new UI ----

        // keeps the atmosphere background texture (and the scales it depends on) up to date
        internal void UpdateBackground()
        {
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
        }

        internal void RefreshAtmoTexture() =>
            MechJebModuleAscentClassicPathMenu.UpdateAtmoTexture(backgroundTexture, Vessel.mainBody, lastMaximumAltitude, realAtmo);

        internal void ClampSizes()
        {
            timeScale = Mathf.Clamp(timeScale, 0, 20);
            downrangeScale = Mathf.Clamp(downrangeScale, 0, 20);
            hSize = Mathf.Clamp(hSize, 1, 20);
            vSize = Mathf.Clamp(vSize, 1, 10);
        }

        internal void ApplySizes()
        {
            width = 128 * hSize;
            height = 128 * vSize;
        }

        // x units (m or s) per pixel, and the largest x recorded since the mark
        internal double CurrentScaleX(out float maxX)
        {
            maxX = (float)(downrange
                ? recorder.Maximums[(int)RecordType.DOWN_RANGE]
                : recorder.Maximums[(int)RecordType.TIME_SINCE_MARK]);

            double maxXScaled = (downrange ? maxX : maxX / precision) / width;
            double autoScaleX = Math.Max(Math.Ceiling(Math.Log(maxXScaled, 2)), 0);
            double manualScaleX = downrange ? downrangeScale : timeScale;
            double activeScaleX = autoScale ? autoScaleX : manualScaleX;

            return downrange ? Math.Pow(2, activeScaleX) : precision * Math.Pow(2, activeScaleX);
        }

        internal void UpdateScale()
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
                    // ScaleTicks is only a target: rounding the limits out to a whole step can give one more
                    graphStates[t].labels = new string[2 * ScaleTicks];
                    graphStates[t].labelsPos = new double[2 * ScaleTicks];
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
                    while (i < graphStates[t].labels.Length && currX <= maximum + 0.5 * step)
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

        internal void ResetScale()
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

        public override string GetName() => Localizer.Format("#MechJeb_Flightrecord_title"); //"Flight Recorder"

        public override string IconName() => "Flight Recorder";
    }
}
