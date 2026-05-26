using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.ManeuversTests
{
    public class FineTuneClosestApproachToCelestialTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public FineTuneClosestApproachToCelestialTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i <= 25; i++)
                yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void RandomLunarTransfers(int seed)
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var rng = new Random(seed);

            double soi   = 66167158.6569544;
            double rmoon = 1737100;
            double mu0   = 398600435436096;
            var    r0    = new V3(-6419931.35599855, -1598504.22548976, 1968486.96029449);
            var    v0    = new V3(-747.608332256415, -9974.07871417746, -3695.67552943825);
            double mu1   = 4902800066163.8;
            var    r1    = new V3(4781917.4840911, -344412903.013029, -122488006.928327);
            var    v1    = new V3(996.891435624119, 120.171877824752, -367.753988404139);
            double peR   = 0.80 * soi * rng.NextDouble() + rmoon / 10;
            double inc   = Deg2Rad(120 * rng.NextDouble() + 30); // inclinations <= 30 && >= 150 degrees are not accssible for this test.
            double tsoi  = 403785.23845505138;                   // time on initial coast to SOI

            Print($"periapsis: {peR - rmoon} inc: {Rad2Deg(inc)}");

            var maneuver = new FineTuneClosestApproachToCelestial();

            (V3 dv, double dt1, double dt2) = maneuver.Maneuver(mu0, r0, v0, mu1, r1, v1, soi, tsoi, peR, inc);

            (V3 r0Burn, V3 v0Burn) = Shepperd.Solve(mu0, dt1, r0, v0);
            (V3 rf0, V3 vf0) = Shepperd.Solve(mu0, dt2, r0Burn, v0Burn + dv);
            (V3 rf1, V3 vf1) = Shepperd.Solve(mu0, dt1 + dt2, r1, v1);

            V3 rrel0 = r0 - rf1;
            V3 vrel0 = v0 - vf1;
            Print($"starting inc: {Rad2Deg(SafeAcos(V3.Cross(rrel0, vrel0).normalized.z))}");

            V3 rsoi = rf0 - rf1;
            V3 vsoi = vf0 - vf1;

            (rf1 - rf0).magnitude.ShouldEqual(soi, 1e-4);

            Astro.PeriapsisFromStateVectors(mu1, rsoi, vsoi).ShouldEqual(peR, 1e-4);

            Astro.IncFromStateVectors(rsoi, vsoi).ShouldEqual(inc, 1e-4);
        }
    }
}
