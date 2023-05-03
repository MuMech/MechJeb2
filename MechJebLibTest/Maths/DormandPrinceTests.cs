/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: MIT-0 OR LGPL-2.1+ OR CC0-1.0
 */

using System;
using AssertExtensions;
using MechJebLib.Core.ODE;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.Maths
{
    public class DormandPrinceTests
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

            public void dydt(Vn y, double x, Vn dy)
            {
                dy[0] = y[1];
                dy[1] = -_k / _m * y[0];
            }
        }

        [Fact]
        public void SimpleOscillatorInterpolant()
        {
            double k = 4;
            double m = 1;
            double x0 = 0;
            double v0 = 1;
            double tf = 4;
            var solver = new DormandPrince5 { Interpnum = 20, Accuracy = 1e-9 };
            var ode = new SimpleOscillator(k, m);
            var f = new Action<Vn, double, Vn>(ode.dydt);

            using var y0 = Vn.Rent(2);
            y0[0] = x0;
            y0[1] = v0;
            using var yf = Vn.Rent(2);
            double omega = Math.Sqrt(k / m);

            const int COUNT = 20;

            double[] expected = new double[COUNT + 1];

            for (int i = 0; i <= COUNT; i++)
            {
                double t = tf * i / COUNT;
                expected[i] = x0 * Math.Cos(omega * t) + v0 * Math.Sin(omega * t) / omega;
            }

            for (int i = 0; i <= COUNT; i++)
            {
                double t = tf * i / COUNT;
                solver.Solve(f, y0, yf, 0, t);
                yf[0].ShouldEqual(expected[i], 1e-10);
            }

            using (var interpolant = Hn.Get(ode.N))
            {
                solver.Solve(f, y0, yf, 0, 4, interpolant);

                for (int i = 0; i <= COUNT; i++)
                {
                    double t = tf * i / COUNT;
                    using Vn y = interpolant.Evaluate(t);

                    y[0].ShouldEqual(expected[i], 1e-10);
                }
            }

            long start = GC.GetAllocatedBytesForCurrentThread();

            using (var interpolant = Hn.Get(ode.N))
            {
                solver.Solve(f, y0, yf, 0, 4, interpolant);

                for (int i = 0; i <= COUNT; i++)
                {
                    double t = tf * i / COUNT;
                    using Vn y = interpolant.Evaluate(t);

                    y[0].ShouldEqual(expected[i], 1e-10);
                }
            }

            Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - start);
        }
    }
}
