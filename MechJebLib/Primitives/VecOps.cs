using System;

namespace MechJebLib.Primitives
{
    // BLAS Level-1 operations on dense double[] vectors.
    // Length is passed explicitly (not derived from x.Length) so callers can
    // operate on the logical region of an oversized buffer (e.g. PooledVector).
    //
    // Aliasing rule: for in-place ops (Axpy, Axpby), x and y must not refer to
    // the same array. Same as real BLAS. Caller's problem.
    //
    // Nrm2 uses naive sqrt(dot(x, x)). Inputs are assumed bounded so that
    // Σ x[i]² fits in a double — true for PIPG iterates (bounded by projection
    // radii) and typical ODE state vectors. If you ever need overflow safety,
    // swap in the two-pass scaled version; it's a drop-in.
    public static class VecOps
    {
        public static void Copy(double[] src, double[] dst, int n) => Array.Copy(src, 0, dst, 0, n);

        public static void Fill(double[] x, double value, int n)
        {
            for (int i = 0; i < n; i++) x[i] = value;
        }

        public static void Scal(double a, double[] x, int n)
        {
            for (int i = 0; i < n; i++) x[i] *= a;
        }

        // y ← a·x + y
        public static void Axpy(double a, double[] x, double[] y, int n)
        {
            for (int i = 0; i < n; i++) y[i] += a * x[i];
        }

        // y ← a·x + b·y
        public static void Axpby(double a, double[] x, double b, double[] y, int n)
        {
            for (int i = 0; i < n; i++) y[i] = a * x[i] + b * y[i];
        }

        public static double Dot(double[] x, double[] y, int n)
        {
            double s = 0.0;
            for (int i = 0; i < n; i++) s += x[i] * y[i];
            return s;
        }

        public static double Nrm2(double[] x, int n) => Math.Sqrt(Dot(x, x, n));

        public static double NrmInf(double[] x, int n)
        {
            double m = 0.0;
            for (int i = 0; i < n; i++)
            {
                double a = Math.Abs(x[i]);
                if (a > m) m = a;
            }

            return m;
        }

        // x ← max(x, 0). Used by cone projections (clip ℝ₊).
        public static void MaxZero(double[] x, int n)
        {
            for (int i = 0; i < n; i++)
                if (x[i] < 0.0)
                    x[i] = 0.0;
        }

        // Σ|x[i]|  (BLAS dasum, the L1 norm)
        public static double Asum(double[] x, int n)
        {
            double s = 0.0;
            for (int i = 0; i < n; i++) s += Math.Abs(x[i]);
            return s;
        }

        // Σ x[i]  (signed sum; not in classic BLAS but a common useful primitive)
        public static double Sum(double[] x, int n)
        {
            double s = 0.0;
            for (int i = 0; i < n; i++) s += x[i];
            return s;
        }

        // Index of the element with largest |x[i]| (BLAS idamax — but 0-based here).
        // Returns -1 when n ≤ 0. On ties, returns the first occurrence.
        public static int Iamax(double[] x, int n)
        {
            if (n <= 0) return -1;
            int idx = 0;
            double m = Math.Abs(x[0]);
            for (int i = 1; i < n; i++)
            {
                double a = Math.Abs(x[i]);
                if (a > m)
                {
                    m = a;
                    idx = i;
                }
            }

            return idx;
        }

        // x ↔ y  (BLAS dswap)
        public static void Swap(double[] x, double[] y, int n)
        {
            for (int i = 0; i < n; i++)
            {
                (x[i], y[i]) = (y[i], x[i]);
            }
        }

        // z[i] ← x[i] * y[i]  (Hadamard product).
        // Aliasing z = x or z = y is safe — each iteration reads x[i],y[i] then writes z[i].
        public static void Mul(double[] x, double[] y, double[] z, int n)
        {
            for (int i = 0; i < n; i++) z[i] = x[i] * y[i];
        }

        // z[i] ← x[i] / y[i]  (element-wise divide).
        // Aliasing z = x or z = y is safe.
        // Division by zero follows IEEE 754: ±Inf for nonzero numerator, NaN for 0/0.
        public static void Div(double[] x, double[] y, double[] z, int n)
        {
            for (int i = 0; i < n; i++) z[i] = x[i] / y[i];
        }

        // z[i] ← x[i] + y[i]  (element-wise add).
        // Aliasing z = x or z = y is safe.
        public static void Add(double[] x, double[] y, double[] z, int n)
        {
            for (int i = 0; i < n; i++) z[i] = x[i] + y[i];
        }

        // z[i] ← x[i] - y[i]  (element-wise subtract).
        // Aliasing z = x or z = y is safe.
        public static void Sub(double[] x, double[] y, double[] z, int n)
        {
            for (int i = 0; i < n; i++) z[i] = x[i] - y[i];
        }

        // z[i] ← x[i] + a  (scalar add / shift).
        // Aliasing z = x is safe.
        public static void Shift(double[] x, double a, double[] z, int n)
        {
            for (int i = 0; i < n; i++) z[i] = x[i] + a;
        }

        // y[i] ← Abs(x[i]) (element-wise Abs)
        public static void Abs(double[] x, double[] y, int n)
        {
            for (int i = 0; i < n; i++) y[i] = Math.Abs(x[i]);
        }

        // Apply a Givens (plane) rotation to (x, y):
        //   x[i] ←  c·x[i] + s·y[i]
        //   y[i] ← -s·x[i] + c·y[i]
        // c, s typically come from Rotg. Both vectors are modified in place.
        public static void Rot(double[] x, double[] y, double c, double s, int n)
        {
            for (int i = 0; i < n; i++)
            {
                double xi = x[i];
                double yi = y[i];
                x[i] = c * xi + s * yi;
                y[i] = -s * xi + c * yi;
            }
        }

        // Construct a Givens rotation (c, s) such that
        //   [ c  s] [a]   [r]
        //   [-s  c] [b] = [0]
        // Output r = ±sqrt(a² + b²), sign chosen by netlib's drotg convention
        // (sign of whichever of a, b is larger in magnitude). c² + s² = 1.
        //
        // We don't return BLAS's `z` reconstruction parameter — a, b are inputs
        // by value, and r is delivered as an explicit out.
        public static void Rotg(double a, double b, out double c, out double s, out double r)
        {
            double absA = Math.Abs(a);
            double absB = Math.Abs(b);
            double scale = absA + absB;
            if (scale == 0.0)
            {
                c = 1.0;
                s = 0.0;
                r = 0.0;
                return;
            }

            // sigma = sign(roe) where roe = a if |a| > |b| else b.
            double sigma = absA > absB ? a < 0.0 ? -1.0 : 1.0
                : b < 0.0 ? -1.0 : 1.0;
            double aS = a / scale;
            double bS = b / scale;
            r = sigma * scale * Math.Sqrt(aS * aS + bS * bS);
            c = a / r;
            s = b / r;
        }

        // Fused linear combination:  y[i] ← y0[i] + Σₖ aₖ · xₖ[i]
        //
        // One pass over the data — call site reads like an axpy chain but executes
        // as a single loop. The shape comes from RK k-vector mixing, e.g.
        //   Ynew = Y + h·(A51·K1 + A52·K2 + A53·K3 + A54·K4)
        // is LinComb4(Ynew, Y, h·A51, K1, h·A52, K2, h·A53, K3, h·A54, K4, N).
        //
        // Aliasing y = y0 (in-place update) and y = any xₖ are both safe — each
        // element is read once and written once per iteration.
        public static void LinComb1(double[] y, double[] y0,
            double a1, double[] x1, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i];
        }

        public static void LinComb2(double[] y, double[] y0,
            double a1, double[] x1, double a2, double[] x2, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i] + a2 * x2[i];
        }

        public static void LinComb3(double[] y, double[] y0,
            double a1, double[] x1, double a2, double[] x2,
            double a3, double[] x3, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i] + a2 * x2[i] + a3 * x3[i];
        }

        public static void LinComb4(double[] y, double[] y0,
            double a1, double[] x1, double a2, double[] x2,
            double a3, double[] x3, double a4, double[] x4, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i] + a2 * x2[i] + a3 * x3[i] + a4 * x4[i];
        }

        public static void LinComb5(double[] y, double[] y0,
            double a1, double[] x1, double a2, double[] x2,
            double a3, double[] x3, double a4, double[] x4,
            double a5, double[] x5, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i] + a2 * x2[i] + a3 * x3[i]
                    + a4 * x4[i] + a5 * x5[i];
        }

        public static void LinComb6(double[] y, double[] y0,
            double a1, double[] x1, double a2, double[] x2,
            double a3, double[] x3, double a4, double[] x4,
            double a5, double[] x5, double a6, double[] x6, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i] + a2 * x2[i] + a3 * x3[i]
                    + a4 * x4[i] + a5 * x5[i] + a6 * x6[i];
        }

        public static void LinComb7(double[] y, double[] y0,
            double a1, double[] x1, double a2, double[] x2,
            double a3, double[] x3, double a4, double[] x4,
            double a5, double[] x5, double a6, double[] x6,
            double a7, double[] x7, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i] + a2 * x2[i] + a3 * x3[i]
                    + a4 * x4[i] + a5 * x5[i] + a6 * x6[i]
                    + a7 * x7[i];
        }

        public static void LinComb8(double[] y, double[] y0,
            double a1, double[] x1, double a2, double[] x2,
            double a3, double[] x3, double a4, double[] x4,
            double a5, double[] x5, double a6, double[] x6,
            double a7, double[] x7, double a8, double[] x8, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i] + a2 * x2[i] + a3 * x3[i]
                    + a4 * x4[i] + a5 * x5[i] + a6 * x6[i]
                    + a7 * x7[i] + a8 * x8[i];
        }

        public static void LinComb9(double[] y, double[] y0,
            double a1, double[] x1, double a2, double[] x2,
            double a3, double[] x3, double a4, double[] x4,
            double a5, double[] x5, double a6, double[] x6,
            double a7, double[] x7, double a8, double[] x8,
            double a9, double[] x9, int n)
        {
            for (int i = 0; i < n; i++)
                y[i] = y0[i] + a1 * x1[i] + a2 * x2[i] + a3 * x3[i]
                    + a4 * x4[i] + a5 * x5[i] + a6 * x6[i]
                    + a7 * x7[i] + a8 * x8[i] + a9 * x9[i];
        }

        // Cubic Hermite interpolant. Evaluates the cubic polynomial through
        // (x1, y1) with slope yp1 and (x2, y2) with slope yp2, at the point x.
        //   y[i] ← h00·y1[i] + (h10·dx)·yp1[i] + h01·y2[i] + (h11·dx)·yp2[i]
        // Aliasing y with any input is safe.
        public static void CubicHermiteInterpolant(double x1, double[] y1, double[] yp1,
            double x2, double[] y2, double[] yp2, double x, double[] y, int n)
        {
            double dx = x2 - x1;
            double t = (x - x1) / dx;
            double t2 = t * t;
            double t3 = t2 * t;
            double h00 = 2 * t3 - 3 * t2 + 1;
            double h10dx = (t3 - 2 * t2 + t) * dx;
            double h01 = -2 * t3 + 3 * t2;
            double h11dx = (t3 - t2) * dx;
            for (int i = 0; i < n; i++)
                y[i] = h00 * y1[i] + h10dx * yp1[i] + h01 * y2[i] + h11dx * yp2[i];
        }
    }
}
