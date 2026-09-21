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

            ci = ThrustMagnitudeConstraints(f, j, ci);

            ci = ControlNormConstraints(f, j, ci);

            for (int p = 0; p < _optimizer.Phases.Count; p++)
            {
                ci = DynamicConstraints(f, j, ci, p);
                ci = StagingConstraints(f, j, ci, p);
                ci = ContinuityConstraints(f, j, ci, p);
            }

            PhaseProxy lastPhase = _vars[-1];

            int start = ci;

            _optimizer.Terminal.Constraints(x, lastPhase.R.LastIdx, lastPhase.V.LastIdx, f, j, ref ci);

            if (_firstPass)
                for (int i = start; i < ci; i++)
                    ConstraintNames[i] = $"Terminal Constraint number {i - start + 1}";

            if (ci != _vars.TotalConstraints + 1)
                throw new Exception($"Constraint num mismatch {ci} != {_vars.TotalConstraints + 1}");

            if (_firstPass)
                for (int i = 0; i < _vars.TotalConstraints + 1; i++)
                    if (!ConstraintNames.ContainsKey(i))
                        throw new Exception($"Missing constraint in dictionary: {i}");
        }

        private int ThrustMagnitudeConstraints(double[] f, alglib.sparsematrix j, int ci)
        {
            for (int p = 0; p < _optimizer.Phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];

                if (_optimizer.Phases[p].GuidedCoast)
                    continue;

                if (_optimizer.Phases[p].Unguided)
                {
                    int k = 0;

                    if (_firstPass) ConstraintNames[ci] = $"Thrust magnitude inequality constraint for phase {p} grid {k}";

                    ci = ApplyScalarConstraint(f, j, ci, x => x[0], new[] { thisPhase.T[k] }, new[] { thisPhase.T.Idx(k) });

                    continue;
                }

                for (int k = 0; k < _optimizer.K; k++)
                {
                    if (k % 3 == 0)
                        continue;

                    if (_firstPass) ConstraintNames[ci] = $"Thrust magnitude inequality constraint for phase {p} grid {k}";

                    ci = ApplyScalarConstraint(f, j, ci, x => x[0], new[] { thisPhase.T[k] }, new[] { thisPhase.T.Idx(k) });
                }
            }

            return ci;
        }

        private int ControlNormConstraints(double[] f, alglib.sparsematrix j, int ci)
        {
            for (int p = 0; p < _optimizer.Phases.Count; p++)
            {
                PhaseProxy thisPhase = _vars[p];

                if (_optimizer.Phases[p].GuidedCoast)
                    continue;

                if (_optimizer.Phases[p].Unguided)
                {
                    int k = 0;

                    if (_firstPass) ConstraintNames[ci] = $"Control unit quaternion constraint for phase {p} grid {k}";

                    ci = ApplyScalarConstraintQ3(f, j, ci, x => x[0].magnitude - 1, new[] { thisPhase.U[k] }, new[] { thisPhase.U.Idx(k) });

                    continue;
                }

                for (int k = 0; k < _optimizer.K; k++)
                {
                    if (k % 3 == 0)
                        continue;

                    if (_firstPass) ConstraintNames[ci] = $"Control unit quaternion constraint for phase {p} grid {k}";

                    ci = ApplyScalarConstraintQ3(f, j, ci, x => x[0].magnitude - 1, new[] { thisPhase.U[k] }, new[] { thisPhase.U.Idx(k) });
                }
            }

            return ci;
        }

        private int ContinuityConstraints(double[] f, alglib.sparsematrix j, int ci, int p)
        {
            // skip the first phase, nothing to link to
            if (p == 0)
                return ci;

            // state continuity
            if (_firstPass)
            {
                ConstraintNames[ci] = $"Continuity constraint for phase {p} and phase {p - 1}: Rx";
                ConstraintNames[ci + 1] = $"Continuity constraint for phase {p} and phase {p - 1}: Ry";
                ConstraintNames[ci + 2] = $"Continuity constraint for phase {p} and phase {p - 1}: Rz";
                ConstraintNames[ci + 3] = $"Continuity constraint for phase {p} and phase {p - 1}: Vx";
                ConstraintNames[ci + 4] = $"Continuity constraint for phase {p} and phase {p - 1}: Vy";
                ConstraintNames[ci + 5] = $"Continuity constraint for phase {p} and phase {p - 1}: Vz";
            }

            PhaseProxy thisPhase = _vars[p];
            PhaseProxy prevPhase = _vars[p - 1];

            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.R.Last, thisPhase.R.First }, new[] { prevPhase.R.LastIdx, thisPhase.R.FirstIdx });
            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.V.Last, thisPhase.V.First }, new[] { prevPhase.V.LastIdx, thisPhase.V.FirstIdx });

            bool thisUnCollocatedControl = _optimizer.Phases[p].Unguided;
            bool prevUnCollocatedControl = _optimizer.Phases[p - 1].Unguided;
            bool eitherIsCoast = _optimizer.Phases[p].Coast || _optimizer.Phases[p - 1].Coast;

            if ((thisUnCollocatedControl || prevUnCollocatedControl) && !eitherIsCoast)
            {
                if (_firstPass)
                {
                    ConstraintNames[ci] = $"Continuity constraint for phase {p} and phase {p - 1}: Ux";
                    ConstraintNames[ci + 1] = $"Continuity constraint for phase {p} and phase {p - 1}: Uy";
                    ConstraintNames[ci + 2] = $"Continuity constraint for phase {p} and phase {p - 1}: Uz";
                    ConstraintNames[ci + 3] = $"Continuity constraint for phase {p} and phase {p - 1}: Uw";
                }

                ci = ApplyVectorConstraintQ3(f, j, ci, Q3Diff, new[] { prevPhase.U.Last, thisPhase.U.First }, new[] { prevPhase.U.LastIdx, thisPhase.U.FirstIdx });
            }

            // mass continuity for coast-within-phase
            if (p <= 0 || !_optimizer.Phases[p].MassContinuity) return ci;

            if (_firstPass) ConstraintNames[ci] = $"Continuity constraint for phase {p} and phase {p - 1}: M";

            ci = ApplyScalarConstraint(f, j, ci, Diff, new[] { prevPhase.M.Last, thisPhase.M.First }, new[] { prevPhase.M.LastIdx, thisPhase.M.FirstIdx });

            return ci;

            DualQ3 Q3Diff(DualQ3[] x) => x[0] - x[1];

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

            double preMf = _optimizer.Phases[preIdx].MinM;
            double postM0 = _optimizer.Phases[postIdx].M0;

            // fractional propellant remaining at the end of the burn, ending here
            double b = prePhase.M.Last / preMf - 1.0;
            // fractional propellant consumed by the burn starting next
            double a = 1.0 - postPhase.M.Last / postM0;
            double u = a * a + b * b + 2e-6;

            // smoothed Fischer-Burmeister constraint on burned/unburned mass
            f[ci++] = Sqrt(u) - (a + b);
            alglib.sparseappendemptyrow(j);
            alglib.sparseappendelement(j, prePhase.M.LastIdx, (b / Sqrt(u) - 1) / preMf);
            alglib.sparseappendelement(j, postPhase.M.LastIdx, (1 - a / Sqrt(u)) / postM0);

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
                    if (_optimizer.Phases[p].GuidedCoast)
                        continue;

                    PhaseProxy thisPhase = _vars[p];

                    for (int k = 0; k < _optimizer.K; k++)
                    {
                        // this is a control constraint so can only be applied at the collocation points
                        if (k % 3 == 0)
                            continue;

                        if (_firstPass) ConstraintNames[ci] = $"QAlpha constraint for phase {p} grid {k}";

                        if (_optimizer.Phases[p].Unguided)
                            ci = ApplyQAlphaConstraint(f, j, ci, QAlphaConstraint, thisPhase.R[k], thisPhase.V[k], thisPhase.U.First, thisPhase.R.Idx(k), thisPhase.V.Idx(k), thisPhase.U.FirstIdx);
                        else
                            ci = ApplyQAlphaConstraint(f, j, ci, QAlphaConstraint, thisPhase.R[k], thisPhase.V[k], thisPhase.U[k], thisPhase.R.Idx(k), thisPhase.V.Idx(k), thisPhase.U.Idx(k));
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
                        if (_firstPass) ConstraintNames[ci] = $"MaxQ constraint for phase {p} grid {k}";

                        ci = ApplyScalarConstraintV3(f, j, ci, QConstraint, new[] { thisPhase.R[k], thisPhase.V[k] }, new[] { thisPhase.R.Idx(k), thisPhase.V.Idx(k) });
                    }
                }
            }

            return ci;

            Dual QAlphaConstraint(DualV3 r, DualV3 v, DualQ3 u)
            {
                Dual rm = r.magnitude;
                DualV3 vr = v - DualV3.Cross(w, r);
                Dual q = 0.5 * rho0InvQAlphaMax * Dual.Exp(-(rm - rBody) / h0) * vr.sqrMagnitude;
                Dual alpha = DualV3.AngleUnit(vr.normalized, u * V3.forward);

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
            for (int n = 0; n < _optimizer.N; n++)
            {
                if (_firstPass)
                {
                    ConstraintNames[ci] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotX midpoint1";
                    ConstraintNames[ci + 1] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotY midpoint1";
                    ConstraintNames[ci + 2] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotZ midpoint1";
                    ConstraintNames[ci + 3] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotX midpoint2";
                    ConstraintNames[ci + 4] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotY midpoint2";
                    ConstraintNames[ci + 5] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotZ midpoint2";
                    ConstraintNames[ci + 6] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotX endpoint";
                    ConstraintNames[ci + 7] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotY endpoint";
                    ConstraintNames[ci + 8] = $"Dynamical Constraints for phase {p} {n}th constraint: RDotZ endpoint";
                    ConstraintNames[ci + 9] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotX midpoint1";
                    ConstraintNames[ci + 10] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotY midpoint1";
                    ConstraintNames[ci + 11] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotZ midpoint1";
                    ConstraintNames[ci + 12] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotX midpoint2";
                    ConstraintNames[ci + 13] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotY midpoint2";
                    ConstraintNames[ci + 14] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotZ midpoint2";
                    ConstraintNames[ci + 15] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotX endpoint";
                    ConstraintNames[ci + 16] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotY endpoint";
                    ConstraintNames[ci + 17] = $"Dynamical Constraints for phase {p} {n}th constraint: VDotZ endpoint";
                }

                int idx = 3 * n;

                double m0, m1, m2, m3;
                int m0Idx, m1Idx, m2Idx, m3Idx;

                if (_optimizer.Phases[p].Coast)
                {
                    m0 = m1 = m2 = m3 = thisPhase.M.First;
                    m0Idx = m1Idx = m2Idx = m3Idx = thisPhase.M.FirstIdx;
                }
                else
                {
                    m0 = thisPhase.M[idx];
                    m1 = thisPhase.M[idx + 1];
                    m2 = thisPhase.M[idx + 2];
                    m3 = thisPhase.M[idx + 3];
                    m0Idx = thisPhase.M.Idx(idx);
                    m1Idx = thisPhase.M.Idx(idx + 1);
                    m2Idx = thisPhase.M.Idx(idx + 2);
                    m3Idx = thisPhase.M.Idx(idx + 3);
                }

                Q3 u1, u2;
                (int, int, int, int) u1Idx, u2Idx;
                double t1, t2;
                int t1Idx, t2Idx;

                if (_optimizer.Phases[p].Unguided)
                {
                    u1 = u2 = thisPhase.U.First;
                    u1Idx = u2Idx = thisPhase.U.FirstIdx;
                    // TODO: we could support fully collocated thrust for throtlleable solids
                    // TODO: unguided coasts shoudn't have throttle
                    t1 = t2 = thisPhase.T.First;
                    t1Idx = t2Idx = thisPhase.T.FirstIdx;
                }
                else if (_optimizer.Phases[p].GuidedCoast)
                {
                    u1 = u2 = Q3.zero;
                    t1 = t2 = 0;
                    u1Idx = u2Idx = (-1, -1, -1, -1);
                    t1Idx = t2Idx = -1;
                }
                else
                {
                    u1 = thisPhase.U[idx + 1];
                    u2 = thisPhase.U[idx + 2];
                    u1Idx = thisPhase.U.Idx(idx + 1);
                    u2Idx = thisPhase.U.Idx(idx + 2);
                    t1 = thisPhase.T[idx + 1];
                    t2 = thisPhase.T[idx + 2];
                    t1Idx = thisPhase.T.Idx(idx + 1);
                    t2Idx = thisPhase.T.Idx(idx + 2);
                }

                var point = new GaussLegendreSegment
                {
                    R0 = thisPhase.R[idx],
                    R1 = thisPhase.R[idx + 1],
                    R2 = thisPhase.R[idx + 2],
                    R3 = thisPhase.R[idx + 3],
                    V0 = thisPhase.V[idx],
                    V1 = thisPhase.V[idx + 1],
                    V2 = thisPhase.V[idx + 2],
                    V3 = thisPhase.V[idx + 3],
                    M0 = m0,
                    M1 = m1,
                    M2 = m2,
                    M3 = m3,
                    U1 = u1,
                    U2 = u2,
                    T1 = t1,
                    T2 = t2,
                    Bt = thisPhase.Bt()
                };

                var indexes = new GaussLegendreIndexes
                {
                    R0Idx = thisPhase.R.Idx(idx),
                    R1Idx = thisPhase.R.Idx(idx + 1),
                    R2Idx = thisPhase.R.Idx(idx + 2),
                    R3Idx = thisPhase.R.Idx(idx + 3),
                    V0Idx = thisPhase.V.Idx(idx),
                    V1Idx = thisPhase.V.Idx(idx + 1),
                    V2Idx = thisPhase.V.Idx(idx + 2),
                    V3Idx = thisPhase.V.Idx(idx + 3),
                    M0Idx = m0Idx,
                    M1Idx = m1Idx,
                    M2Idx = m2Idx,
                    M3Idx = m3Idx,
                    U1Idx = u1Idx,
                    U2Idx = u2Idx,
                    T1Idx = t1Idx,
                    T2Idx = t2Idx,
                    BtIdx = thisPhase.BtIdx()
                };

                if (h0 > 0 && rho0CdAref > 0)
                    ci = ApplyGaussLegendreDynamics(f, j, ci, VDotAtmo, point, indexes, _optimizer.N);
                else
                    ci = ApplyGaussLegendreDynamics(f, j, ci, VDotVacuum, point, indexes, _optimizer.N);

                if (!_optimizer.Phases[p].Coast)
                {
                    if (_firstPass)
                    {
                        ConstraintNames[ci] = $"Dynamical Constraints for phase {p} {n}th constraint: MDot midpoint1";
                        ConstraintNames[ci + 1] = $"Dynamical Constraints for phase {p} {n}th constraint: MDot midpoint2";
                        ConstraintNames[ci + 2] = $"Dynamical Constraints for phase {p} {n}th constraint: MDot continuity";
                    }

                    ci = ApplyMDotDynamics(f, j, ci, mdot, point, indexes, _optimizer.N);
                }
            }

            return ci;

            DualV3 VDotVacuum(ref GaussLegendreDualPoint d)
            {
                Dual r3 = d.R.sqrMagnitude * d.R.magnitude;
                return -d.R / r3 + vacThrust / d.M * (d.U * V3.forward) * d.T;
            }

            DualV3 VDotAtmo(ref GaussLegendreDualPoint d)
            {
                Dual r = d.R.magnitude;
                Dual r3 = d.R.sqrMagnitude * r;
                DualV3 vr = d.V - DualV3.Cross(w, d.R);
                var normAtmosphere = Dual.Exp(-(r - rBody) / h0);
                var normAtmosphere2 = Dual.Exp(-(r - r0) / h0);
                DualV3 drag = 0.5 * rho0CdAref * normAtmosphere * vr.sqrMagnitude * vr.normalized;
                //T = ṁ [v_e_sl + (v_e_vac - v_e_sl)(1 - p_amb/p₀)]
                Dual thrust = mdot * (vexCurrent + (vexVacuum - vexCurrent) * (1.0 - normAtmosphere2));
                return -d.R / r3 + thrust / d.M * (d.U * V3.forward) * d.T - drag / d.M;
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
                    f[ci++] = -lastPhase.M.Last;
                    alglib.sparseappendemptyrow(j);
                    alglib.sparseappendelement(j, lastPhase.M.LastIdx, -1.0);

                    break;
                case Optimizer.ObjectiveType.MAX_ENERGY:
                    V3 rf = lastPhase.R.Last;
                    V3 vf = lastPhase.V.Last;
                    (int, int, int) ri = lastPhase.R.LastIdx;
                    (int, int, int) vi = lastPhase.V.LastIdx;

                    ci = ApplyScalarConstraintV3(f, j, ci, MaxOrbitalEnergyObjective, new[] { rf, vf }, new[] { ri, vi });

                    break;
                case Optimizer.ObjectiveType.MIN_THRUST_ACCEL: // TODO: fix this
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
                                double ux = thisPhase.UX[k];
                                double uy = thisPhase.UY[k];
                                double uz = thisPhase.UZ[k];

                                if (k == 0 || k == _optimizer.K - 1)
                                {
                                    val += u * thrust * h6 / mk;
                                    jac[thisPhase.M.Idx(k)] = -u * thrust * h6 / (mk * mk);
                                    jac[thisPhase.UX.Idx(k)] = ux * thrust * h6 / (u * mk);
                                    jac[thisPhase.UY.Idx(k)] = uy * thrust * h6 / (u * mk);
                                    jac[thisPhase.UZ.Idx(k)] = uz * thrust * h6 / (u * mk);
                                    jac[thisPhase.BtIdx()] += u * thrust / mk / den;
                                    continue;
                                }

                                if (k % 2 == 0)
                                {
                                    val += u * thrust * h6 * 2.0 / mk;
                                    jac[thisPhase.M.Idx(k)] = -2.0 * u * thrust * h6 / (mk * mk);
                                    jac[thisPhase.UX.Idx(k)] = 2.0 * ux * thrust * h6 / (u * mk);
                                    jac[thisPhase.UY.Idx(k)] = 2.0 * uy * thrust * h6 / (u * mk);
                                    jac[thisPhase.UZ.Idx(k)] = 2.0 * uz * thrust * h6 / (u * mk);
                                    jac[thisPhase.BtIdx()] += 2.0 * u * thrust / mk / den;
                                }
                                else
                                {
                                    val += u * thrust * h6 * 4.0 / mk;
                                    jac[thisPhase.M.Idx(k)] = -4.0 * u * thrust * h6 / (mk * mk);
                                    jac[thisPhase.UX.Idx(k)] = 4.0 * ux * thrust * h6 / (u * mk);
                                    jac[thisPhase.UY.Idx(k)] = 4.0 * uy * thrust * h6 / (u * mk);
                                    jac[thisPhase.UZ.Idx(k)] = 4.0 * uz * thrust * h6 / (u * mk);
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
