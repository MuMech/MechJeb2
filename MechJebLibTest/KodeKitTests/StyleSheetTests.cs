/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The rule tables (<see cref="StyleSheet"/>): matching, specificity and the compiled looks.</summary>
    public class StyleSheetTests
    {
        private static readonly StyleColor Red = new StyleColor(1, 0, 0), Green = new StyleColor(0, 1, 0), Blue = new StyleColor(0, 0, 1);

        [Fact]
        public void ARuleAppliesToItsPartAndToThePartsThatRefineIt()
        {
            var s = new StyleSheet("t");
            s.Rule("button").Fill(Red);
            Assert.Equal(Red, s.Resolve("button", StyleFlags.None).Fill);
            Assert.Equal(Red, s.Resolve("button.primary", StyleFlags.None).Fill);
            Assert.Equal(Red, s.Resolve("button.primary.big", StyleFlags.None).Fill);
        }

        [Fact]
        public void ARuleDoesNotApplyToAPartThatOnlySharesItsPrefix()
        {
            var s = new StyleSheet("t");
            s.Rule("switch").Fill(Red);
            Assert.Equal(0, s.Table("switch-knob").Rules);
            Assert.Equal(0, s.Table("switches").Rules);
            Assert.Equal(StyleColor.White, s.Resolve("switch-knob", StyleFlags.None).Fill);
        }

        [Fact]
        public void TheDeeperPartWinsOverItsParentWhateverTheirFlags()
        {
            var s = new StyleSheet("t");
            s.Rule("button.primary").Fill(Green);
            s.Rule("button", StyleFlags.Hover | StyleFlags.Press).Fill(Red);
            Assert.Equal(Green, s.Resolve("button.primary", StyleFlags.Hover | StyleFlags.Press).Fill);
        }

        [Fact]
        public void MoreNeededFlagsWinOverFewer()
        {
            var s = new StyleSheet("t");
            s.Rule("cell", StyleFlags.On | StyleFlags.Hover).Tint(Green);
            s.Rule("cell", StyleFlags.Hover).Tint(Red);
            Assert.Equal(Green, s.Resolve("cell", StyleFlags.On | StyleFlags.Hover).Tint);
            Assert.Equal(Red, s.Resolve("cell", StyleFlags.Hover).Tint);
        }

        [Fact]
        public void OfTwoEquallySpecificRulesTheLaterWins()
        {
            var s = new StyleSheet("t");
            s.Rule("cell", StyleFlags.Hover).Tint(Red);
            s.Rule("cell", StyleFlags.Press).Tint(Green);
            Assert.Equal(Green, s.Resolve("cell", StyleFlags.Hover | StyleFlags.Press).Tint);
        }

        [Fact]
        public void ARuleSetsOnlyTheValuesItNamesTheOthersComeFromLessSpecificRules()
        {
            var s = new StyleSheet("t");
            s.Rule("cell").Sprite("pill").Fill(Red).Text(Blue);
            s.Rule("cell", StyleFlags.On).Fill(Green);
            StyleLook on = s.Resolve("cell", StyleFlags.On);
            Assert.Equal(Green, on.Fill);
            Assert.Equal("pill", on.Sprite);
            Assert.Equal(Blue, on.Text);
        }

        [Fact]
        public void ANotFlagExcludesTheStatesThatHaveIt()
        {
            var s = new StyleSheet("t");
            s.Rule("cell").Fill(Red);
            s.Rule("cell", StyleFlags.Hover, StyleFlags.On).Fill(Green);
            Assert.Equal(Green, s.Resolve("cell", StyleFlags.Hover).Fill);
            Assert.Equal(Red, s.Resolve("cell", StyleFlags.Hover | StyleFlags.On).Fill);
        }

        [Fact]
        public void AValueNoRuleSetsKeepsItsDefault()
        {
            var s = new StyleSheet("t");
            s.Rule("cell").Tint(Red);
            StyleLook l = s.Resolve("cell", StyleFlags.None);
            Assert.False(l.SetsSprite);
            Assert.Equal(-1, l.SpriteIndex);
            Assert.Equal(StyleColor.White, l.Fill);
            Assert.False(l.SetsText);
            Assert.False(l.SetsBold);
            Assert.False(l.SetsHidden);
        }

        [Fact]
        public void ANullSpriteIsSetAsNoSprite()
        {
            var s = new StyleSheet("t");
            s.Rule("cell").Sprite("pill");
            s.Rule("cell", StyleFlags.On).Sprite(null);
            StyleLook l = s.Resolve("cell", StyleFlags.On);
            Assert.True(l.SetsSprite);
            Assert.Null(l.Sprite);
            Assert.Equal(-1, l.SpriteIndex);
        }

        [Fact]
        public void APartsTableIsCompiledOnceAndKept()
        {
            var s = new StyleSheet("t");
            s.Rule("cell").Fill(Red);
            Assert.Same(s.Table("cell"), s.Table("cell"));
        }

        [Fact]
        public void ARuleAddedOrChangedAfterTheFirstTableThrows()
        {
            var s = new StyleSheet("t");
            StyleRule r = s.Rule("cell").Fill(Red);
            s.Table("cell");
            Assert.True(s.Frozen);
            Assert.Throws<InvalidOperationException>(() => s.Rule("cell", StyleFlags.On));
            Assert.Throws<InvalidOperationException>(() => r.Fill(Green));
        }

        [Fact]
        public void ARuleThatNeedsAndExcludesTheSameFlagThrows()
        {
            var s = new StyleSheet("t");
            Assert.Throws<ArgumentException>(() => s.Rule("cell", StyleFlags.On, StyleFlags.On));
            Assert.Throws<ArgumentException>(() => s.Rule(""));
        }

        [Fact]
        public void SpriteIdsAreNumberedOnceInTheOrderTheRulesNameThem()
        {
            var s = new StyleSheet("t");
            s.Rule("a").Sprite("pill");
            s.Rule("b").Sprite("frame");
            s.Rule("c").Sprite("pill");
            s.Rule("d").Sprite(null);
            Assert.Equal(1, s.Resolve("b", StyleFlags.None).SpriteIndex);
            Assert.Equal(new[] { "pill", "frame" }, s.Sprites);
            Assert.Equal(0, s.Resolve("c", StyleFlags.None).SpriteIndex);
        }

        [Fact]
        public void EveryCombinationOfTheSixFlagsHasALook()
        {
            var s = new StyleSheet("t");
            s.Rule("cell", StyleFlags.Invalid).Fill(Red);
            StyleTable t = s.Table("cell");
            StyleFlags all = StyleFlags.Hover | StyleFlags.Press | StyleFlags.Focus | StyleFlags.On | StyleFlags.Disabled | StyleFlags.Invalid;
            Assert.Equal(StyleStates.Count - 1, (int)all);
            Assert.Equal(Red, t[all].Fill);
            Assert.Equal(StyleColor.White, t[all & ~StyleFlags.Invalid].Fill);
        }

        [Fact]
        public void AModelsOnFocusDisabledAndInvalidAreStyleFlagsTheOtherPartFlagsAreNot()
        {
            Assert.Equal(StyleFlags.On | StyleFlags.Focus | StyleFlags.Disabled | StyleFlags.Invalid,
                StyleStates.From(PartFlags.On | PartFlags.Focus | PartFlags.Disabled | PartFlags.Invalid | PartFlags.Expanded | PartFlags.Mixed | PartFlags.Grabbed));
            Assert.Equal(StyleFlags.None, StyleStates.From(PartFlags.Expanded));
        }

        [Fact]
        public void AColourFromBytesIsUnitysColour32Conversion()
        {
            StyleColor c = StyleColor.FromBytes(200, 200, 200, 128);
            Assert.Equal(200 / 255f, c.R);
            Assert.Equal(128 / 255f, c.A);
            Assert.Equal(new StyleColor(0x4C / 255f, 0xAC / 255f, 0x50 / 255f), StyleColor.Rgb(0x4CAC50));
        }
    }
}
