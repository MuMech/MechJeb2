/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.V2Tests
{
    public class InterpolationTests
    {
        [Fact]
        private void LerpEndpointsAndMidpoint()
        {
            var a = new V2(1, 2);
            var b = new V2(5, -6);

            V2.Lerp(a, b, 0).ShouldEqual(a);
            V2.Lerp(a, b, 1).ShouldEqual(b);
            V2.Lerp(a, b, 0.5).ShouldEqual(new V2(3, -2));
        }

        [Fact]
        private void LerpExtrapolates()
        {
            var a = new V2(0, 0);
            var b = new V2(1, 2);

            V2.Lerp(a, b, 2).ShouldEqual(new V2(2, 4));
            V2.Lerp(a, b, -1).ShouldEqual(new V2(-1, -2));
        }

        [Fact]
        private void SlerpEndpoints()
        {
            var a = new V2(2, 0);
            var b = new V2(0, 3);

            V2.Slerp(a, b, 0).ShouldEqual(a);
            V2.Slerp(a, b, 1).ShouldEqual(b);
        }

        [Fact]
        private void SlerpMidpointOfUnitVectors()
        {
            V2.Slerp(V2.xaxis, V2.yaxis, 0.5).ShouldEqual(new V2(Sqrt(0.5), Sqrt(0.5)));
        }

        [Fact]
        private void SlerpConstantAngularRate()
        {
            V2 a = V2.xaxis;
            var b = new V2(Cos(2.0), Sin(2.0));

            for (int i = 0; i <= 10; i++)
            {
                double t = i / 10.0;
                V2.SignedAngle(a, V2.Slerp(a, b, t)).ShouldEqual(2.0 * t, 1e-14);
            }
        }

        [Fact]
        private void SlerpTakesShortWayClockwise()
        {
            V2 a = V2.xaxis;
            var b = new V2(0, -1);

            V2.Slerp(a, b, 0.5).ShouldEqual(new V2(Sqrt(0.5), -Sqrt(0.5)));
        }

        [Fact]
        private void SlerpInterpolatesMagnitudeLinearly()
        {
            var a = new V2(2, 0);
            var b = new V2(0, 4);

            V2 mid = V2.Slerp(a, b, 0.5);

            mid.magnitude.ShouldEqual(3, 1e-14);
            V2.Angle(a, mid).ShouldEqual(PI / 4, 1e-14);
        }

        [Fact]
        private void SlerpNearlyParallel()
        {
            V2 a = V2.xaxis;
            var b = new V2(Cos(1e-9), Sin(1e-9));

            V2 mid = V2.Slerp(a, b, 0.5);

            mid.magnitude.ShouldEqual(1, 1e-14);
            (V2.SignedAngle(a, mid) / 0.5e-9).ShouldEqual(1.0, 1e-6);
        }

        [Fact]
        private void SlerpAntiparallelRotatesCounterClockwise()
        {
            V2 a = V2.xaxis;
            V2 b = -V2.xaxis;

            V2.Slerp(a, b, 0.5).ShouldEqual(V2.yaxis);
            V2.Slerp(a, b, 1).ShouldEqual(b);
        }

        [Fact]
        private void SlerpWithZeroVectorFallsBackToLerp()
        {
            var b = new V2(2, 4);

            V2.Slerp(V2.zero, b, 0.5).ShouldEqual(new V2(1, 2));
            V2.Slerp(b, V2.zero, 0.5).ShouldEqual(new V2(1, 2));
        }

        [Fact]
        private void SlerpExtrapolates()
        {
            V2 a = V2.xaxis;
            V2 b = V2.yaxis;

            V2.Slerp(a, b, 2).ShouldEqual(-V2.xaxis);
            V2.Slerp(a, b, -1).ShouldEqual(-V2.yaxis);
        }
    }
}
