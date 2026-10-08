/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Utils;
using static System.FormattableString;

namespace MechJebLib.Interpolants
{
    internal class CubicHermiteDoubleNode : InterpolantNode<double>
    {
        private static readonly ObjectPool<CubicHermiteDoubleNode> _pool = new ObjectPool<CubicHermiteDoubleNode>(New, Clear);

        public static CubicHermiteDoubleNode Rent(double t, double h, double y, double dy, double ynew, double dynew)
        {
            CubicHermiteDoubleNode node = _pool.Borrow();
            node._t = t;
            node._h = h;
            node._y = y;
            node._dy = dy;
            node._ynew = ynew;
            node._dynew = dynew;
            return node;
        }

        private CubicHermiteDoubleNode() { }

        private static CubicHermiteDoubleNode New() => new CubicHermiteDoubleNode();

        private static void Clear(CubicHermiteDoubleNode o)
        {
            o.LeftT = o.RightT = 0;
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o._y = o._dy = o._ynew = o._dynew = 0;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        private double _t;
        private double _h;
        private double _y;
        private double _dy;
        private double _ynew;
        private double _dynew;
        // ReSharper restore NullableWarningSuppressionIsUsed

        // a zero-width node would interpolate 0/0, so it degenerates to a constant
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        public override double Evaluate(double x) =>
            _t + _h == _t ? _y : Functions.Interpolants.CubicHermiteInterpolant(_t, _y, _dy, _t + _h, _ynew, _dynew, x);

        public override void Dispose() => _pool.Release(this);

        public override string ToString() =>
            Invariant(
                $"[CubicHermiteDoubleNode leftT={LeftT:G17} rightT={RightT:G17} t={_t:G17} h={_h:G17} y={_y:G17} dy={_dy:G17} ynew={_ynew:G17} dynew={_dynew:G17}]");
    }
}
