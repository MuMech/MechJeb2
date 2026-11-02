/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Control;
using Xunit;
using static MechJebLib.Utils.Statics;
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
        public void NaNInputPassesThroughAndRecovers()
        {
            var pid = new PIDLoop { Kp = 2.0, Ki = 1.0, Kd = 0.1, Ts = 0.02 };

            pid.Update(1, 0);
            Assert.True(double.IsNaN(pid.Update(1, double.NaN)));
            Assert.True(double.IsNaN(pid.Update(double.NaN, 0)));
            Assert.True(IsFinite(pid.Update(1, 0)));
            Assert.True(IsFinite(pid.Update(1, 0)));
        }

        [Fact]
        public void NaNInputPassesThroughAndRecovers2()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Ki = 1.0, Kd = 0.1, Ts = 0.02 };

            pid.Update(1, 0);
            Assert.True(double.IsNaN(pid.Update(1, double.NaN)));
            Assert.True(double.IsNaN(pid.Update(double.NaN, 0)));
            Assert.True(IsFinite(pid.Update(1, 0)));
            Assert.True(IsFinite(pid.Update(1, 0)));
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
                N = 3.37809448105627,
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
            const double T_FINAL = 10.0;

            double y = 0;
            int steps = (int)(T_FINAL / pid.Ts);

            double peakValue = 0;
            double peakTime = 0;
            double settlingTime = 0;
            double riseTimeStart = -1;
            double riseTimeEnd = -1;

            for (int i = 0; i < steps; i++)
            {
                double t = i * pid.Ts;
                double u = pid.Update(SETPOINT, y);
                y = plant.Process(u);

                if (y > peakValue)
                {
                    peakValue = y;
                    peakTime = t;
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
            double riseTime = riseTimeEnd - riseTimeStart;

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
            var pid = new PIDLoop2 { Kp = 2.10612233627086, Ti = 58.8041302675465, Ts = 0.02 };

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
            const double T_FINAL = 10.0;

            double y = 0;
            int steps = (int)(T_FINAL / pid.Ts);

            double peakValue = 0;
            double peakTime = 0;
            double settlingTime = 0;
            double riseTimeStart = -1;
            double riseTimeEnd = -1;

            for (int i = 0; i < steps; i++)
            {
                double t = i * pid.Ts;
                double u = pid.Update(SETPOINT, y);
                y = plant.Process(u);

                if (y > peakValue)
                {
                    peakValue = y;
                    peakTime = t;
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
            double riseTime = riseTimeEnd - riseTimeStart;

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

        /// <summary>
        ///     The RCS velocity loop: at a large error P alone saturates the output, and the integral must not wind up
        ///     behind it.  Tracking anti-windup, with its time constant of Ti, does wind up.
        /// </summary>
        [Fact]
        public void ConditionalIntegrationHoldsIntegralWhileSaturated()
        {
            const double TF = 1.0;
            double kp = 0.53 / (3 * Sqrt(2) * TF * TF);
            double ki = kp / (12 * Sqrt(2) * TF);

            var conditional = new PIDLoop2
            {
                Kp = kp, Ki = ki, Ts = 0.02, MinOutput = -1, MaxOutput = 1, ConditionalIntegration = true
            };
            var tracking = new PIDLoop2 { Kp = kp, Ki = ki, Ts = 0.02, MinOutput = -1, MaxOutput = 1 };

            for (int i = 0; i < 500; i++)
            {
                Assert.Equal(1.0, conditional.Update(20, 0), 12);
                tracking.Update(20, 0);
            }

            Assert.Equal(0.0, conditional.ITerm, 12);
            Assert.True(tracking.ITerm > 0.4, $"tracking ITerm {tracking.ITerm} should have wound up");
        }

        [Fact]
        public void ConditionalIntegrationIntegratesInsideLimits()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Ki = 1.0, Ts = 0.02, MinOutput = -10, MaxOutput = 10, ConditionalIntegration = true };

            Assert.Equal(2.01, pid.Update(1, 0), 8);
            Assert.Equal(2.03, pid.Update(1, 0), 8);
            Assert.Equal(2.05, pid.Update(1, 0), 8);
        }

        /// <summary>
        ///     The step to 0.05 would pass the limit, so the integral holds at 0.03 and the output sits at the limit, not one
        ///     step inside it.
        /// </summary>
        [Fact]
        public void ConditionalIntegrationReachesTheLimit()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Ki = 1.0, Ts = 0.02, MinOutput = -2.04, MaxOutput = 2.04, ConditionalIntegration = true };

            Assert.Equal(2.01, pid.Update(1, 0), 8);
            Assert.Equal(2.03, pid.Update(1, 0), 8);

            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(2.04, pid.Update(1, 0), 8);
                Assert.Equal(0.03, pid.ITerm, 8);
            }
        }

        /// <summary>
        ///     P alone is past the limit before the deadband, but not after it, so the integral runs until the output
        ///     itself saturates.
        /// </summary>
        [Fact]
        public void ConditionalIntegrationSeesTheOutputDeadband()
        {
            var pid = new PIDLoop2
            {
                Kp = 2.0, Ki = 1.0, Ts = 0.02, MinOutput = -1.6, MaxOutput = 1.6, OutputDeadband = 0.5, ConditionalIntegration = true
            };

            Assert.Equal(1.51, pid.Update(1, 0), 8);
            Assert.Equal(1.53, pid.Update(1, 0), 8);
            Assert.Equal(1.55, pid.Update(1, 0), 8);
            Assert.Equal(1.57, pid.Update(1, 0), 8);
            Assert.Equal(1.59, pid.Update(1, 0), 8);

            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(1.6, pid.Update(1, 0), 8);
                Assert.Equal(0.09, pid.ITerm, 8);
            }
        }

        /// <summary>
        ///     The step to 0.05 would pass the output limit, but MaxIntegral cuts it to 0.04, which doesn't, so it's kept.
        /// </summary>
        [Fact]
        public void ConditionalIntegrationSeesTheIntegralLimits()
        {
            var pid = new PIDLoop2
            {
                Kp = 2.0, Ki = 1.0, Ts = 0.02, MinOutput = -2.045, MaxOutput = 2.045, MinIntegral = -0.04, MaxIntegral = 0.04,
                ConditionalIntegration = true
            };

            Assert.Equal(2.01, pid.Update(1, 0), 8);
            Assert.Equal(2.03, pid.Update(1, 0), 8);

            for (int i = 0; i < 6; i++)
            {
                Assert.Equal(2.04, pid.Update(1, 0), 8);
                Assert.Equal(0.04, pid.ITerm, 8);
            }
        }

        /// <summary>
        ///     Velocity steps on the PIDLoop2 that PIDControllerV2 runs for MechJebModuleRCSController.
        /// </summary>
        [Theory]
        [InlineData(0.3, 1.0)] // audit item CTRL-10: 7.3 cm/s of overshoot, and still 3 cm/s off after 20 s
        [InlineData(0.1, 1.0)] // a long saturation, the integral would pass 1
        [InlineData(1.0, 2.0)]
        [InlineData(3.0, 5.0)] // the integral peaks near 0.5 here, so limiting it to the output range would never act
        public void ConditionalIntegrationVelocityStepDoesNotOvershoot(double accel, double dv)
        {
            (double overshoot, double after20) = RcsVelocityStep(RcsLoop(true), accel, dv);

            Assert.True(overshoot < 0.005, $"overshoot {overshoot} m/s");
            Assert.True(Abs(after20) < 0.01, $"{after20} m/s left after 20 s");
        }

        [Fact]
        public void ConditionalIntegrationWorksThroughTheThrustFloor()
        {
            // proportional alone is 3 %, under the 5 % ModuleRCS needs to fire, so only the integral can remove this error
            (_, double after20) = RcsVelocityStep(RcsLoop(true), 1.0, 0.005);

            Assert.True(Abs(after20) < 0.004, $"{after20} m/s left after 20 s");
        }

        /// <summary>
        ///     Why PIDControllerBase sets ConditionalIntegration: P alone saturates this loop for the whole step, and
        ///     tracking lets the integral relax toward the output limit with a time constant of Ti (17 s here).  All of it
        ///     unwinds past zero: 2.6 cm/s, against none for conditional integration.
        /// </summary>
        [Fact]
        public void TrackingVelocityStepOvershoots()
        {
            (double overshoot, _) = RcsVelocityStep(RcsLoop(false), 3.0, 5.0);

            Assert.True(overshoot > 0.01, $"overshoot {overshoot} m/s");
        }

        /// <summary>
        ///     MechJebModuleRCSController's loop at Tf 1, set up as PIDControllerBase.Configure sets it up (Kd is 0).
        /// </summary>
        private static PIDLoop2 RcsLoop(bool conditionalIntegration)
        {
            const double TF = 1.0;
            double kp = 0.53 / (3 * Sqrt(2) * TF * TF);

            // Kp first: the Ki setter scales by the current K
            return new PIDLoop2
            {
                Ts = 0.02, Kp = kp, Ki = kp / (12 * Sqrt(2) * TF), MinOutput = -1, MaxOutput = 1,
                ConditionalIntegration = conditionalIntegration
            };
        }

        /// <summary>
        ///     One rigid axis, with the error in ticks of full thrust, and ModuleRCS's 5 % floor and one tick of delay on
        ///     the output.
        /// </summary>
        /// <returns>the largest velocity reached past zero, and the velocity after 20 s</returns>
        private static (double overshoot, double after20) RcsVelocityStep(PIDLoop2 pid, double accel, double dv)
        {
            const int TICKS = 1000;

            double v = dv, pending = 0, overshoot = 0;

            for (int i = 0; i < TICKS; i++)
            {
                double applied = Abs(pending) < 0.05 ? 0 : pending;
                pending = pid.Update(v / (accel * pid.Ts), 0);

                v -= applied * accel * pid.Ts;
                overshoot = Max(overshoot, -v);
            }

            return (overshoot, v);
        }

        [Fact]
        public void IntegralLimits()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Ki = 1.0, Ts = 0.02, MinIntegral = -0.05, MaxIntegral = 0.05 };

            Assert.Equal(2.01, pid.Update(1, 0), 8);
            Assert.Equal(2.03, pid.Update(1, 0), 8);
            Assert.Equal(2.05, pid.Update(1, 0), 8);
            Assert.Equal(2.05, pid.Update(1, 0), 8);
            Assert.Equal(0.05, pid.ITerm, 12);

            for (int i = 0; i < 100; i++)
                pid.Update(-1, 0);

            Assert.Equal(-0.05, pid.ITerm, 12);
        }

        /// <summary>
        ///     With Td == 0 the derivative recursion has its pole at -1, so a DTerm left from an earlier Td must be
        ///     dropped rather than flipping sign forever.
        /// </summary>
        [Fact]
        public void DerivativeTurnedOffMidRun()
        {
            var pid = new PIDLoop2 { Kp = 1.0, Kd = 0.1, Ts = 0.02 };

            for (int i = 0; i < 5; i++)
                pid.Update(i, 0);

            Assert.NotEqual(0.0, pid.DTerm);

            pid.Kd = 0;

            for (int i = 0; i < 5; i++)
            {
                Assert.Equal(5.0, pid.Update(5, 0), 12);
                Assert.Equal(0.0, pid.DTerm);
            }
        }

        [Fact]
        public void IntegralOnly()
        {
            var pid = new PIDLoop2 { Kp = 0, Ki = 1.0, Ts = 0.02 };

            Assert.Equal(1.0, pid.Ki, 12);
            Assert.Equal(0.01, pid.Update(1, 0), 8);
            Assert.Equal(0.03, pid.Update(1, 0), 8);
            Assert.Equal(0.05, pid.Update(1, 0), 8);
        }

        [Fact]
        public void DerivativeOnly()
        {
            var pid = new PIDLoop2 { Kp = 0, Kd = 0.1, Ts = 0.02 };
            pid.Tf = 0.5 * pid.Ts;

            Assert.Equal(0.1, pid.Kd, 12);
            Assert.Equal(5.0, pid.Update(1, 0), 8);
            Assert.Equal(0.0, pid.Update(1, 0), 8);
        }

        /// <summary>
        ///     With Tf = Ts/2 the Tustin derivative's pole is at zero, and it is the plain difference Kd * (e - e1) / Ts.
        /// </summary>
        [Fact]
        public void DerivativeWithHalfStepFilterIsPlainDifference()
        {
            var pid = new PIDLoop2 { Kp = 2.0, Ki = 0.1, Kd = 0.001, Ts = 0.02 };
            pid.Tf = 0.5 * pid.Ts;

            double[] errors = { 1, 1, 3, 3, 2 };
            double e1 = 0;

            foreach (double e in errors)
            {
                pid.Update(e, 0);
                Assert.Equal(0.001 * (e - e1) / 0.02, pid.DTerm, 12);
                e1 = e;
            }
        }
    }
}
