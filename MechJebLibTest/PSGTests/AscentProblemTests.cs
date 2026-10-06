/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using Xunit;

namespace MechJebLibTest.PSGTests
{
    public class AscentProblemTests
    {
        // normalized Earth with the RSS scale height
        private const double R_BODY = 1.0;
        private const double H0     = 7566.61914736748 / 6371000;

        [Fact]
        public void NormalizedDensityIsExponentialAboveGround()
        {
            foreach (double x in new[] { 0, 1e-6, 0.5, 1, 10, 100 })
            {
                double r   = R_BODY + x * H0;
                Dual   rho = AscentProblem.NormalizedDensity(new Dual(r, 1), R_BODY, H0);

                // identical to the plain exponential atmosphere
                rho.M.ShouldEqual(Math.Exp(-(r - R_BODY) / H0), 0);
                rho.D.ShouldEqual(-Math.Exp(-(r - R_BODY) / H0) / H0, 1e-15);
            }
        }

        [Fact]
        public void NormalizedDensityIsC1AtTheSurface()
        {
            double dr = 1e-9 * H0;

            Dual above = AscentProblem.NormalizedDensity(new Dual(R_BODY + dr, 1), R_BODY, H0);
            Dual below = AscentProblem.NormalizedDensity(new Dual(R_BODY - dr, 1), R_BODY, H0);

            above.M.ShouldEqual(1.0, 1e-8);
            below.M.ShouldEqual(1.0, 1e-8);
            above.D.ShouldEqual(-1.0 / H0, 1e-8);
            below.D.ShouldEqual(-1.0 / H0, 1e-8);
        }

        [Fact]
        public void NormalizedDensityIsBoundedUnderground()
        {
            double previous = 1.0;

            // monotonically increasing with depth, and the scale height has relaxed to RBody by the center
            foreach (double r in new[] { 0.99, 0.9, 0.5, 0.1, 0.0 })
            {
                Dual rho = AscentProblem.NormalizedDensity(new Dual(r, 1), R_BODY, H0);

                Assert.True(rho.M > previous);
                Assert.True(rho.D < 0);
                Assert.True(rho.M < Math.Exp(2));
                previous = rho.M;
            }

            AscentProblem.NormalizedDensity(0, R_BODY, H0).M.ShouldEqual(Math.Exp(2 - H0 / R_BODY), 1e-12);
        }
    }
}
