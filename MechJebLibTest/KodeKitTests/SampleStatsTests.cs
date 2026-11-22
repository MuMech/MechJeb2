/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>The values a graph's range or point reads off a series, without KSP or Unity.</summary>
    public class SampleStatsTests
    {
        private sealed class Samples : ISamples
        {
            private readonly double[] _x, _y;

            public Samples(double[] x, double[] y)
            {
                _x = x;
                _y = y;
            }

            public int Count => _x.Length;
            public double X(int i) => _x[i];
            public double Y(int i) => _y[i];
        }

        // y = 10, 30, 20, 50, 40 at x = 0, 1, 2, 3, 4
        private static Samples Zigzag() => new Samples(new double[] { 0, 1, 2, 3, 4 }, new double[] { 10, 30, 20, 50, 40 });

        [Fact]
        public void AValueBetweenTwoSamplesIsInterpolated()
        {
            Assert.True(SampleStats.ValueAt(Zigzag(), 0.5, out double y));
            Assert.Equal(20, y, 9);
            Assert.True(SampleStats.ValueAt(Zigzag(), 3, out y));
            Assert.Equal(50, y, 9);
        }

        [Fact]
        public void OutsideTheSamplesThereIsNoValue()
        {
            Assert.False(SampleStats.ValueAt(Zigzag(), -0.1, out double y));
            Assert.True(double.IsNaN(y));
            Assert.False(SampleStats.ValueAt(Zigzag(), 4.1, out _));
            Assert.False(SampleStats.ValueAt(new Samples(new double[0], new double[0]), 0, out _));
        }

        [Fact]
        public void ARangeGivesItsEndsChangeMinAndMax()
        {
            Assert.True(SampleStats.Over(Zigzag(), 0.5, 3.5, out RangeStats s));
            Assert.Equal(20, s.AtStart, 9);
            Assert.Equal(45, s.AtEnd, 9);
            Assert.Equal(25, s.Delta, 9);
            Assert.Equal(20, s.Min, 9);  // at 0.5 and at 2
            Assert.Equal(50, s.Max, 9);
            Assert.Equal(3, s.MaxAt, 9);
            Assert.Equal(3, s.Samples);
        }

        [Fact]
        public void TheInterpolatedEndsCountForMinAndMax()
        {
            // inside 0.2 .. 0.8 no sample: the ends are the extremes
            Assert.True(SampleStats.Over(Zigzag(), 0.8, 0.2, out RangeStats s));
            Assert.Equal(14, s.Min, 9);
            Assert.Equal(0.2, s.MinAt, 9);
            Assert.Equal(26, s.Max, 9);
            Assert.Equal(0, s.Samples);
        }

        [Fact]
        public void ARangeWithAnEndOutsideTheSamplesHasNoValues()
        {
            Assert.False(SampleStats.Over(Zigzag(), 3, 5, out _));
        }

        [Fact]
        public void XThatStepsBackIsReadOnTheFirstSegmentSpanningIt()
        {
            // a downrange that goes 0, 10, 5, 20
            var s = new Samples(new double[] { 0, 10, 5, 20 }, new double[] { 0, 100, 200, 300 });
            Assert.True(SampleStats.ValueAt(s, 7, out double y));
            Assert.Equal(70, y, 9);
        }

        [Fact]
        public void NaNSamplesAreSkippedByMinAndMax()
        {
            var s = new Samples(new double[] { 0, 1, 2, 3 }, new double[] { 1, double.NaN, 5, 2 });
            Assert.True(SampleStats.Over(s, 0, 3, out RangeStats st));
            Assert.Equal(1, st.Min, 9);
            Assert.Equal(5, st.Max, 9);
        }
    }
}
