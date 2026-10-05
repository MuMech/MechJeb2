/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.M4Tests
{
    public class QuaternionTests
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

        private static readonly Q3 _q = new Q3(0.5, -1, 2, 3);

        [Fact]
        private void MatrixTimesQuaternionTest()
        {
            (_a * _q).ShouldEqual(new Q3(16.5, 34.5, 52.5, 70.5));
        }

        [Fact]
        private void MatrixTimesBasisQuaternionTest()
        {
            // rows and columns are indexed (x, y, z, w) so the basis quaternions pick out columns
            (_a * Q3.xaxis).ShouldEqual(new Q3(1, 5, 9, 13));
            (_a * Q3.yaxis).ShouldEqual(new Q3(2, 6, 10, 14));
            (_a * Q3.zaxis).ShouldEqual(new Q3(3, 7, 11, 15));
            (_a * Q3.waxis).ShouldEqual(new Q3(4, 8, 12, 16));
        }

        [Fact]
        private void MatrixTimesQuaternionMatchesIndexerTest()
        {
            Q3 r = _a * _q;

            for (int i = 0; i < 4; i++)
            {
                double sum = 0;
                for (int j = 0; j < 4; j++)
                    sum += _a[i, j] * _q[j];
                r[i].ShouldEqual(sum);
            }
        }

        [Fact]
        private void IdentityTimesQuaternionTest()
        {
            Assert.True(M4.identity * _q == _q);
            Assert.True(M4.zero * _q == Q3.zero);
        }

        [Fact]
        private void QuaternionTimesMatrixTest()
        {
            (_q * _a).ShouldEqual(new Q3(52.5, 57, 61.5, 66));
            (_q * _a).ShouldEqual(_a.transpose * _q);
            (Q3.xaxis * _a).ShouldEqual(new Q3(1, 2, 3, 4));
        }

        [Fact]
        private void MatrixProductTimesQuaternionTest()
        {
            (_a * _b * _q).ShouldEqual(_a * (_b * _q));
            (_q * (_a * _b)).ShouldEqual(_q * _a * _b);
        }

        [Fact]
        private void LinearityTest()
        {
            ((_a + _b) * _q).ShouldEqual(_a * _q + _b * _q);
            (2.5 * _a * _q).ShouldEqual(2.5 * (_a * _q));
            (_a * (_q * 2.5)).ShouldEqual(2.5 * (_a * _q));
        }

        [Fact]
        private void QuadraticFormTest()
        {
            _a.QuadraticForm(_q).ShouldEqual(290.25);
            Q3.Dot(_q, _a * _q).ShouldEqual(290.25);
            Q3.Dot(_q * _a, _q).ShouldEqual(290.25);
        }

        [Fact]
        private void QuadraticFormMatchesExplicitSumTest()
        {
            M4 m = _a - 3 * _b;

            double sum = 0;
            for (int i = 0; i < 4; i++)
            for (int j = 0; j < 4; j++)
                sum += _q[i] * m[i, j] * _q[j];

            m.QuadraticForm(_q).ShouldEqual(sum);
        }

        [Fact]
        private void QuadraticFormIdentityTest()
        {
            M4.identity.QuadraticForm(_q).ShouldEqual(Q3.Dot(_q, _q));
            M4.identity.QuadraticForm(_q.normalized).ShouldEqual(1, 1e-14);
        }

        [Fact]
        private void QuadraticFormDiagonalTest()
        {
            // the squared magnitude of the vector part
            M4.Diagonal(1, 1, 1, 0).QuadraticForm(_q).ShouldEqual(_q.x * _q.x + _q.y * _q.y + _q.z * _q.z);
            M4.Diagonal(0, 0, 0, 1).QuadraticForm(_q).ShouldEqual(_q.w * _q.w);
        }

        [Fact]
        private void QuadraticFormAntisymmetricTest()
        {
            M4 s = _a - _a.transpose;
            s.QuadraticForm(_q).ShouldBeZero();
        }

        [Fact]
        private void QuadraticFormSymmetricPartTest()
        {
            // only the symmetric part of the matrix contributes to the quadratic form
            M4 sym = (_a + _a.transpose) / 2;
            sym.QuadraticForm(_q).ShouldEqual(_a.QuadraticForm(_q));
        }

        [Fact]
        private void MatrixTimesDualQuaternionTest()
        {
            var dq = new Q3(1, 2, -1, 0.5);
            DualQ3 r = _a * new DualQ3(_q, dq);

            r.M.ShouldEqual(_a * _q);
            r.D.ShouldEqual(_a * dq);
        }

        [Fact]
        private void DualQuadraticFormGradientTest()
        {
            Q3[] basis = { Q3.xaxis, Q3.yaxis, Q3.zaxis, Q3.waxis };
            var expected = new Q3(69, 91.5, 114, 136.5);
            Q3 gradient = (_a + _a.transpose) * _q;
            gradient.ShouldEqual(expected);

            for (int i = 0; i < 4; i++)
            {
                Dual f = _a.QuadraticForm(new DualQ3(_q, basis[i]));
                f.M.ShouldEqual(290.25);
                f.D.ShouldEqual(expected[i]);
            }
        }

        [Fact]
        private void DualQuadraticFormDirectionalDerivativeTest()
        {
            var dq = new Q3(1, 2, -1, 0.5);
            Dual f = _a.QuadraticForm(new DualQ3(_q, dq));

            f.M.ShouldEqual(_a.QuadraticForm(_q));
            f.D.ShouldEqual(Q3.Dot(dq, (_a + _a.transpose) * _q));
        }

        [Fact]
        private void DualQuadraticFormConstantTest()
        {
            Dual f = _a.QuadraticForm(new DualQ3(_q));

            f.M.ShouldEqual(290.25);
            f.D.ShouldEqual(0);
        }
    }
}
