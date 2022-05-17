/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System.Collections.Generic;
using MechJebLib.Maths;
using MechJebLib.Utils;

#nullable enable

namespace MechJebLib.Structs
{
    public class Hn : HBase<List<double>>
    {
        public readonly int N;

        public Hn(int n)
        {
            N = n;
        }

        protected override List<double> Allocate()
        {
            return DoublePool.Pool.Rent(N);
        }

        protected override List<double> Allocate(List<double> value)
        {
            List<double> list = DoublePool.Pool.Rent(N);
            for (int i = 0; i < N; i++)
                list[i] = value[i];
            return list;
        }

        protected override void Subtract(List<double> a, List<double> b, ref List<double> result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] - b[i];
        }

        protected override void Divide(List<double> a, double b, ref List<double> result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] / b;
        }

        protected override void Multiply(List<double> a, double b, ref List<double> result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] * b;
        }

        protected override void Addition(List<double> a, List<double> b, ref List<double> result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] + b[i];
        }

        protected override List<double> Interpolant(double x1, List<double> y1, List<double> yp1, double x2, List<double> y2, List<double> yp2, double x)
        {
            List<double> foo = Utils.DoublePool.Pool.Rent(N);
            Functions.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x, N, foo);
            return foo;
        }

        private void DisposeKeyframe(HFrame<List<double>> frame)
        {
            DoublePool.Pool.Return(frame.InValue);
            DoublePool.Pool.Return(frame.OutValue);
            DoublePool.Pool.Return(frame.InTangent);
            DoublePool.Pool.Return(frame.OutTangent);
        }

        public override void Clear()
        {
            for(int i = 0; i < _list.Count; i++)
                DisposeKeyframe(_list.Values[i]);
            _list.Clear();
        }
    }
}
