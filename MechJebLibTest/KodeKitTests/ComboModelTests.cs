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
    /// <summary>KodeKit's typeable drop-down (ComboBox), without KSP or Unity.</summary>
    public class ComboModelTests
    {
        private static readonly string[] Bodies = { "Sun", "Moho", "Eve", "Gilly", "Kerbin", "Mun", "Minmus", "Duna", "Ike", "Dres", "Jool", "Laythe" };

        private sealed class Host
        {
            public int Value;
            public readonly List<(int, ChangeSource)> Sets = new List<(int, ChangeSource)>();
            public readonly ComboModel Model;

            public Host(string[] items, int value = 0, bool filterable = false, string[] keys = null)
            {
                Value = value;
                Model = new ComboModel(() => Value, (i, s) =>
                {
                    Value = i;
                    Sets.Add((i, s));
                }) { Filterable = filterable };
                Model.SetItems(items, keys);
            }
        }

        // ---- opening ----

        [Fact]
        public void OpeningHighlightsTheSelectedEntry()
        {
            var h = new Host(Bodies, 4);
            h.Model.Open();
            Assert.True(h.Model.IsOpen);
            Assert.Equal(4, h.Model.HighlightedItem);
            Assert.Equal(Bodies.Length, h.Model.RowCount);
        }

        [Fact]
        public void ASelectedValueOutsideTheEntriesHighlightsTheNearestEnd()
        {
            var h = new Host(Bodies, -3);
            h.Model.Open();
            Assert.Equal(0, h.Model.HighlightedItem);
            h.Model.Close();
            h.Value = 99;
            h.Model.Open();
            Assert.Equal(Bodies.Length - 1, h.Model.HighlightedItem);
        }

        [Fact]
        public void AnEmptyListOpensWithNothingHighlighted()
        {
            var h = new Host(new string[0]);
            h.Model.Open();
            Assert.Equal(-1, h.Model.Highlight);
            Assert.False(h.Model.Handle(ScopeKeys.Enter));
            Assert.False(h.Model.NoMatch);
        }

        [Fact]
        public void AClosedListHasNoRows()
        {
            var h = new Host(Bodies);
            Assert.Equal(0, h.Model.RowCount);
            Assert.Equal(-1, h.Model.RowOf(3));
        }

        // ---- keys ----

        [Fact]
        public void DownAndUpMoveOneRowAndClampWithoutWrapping()
        {
            var h = new Host(Bodies, 10);
            h.Model.Open();
            h.Model.Handle(ScopeKeys.Down);
            Assert.Equal(11, h.Model.HighlightedItem);
            Assert.False(h.Model.Handle(ScopeKeys.Down));
            Assert.Equal(11, h.Model.HighlightedItem);
            h.Model.Close();
            h.Value = 0;
            h.Model.Open();
            Assert.False(h.Model.Handle(ScopeKeys.Up));
            Assert.Equal(0, h.Model.HighlightedItem);
        }

        [Fact]
        public void PageKeysMoveEightRows()
        {
            var h = new Host(Bodies, 0);
            h.Model.Open();
            h.Model.Handle(ScopeKeys.PageDown);
            Assert.Equal(8, h.Model.HighlightedItem);
            h.Model.Handle(ScopeKeys.PageDown);
            Assert.Equal(11, h.Model.HighlightedItem);
            h.Model.Handle(ScopeKeys.PageUp);
            Assert.Equal(3, h.Model.HighlightedItem);
        }

        [Fact]
        public void HomeAndEndJumpToTheEnds()
        {
            var h = new Host(Bodies, 5);
            h.Model.Open();
            h.Model.Handle(ScopeKeys.End);
            Assert.Equal(11, h.Model.HighlightedItem);
            h.Model.Handle(ScopeKeys.Home);
            Assert.Equal(0, h.Model.HighlightedItem);
        }

        [Fact]
        public void HomeAndEndAreTheFilterFieldsWhenFilterable()
        {
            var h = new Host(Bodies, 5, true);
            h.Model.Open();
            Assert.False(h.Model.Handle(ScopeKeys.End));
            Assert.False(h.Model.Handle(ScopeKeys.Home));
            Assert.Equal(5, h.Model.HighlightedItem);
        }

        [Fact]
        public void EnterPicksTheHighlightedEntryWithTheKeyboardSource()
        {
            var h = new Host(Bodies, 4);
            h.Model.Open();
            h.Model.Handle(ScopeKeys.Down);
            Assert.True(h.Model.Handle(ScopeKeys.Enter));
            Assert.False(h.Model.IsOpen);
            Assert.Equal(new[] { (5, ChangeSource.Keyboard) }, h.Sets);
            Assert.Equal(ChangeSource.Keyboard, h.Model.LastSource);
        }

        [Fact]
        public void EscapeClosesWithoutAPick()
        {
            var h = new Host(Bodies, 4);
            int closed = 0;
            h.Model.Closed += () => closed++;
            h.Model.Open();
            h.Model.Handle(ScopeKeys.Down);
            Assert.True(h.Model.Handle(ScopeKeys.Escape));
            Assert.False(h.Model.IsOpen);
            Assert.Empty(h.Sets);
            Assert.Equal(4, h.Value);
            Assert.Equal(1, closed);
        }

        [Fact]
        public void KeysOnAClosedListDoNothing()
        {
            var h = new Host(Bodies, 4);
            Assert.False(h.Model.Handle(ScopeKeys.Down));
            Assert.False(h.Model.Handle(ScopeKeys.Enter));
            Assert.Empty(h.Sets);
        }

        // ---- picks ----

        [Fact]
        public void PickingTheSelectedEntryClosesWithoutCallingTheSetter()
        {
            var h = new Host(Bodies, 4);
            h.Model.Open();
            Assert.True(h.Model.PickRow(h.Model.RowOf(4), ChangeSource.Pointer));
            Assert.False(h.Model.IsOpen);
            Assert.Empty(h.Sets);
        }

        [Fact]
        public void AClickPicksItsRowsEntryWithThePointerSource()
        {
            var h = new Host(Bodies, 4);
            h.Model.Open();
            Assert.True(h.Model.PickRow(7, ChangeSource.Pointer));
            Assert.Equal(new[] { (7, ChangeSource.Pointer) }, h.Sets);
        }

        [Fact]
        public void TheListIsClosedWhenTheSetterRuns()
        {
            ComboModel m = null;
            bool? openInSetter = null;
            m = new ComboModel(() => 0, (i, s) => openInSetter = m.IsOpen);
            m.SetItems(Bodies);
            m.Open();
            m.PickRow(3, ChangeSource.Pointer);
            Assert.False(openInSetter);
        }

        [Fact]
        public void ALetterWrapsToEntryZero()
        {
            var h = new Host(new[] { "Alpha", "Beta", "Gamma" }, 2);
            h.Model.Open();
            Assert.Equal(0, h.Model.Type('a', 0));
        }

        [Fact]
        public void NewItemsWhileOpenCloseWithoutAPick()
        {
            var h = new Host(Bodies, 4);
            h.Model.Open();
            h.Model.SetItems(new[] { "A", "B" });
            Assert.False(h.Model.IsOpen);
            Assert.False(h.Model.PickRow(1, ChangeSource.Pointer));
            Assert.Empty(h.Sets);
        }

        [Fact]
        public void TheSameItemsArrayChangesNothing()
        {
            var h = new Host(Bodies, 4);
            h.Model.Open();
            int version = h.Model.RowsVersion;
            h.Model.SetItems(Bodies);
            Assert.True(h.Model.IsOpen);
            Assert.Equal(version, h.Model.RowsVersion);
        }

        [Fact]
        public void KeysOfAnotherLengthAreIgnored()
        {
            var h = new Host(Bodies, 0, true, new[] { "x" });
            h.Model.Open();
            Assert.True(h.Model.SetFilter("x"));
            Assert.True(h.Model.NoMatch);
        }

        // ---- the filter ----

        [Fact]
        public void TheFilterRanksTheBestMatchFirstAndHighlightsIt()
        {
            var h = new Host(Bodies, 0, true);
            h.Model.Open();
            Assert.True(h.Model.SetFilter("mi"));
            Assert.Equal("Minmus", h.Model.ItemLabel(h.Model.ItemAt(0)));
            Assert.Equal(0, h.Model.Highlight);
            Assert.Equal(h.Model.RowCount, h.Model.EntryCount);
        }

        [Fact]
        public void EnterPicksTheBestMatch()
        {
            var h = new Host(Bodies, 0, true);
            h.Model.Open();
            h.Model.SetFilter("jo");
            h.Model.Handle(ScopeKeys.Enter);
            Assert.Equal(10, h.Value);
        }

        [Fact]
        public void TheFilterMatchesTheKeyToo()
        {
            string[] labels = { "太阳", "凯尔滨", "穆恩" };
            string[] keys = { "Sun", "Kerbin", "Mun" };
            var h = new Host(labels, 0, true, keys);
            h.Model.Open();
            h.Model.SetFilter("kerb");
            Assert.Equal(1, h.Model.EntryCount);
            Assert.Equal(1, h.Model.HighlightedItem);
        }

        [Fact]
        public void NothingMatchingIsTheNoMatchStateAndEnterPicksNothing()
        {
            var h = new Host(Bodies, 3, true);
            h.Model.Open();
            h.Model.SetFilter("zzz");
            Assert.True(h.Model.NoMatch);
            Assert.Equal(0, h.Model.RowCount);
            Assert.Equal(-1, h.Model.Highlight);
            Assert.False(h.Model.Handle(ScopeKeys.Enter));
            Assert.True(h.Model.IsOpen);
            Assert.False(h.Model.Handle(ScopeKeys.Down));
            Assert.Empty(h.Sets);
        }

        [Fact]
        public void ClearingTheFilterBringsEveryEntryBackWithTheSelectedHighlighted()
        {
            var h = new Host(Bodies, 6, true);
            h.Model.Open();
            h.Model.SetFilter("du");
            h.Model.SetFilter("");
            Assert.Equal(Bodies.Length, h.Model.RowCount);
            for (int r = 0; r < Bodies.Length; r++) Assert.Equal(r, h.Model.ItemAt(r));
            Assert.Equal(6, h.Model.HighlightedItem);
            Assert.False(h.Model.NoMatch);
        }

        [Fact]
        public void SpacesAloneAreAnEmptyFilter()
        {
            var h = new Host(Bodies, 6, true);
            h.Model.Open();
            h.Model.SetFilter("   ");
            Assert.Equal(Bodies.Length, h.Model.RowCount);
            Assert.False(h.Model.NoMatch);
        }

        [Fact]
        public void TheFilterStartsEmptyAtEachOpen()
        {
            var h = new Host(Bodies, 0, true);
            h.Model.Open();
            h.Model.SetFilter("ike");
            h.Model.Close();
            Assert.Equal("", h.Model.Filter);
            h.Model.Open();
            Assert.Equal(Bodies.Length, h.Model.RowCount);
        }

        [Fact]
        public void TheFilterAppliesOnlyWhileOpenAndFilterable()
        {
            var h = new Host(Bodies, 0, true);
            Assert.False(h.Model.SetFilter("ike"));
            var plain = new Host(Bodies, 0);
            plain.Model.Open();
            Assert.False(plain.Model.SetFilter("ike"));
            Assert.Equal(Bodies.Length, plain.Model.RowCount);
        }

        [Fact]
        public void TheSameFilterAgainChangesNothing()
        {
            var h = new Host(Bodies, 0, true);
            h.Model.Open();
            h.Model.SetFilter("du");
            int version = h.Model.RowsVersion;
            Assert.False(h.Model.SetFilter("du"));
            Assert.Equal(version, h.Model.RowsVersion);
        }

        [Fact]
        public void ARowsLookupFollowsTheFilter()
        {
            var h = new Host(Bodies, 0, true);
            h.Model.Open();
            h.Model.SetFilter("laythe");
            Assert.Equal(0, h.Model.RowOf(11));
            Assert.Equal(-1, h.Model.RowOf(0));
        }

        // ---- groups ----

        private static Host Grouped(int value = 0, bool filterable = false)
        {
            var h = new Host(Bodies, value, filterable);
            h.Model.Close();
            h.Model.Group = i => i == 0 ? "Star" : i < 4 ? "Inner" : "Outer";
            return h;
        }

        [Fact]
        public void GroupsShowAsHeaderRowsAboveTheirEntries()
        {
            Host h = Grouped();
            h.Model.Open();
            Assert.Equal(Bodies.Length + 3, h.Model.RowCount);
            Assert.True(h.Model.IsHeader(0));
            Assert.Equal("Star", h.Model.HeaderAt(0));
            Assert.Equal(0, h.Model.ItemAt(1));
            Assert.True(h.Model.IsHeader(2));
            Assert.Equal("Inner", h.Model.HeaderAt(2));
            Assert.Null(h.Model.HeaderAt(1));
            Assert.Equal(Bodies.Length, h.Model.EntryCount);
        }

        [Fact]
        public void TheHighlightSkipsHeaders()
        {
            Host h = Grouped(0);
            h.Model.Open();
            Assert.Equal(1, h.Model.Highlight);
            h.Model.Handle(ScopeKeys.Down);
            Assert.Equal(1, h.Model.HighlightedItem);
            Assert.Equal(3, h.Model.Highlight);
            h.Model.Handle(ScopeKeys.Up);
            Assert.Equal(0, h.Model.HighlightedItem);
            Assert.False(h.Model.Handle(ScopeKeys.Up));
            Assert.False(h.Model.HighlightRow(0));
        }

        [Fact]
        public void AHeaderCanNotBePicked()
        {
            Host h = Grouped(5);
            h.Model.Open();
            Assert.False(h.Model.PickRow(0, ChangeSource.Pointer));
            Assert.True(h.Model.IsOpen);
            Assert.Empty(h.Sets);
        }

        [Fact]
        public void AFilteredListHasNoHeaders()
        {
            Host h = Grouped(0, true);
            h.Model.Open();
            h.Model.SetFilter("m");
            for (int r = 0; r < h.Model.RowCount; r++) Assert.False(h.Model.IsHeader(r));
            h.Model.SetFilter("");
            Assert.True(h.Model.IsHeader(0));
        }

        // ---- typeahead ----

        [Fact]
        public void ALetterJumpsToTheNextEntryStartingWithIt()
        {
            var h = new Host(Bodies, 0);
            h.Model.Open();
            Assert.Equal(1, h.Model.Type('m', 0));
            Assert.Equal(5, h.Model.Type('m', 0.1));
            Assert.Equal(6, h.Model.Type('m', 0.2));
            Assert.Equal(1, h.Model.Type('m', 0.3));
        }

        [Fact]
        public void LettersTypedQuicklyMakeAPrefix()
        {
            var h = new Host(Bodies, 0);
            h.Model.Open();
            h.Model.Type('m', 0);
            Assert.Equal(6, h.Model.Type('i', 0.2));
            Assert.Equal("Minmus", h.Model.ItemLabel(h.Model.HighlightedItem));
        }

        [Fact]
        public void ALetterThatFindsNothingIsDroppedAndTheNextStartsAfresh()
        {
            var h = new Host(new[] { "OBT", "KILLROT", "NODE", "SURF", "TGT", "ADV", "AUTO", "OFF" }, 0);
            h.Model.Open();
            Assert.Equal(-1, h.Model.Type('1', 0));
            Assert.Equal(3, h.Model.Type('s', 0.3));
        }

        [Fact]
        public void TypeaheadIsOffWhenFilterable()
        {
            var h = new Host(Bodies, 0, true);
            h.Model.Open();
            Assert.Equal(-1, h.Model.Type('m', 0));
            Assert.Equal(0, h.Model.HighlightedItem);
        }

        [Fact]
        public void TypeaheadPassesOverHeaders()
        {
            Host h = Grouped(0);
            h.Model.Open();
            // "Inner" is a header: 'i' finds Ike, an entry
            Assert.Equal(8, h.Model.ItemAt(h.Model.Type('i', 0)));
        }

        // ---- events and allocation ----

        [Fact]
        public void HighlightChangedFiresOnEachMove()
        {
            var h = new Host(Bodies, 0);
            int n = 0;
            h.Model.HighlightChanged += () => n++;
            h.Model.Open();
            Assert.Equal(1, n);
            h.Model.Handle(ScopeKeys.Down);
            h.Model.Handle(ScopeKeys.Down);
            Assert.Equal(3, n);
            h.Model.Handle(ScopeKeys.Up);
            Assert.Equal(4, n);
        }

        [Fact]
        public void RowsChangedFiresOnOpenAndOnANewFilter()
        {
            var h = new Host(Bodies, 0, true);
            int n = 0;
            h.Model.RowsChanged += () => n++;
            h.Model.Open();
            h.Model.SetFilter("d");
            h.Model.SetFilter("du");
            Assert.Equal(3, n);
        }

        [Fact]
        public void AKeystrokeInTheFilterAllocatesNothing()
        {
            var items = new string[500];
            for (int i = 0; i < items.Length; i++) items[i] = "Entry number " + i;
            var m = new ComboModel(() => 0, (i, s) => { }) { Filterable = true };
            m.SetItems(items);
            m.Open();
            string[] queries = { "e", "en", "ent", "entr", "entry", "entry 4", "entry 49", "" };
            foreach (string q in queries) m.SetFilter(q);
            for (int i = 0; i < 20; i++) m.Handle(ScopeKeys.Down);
            m.SetFilter("x");
            AssertionExtensions.ShouldNotAllocate(() =>
            {
                foreach (string q in queries) m.SetFilter(q);
                for (int i = 0; i < 20; i++) m.Handle(ScopeKeys.Down);
            });
        }
    }
}
