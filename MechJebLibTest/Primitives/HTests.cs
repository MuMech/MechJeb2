/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */
﻿/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Primitives
{
    public class HTests
    {
        private readonly H1 _interpolant1 = H1.Get();

        [Fact]
        public void H1Linear()
        {
            _interpolant1.Add(0, 0, 1);
            _interpolant1.Add(1, 1, 1);
            _interpolant1.Evaluate(0.5).ShouldEqual(0.5, 1e-15);
            _interpolant1.Evaluate(0.5).ShouldEqual(0.5, 1e-15);
        }

        [Fact]
        public void H1ZeroSlope()
        {
            _interpolant1.Add(0, 0, 0);
            _interpolant1.Add(1, 0, 0);
            _interpolant1.Evaluate(0.5).ShouldEqual(0, 1e-15);
            _interpolant1.Evaluate(0.5).ShouldEqual(0, 1e-15);
        }

        [Fact]
        public void H1SupportsDerivativeJump()
        {
            _interpolant1.Add(0, 0, 1);
            _interpolant1.Add(1, 1, 1);
            _interpolant1.Add(1, 1, -1);
            _interpolant1.Add(2, 0, -1);

            _interpolant1.Evaluate(0.5).ShouldEqual(0.5, 1e-15);
            _interpolant1.Evaluate(1.5).ShouldEqual(0.5, 1e-15);
        }

        [Fact]
        public void H1SingleEntry()
        {
            _interpolant1.Add(2, 1, 0);

            _interpolant1.Evaluate(1.5).ShouldEqual(1.0, 1e-15);

            _interpolant1.Evaluate(2.0).ShouldEqual(1.0, 1e-15);

            _interpolant1.Evaluate(2.5).ShouldEqual(1.0, 1e-15);
        }

        [Fact]
        public void H1SingleEntry2()
        {
            _interpolant1.Add(2, 1, 1);

            _interpolant1.Evaluate(1.5).ShouldEqual(0.5, 1e-15);

            _interpolant1.Evaluate(2.0).ShouldEqual(1.0, 1e-15);

            _interpolant1.Evaluate(2.5).ShouldEqual(1.5, 1e-15);
        }

        [Fact]
        public void H1AutoTangent()
        {
            _interpolant1.Add(2, 1);
            _interpolant1.Add(1, 2);
            _interpolant1.Add(3, 2);
            _interpolant1.Add(0, 1);
            _interpolant1.Add(4, 1);
            // FIXME: need a proper test of the slopes
        }
    }
}
