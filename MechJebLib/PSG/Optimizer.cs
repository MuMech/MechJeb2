/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Threading;
using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLib.PSG
{
    public class Optimizer : IDisposable
    {
        public enum Cost { MAX_MASS, MAX_ENERGY, MIN_THRUST_ACCEL, MIN_TIME }

        public enum OptimStatus { CREATED, SUCCESS, CANCELLED, FAILED }

        private readonly Problem         _problem;
        private readonly PhaseCollection _phases;
        private readonly ITerminal       _terminal;
        private readonly Cost            _cost;
        private readonly VariableProxy   _vars;

        private readonly alglib.minnlcreport      _rep = new alglib.minnlcreport();
        private readonly alglib.ndimensional_sjac _constraintHandle;
        private          alglib.minnlcstate       _state = new alglib.minnlcstate();

        private CancellationToken _timeoutToken;

        public int         Iterations;
        public OptimStatus Status;
        public int         TerminationType;
        public double      PrimalFeasibility;
        public double      Objective;
        public Solution?   Solution;

        private int    _k                  => 2 * N - 1;
        public  int    N                   { get; set; } = 6;
        public  int    Maxits              { get; set; } = 4000;
        public  double SQPTrustRegionLimit { get; set; } = 1e-3;
        public  double Epsf                { get; set; } = 0; // 1e-9;
        public  double Diffstep            { get; set; } = 1e-9;
        public  double Stpmax              { get; set; } = 10;
        public  int    OptimizerTimeout    { get; set; } = 120_000; // milliseconds

        public Optimizer(Problem problem, PhaseCollection phases, ITerminal terminal, Cost cost)
        {
            _phases           = phases.DeepCopy();
            _terminal         = terminal;
            _cost             = cost;
            _vars             = new VariableProxy(_phases, _terminal, N);
            _problem          = problem;
            _constraintHandle = ConstraintFunction;
            Status            = OptimStatus.CREATED;
            _xGuess           = Array.Empty<double>();
            nu                = Array.Empty<double>();
            nl                = Array.Empty<double>();
        }

        public void Dispose()
        {
        }

        private void CalculatePrimalFeasibility(double[] f)
        {
            Objective         = f[0];
            PrimalFeasibility = 0;
            for (int i = 1; i < f.Length; i++)
            {
                double upper = f[i] - nu[i - 1];
                double lower = nl[i - 1] - f[i];

                if (upper > 0)
                {
                    if (upper > 1e-5)
                        DebugPrint($"Constraint {i - 1} violated: {f[i]} upper limit: {upper}");
                    PrimalFeasibility += upper * upper;
                }
                else if (lower > 0)
                {
                    if (lower > 1e-5)
                        DebugPrint($"Constraint {i - 1} violated: {f[i]} lower limit: {lower}");
                    PrimalFeasibility += lower * lower;
                }
            }

            PrimalFeasibility = Sqrt(PrimalFeasibility);
        }

        private void ConstraintFunction(double[] x, double[] f, alglib.sparsematrix j, object? o)
        {
            _timeoutToken.ThrowIfCancellationRequested();
            _vars.WrapVars(x);

            int ci = 0;
            f[ci++] = ObjectiveFunction(j);

            for (int p = 0; p < _phases.Count; p++)
            {
                ci = DynamicConstraints(f, j, p, ci);
                ci = StagingConstraints(f, j, p, ci);
                ci = ControlNormConstraints(f, j, p, ci);
                ci = UnguidedControlConstraints(f, j, p, ci);

                if (p == _phases.Count - 1) break;

                ci = ContinuityConstraints(f, j, p, ci);
            }

            PhaseProxy lastPhase = _vars[-1];
            _terminal.Constraints(x, lastPhase.R.Idx(-1), lastPhase.V.Idx(-1), f, j, ref ci);

            if (ci != _vars.TotalConstraints + 1)
                throw new Exception("Constraint num mismatch");
        }

        private int UnguidedControlConstraints(double[] f, alglib.sparsematrix j, int p, int ci)
        {
            PhaseProxy thisPhase = _vars[p];

            if (!_phases[p].Unguided)
                return ci;

            if (p == 0)
            {
                f[ci++] = _problem.U0.x;
                f[ci++] = _problem.U0.y;
                f[ci++] = _problem.U0.z;
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendemptyrow(j);
            }
            else
            {
                PhaseProxy prevPhase = _vars[p - 1];

                V3 u  = thisPhase.U[0];
                V3 u0 = prevPhase.U[-1];

                f[ci++] = u.x - u0.x;
                f[ci++] = u.y - u0.y;
                f[ci++] = u.z - u0.z;
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, prevPhase.Ux.Idx(-1), -1.0);
                alglib.sparseappendelement(j, thisPhase.Ux.Idx(0), 1.0);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, prevPhase.Uy.Idx(-1), -1.0);
                alglib.sparseappendelement(j, thisPhase.Uy.Idx(0), 1.0);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, prevPhase.Uz.Idx(-1), -1.0);
                alglib.sparseappendelement(j, thisPhase.Uz.Idx(0), 1.0);
            }

            return ci;
        }

        private int ControlNormConstraints(double[] f, alglib.sparsematrix j, int p, int ci)
        {
            PhaseProxy thisPhase = _vars[p];

            // TODO: what about e.g. unconstrained initial coast initial controls?
            if (_phases[p].GuidedCoast)
                return ci;

            if (_phases[p].Unguided)
            {
                V3 u = thisPhase.U[0];
                f[ci++] = u.sqrMagnitude - 1.0;

                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Ux.Idx(0), 2 * u.x);
                alglib.sparseappendelement(j, thisPhase.Uy.Idx(0), 2 * u.y);
                alglib.sparseappendelement(j, thisPhase.Uz.Idx(0), 2 * u.z);
            }
            else
            {
                for (int k = 0; k < _k; k++)
                {
                    V3  u  = thisPhase.U[k];
                    f[ci++] = u.sqrMagnitude - 1.0;

                    alglib.sparseappendemptyrow(j);
                    alglib.sparseappendelement(j, thisPhase.Ux.Idx(k), 2 * u.x);
                    alglib.sparseappendelement(j, thisPhase.Uy.Idx(k), 2 * u.y);
                    alglib.sparseappendelement(j, thisPhase.Uz.Idx(k), 2 * u.z);
                }
            }

            return ci;
        }

        private int ContinuityConstraints(double[] f, alglib.sparsematrix j, int p, int ci)
        {
            PhaseProxy thisPhase = _vars[p];
            PhaseProxy nextPhase = _vars[p + 1];

            // continuity conditions per phase
            V3 rMinus = thisPhase.R[-1];
            V3 vMinus = thisPhase.V[-1];
            V3 rPlus  = nextPhase.R[0];
            V3 vPlus  = nextPhase.V[0];

            f[ci++] = rPlus.x - rMinus.x;
            f[ci++] = rPlus.y - rMinus.y;
            f[ci++] = rPlus.z - rMinus.z;
            f[ci++] = vPlus.x - vMinus.x;
            f[ci++] = vPlus.y - vMinus.y;
            f[ci++] = vPlus.z - vMinus.z;

            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Rx.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Rx.Idx(0), 1.0);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Ry.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Ry.Idx(0), 1.0);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Rz.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Rz.Idx(0), 1.0);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Vx.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Vx.Idx(0), 1.0);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Vy.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Vy.Idx(0), 1.0);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Vz.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Vz.Idx(0), 1.0);

            // continuity of the controls
            V3 uMinus = thisPhase.U[-1];
            V3 uPlus  = nextPhase.U[0];

            f[ci++] = uPlus.x - uMinus.x;
            f[ci++] = uPlus.y - uMinus.y;
            f[ci++] = uPlus.z - uMinus.z;

            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Ux.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Ux.Idx(0), 1.0);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Uy.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Uy.Idx(0), 1.0);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, thisPhase.Uz.Idx(-1), -1.0);
            alglib.sparseappendelement(j, nextPhase.Uz.Idx(0), 1.0);
            return ci;
        }

        private int StagingConstraints(double[] f, alglib.sparsematrix j, int p, int ci)
        {
            PhaseProxy thisPhase          = _vars[p];

            int        nextBurnPhaseIndex = -1;

            for (int p2 = p + 1; p2 < _phases.Count; p2++)
            {
                if (_phases[p2].Coast || !_phases[p2].AllowShutdown)
                    continue;
                nextBurnPhaseIndex = p2;
                break;
            }

            if (nextBurnPhaseIndex < 0 || _phases[p].Coast || !_phases[p].AllowShutdown)
            {
                f[ci++] = 0;
                alglib.sparseappendemptyrow(j);
            }
            else
            {
                PhaseProxy nextBurnPhase = _vars[nextBurnPhaseIndex];

                // next burn time
                double a = nextBurnPhase.Bt();
                // this burn time remaining
                double b = _phases[p].bt - thisPhase.Bt();
                double u = a * a + b * b + 2e-6;

                // smoothed Fischer-Burmeister constraint on burn times
                f[ci++] = Sqrt(u) - (a + b);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.BtIdx(), 1 - b / Sqrt(u));
                alglib.sparseappendelement(j, nextBurnPhase.BtIdx(), a / Sqrt(u) - 1);

                // relaxed bilinear constraint on burn times (needs inequality constraint)
                //f[ci++] = a * b;  //where a <= 1e-6 && b <= 1e-6
            }

            return ci;
        }

        private int DynamicConstraints(double[] f, alglib.sparsematrix j, int p, int ci)
        {
            PhaseProxy thisPhase = _vars[p];

            double mdot   = _phases[p].Mdot;
            double thrust = _phases[p].Thrust;
            double h      = thisPhase.Bt() / (N - 1);

            // dynamical constraints per phase
            for (int n = 0; n < N - 1; n += 1)
            {
                int idx = 2 * n;

                V3 r0 = thisPhase.R[idx];
                V3 r1 = thisPhase.R[idx + 1];
                V3 r2 = thisPhase.R[idx + 2];
                V3 v0 = thisPhase.V[idx];
                V3 v1 = thisPhase.V[idx + 1];
                V3 v2 = thisPhase.V[idx + 2];

                double m0, m1, m2;

                if (_phases[p].Coast)
                {
                    m0 = m1 = m2 = thisPhase.M[0];
                }
                else
                {
                    m0 = thisPhase.M[idx];
                    m1 = thisPhase.M[idx + 1];
                    m2 = thisPhase.M[idx + 2];
                }

                V3 u0, u1, u2;

                if (_phases[p].GuidedCoast)
                {
                    u0 = u1 = u2 = V3.zero;
                }
                else if (_phases[p].Unguided)
                {
                    u0 = u1 = u2 = thisPhase.U[0];
                }
                else
                {
                    u0 = thisPhase.U[idx];
                    u1 = thisPhase.U[idx + 1];
                    u2 = thisPhase.U[idx + 2];
                }

                double r0M = r0.magnitude;
                double r02 = r0M * r0M;
                double r03 = r02 * r0M;
                double r05 = r03 * r02;
                double r1M = r1.magnitude;
                double r12 = r1M * r1M;
                double r13 = r12 * r1M;
                double r15 = r13 * r12;
                double r2M = r2.magnitude;
                double r22 = r2M * r2M;
                double r23 = r22 * r2M;
                double r25 = r23 * r22;

                double h6 = h / 6.0;
                double h8 = 0.125 * h;

                // dr/dt = v

                V3 dR1 = r2 - r0 - h6 * (v0 + 4 * v1 + v2);
                V3 dR2 = r1 - 0.5 * (r0 + r2) - h8 * (v0 - v2);

                f[ci++] = dR1.x;
                f[ci++] = dR2.x;
                f[ci++] = dR1.y;
                f[ci++] = dR2.y;
                f[ci++] = dR1.z;
                f[ci++] = dR2.z;

                V3 dR1dBt = -1.0 / 6.0 / (N - 1) * (v0 + 4 * v1 + v2);
                V3 dR2dBt = -0.125 / (N - 1) * (v0 - v2);

                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx), -1.0);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 2), 1.0);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx), -h6);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx + 1), -4.0 * h6);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx + 2), -h6);
                alglib.sparseappendelement(j, thisPhase.BtIdx(), dR1dBt.x);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx), -0.5);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 1), 1.0);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 2), -0.5);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx), -h8);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx + 2), h8);
                alglib.sparseappendelement(j, thisPhase.BtIdx(), dR2dBt.x);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx), -1.0);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 2), 1.0);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx), -h6);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx + 1), -4.0 * h6);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx + 2), -h6);
                alglib.sparseappendelement(j, thisPhase.BtIdx(), dR1dBt.y);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx), -0.5);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 1), 1.0);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 2), -0.5);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx), -h8);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx + 2), h8);
                alglib.sparseappendelement(j, thisPhase.BtIdx(), dR2dBt.y);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx), -1.0);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 2), 1.0);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx), -h6);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx + 1), -4.0 * h6);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx + 2), -h6);
                alglib.sparseappendelement(j, thisPhase.BtIdx(), dR1dBt.z);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx), -0.5);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 1), 1.0);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 2), -0.5);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx), -h8);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx + 2), h8);
                alglib.sparseappendelement(j, thisPhase.BtIdx(), dR2dBt.z);

                // dv/dt = -r/r3 + T/m*u

                V3 dvdt0 = -r0 / r03 + thrust / m0 * u0;
                V3 dvdt1 = -r1 / r13 + thrust / m1 * u1;
                V3 dvdt2 = -r2 / r23 + thrust / m2 * u2;

                V3 dv1 = v2 - v0 - h6 * (dvdt0 + 4 * dvdt1 + dvdt2);
                V3 dv2 = v1 - 0.5 * (v0 + v2) - h8 * (dvdt0 - dvdt2);

                f[ci++] = dv1.x;
                f[ci++] = dv2.x;
                f[ci++] = dv1.y;
                f[ci++] = dv2.y;
                f[ci++] = dv1.z;
                f[ci++] = dv2.z;

                V3 dv1dbt = -1.0 / 6.0 / (N - 1) * (dvdt0 + 4 * dvdt1 + dvdt2);
                V3 dv2dbt = -0.125 / (N - 1) * (dvdt0 - dvdt2);

                V3 dv1dm0 = h6 * thrust * u0 / (m0 * m0);
                V3 dv1dm1 = 4.0 * h6 * thrust * u1 / (m1 * m1);
                V3 dv1dm2 = h6 * thrust * u2 / (m2 * m2);
                V3 dv2dm0 = h8 * thrust * u0 / (m0 * m0);
                V3 dv2dm2 = -h8 * thrust * u2 / (m2 * m2);

                M3 dvdr0 = 3 * V3.Outer(r0, r0) / r05 - M3.identity / r03;
                M3 dvdr1 = 3 * V3.Outer(r1, r1) / r15 - M3.identity / r13;
                M3 dvdr2 = 3 * V3.Outer(r2, r2) / r25 - M3.identity / r23;

                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx), -h6 * dvdr0[0, 0]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 1), -4.0 * h6 * dvdr1[0, 0]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 2), -h6 * dvdr2[0, 0]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx), -h6 * dvdr0[1, 0]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 1), -4.0 * h6 * dvdr1[1, 0]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 2), -h6 * dvdr2[1, 0]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx), -h6 * dvdr0[2, 0]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 1), -4.0 * h6 * dvdr1[2, 0]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 2), -h6 * dvdr2[2, 0]);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx), -1.0);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx + 2), 1.0);

                if (!_phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv1dm0.x);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 1), dv1dm1.x);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv1dm2.x);
                }

                if (!_phases[p].GuidedCoast)
                {
                    if (_phases[p].Unguided)
                    {
                        alglib.sparseappendelement(j, thisPhase.Ux.Idx(0), -h6 * thrust / m0 - 4.0 * h6 * thrust / m1 - h6 * thrust / m2);
                    }
                    else
                    {
                        alglib.sparseappendelement(j, thisPhase.Ux.Idx(idx), -h6 * thrust / m0);
                        alglib.sparseappendelement(j, thisPhase.Ux.Idx(idx + 1), -4.0 * h6 * thrust / m1);
                        alglib.sparseappendelement(j, thisPhase.Ux.Idx(idx + 2), -h6 * thrust / m2);
                    }
                }

                alglib.sparseappendelement(j, thisPhase.BtIdx(), dv1dbt.x);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx), -h8 * dvdr0[0, 0]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 2), h8 * dvdr2[0, 0]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx), -h8 * dvdr0[1, 0]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 2), h8 * dvdr2[1, 0]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx), -h8 * dvdr0[2, 0]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 2), h8 * dvdr2[2, 0]);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx), -0.5);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx + 1), 1.0);
                alglib.sparseappendelement(j, thisPhase.Vx.Idx(idx + 2), -0.5);

                if (!_phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv2dm0.x);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv2dm2.x);
                }

                if (!_phases[p].GuidedCoast)
                {
                    if (_phases[p].Unguided)
                    {
                        alglib.sparseappendelement(j, thisPhase.Ux.Idx(0), -h8 * thrust / m0 + h8 * thrust / m2);
                    }
                    else
                    {
                        alglib.sparseappendelement(j, thisPhase.Ux.Idx(idx), -h8 * thrust / m0);
                        alglib.sparseappendelement(j, thisPhase.Ux.Idx(idx + 2), h8 * thrust / m2);
                    }
                }

                alglib.sparseappendelement(j, thisPhase.BtIdx(), dv2dbt.x);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx), -h6 * dvdr0[0, 1]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 1), -4.0 * h6 * dvdr1[0, 1]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 2), -h6 * dvdr2[0, 1]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx), -h6 * dvdr0[1, 1]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 1), -4.0 * h6 * dvdr1[1, 1]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 2), -h6 * dvdr2[1, 1]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx), -h6 * dvdr0[2, 1]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 1), -4.0 * h6 * dvdr1[2, 1]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 2), -h6 * dvdr2[2, 1]);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx), -1.0);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx + 2), 1.0);
                if (!_phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv1dm0.y);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 1), dv1dm1.y);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv1dm2.y);
                }

                if (!_phases[p].GuidedCoast)
                {
                    if (_phases[p].Unguided)
                    {
                        alglib.sparseappendelement(j, thisPhase.Uy.Idx(0), -h6 * thrust / m0 - 4.0 * h6 * thrust / m1 - h6 * thrust / m2);
                    }
                    else
                    {
                        alglib.sparseappendelement(j, thisPhase.Uy.Idx(idx), -h6 * thrust / m0);
                        alglib.sparseappendelement(j, thisPhase.Uy.Idx(idx + 1), -4.0 * h6 * thrust / m1);
                        alglib.sparseappendelement(j, thisPhase.Uy.Idx(idx + 2), -h6 * thrust / m2);
                    }
                }

                alglib.sparseappendelement(j, thisPhase.BtIdx(), dv1dbt.y);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx), -h8 * dvdr0[0, 1]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 2), h8 * dvdr2[0, 1]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx), -h8 * dvdr0[1, 1]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 2), h8 * dvdr2[1, 1]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx), -h8 * dvdr0[2, 1]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 2), h8 * dvdr2[2, 1]);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx), -0.5);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx + 1), 1.0);
                alglib.sparseappendelement(j, thisPhase.Vy.Idx(idx + 2), -0.5);
                if (!_phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv2dm0.y);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv2dm2.y);
                }

                if (!_phases[p].GuidedCoast)
                {
                    if (_phases[p].Unguided)
                    {
                        alglib.sparseappendelement(j, thisPhase.Uy.Idx(0), -h8 * thrust / m0 + h8 * thrust / m2);
                    }
                    else
                    {
                        alglib.sparseappendelement(j, thisPhase.Uy.Idx(idx), -h8 * thrust / m0);
                        alglib.sparseappendelement(j, thisPhase.Uy.Idx(idx + 2), h8 * thrust / m2);
                    }
                }

                alglib.sparseappendelement(j, thisPhase.BtIdx(), dv2dbt.y);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx), -h6 * dvdr0[0, 2]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 1), -4.0 * h6 * dvdr1[0, 2]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 2), -h6 * dvdr2[0, 2]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx), -h6 * dvdr0[1, 2]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 1), -4.0 * h6 * dvdr1[1, 2]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 2), -h6 * dvdr2[1, 2]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx), -h6 * dvdr0[2, 2]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 1), -4.0 * h6 * dvdr1[2, 2]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 2), -h6 * dvdr2[2, 2]);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx), -1.0);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx + 2), 1.0);
                if (!_phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv1dm0.z);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 1), dv1dm1.z);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv1dm2.z);
                }

                if (!_phases[p].GuidedCoast)
                {
                    if (_phases[p].Unguided)
                    {
                        alglib.sparseappendelement(j, thisPhase.Uz.Idx(0), -h6 * thrust / m0 - 4.0 * h6 * thrust / m1 - h6 * thrust / m2);
                    }
                    else
                    {
                        alglib.sparseappendelement(j, thisPhase.Uz.Idx(idx), -h6 * thrust / m0);
                        alglib.sparseappendelement(j, thisPhase.Uz.Idx(idx + 1), -4.0 * h6 * thrust / m1);
                        alglib.sparseappendelement(j, thisPhase.Uz.Idx(idx + 2), -h6 * thrust / m2);
                    }
                }

                alglib.sparseappendelement(j, thisPhase.BtIdx(), dv1dbt.z);
                alglib.sparseappendemptyrow(j);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx), -h8 * dvdr0[0, 2]);
                alglib.sparseappendelement(j, thisPhase.Rx.Idx(idx + 2), h8 * dvdr2[0, 2]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx), -h8 * dvdr0[1, 2]);
                alglib.sparseappendelement(j, thisPhase.Ry.Idx(idx + 2), h8 * dvdr2[1, 2]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx), -h8 * dvdr0[2, 2]);
                alglib.sparseappendelement(j, thisPhase.Rz.Idx(idx + 2), h8 * dvdr2[2, 2]);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx), -0.5);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx + 1), 1.0);
                alglib.sparseappendelement(j, thisPhase.Vz.Idx(idx + 2), -0.5);
                if (!_phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv2dm0.z);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv2dm2.z);
                }

                if (!_phases[p].GuidedCoast)
                {
                    if (_phases[p].Unguided)
                    {
                        alglib.sparseappendelement(j, thisPhase.Uz.Idx(0), -h8 * thrust / m0 + h8 * thrust / m2);
                    }
                    else
                    {
                        alglib.sparseappendelement(j, thisPhase.Uz.Idx(idx), -h8 * thrust / m0);
                        alglib.sparseappendelement(j, thisPhase.Uz.Idx(idx + 2), h8 * thrust / m2);
                    }
                }

                alglib.sparseappendelement(j, thisPhase.BtIdx(), dv2dbt.z);

                // dm/dt = -mdot (as algebraic constraints rather than defect constraints)

                if (!_phases[p].Coast)
                {
                    f[ci++] = m1 - _phases[p].m0 + (n + 0.5) * h * mdot;
                    f[ci++] = m2 - _phases[p].m0 + (n + 1.0) * h * mdot;
                    alglib.sparseappendemptyrow(j);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 1), 1.0);
                    alglib.sparseappendelement(j, thisPhase.BtIdx(), (n + 0.5) / (N - 1.0) * mdot);
                    alglib.sparseappendemptyrow(j);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), 1.0);
                    alglib.sparseappendelement(j, thisPhase.BtIdx(), (n + 1.0) / (N - 1.0) * mdot);
                }
            }

            return ci;
        }

        private double ObjectiveFunction(alglib.sparsematrix j)
        {
            PhaseProxy lastPhase = _vars[-1];
            double     val       = 0;

            // cost metric
            switch (_cost)
            {
                case Cost.MIN_TIME:
                    alglib.sparseappendemptyrow(j);
                    for (int p = 0; p < _phases.Count; p++)
                    {
                        if (_phases[p].Coast || !_phases[p].AllowShutdown)
                            continue;

                        PhaseProxy thisPhase = _vars[p];

                        val += thisPhase.Bt();

                        alglib.sparseappendelement(j, thisPhase.BtIdx(), 1.0);
                    }

                    break;
                case Cost.MAX_MASS:
                    alglib.sparseappendemptyrow(j);
                    for (int p = 0; p < _phases.Count; p++)
                    {
                        if (_phases[p].Coast || !_phases[p].AllowShutdown)
                            continue;
                        PhaseProxy thisPhase = _vars[p];
                        val += -thisPhase.M[-1];
                        alglib.sparseappendelement(j, thisPhase.M.Idx(-1), -1.0);
                    }

                    break;
                case Cost.MAX_ENERGY:
                    V3     vf   = lastPhase.V[-1];
                    V3     rf   = lastPhase.R[-1];
                    double rfm  = rf.magnitude;
                    double rfm3 = rfm * rfm * rfm;

                    val = -(0.5 * vf.sqrMagnitude - 1.0 / rfm);
                    alglib.sparseappendemptyrow(j);
                    alglib.sparseappendelement(j, lastPhase.Rx.Idx(-1), -1.0 / rfm3 * rf.x);
                    alglib.sparseappendelement(j, lastPhase.Ry.Idx(-1), -1.0 / rfm3 * rf.y);
                    alglib.sparseappendelement(j, lastPhase.Rz.Idx(-1), -1.0 / rfm3 * rf.z);
                    alglib.sparseappendelement(j, lastPhase.Vx.Idx(-1), -vf.x);
                    alglib.sparseappendelement(j, lastPhase.Vy.Idx(-1), -vf.y);
                    alglib.sparseappendelement(j, lastPhase.Vz.Idx(-1), -vf.z);

                    break;
                case Cost.MIN_THRUST_ACCEL:
                    alglib.sparseappendemptyrow(j);
                    for (int p = 0; p < _phases.Count; p++)
                    {
                        if (_phases[p].Coast || !_phases[p].AllowShutdown)
                            continue;

                        PhaseProxy thisPhase = _vars[p];

                        double thrust = _phases[p].Thrust;
                        double den    = (N - 1) * 6;
                        double h6     = thisPhase.Bt() / den;

                        double sum = 0;
                        for (int k = 0; k < _k; k += 1)
                        {
                            double mk = thisPhase.M[k];

                            if (k == 0 || k == _k - 1)
                            {
                                val += thrust * h6 / mk;
                                alglib.sparseappendelement(j, thisPhase.M.Idx(k), -thrust * h6 / (mk * mk));
                                sum += thrust / mk / den;
                                continue;
                            }

                            if (k % 2 == 0)
                            {
                                val += thrust * h6 * 2.0 / mk;
                                alglib.sparseappendelement(j, thisPhase.M.Idx(k), -2.0 * thrust * h6 / (mk * mk));
                                sum += 2.0 * thrust / mk / den;
                            }
                            else
                            {
                                val += thrust * h6 * 4.0 / mk;
                                alglib.sparseappendelement(j, thisPhase.M.Idx(k), -4.0 * thrust * h6 / (mk * mk));
                                sum += 4.0 * thrust / mk / den;
                            }
                        }

                        alglib.sparseappendelement(j, thisPhase.BtIdx(), sum);
                    }

                    break;
                default:
                    throw new Exception("code should not be reachable");
            }

            return val;
        }

        private (double t0, double oldt0) TranscribePhasesFromOldSolution(int phaseStart, int phaseLimit, double t0, Solution oldSolution, double oldt0, double oldtf)
        {
            double tbt    = 0;
            double oldtbt = oldtf - oldt0;
            double frac   = 1.0;

            if (!_phases[phaseStart].Coast)
            {
                for (int p = phaseStart; p < phaseLimit; p++)
                    tbt += _phases[p].bt;

                frac = oldtbt > tbt ? oldtbt / tbt : 1.0;
            }

            for (int p = phaseStart; p < phaseLimit; p++)
            {
                Phase      phase     = _phases[p];
                PhaseProxy thisPhase = _vars[p];

                oldtbt = oldtf - oldt0;
                double bt    = _phases[p].Coast ? oldtbt : Clamp(phase.bt, 0, oldtbt / frac);
                double oldbt = bt * frac;
                oldbt = Min(oldbt, oldtf - oldt0);
                double oldh = oldbt / (_k - 1);

                double tf = t0 + bt;
                double h  = bt / (_k - 1);

                double m0   = phase.m0;
                double mdot = -phase.Mdot;

                for (int k = 0; k < _k; k++)
                {
                    double dt    = k * h;
                    double olddt = k * oldh;

                    V3 r = oldSolution.RBar(oldt0 + olddt);
                    thisPhase.R[k] = r;

                    V3 v = oldSolution.VBar(oldt0 + olddt);
                    thisPhase.V[k] = v;

                    if (!phase.GuidedCoast)
                    {
                        V3 u = oldSolution.UBar(oldt0 + olddt);
                        if (u.sqrMagnitude < 0.01)
                            u = v.normalized;

                        if (phase.Unguided)
                            thisPhase.U[0] += u;
                        else
                            thisPhase.U[k] = u;
                    }

                    if (!phase.Coast)
                        thisPhase.M[k] = m0 + mdot * dt;
                }

                if (phase.Coast)
                    thisPhase.M[0] = m0;

                if (phase.Unguided)
                    thisPhase.U[0] = thisPhase.U[0].normalized;

                thisPhase.Bt() = tf - t0;

                t0    =  tf;
                oldt0 += oldbt;
            }

            return (t0, oldt0);
        }

        // this makes no assumptions about the phases in the old solution, just that
        // we must be doing burn / coast-burn or / burn-coast-burn, where the burns
        // may be multi-stage.
        public void TranscribePreviousSolution(Solution oldSolution)
        {
            _xGuess = new double[_vars.TotalVariables];
            _vars.WrapVars(_xGuess);

            int coastPhaseIndex = -1;

            for (int p = 0; p < _phases.Count; p++)
                if (_phases[p].Coast)
                    coastPhaseIndex = p;

            double t0    = 0;
            double oldt0 = (_problem.T0 - oldSolution.T0) / _problem.Scale.TimeScale;

            (double oldburn1, double oldcoast, double oldburn2) = oldSolution.TgoBarSplit(oldt0);

            if (coastPhaseIndex > 0)
            {
                // transcribe the first burn phase(s)
                // FIXME: if there's no coast in the oldSolution, guess a coast?
                double oldtf1 = oldt0 + oldburn1;
                (t0, oldt0) = TranscribePhasesFromOldSolution(0, coastPhaseIndex, t0, oldSolution, oldt0, oldtf1);
            }
            else
            {
                // there's no new first burn phase, so skip any first burn phase in the old solution.
                oldt0 += oldburn1;
            }

            if (coastPhaseIndex >= 0)
            {
                // transcribe the coast phase
                double oldtf2 = oldt0 + oldcoast;
                (t0, oldt0) = TranscribePhasesFromOldSolution(coastPhaseIndex, coastPhaseIndex + 1, t0, oldSolution, oldt0, oldtf2);
            }
            else
            {
                // there's no new coast phase, so skip any coast phase in the old solution.
                oldt0 += oldcoast;
            }

            // transcribe the last burn phase(s)
            double oldtf3 = oldt0 + oldburn2;
            TranscribePhasesFromOldSolution(coastPhaseIndex + 1, _phases.Count, t0, oldSolution, oldt0, oldtf3);
        }

        // the phases in the old solution must match the phases in this optimizer.
        // variables on the phases may change, but the number and order of phases must not.
        public void TranscribePreviousBootSolution(Solution oldSolution)
        {
            _xGuess = new double[_vars.TotalVariables];
            _vars.WrapVars(_xGuess);

            double t0    = 0;
            double oldt0 = 0;
            for (int p = 0; p < _phases.Count; p++)
            {
                Phase      phase     = _phases[p];
                PhaseProxy thisPhase = _vars[p];

                double oldbt = oldSolution.BtBar(p, 0);
                double oldtf = oldt0 + oldbt;
                double oldh  = (oldtf - oldt0) / (_k - 1);

                // a previous infinite stage can exceed the burn time, so start by clamping it back
                // down, but we want to end at the same location, so we index into the old solution
                // by steps from the old burn time.
                double bt = oldbt;
                if (!phase.Coast)
                    bt = Min(oldbt, phase.bt);
                double tf = t0 + bt;
                double h  = (tf - t0) / (_k - 1);

                double m0   = phase.m0;
                double mdot = -phase.Mdot;

                for (int k = 0; k < _k; k++)
                {
                    double dt    = k * h;
                    double olddt = k * oldh;

                    V3 r = oldSolution.RBar(t0 + olddt);
                    thisPhase.R[k] = r;

                    V3 v = oldSolution.VBar(t0 + olddt);
                    thisPhase.V[k] = v;

                    if (!phase.GuidedCoast)
                    {
                        V3 u = oldSolution.UBar(t0 + olddt);
                        if (u.sqrMagnitude < 0.01)
                            u = v.normalized;

                        if (phase.Unguided)
                            thisPhase.U[0] += u;
                        else
                            thisPhase.U[k] = u;
                    }

                    if (!phase.Coast)
                        thisPhase.M[k] = m0 + mdot * dt;
                }

                if (phase.Coast)
                    thisPhase.M[0] = m0;

                if (phase.Unguided)
                    thisPhase.U[0] = thisPhase.U[0].normalized;

                thisPhase.Bt() = tf - t0;

                t0    = tf;
                oldt0 = oldtf;
            }
        }

        private double[] _xGuess;
        private double[] nu;
        private double[] nl;

        private Solution UnSafeRun()
        {
            double[] x    = new double[_vars.TotalVariables];
            double[] bndl = new double[_vars.TotalVariables];
            double[] bndu = new double[_vars.TotalVariables];
            bool[] boxConstrained = new  bool[_vars.TotalVariables];
            nl = new double[_vars.TotalConstraints];
            nu = new double[_vars.TotalConstraints];
            double[] f = new double[_vars.TotalConstraints + 1];

            alglib.sparsecreatecrsempty(_vars.TotalVariables, out alglib.sparsematrix j2);
            ConstraintFunction(_xGuess, f, j2, null);

            CalculatePrimalFeasibility(f);

            DebugPrint($"Initial Cost: {Objective}");
            DebugPrint($"Initial PrimalFeasibility: {PrimalFeasibility}");

            for (int i = 0; i < bndu.Length; i++)
            {
                bndu[i] = double.PositiveInfinity;
                bndl[i] = double.NegativeInfinity;
            }

            // box constraints on initial conditions

            PhaseProxy firstPhase = _vars[0];
            (int r0X, int r0Y, int r0Z) = firstPhase.R.Idx(0);
            (int v0X, int v0Y, int v0Z) = firstPhase.V.Idx(0);

            bndu[r0X] = bndl[r0X] = _problem.R0.x;
            bndu[r0Y] = bndl[r0Y] = _problem.R0.y;
            bndu[r0Z] = bndl[r0Z] = _problem.R0.z;
            bndu[v0X] = bndl[v0X] = _problem.V0.x;
            bndu[v0Y] = bndl[v0Y] = _problem.V0.y;
            bndu[v0Z] = bndl[v0Z] = _problem.V0.z;

            boxConstrained[r0X] = true;
            boxConstrained[r0Y] = true;
            boxConstrained[r0Z] = true;
            boxConstrained[v0X] = true;
            boxConstrained[v0Y] = true;
            boxConstrained[v0Z] = true;

            // box constraints on burntime
            for (int p = 0; p < _phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];
                int        idx       = thisPhase.BtIdx();
                if (_phases[p].Coast)
                {
                    bndl[idx] = _phases[p].mint;
                    bndu[idx] = _phases[p].maxt;
                }
                else
                {
                    bndl[idx] = _phases[p].AllowShutdown ? 0 : _phases[p].bt;
                    if (_phases[p].AllowInfiniteBurntime)
                        bndu[idx] = _phases[p].Infinite ? double.PositiveInfinity : 0.999 * _phases[p].tau;
                    else
                        bndu[idx] = _phases[p].bt;
                }
                if (bndu[idx] <= bndl[idx])
                    boxConstrained[idx] = true;
            }

            // FIXME: set box path boundaries on mass
            // FIXME: set box path boundaries on control
            for (int p = 0; p < _phases.Count; p++)
            {
                // box constraints on initial stage masses
                PhaseProxy thisPhase = _vars[p];

                int idx             = thisPhase.M.Idx(0);
                bndu[idx]           = bndl[idx] = _phases[p].m0;
                boxConstrained[idx] = true;
            }

            for (int i = 0; i < nl.Length; i++)
                nu[i] = nl[i] = 0;

            alglib.minnlccreate(_vars.TotalVariables, _xGuess, out _state);
            alglib.minnlcsetbc(_state, bndl, bndu);
            alglib.minnlcsetnlc2(_state, nl, nu);
            //alglib.minnlcsetstpmax(_state, Stpmax);
            alglib.minnlcsetalgosqp(_state);
            alglib.minnlcsetcond3(_state, Epsf, SQPTrustRegionLimit, Maxits);

            //alglib.minnlcoptguardgradient(_state, Diffstep);
            //alglib.minnlcoptguardsmoothness(_state, 1);
            //alglib.trace_file("SQP,PREC.F6", "/tmp/trace.log");

            alglib.minnlcoptimize(_state, _constraintHandle, null, null);
            alglib.minnlcresultsbuf(_state, ref x, _rep);

            TerminationType = _rep.terminationtype;
            Iterations      = _rep.iterationscount;

            DebugPrint("terminationtype: " + TerminationType);
            DebugPrint("iterations: " + Iterations);

            alglib.minnlcoptguardresults(_state, out alglib.optguardreport ogrep);

            if (ogrep.badgradsuspected)
                if (!DoubleMatrixSparsityValidation(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 1e-4))
                    throw new Exception(
                        $"badgradsuspected: constraint: {ogrep.badgradfidx} variable: {ogrep.badgradvidx} {ogrep.badgraduser[ogrep.badgradfidx, ogrep.badgradvidx]:e} != {ogrep.badgradnum[ogrep.badgradfidx, ogrep.badgradvidx]:e}\nuser:\n{DoubleMatrixString(ogrep.badgraduser)}\nnumerical:\n{DoubleMatrixString(ogrep.badgradnum)}\nsparsity check:\n{DoubleMatrixSparsityCheck(ogrep.badgraduser, ogrep.badgradnum, boxConstrained, 1e-2)}");

            if (ogrep.nonc0suspected)
                throw new Exception("nonc0suspected");

            if (ogrep.nonc1suspected)
                throw new Exception("nonc1suspected");

            alglib.sparsecreatecrsempty(_vars.TotalVariables, out alglib.sparsematrix j);

            if (_rep.terminationtype != 8)
                ConstraintFunction(x, f, j, null);

            CalculatePrimalFeasibility(f);

            DebugPrint($"Cost: {Objective}");
            DebugPrint($"PrimalFeasibility: {PrimalFeasibility}");

            Status = PrimalFeasibility > 1e-4 ? OptimStatus.FAILED : OptimStatus.SUCCESS;

            return GenerateSolution(x);
        }

        public Solution? Run()
        {
            foreach (Phase phase in _phases)
                DebugPrint(phase.ToString());

            try
            {
                var tokenSource = new CancellationTokenSource();
                tokenSource.CancelAfter(OptimizerTimeout);
                _timeoutToken = tokenSource.Token;
                Solution      = UnSafeRun();
                return Solution;
            }
            catch (OperationCanceledException)
            {
                Status = OptimStatus.CANCELLED;
            }

            return null;
        }

        private Solution GenerateSolution(double[] x)
        {
            _vars.WrapVars(x);
            var solution = new Solution(_problem);

            var temp  = new InterpolantLayout();                           // FIXME: rename
            var temp2 = Vn.Rent(InterpolantLayout.INTERPOLANT_LAYOUT_LEN); // FIXME: rename

            double dv = 0;
            double ti = 0;
            double m0 = _problem.M0;

            int  optimizedShutdownIndex = -1;
            int  terminalStageIndex     = -1;
            bool pruningStages          = false;

            for (int p = 0; p < _phases.Count; p++)
            {
                Phase      phase       = _phases[p];
                PhaseProxy thisPhase   = _vars[p];
                var        interpolant = Hn.Get(InterpolantLayout.INTERPOLANT_LAYOUT_LEN);

                double bt = thisPhase.Bt();
                double h  = bt / (N - 1);

                for (int n = 0; n < N; n++)
                {
                    temp.R = thisPhase.R[2 * n];
                    temp.V = thisPhase.V[2 * n];
                    temp.M = _phases[p].Coast ? thisPhase.M[0] : thisPhase.M[2 * n];

                    if (phase.GuidedCoast) // guided coast
                    {
                        PhaseProxy prevPhase = _vars[p - 1];
                        PhaseProxy nextPhase = _vars[p + 1];

                        V3 u0 = p == 0 ? _problem.U0 : prevPhase.U[-1];
                        V3 uf = nextPhase.U[0];

                        temp.U = V3.Slerp(u0, uf, (double)n / (N - 1));
                    }
                    else
                    {
                        temp.U = phase.Unguided ? thisPhase.U[0] : thisPhase.U[2 * n];
                    }

                    double t = ti + n * h;
                    temp.Dv = dv + _phases[p].DeltaVForTime(t - ti);

                    temp.CopyTo(temp2);
                    interpolant.Add(t, temp2);

                    m0 = temp.M;
                }

                // FIXME: need a SolutionBuilder that all this post-processing junk can be moved to.

                // XXX: this is even jankier.
                bool freeBurntimeLeft = _phases[p].bt - bt > 1e-3;  // is there unburned propellant going to be left in this stage?
                bool prunableStage    = pruningStages && bt < 1e-3; // is this is a prunable stage (negligible propellant use after we can prune)

                if (_phases[p].AllowShutdown && !prunableStage)
                    optimizedShutdownIndex = p;

                _phases[p].PreciseShutdown = false;

                if (!_phases[p].AllowShutdown || !prunableStage)
                    terminalStageIndex = p;

                _phases[p].TerminalStage = false;

                // hit a stage with some free propellant left
                if (_phases[p].AllowShutdown && freeBurntimeLeft)
                    pruningStages = true;

                double tf = ti + bt;
                solution.AddSegment(ti, tf, interpolant, _phases[p]);
                ti = tf;
                dv = temp.Dv;
            }

            // XXX: continuation of jank.
            if (optimizedShutdownIndex >= 0)
                solution.Phases[optimizedShutdownIndex].PreciseShutdown = true;

            if (terminalStageIndex >= 0)
                solution.Phases[terminalStageIndex].TerminalStage = true;

            return solution;
        }

        public bool Success() => Status == OptimStatus.SUCCESS;
    }
}
