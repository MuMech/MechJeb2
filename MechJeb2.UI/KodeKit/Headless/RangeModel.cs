#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>What lies under the pointer on a graph with a <see cref="RangeModel"/>.</summary>
    public enum RangePart { None, Start, End, Body, Point }

    /// <summary>What a pointer drag on a <see cref="RangeModel"/> is doing.</summary>
    public enum RangeDrag { None, Create, Start, End, Move, Point }

    /// <summary>
    /// A range selection and marked points on one axis of a graph, without Unity (ui-sdk's spans and areas): pick a time range on
    /// the Flight Recorder, mark a moment to read its values. It works in data units over a <see cref="ViewportModel"/>, which
    /// maps them to the plot (<see cref="ViewportModel.ToFraction"/>) and gives the data's limits; the pointer methods take
    /// fractions of the plot, so nothing here depends on its size.
    /// <list type="bullet">
    /// <item>A range runs from <see cref="Start"/> to <see cref="End"/> with Start ≤ End however it was given (the ends of
    /// <see cref="Select"/> are put in order), inside the data (<see cref="ViewportModel.DataMin"/> to DataMax), and at least
    /// <see cref="MinWidth"/> wide (0: the viewport's <see cref="ViewportModel.MinSpan"/>, so a range can always be zoomed to).
    /// A selection narrower than that leaves no range; data narrower than it takes none.</item>
    /// <item>Each end moves on its own (<see cref="SetStart"/>, <see cref="SetEnd"/>) and stops <see cref="MinWidth"/> short of
    /// the other: the ends never cross. A move of the whole range (<see cref="MoveBy"/>, <see cref="MoveTo"/>) keeps its width
    /// and stops at the data's ends.</item>
    /// <item>Points are kept in the order they were added, each inside the data, at most <see cref="MaxPoints"/> (a new one past
    /// that drops the oldest, and ends a drag of it). <see cref="ActivePoint"/> is the last one added or moved, the one a readout shows.
    /// <see cref="NearestPoint"/> finds the point nearest a value within a distance.</item>
    /// <item>The pointer: <see cref="HitTest"/> looks at points first, then the range's ends, then its body, each within
    /// <see cref="PickFraction"/> of the plot. A drag that starts on an end moves that end, on a point moves it, on the body (when
    /// the gesture selects) moves the whole range, elsewhere (when it selects) draws a new range from where it started; the value
    /// grabbed stays under the pointer. A drag that grabs nothing returns false, and the host pans instead.</item>
    /// <item>A click (<see cref="Click"/>) on a point removes it, elsewhere marks a new one.</item>
    /// <item>When the data shrinks or is replaced (<see cref="ClampToData"/>), the range and points move inside it; a range left
    /// narrower than <see cref="MinWidth"/> is cleared.</item>
    /// <item><see cref="Version"/> counts every change, for a host to redraw. A non-finite argument changes nothing and returns
    /// false.</item>
    /// </list>
    /// </summary>
    public sealed class RangeModel : IDescribeParts
    {
        /// <summary>What the graph is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The range's two ends (sliders; grabbed while dragged) when there is one, and each marked point (the active one on).</summary>
        public void Describe(List<PartDescription> into)
        {
            bool named = !string.IsNullOrEmpty(Label);
            if (HasRange)
            {
                bool moving = Dragging == RangeDrag.Move || Dragging == RangeDrag.Create;
                into.Add(new PartDescription
                {
                    Name = "start", Role = PartRole.Slider, Label = named ? "range start " + Label : null,
                    Flags = moving || Dragging == RangeDrag.Start ? PartFlags.Grabbed : PartFlags.None
                });
                into.Add(new PartDescription
                {
                    Name = "end", Role = PartRole.Slider, Label = named ? "range end " + Label : null,
                    Flags = moving || Dragging == RangeDrag.End ? PartFlags.Grabbed : PartFlags.None
                });
            }
            for (int i = 0; i < points.Count; i++)
                into.Add(new PartDescription
                {
                    Name = "point." + i, Role = PartRole.Slider, Label = named ? "point " + (i + 1) + " " + Label : null,
                    Flags = (i == ActivePoint ? PartFlags.On : PartFlags.None) | (Dragging == RangeDrag.Point && i == DraggedPoint ? PartFlags.Grabbed : PartFlags.None)
                });
        }

        public RangeModel(ViewportModel view) => View = view ?? throw new ArgumentNullException(nameof(view));

        /// <summary>The axis the range and points are on: its data limits and its mapping to the plot.</summary>
        public readonly ViewportModel View;

        /// <summary>The narrowest range, in data units (0: the viewport's MinSpan, or a billionth of the data's extent).</summary>
        public double MinWidth;

        /// <summary>How near an end or a point the pointer must be to grab it, as a fraction of the plot's width.</summary>
        public double PickFraction = 0.015;

        /// <summary>The most points kept; a new one past that drops the oldest.</summary>
        public int MaxPoints = 8;

        public bool HasRange { get; private set; }

        /// <summary>The range's start, in data units (meaningful while <see cref="HasRange"/>).</summary>
        public double Start { get; private set; }

        /// <summary>The range's end, in data units: at least <see cref="Start"/> + <see cref="EffectiveMinWidth"/>.</summary>
        public double End { get; private set; }

        public double Width => HasRange ? End - Start : 0;

        /// <summary>Bumped by every change of the range or the points.</summary>
        public int Version { get; private set; }

        /// <summary>The source of the last change (for an observer, ui-sdk's change source).</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>What the drag in progress moves.</summary>
        public RangeDrag Dragging { get; private set; }

        /// <summary>The point the drag in progress moves (-1 when it moves none).</summary>
        public int DraggedPoint { get; private set; } = -1;

        readonly List<double> points = new List<double>();

        public int PointCount => points.Count;

        public double Point(int i) => points[i];

        /// <summary>The last point added or moved, the one a readout shows; -1 when there is none.</summary>
        public int ActivePoint { get; private set; } = -1;

        public double EffectiveMinWidth
        {
            get
            {
                if (MinWidth > 0) return MinWidth;
                if (View.MinSpan > 0) return View.MinSpan;
                return Math.Max(View.Extent, 1) * 1e-9;
            }
        }

        // ---- the range ----

        /// <summary>Selects from <paramref name="from"/> to <paramref name="to"/>, in either order, clamped to the data. False,
        /// with the range unchanged, when the clamped range is narrower than the minimum width.</summary>
        public bool Select(double from, double to, ChangeSource source)
        {
            if (!Finite(from) || !Finite(to)) return false;
            if (to < from)
            {
                double t = to;
                to = from;
                from = t;
            }

            from = ClampX(from);
            to = ClampX(to);
            if (to - from < EffectiveMinWidth) return false;
            return SetRange(from, to, source);
        }

        /// <summary>Moves the start, no nearer the end than the minimum width, nor before the data.</summary>
        public bool SetStart(double x, ChangeSource source)
        {
            if (!HasRange || !Finite(x)) return false;
            x = Math.Max(View.DataMin, Math.Min(x, End - EffectiveMinWidth));
            return SetRange(x, End, source);
        }

        /// <summary>Moves the end, no nearer the start than the minimum width, nor past the data.</summary>
        public bool SetEnd(double x, ChangeSource source)
        {
            if (!HasRange || !Finite(x)) return false;
            x = Math.Min(View.DataMax, Math.Max(x, Start + EffectiveMinWidth));
            return SetRange(Start, x, source);
        }

        /// <summary>Moves the whole range by data units, keeping its width, up to the data's ends.</summary>
        public bool MoveBy(double units, ChangeSource source) => HasRange && Finite(units) && MoveTo(Start + units, source);

        /// <summary>Puts the range's start at a value, keeping its width, up to the data's ends.</summary>
        public bool MoveTo(double start, ChangeSource source)
        {
            if (!HasRange || !Finite(start)) return false;
            double width = End - Start;
            start = Math.Max(View.DataMin, Math.Min(start, View.DataMax - width));
            return SetRange(start, start + width, source);
        }

        public bool Clear(ChangeSource source)
        {
            if (!HasRange) return false;
            HasRange = false;
            Changed(source);
            return true;
        }

        /// <summary>Zooms the viewport to the range (<see cref="ViewportModel.Show"/>). False without a range.</summary>
        public bool ZoomToRange(ChangeSource source) => HasRange && View.Show(Start, End, source);

        /// <summary>The range contains a value (ends included).</summary>
        public bool Contains(double x) => HasRange && x >= Start && x <= End;

        // ---- points ----

        /// <summary>Marks a point, clamped to the data; it becomes the active one. Returns its index (-1 for a bad value).</summary>
        public int AddPoint(double x, ChangeSource source)
        {
            if (!Finite(x) || MaxPoints <= 0) return -1;
            while (points.Count >= MaxPoints)
            {
                points.RemoveAt(0);
                // the point being dragged is the one dropped: that drag is over
                if (DraggedPoint == 0) EndDrag();
                else if (DraggedPoint > 0) DraggedPoint--;
            }

            points.Add(ClampX(x));
            ActivePoint = points.Count - 1;
            Changed(source);
            return ActivePoint;
        }

        /// <summary>Moves a point, clamped to the data; it becomes the active one.</summary>
        public bool MovePoint(int i, double x, ChangeSource source)
        {
            if (i < 0 || i >= points.Count || !Finite(x)) return false;
            x = ClampX(x);
            bool moved = !points[i].Equals(x) || ActivePoint != i;
            points[i] = x;
            ActivePoint = i;
            if (moved) Changed(source);
            return moved;
        }

        public bool RemovePoint(int i, ChangeSource source)
        {
            if (i < 0 || i >= points.Count) return false;
            points.RemoveAt(i);
            if (DraggedPoint == i)
            {
                DraggedPoint = -1;
                if (Dragging == RangeDrag.Point) Dragging = RangeDrag.None;
            }
            else if (DraggedPoint > i) DraggedPoint--;

            // the newest left is the active one
            ActivePoint = points.Count - 1;
            Changed(source);
            return true;
        }

        public bool ClearPoints(ChangeSource source)
        {
            if (points.Count == 0) return false;
            points.Clear();
            ActivePoint = DraggedPoint = -1;
            if (Dragging == RangeDrag.Point) Dragging = RangeDrag.None;
            Changed(source);
            return true;
        }

        /// <summary>The point nearest <paramref name="x"/> no farther than <paramref name="maxDistance"/> (data units); -1 when none.
        /// Of two at the same distance, the one added last.</summary>
        public int NearestPoint(double x, double maxDistance)
        {
            if (!Finite(x) || double.IsNaN(maxDistance)) return -1;
            int best = -1;
            double bestD = maxDistance;
            for (int i = 0; i < points.Count; i++)
            {
                double d = Math.Abs(points[i] - x);
                if (d <= bestD)
                {
                    best = i;
                    bestD = d;
                }
            }

            return best;
        }

        // ---- the pointer, in fractions of the plot ----

        /// <summary>What is under the pointer at <paramref name="fraction"/> of the plot: a point (its index in
        /// <paramref name="point"/>), an end, the range's body, or nothing.</summary>
        public RangePart HitTest(double fraction, out int point)
        {
            point = -1;
            if (!Finite(fraction)) return RangePart.None;
            double x = View.FromFraction(fraction), pick = Math.Max(0, PickFraction) * View.Span;
            point = NearestPoint(x, pick);
            if (point >= 0) return RangePart.Point;
            if (!HasRange) return RangePart.None;
            double toStart = Math.Abs(x - Start), toEnd = Math.Abs(x - End);
            if (Math.Min(toStart, toEnd) <= pick) return toStart < toEnd || (toStart.Equals(toEnd) && x < Start) ? RangePart.Start : RangePart.End;
            return Contains(x) ? RangePart.Body : RangePart.None;
        }

        double grabOffset; // the value grabbed, from what the drag moves (an end, the start, a point)
        double anchor;     // a new range's first end

        /// <summary>
        /// A drag starts at <paramref name="fraction"/> of the plot. An end or a point under it is grabbed; with
        /// <paramref name="select"/> (the gesture that selects), the body moves the range and anywhere else starts a new one.
        /// False when it grabs nothing: the host's own drag (a pan) takes it.
        /// </summary>
        public bool BeginDrag(double fraction, bool select, ChangeSource source)
        {
            EndDrag();
            if (!Finite(fraction)) return false;
            double x = View.FromFraction(fraction);
            switch (HitTest(fraction, out int point))
            {
                case RangePart.Point:
                    Dragging = RangeDrag.Point;
                    DraggedPoint = point;
                    grabOffset = x - points[point];
                    MovePoint(point, points[point], source); // it becomes the active one
                    return true;
                case RangePart.Start:
                    Dragging = RangeDrag.Start;
                    grabOffset = x - Start;
                    return true;
                case RangePart.End:
                    Dragging = RangeDrag.End;
                    grabOffset = x - End;
                    return true;
                case RangePart.Body when select:
                    Dragging = RangeDrag.Move;
                    grabOffset = x - Start;
                    return true;
            }

            if (!select) return false;
            Dragging = RangeDrag.Create;
            anchor = ClampX(x);
            Clear(source);
            return true;
        }

        /// <summary>The drag in progress reaches <paramref name="fraction"/> of the plot. True when something changed.</summary>
        public bool DragTo(double fraction, ChangeSource source)
        {
            if (!Finite(fraction)) return false;
            double x = View.FromFraction(fraction);
            switch (Dragging)
            {
                case RangeDrag.Create:
                    double to = ClampX(x);
                    if (Math.Abs(to - anchor) >= EffectiveMinWidth) return SetRange(Math.Min(anchor, to), Math.Max(anchor, to), source);
                    return Clear(source);
                case RangeDrag.Start: return SetStart(x - grabOffset, source);
                case RangeDrag.End: return SetEnd(x - grabOffset, source);
                case RangeDrag.Move: return MoveTo(x - grabOffset, source);
                case RangeDrag.Point: return MovePoint(DraggedPoint, x - grabOffset, source);
                default: return false;
            }
        }

        public void EndDrag()
        {
            Dragging = RangeDrag.None;
            DraggedPoint = -1;
        }

        /// <summary>A click at <paramref name="fraction"/> of the plot: removes the point under it, or marks a new one there.</summary>
        public bool Click(double fraction, ChangeSource source)
        {
            if (!Finite(fraction)) return false;
            if (HitTest(fraction, out int point) == RangePart.Point) return RemovePoint(point, source);
            return AddPoint(View.FromFraction(fraction), source) >= 0;
        }

        // ---- the data ----

        /// <summary>Moves the range and the points back inside the viewport's data, after it shrank or was replaced; clears a range
        /// left too narrow. True when anything changed. Nothing to do while the data only grows.</summary>
        public bool ClampToData(ChangeSource source = ChangeSource.Program)
        {
            bool changed = false;
            for (int i = 0; i < points.Count; i++)
            {
                double x = ClampX(points[i]);
                if (x.Equals(points[i])) continue;
                points[i] = x;
                changed = true;
            }

            double s = ClampX(Start), e = ClampX(End);
            // a range the data still holds stays as it is: one at the minimum width can be a rounding error under it
            if (HasRange && (!s.Equals(Start) || !e.Equals(End)))
            {
                if (e - s < EffectiveMinWidth)
                {
                    HasRange = false;
                    if (Dragging != RangeDrag.Create) EndDrag();
                    changed = true;
                }
                else
                {
                    Start = s;
                    End = e;
                    changed = true;
                }
            }

            if (changed) Changed(source);
            return changed;
        }

        /// <summary>Clears the range and the points (the data restarted).</summary>
        public bool Reset(ChangeSource source)
        {
            EndDrag();
            bool a = Clear(source), b = ClearPoints(source);
            return a || b;
        }

        // ---- the rules ----

        static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);

        double ClampX(double x) => Math.Max(View.DataMin, Math.Min(x, View.DataMax));

        bool SetRange(double start, double end, ChangeSource source)
        {
            if (HasRange && start.Equals(Start) && end.Equals(End)) return false;
            HasRange = true;
            Start = start;
            End = end;
            Changed(source);
            return true;
        }

        void Changed(ChangeSource source)
        {
            LastSource = source;
            Version++;
        }
    }
}
