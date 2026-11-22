/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's drag to reorder, without KSP or Unity.</summary>
    public class ReorderModelTests
    {
        private static List<string> Items(int n)
        {
            var l = new List<string>();
            for (int i = 0; i < n; i++) l.Add(((char)('a' + i)).ToString());
            return l;
        }

        private static string Join(List<string> l) => string.Concat(l);

        private static ReorderModel Model(List<string> items, List<(int, int)> calls = null) =>
            new ReorderModel(() => items.Count, (from, to) =>
            {
                calls?.Add((from, to));
                string s = items[from];
                items.RemoveAt(from);
                items.Insert(to, s);
            });

        [Fact]
        public void ADropBelowARowFurtherDownPutsTheRowJustAboveTheGap()
        {
            List<string> items = Items(5);
            ReorderModel m = Model(items);
            Assert.True(m.Begin(1));
            m.Over(4);
            Assert.Equal(3, m.To);
            ListMove done = m.Drop();
            Assert.True(done.Moved);
            Assert.Equal(1, done.From);
            Assert.Equal(3, done.To);
            Assert.Equal("acdbe", Join(items));
        }

        [Fact]
        public void ADropAboveARowFurtherUpPutsTheRowAtTheGap()
        {
            List<string> items = Items(5);
            ReorderModel m = Model(items);
            m.Begin(3);
            m.Over(0);
            Assert.Equal(new ListMove(3, 0, true), m.Drop());
            Assert.Equal("dabce", Join(items));
        }

        [Fact]
        public void TheLastGapPutsTheRowAtTheEnd()
        {
            List<string> items = Items(4);
            ReorderModel m = Model(items);
            m.Begin(0);
            m.Over(4);
            m.Drop();
            Assert.Equal("bcda", Join(items));
        }

        [Fact]
        public void TheTwoGapsAroundTheRowAreNoMove()
        {
            var calls = new List<(int, int)>();
            List<string> items = Items(5);
            ReorderModel m = Model(items, calls);
            foreach (int gap in new[] { 2, 3 })
            {
                m.Begin(2);
                m.Over(gap);
                Assert.False(m.ShowsIndicator);
                Assert.False(m.Drop().Moved);
            }
            Assert.Empty(calls);
            Assert.Equal("abcde", Join(items));
        }

        [Fact]
        public void NothingMovesDuringTheDrag()
        {
            var calls = new List<(int, int)>();
            List<string> items = Items(5);
            ReorderModel m = Model(items, calls);
            m.Begin(0);
            for (int g = 0; g <= 5; g++) m.Over(g);
            Assert.Empty(calls);
            Assert.True(m.ShowsIndicator);
        }

        [Fact]
        public void APointerOutsideTheListDropsNothing()
        {
            var calls = new List<(int, int)>();
            List<string> items = Items(5);
            ReorderModel m = Model(items, calls);
            m.Begin(0);
            m.Over(4);
            m.Outside();
            Assert.Equal(-1, m.Gap);
            Assert.False(m.ShowsIndicator);
            Assert.False(m.Drop().Moved);
            Assert.Empty(calls);
        }

        [Fact]
        public void CancelEndsTheDragWithoutAMove()
        {
            var calls = new List<(int, int)>();
            ReorderModel m = Model(Items(5), calls);
            m.Begin(0);
            m.Over(4);
            m.Cancel();
            Assert.False(m.Dragging);
            Assert.Equal(-1, m.From);
            Assert.False(m.Drop().Moved);
            Assert.Empty(calls);
        }

        [Fact]
        public void ADropEndsTheDrag()
        {
            ReorderModel m = Model(Items(5));
            m.Begin(0);
            m.Over(3);
            m.Drop();
            Assert.False(m.Dragging);
            Assert.Equal(-1, m.To);
        }

        [Fact]
        public void ADragCanOnlyStartOnARowOfTheList()
        {
            ReorderModel m = Model(Items(3));
            Assert.False(m.Begin(-1));
            Assert.False(m.Begin(3));
            Assert.False(m.Dragging);
        }

        [Fact]
        public void AFixedRowCantBeDragged()
        {
            ReorderModel m = Model(Items(5));
            m.IsFixed = i => i == 0;
            Assert.False(m.Begin(0));
            Assert.False(m.Dragging);
        }

        [Fact]
        public void AFixedRowKeepsItsIndexTheGapStopsBesideIt()
        {
            List<string> items = Items(6);
            ReorderModel m = Model(items);
            m.IsFixed = i => items[i] == "a" || items[i] == "e";
            m.Begin(2);
            Assert.Equal(1, m.MinGap);
            Assert.Equal(4, m.MaxGap);
            m.Over(0);
            Assert.Equal(1, m.Gap);
            m.Over(6);
            Assert.Equal(4, m.Gap);
            m.Drop();
            Assert.Equal("abdcef", Join(items));
            Assert.Equal("a", items[0]);
            Assert.Equal("e", items[4]);
        }

        [Fact]
        public void TheGapIsClampedToTheList()
        {
            ReorderModel m = Model(Items(3));
            m.Begin(0);
            m.Over(99);
            Assert.Equal(3, m.Gap);
            Assert.Equal(2, m.To);
        }

        [Fact]
        public void ARowRemovedDuringTheDragCancelsIt()
        {
            List<string> items = Items(3);
            var calls = new List<(int, int)>();
            ReorderModel m = Model(items, calls);
            m.Begin(2);
            items.RemoveAt(2);
            m.Over(0);
            Assert.False(m.Dragging);
            Assert.False(m.Drop().Moved);
            Assert.Empty(calls);
        }

        [Fact]
        public void MoveByOneIsTheMoveUpAndDownButtons()
        {
            List<string> items = Items(4);
            ReorderModel m = Model(items);
            Assert.True(m.MoveBy(2, -1, ChangeSource.Pointer));
            Assert.Equal("acbd", Join(items));
            Assert.True(m.MoveBy(1, 1, ChangeSource.Keyboard));
            Assert.Equal("abcd", Join(items));
            Assert.Equal(ChangeSource.Keyboard, m.Source);
        }

        [Fact]
        public void AMovePastAnEndIsNoMove()
        {
            var calls = new List<(int, int)>();
            ReorderModel m = Model(Items(3), calls);
            Assert.False(m.MoveBy(0, -1, ChangeSource.Agent));
            Assert.False(m.MoveBy(2, 1, ChangeSource.Agent));
            Assert.Empty(calls);
        }

        [Fact]
        public void AMoveWithoutADragStopsAtAFixedRow()
        {
            List<string> items = Items(5);
            ReorderModel m = Model(items);
            m.IsFixed = i => items[i] == "b";
            Assert.False(m.MoveBy(2, -1, ChangeSource.Agent));
            Assert.False(m.Move(1, 3, ChangeSource.Agent));
            // clamped to just below the fixed row: already there
            Assert.False(m.Move(2, 0, ChangeSource.Agent));
            Assert.True(m.Move(4, 0, ChangeSource.Agent));
            Assert.Equal("abecd", Join(items));
        }

        [Fact]
        public void AMoveToItsOwnIndexIsNoMove()
        {
            var calls = new List<(int, int)>();
            ReorderModel m = Model(Items(3), calls);
            Assert.False(m.Move(1, 1, ChangeSource.Agent));
            Assert.Empty(calls);
        }

        [Fact]
        public void TheDragRecordsItsSource()
        {
            ReorderModel m = Model(Items(3));
            m.Begin(0, ChangeSource.Agent);
            Assert.Equal(ChangeSource.Agent, m.Source);
        }

        // ---- where the pointer falls: gaps on a virtual list's geometry ----

        [Fact]
        public void TheUpperHalfOfARowIsTheGapAboveItTheLowerHalfTheGapBelow()
        {
            var list = new VirtualListModel(10, 20, 100);
            Assert.Equal(3, ListDrop.GapAt(list, 60 + 9));
            Assert.Equal(4, ListDrop.GapAt(list, 60 + 11));
        }

        [Fact]
        public void AboveTheListIsGapZeroBelowItIsTheCount()
        {
            var list = new VirtualListModel(10, 20, 100);
            Assert.Equal(0, ListDrop.GapAt(list, -5));
            Assert.Equal(10, ListDrop.GapAt(list, 500));
            Assert.Equal(0, ListDrop.GapAt(new VirtualListModel(0, 20, 100), 50));
        }

        [Fact]
        public void TheSpacingUnderARowIsItsLowerHalf()
        {
            var list = new VirtualListModel(10, 20, 100, 4);
            // row 2 spans 48..68, its spacing 68..72
            Assert.Equal(3, ListDrop.GapAt(list, 70));
        }

        [Fact]
        public void MeasuredHeightsPlaceTheGaps()
        {
            var list = new VirtualListModel(4, 20, 100);
            list.SetRowHeights(i => i == 1 ? 60 : 20);
            // row 1 spans 20..80: its middle is 50
            Assert.Equal(1, ListDrop.GapAt(list, 45));
            Assert.Equal(2, ListDrop.GapAt(list, 55));
        }

        [Fact]
        public void TheIndicatorSitsInTheMiddleOfTheSpacing()
        {
            var list = new VirtualListModel(10, 20, 100, 4);
            Assert.Equal(0, ListDrop.IndicatorY(list, 0));
            Assert.Equal(3 * 24 - 2, ListDrop.IndicatorY(list, 3));
            Assert.Equal(list.ContentHeight, ListDrop.IndicatorY(list, 10));
        }

        [Fact]
        public void OverAtFollowsThePointerOnTheList()
        {
            List<string> items = Items(10);
            ReorderModel m = Model(items);
            var list = new VirtualListModel(10, 20, 100);
            m.Begin(0);
            m.OverAt(list, 7 * 20 + 15);
            Assert.Equal(8, m.Gap);
            Assert.Equal(7, m.To);
            m.Drop();
            Assert.Equal("bcdefghaij", Join(items));
        }
    }
}
