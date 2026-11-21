/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's focus recovery (a field being edited keeps the keyboard across its window's rebuild), without KSP or Unity.</summary>
    public class FocusRecoveryTests
    {
        private sealed class Field : IFocusField
        {
            private readonly List<string> _calls;

            public Field(string? key, List<string>? calls = null, string name = "")
            {
                FocusKey = key;
                _calls   = calls ?? new List<string>();
                Name     = name;
            }

            public string         Name     { get; }
            public bool           Focused;
            public FocusSnapshot? Took;
            public int            Holds;
            public FocusRelease?  Released;
            public int            Releases;

            public string? FocusKey   { get; }
            public string? FocusCheck { get; set; }
            public bool    CanFocus   { get; set; } = true;

            public void TakeOver(FocusSnapshot edit)
            {
                Focused = true;
                Took    = edit;
                _calls.Add("take " + Name);
            }

            public void Hold()
            {
                Holds++;
                _calls.Add("hold " + Name);
            }

            public void Release(FocusRelease how)
            {
                Released = how;
                Releases++;
                _calls.Add(how + " " + Name);
            }
        }

        private sealed class Clock
        {
            public int Tick;
        }

        private static (FocusRecovery r, Clock c) Recovery(int maxAge = 1)
        {
            var c = new Clock();
            return (new FocusRecovery(() => c.Tick, maxAge), c);
        }

        private static readonly object _window = new object();

        [Fact]
        public void AFieldAtTheRememberedPlaceWithAnotherCheckIsAnotherFieldAndGetsNothing()
        {
            (FocusRecovery r, _) = Recovery();
            var calls = new List<string>();
            var old = new Field("Field:f1", calls, "old") { FocusCheck = "250" };
            r.Remember(_window, old, "25", "250", 2, 2, null);
            // a field added before it: the place now holds another value's field
            var other = new Field("Field:f1", calls, "other") { FocusCheck = "3" };
            Assert.Equal(FocusOutcome.Mismatched, r.Recover(_window, new IFocusField[] { new Field("Field:f0") { FocusCheck = "250" }, other }));
            Assert.False(other.Focused);
            Assert.Null(other.Took);
            Assert.Equal(FocusRelease.Ended, old.Released);
            Assert.Equal(new[] { "hold old", "Ended old" }, calls);
        }

        [Fact]
        public void AFieldShowingTheTextAtFocusIsTheSameFieldOfAViewThatKeepsTheTypedTextApart()
        {
            (FocusRecovery r, _) = Recovery();
            // the old view's getter returned the typed text; the rebuilt view's returns the model, still the text at focus
            var old = new Field("Field:f0") { FocusCheck = "1.2" };
            r.Remember(_window, old, "1.2", "1", 3, 3, null);
            var rebuilt = new Field("Field:f0") { FocusCheck = "1" };
            Assert.Equal(FocusOutcome.Recovered, r.Recover(_window, new IFocusField[] { rebuilt }));
            Assert.True(rebuilt.Focused);
        }

        [Fact]
        public void AFieldWithTheSameCheckAtThePlaceTakesOverTheEdit()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("Field:f1") { FocusCheck = "250" };
            r.Remember(_window, old, "25", "250", 2, 2, null);
            var same = new Field("Field:f1") { FocusCheck = "250" };
            Assert.Equal(FocusOutcome.Recovered, r.Recover(_window, new IFocusField[] { same }));
            Assert.True(same.Focused);
        }

        [Fact]
        public void AFieldWithoutACheckIsFoundByItsStableNameAlone()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("InlineEdit=item:apoapsis");
            r.Remember(_window, old, "80", "75", 2, 2, null);
            var moved = new Field("InlineEdit=item:apoapsis") { FocusCheck = "anything" };
            Assert.Equal(FocusOutcome.Recovered, r.Recover(_window, new IFocusField[] { new Field("InlineEdit=item:periapsis"), moved }));
            Assert.True(moved.Focused);
        }

        [Fact]
        public void TheSameFieldStillThereIsKeptWhateverItsCheckSays()
        {
            (FocusRecovery r, _) = Recovery();
            var f = new Field("Field:f0") { FocusCheck = "1" };
            r.Remember(_window, f, "12", "1", 2, 2, null);
            f.FocusCheck = "12";
            Assert.Equal(FocusOutcome.Recovered, r.Recover(_window, new IFocusField[] { f }));
            Assert.Equal(FocusRelease.Kept, f.Released);
        }

        [Fact]
        public void TheSameFieldOfTheRebuiltWindowTakesOverTheEditWithItsTextCaretAndSelection()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("id:Alt");
            Assert.True(r.Remember(_window, old, "123", "100", 1, 3, null));

            var other = new Field("id:Inc");
            var same  = new Field("id:Alt");
            Assert.Equal(FocusOutcome.Recovered, r.Recover(_window, new IFocusField[] { other, same }));

            Assert.True(same.Focused);
            Assert.False(other.Focused);
            Assert.Equal("123", same.Took!.Text);
            Assert.Equal("100", same.Took.Original);
            Assert.Equal(1, same.Took.Anchor);
            Assert.Equal(3, same.Took.Caret);
            Assert.Same(old, same.Took.From);
        }

        [Fact]
        public void ARememberedFieldHoldsItsEndOfEditUntilItIsReleased()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("at:f0");

            r.Remember(_window, old, "1", null, 1, 1, null);

            Assert.Equal(1, old.Holds);
            Assert.Equal(0, old.Releases);
        }

        [Fact]
        public void TheOldFieldHandsTheEditOverBeforeTheNewOneTakesIt()
        {
            (FocusRecovery r, _) = Recovery();
            var calls = new List<string>();
            r.Remember(_window, new Field("at:f0", calls, "old"), "1", null, 1, 1, null);

            r.Recover(_window, new IFocusField[] { new Field("at:f0", calls, "new") });

            Assert.Equal(new[] { "hold old", "HandedOver old", "take new" }, calls);
        }

        [Fact]
        public void AFieldNotRecoveredEndsItsEditOnce()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("at:f0");
            r.Remember(_window, old, "1", null, 0, 1, null);

            r.Recover(_window, new IFocusField[] { new Field("at:f1") });
            r.Recover(_window, new IFocusField[] { new Field("at:f1") });

            Assert.Equal(FocusRelease.Ended, old.Released);
            Assert.Equal(1, old.Releases);
        }

        [Fact]
        public void AFieldWithoutANameIsNotRemembered()
        {
            (FocusRecovery r, _) = Recovery();

            Assert.False(r.Remember(_window, new Field(null), "1", null, 0, 1, null));
            Assert.False(r.Remember(_window, new Field(""), "1", null, 0, 1, null));
            Assert.Null(r.Pending);
        }

        [Fact]
        public void AFieldNotRememberedIsNotHeld()
        {
            (FocusRecovery r, _) = Recovery();
            var f = new Field(null);

            r.Remember(_window, f, "1", null, 0, 1, null);

            Assert.Equal(0, f.Holds);
        }

        [Fact]
        public void AnEditWithoutAWindowIsNotRemembered()
        {
            (FocusRecovery r, _) = Recovery();

            Assert.False(r.Remember(null!, new Field("at:f0"), "1", null, 0, 1, null));
            Assert.Null(r.Pending);
        }

        [Fact]
        public void ANewEditReplacesTheOneRemembered()
        {
            (FocusRecovery r, _) = Recovery();
            var first = new Field("at:f0");
            r.Remember(_window, first, "1", null, 0, 1, null);

            r.Remember(_window, new Field("at:f1"), "2", null, 0, 1, null);

            Assert.Equal("at:f1", r.Pending!.Field);
            Assert.Equal(FocusRelease.Ended, first.Released);
        }

        [Fact]
        public void TheSameFieldRememberedAgainIsHeldOnce()
        {
            (FocusRecovery r, _) = Recovery();
            var f = new Field("at:f0");
            r.Remember(_window, f, "1", null, 0, 1, null);

            r.Remember(_window, f, "12", null, 0, 2, null);

            Assert.Equal(1, f.Holds);
            Assert.Equal(0, f.Releases);
            Assert.Equal("12", r.Pending!.Text);
        }

        [Fact]
        public void AFieldWithoutANameDropsAnEarlierEdit()
        {
            (FocusRecovery r, _) = Recovery();
            var first = new Field("at:f0");
            r.Remember(_window, first, "1", null, 0, 1, null);

            r.Remember(_window, new Field(null), "2", null, 0, 1, null);

            Assert.Null(r.Pending);
            Assert.Equal(FocusRelease.Ended, first.Released);
        }

        [Fact]
        public void TheCaretAndSelectionStayWithinTheText()
        {
            (FocusRecovery r, _) = Recovery();

            r.Remember(_window, new Field("at:f0"), "12", null, -3, 9, null);

            Assert.Equal(0, r.Pending!.Anchor);
            Assert.Equal(2, r.Pending.Caret);
        }

        [Fact]
        public void ANullTextIsRememberedAsEmpty()
        {
            (FocusRecovery r, _) = Recovery();

            r.Remember(_window, new Field("at:f0"), null!, null, 0, 4, null);

            Assert.Equal("", r.Pending!.Text);
            Assert.Equal(0, r.Pending.Caret);
        }

        [Fact]
        public void AnotherWindowsRebuildLeavesTheEditRemembered()
        {
            (FocusRecovery r, _) = Recovery();
            r.Remember(_window, new Field("at:f0"), "1", null, 0, 1, null);
            var elsewhere = new Field("at:f0");

            Assert.Equal(FocusOutcome.None, r.Recover(new object(), new IFocusField[] { elsewhere }));

            Assert.False(elsewhere.Focused);
            Assert.True(r.Waits(_window));
        }

        [Fact]
        public void WindowsAreComparedWithEquals()
        {
            (FocusRecovery r, _) = Recovery();
            r.Remember("Ascent Guidance", new Field("at:f0"), "1", null, 0, 1, null);
            var f = new Field("at:f0");

            Assert.Equal(FocusOutcome.Recovered, r.Recover(new string("Ascent Guidance".ToCharArray()), new IFocusField[] { f }));
            Assert.True(f.Focused);
        }

        [Fact]
        public void AWindowWithoutTheFieldGivesTheKeyboardToNoField()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("at:p1/f0");
            r.Remember(_window, old, "1", null, 0, 1, null);
            var neighbour = new Field("at:p0/f0");

            Assert.Equal(FocusOutcome.Gone, r.Recover(_window, new IFocusField[] { neighbour }));

            Assert.False(neighbour.Focused);
            Assert.Equal(FocusRelease.Ended, old.Released);
            Assert.Null(r.Pending);
            Assert.Equal(FocusOutcome.Gone, r.LastOutcome);
        }

        [Fact]
        public void AFieldThatCantTakeTheKeyboardIsGone()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("at:f0");
            r.Remember(_window, old, "1", null, 0, 1, null);
            var hidden = new Field("at:f0") { CanFocus = false };

            Assert.Equal(FocusOutcome.Gone, r.Recover(_window, new IFocusField[] { hidden }));

            Assert.False(hidden.Focused);
            Assert.Equal(FocusRelease.Ended, old.Released);
        }

        [Fact]
        public void TwoFieldsWithTheNameAreNotGuessed()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("at:f0");
            r.Remember(_window, old, "1", null, 0, 1, null);
            var a = new Field("at:f0");
            var b = new Field("at:f0");

            Assert.Equal(FocusOutcome.Ambiguous, r.Recover(_window, new IFocusField[] { a, b }));

            Assert.False(a.Focused);
            Assert.False(b.Focused);
            Assert.Equal(FocusRelease.Ended, old.Released);
        }

        [Fact]
        public void NoFieldsAtAllIsGone()
        {
            (FocusRecovery r, _) = Recovery();
            r.Remember(_window, new Field("at:f0"), "1", null, 0, 1, null);

            Assert.Equal(FocusOutcome.Gone, r.Recover(_window, null!));
        }

        [Fact]
        public void AnEditWaitsForItsWindowForMaxAgeTicks()
        {
            (FocusRecovery r, Clock c) = Recovery(1);
            r.Remember(_window, new Field("at:f0"), "1", null, 0, 1, null);
            c.Tick = 1;
            var f = new Field("at:f0");

            Assert.False(r.Expire());
            Assert.Equal(FocusOutcome.Recovered, r.Recover(_window, new IFocusField[] { f }));
        }

        [Fact]
        public void AnEditNotRecoveredInTimeIsDropped()
        {
            (FocusRecovery r, Clock c) = Recovery(1);
            var old = new Field("at:f0");
            r.Remember(_window, old, "1", null, 0, 1, null);
            c.Tick = 2;
            var f = new Field("at:f0");

            Assert.Equal(FocusOutcome.Expired, r.Recover(_window, new IFocusField[] { f }));

            Assert.False(f.Focused);
            Assert.Null(r.Pending);
            Assert.Equal(FocusRelease.Ended, old.Released);
        }

        [Fact]
        public void ExpireDropsAnOldEditAndKeepsAFreshOne()
        {
            (FocusRecovery r, Clock c) = Recovery(1);
            r.Remember(_window, new Field("at:f0"), "1", null, 0, 1, null);

            Assert.False(r.Expire());
            c.Tick = 5;
            Assert.True(r.Expire());
            Assert.Null(r.Pending);
            Assert.Equal(FocusOutcome.Expired, r.LastOutcome);
        }

        [Fact]
        public void AnEditIsRecoveredOnce()
        {
            (FocusRecovery r, _) = Recovery();
            r.Remember(_window, new Field("at:f0"), "1", null, 0, 1, null);
            var f = new Field("at:f0");
            r.Recover(_window, new IFocusField[] { f });
            f.Focused = false;

            Assert.Equal(FocusOutcome.None, r.Recover(_window, new IFocusField[] { f }));
            Assert.False(f.Focused);
        }

        [Fact]
        public void ForgetDropsTheEditAndEndsIt()
        {
            (FocusRecovery r, _) = Recovery();
            var old = new Field("at:f0");
            r.Remember(_window, old, "1", null, 0, 1, null);

            r.Forget();

            Assert.False(r.Waits(_window));
            Assert.Equal(FocusRelease.Ended, old.Released);
        }

        [Fact]
        public void TheSameFieldStillThereKeepsItsEndOfEdit()
        {
            (FocusRecovery r, _) = Recovery();
            var calls = new List<string>();
            var f = new Field("at:f0", calls, "f");
            r.Remember(_window, f, "1", null, 0, 1, null);

            Assert.Equal(FocusOutcome.Recovered, r.Recover(_window, new IFocusField[] { f }));

            Assert.Equal(new[] { "hold f", "Kept f" }, calls);
        }

        // ---- the edit's commit ----

        private sealed class TextBox
        {
            public string V = "";
        }

        private static CommitTarget<string> FieldTarget(CommitPath path, TextBox box) => path.Target("Field", () => box.V, s => box.V = s);

        [Fact]
        public void OnlyAnOpenCommitIsCarried()
        {
            (FocusRecovery r, _) = Recovery();
            var box = new TextBox();
            var path = new CommitPath("W");
            CommitTarget<string> t = FieldTarget(path, box);
            t.Set("5", ChangeSource.Keyboard); // ungrouped: settled at once

            r.Remember(_window, new Field("at:f0"), "5", null, 1, 1, t.Last);

            Assert.Null(r.Pending!.Session);
        }

        [Fact]
        public void AnEditCarriedIntoTheRebuiltWindowIsOneCommitFromTheValueBeforeTheEdit()
        {
            var box = new TextBox { V = "100" };
            // the window before the rebuild: focus, "1", "12"
            var before = new CommitPath("W");
            CommitTarget<string> a = FieldTarget(before, box);
            a.BeginGroup();
            a.Set("1", ChangeSource.Keyboard);
            a.Set("12", ChangeSource.Keyboard);
            Commit first = a.Last!;
            Assert.True(first.Open);
            (FocusRecovery r, _) = Recovery();
            r.Remember(_window, new Field("at:f0"), "12", "100", 2, 2, first);
            a.EndGroup(); // the old field is deselected and destroyed

            // the rebuilt window: the same field takes the edit over, "123", Enter
            var after = new CommitPath("W");
            var settled = new List<Commit>();
            after.Settled += settled.Add;
            var undo = new UndoStack(after) { Recording = true };
            CommitTarget<string> b = FieldTarget(after, box);
            b.ContinueGroup(r.Pending!.Session);
            b.Set("123", ChangeSource.Keyboard);
            b.EndGroup();

            Commit c = Assert.Single(after.Log);
            Assert.Equal("100", c.Old);
            Assert.Equal("123", c.New);
            Assert.Same(first, c.Replaces);
            Assert.Equal(3, c.Count);
            Assert.Single(settled);
            Assert.Single(undo.Done);
            Assert.Equal(UndoResult.Done, undo.Undo());
            Assert.Equal("100", box.V);
        }

        [Fact]
        public void AnEditCarriedOverThatChangesNothingMoreMakesNoCommit()
        {
            var box = new TextBox();
            var before = new CommitPath("W");
            CommitTarget<string> a = FieldTarget(before, box);
            a.BeginGroup();
            a.Set("4", ChangeSource.Keyboard);
            Commit first = a.Last!;
            a.EndGroup();

            var after = new CommitPath("W");
            CommitTarget<string> b = FieldTarget(after, box);
            b.ContinueGroup(first);
            b.EndGroup();

            Assert.Empty(after.Log);
            Assert.Equal(0, after.Total);
        }

        [Fact]
        public void ContinueGroupWithoutACommitIsAPlainGroup()
        {
            var box = new TextBox();
            var path = new CommitPath("W");
            CommitTarget<string> t = FieldTarget(path, box);

            t.ContinueGroup(null);
            t.Set("1", ChangeSource.Keyboard);
            t.Set("12", ChangeSource.Keyboard);
            t.EndGroup();

            Commit c = Assert.Single(path.Log);
            Assert.Equal("", c.Old);
            Assert.Null(c.Replaces);
            Assert.Equal(2, c.Count);
        }

        [Fact]
        public void AnotherSourceDoesNotCarryOnTheEdit()
        {
            var box = new TextBox();
            var before = new CommitPath("W");
            CommitTarget<string> a = FieldTarget(before, box);
            a.BeginGroup();
            a.Set("4", ChangeSource.Keyboard);
            Commit first = a.Last!;
            a.EndGroup();

            var after = new CommitPath("W");
            CommitTarget<string> b = FieldTarget(after, box);
            b.ContinueGroup(first);
            b.Set("9", ChangeSource.Agent);
            b.EndGroup();

            Commit c = Assert.Single(after.Log);
            Assert.Equal("4", c.Old);
            Assert.Null(c.Replaces);
        }
    }
}
