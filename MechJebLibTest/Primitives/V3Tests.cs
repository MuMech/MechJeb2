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
