/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>When a tooltip or hover card opens and closes (HoverModel), without KSP or Unity.</summary>
    public class HoverModelTests
    {
        private static readonly object _a = "a", _b = "b";

        private static HoverModel Card() => new HoverModel { OpenDelay = 1, CloseDelay = 0.2 };

        [Fact]
        public void TheCardOpensOnceThePointerRestsForTheOpenDelay()
        {
            var m = new HoverModel { OpenDelay = 1 };
            m.Enter(_a, 10);
            Assert.True(m.Waiting);
            Assert.Equal(HoverChange.None, m.Tick(10.99));
            Assert.False(m.IsShown);
            Assert.Equal(HoverChange.Opened, m.Tick(11));
            Assert.Same(_a, m.Shown);
            Assert.Equal(HoverChange.None, m.Tick(12));
        }

        [Fact]
        public void LeavingBeforeTheDelayOpensNothing()
        {
            var m = new HoverModel { OpenDelay = 1 };
            m.Enter(_a, 0);
            m.Leave(_a, 0.5);
            Assert.Equal(HoverChange.None, m.Tick(5));
            Assert.False(m.IsShown);
            Assert.Null(m.Hovered);
        }

        [Fact]
        public void WithNoCloseDelayLeavingClosesTheCardAtOnce()
        {
            var m = new HoverModel { OpenDelay = 1 };
            m.Enter(_a, 0);
            m.Tick(1);
            Assert.Equal(HoverChange.Closed, m.Leave(_a, 2));
            Assert.False(m.IsShown);
        }

        [Fact]
        public void WithACloseDelayTheCardStaysUntilTheDelayIsOver()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            m.Tick(1);
            Assert.Equal(HoverChange.None, m.Leave(_a, 2));
            Assert.True(m.IsShown);
            Assert.True(m.Closing);
            Assert.Equal(HoverChange.None, m.Tick(2.19));
            Assert.Equal(HoverChange.Closed, m.Tick(2.2));
            Assert.False(m.IsShown);
            Assert.Null(m.Hovered);
        }

        [Fact]
        public void ReenteringTheSameTargetWithinTheCloseDelayKeepsTheCard()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            m.Tick(1);
            m.Leave(_a, 2);
            Assert.Equal(HoverChange.None, m.Enter(_a, 2.1));
            Assert.False(m.Closing);
            Assert.Equal(HoverChange.None, m.Tick(5));
            Assert.Same(_a, m.Shown);
        }

        [Fact]
        public void ReenteringTheSameTargetWithinTheCloseDelayKeepsTheWaitGoing()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            m.Leave(_a, 0.5);
            m.Enter(_a, 0.6); // a gap between two cells of one row
            Assert.Equal(1, m.OpensAt);
            Assert.Equal(HoverChange.Opened, m.Tick(1));
        }

        [Fact]
        public void ReenteringAfterTheCloseDelayWaitsAgain()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            m.Tick(1);
            m.Leave(_a, 2);
            m.Tick(3);
            Assert.False(m.IsShown);
            m.Enter(_a, 3);
            Assert.Equal(HoverChange.None, m.Tick(3.5));
            Assert.Equal(HoverChange.Opened, m.Tick(4));
        }

        [Fact]
        public void AnotherTargetEnteredWhileTheCardClosesSwitchesAtOnce()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            m.Tick(1);
            m.Leave(_a, 2);
            Assert.Equal(HoverChange.Switched, m.Enter(_b, 2.05));
            Assert.Same(_b, m.Shown);
            Assert.False(m.Closing);
            Assert.Equal(HoverChange.None, m.Tick(2.5)); // the old leave doesn't close the new card
            Assert.Same(_b, m.Shown);
        }

        [Fact]
        public void AnotherTargetEnteredWhileTheCardIsShownAndNotClosingClosesItAndWaitsAgain()
        {
            var m = new HoverModel { OpenDelay = 1 };
            m.Enter(_a, 0);
            m.Tick(1);
            Assert.Equal(HoverChange.Closed, m.Enter(_b, 2)); // a target inside another
            Assert.False(m.IsShown);
            Assert.True(m.Waiting);
            Assert.Equal(HoverChange.None, m.Tick(2.5));
            Assert.Equal(HoverChange.Opened, m.Tick(3));
            Assert.Same(_b, m.Shown);
        }

        [Fact]
        public void APressClosesTheShownCard()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            m.Tick(1);
            Assert.Equal(HoverChange.Closed, m.Press(1.5));
            Assert.False(m.IsShown);
        }

        [Fact]
        public void APressCancelsTheWaitAndNothingOpensWhileThePointerStays()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            Assert.Equal(HoverChange.None, m.Press(0.5));
            Assert.False(m.Waiting);
            Assert.Equal(HoverChange.None, m.Tick(10));
            Assert.False(m.IsShown);
        }

        [Fact]
        public void AfterAPressLeavingAndEnteringAgainWaitsAndOpens()
        {
            var m = new HoverModel { OpenDelay = 1 };
            m.Enter(_a, 0);
            m.Tick(1);
            m.Press(1.5);
            m.Leave(_a, 2);
            m.Enter(_a, 3);
            Assert.Equal(HoverChange.Opened, m.Tick(4));
        }

        [Fact]
        public void APressedTargetReenteredWithinTheCloseDelayStaysClosed()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            m.Tick(1);
            m.Press(1.5);
            m.Leave(_a, 2);
            m.Enter(_a, 2.1);
            Assert.Equal(HoverChange.None, m.Tick(10));
            Assert.False(m.IsShown);
        }

        [Fact]
        public void APressWithNothingHoveredChangesNothing()
        {
            var m = new HoverModel();
            Assert.Equal(HoverChange.None, m.Press(0));
            m.Enter(_a, 1);
            Assert.Equal(HoverChange.Opened, m.Tick(2));
        }

        [Fact]
        public void ATargetThatGoesAwayClosesItsCardAtOnce()
        {
            HoverModel m = Card();
            m.Enter(_a, 0);
            m.Tick(1);
            Assert.Equal(HoverChange.Closed, m.Cancel(_a));
            Assert.False(m.IsShown);
            Assert.Null(m.Hovered);
        }

        [Fact]
        public void ATargetThatGoesAwayDropsItsWait()
        {
            var m = new HoverModel();
            m.Enter(_a, 0);
            Assert.Equal(HoverChange.None, m.Cancel(_a));
            Assert.Equal(HoverChange.None, m.Tick(5));
            Assert.False(m.IsShown);
        }

        [Fact]
        public void AnotherTargetGoingAwayChangesNothing()
        {
            var m = new HoverModel();
            m.Enter(_a, 0);
            m.Tick(1);
            Assert.Equal(HoverChange.None, m.Cancel(_b));
            Assert.Same(_a, m.Shown);
        }

        [Fact]
        public void LeavingAnotherTargetChangesNothing()
        {
            var m = new HoverModel();
            m.Enter(_a, 0);
            m.Tick(1);
            Assert.Equal(HoverChange.None, m.Leave(_b, 2));
            Assert.Same(_a, m.Shown);
        }

        [Fact]
        public void ADeclinedCardIsTriedAgainAtTheNextTick()
        {
            var m = new HoverModel();
            m.Enter(_a, 0);
            Assert.Equal(HoverChange.Opened, m.Tick(1));
            m.Decline();
            Assert.False(m.IsShown);
            Assert.Equal(HoverChange.Opened, m.Tick(1.1));
        }

        [Fact]
        public void EachTargetKeepsTheDelaysItEnteredWith()
        {
            var m = new HoverModel { OpenDelay = 1, CloseDelay = 0 };
            m.Enter(_a, 0, 0.5, 0.3);
            Assert.Equal(HoverChange.Opened, m.Tick(0.5));
            Assert.Equal(HoverChange.None, m.Leave(_a, 1)); // its own close delay, not the model's 0
            Assert.Equal(HoverChange.Closed, m.Tick(1.3));
        }

        [Fact]
        public void EnteringTheSameTargetAgainChangesNothing()
        {
            var m = new HoverModel();
            m.Enter(_a, 0);
            m.Enter(_a, 0.9);
            Assert.Equal(HoverChange.Opened, m.Tick(1));
        }

        [Fact]
        public void ResetClosesTheCardAndDropsTheWait()
        {
            var m = new HoverModel();
            m.Enter(_a, 0);
            m.Tick(1);
            Assert.Equal(HoverChange.Closed, m.Reset());
            Assert.Equal(HoverChange.None, m.Tick(5));
            Assert.Null(m.Hovered);
        }
    }
}
