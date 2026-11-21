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
    /// A tree drawn from a <see cref="TreeCollection{T}"/> on a <see cref="VirtualList{TRow}"/>: one ListRow per visible row,
    /// indented by its depth, a branch with a fold chevron (down: open, right: closed) and its label in the header colour. Only the
    /// rows in view are built.
    /// <list type="bullet">
    /// <item>A click on a branch's row folds it (<see cref="Fold"/>): the rows under it are inserted or removed below it, the rows
    /// in view stay where they are. A click on a leaf calls the host's <c>leafClick</c>.</item>
    /// <item>No key folds or moves: the tree's keys (Left, Right, Up, Down, <c>*</c>) are KSP's flight and camera keys, and a list
    /// row holds no keyboard lock. Keyboard and agent folds call the model (<see cref="TreeCollection{T}.Toggle"/>); the list follows
    /// the model's <see cref="TreeCollection{T}.Version"/> once a frame.</item>
    /// <item>After a search (<see cref="Find"/>) the list shows the results from the top; while a search matches nothing it has
    /// no row, so its <see cref="VirtualList{TRow}.EmptyLabel"/> shows.</item>
    /// <item>Each row is as high as its text wrapped at the list's width less its indent (measured once per text and depth,
    /// cached until a restyle or a width change).</item>
    /// <item>A row's index changes with every fold: a handler asks <see cref="VirtualList{TRow}.IndexOf"/> at the click.</item>
    /// </list>
    /// KodeUI's <c>TreeView</c> (a copy of KodeUI's sources, unused by MechJeb) is not this tree's view: it builds a row per item,
    /// takes its look from KodeUI skin styles that KodeKit's themes don't set, and KodeUI compiles alone, without KodeKit's models.
    /// </summary>
    public sealed class TreeList<T>
    {
        sealed class Parts
        {
            public Image Chevron;
            public LayoutElement ChevronLayout;
            public TMP_Text Label;
            public RectOffset Padding;
            public bool Branch;
        }

        const int BasePadding = 8;

        public readonly TreeCollection<T> Tree;
        public readonly VirtualList<UIButton> List;

        readonly Func<TreeRow<T>, string> text;
        readonly Action<TreeNode<T>, int> leafClick;
        readonly Dictionary<UIButton, Parts> parts = new Dictionary<UIButton, Parts>();
        int shownVersion;
        Theme theme;
        float chevronSize = 10, spacing;

        /// <summary>Called after a click folded a branch (the rows below it moved): a drag on the list's rows ends here.</summary>
        public event Action Folded;

        /// <param name="height">The list's height, as <see cref="KodeWindow.ScrollList"/>'s.</param>
        /// <param name="text">A visible row's text.</param>
        /// <param name="leafClick">A click on a leaf: its node and its visible row.</param>
        public TreeList(KodeWindow w, UIObject parent, float height, TreeCollection<T> tree, Func<TreeRow<T>, string> text, Action<TreeNode<T>, int> leafClick)
        {
            Tree = tree ?? throw new ArgumentNullException(nameof(tree));
            // its description's label: the window's title unless the host named the tree
            if (string.IsNullOrEmpty(tree.Label)) tree.Label = w.Behaviour.Frame.Label;
            this.text = text ?? throw new ArgumentNullException(nameof(text));
            this.leafClick = leafClick;
            shownVersion = tree.Version;
            List = new VirtualList<UIButton>(w, parent, height, RowCount, Create, b => b.rectTransform, Bind);
            KodeWindow.Styled(List.View.gameObject, th =>
            {
                theme = th;
                float size = Mathf.Round(th.FontSize * 0.75f);
                if (size == chevronSize) return;
                chevronSize = size;
                // a new indent: every row's padding again
                List.Rebind();
            });

            // a row's height is its text's wrapped height (at the width its indent leaves) plus the row's padding, found from row 0
            var heights = new List<Dictionary<string, float>>();
            float atWidth = -1, padding = 0;
            int atStyle = -1;
            List.SetRowHeights((i, width) =>
            {
                IReadOnlyList<TreeRow<T>> rows = Tree.VisibleRows;
                if (i >= rows.Count) return List.Model.RowHeight;
                if (width != atWidth || List.StyleVersion != atStyle)
                {
                    atWidth = width;
                    atStyle = List.StyleVersion;
                    foreach (Dictionary<string, float> d in heights) d.Clear();
                    theme = KodeWindow.ThemeOf(List.View.gameObject);
                    padding = List.Model.RowHeight - TextMeasure.Measure(text(rows[0]) ?? "", theme, WrapWidth(rows[0], width)).y;
                }
                TreeRow<T> r = rows[i];
                int slot = r.Depth * 2 + (r.Node.IsBranch ? 1 : 0);
                while (heights.Count <= slot) heights.Add(new Dictionary<string, float>());
                string t = text(r) ?? "";
                if (heights[slot].TryGetValue(t, out float h)) return h;
                h = TextMeasure.Measure(t, theme, WrapWidth(r, width)).y + padding;
                heights[slot][t] = h;
                return h;
            });
            w.AddSync(Sync);
        }

        /// <summary>The rows the list shows: the visible rows, or none while a search matches nothing.</summary>
        public int RowCount => Tree.Searching && Tree.MatchCount == 0 ? 0 : Tree.VisibleRows.Count;

        float Indent => chevronSize + spacing;

        int LeftPadding(int depth) => BasePadding + Mathf.RoundToInt(depth * Indent);

        // the text's width in a row: the row less its padding and, on a branch, the chevron
        float WrapWidth(TreeRow<T> r, float width) =>
            Math.Max(1, width - LeftPadding(r.Depth) - BasePadding - (r.Node.IsBranch ? chevronSize + spacing : 0));

        UIButton Create(UIObject content)
        {
            UIButton b = null;
            b = KodeWindow.ListRow(content, "", () =>
            {
                int i = List.IndexOf(b);
                if (i >= 0) Click(i);
            });
            var go = new GameObject("Chevron", typeof(RectTransform), typeof(Image), typeof(LayoutElement)) { layer = b.gameObject.layer };
            go.transform.SetParent(b.Content.transform, false);
            go.transform.SetSiblingIndex(0);
            var p = new Parts
            {
                Chevron = go.GetComponent<Image>(),
                ChevronLayout = go.GetComponent<LayoutElement>(),
                Label = b.GetComponentInChildren<TMP_Text>(true),
                Padding = b.Content.layoutGroup.padding
            };
            p.Chevron.sprite = Sprites.ChevronDown;
            p.Chevron.raycastTarget = false;
            p.Chevron.preserveAspect = true;
            if (b.Content.layoutGroup is HorizontalOrVerticalLayoutGroup g) spacing = g.spacing;
            parts[b] = p;
            // after the ListRow's own restyle (which sets the label's colour): the chevron's size and colour, a branch's label colour
            KodeWindow.Styled(b.gameObject, th =>
            {
                float size = Mathf.Round(th.FontSize * 0.75f);
                p.ChevronLayout.minWidth = p.ChevronLayout.preferredWidth = p.ChevronLayout.minHeight = p.ChevronLayout.preferredHeight = size;
                p.Chevron.color = th.Header;
                if (p.Branch) p.Label.color = th.Header;
            });
            return b;
        }

        void Bind(UIButton b, int i)
        {
            IReadOnlyList<TreeRow<T>> rows = Tree.VisibleRows;
            if (i >= rows.Count || !parts.TryGetValue(b, out Parts p)) return;
            TreeRow<T> r = rows[i];
            p.Branch = r.Node.IsBranch;
            int left = LeftPadding(r.Depth);
            if (p.Padding.left != left)
            {
                p.Padding.left = left;
                LayoutRebuilder.MarkLayoutForRebuild(b.Content.rectTransform);
            }
            // a leaf has no chevron slot: its text lines up with its parent's
            if (p.ChevronLayout.ignoreLayout == p.Branch) p.ChevronLayout.ignoreLayout = !p.Branch;
            p.Chevron.enabled = r.Expandable;
            p.Chevron.rectTransform.localRotation = Quaternion.Euler(0, 0, r.Open ? 0 : 90);
            b.Text(text(r));
            Theme th = theme ?? KodeWindow.ThemeOf(b.gameObject);
            p.Label.color = p.Branch ? th.Header : th.Text;
        }

        void Click(int i)
        {
            IReadOnlyList<TreeRow<T>> rows = Tree.VisibleRows;
            if (i >= rows.Count) return;
            TreeRow<T> r = rows[i];
            if (r.Node.IsBranch) Fold(i);
            else leafClick?.Invoke(r.Node, i);
        }

        /// <summary>Opens or closes the branch at visible row <paramref name="row"/> (a click on it); false when nothing changed.</summary>
        public bool Fold(int row)
        {
            IReadOnlyList<TreeRow<T>> rows = Tree.VisibleRows;
            if (row < 0 || row >= rows.Count) return false;
            int before = RowCount;
            if (!Tree.Toggle(rows[row].Node.Key)) return false;
            int after = RowCount;
            shownVersion = Tree.Version;
            Folded?.Invoke();
            // the rows under the branch, inserted or removed below it: the rows in view keep their place
            if (after > before) List.Inserted(row + 1, after - before);
            else if (after < before) List.Removed(row + 1, before - after);
            else List.Rebind();
            return true;
        }

        /// <summary>A search (or its end, for an empty query): the tree filtered and folded by the model, the list shown from the top.</summary>
        public int Find(string query)
        {
            int found = Tree.Find(query);
            Refresh(true);
            return found;
        }

        /// <summary>The tree changed (a search, an agent's fold): the list takes its rows again, from the top when <paramref name="toTop"/>.</summary>
        public void Refresh(bool toTop)
        {
            shownVersion = Tree.Version;
            List.SetCount(RowCount);
            if (toTop) List.ScrollTo(0, ScrollAlign.Start);
            List.Rebind();
        }

        // once a frame: a change made through the model elsewhere (an agent) shows
        void Sync()
        {
            if (shownVersion != Tree.Version) Refresh(false);
        }
    }
}
