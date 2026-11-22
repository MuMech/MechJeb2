/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>ui-sdk's pattern checks (labels, roles, one item on, keys handled) on every model of the headless folder.</summary>
    public class PatternChecksTests
    {
        private static ChoiceModel Choice(int count, int value, string label = "Mode", bool itemLabels = true)
        {
            int v = value;
            return new ChoiceModel(count, () => v, i => v = i)
            {
                Label      = label,
                ItemLabels = itemLabels ? Enumerable.Range(0, count).Select(i => "item " + i).ToArray() : null
            };
        }

        private static StepperModel Stepper(string label, bool valid = true) =>
            new StepperModel(() => "1", _ => { }, _ => { }, () => 1, () => valid) { Label = label };

        private static ComboModel Combo(bool open, bool filterable = false, bool grouped = false)
        {
            int v = 1;
            var c = new ComboModel(() => v, (i, s) => v = i) { Label = "Body", Filterable = filterable };
            if (grouped) c.Group = i => i < 2 ? "Planets" : "Moons";
            c.SetItems(new[] { "Kerbin", "Duna", "Mun", "Minmus" });
            if (open) c.Open();
            return c;
        }

        private static InlineEditModel Inline(bool editing, bool valid = true)
        {
            string value = "100";
            var m = new InlineEditModel(() => value, (s, src) => value = s, s => valid) { Label = "Altitude" };
            if (editing) m.Begin();
            return m;
        }

        private static RangeModel Range(bool withRange)
        {
            var r = new RangeModel(new ViewportModel(0, 100)) { Label = "Altitude" };
            if (withRange) r.Select(10, 20, ChangeSource.Pointer);
            r.AddPoint(50, ChangeSource.Pointer);
            return r;
        }

        private static TimelineModel Timeline()
        {
            var t = new TimelineModel(new ViewportModel(0, 100)) { Label = "Events", LaneCount = 2 };
            t.Set(1, 0, 10, 0, 0, "Burn 1");
            t.Set(2, 20, double.NaN, 1, 0, "Staging");
            t.SelectId(2, ChangeSource.Pointer);
            return t;
        }

        private static TreeCollection<string> Tree()
        {
            var t = new TreeCollection<string> { Label = "Items" };
            t.AddBranch("orbit", "Orbit");
            t.Add("orbit/ap", "Apoapsis", "ap", "orbit");
            t.Add("orbit/pe", "Periapsis", "pe", "orbit");
            t.Open("orbit");
            t.Check("orbit/ap", true);
            return t;
        }

        private static ToastQueue Toasts()
        {
            var q = new ToastQueue { Label = "Notifications" };
            q.Show("Window deleted.", ToastSeverity.Info, 0);
            q.Show("Copied.", ToastSeverity.Success, 0);
            return q;
        }

        private static TextFieldModel Text(bool valid) =>
            new TextFieldModel(() => "1", (s, src) => { }, TextCommit.AsTyped, null, () => valid) { Label = "Altitude" };

        private static ToggleModel Toggle(ToggleKind kind, bool on) => new ToggleModel(kind, () => on, (v, s) => { }) { Label = "Autostage" };

        private static Switch3Model Switch3(int at) =>
            new Switch3Model(new[] { "Off", "Auto", "On" }, () => at, (v, s) => { }) { Label = "Mode" };

        private static TableModel Table(bool selected)
        {
            var t = new TableModel(() => 2, (int r, int k, CharBuffer b, ref string u) => b.AppendInt(r * 10 + k)) { Label = "Stage stats" }.WithHighlight(() => 1);
            t.AddColumn(0, "Stage");
            t.AddColumn(1, "ΔV", TableWidthRule.Content, 0, " m/s");
            t.Sync();
            if (selected)
            {
                t.Selection.ToggleRow(0, ChangeSource.Pointer);
                t.Selection.SelectCells(0, 0, 1, 1, ChangeSource.Pointer);
            }
            return t;
        }

        private static MenuModel Menu()
        {
            var m = new MenuModel { Label = "Apoapsis" };
            m.Open("row", new[] { MenuEntry.Caption("Apoapsis"), MenuEntry.Item("Copy value", s => { }), MenuEntry.Separator(),
                MenuEntry.Item("Orbit Info", s => { }, () => false), MenuEntry.Item("Locked", s => { }, null, () => true) }, 0);
            return m;
        }

        private static PaletteModel Palette(bool armed)
        {
            var m = new PaletteModel { Label = "Command palette" };
            m.Open(new[]
            {
                new PaletteCommand("window:Ascent", () => "Ascent Guidance", "AscentMenu", s => { }),
                new PaletteCommand("ascent:engage", () => "Engage ascent autopilot", null, s => { }).Confirm(() => "Engages it"),
                new PaletteCommand("node:one", () => "Execute next node", null, s => { }).When(() => false, () => "No node")
            });
            if (armed)
            {
                m.Move(1);
                m.Enter(ChangeSource.Keyboard);
            }
            return m;
        }

        private static DialogModel Prompt(string text) => new DialogModel("Rename", "Name:").MakePrompt(text, s => s.Length > 0);

        // One labelled, well-formed example of each widget model, in the states it is drawn in; a widget model added to the folder
        // needs an entry here (a generic model under its generic definition).
        private static readonly Dictionary<Type, Func<IDescribeParts[]>> Samples = new Dictionary<Type, Func<IDescribeParts[]>>
        {
            [typeof(ChoiceModel)]      = () => new IDescribeParts[] { Choice(1, 0), Choice(2, 1), Choice(5, 4) },
            [typeof(StepperModel)]     = () => new IDescribeParts[] { Stepper("Altitude"), Stepper("Altitude", false) },
            [typeof(ComboModel)]       = () => new IDescribeParts[] { Combo(false), Combo(true), Combo(true, true), Combo(true, false, true) },
            [typeof(DialogModel)]      = () => new IDescribeParts[] { new DialogModel("Delete window?", "It can't be undone.") { ConfirmText = "Delete", Destructive = true }, Prompt("Ascent"), Prompt("") },
            [typeof(InlineEditModel)]  = () => new IDescribeParts[] { Inline(false), Inline(true), Inline(true, false) },
            [typeof(ViewportModel)]    = () => new IDescribeParts[] { new ViewportModel(0, 100) { Label = "Altitude" } },
            [typeof(RangeModel)]       = () => new IDescribeParts[] { Range(false), Range(true) },
            [typeof(TimelineModel)]    = () => new IDescribeParts[] { Timeline() },
            [typeof(WindowFrameModel)] = () => new IDescribeParts[] { new WindowFrameModel { Label = "Ascent Guidance" } },
            [typeof(ResizeModel)]      = () => new IDescribeParts[] { new ResizeModel { Label = "Ascent Guidance" } },
            [typeof(TreeCollection<>)] = () => new IDescribeParts[] { Tree() },
            [typeof(ToastQueue)]       = () => new IDescribeParts[] { Toasts() },
            [typeof(TextFieldModel)]   = () => new IDescribeParts[] { Text(true), Text(false) },
            [typeof(ToggleModel)]      = () => new IDescribeParts[] { Toggle(ToggleKind.Switch, true), Toggle(ToggleKind.CheckBox, false) },
            [typeof(Switch3Model)]     = () => new IDescribeParts[] { Switch3(0), Switch3(2) },
            [typeof(SliderModel)]      = () => new IDescribeParts[] { new SliderModel(() => 0.5f, (v, s) => { }, 0, 1) { Label = "Opacity" } },
            [typeof(TableModel)]       = () => new IDescribeParts[] { Table(false), Table(true) },
            [typeof(MenuModel)]        = () => new IDescribeParts[] { Menu() },
            [typeof(GaugeModel)]       = () => new IDescribeParts[] { new GaugeModel(() => 0.5) { Label = "Throttle" }, new GaugeModel(() => double.NaN) { Label = "Acceleration" } },
            [typeof(PaletteModel)]     = () => new IDescribeParts[] { Palette(false), Palette(true) }
        };

        // every class of the headless folder that can be made: static classes, attributes, delegate types and nested types are not models
        private static Type[] FolderClasses() =>
            typeof(ChoiceModel).Assembly.GetTypes()
                .Where(t => t.Namespace == "KodeKit.Headless" && t.IsClass && t.IsPublic && !t.IsNested && !t.IsAbstract &&
                            !typeof(Attribute).IsAssignableFrom(t) && !typeof(Delegate).IsAssignableFrom(t))
                .ToArray();

        private static bool Describes(Type t) => typeof(IDescribeParts).IsAssignableFrom(t);

        private static NotAWidgetAttribute NotAWidget(Type t) =>
            (NotAWidgetAttribute)Attribute.GetCustomAttribute(t, typeof(NotAWidgetAttribute), false);

        [Fact]
        public void EveryClassInTheFolderDescribesItsPartsOrSaysWhyNot()
        {
            Type[] classes = FolderClasses();
            Assert.True(classes.Length > 20, "found " + classes.Length + " classes");
            string[] neither = classes.Where(t => !Describes(t) && NotAWidget(t) == null).Select(t => t.Name).ToArray();
            Assert.True(neither.Length == 0, "classes that neither describe their parts nor carry [NotAWidget]: " + string.Join(", ", neither));
        }

        [Fact]
        public void NoClassBothDescribesItsPartsAndSaysItIsNoWidget()
        {
            string[] both = FolderClasses().Where(t => Describes(t) && NotAWidget(t) != null).Select(t => t.Name).ToArray();
            Assert.True(both.Length == 0, "both: " + string.Join(", ", both));
        }

        [Fact]
        public void EveryNotAWidgetMarkSaysWhy()
        {
            string[] silent = FolderClasses().Where(t => NotAWidget(t) != null && string.IsNullOrWhiteSpace(NotAWidget(t).Why)).Select(t => t.Name).ToArray();
            Assert.Empty(silent);
        }

        [Fact]
        public void EveryDescribingModelHasASampleHere()
        {
            Type[] all = FolderClasses().Where(Describes).ToArray();
            string[] missing = all.Where(t => !Samples.ContainsKey(t)).Select(t => t.Name).ToArray();
            Assert.True(all.Length >= 12 && missing.Length == 0, "models without a sample in PatternChecksTests.Samples: " + string.Join(", ", missing));
        }

        [Fact]
        public void EverySampleHasALabelAndAtLeastOnePart()
        {
            foreach (KeyValuePair<Type, Func<IDescribeParts[]>> e in Samples)
            foreach (IDescribeParts m in e.Value())
            {
                Assert.False(string.IsNullOrEmpty(m.Label), e.Key.Name + " has no label");
                var parts = new List<PartDescription>();
                m.Describe(parts);
                Assert.NotEmpty(parts);
            }
        }

        [Fact]
        public void AnOpenDropDownDescribesItsListAndHighlightsOneOption()
        {
            ComboModel c = Combo(true);
            var parts = new List<PartDescription>();
            c.Describe(parts);
            Assert.True(parts[0].Has(PartFlags.Expanded));
            Assert.Single(parts, p => p.Role == PartRole.ListBox);
            Assert.Equal(4, parts.Count(p => p.Role == PartRole.Option));
            Assert.Single(parts, p => p.Role == PartRole.Option && p.Has(PartFlags.On) && p.Label == "Duna");
            Assert.Single(parts, p => p.Role == PartRole.Option && p.Has(PartFlags.Focus));
            c.Close();
            parts.Clear();
            c.Describe(parts);
            Assert.Single(parts);
            Assert.False(parts[0].Has(PartFlags.Expanded));
        }

        [Fact]
        public void ADialogDescribesItsStopsInFocusOrderAndItsInvalidInput()
        {
            var parts = new List<PartDescription>();
            Prompt("").Describe(parts);
            Assert.Equal(new[] { "dialog", "input", "confirm", "cancel" }, parts.Select(p => p.Name).ToArray());
            Assert.True(parts[1].Has(PartFlags.Invalid) && parts[1].Has(PartFlags.Focus));
            Assert.True(parts[2].Has(PartFlags.Disabled));
            Assert.Equal("Rename", parts[0].Label);
        }

        [Fact]
        public void ATreeDescribesItsOpenAndCheckedRows()
        {
            var parts = new List<PartDescription>();
            Tree().Describe(parts);
            PartDescription orbit = parts.Single(p => p.Name == "node.orbit");
            Assert.True(orbit.Has(PartFlags.Expanded));
            Assert.True(orbit.Has(PartFlags.Mixed));
            Assert.True(parts.Single(p => p.Name == "node.orbit/ap").Has(PartFlags.On));
        }

        [Fact]
        public void TwoHighlightedOptionsFail()
        {
            var list = new Listed(new PartDescription { Name = "a", Role = PartRole.Option, Label = "a", Flags = PartFlags.Focus },
                new PartDescription { Name = "b", Role = PartRole.Option, Label = "b", Flags = PartFlags.Focus | PartFlags.On },
                new PartDescription { Name = "c", Role = PartRole.Option, Label = "c", Flags = PartFlags.On });
            List<string> f = Patterns.Check(list);
            Assert.Contains(f, x => x.Contains("2 options highlighted"));
            Assert.Contains(f, x => x.Contains("2 options on"));
        }

        private sealed class Listed : IDescribeParts
        {
            private readonly PartDescription[] parts;

            public Listed(params PartDescription[] parts) => this.parts = parts;

            public string Label { get; set; } = "List";

            public void Describe(List<PartDescription> into) => into.AddRange(parts);
        }

        [Fact]
        public void EverySampleOfEveryModelPassesTheChecks()
        {
            var failures = new List<string>();
            foreach (KeyValuePair<Type, Func<IDescribeParts[]>> e in Samples)
            foreach (IDescribeParts m in e.Value())
            foreach (string f in Patterns.Check(m))
                failures.Add(e.Key.Name + ": " + f);
            Assert.True(failures.Count == 0, string.Join("\n", failures));
        }

        [Fact]
        public void ARadioGroupWithoutAnItemOnFails()
        {
            ChoiceModel c = Choice(3, 7); // a value that is no entry selects nothing
            Assert.Contains(Patterns.Check(c), f => f.Contains("0 items on, not 1"));
        }

        [Fact]
        public void AnUnlabelledControlAndItsPartsFail()
        {
            List<string> f = Patterns.Check(Choice(2, 0, null, false));
            Assert.Contains("the control has no label", f);
            Assert.Contains("part group: group without a label", f);
            Assert.Contains("part item.0: focusable without a label", f);
            List<string> s = Patterns.Check(Stepper(null));
            Assert.Contains("part field: focusable without a label", s);
            Assert.Contains("part dec: button without a label", s);
        }

        [Fact]
        public void AFocusablePartThatHandlesNoKeyFails()
        {
            Assert.Contains(Patterns.Check(new Custom(p => p.Keys = new PartKey[0])), f => f.Contains("handles no key"));
        }

        [Fact]
        public void TwoFocusedRadiosAndRepeatedNamesFail()
        {
            List<string> f = Patterns.Check(new Custom(p => { }, true));
            Assert.Contains(f, x => x.Contains("2 items focused"));
            Assert.Contains(f, x => x.Contains("the name is used twice"));
        }

        [Fact]
        public void ChoiceKeysItDescribesAreKeysItHandles()
        {
            ChoiceModel c = Choice(3, 1);
            var parts = new List<PartDescription>();
            c.Describe(parts);
            var map = new Dictionary<PartKey, ChoiceKey>
            {
                [PartKey.Left] = ChoiceKey.Left, [PartKey.Right] = ChoiceKey.Right, [PartKey.Home] = ChoiceKey.Home,
                [PartKey.End]  = ChoiceKey.End, [PartKey.Enter] = ChoiceKey.Enter, [PartKey.Space] = ChoiceKey.Space
            };
            foreach (PartKey k in parts.SelectMany(p => p.Keys).Distinct())
                Assert.True(map.ContainsKey(k) && c.Key(map[k]), "key " + k);
        }

        [Fact]
        public void ListOptionKeysItDescribesAreKeysItHandles()
        {
            ChoiceModel c = Choice(3, 1);
            c.ItemRole = PartRole.Option;
            var parts = new List<PartDescription>();
            c.Describe(parts);
            var map = new Dictionary<PartKey, ChoiceKey>
            {
                [PartKey.Up]  = ChoiceKey.Up, [PartKey.Down] = ChoiceKey.Down, [PartKey.Home] = ChoiceKey.Home,
                [PartKey.End] = ChoiceKey.End, [PartKey.Enter] = ChoiceKey.Enter, [PartKey.Space] = ChoiceKey.Space
            };
            foreach (PartKey k in parts.SelectMany(p => p.Keys).Distinct())
                Assert.True(map.ContainsKey(k) && c.Key(map[k]), "key " + k);
        }

        [Fact]
        public void StepperKeysItDescribesAreKeysItHandles()
        {
            StepperModel s = Stepper("Altitude");
            var parts = new List<PartDescription>();
            s.Describe(parts);
            var map = new Dictionary<PartKey, StepperKey>
            {
                [PartKey.Up] = StepperKey.Up, [PartKey.Down] = StepperKey.Down, [PartKey.PageUp] = StepperKey.PageUp, [PartKey.PageDown] = StepperKey.PageDown
            };
            foreach (PartKey k in parts.SelectMany(p => p.Keys).Distinct())
                Assert.True(map.ContainsKey(k) && s.Key(map[k], false), "key " + k);
        }

        [Fact]
        public void TheDescriptionFollowsTheModel()
        {
            int v = 0;
            var c = new ChoiceModel(3, () => v, i => v = i) { Label = "Mode", ItemLabels = new[] { "a", "b", "c" } };
            c.Select(2, ChangeSource.Pointer);
            var parts = new List<PartDescription>();
            c.Describe(parts);
            Assert.Equal(4, parts.Count);
            Assert.True(parts[3].Has(PartFlags.On));
            Assert.True(parts[3].Has(PartFlags.Focus));
            Assert.False(parts[1].Has(PartFlags.On));
            Assert.Empty(Patterns.Check(c));
        }

        [Fact]
        public void AnInvalidStepperFieldReportsIt()
        {
            var parts = new List<PartDescription>();
            Stepper("Altitude", false).Describe(parts);
            Assert.True(parts.Single(p => p.Name == "field").Has(PartFlags.Invalid));
        }

        private static CommitTarget<bool> Check(CommitPath p, string label)
        {
            bool on = false;
            CommitTarget<bool> t = p.Target("CheckBox", () => on, x => on = x);
            t.Model = new ToggleModel(ToggleKind.CheckBox, () => on, t.Set) { Label = label };
            return t;
        }

        [Fact]
        public void TwoControlsOfOneKindWithOneLabelInAWindowFailTheUniqueLabelCheck()
        {
            var p = new CommitPath("PSG Stages", () => 0);
            // a twin stage's two rows named by their KSP stage alone
            List<string> failures = Patterns.CheckLabelsUnique(new CommitTarget[] { Check(p, "Guided (stage 3)"), Check(p, "Guided (stage 3)") });
            Assert.Single(failures);
            Assert.Contains("Guided (stage 3)", failures[0]);
        }

        [Fact]
        public void ATwinStagesRowsNamedByTheirPartPassTheUniqueLabelCheck()
        {
            var p = new CommitPath("PSG Stages", () => 0);
            Assert.Empty(Patterns.CheckLabelsUnique(new CommitTarget[] { Check(p, "Guided (stage 3, boosters)"), Check(p, "Guided (stage 3, core)") }));
        }

        [Fact]
        public void OneLabelOnTwoKindsOfControlPassesTheUniqueLabelCheck()
        {
            // a switch and its field on one row (Smart A.S.S.'s smooth control): the action tells them apart
            var p = new CommitPath("Window", () => 0);
            bool on = false;
            CommitTarget<bool> sw = p.Target("Switch", () => on, x => on = x);
            sw.Model = new ToggleModel(ToggleKind.Switch, () => on, sw.Set) { Label = "Smooth Control" };
            string text = "1";
            CommitTarget<string> field = p.Target("Field", () => text, x => text = x);
            field.Model = new TextFieldModel(() => text, field.Set, TextCommit.AsTyped) { Label = "Smooth Control" };
            Assert.Empty(Patterns.CheckLabelsUnique(new CommitTarget[] { sw, field }));
        }

        private sealed class Custom : IDescribeParts
        {
            private readonly Action<PartDescription> edit;
            private readonly bool                    extra;

            public Custom(Action<PartDescription> edit, bool extra = false)
            {
                this.edit  = edit;
                this.extra = extra;
            }

            public string Label { get; set; } = "Custom";

            public void Describe(List<PartDescription> into)
            {
                var a = new PartDescription { Name = "a", Role = PartRole.Radio, Label = "a", Focusable = true, Flags = PartFlags.On | PartFlags.Focus, Keys = new[] { PartKey.Enter } };
                edit(a);
                into.Add(a);
                if (extra)
                    into.Add(new PartDescription { Name = "a", Role = PartRole.Radio, Label = "b", Focusable = true, Flags = PartFlags.Focus, Keys = new[] { PartKey.Enter } });
            }
        }
    }
}
