/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.ODE;
using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    internal class Tsit5Node : InterpolantNode<Vec>
    {
        private static readonly ObjectPool<Tsit5Node> _pool = new ObjectPool<Tsit5Node>(New, Clear);

        public static Tsit5Node Rent(double t, double h, Vec y, Vec k1, Vec k2, Vec k3, Vec k4, Vec k5, Vec k6, Vec k7)
        {
            Tsit5Node node = _pool.Borrow();
            node.T = t;
            node.H = h;
            node._y = y.Dup();
            node._k1 = k1.Dup();
            node._k2 = k2.Dup();
            node._k3 = k3.Dup();
            node._k4 = k4.Dup();
            node._k5 = k5.Dup();
            node._k6 = k6.Dup();
            node._k7 = k7.Dup();
            return node;
        }

        private Tsit5Node() { }

        private static Tsit5Node New() => new Tsit5Node();

        private static void Clear(Tsit5Node o)
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o._y = o._k1 = o._k2 = o._k3 = o._k4 = o._k5 = o._k6 = o._k7 = null!;
            o.T = 0;
            o.H = 0;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        private Vec _y = null!;
        private Vec _k1 = null!;
        private Vec _k2 = null!;
        private Vec _k3 = null!;
        private Vec _k4 = null!;
        private Vec _k5 = null!;
        private Vec _k6 = null!;
        private Vec _k7 = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed


        public override Vec Evaluate(double x)
        {
            var yout = Vec.Rent(_y.Length);
            Tsit5Math.Interpolate(x, T, H, _y, _k1, _k2, _k3, _k4, _k5, _k6, _k7, yout);
            return yout;
        }

        public override void Dispose()
        {
            _y.Dispose();
            _k1.Dispose();
            _k2.Dispose();
            _k3.Dispose();
            _k4.Dispose();
            _k5.Dispose();
            _k6.Dispose();
            _k7.Dispose();
            _pool.Release(this);
        }
    }
}
