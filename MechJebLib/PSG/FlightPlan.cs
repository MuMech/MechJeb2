using System.Collections.Generic;
using System.Text;
using MechJebLib.FuelFlowSimulation;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public enum StageType { BURN, COAST_DEDICATED, COAST_BEFORE, COAST_DURING, COAST_AFTER }

    public class FlightPlan
    {
        public readonly List<FlightPlanStage> Stages = new List<FlightPlanStage>();

        private void AddStage(int mjStage, FuelStats vacStat, bool unguided, bool allowShutdown, StageType stageType, int kspStage, bool skipToNextBurn, double atmoISP)
        {
            Stages.Add(new FlightPlanStage(mjStage, vacStat, unguided, allowShutdown, stageType, kspStage, skipToNextBurn, atmoISP));
        }

        public static FlightPlan? FromLogicalPlan(LogicalPlan plan, List<FuelStats> vacStats, List<FuelStats> atmoStats, double minDeltaV)
        {
            var flightPlan = new FlightPlan();

            bool first = true;

            int cursor = StartingCursor(vacStats, minDeltaV, plan.NumBurnRows());

            foreach (LogicalPlanStage stage in plan.Stages)
            {
                if (stage.SkipToNextBurn)
                {
                    if (!first)
                        cursor++;
                    while (cursor < vacStats.Count && vacStats[cursor].DeltaV < minDeltaV )
                        cursor++;
                    first = false;
                }

                if (cursor >= vacStats.Count)
                    return null;

                int kspStage = vacStats[cursor].KSPStage;

                if (stage.StageType == StageType.COAST_DEDICATED)
                {
                    kspStage = stage.KSPStage;
                    while(cursor < vacStats.Count && vacStats[cursor].KSPStage != kspStage)
                        cursor++;
                }

                if (cursor >= vacStats.Count)
                    return null;

                double atmoISP = cursor < atmoStats.Count ? atmoStats[cursor].Isp : -1;

                flightPlan.AddStage(cursor, vacStats[cursor], stage.Unguided, stage.AllowShutdown, stage.StageType, kspStage, stage.SkipToNextBurn, atmoISP);
            }

            return flightPlan;
        }

        private static int StartingCursor(List<FuelStats> vacStats, double minDeltaV, int numBurnRows)
        {
            for (int i = vacStats.Count - 1; i >= 0; i--)
            {
                if (vacStats[i].DeltaV < minDeltaV)
                    continue;
                if (--numBurnRows == 0)
                    return i;
            }

            return 0;
        }

        public bool AllUnguidedStages()
        {
            foreach (FlightPlanStage stage in Stages)
            {
                if (stage.Done)
                    continue;
                if (stage is { StageType: StageType.BURN, Unguided: false })
                    return false;
            }
            return true;
        }

        /// <summary>
        ///     The part persistentIds of the engines of stage index that leave the vessel with it: its planned engines but those a
        ///     later burn burns too (a sustainer, a core fed by drop tanks).  The stages are top-down, so the later burns are the
        ///     stages before it.  Only burns count: a COAST_AFTER coast is on its burn's own row, with the same engines.
        /// </summary>
        public void PartsJettisonedWith(int index, HashSet<uint> into)
        {
            into.Clear();
            foreach (EngineSignature s in Stages[index].PlannedSignatures)
                into.Add(s.PartPersistentID);

            for (int i = 0; i < index; i++)
                if (Stages[i].StageType == StageType.BURN)
                    foreach (EngineSignature s in Stages[i].PlannedSignatures)
                        into.Remove(s.PartPersistentID);
        }

        /// <summary>
        ///     The index of the burn a COAST_AFTER coast follows, which the coast carries (in the burn's KSP stage) to the next
        ///     burn, or -1 without one.  The coast and the burn share a stats row, and the burn is listed right after the coast.
        /// </summary>
        public int CoastAfterBurnIndex()
        {
            for (int i = 0; i < Stages.Count; i++)
                if (Stages[i].StageType == StageType.COAST_AFTER)
                    return i + 1 < Stages.Count && Stages[i + 1].StageType == StageType.BURN ? i + 1 : -1;

            return -1;
        }

        public FlightPlan DeepCopy()
        {
            var dup = new FlightPlan();
            foreach (FlightPlanStage stage in Stages)
                dup.Stages.Add(stage.DeepCopy());
            return dup;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"FlightPlan ({Stages.Count} stages):");
            for (int i = 0; i < Stages.Count; i++)
                sb.AppendLine($"  {i}: {Stages[i]}");
            return sb.ToString();
        }

        // FIXME: probably edge conditions, particularly if an interior stage vanishes
        public void ReconcileStages(List<FuelStats> vacStats, List<FuelStats> atmoStats, double minDeltaV)
        {
            int cursor = 0;
            bool first = true;
            foreach (FlightPlanStage stage in Stages)
            {
                if (stage.Done)
                    continue;

                cursor = ReconcileStage(cursor, stage, vacStats, atmoStats, minDeltaV, first);

                if (cursor >= vacStats.Count)
                    stage.Done = true;

                first = false;
            }
        }

        private static int ReconcileStage(int origCursor, FlightPlanStage stage, List<FuelStats> vacStats, List<FuelStats> atmoStats, double minDeltaV, bool first)
        {
            // COAST_DEDICATED matches on user-specified KSPStage at least for now since there's no EngineSignature to match
            if (stage.StageType == StageType.COAST_DEDICATED)
            {
                // this will deliberate return fuelStats.Count-1 if the stage isn't found since it is assumed that
                // a fairing was released.  marking the coast done happens externally.
                while(origCursor < vacStats.Count-1 && vacStats[origCursor].KSPStage != stage.KSPStage)
                    origCursor++;
                return origCursor;
            }

            int cursor = FindMatchingBurnRow(origCursor, stage, vacStats, minDeltaV, first);

            // if we have a non-Done COAST_AFTER stage with no matching EngineSignature, we assume we're doing that coast now.
            // (COAST_BEFORE/COAST_DURING should have a matching EngineSignature in the future)
            if (cursor >= vacStats.Count && stage.StageType == StageType.COAST_AFTER)
                cursor = vacStats.Count - 1;

            if (cursor >= vacStats.Count)
            {
                stage.Done = true;
                return cursor;
            }

            stage.FuelStats = vacStats[cursor].DeepCopy();
            stage.KSPStage = stage.FuelStats.KSPStage;
            stage.MJStage = cursor;
            stage.AtmoISP = cursor < atmoStats.Count ? atmoStats[cursor].Isp : -1;
            return cursor;
        }

        // Fuzzy match: a non-empty current row that shares at least one engine with the engines the stage was planned with.
        // An engine-out takes engines away from the row and a hot-staged row (the next stage lit while this one still burns)
        // adds some, and either way the stage is still there.  The monotone cursor of ReconcileStages, walking from the top of
        // the rocket down, keeps rows that share engines apart (a sustainer and the boosters burning with it, drop tanks, a
        // vernier tail).  The planned engines never change, so a hot-staged row matched once doesn't widen what matches next.
        private static bool EngineSignaturesMatch(IReadOnlyList<EngineSignature> planned, FuelStats current)
        {
            if (current.EngineSignatures.Count == 0) return false;

            foreach (EngineSignature c in current.EngineSignatures)
                foreach (EngineSignature p in planned)
                    if (c == p)
                        return true;

            return false;
        }

        // This will find the same row repeatedly if it is called with the same desired engine signature
        private static int FindMatchingBurnRow(int cursor, FlightPlanStage stage, List<FuelStats> fuelStats, double minDeltaV, bool first)
        {
            if (stage.SkipToNextBurn && !first)
                cursor++;

            while (cursor < fuelStats.Count && !EngineSignaturesMatch(stage.PlannedSignatures, fuelStats[cursor]))
                cursor++;

            if (cursor >= fuelStats.Count)
                return fuelStats.Count;

            while (cursor < fuelStats.Count && EngineSignaturesMatch(stage.PlannedSignatures, fuelStats[cursor]) && fuelStats[cursor].DeltaV < minDeltaV)
                cursor++;

            if (cursor >= fuelStats.Count || !EngineSignaturesMatch(stage.PlannedSignatures, fuelStats[cursor]))
                return fuelStats.Count;

            return cursor;
        }
    }

    public class FlightPlanStage
    {
        public FuelStats FuelStats;
        public int MJStage;
        public int KSPStage;
        public bool Done;
        public double AtmoISP;

        public readonly bool Unguided;
        public readonly bool AllowShutdown;
        public readonly StageType StageType;
        public readonly bool SkipToNextBurn;
        public          double DeltaV   => StageType == StageType.BURN && !Done ? FuelStats.DeltaV : 0;

        // the engines the stage was planned with, which ReconcileStages matches the stage stats against: FuelStats follows the
        // row it matched, engine-outs included, but this never changes (copies share it)
        public readonly IReadOnlyList<EngineSignature> PlannedSignatures;

        public FlightPlanStage(int mjStage, FuelStats vacStat, bool unguided, bool allowShutdown, StageType stageType, int kspStage, bool skipToNextBurn, double atmoISP)
        {
            PlannedSignatures = new List<EngineSignature>(vacStat.EngineSignatures).AsReadOnly();
            FuelStats = vacStat.DeepCopy();
            KSPStage = kspStage;
            MJStage = mjStage;
            Unguided = unguided;
            AllowShutdown = allowShutdown;
            StageType = stageType;
            SkipToNextBurn = skipToNextBurn;
            AtmoISP = atmoISP;
        }

        public FlightPlanStage DeepCopy()
        {
            var dup = (FlightPlanStage)MemberwiseClone();
            dup.FuelStats = FuelStats.DeepCopy();
            return dup;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{StageType} kspStage: {KSPStage} mjStage: {MJStage}");
            sb.Append($" dV: {DeltaV:F1}");
            if (Unguided)
                sb.Append(" (unguided)");
            sb.Append(!AllowShutdown ? " (no shutdown)" : " (allow shutdown)");
            if (Done)
                sb.Append(" (done)");
            return sb.ToString();
        }
    }
}
