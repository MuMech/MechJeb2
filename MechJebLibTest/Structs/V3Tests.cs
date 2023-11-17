using System;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Structs
{
    public class V3Tests
    {
        [Fact]
        private void ComponentTests()
        {
            Assert.Equal(1.0, new V3(1, 2, 3).min_magnitude);
            Assert.Equal(1.0, new V3(2, 1, 3).min_magnitude);
            Assert.Equal(1.0, new V3(3, 2, 1).min_magnitude);
            Assert.Equal(1.0, new V3(2, 3, 1).min_magnitude);
            Assert.Equal(1.0, new V3(1, 3, 2).min_magnitude);
            Assert.Equal(1.0, new V3(3, 1, 2).min_magnitude);
            Assert.Equal(1.0, new V3(-1, -2, -3).min_magnitude);
            Assert.Equal(1.0, new V3(-2, -1, -3).min_magnitude);
            Assert.Equal(1.0, new V3(-3, -2, -1).min_magnitude);
            Assert.Equal(1.0, new V3(-2, -3, -1).min_magnitude);
            Assert.Equal(1.0, new V3(-1, -3, -2).min_magnitude);
            Assert.Equal(1.0, new V3(-3, -1, -2).min_magnitude);
            Assert.Equal(3.0, new V3(1, 2, 3).max_magnitude);
            Assert.Equal(3.0, new V3(2, 1, 3).max_magnitude);
            Assert.Equal(3.0, new V3(3, 2, 1).max_magnitude);
            Assert.Equal(3.0, new V3(2, 3, 1).max_magnitude);
            Assert.Equal(3.0, new V3(1, 3, 2).max_magnitude);
            Assert.Equal(3.0, new V3(3, 1, 2).max_magnitude);
            Assert.Equal(3.0, new V3(-1, -2, -3).max_magnitude);
            Assert.Equal(3.0, new V3(-2, -1, -3).max_magnitude);
            Assert.Equal(3.0, new V3(-3, -2, -1).max_magnitude);
            Assert.Equal(3.0, new V3(-2, -3, -1).max_magnitude);
            Assert.Equal(3.0, new V3(-1, -3, -2).max_magnitude);
            Assert.Equal(3.0, new V3(-3, -1, -2).max_magnitude);
            Assert.Equal(0, new V3(1, 2, 3).min_magnitude_index);
            Assert.Equal(1, new V3(2, 1, 3).min_magnitude_index);
            Assert.Equal(2, new V3(3, 2, 1).min_magnitude_index);
            Assert.Equal(2, new V3(2, 3, 1).min_magnitude_index);
            Assert.Equal(0, new V3(1, 3, 2).min_magnitude_index);
            Assert.Equal(1, new V3(3, 1, 2).min_magnitude_index);
            Assert.Equal(0, new V3(-1, -2, -3).min_magnitude_index);
            Assert.Equal(1, new V3(-2, -1, -3).min_magnitude_index);
            Assert.Equal(2, new V3(-3, -2, -1).min_magnitude_index);
            Assert.Equal(2, new V3(-2, -3, -1).min_magnitude_index);
            Assert.Equal(0, new V3(-1, -3, -2).min_magnitude_index);
            Assert.Equal(1, new V3(-3, -1, -2).min_magnitude_index);
            Assert.Equal(2, new V3(1, 2, 3).max_magnitude_index);
            Assert.Equal(2, new V3(2, 1, 3).max_magnitude_index);
            Assert.Equal(0, new V3(3, 2, 1).max_magnitude_index);
            Assert.Equal(1, new V3(2, 3, 1).max_magnitude_index);
            Assert.Equal(1, new V3(1, 3, 2).max_magnitude_index);
            Assert.Equal(0, new V3(3, 1, 2).max_magnitude_index);
            Assert.Equal(2, new V3(-1, -2, -3).max_magnitude_index);
            Assert.Equal(2, new V3(-2, -1, -3).max_magnitude_index);
            Assert.Equal(0, new V3(-3, -2, -1).max_magnitude_index);
            Assert.Equal(1, new V3(-2, -3, -1).max_magnitude_index);
            Assert.Equal(1, new V3(-1, -3, -2).max_magnitude_index);
            Assert.Equal(0, new V3(-3, -1, -2).max_magnitude_index);
        }

        [Fact]
        private void MagnitudeLargeTest()
        {
            double large = 1e+190;

            var largeV = new V3(large, large, large);
            Assert.Equal(Sqrt(3) * large, largeV.magnitude);
        }

        [Fact]
        private void MagnitudeSmallTest()
        {
            double small = 1e-190;
            var smallV = new V3(small, small, small);

            Assert.Equal(Sqrt(3) * small, smallV.magnitude);
        }

        [Fact]
        private void NormalizeLargeTest()
        {
            double large = 1e+190;
            var largeV = new V3(large, large, large);

            Assert.Equal(1.0 / Sqrt(3), largeV.normalized[0]);
            Assert.Equal(1.0 / Sqrt(3), largeV.normalized[1]);
            Assert.Equal(1.0 / Sqrt(3), largeV.normalized[2]);
        }

        [Fact]
        private void NormalizeSmallTest()
        {
            double small = 1e-190;
            var smallV = new V3(small, small, small);

            Assert.Equal(1.0 / Sqrt(3), smallV.normalized[0]);
            Assert.Equal(1.0 / Sqrt(3), smallV.normalized[1]);
            Assert.Equal(1.0 / Sqrt(3), smallV.normalized[2]);
        }

        [Fact]
        private void ZeroTests()
        {
            Assert.Equal(0, V3.zero.magnitude);
            Assert.Equal(0, V3.zero.sqrMagnitude);
            Assert.Equal(V3.zero, V3.zero.normalized);
        }

        [Fact]
        private void RandomSphericalConversions()
        {
            const int NTRIALS = 5000;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var v = new V3(40 * random.NextDouble() - 20, 40 * random.NextDouble() - 20, 40 * random.NextDouble() - 20);

                v.cart2sph.sph2cart.ShouldEqual(v, 1e-13);
            }
        }

        [Fact]
        private void CartesianToSpherical()
        {
            new V3(1, 1, 1).cart2sph.ShouldEqual(new V3(Sqrt(3), Acos(1 / Sqrt(3)), PI / 4));
            new V3(1, 1, -1).cart2sph.ShouldEqual(new V3(Sqrt(3), Acos(-1 / Sqrt(3)), PI / 4));
            new V3(1, -1, 1).cart2sph.ShouldEqual(new V3(Sqrt(3), Acos(1 / Sqrt(3)), 7 * PI / 4));
            new V3(1, -1, -1).cart2sph.ShouldEqual(new V3(Sqrt(3), Acos(-1 / Sqrt(3)), 7 * PI / 4));
            new V3(-1, 1, 1).cart2sph.ShouldEqual(new V3(Sqrt(3), Acos(1 / Sqrt(3)), 3 * PI / 4));
            new V3(-1, 1, -1).cart2sph.ShouldEqual(new V3(Sqrt(3), Acos(-1 / Sqrt(3)), 3 * PI / 4));
            new V3(-1, -1, 1).cart2sph.ShouldEqual(new V3(Sqrt(3), Acos(1 / Sqrt(3)), 5 * PI / 4));
            new V3(-1, -1, -1).cart2sph.ShouldEqual(new V3(Sqrt(3), Acos(-1 / Sqrt(3)), 5 * PI / 4));
        }
    }
}
