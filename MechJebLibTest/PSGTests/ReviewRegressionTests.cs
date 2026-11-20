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


        // A solve started before the cut (more than PreStageTime before it) lands while JETTISON waits for KSP to stage.  Its
        // booster ends 3 s later than the one being cut (the vessel was behind when it started).  Nothing cancels a solve at a
        // jettison (only at the coast, terminal guidance and the end), so it is flown: the cut booster is throttled up again.
        [Fact]
        public void StaleSolutionInJettisonDoesNotRelightTheCutBooster()
        {
            AscentFlight flight = ThreeStages();
            flight.RefuseStagingUntil = MET0 + 51;
            flight.Engage(Solve(flight.Plan(), MET0, Cut(50), Fixed(40), Fixed(30)));

            Assert.True(flight.RunUntil(PSGState.JETTISON, 100));
            flight.Run(0.4);
            Assert.Equal(PSGState.JETTISON, flight.Machine.State);

            Assert.True(flight.Machine.SetSolution(Solve(flight.FlownPlan, MET0 + 39, Cut(14), Fixed(40), Fixed(30))));
            flight.Fly();

            Assert.True(flight.EngineOf(3).Burns == 1,
                $"the cut booster lit {flight.EngineOf(3).Burns} times: {flight.Sequence()}, {flight.StagingLog()}");
        }
    }
}
