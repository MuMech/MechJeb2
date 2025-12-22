/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class UtilityMethodTests
    {
        [Fact]
        private void IsFiniteWithNormalValues()
        {
            new V3(1, 2, 3).IsFinite().ShouldBeTrue();
            new V3(0, 0, 0).IsFinite().ShouldBeTrue();
            new V3(-1, -2, -3).IsFinite().ShouldBeTrue();
            new V3(1e100, 1e-100, 0).IsFinite().ShouldBeTrue();
        }

        [Fact]
        private void IsFiniteWithNaN()
        {
            new V3(double.NaN, 0, 0).IsFinite().ShouldBeFalse();
            new V3(0, double.NaN, 0).IsFinite().ShouldBeFalse();
            new V3(0, 0, double.NaN).IsFinite().ShouldBeFalse();
            new V3(double.NaN, double.NaN, double.NaN).IsFinite().ShouldBeFalse();
            new V3(1, 2, double.NaN).IsFinite().ShouldBeFalse();
        }

        [Fact]
        private void IsFiniteWithInfinity()
        {
            new V3(double.PositiveInfinity, 0, 0).IsFinite().ShouldBeFalse();
            new V3(0, double.NegativeInfinity, 0).IsFinite().ShouldBeFalse();
            new V3(0, 0, double.PositiveInfinity).IsFinite().ShouldBeFalse();
            new V3(double.PositiveInfinity, double.NegativeInfinity, 0).IsFinite().ShouldBeFalse();
            new V3(1, 2, double.NegativeInfinity).IsFinite().ShouldBeFalse();
        }

        [Fact]
        private void IsFiniteWithMixedSpecialValues()
        {
            new V3(double.NaN, double.PositiveInfinity, 0).IsFinite().ShouldBeFalse();
            new V3(1, double.NaN, double.NegativeInfinity).IsFinite().ShouldBeFalse();
            new V3(double.PositiveInfinity, double.NegativeInfinity, double.NaN).IsFinite().ShouldBeFalse();
        }

        [Fact]
        private void IsFiniteWithLargeFiniteValues()
        {
            new V3(double.MaxValue, 0, 0).IsFinite().ShouldBeTrue();
            new V3(double.MinValue, 0, 0).IsFinite().ShouldBeTrue();
            new V3(double.MaxValue, double.MinValue, 0).IsFinite().ShouldBeTrue();
            new V3(1e308, -1e308, 1e-300).IsFinite().ShouldBeTrue();
        }

        [Fact]
        private void IsFiniteWithEpsilonValues()
        {
            new V3(double.Epsilon, 0, 0).IsFinite().ShouldBeTrue();
            new V3(0, double.Epsilon, 0).IsFinite().ShouldBeTrue();
            new V3(double.Epsilon, double.Epsilon, double.Epsilon).IsFinite().ShouldBeTrue();
        }

        [Fact]
        private void CopyFromListAtDefaultIndex()
        {
            var list = new List<double>
            {
                1.5,
                2.5,
                3.5,
                4.5,
                5.5
            };
            var v = new V3(0, 0, 0);

            v.CopyFrom(list);

            v.x.ShouldEqual(1.5);
            v.y.ShouldEqual(2.5);
            v.z.ShouldEqual(3.5);
        }

        [Fact]
        private void CopyFromListAtSpecificIndex()
        {
            var list = new List<double>
            {
                1,
                2,
                3,
                4,
                5,
                6,
                7
            };
            var v = new V3(0, 0, 0);

            v.CopyFrom(list, 2);

            v.x.ShouldEqual(3);
            v.y.ShouldEqual(4);
            v.z.ShouldEqual(5);
        }

        [Fact]
        private void CopyFromArrayAtDefaultIndex()
        {
            double[] array = { 7.7, 8.8, 9.9 };
            var      v     = new V3(0, 0, 0);

            v.CopyFrom(array);

            v.x.ShouldEqual(7.7);
            v.y.ShouldEqual(8.8);
            v.z.ShouldEqual(9.9);
        }

        [Fact]
        private void CopyFromArrayAtSpecificIndex()
        {
            double[] array = { 1, 2, 3, 4, 5, 6 };
            var      v     = new V3(0, 0, 0);

            v.CopyFrom(array, 3);

            v.x.ShouldEqual(4);
            v.y.ShouldEqual(5);
            v.z.ShouldEqual(6);
        }

        [Fact]
        private void CopyFromOverwritesExistingValues()
        {
            var list = new List<double> { 10, 20, 30 };
            var v    = new V3(99, 88, 77);

            v.CopyFrom(list);

            v.x.ShouldEqual(10);
            v.y.ShouldEqual(20);
            v.z.ShouldEqual(30);
        }

        [Fact]
        private void CopyFromWithNegativeValues()
        {
            double[] array = { -1.5, -2.5, -3.5 };
            var      v     = new V3(0, 0, 0);

            v.CopyFrom(array);

            v.x.ShouldEqual(-1.5);
            v.y.ShouldEqual(-2.5);
            v.z.ShouldEqual(-3.5);
        }

        [Fact]
        private void CopyFromWithSpecialValues()
        {
            var list = new List<double> { double.NaN, double.PositiveInfinity, double.NegativeInfinity };
            var v    = new V3(0, 0, 0);

            v.CopyFrom(list);

            v.x.ShouldBeNaN();
            v.y.ShouldBePositiveInfinity();
            v.z.ShouldBeNegativeInfinity();
        }

        [Fact]
        private void CopyToListAtDefaultIndex()
        {
            var v = new V3(1.5, 2.5, 3.5);
            var list = new List<double>
            {
                0,
                0,
                0,
                0,
                0
            };

            v.CopyTo(list);

            list[0].ShouldEqual(1.5);
            list[1].ShouldEqual(2.5);
            list[2].ShouldEqual(3.5);
            list[3].ShouldEqual(0);
            list[4].ShouldEqual(0);
        }

        [Fact]
        private void CopyToListAtSpecificIndex()
        {
            var v = new V3(7, 8, 9);
            var list = new List<double>
            {
                1,
                2,
                3,
                4,
                5,
                6,
                7
            };

            v.CopyTo(list, 2);

            list[0].ShouldEqual(1);
            list[1].ShouldEqual(2);
            list[2].ShouldEqual(7);
            list[3].ShouldEqual(8);
            list[4].ShouldEqual(9);
            list[5].ShouldEqual(6);
            list[6].ShouldEqual(7);
        }

        [Fact]
        private void CopyToArrayAtDefaultIndex()
        {
            var      v     = new V3(11.1, 22.2, 33.3);
            double[] array = { 0, 0, 0, 0 };

            v.CopyTo(array);

            array[0].ShouldEqual(11.1);
            array[1].ShouldEqual(22.2);
            array[2].ShouldEqual(33.3);
            array[3].ShouldEqual(0);
        }

        [Fact]
        private void CopyToArrayAtSpecificIndex()
        {
            var      v     = new V3(100, 200, 300);
            double[] array = new double[6];

            v.CopyTo(array, 3);

            array[0].ShouldEqual(0);
            array[1].ShouldEqual(0);
            array[2].ShouldEqual(0);
            array[3].ShouldEqual(100);
            array[4].ShouldEqual(200);
            array[5].ShouldEqual(300);
        }

        [Fact]
        private void CopyToWithNegativeValues()
        {
            var v    = new V3(-10, -20, -30);
            var list = new List<double> { 0, 0, 0 };

            v.CopyTo(list);

            list[0].ShouldEqual(-10);
            list[1].ShouldEqual(-20);
            list[2].ShouldEqual(-30);
        }

        [Fact]
        private void CopyToWithSpecialValues()
        {
            var      v     = new V3(double.NaN, double.PositiveInfinity, double.NegativeInfinity);
            double[] array = new double[3];

            v.CopyTo(array);

            array[0].ShouldBeNaN();
            array[1].ShouldBePositiveInfinity();
            array[2].ShouldBeNegativeInfinity();
        }

        [Fact]
        private void CopyTo2DArrayBasic()
        {
            var       v     = new V3(5, 6, 7);
            double[,] array = new double[4, 4];

            v.CopyTo(array, 0, 0);

            array[0, 0].ShouldEqual(5);
            array[1, 0].ShouldEqual(6);
            array[2, 0].ShouldEqual(7);
            array[3, 0].ShouldEqual(0);
            array[0, 1].ShouldEqual(0);
        }

        [Fact]
        private void CopyTo2DArrayAtOffset()
        {
            var       v     = new V3(10, 20, 30);
            double[,] array = new double[5, 5];

            v.CopyTo(array, 1, 2);

            array[0, 2].ShouldEqual(0);
            array[1, 2].ShouldEqual(10);
            array[2, 2].ShouldEqual(20);
            array[3, 2].ShouldEqual(30);
            array[4, 2].ShouldEqual(0);
            array[1, 1].ShouldEqual(0);
            array[1, 3].ShouldEqual(0);
        }

        [Fact]
        private void CopyTo2DArrayWithNegativeValues()
        {
            var       v     = new V3(-1.5, -2.5, -3.5);
            double[,] array = new double[3, 3];

            v.CopyTo(array, 0, 1);

            array[0, 1].ShouldEqual(-1.5);
            array[1, 1].ShouldEqual(-2.5);
            array[2, 1].ShouldEqual(-3.5);
        }

        [Fact]
        private void CopyTo2DArrayMultipleVectors()
        {
            var       v1    = new V3(1, 2, 3);
            var       v2    = new V3(4, 5, 6);
            var       v3    = new V3(7, 8, 9);
            double[,] array = new double[3, 3];

            v1.CopyTo(array, 0, 0);
            v2.CopyTo(array, 0, 1);
            v3.CopyTo(array, 0, 2);

            array[0, 0].ShouldEqual(1);
            array[1, 0].ShouldEqual(2);
            array[2, 0].ShouldEqual(3);
            array[0, 1].ShouldEqual(4);
            array[1, 1].ShouldEqual(5);
            array[2, 1].ShouldEqual(6);
            array[0, 2].ShouldEqual(7);
            array[1, 2].ShouldEqual(8);
            array[2, 2].ShouldEqual(9);
        }

        [Fact]
        private void CopyFromToRoundTrip()
        {
            var original = new V3(1.234, 5.678, 9.012);
            var list = new List<double>
            {
                0,
                0,
                0,
                0,
                0
            };
            var restored = new V3(0, 0, 0);

            original.CopyTo(list, 1);
            restored.CopyFrom(list, 1);

            restored.ShouldEqual(original);
        }

        [Fact]
        private void CopyFromToWithLargeValues()
        {
            var      v     = new V3(1e100, -1e100, 1e-100);
            double[] array = new double[3];
            var      v2    = new V3(0, 0, 0);

            v.CopyTo(array);
            v2.CopyFrom(array);

            v2.x.ShouldEqual(1e100);
            v2.y.ShouldEqual(-1e100);
            v2.z.ShouldEqual(1e-100);
        }

        [Fact]
        private void CopyFromIndicesSequential()
        {
            double[] array = { 1, 2, 3, 4, 5 };
            var      v     = V3.CopyFromIndices(array, (0, 1, 2));

            v.ShouldEqual(new V3(1, 2, 3));
        }

        [Fact]
        private void CopyFromIndicesNonSequential()
        {
            double[] array = { 10, 20, 30, 40, 50, 60 };
            var      v     = V3.CopyFromIndices(array, (1, 3, 5));

            v.ShouldEqual(new V3(20, 40, 60));
        }

        [Fact]
        private void CopyFromIndicesReversed()
        {
            double[] array = { 1, 2, 3, 4, 5 };
            var      v     = V3.CopyFromIndices(array, (4, 2, 0));

            v.ShouldEqual(new V3(5, 3, 1));
        }

        [Fact]
        private void CopyFromIndicesDuplicateIndices()
        {
            double[] array = { 7, 8, 9 };
            var      v     = V3.CopyFromIndices(array, (1, 1, 1));

            v.ShouldEqual(new V3(8, 8, 8));
        }

        [Fact]
        private void CopyFromIndicesWithList()
        {
            var list = new List<double> { 100, 200, 300, 400 };
            var v    = V3.CopyFromIndices(list, (3, 1, 0));

            v.ShouldEqual(new V3(400, 200, 100));
        }

        [Fact]
        private void CopyFromIndicesWithSpecialValues()
        {
            double[] array = { double.NaN, double.PositiveInfinity, double.NegativeInfinity, 0 };
            var      v     = V3.CopyFromIndices(array, (0, 1, 2));

            v.x.ShouldBeNaN();
            v.y.ShouldBePositiveInfinity();
            v.z.ShouldBeNegativeInfinity();
        }

        [Fact]
        private void CopyToIndicesSequential()
        {
            var      v     = new V3(10, 20, 30);
            double[] array = new double[5];

            v.CopyToIndices(array, (0, 1, 2));

            array[0].ShouldEqual(10);
            array[1].ShouldEqual(20);
            array[2].ShouldEqual(30);
            array[3].ShouldEqual(0);
            array[4].ShouldEqual(0);
        }

        [Fact]
        private void CopyToIndicesNonSequential()
        {
            var      v     = new V3(5, 6, 7);
            double[] array = new double[6];

            v.CopyToIndices(array, (1, 3, 5));

            array[0].ShouldEqual(0);
            array[1].ShouldEqual(5);
            array[2].ShouldEqual(0);
            array[3].ShouldEqual(6);
            array[4].ShouldEqual(0);
            array[5].ShouldEqual(7);
        }

        [Fact]
        private void CopyToIndicesReversed()
        {
            var      v     = new V3(1, 2, 3);
            double[] array = new double[5];

            v.CopyToIndices(array, (4, 2, 0));

            array[0].ShouldEqual(3);
            array[1].ShouldEqual(0);
            array[2].ShouldEqual(2);
            array[3].ShouldEqual(0);
            array[4].ShouldEqual(1);
        }

        [Fact]
        private void CopyToIndicesDuplicateIndicesLastWins()
        {
            var      v     = new V3(1, 2, 3);
            double[] array = new double[3];

            v.CopyToIndices(array, (0, 0, 0));

            array[0].ShouldEqual(3);
            array[1].ShouldEqual(0);
            array[2].ShouldEqual(0);
        }

        [Fact]
        private void CopyToIndicesWithList()
        {
            var v = new V3(100, 200, 300);
            var list = new List<double>
            {
                0,
                0,
                0,
                0,
                0
            };

            v.CopyToIndices(list, (4, 2, 0));

            list[0].ShouldEqual(300);
            list[1].ShouldEqual(0);
            list[2].ShouldEqual(200);
            list[3].ShouldEqual(0);
            list[4].ShouldEqual(100);
        }

        [Fact]
        private void CopyToIndicesOverwritesExistingValues()
        {
            var      v     = new V3(7, 8, 9);
            double[] array = { 1, 2, 3, 4, 5 };

            v.CopyToIndices(array, (0, 2, 4));

            array[0].ShouldEqual(7);
            array[1].ShouldEqual(2);
            array[2].ShouldEqual(8);
            array[3].ShouldEqual(4);
            array[4].ShouldEqual(9);
        }

        [Fact]
        private void CopyToIndicesWithSpecialValues()
        {
            var      v     = new V3(double.NaN, double.PositiveInfinity, double.NegativeInfinity);
            double[] array = new double[3];

            v.CopyToIndices(array, (0, 1, 2));

            array[0].ShouldBeNaN();
            array[1].ShouldBePositiveInfinity();
            array[2].ShouldBeNegativeInfinity();
        }

        [Fact]
        private void CopyFromToIndicesRoundTrip()
        {
            var      original = new V3(1.5, 2.5, 3.5);
            double[] array    = new double[10];
            var      indices  = (2, 5, 8);

            original.CopyToIndices(array, indices);
            var restored = V3.CopyFromIndices(array, indices);

            restored.ShouldEqual(original);
        }

        [Fact]
        private void CopyFromToIndicesScatteredRoundTrip()
        {
            var      original = new V3(-7.7, 8.8, -9.9);
            double[] array    = new double[6];
            var      indices  = (5, 0, 3);

            original.CopyToIndices(array, indices);
            var restored = V3.CopyFromIndices(array, indices);

            restored.ShouldEqual(original);
        }
    }
}
