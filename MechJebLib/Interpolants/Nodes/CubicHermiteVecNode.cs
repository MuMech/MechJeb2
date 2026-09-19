/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    internal class CubicHermiteVecNode : InterpolantNode<Vec>
    {
        private static readonly ObjectPool<CubicHermiteVecNode> _pool = new ObjectPool<CubicHermiteVecNode>(New, Clear);

        public static CubicHermiteVecNode Rent(double t, double h, Vec y, Vec dy, Vec ynew, Vec dynew)
        {
            CubicHermiteVecNode node = _pool.Borrow();
            node._t = t;
            node._h = h;
            node._y = y.Dup();
            node._dy = dy.Dup();
            node._ynew = ynew.Dup();
            node._dynew = dynew.Dup();
            return node;
        }

        private CubicHermiteVecNode() { }

        private static CubicHermiteVecNode New() => new CubicHermiteVecNode();

        private static void Clear(CubicHermiteVecNode o)
        {
            o.LeftT = o.RightT = 0;
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o._y = o._dy = o._ynew = o._dynew = null!;
            o._t = 0;
            o._h = 0;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        private double _t;
        private double _h;
        private Vec _y = null!;
        private Vec _dy = null!;
        private Vec _ynew = null!;
        private Vec _dynew = null!;
        // ReSharper restore NullableWarningSuppressionIsUsed

        public override Vec Evaluate(double x)
        {
            var yout = Vec.Rent(_y.Length);
            yout.CubicHermiteInterpolant(_t, _y, _dy, _t + _h, _ynew, _dynew, x);
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
