/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using System.Collections.Generic;
using MechJebLib.Functions;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using MechJebLib.TwoBody;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;
using static System.Math;
using Random = System.Random;

namespace MechJebLibTest.TwoBodyTests
{
    public class ShepperdTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ShepperdTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        public static IEnumerable<object[]> Seeds()
        {
            for (int i = 0; i <= 500; i++)
                yield return new object[] { i };
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void RandomForwardAndBack(int seed)
        {
            var rng = new Random(seed);

            var    r0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            var    v0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            double dt = 40 * rng.NextDouble() - 20;

            (V3 rf, V3 vf) = Shepperd.Solve(1.0, dt, r0, v0);
            (V3 rp, V3 vp) = Shepperd.Solve(1.0, -dt, rf, vf);

            if (!NearlyEqual(rp, r0, 1e-8) || !NearlyEqual(vp, v0, 1e-8))
            {
                _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + "\nrf:" + rf + " vf:" + vf + "\nrf2:" + rp + " vf2:" +
                    vp + "\n");
            }

            rp.ShouldEqual(r0, 1e-8);
            vp.ShouldEqual(v0, 1e-8);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void RandomForwardAndBack2(int seed)
        {
            var rng = new Random(seed);

            var    r0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            var    v0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            double dt = 40 * rng.NextDouble() - 20;

            (V3 rf, V3 vf, M3 _, M3 _, M3 _, M3 _) = Shepperd.Solve2(1.0, dt, r0, v0);
            (V3 rp, V3 vp, M3 _, M3 _, M3 _, M3 _) = Shepperd.Solve2(1.0, -dt, rf, vf);

            if (!NearlyEqual(rp, r0, 1e-8) || !NearlyEqual(vp, v0, 1e-8))
                _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + "\nrf:" + rf + " vf:" + vf + "\nrp:" + rp + " vp:" + vp);

            rp.ShouldEqual(r0, 1e-8);
            vp.ShouldEqual(v0, 1e-8);
        }

        private readonly KeplerODE _ode = new KeplerODE();

        private class KeplerODE
        {
            public static int N => 6;

            public void Rhs(Vec yin, double x, Vec dyout)
            {
                var r = new V3(yin[0], yin[1], yin[2]);
                var v = new V3(yin[3], yin[4], yin[5]);

                double rm2 = r.sqrMagnitude;
                double rm  = Sqrt(rm2);
                double rm3 = rm2 * rm;

                V3 dr = v;
                V3 dv = -r / rm3;

                dyout.Set(0, dr);
                dyout.Set(3, dv);
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void RandomComparedToDP5(int seed)
        {
            var solver = new DP5 { Rtol = 1e-6, Hmin = EPS, ThrowOnMaxIter = true, Maxiter = 2000 };
            var rng    = new Random(seed);

            var    r0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            var    v0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            double dt = 10 * rng.NextDouble() - 5;

            (double _, double ecc, double _, double _, double _, double _, double l) =
                Astro.KeplerianFromStateVectors(1.0, r0, v0);

            // RK methods have issue with small SLRs
            if (l < 0.1)
                return;

            (V3 rf, V3 vf) = Shepperd.Solve(1.0, dt, r0, v0);

            V3 rf2, vf2;

            using (var y0 = Vec.Rent(KeplerODE.N))
            using (var yf = Vec.Rent(KeplerODE.N))
            {
                y0.Set(0, r0);
                y0.Set(3, v0);

                try
                {
                    solver.Solve(_ode.Rhs, y0, yf, 0, dt);
                }
                catch (ArgumentException) // sometimes RK method still throws
                {
                    return;
                }

                rf2 = yf.Get(0);
                vf2 = yf.Get(3);
            }

            if (!NearlyEqual(rf, rf2, 1e-5) || !NearlyEqual(vf, vf2, 1e-5))
            {
                _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + " ecc:" + ecc + "\nrf:" + rf + " vf:" + vf + "\nrf2:" +
                    rf2 + " vf2:" +
                    vf2 + "\n");
            }

            rf.ShouldEqual(rf2, 1e-5);
            vf.ShouldEqual(vf2, 1e-5);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void RandomComparedToTsit5(int seed)
        {
            var solver = new Tsit5 { Rtol = 1e-6, Hmin = EPS, ThrowOnMaxIter = true, Maxiter = 2000 };
            var rng    = new Random(seed);

            var    r0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            var    v0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            double dt = 10 * rng.NextDouble() - 5;

            (double _, double ecc, double _, double _, double _, double _, double l) =
                Astro.KeplerianFromStateVectors(1.0, r0, v0);

            // RK methods have issue with small SLRs
            if (l < 0.1)
                return;

            (V3 rf, V3 vf) = Shepperd.Solve(1.0, dt, r0, v0);

            V3 rf2, vf2;

            using (var y0 = Vec.Rent(6))
            using (var yf = Vec.Rent(6))
            {
                y0.Set(0, r0);
                y0.Set(3, v0);

                try
                {
                    solver.Solve(_ode.Rhs, y0, yf, 0, dt);
                }
                catch (ArgumentException) // sometimes RK method still throws
                {
                    return;
                }

                rf2 = yf.Get(0);
                vf2 = yf.Get(3);
            }

            if (!NearlyEqual(rf, rf2, 1e-5) || !NearlyEqual(vf, vf2, 1e-5))
            {
                _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + " ecc:" + ecc + "\nrf:" + rf + " vf:" + vf + "\nrf2:" +
                    rf2 + " vf2:" +
                    vf2 + "\n");
            }

            rf.ShouldEqual(rf2, 1e-5);
            vf.ShouldEqual(vf2, 1e-5);
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void RandomComparedToDP8(int seed)
        {
            var solver = new DP8
            {
                Atol = 1e-6,
                Rtol = 1e-4,
                Hmin = EPS,
                ThrowOnMaxIter = true,
                Maxiter = 2000000
            };
            var rng = new Random(seed);

            var    r0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            var    v0 = new V3(4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2, 4 * rng.NextDouble() - 2);
            double dt = 10 * rng.NextDouble() - 5;

            (double _, double ecc, double _, double _, double _, double _, double l) =
                Astro.KeplerianFromStateVectors(1.0, r0, v0);

            // RK methods have issue with small SLRs
            if (l < 0.1)
                return;

            (V3 rf, V3 vf) = Shepperd.Solve(1.0, dt, r0, v0);

            V3 rf2, vf2;

            using (var y0 = Vec.Rent(6))
            using (var yf = Vec.Rent(6))
            {
                y0.Set(0, r0);
                y0.Set(3, v0);

                try
                {
                    solver.Solve(_ode.Rhs, y0, yf, 0, dt);
                }
                catch (ArgumentException) // sometimes RK method still throws
                {
                    return;
                }

                rf2 = yf.Get(0);
                vf2 = yf.Get(3);
            }

            if (!NearlyEqual(rf, rf2, 1e-5) || !NearlyEqual(vf, vf2, 1e-5))
            {
                _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + " ecc:" + ecc + "\nrf:" + rf + " vf:" + vf + "\nrf2:" +
                    rf2 + " vf2:" +
                    vf2 + "\n");
            }

            rf.ShouldEqual(rf2, 1e-5);
            vf.ShouldEqual(vf2, 1e-5);
        }

        // Build the STM by central finite differences using Solve() as the propagator.
        // Returns the four 3x3 blocks of the 6x6 state transition matrix:
        //   [ 𝛿rf ] = [ stm00 stm01 ] [ 𝛿r0 ]
        //   [ 𝛿vf ]   [ stm10 stm11 ] [ 𝛿v0 ]
        private static (M3 stm00, M3 stm01, M3 stm10, M3 stm11) StmByFiniteDifference(
            double mu, double dt, V3 r0, V3 v0, double h)
        {
            var rCols    = new V3[3];
            var vCols    = new V3[3];
            var rColsVel = new V3[3];
            var vColsVel = new V3[3];

            for (int j = 0; j < 3; j++)
            {
                V3 r0p = r0;
                r0p[j] += h;
                V3 r0m = r0;
                r0m[j] -= h;
                (V3 rfp, V3 vfp) = Shepperd.Solve(mu, dt, r0p, v0);
                (V3 rfm, V3 vfm) = Shepperd.Solve(mu, dt, r0m, v0);
                rCols[j] = (rfp - rfm) / (2 * h);
                vCols[j] = (vfp - vfm) / (2 * h);

                V3 v0p = v0;
                v0p[j] += h;
                V3 v0m = v0;
                v0m[j] -= h;
                (rfp, vfp) = Shepperd.Solve(mu, dt, r0, v0p);
                (rfm, vfm) = Shepperd.Solve(mu, dt, r0, v0m);
                rColsVel[j] = (rfp - rfm) / (2 * h);
                vColsVel[j] = (vfp - vfm) / (2 * h);
            }

            var stm00 = new M3(rCols[0], rCols[1], rCols[2]);
            var stm01 = new M3(rColsVel[0], rColsVel[1], rColsVel[2]);
            var stm10 = new M3(vCols[0], vCols[1], vCols[2]);
            var stm11 = new M3(vColsVel[0], vColsVel[1], vColsVel[2]);
            return (stm00, stm01, stm10, stm11);
        }

        private void AssertStmMatchesFD(double mu, double dt, V3 r0, V3 v0, double tol = 1e-6, double h = 1e-6)
        {
            (V3 _, V3 _, M3 a00, M3 a01, M3 a10, M3 a11) = Shepperd.Solve2(mu, dt, r0, v0);
            (M3 b00, M3 b01, M3 b10, M3 b11) = StmByFiniteDifference(mu, dt, r0, v0, h);

            if (!NearlyEqual(a00, b00, tol) || !NearlyEqual(a01, b01, tol) ||
                !NearlyEqual(a10, b10, tol) || !NearlyEqual(a11, b11, tol))
            {
                _testOutputHelper.WriteLine($"r0:{r0} v0:{v0} dt:{dt}");
                _testOutputHelper.WriteLine($"stm00 analytic:\n{a00}\nstm00 FD:\n{b00}");
                _testOutputHelper.WriteLine($"stm01 analytic:\n{a01}\nstm01 FD:\n{b01}");
                _testOutputHelper.WriteLine($"stm10 analytic:\n{a10}\nstm10 FD:\n{b10}");
                _testOutputHelper.WriteLine($"stm11 analytic:\n{a11}\nstm11 FD:\n{b11}");
            }

            a00.ShouldEqual(b00, tol);
            a01.ShouldEqual(b01, tol);
            a10.ShouldEqual(b10, tol);
            a11.ShouldEqual(b11, tol);
        }

        [Fact]
        public void Stm_CircularEquatorialPrograde() => AssertStmMatchesFD(1.0, 3 * PI / 4, new V3(1, 0, 0), new V3(0, 1, 0));

        [Fact]
        public void Stm_CircularEquatorialRetrograde() => AssertStmMatchesFD(1.0, 3 * PI / 4, new V3(1, 0, 0), new V3(0, -1, 0));

        [Fact]
        public void Stm_CircularPolar() => AssertStmMatchesFD(1.0, 3 * PI / 4, new V3(1, 0, 0), new V3(0, 0, 1));

        [Fact]
        public void Stm_EllipticalEquatorial()
        {
            // v = 1.2 at r = 1, mu = 1 -> e ≈ 0.44, a ≈ 1.79
            AssertStmMatchesFD(1.0, 2.0, new V3(1, 0, 0), new V3(0, 1.2, 0));
        }

        [Fact]
        public void Stm_EllipticalInclined() => AssertStmMatchesFD(1.0, 2.0, new V3(1, 0, 0), new V3(0, 0.9, 0.3));

        [Fact]
        public void Stm_HyperbolicEquatorial()
        {
            // v = 1.5 at r = 1, mu = 1 -> hyperbolic (specific energy > 0)
            AssertStmMatchesFD(1.0, 2.0, new V3(1, 0, 0), new V3(0, 1.5, 0));
        }

        [Fact]
        public void Stm_HyperbolicInclined() => AssertStmMatchesFD(1.0, 2.0, new V3(1, 0, 0), new V3(0, 1.4, 0.3));
    }
}
