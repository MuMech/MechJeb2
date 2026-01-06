/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.Q3Tests
{
    public class OperationTests
    {
        [Fact]
        private void ConstructorAndAccessors()
        {
            var q = new Q3(1, 2, 3, 4);

            q.x.ShouldEqual(1);
            q.y.ShouldEqual(2);
            q.z.ShouldEqual(3);
            q.w.ShouldEqual(4);
        }

        [Fact]
        private void IndexerAccess()
        {
            var q = new Q3(5, 6, 7, 8);

            q[0].ShouldEqual(5);
            q[1].ShouldEqual(6);
            q[2].ShouldEqual(7);
            q[3].ShouldEqual(8);
        }

        [Fact]
        private void IndexerThrowsOnInvalidIndex()
        {
            var q = new Q3(1, 2, 3, 4);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                double _ = q[4];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                double _ = q[-1];
            });
        }

        [Fact]
        private void IdentityQuaternion()
        {
            Q3 q = Q3.identity;

            q.x.ShouldEqual(0);
            q.y.ShouldEqual(0);
            q.z.ShouldEqual(0);
            q.w.ShouldEqual(1);
        }

        [Fact]
        private void MultiplyQuaternions()
        {
            var q1     = Q3.AngleAxis(PI / 2, V3.up);
            var q2     = Q3.AngleAxis(PI / 2, V3.forward);
            Q3  result = q1 * q2;

            var v        = new V3(1, 0, 0);
            V3  rotated  = result * v;
            V3  expected = q1 * (q2 * v);

            rotated.ShouldEqual(expected, 1e-14);
        }

        [Fact]
        private void MultiplyIdentity()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);

            (q * Q3.identity).ShouldEqual(q, 1e-14);
            (Q3.identity * q).ShouldEqual(q, 1e-14);
        }

        [Fact]
        private void MultiplyQuaternionVector()
        {
            var q = Q3.AngleAxis(PI / 2, V3.up);
            V3  v = V3.forward;

            (q * v).ShouldEqual(V3.left, 1e-14);
        }

        [Fact]
        private void MultiplyPreservesVectorMagnitude()
        {
            var q = Q3.AngleAxis(1.234, new V3(5, -3, 2).normalized);
            var v = new V3(7, 8, 9);

            (q * v).magnitude.ShouldEqual(v.magnitude, 1e-14);
        }

        [Fact]
        private void DotProduct()
        {
            var q1 = new Q3(1, 0, 0, 0);
            var q2 = new Q3(0, 1, 0, 0);

            Q3.Dot(q1, q2).ShouldEqual(0);

            Q3.Dot(q1, q1).ShouldEqual(1);
            Q3.Dot(Q3.identity, Q3.identity).ShouldEqual(1);
        }

        [Fact]
        private void DotProductNormalized()
        {
            var q1 = Q3.AngleAxis(PI / 3, V3.up);
            var q2 = Q3.AngleAxis(PI / 6, V3.up);

            double dot = Q3.Dot(q1, q2);
            dot.ShouldBePositive();
            dot.ShouldBeLessThanOrEqual(1.0);
        }

        [Fact]
        private void AngleBetweenQuaternions()
        {
            var q1 = Q3.AngleAxis(0, V3.xaxis);
            var q2 = Q3.AngleAxis(PI / 2, V3.xaxis);

            double angle = Q3.Angle(q1, q2);
            angle.ShouldEqual(PI / 2, 1e-14);
        }

        [Fact]
        private void AngleIdenticalQuaternions()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);

            Q3.Angle(q, q).ShouldEqual(0, 1e-14);

            Q3.Angle(Q3.identity, Q3.identity).ShouldEqual(0);
        }

        [Fact]
        private void AngleOppositeQuaternions()
        {
            var q1 = new Q3(0.5, 0.5, 0.5, 0.5);
            var q2 = new Q3(-0.5, -0.5, -0.5, -0.5);

            Q3.Angle(q1, q2).ShouldEqual(0, 1e-14);
        }

        [Fact]
        private void AngleSmallDifference()
        {
            Q3  q1 = Q3.identity;
            var q2 = Q3.AngleAxis(1e-8, V3.up);

            Q3.Angle(q1, q2).ShouldEqual(1e-8, 1e-16);
        }

        [Fact]
        private void EqualityOperator()
        {
            var q1 = new Q3(1, 2, 3, 4);
            var q2 = new Q3(1, 2, 3, 4);
            var q3 = new Q3(1, 2, 3, 4.001);

            (q1 == q2).ShouldBeTrue();
            (q1 == q3).ShouldBeFalse();
        }

        [Fact]
        private void EqualityWithNegativeEquivalent()
        {
            var q1 = new Q3(0.5, 0.5, 0.5, 0.5);
            var q2 = new Q3(-0.5, -0.5, -0.5, -0.5);

            (q1 == q2).ShouldBeFalse();
        }

        [Fact]
        private void InequalityOperator()
        {
            var q1 = new Q3(1, 2, 3, 4);
            var q2 = new Q3(1, 2, 3, 4);
            var q3 = new Q3(1, 2, 3.001, 4);

            (q1 != q2).ShouldBeFalse();
            (q1 != q3).ShouldBeTrue();
        }

        [Fact]
        private void EqualityWithNaN()
        {
            var q1 = new Q3(double.NaN, 2, 3, 4);
            var q2 = new Q3(double.NaN, 2, 3, 4);

            (q1 == q2).ShouldBeFalse();
            (q1 != q2).ShouldBeTrue();
        }

        [Fact]
        private void ToEulerAnglesIdentity()
        {
            V3 euler = Q3.identity.eulerAngles;

            euler.ShouldEqual(V3.zero, 1e-14);
        }

        [Fact]
        private void ToEulerAnglesRoll()
        {
            var q     = Q3.AngleAxis(PI / 4, V3.forward);
            V3  euler = q.eulerAngles;

            euler.roll.ShouldEqual(PI / 4, 1e-14);
            euler.pitch.ShouldBeZero(1e-14);
            euler.yaw.ShouldBeZero(1e-14);
        }

        [Fact]
        private void ToEulerAnglesPitch()
        {
            var q     = Q3.AngleAxis(PI / 4, V3.left);
            V3  euler = q.eulerAngles;

            euler.pitch.ShouldEqual(-PI / 4, 1e-14);
            euler.roll.ShouldBeZero(1e-14);
            euler.yaw.ShouldBeZero(1e-14);
        }

        [Fact]
        private void ToEulerAnglesYaw()
        {
            var q     = Q3.AngleAxis(PI / 4, V3.up);
            V3  euler = q.eulerAngles;

            euler.yaw.ShouldEqual(-PI / 4, 1e-14);
            euler.roll.ShouldBeZero(1e-14);
            euler.pitch.ShouldBeZero(1e-14);
        }

        [Fact]
        private void ToEulerAnglesCombined()
        {
            double roll  = PI / 6;
            double pitch = PI / 4;
            double yaw   = PI / 3;

            var qRoll  = Q3.AngleAxis(roll, V3.forward);
            var qPitch = Q3.AngleAxis(pitch, V3.left);
            var qYaw   = Q3.AngleAxis(yaw, V3.up);
            Q3  q      = qYaw * qPitch * qRoll;

            V3 euler = Q3.ToEulerAngles(q);

            euler.x.ShouldEqual(roll, 1e-13);
            euler.y.ShouldEqual(-pitch, 1e-13);
            euler.z.ShouldEqual(-yaw, 1e-13);
        }

        [Fact]
        private void ToEulerAnglesGimbalLock()
        {
            var q     = Q3.AngleAxis(PI / 2, V3.left);
            V3  euler = q.eulerAngles;

            euler.pitch.ShouldEqual(-PI / 2, 1e-14);
        }

        [Fact]
        private void MultiplyNonCommutative()
        {
            var q1 = Q3.AngleAxis(PI / 3, V3.up);
            var q2 = Q3.AngleAxis(PI / 4, V3.forward);

            (q1 * q2).ShouldNotEqual(q2 * q1, 1e-14);
        }

        [Fact]
        private void MultiplyAssociative()
        {
            var q1 = Q3.AngleAxis(0.5, new V3(1, 0, 0));
            var q2 = Q3.AngleAxis(0.7, new V3(0, 1, 0));
            var q3 = Q3.AngleAxis(0.3, new V3(0, 0, 1));

            (q1 * q2 * q3).ShouldEqual(q1 * (q2 * q3), 1e-14);
        }

        [Fact]
        private void RotateVectorChaining()
        {
            var q1 = Q3.AngleAxis(PI / 4, V3.up);
            var q2 = Q3.AngleAxis(PI / 3, V3.forward);
            var v  = new V3(1, 2, 3);

            V3 result1 = q1 * q2 * v;
            V3 result2 = q1 * (q2 * v);

            result1.ShouldEqual(result2, 1e-14);
        }

        [Fact]
        private void QuaternionMagnitudeOne()
        {
            var q = Q3.AngleAxis(1.234, new V3(5, -3, 7).normalized);

            double mag = Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void LargeAngleNormalization()
        {
            var q1 = Q3.AngleAxis(8 * PI + 0.5, V3.up);
            var q2 = Q3.AngleAxis(0.5, V3.up);

            q1.ShouldEqual(q2, 1e-14);
        }

        [Fact]
        private void HashCodeConsistency()
        {
            var q     = new Q3(1, 2, 3, 4);
            int hash1 = q.GetHashCode();
            int hash2 = q.GetHashCode();

            hash1.ShouldEqual(hash2);
        }

        [Fact]
        private void EqualsMethod()
        {
            var q1 = new Q3(1, 2, 3, 4);
            var q2 = new Q3(1, 2, 3, 4);
            var q3 = new Q3(1, 2, 3, 5);

            q1.Equals(q2).ShouldBeTrue();
            q1.Equals(q3).ShouldBeFalse();
        }

        [Fact]
        private void EqualsWithObject()
        {
            var    q1   = new Q3(1, 2, 3, 4);
            object q2   = new Q3(1, 2, 3, 4);
            object notQ = "not a quaternion";

            q1.Equals(q2).ShouldBeTrue();
            q1.Equals(notQ).ShouldBeFalse();
            q1.Equals(null).ShouldBeFalse();
        }

        [Fact]
        private void ToStringFormat()
        {
            var q = new Q3(1.5, 2.5, 3.5, 4.5);

            q.ToString().ShouldEqual("(1.5, 2.5, 3.5, 4.5)");
            q.ToString("F2").ShouldEqual("(1.50, 2.50, 3.50, 4.50)");
        }

        [Fact]
        private void ScalarMultiplicationRight()
        {
            var q      = new Q3(1, 2, 3, 4);
            Q3  result = q * 2;

            result.x.ShouldEqual(2);
            result.y.ShouldEqual(4);
            result.z.ShouldEqual(6);
            result.w.ShouldEqual(8);
        }

        [Fact]
        private void ScalarMultiplicationLeft()
        {
            var q      = new Q3(1, 2, 3, 4);
            Q3  result = 2 * q;

            result.x.ShouldEqual(2);
            result.y.ShouldEqual(4);
            result.z.ShouldEqual(6);
            result.w.ShouldEqual(8);
        }

        [Fact]
        private void ScalarMultiplicationIsCommutative()
        {
            var q = new Q3(1.5, -2.7, 3.2, 0.8);

            (q * 3.5).ShouldEqual(3.5 * q);
        }

        [Fact]
        private void ScalarMultiplicationByZero()
        {
            var q = new Q3(1, 2, 3, 4);

            (q * 0).ShouldEqual(new Q3(0, 0, 0, 0));
            (0 * q).ShouldEqual(new Q3(0, 0, 0, 0));
        }

        [Fact]
        private void ScalarMultiplicationByOne()
        {
            var q = new Q3(1.5, -2.7, 3.2, 0.8);

            (q * 1).ShouldEqual(q);
            (1 * q).ShouldEqual(q);
        }

        [Fact]
        private void ScalarMultiplicationByNegative()
        {
            var q      = new Q3(1, 2, 3, 4);
            Q3  result = q * -1;

            result.ShouldEqual(new Q3(-1, -2, -3, -4));
        }

        [Fact]
        private void ScalarMultiplicationWithIdentity()
        {
            Q3 result = Q3.identity * 5;

            result.x.ShouldEqual(0);
            result.y.ShouldEqual(0);
            result.z.ShouldEqual(0);
            result.w.ShouldEqual(5);
        }

        [Fact]
        private void ScalarMultiplicationLargeValues()
        {
            var q      = new Q3(1e150, 2e150, 3e150, 4e150);
            Q3  result = q * 2;

            result.x.ShouldEqual(2e150);
            result.y.ShouldEqual(4e150);
            result.z.ShouldEqual(6e150);
            result.w.ShouldEqual(8e150);
        }

        [Fact]
        private void ScalarMultiplicationSmallValues()
        {
            var q      = new Q3(1e-150, 2e-150, 3e-150, 4e-150);
            Q3  result = q * 2;

            result.x.ShouldEqual(2e-150);
            result.y.ShouldEqual(4e-150);
            result.z.ShouldEqual(6e-150);
            result.w.ShouldEqual(8e-150);
        }

        [Fact]
        private void ScalarDivision()
        {
            var q      = new Q3(2, 4, 6, 8);
            Q3  result = q / 2;

            result.x.ShouldEqual(1);
            result.y.ShouldEqual(2);
            result.z.ShouldEqual(3);
            result.w.ShouldEqual(4);
        }

        [Fact]
        private void ScalarDivisionByOne()
        {
            var q = new Q3(1.5, -2.7, 3.2, 0.8);

            (q / 1).ShouldEqual(q);
        }

        [Fact]
        private void ScalarDivisionByNegative()
        {
            var q      = new Q3(2, 4, 6, 8);
            Q3  result = q / -2;

            result.ShouldEqual(new Q3(-1, -2, -3, -4));
        }

        [Fact]
        private void ScalarDivisionByZeroProducesInfinity()
        {
            var q      = new Q3(1, -2, 3, -4);
            Q3  result = q / 0;

            result.x.ShouldBePositiveInfinity();
            result.y.ShouldBeNegativeInfinity();
            result.z.ShouldBePositiveInfinity();
            result.w.ShouldBeNegativeInfinity();
        }

        [Fact]
        private void ScalarDivisionZeroByZeroProducesNaN()
        {
            var q      = new Q3(0, 0, 0, 0);
            Q3  result = q / 0;

            result.x.ShouldBeNaN();
            result.y.ShouldBeNaN();
            result.z.ShouldBeNaN();
            result.w.ShouldBeNaN();
        }

        [Fact]
        private void ScalarDivisionLargeValues()
        {
            var q      = new Q3(2e150, 4e150, 6e150, 8e150);
            Q3  result = q / 2;

            result.x.ShouldEqual(1e150);
            result.y.ShouldEqual(2e150);
            result.z.ShouldEqual(3e150);
            result.w.ShouldEqual(4e150);
        }

        [Fact]
        private void ScalarDivisionSmallValues()
        {
            var q      = new Q3(2e-150, 4e-150, 6e-150, 8e-150);
            Q3  result = q / 2;

            result.x.ShouldEqual(1e-150);
            result.y.ShouldEqual(2e-150);
            result.z.ShouldEqual(3e-150);
            result.w.ShouldEqual(4e-150);
        }

        [Fact]
        private void ScalarMultiplyThenDivideRoundTrip()
        {
            var q = new Q3(1, 2, 3, 4);

            (q * 5 / 5).ShouldEqual(q);
        }

        [Fact]
        private void UnaryNegation()
        {
            var q      = new Q3(1, -2, 3, -4);
            Q3  result = -q;

            result.x.ShouldEqual(-1);
            result.y.ShouldEqual(2);
            result.z.ShouldEqual(-3);
            result.w.ShouldEqual(4);
        }

        [Fact]
        private void UnaryNegationOfIdentity()
        {
            Q3 result = -Q3.identity;

            result.x.ShouldEqual(0);
            result.y.ShouldEqual(0);
            result.z.ShouldEqual(0);
            result.w.ShouldEqual(-1);
        }

        [Fact]
        private void UnaryNegationOfZeroQuaternion()
        {
            var q = new Q3(0, 0, 0, 0);

            (-q).ShouldEqual(q);
        }

        [Fact]
        private void DoubleNegation()
        {
            var q = new Q3(1.5, -2.7, 3.2, 0.8);

            (- -q).ShouldEqual(q);
        }

        [Fact]
        private void NegationEquivalentToMultiplyByNegativeOne()
        {
            var q = new Q3(1, 2, 3, 4);

            (-q).ShouldEqual(q * -1);
            (-q).ShouldEqual(-1 * q);
        }

        [Fact]
        private void NegatedQuaternionRepresentsSameRotation()
        {
            var q = Q3.AngleAxis(PI / 3, new V3(1, 2, 3).normalized);
            var v = new V3(5, 6, 7);

            (q * v).ShouldEqual(-q * v, 1e-14);
        }

        [Fact]
        private void NegationWithSpecialValues()
        {
            var q      = new Q3(double.PositiveInfinity, double.NegativeInfinity, double.NaN, 0);
            Q3  result = -q;

            result.x.ShouldBeNegativeInfinity();
            result.y.ShouldBePositiveInfinity();
            result.z.ShouldBeNaN();
            result.w.ShouldEqual(0);
        }

        [Fact]
        private void NegationLargeValues()
        {
            var q      = new Q3(1e300, -1e300, 1e-300, -1e-300);
            Q3  result = -q;

            result.x.ShouldEqual(-1e300);
            result.y.ShouldEqual(1e300);
            result.z.ShouldEqual(-1e-300);
            result.w.ShouldEqual(1e-300);
        }

        [Fact]
        private void ScalarOperatorsPrecedence()
        {
            var q = new Q3(1, 2, 3, 4);

            (q * 2 / 2).ShouldEqual(q);
            (2 * q / 2).ShouldEqual(q);
        }

        [Fact]
        private void ScalarMultiplicationDistributive()
        {
            var          q1 = new Q3(1, 2, 3, 4);
            var          q2 = new Q3(5, 6, 7, 8);
            const double s  = 3;

            var sum       = new Q3(q1.x + q2.x, q1.y + q2.y, q1.z + q2.z, q1.w + q2.w);
            var scaled1   = new Q3(q1.x * s, q1.y * s, q1.z * s, q1.w * s);
            var scaled2   = new Q3(q2.x * s, q2.y * s, q2.z * s, q2.w * s);
            var scaledSum = new Q3(scaled1.x + scaled2.x, scaled1.y + scaled2.y, scaled1.z + scaled2.z, scaled1.w + scaled2.w);

            (sum * s).ShouldEqual(scaledSum);
        }
    }
}
