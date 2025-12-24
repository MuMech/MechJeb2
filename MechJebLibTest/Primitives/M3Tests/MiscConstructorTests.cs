/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class MiscConstructorTests
    {
        [Fact]
        private void AngleAxisZeroAngle()
        {
            var m = M3.AngleAxis(0, V3.xaxis);
            m.ShouldEqual(M3.identity);
        }

        [Fact]
        private void AngleAxisAroundX()
        {
            var m = M3.AngleAxis(PI / 2, V3.xaxis);

            (m * V3.yaxis).ShouldEqual(V3.zaxis, 1e-14);
            (m * V3.zaxis).ShouldEqual(-V3.yaxis, 1e-14);
            (m * V3.xaxis).ShouldEqual(V3.xaxis, 1e-14);
        }

        [Fact]
        private void AngleAxisAroundY()
        {
            var m = M3.AngleAxis(PI / 2, V3.yaxis);

            (m * V3.zaxis).ShouldEqual(V3.xaxis, 1e-14);
            (m * V3.xaxis).ShouldEqual(-V3.zaxis, 1e-14);
            (m * V3.yaxis).ShouldEqual(V3.yaxis, 1e-14);
        }

        [Fact]
        private void AngleAxisAroundZ()
        {
            var m = M3.AngleAxis(PI / 2, V3.zaxis);

            (m * V3.xaxis).ShouldEqual(V3.yaxis, 1e-14);
            (m * V3.yaxis).ShouldEqual(-V3.xaxis, 1e-14);
            (m * V3.zaxis).ShouldEqual(V3.zaxis, 1e-14);
        }

        [Fact]
        private void AngleAxis180Degrees()
        {
            var m = M3.AngleAxis(PI, V3.xaxis);

            (m * V3.xaxis).ShouldEqual(V3.xaxis, 1e-14);
            (m * V3.yaxis).ShouldEqual(-V3.yaxis, 1e-14);
            (m * V3.zaxis).ShouldEqual(-V3.zaxis, 1e-14);
        }

        [Fact]
        private void AngleAxisNegativeAngle()
        {
            var mPos = M3.AngleAxis(PI / 4, V3.zaxis);
            var mNeg = M3.AngleAxis(-PI / 4, V3.zaxis);

            (mPos * mNeg).ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void AngleAxisArbitraryAxis()
        {
            V3 axis = new V3(1, 1, 1).normalized;
            var m = M3.AngleAxis(PI / 3, axis);

            m.isOrthogonal.ShouldBeTrue();
            m.determinant.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void AngleAxisNormalizesAxis()
        {
            var m1 = M3.AngleAxis(PI / 4, new V3(2, 0, 0));
            var m2 = M3.AngleAxis(PI / 4, V3.xaxis);

            m1.ShouldEqual(m2, 1e-14);
        }

        [Fact]
        private void AngleAxisPreservesAxisVector()
        {
            V3 axis = new V3(1, 2, 3).normalized;
            var m = M3.AngleAxis(1.234, axis);

            (m * axis).ShouldEqual(axis, 1e-14);
        }

        [Fact]
        private void AngleAxisMatchesQuaternion()
        {
            V3 axis = new V3(1, 2, 3).normalized;
            double angle = 0.789;

            var mFromAngleAxis = M3.AngleAxis(angle, axis);
            var q = Q3.AngleAxis(angle, axis);
            var mFromQuat = M3.Rotate(q);

            mFromAngleAxis.ShouldEqual(mFromQuat, 1e-14);
        }

        [Fact]
        private void AngleAxisLargeAngle()
        {
            var m1 = M3.AngleAxis(4 * PI + PI / 3, V3.zaxis);
            var m2 = M3.AngleAxis(PI / 3, V3.zaxis);

            m1.ShouldEqual(m2, 1e-14);
        }

        [Fact]
        private void AngleAxisSmallAngle()
        {
            double small = 1e-10;
            var m = M3.AngleAxis(small, V3.zaxis);

            m.m00.ShouldEqual(1.0, 1e-20);
            m.m11.ShouldEqual(1.0, 1e-20);
            m.m01.ShouldEqual(-small, 1e-20);
            m.m10.ShouldEqual(small, 1e-20);
        }

        [Fact]
        private void AngleAxisComposition()
        {
            var m1 = M3.AngleAxis(PI / 6, V3.xaxis);
            var m2 = M3.AngleAxis(PI / 4, V3.xaxis);
            var m3 = M3.AngleAxis(PI / 6 + PI / 4, V3.xaxis);

            (m2 * m1).ShouldEqual(m3, 1e-14);
        }

        [Fact]
        private void AngleAxisInverseIsNegativeAngle()
        {
            V3 axis = new V3(1, 2, 3).normalized;
            double angle = 0.5;

            var m = M3.AngleAxis(angle, axis);
            var mInv = M3.AngleAxis(-angle, axis);

            (m * mInv).ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void AngleAxisInverseIsTranspose()
        {
            V3 axis = new V3(1, 2, 3).normalized;
            var m = M3.AngleAxis(0.789, axis);

            (m * m.transpose).ShouldEqual(M3.identity, 1e-14);
        }

        [Fact]
        private void AngleAxisTraceFormula()
        {
            double angle = PI / 5;
            V3 axis = new V3(3, -2, 1).normalized;
            var m = M3.AngleAxis(angle, axis);

            m.trace.ShouldEqual(1 + 2 * Cos(angle), 1e-14);
        }

        [Fact]
        private void FromQuaternionIdentity()
        {
            var m = M3.FromQuaternion(Q3.identity);
            m.ShouldEqual(M3.identity);
        }

        [Fact]
        private void FromQuaternionMatchesRotate()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);

            var m1 = M3.FromQuaternion(q);
            var m2 = M3.Rotate(q);

            m1.ShouldEqual(m2);
        }

        [Fact]
        private void FromQuaternion90DegreesX()
        {
            var q = Q3.AngleAxis(PI / 2, V3.xaxis);
            var m = M3.FromQuaternion(q);

            (m * V3.yaxis).ShouldEqual(V3.zaxis, 1e-14);
            (m * V3.zaxis).ShouldEqual(-V3.yaxis, 1e-14);
        }

        [Fact]
        private void FromQuaternion90DegreesY()
        {
            var q = Q3.AngleAxis(PI / 2, V3.yaxis);
            var m = M3.FromQuaternion(q);

            (m * V3.zaxis).ShouldEqual(V3.xaxis, 1e-14);
            (m * V3.xaxis).ShouldEqual(-V3.zaxis, 1e-14);
        }

        [Fact]
        private void FromQuaternion90DegreesZ()
        {
            var q = Q3.AngleAxis(PI / 2, V3.zaxis);
            var m = M3.FromQuaternion(q);

            (m * V3.xaxis).ShouldEqual(V3.yaxis, 1e-14);
            (m * V3.yaxis).ShouldEqual(-V3.xaxis, 1e-14);
        }

        [Fact]
        private void FromQuaternionArbitrary()
        {
            var q = Q3.AngleAxis(0.789, new V3(3, -2, 1).normalized);
            var m = M3.FromQuaternion(q);

            m.isOrthogonal.ShouldBeTrue();
            m.determinant.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void FromQuaternionComposition()
        {
            var q1 = Q3.AngleAxis(PI / 3, V3.xaxis);
            var q2 = Q3.AngleAxis(PI / 4, V3.yaxis);

            var m1 = M3.FromQuaternion(q1);
            var m2 = M3.FromQuaternion(q2);
            var mCombined = M3.FromQuaternion(q2 * q1);

            (m2 * m1).ShouldEqual(mCombined, 1e-14);
        }

        [Fact]
        private void FromQuaternionVectorRotation()
        {
            var q = Q3.AngleAxis(0.5, new V3(1, 1, 1).normalized);
            var m = M3.FromQuaternion(q);
            var v = new V3(3, 4, 5);

            (m * v).ShouldEqual(q * v, 1e-14);
        }

        [Fact]
        private void LerpAtZero()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            M3.Lerp(a, b, 0).ShouldEqual(a);
        }

        [Fact]
        private void LerpAtOne()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            M3.Lerp(a, b, 1).ShouldEqual(b);
        }

        [Fact]
        private void LerpAtHalf()
        {
            var a = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            var b = new M3(2, 4, 6, 8, 10, 12, 14, 16, 18);

            var expected = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            M3.Lerp(a, b, 0.5).ShouldEqual(expected);
        }

        [Fact]
        private void LerpIdentityToZero()
        {
            var result = M3.Lerp(M3.identity, M3.zero, 0.5);

            result.m00.ShouldEqual(0.5);
            result.m11.ShouldEqual(0.5);
            result.m22.ShouldEqual(0.5);
            result.m01.ShouldEqual(0);
        }

        [Fact]
        private void LerpClampsNegative()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            M3.Lerp(a, b, -0.5).ShouldEqual(a);
        }

        [Fact]
        private void LerpClampsGreaterThanOne()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            M3.Lerp(a, b, 1.5).ShouldEqual(b);
        }

        [Fact]
        private void LerpQuarterWay()
        {
            var a = new M3(0, 0, 0, 0, 0, 0, 0, 0, 0);
            var b = new M3(4, 8, 12, 16, 20, 24, 28, 32, 36);

            var expected = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            M3.Lerp(a, b, 0.25).ShouldEqual(expected);
        }

        [Fact]
        private void LerpSymmetric()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            var result1 = M3.Lerp(a, b, 0.3);
            var result2 = M3.Lerp(b, a, 0.7);

            result1.ShouldEqual(result2, 1e-14);
        }

        [Fact]
        private void LerpSameMatrix()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            M3.Lerp(m, m, 0.5).ShouldEqual(m);
        }

        [Fact]
        private void LerpPreservesSymmetry()
        {
            var a = new M3(1, 2, 3, 2, 4, 5, 3, 5, 6);
            var b = new M3(6, 5, 4, 5, 3, 2, 4, 2, 1);

            var result = M3.Lerp(a, b, 0.5);
            result.isSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void LerpWithNegativeValues()
        {
            var a = new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9);
            var b = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            M3.Lerp(a, b, 0.5).ShouldEqual(M3.zero);
        }

        [Fact]
        private void LerpLargeValues()
        {
            var a = M3.Diagonal(1e100, 1e100, 1e100);
            var b = M3.Diagonal(3e100, 3e100, 3e100);

            var result = M3.Lerp(a, b, 0.5);
            result.m00.ShouldEqual(2e100);
            result.m11.ShouldEqual(2e100);
            result.m22.ShouldEqual(2e100);
        }

        [Fact]
        private void LerpSmallValues()
        {
            var a = M3.Diagonal(1e-100, 1e-100, 1e-100);
            var b = M3.Diagonal(3e-100, 3e-100, 3e-100);

            var result = M3.Lerp(a, b, 0.5);
            result.m00.ShouldEqual(2e-100);
            result.m11.ShouldEqual(2e-100);
            result.m22.ShouldEqual(2e-100);
        }
    }
}
