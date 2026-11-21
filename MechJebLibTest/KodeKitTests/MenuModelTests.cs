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
    /// <summary>A context menu's entries and the rules that close it (MenuModel), without KSP or Unity.</summary>
    public class MenuModelTests
    {
        private static readonly object _row = "row", _title = "title";

        private sealed class Log
        {
            public readonly List<string> Runs = new List<string>();
            public readonly List<MenuClose> Closes = new List<MenuClose>();
            public ChangeSource Source;
        }

        // Copy (enabled), a separator, a caption, Add (disabled unless addable), Locked (a check)
        private static MenuModel Menu(Log log, out List<MenuEntry> entries, bool addable = false, bool locked = true)
        {
            var m = new MenuModel { Label = "Apoapsis", LeaveDelay = 1, Margin = 8 };
            m.Closed += r => log.Closes.Add(r);
            entries = new List<MenuEntry>
            {
                MenuEntry.Item("Copy value", s =>
                {
                    log.Runs.Add("copy");
                    log.Source = s;
                }),
                MenuEntry.Separator(),
                MenuEntry.Caption("Add to a custom window:"),
                MenuEntry.Item("Orbit Info", s => log.Runs.Add("add"), () => addable),
                MenuEntry.Item("Locked", s => log.Runs.Add("lock"), null, () => locked)
            };
            return m;
        }

        private static MenuModel Opened(Log log, bool addable = false)
        {
            MenuModel m = Menu(log, out List<MenuEntry> e, addable);
            Assert.True(m.Open(_row, e, 10));
            m.Place(new FrameBox(100, 300, 200, 200)); // left, top, right, bottom
            return m;
        }

        [Fact]
        public void OpeningShowsTheEntriesAndStartsWithNothingPicked()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            Assert.True(m.IsOpen);
            Assert.Same(_row, m.Owner);
            Assert.Equal(5, m.Entries.Count);
            Assert.Equal(-1, m.LastPicked);
            Assert.Empty(log.Closes);
        }

        [Fact]
        public void AMenuWithNoActionOpensNothing()
        {
            var m = new MenuModel();
            Assert.False(m.Open(_row, new[] { MenuEntry.Caption("x"), MenuEntry.Separator() }, 0));
            Assert.False(m.IsOpen);
            Assert.Empty(m.Entries);
            Assert.False(m.Open(_row, null, 0));
        }

        [Fact]
        public void OpeningAnotherMenuClosesTheFirstAsReplaced()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            Assert.True(m.Open(_title, new[] { MenuEntry.Item("Close", s => { }) }, 11));
            Assert.Equal(new[] { MenuClose.Replaced }, log.Closes);
            Assert.True(m.IsOpen);
            Assert.Same(_title, m.Owner);
            Assert.Single(m.Entries);
        }

        [Fact]
        public void PickingAnEnabledEntryClosesTheMenuThenRunsItWithThePicksSource()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            bool openDuringRun = true;
            m.Closed += r => openDuringRun = m.IsOpen;
            Assert.True(m.Pick(0, ChangeSource.Agent));
            Assert.Equal(new[] { "copy" }, log.Runs);
            Assert.Equal(ChangeSource.Agent, log.Source);
            Assert.False(openDuringRun);
            Assert.False(m.IsOpen);
            Assert.Equal(MenuClose.Picked, m.LastClose);
            Assert.Equal(0, m.LastPicked);
        }

        [Fact]
        public void AnActionThatOpensAnotherMenuFindsTheFirstClosed()
        {
            var log = new Log();
            var m = new MenuModel();
            m.Closed += r => log.Closes.Add(r);
            m.Open(_row, new[] { MenuEntry.Item("More", s => m.Open(_title, new[] { MenuEntry.Item("Close", x => { }) }, 1)) }, 0);
            Assert.True(m.Pick(0, ChangeSource.Pointer));
            Assert.Equal(new[] { MenuClose.Picked }, log.Closes); // not Replaced: it was closed before the action ran
            Assert.True(m.IsOpen);
            Assert.Same(_title, m.Owner);
        }

        [Fact]
        public void PickingADisabledEntryACaptionOrASeparatorDoesNothingAndTheMenuStays()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            Assert.False(m.Pick(3, ChangeSource.Pointer)); // disabled
            Assert.False(m.Pick(2, ChangeSource.Pointer)); // caption
            Assert.False(m.Pick(1, ChangeSource.Pointer)); // separator
            Assert.False(m.Pick(9, ChangeSource.Pointer));
            Assert.False(m.Pick(-1, ChangeSource.Pointer));
            Assert.Empty(log.Runs);
            Assert.True(m.IsOpen);
            Assert.Empty(log.Closes);
        }

        [Fact]
        public void AnEntryIsEnabledAsItsOwnerSaysWhenPicked()
        {
            var log = new Log();
            bool addable = false;
            MenuModel m = Menu(log, out List<MenuEntry> e);
            e[3] = MenuEntry.Item("Orbit Info", s => log.Runs.Add("add"), () => addable);
            m.Open(_row, e, 0);
            Assert.False(m.Entries[3].IsEnabled());
            addable = true;
            Assert.True(m.Entries[3].IsEnabled());
            Assert.True(m.Pick(3, ChangeSource.Pointer));
            Assert.Equal(new[] { "add" }, log.Runs);
        }

        [Fact]
        public void APickOnAClosedMenuDoesNothing()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            m.Close(MenuClose.Program);
            Assert.False(m.Pick(0, ChangeSource.Pointer));
            Assert.Empty(log.Runs);
        }

        [Fact]
        public void APressOutsideTheMenuClosesIt()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            Assert.True(m.Press(50, 250));
            Assert.Equal(new[] { MenuClose.OutsidePress }, log.Closes);
            Assert.Empty(log.Runs);
        }

        [Fact]
        public void APressOnTheMenuLeavesItToTheEntryUnderThePointer()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            Assert.False(m.Press(150, 250));
            Assert.False(m.Press(100, 300)); // on the edge
            Assert.True(m.IsOpen);
        }

        [Fact]
        public void APressInTheMarginButOffTheBoxIsOutside()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            Assert.True(m.Press(96, 250)); // the margin is for the pointer's resting place, not for presses
        }

        [Fact]
        public void ThePointerOutsideForTheLeaveDelayClosesTheMenu()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            Assert.False(m.PointerAt(150, 250, 10.2));
            Assert.False(m.PointerAt(400, 250, 10.5));
            Assert.True(m.PointerOutside);
            Assert.False(m.Tick(11.49));
            Assert.True(m.IsOpen);
            Assert.True(m.Tick(11.5));
            Assert.Equal(new[] { MenuClose.PointerLeft }, log.Closes);
        }

        [Fact]
        public void ComingBackBeforeTheDelayKeepsTheMenuAndALaterLeaveWaitsAgain()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            m.PointerAt(400, 250, 10);
            m.PointerAt(150, 250, 10.9);
            Assert.False(m.PointerOutside);
            Assert.False(m.Tick(20));
            m.PointerAt(400, 250, 21);
            Assert.False(m.PointerAt(400, 250, 21.9));
            Assert.True(m.PointerAt(400, 250, 22));
            Assert.Equal(MenuClose.PointerLeft, m.LastClose);
        }

        [Fact]
        public void ThePointerOnTheMenusEdgeOrInItsMarginIsInside()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            m.PointerAt(94, 305, 10); // 6 left and 5 above the corner: the opening pointer's place
            m.PointerAt(207, 195, 10);
            Assert.False(m.PointerOutside);
            Assert.False(m.Tick(100));
            m.PointerAt(91, 250, 100);
            Assert.True(m.PointerOutside);
        }

        [Fact]
        public void AMenuNotPlacedYetHoldsThePointer()
        {
            var log = new Log();
            MenuModel m = Menu(log, out List<MenuEntry> e);
            m.Open(_row, e, 0);
            m.PointerAt(5000, 5000, 0);
            Assert.False(m.Tick(10));
        }

        [Fact]
        public void AnyKeyDownClosesTheMenuWithoutAskingWhichKey()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            Assert.True(m.KeyDown());
            Assert.Equal(new[] { MenuClose.KeyPress }, log.Closes);
            Assert.False(m.KeyDown());
        }

        [Fact]
        public void EachMenuClosesOnceWithItsFirstReason()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            m.KeyDown();
            m.Close(MenuClose.OwnerClosed);
            Assert.False(m.Press(0, 0));
            Assert.False(m.Tick(1000));
            Assert.Equal(new[] { MenuClose.KeyPress }, log.Closes);
            Assert.Equal(MenuClose.KeyPress, m.LastClose);
            Assert.Null(m.Owner);
            Assert.Empty(m.Entries);
        }

        [Fact]
        public void APopupLayersDismissalMapsToTheMenusReason()
        {
            Assert.Equal(MenuClose.OutsidePress, MenuModel.FromDismiss(PopupDismiss.OutsidePress));
            Assert.Equal(MenuClose.OwnerClosed, MenuModel.FromDismiss(PopupDismiss.OwnerClosed));
            Assert.Equal(MenuClose.OwnerClosed, MenuModel.FromDismiss(PopupDismiss.ParentClosed));
            Assert.Equal(MenuClose.SceneChange, MenuModel.FromDismiss(PopupDismiss.SceneChange));
            Assert.Equal(MenuClose.Program, MenuModel.FromDismiss(PopupDismiss.Program));
        }

        [Fact]
        public void TheMenuOnAPopupLayerClosesWithItsOwner()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            var layers = new PopupLayers();
            PopupLayer layer = layers.Push(_row);
            layer.Place(m.Box);
            layer.Dismissed += d => m.Close(MenuModel.FromDismiss(d));
            layers.OwnerClosed(_row);
            Assert.Equal(new[] { MenuClose.OwnerClosed }, log.Closes);
        }

        [Fact]
        public void TheDescriptionIsAMenuOfItemsNoneFocusable()
        {
            var log = new Log();
            MenuModel m = Opened(log);
            var parts = new List<PartDescription>();
            m.Describe(parts);
            Assert.Equal(6, parts.Count);
            Assert.Equal(PartRole.Menu, parts[0].Role);
            Assert.Equal("Apoapsis", parts[0].Label);
            Assert.True(parts[0].Has(PartFlags.Expanded));
            Assert.Equal(PartRole.MenuItem, parts[1].Role);
            Assert.Equal("Copy value", parts[1].Label);
            Assert.Equal(PartRole.Separator, parts[2].Role);
            Assert.Equal(PartRole.Group, parts[3].Role);
            Assert.True(parts[4].Has(PartFlags.Disabled));
            Assert.True(parts[5].Has(PartFlags.On));
            Assert.All(parts, p => Assert.False(p.Focusable));
            Assert.All(parts, p => Assert.Empty(p.Keys));
            Assert.Empty(Patterns.Check(m));
        }

        [Fact]
        public void AnEntryWithoutALabelFailsThePatternChecks()
        {
            var m = new MenuModel { Label = "Apoapsis" };
            m.Open(_row, new[] { MenuEntry.Item(null, s => { }) }, 0);
            Assert.Contains(Patterns.Check(m), f => f.Contains("menuitem without a label"));
        }

        [Fact]
        public void NamedEntriesKeepTheirNamesInTheDescription()
        {
            var m = new MenuModel { Label = "Orbit Info" };
            MenuEntry close = MenuEntry.Item("Close", s => { });
            close.Name = "close";
            m.Open(_title, new[] { close }, 0);
            var parts = new List<PartDescription>();
            m.Describe(parts);
            Assert.Equal("close", parts[1].Name);
        }
    }
}
