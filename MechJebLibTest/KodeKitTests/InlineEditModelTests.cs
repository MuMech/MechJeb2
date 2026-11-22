/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Globalization;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's inline edit (a read-out that turns into a field), without KSP or Unity.</summary>
    public class InlineEditModelTests
    {
        private sealed class Value
        {
            public          string?                                   Text     = "100";
            public          bool                                      Editable = true;
            public          int                                       Parses;
            public readonly List<KeyValuePair<string, ChangeSource>> Writes   = new List<KeyValuePair<string, ChangeSource>>();

            public void Commit(string s, ChangeSource source)
            {
                Text = s;
                Writes.Add(new KeyValuePair<string, ChangeSource>(s, source));
            }

            public bool Parse(string s)
            {
                Parses++;
                return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
            }
        }

        private static (Value v, InlineEditModel m) Make()
        {
            var v = new Value();
            return (v, new InlineEditModel(() => v.Text, v.Commit, v.Parse, () => v.Editable));
        }

        [Fact]
        public void TheReadOutShowsTheValueUntilAnEditBegins()
        {
            (Value v, InlineEditModel m) = Make();
            Assert.False(m.Editing);
            Assert.Equal("100", m.Shown);
            v.Text = "120";
            Assert.Equal("120", m.Shown);
        }

        [Fact]
        public void BeginStartsTheDraftFromTheValuesText()
        {
            (_, InlineEditModel m) = Make();
            Assert.True(m.Begin());
            Assert.True(m.Editing);
            Assert.Equal("100", m.Draft);
            Assert.Equal("100", m.Original);
            Assert.False(m.Typed);
        }

        [Fact]
        public void TypingChangesTheDraftAndNotTheValue()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            Assert.True(m.Type("2"));
            Assert.True(m.Type("25"));
            Assert.Equal("25", m.Shown);
            Assert.Equal("100", v.Text);
            Assert.Empty(v.Writes);
            Assert.True(m.Typed);
        }

        [Fact]
        public void EnterWritesTheDraftOnceWithItsSourceAndEndsTheEdit()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("250");
            Assert.Equal(TextEditEnd.Committed, m.Submit(ChangeSource.Keyboard));
            Assert.False(m.Editing);
            Assert.Single(v.Writes);
            Assert.Equal("250", v.Writes[0].Key);
            Assert.Equal(ChangeSource.Keyboard, v.Writes[0].Value);
            Assert.Equal(ChangeSource.Keyboard, m.LastSource);
            Assert.Equal("250", m.Shown);
        }

        [Fact]
        public void AFocusLossWritesTheDraftOnce()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("75");
            Assert.Equal(TextEditEnd.Committed, m.Leave(ChangeSource.Pointer));
            Assert.False(m.Editing);
            Assert.Single(v.Writes);
            Assert.Equal("75", v.Text);
        }

        [Fact]
        public void EscapeDropsTheDraftAndWritesNothing()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("999");
            Assert.Equal(TextEditEnd.Cancelled, m.Cancel());
            Assert.False(m.Editing);
            Assert.Empty(v.Writes);
            Assert.Equal("100", m.Shown);
            Assert.Equal(TextEditEnd.Cancelled, m.LastEnd);
        }

        [Fact]
        public void ADraftEqualToTheValueWritesNothing()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("10");
            m.Type("100");
            Assert.Equal(TextEditEnd.Unchanged, m.Submit(ChangeSource.Keyboard));
            Assert.Empty(v.Writes);
            Assert.False(m.Editing);
        }

        [Fact]
        public void AnInvalidDraftAtEnterIsRefusedAndTheEditStaysOpen()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("abc");
            Assert.False(m.Valid);
            Assert.Equal(TextEditEnd.Refused, m.Submit(ChangeSource.Keyboard));
            Assert.True(m.Editing);
            Assert.Equal("abc", m.Draft);
            Assert.Empty(v.Writes);
            Assert.Equal(TextEditEnd.Refused, m.LastEnd);
        }

        [Fact]
        public void AFixedDraftAfterARefusalCommits()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("abc");
            m.Submit(ChangeSource.Keyboard);
            m.Type("42");
            Assert.True(m.Valid);
            Assert.Equal(TextEditEnd.Committed, m.Submit(ChangeSource.Keyboard));
            Assert.Single(v.Writes);
            Assert.Equal("42", v.Text);
        }

        [Fact]
        public void AnInvalidDraftAtAFocusLossEndsTheEditWithTheValueAsItWas()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("1e");
            Assert.Equal(TextEditEnd.Refused, m.Leave(ChangeSource.Pointer));
            Assert.False(m.Editing);
            Assert.Empty(v.Writes);
            Assert.Equal("100", m.Shown);
            Assert.True(m.Valid);
        }

        [Fact]
        public void ValidityIsCheckedWhenTheDraftChangesNotEveryFrame()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("5");
            int checks = v.Parses;
            for (int i = 0; i < 100; i++) m.Sync();
            _ = m.Valid;
            Assert.Equal(checks, v.Parses);
            m.Type("5");
            Assert.Equal(checks, v.Parses);
        }

        [Fact]
        public void BeginOnAReadOnlyValueOpensNoEdit()
        {
            (Value v, InlineEditModel m) = Make();
            v.Editable = false;
            Assert.False(m.Enabled);
            Assert.False(m.Begin());
            Assert.False(m.Editing);
            Assert.False(m.Type("5"));
            Assert.Equal(TextEditEnd.None, m.Submit(ChangeSource.Keyboard));
            Assert.Empty(v.Writes);
        }

        [Fact]
        public void AValueThatBecomesReadOnlyEndsTheEditWithoutWriting()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("5");
            v.Editable = false;
            Assert.True(m.Sync());
            Assert.False(m.Editing);
            Assert.Equal(TextEditEnd.Disabled, m.LastEnd);
            Assert.Empty(v.Writes);
        }

        [Fact]
        public void AnUntypedDraftFollowsTheValueChangingUnderneath()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            v.Text = "130";
            Assert.True(m.Sync());
            Assert.Equal("130", m.Draft);
            Assert.Equal("130", m.Original);
            Assert.False(m.Stale);
            Assert.Equal(TextEditEnd.Unchanged, m.Submit(ChangeSource.Keyboard));
            Assert.Empty(v.Writes);
        }

        [Fact]
        public void ATypedDraftIsKeptWhenTheValueChangesUnderneathAndWinsAtEnter()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("250");
            v.Text = "130";
            Assert.False(m.Sync());
            Assert.Equal("250", m.Draft);
            Assert.True(m.Stale);
            Assert.Equal(TextEditEnd.Committed, m.Submit(ChangeSource.Keyboard));
            Assert.Equal("250", v.Text);
            Assert.Single(v.Writes);
            Assert.False(m.Stale);
        }

        [Fact]
        public void TypingTheOriginalTextBackAfterAChangeUnderneathStillWritesIt()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("1");
            v.Text = "130";
            m.Sync();
            m.Type("100");
            Assert.False(m.Sync());
            Assert.Equal("100", m.Draft);
            // the draft is the text the edit started from, not the value now: Enter writes it
            Assert.Equal(TextEditEnd.Committed, m.Submit(ChangeSource.Keyboard));
            Assert.Equal("100", v.Text);
        }

        [Fact]
        public void EscapeAfterAChangeUnderneathShowsTheNewValue()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("250");
            v.Text = "130";
            m.Sync();
            m.Cancel();
            Assert.Equal("130", m.Shown);
            Assert.Empty(v.Writes);
        }

        [Fact]
        public void BeginWhileEditingKeepsTheDraft()
        {
            (_, InlineEditModel m) = Make();
            m.Begin();
            m.Type("7");
            Assert.False(m.Begin());
            Assert.Equal("7", m.Draft);
        }

        [Fact]
        public void EndingWithNoEditOpenDoesNothing()
        {
            (Value v, InlineEditModel m) = Make();
            Assert.Equal(TextEditEnd.None, m.Submit(ChangeSource.Keyboard));
            Assert.Equal(TextEditEnd.None, m.Leave(ChangeSource.Pointer));
            Assert.Equal(TextEditEnd.None, m.Cancel());
            Assert.False(m.Sync());
            Assert.Empty(v.Writes);
        }

        [Fact]
        public void TheEditIsOverWhenTheCommitRuns()
        {
            var v = new Value();
            InlineEditModel? m = null;
            bool editingInCommit = true;
            string? shownInCommit = null;
            m = new InlineEditModel(() => v.Text, (s, src) =>
            {
                editingInCommit = m!.Editing;
                v.Commit(s, src);
                shownInCommit = m.Shown;
            }, v.Parse);
            m.Begin();
            m.Type("5");
            m.Submit(ChangeSource.Keyboard);
            Assert.False(editingInCommit);
            Assert.Equal("5", shownInCommit);
        }

        [Fact]
        public void ANewEditAfterACommitStartsFromTheWrittenValue()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("5");
            m.Submit(ChangeSource.Keyboard);
            Assert.True(m.Begin());
            Assert.Equal("5", m.Draft);
            Assert.False(m.Typed);
        }

        [Fact]
        public void WithoutAParseCheckEveryDraftIsValid()
        {
            var v = new Value();
            var m = new InlineEditModel(() => v.Text, v.Commit);
            m.Begin();
            m.Type("anything");
            Assert.True(m.Valid);
            Assert.Equal(TextEditEnd.Committed, m.Submit(ChangeSource.Keyboard));
            Assert.Equal("anything", v.Text);
        }

        [Fact]
        public void ANullTypedTextIsAnEmptyDraft()
        {
            (_, InlineEditModel m) = Make();
            m.Begin();
            m.Type(null);
            Assert.Equal("", m.Draft);
            Assert.False(m.Valid);
        }

        [Fact]
        public void ANullValueTextReadsAsEmpty()
        {
            var v = new Value { Text = null };
            var m = new InlineEditModel(() => v.Text, v.Commit);
            Assert.Equal("", m.Shown);
            m.Begin();
            Assert.Equal("", m.Draft);
        }

        private static string StripJunk(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"[^\d+,\-.eE]", "");

        [Fact]
        public void TheNormaliserCleansTheDraftAsTheValueWouldKeepIt()
        {
            var v = new Value();
            var m = new InlineEditModel(() => v.Text, v.Commit, v.Parse, null, StripJunk);
            m.Begin();
            m.Type("1x0");
            Assert.Equal("10", m.Draft);
            Assert.True(m.Valid);
        }

        [Fact]
        public void AJunkKeyAfterAValidDraftLeavesItValidAndEnterWritesTheCleanText()
        {
            var v = new Value();
            var m = new InlineEditModel(() => v.Text, v.Commit, v.Parse, null, StripJunk);
            m.Begin();
            m.Type("15");
            m.Type("15k");
            Assert.Equal("15", m.Draft);
            Assert.True(m.Valid);
            Assert.Equal(TextEditEnd.Committed, m.Leave(ChangeSource.Pointer));
            Assert.Equal("15", v.Text);
        }

        [Fact]
        public void WithoutANormaliserTheDraftIsTheTextAsTyped()
        {
            (_, InlineEditModel m) = Make();
            m.Begin();
            m.Type("1x0");
            Assert.Equal("1x0", m.Draft);
            Assert.False(m.Valid);
        }

        [Fact]
        public void AnAgentsEnterCarriesTheAgentSource()
        {
            (Value v, InlineEditModel m) = Make();
            m.Begin();
            m.Type("3");
            m.Submit(ChangeSource.Agent);
            Assert.Equal(ChangeSource.Agent, v.Writes[0].Value);
        }

        [Fact]
        public void AFinalWriteTheValueRefusesEndsTheEditAsRefusedWithNothingWritten()
        {
            (Value v, InlineEditModel m) = Make();
            m.Store.Allow = (s, src) => false;
            m.Begin();
            m.Type("250");
            Assert.Equal(TextEditEnd.Refused, m.Submit(ChangeSource.Keyboard));
            Assert.False(m.Editing);
            Assert.Equal(TextEditEnd.Refused, m.LastEnd);
            Assert.Empty(v.Writes);
        }
    }
}
