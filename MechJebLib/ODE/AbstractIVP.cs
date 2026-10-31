/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Threading;
using MechJebLib.Interpolants;
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
        public enum IVPStatus { Initialized, Success, EventTerminated, MaxEvalsExceeded, NonFiniteAtMinStep, Failed }

        private readonly List<Event> _activeEvents = new List<Event>();

        private Func<Vec, double, AbstractIVP, double> _eventFunc = null!;

        // the caller's RHS, called only through _countedRhs
        private IVPFunc _f = null!;

        protected double HabsNext;
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
        ///     Minimum h step (might be violated on the last step or before an event).
        /// </summary>
        public double Hmin { get; set; } = 0;

        /// <summary>
        ///     Maximum h step.
        /// </summary>
        public double Hmax { get; set; } = double.PositiveInfinity;

        /// <summary>
        ///     Maximum number of RHS evaluations, counting rejected steps (zero or less for no limit).  Integration stops at the
        ///     last accepted step before a step attempt that would go over it.
        /// </summary>
        public int MaxEvals { get; set; } = 12000;

        /// <summary>
        ///     Number of RHS evaluations made by the last Solve.
        /// </summary>
        public int Evals { get; private set; }

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
        ///     Throw exception when MaxEvals is hit, otherwise integration ends at the last accepted step with Status
        ///     MaxEvalsExceeded.
        /// </summary>
        public bool ThrowOnMaxEvals { get; set; } = true;

        /// <summary>
        ///     Throw exception when the step size would go below the minimum, or the step is still NaN or infinite at the
        ///     minimum step size.  Otherwise a finite step at the minimum step size is accepted whatever its error, and a
        ///     non-finite one ends the integration at the last accepted step with Status NonFiniteAtMinStep (T, yf and the
        ///     interpolant all stop there).
        /// </summary>
        public bool ThrowOnMinStep { get; set; } = true;

        public IVPStatus Status { get; set; } = IVPStatus.Success;

        public CancellationToken CancellationToken { get; set; }

        protected AbstractIVP()
        {
            _eventFunctionDelegate = EventFuncWrapper;
            _countedRhs            = CountedRhs;
        }

        private readonly Func<double, object?, double> _eventFunctionDelegate;

        // every RHS evaluation goes through here so MaxEvals can bound the step loop, cached so a Solve doesn't allocate
        private readonly IVPFunc _countedRhs;

        private void CountedRhs(Vec y, double t, Vec dy)
        {
            Evals++;
            _f(y, t, dy);
        }

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
            Interpolant<Vec>? interpolant = null,
            IReadOnlyList<Event>? events = null)
        {
            try
            {
                N = y0.Count;
                Y = Vec.Rent(N);
                Dy = Vec.Rent(N);
                Ynew = Vec.Rent(N);
                Dynew = Vec.Rent(N);

                _f = f;

                Init();
                _Solve(_countedRhs, y0, yf, t0, tf, interpolant, events);
            }
            catch (Exception)
            {
                if (Status == IVPStatus.Initialized)
                    Status = IVPStatus.Failed;
                throw;
            }
            finally
            {
                _f = null!;
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
            Interpolant<Vec>? interpolant,
            IReadOnlyList<Event>? events)
        {
            Status = IVPStatus.Initialized;

            Direction = t0 != tf ? Math.Sign(tf - t0) : 1;
            MaxStep = Hmax;
            MinStep = Hmin;

            T = t0;
            Y.CopyFrom(y0);
            Evals = 0;

            // no step can be taken from a non-finite start, however small, so these throw whatever ThrowOnMinStep says
            if (!Y.IsFinite())
                throw new ArgumentException($"Initial state is not finite at t={T}");

            f(Y, T, Dy);

            if (!Dy.IsFinite())
                throw new InvalidOperationException($"RHS is not finite at the initial state at t={T}");

            Habs = Hstart > 0 ? Hstart : SelectInitialStep(f, T, Y, Dy, Direction);
            Habs = Clamp(Habs, MinStep, MaxStep);

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

                // no step could be taken from T (Status says why): Y, T and the interpolant stay at the last accepted step
                if (!Step(f))
                    break;

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
                                // take a snapshot of the full interpolant with all values
                                interpolant?.Append(SnapshotStep(), T, _activeEvents[i].Time);
                                // evaluate the interpolant and update Ynew, Tnew, Dynew for next step
                                using var yinterp = Vec.Rent(N);
                                Interpolate(_activeEvents[i].Time, yinterp);
                                Ynew.CopyFrom(yinterp);
                                Tnew = _activeEvents[i].Time;
                                f(Ynew, Tnew, Dynew);
                                break;
                            }
                        }
                    }

                    for (int i = 0; i < events.Count; i++)
                        events[i].LastValue = events[i].NewValue;
                }

                if (!terminate)
                    interpolant?.Append(SnapshotStep(), T, Tnew);

                // take a step
                Y.CopyFrom(Ynew);
                Dy.CopyFrom(Dynew);
                T = Tnew;
                Habs = HabsNext;

                if (terminate)
                {
                    Status = IVPStatus.EventTerminated;
                    break;
                }
            }

            if (t0 == tf)
                interpolant?.Append(ConstantVecNode.Rent(Y), T, T);

            Y.CopyTo(yf);

            // nothing else overwrote our status with a reason
            if (Status == IVPStatus.Initialized)
                Status = IVPStatus.Success;
        }

        private bool IsActiveEvent(Event e)
        {
            bool up = e.LastValue <= 0 && e.NewValue >= 0;
            bool down = e.LastValue >= 0 && e.NewValue <= 0;
            bool either = up || down;
            return (up && e.Direction > 0) || (down && e.Direction < 0) || (either && e.Direction == 0);
        }

        /// <summary>
        ///     Take one step from T, setting Habs, HabsNext, Tnew, Ynew and Dynew.
        /// </summary>
        /// <returns>false if no step can be taken, with Status set to say why</returns>
        protected abstract bool Step(IVPFunc f);

        protected abstract double SelectInitialStep(IVPFunc f, double t0, Vec y0,
            Vec f0, int direction);

        protected abstract void                 InitInterpolant();
        protected abstract InterpolantNode<Vec> SnapshotStep();
        protected abstract void                 Interpolate(double x, Vec yout);
        protected abstract void                 Init();
        protected abstract void                 Cleanup();
    }
}
