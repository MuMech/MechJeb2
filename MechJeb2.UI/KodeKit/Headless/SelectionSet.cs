#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>
    /// Several rows of a list selected at once, without Unity (ui-sdk's <c>createSelection</c>, redesigned with no modifier key):
    /// KodeKit draws it as a check box per row (a toggle column) and an "all" check box, and the host's buttons act on the
    /// selected rows (the custom window editor's Remove and Move). The list is the host's: <c>count</c> rows, row <c>i</c>'s key
    /// <c>keyAt(i)</c>.
    /// <list type="bullet">
    /// <item>The selection is a set of keys, not of indexes: a row keeps its state when rows above it come, go or move, and when
    /// the host draws the list again (a rebind, a scroll of a virtual list, a rebuilt window).</item>
    /// <item>A click toggles one row (<see cref="Toggle"/>, <see cref="Set"/>); no key or modifier is read. <see cref="SelectAll"/>
    /// selects the rows shown, <see cref="Clear"/> empties the selection.</item>
    /// <item>A filter (<see cref="ShowOnly"/>) hides rows without unselecting them; <see cref="SelectAll"/> and
    /// <see cref="SelectRange"/> take only shown rows, and the actions (<see cref="SelectedIndexes"/>, <see cref="MoveSelected"/>,
    /// <see cref="RemoveSelected"/>) act only on shown rows, so a hidden row is never removed or moved unseen.</item>
    /// <item>A range (<see cref="SelectRange"/>) follows the list's order, whichever end is given first.</item>
    /// <item><see cref="Prune"/> drops the keys of rows that are gone from the list (not those a filter hides); the host calls it
    /// when its rows change.</item>
    /// <item><see cref="MoveSelected"/> moves the selected rows one place up or down as blocks: a block against the list's end, or
    /// against a selected row that can't move, stays. <see cref="RemoveSelected"/> removes from the last row up. Each calls the
    /// host's edit once per row: the host wraps the call in one commit.</item>
    /// <item>Every change bumps <see cref="Version"/>, records its source and fires <see cref="Changed"/>; a call that changes
    /// nothing fires nothing.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: the rows a list's check boxes select; each check box's ToggleModel describes it")]
    public sealed class SelectionSet<TKey>
    {
        readonly Func<int> count;
        readonly Func<int, TKey> keyAt;
        readonly List<TKey> keys = new List<TKey>();
        readonly IEqualityComparer<TKey> comparer;
        Func<int, bool> shown;

        public SelectionSet(Func<int> count, Func<int, TKey> keyAt, IEqualityComparer<TKey> comparer = null)
        {
            this.count = count ?? throw new ArgumentNullException(nameof(count));
            this.keyAt = keyAt ?? throw new ArgumentNullException(nameof(keyAt));
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
        }

        /// <summary>
        /// Which rows the host shows (a filter); null: all of them. Hidden rows keep their state and are left out of
        /// <see cref="SelectAll"/>, <see cref="SelectRange"/> and the actions.
        /// </summary>
        public SelectionSet<TKey> ShowOnly(Func<int, bool> isShown)
        {
            shown = isShown;
            return this;
        }

        /// <summary>Bumped at every change.</summary>
        public int Version { get; private set; }

        /// <summary>The source of the last change.</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>The selection changed.</summary>
        public event Action Changed;

        /// <summary>How many keys are selected, shown or not (keys of rows that are gone count until <see cref="Prune"/>).</summary>
        public int KeyCount => keys.Count;

        /// <summary>The selected keys, in the order they were selected.</summary>
        public IReadOnlyList<TKey> Keys => keys;

        int Rows => Math.Max(0, count());

        bool IsShown(int row) => shown == null || shown(row);

        bool Has(TKey key)
        {
            for (int i = 0; i < keys.Count; i++)
                if (comparer.Equals(keys[i], key)) return true;
            return false;
        }

        bool Drop(TKey key)
        {
            for (int i = 0; i < keys.Count; i++)
                if (comparer.Equals(keys[i], key))
                {
                    keys.RemoveAt(i);
                    return true;
                }
            return false;
        }

        /// <summary>Row <paramref name="row"/> is selected (false outside the list).</summary>
        public bool IsSelected(int row) => keys.Count > 0 && row >= 0 && row < Rows && Has(keyAt(row));

        /// <summary>The key is selected.</summary>
        public bool Contains(TKey key) => Has(key);

        /// <summary>How many shown rows are selected.</summary>
        public int Count
        {
            get
            {
                if (keys.Count == 0) return 0;
                int n = 0, rows = Rows;
                for (int r = 0; r < rows; r++)
                    if (IsShown(r) && Has(keyAt(r))) n++;
                return n;
            }
        }

        /// <summary>How many selected rows a filter hides.</summary>
        public int HiddenCount
        {
            get
            {
                if (shown == null || keys.Count == 0) return 0;
                int n = 0, rows = Rows;
                for (int r = 0; r < rows; r++)
                    if (!shown(r) && Has(keyAt(r))) n++;
                return n;
            }
        }

        /// <summary>Every shown row is selected, and there is at least one (the "all" check box's state).</summary>
        public bool AllSelected
        {
            get
            {
                int rows = Rows, seen = 0;
                for (int r = 0; r < rows; r++)
                {
                    if (!IsShown(r)) continue;
                    if (!Has(keyAt(r))) return false;
                    seen++;
                }
                return seen > 0;
            }
        }

        /// <summary>Selects or unselects row <paramref name="row"/>. False when nothing changed.</summary>
        public bool Set(int row, bool on, ChangeSource source)
        {
            if (row < 0 || row >= Rows) return false;
            TKey k = keyAt(row);
            bool changed = on ? !Has(k) : Drop(k);
            if (!changed) return false;
            if (on) keys.Add(k);
            Done(source);
            return true;
        }

        /// <summary>Turns row <paramref name="row"/> to the other state. False outside the list.</summary>
        public bool Toggle(int row, ChangeSource source) => row >= 0 && row < Rows && Set(row, !IsSelected(row), source);

        /// <summary>Selects the shown rows <paramref name="from"/> to <paramref name="to"/>, both included, in the list's order.</summary>
        public bool SelectRange(int from, int to, ChangeSource source)
        {
            int rows = Rows;
            int a = Math.Max(0, Math.Min(from, to)), b = Math.Min(rows - 1, Math.Max(from, to));
            bool changed = false;
            for (int r = a; r <= b; r++)
            {
                if (!IsShown(r)) continue;
                TKey k = keyAt(r);
                if (Has(k)) continue;
                keys.Add(k);
                changed = true;
            }
            if (changed) Done(source);
            return changed;
        }

        /// <summary>Selects every shown row.</summary>
        public bool SelectAll(ChangeSource source) => SelectRange(0, Rows - 1, source);

        /// <summary>Unselects every row, hidden ones too.</summary>
        public bool Clear(ChangeSource source)
        {
            if (keys.Count == 0) return false;
            keys.Clear();
            Done(source);
            return true;
        }

        /// <summary>The "all" check box: <see cref="SelectAll"/> when on, <see cref="Clear"/> when off.</summary>
        public bool SetAll(bool on, ChangeSource source) => on ? SelectAll(source) : Clear(source);

        /// <summary>The shown selected rows' indexes into <paramref name="into"/> (cleared first), in the list's order.</summary>
        public void SelectedIndexes(List<int> into)
        {
            into.Clear();
            int rows = Rows;
            for (int r = 0; r < rows; r++)
                if (IsShown(r) && Has(keyAt(r))) into.Add(r);
        }

        /// <summary>Drops the keys of rows that are no longer in the list. True when it dropped one.</summary>
        public bool Prune()
        {
            if (keys.Count == 0) return false;
            int rows = Rows;
            bool changed = false;
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int r = 0; r < rows && !found; r++) found = comparer.Equals(keyAt(r), keys[i]);
                if (found) continue;
                keys.RemoveAt(i);
                changed = true;
            }
            if (changed) Done(LastSource);
            return changed;
        }

        /// <summary>
        /// Moves the shown selected rows one place up (<paramref name="direction"/> &lt; 0) or down (&gt; 0) as blocks, through
        /// <paramref name="move"/>(from, to), which must move the host's row (keys follow their rows). A block at the list's end, or
        /// behind a selected row that stays, stays. Returns the number of rows moved.
        /// </summary>
        public int MoveSelected(int direction, Action<int, int> move)
        {
            if (direction == 0 || move == null) return 0;
            int rows = Rows, moved = 0;
            if (direction < 0)
            {
                for (int r = 1; r < rows; r++)
                    if (Movable(r) && !Has(keyAt(r - 1)))
                    {
                        move(r, r - 1);
                        moved++;
                    }
            }
            else
            {
                for (int r = rows - 2; r >= 0; r--)
                    if (Movable(r) && !Has(keyAt(r + 1)))
                    {
                        move(r, r + 1);
                        moved++;
                    }
            }
            return moved;
        }

        bool Movable(int row) => IsShown(row) && Has(keyAt(row));

        /// <summary>
        /// Removes the shown selected rows through <paramref name="removeAt"/>(index), from the last up so the indexes stay right,
        /// each row's key dropped as it goes (a key that names a row by its place among equal rows then never passes to another
        /// row). Returns the number removed.
        /// </summary>
        public int RemoveSelected(Action<int> removeAt, ChangeSource source)
        {
            if (removeAt == null) return 0;
            int removed = 0;
            for (int r = Rows - 1; r >= 0; r--)
            {
                if (!Movable(r)) continue;
                TKey k = keyAt(r);
                removeAt(r);
                Drop(k);
                removed++;
            }
            if (removed > 0) Done(source);
            return removed;
        }

        void Done(ChangeSource source)
        {
            LastSource = source;
            Version++;
            Changed?.Invoke();
        }
    }
}
