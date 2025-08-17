/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class ComponentOperationsTests
    {
        [Fact]
        private void ScaleBasicVectors()
        {
            var a = new V3(2, 3, 4);
            var b = new V3(5, 6, 7);
            V3.Scale(a, b).ShouldEqual(new V3(10, 18, 28));
        }

        [Fact]
        private void ScaleWithNegativeComponents()
        {
            var a = new V3(2, -3, 4);
            var b = new V3(-5, 6, -7);
            V3.Scale(a, b).ShouldEqual(new V3(-10, -18, -28));
        }

        [Fact]
        private void ScaleWithZeros()
        {
            var a = new V3(2, 3, 4);
            var b = new V3(0, 1, 0);
            V3.Scale(a, b).ShouldEqual(new V3(0, 3, 0));

            V3.Scale(V3.zero, new V3(5, 6, 7)).ShouldEqual(V3.zero);
        }

        [Fact]
        private void ScaleWithOnes()
        {
            var v = new V3(2, 3, 4);
            V3.Scale(v, V3.one).ShouldEqual(v);
        }

        [Fact]
        private void ScaleMethodInPlace()
        {
            var v = new V3(2, 3, 4);
            var scale = new V3(5, 6, 7);
            v.Scale(scale);
            v.ShouldEqual(new V3(10, 18, 28));
        }

        [Fact]
        private void DivideBasicVectors()
        {
            var a = new V3(10, 18, 28);
            var b = new V3(5, 6, 7);
            V3.Divide(a, b).ShouldEqual(new V3(2, 3, 4));
        }

        [Fact]
        private void DivideWithNegativeComponents()
        {
            var a = new V3(-10, 18, -28);
            var b = new V3(5, -6, 7);
            V3.Divide(a, b).ShouldEqual(new V3(-2, -3, -4));
        }

        [Fact]
        private void DivideByOnes()
        {
            var v = new V3(2, 3, 4);
            V3.Divide(v, V3.one).ShouldEqual(v);
        }

        [Fact]
        private void DivideProducesInfinity()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(0, 0, 0);
            var result = V3.Divide(a, b);
            result.x.ShouldBePositiveInfinity();
            result.y.ShouldBePositiveInfinity();
            result.z.ShouldBePositiveInfinity();
        }

        [Fact]
        private void DivideZeroByZero()
        {
            var result = V3.Divide(V3.zero, V3.zero);
            result.x.ShouldBeNaN();
            result.y.ShouldBeNaN();
            result.z.ShouldBeNaN();
        }

        [Fact]
        private void AbsPositiveComponents()
        {
            var v = new V3(1, 2, 3);
            V3.Abs(v).ShouldEqual(v);
        }

        [Fact]
        private void AbsNegativeComponents()
        {
            var v = new V3(-1, -2, -3);
            V3.Abs(v).ShouldEqual(new V3(1, 2, 3));
        }

        [Fact]
        private void AbsMixedComponents()
        {
            var v = new V3(-1, 2, -3);
            V3.Abs(v).ShouldEqual(new V3(1, 2, 3));
        }

        [Fact]
        private void AbsZeroVector()
        {
            V3.Abs(V3.zero).ShouldEqual(V3.zero);
        }

        [Fact]
        private void AbsSpecialValues()
        {
            var v = new V3(double.NegativeInfinity, double.NaN, -0.0);
            var result = V3.Abs(v);
            result.x.ShouldBePositiveInfinity();
            result.y.ShouldBeNaN();
            result.z.ShouldEqual(0.0);
        }

        [Fact]
        private void SignPositiveComponents()
        {
            var v = new V3(5, 10, 15);
            V3.Sign(v).ShouldEqual(V3.one);
        }

        [Fact]
        private void SignNegativeComponents()
        {
            var v = new V3(-5, -10, -15);
            V3.Sign(v).ShouldEqual(new V3(-1, -1, -1));
        }

        [Fact]
        private void SignMixedComponents()
        {
            var v = new V3(-5, 10, -15);
            V3.Sign(v).ShouldEqual(new V3(-1, 1, -1));
        }

        [Fact]
        private void SignZeroComponents()
        {
            var v = new V3(0, 5, -3);
            V3.Sign(v).ShouldEqual(new V3(0, 1, -1));

            V3.Sign(V3.zero).ShouldEqual(V3.zero);
        }

        [Fact]
        private void SqrtPositiveComponents()
        {
            var v = new V3(4, 9, 16);
            V3.Sqrt(v).ShouldEqual(new V3(2, 3, 4));
        }

        [Fact]
        private void SqrtZeroVector()
        {
            V3.Sqrt(V3.zero).ShouldEqual(V3.zero);
        }

        [Fact]
        private void SqrtOneVector()
        {
            V3.Sqrt(V3.one).ShouldEqual(V3.one);
        }

        [Fact]
        private void SqrtLargeValues()
        {
            var v = new V3(1e100, 4e200, 9e150);
            var result = V3.Sqrt(v);
            result.x.ShouldEqual(1e50);
            result.y.ShouldEqual(2e100);
            result.z.ShouldEqual(3e75);
        }

        [Fact]
        private void SqrtNegativeProducesNaN()
        {
            var v = new V3(-1, -4, -9);
            var result = V3.Sqrt(v);
            result.x.ShouldBeNaN();
            result.y.ShouldBeNaN();
            result.z.ShouldBeNaN();
        }

        [Fact]
        private void MaxBasicVectors()
        {
            var a = new V3(1, 5, 3);
            var b = new V3(4, 2, 6);
            V3.Max(a, b).ShouldEqual(new V3(4, 5, 6));
        }

        [Fact]
        private void MaxWithNegativeComponents()
        {
            var a = new V3(-1, -5, 3);
            var b = new V3(-4, -2, -6);
            V3.Max(a, b).ShouldEqual(new V3(-1, -2, 3));
        }

        [Fact]
        private void MaxIdenticalVectors()
        {
            var v = new V3(1, 2, 3);
            V3.Max(v, v).ShouldEqual(v);
        }

        [Fact]
        private void MaxWithZero()
        {
            var v = new V3(-1, 0, 1);
            V3.Max(v, V3.zero).ShouldEqual(new V3(0, 0, 1));
        }

        [Fact]
        private void MaxWithInfinity()
        {
            var a = new V3(1, double.NegativeInfinity, 3);
            var b = new V3(double.PositiveInfinity, 2, double.NegativeInfinity);
            V3.Max(a, b).ShouldEqual(new V3(double.PositiveInfinity, 2, 3));
        }

        [Fact]
        private void MaxWithNaN()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(double.NaN, 5, 6);
            var result = V3.Max(a, b);
            result.x.ShouldBeNaN();
            result.y.ShouldEqual(5);
            result.z.ShouldEqual(6);
        }

        [Fact]
        private void MinBasicVectors()
        {
            var a = new V3(1, 5, 3);
            var b = new V3(4, 2, 6);
            V3.Min(a, b).ShouldEqual(new V3(1, 2, 3));
        }

        [Fact]
        private void MinWithNegativeComponents()
        {
            var a = new V3(-1, -5, 3);
            var b = new V3(-4, -2, -6);
            V3.Min(a, b).ShouldEqual(new V3(-4, -5, -6));
        }

        [Fact]
        private void MinIdenticalVectors()
        {
            var v = new V3(1, 2, 3);
            V3.Min(v, v).ShouldEqual(v);
        }

        [Fact]
        private void MinWithZero()
        {
            var v = new V3(-1, 0, 1);
            V3.Min(v, V3.zero).ShouldEqual(new V3(-1, 0, 0));
        }

        [Fact]
        private void MinWithInfinity()
        {
            var a = new V3(1, double.PositiveInfinity, 3);
            var b = new V3(double.NegativeInfinity, 2, double.PositiveInfinity);
            V3.Min(a, b).ShouldEqual(new V3(double.NegativeInfinity, 2, 3));
        }

        [Fact]
        private void MinWithNaN()
        {
            var a = new V3(1, 2, 3);
            var b = new V3(double.NaN, 5, 6);
            var result = V3.Min(a, b);
            result.x.ShouldBeNaN();
            result.y.ShouldEqual(2);
            result.z.ShouldEqual(3);
        }

        [Fact]
        private void ComponentWiseOperationsPreserveStructure()
        {
            var v = new V3(2, 3, 4);
            var identity = V3.Divide(V3.Scale(v, v), v);
            identity.ShouldEqual(v);
        }

        [Fact]
        private void MaxMinRelationship()
        {
            var a = new V3(1, 5, 3);
            var b = new V3(4, 2, 6);
            var max = V3.Max(a, b);
            var min = V3.Min(a, b);

            (max.x >= min.x).ShouldBeTrue();
            (max.y >= min.y).ShouldBeTrue();
            (max.z >= min.z).ShouldBeTrue();

            (max.x >= a.x && max.x >= b.x).ShouldBeTrue();
            (min.x <= a.x && min.x <= b.x).ShouldBeTrue();
        }
    }
}
