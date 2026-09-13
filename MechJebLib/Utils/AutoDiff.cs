/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using MechJebLib.Primitives;

namespace MechJebLib.Utils
{
    public static class AutoDiff
    {
        private static readonly ThreadLocal<Dual[]> _dualBuffer = new ThreadLocal<Dual[]>(() => new Dual[16]);
        private static readonly ThreadLocal<DualV3[]> _dualV3Buffer = new ThreadLocal<DualV3[]>(() => new DualV3[16]);
        private static readonly ThreadLocal<DualQ3[]> _dualQ3Buffer = new ThreadLocal<DualQ3[]>(() => new DualQ3[16]);

        private static Dual[] RentDualArray(int n)
        {
            Dual[] buf = _dualBuffer.Value;
            if (buf.Length < n)
            {
                buf = new Dual[n];
                _dualBuffer.Value = buf;
            }

            return buf;
        }

        private static DualV3[] RentDualV3Array(int n)
        {
            DualV3[] buf = _dualV3Buffer.Value;
            if (buf.Length < n)
            {
                buf = new DualV3[n];
                _dualV3Buffer.Value = buf;
            }

            return buf;
        }

        private static DualQ3[] RentDualQ3Array(int n)
        {
            DualQ3[] buf = _dualQ3Buffer.Value;
            if (buf.Length < n)
            {
                buf = new DualQ3[n];
                _dualQ3Buffer.Value = buf;
            }

            return buf;
        }

        private static (double, Vec) Gradient(Func<Dual[], Dual> f, double[] point)
        {
            int n = point.Length;
            var partials = Vec.Rent(n);
            var ans = new Dual(0);

            Dual[] duals = RentDualArray(n);

            for (int j = 0; j < n; j++)
                duals[j] = new Dual(point[j]);

            for (int i = 0; i < n; i++)
            {
                duals[i] = new Dual(point[i], 1.0);

                ans = f(duals);
                partials[i] = ans.D;

                duals[i] = new Dual(point[i]);
            }

            return (ans.M, partials);
        }

        private static (double, Vec) GradientV3(Func<DualV3[], Dual> f, V3[] point)
        {
            int n = point.Length;
            var partials = Vec.Rent(3 * n);
            var ans = new Dual(0);

            DualV3[] duals = RentDualV3Array(n);
            for (int j = 0; j < n; j++)
                duals[j] = new DualV3(point[j], V3.zero);

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    duals[i] = new DualV3(point[i], k switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis });

                    ans = f(duals);
                    partials[i * 3 + k] = ans.D;

                    duals[i] = new DualV3(point[i], V3.zero);
                }
            }

            return (ans.M, partials);
        }

        private static (double, Vec) GradientQ3(Func<DualQ3[], Dual> f, Q3[] point)
        {
            int n = point.Length;
            var partials = Vec.Rent(3 * n);
            var ans = new Dual(0);

            DualQ3[] duals = RentDualQ3Array(n);
            for (int j = 0; j < n; j++)
                duals[j] = new DualQ3(point[j], Q3.zero);

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 4; k++)
                {
                    duals[i] = new DualQ3(point[i], k switch { 0 => Q3.xaxis, 1 => Q3.yaxis, 2 => Q3.zaxis, _ => Q3.waxis });

                    ans = f(duals);
                    partials[i * 3 + k] = ans.D;

                    duals[i] = new DualQ3(point[i], Q3.zero);
                }
            }

            return (ans.M, partials);
        }

        public static (V3, Vec partialX, Vec partialY, Vec partialZ) JacobianV3(Func<DualV3[], DualV3> f, V3[] point)
        {
            int n = point.Length;
            var partialX = Vec.Rent(3 * n);
            var partialY = Vec.Rent(3 * n);
            var partialZ = Vec.Rent(3 * n);
            var ans = new DualV3(V3.zero, V3.zero);

            DualV3[] duals = RentDualV3Array(n);
            for (int j = 0; j < n; j++)
                duals[j] = new DualV3(point[j], V3.zero);

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    duals[i] = new DualV3(point[i], k switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis });

                    ans = f(duals);
                    partialX[i * 3 + k] = ans.D.x;
                    partialY[i * 3 + k] = ans.D.y;
                    partialZ[i * 3 + k] = ans.D.z;

                    duals[i] = new DualV3(point[i], V3.zero);
                }
            }

            return (ans.M, partialX, partialY, partialZ);
        }

        public static (Q3, Vec partialX, Vec partialY, Vec partialZ, Vec partialW) JacobianQ3(Func<DualQ3[], DualQ3> f, Q3[] point)
        {
            int n = point.Length;
            var partialX = Vec.Rent(3 * n);
            var partialY = Vec.Rent(3 * n);
            var partialZ = Vec.Rent(3 * n);
            var partialW = Vec.Rent(3 * n);
            var ans = new DualQ3(Q3.zero, Q3.zero);

            DualQ3[] duals = RentDualQ3Array(n);
            for (int j = 0; j < n; j++)
                duals[j] = new DualQ3(point[j], Q3.zero);

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    duals[i] = new DualQ3(point[i], k switch { 0 => Q3.xaxis, 1 => Q3.yaxis, 2 => Q3.zaxis, _ => Q3.waxis });

                    ans = f(duals);
                    partialX[i * 3 + k] = ans.D.x;
                    partialY[i * 3 + k] = ans.D.y;
                    partialZ[i * 3 + k] = ans.D.z;
                    partialW[i * 3 + k] = ans.D.w;

                    duals[i] = new DualQ3(point[i], Q3.zero);
                }
            }

            return (ans.M, partialX, partialY, partialZ, partialW);
        }

        private static readonly ThreadLocal<Dictionary<int, double>> _elementDictionary = new ThreadLocal<Dictionary<int, double>>(() => new Dictionary<int, double>());
        private static readonly ThreadLocal<List<int>> _elementKeys = new ThreadLocal<List<int>>(() => new List<int>());

        private static void AppendSortedRow(alglib.sparsematrix j, Dictionary<int, double> elements)
        {
            List<int> keys = _elementKeys.Value;

            keys.Clear();
            foreach (KeyValuePair<int, double> kv in elements)
                keys.Add(kv.Key);
            keys.Sort();

            alglib.sparseappendemptyrow(j);
            foreach (int element in keys)
            {
                double v = elements[element];
                if (v != 0)
                    alglib.sparseappendelement(j, element, v);
            }
        }

        public static int ApplyScalarConstraint(double[] f, alglib.sparsematrix j, int ci, Func<Dual[], Dual> g, double[] p, int[] idx)
        {
            (double value, Vec partials) = Gradient(g, p);

            int n = p.Length;

            Dictionary<int, double> elements = _elementDictionary.Value;

            elements.Clear();

            for (int i = 0; i < n; i++)
                elements[idx[i]] = partials[i];

            f[ci++] = value;
            AppendSortedRow(j, elements);

            partials.Dispose();

            return ci;
        }

        public static int ApplyScalarConstraintV3(double[] f, alglib.sparsematrix j, int ci, Func<DualV3[], Dual> g, V3[] p, (int, int, int)[] idx)
        {
            (double value, Vec partials) = GradientV3(g, p);

            int n = p.Length;

            Dictionary<int, double> elements = _elementDictionary.Value;
            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partials[3 * i];
                elements[idx[i].Item2] = partials[3 * i + 1];
                elements[idx[i].Item3] = partials[3 * i + 2];
            }

            f[ci++] = value;
            AppendSortedRow(j, elements);

            partials.Dispose();

            return ci;
        }

        public static int ApplyScalarConstraintQ3(double[] f, alglib.sparsematrix j, int ci, Func<DualQ3[], Dual> g, Q3[] p, (int, int, int, int)[] idx)
        {
            (double value, Vec partials) = GradientQ3(g, p);

            int n = p.Length;

            Dictionary<int, double> elements = _elementDictionary.Value;
            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partials[3 * i];
                elements[idx[i].Item2] = partials[3 * i + 1];
                elements[idx[i].Item3] = partials[3 * i + 2];
                elements[idx[i].Item4] = partials[3 * i + 3];
            }

            f[ci++] = value;
            AppendSortedRow(j, elements);

            partials.Dispose();

            return ci;
        }

        public static int ApplyVectorConstraintV3(double[] f, alglib.sparsematrix j, int ci, Func<DualV3[], DualV3> g, V3[] p, (int, int, int)[] idx)
        {
            (V3 value, Vec partialX, Vec partialY, Vec partialZ) = JacobianV3(g, p);

            int n = p.Length;

            Dictionary<int, double> elements = _elementDictionary.Value;
            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partialX[3 * i];
                elements[idx[i].Item2] = partialX[3 * i + 1];
                elements[idx[i].Item3] = partialX[3 * i + 2];
            }

            f[ci++] = value.x;
            AppendSortedRow(j, elements);

            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partialY[3 * i];
                elements[idx[i].Item2] = partialY[3 * i + 1];
                elements[idx[i].Item3] = partialY[3 * i + 2];
            }

            f[ci++] = value.y;
            AppendSortedRow(j, elements);

            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partialZ[3 * i];
                elements[idx[i].Item2] = partialZ[3 * i + 1];
                elements[idx[i].Item3] = partialZ[3 * i + 2];
            }

            f[ci++] = value.z;
            AppendSortedRow(j, elements);

            partialX.Dispose();
            partialY.Dispose();
            partialZ.Dispose();

            return ci;
        }

        public static int ApplyVectorConstraintQ3(double[] f, alglib.sparsematrix j, int ci, Func<DualQ3[], DualQ3> g, Q3[] p, (int, int, int, int)[] idx)
        {
            (Q3 value, Vec partialX, Vec partialY, Vec partialZ, Vec partialW) = JacobianQ3(g, p);

            int n = p.Length;

            Dictionary<int, double> elements = _elementDictionary.Value;
            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partialX[3 * i];
                elements[idx[i].Item2] = partialX[3 * i + 1];
                elements[idx[i].Item3] = partialX[3 * i + 2];
                elements[idx[i].Item4] = partialX[3 * i + 3];
            }

            f[ci++] = value.x;
            AppendSortedRow(j, elements);

            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partialY[3 * i];
                elements[idx[i].Item2] = partialY[3 * i + 1];
                elements[idx[i].Item3] = partialY[3 * i + 2];
                elements[idx[i].Item4] = partialY[3 * i + 3];
            }

            f[ci++] = value.y;
            AppendSortedRow(j, elements);

            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partialZ[3 * i];
                elements[idx[i].Item2] = partialZ[3 * i + 1];
                elements[idx[i].Item3] = partialZ[3 * i + 2];
                elements[idx[i].Item4] = partialZ[3 * i + 3];
            }

            f[ci++] = value.z;
            AppendSortedRow(j, elements);

            elements.Clear();

            for (int i = 0; i < n; i++)
            {
                elements[idx[i].Item1] = partialW[3 * i];
                elements[idx[i].Item2] = partialW[3 * i + 1];
                elements[idx[i].Item3] = partialW[3 * i + 2];
                elements[idx[i].Item4] = partialW[3 * i + 3];
            }

            f[ci++] = value.w;
            AppendSortedRow(j, elements);

            partialX.Dispose();
            partialY.Dispose();
            partialZ.Dispose();

            return ci;
        }

        public struct GaussLegendreSegment
        {
            public V3 R0, R1, R2, R3, V0, V1, V2, V3;
            public double M0, M1, M2, M3;
            public Q3 U1, U2;
            public double T1, T2;
            public double Bt;
        }

        public struct GaussLegendreDualPoint
        {
            public DualV3 R, V;
            public Dual M;
            public DualQ3 U;
            public Dual T;
        }

        public struct GaussLegendreIndexes
        {
            public (int, int, int) R0Idx, R1Idx, R2Idx, R3Idx, V0Idx, V1Idx, V2Idx, V3Idx;
            public int M0Idx, M1Idx, M2Idx, M3Idx;
            public (int, int, int, int) U1Idx, U2Idx;
            public int T1Idx, T2Idx;
            public int BtIdx;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public int Index(int k)
            {
                if (k < 24)
                {
                    int bank = k / 12;
                    int axis = k % 12 / 4;
                    int point = k % 4;
                    return bank switch
                    {
                        0 => axis switch
                        {
                            0 => point switch { 0 => R0Idx.Item1, 1 => R1Idx.Item1, 2 => R2Idx.Item1, _ => R3Idx.Item1 },
                            1 => point switch { 0 => R0Idx.Item2, 1 => R1Idx.Item2, 2 => R2Idx.Item2, _ => R3Idx.Item2 },
                            _ => point switch { 0 => R0Idx.Item3, 1 => R1Idx.Item3, 2 => R2Idx.Item3, _ => R3Idx.Item3 }
                        },
                        _ => axis switch
                        {
                            0 => point switch { 0 => V0Idx.Item1, 1 => V1Idx.Item1, 2 => V2Idx.Item1, _ => V3Idx.Item1 },
                            1 => point switch { 0 => V0Idx.Item2, 1 => V1Idx.Item2, 2 => V2Idx.Item2, _ => V3Idx.Item2 },
                            _ => point switch { 0 => V0Idx.Item3, 1 => V1Idx.Item3, 2 => V2Idx.Item3, _ => V3Idx.Item3 }
                        }
                    };
                }

                if (k < 28)
                {
                    int point = k % 4;
                    return point switch { 0 => M0Idx, 1 => M1Idx, 2 => M2Idx, _ => M3Idx };
                }

                if (k < 36)
                {
                    int axis = (k - 28) / 2;
                    int point = k % 2;
                    return axis switch
                    {
                        0 => point switch { 0 => U1Idx.Item1, _ => U2Idx.Item1 },
                        1 => point switch { 0 => U1Idx.Item2, _ => U2Idx.Item2 },
                        2 => point switch { 0 => U1Idx.Item3, _ => U2Idx.Item3 },
                        _ => point switch { 0 => U1Idx.Item4, _ => U2Idx.Item4 }
                    };
                }

                if (k < 38)
                {
                    int point = k % 2;
                    return point switch { 0 => T1Idx, _ => T2Idx };
                }

                return BtIdx;
            }
        }

        public delegate DualV3 DynamicsCallback(ref GaussLegendreDualPoint d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetDual(int k, ref GaussLegendreDualPoint d0, ref GaussLegendreDualPoint d1, ref GaussLegendreDualPoint d2, ref GaussLegendreDualPoint d3, double val)
        {
            if (k < 24)
            {
                int bank = k / 12;
                int axis = k % 12 / 4;
                int point = k % 4;
                V3 vec = val * axis switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis };
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
                                case 2:
                                    d2.R = new DualV3(d2.R.M, vec);
                                    break;
                                default:
                                    d3.R = new DualV3(d3.R.M, vec);
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
                                case 2:
                                    d2.V = new DualV3(d2.V.M, vec);
                                    break;
                                default:
                                    d3.V = new DualV3(d3.V.M, vec);
                                    break;
                            }
                        }
                        break;
                }
            }
            else if (k < 28)
            {
                int point = k % 4;
                switch (point)
                {
                    case 0:
                        d0.M = new Dual(d0.M.M, val);
                        break;
                    case 1:
                        d1.M = new Dual(d1.M.M, val);
                        break;
                    case 2:
                        d2.M = new Dual(d2.M.M, val);
                        break;
                    default:
                        d3.M = new Dual(d3.M.M, val);
                        break;
                }
            }
            else if (k < 36)
            {
                int axis = (k - 28) / 2;
                int point = k % 2;
                Q3 vec = val * axis switch { 0 => Q3.xaxis, 1 => Q3.yaxis, 2 => Q3.zaxis, _ => Q3.waxis };
                switch (point)
                {
                    case 0:
                        d1.U = new DualQ3(d1.U.M, vec);
                        break;
                    default:
                        d2.U = new DualQ3(d2.U.M, vec);
                        break;
                }
            }
            else if (k < 38)
            {
                int point = k % 2;
                switch (point)
                {
                    case 0:
                        d1.T = new Dual(d1.T.M, val);
                        break;
                    default:
                        d2.T = new Dual(d2.T.M, val);
                        break;
                }
            }
        }

        // 0 = d0.Rx, 1 = d1.Rx, 2 = d2.Rx, 3 = d3.Rx
        // 4 = d0.Ry, 5 = d1.Ry, 6 = d2.Ry, 7 = d3.Ry
        // 8 = d0.Rz, 9 = d1.Rz, 10 = d2.Rz, 11 = d3.Rz
        // 12 = d0.Vx, 13 = d1.Vx, 14 = d2.Vx, 15 = d3.Vx
        // 16 = d0.Vy, 17 = d1.Vy, 18 = d2.Vy, 19 = d3.Vy
        // 20 = d0.Vz, 21 = d1.Vz, 22 = d2.Vz, 23 = d3.Vz
        // 24 = d0.M, 25 = d1.M, 26 = d2.M, 27 = d3.M
        // 28 = d1.Ux, 29 = d2.Ux
        // 30 = d1.Uy, 31 = d2.Uy
        // 32 = d1.Uz, 33 = d2.Uz
        // 34 = d1.Uw, 35 = d2.Uw
        // 36 = d1.T, 37 = d2.T
        // 38 = Bt

        public static int ApplyQAlphaConstraint(double[] f, alglib.sparsematrix j, int ci, Func<DualV3, DualV3, DualQ3, Dual> g, V3 r, V3 v, Q3 u, (int, int, int) ridx, (int, int, int) vidx, (int, int, int, int) uidx)
        {
            var rd = new DualV3(r, V3.zero);
            var vd = new DualV3(v, V3.zero);
            var ud = new DualQ3(u, Q3.zero);

            alglib.sparseappendemptyrow(j);

            rd = new DualV3(rd.M, V3.xaxis);
            Dual ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, ridx.Item1, ans.D);
            rd = new DualV3(rd.M, V3.yaxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, ridx.Item2, ans.D);
            rd = new DualV3(rd.M, V3.zaxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, ridx.Item3, ans.D);
            rd = new DualV3(rd.M, V3.zero);

            vd = new DualV3(vd.M, V3.xaxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, vidx.Item1, ans.D);
            vd = new DualV3(vd.M, V3.yaxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, vidx.Item2, ans.D);
            vd = new DualV3(vd.M, V3.zaxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, vidx.Item3, ans.D);
            vd = new DualV3(vd.M, V3.zero);

            ud = new DualQ3(ud.M, Q3.xaxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, uidx.Item1, ans.D);
            ud = new DualQ3(ud.M, Q3.yaxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, uidx.Item2, ans.D);
            ud = new DualQ3(ud.M, Q3.zaxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, uidx.Item3, ans.D);
            ud = new DualQ3(ud.M, Q3.waxis);
            ans = g(rd, vd, ud);
            alglib.sparseappendelement(j, uidx.Item4, ans.D);

            f[ci++] = ans.M;

            return ci;
        }

        public static int ApplyMDotDynamics(double[] f, alglib.sparsematrix j, int ci, double mdot, GaussLegendreSegment segment, GaussLegendreIndexes indexes, int n)
        {
            var jac = Vec.Rent(NUM_VARS, true);
            var ans = new Dual();

            var d0 = new GaussLegendreDualPoint { R = segment.R0, V = segment.V0, U = Q3.nan, T = double.NaN, M = segment.M0 };
            var d1 = new GaussLegendreDualPoint { R = segment.R1, V = segment.V1, U = segment.U1, T = segment.T1, M = segment.M1 };
            var d2 = new GaussLegendreDualPoint { R = segment.R2, V = segment.V2, U = segment.U2, T = segment.T2, M = segment.M2 };
            var d3 = new GaussLegendreDualPoint { R = segment.R3, V = segment.V3, U = Q3.nan, T = double.NaN, M = segment.M3 };
            var dbt = new Dual(segment.Bt);

            bool singleControlVariable = indexes.Index(36) == indexes.Index(37);

            int[] idxs1 = { 24, 25, 36, 37, 38 };

            foreach (int k in idxs1)
            {
                if (k < 38)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 1);
                else
                    dbt = new Dual(segment.Bt, 1);

                Dual h = dbt / n;
                Dual h4 = h / 4.0;
                Dual ha = h * (3 - 2 * Math.Sqrt(3)) / 12;

                ans = d0.M - d1.M - mdot * (h4 * d1.T + ha * d2.T);

                if (singleControlVariable && (k == 36 || k == 37))
                    jac[36] += ans.D;
                else
                    jac[k] = ans.D;

                if (k < 38)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 0);
                else
                    dbt = new Dual(segment.Bt);
            }

            f[ci++] = ans.M;

            int lastindex = -1;

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs1)
                if (jac[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jac[k]);
                    lastindex = index;
                }

            jac.Dispose();
            jac = Vec.Rent(NUM_VARS, true);

            int[] idxs2 = { 24, 26, 36, 37, 38};

            foreach (int k in idxs2)
            {
                if (k < 38)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 1);
                else
                    dbt = new Dual(segment.Bt, 1);

                Dual h = dbt / n;
                Dual h4 = h / 4.0;
                Dual hb = h * (3 + 2 * Math.Sqrt(3)) / 12;

                ans = d0.M - d2.M - mdot * (hb * d1.T + h4 * d2.T);

                if (singleControlVariable && (k == 36 || k == 37))
                    jac[36] += ans.D;
                else
                    jac[k] = ans.D;

                if (k < 38)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 0);
                else
                    dbt = new Dual(segment.Bt);
            }

            f[ci++] = ans.M;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs2)
                if (jac[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jac[k]);
                    lastindex = index;
                }

            jac.Dispose();
            jac = Vec.Rent(NUM_VARS, true);

            int[] idxs3 = { 24, 27, 36, 37, 38 };

            foreach (int k in idxs3)
            {
                if (k < 38)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 1);
                else
                    dbt = new Dual(segment.Bt, 1);

                Dual h = dbt / n;
                Dual h2 = h / 2.0;

                ans = d0.M - d3.M - mdot * (h2 * d1.T + h2 * d2.T);

                if (singleControlVariable && (k == 36 || k == 37))
                    jac[36] += ans.D;
                else
                    jac[k] = ans.D;

                if (k < 38)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 0);
                else
                    dbt = new Dual(segment.Bt);
            }

            f[ci++] = ans.M;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs3)
                if (jac[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jac[k]);
                    lastindex = index;
                }

            jac.Dispose();

            return ci;
        }

        private const int NUM_VARS = 39;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ApplyGaussLegendreDynamics(double[] f, alglib.sparsematrix j, int ci, DynamicsCallback vDot, GaussLegendreSegment segment, GaussLegendreIndexes indexes, int n)
        {
            var d0 = new GaussLegendreDualPoint { R = segment.R0, V = segment.V0, U = Q3.nan, T = double.NaN, M = segment.M0 };
            var d1 = new GaussLegendreDualPoint { R = segment.R1, V = segment.V1, U = segment.U1, T = segment.T1, M = segment.M1 };
            var d2 = new GaussLegendreDualPoint { R = segment.R2, V = segment.V2, U = segment.U2, T = segment.T2, M = segment.M2 };
            var d3 = new GaussLegendreDualPoint { R = segment.R3, V = segment.V3, U = Q3.nan, T = double.NaN, M = segment.M3 };
            var dbt = new Dual(segment.Bt);

            ci = RDotEquation1(f, j, ci, segment, indexes, n, dbt, d0, d1, d2);
            ci = RDotEquation2(f, j, ci, segment, indexes, n, dbt, d0, d1, d2);
            ci = RDotEquation3(f, j, ci, segment, indexes, n, dbt, d0, d1, d2, d3);

            ci = VDotEquation1(f, j, ci, vDot, segment, indexes, n, dbt, d0, d1, d2, d3);
            ci = VDotEquation2(f, j, ci, vDot, segment, indexes, n, dbt, d0, d1, d2, d3);
            ci = VDotEquation3(f, j, ci, vDot, segment, indexes, n, dbt, d0, d1, d2, d3);

            return ci;
        }

        private static int VDotEquation1(double[] f, alglib.sparsematrix j, int ci, DynamicsCallback vDot,
            GaussLegendreSegment segment, GaussLegendreIndexes indexes, int n, Dual dbt,
            GaussLegendreDualPoint d0, GaussLegendreDualPoint d1, GaussLegendreDualPoint d2,
            GaussLegendreDualPoint d3)
        {
            bool singleControlVariable = indexes.Index(36) == indexes.Index(37);

            using var jacX = Vec.Rent(NUM_VARS, true);
            using var jacY = Vec.Rent(NUM_VARS, true);
            using var jacZ = Vec.Rent(NUM_VARS, true);

            var ans = new DualV3(0, 0, 0);

            for (int k = 0; k < NUM_VARS; k++)
            {
                if (k < NUM_VARS-1)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 1);
                else
                    dbt = new Dual(segment.Bt, 1);

                Dual h = dbt / n;
                Dual h4 = h / 4.0;
                Dual ha = h * (3 - 2 * Math.Sqrt(3)) / 12;

                ans = d0.V - d1.V + h4 * vDot(ref d1) + ha * vDot(ref d2);

                if (singleControlVariable && (k == 36 || k == 37))
                {
                    jacX[36] += ans.D.x;
                    jacY[36] += ans.D.y;
                    jacZ[36] += ans.D.z;
                }
                else
                {
                    jacX[k] = ans.D.x;
                    jacY[k] = ans.D.y;
                    jacZ[k] = ans.D.z;
                }

                if (k < NUM_VARS-1)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 0);
                else
                    dbt = new Dual(segment.Bt);
            }

            f[ci++] = ans.M.x;

            int lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacX[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacX[k]);
                    lastindex = index;
                }

            f[ci++] = ans.M.y;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacY[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacY[k]);
                    lastindex = index;
                }

            f[ci++] = ans.M.z;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacZ[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacZ[k]);
                    lastindex = index;
                }

            return ci;
        }

        private static int VDotEquation2(double[] f, alglib.sparsematrix j, int ci, DynamicsCallback vDot,
            GaussLegendreSegment segment, GaussLegendreIndexes indexes, int n, Dual dbt,
            GaussLegendreDualPoint d0, GaussLegendreDualPoint d1, GaussLegendreDualPoint d2,
            GaussLegendreDualPoint d3)
        {
            bool singleControlVariable = indexes.Index(28) == indexes.Index(29);

            using var jacX = Vec.Rent(NUM_VARS, true);
            using var jacY = Vec.Rent(NUM_VARS, true);
            using var jacZ = Vec.Rent(NUM_VARS, true);

            var ans = new DualV3(0, 0, 0);

            for (int k = 0; k < NUM_VARS; k++)
            {
                if (k < NUM_VARS-1)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 1);
                else
                    dbt = new Dual(segment.Bt, 1);

                Dual h = dbt / n;
                Dual h4 = h / 4.0;
                Dual hb = h * (3 + 2 * Math.Sqrt(3)) / 12;

                ans = d0.V - d2.V + hb * vDot(ref d1) + h4 * vDot(ref d2);

                if (singleControlVariable && (k == 36 || k == 37))
                {
                    jacX[36] += ans.D.x;
                    jacY[36] += ans.D.y;
                    jacZ[36] += ans.D.z;
                }
                else
                {
                    jacX[k] = ans.D.x;
                    jacY[k] = ans.D.y;
                    jacZ[k] = ans.D.z;
                }

                if (k < NUM_VARS-1)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 0);
                else
                    dbt = new Dual(segment.Bt);
            }

            f[ci++] = ans.M.x;

            int lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacX[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacX[k]);
                    lastindex = index;
                }

            f[ci++] = ans.M.y;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacY[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacY[k]);
                    lastindex = index;
                }

            f[ci++] = ans.M.z;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacZ[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacZ[k]);
                    lastindex = index;
                }

            return ci;
        }

        private static int VDotEquation3(double[] f, alglib.sparsematrix j, int ci, DynamicsCallback vDot,
            GaussLegendreSegment segment, GaussLegendreIndexes indexes, int n, Dual dbt,
            GaussLegendreDualPoint d0, GaussLegendreDualPoint d1, GaussLegendreDualPoint d2,
            GaussLegendreDualPoint d3)
        {
            bool singleControlVariable = indexes.Index(28) == indexes.Index(29);

            using var jacX = Vec.Rent(NUM_VARS, true);
            using var jacY = Vec.Rent(NUM_VARS, true);
            using var jacZ = Vec.Rent(NUM_VARS, true);

            var ans = new DualV3(0, 0, 0);

            for (int k = 0; k < NUM_VARS; k++)
            {
                if (k < NUM_VARS-1)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 1);
                else
                    dbt = new Dual(segment.Bt, 1);

                Dual h = dbt / n;
                Dual h2 = h / 2.0;

                ans = d0.V - d3.V + h2 * (vDot(ref d1) + vDot(ref d2));

                if (singleControlVariable && (k == 36 || k == 37))
                {
                    jacX[36] += ans.D.x;
                    jacY[36] += ans.D.y;
                    jacZ[36] += ans.D.z;
                }
                else
                {
                    jacX[k] = ans.D.x;
                    jacY[k] = ans.D.y;
                    jacZ[k] = ans.D.z;
                }

                if (k < NUM_VARS-1)
                    SetDual(k, ref d0, ref d1, ref d2, ref d3, 0);
                else
                    dbt = new Dual(segment.Bt);
            }

            f[ci++] = ans.M.x;

            int lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacX[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacX[k]);
                    lastindex = index;
                }

            f[ci++] = ans.M.y;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacY[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacY[k]);
                    lastindex = index;
                }

            f[ci++] = ans.M.z;

            lastindex = -1;

            alglib.sparseappendemptyrow(j);
            for (int k = 0; k < NUM_VARS; k++)
                if (jacZ[k] != 0)
                {
                    int index = indexes.Index(k);
                    if (lastindex == index)
                        continue;
                    alglib.sparseappendelement(j, index, jacZ[k]);
                    lastindex = index;
                }

            return ci;
        }

        private static int RDotEquation1(double[] f, alglib.sparsematrix j, int ci, GaussLegendreSegment segment,
            GaussLegendreIndexes indexes, int n, Dual dbt, GaussLegendreDualPoint d0, GaussLegendreDualPoint d1, GaussLegendreDualPoint d2)
        {
            using var jacX = Vec.Rent(NUM_VARS, true);
            using var jacY = Vec.Rent(NUM_VARS, true);
            using var jacZ = Vec.Rent(NUM_VARS, true);

            Dual h = dbt / n;
            Dual h4 = h / 4.0;
            Dual ha = h * (3 - 2 * Math.Sqrt(3)) / 12;

            d0.R = new DualV3(d0.R.M, V3.one);

            DualV3 ans = d0.R - d1.R + h4 * d1.V + ha * d2.V;

            jacX[0] = ans.x.D;
            jacY[4] = ans.y.D;
            jacZ[8] = ans.z.D;

            d0.R = new DualV3(d0.R.M, V3.zero);
            d1.R = new DualV3(d1.R.M, V3.one);

            ans = d0.R - d1.R + h4 * d1.V + ha * d2.V;

            jacX[1] = ans.x.D;
            jacY[5] = ans.y.D;
            jacZ[9] = ans.z.D;

            d1.R = new DualV3(d1.R.M, V3.zero);
            d1.V = new DualV3(d1.V.M, V3.one);

            ans = d0.R - d1.R + h4 * d1.V + ha * d2.V;

            jacX[13] = ans.x.D;
            jacY[17] = ans.y.D;
            jacZ[21] = ans.z.D;

            d1.V = new DualV3(d1.V.M, V3.zero);
            d2.V = new DualV3(d2.V.M, V3.one);

            ans = d0.R - d1.R + h4 * d1.V + ha * d2.V;

            jacX[14] = ans.x.D;
            jacY[18] = ans.y.D;
            jacZ[22] = ans.z.D;

            d2.V = new DualV3(d2.V.M, V3.zero);
            dbt = new Dual(segment.Bt, 1);

            h = dbt / n;
            h4 = h / 4.0;
            ha = h * (3 - 2 * Math.Sqrt(3)) / 12;

            ans = d0.R - d1.R + h4 * d1.V + ha * d2.V;

            jacX[NUM_VARS-1] = ans.x.D;
            jacY[NUM_VARS-1] = ans.y.D;
            jacZ[NUM_VARS-1] = ans.z.D;

            f[ci++] = ans.M.x;

            int[] idxs1 = { 0, 1, 13, 14, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs1)
                alglib.sparseappendelement(j, indexes.Index(k), jacX[k]);

            f[ci++] = ans.M.y;

            int[] idxs2 = { 4, 5, 17, 18, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs2)
                alglib.sparseappendelement(j, indexes.Index(k), jacY[k]);

            f[ci++] = ans.M.z;

            int[] idxs3 = { 8, 9, 21, 22, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs3)
                alglib.sparseappendelement(j, indexes.Index(k), jacZ[k]);

            return ci;
        }

        private static int RDotEquation2(double[] f, alglib.sparsematrix j, int ci, GaussLegendreSegment segment,
            GaussLegendreIndexes indexes, int n, Dual dbt, GaussLegendreDualPoint d0, GaussLegendreDualPoint d1, GaussLegendreDualPoint d2)
        {
            using var jacX = Vec.Rent(NUM_VARS, true);
            using var jacY = Vec.Rent(NUM_VARS, true);
            using var jacZ = Vec.Rent(NUM_VARS, true);

            Dual h = dbt / n;
            Dual h4 = h / 4.0;
            Dual hb = h * (3 + 2 * Math.Sqrt(3)) / 12;

            d0.R = new DualV3(d0.R.M, V3.one);

            DualV3 ans = d0.R - d2.R + hb * d1.V + h4 * d2.V;

            jacX[0] = ans.x.D;
            jacY[4] = ans.y.D;
            jacZ[8] = ans.z.D;

            d0.R = new DualV3(d0.R.M, V3.zero);
            d2.R = new DualV3(d2.R.M, V3.one);

            ans = d0.R - d2.R + hb * d1.V + h4 * d2.V;

            jacX[2] = ans.x.D;
            jacY[6] = ans.y.D;
            jacZ[10] = ans.z.D;

            d2.R = new DualV3(d2.R.M, V3.zero);
            d1.V = new DualV3(d1.V.M, V3.one);

            ans = d0.R - d2.R + hb * d1.V + h4 * d2.V;

            jacX[13] = ans.x.D;
            jacY[17] = ans.y.D;
            jacZ[21] = ans.z.D;

            d1.V = new DualV3(d1.V.M, V3.zero);
            d2.V = new DualV3(d2.V.M, V3.one);

            ans = d0.R - d2.R + hb * d1.V + h4 * d2.V;

            jacX[14] = ans.x.D;
            jacY[18] = ans.y.D;
            jacZ[22] = ans.z.D;

            d2.V = new DualV3(d2.V.M, V3.zero);
            dbt = new Dual(segment.Bt, 1);

            h = dbt / n;
            h4 = h / 4.0;
            hb = h * (3 + 2 * Math.Sqrt(3)) / 12;

            ans = d0.R - d2.R + hb * d1.V + h4 * d2.V;

            jacX[NUM_VARS-1] = ans.x.D;
            jacY[NUM_VARS-1] = ans.y.D;
            jacZ[NUM_VARS-1] = ans.z.D;

            f[ci++] = ans.M.x;

            int[] idxs1 = { 0, 2, 13, 14, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs1)
                alglib.sparseappendelement(j, indexes.Index(k), jacX[k]);

            f[ci++] = ans.M.y;

            int[] idxs2 = { 4, 6, 17, 18, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs2)
                alglib.sparseappendelement(j, indexes.Index(k), jacY[k]);

            f[ci++] = ans.M.z;

            int[] idxs3 = { 8, 10, 21, 22, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs3)
                alglib.sparseappendelement(j, indexes.Index(k), jacZ[k]);

            return ci;
        }

        private static int RDotEquation3(double[] f, alglib.sparsematrix j, int ci, GaussLegendreSegment segment,
            GaussLegendreIndexes indexes, int n, Dual dbt, GaussLegendreDualPoint d0, GaussLegendreDualPoint d1, GaussLegendreDualPoint d2, GaussLegendreDualPoint d3)
        {
            using var jacX = Vec.Rent(NUM_VARS, true);
            using var jacY = Vec.Rent(NUM_VARS, true);
            using var jacZ = Vec.Rent(NUM_VARS, true);

            Dual h = dbt / n;
            Dual h2 = 0.5 * h;

            d0.R = new DualV3(d0.R.M, V3.one);

            DualV3 ans = d0.R - d3.R + h2 * d1.V + h2 * d2.V;

            jacX[0] = ans.x.D;
            jacY[4] = ans.y.D;
            jacZ[8] = ans.z.D;

            d0.R = new DualV3(d0.R.M, V3.zero);
            d3.R = new DualV3(d3.R.M, V3.one);

            ans = d0.R - d3.R + h2 * (d1.V + d2.V);

            jacX[3] = ans.x.D;
            jacY[7] = ans.y.D;
            jacZ[11] = ans.z.D;

            d3.R = new DualV3(d3.R.M, V3.zero);
            d1.V = new DualV3(d1.V.M, V3.one);

            ans = d0.R - d3.R + h2 * (d1.V + d2.V);

            jacX[13] = ans.x.D;
            jacY[17] = ans.y.D;
            jacZ[21] = ans.z.D;

            d1.V = new DualV3(d1.V.M, V3.zero);
            d2.V = new DualV3(d2.V.M, V3.one);

            ans = d0.R - d3.R + h2 * (d1.V + d2.V);

            jacX[14] = ans.x.D;
            jacY[18] = ans.y.D;
            jacZ[22] = ans.z.D;

            d2.V = new DualV3(d2.V.M, V3.zero);
            dbt = new Dual(segment.Bt, 1);

            h = dbt / n;
            h2 = 0.5 * h;

            ans = d0.R - d3.R + h2 * (d1.V + d2.V);

            jacX[NUM_VARS-1] = ans.x.D;
            jacY[NUM_VARS-1] = ans.y.D;
            jacZ[NUM_VARS-1] = ans.z.D;

            f[ci++] = ans.M.x;

            int[] idxs1 = { 0, 3, 13, 14, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs1)
                alglib.sparseappendelement(j, indexes.Index(k), jacX[k]);

            f[ci++] = ans.M.y;

            int[] idxs2 = { 4, 7, 17, 18, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs2)
                alglib.sparseappendelement(j, indexes.Index(k), jacY[k]);

            f[ci++] = ans.M.z;

            int[] idxs3 = { 8, 11, 21, 22, NUM_VARS-1 };

            alglib.sparseappendemptyrow(j);
            foreach (int k in idxs3)
                alglib.sparseappendelement(j, indexes.Index(k), jacZ[k]);

            return ci;
        }
    }
}
