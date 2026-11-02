/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Threading;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.Utils;
using Xunit;

namespace MechJebLibTest.PSGTests
{
    // MechJebModulePSGSimulation cancels the running job when it is disabled, which has to stop the optimizer itself and not
    // just the guesser
    public class CancellationTests
    {
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        // KerbinTests.MainSailTinCanVacuum
        private static Ascent.AscentBuilder MainSailTinCanVacuum() =>
            Ascent.Builder()
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0)
               .AddCoast(79699.9986022711, 79699.9986022711, 0, 450, 0, 0, massContinuity: true)
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: true, ispCurrent: 285.387620615011, minThrottle: 0.0, massContinuity: true)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06), new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), 82132.7494998545, 3531600000000, 600000)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false);

        // The tests warm start from one shared cold solve.  A warm start skips the guesser, which already watched the job's
        // CancelToken, so only the optimizer can see the cancel.  The tests in a class run sequentially, so sharing is safe.
        private static readonly Lazy<Solution> _coldSolution = new Lazy<Solution>(() =>
        {
            Ascent ascent = MainSailTinCanVacuum().Build();
            ascent.Run();
            return ascent.GetOptimizer()?.Solution ?? throw new Exception("null solution");
        });

        // waits for a cancelled job to stop and returns the state it stopped in
        private static AsyncJob.JobState WaitForStop(Ascent ascent)
        {
            bool stopped = SpinWait.SpinUntil(() => ascent.IsStopped, _timeout);
            AsyncJob.JobState state = ascent.State;
            Exception? exception = ascent.Exception;

            // if the cancel was ignored this waits for the job to finish, so it does not burn a core for the rest of the tests
            ascent.CancelAndReset();

            Assert.True(stopped, "the job did not stop after being cancelled");
            Assert.Null(exception);
            return state;
        }

        [Theory]
        // cancelled before the optimizer starts, seen by its first constraint evaluation outside of ALGLIB
        [InlineData(0)]
        // cancelled mid-solve (this warm start takes ~1 s), seen inside minnlcoptimize where ALGLIB rewraps the exception
        [InlineData(200)]
        public void CancelStopsWarmStartedSolve(int delayMilliseconds)
        {
            Ascent ascent = MainSailTinCanVacuum().OldSolution(_coldSolution.Value).Build();

            Assert.True(ascent.TryStartJob());
            if (delayMilliseconds == 0)
                ascent.Cancel();
            else
                ascent.CancelAfter(TimeSpan.FromMilliseconds(delayMilliseconds));

            Assert.Equal(AsyncJob.JobState.Cancelled, WaitForStop(ascent));
        }
    }
}
