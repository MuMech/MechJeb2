/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.V4Tests
{
    public class VectorMathOperationTests
    {
        [Fact]
        private void DotProduct()
        {
            V4.Dot(new V4(1, 2, 3, 4), new V4(5, -6, 7, -8)).ShouldEqual(-18);
            V4.Dot(V4.zaxis, V4.waxis).ShouldEqual(0);
            V4.Dot(new V4(1, 2, 3, 4), V4.zero).ShouldEqual(0);
        }

        [Fact]
        private void OuterProductEntries()
        {
            var a = new V4(1, 2, 3, 4);
            var b = new V4(5, 6, 7, 8);

            V4.Outer(a, b).ShouldEqual(new M4(
                5, 6, 7, 8,
                10, 12, 14, 16,
                15, 18, 21, 24,
                20, 24, 28, 32));
        }

        [Fact]
        private void OuterProductIdentities()
        {
            var a = new V4(1.5, -2, 0.5, 3);
            var b = new V4(-1, 4, 2, 0.25);
            var c = new V4(2, 1, -3, 5);

            (V4.Outer(a, b) * c).ShouldEqual(a * V4.Dot(b, c));
            V4.Outer(a, b).transpose.ShouldEqual(V4.Outer(b, a));
            V4.Outer(a, b).trace.ShouldEqual(V4.Dot(a, b));
            V4.Outer(V4.xaxis, V4.waxis)[0, 3].ShouldEqual(1);
        }

        [Fact]
        private void OuterProductMatchesV3OuterInUpperBlock()
        {
            var a = new V3(1.5, -2, 0.5);
            var b = new V3(-1, 4, 2);

            M3 m3 = V3.Outer(a, b);
            M4 m4 = V4.Outer(new V4(a.x, a.y, a.z, 0), new V4(b.x, b.y, b.z, 0));

            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                m4[i, j].ShouldEqual(m3[i, j]);
        }

        [Fact]
        private void Project()
        {
            V4.Project(new V4(1, 2, 3, 4), V4.zaxis).ShouldEqual(new V4(0, 0, 3, 0));
            V4.Project(new V4(1, 2, 3, 4), new V4(0, 1, 0, 1)).ShouldEqual(new V4(0, 3, 0, 3));
        }

        [Fact]
        private void ProjectOnPlane()
        {
            var v = new V4(1, 2, 3, 4);
            var n = new V4(0, 1, 0, 1);

            V4 onPlane = V4.ProjectOnPlane(v, n);

            onPlane.ShouldEqual(new V4(1, -1, 3, 1));
            V4.Dot(onPlane, n).ShouldEqual(0);
            (onPlane + V4.Project(v, n)).ShouldEqual(v);
        }

        [Fact]
        private void ProjectOnPlaneRemovesRadialComponentOfUnitQuaternion()
        {
            V4 q = new V4(1, 2, 3, 4).normalized;
            var g = new V4(-0.5, 1, 0.25, 2);

            V4 tangent = V4.ProjectOnPlane(g, q);

            V4.Dot(tangent, q).ShouldEqual(0, 1e-15);
        }

        [Fact]
        private void ProjectZeroCases()
        {
            V4.Project(V4.zero, V4.one).ShouldEqual(V4.zero);
            V4.Project(V4.one, V4.zero).ShouldEqual(V4.zero);
            V4.Project(V4.zero, V4.zero).ShouldEqual(V4.zero);
            V4.ProjectOnPlane(V4.zero, V4.one).ShouldEqual(V4.zero);
            V4.ProjectOnPlane(V4.one, V4.zero).ShouldEqual(V4.one);
        }

        [Fact]
        private void ProjectHugeAndTinyMagnitudes()
        {
            // compare by ratio since the magnitude in NearlyEqual over/underflows at these scales
            V4 huge = V4.Project(new V4(3e300, 4e300, 0, 0), new V4(1e300, 0, 0, 0));
            (huge.x / 3e300).ShouldEqual(1.0, 1e-15);
            huge.y.ShouldEqual(0);

            V4 tiny = V4.Project(new V4(0, 0, 3e-300, 4e-300), new V4(0, 0, 0, 1e-300));
            tiny.z.ShouldEqual(0);
            (tiny.w / 4e-300).ShouldEqual(1.0, 1e-15);

            V4 hugeOnPlane = V4.ProjectOnPlane(new V4(3e300, 4e300, 0, 0), new V4(1e300, 0, 0, 0));
            (Abs(hugeOnPlane.x) / 3e300).ShouldBeLessThan(1e-15);
            (hugeOnPlane.y / 4e300).ShouldEqual(1.0, 1e-15);
        }

        [Fact]
        private void AngleBasic()
        {
            V4.Angle(V4.xaxis, V4.yaxis).ShouldEqual(PI / 2);
            V4.Angle(V4.zaxis, V4.waxis).ShouldEqual(PI / 2);
            V4.Angle(V4.waxis, -V4.waxis).ShouldEqual(PI);
            V4.Angle(V4.one, V4.one).ShouldEqual(0);
            V4.Angle(V4.one, new V4(1, -1, 1, -1)).ShouldEqual(PI / 2);
            V4.Angle(new V4(1, 0, 0, 0), new V4(1, 0, 0, 1)).ShouldEqual(PI / 4, 1e-15);
        }

        [Fact]
        private void AngleMatchesV3Angle()
        {
            var a = new V3(1.5, -2.7, 0.3);
            var b = new V3(-0.5, 4.1, 2.2);

            V4.Angle(new V4(a.x, a.y, a.z, 0), new V4(b.x, b.y, b.z, 0)).ShouldEqual(V3.Angle(a, b), 1e-15);
        }

        [Fact]
        private void AngleWithZeroVectorIsZero()
        {
            V4.Angle(V4.zero, V4.xaxis).ShouldEqual(0);
            V4.Angle(V4.xaxis, V4.zero).ShouldEqual(0);
            V4.Angle(V4.zero, V4.zero).ShouldEqual(0);
        }

        [Fact]
        private void AngleNearlyParallelIsAccurate()
        {
            (V4.Angle(V4.xaxis, new V4(1, 0, 0, 1e-10)) / 1e-10).ShouldEqual(1.0, 1e-15);
        }

        [Fact]
        private void AngleNearlyAntiparallelIsAccurate()
        {
            double delta = Atan(1e-5);

            ((PI - V4.Angle(V4.xaxis, new V4(-1, 0, 1e-5, 0))) / delta).ShouldEqual(1.0, 1e-9);
        }

        [Fact]
        private void AngleHugeAndTinyMagnitudes()
        {
            V4.Angle(new V4(1e300, 0, 0, 0), new V4(1e300, 0, 1e300, 0)).ShouldEqual(PI / 4, 1e-15);
            V4.Angle(new V4(1e-300, 0, 0, 0), new V4(1e-300, 0, 1e-300, 0)).ShouldEqual(PI / 4, 1e-15);
        }

        [Fact]
        private void Distance()
        {
            V4.Distance(new V4(1, 1, 1, 1), new V4(2, 3, 3, 5)).ShouldEqual(5);
            V4.Distance(new V4(2, 3, 3, 5), new V4(1, 1, 1, 1)).ShouldEqual(5);
            V4.Distance(new V4(1, 2, 3, 4), new V4(1, 2, 3, 4)).ShouldEqual(0);
        }

        [Fact]
        private void ClampMagnitude()
        {
            var v = new V4(1, 2, 2, 4);

            V4.ClampMagnitude(v, 10).ShouldEqual(v);
            V4.ClampMagnitude(v, 5).ShouldEqual(v);
            V4.ClampMagnitude(v, 2.5).ShouldEqual(new V4(0.5, 1, 1, 2));
        }

        [Fact]
        private void OrthonormalIsPerpendicularUnitVector()
        {
            V4.xaxis.orthonormal.ShouldEqual(V4.yaxis);
            V4.zaxis.orthonormal.ShouldEqual(V4.waxis);

            var v = new V4(1, 2, 3, 4);
            V4 o = v.orthonormal;

            o.magnitude.ShouldEqual(1, 1e-15);
            V4.Dot(v, o).ShouldEqual(0, 1e-15);
        }

        [Fact]
        private void OrthonormalMatchesV2InEachPlane()
        {
            V4 o = new V4(3, 4, 0, 0).orthonormal;
            V2 o2 = new V2(3, 4).orthonormal;

            o.ShouldEqual(new V4(o2.x, o2.y, 0, 0));
        }

        [Fact]
        private void OrthonormalOfZeroIsZero()
        {
            V4.zero.orthonormal.ShouldEqual(V4.zero);
        }

        [Fact]
        private void OrthoNormalize()
        {
            var normal = new V4(2, 0, 0, 0);
            var tangent = new V4(1, 0, 5, 0);

            V4.OrthoNormalize(ref normal, ref tangent);

            normal.ShouldEqual(V4.xaxis);
            tangent.ShouldEqual(V4.zaxis);
        }

        [Fact]
        private void OrthoNormalizeKeepsTangentSide()
        {
            var normal = new V4(1, 1, 1, 1);
            var original = new V4(1, 2, 3, 4);
            V4 tangent = original;

            V4.OrthoNormalize(ref normal, ref tangent);

            normal.ShouldEqual(new V4(0.5, 0.5, 0.5, 0.5));
            tangent.ShouldEqual(new V4(-1.5, -0.5, 0.5, 1.5) / Sqrt(5), 1e-15);
            V4.Dot(normal, tangent).ShouldEqual(0, 1e-15);
            V4.Dot(original, tangent).ShouldBePositive();
        }
    }
}
