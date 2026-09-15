/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    internal class CubicHermiteDoubleNode : InterpolantNode<double>
    {
        private static readonly ObjectPool<CubicHermiteDoubleNode> _pool = new ObjectPool<CubicHermiteDoubleNode>(New, Clear);

        public static CubicHermiteDoubleNode Rent(double t, double h, double y, double dy, double ynew, double dynew)
        {
            CubicHermiteDoubleNode node = _pool.Borrow();
            node.T = t;
            node.H = h;
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
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o._y = o._dy = o._ynew = o._dynew = 0;
            o.T = 0;
            o.H = 0;
        }

        // ReSharper disable NullableWarningSuppressionIsUsed
        private double _y;
        private double _dy;
        private double _ynew;
        private double _dynew;
        // ReSharper restore NullableWarningSuppressionIsUsed

        public override double Evaluate(double x) => Functions.Interpolants.CubicHermiteInterpolant(T, _y, _dy, T + H, _ynew, _dynew, x);

        public override void Dispose() => _pool.Release(this);
    }
}
