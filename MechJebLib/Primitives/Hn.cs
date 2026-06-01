/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Interpolants;
using MechJebLib.Utils;
using static System.Math;

namespace MechJebLib.Primitives
{
    public class Hn : HBase<Vec>, IInterpolant
    {
        public int N;

        private static readonly ObjectPool<Hn> _pool = new ObjectPool<Hn>(New, Clear);

        private static Hn New() => new Hn();

        public void Add(double time, double[] value, double[] inTangent, double[] outTangent)
        {
            _list[time] = new HFrame<Vec>(time, Allocate(value), Allocate(inTangent), Allocate(outTangent));
            MinT = Min(MinT, time);
            MaxT = Max(MaxT, time);
            RecomputeTangents(_list.IndexOfKey(time));
            LastLo = -1;
        }

        public void Add(double time, double[] value, double[] tangent)
        {
            if (_list.ContainsKey(time))
            {
                HFrame<Vec> temp = _list.Values[_list.IndexOfKey(time)];
                temp.Value = Allocate(value);
                temp.OutTangent = Allocate(tangent);
                _list[time] = temp;
            }
            else
            {
                Add(time, value, tangent, tangent);
            }
        }

        public static Hn Get(int n)
        {
            Hn h = _pool.Borrow();
            h.N = n;
            h.Clear();
            return h;
        }

        public override void Dispose()
        {
            base.Dispose();
            _pool.Release(this);
        }

        protected override Vec Allocate() => Vec.Rent(N, true);

        private Vec Allocate(IReadOnlyList<double> value)
        {
            var list = Vec.Rent(N);
            for (int i = 0; i < N; i++)
                list[i] = value[i];
            return list;
        }

        protected override Vec Allocate(Vec value)
        {
            var list = Vec.Rent(N);
            for (int i = 0; i < N; i++)
                list[i] = value[i];
            return list;
        }

        protected override void Subtract(Vec a, Vec b, ref Vec result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] - b[i];
        }

        protected override void Divide(Vec a, double b, ref Vec result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] / b;
        }

        protected override void Multiply(Vec a, double b, ref Vec result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] * b;
        }

        protected override void Addition(Vec a, Vec b, ref Vec result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] + b[i];
        }

        protected override Vec Interpolant(double x1, Vec y1, Vec yp1, double x2, Vec y2, Vec yp2, double x) =>
            Vec.Rent(N).CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x);

        private void DisposeKeyframe(HFrame<Vec> frame)
        {
            frame.Value.Dispose();
            frame.InTangent.Dispose();
            frame.OutTangent.Dispose();
        }

        private static void Clear(Hn h) => h.Clear();

        public override void Clear()
        {
            for (int i = 0; i < _list.Count; i++)
            {
                DisposeKeyframe(_list.Values[i]);
            }

            MinT = double.MaxValue;
            MaxT = double.MinValue;
            LastLo = -1;
            _list.Clear();
        }
    }
}
