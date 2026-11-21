#if !CSHARP_7_3
#nullable disable
#endif
using System;
using System.Collections.Generic;

// No Unity here: this folder is compiled into MechJeb2.UI and, on its own, into MechJebLibTest, whose build would fail on
// a Unity type. Widgets' behaviour lives here and is tested without KSP; KodeKit's factories only draw it.
namespace KodeKit.Headless
{
    /// <summary>The keys a timeline understands, as its host reports them.</summary>
    public enum TimelineKey { Next, Previous, First, Last, Clear }

    /// <summary>What a click on a <see cref="TimelineModel"/> did.</summary>
    public enum TimelineClick { Nothing, Selected, Zoomed, Cleared }

    /// <summary>
    /// One item of a <see cref="TimelineModel"/>: a span (a burn, a coast, a range) or an instant (a stage separation, a mark), on
    /// one of the host's lanes. The host fills <see cref="Id"/> to <see cref="Tag"/> through <see cref="TimelineModel.Set"/>; the
    /// model sets the layout (<see cref="Row"/>, <see cref="DisplayRow"/>, <see cref="Group"/>).
    /// </summary>
    [NotAWidget("data: one item of a TimelineModel")]
    public sealed class TimelineItem
    {
        /// <summary>The host's id for it: the selection follows the id when the items are built again.</summary>
        public int Id { get; internal set; }

        public double Start { get; internal set; }

        /// <summary>The span's end; NaN for an instant.</summary>
        public double End { get; internal set; }

        /// <summary>The host's lane (a track of the strip: stages, burns, marks).</summary>
        public int Lane { get; internal set; }

        /// <summary>The host's kind: what it draws the item with (a colour, a mark).</summary>
        public int Kind { get; internal set; }

        /// <summary>The label's localization key (or its text).</summary>
        public string LabelKey { get; internal set; }

        /// <summary>A number the label shows (a Δv, a stage number); NaN for none.</summary>
        public double Value { get; internal set; }

        /// <summary>Anything the host keeps with it.</summary>
        public object Tag { get; internal set; }

        public bool IsInstant => double.IsNaN(End);

        /// <summary>Where it ends: its end, or its time for an instant.</summary>
        public double Finish => IsInstant ? Start : End;

        public double Width => Finish - Start;

        internal TimelineModel owner;
        internal int row, displayRow, group = -1, order, update;

        /// <summary>Its row within its lane (layout).</summary>
        public int Row
        {
            get
            {
                owner?.Layout();
                return row;
            }
        }

        /// <summary>Its row from the strip's top: the rows of the lanes above, then <see cref="Row"/> (layout).</summary>
        public int DisplayRow
        {
            get
            {
                owner?.Layout();
                return displayRow;
            }
        }

        /// <summary>The group it is drawn in at the current zoom (<see cref="TimelineModel.Group"/>), -1 when drawn alone.</summary>
        public int Group
        {
            get
            {
                owner?.Groups();
                return group;
            }
        }

        /// <summary>Its place in time order (<see cref="TimelineModel.Ordered"/>).</summary>
        public int Order
        {
            get
            {
                owner?.Layout();
                return order;
            }
        }
    }

    /// <summary>Items of one row drawn as one mark, too close to tell apart at the current zoom.</summary>
    [NotAWidget("data: the items a TimelineModel draws as one")]
    public sealed class TimelineGroup
    {
        internal readonly List<TimelineItem> items = new List<TimelineItem>();

        public int DisplayRow { get; internal set; }
        public double Start { get; internal set; }
        public double End { get; internal set; }

        /// <summary>The smallest gap between two of its items (0: two at the same time).</summary>
        public double SmallestGap { get; internal set; }

        public int Count => items.Count;

        /// <summary>Its items in time order.</summary>
        public TimelineItem Item(int i) => items[i];

        public bool Contains(TimelineItem item) => item != null && items.Contains(item);
    }

    /// <summary>
    /// Items along one axis of time (or distance), without Unity: the Flight Recorder's events, the maneuver nodes ahead, a PSG
    /// plan's phases. The axis is a <see cref="ViewportModel"/>, which maps data values to the strip (fractions, 0 at the window's
    /// start, 1 at its end) and pans and zooms; nothing here depends on the strip's size in pixels.
    /// <list type="bullet">
    /// <item>An item is a span (start and end, put in order however given) or an instant (end NaN), on a lane the host picks, with
    /// a kind and a label key, under an id the host gives. <see cref="Set"/> gives an id its values (adding its item the first
    /// time); between <see cref="BeginUpdate"/> and <see cref="EndUpdate"/> the items not set again are removed. An id keeps its
    /// item object while it stays, so building the items again allocates nothing for the ids already there, and changes
    /// <see cref="Version"/> only when a value changed. A non-finite start or an infinite end changes nothing (null).</item>
    /// <item>Layout: each lane is split into rows so that no two items of a row overlap. Items go in time order (start, then end,
    /// then id) to the first row of their lane that is free. Two items overlap when they share more than one instant, or when both
    /// are instants at the same time: a span that starts where another ends, or an instant at a span's end, shares its row. Lanes
    /// are drawn top to bottom in their order; a lane with no item keeps one row while <see cref="KeepEmptyLanes"/>, up to
    /// <see cref="LaneCount"/> (or the highest lane used). The layout is in data units, so a zoom never moves an item to another
    /// row.</item>
    /// <item>Grouping, at the current zoom: in a row, items narrower than <see cref="GroupFraction"/> of the window's span, each
    /// less than that from the previous one, are drawn as one group (two or more). A wide span is never grouped. Groups follow the
    /// span only: a pan doesn't change them.</item>
    /// <item>Selection: one item at most, kept by id (<see cref="SelectedId"/>): building the items again keeps it on the item with
    /// that id, and an id no item has selects nothing until it comes back. Every pick calls <see cref="Picked"/>, the item already
    /// selected included (the host acts on it: a point marker moved there again). Next and previous go in time order and stop at
    /// the ends; from no selection, next is the first item and previous the last. A selection by key or program that lies outside
    /// the window pans the view to it, keeping the zoom.</item>
    /// <item>The pointer: <see cref="HitTest"/> takes a fraction of the strip and a display row. In that row a group under the
    /// pointer (within <see cref="PickFraction"/>) wins, then the nearest instant within that distance, then a span under the
    /// pointer (or within that distance of a narrow one). A click (<see cref="Click"/>) on an item selects it; on a group, zooms
    /// in about the group's middle until its two closest items are drawn apart (at least by half), or, when they can't be (the
    /// same time, or closer than the view's narrowest span allows), selects its items in turn; on nothing, clears the
    /// selection.</item>
    /// <item>"Now": <see cref="Now"/> (NaN: none) is drawn as a marker; items that finish before it are past
    /// (<see cref="IsPast"/>), a span around it is current (<see cref="IsCurrent"/>). Moving it bumps <see cref="NowVersion"/>
    /// only, not the layout.</item>
    /// <item><see cref="Version"/> counts the changes of the items and the selection; the groups also follow the view's span.
    /// A non-finite argument changes nothing and returns false (or null).</item>
    /// </list>
    /// </summary>
    public sealed class TimelineModel : IDescribeParts
    {
        /// <summary>What the timeline is called, for <see cref="IDescribeParts"/> (null: the factory gave none).</summary>
        public string Label { get; set; }

        /// <summary>The timeline (a list box: a click picks an item) and each item, by id (an option: on when selected; its label key as its label).</summary>
        public void Describe(List<PartDescription> into)
        {
            into.Add(new PartDescription { Name = "timeline", Role = PartRole.ListBox, Label = Label });
            for (int i = 0; i < items.Count; i++)
            {
                TimelineItem it = items[i];
                into.Add(new PartDescription
                {
                    Name = "item." + it.Id, Role = PartRole.Option, Label = it.LabelKey,
                    Flags = it.Id == SelectedId ? PartFlags.On : PartFlags.None
                });
            }
        }

        public const int NoId = int.MinValue;

        public TimelineModel(ViewportModel view) => View = view ?? throw new ArgumentNullException(nameof(view));

        /// <summary>The axis: its window over the data and its mapping to the strip.</summary>
        public readonly ViewportModel View;

        /// <summary>The lanes drawn at least (0: as many as the highest lane used).</summary>
        public int LaneCount;

        /// <summary>A lane with no item still takes a row (the strip keeps its height as items come and go).</summary>
        public bool KeepEmptyLanes = true;

        /// <summary>How near an item the pointer picks it, as a fraction of the strip's width (the host sets it from pixels).</summary>
        public double PickFraction = 0.01;

        /// <summary>How close items are grouped, as a fraction of the strip's width (the host sets it from pixels); 0: never.</summary>
        public double GroupFraction = 0.02;

        /// <summary>Called on every pick, by the pointer, a key or the program (null: a clear).</summary>
        public event Action<TimelineItem, ChangeSource> Picked;

        /// <summary>Bumped by every change of the items or the selection.</summary>
        public int Version { get; private set; }

        /// <summary>Bumped when <see cref="Now"/> moves.</summary>
        public int NowVersion { get; private set; }

        /// <summary>The source of the last selection change.</summary>
        public ChangeSource LastSource { get; private set; } = ChangeSource.Program;

        readonly List<TimelineItem> items = new List<TimelineItem>();
        readonly List<TimelineItem> ordered = new List<TimelineItem>();
        readonly List<int> laneRows = new List<int>();
        readonly List<int> laneTop = new List<int>();
        readonly List<TimelineItem> rowLast = new List<TimelineItem>();
        readonly List<TimelineGroup> groups = new List<TimelineGroup>();
        readonly List<TimelineGroup> groupPool = new List<TimelineGroup>();
        readonly List<TimelineItem> rowRun = new List<TimelineItem>();
        static readonly OrderComparer byTime = new OrderComparer();

        int laidOut = -1;
        int grouped = -1;
        double groupedSpan = double.NaN, groupedFraction = double.NaN;
        int rowCount;
        double itemsMin = double.NaN, itemsMax = double.NaN;

        // ---- the items ----

        public int Count => items.Count;

        /// <summary>The items in the order they were added.</summary>
        public TimelineItem this[int i] => items[i];

        /// <summary>Removes every item. The selection keeps its id.</summary>
        public void Clear()
        {
            if (items.Count == 0) return;
            for (int i = 0; i < items.Count; i++) items[i].owner = null;
            items.Clear();
            Version++;
        }

        int update;
        bool updating;

        /// <summary>Starts building the items again: <see cref="EndUpdate"/> removes those not <see cref="Set"/> in between.</summary>
        public void BeginUpdate()
        {
            update++;
            updating = true;
        }

        /// <summary>Removes the items not <see cref="Set"/> since <see cref="BeginUpdate"/>.</summary>
        public void EndUpdate()
        {
            if (!updating) return;
            updating = false;
            bool removed = false;
            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i].update == update) continue;
                items[i].owner = null;
                items.RemoveAt(i);
                removed = true;
            }

            if (removed) Version++;
        }

        /// <summary>
        /// The item with id <paramref name="id"/> takes these values, added when no item has that id: a span from
        /// <paramref name="start"/> to <paramref name="end"/> (in either order), or an instant when <paramref name="end"/> is NaN.
        /// An id keeps its item object for as long as it stays, so a reference to it stays that item. Null, with nothing changed,
        /// for a non-finite start or an infinite end. <see cref="Version"/> moves only when something changed.
        /// </summary>
        public TimelineItem Set(int id, double start, double end, int lane, int kind, string labelKey, double value = double.NaN, object tag = null)
        {
            if (!Finite(start) || double.IsInfinity(end)) return null;
            if (end < start)
            {
                double t = end;
                end = start;
                start = t;
            }

            lane = Math.Max(0, lane);
            TimelineItem item = Find(id);
            if (item == null)
            {
                item = new TimelineItem { Id = id, owner = this };
                items.Add(item);
                Version++;
            }
            else if (!item.Start.Equals(start) || !item.End.Equals(end) || item.Lane != lane || item.Kind != kind || item.LabelKey != labelKey ||
                     !item.Value.Equals(value) || !Equals(item.Tag, tag))
                Version++;

            item.Start = start;
            item.End = end;
            item.Lane = lane;
            item.Kind = kind;
            item.LabelKey = labelKey;
            item.Value = value;
            item.Tag = tag;
            item.update = update;
            return item;
        }

        /// <summary>The item with an id; null when none has it.</summary>
        public TimelineItem Find(int id)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i].Id == id) return items[i];
            return null;
        }

        /// <summary>Removes the item with an id. False when none has it.</summary>
        public bool Remove(int id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id != id) continue;
                items[i].owner = null;
                items.RemoveAt(i);
                Version++;
                return true;
            }

            return false;
        }

        // ---- the layout ----

        /// <summary>The items in time order (start, end, then id).</summary>
        public TimelineItem Ordered(int i)
        {
            Layout();
            return ordered[i];
        }

        /// <summary>The rows the strip draws: every lane's.</summary>
        public int RowCount
        {
            get
            {
                Layout();
                return rowCount;
            }
        }

        /// <summary>The rows a lane takes (0 for a lane past the ones drawn).</summary>
        public int RowsOf(int lane)
        {
            Layout();
            return lane >= 0 && lane < laneRows.Count ? laneRows[lane] : 0;
        }

        /// <summary>The display row of a lane's first row (-1 for a lane past the ones drawn).</summary>
        public int TopOf(int lane)
        {
            Layout();
            return lane >= 0 && lane < laneTop.Count ? laneTop[lane] : -1;
        }

        /// <summary>The lane a display row belongs to (-1 outside the strip).</summary>
        public int LaneOfRow(int row)
        {
            Layout();
            for (int lane = 0; lane < laneTop.Count; lane++)
                if (row >= laneTop[lane] && row < laneTop[lane] + laneRows[lane]) return lane;
            return -1;
        }

        /// <summary>The earliest start of the items (NaN with none).</summary>
        public double ItemsMin
        {
            get
            {
                Layout();
                return itemsMin;
            }
        }

        /// <summary>The latest finish of the items (NaN with none).</summary>
        public double ItemsMax
        {
            get
            {
                Layout();
                return itemsMax;
            }
        }

        internal void Layout()
        {
            if (laidOut == Version) return;
            laidOut = Version;
            grouped = -1;

            ordered.Clear();
            ordered.AddRange(items);
            ordered.Sort(byTime);
            itemsMin = itemsMax = double.NaN;
            int lanes = LaneCount;
            for (int i = 0; i < ordered.Count; i++)
            {
                TimelineItem it = ordered[i];
                it.order = i;
                it.group = -1;
                lanes = Math.Max(lanes, it.Lane + 1);
                if (!(it.Start >= itemsMin)) itemsMin = it.Start;
                if (!(it.Finish <= itemsMax)) itemsMax = it.Finish;
            }

            laneRows.Clear();
            laneTop.Clear();
            rowCount = 0;
            for (int lane = 0; lane < lanes; lane++)
            {
                // the first free row of the lane, in time order
                rowLast.Clear();
                for (int i = 0; i < ordered.Count; i++)
                {
                    TimelineItem it = ordered[i];
                    if (it.Lane != lane) continue;
                    int row = 0;
                    while (row < rowLast.Count && Overlap(rowLast[row], it)) row++;
                    if (row == rowLast.Count) rowLast.Add(it);
                    else rowLast[row] = it;
                    it.row = row;
                }

                int rows = rowLast.Count > 0 ? rowLast.Count : KeepEmptyLanes ? 1 : 0;
                laneTop.Add(rowCount);
                laneRows.Add(rows);
                rowCount += rows;
            }

            for (int i = 0; i < ordered.Count; i++)
                ordered[i].displayRow = laneTop[ordered[i].Lane] + ordered[i].row;
        }

        // b comes after a in time order: they overlap when they share more than one instant, or are instants at the same time
        static bool Overlap(TimelineItem a, TimelineItem b)
        {
            if (a.Finish > b.Start) return true;
            return a.IsInstant && b.IsInstant && a.Start.Equals(b.Start);
        }

        sealed class OrderComparer : IComparer<TimelineItem>
        {
            public int Compare(TimelineItem a, TimelineItem b)
            {
                int c = a.Start.CompareTo(b.Start);
                if (c != 0) return c;
                c = a.Finish.CompareTo(b.Finish);
                return c != 0 ? c : a.Id.CompareTo(b.Id);
            }
        }

        // ---- grouping ----

        /// <summary>The groups at the current zoom.</summary>
        public int GroupCount
        {
            get
            {
                Groups();
                return groups.Count;
            }
        }

        public TimelineGroup Group(int i)
        {
            Groups();
            return groups[i];
        }

        /// <summary>The group an item is drawn in at the current zoom (null: drawn alone).</summary>
        public TimelineGroup GroupOf(TimelineItem item)
        {
            if (item == null) return null;
            Groups();
            return item.group >= 0 ? groups[item.group] : null;
        }

        internal void Groups()
        {
            Layout();
            double span = View.Span;
            if (grouped == laidOut && span.Equals(groupedSpan) && GroupFraction.Equals(groupedFraction)) return;
            grouped = laidOut;
            groupedSpan = span;
            groupedFraction = GroupFraction;

            groupPool.AddRange(groups);
            groups.Clear();
            for (int i = 0; i < ordered.Count; i++) ordered[i].group = -1;
            double near = GroupFraction > 0 && span > 0 ? GroupFraction * span : 0;
            if (!(near > 0)) return;

            // each row on its own: its items are in time order within the ordered list
            for (int row = 0; row < rowCount; row++)
            {
                rowRun.Clear();
                for (int i = 0; i < ordered.Count; i++)
                {
                    TimelineItem it = ordered[i];
                    if (it.displayRow != row) continue;
                    bool small = it.Width < near;
                    if (small && rowRun.Count > 0 && it.Start - rowRun[rowRun.Count - 1].Finish < near)
                    {
                        rowRun.Add(it);
                        continue;
                    }

                    CloseRun(row);
                    if (small) rowRun.Add(it);
                }

                CloseRun(row);
            }
        }

        // the run of small items gathered so far becomes a group when it holds two or more
        void CloseRun(int row)
        {
            if (rowRun.Count >= 2)
            {
                TimelineGroup g;
                if (groupPool.Count > 0)
                {
                    g = groupPool[groupPool.Count - 1];
                    groupPool.RemoveAt(groupPool.Count - 1);
                }
                else g = new TimelineGroup();

                g.items.Clear();
                g.DisplayRow = row;
                g.Start = rowRun[0].Start;
                g.End = rowRun[0].Finish;
                g.SmallestGap = double.PositiveInfinity;
                for (int k = 0; k < rowRun.Count; k++)
                {
                    TimelineItem it = rowRun[k];
                    if (k > 0) g.SmallestGap = Math.Min(g.SmallestGap, Math.Max(0, it.Start - rowRun[k - 1].Finish));
                    g.End = Math.Max(g.End, it.Finish);
                    it.group = groups.Count;
                    g.items.Add(it);
                }

                groups.Add(g);
            }

            rowRun.Clear();
        }

        // ---- selection ----

        /// <summary>The selected item's id (<see cref="NoId"/>: none).</summary>
        public int SelectedId { get; private set; } = NoId;

        /// <summary>The selected item: null when nothing is selected or no item has the selected id.</summary>
        public TimelineItem Selected => SelectedId == NoId ? null : Find(SelectedId);

        /// <summary>Selects an item (null clears) and calls <see cref="Picked"/>, even when it was already selected.</summary>
        public bool Select(TimelineItem item, ChangeSource source)
        {
            if (item != null && !items.Contains(item)) return false;
            int id = item?.Id ?? NoId;
            LastSource = source;
            if (id != SelectedId)
            {
                SelectedId = id;
                Version++;
            }

            if (item != null && source != ChangeSource.Pointer) Reveal(item, source);
            Picked?.Invoke(item, source);
            return true;
        }

        /// <summary>Selects the item with an id; false when none has it.</summary>
        public bool SelectId(int id, ChangeSource source)
        {
            TimelineItem item = Find(id);
            return item != null && Select(item, source);
        }

        public bool ClearSelection(ChangeSource source) => SelectedId != NoId && Select(null, source);

        /// <summary>The next item in time order (from none: the first). False at the last.</summary>
        public bool SelectNext(ChangeSource source) => Step(1, source);

        /// <summary>The previous item in time order (from none: the last). False at the first.</summary>
        public bool SelectPrevious(ChangeSource source) => Step(-1, source);

        bool Step(int dir, ChangeSource source)
        {
            Layout();
            if (ordered.Count == 0) return false;
            TimelineItem sel = Selected;
            int i = sel == null ? (dir > 0 ? 0 : ordered.Count - 1) : sel.order + dir;
            if (i < 0 || i >= ordered.Count) return false;
            return Select(ordered[i], source);
        }

        /// <summary>A key on the focused timeline. True when the timeline handled it.</summary>
        public bool Key(TimelineKey key, ChangeSource source = ChangeSource.Keyboard)
        {
            switch (key)
            {
                case TimelineKey.Next: SelectNext(source); return true;
                case TimelineKey.Previous: SelectPrevious(source); return true;
                case TimelineKey.First:
                    Layout();
                    if (ordered.Count > 0) Select(ordered[0], source);
                    return true;
                case TimelineKey.Last:
                    Layout();
                    if (ordered.Count > 0) Select(ordered[ordered.Count - 1], source);
                    return true;
                case TimelineKey.Clear: ClearSelection(source); return true;
                default: return false;
            }
        }

        /// <summary>Pans the view so an item outside it is in the middle, keeping the zoom. False when it is already in view.</summary>
        public bool Reveal(TimelineItem item, ChangeSource source)
        {
            if (item == null) return false;
            if (item.Finish >= View.Start && item.Start <= View.End) return false;
            return View.PanTo((item.Start + item.Finish) / 2 - View.Span / 2, source);
        }

        // ---- the pointer ----

        /// <summary>
        /// What lies at <paramref name="fraction"/> of the strip in display row <paramref name="row"/>: a group (in
        /// <paramref name="group"/>, the item null), an item, or nothing.
        /// </summary>
        public TimelineItem HitTest(double fraction, int row, out TimelineGroup group)
        {
            group = null;
            if (!Finite(fraction)) return null;
            Groups();
            double x = View.FromFraction(fraction), pick = Math.Max(0, PickFraction) * View.Span;
            for (int g = 0; g < groups.Count; g++)
            {
                TimelineGroup gr = groups[g];
                if (gr.DisplayRow == row && x >= gr.Start - pick && x <= gr.End + pick)
                {
                    group = gr;
                    return null;
                }
            }

            TimelineItem best = null;
            double bestD = double.PositiveInfinity;
            for (int i = 0; i < ordered.Count; i++)
            {
                TimelineItem it = ordered[i];
                if (it.displayRow != row || it.group >= 0) continue;
                double d;
                if (it.IsInstant) d = Math.Abs(x - it.Start);
                else if (x >= it.Start && x <= it.End) d = 0;
                else d = Math.Min(Math.Abs(x - it.Start), Math.Abs(x - it.End));
                if (d > pick) continue;
                // an instant beats a span it sits on
                if (d < bestD || (d.Equals(bestD) && it.IsInstant && best != null && !best.IsInstant))
                {
                    best = it;
                    bestD = d;
                }
            }

            return best;
        }

        /// <summary>A click at <paramref name="fraction"/> of the strip in display row <paramref name="row"/>: see the summary.</summary>
        public TimelineClick Click(double fraction, int row, ChangeSource source)
        {
            if (!Finite(fraction)) return TimelineClick.Nothing;
            TimelineItem item = HitTest(fraction, row, out TimelineGroup group);
            if (item != null)
            {
                Select(item, source);
                return TimelineClick.Selected;
            }

            if (group != null) return ClickGroup(group, source);
            return ClearSelection(source) ? TimelineClick.Cleared : TimelineClick.Nothing;
        }

        TimelineClick ClickGroup(TimelineGroup group, ChangeSource source)
        {
            // the span that draws the closest two items a group's distance apart (half the window at the most), centred on the group
            double fraction = GroupFraction > 0 ? GroupFraction : 0.02;
            double span = Math.Min(group.SmallestGap / fraction * 0.99, View.Span / 2);
            double minSpan = View.MinSpan > 0 ? View.MinSpan : Math.Max(View.Extent, 1) * 1e-9;
            if (span >= minSpan)
            {
                double mid = (group.Start + group.End) / 2;
                View.Show(mid - span / 2, mid + span / 2, source);
                return TimelineClick.Zoomed;
            }

            // they can't be drawn apart: each click selects the next of them
            TimelineItem sel = Selected;
            int k = 0;
            for (int i = 0; i < group.Count; i++)
                if (group.Item(i) == sel)
                    k = (i + 1) % group.Count;
            Select(group.Item(k), source);
            return TimelineClick.Selected;
        }

        // ---- now ----

        /// <summary>The time drawn as "now" (NaN: no marker).</summary>
        public double Now { get; private set; } = double.NaN;

        /// <summary>Moves the "now" marker (NaN hides it). False when it didn't move or the value is infinite.</summary>
        public bool SetNow(double now)
        {
            if (double.IsInfinity(now) || now.Equals(Now)) return false;
            Now = now;
            NowVersion++;
            return true;
        }

        /// <summary>Where "now" falls on the strip (see <see cref="ViewportModel.ToFraction"/>); NaN without one.</summary>
        public double NowFraction => double.IsNaN(Now) ? double.NaN : View.ToFraction(Now);

        /// <summary>"Now" is set and inside the window.</summary>
        public bool NowShown
        {
            get
            {
                double f = NowFraction;
                return f >= 0 && f <= 1;
            }
        }

        /// <summary>The item finishes before "now" (false without one).</summary>
        public bool IsPast(TimelineItem item) => item != null && item.Finish < Now;

        /// <summary>A span that "now" is inside (false for an instant or without "now").</summary>
        public bool IsCurrent(TimelineItem item) => item != null && !item.IsInstant && Now >= item.Start && Now < item.End;

        /// <summary>The first item, in time order, that starts at or after a time (null: none).</summary>
        public TimelineItem NextAfter(double t)
        {
            Layout();
            if (double.IsNaN(t)) return null;
            for (int i = 0; i < ordered.Count; i++)
                if (ordered[i].Start >= t) return ordered[i];
            return null;
        }

        static bool Finite(double x) => !double.IsNaN(x) && !double.IsInfinity(x);
    }
}
