/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using static MechJebLib.Utils.Statics;

#nullable enable

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.Core
{
    public static class BrentMin
    {
        // Brent's 1-dimensional derivative-free local minimization method
        //
        // Uses golden section search and successive parabolic interpolation.
        //
        // f - 1-dimensional BrentFun function
        // xmin - lower endpoint of the interval
        // xmax - upper endpoint of the interval
        // rtol - tolerance to solve to (1e-4 or something like that)
        // o - object to be passed to BrentFun for extra data (may be null)
        // maxiter - cap on iterations (will throw TimeoutException if exceeded)
        // fx - output of solved value
        // y - output of the function at the solved value
        //
        public static (double x, double fx) Solve(Func<double, object?, double> f, double xmin, double xmax, object? o = null, double rtol = 1e-9,
            int maxiter = 50)
        {
            double c = 0.5 * (3.0 - Math.Sqrt(5.0)); //  C is the square of the inverse of the golden ratio.
            double d = 0;
            double e = 0.0;
            double eps = Math.Sqrt(EPS);
            double sa = xmin;
            double sb = xmax;

            double x = sa + c * (xmax - xmin);

            double w = x;
            double v = w;
            double fx = f(x, o);
            double fw = fx;
            double fv = fw;

            int i = 0;

            while (true)
            {
                double m = 0.5 * (sa + sb);
                double t = eps * Math.Abs(x) + rtol;
                double t2 = 2.0 * t;
                //
                //  Check the stopping criterion.
                //
                if (Math.Abs(x - m) <= t2 - 0.5 * (sb - sa)) break;
                //
                //  Fit a parabola.
                //
                double r = 0.0;
                double q = r;
                double p = q;

                if (t < Math.Abs(e))
                {
                    r = (x - w) * (fx - fv);
                    q = (x - v) * (fx - fw);
                    p = (x - v) * q - (x - w) * r;
                    q = 2.0 * (q - r);
                    if (0.0 < q) p = -p;
                    q = Math.Abs(q);
                    r = e;
                    e = d;
                }

                double u;

                if (Math.Abs(p) < Math.Abs(0.5 * q * r) &&
                    q * (sa - x) < p &&
                    p < q * (sb - x))
                {
                    //
                    //  Take the parabolic interpolation step.
                    //
                    d = p / q;
                    u = x + d;
                    //
                    //  F must not be evaluated too close to A or B.
                    //
                    if (u - sa < t2 || sb - u < t2)
                    {
                        if (x < m)
                            d = t;
                        else
                            d = -t;
                    }
                }
                //
                //  A golden-section step.
                //
                else
                {
                    if (x < m)
                        e = sb - x;
                    else
                        e = sa - x;
                    d = c * e;
                }

                //
                //  F must not be evaluated too close to X.
                //
                if (t <= Math.Abs(d))
                    u = x + d;
                else if (0.0 < d)
                    u = x + t;
                else
                    u = x - t;

                double fu = f(u, o);
                //
                //  Update A, B, V, W, and X.
                //
                if (fu <= fx)
                {
                    if (u < x)
                        sb = x;
                    else
                        sa = x;
                    v  = w;
                    fv = fw;
                    w  = x;
                    fw = fx;
                    x  = u;
                    fx = fu;
                }
                else
                {
                    if (u < x)
                        sa = u;
                    else
                        sb = u;

                    if (fu <= fw || w == x)
                    {
                        v  = w;
                        fv = fw;
                        w  = u;
                        fw = fu;
                    }

                    else if (fu <= fv || v == x || v == w)
                    {
                        v  = u;
                        fv = fu;
                    }
                }

                if (i++ >= maxiter && maxiter > 0)
                    throw new TimeoutException("Brent's minimization method: maximum iterations exceeded");
            }

            return (x, fx);
        }
    }
}
