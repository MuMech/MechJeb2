/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Globalization;
using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Primitives.V4Tests
{
    public class EqualityHashingTests
    {
        [Fact]
        private void EqualsWithIdenticalAndDifferentVectors()
        {
            var v1 = new V4(1.5, -3.2, 0.7, 4);
            var v2 = new V4(1.5, -3.2, 0.7, 4);
            var v3 = new V4(1.5, -3.2, 0.7, 4.1);

            v1.Equals(v2).ShouldBeTrue();
            v1.Equals(v3).ShouldBeFalse();
            v1.Equals((object)v2).ShouldBeTrue();
            v1.Equals(null).ShouldBeFalse();
            v1.Equals(new Q3(1.5, -3.2, 0.7, 4)).ShouldBeFalse();
        }

        [Fact]
        private void NaNEqualsVersusOperator()
        {
            V4 a = V4.nan;
            V4 b = V4.nan;

            a.Equals(b).ShouldBeTrue();
            (a == b).ShouldBeFalse();
            (a != b).ShouldBeTrue();
        }

        [Fact]
        private void HashCodeConsistentWithEquals()
        {
            new V4(1.5, -3.2, 0.7, 4).GetHashCode().ShouldEqual(new V4(1.5, -3.2, 0.7, 4).GetHashCode());

            var set = new HashSet<V4> { new V4(1, 2, 3, 4), new V4(1, 2, 3, 4), new V4(4, 3, 2, 1) };

            set.Count.ShouldEqual(2);
        }

        [Fact]
        private void ToStringFormats()
        {
            var v = new V4(1.5, -2.25, 3, 0.125);

            v.ToString().ShouldEqual("[1.5, -2.25, 3, 0.125]");
            new V4(1.04, -2.06, 3.01, 0.44).ToString("F1").ShouldEqual("[1.0, -2.1, 3.0, 0.4]");
            v.ToString("F3", CultureInfo.GetCultureInfo("de-DE")).ShouldEqual("[1,500, -2,250, 3,000, 0,125]");
            new V4(0.1, 0, 0, 0).ToString().ShouldEqual("[0.10000000000000001, 0, 0, 0]");
        }

        [Fact]
        private void IsFiniteChecks()
        {
            new V4(1, -2, 3, -4).IsFinite().ShouldBeTrue();
            new V4(double.NaN, 0, 0, 0).IsFinite().ShouldBeFalse();
            new V4(0, 0, 0, double.PositiveInfinity).IsFinite().ShouldBeFalse();
            IsFinite(new V4(1, -2, 3, -4)).ShouldBeTrue();
            IsFinite(new V4(0, 0, double.NegativeInfinity, 0)).ShouldBeFalse();
        }

        [Fact]
        private void NearlyEqualRelativeTolerance()
        {
            NearlyEqual(new V4(1e10, 1, 1, 1), new V4(1e10 + 1e-3, 1, 1, 1), 1e-12).ShouldBeTrue();
            NearlyEqual(new V4(1, 1, 1, 1), new V4(1, 1, 1, 1 + 1e-6), 1e-12).ShouldBeFalse();
            NearlyEqual(V4.nan, V4.zero).ShouldBeFalse();
        }

        [Fact]
        private void CopyToAndFromList()
        {
            var v = new V4(3, -4, 5, -6);
            var list = new List<double> { 0, 0, 0, 0, 0, 0 };

            v.CopyTo(list, 1);

            list[0].ShouldEqual(0);
            list[1].ShouldEqual(3);
            list[2].ShouldEqual(-4);
            list[3].ShouldEqual(5);
            list[4].ShouldEqual(-6);
            list[5].ShouldEqual(0);

            var w = new V4();
            w.CopyFrom(list, 1);

            w.ShouldEqual(v);
        }

        [Fact]
        private void CopyToTwoDimensionalArray()
        {
            var v = new V4(3, -4, 5, -6);
            double[,] array = new double[5, 2];

            v.CopyTo(array, 1, 1);

            array[1, 1].ShouldEqual(3);
            array[2, 1].ShouldEqual(-4);
            array[3, 1].ShouldEqual(5);
            array[4, 1].ShouldEqual(-6);
            array[0, 1].ShouldEqual(0);
            array[1, 0].ShouldEqual(0);
        }

        [Fact]
        private void CopyIndicesRoundTrip()
        {
            double[] array = { 10, 20, 30, 40, 50 };

            V4 v = V4.CopyFromIndices(array, (4, 0, 2, 1));

            v.ShouldEqual(new V4(50, 10, 30, 20));

            double[] dest = new double[5];
            v.CopyToIndices(dest, (3, 1, 4, 0));

            dest[3].ShouldEqual(50);
            dest[1].ShouldEqual(10);
            dest[4].ShouldEqual(30);
            dest[0].ShouldEqual(20);
            dest[2].ShouldEqual(0);
        }
    }
}
