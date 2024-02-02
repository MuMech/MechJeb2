/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PVG
{
    public partial class Optimizer : IDisposable
    {
        private const double PM0 = 1;

        public readonly double      ZnormTerminationLevel = 1e-9;
        public          double      Znorm;
        public          int         MaxIter          { get; set; } = 200000; // rely more on the optimizertimeout instead of iterations
        public          double      LmEpsx           { get; set; } = EPS;    // rely more on manual termination at znorm=1e-9
        public          double      LmDiffStep       { get; set; } = 1e-10;
        public          int         OptimizerTimeout { get; set; } = 5000; // milliseconds
        public          int         LmStatus;
        public          int         LmIterations;
        public          OptimStatus Status;

        private readonly Problem                  _problem;
        private readonly List<Phase>              _phases;
        private readonly List<Vn>                 _initial  = new List<Vn>();
        private readonly List<Vn>                 _terminal = new List<Vn>();
        private readonly List<Vn>                 _residual = new List<Vn>();
        private          int                      lastPhase => _phases.Count - 1;
        private readonly alglib.minlmreport       _rep = new alglib.minlmreport();
        private readonly alglib.ndimensional_fvec _residualHandle;
        private          alglib.minlmstate        _state = new alglib.minlmstate();

        public enum OptimStatus { CREATED, BOOTSTRAPPED, SUCCESS, FAILED }

        private Optimizer(Problem problem, IEnumerable<Phase> phases)
        {
            _phases         = new List<Phase>(phases);
            _problem        = problem;
            _residualHandle = ResidualFunction;
            Status          = OptimStatus.CREATED;
        }

        private void ExpandArrays()
        {
            while (_initial.Count < _phases.Count)
                _initial.Add(Vn.Rent(OutputLayout.OUTPUT_LAYOUT_LEN));
            while (_terminal.Count < _phases.Count)
                _terminal.Add(Vn.Rent(OutputLayout.OUTPUT_LAYOUT_LEN));
            while (_residual.Count < _phases.Count)
                _residual.Add(Vn.Rent(ResidualLayout.RESIDUAL_LAYOUT_LEN));
        }

        private void CopyToInitial(double[] yin)
        {
            for (int i = 0; i < yin.Length; i++)
                _initial[i / OutputLayout.OUTPUT_LAYOUT_LEN][i % OutputLayout.OUTPUT_LAYOUT_LEN] = yin[i];
        }

        private double CalcBTConstraint(int p)
        {
            var yfp = OutputLayout.CreateFrom(_terminal[p]);
            var y0p = InputLayout.CreateFrom(_initial[p]);
            var y0 = InputLayout.CreateFrom(_initial[0]);
            var yf = OutputLayout.CreateFrom(_terminal[lastPhase]);

            // handle coasts
            if (_phases[p].Coast && _phases[p].OptimizeTime)
            {
                var y0p1 = InputLayout.CreateFrom(_initial[p+1]);

                double k = _phases[lastPhase].c * yf.PV.magnitude / ( yf.M * yf.Pm);

                return y0p1.PV.magnitude / y0p1.M - k * y0p1.Pm / _phases[p+1].c;
            }

            if (_phases[p].OptimizeTime)
            {
                    //if (_phases[lastPhase].BeforeCoast) // no coast so no mass costate
                        return y0.CostateMagnitude - 1;
                    return H(yf, lastPhase);

                /*
                if (_phases[p].DropMass > 0 && p < lastPhase)
                {
                    throw new Exception("this doesn't work yet");
                    var y0p1 = OutputLayout.CreateFrom(_initial[p + 1]);
                    return H(yfp, p) - H(y0p1, p + 1);
                }
                */

                // any other optimized burntimes
                return yfp.H0 - y0p.H0;
            }

            // fixed burntime
            return y0p.Bt - _phases[p].bt;
        }

        private double H(OutputLayout y, int p) => y.H0 + _phases[p].thrust * (y.PV.magnitude / y.M - y.Pm / _phases[p].c);

        private void BaseResiduals()
        {
            var y0 = InputLayout.CreateFrom(_initial[0]);
            var yf = OutputLayout.CreateFrom(_terminal[lastPhase]);
            var z = ResidualLayout.CreateFrom(_residual[0]);

            z.R  = y0.R - _problem.R0;
            z.V  = y0.V - _problem.V0;
            z.M  = y0.M - _problem.M0;
            if (_phases[0].Coast && _phases[0].OptimizeTime)
                z.Pm = y0.Pm - PM0;
            else
                z.Pm = y0.Pm;
            z.Terminal = _problem.Terminal.TerminalConstraints(yf);
            z.Bt       = CalcBTConstraint(0);
            z.CopyTo(_residual[0]);
        }

        private void ContinuityConditions()
        {
            for (int p = 1; p < _phases.Count; p++)
            {
                var y0 = InputLayout.CreateFrom(_initial[p]);
                var yf = OutputLayout.CreateFrom(_terminal[p - 1]);
                var z = ContinuityLayout.CreateFrom(_residual[p]);

                z.R  = yf.R - y0.R;
                z.V  = yf.V - y0.V;
                z.Pv = yf.PV - y0.PV;
                z.Pr = yf.PR - y0.PR;

                if (_phases[p].BeforeCoast)
                    z.Pm = y0.Pm;
                else if (_phases[p].Coast)
                    z.Pm = y0.Pm - PM0;
                else
                    z.Pm = yf.Pm - y0.Pm;

                if (_phases[p].MassContinuity)
                    z.M = yf.M - (_phases[p - 1].DropMass + y0.M);
                else
                    z.M = _phases[p].m0 - y0.M;

                z.Bt = CalcBTConstraint(p);

                z.CopyTo(_residual[p]);
            }
        }

        private void CalculateResiduals()
        {
            BaseResiduals();
            ContinuityConditions();
        }

        private void CopyToZ(double[] z)
        {
            for (int i = 0; i < z.Length; i++)
            {
                z[i] = _residual[i / ResidualLayout.RESIDUAL_LAYOUT_LEN][i % ResidualLayout.RESIDUAL_LAYOUT_LEN];
            }
        }

        private void CalculateZnorm(double[] z)
        {
            Znorm = 0;
            for (int i = 0; i < z.Length; i++)
            {
                Znorm += z[i] * z[i];
            }

            Znorm = Sqrt(Znorm);
        }

        private bool _terminating;

        internal void ResidualFunction(double[] yin, double[] zout, object? o)
        {
            if (_terminating)
                return;

            _timeoutToken.ThrowIfCancellationRequested();

            CopyToInitial(yin);
            Shooting();
            // need to backwards integrate the mass costate here
            CalculateResiduals();
            CopyToZ(zout);
            CalculateZnorm(zout);

            if (Znorm < ZnormTerminationLevel)
            {
                alglib.minlmrequesttermination(_state);
                _terminating = true;
            }
        }

        private CancellationToken _timeoutToken;

        private void UnSafeRun()
        {
            _terminating = false;

            ExpandArrays();

            double[] yGuess = new double[_phases.Count * InputLayout.INPUT_LAYOUT_LEN];
            double[] yNew = new double[_phases.Count * InputLayout.INPUT_LAYOUT_LEN];
            double[] z = new double[_phases.Count * ResidualLayout.RESIDUAL_LAYOUT_LEN];
            double[] bndu = new double[_phases.Count * InputLayout.INPUT_LAYOUT_LEN];
            double[] bndl = new double[_phases.Count * InputLayout.INPUT_LAYOUT_LEN];

            for (int i = 0; i < bndu.Length; i++)
            {
                bndu[i] = double.PositiveInfinity;
                bndl[i] = double.NegativeInfinity;
            }

            for (int i = 0; i < yGuess.Length; i++)
                yGuess[i] = _initial[i / InputLayout.INPUT_LAYOUT_LEN][i % InputLayout.INPUT_LAYOUT_LEN];

            for (int i = 0; i < _phases.Count; i++)
            {
                // pin the maximum time of any finite burn phase to below the tau value of the stage
                if (!_phases[i].Coast && !_phases[i].Infinite && _phases[i].OptimizeTime)
                    bndu[i * InputLayout.INPUT_LAYOUT_LEN + InputLayout.BT_INDEX] = _phases[i].tau * (1 - 1e-9);

                // pin the time of any phase which isn't allowed to be optimized
                if (!_phases[i].OptimizeTime)
                    bndu[i * InputLayout.INPUT_LAYOUT_LEN + InputLayout.BT_INDEX] =
                        bndl[i * InputLayout.INPUT_LAYOUT_LEN + InputLayout.BT_INDEX] = _phases[i].bt;

                // pin the m0 of the stage based on the value computed for the phase
                // FIXME: stage and a half or coasts-within-stages would require dropping this.
                bndl[i * InputLayout.INPUT_LAYOUT_LEN + InputLayout.M_INDEX] = _phases[i].m0;
                bndu[i * InputLayout.INPUT_LAYOUT_LEN + InputLayout.M_INDEX] = _phases[i].m0;
            }

            // pin r0 and v0 by box equality constraints in the optimizer
            bndl[InputLayout.RX_INDEX] = bndu[InputLayout.RX_INDEX] = _problem.R0.x;
            bndl[InputLayout.RY_INDEX] = bndu[InputLayout.RY_INDEX] = _problem.R0.y;
            bndl[InputLayout.RZ_INDEX] = bndu[InputLayout.RZ_INDEX] = _problem.R0.z;
            bndl[InputLayout.VX_INDEX] = bndu[InputLayout.VX_INDEX] = _problem.V0.x;
            bndl[InputLayout.VY_INDEX] = bndu[InputLayout.VY_INDEX] = _problem.V0.y;
            bndl[InputLayout.VZ_INDEX] = bndu[InputLayout.VZ_INDEX] = _problem.V0.z;

            alglib.minlmcreatev(ResidualLayout.RESIDUAL_LAYOUT_LEN * _phases.Count, yGuess, LmDiffStep, out _state);
            alglib.minlmsetbc(_state, bndl, bndu);
            alglib.minlmsetcond(_state, LmEpsx, MaxIter);
            alglib.minlmoptimize(_state, _residualHandle, null, null);
            alglib.minlmresultsbuf(_state, ref yNew, _rep);

            LmStatus     = _rep.terminationtype;
            LmIterations = _rep.iterationscount;

            if (_rep.terminationtype != 8)
                ResidualFunction(yNew, z, null);
        }

        public Optimizer Run()
        {
            if (Status != OptimStatus.BOOTSTRAPPED)
                throw new Exception("run should only be called on BOOTSTRAPPED optimizer");

            try
            {
                var tokenSource = new CancellationTokenSource(); // FIXME: bit of garbage here
                tokenSource.CancelAfter(OptimizerTimeout);
                _timeoutToken = tokenSource.Token;
                UnSafeRun();
            }
            catch (OperationCanceledException)
            {
            }

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(_phases[p].ToString());
            }

            Print("solved initial: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_initial[p]));
            }

            Print("solved terminal: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_terminal[p]));
            }

            Print("solved residuals: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_residual[p]));
            }

            Status = Success() ? OptimStatus.SUCCESS : OptimStatus.FAILED;

            return this;
        }

        private void Shooting(Solution? solution = null)
        {
            using var integArray = Vn.Rent(OutputLayout.OUTPUT_LAYOUT_LEN);

            var y0 = new InputLayout();

            double t0 = 0;
            double lastDv = 0;

            for (int p = 0; p <= lastPhase; p++)
            {
                Phase phase = _phases[p];

                y0.CopyFrom(_initial[p]);

                if (p == 0)
                {
                    y0.R  = _problem.R0;
                    y0.V  = _problem.V0;
                }

                if (phase.BeforeCoast)
                    y0.Pm = 0;
                if (phase.Coast && phase.OptimizeTime)
                    y0.Pm = PM0;

                y0.M = phase.m0;

                y0.CopyTo(_initial[p]);

                double bt = phase.OptimizeTime ? y0.Bt : phase.bt;
                double tf = t0 + bt;
                phase.u0 = GetIntertialHeading(p, y0.PV);

                var y0p = new OutputLayout(y0);
                y0p.DV = lastDv;
                y0p.CopyTo(integArray);

                if (solution != null)
                    phase.Integrate(integArray, _terminal[p], t0, tf, solution);
                else
                    phase.Integrate(integArray, _terminal[p], t0, tf);

                var yf = OutputLayout.CreateFrom(_terminal[p]);
                lastDv = yf.DV;

                t0 += bt;
            }
        }

        /// <summary>
        /// This does single-shooting given the initial conditions and a guess of the costate which is internally feasible but does
        /// not meet the terminal conditions.
        /// </summary>
        /// <param name="pv0"></param>
        /// <param name="pr0"></param>
        public Optimizer Bootstrap(V3 pv0, V3 pr0)
        {
            if (Status != OptimStatus.CREATED)
                throw new Exception("bootstrap should only be called on CREATED optimizer");

            ExpandArrays();

            using var integArray = Vn.Rent(OutputLayout.OUTPUT_LAYOUT_LEN);

            double t0 = 0;
            double lastDv = 0;

            for (int p = 0; p <= lastPhase; p++)
            {
                Phase phase = _phases[p];

                var y0 = new InputLayout();

                if (p == 0)
                {
                    y0.R  = _problem.R0;
                    y0.V  = _problem.V0;
                    y0.PV = pv0;
                    y0.PR = pr0;
                    y0.Bt = phase.bt;
                }
                else
                {
                    _terminal[p - 1].CopyTo(_initial[p]);
                    y0.CopyFrom(_initial[p]);
                    y0.Bt = phase.bt;
                }

                if (phase.BeforeCoast)
                    y0.Pm = 0;
                if (phase.Coast && phase.OptimizeTime)
                    y0.Pm = PM0;

                y0.M = phase.m0;

                y0.CopyTo(_initial[p]);

                double tf = t0 + y0.Bt;
                phase.u0 = GetIntertialHeading(p, y0.PV);

                var y0p = new OutputLayout(y0);
                y0p.DV = lastDv;
                y0p.CopyTo(integArray);

                phase.Integrate(integArray, _terminal[p], t0, tf);

                var yf = OutputLayout.CreateFrom(_terminal[p]);
                lastDv = yf.DV;

                t0 += tf;
            }

            CalculateResiduals();

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(_phases[p].ToString());
            }

            Print("bootstrap1 initial: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_initial[p]));
            }

            Print("bootstrap1 terminal: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_terminal[p]));
            }

            Print("bootstrap1 residuals: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_residual[p]));
            }

            Status = OptimStatus.BOOTSTRAPPED;

            return this;
        }

        /// <summary>
        /// This does multiple shooting using an old solution state as the initial guess.
        /// </summary>
        /// <param name="solution"></param>
        public Optimizer Bootstrap(Solution solution)
        {
            if (Status != OptimStatus.CREATED)
                throw new Exception("bootstrap should only be called on CREATED optimizer");

            ExpandArrays();

            using var integArray = Vn.Rent(OutputLayout.OUTPUT_LAYOUT_LEN);

            //double tbar = solution.Tbar(_problem.t0);

            double t0 = 0;
            double lastDv = 0;

            for (int p = 0; p <= lastPhase; p++)
            {
                Phase phase = _phases[p];

                var y0 = new InputLayout();

                if (p == 0)
                {
                    y0.R  = _problem.R0;
                    y0.V  = _problem.V0;
                    y0.Bt = phase.bt;
                    y0.PV = solution.Pv(_problem.T0);
                    y0.PR = solution.Pr(_problem.T0);
                }
                else
                {
                    _terminal[p - 1].CopyTo(_initial[p]);
                    y0.CopyFrom(_initial[p]);
                    y0.Bt = phase.bt;
                }

                if (phase.BeforeCoast)
                    y0.Pm = 0;
                if (phase.Coast && phase.OptimizeTime)
                    y0.Pm = PM0;

                y0.M = phase.m0;

                y0.CopyTo(_initial[p]);

                double tf = t0 + y0.Bt;

                phase.u0 = GetIntertialHeading(p, y0.PV);

                var y0p = new OutputLayout(y0);
                y0p.DV = lastDv;
                y0p.CopyTo(integArray);

                phase.Integrate(integArray, _terminal[p], t0, tf);

                var yf = OutputLayout.CreateFrom(_terminal[p]);
                lastDv = yf.DV;

                t0 += tf;
            }

            CalculateResiduals();

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(_phases[p].ToString());
            }

            Print("bootstrap2 initial: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_initial[p]));
            }

            Print("bootstrap2 terminal: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_terminal[p]));
            }

            Print("bootstrap2 residuals: ");

            for (int p = 0; p <= lastPhase; p++)
            {
                Print(DoubleArrayString(_residual[p]));
            }

            Status = OptimStatus.BOOTSTRAPPED;

            return this;
        }

        private V3 GetIntertialHeading(int p, V3 pv0)
        {
            if (p == 0)
                return _problem.U0;

            if (_phases[p - 1].Unguided)
                return _phases[p - 1].u0;

            return pv0.normalized;
        }

        public Solution GetSolution()
        {
            if (Status != OptimStatus.SUCCESS)
                throw new Exception("getting solution from bad/failed optimizer state");

            var solution = new Solution(_problem);

            Shooting(solution);

            return solution;
        }

        public bool Success() =>
            // even if we didn't terminate successfully, we're close enough to a zero to use the solution
            Znorm < 1e-5;

        public static OptimizerBuilder Builder() => new OptimizerBuilder();

        public void Dispose()
        {
            // FIXME: ObjectPooling
        }
    }
}
