using System.Collections.Generic;
using System.Text;
using static MechJebLib.Utils.Statics;

namespace MechJebLib.PSG
{
    public class LogicalPlanStage
    {
        public bool AllowShutdown;

        public bool Unguided;

        // only on a coast, which must be guided: spin the vessel up during it, at the spin-up rate, before the next stage
        public bool Spinup;

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
            if (Spinup)
                sb.Append(" (spin-up)");
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

            // the spin-up rides on a guided coast only
            foreach (LogicalPlanStage stage in Stages)
                if (stage.StageType == StageType.BURN || stage.Unguided)
                    stage.Spinup = false;
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
    }
}
