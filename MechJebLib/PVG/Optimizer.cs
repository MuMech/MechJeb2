/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG
{
    public partial class Optimizer : IDisposable
    {
        public double ZnormTerminationLevel = 1e-9;
        public double Znorm;
        public int    MAXITS  { get; set; } = 200000; // rely more on the optimizertimeout instead of iterations
        public double LMEPSX  { get; set; } = EPS;    // rely more on manual termination at znorm=1e-9
        public double NLCEPSX { get; set; } = 1e-3;

        public double DIFFSTEP { get; set; } = 1e-10;

        //public double      STPMAX = 1e-4;
        public int         OptimizerTimeout { get; set; } = 2000000; // milliseconds (FIXME: this is a very long time)
        public int         TerminationType;
        public int         Iterations;
        public OptimStatus Status;

        private readonly SegmentCollection        _segments;
        private readonly Problem                  _problem;
        private readonly alglib.minnlcreport      _nlcrep   = new alglib.minnlcreport();
        private readonly alglib.minlmreport       _minlmrep = new alglib.minlmreport();
        private readonly alglib.ndimensional_jac _objectiveAndConstraintsHandle;
        private readonly alglib.ndimensional_fvec _equalityConstraintsHandle;
        private          alglib.minnlcstate       _nlcstate   = new alglib.minnlcstate();
        private          alglib.minlmstate        _minlmstate = new alglib.minlmstate();
        private          double[]                 _x0;
        private          double[]                 _x;
        private          double[]                 _z;
        private          double[]                 _bndl;
        private          double[]                 _bndu;

        public enum OptimStatus { CREATED, BOOTSTRAPPED, SUCCESS, FAILED }

        private Optimizer(Problem problem, IEnumerable<Phase> phases)
        {
            _problem                       = problem;
            _segments                      = new SegmentCollection(problem, phases);
            _objectiveAndConstraintsHandle = ObjectiveAndConstraintsFunction;
            _equalityConstraintsHandle     = EqualityConstraintsFunction;
            Status                         = OptimStatus.CREATED;
        }

        private bool _terminating;

        internal void EqualityConstraintsFunction(double[] yin, double[] zout, object? o)
        {
            if (_terminating)
                return;

            _timeoutToken.ThrowIfCancellationRequested();

            _segments.UnpackVariables(yin);
            _segments.MultipleShooting();
            Znorm = _segments.CalculateEqualityConstraints(zout);

            if (Znorm < ZnormTerminationLevel)
            {
                alglib.minnlcrequesttermination(_nlcstate);
                _terminating = true;
            }
        }

        internal void EqualityConstraintsFunction(double[] yin, double[] zout, double [,] jac, object? o)
        {
            if (_terminating)
                return;

            _timeoutToken.ThrowIfCancellationRequested();

            _segments.UnpackVariables(yin);
            _segments.MultipleShooting();
            Znorm = _segments.CalculateEqualityConstraints(zout, jac);

            if (Znorm < ZnormTerminationLevel)
            {
                alglib.minnlcrequesttermination(_nlcstate);
                _terminating = true;
            }
        }

        internal void ObjectiveAndConstraintsFunction(double[] yin, double[] zout, object o)
        {
            _timeoutToken.ThrowIfCancellationRequested();

            _segments.UnpackVariables(yin);
            _segments.MultipleShooting();

            Znorm = _segments.CalculateObjectiveAndConstraints(zout);
        }

        internal void ObjectiveAndConstraintsFunction(double[] yin, double[] zout, double[,] jac, object o)
        {
            _timeoutToken.ThrowIfCancellationRequested();

            _segments.UnpackVariables(yin);
            _segments.MultipleShooting();

            Znorm = _segments.CalculateObjectiveAndConstraints(zout, jac);
        }

        private CancellationToken _timeoutToken;

        private void FindFeasiblePoint()
        {
            _terminating = false;

            alglib.minlmcreatev(_segments.EqualityConstraintLength(), _x0, DIFFSTEP, out _minlmstate);
            alglib.minlmsetcond(_minlmstate, LMEPSX, MAXITS);
            alglib.minlmsetbc(_minlmstate, _bndl, _bndu);
            alglib.minlmoptimize(_minlmstate, _equalityConstraintsHandle, null, null);
            alglib.minlmresultsbuf(_minlmstate, ref _x, _minlmrep);

            TerminationType = _minlmrep.terminationtype;
            Iterations      = _minlmrep.iterationscount;

            Print("LM terminationtype: " + TerminationType);
            Print("LM iterations: " + Iterations);

            //ObjectiveAndConstraintsFunction(_x, _z, null);
            Print("Znorm: " + Znorm);
        }

        private void SolveWithObjective()
        {
            bool optguard = true;

            alglib.trace_file("SQP,SQP.PROBING,PREC.F6", "/tmp/trace.log");

            //alglib.minnlccreatef(_x, DIFFSTEP, out _nlcstate);
            alglib.minnlccreate(_x, out _nlcstate);

            alglib.minnlcsetbc(_nlcstate, _bndl, _bndu);
            //alglib.minnlcsetstpmax(_state, STPMAX);
            //alglib.minnlcsetalgoslp(_state);
            alglib.minnlcsetalgosqp(_nlcstate);
            alglib.minnlcsetcond(_nlcstate, NLCEPSX, MAXITS);
            alglib.minnlcsetnlc(_nlcstate, _segments.EqualityConstraintLength(), _segments.InequalityConstraintLength());

            if (optguard)
            {
                alglib.minnlcoptguardsmoothness(_nlcstate);
                alglib.minnlcoptguardgradient(_nlcstate, DIFFSTEP);
            }

            alglib.minnlcoptimize(_nlcstate, _objectiveAndConstraintsHandle, null, null);
            alglib.minnlcresultsbuf(_nlcstate, ref _x, _nlcrep);

            if (optguard)
            {
                alglib.minnlcoptguardresults(_nlcstate, out alglib.optguardreport ogrep);

                if (ogrep.badgradsuspected)
                    throw new Exception(
                        $"badgradsuspected: {ogrep.badgradfidx},{ogrep.badgradvidx}\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, 1e-2)}");

                if (ogrep.nonc0suspected || ogrep.nonc1suspected)
                    throw new Exception("alglib optguard caught an error, i should report better on errors now");
            }

            TerminationType = _nlcrep.terminationtype;
            Iterations      = _nlcrep.iterationscount;

            Print("NLC terminationtype: " + TerminationType);
            Print("NLC iterations: " + Iterations);

            //ObjectiveAndConstraintsFunction(_x, _z, null);
            Print("Znorm: " + Znorm);
        }

        private void UnSafeRun()
        {
            int zlen = _segments.ObjectiveAndConstraintsLength();
            int xlen = _segments.VariableLength();

            _x0   = new double[xlen];
            _x    = new double[xlen];
            _z    = new double[zlen];
            _bndu = new double[xlen];
            _bndl = new double[xlen];

            _segments.BoundaryConditions(_bndl, _bndu);
            _segments.PackVariables(_x0);

            FindFeasiblePoint();

            if (Znorm > ZnormTerminationLevel)
                return;

            if (_segments.NeedsObjectiveFunction())
                SolveWithObjective();
        }

        public Optimizer Run()
        {
            if (Status != OptimStatus.BOOTSTRAPPED)
                throw new Exception("run should only be called on BOOTSTRAPPED optimizer");

            try
            {
                var tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(OptimizerTimeout);
                _timeoutToken = tokenSource.Token;
                UnSafeRun();
            }
            catch (OperationCanceledException)
            {
                Print("optimizer timeout");
            }

            Status = Success() ? OptimStatus.SUCCESS : OptimStatus.FAILED;

            return this;
        }

        public Optimizer Bootstrap(V3 pv0, V3 pr0)
        {
            if (Status != OptimStatus.CREATED)
                throw new Exception("bootstrap should only be called on CREATED optimizer");

            var x0 = new IntegratorRecord
            {
                R  = _problem.R0,
                V  = _problem.V0,
                Pv = pv0,
                Pr = pr0,
                M  = _problem.M0,
                DV = 0
            };

            _segments.UnpackBurntimesFromPhases();

            _segments.SingleShooting(x0);

            Status = OptimStatus.BOOTSTRAPPED;

            return this;
        }

        // FIXME: this still needs to be a much better algorithm
        public Optimizer Bootstrap(Solution solution)
        {
            if (Status != OptimStatus.CREATED)
                throw new Exception("bootstrap should only be called on CREATED optimizer");

            var x0 = new IntegratorRecord
            {
                R  = _problem.R0,
                V  = _problem.V0,
                Pv = solution.Pv(_problem.T0),
                Pr = solution.Pr(_problem.T0),
                M  = _problem.M0,
                DV = 0
            };

            _segments.UnpackBurntimesFromPhases();

            _segments.SingleShooting(x0);

            Status = OptimStatus.BOOTSTRAPPED;

            return this;
        }

        public Solution GetSolution()
        {
            if (Status != OptimStatus.SUCCESS)
                throw new Exception("getting solution from bad/failed optimizer state");

            return _segments.GetSolution();
        }

        // even if we didn't terminate successfully, we're close enough to a zero to use the solution
        public bool Success() => Znorm < 1e-5;

        public static OptimizerBuilder Builder() => new OptimizerBuilder();

        public void Dispose()
        {
            // FIXME: ObjectPooling
        }
    }
}
