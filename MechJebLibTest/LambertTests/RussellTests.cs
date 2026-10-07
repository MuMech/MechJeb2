/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using System.Linq;
using MechJebLib.Functions;
using MechJebLib.Lambert;
using MechJebLib.Maths;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using MechJebLib.Utils;
using Xunit;
using static System.Math;

namespace MechJebLibTest.LambertTests
{
    public class RussellTests
    {
        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i < 250; i++)
                yield return new object[] { i };
        }

        // the transfer geometry for d = 1 (short way) or d = -1 (long way)
        private static TransferGeometry Geometry(int direction) => direction == 1 ? TransferGeometry.ShortWay : TransferGeometry.LongWay;

        // recover the vercosine iteration variables (k, p) from the solution velocity, to check which code paths were tested
        private static (double k, double p) VercosineKP(V3 r0, V3 rf, V3 v0, int direction)
        {
            double r1 = r0.magnitude;
            double r2 = rf.magnitude;
            double ctheta = V3.Dot(r0, rf) / (r1 * r2);
            double semiLatusRectum = V3.Cross(r0, v0).sqrMagnitude;
            double p = r1 * r2 * (1 - ctheta) / (semiLatusRectum * (r1 + r2));
            double tau = direction * Sqrt(r1 * r2 * (1 + ctheta)) / (r1 + r2);
            return ((1 - p) / tau, p);
        }

        // within the limit of Russell (2022) around the r1 == r2 singularity
        private static bool NearSingularity(V3 r0, V3 rf)
        {
            double r1 = r0.magnitude;
            double r2 = rf.magnitude;
            return Sqrt(0.5) - Sqrt(Max(r1 * r2 + V3.Dot(r0, rf), 0)) / (r1 + r2) < 1e-7;
        }

        // parabolic time of flight (Russell 2019, Eq. 39), mu = 1
        private static double ParabolicTof(V3 r0, V3 rf, int direction)
        {
            double r1 = r0.magnitude;
            double r2 = rf.magnitude;
            double tau = direction * Sqrt(r1 * r2 + V3.Dot(r0, rf)) / (r1 + r2);
            return (r1 + r2) * Sqrt(r1 + r2) * Sqrt(1 - Sqrt(2) * tau) * (tau + Sqrt(2)) / 3;
        }

        private static void CheckTransfer(V3 r0, V3 rf, double dt, V3 vi, V3 vf, double tol)
        {
            (V3 rfShepperd, V3 vfShepperd) = Shepperd.Solve(1.0, dt, r0, vi);
            rfShepperd.ShouldEqual(rf, tol);
            vfShepperd.ShouldEqual(vf, tol);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void RandomMultipleRevolution(int seed)
        {
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

            // the arc that goes the same way around as v0
            V3 h = V3.Cross(r0, v0);

            if (NearSingularity(r0, rfShepperd))
            {
                Logger.Print($"skipping, too close to r1 == r2: {Sqrt(0.5) - Sqrt(r0.magnitude * rfShepperd.magnitude + V3.Dot(r0, rfShepperd)) / (r0.magnitude + rfShepperd.magnitude):E2}");
                Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rfShepperd, dt, TransferGeometry.Prograde, h: h));
                return;
            }

            (V3 viRussell, V3 vfRussell) = Russell.Solve(1.0, r0, rfShepperd, dt, TransferGeometry.Prograde, h: h);

            viRussell.ShouldEqual(v0, tol);
            vfRussell.ShouldEqual(vfShepperd, tol);

            if (period <= 0) return;

            for (int m = 1; m < 10; m++)
            {
                try
                {
                    (V3 viNRev, V3 vfNRev) = Russell.Solve(1.0, r0, rfShepperd, dt + m * period, TransferGeometry.Prograde, m, h);
                    viNRev.ShouldEqual(viRussell, tol);
                    vfNRev.ShouldEqual(vfRussell, tol);
                }
                catch (Exception)
                {
                    (V3 viNRev, V3 vfNRev) = Russell.Solve(1.0, r0, rfShepperd, dt + m * period, TransferGeometry.Prograde, -m, h);

                    viNRev.ShouldEqual(viRussell, tol);
                    vfNRev.ShouldEqual(vfRussell, tol);
                }
            }
        }

        [Theory, MemberData(nameof(Seeds))]
        private void RandomPositions(int seed)
        {
            double tol = 1e-6;

            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double dt = random.NextDouble() * 6 + 0.05;

            (V3 viShort, V3 vfShort) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.ShortWay);
            CheckTransfer(r0, rf, dt, viShort, vfShort, tol);

            (V3 viLong, V3 vfLong) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.LongWay);
            CheckTransfer(r0, rf, dt, viLong, vfLong, tol);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void RandomPositionsComparedToIzzoAndGooding(int seed)
        {
            double tol = 1e-6;

            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            double dt = random.NextDouble() * 6 + 0.05;

            // avoid inherent singularity at nearly collinear ri, rf for longway/shortway
            if (Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.99998)
                return;

            // relax tolerance for nearly collinear
            if (Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.999)
                tol = 2e-3;

            V3 vi1, vf1, vi2, vf2;

            (vi1, vf1) = Russell.Solve(1.0, r0, rf, dt);
            (vi2, vf2) = Izzo.Solve(1.0, r0, rf, dt);
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);
            (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt);
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);

            (vi1, vf1) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.LongWay);
            (vi2, vf2) = Izzo.Solve(1.0, r0, rf, dt, TransferGeometry.LongWay);
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);
            (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt, TransferGeometry.LongWay);
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);
        }

        // both multi-rev branches are solutions, and n > 0 is the long-period branch
        [Theory, MemberData(nameof(Seeds))]
        private void RandomMultipleRevolutionBranches(int seed)
        {
            double tol = 1e-6;

            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            int m = random.Next(1, 20);
            double dt = Pow(10, 2 * random.NextDouble() + 2) * m;
            TransferGeometry geometry = random.Next(2) == 0 ? TransferGeometry.ShortWay : TransferGeometry.LongWay;

            (V3 viLong, V3 vfLong) = Russell.Solve(1.0, r0, rf, dt, geometry, m);
            CheckTransfer(r0, rf, dt, viLong, vfLong, tol);

            (V3 viShort, V3 vfShort) = Russell.Solve(1.0, r0, rf, dt, geometry, -m);
            CheckTransfer(r0, rf, dt, viShort, vfShort, tol);

            Assert.True(Astro.SmaFromStateVectors(1.0, r0, viLong) > Astro.SmaFromStateVectors(1.0, r0, viShort));
        }

        // the sign of nrev selects the same branch as the nrev argument of Izzo and Gooding
        [Theory, MemberData(nameof(Seeds))]
        private void RandomMultipleRevolutionComparedToIzzoAndGooding(int seed)
        {
            double tol = 1e-6;

            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            int m = random.Next(1, 6);
            double dt = Pow(10, 2 * random.NextDouble() + 2) * m;
            TransferGeometry geometry = random.Next(2) == 0 ? TransferGeometry.ShortWay : TransferGeometry.LongWay;

            foreach (int n in new[] { m, -m })
            {
                (V3 vi1, V3 vf1) = Russell.Solve(1.0, r0, rf, dt, geometry, n);

                (V3 vi2, V3 vf2) = Izzo.Solve(1.0, r0, rf, dt, geometry, n, rtol: 1e-12);
                vi1.ShouldEqual(vi2, tol);
                vf1.ShouldEqual(vf2, tol);

                (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt, geometry, n);
                vi1.ShouldEqual(vi2, tol);
                vf1.ShouldEqual(vf2, tol);
            }
        }

        // prograde and retrograde pick the short way or long way, depending on which way around h the transfer goes
        [Theory, MemberData(nameof(Seeds))]
        private void RandomProgradeRetrograde(int seed)
        {
            double tol = 1e-6;

            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var h = new V3(2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1);
            double dt = random.NextDouble() * 6 + 0.05;

            // avoid inherent singularity at nearly collinear ri, rf for longway/shortway
            if (Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.99998)
                return;

            // relax tolerance for nearly collinear
            if (Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.999)
                tol = 2e-3;

            bool shortWayIsPrograde = V3.Dot(V3.Cross(r0, rf), h) >= 0;

            (V3 viShort, V3 vfShort) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.ShortWay);
            (V3 viLong, V3 vfLong) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.LongWay);

            (V3 viPro, V3 vfPro) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.Prograde, h: h);
            viPro.ShouldEqual(shortWayIsPrograde ? viShort : viLong, tol);
            vfPro.ShouldEqual(shortWayIsPrograde ? vfShort : vfLong, tol);
            Assert.True(V3.Dot(V3.Cross(r0, viPro), h) > 0);

            (V3 viRetro, V3 vfRetro) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.Retrograde, h: h);
            viRetro.ShouldEqual(shortWayIsPrograde ? viLong : viShort, tol);
            vfRetro.ShouldEqual(shortWayIsPrograde ? vfLong : vfShort, tol);
            Assert.True(V3.Dot(V3.Cross(r0, viRetro), h) < 0);

            foreach (TransferGeometry geometry in new[] { TransferGeometry.Prograde, TransferGeometry.Retrograde })
            {
                (V3 vi1, V3 vf1) = Russell.Solve(1.0, r0, rf, dt, geometry, h: h);

                (V3 vi2, V3 vf2) = Izzo.Solve(1.0, r0, rf, dt, geometry, h: h);
                vi1.ShouldEqual(vi2, tol);
                vf1.ShouldEqual(vf2, tol);

                (vi2, vf2) = Gooding.Solve(1.0, r0, rf, dt, geometry, h: h);
                vi1.ShouldEqual(vi2, tol);
                vf1.ShouldEqual(vf2, tol);
            }
        }

        [Theory]
        [InlineData(TransferGeometry.ShortWay)]
        [InlineData(TransferGeometry.LongWay)]
        private void NearHalfRevolution(TransferGeometry direction)
        {
            var r0 = new V3(1, 0, 0);
            double theta = PI - 1e-6;
            V3 rf = 1.3 * new V3(Cos(theta), Sin(theta), 0);

            (V3 vi, V3 vf) = Russell.Solve(1.0, r0, rf, 3.0, direction);
            CheckTransfer(r0, rf, 3.0, vi, vf, 1e-6);
        }

        [Theory]
        [InlineData(TransferGeometry.Prograde, 2.0, 1.0, 0)]
        [InlineData(TransferGeometry.Prograde, 0.5, 0.2, 0)]
        [InlineData(TransferGeometry.Prograde, 4.0, 3.0, 0)]
        [InlineData(TransferGeometry.Retrograde, 2.0, 1.0, 0)]
        [InlineData(TransferGeometry.ShortWay, 2.0, 1.0, 0)]
        [InlineData(TransferGeometry.LongWay, 2.0, 1.0, 0)]
        [InlineData(TransferGeometry.Prograde, 2.0, 5.0, 1)]
        [InlineData(TransferGeometry.Prograde, 2.0, 5.0, -1)]
        [InlineData(TransferGeometry.Retrograde, 0.5, 5.0, 1)]
        private void ExactHalfRevolution(TransferGeometry direction, double ratio, double tofByHohmann, int nrev)
        {
            // rf is a power of two multiple of r0 so it is still exactly antiparallel after rounding, and h is not normal to r0
            var r0 = new V3(0.3, -0.7, 0.6);
            V3 rf = -ratio * r0;
            var h = new V3(0.2, 0.5, 0.9);
            double dt = tofByHohmann * PI * Pow(0.5 * (r0.magnitude + rf.magnitude), 1.5);

            // the exact half-rev uses h even without the band around it
            (V3 vi, V3 vf, _, _, double tau, bool halfRev) = Russell.SolveWithState(1.0, r0, rf, dt, direction, nrev, h, 50, 0.0, out _);
            Assert.Equal(0.0, tau);
            Assert.True(halfRev);
            CheckTransfer(r0, rf, dt, vi, vf, 1e-10);

            // the transfer plane contains h projected normal to r0, and the motion is counterclockwise around h for prograde
            // and the short way
            V3 hHat = (h - V3.Dot(h, r0) / r0.sqrMagnitude * r0).normalized;
            bool clockwise = direction == TransferGeometry.Retrograde || direction == TransferGeometry.LongWay;
            V3.Cross(r0, vi).normalized.ShouldEqual(clockwise ? -hHat : hHat, 1e-12);

            // continuous with transfer angles just short of pi with (r0 x rf) . h > 0
            double theta = PI - 1e-7;
            V3 rfNear = rf.magnitude * (Cos(theta) * r0.normalized + Sin(theta) * V3.Cross(hHat, r0).normalized);
            (V3 viNear, V3 vfNear) = Russell.Solve(1.0, r0, rfNear, dt, direction, nrev, h);
            vi.ShouldEqual(viNear, 1e-6);
            vf.ShouldEqual(vfNear, 1e-6);
        }

        [Theory]
        [InlineData(TransferGeometry.Prograde, 1)]
        [InlineData(TransferGeometry.Prograde, -1)]
        [InlineData(TransferGeometry.Retrograde, 1)]
        [InlineData(TransferGeometry.Retrograde, -1)]
        [InlineData(TransferGeometry.ShortWay, 1)]
        [InlineData(TransferGeometry.ShortWay, -1)]
        [InlineData(TransferGeometry.LongWay, 1)]
        [InlineData(TransferGeometry.LongWay, -1)]
        private void HalfRevolutionBand(TransferGeometry direction, int side)
        {
            var r0 = new V3(0.3, -0.7, 0.6);
            var h = new V3(0.2, 0.5, 0.9);
            V3 hHat = (h - V3.Dot(h, r0) / r0.sqrMagnitude * r0).normalized;
            V3 tHat = V3.Cross(hHat, r0).normalized;
            double delta = side * 1e-13;
            double dt = 4.0;

            // r2 on either side of the half-rev in the plane of h is exact, where the Lagrange coefficients miss by about 1e-3
            V3 rf = 2.0 * r0.magnitude * (-Cos(delta) * r0.normalized + Sin(delta) * tHat);
            (V3 vi, V3 vf, _, _, _, bool halfRev) = Russell.SolveWithState(1.0, r0, rf, dt, direction, 0, h, 50, 1e-12, out _);
            Assert.True(halfRev);
            CheckTransfer(r0, rf, dt, vi, vf, 1e-10);
            Assert.False(Russell.SolveWithState(1.0, r0, rf, dt, direction, 0, h, 50, 0.0, out _).halfRev);

            // r2 out of the plane of h misses by about the angle from the half-rev
            V3 rfOut = 2.0 * r0.magnitude * (-Cos(delta) * r0.normalized + Sin(delta) * hHat);
            (vi, vf) = Russell.Solve(1.0, r0, rfOut, dt, direction, 0, h);
            (V3 rfShepperd, V3 vfShepperd) = Shepperd.Solve(1.0, dt, r0, vi);
            rfShepperd.ShouldEqual(rfOut, 1e-11);
            vfShepperd.ShouldEqual(vf, 1e-10);

            // there are no derivatives inside the band
            Assert.Throws<ArgumentException>(() =>
                Russell.Solve(1.0, new DualV3(r0, V3.zero), new DualV3(rf, V3.zero), new Dual(dt, 1.0), direction, 0, h));
        }

        [Theory]
        [InlineData(TransferGeometry.ShortWay)]
        [InlineData(TransferGeometry.LongWay)]
        private void NearFullRevolution(TransferGeometry direction)
        {
            // for the short way the transfer angle is 1e-4 and for the long way it is 2pi - 1e-4
            var r0 = new V3(1, 0, 0);
            V3 rf = 1.5 * new V3(Cos(1e-4), Sin(1e-4), 0);

            (V3 vi, V3 vf) = Russell.Solve(1.0, r0, rf, 5.0, direction);
            CheckTransfer(r0, rf, 5.0, vi, vf, 1e-6);
        }

        [Fact]
        private void HugeKIteration()
        {
            // very fast long way hyperbola uses the series in 1/k.  the periapsis is ~1e-7 which is too close for Shepperd
            // to check, so compare to the other solvers.
            var r0 = new V3(1, 0, 0);
            var rf = new V3(0, 1, 0);

            (V3 vi, V3 vf) = Russell.Solve(1.0, r0, rf, 2e-3, TransferGeometry.LongWay);

            (V3 viIzzo, V3 vfIzzo) = Izzo.Solve(1.0, r0, rf, 2e-3, TransferGeometry.LongWay, rtol: 1e-14);
            vi.ShouldEqual(viIzzo, 1e-12);
            vf.ShouldEqual(vfIzzo, 1e-12);

            (V3 viGooding, V3 vfGooding) = Gooding.Solve(1.0, r0, rf, 2e-3, TransferGeometry.LongWay);
            vi.ShouldEqual(viGooding, 1e-12);
            vf.ShouldEqual(vfGooding, 1e-12);

            (double k, _) = VercosineKP(r0, rf, vi, -1);
            Assert.True(k > 1000, $"k = {k}");
        }

        [Fact]
        private void LittlePIteration()
        {
            // very fast short way hyperbola iterates on p instead of k
            var r0 = new V3(1, 0, 0);
            var rf = new V3(0, 1, 0);

            (V3 vi, V3 vf) = Russell.Solve(1.0, r0, rf, 2e-3, TransferGeometry.ShortWay);
            CheckTransfer(r0, rf, 2e-3, vi, vf, 1e-6);

            (_, double p) = VercosineKP(r0, rf, vi, 1);
            Assert.True(p < 0.1, $"p = {p}");
        }

        // down to the minimum time of flight of 1e-3 times the parabolic time of flight
        [Theory, MemberData(nameof(Seeds))]
        private void RandomSmallTimeOfFlightComparedToGooding(int seed)
        {
            double tol = 1e-9;

            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            int direction = random.Next(2) == 0 ? 1 : -1;
            double dt = ParabolicTof(r0, rf, direction) * Pow(10, -1 - 1.9 * random.NextDouble());

            // avoid inherent singularity at nearly collinear ri, rf
            if (Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.99998)
                return;

            // relax tolerance for nearly collinear
            if (Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.999)
                tol = 2e-3;

            (V3 vi1, V3 vf1) = Russell.Solve(1.0, r0, rf, dt, Geometry(direction));
            (V3 vi2, V3 vf2) = Gooding.Solve(1.0, r0, rf, dt, Geometry(direction));
            vi1.ShouldEqual(vi2, tol);
            vf1.ShouldEqual(vf2, tol);
        }

        [Theory]
        [InlineData(TransferGeometry.ShortWay)]
        [InlineData(TransferGeometry.LongWay)]
        private void HugeTimeOfFlight(TransferGeometry direction)
        {
            // TOF/S > 1e4 root-solves log(TOF/S)
            var r0 = new V3(1, 0, 0);
            var rf = new V3(0, 1, 0);

            (V3 vi, V3 vf) = Russell.Solve(1.0, r0, rf, 1e5, direction);
            CheckTransfer(r0, rf, 1e5, vi, vf, 1e-6);
        }

        [Theory]
        [InlineData(1000)]
        [InlineData(-1000)]
        private void HugeNumberOfRevolutions(int n)
        {
            var r0 = new V3(1, 0, 0);
            var rf = new V3(0, 1.2, 0);
            double dt = 2e4;

            (V3 vi, V3 vf) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.ShortWay, n);
            CheckTransfer(r0, rf, dt, vi, vf, 1e-6);
        }

        // above the largest number of revolutions in the interpolation table
        [Theory]
        [InlineData(10000)]
        [InlineData(-10000)]
        private void BeyondInterpolatedNumberOfRevolutions(int n)
        {
            var r0 = new V3(1, 0, 0);
            var rf = new V3(0, 1.2, 0);
            double dt = 2e5;

            (V3 vi, V3 vf) = Russell.Solve(1.0, r0, rf, dt, TransferGeometry.LongWay, n);
            CheckTransfer(r0, rf, dt, vi, vf, 1e-6);
        }

        [Theory]
        [InlineData(TransferGeometry.ShortWay, 1)]
        [InlineData(TransferGeometry.ShortWay, 3)]
        [InlineData(TransferGeometry.LongWay, 1)]
        [InlineData(TransferGeometry.LongWay, 3)]
        private void MultipleRevolutionNearMinimumTime(TransferGeometry direction, int m)
        {
            var r0 = new V3(1, 0, 0);
            var rf = new V3(0, 1.2, 0);

            // bisect for the minimum time of flight
            double lo = 0.1;
            double hi = 100 * m;

            for (int i = 0; i < 60; i++)
            {
                double mid = 0.5 * (lo + hi);
                try
                {
                    Russell.Solve(1.0, r0, rf, mid, direction, m);
                    hi = mid;
                }
                catch (Exception)
                {
                    lo = mid;
                }
            }

            Assert.Throws<Exception>(() => Russell.Solve(1.0, r0, rf, lo, direction, m));
            Assert.Throws<Exception>(() => Russell.Solve(1.0, r0, rf, lo, direction, -m));

            (V3 viLong, V3 vfLong) = Russell.Solve(1.0, r0, rf, hi, direction, m);
            CheckTransfer(r0, rf, hi, viLong, vfLong, 1e-6);

            (V3 viShort, V3 vfShort) = Russell.Solve(1.0, r0, rf, hi, direction, -m);
            CheckTransfer(r0, rf, hi, viShort, vfShort, 1e-6);
        }

        // r1 == r2 == 1 geometry with the given tau, where tau < 0 is the long way, and its S
        private static (V3 r0, V3 rf, TransferGeometry direction, double S) GeometryFromTau(double tau)
        {
            double theta = 2 * Acos(Sqrt(2) * Abs(tau));
            return (new V3(1, 0, 0), new V3(Cos(theta), Sin(theta), 0), tau >= 0 ? TransferGeometry.ShortWay : TransferGeometry.LongWay,
                2 * Sqrt(2));
        }

        // the error metric for k from Russell (2022), Eq. 28
        private static double GuessError(double k, double kTrue) => Abs(k - kTrue) / Max(Abs(kTrue), 1.0);

        // samples over the whole domain of the interpolation tables (Russell 2022), as (tau, tofbyS, nrev)
        private static IEnumerable<(double tau, double tofbyS, int nrev)> DomainSamples(int count, bool multiRev)
        {
            var random = new Random(42);

            for (int i = 0; i < count; i++)
            {
                double x = 0.995 * (2 * random.NextDouble() - 1);
                double tau = RussellGuess.TauFromX(x);

                if (!multiRev)
                {
                    double y = 0.001 + 0.999 * random.NextDouble();
                    yield return (tau, RussellGuess.ZeroRevTofbyS(x, y), 0);
                }
                else
                {
                    int n = (int)Pow(RussellGuess.N_MAX, random.NextDouble());
                    int nrev = random.Next(2) == 0 ? n : -n;
                    double y = 0.001 + 0.999 * random.NextDouble();
                    (_, double tofbySBottom) = Russell.MultiRevBottom(tau, n);
                    yield return (tau, tofbySBottom + RussellGuess.MultiRevGamma(y, n), nrev);
                }
            }
        }

        private void LogIterations(string name, List<int> iterations)
        {
            var histogram = new SortedDictionary<int, int>();
            foreach (int i in iterations)
                histogram[i] = histogram.TryGetValue(i, out int c) ? c + 1 : 1;
            Logger.Print(
                $"{name}: mean {iterations.Average():F3} max {iterations.Max()} histogram {string.Join(" ", histogram.Select(kv => $"{kv.Key}:{kv.Value}"))}");
        }

        // the interpolated initial guess is within tolerance of the solution over the domain of the tables
        [Theory]
        [InlineData(false, 2e-3)]
        [InlineData(true, 4e-4)]
        private void InitialGuessAccuracy(bool multiRev, double tol)
        {
            double maxError = 0;

            foreach ((double tau, double tofbyS, int nrev) in DomainSamples(20000, multiRev))
            {
                (V3 r0, V3 rf, TransferGeometry direction, double S) = GeometryFromTau(tau);
                (_, _, double k, _, double tauSolved, _) = Russell.SolveWithState(1.0, r0, rf, tofbyS * S, direction, nrev, null, 50, 0.0, out _);

                double guess;
                if (nrev == 0)
                {
                    guess = RussellGuess.ZeroRev(tauSolved, tofbyS);
                }
                else
                {
                    (double kBottom, double tofbySBottom) = Russell.MultiRevBottom(tauSolved, Abs(nrev));
                    guess = RussellGuess.MultiRev(RussellGuess.X(tauSolved), RussellGuess.Z(Abs(nrev)), tofbyS - tofbySBottom, nrev, kBottom);
                }

                double error = GuessError(guess, k);
                maxError = Max(maxError, error);
                Assert.True(error < tol, $"tau = {tau:R} tofbyS = {tofbyS:R} nrev = {nrev} k = {k:R} guess = {guess:R} error = {error:E3}");
            }

            Logger.Print($"max error {maxError:E3}");
        }

        // the interpolated seed for the Newton iteration for kBottom is accurate relative to the distance of kBottom from
        // +-sqrt(2), and the iteration takes at most two steps, also beyond the largest number of revolutions in the table
        // where the seed is extrapolated.
        [Fact]
        private void KBottomSeed()
        {
            var random = new Random(42);
            var iterations = new List<int>();
            double maxError = 0;
            double maxErrorBeyond = 0;

            for (int i = 0; i < 20000; i++)
            {
                double tau = RussellGuess.TauFromX(0.995 * (2 * random.NextDouble() - 1));
                int n = (int)Pow(1e6, random.NextDouble());

                double seed = RussellGuess.KBottom(tau, RussellGuess.X(tau), RussellGuess.Z(n), n);
                (double kBottom, _) = Russell.MultiRevBottom(tau, n, Russell.KBottomAsymptotic(tau), out _);
                (double kSeeded, _) = Russell.MultiRevBottom(tau, n, seed, out int iters);

                double error = Abs(seed - kBottom) / Min(Sqrt(2) - Abs(kBottom), 1.0);
                iterations.Add(iters);

                if (n <= RussellGuess.N_MAX)
                {
                    maxError = Max(maxError, error);
                    Assert.True(error < 2e-6, $"tau = {tau:R} n = {n} kBottom = {kBottom:R} seed = {seed:R} error = {error:E3}");
                }
                else
                {
                    maxErrorBeyond = Max(maxErrorBeyond, error);
                }

                Assert.True(Abs(kSeeded - kBottom) < 1e-12, $"tau = {tau:R} n = {n} kBottom = {kBottom:R} kSeeded = {kSeeded:R}");
                Assert.True(iters <= 2, $"tau = {tau:R} n = {n} kBottom = {kBottom:R} seed = {seed:R} error = {error:E3} iterations = {iters}");
            }

            Logger.Print($"max error {maxError:E3}, beyond the table {maxErrorBeyond:E3}");
            LogIterations("kBottom", iterations);
        }

        // the number of iterations from the initial guess, over the domain of the tables and typical problems.  more
        // iterations are only needed for zero-rev long way transfers with very long times of flight and r1 ~= r2 (which
        // approach the r1 == r2 singularity).
        [Fact]
        private void IterationCounts()
        {
            foreach (bool multiRev in new[] { false, true })
            {
                var iterations = new List<int>();
                foreach ((double tau, double tofbyS, int nrev) in DomainSamples(20000, multiRev))
                {
                    (V3 r0, V3 rf, TransferGeometry direction, double S) = GeometryFromTau(tau);
                    Russell.SolveWithState(1.0, r0, rf, tofbyS * S, direction, nrev, null, 50, 0.0, out int iters);
                    iterations.Add(iters);
                }

                LogIterations(multiRev ? "multi-rev domain" : "zero-rev domain", iterations);
                Assert.True(iterations.Average() < 2.0);
                Assert.True(iterations.Max() <= (multiRev ? 3 : 15));
            }

            var random = new Random(42);
            var typical = new List<int>();
            for (int i = 0; i < 20000; i++)
            {
                var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                TransferGeometry geometry = random.Next(2) == 0 ? TransferGeometry.ShortWay : TransferGeometry.LongWay;
                int m = random.Next(0, 20);
                double dt = m == 0 ? random.NextDouble() * 6 + 0.05 : Pow(10, 2 * random.NextDouble() + 2) * m;
                int nrev = random.Next(2) == 0 ? m : -m;

                if (NearSingularity(r0, rf))
                    continue;

                Russell.SolveWithState(1.0, r0, rf, dt, geometry, nrev, null, 50, 0.0, out int iters);
                typical.Add(iters);
            }

            LogIterations("typical", typical);
            Assert.True(typical.Average() < 2.0);
            Assert.True(typical.Max() <= 3);
        }

        [Fact]
        private void InvalidInputsThrow()
        {
            var r0 = new V3(1, 0, 0);

            // prograde and retrograde need an axis
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, new V3(0, 1, 0), 1.0, TransferGeometry.Prograde));
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, new V3(0, 1, 0), 1.0, TransferGeometry.Retrograde));

            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, r0, 1.0));

            // the exact half-rev needs an axis which is not parallel to r0, and has no derivatives
            var rfHalfRev = new V3(-2, 0, 0);
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rfHalfRev, 1.0));
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rfHalfRev, 1.0, TransferGeometry.LongWay));
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rfHalfRev, 1.0, TransferGeometry.Prograde, h: new V3(3, 0, 0)));
            Assert.Throws<ArgumentException>(() =>
                Russell.Solve(1.0, new DualV3(r0, V3.zero), new DualV3(rfHalfRev, V3.zero), new Dual(1.0, 1.0), TransferGeometry.Prograde, h: new V3(0, 0, 1)));
            Russell.Solve(1.0, r0, rfHalfRev, 1.0, TransferGeometry.Prograde, h: new V3(0, 0, 1));

            // too close to the r1 == r2 singularity
            var rf = new V3(Cos(1e-4), Sin(1e-4), 0);
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rf, 1.0, TransferGeometry.ShortWay));
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rf, 10.0, TransferGeometry.LongWay));
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rf, 10.0, TransferGeometry.ShortWay, 1));

            // zero-rev time of flight too small
            rf = new V3(0, 1, 0);
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rf, 0.9e-3 * ParabolicTof(r0, rf, 1), TransferGeometry.ShortWay));
            Assert.Throws<ArgumentException>(() => Russell.Solve(1.0, r0, rf, 0.9e-3 * ParabolicTof(r0, rf, -1), TransferGeometry.LongWay));
            Russell.Solve(1.0, r0, rf, 1.1e-3 * ParabolicTof(r0, rf, 1), TransferGeometry.ShortWay);
            Russell.Solve(1.0, r0, rf, 1.1e-3 * ParabolicTof(r0, rf, -1), TransferGeometry.LongWay);
        }

        // central differences of the V3 solver along the direction of the dual parts
        private static void CheckDualAgainstFiniteDifferences(V3 r0, V3 rf, double dt, TransferGeometry direction, int n, V3 dr0, V3 drf, double ddt,
            double tol)
        {
            (DualV3 vi, DualV3 vf) = Russell.Solve(1.0, new DualV3(r0, dr0), new DualV3(rf, drf), new Dual(dt, ddt), direction, n);

            double h = 1e-6 * Min(1.0, dt);
            (V3 viPlus, V3 vfPlus) = Russell.Solve(1.0, r0 + h * dr0, rf + h * drf, dt + h * ddt, direction, n);
            (V3 viMinus, V3 vfMinus) = Russell.Solve(1.0, r0 - h * dr0, rf - h * drf, dt - h * ddt, direction, n);

            double scale = Max(1.0, Max(vi.D.magnitude, vf.D.magnitude));

            ((viPlus - viMinus) / (2 * h) / scale).ShouldEqual(vi.D / scale, tol);
            ((vfPlus - vfMinus) / (2 * h) / scale).ShouldEqual(vf.D / scale, tol);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void DualMatchesFiniteDifferences(int seed)
        {
            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var dr0 = new V3(2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1);
            var drf = new V3(2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1);
            double dt = random.NextDouble() * 6 + 0.05;
            double ddt = 2 * random.NextDouble() - 1;
            int direction = random.Next(2) == 0 ? 1 : -1;

            // the partials are singular at the half-rev
            if (V3.Dot(r0.normalized, rf.normalized) < -0.999)
                return;

            CheckDualAgainstFiniteDifferences(r0, rf, dt, Geometry(direction), 0, dr0, drf, ddt, 1e-5);
        }

        [Theory, MemberData(nameof(Seeds))]
        private void DualMultipleRevolutionMatchesFiniteDifferences(int seed)
        {
            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var dr0 = new V3(2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1);
            var drf = new V3(2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1);
            int m = random.Next(1, 20);
            double dt = Pow(10, 2 * random.NextDouble() + 2) * m;
            double ddt = 2 * random.NextDouble() - 1;
            int direction = random.Next(2) == 0 ? 1 : -1;

            if (V3.Dot(r0.normalized, rf.normalized) < -0.999)
                return;

            CheckDualAgainstFiniteDifferences(r0, rf, dt, Geometry(direction), m, dr0, drf, ddt, 1e-5);
            CheckDualAgainstFiniteDifferences(r0, rf, dt, Geometry(direction), -m, dr0, drf, ddt, 1e-5);
        }

        // fast hyperbolas, which use the series in 1/k (long way) or iterate on p (short way)
        [Theory, MemberData(nameof(Seeds))]
        private void DualSmallTimeOfFlightMatchesFiniteDifferences(int seed)
        {
            var random = new Random(seed);

            var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var rf = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
            var dr0 = new V3(2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1);
            var drf = new V3(2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1, 2 * random.NextDouble() - 1);
            int direction = random.Next(2) == 0 ? 1 : -1;
            double dt = ParabolicTof(r0, rf, direction) * Pow(10, -1.5 - 1.4 * random.NextDouble());
            double ddt = 2 * random.NextDouble() - 1;

            if (Abs(V3.Dot(r0.normalized, rf.normalized)) > 0.999)
                return;

            CheckDualAgainstFiniteDifferences(r0, rf, dt, Geometry(direction), 0, dr0, drf, ddt, 1e-5);
        }
    }
}
