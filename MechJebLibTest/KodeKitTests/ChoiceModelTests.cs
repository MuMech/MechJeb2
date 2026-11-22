/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's Segmented behaviour, without KSP or Unity.</summary>
    public class ChoiceModelTests
    {
        private sealed class Model
        {
            public          int       Value;
            public readonly List<int> Sets = new List<int>();

            public void Set(int v)
            {
                Value = v;
                Sets.Add(v);
            }
        }

        private static (Model m, ChoiceModel c) Make(int count, int value = 0)
        {
            var m = new Model { Value = value };
            return (m, new ChoiceModel(count, () => m.Value, m.Set));
        }

        [Fact]
        public void ShowsTheModelsValue()
        {
            (Model m, ChoiceModel c) = Make(4, 2);
            Assert.Equal(2, c.Shown);
            Assert.True(c.IsOn(2));
            Assert.False(c.IsOn(1));
        }

        [Fact]
        public void APickSetsTheModelAndShowsIt()
        {
            (Model m, ChoiceModel c) = Make(4);
            Assert.True(c.Select(3, ChangeSource.Pointer));
            Assert.Equal(3, m.Value);
            Assert.Equal(3, c.Shown);
            Assert.Equal(ChangeSource.Pointer, c.LastSource);
        }

        [Fact]
        public void PickingTheSelectedEntryStillCallsTheSetter()
        {
            // the Translatron's OFF releases the throttle when picked again
            (Model m, ChoiceModel c) = Make(4, 1);
            Assert.True(c.Select(1, ChangeSource.Pointer));
            Assert.Equal(new[] { 1 }, m.Sets);
        }

        [Fact]
        public void SyncReportsOnlyAChange()
        {
            (Model m, ChoiceModel c) = Make(3);
            Assert.False(c.Sync());
            m.Value = 2;
            Assert.True(c.Sync());
            Assert.Equal(2, c.Shown);
            Assert.False(c.Sync());
        }

        [Fact]
        public void APickThatChangesTheShownEntryTellsTheHostToRepaint()
        {
            // a pick syncs the model itself, so the host's next Sync finds nothing new: the event is its only repaint
            (_, ChoiceModel c) = Make(3);
            int repaints = 0;
            c.ShownChanged += () => repaints++;
            c.Select(2, ChangeSource.Pointer);
            Assert.Equal(1, repaints);
            Assert.False(c.Sync());
            Assert.Equal(1, repaints);
        }

        [Fact]
        public void APickOfTheShownEntryOrARefusedPickAsksNoRepaint()
        {
            (_, ChoiceModel c) = Make(3, 1);
            int repaints = 0;
            c.ShownChanged += () => repaints++;
            c.Select(1, ChangeSource.Pointer);
            c.Allow = (i, s) => false;
            c.Select(2, ChangeSource.Pointer);
            Assert.Equal(0, repaints);
        }

        [Fact]
        public void AModelChangeFromOutsideAsksARepaintOnSync()
        {
            (Model m, ChoiceModel c) = Make(3);
            int repaints = 0;
            c.ShownChanged += () => repaints++;
            m.Value = 2;
            Assert.True(c.Sync());
            Assert.Equal(1, repaints);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(99)]
        public void AValueOutsideTheEntriesShowsNone(int value)
        {
            (Model m, ChoiceModel c) = Make(3, value);
            Assert.Equal(-1, c.Shown);
            for (int i = 0; i < 3; i++) Assert.False(c.IsOn(i));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(4)]
        public void APickOutsideTheEntriesDoesNothing(int i)
        {
            (Model m, ChoiceModel c) = Make(4, 1);
            Assert.False(c.Select(i, ChangeSource.Agent));
            Assert.Empty(m.Sets);
        }

        [Fact]
        public void ARefusedPickChangesNothing()
        {
            (Model m, ChoiceModel c) = Make(2);
            c.Allow = (i, src) => false; // the ascent type while the autopilot runs
            Assert.False(c.Select(1, ChangeSource.Pointer));
            Assert.Empty(m.Sets);
            Assert.Equal(0, c.Shown);
        }

        [Fact]
        public void AllowSeesTheEntryAndTheSource()
        {
            (Model m, ChoiceModel c) = Make(3);
            c.Allow = (i, src) => src != ChangeSource.Agent || i != 2;
            Assert.False(c.Select(2, ChangeSource.Agent));
            Assert.True(c.Select(2, ChangeSource.Pointer));
            Assert.Equal(2, m.Value);
        }

        [Fact]
        public void ArrowsSelectAndWrap()
        {
            (Model m, ChoiceModel c) = Make(3);
            Assert.True(c.Key(ChoiceKey.Left));
            Assert.Equal(2, m.Value);
            Assert.True(c.Key(ChoiceKey.Right));
            Assert.Equal(0, m.Value);
            Assert.True(c.Key(ChoiceKey.Right));
            Assert.Equal(1, m.Value);
            Assert.Equal(ChangeSource.Keyboard, c.LastSource);
        }

        [Fact]
        public void HomeAndEndJump()
        {
            (Model m, ChoiceModel c) = Make(5, 2);
            c.Key(ChoiceKey.End);
            Assert.Equal(4, m.Value);
            c.Key(ChoiceKey.Home);
            Assert.Equal(0, m.Value);
        }

        [Fact]
        public void EnterPicksTheFocusedEntry()
        {
            (Model m, ChoiceModel c) = Make(3, 1);
            Assert.True(c.Key(ChoiceKey.Enter));
            Assert.Equal(new[] { 1 }, m.Sets);
        }

        [Fact]
        public void ARefusedKeyMoveKeepsTheModel()
        {
            (Model m, ChoiceModel c) = Make(3);
            c.Allow = (i, src) => i != 1;
            c.Key(ChoiceKey.Right); // to 1: refused
            Assert.Equal(0, m.Value);
            Assert.Equal(0, c.Shown);
            c.Key(ChoiceKey.Right); // on from the focus, to 2
            Assert.Equal(2, m.Value);
        }

        [Fact]
        public void AnEmptyChoiceHandlesNothing()
        {
            (Model m, ChoiceModel c) = Make(0);
            Assert.Equal(-1, c.Shown);
            Assert.False(c.Key(ChoiceKey.Right));
            Assert.False(c.Select(0, ChangeSource.Pointer));
        }
    

        [Fact]
        public void UpAndDownMoveLikeLeftAndRightAndWrap()
        {
            (Model m, ChoiceModel c) = Make(3, 0);
            c.Key(ChoiceKey.Up);
            Assert.Equal(2, m.Value);
            c.Key(ChoiceKey.Down);
            Assert.Equal(0, m.Value);
        }

        [Fact]
        public void TabsDescribeTabItemsInAGroup()
        {
            (Model m, ChoiceModel c) = Make(2, 1);
            c.Label = "Pages";
            c.ItemLabels = new[] { "a", "b" };
            c.ItemRole = PartRole.Tab;
            var parts = new List<PartDescription>();
            c.Describe(parts);
            Assert.Equal(PartRole.Group, parts[0].Role);
            Assert.Equal(PartRole.Tab, parts[1].Role);
            Assert.True(parts[2].Has(PartFlags.On));
            Assert.Empty(Patterns.Check(c));
        }

        [Fact]
        public void ATabListWithNoTabOnFailsTheChecks()
        {
            (Model m, ChoiceModel c) = Make(2, -1);
            c.Label = "Pages";
            c.ItemLabels = new[] { "a", "b" };
            c.ItemRole = PartRole.Tab;
            Assert.Contains(Patterns.Check(c), f => f.Contains("0 items on, not 1"));
        }

        [Fact]
        public void AListOfOptionsIsAListBoxAndMayHaveNothingChosen()
        {
            (Model m, ChoiceModel c) = Make(3, -1);
            c.Label = "Operations";
            c.ItemLabels = new[] { "a", "b", "c" };
            c.ItemRole = PartRole.Option;
            var parts = new List<PartDescription>();
            c.Describe(parts);
            Assert.Equal(PartRole.ListBox, parts[0].Role);
            Assert.Equal(PartRole.Option, parts[1].Role);
            Assert.Equal(-1, c.Shown);
            Assert.Empty(Patterns.Check(c));
        }

        [Fact]
        public void PickingAnOptionMarksItAndRaisesShownChangedOnce()
        {
            (Model m, ChoiceModel c) = Make(3, -1);
            c.ItemRole = PartRole.Option;
            int shown = 0;
            c.ShownChanged += () => shown++;
            Assert.True(c.Select(2, ChangeSource.Pointer));
            Assert.Equal(2, m.Value);
            Assert.True(c.IsOn(2));
            Assert.Equal(1, shown);
        }
    }
}
