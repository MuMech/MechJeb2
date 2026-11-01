/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.Lambert;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using Xunit;
using static System.Math;

namespace MechJebLibTest.LambertTests
{
    public class LambertSolverTests
    {
        private const double MU_KERBIN = 3.5316e12;
        private const double MU_SUN = 1.1723328e18;

        private static (V3 r, V3 v) Circular(double mu, double radius, double angle) =>
            (new V3(radius * Cos(angle), radius * Sin(angle), 0), Sqrt(mu / radius) * new V3(-Sin(angle), Cos(angle), 0));

        [Theory]
        [InlineData(MU_SUN, 13.6e9, 200, 100)]     // the Sun at Kerbin's distance
        [InlineData(MU_SUN, 13.6e9, 1000, 100)]
        [InlineData(MU_SUN, 13.6e9, 100e3, 1000)]
        [InlineData(MU_KERBIN, 33e6, 500, 100)]    // high Kerbin orbits
        [InlineData(MU_KERBIN, 47e6, 5000, 100)]
        private void SolvesWhatRussellRefuses(double mu, double radius, double separation, double tof)
        {
            (V3 r1, V3 v1) = Circular(mu, radius, 0);
            (V3 r2Start, V3 v2) = Circular(mu, radius, separation / radius);
            (V3 r2, V3 _) = Shepperd.Solve(mu, tof, r2Start, v2);

            Assert.Throws<ArgumentException>(() => Russell.Solve(mu, r1, r2, tof, TransferGeometry.Prograde, 0, V3.Cross(r1, v1)));

            (V3 vi, V3 vf) = LambertSolver.Solve(mu, r1, r2, tof, TransferGeometry.Prograde, V3.Cross(r1, v1));

            (V3 rArrival, V3 vArrival) = Shepperd.Solve(mu, tof, r1, vi);

            // the closing speed of the transfer is the separation over the time of flight, to within a tenth
            Assert.InRange((vi - v1).magnitude, 0.9 * separation / tof, 1.1 * separation / tof);
            Assert.InRange((rArrival - r2).magnitude, 0, 0.1);
            Assert.InRange((vArrival - vf).magnitude, 0, 1e-3);
        }

        [Fact]
        private void KeepsRussellInsideItsDomain()
        {
            (V3 r1, V3 v1) = Circular(MU_KERBIN, 700e3, 0);
            (V3 r2, V3 _) = Circular(MU_KERBIN, 800e3, 2.0);

            (V3 vi, V3 vf) = LambertSolver.Solve(MU_KERBIN, r1, r2, 900, TransferGeometry.Prograde, V3.Cross(r1, v1));
            (V3 viRussell, V3 vfRussell) = Russell.Solve(MU_KERBIN, r1, r2, 900, TransferGeometry.Prograde, 0, V3.Cross(r1, v1));

            Assert.Equal(viRussell, vi);
            Assert.Equal(vfRussell, vf);
        }

        [Fact]
        private void RefusesBadInputInsteadOfReachingGooding()
        {
            (V3 r1, V3 v1) = Circular(MU_KERBIN, 700e3, 0);
            V3 h = V3.Cross(r1, v1);

            Assert.ThrowsAny<Exception>(() => LambertSolver.Solve(MU_KERBIN, r1, r1, double.NaN, TransferGeometry.Prograde, h));
            Assert.ThrowsAny<Exception>(() => LambertSolver.Solve(MU_KERBIN, new V3(double.NaN, 0, 0), r1, 100, TransferGeometry.Prograde, h));
        }
    }
}
