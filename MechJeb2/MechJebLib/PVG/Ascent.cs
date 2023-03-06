/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using System.Collections.Generic;
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

        private          List<Phase> _phases              => _input._phases;
        private          V3          _r0                  => _input._r0;
        private          V3          _v0                  => _input._v0;
        private          V3          _u0                  => _input._u0;
        private          double      _t0                  => _input._t0;
        private          double      _mu                  => _input._mu;
        private          double      _rbody               => _input._rbody;
        private          double      _peR                 => _input._peR;
        private          double      _apR                 => _input._apR;
        private          double      _attR                => _input._attR;
        private          double      _incT                => _input._incT;
        private          double      _lanT                => _input._lanT;
        private          double      _fpaT                => _input._fpaT;
        private          double      _hT                  => _input._hT;
        private          bool        _attachAltFlag       => _input._attachAltFlag;
        private          bool        _lanflag             => _input._lanflag;
        private          bool        _fixedBurnTime       => _input._fixedBurnTime;
        private          Solution?   _solution            => _input._solution;
        private          int         _optimizedPhase      => _input._optimizedPhase;
        private          int         _optimizedCoastPhase => _input._optimizedCoastPhase;

        private double     _vT;
        private double     _gammaT;
        private double     _smaT;
        private double     _eccT;
        private Optimizer? _optimizer;
        
        private Ascent(AscentBuilder builder)
        {
            _input  = builder;
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
                .TerminalConditions(_hT);

            if (_solution == null)
            {
                _optimizer = _fixedBurnTime ? InitialBootstrappingFixed(builder) : InitialBootstrappingOptimized(builder);
            }
            else
            {
                _optimizer = ConvergedOptimization(builder, _solution);
            }
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
            Optimizer pvg = builder.Build(this._phases);
            pvg.Bootstrap(solution);
            pvg.Run();
            
            using Solution solution2 = pvg.GetSolution();
            
            (V3 rf, V3 vf) = solution2.TerminalStateVectors();

            (double _, double _, double _, double _, double _, double tanof) =
                Functions.KeplerianFromStateVectors(_mu, rf, vf);

            if ( _attachAltFlag || Math.Abs(ClampPi(tanof)) < PI/2.0 )
                return pvg;
            
            ApplyFPA(builder);
            ApplyOldBurnTimesToPhases(solution2);
            
            using Optimizer pvg2 = builder.Build(this._phases);
            pvg2.Bootstrap(solution2);
            pvg2.Run();
            
            return pvg2.Success() ? pvg2 : pvg;
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

        private Optimizer InitialBootstrappingFixed(Optimizer.OptimizerBuilder builder)
        {
            ApplyEnergy(builder);
            
            // guess the initial launch direction
            V3 enu = Functions.ENUHeadingForInclination(_incT, _r0);
            enu.z = 1.0; // add 45 degrees up
            V3 pvGuess = Functions.ENUToECI(_r0, enu).normalized;

            List<Phase> bootphases = DupPhases(_phases);

            // FIXME: we may want to convert this to an optimized burntime circular orbit problem with an infinite upper stage for bootstrapping
            for (int p = 0; p < bootphases.Count; p++)
            {
                bootphases[p].Unguided     = false;
                
                if (p == _optimizedCoastPhase)
                {
                    bootphases[p].OptimizeTime = false;
                    // FIXME: a problem here is that if we require a coast to hit the target then we'll never converge
                    bootphases[p].bt           = 0;
                }
            }

            using Optimizer pvg = builder.Build(bootphases);
            pvg.Bootstrap(pvGuess, _r0.normalized);
            pvg.Run();
            
            if (!pvg.Success())
                throw new Exception("Target unreachable (fixed bootstrapping)");
            
            using Solution solution = pvg.GetSolution();
            ApplyOldBurnTimesToPhases(solution);

            List<Phase> bootphases2 = DupPhases(_phases);
            
            if (_optimizedCoastPhase > -1)
            {
                double total = 0.0;
                
                for (int i = 0; i < bootphases2.Count; i++)
                    total += bootphases2[i].bt;
                
                bootphases2[_optimizedCoastPhase].bt           = total;
            }

            using Optimizer pvg2 = builder.Build(bootphases2);
            pvg2.Bootstrap(solution);
            pvg2.Run();
            
            if (!pvg2.Success())
                throw new Exception("Target unreachable");
            
            return pvg2;
        }

        private Optimizer InitialBootstrappingOptimized(Optimizer.OptimizerBuilder builder)
        {
            ApplyFPA(builder);
            
            // guess the initial launch direction
            V3 enu = Functions.ENUHeadingForInclination(_incT, _r0);
            enu.z = 1.0; // add 45 degrees up
            V3 pvGuess = Functions.ENUToECI(_r0, enu).normalized;
            
            List<Phase> bootphases = DupPhases(_phases);
            
            bootphases[bootphases.Count - 1].Infinite = true;
            bootphases[bootphases.Count - 1].Unguided = false;

            if (_optimizedCoastPhase > -1)
            {
                bootphases[_optimizedCoastPhase].OptimizeTime = false;
                bootphases[_optimizedCoastPhase].bt           = 0;
            }
            
            using Optimizer pvg = builder.Build(bootphases);
            pvg.Bootstrap(pvGuess, _r0.normalized);
            pvg.Run();
            
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
                _phases[_optimizedCoastPhase].bt           = total;
            }

            using Optimizer pvg2 = builder.Build(this._phases);
            pvg2.Bootstrap(solution);
            pvg2.Run();
            
            if (!pvg2.Success())
                throw new Exception("Target unreachable");
            
            if (_attachAltFlag)
                return pvg2;
            
            // we have a periapsis attachment solution, redo with free attachment
            using Solution solution2 = pvg2.GetSolution();

            ApplyKepler(builder);
            ApplyOldBurnTimesToPhases(solution2);

            using Optimizer pvg3 = builder.Build(this._phases);
            pvg3.Bootstrap(solution2);
            pvg3.Run();
            
            if (!pvg3.Success())
                return pvg2;
            
            // sanity check to force back near-apoapsis attatchment back to periapsis attachment
            using Solution solution3 = pvg3.GetSolution();
            
            (V3 rf, V3 vf) = solution3.TerminalStateVectors();

            (double _, double _, double _, double _, double _, double tanof) =
                Functions.KeplerianFromStateVectors(_mu, rf, vf);
            
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

        // FIXME: this obviously creates garbage and needs to return an IDisposable wrapping List<Phases> that has a pool
        private List<Phase> DupPhases(List<Phase> oldphases)
        {
            var newphases = new List<Phase>();

            foreach (Phase phase in oldphases)
                newphases.Add(phase.DeepCopy());
            
            return newphases;
        }

        public static AscentBuilder Builder()
        {
            return new AscentBuilder();
        }
    }
}
