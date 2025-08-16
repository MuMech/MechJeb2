using System;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Structs
{
    public class V3Tests
    {
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
