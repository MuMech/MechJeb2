/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;

namespace MechJebLib.Control
{
    /// <summary>
    ///     A vessel as one box for each part, around an origin that's its docking point.  One box around a whole vessel says
    ///     how big it is, and a route needs to know how far it reaches from that point in each direction: a port at one end of
    ///     a long target has all of it on one side.
    /// </summary>
    public class VesselShape
    {
        // how often a path is checked, in metres: the distance can't change by more than this between two checks
        private const double STEP = 1;

        // a path can run this much closer to the shape than it starts, since the distance is measured to boxes and not to parts
        private const double SLACK = 0.5;

        // how much clearance a metre of a longer way out is worth
        private const double LENGTH_COST = 0.05;

        private const int SIDES = 16;

        private readonly List<V3> _min = new List<V3>();
        private readonly List<V3> _max = new List<V3>();

        public int Count => _min.Count;

        public V3 Min(int index) => _min[index];

        public V3 Max(int index) => _max[index];

        public void Clear()
        {
            _min.Clear();
            _max.Clear();
        }

        public void Add(V3 min, V3 max)
        {
            _min.Add(min);
            _max.Add(max);
        }

        /// <summary>How far the shape reaches along a unit direction, and never less than the origin.</summary>
        public double Reach(V3 direction)
        {
            double reach = 0;
            for (int i = 0; i < _min.Count; i++)
                reach = Math.Max(reach, Corner(_min[i], _max[i], direction));

            return reach;
        }

        // the box's furthest corner along a direction
        private static double Corner(V3 min, V3 max, V3 direction) =>
            V3.Dot(new V3(direction.x > 0 ? max.x : min.x, direction.y > 0 ? max.y : min.y, direction.z > 0 ? max.z : min.z), direction);

        /// <summary>How far the furthest corner of the shape is from the origin.</summary>
        public double Radius()
        {
            double squared = 0;
            for (int i = 0; i < _min.Count; i++)
                squared = Math.Max(squared, V3.Max(V3.Abs(_min[i]), V3.Abs(_max[i])).sqrMagnitude);

            return Math.Sqrt(squared);
        }

        /// <summary>The distance from a point to the nearest box: 0 inside one, and infinite with no boxes.</summary>
        public double Distance(V3 point) => Math.Sqrt(SquaredDistance(point));

        // the square of Distance, for loops that only compare distances
        private double SquaredDistance(V3 point)
        {
            double squared = double.PositiveInfinity;
            for (int i = 0; i < _min.Count; i++)
            {
                V3 outside = V3.Max(V3.Max(_min[i] - point, point - _max[i]), V3.zero);
                squared = Math.Min(squared, V3.Dot(outside, outside));
            }

            return squared;
        }

        /// <summary>The closest a straight path comes to the shape.</summary>
        public double PathDistance(V3 from, V3 to)
        {
            double length = (to - from).magnitude;
            double closestSquared = SquaredDistance(from);
            for (double s = STEP; s < length; s += STEP)
                closestSquared = Math.Min(closestSquared, SquaredDistance(from + (to - from) * (s / length)));

            return Math.Sqrt(Math.Min(closestSquared, SquaredDistance(to)));
        }

        /// <summary>
        ///     Whether a straight path takes a vessel into the shape: closer than it is now, and closer than it needs to be. A
        ///     vessel that starts close may stay there or leave, which is what makes this differ from comparing distances.
        /// </summary>
        /// <param name="from">where the path starts</param>
        /// <param name="to">where the path ends</param>
        /// <param name="need">the clearance the vessel wants</param>
        public bool Blocks(V3 from, V3 to, double need) => PathDistance(from, to) < Math.Min(Distance(from), need) - SLACK;

        /// <summary>The side to pass on: the direction across a unit axis that the shape reaches least far in.</summary>
        /// <param name="axis">the docking axis</param>
        /// <param name="near">which side the vessel is on, to settle a tie</param>
        public V3 ShortestSide(V3 axis, V3 near)
        {
            V3 x = V3.Cross(axis, Math.Abs(axis.x) < 0.9 ? V3.xaxis : V3.yaxis).normalized;
            V3 y = V3.Cross(axis, x);
            V3 towards = near.safeNormalized;
            V3 best = x;
            double bestReach = double.PositiveInfinity;
            for (int i = 0; i < SIDES; i++)
            {
                double angle = 2 * Math.PI * i / SIDES;
                V3 side = x * Math.Cos(angle) + y * Math.Sin(angle);
                double reach = Reach(side) - STEP * V3.Dot(side, towards);
                if (reach < bestReach)
                {
                    best = side;
                    bestReach = reach;
                }
            }

            return best;
        }

        /// <summary>
        ///     The way out for a vessel that's in among the shape's boxes: the straight line with the most room along it to a
        ///     point where it has the clearance it needs. The nearest face of a box around the whole shape can lead across a part.
        /// </summary>
        /// <param name="from">where the vessel is</param>
        /// <param name="need">the clearance it wants</param>
        /// <param name="exit">the point to go to</param>
        /// <returns>false when it's already clear or has no way out</returns>
        public bool Exit(V3 from, double need, out V3 exit)
        {
            exit = from;
            if (Distance(from) >= need)
                return false;

            double needSquared = need * need;
            double farthest = need + from.magnitude + 2 * Radius();
            double bestScore = double.NegativeInfinity;
            bool found = false;
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    for (int z = -1; z <= 1; z++)
                    {
                        if (x == 0 && y == 0 && z == 0)
                            continue;

                        V3 direction = new V3(x, y, z).normalized;
                        double closestSquared = double.PositiveInfinity;
                        for (double s = STEP; s <= farthest; s += STEP)
                        {
                            V3 point = from + direction * s;
                            double squared = SquaredDistance(point);
                            closestSquared = Math.Min(closestSquared, squared);
                            if (squared < needSquared)
                                continue;

                            double score = Math.Sqrt(closestSquared) - LENGTH_COST * s;
                            if (score > bestScore)
                            {
                                bestScore = score;
                                exit = point;
                                found = true;
                            }

                            break;
                        }
                    }

            return found;
        }
    }
}
