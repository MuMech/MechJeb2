/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using static System.Math;
using static MechJebLib.Utils.AutoDiff;

namespace MechJebLib.PSG
{
    public class AscentProblem
    {
        private readonly Optimizer _optimizer;
        private readonly VariableProxy _vars;
        public readonly Dictionary<int, string> ConstraintNames = new Dictionary<int, string>();
        private bool _firstPass;

        public AscentProblem(Optimizer optimizer)
        {
            _optimizer = optimizer;
            _vars = new VariableProxy(_optimizer.Problem, _optimizer.Phases, _optimizer.Terminal, _optimizer.N);
        }

        public readonly struct ConstraintArgs
        {
            public readonly bool FirstPass;

            public ConstraintArgs(bool firstPass)
            {
                FirstPass = firstPass;
            }
        }

        private readonly ConstraintArgs _defaultArgs = new ConstraintArgs(false);

        public void ConstraintFunction(double[] f, alglib.sparsematrix j, double[] x, object? o)
        {
            ConstraintArgs args = (ConstraintArgs?)o ?? _defaultArgs;

            _firstPass = args.FirstPass;

            _optimizer.TimeoutToken.ThrowIfCancellationRequested();
            _vars.WrapVars(x);

            int ci = 0;

            ci = ObjectiveFunction(f, j, ci);

            ci = DynamicPressureConstraints(f, j, ci);

            ci = ControlNormConstraints(f, j, ci);

            for (int p = 0; p < _optimizer.Phases.Count; p++)
            {
                ci = DynamicConstraints(f, j, ci, p);
                ci = StagingConstraints(f, j, ci, p);
                ci = ContinuityConstraints(f, j, ci, p);
            }

            PhaseProxy lastPhase = _vars[-1];

            int start = ci;

            _optimizer.Terminal.Constraints(x, lastPhase.R.Idx(-1), lastPhase.V.Idx(-1), f, j, ref ci);

            if (_firstPass)
                for (int i = start; i < ci; i++)
                    ConstraintNames[i] = $"Terminal Constraint number {i - start + 1}";


            if (ci != _vars.TotalConstraints + 1)
                throw new Exception("Constraint num mismatch");

            if (_firstPass)
                for (int i = 0; i < _vars.TotalConstraints + 1; i++)
                    if (!ConstraintNames.ContainsKey(i))
                        throw new Exception($"Missing constraint in dictionary: {i}");
        }

        private int ControlNormConstraints(double[] f, alglib.sparsematrix j, int ci)
        {
            for (int p = 0; p < _optimizer.Phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];

                if (_optimizer.Phases[p].GuidedCoast)
                    continue;

                for (int k = 0; k < _optimizer.K; k++)
                {
                    if (_firstPass) ConstraintNames[ci] = $"Control norm constraint for phase {p} knot {k}";

                    ci = ApplyScalarConstraintV3(f, j, ci, x => x[0].magnitude, new[] { thisPhase.U[k] }, new[] { thisPhase.U.Idx(k) });
                    if (_optimizer.Phases[p].Unguided)
                        break;
                }
            }

            return ci;
        }

        private int ContinuityConstraints(double[] f, alglib.sparsematrix j, int ci, int p)
        {
            if (p == 0)
                return ci;

            if (_firstPass)
            {
                ConstraintNames[ci] = $"Continuity constraint for phase {p} and phase {p - 1}: Rx";
                ConstraintNames[ci + 1] = $"Continuity constraint for phase {p} and phase {p - 1}: Ry";
                ConstraintNames[ci + 2] = $"Continuity constraint for phase {p} and phase {p - 1}: Rz";
                ConstraintNames[ci + 3] = $"Continuity constraint for phase {p} and phase {p - 1}: Vx";
                ConstraintNames[ci + 4] = $"Continuity constraint for phase {p} and phase {p - 1}: Vy";
                ConstraintNames[ci + 5] = $"Continuity constraint for phase {p} and phase {p - 1}: Vz";
                ConstraintNames[ci + 6] = $"Continuity constraint for phase {p} and phase {p - 1}: Ux";
                ConstraintNames[ci + 7] = $"Continuity constraint for phase {p} and phase {p - 1}: Uy";
                ConstraintNames[ci + 8] = $"Continuity constraint for phase {p} and phase {p - 1}: Uz";
            }

            PhaseProxy thisPhase = _vars[p];
            PhaseProxy prevPhase = _vars[p - 1];

            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.R[-1], thisPhase.R[0] }, new[] { prevPhase.R.Idx(-1), thisPhase.R.Idx(0) });
            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.V[-1], thisPhase.V[0] }, new[] { prevPhase.V.Idx(-1), thisPhase.V.Idx(0) });
            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.U[-1], thisPhase.U[0] }, new[] { prevPhase.U.Idx(-1), thisPhase.U.Idx(0) });

            // mass continuity for coast-within-phase
            if (p <= 0 || !_optimizer.Phases[p].MassContinuity) return ci;

            if (_firstPass) ConstraintNames[ci] = $"Continuity constraint for phase {p} and phase {p - 1}: M";

            ci = ApplyScalarConstraint(f, j, ci, Diff, new[] { prevPhase.M[-1], thisPhase.M[0] }, new[] { prevPhase.M.Idx(-1), thisPhase.M.Idx(0) });

            return ci;

            DualV3 VecDiff(DualV3[] x) => x[0] - x[1];

            Dual Diff(Dual[] x) => x[0] - x[1];
        }

        private int NextAdjustableBurn(int p)
        {
            for (int p2 = p + 1; p2 < _optimizer.Phases.Count; p2++)
            {
                if (_optimizer.Phases[p2].Coast || !_optimizer.Phases[p2].AllowShutdown)
                    continue;

                return p2;
            }

            return -1;
        }

        private int StagingConstraints(double[] f, alglib.sparsematrix j, int ci, int p)
        {
            if (_firstPass) ConstraintNames[ci] = $"Staging constraint for phase {p}";

            if (_optimizer.Phases[p].Coast || !_optimizer.Phases[p].AllowShutdown || _optimizer.Phases[p].MassContinuity)
            {
                f[ci++] = 0;
                alglib.sparseappendemptyrow(j);
                return ci;
            }

            int nextBurnPhaseIndex = NextAdjustableBurn(p);

            if (nextBurnPhaseIndex < 0)
            {
                f[ci++] = 0;
                alglib.sparseappendemptyrow(j);
                return ci;
            }

            int nextNextBurnPhaseIndex = NextAdjustableBurn(nextBurnPhaseIndex);

            if (nextNextBurnPhaseIndex < 0 && _optimizer.Phases[nextBurnPhaseIndex].MassContinuity)
            {
                f[ci++] = 0;
                alglib.sparseappendemptyrow(j);
                return ci;
            }

            bool combiningThisBurn = nextNextBurnPhaseIndex > 0 && _optimizer.Phases[nextBurnPhaseIndex].MassContinuity;
            bool combiningNextBurn = nextNextBurnPhaseIndex > 0 && !combiningThisBurn && _optimizer.Phases[nextNextBurnPhaseIndex].MassContinuity;

            // The complementarity is between the burn ending at preIdx and the burn starting
            // at postIdx. When burns are combined across a mass-continuity coast, we step to
            // the far side of the combined burn for the relevant endpoint.
            int preIdx = combiningThisBurn ? nextBurnPhaseIndex : p;
            int postIdx = combiningThisBurn || combiningNextBurn ? nextNextBurnPhaseIndex : nextBurnPhaseIndex;

            PhaseProxy prePhase = _vars[preIdx];
            PhaseProxy postPhase = _vars[postIdx];

            double preMf = _optimizer.Phases[preIdx].Mf;
            double postM0 = _optimizer.Phases[postIdx].M0;

            // fractional propellant remaining at the end of the burn, ending here
            double b = prePhase.M[-1] / preMf - 1.0;
            // fractional propellant consumed by the burn starting next
            double a = 1.0 - postPhase.M[-1] / postM0;
            double u = a * a + b * b + 2e-6;

            // smoothed Fischer-Burmeister constraint on burned/unburned mass
            f[ci++] = Sqrt(u) - (a + b);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, prePhase.M.Idx(-1), (b / Sqrt(u) - 1) / preMf);
            alglib.sparseappendelement(j, postPhase.M.Idx(-1), (1 - a / Sqrt(u)) / postM0);

            return ci;
        }

        private int DynamicPressureConstraints(double[] f, alglib.sparsematrix j, int ci)
        {
            double rho0InvQAlphaMax = _optimizer.Problem.Rho0InvQAlphaMax;
            double rho0InvQMax = _optimizer.Problem.Rho0InvQMax;
            double rBody = _optimizer.Problem.RBody;
            double h0 = _optimizer.Problem.H0;
            V3 w = _optimizer.Problem.W;

            if (h0 <= 0)
                return ci;


            if (rho0InvQAlphaMax > 0)
            {
                for (int p = 0; p < _optimizer.Phases.Count; p++)
                {
                    PhaseProxy thisPhase = _vars[p];

                    for (int k = 0; k < _optimizer.K; k++)
                    {
                        if (_firstPass) ConstraintNames[ci] = $"QAlpha constraint for phase {p} knot {k}";

                        if (_optimizer.Phases[p].Unguided)
                            ci = ApplyScalarConstraintV3(f, j, ci, QAlphaConstraint, new[] { thisPhase.R[k], thisPhase.V[k], thisPhase.U[0] }, new[] { thisPhase.R.Idx(k), thisPhase.V.Idx(k), thisPhase.U.Idx(0) });
                        else
                            ci = ApplyScalarConstraintV3(f, j, ci, QAlphaConstraint, new[] { thisPhase.R[k], thisPhase.V[k], thisPhase.U[k] }, new[] { thisPhase.R.Idx(k), thisPhase.V.Idx(k), thisPhase.U.Idx(k) });
                    }
                }
            }

            if (rho0InvQMax > 0)
            {
                for (int p = 0; p < _optimizer.Phases.Count; p++)
                {
                    PhaseProxy thisPhase = _vars[p];

                    for (int k = 0; k < _optimizer.K; k++)
                    {
                        if (_firstPass) ConstraintNames[ci] = $"MaxQ constraint for phase {p} knot {k}";

                        ci = ApplyScalarConstraintV3(f, j, ci, QConstraint, new[] { thisPhase.R[k], thisPhase.V[k] }, new[] { thisPhase.R.Idx(k), thisPhase.V.Idx(k) });
                    }
                }
            }

            return ci;

            Dual QAlphaConstraint(DualV3[] x)
            {
                DualV3 r = x[0];
                DualV3 v = x[1];
                DualV3 u = x[2];

                Dual rm = r.magnitude;
                DualV3 vr = v - DualV3.Cross(w, r);
                Dual q = 0.5 * rho0InvQAlphaMax * Dual.Exp(-(rm - rBody) / h0) * vr.sqrMagnitude;
                Dual alpha = DualV3.AngleUnit(vr.normalized, u);

                return q * alpha / 100.0;
            }

            Dual QConstraint(DualV3[] x)
            {
                DualV3 r = x[0];
                DualV3 v = x[1];

                Dual rm = r.magnitude;
                DualV3 vr = v - DualV3.Cross(w, r);
                Dual q = 0.5 * rho0InvQMax * Dual.Exp(-(rm - rBody) / h0) * vr.sqrMagnitude;

                return q / 100.0;
            }
        }

        private int DynamicConstraints(double[] f, alglib.sparsematrix j, int ci, int p)
        {
            PhaseProxy thisPhase = _vars[p];

            double mdot = _optimizer.Phases[p].Mdot;
            double vacThrust = _optimizer.Phases[p].VacThrust;
            double vexVacuum = _optimizer.Phases[p].VexVacuum;
            double vexCurrent = _optimizer.Phases[p].VexCurrent;

            double rho0CdAref = _optimizer.Problem.Rho0CdAref;
            double rBody = _optimizer.Problem.RBody;
            double h0 = _optimizer.Problem.H0;
            double r0 = _optimizer.Problem.R0.magnitude;
            V3 w = _optimizer.Problem.W;

            // dynamical constraints per phase
            for (int n = 0; n < _optimizer.N - 1; n += 1)
            {
                if (_firstPass)
                {
                    ConstraintNames[ci] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotX";
                    ConstraintNames[ci + 1] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotX midpoint";
                    ConstraintNames[ci + 2] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotY";
                    ConstraintNames[ci + 3] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotY midpoint";
                    ConstraintNames[ci + 4] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotZ";
                    ConstraintNames[ci + 5] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotZ midpoint";
                    ConstraintNames[ci + 6] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotX";
                    ConstraintNames[ci + 7] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotX midpoint";
                    ConstraintNames[ci + 8] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotY";
                    ConstraintNames[ci + 9] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotY midpoint";
                    ConstraintNames[ci + 10] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotZ";
                    ConstraintNames[ci + 11] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotZ midpoint";
                }

                int idx = 2 * n;

                double m0, m1, m2;
                int m0Idx, m1Idx, m2Idx;

                if (_optimizer.Phases[p].Coast)
                {
                    m0 = m1 = m2 = thisPhase.M[0];
                    m0Idx = m1Idx = m2Idx = thisPhase.M.Idx(0);
                }
                else
                {
                    m0 = thisPhase.M[idx];
                    m1 = thisPhase.M[idx + 1];
                    m2 = thisPhase.M[idx + 2];
                    m0Idx = thisPhase.M.Idx(idx);
                    m1Idx = thisPhase.M.Idx(idx + 1);
                    m2Idx = thisPhase.M.Idx(idx + 2);
                }

                V3 u0, u1, u2;
                (int, int, int) u0Idx, u1Idx, u2Idx;

                if (_optimizer.Phases[p].Unguided || _optimizer.Phases[p].GuidedCoast)
                {
                    u0 = u1 = u2 = thisPhase.U[0];
                    u0Idx = u1Idx = u2Idx = thisPhase.U.Idx(0);
                }
                else
                {
                    u0 = thisPhase.U[idx];
                    u1 = thisPhase.U[idx + 1];
                    u2 = thisPhase.U[idx + 2];
                    u0Idx = thisPhase.U.Idx(idx);
                    u1Idx = thisPhase.U.Idx(idx + 1);
                    u2Idx = thisPhase.U.Idx(idx + 2);
                }

                var point = new HermiteSimpsonSegment
                {
                    R0 = thisPhase.R[idx],
                    R1 = thisPhase.R[idx + 1],
                    R2 = thisPhase.R[idx + 2],
                    V0 = thisPhase.V[idx],
                    V1 = thisPhase.V[idx + 1],
                    V2 = thisPhase.V[idx + 2],
                    M0 = m0,
                    M1 = m1,
                    M2 = m2,
                    U0 = u0,
                    U1 = u1,
                    U2 = u2,
                    Bt = thisPhase.Bt()
                };

                var indexes = new HermiteSimpsonIndexes
                {
                    R0Idx = thisPhase.R.Idx(idx),
                    R1Idx = thisPhase.R.Idx(idx + 1),
                    R2Idx = thisPhase.R.Idx(idx + 2),
                    V0Idx = thisPhase.V.Idx(idx),
                    V1Idx = thisPhase.V.Idx(idx + 1),
                    V2Idx = thisPhase.V.Idx(idx + 2),
                    M0Idx = m0Idx,
                    M1Idx = m1Idx,
                    M2Idx = m2Idx,
                    U0Idx = u0Idx,
                    U1Idx = u1Idx,
                    U2Idx = u2Idx,
                    BtIdx = thisPhase.BtIdx()
                };


                if (h0 > 0 && rho0CdAref > 0)
                    ci = ApplyHermiteSimpsonDynamics(f, j, ci, VDot, point, indexes, _optimizer.N);
                else
                    ci = ApplyHermiteSimpsonDynamics(f, j, ci, VDotVacuum, point, indexes, _optimizer.N);

                // dm/dt = -mdot (as algebraic constraints rather than defect constraints)

                if (!_optimizer.Phases[p].Coast)
                {
                    if (_firstPass)
                    {
                        ConstraintNames[ci] = $"Dynamical Constraints for phase {p} {n}th constraint: MDot";
                        ConstraintNames[ci + 1] = $"Dynamical Constraints for phase {p} {n}th constraint: MDot midpoint";
                    }

                    ci = ApplyMDotDynamics(f, j, ci, mdot, point, indexes, _optimizer.N);
                }
            }

            return ci;

            DualV3 VDotVacuum(ref HermiteSimpsonDualPoint d)
            {
                Dual r3 = d.R.sqrMagnitude * d.R.magnitude;
                return -d.R / r3 + vacThrust / d.M * d.U;
            }

            DualV3 VDot(ref HermiteSimpsonDualPoint d)
            {
                Dual r = d.R.magnitude;
                Dual r3 = d.R.sqrMagnitude * r;
                DualV3 vr = d.V - DualV3.Cross(w, d.R);
                var normAtmosphere = Dual.Exp(-(r - rBody) / h0);
                var normAtmosphere2 = Dual.Exp(-(r - r0) / h0);
                DualV3 drag = 0.5 * rho0CdAref * normAtmosphere * vr.sqrMagnitude * vr.normalized;
                //T = ṁ [v_e_sl + (v_e_vac - v_e_sl)(1 - p_amb/p₀)]
                Dual thrust = mdot * (vexCurrent + (vexVacuum - vexCurrent) * (1.0 - normAtmosphere2));
                return -d.R / r3 + thrust / d.M * d.U - drag / d.M;
            }
        }

        private int ObjectiveFunction(double[] f, alglib.sparsematrix j, int ci)
        {
            PhaseProxy lastPhase = _vars[-1];

            if (_firstPass) ConstraintNames[ci] = "Objective Function";

            double val = 0;

            // cost metric
            switch (_optimizer.Objective)
            {
                case Optimizer.ObjectiveType.MIN_TIME:
                    alglib.sparseappendemptyrow(j);

                    for (int p = 0; p < _optimizer.Phases.Count; p++)
                    {
                        if (_optimizer.Phases[p].Coast || !_optimizer.Phases[p].AllowShutdown)
                            continue;

                        PhaseProxy thisPhase = _vars[p];

                        val += thisPhase.Bt();

                        alglib.sparseappendelement(j, thisPhase.BtIdx(), 1.0);
                    }

                    f[ci++] = val;

                    break;
                case Optimizer.ObjectiveType.MAX_MASS: // this doesn't work for upper stages with fixed burntimes
                    f[ci++] = -lastPhase.M[-1];
                    alglib.sparseappendemptyrow(j);
                    alglib.sparseappendelement(j, lastPhase.M.Idx(-1), -1.0);

                    break;
                case Optimizer.ObjectiveType.MAX_ENERGY:
                    V3 rf = lastPhase.R[-1];
                    V3 vf = lastPhase.V[-1];
                    (int, int, int) ri = lastPhase.R.Idx(-1);
                    (int, int, int) vi = lastPhase.V.Idx(-1);

                    ci = ApplyScalarConstraintV3(f, j, ci, MaxOrbitalEnergyObjective, new[] { rf, vf }, new[] { ri, vi });

                    break;
                case Optimizer.ObjectiveType.MIN_THRUST_ACCEL:
                    {
                        using var jac = Vec.Rent(_vars.TotalVariables, true);

                        for (int p = 0; p < _optimizer.Phases.Count; p++)
                        {
                            if (_optimizer.Phases[p].Coast || !_optimizer.Phases[p].AllowShutdown)
                                continue;

                            PhaseProxy thisPhase = _vars[p];

                            double thrust = _optimizer.Phases[p].VacThrust;
                            double den = (_optimizer.N - 1) * 6;
                            double h6 = thisPhase.Bt() / den;

                            for (int k = 0; k < _optimizer.K; k += 1)
                            {
                                double mk = thisPhase.M[k];
                                double u = thisPhase.U[k].magnitude;
                                double ux = thisPhase.Ux[k];
                                double uy = thisPhase.Uy[k];
                                double uz = thisPhase.Uz[k];

                                if (k == 0 || k == _optimizer.K - 1)
                                {
                                    val += u * thrust * h6 / mk;
                                    jac[thisPhase.M.Idx(k)] = -u * thrust * h6 / (mk * mk);
                                    jac[thisPhase.Ux.Idx(k)] = ux * thrust * h6 / (u * mk);
                                    jac[thisPhase.Uy.Idx(k)] = uy * thrust * h6 / (u * mk);
                                    jac[thisPhase.Uz.Idx(k)] = uz * thrust * h6 / (u * mk);
                                    jac[thisPhase.BtIdx()] += u * thrust / mk / den;
                                    continue;
                                }

                                if (k % 2 == 0)
                                {
                                    val += u * thrust * h6 * 2.0 / mk;
                                    jac[thisPhase.M.Idx(k)] = -2.0 * u * thrust * h6 / (mk * mk);
                                    jac[thisPhase.Ux.Idx(k)] = 2.0 * ux * thrust * h6 / (u * mk);
                                    jac[thisPhase.Uy.Idx(k)] = 2.0 * uy * thrust * h6 / (u * mk);
                                    jac[thisPhase.Uz.Idx(k)] = 2.0 * uz * thrust * h6 / (u * mk);
                                    jac[thisPhase.BtIdx()] += 2.0 * u * thrust / mk / den;
                                }
                                else
                                {
                                    val += u * thrust * h6 * 4.0 / mk;
                                    jac[thisPhase.M.Idx(k)] = -4.0 * u * thrust * h6 / (mk * mk);
                                    jac[thisPhase.Ux.Idx(k)] = 4.0 * ux * thrust * h6 / (u * mk);
                                    jac[thisPhase.Uy.Idx(k)] = 4.0 * uy * thrust * h6 / (u * mk);
                                    jac[thisPhase.Uz.Idx(k)] = 4.0 * uz * thrust * h6 / (u * mk);
                                    jac[thisPhase.BtIdx()] += 4.0 * u * thrust / mk / den;
                                }
                            }
                        }

                        f[ci++] = val;

                        alglib.sparseappendemptyrow(j);

                        for (int k = 0; k < _vars.TotalVariables; k++)
                            if (jac[k] != 0)
                                alglib.sparseappendelement(j, k, jac[k]);
                    }

                    break;
                default:
                    throw new Exception("code should not be reachable");
            }

            return ci;

            Dual MaxOrbitalEnergyObjective(DualV3[] p) => -(0.5 * DualV3.Dot(p[1], p[1]) - 1.0 / p[0].magnitude);
        }
    }
}
