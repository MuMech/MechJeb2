/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG
{
    public partial class Optimizer : IDisposable
    {
        public double      ZnormTerminationLevel = 1e-9;
        public double      Znorm;
        public int         MaxIter          { get; set; } = 20000; // this could maybe be pushed down
        public double      LmEpsx           { get; set; } = 1e-10; // we terminate manually at 1e-9 so could lower this?
        public double      LmDiffStep       { get; set; } = 1e-9;
        public int         OptimizerTimeout { get; set; } = 5000; // milliseconds
        public int         LmStatus;
        public int         LmIterations;
        public OptimStatus Status;
        
        private readonly Problem                  _problem;
        private readonly List<Phase>              _phases;
        private readonly List<DD>                 _initial  = new List<DD>();
        private readonly List<DD>                 _terminal = new List<DD>();
        private readonly List<DD>                 _residual = new List<DD>();
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
                _initial.Add(DD.Rent(ArrayWrapper.ARRAY_WRAPPER_LEN));
            while (_terminal.Count < _phases.Count)
                _terminal.Add(DD.Rent(ArrayWrapper.ARRAY_WRAPPER_LEN));
            while (_residual.Count < _phases.Count)
                _residual.Add(DD.Rent(ResidualWrapper.RESIDUAL_WRAPPER_LEN));
        }

        private void CopyToInitial(double[] yin)
        {
            for (int i = 0; i < yin.Length; i++)
                _initial[i / ArrayWrapper.ARRAY_WRAPPER_LEN][i % ArrayWrapper.ARRAY_WRAPPER_LEN] = yin[i];
        }

        private double CalcBTConstraint(int p)
        {
            using var yfp = ArrayWrapper.Rent(_terminal[p]);
            using var y0p = ArrayWrapper.Rent(_initial[p]);
            using var yf = ArrayWrapper.Rent(_terminal[lastPhase]);

            // handle coasts
            if (_phases[p].Coast && _phases[p].OptimizeTime)
            {
                return yfp.H0; // coast after jettison or an initial first coast
            }

            if (_phases[p].OptimizeTime)
            {
                if (_phases[p].Coast)
                {
                    if (p == 0)
                        return yfp.H0; // initial first coast
                    return yfp.H0; // coast after jettison
                    // FIXME: coasts during stages
                }

                // handle the optimized burntime that gives rise to the free final time constraint
                if (_phases[p].LastFreeBurn)
                {
                    //if (_phases[p].FinalMassProblem) return H(yf[phases.Count - 1], phases.Count - 1);
                    return yf.CostateMagnitude - 1;
                }

                if (_phases[p].DropMassBar > 0 && p < lastPhase)
                {
                    using var y0p1 = ArrayWrapper.Rent(_initial[p + 1]);
                    return H(yfp, p) - H(y0p1, p + 1);
                }

                // any other optimized burntimes
                return yfp.H0 - y0p.H0;
            }

            return y0p.Bt - _phases[p].bt_bar;
        }

        private double H(ArrayWrapper y, int p)
        {
            return y.H0 + _phases[p].thrust_bar * y.PV.magnitude / y.M - y.Pm * _phases[p].mdot_bar;
        }

        private void BaseResiduals()
        {
            using var y0 = ArrayWrapper.Rent(_initial[0]);
            using var yf = ArrayWrapper.Rent(_terminal[lastPhase]);
            using var z = ResidualWrapper.Rent(_residual[0]);

            z.R        = y0.R - _problem.R0Bar;
            z.V        = y0.V - _problem.V0Bar;
            z.M        = y0.M - _problem.M0Bar;
            z.Terminal = _problem.Terminal.TerminalConstraints(yf);
            z.Bt       = CalcBTConstraint(0);
            //z.Pm_transversality = yf_scratch[phases.Count - 1].Pm - 1;
        }

        private void ContinuityConditions()
        {
            for (int p = 1; p < _phases.Count; p++)
            {
                using var y0 = ArrayWrapper.Rent(_initial[p]);
                using var yf = ArrayWrapper.Rent(_terminal[p - 1]);
                using var z = ResidualWrapper.Rent(_residual[p]);

                z.RContinuity  = yf.R - y0.R;
                z.VContinuity  = yf.V - y0.V;
                z.PvContinuity = yf.PV - y0.PV;
                z.PrContinuity = yf.PR - y0.PR;

                if (_phases[p].MassContinuity)
                    z.M_continuity = yf.M - (_phases[p - 1].DropMassBar + y0.M);
                else
                    z.M_continuity = _phases[p].m0_bar - y0.M;

                z.Bt = CalcBTConstraint(p);

                //z.Pm_transversality =
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
                z[i] = _residual[i / ResidualWrapper.RESIDUAL_WRAPPER_LEN][i % ResidualWrapper.RESIDUAL_WRAPPER_LEN];
            }
        }

        private void CalculateZnorm(double[] z)
        {
            Znorm = 0;
            for (int i = 0; i < z.Length; i++)
            {
                Znorm += z[i] * z[i];
            }

            Znorm = Math.Sqrt(Znorm);
        }

        private bool _terminating = false;


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

        private void AnalyzePhases()
        {
            int lastFreeBurnPhase = -1;

            for (int p = 0; p <= lastPhase; p++)
            {
                _phases[p].LastFreeBurn       = false;
                if (_phases[p].OptimizeTime && !_phases[p].Coast)
                    lastFreeBurnPhase = p;
            }

            if (lastFreeBurnPhase >= 0)
                _phases[lastFreeBurnPhase].LastFreeBurn = true;
        }
        
        private CancellationToken _timeoutToken;

        private void UnSafeRun()
        {
            _terminating = false;
            
            AnalyzePhases();
            ExpandArrays();

            double[] yGuess = new double[_phases.Count * ArrayWrapper.ARRAY_WRAPPER_LEN];
            double[] yNew = new double[_phases.Count * ArrayWrapper.ARRAY_WRAPPER_LEN];
            double[] z = new double[_phases.Count * ResidualWrapper.RESIDUAL_WRAPPER_LEN];

            for (int i = 0; i < yGuess.Length; i++)
                yGuess[i] = _initial[i / ArrayWrapper.ARRAY_WRAPPER_LEN][i % ArrayWrapper.ARRAY_WRAPPER_LEN];
            
            alglib.minlmcreatev(ArrayWrapper.ARRAY_WRAPPER_LEN * _phases.Count, yGuess, LmDiffStep, out _state);
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
                var  tokenSource = new CancellationTokenSource(); // FIXME: bit of garbage here
                tokenSource.CancelAfter(OptimizerTimeout);
                _timeoutToken = tokenSource.Token;
                UnSafeRun();
            }
            catch (OperationCanceledException)
            {
                
            }
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(_phases[p].ToString());
            }
            
            Log("solved initial: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_initial[p]));
            }
            
            Log("solved terminal: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_terminal[p]));
            }
            
            Log("solved residuals: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_residual[p]));
            }

            Status = Success() ? OptimStatus.SUCCESS : OptimStatus.FAILED;

            return this;
        }

        private void Shooting(Solution? solution = null)
        {
            using var integArray = DD.Rent(ArrayWrapper.ARRAY_WRAPPER_LEN);
            using var integ = ArrayWrapper.Rent(integArray);

            double t0 = 0;
            double lastDv = 0;

            for (int p = 0; p <= lastPhase; p++)
            {
                Phase phase = _phases[p];

                using var y0 = ArrayWrapper.Rent(_initial[p]);
                using var yf = ArrayWrapper.Rent(_terminal[p]);

                if (p == 0)
                {
                    y0.R = _problem.R0Bar;
                    y0.V = _problem.V0Bar;
                    y0.M = _problem.M0Bar;
                    y0.CopyTo(integArray);
                }
                else
                {
                    y0.CopyTo(integArray);
                }

                integ.DV = lastDv;

                double bt = phase.OptimizeTime ? y0.Bt : phase.bt_bar;

                double tf = t0 + bt;

                phase.u0 = GetIntertialHeading(p, y0.PV);

                if (solution != null)
                {
                    phase.Integrate(integArray, _terminal[p], t0, tf, solution);
                }
                else
                {
                    phase.Integrate(integArray, _terminal[p], t0, tf);
                }

                lastDv = yf.DV;

                t0 += bt;
            }
        }

        public Solution GetSolution()
        {
            if (Status != OptimStatus.SUCCESS)
                throw new Exception("getting solution from bad/failed optimizer state");
            
            var solution = new Solution(_problem);

            Shooting(solution);

            return solution;
        }

        public Optimizer Bootstrap(V3 pv0, V3 pr0)
        {
            if (Status != OptimStatus.CREATED)
                throw new Exception("bootstrap should only be called on CREATED optimizer");
            
            ExpandArrays();
            
            using var integArray = DD.Rent(ArrayWrapper.ARRAY_WRAPPER_LEN);
            using var integ = ArrayWrapper.Rent(integArray);

            double t0 = 0;
            double lastDv = 0;

            for (int p = 0; p <= lastPhase; p++)
            {
                Phase phase = _phases[p];
                
                using var y0 = ArrayWrapper.Rent(_initial[p]);
                using var yf = ArrayWrapper.Rent(_terminal[p]);

                if (p == 0)
                {
                    y0.R  = _problem.R0Bar;
                    y0.V  = _problem.V0Bar;
                    y0.M  = phase.m0_bar;
                    y0.PV = pv0;
                    y0.PR = pr0;
                    y0.Bt = phase.bt_bar;
                    y0.CopyTo(integArray);
                }
                else
                {
                    _terminal[p - 1].CopyTo(_initial[p]);
                    y0.Bt = phase.bt_bar;
                    y0.M  = phase.m0_bar;
                    _initial[p].CopyTo(integArray);
                }
                
                double tf = t0 + y0.Bt;
                
                integ.DV = lastDv;
                
                phase.u0 = GetIntertialHeading(p, y0.PV);

                phase.Integrate(integArray, _terminal[p], t0, tf);

                lastDv =  yf.DV;
                
                t0     += tf;
            }
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(_phases[p].ToString());
            }
            
            Log("bootstrap1 initial: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_initial[p]));
            }
            
            Log("bootstrap1 terminal: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_terminal[p]));
            }
            
            Log("bootstrap1 residuals: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_residual[p]));
            }

            Status = OptimStatus.BOOTSTRAPPED;    

            return this;
        }
        
        public Optimizer Bootstrap(Solution solution)
        {
            if (Status != OptimStatus.CREATED)
                throw new Exception("bootstrap should only be called on CREATED optimizer");
            
            ExpandArrays();
            
            using var integArray = DD.Rent(ArrayWrapper.ARRAY_WRAPPER_LEN);
            using var integ = ArrayWrapper.Rent(integArray);

            //double tbar = solution.Tbar(_problem.t0);
            
            double t0 = 0;
            double lastDv = 0;

            int segmentOffset = solution.Segments - _phases.Count;
            if (segmentOffset < 0)
                segmentOffset = 0;
            
            for (int p = 0; p <= lastPhase; p++)
            {
                Phase phase = _phases[p];
                
                using var y0 = ArrayWrapper.Rent(_initial[p]);
                using var yf = ArrayWrapper.Rent(_terminal[p]);

                if (p == 0)
                {
                    y0.R  = _problem.R0Bar;
                    y0.V  = _problem.V0Bar;
                    y0.M  = _problem.M0Bar;
                    if (!phase.OptimizeTime)
                    {
                        y0.Bt = phase.bt_bar;
                    } 
                    else
                    {
                        // FIXME: this needs to be smarter to deal with crazy rearrangement
                        // - e.g. if we're looking for a coast, we should probably go searching for the one coast
                        y0.Bt = solution.Bt(p + segmentOffset, _problem.T0) / _problem.Scale.timeScale;
                    }
                    y0.PV = solution.Pv(_problem.T0);
                    y0.PR = solution.Pr(_problem.T0);
                    y0.CopyTo(integArray);
                    integ.DV = 0;
                }
                else
                {
                    _terminal[p - 1].CopyTo(_initial[p]);
                    if (!phase.OptimizeTime)
                    {
                        y0.Bt = phase.bt_bar;
                    } 
                    else
                    {
                        // FIXME: testing hack
                        if (phase.Coast && phase.OptimizeTime)
                            y0.Bt = 0.1;
                        // FIXME: this needs to be smarter to deal with crazy rearrangement
                        // - e.g. if we're looking for a coast, we should probably go searching for the one coast
                        else
                            y0.Bt = solution.Bt(p + segmentOffset, _problem.T0) / _problem.Scale.timeScale;
                    }
                    y0.M  = phase.m0_bar;
                    _initial[p].CopyTo(integArray);
                }
                
                double tf = t0 + y0.Bt;
                
                integ.DV = lastDv;

                phase.u0 = GetIntertialHeading(p, y0.PV);
                
                phase.Integrate(integArray, _terminal[p], t0, tf);

                lastDv = yf.DV;

                t0 += tf;
                
            }
            
            CalculateResiduals();

            for(int p = 0; p <= lastPhase; p++)
            {
                Log(_phases[p].ToString());
            }
            
            Log("bootstrap2 initial: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_initial[p]));
            }
            
            Log("bootstrap2 terminal: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_terminal[p]));
            }
            
            Log("bootstrap2 residuals: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Log(DoubleArrayString(_residual[p]));
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

        public bool Success()
        {
            // even if we didn't terminate successfully, we're close enough to a zero to use the solution
            return Znorm < 1e-5;
        }

        public static OptimizerBuilder Builder()
        {
            return new OptimizerBuilder();
        }

        public void Dispose()
        {
            // FIXME: ObjectPooling
        }
    }
}
