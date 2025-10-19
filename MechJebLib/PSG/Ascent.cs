/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public partial class Ascent
    {
        private readonly AscentBuilder _input;
        private          double        _eccT;
        private          double        _gammaT;
        private          Optimizer?    _optimizer;
        private          double        _smaT;

        private double _vT;

        private Ascent(AscentBuilder builder)
        {
            _input = builder;
        }

        private List<Phase> _phases        => _input._phases;
        private V3          _r0            => _input._r0;
        private V3          _v0            => _input._v0;
        private V3          _u0            => _input._u0;
        private double      _t0            => _input._t0;
        private double      _mu            => _input._mu;
        private double      _rbody         => _input._rbody;
        private double      _peR           => _input._peR;
        private double      _apR           => _input._apR;
        private double      _attR          => _input._attR;
        private double      _incT          => _input._incT;
        private double      _lanT          => _input._lanT;
        private double      _fpaT          => _input._fpaT;
        private double      _hT            => _input._hT;
        private bool        _attachAltFlag => _input._attachAltFlag;
        private bool        _lanflag       => _input._lanflag;
        private bool        _fixedBurnTime => _input._fixedBurnTime;
        private Solution?   _solution      => _input._solution;

        public void Run()
        {
            (_smaT, _eccT) = Astro.SmaEccFromApsides(_peR, _apR);

            using Optimizer.OptimizerBuilder builder = Optimizer.Builder()
                .Initial(_r0, _v0, _u0, _t0, _mu, _rbody)
                .TerminalConditions(_hT);

            if (_solution == null)
            {
                _optimizer = _fixedBurnTime
                    ? InitialBootstrappingFixed(builder)
                    : InitialBootstrappingOptimized(builder);
            }
            else
            {
                _optimizer = ConvergedOptimization(builder, _solution);
            }
        }

        public Optimizer? GetOptimizer() => _optimizer;

        private Optimizer ConvergedOptimization(Optimizer.OptimizerBuilder builder, Solution oldSolution)
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

            Optimizer psg = builder.Build(_phases);
            psg.TranscribePreviousSolution(oldSolution);
            Solution? solution = psg.Run();

            if (!psg.Success() || solution == null)
                throw new Exception("converged optimizer failed");

            if (_attachAltFlag || _fixedBurnTime || _eccT < 1e-4)
                return psg;

            ApplyFPA(builder);

            Optimizer psg2 = builder.Build(_phases);
            psg2.TranscribePreviousBootSolution(solution);
            Solution? solution2 = psg2.Run();

            return psg2.Success() ? psg2 : psg;
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
            ApplyEnergy(builder);

            List<Phase> bootPhases = DupPhases(_phases);

            // FIXME: we may want to convert this to an optimized burntime circular orbit problem with an infinite upper stage for bootstrapping?
            foreach (Phase p in bootPhases)
                p.Unguided = false;

            Optimizer psg      = builder.Build(bootPhases);
            Solution  solution = psg.InitialGuess(_incT);
            psg.TranscribePreviousBootSolution(solution);
            Solution? solution2 = psg.Run();

            if (!psg.Success() || solution2 == null)
                throw new Exception("Target unreachable (fixed bootstrapping)");

            List<Phase> bootphases2 = DupPhases(_phases);

            Optimizer psg2 = builder.Build(bootphases2);
            psg2.TranscribePreviousBootSolution(solution2);
            Solution? solution3 = psg2.Run();

            if (!psg2.Success() || solution3 == null)
                throw new Exception("Target unreachable");

            return psg2;
        }

        private Optimizer InitialBootstrappingOptimized(Optimizer.OptimizerBuilder builder)
        {
            /*
             * Initial bootstrapping with infinite stage, forced FPA attachment
             */

            ApplyFPA(builder);
            List<Phase> bootPhases = DupPhases(_phases);

            // set everything to guided, find the last allowShutdown stage
            var allowShutdownIndexes = new List<int>();
            for (int p = 0; p < bootPhases.Count; p++)
            {
                bootPhases[p].Tagged = false;
                if (bootPhases[p].Unguided)
                {
                    bootPhases[p].Unguided = false;
                    bootPhases[p].Tagged   = true;
                }

                if (bootPhases[p].AllowShutdown && !bootPhases[p].Coast)
                    allowShutdownIndexes.Add(p);
            }

            int allowShutdownStage = allowShutdownIndexes[allowShutdownIndexes.Count - 1];
            bootPhases[allowShutdownStage].Infinite              = true;
            bootPhases[allowShutdownStage].AllowInfiniteBurntime = true;

            DebugPrint("*** PHASE 1: DOING INITIAL ALL-GUIDED ROCKET WITH INFINITE STAGE ***");
            Optimizer psg      = builder.Build(bootPhases);
            Solution? solution = psg.InitialGuess(_incT);
            psg.TranscribePreviousBootSolution(solution);
            solution      = psg.Run();

            if (!psg.Success() || solution == null)
                throw new Exception("Target unreachable (infinite ISP)");

            // FIXME: should skip this if the infinite stage is zero length in the solution
            if (bootPhases[allowShutdownStage].Infinite)
            {
                bootPhases[allowShutdownStage].Infinite = false;

                DebugPrint("*** PHASE 3: REMOVING INFINITE FLAG ***");
                psg = builder.Build(bootPhases);
                psg.TranscribePreviousBootSolution(solution);
                solution      = psg.Run();

                if (!psg.Success() || solution == null)
                    throw new Exception("Target unreachable");
            }

            bool reConverge = false;

            foreach (Phase p in bootPhases)
            {
                if (!p.Tagged) continue;

                reConverge = true;
                p.Unguided = true;
                p.Tagged   = false;
            }

            if (reConverge)
            {
                DebugPrint("*** PHASE 4: ADDING BACK UNGUIDED STAGES ***");
                psg = builder.Build(bootPhases);
                psg.TranscribePreviousBootSolution(solution);
                solution      = psg.Run();

                if (!psg.Success() || solution == null)
                    throw new Exception("Target unreachable (readding unguided stages)");
            }

            if (_attachAltFlag || _eccT < 1e-4)
                return psg;

            /*
             * relaxing to free attachment
             */

            ApplyKepler(builder);

            DebugPrint("*** PHASE 5: RELAXING TO FREE ATTACHMENT ***");
            Optimizer psg2 = builder.Build(bootPhases);
            psg2.TranscribePreviousBootSolution(solution);
            Solution? solution2 = psg2.Run();

            if (!psg2.Success() || solution2 == null)
            {
                DebugPrint("*** FREE ATTACHMENT FAILED, FALLING BACK TO PERIAPSIS ***");
                return psg;
            }

            // this should catch if free attachment picked the apoapsis accidentally
            if (solution.Vgo(solution2.T0) < solution2.Vgo(solution2.T0))
            {
                DebugPrint($"*** PERIAPSIS ATTACHMENT IS MORE OPTIMAL ({solution.Vgo(solution2.T0)} < {solution2.Vgo(solution2.T0)}) THAN FREE ATTACHMENT SOLN ***");
                return psg;
            }

            // this catches issues with circular orbits where the kepler constraints fail to converge well
            if (psg.PrimalFeasibility * 1000 < psg2.PrimalFeasibility)
            {
                DebugPrint($"*** FREE ATTACHMENT PRIMAL FEASIBILITY ({psg2.PrimalFeasibility}) IS MUCH WORSE THAN PERIAPSIS ({psg.PrimalFeasibility}) ***");
                return psg;
            }

            return psg2;
        }

        // FIXME: this obviously creates garbage and needs to return an IDisposable wrapping List<Phases> that has a pool
        private List<Phase> DupPhases(List<Phase> oldphases)
        {
            var newphases = new List<Phase>();

            foreach (Phase phase in oldphases)
                newphases.Add(phase.DeepCopy());

            return newphases;
        }

        public static AscentBuilder Builder() => new AscentBuilder();
    }
}
