/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using MechJebLib.Maths;
using MechJebLib.Utils;

#nullable enable

namespace MechJebLib.Structs
{
    public class Hn : HBase<DDArray>
    {
        public int N;

        private static readonly ObjectPool<Hn> _pool = new ObjectPool<Hn>(New);

        private static Hn New()
        {
            return new Hn();
        }

        public static Hn Get(int n)
        {
            Hn h = _pool.Get();
            h.N = n;
            return h;
        }

        public override void Dispose()
        {
            base.Dispose();
            _pool.Return(this);
        }

        protected override DDArray Allocate()
        {
            return DDArray.Rent(N);
        }

        protected override DDArray Allocate(DDArray value)
        {
            DDArray list = DDArray.Rent(N);
            for (int i = 0; i < N; i++)
                list[i] = value[i];
            return list;
        }

        protected override void Subtract(DDArray a, DDArray b, ref DDArray result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] - b[i];
        }

        protected override void Divide(DDArray a, double b, ref DDArray result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] / b;
        }

        protected override void Multiply(DDArray a, double b, ref DDArray result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] * b;
        }

        protected override void Addition(DDArray a, DDArray b, ref DDArray result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] + b[i];
        }

        protected override DDArray Interpolant(double x1, DDArray y1, DDArray yp1, double x2, DDArray y2, DDArray yp2, double x)
        {
            DDArray foo = Utils.DDArray.Rent(N);
            Functions.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x, N, foo);
            return foo;
        }

        private void DisposeKeyframe(HFrame<DDArray> frame)
        {
            DDArray.Return(frame.InValue);
            DDArray.Return(frame.OutValue);
            DDArray.Return(frame.InTangent);
            DDArray.Return(frame.OutTangent);
        }

        public override void Clear()
        {
            for(int i = 0; i < _list.Count; i++)
                DisposeKeyframe(_list.Values[i]);
            _list.Clear();
        }
    }
}
