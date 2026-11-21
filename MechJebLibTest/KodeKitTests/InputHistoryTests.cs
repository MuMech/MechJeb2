/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Linq;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's input history (recent values of a field, offered again with Up and Down), without KSP or Unity.</summary>
    public class InputHistoryTests
    {
        private static InputHistory With(string key, params string[] oldestFirst)
        {
            var h = new InputHistory();
            foreach (string v in oldestFirst) h.Record(key, v);
            return h;
        }

        // a field on a commit path, as KodeKit's field is: its text is the target's value
        private sealed class Field
        {
            public readonly CommitPath         Path = new CommitPath("window");
            public readonly CommitTarget<string> Target;
            public          string             Text = "100";

            public Field() => Target = Path.Target("Field", () => Text, s => Text = s);

            // an edit session: focus, the texts typed, its end
            public void Edit(ChangeSource source, params string[] texts)
            {
                Target.BeginGroup();
                foreach (string t in texts) Target.Set(t, source);
                Target.EndGroup();
            }
        }

        private static (Field, InputHistory, List<Commit>) Recording()
        {
            var f       = new Field();
            var h       = new InputHistory();
            var settled = new List<Commit>();
            f.Path.Settled += c =>
            {
                settled.Add(c);
                h.Record("k", c);
            };
            return (f, h, settled);
        }

        // ---- the lists ----

        [Fact]
        public void TheMostRecentValueComesFirst()
        {
            InputHistory h = With("alt", "80", "100", "250");
            Assert.Equal(new[] { "250", "100", "80" }, h.Values("alt"));
        }

        [Fact]
        public void AKeyWithNoValuesHasAnEmptyList()
        {
            var h = new InputHistory();
            Assert.Empty(h.Values("alt"));
            Assert.Empty(h.Values(null));
        }

        [Fact]
        public void AValueAlreadyInTheListMovesToTheFront()
        {
            InputHistory h = With("alt", "80", "100", "250");
            Assert.True(h.Record("alt", "80"));
            Assert.Equal(new[] { "80", "250", "100" }, h.Values("alt"));
        }

        [Fact]
        public void AValueAlreadyFirstChangesNothing()
        {
            InputHistory h       = With("alt", "80");
            int          changed = 0;
            h.Changed += () => changed++;
            Assert.False(h.Record("alt", "80"));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void AListKeepsAtMostPerKeyValuesAndDropsTheOldest()
        {
            var h = new InputHistory(3);
            foreach (string v in new[] { "1", "2", "3", "4" }) h.Record("k", v);
            Assert.Equal(new[] { "4", "3", "2" }, h.Values("k"));
        }

        [Fact]
        public void ANewKeyPastTheCapDropsTheKeyRecordedIntoLeastRecently()
        {
            var h = new InputHistory(8, 2);
            h.Record("a", "1");
            h.Record("b", "1");
            h.Record("a", "2");
            h.Record("c", "1");
            Assert.Equal(2, h.Count);
            Assert.Empty(h.Values("b"));
            Assert.Equal(new[] { "2", "1" }, h.Values("a"));
            Assert.Equal("c", h.KeyAt(0));
        }

        [Fact]
        public void ValuesAreTrimmed()
        {
            var h = new InputHistory();
            h.Record("k", "  250 ");
            Assert.Equal(new[] { "250" }, h.Values("k"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("1\n2")]
        [InlineData("1\r2")]
        [InlineData("12345678901")]
        public void EmptyTooLongOrMultiLineValuesAreNotRecorded(string? value)
        {
            var h = new InputHistory(8, 64, 10);
            Assert.False(h.Record("k", value!));
            Assert.Empty(h.Values("k"));
        }

        [Fact]
        public void NoKeyRecordsNothing()
        {
            var h = new InputHistory();
            Assert.False(h.Record(null!, "1"));
            Assert.False(h.Record("", "1"));
            Assert.Equal(0, h.Count);
        }

        [Fact]
        public void ChangedFiresOnARecordARemovalAndAClear()
        {
            var h       = new InputHistory();
            int changed = 0;
            h.Changed += () => changed++;
            h.Record("k", "1");
            Assert.Equal(1, changed);
            Assert.True(h.Remove("k"));
            Assert.Equal(2, changed);
            h.Record("k", "1");
            h.Clear();
            Assert.Equal(4, changed);
            h.Clear();
            Assert.Equal(4, changed);
        }

        [Fact]
        public void LoadPutsBackASavedListWithoutChanged()
        {
            var h       = new InputHistory(3);
            int changed = 0;
            h.Changed += () => changed++;
            h.Load("k", new[] { "3", " 2 ", "3", "", "1", "0" });
            Assert.Equal(new[] { "3", "2", "1" }, h.Values("k"));
            Assert.Equal(0, changed);
        }

        [Fact]
        public void LoadKeepsTheKeyCap()
        {
            var h = new InputHistory(8, 2);
            h.Load("a", new[] { "1" });
            h.Load("b", new[] { "1" });
            h.Load("c", new[] { "1" });
            Assert.Equal(2, h.Count);
            Assert.Equal("a", h.KeyAt(0));
            Assert.Equal("b", h.KeyAt(1));
        }

        [Fact]
        public void ClearWithoutNotifyDoesNotFireChanged()
        {
            InputHistory h       = With("k", "1");
            int          changed = 0;
            h.Changed += () => changed++;
            h.Clear(false);
            Assert.Equal(0, h.Count);
            Assert.Equal(0, changed);
        }

        // ---- what is recorded ----

        [Fact]
        public void AFinishedEditOfThePlayerIsRecordedOnce()
        {
            (Field f, InputHistory h, List<Commit> settled) = Recording();
            f.Edit(ChangeSource.Keyboard, "2", "25", "250");
            Assert.Single(settled);
            Assert.Equal(new[] { "250" }, h.Values("k"));
        }

        [Fact]
        public void AnEditStillOpenIsNotRecorded()
        {
            var f = new Field();
            var h = new InputHistory();
            f.Target.BeginGroup();
            f.Target.Set("250", ChangeSource.Keyboard);
            Assert.False(h.Record("k", f.Target.Last));
            Assert.Empty(h.Values("k"));
        }

        [Fact]
        public void AnEditThatEndsOnTheOldValueIsNotRecorded()
        {
            (Field f, InputHistory h, _) = Recording();
            // typed, then Escape put the text from before the edit back
            f.Edit(ChangeSource.Keyboard, "2", "100");
            Assert.Empty(h.Values("k"));
        }

        [Fact]
        public void AnAgentsChangeIsNotRecorded()
        {
            (Field f, InputHistory h, _) = Recording();
            f.Edit(ChangeSource.Agent, "250");
            // an agent typing through the bridge: the field reports the host's source
            using (CommitPath.As(ChangeSource.Agent)) f.Edit(CommitPath.Resolve(ChangeSource.Keyboard), "300");
            Assert.Equal("300", f.Text);
            Assert.Empty(h.Values("k"));
        }

        [Fact]
        public void TheProgramsChangeIsNotRecorded()
        {
            (Field f, InputHistory h, _) = Recording();
            f.Target.Set("250", ChangeSource.Program);
            Assert.Empty(h.Values("k"));
        }

        [Fact]
        public void AnUndoIsNotRecordedEvenWhenThePlayerAskedForIt()
        {
            (Field f, InputHistory h, _) = Recording();
            var stack = new UndoStack(f.Path) { Recording = true };
            f.Edit(ChangeSource.Keyboard, "250");
            h.Clear();
            Assert.Equal(UndoResult.Done, stack.Undo(ChangeSource.Pointer));
            Assert.Equal("100", f.Text);
            Assert.Empty(h.Values("k"));
        }

        [Fact]
        public void AChangeMadeInsideAnotherControlsSetterIsNotRecorded()
        {
            var path  = new CommitPath("w");
            string a  = "1", b = "1";
            CommitTarget<string> tb = path.Target("Field", () => b, s => b = s);
            CommitTarget<string> ta = path.Target("Field", () => a, s =>
            {
                a = s;
                tb.Set("9", ChangeSource.Keyboard);
            });
            var h = new InputHistory();
            path.Settled += c => h.Record(c.Target == tb ? "b" : "a", c);
            ta.Set("2", ChangeSource.Keyboard);
            Assert.Equal(new[] { "2" }, h.Values("a"));
            Assert.Empty(h.Values("b"));
        }

        [Fact]
        public void ANonTextValueIsNotRecorded()
        {
            var path = new CommitPath("w");
            bool v   = false;
            CommitTarget<bool> t = path.Target("Toggle", () => v, s => v = s);
            var h = new InputHistory();
            path.Settled += c => h.Record("k", c);
            t.Set(true, ChangeSource.Pointer);
            Assert.Empty(h.Values("k"));
        }

        // ---- the walk ----

        [Fact]
        public void OlderShowsTheMostRecentThenEachOlderValue()
        {
            var b = new InputHistoryBrowser(With("k", "80", "100", "250"), "k");
            Assert.Equal("250", b.Older("12"));
            Assert.Equal("100", b.Older("250"));
            Assert.Equal("80", b.Older("100"));
        }

        [Fact]
        public void OlderAtTheOldestShowsNothingNew()
        {
            var b = new InputHistoryBrowser(With("k", "80"), "k");
            Assert.Equal("80", b.Older("12"));
            Assert.Null(b.Older("80"));
            Assert.Equal(0, b.Index);
        }

        [Fact]
        public void OlderWithNoValuesShowsNothing()
        {
            var b = new InputHistoryBrowser(new InputHistory(), "k");
            Assert.Null(b.Older("12"));
            Assert.False(b.Browsing);
        }

        [Fact]
        public void ValuesEqualToTheTypedTextArePassedOver()
        {
            var b = new InputHistoryBrowser(With("k", "80", "100", "250"), "k");
            // the field shows the last value committed, which is first in the list
            Assert.Equal("100", b.Older("250"));
            Assert.Equal("80", b.Older("100"));
            Assert.Equal("100", b.Newer("80"));
            Assert.Equal("250", b.Newer("100"));
            Assert.False(b.Browsing);
        }

        [Fact]
        public void NewerPastTheMostRecentBringsTheTypedTextBack()
        {
            var b = new InputHistoryBrowser(With("k", "80", "100"), "k");
            b.Older("12");
            b.Older("100");
            Assert.Equal("100", b.Newer("80"));
            Assert.Equal("12", b.Newer("100"));
            Assert.False(b.Browsing);
            Assert.Null(b.Newer("12"));
        }

        [Fact]
        public void NewerWhenNotWalkingShowsNothing()
        {
            var b = new InputHistoryBrowser(With("k", "80"), "k");
            Assert.Null(b.Newer("12"));
        }

        [Fact]
        public void AnotherTextEndsTheWalk()
        {
            var b = new InputHistoryBrowser(With("k", "80", "100", "250"), "k");
            b.Older("12");
            b.Older("250");
            // the player typed over "100"
            Assert.Equal("250", b.Older("1001"));
            Assert.Equal("1001", b.Typed);
            Assert.Equal("1001", b.Newer("250"));
        }

        [Fact]
        public void ResetEndsTheWalk()
        {
            var b = new InputHistoryBrowser(With("k", "80", "100"), "k");
            b.Older("12");
            b.Reset();
            Assert.False(b.Browsing);
            Assert.Null(b.Typed);
            // a new walk from the text the field has now
            Assert.Equal("80", b.Older("100"));
            Assert.Equal("100", b.Typed);
        }

        [Fact]
        public void TheWalkReadsTheLiveList()
        {
            InputHistory h = With("k", "80", "100");
            var          b = new InputHistoryBrowser(h, "k");
            Assert.Equal("100", b.Older("12"));
            h.Record("k", "300");
            // "100" is now second: the walk goes on from that place, past the value shown
            Assert.Equal("80", b.Older("100"));
            Assert.Equal("100", b.Newer("80"));
            Assert.Equal("300", b.Newer("100"));
        }

        [Fact]
        public void TheWalkSurvivesAListThatShrank()
        {
            InputHistory h = With("k", "80", "100", "250");
            var          b = new InputHistoryBrowser(h, "k");
            b.Older("12");
            b.Older("250");
            b.Older("100");
            h.Remove("k");
            Assert.Null(b.Older("80"));
            Assert.Equal("12", b.Newer("80"));
        }

        [Fact]
        public void BrowsersOfTwoFieldsWithOneKeyWalkTheSameList()
        {
            InputHistory h = With("alt", "80", "100");
            var          a = new InputHistoryBrowser(h, "alt");
            var          b = new InputHistoryBrowser(h, "alt");
            Assert.Equal(a.Older("1"), b.Older("2"));
            Assert.Equal(new[] { "100", "80" }, h.Values("alt").ToArray());
        }
    }
}
