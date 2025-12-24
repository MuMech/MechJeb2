/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Primitives.M3Tests
{
    public class DiagonalSkewConstructionTests
    {
        [Fact]
        private void DiagonalFromScalar()
        {
            var m = M3.Diagonal(5);

            m.m00.ShouldEqual(5);
            m.m11.ShouldEqual(5);
            m.m22.ShouldEqual(5);
            m.m01.ShouldEqual(0);
            m.m02.ShouldEqual(0);
            m.m10.ShouldEqual(0);
            m.m12.ShouldEqual(0);
            m.m20.ShouldEqual(0);
            m.m21.ShouldEqual(0);
        }

        [Fact]
        private void DiagonalFromScalarZero()
        {
            var m = M3.Diagonal(0);
            m.ShouldEqual(M3.zero);
        }

        [Fact]
        private void DiagonalFromScalarOne()
        {
            var m = M3.Diagonal(1);
            m.ShouldEqual(M3.identity);
        }

        [Fact]
        private void DiagonalFromScalarNegative()
        {
            var m = M3.Diagonal(-3);

            m.m00.ShouldEqual(-3);
            m.m11.ShouldEqual(-3);
            m.m22.ShouldEqual(-3);
        }

        [Fact]
        private void DiagonalFromVector()
        {
            var m = M3.Diagonal(new V3(2, 3, 4));

            m.m00.ShouldEqual(2);
            m.m11.ShouldEqual(3);
            m.m22.ShouldEqual(4);
            m.m01.ShouldEqual(0);
            m.m02.ShouldEqual(0);
            m.m10.ShouldEqual(0);
            m.m12.ShouldEqual(0);
            m.m20.ShouldEqual(0);
            m.m21.ShouldEqual(0);
        }

        [Fact]
        private void DiagonalFromVectorZero()
        {
            var m = M3.Diagonal(V3.zero);
            m.ShouldEqual(M3.zero);
        }

        [Fact]
        private void DiagonalFromVectorOne()
        {
            var m = M3.Diagonal(V3.one);
            m.ShouldEqual(M3.identity);
        }

        [Fact]
        private void DiagonalFromThreeScalars()
        {
            var m = M3.Diagonal(5, 6, 7);

            m.m00.ShouldEqual(5);
            m.m11.ShouldEqual(6);
            m.m22.ShouldEqual(7);
            m.m01.ShouldEqual(0);
            m.m02.ShouldEqual(0);
            m.m10.ShouldEqual(0);
            m.m12.ShouldEqual(0);
            m.m20.ShouldEqual(0);
            m.m21.ShouldEqual(0);
        }

        [Fact]
        private void DiagonalFromThreeScalarsMatchesVector()
        {
            var m1 = M3.Diagonal(2, 3, 4);
            var m2 = M3.Diagonal(new V3(2, 3, 4));

            m1.ShouldEqual(m2);
        }

        [Fact]
        private void DiagonalMatrixIsSymmetric()
        {
            var m = M3.Diagonal(2, 3, 4);
            m.isSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void DiagonalMatrixDeterminant()
        {
            var m = M3.Diagonal(2, 3, 4);
            m.determinant.ShouldEqual(24);
        }

        [Fact]
        private void DiagonalMatrixTrace()
        {
            var m = M3.Diagonal(2, 3, 4);
            m.trace.ShouldEqual(9);
        }

        [Fact]
        private void DiagonalMatrixInverse()
        {
            var m = M3.Diagonal(2, 4, 8);
            var inv = m.inverse;

            inv.ShouldEqual(M3.Diagonal(0.5, 0.25, 0.125), 1e-14);
        }

        [Fact]
        private void DiagonalMatrixMultiplication()
        {
            var m1 = M3.Diagonal(2, 3, 4);
            var m2 = M3.Diagonal(5, 6, 7);

            (m1 * m2).ShouldEqual(M3.Diagonal(10, 18, 28));
        }

        [Fact]
        private void DiagonalMatrixVectorMultiplication()
        {
            var m = M3.Diagonal(2, 3, 4);
            var v = new V3(1, 2, 3);

            (m * v).ShouldEqual(new V3(2, 6, 12));
        }

        [Fact]
        private void DiagonalMatrixLargeValues()
        {
            var m = M3.Diagonal(1e100, 1e150, 1e200);

            m.m00.ShouldEqual(1e100);
            m.m11.ShouldEqual(1e150);
            m.m22.ShouldEqual(1e200);
        }

        [Fact]
        private void DiagonalMatrixSmallValues()
        {
            var m = M3.Diagonal(1e-100, 1e-150, 1e-200);

            m.m00.ShouldEqual(1e-100);
            m.m11.ShouldEqual(1e-150);
            m.m22.ShouldEqual(1e-200);
        }

        [Fact]
        private void SkewBasicVector()
        {
            var v = new V3(1, 2, 3);
            var m = M3.Skew(v);

            m.m00.ShouldEqual(0);
            m.m01.ShouldEqual(-3);
            m.m02.ShouldEqual(2);
            m.m10.ShouldEqual(3);
            m.m11.ShouldEqual(0);
            m.m12.ShouldEqual(-1);
            m.m20.ShouldEqual(-2);
            m.m21.ShouldEqual(1);
            m.m22.ShouldEqual(0);
        }

        [Fact]
        private void SkewZeroVector()
        {
            var m = M3.Skew(V3.zero);
            m.ShouldEqual(M3.zero);
        }

        [Fact]
        private void SkewXAxis()
        {
            var m = M3.Skew(V3.xaxis);

            m.m00.ShouldEqual(0);
            m.m01.ShouldEqual(0);
            m.m02.ShouldEqual(0);
            m.m10.ShouldEqual(0);
            m.m11.ShouldEqual(0);
            m.m12.ShouldEqual(-1);
            m.m20.ShouldEqual(0);
            m.m21.ShouldEqual(1);
            m.m22.ShouldEqual(0);
        }

        [Fact]
        private void SkewYAxis()
        {
            var m = M3.Skew(V3.yaxis);

            m.m00.ShouldEqual(0);
            m.m01.ShouldEqual(0);
            m.m02.ShouldEqual(1);
            m.m10.ShouldEqual(0);
            m.m11.ShouldEqual(0);
            m.m12.ShouldEqual(0);
            m.m20.ShouldEqual(-1);
            m.m21.ShouldEqual(0);
            m.m22.ShouldEqual(0);
        }

        [Fact]
        private void SkewZAxis()
        {
            var m = M3.Skew(V3.zaxis);

            m.m00.ShouldEqual(0);
            m.m01.ShouldEqual(-1);
            m.m02.ShouldEqual(0);
            m.m10.ShouldEqual(1);
            m.m11.ShouldEqual(0);
            m.m12.ShouldEqual(0);
            m.m20.ShouldEqual(0);
            m.m21.ShouldEqual(0);
            m.m22.ShouldEqual(0);
        }

        [Fact]
        private void SkewIsSkewSymmetric()
        {
            var v = new V3(3, -5, 7);
            var m = M3.Skew(v);

            m.isSkewSymmetric.ShouldBeTrue();
        }

        [Fact]
        private void SkewDiagonalIsZero()
        {
            var v = new V3(3, -5, 7);
            var m = M3.Skew(v);

            m.m00.ShouldEqual(0);
            m.m11.ShouldEqual(0);
            m.m22.ShouldEqual(0);
        }

        [Fact]
        private void SkewTraceIsZero()
        {
            var v = new V3(3, -5, 7);
            var m = M3.Skew(v);

            m.trace.ShouldEqual(0);
        }

        [Fact]
        private void SkewDeterminantIsZero()
        {
            var v = new V3(3, -5, 7);
            var m = M3.Skew(v);

            m.determinant.ShouldBeZero(1e-14);
        }

        [Fact]
        private void SkewIsSingular()
        {
            var v = new V3(3, -5, 7);
            var m = M3.Skew(v);

            m.isSingular.ShouldBeTrue();
        }

        [Fact]
        private void SkewCrossProductEquivalence()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            var skewA = M3.Skew(a);
            V3 crossProduct = V3.Cross(a, b);
            V3 matrixProduct = skewA * b;

            matrixProduct.ShouldEqual(crossProduct);
        }

        [Fact]
        private void SkewCrossProductEquivalenceArbitrary()
        {
            var a = new V3(3.5, -2.7, 8.1);
            var b = new V3(-1.2, 4.6, -0.9);

            var skewA = M3.Skew(a);
            V3 crossProduct = V3.Cross(a, b);
            V3 matrixProduct = skewA * b;

            matrixProduct.ShouldEqual(crossProduct, 1e-14);
        }

        [Fact]
        private void SkewNegativeVector()
        {
            var v = new V3(1, 2, 3);
            var m1 = M3.Skew(v);
            var m2 = M3.Skew(-v);

            m2.ShouldEqual(-m1);
        }

        [Fact]
        private void SkewScaledVector()
        {
            var v = new V3(1, 2, 3);
            double k = 2.5;

            var m1 = M3.Skew(v * k);
            var m2 = M3.Skew(v) * k;

            m1.ShouldEqual(m2);
        }

        [Fact]
        private void SkewLinearInVector()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            var skewSum = M3.Skew(a + b);
            var sumSkew = M3.Skew(a) + M3.Skew(b);

            skewSum.ShouldEqual(sumSkew);
        }

        [Fact]
        private void SkewTransposeIsNegative()
        {
            var v = new V3(3, -5, 7);
            var m = M3.Skew(v);

            m.transpose.ShouldEqual(-m);
        }

        [Fact]
        private void SkewLargeValues()
        {
            var v = new V3(1e100, 1e100, 1e100);
            var m = M3.Skew(v);

            m.m01.ShouldEqual(-1e100);
            m.m10.ShouldEqual(1e100);
        }

        [Fact]
        private void SkewSmallValues()
        {
            var v = new V3(1e-100, 1e-100, 1e-100);
            var m = M3.Skew(v);

            m.m01.ShouldEqual(-1e-100);
            m.m10.ShouldEqual(1e-100);
        }
    }
}
