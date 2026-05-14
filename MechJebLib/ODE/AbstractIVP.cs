/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Primitives;
using MechJebLib.Rootfinding;
using static MechJebLib.Utils.Statics;

// ReSharper disable CompareOfFloatsByEqualityOperator
namespace MechJebLib.ODE
{
    using IVPFunc = Action<Vec, double, Vec>;

    // TODO:
    //  - Needs working event API
    public abstract class AbstractIVP
    {
        public enum IVPStatus { Initialized, Success, EventTerminated, MaxStepsExceeded, Failed }

        private readonly List<Event> _activeEvents = new List<Event>();

        private Func<Vec, double, AbstractIVP, double> _eventFunc = null!;

        private double _habsNext;
        protected int Direction;
        protected bool Snapping;
        protected double Habs;
        protected double MaxStep;
        protected double MinStep;

        protected int N;
        public double T;
        protected double Tnew;

        // ReSharper disable NullableWarningSuppressionIsUsed
        protected Vec Y = null!;
        protected Vec Ynew = null!;
        protected Vec Dy = null!;
        protected Vec Dynew = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed


        /// <summary>
        ///     Minimum h step (may be violated on the last step or before an event).
        /// </summary>
        public double Hmin { get; set; } = 0;

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
        public double Hstart { get; set; } = 0.0;

        /// <summary>
        ///     Interpolants are pulled on an evenly spaced grid
        /// </summary>
        public int Interpnum { get; set; } = 20;

        /// <summary>
        ///     Throw exception when MaxIter is hit (old PVG optimizer worked better with this set to false).
        /// </summary>
        public bool ThrowOnMaxIter { get; set; } = true;

        /// <summary>
        ///     Throw exception when MinStep is hit.
        /// </summary>
        public bool ThrowOnMinStep { get; set; } = true;

        public IVPStatus Status { get; set; } = IVPStatus.Success;

        public CancellationToken CancellationToken { get; }

        private Func<double, object?, double> _eventFunctionDelegate => EventFuncWrapper;

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
        public void Solve(IVPFunc f, Vec y0, Vec yf, double t0, double tf,
            Hn? interpolant = null,
            IReadOnlyList<Event>? events = null)
        {
            try
            {
                N = y0.Count;
                Y = Vec.Rent(N);
                Dy = Vec.Rent(N);
                Ynew = Vec.Rent(N);
                Dynew = Vec.Rent(N);

                Init();
                _Solve(f, y0, yf, t0, tf, interpolant, events);
            }
            catch (Exception)
            {
                if (Status == IVPStatus.Initialized)
                    Status = IVPStatus.Failed;
                throw;
            }
            finally
            {
                Y.Dispose();
                Dy.Dispose();
                Dynew.Dispose();
                Ynew.Dispose();
                Cleanup();
            }
        }

        private double EventFuncWrapper(double x, object? o)
        {
            using var yinterp = Vec.Rent(N);
            Interpolate(x, yinterp);
            return _eventFunc(yinterp, x, this);
        }

        private void _Solve(IVPFunc f, Vec y0, Vec yf, double t0, double tf,
            Hn? interpolant,
            IReadOnlyList<Event>? events)
        {
            Status = IVPStatus.Initialized;

            Direction = t0 != tf ? Math.Sign(tf - t0) : 1;
            MaxStep = Hmax;
            MinStep = Hmin;

            T = t0;
            Y.CopyFrom(y0);
            double niter       = 0;
            int    interpCount = 1;

            f(Y, T, Dy);

            Habs = Hstart > 0 ? Hstart : SelectInitialStep(f, T, Y, Dy, Direction);
            Habs = Clamp(Habs, MinStep, MaxStep);

            interpolant?.Add(T, Y, Dy);

            if (events != null)
            {
                for (int i = 0; i < events.Count; i++)
                    events[i].LastValue = events[i].F(Y, T, this);
            }

            bool terminate = false;

            while ((Direction > 0 && T < tf) || (Direction < 0 && T > tf))
            {
                CancellationToken.ThrowIfCancellationRequested();

                double tnext = T + Habs * Direction;

                if (Direction * (tnext - tf) > 0)
                {
                    Snapping = true;
                    MaxStep = Habs = Math.Abs(tf - T);
                }
                else
                {
                    Snapping = false;
                    Habs = Math.Abs(tnext - T); // deliberate for bit-stability
                }

                (Habs, _habsNext) = Step(f);

                Tnew = Snapping && MaxStep == Habs ? tf : T + Habs * Direction;

                // handle events, this assumes only one trigger per event per step
                if (events != null)
                {
                    _activeEvents.Clear();

                    for (int i = 0; i < events.Count; i++)
                    {
                        events[i].NewValue = events[i].F(Ynew, Tnew, this);
                        if (IsActiveEvent(events[i]))
                            _activeEvents.Add(events[i]);
                    }

                    if (_activeEvents.Count > 0)
                    {
                        InitInterpolant();

                        for (int i = 0; i < _activeEvents.Count; i++)
                        {
                            _eventFunc = _activeEvents[i].F;
                            (double tevent, _) = Bisection.Solve(_eventFunctionDelegate, T, Tnew, null, EPS);
                            _activeEvents[i].Time = tevent;
                        }

                        _activeEvents.Sort();

                        for (int i = 0; i < _activeEvents.Count; i++)
                        {
                            if (_activeEvents[i].Terminal)
                            {
                                terminate = true;
                                // Truncate the step to the event point. Evaluate the
                                // interpolant FIRST against the full-step (T,Y,Dy)/
                                // (Tnew,Ynew,Dynew) — interpolants that read Tnew/Ynew/
                                // Dynew (e.g. the cubic Hermite in BS3) would degenerate
                                // if we mutated those first. Then commit the new
                                // endpoint and refresh Dynew so any downstream
                                // Interpolate() over [T, Tnew] sees a consistent right
                                // endpoint.
                                using var yinterp = Vec.Rent(N);
                                Interpolate(_activeEvents[i].Time, yinterp);
                                Tnew = _activeEvents[i].Time;
                                Ynew.CopyFrom(yinterp);
                                f(Ynew, Tnew, Dynew);
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
                T = Tnew;
                Habs = _habsNext;

                if (terminate)
                {
                    Status = IVPStatus.EventTerminated;
                    break;
                }

                // handle max iterations
                if (Maxiter > 0 && niter++ > Maxiter)
                {
                    Status = IVPStatus.MaxStepsExceeded;

                    if (ThrowOnMaxIter)
                        throw new InvalidOperationException("maximum iterations exceeded");

                    break;
                }
            }

            interpolant?.Add(T, Y, Dy);

            Y.CopyTo(yf);

            // nothing else overwrote our status with a reason
            if (Status == IVPStatus.Initialized)
                Status = IVPStatus.Success;
        }

        private bool IsActiveEvent(Event e)
        {
            bool up     = e.LastValue <= 0 && e.NewValue >= 0;
            bool down   = e.LastValue >= 0 && e.NewValue <= 0;
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

                using var yinterp = Vec.Rent(N);
                using var finterp = Vec.Rent(N);

                InitInterpolant();
                Interpolate(tinterp, yinterp);
                f(yinterp, tinterp, finterp);
                interpolant?.Add(tinterp, yinterp, finterp);
                interpCount++;
            }

            return interpCount;
        }

        protected abstract (double, double) Step(IVPFunc f);

        protected abstract double SelectInitialStep(IVPFunc f, double t0, Vec y0,
            Vec f0, int direction);

        protected abstract void InitInterpolant();
        protected abstract void Interpolate(double x, Vec yout);
        protected abstract void Init();
        protected abstract void Cleanup();
    }
}
