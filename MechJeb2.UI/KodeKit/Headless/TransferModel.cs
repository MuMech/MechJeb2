#if !CSHARP_7_3
#nullable disable
#endif
using System;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>Whether a transfer leaves the row in its source list (a copy) or takes it out (a move).</summary>
    public enum TransferMode { Copy, Move }

    /// <summary>A transfer a drop made (or would make): source row <see cref="SourceIndex"/> into the target at <see cref="At"/>.</summary>
    public struct ListTransfer
    {
        public int SourceIndex, At;
        public TransferMode Mode;

        /// <summary>True when the lists changed (the insert ran).</summary>
        public bool Done;

        public ListTransfer(int sourceIndex, int at, TransferMode mode, bool done)
        {
            SourceIndex = sourceIndex;
            At = at;
            Mode = mode;
            Done = done;
        }

        public override string ToString() => Done ? $"{Mode} {SourceIndex} -> {At}" : "no transfer";
    }

    /// <summary>
    /// Drag a row from one list into another, without Unity (ui-sdk's <c>createTransfer</c>): KodeKit's <c>ListDrag</c> draws it
    /// with a drop indicator in the target list. The lists are the host's: <c>insert(sourceIndex, at)</c> puts source row
    /// <c>sourceIndex</c> (or a copy of it) into the target at index <c>at</c>, and <c>remove(sourceIndex)</c> takes it out of the source.
    /// <list type="bullet">
    /// <item>A drag starts on a source row (<see cref="Begin"/>) only when <see cref="Allow"/> accepts it; it follows the pointer over the
    /// target's gaps (<see cref="Over"/>, <see cref="OverAt"/>, 0 to the target's count) and ends with <see cref="Drop"/> or
    /// <see cref="Cancel"/>. Nothing changes before the drop.</item>
    /// <item>Outside the target (<see cref="Outside"/>, over the source list included) a release transfers nothing.</item>
    /// <item><see cref="TransferMode.Copy"/> inserts and leaves the source as it is; <see cref="TransferMode.Move"/> inserts, then
    /// removes the row from the source, in that order (the insert reads the row while it is still there). The two lists are
    /// different ones: within one list the removal would shift the insert's index, and a move there is a <see cref="ReorderModel"/>.</item>
    /// <item>Every gap of the target is a real transfer: an insert at its end is gap = count.</item>
    /// <item><see cref="Transfer"/> is the same without a drag (a click on the row, an agent), with the same rules; its
    /// <c>at</c> is clamped to the target.</item>
    /// </list>
    /// </summary>
    [NotAWidget("a drag gesture between two lists (ListDrag): the lists and their rows have no part description yet (roadmap item 32)")]
    public sealed class TransferModel
    {
        readonly Func<int> sourceCount, targetCount;
        readonly Action<int, int> insert;
        readonly Action<int> remove;

        /// <summary>When set, false for a source row that can't be transferred.</summary>
        public Func<int, bool> Allow;

        public TransferModel(Func<int> sourceCount, Func<int> targetCount, Action<int, int> insert, TransferMode mode = TransferMode.Copy,
            Action<int> remove = null)
        {
            this.sourceCount = sourceCount ?? throw new ArgumentNullException(nameof(sourceCount));
            this.targetCount = targetCount ?? throw new ArgumentNullException(nameof(targetCount));
            this.insert = insert ?? throw new ArgumentNullException(nameof(insert));
            if (mode == TransferMode.Move && remove == null) throw new ArgumentNullException(nameof(remove), "a move takes the row out of its source");
            this.remove = remove;
            Mode = mode;
        }

        public TransferMode Mode { get; }

        public bool Dragging { get; private set; }

        /// <summary>The dragged source row (-1: no drag).</summary>
        public int SourceIndex { get; private set; } = -1;

        /// <summary>The target's gap under the pointer (-1: outside the target, or no drag).</summary>
        public int Gap { get; private set; } = -1;

        public ChangeSource Source { get; private set; } = ChangeSource.Program;

        /// <summary>True while a drop would transfer: the host draws the indicator at <see cref="Gap"/> in the target.</summary>
        public bool ShowsIndicator => Dragging && Gap >= 0;

        bool Allowed(int i) => i >= 0 && i < sourceCount() && (Allow == null || Allow(i));

        /// <summary>Starts a drag on source row <paramref name="sourceIndex"/>; false (no drag) when it can't be transferred.</summary>
        public bool Begin(int sourceIndex, ChangeSource source = ChangeSource.Pointer)
        {
            Cancel();
            if (!Allowed(sourceIndex)) return false;
            Dragging = true;
            SourceIndex = sourceIndex;
            Source = source;
            return true;
        }

        /// <summary>The pointer is over the target's gap <paramref name="gap"/> (clamped to the target); negative: <see cref="Outside"/>.</summary>
        public void Over(int gap)
        {
            if (!Dragging) return;
            Gap = gap < 0 ? -1 : Math.Min(gap, targetCount());
        }

        /// <summary>The pointer is at <paramref name="y"/> in the target's content (list units, down from its top).</summary>
        public void OverAt(VirtualListModel target, float y) => Over(ListDrop.GapAt(target, y));

        /// <summary>The pointer is not over the target: a release now transfers nothing.</summary>
        public void Outside() => Over(-1);

        /// <summary>Ends the drag: transfers the row when the pointer is over the target. Returns what happened.</summary>
        public ListTransfer Drop()
        {
            if (!Dragging) return new ListTransfer(-1, -1, Mode, false);
            int from = SourceIndex, at = Gap;
            ChangeSource source = Source;
            Cancel();
            if (at < 0) return new ListTransfer(from, -1, Mode, false);
            return Transfer(from, at, source);
        }

        /// <summary>Ends the drag without a transfer.</summary>
        public void Cancel()
        {
            Dragging = false;
            SourceIndex = -1;
            Gap = -1;
        }

        /// <summary>Transfers source row <paramref name="sourceIndex"/> into the target at <paramref name="at"/> (clamped; past the end appends).</summary>
        public ListTransfer Transfer(int sourceIndex, int at, ChangeSource source)
        {
            if (!Allowed(sourceIndex)) return new ListTransfer(sourceIndex, -1, Mode, false);
            at = Math.Max(0, Math.Min(targetCount(), at));
            Source = source;
            insert(sourceIndex, at);
            if (Mode == TransferMode.Move) remove(sourceIndex);
            return new ListTransfer(sourceIndex, at, Mode, true);
        }
    }
}
