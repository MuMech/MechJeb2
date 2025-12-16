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

        public void ConstraintFunction(double[] x, double[] f, alglib.sparsematrix j, object? o)
        {
            bool debug = o != null;

            _optimizer._timeoutToken.ThrowIfCancellationRequested();
            _vars.WrapVars(x);

            int ci = 0;
            f[ci++] = ObjectiveFunction(j);

            for (int p = 0; p < _optimizer._phases.Count; p++)
            {
                if (debug) DebugPrint($"start of dynamic constraints: {ci - 1}");
                ci = DynamicConstraints(f, j, p, ci, debug);
                if (debug) DebugPrint($"start of staging constraints: {ci - 1}");
                ci = StagingConstraints(f, j, p, ci);
                if (debug) DebugPrint($"start of control norm constraints: {ci - 1}");
                ci = ControlNormConstraints(f, j, p, ci);
                if (debug) DebugPrint($"start of continuity constraints: {ci - 1}");
                ci = ContinuityConstraints(f, j, p, ci, debug);
            }

            if (debug) DebugPrint($"start of terminal constraints: {ci - 1}");
            PhaseProxy lastPhase = _vars[-1];

            _optimizer._terminal.Constraints(x, lastPhase.R.Idx(-1), lastPhase.V.Idx(-1), f, j, ref ci);

            if (ci != _vars.TotalConstraints + 1)
                throw new Exception("Constraint num mismatch");
        }

        private int ControlNormConstraints(double[] f, alglib.sparsematrix j, int p, int ci)
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

        private int ContinuityConstraints(double[] f, alglib.sparsematrix j, int p, int ci, bool debug)
        {
            if (p == 0)
                return ci;

            PhaseProxy thisPhase = _vars[p];
            PhaseProxy prevPhase = _vars[p - 1];

            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.R[-1], thisPhase.R[0] }, new[] { prevPhase.R.Idx(-1), thisPhase.R.Idx(0) });
            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.V[-1], thisPhase.V[0] }, new[] { prevPhase.V.Idx(-1), thisPhase.V.Idx(0) });
            ci = ApplyVectorConstraintV3(f, j, ci, VecDiff, new[] { prevPhase.U[-1], thisPhase.U[0] }, new[] { prevPhase.U.Idx(-1), thisPhase.U.Idx(0) });

            // mass continuity for coast-within-phase
            if (p > 0 && _optimizer._phases[p].MassContinuity)
            {
                if (debug) DebugPrint($"mass continuity constraint: {ci - 1}");
                ci = ApplyScalarConstraint(f, j, ci, Diff, new[] { prevPhase.M[-1], thisPhase.M[0] }, new[] { prevPhase.M.Idx(-1), thisPhase.M.Idx(0) });
            }

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

        private int StagingConstraints(double[] f, alglib.sparsematrix j, int p, int ci)
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

        private int DynamicConstraints(double[] f, alglib.sparsematrix j, int p, int ci, bool debug)
        {
            PhaseProxy thisPhase = _vars[p];

            double mdot   = _optimizer._phases[p].Mdot;
            double thrust = _optimizer._phases[p].Thrust;
            double h      = thisPhase.Bt() / (_optimizer.N - 1);

            // dynamical constraints per phase
            for (int n = 0; n < _optimizer.N - 1; n += 1)
            {
                int idx = 2 * n;

                V3 r0 = thisPhase.R[idx];
                V3 r1 = thisPhase.R[idx + 1];
                V3 r2 = thisPhase.R[idx + 2];
                V3 v0 = thisPhase.V[idx];
                V3 v1 = thisPhase.V[idx + 1];
                V3 v2 = thisPhase.V[idx + 2];

                double m0, m1, m2;

                if (_optimizer._phases[p].Coast)
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

                if (_optimizer._phases[p].GuidedCoast)
                {
                    u0 = u1 = u2 = V3.zero;
                }
                else if (_optimizer._phases[p].Unguided)
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

                if (debug) DebugPrint($"start of position constraints for {n}: {ci - 1}");
                f[ci++] = dR1.x;
                f[ci++] = dR2.x;
                f[ci++] = dR1.y;
                f[ci++] = dR2.y;
                f[ci++] = dR1.z;
                f[ci++] = dR2.z;

                V3 dR1dBt = -1.0 / 6.0 / (_optimizer.N - 1) * (v0 + 4 * v1 + v2);
                V3 dR2dBt = -0.125 / (_optimizer.N - 1) * (v0 - v2);

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

                if (debug) DebugPrint($"start of velocity constraints for {n}: {ci - 1}");
                f[ci++] = dv1.x;
                f[ci++] = dv2.x;
                f[ci++] = dv1.y;
                f[ci++] = dv2.y;
                f[ci++] = dv1.z;
                f[ci++] = dv2.z;

                V3 dv1dbt = -1.0 / 6.0 / (_optimizer.N - 1) * (dvdt0 + 4 * dvdt1 + dvdt2);
                V3 dv2dbt = -0.125 / (_optimizer.N - 1) * (dvdt0 - dvdt2);

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

                if (!_optimizer._phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv1dm0.x);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 1), dv1dm1.x);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv1dm2.x);
                }

                if (!_optimizer._phases[p].GuidedCoast)
                {
                    if (_optimizer._phases[p].Unguided)
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

                if (!_optimizer._phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv2dm0.x);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv2dm2.x);
                }

                if (!_optimizer._phases[p].GuidedCoast)
                {
                    if (_optimizer._phases[p].Unguided)
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
                if (!_optimizer._phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv1dm0.y);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 1), dv1dm1.y);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv1dm2.y);
                }

                if (!_optimizer._phases[p].GuidedCoast)
                {
                    if (_optimizer._phases[p].Unguided)
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
                if (!_optimizer._phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv2dm0.y);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv2dm2.y);
                }

                if (!_optimizer._phases[p].GuidedCoast)
                {
                    if (_optimizer._phases[p].Unguided)
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
                if (!_optimizer._phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv1dm0.z);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 1), dv1dm1.z);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv1dm2.z);
                }

                if (!_optimizer._phases[p].GuidedCoast)
                {
                    if (_optimizer._phases[p].Unguided)
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
                if (!_optimizer._phases[p].Coast)
                {
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx), dv2dm0.z);
                    alglib.sparseappendelement(j, thisPhase.M.Idx(idx + 2), dv2dm2.z);
                }

                if (!_optimizer._phases[p].GuidedCoast)
                {
                    if (_optimizer._phases[p].Unguided)
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

            return ci;
        }

        private double ObjectiveFunction(alglib.sparsematrix j)
        {
            PhaseProxy lastPhase = _vars[-1];
            double     val       = 0;

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

                    break;
                case Optimizer.Cost.MAX_MASS:
                    val = -lastPhase.M[-1];
                    alglib.sparseappendemptyrow(j);
                    alglib.sparseappendelement(j, lastPhase.M.Idx(-1), -1.0);

                    break;
                case Optimizer.Cost.MAX_ENERGY:
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

                    break;
                default:
                    throw new Exception("code should not be reachable");
            }

            return val;
        }
    }
}
