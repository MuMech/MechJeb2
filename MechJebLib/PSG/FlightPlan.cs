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

        // subset match -- every item in a non-empty current must be in plan
        private static bool EngineSignaturesMatch(FuelStats plan, FuelStats current)
        {
            if (current.EngineSignatures.Count == 0) return false;

            if (current.EngineSignatures.Count > plan.EngineSignatures.Count) return false;

            foreach (EngineSignature c in current.EngineSignatures)
            {
                bool found = false;
                foreach (EngineSignature p in plan.EngineSignatures)
                {
                    if (c == p)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) return false;
            }

            return true;
        }

        // This will find the same row repeatedly if it is called with the same desired engine signature
        private static int FindMatchingBurnRow(int cursor, FlightPlanStage stage, List<FuelStats> fuelStats, double minDeltaV, bool first)
        {
            if (stage.SkipToNextBurn && !first)
                cursor++;

            while (cursor < fuelStats.Count && !EngineSignaturesMatch(stage.FuelStats, fuelStats[cursor]))
                cursor++;

            if (cursor >= fuelStats.Count)
                return fuelStats.Count;

            while (cursor < fuelStats.Count && EngineSignaturesMatch(stage.FuelStats, fuelStats[cursor]) && fuelStats[cursor].DeltaV < minDeltaV)
                cursor++;

            if (cursor >= fuelStats.Count || !EngineSignaturesMatch(stage.FuelStats, fuelStats[cursor]))
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

        public FlightPlanStage(int mjStage, FuelStats vacStat, bool unguided, bool allowShutdown, StageType stageType, int kspStage, bool skipToNextBurn, double atmoISP)
        {
            FuelStats = vacStat.DeepCopy();
            KSPStage = kspStage;
            MJStage = mjStage;
            Unguided = unguided;
            AllowShutdown = allowShutdown;
            StageType = stageType;
            SkipToNextBurn = skipToNextBurn;
            AtmoISP = atmoISP;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"{StageType} kspStage: {KSPStage} mjStage: {MJStage}");
            sb.Append($" dV: {DeltaV:F1}");
            if (Unguided)
                sb.Append(" (unguided)");
            sb.Append(!AllowShutdown ? " (no shutdown)" : " (allow shutdown)");
            return sb.ToString();
        }
    }
}
