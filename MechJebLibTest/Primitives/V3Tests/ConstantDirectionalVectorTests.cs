/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives.V3Tests
{
    public class ConstantDirectionalVectorTests
    {
        [Fact]
        private void ZeroVector()
        {
            V3 v = V3.zero;
            v.x.ShouldEqual(0.0);
            v.y.ShouldEqual(0.0);
            v.z.ShouldEqual(0.0);
        }

        [Fact]
        private void OneVector()
        {
            V3 v = V3.one;
            v.x.ShouldEqual(1.0);
            v.y.ShouldEqual(1.0);
            v.z.ShouldEqual(1.0);
        }

        [Fact]
        private void PositiveInfinityVector()
        {
            V3 v = V3.positiveinfinity;
            Assert.Equal(double.PositiveInfinity, v.x);
            Assert.Equal(double.PositiveInfinity, v.y);
            Assert.Equal(double.PositiveInfinity, v.z);
        }

        [Fact]
        private void NegativeInfinityVector()
        {
            V3 v = V3.negativeinfinity;
            Assert.Equal(double.NegativeInfinity, v.x);
            Assert.Equal(double.NegativeInfinity, v.y);
            Assert.Equal(double.NegativeInfinity, v.z);
        }

        [Fact]
        private void MaxValueVector()
        {
            V3 v = V3.maxvalue;
            Assert.Equal(double.MaxValue, v.x);
            Assert.Equal(double.MaxValue, v.y);
            Assert.Equal(double.MaxValue, v.z);
        }

        [Fact]
        private void MinValueVector()
        {
            V3 v = V3.minvalue;
            Assert.Equal(double.MinValue, v.x);
            Assert.Equal(double.MinValue, v.y);
            Assert.Equal(double.MinValue, v.z);
        }

        [Fact]
        private void NanVector()
        {
            V3 v = V3.nan;
            Assert.True(double.IsNaN(v.x));
            Assert.True(double.IsNaN(v.y));
            Assert.True(double.IsNaN(v.z));
        }

        [Fact]
        private void DirectionalVectorsNorthEastDown()
        {
            V3.down.ShouldEqual(new V3(0.0, 0.0, 1.0));
            V3.up.ShouldEqual(new V3(0.0, 0.0, -1.0));
            V3.left.ShouldEqual(new V3(0.0, -1.0, 0.0));
            V3.right.ShouldEqual(new V3(0.0, 1.0, 0.0));
            V3.forward.ShouldEqual(new V3(1.0, 0.0, 0.0));
            V3.back.ShouldEqual(new V3(-1.0, 0.0, 0.0));
        }

        [Fact]
        private void NorthPoleVector() => V3.northpole.ShouldEqual(new V3(0, 0, 1));

        [Fact]
        private void AxisVectors()
        {
            V3.xaxis.ShouldEqual(new V3(1, 0, 0));
            V3.yaxis.ShouldEqual(new V3(0, 1, 0));
            V3.zaxis.ShouldEqual(new V3(0, 0, 1));
        }

        [Fact]
        private void DirectionalVectorsAreUnitVectors()
        {
            V3.down.magnitude.ShouldEqual(1.0);
            V3.up.magnitude.ShouldEqual(1.0);
            V3.left.magnitude.ShouldEqual(1.0);
            V3.right.magnitude.ShouldEqual(1.0);
            V3.forward.magnitude.ShouldEqual(1.0);
            V3.back.magnitude.ShouldEqual(1.0);
            V3.xaxis.magnitude.ShouldEqual(1.0);
            V3.yaxis.magnitude.ShouldEqual(1.0);
            V3.zaxis.magnitude.ShouldEqual(1.0);
            V3.northpole.magnitude.ShouldEqual(1.0);
        }

        [Fact]
        private void OppositeDirectionsNegateEachOther()
        {
            (V3.up + V3.down).ShouldBeZero();
            (V3.left + V3.right).ShouldBeZero();
            (V3.forward + V3.back).ShouldBeZero();
        }

        [Fact]
        private void AxisVectorsAreOrthogonal()
        {
            V3.Dot(V3.xaxis, V3.yaxis).ShouldBeZero();
            V3.Dot(V3.xaxis, V3.zaxis).ShouldBeZero();
            V3.Dot(V3.yaxis, V3.zaxis).ShouldBeZero();
        }

        [Fact]
        private void DirectionalVectorsFormRightHandedSystem()
        {
            V3.Cross(V3.forward, V3.right).ShouldEqual(V3.down);
            V3.Cross(V3.right, V3.down).ShouldEqual(V3.forward);
            V3.Cross(V3.down, V3.forward).ShouldEqual(V3.right);
        }

        [Fact]
        private void StaticAccessorsAreImmutable()
        {
            // ReSharper disable once NotAccessedVariable
            V3 v1 = V3.zero;
            v1.x = 10;
            V3 v2 = V3.zero;
            v2.ShouldEqual(new V3(0, 0, 0));

            V3 v3 = V3.one;
            v3.Set(5, 5, 5);
            V3 v4 = V3.one;
            v4.ShouldEqual(new V3(1, 1, 1));
        }
    }
}
