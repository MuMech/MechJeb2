/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Functions;
using MechJebLib.Utils;

namespace MechJebLib.Primitives
{
    public class H3 : HBase<V3>
    {
        private static readonly ObjectPool<H3> _pool = new ObjectPool<H3>(New, Clear);

        private H3()
        {
        }

        private static H3 New() => new H3();

        public static H3 Get()
        {
            H3 h = _pool.Borrow();
            return h;
        }

        public override void Dispose()
        {
            base.Dispose();
            _pool.Release(this);
        }

        protected override V3 Allocate() => V3.zero;

        protected override V3 Allocate(V3 value) => value;

        protected override void Subtract(V3 a, V3 b, ref V3 result) => result = a - b;

        protected override void Divide(V3 a, double b, ref V3 result) => result = a / b;

        protected override void Multiply(V3 a, double b, ref V3 result) => result = a * b;

        protected override void Addition(V3 a, V3 b, ref V3 result) => result = a + b;

        protected override V3 Interpolant(double x1, V3 y1, V3 yp1, double x2, V3 y2, V3 yp2, double x) =>
            Interpolants.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x);

        private static void Clear(H3 h) => h.Clear();
    }
}
