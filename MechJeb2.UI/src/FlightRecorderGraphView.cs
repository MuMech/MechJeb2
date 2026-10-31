using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using KSP.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static MechJebLib.Utils.Statics;
using RecordType = MuMech.MechJebModuleFlightRecorder.RecordType;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModuleFlightRecorderGraph ("Flight Recorder") in KodeKit, row for row with its WindowGUI. The plot is a KodeKit LineGraph:
    /// one decimated mesh per graph (rebuilt a few times a second, one per frame), a mesh for the stage markers, Y labels for the selected graph.
    /// The model work of WindowGUI (scales, background texture, sizes) runs in BeforeSync through the module's shared methods.
    /// Differences: the Y labels are evenly spaced (LineGraph's), the plot size changes by rebuilding the view, and the
    /// graph is clipped to the plot instead of skipping the points outside it.
    /// </summary>
    [KodeViewFor(typeof(MechJebModuleFlightRecorderGraph))]
    public class FlightRecorderGraphView : KodeView
    {
        readonly MechJebModuleFlightRecorderGraph g;

        public FlightRecorderGraphView(MechJebModuleFlightRecorderGraph g) : base(g) => this.g = g;

        protected override float MinWidth => 400;

        MechJebModuleFlightRecorder Rec => g.recorder;

        // ---- data sources over the recorder's history (no allocation) ----

        int PointCount
        {
            get
            {
                MechJebModuleFlightRecorder rec = Rec;
                if (rec == null || rec.HistoryIdx < 0) return 0;
                return Math.Min(rec.HistoryIdx + 1, rec.History.Length);
            }
        }

        double XOf(int i) => g.downrange ? Rec.History[i].DownRange : Rec.History[i].TimeSinceMark;

        sealed class Series : IGraphSource
        {
            readonly FlightRecorderGraphView v;
            readonly RecordType type;

            public Series(FlightRecorderGraphView v, RecordType type)
            {
                this.v = v;
                this.type = type;
            }

            public int Count => v.PointCount;
            public double X(int i) => v.XOf(i);
            public double Y(int i) => v.Rec.History[i][type];
        }

        // the history indices where the stage changed
        sealed class StageMarkers : IGraphSource
        {
            readonly FlightRecorderGraphView v;
            public readonly List<int> Indices = new List<int>();

            public StageMarkers(FlightRecorderGraphView v) => this.v = v;

            public int Count => Indices.Count;
            public double X(int i) => v.XOf(Indices[i]);
            public double Y(int i) => 0;
        }

        StageMarkers stageMarkers;
        int scannedStages, lastStage;

        void ScanStages()
        {
            int n = PointCount;
            if (n < scannedStages)
            {
                // a new mark restarted the history
                stageMarkers.Indices.Clear();
                scannedStages = 0;
            }

            if (scannedStages == 0 && n > 0)
            {
                lastStage = Rec.History[0].CurrentStage;
                scannedStages = 1;
            }

            for (; scannedStages < n; scannedStages++)
            {
                int stage = Rec.History[scannedStages].CurrentStage;
                if (stage == lastStage) continue;
                lastStage = stage;
                stageMarkers.Indices.Add(scannedStages);
            }
        }

        // ---- cached label texts: the getters run every frame ----

        long shownSeconds = long.MinValue;
        string timeText;

        string TimeText()
        {
            if (Rec == null) return timeText ?? "";
            long s = (long)Rec.TimeSinceMark;
            if (timeText == null || s != shownSeconds)
            {
                shownSeconds = s;
                timeText = Localizer.Format("#MechJeb_Flightrecord_Label1", GuiUtils.TimeToDHMS(Rec.TimeSinceMark)); //Time <<1>>
            }

            return timeText;
        }

        double shownRange = double.NaN;
        string rangeText;

        string RangeText()
        {
            if (Rec == null || Rec.HistoryIdx < 0 || Rec.HistoryIdx >= Rec.History.Length) return rangeText ?? "";
            double d = Rec.History[Rec.HistoryIdx].DownRange;
            if (rangeText == null || !d.Equals(shownRange))
            {
                shownRange = d;
                rangeText = Localizer.Format("#MechJeb_Flightrecord_Label2", d.ToSI()) + "m"; //Downrange <<1>>
            }

            return rangeText;
        }

        int shownIdx = -2;
        string storageText;

        string StorageText()
        {
            if (Rec == null) return storageText ?? "";
            int idx = Rec.HistoryIdx;
            if (storageText == null || idx != shownIdx)
            {
                shownIdx = idx;
                storageText = Localizer.Format("#MechJeb_Flightrecord_Label3", (100 * idx / (float)Rec.History.Length).ToString("F1")); //Storage: <<1>> %
            }

            return storageText;
        }

        double curScaleX = 1;
        double shownScale = double.NaN;
        bool shownScaleDownrange;
        string scaleText;

        string ScaleText()
        {
            if (scaleText == null || !curScaleX.Equals(shownScale) || g.downrange != shownScaleDownrange)
            {
                shownScale = curScaleX;
                shownScaleDownrange = g.downrange;
                scaleText = g.downrange ? curScaleX.ToSI(2) + "m/px" : GuiUtils.TimeToDHMS(curScaleX, 1) + "/px";
            }

            return scaleText;
        }

        static readonly Dictionary<int, string> numbers = new Dictionary<int, string>();

        static string Number(int n)
        {
            if (!numbers.TryGetValue(n, out string s)) numbers[n] = s = n.ToString();
            return s;
        }

        // ---- the plot ----

        LineGraph graph;

        static Texture2D plotBackdrop;

        static Texture2D PlotBackdrop
        {
            get
            {
                if (plotBackdrop != null) return plotBackdrop;
                plotBackdrop = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                plotBackdrop.SetPixel(0, 0, new Color(0.07f, 0.08f, 0.09f, 1f));
                plotBackdrop.Apply();
                return plotBackdrop;
            }
        }
        RawImage background;
        CanvasGroup yLabels;
        int builtWidth, builtHeight;
        int lastRefreshFrame = int.MinValue / 2;
        float lastRefreshTime;
        double lastScaleX = double.NaN;
        bool lastDownrange;
        int lastScaleIdx = -1, lastMask = -1;

        protected override void BeforeSync()
        {
            if (Rec == null || g.Vessel == null) return; // the recorder only exists in flight

            g.UpdateBackground();

            // the graph used for the scale labels must be a displayed one
            if (!g.displayed[g.scaleIdx])
            {
                int newIdx = 0;
                while (newIdx < MechJebModuleFlightRecorderGraph.typeCount && !g.displayed[newIdx])
                    newIdx++;

                if (newIdx == MechJebModuleFlightRecorderGraph.typeCount)
                    newIdx = 0;
                g.scaleIdx = newIdx;
            }

            // what IMGUI's Repaint pass did
            g.UpdateScale();
            g.ApplySizes();
            if (g.width != builtWidth || g.height != builtHeight)
            {
                RebuildRequested = true;
                return;
            }

            curScaleX = g.CurrentScaleX(out float maxX);
            if (float.IsNaN(maxX) || maxX < 0) maxX = 0;
            graph.VisibleWidth = g.width * curScaleX;
            graph.DataMaxX = maxX;
            ScanStages();

            int mask = 0;
            for (int i = 0; i < MechJebModuleFlightRecorderGraph.graphInfos.Length; i++)
                if (g.displayed[(int)MechJebModuleFlightRecorderGraph.graphInfos[i].Type]) mask |= 1 << i;
            if (g.stages) mask |= 1 << 30;

            bool force = !curScaleX.Equals(lastScaleX) || g.downrange != lastDownrange || g.scaleIdx != lastScaleIdx || mask != lastMask;
            // LineGraph rebuilds one mesh per frame after a Refresh: don't refresh again before it has gone through them all
            bool due = Time.frameCount - lastRefreshFrame >= graph.Series.Count + 2 && Time.unscaledTime - lastRefreshTime >= 0.2f;
            if (force || due)
            {
                lastScaleX = curScaleX;
                lastDownrange = g.downrange;
                lastScaleIdx = g.scaleIdx;
                lastMask = mask;
                lastRefreshFrame = Time.frameCount;
                lastRefreshTime = Time.unscaledTime;

                MechJebModuleFlightRecorderGraph.graphState selected = g.graphStates[g.scaleIdx];
                graph.Refresh(selected.minimum, selected.maximum);
                // every graph has its own Y range
                for (int i = 0; i < graph.Series.Count; i++)
                {
                    MechJebModuleFlightRecorderGraph.graphState s = g.graphStates[(int)MechJebModuleFlightRecorderGraph.graphInfos[i].Type];
                    graph.Series[i].YMin = s.minimum;
                    graph.Series[i].YMax = s.maximum;
                }
            }

            // the atmosphere behind the altitude graphs, an opaque dark backdrop otherwise
            bool atmosphere = g.displayed[(int)RecordType.ALTITUDE_ASL] || g.displayed[(int)RecordType.ALTITUDE_TRUE];
            Texture texture = atmosphere && MechJebModuleFlightRecorderGraph.backgroundTexture != null
                ? MechJebModuleFlightRecorderGraph.backgroundTexture
                : PlotBackdrop; // not Texture2D.blackTexture: that one is transparent
            if (background.texture != texture) background.texture = texture;

            float alpha = g.scaleIdx != 0 ? 1 : 0;
            if (yLabels.alpha != alpha) yLabels.alpha = alpha;
        }

        // GUI.color = the graph's colour around IMGUI's toggle tints the box and the text
        static void Colorize(KodeWindow w, Toggle toggle, Color color)
        {
            TMP_Text label = KodeWindow.LabelOf(toggle);
            Graphic box = KodeWindow.ModernLayout ? null : toggle.targetGraphic;
            void Paint()
            {
                label.color = color;
                if (box != null) box.color = color;
            }
            Paint();
            w.Behaviour.AddRestyle(Paint);
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            g.ApplySizes();
            builtWidth = g.width;
            builtHeight = g.height;
            stageMarkers = new StageMarkers(this);

            if (KodeWindow.ModernLayout)
            {
                BuildModernControls(w, r);
                BuildPlot(w, r);
                return;
            }

            string resume = KodeWindow.Loc("#MechJeb_Flightrecord_Button1_1"), pause = KodeWindow.Loc("#MechJeb_Flightrecord_Button1_2"); //"Resume""Pause"
            string downrange = KodeWindow.Loc("#MechJeb_Flightrecord_Button2_1"), time = KodeWindow.Loc("#MechJeb_Flightrecord_Button2_2"); //"Downrange""Time"

            // row 1: pause, downrange/time, time and range readouts, mark, reset scale
            Layout top = KodeWindow.Row(r);
            MiscViewKit.BoundButton(w, top, () => g.paused ? resume : pause, () => g.paused = !g.paused);
            MiscViewKit.BoundButton(w, top, () => g.downrange ? downrange : time, () => g.downrange = !g.downrange);
            w.BoundLabel(top, Localizer.Format("#MechJeb_Flightrecord_Label1", "999d 99h 99m 99s"), TimeText);
            w.BoundLabel(top, Localizer.Format("#MechJeb_Flightrecord_Label2", "-999.9M") + "m", RangeText);
            KodeWindow.FlexibleSpace(top);
            KodeWindow.Button(top, "#MechJeb_Flightrecord_Button3", Mark); //"Mark"
            KodeWindow.Button(top, "#MechJeb_Flightrecord_Button4", g.ResetScale); //"Reset Scale"

            // row 2: scale, plot size, real atmo, csv, storage
            Layout options = KodeWindow.Row(r);
            w.Toggle(options, "#MechJeb_Flightrecord_checkbox1", () => g.autoScale, v => g.autoScale = v); //Auto Scale
            UIButton scaleDown = KodeWindow.Button(options, "-", () => ScaleBy(-1));
            w.Visible(scaleDown.gameObject, () => !g.autoScale);
            w.BoundLabel(options, "999.99Mm/px", ScaleText);
            UIButton scaleUp = KodeWindow.Button(options, "+", () => ScaleBy(1));
            w.Visible(scaleUp.gameObject, () => !g.autoScale);

            KodeWindow.Button(options, "-", () => Resize(-1, 0));
            w.BoundLabel(options, "9999", () => Number(g.width));
            KodeWindow.Button(options, "+", () => Resize(1, 0));
            KodeWindow.Label(options, "x");
            KodeWindow.Button(options, "-", () => Resize(0, -1));
            w.BoundLabel(options, "9999", () => Number(g.height));
            KodeWindow.Button(options, "+", () => Resize(0, 1));

            w.Toggle(options, "#MechJeb_Flightrecord_checkbox2", () => g.realAtmo, SetRealAtmo); //Real Atmo
            KodeWindow.FlexibleSpace(options);
            KodeWindow.Button(options, "#MechJeb_Flightrecord_Button5", () => Rec.DumpCsv()); //CSV
            w.BoundLabel(options, Localizer.Format("#MechJeb_Flightrecord_Label3", "100.0"), StorageText);

            // row 3: which graphs to show, in their colours
            Layout toggles = KodeWindow.Row(r);
            w.Toggle(toggles, "#MechJeb_Flightrecord_checkbox3", () => g.stages, v => g.stages = v); //"Stages"
            foreach (MechJebModuleFlightRecorderGraph.GraphInfo info in MechJebModuleFlightRecorderGraph.graphInfos)
            {
                if (info.NewRow) toggles = KodeWindow.Row(r);
                RecordType type = info.Type;
                Colorize(w, w.Toggle(toggles, info.Label, () => g.displayed[(int)type], v => g.displayed[(int)type] = v), info.Color);
            }

            BuildPlot(w, r);
        }

        // ---- the controls' actions, in both layouts ----

        void Mark()
        {
            g.ResetScale(); // TODO : should check something else to catch Mark calls from other code
            Rec.Mark();
        }

        void ScaleBy(int d)
        {
            if (g.downrange) g.downrangeScale += d;
            else g.timeScale += d;
            g.ClampSizes();
        }

        void Resize(int dh, int dv)
        {
            g.hSize += dh;
            g.vSize += dv;
            g.ClampSizes();
        }

        void SetRealAtmo(bool v)
        {
            g.realAtmo = v;
            if (MechJebModuleFlightRecorderGraph.backgroundTexture != null && g.Vessel != null) g.RefreshAtmoTexture();
        }

        // modern layout: Downrange / Time as one choice, switches, and the 16 series as a legend of chips in their colours
        void BuildModernControls(KodeWindow w, UIObject r)
        {
            string resume = KodeWindow.Loc("#MechJeb_Flightrecord_Button1_1"), pause = KodeWindow.Loc("#MechJeb_Flightrecord_Button1_2"); //"Resume""Pause"
            string[] axis = { KodeWindow.Loc("#MechJeb_Flightrecord_Button2_1"), KodeWindow.Loc("#MechJeb_Flightrecord_Button2_2") }; //"Downrange""Time"

            Layout top = KodeWindow.Row(r);
            MiscViewKit.BoundButton(w, top, () => g.paused ? resume : pause, () => g.paused = !g.paused);
            w.Segmented(top, axis, () => g.downrange ? 0 : 1, i => g.downrange = i == 0).FlexibleLayout(0f, 0f);
            w.BoundLabel(top, Localizer.Format("#MechJeb_Flightrecord_Label1", "999d 99h 99m 99s"), TimeText);
            w.BoundLabel(top, Localizer.Format("#MechJeb_Flightrecord_Label2", "-999.9M") + "m", RangeText);
            KodeWindow.FlexibleSpace(top);
            KodeWindow.Button(top, "#MechJeb_Flightrecord_Button3", Mark); //"Mark"
            KodeWindow.Button(top, "#MechJeb_Flightrecord_Button4", g.ResetScale); //"Reset Scale"
            KodeWindow.Button(top, "#MechJeb_Flightrecord_Button5", () => Rec.DumpCsv()); //CSV

            Layout options = KodeWindow.Row(r);
            w.SwitchRow(options, "#MechJeb_Flightrecord_checkbox1", () => g.autoScale, v => g.autoScale = v).transform.parent //Auto Scale
                .GetComponent<LayoutElement>().flexibleWidth = 0;
            Layout scale = Nudge(w, options, "999.99Mm/px", ScaleText, () => ScaleBy(-1), () => ScaleBy(1));
            // auto scale picks the scale itself: show it, hide its - and +
            w.Visible(scale.transform.GetChild(0).gameObject, () => !g.autoScale);
            w.Visible(scale.transform.GetChild(scale.transform.childCount - 1).gameObject, () => !g.autoScale);
            KodeWindow.FlexibleSpace(options);
            Nudge(w, options, "9999", () => Number(g.width), () => Resize(-1, 0), () => Resize(1, 0));
            KodeWindow.Label(options, "×");
            Nudge(w, options, "9999", () => Number(g.height), () => Resize(0, -1), () => Resize(0, 1));
            KodeWindow.RowOf(w.SwitchRow(options, "#MechJeb_Flightrecord_checkbox2", () => g.realAtmo, SetRealAtmo)) //Real Atmo
                .GetComponent<LayoutElement>().flexibleWidth = 0;
            UIText storage = w.BoundLabel(options, Localizer.Format("#MechJeb_Flightrecord_Label3", "100.0"), StorageText);
            KodeWindow.ToneText(storage, KodeWindow.Tone.Dim);

            Layout chips = KodeWindow.FlowWrap(r, 4);
            w.Chip(chips, "#MechJeb_Flightrecord_checkbox3", () => g.stages, v => g.stages = v); //"Stages"
            foreach (MechJebModuleFlightRecorderGraph.GraphInfo info in MechJebModuleFlightRecorderGraph.graphInfos)
            {
                RecordType type = info.Type;
                w.Chip(chips, info.Label, () => g.displayed[(int)type], v => g.displayed[(int)type] = v, info.Color);
            }
        }

        /// <summary>[−] value [+] on a value the window computes (a scale, a plot size): a stepper without a field.</summary>
        static Layout Nudge(KodeWindow w, UIObject parent, string template, Func<string> value, Action minus, Action plus)
        {
            Layout row = KodeWindow.Row(parent);
            row.FlexibleLayout(0f, 0f);
            row.Spacing(1);
            KodeWindow.Button(row, "-", minus);
            UIText t = w.BoundLabel(row, template, value);
            t.tmpText.alignment = TextAlignmentOptions.Center;
            KodeWindow.Button(row, "+", plus);
            return row;
        }

        // the plot, and next to it which graph the Y labels belong to (both layouts)
        void BuildPlot(KodeWindow w, UIObject r)
        {
            // the plot, and next to it which graph the Y labels belong to
            Layout plotRow = KodeWindow.Row(r);
            Layout left = plotRow.Add<Layout>("Plot");
            left.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false);
            left.Finish();
            int labels = Mathf.Clamp(g.height / 24 + 1, 3, 11);
            // the scrollbar row spans the window, as IMGUI's HorizontalScrollbar does
            graph = new LineGraph(w, left, g.width, g.height, labels, r);

            background = graph.Plot.Find("Background").GetComponent<RawImage>();
            yLabels = graph.Plot.parent.Find("YLabels").gameObject.AddComponent<CanvasGroup>();
            yLabels.interactable = yLabels.blocksRaycasts = false;
            w.Visible(graph.Stages.gameObject, () => g.stages);
            graph.Stages.Source = stageMarkers;

            foreach (MechJebModuleFlightRecorderGraph.GraphInfo info in MechJebModuleFlightRecorderGraph.graphInfos)
            {
                RecordType type = info.Type;
                LineSeriesGraphic series = graph.AddSeries(new Series(this, type), info.Color);
                w.Visible(series.gameObject, () => g.displayed[(int)type]);
            }

            Layout scales = KodeWindow.Panel(plotRow);
            foreach (MechJebModuleFlightRecorderGraph.GraphInfo info in MechJebModuleFlightRecorderGraph.graphInfos)
            {
                RecordType type = info.Type;
                Toggle pick = w.Toggle(scales, info.ScaleLabel, () => g.scaleIdx == (int)type, v =>
                {
                    if (v) g.scaleIdx = (int)type;
                });
                Colorize(w, pick, info.Color);
                w.Visible(pick, () => g.displayed[(int)type]);
            }
        }
    }
}
