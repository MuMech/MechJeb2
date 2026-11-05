/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using static System.Math;

namespace MechJebLib.Utils
{
    /// <summary>
    ///     Stops ALGLIB's filter SQP (minnlcsetalgosqp) on feasibility and first-order optimality, like the ConstraintTolerance
    ///     and OptimalityTolerance options of fmincon.  ALGLIB itself can only stop on the change over one step, the trust
    ///     radius or the iteration count.
    ///     Everything is measured in the solver's scaled units: constraints and multipliers as it holds them, and the objective
    ///     normalized by the scale it picks from its gradient norm.  The multipliers are read from the solver state, which is
    ///     public but not a documented API, so FsqpKktStopTests pins down what those fields mean.
    ///     A small Lagrangian gradient does not mean a small objective error: with curvature H the error is about g^2/(2H), and
    ///     the ascent objectives have H around 1e-7.  The optimality tolerance and asking for two passes in a row were chosen on
    ///     the PSG and maneuver tests, where replaying traces showed a single pass at 1e-4 stopping 4.5e-3 short of the optimum.
    /// </summary>
    internal sealed class FsqpKktStop
    {
        // Well under the 1e-10 that ReturnFromMoon and FineTuneClosestApproachToCelestial need before they'll compare the two
        // transfer directions by cost: at 1e-8 they fall back to comparing residuals and pick a different direction.
        public const double DEFAULT_CONSTRAINT_TOLERANCE = 1e-11;
        public const double DEFAULT_OPTIMALITY_TOLERANCE = 3e-5;

        private const int REQUIRED_PASSES = 2;

        private readonly alglib.ndimensional_rep _reportHandle;

        private alglib.minnlcstate? _state;
        private double _constraintTolerance;
        private double _optimalityTolerance;
        private double[] _y = Array.Empty<double>();
        private double[] _g = Array.Empty<double>();
        private bool _seenStartingPoint;
        private int _passes;

        /// <summary>Largest constraint violation at the last accepted iterate, NaN until one has been measured.</summary>
        public double PrimalInfeasibility { get; private set; } = double.NaN;

        /// <summary>Infinity norm of the Lagrangian gradient at the last accepted iterate, NaN until one has been measured.</summary>
        public double DualInfeasibility { get; private set; } = double.NaN;

        /// <summary>Largest |multiplier * slack| at the last accepted iterate, NaN until one has been measured.</summary>
        public double Complementarity { get; private set; } = double.NaN;

        /// <summary>True once termination has been requested from the solver.</summary>
        public bool StopRequested { get; private set; }

        public FsqpKktStop() => _reportHandle = (x, func, obj) => Report();

        /// <summary>
        ///     Starts over for a new solve, turns on iteration reports for the state, and returns the callback to pass to
        ///     minnlcoptimize.  The algorithm has to have been selected already.  One of these can be kept and attached to each
        ///     solve in turn, which allocates nothing unless the problem changes size.
        /// </summary>
        /// <param name="state">the solver state</param>
        /// <param name="constraintTolerance">largest allowed constraint violation</param>
        /// <param name="optimalityTolerance">largest allowed first-order optimality measure, zero only measures and never stops</param>
        public alglib.ndimensional_rep Attach(alglib.minnlcstate state, double constraintTolerance = DEFAULT_CONSTRAINT_TOLERANCE,
            double optimalityTolerance = DEFAULT_OPTIMALITY_TOLERANCE)
        {
            // 4 and 5 are minnlcsetalgosqp and minnlcsetalgosqpbfgs, the others don't fill in fsqpsolverstate
            CheckDebug.True(state.innerobj.solvertype == 4 || state.innerobj.solvertype == 5);

            _state = state;
            _constraintTolerance = constraintTolerance;
            _optimalityTolerance = optimalityTolerance;
            _seenStartingPoint = false;
            _passes = 0;
            PrimalInfeasibility = double.NaN;
            DualInfeasibility = double.NaN;
            Complementarity = double.NaN;
            StopRequested = false;

            alglib.minnlcsetxrep(state, true);
            return _reportHandle;
        }

        /// <summary>Lets go of the solver state, for when this is going to outlive it.</summary>
        public void Detach() => _state = null;

        private void Report()
        {
            alglib.minnlcstate state = _state!;

            // the first report is the starting point, which comes before any QP has produced multipliers
            if (!_seenStartingPoint)
            {
                _seenStartingPoint = true;
                return;
            }

            Measure(state.innerobj.fsqpsolverstate);

            if (!(_optimalityTolerance > 0) || StopRequested)
                return;

            if (PrimalInfeasibility <= _constraintTolerance && Max(DualInfeasibility, Complementarity) <= _optimalityTolerance)
                _passes++;
            else
                _passes = 0;

            if (_passes < REQUIRED_PASSES)
                return;

            // this is only acted on after the next QP and function evaluation, but the solver then discards those and returns
            // the iterate being reported here
            StopRequested = true;
            alglib.minnlcrequesttermination(state);
        }

        // At a report stepk is the accepted iterate, and the multipliers are from the QP which produced it, so this is the usual
        // SQP dual residual of grad L(x[k+1], lambda[k+1]).  The solver keeps its own in tmpcandlaggrad, but that goes stale in
        // feasibility restoration and it includes an augmentation term.
        private void Measure(alglib.nlcfsqp.minfsqpstate s)
        {
            // none of the callers have linear constraints, which would need an s.scla term in the gradient
            CheckDebug.True(s.lccnt == 0);

            if (_y.Length != 1 + s.nnlc)
                _y = new double[1 + s.nnlc];
            if (_g.Length != s.n)
                _g = new double[s.n];

            // row zero of the Jacobian is the objective gradient
            _y[0] = 1.0;
            Array.Copy(s.lagxcmult, s.lccnt, _y, 1, s.nnlc);

            alglib.sparse.sparsemtv(s.stepk.sj, _y, ref _g, null);
            VecOps.Axpy(1.0, s.lagbcmult, _g, s.n);
            DualInfeasibility = Abs(_g[VecOps.Iamax(_g, s.n)]);

            double primal = 0;
            double complementarity = 0;

            for (int i = 0; i < s.nnlc; i++)
            {
                double c = s.stepk.fi[1 + i];
                double lambda = s.lagxcmult[s.lccnt + i];

                if (s.hasnl[i])
                    primal = Max(primal, s.scalednl[i] - c);
                if (s.hasnu[i])
                    primal = Max(primal, c - s.scalednu[i]);

                // a positive multiplier is on the upper bound and a negative one on the lower bound
                if (lambda > 0 && s.hasnu[i])
                    complementarity = Max(complementarity, Abs(lambda * (c - s.scalednu[i])));
                if (lambda < 0 && s.hasnl[i])
                    complementarity = Max(complementarity, Abs(lambda * (c - s.scalednl[i])));
            }

            for (int i = 0; i < s.n; i++)
            {
                double lambda = s.lagbcmult[i];

                if (lambda > 0 && s.hasbndu[i])
                    complementarity = Max(complementarity, Abs(lambda * (s.stepk.x[i] - s.scaledbndu[i])));
                if (lambda < 0 && s.hasbndl[i])
                    complementarity = Max(complementarity, Abs(lambda * (s.stepk.x[i] - s.scaledbndl[i])));
            }

            PrimalInfeasibility = primal;
            Complementarity = complementarity;
        }
    }
}
