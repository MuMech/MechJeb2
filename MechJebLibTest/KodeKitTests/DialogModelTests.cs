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
    /// <summary>KodeKit's modal dialog (confirmations of destructive actions, prompts) and its queue, without KSP or Unity.</summary>
    public class DialogModelTests
    {
        // a dialog with a record of what it did
        private sealed class Asked
        {
            public readonly DialogModel  Dialog;
            public readonly List<string> Calls = new List<string>();

            public Asked(string title = "Delete window?", object? owner = null, bool destructive = false, string? other = null)
            {
                Dialog = new DialogModel(title, "It can't be undone.")
                {
                    Destructive = destructive,
                    Owner       = owner,
                    OnConfirm   = () => Calls.Add("confirm"),
                    OnCancel    = () => Calls.Add("cancel"),
                    OnOther     = () => Calls.Add("other")
                };
                Dialog.Closed += d => Calls.Add("closed " + d.Result);
                if (other != null) Dialog.OtherText = other;
            }
        }

        // ---- the answer ----

        [Fact]
        public void ConfirmRunsTheActionThenClosed()
        {
            var a = new Asked();
            Assert.True(a.Dialog.Confirm(ChangeSource.Pointer));
            Assert.Equal(new[] { "confirm", "closed Confirmed" }, a.Calls);
            Assert.Equal(DialogResult.Confirmed, a.Dialog.Result);
            Assert.Equal(ChangeSource.Pointer, a.Dialog.ResultSource);
        }

        [Fact]
        public void CancelRunsOnlyTheCancelAction()
        {
            var a = new Asked();
            a.Dialog.Cancel(ChangeSource.Keyboard);
            Assert.Equal(new[] { "cancel", "closed Cancelled" }, a.Calls);
        }

        [Fact]
        public void ADialogEndsOnceAndLaterAnswersDoNothing()
        {
            var a = new Asked();
            Assert.True(a.Dialog.Cancel());
            Assert.False(a.Dialog.Confirm());
            Assert.False(a.Dialog.Dismiss());
            Assert.False(a.Dialog.Press(DialogStop.Confirm, ChangeSource.Pointer));
            Assert.Equal(new[] { "cancel", "closed Cancelled" }, a.Calls);
            Assert.Equal(DialogResult.Cancelled, a.Dialog.Result);
        }

        [Fact]
        public void ADismissedDialogRunsNoAction()
        {
            var a = new Asked();
            a.Dialog.Dismiss();
            Assert.Equal(new[] { "closed Dismissed" }, a.Calls);
        }

        [Fact]
        public void TheThirdButtonRunsItsOwnAction()
        {
            var a = new Asked(other: "Keep both");
            Assert.True(a.Dialog.Press(DialogStop.Other, ChangeSource.Pointer));
            Assert.Equal(new[] { "other", "closed Other" }, a.Calls);
        }

        [Fact]
        public void ADialogWithoutAThirdButtonRefusesItsPress()
        {
            var a = new Asked();
            Assert.False(a.Dialog.Press(DialogStop.Other, ChangeSource.Pointer));
            Assert.Equal(DialogResult.None, a.Dialog.Result);
        }

        [Fact]
        public void AnActionThatThrowsStillEndsTheDialogAndCallsClosed()
        {
            var a = new Asked();
            a.Dialog.OnConfirm = () => throw new InvalidOperationException();
            Assert.Throws<InvalidOperationException>(() => a.Dialog.Confirm());
            Assert.Equal(DialogResult.Confirmed, a.Dialog.Result);
            Assert.Equal(new[] { "closed Confirmed" }, a.Calls);
        }

        // ---- focus and keys ----

        [Fact]
        public void TheFocusStartsOnConfirm()
        {
            Assert.Equal(DialogStop.Confirm, new Asked().Dialog.Focused);
        }

        [Fact]
        public void ADestructiveDialogStartsOnCancel()
        {
            Assert.Equal(DialogStop.Cancel, new Asked(destructive: true).Dialog.Focused);
        }

        [Fact]
        public void ASetDefaultWinsOverTheDestructiveDefault()
        {
            var a = new Asked(destructive: true);
            a.Dialog.Default = DialogStop.Confirm;
            Assert.Equal(DialogStop.Confirm, a.Dialog.Focused);
        }

        [Fact]
        public void TheStopsAreConfirmTheThirdButtonThenCancel()
        {
            AssertionExtensions.EqualSequence(new[] { DialogStop.Confirm, DialogStop.Other, DialogStop.Cancel }, new Asked(other: "Keep").Dialog.Stops);
            AssertionExtensions.EqualSequence(new[] { DialogStop.Confirm, DialogStop.Cancel }, new Asked().Dialog.Stops);
        }

        [Fact]
        public void EnterPressesTheFocusedButton()
        {
            var a = new Asked(destructive: true);
            Assert.True(a.Dialog.Key(ScopeKeys.Enter));
            Assert.Equal(DialogResult.Cancelled, a.Dialog.Result);
            Assert.Equal(ChangeSource.Keyboard, a.Dialog.ResultSource);
        }

        [Fact]
        public void TabThenEnterConfirmsADestructiveDialog()
        {
            var a = new Asked(destructive: true);
            a.Dialog.Key(ScopeKeys.Tab);
            Assert.Equal(DialogStop.Confirm, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Enter);
            Assert.Equal(DialogResult.Confirmed, a.Dialog.Result);
        }

        [Fact]
        public void EscapeCancelsWhateverHasTheFocus()
        {
            var a = new Asked();
            Assert.Equal(DialogStop.Confirm, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Escape);
            Assert.Equal(DialogResult.Cancelled, a.Dialog.Result);
        }

        [Fact]
        public void TabAndShiftTabWrapInsideTheDialog()
        {
            var a = new Asked(other: "Keep");
            a.Dialog.Key(ScopeKeys.Tab);
            a.Dialog.Key(ScopeKeys.Tab);
            Assert.Equal(DialogStop.Cancel, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Tab);
            Assert.Equal(DialogStop.Confirm, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Tab, true);
            Assert.Equal(DialogStop.Cancel, a.Dialog.Focused);
        }

        [Fact]
        public void LeftAndRightMoveAmongTheButtonsOnly()
        {
            var a = new Asked();
            a.Dialog.MakePrompt("name");
            Assert.Equal(DialogStop.Input, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Right);
            Assert.Equal(DialogStop.Confirm, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Left);
            Assert.Equal(DialogStop.Cancel, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Right);
            Assert.Equal(DialogStop.Confirm, a.Dialog.Focused);
        }

        [Fact]
        public void KeysOtherThanTheDialogsDoNothing()
        {
            var a = new Asked();
            Assert.False(a.Dialog.Key(ScopeKeys.Down));
            Assert.Equal(DialogResult.None, a.Dialog.Result);
            Assert.Equal(DialogStop.Confirm, a.Dialog.Focused);
        }

        [Fact]
        public void TheVersionChangesWithTheFocus()
        {
            var a = new Asked();
            int v = a.Dialog.Version;
            a.Dialog.Key(ScopeKeys.Tab);
            Assert.NotEqual(v, a.Dialog.Version);
        }

        // ---- the prompt ----

        [Fact]
        public void APromptStartsOnItsInputAndTabsThroughIt()
        {
            var a = new Asked();
            a.Dialog.MakePrompt("Orbit info");
            AssertionExtensions.EqualSequence(new[] { DialogStop.Input, DialogStop.Confirm, DialogStop.Cancel }, a.Dialog.Stops);
            Assert.Equal(DialogStop.Input, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Tab, true);
            Assert.Equal(DialogStop.Cancel, a.Dialog.Focused);
            a.Dialog.Key(ScopeKeys.Tab);
            Assert.Equal(DialogStop.Input, a.Dialog.Focused);
        }

        [Fact]
        public void TabOnThePromptsInputMovesTheFocusToConfirm()
        {
            var a = new Asked();
            a.Dialog.MakePrompt("Orbit info");
            int v = a.Dialog.Version;
            Assert.True(a.Dialog.Key(ScopeKeys.Tab));
            Assert.Equal(DialogStop.Confirm, a.Dialog.Focused);
            Assert.NotEqual(v, a.Dialog.Version);
            Assert.Equal(DialogResult.None, a.Dialog.Result);
        }

        [Fact]
        public void AnActionThatThrowsIsReportedOnceWhenAHandlerIsSet()
        {
            var a = new Asked();
            var reported = new List<Exception>();
            a.Dialog.OnConfirm = () => throw new InvalidOperationException();
            a.Dialog.ActionFailed += (d, e) => reported.Add(e);
            Assert.True(a.Dialog.Confirm());
            Assert.Single(reported);
            Assert.IsType<InvalidOperationException>(reported[0]);
            Assert.Equal(DialogResult.Confirmed, a.Dialog.Result);
            Assert.Equal(new[] { "closed Confirmed" }, a.Calls);
        }

        [Fact]
        public void TheQueueMovesOnWhenAReportedActionThrows()
        {
            var q = new DialogQueue();
            Asked a = new Asked("A"), b = new Asked("B");
            a.Dialog.OnConfirm = () => throw new InvalidOperationException();
            a.Dialog.ActionFailed += (d, e) => { };
            q.Show(a.Dialog);
            q.Show(b.Dialog);
            a.Dialog.Confirm();
            Assert.Same(b.Dialog, q.Current);
        }

        [Fact]
        public void ADialogShownFromClosedDuringDismissAllIsRefused()
        {
            var q = new DialogQueue();
            Asked a = new Asked("A"), b = new Asked("B");
            var late = new Asked("late");
            bool shown = true;
            a.Dialog.Closed += d => shown = q.Show(late.Dialog);
            q.Show(a.Dialog);
            q.Show(b.Dialog);
            q.DismissAll();
            Assert.False(shown);
            Assert.False(q.Busy);
            Assert.Null(q.Current);
            Assert.True(q.Show(new Asked("after").Dialog));
        }

        [Fact]
        public void EnterOnThePromptsInputConfirms()
        {
            var a = new Asked();
            a.Dialog.MakePrompt("Orbit info");
            a.Dialog.Input = "Landing info";
            a.Dialog.Key(ScopeKeys.Enter);
            Assert.Equal(DialogResult.Confirmed, a.Dialog.Result);
            Assert.Equal("Landing info", a.Dialog.Input);
        }

        [Fact]
        public void AnInvalidInputCannotBeConfirmed()
        {
            var a = new Asked();
            a.Dialog.MakePrompt("Orbit info", s => s.Trim().Length > 0);
            a.Dialog.Input = "  ";
            Assert.False(a.Dialog.Valid);
            Assert.False(a.Dialog.CanConfirm);
            Assert.False(a.Dialog.Confirm());
            Assert.False(a.Dialog.Key(ScopeKeys.Enter));
            Assert.Equal(DialogResult.None, a.Dialog.Result);
            Assert.Empty(a.Calls);
        }

        [Fact]
        public void AnInvalidInputCanStillBeCancelled()
        {
            var a = new Asked();
            a.Dialog.MakePrompt("", s => s.Length > 0);
            Assert.True(a.Dialog.Cancel());
            Assert.Equal(DialogResult.Cancelled, a.Dialog.Result);
        }

        [Fact]
        public void TheInputBecomingValidAllowsConfirm()
        {
            var a = new Asked();
            a.Dialog.MakePrompt("", s => s.Length > 0);
            int v = a.Dialog.Version;
            a.Dialog.Input = "x";
            Assert.True(a.Dialog.CanConfirm);
            Assert.NotEqual(v, a.Dialog.Version);
        }

        [Fact]
        public void ADialogWithoutInputIsAlwaysValid()
        {
            Assert.True(new Asked().Dialog.Valid);
        }

        // ---- the owner ----

        [Fact]
        public void AnOwnerThatGoesDismissesTheDialogAtTheNextTick()
        {
            bool alive = true;
            var a = new Asked();
            a.Dialog.OwnerAlive = () => alive;
            Assert.False(a.Dialog.Tick());
            alive = false;
            Assert.True(a.Dialog.Tick());
            Assert.Equal(DialogResult.Dismissed, a.Dialog.Result);
            Assert.Equal(new[] { "closed Dismissed" }, a.Calls);
        }

        // ---- the queue ----

        [Fact]
        public void TheFirstDialogIsShownAtOnce()
        {
            var q = new DialogQueue();
            var opened = new List<DialogModel>();
            q.Opened += opened.Add;
            var a = new Asked();
            Assert.True(q.Show(a.Dialog));
            Assert.Same(a.Dialog, q.Current);
            Assert.True(a.Dialog.IsShown);
            Assert.Equal(new[] { a.Dialog }, opened);
        }

        [Fact]
        public void ASecondDialogWaitsAndIsShownWhenTheFirstEnds()
        {
            var q = new DialogQueue();
            var events = new List<string>();
            q.Opened += d => events.Add("open " + d.Title);
            q.Hidden += d => events.Add("hide " + d.Title);
            Asked first = new Asked("A"), second = new Asked("B");
            q.Show(first.Dialog);
            Assert.True(q.Show(second.Dialog));
            Assert.Same(first.Dialog, q.Current);
            Assert.Equal(new[] { second.Dialog }, q.Pending);
            first.Dialog.Cancel();
            Assert.Same(second.Dialog, q.Current);
            Assert.Equal(new[] { "open A", "hide A", "open B" }, events);
        }

        [Fact]
        public void TheSameQuestionAskedAgainIsRefused()
        {
            var q = new DialogQueue();
            object button = new object();
            var a = new Asked(owner: button);
            q.Show(a.Dialog);
            Assert.False(q.Show(new Asked(owner: button).Dialog));
            Assert.Empty(q.Pending);
        }

        [Fact]
        public void TheSameQuestionWaitingIsRefusedToo()
        {
            var q = new DialogQueue();
            object button = new object();
            q.Show(new Asked("A").Dialog);
            Assert.True(q.Show(new Asked("B", button).Dialog));
            Assert.False(q.Show(new Asked("B", button).Dialog));
            Assert.Single(q.Pending);
        }

        [Fact]
        public void AnEndedOrShownDialogIsRefused()
        {
            var q = new DialogQueue();
            var a = new Asked();
            q.Show(a.Dialog);
            Assert.False(q.Show(a.Dialog));
            var ended = new Asked("other");
            ended.Dialog.Cancel();
            Assert.False(q.Show(ended.Dialog));
        }

        [Fact]
        public void TheQueueHoldsAtMostMaxPending()
        {
            var q = new DialogQueue();
            q.Show(new Asked("shown").Dialog);
            for (int i = 0; i < DialogQueue.MaxPending; i++) Assert.True(q.Show(new Asked("d" + i).Dialog));
            Assert.False(q.Show(new Asked("one too many").Dialog));
        }

        [Fact]
        public void AWaitingDialogWhoseOwnerWentIsSkipped()
        {
            var q = new DialogQueue();
            bool alive = true;
            Asked first = new Asked("A"), gone = new Asked("B"), last = new Asked("C");
            gone.Dialog.OwnerAlive = () => alive;
            q.Show(first.Dialog);
            q.Show(gone.Dialog);
            q.Show(last.Dialog);
            alive = false;
            first.Dialog.Confirm();
            Assert.Same(last.Dialog, q.Current);
            Assert.Equal(DialogResult.Dismissed, gone.Dialog.Result);
            Assert.Empty(gone.Calls.FindAll(c => c != "closed Dismissed"));
        }

        [Fact]
        public void TickDismissesTheShownAndWaitingDialogsWhoseOwnerWent()
        {
            var q = new DialogQueue();
            bool alive = true;
            Asked shown = new Asked("A"), waiting = new Asked("B");
            shown.Dialog.OwnerAlive = () => alive;
            waiting.Dialog.OwnerAlive = () => alive;
            q.Show(shown.Dialog);
            q.Show(waiting.Dialog);
            alive = false;
            q.Tick();
            Assert.Null(q.Current);
            Assert.False(q.Busy);
            Assert.Equal(DialogResult.Dismissed, shown.Dialog.Result);
            Assert.Equal(DialogResult.Dismissed, waiting.Dialog.Result);
        }

        [Fact]
        public void DismissAllEndsEveryDialogAndShowsNoneNext()
        {
            var q = new DialogQueue();
            var opened = new List<string>();
            q.Opened += d => opened.Add(d.Title);
            Asked a = new Asked("A"), b = new Asked("B");
            q.Show(a.Dialog);
            q.Show(b.Dialog);
            q.DismissAll();
            Assert.Equal(new[] { "A" }, opened);
            Assert.False(q.Busy);
            Assert.Equal(DialogResult.Dismissed, a.Dialog.Result);
            Assert.Equal(DialogResult.Dismissed, b.Dialog.Result);
        }

        [Fact]
        public void AWaitingDialogCancelledByTheProgramLeavesTheQueue()
        {
            var q = new DialogQueue();
            Asked a = new Asked("A"), b = new Asked("B");
            q.Show(a.Dialog);
            q.Show(b.Dialog);
            b.Dialog.Cancel();
            Assert.Empty(q.Pending);
            a.Dialog.Cancel();
            Assert.Null(q.Current);
        }

        [Fact]
        public void ADialogShownFromAnActionWaitsForTheOneThatEnds()
        {
            var q = new DialogQueue();
            var next = new Asked("next");
            var a = new Asked("A");
            a.Dialog.OnConfirm = () => q.Show(next.Dialog);
            q.Show(a.Dialog);
            a.Dialog.Confirm();
            Assert.Same(next.Dialog, q.Current);
            Assert.False(a.Dialog.IsShown);
        }

        [Fact]
        public void TheQueueMovesOnWhenAnActionThrows()
        {
            var q = new DialogQueue();
            Asked a = new Asked("A"), b = new Asked("B");
            a.Dialog.OnConfirm = () => throw new InvalidOperationException();
            q.Show(a.Dialog);
            q.Show(b.Dialog);
            Assert.Throws<InvalidOperationException>(() => a.Dialog.Confirm());
            Assert.Same(b.Dialog, q.Current);
        }
    }
}
