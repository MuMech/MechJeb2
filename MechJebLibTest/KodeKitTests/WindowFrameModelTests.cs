/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's window drag, clamp, snapping and saved position, and the resize grip, without KSP or Unity.</summary>
    public class WindowFrameModelTests
    {
        // a 1920 x 1080 canvas centred on its origin, as KSP's UI canvas is
        private static readonly FrameBox _canvas = new FrameBox(-960, 540, 960, -540);

        private static WindowFrameModel Make(float x = 0, float y = 0, float width = 300, float height = 200, float scale = 1,
            float snap = 0)
        {
            var f = new WindowFrameModel { SnapDistance = snap };
            f.Load(x, y, width, height, scale, _canvas);
            return f;
        }

        // ---- clamp ----

        [Fact]
        public void ALeftDragKeepsMinVisibleUnitsOnTheCanvas()
        {
            WindowFrameModel f = Make();
            f.Place(-5000, 0);
            Assert.Equal(-960 - 300 + 20, f.X);
        }

        [Fact]
        public void ARightDragKeepsMinVisibleUnitsOnTheCanvas()
        {
            WindowFrameModel f = Make();
            f.Place(5000, 0);
            Assert.Equal(960 - 20, f.X);
        }

        [Fact]
        public void TheTitleBarNeverGoesAboveTheTop()
        {
            WindowFrameModel f = Make();
            f.Place(0, 5000);
            Assert.Equal(540, f.Y);
        }

        [Fact]
        public void TheTopEdgeStaysMinVisibleUnitsAboveTheBottom()
        {
            WindowFrameModel f = Make();
            f.Place(0, -5000);
            Assert.Equal(-540 + 20, f.Y);
        }

        [Fact]
        public void APlaceInsideTheCanvasIsKept()
        {
            WindowFrameModel f = Make();
            f.Place(-100, 50);
            Assert.Equal(-100, f.X);
            Assert.Equal(50, f.Y);
        }

        [Fact]
        public void ClampReportsWhetherTheWindowMoved()
        {
            WindowFrameModel f = Make(0, 0);
            Assert.False(f.Clamp());
            f.Load(2000, 0, 300, 200, 1, _canvas);
            Assert.True(f.Clamp());
            Assert.Equal(940, f.X);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void TheLeftClampUsesTheScaledWidth(float scale)
        {
            WindowFrameModel f = Make(scale: scale);
            f.Place(-5000, 0);
            Assert.Equal(-960 - 300 * scale + 20, f.X);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void TheRightAndBottomClampsDontDependOnTheScale(float scale)
        {
            WindowFrameModel f = Make(scale: scale);
            f.Place(5000, -5000);
            Assert.Equal(940, f.X);
            Assert.Equal(-520, f.Y);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void TheBoxIsTheScaledSizeFromTheTopLeftCorner(float scale)
        {
            FrameBox b = Make(10, 20, scale: scale).Box;
            Assert.Equal(10, b.Left);
            Assert.Equal(20, b.Top);
            Assert.Equal(10 + 300 * scale, b.Right);
            Assert.Equal(20 - 200 * scale, b.Bottom);
        }

        // ---- drag ----

        [Fact]
        public void ADragFollowsThePointer()
        {
            WindowFrameModel f = Make();
            f.BeginDrag();
            f.DragBy(30, -40);
            f.DragBy(5, 5);
            Assert.Equal(35, f.X);
            Assert.Equal(-35, f.Y);
            Assert.True(f.Dragging);
            f.EndDrag();
            Assert.False(f.Dragging);
        }

        [Fact]
        public void DraggingPastAnEdgeAndBackMovesBackAtOnce()
        {
            WindowFrameModel f = Make(900);
            f.BeginDrag();
            f.DragBy(500, 0); // stops at 940
            Assert.Equal(940, f.X);
            f.DragBy(-100, 0);
            Assert.Equal(840, f.X);
        }

        [Fact]
        public void ADragOutsideBeginAndEndMovesWithoutSnapping()
        {
            WindowFrameModel f = Make(-650, snap: 8); // left edge 310 units from the canvas's
            f.DragBy(-305, 0);
            Assert.Equal(-955, f.X);
        }

        [Fact]
        public void ADragKeepsTheWindowWhereItWasRelativeToThePressPoint()
        {
            WindowFrameModel f = Make(10, 20);
            f.BeginDrag(100, 15); // pressed 90 right of and 5 below the corner
            f.DragTo(130, -25);   // the first event, already past the event system's threshold
            Assert.Equal(40, f.X);
            Assert.Equal(-20, f.Y);
        }

        [Fact]
        public void ManySmallMovesDontDrift()
        {
            WindowFrameModel f = Make(0.1f, 0.3f);
            f.BeginDrag(0.7f, 0.9f);
            float x = 0.7f, y = 0.9f;
            for (int i = 0; i < 10000; i++)
            {
                x += 0.013f;
                y -= 0.007f;
                f.DragTo(x, y);
            }
            Assert.Equal(0.1f + (x - 0.7f), f.X, 4);
            Assert.Equal(0.3f + (y - 0.9f), f.Y, 4);
        }

        [Fact]
        public void PastAnEdgeAndBackByPointerPositionsMovesBackAtOnce()
        {
            WindowFrameModel f = Make(900);
            f.BeginDrag(950, 0);
            f.DragTo(1450, 0); // stops at 940
            Assert.Equal(940, f.X);
            f.DragTo(1350, 0);
            Assert.Equal(840, f.X);
        }

        [Fact]
        public void DragToOutsideADragDoesNothing()
        {
            WindowFrameModel f = Make(10, 20);
            f.DragTo(500, 500);
            Assert.Equal(10, f.X);
            Assert.Equal(20, f.Y);
            Assert.False(f.Dragging);
        }

        [Fact]
        public void APressPointDragSnapsLikeADeltaDrag()
        {
            WindowFrameModel f = Make(-940, snap: 8);
            f.BeginDrag(-900, 0);
            f.DragTo(-915, 0); // left edge 5 units inside
            Assert.Equal(-960, f.X);
            f.DragTo(-885, 0); // 35 units in: lets go
            Assert.Equal(-925, f.X);
        }

        // ---- snapping ----

        [Fact]
        public void WithSnappingOffTheWindowStaysWhereThePointerPutsIt()
        {
            WindowFrameModel f = Make(-950);
            f.BeginDrag();
            f.DragBy(-5, 0);
            Assert.Equal(-955, f.X);
        }

        [Fact]
        public void AnEdgeWithinTheDistanceSnapsToTheCanvasEdge()
        {
            WindowFrameModel f = Make(-940, snap: 8);
            f.BeginDrag();
            f.DragBy(-15, 0); // left edge 5 units inside
            Assert.Equal(-960, f.X);
        }

        [Fact]
        public void AnEdgeBeyondTheDistanceDoesntSnap()
        {
            WindowFrameModel f = Make(-940, snap: 8);
            f.BeginDrag();
            f.DragBy(-10, 0); // 10 units inside
            Assert.Equal(-950, f.X);
        }

        [Fact]
        public void TheRightEdgeSnapsToTheCanvasRight()
        {
            WindowFrameModel f = Make(600, snap: 8);
            f.BeginDrag();
            f.DragBy(55, 0); // right edge at 955
            Assert.Equal(960 - 300, f.X);
        }

        [Fact]
        public void TheTopAndBottomEdgesSnapToTheCanvas()
        {
            WindowFrameModel f = Make(0, 500, snap: 8);
            f.BeginDrag();
            f.DragBy(0, 35); // top at 535
            Assert.Equal(540, f.Y);
            f.DragBy(0, -871); // the pointer's top at -336: the bottom 4 units above the canvas's
            Assert.Equal(-540 + 200, f.Y);
        }

        [Fact]
        public void ASnapLetsGoOnceThePointerIsBeyondTheDistance()
        {
            WindowFrameModel f = Make(-940, snap: 8);
            f.BeginDrag();
            f.DragBy(-15, 0);
            Assert.Equal(-960, f.X);
            f.DragBy(30, 0); // the pointer's place is -925: 35 units in
            Assert.Equal(-925, f.X);
        }

        [Fact]
        public void TheSnappedEdgeUsesTheScaledSize()
        {
            WindowFrameModel f = Make(300, scale: 2, snap: 8); // 600 wide: right edge at 900
            f.BeginDrag();
            f.DragBy(55, 0);
            Assert.Equal(960 - 600, f.X);
        }

        [Fact]
        public void AWindowSnapsBesideAnother()
        {
            WindowFrameModel f = Make(0, 100, snap: 8);
            f.AddOther(new FrameBox(-400, 150, -100, 0)); // to its left, facing it
            f.BeginDrag();
            f.DragBy(-94, 0); // left edge at -94: 6 units from the other's right
            Assert.Equal(-100, f.X);
        }

        [Fact]
        public void AWindowSnapsBelowAnother()
        {
            WindowFrameModel f = Make(0, -100, snap: 8);
            f.AddOther(new FrameBox(50, 300, 350, -90)); // above it, overlapping horizontally
            f.BeginDrag();
            f.DragBy(0, 5); // top at -95: 5 units below the other's bottom
            Assert.Equal(-90, f.Y);
        }

        [Fact]
        public void AlignedEdgesSnapToo()
        {
            WindowFrameModel f = Make(-397, -100, snap: 8);
            f.AddOther(new FrameBox(-400, 300, -100, -150));
            f.BeginDrag();
            f.DragBy(0, 0);
            Assert.Equal(-400, f.X); // left edges aligned
        }

        [Fact]
        public void AWindowThatDoesntFaceTheOtherDoesntSnapToIt()
        {
            WindowFrameModel f = Make(0, -200, snap: 8); // top -200, bottom -400
            f.AddOther(new FrameBox(-400, 300, -100, 0)); // far above: no vertical overlap
            f.BeginDrag();
            f.DragBy(-95, 0);
            Assert.Equal(-95, f.X);
        }

        [Fact]
        public void TheNearestEdgeWins()
        {
            WindowFrameModel f = Make(0, 100, snap: 8);
            f.AddOther(new FrameBox(-400, 150, -100, 0));  // its right at -100
            f.AddOther(new FrameBox(-500, 150, -103, -10)); // its right at -103
            f.BeginDrag();
            f.DragBy(-102, 0);
            Assert.Equal(-103, f.X);
        }

        [Fact]
        public void TheClampAppliesAfterTheSnap()
        {
            WindowFrameModel f = Make(800, snap: 8);
            f.AddOther(new FrameBox(945, 150, 1300, -100)); // a window mostly off the canvas
            f.BeginDrag();
            f.DragBy(140, 0); // left edge at 940, 5 from the other's left: snapping there goes past the clamp
            Assert.Equal(940, f.X);
        }

        [Fact]
        public void TheOtherWindowsAreForgottenOnClear()
        {
            WindowFrameModel f = Make(snap: 8);
            f.AddOther(new FrameBox(0, 0, 1, -1));
            Assert.Equal(1, f.OtherCount);
            f.ClearOthers();
            Assert.Equal(0, f.OtherCount);
        }

        // ---- the saved position ----

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void TheImguiPositionIsScreenPixelsOverTheScale(float scale)
        {
            WindowFrameModel.ToImgui(300, 120, scale, out float x, out float y);
            Assert.Equal(300 / scale, x);
            Assert.Equal(120 / scale, y);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void ASavedPositionRoundTrips(float scale)
        {
            WindowFrameModel.FromImgui(200, 73.5f, scale, out float sx, out float sy);
            WindowFrameModel.ToImgui(sx, sy, scale, out float x, out float y);
            Assert.Equal(200, x, 4);
            Assert.Equal(73.5, y, 4);
        }

        [Fact]
        public void FlipYTurnsATopDownPointIntoABottomUpOne()
        {
            Assert.Equal(1080 - 110, WindowFrameModel.FlipY(110, 1080));
            Assert.Equal(110, WindowFrameModel.FlipY(WindowFrameModel.FlipY(110, 1080), 1080));
        }

        // ---- resize ----

        [Fact]
        public void AResizeKeepsTheGrabOffset()
        {
            var r = new ResizeModel();
            r.Begin(295, 196, 300, 200); // pressed 5 and 4 units inside the corner
            Assert.Equal(300, r.Width);
            Assert.Equal(200, r.Height);
            Assert.True(r.DragTo(345, 246));
            Assert.Equal(350, r.Width);
            Assert.Equal(250, r.Height);
        }

        [Fact]
        public void AResizeNeverGoesBelowTheMinimum()
        {
            var r = new ResizeModel { MinWidth = 120, MinHeight = 80 };
            r.Begin(300, 200, 300, 200);
            r.DragTo(10, 10);
            Assert.Equal(120, r.Width);
            Assert.Equal(80, r.Height);
        }

        [Fact]
        public void WithoutAMinimumAPointerPastTheLeftEdgeGivesANegativeWidth()
        {
            var r = new ResizeModel();
            r.Begin(300, 200, 300, 200);
            r.DragTo(-20, -30);
            Assert.Equal(-20, r.Width);
            Assert.Equal(-30, r.Height);
        }

        [Fact]
        public void AResizeStartsAtTheWindowsSizeClampedToTheMinimum()
        {
            var r = new ResizeModel { MinWidth = 400 };
            r.Begin(300, 200, 300, 200);
            Assert.Equal(400, r.Width);
        }

        [Fact]
        public void APointerMoveOutsideAResizeDoesNothing()
        {
            var r = new ResizeModel();
            Assert.False(r.DragTo(10, 10));
            Assert.False(r.End());
            r.Begin(0, 0, 100, 100);
            Assert.True(r.End());
            Assert.False(r.Dragging);
            Assert.False(r.DragTo(10, 10));
        }

        [Fact]
        public void ALocalPointIsMeasuredFromTheTopLeftCornerDown()
        {
            // pivot top-left (a KodeWindow's): local (10, -20) is 10 right, 20 down
            ResizeModel.FromLocal(10, -20, 0, 1, 300, 200, out float x, out float y);
            Assert.Equal(10, x);
            Assert.Equal(20, y);
            // pivot centred: local (0, 0) is the middle
            ResizeModel.FromLocal(0, 0, 0.5f, 0.5f, 300, 200, out x, out y);
            Assert.Equal(150, x);
            Assert.Equal(100, y);
        }
    }
}
