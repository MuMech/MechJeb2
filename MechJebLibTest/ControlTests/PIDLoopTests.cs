using System.Collections.Generic;
using MechJebLib.Control;
using MechJebLib.ODE;
using Xunit;
using Xunit.Abstractions;

namespace MechJebLibTest.ControlTests
{
    public class PIDLoopTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public PIDLoopTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void LinearFirstOrderRCCircuitPController()
        {
            const double R = 1.0;
            const double C = 1.0;
            const double INV_TAU = 1.0 / (R * C);
            const double V0 = 0.0;
            const double V_TARGET = 1.0;

            var pid = new PIDLoop { Kp = 1.0, H = 0.02 };
            var solver = new BS3 { Maxiter = 2000 };

            double[] y0 = { V0 };
            double[] yf = new double[1];

            for (double t = 0; t <= 10; t += 0.02)
            {
                double u = pid.Update(V_TARGET, y0[0]);

                void dydt(IList<double> y, double x, IList<double> dy)
                {
                    dy[0] = INV_TAU * (u - y[0]);
                    _testOutputHelper.WriteLine($"x={x} y={y[0]} dy={dy[0]}");
                }

                solver.Solve(dydt, y0, yf, t, t + 0.02);
                y0[0] = yf[0];
            }
        }

        [Fact]
        public void ProportionalTest()
        {
            var pid = new PIDLoop { Kp = 2.0, H = 0.02 };

            Assert.Equal(2.0, pid.Update(0, -1), 8);
            Assert.Equal(-2.0, pid.Update(0, 1), 8);
        }

        [Fact]
        public void ProportionalTest2()
        {
            var pid = new PIDLoop2 { Kp = 2.0, H = 0.02 };

            Assert.Equal(2.0, pid.Update(0, -1), 8);
            Assert.Equal(-2.0, pid.Update(0, 1), 8);
        }

        [Fact]
        public void DerivativeTest()
        {
            var pid = new PIDLoop { Kp = 2.0, Kd = 1.0, N = 100, H = 0.02 };

            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(52.0, pid.Update(1, 0), 8);
            Assert.Equal(2.0, pid.Update(1, 0), 8);
            Assert.Equal(2.0, pid.Update(1, 0), 8);
        }

        [Fact]
        public void DerivativeTestBothCoefficients()
        {
            var pid = new PIDLoop { Kp = 2.0, Kd = 1.0, N = 50, H = 0.02 };

            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(35.3333333333333, pid.Update(1, 0), 8);
            Assert.Equal(13.1111111111111, pid.Update(1, 0), 8);
            Assert.Equal(5.70370370370372, pid.Update(1, 0), 8);
        }

        [Fact]
        public void DerivativeTest2()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Kd = 1.0, N = 100, H = 0.02 };

            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(52.0, pid.Update(1, 0), 8);
            Assert.Equal(2.0, pid.Update(1, 0), 8);
            Assert.Equal(2.0, pid.Update(1, 0), 8);
        }

        [Fact]
        public void DerivativeTestBothCoefficients2()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Kd = 1.0, N = 50, H = 0.02 };

            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(0.0, pid.Update(0, 0), 8);
            Assert.Equal(35.3333333333333, pid.Update(1, 0), 8);
            Assert.Equal(13.1111111111111, pid.Update(1, 0), 8);
            Assert.Equal(5.70370370370372, pid.Update(1, 0), 8);
        }

        [Fact]
        public void IntegralTest()
        {
            var pid = new PIDLoop { Kp = 2.0, Ki = 1.0, H = 0.02 };

            Assert.Equal(2.01, pid.Update(1, 0), 8);
            Assert.Equal(2.03, pid.Update(1, 0), 8);
            Assert.Equal(2.05, pid.Update(1, 0), 8);
            Assert.Equal(2.07, pid.Update(1, 0), 8);
        }

        [Fact]
        public void IntegralTest2()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Ki = 1.0, H = 0.02 };

            Assert.Equal(2.01, pid.Update(1, 0), 8);
            Assert.Equal(2.03, pid.Update(1, 0), 8);
            Assert.Equal(2.05, pid.Update(1, 0), 8);
            Assert.Equal(2.07, pid.Update(1, 0), 8);
        }
    }
}
