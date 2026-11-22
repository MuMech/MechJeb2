#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>A check box's three states: a branch is <see cref="Some"/> when only part of its leaves are checked.</summary>
    public enum CheckState { None, Some, All }

    /// <summary>
    /// A tree's state by node key (which branches are open, which nodes are checked, the open branches from before a search):
    /// kept apart from the nodes so it outlives a <see cref="TreeCollection{T}"/> built again with the same keys (a view rebuilt).
    /// </summary>
    [NotAWidget("data: a tree's open and checked keys, kept across rebuilds")]
    public sealed class TreeState
    {
        public readonly HashSet<string> Open = new HashSet<string>(StringComparer.Ordinal);
        public readonly HashSet<string> Checked = new HashSet<string>(StringComparer.Ordinal);

        // the open branches when the search started; null outside a search
        internal HashSet<string> BeforeSearch;
    }

    /// <summary>One node of a <see cref="TreeCollection{T}"/>: a branch (it has children, or was added as one) or a leaf.</summary>
    [NotAWidget("data: one node of a TreeCollection")]
    public sealed class TreeNode<T>
    {
        internal readonly List<TreeNode<T>> children = new List<TreeNode<T>>();
        internal readonly bool branch;

        internal TreeNode(string key, string label, T value, TreeNode<T> parent, string searchKey, bool branch)
        {
            Key = key;
            Label = label;
            Value = value;
            Parent = parent;
            SearchKey = searchKey;
            this.branch = branch;
            Depth = parent == null ? 0 : parent.Depth + 1;
        }

        /// <summary>The node's stable key: the state (open, checked) is kept by it.</summary>
        public string Key { get; }

        /// <summary>What the user sees, and what a search matches first.</summary>
        public string Label { get; }

        /// <summary>A second name a search also matches (an English name under a localized label), or null.</summary>
        public string SearchKey { get; }

        public T Value { get; }
        public TreeNode<T> Parent { get; }
        public int Depth { get; }
        public IReadOnlyList<TreeNode<T>> Children => children;

        /// <summary>True when the node has children or was added with <see cref="TreeCollection{T}.AddBranch"/>.</summary>
        public bool IsBranch => branch || children.Count > 0;

        /// <summary>During a search: true for a leaf that matches it.</summary>
        public bool Matched { get; internal set; }

        /// <summary>The leaves under this branch (itself, for a leaf).</summary>
        public int LeafCount { get; internal set; }

        /// <summary>During a search, the matching leaves under this branch (itself, for a matching leaf); outside one, <see cref="LeafCount"/>.</summary>
        public int MatchCount { get; internal set; }

        public override string ToString() => Key;
    }

    /// <summary>A visible row of a tree: its node, its depth, whether it can fold (a branch with a visible child) and whether it is open.</summary>
    public struct TreeRow<T>
    {
        public TreeNode<T> Node;
        public int Depth;
        public bool Expandable;
        public bool Open;
    }

    /// <summary>
    /// A tree of nodes shown as a flat list of visible rows, without Unity (ui-sdk's <c>createTreeCollection</c>): KodeKit's
    /// <c>TreeList</c> draws <see cref="VisibleRows"/> on a <c>VirtualList</c>, a row per visible node, indented by its depth.
    /// <list type="bullet">
    /// <item>Nodes are added parent first (<see cref="Add"/>, <see cref="AddBranch"/>), each with a key unique in the tree. The state is
    /// kept by key in a <see cref="TreeState"/>, so a collection built again with the same keys (and the same state) keeps it.</item>
    /// <item><see cref="VisibleRows"/> lists the roots and, under each open branch, its children, depth first in the order they were
    /// added (<see cref="MoveChild"/> changes it). A closed branch hides all its descendants, whatever their own state.</item>
    /// <item><see cref="Open"/>, <see cref="Close"/>, <see cref="Toggle"/> act on one branch only (a leaf, or a branch with no visible
    /// child, does nothing); <see cref="OpenAll"/> and <see cref="CloseAll"/> on every branch; <see cref="Reveal"/> opens a node's ancestors.</item>
    /// <item><see cref="Find"/> searches the leaves (label, then search key, with <see cref="FuzzyMatcher"/>): only matching leaves stay
    /// visible, every branch with a match opens and every other branch closes (they stay listed, with nothing to show). An empty query
    /// ends the search: every leaf shows again and the branches open before the search are open again.</item>
    /// <item><see cref="Check"/> sets a node and all its descendants. A branch's state is computed from its leaves: all, some or none
    /// checked (<see cref="StateOf"/>); a branch without leaves has its own.</item>
    /// <item>Every change bumps <see cref="Version"/>: a view compares it once a frame. Reading <see cref="VisibleRows"/> rebuilds the
    /// list only after a change, and allocates nothing once its buffer fits.</item>
    /// </list>
    /// </summary>
    public sealed class TreeCollection<T> : IDescribeParts
    {
        /// <summary>What the tree is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The tree and each visible row, by key (a tree item: expanded when open; on or mixed by its check, when it has one).</summary>
        public void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription { Name = "tree", Role = PartRole.Tree, Label = Label });
            IReadOnlyList<TreeRow<T>> visible = VisibleRows;
            for (int i = 0; i < visible.Count; i++)
            {
                TreeRow<T> r = visible[i];
                CheckState check = StateOf(r.Node.Key);
                PartFlags f = r.Open ? PartFlags.Expanded : PartFlags.None;
                if (check == CheckState.All) f |= PartFlags.On;
                else if (check == CheckState.Some) f |= PartFlags.Mixed;
                into.Add(new PartDescription { Name = "node." + r.Node.Key, Role = PartRole.TreeItem, Label = r.Node.Label, Flags = f });
            }
        }

        readonly List<TreeNode<T>> nodes = new List<TreeNode<T>>();
        readonly List<TreeNode<T>> roots = new List<TreeNode<T>>();
        readonly Dictionary<string, TreeNode<T>> byKey = new Dictionary<string, TreeNode<T>>(StringComparer.Ordinal);
        readonly List<TreeRow<T>> rows = new List<TreeRow<T>>();
        readonly FuzzyMatcher matcher = new FuzzyMatcher();
        int rowsVersion = -1;
        bool countsValid;

        public TreeCollection(TreeState state = null) => State = state ?? new TreeState();

        public TreeState State { get; }

        /// <summary>Bumped by every change of the nodes, the open branches, the checks or the search.</summary>
        public int Version { get; private set; }

        public int Count => nodes.Count;
        public IReadOnlyList<TreeNode<T>> Roots => roots;

        /// <summary>The applied search ("" outside one).</summary>
        public string Query { get; private set; } = "";

        public bool Searching => Query.Length > 0;

        /// <summary>The leaves the search matches (every leaf outside a search).</summary>
        public int MatchCount
        {
            get
            {
                Counts();
                int n = 0;
                for (int i = 0; i < roots.Count; i++) n += roots[i].MatchCount;
                return n;
            }
        }

        public bool Contains(string key) => key != null && byKey.ContainsKey(key);

        /// <summary>The node with <paramref name="key"/>, or null.</summary>
        public TreeNode<T> Get(string key) => key != null && byKey.TryGetValue(key, out TreeNode<T> n) ? n : null;

        /// <summary>Adds a node under <paramref name="parentKey"/> (null: a root). A node with children is a branch.</summary>
        public TreeNode<T> Add(string key, string label, T value, string parentKey = null, string searchKey = null) =>
            AddNode(key, label, value, parentKey, searchKey, false);

        /// <summary>Adds a branch: it stays one when it has no child (an empty category).</summary>
        public TreeNode<T> AddBranch(string key, string label, T value = default(T), string parentKey = null) =>
            AddNode(key, label, value, parentKey, null, true);

        TreeNode<T> AddNode(string key, string label, T value, string parentKey, string searchKey, bool branch)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (byKey.ContainsKey(key)) throw new ArgumentException("a key is unique in its tree: " + key, nameof(key));
            TreeNode<T> parent = null;
            if (parentKey != null && !byKey.TryGetValue(parentKey, out parent))
                throw new ArgumentException("a parent is added before its children: " + parentKey, nameof(parentKey));
            var node = new TreeNode<T>(key, label ?? "", value, parent, searchKey, branch);
            nodes.Add(node);
            byKey[key] = node;
            if (parent == null) roots.Add(node);
            else parent.children.Add(node);
            if (Searching) Score(node);
            Changed();
            return node;
        }

        /// <summary>Removes every node; the state (by key) stays, for nodes added again with the same keys.</summary>
        public void Clear()
        {
            nodes.Clear();
            roots.Clear();
            byKey.Clear();
            Changed();
        }

        void Changed()
        {
            Version++;
            countsValid = false;
        }

        // ---- folding ----

        public bool IsOpen(string key) => key != null && State.Open.Contains(key);

        /// <summary>Opens a branch; false when nothing changed (a leaf, a branch with no visible child, already open).</summary>
        public bool Open(string key)
        {
            TreeNode<T> n = Get(key);
            if (n == null || !Expandable(n) || !State.Open.Add(key)) return false;
            Changed();
            return true;
        }

        /// <summary>Closes a branch; false when nothing changed.</summary>
        public bool Close(string key)
        {
            if (Get(key) == null || !State.Open.Remove(key)) return false;
            Changed();
            return true;
        }

        /// <summary>Opens a closed branch or closes an open one; false when nothing changed.</summary>
        public bool Toggle(string key) => IsOpen(key) ? Close(key) : Open(key);

        /// <summary>Opens every branch that has a visible child.</summary>
        public void OpenAll()
        {
            bool changed = false;
            for (int i = 0; i < nodes.Count; i++)
                if (Expandable(nodes[i]) && State.Open.Add(nodes[i].Key))
                    changed = true;
            if (changed) Changed();
        }

        /// <summary>Closes every branch.</summary>
        public void CloseAll()
        {
            if (State.Open.Count == 0) return;
            State.Open.Clear();
            Changed();
        }

        /// <summary>Opens the ancestors of <paramref name="key"/>, so its row is visible (if the search shows it).</summary>
        public void Reveal(string key)
        {
            TreeNode<T> n = Get(key);
            bool changed = false;
            for (TreeNode<T> p = n?.Parent; p != null; p = p.Parent)
                if (State.Open.Add(p.Key))
                    changed = true;
            if (changed) Changed();
        }

        // a node shows (when its ancestors are open): every branch, and a leaf outside a search or matching it
        bool Shown(TreeNode<T> n) => n.IsBranch || !Searching || n.Matched;

        /// <summary>True for a branch with at least one child that shows.</summary>
        public bool Expandable(TreeNode<T> n)
        {
            if (n == null) return false;
            for (int i = 0; i < n.children.Count; i++)
                if (Shown(n.children[i]))
                    return true;
            return false;
        }

        // ---- the visible rows ----

        /// <summary>The rows to draw, top to bottom (the virtual list's count is their count). Owned by the collection: valid until the next change.</summary>
        public IReadOnlyList<TreeRow<T>> VisibleRows
        {
            get
            {
                if (rowsVersion != Version)
                {
                    rowsVersion = Version;
                    rows.Clear();
                    for (int i = 0; i < roots.Count; i++) AddRows(roots[i]);
                }
                return rows;
            }
        }

        void AddRows(TreeNode<T> n)
        {
            if (!Shown(n)) return;
            bool expandable = Expandable(n), open = expandable && State.Open.Contains(n.Key);
            rows.Add(new TreeRow<T> { Node = n, Depth = n.Depth, Expandable = expandable, Open = open });
            if (!open) return;
            for (int i = 0; i < n.children.Count; i++) AddRows(n.children[i]);
        }

        /// <summary>The visible row of <paramref name="key"/>, or -1 (hidden, or no such node).</summary>
        public int RowOf(string key)
        {
            IReadOnlyList<TreeRow<T>> r = VisibleRows;
            for (int i = 0; i < r.Count; i++)
                if (string.Equals(r[i].Node.Key, key, StringComparison.Ordinal))
                    return i;
            return -1;
        }

        // ---- search ----

        /// <summary>
        /// Searches the leaves for <paramref name="query"/>: opens the branches holding a match (and their ancestors), closes every
        /// other one, and hides the leaves that don't match. An empty query ends the search and opens again the branches open before
        /// it. Returns the matching leaves (every leaf for an empty query).
        /// </summary>
        public int Find(string query)
        {
            query = query ?? "";
            matcher.Query = query;
            if (matcher.IsEmpty)
            {
                if (Searching || State.BeforeSearch != null)
                {
                    Query = "";
                    if (State.BeforeSearch != null)
                    {
                        State.Open.Clear();
                        State.Open.UnionWith(State.BeforeSearch);
                        State.BeforeSearch = null;
                    }
                    for (int i = 0; i < nodes.Count; i++) nodes[i].Matched = false;
                    Changed();
                }
                return MatchCount;
            }

            if (State.BeforeSearch == null) State.BeforeSearch = new HashSet<string>(State.Open, StringComparer.Ordinal);
            Query = query;
            for (int i = 0; i < nodes.Count; i++) Score(nodes[i]);
            Changed();
            Counts();
            State.Open.Clear();
            for (int i = 0; i < nodes.Count; i++)
                if (nodes[i].IsBranch && nodes[i].MatchCount > 0)
                    State.Open.Add(nodes[i].Key);
            return MatchCount;
        }

        // a leaf matches by its label, or its search key (a label wins a tie, as in FuzzyFilter)
        void Score(TreeNode<T> n)
        {
            if (n.IsBranch)
            {
                n.Matched = false;
                return;
            }
            int s = matcher.Score(n.Label);
            if (s == FuzzyMatcher.NoMatch && n.SearchKey != null)
            {
                int k = matcher.Score(n.SearchKey);
                if (k != FuzzyMatcher.NoMatch) s = k - FuzzyFilter.KeyPenalty;
            }
            n.Matched = s != FuzzyMatcher.NoMatch;
        }

        // the leaf and match counts, children before parents: a parent is added before its children, so backwards
        void Counts()
        {
            if (countsValid) return;
            countsValid = true;
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                TreeNode<T> n = nodes[i];
                if (!n.IsBranch)
                {
                    n.LeafCount = 1;
                    n.MatchCount = !Searching || n.Matched ? 1 : 0;
                    continue;
                }
                int leaves = 0, matches = 0;
                for (int c = 0; c < n.children.Count; c++)
                {
                    leaves += n.children[c].LeafCount;
                    matches += n.children[c].MatchCount;
                }
                n.LeafCount = leaves;
                n.MatchCount = matches;
            }
        }

        /// <summary>Recounts <see cref="TreeNode{T}.LeafCount"/> and <see cref="TreeNode{T}.MatchCount"/> if a change made them stale.</summary>
        public void UpdateCounts() => Counts();

        // ---- checks ----

        /// <summary>Checks (or unchecks) a node and all its descendants.</summary>
        public void Check(string key, bool on)
        {
            TreeNode<T> n = Get(key);
            if (n == null) return;
            SetChecked(n, on);
            Changed();
        }

        void SetChecked(TreeNode<T> n, bool on)
        {
            if (on) State.Checked.Add(n.Key);
            else State.Checked.Remove(n.Key);
            for (int i = 0; i < n.children.Count; i++) SetChecked(n.children[i], on);
        }

        /// <summary>A click on a check box: a node all checked unchecks, any other (none or some) checks all.</summary>
        public void ToggleCheck(string key) => Check(key, StateOf(key) != CheckState.All);

        /// <summary>A leaf's state, or a branch's from its leaves: all, some or none checked (a branch without leaves: its own).</summary>
        public CheckState StateOf(string key)
        {
            TreeNode<T> n = Get(key);
            if (n == null) return CheckState.None;
            int leaves = 0, on = 0;
            CountChecked(n, ref leaves, ref on);
            if (leaves == 0) return State.Checked.Contains(n.Key) ? CheckState.All : CheckState.None;
            return on == 0 ? CheckState.None : on == leaves ? CheckState.All : CheckState.Some;
        }

        void CountChecked(TreeNode<T> n, ref int leaves, ref int on)
        {
            if (!n.IsBranch)
            {
                leaves++;
                if (State.Checked.Contains(n.Key)) on++;
                return;
            }
            for (int i = 0; i < n.children.Count; i++) CountChecked(n.children[i], ref leaves, ref on);
        }

        // ---- order ----

        /// <summary>Moves child <paramref name="from"/> of <paramref name="parentKey"/> (null: the roots) to index <paramref name="to"/>, with its subtree.</summary>
        public bool MoveChild(string parentKey, int from, int to)
        {
            List<TreeNode<T>> list;
            if (parentKey == null) list = roots;
            else
            {
                TreeNode<T> p = Get(parentKey);
                if (p == null) return false;
                list = p.children;
            }
            if (from < 0 || from >= list.Count) return false;
            to = Math.Max(0, Math.Min(list.Count - 1, to));
            if (to == from) return false;
            TreeNode<T> n = list[from];
            list.RemoveAt(from);
            list.Insert(to, n);
            Changed();
            return true;
        }

        /// <summary>The index of <paramref name="n"/> among its siblings.</summary>
        public int SiblingIndex(TreeNode<T> n) => n == null ? -1 : (n.Parent == null ? roots : n.Parent.children).IndexOf(n);
    }

    /// <summary>
    /// Drag to reorder a tree's rows among their siblings (ui-sdk's <c>createTreeReorder</c>, within a parent only), on a
    /// <see cref="ReorderModel"/> over the tree's visible rows: KodeKit's <c>ListDrag.ForReorder</c> draws it with <see cref="Model"/>.
    /// <list type="bullet">
    /// <item>A row moves among the visible rows of its own parent: every row of another parent is a fixed row, so the gap stops at
    /// the nearest one. An open sibling branch's children are such rows: close it to move a row past it.</item>
    /// <item>A branch moves with its subtree (<see cref="TreeCollection{T}.MoveChild"/>).</item>
    /// <item>No reorder during a search: hidden leaves would make a visible move another move among the children.</item>
    /// <item><see cref="Move"/> is the same move without a drag (an agent, a button), with the same rules.</item>
    /// </list>
    /// </summary>
    [NotAWidget("a drag gesture on a tree's siblings (the tree describes its rows)")]
    public sealed class TreeReorder<T>
    {
        readonly TreeCollection<T> tree;
        int anchor = -1;

        /// <summary>Called after a move: the node, its old and its new index among its siblings.</summary>
        public event Action<TreeNode<T>, int, int> Moved;

        public TreeReorder(TreeCollection<T> tree)
        {
            this.tree = tree ?? throw new ArgumentNullException(nameof(tree));
            Model = new ReorderModel(() => tree.VisibleRows.Count, MoveRows) { IsFixed = IsWall };
        }

        public ReorderModel Model { get; }

        // a row of another parent than the moved one is a wall; during a search every row is
        bool IsWall(int i)
        {
            if (tree.Searching) return true;
            int moved = Model.Dragging ? Model.From : anchor;
            if (moved < 0) return false;
            IReadOnlyList<TreeRow<T>> rows = tree.VisibleRows;
            return rows[i].Node.Parent != rows[moved].Node.Parent;
        }

        /// <summary>Moves visible row <paramref name="row"/> to visible index <paramref name="to"/> (clamped to its siblings); false when nothing moved.</summary>
        public bool Move(int row, int to, ChangeSource source)
        {
            anchor = row;
            try
            {
                return Model.Move(row, to, source);
            }
            finally
            {
                anchor = -1;
            }
        }

        // the walls keep the move among single-row siblings, so a row offset is a sibling offset
        void MoveRows(int from, int to)
        {
            TreeNode<T> n = tree.VisibleRows[from].Node;
            int ci = tree.SiblingIndex(n), co = ci + (to - from);
            if (tree.MoveChild(n.Parent?.Key, ci, co)) Moved?.Invoke(n, ci, co);
        }
    }
}
