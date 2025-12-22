/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class VectorMathOperationTests
    {
        [Fact]
        private void DotProductBasicVectors()
        {
            V3.Dot(V3.xaxis, V3.xaxis).ShouldEqual(1.0);
            V3.Dot(V3.xaxis, V3.yaxis).ShouldEqual(0.0);
            V3.Dot(V3.xaxis, V3.zaxis).ShouldEqual(0.0);
        }

        [Fact]
        private void DotProductOrthogonalVectors()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 1, 0);
            V3.Dot(a, b).ShouldEqual(0.0);
        }

        [Fact]
        private void DotProductParallelVectors()
        {
            var a = new V3(2, 3, 4);
            var b = new V3(4, 6, 8);
            V3.Dot(a, b).ShouldEqual(58.0);
        }

        [Fact]
        private void DotProductAntiparallelVectors()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(-1, -2, -3);
            V3.Dot(a, b).ShouldEqual(-14.0);
        }

        [Fact]
        private void DotProductWithZeroVector()
        {
            var v = new V3(1, 2, 3);
            V3.Dot(v, V3.zero).ShouldEqual(0.0);
            V3.Dot(V3.zero, v).ShouldEqual(0.0);
        }

        [Fact]
        private void DotProductCommutative()
        {
            var a = new V3(1.5, 2.7, -3.2);
            var b = new V3(-0.5, 4.1, 2.3);
            V3.Dot(a, b).ShouldEqual(V3.Dot(b, a));
        }

        [Fact]
        private void CrossProductBasicVectors()
        {
            V3.Cross(V3.xaxis, V3.yaxis).ShouldEqual(V3.zaxis);
            V3.Cross(V3.yaxis, V3.zaxis).ShouldEqual(V3.xaxis);
            V3.Cross(V3.zaxis, V3.xaxis).ShouldEqual(V3.yaxis);
        }

        [Fact]
        private void CrossProductAnticommutative()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);
            V3.Cross(a, b).ShouldEqual(-V3.Cross(b, a));
        }

        [Fact]
        private void CrossProductParallelVectors()
        {
            var a = new V3(2, 4, 6);
            var b = new V3(1, 2, 3);
            V3.Cross(a, b).ShouldBeZero();
        }

        [Fact]
        private void CrossProductWithZeroVector()
        {
            var v = new V3(1, 2, 3);
            V3.Cross(v, V3.zero).ShouldBeZero();
            V3.Cross(V3.zero, v).ShouldBeZero();
        }

        [Fact]
        private void CrossProductOrthogonality()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);
            var c = V3.Cross(a, b);

            V3.Dot(c, a).ShouldBeZero(1e-14);
            V3.Dot(c, b).ShouldBeZero(1e-14);
        }

        [Fact]
        private void CrossProductMagnitude()
        {
            var a = new V3(3, 0, 0);
            var b = new V3(0, 4, 0);
            var c = V3.Cross(a, b);

            c.magnitude.ShouldEqual(12.0);
        }

        [Fact]
        private void ProjectOntoNormalVector()
        {
            var vector = new V3(3, 4, 5);
            var normal = new V3(1, 0, 0);

            V3.Project(vector, normal).ShouldEqual(new V3(3, 0, 0));
        }

        [Fact]
        private void ProjectOntoArbitraryVector()
        {
            var vector = new V3(3, 4, 0);
            var onto   = new V3(1, 1, 0);

            var projected = V3.Project(vector, onto);
            projected.ShouldEqual(new V3(3.5, 3.5, 0));
        }

        [Fact]
        private void ProjectOntoZeroVector()
        {
            var vector = new V3(1, 2, 3);
            V3.Project(vector, V3.zero).ShouldEqual(V3.zero);
        }

        [Fact]
        private void ProjectZeroVector()
        {
            var normal = new V3(1, 2, 3);
            V3.Project(V3.zero, normal).ShouldEqual(V3.zero);
        }

        [Fact]
        private void ProjectWithOverflow()
        {
            var vector = new V3(1e200, 0, 0);
            var onto   = new V3(1e200, 1e200, 0);
            V3.Project(vector, onto).ShouldEqual(new V3(5e199, 5e199, 0));
        }

        [Fact]
        private void ProjectOnPlaneWithNormalVector()
        {
            var vector      = new V3(3, 4, 5);
            var planeNormal = new V3(0, 0, 1);

            V3.ProjectOnPlane(vector, planeNormal).ShouldEqual(new V3(3, 4, 0));
        }

        [Fact]
        private void ProjectOnPlaneWithArbitraryNormal()
        {
            var vector      = new V3(1, 2, 3);
            var planeNormal = new V3(1, 1, 1);

            var projected = V3.ProjectOnPlane(vector, planeNormal);
            V3.Dot(projected, planeNormal).ShouldBeZero(1e-14);
        }

        [Fact]
        private void ProjectOnPlaneWithZeroNormal()
        {
            var vector = new V3(1, 2, 3);
            V3.ProjectOnPlane(vector, V3.zero).ShouldEqual(vector);
        }

        [Fact]
        private void ProjectOnPlaneWithOverflow()
        {
            var vector      = new V3(1e200, 0, 0);
            var planeNormal = new V3(1e200, 1e200, 0);

            V3.ProjectOnPlane(vector, planeNormal).ShouldEqual(new V3(5e199, -5e199, 0));
        }

        [Fact]
        private void ProjectAndProjectOnPlaneAreComplementary()
        {
            var vector = new V3(3, 4, 5);
            var normal = new V3(1, 2, 2);

            var onNormal = V3.Project(vector, normal);
            var onPlane  = V3.ProjectOnPlane(vector, normal);

            (onNormal + onPlane).ShouldEqual(vector, 1e-14);
        }

        [Fact]
        private void AngleBetweenOrthogonalVectors()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(0, 1, 0);

            V3.Angle(a, b).ShouldEqual(PI / 2);
        }

        [Fact]
        private void AngleBetweenParallelVectors()
        {
            var a = new V3(2, 3, 4);
            var b = new V3(4, 6, 8);

            V3.Angle(a, b).ShouldEqual(0.0);
        }

        [Fact]
        private void AngleBetweenAntiparallelVectors()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(-1, 0, 0);

            V3.Angle(a, b).ShouldEqual(PI);
        }

        [Fact]
        private void AngleAt45Degrees()
        {
            var a = new V3(1, 0, 0);
            var b = new V3(1, 1, 0);

            V3.Angle(a, b).ShouldEqual(PI / 4, 1e-14);
        }

        [Fact]
        private void AngleWithZeroVector()
        {
            var v = new V3(1, 2, 3);

            V3.Angle(v, V3.zero).ShouldEqual(0.0);
            V3.Angle(V3.zero, v).ShouldEqual(0.0);
        }

        [Fact]
        private void AngleIsCommutative()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            V3.Angle(a, b).ShouldEqual(V3.Angle(b, a));
        }

        [Fact]
        private void AngleWithVerySmallVectors()
        {
            var a = new V3(1e-200, 0, 0);
            var b = new V3(0, 1e-200, 0);

            V3.Angle(a, b).ShouldEqual(PI / 2);
        }

        [Fact]
        private void SignedAnglePositive()
        {
            var from = new V3(1, 0, 0);
            var to   = new V3(0, 1, 0);
            var axis = new V3(0, 0, 1);

            V3.SignedAngle(from, to, axis).ShouldEqual(PI / 2);
        }

        [Fact]
        private void SignedAngleNegative()
        {
            var from = new V3(1, 0, 0);
            var to   = new V3(0, -1, 0);
            var axis = new V3(0, 0, 1);

            V3.SignedAngle(from, to, axis).ShouldEqual(-PI / 2);
        }

        [Fact]
        private void SignedAngleFullRotation()
        {
            var from = new V3(1, 0, 0);
            var to   = new V3(-1, 0, 0);
            var axis = new V3(0, 0, 1);

            double angle = V3.SignedAngle(from, to, axis);
            Abs(angle).ShouldEqual(PI);
        }

        [Fact]
        private void SignedAngleWithParallelVectors()
        {
            var from = new V3(1, 2, 3);
            var to   = new V3(2, 4, 6);
            var axis = new V3(0, 0, 1);

            V3.SignedAngle(from, to, axis).ShouldEqual(0.0);
        }

        [Fact]
        private void SignedAngleFlipsWithAxis()
        {
            var from  = new V3(1, 0, 0);
            var to    = new V3(0, 1, 0);
            var axis1 = new V3(0, 0, 1);
            var axis2 = new V3(0, 0, -1);

            double angle1 = V3.SignedAngle(from, to, axis1);
            double angle2 = V3.SignedAngle(from, to, axis2);

            angle1.ShouldEqual(-angle2);
        }

        [Fact]
        private void DistanceBetweenPoints()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 6, 3);

            V3.Distance(a, b).ShouldEqual(5.0);
        }

        [Fact]
        private void DistanceToSamePoint()
        {
            var v = new V3(1, 2, 3);

            V3.Distance(v, v).ShouldEqual(0.0);
        }

        [Fact]
        private void DistanceIsCommutative()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            V3.Distance(a, b).ShouldEqual(V3.Distance(b, a));
        }

        [Fact]
        private void DistanceWithNegativeCoordinates()
        {
            var a = new V3(-1, -2, -3);
            var b = new V3(2, 2, 2);

            V3.Distance(a, b).ShouldEqual(Sqrt(50));
        }

        [Fact]
        private void DistanceFromOrigin()
        {
            var v = new V3(3, 4, 0);

            V3.Distance(v, V3.zero).ShouldEqual(5.0);
            V3.Distance(V3.zero, v).ShouldEqual(5.0);
        }

        [Fact]
        private void DistanceWithLargeValues()
        {
            var a = new V3(1e100, 0, 0);
            var b = new V3(0, 0, 0);

            V3.Distance(a, b).ShouldEqual(1e100);
        }

        [Fact]
        private void DistanceOverflowWithLargeDifferences()
        {
            var a = new V3(1e200, 1e200, 1e200);
            var b = new V3(-1e200, -1e200, -1e200);

            double distance = V3.Distance(a, b);

            distance.ShouldEqual(Sqrt(3) * 2e200);
        }

        [Fact]
        private void OrthoNormalizeBasicVectors()
        {
            var normal  = new V3(1, 1, 0);
            var tangent = new V3(1, 0, 0);

            V3.OrthoNormalize(ref normal, ref tangent);

            normal.magnitude.ShouldEqual(1.0);
            tangent.magnitude.ShouldEqual(1.0);
            V3.Dot(normal, tangent).ShouldBeZero(1e-14);
        }

        [Fact]
        private void OrthoNormalizePreservesNormalDirection()
        {
            var normal  = new V3(2, 0, 0);
            var tangent = new V3(1, 1, 0);

            V3 originalNormalDir = normal.normalized;
            V3.OrthoNormalize(ref normal, ref tangent);

            normal.ShouldEqual(originalNormalDir);
        }

        [Fact]
        private void OrthoNormalizeCreatesOrthogonalVectors()
        {
            var normal  = new V3(0, 0, 1);
            var tangent = new V3(1, 1, 0);

            V3.OrthoNormalize(ref normal, ref tangent);

            normal.magnitude.ShouldEqual(1.0);
            tangent.magnitude.ShouldEqual(1.0);

            V3.Dot(normal, tangent).ShouldBeZero(1e-14);

            tangent.z.ShouldBeZero(1e-14);

            V3.Dot(tangent, new V3(1, 1, 0).normalized).ShouldBePositive();
        }

        [Fact]
        private void OrthoNormalizeWithNearlyParallelVectors()
        {
            var normal  = new V3(1, 0, 0);
            var tangent = new V3(1, 0.001, 0);

            V3.OrthoNormalize(ref normal, ref tangent);

            normal.magnitude.ShouldEqual(1.0);
            tangent.magnitude.ShouldEqual(1.0);
            V3.Dot(normal, tangent).ShouldBeZero(1e-14);
        }

        [Fact]
        private void DotProductWithLargeVectors()
        {
            var a = new V3(1e100, 1e100, 1e100);
            var b = new V3(2, 3, 4);

            V3.Dot(a, b).ShouldEqual(9e100);
        }

        [Fact]
        private void CrossProductRightHandedRule()
        {
            V3.Cross(V3.forward, V3.right).ShouldEqual(V3.down);
            V3.Cross(V3.right, V3.down).ShouldEqual(V3.forward);
            V3.Cross(V3.down, V3.forward).ShouldEqual(V3.right);
        }

        [Fact]
        private void AnglePrecisionForSmallAngles()
        {
            const double SMALL = 1e-157;

            var a = new V3(1, 0, 0);
            var b = new V3(Cos(SMALL), Sin(SMALL), 0);

            V3.Angle(a, b).ShouldNotBeZero();
            V3.Angle(a, b).ShouldEqual(SMALL, 1e-10);
        }

        [Fact]
        private void OuterProductBasicVectors()
        {
            var a      = new V3(1, 2, 3);
            var b      = new V3(4, 5, 6);
            M3  result = V3.Outer(a, b);

            result.GetColumn(0).ShouldEqual(a * 4);
            result.GetColumn(1).ShouldEqual(a * 5);
            result.GetColumn(2).ShouldEqual(a * 6);
        }

        [Fact]
        private void OuterProductUnitVectors()
        {
            M3 result = V3.Outer(V3.xaxis, V3.yaxis);

            result.m00.ShouldEqual(0);
            result.m01.ShouldEqual(1);
            result.m02.ShouldEqual(0);
            result.m10.ShouldEqual(0);
            result.m11.ShouldEqual(0);
            result.m12.ShouldEqual(0);
            result.m20.ShouldEqual(0);
            result.m21.ShouldEqual(0);
            result.m22.ShouldEqual(0);
        }

        [Fact]
        private void OuterProductWithZeroVector()
        {
            var v = new V3(1, 2, 3);

            V3.Outer(v, V3.zero).ShouldEqual(M3.zero);
            V3.Outer(V3.zero, v).ShouldEqual(M3.zero);
        }

        [Fact]
        private void OuterProductSymmetry()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            M3 ab = V3.Outer(a, b);
            M3 ba = V3.Outer(b, a);

            ab.transpose.ShouldEqual(ba);
        }

        [Fact]
        private void OuterProductWithNegativeComponents()
        {
            var a      = new V3(-1, 2, -3);
            var b      = new V3(4, -5, 6);
            M3  result = V3.Outer(a, b);

            result.GetColumn(0).ShouldEqual(new V3(-4, 8, -12));
            result.GetColumn(1).ShouldEqual(new V3(5, -10, 15));
            result.GetColumn(2).ShouldEqual(new V3(-6, 12, -18));
        }

        [Fact]
        private void OuterProductRankOne()
        {
            var a      = new V3(1, 2, 3);
            var b      = new V3(4, 5, 6);
            M3  result = V3.Outer(a, b);

            result.determinant.ShouldEqual(0.0);
        }

        [Fact]
        private void OuterProductScalarMultiplication()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            M3 result1 = V3.Outer(a * 2, b);
            M3 result2 = V3.Outer(a, b * 2);
            M3 result3 = V3.Outer(a, b) * 2;

            result1.ShouldEqual(result3);
            result2.ShouldEqual(result3);
        }

        [Fact]
        private void OuterProductVectorMultiplication()
        {
            var a = new V3(2, 3, 4);
            var b = new V3(5, 6, 7);
            var c = new V3(1, 1, 1);

            M3 outer  = V3.Outer(a, b);
            V3 result = outer * c;

            double bDotC = V3.Dot(b, c);
            result.ShouldEqual(a * bDotC);
        }

        [Fact]
        private void OuterProductLargeValues()
        {
            var a      = new V3(1e100, 1e100, 1e100);
            var b      = new V3(1e50, 1e50, 1e50);
            M3  result = V3.Outer(a, b);

            result.m00.ShouldEqual(1e150);
            result.m11.ShouldEqual(1e150);
            result.m22.ShouldEqual(1e150);
        }

        [Fact]
        private void OuterProductSmallValues()
        {
            var a      = new V3(1e-100, 1e-100, 1e-100);
            var b      = new V3(1e-50, 1e-50, 1e-50);
            M3  result = V3.Outer(a, b);

            result.m00.ShouldEqual(1e-150);
            result.m11.ShouldEqual(1e-150);
            result.m22.ShouldEqual(1e-150);
        }
    }
}
