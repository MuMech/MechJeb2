using System;
using UnityEngine;

namespace MuMech {
    public delegate double BrentFun(double x, object o);
    public delegate double BrentFun2(double x, ReadOnlySpan<double> k1, ReadOnlySpan<double> k2, ReadOnlySpan<double> k3, ReadOnlySpan<double> k4, object o);

    public class Brent {
        // Brent's rootfinding method
        //
        // Uses secant or inverse quadratic interpolation as appropriate, with a fallback to bisection if necessary.
        //
        // f - 1-dimensional BrentFun function to find the root on
        // a - first guess of x value
        // b - second guess of x value
        // rtol - tolerance to solve to (1e-4 or something like that)
        // x - output of solved value
        // y - output of the function at the solved value (should be zero to rtol)
        // o - object to be passed to BrentFun for extra data (may be null)
        // maxiter - cap on iterations (will throw TimeoutException if exceeded)
        // sign - select the positive or negative side of the root for return value
        //
        // NOTE: please make any fixes to both versions of this routine
        //
        public static void Root(BrentFun f, double a, double b, double rtol, out double x, out double y, object o, int maxiter = 50, int sign=0)
        {
            double c = 0;
            double d = Double.MaxValue;

            double fa = f(a, o);
            double fb = f(b, o);

            double fc = 0;
            double s = 0;
            double fs = 0;

            if (fa * fb >= 0)
                throw new ArgumentException("Brent's rootfinding method: guess does not bracket the root");

            if (Math.Abs(fa) < Math.Abs(fb))
            {
                double tmp = a;
                a = b;
                b = tmp;
                tmp = fa;
                fa = fb;
                fb = tmp;
            }

            c = a;
            fc = fa;
            bool mflag = true;
            int i = 0;

            while ((!(fb==0) && (Math.Abs(a-b) > rtol)) || ((sign!=0) && (Math.Sign(fb)!=sign)))
            {
                if ((fa != fc) && (fb != fc))
                {
                    // inverse quadratic interpolation
                    s = a * fb * fc / (fa - fb) / (fa - fc) + b * fa * fc / (fb - fa) /
                        (fb - fc) + c * fa * fb / (fc - fa) / (fc - fb);
                }
                else
                {
                    // secant
                    s = b - fb * (b - a) / (fb - fa);
                }

                double tmp2 = (3 * a + b) / 4;
                if ((!(((s > tmp2) && (s < b)) || ((s < tmp2) && (s > b)))) ||
                        (mflag && (Math.Abs(s - b) >= (Math.Abs(b - c) / 2))) ||
                        (!mflag && (Math.Abs(s - b) >= (Math.Abs(c - d) / 2))))
                {
                    // bisection
                    s = (a + b) / 2;
                    mflag = true;
                }
                else
                {
                    if ((mflag && (Math.Abs(b - c) < rtol)) || (!mflag && (Math.Abs(c - d) < rtol)))
                    {
                        s = (a + b) / 2;
                        mflag = true;
                    }
                    else
                    {
                        mflag = false;
                    }
                }

                fs = f(s, o);
                d = c;
                c = b;
                fc = fb;

                if (fa * fs < 0)
                {
                    b = s;
                    fb = fs;
                }
                else
                {
                    a = s;
                    fa = fs;
                }

                if (Math.Abs(fa) < Math.Abs(fb))
                {
                    double tmp = a;
                    a = b;
                    b = tmp;
                    tmp = fa;
                    fa = fb;
                    fb = tmp;
                }

                if (i++ >= maxiter)
                {
                    x = b;
                    y = fb;
                    throw new TimeoutException("Brent's rootfinding method: maximum iterations exceeded");
                }
            }

            x = b;
            y = fb;
        }

        // This is necessary due to the fact that we can't create closures over Span<T>'s in C# 8.0, if this gets fixed or
        // someone can find a workaround then we could eliminate this unfortunate copypasta.
        //
        // (see my question at https://stackoverflow.com/questions/59605908/what-is-a-working-alternative-to-being-unable-to-pass-a-spant-into-lambda-expr )
        //
        public static void Root(BrentFun2 f, double a, double b, double rtol, out double x, out double y, object o, ReadOnlySpan<double> k1, ReadOnlySpan<double> k2, ReadOnlySpan<double> k3, ReadOnlySpan<double> k4, int maxiter = 50, int sign = 0)
        {
            double c = 0;
            double d = Double.MaxValue;

            double fa = f(a, k1, k2, k3, k4, o);
            double fb = f(b, k1, k2, k3, k4, o);

            double fc = 0;
            double s = 0;
            double fs = 0;

            if (fa * fb >= 0)
                throw new ArgumentException("Brent's rootfinding method: guess does not bracket the root");

            if (Math.Abs(fa) < Math.Abs(fb))
            {
                double tmp = a;
                a = b;
                b = tmp;
                tmp = fa;
                fa = fb;
                fb = tmp;
            }

            c = a;
            fc = fa;
            bool mflag = true;
            int i = 0;

            while ((!(fb==0) && (Math.Abs(a-b) > rtol)) || ((sign!=0) && (Math.Sign(fb)!=sign)))
            {
                if ((fa != fc) && (fb != fc))
                {
                    // inverse quadratic interpolation
                    s = a * fb * fc / (fa - fb) / (fa - fc) + b * fa * fc / (fb - fa) /
                        (fb - fc) + c * fa * fb / (fc - fa) / (fc - fb);
                }
                else
                {
                    // secant
                    s = b - fb * (b - a) / (fb - fa);
                }

                double tmp2 = (3 * a + b) / 4;
                if ((!(((s > tmp2) && (s < b)) || ((s < tmp2) && (s > b)))) ||
                        (mflag && (Math.Abs(s - b) >= (Math.Abs(b - c) / 2))) ||
                        (!mflag && (Math.Abs(s - b) >= (Math.Abs(c - d) / 2))))
                {
                    // bisection
                    s = (a + b) / 2;
                    mflag = true;
                }
                else
                {
                    if ((mflag && (Math.Abs(b - c) < rtol)) || (!mflag && (Math.Abs(c - d) < rtol)))
                    {
                        s = (a + b) / 2;
                        mflag = true;
                    }
                    else
                    {
                        mflag = false;
                    }
                }

                fs = f(s, k1, k2, k3, k4, o);
                d = c;
                c = b;
                fc = fb;

                if (fa * fs < 0)
                {
                    b = s;
                    fb = fs;
                }
                else
                {
                    a = s;
                    fa = fs;
                }

                if (Math.Abs(fa) < Math.Abs(fb))
                {
                    double tmp = a;
                    a = b;
                    b = tmp;
                    tmp = fa;
                    fa = fb;
                    fb = tmp;
                }

                if (i++ >= maxiter)
                {
                    x = b;
                    y = fb;
                    throw new TimeoutException("Brent's rootfinding method: maximum iterations exceeded");
                }
            }

            x = b;
            y = fb;
        }

        // Brent's 1-dimensional derivative-free local minimization method
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
        public static void Minimize(BrentFun f, double a, double b, double tol, out double x, out double y, object o, int maxiter = 50)
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
            c = 0.5 * ( 3.0 - Math.Sqrt ( 5.0 ) );

            eps = Math.Sqrt ( MuUtils.DBL_EPSILON );

            sa = a;
            sb = b;
            x = sa + c * ( b - a );
            w = x;
            v = w;
            e = 0.0;
            fx = f(x, o);
            fw = fx;
            fv = fw;

            while(true)
            {
                m = 0.5 * ( sa + sb ) ;
                t = eps * Math.Abs ( x ) + tol;
                t2 = 2.0 * t;
                //
                //  Check the stopping criterion.
                //
                if ( Math.Abs ( x - m ) <= t2 - 0.5 * ( sb - sa ) )
                {
                    break;
                }
                //
                //  Fit a parabola.
                //
                r = 0.0;
                q = r;
                p = q;

                if ( t < Math.Abs ( e ) )
                {
                    r = ( x - w ) * ( fx - fv );
                    q = ( x - v ) * ( fx - fw );
                    p = ( x - v ) * q - ( x - w ) * r;
                    q = 2.0 * ( q - r );
                    if ( 0.0 < q )
                    {
                        p = - p;
                    }
                    q = Math.Abs ( q );
                    r = e;
                    e = d;
                }

                if ( Math.Abs ( p ) < Math.Abs ( 0.5 * q * r ) &&
                        q * ( sa - x ) < p &&
                        p < q * ( sb - x ) )
                {
                    //
                    //  Take the parabolic interpolation step.
                    //
                    d = p / q;
                    u = x + d;
                    //
                    //  F must not be evaluated too close to A or B.
                    //
                    if ( ( u - sa ) < t2 || ( sb - u ) < t2 )
                    {
                        if ( x < m )
                        {
                            d = t;
                        }
                        else
                        {
                            d = - t;
                        }
                    }
                }
                //
                //  A golden-section step.
                //
                else
                {
                    if ( x < m )
                    {
                        e = sb - x;
                    }
                    else
                    {
                        e = sa - x;
                    }
                    d = c * e;
                }
                //
                //  F must not be evaluated too close to X.
                //
                if ( t <= Math.Abs ( d ) )
                {
                    u = x + d;
                }
                else if ( 0.0 < d )
                {
                    u = x + t;
                }
                else
                {
                    u = x - t;
                }

                fu = f(u, o);
                //
                //  Update A, B, V, W, and X.
                //
                if ( fu <= fx )
                {
                    if ( u < x )
                    {
                        sb = x;
                    }
                    else
                    {
                        sa = x;
                    }
                    v = w;
                    fv = fw;
                    w = x;
                    fw = fx;
                    x = u;
                    fx = fu;
                }
                else
                {
                    if ( u < x )
                    {
                        sa = u;
                    }
                    else
                    {
                        sb = u;
                    }

                    if ( fu <= fw || w == x )
                    {
                        v = w;
                        fv = fw;
                        w = u;
                        fw = fu;
                    }
                    else if ( fu <= fv || v == x || v == w )
                    {
                        v = u;
                        fv = fu;
                    }
                }
                if (i++ >= maxiter)
                    throw new TimeoutException("Brent's minimization method: maximum iterations exceeded");
            }
            y = fx;
        }
    }
}
