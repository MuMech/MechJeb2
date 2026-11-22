/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's range selection and marked points on a graph axis, without KSP or Unity.</summary>
    public class RangeModelTests
    {
        // 0 to 1000 of data, the whole of it shown, a minimum width of 10
        private static RangeModel Model()
        {
            var view = new ViewportModel(0, 1000);
            return new RangeModel(view) { MinWidth = 10 };
        }

        [Fact]
        public void ARangeStartsEmpty()
        {
            RangeModel r = Model();
            Assert.False(r.HasRange);
            Assert.Equal(0, r.Width);
            Assert.Equal(0, r.PointCount);
            Assert.Equal(-1, r.ActivePoint);
        }

        [Fact]
        public void SelectPutsTheEndsInOrder()
        {
            RangeModel r = Model();
            Assert.True(r.Select(400, 200, ChangeSource.Program));
            Assert.True(r.HasRange);
            Assert.Equal(200, r.Start, 9);
            Assert.Equal(400, r.End, 9);
            Assert.Equal(200, r.Width, 9);
        }

        [Fact]
        public void SelectClampsTheRangeToTheData()
        {
            RangeModel r = Model();
            Assert.True(r.Select(-50, 1200, ChangeSource.Program));
            Assert.Equal(0, r.Start, 9);
            Assert.Equal(1000, r.End, 9);
        }

        [Fact]
        public void ASelectionNarrowerThanTheMinimumWidthIsRefused()
        {
            RangeModel r = Model();
            r.Select(100, 300, ChangeSource.Program);
            Assert.False(r.Select(500, 505, ChangeSource.Program));
            Assert.Equal(100, r.Start, 9);
            Assert.Equal(300, r.End, 9);
            // clamped to the data, too narrow too
            Assert.False(r.Select(995, 2000, ChangeSource.Program));
        }

        [Fact]
        public void WithoutAMinimumWidthTheViewportsMinSpanIsUsed()
        {
            var view = new ViewportModel(0, 1000) { MinSpan = 25 };
            var r = new RangeModel(view);
            Assert.Equal(25, r.EffectiveMinWidth, 9);
            Assert.False(r.Select(0, 20, ChangeSource.Program));
            Assert.True(r.Select(0, 25, ChangeSource.Program));
        }

        [Fact]
        public void TheStartStopsTheMinimumWidthShortOfTheEnd()
        {
            RangeModel r = Model();
            r.Select(100, 300, ChangeSource.Program);
            Assert.True(r.SetStart(500, ChangeSource.Pointer));
            Assert.Equal(290, r.Start, 9);
            Assert.Equal(300, r.End, 9);
        }

        [Fact]
        public void TheEndStopsTheMinimumWidthPastTheStart()
        {
            RangeModel r = Model();
            r.Select(100, 300, ChangeSource.Program);
            Assert.True(r.SetEnd(0, ChangeSource.Pointer));
            Assert.Equal(100, r.Start, 9);
            Assert.Equal(110, r.End, 9);
        }

        [Fact]
        public void TheEndsStopAtTheData()
        {
            RangeModel r = Model();
            r.Select(100, 300, ChangeSource.Program);
            r.SetStart(-100, ChangeSource.Pointer);
            r.SetEnd(5000, ChangeSource.Pointer);
            Assert.Equal(0, r.Start, 9);
            Assert.Equal(1000, r.End, 9);
        }

        [Fact]
        public void AMoveKeepsTheWidthAndStopsAtTheDatasEnds()
        {
            RangeModel r = Model();
            r.Select(100, 300, ChangeSource.Program);
            Assert.True(r.MoveBy(50, ChangeSource.Pointer));
            Assert.Equal(150, r.Start, 9);
            Assert.Equal(350, r.End, 9);
            r.MoveBy(10000, ChangeSource.Pointer);
            Assert.Equal(800, r.Start, 9);
            Assert.Equal(1000, r.End, 9);
            r.MoveTo(-10, ChangeSource.Pointer);
            Assert.Equal(0, r.Start, 9);
            Assert.Equal(200, r.End, 9);
        }

        [Fact]
        public void EndsAndMovesNeedARange()
        {
            RangeModel r = Model();
            Assert.False(r.SetStart(10, ChangeSource.Pointer));
            Assert.False(r.SetEnd(10, ChangeSource.Pointer));
            Assert.False(r.MoveBy(10, ChangeSource.Pointer));
            Assert.False(r.ZoomToRange(ChangeSource.Pointer));
        }

        [Fact]
        public void ClearRemovesTheRangeOnce()
        {
            RangeModel r = Model();
            r.Select(100, 300, ChangeSource.Program);
            Assert.True(r.Clear(ChangeSource.Pointer));
            Assert.False(r.HasRange);
            Assert.False(r.Clear(ChangeSource.Pointer));
        }

        [Fact]
        public void ZoomToRangeShowsTheRangeInTheViewport()
        {
            RangeModel r = Model();
            r.Select(200, 300, ChangeSource.Program);
            Assert.True(r.ZoomToRange(ChangeSource.Pointer));
            Assert.Equal(200, r.View.Start, 9);
            Assert.Equal(300, r.View.End, 9);
            Assert.True(r.View.Zoomed);
            Assert.Equal(ChangeSource.Pointer, r.View.LastSource);
        }

        [Fact]
        public void PointsAreClampedToTheDataAndTheLastAddedIsActive()
        {
            RangeModel r = Model();
            Assert.Equal(0, r.AddPoint(100, ChangeSource.Pointer));
            Assert.Equal(1, r.AddPoint(5000, ChangeSource.Pointer));
            Assert.Equal(1000, r.Point(1), 9);
            Assert.Equal(1, r.ActivePoint);
        }

        [Fact]
        public void AMovedPointBecomesTheActiveOne()
        {
            RangeModel r = Model();
            r.AddPoint(100, ChangeSource.Pointer);
            r.AddPoint(200, ChangeSource.Pointer);
            Assert.True(r.MovePoint(0, 150, ChangeSource.Pointer));
            Assert.Equal(150, r.Point(0), 9);
            Assert.Equal(0, r.ActivePoint);
        }

        [Fact]
        public void PointsPastTheMaximumDropTheOldest()
        {
            RangeModel r = Model();
            r.MaxPoints = 3;
            for (int i = 1; i <= 4; i++) r.AddPoint(i * 100, ChangeSource.Pointer);
            Assert.Equal(3, r.PointCount);
            Assert.Equal(200, r.Point(0), 9);
            Assert.Equal(400, r.Point(2), 9);
            Assert.Equal(2, r.ActivePoint);
        }

        [Fact]
        public void DroppingTheDraggedPointEndsItsDrag()
        {
            RangeModel r = Model();
            r.MaxPoints = 2;
            r.AddPoint(500, ChangeSource.Pointer);
            r.AddPoint(800, ChangeSource.Pointer);
            Assert.True(r.BeginDrag(0.5, false, ChangeSource.Pointer));
            Assert.Equal(0, r.DraggedPoint);
            r.AddPoint(100, ChangeSource.Pointer); // drops the point at 500
            Assert.Equal(RangeDrag.None, r.Dragging);
            Assert.Equal(-1, r.DraggedPoint);
            Assert.False(r.DragTo(0.9, ChangeSource.Pointer));
            Assert.Equal(800, r.Point(0), 9);
            Assert.Equal(100, r.Point(1), 9);
        }

        [Fact]
        public void RemovingAPointMakesTheNewestLeftActive()
        {
            RangeModel r = Model();
            r.AddPoint(100, ChangeSource.Pointer);
            r.AddPoint(200, ChangeSource.Pointer);
            r.AddPoint(300, ChangeSource.Pointer);
            r.MovePoint(0, 110, ChangeSource.Pointer);
            Assert.True(r.RemovePoint(0, ChangeSource.Pointer));
            Assert.Equal(2, r.PointCount);
            Assert.Equal(1, r.ActivePoint);
            Assert.False(r.RemovePoint(5, ChangeSource.Pointer));
            r.ClearPoints(ChangeSource.Pointer);
            Assert.Equal(-1, r.ActivePoint);
        }

        [Fact]
        public void TheNearestPointIsFoundWithinTheDistanceOnly()
        {
            RangeModel r = Model();
            r.AddPoint(100, ChangeSource.Pointer);
            r.AddPoint(130, ChangeSource.Pointer);
            Assert.Equal(1, r.NearestPoint(120, 15));
            Assert.Equal(0, r.NearestPoint(105, 15));
            Assert.Equal(-1, r.NearestPoint(160, 15));
        }

        [Fact]
        public void HitTestLooksAtPointsThenEndsThenTheBody()
        {
            RangeModel r = Model(); // pick 0.015 of 1000 = 15 units
            r.Select(200, 400, ChangeSource.Program);
            r.AddPoint(205, ChangeSource.Pointer);
            Assert.Equal(RangePart.Point, r.HitTest(0.2, out int p));
            Assert.Equal(0, p);
            Assert.Equal(RangePart.End, r.HitTest(0.39, out _));
            Assert.Equal(RangePart.Body, r.HitTest(0.3, out _));
            Assert.Equal(RangePart.None, r.HitTest(0.6, out _));
            r.RemovePoint(0, ChangeSource.Pointer);
            Assert.Equal(RangePart.Start, r.HitTest(0.21, out _));
        }

        [Fact]
        public void PickDistanceFollowsTheZoom()
        {
            RangeModel r = Model();
            r.Select(200, 400, ChangeSource.Program);
            // zoomed ×10 on 200..300: 0.015 of the plot is 1.5 units
            r.View.Show(200, 300, ChangeSource.Program);
            Assert.Equal(RangePart.Body, r.HitTest(0.1, out _)); // 210, 10 units from the start
            Assert.Equal(RangePart.Start, r.HitTest(0.01, out _));
        }

        [Fact]
        public void ADragThatSelectsDrawsANewRangeFromWhereItStarted()
        {
            RangeModel r = Model();
            Assert.True(r.BeginDrag(0.5, true, ChangeSource.Pointer));
            Assert.Equal(RangeDrag.Create, r.Dragging);
            r.DragTo(0.3, ChangeSource.Pointer);
            Assert.True(r.HasRange);
            Assert.Equal(300, r.Start, 9);
            Assert.Equal(500, r.End, 9);
            r.EndDrag();
            Assert.Equal(RangeDrag.None, r.Dragging);
        }

        [Fact]
        public void ACreatingDragNarrowerThanTheMinimumShowsNoRange()
        {
            RangeModel r = Model();
            r.BeginDrag(0.5, true, ChangeSource.Pointer);
            r.DragTo(0.505, ChangeSource.Pointer);
            Assert.False(r.HasRange);
            r.DragTo(0.6, ChangeSource.Pointer);
            Assert.True(r.HasRange);
            r.DragTo(0.502, ChangeSource.Pointer);
            Assert.False(r.HasRange);
        }

        [Fact]
        public void ADragOnAnEndMovesThatEndKeepingTheGrabbedOffset()
        {
            RangeModel r = Model();
            r.Select(200, 400, ChangeSource.Program);
            Assert.True(r.BeginDrag(0.41, false, ChangeSource.Pointer)); // 10 past the end
            Assert.Equal(RangeDrag.End, r.Dragging);
            r.DragTo(0.61, ChangeSource.Pointer);
            Assert.Equal(200, r.Start, 9);
            Assert.Equal(600, r.End, 9);
        }

        [Fact]
        public void ADragOnTheBodyMovesTheRangeOnlyWhenItSelects()
        {
            RangeModel r = Model();
            r.Select(200, 400, ChangeSource.Program);
            Assert.False(r.BeginDrag(0.3, false, ChangeSource.Pointer)); // the host pans
            Assert.Equal(RangeDrag.None, r.Dragging);
            Assert.True(r.BeginDrag(0.3, true, ChangeSource.Pointer));
            Assert.Equal(RangeDrag.Move, r.Dragging);
            r.DragTo(0.4, ChangeSource.Pointer);
            Assert.Equal(300, r.Start, 9);
            Assert.Equal(500, r.End, 9);
        }

        [Fact]
        public void ADragThatGrabsNothingAndDoesNotSelectIsLeftToTheHost()
        {
            RangeModel r = Model();
            Assert.False(r.BeginDrag(0.5, false, ChangeSource.Pointer));
            Assert.False(r.DragTo(0.7, ChangeSource.Pointer));
            Assert.False(r.HasRange);
        }

        [Fact]
        public void ADragOnAPointMovesItAndMakesItActive()
        {
            RangeModel r = Model();
            r.AddPoint(500, ChangeSource.Pointer);
            r.AddPoint(800, ChangeSource.Pointer);
            Assert.True(r.BeginDrag(0.5, false, ChangeSource.Pointer));
            Assert.Equal(RangeDrag.Point, r.Dragging);
            Assert.Equal(0, r.DraggedPoint);
            Assert.Equal(0, r.ActivePoint);
            r.DragTo(0.55, ChangeSource.Pointer);
            Assert.Equal(550, r.Point(0), 9);
        }

        [Fact]
        public void AClickMarksAPointOrRemovesTheOneUnderIt()
        {
            RangeModel r = Model();
            Assert.True(r.Click(0.25, ChangeSource.Pointer));
            Assert.Equal(1, r.PointCount);
            Assert.Equal(250, r.Point(0), 9);
            Assert.True(r.Click(0.255, ChangeSource.Pointer));
            Assert.Equal(0, r.PointCount);
        }

        [Fact]
        public void ShrunkDataMovesTheRangeAndPointsInsideOrClearsATooNarrowRange()
        {
            RangeModel r = Model();
            r.Select(200, 600, ChangeSource.Program);
            r.AddPoint(700, ChangeSource.Pointer);
            r.View.SetData(0, 400);
            Assert.True(r.ClampToData());
            Assert.Equal(200, r.Start, 9);
            Assert.Equal(400, r.End, 9);
            Assert.Equal(400, r.Point(0), 9);
            r.View.SetData(0, 205);
            r.ClampToData();
            Assert.False(r.HasRange);
        }

        [Fact]
        public void ARangeAtTheMinimumWidthSurvivesTheDataCheck()
        {
            // an end stopped at the minimum width can be a rounding error short of it: the data check must keep it
            var view = new ViewportModel(0, 26) { MinSpan = 0.8 };
            var r = new RangeModel(view);
            for (int i = 1; i <= 200; i++)
            {
                double start = i * 0.0613;
                Assert.True(r.Select(start, start + 9.866, ChangeSource.Program));
                r.SetEnd(0, ChangeSource.Pointer);
                r.ClampToData();
                Assert.True(r.HasRange, $"cleared at start {start}");
            }
        }

        [Fact]
        public void GrowingDataChangesNothing()
        {
            RangeModel r = Model();
            r.Select(200, 600, ChangeSource.Program);
            int version = r.Version;
            r.View.SetData(0, 5000);
            Assert.False(r.ClampToData());
            Assert.Equal(version, r.Version);
        }

        [Fact]
        public void EveryChangeBumpsTheVersionAndRecordsItsSource()
        {
            RangeModel r = Model();
            int v = r.Version;
            r.Select(100, 200, ChangeSource.Agent);
            Assert.Equal(v + 1, r.Version);
            Assert.Equal(ChangeSource.Agent, r.LastSource);
            r.Select(100, 200, ChangeSource.Program); // the same range: no change
            Assert.Equal(v + 1, r.Version);
            r.AddPoint(5, ChangeSource.Pointer);
            Assert.Equal(v + 2, r.Version);
            Assert.Equal(ChangeSource.Pointer, r.LastSource);
        }

        [Fact]
        public void ResetClearsTheRangeAndThePoints()
        {
            RangeModel r = Model();
            r.Select(100, 200, ChangeSource.Program);
            r.AddPoint(5, ChangeSource.Pointer);
            Assert.True(r.Reset(ChangeSource.Program));
            Assert.False(r.HasRange);
            Assert.Equal(0, r.PointCount);
        }

        [Fact]
        public void BadInputChangesNothing()
        {
            RangeModel r = Model();
            r.Select(100, 200, ChangeSource.Program);
            r.AddPoint(50, ChangeSource.Program);
            int v = r.Version;
            Assert.False(r.Select(double.NaN, 300, ChangeSource.Program));
            Assert.False(r.SetStart(double.PositiveInfinity, ChangeSource.Program));
            Assert.False(r.SetEnd(double.NaN, ChangeSource.Program));
            Assert.False(r.MoveBy(double.NaN, ChangeSource.Program));
            Assert.False(r.MoveTo(double.NegativeInfinity, ChangeSource.Program));
            Assert.Equal(-1, r.AddPoint(double.NaN, ChangeSource.Program));
            Assert.False(r.MovePoint(0, double.NaN, ChangeSource.Program));
            Assert.False(r.BeginDrag(double.NaN, true, ChangeSource.Program));
            Assert.False(r.Click(double.NaN, ChangeSource.Program));
            Assert.Equal(-1, r.NearestPoint(double.NaN, 10));
            Assert.Equal(v, r.Version);
            Assert.Equal(100, r.Start, 9);
            Assert.Equal(50, r.Point(0), 9);
        }
    }
}
