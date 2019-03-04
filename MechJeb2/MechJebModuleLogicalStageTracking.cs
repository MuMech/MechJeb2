using UnityEngine;
using KSP.UI.Screens;
using System.Collections.Generic;

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
        public MechJebModuleLogicalStageTracking(MechJebCore core) : base(core) { }

        public List<Stage> stages = new List<Stage>();

        // this is used to track how many significant stages we've dropped
        public int stageCount = 0;

        double lastTime = 0;

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onStageActivate.Add(handleStageEvent);
        }

        public override void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(handleStageEvent);
        }

        public override void OnModuleEnabled()
        {
            Reset();
        }

        private void handleStageEvent(int data)
        {
            if ( !enabled || stages.Count == 0 )
                return;

            while ( stages[0].ksp_stage > ( vessel.currentStage - 1 ) )
            {
                // we did drop a relevant stage
                stageCount += 1;
                stages[0].staged = true;
                stages.RemoveAt(0);
                Debug.Log("[MechJebModuleLogicalStageTracking] dropping a stage");
            }
        }

        public void Reset()
        {
            stages.Clear();
        }

        public void Update()
        {
            if ( lastTime == Planetarium.GetUniversalTime() )
                return;

            core.stageStats.RequestUpdate(this, true);

            int j = 0;

            for ( int i = core.stageStats.vacStats.Length - 1; i >= 0; i-- )
            {
                var stats = core.stageStats.vacStats[i];

                // FIXME: either tweakability or identify ullage + sep motors correctly
                if ( stats.deltaV < 20 )
                {
                    if ( !(j == 0 && stages.Count > 0 && stages[0].ksp_stage == i) ) // check if we're just burning down the current stage
                        continue;
                }

                if (j >= stages.Count)
                {
                    Debug.Log("[MechJebModuleLogicalStageTracking] adding a new stage: " + j);
                    stages.Add(new Stage(this));
                }

                stages[j].ksp_stage = i;
                stages[j].rocket_stage = j + stageCount;
                stages[j].Sync();

                j++;
            }

            while( stages.Count > core.stageStats.vacStats.Length )
            {
                Debug.Log("[MechJebModuleLogicalStageTracking] upper stage disappeared (user reconfig most likely)");
                stages.RemoveAt(stages.Count-1);
            }

            lastTime = Planetarium.GetUniversalTime();
        }

        public class Stage
        {
            private MechJebModuleLogicalStageTracking parent;

            // true if the this was jettisoned normally
            public bool staged = false;

            // ksp stage from StageStats
            public int ksp_stage;
            // ∆v of the stage in m/s
            public double deltaV;
            // burntime left in the stage in secs
            public double deltaTime;
            // effective isp of the stage (this is derived from the total mass loss and the total ∆v)
            public double isp;
            // effective exhaust velocity of the stage
            public double v_e { get { return isp * 9.80665; } }
            // starting thrust of the stage
            public double startThrust;
            // effective thrust (the "average thrust" derived from the total mdot and the v_e)
            public double effectiveThrust { get { return v_e * ( startMass - endMass ) / deltaTime; } }
            // starting mass of the stage
            public double startMass;
            // ending mass of the stage
            public double endMass;
            // starting acceleration of the stage
            public double a0 { get { return startThrust / startMass; } }
            // ideal time to consume the rocket completely
            public double tau { get { return v_e / a0; } }

            // the stage of the rocket (0-indexed, and should track across staging events)
            public int rocket_stage = 0;

            // the last parts list
            public List<Part> parts = new List<Part>();

            private FuelFlowSimulation.Stats vacStats;

            public void Sync()
            {
                if (ksp_stage > parent.core.stageStats.vacStats.Length - 1)
                    return;

                vacStats = parent.core.stageStats.vacStats[ksp_stage];
                deltaV = vacStats.deltaV;
                deltaTime = vacStats.deltaTime;
                isp = vacStats.isp;
                startThrust = vacStats.startThrust * 1000;
                startMass = vacStats.startMass * 1000;
                endMass = vacStats.endMass * 1000;

                parts.Clear();
                for(int i = 0; i < vacStats.parts.Count; i++)
                {
                    parts.Add(vacStats.parts[i]);
                }
            }

            public override string ToString()
            {
                return "ksp_stage: "+ ksp_stage + " rocket_stage: " + rocket_stage + " isp:" + isp + " thrust:" + startThrust + " m0: " + startMass + " maxt:" + deltaTime;
            }

            public Stage(MechJebModuleLogicalStageTracking parent)
            {
                this.parent = parent;
            }
        }
    }
}
