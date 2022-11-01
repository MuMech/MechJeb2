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
using ProceduralFairings;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PVG
{
    /// <summary>
    ///     TODO:
    ///     - proper cold bootstrap of the Pv/Pr guess based on launch azimuth + 45 degrees (DONE)
    ///     - infinite ISP upper stage bootstrap (THIS here is what Reset Guidance should do) (DONE)
    ///     - handle LAN and FPA5 (DONE)
    ///     - handle attachaltflag (DONE)
    ///     - handle fixed coasts (DONE)
    ///     - handle optimized coasts
    ///     - handle re-guessing if FPA4 got the northgoing/southgoing sense wrong (and/or pin the LAN first then remove the
    ///     LAN constraint)
    ///     - bootstrap with periapsis attachment first then do kepler4/kepler5 for free attachment (DONE)
    ///     - report failures back in the UI
    ///     - retry on failures
    ///     - remove hardcoding of upper stage optimization / infinite bootstrap
    ///     - consider some kind of optimized time / infinite bootstrap initially for fixed burntime
    ///     - support ExtendIfNeeded
    /// </summary>
    public partial class Ascent
    {
        private readonly List<Phase> _phases = new List<Phase>();
        private          V3          _r0;
        private          V3          _v0;
        private          V3          _u0;
        private          double      _t0;
        private          double      _mu;
        private          double      _rbody;
        private          double      _peR;
        private          double      _apR;
        private          double      _attR;
        private          double      _vT;
        private          double      _gammaT;
        private          double      _incT;
        private          double      _lanT;
        private          double      _smaT;
        private          double      _eccT;
        private          double      _hT; // terminal condition
        private          bool        _attachAltFlag;
        private          bool        _lanflag;
        private          bool        _fixedBurnTime;
        public           Solution?   _solution;
        private          int         _lastPhase => _phases.Count - 1;
        private          int         _optimizedPhase;
        private          int         _optimizedCoastPhase = -1;
        private          Optimizer?  _optimizer;

        public void Run()
        {
            (_smaT, _eccT) = Functions.SmaEccFromApsides(_peR, _apR);
            
            if (!_attachAltFlag)
                _attR = _peR;
            
            foreach (Phase phase in _phases)
            {
                // FIXME: the analytic coast integrator is definitely buggy so the Shepperd solver must be buggy
                /*if (phase.Coast)
                    phase.Integrator = new VacuumCoastIntegrator();
                else*/
                    phase.Integrator = phase.Unguided ? (IPVGIntegrator)new VacuumThrustIntegrator() : new VacuumThrustAnalytic();
                    phase.Integrator = new VacuumThrustIntegrator();
            }

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
                {
                    ApplyFPA(builder);
                }
                else
                {
                    ApplyKepler(builder);
                }
            }

            Optimizer pvg = builder.Build();
            pvg.Bootstrap(solution);
            pvg.Run();

            return pvg;
        }

        private void ApplyFPA(Optimizer.OptimizerBuilder builder)
        {
            (_vT, _gammaT) = Functions.ConvertApsidesTargetToFPA(_peR, _apR, _attR, _mu);
            if (_lanflag)
            {
                builder.TerminalFPA5(_attR, _vT, _gammaT, _incT, _lanT);
            }
            else
            {
                builder.TerminalFPA4(_attR, _vT, _gammaT, _incT);
            }
        }

        private void ApplyKepler(Optimizer.OptimizerBuilder builder)
        {
            (_smaT, _eccT) = Functions.SmaEccFromApsides(_peR, _apR);

            if (_lanflag)
            {
                builder.TerminalKepler4(_smaT, _eccT, _incT, _lanT);
            }
            else
            {
                builder.TerminalKepler3(_smaT, _eccT, _incT);
            }
        }

        private void ApplyEnergy(Optimizer.OptimizerBuilder builder)
        {
            (_vT, _gammaT) = Functions.ConvertApsidesTargetToFPA(_peR, _apR, _attR, _mu);
            
            if (_lanflag)
            {
                builder.TerminalEnergy4(_attR, _incT, _lanT);
            }
            else
            {
                builder.TerminalEnergy3(_attR, _gammaT, _incT);
            }
        }

        private Optimizer InitialBootstrapping(Optimizer.OptimizerBuilder builder)
        {
            if (_fixedBurnTime)
            {
                ApplyEnergy(builder);
            }
            else
            {
                ApplyFPA(builder);
            }

            using Optimizer pvg = builder.Build();

            // guess the initial launch direction
            V3 enu = Functions.ENUHeadingForInclination(_incT, _r0);
            enu.z = 1.0; // add 45 degrees up
            V3 pvGuess = Functions.ENUToECI(_r0, enu).normalized;
            
            int infinitePhase = _fixedBurnTime ? _lastPhase : _optimizedPhase;

            bool savedUnguided = _phases[infinitePhase].Unguided;

            _phases[infinitePhase].Infinite = true;
            _phases[infinitePhase].Unguided = false;

            if (_optimizedCoastPhase > -1)
                _phases[_optimizedCoastPhase].OptimizeTime = false;

            pvg.Bootstrap(pvGuess, _r0.normalized);
            pvg.Run();
            
            Log("finished a pvg run");

            _phases[infinitePhase].Infinite = false;
            _phases[infinitePhase].Unguided = savedUnguided;

            if (_optimizedCoastPhase > -1)
                _phases[_optimizedCoastPhase].OptimizeTime = true;
           
            if (!pvg.Success())
                throw new Exception("FIXME: need to handle this error better");
            
            using Solution solution = pvg.GetSolution();

            Log("starting a pvg2 run");
            
            using Optimizer pvg2 = builder.Build();
            pvg2.Bootstrap(solution);
            pvg2.Run();

            Log("finished a pvg2 run");
            
            if (!pvg2.Success())
            {
                Log("failure from pvg2 why no exception?");
                throw new Exception("FIXME: need to handle this error better");
            }

            Log("starting a pvg3 run");
            // we have a periapsis attachment solution, redo with free attachment
            if (_attachAltFlag || _fixedBurnTime)
                return pvg2;
            
            using Solution solution2 = pvg.GetSolution();

            ApplyKepler(builder);

            using Optimizer pvg3 = builder.Build();
            pvg3.Bootstrap(solution2);
            pvg3.Run();
            
            Log("finished a pvg3 run");

            if (!pvg3.Success())
                return pvg2;
            
            using Solution solution3 = pvg.GetSolution();
            
            (V3 rf, V3 vf) = solution3.TerminalStateVectors();

            (double smaf, double eccf, double incf, double lanf, double argpf, double tanof) =
                Functions.KeplerianFromStateVectors(_mu, rf, vf);
            
            // FIXME: we need to somehow pass this into ConvergedOptimization
            return Math.Abs(ClampPi(tanof)) > PI/2.0 ? pvg2 : pvg3;

        }

        public static AscentBuilder Builder()
        {
            // FIXME: this is very wrong the Ascent object shouldn't be insteantiated until Build() is called.
            return new AscentBuilder(new Ascent());
        }
    }
}
