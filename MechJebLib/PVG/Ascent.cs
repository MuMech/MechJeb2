/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PVG
{
    /// <summary>
    /// </summary>
    public partial class Ascent
    {
        private readonly AscentBuilder _input;

        private PhaseCollection _phases              => _input._phases;
        private V3              _r0                  => _input._r0;
        private V3              _v0                  => _input._v0;
        private V3              _u0                  => _input._u0;
        private double          _t0                  => _input._t0;
        private double          _mu                  => _input._mu;
        private double          _rbody               => _input._rbody;
        private double          _peR                 => _input._peR;
        private double          _apR                 => _input._apR;
        private double          _attR                => _input._attR;
        private double          _incT                => _input._incT;
        private double          _lanT                => _input._lanT;
        private double          _fpaT                => _input._fpaT;
        private double          _hT                  => _input._hT;
        private bool            _attachAltFlag       => _input._attachAltFlag;
        private bool            _lanflag             => _input._lanflag;
        private bool            _fixedBurnTime       => _input._fixedBurnTime;
        private Solution?       _solution            => _input._solution;
        private int             _optimizedPhase      => _input._optimizedPhase;
        private int             _optimizedCoastPhase => _input._optimizedCoastPhase;

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
            (_smaT, _eccT) = Astro.SmaEccFromApsides(_peR, _apR);

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

        public void Test(V3 pv0, V3 pr0)
        {
            (_smaT, _eccT) = Astro.SmaEccFromApsides(_peR, _apR);

            using Optimizer.OptimizerBuilder builder = Optimizer.Builder()
                .Initial(_r0, _v0, _u0, _t0, _mu, _rbody)
                .TerminalConditions(_hT);

            ForceNumericalIntegration();

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

            using Optimizer pvg = builder.Build(_phases);
            pvg.Bootstrap(pv0, pr0);
            pvg.Run();

            _optimizer = pvg;
        }

        public Optimizer? GetOptimizer() => _optimizer;

        private Optimizer ConvergedOptimization(Optimizer.OptimizerBuilder builder, Solution solution)
        {
            ForceNumericalIntegration();

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
            Optimizer pvg = builder.Build(_phases);
            pvg.Bootstrap(solution);
            pvg.Run();

            if (!pvg.Success())
                throw new Exception("converged optimizer failed");

            using Solution solution2 = pvg.GetSolution();

            (V3 rf, V3 vf) = solution2.TerminalStateVectors();

            (_, _, _, _, _, double tanof, _) =
                Astro.KeplerianFromStateVectors(_mu, rf, vf);

            if (_attachAltFlag || _fixedBurnTime || Abs(ClampPi(tanof)) < PI / 2.0)
                return pvg;

            ApplyFPA(builder);
            ApplyOldBurnTimesToPhases(solution2);

            using Optimizer pvg2 = builder.Build(_phases);
            pvg2.Bootstrap(solution2);
            pvg2.Run();

            return pvg2.Success() ? pvg2 : pvg;
        }

        private void ApplyFPAToFixed(Optimizer.OptimizerBuilder builder)
        {
            (_vT, _gammaT) = Astro.ConvertApsidesTargetToFPA(_attR, _attR, _attR, _mu);
            if (_lanflag)
                builder.TerminalFPA5(_attR, _vT, _gammaT, _incT, _lanT);
            else
                builder.TerminalFPA4(_attR, _vT, _gammaT, _incT);
        }

        private void ApplyFPA(Optimizer.OptimizerBuilder builder)
        {
            // If _attachAltFlag is NOT set then we are bootstrapping with ApplyFPA prior to
            // trying free attachment with Kepler and attR is invalid and we need to fix to
            // the PeR.  This should be fixed in the AscentBuilder by having more APIs than
            // just "SetTarget" that fixes this correctly there.
            double attR = _attachAltFlag ? _attR : _peR;

            (_vT, _gammaT) = Astro.ConvertApsidesTargetToFPA(_peR, _apR, attR, _mu);
            if (_lanflag)
                builder.TerminalFPA5(attR, _vT, _gammaT, _incT, _lanT);
            else
                builder.TerminalFPA4(attR, _vT, _gammaT, _incT);
        }

        private void ApplyKepler(Optimizer.OptimizerBuilder builder)
        {
            (_smaT, _eccT) = Astro.SmaEccFromApsides(_peR, _apR);

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
            /*
             * Analytic initial bootstrapping with infinite upper stage and no coast, forced FPA attachment with circular orbit
             */

            Print("*** PHASE 1: DOING INITIAL INFINITE UPPER STAGE ***");
            ApplyFPAToFixed(builder);

            // guess the initial launch direction
            V3 enu = Astro.ENUHeadingForInclination(_incT, _r0);
            enu.z = 1.0; // add 45 degrees up
            V3 pvGuess = Astro.ENUToECI(_r0, enu).normalized / Sqrt(2);

            PhaseCollection bootphases = InfiniteUpperNoCoast();

            using Optimizer pvg = builder.Build(bootphases);
            pvg.Bootstrap(pvGuess, _r0.normalized / Sqrt(2));
            pvg.Run();

            if (!pvg.Success())
                throw new Exception("Target unreachable (infinite ISP");

            /*
             * Adding coast to infinite upper stage with circular FPA target
             */

            //if (_optimizedCoastPhase > -1)
            //{
            Print("*** PHASE 2: ADDING COAST TO INFINITE UPPER STAGE ***");
            using Solution solution = pvg.GetSolution();
            ApplyOldBurnTimesToPhases(solution);

            PhaseCollection bootphases2 = DupPhases(_phases);

            for (int i = 0; i < bootphases2.Count; i++)
            {
                // set the coast to the total burntime / 2
                if (i == _optimizedCoastPhase)
                    bootphases2[i].bt = Clamp(TotalBurntime(solution) / 2, bootphases2[i].mint, bootphases2[i].maxt);

                // make all stages guided and unoptimized
                bootphases2[i].Unguided     = false;
                bootphases2[i].OptimizeTime = false;
            }

            // set the top stage to infinite + optimized
            bootphases2[bootphases2.Count - 1].Infinite     = true;
            bootphases2[bootphases2.Count - 1].OptimizeTime = true;

            Optimizer pvg2 = builder.Build(bootphases2);
            pvg2.Bootstrap(solution);
            pvg2.Run();

            if (!pvg2.Success())
                throw new Exception("Target unreachable (infinite ISP w/coast)");
            //}

            /*
             * Solving problem with all guided stages
             */

            Print("*** PHASE 3: DOING FINITE GUIDED STAGES ***");

            using Solution solution2 = pvg2.GetSolution();
            ApplyOldBurnTimesToPhases(solution2);

            PhaseCollection bootphases3 = DupPhases(_phases);

            for (int i = 0; i < bootphases3.Count; i++)
            {
                // make all stages guided
                bootphases3[i].Unguided = false;
            }

            bootphases3[bootphases3.Count - 1].OptimizeTime = true;

            Optimizer pvg3 = builder.Build(bootphases3);
            pvg3.Bootstrap(solution2);
            pvg3.Run();

            if (!pvg3.Success())
                throw new Exception("Target unreachable (finite guided stages)");

            /*
             * Solving problem with unguided stages
             */

            Print("*** PHASE 4: ADDING UNGUIDED STAGES ***");
            ApplyEnergy(builder);

            using Solution solution3 = pvg3.GetSolution();
            ApplyOldBurnTimesToPhases(solution3);

            Optimizer pvg4 = builder.Build(_phases);
            pvg4.Bootstrap(solution3);
            pvg4.Run();

            if (!pvg4.Success())
                throw new Exception("Target unreachable (analytic)");

            Print("*** PHASE 5: NUMERICAL INTEGRATION ***");
            ApplyEnergy(builder);
            ForceNumericalIntegration();

            using Solution solution4 = pvg4.GetSolution();
            ApplyOldBurnTimesToPhases(solution4);

            Optimizer pvg5 = builder.Build(_phases);
            pvg5.Bootstrap(solution3);
            pvg5.Run();

            if (!pvg5.Success())
                throw new Exception("Target unreachable");

            return pvg5;
        }

        private PhaseCollection InfiniteUpperNoCoast()
        {
            PhaseCollection bootphases = DupPhases(_phases);

            for (int i = 0; i < bootphases.Count; i++)
            {
                // remove the coast phase by setting burntime to zero
                if (i == _optimizedCoastPhase)
                    bootphases[i].bt = 0;

                // make all stages guided and unoptimized
                bootphases[i].Unguided     = false;
                bootphases[i].OptimizeTime = false;
            }

            // set only the top stage to infinite + optimized
            bootphases[bootphases.Count - 1].Infinite     = true;
            bootphases[bootphases.Count - 1].OptimizeTime = true;
            return bootphases;
        }

        private Optimizer InitialBootstrappingOptimized(Optimizer.OptimizerBuilder builder)
        {
            /*
             * Analytic initial bootstrapping with infinite upper stage and no coast, forced FPA attachment
             */

            Print("*** PHASE 1: DOING INITIAL INFINITE UPPER STAGE ***");
            ApplyFPA(builder);

            // guess the initial launch direction
            V3 enu = Astro.ENUHeadingForInclination(_incT, _r0);
            enu.z = 1.0; // add 45 degrees up
            V3 pvGuess = Astro.ENUToECI(_r0, enu).normalized / Sqrt(2);

            PhaseCollection bootphases = InfiniteUpperNoCoast();

            using Optimizer pvg = builder.Build(bootphases);
            pvg.Bootstrap(pvGuess, _r0.normalized / Sqrt(2));
            pvg.Run();

            if (!pvg.Success())
                throw new Exception("Target unreachable (infinite ISP)");

            /*
             * Analytic bootstrapping with finite upper stage and coast, forced FPA attachment
             */

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

            Print("*** PHASE 2: ADDING COAST AND FINITE UPPER STAGE ***");
            using Optimizer pvg2 = builder.Build(_phases);
            pvg2.Bootstrap(solution);
            pvg2.Run();

            /*
             * Numerical re-bootstrapping with finite upper stage and coast, forced FPA attachment
             */

            ForceNumericalIntegration();
            Optimizer pvg3 = builder.Build(_phases);
            if (pvg2.Success())
            {
                Print("*** PHASE 3: REDOING WITH NUMERICAL INTEGRATION ***");
                using Solution solution2 = pvg2.GetSolution();
                pvg3.Bootstrap(solution2);
                pvg3.Run();

                if (!pvg3.Success())
                    throw new Exception("Target unreachable after analytic convergence");
            }
            else
            {
                Print("*** PHASE 3: ATTEMPTING NUMERICAL INTEGRATION AFTER ANALYTICAL FAILURE ***");
                pvg3.Bootstrap(solution);
                pvg3.Run();

                if (!pvg3.Success())
                    throw new Exception("Target unreachable");
            }

            if (_attachAltFlag || _eccT < 1e-4)
                return pvg3;

            /*
             * Numerical re-bootstrapping with finite upper stage and coast, free attachment
             */

            using Solution solution3 = pvg3.GetSolution();

            ApplyKepler(builder);
            ApplyOldBurnTimesToPhases(solution3);

            Print("*** PHASE 4: RELAXING TO FREE ATTACHMENT ***");
            Optimizer pvg4 = builder.Build(_phases);
            pvg4.Bootstrap(solution3);
            pvg4.Run();

            if (!pvg4.Success())
            {
                Print("*** FREE ATTACHMENT FAILED, FALLING BACK TO PERIAPSIS ***");
                return pvg3;
            }

            /*
             * Sanity check to ensure free attachment did not attach to the apoapsis
             */

            using Solution solution4 = pvg4.GetSolution();

            if (solution3.Vgo(solution3.T0) < solution4.Vgo(solution4.T0))
            {
                // this probably means apoapsis attachment or wrong side of the periapsis
                Print(
                    $"*** PERIAPSIS ATTACHMENT IS MORE OPTIMAL ({solution3.Vgo(solution3.T0)} < {solution4.Vgo(solution4.T0)}) THAN FREE ATTACHMENT SOLN ***");
                return pvg3;
            }

            return pvg4;
        }

        private void ForceNumericalIntegration()
        {
            for (int i = 0; i < _phases.Count; i++)
                _phases[i].Analytic = false;
        }

        private double TotalBurntime(Solution solution) => solution.Tgo(solution.T0);

        private void ApplyOldBurnTimesToPhases(Solution oldSolution)
        {
            PhaseCollection oldphases = oldSolution.Phases;

            for (int i = 0; i < _phases.Count; i++)
            {
                if (!_phases[i].OptimizeTime || _phases[i].Coast)
                    continue;

                for (int j = 0; j < oldphases.Count; j++)
                {
                    if (!oldphases[j].OptimizeTime || oldphases[j].Coast)
                        continue;

                    _phases[i].bt = Min(oldSolution.Bt(j, _t0), _phases[i].tau * (1 - 1e-5));
                }
            }
        }

        // FIXME: this obviously creates garbage and needs to return an IDisposable wrapping List<Phases> that has a pool
        private PhaseCollection DupPhases(PhaseCollection oldphases)
        {
            var newphases = new PhaseCollection();

            foreach (Phase phase in oldphases)
                newphases.Add(phase.DeepCopy());

            return newphases;
        }

        public static AscentBuilder Builder() => new AscentBuilder();
    }
}
