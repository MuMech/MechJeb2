/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.ODETests
{
    public class NaNTests
    {
        public static IEnumerable<object[]> Solvers()
        {
            yield return new object[] { new DP5() };
            yield return new object[] { new DP8() };
            yield return new object[] { new BS3() };
            yield return new object[] { new Tsit5() };
        }

        // on a thread of its own, so that a solver that never returns fails its test instead of hanging the whole run, and the
        // time limit doesn't run out waiting for a thread pool that the other tests are keeping busy
        private static void Run(Action action) =>
            Assert.True(Task.Factory.StartNew(action, TaskCreationOptions.LongRunning).Wait(TimeSpan.FromSeconds(30)), "the solver never returned");

        private static void AssertFails(Action action)
        {
            AggregateException e = Assert.Throws<AggregateException>(() => Run(action));
            Assert.IsType<InvalidOperationException>(e.InnerException);
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void NaNStateFails(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = double.NaN;

            AssertFails(() => solver.Solve((y, x, dy) => dy[0] = -y[0], y0, yf, 0, 1));
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void NaNStateFailsWithAStartingStep(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = double.NaN;
            solver.Hstart = 0.1;

            AssertFails(() => solver.Solve((y, x, dy) => dy[0] = -y[0], y0, yf, 0, 1));
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void RhsGoingNaNFails(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = 1;

            AssertFails(() => solver.Solve((y, x, dy) => dy[0] = x > 0.5 ? double.NaN : 1, y0, yf, 0, 1));
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void NaNStateFailsWithoutMinStepThrow(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = double.NaN;
            solver.ThrowOnMinStep = false;

            AssertFails(() => solver.Solve((y, x, dy) => dy[0] = -y[0], y0, yf, 0, 1));
        }

        // Not DP8, which nothing but the tests uses: it runs out of steps on this RHS without any NaN.
        public static IEnumerable<object[]> LowerOrderSolvers()
        {
            yield return new object[] { new DP5() };
            yield return new object[] { new BS3() };
            yield return new object[] { new Tsit5() };
        }

        // dy = -sqrt(y) is NaN once a stage overshoots y = 0, which the first step of 1.9 does: only the step is at fault
        [Theory]
        [MemberData(nameof(LowerOrderSolvers))]
        public void StepTooLargeForTheRhsShrinks(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = 1;
            solver.Hstart = 1.9;

            Run(() => solver.Solve((y, x, dy) => dy[0] = -Sqrt(y[0]), y0, yf, 0, 1.9));

            Assert.Equal(0.0025, yf[0], 5);
        }
    }
}
