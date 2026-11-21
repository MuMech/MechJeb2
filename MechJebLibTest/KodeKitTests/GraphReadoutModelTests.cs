/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using KodeKit.Headless;
using Xunit;

namespace MechJebLibTest.KodeKitTests
{
    /// <summary>A graph's hover read-out (the values under the pointer), without KSP or Unity.</summary>
    public class GraphReadoutModelTests
    {
        private sealed class Samples : ISamples
        {
            public double[] Xs, Ys;

            public Samples(double[] x, double[] y)
            {
                Xs = x;
                Ys = y;
            }

            public int Count => Xs.Length;
            public double X(int i) => Xs[i];
            public double Y(int i) => Ys[i];
        }

        // y = 10, 30, 20, 50, 40 at x = 0, 10, 20, 30, 40
        private static Samples Zigzag() => new Samples(new double[] { 0, 10, 20, 30, 40 }, new double[] { 10, 30, 20, 50, 40 });

        // data from 0 to 40 shown whole, one row on the zigzag
        private static GraphReadoutModel Readout(out ViewportModel view, Samples s = null)
        {
            view = new ViewportModel(0, 40);
            var m = new GraphReadoutModel(view);
            m.Add(s ?? Zigzag(), "Altitude");
            return m;
        }

        [Fact]
        public void ThePointerReadsTheXUnderItThroughTheView()
        {
            GraphReadoutModel m = Readout(out _);
            Assert.True(m.Move(0.25));
            Assert.True(m.Active);
            Assert.Equal(10, m.X, 9);
            Assert.Equal(30, m.Value(0), 9);
        }

        [Fact]
        public void AZoomedAndPannedViewMapsThePointerToItsWindow()
        {
            GraphReadoutModel m = Readout(out ViewportModel view);
            view.Show(20, 30, ChangeSource.Pointer);
            m.Move(0.5);
            Assert.Equal(25, m.X, 9);
            Assert.Equal(35, m.Value(0), 9);
            view.PanBy(-0.5, ChangeSource.Pointer); // 15 to 25
            Assert.True(m.Sync());
            Assert.Equal(20, m.X, 9);
            Assert.Equal(20, m.Value(0), 9);
        }

        [Fact]
        public void AValueBetweenTwoSamplesIsOnTheStraightLineBetweenThem()
        {
            GraphReadoutModel m = Readout(out _);
            m.Move(0.125 + 0.0625); // x = 7.5
            Assert.Equal(7.5, m.X, 9);
            Assert.Equal(25, m.Value(0), 9);
        }

        [Fact]
        public void OnASampleTheValueIsTheSampleItself()
        {
            // a hit on a sample returns its value bit for bit, not a value interpolated towards it
            var s = new Samples(new double[] { 0, 0.1, 0.3 }, new double[] { 1, 0.1 + 0.2, 7 });
            Assert.True(SampleStats.ValueAtSorted(s, 0.1, out double y));
            Assert.Equal(0.1 + 0.2, y);
            Assert.True(SampleStats.ValueAtSorted(s, 0.3, out y));
            Assert.Equal(7, y);
        }

        [Fact]
        public void OutsideASeriesSamplesItHasNoValue()
        {
            var s = new Samples(new double[] { 10, 20 }, new double[] { 1, 2 });
            GraphReadoutModel m = Readout(out _, s);
            m.Move(0.1); // x = 4, before the first sample
            Assert.True(m.Active);
            Assert.False(m.HasValue(0));
            Assert.True(double.IsNaN(m.Value(0)));
            m.Move(0.9); // x = 36, past the last
            Assert.False(m.HasValue(0));
        }

        [Fact]
        public void ANaNSampleNextToTheXIsAGapWithNoValue()
        {
            var s = new Samples(new double[] { 0, 10, 20, 30 }, new double[] { 1, double.NaN, 3, 4 });
            GraphReadoutModel m = Readout(out _, s);
            m.Move(0.125); // x = 5: between 1 and NaN
            Assert.False(m.HasValue(0));
            m.Move(0.25); // on the NaN sample
            Assert.False(m.HasValue(0));
            m.Move(0.625); // x = 25: between 3 and 4
            Assert.Equal(3.5, m.Value(0), 9);
        }

        [Fact]
        public void AnEmptySeriesHasNoValueAndTheOthersStillRead()
        {
            var view = new ViewportModel(0, 40);
            var m = new GraphReadoutModel(view);
            m.Add(new Samples(new double[0], new double[0]), "Empty");
            m.Add(Zigzag(), "Zigzag");
            m.Move(0.5);
            Assert.False(m.HasValue(0));
            Assert.Equal(20, m.Value(1), 9);
        }

        [Fact]
        public void TheEdgesOfTheDataAreTheirSamples()
        {
            GraphReadoutModel m = Readout(out _);
            m.Move(0);
            Assert.Equal(10, m.Value(0), 9);
            m.Move(1);
            Assert.Equal(40, m.Value(0), 9);
        }

        [Fact]
        public void APointerOffThePlotReadsNothing()
        {
            GraphReadoutModel m = Readout(out _);
            m.Move(1.2);
            Assert.False(m.Active);
            Assert.True(double.IsNaN(m.X));
            Assert.False(m.HasValue(0));
        }

        [Fact]
        public void LeavingThePlotEndsTheReadout()
        {
            GraphReadoutModel m = Readout(out _);
            m.Move(0.5);
            Assert.True(m.Leave());
            Assert.False(m.Active);
            Assert.False(m.Hovering);
            Assert.Equal(0, m.Text.Length);
        }

        [Fact]
        public void AButtonHeldOnThePlotHidesTheReadoutUntilItsRelease()
        {
            GraphReadoutModel m = Readout(out _);
            m.Move(0.5);
            Assert.True(m.Press(true));
            Assert.False(m.Active);
            Assert.True(m.Hovering);
            m.Move(0.25); // a drag moves the pointer: still nothing
            Assert.False(m.Active);
            Assert.True(m.Press(false));
            Assert.True(m.Active);
            Assert.Equal(10, m.X, 9);
        }

        [Fact]
        public void AHiddenSeriesIsLeftOutOfTheReadout()
        {
            bool shown = true;
            var view = new ViewportModel(0, 40);
            var m = new GraphReadoutModel(view);
            m.Add(Zigzag(), "Zigzag", null, () => shown);
            m.Add(Zigzag(), "Other");
            m.Move(0.5);
            Assert.Equal(2, m.ShownCount);
            int shape = m.ShapeVersion;
            shown = false;
            Assert.True(m.Refresh());
            Assert.False(m.Shown(0));
            Assert.False(m.HasValue(0));
            Assert.Equal(1, m.ShownCount);
            Assert.NotEqual(shape, m.ShapeVersion);
            string text = m.Text.ToString();
            Assert.Contains("\nOther ", text);
            Assert.DoesNotContain("Zigzag", text);
        }

        [Fact]
        public void TheTextHoldsTheXThenEachShownRow()
        {
            var view = new ViewportModel(0, 40);
            var m = new GraphReadoutModel(view);
            m.SetX(() => "Time", (b, x) => b.Append(x, 1).Append(" s"));
            m.Add(Zigzag(), "Altitude", (b, v) => b.Append(v, 0).Append(" m"));
            m.Add(new Samples(new double[] { 30, 40 }, new double[] { 1, 2 }), "Late");
            m.Move(0.25);
            Assert.Equal("Time 10.0 s\nAltitude 30 m\nLate —", m.Text.ToString());
        }

        [Fact]
        public void AStillPointerOverStillDataChangesNothing()
        {
            GraphReadoutModel m = Readout(out _);
            m.Move(0.5);
            int version = m.Version, shape = m.ShapeVersion;
            Assert.False(m.Move(0.5));
            Assert.False(m.Refresh());
            Assert.False(m.Sync());
            Assert.Equal(version, m.Version);
            Assert.Equal(shape, m.ShapeVersion);
        }

        [Fact]
        public void ANewSampleUnderAStillPointerChangesTheValueButNotTheShape()
        {
            var s = new Samples(new double[] { 0, 10 }, new double[] { 0, 10 });
            GraphReadoutModel m = Readout(out ViewportModel view, s);
            m.Move(0.5); // x = 20: past the data
            Assert.False(m.HasValue(0));
            int shape = m.ShapeVersion;
            s.Xs = new double[] { 0, 10, 30 };
            s.Ys = new double[] { 0, 10, 30 };
            Assert.True(m.Refresh());
            Assert.Equal(20, m.Value(0), 9);
            Assert.Equal(shape, m.ShapeVersion);
        }

        [Fact]
        public void TheViewMovingUnderAStillPointerReadsTheNewX()
        {
            GraphReadoutModel m = Readout(out ViewportModel view);
            m.Move(0.5);
            view.Wheel(1, 0.25, ChangeSource.Pointer); // zoom in about x = 10
            Assert.True(m.Sync());
            Assert.Equal(view.FromFraction(0.5), m.X, 9);
        }

        [Fact]
        public void AFallingXIsReadAsWellAsARisingOne()
        {
            var s = new Samples(new double[] { 40, 30, 20, 10, 0 }, new double[] { 40, 50, 20, 30, 10 });
            Assert.True(SampleStats.ValueAtSorted(s, 25, out double y));
            Assert.Equal(35, y, 9);
            Assert.True(SampleStats.ValueAtSorted(s, 40, out y));
            Assert.Equal(40, y, 9);
            Assert.True(SampleStats.ValueAtSorted(s, 0, out y));
            Assert.Equal(10, y, 9);
        }

        [Fact]
        public void AnXThatStepsBackIsStillReadOnASegmentThatSpansIt()
        {
            // 0, 10, 8, 20: x = 9 lies on two segments; either gives a value on the line
            var s = new Samples(new double[] { 0, 10, 8, 20 }, new double[] { 0, 10, 8, 20 });
            Assert.True(SampleStats.ValueAtSorted(s, 9, out double y));
            Assert.Equal(9, y, 9);
            // the ends don't bracket 15 here: the scan answers
            var back = new Samples(new double[] { 0, 20, 10 }, new double[] { 0, 20, 10 });
            Assert.True(SampleStats.ValueAtSorted(back, 15, out y));
            Assert.Equal(15, y, 9);
        }

        [Fact]
        public void TheBisectionAgreesWithTheScanOnALongSeries()
        {
            int n = 5000;
            var xs = new double[n];
            var ys = new double[n];
            for (int i = 0; i < n; i++)
            {
                xs[i] = i * 0.2;
                ys[i] = System.Math.Sin(i * 0.01) * 1000;
            }

            var s = new Samples(xs, ys);
            for (double x = 0; x < 999; x += 3.7)
            {
                Assert.True(SampleStats.ValueAt(s, x, out double a));
                Assert.True(SampleStats.ValueAtSorted(s, x, out double b));
                Assert.Equal(a, b, 9);
            }
        }

        [Fact]
        public void SamplesThatShareAnXGiveTheFirstOnThatX()
        {
            // a phase boundary: two samples at x = 10
            var s = new Samples(new double[] { 0, 10, 10, 20 }, new double[] { 0, 5, 50, 60 });
            Assert.True(SampleStats.ValueAtSorted(s, 10, out double y));
            Assert.Equal(5, y, 9);
            Assert.True(SampleStats.ValueAtSorted(s, 15, out y));
            Assert.Equal(55, y, 9);
        }

        [Fact]
        public void MovingThePointerDoesNotAllocate()
        {
            int n = 20000;
            var xs = new double[n];
            var ys = new double[n];
            for (int i = 0; i < n; i++)
            {
                xs[i] = i;
                ys[i] = i * 2;
            }

            var view = new ViewportModel(0, n - 1);
            var m = new GraphReadoutModel(view);
            m.SetX(() => "Time", (b, x) => b.AppendDHMS(x, 1, 21600, 9201600));
            for (int r = 0; r < 16; r++) m.Add(new Samples(xs, ys), "Series", (b, v) => b.AppendSI(v, 4).Append("m/s"));
            m.Move(0.1);
            m.Move(0.9);
            view.Wheel(1, 0.5, ChangeSource.Pointer);
            m.Sync();
            AssertionExtensions.ShouldNotAllocate(() =>
            {
                for (int i = 0; i < 1000; i++)
                {
                    m.Move(i / 1000.0);
                    m.Sync();
                }

                m.Press(true);
                m.Press(false);
                m.Leave();
                m.Move(0.3);
            });
        }
    }
}
