/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.InterpolantsTests
{
    // A DeepCopy() is handed to another thread, so it has to evaluate identically and share no pooled storage: if it did,
    // disposing the original would either corrupt the copy's values or make disposing the copy a double-release.
    //
    // A PSG Solution is denormalized by rescaling a DeepCopy() of each of its interpolants, which must then be the original in
    // the new units, and leave the original untouched.
    public class InterpolantDeepCopyTests
    {
        private const int LEN     = 7;
        private const int SAMPLES = 32;

        // spans the whole interpolant plus a little extrapolation past both ends
        private static double Sample(int i) => -0.25 + 4.5 * i / SAMPLES;

        private static Vec RentFilled(double seed)
        {
            var v = Vec.Rent(LEN);
            for (int i = 0; i < LEN; i++)
                v[i] = seed + 0.37 * i - 0.11 * i * i;
            return v;
        }

        // one segment of every Vec node type, over [0,1] [1,2] [2,3] [3,4]
        private static VecInterpolant BuildVecInterpolant()
        {
            using Vec a = RentFilled(1.0);
            using Vec b = RentFilled(-2.0);
            using Vec c = RentFilled(0.5);
            using Vec d = RentFilled(3.0);
            using Vec e = RentFilled(-0.75);
            using Vec f = RentFilled(2.25);
            using Vec g = RentFilled(-1.5);
            using Vec h = RentFilled(0.125);

            var interpolant = VecInterpolant.Rent();
            interpolant.Append(CubicHermiteVecNode.Rent(0, 1, a, b, c, d), 0, 1);
            interpolant.Append(DP5Node.Rent(1, 1, a, b, c, d, e, f, g), 1, 2);
            interpolant.Append(Tsit5Node.Rent(2, 1, h, a, b, c, d, e, f, g), 2, 3);
            interpolant.Append(ConstantVecNode.Rent(e), 3, 4);
            return interpolant;
        }

        [Fact]
        public void VecInterpolantDeepCopyIsIndependent()
        {
            VecInterpolant original = BuildVecInterpolant();
            VecInterpolant copy     = original.DeepCopy();

            copy.MinT.ShouldEqual(original.MinT, 0);
            copy.MaxT.ShouldEqual(original.MaxT, 0);

            var expected = new double[SAMPLES + 1, LEN];

            for (int i = 0; i <= SAMPLES; i++)
            {
                using Vec y  = original.Evaluate(Sample(i));
                using Vec yc = copy.Evaluate(Sample(i));
                for (int j = 0; j < LEN; j++)
                {
                    yc[j].ShouldEqual(y[j], 0);
                    expected[i, j] = y[j];
                }
            }

            original.Dispose();

            for (int i = 0; i <= SAMPLES; i++)
            {
                using Vec yc = copy.Evaluate(Sample(i));
                for (int j = 0; j < LEN; j++)
                    yc[j].ShouldEqual(expected[i, j], 0);
            }

            copy.Dispose();
        }

        // every double node type over [0,1] [1,2] [2,4], and a zero-width Hermite node at 4
        private static DoubleInterpolant BuildDoubleInterpolant()
        {
            DoubleInterpolant interpolant = DoubleInterpolant.Rent();
            interpolant.Append(CubicHermiteDoubleNode.Rent(0, 1, 1.25, -3.0, 4.5, 0.5), 0, 1);
            interpolant.Append(ConstantDoubleNode.Rent(2.5), 1, 2);
            interpolant.Append(CubicHermiteDoubleNode.Rent(2, 2, 2.5, 0.25, -1.0, 2.0), 2, 4);
            interpolant.Append(CubicHermiteDoubleNode.Rent(4, 0, -0.75, 1.0, 3.0, 2.0), 4, 4);
            return interpolant;
        }

        [Fact]
        public void DoubleInterpolantDeepCopyIsIndependent()
        {
            DoubleInterpolant original = BuildDoubleInterpolant();

            DoubleInterpolant copy = original.DeepCopy();

            copy.ToString().ShouldEqual(original.ToString());
            copy.MinT.ShouldEqual(original.MinT, 0);
            copy.MaxT.ShouldEqual(original.MaxT, 0);

            var expected = new double[SAMPLES + 1];

            for (int i = 0; i <= SAMPLES; i++)
            {
                expected[i] = original.Evaluate(Sample(i));
                copy.Evaluate(Sample(i)).ShouldEqual(expected[i], 0);
            }

            original.Dispose();

            for (int i = 0; i <= SAMPLES; i++)
                copy.Evaluate(Sample(i)).ShouldEqual(expected[i], 0);

            copy.Dispose();
        }

        // Powers of two rescale exactly, which checks every node scales its time and value data consistently (the DP5 and
        // Tsit5 stages are derivatives, and take yScale / timeScale).  Real scales aren't powers of two, and only round off.
        [Theory]
        [InlineData(8, 0)]
        [InlineData(0.125, 0)]
        [InlineData(806.2093, 1e-13)]
        public void VecInterpolantRescale(double timeScale, double tol)
        {
            using VecInterpolant original = BuildVecInterpolant();
            using VecInterpolant scaled   = original.DeepCopy();
            using var yScale = Vec.Rent(LEN);
            for (int j = 0; j < LEN; j++)
                yScale[j] = tol == 0 ? Math.Pow(2, j - 3) : 1.37 * j + 0.29;

            scaled.Rescale(timeScale, yScale);

            scaled.MinT.ShouldEqual(original.MinT * timeScale, tol);
            scaled.MaxT.ShouldEqual(original.MaxT * timeScale, tol);

            // the rescaled copy at timeScale·x is the original at x, which the copy's rescale left alone
            for (int i = 0; i <= SAMPLES; i++)
            {
                using Vec y  = original.Evaluate(Sample(i));
                using Vec ys = scaled.Evaluate(timeScale * Sample(i));
                for (int j = 0; j < LEN; j++)
                    ys[j].ShouldEqual(y[j] * yScale[j], tol);
            }
        }

        [Theory]
        [InlineData(8, 0.25, 0)]
        [InlineData(0.125, 16, 0)]
        [InlineData(806.2093, 3.7, 1e-13)]
        public void DoubleInterpolantRescale(double timeScale, double yScale, double tol)
        {
            using DoubleInterpolant original = BuildDoubleInterpolant();
            using DoubleInterpolant scaled   = original.DeepCopy();

            scaled.Rescale(timeScale, yScale);

            scaled.MinT.ShouldEqual(original.MinT * timeScale, tol);
            scaled.MaxT.ShouldEqual(original.MaxT * timeScale, tol);

            for (int i = 0; i <= SAMPLES; i++)
                scaled.Evaluate(timeScale * Sample(i)).ShouldEqual(original.Evaluate(Sample(i)) * yScale, tol);
        }

        [Fact]
        public void RescaleNeedsAPositiveTimeScale()
        {
            using DoubleInterpolant interpolant = BuildDoubleInterpolant();
            Assert.Throws<ArgumentException>(() => interpolant.Rescale(0, 1));
            Assert.Throws<ArgumentException>(() => interpolant.Rescale(-1, 1));
        }
    }
}
