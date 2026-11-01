/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Threading;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;

namespace MechJebLibTest.PSGTests.AscentTests
{
    public class KerbinTests
    {
        private const double MU = 3531600000000;

        // the MainSailTinCan stages are the stage stats of the stock MechJebLibTest/Craft/Stock/Mainsail Tin Can.craft
        [Fact]
        private void MainSailTinCanVacuum()
        {
            double t0 = 82132.7494998545;


            Ascent ascent = Ascent.Builder()
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
               .AddCoast(79699.9986022711, 79699.9986022711, 0, 450, 0, 0, massContinuity: true)
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity: true)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06), new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), t0, MU, 600000)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (psg.Objective == Optimizer.ObjectiveType.MIN_THRUST_ACCEL).ShouldBeTrue();
            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Vgo(t0).ShouldEqual(2595.8963285782665, 1e-3);
            solution.Tgo(t0).ShouldEqual(610.08086261522431, 1e-3);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, _, _, _, _) = Astro.KeplerianFromStateVectors(MU, rf, vf);

            smaf.ShouldEqual(700000, 1e-7);
            eccf.ShouldBeZero(1e-6);
            incf.ShouldBeZero(1e-7);
        }

        [Fact]
        private void MainSailTinCanAtmo()
        {
            double t0 = 82132.7494998545;


            Ascent ascent = Ascent.Builder()
               .AerodynamicConstants(0.5, 19.6, 1.22497705725583, 4000, 0, 6797.80562531277, new V3(0, 0, 0.000291570900559802))
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
               .AddCoast(79699.9986022711, 79699.9986022711, 0, 450, 0, 0, massContinuity: true)
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity: true)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06), new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), t0, MU, 600000)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
               // the final MIN_THRUST_ACCEL stage runs close to the default timeout, which would fall back to the MIN_TIME solution
               .OptimizerTimeout(Timeout.Infinite)
               .Build();

            ascent.Run();

            Optimizer psg = ascent.GetOptimizer() ?? throw new Exception("null optimizer");
            using Solution solution = psg.Solution ?? throw new Exception("null solution");

            (psg.Objective == Optimizer.ObjectiveType.MIN_THRUST_ACCEL).ShouldBeTrue();
            psg.PrimalFeasibility.ShouldBeZero(1e-5);
            solution.Vgo(t0).ShouldEqual(3522.6470530283032, 1e-3);
            solution.Tgo(t0).ShouldEqual(472.31028465910936, 1e-3);

            (V3 rf, V3 vf) = solution.TerminalStateVectors();

            (double smaf, double eccf, double incf, _, _, _, _) = Astro.KeplerianFromStateVectors(MU, rf, vf);

            smaf.ShouldEqual(700000, 1e-7);
            eccf.ShouldBeZero(1e-6);
            incf.ShouldBeZero(1e-7);
        }
    }
}
