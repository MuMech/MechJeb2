/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's graph viewport (pan and zoom of one axis), without KSP or Unity.</summary>
    public class ViewportModelTests
    {
        private const double TOL = 1e-9;

        // 0 to 1000 of data, the whole of it shown
        private static ViewportModel Whole() => new ViewportModel(0, 1000);

        [Fact]
        public void WithoutADefaultSpanItShowsTheWholeData()
        {
            ViewportModel v = Whole();
            Assert.Equal(0, v.Start, 9);
            Assert.Equal(1000, v.Span, 9);
            Assert.False(v.Zoomed);
            Assert.Equal(1, v.Magnification, 9);
        }

        [Fact]
        public void FractionsMapTheWindowOntoZeroToOne()
        {
            ViewportModel v = Whole();
            v.Show(200, 400, ChangeSource.Program);
            Assert.Equal(0, v.ToFraction(200), 9);
            Assert.Equal(1, v.ToFraction(400), 9);
            Assert.Equal(0.25, v.ToFraction(250), 9);
            Assert.Equal(350, v.FromFraction(0.75), 9);
        }

        [Fact]
        public void AZoomKeepsTheValueUnderThePointerInPlace()
        {
            ViewportModel v = Whole();
            double under = v.FromFraction(0.3);
            Assert.True(v.ZoomAt(4, 0.3, ChangeSource.Pointer));
            Assert.Equal(250, v.Span, 9);
            Assert.Equal(under, v.FromFraction(0.3), 9);
            Assert.True(v.Zoomed);
            Assert.Equal(ChangeSource.Pointer, v.LastSource);
        }

        [Fact]
        public void WheelNotchesZoomInAwayFromTheUserAndOutTowardsThem()
        {
            ViewportModel v = Whole();
            v.Wheel(2, 0.5, ChangeSource.Pointer);
            Assert.Equal(1000 / (1.25 * 1.25), v.Span, 9);
            v.Wheel(-1, 0.5, ChangeSource.Pointer);
            Assert.Equal(1000 / 1.25, v.Span, 9);
        }

        [Fact]
        public void AZoomStopsAtTheMinimumSpan()
        {
            ViewportModel v = Whole();
            v.MinSpan = 10;
            v.ZoomAt(1e6, 0.5, ChangeSource.Pointer);
            Assert.Equal(10, v.Span, 9);
        }

        [Fact]
        public void AZoomOutStopsAtTheWholeData()
        {
            ViewportModel v = Whole();
            v.ZoomAt(4, 0.5, ChangeSource.Pointer);
            v.ZoomAt(0.001, 0.5, ChangeSource.Pointer);
            Assert.Equal(1000, v.Span, 9);
            Assert.Equal(0, v.Start, 9);
        }

        [Fact]
        public void ZoomingBackOutToTheDefaultSpanIsAReset()
        {
            ViewportModel v = Whole();
            v.ZoomAt(4, 0.5, ChangeSource.Pointer);
            Assert.True(v.Zoomed);
            v.ZoomAt(0.25, 0.5, ChangeSource.Pointer);
            Assert.False(v.Zoomed);
        }

        [Fact]
        public void APanCannotGoPastTheData()
        {
            ViewportModel v = Whole();
            v.Show(100, 200, ChangeSource.Program);
            v.PanByData(-500, ChangeSource.Pointer);
            Assert.Equal(0, v.Start, 9);
            v.PanByData(5000, ChangeSource.Pointer);
            Assert.Equal(1000, v.End, 9);
        }

        [Fact]
        public void OverscrollLetsAPanGoPastTheDataByAShareOfTheSpan()
        {
            ViewportModel v = Whole();
            v.Overscroll = 0.5;
            v.Show(100, 200, ChangeSource.Program);
            v.PanByData(-500, ChangeSource.Pointer);
            Assert.Equal(-50, v.Start, 9);
            v.PanByData(5000, ChangeSource.Pointer);
            Assert.Equal(1050, v.End, 9);
        }

        [Fact]
        public void APanByAFractionMovesBySpans()
        {
            ViewportModel v = Whole();
            v.Show(100, 200, ChangeSource.Program);
            v.PanBy(0.5, ChangeSource.Pointer);
            Assert.Equal(150, v.Start, 9);
            Assert.Equal(100, v.Span, 9);
        }

        [Fact]
        public void AWindowWiderThanTheDataStartsAtTheDataStart()
        {
            // the Flight Recorder's pixel scale: 100 s across the plot, 40 s recorded
            var v = new ViewportModel(0, 40, 100);
            Assert.Equal(100, v.Span, 9);
            Assert.Equal(0, v.Start, 9);
            Assert.False(v.PanByData(30, ChangeSource.Pointer));
            Assert.Equal(0, v.Start, 9);
        }

        [Fact]
        public void UntilZoomedTheSpanFollowsTheDefault()
        {
            var v = new ViewportModel(0, 40, 100);
            v.SetDefaultSpan(200);
            Assert.Equal(200, v.Span, 9);
        }

        [Fact]
        public void ZoomedTheSpanKeepsToTheUsersChoice()
        {
            var v = new ViewportModel(0, 400, 400);
            v.ZoomAt(4, 0.5, ChangeSource.Pointer);
            v.SetDefaultSpan(800);
            Assert.Equal(100, v.Span, 9);
            Assert.Equal(800, v.DefaultSpan, 9);
        }

        [Fact]
        public void FollowKeepsTheEndOnTheDataAsItGrows()
        {
            var v = new ViewportModel(0, 1000, 100) { Follow = true };
            v.SetData(0, 1000); // no change
            v.SetData(0, 1200);
            Assert.Equal(1200, v.End, 9);
            Assert.Equal(100, v.Span, 9);
        }

        [Fact]
        public void PanningAwayFromTheEndStopsFollowingAndBackToItFollowsAgain()
        {
            var v = new ViewportModel(0, 1000, 100) { Follow = true };
            v.SetData(0, 1001);
            v.PanBy(-1, ChangeSource.Pointer);
            Assert.False(v.Follow);
            v.SetData(0, 1100);
            Assert.Equal(901, v.End, 9);
            v.Key(ViewportKey.End);
            Assert.True(v.Follow);
            v.SetData(0, 1200);
            Assert.Equal(1200, v.End, 9);
        }

        [Fact]
        public void WithoutAutoFollowAPanLeavesFollowOff()
        {
            var v = new ViewportModel(0, 1000, 100) { AutoFollow = false };
            v.Key(ViewportKey.End);
            Assert.False(v.Follow);
            v.SetData(0, 1100);
            Assert.Equal(1000, v.End, 9);
        }

        [Fact]
        public void AResetGoesBackToTheDefaultSpanAtTheDataEndAndFollows()
        {
            var v = new ViewportModel(0, 1000, 100);
            v.Show(10, 20, ChangeSource.Pointer);
            Assert.True(v.Reset(ChangeSource.Pointer));
            Assert.False(v.Zoomed);
            Assert.True(v.Follow);
            Assert.Equal(100, v.Span, 9);
            Assert.Equal(1000, v.End, 9);
        }

        [Fact]
        public void AResetThatDoesNotFollowGoesBackToTheDataStart()
        {
            var v = new ViewportModel(0, 1000) { ResetFollows = false };
            v.Show(500, 600, ChangeSource.Pointer);
            v.Reset(ChangeSource.Pointer);
            Assert.False(v.Follow);
            Assert.Equal(0, v.Start, 9);
            Assert.Equal(1000, v.Span, 9);
        }

        [Fact]
        public void ShowZoomsToARangeWhicheverWayItIsGiven()
        {
            ViewportModel v = Whole();
            v.Show(600, 400, ChangeSource.Agent);
            Assert.Equal(400, v.Start, 9);
            Assert.Equal(600, v.End, 9);
            Assert.Equal(ChangeSource.Agent, v.LastSource);
            Assert.Equal(5, v.Magnification, 9);
        }

        [Fact]
        public void FitShowsTheWholeData()
        {
            var v = new ViewportModel(0, 1000, 100);
            v.Fit(ChangeSource.Pointer);
            Assert.Equal(0, v.Start, 9);
            Assert.Equal(1000, v.Span, 9);
        }

        [Fact]
        public void TheScrollbarIsAViewOfTheWindow()
        {
            ViewportModel v = Whole();
            v.Show(250, 500, ChangeSource.Program);
            Assert.Equal(0.25, v.ScrollSize, 9);
            Assert.Equal(250 / 750.0, v.ScrollValue, 9);
            v.ScrollTo(1, ChangeSource.Pointer);
            Assert.Equal(1000, v.End, 9);
            v.ScrollTo(0, ChangeSource.Pointer);
            Assert.Equal(0, v.Start, 9);
        }

        [Fact]
        public void AScrollbarOverDataThatFitsIsFullAndAtItsEnd()
        {
            var v = new ViewportModel(0, 40, 100);
            Assert.Equal(1, v.ScrollSize, 9);
            Assert.Equal(1, v.ScrollValue, 9);
        }

        [Fact]
        public void KeysPanAndZoomAboutTheMiddle()
        {
            ViewportModel v = Whole();
            v.Show(400, 600, ChangeSource.Program);
            Assert.True(v.Key(ViewportKey.PanRight));
            Assert.Equal(420, v.Start, 9);
            Assert.True(v.Key(ViewportKey.PanLeft));
            Assert.Equal(400, v.Start, 9);
            Assert.True(v.Key(ViewportKey.ZoomIn));
            Assert.Equal(100, v.Span, 9);
            Assert.Equal(500, v.FromFraction(0.5), 9);
            Assert.True(v.Key(ViewportKey.ZoomOut));
            Assert.Equal(200, v.Span, 9);
            Assert.Equal(ChangeSource.Keyboard, v.LastSource);
        }

        [Fact]
        public void HomeAndEndJumpToTheDataEnds()
        {
            ViewportModel v = Whole();
            v.Show(400, 600, ChangeSource.Program);
            v.Key(ViewportKey.Home);
            Assert.Equal(0, v.Start, 9);
            v.Key(ViewportKey.End);
            Assert.Equal(1000, v.End, 9);
        }

        [Fact]
        public void TheResetKeyResets()
        {
            ViewportModel v = Whole();
            v.Show(400, 600, ChangeSource.Program);
            v.Key(ViewportKey.Reset);
            Assert.False(v.Zoomed);
            Assert.Equal(1000, v.Span, 9);
        }

        [Fact]
        public void TheVersionMovesWithTheWindowAndTheDataOnly()
        {
            ViewportModel v = Whole();
            int before = v.Version;
            v.PanBy(0.5, ChangeSource.Pointer); // shows it all: nothing moves
            Assert.Equal(before, v.Version);
            v.ZoomAt(2, 0.5, ChangeSource.Pointer);
            Assert.True(v.Version > before);
            before = v.Version;
            v.SetData(0, 1000);
            Assert.Equal(before, v.Version);
            v.SetData(0, 2000);
            Assert.True(v.Version > before);
        }

        [Fact]
        public void FollowSetAfterGoingToTheEndStaysOnWithoutAutoFollow()
        {
            // the PSG Solution window's follow toggle: AutoFollow is off there
            var v = new ViewportModel(0, 1000, 100) { AutoFollow = false };
            v.Key(ViewportKey.End, ChangeSource.Pointer);
            v.Follow = true;
            v.SetData(0, 1100);
            Assert.True(v.Follow);
            Assert.Equal(1100, v.End, 9);
        }

        [Fact]
        public void TheDataGrowingIsNotAMove()
        {
            var v = new ViewportModel(0, 1000, 100) { Follow = true };
            v.SetData(0, 1001);
            int moves = v.MoveVersion, versions = v.Version;
            v.SetData(0, 1100);
            v.SetDefaultSpan(200);
            Assert.Equal(moves, v.MoveVersion);
            Assert.True(v.Version > versions);
            v.PanBy(-0.5, ChangeSource.Pointer);
            Assert.True(v.MoveVersion > moves);
            moves = v.MoveVersion;
            v.Follow = false;
            Assert.Equal(moves, v.MoveVersion);
            v.Reset(ChangeSource.Pointer);
            Assert.True(v.MoveVersion > moves);
        }

        [Fact]
        public void BadInputChangesNothing()
        {
            ViewportModel v = Whole();
            v.Show(100, 200, ChangeSource.Program);
            int before = v.Version;
            Assert.False(v.ZoomAt(0, 0.5, ChangeSource.Pointer));
            Assert.False(v.ZoomAt(double.NaN, 0.5, ChangeSource.Pointer));
            Assert.False(v.Show(double.NaN, 3, ChangeSource.Pointer));
            Assert.False(v.SetData(double.NaN, 3));
            Assert.False(v.Wheel(0, 0.5, ChangeSource.Pointer));
            foreach (double bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                Assert.False(v.ZoomAt(2, bad, ChangeSource.Agent));
                Assert.False(v.ZoomAtValue(2, bad, ChangeSource.Agent));
                Assert.False(v.Wheel(bad, 0.5, ChangeSource.Agent));
                Assert.False(v.Wheel(1, bad, ChangeSource.Agent));
                Assert.False(v.PanBy(bad, ChangeSource.Agent));
                Assert.False(v.PanByData(bad, ChangeSource.Agent));
                Assert.False(v.PanTo(bad, ChangeSource.Agent));
                Assert.False(v.ScrollTo(bad, ChangeSource.Agent));
                Assert.False(v.Show(bad, 300, ChangeSource.Agent));
                Assert.False(v.Show(100, bad, ChangeSource.Agent));
                Assert.False(v.SetData(0, bad));
            }

            Assert.Equal(before, v.Version);
            Assert.Equal(100, v.Start, 9);
            Assert.Equal(100, v.Span, 9);
        }

        [Fact]
        public void EmptyDataStillHasAWindow()
        {
            var v = new ViewportModel();
            Assert.True(v.Span > 0);
            Assert.Equal(0, v.Start, 9);
            v.ZoomAt(2, 0.5, ChangeSource.Pointer);
            Assert.True(v.Span > 0);
            Assert.Equal(0, v.Start, 9);
        }

        [Fact]
        public void ZoomAtAValueOutsideTheWindowZoomsAboutTheMiddle()
        {
            ViewportModel v = Whole();
            v.Show(400, 600, ChangeSource.Program);
            v.ZoomAtValue(2, 900, ChangeSource.Program);
            Assert.Equal(500, v.FromFraction(0.5), 9);
            v.ZoomAtValue(2, 450, ChangeSource.Program);
            Assert.Equal(450, v.FromFraction(v.ToFraction(450)), 9);
            Assert.Equal(50, v.Span, 9);
        }
    }
}
