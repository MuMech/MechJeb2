/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V4Tests
{
    public class OperatorTests
    {
        [Fact]
        private void Addition()
        {
            (new V4(1, 2, 3, 4) + new V4(3, -5, 0.5, -4)).ShouldEqual(new V4(4, -3, 3.5, 0));
        }

        [Fact]
        private void Subtraction()
        {
            (new V4(1, 2, 3, 4) - new V4(3, -5, 0.5, -4)).ShouldEqual(new V4(-2, 7, 2.5, 8));
        }

        [Fact]
        private void ComponentwiseMultiplication()
        {
            (new V4(2, 3, -1, 0.5) * new V4(4, -5, 6, 8)).ShouldEqual(new V4(8, -15, -6, 4));
        }

        [Fact]
        private void ComponentwiseDivision()
        {
            (new V4(8, -15, -6, 4) / new V4(4, -5, 6, 8)).ShouldEqual(new V4(2, 3, -1, 0.5));
        }

        [Fact]
        private void UnaryNegation()
        {
            (-new V4(1, -2, 3, -4)).ShouldEqual(new V4(-1, 2, -3, 4));
        }

        [Fact]
        private void ScalarMultiplicationBothSides()
        {
            var v = new V4(1.5, -2, 3, 0.25);

            (v * 2).ShouldEqual(new V4(3, -4, 6, 0.5));
            (2 * v).ShouldEqual(new V4(3, -4, 6, 0.5));
        }

        [Fact]
        private void ScalarDivision()
        {
            (new V4(3, -4, 6, 0.5) / 2).ShouldEqual(new V4(1.5, -2, 3, 0.25));
        }

        [Fact]
        private void ScalarDividedByVector()
        {
            (12 / new V4(3, -4, 6, 0.5)).ShouldEqual(new V4(4, -3, 2, 24));
        }

        [Fact]
        private void DivisionByZeroGivesInfinity()
        {
            V4 v = new V4(1, -1, 1, -1) / 0.0;

            v.x.ShouldBePositiveInfinity();
            v.y.ShouldBeNegativeInfinity();
            v.z.ShouldBePositiveInfinity();
            v.w.ShouldBeNegativeInfinity();
        }

        [Fact]
        private void EqualityOperators()
        {
            (new V4(1, 2, 3, 4) == new V4(1, 2, 3, 4)).ShouldBeTrue();
            (new V4(1, 2, 3, 4) == new V4(1, 2, 3, 5)).ShouldBeFalse();
            (new V4(1, 2, 3, 4) != new V4(0, 2, 3, 4)).ShouldBeTrue();
            (new V4(1, 2, 3, 4) != new V4(1, 2, 3, 4)).ShouldBeFalse();
        }

        [Fact]
        private void StaticComponentwiseHelpers()
        {
            var a = new V4(-4, 9, 16, -1);
            var b = new V4(2, -3, 4, 1);

            V4.Scale(a, b).ShouldEqual(new V4(-8, -27, 64, -1));
            V4.Divide(a, b).ShouldEqual(new V4(-2, -3, 4, -1));
            V4.Abs(a).ShouldEqual(new V4(4, 9, 16, 1));
            V4.Sign(a).ShouldEqual(new V4(-1, 1, 1, -1));
            V4.Sign(V4.zero).ShouldEqual(V4.zero);
            V4.Sqrt(new V4(4, 9, 16, 1)).ShouldEqual(new V4(2, 3, 4, 1));
            V4.Max(a, b).ShouldEqual(new V4(2, 9, 16, 1));
            V4.Min(a, b).ShouldEqual(new V4(-4, -3, 4, -1));
        }

        [Fact]
        private void InstanceScale()
        {
            var v = new V4(2, 3, 4, 5);
            v.Scale(new V4(-1, 4, 0.5, 0));

            v.ShouldEqual(new V4(-2, 12, 2, 0));
        }
    }
}
