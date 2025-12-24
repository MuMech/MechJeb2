/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class MatrixAnalysisTests
    {
        [Fact]
        private void TraceIdentityMatrix() => M3.identity.trace.ShouldEqual(3.0);

        [Fact]
        private void TraceZeroMatrix() => M3.zero.trace.ShouldEqual(0.0);

        [Fact]
        private void TraceArbitraryMatrix()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            m.trace.ShouldEqual(15.0);
        }

        [Fact]
        private void TraceNegativeValues()
        {
            var m = new M3(-1, 2, 3, 4, -5, 6, 7, 8, -9);
            m.trace.ShouldEqual(-15.0);
        }

        [Fact]
        private void TraceDiagonalMatrix()
        {
            var m = M3.Diagonal(2, 3, 4);
            m.trace.ShouldEqual(9.0);
        }

        [Fact]
        private void TraceStaticMethod()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            M3.Trace(m).ShouldEqual(m.trace);
        }

        [Fact]
        private void TraceRotationMatrix()
        {
            var q = Q3.AngleAxis(PI / 3, new V3(1, 2, 3).normalized);
            var m = M3.Rotate(q);

            // For rotation by angle θ, trace = 1 + 2cos(θ)
            m.trace.ShouldEqual(1 + 2 * Cos(PI / 3), 1e-14);
        }

        [Fact]
        private void DiagonalIdentityMatrix() => M3.identity.diagonal.ShouldEqual(V3.one);

        [Fact]
        private void DiagonalZeroMatrix() => M3.zero.diagonal.ShouldEqual(V3.zero);

        [Fact]
        private void DiagonalArbitraryMatrix()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            m.diagonal.ShouldEqual(new V3(1, 5, 9));
        }

        [Fact]
        private void DiagonalFromDiagonalMatrix()
        {
            var v = new V3(2, 3, 4);
            var m = M3.Diagonal(v);
            m.diagonal.ShouldEqual(v);
        }

        [Fact]
        private void IsOrthogonalIdentity() => M3.identity.isOrthogonal.ShouldBeTrue();

        [Fact]
        private void IsOrthogonalRotationMatrix()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);
            var m = M3.Rotate(q);
            m.isOrthogonal.ShouldBeTrue();
        }

        [Fact]
        private void IsOrthogonalPermutationMatrix()
        {
            var m = new M3(0, 1, 0, 0, 0, 1, 1, 0, 0);
            m.isOrthogonal.ShouldBeTrue();
        }

        [Fact]
        private void IsOrthogonalReflectionMatrix()
        {
            var m = new M3(-1, 0, 0, 0, 1, 0, 0, 0, 1);
            m.isOrthogonal.ShouldBeTrue();
        }

        [Fact]
        private void IsOrthogonalScaledMatrixFalse()
        {
            M3 m = M3.identity * 2;
            m.isOrthogonal.ShouldBeFalse();
        }

        [Fact]
        private void IsOrthogonalArbitraryMatrixFalse()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            m.isOrthogonal.ShouldBeFalse();
        }

        [Fact]
        private void IsOrthogonalZeroMatrixFalse() => M3.zero.isOrthogonal.ShouldBeFalse();

        [Fact]
        private void IsSymmetricIdentity() => M3.identity.isSymmetric.ShouldBeTrue();

        [Fact]
        private void IsSymmetricZero() => M3.zero.isSymmetric.ShouldBeTrue();

        [Fact]
        private void IsSymmetricDiagonalMatrix()
        {
            var m = M3.Diagonal(1, 2, 3);
            m.isSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void IsSymmetricSymmetricMatrix()
        {
            var m = new M3(1, 2, 3, 2, 4, 5, 3, 5, 6);
            m.isSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void IsSymmetricRotationMatrixFalse()
        {
            var q = Q3.AngleAxis(PI / 4, V3.zaxis);
            var m = M3.Rotate(q);
            m.isSymmetric.ShouldBeFalse();
        }

        [Fact]
        private void IsSymmetricArbitraryMatrixFalse()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            m.isSymmetric.ShouldBeFalse();
        }

        [Fact]
        private void IsSkewSymmetricZero() => M3.zero.isSkewSymmetric.ShouldBeTrue();

        [Fact]
        private void IsSkewSymmetricSkewMatrix()
        {
            var m = M3.Skew(new V3(1, 2, 3));
            m.isSkewSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void IsSkewSymmetricManualSkewMatrix()
        {
            var m = new M3(0, -3, 2, 3, 0, -1, -2, 1, 0);
            m.isSkewSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void IsSkewSymmetricIdentityFalse() => M3.identity.isSkewSymmetric.ShouldBeFalse();

        [Fact]
        private void IsSkewSymmetricSymmetricMatrixFalse()
        {
            var m = new M3(1, 2, 3, 2, 4, 5, 3, 5, 6);
            m.isSkewSymmetric.ShouldBeFalse();
        }

        [Fact]
        private void IsSkewSymmetricDiagonalZero()
        {
            var m = M3.Skew(new V3(5, -3, 7));
            m.m00.ShouldEqual(0.0);
            m.m11.ShouldEqual(0.0);
            m.m22.ShouldEqual(0.0);
        }

        [Fact]
        private void IsSingularZeroMatrix() => M3.zero.isSingular.ShouldBeTrue();

        [Fact]
        private void IsSingularSingularMatrix()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            m.isSingular.ShouldBeTrue();
        }

        [Fact]
        private void IsSingularRankDeficientMatrix()
        {
            var m = new M3(1, 2, 3, 2, 4, 6, 1, 1, 1);
            m.isSingular.ShouldBeTrue();
        }

        [Fact]
        private void IsSingularIdentityFalse() => M3.identity.isSingular.ShouldBeFalse();

        [Fact]
        private void IsSingularRotationMatrixFalse()
        {
            var q = Q3.AngleAxis(1.234, new V3(1, 2, 3).normalized);
            var m = M3.Rotate(q);
            m.isSingular.ShouldBeFalse();
        }

        [Fact]
        private void IsSingularDiagonalNonZeroFalse()
        {
            var m = M3.Diagonal(1, 2, 3);
            m.isSingular.ShouldBeFalse();
        }

        [Fact]
        private void IsSingularDiagonalWithZero()
        {
            var m = M3.Diagonal(1, 0, 3);
            m.isSingular.ShouldBeTrue();
        }

        [Fact]
        private void TraceInvariantUnderSimilarity()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 10);
            var p = M3.Rotate(Q3.AngleAxis(0.5, new V3(1, 1, 1).normalized));

            double originalTrace = m.trace;
            double similarTrace  = (p * m * p.transpose).trace;

            originalTrace.ShouldEqual(similarTrace, 1e-13);
        }

        [Fact]
        private void TraceOfProductCommutes()
        {
            var a = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);
            var b = new M3(9, 8, 7, 6, 5, 4, 3, 2, 1);

            (a * b).trace.ShouldEqual((b * a).trace, 1e-14);
        }

        [Fact]
        private void SymmetricPlusSkewSymmetricDecomposition()
        {
            var m = new M3(1, 2, 3, 4, 5, 6, 7, 8, 9);

            M3 symmetric     = (m + m.transpose) / 2;
            M3 skewSymmetric = (m - m.transpose) / 2;

            symmetric.isSymmetric.ShouldBeTrue();
            skewSymmetric.isSkewSymmetric.ShouldBeTrue();
            (symmetric + skewSymmetric).ShouldEqual(m, 1e-14);
        }
    }
}
