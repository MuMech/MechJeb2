/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */
﻿using MechJebLib.Primitives;

namespace MechJebLib.PSG
{
    public class SolutionBuilder
    {
        private readonly int             _n;
        private readonly VariableProxy   _vars;
        private readonly Problem         _problem;
        private readonly PhaseCollection _phases;

        private int _k => 2 * _n - 1;

        public SolutionBuilder(int n, VariableProxy vars, Problem problem, PhaseCollection phases)
        {
            _n       = n;
            _vars    = vars;
            _problem = problem;
            _phases  = phases.DeepCopy();
            AnalyzeStages();
        }

        private void AnalyzeStages()
        {
            int  optimizedShutdownIndex = -1;
            int  terminalStageIndex     = -1;
            bool pruningStages          = false;

            for (int p = 0; p < _phases.Count; p++)
            {
                Phase      phase     = _phases[p];
                PhaseProxy thisPhase = _vars[p];
                double     bt        = thisPhase.Bt();

                // is there unburned propellant going to be left in this stage?
                bool freeBurntimeLeft = phase.Bt - bt > 1e-3;
                // is this is a prunable stage (negligible propellant use after we can prune)
                bool prunableStage = pruningStages && bt < 1e-3;

                if (phase.AllowShutdown && !prunableStage)
                    optimizedShutdownIndex = p;

                phase.PreciseShutdown = false;

                if (!phase.AllowShutdown || !prunableStage)
                    terminalStageIndex = p;

                phase.TerminalStage = false;

                // hit a stage with some free propellant left
                if (phase.AllowShutdown && freeBurntimeLeft)
                    pruningStages = true;
            }

            if (optimizedShutdownIndex >= 0)
                _phases[optimizedShutdownIndex].PreciseShutdown = true;

            if (terminalStageIndex >= 0)
                _phases[terminalStageIndex].TerminalStage = true;
        }

        public Solution Build()
        {
            var solution = new Solution(_problem);

            double dv = 0;
            double ti = 0;

            for (int p = 0; p < _phases.Count; p++)
            {
                Phase      phase       = _phases[p];
                PhaseProxy thisPhase   = _vars[p];
                var        interpolant = Hn.Get(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);

                double bt = thisPhase.Bt();
                double h  = bt / (_n - 1);
                double m0 = thisPhase.M[0];

                using var outTangent = Vn.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
                using var inTangent  = Vn.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);

                for (int n = 0; n < _n - 1; n++)
                {
                    double   dt1    = n * h;
                    using Vn array1 = InterpolantValues(thisPhase, 2 * n, phase, dv, m0, dt1);

                    double   dt2    = (n + 0.5) * h;
                    using Vn array2 = InterpolantValues(thisPhase, 2 * n + 1, phase, dv, m0, dt2);

                    double   dt3    = (n + 1.0) * h;
                    using Vn array3 = InterpolantValues(thisPhase, 2 * n + 2, phase, dv, m0, dt3);

                    for (int i = 0; i < InterpolantLayout.INTERPOLANT_LAYOUT_LEN; i++)
                        outTangent[i] = (-3 * array1[i] + 4 * array2[i] - array3[i]) / h;

                    if (n == 0)
                        outTangent.CopyTo(inTangent);

                    interpolant.Add(ti + dt1, array1, inTangent, outTangent);

                    for (int i = 0; i < InterpolantLayout.INTERPOLANT_LAYOUT_LEN; i++)
                        inTangent[i] = (array1[i] - 4 * array2[i] + 3 * array3[i]) / h;

                    if (n < _n - 2) continue;

                    inTangent.CopyTo(outTangent);
                    interpolant.Add(ti + dt3, array3, inTangent, outTangent);
                }

                double tf = ti + bt;
                solution.AddSegment(ti, tf, interpolant, _phases[p]);
                ti = tf;

                dv = solution.DVBar(solution.Tmax);
            }

            return solution;
        }

        private static Vn InterpolantValues(PhaseProxy thisPhase, int k, Phase phase, double dv, double m0, double dt)
        {
            var layout = new InterpolantLayout
            {
                R  = thisPhase.R[k],
                V  = thisPhase.V[k],
                M  = phase.Coast ? thisPhase.M[0] : thisPhase.M[k],
                U  = phase.Unguided ? thisPhase.U[0] : thisPhase.U[k],
                Dv = dv + phase.DeltaVForTime(m0, dt)
            };

            var array = Vn.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            layout.CopyTo(array);
            return array;
        }
    }
}
