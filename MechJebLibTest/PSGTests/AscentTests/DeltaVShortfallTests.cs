/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.PSGTests.AscentTests
{
    [ParallelTestMethods]
    public class DeltaVShortfallTests
    {
        // guidance's default pitch start height, which the PSG simulation rises to straight up from the pad before it solves
        private const double RISE = 100;

        private const double MU = 3531600000000;
        private const double M0 = 79699.9986022711;
        private const double ISP = 310.000018173591;

        // the vacuum Mainsail Tin Can of KerbinTests, burn-coast-burn to a 100 km circular orbit, with a given empty mass
        private static Solution Solve(double mf)
        {
            Ascent ascent = Ascent.Builder()
               .AddStage(M0, mf, 1500000.09246676, ISP, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
               .AddCoast(M0, M0, 0, 450, 0, 0, massContinuity: true)
               .AddStage(M0, mf, 1500000.09246676, ISP, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity: true)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06),
                    new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), 0, MU, 600000)
               .VerticalRise(RISE)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            psg.Success().ShouldBeTrue();
            return (psg.Solution ?? throw new Exception("null solution")).DeNormalized();
        }

        [Fact]
        private void EnoughPropellantHasNoShortfall()
        {
            using Solution solution = Solve(15700.0014199451);

            solution.DeltaVShortfall().ShouldBeZero();
        }

        // about 2000 m/s of propellant for an orbit that takes about 2600 m/s: the optimizer still converges, by burning the stage
        // past its empty mass, and the shortfall is exactly the delta-v that burns beyond the stage's own
        [Fact]
        private void TooLittlePropellantGivesTheShortfall()
        {
            const double MF = 41300;
            using Solution solution = Solve(MF);

            double stageDeltaV = ISP * G0 * Log(M0 / MF);
            double shortfall = solution.DeltaVShortfall();

            shortfall.ShouldBePositive();
            shortfall.ShouldEqual(solution.Vgo(solution.TStart) - stageDeltaV, 1e-6);
            (shortfall > 300 && shortfall < 1000).ShouldBeTrue();
        }
    }
}
