/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;

namespace MechJebLib.PSG
{
    /// <summary>
    ///     A LogicalPlan as the stage editor shows and edits it: the vessel's stage stats as rows in flight order, the burn rows
    ///     the plan covers with their Guided and Full burn flags, and the plan's one coast as a slot among the places a coast
    ///     can go.  Every edit rebuilds the LogicalPlan from that description (Plan), so the plan stays valid by construction.
    ///     The plan's burns cover the bottom-most burn rows (VacStats rows with at least MinDeltaV), as FlightPlan maps them:
    ///     "the stages above this one are left out" is the number of burn rows planned.
    /// </summary>
    public sealed class PlanEditor
    {
        public enum RowKind
        {
            // at least MinDeltaV: a burn the plan can use
            BURN,

            // engines, but under MinDeltaV: the plan skips it
            UNDER_MIN,

            // no engines (a decoupler or fairing stage): a place for a dedicated coast
            NO_ENGINE
        }

        public enum CoastKind { BEFORE, DURING, AFTER, DEDICATED }

        public sealed class Row
        {
            public FuelStats Stats = null!;

            // the row's index in VacStats (0 is the top of the rocket)
            public int VacIndex;

            public RowKind Kind;

            // for a BURN row: its position among the burn rows in flight order (0 is the first burn), -1 otherwise
            public int BurnIndex = -1;

            // a burn row the plan covers
            public bool Planned;

            public bool Guided = true;

            // fixed burn time: the whole stage burns (no optimized shutdown)
            public bool FullBurn;

            public int KSPStage => Stats.KSPStage;
        }

        /// <summary>A place for the coast: before, during or after a planned burn row, or at an engine-less row.</summary>
        public readonly struct CoastSlot
        {
            public readonly CoastKind Kind;

            // the VacStats index of the burn row (BEFORE, DURING, AFTER) or of the engine-less row (DEDICATED)
            public readonly int VacIndex;

            public CoastSlot(CoastKind kind, int vacIndex)
            {
                Kind = kind;
                VacIndex = vacIndex;
            }

            public bool Equals(CoastSlot o) => Kind == o.Kind && VacIndex == o.VacIndex;

            public override string ToString() => $"{Kind} {VacIndex}";
        }

        // every VacStats row, in flight order (the first stage to fly first)
        public readonly List<Row> Rows = new List<Row>();

        // the BURN rows, in flight order
        public readonly List<Row> BurnRows = new List<Row>();

        // how many burn rows the plan covers, counted from the first burn
        public int PlannedCount { get; private set; }

        public CoastSlot? Coast { get; private set; }

        public bool CoastGuided { get; private set; } = true;

        // false when the LogicalPlan it was read from can't be mapped onto these stage stats (the vessel changed)
        public bool Fits { get; private set; } = true;

        private readonly List<CoastSlot> _slots = new List<CoastSlot>();

        public PlanEditor(LogicalPlan plan, List<FuelStats> vacStats, double minDeltaV)
        {
            for (int i = vacStats.Count - 1; i >= 0; i--)
            {
                FuelStats s = vacStats[i];
                var row = new Row { Stats = s, VacIndex = i };
                if (s.DeltaV >= minDeltaV)
                {
                    row.Kind = RowKind.BURN;
                    row.BurnIndex = BurnRows.Count;
                    BurnRows.Add(row);
                }
                else
                    row.Kind = s.EngineSignatures.Count > 0 ? RowKind.UNDER_MIN : RowKind.NO_ENGINE;

                Rows.Add(row);
            }

            Read(plan, vacStats, minDeltaV);
            BuildSlots();
        }

        // the description of plan: which burn rows, their flags and the coast, from FlightPlan's mapping of it
        private void Read(LogicalPlan plan, List<FuelStats> vacStats, double minDeltaV)
        {
            if (plan.Stages.Count == 0)
            {
                // nothing planned yet: every burn row, all optimized
                PlannedCount = BurnRows.Count;
                MarkPlanned();
                return;
            }

            FlightPlan? mapped = FlightPlan.FromLogicalPlan(plan, vacStats, vacStats, minDeltaV);
            if (mapped == null || mapped.Stages.Count != plan.Stages.Count)
            {
                Fits = false;
                PlannedCount = BurnRows.Count;
                MarkPlanned();
                return;
            }

            int planned = 0;
            for (int i = 0; i < plan.Stages.Count; i++)
            {
                LogicalPlanStage stage = plan.Stages[i];
                FlightPlanStage at = mapped.Stages[i];
                if (stage.StageType == StageType.BURN)
                {
                    Row? row = BurnRowAt(at.MJStage);
                    if (row == null)
                    {
                        Fits = false;
                        continue;
                    }

                    if (!row.Planned) planned++;
                    row.Planned = true;
                    row.Guided = !stage.Unguided;
                    row.FullBurn = !stage.AllowShutdown;
                    continue;
                }

                Coast = new CoastSlot(KindOf(stage.StageType), at.MJStage);
                CoastGuided = !stage.Unguided;
            }

            PlannedCount = planned;
        }

        private void MarkPlanned()
        {
            for (int i = 0; i < BurnRows.Count; i++)
                BurnRows[i].Planned = i < PlannedCount;
        }

        private Row? BurnRowAt(int vacIndex)
        {
            foreach (Row r in BurnRows)
                if (r.VacIndex == vacIndex)
                    return r;
            return null;
        }

        private static CoastKind KindOf(StageType type) =>
            type switch
            {
                StageType.COAST_BEFORE => CoastKind.BEFORE,
                StageType.COAST_DURING => CoastKind.DURING,
                StageType.COAST_AFTER  => CoastKind.AFTER,
                _                      => CoastKind.DEDICATED
            };

        // the places a coast can go, in flight order: before, during and after each planned burn row (but not before the first
        // burn or after the last), and at each engine-less row between two planned burns
        private void BuildSlots()
        {
            _slots.Clear();
            for (int i = 0; i < PlannedCount; i++)
            {
                Row r = BurnRows[i];
                if (i > 0) _slots.Add(new CoastSlot(CoastKind.BEFORE, r.VacIndex));
                _slots.Add(new CoastSlot(CoastKind.DURING, r.VacIndex));
                if (i == PlannedCount - 1) break;
                _slots.Add(new CoastSlot(CoastKind.AFTER, r.VacIndex));

                // engine-less rows between this burn and the next, in flight order (VacStats indices run the other way)
                Row next = BurnRows[i + 1];
                foreach (Row x in Rows)
                    if (x.Kind == RowKind.NO_ENGINE && x.VacIndex < r.VacIndex && x.VacIndex > next.VacIndex)
                        _slots.Add(new CoastSlot(CoastKind.DEDICATED, x.VacIndex));
            }

            if (Coast.HasValue && SlotIndex(Coast.Value) < 0)
                Coast = null;
        }

        public IReadOnlyList<CoastSlot> Slots => _slots;

        public int SlotIndex(CoastSlot slot)
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].Equals(slot))
                    return i;
            return -1;
        }

        // ---- edits ----

        /// <summary>Plans the burn rows up to and including burn row index n (in flight order): the rows above it are left out.</summary>
        public void EndAt(int burnIndex)
        {
            PlannedCount = System.Math.Max(1, System.Math.Min(burnIndex + 1, BurnRows.Count));
            MarkPlanned();
            BuildSlots();
            Fits = true;
        }

        public void SetGuided(int burnIndex, bool guided) => BurnRows[burnIndex].Guided = guided;

        public void SetFullBurn(int burnIndex, bool fullBurn) => BurnRows[burnIndex].FullBurn = fullBurn;

        /// <summary>
        ///     Adds the coast where it is most often wanted: before the last planned burn (coast to apoapsis, then circularize),
        ///     or during the only burn.
        /// </summary>
        public void AddCoast()
        {
            if (_slots.Count == 0) return;
            Row last = BurnRows[PlannedCount - 1];
            var before = new CoastSlot(CoastKind.BEFORE, last.VacIndex);
            Coast = SlotIndex(before) >= 0 ? before : _slots[_slots.Count - 1];
            CoastGuided = true;
        }

        public void RemoveCoast() => Coast = null;

        /// <summary>Moves the coast to the previous (-1, earlier in the flight) or next (+1) slot. False at the ends.</summary>
        public bool MoveCoast(int direction)
        {
            if (!Coast.HasValue) return false;
            int i = SlotIndex(Coast.Value) + direction;
            if (i < 0 || i >= _slots.Count) return false;
            Coast = _slots[i];
            return true;
        }

        public void SetCoastGuided(bool guided) => CoastGuided = guided;

        /// <summary>The LogicalPlan this description makes, top-down like VacStats, sanitized.</summary>
        public LogicalPlan Plan()
        {
            var plan = new LogicalPlan();
            CoastSlot? coast = Coast;

            // top-down: the last planned burn first
            for (int i = PlannedCount - 1; i >= 0; i--)
            {
                Row r = BurnRows[i];

                if (coast is { Kind: CoastKind.AFTER } a && a.VacIndex == r.VacIndex) plan.Stages.Add(CoastStage(StageType.COAST_AFTER, r));

                if (coast is { Kind: CoastKind.DURING } du && du.VacIndex == r.VacIndex)
                {
                    plan.Stages.Add(BurnStage(r));
                    plan.Stages.Add(CoastStage(StageType.COAST_DURING, r));
                }

                plan.Stages.Add(BurnStage(r));

                if (coast is { Kind: CoastKind.BEFORE } b && b.VacIndex == r.VacIndex) plan.Stages.Add(CoastStage(StageType.COAST_BEFORE, r));

                // a dedicated coast between this burn and the one flown before it goes under this burn (top-down)
                if (coast is { Kind: CoastKind.DEDICATED } dc && i > 0 && dc.VacIndex < BurnRows[i - 1].VacIndex && dc.VacIndex > r.VacIndex)
                {
                    LogicalPlanStage ded = CoastStage(StageType.COAST_DEDICATED, r);
                    ded.KSPStage = RowAtVac(dc.VacIndex)?.KSPStage ?? r.KSPStage;
                    plan.Stages.Add(ded);
                }
            }

            plan.Sanitize();
            return plan;
        }

        private Row? RowAtVac(int vacIndex)
        {
            foreach (Row r in Rows)
                if (r.VacIndex == vacIndex)
                    return r;
            return null;
        }

        private static LogicalPlanStage BurnStage(Row r) =>
            new LogicalPlanStage { StageType = StageType.BURN, Unguided = !r.Guided, AllowShutdown = !r.FullBurn };

        private LogicalPlanStage CoastStage(StageType type, Row r) =>
            new LogicalPlanStage { StageType = type, Unguided = !CoastGuided, AllowShutdown = !r.FullBurn };
    }
}
