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


        // A coast-before that a Solution skips (shorter than SKIP_TIME): the booster is cut and jettisoned, and the coast's stage
        // lights.  PSGSimulation never marks the coast done (MarkCoastDone runs only on COAST_ENDED) and FindStartingIndex
        // starts the next solve at it, so the next Solution starts with the coast, which the optimizer (MinCoast 0) may now
        // make longer than SKIP_TIME.  CurrentPhase(false) is then -1 and CoastDue fires: the burning upper stage is cut for a
        // coast before itself and relit (a no-relight engine, F23; the throttle cut, F56).
        [Fact]
        public void SkippedCoastBeforeComingBackDoesNotCutTheBurningStage()
        {
            AscentFlight flight = ThreeStages();
            flight.Engage(Solve(flight.Plan(BEFORE, 1), MET0, Cut(50), Coast(0), Fixed(40), Fixed(30)));

            Assert.True(flight.RunUntil(f => f.Met >= MET0 + 55, 100));
            Assert.Equal(PSGState.BURNING, flight.Machine.State);
            Assert.Equal(1, flight.Stage);
            Assert.Equal(1, flight.EngineOf(2).Burns);

            // flight plan stages: 0 BURN row 0, 1 BURN row 1, 2 COAST_BEFORE row 1, 3 BURN row 2 (gone)
            double now = flight.Met;
            Assert.True(flight.Machine.SetSolution(Solve(flight.FlownPlan, now, 2, Coast(1), Fixed(35), Fixed(30))));
            flight.Fly();

            Assert.True(flight.Count(AscentEvents.COAST_STARTED) == 0 && flight.EngineOf(2).Burns == 1,
                $"{flight.Sequence()}; upper stage lit {flight.EngineOf(2).Burns} times");
        }
    }
}
