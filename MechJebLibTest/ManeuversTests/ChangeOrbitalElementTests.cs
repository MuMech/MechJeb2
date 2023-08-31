using System;
using AssertExtensions;
using MechJebLib.Core;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.ManeuversTests
{
    public class ChangeOrbitalElementTests
    {
        [Fact]
        private void ChangeOrbitalElementTest()
        {
            const int NTRIALS = 50;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var r = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double newR = random.NextDouble() * r.magnitude;

                double rscale = random.NextDouble() * 1.5e8 + 1;
                double vscale = random.NextDouble() * 3e4 + 1;
                r    *= rscale;
                v    *= vscale;
                newR *= rscale;
                double mu = rscale * vscale * vscale;

                V3 dv = ChangeOrbitalElement.ChangePeriapsis(mu, r, v, newR, true);
                Maths.PeriapsisFromStateVectors(mu, r, v + dv).ShouldEqual(newR, 1e-9);

                // validate this API works left handed.
                V3 dv2 = ChangeOrbitalElement.ChangePeriapsis(mu, r.xzy, v.xzy, newR);
                dv2.ShouldEqual(dv.xzy, 1e-3);

                // now test apoapsis changing to elliptical
                newR = random.NextDouble() * rscale * 1e9 + r.magnitude;
                V3 dv3 = ChangeOrbitalElement.ChangeApoapsis(mu, r, v, newR, true);
                Maths.ApoapsisFromStateVectors(mu, r, v + dv3).ShouldEqual(newR, 1e-5);

                // now test apoapsis changing to hyperbolic
                newR = -(random.NextDouble() * 1e9 + 1e3) * rscale;
                V3 dv4 = ChangeOrbitalElement.ChangeApoapsis(mu, r, v, newR, true);
                Maths.ApoapsisFromStateVectors(mu, r, v + dv4).ShouldEqual(newR, 1e-5);

                // now test changing the SMA.
                newR = random.NextDouble() * 1e3 * rscale;
                /*
                mu   = 8657343022269006;
                r    = new V3(-37559037.128101, -13154072.5832729, -86913972.3215555);
                v    = new V3(23370.6359939819, 12558.3695536628, 17864.1347044172);
                newR = 35354091.544774853;
                */
                V3 dv5 = ChangeOrbitalElement.ChangeSMA(mu, r, v, newR, true);
                Maths.SmaFromStateVectors(mu, r, v + dv5).ShouldEqual(newR, 1e-9);

                // now test changing the Ecc
                double newEcc = random.NextDouble() * 5 + 2.5;
                V3 dv6 = ChangeOrbitalElement.ChangeECC(mu, r, v, newEcc);
                (_, double ecc) = Maths.SmaEccFromStateVectors(mu, r, v + dv6);
                ecc.ShouldEqual(newEcc, 1e-9);
            }
        }

        [Fact]
        private void InitialCircularPeriapsisChange() => ChangeOrbitalElement.ChangePeriapsis(1.0, new V3(1, 0, 0), new V3(0, 1.0, 0), 1.0);
    }
}
