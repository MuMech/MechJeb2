/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's commit path (every widget edit, with its source), without KSP or Unity.</summary>
    public class CommitPathTests
    {
        private sealed class Host
        {
            public          int          Tick;
            public          int          Edits;
            public readonly List<Commit> Seen = new List<Commit>();
            public readonly CommitPath   Path;

            public Host(int capacity = 32)
            {
                Path        =  new CommitPath("Window", () => Tick, capacity) { Edited = () => Edits++ };
                Path.Committed += Seen.Add;
            }
        }

        private static (Host h, CommitTarget<int> t, Func<int> value) IntTarget(int start = 0, bool persist = true)
        {
            var h = new Host();
            int v = start;
            CommitTarget<int> t = h.Path.Target("Toggle", () => v, x => v = x, null, null, persist);
            return (h, t, () => v);
        }

        [Fact]
        public void ASetCallsTheSetterThenEditedThenReportsOneCommit()
        {
            var order = new List<string>();
            var path = new CommitPath("W") { Edited = () => order.Add("edited") };
            path.Committed += c => order.Add("commit");
            int v = 0;
            CommitTarget<int> t = path.Target("Segmented", () => v, x =>
            {
                order.Add("set");
                v = x;
            });

            t.Set(2, ChangeSource.Pointer);

            Assert.Equal(new[] { "set", "edited", "commit" }, order);
            Assert.Equal(2, v);
            Assert.Equal(1, path.Total);
        }

        [Fact]
        public void ACommitHoldsTheOldAndNewValuesWhatWasAskedAndTheSource()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(1);
            t.Set(3, ChangeSource.Keyboard);
            Commit c = Assert.Single(h.Seen);
            Assert.Same(t, c.Target);
            Assert.Equal(1, c.Old);
            Assert.Equal(3, c.New);
            Assert.Equal(3, c.Asked);
            Assert.Equal(ChangeSource.Keyboard, c.Source);
            Assert.Equal(1, c.Count);
            Assert.True(c.Changed);
        }

        [Fact]
        public void TheNewValueIsReadBackFromTheModel()
        {
            var path = new CommitPath("W");
            string text = "1";
            // the model strips junk, as MechJeb's editables do
            CommitTarget<string> t = path.Target("Field", () => text, s => text = s.Replace("x", ""));
            t.Set("2x", ChangeSource.Keyboard);
            Assert.Equal("1", path.Last.Old);
            Assert.Equal("2", path.Last.New);
            Assert.Equal("2x", path.Last.Asked);
        }

        [Fact]
        public void ASetThatKeepsTheValueIsStillACommit()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(2);
            t.Set(2, ChangeSource.Pointer);
            Commit c = Assert.Single(h.Seen);
            Assert.False(c.Changed);
            Assert.Equal(1, h.Edits);
        }

        [Fact]
        public void ATargetThatDoesNotPersistDoesNotCallEdited()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(0, false);
            t.Set(1, ChangeSource.Pointer);
            Assert.Equal(0, h.Edits);
            Assert.Single(h.Seen);
        }

        [Fact]
        public void ASetterThatThrowsRecordsNothingAndCallsNoEdited()
        {
            var h = new Host();
            CommitTarget<int> t = h.Path.Target<int>("Field", () => 0, x => throw new InvalidOperationException());
            Assert.Throws<InvalidOperationException>(() => t.Set(1, ChangeSource.Pointer));
            Assert.Empty(h.Seen);
            Assert.Equal(0, h.Edits);
            Assert.Null(h.Path.Last);
        }

        [Fact]
        public void ApplyRecordsTheValueBeforeAndAfterAndTheAskedDelta()
        {
            var path = new CommitPath("W");
            double v = 10;
            CommitTarget<string> t = path.Target("Stepper", () => v.ToString(System.Globalization.CultureInfo.InvariantCulture),
                s => v = double.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
            t.Apply(2.5, d => v += d, ChangeSource.Pointer);
            Assert.Equal("10", path.Last.Old);
            Assert.Equal("12.5", path.Last.New);
            Assert.Equal(2.5, path.Last.Asked);
        }

        [Fact]
        public void ChangesInsideAGroupAreOneCommit()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(0);
            t.BeginGroup();
            t.Set(1, ChangeSource.Pointer);
            t.Set(2, ChangeSource.Pointer);
            t.Set(3, ChangeSource.Pointer);
            t.EndGroup();

            Assert.Equal(1, h.Path.Total);
            Commit c = h.Path.Last;
            Assert.Equal(0, c.Old);
            Assert.Equal(3, c.New);
            Assert.Equal(3, c.Count);
            Assert.False(c.Open);
            // each change still reaches the observers, as the same commit
            Assert.Equal(3, h.Seen.Count);
            Assert.All(h.Seen, s => Assert.Same(c, s));
            Assert.Equal(3, h.Edits);
        }

        [Fact]
        public void AGroupStaysOpenUntilItsLastEnd()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(0);
            t.BeginGroup();
            t.BeginGroup();
            t.Set(1, ChangeSource.Pointer);
            t.EndGroup();
            Assert.True(h.Path.Last.Open);
            t.Set(2, ChangeSource.Pointer);
            t.EndGroup();
            Assert.False(h.Path.Last.Open);
            Assert.Equal(1, h.Path.Total);
        }

        [Fact]
        public void AChangeAfterTheGroupIsANewCommit()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(0);
            t.BeginGroup();
            t.Set(1, ChangeSource.Pointer);
            t.EndGroup();
            t.Set(2, ChangeSource.Pointer);
            Assert.Equal(2, h.Path.Total);
            Assert.Equal(1, h.Path.Last.Old);
        }

        [Fact]
        public void ExtraEndsDoNothing()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(0);
            t.EndGroup();
            t.BeginGroup();
            t.EndGroup();
            t.EndGroup();
            Assert.False(t.Grouping);
            t.Set(1, ChangeSource.Pointer);
            Assert.False(h.Path.Last.Open);
        }

        [Fact]
        public void AnotherSourceInsideAGroupStartsANewCommit()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(0);
            t.BeginGroup();
            t.Set(1, ChangeSource.Keyboard);
            Commit typed = h.Path.Last;
            t.Set(2, ChangeSource.Agent);
            Assert.Equal(2, h.Path.Total);
            Assert.False(typed.Open);
            Assert.True(h.Path.Last.Open);
            Assert.Equal(ChangeSource.Agent, h.Path.Last.Source);
            Assert.Equal(1, h.Path.Last.Old);
        }

        [Fact]
        public void GroupsArePerTarget()
        {
            var h = new Host();
            int a = 0, b = 0;
            CommitTarget<int> ta = h.Path.Target("Slider", () => a, x => a = x);
            CommitTarget<int> tb = h.Path.Target("Slider", () => b, x => b = x);
            ta.BeginGroup();
            ta.Set(1, ChangeSource.Pointer);
            tb.Set(5, ChangeSource.Agent);
            ta.Set(2, ChangeSource.Pointer);
            ta.EndGroup();
            Assert.Equal(2, h.Path.Total);
            Assert.Equal(2, ta.Last.Count);
            Assert.Equal(1, tb.Last.Count);
        }

        [Fact]
        public void AGroupCanTakeInTheChangeItsPressMadeInTheSameTick()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(0);
            h.Tick = 7;
            t.Set(4, ChangeSource.Pointer); // the slider jumps on the press, before the group's handler runs
            Commit press = h.Path.Last;
            t.BeginGroup(true);
            t.Set(5, ChangeSource.Pointer);
            t.EndGroup();
            Assert.Equal(0, h.Path.Last.Old);
            Assert.Equal(5, h.Path.Last.New);
            Assert.Same(press, h.Path.Last.Replaces);
            Assert.Equal(2, h.Path.Last.Count);
            // the press's commit was final when it was reported: it stays as it was
            Assert.Equal(4, press.New);
            Assert.False(press.Open);
        }

        [Fact]
        public void AnUngroupedCommitSettlesAtOnce()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget();
            var settled = new List<Commit>();
            h.Path.Settled += settled.Add;
            t.Set(1, ChangeSource.Pointer);
            Assert.Single(settled);
            Assert.Same(h.Seen[0], settled[0]);
            Assert.False(settled[0].Open);
        }

        [Fact]
        public void AGroupSettlesOnceWithTheValueItEndedWith()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget();
            var settled = new List<object>();
            h.Path.Settled += c => settled.Add(c.New);
            t.BeginGroup();
            t.Set(1, ChangeSource.Keyboard);
            t.Set(12, ChangeSource.Keyboard);
            Assert.Empty(settled);
            Assert.Equal(2, h.Seen.Count);
            t.EndGroup();
            Assert.Equal(new object[] { 12 }, settled);
        }

        [Fact]
        public void AnotherSourceInsideAGroupSettlesTheGroupsCommitSoFar()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget();
            var settled = new List<Commit>();
            h.Path.Settled += settled.Add;
            t.BeginGroup();
            t.Set(1, ChangeSource.Keyboard);
            t.Set(2, ChangeSource.Agent);
            Assert.Single(settled);
            Assert.Equal(ChangeSource.Keyboard, settled[0].Source);
            t.EndGroup();
            Assert.Equal(2, settled.Count);
            Assert.Equal(ChangeSource.Agent, settled[1].Source);
        }

        [Fact]
        public void ASettledCommitNeverChangesAgain()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget();
            t.BeginGroup();
            t.Set(1, ChangeSource.Pointer);
            t.EndGroup();
            Commit first = h.Path.Last;
            t.BeginGroup(true); // same tick: a group may carry it on, but not reopen it
            t.Set(2, ChangeSource.Pointer);
            t.EndGroup();
            Assert.Equal(1, first.New);
            Assert.Equal(1, first.Count);
            Assert.False(first.Open);
            Assert.NotSame(first, h.Path.Last);
        }

        [Fact]
        public void AGroupDoesNotTakeInAnOlderCommit()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget(0);
            h.Tick = 7;
            t.Set(4, ChangeSource.Pointer);
            h.Tick = 8;
            t.BeginGroup(true);
            t.Set(5, ChangeSource.Pointer);
            t.EndGroup();
            Assert.Equal(2, h.Path.Total);
            Assert.Equal(4, h.Path.Last.Old);
        }

        [Fact]
        public void AGroupDoesNotTakeInACommitThatAnotherFollowed()
        {
            var h = new Host();
            int a = 0, b = 0;
            CommitTarget<int> ta = h.Path.Target("Slider", () => a, x => a = x);
            CommitTarget<int> tb = h.Path.Target("Toggle", () => b, x => b = x);
            ta.Set(1, ChangeSource.Pointer);
            tb.Set(1, ChangeSource.Pointer);
            ta.BeginGroup(true);
            ta.Set(2, ChangeSource.Pointer);
            Assert.Equal(3, h.Path.Total);
        }

        [Fact]
        public void KeysAreKindAndOrderAndFindReturnsTheTarget()
        {
            var path = new CommitPath("W");
            object c1 = new object(), c2 = new object();
            CommitTarget<int> a = path.Target("Toggle", () => 0, x => { }, null, c1);
            path.Target("Field", () => "", x => { });
            CommitTarget<int> b = path.Target("Toggle", () => 0, x => { }, "Label", c2);
            Assert.Equal("Toggle#0", a.Key);
            Assert.Equal("Toggle#1", b.Key);
            Assert.Equal("Field#0", path.Targets[1].Key);
            Assert.Same(b, path.Find(c2));
            Assert.Same(a, path.FindKey("Toggle#0"));
            Assert.Null(path.Find(new object()));
            Assert.Equal("Label", b.Label);
        }

        [Fact]
        public void ARemovedTargetLeavesThePathAndRefusesWrites()
        {
            var path = new CommitPath("W");
            int v = 0;
            object row = new object();
            CommitTarget<int> t = path.Target("Toggle", () => v, x => v = x, null, row);
            CommitTarget? removedSeen = null;
            path.TargetRemoved += x => removedSeen = x;
            Assert.True(path.Remove(t));
            Assert.Empty(path.Targets);
            Assert.True(t.Removed);
            Assert.Same(t, removedSeen);
            Assert.Null(path.Find(row));
            Assert.False(t.Write(3, ChangeSource.Agent));
            Assert.Equal(0, v);
            Assert.False(path.Remove(t));
            // keys aren't reused: the next toggle is #1
            Assert.Equal("Toggle#1", path.Target("Toggle", () => 0, x => { }).Key);
        }

        [Fact]
        public void RemovingATargetMidGestureSettlesItsCommit()
        {
            (Host h, CommitTarget<int> t, _) = IntTarget();
            var settled = new List<Commit>();
            h.Path.Settled += settled.Add;
            t.BeginGroup();
            t.BeginGroup();
            t.Set(1, ChangeSource.Pointer);
            h.Path.Remove(t);
            Assert.Single(settled);
            Assert.False(t.Grouping);
        }

        [Fact]
        public void AnIdComesFromTheLabelAndStaysUniqueInThePath()
        {
            var path = new CommitPath("W");
            CommitTarget<bool> sw = path.Target("Switch", () => false, x => { }, "#Autowarp");
            CommitTarget<string> field = path.Target("Field", () => "", x => { }, "#Autowarp");
            CommitTarget<int> plain = path.Target("Segmented", () => 0, x => { });
            Assert.Equal("#Autowarp", sw.Id);
            Assert.Equal("#Autowarp/Field", field.Id);
            Assert.Null(plain.Id);
            Assert.Same(field, path.FindId("#Autowarp/Field"));
            plain.Id = "steps";
            Assert.Same(plain, path.FindId("steps"));
        }

        // two lazy panels with the same label (the planner's operations), built in either order: each panel's control has the same Id
        private static Dictionary<string, string> PanelIds(bool secondFirst)
        {
            var path = new CommitPath("W");
            path.Target("Switch", () => false, x => { }, "#Top");
            var ids = new Dictionary<string, string>();
            void Build(string scope)
            {
                path.NameScope = scope;
                ids[scope] = path.Target("ComboBox", () => 0, x => { }, "#MechJeb_Maneu_STB").Id;
                path.NameScope = "";
            }
            if (secondFirst)
            {
                Build("p1/");
                Build("p0/");
            }
            else
            {
                Build("p0/");
                Build("p1/");
            }
            return ids;
        }

        [Fact]
        public void ALabelRepeatedAcrossLazyPanelsHasOneIdPerPanelWhicheverPanelIsBuiltFirst()
        {
            Dictionary<string, string> inOrder = PanelIds(false), reversed = PanelIds(true);
            Assert.Equal("p0/#MechJeb_Maneu_STB", inOrder["p0/"]);
            Assert.Equal("p1/#MechJeb_Maneu_STB", inOrder["p1/"]);
            Assert.Equal(inOrder["p0/"], reversed["p0/"]);
            Assert.Equal(inOrder["p1/"], reversed["p1/"]);
        }

        [Fact]
        public void TargetsMadeInsideATagScopeCarryTheTag()
        {
            var path = new CommitPath("W");
            CommitTarget<int> before = path.Target("Toggle", () => 0, x => { });
            CommitTarget<int> inside;
            using (path.Tagging("executor"))
                inside = path.Target("Toggle", () => 0, x => { });
            CommitTarget<int> after = path.Target("Toggle", () => 0, x => { });
            Assert.Null(before.Tag);
            Assert.Equal("executor", inside.Tag);
            Assert.Null(after.Tag);
        }

        [Fact]
        public void FindKeyParsesTheKey()
        {
            var path = new CommitPath("W");
            path.Target("Toggle", () => 0, x => { });
            CommitTarget<int> b = path.Target("Toggle", () => 0, x => { });
            Assert.Same(b, path.FindKey("Toggle#1"));
            Assert.Null(path.FindKey("Toggle#2"));
            Assert.Null(path.FindKey("Toggle"));
            Assert.Null(path.FindKey("Toggle#-1"));
            Assert.Null(path.FindKey(null));
        }

        [Fact]
        public void WriteTakesNullForAReferenceTypeButNotForAValueType()
        {
            var path = new CommitPath("W");
            string s = "a";
            CommitTarget<string> text = path.Target("Field", () => s, x => s = x);
            Assert.True(text.Write(null, ChangeSource.Program));
            Assert.Null(s);
            int v = 1;
            CommitTarget<int> number = path.Target("Toggle", () => v, x => v = x);
            Assert.False(number.Write(null, ChangeSource.Program));
            Assert.Equal(1, v);
        }

        [Fact]
        public void WriteSetsThroughThePathAndRefusesAnotherType()
        {
            (Host h, CommitTarget<int> t, Func<int> v) = IntTarget(0);
            Assert.True(t.Write(4, ChangeSource.Program));
            Assert.Equal(4, v());
            Assert.Equal(ChangeSource.Program, h.Path.Last.Source);
            Assert.False(t.Write("4", ChangeSource.Program));
            Assert.Equal(1, h.Path.Total);
        }

        [Fact]
        public void TheLogKeepsTheLastCommitsUpToItsCapacity()
        {
            var h = new Host(3);
            int v = 0;
            CommitTarget<int> t = h.Path.Target("Toggle", () => v, x => v = x);
            for (int i = 1; i <= 5; i++) t.Set(i, ChangeSource.Pointer);
            Assert.Equal(3, h.Path.Log.Count);
            Assert.Equal(3, h.Path.Log[0].New);
            Assert.Equal(5, h.Path.Log[2].New);
            Assert.Equal(5, h.Path.Total);
            h.Path.ClearLog();
            Assert.Empty(h.Path.Log);
            Assert.Null(h.Path.Last);
        }

        [Fact]
        public void AHostScopeSetsTheSourceWidgetsReport()
        {
            Assert.Equal(ChangeSource.Pointer, CommitPath.Resolve(ChangeSource.Pointer));
            using (CommitPath.As(ChangeSource.Agent))
                Assert.Equal(ChangeSource.Agent, CommitPath.Resolve(ChangeSource.Pointer));
            Assert.Equal(ChangeSource.Pointer, CommitPath.Resolve(ChangeSource.Pointer));
            Assert.Equal(0, CommitPath.OpenScopes);
        }

        [Fact]
        public void ACommitAnObserverMakesReachesEveryObserverAfterTheOneBeingReported()
        {
            var path = new CommitPath("W");
            int a = 0, b = 0;
            CommitTarget<int> ta = path.Target("Toggle", () => a, x => a = x);
            CommitTarget<int> tb = path.Target("Toggle", () => b, x => b = x);
            path.Committed += c =>
            {
                if (c.Target == ta) tb.Set(a * 10, ChangeSource.Program);
            };
            var seen = new List<Commit>();
            path.Committed += seen.Add;
            ta.Set(1, ChangeSource.Pointer);
            Assert.Equal(2, seen.Count);
            Assert.Same(ta, seen[0].Target);
            Assert.Same(tb, seen[1].Target);
            Assert.True(seen[0].Seq < seen[1].Seq);
            Assert.False(seen[1].Nested);
        }

        [Fact]
        public void ACommitMadeInsideAnotherTargetsSetterIsNested()
        {
            var path = new CommitPath("W");
            int a = 0, b = 0;
            CommitTarget<int> tb = path.Target("Toggle", () => b, x => b = x);
            CommitTarget<int> ta = path.Target("Toggle", () => a, x =>
            {
                a = x;
                tb.Set(x * 10, ChangeSource.Program);
            });
            var seen = new List<Commit>();
            path.Committed += seen.Add;
            ta.Set(1, ChangeSource.Pointer);
            Assert.Equal(2, seen.Count);
            Assert.Same(tb, seen[0].Target);
            Assert.True(seen[0].Nested);
            Assert.Same(ta, seen[1].Target);
            Assert.False(seen[1].Nested);
            Assert.True(seen[0].Seq < seen[1].Seq);
        }

        [Fact]
        public void ASetterThatThrowsLeavesTheNextCommitNotNested()
        {
            var path = new CommitPath("W");
            int v = 0;
            CommitTarget<int> bad = path.Target("Toggle", () => 0, x => throw new InvalidOperationException());
            CommitTarget<int> good = path.Target("Toggle", () => v, x => v = x);
            Assert.Throws<InvalidOperationException>(() => bad.Set(1, ChangeSource.Pointer));
            good.Set(1, ChangeSource.Pointer);
            Assert.False(path.Last.Nested);
        }

        [Theory]
        [InlineData(ChangeSource.Pointer, ChangeActor.User)]
        [InlineData(ChangeSource.Keyboard, ChangeActor.User)]
        [InlineData(ChangeSource.Agent, ChangeActor.Agent)]
        [InlineData(ChangeSource.Program, ChangeActor.Program)]
        public void AClickAndAKeyAreTheSamePlayer(ChangeSource source, ChangeActor actor)
        {
            (Host h, CommitTarget<int> t, _) = IntTarget();
            t.Set(1, source);
            Assert.Equal(actor, h.Path.Last.Actor);
            Assert.Equal(actor, ChangeSources.Actor(source));
        }

        [Fact]
        public void TheOutermostScopeWins()
        {
            // a drop-down's Enter opens As(Keyboard) inside an agent's As(Agent): the agent made the pick
            using (CommitPath.As(ChangeSource.Agent))
            {
                using (CommitPath.As(ChangeSource.Keyboard))
                    Assert.Equal(ChangeSource.Agent, CommitPath.Resolve(ChangeSource.Pointer));
                Assert.Equal(ChangeSource.Agent, CommitPath.Resolve(ChangeSource.Pointer));
            }
            Assert.Equal(ChangeSource.Pointer, CommitPath.Resolve(ChangeSource.Pointer));
        }

        [Fact]
        public void AScopeDisposedTwiceClosesOnce()
        {
            using (CommitPath.As(ChangeSource.Agent))
            {
                CommitPath.SourceScope inner = CommitPath.As(ChangeSource.Program);
                CommitPath.SourceScope copy = inner;
                inner.Dispose();
                copy.Dispose();
                Assert.Equal(1, CommitPath.OpenScopes);
                Assert.Equal(ChangeSource.Agent, CommitPath.Resolve(ChangeSource.Pointer));
            }
            Assert.Equal(0, CommitPath.OpenScopes);
        }

        [Fact]
        public void AHostResetsAScopeLeftOpen()
        {
            CommitPath.As(ChangeSource.Agent); // never disposed: a handler that threw
            Assert.Equal(ChangeSource.Agent, CommitPath.Resolve(ChangeSource.Pointer));
            Assert.True(CommitPath.ResetScopes());
            Assert.Equal(ChangeSource.Pointer, CommitPath.Resolve(ChangeSource.Pointer));
            Assert.False(CommitPath.ResetScopes());
        }

        [Fact]
        public void ASegmentedPickRecordsTheSourceTheModelShows()
        {
            var path = new CommitPath("W");
            int v = 0;
            CommitTarget<int> t = path.Target("Segmented", () => v, x => v = x);
            var choice = new ChoiceModel(3, () => v, t.Set);
            choice.Select(2, ChangeSource.Agent);
            Assert.Equal(ChangeSource.Agent, choice.LastSource);
            Assert.Equal(choice.LastSource, path.Last.Source);
            choice.Key(ChoiceKey.Left);
            Assert.Equal(ChangeSource.Keyboard, choice.LastSource);
            Assert.Equal(choice.LastSource, path.Last.Source);
            Assert.Equal(2, path.Last.Old);
            Assert.Equal(1, path.Last.New);
        }

        [Fact]
        public void AStepperNudgeAndTypingRecordTheSourceTheModelShows()
        {
            var path = new CommitPath("W");
            double v = 1;
            string Text() => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            CommitTarget<string> t = path.Target("Stepper", Text, s => v = double.Parse(s, System.Globalization.CultureInfo.InvariantCulture));
            var m = new StepperModel(Text, t.Set, (d, source) => t.Apply(d, x => v += x, source), () => 1);
            m.Nudge(1, false, ChangeSource.Pointer);
            Assert.Equal(m.LastSource, path.Last.Source);
            Assert.Equal("2", path.Last.New);
            m.Key(StepperKey.Up, false);
            Assert.Equal(ChangeSource.Keyboard, path.Last.Source);
            m.Type("7", ChangeSource.Agent);
            Assert.Equal(ChangeSource.Agent, m.LastSource);
            Assert.Equal(m.LastSource, path.Last.Source);
            Assert.Equal("3", path.Last.Old);
            Assert.Equal("7", path.Last.New);
        }

        [Fact]
        public void ARefusedPickIsNoCommit()
        {
            var path = new CommitPath("W");
            int v = 0;
            CommitTarget<int> t = path.Target("Segmented", () => v, x => v = x);
            var choice = new ChoiceModel(3, () => v, t.Set) { Allow = (i, s) => false };
            choice.Select(1, ChangeSource.Pointer);
            Assert.Equal(0, path.Total);
        }

        [Fact]
        public void AHeldRepeatIsOneCommit()
        {
            var path = new CommitPath("W");
            double v = 0;
            string Text() => v.ToString(System.Globalization.CultureInfo.InvariantCulture);
            CommitTarget<string> t = path.Target("Stepper", Text, s => { });
            var m = new StepperModel(Text, t.Set, (d, source) => t.Apply(d, x => v += x, source), () => 1);
            t.BeginGroup(); // the press
            for (int i = 0; i < 5; i++) m.Nudge(1, false, ChangeSource.Pointer);
            t.EndGroup(); // the release
            Assert.Equal(1, path.Total);
            Assert.Equal("0", path.Last.Old);
            Assert.Equal("5", path.Last.New);
            Assert.Equal(5, path.Last.Count);
        }
    }
}
