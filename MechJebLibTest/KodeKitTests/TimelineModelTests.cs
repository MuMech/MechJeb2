/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's timeline (items on lanes along an axis, their selection and grouping), without KSP or Unity.</summary>
    public class TimelineModelTests
    {
        private const double NaN = double.NaN;

        // 0 to 1000 of data, all of it shown
        private static TimelineModel Model()
        {
            var view = new ViewportModel(0, 1000);
            return new TimelineModel(view) { GroupFraction = 0.02, PickFraction = 0.01 };
        }

        [Fact]
        public void ASpansEndsArePutInOrder()
        {
            TimelineModel t = Model();
            TimelineItem it = t.Set(1, 300, 100, 0, 0, "a");
            Assert.Equal(100, it.Start);
            Assert.Equal(300, it.End);
            Assert.False(it.IsInstant);
            Assert.Equal(200, it.Width);
        }

        [Fact]
        public void ANaNEndIsAnInstant()
        {
            TimelineModel t = Model();
            TimelineItem it = t.Set(1, 100, NaN, 0, 0, "a");
            Assert.True(it.IsInstant);
            Assert.Equal(100, it.Finish);
            Assert.Equal(0, it.Width);
        }

        [Fact]
        public void ANonFiniteStartOrAnInfiniteEndAddsNothing()
        {
            TimelineModel t = Model();
            Assert.Null(t.Set(1, NaN, 10, 0, 0, "a"));
            Assert.Null(t.Set(2, double.PositiveInfinity, NaN, 0, 0, "a"));
            Assert.Null(t.Set(3, 0, double.PositiveInfinity, 0, 0, "a"));
            Assert.Equal(0, t.Count);
        }

        [Fact]
        public void SettingAnIdAgainUpdatesItsItem()
        {
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 0, 10, 0, 0, "a");
            TimelineItem b = t.Set(1, 20, 30, 1, 2, "b", 5);
            Assert.Same(a, b);
            Assert.Equal(1, t.Count);
            Assert.Equal(20, a.Start);
            Assert.Equal(30, a.End);
            Assert.Equal(1, a.Lane);
            Assert.Equal(2, a.Kind);
            Assert.Equal("b", a.LabelKey);
            Assert.Equal(5, a.Value);
        }

        [Fact]
        public void ASetThatChangesNothingKeepsTheVersion()
        {
            TimelineModel t = Model();
            t.Set(1, 0, 10, 0, 0, "a");
            int v = t.Version;
            t.Set(1, 0, 10, 0, 0, "a");
            Assert.Equal(v, t.Version);
            t.Set(1, 0, 11, 0, 0, "a");
            Assert.True(t.Version > v);
        }

        [Fact]
        public void AnUpdateRemovesTheItemsNotSetAgainAndKeepsTheOthersObjects()
        {
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 0, 10, 0, 0, "a");
            t.Set(2, 20, NaN, 0, 0, "b");
            int v = t.Version;
            t.BeginUpdate();
            TimelineItem again = t.Set(1, 0, 10, 0, 0, "a");
            t.EndUpdate();
            Assert.Same(a, again);
            Assert.Equal(1, t.Count);
            Assert.Null(t.Find(2));
            Assert.True(t.Version > v);
            // the same items again: nothing changes
            v = t.Version;
            t.BeginUpdate();
            t.Set(1, 0, 10, 0, 0, "a");
            t.EndUpdate();
            Assert.Equal(v, t.Version);
        }

        [Fact]
        public void ItemsAreOrderedByStartThenEndThenId()
        {
            TimelineModel t = Model();
            t.Set(3, 50, 60, 0, 0, "c");
            t.Set(2, 10, 40, 1, 0, "b");
            t.Set(1, 10, 20, 2, 0, "a");
            t.Set(0, 10, 20, 3, 0, "z");
            Assert.Equal(0, t.Ordered(0).Id);
            Assert.Equal(1, t.Ordered(1).Id);
            Assert.Equal(2, t.Ordered(2).Id);
            Assert.Equal(3, t.Ordered(3).Id);
        }

        [Fact]
        public void OverlappingSpansOfALaneTakeSeparateRows()
        {
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 0, 100, 0, 0, "a");
            TimelineItem b = t.Set(2, 50, 150, 0, 0, "b");
            TimelineItem c = t.Set(3, 120, 200, 0, 0, "c");
            Assert.Equal(2, t.RowCount);
            Assert.Equal(0, a.Row);
            Assert.Equal(1, b.Row);
            // the first free row: a's ended at 100
            Assert.Equal(0, c.Row);
        }

        [Fact]
        public void ASpanThatStartsWhereAnotherEndsSharesItsRow()
        {
            TimelineModel t = Model();
            TimelineItem burn = t.Set(1, 0, 100, 0, 0, "burn");
            TimelineItem coast = t.Set(2, 100, 300, 0, 0, "coast");
            TimelineItem stage = t.Set(3, 300, NaN, 0, 0, "stage");
            Assert.Equal(1, t.RowCount);
            Assert.Equal(0, burn.Row);
            Assert.Equal(0, coast.Row);
            Assert.Equal(0, stage.Row);
        }

        [Fact]
        public void TwoInstantsAtTheSameTimeTakeSeparateRows()
        {
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 100, NaN, 0, 0, "a");
            TimelineItem b = t.Set(2, 100, NaN, 0, 0, "b");
            TimelineItem c = t.Set(3, 101, NaN, 0, 0, "c");
            Assert.Equal(0, a.Row);
            Assert.Equal(1, b.Row);
            Assert.Equal(0, c.Row);
        }

        [Fact]
        public void AnInstantInsideASpanTakesAnotherRow()
        {
            TimelineModel t = Model();
            t.Set(1, 0, 100, 0, 0, "a");
            TimelineItem i = t.Set(2, 50, NaN, 0, 0, "i");
            Assert.Equal(1, i.Row);
        }

        [Fact]
        public void LanesAreDrawnInOrderEachBelowTheRowsOfTheOnesAbove()
        {
            TimelineModel t = Model();
            t.Set(1, 0, 100, 0, 0, "a");
            t.Set(2, 50, 150, 0, 0, "b");
            TimelineItem c = t.Set(3, 0, NaN, 1, 0, "c");
            Assert.Equal(2, t.RowsOf(0));
            Assert.Equal(1, t.RowsOf(1));
            Assert.Equal(2, t.TopOf(1));
            Assert.Equal(2, c.DisplayRow);
            Assert.Equal(3, t.RowCount);
            Assert.Equal(1, t.LaneOfRow(2));
            Assert.Equal(0, t.LaneOfRow(1));
            Assert.Equal(-1, t.LaneOfRow(3));
        }

        [Fact]
        public void AnEmptyLaneKeepsOneRowWhileKeepEmptyLanes()
        {
            TimelineModel t = Model();
            t.LaneCount = 3;
            TimelineItem c = t.Set(1, 0, NaN, 2, 0, "c");
            Assert.Equal(3, t.RowCount);
            Assert.Equal(2, c.DisplayRow);
            t.KeepEmptyLanes = false;
            t.Set(2, 5, NaN, 2, 0, "d"); // a change, for the layout to run again
            Assert.Equal(1, t.RowCount);
            Assert.Equal(0, c.DisplayRow);
        }

        [Fact]
        public void AZoomNeverMovesAnItemToAnotherRow()
        {
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 100, NaN, 0, 0, "a");
            TimelineItem b = t.Set(2, 101, NaN, 0, 0, "b");
            Assert.Equal(0, b.Row);
            t.View.ZoomAt(100, 0.1, ChangeSource.Pointer);
            Assert.Equal(0, a.Row);
            Assert.Equal(0, b.Row);
            Assert.Equal(1, t.RowCount);
        }

        [Fact]
        public void TheItemsExtentIsTheEarliestStartAndTheLatestFinish()
        {
            TimelineModel t = Model();
            Assert.True(double.IsNaN(t.ItemsMin));
            t.Set(1, 50, 80, 0, 0, "a");
            t.Set(2, 10, NaN, 1, 0, "b");
            t.Set(3, 60, 300, 0, 0, "c");
            Assert.Equal(10, t.ItemsMin);
            Assert.Equal(300, t.ItemsMax);
        }

        // ---- grouping ----

        [Fact]
        public void SmallItemsCloserThanTheGroupDistanceAreOneGroup()
        {
            // 2 % of 1000: 20
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 100, NaN, 0, 0, "a");
            TimelineItem b = t.Set(2, 110, NaN, 0, 0, "b");
            TimelineItem c = t.Set(3, 125, 130, 0, 0, "c");
            TimelineItem d = t.Set(4, 400, NaN, 0, 0, "d");
            Assert.Equal(1, t.GroupCount);
            TimelineGroup g = t.Group(0);
            Assert.Equal(3, g.Count);
            Assert.Same(a, g.Item(0));
            Assert.Same(c, g.Item(2));
            Assert.Equal(100, g.Start);
            Assert.Equal(130, g.End);
            Assert.Equal(10, g.SmallestGap, 9);
            Assert.Same(g, t.GroupOf(b));
            Assert.Null(t.GroupOf(d));
        }

        [Fact]
        public void ALoneItemIsNotAGroup()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 300, NaN, 0, 0, "b");
            Assert.Equal(0, t.GroupCount);
        }

        [Fact]
        public void AWideSpanIsNeverGrouped()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            TimelineItem span = t.Set(2, 105, 400, 0, 0, "wide");
            t.Set(3, 405, NaN, 0, 0, "b");
            Assert.Equal(0, t.GroupCount);
            Assert.Equal(-1, span.Group);
        }

        [Fact]
        public void ItemsOfDifferentRowsAreNotGrouped()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 105, NaN, 1, 0, "b");
            Assert.Equal(0, t.GroupCount);
        }

        [Fact]
        public void AZoomInSplitsAGroupAndAZoomOutMakesItAgain()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 110, NaN, 0, 0, "b");
            Assert.Equal(1, t.GroupCount);
            // a span of 250: 2 % is 5, under the gap of 10
            t.View.Show(0, 250, ChangeSource.Pointer);
            Assert.Equal(0, t.GroupCount);
            t.View.Reset(ChangeSource.Pointer);
            Assert.Equal(1, t.GroupCount);
        }

        [Fact]
        public void APanDoesNotChangeTheGroups()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 103, NaN, 0, 0, "b");
            t.View.Show(0, 500, ChangeSource.Pointer);
            Assert.Equal(1, t.GroupCount);
            t.View.PanBy(0.5, ChangeSource.Pointer);
            Assert.Equal(1, t.GroupCount);
        }

        [Fact]
        public void AGroupFractionOfZeroGroupsNothing()
        {
            TimelineModel t = Model();
            t.GroupFraction = 0;
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 100.5, NaN, 0, 0, "b");
            Assert.Equal(0, t.GroupCount);
        }

        // ---- selection ----

        [Fact]
        public void APickCallsPickedEvenOnTheItemAlreadySelected()
        {
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 100, NaN, 0, 0, "a");
            int calls = 0;
            TimelineItem? last = null;
            t.Picked += (it, src) =>
            {
                calls++;
                last = it;
            };
            Assert.True(t.Select(a, ChangeSource.Pointer));
            int v = t.Version;
            Assert.True(t.Select(a, ChangeSource.Pointer));
            Assert.Equal(2, calls);
            Assert.Same(a, last);
            // the second pick changed nothing to redraw
            Assert.Equal(v, t.Version);
        }

        [Fact]
        public void TheSelectionFollowsItsIdWhenTheItemsAreBuiltAgain()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 200, NaN, 0, 0, "b");
            t.SelectId(2, ChangeSource.Pointer);
            t.Clear();
            t.Set(2, 250, NaN, 0, 0, "b");
            t.Set(1, 100, NaN, 0, 0, "a");
            Assert.Equal(2, t.Selected.Id);
            Assert.Equal(250, t.Selected.Start);
        }

        [Fact]
        public void AnIdNoItemHasSelectsNothingUntilItComesBack()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.SelectId(1, ChangeSource.Pointer);
            t.Clear();
            t.Set(5, 100, NaN, 0, 0, "x");
            Assert.Null(t.Selected);
            Assert.Equal(1, t.SelectedId);
            t.Set(1, 120, NaN, 0, 0, "a");
            Assert.Equal(1, t.Selected!.Id);
        }

        [Fact]
        public void NextAndPreviousGoInTimeOrderAndStopAtTheEnds()
        {
            TimelineModel t = Model();
            t.Set(3, 300, NaN, 1, 0, "c");
            t.Set(1, 100, 150, 0, 0, "a");
            t.Set(2, 200, NaN, 2, 0, "b");
            Assert.True(t.SelectNext(ChangeSource.Keyboard));
            Assert.Equal(1, t.Selected.Id);
            Assert.True(t.SelectNext(ChangeSource.Keyboard));
            Assert.Equal(2, t.Selected.Id);
            Assert.True(t.SelectNext(ChangeSource.Keyboard));
            Assert.Equal(3, t.Selected.Id);
            Assert.False(t.SelectNext(ChangeSource.Keyboard));
            Assert.Equal(3, t.Selected.Id);
            Assert.True(t.SelectPrevious(ChangeSource.Keyboard));
            Assert.Equal(2, t.Selected.Id);
        }

        [Fact]
        public void FromNoSelectionPreviousIsTheLastItem()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 200, NaN, 0, 0, "b");
            Assert.True(t.SelectPrevious(ChangeSource.Keyboard));
            Assert.Equal(2, t.Selected.Id);
        }

        [Fact]
        public void ClearSelectionCallsPickedWithNull()
        {
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 100, NaN, 0, 0, "a");
            t.Select(a, ChangeSource.Pointer);
            TimelineItem? last = a;
            t.Picked += (it, src) => last = it;
            Assert.True(t.ClearSelection(ChangeSource.Pointer));
            Assert.Null(last);
            Assert.Null(t.Selected);
            Assert.False(t.ClearSelection(ChangeSource.Pointer));
        }

        [Fact]
        public void AKeySelectionOutsideTheWindowPansTheViewToItKeepingTheZoom()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 900, NaN, 0, 0, "b");
            t.View.Show(0, 200, ChangeSource.Pointer);
            double span = t.View.Span;
            t.SelectId(1, ChangeSource.Program);
            Assert.Equal(0, t.View.Start, 9);
            Assert.True(t.SelectNext(ChangeSource.Keyboard));
            Assert.Equal(span, t.View.Span, 9);
            Assert.Equal(900, t.View.Start + t.View.Span / 2, 6);
        }

        [Fact]
        public void APointerSelectionDoesNotPanTheView()
        {
            TimelineModel t = Model();
            TimelineItem b = t.Set(2, 900, NaN, 0, 0, "b");
            t.View.Show(0, 200, ChangeSource.Pointer);
            t.Select(b, ChangeSource.Pointer);
            Assert.Equal(0, t.View.Start, 9);
        }

        [Fact]
        public void KeysSelectFirstLastAndClear()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 200, NaN, 0, 0, "b");
            Assert.True(t.Key(TimelineKey.Last));
            Assert.Equal(2, t.Selected.Id);
            Assert.True(t.Key(TimelineKey.First));
            Assert.Equal(1, t.Selected.Id);
            Assert.True(t.Key(TimelineKey.Clear));
            Assert.Null(t.Selected);
        }

        [Fact]
        public void SelectingAnItemOfAnotherModelChangesNothing()
        {
            TimelineModel t = Model(), other = Model();
            TimelineItem x = other.Set(1, 100, NaN, 0, 0, "x");
            Assert.False(t.Select(x, ChangeSource.Pointer));
            Assert.Null(t.Selected);
        }

        // ---- the pointer ----

        [Fact]
        public void AnInstantIsHitWithinThePickDistance()
        {
            // 1 % of 1000: 10
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 500, NaN, 0, 0, "a");
            Assert.Same(a, t.HitTest(0.508, 0, out _));
            Assert.Null(t.HitTest(0.512, 0, out _));
            // another row
            Assert.Null(t.HitTest(0.5, 1, out _));
        }

        [Fact]
        public void ASpanIsHitAnywhereOnIt()
        {
            TimelineModel t = Model();
            TimelineItem s = t.Set(1, 200, 400, 0, 0, "s");
            Assert.Same(s, t.HitTest(0.3, 0, out _));
            Assert.Same(s, t.HitTest(0.405, 0, out _));
            Assert.Null(t.HitTest(0.45, 0, out _));
        }

        [Fact]
        public void AnInstantBeatsTheSpanItSitsOn()
        {
            TimelineModel t = Model();
            t.Set(1, 0, 300, 0, 0, "s");
            TimelineItem i = t.Set(2, 300, NaN, 0, 0, "i");
            Assert.Equal(0, i.Row);
            Assert.Same(i, t.HitTest(0.3, 0, out _));
        }

        [Fact]
        public void TheHitTestFollowsTheView()
        {
            TimelineModel t = Model();
            TimelineItem a = t.Set(1, 150, NaN, 0, 0, "a");
            t.View.Show(100, 200, ChangeSource.Pointer);
            Assert.Same(a, t.HitTest(0.5, 0, out _));
            Assert.Null(t.HitTest(0.15, 0, out _));
        }

        [Fact]
        public void AGroupWinsTheHitTestOverItsItems()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 110, NaN, 0, 0, "b");
            Assert.Null(t.HitTest(0.1, 0, out TimelineGroup? g));
            Assert.NotNull(g);
            Assert.Equal(2, g!.Count);
        }

        [Fact]
        public void AClickOnAnItemSelectsIt()
        {
            TimelineModel t = Model();
            t.Set(1, 500, NaN, 0, 0, "a");
            Assert.Equal(TimelineClick.Selected, t.Click(0.5, 0, ChangeSource.Pointer));
            Assert.Equal(1, t.Selected.Id);
            Assert.Equal(ChangeSource.Pointer, t.LastSource);
        }

        [Fact]
        public void AClickOnNothingClearsTheSelection()
        {
            TimelineModel t = Model();
            t.Set(1, 500, NaN, 0, 0, "a");
            t.Click(0.5, 0, ChangeSource.Pointer);
            Assert.Equal(TimelineClick.Cleared, t.Click(0.9, 0, ChangeSource.Pointer));
            Assert.Null(t.Selected);
            Assert.Equal(TimelineClick.Nothing, t.Click(0.9, 0, ChangeSource.Pointer));
        }

        [Fact]
        public void AClickOnAGroupZoomsUntilItsClosestItemsAreApart()
        {
            TimelineModel t = Model();
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 110, NaN, 0, 0, "b");
            t.Set(3, 112, NaN, 0, 0, "c");
            Assert.Equal(TimelineClick.Zoomed, t.Click(0.106, 0, ChangeSource.Pointer));
            Assert.Equal(0, t.GroupCount);
            Assert.Equal(106, t.View.Start + t.View.Span / 2, 6);
            Assert.Null(t.Selected);
        }

        [Fact]
        public void AClickOnAGroupOfOneTimeSelectsItsItemsInTurn()
        {
            TimelineModel t = Model();
            t.View.MinSpan = 1;
            // three marks at the same time go to three rows; two at almost the same time, closer than MinSpan allows, share one
            t.Set(1, 100, NaN, 0, 0, "a");
            t.Set(2, 100.001, NaN, 0, 0, "b");
            Assert.Equal(1, t.GroupCount);
            Assert.Equal(TimelineClick.Selected, t.Click(0.1, 0, ChangeSource.Pointer));
            Assert.Equal(1, t.Selected.Id);
            t.Click(0.1, 0, ChangeSource.Pointer);
            Assert.Equal(2, t.Selected.Id);
            t.Click(0.1, 0, ChangeSource.Pointer);
            Assert.Equal(1, t.Selected.Id);
        }

        [Fact]
        public void ANonFiniteClickChangesNothing()
        {
            TimelineModel t = Model();
            t.Set(1, 500, NaN, 0, 0, "a");
            Assert.Equal(TimelineClick.Nothing, t.Click(NaN, 0, ChangeSource.Pointer));
            Assert.Null(t.HitTest(double.PositiveInfinity, 0, out _));
        }

        // ---- now ----

        [Fact]
        public void NowMovesTheMarkerWithoutChangingTheLayoutVersion()
        {
            TimelineModel t = Model();
            t.Set(1, 100, 200, 0, 0, "a");
            int v = t.Version, nv = t.NowVersion;
            Assert.True(t.SetNow(150));
            Assert.Equal(v, t.Version);
            Assert.Equal(nv + 1, t.NowVersion);
            Assert.False(t.SetNow(150));
            Assert.False(t.SetNow(double.PositiveInfinity));
            Assert.Equal(0.15, t.NowFraction, 9);
            Assert.True(t.NowShown);
        }

        [Fact]
        public void NoNowShowsNoMarker()
        {
            TimelineModel t = Model();
            Assert.True(double.IsNaN(t.Now));
            Assert.False(t.NowShown);
            t.SetNow(100);
            t.SetNow(NaN);
            Assert.False(t.NowShown);
        }

        [Fact]
        public void NowOutsideTheWindowIsNotShown()
        {
            TimelineModel t = Model();
            t.SetNow(900);
            t.View.Show(0, 100, ChangeSource.Pointer);
            Assert.False(t.NowShown);
        }

        [Fact]
        public void ItemsBeforeNowArePastAndASpanAroundItIsCurrent()
        {
            TimelineModel t = Model();
            TimelineItem done = t.Set(1, 10, 50, 0, 0, "done");
            TimelineItem burning = t.Set(2, 90, 120, 0, 0, "burning");
            TimelineItem mark = t.Set(3, 100, NaN, 1, 0, "mark");
            t.SetNow(100);
            Assert.True(t.IsPast(done));
            Assert.False(t.IsPast(burning));
            Assert.True(t.IsCurrent(burning));
            Assert.False(t.IsCurrent(mark));
            Assert.False(t.IsPast(mark));
            Assert.Same(mark, t.NextAfter(95));
        }

        [Fact]
        public void TheVersionCountsItemAndSelectionChanges()
        {
            TimelineModel t = Model();
            int v = t.Version;
            TimelineItem a = t.Set(1, 100, NaN, 0, 0, "a");
            Assert.True(t.Version > v);
            v = t.Version;
            t.Select(a, ChangeSource.Pointer);
            Assert.True(t.Version > v);
            v = t.Version;
            Assert.True(t.Remove(1));
            Assert.True(t.Version > v);
            Assert.False(t.Remove(1));
        }
    }
}
