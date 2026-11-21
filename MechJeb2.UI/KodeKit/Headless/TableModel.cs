#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;
using System.Text;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>How a table column's width is decided.</summary>
    public enum TableWidthRule
    {
        /// <summary>A fixed width (<see cref="TableColumn.Width"/>, in layout units), whatever the texts.</summary>
        Fixed,

        /// <summary>
        /// As wide as its header and its widest cell so far (<see cref="TableColumn.ContentChars"/>), never narrower than
        /// <see cref="TableColumn.Width"/>: it only grows, so changing values never lay the table out again.
        /// </summary>
        Content
    }

    /// <summary>What a <see cref="TableModel.Sync"/> changed, for the host that draws it.</summary>
    [Flags]
    public enum TableChange
    {
        None = 0,

        /// <summary>The cells that exist: the row count, a column shown or hidden, a header's text (<see cref="TableModel.ShapeVersion"/>).</summary>
        Shape = 1,

        /// <summary>The cells' texts were formatted again.</summary>
        Values = 2,

        /// <summary>A content column's <see cref="TableColumn.ContentChars"/> changed: the host measures it again.</summary>
        Widths = 4,

        /// <summary>The highlighted row (<see cref="TableModel.Highlighted"/>).</summary>
        Highlight = 8,

        /// <summary>The selection (<see cref="TableModel.Selection"/>).</summary>
        Selection = 16
    }

    /// <summary>
    /// Formats the cell of row <paramref name="row"/> in the column whose key is <paramref name="key"/>: the value into
    /// <paramref name="value"/> (empty, reused), and its unit, which starts as the column's <see cref="TableColumn.Unit"/>, into
    /// <paramref name="unit"/> (a constant string: nothing is allocated per cell).
    /// </summary>
    public delegate void TableCellFormat(int row, int key, CharBuffer value, ref string unit);

    /// <summary>One column of a <see cref="TableModel"/>: its key, header, unit, visibility and width rule.</summary>
    [NotAWidget("data: a column of a TableModel, which describes it")]
    public sealed class TableColumn
    {
        internal readonly List<CharBuffer> Cells = new List<CharBuffer>();
        internal readonly List<int> ValueLengths = new List<int>();

        internal TableColumn(int index, int key, string header, TableWidthRule rule, float width, string unit)
        {
            Index = index;
            Key = key;
            Header = header ?? "";
            WidthRule = rule;
            Width = width;
            Unit = unit;
        }

        /// <summary>The column's place in <see cref="TableModel.Columns"/>.</summary>
        public int Index { get; }

        /// <summary>The owner's id of the column (an enum value), passed to the formatter and the owner's callbacks.</summary>
        public int Key { get; }

        /// <summary>A name for the column's objects (null: its key), and its description's label when its header is empty.</summary>
        public string Name { get; set; }

        /// <summary>The header's text as shown (pulled from the owner at each sync when the table has a header source).</summary>
        public string Header { get; internal set; }

        /// <summary>What the column means, for a tooltip (null: none).</summary>
        public string Tooltip { get; set; }

        /// <summary>The unit its cells start with (the formatter can change it per cell); null or empty: none.</summary>
        public string Unit { get; set; }

        public TableWidthRule WidthRule { get; }

        /// <summary>A fixed column's width; a content column's least width (layout units).</summary>
        public float Width { get; }

        /// <summary>The widest cell so far, in characters (value and unit, markup left out); 0 after its header changed.</summary>
        public int ContentChars { get; internal set; }

        /// <summary>Shown (pulled from the owner at each sync when the table has a visibility source).</summary>
        public bool Visible { get; internal set; } = true;

        /// <summary>Whether the copies (<see cref="TableModel.CopyRows"/>, <see cref="TableModel.CopyMarkdown"/>) write it (false: a column of buttons).</summary>
        public bool Copied { get; set; } = true;

        /// <summary>Whether <see cref="TableModel.Hide"/> may hide it (a table's key column stays).</summary>
        public bool Hideable { get; set; } = true;
    }

    /// <summary>
    /// A table of rows × columns without Unity: the behaviour of MechJeb's Delta-V Stats grid and PSG Stages table, which KodeKit
    /// draws. The owner gives the row count, the cell formatter, and optionally a source version, the highlighted row, the columns'
    /// visibility and headers, the rows' keys and what hiding a column does; <see cref="Sync"/> pulls them (once a refresh) and
    /// formats the cells into reused buffers.
    /// <list type="bullet">
    /// <item>Columns are added once (<see cref="AddColumn"/>), in display order; a column's key is the owner's id for it.</item>
    /// <item>The shape (row count, visible columns, header texts) has a version, <see cref="ShapeVersion"/>, bumped when it
    /// changes: the host makes or shows cells then, and draws the values otherwise.</item>
    /// <item>Only visible columns are formatted. Without a source version the cells are formatted at every sync (live values);
    /// with one, only when it or the shape changed.</item>
    /// <item>A cell is its value, then, when its unit is not empty, <see cref="UnitMarkup"/> and the unit (KodeKit dims the unit
    /// with a TMP tag). Formatting allocates nothing: each cell keeps its buffer.</item>
    /// <item>A content column's width only grows: <see cref="TableColumn.ContentChars"/> is its widest cell so far, reset when
    /// its header changes (and by <see cref="ResetWidths"/>).</item>
    /// <item>The highlighted row (<see cref="Highlighted"/>, -1 for none) is the owner's: a value outside the rows highlights none.</item>
    /// <item><see cref="Hide"/> hides a column through the owner (Delta-V Stats: a click on its header); a column that isn't
    /// hideable, or a table without a hide callback, refuses.</item>
    /// <item>The selection (<see cref="Selection"/>: rows toggled, a cell range) is kept by row key; rows that vanish leave it.
    /// <see cref="CopyText"/> writes the selected cells as shown, tab and newline separated; <see cref="CopyRows"/> (a Copy button)
    /// every row's when nothing is selected; <see cref="CopyMarkdown"/> the same rows as a Markdown table.</item>
    /// <item><see cref="Changed"/> fires with what changed, whoever changed it.</item>
    /// </list>
    /// </summary>
    public sealed class TableModel : IDescribeParts
    {
        readonly Func<int> rowCount;
        readonly TableCellFormat format;
        readonly List<TableColumn> columns = new List<TableColumn>();
        Func<long> version;
        Func<int> highlight;
        Func<int, bool> visible;
        Func<int, string> header;
        Func<int, long> rowKey;
        Action<int, ChangeSource> hide;
        long formattedVersion;
        bool formatted, dirty;

        public TableModel(Func<int> rowCount, TableCellFormat format)
        {
            this.rowCount = rowCount ?? throw new ArgumentNullException(nameof(rowCount));
            this.format = format ?? throw new ArgumentNullException(nameof(format));
            Selection = new TableSelection(this);
        }

        // ---- the owner's sources, set once while the table is made ----

        /// <summary>The source's version: cells are formatted again only when it changes (or the shape does).</summary>
        public TableModel WithVersion(Func<long> source)
        {
            version = source;
            return this;
        }

        /// <summary>The highlighted row (-1: none), pulled at each sync.</summary>
        public TableModel WithHighlight(Func<int> row)
        {
            highlight = row;
            return this;
        }

        /// <summary>Each column's visibility and header text by key, pulled at each sync (null: the column's own).</summary>
        public TableModel WithColumnState(Func<int, bool> isVisible, Func<int, string> headerText)
        {
            visible = isVisible;
            header = headerText;
            return this;
        }

        /// <summary>A key per row, so the selection follows a row when rows above it come or go (default: the row's index).</summary>
        public TableModel WithRowKeys(Func<int, long> key)
        {
            rowKey = key;
            return this;
        }

        /// <summary>What hiding a column does (by its key); without it <see cref="Hide"/> refuses.</summary>
        public TableModel WithHide(Action<int, ChangeSource> hideColumn)
        {
            hide = hideColumn;
            return this;
        }

        public TableColumn AddColumn(int key, string headerText, TableWidthRule rule = TableWidthRule.Content, float width = 0, string unit = null)
        {
            var c = new TableColumn(columns.Count, key, headerText, rule, width, unit);
            columns.Add(c);
            dirty = true;
            return c;
        }

        // ---- state ----

        /// <summary>What the table is called, for <see cref="IDescribeParts"/>.</summary>
        public string Label { get; set; }

        /// <summary>Inserted between a cell's value and a non-empty unit (null: nothing).</summary>
        public string UnitMarkup { get; set; }

        public IReadOnlyList<TableColumn> Columns => columns;

        public int RowCount { get; private set; }

        /// <summary>The highlighted row, -1 for none.</summary>
        public int Highlighted { get; private set; } = -1;

        /// <summary>Bumped whenever the shape changes (row count, a column shown or hidden, a header's text).</summary>
        public int ShapeVersion { get; private set; }

        public TableSelection Selection { get; }

        /// <summary>The source of the last <see cref="Hide"/> or selection change.</summary>
        public ChangeSource LastSource { get; internal set; } = ChangeSource.Program;

        /// <summary>Fires with what changed, at a sync or a call that changed something.</summary>
        public event Action<TableChange> Changed;

        public int VisibleColumns
        {
            get
            {
                int n = 0;
                for (int i = 0; i < columns.Count; i++)
                    if (columns[i].Visible) n++;
                return n;
            }
        }

        /// <summary>The first column with this key, or null.</summary>
        public TableColumn ColumnOf(int key)
        {
            for (int i = 0; i < columns.Count; i++)
                if (columns[i].Key == key) return columns[i];
            return null;
        }

        /// <summary>
        /// The buffer of a cell (column by index): its value and unit as last formatted. A hidden column isn't formatted, so its
        /// cells keep the text of their last format (empty if it never was shown).
        /// </summary>
        public CharBuffer Cell(int row, int column)
        {
            TableColumn c = columns[column];
            Ensure(c, row + 1);
            return c.Cells[row];
        }

        /// <summary>A row's key (its index unless the owner gave keys).</summary>
        public long KeyOf(int row) => rowKey != null ? rowKey(row) : row;

        /// <summary>The next sync formats the cells even if the source's version didn't change.</summary>
        public void Invalidate() => dirty = true;

        /// <summary>
        /// Bumped by <see cref="ResetWidths"/>: a host that measured columns at their widest cell measures them all again (a column
        /// may now be narrower than it was).
        /// </summary>
        public int WidthVersion { get; private set; }

        /// <summary>Forgets every content column's widest cell (a restyle): the next sync measures them afresh.</summary>
        public void ResetWidths()
        {
            for (int i = 0; i < columns.Count; i++) columns[i].ContentChars = 0;
            WidthVersion++;
            dirty = true;
        }

        /// <summary>Pulls the owner's state and formats what is needed. Returns what changed (<see cref="Changed"/> fired with it).</summary>
        public TableChange Sync()
        {
            TableChange change = TableChange.None;
            int rows = Math.Max(0, rowCount());
            bool shape = rows != RowCount;
            RowCount = rows;
            for (int i = 0; i < columns.Count; i++)
            {
                TableColumn c = columns[i];
                bool v = visible == null || visible(c.Key);
                if (v != c.Visible)
                {
                    c.Visible = v;
                    shape = true;
                }
                string h = header?.Invoke(c.Key);
                if (h != null && h != c.Header)
                {
                    c.Header = h;
                    c.ContentChars = 0; // fit to the new header
                    shape = true;
                    change |= TableChange.Widths;
                }
            }
            if (shape)
            {
                ShapeVersion++;
                change |= TableChange.Shape;
            }

            int hl = highlight != null ? highlight() : -1;
            if (hl < 0 || hl >= rows) hl = -1;
            if (hl != Highlighted)
            {
                Highlighted = hl;
                change |= TableChange.Highlight;
            }

            long ver = version != null ? version() : 0;
            if (version == null || shape || dirty || !formatted || ver != formattedVersion)
            {
                formattedVersion = ver;
                formatted = true;
                dirty = false;
                if (Format()) change |= TableChange.Widths;
                change |= TableChange.Values;
            }

            if (Selection.Prune()) change |= TableChange.Selection;
            if (change != TableChange.None) Changed?.Invoke(change);
            return change;
        }

        // every visible cell, its value then its unit; true when a content column's widest cell grew
        bool Format()
        {
            bool grew = false;
            int rows = RowCount;
            string markup = UnitMarkup;
            for (int i = 0; i < columns.Count; i++)
            {
                TableColumn c = columns[i];
                if (!c.Visible) continue;
                Ensure(c, rows);
                for (int r = 0; r < rows; r++)
                {
                    CharBuffer b = c.Cells[r].Clear();
                    string unit = c.Unit;
                    format(r, c.Key, b, ref unit);
                    int valueLength = b.Length;
                    c.ValueLengths[r] = valueLength;
                    int length = valueLength + (unit?.Length ?? 0);
                    if (!string.IsNullOrEmpty(unit))
                    {
                        if (markup != null) b.Append(markup);
                        b.Append(unit);
                    }
                    if (length > c.ContentChars)
                    {
                        c.ContentChars = length;
                        grew = true;
                    }
                }
            }
            return grew;
        }

        static void Ensure(TableColumn c, int rows)
        {
            while (c.Cells.Count < rows)
            {
                c.Cells.Add(new CharBuffer(16));
                c.ValueLengths.Add(0);
            }
        }

        /// <summary>
        /// A column of any table was hidden (<see cref="Hide"/>, a saved setting), with its source: for an observer of the whole UI (the
        /// agent bridge's change feed).
        /// </summary>
        public static event Action<TableModel, TableColumn, ChangeSource> AnyHidden;

        /// <summary>Hides a column through the owner. True when the owner was asked (the next sync shows the result, run here).</summary>
        public bool Hide(int column, ChangeSource source)
        {
            if (column < 0 || column >= columns.Count || hide == null) return false;
            TableColumn c = columns[column];
            if (!c.Hideable || !c.Visible) return false;
            LastSource = source;
            hide(c.Key, source);
            Sync();
            AnyHidden?.Invoke(this, c, source);
            return true;
        }

        internal void RaiseSelection()
        {
            Changed?.Invoke(TableChange.Selection);
        }

        /// <summary>A cell's text as shown (value and unit, markup left out, trailing spaces trimmed), appended to <paramref name="into"/>.</summary>
        public void AppendShown(StringBuilder into, int row, int column)
        {
            TableColumn c = columns[column];
            if (row < 0 || row >= RowCount || row >= c.Cells.Count) return;
            CharBuffer b = c.Cells[row];
            int valueLength = c.ValueLengths[row];
            int start = into.Length;
            into.Append(b.Chars, 0, Math.Min(valueLength, b.Length));
            int unitStart = valueLength + (UnitMarkup != null && b.Length > valueLength ? UnitMarkup.Length : 0);
            if (unitStart < b.Length) into.Append(b.Chars, unitStart, b.Length - unitStart);
            int end = into.Length;
            while (end > start && into[end - 1] == ' ') end--;
            into.Length = end;
        }

        /// <summary>
        /// The selected cells as shown: one line per row with a selected cell, the visible columns that have one, separated by tabs
        /// (an unselected cell inside that block is empty). With <paramref name="headers"/>, a first line of their headers.
        /// </summary>
        public void CopyText(StringBuilder into, bool headers = false)
        {
            bool any = false;
            if (headers)
            {
                for (int col = 0; col < columns.Count; col++)
                {
                    if (!ColumnSelected(col)) continue;
                    if (any) into.Append('\t');
                    into.Append(columns[col].Header.Trim()); // a header padded for its width (Delta-V Stats) is written without its spaces
                    any = true;
                }
                if (any) into.Append('\n');
            }
            for (int r = 0; r < RowCount; r++)
            {
                if (!RowHasSelection(r)) continue;
                bool first = true;
                for (int col = 0; col < columns.Count; col++)
                {
                    if (!ColumnSelected(col)) continue;
                    if (!first) into.Append('\t');
                    first = false;
                    if (Selection.Contains(r, col)) AppendShown(into, r, col);
                }
                into.Append('\n');
            }
        }

        /// <summary>
        /// The Copy button's text: the selected cells as <see cref="CopyText"/> writes them, or, when nothing is selected, every row's
        /// visible cells (a first line of headers with <paramref name="headers"/>). Returns the number of rows written.
        /// </summary>
        public int CopyRows(StringBuilder into, bool headers = true)
        {
            bool all = NothingSelected;
            if (headers)
            {
                bool first = true;
                for (int col = 0; col < columns.Count; col++)
                {
                    if (!CopiedColumn(col, all)) continue;
                    if (!first) into.Append('\t');
                    first = false;
                    into.Append(columns[col].Header.Trim()); // a header padded for its width (Delta-V Stats) is written without its spaces
                }
                if (!first) into.Append('\n');
            }
            int rows = 0;
            for (int r = 0; r < RowCount; r++)
            {
                if (!all && !RowHasSelection(r)) continue;
                bool first = true;
                for (int col = 0; col < columns.Count; col++)
                {
                    if (!CopiedColumn(col, all)) continue;
                    if (!first) into.Append('\t');
                    first = false;
                    if (all || Selection.Contains(r, col)) AppendShown(into, r, col);
                }
                into.Append('\n');
                rows++;
            }
            return rows;
        }

        /// <summary>
        /// The Copy as Markdown button's text: the rows <see cref="CopyRows"/> copies (the selected ones, every row when nothing is
        /// selected) as a GitHub-flavoured Markdown table (<see cref="MarkdownTable"/>): the copied columns' headers, an alignment row
        /// with every column right-aligned (<c>---:</c>, as the grid draws its cells), one line per row; cells as shown, units kept,
        /// an unselected cell inside the block empty. Returns the number of rows written (nothing at all when no column is shown).
        /// </summary>
        public int CopyMarkdown(StringBuilder into)
        {
            bool all = NothingSelected;
            var headerTexts = new List<string>();
            for (int col = 0; col < columns.Count; col++)
                if (CopiedColumn(col, all)) headerTexts.Add(columns[col].Header);
            if (headerTexts.Count == 0) return 0;
            var right = new bool[headerTexts.Count];
            for (int i = 0; i < right.Length; i++) right[i] = true;
            MarkdownTable.Header(into, headerTexts, right);
            var cells = new string[headerTexts.Count];
            var cell = new StringBuilder();
            int rows = 0;
            for (int r = 0; r < RowCount; r++)
            {
                if (!all && !RowHasSelection(r)) continue;
                int k = 0;
                for (int col = 0; col < columns.Count; col++)
                {
                    if (!CopiedColumn(col, all)) continue;
                    cell.Length = 0;
                    if (all || Selection.Contains(r, col)) AppendShown(cell, r, col);
                    cells[k++] = cell.ToString();
                }
                MarkdownTable.Row(into, cells);
                rows++;
            }
            return rows;
        }

        bool NothingSelected => Selection.RowCount == 0 && !Selection.HasRange;

        // a column the copies write: every shown one when nothing is selected, else the shown ones with a selected cell
        bool CopiedColumn(int col, bool all) => columns[col].Copied && (all ? columns[col].Visible : ColumnSelected(col));

        bool ColumnSelected(int col)
        {
            if (!columns[col].Visible) return false;
            for (int r = 0; r < RowCount; r++)
                if (Selection.Contains(r, col)) return true;
            return false;
        }

        bool RowHasSelection(int r)
        {
            for (int col = 0; col < columns.Count; col++)
                if (Selection.Contains(r, col)) return true;
            return false;
        }

        /// <summary>The table, its visible columns' headers, its rows (On: selected; Current: highlighted) and the cells of a selected range.</summary>
        public void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription { Name = "table", Role = PartRole.Table, Label = Label });
            int first = -1;
            for (int i = 0; i < columns.Count; i++)
            {
                TableColumn c = columns[i];
                if (!c.Visible) continue;
                if (first < 0) first = i;
                // a column without a header text (PSG Stages' buttons) is described by its name
                into.Add(new PartDescription { Name = "column." + c.Key, Role = PartRole.ColumnHeader, Label = string.IsNullOrEmpty(c.Header) ? c.Name : c.Header });
            }
            var text = new StringBuilder();
            for (int r = 0; r < RowCount; r++)
            {
                text.Length = 0;
                if (first >= 0) AppendShown(text, r, first);
                PartFlags flags = (Selection.IsRowSelected(r) ? PartFlags.On : PartFlags.None) | (r == Highlighted ? PartFlags.Current : PartFlags.None);
                into.Add(new PartDescription { Name = "row." + r, Role = PartRole.Row, Label = text.ToString(), Flags = flags });
            }
            if (!Selection.HasRange) return;
            for (int r = Selection.Top; r <= Selection.Bottom && r < RowCount; r++)
            for (int col = Selection.Left; col <= Selection.Right && col < columns.Count; col++)
            {
                if (!columns[col].Visible) continue;
                text.Length = 0;
                AppendShown(text, r, col);
                into.Add(new PartDescription { Name = "cell." + r + "." + columns[col].Key, Role = PartRole.Cell, Label = text.ToString(), Flags = PartFlags.On });
            }
        }
    }

    /// <summary>
    /// The selection of a <see cref="TableModel"/>, for select-and-copy (roadmap item 21, redesigned with no modifier key: a toggle
    /// per row and a cell range set by two picks).
    /// <list type="bullet">
    /// <item>Selected rows are kept by key (<see cref="TableModel.KeyOf"/>): a row that vanishes leaves the selection; one that
    /// moves keeps it.</item>
    /// <item>A range of rows (<see cref="SelectRows"/>) follows the table's order, whichever end is given first.</item>
    /// <item>A cell range is an anchor and a focus cell (row, column index); it is clamped to the rows that are left.</item>
    /// <item>Every change records its source and fires the table's <see cref="TableModel.Changed"/> with
    /// <see cref="TableChange.Selection"/>, and <see cref="Changed"/>.</item>
    /// </list>
    /// </summary>
    [NotAWidget("helper: the selected rows and cells of a TableModel, which describes them")]
    public sealed class TableSelection
    {
        readonly TableModel table;
        readonly List<long> keys = new List<long>();
        int anchorRow = -1, anchorColumn, focusRow, focusColumn;

        internal TableSelection(TableModel table) => this.table = table;

        /// <summary>The selection changed.</summary>
        public event Action Changed;

        /// <summary>How many rows are selected.</summary>
        public int RowCount => keys.Count;

        public bool HasRange => anchorRow >= 0;
        public int Top => Math.Min(anchorRow, focusRow);
        public int Bottom => Math.Max(anchorRow, focusRow);
        public int Left => Math.Min(anchorColumn, focusColumn);
        public int Right => Math.Max(anchorColumn, focusColumn);

        public bool IsRowSelected(int row) => row >= 0 && row < table.RowCount && keys.Contains(table.KeyOf(row));

        /// <summary>The cell is selected: its row is, or it is in the range; a hidden column's cells never are.</summary>
        public bool Contains(int row, int column)
        {
            if (row < 0 || row >= table.RowCount || column < 0 || column >= table.Columns.Count || !table.Columns[column].Visible) return false;
            if (IsRowSelected(row)) return true;
            return HasRange && row >= Top && row <= Bottom && column >= Left && column <= Right;
        }

        public void ToggleRow(int row, ChangeSource source)
        {
            if (row < 0 || row >= table.RowCount) return;
            long k = table.KeyOf(row);
            if (!keys.Remove(k)) keys.Add(k);
            Done(source);
        }

        /// <summary>Selects rows <paramref name="from"/> to <paramref name="to"/>, both included, in the table's order.</summary>
        public void SelectRows(int from, int to, ChangeSource source)
        {
            int a = Math.Max(0, Math.Min(from, to)), b = Math.Min(table.RowCount - 1, Math.Max(from, to));
            for (int r = a; r <= b; r++)
            {
                long k = table.KeyOf(r);
                if (!keys.Contains(k)) keys.Add(k);
            }
            Done(source);
        }

        public void SelectAll(ChangeSource source) => SelectRows(0, table.RowCount - 1, source);

        /// <summary>A cell range from the anchor cell to the focus cell (column indexes of <see cref="TableModel.Columns"/>).</summary>
        public void SelectCells(int anchorRow, int anchorColumn, int focusRow, int focusColumn, ChangeSource source)
        {
            if (table.RowCount == 0 || table.Columns.Count == 0) return;
            this.anchorRow = Clamp(anchorRow, table.RowCount);
            this.anchorColumn = Clamp(anchorColumn, table.Columns.Count);
            this.focusRow = Clamp(focusRow, table.RowCount);
            this.focusColumn = Clamp(focusColumn, table.Columns.Count);
            Done(source);
        }

        /// <summary>Moves the range's focus cell (a second pick); without a range, starts one there.</summary>
        public void ExtendTo(int row, int column, ChangeSource source)
        {
            if (!HasRange)
            {
                SelectCells(row, column, row, column, source);
                return;
            }
            SelectCells(anchorRow, anchorColumn, row, column, source);
        }

        public void Clear(ChangeSource source)
        {
            keys.Clear();
            anchorRow = -1;
            Done(source);
        }

        static int Clamp(int v, int count) => v < 0 ? 0 : v >= count ? count - 1 : v;

        void Done(ChangeSource source)
        {
            table.LastSource = source;
            Changed?.Invoke();
            table.RaiseSelection();
        }

        // after the rows changed: keys of rows that are gone leave, the range is clamped to the rows left. True when it changed.
        internal bool Prune()
        {
            bool changed = false;
            for (int i = keys.Count - 1; i >= 0; i--)
            {
                bool found = false;
                for (int r = 0; r < table.RowCount && !found; r++) found = table.KeyOf(r) == keys[i];
                if (found) continue;
                keys.RemoveAt(i);
                changed = true;
            }
            if (HasRange)
            {
                if (table.RowCount == 0) anchorRow = -1;
                else
                {
                    int a = Clamp(anchorRow, table.RowCount), f = Clamp(focusRow, table.RowCount);
                    if (a != anchorRow || f != focusRow) changed = true;
                    anchorRow = a;
                    focusRow = f;
                }
                if (table.RowCount == 0) changed = true;
            }
            if (changed) Changed?.Invoke();
            return changed;
        }
    }
}
