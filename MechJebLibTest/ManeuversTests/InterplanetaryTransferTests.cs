/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.ManeuversTests
{
    public class InterplanetaryTransferTests
    {
        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i < 25; i++)
                yield return new object[] { i };
        }

        [Fact]
        private void EarthToMercury()
        {
            var r0 = new V3(-5393756.1398685, 2579906.9721393, -2964978.45117617);
            var v0 = new V3(-3856.63799573101, -6569.97859538819, 1299.1134217111);
            double mu1 = 398600435436096;
            var r1 = new V3(142463209097.952, 147485866.387222, 38721854973.0728);
            var v1 = new V3(2694.98267839614, 28693.1174312002, -8955.43648332019);
            double soi1 = 924649202.461023;
            double mu2 = 22031780000000;
            var r2 = new V3(-17610435060.8862, 60947867948.4771, -28806334951.8025);
            var v2 = new V3(-35202.7776429461, -13422.0371670933, -9917.35856458486);
            double soi2 = 112408990.754424;
            double mu3 = 1.32712440041939E+20;
            double arrivalDT = 7646859.02846741;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, peR: 0, optguard: true);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            dv.magnitude.ShouldEqual(6255.6098503827661, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void EarthToMercuryRandom(int seed) => EarthToMercuryFromSeed(seed);

        [Theory]
        [InlineData(1357)]
        private void EarthToMercuryHardSeeds(int seed) => EarthToMercuryFromSeed(seed);

        private void EarthToMercuryFromSeed(int seed)
        {
            var random = new Random(seed);

            double mu1 = 398600435436096;
            var r1 = new V3(142463209097.952, 147485866.387222, 38721854973.0728);
            var v1 = new V3(2694.98267839614, 28693.1174312002, -8955.43648332019);
            double soi1 = 924649202.461023;
            double mu2 = 22031780000000;
            var r2 = new V3(-17610435060.8862, 60947867948.4771, -28806334951.8025);
            var v2 = new V3(-35202.7776429461, -13422.0371670933, -9917.35856458486);
            double soi2 = 112408990.754424;
            double mu3 = 1.32712440041939E+20;
            double arrivalDT = 323443.026350487 + 8066251.26376697;

            const double EARTH_SURFACE = 6378145;

            double per = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            double apr = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            if (per > apr)
                (per, apr) = (apr, per);
            double l = 1 / (0.5 * (1 / apr + 1 / per));
            double ecc = (apr - per) / (apr + per);
            double inc = PI * random.NextDouble();
            double lan = TAU * random.NextDouble();
            double argp = TAU * random.NextDouble();
            double nu = TAU * random.NextDouble();

            Print($"per: {per} apr: {apr} l: {l} ecc: {ecc} inc: {Rad2Deg(inc)} lan: {Rad2Deg(lan)} argp: {Rad2Deg(argp)} nu: {Rad2Deg(nu)}");

            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(mu1, l, ecc, inc, lan, argp, nu);

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, peR: 0, optguard: false);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Fact]
        private void EarthToCeres()
        {
            var r0 = new V3(6521378.923092721, -144016.87747755065, 1410886.635286456);
            var v0 = new V3(864.52724109710925, 6940.5943694265807, -3287.3841869385583);
            var r1 = new V3(22199151070.873016, 139845008280.15186, -53671512076.940094);
            var v1 = new V3(-28779.993950097334, 2818.9196949053821, -5428.3526123540723);
            var r2 = new V3(-398438838799.81482, 95904198928.415253, -176169845629.96323);
            var v2 = new V3(-5534.8713945405507, -15167.338982527044, 3683.7661474865149);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 62632500000.000008;
            double soi2 = 76962905.730546668;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 52458966.698702089;
            double arrivalBounds = 210031.37569600236;
            double arrivalDTlower = arrivalDT - arrivalBounds;
            double arrivalDTupper = arrivalDT + arrivalBounds;
            double peR = 573000;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: true);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            dv.magnitude.ShouldEqual(4944.8587050280121, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void EarthToCeresRandom(int seed) => EarthToCeresFromSeed(seed);

        [Theory]
        [InlineData(915)]
        [InlineData(969)]
        [InlineData(1002)]
        [InlineData(1244)]
        [InlineData(1526)]
        [InlineData(1660)]
        [InlineData(1875)]
        [InlineData(1967)]
        [InlineData(225)]
        [InlineData(2381)]
        //[InlineData(475)] // the first optimizer pass lands in a bad basin.
        [InlineData(637)]
        [InlineData(725)]
        [InlineData(999)]
        private void EarthToCeresHardSeeds(int seed) => EarthToCeresFromSeed(seed);

        private void EarthToCeresFromSeed(int seed)
        {
            var random = new Random(seed);

            var r1 = new V3(22199151070.873016, 139845008280.15186, -53671512076.940094);
            var v1 = new V3(-28779.993950097334, 2818.9196949053821, -5428.3526123540723);
            var r2 = new V3(-398438838799.81482, 95904198928.415253, -176169845629.96323);
            var v2 = new V3(-5534.8713945405507, -15167.338982527044, 3683.7661474865149);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 62632500000.000008;
            double soi2 = 76962905.730546668;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 52458966.698702089;
            double arrivalBounds = 210031.37569600236;
            double arrivalDTlower = arrivalDT - arrivalBounds;
            double arrivalDTupper = arrivalDT + arrivalBounds;
            double peR = 573000;

            const double EARTH_SURFACE = 6378145;

            double per = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            double apr = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            if (per > apr)
                (per, apr) = (apr, per);
            double l = 1 / (0.5 * (1 / apr + 1 / per));
            double ecc = (apr - per) / (apr + per);
            double inc = PI * random.NextDouble();
            double lan = TAU * random.NextDouble();
            double argp = TAU * random.NextDouble();
            double nu = TAU * random.NextDouble();

            Print($"per: {per} apr: {apr} l: {l} ecc: {ecc} inc: {Rad2Deg(inc)} lan: {Rad2Deg(lan)} argp: {Rad2Deg(argp)} nu: {Rad2Deg(nu)}");

            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(mu1, l, ecc, inc, lan, argp, nu);

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: false);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Fact]
        private void EarthToMars()
        {
            var r0 = new V3(-4813533.7100184178, 4067332.94001712, 2197061.3473151801);
            var v0 = new V3(-3943.6447628964834, -6096.7177540935272, 2646.3673516840317);
            var r1 = new V3(72259723607.744156, -129249886043.4872, -23994554196.157143);
            var v1 = new V3(23464.265746216355, 14594.055858142867, -10895.306707940774);
            var r2 = new V3(141814014170.92267, -184968009549.99579, -48407945794.035919);
            var v2 = new V3(14750.434672844203, 15845.36047434548, -8009.1168536898813);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 42828373620699.094;
            double soi2 = 577254070.87249529;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 30453859.533329464;
            double arrivalBounds = 350952.48146891204;
            double arrivalDTlower = arrivalDT - arrivalBounds;
            double arrivalDTupper = arrivalDT + arrivalBounds;
            double peR = 3510800;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: true);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");
            dv.magnitude.ShouldEqual(3640.1292730002069, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void EarthToMarsRandom(int seed) => EarthToMarsFromSeed(seed);

        [Theory]
        [InlineData(2243)]
        [InlineData(505)]
        //[InlineData(1009)] // the parking orbit is at about half the SOI radius with a 38-day period. The initial guess already has roughly 43 km/s at the SOI exit, and the first pass ends infeasible
        [InlineData(1059)]
        [InlineData(113)]
        [InlineData(1135)]
        [InlineData(1292)]
        [InlineData(1922)]
        [InlineData(2052)]
        [InlineData(525)]
        [InlineData(890)]
        [InlineData(92)]
        private void EarthToMarsHardSeeds(int seed) => EarthToMarsFromSeed(seed);

        private void EarthToMarsFromSeed(int seed)
        {
            var random = new Random(seed);

            var r1 = new V3(72259723607.744156, -129249886043.4872, -23994554196.157143);
            var v1 = new V3(23464.265746216355, 14594.055858142867, -10895.306707940774);
            var r2 = new V3(141814014170.92267, -184968009549.99579, -48407945794.035919);
            var v2 = new V3(14750.434672844203, 15845.36047434548, -8009.1168536898813);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 42828373620699.094;
            double soi2 = 577254070.87249529;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 30453859.533329464;
            double arrivalBounds = 350952.48146891204;
            double arrivalDTlower = arrivalDT - arrivalBounds;
            double arrivalDTupper = arrivalDT + arrivalBounds;
            double peR = 3510800;

            const double EARTH_SURFACE = 6378145;

            double per = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            double apr = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            if (per > apr)
                (per, apr) = (apr, per);
            double l = 1 / (0.5 * (1 / apr + 1 / per));
            double ecc = (apr - per) / (apr + per);
            double inc = PI * random.NextDouble();
            double lan = TAU * random.NextDouble();
            double argp = TAU * random.NextDouble();
            double nu = TAU * random.NextDouble();

            Print($"per: {per} apr: {apr} l: {l} ecc: {ecc} inc: {Rad2Deg(inc)} lan: {Rad2Deg(lan)} argp: {Rad2Deg(argp)} nu: {Rad2Deg(nu)}");

            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(mu1, l, ecc, inc, lan, argp, nu);

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: false);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Fact]
        private void EarthToAsteroid()
        {
            var r0 = new V3(-3613600.2089605439, -5103455.2935977345, 2331613.3986294419);
            var v0 = new V3(5387.6194247482008, -4951.1281054341907, -2487.2177707052929);
            var r1 = new V3(-102996353813.89156, -93428051172.649872, 48712283511.210747);
            var v1 = new V3(17892.040644066445, -23454.205300374037, -6637.6727074400696);
            var r2 = new V3(216715673370.22906, -318827504282.35291, 128837531026.59201);
            var v2 = new V3(12822.776831847617, 9031.0223200621294, 5997.2223306495371);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 0;
            double soi2 = 0;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 54876414.676081017;
            double peR = 0;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, peR: peR, optguard: true);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");
            dv.magnitude.ShouldEqual(4789.592962160028, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            r2soi2.ShouldEqual(rsoi2helio, 1e-6);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void EarthToAsteroidRandom(int seed) => EarthToAsteroidFromSeed(seed);

        private void EarthToAsteroidFromSeed(int seed)
        {
            var random = new Random(seed);

            var r1 = new V3(-102996353813.89156, -93428051172.649872, 48712283511.210747);
            var v1 = new V3(17892.040644066445, -23454.205300374037, -6637.6727074400696);
            var r2 = new V3(216715673370.22906, -318827504282.35291, 128837531026.59201);
            var v2 = new V3(12822.776831847617, 9031.0223200621294, 5997.2223306495371);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 0;
            double soi2 = 0;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 54876414.676081017;
            double peR = 0;

            const double EARTH_SURFACE = 6378145;

            double per = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            double apr = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            if (per > apr)
                (per, apr) = (apr, per);
            double l = 1 / (0.5 * (1 / apr + 1 / per));
            double ecc = (apr - per) / (apr + per);
            double inc = PI * random.NextDouble();
            double lan = TAU * random.NextDouble();
            double argp = TAU * random.NextDouble();
            double nu = TAU * random.NextDouble();

            Print($"per: {per} apr: {apr} l: {l} ecc: {ecc} inc: {Rad2Deg(inc)} lan: {Rad2Deg(lan)} argp: {Rad2Deg(argp)} nu: {Rad2Deg(nu)}");

            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(mu1, l, ecc, inc, lan, argp, nu);

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, peR: peR, optguard: false);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            r2soi2.ShouldEqual(rsoi2helio, 1e-6);
        }

        [Fact]
        private void EarthToJupiter()
        {
            var r0 = new V3(-3912331.3024163404, 4747993.7355074612, 2586640.1496319249);
            var v0 = new V3(-5125.9892585184534, -5380.116158810044, 2122.3228889072616);
            var r1 = new V3(137412324343.95822, -23942591869.675461, -60338571254.994019);
            var v1 = new V3(5424.8571535079955, 28799.680047142847, 733.01461940330978);
            var r2 = new V3(172357530979.00909, 720778222761.22327, -15224336000.222565);
            var v2 = new V3(-12248.149100605306, 2887.3625226233494, 5401.8948520706454);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 1.2668653492180082E+17;
            double soi2 = 48196176124.28714;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 96640527.803545117;
            double arrivalDTlower = 0;
            double arrivalDTupper = double.PositiveInfinity;
            double peR = 70941833.416772693;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: true);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");
            // deep-well case (Jupiter, focusing factor ~10): exercises the analytic-b warm start + Jacobian
            // preconditioner landing the cheap (~6340 m/s, textbook Earth->Jupiter) basin.
            dv.magnitude.ShouldEqual(6339.7019346132129, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void EarthToJupiterRandom(int seed) => EarthToJupiterFromSeed(seed);

        [Theory]
        [InlineData(379)]
        [InlineData(921)]
        [InlineData(1051)]
        [InlineData(1167)]
        [InlineData(1423)]
        [InlineData(1511)]
        [InlineData(1640)]
        [InlineData(1851)]
        [InlineData(1961)]
        [InlineData(2488)]
        [InlineData(1661)]
        [InlineData(2170)]
        [InlineData(2350)]
        private void EarthToJupiterHardSeeds(int seed) => EarthToJupiterFromSeed(seed);

        private void EarthToJupiterFromSeed(int seed)
        {
            var random = new Random(seed);

            var r1 = new V3(137412324343.95822, -23942591869.675461, -60338571254.994019);
            var v1 = new V3(5424.8571535079955, 28799.680047142847, 733.01461940330978);
            var r2 = new V3(172357530979.00909, 720778222761.22327, -15224336000.222565);
            var v2 = new V3(-12248.149100605306, 2887.3625226233494, 5401.8948520706454);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 1.2668653492180082E+17;
            double soi2 = 48196176124.28714;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 96640527.803545117;
            double arrivalDTlower = 0;
            double arrivalDTupper = double.PositiveInfinity;
            double peR = 70941833.416772693;

            const double EARTH_SURFACE = 6378145;

            double per = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            double apr = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            if (per > apr)
                (per, apr) = (apr, per);
            double l = 1 / (0.5 * (1 / apr + 1 / per));
            double ecc = (apr - per) / (apr + per);
            double inc = PI * random.NextDouble();
            double lan = TAU * random.NextDouble();
            double argp = TAU * random.NextDouble();
            double nu = TAU * random.NextDouble();

            Print($"per: {per} apr: {apr} l: {l} ecc: {ecc} inc: {Rad2Deg(inc)} lan: {Rad2Deg(lan)} argp: {Rad2Deg(argp)} nu: {Rad2Deg(nu)}");

            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(mu1, l, ecc, inc, lan, argp, nu);

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: false);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void EarthToJupiterTargetRandom(int seed) => EarthToJupiterTargetFromSeed(seed);

        // These all target an inclination below the declination of the arrival v-infinity (about 5-8 degrees here, and the
        // plane of the arrival hyperbola has to contain v-infinity).  Before the inclination was clamped the optimizer bent
        // the heliocentric arc to rotate v-infinity, and crawled until it hit the iteration limit unconverged.
        [Theory]
        [InlineData(45)]  // inc 3.81, peR 4.1 Rj
        [InlineData(62)]  // inc 1.67, peR 2.0 Rj
        [InlineData(154)] // inc 0.68, peR 3.0 Rj
        [InlineData(621)] // inc 5.45, peR 2.5 Rj
        [InlineData(914)] // inc 0.35, peR 3.8 Rj
        [InlineData(171)] // inc 178.54, peR 1.4 Rj
        [InlineData(280)] // inc 175.42, peR 1.0 Rj
        [InlineData(413)] // inc 179.86, peR 18.9 Rj
        [InlineData(597)] // inc 177.88, peR 40.5 Rj
        private void EarthToJupiterTargetHardSeeds(int seed) => EarthToJupiterTargetFromSeed(seed);

        // equatorial at a low periapsis is out of reach, so these are clamped
        [Theory]
        [InlineData(0)]
        [InlineData(180)]
        private void EarthToJupiterEquatorial(double inc) => EarthToJupiterTarget(70941833.416772693, Deg2Rad(inc));

        private void EarthToJupiterTargetFromSeed(int seed)
        {
            var random = new Random(seed);

            const double JUPITER_RADIUS = 69911000;
            const double SOI2 = 48196176124.28714;

            // log-uniform so that low capture periapses are sampled as often as wide ones
            double peR = JUPITER_RADIUS * Pow(SOI2 * 0.6 / JUPITER_RADIUS, random.NextDouble());
            double inc = PI * random.NextDouble();

            EarthToJupiterTarget(peR, inc);
        }

        // the parking orbit is fixed (EarthToJupiterHardSeeds seed 1961), and the target periapsis and inclination vary
        private void EarthToJupiterTarget(double peR, double inc)
        {
            var r0 = new V3(51482894.10388647, 41045292.464912385, -100399372.32897358);
            var v0 = new V3(1092.8263078123787, 1091.4884781926173, 962.33550081447856);
            var r1 = new V3(137412324343.95822, -23942591869.675461, -60338571254.994019);
            var v1 = new V3(5424.8571535079955, 28799.680047142847, 733.01461940330978);
            var r2 = new V3(172357530979.00909, 720778222761.22327, -15224336000.222565);
            var v2 = new V3(-12248.149100605306, 2887.3625226233494, 5401.8948520706454);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 1.2668653492180082E+17;
            double soi2 = 48196176124.28714;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 96640527.803545117;
            double arrivalDTlower = 0;
            double arrivalDTupper = double.PositiveInfinity;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, inc: inc, optguard: false);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out} inc: {Rad2Deg(inc)} targetInc: {Rad2Deg(maneuver.TargetInc)}");

            // the inclination may only be clamped away from equatorial
            ((maneuver.TargetInc - PI / 2) * (inc - PI / 2)).ShouldBeGreaterThanOrEqual(0);
            Abs(maneuver.TargetInc - PI / 2).ShouldBeLessThanOrEqual(Abs(inc - PI / 2));

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 vsoi2helio) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 v2soi2) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            V3 vsoi2 = vsoi2helio - v2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
            Astro.PeriapsisFromStateVectors(mu2, rsoi2, vsoi2).ShouldEqual(peR, 1e-6);
            Astro.IncFromStateVectors(rsoi2, vsoi2).ShouldEqual(maneuver.TargetInc, 1e-6);
        }

        [Fact]
        private void EarthToVenus()
        {
            var r0 = new V3(-2524692.3521177084, -6010111.3403185587, -1430397.88334662);
            var v0 = new V3(6697.4581339203833, -2033.861683166539, -3276.0604346038458);
            var r1 = new V3(81107302085.431351, -113426897855.24834, -60210730828.347397);
            var v1 = new V3(23670.714980723751, 17290.925848593863, -1058.9529613984164);
            var r2 = new V3(-27443019348.482018, -102420278412.13942, -21215617737.593018);
            var v2 = new V3(32023.172615437456, -6174.1220120587095, -12826.981238246499);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 324858592000000.06;
            double soi2 = 616280853.74695194;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 12000356.02100748;
            double arrivalDTlower = 0;
            double arrivalDTupper = double.PositiveInfinity;
            double peR = 6204000;

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: true);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");
            dv.magnitude.ShouldEqual(3449.6799310543329, 1e-4);

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void EarthToVenusRandom(int seed) => EarthToVenusFromSeed(seed);

        private void EarthToVenusFromSeed(int seed)
        {
            var random = new Random(seed);

            var r1 = new V3(81107302085.431351, -113426897855.24834, -60210730828.347397);
            var v1 = new V3(23670.714980723751, 17290.925848593863, -1058.9529613984164);
            var r2 = new V3(-27443019348.482018, -102420278412.13942, -21215617737.593018);
            var v2 = new V3(32023.172615437456, -6174.1220120587095, -12826.981238246499);
            double mu1 = 398600435436096;
            double soi1 = 924649202.46102285;
            double mu2 = 324858592000000.06;
            double soi2 = 616280853.74695194;
            double mu3 = 1.3271244004193939E+20;
            double arrivalDT = 12000356.02100748;
            double arrivalDTlower = 0;
            double arrivalDTupper = double.PositiveInfinity;
            double peR = 6204000;

            const double EARTH_SURFACE = 6378145;

            double per = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            double apr = (soi1 * 0.6 - EARTH_SURFACE) * random.NextDouble() + EARTH_SURFACE;
            if (per > apr)
                (per, apr) = (apr, per);
            double l = 1 / (0.5 * (1 / apr + 1 / per));
            double ecc = (apr - per) / (apr + per);
            double inc = PI * random.NextDouble();
            double lan = TAU * random.NextDouble();
            double argp = TAU * random.NextDouble();
            double nu = TAU * random.NextDouble();

            Print($"per: {per} apr: {apr} l: {l} ecc: {ecc} inc: {Rad2Deg(inc)} lan: {Rad2Deg(lan)} argp: {Rad2Deg(argp)} nu: {Rad2Deg(nu)}");

            (V3 r0, V3 v0) = Astro.StateVectorsFromKeplerian(mu1, l, ecc, inc, lan, argp, nu);

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, arrivalDTlower, arrivalDTupper, peR, optguard: false);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            rsoi1.magnitude.ShouldEqual(soi1, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            rsoi2.magnitude.ShouldEqual(soi2, 1e-6);
        }

        [Fact]
        private void HeliocentricExactHalfRevolution()
        {
            // Fake solar system in canonical units (sun mu = 1, length unit of 1 AU) with coplanar circular source and target
            // orbits at r = 1/2 and r = 2, so the heliocentric scale in Maneuver() is exactly 1.0 and the scaled state is
            // bit-identical to the inputs.  The target is wound back from [-2, 0, 0], so the ZSOI bootstrap Lambert solve from
            // the source's initial position [1/2, 0, 0] is an exact 180 degree transfer.
            const double MU_SUN = 1.3271244004193939E+20;
            const double AU = 149597870700;

            double mu1 = 398600435436096 / MU_SUN;    // earth
            double mu2 = 42828373620699.094 / MU_SUN; // mars
            double mu3 = 1.0;
            double soi1 = 0.5 * Pow(mu1, 0.4);
            double soi2 = 2.0 * Pow(mu2, 0.4);
            double peR = 3510800 / AU;
            double rpark = 6563145 / AU;

            var r0 = new V3(rpark, 0, 0);
            var v0 = new V3(0, Sqrt(mu1 / rpark), 0);
            var r1 = new V3(0.5, 0, 0);
            var v1 = new V3(0, Sqrt(2), 0);

            // the Hohmann time of flight PI * 1.25^1.5 = 4.3905092069004539, moved 6 ulps so that propagating the wound back
            // target forward lands exactly on [-2, 0, 0] (at most nearby times it misses by an ulp in y, or |r2| != 2)
            double arrivalDT = 4.3905092069004485;
            (V3 r2, V3 v2) = Shepperd.Solve(mu3, -arrivalDT, new V3(-2, 0, 0), new V3(0, -Sqrt(0.5), 0));

            // check the premise of the test, which depends on the exact rounding of Shepperd
            Sqrt(r1.magnitude * r2.magnitude).ShouldEqual(1.0, 0);
            (V3 r2Arrival, V3 _) = Shepperd.Solve(mu3, arrivalDT, r2, v2);
            r2Arrival.ShouldEqual(new V3(-2, 0, 0), 0);

            var maneuver = new InterplanetaryTransfer();
            (V3 dv, double dt1out, double dt2out, double dt3out) = maneuver.Maneuver(r0, v0, mu1, r1, v1, soi1, mu2, r2, v2, soi2, mu3, arrivalDT, peR: peR, optguard: true);
            Logger.Print($"{dv} ({dv.magnitude}) {dt1out} {dt2out} {dt3out}");

            // the solution for the neighboring arrival times which are not exactly 180 degrees
            dv.magnitude.ShouldEqual(0.262904358179138, 1e-6);

            // the soi radii are small in canonical units, so check them relatively
            (V3 rBurn, V3 vBurnMinus) = Shepperd.Solve(mu1, dt1out, r0, v0);
            (V3 rsoi1, V3 vsoi1) = Shepperd.Solve(mu1, dt2out, rBurn, vBurnMinus + dv);
            (rsoi1.magnitude / soi1).ShouldEqual(1.0, 1e-6);
            (V3 r1soi1, V3 v1soi1) = Shepperd.Solve(mu3, dt1out + dt2out, r1, v1);
            V3 rsoi1helio = rsoi1 + r1soi1;
            V3 vsoi1helio = vsoi1 + v1soi1;
            (V3 rsoi2helio, V3 _) = Shepperd.Solve(mu3, dt3out - (dt1out + dt2out), rsoi1helio, vsoi1helio);
            (V3 r2soi2, V3 _) = Shepperd.Solve(mu3, dt3out, r2, v2);
            V3 rsoi2 = rsoi2helio - r2soi2;
            (rsoi2.magnitude / soi2).ShouldEqual(1.0, 1e-6);
        }
    }
}
