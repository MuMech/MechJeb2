/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Lambert;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using Xunit;
using Xunit.Abstractions;

namespace MechJebLibTest.LambertTests
{
    public class GoodingTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public GoodingTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i < 250; i++)
                yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        private void RandomMultipleRevolution(int seed)
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            double tol = 1e-6;

            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double dt, period;
            double ecc = Astro.EccFromStateVectors(1.0, r0, v0);

            if (ecc < 1)
            {
                period = Astro.PeriodFromStateVectors(1.0, r0, v0);
                dt = random.NextDouble() * period;
            }
            else
            {
                period = 0;
                dt = random.NextDouble() * 5;
            }

            (V3 rfShepperd, V3 vfShepperd) = Shepperd.Solve(1.0, dt, r0, v0);

            // avoid inherent singularity at collinear ri, rf
            if (Math.Abs(V3.Dot(r0.normalized, rfShepperd.normalized)) > 0.99998)
                return;

            // relax tolerance for nearly collinear
            if (Math.Abs(V3.Dot(r0.normalized, rfShepperd.normalized)) > 0.999)
                tol = 5e-2;

            (V3 viGooding, V3 vfGooding) = Gooding.Solve(1.0, r0, rfShepperd, dt, TransferGeometry.Prograde, 0, V3.Cross(r0, v0));

            viGooding.ShouldEqual(v0, tol);
            vfGooding.ShouldEqual(vfShepperd, tol);

            if (period <= 0) return;

            for (int n = 1; n < 10; n++)
            {
                try
                {
                    (V3 viNRev, V3 vfNRev) = Gooding.Solve(1.0, r0, rfShepperd, dt + n * period, TransferGeometry.Prograde, -n, V3.Cross(r0, v0));

                    viNRev.ShouldEqual(viGooding, tol);
                    vfNRev.ShouldEqual(vfGooding, tol);
                }
                catch (Exception)
                {
                    (V3 viNRev, V3 vfNRev) = Gooding.Solve(1.0, r0, rfShepperd, dt + n * period, TransferGeometry.Prograde, n, V3.Cross(r0, v0));

                    viNRev.ShouldEqual(viGooding, tol);
                    vfNRev.ShouldEqual(vfGooding, tol);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        private void RandomPositions(int seed)
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            double tol = 1e-6;

            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double dt = random.NextDouble() * 6 + 0.05;

            // avoid inherent singularity at nearly collinear ri, rf
            if (Math.Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.99998)
                return;

            // relax tolerance for nearly collinear
            if (Math.Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.999)
                tol = 2e-3;

            (V3 viPrograde, V3 vfPrograde) = Gooding.Solve(1.0, r0, rf, dt);
            (V3 viRetrograde, V3 vfRetrograde) = Gooding.Solve(1.0, r0, rf, dt);

            (V3 rfShepperdPrograde, V3 vfShepperdPrograde) = Shepperd.Solve(1.0, dt, r0, viPrograde);
            vfShepperdPrograde.ShouldEqual(vfPrograde, tol);
            rfShepperdPrograde.ShouldEqual(rf, tol);

            (V3 rfShepperdRetrograde, V3 vfShepperdRetrograde) = Shepperd.Solve(1.0, dt, r0, viRetrograde);
            vfShepperdRetrograde.ShouldEqual(vfRetrograde, tol);
            rfShepperdRetrograde.ShouldEqual(rf, tol);
        }
    }
}
