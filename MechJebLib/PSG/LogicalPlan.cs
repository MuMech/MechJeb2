using System.Collections.Generic;
using System.Text;
using MechJebLib.FuelFlowSimulation;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public class LogicalPlanStage
    {
        public bool AllowShutdown;

        public bool Unguided;

        // This leaks KSPStage information into the logical plan and is only valid for COAST_DEDICATED
        public int KSPStage;

        // internal state
        public bool SkipToNextBurn = true;

        public StageType StageType = StageType.BURN;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append(StageType);
            if (StageType == StageType.COAST_DEDICATED)
                sb.Append($" kspStage: {KSPStage}");
            if (Unguided)
                sb.Append(" (unguided)");
            sb.Append(!AllowShutdown ? " (no shutdown)" : " (allow shutdown)");
            if (!SkipToNextBurn)
                sb.Append(" (no advance)");
            return sb.ToString();
        }
    }

    public class LogicalPlan
    {
        public List<LogicalPlanStage> Stages = new List<LogicalPlanStage>();

        // FIXME: add more sanity-checking on loading from config: may be loaded from a different rocket or hand-edited
        public void Sanitize()
        {
            // configuration is invalid if there is more than one coast, so reset it
            if (NumCoasts() > 1)
            {
                Print($"[MechJebLib.LogicalPlan.Sanitize] invalid number of coasts in loaded LogicalPlan: {NumCoasts()}");
                Stages.Clear();
                return;
            }

            RemoveInvalidCoasts();
            ComputeSkipToNextBurn();
        }

        private void RemoveInvalidCoasts()
        {
            if (Stages.Count > 0 && Stages[0].StageType != StageType.BURN)
                Stages.RemoveAt(0);

            if (Stages.Count > 0 && Stages[Stages.Count - 1].StageType != StageType.BURN)
                Stages.RemoveAt(Stages.Count - 1);
        }

        private void ComputeSkipToNextBurn()
        {
            bool nextSkipToNextBurn = true;

            foreach (LogicalPlanStage stage in Stages)
            {
                if (stage.StageType != StageType.BURN)
                {
                    stage.SkipToNextBurn = stage.StageType == StageType.COAST_AFTER;
                    if (stage.StageType == StageType.COAST_AFTER || stage.StageType == StageType.COAST_DURING)
                        nextSkipToNextBurn = false;
                    continue;
                }

                stage.SkipToNextBurn = nextSkipToNextBurn;
                nextSkipToNextBurn = true;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"LogicalPlan ({Stages.Count} stages):");
            for (int i = 0; i < Stages.Count; i++)
                sb.AppendLine($"  {i}: {Stages[i]}");
            return sb.ToString();
        }

        public bool AllFixedBurnTimes()
        {
            foreach (LogicalPlanStage stage in Stages)
                if (stage.StageType == StageType.BURN && stage.AllowShutdown)
                    return false;
            return true;
        }

        private int NumCoasts()
        {
            int num = 0;
            foreach (LogicalPlanStage stage in Stages)
                if (stage.StageType != StageType.BURN)
                    num++;
            return num;
        }

        // NOTE: careful about COAST_DURING and one burn row getting split into two logical burns
        internal int NumBurnRows()
        {
            int num = 0;
            foreach (LogicalPlanStage stage in Stages)
                if (stage.SkipToNextBurn)
                    num++;
            return num;
        }

        // This is to be deprecated, the LogicalPlan should become the thing the UI/UX generates directly, this is just
        // a back-compatibility layer for now to convert all the old settings.
        // TODO in UI/UX overhaul: if you have one KSPstage with two MJstages that are both > minDeltaV you need to be able to
        // target a coast to BEFORE/AFTER/DURING either of the MJstages (more evidence KSP stage #'s are the wrong user-facing API)
        public static LogicalPlan GenerateLogicalPlan(List<FuelStats> vacStats, List<int> unguidedStages, List<int> fixedStages, int lastStage, double minDeltaV, int coastStage, int coastLocation)
        {
            var plan = new LogicalPlan();

            if (vacStats.Count > 0 && lastStage <= vacStats[vacStats.Count - 1].KSPStage)
            {
                lastStage = Clamp(lastStage, 0,  vacStats[vacStats.Count - 1].KSPStage);

                bool addedCoast = false;

                // we're counting down the rocket from the top
                foreach (FuelStats stats in vacStats)
                {
                    int kspStage = stats.KSPStage;

                    // skip until we find the configured "top"
                    if (stats.KSPStage < lastStage)
                        continue;

                    // this catches dedicated coast phases
                    if (coastStage >= 0 && stats.KSPStage > coastStage && !addedCoast)
                    {
                        var stage2 = new LogicalPlanStage();
                        stage2.StageType = StageType.COAST_DEDICATED;
                        stage2.Unguided = unguidedStages.Contains(stats.KSPStage);
                        stage2.AllowShutdown = !fixedStages.Contains(stats.KSPStage);
                        stage2.KSPStage = coastStage;
                        plan.Stages.Add(stage2);
                        addedCoast  = true;
                    }

                    // skip all the tiny stages
                    if (stats.DeltaV < minDeltaV)
                        continue;

                    bool doingCoastAfter = coastStage == kspStage && coastLocation == 1;
                    bool doingCoastDuring = coastStage == kspStage && coastLocation == 0;
                    bool doingCoastBefore = coastStage == kspStage && coastLocation == -1;

                    if (doingCoastAfter && !addedCoast)
                    {
                        var stage2 = new LogicalPlanStage();
                        stage2.StageType = StageType.COAST_AFTER;
                        stage2.Unguided = unguidedStages.Contains(stats.KSPStage);
                        stage2.AllowShutdown = !fixedStages.Contains(stats.KSPStage);
                        plan.Stages.Add(stage2);
                        addedCoast  = true;
                    }

                    if (doingCoastDuring && !addedCoast)
                    {
                        var stage3 = new LogicalPlanStage();
                        stage3.StageType = StageType.BURN;
                        stage3.Unguided = unguidedStages.Contains(stats.KSPStage);
                        stage3.AllowShutdown = !fixedStages.Contains(stats.KSPStage);
                        plan.Stages.Add(stage3);

                        var stage2 = new LogicalPlanStage();
                        stage2.StageType = StageType.COAST_DURING;
                        stage2.Unguided = unguidedStages.Contains(stats.KSPStage);
                        stage2.AllowShutdown = !fixedStages.Contains(stats.KSPStage);
                        plan.Stages.Add(stage2);
                        addedCoast  = true;
                    }

                    var stage = new LogicalPlanStage();
                    stage.StageType = StageType.BURN;
                    stage.Unguided = unguidedStages.Contains(stats.KSPStage);
                    stage.AllowShutdown = !fixedStages.Contains(stats.KSPStage);
                    plan.Stages.Add(stage);

                    if (doingCoastBefore && !addedCoast)
                    {
                        var stage2 = new LogicalPlanStage();
                        stage2.StageType = StageType.COAST_BEFORE;
                        stage2.Unguided = unguidedStages.Contains(stats.KSPStage);
                        stage2.AllowShutdown = !fixedStages.Contains(stats.KSPStage);
                        plan.Stages.Add(stage2);
                        addedCoast  = true;
                    }
                }
            }

            plan.RemoveInvalidCoasts();

            plan.ComputeSkipToNextBurn();

            return plan;
        }
    }
}
