/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.M4Tests
{
    public class MatrixAnalysisTests
    {
        // rank 2
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

        private static readonly M4 _c = new M4(
            1, 2, 0, 1,
            0, 1, 3, 0,
            2, 0, 1, 1,
            1, 1, 0, 2);

        // left multiplication by the unit quaternion (1/2, 1/2, 1/2, 1/2), a 4D rotation
        private static readonly M4 _rotation = new M4(
            0.5, -0.5, 0.5, 0.5,
            0.5, 0.5, -0.5, 0.5,
            -0.5, 0.5, 0.5, 0.5,
            -0.5, -0.5, -0.5, 0.5);

        // cyclic shift, an odd permutation
        private static readonly M4 _permutation = new M4(
            0, 1, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1,
            1, 0, 0, 0);

        // left multiplication by the pure quaternion (1, 2, 3, 0)
        private static readonly M4 _skew = new M4(
            0, -3, 2, 1,
            3, 0, -1, 2,
            -2, 1, 0, 3,
            -1, -2, -3, 0);

        [Fact]
        private void TraceStaticMethod()
        {
            M4.Trace(_a).ShouldEqual(34);
            M4.Trace(_a).ShouldEqual(_a.trace);
        }

        [Fact]
        private void DeterminantBasicMatrices()
        {
            M4.identity.determinant.ShouldEqual(1);
            M4.zero.determinant.ShouldEqual(0);
            M4.Diagonal(2, 3, 4, 5).determinant.ShouldEqual(120);
        }

        [Fact]
        private void DeterminantKnownValue()
        {
            _b.determinant.ShouldEqual(-185);
            M4.Determinant(_b).ShouldEqual(-185);
        }

        [Fact]
        private void DeterminantRankDeficientIsZero()
        {
            _a.determinant.ShouldEqual(0);
        }

        [Fact]
        private void DeterminantOfOrthogonalMatrices()
        {
            _rotation.determinant.ShouldEqual(1);
            _permutation.determinant.ShouldEqual(-1);
            M4.Diagonal(-1, 1, 1, 1).determinant.ShouldEqual(-1);
        }

        [Fact]
        private void DeterminantOfProductIsProductOfDeterminants()
        {
            (_b * _c).determinant.ShouldEqual(_b.determinant * _c.determinant);
            (_c * _b).determinant.ShouldEqual(_b.determinant * _c.determinant);
        }

        [Fact]
        private void DeterminantInvariantUnderTranspose()
        {
            _b.transpose.determinant.ShouldEqual(_b.determinant);
            _c.transpose.determinant.ShouldEqual(_c.determinant);
        }

        [Fact]
        private void DeterminantChangesSignOnSwap()
        {
            _b.WithSwappedRows(0, 2).determinant.ShouldEqual(-_b.determinant);
            _b.WithSwappedColumns(1, 3).determinant.ShouldEqual(-_b.determinant);
        }

        [Fact]
        private void DeterminantScalesWithFourthPower()
        {
            (_b * 2).determinant.ShouldEqual(16 * _b.determinant);
            (-_b).determinant.ShouldEqual(_b.determinant);
        }

        [Fact]
        private void InverseTimesMatrixIsIdentity()
        {
            (_b * _b.inverse).ShouldEqual(M4.identity, 1e-14);
            (_b.inverse * _b).ShouldEqual(M4.identity, 1e-14);
            (_c * _c.inverse).ShouldEqual(M4.identity, 1e-14);
            (_c.inverse * _c).ShouldEqual(M4.identity, 1e-14);
        }

        [Fact]
        private void InverseStaticMethod()
        {
            M4.Inverse(_b).ShouldEqual(_b.inverse);
        }

        [Fact]
        private void InverseOfInverse()
        {
            _b.inverse.inverse.ShouldEqual(_b, 1e-14);
        }

        [Fact]
        private void InverseDeterminantIsReciprocal()
        {
            _b.inverse.determinant.ShouldEqual(1.0 / _b.determinant, 1e-14);
        }

        [Fact]
        private void InverseOfDiagonal()
        {
            M4.Diagonal(2, 4, -0.5, 8).inverse.ShouldEqual(M4.Diagonal(0.5, 0.25, -2, 0.125));
        }

        [Fact]
        private void InverseOfOrthogonalIsTranspose()
        {
            _rotation.inverse.ShouldEqual(_rotation.transpose);
            _permutation.inverse.ShouldEqual(_permutation.transpose);
        }

        [Fact]
        private void InverseOfProduct()
        {
            (_b * _c).inverse.ShouldEqual(_c.inverse * _b.inverse, 1e-14);
        }

        [Fact]
        private void IsOrthogonalTrueCases()
        {
            M4.identity.isOrthogonal.ShouldBeTrue();
            _rotation.isOrthogonal.ShouldBeTrue();
            _permutation.isOrthogonal.ShouldBeTrue();
            M4.Diagonal(-1, 1, 1, 1).isOrthogonal.ShouldBeTrue();
            (_rotation * _permutation).isOrthogonal.ShouldBeTrue();
        }

        [Fact]
        private void IsOrthogonalFalseCases()
        {
            (M4.identity * 2).isOrthogonal.ShouldBeFalse();
            _b.isOrthogonal.ShouldBeFalse();
            M4.zero.isOrthogonal.ShouldBeFalse();
        }

        [Fact]
        private void IsOrthogonalToleratesRoundingError()
        {
            // M * M^T is about 2e-14 away from the identity
            (_rotation * (1 + 1e-14)).isOrthogonal.ShouldBeTrue();
            (_rotation * (1 + 1e-9)).isOrthogonal.ShouldBeFalse();
        }

        // [R(a) 0; 0 R(b)] built from two 2D rotations
        private static M4 BlockRotation(double a, double b)
        {
            var ra = M2.Rotate(a);
            var rb = M2.Rotate(b);

            return new M4(
                ra.m00, ra.m01, 0, 0,
                ra.m10, ra.m11, 0, 0,
                0, 0, rb.m00, rb.m01,
                0, 0, rb.m10, rb.m11);
        }

        [Fact]
        private void IsOrthogonalProductOfManyRotations()
        {
            // _rotation mixes the two blocks so the rounding error spreads over the whole matrix
            M4 m = M4.identity;
            for (int i = 0; i < 1000; i++)
                m = _rotation * BlockRotation(0.1 * i, 0.3 * i) * m;

            m.isOrthogonal.ShouldBeTrue();
        }

        [Fact]
        private void IsOrthogonalNaNFalse() => M4.Diagonal(double.NaN, 1, 1, 1).isOrthogonal.ShouldBeFalse();

        [Fact]
        private void IsSymmetricNaNFalse()
        {
            var m = new M4(
                double.NaN, 1, 0, 0,
                2, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1);

            m.isSymmetric.ShouldBeFalse();
        }

        [Fact]
        private void IsSkewSymmetricNaNFalse()
        {
            var m = new M4(
                double.NaN, 1, 0, 0,
                1, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 0);

            m.isSkewSymmetric.ShouldBeFalse();
        }

        [Fact]
        private void IsSkewSymmetricTrueCases()
        {
            M4.zero.isSkewSymmetric.ShouldBeTrue();
            _skew.isSkewSymmetric.ShouldBeTrue();
            (_b - _b.transpose).isSkewSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void IsSkewSymmetricFalseCases()
        {
            M4.identity.isSkewSymmetric.ShouldBeFalse();
            (_b + _b.transpose).isSkewSymmetric.ShouldBeFalse();
            _b.isSkewSymmetric.ShouldBeFalse();
        }

        [Fact]
        private void SymmetricPlusSkewSymmetricDecomposition()
        {
            M4 sym = (_b + _b.transpose) * 0.5;
            M4 skew = (_b - _b.transpose) * 0.5;

            sym.isSymmetric.ShouldBeTrue();
            skew.isSkewSymmetric.ShouldBeTrue();
            (sym + skew).ShouldEqual(_b);
        }

        [Fact]
        private void IsSingularTrueCases()
        {
            M4.zero.isSingular.ShouldBeTrue();
            _a.isSingular.ShouldBeTrue();
            M4.Diagonal(1, 2, 0, 4).isSingular.ShouldBeTrue();
        }

        [Fact]
        private void IsSingularFalseCases()
        {
            M4.identity.isSingular.ShouldBeFalse();
            _b.isSingular.ShouldBeFalse();
            _rotation.isSingular.ShouldBeFalse();
        }

        [Fact]
        private void IsSingularIndependentOfScale()
        {
            // rank 2 up to rounding, but rounding leaves det = -7.1e-15, and at 1e100 the determinant is Inf - Inf
            (_a * 1.1).isSingular.ShouldBeTrue();
            (_a * 1e100).isSingular.ShouldBeTrue();

            // perfectly conditioned, with determinants that are tiny, underflow, or overflow
            M4.Diagonal(1e-5, 1e-5, 1e-5, 1e-5).isSingular.ShouldBeFalse();
            M4.Diagonal(1e-90, 1e-90, 1e-90, 1e-90).isSingular.ShouldBeFalse();
            M4.Diagonal(1e90, 1e90, 1e90, 1e90).isSingular.ShouldBeFalse();
        }

        [Fact]
        private void IsSingularNearlyDependentRowsFalse()
        {
            // the first three rows of _b, then their sum
            var m = new M4(
                2, 0, 1, 3,
                1, 4, 0, 2,
                0, 1, 3, 1,
                3, 5, 4, 6);

            m.isSingular.ShouldBeTrue();
            (m + M4.Diagonal(0, 0, 0, 1e-10)).isSingular.ShouldBeFalse();
        }
    }
}
