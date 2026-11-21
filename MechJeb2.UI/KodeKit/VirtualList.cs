using System;
using System.Collections.Generic;
using KodeKit.Headless;
using KodeUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KodeKit
{
    /// <summary>
    /// A long list that builds only the rows it shows (ui-sdk's virtualizer), drawn from a <see cref="VirtualListModel"/>:
    /// a scroll list like <see cref="KodeWindow.ScrollList"/>, whose content has no layout group. It holds a pool of rows
    /// (<see cref="VirtualListModel.PoolSize"/>, about the rows that fit plus the overscan), each placed by hand at its
    /// index's offset and bound to it with the host's <c>bind</c> callback when a scroll brings a new index into view.
    /// <list type="bullet">
    /// <item>Each row is a layout root (its parent has no group): a text change inside it relayouts that row only.</item>
    /// <item>Every row has the measured height of the first row (bound to index 0), measured again after a restyle or a width
    /// change; or, with <see cref="SetRowHeights"/>, each row its own height at the list's width (rows whose text wraps). One
    /// row can be taller (<see cref="SetExpanded"/>).</item>
    /// <item>Nothing is measured or drawn before the list has a width (its window's first layout).</item>
    /// <item>A row's index can change on any scroll: a click handler asks <see cref="IndexOf"/>, never captures an index.</item>
    /// <item>A host whose rows show live values updates them from <see cref="VirtualListModel.RenderFirst"/> to
    /// <see cref="VirtualListModel.RenderLast"/> with <see cref="RowAt"/>.</item>
    /// </list>
    /// </summary>
    public sealed class VirtualList<TRow> : IVirtualList where TRow : class
    {
        public VirtualListModel Model { get; }
        public readonly ScrollView View;
        public readonly Layout Content;

        readonly Func<UIObject, TRow> create;
        readonly Func<TRow, RectTransform> rectOf;
        readonly Action<TRow, int> bind;
        readonly RectTransform content, viewport;

        // the pool: rows[s] shows index bound[s] (-1: none, hidden)
        readonly List<TRow> rows = new List<TRow>();
        readonly List<int> bound = new List<int>();
        readonly List<float> placedTop = new List<float>();
        readonly List<float> placedHeight = new List<float>();
        int poolUsed;
        int laidVersion = -1;
        bool measure = true;
        float measuredWidth;
        Func<int, float, float> heightAtWidth;
        readonly Func<int, float> heightOf;
        GameObject empty;

        // a scroll asked before the rows have a height (the window not laid out yet), applied once they are measured
        bool pending;
        int pendingIndex = -1;
        float pendingOffset;
        ScrollAlign pendingAlign;
        ChangeSource pendingSource;

        bool Unmeasured => Model.Count > 0 && Model.PoolSize == 0;

        /// <summary>Bumped by every restyle: a host that caches measured heights drops them when it changes.</summary>
        public int StyleVersion { get; private set; }

        /// <summary>
        /// Bumped whenever the data behind an index may have changed (<see cref="SetCount"/>, <see cref="Rebind"/>, and so
        /// <see cref="Inserted"/> and <see cref="Removed"/>): a row keeps its index across them, so anything cached per row by
        /// its index (a selection box's name) compares this too.
        /// </summary>
        public int BindVersion { get; private set; }

        /// <summary>The width rows are measured at (the content's), 0 before the first layout.</summary>
        public float MeasuredWidth => measuredWidth;

        public ScrollRect ScrollRect => View.ScrollRect;

        /// <param name="height">The list's height, as <see cref="KodeWindow.ScrollList"/>'s.</param>
        /// <param name="count">The rows.</param>
        /// <param name="create">Makes a row under the content (a ListRow, a Layout of widgets).</param>
        /// <param name="rectOf">The row's root.</param>
        /// <param name="bind">Shows index <c>i</c> in a row; called only when the row's index changes, or on <see cref="Rebind"/>.</param>
        /// <param name="spacing">Between rows, as <see cref="KodeWindow.ScrollList"/>'s.</param>
        public VirtualList(KodeWindow w, UIObject parent, float height, int count, Func<UIObject, TRow> create, Func<TRow, RectTransform> rectOf,
            Action<TRow, int> bind, int spacing = 0)
            : this(w.AddSync, parent, height, count, create, rectOf, bind, spacing)
        {
        }

        /// <summary>
        /// A list outside a window (a drop-down's popup): <paramref name="addSync"/> takes the per-frame sync, which its host runs once a
        /// frame while the list lives (a window's syncs outlive anything built and destroyed in it).
        /// </summary>
        public VirtualList(Action<Action> addSync, UIObject parent, float height, int count, Func<UIObject, TRow> create, Func<TRow, RectTransform> rectOf,
            Action<TRow, int> bind, int spacing = 0)
        {
            this.create = create;
            this.rectOf = rectOf;
            this.bind = bind;
            heightOf = i => heightAtWidth(i, measuredWidth);
            Model = new VirtualListModel(count, 0, height, spacing);

            View = parent.Add<ScrollView>("VirtualList");
            View.PreferredHeight(height);
            View.FlexibleLayout(true, false);
            View.Horizontal(false);
            View.Vertical(true);
            View.BackgroundColor(KodeWindow.ListBackground);
            ScrollRect sr = View.ScrollRect;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 25;
            sr.inertia = false;

            // no layout group: the rows are placed by hand
            Content = View.Content;
            content = Content.rectTransform;
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;

            // the viewport (which masks the rows) leaves room for the scrollbar on the right
            viewport = View.Viewport.rectTransform;
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = new Vector2(-12, 0);

            UIScrollbar bar = View.Add<UIScrollbar>("Bar");
            KodeWindow.NoSelect(bar.GetComponent<Scrollbar>());
            bar.Direction(Scrollbar.Direction.BottomToTop);
            RectTransform br = bar.rectTransform;
            br.anchorMin = new Vector2(1, 0);
            br.anchorMax = new Vector2(1, 1);
            br.pivot = new Vector2(1, 1);
            br.sizeDelta = new Vector2(10, 0);
            br.anchoredPosition = Vector2.zero;
            View.VerticalScrollbar = bar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            Image barImage = bar.GetComponent<Image>();
            Image handleImage = bar.scrollbar.handleRect.GetComponent<Image>();
            KodeWindow.Styled(bar.gameObject, th =>
            {
                barImage.color = new Color(0, 0, 0, 0.35f);
                handleImage.color = new Color(th.Text.r, th.Text.g, th.Text.b, 0.6f);
                // a new font size or padding: the rows' height again
                measure = true;
                StyleVersion++;
            });

            sr.onValueChanged.AddListener(OnScrolled);
            addSync(Sync);
            Sync();
        }

        /// <summary>Called with each row's root as the pool makes it (a drag handler goes on it).</summary>
        public event Action<GameObject> RowCreated;

        /// <summary>Called with a row's root and its new index each time the row is bound to an index (a scroll, a rebind).</summary>
        public event Action<GameObject, int> RowBound;

        public RectTransform ContentRect => content;
        public RectTransform ViewportRect => viewport;

        /// <summary>The roots of the rows built so far.</summary>
        public IEnumerable<GameObject> RowObjects
        {
            get
            {
                for (int s = 0; s < rows.Count; s++) yield return rectOf(rows[s]).gameObject;
            }
        }

        /// <summary>The index the row with root <paramref name="go"/> shows (-1: none, or not a row of this list).</summary>
        public int IndexOfObject(GameObject go)
        {
            for (int s = 0; s < rows.Count; s++)
                if (rectOf(rows[s]).gameObject == go) return bound[s];
            return -1;
        }

        /// <summary><paramref name="n"/> rows inserted at <paramref name="at"/> in the host's data: the view keeps its rows in place, and the rows are bound again.</summary>
        public void Inserted(int at, int n)
        {
            Model.Inserted(at, n);
            Rebind();
        }

        /// <summary><paramref name="n"/> rows removed from <paramref name="at"/> in the host's data: the view keeps its rows in place, and the rows are bound again.</summary>
        public void Removed(int at, int n)
        {
            Model.Removed(at, n);
            Rebind();
        }

        /// <summary>The first row built (the one measured), or null.</summary>
        public TRow FirstRow => rows.Count > 0 ? rows[0] : null;

        /// <summary>Rows built so far (the pool): the point of the list is that this stays near the rows that fit.</summary>
        public int PoolCount => rows.Count;

        public int Count => Model.Count;

        /// <summary>The row showing <paramref name="index"/>, or null when that index isn't rendered.</summary>
        public TRow RowAt(int index)
        {
            if (index < Model.RenderFirst || index > Model.RenderLast || poolUsed == 0) return null;
            int s = index % poolUsed;
            return s < bound.Count && bound[s] == index ? rows[s] : null;
        }

        /// <summary>The index a row shows (-1: none).</summary>
        public int IndexOf(TRow row)
        {
            for (int s = 0; s < rows.Count; s++)
                if (ReferenceEquals(rows[s], row)) return bound[s];
            return -1;
        }

        /// <summary>A new row count. The first visible row stays where it is if it still exists (see <see cref="VirtualListModel"/>).</summary>
        public void SetCount(int count)
        {
            BindVersion++;
            Model.SetCount(count);
            Sync();
        }

        /// <summary>The rows show other data (a new filter): bind every rendered row again, and measure them again if they have their own heights.</summary>
        public void Rebind()
        {
            BindVersion++;
            for (int s = 0; s < bound.Count; s++) bound[s] = -1;
            if (heightAtWidth != null && measuredWidth > 0) Model.RefreshHeights();
            Sync();
        }

        /// <summary>
        /// Each row's own height: <paramref name="height"/>(index, width) at the content's width, asked for every row when the list
        /// is measured (its first layout, a restyle, a width change), when the count changes and on <see cref="Rebind"/>, never per
        /// scroll. The host caches what is costly to measure. Null: every row as high as the first.
        /// </summary>
        public void SetRowHeights(Func<int, float, float> height)
        {
            heightAtWidth = height;
            if (height == null) Model.SetRowHeights(null);
            measure = true;
            Sync();
        }

        /// <summary>Scrolls row <paramref name="index"/> into view (for the program, a key or an agent).</summary>
        public void ScrollTo(int index, ScrollAlign align, ChangeSource source = ChangeSource.Program)
        {
            if (Unmeasured)
            {
                pending = true;
                pendingIndex = index;
                pendingAlign = align;
                pendingSource = source;
                return;
            }
            pending = false;
            Model.ScrollTo(index, align, source);
            PushOffset();
            Sync();
        }

        /// <summary>Scrolls to an offset from the content's top, clamped.</summary>
        public void SetOffset(float offset, ChangeSource source = ChangeSource.Program)
        {
            if (Unmeasured)
            {
                pending = true;
                pendingIndex = -1;
                pendingOffset = offset;
                pendingSource = source;
                return;
            }
            pending = false;
            Model.SetOffset(offset, source);
            PushOffset();
            Sync();
        }

        /// <summary>Makes row <paramref name="index"/> (-1: none) taller by <paramref name="extra"/> (an editor under the selected row).</summary>
        public void SetExpanded(int index, float extra)
        {
            Model.SetExpanded(index, extra);
            Sync();
        }

        /// <summary>A label shown at the top of the list while it has no row ("No item matches.").</summary>
        public UIText EmptyLabel(string textOrKey)
        {
            UIText t = KodeWindow.Label(Content, textOrKey);
            RectTransform r = t.rectTransform;
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 1);
            r.offsetMin = new Vector2(8, 0);
            r.offsetMax = new Vector2(-8, 0);
            KodeWindow.Styled(t.gameObject, th => r.sizeDelta = new Vector2(r.sizeDelta.x, TextMeasure.Measure("X", th).y + 4));
            empty = t.gameObject;
            empty.SetActive(Model.Count == 0);
            return t;
        }

        void OnScrolled(Vector2 _)
        {
            float y = content.anchoredPosition.y;
            if (Math.Abs(y - Model.Offset) < 0.01f) return;
            Model.SetOffset(y, KodeWindow.Pointer);
            Sync();
        }

        void PushOffset()
        {
            Vector2 p = content.anchoredPosition;
            if (Math.Abs(p.y - Model.Offset) < 0.01f) return;
            content.anchoredPosition = new Vector2(p.x, Model.Offset);
            View.ScrollRect.StopMovement();
        }

        /// <summary>Once a frame, and after every change: measure, then place and bind the rows in view.</summary>
        void Sync()
        {
            float vh = viewport.rect.height;
            if (vh > 0) Model.SetViewportHeight(vh);
            float width = content.rect.width;
            if (width > 0 && Math.Abs(width - measuredWidth) > 0.5f) measure = true;
            if (measure && Model.Count > 0 && width > 0) Measure(width);
            if (pending && !Unmeasured)
            {
                pending = false;
                if (pendingIndex >= 0) Model.ScrollTo(pendingIndex, pendingAlign, pendingSource);
                else Model.SetOffset(pendingOffset, pendingSource);
                PushOffset();
            }

            if (empty != null && empty.activeSelf != (Model.Count == 0)) empty.SetActive(Model.Count == 0);

            bool geometry = laidVersion != Model.GeometryVersion;
            if (geometry)
            {
                laidVersion = Model.GeometryVersion;
                float h = Model.ContentHeight;
                if (Math.Abs(content.sizeDelta.y - h) > 0.01f) content.sizeDelta = new Vector2(content.sizeDelta.x, h);
                PushOffset();
            }

            int pool = Model.PoolSize;
            if (pool != poolUsed)
            {
                // another slot for each index: bind all again
                poolUsed = pool;
                for (int s = 0; s < bound.Count; s++) bound[s] = -1;
            }
            while (rows.Count < pool) AddRow();

            // hide the rows that left the view
            int first = Model.RenderFirst, last = Model.RenderLast;
            for (int s = 0; s < rows.Count; s++)
            {
                int i = s < pool ? bound[s] : -1;
                if (s >= pool || i < first || i > last)
                {
                    bound[s] = -1;
                    GameObject go = rectOf(rows[s]).gameObject;
                    if (go.activeSelf) go.SetActive(false);
                }
            }
            for (int i = first; i <= last; i++)
            {
                int s = i % pool;
                TRow row = rows[s];
                RectTransform r = rectOf(row);
                if (bound[s] != i)
                {
                    if (!r.gameObject.activeSelf) r.gameObject.SetActive(true);
                    bound[s] = i;
                    bind(row, i);
                    RowBound?.Invoke(r.gameObject, i);
                    placedTop[s] = float.NaN;
                }
                float top = Model.RowTop(i), height = Model.HeightOf(i);
                if (placedTop[s] != top || placedHeight[s] != height)
                {
                    placedTop[s] = top;
                    placedHeight[s] = height;
                    r.anchoredPosition = new Vector2(0, -top);
                    r.sizeDelta = new Vector2(0, height);
                }
            }
        }

        TRow AddRow()
        {
            TRow row = create(Content);
            RectTransform r = rectOf(row);
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(1, 1);
            r.pivot = new Vector2(0.5f, 1);
            r.gameObject.SetActive(false);
            rows.Add(row);
            bound.Add(-1);
            placedTop.Add(float.NaN);
            placedHeight.Add(float.NaN);
            RowCreated?.Invoke(r.gameObject);
            return row;
        }

        // the rows' height: the preferred height of a row showing index 0, then each row's own if the host gives them
        void Measure(float width)
        {
            measure = false;
            measuredWidth = width;
            if (rows.Count == 0) AddRow();
            TRow row = rows[0];
            RectTransform r = rectOf(row);
            bool wasActive = r.gameObject.activeSelf;
            if (!wasActive) r.gameObject.SetActive(true);
            bind(row, 0);
            bound[0] = 0;
            placedTop[0] = float.NaN;
            LayoutRebuilder.ForceRebuildLayoutImmediate(r);
            float h = LayoutUtility.GetPreferredHeight(r);
            if (h > 0) Model.SetRowHeight(h);
            // slot 0 is placed (or hidden) by the sync that follows
            if (poolUsed == 0) bound[0] = -1;
            for (int s = 1; s < bound.Count; s++) placedTop[s] = float.NaN;
            if (heightAtWidth != null) Model.SetRowHeights(heightOf);
        }
    }

    /// <summary>What a drag between virtual lists needs of one (<see cref="ListDrag"/>), whatever its row type.</summary>
    public interface IVirtualList
    {
        VirtualListModel Model { get; }
        ScrollRect ScrollRect { get; }
        RectTransform ContentRect { get; }
        RectTransform ViewportRect { get; }
        IEnumerable<GameObject> RowObjects { get; }
        event Action<GameObject> RowCreated;
        int IndexOfObject(GameObject go);
        void SetOffset(float offset, ChangeSource source = ChangeSource.Program);
    }

    /// <summary>Factories for <see cref="VirtualList{TRow}"/>.</summary>
    public static class VirtualList
    {
        /// <summary>
        /// A virtual list of ListRows (KodeWindow.ListRow): flat clickable rows of text, which wraps as in a ScrollList, each row as
        /// high as its text at the list's width (measured once per text with a hidden text, cached until a restyle or a width
        /// change). <paramref name="text"/> gives index i's text and <paramref name="click"/> gets the index clicked.
        /// </summary>
        /// <param name="bound">When set, called after a row is bound to an index (a selected look).</param>
        public static VirtualList<UIButton> ListRows(KodeWindow w, UIObject parent, float height, int count, Func<int, string> text, Action<int> click,
            Action<UIButton, int> bound = null)
        {
            VirtualList<UIButton> list = null;
            list = new VirtualList<UIButton>(w, parent, height, count, content =>
            {
                UIButton b = null;
                b = KodeWindow.ListRow(content, "", () =>
                {
                    int i = list.IndexOf(b);
                    if (i >= 0) click(i);
                });
                return b;
            }, b => b.rectTransform, (b, i) =>
            {
                b.Text(text(i));
                bound?.Invoke(b, i);
            });

            // a row's height is its text's wrapped height plus the row's padding, found from row 0 (the list's own measure)
            var heights = new Dictionary<string, float>();
            float atWidth = -1, padding = 0, wrap = 0;
            int atStyle = -1;
            Theme theme = null;
            list.SetRowHeights((i, width) =>
            {
                if (width != atWidth || list.StyleVersion != atStyle)
                {
                    atWidth = width;
                    atStyle = list.StyleVersion;
                    heights.Clear();
                    theme = KodeWindow.ThemeOf(list.View.gameObject);
                    UIButton first = list.RowAt(0) ?? list.FirstRow;
                    RectOffset pad = first != null ? first.Content.layoutGroup.padding : null;
                    wrap = Math.Max(1, width - (pad != null ? pad.horizontal : 16) - Lead(first));
                    padding = list.Model.RowHeight - TextMeasure.Measure(text(0) ?? "", theme, wrap).y;
                }
                string t = text(i) ?? "";
                if (heights.TryGetValue(t, out float h)) return h;
                h = TextMeasure.Measure(t, theme, wrap).y + padding;
                heights[t] = h;
                return h;
            });
            return list;
        }

        // what a row's text shares its line with: the width of the row's other parts (a toggle column's check box) and their spacing
        static float Lead(UIButton row)
        {
            if (row == null || row.ChildText == null) return 0;
            Transform content = row.Content.transform, text = row.ChildText.transform;
            float lead = 0, spacing = row.Content.layoutGroup is HorizontalOrVerticalLayoutGroup g ? g.spacing : 0;
            for (int i = 0; i < content.childCount; i++)
            {
                Transform c = content.GetChild(i);
                if (c == text || !c.gameObject.activeSelf) continue;
                lead += LayoutUtility.GetPreferredWidth((RectTransform)c) + spacing;
            }
            return lead;
        }
    }
}
