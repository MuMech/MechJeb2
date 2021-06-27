#nullable enable
using System;

namespace MuMech
{
    public static class BrentMin
    {
        // BrentRoot's 1-dimensional derivative-free local minimization method
        //
        // Uses golden section search and successive parabolic interpolation.
        //
        // f - 1-dimensional BrentFun function to find the minimum of
        // a - lower endpoint of the interval
        // b - upper endpoint of the interval
        // rtol - tolerance to solve to (1e-4 or something like that)
        // x - output of solved value
        // y - output of the function at the solved value
        // o - object to be passed to BrentFun for extra data (may be null)
        // maxiter - cap on iterations (will throw TimeoutException if exceeded)
        //
        public static void Minimize(Func<double, object?, double> f, double a, double b, double tol, out double x, out double y, object o,
            int maxiter = 50)
        {
            double c;
            double d = 0;
            double e;
            double eps;
            double fu;
            double fv;
            double fw;
            double fx;
            double m;
            double p;
            double q;
            double r;
            double sa;
            double sb;
            double t2;
            double t;
            double u;
            double v;
            double w;
            int i = 0;

            //
            //  C is the square of the inverse of the golden ratio.
            //
            c = 0.5 * (3.0 - Math.Sqrt(5.0));

            eps = Math.Sqrt(MuUtils.DBL_EPSILON);

            sa = a;
            sb = b;
            x  = sa + c * (b - a);
            w  = x;
            v  = w;
            e  = 0.0;
            fx = f(x, o);
            fw = fx;
            fv = fw;

            while (true)
            {
                m  = 0.5 * (sa + sb);
                t  = eps * Math.Abs(x) + tol;
                t2 = 2.0 * t;
                //
                //  Check the stopping criterion.
                //
                if (Math.Abs(x - m) <= t2 - 0.5 * (sb - sa)) break;
                //
                //  Fit a parabola.
                //
                r = 0.0;
                q = r;
                p = q;

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

                fu = f(u, o);
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

                if (i++ >= maxiter)
                    throw new TimeoutException("BrentRoot's minimization method: maximum iterations exceeded");
            }

            y = fx;
        }
    }
}
