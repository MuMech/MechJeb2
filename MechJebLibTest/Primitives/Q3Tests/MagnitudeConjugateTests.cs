/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.Q3Tests
{
    public class MagnitudeConjugateTests
    {
        [Fact]
        private void MaxMagnitudeIdentity() => Q3.identity.max_magnitude.ShouldEqual(1.0);

        [Fact]
        private void MaxMagnitudeAllPositive()
        {
            new Q3(1, 2, 3, 4).max_magnitude.ShouldEqual(4.0);
            new Q3(5, 2, 3, 4).max_magnitude.ShouldEqual(5.0);
            new Q3(1, 6, 3, 4).max_magnitude.ShouldEqual(6.0);
            new Q3(1, 2, 7, 4).max_magnitude.ShouldEqual(7.0);
        }

        [Fact]
        private void MaxMagnitudeNegativeComponents()
        {
            new Q3(-5, 2, 3, 4).max_magnitude.ShouldEqual(5.0);
            new Q3(1, -6, 3, 4).max_magnitude.ShouldEqual(6.0);
            new Q3(1, 2, -7, 4).max_magnitude.ShouldEqual(7.0);
            new Q3(1, 2, 3, -8).max_magnitude.ShouldEqual(8.0);
        }

        [Fact]
        private void MaxMagnitudeMixedSigns()
        {
            new Q3(-1, -2, -3, -4).max_magnitude.ShouldEqual(4.0);
            new Q3(-10, 5, -3, 8).max_magnitude.ShouldEqual(10.0);
        }

        [Fact]
        private void MaxMagnitudeWithZeros()
        {
            new Q3(0, 0, 0, 5).max_magnitude.ShouldEqual(5.0);
            new Q3(3, 0, 0, 0).max_magnitude.ShouldEqual(3.0);
            new Q3(0, 0, 0, 0).max_magnitude.ShouldEqual(0.0);
        }

        [Fact]
        private void MinMagnitudeIdentity() => Q3.identity.min_magnitude.ShouldEqual(0.0);

        [Fact]
        private void MinMagnitudeAllPositive()
        {
            new Q3(1, 2, 3, 4).min_magnitude.ShouldEqual(1.0);
            new Q3(5, 2, 3, 4).min_magnitude.ShouldEqual(2.0);
            new Q3(5, 6, 3, 4).min_magnitude.ShouldEqual(3.0);
            new Q3(5, 6, 7, 4).min_magnitude.ShouldEqual(4.0);
        }

        [Fact]
        private void MinMagnitudeNegativeComponents()
        {
            new Q3(-1, 2, 3, 4).min_magnitude.ShouldEqual(1.0);
            new Q3(5, -2, 3, 4).min_magnitude.ShouldEqual(2.0);
            new Q3(5, 6, -3, 4).min_magnitude.ShouldEqual(3.0);
            new Q3(5, 6, 7, -4).min_magnitude.ShouldEqual(4.0);
        }

        [Fact]
        private void MinMagnitudeWithZeros()
        {
            new Q3(0, 2, 3, 4).min_magnitude.ShouldEqual(0.0);
            new Q3(1, 0, 3, 4).min_magnitude.ShouldEqual(0.0);
            new Q3(1, 2, 0, 4).min_magnitude.ShouldEqual(0.0);
            new Q3(1, 2, 3, 0).min_magnitude.ShouldEqual(0.0);
        }

        [Fact]
        private void MinMagnitudeAllEqual()
        {
            new Q3(5, 5, 5, 5).min_magnitude.ShouldEqual(5.0);
            new Q3(-3, -3, -3, -3).min_magnitude.ShouldEqual(3.0);
        }

        [Fact]
        private void MagnitudeIdentity() => Q3.identity.magnitude.ShouldEqual(1.0);

        [Fact]
        private void MagnitudeUnitQuaternions()
        {
            new Q3(1, 0, 0, 0).magnitude.ShouldEqual(1.0);
            new Q3(0, 1, 0, 0).magnitude.ShouldEqual(1.0);
            new Q3(0, 0, 1, 0).magnitude.ShouldEqual(1.0);
            new Q3(0, 0, 0, 1).magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void MagnitudeSimpleQuaternions()
        {
            new Q3(1, 1, 1, 1).magnitude.ShouldEqual(2.0);
            new Q3(3, 4, 0, 0).magnitude.ShouldEqual(5.0);
            new Q3(2, 3, 6, 0).magnitude.ShouldEqual(7.0);
        }

        [Fact]
        private void MagnitudeNegativeComponents()
        {
            new Q3(-1, -1, -1, -1).magnitude.ShouldEqual(2.0);
            new Q3(-3, 4, 0, 0).magnitude.ShouldEqual(5.0);
            new Q3(2, -3, 6, 0).magnitude.ShouldEqual(7.0);
        }

        [Fact]
        private void MagnitudeZeroQuaternion() => new Q3(0, 0, 0, 0).magnitude.ShouldEqual(0.0);

        [Fact]
        private void MagnitudeFromAngleAxis()
        {
            var q = Q3.AngleAxis(PI / 3, new V3(1, 2, 3).normalized);
            q.magnitude.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void MagnitudeVeryLargeComponents()
        {
            const double LARGE = 1e150;
            new Q3(LARGE, 0, 0, 0).magnitude.ShouldEqual(LARGE);
            new Q3(LARGE, LARGE, LARGE, LARGE).magnitude.ShouldEqual(2 * LARGE);
        }

        [Fact]
        private void MagnitudeVerySmallComponents()
        {
            const double SMALL = 1e-150;
            new Q3(SMALL, 0, 0, 0).magnitude.ShouldEqual(SMALL);
            new Q3(SMALL, SMALL, SMALL, SMALL).magnitude.ShouldEqual(2 * SMALL);
        }

        [Fact]
        private void MagnitudeAvoidingOverflow()
        {
            const double LARGE = 1e200;
            var          q     = new Q3(LARGE, LARGE, LARGE, LARGE);

            q.magnitude.ShouldEqual(2 * LARGE);
            q.magnitude.ShouldBeFinite();
        }

        [Fact]
        private void MagnitudeAvoidingUnderflow()
        {
            const double SMALL = 1e-200;
            var          q     = new Q3(SMALL, SMALL, SMALL, SMALL);

            q.magnitude.ShouldEqual(2 * SMALL);
            q.magnitude.ShouldBeGreaterThan(0);
        }

        [Fact]
        private void ConjugateIdentity() => Q3.identity.conjugate.ShouldEqual(Q3.identity);

        [Fact]
        private void ConjugateNegatesVectorPart()
        {
            var q    = new Q3(1, 2, 3, 4);
            Q3  conj = q.conjugate;

            conj.x.ShouldEqual(-1);
            conj.y.ShouldEqual(-2);
            conj.z.ShouldEqual(-3);
            conj.w.ShouldEqual(4);
        }

        [Fact]
        private void ConjugatePreservesScalarPart()
        {
            var q = new Q3(5, 6, 7, 8);

            q.conjugate.w.ShouldEqual(q.w);
        }

        [Fact]
        private void ConjugateOfConjugateIsOriginal()
        {
            var q = new Q3(1.5, -2.5, 3.5, -4.5);

            q.conjugate.conjugate.ShouldEqual(q);
        }

        [Fact]
        private void ConjugatePreservesMagnitude()
        {
            var q = new Q3(1, 2, 3, 4);

            q.conjugate.magnitude.ShouldEqual(q.magnitude);
        }

        [Fact]
        private void ConjugateTimesOriginalGivesScalar()
        {
            var q      = Q3.AngleAxis(PI / 4, new V3(1, 2, 3).normalized);
            Q3  result = q * q.conjugate;

            result.x.ShouldBeZero(1e-14);
            result.y.ShouldBeZero(1e-14);
            result.z.ShouldBeZero(1e-14);
            result.w.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void ConjugateOfNormalizedIsInverse()
        {
            var q    = Q3.AngleAxis(0.789, new V3(3, -2, 1).normalized);
            Q3  conj = q.conjugate;
            var inv  = Q3.Inverse(q);

            conj.ShouldEqual(inv, 1e-14);
        }

        [Fact]
        private void ConjugateWithZeroVectorPart()
        {
            var q = new Q3(0, 0, 0, 5);

            q.conjugate.ShouldEqual(q);
        }

        [Fact]
        private void ConjugateWithZeroScalarPart()
        {
            var q    = new Q3(1, 2, 3, 0);
            Q3  conj = q.conjugate;

            conj.x.ShouldEqual(-1);
            conj.y.ShouldEqual(-2);
            conj.z.ShouldEqual(-3);
            conj.w.ShouldEqual(0);
        }

        [Fact]
        private void ConjugateReversesRotation()
        {
            var q = Q3.AngleAxis(PI / 3, V3.up);
            var v = new V3(1, 2, 3);

            V3 rotated   = q * v;
            V3 unrotated = q.conjugate * rotated;

            unrotated.ShouldEqual(v, 1e-14);
        }

        [Fact]
        private void ConjugateWithSpecialValues()
        {
            var q    = new Q3(double.PositiveInfinity, double.NegativeInfinity, 0, 1);
            Q3  conj = q.conjugate;

            conj.x.ShouldBeNegativeInfinity();
            conj.y.ShouldBePositiveInfinity();
            conj.z.ShouldEqual(0);
            conj.w.ShouldEqual(1);
        }

        [Fact]
        private void MagnitudeMaxMinRelationship()
        {
            var q = new Q3(1, 2, 3, 4);

            q.max_magnitude.ShouldBeGreaterThanOrEqual(q.min_magnitude);
            q.magnitude.ShouldBeLessThanOrEqual(2 * q.max_magnitude);
        }
    }
}
