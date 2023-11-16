/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

#nullable enable

using MechJebLib.Functions;
using MechJebLib.Utils;

namespace MechJebLib.Primitives
{
    public class H1 : HBase<double>
    {
        private static readonly ObjectPool<H1> _pool = new ObjectPool<H1>(New, Clear);

        private H1()
        {
        }

        private static H1 New() => new H1();

        public static H1 Get(bool unityCompat = false)
        {
            H1 h = _pool.Borrow();
            h.UnityCompat = unityCompat;
            return h;
        }

        public override void Dispose()
        {
            base.Dispose();
            _pool.Release(this);
        }

        protected override double Allocate() => 0.0;

        protected override double Allocate(double value) => value;

        protected override void Subtract(double a, double b, ref double result) => result = a - b;

        protected override void Divide(double a, double b, ref double result) => result = a / b;

        protected override void Multiply(double a, double b, ref double result) => result = a * b;

        protected override void Addition(double a, double b, ref double result) => result = a + b;

        protected override double Interpolant(double x1, double y1, double yp1, double x2, double y2, double yp2, double x) =>
            Interpolants.CubicHermiteInterpolant(x1, y1, yp1, x2, y2, yp2, x);

        private static void Clear(H1 h) => h.Clear();
    }
}
