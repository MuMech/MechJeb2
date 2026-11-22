/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>An agent's actions on a control, through its model (<see cref="ControlActions"/>): one commit each, as the agent.</summary>
    public class ControlActionsTests
    {
        private sealed class Host
        {
            public readonly CommitPath   Path    = new CommitPath("Window", () => 0);
            public readonly List<Commit> Settled = new List<Commit>();

            public Host() => Path.Settled += Settled.Add;
        }

        private static (Host h, CommitTarget<bool> t, ToggleModel m) Toggle(bool on = false)
        {
            var  h = new Host();
            bool v = on;
            CommitTarget<bool> t = h.Path.Target("Switch", () => v, x => v = x, "Autostage");
            var m = new ToggleModel(ToggleKind.Switch, () => v, t.Set) { Label = "Autostage" };
            t.Model = m;
            return (h, t, m);
        }

        private static void OneAgentCommit(Host h)
        {
            Assert.Single(h.Settled);
            Assert.Equal(ChangeSource.Agent, h.Settled[0].Source);
            Assert.Equal(ChangeActor.Agent, h.Settled[0].Actor);
        }

        [Fact]
        public void AFlipIsOneCommitAsTheAgent()
        {
            (Host h, CommitTarget<bool> t, ToggleModel m) = Toggle();
            Assert.Null(ControlActions.Run(t, "flip", null));
            Assert.True(m.On);
            OneAgentCommit(h);
        }

        [Fact]
        public void SetToTheValueItHasChangesNothing()
        {
            (Host h, CommitTarget<bool> t, _) = Toggle(true);
            Assert.Null(ControlActions.Run(t, "set", new[] { "true" }));
            Assert.Empty(h.Settled);
            Assert.Null(ControlActions.Run(t, "set", new[] { "false" }));
            OneAgentCommit(h);
        }

        [Fact]
        public void ASetThatIsNoBooleanIsAnError()
        {
            (_, CommitTarget<bool> t, _) = Toggle();
            Assert.Equal("set takes true or false", ControlActions.Run(t, "set", new[] { "yes" }));
        }

        [Fact]
        public void ADisabledControlRefuses()
        {
            var  h = new Host();
            bool v = false;
            CommitTarget<bool> t = h.Path.Target("Switch", () => v, x => v = x);
            t.Model = new ToggleModel(ToggleKind.Switch, () => v, t.Set, () => false);
            Assert.Equal("the control is disabled", ControlActions.Run(t, "flip", null));
            Assert.Empty(h.Settled);
        }

        [Fact]
        public void ASelectByTextOrIndexPicksThroughTheChoice()
        {
            var h = new Host();
            int v = 0;
            CommitTarget<int> t = h.Path.Target("Segmented", () => v, x => v = x);
            t.Model = new ChoiceModel(3, () => v, t.Set) { Label = "Mode", ItemLabels = new[] { "OFF", "KEEP OBT", "KEEP SURF" } };
            Assert.Null(ControlActions.Run(t, "select", new[] { "keep surf" }));
            Assert.Equal(2, v);
            OneAgentCommit(h);
            Assert.Null(ControlActions.Run(t, "select", new[] { "#1" }));
            Assert.Equal(1, v);
        }

        [Fact]
        public void PickingTheSelectedEntryAgainStillCallsTheSetter()
        {
            var h = new Host();
            int v = 0, calls = 0;
            CommitTarget<int> t = h.Path.Target("Segmented", () => v, x =>
            {
                v = x;
                calls++;
            });
            t.Model = new ChoiceModel(2, () => v, t.Set) { ItemLabels = new[] { "OFF", "ON" } };
            Assert.Null(ControlActions.Run(t, "select", new[] { "OFF" }));
            Assert.Equal(1, calls);
        }

        [Fact]
        public void ATextBeginningSeveralItemsIsAnErrorListingThem()
        {
            var h = new Host();
            int v = 0;
            CommitTarget<int> t = h.Path.Target("Segmented", () => v, x => v = x);
            t.Model = new ChoiceModel(3, () => v, t.Set) { ItemLabels = new[] { "OFF", "KEEP OBT", "KEEP SURF" } };
            string e = ControlActions.Run(t, "select", new[] { "KEEP" });
            Assert.Contains("begins several items: KEEP OBT, KEEP SURF", e);
            Assert.Empty(h.Settled);
        }

        [Fact]
        public void ADropDownIsSetThroughItsTarget()
        {
            var h = new Host();
            int v = 0;
            CommitTarget<int> t = h.Path.Target("ComboBox", () => v, x => v = x);
            t.Model = new ComboModel(() => v, t.Set, 1.0, () => new[] { "KSC", "Island", "Desert" });
            Assert.Null(ControlActions.Run(t, "select", new[] { "Isl" }));
            Assert.Equal(1, v);
            OneAgentCommit(h);
        }

        [Fact]
        public void ANudgeIsACommitPerStepAndBigIsTenSteps()
        {
            var    h = new Host();
            double value = 10;
            CommitTarget<string> t = h.Path.Target("Stepper", () => value.ToString(), s => value = double.Parse(s));
            t.Model = new StepperModel(() => value.ToString(), t.Set, (d, s) => t.Apply(d, x => value += x, s), () => 1);
            Assert.Null(ControlActions.Run(t, "nudge", new[] { "+2" }));
            Assert.Equal(12, value);
            Assert.Equal(2, h.Settled.Count);
            Assert.Null(ControlActions.Run(t, "decrement", new[] { "big" }));
            Assert.Equal(2, value);
            Assert.All(h.Settled, c => Assert.Equal(ChangeSource.Agent, c.Source));
        }

        [Fact]
        public void AFieldsSetIsOneEditAndOneCommit()
        {
            var    h    = new Host();
            string text = "100";
            CommitTarget<string> t = h.Path.Target("Field", () => text, s => text = s);
            t.Model = new TextFieldModel(() => text, t.Set, TextCommit.AsTyped, null, null, null, null, () => t.BeginGroup(), t.EndGroup);
            Assert.Null(ControlActions.Run(t, "set", new[] { "250" }));
            Assert.Equal("250", text);
            OneAgentCommit(h);
            Assert.Equal("100", h.Settled[0].Old);
            Assert.False(((TextFieldModel)t.Model).Editing);
        }

        [Fact]
        public void AnInlineEditThatRefusesTheTextIsAnErrorAndTheEditEnds()
        {
            var    h    = new Host();
            string text = "5";
            CommitTarget<string> t = h.Path.Target("InlineEdit", () => text, s => text = s);
            var m = new InlineEditModel(() => text, t.Set, s => double.TryParse(s, out _));
            t.Model = m;
            Assert.Contains("does not accept", ControlActions.Run(t, "set", new[] { "abc" }));
            Assert.Equal("5", text);
            Assert.False(m.Editing);
            Assert.Null(ControlActions.Run(t, "set", new[] { "7" }));
            Assert.Equal("7", text);
            OneAgentCommit(h);
        }

        [Fact]
        public void ASliderIsSetConstrainedToItsRange()
        {
            var   h = new Host();
            float v = 0.5f;
            CommitTarget<float> t = h.Path.Target("Slider", () => v, x => v = x);
            t.Model = new SliderModel(() => v, t.Set, 0, 1);
            Assert.Null(ControlActions.Run(t, "set", new[] { "3" }));
            Assert.Equal(1f, v);
            OneAgentCommit(h);
            Assert.Equal("set takes a number", ControlActions.Run(t, "set", new[] { "x" }));
        }

        [Fact]
        public void ATargetWithoutAModelIsWrittenThroughThePath()
        {
            var  h = new Host();
            bool v = false;
            CommitTarget<bool> chip = h.Path.Target("Chip", () => v, x => v = x, "Stages");
            Assert.Null(ControlActions.Run(chip, "flip", null));
            Assert.True(v);
            OneAgentCommit(h);
            int i = 0;
            CommitTarget<int> arrows = h.Path.Target("ArrowSelector", () => i, x => i = x, "Node");
            Assert.Null(ControlActions.Run(arrows, "select", new[] { "#2" }));
            Assert.Equal(2, i);
            Assert.Contains("#n", ControlActions.Run(arrows, "select", new[] { "second" }));
        }

        [Fact]
        public void ASelectPastAnArrowSelectorsEntriesIsRefusedAndWritesNothing()
        {
            var h = new Host();
            int i = 0;
            CommitTarget<int> arrows = h.Path.Target("ArrowSelector", () => i, x => i = x, "Node");
            arrows.Model = new IndexChoice(() => 3);
            Assert.Null(ControlActions.Run(arrows, "select", new[] { "#2" }));
            Assert.Equal(2, i);
            Assert.Contains("3 items", ControlActions.Run(arrows, "select", new[] { "#99999" }));
            Assert.Equal(2, i);
            Assert.Single(h.Settled);
        }

        [Fact]
        public void ASelectOnAKindWithoutSelectIsRefused()
        {
            var h = new Host();
            bool v = false;
            CommitTarget<bool> chip = h.Path.Target("Chip", () => v, x => v = x, "Stages");
            Assert.Contains("no action \"select\"", ControlActions.Run(chip, "select", new[] { "#1" }));
            Assert.False(v);
            Assert.Empty(h.Settled);
        }

        [Fact]
        public void AListsRowsAreMovedAndAddedThroughItsEdits()
        {
            var h     = new Host();
            var items = new List<string> { "a", "b", "c" };
            var registry = new[] { "x", "y" };
            CommitTarget<int> t = h.Path.Target("ItemList", () => items.Count, _ => { });
            var reorder = new ReorderModel(() => items.Count, (from, to) =>
            {
                string s = items[from];
                items.RemoveAt(from);
                items.Insert(to, s);
            });
            var transfer = new TransferModel(() => registry.Length, () => items.Count, (src, at) => items.Insert(at, registry[src]));
            t.Model = new ListEdits { Reorder = reorder, Transfer = transfer };
            Assert.Null(ControlActions.Run(t, "move", new[] { "0", "2" }));
            Assert.Equal(new[] { "b", "c", "a" }, items);
            Assert.Null(ControlActions.Run(t, "add", new[] { "1", "0" }));
            Assert.Equal(new[] { "y", "b", "c", "a" }, items);
            Assert.Equal(ChangeSource.Agent, reorder.Source);
            Assert.Equal(ChangeSource.Agent, transfer.Source);
            Assert.Equal(new[] { "select", "move", "add" }, ControlInfo.Of(t).Actions);
        }

        [Fact]
        public void AListsRowsAreListedSelectedAndAddedByTheirLabels()
        {
            var h = new Host();
            var items = new List<string> { "Apoapsis", "Periapsis" };
            var registry = new[] { "Orbital speed", "Inclination" };
            int selected = -1;
            CommitTarget<int> t = h.Path.Target("ItemList", () => items.Count, _ => { });
            t.Model = new ListEdits
            {
                Count = () => items.Count, Label = i => items[i], Selected = () => selected, Select = i => selected = i,
                SourceCount = () => registry.Length, SourceLabel = i => registry[i],
                Transfer = new TransferModel(() => registry.Length, () => items.Count, (src, at) => items.Insert(at, registry[src]))
            };
            ControlInfo c = ControlInfo.Of(t);
            Assert.Equal(new[] { "Apoapsis", "Periapsis" }, c.Items);
            Assert.Equal(new[] { "Orbital speed", "Inclination" }, c.Sources);
            Assert.Null(ControlActions.Run(t, "select", new[] { "peri" }));
            Assert.Equal(1, selected);
            Assert.Equal("Periapsis", ControlInfo.Of(t).Value);
            Assert.Empty(h.Settled);
            Assert.Null(ControlActions.Run(t, "add", new[] { "Incl" }));
            Assert.Equal("Inclination", items[2]);
        }

        [Fact]
        public void ANudgeOfMoreThanItsBoundIsAnErrorAndChangesNothing()
        {
            var    h     = new Host();
            double value = 0;
            CommitTarget<string> t = h.Path.Target("Stepper", () => value.ToString(), s => value = double.Parse(s));
            t.Model = new StepperModel(() => value.ToString(), t.Set, (d, s) => t.Apply(d, x => value += x, s), () => 1);
            Assert.Equal("nudge takes at most 1000 steps", ControlActions.Run(t, "nudge", new[] { "2000000000" }));
            Assert.Equal(0, value);
            Assert.Empty(h.Settled);
            Assert.Null(ControlActions.Run(t, "nudge", new[] { "-1000" }));
            Assert.Equal(-1000, value);
        }

        [Fact]
        public void AnUnknownActionListsTheControlsActions()
        {
            (_, CommitTarget<bool> t, _) = Toggle();
            Assert.Equal("no action \"press\" on switch \"Autostage\"; it has flip, set", ControlActions.Run(t, "press", null));
        }

        [Fact]
        public void ARemovedTargetRefuses()
        {
            (Host h, CommitTarget<bool> t, _) = Toggle();
            h.Path.Remove(t);
            Assert.Equal("the control is gone", ControlActions.Run(t, "flip", null));
        }

        [Fact]
        public void TheAgentsCommitIsUndoneByItsSourceAndThePlayersStays()
        {
            var    h     = new Host();
            var    stack = new UndoStack(h.Path) { Recording = true };
            string text  = "1";
            CommitTarget<string> t = h.Path.Target("Field", () => text, s => text = s);
            t.Model = new TextFieldModel(() => text, t.Set, TextCommit.AsTyped, null, null, null, null, () => t.BeginGroup(), t.EndGroup);
            t.Set("2", ChangeSource.Keyboard);
            Assert.Null(ControlActions.Run(t, "set", new[] { "3" }));
            Assert.Equal(UndoResult.Done, stack.Undo(ChangeSource.Agent));
            Assert.Equal("2", text);
            Assert.Equal(UndoResult.Nothing, stack.Undo(ChangeSource.Agent));
        }
    }
}
