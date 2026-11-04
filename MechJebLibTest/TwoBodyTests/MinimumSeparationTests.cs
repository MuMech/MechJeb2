/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Minimization;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;
using Random = System.Random;

namespace MechJebLibTest.TwoBodyTests
{
    public class MinimumSeparationTests
    {
        // Kerbin
        private const double MU = 3.5316e12;
        private const double RADIUS = 600000;

        // The solver uses 0.3, so this samples about 10x finer
        private const double SCAN_STEP_FRACTION = 0.03;

        private static (V3 r, V3 v) Periapsis(double rp, double ecc, double inc = 0, double lan = 0, double argp = 0) =>
            Astro.StateVectorsFromKeplerian(MU, rp * (1 + ecc), ecc, inc, lan, argp, 0);

        private static (V3 r, V3 v) Ellipse(double rp, double rap, double inc = 0) => Periapsis(rp, (rap - rp) / (rap + rp), inc);

        private static double Period((V3 r, V3 v) s) => Astro.PeriodFromStateVectors(MU, s.r, s.v);

        private static (V3 r, V3 v) Propagate((V3 r, V3 v) s, double t) => Shepperd.Solve(MU, t, s.r, s.v);

        private static (double dt, double distance) Solve((V3 r, V3 v) a, (V3 r, V3 v) b, double dtMax) =>
            MinimumSeparation.Solve(MU, a.r, a.v, b.r, b.v, dtMax);

        private static MinimumSeparation.Sample Evaluate((V3 r, V3 v) a, (V3 r, V3 v) b, double t) =>
            MinimumSeparation.Evaluate(MU, a.r, a.v, b.r, b.v, t);

        // Scans the distance alone and never looks at the range rate the solver depends on, so it can disagree with the solver.
        private static (double dt, double distance) DenseScan((V3 r, V3 v) a, (V3 r, V3 v) b, double dtMax)
        {
            (double distance, double timescale) At(double t)
            {
                (V3 raf, V3 vaf) = Propagate(a, t);
                (V3 rbf, V3 vbf) = Propagate(b, t);
                return ((raf - rbf).magnitude, Min(raf.magnitude / vaf.magnitude, rbf.magnitude / vbf.magnitude));
            }

            (double d1, double timescale) = At(0);
            double bestDt = 0;
            double bestDistance = d1;
            double t0 = double.NaN;
            double d0 = double.PositiveInfinity;
            double t1 = 0;

            while (t1 < dtMax)
            {
                double t2 = Min(t1 + SCAN_STEP_FRACTION * timescale, dtMax);
                (double d2, double timescale2) = At(t2);

                if (d2 < bestDistance)
                {
                    bestDistance = d2;
                    bestDt = t2;
                }

                if (d1 <= d0 && d1 <= d2 && !double.IsNaN(t0))
                {
                    (double x, double fx) = BrentMin.Solve((t, o) => At(t).distance, t0, t2, null, 1e-9, 200);

                    if (fx < bestDistance)
                    {
                        bestDistance = fx;
                        bestDt = x;
                    }
                }

                t0 = t1;
                d0 = d1;
                t1 = t2;
                d1 = d2;
                timescale = timescale2;
            }

            return (bestDt, bestDistance);
        }

        private static void AssertMatchesDenseScan((V3 r, V3 v) a, (V3 r, V3 v) b, double dtMax)
        {
            (double dt, double distance) = Solve(a, b, dtMax);
            (double _, double scanDistance) = DenseScan(a, b, dtMax);

            double slack = Max(1.0, 1e-3 * scanDistance);
            Assert.InRange(distance, scanDistance - slack, scanDistance + slack);
            Assert.InRange(dt, 0, dtMax);

            MinimumSeparation.Sample sample = Evaluate(a, b, dt);
            sample.Distance.ShouldEqual(distance, 1e-9);

            if (dt <= 0 || dt >= dtMax)
                return;

            Assert.True(sample.Drdv > 0, $"not a minimum: drdv={sample.Drdv} at dt={dt}");
            Assert.InRange(sample.Rdv / sample.Drdv, -1e-3, 1e-3);
        }

        public static IEnumerable<object[]> Seeds(int count)
        {
            for (int i = 0; i < count; i++)
                yield return new object[] { i };
        }

        [Theory]
        [InlineData(1000e3, 0.2, 1500e3, 0.0, 1000)]
        [InlineData(1000e3, 0.7, 2000e3, 0.3, 5000)]
        [InlineData(1000e3, 1.5, 2000e3, 0.1, 500)] // a is hyperbolic
        [InlineData(1000e3, 2.5, 3000e3, 1.2, 500)] // both are hyperbolic
        public void DerivativesMatchCentralDifferences(double rpa, double ecca, double rpb, double eccb, double t)
        {
            const double H = 1;

            (V3 r, V3 v) a = Periapsis(rpa, ecca, 0.3, 0.2, 0.5);
            (V3 r, V3 v) b = Periapsis(rpb, eccb, 0.7, 1.3, 2.1);

            MinimumSeparation.Sample mid = Evaluate(a, b, t);
            MinimumSeparation.Sample lo = Evaluate(a, b, t - H);
            MinimumSeparation.Sample hi = Evaluate(a, b, t + H);

            // the range rate itself, which is half the derivative of the squared distance
            (0.25 * (hi.Distance * hi.Distance - lo.Distance * lo.Distance) / H).ShouldEqual(mid.Rdv, 1e-4);
            (0.5 * (hi.Rdv - lo.Rdv) / H).ShouldEqual(mid.Drdv, 1e-4);
            (0.5 * (hi.Drdv - lo.Drdv) / H).ShouldEqual(mid.Ddrdv, 1e-4);
        }

        public static IEnumerable<object[]> ShorterTargetPeriodCases()
        {
            foreach (double inc in new[] { 0, 0.01, 0.087 })
            foreach (double vesselPhase in new[] { 0.02, 0.25, 0.5, 0.75 })
                for (int targetPhase = 0; targetPhase < 40; targetPhase++)
                    yield return new object[] { inc, vesselPhase, targetPhase };
        }

        // A target in a much shorter orbit has many passes in the window, and only one of them is the real minimum.
        [Theory]
        [MemberData(nameof(ShorterTargetPeriodCases))]
        public void ShorterTargetPeriod(double inc, double vesselPhase, int targetPhase)
        {
            (V3 r, V3 v) a0 = Ellipse(RADIUS + 700e3, RADIUS + 20e6);
            (V3 r, V3 v) b0 = Ellipse(RADIUS + 750e3, RADIUS + 750e3, inc);

            double period = Period(a0);

            AssertMatchesDenseScan(Propagate(a0, vesselPhase * period), Propagate(b0, targetPhase / 40.0 * Period(b0)), period);
        }

        [Fact]
        public void IsNotTheFirstLocalMinimum()
        {
            (V3 r, V3 v) a0 = Ellipse(RADIUS + 700e3, RADIUS + 20e6);
            (V3 r, V3 v) b0 = Ellipse(RADIUS + 750e3, RADIUS + 750e3);

            double period = Period(a0);
            (V3 r, V3 v) a = Propagate(a0, 0.5 * period);
            (V3 r, V3 v) b = Propagate(b0, 0.2 * Period(b0));

            // the first pass is where the range rate first goes from negative to positive
            const double STEP = 5;
            double t = 0;
            MinimumSeparation.Sample first = Evaluate(a, b, t);
            while (first.Rdv >= 0)
                first = Evaluate(a, b, t += STEP);
            while (first.Rdv < 0)
                first = Evaluate(a, b, t += STEP);

            (double dt, double distance) = Solve(a, b, period);

            Assert.True(t < period);
            Assert.True(dt > t, $"found the first pass at {dt}");
            Assert.True(distance < 0.5 * first.Distance, $"{distance} is not much closer than the first pass at {first.Distance}");
        }

        private static (V3 r, V3 v) RandomEllipse(Random rng, double maxEcc)
        {
            double rp = RADIUS + 1e5 + rng.NextDouble() * 5e6;
            double ecc = rng.NextDouble() * maxEcc;
            (V3 r, V3 v) s = Periapsis(rp, ecc, rng.NextDouble() * 0.3, rng.NextDouble() * TAU, rng.NextDouble() * TAU);
            return Propagate(s, rng.NextDouble() * Period(s));
        }

        private static (V3 r, V3 v) RandomHyperbola(Random rng, out double meanMotion)
        {
            double rp = RADIUS + 1e5 + rng.NextDouble() * 3e6;
            double ecc = 1.01 + rng.NextDouble() * 2;
            double sma = rp / (ecc - 1);
            meanMotion = Sqrt(MU / (sma * sma * sma));
            (V3 r, V3 v) s = Periapsis(rp, ecc, rng.NextDouble() * 0.3, rng.NextDouble() * TAU, rng.NextDouble() * TAU);
            return Propagate(s, -rng.NextDouble() * 20 / meanMotion);
        }

        [Theory]
        [MemberData(nameof(Seeds), 100)]
        public void RandomEllipses(int seed)
        {
            var rng = new Random(seed);

            (V3 r, V3 v) a = RandomEllipse(rng, 0.98);
            (V3 r, V3 v) b = RandomEllipse(rng, seed % 2 == 0 ? 0.05 : 0.98);

            double periodA = Period(a);

            // no more orbits of the shorter orbit than the solver's sample limit is meant to resolve
            AssertMatchesDenseScan(a, b, Min(periodA, 50 * Min(periodA, Period(b))));
        }

        [Theory]
        [MemberData(nameof(Seeds), 50)]
        public void RandomHyperbolas(int seed)
        {
            var rng = new Random(seed);

            (V3 r, V3 v) a = RandomHyperbola(rng, out double meanMotion);
            (V3 r, V3 v) b = seed % 2 == 0 ? RandomHyperbola(rng, out double _) : RandomEllipse(rng, 0.5);

            AssertMatchesDenseScan(a, b, 100 / meanMotion);
        }

        [Fact]
        public void RecedingWithNothingCloserReturnsTheStart()
        {
            // faster radially than the target can ever close the distance by orbiting
            (V3 r, V3 v) a = (new V3(1e7, 0, 0), new V3(5000, 100, 0));
            (V3 r, V3 v) b = Ellipse(RADIUS + 750e3, RADIUS + 750e3);

            (double dt, double distance) = Solve(a, b, 1e5);

            Assert.Equal(0, dt);
            distance.ShouldEqual((a.r - b.r).magnitude, 1e-12);
        }

        [Fact]
        public void StillClosingAtTheBoundReturnsTheBound()
        {
            (V3 r, V3 v) a = (new V3(1e7, 0, 0), new V3(-5000, 100, 0));
            (V3 r, V3 v) b = Ellipse(RADIUS + 750e3, RADIUS + 750e3);

            const double DT_MAX = 100;

            (double dt, double distance) = Solve(a, b, DT_MAX);

            Assert.Equal(DT_MAX, dt);
            distance.ShouldEqual(Evaluate(a, b, DT_MAX).Distance, 1e-12);
        }

        [Fact]
        public void EmptyWindowReturnsTheStart()
        {
            (V3 r, V3 v) a = (new V3(1e7, 0, 0), new V3(-5000, 100, 0));
            (V3 r, V3 v) b = Ellipse(RADIUS + 750e3, RADIUS + 750e3);

            (double dt, double distance) = Solve(a, b, 0);

            Assert.Equal(0, dt);
            distance.ShouldEqual((a.r - b.r).magnitude, 1e-12);
        }

        // With two coplanar circles the separation is known exactly: its minima are where the phase between them is zero and
        // its maxima where it is pi.  Over one synodic period the only minimum is the one found from where the phase starts.
        [Theory]
        [InlineData(0.5, 1)] // just past a minimum, so it has to get over the maximum first
        [InlineData(PI, 0)] // on the maximum
        [InlineData(4.0, -1)] // already on the way down to the minimum
        public void CoplanarCircles(double phase, int rdvSign)
        {
            const double R1 = 1000e3;
            const double R2 = 2000e3;

            double v1 = Sqrt(MU / R1);
            double v2 = Sqrt(MU / R2);
            double rate = v1 / R1 - v2 / R2;
            double synodicPeriod = TAU / rate;

            (V3 r, V3 v) a = (R1 * new V3(Cos(phase), Sin(phase), 0), v1 * new V3(-Sin(phase), Cos(phase), 0));
            (V3 r, V3 v) b = (new V3(R2, 0, 0), new V3(0, v2, 0));

            double rdv = Evaluate(a, b, 0).Rdv;

            if (rdvSign == 0)
                Assert.InRange(rdv / Evaluate(a, b, 0).Drdv, -1e-6, 1e-6);
            else
                Assert.Equal(rdvSign, Sign(rdv));

            double expected = (TAU - phase) / rate;

            (double dt, double distance) = Solve(a, b, synodicPeriod);

            Assert.InRange(dt, expected - 1e-3, expected + 1e-3);
            Assert.InRange(distance, R2 - R1 - 1e-3, R2 - R1 + 1e-3);

            // it doesn't matter which one sets out from where
            (double dtSwapped, double distanceSwapped) = Solve(b, a, synodicPeriod);

            Assert.InRange(dtSwapped, expected - 1e-3, expected + 1e-3);
            Assert.InRange(distanceSwapped, R2 - R1 - 1e-3, R2 - R1 + 1e-3);
        }

        [Fact]
        public void IdenticalStates()
        {
            (V3 r, V3 v) a = Ellipse(RADIUS + 100e3, RADIUS + 100e3);

            (double dt, double distance) = Solve(a, a, Period(a));

            Assert.Equal(0, dt);
            Assert.Equal(0, distance);
        }

        [Fact]
        public void CoOrbitalOnTheSameCircle()
        {
            (V3 r, V3 v) a = Ellipse(RADIUS + 100e3, RADIUS + 100e3);
            double period = Period(a);
            (V3 r, V3 v) b = Propagate(a, 1e-5 * period);

            (double dt, double distance) = Solve(a, b, period);

            Assert.InRange(dt, 0, period);
            Assert.InRange(distance, 0, (a.r - b.r).magnitude + 1e-3);
            Assert.InRange(distance, (a.r - b.r).magnitude - 1e-3, double.PositiveInfinity);
        }

        [Fact]
        public void CoOrbitalOnTheSameEllipse()
        {
            (V3 r, V3 v) a = Ellipse(RADIUS + 100e3, RADIUS + 1200e3);
            double period = Period(a);
            (V3 r, V3 v) b = Propagate(a, 1e-5 * period);

            (double dt, double distance) = Solve(a, b, period);

            Assert.InRange(dt, 0, period);
            Assert.InRange(distance, 0, (a.r - b.r).magnitude);
        }

        // There is no accuracy to claim with this many orbits in the window, but without the sample limit this would run for
        // many minutes, and that would be every frame in a rendezvous window.
        [Fact]
        public void SampleLimitBoundsAnExtremePeriodRatio()
        {
            (V3 r, V3 v) a = Ellipse(RADIUS + 700e3, 5e10);
            (V3 r, V3 v) b = Ellipse(RADIUS + 100e3, RADIUS + 100e3);
            double period = Period(a);

            Assert.True(period / Period(b) > 1e6);

            (double dt, double distance) = Solve(a, b, period);

            Assert.InRange(dt, 0, period);
            Assert.True(distance >= 0 && distance < double.PositiveInfinity);
        }

        [Fact]
        public void NonFiniteInputThrows()
        {
            (V3 r, V3 v) a = Ellipse(RADIUS + 100e3, RADIUS + 100e3);
            (V3 r, V3 v) b = Ellipse(RADIUS + 200e3, RADIUS + 200e3);

            Assert.ThrowsAny<Exception>(() => Solve(a, b, double.NaN));
            Assert.ThrowsAny<Exception>(() => Solve(a, b, double.PositiveInfinity));
            Assert.ThrowsAny<Exception>(() => Solve((new V3(double.NaN, 0, 0), a.v), b, 1000));
        }
    }
}
