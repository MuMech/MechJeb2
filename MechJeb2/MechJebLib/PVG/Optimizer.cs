#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG
{
    public partial class Optimizer : IDisposable
    {
        public           double                   ZnormTerminationLevel = 1e-9;
        public           double                   Znorm;
        public           int                      MaxIter    { get; set; } = 20000; // this could maybe be pushed down
        public           double                   lmEpsx     { get; set; } = 1e-10; // we terminate manually at 1e-9 so could lower this?
        public           double                   lmDiffStep { get; set; } = 1e-9;
        public           int                      LmStatus;
        public           int                      LmIterations;
        
        private readonly Problem                  _problem;
        private readonly List<Phase>              _phases;
        private readonly List<DD>                 _initial  = new List<DD>();
        private readonly List<DD>                 _terminal = new List<DD>();
        private readonly List<DD>                 _residual = new List<DD>();
        private          int                      lastPhase => _phases.Count - 1;
        private readonly alglib.minlmreport       rep = new alglib.minlmreport();
        private readonly alglib.ndimensional_fvec ResidualHandle;
        private          alglib.minlmstate        _state = new alglib.minlmstate();
        

        private Optimizer(Problem problem, IEnumerable<Phase> phases)
        {
            _phases        = new List<Phase>(phases);
            _problem       = problem;
            ResidualHandle = ResidualFunction;
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
            if (_phases[p].Coast)
            {
                // coasts within a stage
                //return yfp.PV.magnitude - y0p.PV.magnitude;
            }

            if (_phases[p].OptimizeTime)
            {
                // handle the optimized burntime that gives rise to the free final time constraint
                // NOTE: This is supposed to work only if the target is a constant of the Keplerian motion
                //       and should not work for e.g. the FlightPathAngle terminal conditions when gammaT != 0.
                //       See discussion (section IV):
                //       https://www.researchgate.net/publication/245433631_Rapid_Optimal_Multi-Burn_Ascent_Planning_and_Guidance
                //       I tried using equation 40 instead but this failed to converge.
                //
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

            z.R        = y0.R - _problem.r0_bar;
            z.V        = y0.V - _problem.v0_bar;
            z.M        = y0.M - _problem.m0_bar;
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

        private bool terminating = false;


        internal void ResidualFunction(double[] yin, double[] zout, object? o)
        {
            if (terminating)
                return;
            
            CopyToInitial(yin);
            Shooting();
            // need to backwards integrate the mass costate here
            CalculateResiduals();
            CopyToZ(zout);
            CalculateZnorm(zout);

            if (Znorm < ZnormTerminationLevel)
            {
                alglib.minlmrequesttermination(_state);
                terminating = true;
            }
        }

        private bool IsCoastAfterJettison(int p)
        {
            if (p == 0 || p == lastPhase) return false;
            //FIXME: make this a little less brittle
            return _phases[p].Coast && _phases[p - 1].m0_bar != _phases[p + 1].m0_bar;
        }

        private void AnalyzePhases()
        {
            int lastFreeBurnPhase = -1;

            for (int p = 0; p <= lastPhase; p++)
            {
                _phases[p].First              = false;
                _phases[p].LastFreeBurn       = false;
                if (_phases[p].OptimizeTime && !_phases[p].Coast)
                    lastFreeBurnPhase = p;
            }

            if (lastFreeBurnPhase >= 0)
                _phases[lastFreeBurnPhase].LastFreeBurn = true;

            _phases[0].First = true;
        }

        public Optimizer Run()
        {
            terminating = false;
            
            AnalyzePhases();
            ExpandArrays();

            double[] yGuess = new double[_phases.Count * ArrayWrapper.ARRAY_WRAPPER_LEN];
            double[] yNew = new double[_phases.Count * ArrayWrapper.ARRAY_WRAPPER_LEN];
            double[] z = new double[_phases.Count * ResidualWrapper.RESIDUAL_WRAPPER_LEN];

            for (int i = 0; i < yGuess.Length; i++)
                yGuess[i] = _initial[i / ArrayWrapper.ARRAY_WRAPPER_LEN][i % ArrayWrapper.ARRAY_WRAPPER_LEN];
            
            alglib.minlmcreatev(ArrayWrapper.ARRAY_WRAPPER_LEN * _phases.Count, yGuess, lmDiffStep, out _state);
            alglib.minlmsetcond(_state, lmEpsx, MaxIter);
            alglib.minlmoptimize(_state, ResidualHandle, null, null);
            alglib.minlmresultsbuf(_state, ref yNew, rep);

            LmStatus     = rep.terminationtype;
            LmIterations = rep.iterationscount;
            
            if (rep.terminationtype != 8)
                ResidualFunction(yNew, z, null);
            
            Debug.Log("solved initial: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_initial[p]));
            }
            
            Debug.Log("solved terminal: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_terminal[p]));
            }
            
            Debug.Log("solved residuals: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_residual[p]));
            }

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
                    y0.R = _problem.r0_bar;
                    y0.V = _problem.v0_bar;
                    y0.M = _problem.m0_bar;
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
            var solution = new Solution(_problem);

            Shooting(solution);

            return solution;
        }

        public Optimizer Bootstrap(V3 pv0, V3 pr0)
        {
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
                    y0.R  = _problem.r0_bar;
                    y0.V  = _problem.v0_bar;
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
            
            Debug.Log("bootstrap1 initial: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_initial[p]));
            }
            
            Debug.Log("bootstrap1 terminal: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_terminal[p]));
            }
            
            Debug.Log("bootstrap1 residuals: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_residual[p]));
            }

            return this;
        }
        
        public Optimizer Bootstrap(Solution solution)
        {
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
                    y0.R  = _problem.r0_bar;
                    y0.V  = _problem.v0_bar;
                    y0.M  = _problem.m0_bar;
                    if (!phase.OptimizeTime)
                    {
                        y0.Bt = phase.bt_bar;
                    } 
                    else
                    {
                        // FIXME: this needs to be smarter to deal with crazy rearrangement
                        // - e.g. if we're looking for a coast, we should probably go searching for the one coast
                        y0.Bt = solution.Bt(p + segmentOffset, _problem.t0) / _problem.Scale.timeScale;
                    }
                    y0.PV = solution.Pv(_problem.t0);
                    y0.PR = solution.Pr(_problem.t0);
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
                        // FIXME: this needs to be smarter to deal with crazy rearrangement
                        // - e.g. if we're looking for a coast, we should probably go searching for the one coast
                        y0.Bt = solution.Bt(p + segmentOffset, _problem.t0) / _problem.Scale.timeScale;
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

            Debug.Log("bootstrap2 initial: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_initial[p]));
            }
            
            Debug.Log("bootstrap2 terminal: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_terminal[p]));
            }
            
            Debug.Log("bootstrap2 residuals: ");
            
            for(int p = 0; p <= lastPhase; p++)
            {
                Debug.Log(DoubleArrayString(_residual[p]));
            }

            return this;
        }

        private V3 GetIntertialHeading(int p, V3 pv0)
        {
            if (p == 0)
                return _problem.u0;

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
