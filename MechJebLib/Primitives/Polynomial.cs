using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using MechJebLib.Utils;
using static System.Math;
using static MechJebLib.Utils.Statics;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace MechJebLib.Primitives
{
    public class Polynomial
    {
        public readonly  double[] Coef;
        private readonly double[] _pi;
        private readonly double[] _sigma;

        public readonly int N;

        public Polynomial(int n)
        {
            Coef   = new double[n + 1];
            N      = n;
            _pi    = new double[n + 1];
            _sigma = new double[n + 1];
        }

        public Polynomial(double[] coef)
        {
            Coef   = coef;
            N      = coef.Length - 1;
            _pi    = new double[N + 1];
            _sigma = new double[N + 1];
        }

        public double this[int index]
        {
            get => Coef[index];
            set => Coef[index] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Eval(double x) => Eval(N, Coef, x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double ConditionNumber(double x) => ConditionNumber(N, Coef, x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (double, double) EvalWithDerivative(double x) => EvalWithDerivative(N, Coef, x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Polynomial Derivative()
        {
            var d = new Polynomial(N - 1);
            Derivative(N, d.Coef, Coef);
            return d;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Polynomial Deflate(double root)
        {
            var p = new Polynomial(N - 1);
            Deflate(N, p.Coef, Coef, root);
            return p;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Polynomial Inflate(double root)
        {
            var p = new Polynomial(N + 1);
            Inflate(N, p.Coef, Coef, root);
            return p;
        }

        public const double DEFAULT_ERROR = 6e-7;

        /// <summary>
        ///     Finds all roots of the polynomial and returns the number of roots found.
        /// </summary>
        /// <param name="roots"></param>
        /// <param name="xError"></param>
        /// <param name="boundError"></param>
        /// <returns></returns>
        public int Roots(double[] roots, double xError = DEFAULT_ERROR, bool boundError = false)
        {
            Check.CanContain(roots, N);
            return PolynomialRoots(N, roots, Coef, xError, boundError);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double MultSign(double v, double sign) => v * (sign < 0 ? -1 : 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDifferentSign(double a, double b) => a < 0 != b < 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LinearRoot(double[] roots, double[] coef)
        {
            Check.CanContain(roots, 1);

            roots[0] = -coef[0] / coef[1];
            return coef[1] != 0 ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LinearRoot(double[] roots, double[] coef, double x0, double x1)
        {
            Check.CanContain(roots, 1);

            if (coef[1] != 0)
            {
                roots[0] = -coef[0] / coef[1];
                return roots[0] >= x0 && roots[0] <= x1 ? 1 : 0;
            }

            roots[0] = (x0 + x1) / 2;
            return coef[0] == 0 ? 1 : 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int QuadraticRoots(IList<double> roots, double[] coef)
        {
            Check.CanContain(roots, 2);

            (double c, double b, double a) = (coef[0], coef[1], coef[2]);
            double delta = b * b - 4 * a * c;

            if (delta > 0)
            {
                double d = Sqrt(delta);
                double q = -0.5 * (b + MultSign(d, b));
                double rv0 = q / a;
                double rv1 = c / q;
                roots[0] = Min(rv0, rv1);
                roots[1] = Max(rv0, rv1);
                return 2;
            }

            if (delta < 0)
                return 0;

            roots[0] = -0.5 * b / a;
            return a != 0 ? 1 : 0;
        }

        private static int QuadraticRoots(double[] roots, double[] coef, double x0, double x1)
        {
            Check.CanContain(roots, 2);

            (double c, double b, double a) = (coef[0], coef[1], coef[2]);
            double delta = b * b - 4 * a * c;

            if (delta > 0)
            {
                double d = Sqrt(delta);
                double q = -0.5 * (b + MultSign(d, b));
                double rv0 = q / a;
                double rv1 = c / q;
                double r0 = Min(rv0, rv1);
                double r1 = Max(rv0, rv1);
                int ri0 = (r0 >= x0) & (r0 <= x1) ? 1 : 0;
                int ri1 = (r1 >= x0) & (r1 <= x1) ? 1 : 0;
                roots[0]   = r0;
                roots[ri0] = r1;
                return ri0 + ri1;
            }

            if (delta < 0)
                return 0;

            roots[0] = -0.5 * b / a;
            return (roots[0] >= x0) & (roots[0] <= x1) ? 1 : 0;
        }

        public static (bool, double[]) QuadraticFirstRoot(double[] coef) => throw new NotImplementedException();

        public static (bool, double[]) QuadraticFirstRoot(double[] coef, double x0, double x1) => throw new NotImplementedException();

        public static bool QuadraticHasRoot(double[] coef) => throw new NotImplementedException();

        public static bool QuadraticHasRoot(double[] coef, double x0, double x1) => throw new NotImplementedException();

        public static int CubicRoots(double[] roots, double[] coef, double xError, bool boundError)
        {
            Check.CanContain(roots, 3);

            if (coef[3] == 0)
                return QuadraticRoots(roots, coef);

            double a = coef[3] * 3;
            double b_2 = coef[2];
            double c = coef[1];

            double[] deriv = { c, 2 * b_2, a, 0 };

            double delta_4 = b_2 * b_2 - a * c;

            if (delta_4 > 0)
            {
                double d_2 = Sqrt(delta_4);
                double q = -(b_2 + MultSign(d_2, b_2));
                double rv0 = q / a;
                double rv1 = c / q;
                double xa = Min(rv0, rv1);
                double xb = Max(rv0, rv1);

                double ya = Eval(3, coef, xa);
                double yb = Eval(3, coef, xb);

                if (!IsDifferentSign(coef[3], ya))
                {
                    roots[0] = NewtonFindOpenMin(3, coef, deriv, xa, ya, xError, boundError);

                    if (!boundError)
                    {
                        if (!IsDifferentSign(ya, yb))
                            return 1;

                        double[] defPoly = new double[4]; // XXX: why is this deflated poly length 4?
                        Deflate(3, defPoly, coef, roots[0]);

                        // FIXME: better array slices somehow without allocation and boxing?
                        var slice = new ArraySegment<double>(roots, 1, roots.Length - 1);
                        return QuadraticRoots(slice, defPoly) + 1;
                    }

                    if (!IsDifferentSign(ya, yb))
                        return 1;

                    roots[1] = NewtonFindClosed(3, coef, deriv, xa, xb, ya, yb, xError, boundError);
                    roots[2] = NewtonFindOpenMax(3, coef, deriv, xb, yb, xError, boundError);
                    return 3;
                }

                roots[0] = NewtonFindOpenMax(3, coef, deriv, xb, yb, xError, boundError);
                return 1;
            }

            double x_inf = -b_2 / a;
            double y_inf = Eval(3, coef, x_inf);

            if (IsDifferentSign(coef[3], y_inf))
            {
                roots[0] = NewtonFindOpenMax(3, coef, deriv, x_inf, y_inf, xError, boundError);
            }
            else
            {
                roots[0] = NewtonFindOpenMin(3, coef, deriv, x_inf, y_inf, xError, boundError);
            }

            return 1;
        }

        public static (int, double[]) CubicRoots(double[] coef, double x0, double x1) => throw new NotImplementedException();

        public static (bool, double[]) CubicFirstRoot(double[] coef) => throw new NotImplementedException();

        public static (bool, double[]) CubicFirstRoot(double[] coef, double x0, double x1) => throw new NotImplementedException();

        public static bool CubicHasRoot(double[] coef) => throw new NotImplementedException();

        public static bool CubicHasRoot(double[] coef, double x0, double x1) => throw new NotImplementedException();

        private static int PolynomialRoots(int n, double[] roots, double[] coef, double xError,
            bool boundError)
        {
            Check.CanContain(roots, n);
            Check.CanContain(coef, n + 1);

            if (n == 1) return LinearRoot(roots, coef);
            if (n == 2) return QuadraticRoots(roots, coef);
            if (n == 3) return CubicRoots(roots, coef, xError, boundError);
            if (coef[n] == 0) return PolynomialRoots(n - 1, roots, coef, xError, boundError);

            double[] deriv = new double[n];
            Derivative(n, deriv, coef);

            double[] derivRoots = new double[n - 1];
            int nd = PolynomialRoots(n - 1, derivRoots, deriv, xError, boundError);
            if ((n & 1) == 1 || ((n & 1) == 0 && nd > 0))
            {
                int nr = 0;
                double xa = derivRoots[0];
                double ya = Eval(n, coef, xa);
                if (IsDifferentSign(coef[n], ya) != ((n & 1) == 1))
                {
                    roots[0] = NewtonFindOpenMin(n, coef, deriv, xa, ya, xError, boundError);
                    nr       = 1;
                }

                for (int i = 1; i < nd; ++i)
                {
                    double xb = derivRoots[i];
                    double yb = Eval(n, coef, xb);
                    if (IsDifferentSign(ya, yb))
                    {
                        roots[nr++] = NewtonFindClosed(n, coef, deriv, xa, xb, ya, yb, xError, boundError);
                    }

                    xa = xb;
                    ya = yb;
                }

                if (IsDifferentSign(coef[n], ya))
                {
                    roots[nr++] = NewtonFindOpenMax(n, coef, deriv, xa, ya, xError, boundError);
                }

                return nr;
            }

            if ((n & 1) == 1)
            {
                roots[0] = NewtonFindOpen(n, coef, deriv, xError, boundError);
                return 1;
            }

            return 0; // this should not happen
        }

        public static (int, double[]) PolynomialRoots(double[] coef, double x0, double x1) => throw new NotImplementedException();

        public static (bool, double[]) PolynomialFirstRoot(double[] coef) => throw new NotImplementedException();

        public static (bool, double[]) PolynomialFirstRoot(double[] coef, double x0, double x1) => throw new NotImplementedException();

        public static bool PolynomialHasRoot(double[] coef) => throw new NotImplementedException();

        public static bool PolynomialHasRoot(double[] coef, double x0, double x1) => throw new NotImplementedException();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double ConditionNumber(int n, double[] coef, double x)
        {
            double r1 = coef[n];
            double r2 = Abs(coef[n]);

            for (int i = n - 1; i >= 0; --i)
            {
                r1 = r1 * x + coef[i];
                r2 = r2 * Abs(x) + Abs(coef[i]);
            }

            return r2 / Abs(r1);
        }

        private static double Eval(int n, double[] coef, double x)
        {
            return Horner(n, coef, x);
            //return CompHorner(n, new double[n + 1], new double[n + 1], coef, x);

            int k = 5;

            // FIXME: lots of allocation
            double[][] p = new double[(1<<k)-1][];
            for (int i = 0; i < (1<<k)-1; i++)
                p[i] = new double[n + 1];
            for (int i = 0; i <= n; i++)
                p[0][i] = coef[i];

            return CompHornerK(n, k, new double[(1<<k)-1], p, x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static (double, double) EvalWithDerivative(int n, double[] coef, double x)
        {
            if (n < 1) { return (coef[0], 0); }

            double p = coef[n] * x + coef[n - 1];
            double dp = coef[n];

            for (int i = n - 2; i >= 0; --i)
            {
                dp = dp * x + p;
                p  = p * x + coef[i];
            }

            return (p, dp);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Derivative(int n, double[] deriv, double[] coef)
        {
            deriv[0] = coef[1];
            for (int i = 2; i <= n; ++i)
                deriv[i - 1] = i * coef[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Deflate(int n, double[] deflate, double[] coef, double root)
        {
            deflate[n - 1] = coef[n];
            for (int i = n - 1; i > 0; --i)
                deflate[i - 1] = coef[i] + root * deflate[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void Inflate(int n, double[] inflate, double[] coef, double root)
        {
            inflate[n + 1] = coef[n];
            for (int i = n - 1; i >= 0; --i)
                inflate[i + 1] = coef[i] - root * coef[i + 1];
            inflate[0] = -root * coef[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double NewtonFindOpen(int n, double[] coef, double[] deriv, double xError, bool boundError)
        {
            Check.CanContain(coef, n + 1);
            Check.CanContain(deriv, n);
            Check.True((n & 1) == 1);
            const double XR = 0;
            double yr = coef[0]; // PolynomialEval<N,double>( coef, xr );
            return IsDifferentSign(coef[n], yr)
                ? NewtonFindOpenMax(n, coef, deriv, XR, yr, xError, boundError)
                : NewtonFindOpenMin(n, coef, deriv, XR, yr, xError, boundError);
        }

        /// <summary>
        ///     Finds the single root from negative infinity to the given x bound `x1`.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double NewtonFindOpenMin(int n, double[] coef, double[] deriv, double x1, double y1, double xError,
            bool boundError)
        {
            Check.CanContain(coef, n + 1);
            Check.CanContain(deriv, n);

            return NewtonFindOpen(n, coef, deriv, x1, y1, x1 - 1, xError, boundError, true);
        }

        /// <summary>
        ///     Finds the single root from the given x bound `x0` to positive infinity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double NewtonFindOpenMax(int n, double[] coef, double[] deriv, double x0, double y0, double xError,
            bool boundError)
        {
            Check.CanContain(coef, n + 1);
            Check.CanContain(deriv, n);

            return NewtonFindOpen(n, coef, deriv, x0, y0, x0 + 1, xError, boundError, false);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double NewtonFindClosed(int n, double[] coef, double[] deriv, double x0, double x1, double y0, double y1,
            double xError, bool boundError)
        {
            Check.CanContain(coef, n + 1);
            Check.CanContain(deriv, n);

            double ep2 = 2 * xError;
            double xr = (x0 + x1) / 2; // mid point
            if (x1 - x0 <= ep2) return xr;

            if (n <= 3)
            {
                double xr0 = xr;
                for (int safetyCounter = 0; safetyCounter < 16; ++safetyCounter)
                {
                    double xn = xr - Eval(n, coef, xr) / Eval(2, deriv, xr);
                    xn = Clamp(xn, x0, x1);
                    if (Abs(xr - xn) <= xError) return xn;
                    xr = xn;
                }

                if (!IsFinite(xr)) xr = xr0;
            }

            double yr = Eval(n, coef, xr);
            double xb0 = x0;
            double xb1 = x1;

            while (true)
            {
                bool side = IsDifferentSign(y0, yr);
                if (side) xb1 = xr;
                else xb0      = xr;
                double dy = Eval(n - 1, deriv, xr);
                double dx = yr / dy;
                double xn = xr - dx;
                if (xn > xb0 && xn < xb1)
                {
                    // valid Newton step
                    double stepsize = Abs(xr - xn);
                    xr = xn;
                    if (stepsize > xError)
                    {
                        yr = Eval(n, coef, xr);
                    }
                    else
                    {
                        if (boundError)
                        {
                            double xs;
                            if (xError == 0)
                            {
                                xs = NextAfter(side ? xb1 : xb0, side ? xb0 : xb1);
                            }
                            else
                            {
                                xs = xn - MultSign(xError, side ? 0 : -1);
                                if (xs == xn) xs = NextAfter(side ? xb1 : xb0, side ? xb0 : xb1);
                            }

                            double ys = Eval(n, coef, xs);
                            bool s = IsDifferentSign(y0, ys);
                            if (side != s) return xn;
                            xr = xs;
                            yr = ys;
                        }
                        else break;
                    }
                }
                else
                {
                    // Newton step failed
                    xr = (xb0 + xb1) / 2;
                    if (xr == xb0 || xr == xb1 || xb1 - xb0 <= ep2)
                    {
                        if (boundError)
                        {
                            if (xError == 0)
                            {
                                double xm = side ? xb0 : xb1;
                                double ym = Eval(n, coef, xm);
                                if (Abs(ym) < Abs(yr)) xr = xm;
                            }
                        }

                        break;
                    }

                    yr = Eval(n, coef, xr);
                }
            }

            return xr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static double NewtonFindOpen(int n, double[] coef, double[] deriv, double xm, double ym, double xr, double xError,
            bool boundError, bool openMin)
        {
            Check.CanContain(coef, n + 1);
            Check.CanContain(deriv, n);

            double delta = 1;
            double yr = Eval(n, coef, xr);

            bool otherside = IsDifferentSign(ym, yr);

            while (yr != 0)
            {
                if (otherside)
                {
                    return openMin
                        ? NewtonFindClosed(n, coef, deriv, xr, xm, yr, ym, xError, boundError)
                        : NewtonFindClosed(n, coef, deriv, xm, xr, ym, yr, xError, boundError);
                }

                open_interval:
                xm = xr;
                ym = yr;
                double dy = Eval(n - 1, deriv, xr);
                double dx = yr / dy;
                double xn = xr - dx;
                double dif = openMin ? xr - xn : xn - xr;
                if (dif <= 0 && IsFinite(xn))
                {
                    // valid Newton step
                    xr = xn;
                    if (dif <= xError)
                    {
                        // we might have converged
                        if (xr == xm) break;
                        double xs = xn - MultSign(xError, openMin ? -1 : 0);
                        double ys = Eval(n, coef, xs);
                        bool s = IsDifferentSign(ym, ys);
                        if (s) break;
                        xr = xs;
                        yr = ys;
                        goto open_interval;
                    }
                }
                else
                {
                    // Newton step failed
                    xr    =  openMin ? xr - delta : xr + delta;
                    delta *= 2;
                }

                yr        = Eval(n, coef, xr);
                otherside = IsDifferentSign(ym, yr);
            }

            return xr;
        }

        private static (double x, double y) TwoSum(double a, double b)
        {
            double x = a + b;
            double z = x - a;
            double y = a - (x - z) + (b - z);
            return (x, y);
        }

        private static (double x, double y) Split(double a)
        {
            double z = a * (1 << (27 + 1));
            double x = z - (z - a);
            double y = a - x;
            return (x, y);
        }

        public static (double x, double y) TwoProd(double a, double b)
        {
            double x = a * b;
            (double ah, double al) = Split(a);
            (double bh, double bl) = Split(b);
            double y = al * bl - (x - ah * bh - al * bh - ah * bl);
            return (x, y);
        }

        public static (double x, double y) ThreeProd(double a, double b, double c)
        {
            double x = a * b * c;
            (double ah, double al) = TwoProd(a, b);
            (double bh, double bl) = Split(c);
            double y = al * bl - (x - ah * bh - al * bh - ah * bl);
            return (x, y);
        }

        private static void VecSum(int n, double[] p)
        {
            for (int i = 1; i < n; i++)
                (p[i], p[i - 1]) = TwoSum(p[i], p[i - 1]);
        }

        private static double SumK(int k, int n, double[] p)
        {
            for (int i = 0; i < k - 1; i++)
                VecSum(n, p);
            double r = 0;
            for (int i = 0; i < n; i++)
                r += p[i];
            return r;
        }

        private static double EFTHorner(int n, double[] p1, double[] p2, double[] coef, double x)
        {
            double r = coef[n];

            for (int i = n - 1; i >= 0; i--)
            {
                double t;
                (t, p1[i]) = TwoProd(r, x);
                (r, p2[i]) = TwoSum(t, coef[i]);
            }

            return r;
        }

        private static void Add(int n, double[] r, double[] a, double[] b)
        {
            for (int i = 0; i < n; i++)
                r[i] = a[i] + b[i];
        }

        private static double Horner(int n, double[] coef, double x)
        {
            double r = coef[n];
            for (int i = n - 1; i >= 0; i--)
                r = r * x + coef[i];
            return r;
        }

        private static double CompHorner(int n, double[] p1, double[] p2, double[] coef, double x)
        {
            double r = EFTHorner(n, p1, p2, coef, x);
            Add(n, p1, p1, p2);
            return r + Horner(n, p1, x);
        }

        private static void EFTHornerK(int i, int k, int n, double[] h, double[][] p, double x)
        {
            if (n == 0)
                return;

            if (k == 1)
            {
                h[i] = Horner(n, p[i], x);
            }
            else
            {
                int left = 2 * i + 1;
                int right = 2 * i + 2;
                h[i] = EFTHorner(n, p[left], p[right], p[i], x);
                EFTHornerK(left, k - 1, n - 1, h, p, x);
                EFTHornerK(right, k - 1, n - 1, h, p, x);
            }
        }

        private static double CompHornerK(int n, int k, double[] h, double[][] p, double x)
        {
            EFTHornerK(0, k, n, h, p, x);
            return SumK(k, 1<<k-1, h);
        }
    }
}
