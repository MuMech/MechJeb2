/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using System.Linq;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.PSG;
using Xunit;

namespace MechJebLibTest.PSGTests
{
    // rows are listed top-down like VacStats: index 0 is KSP stage 0, the last row is the current stage
    public class LogicalPlanTests
    {
        private const double MIN_DV = 40;

        private const int COAST_BEFORE = -1;
        private const int COAST_DURING = 0;
        private const int COAST_AFTER  = 1;

        private static FuelStats Row(int kspStage, double deltaV, params uint[] engines)
        {
            var stats = new FuelStats
            {
                KSPStage = kspStage, DeltaV = deltaV, Isp = 300, StartMass = 10, EndMass = 5, MaxThrust = 100, MinThrust = 100
            };

            foreach (uint engine in engines)
                stats.AddEngineSignature(new EngineSignature(engine, 0));
            stats.SortEngineSignatures();

            return stats;
        }

        private static LogicalPlan Generate(List<FuelStats> rows, int lastStage = 0, int coastStage = -1, int coastLocation = 0) =>
            LogicalPlan.GenerateLogicalPlan(rows, new List<int>(), new List<int>(), lastStage, MIN_DV, coastStage, coastLocation);

        private static FlightPlan GenerateFlightPlan(List<FuelStats> rows, int lastStage = 0, int coastStage = -1, int coastLocation = 0)
        {
            FlightPlan? flightPlan = FlightPlan.FromLogicalPlan(Generate(rows, lastStage, coastStage, coastLocation), rows, rows, MIN_DV);
            Assert.NotNull(flightPlan);
            return flightPlan!;
        }

        private static LogicalPlan Plan(params StageType[] types)
        {
            var plan = new LogicalPlan();
            foreach (StageType type in types)
                plan.Stages.Add(new LogicalPlanStage { StageType = type, SkipToNextBurn = false });
            return plan;
        }

        private static string Describe(LogicalPlan plan) =>
            string.Join(" ", plan.Stages.Select(s => $"{s.StageType}:{(s.SkipToNextBurn ? "T" : "F")}"));

        private static string Describe(FlightPlan plan) =>
            string.Join(" ", plan.Stages.Select(s => s.Done ? $"{s.StageType}:Done" : $"{s.StageType}:{s.MJStage}"));

        private static List<FuelStats> ThreeStages() =>
            new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 2), Row(2, 3000, 3) };

        [Fact]
        public void PlainRocket()
        {
            Assert.Equal("BURN:T BURN:T BURN:T", Describe(Generate(ThreeStages())));
            Assert.Equal("BURN:0 BURN:1 BURN:2", Describe(GenerateFlightPlan(ThreeStages())));
        }

        [Fact]
        public void CoastBefore()
        {
            Assert.Equal("BURN:T BURN:T COAST_BEFORE:F BURN:T", Describe(Generate(ThreeStages(), coastStage: 1, coastLocation: COAST_BEFORE)));
            Assert.Equal("BURN:0 BURN:1 COAST_BEFORE:1 BURN:2",
                Describe(GenerateFlightPlan(ThreeStages(), coastStage: 1, coastLocation: COAST_BEFORE)));
        }

        [Fact]
        public void CoastDuring()
        {
            Assert.Equal("BURN:T BURN:T COAST_DURING:F BURN:F BURN:T",
                Describe(Generate(ThreeStages(), coastStage: 1, coastLocation: COAST_DURING)));
            Assert.Equal("BURN:0 BURN:1 COAST_DURING:1 BURN:1 BURN:2",
                Describe(GenerateFlightPlan(ThreeStages(), coastStage: 1, coastLocation: COAST_DURING)));
        }

        [Fact]
        public void CoastAfter()
        {
            Assert.Equal("BURN:T COAST_AFTER:T BURN:F BURN:T", Describe(Generate(ThreeStages(), coastStage: 1, coastLocation: COAST_AFTER)));
            Assert.Equal("BURN:0 COAST_AFTER:1 BURN:1 BURN:2",
                Describe(GenerateFlightPlan(ThreeStages(), coastStage: 1, coastLocation: COAST_AFTER)));
        }

        [Fact]
        public void DedicatedCoast()
        {
            // KSP stage 1 decouples the lower stage and coasts, no engines
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 3000, 3) };

            Assert.Equal("BURN:T COAST_DEDICATED:F BURN:T", Describe(Generate(rows, coastStage: 1)));
            Assert.Equal("BURN:0 COAST_DEDICATED:1 BURN:2", Describe(GenerateFlightPlan(rows, coastStage: 1)));
        }

        [Fact]
        public void CoastAfterTheFinalBurnIsRemoved()
        {
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 3000, 2) };

            Assert.Equal("BURN:T BURN:T", Describe(Generate(rows, coastStage: 0, coastLocation: COAST_AFTER)));
        }

        [Fact]
        public void CoastBeforeTheFirstBurnIsRemoved()
        {
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 3000, 2) };

            Assert.Equal("BURN:T BURN:T", Describe(Generate(rows, coastStage: 1, coastLocation: COAST_BEFORE)));
        }

        [Fact]
        public void DedicatedCoastAboveLastStageIsRemoved()
        {
            var rows = new List<FuelStats> { Row(0, 0), Row(1, 2000, 1), Row(2, 3000, 2) };

            Assert.Equal("BURN:T BURN:T", Describe(Generate(rows, 1, 0)));
        }

        [Fact]
        public void PayloadAboveLastStageIsSkipped()
        {
            // the payload has its own engine with more than MinDeltaV
            var rows = new List<FuelStats> { Row(0, 500, 9), Row(1, 2000, 1), Row(2, 3000, 2) };

            Assert.Equal("BURN:1 BURN:2", Describe(GenerateFlightPlan(rows, 1)));
        }

        [Fact]
        public void PayloadAboveLastStageIsSkippedWithCoastDuring()
        {
            // coast-during adds two BURNs on one row, the starting row has to be counted with SkipToNextBurn
            var rows = new List<FuelStats> { Row(0, 500, 9), Row(1, 2000, 1), Row(2, 3000, 2) };

            Assert.Equal("BURN:1 BURN:2 COAST_DURING:2 BURN:2", Describe(GenerateFlightPlan(rows, 1, 2, COAST_DURING)));
        }

        [Fact]
        public void SplitCoastStageGetsOneCoast()
        {
            // KSP stage 1 burns in two segments over MinDeltaV (engine 4 burns out early), the later segment is listed first
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 500, 2), Row(1, 800, 2, 4), Row(2, 3000, 3) };

            Assert.Equal("BURN:T COAST_AFTER:T BURN:F BURN:T BURN:T", Describe(Generate(rows, coastStage: 1, coastLocation: COAST_AFTER)));
            Assert.Equal("BURN:0 COAST_AFTER:1 BURN:1 BURN:2 BURN:3",
                Describe(GenerateFlightPlan(rows, coastStage: 1, coastLocation: COAST_AFTER)));
        }

        [Fact]
        public void SanitizeClearsMoreThanOneCoast()
        {
            LogicalPlan plan = Plan(StageType.BURN, StageType.COAST_BEFORE, StageType.COAST_AFTER, StageType.BURN);
            plan.Sanitize();

            Assert.Empty(plan.Stages);
        }

        [Fact]
        public void SanitizeRemovesCoastsAtTheEnds()
        {
            LogicalPlan plan = Plan(StageType.COAST_AFTER, StageType.BURN, StageType.BURN);
            plan.Sanitize();
            Assert.Equal("BURN:T BURN:T", Describe(plan));

            plan = Plan(StageType.BURN, StageType.BURN, StageType.COAST_BEFORE);
            plan.Sanitize();
            Assert.Equal("BURN:T BURN:T", Describe(plan));

            plan = Plan(StageType.COAST_DEDICATED);
            plan.Sanitize();
            Assert.Empty(plan.Stages);
        }

        [Fact]
        public void SanitizeRecomputesSkipToNextBurn()
        {
            LogicalPlan plan = Plan(StageType.BURN, StageType.BURN, StageType.COAST_BEFORE, StageType.BURN);
            plan.Sanitize();
            Assert.Equal("BURN:T BURN:T COAST_BEFORE:F BURN:T", Describe(plan));

            plan = Plan(StageType.BURN, StageType.BURN, StageType.COAST_DURING, StageType.BURN, StageType.BURN);
            plan.Sanitize();
            Assert.Equal("BURN:T BURN:T COAST_DURING:F BURN:F BURN:T", Describe(plan));

            plan = Plan(StageType.BURN, StageType.COAST_AFTER, StageType.BURN, StageType.BURN);
            plan.Sanitize();
            Assert.Equal("BURN:T COAST_AFTER:T BURN:F BURN:T", Describe(plan));

            plan = Plan(StageType.BURN, StageType.COAST_DEDICATED, StageType.BURN);
            plan.Sanitize();
            Assert.Equal("BURN:T COAST_DEDICATED:F BURN:T", Describe(plan));
        }

        [Fact]
        public void ReconcileOnThePadChangesNothing()
        {
            FlightPlan plan = GenerateFlightPlan(ThreeStages(), coastStage: 1, coastLocation: COAST_DURING);
            plan.ReconcileStages(ThreeStages(), ThreeStages(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1 COAST_DURING:1 BURN:1 BURN:2", Describe(plan));
        }

        [Fact]
        public void ReconcileDropTanksStaged()
        {
            // core engine 5 burns from the drop tanks in KSP stage 2, then from its own tanks in KSP stage 1
            FlightPlan plan = GenerateFlightPlan(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5), Row(2, 800, 5) });
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileDropTanksDryButAttached()
        {
            FlightPlan plan = GenerateFlightPlan(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5), Row(2, 800, 5) });
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5), Row(2, 5, 5) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileEngineOut()
        {
            FlightPlan plan = GenerateFlightPlan(new List<FuelStats> { Row(0, 2000, 1), Row(1, 3000, 2, 3, 4) });
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 2500, 2, 4) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1", Describe(plan));
            Assert.Equal(2, plan.Stages[1].FuelStats.EngineSignatures.Count);
        }

        [Fact]
        public void ReconcileBoosterBurnoutWithSustainerStage()
        {
            // sustainer 2 burns with boosters 3 and 4, then alone in the same KSP stage
            FlightPlan plan = GenerateFlightPlan(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1000, 2), Row(1, 2000, 2, 3, 4) });
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1000, 2) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileSkipsSmallVernierTail()
        {
            // verniers 3 and 4 burn a little longer than main engine 2 (issue #1917)
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 10, 3, 4), Row(1, 3000, 2, 3, 4) };

            FlightPlan plan = GenerateFlightPlan(rows);
            Assert.Equal("BURN:0 BURN:2", Describe(plan));

            plan.ReconcileStages(rows, rows, MIN_DV);
            Assert.Equal("BURN:0 BURN:2", Describe(plan));

            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 10, 3, 4) }, new List<FuelStats>(), MIN_DV);
            Assert.Equal("BURN:0 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileIgnoresEngineLessRowsAbovePayloadEngine()
        {
            // parachutes in KSP stage 0, service module engine in stage 1, both above LastStage
            var rows = new List<FuelStats> { Row(0, 0), Row(1, 600, 9), Row(2, 2000, 1), Row(3, 3000, 2) };

            FlightPlan plan = GenerateFlightPlan(rows, 2);
            plan.ReconcileStages(rows, rows, MIN_DV);

            Assert.Equal("BURN:2 BURN:3", Describe(plan));
        }

        [Fact]
        public void ReconcileCoastAfterMidCoast()
        {
            FlightPlan plan = GenerateFlightPlan(ThreeStages(), coastStage: 1, coastLocation: COAST_AFTER);

            // the middle stage has burned out and the bottom stage is gone
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 0) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 COAST_AFTER:1 BURN:Done BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileDedicatedCoastAfterFairingJettison()
        {
            // KSP stage 2 decouples the lower stage and coasts, stage 1 jettisons the fairing during the coast
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 0), Row(3, 3000, 2) };

            FlightPlan plan = GenerateFlightPlan(rows, coastStage: 2);
            Assert.Equal("BURN:0 COAST_DEDICATED:2 BURN:3", Describe(plan));

            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 0) }, new List<FuelStats>(), MIN_DV);
            Assert.Equal("BURN:0 COAST_DEDICATED:2 BURN:Done", Describe(plan));
        }
    }
}
