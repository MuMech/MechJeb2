/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;

namespace MechJebLib.PSG
{
    /// <summary>
    ///     Where a first solve is: which of its optimizer passes runs, for how long, and how far that pass has got. The number of
    ///     iterations a pass needs is not known in advance, so there is no percentage.
    /// </summary>
    public readonly struct SolveProgress
    {
        /// <summary>The pass running, from 1, or 0 before the first.</summary>
        public readonly int Pass;

        /// <summary>The passes this solve makes if each one converges.</summary>
        public readonly int PassCount;

        /// <summary>What the pass running does, in words, or null before the first.</summary>
        public readonly string? Name;

        /// <summary>Seconds since the pass started.</summary>
        public readonly double PassSeconds;

        /// <summary>Iterations of the pass so far.</summary>
        public readonly int Iterations;

        /// <summary>Largest constraint violation at the pass's last iterate, in the solver's scaled units, NaN before the first.</summary>
        public readonly double Infeasibility;

        public SolveProgress(int pass, int passCount, string? name, double passSeconds, int iterations, double infeasibility)
        {
            Pass          = pass;
            PassCount     = passCount;
            Name          = name;
            PassSeconds   = passSeconds;
            Iterations    = iterations;
            Infeasibility = infeasibility;
        }
    }

    public partial class Ascent
    {
        // written by the solve's thread, read by the game's: each field on its own is whole, and a reading that mixes two passes
        // is only shown for a frame
        private volatile int       _pass;
        private volatile int       _passCount;
        private volatile string?   _passName;
        private volatile Optimizer? _running;
        private          long      _passStartTicks;

        /// <summary>Where the solve is, for a progress line. Safe to call from another thread while it runs.</summary>
        public SolveProgress GetProgress()
        {
            Optimizer? running = _running;
            long start = System.Threading.Interlocked.Read(ref _passStartTicks);
            double seconds = start == 0 ? 0 : (DateTime.UtcNow.Ticks - start) / (double)TimeSpan.TicksPerSecond;

            return new SolveProgress(_pass, _passCount, _passName, seconds, running?.LiveIterations ?? 0, running?.LiveInfeasibility ?? double.NaN);
        }

        // the passes of a first solve, counted before it starts: every one that runs when each converges
        private int CountPasses()
        {
            if (_fixedBurnTime)
                return 2;

            int count = 2; // every stage guided, then throttling
            foreach (Phase p in _phases)
            {
                if (!p.Unguided) continue;

                count++;
                break;
            }

            if (!_problem.Terminal.IsFPA()) count++;
            if (_problem.Rho0InvQAlphaMax > 0 || _problem.Rho0InvQMax > 0) count++;

            return count;
        }

        private void StartPass(string name, Optimizer? optimizer = null)
        {
            _running  = optimizer;
            _passName = name;
            _pass++;
            System.Threading.Interlocked.Exchange(ref _passStartTicks, DateTime.UtcNow.Ticks);
        }

        private Solution? RunPass(Optimizer optimizer)
        {
            _running = optimizer;
            return optimizer.Run();
        }
    }
}
