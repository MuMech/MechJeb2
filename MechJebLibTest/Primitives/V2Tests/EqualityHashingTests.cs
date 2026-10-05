/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Globalization;
using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Primitives.V2Tests
{
    public class EqualityHashingTests
    {
        [Fact]
        private void EqualsWithIdenticalAndDifferentVectors()
        {
            var v1 = new V2(1.5, -3.2);
            var v2 = new V2(1.5, -3.2);
            var v3 = new V2(1.5, -3.3);

            v1.Equals(v2).ShouldBeTrue();
            v1.Equals(v3).ShouldBeFalse();
            v1.Equals((object)v2).ShouldBeTrue();
            v1.Equals(null).ShouldBeFalse();
            v1.Equals(new V3(1.5, -3.2)).ShouldBeFalse();
        }

        [Fact]
        private void NaNEqualsVersusOperator()
        {
            V2 a = V2.nan;
            V2 b = V2.nan;

            a.Equals(b).ShouldBeTrue();
            (a == b).ShouldBeFalse();
            (a != b).ShouldBeTrue();
        }

        [Fact]
        private void HashCodeConsistentWithEquals()
        {
            new V2(1.5, -3.2).GetHashCode().ShouldEqual(new V2(1.5, -3.2).GetHashCode());

            var set = new HashSet<V2> { new V2(1, 2), new V2(1, 2), new V2(2, 1) };

            set.Count.ShouldEqual(2);
        }

        [Fact]
        private void ToStringFormats()
        {
            var v = new V2(1.5, -2.25);

            v.ToString().ShouldEqual("[1.5, -2.25]");
            new V2(1.04, -2.06).ToString("F1").ShouldEqual("[1.0, -2.1]");
            v.ToString("F3", CultureInfo.GetCultureInfo("de-DE")).ShouldEqual("[1,500, -2,250]");
            new V2(0.1, 0).ToString().ShouldEqual("[0.10000000000000001, 0]");
        }

        [Fact]
        private void IsFiniteChecks()
        {
            new V2(1, -2).IsFinite().ShouldBeTrue();
            new V2(double.NaN, 0).IsFinite().ShouldBeFalse();
            new V2(0, double.PositiveInfinity).IsFinite().ShouldBeFalse();
            IsFinite(new V2(1, -2)).ShouldBeTrue();
            IsFinite(new V2(0, double.NegativeInfinity)).ShouldBeFalse();
        }

        [Fact]
        private void NearlyEqualRelativeTolerance()
        {
            NearlyEqual(new V2(1e10, 1), new V2(1e10 + 1e-3, 1), 1e-12).ShouldBeTrue();
            NearlyEqual(new V2(1, 1), new V2(1 + 1e-6, 1), 1e-12).ShouldBeFalse();
            NearlyEqual(V2.nan, new V2(0, 0)).ShouldBeFalse();
        }

        [Fact]
        private void CopyToAndFromList()
        {
            var v = new V2(3, -4);
            var list = new List<double> { 0, 0, 0, 0 };

            v.CopyTo(list, 1);

            list[0].ShouldEqual(0);
            list[1].ShouldEqual(3);
            list[2].ShouldEqual(-4);
            list[3].ShouldEqual(0);

            var w = new V2();
            w.CopyFrom(list, 1);

            w.ShouldEqual(v);
        }

        [Fact]
        private void CopyToTwoDimensionalArray()
        {
            var v = new V2(3, -4);
            double[,] array = new double[3, 2];

            v.CopyTo(array, 1, 1);

            array[1, 1].ShouldEqual(3);
            array[2, 1].ShouldEqual(-4);
            array[0, 1].ShouldEqual(0);
            array[1, 0].ShouldEqual(0);
        }

        [Fact]
        private void CopyIndicesRoundTrip()
        {
            double[] array = { 10, 20, 30, 40 };

            V2 v = V2.CopyFromIndices(array, (3, 0));

            v.ShouldEqual(new V2(40, 10));

            double[] dest = new double[4];
            v.CopyToIndices(dest, (2, 1));

            dest[2].ShouldEqual(40);
            dest[1].ShouldEqual(10);
        }
    }
}
