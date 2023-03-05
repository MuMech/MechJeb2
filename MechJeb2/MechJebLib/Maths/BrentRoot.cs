/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using System;
using MechJebLib.Utils;

namespace MechJebLib.Maths
{
    /// <summary>
    ///     Brent's rootfinding method.
    /// </summary>
    public static class BrentRoot
    {
        private const double EPS = 2.24e-15;

        /// <summary>
        ///     Brent's rootfinding method, bounded search.  Uses secant, inverse quadratic interpolation and bisection.
        /// </summary>
        /// <param name="f">1 dimensional function</param>
        /// <param name="a">minimum bounds</param>
        /// <param name="b">maximum bounds</param>
        /// <param name="o">state object to pass to function</param>
        /// <param name="maxiter">maximum iterations</param>
        /// <param name="rtol">tolerance</param>
        /// <param name="sign">try to return x such that f(x0) has the same sign as this (0 is best match)</param>
        /// <returns>value for which the function evaluates to zero</returns>
        /// <exception cref="ArgumentException">guess does not brack the root</exception>
        public static double Solve(Func<double, object?, double> f, double a, double b, object? o, int maxiter = 100, double rtol = EPS,
            int sign = 0)
        {
            double fa = f(a, o);
            double fb = f(b, o);

            Check.Finite(a);
            Check.Finite(fa);
            Check.Finite(b);
            Check.Finite(fb);

            if (fa == 0)
                return a;

            if (fb == 0)
                return b;

            if (fa * fb > 0)
                throw new ArgumentException("Brent's rootfinding method: guess does not bracket the root");

            if (Math.Abs(fa) < Math.Abs(fb))
            {
                (a, b)   = (b, a);
                (fa, fb) = (fb, fa);
            }

            return InternalSolve(f, a, b, fa, fb, o, maxiter, rtol, sign);
        }

        /// <summary>
        ///     Brent's rootfinding method, unbounded search.  Uses secant, inverse quadratic interpolation and bisection.
        /// </summary>
        /// <param name="f">1 dimensional function</param>
        /// <param name="x">Initial guess</param>
        /// <param name="o">state object to pass to function</param>
        /// <param name="maxiter">maximum iterations</param>
        /// <param name="rtol">tolerance</param>
        /// <param name="sign">try to return x such that f(x0) has the same sign as this (0 is best match)</param>
        /// <returns>value for which the function evaluates to zero</returns>
        public static double Solve(Func<double, object?, double> f, double x, object? o, int maxiter = 100, double rtol = EPS, int sign = 0)
        {
            double a = x;
            double b = x;
            double fa = f(x, o);
            double fb = fa;

            Check.Finite(x);
            Check.Finite(fa);

            if (fa == 0)
                return a;

            // initial guess to expand search for the zero
            double dx = Math.Abs(x / 50);

            // if x is zero use a larger expansion
            if (dx <= 2.24e-15)
                dx = 1 / 50d;

            // expand by sqrt(2) each iteration
            double sqrt2 = Math.Sqrt(2);

            // iterate until we find a range that brackets the root
            while (fa > 0 == fb > 0)
            {
                dx *= sqrt2;
                a  =  x - dx;
                fa =  f(a, o);

                Check.Finite(a);
                Check.Finite(fa);

                if (fa == 0)
                    return a;

                if (fa > 0 != fb > 0)
                    break;

                b  = x + dx;
                fb = f(b, o);

                Check.Finite(b);
                Check.Finite(fb);

                if (fb == 0)
                    return b;
            }

            return InternalSolve(f, a, b, fa, fb, o, maxiter, rtol, sign);
        }

        // This is the actual algorithm
        private static double InternalSolve(Func<double, object?, double> f, double a, double b, double fa, double fb, object? o, int maxiter,
            double rtol, int sign)
        {
            bool maybeOneMore = sign != 0;

            double i = 0;

            // this ensures we always run the first if block first and initialize c,d,e
            double fc = fb;
            double c = double.NaN;
            double d = double.NaN;
            double e = double.NaN;

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            while (fb != 0 && a != b)
            {
                if (fb > 0 == fc > 0)
                {
                    c  = a;
                    fc = fa;
                    d  = b - a;
                    e  = d;
                }

                if (Math.Abs(fc) < Math.Abs(fb))
                {
                    a  = b;
                    b  = c;
                    c  = a;
                    fa = fb;
                    fb = fc;
                    fc = fa;
                }

                double m = 0.5 * (c - b);
                double toler = 2.0 * rtol * Math.Max(Math.Abs(b), 1.0);
                if (Math.Abs(m) <= toler || fb == 0.0)
                {
                    // try one more round to improve fc if fb does not match the sign
                    if (maybeOneMore && Math.Sign(fb) != sign)
                        maybeOneMore = false;
                    else
                        break;
                }

                if (Math.Abs(e) < toler || Math.Abs(fa) <= Math.Abs(fb))
                {
                    // Bisection
                    d = m;
                    e = m;
                }
                else
                {
                    // Interpolation
                    double s = fb / fa;
                    double p;
                    double q;
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (a == c)
                    {
                        // Linear interpolation
                        p = 2.0 * m * s;
                        q = 1.0 - s;
                    }
                    else
                    {
                        // Inverse quadratic interpolation
                        q = fa / fc;
                        double r = fb / fc;
                        p = s * (2.0 * m * q * (q - r) - (b - a) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }

                    if (p > 0)
                        q = -q;
                    else
                        p = -p;

                    // Acceptibility check
                    if (2.0 * p < 3.0 * m * q - Math.Abs(toler * q) && p < Math.Abs(0.5 * e * q))
                    {
                        e = d;
                        d = p / q;
                    }
                    else
                    {
                        d = m;
                        e = m;
                    }
                }

                a  = b;
                fa = fb;

                if (Math.Abs(d) > toler)
                    b += d;
                else if (b > c)
                    b -= toler;
                else
                    b += toler;

                fb = f(b, o);

                Check.Finite(a);
                Check.Finite(fa);

                if (i++ >= maxiter && maxiter > 0)
                    throw new TimeoutException("Brent's rootfinding method: maximum iterations exceeded: " + Math.Abs(a - c));
            }

            if (sign != 0 && Math.Sign(fb) != sign)
                return c;

            return b;
        }
    }
}
