/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.M4Tests
{
    public class VectorTests
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

        private static readonly V4 _v = new V4(0.5, -1, 2, 3);

        [Fact]
        private void ColumnConstructorTest()
        {
            var m = new M4(new V4(1, 5, 9, 13), new V4(2, 6, 10, 14), new V4(3, 7, 11, 15), new V4(4, 8, 12, 16));

            m.ShouldEqual(_a);
        }

        [Fact]
        private void GetColumnAndRowTest()
        {
            _a.GetColumn(0).ShouldEqual(new V4(1, 5, 9, 13));
            _a.GetColumn(3).ShouldEqual(new V4(4, 8, 12, 16));
            _a.GetRow(0).ShouldEqual(new V4(1, 2, 3, 4));
            _a.GetRow(2).ShouldEqual(new V4(9, 10, 11, 12));

            new M4(_a.GetColumn(0), _a.GetColumn(1), _a.GetColumn(2), _a.GetColumn(3)).ShouldEqual(_a);
            new M4(_a.GetRow(0), _a.GetRow(1), _a.GetRow(2), _a.GetRow(3)).ShouldEqual(_a.transpose);
        }

        [Fact]
        private void GetColumnAndRowThrowTest()
        {
            Assert.Throws<IndexOutOfRangeException>(() => _a.GetColumn(4));
            Assert.Throws<IndexOutOfRangeException>(() => _a.GetColumn(-1));
            Assert.Throws<IndexOutOfRangeException>(() => _a.GetRow(4));
            Assert.Throws<IndexOutOfRangeException>(() => _a.GetRow(-1));
        }

        [Fact]
        private void WithColumnTest()
        {
            for (int c = 0; c < 4; c++)
            {
                M4 m = _a.WithColumn(c, _v);

                for (int j = 0; j < 4; j++)
                    m.GetColumn(j).ShouldEqual(j == c ? _v : _a.GetColumn(j));
            }

            Assert.Throws<IndexOutOfRangeException>(() => _a.WithColumn(4, _v));
        }

        [Fact]
        private void WithRowTest()
        {
            for (int r = 0; r < 4; r++)
            {
                M4 m = _a.WithRow(r, _v);

                for (int i = 0; i < 4; i++)
                    m.GetRow(i).ShouldEqual(i == r ? _v : _a.GetRow(i));
            }

            Assert.Throws<IndexOutOfRangeException>(() => _a.WithRow(4, _v));
        }

        [Fact]
        private void SwappedRowsAndColumnsTest()
        {
            M4 rows = _a.WithSwappedRows(0, 3);

            rows.GetRow(0).ShouldEqual(_a.GetRow(3));
            rows.GetRow(3).ShouldEqual(_a.GetRow(0));
            rows.GetRow(1).ShouldEqual(_a.GetRow(1));
            rows.GetRow(2).ShouldEqual(_a.GetRow(2));

            M4 cols = _a.WithSwappedColumns(1, 2);

            cols.GetColumn(1).ShouldEqual(_a.GetColumn(2));
            cols.GetColumn(2).ShouldEqual(_a.GetColumn(1));
            cols.GetColumn(0).ShouldEqual(_a.GetColumn(0));
            cols.GetColumn(3).ShouldEqual(_a.GetColumn(3));

            _a.WithSwappedRows(2, 2).ShouldEqual(_a);
            _a.WithSwappedColumns(1, 1).ShouldEqual(_a);
        }

        [Fact]
        private void DiagonalAccessTest()
        {
            _a.diagonal.ShouldEqual(new V4(1, 6, 11, 16));
            _a.WithDiagonal(_a.diagonal).ShouldEqual(_a);

            M4 m = _a.WithDiagonal(_v);

            m.diagonal.ShouldEqual(_v);
            m.ShouldEqual(_a.WithDiagonal(0.5, -1, 2, 3));
            (m - _a).ShouldEqual(M4.Diagonal(_v - _a.diagonal));
        }

        [Fact]
        private void DiagonalFactoryTest()
        {
            M4.Diagonal(_v).ShouldEqual(M4.Diagonal(0.5, -1, 2, 3));
            (M4.Diagonal(_v) * _b.GetColumn(0)).ShouldEqual(V4.Scale(_v, _b.GetColumn(0)));
        }

        [Fact]
        private void MatrixTimesVectorTest()
        {
            (_a * _v).ShouldEqual(new V4(16.5, 34.5, 52.5, 70.5));
            (M4.identity * _v).ShouldEqual(_v);
            _a.MultiplyVector(_v).ShouldEqual(_a * _v);
        }

        [Fact]
        private void MatrixTimesBasisVectorTest()
        {
            (_a * V4.xaxis).ShouldEqual(_a.GetColumn(0));
            (_a * V4.yaxis).ShouldEqual(_a.GetColumn(1));
            (_a * V4.zaxis).ShouldEqual(_a.GetColumn(2));
            (_a * V4.waxis).ShouldEqual(_a.GetColumn(3));
        }

        [Fact]
        private void VectorTimesMatrixTest()
        {
            (_v * _a).ShouldEqual(new V4(52.5, 57, 61.5, 66));
            (_v * _a).ShouldEqual(_a.transpose * _v);
            (V4.zaxis * _a).ShouldEqual(_a.GetRow(2));
        }

        [Fact]
        private void MatchesQuaternionOperatorsTest()
        {
            var q = new Q3(_v.x, _v.y, _v.z, _v.w);
            Q3 mq = _a * q;
            Q3 qm = q * _a;

            (_a * _v).ShouldEqual(new V4(mq.x, mq.y, mq.z, mq.w));
            (_v * _a).ShouldEqual(new V4(qm.x, qm.y, qm.z, qm.w));
        }

        [Fact]
        private void QuadraticFormTest()
        {
            _a.QuadraticForm(_v).ShouldEqual(290.25);
            _a.QuadraticForm(_v).ShouldEqual(_a.QuadraticForm(new Q3(_v.x, _v.y, _v.z, _v.w)));
            M4.identity.QuadraticForm(_v).ShouldEqual(_v.sqrMagnitude);
        }

        [Fact]
        private void MatrixProductAssociativityTest()
        {
            (_a * _b * _v).ShouldEqual(_a * (_b * _v));
            (_v * _a * _b).ShouldEqual(_v * (_a * _b));
        }
    }
}
