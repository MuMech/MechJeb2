/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Control;
using MechJebLib.Primitives;
using Xunit;
using static System.Math;

namespace MechJebLibTest.ControlTests
{
    public class CoMShiftTrackerTests
    {
        private const double DT        = 0.02;
        private const double THRESHOLD = 0.2;

        private static readonly V3 _centre = new V3(1, 2, 3);

        private static CoMShiftTracker Tracker(V3 com)
        {
            var tracker = new CoMShiftTracker();
            tracker.Reset(com);
            return tracker;
        }

        private static V3 Bent(double hz, double t) => _centre + V3.yaxis * Sin(2 * PI * hz * t);

        [Theory]
        [InlineData(0.25)]
        [InlineData(0.4)]
        [InlineData(0.5)]
        [InlineData(1)]
        [InlineData(2.3)]
        public void BendingDoesNotTrip(double hz)
        {
            CoMShiftTracker tracker = Tracker(Bent(hz, 0));

            for (int i = 1; i < 2000; i++)
                Assert.False(tracker.Update(Bent(hz, i * DT), THRESHOLD), $"tripped on tick {i}");
        }

        [Fact]
        public void StartingAtThePeakOfABendTripsOnce()
        {
            // the reference starts a metre off the centre, and has to find it
            const double HZ = 0.25;

            CoMShiftTracker tracker = Tracker(Bent(HZ, 1));

            int trips = 0;
            for (int i = 1; i < 2000; i++)
            {
                if (tracker.Update(Bent(HZ, 1 + i * DT), THRESHOLD))
                    trips++;
            }

            Assert.Equal(1, trips);
        }

        [Fact]
        public void ARealShiftTripsAndThenSettles()
        {
            CoMShiftTracker tracker = Tracker(_centre);
            V3 moved = _centre + V3.xaxis;

            for (int i = 0; i < CoMShiftTracker.WINDOW; i++)
                tracker.Update(_centre, THRESHOLD);

            // the average walks over in steps, so a jump this size trips more than once on the way
            int trips = 0;
            for (int i = 0; i < 3 * CoMShiftTracker.WINDOW; i++)
            {
                if (tracker.Update(moved, THRESHOLD))
                {
                    Assert.True(i < CoMShiftTracker.WINDOW);
                    trips++;
                }
            }

            Assert.True(trips > 0);
            Assert.True(tracker.Error <= THRESHOLD);
        }

        [Fact]
        public void ASlowDriftTripsEachTimeItPassesTheThreshold()
        {
            // 1 cm/s for 100 s, against a threshold of 20 cm
            CoMShiftTracker tracker = Tracker(_centre);

            int trips = 0;
            for (int i = 0; i < 5000; i++)
            {
                if (tracker.Update(_centre + V3.xaxis * (0.01 * i * DT), THRESHOLD))
                    trips++;
            }

            Assert.Equal(4, trips);
        }

        [Fact]
        public void WaitsForAFullWindow()
        {
            CoMShiftTracker tracker = Tracker(_centre);
            V3 moved = _centre + V3.xaxis;

            for (int i = 0; i < CoMShiftTracker.WINDOW - 1; i++)
                Assert.False(tracker.Update(moved, THRESHOLD));

            Assert.True(tracker.Update(moved, THRESHOLD));
        }

        [Fact]
        public void TheErrorIsTheDistanceOfTheAverage()
        {
            CoMShiftTracker tracker = Tracker(_centre);

            for (int i = 0; i < CoMShiftTracker.WINDOW; i++)
                tracker.Update(_centre + V3.xaxis * 0.1, THRESHOLD);

            Assert.Equal(0.1, tracker.Error, 9);
        }

        [Fact]
        public void ResetForgetsTheWindow()
        {
            CoMShiftTracker tracker = Tracker(_centre);

            for (int i = 0; i < CoMShiftTracker.WINDOW; i++)
                tracker.Update(_centre, THRESHOLD);

            tracker.Reset(_centre + V3.xaxis);

            Assert.Equal(0, tracker.Error);
            for (int i = 0; i < CoMShiftTracker.WINDOW; i++)
                Assert.False(tracker.Update(_centre + V3.xaxis, THRESHOLD));
        }
    }
}
