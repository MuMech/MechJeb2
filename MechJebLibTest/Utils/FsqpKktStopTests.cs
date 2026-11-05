/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Utils;
using Xunit;
using static System.Math;

namespace MechJebLibTest.Utils
{
    // FsqpKktStop reads the multipliers out of ALGLIB's solver state, which isn't a documented API.  A renamed field fails the
    // build, but these are what would catch a field changing its meaning across an ALGLIB upgrade.
    public class FsqpKktStopTests
    {
        private const int N = 3;

        //     min (x0-1)^2 + (x1-2)^2 + x2^2
        //     x0^2 + x1^2 + x2^2 = 1
        //     0 <= x0*x1 <= 0.1      the upper side is active at the solution
        //     x2 >= 0.1              active at the solution
        private static readonly double[] _x0   = { 0.5, 0.5, 0.5 };
        private static readonly double[] _bndl = { double.NegativeInfinity, double.NegativeInfinity, 0.1 };
        private static readonly double[] _bndu = { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity };
        private static readonly double[] _nl   = { 1.0, 0.0 };
        private static readonly double[] _nu   = { 1.0, 0.1 };

        private readonly double _objectiveScale;

        // the objective scale pushes the solver's own objective normalization away from one
        private FsqpKktStopTests(double objectiveScale) => _objectiveScale = objectiveScale;

        public FsqpKktStopTests() : this(1.0) { }

        private void Function(double[] x, double[] fi, double[,] jac, object? obj = null)
        {
            fi[0] = _objectiveScale * ((x[0] - 1) * (x[0] - 1) + (x[1] - 2) * (x[1] - 2) + x[2] * x[2]);
            fi[1] = x[0] * x[0] + x[1] * x[1] + x[2] * x[2];
            fi[2] = x[0] * x[1];

            jac[0, 0] = _objectiveScale * 2 * (x[0] - 1);
            jac[0, 1] = _objectiveScale * 2 * (x[1] - 2);
            jac[0, 2] = _objectiveScale * 2 * x[2];
            jac[1, 0] = 2 * x[0];
            jac[1, 1] = 2 * x[1];
            jac[1, 2] = 2 * x[2];
            jac[2, 0] = x[1];
            jac[2, 1] = x[0];
            jac[2, 2] = 0;
        }

        private double[] RawLagrangianGradient(double[] x, double[] lagnlc, double[] lagbc)
        {
            double[] fi = new double[3];
            double[,] jac = new double[3, N];
            Function(x, fi, jac);

            double[] g = new double[N];
            for (int i = 0; i < N; i++)
                g[i] = jac[0, i] + lagnlc[0] * jac[1, i] + lagnlc[1] * jac[2, i] + lagbc[i];
            return g;
        }

        private static double MaxAbs(double[] v)
        {
            double max = 0;
            foreach (double d in v)
                max = Max(max, Abs(d));
            return max;
        }

        private static alglib.minnlcstate Create()
        {
            alglib.minnlccreate(N, _x0, out alglib.minnlcstate state);
            alglib.minnlcsetbc(state, _bndl, _bndu);
            alglib.minnlcsetnlc2(state, _nl, _nu);
            alglib.minnlcsetalgosqp(state);
            alglib.minnlcsetcond3(state, 0, 0, 200);
            return state;
        }

        [Fact]
        public void StopsAtTheAcceptedPointWithUsableMultipliers()
        {
            alglib.minnlcstate state = Create();
            var stop = new FsqpKktStop();
            alglib.ndimensional_rep report = stop.Attach(state);

            double[]? requestedAt = null;
            alglib.minnlcoptimize(state, Function, (x, func, obj) =>
            {
                report(x, func, obj);
                if (stop.StopRequested && requestedAt == null)
                    requestedAt = (double[])x.Clone();
            }, null);
            alglib.minnlcresults(state, out double[] xf, out alglib.minnlcreport rep);

            rep.terminationtype.ShouldEqual(8);
            Assert.NotNull(requestedAt);

            // the iterate the stop was decided on is what comes back, not the discarded trial point after it
            for (int i = 0; i < N; i++)
                xf[i].ShouldEqual(requestedAt![i], 0);

            Assert.True(stop.PrimalInfeasibility <= FsqpKktStop.DEFAULT_CONSTRAINT_TOLERANCE);
            Assert.True(stop.DualInfeasibility <= FsqpKktStop.DEFAULT_OPTIMALITY_TOLERANCE);
            Assert.True(stop.Complementarity <= FsqpKktStop.DEFAULT_OPTIMALITY_TOLERANCE);

            // this is the documented API confirming the same answer: the multipliers it returns satisfy stationarity at x
            Assert.True(MaxAbs(RawLagrangianGradient(xf, rep.lagnlc, rep.lagbc)) <= 1e-6);

            // the signs FsqpKktStop's complementarity relies on: upper bound positive, lower bound negative
            Assert.True(rep.lagnlc[1] > 0);
            Assert.True(rep.lagbc[2] < 0);

            // it is the same solution as running to the bitter end
            alglib.minnlcstate full = Create();
            alglib.minnlcoptimize(full, Function, null, null);
            alglib.minnlcresults(full, out double[] xfull, out alglib.minnlcreport repfull);

            Assert.NotEqual(8, repfull.terminationtype);
            Assert.True(rep.iterationscount < repfull.iterationscount);
            for (int i = 0; i < N; i++)
                xf[i].ShouldEqual(xfull[i], 1e-6);
        }

        [Theory]
        [InlineData(1.0)]
        [InlineData(100.0)] // the solver rescales this objective, which the multipliers have to account for
        public void MeasuresTheDualResidualOfTheAcceptedIterate(double objectiveScale)
        {
            var problem = new FsqpKktStopTests(objectiveScale);
            alglib.minnlcstate state = Create();
            var stop = new FsqpKktStop();
            alglib.ndimensional_rep report = stop.Attach(state, optimalityTolerance: 0);

            int checks = 0;
            double solverScale = 0;
            alglib.minnlcoptimize(state, problem.Function, (x, func, obj) =>
            {
                report(x, func, obj);

                alglib.nlcfsqp.minfsqpstate s = state.innerobj.fsqpsolverstate;

                // only once it has settled down, the unscaled multipliers aren't refreshed by feasibility restoration
                if (double.IsNaN(stop.DualInfeasibility) || stop.PrimalInfeasibility > 1e-3)
                    return;

                double[] raw = problem.RawLagrangianGradient(x, s.replagnlc, s.replagbc);

                // solver's objective scale, at which it reports its multipliers.  Not an API FsqpKktStop itself relies on.
                MaxAbs(raw).ShouldEqual(stop.DualInfeasibility * s.fscaleszzz[0], 1e-10 * objectiveScale);
                checks++;
                solverScale = Max(solverScale, s.fscaleszzz[0]);
            }, null);
            alglib.minnlcresults(state, out double[] _, out alglib.minnlcreport _);

            Assert.True(checks > 2);
            Assert.Equal(objectiveScale > 1, solverScale > 1);
        }

        [Fact]
        public void ZeroToleranceMeasuresButNeverStops()
        {
            alglib.minnlcstate state = Create();
            var stop = new FsqpKktStop();
            alglib.minnlcoptimize(state, Function, stop.Attach(state, optimalityTolerance: 0), null);
            alglib.minnlcresults(state, out double[] _, out alglib.minnlcreport rep);

            Assert.False(stop.StopRequested);
            Assert.NotEqual(8, rep.terminationtype);
            Assert.True(stop.DualInfeasibility <= 1e-5);
            Assert.True(stop.PrimalInfeasibility <= 1e-8);
        }

        [Fact]
        public void TheStartingPointIsNotMeasuredAndOnePassIsNotEnough()
        {
            alglib.minnlcstate state = Create();

            // anything passes, so all this is seeing is when the counting happens
            var stop = new FsqpKktStop();
            alglib.ndimensional_rep report = stop.Attach(state, 1e300, 1e300);

            int reports = 0;
            int firstMeasured = 0;
            int firstRequested = 0;
            alglib.minnlcoptimize(state, Function, (x, func, obj) =>
            {
                reports++;
                report(x, func, obj);
                if (firstMeasured == 0 && !double.IsNaN(stop.DualInfeasibility))
                    firstMeasured = reports;
                if (firstRequested == 0 && stop.StopRequested)
                    firstRequested = reports;
            }, null);
            alglib.minnlcresults(state, out double[] _, out alglib.minnlcreport rep);

            firstMeasured.ShouldEqual(2);
            firstRequested.ShouldEqual(3);
            rep.terminationtype.ShouldEqual(8);
        }

        [Fact]
        public void AFailedPassResetsTheCount()
        {
            alglib.minnlcstate state = Create();

            // loose enough that this problem passes on its way in, then wanders back out, then converges
            const double CONSTRAINT_TOLERANCE = 0.1;
            const double OPTIMALITY_TOLERANCE = 2.0;

            var stop = new FsqpKktStop();
            alglib.ndimensional_rep report = stop.Attach(state, CONSTRAINT_TOLERANCE, OPTIMALITY_TOLERANCE);

            int consecutive = 0;
            bool sawReset = false;
            alglib.minnlcoptimize(state, Function, (x, func, obj) =>
            {
                bool wasRequested = stop.StopRequested;
                report(x, func, obj);
                if (wasRequested || double.IsNaN(stop.DualInfeasibility))
                    return;

                bool passed = stop.PrimalInfeasibility <= CONSTRAINT_TOLERANCE && Max(stop.DualInfeasibility, stop.Complementarity) <= OPTIMALITY_TOLERANCE;
                sawReset = sawReset || (!passed && consecutive > 0);
                consecutive = passed ? consecutive + 1 : 0;

                Assert.Equal(consecutive >= 2, stop.StopRequested);
            }, null);

            // otherwise this test no longer tests anything and the tolerances above need picking again
            Assert.True(sawReset);
            Assert.True(stop.StopRequested);
        }

        // the solvers keep one of these and attach it to each pass, and ChangeOrbitalElement detaches it into a pool in between
        [Fact]
        public void ReattachingStartsOver()
        {
            var stop = new FsqpKktStop();

            alglib.minnlcstate first = Create();
            alglib.ndimensional_rep report = stop.Attach(first);
            alglib.minnlcoptimize(first, Function, report, null);
            alglib.minnlcresults(first, out double[] x1, out alglib.minnlcreport rep1);

            rep1.terminationtype.ShouldEqual(8);
            Assert.True(stop.StopRequested);

            stop.Detach();

            alglib.minnlcstate second = Create();
            Assert.Same(report, stop.Attach(second));

            Assert.False(stop.StopRequested);
            Assert.True(double.IsNaN(stop.PrimalInfeasibility));
            Assert.True(double.IsNaN(stop.DualInfeasibility));
            Assert.True(double.IsNaN(stop.Complementarity));

            alglib.minnlcoptimize(second, Function, report, null);
            alglib.minnlcresults(second, out double[] x2, out alglib.minnlcreport rep2);

            // anything left over from the first solve would stop this one at a different point, or never
            rep2.terminationtype.ShouldEqual(8);
            rep2.iterationscount.ShouldEqual(rep1.iterationscount);
            for (int i = 0; i < N; i++)
                x2[i].ShouldEqual(x1[i], 0);

            // the tolerances don't carry over either
            alglib.minnlcstate third = Create();
            alglib.minnlcoptimize(third, Function, stop.Attach(third, optimalityTolerance: 0), null);
            alglib.minnlcresults(third, out double[] _, out alglib.minnlcreport rep3);

            Assert.False(stop.StopRequested);
            Assert.NotEqual(8, rep3.terminationtype);
        }

        [Fact]
        public void ReattachingToADifferentSizedProblem()
        {
            var stop = new FsqpKktStop();

            alglib.minnlcstate first = Create();
            alglib.minnlcoptimize(first, Function, stop.Attach(first), null);

            // min (x0-1)^2 + (x1-2)^2 subject to x0 + x1 = 1, which is at (0, 1)
            alglib.minnlccreate(2, new[] { 0.5, 0.5 }, out alglib.minnlcstate second);
            alglib.minnlcsetnlc2(second, new[] { 1.0 }, new[] { 1.0 });
            alglib.minnlcsetalgosqp(second);
            alglib.minnlcsetcond3(second, 0, 0, 200);

            alglib.minnlcoptimize(second, (x, fi, jac, obj) =>
            {
                fi[0] = (x[0] - 1) * (x[0] - 1) + (x[1] - 2) * (x[1] - 2);
                fi[1] = x[0] + x[1];
                jac[0, 0] = 2 * (x[0] - 1);
                jac[0, 1] = 2 * (x[1] - 2);
                jac[1, 0] = 1;
                jac[1, 1] = 1;
            }, stop.Attach(second), null);
            alglib.minnlcresults(second, out double[] xf, out alglib.minnlcreport rep);

            Assert.True(rep.terminationtype > 0);
            xf[0].ShouldBeZero(1e-5);
            xf[1].ShouldEqual(1, 1e-5);
            Assert.True(stop.DualInfeasibility <= FsqpKktStop.DEFAULT_OPTIMALITY_TOLERANCE);
        }
    }
}
