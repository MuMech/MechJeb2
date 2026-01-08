/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
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
            _guesser       = new AscentGuesser(_problem);
        }

        private readonly Solution? _solution;

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
            var            psg  = new Optimizer(_problem, _phases, _problem.Terminal, cost);
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

            var      psg      = new Optimizer(_problem, bootPhases, _problem.Terminal, Optimizer.Cost.MAX_ENERGY);
            Solution solution = _guesser.InitialGuess(bootPhases, _problem.Terminal.IncT(), _problem.Terminal.TargetOrbitalEnergy());
            psg.TranscribePreviousBootSolution(solution);
            Solution? solution2 = psg.Run();

            if (!psg.Success() || solution2 == null)
                throw new Exception("Target unreachable (fixed bootstrapping)");

            PhaseCollection bootphases2 = _phases.DeepCopy();

            var psg2 = new Optimizer(_problem, bootphases2, _problem.Terminal, Optimizer.Cost.MAX_ENERGY);
            psg2.TranscribePreviousBootSolution(solution2);
            Solution? solution3 = psg2.Run();

            if (!psg2.Success() || solution3 == null)
                throw new Exception("Target unreachable");

            return psg2;
        }

        private Optimizer InitialBootstrappingOptimized()
        {
            Optimizer psg      = InitialBootstrappingOptimizedWithoutQAlpha();
            Solution? solution = psg.Solution;

            if (_problem.Rho0InvQAlphaMax <= 0 || solution == null)
                return psg;

            DebugPrint("*** PHASE 6: Imposing QAlpha Constraints ***");
            var psg2 = new Optimizer(_problem, psg._phases, _problem.Terminal, Optimizer.Cost.MIN_THRUST_ACCEL);
            psg2.TranscribePreviousBootSolution(solution);
            Solution? solution2 = psg2.Run();

            if (!psg2.Success() || solution2 == null)
                throw new Exception("QAlpha failed (ignore)");

            return psg2;
        }

        private Optimizer InitialBootstrappingOptimizedWithoutQAlpha()
        {
            /*
             * Initial bootstrapping with infinite stage, forced FPA attachment
             */

            PhaseCollection bootPhases = _phases.DeepCopy();

            for (int p = 0; p < bootPhases.Count; p++)
            {
                bootPhases[p].Tagged = false;
                if (bootPhases[p].Unguided)
                {
                    bootPhases[p].Unguided = false;
                    bootPhases[p].Tagged   = true;
                }
            }

            Problem problemNoQa = _problem.WithoutQAlpha();

            DebugPrint("*** PHASE 1: DOING INITIAL ALL-GUIDED ROCKET ***");
            var       psg      = new Optimizer(problemNoQa, bootPhases, _problem.Terminal.GetFPA(), Optimizer.Cost.MIN_THRUST_ACCEL);
            Solution? solution = _guesser.InitialGuess(bootPhases, _problem.Terminal.IncT(), _problem.Terminal.TargetOrbitalEnergy());
            psg.TranscribePreviousBootSolution(solution);
            solution = psg.Run();

            if (!psg.Success() || solution == null)
                throw new Exception("Target unreachable (bootstrapping)");

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
                psg = new Optimizer(problemNoQa, bootPhases, _problem.Terminal.GetFPA(), Optimizer.Cost.MIN_THRUST_ACCEL);
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
            var psg2 = new Optimizer(problemNoQa, bootPhases, _problem.Terminal, Optimizer.Cost.MIN_THRUST_ACCEL);
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
                DebugPrint($"*** PERIAPSIS ATTACHMENT IS MORE OPTIMAL ({solution.Vgo(solution2.T0)} < {solution2.Vgo(solution2.T0)}) THAN FREE ATTACHMENT SOLUTION ***");
                return psg;
            }

            return psg2;
        }

        public static AscentBuilder Builder() => new AscentBuilder();
    }
}
