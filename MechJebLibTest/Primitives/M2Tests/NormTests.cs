/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M2Tests
{
    public class NormTests
    {
        private static readonly M2 _a = new M2(
            1, 2,
            3, 4);

        private static readonly M2 _b = new M2(
            2, 0,
            1, 3);

        private static readonly M2 _rotation = new M2(
            0.6, -0.8,
            0.8, 0.6);

        [Fact]
        private void FrobeniusNormZeroMatrix() => M2.zero.frobeniusNorm.ShouldEqual(0.0);

        [Fact]
        private void FrobeniusNormIdentityMatrix() => M2.identity.frobeniusNorm.ShouldEqual(Sqrt(2));

        [Fact]
        private void FrobeniusNormDiagonalMatrix() => M2.Diagonal(3, -4).frobeniusNorm.ShouldEqual(5.0);

        [Fact]
        private void FrobeniusNormArbitraryMatrix() => _a.frobeniusNorm.ShouldEqual(Sqrt(30));

        [Fact]
        private void FrobeniusNormStaticMethod() => M2.FrobeniusNorm(_a).ShouldEqual(_a.frobeniusNorm);

        [Fact]
        private void FrobeniusNormScalarMultiplication() => (-3 * _a).frobeniusNorm.ShouldEqual(3 * _a.frobeniusNorm);

        [Fact]
        private void FrobeniusNormInvariantUnderTranspose() => _a.transpose.frobeniusNorm.ShouldEqual(_a.frobeniusNorm);

        [Fact]
        private void FrobeniusNormInvariantUnderOrthogonalMultiplication()
        {
            (M2.Rotate(1.234) * _a).frobeniusNorm.ShouldEqual(_a.frobeniusNorm, 1e-15);
            (_a * M2.Rotate(1.234)).frobeniusNorm.ShouldEqual(_a.frobeniusNorm, 1e-15);
        }

        [Fact]
        private void FrobeniusNormTriangleInequality() =>
            (_a + _b).frobeniusNorm.ShouldBeLessThanOrEqual(_a.frobeniusNorm + _b.frobeniusNorm);

        [Fact]
        private void FrobeniusNormSubmultiplicative() =>
            (_a * _b).frobeniusNorm.ShouldBeLessThanOrEqual(_a.frobeniusNorm * _b.frobeniusNorm);

        [Fact]
        private void FrobeniusNormLargeAndSmallValues()
        {
            (_a * 1e100).frobeniusNorm.ShouldEqual(_a.frobeniusNorm * 1e100, 1e-15);
            (_a * 1e-100).frobeniusNorm.ShouldEqual(_a.frobeniusNorm * 1e-100, 1e-15);
        }

        [Fact]
        private void InfinityNormZeroMatrix() => M2.zero.infinityNorm.ShouldEqual(0.0);

        [Fact]
        private void InfinityNormIdentityMatrix() => M2.identity.infinityNorm.ShouldEqual(1.0);

        [Fact]
        private void InfinityNormArbitraryMatrices()
        {
            // row sums 3, 7
            _a.infinityNorm.ShouldEqual(7);
            // row sums 2, 4
            _b.infinityNorm.ShouldEqual(4);
        }

        [Fact]
        private void InfinityNormEachRowDominant()
        {
            M2.identity.WithRow(0, new V2(5, -5)).infinityNorm.ShouldEqual(10);
            M2.identity.WithRow(1, new V2(5, -5)).infinityNorm.ShouldEqual(10);
        }

        [Fact]
        private void InfinityNormMixedSigns() => new M2(1, -2, -1, -1).infinityNorm.ShouldEqual(3);

        [Fact]
        private void InfinityNormOrthogonalMatrix() => _rotation.infinityNorm.ShouldEqual(1.4);

        [Fact]
        private void InfinityNormScalarMultiplication() => (-3 * _a).infinityNorm.ShouldEqual(3 * _a.infinityNorm);

        [Fact]
        private void InfinityNormTriangleInequality() =>
            (_a + _b).infinityNorm.ShouldBeLessThanOrEqual(_a.infinityNorm + _b.infinityNorm);

        [Fact]
        private void InfinityNormSubmultiplicative() =>
            (_a * _b).infinityNorm.ShouldBeLessThanOrEqual(_a.infinityNorm * _b.infinityNorm);
    }
}
