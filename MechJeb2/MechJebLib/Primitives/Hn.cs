/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

#nullable enable

using MechJebLib.Maths;
using MechJebLib.Utils;

namespace MechJebLib.Primitives
{
    public class Hn : HBase<DD>
    {
        public int N;

        private static readonly ObjectPool<Hn> _pool = new ObjectPool<Hn>(New, Clear);

        private static Hn New()
        {
            return new Hn();
        }

        public static Hn Get(int n)
        {
            Hn h = _pool.Get();
            h.N = n;
            h.Clear();
            return h;
        }

        public override void Dispose()
        {
            base.Dispose();
            _pool.Return(this);
        }

        protected override DD Allocate()
        {
            return DD.Rent(N);
        }

        protected override DD Allocate(DD value)
        {
            var list = DD.Rent(N);
            for (int i = 0; i < N; i++)
                list[i] = value[i];
            return list;
        }

        protected override void Subtract(DD a, DD b, ref DD result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] - b[i];
        }

        protected override void Divide(DD a, double b, ref DD result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] / b;
        }

        protected override void Multiply(DD a, double b, ref DD result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] * b;
        }

        protected override void Addition(DD a, DD b, ref DD result)
        {
            for (int i = 0; i < N; i++)
                result[i] = a[i] + b[i];
        }

        protected override DD Interpolant(double x1, DD y1, DD yp1, double x2, DD y2, DD yp2, double x)
        {
            var ret = DD.Rent(N);
            Functions.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x, N, ret);
            return ret;
        }

        private void DisposeKeyframe(HFrame<DD> frame)
        {
            DD.Return(frame.Value);
            DD.Return(frame.InTangent);
            DD.Return(frame.OutTangent);
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
            _list.Clear();
        }
    }
}
