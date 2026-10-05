/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.V2Tests
{
    public class MagnitudeNormalizationTests
    {
        [Fact]
        private void Magnitude()
        {
            var v = new V2(3, -4);

            v.magnitude.ShouldEqual(5);
            V2.Magnitude(v).ShouldEqual(5);
            V2.zero.magnitude.ShouldEqual(0);
        }

        [Fact]
        private void SqrMagnitude()
        {
            var v = new V2(3, -4);

            v.sqrMagnitude.ShouldEqual(25);
            V2.SqrMagnitude(v).ShouldEqual(25);
        }

        [Fact]
        private void Normalized()
        {
            new V2(3, -4).normalized.ShouldEqual(new V2(0.6, -0.8));
            V2.Normalize(new V2(0, 7)).ShouldEqual(V2.yaxis);
        }

        [Fact]
        private void NormalizeInPlace()
        {
            var v = new V2(3, -4);
            v.Normalize();

            v.ShouldEqual(new V2(0.6, -0.8));
        }

        [Fact]
        private void NormalizeZeroIsZero()
        {
            V2.zero.normalized.ShouldEqual(V2.zero);
            V2.zero.safeNormalized.ShouldEqual(V2.zero);

            V2 v = V2.zero;
            v.Normalize();
            v.ShouldEqual(V2.zero);

            v = V2.zero;
            v.SafeNormalize();
            v.ShouldEqual(V2.zero);
        }

        [Fact]
        private void SafeNormalizeHugeAndTiny()
        {
            new V2(3e300, -4e300).safeNormalized.ShouldEqual(new V2(0.6, -0.8));
            new V2(3e-300, -4e-300).safeNormalized.ShouldEqual(new V2(0.6, -0.8));
            V2.SafeNormalize(new V2(3e300, -4e300)).ShouldEqual(new V2(0.6, -0.8));

            var v = new V2(3e-300, -4e-300);
            v.SafeNormalize();
            v.ShouldEqual(new V2(0.6, -0.8));
        }

        [Fact]
        private void MaxMinMagnitude()
        {
            var v = new V2(-7, 3);

            v.max_magnitude.ShouldEqual(7);
            v.min_magnitude.ShouldEqual(3);
            v.max_magnitude_index.ShouldEqual(0);
            v.min_magnitude_index.ShouldEqual(1);

            var w = new V2(2, -5);

            w.max_magnitude.ShouldEqual(5);
            w.min_magnitude.ShouldEqual(2);
            w.max_magnitude_index.ShouldEqual(1);
            w.min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void MaxMinMagnitudeIndexTiesPreferFirst()
        {
            var v = new V2(-2, 2);

            v.max_magnitude_index.ShouldEqual(0);
            v.min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void NormalizedMagnitudeIsOne()
        {
            var v = new V2(-1.234, 5.678);

            Abs(v.normalized.magnitude - 1).ShouldBeLessThan(1e-15);
        }
    }
}
