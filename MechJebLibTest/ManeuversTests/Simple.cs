using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.ManeuversTests
{
    public class Simple
    {
        [Fact]
        public void DeltaVToCircularizeTest()
        {
            const int NTRIALS = 50;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var r = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);

                double rscale = random.NextDouble() * 1.5e8 + 1;
                double vscale = random.NextDouble() * 3e4 + 1;
                r *= rscale;
                v *= vscale;
                double mu = rscale * vscale * vscale;

                V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToCircularize(mu, r, v);

                Astro.EccFromStateVectors(mu, r, v + dv).ShouldEqual(0, 1e-14);
                Astro.PeriapsisFromStateVectors(mu, r, v + dv).ShouldEqual(r.magnitude, 1e-7);
                Astro.ApoapsisFromStateVectors(mu, r, v + dv).ShouldEqual(r.magnitude, 1e-7);
            }
        }

        [Fact]
        public void DeltaVToEllipticizeTest()
        {
            const int NTRIALS = 50;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var r = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double newPeR = random.NextDouble() * r.magnitude;
                double newApR = random.NextDouble() * 1e9 + r.magnitude;

                double rscale = random.NextDouble() * 1.5e8 + 1;
                double vscale = random.NextDouble() * 3e4 + 1;
                r      *= rscale;
                v      *= vscale;
                newPeR *= rscale;
                newApR *= rscale;
                double mu = rscale * vscale * vscale;

                V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToEllipticize(mu, r, v, newPeR, newApR);

                Astro.PeriapsisFromStateVectors(mu, r, v + dv).ShouldEqual(newPeR, 1e-4);
                Astro.ApoapsisFromStateVectors(mu, r, v + dv).ShouldEqual(newApR, 1e-4);
            }
        }

        [Fact]
        public void DeltaVToChangeInclinationTest()
        {
            const int NTRIALS = 50;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var r = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);

                double rscale = random.NextDouble() * 1.5e8 + 1;
                double vscale = random.NextDouble() * 3e4 + 1;
                r *= rscale;
                v *= vscale;

                double plusOrMinusOne = random.Next(0, 2) * 2 - 1;
                double lat = Astro.LatitudeFromBCI(r);
                double newInc = Abs(lat) + random.NextDouble() * (PI - 2 * Abs(lat));
                newInc *= plusOrMinusOne;

                V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeInclination(r, v, newInc);

                Astro.IncFromStateVectors(r, v + dv).ShouldEqual(Abs(newInc), 1e-4);
            }
        }

        [Fact]
        public void DeltaVToChangeInclinationTest1()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            double v185 = Astro.CircularVelocity(mu, r185);

            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeInclination(r0, v0, 0);
            Assert.Equal(V3.zero, dv);

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeInclination(r0, v0, Deg2Rad(90));
            Assert.Equal(0, (v0 + dv).x, 9);
            Assert.Equal(0, (v0 + dv).y, 9);
            Assert.Equal(v185, (v0 + dv).z, 9);
        }

        [Fact]
        public void DeltaVToChangeFPATest1()
        {
            const double mu = 3.986004418e+14;
            const double rearth = 6.371e+6;
            const double r185 = rearth + 185e+3;
            double v185 = Astro.CircularVelocity(mu, r185);

            var r0 = new V3(r185, 0, 0);
            var v0 = new V3(0, v185, 0);

            V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(V3.zero, dv);

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, Deg2Rad(90));
            Assert.Equal(v185, dv.x, 9);
            Assert.Equal(-v185, dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, Deg2Rad(-90));
            Assert.Equal(-v185, dv.x, 9);
            Assert.Equal(-v185, dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            r0 = new V3(r185, 0, 0);
            v0 = new V3(v185 / Sqrt(2), v185 / Sqrt(2), 0);

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(-v185 / Sqrt(2), dv.x, 9);
            Assert.Equal(v185 - v185 / Sqrt(2), dv.y, 9);
            Assert.Equal(0, dv.z, 9);

            r0 = new V3(r185, 0, 0);
            v0 = new V3(v185 / Sqrt(2), 0, v185 / Sqrt(2));

            dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeFPA(r0, v0, 0);
            Assert.Equal(-v185 / Sqrt(2), dv.x, 9);
            Assert.Equal(0, dv.y, 9);
            Assert.Equal(v185 - v185 / Sqrt(2), dv.z, 9);
        }
    }
}
