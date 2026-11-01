/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Collections.Generic;
using MechJebLib.Primitives;
using MechJebLib.PSG.Terminal;
using Xunit;
using static System.Math;

namespace MechJebLibTest.PSGTests
{
    // The terminals without a LAN target constrain inclination by h_z/|h| - cos(incT), which has a vanishing gradient for
    // equatorial targets, so those are constrained to the equatorial plane with r.z = 0 and v.z = 0 instead.  These run in
    // normalized units (mu = 1) against a circular orbit with r = v = 1.
    public class TerminalTests
    {
        public enum Kind { KEPLER3, FLIGHT_PATH_ANGLE3_ENERGY, FLIGHT_PATH_ANGLE4 }

        private const double DELTA = 1e-6;

        private static ITerminal Create(Kind kind, double incT) =>
            kind switch
            {
                Kind.KEPLER3                   => new Kepler3(1, 0, incT),
                Kind.FLIGHT_PATH_ANGLE3_ENERGY => new FlightPathAngle3Energy(0, 1, incT),
                Kind.FLIGHT_PATH_ANGLE4        => new FlightPathAngle4(0, 1, 1, incT),
                _                              => throw new ArgumentOutOfRangeException(nameof(kind))
            };

        // the number of rows ahead of the inclination rows
        private static int OtherConstraints(Kind kind) => kind == Kind.FLIGHT_PATH_ANGLE4 ? 3 : 2;

        // the state at the ascending node of the circular orbit with inclination inc
        private static (V3 r, V3 v) StateAtInclination(double inc) => (new V3(1, 0, 0), new V3(0, Cos(inc), Sin(inc)));

        private static double[] Evaluate(ITerminal terminal, V3 r, V3 v, out alglib.sparsematrix j)
        {
            double[] x = { r.x, r.y, r.z, v.x, v.y, v.z };
            double[] f = new double[terminal.NumConstraints];
            alglib.sparsecreatecrsempty(x.Length, out j);

            int ci = 0;
            terminal.Constraints(x, (0, 1, 2), (3, 4, 5), f, j, ref ci);

            ci.ShouldEqual(terminal.NumConstraints);

            return f;
        }

        public static IEnumerable<object[]> Targets()
        {
            foreach (Kind kind in Enum.GetValues(typeof(Kind)))
            {
                yield return new object[] { kind, 0.0, true };
                yield return new object[] { kind, PI, true };
                yield return new object[] { kind, 5e-4, true };
                yield return new object[] { kind, -5e-4, true };
                yield return new object[] { kind, 0.5, false };
                yield return new object[] { kind, PI - 0.5, false };
            }
        }

        public static IEnumerable<object[]> EquatorialTargets()
        {
            foreach (Kind kind in Enum.GetValues(typeof(Kind)))
            {
                yield return new object[] { kind, 0.0 };
                yield return new object[] { kind, PI };
            }
        }

        [Theory]
        [MemberData(nameof(Targets))]
        public void EquatorialTargetsUseTwoPlanarRows(Kind kind, double incT, bool equatorial)
        {
            Create(kind, incT).NumConstraints.ShouldEqual(OtherConstraints(kind) + (equatorial ? 2 : 1));
        }

        // nearly equatorial targets are snapped to exactly equatorial, so the satisfying state is the equatorial one
        [Theory]
        [MemberData(nameof(Targets))]
        public void ConstraintsVanishOnTheTargetOrbit(Kind kind, double incT, bool equatorial)
        {
            (V3 r, V3 v) = StateAtInclination(equatorial ? Round(Abs(incT) / PI) * PI : Abs(incT));

            double[] f = Evaluate(Create(kind, incT), r, v, out _);

            foreach (double fi in f)
                fi.ShouldBeZero(1e-15);
        }

        // the PSG-9 regression: tilting the orbit by DELTA must show up to first order in the inclination rows (the
        // h_z/|h| - cos(incT) row only moves by DELTA^2 / 2), and the rows are wired to r.z and v.z.
        [Theory]
        [MemberData(nameof(EquatorialTargets))]
        public void EquatorialRowsAreFirstOrderInTilt(Kind kind, double incT)
        {
            int rows = OtherConstraints(kind);

            (V3 r, V3 v) = StateAtInclination(incT + DELTA);

            double[] f = Evaluate(Create(kind, incT), r, v, out alglib.sparsematrix j);

            for (int i = 0; i < rows; i++)
                f[i].ShouldBeZero(1e-15);

            f[rows].ShouldBeZero(1e-15);
            Abs(f[rows + 1]).ShouldEqual(DELTA, 1e-6);

            alglib.sparseget(j, rows, 2).ShouldEqual(1.0, 0);
            alglib.sparseget(j, rows + 1, 5).ShouldEqual(1.0, 0);
        }
    }
}
