/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.M2Tests
{
    public class MatrixAnalysisTests
    {
        private static readonly M2 _a = new M2(
            1, 2,
            3, 4);

        private static readonly M2 _b = new M2(
            2, 0,
            1, 3);

        // rank 1
        private static readonly M2 _singular = new M2(
            1, 2,
            2, 4);

        private static readonly M2 _rotation = new M2(
            0.6, -0.8,
            0.8, 0.6);

        [Fact]
        private void DeterminantBasicMatrices()
        {
            M2.identity.determinant.ShouldEqual(1);
            M2.zero.determinant.ShouldEqual(0);
            M2.Diagonal(3, 4).determinant.ShouldEqual(12);
        }

        [Fact]
        private void DeterminantKnownValue()
        {
            _a.determinant.ShouldEqual(-2);
            _b.determinant.ShouldEqual(6);
            M2.Determinant(_a).ShouldEqual(-2);
        }

        [Fact]
        private void DeterminantRankDeficientIsZero() => _singular.determinant.ShouldEqual(0);

        [Fact]
        private void DeterminantOfOrthogonalMatrices()
        {
            _rotation.determinant.ShouldEqual(1);
            M2.Rotate(1.234).determinant.ShouldEqual(1, 1e-15);
            M2.Diagonal(1, -1).determinant.ShouldEqual(-1);
        }

        [Fact]
        private void DeterminantOfProductIsProductOfDeterminants()
        {
            (_a * _b).determinant.ShouldEqual(_a.determinant * _b.determinant);
            (_b * _a).determinant.ShouldEqual(_a.determinant * _b.determinant);
        }

        [Fact]
        private void DeterminantInvariantUnderTranspose() => _a.transpose.determinant.ShouldEqual(_a.determinant);

        [Fact]
        private void DeterminantChangesSignOnSwap()
        {
            _a.WithSwappedRows(0, 1).determinant.ShouldEqual(-_a.determinant);
            _a.WithSwappedColumns(0, 1).determinant.ShouldEqual(-_a.determinant);
        }

        [Fact]
        private void DeterminantScalesWithSquare()
        {
            (_a * 3).determinant.ShouldEqual(9 * _a.determinant);
            (-_a).determinant.ShouldEqual(_a.determinant);
        }

        [Fact]
        private void InverseKnownValues()
        {
            _a.inverse.ShouldEqual(new M2(-2, 1, 1.5, -0.5));
            M2.Inverse(_a).ShouldEqual(_a.inverse);
        }

        [Fact]
        private void InverseTimesMatrixIsIdentity()
        {
            (_a * _a.inverse).ShouldEqual(M2.identity);
            (_a.inverse * _a).ShouldEqual(M2.identity);
            (_b * _b.inverse).ShouldEqual(M2.identity, 1e-15);
            (_b.inverse * _b).ShouldEqual(M2.identity, 1e-15);
        }

        [Fact]
        private void InverseOfInverse() => _b.inverse.inverse.ShouldEqual(_b, 1e-15);

        [Fact]
        private void InverseOfDiagonal() => M2.Diagonal(2, -0.5).inverse.ShouldEqual(M2.Diagonal(0.5, -2));

        [Fact]
        private void InverseOfOrthogonalIsTranspose()
        {
            _rotation.inverse.ShouldEqual(_rotation.transpose, 1e-15);
            M2.Rotate(1.234).inverse.ShouldEqual(M2.Rotate(1.234).transpose, 1e-15);
        }

        [Fact]
        private void InverseOfProduct() => (_a * _b).inverse.ShouldEqual(_b.inverse * _a.inverse, 1e-15);

        [Fact]
        private void IsOrthogonalTrueCases()
        {
            M2.identity.isOrthogonal.ShouldBeTrue();
            _rotation.isOrthogonal.ShouldBeTrue();
            M2.Rotate(1.234).isOrthogonal.ShouldBeTrue();
            M2.Diagonal(1, -1).isOrthogonal.ShouldBeTrue();
            new M2(0, 1, 1, 0).isOrthogonal.ShouldBeTrue();
        }

        [Fact]
        private void IsOrthogonalFalseCases()
        {
            (M2.identity * 2).isOrthogonal.ShouldBeFalse();
            _a.isOrthogonal.ShouldBeFalse();
            M2.zero.isOrthogonal.ShouldBeFalse();
        }

        [Fact]
        private void IsOrthogonalToleratesRoundingError()
        {
            // M * M^T is about 2e-14 away from the identity
            (_rotation * (1 + 1e-14)).isOrthogonal.ShouldBeTrue();
            (_rotation * (1 + 1e-9)).isOrthogonal.ShouldBeFalse();
        }

        [Fact]
        private void IsOrthogonalProductOfManyRotations()
        {
            M2 m = M2.identity;
            for (int i = 0; i < 1000; i++)
                m = M2.Rotate(0.1 * i) * m;

            m.isOrthogonal.ShouldBeTrue();
        }

        [Fact]
        private void IsOrthogonalNaNFalse() => M2.Diagonal(double.NaN, 1).isOrthogonal.ShouldBeFalse();

        [Fact]
        private void IsSymmetricNaNFalse() => new M2(double.NaN, 1, 2, 1).isSymmetric.ShouldBeFalse();

        [Fact]
        private void IsSkewSymmetricNaNFalse() => new M2(double.NaN, 1, 1, 0).isSkewSymmetric.ShouldBeFalse();

        [Fact]
        private void IsSkewSymmetricTrueCases()
        {
            M2.zero.isSkewSymmetric.ShouldBeTrue();
            M2.Skew(3).isSkewSymmetric.ShouldBeTrue();
            (_a - _a.transpose).isSkewSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void IsSkewSymmetricFalseCases()
        {
            M2.identity.isSkewSymmetric.ShouldBeFalse();
            _a.isSkewSymmetric.ShouldBeFalse();
            (_a + _a.transpose).isSkewSymmetric.ShouldBeFalse();
        }

        [Fact]
        private void SymmetricPlusSkewSymmetricDecomposition()
        {
            M2 sym = (_a + _a.transpose) * 0.5;
            M2 skew = (_a - _a.transpose) * 0.5;

            sym.isSymmetric.ShouldBeTrue();
            skew.isSkewSymmetric.ShouldBeTrue();
            (sym + skew).ShouldEqual(_a);
        }

        [Fact]
        private void IsSingularTrueCases()
        {
            M2.zero.isSingular.ShouldBeTrue();
            _singular.isSingular.ShouldBeTrue();
            M2.Diagonal(1, 0).isSingular.ShouldBeTrue();
        }

        [Fact]
        private void IsSingularFalseCases()
        {
            M2.identity.isSingular.ShouldBeFalse();
            _a.isSingular.ShouldBeFalse();
            _rotation.isSingular.ShouldBeFalse();
        }

        [Fact]
        private void IsSingularIndependentOfScale()
        {
            // rank 1 up to rounding, but rounding leaves det = -2.3e-10, and at 1e200 the determinant is Inf - Inf
            new M2(700.1, 1300.3, 1.9 * 700.1, 1.9 * 1300.3).isSingular.ShouldBeTrue();
            (_singular * 1e200).isSingular.ShouldBeTrue();

            // perfectly conditioned, with determinants that are tiny, underflow, or overflow
            M2.Diagonal(1e-9, 1e-9).isSingular.ShouldBeFalse();
            M2.Diagonal(1e-200, 1e-200).isSingular.ShouldBeFalse();
            M2.Diagonal(1e200, 1e200).isSingular.ShouldBeFalse();
        }

        [Fact]
        private void IsSingularNearlyDependentRowsFalse()
        {
            new M2(1, 2, 3, 6).isSingular.ShouldBeTrue();
            new M2(1, 2, 3, 6 + 1e-10).isSingular.ShouldBeFalse();
        }
    }
}
