/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Threading;
using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    public class Optimizer
    {
        public enum ObjectiveType { MAX_MASS, MAX_ENERGY, MIN_THRUST_ACCEL, MIN_TIME }

        public readonly Problem Problem;
        public readonly PhaseCollection Phases;
        public readonly ITerminal Terminal;
        public readonly ObjectiveType Objective;
        private readonly VariableProxy _vars;

        private readonly alglib.minnlcreport _rep = new alglib.minnlcreport();
        private readonly alglib.ndimensional_sjac _constraintHandle;
        private alglib.minnlcstate _state = new alglib.minnlcstate();

        public CancellationToken TimeoutToken;

        public int Iterations;
        public int TerminationType;
        public double PrimalFeasibility;
        public double InitialPrimalFeasibility;
        public double Cost;
        public Solution? Solution;

        public int    K                   => 3 * N + 1;
        public int    N                   { get; set; } = 7;
        public int    Maxits              { get; set; } = 4000;
        public double SQPTrustRegionLimit { get; set; } = 1e-5;
        public double Epsf                { get; set; } = 0; // 1e-9;
        public double Diffstep            { get; set; } = 1e-9;
        public double Stpmax              { get; set; } = 10;
        public int    OptimizerTimeout    { get; set; } = 120_000; // milliseconds

        public Optimizer(Problem problem, PhaseCollection phases, ITerminal terminal, ObjectiveType objective)
        {
            Phases = phases.DeepCopy();
            Phases.SetControlContinuity();
            Terminal = terminal;
            Objective = objective;
            _vars = new VariableProxy(problem, Phases, Terminal, N);
            Problem = problem;
            _ascentProblem = new AscentProblem(this);
            _constraintHandle = (x, f, j, o) => _ascentProblem.ConstraintFunction(f, j, x, o);
            _xGuess = Array.Empty<double>();
            _nu = Array.Empty<double>();
            _nl = Array.Empty<double>();
        }

        private void CalculatePrimalFeasibility(double[] f, bool debug = false)
        {
            Cost = f[0];
            PrimalFeasibility = 0;
            for (int i = 1; i < f.Length; i++)
            {
                double upper = f[i] - _nu[i - 1];
                double lower = _nl[i - 1] - f[i];

                if (upper > 0)
                {
                    if (upper > 1e-5 && debug)
                        DebugPrint($"constraint violation {i}: {f[i]} upper limit: {_nu[i - 1]} ({_ascentProblem.ConstraintNames[i]})");
                    PrimalFeasibility += upper * upper;
                }
                else if (lower > 0)
                {
                    if (lower > 1e-5 && debug)
                        DebugPrint($"constraint violation {i}: {f[i]} lower limit: {_nl[i - 1]} ({_ascentProblem.ConstraintNames[i]})");
                    PrimalFeasibility += lower * lower;
                }
            }

            PrimalFeasibility = Sqrt(PrimalFeasibility);
        }

        private (double t0, double oldt0) TranscribePhasesFromOldSolution(int phaseStart, int phaseLimit, double t0, Solution oldSolution, double oldt0, double oldtf)
        {
            double tbt = 0;
            double oldtbt = oldtf - oldt0;
            double frac = 1.0;

            if (!Phases[phaseStart].Coast)
            {
                for (int p = phaseStart; p < phaseLimit; p++)
                    tbt += Phases[p].Bt;

                frac = oldtbt > tbt ? oldtbt / tbt : 1.0;
            }

            for (int p = phaseStart; p < phaseLimit; p++)
            {
                Phase phase = Phases[p];
                PhaseProxy thisPhase = _vars[p];

                oldtbt = oldtf - oldt0;
                double bt = Phases[p].Coast ? oldtbt : Clamp(phase.Bt, 0, oldtbt / frac);
                double oldbt = bt * frac;
                oldbt = Min(oldbt, oldtf - oldt0);

                double h = bt / N;
                double oldh = oldbt / N;

                double m0 = phase.MassContinuity ? oldSolution.MBar(oldt0) : phase.M0;

                for (int k = 0; k < K; k++)
                {
                    int n = k / 3;
                    double dt = (n + _tau[k % 3]) * h;
                    double olddt = (n + _tau[k % 3]) * oldh;

                    TranscribePoint(oldSolution, k, thisPhase, phase, m0, oldt0 + olddt, dt);
                }

                if (phase.Coast)
                    thisPhase.M.First = m0;

                if (phase.Unguided)
                    thisPhase.U.First = thisPhase.U.First.normalized;

                thisPhase.Bt() = bt;

                t0 += bt;
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

            for (int p = 0; p < Phases.Count; p++)
                if (Phases[p].Coast)
                    coastPhaseIndex = p;

            double t0 = 0;
            double oldt0 = (Problem.T0 - oldSolution.T0) / Problem.Scale.TimeScale;

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
            TranscribePhasesFromOldSolution(coastPhaseIndex + 1, Phases.Count, t0, oldSolution, oldt0, oldtf3);
        }

        private void TranscribePoint(Solution oldSolution, int k, PhaseProxy thisPhase, Phase phase, double m0, double oldt, double dt)
        {
            V3 r = oldSolution.RBar(oldt);
            thisPhase.R[k] = r;

            V3 v = oldSolution.VBar(oldt);
            thisPhase.V[k] = v;

            if (!phase.Coast)
                thisPhase.M[k] = m0 - phase.Mdot * dt;

            V3 u = oldSolution.UBar(oldt);

            if (phase.GuidedCoast)
            {
                // no points to set
            }
            else if (phase.Unguided)
            {
                // pick roughly the midpoint
                if (k < K / 2)
                    thisPhase.U.First = Q3.FromToRotation(V3.forward, u.normalized);
                thisPhase.T.First = phase.Coast ? 0.0 : 1.0;
            }
            else
            {
                if (k % 3 != 0)
                {
                    thisPhase.U[k] = Q3.FromToRotation(V3.forward, u.normalized);
                    thisPhase.T[k] = 1.0;
                }
            }
        }

        private readonly double[] _tau = { 0, 0.5 - Sqrt(3) / 6, 0.5 + Sqrt(3) / 6 };

        // the phases in the old solution must match the phases in this optimizer.
        // variables on the phases may change, but the number and order of phases must not.
        public void TranscribePreviousBootSolution(Solution oldSolution)
        {
            _xGuess = new double[_vars.TotalVariables];
            _vars.WrapVars(_xGuess);

            double t0 = 0;
            double oldt0 = 0;
            for (int p = 0; p < Phases.Count; p++)
            {
                Phase phase = Phases[p];
                PhaseProxy thisPhase = _vars[p];

                double oldbt = oldSolution.BtBar(p, 0);
                double bt = oldbt;
                if (!phase.Coast)
                    bt = Min(oldbt, phase.Bt);

                double h = bt / N;
                double oldh = oldbt / N;

                double m0 = phase.MassContinuity ? oldSolution.MBar(oldt0) : phase.M0;

                for (int k = 0; k < K; k++)
                {
                    int n = k / 3;
                    double dt = (n + _tau[k % 3]) * h;
                    double olddt = (n + _tau[k % 3]) * oldh;

                    TranscribePoint(oldSolution, k, thisPhase, phase, m0, oldt0 + olddt, dt);
                }

                if (phase.Coast)
                    thisPhase.M.First = m0;

                if (phase.Unguided)
                    thisPhase.U.First = thisPhase.U.First.normalized;

                thisPhase.Bt() = bt;

                t0 += bt;
                oldt0 += oldbt;
            }
        }

        private double[] _xGuess;
        private double[] _nu;
        private double[] _nl;
        private readonly AscentProblem _ascentProblem;

        private Solution UnSafeRun()
        {
            double[] x = new double[_vars.TotalVariables];
            double[] bndl = new double[_vars.TotalVariables];
            double[] bndu = new double[_vars.TotalVariables];
            bool[] boxConstrained = new bool[_vars.TotalVariables];
            _nl = new double[_vars.TotalConstraints];
            _nu = new double[_vars.TotalConstraints];
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

            bndu[r0X] = bndl[r0X] = Problem.R0.x;
            bndu[r0Y] = bndl[r0Y] = Problem.R0.y;
            bndu[r0Z] = bndl[r0Z] = Problem.R0.z;
            bndu[v0X] = bndl[v0X] = Problem.V0.x;
            bndu[v0Y] = bndl[v0Y] = Problem.V0.y;
            bndu[v0Z] = bndl[v0Z] = Problem.V0.z;

            boxConstrained[r0X] = true;
            boxConstrained[r0Y] = true;
            boxConstrained[r0Z] = true;
            boxConstrained[v0X] = true;
            boxConstrained[v0Y] = true;
            boxConstrained[v0Z] = true;

            // box constraints on burntime
            for (int p = 0; p < Phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];
                int idx = thisPhase.BtIdx();

                bndl[idx] = Phases[p].MinT;
                bndu[idx] = Phases[p].MaxT;

                if (bndu[idx] <= bndl[idx])
                    boxConstrained[idx] = true;
            }

            for (int p = 0; p < Phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];

                Phase phase = Phases[p];

                if (phase.GuidedCoast)
                    continue;

                if (phase.Unguided)
                {
                    (int idxX, int idxY, int idxZ, int idxW) = thisPhase.U.Idx(0);
                    bndl[idxX] = bndl[idxY] = bndl[idxZ] = bndl[idxW] = -1.2;
                    bndu[idxX] = bndu[idxY] = bndu[idxZ] = bndu[idxW] = 1.2;
                    continue;
                }

                for (int k = 0; k < K; k++)
                {
                    // skip non-collocated points
                    if (k % 3 == 0)
                        continue;
                    (int idxX, int idxY, int idxZ, int idxW) = thisPhase.U.Idx(k);

                    bndl[idxX] = bndl[idxY] = bndl[idxZ] = bndl[idxW] = -1.2;
                    bndu[idxX] = bndu[idxY] = bndu[idxZ] = bndu[idxW] = 1.2;
                }
            }

            // box constraints on stage masses
            for (int p = 0; p < Phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];

                for (int k = 0; k < thisPhase.M.Length; k++)
                {
                    int idx = thisPhase.M.Idx(k);

                    bool doingContinuity = p > 0 && Phases[p].MassContinuity;

                    if (k == 0 && !doingContinuity)
                    {
                        // pin the initial mass if we aren't carrying over from a prior burn-coast-burn
                        bndu[idx] = bndl[idx] = Phases[p].M0;
                        boxConstrained[idx] = true;
                    }
                    else if (k == thisPhase.M.Length - 1 && !Phases[p].AllowShutdown)
                    {
                        // pin the terminal mass if we aren't allowed to shut it down early
                        bndu[idx] = bndl[idx] = Phases[p].MinM;
                        boxConstrained[idx] = true;
                    }
                    else
                    {
                        bndu[idx] = Phases[p].M0;
                        bndl[idx] = Phases[p].MinM;
                    }
                }
            }

            // Initialize constraints to equality constraints
            for (int i = 0; i < _nl.Length; i++)
                _nu[i] = _nl[i] = 0;

            int ci = 0;

            // Dynamic Pressure inequality constraints
            if (Problem.Rho0InvQAlphaMax > 0 && Problem.H0 > 0)
            {
                foreach (Phase p in Phases)
                {
                    if (p.GuidedCoast)
                        continue;

                    for (int i = 0; i < K; i++)
                    {
                        // only at collocation points
                        if (i % 3 == 0)
                            continue;
                        _nu[ci++] = 1.0 / 100.0;
                    }
                }
            }

            if (Problem.Rho0InvQMax > 0 && Problem.H0 > 0)
            {
                for (int p = 0; p < Phases.Count; p++)
                {
                    for (int i = 0; i < K; i++)
                        _nu[ci++] = 1.0 / 100.0;
                }
            }

            // thrust magnitude inequality constraints
            foreach (Phase phase in Phases)
            {
                if (phase.GuidedCoast)
                    continue;

                if (phase.Unguided)
                {
                    _nl[ci] = phase.MinThrottle;
                    _nu[ci++] = 1.0;
                }
                else
                {
                    for (int i = 0; i < K; i++)
                    {
                        // only at collocation points
                        if (i % 3 == 0)
                            continue;
                        _nl[ci] = phase.MinThrottle;
                        _nu[ci++] = 1.0;
                    }
                }
            }

            alglib.sparsecreatecrsempty(_vars.TotalVariables, out alglib.sparsematrix j2);
            _ascentProblem.ConstraintFunction(f, j2, _xGuess, new AscentProblem.ConstraintArgs(true));

            CalculatePrimalFeasibility(f, true);

            DebugPrint($"Initial Cost: {Cost}");
            DebugPrint($"Initial PrimalFeasibility: {PrimalFeasibility}");
            InitialPrimalFeasibility = PrimalFeasibility;

            alglib.minnlccreate(_vars.TotalVariables, _xGuess, out _state);
            alglib.minnlcsetbc(_state, bndl, bndu);
            alglib.minnlcsetnlc2(_state, _nl, _nu);
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
            Iterations = _rep.iterationscount;

            DebugPrint("terminationtype: " + TerminationType);
            DebugPrint("iterations: " + Iterations);

            alglib.minnlcoptguardresults(_state, out alglib.optguardreport ogrep);

            if (ogrep.badgradsuspected)
                if (!DoubleMatrixSparsityValidation(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 5e-2))
                    throw new Exception(
                        $"badgradsuspected:\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, _ascentProblem.ConstraintNames, boxConstrained, 1e-2)}");

            if (ogrep.nonc0suspected)
                throw new Exception("nonc0suspected");

            if (ogrep.nonc1suspected)
                throw new Exception("nonc1suspected");


            alglib.sparsecreatecrsempty(_vars.TotalVariables, out alglib.sparsematrix j);

            _ascentProblem.ConstraintFunction(f, j, x, null);
            CalculatePrimalFeasibility(f, true);

            DebugPrint($"Cost: {Cost}");
            DebugPrint($"PrimalFeasibility: {PrimalFeasibility}");

            _vars.WrapVars(x);
            Solution solution = new SolutionBuilder(N, _vars, Problem, Phases).Build();

            return solution;
        }

        public Solution? Run()
        {
            foreach (Phase phase in Phases)
                DebugPrint(phase.ToString());

            try
            {
                var tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(OptimizerTimeout);
                TimeoutToken = tokenSource.Token;
                Solution = UnSafeRun();
                return Solution;
            }
            catch (OperationCanceledException)
            {
            }

            return null;
        }

        public bool Success() => PrimalFeasibility <= 1e-4;
    }
}
