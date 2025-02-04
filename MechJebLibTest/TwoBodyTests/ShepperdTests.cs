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

namespace MechJebLibTest.TwoBodyTests
{
    public class ShepperdTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public ShepperdTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        private void RandomForwardAndBack()
        {
            const int NTRIALS = 5000;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var    v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double dt = 40 * random.NextDouble() - 20;

                // XXX: this probably needs a test to reject random orbits that are near-parabolic.  See the
                // Farnocchia paper.

                (V3 rf, V3 vf) = Shepperd.Solve(1.0, dt, r0, v0);
                (V3 rp, V3 vp) = Shepperd.Solve(1.0, -dt, rf, vf);

                if (!NearlyEqual(rp, r0, 1e-8) || !NearlyEqual(vp, v0, 1e-8))
                {
                    _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + "\nrf:" + rf + " vf:" + vf + "\nrf2:" + rp + " vf2:" +
                        vp + "\n");
                }

                if ((rp - r0).magnitude / r0.magnitude > 1e-8 || (vp - v0).magnitude / v0.magnitude > 1e-8)
                {
                    _testOutputHelper.WriteLine(r0 + " " + v0);
                }

                rp.ShouldEqual(r0, 1e-8);
                vp.ShouldEqual(vp, 1e-8);
            }
        }

        [Fact]
        private void RandomForwardAndBack2()
        {
            const int NTRIALS = 5;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var    v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double dt = 40 * random.NextDouble() - 20;

                // XXX: this probably needs a test to reject random orbits that are near-parabolic.  See the
                // Farnocchia paper.

                (V3 rf, V3 vf, _, _, _, _) = Shepperd.Solve2(1.0, dt, r0, v0);
                (V3 rp, V3 vp, _, _, _, _) = Shepperd.Solve2(1.0, -dt, rf, vf);

                if (!NearlyEqual(rp, r0, 1e-8) || !NearlyEqual(vp, v0, 1e-8))
                {
                    _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + "\nrf:" + rf + " vf:" + vf + "\nrf2:" + rp + " vf2:" +
                        vp + "\n");
                }

                if ((rp - r0).magnitude / r0.magnitude > 1e-8 || (vp - v0).magnitude / v0.magnitude > 1e-8)
                {
                    _testOutputHelper.WriteLine(r0 + " " + v0);
                }

                rp.ShouldEqual(r0, 1e-8);
                vp.ShouldEqual(vp, 1e-8);
            }
        }

        private readonly VacuumKernel _ode = new VacuumKernel();

        private class VacuumKernel
        {
            public int N => 6;

            public void dydt(IList<double> yin, double x, IList<double> dyout)
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

        [Fact]
        private void RandomComparedToDP5()
        {
            var solver = new DP5 { Rtol = 1e-6, Hmin = EPS, ThrowOnMaxIter = true, Maxiter = 2000 };

            const int NTRIALS = 500;

            var random = new Random();

            int count = 0;

            for (int i = 0; i < NTRIALS; i++)
            {
                var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var    v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double dt = 10 * random.NextDouble() - 5;

                (double _, double ecc, double _, double _, double _, double _, double l) =
                    Astro.KeplerianFromStateVectors(1.0, r0, v0);

                // near-parabolic orbits are difficult for Shepperd, see the Farnocchia paper.
                if (ecc < 1.01 && ecc > 0.99)
                    continue;

                // RK methods have issue with small SLRs
                if (l < 0.1)
                    continue;

                (V3 rf, V3 vf) = Shepperd.Solve(1.0, dt, r0, v0);

                V3 rf2, vf2;

                using (var y0 = Vn.Rent(6))
                using (var yf = Vn.Rent(6))
                {
                    y0.Set(0, r0);
                    y0.Set(3, v0);

                    try
                    {
                        solver.Solve(_ode.dydt, y0, yf, 0, dt);
                    }
                    catch (ArgumentException) // sometimes RK method still throws
                    {
                        continue;
                    }

                    rf2 = yf.Get(0);
                    vf2 = yf.Get(3);
                }

                count++;

                if (!NearlyEqual(rf, rf2, 1e-5) || !NearlyEqual(vf, vf2, 1e-5))
                {
                    _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + " ecc:" + ecc + "\nrf:" + rf + " vf:" + vf + "\nrf2:" +
                        rf2 + " vf2:" +
                        vf2 + "\n");
                }

                rf.ShouldEqual(rf2, 1e-5);
                vf.ShouldEqual(vf2, 1e-5);
            }

            _testOutputHelper.WriteLine($"Successful: {count}");
            Assert.True(count > NTRIALS * 0.90);
        }

        [Fact]
        private void RandomComparedToTsit5()
        {
            var solver = new Tsit5 { Rtol = 1e-6, Hmin = EPS, ThrowOnMaxIter = true, Maxiter = 2000 };

            const int NTRIALS = 500;

            var random = new Random();

            int count = 0;

            for (int i = 0; i < NTRIALS; i++)
            {
                var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var    v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double dt = 10 * random.NextDouble() - 5;

                (double _, double ecc, double _, double _, double _, double _, double l) =
                    Astro.KeplerianFromStateVectors(1.0, r0, v0);

                // near-parabolic orbits are difficult for Shepperd, see the Farnocchia paper.
                if (ecc < 1.01 && ecc > 0.99)
                    continue;

                // RK methods have issue with small SLRs
                if (l < 0.1)
                    continue;

                (V3 rf, V3 vf) = Shepperd.Solve(1.0, dt, r0, v0);

                V3 rf2, vf2;

                using (var y0 = Vn.Rent(6))
                using (var yf = Vn.Rent(6))
                {
                    y0.Set(0, r0);
                    y0.Set(3, v0);

                    try
                    {
                        solver.Solve(_ode.dydt, y0, yf, 0, dt);
                    }
                    catch (ArgumentException) // sometimes RK method still throws
                    {
                        continue;
                    }

                    rf2 = yf.Get(0);
                    vf2 = yf.Get(3);
                }

                count++;

                if (!NearlyEqual(rf, rf2, 1e-5) || !NearlyEqual(vf, vf2, 1e-5))
                {
                    _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + " ecc:" + ecc + "\nrf:" + rf + " vf:" + vf + "\nrf2:" +
                        rf2 + " vf2:" +
                        vf2 + "\n");
                }

                rf.ShouldEqual(rf2, 1e-5);
                vf.ShouldEqual(vf2, 1e-5);
            }

            _testOutputHelper.WriteLine($"Successful: {count}");
            Assert.True(count > NTRIALS * 0.90);
        }

        [Fact]
        private void RandomComparedToDP8()
        {
            var solver = new DP8
            {
                Atol           = 1e-6,
                Rtol           = 1e-4,
                Hmin           = EPS,
                ThrowOnMaxIter = true,
                Maxiter        = 2000000
            };

            const int NTRIALS = 500;

            var random = new Random();

            int count = 0;

            for (int i = 0; i < NTRIALS; i++)
            {
                var    r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var    v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double dt = 10 * random.NextDouble() - 5;

                (double sma, double ecc, double inc, double lan, double argp, double nu, double l) =
                    Astro.KeplerianFromStateVectors(1.0, r0, v0);

                //_testOutputHelper.WriteLine($"sma: {sma} ecc: {ecc} inc: {inc} lan: {lan} argp: {argp} nu: {nu} l: {l}");

                // near-parabolic orbits are difficult for Shepperd, see the Farnocchia paper.
                if (ecc < 1.01 && ecc > 0.99)
                    continue;

                // RK methods have issue with small SLRs
                if (l < 0.1)
                    continue;

                (V3 rf, V3 vf) = Shepperd.Solve(1.0, dt, r0, v0);

                V3 rf2, vf2;

                using (var y0 = Vn.Rent(6))
                using (var yf = Vn.Rent(6))
                {
                    y0.Set(0, r0);
                    y0.Set(3, v0);

                    try
                    {
                        solver.Solve(_ode.dydt, y0, yf, 0, dt);
                    }
                    catch (ArgumentException) // sometimes RK method still throws
                    {
                        continue;
                    }

                    rf2 = yf.Get(0);
                    vf2 = yf.Get(3);
                }

                count++;

                if (!NearlyEqual(rf, rf2, 1e-5) || !NearlyEqual(vf, vf2, 1e-5))
                {
                    _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + " ecc:" + ecc + "\nrf:" + rf + " vf:" + vf + "\nrf2:" +
                        rf2 + " vf2:" +
                        vf2 + "\n");
                }

                rf.ShouldEqual(rf2, 1e-5);
                vf.ShouldEqual(vf2, 1e-5);
            }

            _testOutputHelper.WriteLine($"Successful: {count}");

            Assert.True(count > NTRIALS * 0.90);
        }
    }
}
