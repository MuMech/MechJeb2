/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using System.Collections.Generic;
using AssertExtensions;
using MechJebLib.Functions;
using MechJebLib.ODE;
using MechJebLib.Primitives;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.MathsTests
{
    public class DP5Tests
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

                var solver = new DP5 { Interpnum = count1, Rtol = 1e-9, Atol = 0, Maxiter = 2000 };
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

        private class VacuumKernel
        {
            public int N => 6;

            public void dydt(IList<double> yin, double x, IList<double> dyout)
            {
                var r = new V3(yin[0], yin[1], yin[2]);
                var v = new V3(yin[3], yin[4], yin[5]);

                double rm2 = r.sqrMagnitude;
                double rm = Sqrt(rm2);
                double rm3 = rm2 * rm;

                V3 dr = v;
                V3 dv = -r / rm3;

                dyout.Set(0, dr);
                dyout.Set(3, dv);
            }
        }

        private static double Func(double t, IList<double> y, AbstractIVP i)
        {
            var r = new V3(y[0], y[1], y[2]);

            return r.magnitude - 1.5;
        }

        [Fact]
        public void AltitudeEventTest()
        {
            var ode = new VacuumKernel();
            var solver = new DP5 { Rtol = 1e-9, Atol = 1e-9, Maxiter = 2000 };

            var r0 = new V3(1, 0, 0);
            var v0 = new V3(0, 1.3, 0);

            var e = new List<Event> { new Event(Func) };

            double[] y0 = new double[6];
            double[] yf = new double[6];

            y0.Set(0, r0);
            y0.Set(3, v0);

            solver.Solve(ode.dydt, y0, yf, 0, 10, events: e);

            e[0].Time.ShouldEqual(Astro.TimeToNextRadius(1.0, r0, v0, 1.5), 1e-9);
        }
    }
}
