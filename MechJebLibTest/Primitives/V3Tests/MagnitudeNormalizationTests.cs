/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class MagnitudeNormalizationTests
    {
        [Fact]
        private void MagnitudeOfUnitVectors()
        {
            new V3(1, 0, 0).magnitude.ShouldEqual(1.0);
            new V3(0, 1, 0).magnitude.ShouldEqual(1.0);
            new V3(0, 0, 1).magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void MagnitudeOfSimpleVectors()
        {
            new V3(3, 4, 0).magnitude.ShouldEqual(5.0);
            new V3(1, 1, 1).magnitude.ShouldEqual(Sqrt(3));
            new V3(2, 2, 2).magnitude.ShouldEqual(2 * Sqrt(3));
        }

        [Fact]
        private void MagnitudeOfZeroVector()
        {
            V3.zero.magnitude.ShouldEqual(0.0);
            new V3(0, 0, 0).magnitude.ShouldEqual(0.0);
        }

        [Fact]
        private void MagnitudeOfNegativeComponents()
        {
            new V3(-3, 4, 0).magnitude.ShouldEqual(5.0);
            new V3(-1, -1, -1).magnitude.ShouldEqual(Sqrt(3));
            new V3(3, -4, 0).magnitude.ShouldEqual(5.0);
        }

        [Fact]
        private void MagnitudeOfVeryLargeVectors()
        {
            const double LARGE = 1e190;
            new V3(LARGE, 0, 0).magnitude.ShouldEqual(LARGE);
            new V3(LARGE, LARGE, LARGE).magnitude.ShouldEqual(Sqrt(3) * LARGE);

            const double NEAR_MAX = 1e308;
            new V3(NEAR_MAX, 0, 0).magnitude.ShouldEqual(NEAR_MAX);
        }

        [Fact]
        private void MagnitudeOfVerySmallVectors()
        {
            const double SMALL = 1e-190;
            new V3(SMALL, 0, 0).magnitude.ShouldEqual(SMALL);
            new V3(SMALL, SMALL, SMALL).magnitude.ShouldEqual(Sqrt(3) * SMALL);

            const double TINY = 1e-300;
            new V3(TINY, 0, 0).magnitude.ShouldEqual(TINY);
        }

        [Fact]
        private void MagnitudeAvoidingOverflow()
        {
            const double LARGE = 1e200;
            var          v     = new V3(LARGE, LARGE, LARGE);

            v.magnitude.ShouldEqual(Sqrt(3) * LARGE);
            Assert.True(IsFinite(v.magnitude));
        }

        [Fact]
        private void MagnitudeAvoidingUnderflow()
        {
            const double SMALL = 1e-200;
            var          v     = new V3(SMALL, SMALL, SMALL);

            v.magnitude.ShouldEqual(Sqrt(3) * SMALL);
            Assert.True(v.magnitude > 0);
        }

        [Fact]
        private void SquaredMagnitude()
        {
            new V3(3, 4, 0).sqrMagnitude.ShouldEqual(25.0);
            new V3(1, 1, 1).sqrMagnitude.ShouldEqual(3.0);
            new V3(2, 3, 6).sqrMagnitude.ShouldEqual(49.0);
            V3.zero.sqrMagnitude.ShouldEqual(0.0);
        }

        [Fact]
        private void SquaredMagnitudeOfLargeVectors()
        {
            const double LARGE = 1e150;

            new V3(LARGE, 0, 0).sqrMagnitude.ShouldEqual(LARGE * LARGE);
            new V3(LARGE, LARGE, LARGE).sqrMagnitude.ShouldEqual(3 * LARGE * LARGE);
        }

        [Fact]
        private void StaticMagnitudeMethods()
        {
            var v = new V3(3, 4, 0);

            V3.Magnitude(v).ShouldEqual(5.0);
            V3.SqrMagnitude(v).ShouldEqual(25.0);
        }

        [Fact]
        private void NormalizeSimpleVectors()
        {
            var v          = new V3(3, 4, 0);
            V3  normalized = v.normalized;

            normalized.x.ShouldEqual(0.6);
            normalized.y.ShouldEqual(0.8);
            normalized.z.ShouldEqual(0.0);
            normalized.magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void NormalizeUnitVectors()
        {
            V3.xaxis.normalized.ShouldEqual(V3.xaxis);
            V3.yaxis.normalized.ShouldEqual(V3.yaxis);
            V3.zaxis.normalized.ShouldEqual(V3.zaxis);
        }

        [Fact]
        private void NormalizeZeroVector()
        {
            V3.zero.normalized.ShouldEqual(V3.zero);
            new V3(0, 0, 0).normalized.ShouldEqual(V3.zero);
        }

        [Fact]
        private void NormalizeVeryLargeVectors()
        {
            const double LARGE = 1e190;

            var v = new V3(LARGE, 0, 0);

            v.normalized.ShouldEqual(new V3(1, 0, 0));

            var v2 = new V3(LARGE, LARGE, LARGE);
            V3  n2 = v2.normalized;

            n2.x.ShouldEqual(1.0 / Sqrt(3));
            n2.y.ShouldEqual(1.0 / Sqrt(3));
            n2.z.ShouldEqual(1.0 / Sqrt(3));
            n2.magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void NormalizeVerySmallVectors()
        {
            const double SMALL = 1e-190;

            var v = new V3(SMALL, 0, 0);

            v.normalized.ShouldEqual(new V3(1, 0, 0));

            var v2 = new V3(SMALL, SMALL, SMALL);
            V3  n2 = v2.normalized;

            n2.x.ShouldEqual(1.0 / Sqrt(3));
            n2.y.ShouldEqual(1.0 / Sqrt(3));
            n2.z.ShouldEqual(1.0 / Sqrt(3));
            n2.magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void NormalizeInPlace()
        {
            var v = new V3(3, 4, 0);

            v.Normalize();

            v.x.ShouldEqual(0.6);
            v.y.ShouldEqual(0.8);
            v.z.ShouldEqual(0.0);
            v.magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void NormalizeInPlaceZeroVector()
        {
            var v = new V3(0, 0, 0);

            v.Normalize();

            v.ShouldEqual(V3.zero);
        }

        [Fact]
        private void StaticNormalizeMethod()
        {
            var v          = new V3(3, 4, 0);
            var normalized = V3.Normalize(v);

            normalized.x.ShouldEqual(0.6);
            normalized.y.ShouldEqual(0.8);
            normalized.z.ShouldEqual(0.0);
            normalized.magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void ClampMagnitudeWithinLimit()
        {
            var v       = new V3(3, 4, 0);
            var clamped = V3.ClampMagnitude(v, 10);

            clamped.ShouldEqual(v);
        }

        [Fact]
        private void ClampMagnitudeExceedsLimit()
        {
            var v       = new V3(3, 4, 0);
            var clamped = V3.ClampMagnitude(v, 2.5);

            clamped.magnitude.ShouldEqual(2.5);
            clamped.normalized.ShouldEqual(v.normalized);
        }

        [Fact]
        private void ClampMagnitudeZeroVector()
        {
            var clamped = V3.ClampMagnitude(V3.zero, 5);

            clamped.ShouldEqual(V3.zero);
        }

        [Fact]
        private void ClampMagnitudeToZero()
        {
            var v       = new V3(3, 4, 0);
            var clamped = V3.ClampMagnitude(v, 0);

            clamped.ShouldEqual(V3.zero);
        }

        [Fact]
        private void ClampMagnitudeLargeVectors()
        {
            const double LARGE = 1e190;

            var v       = new V3(LARGE, LARGE, LARGE);
            var clamped = V3.ClampMagnitude(v, 1e189);

            clamped.magnitude.ShouldEqual(1e189);
            Assert.True(IsFinite(clamped.x));
            Assert.True(IsFinite(clamped.y));
            Assert.True(IsFinite(clamped.z));
        }

        [Fact]
        private void NormalizedVectorMaintainsDirection()
        {
            var v      = new V3(1, 2, 3);
            V3  n      = v.normalized;
            V3  scaled = n * v.magnitude;

            scaled.ShouldEqual(v, 1e-14);
        }

        [Fact]
        private void MagnitudeConsistencyWithSquaredMagnitude()
        {
            var    v      = new V3(5, 12, 13);
            double mag    = v.magnitude;
            double sqrMag = v.sqrMagnitude;

            (mag * mag).ShouldEqual(sqrMag, 1e-14);
            Sqrt(sqrMag).ShouldEqual(mag, 1e-14);
        }

        [Fact]
        private void NormalizePreservesZeroForNearZeroComponents()
        {
            var v = new V3(1, 0, 0);
            V3  n = v.normalized;

            n.x.ShouldEqual(1.0);
            n.y.ShouldEqual(0.0);
            n.z.ShouldEqual(0.0);
        }
    }
}
