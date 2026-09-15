/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.Interpolants
{
    public class Interpolant<T>
    {
        // list of interpolant frames
        private readonly List<InterpolantNode<T>> _nodes = new List<InterpolantNode<T>>();

        // tracking of last index from FindIndex() for march-order access acceleration
        private int _lastIndex = -1;

        // nodes[0].T (in march-order)
        private double _firstT = double.NaN;

        // nodes[^1].T (in march-order)
        private double _lastT = double.NaN;

        // actual MaxT (for Direction=+1 this is _lastT plus a delta, for Direction=-1 this is _firstT)
        public double MaxT { get; private set; } = double.NegativeInfinity;

        // actual MinT (for Direction=-1 this is _lastT minus a delta, for Direction=+1 this is _firstT)
        public double MinT { get; private set; } = double.PositiveInfinity;

        // direction is +1 or -1 and for -1 nodes[].T will be in reverse order
        public int Direction;

        public bool IsEmpty => _nodes.Count == 0;

        protected Interpolant() { }

        protected static void Clear(Interpolant<T> o)
        {
            o._firstT = double.NaN;
            o._lastT = double.NaN;
            o.MinT = double.PositiveInfinity;
            o.MaxT = double.NegativeInfinity;
            o.Direction = 0;
            o._lastIndex = -1;
        }

        public void Clear() => Clear(this);

        public void Append(InterpolantNode<T> n, double maxT)
        {
            _nodes.Add(n);
            Direction = Math.Sign(n.H);
            if (Direction * n.T < Direction * _firstT || !IsFinite(_firstT))
                _firstT = n.T;
            if (Direction * n.T > Direction * _lastT || !IsFinite(_lastT))
                _lastT = n.T;

            double min = Direction < 0 ? maxT : n.T;
            if (min < MinT)
                MinT = min;
            double max = Direction > 0 ? maxT : n.T;
            if (max > MaxT)
                MaxT = max;
        }

        private int FindIndex(double x)
        {
            if (Direction * x <= Direction * _firstT)
                return 0;
            if (Direction * x >= Direction * _lastT)
                return _nodes.Count - 1;

            if (_lastIndex > 0 && Direction * x > Direction * _nodes[_lastIndex].T)
            {
                if (Direction * x < Direction * _nodes[_lastIndex + 1].T)
                    return _lastIndex;

                if (_lastIndex + 2 > _nodes.Count - 1)
                    return _lastIndex + 1;

                if (Direction * x >= Direction * _nodes[_lastIndex + 1].T && Direction * x < Direction * _nodes[_lastIndex + 2].T)
                    return _lastIndex + 1;
            }

            int lo = 0;
            int hi = _nodes.Count - 1;

            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = Direction * x.CompareTo(_nodes[i].T);

                if (order == 0)
                    return i;

                if (order > 0)
                    lo = i + 1;
                else
                    hi = i - 1;
            }

            return lo - 1; // lo is the hi value here
        }

        public T Evaluate(double x)
        {
            int i = FindIndex(x);

            T yout = _nodes[i].Evaluate(x);

            _lastIndex = i;

            return yout;
        }

        public virtual void Dispose()
        {
            foreach (InterpolantNode<T> n in _nodes)
                n.Dispose();
            _nodes.Clear();
        }
    }

    public abstract class InterpolantNode<Typ> : IDisposable
    {
        public double T; // left endpoint
        public double H; // signed step (Habs * Direction)

        public abstract Typ Evaluate(double t);

        public abstract void Dispose();
    }
}
