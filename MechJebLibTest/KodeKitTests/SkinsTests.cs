/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>MechJeb's skins as rule sets (<see cref="Skins"/>): every part KodeKit draws resolves in every skin, and the states read alike.</summary>
    public class SkinsTests
    {
        private static IEnumerable<StyleFlags> States() => Enumerable.Range(0, StyleStates.Count).Select(i => (StyleFlags)i);

        [Fact]
        public void EveryPartAWidgetDrawsResolvesInEverySkinNormalAndCompact()
        {
            List<StyleSheet> sheets = Skins.All();
            Assert.Equal(Skins.Names.Length * 2, sheets.Count);
            var missing = new List<string>();
            foreach (StyleSheet s in sheets)
            foreach (string part in StyleParts.All)
            {
                StyleTable t = s.Table(part);
                StyleLook rest = t[StyleFlags.None];
                // a part is styled when rules apply and its look at rest says what to draw: a sprite, a fill, a text colour or its visibility
                bool says = rest.SetsSprite || rest.SetsText || rest.SetsHidden || rest.Fill != StyleColor.White || rest.Tint != StyleColor.White;
                if (t.Rules == 0 || !says) missing.Add(s.Name + ": " + part);
            }
            Assert.True(missing.Count == 0, "parts no rule styles: " + string.Join(", ", missing));
        }

        [Fact]
        public void EveryRuleOfEverySkinIsForAPartAWidgetDraws()
        {
            var stray = new List<string>();
            foreach (StyleSheet s in Skins.All())
            foreach (StyleRule r in s.Rules)
                if (!StyleParts.All.Any(r.AppliesTo))
                    stray.Add(s.Name + ": " + r.Part);
            Assert.True(stray.Count == 0, "rules for parts no widget draws (a typo?): " + string.Join(", ", stray.Distinct()));
        }

        [Fact]
        public void EverySpriteASkinNamesIsAKnownSpriteId()
        {
            string[] unknown = Skins.All().SelectMany(s => s.Sprites).Where(id => !SpriteIds.All.Contains(id)).Distinct().ToArray();
            Assert.Empty(unknown);
        }

        [Fact]
        public void ACompactSkinPaintsEveryPartInEveryStateAsItsSkin()
        {
            foreach (string name in Skins.Names)
            {
                StyleSheet normal = Skins.For(name), compact = Skins.Compact(name);
                Assert.Equal("Compact (" + name + ")", compact.Name);
                foreach (string part in StyleParts.All)
                foreach (StyleFlags f in States())
                    Assert.True(normal.Resolve(part, f).SameAs(compact.Resolve(part, f)), $"{name} {part} {f}");
            }
        }

        [Fact]
        public void EverySkinDrawsTheSameThingsInEachStateOnlyItsColoursDiffer()
        {
            StyleSheet first = Skins.For(Skins.Names[0]);
            foreach (string name in Skins.Names.Skip(1))
            {
                StyleSheet s = Skins.For(name);
                foreach (string part in StyleParts.All)
                foreach (StyleFlags f in States())
                {
                    StyleLook a = first.Resolve(part, f), b = s.Resolve(part, f);
                    string where = $"{name} {part} {f}";
                    Assert.True(a.SetsSprite == b.SetsSprite && a.Sprite == b.Sprite, where + ": sprite");
                    Assert.True(a.SetsHidden == b.SetsHidden && a.Hidden == b.Hidden, where + ": shown");
                    Assert.True(a.SetsBold == b.SetsBold && a.Bold == b.Bold, where + ": weight");
                    Assert.True(a.SetsText == b.SetsText, where + ": text");
                }
            }
        }

        [Fact]
        public void DisabledBeatsPressedBeatsFocusedBeatsHoveredAsInUguisTransitions()
        {
            StyleSheet s = Skins.For("Flat");
            StyleColor wash = new StyleColor(1, 1, 1, 0.1f), press = new StyleColor(1, 1, 1, 0.18f);
            Assert.Equal(wash, s.Resolve(StyleParts.Segment, StyleFlags.Hover).Tint);
            // a button just clicked is uGUI's selected: its normal tint, not the hover one
            Assert.Equal(StyleColor.Clear, s.Resolve(StyleParts.Segment, StyleFlags.Hover | StyleFlags.Focus).Tint);
            Assert.Equal(press, s.Resolve(StyleParts.Segment, StyleFlags.Hover | StyleFlags.Focus | StyleFlags.Press).Tint);
            Assert.Equal(Skins.DisabledTint(StyleColor.Clear),
                s.Resolve(StyleParts.Segment, StyleFlags.Hover | StyleFlags.Press | StyleFlags.Focus | StyleFlags.Disabled).Tint);
        }

        [Fact]
        public void AChosenSegmentKeepsItsOwnTintsInEveryState()
        {
            StyleSheet s = Skins.For("Classic");
            Assert.Equal(StyleColor.White, s.Resolve(StyleParts.Segment, StyleFlags.On).Tint);
            Assert.Equal(StyleColor.Grey(0.92f), s.Resolve(StyleParts.Segment, StyleFlags.On | StyleFlags.Hover).Tint);
            Assert.Equal(StyleColor.Grey(0.8f), s.Resolve(StyleParts.Segment, StyleFlags.On | StyleFlags.Hover | StyleFlags.Press).Tint);
            Assert.Equal(Skins.DisabledTint(StyleColor.White), s.Resolve(StyleParts.Segment, StyleFlags.On | StyleFlags.Hover | StyleFlags.Disabled).Tint);
            Assert.Equal(SpriteIds.RoleSelected, s.Resolve(StyleParts.Segment, StyleFlags.On | StyleFlags.Hover).Sprite);
            Assert.Null(s.Resolve(StyleParts.Segment, StyleFlags.Hover).Sprite);
        }

        [Fact]
        public void ADisabledTintIsUguisGreyAsSeeThroughAsTheTintAtRest()
        {
            Assert.Equal(StyleColor.FromBytes(200, 200, 200, 128), Skins.DisabledTint(StyleColor.White));
            Assert.Equal(0, Skins.DisabledTint(StyleColor.Clear).A);
        }

        [Fact]
        public void ASwitchIsTheAccentWhenOnAndItsKnobWhite()
        {
            foreach (string name in Skins.Names)
            {
                StyleSheet s = Skins.For(name);
                SkinPalette p = Skins.PaletteFor(name);
                Assert.Equal(p.Accent, s.Resolve(StyleParts.Switch, StyleFlags.On).Fill);
                Assert.Equal(p.Dim.WithAlpha(0.35f), s.Resolve(StyleParts.Switch, StyleFlags.None).Fill);
                Assert.Equal(StyleColor.White, s.Resolve(StyleParts.SwitchKnob, StyleFlags.On).Fill);
                Assert.Equal(p.Text.WithAlpha(0.9f), s.Resolve(StyleParts.SwitchKnob, StyleFlags.None).Fill);
            }
        }

        [Fact]
        public void TheChosenTabIsBoldAndBrightOverItsUnderline()
        {
            StyleSheet s = Skins.For("Flat");
            SkinPalette p = Skins.PaletteFor("Flat");
            StyleLook off = s.Resolve(StyleParts.Tab, StyleFlags.Hover), on = s.Resolve(StyleParts.Tab, StyleFlags.On | StyleFlags.Hover);
            Assert.False(off.Bold);
            Assert.Equal(p.Dim, off.Text);
            Assert.True(on.Bold);
            Assert.Equal(p.Text, on.Text);
            Assert.Equal(StyleColor.Clear, on.Tint);
            Assert.True(s.Resolve(StyleParts.TabLine, StyleFlags.None).Hidden);
            Assert.False(s.Resolve(StyleParts.TabLine, StyleFlags.On).Hidden);
        }

        [Fact]
        public void TheFocusFrameIsOneSpriteInTheAccentShownOnlyWithFocus()
        {
            foreach (StyleSheet s in Skins.All())
            {
                Assert.True(s.Resolve(StyleParts.FocusFrame, StyleFlags.None).Hidden);
                StyleLook f = s.Resolve(StyleParts.FocusFrame, StyleFlags.Focus);
                Assert.False(f.Hidden);
                Assert.Equal(SpriteIds.Frame, f.Sprite);
            }
        }

        [Fact]
        public void ARoleButtonTakesItsRolesSpriteFillAndTextAndTheButtonsTints()
        {
            SkinPalette p = Skins.PaletteFor("Classic");
            StyleSheet s = Skins.For("Classic");
            StyleLook danger = s.Resolve(StyleParts.ButtonDanger, StyleFlags.Hover);
            Assert.Equal(SpriteIds.RoleDanger, danger.Sprite);
            Assert.Equal(p.DangerFill, danger.Fill);
            Assert.Equal(p.DangerText, danger.Text);
            Assert.Equal(StyleColor.Grey(0.92f), danger.Tint);
            Assert.Equal(StyleColor.Grey(0.78f), s.Resolve(StyleParts.ButtonPrimary, StyleFlags.Press).Tint);
        }

        [Fact]
        public void TheCloseButtonRefinesTheTitleIconWithARedHover()
        {
            StyleSheet s = Skins.For("Glass");
            Assert.Equal(new StyleColor(0.85f, 0.25f, 0.2f, 0.85f), s.Resolve(StyleParts.TitleClose, StyleFlags.Hover).Tint);
            Assert.Equal(s.Resolve(StyleParts.TitleIcon, StyleFlags.Hover).Text, s.Resolve(StyleParts.TitleClose, StyleFlags.Hover).Text);
            Assert.NotEqual(s.Resolve(StyleParts.TitleIcon, StyleFlags.Hover).Tint, s.Resolve(StyleParts.TitleClose, StyleFlags.Hover).Tint);
        }

        // the part KodeKit draws each model's parts with (KodeWindow.Layouts.cs, Dialog.cs): the model's flags paint it
        [Fact]
        public void TheFlagsAModelReportsPaintThePartItsWidgetDraws()
        {
            StyleSheet s = Skins.For("Flat");
            SkinPalette p = Skins.PaletteFor("Flat");
            var parts = new List<PartDescription>();
            new ToggleModel(ToggleKind.Switch, () => true, (v, src) => { }) { Label = "Autostage" }.Describe(parts);
            Assert.Equal(p.Accent, s.Resolve(StyleParts.Switch, StyleStates.From(parts[0].Flags)).Fill);

            parts.Clear();
            new ChoiceModel(3, () => 1, (v, src) => { }) { Label = "Mode" }.Describe(parts);
            PartDescription chosen = parts.Single(d => d.Name == "item.1"), other = parts.Single(d => d.Name == "item.0");
            Assert.Equal(p.SelectedText, s.Resolve(StyleParts.Segment, StyleStates.From(chosen.Flags)).Text);
            Assert.Equal(p.Text, s.Resolve(StyleParts.Segment, StyleStates.From(other.Flags) & ~StyleFlags.Focus).Text);
            Assert.Equal(p.AccentText, s.Resolve(StyleParts.Pill, StyleStates.From(chosen.Flags)).Text);

            parts.Clear();
            var dialog = new DialogModel("Delete window?", "It can't be undone.") { ConfirmText = "Delete", Destructive = true };
            dialog.Describe(parts);
            foreach (PartDescription stop in parts.Where(d => d.Role == PartRole.Button))
                Assert.Equal(!stop.Has(PartFlags.Focus), s.Resolve(StyleParts.FocusFrame, StyleStates.From(stop.Flags)).Hidden);
            Assert.Contains(parts, d => d.Has(PartFlags.Focus));
        }

        [Fact]
        public void TheDefaultPalettesAreMechJebsSkins()
        {
            Assert.Equal(Skins.Names, new[] { "Classic", "Flat", "Glass", "ZTheme" });
            foreach (string n in Skins.Names) Assert.Equal(n, Skins.PaletteFor(n).Name);
            Assert.Null(Skins.PaletteFor("Default"));
            // the drawn skins carry their colour in their role sprites; Classic tints KSP's button by role
            Assert.Equal(StyleColor.White, Skins.PaletteFor("Flat").PrimaryFill);
            Assert.Equal(new StyleColor(0.62f, 0.8f, 1f), Skins.PaletteFor("Classic").PrimaryFill);
        }

        private static string UiFolder([CallerFilePath] string here = "") =>
            Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", "..", "MechJeb2.UI"));

        [Fact]
        public void EveryStylePartIsDrawnByAKodeKitWidgetAndEveryPartAWidgetNamesIsAStylePart()
        {
            string ui = UiFolder();
            if (!Directory.Exists(ui)) return; // the sources are not next to this build
            var used = new HashSet<string>();
            var literal = new List<string>();
            var name = new Regex(@"StyleParts\.(\w+)");
            var add = new Regex(@"StyledPart\.Add\([^,]+,\s*""|\.Follower\([^,]+,\s*""|\bPart\([^,]+,[^,]+,\s*""");
            foreach (string file in Directory.GetFiles(ui, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "Headless" + Path.DirectorySeparatorChar)) continue;
                string text = File.ReadAllText(file);
                foreach (Match m in name.Matches(text)) used.Add(m.Groups[1].Value);
                if (add.IsMatch(text)) literal.Add(Path.GetFileName(file));
            }
            string[] constants = typeof(StyleParts).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.IsLiteral).Select(f => f.Name).ToArray();
            Assert.True(constants.Length == StyleParts.All.Length, "StyleParts.All lists " + StyleParts.All.Length + " of " + constants.Length + " parts");
            string[] unused = constants.Where(c => !used.Contains(c)).ToArray();
            Assert.True(unused.Length == 0, "style parts no widget draws: " + string.Join(", ", unused));
            Assert.True(literal.Count == 0, "a part named by a literal, not a StyleParts constant: " + string.Join(", ", literal));
        }
    }
}
