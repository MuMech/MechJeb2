using System;
using MechJebLib.Functions;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;

namespace MechJebLibTest.ManeuversTests
{
    public class ReturnFromMoonTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ReturnFromMoonTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        private void NextManeuverToReturnFromMoonTest()
        {
            // this forces JIT compilation(?) of the SQL solver which takes ~250ms
            ChangeOrbitalElement.ChangePeriapsis(1.0, new V3(1, 0, 0), new V3(0, 1.0, 0), 1.0);

            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            const double CENTRAL_MU = 398600435436096;
            const double MOON_MU = 4902800066163.8;
            var moonR0 = new V3(325420116.073166, -166367503.579338, -138858150.96145);
            var moonV0 = new V3(577.012296778094, 761.848508254181, 297.464594270612);
            const double MOON_SOI = 66167158.6569544;
            /*
            var r0 = new V3(4198676.73768844, 5187520.71497923, -3.29371833446352);
            var v0 = new V3(-666.230112925872, 539.234048888927, 0.000277598267012666);
            */
            const double PER = 6.3781e6 + 60000; // 60km

            // this test periodically just fails, although its about 1-in-150 right now
            for (int i = 0; i < 1; i++)
            {
                var random = new Random();
                var r0 = new V3(6616710 * random.NextDouble() - 3308350, 6616710 * random.NextDouble() - 3308350,
                    6616710 * random.NextDouble() - 3308350);
                var v0 = new V3(2000 * random.NextDouble() - 1000, 2000 * random.NextDouble() - 1000, 2000 * random.NextDouble() - 1000);

                // skip if its already an escape orbit
                if (Astro.EccFromStateVectors(MOON_MU, r0, v0) > 1 ||
                    Astro.ApoapsisFromStateVectors(MOON_MU, r0, v0) > MOON_SOI)
                    continue;

                r0 = new V3(3033960.04435434, -538512.74449519, -1569171.01639252);
                v0 = new V3(344.550626047212, -973.108221764261, 907.813925253141);

                /*

                r0 = new V3(-23744.6019871556, -935848.195057236, -2970820.75826717);
                v0 = new V3(-511.683969531061, -141.692448007731, 975.982327934346);

                r0 = new V3(-1358340.16497667, -2896749.43027493, -2706207.90479207);
                v0 = new V3(774.176044284448, -468.922061598358, -642.571722922182);

                 r0 = new V3(-2370135.82005065, -78770.3300753334, 504857.052794559);
                 v0 = new V3(-735.735041897621, -590.266316007015, -137.364944041411);

                r0 = new V3(1922287.76973033, 1688857.60199125, -1128186.04080648);
                v0 = new V3(-644.272529354446, 90.1035308326145, -74.2516010414118);


                r0 = new V3(1448096.6091073, 2242802.60448088, 593799.176165359);
                v0 = new V3(316.818652356425, -684.168486708854, -453.106628476226);

                r0 = new V3(72567.8252483425, -1202508.27881747, 995820.818247795);
                v0 = new V3(733.61121478193, -501.358138165138, -26.4032981481417);

                r0 = new V3(1105140.06213014, -8431008.05414815, -4729658.84487529);
                v0 = new V3(-293.374690393787, -1173.3597686151, -411.755491683683);
                */
                _testOutputHelper.WriteLine($"iteration: {i}");

                (V3 dv, double dt, double newPeR) =
                    ReturnFromMoon.NextManeuver(398600435436096, 4902800066163.8, moonR0, moonV0, 66167158.6569544, r0, v0, PER, 0, 0);

                (V3 r1, V3 v1) = Shepperd.Solve(MOON_MU, dt, r0, v0);
                double tt1 = Astro.TimeToNextRadius(MOON_MU, r1, v1 + dv, MOON_SOI);
                (V3 r2, V3 v2)         = Shepperd.Solve(MOON_MU, tt1, r1, v1 + dv);
                (V3 moonR2, V3 moonV2) = Shepperd.Solve(CENTRAL_MU, dt + tt1, moonR0, moonV0);
                V3 r3 = moonR2 + r2;
                V3 v3 = moonV2 + v2;

                _testOutputHelper.WriteLine($"periapsis: {Astro.PeriapsisFromStateVectors(CENTRAL_MU, r3, v3)}");

                Astro.PeriapsisFromStateVectors(CENTRAL_MU, r3, v3).ShouldEqual(PER, 1e-3);
                newPeR.ShouldEqual(PER, 1e-3);
            }
        }

        [Fact]
        private void NextManeuverToReturnFromMoonOptguardTest()
        {
            // this forces JIT compilation(?) of the SQL solver which takes ~250ms
            ChangeOrbitalElement.ChangePeriapsis(1.0, new V3(1, 0, 0), new V3(0, 1.0, 0), 1.0);

            Logger.Register(o => _testOutputHelper.WriteLine((string)o));
            const double CENTRAL_MU = 398600435436096;
            const double MOON_MU = 4902800066163.8;
            var moonR0 = new V3(325420116.073166, -166367503.579338, -138858150.96145);
            var moonV0 = new V3(577.012296778094, 761.848508254181, 297.464594270612);
            const double MOON_SOI = 66167158.6569544;

            var r0 = new V3(4198676.73768844, 5187520.71497923, -3.29371833446352);
            var v0 = new V3(-666.230112925872, 539.234048888927, 0.000277598267012666);

            const double PER = 6.3781e6 + 60000; // 60km

            (V3 dv, double dt, double newPeR) =
                ReturnFromMoon.NextManeuver(398600435436096, 4902800066163.8, moonR0, moonV0, 66167158.6569544, r0, v0, PER, 0, 0, optguard: true);

            (V3 r1, V3 v1) = Shepperd.Solve(MOON_MU, dt, r0, v0);
            double tt1 = Astro.TimeToNextRadius(MOON_MU, r1, v1 + dv, MOON_SOI);
            (V3 r2, V3 v2)         = Shepperd.Solve(MOON_MU, tt1, r1, v1 + dv);
            (V3 moonR2, V3 moonV2) = Shepperd.Solve(CENTRAL_MU, dt + tt1, moonR0, moonV0);
            V3 r3 = moonR2 + r2;
            V3 v3 = moonV2 + v2;

            _testOutputHelper.WriteLine($"periapsis: {Astro.PeriapsisFromStateVectors(CENTRAL_MU, r3, v3)}");

            Astro.PeriapsisFromStateVectors(CENTRAL_MU, r3, v3).ShouldEqual(PER, 1e-3);
            newPeR.ShouldEqual(PER, 1e-3);
        }
    }
}
