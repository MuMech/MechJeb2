/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Rootfinding;
using Xunit;

namespace MechJebLibTest.RootfindingTests
{
    public class BisectionTests
    {
        [Fact]
        private void SingularityTest()
        {
            double x, fx;

            (x, fx) = Bisection.Solve((t, o) => 1.0 / t, -1.0, 1.0, null);
            x.ShouldEqual(-4.9406564584124654E-324, 0);
            fx.ShouldEqual(double.NegativeInfinity);
            (x, fx) = Bisection.Solve((t, o) => 1.0 / t, 1.0, -1.0, null, 0, false);
            x.ShouldEqual(-4.9406564584124654E-324, 0);
            fx.ShouldEqual(double.NegativeInfinity);
            (x, fx) = Bisection.Solve((t, o) => 1.0 / t, -1.0, 1.0, null, 0, false);
            x.ShouldEqual(0, 0);
            fx.ShouldEqual(double.PositiveInfinity);
            (x, fx) = Bisection.Solve((t, o) => 1.0 / t, 1.0, -1.0, null);
            x.ShouldEqual(0, 0);
            fx.ShouldEqual(double.PositiveInfinity);
        }

        [Fact]
        private void IrrationalTest()
        {
            double x, fx;

            (x, fx) = Bisection.Solve((t, o) => 0.5 * t * t - 3.0, 2.0, 3.0, null);
            x.ShouldEqual(2.4494897427831779, 0);
            fx.ShouldEqual(-4.4408920985006262E-16);
            (x, fx) = Bisection.Solve((t, o) => 0.5 * t * t - 3.0, 2.0, 3.0, null, 0, false);
            x.ShouldEqual(2.4494897427831783, 0);
            fx.ShouldEqual(4.4408920985006262E-16);
            (x, fx) = Bisection.Solve((t, o) => 0.5 * t * t - 3.0, 3.0, 2.0, null);
            x.ShouldEqual(2.4494897427831783, 0);
            fx.ShouldEqual(4.4408920985006262E-16);
            (x, fx) = Bisection.Solve((t, o) => 0.5 * t * t - 3.0, 3.0, 2.0, null, 0, false);
            x.ShouldEqual(2.4494897427831779, 0);
            fx.ShouldEqual(-4.4408920985006262E-16);
        }

        // f(0) is exactly 0 at the first midpoint: that is the root, where it used to walk to the bracket's end
        [Fact]
        private void ExactZeroAtTheMidpoint()
        {
            (double x, double fx) = Bisection.Solve((t, o) => t, -1.0, 1.0, null);
            x.ShouldEqual(0, 0);
            fx.ShouldEqual(0, 0);
        }

        // two values of the same sign whose product underflows to 0 don't bracket a root
        [Fact]
        private void TinyValuesOfOneSignDontBracket() =>
            Assert.Throws<ArgumentException>(() => Bisection.Solve((t, o) => 1e-200, -1.0, 1.0, null));
    }
}
