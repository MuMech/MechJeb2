/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Threading;
using MechJebLib.Utils;
using Xunit;

namespace MechJebLibTest.Utils
{
    public class AsyncJobTests
    {
        private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

        // runs until it is cancelled
        private sealed class CancellableJob : AsyncJob
        {
            public readonly ManualResetEventSlim Started = new ManualResetEventSlim();
            public bool Exited;

            public override void Run(object? o = null)
            {
                Started.Set();
                try
                {
                    while (true)
                    {
                        CancelToken.ThrowIfCancellationRequested();
                        Thread.Yield();
                    }
                }
                finally
                {
                    Exited = true;
                }
            }
        }

        // never checks the CancelToken, so it runs until it is let go
        private sealed class UncancellableJob : AsyncJob
        {
            public readonly ManualResetEventSlim Started = new ManualResetEventSlim();
            public bool Finished;

            public override void Run(object? o = null)
            {
                Started.Set();
                Thread.Sleep(50);
                Finished = true;
            }
        }

        [Fact]
        public void CancelAndReset_StopsRunningJob()
        {
            var job = new CancellableJob();
            Assert.True(job.TryStartJob());
            try
            {
                Assert.True(job.Started.Wait(_timeout));
            }
            finally
            {
                // stops the spinning job even if the assert fails, so it does not burn a core for the rest of the tests
                job.CancelAndReset();
            }

            Assert.True(job.Exited);
            Assert.True(job.IsReady);
            Assert.True(job.TryStartJob());
            job.CancelAndReset();
        }

        [Fact]
        public void CancelAndReset_RightAfterStart_LeavesJobReady()
        {
            // whether or not the task gets to start before the cancel, the job must not be left in Running
            var job = new CancellableJob();
            Assert.True(job.TryStartJob());

            job.CancelAndReset();

            Assert.True(job.IsReady);
            Assert.True(job.TryStartJob());
            job.CancelAndReset();
        }

        [Fact]
        public void Cancel_RightAfterStart_LeavesJobCancelled()
        {
            var job = new CancellableJob();
            Assert.True(job.TryStartJob());
            try
            {
                job.Cancel();
                Assert.True(SpinWait.SpinUntil(() => job.IsStopped, _timeout));
                Assert.True(job.IsCancelled);
            }
            finally
            {
                job.CancelAndReset();
            }
        }

        [Fact]
        public void CancelAndReset_WaitsForJobThatIgnoresCancel()
        {
            var job = new UncancellableJob();
            Assert.True(job.TryStartJob());
            Assert.True(job.Started.Wait(_timeout));

            job.CancelAndReset();

            Assert.True(job.Finished);
            Assert.True(job.IsReady);
        }

        [Fact]
        public void CancelAndReset_NeverStarted_IsNoOp()
        {
            var job = new CancellableJob();

            job.CancelAndReset();

            Assert.True(job.IsReady);
            Assert.True(job.TryStartJob());
            job.CancelAndReset();
        }
    }
}
