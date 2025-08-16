/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class ConstructorAccessorTests
    {
        [Fact]
        private void ThreeParameterConstructor()
        {
            var v = new V3(1.5, 2.5, 3.5);
            v.x.ShouldEqual(1.5);
            v.y.ShouldEqual(2.5);
            v.z.ShouldEqual(3.5);
        }

        [Fact]
        private void TwoParameterConstructorSetsZToZero()
        {
            var v = new V3(4.2, 5.3);
            v.x.ShouldEqual(4.2);
            v.y.ShouldEqual(5.3);
            v.z.ShouldEqual(0.0);
        }

        [Fact]
        private void XYZAccessors()
        {
            var v = new V3(10, 20, 30);
            v.x.ShouldEqual(10);
            v.y.ShouldEqual(20);
            v.z.ShouldEqual(30);

            v.x = 100;
            v.y = 200;
            v.z = 300;

            v.x.ShouldEqual(100);
            v.y.ShouldEqual(200);
            v.z.ShouldEqual(300);
        }

        [Fact]
        private void RollPitchYawAccessors()
        {
            var v = new V3(1.1, 2.2, 3.3);
            v.roll.ShouldEqual(1.1);
            v.pitch.ShouldEqual(2.2);
            v.yaw.ShouldEqual(3.3);

            v.roll  = 4.4;
            v.pitch = 5.5;
            v.yaw   = 6.6;

            v.x.ShouldEqual(4.4);
            v.y.ShouldEqual(5.5);
            v.z.ShouldEqual(6.6);
        }

        [Fact]
        private void IndexerGetter()
        {
            var v = new V3(7, 8, 9);
            v[0].ShouldEqual(7);
            v[1].ShouldEqual(8);
            v[2].ShouldEqual(9);
        }

        [Fact]
        private void IndexerSetter()
        {
            var v = new V3(0, 0, 0);

            v[0] = 11;
            v[1] = 22;
            v[2] = 33;

            v.x.ShouldEqual(11);
            v.y.ShouldEqual(22);
            v.z.ShouldEqual(33);
        }

        [Fact]
        private void IndexerThrowsOnInvalidIndex()
        {
            var v = new V3(1, 2, 3);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                double _ = v[3];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                double _ = v[-1];
            });
            Assert.Throws<IndexOutOfRangeException>(() => { v[3]  = 42; });
            Assert.Throws<IndexOutOfRangeException>(() => { v[-1] = 42; });
        }

        [Fact]
        private void SetMethod()
        {
            var v = new V3(0, 0, 0);
            v.Set(2.5, 3.5, 4.5);

            v.x.ShouldEqual(2.5);
            v.y.ShouldEqual(3.5);
            v.z.ShouldEqual(4.5);
        }

        [Fact]
        private void SetMethodOverwritesExistingValues()
        {
            var v = new V3(99, 88, 77);
            v.Set(-1, -2, -3);

            v.x.ShouldEqual(-1);
            v.y.ShouldEqual(-2);
            v.z.ShouldEqual(-3);
        }

        [Fact]
        private void NegativeAndZeroValues()
        {
            var v = new V3(-1.5, 0, 2.5);
            v.x.ShouldEqual(-1.5);
            v.y.ShouldEqual(0);
            v.z.ShouldEqual(2.5);

            v[0] = 0;
            v[1] = -100;
            v[2] = 0;

            v.x.ShouldEqual(0);
            v.y.ShouldEqual(-100);
            v.z.ShouldEqual(0);
        }

        [Fact]
        private void LargeAndSmallValues()
        {
            var v = new V3(1e100, 1e-100, 1e50);
            v.x.ShouldEqual(1e100);
            v.y.ShouldEqual(1e-100);
            v.z.ShouldEqual(1e50);

            v.Set(1e-200, 1e200, 0);
            v[0].ShouldEqual(1e-200);
            v[1].ShouldEqual(1e200);
            v[2].ShouldEqual(0);
        }
    }
}
