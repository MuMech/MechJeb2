using System;
using System.Collections.Generic;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.ODE
{
    public class DenseOutput : IDisposable, IInterpolant
    {
        private static readonly ObjectPool<DenseOutput> _pool = new ObjectPool<DenseOutput>(New, Clear);

        private readonly List<DenseNode> _nodes = new List<DenseNode>();
        private int _lastIndex = -1;
        private double _minT = double.NaN;
        private double _maxT = double.NaN;
        public double MaxT { get; private set; } = double.NegativeInfinity;
        public double MinT { get; private set; } = double.PositiveInfinity;
        public int N = -1;
        public int Direction;

        public static DenseOutput Rent() => _pool.Borrow();

        private static DenseOutput New() => new DenseOutput();

        private DenseOutput() { }

        private static void Clear(DenseOutput o)
        {
            o.N = -1;
            o._minT = double.NaN;
            o._maxT = double.NaN;
            o.MinT = double.PositiveInfinity;
            o.MaxT = double.NegativeInfinity;
            o.Direction = 0;
            o._lastIndex = -1;
        }

        public void Append(DenseNode n)
        {
            _nodes.Add(n);
            if (N < 1)
                N = n.N;
            else if (N != n.N)
                throw new ArgumentException($"[MechJeb2] DenseOutput.Add(): node lengths do not match {N} != {n.N}");
            Direction = Math.Sign(n.H);
            if (Direction * n.T < Direction * _minT || !IsFinite(_minT))
                _minT = n.T;
            if (Direction * n.T > Direction * _maxT || !IsFinite(_maxT))
                _maxT = n.T;

            double min = Direction < 0 ? n.T + n.H : n.T;
            if (min < MinT)
                MinT = min;
            double max = Direction > 0 ? n.T + n.H : n.T;
            if (max > MaxT)
                MaxT = max;
        }

        private int FindIndex(double x)
        {
            if (Direction * x < Direction * _minT)
                return 0;
            if (Direction * x > Direction * _maxT)
                return _nodes.Count - 1;

            if (_lastIndex > 0 && Direction * x > Direction * _nodes[_lastIndex].T)
            {
                if (Direction * x < Direction * _nodes[_lastIndex + 1].T)
                    return _lastIndex;

                if (Direction * x >= Direction * _nodes[_lastIndex + 1].T && Direction * x < Direction * _nodes[_lastIndex + 2].T)
                    return _lastIndex + 1;
            }

            int lo = 0;
            int hi = _nodes.Count - 1;

            while (lo <= hi)
            {
                int i     = lo + ((hi - lo) >> 1);
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

        public Vec Evaluate(double x)
        {
            int i = FindIndex(x);

            var yout = Vec.Rent(N);

            _nodes[i].Evaluate(x, yout);

            _lastIndex = i;

            return yout;
        }

        public void Dispose()
        {
            foreach (DenseNode n in _nodes)
                n.Dispose();
            _nodes.Clear();
            _pool.Release(this);
        }
    }
}
