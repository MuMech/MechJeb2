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


        // A booster the optimizer cuts with propellant left, whose remaining burn a Solution shrinks under SKIP_TIME (0.128 s in these tests) while the
        // vessel is in it: guidance skips it and flies the next burn's steering and throttle on the booster, which nothing cuts
        // or stages (autostaging won't drop an engine with propellant): it burns out.
        [Fact]
        public void SkippedCutBoosterIsNotBurnedOut()
        {
            AscentFlight flight = ThreeStages();
            flight.Engage(Solve(flight.Plan(), MET0, Cut(0.1), Fixed(40), Fixed(30)));

            flight.Run(10);

            Assert.True(flight.EngineOf(3).Fuel > 59,
                $"{flight.Sequence()}; booster fuel left {flight.EngineOf(3).Fuel:F2} s of 60 after 10 s, stage {flight.Stage}");
        }
    }
}
