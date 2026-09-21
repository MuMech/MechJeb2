/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    /// <summary>
    ///     This is used to reproduce the unity out-of-bounds behavior of FloatCurves and to
    ///     reproduce a FloatCurve with a single frame.
    /// </summary>
    public class ConstantDoubleNode : InterpolantNode<double>
    {
        private static readonly ObjectPool<ConstantDoubleNode> _pool = new ObjectPool<ConstantDoubleNode>(New, Clear);

        private double _y;

        private ConstantDoubleNode() { }

        public static ConstantDoubleNode Rent(double y)
        {
            ConstantDoubleNode node = _pool.Borrow();
            node._y = y;
            return node;
        }

        private static ConstantDoubleNode New() => new ConstantDoubleNode();

        private static void Clear(ConstantDoubleNode o)
        {
            o.LeftT = o.RightT = 0;
            o._y = 0;
        }

        public override double Evaluate(double t) => _y;

        public override void Dispose() { _pool.Release(this); }
    }
}
