/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using Xunit;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.ManeuversTests
{
    public class Simple
    {
        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i < 50; i++)
                yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void DeltaVToCircularizeTest(int seed)
        {
            var random = new Random(seed);

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

        // the burn is along the velocity and puts the apoapsis where asked, from 1 to 1e4 radii out, and does nothing below r
        [Theory]
        [MemberData(nameof(Seeds))]
        public void DeltaVToChangeApoapsisProgradeTest(int seed)
        {
            var random = new Random(seed);

            var r = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var v = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double newApR = r.magnitude * Pow(10, 4 * random.NextDouble());

            double rscale = random.NextDouble() * 1.5e8 + 1;
            double vscale = random.NextDouble() * 3e4 + 1;
            r *= rscale;
            v *= vscale;
            newApR *= rscale;
            double mu = rscale * vscale * vscale;

            V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToChangeApoapsisPrograde(mu, r, v, newApR);

            Astro.ApoapsisFromStateVectors(mu, r, v + dv).ShouldEqual(newApR, 1e-9);
            Assert.True(V3.Cross(dv, v).magnitude <= 1e-12 * dv.magnitude * v.magnitude);
            Assert.True(V3.Dot(v + dv, v) > 0);

            MechJebLib.Maneuvers.Simple.DeltaVToChangeApoapsisPrograde(mu, r, v, 0.5 * r.magnitude).ShouldEqual(V3.zero, 0);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void DeltaVToEllipticizeTest(int seed)
        {
            var random = new Random(seed);

            var r = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var v = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double newPeR = random.NextDouble() * r.magnitude;
            double newApR = random.NextDouble() * 1e9 + r.magnitude;

            double rscale = random.NextDouble() * 1.5e8 + 1;
            double vscale = random.NextDouble() * 3e4 + 1;
            r *= rscale;
            v *= vscale;
            newPeR *= rscale;
            newApR *= rscale;
            double mu = rscale * vscale * vscale;

            V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToEllipticize(mu, r, v, newPeR, newApR);

            Astro.PeriapsisFromStateVectors(mu, r, v + dv).ShouldEqual(newPeR, 1e-4);
            Astro.ApoapsisFromStateVectors(mu, r, v + dv).ShouldEqual(newApR, 1e-4);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void DeltaVToChangeInclinationTest(int seed)
        {
            var random = new Random(seed);

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

        // after the coast the orbit is circular at the burn point, which is where the vessel is then, not where it started
        [Theory]
        [MemberData(nameof(Seeds))]
        public void DeltaVToCircularizeAfterTimeTest(int seed)
        {
            var random = new Random(seed);

            double ecc = 0.8 * random.NextDouble();
            (V3 r, V3 v) = Astro.StateVectorsFromKeplerian(1.0, 1.0, ecc, PI * random.NextDouble(), TAU * random.NextDouble(),
                TAU * random.NextDouble(), TAU * random.NextDouble());
            double dt = 10 * random.NextDouble();

            V3 dv = MechJebLib.Maneuvers.Simple.DeltaVToCircularizeAfterTime(1.0, r, v, dt);

            (V3 r1, V3 v1) = Shepperd.Solve(1.0, dt, r, v);
            Astro.EccFromStateVectors(1.0, r1, v1 + dv).ShouldEqual(0, 1e-9);
            (v1 + dv).magnitude.ShouldEqual(Sqrt(1.0 / r1.magnitude), 1e-9);
        }
    }
}
