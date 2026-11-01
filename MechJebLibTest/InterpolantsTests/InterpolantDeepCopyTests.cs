/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.InterpolantsTests
{
    // A DeepCopy() is handed to another thread, so it has to evaluate identically and share no pooled storage: if it did,
    // disposing the original would either corrupt the copy's values or make disposing the copy a double-release.
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

        [Fact]
        public void DoubleInterpolantDeepCopyIsIndependent()
        {
            DoubleInterpolant original = DoubleInterpolant.Rent();
            original.Append(CubicHermiteDoubleNode.Rent(0, 1, 1.25, -3.0, 4.5, 0.5), 0, 1);
            original.Append(ConstantDoubleNode.Rent(2.5), 1, 2);
            original.Append(CubicHermiteDoubleNode.Rent(2, 2, 2.5, 0.25, -1.0, 2.0), 2, 4);

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
    }
}
