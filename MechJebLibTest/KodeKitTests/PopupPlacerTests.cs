/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's popup placement (drop-down lists, dialogs, the toast stack, the tooltip's clamp), without KSP or Unity.</summary>
    public class PopupPlacerTests
    {
        // a 1920 x 1080 canvas centred on its origin, as KSP's UI canvas is
        private static readonly FrameBox _screen = new FrameBox(-960, 540, 960, -540);

        // a drop-down's list: 150 x 160 units before its scale
        private const float W = 150, H = 160;

        // a 150 x 20 button whose top-left corner is at (left, top)
        private static FrameBox Button(float left, float top) => new FrameBox(left, top, left + 150, top - 20);

        private static void OnScreen(PopupPlacement p, FrameBox screen, float margin = 0)
        {
            Assert.True(p.Box.Left >= screen.Left + margin - 1e-3f, $"left {p}");
            Assert.True(p.Box.Right <= screen.Right - margin + 1e-3f, $"right {p}");
            Assert.True(p.Box.Bottom >= screen.Bottom + margin - 1e-3f, $"bottom {p}");
            Assert.True(p.Box.Top <= screen.Top - margin + 1e-3f, $"top {p}");
        }

        private static void Size(PopupPlacement p, float scale)
        {
            Assert.Equal(W * scale, p.Box.Width, 3);
            Assert.Equal(H * scale, p.Box.Height, 3);
        }

        // ---- below, the drop-down's side ----

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void InTheMiddleItOpensBelowItsAnchorLeftEdgesLinedUpAtItsScale(float scale)
        {
            FrameBox b = Button(-100, 100);
            PopupPlacement p = PopupPlacer.Place(b, W, H, _screen, scale);
            Assert.Equal(PopupSide.Below, p.Side);
            Assert.False(p.Flipped);
            Assert.False(p.Clamped);
            Assert.True(p.Fits);
            Assert.Equal(b.Left, p.Box.Left, 3);
            Assert.Equal(b.Bottom, p.Box.Top, 3);
            Size(p, scale);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void AtTheBottomEdgeItFlipsAboveItsAnchor(float scale)
        {
            FrameBox b = Button(-100, -500);
            PopupPlacement p = PopupPlacer.Place(b, W, H, _screen, scale);
            Assert.Equal(PopupSide.Above, p.Side);
            Assert.True(p.Flipped);
            Assert.False(p.Clamped);
            Assert.Equal(b.Top, p.Box.Bottom, 3);
            Assert.Equal(b.Left, p.Box.Left, 3);
            Size(p, scale);
            OnScreen(p, _screen);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void AtTheTopEdgeItStaysBelow(float scale)
        {
            FrameBox b = Button(-100, 540);
            PopupPlacement p = PopupPlacer.Place(b, W, H, _screen, scale);
            Assert.Equal(PopupSide.Below, p.Side);
            Assert.Equal(520, p.Box.Top, 3);
            OnScreen(p, _screen);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void AtTheRightEdgeItSlidesLeftUntilWholeOnScreen(float scale)
        {
            FrameBox b = Button(900, 100); // the button itself runs off the right edge
            PopupPlacement p = PopupPlacer.Place(b, W, H, _screen, scale);
            Assert.Equal(PopupSide.Below, p.Side);
            Assert.True(p.Clamped);
            Assert.Equal(960, p.Box.Right, 3);
            Assert.Equal(b.Bottom, p.Box.Top, 3);
            Size(p, scale);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void AtTheLeftEdgeItSlidesRightUntilWholeOnScreen(float scale)
        {
            FrameBox b = Button(-1000, 100);
            PopupPlacement p = PopupPlacer.Place(b, W, H, _screen, scale);
            Assert.True(p.Clamped);
            Assert.Equal(-960, p.Box.Left, 3);
            OnScreen(p, _screen);
        }

        [Theory]
        [InlineData(1f, -960, -520)]
        [InlineData(1.5f, -960, -520)]
        [InlineData(2f, -960, -520)]
        [InlineData(1f, 810, -520)]
        [InlineData(1.5f, 810, -520)]
        [InlineData(2f, 810, -520)]
        public void InABottomCornerItFlipsAboveAndStaysOnScreen(float scale, float left, float top)
        {
            PopupPlacement p = PopupPlacer.Place(Button(left, top), W, H, _screen, scale);
            Assert.True(p.Flipped);
            Assert.Equal(-520, p.Box.Bottom, 3);
            OnScreen(p, _screen);
            Size(p, scale);
        }

        [Theory]
        [InlineData(1f, -960)]
        [InlineData(1.5f, -960)]
        [InlineData(2f, -960)]
        [InlineData(1f, 810)]
        [InlineData(1.5f, 810)]
        [InlineData(2f, 810)]
        public void InATopCornerItStaysBelowAndOnScreen(float scale, float left)
        {
            PopupPlacement p = PopupPlacer.Place(Button(left, 540), W, H, _screen, scale);
            Assert.False(p.Flipped);
            Assert.Equal(520, p.Box.Top, 3);
            OnScreen(p, _screen);
        }

        [Fact]
        public void ItFlipsOnlyWhenTheOtherSideHasRoom()
        {
            // 100 units below, 200 above: a 160-unit list doesn't fit below and fits above
            var screen = new FrameBox(-500, 220, 500, -100);
            PopupPlacement p = PopupPlacer.Place(Button(0, 20), W, H, screen);
            Assert.Equal(PopupSide.Above, p.Side);
            // 100 below and 50 above: neither fits, below has more room: it stays below, slid up onto the screen
            screen = new FrameBox(-500, 70, 500, -100);
            p = PopupPlacer.Place(Button(0, 20), W, H, screen);
            Assert.Equal(PopupSide.Below, p.Side);
            Assert.True(p.Clamped);
            Assert.Equal(-100, p.Box.Bottom, 3);
        }

        [Fact]
        public void WhenNeitherSideFitsItTakesTheSideWithMoreRoom()
        {
            // 60 below, 100 above, a 160-unit list
            var screen = new FrameBox(-500, 120, 500, -60);
            PopupPlacement p = PopupPlacer.Place(Button(0, 20), W, H, screen);
            Assert.Equal(PopupSide.Above, p.Side);
            Assert.Equal(120, p.Box.Top, 3);
        }

        [Fact]
        public void APopupLargerThanTheScreenKeepsItsTopLeftCornerOnIt()
        {
            var screen = new FrameBox(0, 100, 100, 0);
            PopupPlacement p = PopupPlacer.Place(Button(50, 50), 300, 300, screen);
            Assert.False(p.Fits);
            Assert.Equal(0, p.Box.Left, 3);
            Assert.Equal(100, p.Box.Top, 3);
        }

        [Fact]
        public void TheOffsetAndTheMarginKeepTheirDistances()
        {
            PopupPlacement p = PopupPlacer.Place(Button(-100, 100), W, H, _screen, 1, PopupSide.Below, PopupAlign.Start, 4);
            Assert.Equal(76, p.Box.Top, 3);
            // at the right edge with an 8-unit margin
            p = PopupPlacer.Place(Button(900, 100), W, H, _screen, 1, PopupSide.Below, PopupAlign.Start, 0, 8);
            Assert.Equal(952, p.Box.Right, 3);
            // at the bottom edge: the room below counts the margin, so a list that fits only without it flips
            p = PopupPlacer.Place(Button(-100, -360), W, H, _screen, 1, PopupSide.Below, PopupAlign.Start, 0, 8);
            Assert.True(p.Flipped);
        }

        [Fact]
        public void CentreAndEndAlignmentsLineUpWithTheAnchor()
        {
            FrameBox b = Button(-100, 100);
            Assert.Equal(-100 + 75 - 50, PopupPlacer.Place(b, 100, H, _screen, 1, PopupSide.Below, PopupAlign.Centre).Box.Left, 3);
            Assert.Equal(50 - 100, PopupPlacer.Place(b, 100, H, _screen, 1, PopupSide.Below, PopupAlign.End).Box.Left, 3);
        }

        // ---- right and left, a submenu's sides ----

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void ARightPopupFlipsLeftAtTheRightEdgeTopEdgesLinedUp(float scale)
        {
            FrameBox b = Button(0, 100);
            PopupPlacement p = PopupPlacer.Place(b, W, H, _screen, scale, PopupSide.Right);
            Assert.Equal(PopupSide.Right, p.Side);
            Assert.Equal(b.Right, p.Box.Left, 3);
            Assert.Equal(b.Top, p.Box.Top, 3);

            b = Button(800, 100);
            p = PopupPlacer.Place(b, W, H, _screen, scale, PopupSide.Right);
            Assert.Equal(PopupSide.Left, p.Side);
            Assert.Equal(b.Left, p.Box.Right, 3);
            OnScreen(p, _screen);
        }

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void ALeftPopupFlipsRightAtTheLeftEdgeAndSlidesUpAtTheBottom(float scale)
        {
            FrameBox b = Button(-960, -500);
            PopupPlacement p = PopupPlacer.Place(b, W, H, _screen, scale, PopupSide.Left);
            Assert.Equal(PopupSide.Right, p.Side);
            Assert.Equal(b.Right, p.Box.Left, 3);
            Assert.Equal(-540, p.Box.Bottom, 3);
            OnScreen(p, _screen);
        }

        [Fact]
        public void APointAnchorIsARectOfNoSize()
        {
            var point = new FrameBox(10, 10, 10, 10);
            PopupPlacement p = PopupPlacer.Place(point, W, H, _screen);
            Assert.Equal(10, p.Box.Left, 3);
            Assert.Equal(10, p.Box.Top, 3);
        }

        [Fact]
        public void AScaleOfZeroOrLessCountsAsOne()
        {
            Assert.Equal(H, PopupPlacer.Place(Button(0, 100), W, H, _screen, 0).Box.Height, 3);
            Assert.Equal(H, PopupPlacer.Place(Button(0, 100), W, H, _screen, -2).Box.Height, 3);
        }

        // ---- the list's height ----

        [Fact]
        public void TheTallestListIsTheLargerRoomLessTheMarginWithAFloor()
        {
            // 480 below, 580 above
            Assert.Equal(572, PopupPlacer.MaxHeight(Button(0, -40), _screen, 8, 200), 3);
            Assert.Equal(572, PopupPlacer.MaxHeight(Button(0, 60), _screen, 8, 200), 3);
            // 1060 above at the bottom edge
            Assert.Equal(1052, PopupPlacer.MaxHeight(Button(0, -520), _screen, 8, 200), 3);
            // a screen of 150 units: the floor is the screen's height
            var tiny = new FrameBox(0, 150, 300, 0);
            Assert.Equal(150, PopupPlacer.MaxHeight(new FrameBox(0, 80, 100, 70), tiny, 8, 200), 3);
        }

        [Theory]
        [InlineData(-552)]
        [InlineData(-700)]
        [InlineData(700)]
        public void AListWhoseAnchorIsOffTheScreenIsNoTallerThanTheScreenLessTheMargin(float top)
        {
            // the button below the bottom edge (or above the top): the room on its far side is the screen's height, no more
            Assert.Equal(1072, PopupPlacer.MaxHeight(Button(0, top), _screen, 8, 200), 3);
        }

        // ---- the dialog, the toasts, the tooltip ----

        [Theory]
        [InlineData(1f)]
        [InlineData(1.5f)]
        [InlineData(2f)]
        public void ADialogIsCentredOnWholeUnits(float scale)
        {
            PopupPlacement p = PopupPlacer.Centre(341, 121, _screen, scale);
            Assert.Equal(System.Math.Round(-341 * scale / 2), p.Box.Left, 3);
            Assert.Equal(System.Math.Round(121 * scale / 2), p.Box.Top, 3);
            Assert.Equal(341 * scale, p.Box.Width, 3);
            OnScreen(p, _screen);
        }

        [Theory]
        [InlineData(PopupCorner.BottomRight, 1f)]
        [InlineData(PopupCorner.BottomLeft, 1.5f)]
        [InlineData(PopupCorner.TopRight, 2f)]
        [InlineData(PopupCorner.TopLeft, 1f)]
        public void TheToastStackSitsInItsCornerTheMarginIn(PopupCorner corner, float scale)
        {
            PopupPlacement p = PopupPlacer.Corner(300, 100, _screen, corner, 16, scale);
            bool right = corner == PopupCorner.BottomRight || corner == PopupCorner.TopRight;
            bool bottom = corner == PopupCorner.BottomRight || corner == PopupCorner.BottomLeft;
            if (right) Assert.Equal(944, p.Box.Right, 3);
            else Assert.Equal(-944, p.Box.Left, 3);
            if (bottom) Assert.Equal(-524, p.Box.Bottom, 3);
            else Assert.Equal(524, p.Box.Top, 3);
            Assert.Equal(300 * scale, p.Box.Width, 3);
            Assert.False(p.Clamped);
        }

        [Fact]
        public void ATallToastStackStaysOnScreenFromItsTop()
        {
            PopupPlacement p = PopupPlacer.Corner(300, 2000, _screen, PopupCorner.BottomRight, 16);
            Assert.True(p.Clamped);
            Assert.Equal(540, p.Box.Top, 3);
        }

        [Fact]
        public void TheClampMovesATooltipBackOnScreenAndKeepsItsSize()
        {
            var tip = new FrameBox(900, -500, 1100, -560);
            FrameBox c = PopupPlacer.Clamp(tip, _screen);
            Assert.Equal(960, c.Right, 3);
            Assert.Equal(-540, c.Bottom, 3);
            Assert.Equal(200, c.Width, 3);
            Assert.Equal(60, c.Height, 3);
            // inside: unchanged
            var inside = new FrameBox(0, 0, 100, -50);
            Assert.Equal(inside.Left, PopupPlacer.Clamp(inside, _screen).Left);
            Assert.Equal(inside.Top, PopupPlacer.Clamp(inside, _screen).Top);
        }

        [Fact]
        public void TheOppositeOfEachSide()
        {
            Assert.Equal(PopupSide.Above, PopupPlacer.Opposite(PopupSide.Below));
            Assert.Equal(PopupSide.Below, PopupPlacer.Opposite(PopupSide.Above));
            Assert.Equal(PopupSide.Left, PopupPlacer.Opposite(PopupSide.Right));
            Assert.Equal(PopupSide.Right, PopupPlacer.Opposite(PopupSide.Left));
        }
    }
}
