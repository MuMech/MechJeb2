using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MuMech
{
    // Tracking actual concrete rocket stages (like what the "main" stage is or the
    // "booster" or "upper" stage) is really fucking annoying in KSP, but vital to do
    // any kind of significant trajectory optimization.  The KSP stage list might have
    // 15 stages for a 3 stage rocket, only 3 of which are "significant", while the other
    // ones are just ullage, decouplers, fairing sep, launch tower sep, etc.  And then
    // button mashing game players will expect to be able to rearrange their staging as their
    // rocket is ascending and just have MJ all magically figure shit out on the fly and not
    // give up and drive their rocket into the ground in protest.
    //
    // The point of this class is so that consumers (mostly PVG) can get a list of significant stages
    // that ignores most of the pointless ones out of StageStats, but also TRACKS the significant
    // stages.  The List<Stage> stages is designed such that PVG can hold onto one of the
    // elements and it will update its information correctly (it becomes an intermediary between
    // MJs StageStats and the guidance controler).  Consumers need to check the "staged" flag to
    // determine if the stage has gone away.
    //
    public class MechJebModuleLogicalStageTracking : ComputerModule
    {
        public class StageContainer : List<Stage>
        {
            public override string ToString()
            {
                var sb = new StringBuilder();
                foreach (Stage stage in this) sb.AppendLine(stage.ToString());

                return sb.ToString();
            }
        }

        public MechJebModuleLogicalStageTracking(MechJebCore core) : base(core)
        {
        }

        public readonly StageContainer Stages = new StageContainer();

        // this is used to track how many significant stages we've dropped
        private int _stageCount;

        public override void OnModuleEnabled()
        {
            Reset();
        }

        public void Reset()
        {
            Stages.Clear();
            _lastNonZeroStages = -1;
        }

        private int _lastNonZeroStages = -1;

        // FIXME: this is no longer worth being a computer module, its just a sub-part of the GuidanceController
        public void Update()
        {
            core.stageStats.RequestUpdate(this, true);

            /*
             * Handle staging events: if we just staged and have one less nonzero-stage then remove a stage
             */

            int currentNonZeroStages = 0;

            for (int i = core.stageStats.vacStats.Length - 1; i >= 0; i--)
            {
                FuelFlowSimulation.FuelStats fuelStats = core.stageStats.vacStats[i];
                if (fuelStats.DeltaV <= 0)
                    continue;

                currentNonZeroStages++;
            }

            if (currentNonZeroStages < _lastNonZeroStages)
            {
                _stageCount      += 1;
                Stages[0].Staged =  true;
                Stages.RemoveAt(0);
                Debug.Log("[MechJebModuleLogicalStageTracking] dropping a stage");
            }

            /*
             * Deal with resynchronization and with user reconfiguration.
             */

            int j = 0;

            for (int i = core.stageStats.vacStats.Length - 1; i >= 0; i--)
            {
                FuelFlowSimulation.FuelStats fuelStats = core.stageStats.vacStats[i];

                if (fuelStats.DeltaV <= 0)
                    continue;

                if (j >= Stages.Count)
                {
                    Debug.Log("[MechJebModuleLogicalStageTracking] adding a new stage: " + j);
                    Stages.Add(new Stage(this));
                }

                Stages[j].KspStage    = i;
                Stages[j].RocketStage = j + _stageCount;
                Stages[j].Sync();

                j++;
            }

            while (Stages.Count > core.stageStats.vacStats.Length)
            {
                Debug.Log("[MechJebModuleLogicalStageTracking] upper stage disappeared (user reconfig most likely)");
                Stages.RemoveAt(Stages.Count - 1);
            }

            _lastNonZeroStages = currentNonZeroStages;
        }

        public class Stage
        {
            private readonly MechJebModuleLogicalStageTracking _parent;

            // true if the this was jettisoned normally
            public bool Staged;

            // ksp stage from StageStats
            public int KspStage;

            // ∆v of the stage in m/s
            public double DeltaV;

            // burntime left in the stage in secs
            public double DeltaTime;

            // effective isp of the stage (this is derived from the total mass loss and the total ∆v)
            public double Isp;

            // effective exhaust velocity of the stage
            public double Ve => Isp * 9.80665;

            // starting thrust of the stage
            public double StartThrust;

            // ending thrust (this should be the same, except for burned out ullage motors)
            public double EndThrust;

            // effective thrust (the "average thrust" derived from the total mdot and the v_e)
            public double EffectiveThrust => Ve * (StartMass - EndMass) / DeltaTime;

            // starting mass of the stage
            public double StartMass;

            // ending mass of the stage
            public double EndMass;

            // starting acceleration of the stage (use EndThrust to avoid counting ullage motors)
            private double A0 => EndThrust / StartMass;

            // ideal time to consume the rocket completely
            public double Tau => Ve / A0;

            // the stage of the rocket (0-indexed, and should track across staging events)
            public int RocketStage;

            // the last parts list
            private readonly List<Part> _parts = new List<Part>();

            private FuelFlowSimulation.FuelStats _vacFuelStats;

            public void Sync()
            {
                if (KspStage > _parent.core.stageStats.vacStats.Length - 1)
                    return;

                _vacFuelStats = _parent.core.stageStats.vacStats[KspStage];
                DeltaV        = _vacFuelStats.DeltaV;
                DeltaTime     = _vacFuelStats.DeltaTime;
                Isp           = _vacFuelStats.Isp;
                StartThrust   = _vacFuelStats.StartThrust * 1000;
                EndThrust     = _vacFuelStats.EndThrust * 1000;
                StartMass     = _vacFuelStats.StartMass * 1000;
                EndMass       = _vacFuelStats.EndMass * 1000;

                _parts.Clear();
                for (int i = 0; i < _vacFuelStats.Parts.Count; i++) _parts.Add(_vacFuelStats.Parts[i]);
            }

            public override string ToString()
            {
                return "ksp_stage: " + KspStage + " rocket_stage: " + RocketStage + " isp:" + Isp + " thrust:" + StartThrust + " m0: " + StartMass +
                       " maxt:" + DeltaTime;
            }

            public Stage(MechJebModuleLogicalStageTracking parent)
            {
                _parent = parent;
            }
        }
    }
}
