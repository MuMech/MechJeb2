using System;
using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.Utils
{
    public static class AutoDiff
    {
        public static (double, double[]) Gradient(Func<Dual[], Dual> f, double[] point)
        {
            int      n        = point.Length;
            double[] partials = new double[n]; // GARBAGE
            var      ans      = new Dual(0);

            for (int i = 0; i < n; i++)
            {
                var duals = new Dual[n]; // GARBAGE
                for (int j = 0; j < n; j++)
                    duals[j] = new Dual(point[j], i == j ? 1.0 : 0.0);

                ans         = f(duals);
                partials[i] = ans.D;
            }

            return (ans.M, partials);
        }

        public static (double, double[]) GradientV3(Func<DualV3[], Dual> f, V3[] point)
        {
            int      n        = point.Length;
            double[] partials = new double[3 * n]; // GARBAGE
            var      ans      = new Dual(0);

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var duals = new DualV3[n]; // GARBAGE
                    for (int j = 0; j < n; j++)
                        duals[j] = new DualV3(point[j], V3.zero); // GARBAGE

                    duals[i] = new DualV3(point[i], k switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis }); // GARBAGE

                    ans                 = f(duals);
                    partials[i * 3 + k] = ans.D;
                }
            }

            return (ans.M, partials);
        }

        public static (V3, V3[]) JacobianV3(Func<DualV3[], DualV3> f, V3[] point)
        {
            int n        = point.Length;
            var partials = new V3[3 * n]; // GARBAGE
            var ans      = new DualV3(V3.zero, V3.zero);

            for (int i = 0; i < n; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    var duals = new DualV3[n]; // GARBAGE
                    for (int j = 0; j < n; j++)
                        duals[j] = new DualV3(point[j], V3.zero); // GARBAGE

                    duals[i] = new DualV3(point[i], k switch { 0 => V3.xaxis, 1 => V3.yaxis, _ => V3.zaxis }); // GARBAGE

                    ans                 = f(duals);
                    partials[i * 3 + k] = ans.D;
                }
            }

            return (ans.M, partials);
        }

        public static int ApplyScalarConstraint(double[] f, alglib.sparsematrix j, int ci, Func<Dual[], Dual> g, double[] p, int[] idx)
        {
            (double value, double[] partials) = Gradient(g, p);

            int n        = p.Length;

            var elements = new SortedDictionary<int, double>();
            for (int i = 0; i < n; i++)
                elements[idx[i]] = partials[i];

            f[ci++] = value;
            alglib.sparseappendemptyrow(j);
            foreach (KeyValuePair<int, double> kv in elements)
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
                alglib.sparseappendelement(j, kv.Key, kv.Value);

            return ci;
        }
    }
}
