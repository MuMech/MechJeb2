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
    public class PlanEditorTests
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

        private static List<FuelStats> ThreeStages() =>
            new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 2), Row(2, 3000, 3) };

        // KSP stage 1 decouples the lower stage and has no engines: a place for a dedicated coast
        private static List<FuelStats> WithDecoupler() =>
            new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 3000, 3) };

        private static LogicalPlan Generate(List<FuelStats> rows, int lastStage = 0, int coastStage = -1, int coastLocation = 0,
            List<int>? unguided = null, List<int>? fixedStages = null) =>
            LogicalPlan.GenerateLogicalPlan(rows, unguided ?? new List<int>(), fixedStages ?? new List<int>(), lastStage, MIN_DV, coastStage,
                coastLocation);

        private static string Describe(LogicalPlan plan) =>
            string.Join(" ", plan.Stages.Select(s =>
                $"{s.StageType}{(s.StageType == StageType.COAST_DEDICATED ? "@" + s.KSPStage : "")}{(s.Unguided ? "/u" : "")}{(s.AllowShutdown ? "" : "/f")}:{(s.SkipToNextBurn ? "T" : "F")}"));

        private static string Describe(FlightPlan plan) => string.Join(" ", plan.Stages.Select(s => $"{s.StageType}:{s.MJStage}"));

        private static FlightPlan Fly(LogicalPlan plan, List<FuelStats> rows)
        {
            FlightPlan? flight = FlightPlan.FromLogicalPlan(plan, rows, rows, MIN_DV);
            Assert.NotNull(flight);
            return flight!;
        }

        // reading a generated plan into the editor and writing it back gives the same plan, flags included
        [Theory]
        [InlineData(-1, 0)]
        [InlineData(1, COAST_BEFORE)]
        [InlineData(1, COAST_DURING)]
        [InlineData(1, COAST_AFTER)]
        [InlineData(0, COAST_BEFORE)]
        [InlineData(2, COAST_DURING)]
        public void RoundTrip(int coastStage, int coastLocation)
        {
            List<FuelStats> rows = ThreeStages();
            LogicalPlan generated = Generate(rows, coastStage: coastStage, coastLocation: coastLocation, unguided: new List<int> { 2 },
                fixedStages: new List<int> { 1 });

            var editor = new PlanEditor(generated, rows, MIN_DV);

            Assert.True(editor.Fits);
            Assert.Equal(Describe(generated), Describe(editor.Plan()));
        }

        [Fact]
        public void RoundTripDedicatedCoast()
        {
            List<FuelStats> rows = WithDecoupler();
            LogicalPlan generated = Generate(rows, coastStage: 1);

            var editor = new PlanEditor(generated, rows, MIN_DV);

            Assert.Equal(new PlanEditor.CoastSlot(PlanEditor.CoastKind.DEDICATED, 1), editor.Coast);
            Assert.Equal(Describe(generated), Describe(editor.Plan()));
            Assert.Equal("BURN:0 COAST_DEDICATED:1 BURN:2", Describe(Fly(editor.Plan(), rows)));
        }

        [Fact]
        public void RoundTripPayloadAboveTheLastStage()
        {
            // the payload has its own engine with more than MinDeltaV, and the plan stops under it
            var rows = new List<FuelStats> { Row(0, 500, 9), Row(1, 2000, 1), Row(2, 3000, 2) };
            LogicalPlan generated = Generate(rows, 1);

            var editor = new PlanEditor(generated, rows, MIN_DV);

            Assert.Equal(2, editor.PlannedCount);
            Assert.Equal(3, editor.BurnRows.Count);
            Assert.False(editor.BurnRows[2].Planned);
            Assert.Equal(Describe(generated), Describe(editor.Plan()));
        }

        [Fact]
        public void RowsAreInFlightOrder()
        {
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 20, 5), Row(3, 3000, 3) };
            var editor = new PlanEditor(new LogicalPlan(), rows, MIN_DV);

            Assert.Equal(new[] { 3, 2, 1, 0 }, editor.Rows.Select(r => r.KSPStage));
            // by name: Mono's xunit sees an enum array as Int32 and calls it a different collection
            Assert.Equal(new[] { "BURN", "UNDER_MIN", "NO_ENGINE", "BURN" }, editor.Rows.Select(r => r.Kind.ToString()));
            Assert.Equal(new[] { 3, 0 }, editor.BurnRows.Select(r => r.KSPStage));
            // an empty plan plans every burn row, all optimized and guided
            Assert.Equal(2, editor.PlannedCount);
            Assert.All(editor.BurnRows, r => Assert.True(r.Planned && r.Guided && !r.FullBurn));
        }

        [Fact]
        public void EndAtLeavesOutTheRowsAbove()
        {
            List<FuelStats> rows = ThreeStages();
            var editor = new PlanEditor(Generate(rows), rows, MIN_DV);

            // in flight order the burn rows are KSP stages 2, 1, 0: end after the second
            editor.EndAt(1);

            Assert.Equal(2, editor.PlannedCount);
            Assert.Equal(Describe(Generate(rows, 1)), Describe(editor.Plan()));
            Assert.Equal("BURN:1 BURN:2", Describe(Fly(editor.Plan(), rows)));

            // at least one burn stays planned
            editor.EndAt(-5);
            Assert.Equal(1, editor.PlannedCount);
        }

        [Fact]
        public void FlagsAreWrittenPerBurnRow()
        {
            List<FuelStats> rows = ThreeStages();
            var editor = new PlanEditor(Generate(rows), rows, MIN_DV);

            editor.SetGuided(0, false);
            editor.SetFullBurn(1, true);

            Assert.Equal(Describe(Generate(rows, unguided: new List<int> { 2 }, fixedStages: new List<int> { 1 })), Describe(editor.Plan()));
        }

        [Fact]
        public void CoastSlotsInFlightOrder()
        {
            List<FuelStats> rows = WithDecoupler();
            var editor = new PlanEditor(Generate(rows), rows, MIN_DV);

            // burn rows in flight order: KSP stage 2 (vac 2), then KSP stage 0 (vac 0), with the decoupler (vac 1) between them
            Assert.Equal(new[] { "DURING 2", "AFTER 2", "DEDICATED 1", "BEFORE 0", "DURING 0" }, editor.Slots.Select(s => s.ToString()));
        }

        [Fact]
        public void AddCoastGoesBeforeTheLastBurn()
        {
            List<FuelStats> rows = ThreeStages();
            var editor = new PlanEditor(Generate(rows), rows, MIN_DV);

            editor.AddCoast();

            Assert.Equal(new PlanEditor.CoastSlot(PlanEditor.CoastKind.BEFORE, 0), editor.Coast);
            Assert.Equal(Describe(Generate(rows, coastStage: 0, coastLocation: COAST_BEFORE)), Describe(editor.Plan()));
        }

        [Fact]
        public void MoveCoastStepsThroughTheSlots()
        {
            List<FuelStats> rows = ThreeStages();
            var editor = new PlanEditor(Generate(rows), rows, MIN_DV);
            editor.AddCoast();

            // BEFORE 0 -> AFTER 1 -> DURING 1 -> BEFORE 1 -> AFTER 2 -> DURING 2, then no further
            var seen = new List<string> { editor.Coast!.Value.ToString() };
            while (editor.MoveCoast(-1))
                seen.Add(editor.Coast!.Value.ToString());

            Assert.Equal(new[] { "BEFORE 0", "AFTER 1", "DURING 1", "BEFORE 1", "AFTER 2", "DURING 2" }, seen);

            // each slot makes the plan the stage-number settings would have made
            editor.MoveCoast(+1);
            editor.MoveCoast(+1);
            editor.MoveCoast(+1);
            Assert.Equal("DURING 1", editor.Coast!.Value.ToString());
            Assert.Equal(Describe(Generate(rows, coastStage: 1, coastLocation: COAST_DURING)), Describe(editor.Plan()));
            Assert.Equal("BURN:0 BURN:1 COAST_DURING:1 BURN:1 BURN:2", Describe(Fly(editor.Plan(), rows)));
        }

        [Fact]
        public void EndAtDropsACoastWhoseSlotIsGone()
        {
            List<FuelStats> rows = ThreeStages();
            var editor = new PlanEditor(Generate(rows), rows, MIN_DV);
            editor.AddCoast(); // before the top burn, KSP stage 0

            editor.EndAt(1);

            Assert.Null(editor.Coast);
        }

        [Fact]
        public void APlanThatNoLongerFitsIsReported()
        {
            // a plan for four burn rows on a vessel with three
            var rows4 = new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 2), Row(2, 3000, 3), Row(3, 900, 4) };
            LogicalPlan plan = Generate(rows4);

            var editor = new PlanEditor(plan, ThreeStages(), MIN_DV);

            Assert.False(editor.Fits);
            // re-fitting by ending the plan somewhere makes it valid again
            editor.EndAt(2);
            Assert.True(editor.Fits);
            Assert.NotNull(FlightPlan.FromLogicalPlan(editor.Plan(), ThreeStages(), ThreeStages(), MIN_DV));
        }

        [Fact]
        public void CoastGuidedFlag()
        {
            List<FuelStats> rows = ThreeStages();
            var editor = new PlanEditor(Generate(rows), rows, MIN_DV);
            editor.AddCoast();

            editor.SetCoastGuided(false);

            Assert.True(editor.Plan().Stages.Single(s => s.StageType != StageType.BURN).Unguided);
            var reread = new PlanEditor(editor.Plan(), rows, MIN_DV);
            Assert.False(reread.CoastGuided);
        }

        // the rows the vessel stages into: the first row of each KSP stage after the first burn, not the second part of a stage
        // with two burn rows nor an engine-less stage
        [Fact]
        public void StagedRows()
        {
            // flight order: stage 3 (boosters with the core, then the core alone: two rows), stage 2 (a decoupler), stage 1, stage 0
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 2), Row(2, 0), Row(3, 900, 3), Row(3, 1200, 3, 4) };

            var editor = new PlanEditor(new LogicalPlan(), rows, MIN_DV);

            Assert.Equal(new[] { 3, 3, 2, 1, 0 }, editor.Rows.Select(r => r.KSPStage).ToArray());
            Assert.Equal(new[] { false, false, false, true, true }, editor.Rows.Select(r => r.Staged).ToArray());
        }
    }
}
