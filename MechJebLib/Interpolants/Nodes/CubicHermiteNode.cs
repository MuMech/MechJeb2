/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    internal class CubicHermiteNode : InterpolantNode<Vec>
    {
        private static readonly ObjectPool<CubicHermiteNode> _pool = new ObjectPool<CubicHermiteNode>(New, Clear);

        public static CubicHermiteNode Rent(double t, double h, Vec y, Vec dy, Vec ynew, Vec dynew)
        {
            CubicHermiteNode node = _pool.Borrow();
            node.T = t;
            node.H = h;
            node._y = y.Dup();
            node._dy = dy.Dup();
            node._ynew = ynew.Dup();
            node._dynew = dynew.Dup();
            return node;
        }

        private CubicHermiteNode() { }

        private static CubicHermiteNode New() => new CubicHermiteNode();

        private static void Clear(CubicHermiteNode o)
        {
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o._y = o._dy = o._ynew = o._dynew = null!;
            o.T = 0;
            o.H = 0;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        private Vec _y = null!;
        private Vec _dy = null!;
        private Vec _ynew = null!;
        private Vec _dynew = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed

        public override Vec Evaluate(double x)
        {
            var yout = Vec.Rent(_y.Length);
            yout.CubicHermiteInterpolant(T, _y, _dy, T + H, _ynew, _dynew, x);
            return yout;
        }

        public override void Dispose()
        {
            _y.Dispose();
            _dy.Dispose();
            _ynew.Dispose();
            _dynew.Dispose();
            _pool.Release(this);
        }
    }
}
