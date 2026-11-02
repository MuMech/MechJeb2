/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Control;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.ControlTests
{
    public class StuckDetectorTests
    {
        private const double TIME = 3;
        private const double MIN_RESPONSE = 0.25;
        private const double DT = 0.02;

        private static V3 Along(double x) => new V3(x, 0, 0);

        // ticks until it fires, or -1 if it never does, for a vessel at full thrust towards a velocity of command, which gets rate of the thrust's
        // acceleration from it
        private static int TicksToStuck(double velocity, double command, double accel, double rate, double seconds, bool saturated = true)
        {
            var detector = new StuckDetector(TIME, MIN_RESPONSE);
            for (int tick = 0; tick < seconds / DT; tick++)
            {
                if (detector.Update(Along(command - velocity), Along(velocity), accel, DT, saturated))
                    return tick;
                velocity += (command > velocity ? 1 : -1) * rate * accel * DT;
            }

            return -1;
        }

        [Fact]
        public void AFreeVesselIsNeverStuck() => Assert.Equal(-1, TicksToStuck(0, 8, 1.85, 1, 4));

        [Fact]
        public void AVesselThatCannotMoveIsStuckAfterTheTime()
        {
            int ticks = TicksToStuck(0, 1, 1.85, 0, 10);
            Assert.InRange(ticks * DT, TIME - 2 * DT, TIME + 2 * DT);
        }

        [Fact]
        public void TheFlownBlockedTugIsStuck()
        {
            // flight 1n: pressed against the port at a command of 1 m/s, on 1.85 m/s²
            Assert.True(TicksToStuck(0, 1, 1.85, 0, 4) >= 0);
        }

        [Fact]
        public void AHeavyVesselSlowToAnswerIsNotStuck()
        {
            // the 24 t tug, 0.17 m/s² across, 18 s at full thrust to change speed by 3 m/s
            Assert.Equal(-1, TicksToStuck(0, 3, 0.17, 1, 17));
        }

        [Fact]
        public void ACommandThatStepsWhilePushingIsNotStuck()
        {
            // flight f6, the 24 t tug: braking from 0.93 m/s when the command steps from 0 to -1 m/s, which makes the error jump, on 0.33 m/s²
            var detector = new StuckDetector(TIME, MIN_RESPONSE);
            double velocity = 0.93;
            for (int tick = 0; tick < 6 / DT; tick++)
            {
                double command = tick * DT < 0.2 ? 0 : -1;
                Assert.False(detector.Update(Along(command - velocity), Along(velocity), 0.33, DT, true));
                velocity -= 0.33 * DT;
            }
        }

        [Fact]
        public void AResponseUnderTheShareCounts() => Assert.True(TicksToStuck(0, 50, 1.85, 0.2, 4) >= 0);

        [Fact]
        public void AResponseOverTheShareDoesNotCount() => Assert.Equal(-1, TicksToStuck(0, 50, 1.85, 0.3, 20));

        [Fact]
        public void ALoopInsideItsLimitIsNeverStuck() => Assert.Equal(-1, TicksToStuck(0, 1, 1.85, 0, 20, false));

        [Fact]
        public void LeavingTheLimitStartsTheTimeAgain()
        {
            var detector = new StuckDetector(TIME, MIN_RESPONSE);
            for (int tick = 0; tick < 2.9 / DT; tick++)
                Assert.False(detector.Update(Along(1), V3.zero, 1.85, DT, true));

            Assert.False(detector.Update(Along(1), V3.zero, 1.85, DT, false));
            for (int tick = 0; tick < 2.9 / DT; tick++)
                Assert.False(detector.Update(Along(1), V3.zero, 1.85, DT, true));
        }

        [Fact]
        public void ANewDirectionStartsTheTimeAgain()
        {
            var detector = new StuckDetector(TIME, MIN_RESPONSE);
            for (int tick = 0; tick < 2.9 / DT; tick++)
                Assert.False(detector.Update(Along(1), V3.zero, 1.85, DT, true));

            for (int tick = 0; tick < 2.9 / DT; tick++)
                Assert.False(detector.Update(new V3(0, 1, 0), V3.zero, 1.85, DT, true));
        }

        [Fact]
        public void ASmallChangeOfDirectionIsTheSamePush()
        {
            // a turning target swings the push slowly, and flight 1n's was 29 degrees in 3 s
            var detector = new StuckDetector(TIME, MIN_RESPONSE);
            bool stuck = false;
            for (int tick = 0; tick < 3.1 / DT; tick++)
                stuck |= detector.Update(new V3(1, 0.5 * tick * DT / 3, 0), V3.zero, 1.85, DT, true);

            Assert.True(stuck);
        }

        [Fact]
        public void ARockingVesselStaysStuck()
        {
            // bouncing against the target moves the velocity a little either way, and that's no answer to the thrust
            var detector = new StuckDetector(TIME, MIN_RESPONSE);
            bool stuck = false;
            for (int tick = 0; tick < 3.1 / DT; tick++)
                stuck |= detector.Update(Along(1), Along(tick % 50 < 25 ? 0.03 : -0.03), 1.85, DT, true);

            Assert.True(stuck);
        }

        [Fact]
        public void ReboundingOffTheTargetThenStuckStillFires()
        {
            // the first contact throws it back, the thrust stops that, and then it's against the target: the time starts from there
            var detector = new StuckDetector(TIME, MIN_RESPONSE);
            bool stuck = false;
            double velocity = -0.5;
            for (int tick = 0; tick < 4 / DT && !stuck; tick++)
            {
                stuck    =  detector.Update(Along(1 - velocity), Along(velocity), 1.85, DT, true);
                velocity =  System.Math.Min(velocity + 1.85 * DT, 0);
            }

            Assert.True(stuck);
        }

        [Fact]
        public void ResetStartsTheTimeAgain()
        {
            var detector = new StuckDetector(TIME, MIN_RESPONSE);
            for (int tick = 0; tick < 2.9 / DT; tick++)
                Assert.False(detector.Update(Along(1), V3.zero, 1.85, DT, true));

            detector.Reset();
            Assert.False(detector.Update(Along(1), V3.zero, 1.85, DT, true));
        }
    }
}
