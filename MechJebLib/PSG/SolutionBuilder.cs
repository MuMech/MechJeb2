/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    public class SolutionBuilder
    {
        private readonly int _n;
        private readonly VariableProxy _vars;
        private readonly Problem _problem;
        private readonly PhaseCollection _phases;
        private readonly FlightPlan? _flightPlan;
        private readonly AscentProblem _ascentProblem;

        public SolutionBuilder(int n, VariableProxy vars, Problem problem, PhaseCollection phases, FlightPlan? flightPlan, AscentProblem ascentProblem)
        {
            _n = n;
            _vars = vars;
            _problem = problem;
            _phases = phases.DeepCopy();
            _flightPlan = flightPlan;
            _ascentProblem = ascentProblem;

            double[] bt = new double[_phases.Count];
            double[] mf = new double[_phases.Count];
            for (int p = 0; p < _phases.Count; p++)
            {
                bt[p] = _vars[p].Bt();
                mf[p] = _vars[p].M.Last;
            }

            AnalyzeStages(_phases, bt, mf);
        }

        private readonly double[] _tau = { 0, 0.5 - Sqrt(3) / 6, 0.5 + Sqrt(3) / 6 };

        // A phase shorter than this, in normalized time, is too short to fly (Phase.Skip).  Guidance pauses the optimizer before
        // the end of a phase (PSGSimulation's PreStageTime), so the burn guidance is flying is never this short in a new
        // Solution: a phase this short is one the optimizer shrank, or a pruned stage.
        internal const double SKIP_TIME = 1e-3;

        // Flags the phases for guidance from the optimizer's burn times bt and end masses mf, both normalized: the burn with the
        // precise shutdown, the terminal stage, the burns cut with propellant left, and the phases guidance skips.
        internal static void AnalyzeStages(PhaseCollection phases, IReadOnlyList<double> bt, IReadOnlyList<double> mf)
        {
            int optimizedShutdownIndex = -1;
            int terminalStageIndex = -1;
            bool pruningStages = false;

            for (int p = 0; p < phases.Count; p++)
            {
                Phase phase = phases[p];

                // is there unburned propellant going to be left in this stage?
                bool freeBurntimeLeft = mf[p] - phase.Mf > 1e-3;
                // is this is a prunable stage (negligible propellant use after we can prune)
                bool prunableStage = pruningStages && bt[p] < SKIP_TIME;

                // coasts are AllowShutdown, but there's no engine to shut down precisely, so they must not take the flag
                // from the burn before them
                if (phase.AllowShutdown && !phase.Coast && !prunableStage)
                    optimizedShutdownIndex = p;

                phase.PreciseShutdown = false;

                if (!phase.AllowShutdown || !prunableStage)
                    terminalStageIndex = p;

                phase.TerminalStage = false;

                // a burn the optimizer shut down with propellant left, which guidance cuts at its planned end
                phase.PropellantLeft = phase.AllowShutdown && !phase.Coast && freeBurntimeLeft;

                // hit a stage with some free propellant left
                if (phase.AllowShutdown && freeBurntimeLeft)
                    pruningStages = true;
            }

            if (optimizedShutdownIndex >= 0)
                phases[optimizedShutdownIndex].PreciseShutdown = true;

            Check.True(terminalStageIndex >= 0, "no terminal stage found");
            phases[terminalStageIndex].TerminalStage = true;

            // Guidance never flies a phase too short to fly, nor the pruned stages after the terminal stage (all of them shorter
            // than SKIP_TIME too).  The terminal stage is always flown, however short: it ends the ascent, and terminal guidance
            // trims it.  A burn the optimizer cuts with propellant left is flown however short, so that guidance cuts and
            // jettisons it: skipped, it would be flown on the next burn's steering and throttle, and nothing would stage it off
            // (autostaging won't drop an engine with propellant left).  The exception is the burn before a coast-during, which
            // the zero coast constraint shrinks with the coast (below): its propellant is the burn after the coast's.
            for (int p = 0; p < phases.Count; p++)
            {
                bool cutBurn = phases[p].PropellantLeft && !(p + 1 < phases.Count && phases.IsDuringCoast(p + 1));
                phases[p].Skip = p > terminalStageIndex || (p < terminalStageIndex && bt[p] < SKIP_TIME && !cutBurn);
            }

            // The zero coast constraint shrinks the burn before a coast-during to nothing with the coast (AscentProblem.
            // ZeroCoastConstraint), so both are skipped and guidance flies the stage as the one burn after the coast.  A burn
            // before it that is still flown would be flown until its stage burns out, with no hand-off to the burn after it.
            for (int p = 1; p < phases.Count; p++)
                if (phases.IsDuringCoast(p) && phases[p].Skip && !phases[p - 1].Skip)
                    Print($"[SolutionBuilder] [WARNING] the coast-during of phase {p} is too short to fly (bt: {bt[p]}), but the burn " +
                        $"before it isn't (bt: {bt[p - 1]})");
        }

        public Solution Build()
        {
            var solution = new Solution(_problem, _flightPlan);

            double ti = _problem.T0;

            // scratch buffers, CubicHermiteVecNode.Rent copies out of them so they can be reused for every node
            using var y0 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            using var dy0 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            using var y1 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);
            using var dy1 = Vec.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);

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
                    InterpolantValues(n, h, p, y0, dy0, y1, dy1);
                    interpolant.Append(CubicHermiteVecNode.Rent(t1, t2 - t1, y0, dy0, y1, dy1), t0, t3);
                }

                double tf = ti + bt;
                solution.AddSegment(interpolant, _phases[p]);
                ti = tf;
            }

            return solution;
        }

        private void InterpolantValues(int n, double h, int p, Vec y0, Vec dy0, Vec y1, Vec dy1)
        {
            Phase phase = _phases[p];
            PhaseProxy thisPhase = _vars[p];

            int k = 3 * n;

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
                    u0 = prevPhase.U.Last;
                }
                else
                {
                    u0 = _problem.U0;
                }

                PhaseProxy nextPhase = _vars[p + 1];
                V3 uf = nextPhase.U.First;

                // the slerp fraction has to be the node's own fraction of the coast, since the hermite is anchored at
                // t1 = (n + tau1) * h and t2 = (n + tau2) * h and gets extrapolated out to the ends of the segment.
                y0Layout.U = V3.Slerp(u0, uf, (n + _tau[1]) / _n);
                y1Layout.U = V3.Slerp(u0, uf, (n + _tau[2]) / _n);
                dy0U = (V3.Slerp(y0Layout.U, y1Layout.U, FINITE_DIFF) - y0Layout.U) / (FINITE_DIFF * htau);
                dy1U = (V3.Slerp(y1Layout.U, y0Layout.U, -FINITE_DIFF) - y1Layout.U) / (FINITE_DIFF * htau);

                y0Layout.T = y1Layout.T = 0;
                dyT = 0;
            }
            else if (phase.Unguided)
            {
                y0Layout.U = y1Layout.U = thisPhase.U.First;
                dy0U = dy1U = V3.zero;
                y0Layout.T = y1Layout.T = thisPhase.T.First;
                dyT = 0;
            }
            else
            {
                dy0U = dy1U = (thisPhase.U[k + 2] - thisPhase.U[k + 1]) / htau;
                y0Layout.U = thisPhase.U[k + 1];
                y1Layout.U = thisPhase.U[k + 2];
                y0Layout.T = thisPhase.T[k + 1];
                y1Layout.T = thisPhase.T[k + 2];
                dyT = (y1Layout.T - y0Layout.T) / htau;
            }

            var dy0Layout = new InterpolantLayout
            {
                R = thisPhase.V[k + 1],
                V = VDot(phase, y0Layout),
                M = -phase.Mdot * y0Layout.T,
                U = dy0U,
                T = dyT
            };
            var dy1Layout = new InterpolantLayout
            {
                R = thisPhase.V[k + 2],
                V = VDot(phase, y1Layout),
                M = -phase.Mdot * y1Layout.T,
                U = dy1U,
                T = dyT
            };

            y0Layout.CopyTo(y0);
            dy0Layout.CopyTo(dy0);
            y1Layout.CopyTo(y1);
            dy1Layout.CopyTo(dy1);
        }

        // The NLP's dynamics evaluated with constant duals (the discarded derivative work is negligible here).
        private V3 VDot(Phase phase, InterpolantLayout d) => _ascentProblem.VDot(phase, d.R, d.V, d.M, d.U, d.T).M;
    }
}
