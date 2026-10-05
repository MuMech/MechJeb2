/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V2Tests
{
    public class OperatorTests
    {
        [Fact]
        private void Addition()
        {
            (new V2(1, 2) + new V2(3, -5)).ShouldEqual(new V2(4, -3));
        }

        [Fact]
        private void Subtraction()
        {
            (new V2(1, 2) - new V2(3, -5)).ShouldEqual(new V2(-2, 7));
        }

        [Fact]
        private void ComponentwiseMultiplication()
        {
            (new V2(2, 3) * new V2(4, -5)).ShouldEqual(new V2(8, -15));
        }

        [Fact]
        private void ComponentwiseDivision()
        {
            (new V2(8, -15) / new V2(4, -5)).ShouldEqual(new V2(2, 3));
        }

        [Fact]
        private void UnaryNegation()
        {
            (-new V2(1, -2)).ShouldEqual(new V2(-1, 2));
        }

        [Fact]
        private void ScalarMultiplicationBothSides()
        {
            var v = new V2(1.5, -2);

            (v * 2).ShouldEqual(new V2(3, -4));
            (2 * v).ShouldEqual(new V2(3, -4));
        }

        [Fact]
        private void ScalarDivision()
        {
            (new V2(3, -4) / 2).ShouldEqual(new V2(1.5, -2));
        }

        [Fact]
        private void ScalarDividedByVector()
        {
            (12 / new V2(3, -4)).ShouldEqual(new V2(4, -3));
        }

        [Fact]
        private void DivisionByZeroGivesInfinity()
        {
            V2 v = new V2(1, -1) / 0.0;

            v.x.ShouldBePositiveInfinity();
            v.y.ShouldBeNegativeInfinity();
        }

        [Fact]
        private void EqualityOperators()
        {
            (new V2(1, 2) == new V2(1, 2)).ShouldBeTrue();
            (new V2(1, 2) == new V2(1, 3)).ShouldBeFalse();
            (new V2(1, 2) != new V2(2, 2)).ShouldBeTrue();
            (new V2(1, 2) != new V2(1, 2)).ShouldBeFalse();
        }

        [Fact]
        private void StaticComponentwiseHelpers()
        {
            var a = new V2(-4, 9);
            var b = new V2(2, -3);

            V2.Scale(a, b).ShouldEqual(new V2(-8, -27));
            V2.Divide(a, b).ShouldEqual(new V2(-2, -3));
            V2.Abs(a).ShouldEqual(new V2(4, 9));
            V2.Sign(a).ShouldEqual(new V2(-1, 1));
            V2.Sign(V2.zero).ShouldEqual(V2.zero);
            V2.Sqrt(new V2(4, 9)).ShouldEqual(new V2(2, 3));
            V2.Max(a, b).ShouldEqual(new V2(2, 9));
            V2.Min(a, b).ShouldEqual(new V2(-4, -3));
        }

        [Fact]
        private void InstanceScale()
        {
            var v = new V2(2, 3);
            v.Scale(new V2(-1, 4));

            v.ShouldEqual(new V2(-2, 12));
        }
    }
}
