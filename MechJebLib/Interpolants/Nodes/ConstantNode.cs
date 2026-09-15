/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Primitives;

namespace MechJebLib.Interpolants
{
    /// <summary>
    ///     This is a fake "interpolant" for zero-length t0 == tf "integration".
    /// </summary>
    public class ConstantNode : InterpolantNode<Vec>
    {
        private readonly Vec _y;

        public ConstantNode(double t, Vec y)
        {
            T = t;
            _y = y.Dup();
            H = 0;
        }

        public override Vec Evaluate(double t)
        {
            var yout = Vec.Rent(_y.Length);
            yout.CopyFrom(_y);
            return yout;
        }

        public override void Dispose() => _y.Dispose();
    }
}
