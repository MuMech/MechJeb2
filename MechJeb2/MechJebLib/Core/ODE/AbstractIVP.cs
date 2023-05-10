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
    using IVPFunc = Action<IList<double>, double, IList<double>>;

    // TODO:
    //  - Needs better MinStep based on next floating point number
    //  - Configurable to throw or just continue at MinStep
    //  - Needs better initial step guessing
    //  - Needs working event API
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
            IReadOnlyList<Event>? events = null)
        {
            try
            {
                N     = y0.Count;
                Y     = Y.Expand(N);
                Dy    = Dy.Expand(N);
                Ynew  = Ynew.Expand(N);
                Dynew = Dynew.Expand(N);

                Init();
                _Solve(f, y0, yf, t0, tf, interpolant, events);
            }

            finally
            {
                Cleanup();
            }
        }

        protected        double[]    Y     = new double[1];
        protected        double[]    Ynew  = new double[1];
        protected        double[]    Dy    = new double[1];
        protected        double[]    Dynew = new double[1];
        protected        double      Habs;
        protected        int         Direction;
        protected        double      T, Tnew;
        protected        double      MaxStep;
        protected        double      MinStep;
        private          double      _habsNext;
        private readonly List<Event> _activeEvents = new List<Event>();

        private Func<double, IList<double>, AbstractIVP, double> _eventFunc = null!;

        private Func<double, object?, double> _eventFunctionDelegate => EventFuncWrapper;

        private double EventFuncWrapper(double x, object? o)
        {
            using var yinterp = Vn.Rent(N);
            Interpolate(x, yinterp);
            return _eventFunc(x, yinterp, this);
        }

        private void _Solve(IVPFunc f, IReadOnlyList<double> y0, IList<double> yf, double t0, double tf, Hn? interpolant,
            IReadOnlyList<Event>? events)
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

            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                    events[i].LastValue = events[i].F(T, Y, this);
            }

            bool terminate = false;

            while ((Direction > 0 && T < tf) || (Direction < 0 && T > tf))
            {
                CancellationToken.ThrowIfCancellationRequested();

                double tnext = T + Habs * Direction;

                if (Direction * (tnext - tf) > 0)
                    MaxStep = Habs = Math.Abs(tf - T);
                else
                    Habs = Math.Abs(tnext - T);

                (Habs, _habsNext) = Step(f);

                Tnew = T + Habs * Direction;

                // handle events, this assumes only one trigger per event per step
                if (events != null)
                {
                    for (int i = 0; i < events.Count; i++)
                    {
                        events[i].NewValue = events[i].F(Tnew, Ynew, this);
                        _activeEvents.Clear();
                        if (IsActiveEvent(events[i]))
                            _activeEvents.Add(events[i]);
                    }

                    if (_activeEvents.Count > 0)
                    {
                        InitInterpolant();

                        for (int i = 0; i < _activeEvents.Count; i++)
                        {
                            _eventFunc            = _activeEvents[i].F;
                            (double tevent, _)    = Bisection.Solve(_eventFunctionDelegate, T, Tnew, null, EPS);
                            _activeEvents[i].Time = tevent;
                        }

                        _activeEvents.Sort();

                        for (int i = 0; i < _activeEvents.Count; i++)
                        {
                            if (_activeEvents[i].Terminal)
                            {
                                terminate = true;
                                using var yinterp = Vn.Rent(N);
                                Interpolate(_activeEvents[i].Time, yinterp);
                                Tnew = _activeEvents[i].Time;
                                Ynew.CopyFrom(yinterp);
                                break;
                            }
                        }
                    }

                    for (int i = 0; i < events.Count; i++)
                        events[i].LastValue = events[i].NewValue;
                }

                // extract a low fidelity interpolant
                if (interpolant != null)
                    interpCount = FillInterpolant(f, t0, tf, interpolant, interpCount);

                // take a step
                Y.CopyFrom(Ynew);
                Dy.CopyFrom(Dynew);
                T    = Tnew;
                Habs = _habsNext;

                if (terminate)
                    break;

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

        private bool IsActiveEvent(Event e)
        {
            bool up = e.LastValue <= 0 && e.NewValue >= 0;
            bool down = e.LastValue >= 0 && e.NewValue <= 0;
            bool either = up || down;
            return (up && e.Direction > 0) || (down && e.Direction < 0) || (either && e.Direction == 0);
        }

        private int FillInterpolant(IVPFunc f, double t0, double tf, Hn interpolant, int interpCount)
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

            return interpCount;
        }

        protected abstract (double, double) Step(IVPFunc f);
        protected abstract double           SelectInitialStep(double t0, double tf);
        protected abstract void             InitInterpolant();
        protected abstract void             Interpolate(double x, Vn yout);
        protected abstract void             Init();
        protected abstract void             Cleanup();
    }
}
