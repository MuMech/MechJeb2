using JetBrains.Annotations;
using MechJebLib.Simulations;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleNewFFSTesting : ComputerModule
    {
        public MechJebModuleNewFFSTesting(MechJebCore core) : base(core)
        {
            Enabled = true;
        }

        protected override void OnModuleEnabled()
        {
        }

        protected override void OnModuleDisabled()
        {
        }

        public override void OnFixedUpdate()
        {
            SimVessel vessel = new Builder().Build(Vessel);
            Debug.Log($"{vessel}");
            vessel.SetConditions(0, 0, 0);
            var sim = new MechJebLib.Simulations.FuelFlowSimulation(vessel);
            sim.Run();
            foreach (FuelStats segment in sim.Segments)
                Debug.Log($"{segment}");
        }

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onStageActivate.Add(HandleStageEvent);
        }

        public override void OnDestroy()
        {
            GameEvents.onStageActivate.Remove(HandleStageEvent);
        }

        private void HandleStageEvent(int data)
        {
        }
    }
}
