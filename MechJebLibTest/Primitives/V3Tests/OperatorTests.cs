/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class OperatorTests
    {
        [Fact]
        private void AdditionBasicVectors()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);

            (a + b).ShouldEqual(new V3(5, 7, 9));
        }

        [Fact]
        private void AdditionWithNegativeComponents()
        {
            var a = new V3(-1, 2, -3);
            var b = new V3(4, -5, 6);

            (a + b).ShouldEqual(new V3(3, -3, 3));
        }

        [Fact]
        private void AdditionWithZeroVector()
        {
            var v = new V3(1, 2, 3);

            (v + V3.zero).ShouldEqual(v);
            (V3.zero + v).ShouldEqual(v);
        }

        [Fact]
        private void AdditionIsCommutative()
        {
            var a = new V3(1.5, -2.7, 3.2);
            var b = new V3(-0.5, 4.1, -2.3);

            (a + b).ShouldEqual(b + a);
        }

        [Fact]
        private void AdditionIsAssociative()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(4, 5, 6);
            var c = new V3(7, 8, 9);

            ((a + b) + c).ShouldEqual(a + (b + c));
        }

        [Fact]
        private void SubtractionBasicVectors()
        {
            var a = new V3(5, 7, 9);
            var b = new V3(1, 2, 3);

            (a - b).ShouldEqual(new V3(4, 5, 6));
        }

        [Fact]
        private void SubtractionWithNegativeComponents()
        {
            var a = new V3(-1, 2, -3);
            var b = new V3(4, -5, 6);

            (a - b).ShouldEqual(new V3(-5, 7, -9));
        }

        [Fact]
        private void SubtractionWithZeroVector()
        {
            var v = new V3(1, 2, 3);

            (v - V3.zero).ShouldEqual(v);
            (V3.zero - v).ShouldEqual(-v);
        }

        [Fact]
        private void SubtractionIsNotCommutative()
        {
            var a = new V3(5, 7, 9);
            var b = new V3(1, 2, 3);

            (a - b).ShouldEqual(-(b - a));
        }

        [Fact]
        private void SubtractionOfIdenticalVectors()
        {
            var v = new V3(1.5, 2.7, -3.2);

            (v - v).ShouldBeZero();
        }

        [Fact]
        private void ComponentWiseMultiplication()
        {
            var a = new V3(2, 3, 4);
            var b = new V3(5, 6, 7);

            (a * b).ShouldEqual(new V3(10, 18, 28));
        }

        [Fact]
        private void ComponentWiseMultiplicationWithNegatives()
        {
            var a = new V3(-2, 3, -4);
            var b = new V3(5, -6, 7);

            (a * b).ShouldEqual(new V3(-10, -18, -28));
        }

        [Fact]
        private void ComponentWiseMultiplicationWithZero()
        {
            var v = new V3(1, 2, 3);

            (v * V3.zero).ShouldEqual(V3.zero);
        }

        [Fact]
        private void ComponentWiseMultiplicationWithOne()
        {
            var v = new V3(1, 2, 3);

            (v * V3.one).ShouldEqual(v);
        }

        [Fact]
        private void ComponentWiseDivision()
        {
            var a = new V3(10, 18, 28);
            var b = new V3(5, 6, 7);

            (a / b).ShouldEqual(new V3(2, 3, 4));
        }

        [Fact]
        private void ComponentWiseDivisionWithNegatives()
        {
            var a = new V3(-10, 18, -28);
            var b = new V3(5, -6, 7);

            (a / b).ShouldEqual(new V3(-2, -3, -4));
        }

        [Fact]
        private void ComponentWiseDivisionByZeroProducesInfinity()
        {
            var v = new V3(1, -2, 3);
            V3 result = v / V3.zero;

            result.x.ShouldBePositiveInfinity();
            result.y.ShouldBeNegativeInfinity();
            result.z.ShouldBePositiveInfinity();
        }

        [Fact]
        private void ComponentWiseDivisionZeroByZeroProducesNaN()
        {
            V3 result = V3.zero / V3.zero;

            result.x.ShouldBeNaN();
            result.y.ShouldBeNaN();
            result.z.ShouldBeNaN();
        }

        [Fact]
        private void NegationOperator()
        {
            var v = new V3(1, -2, 3);

            (-v).ShouldEqual(new V3(-1, 2, -3));
        }

        [Fact]
        private void NegationOfZeroVector()
        {
            (-V3.zero).ShouldEqual(V3.zero);
        }

        [Fact]
        private void DoubleNegation()
        {
            var v = new V3(1.5, -2.7, 3.2);

            (-(-v)).ShouldEqual(v);
        }

        [Fact]
        private void ScalarMultiplicationRight()
        {
            var v = new V3(2, 3, 4);

            (v * 5).ShouldEqual(new V3(10, 15, 20));
        }

        [Fact]
        private void ScalarMultiplicationLeft()
        {
            var v = new V3(2, 3, 4);

            (5 * v).ShouldEqual(new V3(10, 15, 20));
        }

        [Fact]
        private void ScalarMultiplicationByZero()
        {
            var v = new V3(1, 2, 3);

            (v * 0).ShouldEqual(V3.zero);
            (0 * v).ShouldEqual(V3.zero);
        }

        [Fact]
        private void ScalarMultiplicationByOne()
        {
            var v = new V3(1.5, 2.7, -3.2);

            (v * 1).ShouldEqual(v);
            (1 * v).ShouldEqual(v);
        }

        [Fact]
        private void ScalarMultiplicationByNegative()
        {
            var v = new V3(2, 3, 4);

            (v * -2).ShouldEqual(new V3(-4, -6, -8));
            (-2 * v).ShouldEqual(new V3(-4, -6, -8));
        }

        [Fact]
        private void ScalarMultiplicationIsCommutative()
        {
            var          v = new V3(1.5, -2.7, 3.2);
            const double S = 3.14;

            (v * S).ShouldEqual(S * v);
        }

        [Fact]
        private void ScalarDivisionRight()
        {
            var v = new V3(10, 15, 20);

            (v / 5).ShouldEqual(new V3(2, 3, 4));
        }

        [Fact]
        private void ScalarDivisionByOne()
        {
            var v = new V3(1.5, 2.7, -3.2);

            (v / 1).ShouldEqual(v);
        }

        [Fact]
        private void ScalarDivisionByNegative()
        {
            var v = new V3(10, 15, 20);

            (v / -5).ShouldEqual(new V3(-2, -3, -4));
        }

        [Fact]
        private void ScalarDivisionByZeroProducesInfinity()
        {
            var v = new V3(1, -2, 3);
            V3 result = v / 0;

            result.x.ShouldBePositiveInfinity();
            result.y.ShouldBeNegativeInfinity();
            result.z.ShouldBePositiveInfinity();
        }

        [Fact]
        private void ScalarDivisionLeft()
        {
            var v = new V3(2, 4, 8);

            (16 / v).ShouldEqual(new V3(8, 4, 2));
        }

        [Fact]
        private void ScalarDivisionLeftWithZeroNumerator()
        {
            var v = new V3(1, 2, 3);

            (0 / v).ShouldEqual(V3.zero);
        }

        [Fact]
        private void ScalarDivisionLeftWithZeroComponentProducesInfinity()
        {
            var v = new V3(2, 0, -4);
            V3 result = 8 / v;

            result.x.ShouldEqual(4);
            result.y.ShouldBePositiveInfinity();
            result.z.ShouldEqual(-2);
        }

        [Fact]
        private void EqualityOperatorIdenticalVectors()
        {
            var v1 = new V3(1.5, 2.7, -3.2);
            var v2 = new V3(1.5, 2.7, -3.2);

            (v1 == v2).ShouldBeTrue();
        }

        [Fact]
        private void EqualityOperatorWithinTolerance()
        {
            var v1 = new V3(1.0, 2.0, 3.0);
            var v2 = new V3(1.0 + 1e-16, 2.0 - 1e-16, 3.0 + 1e-16);

            (v1 == v2).ShouldBeTrue();
        }

        [Fact]
        private void EqualityOperatorDifferentVectors()
        {
            var v1 = new V3(1, 2, 3);
            var v2 = new V3(1, 2, 4);

            (v1 == v2).ShouldBeFalse();
        }

        [Fact]
        private void EqualityOperatorWithZeroVectors()
        {
            // ReSharper disable once EqualExpressionComparison
            (V3.zero == V3.zero).ShouldBeTrue();
            (V3.zero == new V3(0, 0, 0)).ShouldBeTrue();
        }

        [Fact]
        private void EqualityOperatorWithNaN()
        {
            var v1 = new V3(double.NaN, 2, 3);
            var v2 = new V3(double.NaN, 2, 3);

            (v1 == v2).ShouldBeFalse();
        }

        [Fact]
        private void InequalityOperatorDifferentVectors()
        {
            var v1 = new V3(1, 2, 3);
            var v2 = new V3(4, 5, 6);

            (v1 != v2).ShouldBeTrue();
        }

        [Fact]
        private void InequalityOperatorIdenticalVectors()
        {
            var v1 = new V3(1.5, 2.7, -3.2);
            var v2 = new V3(1.5, 2.7, -3.2);

            (v1 != v2).ShouldBeFalse();
        }

        [Fact]
        private void InequalityOperatorWithinTolerance()
        {
            var v1 = new V3(1.0, 2.0, 3.0);
            var v2 = new V3(1.0 + 1e-16, 2.0 - 1e-16, 3.0 + 1e-16);

            (v1 != v2).ShouldBeFalse();
        }

        [Fact]
        private void InequalityOperatorWithNaN()
        {
            var v1 = new V3(double.NaN, 2, 3);
            var v2 = new V3(double.NaN, 2, 3);

            (v1 != v2).ShouldBeTrue();
        }

        [Fact]
        private void OperatorPrecedenceAndChaining()
        {
            var v1 = new V3(1, 2, 3);
            var v2 = new V3(4, 5, 6);
            var v3 = new V3(2, 2, 2);

            V3 result = v1 + v2 * v3 - V3.one;
            result.ShouldEqual(new V3(8, 11, 14));
        }

        [Fact]
        private void DistributivePropertyScalarOverAddition()
        {
            var          v1 = new V3(1, 2, 3);
            var          v2 = new V3(4, 5, 6);
            const double S  = 3;

            (S * (v1 + v2)).ShouldEqual(S * v1 + S * v2);
        }

        [Fact]
        private void VectorIdentities()
        {
            var v = new V3(1.5, 2.7, -3.2);

            (v + (-v)).ShouldBeZero();
            (v - v).ShouldBeZero();
            (v * V3.zero).ShouldEqual(V3.zero);
            (v * V3.one).ShouldEqual(v);
            (v / V3.one).ShouldEqual(v);
        }

        [Fact]
        private void LargeValueOperations()
        {
            var v1 = new V3(1e150, 1e150, 1e150);
            var v2 = new V3(2e150, 3e150, 4e150);

            (v1 + v2).ShouldEqual(new V3(3e150, 4e150, 5e150));
            (v2 - v1).ShouldEqual(new V3(1e150, 2e150, 3e150));
            (v1 * 2).ShouldEqual(new V3(2e150, 2e150, 2e150));
            (v1 / 1e150).ShouldEqual(V3.one);
        }

        [Fact]
        private void SmallValueOperations()
        {
            var v1 = new V3(1e-150, 2e-150, 3e-150);
            var v2 = new V3(4e-150, 5e-150, 6e-150);

            (v1 + v2).ShouldEqual(new V3(5e-150, 7e-150, 9e-150));
            (v2 - v1).ShouldEqual(new V3(3e-150, 3e-150, 3e-150));
            (v1 * 2).ShouldEqual(new V3(2e-150, 4e-150, 6e-150));
        }

        [Fact]
        private void MixedScaleOperations()
        {
            var large = new V3(1e100, 0, 0);
            var small = new V3(1e-100, 0, 0);

            (large + small).ShouldEqual(large);
            (large - small).ShouldEqual(large);
        }
    }
}
