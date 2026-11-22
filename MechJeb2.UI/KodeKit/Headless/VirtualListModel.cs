#if !CSHARP_7_3
#nullable disable
#endif
using System;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>Where <see cref="VirtualListModel.ScrollTo"/> puts a row in the viewport.</summary>
    public enum ScrollAlign
    {
        /// <summary>The row's top at the viewport's top.</summary>
        Start,

        /// <summary>The row's middle at the viewport's middle.</summary>
        Center,

        /// <summary>The row's bottom at the viewport's bottom.</summary>
        End,

        /// <summary>No scroll when the row is whole in view; otherwise the least scroll that shows it (Start when it is taller than the viewport).</summary>
        Nearest
    }

    /// <summary>
    /// A long list that builds only the rows it shows, without Unity (ui-sdk's <c>createVirtualizer</c>): KodeKit's
    /// <c>VirtualList</c> draws it on a ScrollRect with a pool of rows bound by index. Offsets are in list units from the
    /// content's top, down.
    /// <list type="bullet">
    /// <item>Every row has the same height (<see cref="RowHeight"/>), or, after <see cref="SetRowHeights"/>, its own measured
    /// height (rows whose text wraps); <see cref="Spacing"/> separates them. One optional expanded row
    /// (<see cref="SetExpanded"/>) is taller by an extra height: an inline editor under the selected row.</item>
    /// <item>Measured heights are asked for every row when they are set and whenever the count changes or
    /// <see cref="RefreshHeights"/> is called (other data at the same count), never per scroll.</item>
    /// <item>The offset stays in [0, <see cref="MaxOffset"/>]; a list shorter than its viewport doesn't scroll.</item>
    /// <item>The visible rows are those that overlap the viewport; the rendered rows add <see cref="Overscan"/> rows on each
    /// side, within the list. An empty list, or rows of no height, render nothing (<see cref="RenderLast"/> below
    /// <see cref="RenderFirst"/>).</item>
    /// <item><see cref="PoolSize"/> rows are enough for any offset: the host's pool binds row <c>i</c> to slot
    /// <c>i % PoolSize</c>, so two rendered rows never share a slot.</item>
    /// <item>The anchor is the first visible row and how far the view is into it. A change of the count, a row's height, the
    /// spacing, the viewport or the expanded row keeps the anchor row where it was in the view (as far as the new limits
    /// allow); rows inserted or removed before it move the anchor with its row.</item>
    /// <item><see cref="ScrollTo"/> takes an index outside the list to its nearest row.</item>
    /// <item>While nothing is drawn (no row, or rows of no height yet) a scroll leaves the view at the top; rows inserted into an
    /// empty list show from the top; a count that drops the expanded row ends the expansion.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: a long list's geometry (which rows to build); the list and its rows have no part description yet (roadmap item 32)")]
    public sealed class VirtualListModel
    {
        float rowHeight, spacing, viewport, offset;
        int count, overscan = 2, expanded = -1;
        float expandedExtra;

        // measured heights: tops[i] is the sum of the heights of the rows above row i (spacing and expansion left out)
        Func<int, float> heightOf;
        float[] tops;
        float minHeight;

        // the anchor: the first visible row and the offset into it
        int anchor;
        float anchorDelta;

        public VirtualListModel(int count, float rowHeight, float viewportHeight, float spacing = 0)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            this.count = count;
            this.rowHeight = Math.Max(0, rowHeight);
            this.spacing = Math.Max(0, spacing);
            viewport = Math.Max(0, viewportHeight);
            Update();
        }

        public int Count => count;
        /// <summary>The rows' height, or the default when they have measured heights (<see cref="Measured"/>).</summary>
        public float RowHeight => rowHeight;

        /// <summary>True when each row has its own height (<see cref="SetRowHeights"/>).</summary>
        public bool Measured => heightOf != null;
        public float Spacing => spacing;
        public float ViewportHeight => viewport;

        /// <summary>The scroll offset: how far the viewport's top is below the content's top.</summary>
        public float Offset => offset;

        /// <summary>The source of the last scroll (for an observer, ui-sdk's change source).</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>Rows rendered beyond each end of the visible ones (2 by default), so a small scroll binds nothing.</summary>
        public int Overscan
        {
            get => overscan;
            set
            {
                overscan = Math.Max(0, value);
                GeometryVersion++;
                Update();
            }
        }

        /// <summary>The expanded row (-1: none) and how much taller than the others it is.</summary>
        public int Expanded => expanded;

        /// <summary>How much taller than the others the expanded row is (0 when none is).</summary>
        public float ExpandedExtra => expanded >= 0 ? expandedExtra : 0;

        /// <summary>Bumped by every change that can move a row: the count, a height, the spacing, the expanded row.</summary>
        public int GeometryVersion { get; private set; }

        public int FirstVisible { get; private set; }
        public int LastVisible { get; private set; } = -1;
        public int RenderFirst { get; private set; }
        public int RenderLast { get; private set; } = -1;
        public int RenderedCount => RenderLast - RenderFirst + 1;

        // nothing to draw: no row, or uniform rows of no height
        bool Empty => count == 0 || (heightOf == null && rowHeight <= 0);

        /// <summary>The whole list's height: the rows and the spacing between them.</summary>
        public float ContentHeight => count == 0 ? 0 : RowTop(count - 1) + HeightOf(count - 1);

        public float MaxOffset => Math.Max(0, ContentHeight - viewport);

        /// <summary>The top of row <paramref name="index"/> in the content.</summary>
        public float RowTop(int index) =>
            (tops != null ? tops[index] : index * rowHeight) + index * spacing + (expanded >= 0 && index > expanded ? expandedExtra : 0);

        /// <summary>The height of row <paramref name="index"/>: its height, plus the extra for the expanded row.</summary>
        public float HeightOf(int index) => (tops != null ? tops[index + 1] - tops[index] : rowHeight) + (index == expanded ? expandedExtra : 0);

        /// <summary>The row at <paramref name="y"/> in the content (the spacing below a row belongs to it), clamped to the list; -1 when it is empty.</summary>
        public int IndexAt(float y)
        {
            if (count == 0) return -1;
            // the last row whose top is at or above y
            int lo = 0, hi = count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (RowTop(mid) <= y) lo = mid;
                else hi = mid - 1;
            }
            return lo;
        }

        /// <summary>
        /// Rows needed to draw any offset: those of the smallest height that fit in the viewport, one more for a partial row, and the
        /// overscan on both sides.
        /// </summary>
        public int PoolSize
        {
            get
            {
                if (Empty) return 0;
                float stride = (tops != null ? minHeight : rowHeight) + spacing;
                if (stride <= 0) return count;
                int fit = (int)Math.Ceiling(viewport / stride) + 1;
                return Math.Min(count, fit + 2 * overscan);
            }
        }

        /// <summary>Scrolls to <paramref name="value"/>, clamped. True when the offset changed.</summary>
        public bool SetOffset(float value, ChangeSource source = ChangeSource.Program)
        {
            LastSource = source;
            float before = offset;
            offset = Clamp(value);
            SetAnchorFromOffset();
            Update();
            return offset != before;
        }

        /// <summary>Scrolls row <paramref name="index"/> into view as <paramref name="align"/> says; returns the new offset.</summary>
        public float ScrollTo(int index, ScrollAlign align, ChangeSource source = ChangeSource.Program)
        {
            if (count == 0) return offset;
            index = Math.Max(0, Math.Min(count - 1, index));
            float top = RowTop(index), h = HeightOf(index), target = offset;
            switch (align)
            {
                case ScrollAlign.Start:
                    target = top;
                    break;
                case ScrollAlign.Center:
                    target = top + h / 2 - viewport / 2;
                    break;
                case ScrollAlign.End:
                    target = top + h - viewport;
                    break;
                case ScrollAlign.Nearest:
                    if (top < offset || h > viewport) target = top;
                    else if (top + h > offset + viewport) target = top + h - viewport;
                    break;
            }
            SetOffset(target, source);
            return offset;
        }

        /// <summary>A new row count (rows added or taken off the end, or a new list): the anchor row stays in place if it still exists.</summary>
        public void SetCount(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            if (value == count) return;
            count = value;
            if (expanded >= value) expanded = -1;
            Evaluate();
            Reanchor();
        }

        /// <summary><paramref name="n"/> rows inserted at <paramref name="at"/>: the rows from there move down, the anchor with its row.</summary>
        public void Inserted(int at, int n)
        {
            if (n <= 0) return;
            at = Math.Max(0, Math.Min(count, at));
            // rows inserted into an empty list leave the view at the top
            if (count > 0 && at <= anchor) anchor += n;
            count += n;
            if (expanded >= at) expanded += n;
            Evaluate();
            Reanchor();
        }

        /// <summary><paramref name="n"/> rows removed from <paramref name="at"/>: a removed anchor is replaced by the row that takes its place.</summary>
        public void Removed(int at, int n)
        {
            if (n <= 0 || at >= count) return;
            at = Math.Max(0, at);
            n = Math.Min(n, count - at);
            count -= n;
            if (at + n <= anchor) anchor -= n;
            else if (at <= anchor)
            {
                anchor = at;
                anchorDelta = 0;
            }
            if (expanded >= at + n) expanded -= n;
            else if (expanded >= at) expanded = -1;
            Evaluate();
            Reanchor();
        }

        public void SetRowHeight(float value)
        {
            value = Math.Max(0, value);
            if (value == rowHeight) return;
            rowHeight = value;
            Reanchor();
        }

        /// <summary>
        /// Gives each row its own height, <paramref name="height"/>(i), asked now for every row and again when the count changes or
        /// on <see cref="RefreshHeights"/>; null goes back to <see cref="RowHeight"/> for all.
        /// </summary>
        public void SetRowHeights(Func<int, float> height)
        {
            heightOf = height;
            Evaluate();
            Reanchor();
        }

        /// <summary>The rows show other data at the same count: ask their heights again.</summary>
        public void RefreshHeights()
        {
            if (heightOf == null) return;
            Evaluate();
            Reanchor();
        }

        public void SetSpacing(float value)
        {
            value = Math.Max(0, value);
            if (value == spacing) return;
            spacing = value;
            Reanchor();
        }

        public void SetViewportHeight(float value)
        {
            value = Math.Max(0, value);
            if (value == viewport) return;
            viewport = value;
            Reanchor();
        }

        /// <summary>Makes row <paramref name="index"/> (-1: none) taller by <paramref name="extra"/>.</summary>
        public void SetExpanded(int index, float extra)
        {
            if (index < 0)
            {
                index = -1;
                extra = 0;
            }
            extra = Math.Max(0, extra);
            if (index == expanded && extra == expandedExtra) return;
            expanded = index;
            expandedExtra = extra;
            Reanchor();
        }

        float Clamp(float v) => Math.Max(0, Math.Min(MaxOffset, v));

        // the measured heights' running sums; the array only grows
        void Evaluate()
        {
            if (heightOf == null)
            {
                tops = null;
                return;
            }
            if (tops == null || tops.Length < count + 1) tops = new float[Math.Max(count + 1, tops == null ? 16 : tops.Length * 2)];
            tops[0] = 0;
            minHeight = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                float h = Math.Max(0, heightOf(i));
                tops[i + 1] = tops[i] + h;
                if (h < minHeight) minHeight = h;
            }
            if (count == 0) minHeight = 0;
        }

        void SetAnchorFromOffset()
        {
            // nothing drawn yet (no row, or rows of no height): the view is at the top
            if (Empty)
            {
                anchor = 0;
                anchorDelta = 0;
                return;
            }
            anchor = Math.Max(0, IndexAt(offset));
            anchorDelta = offset - RowTop(anchor);
        }

        // the geometry changed: put the anchor row back where it was in the view, then clamp
        void Reanchor()
        {
            GeometryVersion++;
            if (count == 0)
            {
                anchor = 0;
                anchorDelta = 0;
                offset = 0;
            }
            else
            {
                if (anchor > count - 1)
                {
                    anchor = count - 1;
                    anchorDelta = 0;
                }
                anchorDelta = Math.Min(anchorDelta, HeightOf(anchor) + spacing);
                float wanted = RowTop(anchor) + anchorDelta;
                offset = Clamp(wanted);
                // clamped: the view now starts elsewhere, and that is the new anchor
                if (offset != wanted) SetAnchorFromOffset();
            }
            Update();
        }

        void Update()
        {
            if (Empty)
            {
                FirstVisible = RenderFirst = 0;
                LastVisible = RenderLast = -1;
                return;
            }
            FirstVisible = IndexAt(offset);
            int last = IndexAt(offset + viewport);
            // a row that only starts at the viewport's bottom edge isn't visible
            if (last > FirstVisible && RowTop(last) >= offset + viewport) last--;
            LastVisible = last;
            RenderFirst = Math.Max(0, FirstVisible - overscan);
            RenderLast = Math.Min(count - 1, LastVisible + overscan);
        }
    }
}
