/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Primitives;
using static MechJebLib.Utils.Statics;

#nullable enable

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.Core.ODE
{
    using IVPFunc = Action<Vn, double, Vn>;
    using IVPEvent = Func<double, Vn, Vn, (double x, bool dir, bool stop)>;

    public abstract class AbstractIVP
    {
        /// <summary>
        ///     Minimum h step (may be violated on the last step or before an event).
        /// </summary>
        public double Hmin { get; set; } = EPS;

        /// <summary>
        ///     Maximum h step.
        /// </summary>
        public double Hmax { get; set; } = double.PositiveInfinity;

        /// <summary>
        ///     Maximum number of steps.
        /// </summary>
        public double Maxiter { get; set; } = 2000;

        /// <summary>
        ///     Desired relative tolerance.
        /// </summary>
        public double Rtol { get; set; } = 1e-9;

        /// <summary>
        ///     Desired absolute tolerance.
        /// </summary>
        public double Atol { get; set; } = 1e-9;

        /// <summary>
        ///     Starting step-size (can be zero for automatic guess).
        /// </summary>
        public double Hstart { get; set; }

        /// <summary>
        ///     Interpolants are pulled on an evenly spaced grid
        /// </summary>
        public int Interpnum { get; set; } = 20;

        /// <summary>
        ///     Throw exception when MaxIter is hit (PVG optimizer works better with this set to false).
        /// </summary>
        public bool ThrowOnMaxIter { get; set; } = true;

        public CancellationToken CancellationToken { get; }

        protected int N;

        /// <summary>
        ///     Dormand Prince 5(4)7FM ODE integrator (aka DOPRI5 aka ODE45)
        /// </summary>
        /// <param name="f"></param>
        /// <param name="y0"></param>
        /// <param name="yf"></param>
        /// <param name="t0"></param>
        /// <param name="tf"></param>
        /// <param name="interpolant"></param>
        /// <param name="events"></param>
        /// <exception cref="ArgumentException"></exception>
        public void Solve(IVPFunc f, IReadOnlyList<double> y0, IList<double> yf, double t0, double tf, Hn? interpolant = null,
            IReadOnlyCollection<IVPEvent>? events = null)
        {
            try
            {
                N     = y0.Count;
                Ynew  = Vn.Rent(N);
                Dynew = Vn.Rent(N);
                Dy    = Vn.Rent(N);
                Y     = Vn.Rent(N);

                Init();
                _Solve(f, y0, yf, t0, tf, interpolant, events);
            }
            finally
            {
                Y.Dispose();
                Dy.Dispose();
                Ynew.Dispose();
                Dynew.Dispose();
                Y = Ynew = Dy = Dynew = null!;

                Cleanup();
            }
        }

        protected Vn     Y     = null!;
        protected Vn     Ynew  = null!;
        protected Vn     Dy    = null!;
        protected Vn     Dynew = null!;
        protected double Habs;
        protected int    Direction;
        protected double T, Tnew;
        protected double MaxStep;
        protected double MinStep;
        private   double _habsNext;

        private void _Solve(IVPFunc f, IReadOnlyList<double> y0, IList<double> yf, double t0, double tf, Hn? interpolant,
            IReadOnlyCollection<IVPEvent>? events)
        {
            Direction = t0 != tf ? Math.Sign(tf - t0) : 1;
            Habs      = SelectInitialStep(t0, tf);
            MaxStep   = Hmax;
            MinStep   = Hmin;

            T = t0;
            Y.CopyFrom(y0);
            double niter = 0;
            int interpCount = 1;

            f(Y, T, Dy);

            interpolant?.Add(T, Y, Dy);

            while (T != tf)
            {
                CancellationToken.ThrowIfCancellationRequested();

                double tnext = T + Habs * Direction;

                if (Direction * (tnext - tf) > 0)
                    MaxStep = Habs = Math.Abs(tf - T);
                else
                    Habs = Math.Abs(tnext - T);

                (Habs, _habsNext) = Step(f);

                Tnew = T + Habs * Direction;

                if (events != null)
                {
                }

                // extract a low fidelity interpolant
                if (interpolant != null)
                {
                    while (interpCount < Interpnum)
                    {
                        double tinterp = t0 + (tf - t0) * interpCount / Interpnum;

                        if (!tinterp.IsWithin(T, Tnew))
                            break;

                        using var yinterp = Vn.Rent(N);
                        using var finterp = Vn.Rent(N);

                        InitInterpolant();
                        Interpolate(tinterp, yinterp);
                        f(yinterp, tinterp, finterp);
                        interpolant?.Add(tinterp, yinterp, finterp);
                        interpCount++;
                    }
                }

                // take a step
                Y.CopyFrom(Ynew);
                Dy.CopyFrom(Dynew);
                T    = Tnew;
                Habs = _habsNext;

                // handle max iterations
                if (Maxiter > 0 && niter++ > Maxiter)
                {
                    if (ThrowOnMaxIter)
                        throw new ArgumentException("maximum iterations exceeded");

                    break;
                }
            }

            interpolant?.Add(T, Y, Dy);

            Y.CopyTo(yf);
        }

        protected abstract (double, double) Step(IVPFunc f);
        protected abstract double           SelectInitialStep(double t0, double tf);
        protected abstract void             InitInterpolant();
        protected abstract void             Interpolate(double x, Vn yout);
        protected abstract void             Init();
        protected abstract void             Cleanup();
    }
}
