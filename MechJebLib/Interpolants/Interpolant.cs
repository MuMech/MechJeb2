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

        // nodes[0].LeftT (in march-order)
        private double _firstT = double.NaN;

        // nodes[^1].LeftT (in march-order)
        private double _lastT = double.NaN;

        // actual MaxT
        public double MaxT { get; private set; } = double.NegativeInfinity;

        // actual MinT
        public double MinT { get; private set; } = double.PositiveInfinity;

        // direction is +1 or -1 and for -1 nodes[].LeftT will be in reverse order
        private int _direction;

        public bool IsEmpty => _nodes.Count == 0;

        protected Interpolant() { }

        protected static void Clear(Interpolant<T> o)
        {
            foreach (InterpolantNode<T> n in o._nodes)
                n.Dispose();
            o._nodes.Clear();
            o._firstT = double.NaN;
            o._lastT = double.NaN;
            o.MinT = double.PositiveInfinity;
            o.MaxT = double.NegativeInfinity;
            o._direction = 0;
            o._lastIndex = -1;
        }

        public void Clear() => Clear(this);

        public void Append(InterpolantNode<T> n, double leftT, double rightT)
        {
            n.LeftT = leftT;
            n.RightT = rightT;

            _nodes.Add(n);

            int newDirection = Math.Sign(rightT - leftT);
            if (newDirection != 0)
            {
                if (_direction != 0 && newDirection != _direction)
                    throw new InvalidOperationException("[MechJeb] internal error: appending an interpolant node that changes interpolant direction");
                _direction = newDirection;
            }

            if (!IsFinite(_firstT))
                _firstT = leftT;
            if (!IsFinite(_lastT))
                _lastT = rightT;
            if (_direction * leftT < _direction * _firstT)
                _firstT = leftT;
            if (_direction * leftT > _direction * _lastT)
                _lastT = leftT;

            double max = _direction > 0 ? rightT : leftT;
            if (max > MaxT)
                MaxT = max;

            double min = _direction > 0 ? leftT : rightT;
            if (min < MinT)
                MinT = min;
        }

        private int FindIndex(double x)
        {
            if (IsEmpty)
                throw new InvalidOperationException("[MechJeb] internal error: interpolant is empty.");

            int direction = _direction != 0 ? _direction : 1;

            if (direction * x <= direction * _nodes[0].RightT)
                return 0;
            if (direction * x >= direction * _nodes[_nodes.Count - 1].LeftT)
                return _nodes.Count - 1;

            if (_lastIndex > 0 && direction * x > direction * _nodes[_lastIndex].LeftT)
            {
                if (direction * x < direction * _nodes[_lastIndex].RightT)
                    return _lastIndex;

                if (direction * x >= direction * _nodes[_lastIndex + 1].LeftT && direction * x < direction * _nodes[_lastIndex + 1].RightT)
                    return _lastIndex + 1;
            }

            int lo = 0;
            int hi = _nodes.Count - 1;

            while (lo <= hi)
            {
                int i = lo + ((hi - lo) >> 1);
                int order = direction * x.CompareTo(_nodes[i].LeftT);

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

        public virtual void Dispose() => Clear(this);
    }

    public abstract class InterpolantNode<Typ> : IDisposable
    {
        public double LeftT;
        public double RightT;

        public abstract Typ Evaluate(double t);

        public abstract void Dispose();
    }
}
