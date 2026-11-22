/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>A control as an agent sees it (<see cref="ControlInfo"/>), and a target's description (<see cref="CommitTarget.Description"/>).</summary>
    public class ControlInfoTests
    {
        private static CommitPath Path() => new CommitPath("Window", () => 0);

        [Fact]
        public void ADescriptionSetByTheViewWinsOverThePathsDescriber()
        {
            CommitPath p = Path();
            p.Describer = t => "from the tooltip of " + t.Label;
            bool v = false;
            CommitTarget<bool> a = p.Target("Switch", () => v, x => v = x, "#key");
            CommitTarget<bool> b = p.Target("Switch", () => v, x => v = x, "#other");
            b.Description = "set by the view";
            Assert.Equal("from the tooltip of #key", a.Description);
            Assert.Equal("set by the view", b.Description);
        }

        [Fact]
        public void TheDescriberIsAskedEachTimeSoADescriptionFollowsTheLanguage()
        {
            CommitPath p = Path();
            string language = "en";
            int asked = 0;
            p.Describer = t =>
            {
                asked++;
                return language == "en" ? "Turns it on" : "打开它";
            };
            int v = 0;
            CommitTarget<int> t1 = p.Target("Field", () => v, x => v = x);
            Assert.Equal(0, asked);
            Assert.Equal("Turns it on", t1.Description);
            language = "zh";
            Assert.Equal("打开它", t1.Description);
            Assert.Equal(2, asked);
        }

        [Fact]
        public void ATargetWithNoDescriberAndNoDescriptionHasNone()
        {
            int v = 0;
            Assert.Null(Path().Target("Field", () => v, x => v = x).Description);
        }

        [Fact]
        public void ASwitchIsDescribedByItsModelWithFlipAndSet()
        {
            CommitPath p = Path();
            bool on = true;
            CommitTarget<bool> t = p.Target("Switch", () => on, x => on = x, "#MechJeb_Autostage");
            t.Model = new ToggleModel(ToggleKind.Switch, () => on, t.Set) { Label = "Autostage" };
            ControlInfo c = ControlInfo.Of(t);
            Assert.Equal("switch", c.Kind);
            Assert.Equal("Autostage", c.Label);
            Assert.Equal("true", c.Value);
            Assert.Equal(new[] { "flip", "set" }, c.Actions);
            Assert.Equal("#MechJeb_Autostage", c.Id);
        }

        [Fact]
        public void KindOfGivesTheKindOfWithoutReadingTheValue()
        {
            CommitPath p = Path();
            bool on = false;
            int reads = 0;
            CommitTarget<bool> sw = p.Target("Switch", () => { reads++; return on; }, x => on = x);
            sw.Model = new ToggleModel(ToggleKind.Switch, () => on, sw.Set) { Label = "Autostage" };
            CommitTarget<int> chip = p.Target("Chip", () => { reads++; return 0; }, x => { });
            int v = 0;
            CommitTarget<int> choice = p.Target("Segmented", () => v, x => v = x);
            choice.Model = new ChoiceModel(2, () => v, i => v = i) { Label = "Mode" };
            Assert.Equal("switch", ControlInfo.KindOf(sw));
            Assert.Equal("chip", ControlInfo.KindOf(chip));
            Assert.Equal("choice", ControlInfo.KindOf(choice));
            Assert.Equal(0, reads);
            Assert.Equal(ControlInfo.Of(sw).Kind, ControlInfo.KindOf(sw));
            Assert.Equal(ControlInfo.Of(chip).Kind, ControlInfo.KindOf(chip));
            Assert.Equal(ControlInfo.Of(choice).Kind, ControlInfo.KindOf(choice));
        }

        [Fact]
        public void ACheckBoxIsACheckBoxNotASwitch()
        {
            CommitPath p = Path();
            bool on = false;
            CommitTarget<bool> t = p.Target("CheckBox", () => on, x => on = x);
            t.Model = new ToggleModel(ToggleKind.CheckBox, () => on, t.Set) { Label = "Guided (stage 1)" };
            Assert.Equal("checkbox", ControlInfo.Of(t).Kind);
        }

        [Fact]
        public void AChoiceShowsItsEntriesAndTheEntryOnAsItsValue()
        {
            CommitPath p = Path();
            int v = 1;
            CommitTarget<int> t = p.Target("Segmented", () => v, x => v = x);
            t.Model = new ChoiceModel(3, () => v, t.Set) { Label = "Mode", ItemLabels = new[] { "OFF", "KEEP OBT", "KEEP SURF" } };
            ControlInfo c = ControlInfo.Of(t);
            Assert.Equal("choice", c.Kind);
            Assert.Equal(new[] { "OFF", "KEEP OBT", "KEEP SURF" }, c.Items);
            Assert.Equal("KEEP OBT", c.Value);
            Assert.Equal(new[] { "select" }, c.Actions);
        }

        [Fact]
        public void ChoicesDrawnAsTabsOrAListSayWhich()
        {
            CommitPath p = Path();
            int v = 0;
            CommitTarget<int> t = p.Target("Segmented", () => v, x => v = x);
            t.Model = new ChoiceModel(2, () => v, t.Set) { ItemRole = PartRole.Tab };
            Assert.Equal("tabs", ControlInfo.Of(t).Kind);
            t.Model = new ChoiceModel(2, () => v, t.Set) { ItemRole = PartRole.Option };
            Assert.Equal("list", ControlInfo.Of(t).Kind);
        }

        [Fact]
        public void AStepperNudgesAndIsSet()
        {
            CommitPath p = Path();
            string text = "10";
            CommitTarget<string> t = p.Target("Stepper", () => text, x => text = x);
            t.Model = new StepperModel(() => text, t.Set, (d, s) => { }, () => 1) { Label = "Lead time" };
            ControlInfo c = ControlInfo.Of(t);
            Assert.Equal("stepper", c.Kind);
            Assert.Equal("10", c.Value);
            Assert.Equal(new[] { "nudge", "set" }, c.Actions);
        }

        [Fact]
        public void AFieldAndAnInlineEditAreSetWithAText()
        {
            CommitPath p = Path();
            string text = "250";
            CommitTarget<string> f = p.Target("Field", () => text, x => text = x);
            f.Model = new TextFieldModel(() => text, f.Set, TextCommit.AsTyped);
            CommitTarget<string> i = p.Target("InlineEdit", () => text, x => text = x);
            i.Model = new InlineEditModel(() => text, i.Set);
            Assert.Equal("text", ControlInfo.Of(f).Kind);
            Assert.Equal("inline", ControlInfo.Of(i).Kind);
            Assert.Equal("250", ControlInfo.Of(f).Value);
            Assert.Equal(new[] { "set" }, ControlInfo.Of(i).Actions);
        }

        [Fact]
        public void ADropDownShowsItsItemsAndTheChosenOne()
        {
            CommitPath p = Path();
            int v = 2;
            CommitTarget<int> t = p.Target("ComboBox", () => v, x => v = x);
            var m = new ComboModel(() => v, t.Set) { Label = "Landing site" };
            m.SetItems(new[] { "KSC", "Island", "Desert" });
            t.Model = m;
            ControlInfo c = ControlInfo.Of(t);
            Assert.Equal("combo", c.Kind);
            Assert.Equal("Desert", c.Value);
            Assert.Equal(3, c.Items.Length);
        }

        [Fact]
        public void AClosedDropDownShowsTheEntriesItsButtonHasNotTheLastOpenedOnes()
        {
            CommitPath p = Path();
            int v = 1;
            string[] entries = { "KSC", "Island" };
            CommitTarget<int> t = p.Target("ComboBox", () => v, x => v = x);
            t.Model = new ComboModel(() => v, t.Set, 1.0, () => entries) { Label = "Landing site" };
            Assert.Equal(new[] { "KSC", "Island" }, ControlInfo.Of(t).Items);
            Assert.Equal("Island", ControlInfo.Of(t).Value);
            entries = new[] { "KSC", "Island", "Desert" };
            Assert.Equal(3, ControlInfo.Of(t).Items.Length);
        }

        [Fact]
        public void ASliderShowsItsValueInvariantly()
        {
            CommitPath p = Path();
            float v = 0.25f;
            CommitTarget<float> t = p.Target("Slider", () => v, x => v = x);
            t.Model = new SliderModel(() => v, t.Set, 0, 1) { Label = "Overdrive" };
            ControlInfo c = ControlInfo.Of(t);
            Assert.Equal("slider", c.Kind);
            Assert.Equal("0.25", c.Value);
            Assert.True(c.Enabled);
        }

        [Fact]
        public void AWidgetWithNoModelIsDescribedFromItsTarget()
        {
            CommitPath p = Path();
            int v = 3;
            CommitTarget<int> t = p.Target("ArrowSelector", () => v, x => v = x, "Node");
            ControlInfo c = ControlInfo.Of(t);
            Assert.Equal("cycle", c.Kind);
            Assert.Equal("Node", c.Label);
            Assert.Equal("3", c.Value);
            Assert.Equal(new[] { "select", "next", "previous" }, c.Actions);
        }

        [Fact]
        public void AWidgetWithNoModelShowsItsKeyThroughThePath()
        {
            CommitPath p = Path();
            p.ShowLabel = key => key == "#MechJeb_Flightrecord_checkbox3" ? "Stages" : key;
            bool on = false;
            CommitTarget<bool> t = p.Target("Chip", () => on, x => on = x, "#MechJeb_Flightrecord_checkbox3");
            Assert.Equal("Stages", ControlInfo.Of(t).Label);
            Assert.Equal("#MechJeb_Flightrecord_checkbox3", ControlInfo.Of(t).Id);
        }

        [Fact]
        public void AModelWithoutALabelTakesTheTargetsLabel()
        {
            CommitPath p = Path();
            bool on = false;
            CommitTarget<bool> t = p.Target("Chip", () => on, x => on = x, "Stages");
            Assert.Equal("Stages", ControlInfo.Of(t).Label);
            Assert.Equal("chip", ControlInfo.Of(t).Kind);
            Assert.Equal("false", ControlInfo.Of(t).Value);
        }
    }
}
