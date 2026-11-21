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
    /// <summary>KodeKit's drag from one list into another, without KSP or Unity.</summary>
    public class TransferModelTests
    {
        private readonly List<string> source = new List<string> { "a", "b", "c" };
        private readonly List<string> target = new List<string> { "x", "y" };

        private TransferModel Model(TransferMode mode = TransferMode.Copy) =>
            new TransferModel(() => source.Count, () => target.Count, (i, at) => target.Insert(at, source[i]), mode,
                mode == TransferMode.Move ? i => source.RemoveAt(i) : (Action<int>)null);

        [Fact]
        public void ADropOverTheTargetInsertsTheRowAtTheGap()
        {
            TransferModel m = Model();
            Assert.True(m.Begin(1));
            m.Over(1);
            Assert.True(m.ShowsIndicator);
            ListTransfer t = m.Drop();
            Assert.True(t.Done);
            Assert.Equal(1, t.SourceIndex);
            Assert.Equal(1, t.At);
            Assert.Equal(new[] { "x", "b", "y" }, target);
        }

        [Fact]
        public void ACopyLeavesTheSourceAsItIs()
        {
            TransferModel m = Model();
            m.Begin(0);
            m.Over(2);
            m.Drop();
            Assert.Equal(new[] { "a", "b", "c" }, source);
            Assert.Equal(new[] { "x", "y", "a" }, target);
        }

        [Fact]
        public void AMoveTakesTheRowOutOfTheSource()
        {
            TransferModel m = Model(TransferMode.Move);
            m.Begin(2);
            m.Over(0);
            ListTransfer t = m.Drop();
            Assert.Equal(TransferMode.Move, t.Mode);
            Assert.Equal(new[] { "a", "b" }, source);
            Assert.Equal(new[] { "c", "x", "y" }, target);
        }

        [Fact]
        public void AMoveNeedsAWayToRemoveTheRow() =>
            Assert.Throws<ArgumentNullException>(() => new TransferModel(() => 1, () => 1, (i, at) => { }, TransferMode.Move));

        [Fact]
        public void NothingChangesDuringTheDrag()
        {
            TransferModel m = Model(TransferMode.Move);
            m.Begin(0);
            m.Over(0);
            m.Over(2);
            Assert.Equal(3, source.Count);
            Assert.Equal(2, target.Count);
        }

        [Fact]
        public void AReleaseOutsideTheTargetTransfersNothing()
        {
            TransferModel m = Model();
            m.Begin(0);
            m.Over(1);
            m.Outside();
            Assert.False(m.ShowsIndicator);
            Assert.False(m.Drop().Done);
            Assert.Equal(2, target.Count);
            Assert.False(m.Dragging);
        }

        [Fact]
        public void CancelEndsTheDragWithoutATransfer()
        {
            TransferModel m = Model();
            m.Begin(0);
            m.Over(1);
            m.Cancel();
            Assert.False(m.Drop().Done);
            Assert.Equal(2, target.Count);
        }

        [Fact]
        public void ARowTheRuleRefusesCantBeDragged()
        {
            TransferModel m = Model();
            m.Allow = i => source[i] != "b";
            Assert.False(m.Begin(1));
            Assert.False(m.Dragging);
            Assert.True(m.Begin(0));
        }

        [Fact]
        public void ARowTheRuleRefusesCantBeTransferredWithoutADrag()
        {
            TransferModel m = Model();
            m.Allow = i => source[i] != "b";
            Assert.False(m.Transfer(1, 0, ChangeSource.Agent).Done);
            Assert.Equal(2, target.Count);
        }

        [Fact]
        public void ADragCanOnlyStartOnARowOfTheSource()
        {
            TransferModel m = Model();
            Assert.False(m.Begin(-1));
            Assert.False(m.Begin(3));
        }

        [Fact]
        public void TheEndOfTheTargetIsAGapAndPastItIsClamped()
        {
            TransferModel m = Model();
            m.Begin(0);
            m.Over(99);
            Assert.Equal(2, m.Gap);
            m.Drop();
            Assert.Equal("a", target[2]);
        }

        [Fact]
        public void AnEmptyTargetTakesTheRowAtZero()
        {
            target.Clear();
            TransferModel m = Model();
            m.Begin(2);
            m.OverAt(new VirtualListModel(0, 20, 100), 50);
            Assert.Equal(0, m.Gap);
            m.Drop();
            Assert.Equal(new[] { "c" }, target);
        }

        [Fact]
        public void TransferWithoutADragClampsWhereItInserts()
        {
            TransferModel m = Model();
            ListTransfer t = m.Transfer(0, 50, ChangeSource.Agent);
            Assert.True(t.Done);
            Assert.Equal(2, t.At);
            Assert.Equal(ChangeSource.Agent, m.Source);
            t = m.Transfer(1, -3, ChangeSource.Pointer);
            Assert.Equal(0, t.At);
            Assert.Equal(new[] { "b", "x", "y", "a" }, target);
        }

        [Fact]
        public void TheDropKeepsTheDragsSource()
        {
            TransferModel m = Model();
            m.Begin(0, ChangeSource.Agent);
            m.Over(0);
            m.Drop();
            Assert.Equal(ChangeSource.Agent, m.Source);
        }
    }
}
