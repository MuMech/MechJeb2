/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Maneuvers;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;

namespace MechJebLibTest.ManeuversTests
{
    public class ChangeOrbitalElementTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ChangeOrbitalElementTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i <= 50; i++)
                yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        private void ChangeOrbitalElementTest(int seed)
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            var random = new Random(seed);

            var    r    = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var    v    = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double newR = random.NextDouble() * r.magnitude;

            double rscale = random.NextDouble() * 1.5e8 + 1;
            double vscale = random.NextDouble() * 3e4 + 1;
            r *= rscale;
            v *= vscale;
            newR *= rscale;
            double mu = rscale * vscale * vscale;

            V3 dv = ChangeOrbitalElement.ChangePeriapsis(mu, r, v, newR, true);
            Astro.PeriapsisFromStateVectors(mu, r, v + dv).ShouldEqual(newR, 1e-9);

            // validate this API works left handed.
            V3 dv2 = ChangeOrbitalElement.ChangePeriapsis(mu, r.xzy, v.xzy, newR);
            dv2.ShouldEqual(dv.xzy, 1e-3);

            // now test apoapsis changing to elliptical
            newR = random.NextDouble() * rscale * 1e9 + r.magnitude;
            V3 dv3 = ChangeOrbitalElement.ChangeApoapsis(mu, r, v, newR, true);
            Astro.ApoapsisFromStateVectors(mu, r, v + dv3).ShouldEqual(newR, 1e-4);

            // now test apoapsis changing to hyperbolic
            newR = -(random.NextDouble() * 1e9 + 1e3) * rscale;
            V3 dv4 = ChangeOrbitalElement.ChangeApoapsis(mu, r, v, newR, true);
            Astro.ApoapsisFromStateVectors(mu, r, v + dv4).ShouldEqual(newR, 1e-5);

            // now test changing the SMA.
            newR = random.NextDouble() * 1e3 * rscale;
            /*
            mu   = 8657343022269006;
            r    = new V3(-37559037.128101, -13154072.5832729, -86913972.3215555);
            v    = new V3(23370.6359939819, 12558.3695536628, 17864.1347044172);
            newR = 35354091.544774853;
            */
            V3 dv5 = ChangeOrbitalElement.ChangeSMA(mu, r, v, newR, true);
            Astro.SmaFromStateVectors(mu, r, v + dv5).ShouldEqual(newR, 1e-9);

            // now test changing the Ecc
            double newEcc = random.NextDouble() * 5 + 2.5;
            V3     dv6    = ChangeOrbitalElement.ChangeECC(mu, r, v, newEcc);
            (_, double ecc) = Astro.SmaEccFromStateVectors(mu, r, v + dv6);
            ecc.ShouldEqual(newEcc, 1e-9);
        }

        [Fact]
        private void InitialCircularPeriapsisChange() => ChangeOrbitalElement.ChangePeriapsis(1.0, new V3(1, 0, 0), new V3(0, 1.0, 0), 1.0);
    }
}
