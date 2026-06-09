using System;
using System.Collections.Generic;
using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using MechJebLib.Utils;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.ODE
{
    public class DenseOutput : IInterpolant
    {
        private static readonly ObjectPool<DenseOutput> _pool = new ObjectPool<DenseOutput>(New, Clear);

        // list of interpolant frames
        private readonly List<DenseNode> _nodes = new List<DenseNode>();

        // tracking of last index from FindIndex() for march-order access acceleration
        private int _lastIndex = -1;

        // nodes[0].T (in march order)
        private double _firstT = double.NaN;

        // nodes[^1].T (in march order)
        private double _lastT = double.NaN;

        // actual MaxT (for Direction=+1 this is _lastT plus a delta, for Direction=-1 this is _firstT)
        public double MaxT { get; private set; } = double.NegativeInfinity;

        // actual MinT (for Direction=-1 this is _lastT minus a delta, for Direction=+1 this is _firstT)
        public double MinT { get; private set; } = double.PositiveInfinity;

        // size of the Vec being interpolated
        public int N = -1;

        // direction is +1 or -1 and for -1 nodes[].T will be in reverse order
        public int Direction;

        public static DenseOutput Rent() => _pool.Borrow();

        private static DenseOutput New() => new DenseOutput();

        private DenseOutput() { }

        private static void Clear(DenseOutput o)
        {
            o.N = -1;
            o._firstT = double.NaN;
            o._lastT = double.NaN;
            o.MinT = double.PositiveInfinity;
            o.MaxT = double.NegativeInfinity;
            o.Direction = 0;
            o._lastIndex = -1;
        }

        public void Append(DenseNode n, double maxT)
        {
            _nodes.Add(n);
            if (N < 1)
                N = n.N;
            else if (N != n.N)
                throw new ArgumentException($"[MechJeb2] DenseOutput.Add(): node lengths do not match {N} != {n.N}");
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
            if (Direction * x < Direction * _firstT)
                return 0;
            if (Direction * x > Direction * _lastT)
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
