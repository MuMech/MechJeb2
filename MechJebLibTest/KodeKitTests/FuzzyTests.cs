/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable disable
using System.Collections.Generic;
using System.Linq;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>KodeKit's fuzzy search, without KSP or Unity.</summary>
    public class FuzzyTests
    {
        private static readonly string[] Labels =
        {
            "Time to apoapsis", "Apoapsis", "Periapsis", "Target apoapsis", "Orbital period", "Argument of periapsis", "Surface speed"
        };

        private static readonly string[] Keys =
        {
            "#MechJeb_TimeToApoapsis", "#MechJeb_Apoapsis", "#MechJeb_Periapsis", "#MechJeb_TargetApoapsis", "#MechJeb_OrbitalPeriod",
            "#MechJeb_ArgumentOfPeriapsis", "#MechJeb_SurfaceSpeed"
        };

        private static readonly string[] Spanish =
        {
            "Tiempo para apoapsis", "Apoapsis", "Periapsis", "Apoapsis del objetivo", "Periodo orbital", "Argumento del periapsis",
            "Velocidad en superficie"
        };

        private static int Score(string query, string text)
        {
            var m = new FuzzyMatcher { Query = query };
            return m.Score(text);
        }

        private static string[] Names(FuzzyFilter f) => f.Results.Select(r => Labels[r.Index]).ToArray();

        [Fact]
        public void ATextWithoutTheQueryLettersInOrderDoesNotMatch()
        {
            Assert.Equal(FuzzyMatcher.NoMatch, Score("zz", "Apoapsis"));
            Assert.Equal(FuzzyMatcher.NoMatch, Score("sa", "Apoapsis"));
        }

        [Fact]
        public void TheQueryIsAnySubsequenceOfTheText()
        {
            Assert.NotEqual(FuzzyMatcher.NoMatch, Score("aps", "Apoapsis"));
            Assert.NotEqual(FuzzyMatcher.NoMatch, Score("tta", "Time to apoapsis"));
        }

        [Fact]
        public void CaseDoesNotMatter()
        {
            Assert.Equal(Score("APO", "apoapsis"), Score("apo", "APOAPSIS"));
        }

        [Fact]
        public void SpacesInTheQueryAreIgnored()
        {
            Assert.Equal(Score("timetoapo", "Time to apoapsis"), Score("time to apo", "Time to apoapsis"));
        }

        [Fact]
        public void AnEmptyQueryMatchesEverythingWithScoreZero()
        {
            Assert.Equal(0, Score("", "anything"));
            Assert.Equal(0, Score("  ", "anything"));
            Assert.Equal(0, Score("", ""));
        }

        [Fact]
        public void ANullOrEmptyTextDoesNotMatchANonEmptyQuery()
        {
            Assert.Equal(FuzzyMatcher.NoMatch, Score("a", null));
            Assert.Equal(FuzzyMatcher.NoMatch, Score("a", ""));
        }

        [Fact]
        public void APrefixScoresAboveAWordStartAboveAScatteredMatch()
        {
            int prefix = Score("apo", "Apoapsis");
            int wordStart = Score("apo", "Time to apoapsis");
            int scattered = Score("apo", "Argument of periapsis");
            Assert.True(prefix > wordStart, $"{prefix} > {wordStart}");
            Assert.True(wordStart > scattered, $"{wordStart} > {scattered}");
        }

        [Fact]
        public void ACamelCaseBoundaryStartsAWord()
        {
            Assert.True(Score("apo", "TimeToApoapsis") > Score("apo", "Timetoapoapsis"));
        }

        [Fact]
        public void TheBestAlignmentWinsNotTheFirst()
        {
            // the first a starts the text but is far from the first b; the later a and b are a word start and its neighbour
            var ranges = new List<TextRange>();
            var m = new FuzzyMatcher { Query = "ab" };
            m.Score("a-----------b ab", ranges);
            Assert.Equal(new[] { new TextRange(14, 2) }, ranges.ToArray());
        }

        [Fact]
        public void ConsecutiveMatchedCharactersMakeOneRange()
        {
            var ranges = new List<TextRange>();
            var m = new FuzzyMatcher { Query = "apo" };
            Assert.NotEqual(FuzzyMatcher.NoMatch, m.Score("Time to apoapsis", ranges));
            Assert.Equal(new[] { new TextRange(8, 3) }, ranges.ToArray());
        }

        [Fact]
        public void ScatteredMatchesGiveOneRangePerRun()
        {
            var ranges = new List<TextRange>();
            var m = new FuzzyMatcher { Query = "tta" };
            m.Score("Time to apoapsis", ranges);
            Assert.Equal(new[] { new TextRange(0, 1), new TextRange(5, 1), new TextRange(8, 1) }, ranges.ToArray());
        }

        [Fact]
        public void RangesAreEmptyWhenNothingMatches()
        {
            var ranges = new List<TextRange> { new TextRange(1, 1) };
            var m = new FuzzyMatcher { Query = "zz" };
            Assert.Equal(FuzzyMatcher.NoMatch, m.Score("Apoapsis", ranges));
            Assert.Empty(ranges);
        }

        [Fact]
        public void TheQueryCanChangeBetweenTexts()
        {
            var m = new FuzzyMatcher { Query = "apo" };
            int a = m.Score("Apoapsis");
            m.Query = "peri";
            Assert.Equal(FuzzyMatcher.NoMatch, m.Score("Apoapsis"));
            m.Query = "apo";
            Assert.Equal(a, m.Score("Apoapsis"));
        }

        [Fact]
        public void AQueryLongerThanTheTextDoesNotMatch()
        {
            Assert.Equal(FuzzyMatcher.NoMatch, Score("apoapsisx", "Apoapsis"));
        }

        [Fact]
        public void TextsOfVeryDifferentLengthsScoreInTheSameBuffers()
        {
            string longText = string.Concat(Enumerable.Repeat("abcdefghij ", 40));
            Assert.NotEqual(FuzzyMatcher.NoMatch, Score("jj", longText));
            Assert.NotEqual(FuzzyMatcher.NoMatch, Score("ab", "ab"));
        }

        [Fact]
        public void AnEmptyQueryListsEveryItemInItsOwnOrder()
        {
            var f = new FuzzyFilter(Labels);
            Assert.Equal(Labels, Names(f));
        }

        [Fact]
        public void ApoRanksTheApoapsisItemsFirstAndHidesTheNonMatches()
        {
            var f = new FuzzyFilter(Labels);
            f.Apply("apo");
            string[] names = Names(f);
            Assert.Equal("Apoapsis", names[0]);
            // the two other word-start matches score the same: the shorter text first
            Assert.Equal("Target apoapsis", names[1]);
            Assert.Equal("Time to apoapsis", names[2]);
            // a scattered subsequence ("Orbital period": a, p, o) still matches, last
            Assert.Equal("Orbital period", names[names.Length - 1]);
            Assert.DoesNotContain("Surface speed", names);
        }

        [Fact]
        public void ClearingTheQueryBringsEveryItemBackInItsOwnOrder()
        {
            var f = new FuzzyFilter(Labels);
            f.Apply("apo");
            f.Apply("");
            Assert.Equal(Labels, Names(f));
        }

        [Fact]
        public void TheSameQueryAppliedAgainChangesNothing()
        {
            var f = new FuzzyFilter(Labels);
            Assert.True(f.Apply("apo"));
            Assert.False(f.Apply("apo"));
            Assert.True(f.Apply("ap"));
        }

        [Fact]
        public void EqualScoresRankTheShorterTextThenTheItemsOwnOrder()
        {
            var f = new FuzzyFilter(new[] { "Speed b", "Speed a", "Speed" });
            f.Apply("speed");
            Assert.Equal(new[] { 2, 0, 1 }, f.Results.Select(r => r.Index).ToArray());
        }

        [Fact]
        public void TheOrderDoesNotDependOnTheQueriesAppliedBefore()
        {
            var a = new FuzzyFilter(Labels);
            var b = new FuzzyFilter(Labels);
            a.Apply("p");
            a.Apply("ap");
            b.Apply("ap");
            Assert.Equal(b.Results.Select(r => r.Index), a.Results.Select(r => r.Index));
        }

        [Fact]
        public void AnItemMatchesByItsKeyWhenItsLocalizedLabelDoesNot()
        {
            var f = new FuzzyFilter(Spanish, Keys);
            f.Apply("time to apo");
            Assert.Equal(0, f.Results[0].Index);
            Assert.True(f.Results[0].ByKey);
        }

        [Fact]
        public void TheLocalizedLabelStillMatchesWhenKeysAreGiven()
        {
            var f = new FuzzyFilter(Spanish, Keys);
            f.Apply("tiempo");
            Assert.Equal(0, f.Results[0].Index);
            Assert.False(f.Results[0].ByKey);
        }

        [Fact]
        public void ATokenKeepsEverySegmentAfterThePrefix()
        {
            Assert.Equal("Altitude_ASL", FuzzyKeys.FromToken("#MechJeb_Altitude_ASL"));
            Assert.Equal("TimeToApoapsis", FuzzyKeys.FromToken("#MechJeb_TimeToApoapsis"));
            Assert.Equal("Other_Token", FuzzyKeys.FromToken("Other_Token"));
            Assert.Null(FuzzyKeys.FromToken(null));
        }

        [Fact]
        public void AnItemIsFoundByAnInnerSegmentOfItsToken()
        {
            var f = new FuzzyFilter(new[] { "Altitude (ASL)", "Speed" }, new[] { FuzzyKeys.FromToken("#MechJeb_Altitude_ASL"), FuzzyKeys.FromToken("#MechJeb_Speed") });
            f.Apply("asl");
            Assert.Equal(new[] { 0 }, f.Results.Select(r => r.Index).ToArray());
            var g = new FuzzyFilter(new[] { "海拔" }, new[] { FuzzyKeys.FromToken("#MechJeb_Altitude_ASL") });
            g.Apply("asl");
            Assert.Equal(0, g.Results[0].Index);
            Assert.True(g.Results[0].ByKey);
        }

        [Fact]
        public void ChineseLabelsMatchAChineseQueryAndTheEnglishKey()
        {
            string[] labels = { "远地点高度", "近地点高度", "轨道速度" };
            string[] keys = { "Apoapsis", "Periapsis", "OrbitalSpeed" };
            var f = new FuzzyFilter(labels, keys);
            f.Apply("地点"); // the middle of the first two
            Assert.Equal(new[] { 0, 1 }, f.Results.Select(r => r.Index).OrderBy(i => i).ToArray());
            Assert.False(f.Results[0].ByKey);
            f.Apply("轨道");
            Assert.Equal(new[] { 2 }, f.Results.Select(r => r.Index).ToArray());
            f.Apply("apo");
            Assert.Equal(new[] { 0 }, f.Results.Select(r => r.Index).ToArray());
            Assert.True(f.Results[0].ByKey);
            var ranges = new List<TextRange>();
            f.Apply("地点");
            f.Highlight(f.Results[0], ranges);
            Assert.Equal(new[] { new TextRange(1, 2) }, ranges.ToArray());
        }

        [Fact]
        public void TheLabelWinsATieWithTheKey()
        {
            var f = new FuzzyFilter(new[] { "Apoapsis" }, new[] { "Apoapsis" });
            f.Apply("apo");
            Assert.False(f.Results[0].ByKey);
        }

        [Fact]
        public void ItemsWithoutAKeyOrWithoutALabelAreHandled()
        {
            var f = new FuzzyFilter(new[] { "Apoapsis", null, "" }, new[] { null, "apo", null });
            f.Apply("apo");
            Assert.Equal(new[] { 0, 1 }, f.Results.Select(r => r.Index).ToArray());
            f.Apply("");
            Assert.Equal(3, f.Results.Count);
        }

        [Fact]
        public void HighlightGivesTheMatchedCharactersOfTheLabel()
        {
            var f = new FuzzyFilter(Labels);
            f.Apply("apo");
            var ranges = new List<TextRange>();
            f.Highlight(f.Results.First(r => Labels[r.Index] == "Time to apoapsis"), ranges);
            Assert.Equal(new[] { new TextRange(8, 3) }, ranges.ToArray());
        }

        [Fact]
        public void HighlightIsEmptyForAKeyMatchAndForAnEmptyQuery()
        {
            var f = new FuzzyFilter(new[] { "Tiempo" }, new[] { "Time" });
            var ranges = new List<TextRange> { new TextRange(0, 1) };
            f.Apply("time");
            f.Highlight(f.Results[0], ranges);
            Assert.Empty(ranges);
            f.Apply("");
            ranges.Add(new TextRange(0, 1));
            f.Highlight(f.Results[0], ranges);
            Assert.Empty(ranges);
        }

        [Fact]
        public void KeysMustBeOnePerLabel()
        {
            Assert.Throws<System.ArgumentException>(() => new FuzzyFilter(new[] { "a", "b" }, new[] { "a" }));
        }

        [Fact]
        public void ApplyingQueriesAllocatesNothingOnceTheBuffersAreGrown()
        {
            var f = new FuzzyFilter(Labels, Keys);
            string[] queries = { "apo", "apoa", "periap", "time to apo", "", "s", "apox" };
            foreach (string q in queries) f.Apply(q); // grows the buffers
            AssertionExtensions.ShouldNotAllocate(() =>
            {
                for (int round = 0; round < 20; round++)
                    foreach (string q in queries)
                        f.Apply(q);
            });
        }
    }
}
