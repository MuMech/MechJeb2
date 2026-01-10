/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Diagnostics;
using System.Threading;
using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    public class Optimizer : IDisposable
    {
        public Stopwatch Timer = new Stopwatch();

        public enum Cost { MAX_MASS, MAX_ENERGY, MIN_THRUST_ACCEL, MIN_TIME }

        public enum OptimStatus { CREATED, SUCCESS, CANCELLED, FAILED }

        public readonly  Problem         _problem;
        public readonly  PhaseCollection _phases;
        public readonly  ITerminal       _terminal;
        public readonly  Cost            _cost;
        private readonly VariableProxy   _vars;

        private readonly alglib.minnlcreport      _rep = new alglib.minnlcreport();
        private readonly alglib.ndimensional_sjac _constraintHandle;
        private          alglib.minnlcstate       _state = new alglib.minnlcstate();

        public CancellationToken _timeoutToken;

        public int         Iterations;
        public OptimStatus Status;
        public int         TerminationType;
        public double      PrimalFeasibility;
        public double      Objective;
        public Solution?   Solution;

        public int    _k                  => 2 * N - 1;
        public int    N                   { get; set; } = 8;
        public int    Maxits              { get; set; } = 4000;
        public double SQPTrustRegionLimit { get; set; } = 1e-4;
        public double Epsf                { get; set; } = 0; // 1e-9;
        public double Diffstep            { get; set; } = 1e-9;
        public double Stpmax              { get; set; } = 10;
        public int    OptimizerTimeout    { get; set; } = 120_000; // milliseconds

        public Optimizer(Problem problem, PhaseCollection phases, ITerminal terminal, Cost cost)
        {
            _phases           = phases.DeepCopy();
            _terminal         = terminal;
            _cost             = cost;
            _vars             = new VariableProxy(problem, _phases, _terminal, N);
            _problem          = problem;
            _ascentProblem    = new AscentProblem(this);
            _constraintHandle = (x, f, j, o) => _ascentProblem.ConstraintFunction(f, j, x, o);
            Status            = OptimStatus.CREATED;
            _xGuess           = Array.Empty<double>();
            nu                = Array.Empty<double>();
            nl                = Array.Empty<double>();
        }

        public void Dispose()
        {
        }

        private void CalculatePrimalFeasibility(double[] f, bool debug = false)
        {
            Objective         = f[0];
            PrimalFeasibility = 0;
            for (int i = 1; i < f.Length; i++)
            {
                double upper = f[i] - nu[i - 1];
                double lower = nl[i - 1] - f[i];

                if (upper > 0)
                {
                    if (upper > 1e-5 && debug)
                        DebugPrint($"constraint violation {i}: {f[i]} upper limit: {nu[i - 1]} ({_ascentProblem.ConstraintNames[i]})");
                    PrimalFeasibility += upper * upper;
                }
                else if (lower > 0)
                {
                    if (lower > 1e-5 && debug)
                        DebugPrint($"constraint violation {i}: {f[i]} lower limit: {nl[i - 1]} ({_ascentProblem.ConstraintNames[i]})");
                    PrimalFeasibility += lower * lower;
                }
            }

            PrimalFeasibility = Sqrt(PrimalFeasibility);
        }

        // TODO: somewhere I need a burntime constraint across the massContinuity phase

        private (double t0, double oldt0) TranscribePhasesFromOldSolution(int phaseStart, int phaseLimit, double t0, Solution oldSolution, double oldt0, double oldtf)
        {
            double tbt    = 0;
            double oldtbt = oldtf - oldt0;
            double frac   = 1.0;

            if (!_phases[phaseStart].Coast)
            {
                for (int p = phaseStart; p < phaseLimit; p++)
                    tbt += _phases[p].bt;

                frac = oldtbt > tbt ? oldtbt / tbt : 1.0;
            }

            for (int p = phaseStart; p < phaseLimit; p++)
            {
                Phase      phase     = _phases[p];
                PhaseProxy thisPhase = _vars[p];

                oldtbt = oldtf - oldt0;
                double bt    = _phases[p].Coast ? oldtbt : Clamp(phase.bt, 0, oldtbt / frac);
                double oldbt = bt * frac;
                oldbt = Min(oldbt, oldtf - oldt0);
                double oldh = oldbt / (_k - 1);

                double tf = t0 + bt;
                double h  = bt / (_k - 1);

                double m0   = phase.m0;
                double mdot = -phase.Mdot;

                for (int k = 0; k < _k; k++)
                {
                    double dt    = k * h;
                    double olddt = k * oldh;

                    V3 r = oldSolution.RBar(oldt0 + olddt);
                    thisPhase.R[k] = r;

                    V3 v = oldSolution.VBar(oldt0 + olddt);
                    thisPhase.V[k] = v;

                    V3 u = oldSolution.UBar(oldt0 + olddt);

                    if (phase.GuidedCoast)
                    {
                        if (k == 0)
                            thisPhase.U[0] = u;
                        if (k == _k - 1)
                            thisPhase.U[-1] = u;
                    }
                    else
                    {
                        if (phase.Unguided)
                            thisPhase.U[0] += u;
                        else
                            thisPhase.U[k] = u;
                    }

                    if (!phase.Coast)
                        thisPhase.M[k] = m0 + mdot * dt;
                }

                if (phase.Coast)
                    thisPhase.M[0] = m0;

                if (phase.Unguided)
                    thisPhase.U[0] = thisPhase.U[0].normalized;

                thisPhase.Bt() = tf - t0;

                t0    =  tf;
                oldt0 += oldbt;
            }

            return (t0, oldt0);
        }

        // this makes no assumptions about the phases in the old solution, just that
        // we must be doing burn / coast-burn or / burn-coast-burn, where the burns
        // may be multi-stage.
        public void TranscribePreviousSolution(Solution oldSolution)
        {
            _xGuess = new double[_vars.TotalVariables];
            _vars.WrapVars(_xGuess);

            int coastPhaseIndex = -1;

            for (int p = 0; p < _phases.Count; p++)
                if (_phases[p].Coast)
                    coastPhaseIndex = p;

            double t0    = 0;
            double oldt0 = (_problem.T0 - oldSolution.T0) / _problem.Scale.TimeScale;

            (double oldburn1, double oldcoast, double oldburn2) = oldSolution.TgoBarSplit(oldt0);

            if (coastPhaseIndex > 0)
            {
                // transcribe the first burn phase(s)
                // FIXME: if there's no coast in the oldSolution, guess a coast?
                double oldtf1 = oldt0 + oldburn1;
                (t0, oldt0) = TranscribePhasesFromOldSolution(0, coastPhaseIndex, t0, oldSolution, oldt0, oldtf1);
            }
            else
            {
                // there's no new first burn phase, so skip any first burn phase in the old solution.
                oldt0 += oldburn1;
            }

            if (coastPhaseIndex >= 0)
            {
                // transcribe the coast phase
                double oldtf2 = oldt0 + oldcoast;
                (t0, oldt0) = TranscribePhasesFromOldSolution(coastPhaseIndex, coastPhaseIndex + 1, t0, oldSolution, oldt0, oldtf2);
            }
            else
            {
                // there's no new coast phase, so skip any coast phase in the old solution.
                oldt0 += oldcoast;
            }

            // transcribe the last burn phase(s)
            double oldtf3 = oldt0 + oldburn2;
            TranscribePhasesFromOldSolution(coastPhaseIndex + 1, _phases.Count, t0, oldSolution, oldt0, oldtf3);
        }

        // the phases in the old solution must match the phases in this optimizer.
        // variables on the phases may change, but the number and order of phases must not.
        public void TranscribePreviousBootSolution(Solution oldSolution)
        {
            _xGuess = new double[_vars.TotalVariables];
            _vars.WrapVars(_xGuess);

            double t0    = 0;
            double oldt0 = 0;
            for (int p = 0; p < _phases.Count; p++)
            {
                Phase      phase     = _phases[p];
                PhaseProxy thisPhase = _vars[p];

                double oldbt = oldSolution.BtBar(p, 0);
                double oldtf = oldt0 + oldbt;
                double oldh  = (oldtf - oldt0) / (_k - 1);

                // a previous infinite stage can exceed the burn time, so start by clamping it back
                // down, but we want to end at the same location, so we index into the old solution
                // by steps from the old burn time.
                double bt = oldbt;
                if (!phase.Coast)
                    bt = Min(oldbt, phase.bt);
                double tf = t0 + bt;
                double h  = (tf - t0) / (_k - 1);

                double m0   = phase.MassContinuity ? oldSolution.MBar(t0) : phase.m0;
                double mdot = -phase.Mdot;

                for (int k = 0; k < _k; k++)
                {
                    double dt    = k * h;
                    double olddt = k * oldh;

                    V3 r = oldSolution.RBar(t0 + olddt);
                    thisPhase.R[k] = r;

                    V3 v = oldSolution.VBar(t0 + olddt);
                    thisPhase.V[k] = v;

                    V3 u = oldSolution.UBar(t0 + olddt);

                    if (phase.GuidedCoast)
                    {
                        if (k == 0)
                            thisPhase.U[0] = u;
                        if (k == _k - 1)
                            thisPhase.U[-1] = u;
                    }
                    else if (phase.Unguided)
                    {
                        thisPhase.U[0] += u;
                    }
                    else
                    {
                        thisPhase.U[k] = u;
                    }

                    if (!phase.Coast)
                        thisPhase.M[k] = m0 + mdot * dt;
                }

                if (phase.Coast)
                    thisPhase.M[0] = m0;

                if (phase.Unguided)
                    thisPhase.U[0] = thisPhase.U[0].normalized;

                thisPhase.Bt() = tf - t0;

                t0    = tf;
                oldt0 = oldtf;
            }
        }

        private          double[]      _xGuess;
        private          double[]      nu;
        private          double[]      nl;
        private readonly AscentProblem _ascentProblem;

        private void PreProcess()
        {
            int lastShutdownPhase = -1;

            for (int p = 0; p < _phases.Count; p++)
            {
                Phase phase = _phases[p];

                if (!phase.Coast && phase.AllowShutdown)
                    lastShutdownPhase = p;
            }

            if (lastShutdownPhase >= 0)
                _phases[lastShutdownPhase].AllowInfiniteBurntime = true;
        }

        private Solution UnSafeRun()
        {
            PreProcess();

            double[] x              = new double[_vars.TotalVariables];
            double[] bndl           = new double[_vars.TotalVariables];
            double[] bndu           = new double[_vars.TotalVariables];
            bool[]   boxConstrained = new bool[_vars.TotalVariables];
            nl = new double[_vars.TotalConstraints];
            nu = new double[_vars.TotalConstraints];
            double[] f = new double[_vars.TotalConstraints + 1];

            for (int i = 0; i < bndu.Length; i++)
            {
                bndu[i] = double.PositiveInfinity;
                bndl[i] = double.NegativeInfinity;
            }

            // box constraints on initial conditions

            PhaseProxy firstPhase = _vars[0];
            (int r0X, int r0Y, int r0Z) = firstPhase.R.Idx(0);
            (int v0X, int v0Y, int v0Z) = firstPhase.V.Idx(0);

            bndu[r0X] = bndl[r0X] = _problem.R0.x;
            bndu[r0Y] = bndl[r0Y] = _problem.R0.y;
            bndu[r0Z] = bndl[r0Z] = _problem.R0.z;
            bndu[v0X] = bndl[v0X] = _problem.V0.x;
            bndu[v0Y] = bndl[v0Y] = _problem.V0.y;
            bndu[v0Z] = bndl[v0Z] = _problem.V0.z;

            boxConstrained[r0X] = true;
            boxConstrained[r0Y] = true;
            boxConstrained[r0Z] = true;
            boxConstrained[v0X] = true;
            boxConstrained[v0Y] = true;
            boxConstrained[v0Z] = true;

            // box constraints on burntime
            for (int p = 0; p < _phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];
                int        idx       = thisPhase.BtIdx();
                if (_phases[p].Coast)
                {
                    bndl[idx] = _phases[p].mint;
                    bndu[idx] = _phases[p].maxt;
                }
                else
                {
                    bndl[idx] = _phases[p].AllowShutdown ? 0 : _phases[p].bt;
                    if (_phases[p].AllowInfiniteBurntime)
                        bndu[idx] = _phases[p].Infinite ? double.PositiveInfinity : 0.999 * _phases[p].tau;
                    else
                        bndu[idx] = _phases[p].bt;
                }

                if (bndu[idx] <= bndl[idx])
                    boxConstrained[idx] = true;
            }

            // FIXME: set box path boundaries on control
            for (int p = 0; p < _phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];

                for (int k = 0; k < thisPhase.U.Length; k++)
                {
                    (int idxX, int idxY, int idxZ) = thisPhase.U.Idx(k);

                    bndl[idxX] = bndl[idxY] = bndl[idxZ] = -2.0;
                    bndu[idxX] = bndu[idxY] = bndu[idxZ] = 2.0;
                }
            }

            // box constraints on stage masses
            for (int p = 0; p < _phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];

                for (int k = 0; k < thisPhase.M.Length; k++)
                {
                    int idx = thisPhase.M.Idx(k);

                    bool doingContinuity = p > 0 && _phases[p].MassContinuity;

                    if ((_phases[p].Infinite || k == 0) && !doingContinuity)
                    {
                        bndu[idx] = bndl[idx] = _phases[p].m0;

                        boxConstrained[idx] = true;
                    }
                    else
                    {
                        bndu[idx] = _phases[p].m0;
                        bndl[idx] = 0;
                    }
                }
            }

            // Initialize constraints to equality constraints
            for (int i = 0; i < nl.Length; i++)
                nu[i] = nl[i] = 0;

            // Dynamic Pressure inequality constraints
            int inequalityPoints = 0;
            if (_problem.Rho0InvQAlphaMax > 0 && _problem.H0 > 0)
                inequalityPoints += _k;
            if (_problem.Rho0InvQMax > 0 && _problem.H0 > 0)
                inequalityPoints += _k;

            for (int i = nl.Length - inequalityPoints * _phases.Count; i < nl.Length; i++)
                    nu[i] = 1.0;

            alglib.sparsecreatecrsempty(_vars.TotalVariables, out alglib.sparsematrix j2);
            _ascentProblem.ConstraintFunction(f, j2, _xGuess, new AscentProblem.ConstraintArgs(true));

            CalculatePrimalFeasibility(f, true);

            DebugPrint($"Initial Cost: {Objective}");
            DebugPrint($"Initial PrimalFeasibility: {PrimalFeasibility}");

            alglib.minnlccreate(_vars.TotalVariables, _xGuess, out _state);
            alglib.minnlcsetbc(_state, bndl, bndu);
            alglib.minnlcsetnlc2(_state, nl, nu);
            //alglib.minnlcsetstpmax(_state, Stpmax);
            alglib.minnlcsetalgosqp(_state);
            //alglib.minnlcsetalgogipm2(_state);
            alglib.minnlcsetcond3(_state, Epsf, SQPTrustRegionLimit, Maxits);

#if DEBUG
            alglib.minnlcoptguardgradient(_state, Diffstep);
#endif

            //alglib.minnlcoptguardsmoothness(_state, 1);
            //alglib.trace_file("SQP,PREC.F6", "/tmp/trace.log");

            alglib.minnlcoptimize(_state, _constraintHandle, null, null);
            alglib.minnlcresultsbuf(_state, ref x, _rep);

            TerminationType = _rep.terminationtype;
            Iterations      = _rep.iterationscount;

            DebugPrint("terminationtype: " + TerminationType);
            DebugPrint("iterations: " + Iterations);

            alglib.minnlcoptguardresults(_state, out alglib.optguardreport ogrep);

            if (ogrep.badgradsuspected)
                if (!DoubleMatrixSparsityValidation(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 1e-2))
                    throw new Exception(
                        $"badgradsuspected: constraint: {ogrep.badgradfidx} ({_ascentProblem.ConstraintNames[ogrep.badgradfidx]}) variable: {ogrep.badgradvidx} user: {ogrep.badgraduser[ogrep.badgradfidx, ogrep.badgradvidx]:e} != numerical: {ogrep.badgradnum[ogrep.badgradfidx, ogrep.badgradvidx]:e}\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 1e-2)}");

            if (ogrep.nonc0suspected)
                throw new Exception("nonc0suspected");

            if (ogrep.nonc1suspected)
                throw new Exception("nonc1suspected");


            alglib.sparsecreatecrsempty(_vars.TotalVariables, out alglib.sparsematrix j);

            _ascentProblem.ConstraintFunction(f, j, x, null);
            CalculatePrimalFeasibility(f, true);

            DebugPrint($"Cost: {Objective}");
            DebugPrint($"PrimalFeasibility: {PrimalFeasibility}");

            Status = PrimalFeasibility > 1e-4 ? OptimStatus.FAILED : OptimStatus.SUCCESS;

            _vars.WrapVars(x);
            Solution solution = new SolutionBuilder(N, _vars, _problem, _phases).Build();

            return solution;
        }

        public Solution? Run()
        {
            foreach (Phase phase in _phases)
                DebugPrint(phase.ToString());

            try
            {
                var tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(OptimizerTimeout);
                _timeoutToken = tokenSource.Token;
                Solution      = UnSafeRun();
                return Solution;
            }
            catch (OperationCanceledException)
            {
                Status = OptimStatus.CANCELLED;
            }

            return null;
        }

        public bool Success() => Status == OptimStatus.SUCCESS;
    }
}
