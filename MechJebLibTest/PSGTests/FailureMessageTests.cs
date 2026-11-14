/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;

namespace MechJebLibTest.PSGTests
{
    // What the player reads in the Ascent window's LAST FAILURE line when a solve fails.
    [ParallelTestMethods]
    public class FailureMessageTests
    {
        private const double MU = 3531600000000;

        // a run that stops at the optimizer's time limit says so, instead of a bare "Target unreachable"
        [Fact]
        private void TimeoutSaysTheOptimizerTimedOut()
        {
            Ascent ascent = Ascent.Builder()
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06),
                    new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), 0, MU, 600000)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
               .OptimizerTimeout(1)
               .Build();

            Exception e = Assert.ThrowsAny<Exception>(() => ascent.Run());

            Assert.Contains("Target unreachable", e.Message);
            Assert.Contains("the optimizer timed out after 0 s", e.Message);
        }
    }
}
