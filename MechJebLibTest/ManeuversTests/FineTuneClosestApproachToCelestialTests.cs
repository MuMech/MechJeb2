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
            var rng = new Random(seed);

            double soi   = 66167158.6569544;
            double rmoon = 1737100;
            // i think as the peR gets larger the feasible set of inclinations tends to shrink?
            double peR = 0.80 * soi * rng.NextDouble() + rmoon / 10;
            // inclinations <= 30 && >= 150 degrees are not reliably feasible for the geometry of this test
            double inc = Deg2Rad(120 * rng.NextDouble() + 30);

            DoTransfer(peR, inc);
        }

        [Fact]
        public void ZeroPeriapsis()
        {
            // tests the PeR=0.0 branch, which could NaN.  inc is degenerate at zero PeR.
            DoTransfer(0.0, double.NaN);
        }

        [Fact]
        public void LargePeriapsis() => DoTransfer(66167158, double.NaN, 1e-10);

        private void DoTransfer(double peR, double inc, double perEps = 1e-4, double incEps = 1e-4)
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            double soi  = 66167158.6569544;
            double mu0  = 398600435436096;
            var    r0   = new V3(-6419931.35599855, -1598504.22548976, 1968486.96029449);
            var    v0   = new V3(-747.608332256415, -9974.07871417746, -3695.67552943825);
            double mu1  = 4902800066163.8;
            var    r1   = new V3(4781917.4840911, -344412903.013029, -122488006.928327);
            var    v1   = new V3(996.891435624119, 120.171877824752, -367.753988404139);
            double tsoi = 403785.23845505138; // time on initial coast to SOI

            Print($"periapsis: {peR} inc: {Rad2Deg(inc)}");

            var maneuver = new FineTuneClosestApproachToCelestial();

            (V3 dv, double dt1, double dt2) = maneuver.Maneuver(mu0, r0, v0, mu1, r1, v1, soi, tsoi, peR, inc: inc);

            (V3 rBurn, V3 vBurn) = Shepperd.Solve(mu0, dt1, r0, v0);
            (V3 rf0, V3 vf0) = Shepperd.Solve(mu0, dt2, rBurn, vBurn + dv);
            (V3 rf1, V3 vf1) = Shepperd.Solve(mu0, dt1 + dt2, r1, v1);

            V3 rrel0 = r0 - rf1;
            V3 vrel0 = v0 - vf1;
            Print($"starting inc: {Rad2Deg(SafeAcos(V3.Cross(rrel0, vrel0).normalized.z))}");

            V3 rsoi = rf0 - rf1;
            V3 vsoi = vf0 - vf1;

            (rf1 - rf0).magnitude.ShouldEqual(soi, 1e-4);

            Astro.PeriapsisFromStateVectors(mu1, rsoi, vsoi).ShouldEqual(peR, perEps);

            // inc is only constrained (and asserted) when finite; NaN requests no inclination constraint
            if (IsFinite(inc))
                Astro.IncFromStateVectors(rsoi, vsoi).ShouldEqual(inc, incEps);
        }
    }
}
