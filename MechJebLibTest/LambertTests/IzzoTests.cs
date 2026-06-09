using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.Lambert;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
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
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

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

            bool             prograde  = V3.Cross(r0, v0).z >= 0;
            TransferGeometry direction = prograde ? TransferGeometry.Prograde : TransferGeometry.Retrograde;

            (V3 viIzzo, V3 vfIzzo) = Izzo.Solve(1.0, r0, rfShepperd, dt, direction, 0, V3.northpole);

            viIzzo.ShouldEqual(v0, tol);
            vfIzzo.ShouldEqual(vfShepperd, tol);

            if (period <= 0) return;

            for (int m = 1; m < 10; m++)
            {
                try
                {
                    (V3 viNRev, V3 vfNRev) = Izzo.Solve(1.0, r0, rfShepperd, dt + m * period, direction, m, V3.northpole);
                    viNRev.ShouldEqual(viIzzo, tol);
                    vfNRev.ShouldEqual(vfIzzo, tol);
                }
                catch (Exception)
                {
                    (V3 viNRev, V3 vfNRev) = Izzo.Solve(1.0, r0, rfShepperd, dt + m * period, direction, -m, V3.northpole);

                    viNRev.ShouldEqual(viIzzo, tol);
                    vfNRev.ShouldEqual(vfIzzo, tol);
                }
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        private void RandomMultipleRevolutionShortWay(int seed)
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

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

            bool longway = V3.Dot(V3.Cross(r0, v0), V3.Cross(r0, rfShepperd)) < 0;
            TransferGeometry direction = longway ? TransferGeometry.LongWay : TransferGeometry.ShortWay;

            (V3 viIzzo, V3 vfIzzo) = Izzo.Solve(1.0, r0, rfShepperd, dt, direction, 0);

            viIzzo.ShouldEqual(v0, tol);
            vfIzzo.ShouldEqual(vfShepperd, tol);

            if (period <= 0) return;

            for (int m = 1; m < 10; m++)
            {
                try
                {
                    (V3 viNRev, V3 vfNRev) = Izzo.Solve(1.0, r0, rfShepperd, dt + m * period, direction, m);
                    viNRev.ShouldEqual(viIzzo, tol);
                    vfNRev.ShouldEqual(vfIzzo, tol);
                }
                catch (Exception)
                {
                    (V3 viNRev, V3 vfNRev) = Izzo.Solve(1.0, r0, rfShepperd, dt + m * period, direction, -m);

                    viNRev.ShouldEqual(viIzzo, tol);
                    vfNRev.ShouldEqual(vfIzzo, tol);
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

            var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var    rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double dt = random.NextDouble() * 6 + 0.05;

            (V3 viPrograde, V3 vfPrograde) = Izzo.Solve(1.0, r0, rf, dt, TransferGeometry.Prograde, 0, V3.northpole);
            (V3 viRetrograde, V3 vfRetrograde) = Izzo.Solve(1.0, r0, rf, dt, TransferGeometry.Retrograde, 0, V3.northpole);

            (V3 rfShepperdPrograde, V3 vfShepperdPrograde) = Shepperd.Solve(1.0, dt, r0, viPrograde);
            vfShepperdPrograde.ShouldEqual(vfPrograde, tol);
            rfShepperdPrograde.ShouldEqual(rf, tol);

            (V3 rfShepperdRetrograde, V3 vfShepperdRetrograde) = Shepperd.Solve(1.0, dt, r0, viRetrograde);
            vfShepperdRetrograde.ShouldEqual(vfRetrograde, tol);
            rfShepperdRetrograde.ShouldEqual(rf, tol);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        private void RandomPositionsComparedToGooding(int seed)
        {
            Logger.Register(o => _testOutputHelper.WriteLine((string)o));

            double tol = 1e-6;

            var random = new Random(seed);

            var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var    rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var    v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double dt = random.NextDouble() * 6 + 0.05;

            V3 vi1, vf1, vi2, vf2;

            (vi1, vf1) = Izzo.Solve(1.0, r0, rf, dt, TransferGeometry.Prograde, 0, V3.northpole);
            (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt, TransferGeometry.Prograde, 0, V3.northpole);
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);

            (vi1, vf1) = Izzo.Solve(1.0, r0, rf, dt, TransferGeometry.Retrograde, 0, V3.northpole);
            (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt, TransferGeometry.Retrograde, 0, V3.northpole);
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);

            (vi1, vf1) = Izzo.Solve(1.0, r0, rf, dt, TransferGeometry.Prograde, 0, V3.Cross(r0, v0));
            (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt, TransferGeometry.Prograde, 0, V3.Cross(r0, v0));
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);

            (vi1, vf1) = Izzo.Solve(1.0, r0, rf, dt, TransferGeometry.Retrograde, 0, V3.Cross(r0, v0));
            (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt, TransferGeometry.Retrograde, 0, V3.Cross(r0, v0));
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);

            // avoid inherent singularity at nearly collinear ri, rf for longway/shortway
            if (Math.Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.99998)
                return;

            // relax tolerance for nearly collinear
            if (Math.Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.999)
                tol = 2e-3;

            (vi1, vf1) = Izzo.Solve(1.0, r0, rf, dt);
            (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt);
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);

            (vi1, vf1) = Izzo.Solve(1.0, r0, rf, dt, TransferGeometry.LongWay);
            (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt, TransferGeometry.LongWay);
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);
        }
    }
}
