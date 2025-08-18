/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using Xunit;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Utils.StaticsTests
{
    public class TrigonometryTests
    {
        [Fact]
        private void SafeAcosSpecialValues()
        {
            SafeAcos(double.NaN).ShouldBeNaN();
            SafeAcos(double.PositiveInfinity).ShouldBeNaN();
            SafeAcos(double.NegativeInfinity).ShouldBeNaN();
        }

        [Fact]
        private void SafeAcosOutOfRange()
        {
            SafeAcos(1.00001).ShouldEqual(0, 1e-15);
            SafeAcos(-1.00001).ShouldEqual(PI, 1e-15);
            SafeAcos(2.0).ShouldEqual(0, 1e-15);
            SafeAcos(-2.0).ShouldEqual(PI, 1e-15);
        }

        [Fact]
        private void SafeAcosWellKnownValues()
        {
            SafeAcos(1).ShouldEqual(0);
            SafeAcos(-1).ShouldEqual(PI);
            SafeAcos(0).ShouldEqual(PI / 2);
            SafeAcos(0.5).ShouldEqual(PI / 3);
            SafeAcos(-0.5).ShouldEqual(2 * PI / 3);
            SafeAcos(Sqrt(2) / 2).ShouldEqual(PI / 4);
            SafeAcos(-Sqrt(2) / 2).ShouldEqual(3 * PI / 4);
            SafeAcos(Sqrt(3) / 2).ShouldEqual(PI / 6, 1e-15);
            SafeAcos(-Sqrt(3) / 2).ShouldEqual(5 * PI / 6);
        }

        [Fact]
        private void SafeAsinSpecialValues()
        {
            SafeAsin(double.NaN).ShouldBeNaN();
            SafeAsin(double.PositiveInfinity).ShouldBeNaN();
            SafeAsin(double.NegativeInfinity).ShouldBeNaN();
        }

        [Fact]
        private void SafeAsinOutOfRange()
        {
            SafeAsin(1.00001).ShouldEqual(PI / 2, 1e-15);
            SafeAsin(-1.00001).ShouldEqual(-PI / 2, 1e-15);
            SafeAsin(2.0).ShouldEqual(PI / 2, 1e-15);
            SafeAsin(-2.0).ShouldEqual(-PI / 2, 1e-15);
        }

        [Fact]
        private void SafeAsinWellKnownValues()
        {
            SafeAsin(0).ShouldEqual(0);
            SafeAsin(1).ShouldEqual(PI / 2);
            SafeAsin(-1).ShouldEqual(-PI / 2);
            SafeAsin(0.5).ShouldEqual(PI / 6);
            SafeAsin(-0.5).ShouldEqual(-PI / 6);
            SafeAsin(Sqrt(2) / 2).ShouldEqual(PI / 4);
            SafeAsin(-Sqrt(2) / 2).ShouldEqual(-PI / 4);
            SafeAsin(Sqrt(3) / 2).ShouldEqual(PI / 3);
            SafeAsin(-Sqrt(3) / 2).ShouldEqual(-PI / 3);
        }
    }
}
