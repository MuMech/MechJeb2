/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using MechJebLib.Control;
using MechJebLib.Primitives;
using Xunit;

namespace MechJebLibTest.ControlTests
{
    public class VesselShapeTests
    {
        private static readonly V3 _axis = V3.zaxis;

        // MJ Test Truss as flown: 46 m long, with the port on its side at one end and out along z
        private static VesselShape Truss()
        {
            var shape = new VesselShape();
            shape.Add(new V3(-0.7, -0.7, -1.4), new V3(43.8, 0.7, 0));
            return shape;
        }

        // Two columns with a cross-link behind the port, which is on the end of the first
        private static VesselShape Station()
        {
            var shape = new VesselShape();
            shape.Add(new V3(-1.3, -1.3, -30), new V3(1.3, 1.3, 0));
            shape.Add(new V3(11.2, -1.3, -30), new V3(13.8, 1.3, 0));
            shape.Add(new V3(1.3, -1, -17), new V3(11.2, 1, -15));
            return shape;
        }

        [Fact]
        public void ReachIsTheFurthestCornerAlongTheDirection()
        {
            Assert.Equal(43.8, Truss().Reach(V3.xaxis), 9);
            Assert.Equal(0.7, Truss().Reach(V3.xaxis * -1), 9);
            Assert.Equal(1.4, Truss().Reach(_axis * -1), 9);

            V3 diagonal = new V3(1, 1, 0).normalized;
            Assert.Equal((43.8 + 0.7) * diagonal.x, Truss().Reach(diagonal), 9);
        }

        [Fact]
        public void ReachIsNeverLessThanTheOrigin()
        {
            var shape = new VesselShape();
            shape.Add(new V3(-1, -1, -5), new V3(1, 1, -2));
            Assert.Equal(0, shape.Reach(_axis));
        }

        [Fact]
        public void ANothingShapeReachesNowhere()
        {
            var shape = new VesselShape();
            Assert.Equal(0, shape.Reach(V3.xaxis));
            Assert.Equal(0, shape.Radius());
            Assert.True(double.IsPositiveInfinity(shape.Distance(V3.zero)));
            Assert.False(shape.Blocks(V3.zero, V3.xaxis * 100, 5));
            Assert.False(shape.Exit(V3.zero, 5, out _));
        }

        [Fact]
        public void RadiusIsTheFurthestCornerFromTheOrigin()
        {
            var shape = new VesselShape();
            shape.Add(new V3(-3, -1, 0), new V3(1, 4, 12));
            Assert.Equal(new V3(3, 4, 12).magnitude, shape.Radius(), 9);
        }

        [Fact]
        public void DistanceIsToTheNearestBox()
        {
            VesselShape shape = Station();
            Assert.Equal(0, shape.Distance(new V3(0, 0, -5)));
            Assert.Equal(5, shape.Distance(new V3(0, 0, 5)), 9);
            Assert.Equal(4.7, shape.Distance(new V3(6, 0, -5)), 9); // between the columns, nearer the first
            Assert.Equal(1, shape.Distance(new V3(6, 0, -14)), 9);  // above the cross-link
            Assert.Equal(new V3(3, 4, 0).magnitude, shape.Distance(new V3(-4.3, 5.3, -5)), 9);
        }

        [Fact]
        public void PathDistanceFindsAPartBetweenItsEnds()
        {
            VesselShape shape = Station();
            V3 from = new V3(6, -10, -16);
            V3 to = new V3(6, 10, -16);
            Assert.Equal(0, shape.PathDistance(from, to));
            Assert.True(shape.Distance(from) > 8 && shape.Distance(to) > 8);
        }

        [Fact]
        public void APathThroughAPartIsBlocked()
        {
            VesselShape shape = Station();
            Assert.True(shape.Blocks(new V3(6, 0, -3), new V3(6, 0, -30), 8));
        }

        [Fact]
        public void APathAwayFromAPartIsNotBlocked()
        {
            VesselShape shape = Station();
            Assert.False(shape.Blocks(new V3(6, 0, -3), new V3(6, 30, -3), 8));
            Assert.False(shape.Blocks(new V3(0, 0, 8), new V3(0, 0, 30), 20));
        }

        [Fact]
        public void APathThatStaysAsCloseIsNotBlocked()
        {
            // a vessel beside a column, closer than it would like, moving along it
            Assert.False(Station().Blocks(new V3(-4, 0, -25), new V3(-4, 0, -5), 8));
        }

        [Fact]
        public void APathToAFarPointIsCheckedAllTheWay()
        {
            Assert.True(Truss().Blocks(new V3(20, 0, 20), new V3(20, 0, -20), 5));
        }

        [Fact]
        public void TheShortestSideOfATrussIsAwayFromIt()
        {
            VesselShape shape = Truss();
            V3 side = shape.ShortestSide(_axis, V3.xaxis); // a start offset towards the far end
            Assert.True(shape.Reach(side) < 1.5, $"reach {shape.Reach(side)}");
            Assert.True(V3.Dot(side, V3.xaxis) < 0.5);
        }

        [Fact]
        public void TheShortestSideIsAcrossTheAxis()
        {
            V3 axis = new V3(1, 2, 3).normalized;
            Assert.Equal(0, V3.Dot(Truss().ShortestSide(axis, V3.xaxis), axis), 9);
            Assert.Equal(1, Truss().ShortestSide(axis, V3.xaxis).magnitude, 9);
        }

        [Fact]
        public void AnEvenTieGoesToTheSideTheVesselIsOn()
        {
            var shape = new VesselShape();
            shape.Add(new V3(-2, -2, -5), new V3(2, 2, 0));
            V3 near = new V3(-1, 0, 0);
            Assert.True(V3.Dot(shape.ShortestSide(_axis, near), near) > 0.9);
        }

        [Fact]
        public void AVesselOnTheAxisStillGetsASide() => Assert.Equal(1, Truss().ShortestSide(_axis, V3.zero).magnitude, 9);

        [Fact]
        public void ThereIsNoExitFromClearSpace() => Assert.False(Station().Exit(new V3(0, 0, 30), 10, out _));

        [Fact]
        public void TheExitLeavesTheGapWithoutCrossingAPart()
        {
            // Case E: 3 m behind the port between the columns
            VesselShape shape = Station();
            V3 from = new V3(6, 0, -3);
            Assert.True(shape.Exit(from, 8, out V3 exit));
            Assert.True(shape.Distance(exit) >= 8);
            Assert.True(shape.PathDistance(from, exit) > 3, $"to {exit}, {shape.PathDistance(from, exit)} m clear");
        }

        [Fact]
        public void TheExitFromDeepInTheGapDoesNotGoThroughAColumn()
        {
            // the nearest face of the whole box from here is past the first column
            VesselShape shape = Station();
            V3 from = new V3(3, 0, -10);
            Assert.True(shape.Exit(from, 6, out V3 exit));
            Assert.True(shape.PathDistance(from, exit) > 1, $"to {exit}, {shape.PathDistance(from, exit)} m clear");
        }
    }
}
