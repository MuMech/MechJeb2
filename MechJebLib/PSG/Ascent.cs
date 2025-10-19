/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.PSG.Terminal;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public partial class Ascent
    {
        private readonly Problem         _problem;
        private          Optimizer?      _optimizer;
        private readonly PhaseCollection _phases;
        private readonly bool            _fixedBurnTime;
        private readonly AscentGuesser   _guesser;

        private Ascent(Problem problem, PhaseCollection phases, Solution? oldSolution, bool fixedBurnTime)
        {
            _problem       = problem;
            _phases        = phases;
            _solution      = oldSolution;
            _fixedBurnTime = fixedBurnTime;
            _guesser       = new AscentGuesser(_problem, _phases);
        }

        private readonly Solution? _solution;

        private Optimizer NewOptimizer(PhaseCollection phases, ITerminal terminal, Optimizer.Cost cost) => new Optimizer(_problem, phases, terminal, cost);

        public void Run()
        {
            if (_solution == null)
            {
                _optimizer = _fixedBurnTime
                    ? InitialBootstrappingFixed()
                    : InitialBootstrappingOptimized();
            }
            else
            {
                _optimizer = ConvergedOptimization(_solution);
            }
        }

        public Optimizer? GetOptimizer() => _optimizer;

        private Optimizer ConvergedOptimization(Solution oldSolution)
        {
            Optimizer.Cost cost = _fixedBurnTime ? Optimizer.Cost.MAX_ENERGY : Optimizer.Cost.MIN_THRUST_ACCEL;
            Optimizer      psg  = NewOptimizer(_phases, _problem.Terminal, cost);
            psg.TranscribePreviousSolution(oldSolution);
            Solution? solution = psg.Run();

            if (!psg.Success() || solution == null)
                throw new Exception("converged optimizer failed");

            return psg;
        }

        private Optimizer InitialBootstrappingFixed()
        {
            PhaseCollection bootPhases = _phases.DeepCopy();

            // FIXME: we may want to convert this to an optimized burntime circular orbit problem with an infinite upper stage for bootstrapping?
            foreach (Phase p in bootPhases)
                p.Unguided = false;

            Optimizer psg      = NewOptimizer(bootPhases, _problem.Terminal, Optimizer.Cost.MAX_ENERGY);
            Solution  solution = _guesser.InitialGuess(_problem.Terminal.IncT(), _problem.Terminal.TargetOrbitalEnergy());
            psg.TranscribePreviousBootSolution(solution);
            Solution? solution2 = psg.Run();

            if (!psg.Success() || solution2 == null)
                throw new Exception("Target unreachable (fixed bootstrapping)");

            PhaseCollection bootphases2 = _phases.DeepCopy();

            Optimizer psg2 = NewOptimizer(bootphases2, _problem.Terminal, Optimizer.Cost.MAX_ENERGY);
            psg2.TranscribePreviousBootSolution(solution2);
            Solution? solution3 = psg2.Run();

            if (!psg2.Success() || solution3 == null)
                throw new Exception("Target unreachable");

            return psg2;
        }

        private Optimizer InitialBootstrappingOptimized()
        {
            /*
             * Initial bootstrapping with infinite stage, forced FPA attachment
             */
            PhaseCollection bootPhases = _phases.DeepCopy();

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
            Optimizer psg      = NewOptimizer(bootPhases, _problem.Terminal.GetFPA(), Optimizer.Cost.MIN_THRUST_ACCEL);
            Solution? solution = _guesser.InitialGuess(_problem.Terminal.IncT(), _problem.Terminal.TargetOrbitalEnergy());
            psg.TranscribePreviousBootSolution(solution);
            solution = psg.Run();

            if (!psg.Success() || solution == null)
                throw new Exception("Target unreachable (infinite ISP)");

            // FIXME: should skip this if the infinite stage is zero length in the solution
            if (bootPhases[allowShutdownStage].Infinite)
            {
                bootPhases[allowShutdownStage].Infinite = false;

                DebugPrint("*** PHASE 3: REMOVING INFINITE FLAG ***");
                psg = NewOptimizer(bootPhases, _problem.Terminal.GetFPA(), Optimizer.Cost.MIN_THRUST_ACCEL);
                psg.TranscribePreviousBootSolution(solution);
                solution = psg.Run();

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
                psg = NewOptimizer(bootPhases, _problem.Terminal.GetFPA(), Optimizer.Cost.MIN_THRUST_ACCEL);
                psg.TranscribePreviousBootSolution(solution);
                solution = psg.Run();

                if (!psg.Success() || solution == null)
                    throw new Exception("Target unreachable (readding unguided stages)");
            }

            if (_problem.Terminal.IsFPA())
                return psg;

            /*
             * relaxing to free attachment
             */

            DebugPrint("*** PHASE 5: RELAXING TO FREE ATTACHMENT ***");
            Optimizer psg2 = NewOptimizer(bootPhases, _problem.Terminal, Optimizer.Cost.MIN_THRUST_ACCEL);
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
            //if (psg.PrimalFeasibility * 1000 < psg2.PrimalFeasibility)
            //{
            //    DebugPrint($"*** FREE ATTACHMENT PRIMAL FEASIBILITY ({psg2.PrimalFeasibility}) IS MUCH WORSE THAN PERIAPSIS ({psg.PrimalFeasibility}) ***");
            //    return psg;
            //}

            return psg2;
        }

        public static AscentBuilder Builder() => new AscentBuilder();
    }
}
