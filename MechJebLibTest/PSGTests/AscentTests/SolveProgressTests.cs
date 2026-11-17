/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Threading.Tasks;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;

namespace MechJebLibTest.PSGTests.AscentTests
{
    // The progress line of a first solve on the pad: which pass runs, for how long, and how far it has got.
    [ParallelTestMethods]
    public class SolveProgressTests
    {
        private const double MU = 3531600000000;
        private const double M0 = 79699.9986022711;

        // the vacuum Mainsail Tin Can of KerbinTests, burn-coast-burn to a 100 km circular orbit
        private static Ascent TinCan() =>
            Ascent.Builder()
               .AddStage(M0, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
               .AddCoast(M0, M0, 0, 450, 0, 0, massContinuity: true)
               .AddStage(M0, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0,
                    massContinuity: true)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06),
                    new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), 0, MU, 600000)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
               .Build();

        [Fact]
        private void NothingBeforeTheSolveStarts()
        {
            SolveProgress progress = TinCan().GetProgress();

            progress.Pass.ShouldEqual(0);
            Assert.Null(progress.Name);
            progress.Iterations.ShouldEqual(0);
        }

        // the count made before the solve is the number of passes it then runs, and the last one adds throttling
        [Fact]
        private void AConvergedSolveEndsOnItsLastPass()
        {
            Ascent ascent = TinCan();
            ascent.Run();
            ascent.GetOptimizer()!.Success().ShouldBeTrue();

            SolveProgress progress = ascent.GetProgress();

            progress.Pass.ShouldEqual(progress.PassCount);
            progress.Name.ShouldEqual("adding throttling");
            (progress.Iterations > 0).ShouldBeTrue();
            double.IsNaN(progress.Infeasibility).ShouldBeFalse();
        }

        // read from another thread while the solve runs, as the game does: the pass only goes up and never passes the count
        [Fact]
        private async Task ReadWhileItRuns()
        {
            Ascent ascent = TinCan();
            Task solve = Task.Run(() => ascent.Run());

            int last = 0;
            while (!solve.IsCompleted)
            {
                SolveProgress progress = ascent.GetProgress();
                (progress.Pass >= last).ShouldBeTrue();
                (progress.Pass <= progress.PassCount).ShouldBeTrue();
                (progress.PassSeconds >= 0).ShouldBeTrue();
                last = progress.Pass;
                await Task.Delay(1);
            }

            await solve;
            ascent.GetProgress().Pass.ShouldEqual(ascent.GetProgress().PassCount);
        }
    }
}
