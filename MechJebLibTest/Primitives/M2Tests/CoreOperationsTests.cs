/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.M2Tests
{
    public class CoreOperationsTests
    {
        private static readonly M2 _a = new M2(
            1, 2,
            3, 4);

        private static readonly M2 _b = new M2(
            2, 0,
            1, 3);

        private static readonly V2 _v = new V2(0.5, -1);

        [Fact]
        private void IndividualAccessTest()
        {
            _a.m00.ShouldEqual(1);
            _a.m01.ShouldEqual(2);
            _a.m10.ShouldEqual(3);
            _a.m11.ShouldEqual(4);
        }

        [Fact]
        private void TwoDimensionalAccessTest()
        {
            _a[0, 0].ShouldEqual(1);
            _a[0, 1].ShouldEqual(2);
            _a[1, 0].ShouldEqual(3);
            _a[1, 1].ShouldEqual(4);
        }

        [Fact]
        private void OneDimensionalAccessTest()
        {
            // column-major
            _a[0].ShouldEqual(1);
            _a[1].ShouldEqual(3);
            _a[2].ShouldEqual(2);
            _a[3].ShouldEqual(4);
        }

        [Fact]
        private void IndexOutOfRangeTest()
        {
            Assert.Throws<IndexOutOfRangeException>(() => _a[4]);
            Assert.Throws<IndexOutOfRangeException>(() => _a[-1]);
        }

        [Fact]
        private void ZeroAndIdentityTest()
        {
            M2.zero.ShouldEqual(new M2(0, 0, 0, 0));
            M2.identity.ShouldEqual(new M2(1, 0, 0, 1));
        }

        [Fact]
        private void ColumnConstructorTest()
        {
            new M2(new V2(1, 3), new V2(2, 4)).ShouldEqual(_a);
        }

        [Fact]
        private void GetColumnAndRowTest()
        {
            _a.GetColumn(0).ShouldEqual(new V2(1, 3));
            _a.GetColumn(1).ShouldEqual(new V2(2, 4));
            _a.GetRow(0).ShouldEqual(new V2(1, 2));
            _a.GetRow(1).ShouldEqual(new V2(3, 4));

            new M2(_a.GetRow(0), _a.GetRow(1)).ShouldEqual(_a.transpose);

            Assert.Throws<IndexOutOfRangeException>(() => _a.GetColumn(2));
            Assert.Throws<IndexOutOfRangeException>(() => _a.GetRow(-1));
        }

        [Fact]
        private void WithColumnAndRowTest()
        {
            _a.WithColumn(0, _v).ShouldEqual(new M2(0.5, 2, -1, 4));
            _a.WithColumn(1, _v).ShouldEqual(new M2(1, 0.5, 3, -1));
            _a.WithRow(0, _v).ShouldEqual(new M2(0.5, -1, 3, 4));
            _a.WithRow(1, _v).ShouldEqual(new M2(1, 2, 0.5, -1));

            Assert.Throws<IndexOutOfRangeException>(() => _a.WithColumn(2, _v));
            Assert.Throws<IndexOutOfRangeException>(() => _a.WithRow(2, _v));
        }

        [Fact]
        private void SwappedRowsAndColumnsTest()
        {
            _a.WithSwappedRows(0, 1).ShouldEqual(new M2(3, 4, 1, 2));
            _a.WithSwappedColumns(0, 1).ShouldEqual(new M2(2, 1, 4, 3));
            _a.WithSwappedRows(1, 1).ShouldEqual(_a);
            _a.WithSwappedColumns(0, 0).ShouldEqual(_a);
            _a.WithSwappedRows(0, 1).WithSwappedRows(0, 1).ShouldEqual(_a);
        }

        [Fact]
        private void DiagonalAccessTest()
        {
            _a.diagonal.ShouldEqual(new V2(1, 4));
            _a.WithDiagonal(new V2(9, 8)).ShouldEqual(new M2(9, 2, 3, 8));
            _a.WithDiagonal(9, 8).ShouldEqual(new M2(9, 2, 3, 8));
            _a.WithDiagonal(_a.diagonal).ShouldEqual(_a);
        }

        [Fact]
        private void MatrixMultiplicationTest()
        {
            (_a * _b).ShouldEqual(new M2(4, 6, 10, 12));
            (_b * _a).ShouldEqual(new M2(2, 4, 10, 14));
            (_a * M2.identity).ShouldEqual(_a);
            (M2.identity * _a).ShouldEqual(_a);
        }

        [Fact]
        private void MatrixMultiplicationAssociativeTest()
        {
            var c = new M2(-1, 2, 0.5, 3);

            (_a * _b * c).ShouldEqual(_a * (_b * c));
        }

        [Fact]
        private void ScalarMultiplicationAndDivisionTest()
        {
            (_a * 2).ShouldEqual(new M2(2, 4, 6, 8));
            (2 * _a).ShouldEqual(new M2(2, 4, 6, 8));
            (_a / 2).ShouldEqual(new M2(0.5, 1, 1.5, 2));
        }

        [Fact]
        private void AdditionSubtractionNegationTest()
        {
            (_a + _b).ShouldEqual(new M2(3, 2, 4, 7));
            (_a - _b).ShouldEqual(new M2(-1, 2, 2, 1));
            (-_a).ShouldEqual(new M2(-1, -2, -3, -4));
            (_a - _a).ShouldEqual(M2.zero);
            (_a + -_a).ShouldEqual(M2.zero);
        }

        [Fact]
        private void MatrixTimesVectorTest()
        {
            (_a * _v).ShouldEqual(new V2(-1.5, -2.5));
            _a.MultiplyVector(_v).ShouldEqual(_a * _v);
            (M2.identity * _v).ShouldEqual(_v);
            (_a * V2.xaxis).ShouldEqual(_a.GetColumn(0));
            (_a * V2.yaxis).ShouldEqual(_a.GetColumn(1));
            (_a * _b * _v).ShouldEqual(_a * (_b * _v));
        }

        [Fact]
        private void TransposeTest()
        {
            _a.transpose.ShouldEqual(new M2(1, 3, 2, 4));
            _a.T().ShouldEqual(_a.transpose);
            M2.Transpose(_a).ShouldEqual(_a.transpose);
            _a.transpose.transpose.ShouldEqual(_a);
            (_a * _b).transpose.ShouldEqual(_b.transpose * _a.transpose);
        }

        [Fact]
        private void TraceTest()
        {
            _a.trace.ShouldEqual(5);
            M2.Trace(_a).ShouldEqual(5);
            M2.identity.trace.ShouldEqual(2);
        }

        [Fact]
        private void IsIdentityTest()
        {
            Assert.True(M2.identity.isIdentity);
            Assert.True(M2.Diagonal(1).isIdentity);
            Assert.False(_a.isIdentity);
            Assert.False(M2.zero.isIdentity);
        }

        [Fact]
        private void SymmetricTest()
        {
            Assert.False(_a.isSymmetric);
            Assert.True((_a + _a.transpose).isSymmetric);
            Assert.True(M2.identity.isSymmetric);
        }

        [Fact]
        private void MaxMinMagnitudeTest()
        {
            _a.max_magnitude.ShouldEqual(4);
            _a.min_magnitude.ShouldEqual(1);
            (-_a).max_magnitude.ShouldEqual(4);
            (-_a).min_magnitude.ShouldEqual(1);
            _b.min_magnitude.ShouldEqual(0);
            M2.zero.max_magnitude.ShouldEqual(0);
            M2.identity.min_magnitude.ShouldEqual(0);
        }

        [Fact]
        private void DiagonalFactoryTest()
        {
            M2.Diagonal(2).ShouldEqual(new M2(2, 0, 0, 2));
            M2.Diagonal(new V2(3, 4)).ShouldEqual(new M2(3, 0, 0, 4));
            M2.Diagonal(3, 4).ShouldEqual(new M2(3, 0, 0, 4));
            (M2.Diagonal(3, 4) * _v).ShouldEqual(V2.Scale(new V2(3, 4), _v));
        }

        [Fact]
        private void LerpTest()
        {
            M2.Lerp(_a, _b, 0).ShouldEqual(_a);
            M2.Lerp(_a, _b, 1).ShouldEqual(_b);
            M2.Lerp(_a, _b, 0.5).ShouldEqual((_a + _b) * 0.5);
            M2.Lerp(_a, _b, 0.25).ShouldEqual(_a * 0.75 + _b * 0.25);
        }

        [Fact]
        private void LerpClampsTest()
        {
            M2.Lerp(_a, _b, -1).ShouldEqual(_a);
            M2.Lerp(_a, _b, 2).ShouldEqual(_b);
        }

        [Fact]
        private void CopyTo2DTest()
        {
            double[,] array = new double[3, 4];

            _a.CopyTo(array, 1, 2);

            array[1, 2].ShouldEqual(1);
            array[1, 3].ShouldEqual(2);
            array[2, 2].ShouldEqual(3);
            array[2, 3].ShouldEqual(4);
            array[0, 2].ShouldEqual(0);
            array[1, 1].ShouldEqual(0);

            M2.CopyFrom(array, 1, 2).ShouldEqual(_a);
        }

        [Fact]
        private void CopyTo1DTest()
        {
            var list = new List<double> { 0, 0, 0, 0, 0, 0 };

            _a.CopyTo(list, 1);

            list[0].ShouldEqual(0);
            list[1].ShouldEqual(1);
            list[2].ShouldEqual(2);
            list[3].ShouldEqual(3);
            list[4].ShouldEqual(4);
            list[5].ShouldEqual(0);

            M2.CopyFrom(list, 1).ShouldEqual(_a);
        }

        [Fact]
        private void ToStringTest1()
        {
            Assert.Equal("[1, 2\n3, 4]\n", _a.ToString());
        }

        [Fact]
        private void ToStringTest2()
        {
            Assert.Equal("[0.33, 0.67\n1.00, 1.33]\n", (_a / 3).ToString("F2"));
        }
    }
}
