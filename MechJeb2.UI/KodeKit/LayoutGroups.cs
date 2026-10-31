using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// Children in rows of <see cref="Columns"/> cells, all the same size: as wide as the widest child and as high as the
    /// tallest. Room to spare is shared equally, so the cells line up whatever their labels (SmartASS targets, joined
    /// choices, the navball cross).
    /// </summary>
    public class UniformGridLayout : LayoutGroup
    {
        [SerializeField] int columns = 3;
        [SerializeField] float spacing = 2;
        float cellMinW, cellPrefW, cellMinH, cellPrefH;

        public int Columns
        {
            get => columns;
            set => SetProperty(ref columns, Mathf.Max(1, value));
        }

        public float Spacing
        {
            get => spacing;
            set => SetProperty(ref spacing, value);
        }

        int Rows => (rectChildren.Count + columns - 1) / columns;

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            cellMinW = cellPrefW = 0;
            foreach (RectTransform c in rectChildren)
            {
                cellMinW = Mathf.Max(cellMinW, LayoutUtility.GetMinWidth(c));
                cellPrefW = Mathf.Max(cellPrefW, LayoutUtility.GetPreferredWidth(c));
            }
            float gaps = spacing * (columns - 1) + padding.horizontal;
            SetLayoutInputForAxis(cellMinW * columns + gaps, cellPrefW * columns + gaps, 1, 0);
        }

        public override void CalculateLayoutInputVertical()
        {
            cellMinH = cellPrefH = 0;
            foreach (RectTransform c in rectChildren)
            {
                cellMinH = Mathf.Max(cellMinH, LayoutUtility.GetMinHeight(c));
                cellPrefH = Mathf.Max(cellPrefH, LayoutUtility.GetPreferredHeight(c));
            }
            int rows = Rows;
            float gaps = spacing * Mathf.Max(0, rows - 1) + padding.vertical;
            SetLayoutInputForAxis(cellMinH * rows + gaps, cellPrefH * rows + gaps, -1, 1);
        }

        public override void SetLayoutHorizontal()
        {
            float w = Mathf.Max(cellMinW, (rectTransform.rect.width - padding.horizontal - spacing * (columns - 1)) / columns);
            for (int i = 0; i < rectChildren.Count; i++)
                SetChildAlongAxis(rectChildren[i], 0, padding.left + i % columns * (w + spacing), w);
        }

        public override void SetLayoutVertical()
        {
            int rows = Mathf.Max(1, Rows);
            float h = Mathf.Max(cellMinH, (rectTransform.rect.height - padding.vertical - spacing * (rows - 1)) / rows);
            for (int i = 0; i < rectChildren.Count; i++)
                SetChildAlongAxis(rectChildren[i], 1, padding.top + i / columns * (h + spacing), h);
        }
    }

    /// <summary>
    /// Children left to right at their preferred size, wrapping to a new line when the next one does not fit (chips,
    /// pills). It asks for the width of its widest child (or <see cref="LineWidth"/>) and takes what the window gives it,
    /// so it never widens a window to fit everything on one line.
    /// </summary>
    public class FlowLayout : LayoutGroup
    {
        [SerializeField] float spacing = 4, lineSpacing = 3, lineWidth;

        public float Spacing
        {
            get => spacing;
            set => SetProperty(ref spacing, value);
        }

        public float LineSpacing
        {
            get => lineSpacing;
            set => SetProperty(ref lineSpacing, value);
        }

        /// <summary>The width it asks for; 0: its widest child's.</summary>
        public float LineWidth
        {
            get => lineWidth;
            set => SetProperty(ref lineWidth, value);
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            float widest = 0;
            foreach (RectTransform c in rectChildren)
                widest = Mathf.Max(widest, LayoutUtility.GetPreferredWidth(c));
            SetLayoutInputForAxis(widest + padding.horizontal, Mathf.Max(widest, lineWidth) + padding.horizontal, 1, 0);
        }

        readonly List<float> lineTop = new List<float>(), lineHeight = new List<float>();

        enum Pass { Measure, Horizontal, Vertical }

        // walks the lines at the current width: measures their heights, or places the children (no callbacks, so a
        // relayout makes no garbage)
        float Lines(Pass pass)
        {
            if (pass == Pass.Measure) lineHeight.Clear();
            float room = Mathf.Max(0, rectTransform.rect.width - padding.horizontal);
            float x = 0, y = 0, lineH = 0;
            int line = 0;
            foreach (RectTransform c in rectChildren)
            {
                float w = Mathf.Min(LayoutUtility.GetPreferredWidth(c), room);
                float h = LayoutUtility.GetPreferredHeight(c);
                if (x > 0 && x + w > room)
                {
                    y += lineH + lineSpacing;
                    x = lineH = 0;
                    line++;
                }
                if (pass == Pass.Measure)
                {
                    if (line == lineHeight.Count) lineHeight.Add(0);
                    lineHeight[line] = Mathf.Max(lineHeight[line], h);
                }
                else if (pass == Pass.Horizontal) SetChildAlongAxis(c, 0, padding.left + x, w);
                else SetChildAlongAxis(c, 1, lineTop[line] + (lineHeight[line] - h) / 2, h);
                x += w + spacing;
                lineH = Mathf.Max(lineH, h);
            }
            return rectChildren.Count == 0 ? 0 : y + lineH;
        }

        public override void CalculateLayoutInputVertical()
        {
            float h = Lines(Pass.Measure) + padding.vertical;
            SetLayoutInputForAxis(h, h, -1, 1);
        }

        public override void SetLayoutHorizontal() => Lines(Pass.Horizontal);

        public override void SetLayoutVertical()
        {
            Lines(Pass.Measure);
            lineTop.Clear();
            float y = padding.top;
            foreach (float h in lineHeight)
            {
                lineTop.Add(y);
                y += h + lineSpacing;
            }
            Lines(Pass.Vertical);
        }
    }

    /// <summary>
    /// A row of a <see cref="TableLayout"/>. Its children are the cells; the table places them. Being a (no-op) layout
    /// group keeps uGUI's layout passes walking down into the cells and a change in a cell walking up to the table.
    /// </summary>
    public class TableRow : UIBehaviour, ILayoutElement, ILayoutGroup
    {
        public void CalculateLayoutInputHorizontal() { }
        public void CalculateLayoutInputVertical() { }
        public void SetLayoutHorizontal() { }
        public void SetLayoutVertical() { }
        public float minWidth => 0;
        public float preferredWidth => 0;
        public float flexibleWidth => -1;
        public float minHeight => 0;
        public float preferredHeight => 0;
        public float flexibleHeight => -1;
        public int layoutPriority => -1;

        void Dirty()
        {
            if (IsActive()) LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Dirty();
        }

        protected override void OnDisable()
        {
            // rebuild the table, which no longer shows this row
            if (transform.parent != null) LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform.parent);
            base.OnDisable();
        }

        void OnTransformChildrenChanged() => Dirty();
    }

    /// <summary>
    /// Rows of cells in shared columns: each column is as wide as its widest cell, so toggles, labels, fields and units
    /// line up from row to row (the property grid). <see cref="FlexibleColumn"/> takes the width left over. A row with a
    /// single cell spans every column (notes, sub-headers). Hidden rows and cells are skipped; a hidden cell keeps its
    /// column, so the cells after it stay in place.
    /// </summary>
    public class TableLayout : LayoutGroup
    {
        [SerializeField] float columnSpacing = 6, rowSpacing = 2;
        [SerializeField] int flexibleColumn = 1;

        public float ColumnSpacing
        {
            get => columnSpacing;
            set => SetProperty(ref columnSpacing, value);
        }

        public float RowSpacing
        {
            get => rowSpacing;
            set => SetProperty(ref rowSpacing, value);
        }

        /// <summary>The column that takes the extra width (-1: none, the table keeps its preferred width).</summary>
        public int FlexibleColumn
        {
            get => flexibleColumn;
            set => SetProperty(ref flexibleColumn, value);
        }

        // cells of each shown row, by column (null: a hidden cell); a row of one cell spans the table
        readonly List<RectTransform[]> rows = new List<RectTransform[]>();
        readonly List<float> colMin = new List<float>(), colPref = new List<float>(), colX = new List<float>(), colW = new List<float>();
        readonly List<float> rowH = new List<float>();
        float spanPref, spanMin;
        int columns;

        static bool Shown(RectTransform rt)
        {
            if (!rt.gameObject.activeInHierarchy) return false;
            var ignorer = rt.GetComponent<ILayoutIgnorer>();
            return ignorer == null || !ignorer.ignoreLayout;
        }

        public override void CalculateLayoutInputHorizontal()
        {
            base.CalculateLayoutInputHorizontal();
            columns = 0;
            for (int r = 0; r < rectChildren.Count; r++)
            {
                RectTransform row = rectChildren[r];
                // the arrays are kept from one layout to the next: a relayout makes no garbage
                if (r == rows.Count) rows.Add(null);
                RectTransform[] cells = rows[r];
                if (cells == null || cells.Length != row.childCount) rows[r] = cells = new RectTransform[row.childCount];
                for (int i = 0; i < cells.Length; i++)
                    cells[i] = row.GetChild(i) is RectTransform c && Shown(c) ? c : null;
                if (cells.Length > 1) columns = Mathf.Max(columns, cells.Length);
            }
            if (rows.Count > rectChildren.Count) rows.RemoveRange(rectChildren.Count, rows.Count - rectChildren.Count);

            colMin.Clear();
            colPref.Clear();
            for (int i = 0; i < columns; i++)
            {
                colMin.Add(0);
                colPref.Add(0);
            }
            spanMin = spanPref = 0;
            foreach (RectTransform[] cells in rows)
            {
                if (cells.Length == 1)
                {
                    if (cells[0] == null) continue;
                    spanMin = Mathf.Max(spanMin, LayoutUtility.GetMinWidth(cells[0]));
                    spanPref = Mathf.Max(spanPref, LayoutUtility.GetPreferredWidth(cells[0]));
                    continue;
                }
                for (int i = 0; i < cells.Length; i++)
                {
                    if (cells[i] == null) continue;
                    colMin[i] = Mathf.Max(colMin[i], LayoutUtility.GetMinWidth(cells[i]));
                    colPref[i] = Mathf.Max(colPref[i], LayoutUtility.GetPreferredWidth(cells[i]));
                }
            }

            float gaps = Gaps() + padding.horizontal;
            float min = gaps, pref = gaps;
            for (int i = 0; i < columns; i++)
            {
                min += colMin[i];
                pref += colPref[i];
            }
            min = Mathf.Max(min, spanMin + padding.horizontal);
            pref = Mathf.Max(pref, spanPref + padding.horizontal);
            SetLayoutInputForAxis(min, pref, flexibleColumn >= 0 ? 1 : -1, 0);
        }

        // spacing between the columns that have something in them
        float Gaps()
        {
            int used = 0;
            for (int i = 0; i < columns; i++)
                if (colPref[i] > 0) used++;
            return columnSpacing * Mathf.Max(0, used - 1);
        }

        public override void SetLayoutHorizontal()
        {
            float width = rectTransform.rect.width - padding.horizontal;
            float extra = width - Gaps();
            colW.Clear();
            colX.Clear();
            for (int i = 0; i < columns; i++)
            {
                colW.Add(colPref[i]);
                extra -= colPref[i];
            }
            if (columns > 0)
            {
                int flex = Mathf.Clamp(flexibleColumn, 0, columns - 1);
                if (extra > 0 && flexibleColumn >= 0) colW[flex] += extra;
                // too narrow: the flexible column gives back down to its minimum first, then the others in proportion
                if (extra < 0)
                {
                    float give = Mathf.Min(-extra, colW[flex] - colMin[flex]);
                    colW[flex] -= give;
                    extra += give;
                    float slack = 0;
                    for (int i = 0; i < columns; i++) slack += colW[i] - colMin[i];
                    if (extra < 0 && slack > 0)
                        for (int i = 0; i < columns; i++)
                            colW[i] -= (colW[i] - colMin[i]) / slack * Mathf.Min(-extra, slack);
                }
            }
            float x = 0;
            for (int i = 0; i < columns; i++)
            {
                colX.Add(x);
                if (colPref[i] > 0) x += colW[i] + columnSpacing;
            }

            for (int r = 0; r < rows.Count; r++)
            {
                SetChildAlongAxis(rectChildren[r], 0, padding.left, width);
                RectTransform[] cells = rows[r];
                if (cells.Length == 1)
                {
                    if (cells[0] != null) SetChildAlongAxis(cells[0], 0, 0, width);
                    continue;
                }
                for (int i = 0; i < cells.Length; i++)
                    if (cells[i] != null) SetChildAlongAxis(cells[i], 0, colX[i], colW[i]);
            }
        }

        public override void CalculateLayoutInputVertical()
        {
            rowH.Clear();
            float total = padding.vertical, min = padding.vertical;
            foreach (RectTransform[] cells in rows)
            {
                float h = 0, m = 0;
                foreach (RectTransform c in cells)
                {
                    if (c == null) continue;
                    h = Mathf.Max(h, LayoutUtility.GetPreferredHeight(c));
                    m = Mathf.Max(m, LayoutUtility.GetMinHeight(c));
                }
                rowH.Add(h);
                total += h;
                min += m;
            }
            float gaps = rowSpacing * Mathf.Max(0, rows.Count - 1);
            SetLayoutInputForAxis(min + gaps, total + gaps, -1, 1);
        }

        public override void SetLayoutVertical()
        {
            float y = padding.top;
            for (int r = 0; r < rows.Count; r++)
            {
                float h = rowH[r];
                SetChildAlongAxis(rectChildren[r], 1, y, h);
                // cells at their preferred height, centred in the row (a checkbox beside a taller field)
                foreach (RectTransform c in rows[r])
                {
                    if (c == null) continue;
                    float ch = Mathf.Min(LayoutUtility.GetPreferredHeight(c), h);
                    SetChildAlongAxis(c, 1, (h - ch) / 2, ch);
                }
                y += h + rowSpacing;
            }
        }
    }
}
