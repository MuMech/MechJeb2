/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Control;
using Xunit;
using static System.Math;

namespace MechJebLibTest.ControlTests
{
    public class AntiWindupTests
    {
        private const double MIN = -1;
        private const double MAX = 1;

        // MechJebModuleRCSController's gains at Tf 1, and its tick
        private const double KP = 0.1249;
        private const double KI = 0.00736;
        private const double DT = 0.02;

        [Fact]
        public void IntegratesWhileTheOutputIsInsideTheLimits()
        {
            Assert.Equal(0.25, AntiWindup.Integrate(0.2, 0.3, 0.05, MIN, MAX), 12);
            Assert.Equal(-0.25, AntiWindup.Integrate(-0.2, -0.3, -0.05, MIN, MAX), 12);
        }

        [Fact]
        public void HoldsWhileTheOutputIsSaturated()
        {
            Assert.Equal(0.2, AntiWindup.Integrate(0.2, 20, 0.05, MIN, MAX), 12);
            Assert.Equal(-0.2, AntiWindup.Integrate(-0.2, -20, -0.05, MIN, MAX), 12);
        }

        [Fact]
        public void HoldsAtTheLastStepThatFitsInsideTheLimits()
        {
            // 0.3 + 0.75 passes the limit, even though the integral alone doesn't
            Assert.Equal(0.7, AntiWindup.Integrate(0.7, 0.3, 0.05, MIN, MAX), 12);
        }

        [Fact]
        public void ALastingErrorLeavesTheIntegralBounded()
        {
            double integral = 0;

            for (int i = 0; i < 100000; i++)
                integral = AntiWindup.Integrate(integral, 0.5, 0.01, MIN, MAX);

            Assert.InRange(integral, 0.49, 0.5);
        }

        [Theory]
        [InlineData(0.3, 1.0)] // audit item CTRL-10: 7.3 cm/s of overshoot, and still 3 cm/s off after 20 s
        [InlineData(0.1, 1.0)] // a long saturation, the integral would pass 1
        [InlineData(1.0, 2.0)]
        [InlineData(3.0, 5.0)] // the integral peaks near 0.5 here, so limiting it to the output range would never act
        public void AVelocityStepDoesNotOvershoot(double accel, double dv)
        {
            (double overshoot, double after20) = VelocityStep(accel, dv);

            Assert.True(overshoot < 0.005, $"overshoot {overshoot} m/s");
            Assert.True(Abs(after20) < 0.01, $"{after20} m/s left after 20 s");
        }

        [Fact]
        public void TheIntegralStillWorksThroughTheThrustFloor()
        {
            // proportional alone is 3 %, under the 5 % ModuleRCS needs to fire, so only the integral can remove this error
            (_, double after20) = VelocityStep(1.0, 0.005);

            Assert.True(Abs(after20) < 0.004, $"{after20} m/s left after 20 s");
        }

        /// <summary>
        ///     MechJebModuleRCSController's velocity loop on one rigid axis, with the error in ticks of full thrust, and
        ///     ModuleRCS's 5 % floor and one tick of delay on the output.
        /// </summary>
        /// <returns>the largest velocity reached past zero, and the velocity after 20 s</returns>
        private static (double overshoot, double after20) VelocityStep(double accel, double dv)
        {
            const int TICKS = 1000;

            double v = dv, integral = 0, pending = 0, overshoot = 0;

            for (int i = 0; i < TICKS; i++)
            {
                double error = v / (accel * DT);
                double proportional = KP * error;
                integral = AntiWindup.Integrate(integral, proportional, KI * error * DT, MIN, MAX);

                double applied = Abs(pending) < 0.05 ? 0 : pending;
                pending = Max(MIN, Min(MAX, proportional + integral));

                v -= applied * accel * DT;
                overshoot = Max(overshoot, -v);
            }

            return (overshoot, v);
        }
    }
}
