using System.Collections.Generic;
using JetBrains.Annotations;
using MechJebLib.PSG;

namespace MechJebLibBindings.PSG
{
    public class PersistedLogicalPlanStage
    {
        [UsedImplicitly]
        [Persistent]
        public bool AllowShutdown;

        [UsedImplicitly]
        [Persistent]
        public bool Unguided;

        [UsedImplicitly]
        [Persistent]
        public int KSPStage;

        [UsedImplicitly]
        [Persistent]
        public StageType StageType = StageType.BURN;

        // KSP needs a parameterless constructor to create the stages on load
        [UsedImplicitly]
        public PersistedLogicalPlanStage() { }

        public PersistedLogicalPlanStage(LogicalPlanStage stage)
        {
            AllowShutdown = stage.AllowShutdown;
            Unguided      = stage.Unguided;
            KSPStage      = stage.KSPStage;
            StageType     = stage.StageType;
        }

        // SkipToNextBurn is not persisted, LogicalPlan.Sanitize() recomputes it
        public LogicalPlanStage ToLogicalPlanStage() =>
            new LogicalPlanStage { AllowShutdown = AllowShutdown, Unguided = Unguided, KSPStage = KSPStage, StageType = StageType };
    }

    // KSP ConfigNode persistence for the LogicalPlan, which lives in MechJebLib and can't see the KSP attributes.
    public class PersistedLogicalPlan : IPersistenceLoad, IPersistenceSave
    {
        [UsedImplicitly]
        [Persistent(collectionIndex = "STAGE")]
        public List<PersistedLogicalPlanStage> Stages = new List<PersistedLogicalPlanStage>();

        public LogicalPlan Plan { get; private set; } = new LogicalPlan();

        // KSP needs a parameterless constructor to create this on load
        [UsedImplicitly]
        public PersistedLogicalPlan() { }

        public PersistedLogicalPlan(LogicalPlan plan) => Plan = plan;

        public void PersistenceLoad()
        {
            var plan = new LogicalPlan();
            foreach (PersistedLogicalPlanStage stage in Stages)
                plan.Stages.Add(stage.ToLogicalPlanStage());
            plan.Sanitize();
            Plan = plan;
        }

        public void PersistenceSave()
        {
            Stages = new List<PersistedLogicalPlanStage>();
            foreach (LogicalPlanStage stage in Plan.Stages)
                Stages.Add(new PersistedLogicalPlanStage(stage));
        }

        public bool AllFixedBurnTimes() => Plan.AllFixedBurnTimes();

        public override string ToString() => Plan.ToString();
    }
}
