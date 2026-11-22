/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's virtual list (only the visible rows built), without KSP or Unity.</summary>
    public class VirtualListModelTests
    {
        // 1000 rows of 20, a 100-high viewport: 5 rows in view
        private static VirtualListModel List(int count = 1000, float row = 20, float viewport = 100, float spacing = 0) =>
            new VirtualListModel(count, row, viewport, spacing);

        [Fact]
        public void TheContentIsTheRowsAndTheSpacingBetweenThem()
        {
            Assert.Equal(20000, List().ContentHeight);
            Assert.Equal(10 * 20 + 9 * 2, List(10, 20, 100, 2).ContentHeight);
            Assert.Equal(0, List(0).ContentHeight);
        }

        [Fact]
        public void AtTheTopTheRowsInTheViewportAreVisible()
        {
            VirtualListModel m = List();
            Assert.Equal(0, m.FirstVisible);
            Assert.Equal(4, m.LastVisible);
        }

        [Fact]
        public void APartlyShownRowIsVisible()
        {
            VirtualListModel m = List();
            m.SetOffset(30);
            Assert.Equal(1, m.FirstVisible);
            Assert.Equal(6, m.LastVisible);
        }

        [Fact]
        public void ARowStartingAtTheViewportsBottomEdgeIsNotVisible()
        {
            VirtualListModel m = List();
            m.SetOffset(40);
            Assert.Equal(2, m.FirstVisible);
            Assert.Equal(6, m.LastVisible);
        }

        [Fact]
        public void TheOverscanRendersRowsOnBothSidesWithinTheList()
        {
            VirtualListModel m = List();
            m.SetOffset(200);
            Assert.Equal(10, m.FirstVisible);
            Assert.Equal(8, m.RenderFirst);
            Assert.Equal(16, m.RenderLast);
            m.SetOffset(0);
            Assert.Equal(0, m.RenderFirst);
            m.SetOffset(m.MaxOffset);
            Assert.Equal(999, m.RenderLast);
        }

        [Fact]
        public void TheOffsetIsClampedToTheContent()
        {
            VirtualListModel m = List();
            m.SetOffset(-50);
            Assert.Equal(0, m.Offset);
            m.SetOffset(1e9f);
            Assert.Equal(20000 - 100, m.Offset);
        }

        [Fact]
        public void AListShorterThanItsViewportDoesNotScroll()
        {
            VirtualListModel m = List(3);
            m.SetOffset(40);
            Assert.Equal(0, m.Offset);
            Assert.Equal(0, m.MaxOffset);
            Assert.Equal(2, m.LastVisible);
        }

        [Fact]
        public void AnEmptyListRendersNothing()
        {
            VirtualListModel m = List(0);
            Assert.Equal(0, m.RenderedCount);
            Assert.Equal(0, m.PoolSize);
            Assert.Equal(-1, m.IndexAt(10));
        }

        [Fact]
        public void RowsOfNoHeightRenderNothing()
        {
            VirtualListModel m = List(10, 0);
            Assert.Equal(0, m.RenderedCount);
            Assert.Equal(0, m.PoolSize);
        }

        [Fact]
        public void ThePoolIsTheRowsThatFitPlusOnePlusTheOverscan()
        {
            Assert.Equal(5 + 1 + 4, List().PoolSize);
            Assert.Equal(3, List(3).PoolSize);
            VirtualListModel m = List();
            m.Overscan = 0;
            Assert.Equal(6, m.PoolSize);
        }

        [Fact]
        public void NoTwoRenderedRowsShareAPoolSlotAtAnyOffset()
        {
            foreach (float spacing in new[] { 0f, 2f })
            {
                VirtualListModel m = List(1000, 17, 160, spacing);
                m.SetExpanded(40, 55);
                for (float y = 0; y <= m.MaxOffset; y += 3.7f)
                {
                    m.SetOffset(y);
                    Assert.True(m.RenderedCount <= m.PoolSize, $"offset {y}: {m.RenderedCount} rows for a pool of {m.PoolSize}");
                    var slots = new HashSet<int>();
                    for (int i = m.RenderFirst; i <= m.RenderLast; i++) Assert.True(slots.Add(i % m.PoolSize));
                }
            }
        }

        [Fact]
        public void TheRowAtAPointCountsTheSpacingBelowARowAsItsOwn()
        {
            VirtualListModel m = List(10, 20, 100, 2);
            Assert.Equal(0, m.IndexAt(21));
            Assert.Equal(1, m.IndexAt(22));
            Assert.Equal(9, m.IndexAt(5000));
            Assert.Equal(0, m.IndexAt(-5));
        }

        [Fact]
        public void TheExpandedRowIsTallerAndPushesTheRowsBelowIt()
        {
            VirtualListModel m = List(10);
            m.SetExpanded(3, 50);
            Assert.Equal(70, m.HeightOf(3));
            Assert.Equal(60, m.RowTop(3));
            Assert.Equal(130, m.RowTop(4));
            Assert.Equal(250, m.ContentHeight);
            Assert.Equal(3, m.IndexAt(100));
            Assert.Equal(4, m.IndexAt(130));
        }

        [Fact]
        public void ScrollToStartPutsTheRowAtTheTop()
        {
            VirtualListModel m = List();
            Assert.Equal(500 * 20, m.ScrollTo(500, ScrollAlign.Start));
            Assert.Equal(500, m.FirstVisible);
        }

        [Fact]
        public void ScrollToEndPutsTheRowAtTheBottom()
        {
            VirtualListModel m = List();
            m.ScrollTo(500, ScrollAlign.End);
            Assert.Equal(501 * 20 - 100, m.Offset);
            Assert.Equal(500, m.LastVisible);
        }

        [Fact]
        public void ScrollToCenterPutsTheRowInTheMiddle()
        {
            VirtualListModel m = List();
            m.ScrollTo(500, ScrollAlign.Center);
            Assert.Equal(500 * 20 + 10 - 50, m.Offset);
        }

        [Fact]
        public void ScrollToNearestLeavesAWholeRowAlone()
        {
            VirtualListModel m = List();
            m.SetOffset(200);
            m.ScrollTo(12, ScrollAlign.Nearest);
            Assert.Equal(200, m.Offset);
        }

        [Fact]
        public void ScrollToNearestScrollsTheLeastToShowARow()
        {
            VirtualListModel m = List();
            m.SetOffset(200);
            m.ScrollTo(20, ScrollAlign.Nearest);
            Assert.Equal(21 * 20 - 100, m.Offset);
            m.ScrollTo(5, ScrollAlign.Nearest);
            Assert.Equal(100, m.Offset);
            m.ScrollTo(4, ScrollAlign.Nearest);
            Assert.Equal(80, m.Offset);
        }

        [Fact]
        public void ScrollToNearestShowsTheTopOfARowTallerThanTheViewport()
        {
            VirtualListModel m = List();
            m.SetExpanded(10, 200);
            m.ScrollTo(10, ScrollAlign.Nearest);
            Assert.Equal(200, m.Offset);
        }

        [Fact]
        public void ScrollToIsClampedToTheContentAndTheList()
        {
            VirtualListModel m = List();
            m.ScrollTo(999, ScrollAlign.Start);
            Assert.Equal(m.MaxOffset, m.Offset);
            m.ScrollTo(5000, ScrollAlign.Start);
            Assert.Equal(m.MaxOffset, m.Offset);
            m.ScrollTo(-3, ScrollAlign.End);
            Assert.Equal(0, m.Offset);
        }

        [Fact]
        public void ScrollToInAnEmptyListDoesNothing()
        {
            VirtualListModel m = List(0);
            Assert.Equal(0, m.ScrollTo(4, ScrollAlign.Center));
        }

        [Fact]
        public void AScrollRecordsItsSource()
        {
            VirtualListModel m = List();
            m.SetOffset(40, ChangeSource.Pointer);
            Assert.Equal(ChangeSource.Pointer, m.LastSource);
            m.ScrollTo(3, ScrollAlign.Start, ChangeSource.Agent);
            Assert.Equal(ChangeSource.Agent, m.LastSource);
        }

        [Fact]
        public void RowsAddedAtTheEndLeaveTheViewWhereItWas()
        {
            VirtualListModel m = List(100);
            m.SetOffset(305);
            m.SetCount(500);
            Assert.Equal(305, m.Offset);
            Assert.Equal(15, m.FirstVisible);
        }

        [Fact]
        public void RowsTakenOffTheEndClampTheView()
        {
            VirtualListModel m = List(100);
            m.SetOffset(1800);
            m.SetCount(50);
            Assert.Equal(50 * 20 - 100, m.Offset);
            m.SetCount(2);
            Assert.Equal(0, m.Offset);
            m.SetCount(0);
            Assert.Equal(0, m.Offset);
            Assert.Equal(0, m.RenderedCount);
        }

        [Fact]
        public void AClampedViewStaysWhereItWasWhenRowsComeBack()
        {
            VirtualListModel m = List(100);
            m.SetOffset(1800);
            m.SetCount(50);
            m.SetCount(100);
            Assert.Equal(50 * 20 - 100, m.Offset);
        }

        [Fact]
        public void RowsInsertedAboveTheAnchorKeepItInPlace()
        {
            VirtualListModel m = List(100);
            m.SetOffset(305); // row 15, 5 into it
            m.Inserted(3, 10);
            Assert.Equal(110, m.Count);
            Assert.Equal(25, m.FirstVisible);
            Assert.Equal(25 * 20 + 5, m.Offset);
        }

        [Fact]
        public void RowsInsertedBelowTheAnchorDoNotMoveTheView()
        {
            VirtualListModel m = List(100);
            m.SetOffset(305);
            m.Inserted(30, 10);
            Assert.Equal(305, m.Offset);
        }

        [Fact]
        public void RowsRemovedAboveTheAnchorKeepItInPlace()
        {
            VirtualListModel m = List(100);
            m.SetOffset(305);
            m.Removed(0, 5);
            Assert.Equal(10, m.FirstVisible);
            Assert.Equal(205, m.Offset);
        }

        [Fact]
        public void ARemovedAnchorIsReplacedByTheRowThatTakesItsPlace()
        {
            VirtualListModel m = List(100);
            m.SetOffset(305);
            m.Removed(14, 3);
            Assert.Equal(97, m.Count);
            Assert.Equal(14, m.FirstVisible);
            Assert.Equal(280, m.Offset);
        }

        [Fact]
        public void RemovingTheExpandedRowEndsTheExpansion()
        {
            VirtualListModel m = List(100);
            m.SetExpanded(10, 50);
            m.Removed(10, 1);
            Assert.Equal(-1, m.Expanded);
            Assert.Equal(99 * 20, m.ContentHeight);
        }

        [Fact]
        public void TheExpandedRowFollowsRowsInsertedOrRemovedAboveIt()
        {
            VirtualListModel m = List(100);
            m.SetExpanded(10, 50);
            m.Inserted(0, 2);
            Assert.Equal(12, m.Expanded);
            m.Removed(0, 5);
            Assert.Equal(7, m.Expanded);
        }

        [Fact]
        public void ExpandingARowAboveTheViewKeepsTheAnchorRowInPlace()
        {
            VirtualListModel m = List(100);
            m.SetOffset(305);
            m.SetExpanded(2, 40);
            Assert.Equal(345, m.Offset);
            Assert.Equal(15, m.FirstVisible);
        }

        [Fact]
        public void ExpandingAVisibleRowLeavesTheViewWhereItWas()
        {
            VirtualListModel m = List(100);
            m.SetOffset(305);
            m.SetExpanded(16, 40);
            Assert.Equal(305, m.Offset);
            m.SetExpanded(-1, 0);
            Assert.Equal(305, m.Offset);
        }

        [Fact]
        public void ANewRowHeightKeepsTheAnchorRowInPlace()
        {
            VirtualListModel m = List(100);
            m.SetOffset(305);
            m.SetRowHeight(30);
            Assert.Equal(15, m.FirstVisible);
            Assert.Equal(15 * 30 + 5, m.Offset);
        }

        [Fact]
        public void ATallerViewportShowsMoreRowsFromTheSameTop()
        {
            VirtualListModel m = List(100);
            m.SetOffset(300);
            m.SetViewportHeight(200);
            Assert.Equal(300, m.Offset);
            Assert.Equal(24, m.LastVisible);
            Assert.Equal(10 + 1 + 4, m.PoolSize);
        }

        [Fact]
        public void EveryChangeThatCanMoveARowBumpsTheGeometryVersion()
        {
            VirtualListModel m = List(100);
            var steps = new List<Action>
            {
                () => m.SetCount(90),
                () => m.Inserted(0, 1),
                () => m.Removed(0, 1),
                () => m.SetRowHeight(21),
                () => m.SetSpacing(1),
                () => m.SetViewportHeight(120),
                () => m.SetExpanded(3, 10)
            };
            foreach (Action step in steps)
            {
                int before = m.GeometryVersion;
                step();
                Assert.NotEqual(before, m.GeometryVersion);
            }
            int v = m.GeometryVersion;
            m.SetOffset(50);
            m.SetCount(m.Count);
            Assert.Equal(v, m.GeometryVersion);
        }

        // rows of 20, every third one 40 (a label on two lines)
        private static float Tall(int i) => i % 3 == 0 ? 40 : 20;

        [Fact]
        public void MeasuredRowsHaveTheirOwnHeights()
        {
            VirtualListModel m = List(10, 20, 100, 2);
            m.SetRowHeights(Tall);
            Assert.True(m.Measured);
            Assert.Equal(40, m.HeightOf(0));
            Assert.Equal(20, m.HeightOf(1));
            Assert.Equal(42, m.RowTop(1));
            Assert.Equal(40 + 20 + 20 + 3 * 2, m.RowTop(3));
            Assert.Equal(4 * 40 + 6 * 20 + 9 * 2, m.ContentHeight);
            Assert.Equal(0, m.IndexAt(41));
            Assert.Equal(1, m.IndexAt(42));
            Assert.Equal(3, m.IndexAt(m.RowTop(3) + 39));
        }

        [Fact]
        public void TheVisibleRowsFollowMeasuredHeights()
        {
            VirtualListModel m = List(100);
            m.SetRowHeights(Tall);
            m.SetOffset(m.RowTop(30) + 5);
            Assert.Equal(30, m.FirstVisible);
            // 35 of row 30, then 20, 20, 40 (row 33 ends at 115 from row 30's top + 5): rows 30 to 33 in the 100-high view
            Assert.Equal(33, m.LastVisible);
        }

        [Fact]
        public void ThePoolOfMeasuredRowsCountsTheSmallestRow()
        {
            VirtualListModel m = List(1000);
            m.SetRowHeights(Tall);
            Assert.Equal(5 + 1 + 4, m.PoolSize);
            for (float y = 0; y <= m.MaxOffset; y += 7.3f)
            {
                m.SetOffset(y);
                Assert.True(m.RenderedCount <= m.PoolSize);
                var slots = new HashSet<int>();
                for (int i = m.RenderFirst; i <= m.RenderLast; i++) Assert.True(slots.Add(i % m.PoolSize));
            }
        }

        [Fact]
        public void MeasuredHeightsAreAskedAgainOnlyWhenTheRowsChange()
        {
            int asked = 0;
            VirtualListModel m = List(50);
            m.SetRowHeights(i =>
            {
                asked++;
                return 20;
            });
            Assert.Equal(50, asked);
            m.SetOffset(300);
            m.ScrollTo(40, ScrollAlign.Center);
            Assert.Equal(50, asked);
            m.SetCount(60);
            Assert.Equal(110, asked);
            m.RefreshHeights();
            Assert.Equal(170, asked);
        }

        [Fact]
        public void RefreshedHeightsKeepTheAnchorRowInPlace()
        {
            bool tall = false;
            VirtualListModel m = List(100);
            m.SetRowHeights(i => tall && i < 10 ? 40 : 20);
            m.SetOffset(305);
            tall = true;
            m.RefreshHeights();
            Assert.Equal(15, m.FirstVisible);
            Assert.Equal(505, m.Offset);
        }

        [Fact]
        public void NoMeasuredHeightsGoesBackToTheRowHeight()
        {
            VirtualListModel m = List(10);
            m.SetRowHeights(Tall);
            m.SetRowHeights(null);
            Assert.False(m.Measured);
            Assert.Equal(200, m.ContentHeight);
        }

        [Fact]
        public void TheExpandedRowAddsToItsMeasuredHeight()
        {
            VirtualListModel m = List(10);
            m.SetRowHeights(Tall);
            m.SetExpanded(1, 30);
            Assert.Equal(50, m.HeightOf(1));
            Assert.Equal(40 + 50, m.RowTop(2));
            Assert.Equal(1, m.IndexAt(85));
            Assert.Equal(2, m.IndexAt(90));
        }

        [Fact]
        public void AScrollBeforeTheRowsHaveAHeightLeavesTheViewAtTheTop()
        {
            var m = new VirtualListModel(1000, 0, 160, 0);
            m.SetOffset(-160);
            m.SetRowHeight(40);
            Assert.Equal(0, m.FirstVisible);
            Assert.Equal(0, m.Offset);
        }

        [Fact]
        public void RowsInsertedIntoAnEmptyListShowFromTheTop()
        {
            var m = new VirtualListModel(0, 20, 100);
            m.Inserted(0, 50);
            Assert.Equal(0, m.FirstVisible);
            Assert.Equal(0, m.Offset);
        }

        [Fact]
        public void ACountThatDropsTheExpandedRowEndsTheExpansion()
        {
            VirtualListModel m = List(100);
            m.SetExpanded(80, 50);
            m.SetCount(50);
            Assert.Equal(-1, m.Expanded);
            m.SetCount(100);
            Assert.Equal(100 * 20, m.ContentHeight);
        }

        [Fact]
        public void ANewOverscanBumpsTheGeometryVersion()
        {
            VirtualListModel m = List(100);
            int v = m.GeometryVersion;
            m.Overscan = 4;
            Assert.NotEqual(v, m.GeometryVersion);
        }

        [Fact]
        public void ANegativeCountIsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new VirtualListModel(-1, 20, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => List().SetCount(-1));
        }
    }
}
