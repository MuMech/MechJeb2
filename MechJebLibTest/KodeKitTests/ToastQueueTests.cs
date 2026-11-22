/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's toast queue, without KSP or Unity: time is a number the test passes in.</summary>
    public class ToastQueueTests
    {
        [Fact]
        public void ANewToastIsVisibleWithItsTextAndSeverity()
        {
            var q = new ToastQueue();
            Toast t = q.Show("Saved", ToastSeverity.Success, 0);
            Assert.Single(q.Visible);
            Assert.Same(t, q.Visible[0]);
            Assert.Equal("Saved", t.Text);
            Assert.Equal(ToastSeverity.Success, t.Severity);
            Assert.Equal(1, t.Count);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void EmptyTextPostsNothing(string text)
        {
            var q = new ToastQueue();
            int v = q.Version;
            Assert.Null(q.Show(text, ToastSeverity.Info, 0));
            Assert.Empty(q.Visible);
            Assert.Equal(v, q.Version);
        }

        [Fact]
        public void NullTextPostsNothing()
        {
            Assert.Null(new ToastQueue().Show(null, ToastSeverity.Info, 0));
        }

        [Fact]
        public void ToastsStayInTheOrderTheyWerePosted()
        {
            var q = new ToastQueue();
            q.Show("a", ToastSeverity.Info, 0);
            q.Show("b", ToastSeverity.Info, 0);
            q.Show("c", ToastSeverity.Info, 0);
            Assert.Equal(new[] { "a", "b", "c" }, Texts(q));
        }

        [Fact]
        public void AToastExpiresAfterItsSeveritysDuration()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 4);
            q.Show("x", ToastSeverity.Info, 10);
            q.Update(13.9);
            Assert.Single(q.Visible);
            q.Update(14.1);
            Assert.Empty(q.Visible);
        }

        [Fact]
        public void EachSeverityHasItsOwnDuration()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 2);
            q.SetDuration(ToastSeverity.Error, 10);
            q.Show("info", ToastSeverity.Info, 0);
            q.Show("error", ToastSeverity.Error, 0);
            q.Update(3);
            Assert.Equal(new[] { "error" }, Texts(q));
            Assert.Equal(10, q.GetDuration(ToastSeverity.Error));
        }

        [Fact]
        public void ADurationOfZeroKeepsTheToastUntilItIsDismissed()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Error, 0);
            Toast t = q.Show("stuck", ToastSeverity.Error, 0);
            Assert.True(t.Sticky);
            q.Update(10000);
            Assert.Single(q.Visible);
            Assert.True(q.Dismiss(t.Id, ChangeSource.Pointer, 10000));
            Assert.Empty(q.Visible);
        }

        [Fact]
        public void ARepeatedTextIsCountedNotStacked()
        {
            var q = new ToastQueue();
            Toast a = q.Show("Copied", ToastSeverity.Info, 0);
            Toast b = q.Show("Copied", ToastSeverity.Info, 1);
            Toast c = q.Show("Copied", ToastSeverity.Info, 2);
            Assert.Same(a, b);
            Assert.Same(a, c);
            Assert.Single(q.Visible);
            Assert.Equal(3, a.Count);
        }

        [Fact]
        public void ARepeatGivesTheVisibleToastItsFullTimeAgain()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 4);
            q.Show("x", ToastSeverity.Info, 0);
            q.Show("x", ToastSeverity.Info, 3);
            q.Update(6);
            Assert.Single(q.Visible);
            q.Update(7.1);
            Assert.Empty(q.Visible);
        }

        [Fact]
        public void TheSameTextWithAnotherSeverityIsAnotherToast()
        {
            var q = new ToastQueue();
            q.Show("x", ToastSeverity.Info, 0);
            q.Show("x", ToastSeverity.Error, 0);
            Assert.Equal(2, q.Visible.Count);
        }

        [Fact]
        public void ARepeatOfAWaitingToastIsCountedToo()
        {
            var q = new ToastQueue(1);
            q.Show("first", ToastSeverity.Info, 0);
            Toast w = q.Show("waiting", ToastSeverity.Info, 0);
            Toast again = q.Show("waiting", ToastSeverity.Info, 0);
            Assert.Same(w, again);
            Assert.Equal(2, w.Count);
            Assert.Equal(1, q.PendingCount);
        }

        [Fact]
        public void OnlyMaxVisibleToastsShowTheRestWait()
        {
            var q = new ToastQueue(2);
            q.Show("a", ToastSeverity.Info, 0);
            q.Show("b", ToastSeverity.Info, 0);
            q.Show("c", ToastSeverity.Info, 0);
            q.Show("d", ToastSeverity.Info, 0);
            Assert.Equal(new[] { "a", "b" }, Texts(q));
            Assert.Equal(2, q.PendingCount);
        }

        [Fact]
        public void AWaitingToastTakesTheFreePlaceInOrder()
        {
            var q = new ToastQueue(2);
            q.SetDuration(ToastSeverity.Info, 4);
            q.Show("a", ToastSeverity.Info, 0);
            q.Show("b", ToastSeverity.Info, 0);
            q.Show("c", ToastSeverity.Info, 0);
            q.Show("d", ToastSeverity.Info, 0);
            q.Update(4.5);
            Assert.Equal(new[] { "c", "d" }, Texts(q));
            Assert.Equal(0, q.PendingCount);
        }

        [Fact]
        public void AToastsTimeStartsWhenItBecomesVisible()
        {
            var q = new ToastQueue(1);
            q.SetDuration(ToastSeverity.Info, 4);
            q.Show("a", ToastSeverity.Info, 0);
            Toast b = q.Show("b", ToastSeverity.Info, 0);
            q.Update(5);
            Assert.Equal(new[] { "b" }, Texts(q));
            Assert.Equal(4, b.Remaining);
            q.Update(8.5);
            Assert.Single(q.Visible);
            q.Update(9.1);
            Assert.Empty(q.Visible);
        }

        [Fact]
        public void BeyondMaxPendingTheOldestWaitingToastIsDropped()
        {
            var q = new ToastQueue(1, 2);
            var closed = new List<(string, ToastEnd)>();
            q.Closed += (t, why) => closed.Add((t.Text, why));
            q.Show("shown", ToastSeverity.Info, 0);
            q.Show("w1", ToastSeverity.Info, 0);
            q.Show("w2", ToastSeverity.Info, 0);
            q.Show("w3", ToastSeverity.Info, 0);
            Assert.Equal(2, q.PendingCount);
            Assert.Equal(1, q.Dropped);
            Assert.Equal(new[] { ("w1", ToastEnd.Dropped) }, closed);
        }

        [Fact]
        public void HoveringStopsTheTimers()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 4);
            q.Show("x", ToastSeverity.Info, 0);
            q.SetHovered(true, 1);
            q.Update(100);
            Assert.Single(q.Visible);
            Assert.Equal(3, q.Visible[0].Remaining);
        }

        [Fact]
        public void LeavingResumesWithTheTimeThatWasLeft()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 4);
            q.Show("x", ToastSeverity.Info, 0);
            q.SetHovered(true, 1);
            q.SetHovered(false, 100);
            q.Update(102.5);
            Assert.Single(q.Visible);
            q.Update(103.1);
            Assert.Empty(q.Visible);
        }

        [Fact]
        public void TimeBeforeTheHoverStillCounts()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 4);
            q.Show("x", ToastSeverity.Info, 0);
            q.SetHovered(true, 3.5);
            Assert.Equal(0.5, q.Visible[0].Remaining, 3);
        }

        [Fact]
        public void DismissRemovesAVisibleToastAtOnce()
        {
            var q = new ToastQueue();
            Toast a = q.Show("a", ToastSeverity.Info, 0);
            q.Show("b", ToastSeverity.Info, 0);
            Assert.True(q.Dismiss(a.Id, ChangeSource.Pointer, 0));
            Assert.Equal(new[] { "b" }, Texts(q));
            Assert.False(q.Dismiss(a.Id, ChangeSource.Pointer, 0));
        }

        [Fact]
        public void DismissingFreesAPlaceForAWaitingToast()
        {
            var q = new ToastQueue(1);
            Toast a = q.Show("a", ToastSeverity.Info, 0);
            q.Show("b", ToastSeverity.Info, 0);
            q.Dismiss(a.Id, ChangeSource.Agent, 0);
            Assert.Equal(new[] { "b" }, Texts(q));
        }

        [Fact]
        public void DismissRemovesAWaitingToast()
        {
            var q = new ToastQueue(1);
            q.Show("a", ToastSeverity.Info, 0);
            Toast b = q.Show("b", ToastSeverity.Info, 0);
            Assert.True(q.Dismiss(b.Id, ChangeSource.Pointer, 0));
            Assert.Equal(0, q.PendingCount);
            Assert.Equal(new[] { "a" }, Texts(q));
        }

        [Fact]
        public void DismissAllEmptiesTheVisibleAndTheWaiting()
        {
            var q = new ToastQueue(2);
            for (int i = 0; i < 5; i++) q.Show("t" + i, ToastSeverity.Info, 0);
            q.DismissAll(0);
            Assert.Empty(q.Visible);
            Assert.Equal(0, q.PendingCount);
        }

        [Fact]
        public void ADismissedTextCanBePostedAgainAsANewToast()
        {
            var q = new ToastQueue();
            Toast a = q.Show("x", ToastSeverity.Info, 0);
            q.Dismiss(a.Id, ChangeSource.Pointer, 0);
            Toast b = q.Show("x", ToastSeverity.Info, 1);
            Assert.NotSame(a, b);
            Assert.Equal(1, b.Count);
        }

        [Fact]
        public void VersionChangesWhenWhatIsDrawnChanges()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 4);
            int v = q.Version;
            q.Show("x", ToastSeverity.Info, 0);
            Assert.NotEqual(v, q.Version);
            v = q.Version;
            q.Show("x", ToastSeverity.Info, 1); // the count changes
            Assert.NotEqual(v, q.Version);
            v = q.Version;
            Assert.False(q.Update(2)); // time passes, nothing drawn changes
            Assert.Equal(v, q.Version);
            Assert.True(q.Update(10));
            Assert.NotEqual(v, q.Version);
        }

        [Fact]
        public void PostedReportsEachShowAndWhetherItWasARepeat()
        {
            var q = new ToastQueue();
            var seen = new List<(string, bool, int)>();
            q.Posted += (t, repeat) => seen.Add((t.Text, repeat, t.Count));
            q.Show("x", ToastSeverity.Info, 0);
            q.Show("x", ToastSeverity.Info, 0);
            Assert.Equal(new[] { ("x", false, 1), ("x", true, 2) }, seen);
        }

        [Fact]
        public void ClosedReportsWhyAToastLeft()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 1);
            var ends = new List<ToastEnd>();
            q.Closed += (t, why) => ends.Add(why);
            Toast a = q.Show("a", ToastSeverity.Info, 0);
            q.Show("b", ToastSeverity.Info, 0);
            q.Dismiss(a.Id, ChangeSource.Pointer, 0);
            q.Update(2);
            AssertionExtensions.EqualSequence(new[] { ToastEnd.Dismissed, ToastEnd.Expired }, ends);
        }

        [Fact]
        public void ATimeThatGoesBackwardsChargesNothing()
        {
            var q = new ToastQueue();
            q.SetDuration(ToastSeverity.Info, 4);
            Toast t = q.Show("x", ToastSeverity.Info, 10);
            q.Update(5);
            Assert.Equal(4, t.Remaining);
        }

        static string[] Texts(ToastQueue q)
        {
            var r = new string[q.Visible.Count];
            for (int i = 0; i < r.Length; i++) r[i] = q.Visible[i].Text;
            return r;
        }

        [Fact]
        public void TheQueueIsEmptyOnlyWithNoToastShownOrWaiting()
        {
            var q = new ToastQueue(1);
            Assert.True(q.Empty);
            q.Show("a", ToastSeverity.Info, 0);
            q.Show("b", ToastSeverity.Info, 0);
            Assert.False(q.Empty);
            q.Update(100);
            Assert.False(q.Empty); // "b" came in when "a" expired
            q.Update(200);
            Assert.True(q.Empty);
            q.Show("c", ToastSeverity.Info, 200);
            q.DismissAll(201);
            Assert.True(q.Empty);
        }
    }
}
