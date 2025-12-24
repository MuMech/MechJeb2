using System;
using MechJebLib.Primitives;
using static System.Math;
using static MechJebLib.Utils.AutoDiff;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public class AscentProblem
    {
        private readonly Optimizer     _optimizer;
        private readonly VariableProxy _vars;

        public AscentProblem(Optimizer optimizer)
        {
            _optimizer = optimizer;
            _vars      = new VariableProxy(_optimizer._phases, _optimizer._terminal, _optimizer.N);
        }

        public void ConstraintFunction(double[] f, alglib.sparsematrix j, double[] x, object? o)
        {
            bool debug = o != null;

            _optimizer._timeoutToken.ThrowIfCancellationRequested();
            _vars.WrapVars(x);

            int ci = 0;

            ci = ObjectiveFunction(f, j, ci);

            for (int p = 0; p < _optimizer._phases.Count; p++)
            {
                if (debug) DebugPrint($"start of dynamic constraints: {ci - 1}");
                ci = DynamicConstraints(f, j, ci, p, debug);
                if (debug) DebugPrint($"start of staging constraints: {ci - 1}");
                ci = StagingConstraints(f, j, ci, p);
                if (debug) DebugPrint($"start of control norm constraints: {ci - 1}");
                ci = ControlNormConstraints(f, j, ci, p);
                if (debug) DebugPrint($"start of continuity constraints: {ci - 1}");
                ci = ContinuityConstraints(f, j, ci, p, debug);
            }

            if (debug) DebugPrint($"start of terminal constraints: {ci - 1}");
            PhaseProxy lastPhase = _vars[-1];

            _optimizer._terminal.Constraints(x, lastPhase.R.Idx(-1), lastPhase.V.Idx(-1), f, j, ref ci);

            if (ci != _vars.TotalConstraints + 1)
                throw new Exception("Constraint num mismatch");
        }

        private int ControlNormConstraints(double[] f, alglib.sparsematrix j, int ci, int p)
        {
            PhaseProxy thisPhase = _vars[p];

            if (_optimizer._phases[p].GuidedCoast)
                return ci;

            for (int k = 0; k < _optimizer._k; k++)
            {
                ci = ApplyScalarConstraintV3(f, j, ci, x => x[0].sqrMagnitude - 1.0, new[] { thisPhase.U[k] }, new[] { thisPhase.U.Idx(k) });
                if (_optimizer._phases[p].Unguided)
                    break;
            }

            return ci;
        }

        private int ContinuityConstraints(double[] f, alglib.sparsematrix j, int ci, int p, bool debug)
        {
            if (p == 0)
                return ci;

            PhaseProxy thisPhase = _vars[p];
            PhaseProxy prevPhase = _vars[p - 1];

            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.R[-1], thisPhase.R[0] }, new[] { prevPhase.R.Idx(-1), thisPhase.R.Idx(0) });
            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.V[-1], thisPhase.V[0] }, new[] { prevPhase.V.Idx(-1), thisPhase.V.Idx(0) });
            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.U[-1], thisPhase.U[0] }, new[] { prevPhase.U.Idx(-1), thisPhase.U.Idx(0) });

            // mass continuity for coast-within-phase
            if (p <= 0 || !_optimizer._phases[p].MassContinuity) return ci;

            if (debug) DebugPrint($"mass continuity constraint: {ci - 1}");
            ci = ApplyScalarConstraint(f, j, ci, Diff, new[] { prevPhase.M[-1], thisPhase.M[0] }, new[] { prevPhase.M.Idx(-1), thisPhase.M.Idx(0) });

            return ci;

            DualV3 VecDiff(DualV3[] x)
            {
                return x[0] - x[1];
            }

            Dual Diff(Dual[] x)
            {
                return x[0] - x[1];
            }
        }

        private int NextAdjustableBurn(int p)
        {
            for (int p2 = p + 1; p2 < _optimizer._phases.Count; p2++)
            {
                if (_optimizer._phases[p2].Coast || !_optimizer._phases[p2].AllowShutdown)
                    continue;

                return p2;
            }

            return -1;
        }

        private int StagingConstraints(double[] f, alglib.sparsematrix j, int ci, int p)
        {
            if (_optimizer._phases[p].Coast || !_optimizer._phases[p].AllowShutdown || _optimizer._phases[p].MassContinuity)
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

            if (nextNextBurnPhaseIndex < 0 && _optimizer._phases[nextBurnPhaseIndex].MassContinuity)
            {
                f[ci++] = 0;
                alglib.sparseappendemptyrow(j);
                return ci;
            }

            PhaseProxy thisPhase     = _vars[p];
            PhaseProxy nextBurnPhase = _vars[nextBurnPhaseIndex];

            bool combiningThisBurn = nextNextBurnPhaseIndex > 0 && _optimizer._phases[nextBurnPhaseIndex].MassContinuity;
            bool combiningNextBurn = nextNextBurnPhaseIndex > 0 && !combiningThisBurn && _optimizer._phases[nextNextBurnPhaseIndex].MassContinuity;

            double thisBt;
            double nextBt;

            if (combiningThisBurn)
            {
                thisBt = thisPhase.Bt() + nextBurnPhase.Bt();
                nextBt = _vars[nextNextBurnPhaseIndex].Bt();
            }
            else if (combiningNextBurn)
            {
                thisBt = thisPhase.Bt();
                nextBt = nextBurnPhase.Bt() + _vars[nextNextBurnPhaseIndex].Bt();
            }
            else
            {
                thisBt = thisPhase.Bt();
                nextBt = nextBurnPhase.Bt();
            }

            // next burn time
            double a = nextBt;
            // this burn time remaining
            double b = _optimizer._phases[p].bt - thisBt;
            double u = a * a + b * b + 2e-6;

            // smoothed Fischer-Burmeister constraint on burn times
            f[ci++] = Sqrt(u) - (a + b);
            alglib.sparseappendemptyrow(j);
            if (combiningThisBurn)
            {
                alglib.sparseappendelement(j, thisPhase.BtIdx(), 1 - b / Sqrt(u));
                alglib.sparseappendelement(j, nextBurnPhase.BtIdx(), 1 - b / Sqrt(u));
                alglib.sparseappendelement(j, _vars[nextNextBurnPhaseIndex].BtIdx(), a / Sqrt(u) - 1);
            }
            else if (combiningNextBurn)
            {
                alglib.sparseappendelement(j, thisPhase.BtIdx(), 1 - b / Sqrt(u));
                alglib.sparseappendelement(j, nextBurnPhase.BtIdx(), a / Sqrt(u) - 1);
                alglib.sparseappendelement(j, _vars[nextNextBurnPhaseIndex].BtIdx(), a / Sqrt(u) - 1);
            }
            else
            {
                alglib.sparseappendelement(j, thisPhase.BtIdx(), 1 - b / Sqrt(u));
                alglib.sparseappendelement(j, nextBurnPhase.BtIdx(), a / Sqrt(u) - 1);
            }

            // relaxed bilinear constraint on burn times (needs inequality constraint)
            //f[ci++] = a * b;  //where a <= 1e-6 && b <= 1e-6 -- constraint should be 1/2 of the SFB epsilon value

            return ci;
        }

        private int DynamicConstraints(double[] f, alglib.sparsematrix j, int ci, int p, bool debug)
        {
            _optimizer.Timer.Start();

            PhaseProxy thisPhase = _vars[p];

            double mdot       = _optimizer._phases[p].Mdot;
            double vacThrust  = _optimizer._phases[p].Thrust;
            double vexVacuum  = _optimizer._phases[p].VexVacuum;
            double vexCurrent = _optimizer._phases[p].VexCurrent;
            double h          = thisPhase.Bt() / (_optimizer.N - 1);

            // dynamical constraints per phase
            for (int n = 0; n < _optimizer.N - 1; n += 1)
            {
                int idx = 2 * n;

                double m0,    m1,    m2;
                int    m0Idx, m1Idx, m2Idx;

                if (_optimizer._phases[p].Coast)
                {
                    m0    = m1    = m2    = thisPhase.M[0];
                    m0Idx = m1Idx = m2Idx = thisPhase.M.Idx(0);
                }
                else
                {
                    m0    = thisPhase.M[idx];
                    m1    = thisPhase.M[idx + 1];
                    m2    = thisPhase.M[idx + 2];
                    m0Idx = thisPhase.M.Idx(idx);
                    m1Idx = thisPhase.M.Idx(idx + 1);
                    m2Idx = thisPhase.M.Idx(idx + 2);
                }

                V3              u0,    u1,    u2;
                (int, int, int) u0Idx, u1Idx, u2Idx;

                if (_optimizer._phases[p].Unguided || _optimizer._phases[p].GuidedCoast)
                {
                    u0    = u1    = u2    = thisPhase.U[0];
                    u0Idx = u1Idx = u2Idx = thisPhase.U.Idx(0);
                }
                else
                {
                    u0    = thisPhase.U[idx];
                    u1    = thisPhase.U[idx + 1];
                    u2    = thisPhase.U[idx + 2];
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

                double rho0CdAref = _optimizer._problem.Rho0CdAref;
                double rBody      = _optimizer._problem.RBody;
                double h0         = _optimizer._problem.H0;
                double r0         = _optimizer._problem.R0.magnitude;
                V3     w          = _optimizer._problem.W;

                DualV3 VDot(ref HermiteSimpsonDualPoint d)
                {
                    Dual   r               = d.R.magnitude;
                    Dual   r3              = d.R.sqrMagnitude * r;
                    DualV3 vr              = d.V - DualV3.Cross(w, d.R);
                    var    normAtmosphere  = Dual.Exp(-(r - rBody) / h0);
                    var    normAtmosphere2 = Dual.Exp(-(r - r0) / h0);
                    DualV3 drag            = 0.5 * rho0CdAref * normAtmosphere * vr.sqrMagnitude * vr.normalized;
                    //T = ṁ [v_e_sl + (v_e_vac - v_e_sl)(1 - p_amb/p₀)]
                    Dual thrust = mdot * (vexCurrent + (vexVacuum - vexCurrent) * (1.0 - normAtmosphere2));
                    return -d.R / r3 + thrust / d.M * d.U - drag / d.M;
                }

                DualV3 VDotVacuum(ref HermiteSimpsonDualPoint d)
                {
                    Dual r3 = d.R.sqrMagnitude * d.R.magnitude;
                    return -d.R / r3 + vacThrust / d.M * d.U;
                }

                if (h0 > 0 && rho0CdAref > 0)
                    ci = ApplyHermiteSimpsonDynamics(f, j, ci, VDot, point, indexes, _optimizer.N);
                else
                    ci = ApplyHermiteSimpsonDynamics(f, j, ci, VDotVacuum, point, indexes, _optimizer.N);

                // dm/dt = -mdot (as algebraic constraints rather than defect constraints)

                if (!_optimizer._phases[p].Coast)
                {
                    if (debug) DebugPrint($"start of mass constraints for {n}: {ci - 1}");
                    bool doingMassContinuity = p > 0 && _optimizer._phases[p].MassContinuity;

                    double mi = doingMassContinuity ? thisPhase.M[0] : _optimizer._phases[p].m0;
                    f[ci++] = m1 - mi + (n + 0.5) * h * mdot;
                    f[ci++] = m2 - mi + (n + 1.0) * h * mdot;
                    alglib.sparseappendemptyrow(j);
                    if (doingMassContinuity)
                        alglib.sparseappendelement(j, thisPhase.M.Idx(0), -1.0);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 1), 1.0);
                    alglib.sparseappendelement(j, thisPhase.BtIdx(), (n + 0.5) / (_optimizer.N - 1.0) * mdot);
                    alglib.sparseappendemptyrow(j);
                    if (doingMassContinuity)
                        alglib.sparseappendelement(j, thisPhase.M.Idx(0), -1.0);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), 1.0);
                    alglib.sparseappendelement(j, thisPhase.BtIdx(), (n + 1.0) / (_optimizer.N - 1.0) * mdot);
                }
            }

            _optimizer.Timer.Stop();

            return ci;
        }

        private int ObjectiveFunction(double[] f, alglib.sparsematrix j, int ci)
        {
            PhaseProxy lastPhase = _vars[-1];

            double val = 0;

            // cost metric
            switch (_optimizer._cost)
            {
                case Optimizer.Cost.MIN_TIME:
                    alglib.sparseappendemptyrow(j);

                    for (int p = 0; p < _optimizer._phases.Count; p++)
                    {
                        if (_optimizer._phases[p].Coast || !_optimizer._phases[p].AllowShutdown)
                            continue;

                        PhaseProxy thisPhase = _vars[p];

                        val += thisPhase.Bt();

                        alglib.sparseappendelement(j, thisPhase.BtIdx(), 1.0);
                    }

                    f[ci++] = val;

                    break;
                case Optimizer.Cost.MAX_MASS:
                    f[ci++] = -lastPhase.M[-1];
                    alglib.sparseappendemptyrow(j);
                    alglib.sparseappendelement(j, lastPhase.M.Idx(-1), -1.0);

                    break;
                case Optimizer.Cost.MAX_ENERGY:
                    V3              rf = lastPhase.R[-1];
                    V3              vf = lastPhase.V[-1];
                    (int, int, int) ri = lastPhase.R.Idx(-1);
                    (int, int, int) vi = lastPhase.V.Idx(-1);

                    ci = ApplyScalarConstraintV3(f, j, ci, MaxOrbitalEnergyObjective, new[] { rf, vf }, new[] { ri, vi });

                    break;
                case Optimizer.Cost.MIN_THRUST_ACCEL:
                    alglib.sparseappendemptyrow(j);
                    for (int p = 0; p < _optimizer._phases.Count; p++)
                    {
                        if (_optimizer._phases[p].Coast || !_optimizer._phases[p].AllowShutdown)
                            continue;

                        PhaseProxy thisPhase = _vars[p];

                        double thrust = _optimizer._phases[p].Thrust;
                        double den    = (_optimizer.N - 1) * 6;
                        double h6     = thisPhase.Bt() / den;

                        double sum = 0;
                        for (int k = 0; k < _optimizer._k; k += 1)
                        {
                            double mk = thisPhase.M[k];

                            if (k == 0 || k == _optimizer._k - 1)
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

                    f[ci++] = val;

                    break;
                default:
                    throw new Exception("code should not be reachable");
            }

            return ci;

            Dual MaxOrbitalEnergyObjective(DualV3[] p)
            {
                return -(0.5 * DualV3.Dot(p[1], p[1]) - 1.0 / p[0].magnitude);
            }
        }
    }
}
