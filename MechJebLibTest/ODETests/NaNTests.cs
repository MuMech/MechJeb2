/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MechJebLib.Interpolants;
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

        private static void AssertFails<TException>(Action action) where TException : Exception
        {
            AggregateException e = Assert.Throws<AggregateException>(() => Run(action));
            Assert.IsType<TException>(e.InnerException);
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void NaNStateFails(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = double.NaN;

            AssertFails<ArgumentException>(() => solver.Solve((y, x, dy) => dy[0] = -y[0], y0, yf, 0, 1));
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void NaNStateFailsWithAStartingStep(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = double.NaN;
            solver.Hstart = 0.1;

            AssertFails<ArgumentException>(() => solver.Solve((y, x, dy) => dy[0] = -y[0], y0, yf, 0, 1));
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void RhsGoingNaNFails(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = 1;

            AssertFails<InvalidOperationException>(() => solver.Solve((y, x, dy) => dy[0] = x > 0.5 ? double.NaN : 1, y0, yf, 0, 1));
        }

        // there is no last good point to stop at, so this throws even without ThrowOnMinStep
        [Theory]
        [MemberData(nameof(Solvers))]
        public void NaNStateFailsWithoutMinStepThrow(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = double.NaN;
            solver.ThrowOnMinStep = false;

            AssertFails<ArgumentException>(() => solver.Solve((y, x, dy) => dy[0] = -y[0], y0, yf, 0, 1));
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void RhsNaNAtTheStartFailsWithoutMinStepThrow(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = 1;
            solver.ThrowOnMinStep = false;

            AssertFails<InvalidOperationException>(() => solver.Solve((y, x, dy) => dy[0] = double.NaN, y0, yf, 0, 1));
        }

        // The RHS is NaN past 0.5 whatever the state, so the steps shrink up against it until the minimum step size still
        // crosses it, and the integration ends at the last finite point.  With the boundary at 0.005 the first trial step
        // in SelectInitialStep already lands past it.
        [Theory]
        [MemberData(nameof(Solvers))]
        public void RhsGoingNaNStopsAtTheLastFinitePoint(AbstractRungeKutta solver)
        {
            foreach (double boundary in new[] { 0.5, 0.005 })
            {
                using var y0 = Vec.Rent(1);
                using var yf = Vec.Rent(1);
                y0[0] = 1;
                solver.ThrowOnMinStep = false;

                Run(() => solver.Solve((y, x, dy) => dy[0] = x > boundary ? double.NaN : 1, y0, yf, 0, 1));

                Assert.Equal(AbstractIVP.IVPStatus.NonFiniteAtMinStep, solver.Status);
                Assert.InRange(solver.T, boundary - 1e-9, boundary);
                Assert.Equal(1 + solver.T, yf[0], 12);
            }
        }

        [Theory]
        [MemberData(nameof(LowerOrderSolvers))]
        public void RhsGoingNaNLeavesAFiniteInterpolant(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            using var interpolant = VecInterpolant.Rent();
            y0[0] = 1;
            solver.ThrowOnMinStep = false;

            Run(() => solver.Solve((y, x, dy) => dy[0] = x > 0.5 ? double.NaN : 1, y0, yf, 0, 1, interpolant));

            Assert.Equal(AbstractIVP.IVPStatus.NonFiniteAtMinStep, solver.Status);
            Assert.Equal(0.0, interpolant.MinT);
            Assert.Equal(solver.T, interpolant.MaxT);

            for (int i = 0; i <= 100; i++)
            {
                double t = interpolant.MaxT * i / 100;
                using Vec yt = interpolant.Evaluate(t);
                Assert.Equal(1 + t, yt[0], 12);
            }
        }

        // a fixed step size can't shrink, so the first step that goes NaN is already at the minimum
        [Theory]
        [MemberData(nameof(Solvers))]
        public void FixedStepGoingNaN(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = 1;
            solver.Hmin = solver.Hmax = 0.125;

            void Rhs(Vec y, double x, Vec dy) => dy[0] = x > 0.5 ? double.NaN : 1;

            AssertFails<InvalidOperationException>(() => solver.Solve(Rhs, y0, yf, 0, 1));

            solver.ThrowOnMinStep = false;
            Run(() => solver.Solve(Rhs, y0, yf, 0, 1));

            Assert.Equal(AbstractIVP.IVPStatus.NonFiniteAtMinStep, solver.Status);
            Assert.Equal(0.5, solver.T);
            Assert.Equal(1.5, yf[0], 12);
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void MaxEvalsStopsTheIntegration(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            y0[0] = 1;
            solver.MaxEvals = 60;

            void Rhs(Vec y, double x, Vec dy) => dy[0] = -y[0];

            AssertFails<InvalidOperationException>(() => solver.Solve(Rhs, y0, yf, 0, 10));
            Assert.Equal(AbstractIVP.IVPStatus.MaxEvalsExceeded, solver.Status);

            solver.ThrowOnMaxEvals = false;
            Run(() => solver.Solve(Rhs, y0, yf, 0, 10));

            // DP8 doesn't get its first step accepted within 60 evaluations: it ends where it started
            Assert.Equal(AbstractIVP.IVPStatus.MaxEvalsExceeded, solver.Status);
            Assert.InRange(solver.Evals, 1, 60);
            Assert.InRange(solver.T, 0, 10 - 1e-3);
            Assert.Equal(Exp(-solver.T), yf[0], 6);
        }

        // y[1] runs into zero at tf, so dy[0] = 1/y[1] blows up there: without ThrowOnMinStep the solver grinds up against
        // the asymptote and hands back something finite rather than throwing.
        [Theory]
        [MemberData(nameof(LowerOrderSolvers))]
        public void AsymptoteWithoutMinStepThrowReturnsAFiniteState(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(2);
            using var yf = Vec.Rent(2);
            y0[0] = 0;
            y0[1] = 1;
            solver.ThrowOnMinStep = false;
            solver.MaxEvals = 0;

            Run(() => solver.Solve((y, x, dy) =>
            {
                dy[0] = 1 / y[1];
                dy[1] = -1;
            }, y0, yf, 0, 1));

            Assert.Contains(solver.Status, new[] { AbstractIVP.IVPStatus.Success, AbstractIVP.IVPStatus.NonFiniteAtMinStep });
            Assert.True(yf.IsFinite());
            // FIXME: a step raised to the minimum step size isn't snapped to tf, so this ends a couple of ulps past it
            Assert.InRange(solver.T, 1 - 1e-9, 1 + 1e-12);
            Assert.Equal(1 - solver.T, yf[1], 9);
        }

        [Theory]
        [MemberData(nameof(Solvers))]
        public void CancellationStopsTheSolve(AbstractRungeKutta solver)
        {
            using var y0 = Vec.Rent(1);
            using var yf = Vec.Rent(1);
            using var cts = new CancellationTokenSource();
            y0[0] = 1;
            cts.Cancel();
            solver.CancellationToken = cts.Token;

            AssertFails<OperationCanceledException>(() => solver.Solve((y, x, dy) => dy[0] = -y[0], y0, yf, 0, 1));
        }

        // Not DP8, which nothing but the tests uses: it has no interpolant, and it runs out of steps on the sqrt RHS below
        // without any NaN.
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
