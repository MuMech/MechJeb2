using System.Collections.Generic;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Draws a <see cref="Headless.TableModel"/> as a retained grid (Delta-V Stats): one vertical layout per column, built once,
    /// its header a flat <see cref="KodeWindow.HeaderButton"/> that hides the column (<see cref="Headless.TableModel.Hide"/>), then
    /// a pool of right-aligned cells that only grows. A shape change (<see cref="Headless.TableModel.ShapeVersion"/>) shows and
    /// hides columns and cells; each <see cref="Draw"/> pushes the cells' buffers with <see cref="CharText.Commit"/> (nothing
    /// allocated when nothing changed), widens a content column to its widest cell and its header, and paints the highlighted
    /// row in the theme's accent.
    /// </summary>
    public sealed class TableGrid
    {
        sealed class Column
        {
            public Headless.TableColumn Model;
            public Layout Root;
            public UIButton Header;
            public string HeaderShown;
            public int MeasuredChars;
            public LayoutElement Size;
            public readonly List<UIText> Cells = new List<UIText>();
            public readonly List<CharText> Texts = new List<CharText>();
        }

        readonly Headless.TableModel model;
        readonly List<Column> columns = new List<Column>();
        int builtShape = -1, builtWidths;

        /// <summary>The grid's row of columns.</summary>
        public Layout Root { get; }

        public Headless.TableModel Model => model;

        public TableGrid(UIObject parent, Headless.TableModel model)
        {
            this.model = model;
            Root = KodeWindow.Row(parent);
            // columns at the top: a row's default centres them, so one shorter than the others would sit half a row lower
            Root.Spacing(0).ChildAlignment(TextAnchor.UpperLeft);
            for (int i = 0; i < model.Columns.Count; i++) columns.Add(BuildColumn(model.Columns[i]));
            // what finds the grid from its window: a test, the agent bridge
            Root.gameObject.AddComponent<TableGridHandle>().Grid = this;
        }

        Column BuildColumn(Headless.TableColumn m)
        {
            var c = new Column { Model = m };
            c.Root = Root.Add<Layout>("Col_" + (m.Name ?? m.Key.ToString()));
            c.Root.Vertical().ControlChildSize(true, true).ChildForceExpand(true, false).Spacing(0);
            c.Root.Finish();
            c.Size = c.Root.gameObject.AddComponent<LayoutElement>();
            if (m.WidthRule == Headless.TableWidthRule.Fixed) c.Size.minWidth = c.Size.preferredWidth = m.Width;
            int index = m.Index;
            c.Header = KodeWindow.HeaderButton(c.Root, m.Header, () =>
            {
                if (model.Hide(index, KodeWindow.Pointer)) Draw();
            });
            TMP_Text text = c.Header.GetComponentInChildren<TMP_Text>();
            text.alignment = TextAlignmentOptions.Right;
            text.enableWordWrapping = false; // one line, so the rows of every column line up
            c.HeaderShown = m.Header;
            if (!string.IsNullOrEmpty(m.Tooltip)) Tooltips.Add(c.Header.gameObject, m.Tooltip);
            return c;
        }

        void EnsureCells(Column c, int rows)
        {
            while (c.Cells.Count < rows)
            {
                UIText t = KodeWindow.LiveLabel(c.Root, "0", true);
                t.tmpText.alignment = TextAlignmentOptions.Right;
                c.Cells.Add(t);
                c.Texts.Add(new CharText(t.tmpText, model.Cell(c.Cells.Count - 1, c.Model.Index)));
                if (rowCard != null) AttachRowCard(t, c.Cells.Count - 1);
            }
        }

        // ---- a hover card per row ----

        System.Action<int, HoverCard> rowCard;
        readonly List<object> rowKeys = new List<object>();

        /// <summary>
        /// A hover card on each row: <paramref name="build"/> fills it with the row's details when it opens. Every cell of a row
        /// shares the row's card (moving along the row keeps it; to the next row switches at once), which opens beside the window
        /// at the row's height.
        /// </summary>
        public void RowCards(System.Action<int, HoverCard> build)
        {
            rowCard = build;
            foreach (Column c in columns)
                for (int i = 0; i < c.Cells.Count; i++) AttachRowCard(c.Cells[i], i);
        }

        /// <summary>What the hover is on for a row's cells (<see cref="TooltipTrigger.Key"/>): one key per row.</summary>
        public object RowKey(int row)
        {
            while (rowKeys.Count <= row) rowKeys.Add(new RowHover(rowKeys.Count));
            return rowKeys[row];
        }

        void AttachRowCard(UIText cell, int row) =>
            Tooltips.Add(cell.gameObject, card => rowCard?.Invoke(row, card), RowKey(row), Tooltips.BesideWindow(KodeWindow.CellOf(cell).GetComponent<RectTransform>()));

        /// <summary>A row's hover key: its index (tests read it).</summary>
        public sealed class RowHover
        {
            public RowHover(int row) => Row = row;
            public int Row { get; }
        }

        /// <summary>
        /// Draws the model as its last <see cref="Headless.TableModel.Sync"/> left it. A new shape is drawn only once the grid is
        /// shown (TMP set up under an inactive parent misses Awake); until then nothing is drawn.
        /// </summary>
        public void Draw()
        {
            int rows = model.RowCount;
            if (model.ShapeVersion != builtShape)
            {
                if (!Root.gameObject.activeInHierarchy) return;
                foreach (Column c in columns)
                {
                    bool visible = c.Model.Visible;
                    // create cells while the column is active: TMP set up under an inactive parent misses Awake
                    if (visible && !c.Root.gameObject.activeSelf) c.Root.gameObject.SetActive(true);
                    if (visible) EnsureCells(c, rows);
                    if (c.Root.gameObject.activeSelf != visible) c.Root.gameObject.SetActive(visible);
                    if (c.Model.Header != c.HeaderShown)
                    {
                        c.HeaderShown = c.Model.Header;
                        c.Header.Text(c.HeaderShown);
                        c.MeasuredChars = 0; // re-fit to the new header
                    }
                    for (int i = 0; i < c.Cells.Count; i++)
                    {
                        bool on = i < rows;
                        // the cell, not its text: the cell holds the row's height in the column
                        GameObject cell = KodeWindow.CellOf(c.Cells[i]);
                        if (cell.activeSelf != on) cell.SetActive(on);
                    }
                }
                // only once every column has its cells: a rebuild that threw half way is tried again at the next draw
                builtShape = model.ShapeVersion;
            }

            // the model forgot its widest cells (ResetWidths): measure every column again, so one can narrow
            if (model.WidthVersion != builtWidths)
            {
                builtWidths = model.WidthVersion;
                foreach (Column c in columns) c.MeasuredChars = 0;
            }

            Theme th = Theme.Current;
            int current = model.Highlighted;
            foreach (Column c in columns)
            {
                if (!c.Model.Visible) continue;
                if (c.Model.WidthRule == Headless.TableWidthRule.Content && c.Model.ContentChars > c.MeasuredChars) FitWidth(c);
                for (int row = 0; row < rows && row < c.Texts.Count; row++)
                {
                    c.Texts[row].Commit();
                    Color want = row == current ? th.Accent : th.Text;
                    TMP_Text t = c.Cells[row].tmpText;
                    if (t.color != want) t.color = want;
                }
            }
        }

        /// <summary>Column width = max(header, widest value so far); only grows, so changing values never re-lay out.</summary>
        void FitWidth(Column c)
        {
            c.MeasuredChars = c.Model.ContentChars;
            float value = TextMeasure.Measure(new string('0', c.MeasuredChars), Theme.Current).x;
            float header = TextMeasure.Measure(c.HeaderShown, Theme.Current).x + 12; // + button padding
            c.Size.preferredWidth = Mathf.Max(Mathf.Max(value, header), c.Model.Width);
        }

        // ---- for tests and an agent ----

        /// <summary>The header button of a column (by index in the model).</summary>
        public UIButton HeaderOf(int column) => columns[column].Header;

        /// <summary>The cell of a row in a column (by index in the model), or null when it isn't made yet.</summary>
        public UIText CellAt(int row, int column) => row < columns[column].Cells.Count ? columns[column].Cells[row] : null;

        /// <summary>The columns shown now.</summary>
        public int VisibleColumns
        {
            get
            {
                int n = 0;
                foreach (Column c in columns)
                    if (c.Root.gameObject.activeSelf) n++;
                return n;
            }
        }
    }

    /// <summary>On a <see cref="TableGrid"/>'s root: the grid, for whoever finds it in a window (a test, the agent bridge).</summary>
    public sealed class TableGridHandle : MonoBehaviour
    {
        public TableGrid Grid { get; internal set; }
    }
}
