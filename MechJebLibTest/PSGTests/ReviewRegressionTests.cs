/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Linq;
using MechJebLib.PSG;
using Xunit;
using static MechJebLibTest.PSGTests.AscentFlight;

namespace MechJebLibTest.PSGTests
{
    /// <summary>
    ///     Review of #2776 / #2777: known past failure states of PSG guidance, flown through the AscentStateMachine.  A test that
    ///     failed on dev ef540816, before the fix.  "Behind the plan" is modelled as an engine that lights late (Engine.LightAt): the
    ///     vessel only moves along the synthetic trajectory while it thrusts, so it reaches each planned angular momentum late.
    /// </summary>
    public class ReviewRegressionTests
    {
        private const PlanEditor.CoastKind BEFORE = PlanEditor.CoastKind.BEFORE;

        private static AscentFlight ThreeStages(double booster = 60, double middle = 40, double last = 30)
        {
            var flight = new AscentFlight(2);
            flight.AddEngine(1, 0, -1, last);
            flight.AddEngine(2, 1, 0, middle);
            flight.AddEngine(3, 2, 1, booster);
            return flight;
        }


        // a booster cut before a fixed upper stack, 5 s behind its plan, with no new Solution (the re-solves are paused for the
        // last PreStageTime = 10 s of a phase anyway)
        [Fact]
        public void F6_JettisonCutWaitsForThePlannedAngularMomentum()
        {
            AscentFlight flight = ThreeStages();
            flight.EngineOf(3).LightAt = MET0 + 5;
            flight.Engage(Solve(flight.Plan(), MET0, Cut(50), Fixed(40), Fixed(30)));

            Assert.True(flight.RunUntil(PSGState.JETTISON, 100));

            // the vessel is where the plan is at its cut, or the booster leaves with 5 s of propellant more than planned
            Assert.True(flight.TrajectoryT >= MET0 + 50 - DT,
                $"cut at MET {flight.Met - MET0:F2} with the vessel at plan MET {flight.TrajectoryT - MET0:F2}, booster fuel left {flight.EngineOf(3).Fuel:F2} s");
        }


        // the same booster cut by the coast before the last stages
        [Fact]
        public void F6_CoastCutWaitsForThePlannedAngularMomentum()
        {
            AscentFlight flight = ThreeStages();
            flight.EngineOf(3).LightAt = MET0 + 5;
            flight.Engage(Solve(flight.Plan(BEFORE, 1), MET0, Cut(50), Coast(30), Fixed(40), Fixed(30)));

            Assert.True(flight.RunUntil(PSGState.COASTING, 100));

            Assert.True(flight.TrajectoryT >= MET0 + 50 - DT,
                $"coast at MET {flight.Met - MET0:F2} with the vessel at plan MET {flight.TrajectoryT - MET0:F2}, booster fuel left {flight.EngineOf(3).Fuel:F2} s");
        }


        // A fixed booster behind its plan burns past the planned end of the short cut stage above it.  The re-solves are paused
        // all that time (CurrentPhaseTgo is 0, under PreStageTime), and the cut stage is jettisoned on the clock as soon as the
        // stage stats show the booster gone: lateness comes straight off the cut stage, and here all of it.
        [Theory]
        [InlineData(3)]  // the middle stage burns 7 s of its 10
        [InlineData(12)] // the middle stage past its planned end before it lights: staged off unlit, full
        public void F6_LateBoosterDoesNotEatTheCutStageAboveIt(double late)
        {
            AscentFlight flight = ThreeStages(60, 40, 30);
            flight.EngineOf(3).LightAt = MET0 + late;
            flight.Engage(Solve(flight.Plan(), MET0, Fixed(60), Cut(10), Fixed(30)));

            Assert.True(flight.RunUntil(PSGState.JETTISON, 200), flight.Sequence());
            double planAtCut = flight.TrajectoryT - MET0;
            double middleFuel = flight.EngineOf(2).Fuel;
            int middleBurns = flight.EngineOf(2).Burns;

            Assert.True(middleBurns == 1 && planAtCut >= 70 - DT,
                $"middle stage cut at MET {flight.Met - MET0:F2}, vessel at plan MET {planAtCut:F2} (planned cut 70), " +
                $"middle burns {middleBurns}, fuel left {middleFuel:F2} s of 40 (planned 30)");
        }
    }
}
