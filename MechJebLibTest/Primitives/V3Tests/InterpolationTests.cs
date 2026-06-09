/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class InterpolationTests
    {
        [Fact]
        private void LerpAtZeroReturnsStart()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            V3.Lerp(a, b, 0).ShouldEqual(a);
        }

        [Fact]
        private void LerpAtOneReturnsEnd()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            V3.Lerp(a, b, 1).ShouldEqual(b);
        }

        [Fact]
        private void LerpAtHalfReturnsMidpoint()
        {
            var a = new V3(0, 0, 0);
            var b = new V3(10, 20, 30);

            V3.Lerp(a, b, 0.5).ShouldEqual(new V3(5, 10, 15));
        }

        [Fact]
        private void LerpQuarterAndThreeQuarter()
        {
            var a = new V3(0, 0, 0);
            var b = new V3(8, 16, 24);

            V3.Lerp(a, b, 0.25).ShouldEqual(new V3(2, 4, 6));
            V3.Lerp(a, b, 0.75).ShouldEqual(new V3(6, 12, 18));
        }

        [Fact]
        private void LerpWithNegativeComponents()
        {
            var a = new V3(-10, 5, -3);
            var b = new V3(10, -5, 3);

            V3.Lerp(a, b, 0.5).ShouldEqual(new V3(0, 0, 0));
        }

        [Fact]
        private void LerpIdenticalVectors()
        {
            var v = new V3(3, 4, 5);

            V3.Lerp(v, v, 0).ShouldEqual(v);
            V3.Lerp(v, v, 0.5).ShouldEqual(v);
            V3.Lerp(v, v, 1).ShouldEqual(v);
        }

        [Fact]
        private void LerpWithZeroVectors()
        {
            var v = new V3(6, 8, 10);

            V3.Lerp(V3.zero, v, 0.5).ShouldEqual(new V3(3, 4, 5));
            V3.Lerp(v, V3.zero, 0.5).ShouldEqual(new V3(3, 4, 5));
            V3.Lerp(V3.zero, V3.zero, 0.5).ShouldEqual(V3.zero);
        }

        [Fact]
        private void LerpExtrapolatesBeyondOne()
        {
            var a = new V3(0, 0, 0);
            var b = new V3(10, 20, 30);

            V3.Lerp(a, b, 2).ShouldEqual(new V3(20, 40, 60));
            V3.Lerp(a, b, 1.5).ShouldEqual(new V3(15, 30, 45));
        }

        [Fact]
        private void LerpExtrapolatesBelowZero()
        {
            var a = new V3(10, 20, 30);
            var b = new V3(20, 40, 60);

            V3.Lerp(a, b, -1).ShouldEqual(new V3(0, 0, 0));
            V3.Lerp(a, b, -0.5).ShouldEqual(new V3(5, 10, 15));
        }

        [Fact]
        private void LerpIsLinear()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(5, 10, 15);

            var at25 = V3.Lerp(a, b, 0.25);
            var at75 = V3.Lerp(a, b, 0.75);
            var midpoint = V3.Lerp(at25, at75, 0.5);

            midpoint.ShouldEqual(V3.Lerp(a, b, 0.5), 1e-14);
        }

        [Fact]
        private void LerpWithLargeValues()
        {
            var a = new V3(1e100, 1e100, 1e100);
            var b = new V3(2e100, 2e100, 2e100);

            V3.Lerp(a, b, 0.5).ShouldEqual(new V3(1.5e100, 1.5e100, 1.5e100));
        }

        [Fact]
        private void LerpWithSmallValues()
        {
            var a = new V3(1e-100, 1e-100, 1e-100);
            var b = new V3(2e-100, 2e-100, 2e-100);

            V3.Lerp(a, b, 0.5).ShouldEqual(new V3(1.5e-100, 1.5e-100, 1.5e-100));
        }

        [Fact]
        private void SlerpAtZeroReturnsStart()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 1, 0);

            V3.Slerp(a, b, 0).ShouldEqual(a, 1e-14);
        }

        [Fact]
        private void SlerpAtOneReturnsEnd()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 1, 0);

            V3.Slerp(a, b, 1).ShouldEqual(b, 1e-14);
        }

        [Fact]
        private void SlerpAtHalfBisectsAngle()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 1, 0);

            var result = V3.Slerp(a, b, 0.5);
            V3 expected = new V3(1, 1, 0).normalized;

            result.ShouldEqual(expected, 1e-14);
        }

        [Fact]
        private void SlerpPreservesMagnitudeWithEqualLengths()
        {
            var a = new V3(3, 0, 0);
            var b = new V3(0, 3, 0);

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(3, 1e-14);
        }

        [Fact]
        private void SlerpInterpolatesMagnitude()
        {
            var a = new V3(2, 0, 0);
            var b = new V3(0, 4, 0);

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(3, 1e-14);
        }

        [Fact]
        private void SlerpWithParallelVectors()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(2, 0, 0);

            var result = V3.Slerp(a, b, 0.5);

            result.ShouldEqual(new V3(1.5, 0, 0), 1e-14);
        }

        [Fact]
        private void SlerpWithAntiparallelVectors()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(-1, 0, 0);

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(1, 1e-14);
            V3.Dot(result, a).ShouldBeZero(1e-14);
        }

        [Fact]
        private void SlerpWithZeroStartFallsBackToLerp()
        {
            V3 a = V3.zero;
            var b = new V3(2, 0, 0);

            V3.Slerp(a, b, 0).ShouldEqual(V3.zero);
            V3.Slerp(a, b, 0.5).ShouldEqual(new V3(1, 0, 0));
            V3.Slerp(a, b, 1).ShouldEqual(b);
        }

        [Fact]
        private void SlerpWithZeroEndFallsBackToLerp()
        {
            var a = new V3(2, 0, 0);
            V3 b = V3.zero;

            V3.Slerp(a, b, 0).ShouldEqual(a);
            V3.Slerp(a, b, 0.5).ShouldEqual(new V3(1, 0, 0));
            V3.Slerp(a, b, 1).ShouldEqual(V3.zero);
        }

        [Fact]
        private void SlerpWithBothZeroVectors()
        {
            V3.Slerp(V3.zero, V3.zero, 0).ShouldEqual(V3.zero);
            V3.Slerp(V3.zero, V3.zero, 0.5).ShouldEqual(V3.zero);
            V3.Slerp(V3.zero, V3.zero, 1).ShouldEqual(V3.zero);
        }

        [Fact]
        private void SlerpIdenticalVectors()
        {
            var v = new V3(3, 4, 5);

            V3.Slerp(v, v, 0).ShouldEqual(v, 1e-14);
            V3.Slerp(v, v, 0.5).ShouldEqual(v, 1e-14);
            V3.Slerp(v, v, 1).ShouldEqual(v, 1e-14);
        }

        [Fact]
        private void SlerpMaintainsConstantAngularVelocity()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 0, 1);

            var at25 = V3.Slerp(a, b, 0.25);
            var at50 = V3.Slerp(a, b, 0.5);
            var at75 = V3.Slerp(a, b, 0.75);

            double angle1 = V3.Angle(a, at25);
            double angle2 = V3.Angle(at25, at50);
            double angle3 = V3.Angle(at50, at75);
            double angle4 = V3.Angle(at75, b);

            angle1.ShouldEqual(angle2, 1e-14);
            angle2.ShouldEqual(angle3, 1e-14);
            angle3.ShouldEqual(angle4, 1e-14);
        }

        [Fact]
        private void SlerpAt90Degrees()
        {
            var a = new V3(5, 0, 0);
            var b = new V3(0, 5, 0);

            double totalAngle = V3.Angle(a, b);
            totalAngle.ShouldEqual(PI / 2, 1e-14);

            var midpoint = V3.Slerp(a, b, 0.5);
            V3.Angle(a, midpoint).ShouldEqual(PI / 4, 1e-14);
            V3.Angle(midpoint, b).ShouldEqual(PI / 4, 1e-14);
        }

        [Fact]
        private void SlerpAt180Degrees()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(-1, 0, 0);

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(1, 1e-14);
            V3.Dot(result, a).ShouldBeZero(1e-14);
        }

        [Fact]
        private void SlerpSmallAngle()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(Cos(1e-6), Sin(1e-6), 0);

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(1, 1e-14);
            V3.Angle(a, result).ShouldEqual(0.5e-6, 1e-12);
        }

        [Fact]
        private void SlerpIn3D()
        {
            var a = new V3(1, 0, 0);
            V3 b = new V3(0, 1, 1).normalized;

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(1, 1e-14);

            double angleToA = V3.Angle(a, result);
            double angleToB = V3.Angle(result, b);
            angleToA.ShouldEqual(angleToB, 1e-14);
        }

        [Fact]
        private void SlerpWithDifferentMagnitudesInterpolatesMagnitudeLinearly()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 5, 0);

            V3.Slerp(a, b, 0).magnitude.ShouldEqual(1, 1e-14);
            V3.Slerp(a, b, 0.25).magnitude.ShouldEqual(2, 1e-14);
            V3.Slerp(a, b, 0.5).magnitude.ShouldEqual(3, 1e-14);
            V3.Slerp(a, b, 0.75).magnitude.ShouldEqual(4, 1e-14);
            V3.Slerp(a, b, 1).magnitude.ShouldEqual(5, 1e-14);
        }

        [Fact]
        private void SlerpWithLargeVectors()
        {
            var a = new V3(1e100, 0, 0);
            var b = new V3(0, 1e100, 0);

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(1e100, 1e86);
            V3.Angle(a, result).ShouldEqual(PI / 4, 1e-14);
        }

        [Fact]
        private void SlerpWithSmallVectors()
        {
            var a = new V3(1e-100, 0, 0);
            var b = new V3(0, 1e-100, 0);

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(1e-100, 1e-114);
            V3.Angle(a, result).ShouldEqual(PI / 4, 1e-14);
        }

        [Fact]
        private void SlerpExtrapolatesBeyondOne()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 1, 0);

            var result = V3.Slerp(a, b, 2);

            result.ShouldEqual(new V3(-1, 0, 0), 1e-14);
        }

        [Fact]
        private void SlerpExtrapolatesBelowZero()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 1, 0);

            var result = V3.Slerp(a, b, -1);

            result.ShouldEqual(new V3(0, -1, 0), 1e-14);
        }

        [Fact]
        private void LerpVsSlerpForCollinearVectors()
        {
            var a = new V3(2, 0, 0);
            var b = new V3(4, 0, 0);

            var lerped = V3.Lerp(a, b, 0.5);
            var slerped = V3.Slerp(a, b, 0.5);

            lerped.ShouldEqual(slerped, 1e-14);
        }

        [Fact]
        private void SlerpNearlyParallelVectors()
        {
            var a = new V3(1, 0, 0);
            V3 b = new V3(1, 1e-10, 0).normalized;

            var result = V3.Slerp(a, b, 0.5);

            result.magnitude.ShouldEqual(1, 1e-14);
            result.x.ShouldBePositive();
        }
    }
}
