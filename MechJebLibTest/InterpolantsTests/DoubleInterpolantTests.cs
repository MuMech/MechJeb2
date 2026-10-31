/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Interpolants;
using Xunit;

namespace MechJebLibTest.InterpolantsTests
{
    public class DoubleInterpolantTests
    {
        [Fact]
        // a NaN input (an atmospheric pressure or Mach number gone NaN) searched down to _nodes[-1] and threw, which failed the
        // Δv simulation of the whole vessel
        public void NaNInputDoesNotThrow()
        {
            using var h = DoubleInterpolant.Rent();

            // the shape LoadFromFloatCurve builds for the keys (0, 1) (1, 2) (2, 0): constant ends around Hermite segments
            h.Append(ConstantDoubleNode.Rent(1), 0, 0);
            h.Append(CubicHermiteDoubleNode.Rent(0, 1, 1, 0, 2, 0), 0, 1);
            h.Append(CubicHermiteDoubleNode.Rent(1, 1, 2, 0, 0, 0), 1, 2);
            h.Append(ConstantDoubleNode.Rent(0), 2, 2);

            Assert.Equal(2, h.Evaluate(1), 9);
            Assert.Equal(1, h.Evaluate(-5), 9);
            Assert.Equal(0, h.Evaluate(5), 9);

            h.Evaluate(double.NaN);
        }
    }
}
