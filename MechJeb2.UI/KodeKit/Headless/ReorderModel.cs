#if !CSHARP_7_3
#nullable disable
#endif
using System;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>
    /// Where a dragged row would land in a list drawn from a <see cref="VirtualListModel"/>: a gap, numbered 0 (above the first row)
    /// to <c>Count</c> (below the last). Positions are in list units from the content's top, down.
    /// </summary>
    public static class ListDrop
    {
        /// <summary>
        /// The gap nearest <paramref name="y"/>: above the row under it when <paramref name="y"/> is in that row's upper half,
        /// below it in its lower half (the spacing under a row counts as its lower half); 0 above the list, Count below it.
        /// </summary>
        public static int GapAt(VirtualListModel list, float y)
        {
            if (list == null || list.Count == 0 || y <= 0) return 0;
            if (y >= list.ContentHeight) return list.Count;
            int i = list.IndexAt(y);
            return y < list.RowTop(i) + list.HeightOf(i) / 2 ? i : i + 1;
        }

        /// <summary>Where a drop indicator for <paramref name="gap"/> goes: the middle of the spacing between the two rows, 0 or the content's height at the ends.</summary>
        public static float IndicatorY(VirtualListModel list, int gap)
        {
            if (list == null || list.Count == 0 || gap <= 0) return 0;
            if (gap >= list.Count) return list.ContentHeight;
            return list.RowTop(gap) - list.Spacing / 2;
        }
    }

    /// <summary>A move a reorder made (or would make): the row from <see cref="From"/> to the index <see cref="To"/>.</summary>
    public struct ListMove
    {
        public int From, To;

        /// <summary>True when the list changed (the setter ran).</summary>
        public bool Moved;

        public ListMove(int from, int to, bool moved)
        {
            From = from;
            To = to;
            Moved = moved;
        }

        public override string ToString() => Moved ? $"{From} -> {To}" : "no move";
    }

    /// <summary>
    /// Drag to reorder a list, without Unity (ui-sdk's <c>createReorder</c>): KodeKit's <c>ListDrag</c> draws it on a
    /// <c>VirtualList</c> (a drop indicator in the gap, the rows unchanged until the drop). The list is the host's;
    /// <c>move(from, to)</c> takes row <c>from</c> out and puts it back at index <c>to</c>.
    /// <list type="bullet">
    /// <item>A drag starts on a row (<see cref="Begin"/>), follows the pointer over gaps (<see cref="Over"/>, <see cref="OverAt"/>) and
    /// ends with <see cref="Drop"/> or <see cref="Cancel"/>. Only a drop moves anything: the list is unchanged during the drag.</item>
    /// <item>Gap <c>g</c> puts the row at <c>g</c>, or <c>g - 1</c> when the gap is below the row's own place. The two gaps around the row
    /// (<c>From</c> and <c>From + 1</c>) are no move: <see cref="Drop"/> calls nothing and <see cref="ShowsIndicator"/> is false.</item>
    /// <item>The pointer outside the list (<see cref="Outside"/>, gap -1) drops nothing: releasing there cancels.</item>
    /// <item>A fixed row (<see cref="IsFixed"/>) can't be dragged, and keeps its index: a move never crosses it. The gap is clamped
    /// between the fixed rows around the dragged one, so the indicator stops at the last gap a drop can use.</item>
    /// <item><see cref="Move"/> and <see cref="MoveBy"/> are the same move without a drag (buttons, keys, an agent), with the same rules.</item>
    /// <item>The count is read from the host at each call: a list that changes during a drag (a row removed) clamps the gap, and a
    /// dragged row that no longer exists cancels the drag.</item>
    /// </list>
    /// </summary>
    [NotAWidget("a drag gesture on a list (ListDrag): the list and its rows have no part description yet (roadmap item 32)")]
    public sealed class ReorderModel
    {
        readonly Func<int> count;
        readonly Action<int, int> move;

        /// <summary>When set, true for a row that can't move (a pinned header).</summary>
        public Func<int, bool> IsFixed;

        public ReorderModel(Func<int> count, Action<int, int> move)
        {
            this.count = count ?? throw new ArgumentNullException(nameof(count));
            this.move = move ?? throw new ArgumentNullException(nameof(move));
        }

        public int Count => count();

        public bool Dragging { get; private set; }

        /// <summary>The dragged row (-1: no drag).</summary>
        public int From { get; private set; } = -1;

        /// <summary>The gap under the pointer, clamped to the allowed ones (-1: outside the list, or no drag).</summary>
        public int Gap { get; private set; } = -1;

        /// <summary>The source of the drag, or of the last move made.</summary>
        public ChangeSource Source { get; private set; } = ChangeSource.Program;

        /// <summary>The index the dragged row would get if dropped now (-1: none).</summary>
        public int To => Dragging && Gap >= 0 ? (Gap > From ? Gap - 1 : Gap) : -1;

        /// <summary>True while a drop would move the row: the host draws the indicator at <see cref="Gap"/>.</summary>
        public bool ShowsIndicator => Dragging && Gap >= 0 && To != From;

        /// <summary>The first and last gap the dragged row can use (between the fixed rows around it).</summary>
        public int MinGap { get; private set; }

        public int MaxGap { get; private set; }

        bool Fixed(int i) => IsFixed != null && IsFixed(i);

        /// <summary>Starts a drag on row <paramref name="index"/>; false (no drag) for a row outside the list or a fixed one.</summary>
        public bool Begin(int index, ChangeSource source = ChangeSource.Pointer)
        {
            Cancel();
            int n = count();
            if (index < 0 || index >= n || Fixed(index)) return false;
            Dragging = true;
            From = index;
            Source = source;
            Bounds(index, n, out int lo, out int hi);
            MinGap = lo;
            MaxGap = hi;
            Gap = index;
            return true;
        }

        /// <summary>The pointer is over gap <paramref name="gap"/> (clamped to the allowed gaps); a negative gap is <see cref="Outside"/>.</summary>
        public void Over(int gap)
        {
            if (!Dragging) return;
            if (gap < 0)
            {
                Gap = -1;
                return;
            }
            int n = count();
            if (From >= n)
            {
                Cancel();
                return;
            }
            Bounds(From, n, out int lo, out int hi);
            MinGap = lo;
            MaxGap = hi;
            Gap = Math.Max(lo, Math.Min(hi, gap));
        }

        /// <summary>The pointer is at <paramref name="y"/> in the list's content (list units, down from its top).</summary>
        public void OverAt(VirtualListModel list, float y) => Over(ListDrop.GapAt(list, y));

        /// <summary>The pointer left the list: a release now drops nothing.</summary>
        public void Outside() => Over(-1);

        /// <summary>Ends the drag: moves the row when the gap is a real move. Returns what happened.</summary>
        public ListMove Drop()
        {
            if (!Dragging) return new ListMove(-1, -1, false);
            int from = From, to = To;
            Cancel();
            if (to < 0 || to == from || from >= count()) return new ListMove(from, to < 0 ? from : to, false);
            move(from, to);
            return new ListMove(from, to, true);
        }

        /// <summary>Ends the drag without a move.</summary>
        public void Cancel()
        {
            Dragging = false;
            From = -1;
            Gap = -1;
        }

        /// <summary>Moves row <paramref name="from"/> to index <paramref name="to"/> (clamped to what the fixed rows allow); false when nothing moved.</summary>
        public bool Move(int from, int to, ChangeSource source)
        {
            int n = count();
            if (from < 0 || from >= n || Fixed(from)) return false;
            Bounds(from, n, out int lo, out int hi);
            // the indices the row can take: the gaps lo..hi, less the one it leaves
            to = Math.Max(lo, Math.Min(hi - 1, to));
            if (to == from) return false;
            Source = source;
            move(from, to);
            return true;
        }

        /// <summary>Moves row <paramref name="index"/> by <paramref name="delta"/> places (-1: up one, the Move up button); false when nothing moved.</summary>
        public bool MoveBy(int index, int delta, ChangeSource source) => Move(index, index + delta, source);

        // the gaps row i can use: from just below the fixed row above it to just above the fixed row below it
        void Bounds(int i, int n, out int lo, out int hi)
        {
            lo = 0;
            hi = n;
            if (IsFixed == null) return;
            for (int k = i - 1; k >= 0; k--)
                if (IsFixed(k))
                {
                    lo = k + 1;
                    break;
                }
            for (int k = i + 1; k < n; k++)
                if (IsFixed(k))
                {
                    hi = k;
                    break;
                }
        }
    }
}
