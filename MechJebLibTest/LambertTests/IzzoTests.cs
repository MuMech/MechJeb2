using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using Xunit;
using Xunit.Abstractions;

namespace MechJebLibTest.LambertTests
{
    public class IzzoTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public IzzoTests(ITestOutputHelper testOutputHelper)
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
            double tol = 1e-6;

            var random = new Random(seed);

            var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var    v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
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
                tol = 2e-3;

            bool prograde = V3.Cross(r0, v0).z >= 0;

            (V3 viIzzo, V3 vfIzzo) = Izzo.Solve(1.0, r0, rfShepperd, dt, prograde: prograde);

            viIzzo.ShouldEqual(v0, tol);
            vfIzzo.ShouldEqual(vfShepperd, tol);

            if (period <= 0) return;

            for (int m = 1; m < 10; m++)
            {
                try
                {
                    (V3 viNRev, V3 vfNRev) = Izzo.Solve(1.0, r0, rfShepperd, dt + m * period, m, prograde);
                    viNRev.ShouldEqual(viIzzo, tol);
                    vfNRev.ShouldEqual(vfIzzo, tol);
                }
                catch (Exception e)
                {
                    (V3 viNRev, V3 vfNRev) = Izzo.Solve(1.0, r0, rfShepperd, dt + m * period, m, prograde, false);

                    viNRev.ShouldEqual(viIzzo, tol);
                    vfNRev.ShouldEqual(vfIzzo, tol);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        private void RandomPositions(int seed)
        {
            double tol = 1e-6;

            var random = new Random(seed);

            var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var    rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double dt = random.NextDouble() * 6 + 0.05;

            // avoid inherent singularity at nearly collinear ri, rf
            if (Math.Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.99998)
                return;

            // relax tolerance for nearly collinear
            if (Math.Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.999)
                tol = 2e-3;

            (V3 viPrograde, V3 vfPrograde) = Izzo.Solve(1.0, r0, rf, dt, prograde: true);
            (V3 viRetrograde, V3 vfRetrograde) = Izzo.Solve(1.0, r0, rf, dt, prograde: false);

            (V3 rfShepperdPrograde, V3 vfShepperdPrograde) = Shepperd.Solve(1.0, dt, r0, viPrograde);
            vfShepperdPrograde.ShouldEqual(vfPrograde, tol);
            rfShepperdPrograde.ShouldEqual(rf, tol);

            (V3 rfShepperdRetrograde, V3 vfShepperdRetrograde) = Shepperd.Solve(1.0, dt, r0, viRetrograde);
            vfShepperdRetrograde.ShouldEqual(vfRetrograde, tol);
            rfShepperdRetrograde.ShouldEqual(rf, tol);
        }
    }
}
