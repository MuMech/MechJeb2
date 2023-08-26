using System;
using AssertExtensions;
using MechJebLib.Core.TwoBody;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;

namespace MechJebLibTest.Maneuvers
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
            double centralMu = 398600435436096;
            double moonMu = 4902800066163.8;
            var moonR0 = new V3(325420116.073166, -166367503.579338, -138858150.96145);
            var moonV0 = new V3(577.012296778094, 761.848508254181, 297.464594270612);
            double moonSOI = 66167158.6569544;
            var r0 = new V3(4198676.73768844, 5187520.71497923, -3.29371833446352);
            var v0 = new V3(-666.230112925872, 539.234048888927, 0.000277598267012666);
            double peR = 6.3781e6 + 60000; // 60Mm

            // this test periodically just fails, although its about 1-in-150 right now
            for (int i = 0; i < 1; i++)
            {
                var random = new Random();
                r0 = new V3(6616710 * random.NextDouble() - 3308350, 6616710 * random.NextDouble() - 3308350,
                    6616710 * random.NextDouble() - 3308350);
                v0 = new V3(2000 * random.NextDouble() - 1000, 2000 * random.NextDouble() - 1000, 2000 * random.NextDouble() - 1000);

                // skip if its already an escape orbit
                if (MechJebLib.Core.Maths.EccFromStateVectors(moonMu, r0, v0) > 1 ||
                    MechJebLib.Core.Maths.ApoapsisFromStateVectors(moonMu, r0, v0) > moonSOI)
                    continue;

                /*
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
                    ReturnFromMoon.NextManeuver(398600435436096, 4902800066163.8, moonR0, moonV0, 66167158.6569544, r0, v0, peR, 0, 0);

                (V3 r1, V3 v1) = Shepperd.Solve(moonMu, dt, r0, v0);
                double tt1 = MechJebLib.Core.Maths.TimeToNextRadius(moonMu, r1, v1 + dv, moonSOI);
                (V3 r2, V3 v2)         = Shepperd.Solve(moonMu, tt1, r1, v1 + dv);
                (V3 moonR2, V3 moonV2) = Shepperd.Solve(centralMu, dt + tt1, moonR0, moonV0);
                V3 r3 = moonR2 + r2;
                V3 v3 = moonV2 + v2;

                _testOutputHelper.WriteLine($"periapsis: {MechJebLib.Core.Maths.PeriapsisFromStateVectors(centralMu, r3, v3)}");

                MechJebLib.Core.Maths.PeriapsisFromStateVectors(centralMu, r3, v3).ShouldEqual(peR, 1e-3);
                newPeR.ShouldEqual(peR, 1e-3);
            }
        }
    }
}
