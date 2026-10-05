/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.V4Tests
{
    public class InterpolationTests
    {
        [Fact]
        private void LerpEndpointsAndMidpoint()
        {
            var a = new V4(1, 2, 3, 4);
            var b = new V4(5, -6, 7, 0);

            V4.Lerp(a, b, 0).ShouldEqual(a);
            V4.Lerp(a, b, 1).ShouldEqual(b);
            V4.Lerp(a, b, 0.5).ShouldEqual(new V4(3, -2, 5, 2));
        }

        [Fact]
        private void LerpExtrapolates()
        {
            var b = new V4(1, 2, 3, 4);

            V4.Lerp(V4.zero, b, 2).ShouldEqual(new V4(2, 4, 6, 8));
            V4.Lerp(V4.zero, b, -1).ShouldEqual(new V4(-1, -2, -3, -4));
        }

        [Fact]
        private void SlerpEndpoints()
        {
            var a = new V4(2, 0, 0, 0);
            var b = new V4(0, 0, 3, 0);

            V4.Slerp(a, b, 0).ShouldEqual(a);
            V4.Slerp(a, b, 1).ShouldEqual(b, 1e-15);
        }

        [Fact]
        private void SlerpMidpointOfUnitVectors()
        {
            V4.Slerp(V4.xaxis, V4.waxis, 0.5).ShouldEqual(new V4(Sqrt(0.5), 0, 0, Sqrt(0.5)));
        }

        [Fact]
        private void SlerpConstantAngularRate()
        {
            V4 a = V4.xaxis;
            var b = new V4(Cos(2.0), 0, 0, Sin(2.0));

            for (int i = 0; i <= 10; i++)
            {
                double t = i / 10.0;
                V4.Angle(a, V4.Slerp(a, b, t)).ShouldEqual(2.0 * t, 1e-14);
            }
        }

        [Fact]
        private void SlerpFollowsGreatCircle()
        {
            var a = new V4(1, 2, 0, 0);
            var b = new V4(0, 0, 3, 4);
            double total = V4.Angle(a, b);

            for (int i = 0; i <= 10; i++)
            {
                V4 r = V4.Slerp(a, b, i / 10.0);
                (V4.Angle(a, r) + V4.Angle(r, b)).ShouldEqual(total, 1e-14);
            }
        }

        [Fact]
        private void SlerpInterpolatesMagnitudeLinearly()
        {
            var a = new V4(2, 0, 0, 0);
            var b = new V4(0, 4, 0, 0);

            V4 mid = V4.Slerp(a, b, 0.5);

            mid.magnitude.ShouldEqual(3, 1e-14);
            V4.Angle(a, mid).ShouldEqual(PI / 4, 1e-14);
        }

        [Fact]
        private void SlerpNearlyParallel()
        {
            V4 a = V4.xaxis;
            var b = new V4(Cos(1e-9), 0, Sin(1e-9), 0);

            V4 mid = V4.Slerp(a, b, 0.5);

            mid.magnitude.ShouldEqual(1, 1e-14);
            (V4.Angle(a, mid) / 0.5e-9).ShouldEqual(1.0, 1e-6);
        }

        [Fact]
        private void SlerpAntiparallelUsesOrthonormal()
        {
            V4.Slerp(V4.xaxis, -V4.xaxis, 0.5).ShouldEqual(V4.xaxis.orthonormal);
            V4.Slerp(V4.zaxis, -V4.zaxis, 0.5).ShouldEqual(V4.zaxis.orthonormal);
            V4.Slerp(V4.xaxis, -V4.xaxis, 1).ShouldEqual(-V4.xaxis, 1e-15);
        }

        [Fact]
        private void SlerpWithZeroVectorFallsBackToLerp()
        {
            var b = new V4(2, 4, 6, 8);

            V4.Slerp(V4.zero, b, 0.5).ShouldEqual(new V4(1, 2, 3, 4));
            V4.Slerp(b, V4.zero, 0.5).ShouldEqual(new V4(1, 2, 3, 4));
        }

        [Fact]
        private void SlerpExtrapolates()
        {
            V4 a = V4.xaxis;
            V4 b = V4.yaxis;

            V4.Slerp(a, b, 2).ShouldEqual(-V4.xaxis, 1e-15);
            V4.Slerp(a, b, -1).ShouldEqual(-V4.yaxis, 1e-15);
        }
    }
}
