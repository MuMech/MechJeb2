/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Interpolants;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.InterpolantsTests
{
    public class CubicHermiteNodeTests
    {
        // A zero-length PSG phase builds nodes with t1 == t2 (and non-finite derivatives), which used to evaluate 0/0 = NaN.
        [Fact]
        public void ZeroWidthVecNodeIsConstant()
        {
            using var y = Vec.Rent(2);
            using var dy = Vec.Rent(2);
            using var ynew = Vec.Rent(2);
            using var dynew = Vec.Rent(2);
            y[0] = 1.0;
            y[1] = 2.0;
            dy[0] = dy[1] = double.NaN;
            ynew[0] = 1.0;
            ynew[1] = 2.0;
            dynew[0] = dynew[1] = double.PositiveInfinity;

            using var interpolant = VecInterpolant.Rent();
            interpolant.Append(CubicHermiteVecNode.Rent(0.5, 0, y, dy, ynew, dynew), 0.5, 0.5);

            using Vec yout = interpolant.Evaluate(0.5);

            yout[0].ShouldEqual(1.0, 0);
            yout[1].ShouldEqual(2.0, 0);
        }

        // h is nonzero but below the roundoff of t, so t + h == t
        [Fact]
        public void SubRoundoffWidthDoubleNodeIsConstant()
        {
            using var interpolant = DoubleInterpolant.Rent();
            interpolant.Append(CubicHermiteDoubleNode.Rent(0.5, 1e-17, 3.0, double.NaN, 3.0, double.NaN), 0.5, 0.5);

            interpolant.Evaluate(0.5).ShouldEqual(3.0, 0);
        }
    }
}
