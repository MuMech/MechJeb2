/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
using KSP.UI;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.PVG.Integrators;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG
{
    /// <summary>
    ///     
    /// </summary>
    public partial class Ascent
    {
        private readonly AscentBuilder _input;
        
        private List<Phase> _phases              => _input._phases;
        private V3          _r0                  => _input._r0;
        private V3          _v0                  => _input._v0;
        private V3          _u0                  => _input._u0;
        private double      _t0                  => _input._t0;
        private double      _mu                  => _input._mu;
        private double      _rbody               => _input._rbody;
        private double      _peR                 => _input._peR;
        private double      _apR                 => _input._apR;
        private double      _attR                => _input._attR;
        private double      _incT                => _input._incT;
        private double      _lanT                => _input._lanT;
        private double      _fpaT                => _input._fpaT;
        private double      _hT                  => _input._hT;
        private bool        _attachAltFlag       => _input._attachAltFlag;
        private bool        _lanflag             => _input._lanflag;
        private bool        _fixedBurnTime       => _input._fixedBurnTime;
        private Solution?   _solution            => _input._solution;
        private int         _optimizedPhase      => _input._optimizedPhase;
        private int         _optimizedCoastPhase => _input._optimizedCoastPhase;

        private double     _vT;
        private double     _gammaT;
        private double     _smaT;
        private double     _eccT;
        private Optimizer? _optimizer;
        
        private Ascent(AscentBuilder builder)
        {
            _input = builder;
        }

        public void Run()
        {
            (_smaT, _eccT) = Functions.SmaEccFromApsides(_peR, _apR);

            foreach (Phase phase in _phases)
            {
                // FIXME: the analytic coast integrator is definitely buggy so the Shepperd solver must be buggy
                if (phase.Coast)
                    phase.Integrator = new VacuumThrustIntegrator();
                else
                    // FIXME: make a debug setting to flip between these somewhere
                    phase.Integrator = new VacuumThrustAnalytic();
                    //phase.Integrator = new VacuumThrustIntegrator();
            }

            //_phases[0].Integrator = new VacuumThrustIntegrator();
            //_phases[1].Integrator = new VacuumThrustIntegrator();
            
            //_phases[3].Integrator = new VacuumThrustIntegrator();

            using Optimizer.OptimizerBuilder builder = Optimizer.Builder()
                .Initial(_r0, _v0, _u0, _t0, _mu, _rbody)
                .Phases(_phases)
                .TerminalConditions(_hT);

            _optimizer = _solution == null ? InitialBootstrapping(builder) : ConvergedOptimization(builder, _solution);
        }

        public Optimizer? GetOptimizer()
        {
            return _optimizer;
        }

        private Optimizer ConvergedOptimization(Optimizer.OptimizerBuilder builder, Solution solution)
        {
            if (_fixedBurnTime)
            {
                ApplyEnergy(builder);
            }
            else
            {
                if (_attachAltFlag || _eccT < 1e-4)
                    ApplyFPA(builder);
                else
                    ApplyKepler(builder);
            }

            ApplyOldBurnTimesToPhases(solution);            
            Optimizer pvg = builder.Build();
            pvg.Bootstrap(solution);
            pvg.Run();

            return pvg;
        }

        private void ApplyFPA(Optimizer.OptimizerBuilder builder)
        {
            // If _attachAltFlag is NOT set then we are bootstrapping with ApplyFPA prior to
            // trying free attachment with Kepler and attR is invalid and we need to fix to
            // the PeR.  This should be fixed in the AscentBuilder by having more APIs than
            // just "SetTarget" that fixes this correctly there.
            var attR = _attachAltFlag ? _attR : _peR;
            
            (_vT, _gammaT) = Functions.ConvertApsidesTargetToFPA(_peR, _apR, attR, _mu);
            if (_lanflag)
                builder.TerminalFPA5(attR, _vT, _gammaT, _incT, _lanT);
            else
                builder.TerminalFPA4(attR, _vT, _gammaT, _incT);
        }

        private void ApplyKepler(Optimizer.OptimizerBuilder builder)
        {
            (_smaT, _eccT) = Functions.SmaEccFromApsides(_peR, _apR);

            if (_lanflag)
                builder.TerminalKepler4(_smaT, _eccT, _incT, _lanT);
            else
                builder.TerminalKepler3(_smaT, _eccT, _incT);
        }

        private void ApplyEnergy(Optimizer.OptimizerBuilder builder)
        {
            if (_lanflag)
                builder.TerminalEnergy4(_attR, _incT, _lanT);
            else
                builder.TerminalEnergy3(_attR, _fpaT, _incT);
        }

        private Optimizer InitialBootstrapping(Optimizer.OptimizerBuilder builder)
        {
            if (_fixedBurnTime)
                ApplyEnergy(builder);
            else
                ApplyFPA(builder);
            
            // guess the initial launch direction
            V3 enu = Functions.ENUHeadingForInclination(_incT, _r0);
            enu.z = 1.0; // add 45 degrees up
            V3 pvGuess = Functions.ENUToECI(_r0, enu).normalized;
            
            bool savedUnguided = _phases[_phases.Count - 1].Unguided;
            
            if (!_fixedBurnTime)
                _phases[_optimizedPhase].OptimizeTime = false;
            
            _phases[_phases.Count - 1].Infinite     = true;
            _phases[_phases.Count - 1].Unguided     = false;
            _phases[_phases.Count - 1].OptimizeTime = true;

            if (_optimizedCoastPhase > -1)
                _phases[_optimizedCoastPhase].OptimizeTime = false;

            using Optimizer pvg = builder.Build();
            pvg.Bootstrap(pvGuess, _r0.normalized);
            pvg.Run();
            
            _phases[_phases.Count - 1].Infinite     = false;
            _phases[_phases.Count - 1].Unguided     = savedUnguided;
            _phases[_phases.Count - 1].OptimizeTime = false;

            if (!_fixedBurnTime)
                _phases[_optimizedPhase].OptimizeTime = true;
            
            if (!pvg.Success())
                throw new Exception("Target unreachable (bootstrapping)");
            
            using Solution solution = pvg.GetSolution();
            ApplyOldBurnTimesToPhases(solution);
            
            if (_optimizedCoastPhase > -1)
            {
                double total = 0.0;
                
                for (int i = 0; i < _phases.Count; i++)
                    total += _phases[i].bt;
                
                _phases[_optimizedCoastPhase].OptimizeTime = true;
                _phases[_optimizedCoastPhase].bt           = total / 2;
            }

            using Optimizer pvg2 = builder.Build();
            pvg2.Bootstrap(solution);
            pvg2.Run();
            
            if (!pvg2.Success())
                throw new Exception("Target unreachable");

            // we have a periapsis attachment solution, redo with free attachment
            if (_attachAltFlag || _fixedBurnTime)
                return pvg2;
            
            using Solution solution2 = pvg2.GetSolution();

            ApplyKepler(builder);
            ApplyOldBurnTimesToPhases(solution2);

            using Optimizer pvg3 = builder.Build();
            pvg3.Bootstrap(solution2);
            pvg3.Run();
            
            if (!pvg3.Success())
                return pvg2;
            
            using Solution solution3 = pvg3.GetSolution();
            
            (V3 rf, V3 vf) = solution3.TerminalStateVectors();

            (double _, double _, double _, double _, double _, double tanof) =
                Functions.KeplerianFromStateVectors(_mu, rf, vf);
            
            // FIXME: we need to somehow pass this into ConvergedOptimization
            return Math.Abs(ClampPi(tanof)) > PI/2.0 ? pvg2 : pvg3;
        }

        private void ApplyOldBurnTimesToPhases(Solution oldSolution)
        {
            for (int i = 0; i < _phases.Count; i++)
            {
                if (!_phases[i].OptimizeTime)
                    continue;
                
                for (int j = 0; j < oldSolution.Segments; j++)
                {
                    if (!oldSolution.OptimizeTime(j))
                        continue;

                    if (oldSolution.CoastPhase(j) != _phases[i].Coast)
                        continue;

                    if (!_phases[i].Coast)
                        _phases[i].bt = Math.Min(oldSolution.Bt(j, _t0), _phases[i].bt);
                    else 
                        _phases[i].bt = oldSolution.Bt(j, _t0);
                    
                }
            }
        }

        public static AscentBuilder Builder()
        {
            return new AscentBuilder();
        }
    }
}
