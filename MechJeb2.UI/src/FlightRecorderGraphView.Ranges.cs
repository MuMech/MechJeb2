using System;
using KodeKit;
using KodeKit.Headless;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GraphInfo = MuMech.MechJebModuleFlightRecorderGraph.GraphInfo;
using RecordType = MuMech.MechJebModuleFlightRecorder.RecordType;

namespace MuMech.UI
{
    /// <summary>
    /// The Flight Recorder's range and points (<see cref="LineGraph.Ranges"/>, a <see cref="RangeModel"/> on the X axis): a right
    /// drag on the plot picks a range, a right click marks a point. Under the plot, a readout gives the range's ends and width, the
    /// active point's place, and for each displayed graph its value at the point and its change, minimum and maximum over the
    /// range, read off the recorder's samples (<see cref="SampleStats"/>). Zoom to range shows the range in the plot.
    /// </summary>
    public partial class FlightRecorderGraphView
    {
        // the readout's rows; one table row per graph, in graphInfos' order
        CharText rangeReadout, pointReadout;
        GameObject rangeRow, pointRow, hintRow, tableHeader, pointHeader, rangeHeaders;
        readonly TableRow[] tableRows = new TableRow[MechJebModuleFlightRecorderGraph.graphInfos.Length];
        int readVersion = -1, readCount = -1, readMask = -1;
        bool readDownrange;
        float readTime;

        sealed class TableRow
        {
            public GameObject Row, PointCell, RangeCells;
            public CharText Point, Delta, Min, Max;
            public Series Samples;
            public RecordType Type;
        }

        /// <summary>The plot's range and points (null before the plot is built).</summary>
        public RangeModel Ranges => graph?.Ranges;

        /// <summary>For tests: the text written in a graph's cell (column 0: the point, 1: the change, 2: min, 3: max), or in the
        /// range's and the point's own rows (type -1, column 0 or 1). Allocates.</summary>
        public string ReadoutText(int type, int column)
        {
            if (type < 0) return (column == 0 ? rangeReadout : pointReadout)?.ToString();
            foreach (TableRow row in tableRows)
                if (row != null && (int)row.Type == type)
                    return (column == 0 ? row.Point : column == 1 ? row.Delta : column == 2 ? row.Min : row.Max).ToString();
            return null;
        }

        void BuildReadout(KodeWindow w, UIObject r)
        {
            readVersion = readCount = readMask = -1;
            // what to do when nothing is picked
            Layout hint = KodeWindow.Row(r);
            hintRow = hint.gameObject;
            UIText hintText = KodeWindow.Label(hint, "#MechJeb_NewUI_GraphRangeHint");
            KodeWindow.ToneText(hintText, KodeWindow.Tone.Dim);
            w.Visible(hintRow, () => Ranges != null && !Ranges.HasRange && Ranges.PointCount == 0);

            // the range: its ends and width, zoom to it, clear it
            Layout range = KodeWindow.Row(r);
            rangeRow = range.gameObject;
            rangeRow.name = "RangeRow";
            KodeWindow.Label(range, "#MechJeb_NewUI_GraphRange");
            UIText rangeLabel = KodeWindow.LiveLabel(range, "-999d 99h 99m 99.9s – 999d 99h 99m 99.9s, Δ 999d 99h 99m 99.9s", true);
            rangeReadout = new CharText(rangeLabel.tmpText);
            KodeWindow.Button(range, "#MechJeb_NewUI_GraphZoomToRange", () => Ranges?.ZoomToRange(KodeWindow.Pointer)).gameObject.name = "ZoomToRange";
            KodeWindow.Button(range, "#MechJeb_NewUI_GraphClearRange", () => Ranges?.Clear(KodeWindow.Pointer)).gameObject.name = "ClearRange";
            w.Visible(rangeRow, () => Ranges != null && Ranges.HasRange);

            // the active point: its place, clear the points
            Layout point = KodeWindow.Row(r);
            pointRow = point.gameObject;
            pointRow.name = "PointRow";
            KodeWindow.Label(point, "#MechJeb_NewUI_GraphPoint");
            UIText pointLabel = KodeWindow.LiveLabel(point, "999d 99h 99m 99.9s", true);
            pointReadout = new CharText(pointLabel.tmpText);
            KodeWindow.Button(point, "#MechJeb_NewUI_GraphClearPoints", () => Ranges?.ClearPoints(KodeWindow.Pointer)).gameObject.name = "ClearPoints";
            w.Visible(pointRow, () => Ranges != null && Ranges.PointCount > 0);

            // the table: a row per displayed graph, its name in its colour. The names and headers are plain labels at measured
            // widths (a live cell whose text is set once, at the build, stayed blank in Chinese); the values are live cells
            Layout header = KodeWindow.Row(r);
            tableHeader = header.gameObject;
            tableHeader.name = "ReadoutHeader";
            FixedLabel(header, "", NameWidth, false);
            pointHeader = HeaderCell(header, "#MechJeb_NewUI_GraphPoint").gameObject;
            Layout rangeHeads = KodeWindow.Row(header);
            rangeHeads.FlexibleLayout(0f, 0f);
            rangeHeaders = rangeHeads.gameObject;
            HeaderCell(rangeHeads, "#MechJeb_NewUI_GraphChange");
            HeaderCell(rangeHeads, "#MechJeb_NewUI_GraphMin");
            HeaderCell(rangeHeads, "#MechJeb_NewUI_GraphMax");
            w.Visible(tableHeader, AnyPicked);
            w.Visible(pointHeader, () => Ranges != null && Ranges.ActivePoint >= 0);
            w.Visible(rangeHeaders, () => Ranges != null && Ranges.HasRange);

            for (int i = 0; i < tableRows.Length; i++)
            {
                GraphInfo info = MechJebModuleFlightRecorderGraph.graphInfos[i];
                RecordType type = info.Type;
                var tr = new TableRow { Type = type, Samples = new Series(this, type) };
                Layout row = KodeWindow.Row(r);
                tr.Row = row.gameObject;
                tr.Row.name = "Readout" + type;
                UIText name = FixedLabel(row, info.ScaleLabel, NameWidth, false);
                Color color = info.Color;
                KodeWindow.Styled(name.gameObject, _ => name.tmpText.color = color);
                tr.PointCell = KodeWindow.CellOf(ValueCell(row, out tr.Point));
                Layout cells = KodeWindow.Row(row);
                cells.FlexibleLayout(0f, 0f);
                tr.RangeCells = cells.gameObject;
                ValueCell(cells, out tr.Delta);
                ValueCell(cells, out tr.Min);
                ValueCell(cells, out tr.Max);
                tableRows[i] = tr;
                w.Visible(tr.Row, () => AnyPicked() && g.displayed[(int)type]);
                w.Visible(tr.PointCell, () => Ranges != null && Ranges.ActivePoint >= 0);
                w.Visible(tr.RangeCells, () => Ranges != null && Ranges.HasRange);
            }

            w.AddSync(SyncReadout);
        }

        bool AnyPicked() => Ranges != null && (Ranges.HasRange || Ranges.PointCount > 0);

        // the widest value a cell shows
        const string ValueTemplate = "-999.9 kmm/s";

        // the widest graph name, in the theme's font
        static float NameWidth(Theme th)
        {
            float width = 0;
            foreach (GraphInfo info in MechJebModuleFlightRecorderGraph.graphInfos)
                width = Mathf.Max(width, TextMeasure.Measure(KodeWindow.Loc(info.ScaleLabel), th).x);
            return width + 8;
        }

        // a live cell's width for the template (KodeWindow.LiveLabel's rule), so a header sits over its column
        static float ValueWidth(Theme th) => TextMeasure.Measure(ValueTemplate, th).x + 24;

        // a plain label (a '#key' is localized and follows a language switch) held at a width the theme gives
        static UIText FixedLabel(UIObject row, string text, Func<Theme, float> width, bool right)
        {
            UIText t = KodeWindow.Label(row, text);
            LayoutElement le = t.GetComponent<LayoutElement>() ?? t.gameObject.AddComponent<LayoutElement>();
            KodeWindow.Styled(t.gameObject, th => le.preferredWidth = le.minWidth = width(th));
            if (right) t.tmpText.alignment = TextAlignmentOptions.Right;
            return t;
        }

        static UIText HeaderCell(UIObject row, string key)
        {
            UIText t = FixedLabel(row, key, ValueWidth, true);
            KodeWindow.ToneText(t, KodeWindow.Tone.Dim);
            return t;
        }

        static UIText ValueCell(UIObject row, out CharText chars)
        {
            UIText t = KodeWindow.LiveLabel(row, ValueTemplate, false);
            t.tmpText.alignment = TextAlignmentOptions.Right;
            chars = new CharText(t.tmpText);
            return t;
        }

        /// <summary>How long the last readout took to compute, in ms (for tests and measurements).</summary>
        public double ReadoutMs { get; private set; }

        // a readout dearer than this is computed ten times a second during a drag, not every frame
        const double DragThrottleMs = 0.5;

        // the texts, when the range, the points, the units, the graphs shown or the samples changed; at most ten times a second
        // while the recording grows, and during a drag when a readout costs more than DragThrottleMs (a long history, many
        // graphs: each one scans the samples), at once for the user's other changes
        void SyncReadout()
        {
            RangeModel ranges = Ranges;
            if (ranges == null || Rec == null) return;
            int count = PointCount, mask = DisplayedMask();
            bool picked = ranges.Version != readVersion || g.downrange != readDownrange || mask != readMask;
            bool recent = Time.unscaledTime - readTime < 0.1f;
            if (!picked && (count == readCount || recent)) return;
            if (picked && recent && ranges.Dragging != RangeDrag.None && ReadoutMs > DragThrottleMs) return;
            long started = System.Diagnostics.Stopwatch.GetTimestamp();
            readVersion = ranges.Version;
            readDownrange = g.downrange;
            readMask = mask;
            readCount = count;
            readTime = Time.unscaledTime;
            if (!ranges.HasRange && ranges.PointCount == 0) return;

            if (ranges.HasRange)
            {
                AppendX(rangeReadout.Clear(), ranges.Start).Append(" – ");
                AppendX(rangeReadout, ranges.End).Append(", Δ ");
                AppendX(rangeReadout, ranges.Width).Commit();
            }

            int active = ranges.ActivePoint;
            double at = active >= 0 ? ranges.Point(active) : double.NaN;
            if (active >= 0) AppendX(pointReadout.Clear(), at).Commit();

            foreach (TableRow tr in tableRows)
            {
                if (!g.displayed[(int)tr.Type]) continue;
                if (active >= 0)
                {
                    SampleStats.ValueAt(tr.Samples, at, out double y);
                    AppendValue(tr.Point.Clear(), tr.Type, y, false).Commit();
                }

                if (!ranges.HasRange) continue;
                if (SampleStats.Over(tr.Samples, ranges.Start, ranges.End, out RangeStats st))
                {
                    AppendValue(tr.Delta.Clear(), tr.Type, st.Delta, true).Commit();
                    AppendValue(tr.Min.Clear(), tr.Type, st.Min, false).Commit();
                    AppendValue(tr.Max.Clear(), tr.Type, st.Max, false).Commit();
                }
                else
                {
                    tr.Delta.Clear().Append('—').Commit();
                    tr.Min.Clear().Append('—').Commit();
                    tr.Max.Clear().Append('—').Commit();
                }
            }

            ReadoutMs = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }

        // a place on the X axis: a time since the mark, or a distance downrange
        CharText AppendX(CharText ct, double x)
        {
            AppendX(ct.Chars, x);
            return ct;
        }

        CharBuffer AppendX(CharBuffer b, double x) => g.downrange ? b.AppendSI(x, 4).Append('m') : b.AppendDHMS(x, 1);

        /// <summary>A graph's value in its unit (a change with its sign); a dash where there is none.</summary>
        internal static CharText AppendValue(CharText ct, RecordType type, double v, bool signed)
        {
            AppendValue(ct.Chars, type, v, signed);
            return ct;
        }

        /// <summary>A graph's value in its unit (a change with its sign) into a buffer; a dash where there is none.</summary>
        internal static CharBuffer AppendValue(CharBuffer ct, RecordType type, double v, bool signed)
        {
            if (double.IsNaN(v)) return ct.Append('—');
            if (signed && v > 0) ct.Append('+');
            switch (type)
            {
                case RecordType.ALTITUDE_ASL:
                case RecordType.ALTITUDE_TRUE:
                case RecordType.DOWN_RANGE:
                    return ct.AppendSI(v, 4).Append('m');
                case RecordType.SPEED_SURFACE:
                case RecordType.SPEED_ORBITAL:
                case RecordType.GRAVITY_LOSSES:
                case RecordType.DRAG_LOSSES:
                case RecordType.STEERING_LOSSES:
                case RecordType.DELTA_V_EXPENDED:
                    return ct.AppendSI(v, 4).Append("m/s");
                case RecordType.Q:
                    return ct.AppendSI(v, 4).Append("Pa");
                case RecordType.MASS:
                    return ct.Append(v, 3).Append(" t");
                case RecordType.ACCELERATION:
                    return ct.Append(v, 2).Append(" g");
                case RecordType.AO_A:
                case RecordType.AO_S:
                case RecordType.AO_D:
                case RecordType.PITCH:
                    return ct.Append(v, 1).Append('°');
                default:
                    return ct.AppendSI(v, 4);
            }
        }
    }
}
