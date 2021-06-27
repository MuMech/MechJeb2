#nullable enable
using System;
using System.Collections.Generic;

namespace MuMech.MathJ
{
    public class DormandPrince : ODESolver
    {
        /// <summary>
        ///     Minimum h step (may be violated on the last step or before an event).
        /// </summary>
        public double Hmin { get; set; } = Utils.EPS;

        /// <summary>
        ///     Maximum h step.
        /// </summary>
        public double Hmax { get; set; }

        /// <summary>
        ///     Maximum number of steps.
        /// </summary>
        public double Maxiter { get; set; } = 2000;

        /// <summary>
        ///     Desired local accuracy.
        /// </summary>
        public double Accuracy { get; set; } = 1e-9;

        /// <summary>
        ///     Starting step-size (can be zero for automatic guess).
        /// </summary>
        public double Hstart { get; set; }

        /// <summary>
        ///     Interpolants are pulled on an evenly spaced grid
        /// </summary>
        public double Interpnum { get; set; } = 20;

        /// <summary>
        ///     Throw exception when MaxIter is hit (PVG optimizer works better with this set to false).
        /// </summary>
        public bool ThrowOnMaxIter { get; set; }

        #region IntegrationConstants

        // constants for DP5(4)7FM
        private const double A21 = 1.0 / 5.0;
        private const double A31 = 3.0 / 40.0;
        private const double A32 = 9.0 / 40.0;
        private const double A41 = 44.0 / 45.0;
        private const double A42 = -56.0 / 15.0;
        private const double A43 = 32.0 / 9.0;
        private const double A51 = 19372.0 / 6561.0;
        private const double A52 = -25360.0 / 2187.0;
        private const double A53 = 64448.0 / 6561.0;
        private const double A54 = -212.0 / 729.0;
        private const double A61 = 9017.0 / 3168.0;
        private const double A62 = -355.0 / 33.0;
        private const double A63 = 46732.0 / 5247.0;
        private const double A64 = 49.0 / 176.0;
        private const double A65 = -5103.0 / 18656.0;

        private const double A71 = 35.0 / 384.0;

        //private const double a72 = 0.0;
        private const double A73 = 500.0 / 1113.0;
        private const double A74 = 125.0 / 192.0;
        private const double A75 = -2187.0 / 6784.0;
        private const double A76 = 11.0 / 84.0;

        private const double C2 = 1.0 / 5.0;
        private const double C3 = 3.0 / 10.0;
        private const double C4 = 4.0 / 5.0;
        private const double C5 = 8.0 / 9.0;
        private const double C6 = 1.0;
        private const double C7 = 1.0;

        private const double B1 = 35.0 / 384.0;

        //private const double b2  = 0.0;
        private const double B3 = 500.0 / 1113.0;
        private const double B4 = 125.0 / 192.0;
        private const double B5 = -2187.0 / 6784.0;
        private const double B6 = 11.0 / 84.0;
        private const double B7 = 0.0;

        private const double B1_P = 5179.0 / 57600.0;

        //private const double b2p = 0.0;
        private const double B3_P = 7571.0 / 16695.0;
        private const double B4_P = 393.0 / 640.0;
        private const double B5_P = -92097.0 / 339200.0;
        private const double B6_P = 187.0 / 2100.0;
        private const double B7_P = 1.0 / 40.0;

        #endregion

        private readonly BrentArgs _brentargs = new BrentArgs();

        private readonly HashSet<Event> _events = new HashSet<Event>();

        private readonly Func<double, object?, double> _evtDelegate;

        public override void AddEvent(Event e)
        {
            _events.Add(e);
        }

        public DormandPrince()
        {
            _evtDelegate = EvtWrapper;
        }

        private int                                _n;
        private int                                _m;
        private Action<double[], double, double[]> _dydt = null!;
        private double[]                           _k1   = null!;
        private double[]                           _k2   = null!;
        private double[]                           _k3   = null!;
        private double[]                           _k4   = null!;
        private double[]                           _k5   = null!;
        private double[]                           _k6   = null!;
        private double[]                           _k7   = null!;
        private double[]                           _a    = null!;
        private double[]                           _err  = null!;
        private double[]                           _y    = null!;
        private double[]                           _newy = null!;

        public override void Initialize(Action<double[], double, double[]> dydt, int n, int m)
        {
            _n    = n;
            _m    = m;
            _dydt = dydt;
            _k1   = Utils.DoublePool.Rent(n + m);
            _k2   = Utils.DoublePool.Rent(n);
            _k3   = Utils.DoublePool.Rent(n);
            _k4   = Utils.DoublePool.Rent(n);
            _k5   = Utils.DoublePool.Rent(n);
            _k6   = Utils.DoublePool.Rent(n);
            _k7   = Utils.DoublePool.Rent(n + m);
            _a    = Utils.DoublePool.Rent(n + m);
            _err  = Utils.DoublePool.Rent(n);
            _y    = Utils.DoublePool.Rent(n + m);
            _newy = Utils.DoublePool.Rent(n + m);
        }

        // Dormand Prince 5(4)7FM ODE integrator (aka DOPRI5 aka ODE45)
        //
        // y0      - array of starting y0 values
        // xtbl    - array of x values to evaluate at (2 minimum for start + end)
        // ytbl    - output jagged array of y values at the x evaluation points (n x 2 minimum)
        // EvtFuns - array of event functions
        //
        // ref: https://github.com/scipy/scipy/blob/master/scipy/integrate/dop/dopri5.f
        //
        public override void Integrate(double[] y0, double[] yf, double t0, double tf, CN? interpolant = null)
        {
            double h = Hstart;
            double t = t0;

            for (int i = 0; i < _n; i++) _y[i] = y0[i];

            // auto-guess starting value of h based on tf-t0
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (h == 0)
            {
                double v = Math.Abs(tf - t0);
                h = 0.001 * v;
            }

            // tf-t0 controls the direction of integration, the sign of h is not relevant
            h = Math.Sign(tf - t0) * Math.Abs(h);

            bool fsal = false;        // do first-same-as-last because last time we took a normal step
            bool terminate = false;   // event requested the next step is the last
            double oldh = double.NaN; // if we take a truncated step this will save the last untruncated step size
            double niter = 0;
            double lastInterpolant = t0;
            double interpDt = (tf - t0) / Interpnum;
            double interpLeft = interpDt;

            if (interpolant != null)
            {
                _dydt(_y, t, _k1);
                interpolant.Add(t, _y, _k1);
            }

            EventsFired.Clear();

            while (h > 0 ? t < tf : t > tf)
            {
                if (h > 0 && t + h > tf)
                    h = tf - t;

                if (h < 0 && t + h < tf)
                    h = tf - t;

                if (interpolant != null && Math.Abs(h) > Math.Abs(interpLeft))
                {
                    h = interpLeft;
                    // save the untruncated step size if we weren't already truncated
                    if (!oldh.IsFinite())
                        oldh = h;
                }

                if (fsal)
                    for (int i = 0; i < _n; i++)
                        _k1[i] = _k7[i];
                else
                    _dydt(_y, t, _k1);

                for (int i = 0; i < _n; i++)
                    _a[i] = _y[i] + h * (A21 * _k1[i]);
                _dydt(_a, t + C2 * h, _k2);

                for (int i = 0; i < _n; i++)
                    _a[i] = _y[i] + h * (A31 * _k1[i] + A32 * _k2[i]);
                _dydt(_a, t + C3 * h, _k3);

                for (int i = 0; i < _n; i++)
                    _a[i] = _y[i] + h * (A41 * _k1[i] + A42 * _k2[i] + A43 * _k3[i]);
                _dydt(_a, t + C4 * h, _k4);

                for (int i = 0; i < _n; i++)
                    _a[i] = _y[i] + h * (A51 * _k1[i] + A52 * _k2[i] + A53 * _k3[i] + A54 * _k4[i]);
                _dydt(_a, t + C5 * h, _k5);

                for (int i = 0; i < _n; i++)
                    _a[i] = _y[i] + h * (A61 * _k1[i] + A62 * _k2[i] + A63 * _k3[i] + A64 * _k4[i] + A65 * _k5[i]);
                _dydt(_a, t + C6 * h, _k6);

                for (int i = 0; i < _n; i++)
                    _a[i] = _y[i] + h * (A71 * _k1[i] + A73 * _k3[i] + A74 * _k4[i] + A75 * _k5[i] + A76 * _k6[i]);
                _dydt(_a, t + C7 * h, _k7);

                for (int i = 0; i < _n; i++)
                    _err[i] = _k1[i] * (B1 - B1_P) + _k3[i] * (B3 - B3_P) + _k4[i] * (B4 - B4_P) + _k5[i] * (B5 - B5_P) +
                              _k6[i] * (B6 - B6_P) + _k7[i] * (B7 - B7_P);

                double error = 0;
                for (int i = 0; i < _n; i++)
                    // FIXME: look at dopri fortran code to see how they generate this
                    error = Math.Max(error, Math.Abs(_err[i]));

                if (error > Accuracy)
                {
                    double s = 0.84 * Math.Pow(Accuracy / error, 1.0 / 5.0);

                    if (s < 0.1)
                        s = 0.1;
                    if (s > 4)
                        s = 4;
                    h *= s;

                    if (Hmin > 0 && Math.Abs(h) < Hmin)
                        h = Hmin * Math.Sign(h);
                    if (Hmax > 0 && Math.Abs(h) > Hmax)
                        h = Hmax * Math.Sign(h);

                    fsal = false;

                    continue;
                }

                // search for an event trigger
                foreach (Event e in _events)
                {
                    if (!e.Enabled)
                        continue;

                    double e1 = e.Evaluate(_y, t);
                    double e2 = e.Evaluate(_a, t + h);

                    if (e1 * e2 > 0) continue;

                    EventsFired.Add(e);

                    if (e.Stop)
                        terminate = true;

                    if (e2 != 0)
                    {
                        _brentargs.Evt = e;
                        _brentargs.X1  = t;
                        _brentargs.Y1  = _y;
                        _brentargs.Yp1 = _k1;
                        _brentargs.X2  = t + h;
                        _brentargs.Y2  = _a;
                        _brentargs.Yp2 = _k7;
                        _brentargs.N   = _n;

                        // save the untruncated step size if we weren't already truncated
                        if (!oldh.IsFinite())
                            oldh = h;

                        h = BrentRoot.Solve(_evtDelegate, 0, h, _brentargs, sign: Math.Sign(e2));

                        Functions.CubicHermiteInterpolant(_brentargs.X1, _brentargs.Y1, _brentargs.Yp1, _brentargs.X2, _brentargs.Y2, _brentargs.Yp2,
                            _brentargs.X1 + h, _brentargs.N, _newy);

                        _newy.CopyTo(_a, 0);
                        _dydt(_a, t + h, _k7);
                    }

                    break;
                }

                /*
                 * Start handling taking a real step
                 */

                fsal = true; // advancing so use k7 for k1 next time
                for (int i = 0; i < _n + _m; i++)
                    _y[i] = _a[i]; // FSAL
                t += h;

                interpLeft -= h;

                // add this point to the interpolant
                if (interpolant != null &&
                    (Math.Abs(interpLeft) <= 2*Utils.EPS && Utils.NearlyEqual(t - lastInterpolant, interpDt, 1e-2) || terminate))
                {
                    interpolant.Add(t, _y, _k7);
                    interpLeft      = interpDt;
                    lastInterpolant = t;
                }

                if (terminate)
                    break;

                if (oldh.IsFinite())
                {
                    // we took a truncated step so restore the old step size
                    h    = oldh;
                    oldh = double.NaN;
                }
                else
                {
                    double s = 0.84 * Math.Pow(Accuracy / error, 1.0 / 5.0);

                    if (s < 0.1)
                        s = 0.1;
                    if (s > 4)
                        s = 4;
                    h *= s;

                    if (Hmin > 0 && Math.Abs(h) < Hmin)
                        h = Hmin * Math.Sign(h);
                    if (Hmax > 0 && Math.Abs(h) > Hmax)
                        h = Hmax * Math.Sign(h);
                }

                // handle max iterations
                if (Maxiter <= 0 || niter++ < Maxiter)
                    continue;

                if (ThrowOnMaxIter)
                    throw new ArgumentException("maximum iterations exceeded");

                break;
            }

            // copy results into yf
            for (int i = 0; i < _n; i++) yf[i] = _y[i];
        }

        private double EvtWrapper(double newh, object? o)
        {
            var args = (BrentArgs) o!;

            Functions.CubicHermiteInterpolant(args.X1, args.Y1!, args.Yp1!, args.X2, args.Y2!, args.Yp2!,
                args.X1 + newh, args.N, _newy);
            return args.Evt!.Evaluate(_newy, args.X1 + newh);
        }

        private class BrentArgs
        {
            public Event?    Evt;
            public int       N;
            public double    X1;
            public double    X2;
            public double[]? Y1;
            public double[]? Y2;
            public double[]? Yp1;
            public double[]? Yp2;
        }
    }
}
