/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;
using MechJebLib.Utils;

namespace MechJebLib.Interpolants
{
    /// <summary>
    ///     This is a fake "interpolant" for zero-length t0 == tf "integration".
    /// </summary>
    public class ConstantVecNode : InterpolantNode<Vec>
    {
        private static readonly ObjectPool<ConstantVecNode> _pool = new ObjectPool<ConstantVecNode>(New, Clear);

        // ReSharper disable once NullableWarningSuppressionIsUsed
        private Vec _y = null!;

        private ConstantVecNode()
        {
        }

        public static ConstantVecNode Rent(Vec y)
        {
            ConstantVecNode node = _pool.Borrow();
            node._y = y.Dup();
            return node;
        }

        private static ConstantVecNode New() => new ConstantVecNode();

        private static void Clear(ConstantVecNode o)
        {
            o.LeftT = o.RightT = 0;
            // ReSharper disable once NullableWarningSuppressionIsUsed
            o._y = null!;
        }

        public override Vec Evaluate(double t)
        {
            var yout = Vec.Rent(_y.Length);
            yout.CopyFrom(_y);
            return yout;
        }

        public override void Dispose()
        {
            _y.Dispose();
            _pool.Release(this);
        }
    }
}
