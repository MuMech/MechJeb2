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
    public class FarnocchiaTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public FarnocchiaTests(ITestOutputHelper testOutputHelper)
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

            (V3 rf, V3 vf) = Farnocchia.Solve(1.0, dt, r0, v0);
            (V3 rp, V3 vp) = Farnocchia.Solve(1.0, -dt, rf, vf);

            if ((rp - r0).magnitude / r0.magnitude > 1e-8 || (vp - v0).magnitude / v0.magnitude > 1e-8)
            {
                _testOutputHelper.WriteLine(r0 + " " + v0);
            }

            rp.ShouldEqual(r0, 1e-8);
            vp.ShouldEqual(v0, 1e-8);
        }

        private readonly VacuumKernel _ode = new VacuumKernel();

        private class VacuumKernel
        {
            public int N => 6;

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

            (V3 rf, V3 vf) = Farnocchia.Solve(1.0, dt, r0, v0);

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
    }
}
