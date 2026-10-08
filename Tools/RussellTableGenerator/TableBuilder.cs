/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MechJebLib.Lambert;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace RussellTableGenerator
{
    /// <summary>
    ///     Builds the interpolation tables for the initial guesses of Russell's Lambert solver, following Russell (2022),
    ///     Section II.D: each table is fit by polynomials of a fixed total order over the bins of a binary tree, splitting
    ///     bins in half along a fixed per-level pattern of dimensions until the maximum error in each bin is within
    ///     tolerance.
    /// </summary>
    internal static class TableBuilder
    {
        private const string COMMAND = "dotnet run -c Release --project Tools/RussellTableGenerator";

        // Russell (2022), Section II.D.4: total order, tolerance and split pattern (0 = x, 1 = y, 2 = z) of each table.  The
        // split patterns of the paper are extended by two levels (y, x for zero-rev and y, y for multi-rev), which are only
        // used by a few bins that don't meet the tolerance otherwise.
        private const           int    ZERO_REV_ORDER   = 8;
        private const           double ZERO_REV_TOL     = 1e-3;
        private static readonly byte[] _zeroRevPattern  = { 0, 0, 1, 0, 1, 0, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0, 1, 0 };
        private const           int    MULTI_REV_ORDER  = 5;
        private const           double MULTI_REV_TOL    = 2e-4;
        private static readonly byte[] _multiRevPattern = { 1, 2, 0, 0, 1, 1, 0, 1, 0, 2, 1, 0, 0, 2, 0, 1, 1 };

        // The k-bottom table over x and z (not from the paper, which interpolates the minimum time of flight instead).  The
        // tolerance is relative to the distance of kBottom from +-sqrt(2), and is enough for the Newton iteration for
        // kBottom to converge in at most two steps.
        private const           int    K_BOTTOM_ORDER   = 7;
        private const           double K_BOTTOM_TOL     = 1e-6;
        private static readonly byte[] _kBottomPattern  = { 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 0, 1 };

        // sampling of each bin: fit nodes per continuous dimension (and maximum distinct N), validation points per
        // continuous dimension (and maximum distinct N)
        private const int ZERO_REV_FIT       = 2 * (ZERO_REV_ORDER + 1);
        private const int ZERO_REV_VALIDATE  = 4 * (ZERO_REV_ORDER + 1) + 1;
        private const int MULTI_REV_FIT      = 2 * (MULTI_REV_ORDER + 1);
        private const int MULTI_REV_VALIDATE = 4 * (MULTI_REV_ORDER + 1) + 1;
        private const int MULTI_REV_FIT_N    = 12;
        private const int MULTI_REV_VALID_N  = 24;
        private const int K_BOTTOM_FIT       = 2 * (K_BOTTOM_ORDER + 1);
        private const int K_BOTTOM_VALIDATE  = 4 * (K_BOTTOM_ORDER + 1) + 1;
        private const int K_BOTTOM_FIT_N     = 2 * (K_BOTTOM_ORDER + 1);
        private const int K_BOTTOM_VALID_N   = 4 * (K_BOTTOM_ORDER + 1);

        private sealed class Bin
        {
            public readonly double[] Lo;
            public readonly double[] Hi;
            public readonly int      Depth;
            public          Bin?     Child0;
            public          Bin?     Child1;
            public          double[] Coefs = Array.Empty<double>();
            public          double   MaxError;
            public          double   MaxTargetError;

            public Bin(double[] lo, double[] hi, int depth)
            {
                Lo    = lo;
                Hi    = hi;
                Depth = depth;
            }

            public bool IsLeaf => Child0 == null;
        }

        private sealed class Result
        {
            public string   Name = string.Empty;
            public int      Dims;
            public int      Order;
            public double   Tolerance;
            public byte[]   Pattern = Array.Empty<byte>();
            public double[] Lo      = Array.Empty<double>();
            public double[] Hi      = Array.Empty<double>();
            public int[]    Tree    = Array.Empty<int>();
            public double[] Coefs   = Array.Empty<double>();
            public int      Bins;
            public int      Failed;
            public double   MaxError;
        }

        /// <summary>
        ///     The source of RussellGuessTables.cs.
        /// </summary>
        public static string Generate()
        {
            Result zeroRev = Build("zero-rev", 2, ZERO_REV_ORDER, ZERO_REV_TOL, _zeroRevPattern, new[] { -1.0, 0.0 }, new[] { 1.0, 1.0 }, FitZeroRev);
            Result multiRev = Build("multi-rev", 3, MULTI_REV_ORDER, MULTI_REV_TOL, _multiRevPattern, new[] { -1.0, -1.0, 0.0 },
                new[] { 1.0, 1.0, Log(RussellGuess.N_MAX) }, FitMultiRev);
            Result kBottom = Build("k-bottom", 2, K_BOTTOM_ORDER, K_BOTTOM_TOL, _kBottomPattern, new[] { -1.0, 0.0 }, new[] { 1.0, Log(RussellGuess.N_MAX) },
                FitKBottom);

            return Emit(zeroRev, multiRev, kBottom);
        }

        /// <summary>
        ///     Builds the tree breadth-first, fitting every bin of each level in parallel.
        /// </summary>
        private static Result Build(string name, int dims, int order, double tol, byte[] pattern, double[] lo, double[] hi, Action<Bin, int> fit)
        {
            var root = new Bin(lo, hi, 0);
            var level = new List<Bin> { root };
            int failed = 0;
            int fits = 0;

            while (level.Count > 0)
            {
                Parallel.ForEach(level, bin => fit(bin, order));
                fits += level.Count;

                var next = new List<Bin>();
                foreach (Bin bin in level)
                {
                    if (bin.MaxError <= tol)
                        continue;

                    if (bin.Depth >= pattern.Length)
                    {
                        failed++;
                        Console.WriteLine(
                            $"{name}: bin at maximum depth fails tolerance, error {bin.MaxError:E3} lo {string.Join(",", bin.Lo)} hi {string.Join(",", bin.Hi)}");
                        continue;
                    }

                    int d = pattern[bin.Depth];
                    double mid = 0.5 * (bin.Lo[d] + bin.Hi[d]);
                    double[] hi0 = (double[])bin.Hi.Clone();
                    double[] lo1 = (double[])bin.Lo.Clone();
                    hi0[d] = mid;
                    lo1[d] = mid;
                    bin.Child0 = new Bin(bin.Lo, hi0, bin.Depth + 1);
                    bin.Child1 = new Bin(lo1, bin.Hi, bin.Depth + 1);
                    next.Add(bin.Child0);
                    next.Add(bin.Child1);
                }

                level = next;
            }

            // flatten breadth-first: the children of an internal node are allocated consecutively
            var nodes = new List<Bin> { root };
            var tree = new List<int> { 0 };
            var coefs = new List<double>();
            int bins = 0;
            double maxError = 0;
            double maxTargetError = 0;

            for (int j = 0; j < nodes.Count; j++)
            {
                Bin bin = nodes[j];
                if (bin.IsLeaf)
                {
                    tree[j] = ~bins++;
                    coefs.AddRange(bin.Coefs);
                    maxError       = Max(maxError, bin.MaxError);
                    maxTargetError = Max(maxTargetError, bin.MaxTargetError);
                }
                else
                {
                    tree[j] = nodes.Count;
                    nodes.Add(bin.Child0!);
                    nodes.Add(bin.Child1!);
                    tree.Add(0);
                    tree.Add(0);
                }
            }

            string targetError = maxTargetError > 0 ? $", max error of the fit target {maxTargetError:E3}" : "";
            Console.WriteLine($"{name}: {bins} bins, {fits} fits, {coefs.Count} coefficients, max error {maxError:E3}{targetError}, {failed} bins fail");

            return new Result
            {
                Name      = name,
                Dims      = dims,
                Order     = order,
                Tolerance = tol,
                Pattern   = pattern,
                Lo        = lo,
                Hi        = hi,
                Tree      = tree.ToArray(),
                Coefs     = coefs.ToArray(),
                Bins      = bins,
                Failed    = failed,
                MaxError  = maxError
            };
        }

        // Chebyshev-Lobatto nodes on [-1, 1]
        private static double[] Lobatto(int n) => Enumerable.Range(0, n).Select(i => -Cos(PI * i / (n - 1))).ToArray();

        private static double[] Uniform(int n) => Enumerable.Range(0, n).Select(i => -1.0 + 2.0 * i / (n - 1)).ToArray();

        private static double FromLocal(double u, double lo, double hi) => lo + 0.5 * (u + 1.0) * (hi - lo);

        // the coefficients as they will be read back from the generated source
        private static double RoundTrip(double c) => double.Parse(Format(c), CultureInfo.InvariantCulture);

        private static string Format(double c) => c.ToString("G17", CultureInfo.InvariantCulture);

        /// <summary>
        ///     Weighted least squares fit of the basis functions to the samples.  Columns which are not used get a zero
        ///     coefficient.
        /// </summary>
        private static double[] Fit(List<double[]> basis, List<double> q, List<double> weights, bool[] used)
        {
            int n = q.Count;
            int[] columns = Enumerable.Range(0, used.Length).Where(i => used[i]).ToArray();
            int m = columns.Length;

            var fmatrix = new double[n, m];
            var w = new double[n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < m; j++)
                    fmatrix[i, j] = basis[i][columns[j]];
                w[i] = weights[i];
            }

            alglib.lsfitlinearw(q.ToArray(), w, fmatrix, n, m, out double[] c, out alglib.lsfitreport rep);
            if (rep.terminationtype <= 0)
                throw new Exception($"least squares fit failed: {rep.terminationtype}");

            var coefs = new double[used.Length];
            for (int j = 0; j < m; j++)
                coefs[columns[j]] = RoundTrip(c[j]);

            return coefs;
        }

        private static double[] Basis2(int order, double u, double v)
        {
            var b = new double[(order + 1) * (order + 2) / 2];
            int idx = 0;
            for (int i = 0; i <= order; i++)
            for (int j = 0; j <= order - i; j++)
                b[idx++] = Pow(u, i) * Pow(v, j);
            return b;
        }

        private static double[] Basis3(int order, double u, double v, double w)
        {
            var b = new double[(order + 1) * (order + 2) * (order + 3) / 6];
            int idx = 0;
            for (int i = 0; i <= order; i++)
            for (int j = 0; j <= order - i; j++)
            for (int l = 0; l <= order - i - j; l++)
                b[idx++] = Pow(u, i) * Pow(v, j) * Pow(w, l);
            return b;
        }

        private static void FitZeroRev(Bin bin, int order)
        {
            double ZeroRevK(double u, double v)
            {
                double x = FromLocal(u, bin.Lo[0], bin.Hi[0]);
                double y = FromLocal(v, bin.Lo[1], bin.Hi[1]);
                return RussellTruth.ZeroRevK(RussellGuess.TauFromX(x), RussellGuess.ZeroRevTofbyS(x, y));
            }

            var basis = new List<double[]>();
            var ks = new List<double>();
            var points = new List<(double u, double v)>();

            foreach (double u in Lobatto(ZERO_REV_FIT))
            foreach (double v in Lobatto(ZERO_REV_FIT))
            {
                basis.Add(Basis2(order, u, v));
                ks.Add(ZeroRevK(u, v));
                points.Add((u, v));
            }

            // weighted so that the residual is the error metric
            double[] coefs = Fit(basis, ks, ks.Select(k => 1.0 / Max(Abs(k), 1.0)).ToList(), Enumerable.Repeat(true, basis[0].Length).ToArray());

            foreach (double u in Uniform(ZERO_REV_VALIDATE))
            foreach (double v in Uniform(ZERO_REV_VALIDATE))
            {
                ks.Add(ZeroRevK(u, v));
                points.Add((u, v));
            }

            double maxError = 0;
            for (int i = 0; i < ks.Count; i++)
            {
                double k = RussellGuess.Poly2(coefs, 0, order, points[i].u, points[i].v);
                maxError = Max(maxError, RussellTruth.Error(k, ks[i]));
            }

            bin.Coefs    = coefs;
            bin.MaxError = maxError;
        }

        /// <summary>
        ///     The distinct numbers of revolutions with ln N in [zlo, zhi], at most max of them spread evenly in ln N, including
        ///     the smallest and largest.
        /// </summary>
        private static int[] RevsInBin(double zlo, double zhi, int max)
        {
            int nlo = Max(1, (int)Ceiling(Exp(zlo) * (1 - 1e-12)));
            int nhi = Min(RussellGuess.N_MAX, (int)Floor(Exp(zhi) * (1 + 1e-12)));
            while (nlo > 1 && Log(nlo - 1) >= zlo) nlo--;
            while (Log(nlo) < zlo) nlo++;
            while (nhi < RussellGuess.N_MAX && Log(nhi + 1) <= zhi) nhi++;
            while (nhi > nlo && Log(nhi) > zhi) nhi--;

            if (nhi < nlo)
                return Array.Empty<int>();

            if (nhi - nlo + 1 <= max)
                return Enumerable.Range(nlo, nhi - nlo + 1).ToArray();

            var revs = new SortedSet<int>();
            for (int i = 0; i < max; i++)
                revs.Add((int)Round(Exp(Log(nlo) + (Log(nhi) - Log(nlo)) * i / (max - 1))));
            return revs.ToArray();
        }

        private static void FitMultiRev(Bin bin, int order)
        {
            // the first split is along y = 0, so every bin is on one branch
            if (bin.Lo[1] < 0.0 && bin.Hi[1] > 0.0)
            {
                bin.MaxError = double.PositiveInfinity;
                return;
            }

            int sign = bin.Lo[1] >= 0.0 ? 1 : -1;

            int[] fitRevs = RevsInBin(bin.Lo[2], bin.Hi[2], MULTI_REV_FIT_N);
            int[] checkRevs = RevsInBin(bin.Lo[2], bin.Hi[2], MULTI_REV_VALID_N);

            if (fitRevs.Length == 0)
                throw new Exception($"no revolutions in bin z = [{bin.Lo[2]}, {bin.Hi[2]}]");

            double[] fitNodes = Lobatto(MULTI_REV_FIT);
            double[] checkNodes = Uniform(MULTI_REV_VALIDATE);

            // the bottom of the TOF curve for each x node and number of revolutions
            var bottoms = new Dictionary<(double, int), (double tau, double kBottom, double tofbySBottom)>();

            (double tau, double kBottom, double tofbySBottom) Bottom(double u, int n)
            {
                if (bottoms.TryGetValue((u, n), out (double, double, double) b))
                    return b;
                double tau = RussellGuess.TauFromX(FromLocal(u, bin.Lo[0], bin.Hi[0]));
                (double kBottom, double tofbySBottom) = Russell.MultiRevBottom(tau, n, Russell.KBottomAsymptotic(tau), out _);
                return bottoms[(u, n)] = (tau, kBottom, tofbySBottom);
            }

            double MultiRevK(double u, double v, int n)
            {
                (double tau, double kBottom, double tofbySBottom) = Bottom(u, n);
                double y = FromLocal(v, bin.Lo[1], bin.Hi[1]);
                return RussellTruth.MultiRevK(tau, RussellGuess.MultiRevGamma(y, n), sign * n, kBottom, tofbySBottom);
            }

            double W(int n) => 2.0 * (Log(n) - bin.Lo[2]) / (bin.Hi[2] - bin.Lo[2]) - 1.0;

            var basis = new List<double[]>();
            var ks = new List<double>();
            var points = new List<(double u, double v, int n)>();

            foreach (double u in fitNodes)
            foreach (double v in fitNodes)
            foreach (int n in fitRevs)
            {
                basis.Add(Basis3(order, u, v, W(n)));
                ks.Add(MultiRevK(u, v, n));
                points.Add((u, v, n));
            }

            // with fewer distinct N than the order, the degree in z is limited to what the samples determine
            int maxDegreeZ = Min(order, fitRevs.Length - 1);
            var used = new bool[basis[0].Length];
            int idx = 0;
            for (int i = 0; i <= order; i++)
            for (int j = 0; j <= order - i; j++)
            for (int l = 0; l <= order - i - j; l++)
                used[idx++] = l <= maxDegreeZ;

            double Target(int i) => RussellGuess.MultiRevTarget(ks[i], sign * points[i].n, Bottom(points[i].u, points[i].n).kBottom);

            double[] coefs = Fit(basis, Enumerable.Range(0, ks.Count).Select(Target).ToList(), Enumerable.Repeat(1.0, ks.Count).ToList(), used);

            foreach (double u in checkNodes)
            foreach (double v in checkNodes)
            foreach (int n in checkRevs)
            {
                ks.Add(MultiRevK(u, v, n));
                points.Add((u, v, n));
            }

            double maxError = 0;
            double maxTargetError = 0;
            for (int i = 0; i < ks.Count; i++)
            {
                (double u, double v, int n) = points[i];
                double q = RussellGuess.Poly3(coefs, 0, order, u, v, W(n));
                maxTargetError = Max(maxTargetError, Abs(q - Target(i)));
                maxError = Max(maxError, RussellTruth.Error(RussellGuess.MultiRevFromTarget(q, sign * n, Bottom(u, n).kBottom), ks[i]));
            }

            bin.Coefs          = coefs;
            bin.MaxError       = maxError;
            bin.MaxTargetError = maxTargetError;
        }

        /// <summary>
        ///     The error of a guess for kBottom relative to the distance of kBottom from k = +-sqrt(2), which is what the
        ///     convergence of the Newton iteration for kBottom depends on.
        /// </summary>
        private static double KBottomError(double k, double kBottom) => Abs(k - kBottom) / Min(SQRT2 - Abs(kBottom), 1.0);

        private static void FitKBottom(Bin bin, int order)
        {
            int[] fitRevs = RevsInBin(bin.Lo[1], bin.Hi[1], K_BOTTOM_FIT_N);
            int[] checkRevs = RevsInBin(bin.Lo[1], bin.Hi[1], K_BOTTOM_VALID_N);

            // bins without an integer number of revolutions are never used
            if (fitRevs.Length == 0)
            {
                bin.Coefs    = new double[(order + 1) * (order + 2) / 2];
                bin.MaxError = 0;
                return;
            }

            (double tau, double kBottom, double kBottomInf) Truth(double u, int n)
            {
                double tau = RussellGuess.TauFromX(FromLocal(u, bin.Lo[0], bin.Hi[0]));
                double kBottomInf = Russell.KBottomAsymptotic(tau);
                return (tau, Russell.MultiRevBottom(tau, n, kBottomInf, out _).kBottom, kBottomInf);
            }

            double V(int n) => 2.0 * (Log(n) - bin.Lo[1]) / (bin.Hi[1] - bin.Lo[1]) - 1.0;

            var basis = new List<double[]>();
            var targets = new List<double>();

            foreach (double u in Lobatto(K_BOTTOM_FIT))
            foreach (int n in fitRevs)
            {
                (_, double kBottom, double kBottomInf) = Truth(u, n);
                basis.Add(Basis2(order, u, V(n)));
                targets.Add(RussellGuess.KBottomTarget(kBottom, kBottomInf, n));
            }

            // with fewer distinct N than the order, the degree in z is limited to what the samples determine
            int maxDegreeZ = Min(order, fitRevs.Length - 1);
            var used = new bool[basis[0].Length];
            int idx = 0;
            for (int i = 0; i <= order; i++)
            for (int j = 0; j <= order - i; j++)
                used[idx++] = j <= maxDegreeZ;

            double[] coefs = Fit(basis, targets, Enumerable.Repeat(1.0, targets.Count).ToList(), used);

            double maxError = 0;
            foreach (double u in Lobatto(K_BOTTOM_FIT).Concat(Uniform(K_BOTTOM_VALIDATE)))
            foreach (int n in fitRevs.Concat(checkRevs).Distinct())
            {
                (_, double kBottom, double kBottomInf) = Truth(u, n);
                double guess = RussellGuess.KBottomFromTarget(RussellGuess.Poly2(coefs, 0, order, u, V(n)), kBottomInf, n);
                maxError = Max(maxError, KBottomError(guess, kBottom));
            }

            bin.Coefs    = coefs;
            bin.MaxError = maxError;
        }

        private static void AppendLines(StringBuilder sb, List<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                sb.Append(lines[i]);
                sb.AppendLine(i < lines.Count - 1 && !lines[i].TrimStart().StartsWith("//") ? "," : "");
            }
        }

        private static void EmitTable(StringBuilder sb, Result r, string field)
        {
            const string INDENT = "            ";

            sb.AppendLine($"        // {r.Name}: total order {r.Order}, tolerance {r.Tolerance:G3}, {r.Bins} bins, max error {r.MaxError:E3}");
            sb.AppendLine($"        private static readonly int[] {field}Tree =");
            sb.AppendLine("        {");
            var lines = new List<string>();
            for (int i = 0; i < r.Tree.Length; i += 20)
                lines.Add(INDENT + string.Join(", ", r.Tree.Skip(i).Take(20).Select(t => t.ToString(CultureInfo.InvariantCulture))));
            AppendLines(sb, lines);
            sb.AppendLine("        };");
            sb.AppendLine();

            sb.AppendLine($"        private static readonly double[] {field}Coefs =");
            sb.AppendLine("        {");
            lines.Clear();
            int perBin = r.Coefs.Length / r.Bins;
            for (int b = 0; b < r.Bins; b++)
            {
                lines.Add($"{INDENT}// bin {b}");
                for (int i = 0; i < perBin; i += 5)
                    lines.Add(INDENT + string.Join(", ", r.Coefs.Skip(b * perBin + i).Take(Min(5, perBin - i)).Select(Format)));
            }

            AppendLines(sb, lines);
            sb.AppendLine("        };");
            sb.AppendLine();

            sb.AppendLine($"        private static readonly Table {field} = new Table({r.Dims}, {r.Order},");
            sb.AppendLine($"            new byte[] {{ {string.Join(", ", r.Pattern)} }},");
            sb.AppendLine($"            new double[] {{ {string.Join(", ", r.Lo.Select(Format))} }},");
            sb.AppendLine($"            new double[] {{ {string.Join(", ", r.Hi.Select(Format))} }},");
            sb.AppendLine($"            {field}Tree, {field}Coefs);");
        }

        private static string Emit(Result zeroRev, Result multiRev, Result kBottom)
        {
            var sb = new StringBuilder();
            sb.AppendLine("/*");
            sb.AppendLine(" * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors");
            sb.AppendLine(" * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+");
            sb.AppendLine(" */");
            sb.AppendLine();
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("//     Generated by Tools/RussellTableGenerator, do not edit.  To regenerate run:");
            sb.AppendLine("//");
            sb.AppendLine($"//     {COMMAND}");
            sb.AppendLine("//");
            sb.AppendLine("//     The coefficients are fit to solutions of Lambert's equation computed by the generator, following the");
            sb.AppendLine("//     interpolation scheme of Russell (2022), Section II.D.  The zero-rev table fits k and the multi-rev table");
            sb.AppendLine("//     fits RussellGuess.MultiRevTarget(k), where the max error is the error metric for k of Russell (2022),");
            sb.AppendLine("//     Eq. 28.  The k-bottom table fits RussellGuess.KBottomTarget(kBottom), where the max error is relative to");
            sb.AppendLine("//     the distance of kBottom from +-sqrt(2).  The max errors are over the validation points of each bin.");
            sb.AppendLine("//");
            foreach (Result r in new[] { zeroRev, multiRev, kBottom })
            {
                sb.AppendLine($"//     {r.Name}: {r.Dims}D, total order {r.Order}, tolerance {r.Tolerance:G3}, split pattern {string.Join(",", r.Pattern)},");
                sb.AppendLine($"//         {r.Bins} bins, {r.Coefs.Length} coefficients, max error {r.MaxError:E3}, {r.Failed} bins over tolerance");
            }

            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
            sb.AppendLine("// ReSharper disable all");
            sb.AppendLine("namespace MechJebLib.Lambert");
            sb.AppendLine("{");
            sb.AppendLine("    internal static partial class RussellGuess");
            sb.AppendLine("    {");
            EmitTable(sb, zeroRev, "_zeroRev");
            sb.AppendLine();
            EmitTable(sb, multiRev, "_multiRev");
            sb.AppendLine();
            EmitTable(sb, kBottom, "_kBottom");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
