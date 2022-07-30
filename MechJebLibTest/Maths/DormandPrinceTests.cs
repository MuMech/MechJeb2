/*
 * Copyright Lamont Granquist (lamont@scriptkiddie.org)
 * Dual licensed under the MIT (MIT-LICENSE) license
 * and GPLv2 (GPLv2-LICENSE) license or any later version.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using AssertExtensions;
using MechJebLib.Primitives;
using MuMech.MathJ;
using Xunit;
using static MechJebLib.Utils.Statics;

namespace MechJebLibTest.Maths
{
    public class DormandPrinceTests
    {
        [Theory]
        [ClassData(typeof(SimpleOscillatorTestData))]
        public void SimpleOscillatorTest(double k, double m, double x0, double v0, double tf)
        {
            double t0 = 0.0;

            SimpleOscillator<DormandPrince> ode = new SimpleOscillator<DormandPrince>(k, m);

            ode.Integrator.Hmax           = 0;
            ode.Integrator.Hmin           = EPS;
            ode.Integrator.Accuracy       = 1e-9;
            ode.Integrator.Hstart         = 0;
            ode.Integrator.ThrowOnMaxIter = true;

            var y0 = DD.Rent(2);
            y0[0] = x0;
            y0[1] = v0;
            var yf = DD.Rent(2);
            ode.Integrate(y0, yf, t0, tf);
            double omega = Math.Sqrt(k / m);
            double u = x0 * Math.Cos(omega * (tf - t0)) + v0 * Math.Sin(omega * (tf - t0)) / omega;
            Assert.Equal(u, yf[0], 9);

            long start = GC.GetAllocatedBytesForCurrentThread();

            ode.Integrate(y0, yf, t0, tf);

            Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread() - start );
        }

        private class SimpleOscillator<T> : ODE<T> where T : ODESolver, new()
        {
            private readonly double _k;
            private readonly double _m;

            public SimpleOscillator(double k, double m)
            {
                _k = k;
                _m = m;
            }

            public override int N => 2;

            protected override void dydt(DD y, double x, DD dy)
            {
                dy[0] = y[1];
                dy[1] = -_k / _m * y[0];
            }
        }

        private class SimpleOscillatorTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                double k = 4.0;
                double m = 1.5;
                for (double x0 = -1; x0 < 1.0; x0 += 0.5)
                for (double v0 = -1; v0 < 1.0; v0 += 0.5)
                for (double tf = -1; tf < 4.0; tf += 0.5)
                    yield return new object[] {k, m, x0, v0, tf};
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Fact]
        public void SimpleOscillatorInterpolant()
        {
            double k = 4;
            double m = 1;
            double x0 = 0;
            double v0 = 1;
            SimpleOscillator<DormandPrince> ode = new SimpleOscillator<DormandPrince>(k, m);
            ode.Integrator.Interpnum = 20;

            var y0 = DD.Rent(2);
            y0[0] = x0;
            y0[1] = v0;
            var yf = DD.Rent(2);
            double omega = Math.Sqrt(k / m);
            int t = 3;
            double expected = x0 * Math.Cos(omega * t) + v0 * Math.Sin(omega * t) / omega;
            DD y;

            using (Hn interpolant = ode.GetInterpolant())
            {
                ode.Integrate(y0, yf, 0, 4, interpolant);

                using (y = interpolant.Evaluate(t))
                {
                    y[0].ShouldEqual(expected, 1e-9);
                }

                interpolant.Clear();
            }

            using (Hn interpolant = ode.GetInterpolant())
            {
                long start = GC.GetAllocatedBytesForCurrentThread();

                ode.Integrate(y0, yf, 0, 4, interpolant);
                using (y = interpolant.Evaluate(t))
                {
                    y[0].ShouldEqual(expected, 1e-9);
                }

                Assert.Equal(0,GC.GetAllocatedBytesForCurrentThread() - start );
            }
        }
    }
}
