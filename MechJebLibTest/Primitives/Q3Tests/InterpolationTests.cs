/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.Q3Tests
{
    public class InterpolationTests
    {
        [Fact]
        private void LerpAtZeroReturnsStart()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI / 2, V3.up);

            Q3.Lerp(a, b, 0).ShouldEqual(a, 1e-14);
        }

        [Fact]
        private void LerpAtOneReturnsEnd()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI / 2, V3.up);

            Q3.Lerp(a, b, 1).ShouldEqual(b, 1e-14);
        }

        [Fact]
        private void LerpAtHalfReturnsMidpoint()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI / 2, V3.up);

            var    result = Q3.Lerp(a, b, 0.5);
            double angle  = Q3.Angle(a, result);

            angle.ShouldEqual(PI / 4, 1e-10);
        }

        [Fact]
        private void LerpIdenticalQuaternions()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);

            Q3.Lerp(q, q, 0).ShouldEqual(q, 1e-14);
            Q3.Lerp(q, q, 0.5).ShouldEqual(q, 1e-14);
            Q3.Lerp(q, q, 1).ShouldEqual(q, 1e-14);
        }

        [Fact]
        private void LerpWithIdentityQuaternion()
        {
            var q = Q3.AngleAxis(PI / 3, V3.forward);

            Q3.Lerp(Q3.identity, q, 0).ShouldEqual(Q3.identity, 1e-14);
            Q3.Lerp(Q3.identity, q, 1).ShouldEqual(q, 1e-14);
        }

        [Fact]
        private void LerpResultIsNormalized()
        {
            var a = Q3.AngleAxis(PI / 6, V3.xaxis);
            var b = Q3.AngleAxis(PI / 3, V3.yaxis);

            for (double t = 0; t <= 1; t += 0.1)
            {
                var    result = Q3.Lerp(a, b, t);
                double mag    = Sqrt(result.x * result.x + result.y * result.y + result.z * result.z + result.w * result.w);
                mag.ShouldEqual(1.0, 1e-14);
            }
        }

        [Fact]
        private void LerpSmallAngleDifference()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(1e-8, V3.up);

            var    result = Q3.Lerp(a, b, 0.5);
            double angle  = Q3.Angle(a, result);

            angle.ShouldEqual(0.5e-8, 1e-16);
        }

        [Fact]
        private void LerpLargeAngleDifference()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI, V3.up);

            var result = Q3.Lerp(a, b, 0.5);

            double angleFromA = Q3.Angle(a, result);
            double angleFromB = Q3.Angle(result, b);

            angleFromA.ShouldEqual(angleFromB, 1e-10);
        }

        [Fact]
        private void LerpDifferentAxes()
        {
            var a = Q3.AngleAxis(PI / 4, V3.xaxis);
            var b = Q3.AngleAxis(PI / 4, V3.yaxis);

            var    result = Q3.Lerp(a, b, 0.5);
            double mag    = Sqrt(result.x * result.x + result.y * result.y + result.z * result.z + result.w * result.w);

            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void SlerpAtZeroReturnsStart()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI / 2, V3.up);

            Q3.Slerp(a, b, 0).ShouldEqual(a, 1e-14);
        }

        [Fact]
        private void SlerpAtOneReturnsEnd()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI / 2, V3.up);

            Q3.Slerp(a, b, 1).ShouldEqual(b, 1e-14);
        }

        [Fact]
        private void SlerpAtHalfReturnsMidpoint()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI / 2, V3.up);

            var    result = Q3.Slerp(a, b, 0.5);
            double angle  = Q3.Angle(a, result);

            angle.ShouldEqual(PI / 4, 1e-14);
        }

        [Fact]
        private void SlerpIdenticalQuaternions()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);

            Q3.Slerp(q, q, 0).ShouldEqual(q, 1e-14);
            Q3.Slerp(q, q, 0.5).ShouldEqual(q, 1e-14);
            Q3.Slerp(q, q, 1).ShouldEqual(q, 1e-14);
        }

        [Fact]
        private void SlerpWithIdentityQuaternion()
        {
            var q = Q3.AngleAxis(PI / 3, V3.forward);

            Q3.Slerp(Q3.identity, q, 0).ShouldEqual(Q3.identity, 1e-14);
            Q3.Slerp(Q3.identity, q, 1).ShouldEqual(q, 1e-14);
        }

        [Fact]
        private void SlerpConstantAngularVelocity()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI / 2, V3.up);

            double totalAngle = Q3.Angle(a, b);

            for (double t = 0; t <= 1; t += 0.1)
            {
                var    result = Q3.Slerp(a, b, t);
                double angle  = Q3.Angle(a, result);
                angle.ShouldEqual(t * totalAngle, 1e-14);
            }
        }

        [Fact]
        private void SlerpResultIsNormalized()
        {
            var a = Q3.AngleAxis(PI / 6, V3.xaxis);
            var b = Q3.AngleAxis(PI / 3, V3.yaxis);

            for (double t = 0; t <= 1; t += 0.1)
            {
                var    result = Q3.Slerp(a, b, t);
                double mag    = Sqrt(result.x * result.x + result.y * result.y + result.z * result.z + result.w * result.w);
                mag.ShouldEqual(1.0, 1e-14);
            }
        }

        [Fact]
        private void SlerpTakesShortestPath()
        {
            var a = new Q3(0.5, 0.5, 0.5, 0.5);
            var b = new Q3(-0.5, -0.5, -0.5, -0.5);

            var result = Q3.Slerp(a, b, 0.5);

            result.ShouldEqual(a, 1e-14);
        }

        [Fact]
        private void SlerpNearlyIdenticalQuaternionsFallsBackToLerp()
        {
            Q3  a = Q3.identity;
            var b = Q3.AngleAxis(1e-10, V3.up);

            var    result = Q3.Slerp(a, b, 0.5);
            double mag    = Sqrt(result.x * result.x + result.y * result.y + result.z * result.z + result.w * result.w);

            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void SlerpSmallAngleDifference()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(1e-6, V3.up);

            var    result = Q3.Slerp(a, b, 0.5);
            double angle  = Q3.Angle(a, result);

            angle.ShouldEqual(0.5e-6, 1e-12);
        }

        [Fact]
        private void SlerpLargeAngleDifference()
        {
            var a = Q3.AngleAxis(0, V3.up);
            var b = Q3.AngleAxis(PI, V3.up);

            var    result     = Q3.Slerp(a, b, 0.5);
            double angleFromA = Q3.Angle(a, result);

            angleFromA.ShouldEqual(PI / 2, 1e-14);
        }

        [Fact]
        private void SlerpDifferentAxes()
        {
            var a = Q3.AngleAxis(PI / 4, V3.xaxis);
            var b = Q3.AngleAxis(PI / 4, V3.yaxis);

            var result = Q3.Slerp(a, b, 0.5);

            double angleFromA = Q3.Angle(a, result);
            double angleFromB = Q3.Angle(result, b);

            angleFromA.ShouldEqual(angleFromB, 1e-14);
        }

        [Fact]
        private void SlerpPreservesRotationEffect()
        {
            var a          = Q3.AngleAxis(0, V3.up);
            var b          = Q3.AngleAxis(PI / 2, V3.up);
            V3  testVector = V3.forward;

            V3  rotatedByA   = a * testVector;
            V3  rotatedByB   = b * testVector;
            var mid          = Q3.Slerp(a, b, 0.5);
            V3  rotatedByMid = mid * testVector;

            var expectedMid = V3.Slerp(rotatedByA, rotatedByB, 0.5);

            rotatedByMid.ShouldEqual(expectedMid, 1e-14);
        }

        [Fact]
        private void SlerpQuarterIncrements()
        {
            var a = Q3.AngleAxis(0, V3.forward);
            var b = Q3.AngleAxis(PI, V3.forward);

            Q3.Angle(a, Q3.Slerp(a, b, 0.25)).ShouldEqual(PI / 4, 1e-14);
            Q3.Angle(a, Q3.Slerp(a, b, 0.50)).ShouldEqual(PI / 2, 1e-14);
            Q3.Angle(a, Q3.Slerp(a, b, 0.75)).ShouldEqual(3 * PI / 4, 1e-14);
        }

        [Fact]
        private void SlerpVsLerpForSmallAngles()
        {
            Q3  a = Q3.identity;
            var b = Q3.AngleAxis(0.01, V3.up);

            var slerpResult = Q3.Slerp(a, b, 0.5);
            var lerpResult  = Q3.Lerp(a, b, 0.5);

            double slerpAngle = Q3.Angle(a, slerpResult);
            double lerpAngle  = Q3.Angle(a, lerpResult);

            slerpAngle.ShouldEqual(lerpAngle, 1e-6);
        }

        [Fact]
        private void SlerpVsLerpForLargeAngles()
        {
            Q3  a = Q3.identity;
            var b = Q3.AngleAxis(PI / 2, V3.up);

            var slerpResult = Q3.Slerp(a, b, 0.5);
            var lerpResult  = Q3.Lerp(a, b, 0.5);

            double slerpAngle = Q3.Angle(a, slerpResult);
            double lerpAngle  = Q3.Angle(a, lerpResult);

            slerpAngle.ShouldEqual(PI / 4, 1e-14);
            (Abs(slerpAngle - lerpAngle) < 0.01).ShouldBeTrue();
        }

        [Fact]
        private void LerpAndSlerpAgreementAtEndpoints()
        {
            var a = Q3.AngleAxis(PI / 6, new V3(1, 2, 3).normalized);
            var b = Q3.AngleAxis(PI / 3, new V3(3, 2, 1).normalized);

            Q3.Lerp(a, b, 0).ShouldEqual(Q3.Slerp(a, b, 0), 1e-14);
            Q3.Lerp(a, b, 1).ShouldEqual(Q3.Slerp(a, b, 1), 1e-14);
        }

        [Fact]
        private void SlerpAroundDifferentAxes()
        {
            var a = Q3.AngleAxis(PI / 4, V3.xaxis);
            var b = Q3.AngleAxis(PI / 4, V3.zaxis);

            var    result = Q3.Slerp(a, b, 0.5);
            double mag    = Sqrt(result.x * result.x + result.y * result.y + result.z * result.z + result.w * result.w);

            mag.ShouldEqual(1.0, 1e-14);

            double angleA = Q3.Angle(a, result);
            double angleB = Q3.Angle(result, b);
            angleA.ShouldEqual(angleB, 1e-14);
        }

        [Fact]
        private void SlerpNegativeDotProductHandling()
        {
            var a = Q3.AngleAxis(0.1, V3.up);
            Q3  b = -a;

            var    result = Q3.Slerp(a, b, 0.5);
            double mag    = Sqrt(result.x * result.x + result.y * result.y + result.z * result.z + result.w * result.w);

            mag.ShouldEqual(1.0, 1e-14);
        }

        [Fact]
        private void SlerpContinuityAcrossThreshold()
        {
            Q3  a     = Q3.identity;
            var bNear = Q3.AngleAxis(1e-9, V3.up);
            var bFar  = Q3.AngleAxis(1e-7, V3.up);

            var resultNear = Q3.Slerp(a, bNear, 0.5);
            var resultFar  = Q3.Slerp(a, bFar, 0.5);

            double angleNear = Q3.Angle(a, resultNear);
            double angleFar  = Q3.Angle(a, resultFar);

            angleNear.ShouldEqual(0.5e-9, 1e-16);
            angleFar.ShouldEqual(0.5e-7, 1e-14);
        }
    }
}
