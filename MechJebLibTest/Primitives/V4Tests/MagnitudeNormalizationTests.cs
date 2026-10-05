/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.V4Tests
{
    public class MagnitudeNormalizationTests
    {
        [Fact]
        private void Magnitude()
        {
            var v = new V4(1, -2, 2, -4);

            v.magnitude.ShouldEqual(5);
            V4.Magnitude(v).ShouldEqual(5);
            V4.zero.magnitude.ShouldEqual(0);
        }

        [Fact]
        private void SqrMagnitude()
        {
            var v = new V4(1, -2, 2, -4);

            v.sqrMagnitude.ShouldEqual(25);
            V4.SqrMagnitude(v).ShouldEqual(25);
        }

        [Fact]
        private void Normalized()
        {
            new V4(1, -2, 2, -4).normalized.ShouldEqual(new V4(0.2, -0.4, 0.4, -0.8));
            V4.Normalize(new V4(0, 0, 0, 7)).ShouldEqual(V4.waxis);
        }

        [Fact]
        private void NormalizeInPlace()
        {
            var v = new V4(1, -2, 2, -4);
            v.Normalize();

            v.ShouldEqual(new V4(0.2, -0.4, 0.4, -0.8));
        }

        [Fact]
        private void NormalizeZeroIsZero()
        {
            V4.zero.normalized.ShouldEqual(V4.zero);
            V4.zero.safeNormalized.ShouldEqual(V4.zero);

            V4 v = V4.zero;
            v.Normalize();
            v.ShouldEqual(V4.zero);

            v = V4.zero;
            v.SafeNormalize();
            v.ShouldEqual(V4.zero);
        }

        [Fact]
        private void SafeNormalizeHugeAndTiny()
        {
            var expected = new V4(0.2, -0.4, 0.4, -0.8);

            new V4(1e300, -2e300, 2e300, -4e300).safeNormalized.ShouldEqual(expected);
            new V4(1e-300, -2e-300, 2e-300, -4e-300).safeNormalized.ShouldEqual(expected);
            V4.SafeNormalize(new V4(1e300, -2e300, 2e300, -4e300)).ShouldEqual(expected);

            var v = new V4(1e-300, -2e-300, 2e-300, -4e-300);
            v.SafeNormalize();
            v.ShouldEqual(expected);
        }

        [Fact]
        private void MaxMinMagnitude()
        {
            var v = new V4(1, -5, 3, 2);

            v.max_magnitude.ShouldEqual(5);
            v.min_magnitude.ShouldEqual(1);
            v.max_magnitude_index.ShouldEqual(1);
            v.min_magnitude_index.ShouldEqual(0);
        }

        [Fact]
        private void MaxMinMagnitudeIndexEveryPosition()
        {
            new V4(9, 1, 2, 3).max_magnitude_index.ShouldEqual(0);
            new V4(1, -9, 2, 3).max_magnitude_index.ShouldEqual(1);
            new V4(1, 2, 9, 3).max_magnitude_index.ShouldEqual(2);
            new V4(1, 2, 3, -9).max_magnitude_index.ShouldEqual(3);

            new V4(0.5, 1, 2, 3).min_magnitude_index.ShouldEqual(0);
            new V4(1, -0.5, 2, 3).min_magnitude_index.ShouldEqual(1);
            new V4(1, 2, 0.5, 3).min_magnitude_index.ShouldEqual(2);
            new V4(1, 2, 3, -0.5).min_magnitude_index.ShouldEqual(3);
        }

        [Fact]
        private void MaxMinMagnitudeIndexTiesPreferFirst()
        {
            var v = new V4(2, -2, 2, -2);

            v.max_magnitude_index.ShouldEqual(0);
            v.min_magnitude_index.ShouldEqual(0);
            new V4(1, 3, -3, 1).max_magnitude_index.ShouldEqual(1);
            new V4(3, 1, 3, -1).min_magnitude_index.ShouldEqual(1);
        }

        [Fact]
        private void NormalizedMagnitudeIsOne()
        {
            var v = new V4(-1.234, 5.678, 0.001, -9.87);

            Abs(v.normalized.magnitude - 1).ShouldBeLessThan(1e-15);
        }
    }
}
