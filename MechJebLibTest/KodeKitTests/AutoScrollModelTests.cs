/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's scroll while a drag is near a list's edge, without KSP or Unity.</summary>
    public class AutoScrollModelTests
    {
        // a 200-high viewport, 20-high zones, 50 to 500 units a second, no wait unless a test sets one
        private static AutoScrollModel Model(float delay = 0) =>
            new AutoScrollModel { Edge = 20, MinSpeed = 50, MaxSpeed = 500, Delay = delay, MaxStep = 0.1f };

        [Fact]
        public void TheMiddleOfTheViewportDoesntScroll()
        {
            AutoScrollModel m = Model();
            Assert.Equal(0, m.Speed(100, 200));
            Assert.Equal(0, m.Speed(20, 200));
            Assert.Equal(0, m.Speed(180, 200));
        }

        [Fact]
        public void NearTheTopScrollsUpNearTheBottomDown()
        {
            AutoScrollModel m = Model();
            Assert.True(m.Speed(10, 200) < 0);
            Assert.True(m.Speed(190, 200) > 0);
        }

        [Fact]
        public void TheSpeedGrowsTowardsTheEdge()
        {
            AutoScrollModel m = Model();
            Assert.InRange(m.Speed(180.01f, 200), 50, 50.01f);
            float half = m.Speed(190, 200), edge = m.Speed(200, 200);
            Assert.Equal(50 + 450 * 0.25f, half, 3);
            Assert.Equal(500, edge, 3);
            Assert.True(m.Speed(195, 200) > half);
        }

        [Fact]
        public void PastTheEdgeIsTheFastest()
        {
            AutoScrollModel m = Model();
            Assert.Equal(-500, m.Speed(-40, 200), 3);
            Assert.Equal(500, m.Speed(260, 200), 3);
        }

        [Fact]
        public void TheStepIsTheSpeedTimesTheTimePassed()
        {
            AutoScrollModel m = Model();
            Assert.Equal(500 * 0.02f, m.Step(200, 200, 0.02f), 4);
            Assert.Equal(-500 * 0.05f, m.Step(0, 200, 0.05f), 4);
        }

        [Fact]
        public void ALongFrameCountsAsMaxStep()
        {
            AutoScrollModel m = Model();
            Assert.Equal(500 * 0.1f, m.Step(200, 200, 2f), 4);
        }

        [Fact]
        public void AZoneHeldLessThanTheDelayDoesntScroll()
        {
            AutoScrollModel m = Model(0.15f);
            Assert.Equal(0, m.Step(200, 200, 0.05f));
            Assert.Equal(0, m.Step(200, 200, 0.05f));
            // 0.15 s reached half way through this frame: only that half scrolls
            Assert.Equal(500 * 0.05f, m.Step(200, 200, 0.1f), 4);
            Assert.Equal(500 * 0.02f, m.Step(200, 200, 0.02f), 4);
        }

        [Fact]
        public void LeavingTheZoneStartsTheWaitAgain()
        {
            AutoScrollModel m = Model(0.1f);
            m.Step(200, 200, 0.1f);
            Assert.True(m.Step(200, 200, 0.02f) > 0);
            Assert.Equal(0, m.Step(100, 200, 0.02f));
            Assert.Equal(0, m.Step(200, 200, 0.05f));
        }

        [Fact]
        public void SwitchingEdgesStartsTheWaitAgain()
        {
            AutoScrollModel m = Model(0.1f);
            m.Step(200, 200, 0.1f);
            Assert.Equal(0, m.Step(0, 200, 0.05f));
        }

        [Fact]
        public void ResetStartsTheWaitAgain()
        {
            AutoScrollModel m = Model(0.1f);
            m.Step(200, 200, 0.1f);
            m.Reset();
            Assert.Equal(0, m.Step(200, 200, 0.05f));
        }

        [Fact]
        public void ASmallViewportSplitsAtItsMiddle()
        {
            AutoScrollModel m = Model();
            Assert.True(m.Speed(9, 20) < 0);
            Assert.True(m.Speed(11, 20) > 0);
        }

        [Fact]
        public void TickScrollsTheListAndStopsAtItsEnds()
        {
            AutoScrollModel m = Model();
            var list = new VirtualListModel(100, 20, 200);
            Assert.True(m.Tick(list, 200, 0.1f));
            Assert.Equal(50, list.Offset, 3);
            Assert.Equal(ChangeSource.Pointer, list.LastSource);
            Assert.True(m.Tick(list, 0, 0.1f));
            Assert.Equal(0, list.Offset, 3);
            Assert.False(m.Tick(list, 0, 0.1f));
            list.SetOffset(list.MaxOffset);
            Assert.False(m.Tick(list, 200, 0.1f));
        }

        [Fact]
        public void AListShorterThanItsViewportDoesntScroll()
        {
            AutoScrollModel m = Model();
            var list = new VirtualListModel(3, 20, 200);
            Assert.False(m.Tick(list, 200, 0.1f));
            Assert.Equal(0, list.Offset);
        }
    }
}
