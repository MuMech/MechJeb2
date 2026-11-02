/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class NullspaceTests
    {
        private const double NEGATIVE_ZERO = -0.0;

        // the basis is not unique, so check the properties that define it: unit length, perpendicular to each other and
        // to v, right-handed around v, and the projector onto the plane matches I - n n^T (the basis independent check,
        // equivalent to comparing the span against an SVD).
        private static void AssertBasis(V3 v, double tol = 4 * EPS)
        {
            (V3 p, V3 q) = v.nullspace;
            V3 n = v.safeNormalized;

            (p.magnitude - 1).ShouldBeZero(tol);
            (q.magnitude - 1).ShouldBeZero(tol);
            V3.Dot(p, q).ShouldBeZero(tol);
            V3.Dot(p, n).ShouldBeZero(tol);
            V3.Dot(q, n).ShouldBeZero(tol);
            (V3.Cross(p, q) - n).ShouldBeZero(tol);

            M3 projector = V3.Outer(p, p) + V3.Outer(q, q);
            (projector - (M3.identity - V3.Outer(n, n))).max_magnitude.ShouldBeZero(2 * tol);
        }

        [Theory]
        [InlineData(1, 0, 0)]
        [InlineData(-1, 0, 0)]
        [InlineData(0, 1, 0)]
        [InlineData(0, -1, 0)]
        [InlineData(0, 0, 1)]
        [InlineData(0, 0, -1)]
        [InlineData(1, 1, 1)]
        [InlineData(-1, 2, -3)]
        [InlineData(0.3, -0.4, 12)]
        // on the equator, where the reflection switches between +z and -z
        [InlineData(3, 4, 0)]
        [InlineData(3, 4, NEGATIVE_ZERO)]
        [InlineData(3, 4, 1e-300)]
        [InlineData(3, 4, -1e-300)]
        // next to the poles, and the direction where Frisvad's original (unsigned) formula divides by zero
        [InlineData(1e-9, -2e-9, 1)]
        [InlineData(1e-9, -2e-9, -1)]
        [InlineData(1e-17, 0, -1)]
        [InlineData(0, 1e-17, 1)]
        // where the sum of the squares overflows or underflows
        [InlineData(1e300, -2e300, 3e300)]
        [InlineData(double.MaxValue, double.MaxValue, -double.MaxValue)]
        [InlineData(1e-300, 2e-300, -3e-300)]
        [InlineData(1e200, 1e-200, -1e200)]
        [InlineData(double.Epsilon, 0, 0)]
        [InlineData(0, 0, -double.Epsilon)]
        [InlineData(double.Epsilon, 2 * double.Epsilon, -double.Epsilon)]
        [InlineData(1e-310, -3e-310, 2e-310)]
        public void BasisIsOrthonormalAndRightHanded(double x, double y, double z) => AssertBasis(new V3(x, y, z));

        [Fact]
        public void BasisIsOrthonormalForRandomDirections()
        {
            var random = new Random(42);

            for (int i = 0; i < 10000; i++)
            {
                // a quarter each concentrated at the north pole, the south pole, the equator, and uniform over the sphere
                double z = (i % 4) switch
                {
                    0 => 1 - Pow(10, -17 * random.NextDouble()),
                    1 => -1 + Pow(10, -17 * random.NextDouble()),
                    2 => (random.NextDouble() - 0.5) * Pow(10, -17 * random.NextDouble()),
                    _ => 2 * random.NextDouble() - 1
                };
                double phi = 2 * PI * random.NextDouble();
                double rho = Sqrt(Max(0, 1 - z * z));
                double scale = Pow(10, 600 * (random.NextDouble() - 0.5));

                AssertBasis(new V3(rho * Cos(phi), rho * Sin(phi), z) * scale);
            }
        }

        [Theory]
        [InlineData(0, 0, 1, 1, 0, 0, 0, 1, 0)]
        [InlineData(0, 0, -1, 1, 0, 0, 0, -1, 0)]
        [InlineData(1, 0, 0, 0, 0, -1, 0, 1, 0)]
        [InlineData(-1, 0, 0, 0, 0, 1, 0, 1, 0)]
        [InlineData(0, 1, 0, 1, 0, 0, 0, 0, -1)]
        [InlineData(0, -1, 0, 1, 0, 0, 0, 0, 1)]
        [InlineData(0, 0, 7, 1, 0, 0, 0, 1, 0)]
        public void AxesGiveExactAxes(double x, double y, double z, double px, double py, double pz, double qx, double qy, double qz)
        {
            (V3 p, V3 q) = new V3(x, y, z).nullspace;

            Assert.Equal(new V3(px, py, pz), p);
            Assert.Equal(new V3(qx, qy, qz), q);
        }

        [Fact]
        public void SignedZeroGivesTheSameBasis()
        {
            Assert.True(double.IsNegativeInfinity(1.0 / NEGATIVE_ZERO));

            (V3 p1, V3 q1) = new V3(3, 4, 0).nullspace;
            (V3 p2, V3 q2) = new V3(3, 4, NEGATIVE_ZERO).nullspace;

            Assert.Equal(p1, p2);
            Assert.Equal(q1, q2);
        }

        [Fact]
        public void BasisDependsOnlyOnDirection()
        {
            var v = new V3(0.3, -0.7, 0.2);
            (V3 p, V3 q) = v.nullspace;

            // scaling by a power of two is exact, so the basis is bit-identical
            foreach (double scale in new[] { Pow(2, 500), Pow(2, -500), 2.0, 0.5 })
            {
                (V3 ps, V3 qs) = (v * scale).nullspace;
                Assert.Equal(p, ps);
                Assert.Equal(q, qs);
            }

            // anything else is only equal up to rounding
            foreach (double scale in new[] { 3.0, 1e-100, 7e150 })
            {
                (V3 ps, V3 qs) = (v * scale).nullspace;
                ps.ShouldEqual(p, 4 * EPS);
                qs.ShouldEqual(q, 4 * EPS);
            }
        }

        [Fact]
        public void OppositeVectorsGiveOppositelyHandedBases()
        {
            var v = new V3(0.3, -0.7, 0.2);

            (V3 p1, V3 q1) = v.nullspace;
            (V3 p2, V3 q2) = (-v).nullspace;

            V3.Cross(p1, q1).ShouldEqual(v.normalized, 4 * EPS);
            V3.Cross(p2, q2).ShouldEqual(-v.normalized, 4 * EPS);
        }

        // the nullspace of the zero vector is all of R^3, which can't be returned as two vectors, and like Julia's and
        // SciPy's nullspace a non-finite input is rejected rather than returning garbage
        [Theory]
        [InlineData(0, 0, 0)]
        [InlineData(NEGATIVE_ZERO, NEGATIVE_ZERO, NEGATIVE_ZERO)]
        [InlineData(double.NaN, 0, 0)]
        [InlineData(1, double.NaN, 1)]
        [InlineData(0, double.PositiveInfinity, 0)]
        [InlineData(0, 0, double.NegativeInfinity)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity)]
        public void ZeroAndNonFiniteVectorsThrow(double x, double y, double z)
        {
            var v = new V3(x, y, z);

            ArgumentException e = Assert.Throws<ArgumentException>(() => v.nullspace);
            Assert.StartsWith("vector must be non-zero and finite", e.Message);
        }
    }
}
