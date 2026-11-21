/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    public class SelectionSetTests
    {
        // a list of names (the keys), a filter on it, and the moves and removals the selection asks for
        private sealed class Host
        {
            public readonly List<string> Items;
            public Func<int, bool> Filter;
            public readonly List<string> Edits = new List<string>();

            public Host(params string[] items) => Items = items.ToList();

            public SelectionSet<string> Make()
            {
                var s = new SelectionSet<string>(() => Items.Count, i => Items[i]);
                return s.ShowOnly(i => Filter == null || Filter(i));
            }

            public void Move(int from, int to)
            {
                string x = Items[from];
                Items.RemoveAt(from);
                Items.Insert(to, x);
                Edits.Add(from + ">" + to);
            }

            public void Remove(int at)
            {
                Items.RemoveAt(at);
                Edits.Add("-" + at);
            }

            public string Order => string.Join(",", Items);
        }

        private static List<int> Indexes<T>(SelectionSet<T> s)
        {
            var into = new List<int>();
            s.SelectedIndexes(into);
            return into;
        }

        [Fact]
        public void AClickTogglesOneRowAndRecordsItsSource()
        {
            var h = new Host("a", "b", "c");
            SelectionSet<string> s = h.Make();
            Assert.True(s.Toggle(1, ChangeSource.Pointer));
            Assert.True(s.IsSelected(1));
            Assert.False(s.IsSelected(0));
            Assert.Equal(ChangeSource.Pointer, s.LastSource);
            Assert.True(s.Toggle(1, ChangeSource.Agent));
            Assert.False(s.IsSelected(1));
            Assert.Equal(ChangeSource.Agent, s.LastSource);
        }

        [Fact]
        public void TheSelectionFollowsItsRowsWhenRowsAboveMoveOrGo()
        {
            var h = new Host("a", "b", "c", "d");
            SelectionSet<string> s = h.Make();
            s.Set(2, true, ChangeSource.Pointer); // c
            h.Items.RemoveAt(0);
            Assert.True(s.IsSelected(1));
            h.Items.Insert(0, "z");
            h.Items.Insert(0, "y");
            Assert.True(s.IsSelected(3));
            Assert.Equal(new[] { 3 }, Indexes(s));
        }

        [Fact]
        public void SettingARowToTheStateItHasChangesNothingAndFiresNothing()
        {
            var h = new Host("a", "b");
            SelectionSet<string> s = h.Make();
            int fired = 0;
            s.Changed += () => fired++;
            Assert.False(s.Set(0, false, ChangeSource.Pointer));
            s.Set(0, true, ChangeSource.Pointer);
            Assert.False(s.Set(0, true, ChangeSource.Pointer));
            Assert.False(s.Set(5, true, ChangeSource.Pointer));
            Assert.Equal(1, fired);
            Assert.Equal(1, s.Version);
        }

        [Fact]
        public void SelectAllTakesTheShownRowsAndClearTakesEveryRow()
        {
            var h = new Host("a", "b", "c", "d") { Filter = i => i != 2 };
            SelectionSet<string> s = h.Make();
            s.SelectAll(ChangeSource.Pointer);
            Assert.Equal(new[] { "a", "b", "d" }, s.Keys.OrderBy(k => k).ToArray());
            Assert.True(s.AllSelected);
            h.Filter = null; // the filter cleared: c shows, unselected
            Assert.False(s.AllSelected);
            h.Filter = i => i == 2;
            s.Set(2, true, ChangeSource.Pointer);
            s.Clear(ChangeSource.Pointer);
            Assert.Equal(0, s.KeyCount);
        }

        [Fact]
        public void TheAllBoxIsOffForAnEmptyList()
        {
            var h = new Host();
            SelectionSet<string> s = h.Make();
            Assert.False(s.AllSelected);
            Assert.False(s.SetAll(true, ChangeSource.Pointer));
        }

        [Fact]
        public void AFilterHidesRowsWithoutUnselectingThem()
        {
            var h = new Host("apo", "peri", "alt");
            SelectionSet<string> s = h.Make();
            s.Set(0, true, ChangeSource.Pointer);
            s.Set(1, true, ChangeSource.Pointer);
            h.Filter = i => h.Items[i].StartsWith("a");
            Assert.Equal(1, s.Count);
            Assert.Equal(1, s.HiddenCount);
            Assert.Equal(2, s.KeyCount);
            h.Filter = null;
            Assert.True(s.IsSelected(1));
            Assert.Equal(2, s.Count);
        }

        [Fact]
        public void TheActionsLeaveARowAFilterHides()
        {
            var h = new Host("a", "b", "c", "d");
            SelectionSet<string> s = h.Make();
            s.Set(1, true, ChangeSource.Pointer);
            s.Set(3, true, ChangeSource.Pointer);
            h.Filter = i => h.Items[i] != "d";
            Assert.Equal(new[] { 1 }, Indexes(s));
            Assert.Equal(1, s.RemoveSelected(h.Remove, ChangeSource.Pointer));
            Assert.Equal("a,c,d", h.Order);
            Assert.True(s.Contains("d"));
        }

        [Fact]
        public void ARangeFollowsTheListOrderWhicheverEndComesFirst()
        {
            var h = new Host("a", "b", "c", "d", "e") { Filter = i => i != 2 };
            SelectionSet<string> s = h.Make();
            s.SelectRange(3, 1, ChangeSource.Pointer);
            Assert.Equal(new[] { 1, 3 }, Indexes(s));
            Assert.False(s.Contains("c"));
        }

        [Fact]
        public void PruneDropsRowsThatAreGoneNotRowsAFilterHides()
        {
            var h = new Host("a", "b", "c");
            SelectionSet<string> s = h.Make();
            s.SelectAll(ChangeSource.Pointer);
            h.Filter = i => i != 0;
            h.Items.RemoveAt(1);
            Assert.True(s.Prune());
            Assert.Equal(new[] { "a", "c" }, s.Keys.ToArray());
            Assert.False(s.Prune());
        }

        [Fact]
        public void IndexesAreInTheListOrderNotTheClickOrder()
        {
            var h = new Host("a", "b", "c", "d");
            SelectionSet<string> s = h.Make();
            s.Set(3, true, ChangeSource.Pointer);
            s.Set(0, true, ChangeSource.Pointer);
            s.Set(2, true, ChangeSource.Pointer);
            Assert.Equal(new[] { 0, 2, 3 }, Indexes(s));
        }

        [Fact]
        public void MoveUpMovesEachSelectedBlockOnePlace()
        {
            var h = new Host("a", "b", "c", "d", "e");
            SelectionSet<string> s = h.Make();
            s.Set(2, true, ChangeSource.Pointer); // c
            s.Set(3, true, ChangeSource.Pointer); // d
            Assert.Equal(2, s.MoveSelected(-1, h.Move));
            Assert.Equal("a,c,d,b,e", h.Order);
            Assert.Equal(new[] { 1, 2 }, Indexes(s));
        }

        [Fact]
        public void MoveDownMovesEachSelectedBlockOnePlace()
        {
            var h = new Host("a", "b", "c", "d", "e");
            SelectionSet<string> s = h.Make();
            s.Set(0, true, ChangeSource.Pointer); // a
            s.Set(2, true, ChangeSource.Pointer); // c
            Assert.Equal(2, s.MoveSelected(1, h.Move));
            Assert.Equal("b,a,d,c,e", h.Order);
        }

        [Fact]
        public void ABlockAgainstTheEndStaysAndSoDoesTheRowBehindIt()
        {
            var h = new Host("a", "b", "c", "d");
            SelectionSet<string> s = h.Make();
            s.Set(0, true, ChangeSource.Pointer);
            s.Set(1, true, ChangeSource.Pointer);
            s.Set(3, true, ChangeSource.Pointer);
            Assert.Equal(1, s.MoveSelected(-1, h.Move));
            Assert.Equal("a,b,d,c", h.Order);
            var h2 = new Host("a", "b", "c");
            SelectionSet<string> s2 = h2.Make();
            s2.Set(2, true, ChangeSource.Pointer);
            Assert.Equal(0, s2.MoveSelected(1, h2.Move));
            Assert.Empty(h2.Edits);
        }

        [Fact]
        public void RemoveGoesFromTheLastRowUpAndDropsTheKeys()
        {
            var h = new Host("a", "b", "c", "d");
            SelectionSet<string> s = h.Make();
            s.Set(1, true, ChangeSource.Pointer);
            s.Set(3, true, ChangeSource.Pointer);
            Assert.Equal(2, s.RemoveSelected(h.Remove, ChangeSource.Agent));
            Assert.Equal(new[] { "-3", "-1" }, h.Edits.ToArray());
            Assert.Equal("a,c", h.Order);
            Assert.Equal(0, s.KeyCount);
            Assert.Equal(ChangeSource.Agent, s.LastSource);
        }

        // a key that names a row by its place among equal rows (the editor's item and its occurrence)
        private struct Nth : IEquatable<Nth>
        {
            public string Item;
            public int N;
            public bool Equals(Nth o) => Item == o.Item && N == o.N;
            public override bool Equals(object o) => o is Nth n && Equals(n);
            public override int GetHashCode() => (Item?.GetHashCode() ?? 0) * 31 + N;
        }

        private static Nth NthOf(List<string> items, int i)
        {
            int n = 0;
            for (int j = 0; j < i; j++)
                if (items[j] == items[i]) n++;
            return new Nth { Item = items[i], N = n };
        }

        [Fact]
        public void RemovingAnEqualRowNeverPassesItsCheckToTheOther()
        {
            var h = new Host("x", "a", "x");
            var s = new SelectionSet<Nth>(() => h.Items.Count, i => NthOf(h.Items, i));
            s.Set(0, true, ChangeSource.Pointer); // the first x
            Assert.Equal(1, s.RemoveSelected(h.Remove, ChangeSource.Pointer));
            Assert.Equal("a,x", h.Order);
            Assert.False(s.IsSelected(1)); // the other x, now the first, is not checked
        }

        [Fact]
        public void ReadingTheStateOfARowAllocatesNothing()
        {
            var h = new Host("a", "b", "c");
            var s = new SelectionSet<Nth>(() => h.Items.Count, i => NthOf(h.Items, i));
            s.Set(1, true, ChangeSource.Pointer);
            bool any = false;
            for (int i = 0; i < 10; i++) any |= s.IsSelected(i % 3);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) any |= s.IsSelected(i % 3) && s.Count > 0;
            long after = GC.GetAllocatedBytesForCurrentThread();
            Assert.True(any);
            Assert.Equal(0, after - before);
        }
    }
}
