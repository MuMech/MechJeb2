/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Primitives;
using MechJebLib.PSG;
using MechJebLib.PSG.Terminal;
using Xunit;

namespace MechJebLibTest.PSGTests
{
    // These are always-on Check validations of user-settable PSG inputs, so they must pass in both Debug and Release.
    public class InputValidationTests
    {
        private const double R_BODY = 600000;

        // stage stats from KerbinTests.MainSailTinCanVacuum
        private static Ascent.AscentBuilder Builder(bool allowShutdown = true) =>
            Ascent.Builder()
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, allowShutdown: allowShutdown,
                    ispCurrent: 285.387620615011, minThrottle: 0.0)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06),
                    new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), 82132.7494998545, 3531600000000, R_BODY);

        [Fact]
        public void ValidTargetsBuild()
        {
            Builder().SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false).Build();
            Builder().SetTarget(700000, 800000, 700000, 0, 0, 0, 0, true, true, false).Build();
            // apR < -peR is a hyperbolic orbit
            Builder().SetTarget(700000, -800000, 700000, 0, 0, 0, 0, false, false, false).Build();
            // 0 < apR < peR is remapped to circular
            Builder().SetTarget(700000, 650000, 700000, 0, 0, 0, 0, false, false, false).Build();
        }

        [Fact]
        public void NoStages()
        {
            Assert.Throws<ArgumentException>(() =>
                Ascent.Builder()
                   .Initial(new V3(R_BODY, 0, 0), new V3(0, 100, 0), new V3(1, 0, 0), 0, 3531600000000, R_BODY)
                   .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false)
                   .Build());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(double.NaN)]
        public void BadPeriapsis(double peR) =>
            Assert.Throws<ArgumentException>(() => Builder().SetTarget(peR, 700000, 700000, 0, 0, 0, 0, false, false, false).Build());

        [Theory]
        [InlineData(-1)]
        [InlineData(-700000)] // sma == 0
        [InlineData(0)]
        [InlineData(double.NaN)]
        public void BadApoapsis(double apR) =>
            Assert.Throws<ArgumentException>(() => Builder().SetTarget(700000, apR, 700000, 0, 0, 0, 0, false, false, false).Build());

        [Fact]
        public void BadApoapsisIgnoredForFixedBurnTime()
        {
            // the apsides are not used by the fixed burntime terminal conditions so they are not validated
            Builder(false).SetTarget(700000, -1, 700000, 0, 0, 0, 0, true, false, false).Build();
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(-1, false)]
        [InlineData(double.NaN, false)]
        [InlineData(0, true)]
        public void BadFixedBurnTimeAttachment(double attR, bool lanflag) =>
            Assert.Throws<ArgumentException>(() => Builder(false).SetTarget(700000, 700000, attR, 0, 0, 0, 0, true, lanflag, false).Build());

        [Theory]
        [InlineData(-1, 450)]
        [InlineData(0, -1)]
        [InlineData(100, 50)]
        [InlineData(double.NaN, 450)]
        [InlineData(0, double.NaN)]
        [InlineData(0, double.PositiveInfinity)]
        public void BadCoast(double mint, double maxt) =>
            Assert.Throws<ArgumentException>(() => Ascent.Builder().AddCoast(1000, 1000, mint, maxt, 0, 0));

        [Theory]
        [InlineData(0)]
        [InlineData(-2)]
        public void BadOptimizerTimeout(int milliseconds) =>
            Assert.Throws<ArgumentException>(() => Builder().OptimizerTimeout(milliseconds));

        // a plan listed from the top of the rocket down, as FlightPlan.FromLogicalPlan does
        private static FlightPlan Plan()
        {
            var plan = new FlightPlan();
            foreach (StageType type in new[] { StageType.BURN, StageType.COAST_DURING, StageType.BURN, StageType.COAST_BEFORE, StageType.BURN })
                plan.Stages.Add(new FlightPlanStage(0, new FuelStats(), false, true, type, 0, false, -1));
            return plan;
        }

        // the KerbinTests.MainSailTinCanVacuum burn, coast-during coast and burn, built from the given stages of Plan()
        private static Ascent.AscentBuilder BurnCoastBurn(bool withPlan, int burn1, int coast, int burn2)
        {
            Ascent.AscentBuilder builder = Ascent.Builder()
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, ispCurrent: 285.387620615011, minThrottle: 0.0,
                    flightPlanIndex: burn1)
               .AddCoast(79699.9986022711, 79699.9986022711, 0, 450, 0, 0, massContinuity: true, flightPlanIndex: coast)
               .AddStage(79699.9986022711, 15700.0014199451, 1500000.09246676, 310.000018173591, 0, 0, ispCurrent: 285.387620615011, minThrottle: 0.0,
                    massContinuity: true, flightPlanIndex: burn2)
               .Initial(new V3(365592.912061998, -475862.737003702, -1017.92554628857), new V3(138.749920001822, 106.593409136045, 5.05460467277035E-06),
                    new V3(0.608250428397881, -0.793743408918215, -0.00172494351863861), 82132.7494998545, 3531600000000, R_BODY)
               .SetTarget(700000, 700000, 700000, 0, 0, 0, 0, false, false, false);

            if (withPlan)
                builder.FlightPlan(Plan());

            return builder;
        }

        [Theory]
        [InlineData(false, -1, -1, -1)]
        [InlineData(true, 2, 1, 0)]
        [InlineData(true, 4, 1, 0)] // stages may be skipped (burned out, or before the current coast)
        public void ValidFlightPlanIndicesBuild(bool withPlan, int burn1, int coast, int burn2) =>
            BurnCoastBurn(withPlan, burn1, coast, burn2).Build();

        [Theory]
        [InlineData(false, 2, 1, 0)]    // indices without a plan
        [InlineData(true, -1, -1, -1)]  // a plan without indices
        [InlineData(true, 2, -1, 0)]    // a phase without an index
        [InlineData(true, 5, 1, 0)]     // past the end of the plan
        [InlineData(true, 0, 1, 2)]     // not in flight order
        [InlineData(true, 2, 1, 2)]     // the same stage twice
        [InlineData(true, 3, 1, 0)]     // a burn from a coast stage
        [InlineData(true, 4, 2, 0)]     // a coast from a burn stage
        public void BadFlightPlanIndices(bool withPlan, int burn1, int coast, int burn2) =>
            Assert.Throws<ArgumentException>(() => BurnCoastBurn(withPlan, burn1, coast, burn2).Build());

        [Fact]
        public void NewCoastAllowsNegativeTimes()
        {
            // the hoverslam simulation uses negative coast times to propagate backwards, so Phase.NewCoast itself must not check
            using var phase = Phase.NewCoast(1.0, 1.0, 0, -100, 0, 0);
        }

        [Fact]
        public void TerminalConstructors()
        {
            Assert.Throws<ArgumentException>(() => new FlightPathAngle4(double.NaN, 1, 1, 0));
            Assert.Throws<ArgumentException>(() => new FlightPathAngle4(0, 1, double.NaN, 0));
            Assert.Throws<ArgumentException>(() => new FlightPathAngle5(double.NaN, 1, 1, 0, 0));
            Assert.Throws<ArgumentException>(() => new FlightPathAngle5(0, 1, 1, 0, double.NaN));
            Assert.Throws<ArgumentException>(() => new FlightPathAngle3Energy(0, 0, 0));
            Assert.Throws<ArgumentException>(() => new FlightPathAngle4Energy(0, -1, 0, 0));
        }
    }
}
