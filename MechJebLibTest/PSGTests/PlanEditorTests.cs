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

        private static PlanEditor Editor(List<FuelStats> rows) => new PlanEditor(new LogicalPlan(), rows, MIN_DV);

        private static string Describe(LogicalPlan plan) =>
            string.Join(" ", plan.Stages.Select(s =>
                $"{s.StageType}{(s.StageType == StageType.COAST_DEDICATED ? "@" + s.KSPStage : "")}{(s.Unguided ? "/u" : "")}{(s.AllowShutdown ? "" : "/f")}{(s.Spinup ? "/s" : "")}:{(s.SkipToNextBurn ? "T" : "F")}"));

        private static string Describe(FlightPlan plan) => string.Join(" ", plan.Stages.Select(s => $"{s.StageType}:{s.MJStage}"));

        private static FlightPlan Fly(LogicalPlan plan, List<FuelStats> rows)
        {
            FlightPlan? flight = FlightPlan.FromLogicalPlan(plan, rows, rows, MIN_DV);
            Assert.NotNull(flight);
            return flight!;
        }

        private static LogicalPlanStage Burn(bool unguided = false, bool full = false) =>
            new LogicalPlanStage { StageType = StageType.BURN, Unguided = unguided, AllowShutdown = !full };

        // a coast takes the full burn flag of the burn it is next to
        private static LogicalPlanStage Coast(StageType type, int kspStage = 0, bool unguided = false, bool spinup = false, bool full = false) =>
            new LogicalPlanStage { StageType = type, KSPStage = kspStage, Unguided = unguided, Spinup = spinup, AllowShutdown = !full };

        private static LogicalPlan Sanitized(params LogicalPlanStage[] stages)
        {
            var plan = new LogicalPlan();
            plan.Stages.AddRange(stages);
            plan.Sanitize();
            return plan;
        }

        // the plans of RoundTrip, top-down (the top stage, KSP stage 0, first), with stage 2 unguided and stage 1 a full burn
        private static LogicalPlan RoundTripPlan(string name) =>
            name switch
            {
                "before 1"           => Sanitized(Burn(), Burn(full: true), Coast(StageType.COAST_BEFORE, full: true), Burn(true)),
                "during 1"           => Sanitized(Burn(), Burn(full: true), Coast(StageType.COAST_DURING, full: true), Burn(full: true), Burn(true)),
                "after 1, spin-up"   => Sanitized(Burn(), Coast(StageType.COAST_AFTER, spinup: true, full: true), Burn(full: true), Burn(true)),
                "before 1, unguided" => Sanitized(Burn(), Burn(full: true), Coast(StageType.COAST_BEFORE, unguided: true, full: true), Burn(true)),
                "during 2"           => Sanitized(Burn(), Burn(full: true), Burn(true), Coast(StageType.COAST_DURING), Burn(true)),
                _                    => Sanitized(Burn(), Burn(full: true), Burn(true))
            };

        // reading a plan into the editor and writing it back gives the same plan, flags included
        [Theory]
        [InlineData("none")]
        [InlineData("before 1")]
        [InlineData("during 1")]
        [InlineData("after 1, spin-up")]
        [InlineData("before 1, unguided")]
        [InlineData("during 2")]
        public void RoundTrip(string name)
        {
            List<FuelStats> rows = ThreeStages();
            LogicalPlan plan = RoundTripPlan(name);

            var editor = new PlanEditor(plan, rows, MIN_DV);

            Assert.True(editor.Fits);
            Assert.Equal(Describe(plan), Describe(editor.Plan()));
        }

        [Fact]
        public void RoundTripDedicatedCoast()
        {
            List<FuelStats> rows = WithDecoupler();
            LogicalPlan plan = Sanitized(Burn(), Coast(StageType.COAST_DEDICATED, 1, spinup: true), Burn());

            var editor = new PlanEditor(plan, rows, MIN_DV);

            Assert.Equal(new PlanEditor.CoastSlot(PlanEditor.CoastKind.DEDICATED, 1), editor.Coast);
            Assert.True(editor.CoastSpinup);
            Assert.Equal(Describe(plan), Describe(editor.Plan()));
            Assert.Equal("BURN:0 COAST_DEDICATED:1 BURN:2", Describe(Fly(editor.Plan(), rows)));
        }

        [Fact]
        public void RoundTripPayloadAboveTheLastBurn()
        {
            // the payload has its own engine with more than MinDeltaV, and the plan stops under it
            var rows = new List<FuelStats> { Row(0, 500, 9), Row(1, 2000, 1), Row(2, 3000, 2) };
            LogicalPlan plan = Sanitized(Burn(), Burn());

            var editor = new PlanEditor(plan, rows, MIN_DV);

            Assert.Equal(2, editor.PlannedCount);
            Assert.Equal(3, editor.BurnRows.Count);
            Assert.False(editor.BurnRows[2].Planned);
            Assert.Equal(Describe(plan), Describe(editor.Plan()));
            Assert.Equal("BURN:1 BURN:2", Describe(Fly(editor.Plan(), rows)));
        }

        [Fact]
        public void DefaultPlanBurnsEveryRowGuided()
        {
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 20, 5), Row(3, 3000, 3) };

            Assert.Equal("BURN:T BURN:T", Describe(PlanEditor.DefaultPlan(rows, MIN_DV)));
            Assert.Empty(PlanEditor.DefaultPlan(new List<FuelStats>(), MIN_DV).Stages);
        }

        [Fact]
        public void RowsAreInFlightOrder()
        {
            var rows = new List<FuelStats> { Row(0, 2000, 1), Row(1, 0), Row(2, 20, 5), Row(3, 3000, 3) };
            PlanEditor editor = Editor(rows);

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
            PlanEditor editor = Editor(rows);

            // in flight order the burn rows are KSP stages 2, 1, 0: end after the second
            editor.EndAt(1);

            Assert.Equal(2, editor.PlannedCount);
            Assert.Equal("BURN:T BURN:T", Describe(editor.Plan()));
            Assert.Equal("BURN:1 BURN:2", Describe(Fly(editor.Plan(), rows)));

            // at least one burn stays planned
            editor.EndAt(-5);
            Assert.Equal(1, editor.PlannedCount);
        }

        [Fact]
        public void FlagsAreWrittenPerBurnRow()
        {
            List<FuelStats> rows = ThreeStages();
            PlanEditor editor = Editor(rows);

            editor.SetGuided(0, false);
            editor.SetFullBurn(1, true);

            Assert.Equal("BURN:T BURN/f:T BURN/u:T", Describe(editor.Plan()));
        }

        [Fact]
        public void CoastSlotsInFlightOrder()
        {
            List<FuelStats> rows = WithDecoupler();
            PlanEditor editor = Editor(rows);

            // burn rows in flight order: KSP stage 2 (vac 2), then KSP stage 0 (vac 0), with the decoupler (vac 1) between them
            Assert.Equal(new[] { "DURING 2", "AFTER 2", "DEDICATED 1", "BEFORE 0", "DURING 0" }, editor.Slots.Select(s => s.ToString()));
        }

        [Fact]
        public void AddCoastGoesBeforeTheLastBurn()
        {
            List<FuelStats> rows = ThreeStages();
            PlanEditor editor = Editor(rows);

            editor.AddCoast();

            Assert.Equal(new PlanEditor.CoastSlot(PlanEditor.CoastKind.BEFORE, 0), editor.Coast);
            Assert.Equal("BURN:T COAST_BEFORE:F BURN:T BURN:T", Describe(editor.Plan()));
        }

        [Fact]
        public void MoveCoastStepsThroughTheSlots()
        {
            List<FuelStats> rows = ThreeStages();
            PlanEditor editor = Editor(rows);
            editor.AddCoast();

            // BEFORE 0 -> AFTER 1 -> DURING 1 -> BEFORE 1 -> AFTER 2 -> DURING 2, then no further
            var seen = new List<string> { editor.Coast!.Value.ToString() };
            while (editor.MoveCoast(-1))
                seen.Add(editor.Coast!.Value.ToString());

            Assert.Equal(new[] { "BEFORE 0", "AFTER 1", "DURING 1", "BEFORE 1", "AFTER 2", "DURING 2" }, seen);

            editor.MoveCoast(+1);
            editor.MoveCoast(+1);
            editor.MoveCoast(+1);
            Assert.Equal("DURING 1", editor.Coast!.Value.ToString());
            Assert.Equal("BURN:T BURN:T COAST_DURING:F BURN:F BURN:T", Describe(editor.Plan()));
            Assert.Equal("BURN:0 BURN:1 COAST_DURING:1 BURN:1 BURN:2", Describe(Fly(editor.Plan(), rows)));
        }

        [Fact]
        public void EndAtDropsACoastWhoseSlotIsGone()
        {
            List<FuelStats> rows = ThreeStages();
            PlanEditor editor = Editor(rows);
            editor.AddCoast(); // before the top burn, KSP stage 0
            editor.SetCoastSpinup(true);

            editor.EndAt(1);

            Assert.Null(editor.Coast);
            Assert.False(editor.CoastSpinup);
        }

        [Fact]
        public void APlanThatNoLongerFitsIsReported()
        {
            // a plan for four burn rows on a vessel with three
            var rows4 = new List<FuelStats> { Row(0, 2000, 1), Row(1, 1500, 2), Row(2, 3000, 3), Row(3, 900, 4) };
            LogicalPlan plan = PlanEditor.DefaultPlan(rows4, MIN_DV);

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
            PlanEditor editor = Editor(rows);
            editor.AddCoast();

            editor.SetCoastGuided(false);

            Assert.True(editor.Plan().Stages.Single(s => s.StageType != StageType.BURN).Unguided);
            var reread = new PlanEditor(editor.Plan(), rows, MIN_DV);
            Assert.False(reread.CoastGuided);
        }

        [Fact]
        public void SpinupNeedsAGuidedCoast()
        {
            List<FuelStats> rows = ThreeStages();
            PlanEditor editor = Editor(rows);

            // no coast, no spin-up
            editor.SetCoastSpinup(true);
            Assert.False(editor.CoastSpinup);

            // turning the spin-up on makes the coast guided
            editor.AddCoast();
            editor.SetCoastGuided(false);
            editor.SetCoastSpinup(true);
            Assert.True(editor.CoastSpinup && editor.CoastGuided);

            // and an unguided coast doesn't spin up
            editor.SetCoastGuided(false);
            Assert.False(editor.CoastSpinup);

            editor.SetCoastSpinup(true);
            Assert.True(editor.Plan().Stages.Single(s => s.StageType != StageType.BURN).Spinup);

            // the spin-up goes with the coast
            editor.RemoveCoast();
            editor.AddCoast();
            Assert.False(editor.CoastSpinup);

            // nor does an unguided coast read from a hand-edited save
            LogicalPlan bad = Sanitized(Burn(), Coast(StageType.COAST_BEFORE, unguided: true, spinup: true), Burn(), Burn());
            Assert.False(new PlanEditor(bad, rows, MIN_DV).CoastSpinup);
            Assert.False(bad.Stages[1].Spinup);
        }

        [Fact]
        public void SetCoastOnlyTakesTheEditorsSlots()
        {
            List<FuelStats> rows = ThreeStages();
            PlanEditor editor = Editor(rows);

            // no coast after the last burn
            Assert.False(editor.SetCoast(new PlanEditor.CoastSlot(PlanEditor.CoastKind.AFTER, 0)));
            Assert.Null(editor.Coast);

            Assert.True(editor.SetCoast(new PlanEditor.CoastSlot(PlanEditor.CoastKind.AFTER, 1)));
            Assert.Equal("BURN:T COAST_AFTER:T BURN:F BURN:T", Describe(editor.Plan()));
        }
    }
}
