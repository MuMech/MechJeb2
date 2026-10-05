/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V2Tests
{
    public class ConstructorAccessorTests
    {
        [Fact]
        private void ConstructorSetsComponents()
        {
            var v = new V2(1.5, -2.5);

            v.x.ShouldEqual(1.5);
            v.y.ShouldEqual(-2.5);
        }

        [Fact]
        private void DefaultIsZero()
        {
            var v = new V2();

            v.ShouldEqual(V2.zero);
        }

        [Fact]
        private void IndexerGetter()
        {
            var v = new V2(3, 4);

            v[0].ShouldEqual(3);
            v[1].ShouldEqual(4);
        }

        [Fact]
        private void IndexerSetter()
        {
            var v = new V2(0, 0);
            v[0] = 11;
            v[1] = 22;

            v.x.ShouldEqual(11);
            v.y.ShouldEqual(22);
        }

        [Fact]
        private void IndexerThrowsOnInvalidIndex()
        {
            var v = new V2(1, 2);

            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                double _ = v[2];
            });
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                double _ = v[-1];
            });
            Assert.Throws<IndexOutOfRangeException>(() => { v[2] = 42; });
            Assert.Throws<IndexOutOfRangeException>(() => { v[-1] = 42; });
        }

        [Fact]
        private void SetMethod()
        {
            var v = new V2(0, 0);
            v.Set(2.5, 3.5);

            v.x.ShouldEqual(2.5);
            v.y.ShouldEqual(3.5);
        }

        [Fact]
        private void Constants()
        {
            V2.zero.ShouldEqual(new V2(0, 0));
            V2.one.ShouldEqual(new V2(1, 1));
            V2.xaxis.ShouldEqual(new V2(1, 0));
            V2.yaxis.ShouldEqual(new V2(0, 1));
            V2.maxvalue.ShouldEqual(new V2(double.MaxValue, double.MaxValue));
            V2.minvalue.ShouldEqual(new V2(double.MinValue, double.MinValue));
            V2.positiveinfinity.x.ShouldBePositiveInfinity();
            V2.positiveinfinity.y.ShouldBePositiveInfinity();
            V2.negativeinfinity.x.ShouldBeNegativeInfinity();
            V2.negativeinfinity.y.ShouldBeNegativeInfinity();
            V2.nan.x.ShouldBeNaN();
            V2.nan.y.ShouldBeNaN();
        }

        [Fact]
        private void AxesAreOrthonormalAndCounterClockwise()
        {
            V2.Dot(V2.xaxis, V2.yaxis).ShouldEqual(0);
            V2.xaxis.magnitude.ShouldEqual(1);
            V2.yaxis.magnitude.ShouldEqual(1);
            V2.Cross(V2.xaxis, V2.yaxis).ShouldEqual(1);
        }
    }
}
