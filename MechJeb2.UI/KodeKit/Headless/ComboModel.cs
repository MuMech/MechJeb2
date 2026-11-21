#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

namespace KodeKit.Headless
{
    /// <summary>
    /// A drop-down you can type into (ui-sdk's combobox), without Unity: the behaviour of KodeKit's <c>ComboBox</c>. The host owns the
    /// selected value (read with <c>get</c>, written with <c>set</c>); the model holds the open list: its rows (the entries that pass the
    /// filter, with group headers), the highlighted row, the filter text and the typeahead.
    /// <list type="bullet">
    /// <item>Items are a label each and optionally a key (a language-independent name, such as the English one); keys of another length
    /// than the labels are ignored. A new items array while the list is open closes it without a pick: a row would pick another entry.</item>
    /// <item><see cref="Open"/> starts with an empty filter and the selected entry highlighted (the value clamped into the entries, as the
    /// drop-down always did); with no entry nothing is highlighted.</item>
    /// <item>With <see cref="Filterable"/>, the filter (<see cref="FuzzyFilter"/>: label and key) ranks the entries best first; an empty
    /// filter lists them in their own order. A new filter highlights the best match, or the selected entry when the filter is cleared.
    /// Nothing matching is <see cref="NoMatch"/>: no row is highlighted and Enter picks nothing. The filter only applies while open.</item>
    /// <item>Group headers (<see cref="Group"/>) are rows of their own, shown only while the filter is empty (ranked results have no
    /// groups); the highlight never rests on a header.</item>
    /// <item>Keys (<see cref="Handle"/>): Down/Up move one row, PageDown/PageUp eight, Home/End jump to the first/last entry (not while
    /// filterable: there they move the filter field's caret), Enter picks the highlighted entry, Escape closes without a pick. Moves clamp,
    /// with no wrap; from no highlight, Down goes to the first entry and Up to the last.</item>
    /// <item>Typed letters (<see cref="Type"/>) are typeahead when not filterable (<see cref="TypeaheadModel"/>: a prefix typed within its
    /// timeout, the first letter searching from the row after the highlight, from the first row when nothing is highlighted); a
    /// letter that finds nothing is dropped with the buffer, so the next one starts afresh (a digit typed by mistake, then a letter);
    /// when filterable, letters are the filter's.</item>
    /// <item>A pick (<see cref="Pick"/>, <see cref="PickRow"/>) closes the list, then calls the setter with its source, unless the entry
    /// is the selected one already. A header row can't be picked.</item>
    /// <item>A keystroke allocates nothing once the row lists have grown to the items' count.</item>
    /// </list>
    /// </summary>
    public sealed class ComboModel : IDescribeParts
    {
        public const int PageStep = 8;

        static readonly string[] None = new string[0];

        readonly Func<int> get;
        readonly Action<int, ChangeSource> set;
        readonly TypeaheadModel typeahead;
        readonly FuzzyFilter filter = new FuzzyFilter(None);
        readonly Func<int, string> labelOfRow, keyOfRow;

        string[] labels = None;
        string[] keys;

        // the rows shown: an item's index, or -1 for a group header (its text in headers at the same row)
        readonly List<int> rows = new List<int>();
        readonly List<string> headers = new List<string>();
        int[] rowOfItem = new int[0];

        readonly Func<string[]> entries;

        /// <param name="entries">The entries as the drop-down shows them now (its button's list), for <see cref="Entries"/>; null: the items last set.</param>
        public ComboModel(Func<int> get, Action<int, ChangeSource> set, double typeaheadTimeout = 1.0, Func<string[]> entries = null)
        {
            this.entries = entries;
            this.get = get ?? throw new ArgumentNullException(nameof(get));
            this.set = set ?? throw new ArgumentNullException(nameof(set));
            typeahead = new TypeaheadModel(typeaheadTimeout);
            labelOfRow = r => rows[r] < 0 ? null : labels[rows[r]];
            keyOfRow = r => rows[r] < 0 || keys == null ? null : keys[rows[r]];
        }

        /// <summary>The list shows a filter field: letters go to the filter, Home and End to its caret. Set while closed.</summary>
        public bool Filterable { get; set; }

        /// <summary>Each item's group (null: none), shown as a header above a run of entries of that group. Set while closed.</summary>
        public Func<int, string> Group { get; set; }

        public bool IsOpen { get; private set; }

        /// <summary>The items of the list (set when it opens: a closed drop-down keeps the last ones).</summary>
        public string[] Items => labels;

        /// <summary>The entries now, also while the list is closed (an agent reads them): the drop-down's own, else <see cref="Items"/>.</summary>
        public string[] Entries => entries?.Invoke() ?? labels;

        public int ItemCount => labels.Length;

        /// <summary>The filter applied (empty when closed or not filterable).</summary>
        public string Filter => filter.Query;

        /// <summary>Rows shown, headers included.</summary>
        public int RowCount => rows.Count;

        /// <summary>Entry rows shown (headers left out).</summary>
        public int EntryCount { get; private set; }

        /// <summary>A filter that leaves no entry, over a list that has some: the "no match" state.</summary>
        public bool NoMatch => EntryCount == 0 && labels.Length > 0 && !IsBlank(filter.Query);

        /// <summary>The highlighted row (-1: none).</summary>
        public int Highlight { get; private set; } = -1;

        /// <summary>The highlighted row's item (-1: none).</summary>
        public int HighlightedItem => Highlight < 0 ? -1 : rows[Highlight];

        /// <summary>Bumped when the rows change (open, a new filter, new items): the host binds its rows again.</summary>
        public int RowsVersion { get; private set; }

        /// <summary>The source of the last pick that reached the setter.</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        /// <summary>The rows changed (see <see cref="RowsVersion"/>).</summary>
        public event Action RowsChanged;

        /// <summary>The highlight moved (<see cref="Highlight"/>).</summary>
        public event Action HighlightChanged;

        /// <summary>The list closed, by a pick, Escape, <see cref="Close"/> or new items.</summary>
        public event Action Closed;

        /// <summary>The row's item, or -1 for a header.</summary>
        public int ItemAt(int row) => rows[row];

        public bool IsHeader(int row) => rows[row] < 0;

        /// <summary>The header text of a header row (null for an entry row).</summary>
        public string HeaderAt(int row) => headers[row];

        /// <summary>The row an item is shown on (-1: filtered out, or closed).</summary>
        public int RowOf(int item) => item >= 0 && item < labels.Length && item < rowOfItem.Length ? rowOfItem[item] : -1;

        /// <summary>The label of an item.</summary>
        public string ItemLabel(int item) => labels[item];

        /// <summary>What the drop-down is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        static readonly PartKey[] ListKeys = { PartKey.Up, PartKey.Down, PartKey.PageUp, PartKey.PageDown, PartKey.Home, PartKey.End, PartKey.Enter, PartKey.Escape };
        static readonly PartKey[] FilterKeys = { PartKey.Up, PartKey.Down, PartKey.PageUp, PartKey.PageDown, PartKey.Enter, PartKey.Escape };

        /// <summary>
        /// The button (a combo box, expanded while open); while open, the filter field when filterable, the list (it has the
        /// keyboard), each group header and each entry row (an option: on when it is the selected entry, focused when highlighted).
        /// </summary>
        public void Describe(List<PartDescription> into)
        {
            bool named = !string.IsNullOrEmpty(Label);
            into.Add(new PartDescription { Name = "button", Role = PartRole.ComboBox, Label = Label, Flags = IsOpen ? PartFlags.Expanded : PartFlags.None });
            if (!IsOpen) return;
            if (Filterable)
                into.Add(new PartDescription
                {
                    Name = "filter", Role = PartRole.TextBox, Label = named ? "filter " + Label : null, Focusable = true, Keys = FilterKeys,
                    Flags = NoMatch ? PartFlags.Invalid : PartFlags.None
                });
            into.Add(new PartDescription { Name = "list", Role = PartRole.ListBox, Label = Label, Focusable = !Filterable, Keys = Filterable ? FilterKeys : ListKeys });
            int selected = get();
            for (int r = 0; r < rows.Count; r++)
            {
                int item = rows[r];
                if (item < 0)
                {
                    into.Add(new PartDescription { Name = "header." + r, Role = PartRole.Group, Label = headers[r] });
                    continue;
                }
                into.Add(new PartDescription
                {
                    Name = "option." + item, Role = PartRole.Option, Label = labels[item],
                    Flags = (item == selected ? PartFlags.On : PartFlags.None) | (r == Highlight ? PartFlags.Focus : PartFlags.None)
                });
            }
        }

        /// <summary>New items: the same array (and keys) does nothing; another closes an open list without a pick.</summary>
        public void SetItems(string[] labels, string[] keys = null)
        {
            if (labels == null) labels = None;
            if (keys != null && keys.Length != labels.Length) keys = null;
            if (ReferenceEquals(labels, this.labels) && ReferenceEquals(keys, this.keys)) return;
            Close();
            this.labels = labels;
            this.keys = keys;
            filter.SetItems(labels, keys);
            if (rowOfItem.Length < labels.Length) rowOfItem = new int[labels.Length];
            BuildRows();
        }

        /// <summary>Opens the list: an empty filter, the typeahead forgotten, the selected entry highlighted.</summary>
        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            typeahead.Reset();
            filter.Apply("");
            BuildRows();
            SetHighlight(SelectedRow());
        }

        /// <summary>Closes the list without a pick (the filter is cleared).</summary>
        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            filter.Apply("");
            Highlight = -1;
            BuildRows();
            Closed?.Invoke();
        }

        /// <summary>The filter text (a keystroke in the filter field). False when closed, not filterable, or nothing changed.</summary>
        public bool SetFilter(string text)
        {
            if (!IsOpen || !Filterable) return false;
            if (!filter.Apply(text ?? "")) return false;
            BuildRows();
            SetHighlight(IsBlank(filter.Query) ? SelectedRow() : FirstEntry());
            return true;
        }

        /// <summary>A key on the open list. True when the key did something to the list (a move, a pick, the close).</summary>
        public bool Handle(ScopeKeys key, ChangeSource source = ChangeSource.Keyboard)
        {
            if (!IsOpen) return false;
            switch (key)
            {
                case ScopeKeys.Down: return Move(1);
                case ScopeKeys.Up: return Move(-1);
                case ScopeKeys.PageDown: return Move(PageStep);
                case ScopeKeys.PageUp: return Move(-PageStep);
                case ScopeKeys.Home: return !Filterable && SetHighlight(FirstEntry());
                case ScopeKeys.End: return !Filterable && SetHighlight(LastEntry());
                case ScopeKeys.Enter: return Pick(source);
                case ScopeKeys.Escape:
                    Close();
                    return true;
                default: return false;
            }
        }

        /// <summary>Moves the highlight by <paramref name="by"/> rows, clamped, past headers.</summary>
        public bool Move(int by)
        {
            if (!IsOpen || EntryCount == 0 || by == 0) return false;
            int r;
            if (Highlight < 0) r = by > 0 ? FirstEntry() : LastEntry();
            else
            {
                int clamped = Math.Max(0, Math.Min(rows.Count - 1, Highlight + by));
                int step = by > 0 ? 1 : -1;
                // past headers in the move's direction; at the list's end, back to the nearest entry
                r = clamped;
                while (r >= 0 && r < rows.Count && rows[r] < 0) r += step;
                if (r < 0 || r >= rows.Count)
                {
                    r = clamped;
                    while (r >= 0 && r < rows.Count && rows[r] < 0) r -= step;
                }
                if (r < 0 || r >= rows.Count) r = Highlight;
            }
            return SetHighlight(r);
        }

        /// <summary>Highlights a row (a pointer over it, an agent): a header or a row out of range does nothing.</summary>
        public bool HighlightRow(int row)
        {
            if (!IsOpen || row < 0 || row >= rows.Count || rows[row] < 0) return false;
            return SetHighlight(row);
        }

        /// <summary>A typed character at time <paramref name="now"/> (seconds): typeahead when not filterable. The row moved to, or -1.</summary>
        public int Type(char c, double now)
        {
            if (!IsOpen || Filterable) return -1;
            int r = typeahead.Type(c, now, rows.Count, labelOfRow, Highlight, keys != null ? keyOfRow : null);
            if (r < 0 || rows[r] < 0)
            {
                // a letter that finds nothing isn't kept: the next one starts afresh
                typeahead.Reset();
                return -1;
            }
            SetHighlight(r);
            return r;
        }

        /// <summary>Picks the highlighted entry: closes, then sets it unless it is selected already. False when nothing is highlighted.</summary>
        public bool Pick(ChangeSource source)
        {
            if (!IsOpen || Highlight < 0) return false;
            int item = rows[Highlight];
            Close();
            if (item == get()) return true;
            LastSource = source;
            set(item, source);
            return true;
        }

        /// <summary>Picks the entry on a row (a click). False for a header or a row out of range.</summary>
        public bool PickRow(int row, ChangeSource source)
        {
            if (!IsOpen || row < 0 || row >= rows.Count || rows[row] < 0) return false;
            Highlight = row;
            return Pick(source);
        }

        // ---- rows ----

        static bool IsBlank(string s)
        {
            for (int i = 0; i < s.Length; i++)
                if (!char.IsWhiteSpace(s[i])) return false;
            return true;
        }

        void BuildRows()
        {
            rows.Clear();
            headers.Clear();
            for (int i = 0; i < labels.Length; i++) rowOfItem[i] = -1;
            EntryCount = 0;
            if (IsOpen)
            {
                bool grouped = Group != null && IsBlank(filter.Query);
                string last = null;
                List<FuzzyResult> results = filter.Results;
                for (int k = 0; k < results.Count; k++)
                {
                    int item = results[k].Index;
                    if (grouped)
                    {
                        string g = Group(item);
                        if (g != null && g != last)
                        {
                            rows.Add(-1);
                            headers.Add(g);
                        }
                        last = g;
                    }
                    rowOfItem[item] = rows.Count;
                    rows.Add(item);
                    headers.Add(null);
                    EntryCount++;
                }
            }
            RowsVersion++;
            RowsChanged?.Invoke();
        }

        int SelectedRow()
        {
            if (labels.Length == 0) return -1;
            int item = Math.Max(0, Math.Min(labels.Length - 1, get()));
            int r = RowOf(item);
            return r >= 0 ? r : FirstEntry();
        }

        int FirstEntry()
        {
            for (int r = 0; r < rows.Count; r++)
                if (rows[r] >= 0) return r;
            return -1;
        }

        int LastEntry()
        {
            for (int r = rows.Count - 1; r >= 0; r--)
                if (rows[r] >= 0) return r;
            return -1;
        }

        bool SetHighlight(int row)
        {
            if (row == Highlight) return false;
            Highlight = row;
            HighlightChanged?.Invoke();
            return true;
        }
    }
}
