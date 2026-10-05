/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M4Tests
{
    public class OrthonormalizeTests
    {
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

        private static void ShouldBeOrthonormal(M4 q)
        {
            (q * q.transpose).ShouldEqual(M4.identity, 1e-14);
            (q.transpose * q).ShouldEqual(M4.identity, 1e-14);
        }

        [Fact]
        private void OrthonormalizeIdentityMatrix() => M4.identity.orthonormalized.ShouldEqual(M4.identity);

        [Fact]
        private void OrthonormalizeAlreadyOrthonormal() => _rotation.orthonormalized.ShouldEqual(_rotation, 1e-15);

        [Fact]
        private void OrthonormalizeScaledDiagonal() => M4.Diagonal(2, 3, 4, 5).orthonormalized.ShouldEqual(M4.identity);

        [Fact]
        private void OrthonormalizeArbitraryMatrix() => ShouldBeOrthonormal(_b.orthonormalized);

        [Fact]
        private void OrthonormalizePreservesFirstColumnDirection()
        {
            _b.orthonormalized.GetColumn(0).ShouldEqual(_b.GetColumn(0).normalized);
        }

        [Fact]
        private void OrthonormalizeIsQOfQRDecomposition()
        {
            M4 q = _b.orthonormalized;
            M4 r = q.transpose * _b;

            for (int i = 0; i < 4; i++)
            {
                r[i, i].ShouldBePositive();
                for (int j = 0; j < i; j++)
                    Abs(r[i, j]).ShouldBeLessThan(1e-14);
            }

            (q * r).ShouldEqual(_b, 1e-14);
        }

        [Fact]
        private void OrthonormalizeNearlyOrthogonal()
        {
            M4 perturbed = _rotation + new M4(
                1e-8, 0, 0, 0,
                0, 0, -1e-8, 0,
                0, 2e-8, 0, 0,
                0, 0, 0, -1e-8);

            M4 q = perturbed.orthonormalized;

            ShouldBeOrthonormal(q);
            q.ShouldEqual(_rotation, 1e-7);
        }

        [Fact]
        private void OrthonormalizeLargeAndSmallMagnitudes()
        {
            (_b * 1e100).orthonormalized.ShouldEqual(_b.orthonormalized, 1e-14);
            (_b * 1e-100).orthonormalized.ShouldEqual(_b.orthonormalized, 1e-14);
        }

        [Fact]
        private void OrthonormalizeDeterminantIsUnit()
        {
            Abs(_b.orthonormalized.determinant).ShouldEqual(1, 1e-14);
            Sign(_b.orthonormalized.determinant).ShouldEqual(Sign(_b.determinant));
        }
    }
}
