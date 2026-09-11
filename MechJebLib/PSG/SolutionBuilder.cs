/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;

namespace MechJebLib.PSG
{
    public class SolutionBuilder
    {
        private readonly int _n;
        private readonly VariableProxy _vars;
        private readonly Problem _problem;
        private readonly PhaseCollection _phases;

        public SolutionBuilder(int n, VariableProxy vars, Problem problem, PhaseCollection phases)
        {
            _n = n;
            _vars = vars;
            _problem = problem;
            _phases = phases.DeepCopy();
            AnalyzeStages();
        }

        private void AnalyzeStages()
        {
            int optimizedShutdownIndex = -1;
            int terminalStageIndex = -1;
            bool pruningStages = false;

            for (int p = 0; p < _phases.Count; p++)
            {
                Phase phase = _phases[p];
                PhaseProxy thisPhase = _vars[p];
                double mf = thisPhase.M[-1];
                double bt = thisPhase.Bt();

                // is there unburned propellant going to be left in this stage?
                bool freeBurntimeLeft = mf - phase.Mf > 1e-3;
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

            double ti = 0;

            for (int p = 0; p < _phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];
                var interpolant = Interpolant.Rent();

                double bt = thisPhase.Bt();
                double h = bt / _n;

                for (int n = 0; n < _n; n++)
                {
                    double t0 = ti + n * h;
                    double t1 = ti + (n + 1) * h;
                    (Vec y0, Vec dy0, Vec y1, Vec dy1) = InterpolantValues(n, h, p);
                    interpolant.Append(CubicHermiteNode.Rent(t0, h, y0, dy0, y1, dy1), t1);
                }

                double tf = ti + bt;
                solution.AddSegment(interpolant, _phases[p]);
                ti = tf;

                solution.DVBar(solution.Tmax);
            }

            return solution;
        }

        private (Vec y0, Vec dy0, Vec y1, Vec dy1) InterpolantValues(int n, double h, int p)
        {
            Phase phase = _phases[p];
            PhaseProxy thisPhase = _vars[p];

            int k = 3 * n;

            var y0 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            var dy0 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            var y1 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            var dy1 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);

            var y0layout = new InterpolantLayout { R = thisPhase.R[k], V = thisPhase.V[k], M = phase.Coast ? thisPhase.M[0] : thisPhase.M[k] };
            var y1layout = new InterpolantLayout { R = thisPhase.R[k + 3], V = thisPhase.V[k + 3], M = phase.Coast ? thisPhase.M[0] : thisPhase.M[k + 3] };

            V3 uSlope;

            if (phase.GuidedCoast)
            {
                PhaseProxy prevPhase = _vars[p-1];
                PhaseProxy nextPhase = _vars[p+1];
                bool nextUnCollocatedControl = _phases[p+1].Unguided;
                bool prevUnCollocatedControl = _phases[p-1].Unguided;

                int nextControlIndex = nextUnCollocatedControl ? 0 : 1;
                int prevControlIndex = prevUnCollocatedControl ? -1 : -2;

                V3 u0 = prevPhase.U[prevControlIndex];
                V3 uf = nextPhase.U[nextControlIndex];

                y0layout.U = V3.Slerp(u0, uf, (double)n / (_n + 1));
                y1layout.U = V3.Slerp(u0, uf, (double)(n + 1) / (_n + 1));
                uSlope = (y1layout.U - y0layout.U) / h;
            }
            else if (phase.Unguided)
            {
                y0layout.U = thisPhase.U[0];
                y1layout.U = thisPhase.U[0];
                uSlope = V3.zero;
            }
            else
            {
                double tau1 = 0.5 - Math.Sqrt(3) / 6;
                double tau2 = 0.5 + Math.Sqrt(3) / 6;
                uSlope = (thisPhase.U[k + 2] - thisPhase.U[k + 1]) / ((tau2 - tau1) * h);
                y0layout.U = thisPhase.U[k + 1] - uSlope * tau1 * h;
                y1layout.U = thisPhase.U[k + 2] + uSlope * tau1 * h;
            }

            var dy0layout = new InterpolantLayout { R = thisPhase.V[k], V = VDot(y0layout, _problem, phase), M = -phase.Mdot * y0layout.U.magnitude, U = uSlope };
            var dy1layout = new InterpolantLayout { R = thisPhase.V[k + 3], V = VDot(y1layout, _problem, phase), M = -phase.Mdot * y1layout.U.magnitude, U = uSlope };

            y0layout.CopyTo(y0);
            dy0layout.CopyTo(dy0);
            y1layout.CopyTo(y1);
            dy1layout.CopyTo(dy1);

            return (y0, dy0, y1, dy1);
        }

        //TODO: this duplicates code with AscentProblem

        private static V3 VDot(InterpolantLayout d, Problem problem, Phase phase)
        {
            double rho0CdAref = problem.Rho0CdAref;
            double h0 = problem.H0;

            return h0 > 0 && rho0CdAref > 0 ? VDotAtmo(d, problem, phase) : VDotVacuum(d, phase);
        }

        private static V3 VDotVacuum(InterpolantLayout d, Phase phase)
        {
            double vacThrust = phase.VacThrust;

            double r3 = d.R.sqrMagnitude * d.R.magnitude;
            return -d.R / r3 + vacThrust / d.M * d.U;
        }

        private static V3 VDotAtmo(InterpolantLayout d, Problem problem, Phase phase)
        {
            double rho0CdAref = problem.Rho0CdAref;
            double rBody = problem.RBody;
            double h0 = problem.H0;
            double r0 = problem.R0.magnitude;
            V3 w = problem.W;

            double mdot = phase.Mdot;
            double vexCurrent = phase.VexCurrent;
            double vexVacuum = phase.VexVacuum;

            double r = d.R.magnitude;
            double r3 = d.R.sqrMagnitude * r;
            V3 vr = d.V - V3.Cross(w, d.R);
            double normAtmosphere = Math.Exp(-(r - rBody) / h0);
            double normAtmosphere2 = Math.Exp(-(r - r0) / h0);
            V3 drag = 0.5 * rho0CdAref * normAtmosphere * vr.sqrMagnitude * vr.normalized;
            //T = ṁ [v_e_sl + (v_e_vac - v_e_sl)(1 - p_amb/p₀)]
            double thrust = mdot * (vexCurrent + (vexVacuum - vexCurrent) * (1.0 - normAtmosphere2));
            return -d.R / r3 + thrust / d.M * d.U - drag / d.M;
        }
    }
}
