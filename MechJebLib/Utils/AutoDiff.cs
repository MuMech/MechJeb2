using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MechJebLib.Primitives;

namespace MechJebLib.Utils
{
    public static class AutoDiff
    {
        private static (double, double[]) Gradient(Func<Dual[], Dual> f, double[] point)
        {
            int      n        = point.Length;
            double[] partials = new double[n];
            var      ans      = new Dual(0);

            for (int i = 0; i < n; i++)
            {
                var duals = new Dual[n];
                for (int j = 0; j < n; j++)
                    duals[j] = new Dual(point[j], i == j ? 1.0 : 0.0);

                ans         = f(duals);
                partials[i] = ans.D;
            }

            return (ans.M, partials);
        }

        private static (double, double[]) GradientV3(Func<DualV3[], Dual> f, V3[] point)
        {
            int      n        = point.Length;
            double[] partials = new double[3 * n];
            var      ans      = new Dual(0);

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var duals = new DualV3[n];
                    for (int j = 0; j < n; j++)
                        duals[j] = new DualV3(point[j], V3.zero);

                    duals[i] = new DualV3(point[i], k switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis });

                    ans                 = f(duals);
                    partials[i * 3 + k] = ans.D;
                }
            }

            return (ans.M, partials);
        }

        public static (V3, V3[]) JacobianV3(Func<DualV3[], DualV3> f, V3[] point)
        {
            int n        = point.Length;
            var partials = new V3[3 * n];
            var ans      = new DualV3(V3.zero, V3.zero);

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var duals = new DualV3[n];
                    for (int j = 0; j < n; j++)
                        duals[j] = new DualV3(point[j], V3.zero);

                    duals[i] = new DualV3(point[i], k switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis });

                    ans                 = f(duals);
                    partials[i * 3 + k] = ans.D;
                }
            }

            return (ans.M, partials);
        }

        public static int ApplyScalarConstraint(double[] f, alglib.sparsematrix j, int ci, Func<Dual[], Dual> g, double[] p, int[] idx)
        {
            (double value, double[] partials) = Gradient(g, p);

            int n = p.Length;

            var elements = new SortedDictionary<int, double>();
            for (int i = 0; i < n; i++)
                elements[idx[i]] = partials[i];

            f[ci++] = value;
            alglib.sparseappendemptyrow(j);
            foreach (KeyValuePair<int, double> kv in elements)
                if (kv.Value != 0)
                    alglib.sparseappendelement(j, kv.Key, kv.Value);

            return ci;
        }

        public static int ApplyScalarConstraintV3(double[] f, alglib.sparsematrix j, int ci, Func<DualV3[], Dual> g, V3[] p, (int, int, int)[] idx)
        {
            (double value, double[] partials) = GradientV3(g, p);

            int n = p.Length;

            var elements = new SortedDictionary<int, double>();
            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partials[3 * i];
                elements[idx[i].Item2] = partials[3 * i + 1];
                elements[idx[i].Item3] = partials[3 * i + 2];
            }

            f[ci++] = value;
            alglib.sparseappendemptyrow(j);
            foreach (KeyValuePair<int, double> kv in elements)
                if (kv.Value != 0)
                    alglib.sparseappendelement(j, kv.Key, kv.Value);

            return ci;
        }

        public static int ApplyVectorConstraintV3(double[] f, alglib.sparsematrix j, int ci, Func<DualV3[], DualV3> g, V3[] p, (int, int, int)[] idx)
        {
            (V3 value, V3[] partials) = JacobianV3(g, p);

            int n = p.Length;

            var elements = new SortedDictionary<int, double>();
            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partials[3 * i].x;
                elements[idx[i].Item2] = partials[3 * i + 1].x;
                elements[idx[i].Item3] = partials[3 * i + 2].x;
            }

            f[ci++] = value.x;
            alglib.sparseappendemptyrow(j);
            foreach (KeyValuePair<int, double> kv in elements)
                if (kv.Value != 0)
                    alglib.sparseappendelement(j, kv.Key, kv.Value);

            elements.Clear();
            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partials[3 * i].y;
                elements[idx[i].Item2] = partials[3 * i + 1].y;
                elements[idx[i].Item3] = partials[3 * i + 2].y;
            }

            f[ci++] = value.y;
            alglib.sparseappendemptyrow(j);
            foreach (KeyValuePair<int, double> kv in elements)
                if (kv.Value != 0)
                    alglib.sparseappendelement(j, kv.Key, kv.Value);

            elements.Clear();
            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partials[3 * i].z;
                elements[idx[i].Item2] = partials[3 * i + 1].z;
                elements[idx[i].Item3] = partials[3 * i + 2].z;
            }

            f[ci++] = value.z;
            alglib.sparseappendemptyrow(j);
            foreach (KeyValuePair<int, double> kv in elements)
                if (kv.Value != 0)
                    alglib.sparseappendelement(j, kv.Key, kv.Value);

            return ci;
        }

        public struct HermiteSimpsonSegment
        {
            public V3     R0, R1, R2, V0, V1, V2, U0, U1, U2;
            public double M0, M1, M2;
            public double Bt;
        }

        public struct HermiteSimpsonDualPoint
        {
            public DualV3 R, V, U;
            public Dual   M;

            public void Seed(int k, double s)
            {
                int field   = k / 3;
                int axis    = k % 3;
                V3  seedVec = s * axis switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis };

                switch (field)
                {
                    case 0: R = new DualV3(R.M, seedVec); break;
                    case 1: V = new DualV3(V.M, seedVec); break;
                    case 2: U = new DualV3(U.M, seedVec); break;
                    case 3: M = new Dual(M.M, s); break;
                }
            }
        }

        public struct HermiteSimpsonIndexes
        {
            public (int, int, int) R0Idx, R1Idx, R2Idx, V0Idx, V1Idx, V2Idx, U0Idx, U1Idx, U2Idx;
            public int             M0Idx, M1Idx, M2Idx;
            public int             BtIdx;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Index(int k)
            {
                if (k < 18)
                {
                    int bank  = k / 9;
                    int axis  = k % 9 / 3;
                    int point = k % 3;
                    return bank switch
                    {
                        0 => axis switch
                        {
                            0 => point switch { 0 => R0Idx.Item1, 1 => R1Idx.Item1, _ => R2Idx.Item1 },
                            1 => point switch { 0 => R0Idx.Item2, 1 => R1Idx.Item2, _ => R2Idx.Item2 },
                            _ => point switch { 0 => R0Idx.Item3, 1 => R1Idx.Item3, _ => R2Idx.Item3 }
                        },
                        _ => axis switch
                        {
                            0 => point switch { 0 => V0Idx.Item1, 1 => V1Idx.Item1, _ => V2Idx.Item1 },
                            1 => point switch { 0 => V0Idx.Item2, 1 => V1Idx.Item2, _ => V2Idx.Item2 },
                            _ => point switch { 0 => V0Idx.Item3, 1 => V1Idx.Item3, _ => V2Idx.Item3 }
                        }
                    };
                }

                if (k < 21)
                {
                    int point = k % 3;
                    return point switch { 0 => M0Idx, 1 => M1Idx, _ => M2Idx };
                }

                if (k < 30)
                {
                    int axis  = (k - 21) / 3;
                    int point = k % 3;
                    return axis switch
                    {
                        0 => point switch { 0 => U0Idx.Item1, 1 => U1Idx.Item1, _ => U2Idx.Item1 },
                        1 => point switch { 0 => U0Idx.Item2, 1 => U1Idx.Item2, _ => U2Idx.Item2 },
                        _ => point switch { 0 => U0Idx.Item3, 1 => U1Idx.Item3, _ => U2Idx.Item3 }
                    };
                }

                return BtIdx;
            }
        }

        /*
         * state2 - state0 - h6*(g(p0) + 4*g(p1) + g(p2))
         * state1 - 0.5*(state0 + state2) - h8*(g(p0) - g(p2))
         *
         * V3 dR1 = r2 - r0 - h6 * (v0 + 4 * v1 + v2);
         * V3 dR2 = r1 - 0.5 * (r0 + r2) - h8 * (v0 - v2);
         * V3 dv1 = v2 - v0 - h6 * (dvdt0 + 4 * dvdt1 + dvdt2);
         * V3 dv2 = v1 - 0.5 * (v0 + v2) - h8 * (dvdt0 - dvdt2);
         */

        public delegate DualV3 DynamicsCallback(ref HermiteSimpsonDualPoint d);

        public delegate Dual ScalarDynamicsCallback(ref HermiteSimpsonDualPoint d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetDual(int k, ref HermiteSimpsonDualPoint d0, ref HermiteSimpsonDualPoint d1, ref HermiteSimpsonDualPoint d2, double val)
        {
            if (k < 18)
            {
                int bank  = k / 9;
                int axis  = k % 9 / 3;
                int point = k % 3;
                V3  vec   = val * axis switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis };
                switch (bank)
                {
                    case 0:
                        {
                            switch (point)
                            {
                                case 0:
                                    d0.R = new DualV3(d0.R.M, vec);
                                    break;
                                case 1:
                                    d1.R = new DualV3(d1.R.M, vec);
                                    break;
                                default:
                                    d2.R = new DualV3(d2.R.M, vec);
                                    break;
                            }
                        }
                        break;
                    default:
                        {
                            switch (point)
                            {
                                case 0:
                                    d0.V = new DualV3(d0.V.M, vec);
                                    break;
                                case 1:
                                    d1.V = new DualV3(d1.V.M, vec);
                                    break;
                                default:
                                    d2.V = new DualV3(d2.V.M, vec);
                                    break;
                            }
                        }
                        break;
                }
            }
            else if (k < 21)
            {
                int point = k % 3;
                switch (point)
                {
                    case 0:
                        d0.M = new Dual(d0.M.M, val);
                        break;
                    case 1:
                        d1.M = new Dual(d1.M.M, val);
                        break;
                    default:
                        d2.M = new Dual(d2.M.M, val);
                        break;
                }
            }
            else if (k < 30)
            {
                int axis  = (k - 21) / 3;
                int point = k % 3;
                V3  vec   = val * axis switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis };
                switch (point)
                {
                    case 0:
                        d0.U = new DualV3(d0.U.M, vec);
                        break;
                    case 1:
                        d1.U = new DualV3(d1.U.M, vec);
                        break;
                    default:
                        d2.U = new DualV3(d2.U.M, vec);
                        break;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ApplyHermiteSimpsonDynamics(double[] f, alglib.sparsematrix j, int ci, DynamicsCallback vDot, HermiteSimpsonSegment segment, HermiteSimpsonIndexes indexes, int n)
        {
            const int NUM_VARS = 31;
            DualV3    ans;
            var       jac = new V3[NUM_VARS];

            var d0  = new HermiteSimpsonDualPoint { R = segment.R0, V = segment.V0, U = segment.U0, M = segment.M0 };
            var d1  = new HermiteSimpsonDualPoint { R = segment.R1, V = segment.V1, U = segment.U1, M = segment.M1 };
            var d2  = new HermiteSimpsonDualPoint { R = segment.R2, V = segment.V2, U = segment.U2, M = segment.M2 };
            var dbt = new Dual(segment.Bt);

            /*
             * RDot
             */

            Dual H  = dbt / (n - 1);
            Dual H6 = H / 6.0;

            d0.R = new DualV3(d0.R.M, V3.one);

            ans = d2.R - d0.R - H6 * (d0.V + 4 * d1.V + d2.V);

            jac[0].x = ans.x.D;
            jac[3].y = ans.y.D;
            jac[6].z = ans.z.D;

            d0.R = new DualV3(d0.R.M, V3.zero);
            d2.R = new DualV3(d2.R.M, V3.one);

            ans = d2.R - d0.R - H6 * (d0.V + 4 * d1.V + d2.V);

            jac[2].x = ans.x.D;
            jac[5].y = ans.y.D;
            jac[8].z = ans.z.D;

            d2.R = new DualV3(d2.R.M, V3.zero);
            d0.V = new DualV3(d0.V.M, V3.one);

            ans = d2.R - d0.R - H6 * (d0.V + 4 * d1.V + d2.V);

            jac[9].x  = ans.x.D;
            jac[12].y = ans.y.D;
            jac[15].z = ans.z.D;

            d0.V = new DualV3(d0.V.M, V3.zero);
            d1.V = new DualV3(d1.V.M, V3.one);

            ans = d2.R - d0.R - H6 * (d0.V + 4 * d1.V + d2.V);

            jac[10].x = ans.x.D;
            jac[13].y = ans.y.D;
            jac[16].z = ans.z.D;

            d1.V = new DualV3(d1.V.M, V3.zero);
            d2.V = new DualV3(d2.V.M, V3.one);

            ans = d2.R - d0.R - H6 * (d0.V + 4 * d1.V + d2.V);

            jac[11].x = ans.x.D;
            jac[14].y = ans.y.D;
            jac[17].z = ans.z.D;

            d2.V = new DualV3(d2.V.M, V3.zero);
            dbt  = new Dual(segment.Bt, 1);

            H  = dbt / (n - 1);
            H6 = H / 6.0;

            ans = d2.R - d0.R - H6 * (d0.V + 4 * d1.V + d2.V);

            jac[30].x = ans.x.D;
            jac[30].y = ans.y.D;
            jac[30].z = ans.z.D;

            dbt = new Dual(segment.Bt);

            f[ci++] = ans.M.x;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].x != 0)
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].x);

            f[ci++] = ans.M.y;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].y != 0)
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].y);

            f[ci++] = ans.M.z;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].z != 0)
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].z);

            jac = new V3[NUM_VARS];

            H = dbt / (n - 1);
            Dual H8 = H * 0.125;

            d0.R = new DualV3(d0.R.M, V3.one);

            ans = d1.R - 0.5 * (d0.R + d2.R) - H8 * (d0.V - d2.V);

            jac[0].x = ans.x.D;
            jac[3].y = ans.y.D;
            jac[6].z = ans.z.D;

            d0.R = new DualV3(d0.R.M, V3.zero);
            d1.R = new DualV3(d1.R.M, V3.one);

            ans = d1.R - 0.5 * (d0.R + d2.R) - H8 * (d0.V - d2.V);

            jac[1].x = ans.x.D;
            jac[4].y = ans.y.D;
            jac[7].z = ans.z.D;

            d1.R = new DualV3(d1.R.M, V3.zero);
            d2.R = new DualV3(d2.R.M, V3.one);

            ans = d1.R - 0.5 * (d0.R + d2.R) - H8 * (d0.V - d2.V);

            jac[2].x = ans.x.D;
            jac[5].y = ans.y.D;
            jac[8].z = ans.z.D;

            d2.R = new DualV3(d2.R.M, V3.zero);
            d0.V = new DualV3(d0.V.M, V3.one);

            ans = d1.R - 0.5 * (d0.R + d2.R) - H8 * (d0.V - d2.V);

            jac[9].x  = ans.x.D;
            jac[12].y = ans.y.D;
            jac[15].z = ans.z.D;

            d0.V = new DualV3(d0.V.M, V3.zero);
            d2.V = new DualV3(d2.V.M, V3.one);

            ans = d1.R - 0.5 * (d0.R + d2.R) - H8 * (d0.V - d2.V);

            jac[11].x = ans.x.D;
            jac[14].y = ans.y.D;
            jac[17].z = ans.z.D;

            d2.V = new DualV3(d2.V.M, V3.zero);
            dbt  = new Dual(segment.Bt, 1);

            H  = dbt / (n - 1);
            H8 = H * 0.125;

            ans = d1.R - 0.5 * (d0.R + d2.R) - H8 * (d0.V - d2.V);

            jac[30].x = ans.x.D;
            jac[30].y = ans.y.D;
            jac[30].z = ans.z.D;

            dbt = new Dual(segment.Bt);

            f[ci++] = ans.M.x;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].x != 0)
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].x);

            f[ci++] = ans.M.y;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].y != 0)
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].y);

            f[ci++] = ans.M.z;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].z != 0)
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].z);

            /*
             * VDOT
             */

            bool singleControlVariable = indexes.Index(21) == indexes.Index(22);

            jac = new V3[NUM_VARS];

            for (int k = 0; k < NUM_VARS; k++)
            {
                if (k < 30)
                    SetDual(k, ref d0, ref d1, ref d2, 1);
                else
                    dbt = new Dual(segment.Bt, 1);

                Dual h  = dbt / (n - 1);
                Dual h6 = h / 6.0;

                ans = d2.V - d0.V - h6 * (vDot(ref d0) + 4 * vDot(ref d1) + vDot(ref d2));

                if (singleControlVariable && k >= 21 && k <= 23)
                {
                    jac[21] += ans.D;
                }
                else if (singleControlVariable && k >= 24 && k <= 26)
                {
                    jac[24] += ans.D;
                }
                else if (singleControlVariable && k >= 27 && k <= 29)
                {
                    jac[27] += ans.D;
                }
                else
                {
                    jac[k] = ans.D;
                }

                if (k < 30)
                    SetDual(k, ref d0, ref d1, ref d2, 0);
                else
                    dbt = new Dual(segment.Bt);
            }

            f[ci++] = ans.M.x;

            int lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].x != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].x);
                    lastindex = index;
                }

            f[ci++] = ans.M.y;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].y != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].y);
                    lastindex = index;
                }

            f[ci++] = ans.M.z;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].z != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].z);
                    lastindex = index;
                }

            jac = new V3[NUM_VARS];

            for (int k = 0; k < NUM_VARS; k++)
            {
                if (k < 30)
                    SetDual(k, ref d0, ref d1, ref d2, 1);
                else
                    dbt = new Dual(segment.Bt, 1);

                Dual h  = dbt / (n - 1);
                Dual h8 = h * 0.125;

                ans = d1.V - 0.5 * (d0.V + d2.V) - h8 * (vDot(ref d0) - vDot(ref d2));

                if (singleControlVariable && k >= 21 && k <= 23)
                {
                    jac[21] += ans.D;
                }
                else if (singleControlVariable && k >= 24 && k <= 26)
                {
                    jac[24] += ans.D;
                }
                else if (singleControlVariable && k >= 27 && k <= 29)
                {
                    jac[27] += ans.D;
                }
                else
                {
                    jac[k] = ans.D;
                }

                if (k < 30)
                    SetDual(k, ref d0, ref d1, ref d2, 0);
                else
                    dbt = new Dual(segment.Bt);
            }

            f[ci++] = ans.M.x;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].x != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jac[k].x);
                    lastindex = index;
                }

            // here
            f[ci++] = ans.M.y;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].y != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].y);
                    lastindex = index;
                }

            f[ci++] = ans.M.z;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jac[k].z != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, indexes.Index(k), jac[k].z);
                    lastindex = index;
                }

            return ci;
        }
    }
}
