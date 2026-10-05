/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M2Tests
{
    public class RotationTests
    {
        private static readonly M2 _a = new M2(
            1, 2,
            3, 4);

        private static readonly M2 _b = new M2(
            2, 0,
            1, 3);

        [Fact]
        private void RotateIsProperOrthogonal()
        {
            M2 r = M2.Rotate(1.234);

            r.isOrthogonal.ShouldBeTrue();
            r.determinant.ShouldEqual(1, 1e-15);
        }

        [Fact]
        private void RotateKnownValues()
        {
            M2.Rotate(0).ShouldEqual(M2.identity);
            M2.Rotate(PI / 2).ShouldEqual(new M2(0, -1, 1, 0));
            M2.Rotate(PI).ShouldEqual(new M2(-1, 0, 0, -1));
        }

        [Fact]
        private void RotateMapsXAxisCounterClockwise()
        {
            (M2.Rotate(0.7) * V2.xaxis).ShouldEqual(new V2(Cos(0.7), Sin(0.7)));
            (M2.Rotate(PI / 2) * V2.xaxis).ShouldEqual(V2.yaxis);
        }

        [Fact]
        private void RotateMatchesSignedAngle()
        {
            var v = new V2(3, -4);

            V2.SignedAngle(v, M2.Rotate(1.234) * v).ShouldEqual(1.234, 1e-14);
            V2.SignedAngle(v, M2.Rotate(-2.5) * v).ShouldEqual(-2.5, 1e-14);
            (M2.Rotate(1.234) * v).magnitude.ShouldEqual(v.magnitude, 1e-15);
        }

        [Fact]
        private void RotateComposes()
        {
            (M2.Rotate(0.3) * M2.Rotate(1.1)).ShouldEqual(M2.Rotate(1.4), 1e-15);
            (M2.Rotate(0.3) * M2.Rotate(1.1)).ShouldEqual(M2.Rotate(1.1) * M2.Rotate(0.3), 1e-15);
        }

        [Fact]
        private void RotateInverseIsTransposeAndNegativeAngle()
        {
            M2 r = M2.Rotate(1.234);

            r.transpose.ShouldEqual(M2.Rotate(-1.234));
            r.inverse.ShouldEqual(M2.Rotate(-1.234), 1e-15);
        }

        [Fact]
        private void AngleRoundTrip()
        {
            double[] angles = { 0, PI / 2, -PI / 2, PI, 1e-10, -2.5, 3.0 };

            foreach (double angle in angles)
                M2.Rotate(angle).angle.ShouldEqual(angle, 1e-15);
        }

        [Fact]
        private void AngleOfScaledRotation()
        {
            (M2.Rotate(-2.5) * 7).angle.ShouldEqual(-2.5, 1e-15);
        }

        [Fact]
        private void SkewKnownValues()
        {
            M2.Skew(2.5).ShouldEqual(new M2(0, -2.5, 2.5, 0));
            (M2.Skew(2.5) * new V2(3, -4)).ShouldEqual(new V2(10, 7.5));
        }

        [Fact]
        private void SkewMatchesThreeDimensionalCross()
        {
            var v = new V2(3, -4);
            double omega = -1.7;

            V3 cross = V3.Cross(new V3(0, 0, omega), new V3(v.x, v.y, 0));

            (M2.Skew(omega) * v).ShouldEqual(new V2(cross.x, cross.y));
        }

        [Fact]
        private void SkewMatchesCrossAndOrthonormal()
        {
            var a = new V2(1.5, -2.7);
            var b = new V2(-0.5, 4.1);

            V2.Dot(b, M2.Skew(1) * a).ShouldEqual(V2.Cross(a, b));
            (M2.Skew(2) * a).ShouldEqual(2 * a.magnitude * a.orthonormal, 1e-15);
        }

        [Fact]
        private void SkewIsScaledQuarterTurn()
        {
            M2.Skew(1).ShouldEqual(M2.Rotate(PI / 2));
            M2.Skew(3).ShouldEqual(3 * M2.Rotate(PI / 2));
            M2.Skew(3).isSkewSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void OrthonormalizeIdentityMatrix() => M2.identity.orthonormalized.ShouldEqual(M2.identity);

        [Fact]
        private void OrthonormalizeRotationUnchanged() => M2.Rotate(1.234).orthonormalized.ShouldEqual(M2.Rotate(1.234), 1e-15);

        [Fact]
        private void OrthonormalizeScaledDiagonal() => M2.Diagonal(2, 3).orthonormalized.ShouldEqual(M2.identity);

        [Fact]
        private void OrthonormalizeArbitraryMatrix()
        {
            // det > 0 gives the rotation that carries the x-axis onto the first column
            _b.orthonormalized.ShouldEqual(M2.Rotate(Atan2(1, 2)), 1e-15);

            // det < 0 gives a reflection
            M2 q = _a.orthonormalized;

            (q * q.transpose).ShouldEqual(M2.identity, 1e-15);
            q.determinant.ShouldEqual(-1, 1e-15);
        }

        [Fact]
        private void OrthonormalizePreservesFirstColumnDirection()
        {
            _a.orthonormalized.GetColumn(0).ShouldEqual(_a.GetColumn(0).normalized);
        }

        [Fact]
        private void OrthonormalizeIsQOfQRDecomposition()
        {
            M2 q = _a.orthonormalized;
            M2 r = q.transpose * _a;

            r[0, 0].ShouldBePositive();
            r[1, 1].ShouldBePositive();
            Abs(r[1, 0]).ShouldBeLessThan(1e-14);
            (q * r).ShouldEqual(_a, 1e-15);
        }
    }
}
