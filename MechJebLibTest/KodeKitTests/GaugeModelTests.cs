/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The gauge (<see cref="GaugeModel"/>): HTML's meter rules.</summary>
    public class GaugeModelTests
    {
        // a throttle in percent: 0..100, low under 40, high over 80, the optimum between them
        private sealed class Throttle
        {
            public double Value = 60, Min = 0, Max = 100, Low = 40, High = 80, Optimum = 60;
            public readonly GaugeModel M;

            public Throttle(bool bounds = true, bool optimum = true) =>
                M = new GaugeModel(() => Value, () => Min, () => Max, bounds ? () => Low : (System.Func<double>?)null,
                    bounds ? () => High : (System.Func<double>?)null, optimum ? () => Optimum : (System.Func<double>?)null,
                    (b, v) => b.Append(v, 0).Append(" %")) { Label = "Throttle" };
        }

        [Fact]
        public void AScaleOnlyChangeIsAChange()
        {
            // the value at the bottom of the scale and the bounds fixed: only the max moves (an acceleration's max on staging),
            // which moves the marks
            var t = new Throttle { Value = 0 };
            t.M.Sync();
            t.Max = 200;
            Assert.True(t.M.Sync());
            Assert.False(t.M.Sync());
        }

        [Fact]
        public void TheFractionIsTheValuesPlaceOnTheScale()
        {
            var t = new Throttle { Value = 25 };
            t.M.Sync();
            Assert.Equal(0.25, t.M.Fraction, 9);
        }

        [Fact]
        public void AValueOutsideTheScaleIsClampedForDrawingAndKeptInTheText()
        {
            var t = new Throttle { Value = 130 };
            t.M.Sync();
            Assert.Equal(1, t.M.Fraction);
            Assert.True(t.M.OffScale);
            Assert.Equal(130, t.M.Value);
            var b = new CharBuffer();
            t.M.FormatValue(b);
            Assert.Equal("130 %", b.ToString());
            Assert.Contains("130 %", t.M.Describe());
            Assert.Contains("off the scale", t.M.Describe());
        }

        [Fact]
        public void AValueBelowTheScaleDrawsEmptyAndIsInTheLowZone()
        {
            var t = new Throttle { Value = -10 };
            t.M.Sync();
            Assert.Equal(0, t.M.Fraction);
            Assert.Equal(GaugeZone.Low, t.M.Zone);
        }

        [Fact]
        public void ANaNValueDrawsEmptyAndSaysNotAvailable()
        {
            var t = new Throttle { Value = double.NaN };
            t.M.Sync();
            Assert.False(t.M.Available);
            Assert.Equal(0, t.M.Fraction);
            Assert.Equal(GaugeRating.Plain, t.M.Rating);
            Assert.Equal("Throttle: not available", t.M.Describe());
            var b = new CharBuffer();
            b.Append("x");
            t.M.FormatValue(b);
            Assert.Equal(0, b.Length);
        }

        [Fact]
        public void AnInfiniteValueIsNotAvailable()
        {
            var t = new Throttle { Value = double.PositiveInfinity };
            t.M.Sync();
            Assert.False(t.M.Available);
        }

        [Fact]
        public void AScaleWhoseMaxIsNotAboveItsMinIsNotAvailable()
        {
            var t = new Throttle { Max = 0 };
            t.M.Sync();
            Assert.False(t.M.HasScale);
            Assert.False(t.M.Available);
            Assert.Equal(0, t.M.Fraction);
            Assert.Contains("not available", t.M.Describe());
        }

        [Fact]
        public void ANaNBoundOfTheScaleIsNotAvailable()
        {
            var t = new Throttle { Min = double.NaN };
            t.M.Sync();
            Assert.False(t.M.Available);
        }

        [Fact]
        public void WithoutDelegatesTheScaleIsZeroToOne()
        {
            double v = 0.3;
            var m = new GaugeModel(() => v);
            Assert.Equal(0, m.Min);
            Assert.Equal(1, m.Max);
            Assert.Equal(0.3, m.Fraction, 9);
        }

        [Fact]
        public void TheZoneIsLowBelowTheLowBoundHighAboveTheHighBoundAndMidBetween()
        {
            var t = new Throttle { Value = 30 };
            t.M.Sync();
            Assert.Equal(GaugeZone.Low, t.M.Zone);
            t.Value = 60;
            t.M.Sync();
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
            t.Value = 90;
            t.M.Sync();
            Assert.Equal(GaugeZone.High, t.M.Zone);
        }

        [Fact]
        public void TheBoundsThemselvesAreInTheMidZone()
        {
            var t = new Throttle { Value = 40 };
            t.M.Sync();
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
            t.Value = 80;
            t.M.Sync();
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
        }

        [Fact]
        public void AFloatValueAtADoubleBoundIsAtIt()
        {
            // a vessel's throttle (a float) held at "Limit throttle to" 80 % (a double): 0.8f is 0.800000011920929
            float throttle = 0.8f;
            double limit = 0.8;
            var m = new GaugeModel(() => throttle, null, null, null, () => limit, () => 0.4);
            Assert.Equal(GaugeZone.Mid, m.Zone);
            Assert.Equal(GaugeRating.Good, m.Rating);
        }

        [Fact]
        public void AValueWithinTheTolerancePastABoundIsAtIt()
        {
            var t = new Throttle { Value = 80.4 };
            t.M.Tolerance = 0.005;
            t.M.Sync();
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
            t.Value = 80.6;
            t.M.Sync();
            Assert.Equal(GaugeZone.High, t.M.Zone);
            t.Value = 39.6;
            t.M.Sync();
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
            t.Value = 39.4;
            t.M.Sync();
            Assert.Equal(GaugeZone.Low, t.M.Zone);
        }

        [Fact]
        public void AnOptimumBetweenTheBoundsMakesMidGoodAndTheEndsFair()
        {
            var t = new Throttle { Value = 60 };
            t.M.Sync();
            Assert.Equal(GaugeRating.Good, t.M.Rating);
            t.Value = 20;
            t.M.Sync();
            Assert.Equal(GaugeRating.Fair, t.M.Rating);
            t.Value = 95;
            t.M.Sync();
            Assert.Equal(GaugeRating.Fair, t.M.Rating);
        }

        [Fact]
        public void AnOptimumBelowTheLowBoundMakesLowGoodMidFairAndHighPoor()
        {
            var t = new Throttle { Optimum = 10 };
            t.Value = 20;
            t.M.Sync();
            Assert.Equal(GaugeRating.Good, t.M.Rating);
            t.Value = 60;
            t.M.Sync();
            Assert.Equal(GaugeRating.Fair, t.M.Rating);
            t.Value = 90;
            t.M.Sync();
            Assert.Equal(GaugeRating.Poor, t.M.Rating);
        }

        [Fact]
        public void AnOptimumAboveTheHighBoundMakesHighGoodMidFairAndLowPoor()
        {
            var t = new Throttle { Optimum = 95 };
            t.Value = 90;
            t.M.Sync();
            Assert.Equal(GaugeRating.Good, t.M.Rating);
            t.Value = 60;
            t.M.Sync();
            Assert.Equal(GaugeRating.Fair, t.M.Rating);
            t.Value = 20;
            t.M.Sync();
            Assert.Equal(GaugeRating.Poor, t.M.Rating);
        }

        [Fact]
        public void WithoutAnOptimumTheRatingIsPlainButTheZonesStay()
        {
            var t = new Throttle(optimum: false) { Value = 90 };
            t.M.Sync();
            Assert.Equal(GaugeRating.Plain, t.M.Rating);
            Assert.Equal(GaugeZone.High, t.M.Zone);
        }

        [Fact]
        public void AMissingLowBoundIsTheMinAndAMissingHighBoundTheMax()
        {
            var t = new Throttle(false, false) { Value = 60 };
            t.M.Sync();
            Assert.False(t.M.HasLow);
            Assert.False(t.M.HasHigh);
            Assert.Equal(0, t.M.Low);
            Assert.Equal(100, t.M.High);
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
        }

        [Fact]
        public void ANaNBoundCountsAsMissing()
        {
            var t = new Throttle { Low = double.NaN, Value = 10 };
            t.M.Sync();
            Assert.False(t.M.HasLow);
            Assert.Equal(0, t.M.Low);
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
        }

        [Fact]
        public void BoundsAreClampedIntoTheScale()
        {
            var t = new Throttle { Low = -20, High = 150, Optimum = 300 };
            t.M.Sync();
            Assert.Equal(0, t.M.Low);
            Assert.Equal(100, t.M.High);
            Assert.Equal(100, t.M.Optimum);
        }

        [Fact]
        public void ALowBoundAboveTheHighBoundDegradesToAPlainBar()
        {
            var t = new Throttle { Low = 80, High = 40, Value = 90 };
            t.M.Sync();
            Assert.False(t.M.Zoned);
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
            Assert.Equal(GaugeRating.Plain, t.M.Rating);
            Assert.Equal(0.9, t.M.Fraction, 9);
            Assert.DoesNotContain("high over", t.M.Describe());
        }

        [Fact]
        public void BoundsAreReadAgainAtEachSync()
        {
            var t = new Throttle { Value = 70 };
            t.M.Sync();
            Assert.Equal(GaugeZone.Mid, t.M.Zone);
            t.High = 50;
            Assert.True(t.M.Sync());
            Assert.Equal(GaugeZone.High, t.M.Zone);
        }

        [Fact]
        public void SyncReportsNoChangeWhenNothingMoved()
        {
            var t = new Throttle();
            t.M.Sync();
            Assert.False(t.M.Sync());
        }

        [Fact]
        public void SyncReportsAChangedValueEvenBelowTheResolution()
        {
            // the text shows it
            var t = new Throttle();
            t.M.Sync();
            t.Value = 60.00001;
            Assert.True(t.M.Sync());
        }

        [Fact]
        public void SyncReportsAValueBecomingNotAvailable()
        {
            var t = new Throttle();
            t.M.Sync();
            t.Value = double.NaN;
            Assert.True(t.M.Sync());
            Assert.False(t.M.Sync());
        }

        [Fact]
        public void ABoundRemovedIsAChange()
        {
            var t = new Throttle { Value = 60 };
            t.M.Sync();
            t.Low = double.NaN;
            Assert.True(t.M.Sync());
        }

        [Fact]
        public void TheSentenceSaysTheValueTheScaleTheBoundsAndTheZone()
        {
            var t = new Throttle { Value = 90 };
            t.M.Sync();
            Assert.Equal("Throttle: 90 % on 0 % to 100 %, low under 40 %, high over 80 %: high, fair", t.M.Describe());
        }

        [Fact]
        public void APlainGaugeSaysNoZone()
        {
            var t = new Throttle(false, false) { Value = 60 };
            t.M.Sync();
            Assert.Equal("Throttle: 60 % on 0 % to 100 %", t.M.Describe());
        }

        [Fact]
        public void AGaugeWithABoundAndNoOptimumSaysItsZoneWithoutARating()
        {
            var t = new Throttle(optimum: false) { Value = 90 };
            t.M.Sync();
            Assert.EndsWith(": high", t.M.Describe());
        }

        [Fact]
        public void TheSentenceUsesTheWordsGiven()
        {
            var t = new Throttle { Value = 90 };
            t.M.Sync();
            var words = new GaugeWords
            {
                Reading = "<<1>> = <<2>>", Scale = "[<<1>>, <<2>>]", LowBound = "L <<1>>", HighBound = "H <<1>>", ZoneRated = "<<2>> (<<1>>)",
                High = "haut", Fair = "moyen"
            };
            Assert.Equal("Throttle = 90 % [0 %, 100 %], L 40 %, H 80 %: moyen (haut)", t.M.Describe(words));
        }

        [Fact]
        public void TheSentencesNumbersGoThroughTheFormatNotTheCulture()
        {
            System.Globalization.CultureInfo old = System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
                double v = 0.5;
                var m = new GaugeModel(() => v, null, null, null, null, null, (b, x) => b.Append(x, 2)) { Label = "Ratio" };
                Assert.StartsWith("Ratio: 0.50 on 0.00 to 1.00", m.Describe());
            }
            finally
            {
                System.Globalization.CultureInfo.CurrentCulture = old;
            }
        }

        [Fact]
        public void FillLeavesAPlaceholderWithoutAnArgument()
        {
            Assert.Equal("a x <<2>>", GaugeWords.Fill("a <<1>> <<2>>", "x"));
        }

        [Fact]
        public void TheMeterIsOneLabelledPartThatTakesNoFocus()
        {
            var t = new Throttle();
            var parts = new List<PartDescription>();
            t.M.Describe(parts);
            Assert.Single(parts);
            Assert.Equal(PartRole.Meter, parts[0].Role);
            Assert.Equal("Throttle", parts[0].Label);
            Assert.False(parts[0].Focusable);
            Assert.Empty(Patterns.Check(t.M));
        }

        [Fact]
        public void AMeterWithNoValueIsInvalid()
        {
            var t = new Throttle { Value = double.NaN };
            t.M.Sync();
            var parts = new List<PartDescription>();
            t.M.Describe(parts);
            Assert.True(parts[0].Has(PartFlags.Invalid));
        }

        [Fact]
        public void AMeterWithoutALabelFailsThePatternChecks()
        {
            double v = 0.5;
            Assert.NotEmpty(Patterns.Check(new GaugeModel(() => v)));
        }

        [Fact]
        public void SyncAndTheValuesTextAllocateNothing()
        {
            var t = new Throttle();
            var b = new CharBuffer(32);
            t.M.Sync();
            t.M.FormatValue(b);
            AssertionExtensions.ShouldNotAllocate(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    t.Value = i % 101;
                    t.M.Sync();
                    t.M.FormatValue(b);
                }
                t.Value = 60;
                t.M.Sync();
            });
        }
    }
}
