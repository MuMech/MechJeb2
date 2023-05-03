/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using AssertExtensions;
using MechJebLib.Core.TwoBody;
using MechJebLib.Core.ODE;
using MechJebLib.Primitives;
using Xunit;
using Xunit.Abstractions;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Maths
{
    public class FarnocchiaTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public FarnocchiaTests(ITestOutputHelper testOutputHelper)
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
                var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double dt = 40 * random.NextDouble() - 20;

                (V3 rf, V3 vf) = Farnocchia.Solve(1.0, dt, r0, v0);
                (V3 rp, V3 vp) = Farnocchia.Solve(1.0, -dt, rf, vf);

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

            public void dydt(Vn yin, double x, Vn dyout)
            {
                var r = new V3(yin[0], yin[1], yin[2]);
                var v = new V3(yin[3], yin[4], yin[5]);

                double rm2 = r.sqrMagnitude;
                double rm = Math.Sqrt(rm2);
                double rm3 = rm2 * rm;

                V3 dr = v;
                V3 dv = -r / rm3;

                dyout.Set(0, dr);
                dyout.Set(3, dv);
            }
        }

        [Fact]
        private void RandomComparedToDormandPrince()
        {
            var solver = new DormandPrince5();

            solver.Accuracy       = 1e-9;
            solver.Hmin           = 1e-9;
            solver.ThrowOnMaxIter = true;
            solver.Maxiter        = 2000;

            const int NTRIALS = 50;

            var random = new Random();

            for (int i = 0; i < NTRIALS; i++)
            {
                var r0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                var v0 = new V3(4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2, 4 * random.NextDouble() - 2);
                double dt = 10 * random.NextDouble() - 5;

                (V3 rf, V3 vf) = Farnocchia.Solve(1.0, dt, r0, v0);

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
                    catch (ArgumentException)
                    {
                        // the ODE integrator can throw iterations exceeded for certain initial
                        // conditions it inherently has issues with.
                        continue;
                    }

                    rf2 = yf.Get(0);
                    vf2 = yf.Get(3);
                }

                if (!NearlyEqual(rf, rf2, 1e-5) || !NearlyEqual(vf, vf2, 1e-5))
                {
                    _testOutputHelper.WriteLine("r0 :" + r0 + " v0:" + v0 + " dt:" + dt + "\nrf:" + rf + " vf:" + vf + "\nrf2:" + rf2 + " vf2:" +
                                                vf2 + "\n");
                }

                rf.ShouldEqual(rf2, 1e-5);
                vf.ShouldEqual(vf2, 1e-5);
            }
        }
    }
}
