/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

#nullable enable

using System;
using System.Collections.Generic;
using MechJebLib.Core.Functions;
using MechJebLib.Utils;

namespace MechJebLib.Primitives
{
    public class Hn : HBase<Vn>
    {
        public int N;

        private static readonly ObjectPool<Hn> _pool = new ObjectPool<Hn>(New, Clear);

        private static Hn New()
        {
            return new Hn();
        }

        public void Add(double time, double[] value, double[] inTangent, double[] outTangent)
        {
            _list[time] = new HFrame<Vn>(time, Allocate(value), Allocate(inTangent), Allocate(outTangent));
            MinTime     = Math.Min(MinTime, time);
            MaxTime     = Math.Max(MaxTime, time);
            RecomputeTangents(_list.IndexOfKey(time));
            LastLo = -1;
        }

        public void Add(double time, double[] value, double[] tangent)
        {
            if (_list.ContainsKey(time))
            {
                HFrame<Vn> temp = _list.Values[_list.IndexOfKey(time)];
                temp.Value      = Allocate(value);
                temp.OutTangent = Allocate(tangent);
                _list[time]     = temp;
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

        protected override Vn Allocate()
        {
            return Vn.Rent(N);
        }

        private Vn Allocate(IReadOnlyList<double> value)
        {
            var list = Vn.Rent(N);
            for (int i = 0; i < N; i++)
                list[i] = value[i];
            return list;
        }

        protected override Vn Allocate(Vn value)
        {
            var list = Vn.Rent(N);
            for (int i = 0; i < N; i++)
                list[i] = value[i];
            return list;
        }

        protected override void Subtract(Vn a, Vn b, ref Vn result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] - b[i];
        }

        protected override void Divide(Vn a, double b, ref Vn result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] / b;
        }

        protected override void Multiply(Vn a, double b, ref Vn result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] * b;
        }

        protected override void Addition(Vn a, Vn b, ref Vn result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] + b[i];
        }

        protected override Vn Interpolant(double x1, Vn y1, Vn yp1, double x2, Vn y2, Vn yp2, double x)
        {
            var ret = Vn.Rent(N);
            Interpolants.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x, N, ret);
            return ret;
        }

        private void DisposeKeyframe(HFrame<Vn> frame)
        {
            Vn.Return(frame.Value);
            Vn.Return(frame.InTangent);
            Vn.Return(frame.OutTangent);
        }

        private static void Clear(Hn h)
        {
            h.Clear();
        }

        public override void Clear()
        {
            for (int i = 0; i < _list.Count; i++)
            {
                DisposeKeyframe(_list.Values[i]);
            }

            MinTime = double.MaxValue;
            MaxTime = double.MinValue;
            LastLo  = -1;
            _list.Clear();
        }
    }
}
