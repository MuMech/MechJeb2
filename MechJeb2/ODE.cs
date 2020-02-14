using System;
using System.Collections.Generic;
//using UnityEngine;

namespace MuMech
{
    public delegate void ODEFun(Span<double> y, double x, Span<double> dy, int n, object o);
    public delegate double EvtFun(Span<double> y, double x, int n, object o);

    class ODE
    {

        // constants for DP5(4)7FM
        private const double a21 = 1.0/5.0;
        private const double a31 = 3.0/40.0;
        private const double a32 = 9.0/40.0;
        private const double a41 = 44.0/45.0;
        private const double a42 = -56.0/15.0;
        private const double a43 = 32.0/9.0;
        private const double a51 = 19372.0/6561.0;
        private const double a52 = -25360.0/2187.0;
        private const double a53 = 64448.0/6561.0;
        private const double a54 = -212.0/729.0;
        private const double a61 = 9017.0/3168.0;
        private const double a62 = -355.0/33.0;
        private const double a63 = 46732.0/5247.0;
        private const double a64 = 49.0/176.0;
        private const double a65 = -5103.0/18656.0;
        private const double a71 = 35.0/384.0;
        //private const double a72 = 0.0;
        private const double a73 = 500.0/1113.0;
        private const double a74 = 125.0/192.0;
        private const double a75 = -2187.0/6784.0;
        private const double a76 = 11.0/84.0;

        private const double c2  = 1.0 / 5.0;
        private const double c3  = 3.0 / 10.0;
        private const double c4  = 4.0 / 5.0;
        private const double c5  = 8.0 / 9.0;
        private const double c6  = 1.0;
        private const double c7  = 1.0;

        private const double b1  = 35.0/384.0;
        //private const double b2  = 0.0;
        private const double b3  = 500.0/1113.0;
        private const double b4  = 125.0/192.0;
        private const double b5  = -2187.0/6784.0;
        private const double b6  = 11.0/84.0;
        private const double b7  = 0.0;

        private const double b1p = 5179.0/57600.0;
        //private const double b2p = 0.0;
        private const double b3p = 7571.0/16695.0;
        private const double b4p = 393.0/640.0;
        private const double b5p = -92097.0/339200.0;
        private const double b6p = 187.0/2100.0;
        private const double b7p = 1.0/40.0;

        // Dormand Prince 5(4)7FM ODE integrator (aka DOPRI5)
        //
        // We prefer this over Bogacki–Shampine in order to obtain the free 4th order interpolant.
        //
        // f       - the ODE function to integrate
        // o       - object paramter which is passed through to the ODE function
        // y0      - array of starting y0 values
        // n       - dimensionality of the problem
        // xtbl    - array of x values to evaluate at (2 minimum for start + end)
        // ytbl    - output jagged array of y values at the x evaluation points (n x 2 minimum)
        // eps     - accuracy
        // h       - starting step-size (can be zero for automatic guess)
        // xlist   - output list of all intermediate x values (user allocates, can be null or omitted)
        // ylist   - output list of all intermediate y values (user allocates, can be null or omitted)
        // hmin    - minimum h step (may be violated on the last step)
        // hmax    - maximum h step
        // maxiter - maximum number of steps
        // EvtFuns - array of event functions
        //
        // ref: https://github.com/scipy/scipy/blob/master/scipy/integrate/dop/dopri5.f
        //
        public static void RKDP547FM(ODEFun f, object o, double[] y0, int n, Span<double> xtbl, Span<double[]> ytbl, double eps, double hstart, List<double> xlist = null, List<double []> ylist = null, double hmin = 0, double hmax = 0, int maxiter = 0, EvtFun[] EvtFuns = null)
        {
            Span<double> k1  = stackalloc double[n];
            Span<double> k2  = stackalloc double[n];
            Span<double> k3  = stackalloc double[n];
            Span<double> k4  = stackalloc double[n];
            Span<double> k5  = stackalloc double[n];
            Span<double> k6  = stackalloc double[n];
            Span<double> k7  = stackalloc double[n];
            // accumulator
            Span<double> a   = stackalloc double[n];
            // error
            Span<double> err = stackalloc double[n];
            double h         = hstart;
            double x         = xtbl[0];
            Span<double> y   = stackalloc double[n];

            if ( ylist != null && xlist != null )
            {
                ylist.Clear();
                xlist.Clear();
            }

            y0.CopyTo(y);

            // auto-guess starting value of h based on smallest dx in xtbl
            if ( h == 0 )
            {
                double v = Math.Abs(xtbl[1] - xtbl[0]);
                for(int i = 1; i < xtbl.Length-1; i++)
                {
                    v = Math.Min(v, Math.Abs(xtbl[i+1]-xtbl[i]));
                }
                h = 0.001*v;
            }

            // xtbl controls the direcction of integration, the sign of h is not relevant
            h = Math.Sign(xtbl[1] - xtbl[0]) * Math.Abs(h);

            // copy initial conditions to ybtl output
            int j = 0;
            for(int i = 0; i < n; i++)
            {
                ytbl[i][j] = y[i];
            }
            j++;

            // copy initial conditions to full xlist/ylist output
            if (xlist != null && ylist != null)
            {
                double[] ydup2 = new double[n];
                y.CopyTo(ydup2);
                ylist.Add(ydup2);
                xlist.Add(x);
            }

            bool fsal = false;
            bool at_hmin;
            double h_restart = 0;
            double niter = 0;

            while(j < xtbl.Length)
            {
                double xf = xtbl[j];

                while ( (h > 0) ? x < xf : x > xf )
                {
                    at_hmin = false;
                    if (hmax > 0 && Math.Abs(h) > hmax)
                        h = hmax * Math.Sign(h);
                    if (hmin > 0 && Math.Abs(h) < hmin)
                    {
                        h = hmin * Math.Sign(h);
                        at_hmin = true;
                    }

                    // we may violate hmin in order to exactly hit the boundary conditions
                    if (Math.Abs(h) > Math.Abs(xf - x))
                        h = xf - x;

                    if (fsal)
                    {
                        // FIXME: tricks to make this copy go away?
                        for(int i = 0; i < n; i++)
                            k1[i] = k7[i];
                    }
                    else
                    {
                        f(y, x, k1, n, o);
                    }

                    for(int i = 0; i < n; i++)
                        a[i] = y[i] + h * ( a21 * k1[i] );
                    f(a, x+c2*h, k2, n, o);

                    for(int i = 0; i < n; i++)
                        a[i] = y[i] + h * ( a31 * k1[i] + a32 * k2[i] );
                    f(a, x+c3*h, k3, n, o);

                    for(int i = 0; i < n; i++)
                        a[i] = y[i] + h * ( a41 * k1[i] + a42 * k2[i] + a43 * k3[i] );
                    f(a, x+c4*h, k4, n, o);

                    for(int i = 0; i < n; i++)
                        a[i] = y[i] + h * ( a51 * k1[i] + a52 * k2[i] + a53 * k3[i] + a54 * k4[i] );
                    f(a, x+c5*h, k5, n, o);

                    for(int i = 0; i < n; i++)
                        a[i] = y[i] + h * ( a61 * k1[i] + a62 * k2[i] + a63 * k3[i] + a64 * k4[i] + a65 * k5[i] );
                    f(a, x+c6*h, k6, n, o);

                    for(int i = 0; i < n; i++)
                        a[i] = y[i] + h * ( a71 * k1[i] + a73 * k3[i] + a74 * k4[i] + a75 * k5[i] + a76 * k6[i] );
                    f(a, x+c7*h, k7, n, o);

                    for(int i = 0; i < n; i++)
                        err[i] = k1[i] * (b1-b1p) + k3[i] * (b3-b3p) + k4[i] * (b4-b4p) + k5[i] * (b5-b5p) + k6[i] * (b6-b6p) + k7[i] * (b7-b7p);

                    double error = 0;
                    for(int i = 0; i < n; i++)
                    {
                        // FIXME: look at dopri fortran code to see how they generate this
                        error = Math.Max(error, Math.Abs(err[i]));
                    }
                    double s = 0.84 * Math.Pow(eps / error, 1.0/5.0);

                    int evt = -1;

                    if (error < eps || at_hmin || h_restart != 0)
                    {
                        int sign = 0;

                        if ( EvtFuns != null )
                        {
                            for(int i = 0; i < EvtFuns.Length; i++)
                            {
                                EvtFun e = EvtFuns[i];
                                double e1 = e(y, x, n, o);
                                double e2 = e(a, x+h, n, o);
                                if ( e1 * e2 < 0 )
                                {
                                    evt = i;
                                    // sign is passed to Brent to ensure we select a value on the other side of the event
                                    sign = Math.Sign(e2);
                                    break;
                                }
                            }
                        }

                        if (evt < 0 || h_restart != 0)
                        {
                            fsal = true; // advancing so use k7 for k1 next time
                            for(int i = 0; i < n; i++)
                                y[i] = a[i]; // FSAL
                            x = x + h;

                            if (xlist != null && ylist != null)
                            {
                                double[] ydup = new double[n];
                                for(int i = 0; i < n; i++)
                                    ydup[i] = y[i];
                                ylist.Add(ydup);
                                xlist.Add(x);
                            }
                            if (h_restart != 0)
                            {
                                h = h_restart;
                                s = 1;
                                h_restart = 0;
                            }
                        } else {
                            fsal = false;
                            s = 1;
                            if (h_restart == 0)
                                h_restart = h;
                            Brent.Solve((newh, a1, a2, a3, a4, o2) => EvtWrapper(EvtFuns[evt], newh, x, a1, a2, x+h, a3, a4, n, o2), 0, h, 1e-15, out h, out _, o, y, k1, a, k7, sign: sign);
                        }
                    }
                    else
                    {
                        fsal = false; // rewinding so k7 is no longer valid
                    }

                    if (s < 0.1)
                        s = 0.1;
                    if (s > 4)
                        s = 4;
                    h = h*s;

                    if (maxiter > 0 && niter++ >= maxiter)
                        throw new ArgumentException("maximum iterations exceeded");
                }

                for(int i = 0; i < n; i++)
                    ytbl[i][j] = y[i];

                j++;
            }
        }

        private static double EvtWrapper(EvtFun f, double newh, double x1, ReadOnlySpan<double> y1, ReadOnlySpan<double> yp1, double x2, ReadOnlySpan<double> y2, ReadOnlySpan<double> yp2, int n, object o)
        {
            Span<double> newy  = stackalloc double[n];
            ODE.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x1+newh, n, newy);
            return f(newy, x1+newh, n, o);
        }

        // with x in the interval [x1,x2] with y1=y(x1), yp1=y'(x1), y2=y(x2), yp2=y'(x2) given at the endpoints, find y(x)
        private static void CubicHermiteInterpolant(double x1, ReadOnlySpan<double> y1, ReadOnlySpan<double> yp1, double x2, ReadOnlySpan<double> y2, ReadOnlySpan<double> yp2, double x, int n, Span<double> y)
        {
            double t = (x - x1) / (x2 - x1);
            double h00 = 2 * t * t * t - 3 * t * t + 1;
            double h10 = t * t * t - 2 * t * t + t;
            double h01 = -2 * t * t * t + 3 * t * t;
            double h11 = t * t * t - t * t;
            for(int i = 0; i < n; i++)
            {
                y[i] = h00 * y1[i] + h10 * ( x2 - x1 ) * yp1[i] + h01 * y2[i] + h11 * ( x2 - x1 ) * yp2[i];
            }
        }

        public static void ball(Span<double> y, double x, Span<double> dy, int n, object o)
        {
            dy[0] = y[1];
            dy[1] = -9.8;
        }

        public static double ballevent(Span<double> y, double x, int n, object o)
        {
            return y[0];
        }

        static void Main(string[] args)
        {
            List<double []> ylist = new List<double []>();
            List<double> xlist    = new List<double>();
            double[] y0 = { 0, 20 };
            double[] x = { 0, 5 };
            double[][] y = new double[][]
            {
                new double[2],
                new double[2],
            };
            EvtFun[] EvtFuns = { ballevent };
            ODE.RKDP547FM(ball, null, y0, 2, x, y, 1e-9, 0, hmin: 1e-8, xlist: xlist, ylist: ylist, EvtFuns: EvtFuns);
            for(int i = 0; i < xlist.Count; i++)
            {
                Console.Write(xlist[i]);
                if (i != xlist.Count - 1)
                    Console.Write(", ");
            }
            Console.Write("\n");
            for(int i = 0; i < ylist.Count; i++)
            {
                for(int j = 0; j < 2; j++)
                {
                    Console.Write(ylist[i][j]);
                    if (j != 1)
                        Console.Write(", ");
                }
                Console.Write("\n");
            }
        }

        public static void centralForce(Span<double> y, double x, Span<double> dy, int n, object o)
        {
            double r2 = y[0] * y[0] + y[1] * y[1] + y[2] * y[2];
            double r1 = Math.Sqrt(r2);
            double r3 = r2 * r1;

            // dx = v
            dy[0] = y[3];
            dy[1] = y[4];
            dy[2] = y[5];
            // dv = - r/r^3
            dy[3] = - y[0] / r3;
            dy[4] = - y[1] / r3;
            dy[5] = - y[2] / r3;
        }

        static void Main2(string[] args)
        {
            List<double []> ylist = new List<double []>();
            List<double> xlist    = new List<double>();
            double[] y0 = { 1, 0, 0, 0, 1, 0 };
            double[] x = { 0 * 0.78539816339, 1 * 0.78539816339, 2 * 0.78539816339, 3 * 0.78539816339, 4 * 0.78539816339 };
            double[][] y = new double[][]
            {
                new double[5],
                new double[5],
                new double[5],
                new double[5],
                new double[5],
                new double[5],
            };

            var watch = System.Diagnostics.Stopwatch.StartNew();
            for(int i = 0; i < 100000; i++)
                ODE.RKDP547FM(centralForce, null, y0, 6, x, y, 1e-9, 0, xlist: xlist, ylist: ylist);
            watch.Stop();
            Console.WriteLine("ms : {0:F6}", watch.ElapsedMilliseconds);
            for(int i = 0; i < xlist.Count; i++)
            {
                Console.Write(xlist[i]);
                if (i != xlist.Count - 1)
                    Console.Write(", ");
            }
            Console.Write("\n");
            for(int i = 0; i < ylist.Count; i++)
            {
                for(int j = 0; j < 6; j++)
                {
                    Console.Write(ylist[i][j]);
                    if (j != 5)
                        Console.Write(", ");
                }
                Console.Write("\n");
            }
        }
    }
}
