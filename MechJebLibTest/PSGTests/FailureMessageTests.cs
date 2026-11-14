/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.PSG;
using Xunit;

namespace MechJebLibTest.PSGTests
{
    // What the player reads in the Ascent window's LAST FAILURE line when a solve fails.
    [ParallelTestMethods]
    public class FailureMessageTests
    {
        // a run that stops at the optimizer's time limit says so, instead of a bare "Target unreachable"
        [Fact]
        private void TimeoutSaysTheOptimizerTimedOut()
        {
            Assert.Equal("Target unreachable: the optimizer timed out after 120 s", Ascent.FailureText("Target unreachable", true, 120000));
            Assert.Equal("Target unreachable", Ascent.FailureText("Target unreachable", false, 120000));
        }
    }
}
