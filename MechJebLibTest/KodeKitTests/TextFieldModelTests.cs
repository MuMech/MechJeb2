/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The text field's rules written as typed (<see cref="TextFieldModel"/>); the rules at the end are InlineEditModelTests'.</summary>
    public class TextFieldModelTests
    {
        private sealed class Editable
        {
            public          string                                   Text = "100";
            public readonly List<KeyValuePair<string, ChangeSource>> Sets = new List<KeyValuePair<string, ChangeSource>>();

            // MechJeb's editables strip junk: only digits are kept
            public void Set(string s, ChangeSource source)
            {
                Sets.Add(new KeyValuePair<string, ChangeSource>(s, source));
                var kept = new System.Text.StringBuilder();
                foreach (char c in s)
                    if (char.IsDigit(c))
                        kept.Append(c);
                Text = kept.ToString();
            }

            public bool Parsed => Text.Length > 0;
        }

        private static (Editable e, TextFieldModel m, List<string> session) Make()
        {
            var e       = new Editable();
            var session = new List<string>();
            var m = new TextFieldModel(() => e.Text, e.Set, TextCommit.AsTyped, null, () => e.Parsed, null, null,
                () => session.Add("begin"), () => session.Add("end"));
            return (e, m, session);
        }

        [Fact]
        public void EachTypedTextIsWrittenAtOnceWithItsSource()
        {
            (Editable e, TextFieldModel m, _) = Make();
            m.Begin();
            m.Type("1", ChangeSource.Keyboard);
            m.Type("12", ChangeSource.Keyboard);
            Assert.Equal("12", e.Text);
            Assert.Equal(2, e.Sets.Count);
            Assert.Equal(ChangeSource.Keyboard, e.Sets[1].Value);
        }

        [Fact]
        public void TheDraftIsWhatTheValueKeptSoStrippedJunkIsShownCleaned()
        {
            (_, TextFieldModel m, _) = Make();
            m.Begin();
            m.Type("12x", ChangeSource.Keyboard);
            Assert.Equal("12", m.Shown);
        }

        [Fact]
        public void AnEditIsOneSessionFromBeginToItsEnd()
        {
            (_, TextFieldModel m, List<string> session) = Make();
            m.Begin();
            m.Type("5", ChangeSource.Keyboard);
            m.Type("50", ChangeSource.Keyboard);
            m.Submit(ChangeSource.Keyboard);
            Assert.Equal(new[] { "begin", "end" }, session);
        }

        [Fact]
        public void EnterAndFocusLossWriteNothingMoreAsTyped()
        {
            (Editable e, TextFieldModel m, _) = Make();
            m.Begin();
            m.Type("5", ChangeSource.Keyboard);
            Assert.Equal(TextEditEnd.Committed, m.Submit(ChangeSource.Keyboard));
            m.Begin();
            Assert.Equal(TextEditEnd.Unchanged, m.Leave(ChangeSource.Keyboard));
            Assert.Single(e.Sets);
        }

        [Fact]
        public void EscapeWritesTheOriginalBackOnceInTheSession()
        {
            (Editable e, TextFieldModel m, List<string> session) = Make();
            m.Begin();
            m.Type("7", ChangeSource.Keyboard);
            Assert.Equal(TextEditEnd.Cancelled, m.Cancel(ChangeSource.Keyboard));
            Assert.Equal("100", e.Text);
            Assert.Equal(2, e.Sets.Count);
            Assert.Equal(new[] { "begin", "end" }, session);
        }

        [Fact]
        public void EscapeWithNothingTypedWritesNothing()
        {
            (Editable e, TextFieldModel m, _) = Make();
            m.Begin();
            m.Cancel(ChangeSource.Keyboard);
            Assert.Empty(e.Sets);
        }

        [Fact]
        public void EscapeReturnsToTheTextAResumedEditBeganWith()
        {
            (Editable e, TextFieldModel m, _) = Make();
            m.Begin();
            m.Type("55", ChangeSource.Keyboard);
            Assert.True(m.Resume("42"));
            m.Cancel(ChangeSource.Keyboard);
            Assert.Equal("42", e.Text);
        }

        [Fact]
        public void EnterOnAnInvalidTextStillEndsTheEditAsTyped()
        {
            (_, TextFieldModel m, _) = Make();
            m.Begin();
            m.Type("x", ChangeSource.Keyboard);
            m.Sync();
            Assert.False(m.Valid);
            m.Submit(ChangeSource.Keyboard);
            Assert.False(m.Editing);
        }

        [Fact]
        public void ValidityIsTheValuesOwnCheckReadOnceAFrame()
        {
            (Editable e, TextFieldModel m, _) = Make();
            Assert.True(m.Valid);
            e.Text = "";
            Assert.True(m.Valid);
            Assert.True(m.Sync());
            Assert.False(m.Valid);
        }

        [Fact]
        public void TheValueChangingUnderAnOpenEditKeepsTheTypedTextAndIsStale()
        {
            (Editable e, TextFieldModel m, _) = Make();
            m.Begin();
            m.Type("5", ChangeSource.Keyboard);
            e.Text = "900";
            m.Sync();
            Assert.Equal("5", m.Shown);
            Assert.True(m.Stale);
        }

        [Fact]
        public void WithNoEditOpenTheShownTextFollowsTheValue()
        {
            (Editable e, TextFieldModel m, _) = Make();
            e.Text = "321";
            Assert.True(m.Sync());
            Assert.Equal("321", m.Shown);
        }

        [Fact]
        public void TypingWithNoEditOpenIsWrittenWithoutASession()
        {
            (Editable e, TextFieldModel m, List<string> session) = Make();
            Assert.True(m.Type("8", ChangeSource.Agent));
            Assert.Equal("8", e.Text);
            Assert.Empty(session);
        }

        [Fact]
        public void DropEndsTheEditWithNothingWritten()
        {
            (Editable e, TextFieldModel m, List<string> session) = Make();
            m.Begin();
            Assert.Equal(TextEditEnd.Cancelled, m.Drop());
            Assert.Empty(e.Sets);
            Assert.Equal(new[] { "begin", "end" }, session);
        }

        [Fact]
        public void BeginKeepsAnEditAlreadyOpen()
        {
            (_, TextFieldModel m, List<string> session) = Make();
            m.Begin();
            m.Type("5", ChangeSource.Keyboard);
            Assert.False(m.Begin());
            Assert.Equal("5", m.Draft);
            Assert.Single(session);
        }

        [Fact]
        public void AReadOnlyValueOpensNoEditAndRefusesTyping()
        {
            var  e        = new Editable();
            bool editable = false;
            var  m        = new TextFieldModel(() => e.Text, e.Set, TextCommit.AsTyped, enabled: () => editable);
            Assert.False(m.Begin());
            Assert.False(m.Type("5", ChangeSource.Keyboard));
            Assert.Empty(e.Sets);
        }

        [Fact]
        public void TheEditOnACommitPathIsOneCommitWithTheKeyboardSource()
        {
            var    path   = new CommitPath("w");
            string text   = "100";
            CommitTarget<string> t = path.Target("Field", () => text, s => text = s);
            var    m      = new TextFieldModel(() => text, t.Set, TextCommit.AsTyped,
                beginSession: () => t.BeginGroup(), endSession: t.EndGroup);
            var settled = new List<Commit>();
            path.Settled += settled.Add;
            m.Begin();
            m.Type("1", ChangeSource.Keyboard);
            m.Type("12", ChangeSource.Keyboard);
            m.Type("125", ChangeSource.Keyboard);
            m.Submit(ChangeSource.Keyboard);
            Assert.Single(settled);
            Assert.Equal("100", settled[0].Old);
            Assert.Equal("125", settled[0].New);
            Assert.Equal(ChangeSource.Keyboard, settled[0].Source);
        }

        [Fact]
        public void TheModelDescribesATextBoxFocusedWhileEditingAndInvalidWhenTheValueDoesNotParse()
        {
            (Editable e, TextFieldModel m, _) = Make();
            m.Label = "Altitude";
            m.Begin();
            e.Text = "";
            m.Sync();
            var parts = new List<PartDescription>();
            m.Describe(parts);
            Assert.Equal(PartRole.TextBox, parts[0].Role);
            Assert.True(parts[0].Has(PartFlags.Focus));
            Assert.True(parts[0].Has(PartFlags.Invalid));
        }

        [Fact]
        public void AValueTheHostShowsInAnOpenEditIsTheDraftAndStaysThroughTheSync()
        {
            (Editable e, TextFieldModel m, _) = Make();
            m.Begin();
            e.Set("101", ChangeSource.Keyboard); // a stepper's Up set the value
            Assert.True(m.Shows("101"));
            m.Sync();
            Assert.Equal("101", m.Shown);
            Assert.False(m.Stale);
        }

        [Fact]
        public void EscapeAfterANudgeTheHostShowedWritesTheOriginalBack()
        {
            (Editable e, TextFieldModel m, _) = Make();
            m.Begin();
            e.Set("101", ChangeSource.Keyboard);
            m.Shows("101");
            m.Cancel(ChangeSource.Keyboard);
            Assert.Equal("100", e.Text);
        }

        [Fact]
        public void TheHostShowingATextChangesNothingInAnEditWrittenAtTheEnd()
        {
            string text = "100";
            var    m    = new TextFieldModel(() => text, (s, src) => text = s, TextCommit.AtEnd);
            m.Begin();
            m.Type("5", ChangeSource.Keyboard);
            Assert.False(m.Shows("200"));
            Assert.Equal("5", m.Draft);
        }
    }
}
