/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using static System.Math;

namespace MechJebLib.PSG
{
    /// <summary>
    ///     What of a vessel on the pad stays there at liftoff (launch clamps, and the stands, towers and arms that go with them),
    ///     and the frontal area of the rest, for the drag estimate of the ascent.
    /// </summary>
    public static class PadStructure
    {
        public readonly struct Member
        {
            public readonly bool Clamp;
            public readonly bool Engine;

            public readonly int InverseStage;

            /// <summary>The first stage still to fire that drops the part (so the highest number), or -1.</summary>
            public readonly int DroppedInStage;

            public Member(bool clamp, bool engine, int inverseStage, int droppedInStage)
            {
                Clamp          = clamp;
                Engine         = engine;
                InverseStage   = inverseStage;
                DroppedInStage = droppedInStage;
            }
        }

        /// <summary>A part seen along the vessel's nose: its centre across the axis, and its area.</summary>
        public readonly struct Section
        {
            public readonly double X;
            public readonly double Z;
            public readonly double Area;

            public Section(double x, double z, double area)
            {
                X    = x;
                Z    = z;
                Area = area;
            }
        }

        /// <summary>
        ///     Marks the clamps, and what has been dropped by the time the last of them releases, as left on the pad. If that
        ///     would leave no engine to lift off with (the vessel's root is on the pad's side, or a clamp is staged after the
        ///     rocket's own stages), only the clamps are marked.
        /// </summary>
        /// <returns>Whether more than the clamps stays on the pad.</returns>
        public static bool Find(IReadOnlyList<Member> members, bool[] left)
        {
            int release = int.MaxValue;
            for (int i = 0; i < members.Count; i++)
                if (members[i].Clamp)
                    release = Min(release, members[i].InverseStage);

            bool structure = false;
            bool engineLiftsOff = false;

            for (int i = 0; i < members.Count; i++)
            {
                Member m = members[i];
                left[i] = m.Clamp || m.DroppedInStage >= release;
                structure |= left[i] && !m.Clamp;
                engineLiftsOff |= !left[i] && m.Engine;
            }

            if (engineLiftsOff)
                return structure;

            for (int i = 0; i < members.Count; i++)
                left[i] = members[i].Clamp;

            return false;
        }

        /// <summary>
        ///     The widest part, plus every part centred outside the discs of the ones already counted, widest first: a stack
        ///     counts once, boosters beside it add theirs, and engines or fairing halves behind a wider tank don't.
        /// </summary>
        public static double FrontalArea(IReadOnlyList<Section> sections)
        {
            var widestFirst = new List<Section>(sections);
            widestFirst.Sort((a, b) => b.Area.CompareTo(a.Area));

            var counted = new List<Section>();
            double area = 0;

            foreach (Section s in widestFirst)
            {
                bool behind = false;
                foreach (Section c in counted)
                {
                    double dx = s.X - c.X;
                    double dz = s.Z - c.Z;
                    behind |= PI * (dx * dx + dz * dz) <= c.Area;
                }

                if (behind)
                    continue;

                counted.Add(s);
                area += s.Area;
            }

            return area;
        }
    }
}
