/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.V2Tests
{
    public class VectorMathOperationTests
    {
        [Fact]
        private void DotProduct()
        {
            V2.Dot(new V2(2, 3), new V2(4, -5)).ShouldEqual(-7);
            V2.Dot(V2.xaxis, V2.yaxis).ShouldEqual(0);
            V2.Dot(new V2(1, 2), V2.zero).ShouldEqual(0);
        }

        [Fact]
        private void OuterProductEntries()
        {
            V2.Outer(new V2(1, 2), new V2(3, 4)).ShouldEqual(new M2(3, 4, 6, 8));
            V2.Outer(V2.xaxis, V2.yaxis)[0, 1].ShouldEqual(1);
        }

        [Fact]
        private void OuterProductIdentities()
        {
            var a = new V2(1.5, -2);
            var b = new V2(-1, 4);
            var c = new V2(2, 0.5);

            (V2.Outer(a, b) * c).ShouldEqual(a * V2.Dot(b, c));
            V2.Outer(a, b).transpose.ShouldEqual(V2.Outer(b, a));
            V2.Outer(a, b).trace.ShouldEqual(V2.Dot(a, b));
            V2.Outer(a, b).determinant.ShouldEqual(0);
        }

        [Fact]
        private void OuterProductMatchesV3OuterInUpperBlock()
        {
            var a = new V2(1.5, -2);
            var b = new V2(-1, 4);

            M3 m3 = V3.Outer(new V3(a.x, a.y, 0), new V3(b.x, b.y, 0));
            M2 m2 = V2.Outer(a, b);

            for (int i = 0; i < 2; i++)
            for (int j = 0; j < 2; j++)
                m2[i, j].ShouldEqual(m3[i, j]);
        }

        [Fact]
        private void CrossProductMatchesV3CrossZComponent()
        {
            var a = new V2(1.5, -2.7);
            var b = new V2(-0.5, 4.1);

            V2.Cross(a, b).ShouldEqual(V3.Cross(new V3(a.x, a.y, 0), new V3(b.x, b.y, 0)).z);
        }

        [Fact]
        private void CrossProductSignIsCounterClockwisePositive()
        {
            V2.Cross(V2.xaxis, V2.yaxis).ShouldEqual(1);
            V2.Cross(V2.yaxis, V2.xaxis).ShouldEqual(-1);
        }

        [Fact]
        private void CrossProductAnticommutative()
        {
            var a = new V2(1, 2);
            var b = new V2(4, 5);

            V2.Cross(a, b).ShouldEqual(-V2.Cross(b, a));
        }

        [Fact]
        private void CrossProductParallelIsZero()
        {
            V2.Cross(new V2(1, 2), new V2(2, 4)).ShouldEqual(0);
            V2.Cross(new V2(1, 2), new V2(-3, -6)).ShouldEqual(0);
        }

        [Fact]
        private void ProjectOntoAxis()
        {
            V2.Project(new V2(3, 4), V2.xaxis).ShouldEqual(new V2(3, 0));
            V2.Project(new V2(3, 4), new V2(0, 10)).ShouldEqual(new V2(0, 4));
        }

        [Fact]
        private void ProjectOntoDiagonal()
        {
            V2.Project(new V2(2, 0), new V2(1, 1)).ShouldEqual(new V2(1, 1));
        }

        [Fact]
        private void ProjectZeroCases()
        {
            V2.Project(V2.zero, new V2(1, 1)).ShouldEqual(V2.zero);
            V2.Project(new V2(1, 1), V2.zero).ShouldEqual(V2.zero);
            V2.Project(V2.zero, V2.zero).ShouldEqual(V2.zero);
        }

        [Fact]
        private void ProjectHugeAndTinyMagnitudes()
        {
            // compare by ratio since the magnitude in NearlyEqual over/underflows at these scales
            V2 huge = V2.Project(new V2(3e300, 4e300), new V2(1e300, 0));
            (huge.x / 3e300).ShouldEqual(1.0, 1e-15);
            huge.y.ShouldEqual(0);

            V2 tiny = V2.Project(new V2(3e-300, 4e-300), new V2(0, 1e-300));
            tiny.x.ShouldEqual(0);
            (tiny.y / 4e-300).ShouldEqual(1.0, 1e-15);
        }

        [Fact]
        private void AngleBasic()
        {
            V2.Angle(V2.xaxis, V2.yaxis).ShouldEqual(PI / 2);
            V2.Angle(V2.yaxis, V2.xaxis).ShouldEqual(PI / 2);
            V2.Angle(V2.xaxis, -V2.xaxis).ShouldEqual(PI);
            V2.Angle(V2.xaxis, V2.xaxis).ShouldEqual(0);
            V2.Angle(new V2(1, 0), new V2(1, 1)).ShouldEqual(PI / 4);
        }

        [Fact]
        private void AngleWithZeroVectorIsZero()
        {
            V2.Angle(V2.zero, V2.xaxis).ShouldEqual(0);
            V2.Angle(V2.xaxis, V2.zero).ShouldEqual(0);
            V2.Angle(V2.zero, V2.zero).ShouldEqual(0);
        }

        [Fact]
        private void AngleHugeAndTinyMagnitudes()
        {
            V2.Angle(new V2(1e300, 0), new V2(1e300, 1e300)).ShouldEqual(PI / 4);
            V2.Angle(new V2(1e-300, 0), new V2(1e-300, 1e-300)).ShouldEqual(PI / 4);
        }

        [Fact]
        private void AngleSmallAnglesAreAccurate()
        {
            (V2.Angle(V2.xaxis, new V2(1, 1e-10)) / 1e-10).ShouldEqual(1.0, 1e-15);
        }

        [Fact]
        private void SignedAngleCounterClockwiseIsPositive()
        {
            V2.SignedAngle(V2.xaxis, V2.yaxis).ShouldEqual(PI / 2);
            V2.SignedAngle(V2.yaxis, V2.xaxis).ShouldEqual(-PI / 2);
            V2.SignedAngle(new V2(1, 1), new V2(-1, 1)).ShouldEqual(PI / 2);
            V2.SignedAngle(new V2(1, 0), new V2(-1, -1)).ShouldEqual(-3 * PI / 4);
        }

        [Fact]
        private void SignedAngleMagnitudeMatchesAngle()
        {
            var a = new V2(1.5, -2.7);
            var b = new V2(-0.5, 4.1);

            Abs(V2.SignedAngle(a, b)).ShouldEqual(V2.Angle(a, b));
        }

        [Fact]
        private void SignedAngleAntiparallel()
        {
            Abs(V2.SignedAngle(V2.xaxis, -V2.xaxis)).ShouldEqual(PI);
        }

        [Fact]
        private void SignedAngleWithZeroVectorIsZero()
        {
            V2.SignedAngle(V2.zero, V2.xaxis).ShouldEqual(0);
            V2.SignedAngle(V2.xaxis, V2.zero).ShouldEqual(0);
        }

        [Fact]
        private void SignedAngleHugeAndTinyMagnitudes()
        {
            V2.SignedAngle(new V2(1e300, 1e300), new V2(1e300, 0)).ShouldEqual(-PI / 4);
            V2.SignedAngle(new V2(1e-300, 0), new V2(1e-300, 1e-300)).ShouldEqual(PI / 4);
        }

        [Fact]
        private void Distance()
        {
            V2.Distance(new V2(1, 1), new V2(4, 5)).ShouldEqual(5);
            V2.Distance(new V2(4, 5), new V2(1, 1)).ShouldEqual(5);
            V2.Distance(new V2(1, 2), new V2(1, 2)).ShouldEqual(0);
        }

        [Fact]
        private void ClampMagnitude()
        {
            V2.ClampMagnitude(new V2(3, 4), 10).ShouldEqual(new V2(3, 4));
            V2.ClampMagnitude(new V2(3, 4), 5).ShouldEqual(new V2(3, 4));
            V2.ClampMagnitude(new V2(3, 4), 2.5).ShouldEqual(new V2(1.5, 2));
        }

        [Fact]
        private void OrthonormalIsCounterClockwisePerpendicular()
        {
            V2.xaxis.orthonormal.ShouldEqual(V2.yaxis);
            V2.yaxis.orthonormal.ShouldEqual(-V2.xaxis);

            var v = new V2(3, 4);
            V2 o = v.orthonormal;

            o.magnitude.ShouldEqual(1);
            V2.Dot(v, o).ShouldEqual(0, 1e-15);
            V2.Cross(v, o).ShouldBePositive();
        }

        [Fact]
        private void OrthonormalOfZeroIsZero()
        {
            V2.zero.orthonormal.ShouldEqual(V2.zero);
        }

        [Fact]
        private void OrthoNormalize()
        {
            var normal = new V2(3, 0);
            var tangent = new V2(1, 2);

            V2.OrthoNormalize(ref normal, ref tangent);

            normal.ShouldEqual(V2.xaxis);
            tangent.ShouldEqual(V2.yaxis);
        }

        [Fact]
        private void OrthoNormalizeKeepsTangentSide()
        {
            var normal = new V2(1, 1);
            var tangent = new V2(1, -3);

            V2.OrthoNormalize(ref normal, ref tangent);

            normal.magnitude.ShouldEqual(1);
            tangent.magnitude.ShouldEqual(1);
            V2.Dot(normal, tangent).ShouldEqual(0);
            V2.Cross(normal, tangent).ShouldEqual(-1);
        }
    }
}
