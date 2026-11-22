/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's keyboard scopes (who gets the keys, how long KSP's keyboard lock is held), without KSP or Unity.</summary>
    public class KeyboardScopesTests
    {
        private sealed class Keyboard : IScopeKeyboard
        {
            public int       Frame { get; set; }
            public ScopeKeys Held  { get; set; }
        }

        // a scope with a record of what it got
        private sealed class Owner
        {
            public readonly KeyScope        Scope;
            public readonly List<ScopeKeys> Keys      = new List<ScopeKeys>();
            public readonly List<char>      Text      = new List<char>();
            public          int             Dismissed;
            public          bool            Lock;
            public          int             LockChanges;
            public          bool            Gone;

            public Owner(KeyboardScopes scopes, ScopeKeys routes, bool navigation = false)
            {
                Scope             =  scopes.Create(this, routes, navigation, () => !Gone);
                Scope.Key += k => Keys.Add(k);
                Scope.Typed += c => Text.Add(c);
                Scope.Dismissed += () => Dismissed++;
                Scope.LockChanged += on =>
                {
                    Lock = on;
                    LockChanges++;
                };
            }
        }

        private readonly Keyboard       keyboard = new Keyboard();
        private readonly KeyboardScopes scopes;

        public KeyboardScopesTests() => scopes = new KeyboardScopes(keyboard);

        private Owner Field() => new Owner(scopes, ScopeKeys.None);
        private Owner DropDown() => new Owner(scopes, ScopeKeys.All, true);

        // a frame: keys go down (and stay held), the scopes get them, then the tick after every update
        private void Frame(ScopeKeys down = ScopeKeys.None, string? typed = null)
        {
            keyboard.Frame++;
            keyboard.Held |= down;
            scopes.Dispatch(down, () => typed);
            scopes.Tick();
        }

        private void Release(ScopeKeys keys)
        {
            keyboard.Frame++;
            keyboard.Held &= ~keys;
            scopes.Dispatch(ScopeKeys.None);
            scopes.Tick();
        }

        [Fact]
        public void AFieldsScopeGetsTheKeysItsOwnerAddedOnlyWhileItIsOnTop()
        {
            Owner field = Field();
            field.Scope.Routes |= ScopeKeys.Up | ScopeKeys.Down;
            field.Scope.Open();
            Frame(ScopeKeys.Up);
            Release(ScopeKeys.Up);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Up }, field.Keys);
            // a dialog over the field: its keys are the dialog's (the stepper under it doesn't nudge)
            Owner dialog = new Owner(scopes, ScopeKeys.Enter | ScopeKeys.Escape | ScopeKeys.Tab);
            dialog.Scope.Open();
            Frame(ScopeKeys.Down);
            Release(ScopeKeys.Down);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Up }, field.Keys);
            Assert.Empty(dialog.Keys);
            dialog.Scope.Close();
            Frame();
            Frame(ScopeKeys.Down);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Up, ScopeKeys.Down }, field.Keys);
        }

        [Fact]
        public void AKeyHeldAsADialogClosesDoesNotReachTheFieldUnderIt()
        {
            Owner field = Field();
            field.Scope.Routes |= ScopeKeys.Tab;
            field.Scope.Open();
            Owner dialog = new Owner(scopes, ScopeKeys.Enter | ScopeKeys.Escape | ScopeKeys.Tab);
            dialog.Scope.Open();
            Frame(ScopeKeys.Tab);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Tab }, dialog.Keys);
            dialog.Scope.Drop();
            Frame(ScopeKeys.Tab);
            Assert.Empty(field.Keys);
            Release(ScopeKeys.Tab);
            Frame(ScopeKeys.Tab);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Tab }, field.Keys);
        }

        [Fact]
        public void TwoOwnersOfOneScopeBothGetItsKeys()
        {
            Owner field = Field();
            var second = new List<ScopeKeys>();
            field.Scope.Routes |= ScopeKeys.Up;
            field.Scope.Key += k => second.Add(k);
            field.Scope.Open();
            Frame(ScopeKeys.Up);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Up }, field.Keys);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Up }, second);
        }

        [Fact]
        public void NoScopeWantsNoLock()
        {
            Assert.False(scopes.LockWanted);
            Assert.Null(scopes.Top);
            Assert.False(scopes.NavigationOff);
        }

        [Fact]
        public void AnOpenScopeTakesItsLockAtOnce()
        {
            Owner f = Field();
            f.Scope.Open();
            Assert.True(f.Lock);
            Assert.True(scopes.LockWanted);
            Assert.True(f.Scope.OnTop);
        }

        [Fact]
        public void TheTopScopeAloneGetsTheKeys()
        {
            Owner below = DropDown(), top = DropDown();
            below.Scope.Open();
            Frame();
            top.Scope.Open();
            Frame(ScopeKeys.Down);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Down }, top.Keys);
            Assert.Empty(below.Keys);
        }

        [Fact]
        public void AScopeGetsOnlyTheKeysItRoutes()
        {
            Owner o = new Owner(scopes, ScopeKeys.Up | ScopeKeys.Escape);
            o.Scope.Open();
            Frame(ScopeKeys.Up | ScopeKeys.Down | ScopeKeys.Enter);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Up }, o.Keys);
        }

        [Fact]
        public void TabLeftAndRightAreRoutedKeysToo()
        {
            Owner o = new Owner(scopes, ScopeKeys.Tab | ScopeKeys.Left | ScopeKeys.Right | ScopeKeys.Enter);
            o.Scope.Open();
            Frame(ScopeKeys.Tab | ScopeKeys.Left | ScopeKeys.Right | ScopeKeys.Enter);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Enter, ScopeKeys.Left, ScopeKeys.Right, ScopeKeys.Tab }, o.Keys);
        }

        [Fact]
        public void ATabHeldAsTheTopChangesReachesTheNewTopOnlyOnceUp()
        {
            Owner below = new Owner(scopes, ScopeKeys.Tab), top = Field();
            below.Scope.Open();
            Frame();
            top.Scope.Open();
            Frame(ScopeKeys.Tab);
            top.Scope.Close();
            Frame();
            Assert.Empty(below.Keys);
            Release(ScopeKeys.Tab);
            Frame(ScopeKeys.Tab);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Tab }, below.Keys);
        }

        [Fact]
        public void AKeyTheTopScopeDoesNotRouteReachesNoScopeBelow()
        {
            Owner below = DropDown(), field = Field();
            below.Scope.Open();
            Frame();
            field.Scope.Open();
            Frame(ScopeKeys.Down);
            Assert.Empty(below.Keys);
        }

        [Fact]
        public void EscapeClosesTheTopScopeOnly()
        {
            Owner below = DropDown(), top = DropDown();
            below.Scope.Open();
            Frame();
            top.Scope.Open();
            Frame(ScopeKeys.Escape);
            Assert.Equal(1, top.Dismissed);
            Assert.False(top.Scope.IsOpen);
            Assert.True(below.Scope.IsOpen);
            Assert.Equal(0, below.Dismissed);
            Assert.Same(below.Scope, scopes.Top);
        }

        [Fact]
        public void EscapeDoesNothingToAScopeThatDoesNotRouteIt()
        {
            Owner field = Field();
            field.Scope.Open();
            Frame(ScopeKeys.Escape);
            Assert.True(field.Scope.IsOpen);
            Assert.Equal(0, field.Dismissed);
        }

        [Fact]
        public void TheEscapeThatClosedAFieldDoesNotCloseTheDropDownUnderIt()
        {
            Owner below = DropDown(), field = Field();
            below.Scope.Open();
            Frame();
            field.Scope.Open();
            Frame();
            // the field ends its edit on Escape's down, before the scopes give out the frame's keys
            keyboard.Frame++;
            keyboard.Held |= ScopeKeys.Escape;
            field.Scope.Close();
            scopes.Dispatch(ScopeKeys.Escape);
            scopes.Tick();
            Assert.True(below.Scope.IsOpen);
            Frame();
            Assert.True(below.Scope.IsOpen);
            Release(ScopeKeys.Escape);
            Frame(ScopeKeys.Escape);
            Assert.False(below.Scope.IsOpen);
            Assert.Equal(1, below.Dismissed);
        }

        [Fact]
        public void AKeyHeldAsAScopeOpensReachesItOnlyOnceItCameUpAndWentDownAgain()
        {
            Owner o = DropDown();
            keyboard.Held = ScopeKeys.Enter;
            o.Scope.Open();
            Frame(ScopeKeys.Enter);
            Assert.Empty(o.Keys);
            Release(ScopeKeys.Enter);
            Frame(ScopeKeys.Enter);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Enter }, o.Keys);
        }

        [Fact]
        public void KeysAreGivenOutOncePerFrame()
        {
            Owner o = DropDown();
            o.Scope.Open();
            keyboard.Frame++;
            scopes.Dispatch(ScopeKeys.Down);
            scopes.Dispatch(ScopeKeys.Down);
            Assert.Single(o.Keys);
        }

        [Fact]
        public void AKeyThatClosesTheScopeEndsTheFrameForTheScopeBelow()
        {
            Owner below = DropDown(), top = DropDown();
            below.Scope.Open();
            Frame();
            top.Scope.Open();
            top.Scope.Key += k =>
            {
                if (k == ScopeKeys.Enter) top.Scope.Close();
            };
            Frame(ScopeKeys.Enter, "s");
            Assert.Empty(below.Keys);
            Assert.Empty(below.Text);
        }

        [Fact]
        public void LettersAndDigitsGoToAScopeThatRoutesText()
        {
            Owner o = DropDown();
            o.Scope.Open();
            Frame(ScopeKeys.None, "s1 -");
            Assert.Equal(new[] { 's', '1' }, o.Text);
        }

        [Fact]
        public void TextTypedInTheFrameAScopeOpenedIsNotGivenToIt()
        {
            // a chord that types a letter (P, Right Shift+P) opens the palette in frame N, before the scopes' driver runs
            Owner palette = DropDown();
            keyboard.Frame++;
            palette.Scope.Open();
            scopes.Dispatch(ScopeKeys.None, () => "p");
            scopes.Tick();
            Assert.Empty(palette.Text);
            Frame(ScopeKeys.None, "k");
            Assert.Equal(new[] { 'k' }, palette.Text);
        }

        [Fact]
        public void TextIsNotReadForAScopeThatDoesNotRouteIt()
        {
            Owner f = Field();
            f.Scope.Open();
            bool read = false;
            keyboard.Frame++;
            scopes.Dispatch(ScopeKeys.None, () =>
            {
                read = true;
                return "s";
            });
            Assert.False(read);
        }

        [Fact]
        public void AClosedScopeKeepsItsLockUntilTheFrameAfter()
        {
            Owner f = Field();
            f.Scope.Open();
            Frame();
            f.Scope.Close();
            scopes.Tick();
            Assert.True(f.Lock);
            Assert.True(scopes.LockWanted);
            Frame();
            Assert.False(f.Lock);
            Assert.False(scopes.LockWanted);
        }

        [Fact]
        public void AClosedScopeKeepsItsLockWhileEscapeIsHeld()
        {
            Owner f = Field();
            f.Scope.Open();
            Frame(ScopeKeys.Escape);
            f.Scope.Close();
            for (int i = 0; i < 5; i++) Frame();
            Assert.True(f.Lock);
            Release(ScopeKeys.Escape);
            Assert.False(f.Lock);
        }

        [Fact]
        public void AClosedScopeKeepsItsLockWhileEnterIsHeld()
        {
            Owner f = Field();
            f.Scope.Open();
            Frame(ScopeKeys.Enter);
            f.Scope.Close();
            Frame();
            Assert.True(f.Lock);
            Release(ScopeKeys.Enter);
            Assert.False(f.Lock);
        }

        [Fact]
        public void OtherHeldKeysDoNotDelayTheRelease()
        {
            Owner f = Field();
            f.Scope.Open();
            Frame(ScopeKeys.Down);
            f.Scope.Close();
            Frame();
            Assert.False(f.Lock);
        }

        [Fact]
        public void AScopeOpenedAgainWhileReleasingKeepsItsLock()
        {
            Owner f = Field();
            f.Scope.Open();
            Frame(ScopeKeys.Escape);
            f.Scope.Close();
            f.Scope.Open();
            Release(ScopeKeys.Escape);
            Frame();
            Assert.True(f.Lock);
            Assert.Equal(1, f.LockChanges);
            Assert.True(f.Scope.IsOpen);
        }

        [Fact]
        public void OpeningAnOpenScopeChangesNothing()
        {
            Owner below = DropDown(), top = DropDown();
            below.Scope.Open();
            top.Scope.Open();
            below.Scope.Open();
            Assert.Same(top.Scope, scopes.Top);
            Assert.Equal(2, scopes.OpenScopes.Count);
            Assert.Equal(1, below.LockChanges);
        }

        [Fact]
        public void ADroppedScopeLetsItsLockGoAtOnce()
        {
            Owner f = Field();
            f.Scope.Open();
            Frame(ScopeKeys.Escape);
            f.Scope.Drop();
            Assert.False(f.Lock);
            Assert.False(scopes.LockWanted);
        }

        [Fact]
        public void DroppingAReleasingScopeLetsItsLockGo()
        {
            Owner f = Field();
            f.Scope.Open();
            Frame(ScopeKeys.Escape);
            f.Scope.Close();
            f.Scope.Drop();
            Assert.False(f.Lock);
            Assert.False(scopes.LockWanted);
        }

        [Fact]
        public void AScopeWhoseOwnerIsGoneIsRemoved()
        {
            Owner below = DropDown(), top = DropDown();
            below.Scope.Open();
            top.Scope.Open();
            top.Gone = true;
            Frame(ScopeKeys.Down);
            Assert.False(top.Scope.IsOpen);
            Assert.False(top.Lock);
            Assert.Same(below.Scope, scopes.Top);
        }

        [Fact]
        public void AReleasingScopeWhoseOwnerIsGoneLetsItsLockGo()
        {
            Owner f = Field();
            f.Scope.Open();
            Frame(ScopeKeys.Escape);
            f.Scope.Close();
            f.Gone = true;
            Frame();
            Assert.False(f.Lock);
            Assert.False(scopes.LockWanted);
        }

        [Fact]
        public void AScopeBelowTheTopCanClose()
        {
            Owner bottom = DropDown(), middle = DropDown(), top = Field();
            bottom.Scope.Open();
            middle.Scope.Open();
            top.Scope.Open();
            middle.Scope.Close();
            Assert.Same(top.Scope, scopes.Top);
            Assert.Equal(new[] { bottom.Scope, top.Scope }, scopes.OpenScopes);
        }

        [Fact]
        public void NavigationIsOffWhileAScopeThatTakesItIsOpen()
        {
            Owner field = Field(), drop = DropDown();
            field.Scope.Open();
            Assert.False(scopes.NavigationOff);
            drop.Scope.Open();
            Assert.True(scopes.NavigationOff);
            drop.Scope.Close();
            Assert.False(scopes.NavigationOff);
        }

        [Fact]
        public void NestedScopesLeaveNoLockOnceAllClosed()
        {
            Owner drop = DropDown(), field = Field();
            drop.Scope.Open();
            Frame();
            field.Scope.Open();
            Frame(ScopeKeys.Escape);
            field.Scope.Close();
            Release(ScopeKeys.Escape);
            Assert.False(field.Lock);
            Assert.True(drop.Lock);
            Frame(ScopeKeys.Escape);
            Assert.False(drop.Scope.IsOpen);
            Assert.True(drop.Lock);
            Release(ScopeKeys.Escape);
            Assert.False(drop.Lock);
            Assert.False(scopes.LockWanted);
            Assert.Empty(scopes.OpenScopes);
        }

        [Fact]
        public void KeysKeptFromATopAreForgottenOnceNoScopeHoldsALock()
        {
            Owner first = DropDown();
            keyboard.Held = ScopeKeys.Down;
            first.Scope.Open();
            first.Scope.Drop();
            Assert.False(scopes.LockWanted);
            // the key came up while no scope was open: nothing reads it then
            keyboard.Held = ScopeKeys.None;
            Owner second = DropDown();
            second.Scope.Open();
            Frame(ScopeKeys.Down);
            AssertionExtensions.EqualSequence(new[] { ScopeKeys.Down }, second.Keys);
        }

        [Fact]
        public void ChangedIsCalledForEachChange()
        {
            int changes = 0;
            scopes.Changed += () => changes++;
            Owner f = Field();
            f.Scope.Open();
            Assert.Equal(1, changes);
            f.Scope.Close();
            Assert.Equal(2, changes);
            Frame();
            Assert.Equal(3, changes);
            Frame();
            Assert.Equal(3, changes);
        }
    }
}
