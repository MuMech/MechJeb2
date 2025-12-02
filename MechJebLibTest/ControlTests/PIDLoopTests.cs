using MechJebLib.Control;
using Xunit;
using static System.Math;

namespace MechJebLibTest.ControlTests
{
    public class PIDLoopTests
    {
        [Fact]
        public void ProportionalTest()
        {
            var pid = new PIDLoop { Kp = 2.0, Ts = 0.02 };

            Assert.Equal(2.0, pid.Update(0, -1), 8);
            Assert.Equal(-2.0, pid.Update(0, 1), 8);
        }

        [Fact]
        public void ProportionalTest2()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Ts = 0.02 };

            Assert.Equal(2.0, pid.Update(0, -1), 8);
            Assert.Equal(-2.0, pid.Update(0, 1), 8);
        }

        [Fact]
        public void IntegralTest()
        {
            var pid = new PIDLoop { Kp = 2.0, Ki = 1.0, Ts = 0.02 };

            Assert.Equal(2.01, pid.Update(1, 0), 8);
            Assert.Equal(2.03, pid.Update(1, 0), 8);
            Assert.Equal(2.05, pid.Update(1, 0), 8);
            Assert.Equal(2.07, pid.Update(1, 0), 8);
        }

        [Fact]
        public void IntegralTest2()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Ki = 1.0, Ts = 0.02 };

            Assert.Equal(2.01, pid.Update(1, 0), 8);
            Assert.Equal(2.03, pid.Update(1, 0), 8);
            Assert.Equal(2.05, pid.Update(1, 0), 8);
            Assert.Equal(2.07, pid.Update(1, 0), 8);
        }

        /// <summary>
        ///     This plant requires a PDF controller and tests the filtered derivative implementation.
        /// </summary>
        [Fact]
        public void FirstOrderLagWithIntegrator()
        {
            // From Matlab with pidtune() to reference-tracking
            var pid = new PIDLoop2
            {
                Kp = 2.10612233627086,
                Ti = 58.8041302675465,
                Td = 0.807330894982224,
                N  = 3.37809448105627,
                Ts = 0.02
            };

            pid.K.ShouldEqual(2.10612233627086);
            pid.Ti.ShouldEqual(58.8041302675465);
            pid.Td.ShouldEqual(0.807330894982224);
            pid.N.ShouldEqual(3.37809448105627);
            pid.Ts.ShouldEqual(0.02);
            pid.Ki.ShouldEqual(0.0358158912764876, 1e-14);
            pid.Kd.ShouldEqual(1.7003376306836, 1e-14);
            pid.Tf.ShouldEqual(0.238990028108919, 1e-14);

            // Discrete plant G(z) = (b1*z + b2) / (z^2 + a1*z + a2)
            // From MATLAB c2d of 1/(s*(s+1)) with ZOH, T=0.02
            var plant = new Biquad(
                0,
                0.000198673306755302,
                0.000197353227109592,
                -1.98019867330676,
                0.980198673306755
            );

            const double SETPOINT = 1.0;
            const double T_FINAL  = 10.0;

            double y     = 0;
            int    steps = (int)(T_FINAL / pid.Ts);

            double peakValue     = 0;
            double peakTime      = 0;
            double settlingTime  = 0;
            double riseTimeStart = -1;
            double riseTimeEnd   = -1;

            for (int i = 0; i < steps; i++)
            {
                double t = i * pid.Ts;
                double u = pid.Update(SETPOINT, y);
                y = plant.Process(u);

                if (y > peakValue)
                {
                    peakValue = y;
                    peakTime  = t;
                }

                if (riseTimeStart < 0 && y >= 0.1 * SETPOINT)
                    riseTimeStart = t;
                if (riseTimeEnd < 0 && y >= 0.9 * SETPOINT)
                    riseTimeEnd = t;

                // 2% settling time band
                if (Abs(y - SETPOINT) > 0.02 * SETPOINT)
                    settlingTime = t;
            }

            double overshoot = (peakValue - SETPOINT) / SETPOINT * 100.0;
            double riseTime  = riseTimeEnd - riseTimeStart;

            // Expected: Overshoot=4.22%, SettlingTime=1.85s, RiseTime=0.69s, PeakTime=1.42s
            Assert.True(overshoot > 3.0 && overshoot < 7.0,
                $"Overshoot {overshoot:F2}% outside expected range [3%, 7%]. Expected ~4.2%");

            Assert.True(settlingTime > 1.5 && settlingTime < 2.5,
                $"Settling time {settlingTime:F2}s outside expected range [1.5s, 2.5s]. Expected ~1.85s");

            Assert.True(riseTime > 0.5 && riseTime < 1.0,
                $"Rise time {riseTime:F2}s outside expected range [0.5s, 1.0s]. Expected ~0.69s");

            Assert.True(peakTime > 1.0 && peakTime < 2.0,
                $"Peak time {peakTime:F2}s outside expected range [1.0s, 2.0s]. Expected ~1.42s");
        }

        /// <summary>
        ///     This is the FirstOrderLagWithIntegrator test but with the D term turned off.
        /// </summary>
        [Fact]
        public void FirstOrderLagWithIntegratorNoDerivative()
        {
            // From Matlab with pidtune() to reference-tracking
            var pid = new PIDLoop2
            {
                Kp = 2.10612233627086,
                Ti = 58.8041302675465,
                Ts = 0.02
            };

            pid.K.ShouldEqual(2.10612233627086);
            pid.Ti.ShouldEqual(58.8041302675465);
            pid.Ts.ShouldEqual(0.02);
            pid.Ki.ShouldEqual(0.0358158912764876, 1e-14);

            // Discrete plant G(z) = (b1*z + b2) / (z^2 + a1*z + a2)
            // From MATLAB c2d of 1/(s*(s+1)) with ZOH, T=0.02
            var plant = new Biquad(
                0,
                0.000198673306755302,
                0.000197353227109592,
                -1.98019867330676,
                0.980198673306755
            );

            const double SETPOINT = 1.0;
            const double T_FINAL  = 10.0;

            double y     = 0;
            int    steps = (int)(T_FINAL / pid.Ts);

            double peakValue     = 0;
            double peakTime      = 0;
            double settlingTime  = 0;
            double riseTimeStart = -1;
            double riseTimeEnd   = -1;

            for (int i = 0; i < steps; i++)
            {
                double t = i * pid.Ts;
                double u = pid.Update(SETPOINT, y);
                y = plant.Process(u);

                if (y > peakValue)
                {
                    peakValue = y;
                    peakTime  = t;
                }

                if (riseTimeStart < 0 && y >= 0.1 * SETPOINT)
                    riseTimeStart = t;
                if (riseTimeEnd < 0 && y >= 0.9 * SETPOINT)
                    riseTimeEnd = t;

                // 2% settling time band
                if (Abs(y - SETPOINT) > 0.02 * SETPOINT)
                    settlingTime = t;
            }

            double overshoot = (peakValue - SETPOINT) / SETPOINT * 100.0;
            double riseTime  = riseTimeEnd - riseTimeStart;

            // Expected: Overshoot=34.02%, SettlingTime=7.89s, RiseTime=0.94s, PeakTime=2.38s
            Assert.True(overshoot > 30.0 && overshoot < 36.0,
                $"Overshoot {overshoot:F2}% outside expected range [30%, 36%]. Expected ~34.0%");

            Assert.True(settlingTime > 7.5 && settlingTime < 8.0,
                $"Settling time {settlingTime:F2}s outside expected range [7.5s, 8.5s]. Expected ~7.89s");

            Assert.True(riseTime > 0.5 && riseTime < 1.0,
                $"Rise time {riseTime:F2}s outside expected range [0.5s, 1.0s]. Expected ~0.94s");

            Assert.True(peakTime > 2.0 && peakTime < 3.0,
                $"Peak time {peakTime:F2}s outside expected range [2.0s, 3.0s]. Expected ~2.38s");
        }
    }
}
