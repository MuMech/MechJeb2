/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Text;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;
using static MechJebLib.Utils.AutoDiff;

namespace MechJebLibTest.Utils
{
    public class AutoDiffTests
    {
        public enum SegmentKind { GUIDED_BURN, UNGUIDED_BURN, GUIDED_COAST, UNGUIDED_COAST }

        private const int    N           = 1;
        private const double MDOT        = 1.5;
        private const double THRUST      = 2.0;
        private const double RHO0_CDAREF = 2.0;
        private const double R_BODY      = 1.0;
        private const double H0          = 0.01;

        private static readonly V3 _w = new V3(0, 0, 0.06);

        // One Gauss-Legendre segment (N = 1, so four grid points) of a phase, with the decision variables laid out the
        // way PhaseProxy lays them out and the segment assembled the way AscentProblem.DynamicConstraints does it: a
        // coast has a single mass, an unguided phase a single control and throttle, a guided coast neither.
        private class SegmentLayout
        {
            public readonly bool                 Coast;
            public readonly GaussLegendreIndexes Indexes;
            public readonly int                  NumVars;
            public readonly double[]             X;

            private readonly bool   _guidedCoast;
            private readonly double _thrust;

            public SegmentLayout(SegmentKind kind)
            {
                Coast = kind == SegmentKind.GUIDED_COAST || kind == SegmentKind.UNGUIDED_COAST;
                bool unguided = kind == SegmentKind.UNGUIDED_BURN || kind == SegmentKind.UNGUIDED_COAST;
                _guidedCoast = Coast && !unguided;
                _thrust      = Coast ? 0 : THRUST;

                var ix = new GaussLegendreIndexes
                {
                    R0Idx = (0, 4, 8),
                    R1Idx = (1, 5, 9),
                    R2Idx = (2, 6, 10),
                    R3Idx = (3, 7, 11),
                    V0Idx = (12, 16, 20),
                    V1Idx = (13, 17, 21),
                    V2Idx = (14, 18, 22),
                    V3Idx = (15, 19, 23)
                };

                int idx = 24;

                if (Coast)
                {
                    ix.M0Idx = ix.M1Idx = ix.M2Idx = ix.M3Idx = idx;
                    idx += 1;
                }
                else
                {
                    ix.M0Idx = idx;
                    ix.M1Idx = idx + 1;
                    ix.M2Idx = idx + 2;
                    ix.M3Idx = idx + 3;
                    idx += 4;
                }

                if (unguided)
                {
                    ix.U1Idx = ix.U2Idx = (idx, idx + 1, idx + 2, idx + 3);
                    ix.T1Idx = ix.T2Idx = idx + 4;
                    idx += 5;
                }
                else if (_guidedCoast)
                {
                    ix.U1Idx = ix.U2Idx = (-1, -1, -1, -1);
                    ix.T1Idx = ix.T2Idx = -1;
                }
                else
                {
                    ix.U1Idx = (idx, idx + 2, idx + 4, idx + 6);
                    ix.U2Idx = (idx + 1, idx + 3, idx + 5, idx + 7);
                    ix.T1Idx = idx + 8;
                    ix.T2Idx = idx + 9;
                    idx += 10;
                }

                ix.BtIdx = idx;
                NumVars  = idx + 1;
                Indexes  = ix;

                // low in a scale-height-0.01 atmosphere, so drag (and its 1/m) is significant
                X = new double[NumVars];
                Put(ix.R0Idx, new V3(1.020, 0.000, 0.010));
                Put(ix.R1Idx, new V3(1.019, 0.031, 0.011));
                Put(ix.R2Idx, new V3(1.017, 0.072, 0.013));
                Put(ix.R3Idx, new V3(1.014, 0.101, 0.014));
                Put(ix.V0Idx, new V3(-0.05, 0.98, 0.02));
                Put(ix.V1Idx, new V3(-0.06, 0.99, 0.03));
                Put(ix.V2Idx, new V3(-0.08, 1.01, 0.03));
                Put(ix.V3Idx, new V3(-0.09, 1.02, 0.04));
                X[ix.M0Idx] = 0.50;
                if (!Coast)
                {
                    X[ix.M1Idx] = 0.49;
                    X[ix.M2Idx] = 0.48;
                    X[ix.M3Idx] = 0.47;
                }

                if (!_guidedCoast)
                {
                    Put(ix.U2Idx, new Q3(0.11, 0.19, 0.32, 0.91));
                    Put(ix.U1Idx, new Q3(0.10, 0.20, 0.30, 0.92));
                    X[ix.T2Idx] = 0.95;
                    X[ix.T1Idx] = 0.90;
                }

                X[ix.BtIdx] = 0.1;
            }

            private void Put((int, int, int) idx, V3 v)
            {
                X[idx.Item1] = v.x;
                X[idx.Item2] = v.y;
                X[idx.Item3] = v.z;
            }

            private void Put((int, int, int, int) idx, Q3 q)
            {
                X[idx.Item1] = q.x;
                X[idx.Item2] = q.y;
                X[idx.Item3] = q.z;
                X[idx.Item4] = q.w;
            }

            private static V3 GetV3(double[] x, (int, int, int) idx) => new V3(x[idx.Item1], x[idx.Item2], x[idx.Item3]);

            private static Q3 GetQ3(double[] x, (int, int, int, int) idx) => new Q3(x[idx.Item1], x[idx.Item2], x[idx.Item3], x[idx.Item4]);

            public GaussLegendreSegment Segment(double[] x) =>
                new GaussLegendreSegment
                {
                    R0 = GetV3(x, Indexes.R0Idx),
                    R1 = GetV3(x, Indexes.R1Idx),
                    R2 = GetV3(x, Indexes.R2Idx),
                    R3 = GetV3(x, Indexes.R3Idx),
                    V0 = GetV3(x, Indexes.V0Idx),
                    V1 = GetV3(x, Indexes.V1Idx),
                    V2 = GetV3(x, Indexes.V2Idx),
                    V3 = GetV3(x, Indexes.V3Idx),
                    M0 = x[Indexes.M0Idx],
                    M1 = x[Indexes.M1Idx],
                    M2 = x[Indexes.M2Idx],
                    M3 = x[Indexes.M3Idx],
                    U1 = _guidedCoast ? Q3.zero : GetQ3(x, Indexes.U1Idx),
                    U2 = _guidedCoast ? Q3.zero : GetQ3(x, Indexes.U2Idx),
                    T1 = _guidedCoast ? 0 : x[Indexes.T1Idx],
                    T2 = _guidedCoast ? 0 : x[Indexes.T2Idx],
                    Bt = x[Indexes.BtIdx]
                };

            // AscentProblem's VDotAtmo with constant vexes
            public DualV3 VDot(ref GaussLegendreDualPoint d)
            {
                Dual r = d.R.magnitude;
                Dual r3 = d.R.sqrMagnitude * r;
                DualV3 vr = d.V - DualV3.Cross(_w, d.R);
                DualV3 drag = 0.5 * RHO0_CDAREF * AscentProblem.NormalizedDensity(r, R_BODY, H0) * vr.sqrMagnitude * vr.normalized;
                return -d.R / r3 + _thrust / d.M * d.U.forward * d.T - drag / d.M;
            }
        }

        private static double[] Evaluate(SegmentLayout layout, double[] x, out alglib.sparsematrix j)
        {
            double[] f = new double[layout.Coast ? 18 : 21];
            alglib.sparsecreatecrsempty(layout.NumVars, out j);

            GaussLegendreSegment segment = layout.Segment(x);

            int ci = ApplyGaussLegendreDynamics(f, j, 0, layout.VDot, segment, layout.Indexes, N);
            if (!layout.Coast)
                ci = ApplyMDotDynamics(f, j, ci, MDOT, segment, layout.Indexes, N);

            ci.ShouldEqual(f.Length);

            return f;
        }

        // Several dual seeds map to the same decision variable when a phase has a single mass (coasts) or a single
        // control and throttle (unguided); their partials have to be summed into that one Jacobian column.
        [Theory]
        [InlineData(SegmentKind.GUIDED_BURN)]
        [InlineData(SegmentKind.UNGUIDED_BURN)]
        [InlineData(SegmentKind.GUIDED_COAST)]
        [InlineData(SegmentKind.UNGUIDED_COAST)]
        public void GaussLegendreDynamicsJacobianMatchesFiniteDifferences(SegmentKind kind)
        {
            const double STEP = 1e-6;
            const double TOL  = 1e-7;

            var layout = new SegmentLayout(kind);

            double[] f = Evaluate(layout, layout.X, out alglib.sparsematrix jac);

            var failures = new StringBuilder();

            for (int col = 0; col < layout.NumVars; col++)
            {
                double[] xp = (double[])layout.X.Clone();
                double[] xm = (double[])layout.X.Clone();
                xp[col] += STEP;
                xm[col] -= STEP;

                double[] fp = Evaluate(layout, xp, out _);
                double[] fm = Evaluate(layout, xm, out _);

                for (int row = 0; row < f.Length; row++)
                {
                    double fd = (fp[row] - fm[row]) / (2 * STEP);
                    double analytic = alglib.sparseget(jac, row, col);

                    if (Math.Abs(analytic - fd) > TOL * Math.Max(1.0, Math.Abs(fd)))
                        failures.AppendLine($"row {row} col {col}: analytic {analytic:G17} finite difference {fd:G17}");
                }
            }

            Assert.True(failures.Length == 0, failures.ToString());
        }

        /*
        [Fact]
        private void JacobianDifference()
        {
            (V3 result, V3[] jac) = JacobianV3(x => x[0] - x[1], new[] { new V3(1, 2, 3), new V3(4, 5, 6) });
            result.ShouldEqual(new V3(-3, -3, -3));
            jac[0].ShouldEqual(new V3(1, 0, 0));
            jac[1].ShouldEqual(new V3(0, 1, 0));
            jac[2].ShouldEqual(new V3(0, 0, 1));
            jac[3].ShouldEqual(new V3(-1, 0, 0));
            jac[4].ShouldEqual(new V3(0, -1, 0));
            jac[5].ShouldEqual(new V3(0, 0, -1));
        }

        [Fact]
        private void JacobianCross()
        {
            (V3 result, V3[] jac) = JacobianV3(x => DualV3.Cross(x[0], x[1]), new[] { new V3(1, 2, 3), new V3(4, 5, 6) });
            result.ShouldEqual(new V3(-3, 6, -3));
            jac[0].ShouldEqual(new V3(0, -6, 5));
            jac[1].ShouldEqual(new V3(6, 0, -4));
            jac[2].ShouldEqual(new V3(-5, 4, 0));
            jac[3].ShouldEqual(new V3(0, 3, -2));
            jac[4].ShouldEqual(new V3(-3, 0, 1));
            jac[5].ShouldEqual(new V3(2, -1, 0));
        }
        */
    }
}
