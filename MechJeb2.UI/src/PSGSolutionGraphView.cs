using System;
using System.Collections.Generic;
using KodeKit;
using KodeUI;
using MechJebLib.PSG;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Column = MechJebLib.PSG.SolutionTrace.Column;
using FlownColumn = MuMech.PSGFlownTrack.Column;
using SeriesInfo = MuMech.MechJebModulePSGSolutionGraph.SeriesInfo;

namespace MuMech.UI
{
    /// <summary>
    /// MechJebModulePSGSolutionGraph ("PSG Solution"): PSG's Solutions against MET, on a KodeKit LineGraph. Each quantity has its
    /// own Y range, and the drop-down in the plot's corner picks the one the Y labels show. Three lines per quantity, which the
    /// legend under the plot draws: the Solution guidance flies now (bright), the one the vessel lifted off with (washed out,
    /// hidden while it is the current one) and what the vessel flew (dark). Vertical marks: the current Solution's phase
    /// boundaries, its interpolant nodes' edges, its collocation points (where the optimizer enforces the Qα and Q limits),
    /// and the vessel's MET. Zoom narrows the visible span, which the scrollbar moves; the corner grip resizes the window
    /// and the plot.
    /// </summary>
    [KodeViewFor(typeof(MechJebModulePSGSolutionGraph))]
    public class PSGSolutionGraphView : KodeView
    {
        readonly MechJebModulePSGSolutionGraph g;

        public PSGSolutionGraphView(MechJebModulePSGSolutionGraph g) : base(g) => this.g = g;

        protected override float MinWidth => 460;

        static SeriesInfo[] Infos => MechJebModulePSGSolutionGraph.Series;

        SolutionTrace Plan => g.Plan;

        SolutionTrace Pad => g.PadPlan;

        PSGFlownTrack Flown => g.Flown;

        // ---- how each line and mark looks, shared by the plot and its legend ----

        const float PlanThickness = 2, PadThickness = 1.5f, FlownThickness = 1.5f;

        // opaque shades of the series' colour, since overlapping translucent quads bead a thin line
        static Color PadShade(Color c) => Color.Lerp(c, new Color(0.55f, 0.55f, 0.6f), 0.6f);
        static Color FlownShade(Color c) => Color.Lerp(c, Color.black, 0.45f);

        static readonly Color PhaseColor = new Color(0.8f, 0.8f, 0.8f, 0.85f);
        static readonly Color NodeColor = new Color(0.5f, 0.5f, 0.55f, 0.5f);
        static readonly Color KnotColor = new Color(0.95f, 0.85f, 0.3f, 0.6f);
        static readonly Color NowColor = new Color(1, 1, 1, 0.9f);

        // ---- data sources (no allocation) ----

        sealed class TraceSeries : IGraphSource
        {
            readonly Func<SolutionTrace> trace;
            readonly Column column;
            readonly double factor;

            public TraceSeries(Func<SolutionTrace> trace, SeriesInfo info)
            {
                this.trace = trace;
                column = info.Column;
                factor = info.Factor;
            }

            public int Count => trace().Count;
            public double X(int i) => trace()[Column.MET, i];
            public double Y(int i) => trace()[column, i] * factor;
        }

        sealed class FlownSeries : IGraphSource
        {
            readonly PSGSolutionGraphView v;
            readonly FlownColumn column;
            readonly double factor;

            public FlownSeries(PSGSolutionGraphView v, FlownColumn column, double factor)
            {
                this.v = v;
                this.column = column;
                this.factor = factor;
            }

            public int Count => v.Flown.Count;
            public double X(int i) => v.Flown[FlownColumn.MET, i];
            public double Y(int i) => v.Flown[column, i] * factor;
        }

        sealed class Markers : IGraphSource
        {
            readonly List<double> times;

            public Markers(List<double> times) => this.times = times;

            public int Count => times.Count;
            public double X(int i) => times[i];
            public double Y(int i) => 0;
        }

        sealed class NowMarker : IGraphSource
        {
            public double Met = double.NaN;

            public int Count => double.IsNaN(Met) ? 0 : 1;
            public double X(int i) => Met;
            public double Y(int i) => 0;
        }

        // ---- the plot ----

        LineGraph graph;
        readonly LineSeriesGraphic[] planLines = new LineSeriesGraphic[Infos.Length];
        readonly LineSeriesGraphic[] padLines = new LineSeriesGraphic[Infos.Length];
        readonly LineSeriesGraphic[] flownLines = new LineSeriesGraphic[Infos.Length];
        LineSeriesGraphic nodeLines, knotLines, nowLine;
        LineSeriesGraphic[] extraMarkers;
        readonly NowMarker now = new NowMarker();
        readonly double[] yMin = new double[Infos.Length], yMax = new double[Infos.Length];

        // the sizes the resize grip sets
        LayoutElement rootSize, plotSize, labelsSize;
        ResizeGrip grip;
        int builtLabels, shownWidth = -1, shownPlotHeight = -1;
        float dragStartHeight;
        int dragStartPlotHeight;

        int lastRefreshFrame = int.MinValue / 2;
        float lastRefreshTime;
        int lastMask = -1, lastZoom = -1, lastScaleIdx = -1, lastSampled = -1, lastPadSampled = -1, lastFlownCount = -1;
        bool lastFlown, lastNodes, lastKnots;
        double span = 1;

        static int LabelsFor(int height) => Mathf.Clamp(height / 24 + 1, 3, 11);

        protected override void BeforeSync()
        {
            g.ClampSettings();
            ApplySize();
            // a new height needs another number of Y labels: rebuild once the drag is over
            if (!grip.Dragging && LabelsFor(g.PlotHeight) != builtLabels)
            {
                RebuildRequested = true;
                return;
            }

            bool planChanged = g.Refresh() || g.Sampled != lastSampled || g.PadSampled != lastPadSampled;
            if (planChanged) Summarize();

            now.Met = g.Vessel != null ? g.VesselState.MET : double.NaN;

            int mask = g.DisplayedMask;
            bool force = planChanged || mask != lastMask || g.Zoom != lastZoom || g.ScaleIdx != lastScaleIdx || g.ShowFlown != lastFlown ||
                         g.ShowNodes != lastNodes || g.ShowKnots != lastKnots;
            // LineGraph rebuilds one mesh per frame after a Refresh: don't refresh again before it has gone through them all
            bool due = Time.frameCount - lastRefreshFrame >= graph.Series.Count + 2 && Time.unscaledTime - lastRefreshTime >= 0.5f;
            if (!force && !due) return;

            bool rangesStale = planChanged || Flown.Count != lastFlownCount || g.ShowFlown != lastFlown;
            lastSampled = g.Sampled;
            lastPadSampled = g.PadSampled;
            lastMask = mask;
            lastScaleIdx = g.ScaleIdx;
            lastFlown = g.ShowFlown;
            lastNodes = g.ShowNodes;
            lastKnots = g.ShowKnots;
            lastFlownCount = Flown.Count;
            lastRefreshFrame = Time.frameCount;
            lastRefreshTime = Time.unscaledTime;

            if (rangesStale) ComputeRanges();

            span = Math.Max(Math.Max(Math.Max(Plan.Tf, Pad.Tf), Flown.LastMET), 1);
            double visible = span / (1 << g.Zoom);
            if (g.Zoom != lastZoom)
            {
                // zoom about the vessel's MET when the data reaches it, about the middle of the view otherwise
                double middle = now.Met > 0 && now.Met < span ? now.Met : graph.ViewMin + graph.VisibleWidth / 2;
                graph.ViewMin = middle - visible / 2;
                lastZoom = g.Zoom;
            }

            graph.DataMaxX = span;
            graph.VisibleWidth = visible;
            graph.ViewMin = Math.Max(0, Math.Min(graph.ViewMin, span - visible));

            graph.Refresh(yMin[g.ScaleIdx], yMax[g.ScaleIdx]);
            for (int i = 0; i < Infos.Length; i++)
            {
                planLines[i].YMin = padLines[i].YMin = flownLines[i].YMin = yMin[i];
                planLines[i].YMax = padLines[i].YMax = flownLines[i].YMax = yMax[i];
            }

            // the extra markers aren't LineGraph's: give them its view and redraw them now
            foreach (LineSeriesGraphic m in extraMarkers)
            {
                m.XMin = graph.ViewMin;
                m.XMax = graph.ViewMin + graph.VisibleWidth;
                m.YMin = 0;
                m.YMax = 1;
                m.SetVerticesDirty();
            }
        }

        // the window's width and the plot's height, when the grip or a saved setting changed them
        void ApplySize()
        {
            int width = Math.Max(g.Width, Mathf.CeilToInt(MinWidth));
            if (width != shownWidth)
            {
                shownWidth = width;
                rootSize.minWidth = rootSize.preferredWidth = width;
            }

            if (g.PlotHeight != shownPlotHeight)
            {
                shownPlotHeight = g.PlotHeight;
                plotSize.preferredHeight = labelsSize.preferredHeight = g.PlotHeight;
            }
        }

        void Resized(float width, float height)
        {
            if (dragStartPlotHeight < 0)
            {
                dragStartHeight = height;
                dragStartPlotHeight = g.PlotHeight;
            }

            g.Width = Mathf.RoundToInt(width);
            g.PlotHeight = Mathf.RoundToInt(dragStartPlotHeight + height - dragStartHeight);
            g.ClampSettings();
        }

        void ResizeDone()
        {
            dragStartPlotHeight = -1;
            g.Dirty = true;
        }

        SolutionTrace[] traces;

        // each series' Y range: its data's (current, pad and, when shown, flown), widened to its fixed range
        void ComputeRanges()
        {
            for (int i = 0; i < Infos.Length; i++)
            {
                SeriesInfo info = Infos[i];
                double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
                foreach (SolutionTrace trace in traces)
                {
                    if (trace.Count == 0) continue;
                    (double tMin, double tMax) = trace.Range(info.Column);
                    lo = Math.Min(lo, tMin * info.Factor);
                    hi = Math.Max(hi, tMax * info.Factor);
                }

                if (g.ShowFlown && info.Flown.HasValue)
                {
                    FlownColumn c = info.Flown.Value;
                    for (int k = 0; k < Flown.Count; k++)
                    {
                        double y = Flown[c, k] * info.Factor;
                        if (double.IsNaN(y) || double.IsInfinity(y)) continue;
                        if (y < lo) lo = y;
                        if (y > hi) hi = y;
                    }
                }

                if (lo > hi) lo = hi = 0;
                double pad = Math.Max(hi - lo, Math.Abs(hi) * 1e-3) * 0.05;
                lo = double.IsNaN(info.Min) ? lo - pad : Math.Min(info.Min, lo);
                hi = double.IsNaN(info.Max) ? hi + pad : Math.Max(info.Max, hi);
                if (!(hi > lo)) hi = lo + 1;
                yMin[i] = lo;
                yMax[i] = hi;
            }
        }

        // ---- texts, computed when what they show changes ----

        string statusText = "", summaryText = "", viewText = "";
        double shownViewMin = double.NaN, shownViewWidth = double.NaN;
        bool shownCurrent, shownHold;
        int shownSampled = -1;

        string StatusText()
        {
            bool current = g.PlanIsCurrent;
            if (g.Sampled == shownSampled && current == shownCurrent && g.Hold == shownHold) return statusText;
            shownSampled = g.Sampled;
            shownCurrent = current;
            shownHold = g.Hold;

            if (Plan.Count == 0)
                statusText = "No PSG solution yet: engage the PSG ascent";
            else
                statusText = $"Solution {g.Sampled}: MET {Plan.T0:F0} to {Plan.Tf:F0} s, {Plan.PhaseBoundaries.Count - 1} phases" +
                             (current ? "" : g.Hold ? " (held, guidance has a newer one)" : " (guidance has none now)");
            return statusText;
        }

        string ViewText()
        {
            if (graph == null) return viewText;
            if (graph.ViewMin.Equals(shownViewMin) && graph.VisibleWidth.Equals(shownViewWidth)) return viewText;
            shownViewMin = graph.ViewMin;
            shownViewWidth = graph.VisibleWidth;
            viewText = $"MET {graph.ViewMin:F0} to {graph.ViewMin + graph.VisibleWidth:F0} s";
            return viewText;
        }

        // the peaks of the current and pad Solutions: what the limits and the steering reach
        void Summarize()
        {
            var lines = new List<string>();
            if (Plan.Count > 0) lines.Add("Current: " + Peaks(Plan));
            if (Pad.Count > 0 && !g.PlanIsPad) lines.Add("Pad: " + Peaks(Pad));
            summaryText = string.Join("\n", lines);
        }

        static string Peaks(SolutionTrace t)
        {
            if (!t.Aero.Atmosphere) return "no atmosphere in the plan";
            int qa = ArgMax(t, Column.Q_ALPHA_FRACTION), q = ArgMax(t, Column.Q_FRACTION), aoa = ArgMax(t, Column.AOA);
            var parts = new List<string>();
            if (t.Aero.HasQAlphaLimit)
                parts.Add($"max Qα {t[Column.Q_ALPHA_FRACTION, qa] * 100:F1} % of limit at MET {t[Column.MET, qa]:F0} s");
            if (t.Aero.HasQLimit)
                parts.Add($"max Q {t[Column.Q_FRACTION, q] * 100:F1} % at MET {t[Column.MET, q]:F0} s");
            parts.Add($"max AoA {t[Column.AOA, aoa]:F1}° at MET {t[Column.MET, aoa]:F0} s");
            return string.Join(", ", parts);
        }

        static int ArgMax(SolutionTrace t, Column c)
        {
            int best = 0;
            for (int i = 1; i < t.Count; i++)
                if (t[c, i] > t[c, best])
                    best = i;
            return best;
        }

        // ---- building ----

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            g.ClampSettings();
            lastSampled = lastPadSampled = lastMask = lastZoom = lastScaleIdx = lastFlownCount = -1;
            shownWidth = shownPlotHeight = -1;
            dragStartPlotHeight = -1;
            traces = new[] { Plan, Pad };

            Layout top = KodeWindow.Row(r);
            w.BoundLabel(top, "Solution 999: MET 9999 to 9999 s, 99 phases (held, guidance has a newer one)", StatusText, true);
            w.Toggle(top, "Hold", () => g.Hold, v => g.Hold = v);
            KodeWindow.Button(top, "CSV", () => g.ExportCsv());

            Layout options = KodeWindow.Row(r);
            KodeWindow.Label(options, "Zoom");
            Nudge(w, options, "×256", () => "×" + (1 << g.Zoom), () => g.Zoom--, () => g.Zoom++);
            w.BoundLabel(options, "MET 9999 to 9999 s", ViewText);
            KodeWindow.FlexibleSpace(options);

            Layout chips = KodeWindow.FlowWrap(KodeWindow.Row(r), 4);
            for (int i = 0; i < Infos.Length; i++)
            {
                int k = i;
                w.Chip(chips, Infos[i].Label, () => g.Displayed(k), v => g.SetDisplayed(k, v), Infos[i].Color);
            }

            BuildPlot(w, r);
            BuildLegend(w, r);

            UIText summary = w.Wrap(w.BoundLabel(r, () => summaryText, true));
            KodeWindow.ToneText(summary, KodeWindow.Tone.Dim);

            rootSize = w.Root.GetComponent<LayoutElement>() ?? w.Root.gameObject.AddComponent<LayoutElement>();
            grip = ResizeGrip.Add(w, Resized, ResizeDone);
            ApplySize();
        }

        void BuildPlot(KodeWindow w, UIObject r)
        {
            int height = g.PlotHeight;
            builtLabels = LabelsFor(height);
            Layout plotRow = KodeWindow.Row(r);
            plotRow.Padding(0, 0, 5, 5);
            Layout left = plotRow.Add<Layout>("Plot");
            left.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false);
            left.Finish();
            graph = new LineGraph(w, left, 512, height, builtLabels, r) { Follow = false };
            plotSize = graph.Plot.GetComponent<LayoutElement>();
            plotSize.flexibleWidth = 1;
            labelsSize = graph.Plot.parent.Find("YLabels").GetComponent<LayoutElement>();

            graph.Stages.Source = new Markers(Plan.PhaseBoundaries);
            graph.Stages.color = PhaseColor;
            w.Visible(graph.Stages.gameObject, () => g.ShowPhases);

            nodeLines = Marker("Nodes", new Markers(Plan.NodeBoundaries), NodeColor);
            w.Visible(nodeLines.gameObject, () => g.ShowNodes);
            knotLines = Marker("Knots", new Markers(Plan.Knots), KnotColor);
            w.Visible(knotLines.gameObject, () => g.ShowKnots);

            // the pad Solution first, under the others
            for (int i = 0; i < Infos.Length; i++)
            {
                int k = i;
                SeriesInfo info = Infos[i];
                padLines[i] = graph.AddSeries(new TraceSeries(() => g.PadPlan, info), PadShade(info.Color));
                padLines[i].Thickness = PadThickness;
                w.Visible(padLines[i].gameObject, () => g.Displayed(k) && !g.PlanIsPad);
            }

            for (int i = 0; i < Infos.Length; i++)
            {
                int k = i;
                SeriesInfo info = Infos[i];
                flownLines[i] = graph.AddSeries(info.Flown.HasValue ? new FlownSeries(this, info.Flown.Value, info.Factor) : null,
                    FlownShade(info.Color));
                flownLines[i].Thickness = FlownThickness;
                w.Visible(flownLines[i].gameObject, () => g.ShowFlown && g.Displayed(k));

                planLines[i] = graph.AddSeries(new TraceSeries(() => g.Plan, info), info.Color);
                planLines[i].Thickness = PlanThickness;
                w.Visible(planLines[i].gameObject, () => g.Displayed(k));
            }

            nowLine = Marker("Now", now, NowColor);
            extraMarkers = new[] { nodeLines, knotLines, nowLine };
            BuildScaleCombo(w);
        }

        // what each line and mark is, drawn as on the plot; the marks that can be hidden carry their switch
        void BuildLegend(KodeWindow w, UIObject r)
        {
            var white = new Color(0.95f, 0.95f, 0.95f);

            Layout lines = KodeWindow.FlowWrap(KodeWindow.Row(r), 10);
            LegendItem(lines, white, PlanThickness, false, "Current solution");
            LegendItem(lines, PadShade(white), PadThickness, false, "Pad solution (the one it lifted off with)");
            w.Toggle(LegendCell(lines, FlownShade(white), FlownThickness, false), "Flown", () => g.ShowFlown, v => g.ShowFlown = v);

            Layout marks = KodeWindow.FlowWrap(KodeWindow.Row(r), 10);
            w.Toggle(LegendCell(marks, PhaseColor, 1, true), "Phase start/end", () => g.ShowPhases, v => g.ShowPhases = v);
            w.Toggle(LegendCell(marks, NodeColor, 1, true), "Node edge", () => g.ShowNodes, v => g.ShowNodes = v);
            w.Toggle(LegendCell(marks, KnotColor, 1, true), "Collocation point", () => g.ShowKnots, v => g.ShowKnots = v);
            LegendItem(marks, NowColor, 1, true, "Vessel's MET");

            UIText hint = w.Wrap(KodeWindow.Label(r,
                "Each phase is a chain of nodes, each a cubic through two collocation points: the optimizer enforces the Qα and Q limits " +
                "only at those points.", true));
            KodeWindow.ToneText(hint, KodeWindow.Tone.Dim);
        }

        // a swatch then a label, kept together when the row wraps
        static void LegendItem(UIObject parent, Color color, float thickness, bool vertical, string text) =>
            KodeWindow.Label(LegendCell(parent, color, thickness, vertical), text);

        static Layout LegendCell(UIObject parent, Color color, float thickness, bool vertical)
        {
            Layout cell = KodeWindow.Row(parent);
            cell.FlexibleLayout(0f, 0f);
            cell.Spacing(4);
            RectTransform swatch = Graphs.Surface(cell, 18, 14, "Swatch");
            var bar = Graphs.Fill<Image>(swatch, "Bar");
            bar.color = color;
            bar.raycastTarget = false;
            RectTransform rt = bar.rectTransform;
            if (vertical)
            {
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 1);
                rt.sizeDelta = new Vector2(Math.Max(thickness, 1.5f), 0);
            }
            else
            {
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.sizeDelta = new Vector2(0, thickness);
            }

            return cell;
        }

        LineSeriesGraphic Marker(string name, IGraphSource source, Color color)
        {
            var m = Graphs.Fill<LineSeriesGraphic>(graph.Plot, name);
            m.VerticalMarkers = true;
            m.Thickness = 1;
            m.color = color;
            m.raycastTarget = false;
            m.Source = source;
            return m;
        }

        /// <summary>[−] value [+] on a value the window computes: a stepper without a field.</summary>
        static void Nudge(KodeWindow w, UIObject parent, string template, Func<string> value, Action minus, Action plus)
        {
            Layout row = KodeWindow.Row(parent);
            row.FlexibleLayout(0f, 0f);
            row.Spacing(1);
            KodeWindow.Button(row, "-", minus);
            UIText t = w.BoundLabel(row, template, value);
            t.tmpText.alignment = TextAlignmentOptions.Center;
            KodeWindow.Button(row, "+", plus);
        }

        // ---- the drop-down over the plot: the displayed series, whose Y labels are shown ----

        readonly List<int> scaleSeries = new List<int>();
        string[] scaleNames;
        int scaleNamesMask = -1;

        string[] ScaleNames()
        {
            int mask = g.DisplayedMask;
            if (mask == scaleNamesMask) return scaleNames;
            scaleNamesMask = mask;

            scaleSeries.Clear();
            for (int i = 0; i < Infos.Length; i++)
                if (g.Displayed(i)) scaleSeries.Add(i);

            scaleNames = new string[scaleSeries.Count];
            for (int i = 0; i < scaleNames.Length; i++)
                scaleNames[i] = Infos[scaleSeries[i]].Label;
            // the Y labels must belong to a displayed series
            if (scaleSeries.Count > 0 && !scaleSeries.Contains(g.ScaleIdx)) g.ScaleIdx = scaleSeries[0];
            return scaleNames;
        }

        int ScalePosition()
        {
            ScaleNames();
            int p = scaleSeries.IndexOf(g.ScaleIdx);
            return p < 0 ? 0 : p;
        }

        void BuildScaleCombo(KodeWindow w)
        {
            // over the plot, after the series: the Plot surface has no layout group, so a new selection stays out of the window's layout
            Layout corner = KodeWindow.Row(graph.Plot.GetComponent<UIObject>());
            corner.PreferredSizeFitter(true, true).Anchor(AnchorPresets.TopLeft).Pivot(PivotPresets.TopLeft);
            corner.rectTransform.anchoredPosition = new Vector2(6, -6);
            new ComboBox(w, corner, ScalePosition, i => g.ScaleIdx = scaleSeries[i], ScaleNames, false, i => Infos[scaleSeries[i]].Color);
            w.Visible(corner.gameObject, () => ScaleNames().Length > 0);
        }
    }
}
