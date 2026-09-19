/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using static System.Math;

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

        private readonly double[] _tau = { 0, 0.5 - Sqrt(3) / 6, 0.5 + Sqrt(3) / 6 };

        private void AnalyzeStages()
        {
            int optimizedShutdownIndex = -1;
            int terminalStageIndex = -1;
            bool pruningStages = false;

            for (int p = 0; p < _phases.Count; p++)
            {
                Phase phase = _phases[p];
                PhaseProxy thisPhase = _vars[p];
                double mf = thisPhase.M.Last;
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
                var interpolant = VecInterpolant.Rent();

                double bt = thisPhase.Bt();
                double h = bt / _n;

                for (int n = 0; n < _n; n++)
                {
                    double t0 = ti + n * h;
                    double t1 = ti + (n + _tau[1]) * h;
                    double t2 = ti + (n + _tau[2]) * h;
                    double t3 = ti + (n + 1) * h;
                    (Vec y0, Vec dy0, Vec y1, Vec dy1) = InterpolantValues(n, h, p);
                    interpolant.Append(CubicHermiteVecNode.Rent(t1, t2 - t1, y0, dy0, y1, dy1), t0, t3);
                }

                double tf = ti + bt;
                solution.AddSegment(interpolant, _phases[p]);
                ti = tf;

                //solution.DVBar(solution.Tmax);
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

            var y0Layout = new InterpolantLayout { R = thisPhase.R[k + 1], V = thisPhase.V[k + 1], M = phase.Coast ? thisPhase.M.First : thisPhase.M[k + 1] };
            var y1Layout = new InterpolantLayout { R = thisPhase.R[k + 2], V = thisPhase.V[k + 2], M = phase.Coast ? thisPhase.M.First : thisPhase.M[k + 2] };

            const double FINITE_DIFF = 1e-8;

            V3 dy0U, dy1U;
            double dyT;
            double htau = h * (_tau[2] - _tau[1]);

            if (phase.GuidedCoast)
            {
                V3 u0;

                if (p - 1 >= 0)
                {
                    PhaseProxy prevPhase = _vars[p - 1];
                    u0 = prevPhase.U.Last * V3.forward;
                }
                else
                {
                    u0 = _problem.U0;
                }

                PhaseProxy nextPhase = _vars[p + 1];
                V3 uf = nextPhase.U.First * V3.forward;

                y0Layout.U = V3.Slerp(u0, uf, (double)n / (_n + 1));
                y1Layout.U = V3.Slerp(u0, uf, (double)(n + 1) / (_n + 1));
                dy0U = (V3.Slerp(y0Layout.U, y1Layout.U, FINITE_DIFF) - y0Layout.U) / (FINITE_DIFF * h);
                dy1U = (V3.Slerp(y1Layout.U, y0Layout.U, -FINITE_DIFF) - y1Layout.U) / (FINITE_DIFF * h);

                y0Layout.T = y1Layout.T = 0;
                dyT = 0;
            }
            else if (phase.Unguided)
            {
                y0Layout.U = y1Layout.U = thisPhase.U.First * V3.forward;
                dy0U = dy1U = V3.zero;
                y0Layout.T = y1Layout.T = thisPhase.T.First;
                dyT = 0;
            }
            else
            {
                dy0U = dy1U = (thisPhase.U[k + 2] * V3.forward - thisPhase.U[k + 1] * V3.forward) / htau;
                y0Layout.U = thisPhase.U[k + 1] * V3.forward;
                y1Layout.U = thisPhase.U[k + 2] * V3.forward;
                y0Layout.T = thisPhase.T[k + 1];
                y1Layout.T = thisPhase.T[k + 2];
                dyT = (y1Layout.T - y0Layout.T) / htau;
            }

            var dy0Layout = new InterpolantLayout
            {
                R = thisPhase.V[k + 1],
                V = VDot(y0Layout, _problem, phase),
                M = -phase.Mdot * y0Layout.T,
                U = dy0U,
                T = dyT
            };
            var dy1Layout = new InterpolantLayout
            {
                R = thisPhase.V[k + 2],
                V = VDot(y1Layout, _problem, phase),
                M = -phase.Mdot * y1Layout.T,
                U = dy1U,
                T = dyT
            };

            y0Layout.CopyTo(y0);
            dy0Layout.CopyTo(dy0);
            y1Layout.CopyTo(y1);
            dy1Layout.CopyTo(dy1);

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
            return -d.R / r3 + vacThrust / d.M * d.U * d.T;
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
            double normAtmosphere = Exp(-(r - rBody) / h0);
            double normAtmosphere2 = Exp(-(r - r0) / h0);
            V3 drag = 0.5 * rho0CdAref * normAtmosphere * vr.sqrMagnitude * vr.normalized;
            //T = ṁ [v_e_sl + (v_e_vac - v_e_sl)(1 - p_amb/p₀)]
            double thrust = mdot * (vexCurrent + (vexVacuum - vexCurrent) * (1.0 - normAtmosphere2));
            return -d.R / r3 + thrust / d.M * d.U * d.T - drag / d.M;
        }
    }
}
