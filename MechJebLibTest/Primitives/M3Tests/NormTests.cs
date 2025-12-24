using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class NormTests
    {
        [Fact]
        private void FrobeniusNormZeroMatrix() => M3.zero.frobeniusNorm.ShouldEqual(0.0);

        [Fact]
        private void FrobeniusNormIdentityMatrix() => M3.identity.frobeniusNorm.ShouldEqual(Sqrt(3));

        [Fact]
        private void FrobeniusNormDiagonalMatrix()
        {
            var m = M3.Diagonal(3, 4, 0);
            m.frobeniusNorm.ShouldEqual(5.0);
        }

        [Fact]
        private void FrobeniusNormSimpleMatrix()
        {
            var m = new M3(1, 0, 0, 0, 2, 0, 0, 0, 2);
            m.frobeniusNorm.ShouldEqual(3.0);
        }

        [Fact]
        private void FrobeniusNormArbitraryMatrix()
        {
            var    m        = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            double expected = Sqrt(1 + 4 + 9 + 16 + 25 + 36 + 49 + 64 + 81);
            m.frobeniusNorm.ShouldEqual(expected);
        }

        [Fact]
        private void FrobeniusNormNegativeValues()
        {
            var    m        = new M3(-1, -2, -3, -4, -5, -6, -7, -8, -9);
            double expected = Sqrt(1 + 4 + 9 + 16 + 25 + 36 + 49 + 64 + 81);
            m.frobeniusNorm.ShouldEqual(expected);
        }

        [Fact]
        private void FrobeniusNormRotationMatrix()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);
            var m = M3.Rotate(q);
            m.frobeniusNorm.ShouldEqual(Sqrt(3), 1e-14);
        }

        [Fact]
        private void FrobeniusNormScaledIdentity()
        {
            M3 m = M3.identity * 5;
            m.frobeniusNorm.ShouldEqual(5 * Sqrt(3));
        }

        [Fact]
        private void FrobeniusNormScalarMultiplication()
        {
            var    m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            double k = 3.5;
            (m * k).frobeniusNorm.ShouldEqual(k * m.frobeniusNorm, 1e-14);
        }

        [Fact]
        private void FrobeniusNormStaticMethod()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            M3.FrobeniusNorm(m).ShouldEqual(m.frobeniusNorm);
        }

        [Fact]
        private void FrobeniusNormTriangleInequality()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a + b).frobeniusNorm.ShouldBeLessThanOrEqual(a.frobeniusNorm + b.frobeniusNorm + 1e-14);
        }

        [Fact]
        private void FrobeniusNormSubmultiplicative()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a * b).frobeniusNorm.ShouldBeLessThanOrEqual(a.frobeniusNorm * b.frobeniusNorm + 1e-10);
        }

        [Fact]
        private void FrobeniusNormInvariantUnderTranspose()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            m.frobeniusNorm.ShouldEqual(m.transpose.frobeniusNorm);
        }

        [Fact]
        private void FrobeniusNormInvariantUnderOrthogonalMultiplication()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 10);
            var q = M3.Rotate(Q3.AngleAxis(0.789, new V3(1, 2, 3).normalized));

            (q * m).frobeniusNorm.ShouldEqual(m.frobeniusNorm, 1e-13);
            (m * q).frobeniusNorm.ShouldEqual(m.frobeniusNorm, 1e-13);
        }

        [Fact]
        private void FrobeniusNormSkewSymmetricMatrix()
        {
            var v = new V3(1, 2, 3);
            var m = M3.Skew(v);

            double expected = Sqrt(2 * (1 + 4 + 9));
            m.frobeniusNorm.ShouldEqual(expected);
        }

        [Fact]
        private void FrobeniusNormLargeValues()
        {
            var m = M3.Diagonal(1e150, 1e150, 1e150);
            m.frobeniusNorm.ShouldEqual(Sqrt(3) * 1e150);
        }

        [Fact]
        private void FrobeniusNormSmallValues()
        {
            var m = M3.Diagonal(1e-150, 1e-150, 1e-150);
            m.frobeniusNorm.ShouldEqual(Sqrt(3) * 1e-150);
        }

        [Fact]
        private void FrobeniusNormAlwaysNonNegative()
        {
            var m = new M3(-5, -3, 2, 7, -1, 4, -6, 8, -2);
            m.frobeniusNorm.ShouldBeGreaterThanOrEqual(0.0);
        }

        [Fact]
        private void FrobeniusNormZeroOnlyForZeroMatrix()
        {
            var m = new M3(0, 0, 0, 0, 1e-20, 0, 0, 0, 0);
            m.frobeniusNorm.ShouldBeGreaterThan(0.0);
        }

        [Fact]
        private void InfinityNormZeroMatrix() => M3.zero.infinityNorm.ShouldEqual(0.0);

        [Fact]
        private void InfinityNormIdentityMatrix() => M3.identity.infinityNorm.ShouldEqual(1.0);

        [Fact]
        private void InfinityNormDiagonalMatrix()
        {
            var m = M3.Diagonal(2, 5, 3);
            m.infinityNorm.ShouldEqual(5.0);
        }

        [Fact]
        private void InfinityNormRowDominant()
        {
            var m = new M3(1, 2, 3, 0, 0, 0, 0, 0, 0);
            m.infinityNorm.ShouldEqual(6.0);
        }

        [Fact]
        private void InfinityNormSecondRowDominant()
        {
            var m = new M3(1, 0, 0, 2, 3, 4, 0, 0, 0);
            m.infinityNorm.ShouldEqual(9.0);
        }

        [Fact]
        private void InfinityNormThirdRowDominant()
        {
            var m = new M3(1, 0, 0, 0, 1, 0, 5, 5, 5);
            m.infinityNorm.ShouldEqual(15.0);
        }

        [Fact]
        private void InfinityNormNegativeValues()
        {
            var m = new M3(-1, -2, -3, 0, 0, 0, 0, 0, 0);
            m.infinityNorm.ShouldEqual(6.0);
        }

        [Fact]
        private void InfinityNormMixedSigns()
        {
            var m = new M3(-1, 2, -3, 4, -5, 6, -7, 8, -9);
            m.infinityNorm.ShouldEqual(24.0);
        }

        [Fact]
        private void InfinityNormArbitraryMatrix()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            m.infinityNorm.ShouldEqual(24.0);
        }

        [Fact]
        private void InfinityNormScaledIdentity()
        {
            M3 m = M3.identity * 7;
            m.infinityNorm.ShouldEqual(7.0);
        }

        [Fact]
        private void InfinityNormScalarMultiplication()
        {
            var    m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            double k = 2.5;
            (m * k).infinityNorm.ShouldEqual(k * m.infinityNorm);
        }

        [Fact]
        private void InfinityNormRotationMatrix()
        {
            var q = Q3.AngleAxis(PI / 4, V3.zaxis);
            var m = M3.Rotate(q);

            m.infinityNorm.ShouldBeLessThanOrEqual(2.0);
            m.infinityNorm.ShouldBeGreaterThan(0.0);
        }

        [Fact]
        private void InfinityNormTriangleInequality()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a + b).infinityNorm.ShouldBeLessThanOrEqual(a.infinityNorm + b.infinityNorm);
        }

        [Fact]
        private void InfinityNormSubmultiplicative()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a * b).infinityNorm.ShouldBeLessThanOrEqual(a.infinityNorm * b.infinityNorm);
        }

        [Fact]
        private void InfinityNormSkewSymmetricMatrix()
        {
            var v = new V3(1, 2, 3);
            var m = M3.Skew(v);

            m.infinityNorm.ShouldEqual(5.0);
        }

        [Fact]
        private void InfinityNormLargeValues()
        {
            var m = new M3(1e150, 1e150, 1e150, 0, 0, 0, 0, 0, 0);
            m.infinityNorm.ShouldEqual(3e150);
        }

        [Fact]
        private void InfinityNormSmallValues()
        {
            var m = new M3(1e-150, 1e-150, 1e-150, 0, 0, 0, 0, 0, 0);
            m.infinityNorm.ShouldEqual(3e-150);
        }

        [Fact]
        private void InfinityNormAlwaysNonNegative()
        {
            var m = new M3(-5, -3, 2, 7, -1, 4, -6, 8, -2);
            m.infinityNorm.ShouldBeGreaterThanOrEqual(0.0);
        }

        [Fact]
        private void InfinityNormZeroOnlyForZeroMatrix()
        {
            var m = new M3(0, 0, 0, 0, 1e-20, 0, 0, 0, 0);
            m.infinityNorm.ShouldBeGreaterThan(0.0);
        }

        [Fact]
        private void InfinityNormEqualRows()
        {
            var m = new M3(1, 2, 3, 1, 2, 3, 1, 2, 3);
            m.infinityNorm.ShouldEqual(6.0);
        }
    }
}
