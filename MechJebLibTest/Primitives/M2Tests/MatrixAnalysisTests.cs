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
    }
}
