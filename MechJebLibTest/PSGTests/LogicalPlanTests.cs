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

        private const PlanEditor.CoastKind BEFORE    = PlanEditor.CoastKind.BEFORE;
        private const PlanEditor.CoastKind DURING    = PlanEditor.CoastKind.DURING;
        private const PlanEditor.CoastKind AFTER     = PlanEditor.CoastKind.AFTER;
        private const PlanEditor.CoastKind DEDICATED = PlanEditor.CoastKind.DEDICATED;

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

        // the plan the stage editor makes: the bottom planned burn rows (all of them by default), and the coast in the slot of
        // its kind at the VacStats row coastRow
        private static LogicalPlan Edited(List<FuelStats> rows, int planned = 0, PlanEditor.CoastKind? coast = null, int coastRow = -1)
        {
            var editor = new PlanEditor(new LogicalPlan(), rows, MIN_DV);
            if (planned > 0)
                editor.EndAt(planned - 1);
            if (coast.HasValue)
                Assert.True(editor.SetCoast(new PlanEditor.CoastSlot(coast.Value, coastRow)));
            return editor.Plan();
        }

        private static FlightPlan Fly(List<FuelStats> rows, int planned = 0, PlanEditor.CoastKind? coast = null, int coastRow = -1)
        {
            FlightPlan? flightPlan = FlightPlan.FromLogicalPlan(Edited(rows, planned, coast, coastRow), rows, rows, MIN_DV);
            Assert.NotNull(flightPlan);
            return flightPlan!;
        }

        // whether the editor offers a coast there
        private static bool HasSlot(List<FuelStats> rows, int planned, PlanEditor.CoastKind coast, int coastRow)
        {
            var editor = new PlanEditor(new LogicalPlan(), rows, MIN_DV);
            if (planned > 0)
                editor.EndAt(planned - 1);
            return editor.SetCoast(new PlanEditor.CoastSlot(coast, coastRow));
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
            Assert.Equal("BURN:T BURN:T BURN:T", Describe(Edited(ThreeStages())));
            Assert.Equal("BURN:0 BURN:1 BURN:2", Describe(Fly(ThreeStages())));
        }

        [Fact]
        public void CoastBefore()
        {
            Assert.Equal("BURN:T BURN:T COAST_BEFORE:F BURN:T", Describe(Edited(ThreeStages(), coast: BEFORE, coastRow: 1)));
            Assert.Equal("BURN:0 BURN:1 COAST_BEFORE:1 BURN:2",
                Describe(Fly(ThreeStages(), coast: BEFORE, coastRow: 1)));
        }

        [Fact]
        public void CoastDuring()
        {
            Assert.Equal("BURN:T BURN:T COAST_DURING:F BURN:F BURN:T",
                Describe(Edited(ThreeStages(), coast: DURING, coastRow: 1)));
            Assert.Equal("BURN:0 BURN:1 COAST_DURING:1 BURN:1 BURN:2",
                Describe(Fly(ThreeStages(), coast: DURING, coastRow: 1)));
        }

        [Fact]
        public void CoastAfter()
        {
            Assert.Equal("BURN:T COAST_AFTER:T BURN:F BURN:T", Describe(Edited(ThreeStages(), coast: AFTER, coastRow: 1)));
            Assert.Equal("BURN:0 COAST_AFTER:1 BURN:1 BURN:2",
                Describe(Fly(ThreeStages(), coast: AFTER, coastRow: 1)));
        }

        [Fact]
        public void DedicatedCoast()
        {
            // KSP stage 1 decouples the lower stage and coasts, no engines
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 3000, 3) };

            Assert.Equal("BURN:T COAST_DEDICATED:F BURN:T", Describe(Edited(rows, coast: DEDICATED, coastRow: 1)));
            Assert.Equal("BURN:0 COAST_DEDICATED:1 BURN:2", Describe(Fly(rows, coast: DEDICATED, coastRow: 1)));
        }

        [Fact]
        public void NoCoastAfterTheFinalBurn()
        {
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 3000, 2) };

            Assert.False(HasSlot(rows, 0, AFTER, 0));
        }

        [Fact]
        public void NoCoastBeforeTheFirstBurn()
        {
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 3000, 2) };

            Assert.False(HasSlot(rows, 0, BEFORE, 1));
        }

        [Fact]
        public void NoDedicatedCoastAboveTheLastBurn()
        {
            var rows = new List<FuelStats> { Row(0, 0), Row(1, 2000, 1), Row(2, 3000, 2) };

            Assert.False(HasSlot(rows, 0, DEDICATED, 0));
        }

        [Fact]
        public void PayloadAboveLastStageIsSkipped()
        {
            // the payload has its own engine with more than MinDeltaV
            var rows = new List<FuelStats> { Row(0, 500, 9), Row(1, 2000, 1), Row(2, 3000, 2) };

            Assert.Equal("BURN:1 BURN:2", Describe(Fly(rows, 2)));
        }

        [Fact]
        public void PayloadAboveLastStageIsSkippedWithCoastDuring()
        {
            // coast-during adds two BURNs on one row, the starting row has to be counted with SkipToNextBurn
            var rows = new List<FuelStats> { Row(0, 500, 9), Row(1, 2000, 1), Row(2, 3000, 2) };

            Assert.Equal("BURN:1 BURN:2 COAST_DURING:2 BURN:2", Describe(Fly(rows, 2, DURING, 2)));
        }

        [Fact]
        public void SplitCoastStageGetsOneCoast()
        {
            // KSP stage 1 burns in two segments over MinDeltaV (engine 4 burns out early), the later segment is listed first
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 500, 2), Row(1, 800, 2, 4), Row(2, 3000, 3) };

            Assert.Equal("BURN:T COAST_AFTER:T BURN:F BURN:T BURN:T", Describe(Edited(rows, coast: AFTER, coastRow: 1)));
            Assert.Equal("BURN:0 COAST_AFTER:1 BURN:1 BURN:2 BURN:3",
                Describe(Fly(rows, coast: AFTER, coastRow: 1)));
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
            FlightPlan plan = Fly(ThreeStages(), coast: DURING, coastRow: 1);
            plan.ReconcileStages(ThreeStages(), ThreeStages(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1 COAST_DURING:1 BURN:1 BURN:2", Describe(plan));
        }

        [Fact]
        public void ReconcileDropTanksStaged()
        {
            // core engine 5 burns from the drop tanks in KSP stage 2, then from its own tanks in KSP stage 1
            FlightPlan plan = Fly(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5), Row(2, 800, 5) });
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileDropTanksDryButAttached()
        {
            FlightPlan plan = Fly(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5), Row(2, 800, 5) });
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5), Row(2, 5, 5) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileEngineOut()
        {
            FlightPlan plan = Fly(new List<FuelStats> { Row(0, 2000, 1), Row(1, 3000, 2, 3, 4) });
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 2500, 2, 4) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1", Describe(plan));
            Assert.Equal(2, plan.Stages[1].FuelStats.EngineSignatures.Count);
        }

        [Fact]
        public void ReconcileEngineOutThenStaging()
        {
            FlightPlan plan = Fly(new List<FuelStats> { Row(0, 2000, 1), Row(1, 3000, 2, 3, 4) });

            // engine 3 is lost in the booster, which keeps its stage
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 2500, 2, 4) }, new List<FuelStats>(), MIN_DV);
            Assert.Equal("BURN:0 BURN:1", Describe(plan));

            // then the booster is staged off and only the upper stage is left
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1) }, new List<FuelStats>(), MIN_DV);
            Assert.Equal("BURN:0 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileHotStagedRow()
        {
            FlightPlan plan = Fly(new List<FuelStats> { Row(0, 2000, 1), Row(1, 3000, 2) });

            // hot staging lights the upper engine 1 while booster engine 2 still burns: the current row has both, and the booster
            // keeps its stage
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 50, 1, 2) }, new List<FuelStats>(), MIN_DV);
            Assert.Equal("BURN:0 BURN:1", Describe(plan));
            Assert.Equal(2, plan.Stages[1].FuelStats.EngineSignatures.Count);

            // the booster separates: the matched row's engine 1 doesn't make the booster match the upper stage's row
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1) }, new List<FuelStats>(), MIN_DV);
            Assert.Equal("BURN:0 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileBoosterBurnoutWithSustainerStage()
        {
            // sustainer 2 burns with boosters 3 and 4, then alone in the same KSP stage
            FlightPlan plan = Fly(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1000, 2), Row(1, 2000, 2, 3, 4) });
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1000, 2) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 BURN:1 BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileSkipsSmallVernierTail()
        {
            // verniers 3 and 4 burn a little longer than main engine 2 (issue #1917)
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 10, 3, 4), Row(1, 3000, 2, 3, 4) };

            FlightPlan plan = Fly(rows);
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

            FlightPlan plan = Fly(rows, 2);
            plan.ReconcileStages(rows, rows, MIN_DV);

            Assert.Equal("BURN:2 BURN:3", Describe(plan));
        }

        [Fact]
        public void ReconcileCoastAfterMidCoast()
        {
            FlightPlan plan = Fly(ThreeStages(), coast: AFTER, coastRow: 1);

            // the middle stage has burned out and the bottom stage is gone
            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 0) }, new List<FuelStats>(), MIN_DV);

            Assert.Equal("BURN:0 COAST_AFTER:1 BURN:Done BURN:Done", Describe(plan));
        }

        [Fact]
        public void ReconcileDedicatedCoastAfterFairingJettison()
        {
            // KSP stage 2 decouples the lower stage and coasts, stage 1 jettisons the fairing during the coast
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 0), Row(3, 3000, 2) };

            FlightPlan plan = Fly(rows, coast: DEDICATED, coastRow: 2);
            Assert.Equal("BURN:0 COAST_DEDICATED:2 BURN:3", Describe(plan));

            plan.ReconcileStages(new List<FuelStats> { Row(0, 2000, 1), Row(1, 0) }, new List<FuelStats>(), MIN_DV);
            Assert.Equal("BURN:0 COAST_DEDICATED:2 BURN:Done", Describe(plan));
        }

        private static uint[] Jettisoned(FlightPlan plan, int index)
        {
            var parts = new HashSet<uint> { 99 }; // cleared first
            plan.PartsJettisonedWith(index, parts);
            return parts.OrderBy(p => p).ToArray();
        }

        [Fact]
        public void JettisonedPartsPlainRocket()
        {
            FlightPlan plan = Fly(ThreeStages());

            Assert.Equal(new uint[] { 3 }, Jettisoned(plan, 2));
            Assert.Equal(new uint[] { 2 }, Jettisoned(plan, 1));
        }

        [Fact]
        public void JettisonedPartsDropTanks()
        {
            // core engine 5 burns from the drop tanks in KSP stage 2 and on from its own tanks in KSP stage 1: nothing leaves
            // with the drop tanks' burn, and the core leaves with its own
            FlightPlan plan = Fly(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5), Row(2, 800, 5) });

            Assert.Empty(Jettisoned(plan, 2));
            Assert.Equal(new uint[] { 5 }, Jettisoned(plan, 1));
        }

        [Fact]
        public void JettisonedPartsSustainer()
        {
            // boosters 6 and 7 burn with sustainer 5, which burns on alone: only the boosters leave
            FlightPlan plan = Fly(new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 5), Row(2, 800, 5, 6, 7) });

            Assert.Equal(new uint[] { 6, 7 }, Jettisoned(plan, 2));
        }

        [Fact]
        public void CoastAfterCarriesItsBurn()
        {
            FlightPlan plan = Fly(ThreeStages(), coast: AFTER, coastRow: 1);
            Assert.Equal("BURN:0 COAST_AFTER:1 BURN:1 BURN:2", Describe(plan));

            // the coast is on the burn's row with its engines, which must not count as a later burn's
            Assert.Equal(2, plan.CoastAfterBurnIndex());
            Assert.Equal(new uint[] { 2 }, Jettisoned(plan, 2));
        }

        [Fact]
        public void NoCoastAfterBurn()
        {
            Assert.Equal(-1, Fly(ThreeStages()).CoastAfterBurnIndex());
            Assert.Equal(-1, Fly(ThreeStages(), coast: BEFORE, coastRow: 1).CoastAfterBurnIndex());
            Assert.Equal(-1, Fly(ThreeStages(), coast: DURING, coastRow: 1).CoastAfterBurnIndex());
        }
    }
}
