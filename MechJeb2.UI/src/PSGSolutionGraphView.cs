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
    /// MechJebModulePSGSolutionGraph ("PSG Solution"): the Solution guidance flies, against MET, on a KodeKit LineGraph. Each
    /// quantity has its own Y range, and the drop-down in the plot's corner picks the one the Y labels show. The plan is a solid
    /// line, what the vessel flew a thin one over it. Vertical lines mark the phase boundaries and, on request, the interpolant
    /// nodes' bounds and the collocation points, where the optimizer applies the Qα and Q limits: between them the limits
    /// aren't enforced. A white line is the vessel's MET. Zoom narrows the visible span, which the scrollbar moves.
    /// </summary>
    [KodeViewFor(typeof(MechJebModulePSGSolutionGraph))]
    public class PSGSolutionGraphView : KodeView
    {
        readonly MechJebModulePSGSolutionGraph g;

        public PSGSolutionGraphView(MechJebModulePSGSolutionGraph g) : base(g) => this.g = g;

        protected override float MinWidth => 460;

        static SeriesInfo[] Infos => MechJebModulePSGSolutionGraph.Series;

        SolutionTrace Plan => g.Plan;

        PSGFlownTrack Flown => g.Flown;

        // ---- data sources (no allocation) ----

        sealed class PlanSeries : IGraphSource
        {
            readonly PSGSolutionGraphView v;
            readonly Column column;
            readonly double factor;

            public PlanSeries(PSGSolutionGraphView v, SeriesInfo info)
            {
                this.v = v;
                column = info.Column;
                factor = info.Factor;
            }

            public int Count => v.Plan.Count;
            public double X(int i) => v.Plan[Column.MET, i];
            public double Y(int i) => v.Plan[column, i] * factor;
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
        readonly LineSeriesGraphic[] flownLines = new LineSeriesGraphic[Infos.Length];
        LineSeriesGraphic nodeLines, knotLines, nowLine;
        LineSeriesGraphic[] extraMarkers;
        readonly NowMarker now = new NowMarker();
        readonly double[] yMin = new double[Infos.Length], yMax = new double[Infos.Length];
        int builtVSize;

        int lastRefreshFrame = int.MinValue / 2;
        float lastRefreshTime;
        int lastMask = -1, lastZoom = -1, lastScaleIdx = -1, lastSampled = -1, lastFlownCount = -1;
        bool lastFlown, lastNodes, lastKnots;
        double span = 1;

        protected override void BeforeSync()
        {
            g.ClampSettings();
            if (g.VSize != builtVSize)
            {
                RebuildRequested = true;
                return;
            }

            bool planChanged = g.Refresh() || g.Sampled != lastSampled;
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
            lastMask = mask;
            lastScaleIdx = g.ScaleIdx;
            lastFlown = g.ShowFlown;
            lastNodes = g.ShowNodes;
            lastKnots = g.ShowKnots;
            lastFlownCount = Flown.Count;
            lastRefreshFrame = Time.frameCount;
            lastRefreshTime = Time.unscaledTime;

            if (rangesStale) ComputeRanges();

            span = Math.Max(Math.Max(Plan.Tf, Flown.LastMET), 1);
            double visible = span / (1 << g.Zoom);
            if (g.Zoom != lastZoom)
            {
                // zoom about the middle of the view
                double middle = graph.ViewMin + graph.VisibleWidth / 2;
                graph.ViewMin = middle - visible / 2;
                lastZoom = g.Zoom;
            }

            graph.DataMaxX = span;
            graph.VisibleWidth = visible;
            graph.ViewMin = Math.Max(0, Math.Min(graph.ViewMin, span - visible));

            graph.Refresh(yMin[g.ScaleIdx], yMax[g.ScaleIdx]);
            for (int i = 0; i < Infos.Length; i++)
            {
                planLines[i].YMin = flownLines[i].YMin = yMin[i];
                planLines[i].YMax = flownLines[i].YMax = yMax[i];
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

        // each series' Y range: its data's, the plan's and (when shown) the flown one's, widened to its fixed range
        void ComputeRanges()
        {
            for (int i = 0; i < Infos.Length; i++)
            {
                SeriesInfo info = Infos[i];
                double lo = double.PositiveInfinity, hi = double.NegativeInfinity;
                if (Plan.Count > 0)
                {
                    (double pMin, double pMax) = Plan.Range(info.Column);
                    lo = pMin * info.Factor;
                    hi = pMax * info.Factor;
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
                if (!double.IsNaN(info.Min) && info.Min >= 0 && lo < 0 && !(lo < info.Min)) lo = 0;
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

        // the plan's peaks: what the limits and the steering reach
        void Summarize()
        {
            if (Plan.Count == 0)
            {
                summaryText = "";
                return;
            }

            int qa = ArgMax(Column.Q_ALPHA_FRACTION), q = ArgMax(Column.Q_FRACTION), aoa = ArgMax(Column.AOA);
            var parts = new List<string>();
            if (Plan.Aero.HasQAlphaLimit)
                parts.Add($"max Qα {Plan[Column.Q_ALPHA_FRACTION, qa] * 100:F0} % of limit at MET {Plan[Column.MET, qa]:F0} s");
            if (Plan.Aero.HasQLimit)
                parts.Add($"max Q {Plan[Column.Q_FRACTION, q] * 100:F0} % at MET {Plan[Column.MET, q]:F0} s");
            if (Plan.Aero.Atmosphere)
                parts.Add($"max AoA {Plan[Column.AOA, aoa]:F1}° at MET {Plan[Column.MET, aoa]:F0} s");
            else
                parts.Add("no atmosphere in the plan");
            summaryText = "Plan: " + string.Join(", ", parts);
        }

        int ArgMax(Column c)
        {
            int best = 0;
            for (int i = 1; i < Plan.Count; i++)
                if (Plan[c, i] > Plan[c, best])
                    best = i;
            return best;
        }

        // ---- building ----

        protected override void BuildContents(KodeWindow w)
        {
            UIObject r = w.Root;
            g.ClampSettings();
            builtVSize = g.VSize;
            lastSampled = lastMask = lastZoom = lastScaleIdx = lastFlownCount = -1;

            Layout top = KodeWindow.Row(r);
            w.BoundLabel(top, "Solution 999: MET 9999 to 9999 s, 99 phases (held, guidance has a newer one)", StatusText, true);
            w.Toggle(top, "Hold", () => g.Hold, v => g.Hold = v);
            KodeWindow.Button(top, "CSV", () => g.ExportCsv());

            Layout options = KodeWindow.Row(r);
            KodeWindow.Label(options, "Zoom");
            Nudge(w, options, "×256", () => "×" + (1 << g.Zoom), () => g.Zoom--, () => g.Zoom++);
            w.BoundLabel(options, "MET 9999 to 9999 s", ViewText);
            KodeWindow.FlexibleSpace(options);
            KodeWindow.Label(options, "Height");
            Nudge(w, options, "9", () => g.VSize.ToString(), () => g.VSize--, () => g.VSize++);

            Layout marks = KodeWindow.Row(r);
            w.Toggle(marks, "Flown", () => g.ShowFlown, v => g.ShowFlown = v);
            w.Toggle(marks, "Phases", () => g.ShowPhases, v => g.ShowPhases = v);
            w.Toggle(marks, "Nodes", () => g.ShowNodes, v => g.ShowNodes = v);
            w.Toggle(marks, "Collocation points", () => g.ShowKnots, v => g.ShowKnots = v);
            KodeWindow.FlexibleSpace(marks);

            Layout chips = KodeWindow.FlowWrap(KodeWindow.Row(r), 4);
            for (int i = 0; i < Infos.Length; i++)
            {
                int k = i;
                w.Chip(chips, Infos[i].Label, () => g.Displayed(k), v => g.SetDisplayed(k, v), Infos[i].Color);
            }

            BuildPlot(w, r);

            UIText summary = w.Wrap(w.BoundLabel(r, () => summaryText, true));
            KodeWindow.ToneText(summary, KodeWindow.Tone.Dim);
        }

        void BuildPlot(KodeWindow w, UIObject r)
        {
            int height = 128 * g.VSize;
            Layout plotRow = KodeWindow.Row(r);
            plotRow.Padding(0, 0, 5, 5);
            Layout left = plotRow.Add<Layout>("Plot");
            left.Vertical().ControlChildSize(true, true).ChildForceExpand(false, false);
            left.Finish();
            graph = new LineGraph(w, left, 512, height, Mathf.Clamp(height / 24 + 1, 3, 11), r) { Follow = false };
            graph.Plot.GetComponent<LayoutElement>().flexibleWidth = 1;

            graph.Stages.Source = new Markers(Plan.PhaseBoundaries);
            graph.Stages.color = new Color(0.75f, 0.75f, 0.75f, 0.8f);
            w.Visible(graph.Stages.gameObject, () => g.ShowPhases);

            nodeLines = Marker("Nodes", new Markers(Plan.NodeBoundaries), new Color(0.5f, 0.5f, 0.5f, 0.35f));
            w.Visible(nodeLines.gameObject, () => g.ShowNodes);
            knotLines = Marker("Knots", new Markers(Plan.Knots), new Color(0.9f, 0.8f, 0.2f, 0.3f));
            w.Visible(knotLines.gameObject, () => g.ShowKnots);

            for (int i = 0; i < Infos.Length; i++)
            {
                int k = i;
                SeriesInfo info = Infos[i];
                planLines[i] = graph.AddSeries(new PlanSeries(this, info), info.Color);
                w.Visible(planLines[i].gameObject, () => g.Displayed(k));

                Color faded = info.Color;
                faded.a = 0.7f;
                flownLines[i] = graph.AddSeries(info.Flown.HasValue ? new FlownSeries(this, info.Flown.Value, info.Factor) : null, faded);
                flownLines[i].Thickness = 1;
                w.Visible(flownLines[i].gameObject, () => g.ShowFlown && g.Displayed(k));
            }

            nowLine = Marker("Now", now, new Color(1, 1, 1, 0.6f));
            extraMarkers = new[] { nodeLines, knotLines, nowLine };
            BuildScaleCombo(w);
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
