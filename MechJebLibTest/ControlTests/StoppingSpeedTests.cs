/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Control;
using Xunit;
using static System.Math;

namespace MechJebLibTest.ControlTests
{
    public class StoppingSpeedTests
    {
        private const double ACCEL = 0.025;
        private const double GAIN  = 0.3;

        [Fact]
        public void LinearNearZero()
        {
            Assert.Equal(0, StoppingSpeed.GainLimited(0, ACCEL, GAIN), 12);
            Assert.Equal(0.003, StoppingSpeed.GainLimited(0.01, ACCEL, GAIN), 12);
            Assert.Equal(0.03, StoppingSpeed.GainLimited(0.1, ACCEL, GAIN), 12);
        }

        [Fact]
        public void GainStaysLimitedWhereTheSquareRootIsNot()
        {
            // the bare square root commands more than 7 times this for a centimetre of noise
            double limited = StoppingSpeed.GainLimited(0.01, ACCEL, GAIN);

            Assert.True(Sqrt(2 * ACCEL * 0.01) > 7 * limited);
        }

        [Fact]
        public void BecomesTheStoppingSpeedFarAway()
        {
            const double D = 100;

            double linearRange = ACCEL / (GAIN * GAIN);
            double speed = StoppingSpeed.GainLimited(D, ACCEL, GAIN);

            Assert.Equal(Sqrt(2 * ACCEL * (D - linearRange / 2)), speed, 12);
            Assert.True(speed < Sqrt(2 * ACCEL * D));
            Assert.True(Abs(speed - Sqrt(2 * ACCEL * D)) < 0.01);
        }

        [Fact]
        public void TheTwoLawsJoinWithTheSameValueAndSlope()
        {
            const double H = 1e-7;

            double join = ACCEL / (GAIN * GAIN);

            double below = StoppingSpeed.GainLimited(join - H, ACCEL, GAIN);
            double at = StoppingSpeed.GainLimited(join, ACCEL, GAIN);
            double above = StoppingSpeed.GainLimited(join + H, ACCEL, GAIN);

            Assert.Equal(at, below, 5);
            Assert.Equal(at, above, 5);
            Assert.Equal(GAIN, (at - below) / H, 4);
            Assert.Equal(GAIN, (above - at) / H, 4);
        }

        [Fact]
        public void OnlyTheSizeOfTheDistanceCounts()
        {
            Assert.Equal(StoppingSpeed.GainLimited(0.1, ACCEL, GAIN), StoppingSpeed.GainLimited(-0.1, ACCEL, GAIN), 12);
            Assert.Equal(StoppingSpeed.GainLimited(5, ACCEL, GAIN), StoppingSpeed.GainLimited(-5, ACCEL, GAIN), 12);
        }

        [Fact]
        public void NoAuthorityMeansNoSpeed()
        {
            Assert.Equal(0, StoppingSpeed.GainLimited(0, 0, GAIN), 12);
            Assert.Equal(0, StoppingSpeed.GainLimited(5, 0, GAIN), 12);
        }

        [Fact]
        public void NeverDecreasesWithDistance()
        {
            double last = 0;

            for (double d = 0; d < 10; d += 0.001)
            {
                double speed = StoppingSpeed.GainLimited(d, ACCEL, GAIN);
                Assert.True(speed >= last);
                last = speed;
            }
        }
    }
}
