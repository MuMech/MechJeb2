/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.M4Tests
{
    public class CoreOperationsTests
    {
        private static readonly M4 _a = new M4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        private static readonly M4 _b = new M4(
            2, 0, 1, 3,
            1, 4, 0, 2,
            0, 1, 3, 1,
            5, 2, 1, 0);

        [Fact]
        private void IndividualAccessTest()
        {
            _a.m00.ShouldEqual(1);
            _a.m01.ShouldEqual(2);
            _a.m02.ShouldEqual(3);
            _a.m03.ShouldEqual(4);
            _a.m10.ShouldEqual(5);
            _a.m11.ShouldEqual(6);
            _a.m12.ShouldEqual(7);
            _a.m13.ShouldEqual(8);
            _a.m20.ShouldEqual(9);
            _a.m21.ShouldEqual(10);
            _a.m22.ShouldEqual(11);
            _a.m23.ShouldEqual(12);
            _a.m30.ShouldEqual(13);
            _a.m31.ShouldEqual(14);
            _a.m32.ShouldEqual(15);
            _a.m33.ShouldEqual(16);
        }

        [Fact]
        private void TwoDimensionalAccessTest()
        {
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                _a[i, j].ShouldEqual(1 + 4 * i + j);
        }

        [Fact]
        private void OneDimensionalAccessTest()
        {
            // column-major
            _a[0].ShouldEqual(1);
            _a[1].ShouldEqual(5);
            _a[2].ShouldEqual(9);
            _a[3].ShouldEqual(13);
            _a[4].ShouldEqual(2);
            _a[7].ShouldEqual(14);
            _a[12].ShouldEqual(4);
            _a[15].ShouldEqual(16);

            for (int i = 0; i < 16; i++)
                _a[i].ShouldEqual(_a[i % 4, i / 4]);
        }

        [Fact]
        private void IndexOutOfRangeTest()
        {
            Assert.Throws<IndexOutOfRangeException>(() => _a[-1]);
            Assert.Throws<IndexOutOfRangeException>(() => _a[16]);
            Assert.Throws<IndexOutOfRangeException>(() => _a[0, 4]);
        }

        [Fact]
        private void ZeroTest()
        {
            for (int i = 0; i < 16; i++)
                M4.zero[i].ShouldEqual(0);
        }

        [Fact]
        private void IdentityTest()
        {
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                M4.identity[i, j].ShouldEqual(i == j ? 1 : 0);

            Assert.True(M4.identity.isIdentity);
            Assert.False(_a.isIdentity);
            Assert.False(M4.zero.isIdentity);
        }

        [Fact]
        private void MatrixMultiplicationTest()
        {
            var expected = new M4(
                24, 19, 14, 10,
                56, 47, 34, 34,
                88, 75, 54, 58,
                120, 103, 74, 82);

            Assert.True(_a * _b == expected);
        }

        [Fact]
        private void MatrixMultiplicationIdentityTest()
        {
            Assert.True(M4.identity * _a == _a);
            Assert.True(_a * M4.identity == _a);
        }

        [Fact]
        private void MatrixMultiplicationNonCommutativeTest()
        {
            Assert.True(_a * _b != _b * _a);
        }

        [Fact]
        private void MatrixMultiplicationAssociativeTest()
        {
            M4 c = _a - 2 * _b;
            (_a * _b * c).ShouldEqual(_a * (_b * c));
        }

        [Fact]
        private void ScalarMultiplicationTest()
        {
            M4 left = 2.5 * _a;
            M4 right = _a * 2.5;

            for (int i = 0; i < 16; i++)
            {
                left[i].ShouldEqual(_a[i] * 2.5);
                right[i].ShouldEqual(_a[i] * 2.5);
            }
        }

        [Fact]
        private void ScalarDivisionTest()
        {
            M4 m = _a / 4;

            for (int i = 0; i < 16; i++)
                m[i].ShouldEqual(_a[i] / 4);
        }

        [Fact]
        private void AdditionTest()
        {
            M4 m = _a + _b;

            for (int i = 0; i < 16; i++)
                m[i].ShouldEqual(_a[i] + _b[i]);
        }

        [Fact]
        private void SubtractionTest()
        {
            M4 m = _a - _b;

            for (int i = 0; i < 16; i++)
                m[i].ShouldEqual(_a[i] - _b[i]);
        }

        [Fact]
        private void NegationTest()
        {
            M4 m = -_a;

            for (int i = 0; i < 16; i++)
                m[i].ShouldEqual(-_a[i]);

            Assert.True(_a + -_a == M4.zero);
        }

        [Fact]
        private void TransposeTest()
        {
            M4 t = _a.transpose;

            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                t[i, j].ShouldEqual(_a[j, i]);

            Assert.True(_a.T() == t);
            Assert.True(t.transpose == _a);
        }

        [Fact]
        private void TransposeOfProductTest()
        {
            Assert.True((_a * _b).transpose == _b.transpose * _a.transpose);
        }

        [Fact]
        private void TraceTest()
        {
            _a.trace.ShouldEqual(34);
            M4.identity.trace.ShouldEqual(4);
            M4.zero.trace.ShouldEqual(0);
        }

        [Fact]
        private void SymmetricTest()
        {
            Assert.True((_a + _a.transpose).isSymmetric);
            Assert.True(M4.identity.isSymmetric);
            Assert.False(_a.isSymmetric);
        }

        [Fact]
        private void MaxMagnitudeTest()
        {
            _a.max_magnitude.ShouldEqual(16);
            (-_a).max_magnitude.ShouldEqual(16);
            M4.zero.max_magnitude.ShouldEqual(0);
        }

        [Fact]
        private void DiagonalScalarTest()
        {
            Assert.True(M4.Diagonal(2) == new M4(2, 0, 0, 0, 0, 2, 0, 0, 0, 0, 2, 0, 0, 0, 0, 2));
            Assert.True(M4.Diagonal(1) == M4.identity);
        }

        [Fact]
        private void DiagonalComponentsTest()
        {
            M4 m = M4.Diagonal(1, 2, 3, 4);

            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                m[i, j].ShouldEqual(i == j ? i + 1 : 0);
        }

        [Fact]
        private void CopyTo2DTest()
        {
            double[,] array = new double[6, 7];
            _a.CopyTo(array, 1, 2);

            array[1, 2].ShouldEqual(1);
            array[1, 5].ShouldEqual(4);
            array[2, 2].ShouldEqual(5);
            array[4, 2].ShouldEqual(13);
            array[4, 5].ShouldEqual(16);
            array[0, 0].ShouldEqual(0);
            array[1, 6].ShouldEqual(0);
            array[5, 2].ShouldEqual(0);

            Assert.True(M4.CopyFrom(array, 1, 2) == _a);
        }

        [Fact]
        private void CopyTo1DTest()
        {
            double[] array = new double[20];
            _a.CopyTo(array, 3);

            array[2].ShouldEqual(0);
            array[3].ShouldEqual(1);
            array[4].ShouldEqual(2);
            array[7].ShouldEqual(5);
            array[18].ShouldEqual(16);
            array[19].ShouldEqual(0);

            Assert.True(M4.CopyFrom(array, 3) == _a);
        }

        [Fact]
        private void CopyFromListTest()
        {
            var list = new List<double>();
            for (int i = 1; i <= 16; i++)
                list.Add(i);

            Assert.True(M4.CopyFrom(list, 0) == _a);
        }

        [Fact]
        private void ToStringTest1()
        {
            Assert.Equal(
                "[1, 2, 3, 4\n5, 6, 7, 8\n9, 10, 11, 12\n13, 14, 15, 16]\n",
                _a.ToString()
            );
        }

        [Fact]
        private void ToStringTest2()
        {
            Assert.Equal(
                "[0.33, 0.67, 1.00, 1.33\n1.67, 2.00, 2.33, 2.67\n3.00, 3.33, 3.67, 4.00\n4.33, 4.67, 5.00, 5.33]\n",
                (_a / 3).ToString("F2")
            );
        }
    }
}
