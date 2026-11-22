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
    /// <summary>How the agent bridge's steps name their controls (ControlMatch), without Unity.</summary>
    public class ControlMatchTests
    {
        private static ControlName C(string r, string label, string id = null, bool takes = true) =>
            new ControlName { Ref = r, Label = label, Id = id, TakesAction = takes };

        private static int Resolve(ControlName[] cs, string target, string id, out List<int> candidates)
        {
            candidates = new List<int>();
            return ControlMatch.Resolve(cs, target, id, candidates);
        }

        [Fact]
        public void TwoControlsWithOneLabelAreAmbiguousNotTheFirstBuilt()
        {
            // a twin stage named by its KSP stage alone: the first check box's id is its label
            ControlName[] cs = { C("w1.CheckBox#0", "Guided (stage 3)", "Guided (stage 3)"), C("w1.CheckBox#2", "Guided (stage 3)") };
            Assert.Equal(-2, Resolve(cs, "Guided (stage 3)", null, out List<int> found));
            Assert.Equal(new[] { 0, 1 }, found);
        }

        [Fact]
        public void AnIdGivenAsAnIdNamesItsControl()
        {
            ControlName[] cs = { C("w1.Switch#0", "Autowarp", "#MechJeb_Autowarp"), C("w1.Field#0", "Autowarp", "#MechJeb_Autowarp/Field") };
            Assert.Equal(1, Resolve(cs, null, "#MechJeb_Autowarp/Field", out _));
            Assert.Equal(0, Resolve(cs, null, "#MechJeb_Autowarp", out _));
        }

        [Fact]
        public void ATextThatIsOneControlsIdAndAnothersLabelIsAmbiguous()
        {
            ControlName[] cs = { C("w1.Switch#0", "Smooth", "Smooth Control"), C("w1.Field#0", "Smooth Control") };
            Assert.Equal(-2, Resolve(cs, "Smooth Control", null, out List<int> found));
            Assert.Equal(2, found.Count);
        }

        [Fact]
        public void AnIdInTwoWindowsIsAmbiguous()
        {
            ControlName[] cs = { C("w1.Toggle#0", "RCS", "#rcs"), C("w2.Toggle#0", "RCS", "#rcs") };
            Assert.Equal(-2, Resolve(cs, null, "#rcs", out _));
        }

        [Fact]
        public void ControlsWithoutTheActionAreLeftOutBeforeTheLabelsAreCompared()
        {
            // a switch and its field on one row: flip finds the switch alone
            ControlName[] cs = { C("w1.Switch#0", "Smooth Control", takes: true), C("w1.Field#0", "Smooth Control", takes: false) };
            Assert.Equal(0, Resolve(cs, "Smooth Control", null, out _));
        }

        [Fact]
        public void ARefNamesItsControlWhateverItsActions()
        {
            ControlName[] cs = { C("w1.b0", "Engage", takes: false), C("w1.b1", "Engage") };
            Assert.Equal(0, Resolve(cs, "w1.b0", null, out _));
        }

        [Fact]
        public void APlainSubstringFindsNothing()
        {
            ControlName[] cs = { C("w1.b0", "Disengage") };
            Assert.Equal(-1, Resolve(cs, "Engage", null, out _));
        }

        [Fact]
        public void AButtonKeepsItsRefWhenAButtonBeforeItGoes()
        {
            var refs = new StableRefs();
            // a window shows three buttons; a section folds and the first goes; the third is still w3.b2
            Assert.Equal("w3.b0", refs.Of(3, 'b', 100));
            Assert.Equal("w3.b1", refs.Of(3, 'b', 101));
            Assert.Equal("w3.b2", refs.Of(3, 'b', 102));
            Assert.Equal("w3.b2", refs.Of(3, 'b', 102));
            Assert.Equal("w3.b1", refs.Of(3, 'b', 101));
        }

        [Fact]
        public void ARefIsNeverGivenToAnotherControl()
        {
            var refs = new StableRefs();
            refs.Of(3, 'b', 100);
            refs.Of(3, 'b', 101);
            // an autopilot swaps its buttons: the new one is w3.b2, not the w3.b0 of the button it replaced
            Assert.Equal("w3.b2", refs.Of(3, 'b', 200));
            // kinds and windows count apart
            Assert.Equal("w3.d0", refs.Of(3, 'd', 100));
            Assert.Equal("w4.b0", refs.Of(4, 'b', 100));
        }

        [Fact]
        public void AClosedWindowsRefsAreForgotten()
        {
            var refs = new StableRefs();
            refs.Of(3, 'b', 100);
            refs.Of(4, 'b', 100);
            refs.KeepOnly(new HashSet<int> { 4 });
            Assert.Equal(1, refs.Windows);
            Assert.Equal("w4.b0", refs.Of(4, 'b', 100));
        }

        [Fact]
        public void TheOneLabelATextBeginsIsItsControl()
        {
            ControlName[] cs = { C("w1.b0", "Disengage"), C("w1.b1", "Engage autopilot") };
            Assert.Equal(1, Resolve(cs, "Engage", null, out _));
        }

        private static string[] Ids(string[] names, params string[] targetIds)
        {
            var ids = new string[names.Length];
            var taken = new HashSet<string>(targetIds);
            ControlMatch.ButtonIds(names, taken.Contains, ids);
            return ids;
        }

        [Fact]
        public void AButtonsIdIsTheKeyItWasBuiltWith()
        {
            Assert.Equal(new[] { "#MechJeb_Maneu_button5", "p2/#MechJeb_Maneu_button5" }, Ids(new[] { "#MechJeb_Maneu_button5", "p2/#MechJeb_Maneu_button5" }));
        }

        [Fact]
        public void TheNthButtonOfOneKeyIsNumberedFromTwo()
        {
            Assert.Equal(new[] { "#K", "#K/2", "#L", "#K/3" }, Ids(new[] { "#K", "#K", "#L", "#K" }));
        }

        [Fact]
        public void AButtonWithoutANameHasNoIdAndTakesNoNumber()
        {
            Assert.Equal(new[] { null, "#K", null, "#K/2" }, Ids(new[] { null, "#K", "", "#K" }));
        }

        [Fact]
        public void AButtonWhoseIdATargetHasTakesButton()
        {
            Assert.Equal(new[] { "#K/button", "#L" }, Ids(new[] { "#K", "#L" }, "#K"));
        }

        [Fact]
        public void AnIdGivenAsAnIdNamesAButtonAsATarget()
        {
            ControlName[] cs = { C("w1.b0", "Create node", "#MechJeb_Maneu_button5"), C("w1.Switch#0", "Create node") };
            Assert.Equal(0, Resolve(cs, null, "#MechJeb_Maneu_button5", out _));
            Assert.Equal(-1, Resolve(cs, null, "Create node", out _));
        }

        [Fact]
        public void AnIdWithoutItsPanelsScopeNamesTheOneShownControlOfIt()
        {
            ControlName[] cs = { C("w1.b0", "Create node", "p0/#MechJeb_Maneu_button1"), C("w1.b1", "Remove", "#MechJeb_Maneu_button3") };
            Assert.Equal(0, Resolve(cs, null, "#MechJeb_Maneu_button1", out _));
            Assert.Equal(0, Resolve(cs, null, "p0/#MechJeb_Maneu_button1", out _));
        }

        [Fact]
        public void AnExactIdWinsOverTheSameIdInAPanel()
        {
            ControlName[] cs = { C("w1.b0", "A", "p0/#K"), C("w1.b1", "B", "#K") };
            Assert.Equal(1, Resolve(cs, null, "#K", out _));
        }

        [Fact]
        public void TwoShownPanelsWithTheUnscopedIdAreAmbiguous()
        {
            ControlName[] cs = { C("w1.b0", "A", "p0/#K"), C("w1.b1", "B", "p1/#K") };
            Assert.Equal(-2, Resolve(cs, null, "#K", out List<int> found));
            Assert.Equal(new[] { 0, 1 }, found);
        }

        [Fact]
        public void UnscopedTakesOffOnlyTheBuildScopes()
        {
            Assert.Equal("#MechJeb_Maneu_STB", ControlMatch.Unscoped("p0/p12/#MechJeb_Maneu_STB"));
            Assert.Equal("#K/2", ControlMatch.Unscoped("p3/#K/2"));
            Assert.Equal("plain/#K", ControlMatch.Unscoped("plain/#K"));
            Assert.Equal("#K", ControlMatch.Unscoped("#K"));
        }

        [Fact]
        public void WithinNamesAWindowByItsModuleWholeAndCaseInsensitive()
        {
            Assert.True(ControlMatch.InWindow("MechJebModuleManeuverPlanner", "机动规划", "w3", "MechJebModuleManeuverPlanner"));
            Assert.True(ControlMatch.InWindow("mechjebmodulemaneuverplanner", "Maneuver Planner", "w3", "MechJebModuleManeuverPlanner"));
            Assert.False(ControlMatch.InWindow("MechJebModuleAscent", "Ascent Settings", "w4", "MechJebModuleAscentSettingsMenu"));
            Assert.True(ControlMatch.InWindow("CustomInfoWindow:Delta_V_Stats", "Delta-V Stats", "w5", "CustomInfoWindow:Delta_V_Stats"));
        }

        [Fact]
        public void WithinStillNamesAWindowByItsRefOrAPartOfItsTitle()
        {
            Assert.True(ControlMatch.InWindow("w3", "Maneuver Planner", "w3", null));
            Assert.True(ControlMatch.InWindow("maneuver", "Maneuver Planner", "w3", "MechJebModuleManeuverPlanner"));
            Assert.False(ControlMatch.InWindow("w3", "Maneuver Planner", "w31", null));
            Assert.True(ControlMatch.InWindow(null, "Maneuver Planner", "w3", null));
        }
    }
}
