/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class CoordinateConversionTests
    {
        [Fact]
        private void SphericalToCartesianUnitSphere()
        {
            var v = new V3(1, 0, 0);
            v.sph2cart.ShouldEqual(new V3(0, 0, 1));

            v = new V3(1, PI / 2, 0);
            v.sph2cart.ShouldEqual(new V3(1, 0, 0), 1e-15);

            v = new V3(1, PI / 2, PI / 2);
            v.sph2cart.ShouldEqual(new V3(0, 1, 0), 1e-15);
        }

        [Fact]
        private void SphericalToCartesianBasicAngles()
        {
            var v = new V3(2, PI / 4, 0);
            V3 result = v.sph2cart;
            result.x.ShouldEqual(Sqrt(2), 1e-15);
            result.y.ShouldEqual(0, 1e-15);
            result.z.ShouldEqual(Sqrt(2), 1e-15);

            v = new V3(3, PI / 3, PI / 6);
            result = v.sph2cart;
            result.x.ShouldEqual(3 * Cos(PI / 6) * Sin(PI / 3), 1e-15);
            result.y.ShouldEqual(3 * Sin(PI / 6) * Sin(PI / 3), 1e-15);
            result.z.ShouldEqual(3 * Cos(PI / 3), 1e-15);
        }

        [Fact]
        private void SphericalToCartesianZeroRadius()
        {
            var v = new V3(0, PI / 4, PI / 3);
            v.sph2cart.ShouldBeZero();

            v = new V3(0, 0, 0);
            v.sph2cart.ShouldBeZero();
        }

        [Fact]
        private void SphericalToCartesianNegativeRadius()
        {
            var v = new V3(-2, 0, 0);
            v.sph2cart.ShouldEqual(new V3(0, 0, -2));

            v = new V3(-3, PI / 2, 0);
            v.sph2cart.ShouldEqual(new V3(-3, 0, 0), 1e-15);
        }

        [Fact]
        private void SphericalToCartesianFullSphere()
        {
            var v = new V3(5, 0, 0);
            v.sph2cart.ShouldEqual(new V3(0, 0, 5));

            v = new V3(5, PI, 0);
            v.sph2cart.ShouldEqual(new V3(0, 0, -5), 1e-15);

            v = new V3(4, PI / 2, 0);
            v.sph2cart.ShouldEqual(new V3(4, 0, 0), 1e-15);

            v = new V3(4, PI / 2, PI);
            v.sph2cart.ShouldEqual(new V3(-4, 0, 0), 1e-15);

            v = new V3(4, PI / 2, 3 * PI / 2);
            v.sph2cart.ShouldEqual(new V3(0, -4, 0), 1e-15);
        }

        [Fact]
        private void CartesianToSphericalBasicVectors()
        {
            V3 result = V3.xaxis.cart2sph;
            result.x.ShouldEqual(1);
            result.y.ShouldEqual(PI / 2);
            result.z.ShouldEqual(0);

            result = V3.yaxis.cart2sph;
            result.x.ShouldEqual(1);
            result.y.ShouldEqual(PI / 2);
            result.z.ShouldEqual(PI / 2);

            result = V3.zaxis.cart2sph;
            result.x.ShouldEqual(1);
            result.y.ShouldEqual(0);
            result.z.ShouldEqual(0);
        }

        [Fact]
        private void CartesianToSphericalArbitraryVectors()
        {
            var v = new V3(3, 4, 0);
            V3 result = v.cart2sph;
            result.x.ShouldEqual(5);
            result.y.ShouldEqual(PI / 2);
            result.z.ShouldEqual(Atan2(4, 3));

            v = new V3(1, 1, 1);
            result = v.cart2sph;
            result.x.ShouldEqual(Sqrt(3));
            result.y.ShouldEqual(Acos(1 / Sqrt(3)), 1e-15);
            result.z.ShouldEqual(PI / 4, 1e-15);
        }

        [Fact]
        private void CartesianToSphericalZeroVector()
        {
            V3 result = V3.zero.cart2sph;
            result.x.ShouldEqual(0);
            result.y.ShouldEqual(0);
            result.z.ShouldEqual(0);
        }

        [Fact]
        private void CartesianToSphericalNegativeCoordinates()
        {
            var v = new V3(-1, 0, 0);
            V3 result = v.cart2sph;
            result.x.ShouldEqual(1);
            result.y.ShouldEqual(PI / 2);
            result.z.ShouldEqual(PI);

            v = new V3(0, -1, 0);
            result = v.cart2sph;
            result.x.ShouldEqual(1);
            result.y.ShouldEqual(PI / 2);
            result.z.ShouldEqual(3 * PI / 2);

            v = new V3(-1, -1, 0);
            result = v.cart2sph;
            result.x.ShouldEqual(Sqrt(2));
            result.y.ShouldEqual(PI / 2);
            result.z.ShouldEqual(5 * PI / 4);
        }

        [Fact]
        private void CartesianToSphericalPhiRange()
        {
            var v = new V3(1, 0.001, 0);
            V3 result = v.cart2sph;
            result.z.ShouldBePositive();
            result.z.ShouldBeLessThan(TAU);

            v = new V3(1, -0.001, 0);
            result = v.cart2sph;
            result.z.ShouldBePositive();
            result.z.ShouldBeLessThan(TAU);

            v = new V3(-1, 0.001, 0);
            result = v.cart2sph;
            result.z.ShouldBePositive();
            result.z.ShouldBeLessThan(TAU);
        }

        [Fact]
        private void CartesianToSphericalSmallAnglePrecision()
        {
            const double TINY = 1e-10;
            var v = new V3(TINY, 0, 1);

            V3 result = v.cart2sph;
            double expectedTheta = Atan2(TINY, 1);

            result.x.ShouldEqual(Sqrt(1 + TINY * TINY));
            result.y.ShouldEqual(expectedTheta, 1e-20);
            result.z.ShouldEqual(0);

            const double SMALL = 1e-12;
            v = new V3(1, 0, SMALL);
            result = v.cart2sph;

            expectedTheta = PI / 2 - SMALL;

            result.y.ShouldEqual(expectedTheta, 1e-20);
        }

        [Fact]
        private void CartesianToSphericalPhiSmallAnglePrecision()
        {
            const double TINY = 1e-14;

            var v = new V3(1, TINY, 0);
            V3 result = v.cart2sph;

            result.z.ShouldEqual(TINY, 1e-28);

            v = new V3(-1, TINY, 0);
            result = v.cart2sph;

            result.z.ShouldEqual(PI - TINY, 1e-14);
        }

        [Fact]
        private void CartesianToSphericalRhoOverflow()
        {
            const double LARGE = 1.5e154;
            var          v     = new V3(LARGE, LARGE, LARGE);

            V3 result = v.cart2sph;

            result.x.ShouldEqual(LARGE * Sqrt(3));
            result.y.ShouldEqual(V3.Angle(new V3(0, 0, 1), new V3(1, 1, 1)));
            result.z.ShouldEqual(PI / 4);
        }

        [Fact]
        private void SphericalCartesianRoundTrip()
        {
            var original = new V3(3, 4, 5);
            V3 spherical = original.cart2sph;
            V3 backToCart = spherical.sph2cart;
            backToCart.ShouldEqual(original, 1e-14);

            original = new V3(-2, 3, -1);
            spherical = original.cart2sph;
            backToCart = spherical.sph2cart;
            backToCart.ShouldEqual(original, 1e-14);

            original = new V3(1, 0, 0);
            spherical = original.cart2sph;
            backToCart = spherical.sph2cart;
            backToCart.ShouldEqual(original, 1e-14);
        }

        [Fact]
        private void CartesianSphericalRoundTrip()
        {
            var original = new V3(5, PI / 3, PI / 4);
            V3 cartesian = original.sph2cart;
            V3 backToSph = cartesian.cart2sph;
            backToSph.ShouldEqual(original, 1e-14);

            original = new V3(2, PI / 6, 3 * PI / 2);
            cartesian = original.sph2cart;
            backToSph = cartesian.cart2sph;
            backToSph.ShouldEqual(original, 1e-14);
        }

        [Fact]
        private void XZYSwizzleBasicVectors()
        {
            V3.xaxis.xzy.ShouldEqual(new V3(1, 0, 0));
            V3.yaxis.xzy.ShouldEqual(new V3(0, 0, 1));
            V3.zaxis.xzy.ShouldEqual(new V3(0, 1, 0));
        }

        [Fact]
        private void XZYSwizzleArbitraryVectors()
        {
            var v = new V3(1, 2, 3);
            v.xzy.ShouldEqual(new V3(1, 3, 2));

            v = new V3(-5, 7, -11);
            v.xzy.ShouldEqual(new V3(-5, -11, 7));

            v = new V3(0, 0, 0);
            v.xzy.ShouldEqual(V3.zero);
        }

        [Fact]
        private void XZYSwizzleRoundTrip()
        {
            var v = new V3(4, 5, 6);
            v.xzy.xzy.ShouldEqual(v);

            v = new V3(-1, 2, -3);
            v.xzy.xzy.ShouldEqual(v);

            v = new V3(1e100, 1e-100, 0);
            v.xzy.xzy.ShouldEqual(v);
        }

        [Fact]
        private void XZYSwizzlePreservesMagnitude()
        {
            var v = new V3(3, 4, 5);
            v.xzy.magnitude.ShouldEqual(v.magnitude);

            v = new V3(-2, 7, -11);
            v.xzy.magnitude.ShouldEqual(v.magnitude, 1e-15);

            v = new V3(1e50, 1e-50, 1e25);
            v.xzy.magnitude.ShouldEqual(v.magnitude);
        }

        [Fact]
        private void SphericalToCartesianLargeRadius()
        {
            const double LARGE = 1e150;
            var v = new V3(LARGE, PI / 4, PI / 3);
            V3 result = v.sph2cart;
            result.magnitude.ShouldEqual(LARGE, 1e135);
        }

        [Fact]
        private void SphericalToCartesianSmallRadius()
        {
            const double SMALL = 1e-150;
            var v = new V3(SMALL, PI / 4, PI / 3);
            V3 result = v.sph2cart;
            result.magnitude.ShouldEqual(SMALL, 1e-165);
        }

        [Fact]
        private void CartesianToSphericalLargeVectors()
        {
            const double LARGE = 1e150;
            var v = new V3(LARGE, LARGE, LARGE);
            V3 result = v.cart2sph;
            result.x.ShouldEqual(Sqrt(3) * LARGE, 1e135);
        }

        [Fact]
        private void CartesianToSphericalSmallVectors()
        {
            const double SMALL = 1e-150;
            var v = new V3(SMALL, SMALL, SMALL);
            V3 result = v.cart2sph;
            result.x.ShouldEqual(Sqrt(3) * SMALL, 1e-165);
        }

        [Fact]
        private void SphericalToCartesianSpecialAngles()
        {
            var v = new V3(2, 0, TAU);
            v.sph2cart.ShouldEqual(new V3(0, 0, 2), 1e-15);

            v = new V3(3, TAU, 0);
            v.sph2cart.ShouldEqual(new V3(0, 0, 3), 1e-15);

            v = new V3(4, PI / 2, TAU);
            v.sph2cart.ShouldEqual(new V3(4, 0, 0), 1e-15);
        }

        [Fact]
        private void CartesianToSphericalOnAxis()
        {
            var v = new V3(5, 0, 0);
            V3 result = v.cart2sph;
            result.x.ShouldEqual(5);
            result.y.ShouldEqual(PI / 2, 1e-15);
            result.z.ShouldEqual(0);

            v = new V3(0, 0, 7);
            result = v.cart2sph;
            result.x.ShouldEqual(7);
            result.y.ShouldEqual(0, 1e-15);
            result.z.ShouldEqual(0);

            v = new V3(0, 0, -7);
            result = v.cart2sph;
            result.x.ShouldEqual(7);
            result.y.ShouldEqual(PI, 1e-15);
            result.z.ShouldEqual(0, 1e-15);
        }

        [Fact]
        private void XZYSwizzleWithSpecialValues()
        {
            V3.positiveinfinity.xzy.ShouldEqual(V3.positiveinfinity);
            V3.negativeinfinity.xzy.ShouldEqual(V3.negativeinfinity);

            var v = new V3(double.NaN, 1, 2);
            V3 result = v.xzy;
            result.x.ShouldBeNaN();
            result.y.ShouldEqual(2);
            result.z.ShouldEqual(1);

            v = new V3(double.PositiveInfinity, double.NegativeInfinity, 0);
            result = v.xzy;
            result.x.ShouldBePositiveInfinity();
            result.y.ShouldEqual(0);
            result.z.ShouldBeNegativeInfinity();
        }
    }
}
