using System.Collections.Generic;
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

        private readonly SimVesselManager _vesselManager = new SimVesselManager();

        public override void OnFixedUpdate()
        {
            _vesselManager.Build(Vessel);
            _vesselManager.Update();
            _vesselManager.SetConditions(0, 0, 0);
            _vesselManager.RunFuelFlowSimulation();
            List<FuelStats> segments = _vesselManager.GetFuelFlowResults();
            foreach (FuelStats segment in segments)
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
