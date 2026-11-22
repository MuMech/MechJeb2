/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's type-to-jump behaviour, without KSP or Unity.</summary>
    public class TypeaheadModelTests
    {
        private static readonly string[] Names = { "Apple", "Apricot", "Banana", "Blueberry", "Cherry", "avocado" };

        private static int Type(TypeaheadModel t, char c, double now, int current, string[] names = null, string[] keys = null)
        {
            names = names ?? Names;
            Func<int, string> key = keys == null ? (Func<int, string>)null : i => keys[i];
            return t.Type(c, now, names.Length, i => names[i], current, key);
        }

        [Fact]
        public void TheFirstEntryStartingWithTheLetterIsFocused()
        {
            Assert.Equal(2, Type(new TypeaheadModel(), 'b', 0, 0));
        }

        [Fact]
        public void MatchingIgnoresCase()
        {
            Assert.Equal(4, Type(new TypeaheadModel(), 'C', 0, 0));
        }

        [Fact]
        public void LettersTypedQuicklyMakeAPrefix()
        {
            var t = new TypeaheadModel();
            int at = Type(t, 'b', 0, 0);
            at = Type(t, 'l', 0.2, at);
            Assert.Equal(3, at);
            Assert.Equal("bl", t.Buffer);
        }

        [Fact]
        public void APauseLongerThanTheTimeoutStartsANewPrefix()
        {
            var t = new TypeaheadModel(1.0);
            int at = Type(t, 'b', 0, 0);
            at = Type(t, 'c', 1.5, at);
            Assert.Equal(4, at);
            Assert.Equal("c", t.Buffer);
        }

        [Fact]
        public void APauseOfExactlyTheTimeoutKeepsThePrefix()
        {
            var t = new TypeaheadModel(1.0);
            Type(t, 'b', 0, 0);
            Type(t, 'l', 1.0, 2);
            Assert.Equal("bl", t.Buffer);
        }

        [Fact]
        public void EachLetterRestartsTheTimeout()
        {
            var t = new TypeaheadModel(1.0);
            Type(t, 'b', 0, 0);
            Type(t, 'l', 0.9, 2);
            Type(t, 'u', 1.8, 3);
            Assert.Equal("blu", t.Buffer);
        }

        [Fact]
        public void TheSameLetterAgainStepsThroughTheEntriesThatStartWithIt()
        {
            var t = new TypeaheadModel();
            int at = Type(t, 'a', 0, -1);
            Assert.Equal(0, at);
            at = Type(t, 'a', 0.1, at);
            Assert.Equal(1, at);
            at = Type(t, 'a', 0.2, at);
            Assert.Equal(5, at);
            at = Type(t, 'a', 0.3, at);
            Assert.Equal(0, at); // wraps
        }

        [Fact]
        public void ALongerPrefixStaysOnTheEntryThatStillMatches()
        {
            var t = new TypeaheadModel();
            Type(t, 'a', 0, -1);
            Assert.Equal(1, Type(t, 'p', 0.1, 1));
            Assert.Equal(1, Type(t, 'r', 0.2, 1));
        }

        [Fact]
        public void ALongerPrefixMovesOnWhenTheCurrentEntryNoLongerMatches()
        {
            var t = new TypeaheadModel();
            Type(t, 'a', 0, -1);
            Type(t, 'p', 0.1, 0);
            Assert.Equal(1, Type(t, 'r', 0.2, 0));
        }

        [Fact]
        public void ALetterWithNoEntryMovesNothing()
        {
            Assert.Equal(-1, Type(new TypeaheadModel(), 'z', 0, 3));
        }

        [Fact]
        public void TheSearchWrapsAroundTheEnd()
        {
            Assert.Equal(0, Type(new TypeaheadModel(), 'a', 0, 5));
        }

        [Fact]
        public void ALeadingSpaceAndControlCharactersAreNotTypeahead()
        {
            var t = new TypeaheadModel();
            Assert.Equal(-1, Type(t, ' ', 0, 0));
            Assert.Equal(-1, Type(t, '\n', 0, 0));
            Assert.Equal(0, t.Length);
        }

        [Fact]
        public void ASpaceInsideAPrefixIsPartOfIt()
        {
            string[] names = { "Time to apoapsis", "Time", "Target" };
            var t = new TypeaheadModel();
            Type(t, 't', 0, -1, names);
            Type(t, 'i', 0.1, 0, names);
            Type(t, 'm', 0.2, 0, names);
            Type(t, 'e', 0.3, 0, names);
            Assert.Equal(0, Type(t, ' ', 0.4, 0, names));
        }

        [Fact]
        public void AnEntryMatchesByItsKeyToo()
        {
            string[] spanish = { "Tiempo para apoapsis", "Apoapsis" };
            string[] keys = { "Time to apoapsis", "Apoapsis" };
            var t = new TypeaheadModel();
            Type(t, 't', 0, 1, spanish, keys);
            Type(t, 'i', 0.1, 0, spanish, keys);
            Type(t, 'm', 0.2, 0, spanish, keys);
            Assert.Equal(0, Type(t, 'e', 0.3, 0, spanish, keys));
        }

        [Fact]
        public void AnEmptyListMatchesNothing()
        {
            Assert.Equal(-1, new TypeaheadModel().Type('a', 0, 0, i => "a", -1));
        }

        [Fact]
        public void AnEntryWithoutALabelIsSkipped()
        {
            string[] names = { null, "Alpha" };
            Assert.Equal(1, Type(new TypeaheadModel(), 'a', 0, -1, names));
        }

        [Fact]
        public void ResetForgetsTheTypedLetters()
        {
            var t = new TypeaheadModel();
            Type(t, 'b', 0, 0);
            t.Reset();
            Assert.Equal("", t.Buffer);
            Assert.Equal(4, Type(t, 'c', 0.1, 0));
        }

        [Fact]
        public void ATimeThatGoesBackwardsStartsANewPrefix()
        {
            var t = new TypeaheadModel();
            Type(t, 'b', 10, 0);
            Type(t, 'c', 2, 0);
            Assert.Equal("c", t.Buffer);
        }

        [Fact]
        public void TheBufferIsBounded()
        {
            var t = new TypeaheadModel();
            for (int i = 0; i < TypeaheadModel.MaxLength + 5; i++) Type(t, 'x', i * 0.01, 0);
            Assert.Equal(TypeaheadModel.MaxLength, t.Length);
        }

        [Fact]
        public void ATimeoutMustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TypeaheadModel(0));
        }

        [Fact]
        public void TypingAllocatesNothing()
        {
            var t = new TypeaheadModel();
            Func<int, string> label = i => Names[i];
            t.Type('b', 0, Names.Length, label, 0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int round = 0; round < 50; round++)
            {
                t.Type('b', round * 2.0, Names.Length, label, 0);
                t.Type('l', round * 2.0 + 0.1, Names.Length, label, 2);
            }
            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
        }
    }
}
