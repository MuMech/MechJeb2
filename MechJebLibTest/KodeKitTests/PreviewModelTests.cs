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
    /// <summary>When a hovered entry previews its effect (PreviewModel), without KSP or Unity.</summary>
    public class PreviewModelTests
    {
        private sealed class Host
        {
            public int Value = 2;
            public readonly List<int> Shown = new List<int>();
            public int Reverts;
            public readonly PreviewModel<int> Model;

            public Host(System.Func<int, bool> previewable = null)
            {
                Model = new PreviewModel<int>(() => Value, previewable) { Debounce = 0.15, Enabled = true };
                Model.Previewed += i => Shown.Add(i);
                Model.Reverted += () => Reverts++;
            }
        }

        [Fact]
        public void APreviewStartsOnceTheEntryStaysHoveredForTheDebounce()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 10);
            Assert.True(h.Model.Waiting);
            Assert.False(h.Model.Tick(10.14));
            Assert.Empty(h.Shown);
            Assert.True(h.Model.Tick(10.15));
            Assert.Equal(new[] { 5 }, h.Shown);
            Assert.True(h.Model.IsPreviewing);
            Assert.Equal(5, h.Model.Previewing);
            Assert.False(h.Model.Tick(11));
            Assert.Single(h.Shown);
        }

        [Fact]
        public void SweepingTheListComputesNothing()
        {
            var h = new Host();
            for (int i = 0; i < 10; i++)
            {
                h.Model.Hover(i, ChangeSource.Pointer, i * 0.05);
                h.Model.Tick(i * 0.05 + 0.01);
            }
            Assert.Empty(h.Shown);
            Assert.Equal(0, h.Model.Previews);
        }

        [Fact]
        public void APreviewNeverChangesTheCommittedValue()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Tick(1);
            h.Model.Hover(7, ChangeSource.Keyboard, 1);
            h.Model.Tick(2);
            h.Model.Revert();
            Assert.Equal(2, h.Value);
            Assert.Equal(2, h.Model.Committed);
        }

        [Fact]
        public void HoveringAnotherEntryEndsTheShownPreviewAtOnceAndWaitsAgain()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Tick(1);
            h.Model.Hover(6, ChangeSource.Pointer, 1);
            Assert.Equal(1, h.Reverts);
            Assert.False(h.Model.IsPreviewing);
            Assert.False(h.Model.Tick(1.1));
            Assert.True(h.Model.Tick(1.15));
            Assert.Equal(new[] { 5, 6 }, h.Shown);
        }

        [Fact]
        public void HoveringTheSameEntryAgainKeepsItsWait()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Hover(5, ChangeSource.Keyboard, 0.1);
            Assert.True(h.Model.Tick(0.15));
            Assert.Equal(ChangeSource.Keyboard, h.Model.HoveredSource);
        }

        [Fact]
        public void AnEntryWithoutAPreviewShowsNothingAndEndsTheShownOne()
        {
            var h = new Host(i => i != 9);
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Tick(1);
            h.Model.Hover(9, ChangeSource.Pointer, 1);
            Assert.Equal(1, h.Reverts);
            Assert.False(h.Model.Waiting);
            Assert.False(h.Model.Tick(5));
            Assert.Equal(new[] { 5 }, h.Shown);
            Assert.True(h.Model.IsHovering);
        }

        [Fact]
        public void LeavingTheHoveredEntryRevertsAndDropsTheWait()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Tick(1);
            h.Model.Leave(5);
            Assert.Equal(1, h.Reverts);
            Assert.False(h.Model.IsHovering);

            h.Model.Hover(6, ChangeSource.Pointer, 2);
            h.Model.Leave(6);
            Assert.False(h.Model.Tick(3));
            Assert.Equal(new[] { 5 }, h.Shown);
        }

        [Fact]
        public void APointerLeaveOfAnEntryTheKeyboardMovedAwayFromChangesNothing()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Hover(6, ChangeSource.Keyboard, 0.05);
            h.Model.Leave(5);
            Assert.True(h.Model.IsHovering);
            Assert.True(h.Model.Tick(0.2));
            Assert.Equal(new[] { 6 }, h.Shown);
        }

        [Fact]
        public void RevertOnEscapeOrCloseEndsThePreviewAndTheWait()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Tick(1);
            h.Model.Revert();
            Assert.Equal(1, h.Reverts);
            Assert.False(h.Model.IsPreviewing);
            Assert.False(h.Model.IsHovering);

            h.Model.Hover(6, ChangeSource.Pointer, 2);
            h.Model.Revert();
            Assert.False(h.Model.Tick(5));
            Assert.Equal(1, h.Reverts); // nothing was shown the second time
        }

        [Fact]
        public void ACommitEndsThePreviewAndRecordsItsSourceWithoutSettingAnything()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Tick(1);
            h.Model.Commit(ChangeSource.Keyboard);
            Assert.Equal(1, h.Reverts);
            Assert.Equal(ChangeSource.Keyboard, h.Model.CommittedSource);
            Assert.Equal(2, h.Value);
            Assert.False(h.Model.IsHovering);
        }

        [Fact]
        public void OffByDefaultNothingPreviews()
        {
            var m = new PreviewModel<int>(() => 0);
            int shown = 0;
            m.Previewed += _ => shown++;
            Assert.False(m.Enabled);
            m.Hover(1, ChangeSource.Pointer, 0);
            Assert.False(m.Waiting);
            Assert.False(m.Tick(10));
            Assert.Equal(0, shown);
        }

        [Fact]
        public void TurningPreviewsOffEndsTheShownOne()
        {
            var h = new Host();
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Tick(1);
            h.Model.Enabled = false;
            Assert.Equal(1, h.Reverts);
            Assert.False(h.Model.IsPreviewing);
            h.Model.Hover(6, ChangeSource.Pointer, 2);
            Assert.False(h.Model.Tick(5));
        }

        [Fact]
        public void InvalidatingShowsTheShownPreviewAgainForTheHostToRecompute()
        {
            var h = new Host();
            Assert.False(h.Model.Invalidate());
            h.Model.Hover(5, ChangeSource.Pointer, 0);
            h.Model.Tick(1);
            Assert.True(h.Model.Invalidate());
            Assert.Equal(new[] { 5, 5 }, h.Shown);
            Assert.Equal(2, h.Model.Previews);
            Assert.Equal(0, h.Reverts);
        }

        [Fact]
        public void OnlyOnePreviewIsShownAtATime()
        {
            var h = new Host();
            int shownNow = 0, most = 0;
            h.Model.Previewed += _ => most = System.Math.Max(most, ++shownNow);
            h.Model.Reverted += () => shownNow--;
            double t = 0;
            for (int i = 0; i < 6; i++)
            {
                h.Model.Hover(i, i % 2 == 0 ? ChangeSource.Pointer : ChangeSource.Keyboard, t);
                t += 0.2;
                h.Model.Tick(t);
            }
            Assert.Equal(1, most);
            Assert.Equal(6, h.Shown.Count);
        }
    }
}
