/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using System.Diagnostics;
using MechJebLib.Functions;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.PSG.Terminal;
using Xunit;
using static MechJebLib.Utils.Statics;
using static System.Math;

namespace MechJebLibTest.PSGTests
{
    // The guess by itself: it should reach the circular orbit at the target's GuessRadius() and inclination, without
    // going underground.  The phases are built the way AscentBuilder.Build() builds them.
    [ParallelTestMethods]
    public class AscentGuesserTests
    {
        private const double EARTH_MU = 3.986004418e+14;
        private const double EARTH_RADIUS = 6.371e+6;

        private static void CheckGuess(Problem problem, PhaseCollection phases)
        {
            var stopwatch = Stopwatch.StartNew();
            var guesser = new AscentGuesser(problem, null);
            using Solution solution = guesser.InitialGuess(phases);
            stopwatch.Stop();

            double rT = problem.Terminal.GuessRadius();
            double incT = problem.Terminal.IncT();
            (V3 rf, V3 vf) = solution.TerminalStateVectors();
            double incf = Acos(V3.Cross(rf, vf).normalized.z);

            Print($"guess took {stopwatch.ElapsedMilliseconds} ms, tf: {(solution.Tf - solution.T0) * problem.Scale.TimeScale} s");
            Print($"rf/rT - 1: {rf.magnitude / rT - 1} fpa: {Astro.FlightPathAngle(rf, vf)} rT*vf^2 - 1: {rT * vf.sqrMagnitude - 1} inc: {Rad2Deg(incf)}");

            solution.Segments.ShouldEqual(phases.Count);

            // never underground
            for (int i = 0; i <= 1000; i++)
            {
                double t = solution.T0 + (solution.Tf - solution.T0) * i / 1000;
                solution.R(t).magnitude.ShouldBeGreaterThan(problem.RBody - 100 / problem.Scale.LengthScale);
            }

            (rf.magnitude / rT - 1).ShouldBeZero(1e-6);
            Astro.FlightPathAngle(rf, vf).ShouldBeZero(1e-6);
            (rT * vf.sqrMagnitude - 1).ShouldBeZero(1e-6);
            incf.ShouldEqual(Abs(incT), 1e-5);
        }

        private static PhaseCollection Phases(Problem problem, params Phase[] phases)
        {
            var normalized = new PhaseCollection();
            foreach (Phase phase in phases)
                normalized.Add(phase.Rescale(problem.Scale));
            normalized.FixLastShutdownStage();
            return normalized;
        }

        // the Titan2Tests rocket in vacuum, which has no planetary rotation, so it starts with 408 m/s of horizontal airspeed
        [Fact]
        public void Titan2Circular()
        {
            var r0 = new V3(5593203.65707947, 0, 3050526.81522927);
            var v0 = new V3(0, 407.862893197274, 0);
            double incT = Deg2Rad(28.608);
            double rT = EARTH_RADIUS + 185e+3;

            var problem = new Problem(r0, v0, r0.normalized, 157355.487476332, 0, EARTH_MU, EARTH_RADIUS, 0, 0, 0, 0, V3.zero,
                new Kepler3(rT, 0, incT));

            CheckGuess(problem, Phases(problem,
                Phase.NewStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4),
                Phase.NewStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)));
        }

        // launches from 0.5 degrees north to an equatorial orbit, so it has to dogleg
        [Fact]
        public void Titan2CircularEquatorial()
        {
            var r0 = new V3(6370757.411841836, 0, 55596.757660140334);
            var v0 = new V3(0, 464.5630141793475, 0);
            double rT = EARTH_RADIUS + 185e+3;

            var problem = new Problem(r0, v0, r0.normalized, 157355.487476332, 0, EARTH_MU, EARTH_RADIUS, 0, 0, 0, 0, V3.zero,
                new Kepler3(rT, 0, 0));

            CheckGuess(problem, Phases(problem,
                Phase.NewStage(157355.487476332, 40267.56108456338, 2340000, 301.817977905273, 4, 4),
                Phase.NewStage(32758.6353093992, 6384.144733613521, 456100.006103516, 315.000112652779, 3, 3)));
        }

        // Carnasa's TheStandard in an atmosphere, with a coast and an elliptical target: the guess is circular at periapsis
        [Fact]
        public void TheStandardAtmosphere()
        {
            var r0 = new V3(-521765.111703417, -5568874.59934707, 3050608.87783524);
            var v0 = new V3(406.088016257895, -38.0495807832894, 0.000701038889818476);
            var u0 = new V3(-0.0820737379089317, -0.874094973679233, 0.478771328926086);
            double incT = Deg2Rad(28.608);
            (double smaT, double eccT) = Astro.SmaEccFromApsides(EARTH_RADIUS + 185e+3, EARTH_RADIUS + 10e+6);

            double thrust1 = Astro.ThrustFromMassesIspBurntime(49119.7842689869, 7114.2513992454, 288.000034332275, 170.308460385726);
            double thrust2 = Astro.ThrustFromMassesIspBurntime(2848.62586760223, 1363.71123994759, 270.15767003304, 116.391834883409);
            double thrust3 = Astro.ThrustFromMassesIspBurntime(678.290157913434, 177.582604389742, 230.039271734103, 53.0805126571005);

            // 0.5 * 10 m^2 * 1.225 kg/m^3, 7200 m scale height
            var problem = new Problem(r0, v0, u0, 49119.7842689869, 0, EARTH_MU, EARTH_RADIUS, 7200, 0.5 * 10 * 1.225, 0, 0,
                7.29211585e-5 * V3.northpole, new Kepler3(smaT, eccT, incT));

            CheckGuess(problem, Phases(problem,
                Phase.NewStage(49119.7842689869, 7114.2513992454, thrust1, 288.000034332275, 3, 3),
                Phase.NewStage(2848.62586760223, 1363.71123994759, thrust2, 270.15767003304, 1, 1),
                Phase.NewCoast(678.290157913434, 678.290157913434, 0, 450, 1, 1),
                Phase.NewStage(678.290157913434, 177.582604389742, thrust3, 230.039271734103, 0, 0, allowShutdown: false)));
        }

        // the Mainsail Tin Can from the KerbinTests, without its coast, in Kerbin's atmosphere.  It can't make the 100 km orbit of
        // the KerbinTests without the coast, but it has a few seconds of burn to spare going to 75 km
        [Fact]
        public void MainSailTinCanAtmosphere()
        {
            const double MU = 3531600000000;
            var r0 = new V3(365592.912061998, -475862.737003702, -1017.92554628857);
            var v0 = new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06);
            var u0 = new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861);

            // 0.5 * 19.6 m^2 * 1.22497705725583 kg/m^3, 6797.8 m scale height
            var problem = new Problem(r0, v0, u0, 79699.9986022711, 0, MU, 600000, 6797.80562531277, 0.5 * 19.6 * 1.22497705725583, 0, 0,
                new V3(0, 0, 0.000291570900559802), new Kepler3(675000, 0, 0));

            CheckGuess(problem, Phases(problem,
                Phase.NewStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, ispCurrent: 285.387620615011)));
        }
    }
}
