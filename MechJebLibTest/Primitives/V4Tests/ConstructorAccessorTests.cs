/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V4Tests
{
    public class ConstructorAccessorTests
    {
        [Fact]
        private void ConstructorSetsComponents()
        {
            var v = new V4(1.5, -2.5, 3.5, -4.5);

            v.x.ShouldEqual(1.5);
            v.y.ShouldEqual(-2.5);
            v.z.ShouldEqual(3.5);
            v.w.ShouldEqual(-4.5);
        }

        [Fact]
        private void DefaultIsZero()
        {
            var v = new V4();

            v.ShouldEqual(V4.zero);
        }

        [Fact]
        private void IndexerGetter()
        {
            var v = new V4(3, 4, 5, 6);

            v[0].ShouldEqual(3);
            v[1].ShouldEqual(4);
            v[2].ShouldEqual(5);
            v[3].ShouldEqual(6);
        }

        [Fact]
        private void IndexerSetter()
        {
            var v = new V4(0, 0, 0, 0);
            v[0] = 11;
            v[1] = 22;
            v[2] = 33;
            v[3] = 44;

            v.ShouldEqual(new V4(11, 22, 33, 44));
        }

        [Fact]
        private void IndexerThrowsOnInvalidIndex()
        {
            var v = new V4(1, 2, 3, 4);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                double _ = v[4];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                double _ = v[-1];
            });
            Assert.Throws<IndexOutOfRangeException>(() => { v[4] = 42; });
            Assert.Throws<IndexOutOfRangeException>(() => { v[-1] = 42; });
        }

        [Fact]
        private void SetMethod()
        {
            var v = new V4(0, 0, 0, 0);
            v.Set(2.5, 3.5, 4.5, 5.5);

            v.ShouldEqual(new V4(2.5, 3.5, 4.5, 5.5));
        }

        [Fact]
        private void Constants()
        {
            V4.zero.ShouldEqual(new V4(0, 0, 0, 0));
            V4.one.ShouldEqual(new V4(1, 1, 1, 1));
            V4.xaxis.ShouldEqual(new V4(1, 0, 0, 0));
            V4.yaxis.ShouldEqual(new V4(0, 1, 0, 0));
            V4.zaxis.ShouldEqual(new V4(0, 0, 1, 0));
            V4.waxis.ShouldEqual(new V4(0, 0, 0, 1));
            V4.maxvalue.ShouldEqual(new V4(double.MaxValue, double.MaxValue, double.MaxValue, double.MaxValue));
            V4.minvalue.ShouldEqual(new V4(double.MinValue, double.MinValue, double.MinValue, double.MinValue));

            for (int i = 0; i < 4; i++)
            {
                V4.positiveinfinity[i].ShouldBePositiveInfinity();
                V4.negativeinfinity[i].ShouldBeNegativeInfinity();
                V4.nan[i].ShouldBeNaN();
            }
        }

        [Fact]
        private void AxesAreOrthonormal()
        {
            V4[] axes = { V4.xaxis, V4.yaxis, V4.zaxis, V4.waxis };

            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                V4.Dot(axes[i], axes[j]).ShouldEqual(i == j ? 1 : 0);
        }
    }
}
