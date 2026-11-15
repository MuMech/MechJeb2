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
    /// MechJebModuleFlightRecorderGraph ("Flight Recorder") in KodeKit. The plot is a KodeKit LineGraph:
    /// one decimated mesh per graph (rebuilt a few times a second, one per frame), a mesh for the stage markers, Y labels for the selected graph.
    /// The model work (scales, background texture, sizes) runs in BeforeSync through the module's shared methods.
    /// Differences: the Y labels are as many as the plot's height has room for, a grip in the window's corner resizes the plot, and the
    /// graph is clipped to the plot instead of skipping the points outside it. The plot fills the
    /// window's width, a drop-down above it picks the graph of the Y labels, not the column beside it, and another one can
    /// give a second graph Y labels on the plot's right, in its colour.
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
        double seenMarkUT;

        // a Mark restarts the history, whoever made it (this window, a custom window's MARK, a script): the scales and the stage
        // markers start again. The scales belong to the module, which outlives this view, so their mark is kept there.
        void CheckMark()
        {
            double markUT = Rec.MarkUT;
            if (!markUT.Equals(g.scaleMarkUT))
            {
                g.scaleMarkUT = markUT;
                g.ResetScale();
            }

            if (markUT.Equals(seenMarkUT)) return;
            seenMarkUT = markUT;
            stageMarkers.Indices.Clear();
            scannedStages = 0;
        }

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
                storageText = Localizer.Format("#MechJeb_Flightrecord_Label3", (100 * idx / (float)Math.Max(Rec.History.Length, Rec.MaxHistorySize)).ToString("F1")); //Storage: <<1>> %
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

        // the max samples field: the text as typed, until it is a size the recorder takes
        string historySizeText;

        string HistorySizeText() => historySizeText ?? (Rec == null ? "" : Number(Rec.HistorySize));

        bool ParseHistorySize(string s, out int size) =>
            int.TryParse(s, out size) && size >= MechJebModuleFlightRecorder.MIN_HISTORY_SIZE && size <= MechJebModuleFlightRecorder.MAX_HISTORY_SIZE;

        void SetHistorySize(string s)
        {
            if (Rec != null && ParseHistorySize(s, out int size))
            {
                Rec.HistorySize = size;
                historySizeText = null;
            }
            else historySizeText = s;
        }

        // "Max [3000]": how many samples the recorder keeps before it stops (144 bytes each). Short, so the
        // chips beside it keep to two lines; the field is as wide as 5 digits
        void HistorySizeField(KodeWindow w, UIObject parent)
        {
            Layout row = KodeWindow.Row(parent);
            row.FlexibleLayout(0f, 0f);
            KodeWindow.Label(row, KodeWindow.Loc("#MechJeb_Flightrecord_Label4")); //Max
            EditFields.Field(w, row, HistorySizeText, SetHistorySize, 50, () => historySizeText == null);
        }

        static string Number(int n) => InfoText.Int(n);

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
        int builtLabels, shownWidth = -1, shownHeight = -1;
        // the sizes the resize grip and the - and + buttons set
        LayoutElement plotSize, labelsSize, rightLabelsSize;
        ResizeGrip grip;
        float dragStartWidth, dragStartHeight;
        int dragStartPlotWidth = -1, dragStartPlotHeight;

        static int LabelsFor(int height) => Mathf.Clamp(height / 24 + 1, 3, 11);
        int lastRefreshFrame = int.MinValue / 2;
        float lastRefreshTime;
        double lastScaleX = double.NaN;
        bool lastDownrange;
        int lastScaleIdx = -1, lastMask = -1, lastRightIdx = -2;

        protected override void BeforeSync()
        {
            if (Rec == null || g.Vessel == null) return; // the recorder only exists in flight

            // a saved index can be out of range
            if (g.scaleIdx < 0 || g.scaleIdx >= MechJebModuleFlightRecorderGraph.typeCount)
                g.scaleIdx = 0;

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

            if (g.rightScaleIdx < -1 || g.rightScaleIdx >= MechJebModuleFlightRecorderGraph.typeCount)
                g.rightScaleIdx = -1;
            // the right labels show while their graph is displayed: the plot takes their room back otherwise
            int rightIdx = RightShown();
            if (graph.RightLabels != null && graph.RightLabels.gameObject.activeSelf != rightIdx >= 0)
                graph.RightLabels.gameObject.SetActive(rightIdx >= 0);

            CheckMark();
            // what IMGUI's Repaint pass did
            g.UpdateScale();
            g.ClampSizes();
            g.ApplySizes();
            ApplySize();
            // a new height needs another number of Y labels: rebuild once the drag is over
            if (!grip.Dragging && LabelsFor(g.height) != builtLabels)
            {
                RebuildRequested = true;
                return;
            }

            // the plot fills its row: the X scale and the size shown go by the width it has
            g.width = Math.Max(g.width, Mathf.RoundToInt(graph.Plot.rect.width));

            curScaleX = g.CurrentScaleX(out float maxX);
            if (float.IsNaN(maxX) || maxX < 0) maxX = 0;
            graph.VisibleWidth = g.width * curScaleX;
            graph.DataMaxX = maxX;
            ScanStages();

            int mask = DisplayedMask();
            if (g.stages) mask |= 1 << 30;
            if (g.sharedScales) mask |= 1 << 29;

            bool force = !curScaleX.Equals(lastScaleX) || g.downrange != lastDownrange || g.scaleIdx != lastScaleIdx || mask != lastMask ||
                rightIdx != lastRightIdx;
            // LineGraph rebuilds one mesh per frame after a Refresh: don't refresh again before it has gone through them all
            bool due = Time.frameCount - lastRefreshFrame >= graph.Series.Count + 2 && Time.unscaledTime - lastRefreshTime >= 0.2f;
            if (force || due)
            {
                lastScaleX = curScaleX;
                lastDownrange = g.downrange;
                lastScaleIdx = g.scaleIdx;
                lastRightIdx = rightIdx;
                lastMask = mask;
                lastRefreshFrame = Time.frameCount;
                lastRefreshTime = Time.unscaledTime;

                MechJebModuleFlightRecorderGraph.graphState selected = g.drawnStates[g.scaleIdx];
                graph.Refresh(selected.minimum, selected.maximum);
                // every graph has its own Y range, shared with the other graphs of its unit
                for (int i = 0; i < graph.Series.Count; i++)
                {
                    MechJebModuleFlightRecorderGraph.graphState s = g.drawnStates[(int)MechJebModuleFlightRecorderGraph.graphInfos[i].Type];
                    graph.Series[i].YMin = s.minimum;
                    graph.Series[i].YMax = s.maximum;
                }

                if (rightIdx >= 0)
                {
                    MechJebModuleFlightRecorderGraph.graphState right = g.drawnStates[rightIdx];
                    graph.RefreshRight(right.minimum, right.maximum, ColorOf(rightIdx));
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

        // the right labels' graph when they are shown, -1 otherwise
        int RightShown() => graph.RightLabels != null && g.rightScaleIdx >= 0 && g.displayed[g.rightScaleIdx] ? g.rightScaleIdx : -1;

        static Color ColorOf(int type)
        {
            foreach (MechJebModuleFlightRecorderGraph.GraphInfo info in MechJebModuleFlightRecorderGraph.graphInfos)
                if ((int)info.Type == type) return info.Color;
            return Color.white;
        }

        // one bit per entry of graphInfos
        int DisplayedMask()
        {
            int mask = 0;
            for (int i = 0; i < MechJebModuleFlightRecorderGraph.graphInfos.Length; i++)
                if (g.displayed[(int)MechJebModuleFlightRecorderGraph.graphInfos[i].Type]) mask |= 1 << i;
            return mask;
        }

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            g.ClampSizes();
            g.ApplySizes();
            shownWidth = shownHeight = -1;
            dragStartPlotWidth = -1;
            stageMarkers = new StageMarkers(this);
            scannedStages = 0;
            seenMarkUT = Rec.MarkUT;

            BuildControls(w, r);
            BuildPlot(w, r);
            AddGrip(w);
        }

        void AddGrip(KodeWindow w)
        {
            grip = ResizeGrip.Add(w, Resized, ResizeDone);
            ApplySize();
        }

        // the plot's size as the settings give it, set on the plot and the Y labels without a rebuild
        void ApplySize()
        {
            if (g.width != shownWidth)
            {
                shownWidth = g.width;
                plotSize.preferredWidth = g.width;
            }

            if (g.height != shownHeight)
            {
                shownHeight = g.height;
                plotSize.preferredHeight = labelsSize.preferredHeight = g.height;
                if (rightLabelsSize != null) rightLabelsSize.preferredHeight = g.height;
            }
        }

        // the grip moves the plot's right and bottom edges with the window's
        void Resized(float width, float height)
        {
            if (dragStartPlotWidth < 0)
            {
                dragStartWidth = width;
                dragStartHeight = height;
                // the width the plot has, which filling its row can have made larger than the setting's
                dragStartPlotWidth = Math.Max(g.plotWidth, Mathf.RoundToInt(graph.Plot.rect.width));
                dragStartPlotHeight = g.plotHeight;
            }

            g.plotWidth = Mathf.RoundToInt(dragStartPlotWidth + width - dragStartWidth);
            g.plotHeight = Mathf.RoundToInt(dragStartPlotHeight + height - dragStartHeight);
            g.ClampSizes();
            g.ApplySizes();
        }

        void ResizeDone()
        {
            dragStartPlotWidth = -1;
            g.Dirty = true;
        }

        // ---- the controls' actions ----

        void Mark() => Rec.Mark();

        void ScaleBy(int d)
        {
            if (g.downrange) g.downrangeScale += d;
            else g.timeScale += d;
            g.ClampSizes();
        }

        void Resize(int dh, int dv)
        {
            // step from the width the plot has, which filling its row can have made larger than the setting's
            if (dh != 0) g.plotWidth = Step(g.width, dh);
            if (dv != 0) g.plotHeight = Step(g.plotHeight, dv);
            g.ClampSizes();
            g.Dirty = true;
        }

        // the next multiple of SIZE_STEP up or down
        static int Step(int size, int d)
        {
            const int step = MechJebModuleFlightRecorderGraph.SIZE_STEP;
            return d > 0 ? (size / step + 1) * step : ((size + step - 1) / step - 1) * step;
        }

        void SetRealAtmo(bool v)
        {
            g.realAtmo = v;
            if (MechJebModuleFlightRecorderGraph.backgroundTexture != null && g.Vessel != null) g.RefreshAtmoTexture();
        }

        // Downrange / Time as one choice, switches, and the 16 series as a legend of chips in their colours
        void BuildControls(KodeWindow w, UIObject r)
        {
            string resume = KodeWindow.Loc("#MechJeb_Flightrecord_Button1_1"), pause = KodeWindow.Loc("#MechJeb_Flightrecord_Button1_2"); //"Resume""Pause"
            string[] axis = { KodeWindow.Loc("#MechJeb_Flightrecord_Button2_1"), KodeWindow.Loc("#MechJeb_Flightrecord_Button2_2") }; //"Downrange""Time"

            // the view and where the recording is on the left, the actions on the right
            Layout top = KodeWindow.Row(r);
            w.Segmented(top, axis, () => g.downrange ? 0 : 1, i => g.downrange = i == 0).FlexibleLayout(0f, 0f);
            w.BoundLabel(top, Localizer.Format("#MechJeb_Flightrecord_Label1", "999d 99h 99m 99s"), TimeText);
            w.BoundLabel(top, Localizer.Format("#MechJeb_Flightrecord_Label2", "-999.9M") + "m", RangeText);
            KodeWindow.FlexibleSpace(top);
            MiscViewKit.BoundButton(w, top, () => g.paused ? resume : pause, () => g.paused = !g.paused);
            KodeWindow.Button(top, "#MechJeb_Flightrecord_Button3", Mark); //"Mark"
            KodeWindow.Button(top, "#MechJeb_Flightrecord_Button5", () => Rec.DumpCsv()); //CSV

            // the scale on its own row: automatic or set, and back to the automatic one
            Layout scaleRow = KodeWindow.Row(r);
            w.SwitchRow(scaleRow, "#MechJeb_Flightrecord_checkbox1", () => g.autoScale, v => g.autoScale = v).transform.parent //Auto Scale
                .GetComponent<LayoutElement>().flexibleWidth = 0;
            Layout scale = Nudge(w, scaleRow, "999.99Mm/px", ScaleText, () => ScaleBy(-1), () => ScaleBy(1));
            // auto scale picks the scale itself: show it, hide its - and +
            w.Visible(scale.transform.GetChild(0).gameObject, () => !g.autoScale);
            w.Visible(scale.transform.GetChild(scale.transform.childCount - 1).gameObject, () => !g.autoScale);
            KodeWindow.Button(scaleRow, "#MechJeb_Flightrecord_Button4", g.ResetScale); //"Reset Scale"
            KodeWindow.FlexibleSpace(scaleRow);

            // the plot's size, the options, how full the recording is
            Layout options = KodeWindow.Row(r);
            KodeWindow.RowOf(w.SwitchRow(options, "#MechJeb_Flightrecord_checkbox2", () => g.realAtmo, SetRealAtmo)) //Real Atmo
                .GetComponent<LayoutElement>().flexibleWidth = 0;
            KodeWindow.FlexibleSpace(options);
            Nudge(w, options, "9999", () => Number(g.width), () => Resize(-1, 0), () => Resize(1, 0));
            KodeWindow.Label(options, "×");
            Nudge(w, options, "9999", () => Number(g.height), () => Resize(0, -1), () => Resize(0, 1));
            UIText storage = w.BoundLabel(options, Localizer.Format("#MechJeb_Flightrecord_Label3", "100.0"), StorageText);
            KodeWindow.ToneText(storage, KodeWindow.Tone.Dim);

            // the chips wrap; the max samples field sits at the right end of their last line
            Layout chipsRow = KodeWindow.Row(r);
            chipsRow.ChildAlignment(TextAnchor.LowerLeft);
            Layout chips = KodeWindow.FlowWrap(chipsRow, 4);
            w.Chip(chips, "#MechJeb_Flightrecord_checkbox3", () => g.stages, v => g.stages = v); //"Stages"
            foreach (MechJebModuleFlightRecorderGraph.GraphInfo info in MechJebModuleFlightRecorderGraph.graphInfos)
            {
                RecordType type = info.Type;
                w.Chip(chips, info.Label, () => g.displayed[(int)type], v => g.displayed[(int)type] = v, info.Color);
            }

            HistorySizeField(w, chipsRow);

            // the left Y labels' drop-down on the left, the right labels' one on the right, the Shared scales switch in the
            // middle: the two sides take equal shares of the row (no preferred width of their own), so the switch is centred
            Layout axisRow = KodeWindow.Row(r);
            axisRow.gameObject.name = "AxisRow";
            Layout leftSide = KodeWindow.Row(axisRow);
            leftSide.gameObject.name = "LeftAxis";
            leftSide.PreferredWidth(0);
            Layout axisPick = KodeWindow.Row(leftSide);
            axisPick.FlexibleLayout(0f, 0f);
            new ComboBox(w, axisPick, ScalePosition, i => g.scaleIdx = (int)scaleGraphs[i].Type, ScaleNames, false, i => scaleGraphs[i].Color);
            w.Visible(axisPick.gameObject, () => ScaleNames().Length > 0);

            KodeWindow.RowOf(w.SwitchRow(axisRow, "#MechJeb_Flightrecord_checkbox32", () => g.sharedScales, v => g.sharedScales = v)) //Shared scales
                .GetComponent<LayoutElement>().flexibleWidth = 0;

            // the right labels: none, or one of the displayed graphs
            Layout rightSide = KodeWindow.Row(axisRow);
            rightSide.gameObject.name = "RightAxis";
            rightSide.PreferredWidth(0);
            rightSide.ChildAlignment(TextAnchor.MiddleRight);
            Layout rightPick = KodeWindow.Row(rightSide);
            rightPick.FlexibleLayout(0f, 0f);
            new ComboBox(w, rightPick, RightPosition, i => g.rightScaleIdx = i == 0 ? -1 : (int)scaleGraphs[i - 1].Type, RightNames, false,
                i => i == 0 ? Color.clear : scaleGraphs[i - 1].Color);
            w.Visible(rightPick.gameObject, () => ScaleNames().Length > 0);
            w.Behaviour.AddRelocalize(() => scaleNamesMask = -1);
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

        // the plot, its Y labels and the right labels
        void BuildPlot(KodeWindow w, UIObject r)
        {
            Layout plotRow = KodeWindow.Row(r);
            plotRow.Padding(0, 0, 5, 5);
            Layout left = plotRow.Add<Layout>("Plot");
            left.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false);
            left.Finish();
            builtLabels = LabelsFor(g.height);
            // the scrollbar row spans the window, as IMGUI's HorizontalScrollbar does
            // Y labels at the precision of their step, as the PSG Solution window's
            graph = new LineGraph(w, left, g.width, g.height, builtLabels, r) { LabelFormat = LineGraph.StepLabel };
            plotSize = graph.Plot.GetComponent<LayoutElement>();
            labelsSize = graph.Plot.parent.Find("YLabels").GetComponent<LayoutElement>();

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

            plotSize.flexibleWidth = 1;
            RectTransform right = graph.AddRightLabels(builtLabels);
            rightLabelsSize = right.GetComponent<LayoutElement>();
            right.gameObject.SetActive(RightShown() >= 0);
        }

        // ---- the drop-down above the plot: the displayed graphs ----

        readonly List<MechJebModuleFlightRecorderGraph.GraphInfo> scaleGraphs = new List<MechJebModuleFlightRecorderGraph.GraphInfo>();
        string[] scaleNames, rightNames;
        int scaleNamesMask = -1;

        // the combo box reads this every frame: a new array only when the displayed graphs change
        string[] ScaleNames()
        {
            int mask = DisplayedMask();
            if (mask == scaleNamesMask) return scaleNames;
            scaleNamesMask = mask;

            scaleGraphs.Clear();
            foreach (MechJebModuleFlightRecorderGraph.GraphInfo info in MechJebModuleFlightRecorderGraph.graphInfos)
                if (g.displayed[(int)info.Type]) scaleGraphs.Add(info);

            scaleNames = new string[scaleGraphs.Count];
            rightNames = new string[scaleGraphs.Count + 1];
            rightNames[0] = KodeWindow.Loc("#MechJeb_Flightrecord_Label5"); //None
            for (int i = 0; i < scaleNames.Length; i++)
                rightNames[i + 1] = scaleNames[i] = KodeWindow.Loc(scaleGraphs[i].ScaleLabel);
            return scaleNames;
        }

        string[] RightNames()
        {
            ScaleNames();
            return rightNames;
        }

        // 0 is None
        int RightPosition()
        {
            for (int i = 0; i < scaleGraphs.Count; i++)
                if ((int)scaleGraphs[i].Type == g.rightScaleIdx) return i + 1;
            return 0;
        }

        int ScalePosition()
        {
            for (int i = 0; i < scaleGraphs.Count; i++)
                if ((int)scaleGraphs[i].Type == g.scaleIdx) return i;
            return 0;
        }
    }
}
