/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M4Tests
{
    public class NormTests
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

        // left multiplication by the unit quaternion (1/2, 1/2, 1/2, 1/2), a 4D rotation
        private static readonly M4 _rotation = new M4(
            0.5, -0.5, 0.5, 0.5,
            0.5, 0.5, -0.5, 0.5,
            -0.5, 0.5, 0.5, 0.5,
            -0.5, -0.5, -0.5, 0.5);

        [Fact]
        private void FrobeniusNormZeroMatrix() => M4.zero.frobeniusNorm.ShouldEqual(0.0);

        [Fact]
        private void FrobeniusNormIdentityMatrix() => M4.identity.frobeniusNorm.ShouldEqual(2.0);

        [Fact]
        private void FrobeniusNormDiagonalMatrix() => M4.Diagonal(1, -2, 2, 4).frobeniusNorm.ShouldEqual(5.0);

        [Fact]
        private void FrobeniusNormArbitraryMatrix()
        {
            // 1^2 + 2^2 + ... + 16^2 = 1496
            _a.frobeniusNorm.ShouldEqual(Sqrt(1496));
        }

        [Fact]
        private void FrobeniusNormStaticMethod() => M4.FrobeniusNorm(_b).ShouldEqual(_b.frobeniusNorm);

        [Fact]
        private void FrobeniusNormScalarMultiplication() => (-3 * _b).frobeniusNorm.ShouldEqual(3 * _b.frobeniusNorm);

        [Fact]
        private void FrobeniusNormInvariantUnderTranspose() => _a.transpose.frobeniusNorm.ShouldEqual(_a.frobeniusNorm);

        [Fact]
        private void FrobeniusNormInvariantUnderOrthogonalMultiplication()
        {
            (_rotation * _b).frobeniusNorm.ShouldEqual(_b.frobeniusNorm, 1e-15);
            (_b * _rotation).frobeniusNorm.ShouldEqual(_b.frobeniusNorm, 1e-15);
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
            (_b * 1e100).frobeniusNorm.ShouldEqual(_b.frobeniusNorm * 1e100, 1e-15);
            (_b * 1e-100).frobeniusNorm.ShouldEqual(_b.frobeniusNorm * 1e-100, 1e-15);
        }

        [Fact]
        private void InfinityNormZeroMatrix() => M4.zero.infinityNorm.ShouldEqual(0.0);

        [Fact]
        private void InfinityNormIdentityMatrix() => M4.identity.infinityNorm.ShouldEqual(1.0);

        [Fact]
        private void InfinityNormArbitraryMatrices()
        {
            // row sums 10, 26, 42, 58
            _a.infinityNorm.ShouldEqual(58);
            // row sums 6, 7, 5, 8
            _b.infinityNorm.ShouldEqual(8);
        }

        [Fact]
        private void InfinityNormEachRowDominant()
        {
            var row = new V4(5, -5, 5, -5);

            for (int i = 0; i < 4; i++)
                M4.identity.WithRow(i, row).infinityNorm.ShouldEqual(20);
        }

        [Fact]
        private void InfinityNormMixedSigns()
        {
            new M4(
                1, -2, 3, -4,
                0, 0, 0, 0,
                -1, -1, -1, -1,
                0, 0, 0, 0).infinityNorm.ShouldEqual(10);
        }

        [Fact]
        private void InfinityNormOrthogonalMatrix() => _rotation.infinityNorm.ShouldEqual(2.0);

        [Fact]
        private void InfinityNormScalarMultiplication() => (-3 * _b).infinityNorm.ShouldEqual(3 * _b.infinityNorm);

        [Fact]
        private void InfinityNormTriangleInequality() =>
            (_a + _b).infinityNorm.ShouldBeLessThanOrEqual(_a.infinityNorm + _b.infinityNorm);

        [Fact]
        private void InfinityNormSubmultiplicative() =>
            (_a * _b).infinityNorm.ShouldBeLessThanOrEqual(_a.infinityNorm * _b.infinityNorm);
    }
}
