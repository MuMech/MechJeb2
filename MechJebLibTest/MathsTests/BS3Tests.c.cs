/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using System.Collections.Generic;
using AssertExtensions;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.MathsTests
{
    public class BS3Tests
    {
        private class SimpleOscillator
        {
            private readonly double _k;
            private readonly double _m;

            public SimpleOscillator(double k, double m)
            {
                _k = k;
                _m = m;
            }

            public int N => 2;

            public void dydt(IList<double> y, double x, IList<double> dy)
            {
                dy[0] = y[1];
                dy[1] = -_k / _m * y[0];
            }
        }

        [Fact]
        public void SimpleOscillatorTest()
        {
            var random = new Random();

            const int NTRIALS = 50;

            for (int n = 0; n < NTRIALS; n++)
            {
                double k = 2 * random.NextDouble() + 1;
                double m = 2 * random.NextDouble() + 1;
                double x0 = 4 * random.NextDouble() - 2;
                double v0 = 4 * random.NextDouble() - 2;

                double t0 = 4 * random.NextDouble() - 2;
                double tf = 4 * random.NextDouble() - 2;

                int count1 = random.Next(20, 40);
                int count2 = random.Next(5, 40);

                var solver = new BS3 { Interpnum = count1, Rtol = 1e-8, Atol = 1e-8, Maxiter = 200000 };
                var ode = new SimpleOscillator(k, m);
                var f = new Action<IList<double>, double, IList<double>>(ode.dydt);

                using var y0 = Vn.Rent(2);
                y0[0] = x0;
                y0[1] = v0;
                using var yf = Vn.Rent(2);
                double omega = Sqrt(k / m);

                double dt = (tf - t0) / count1;
                double dt2 = (tf - t0) / count2;

                double[] expected = new double[count1 + 1];

                for (int i = 0; i <= count1; i++)
                {
                    double t = t0 + dt * i;
                    expected[i] = x0 * Cos(omega * (t - t0)) + v0 * Sin(omega * (t - t0)) / omega;
                }

                double[] expected2 = new double[count2 + 1];

                for (int i = 0; i <= count2; i++)
                {
                    double t = t0 + dt2 * i;
                    expected2[i] = x0 * Cos(omega * (t - t0)) + v0 * Sin(omega * (t - t0)) / omega;
                }

                for (int i = 0; i <= count1; i++)
                {
                    double t = t0 + dt * i;
                    solver.Solve(f, y0, yf, t0, t);
                    yf[0].ShouldEqual(expected[i], 4e-6);
                }

                using (var interpolant = Hn.Get(ode.N))
                {
                    solver.Solve(f, y0, yf, t0, tf, interpolant);

                    for (int i = 0; i <= count1; i++)
                    {
                        double t = t0 + dt * i;
                        using Vn y = interpolant.Evaluate(t);

                        y[0].ShouldEqual(expected[i], 4e-6);
                    }

                    for (int i = 0; i <= count2; i++)
                    {
                        double t = t0 + dt2 * i;
                        using Vn y = interpolant.Evaluate(t);

                        y[0].ShouldEqual(expected2[i], 2e-2);
                    }
                }

                long start = GC.GetAllocatedBytesForCurrentThread();

                using (var interpolant = Hn.Get(ode.N))
                {
                    solver.Solve(f, y0, yf, t0, tf, interpolant);

                    for (int i = 0; i <= count1; i++)
                    {
                        double t = t0 + dt * i;
                        using Vn y = interpolant.Evaluate(t);

                        y[0].ShouldEqual(expected[i], 4e-6);
                    }

                    for (int i = 0; i <= count2; i++)
                    {
                        double t = t0 + dt2 * i;
                        using Vn y = interpolant.Evaluate(t);

                        y[0].ShouldEqual(expected2[i], 2e-2);
                    }
                }

                Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - start);
            }
        }
    }
}
