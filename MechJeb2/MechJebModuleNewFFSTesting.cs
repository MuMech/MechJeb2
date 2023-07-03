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

        private bool _needsRebuild;

        protected override void OnModuleEnabled()
        {
            _needsRebuild = true;
        }

        protected override void OnModuleDisabled()
        {
            _vesselManager.Release();
        }

        private readonly SimVesselManager _vesselManager = new SimVesselManager();

        public override void OnFixedUpdate()
        {
            if (_needsRebuild)
                _vesselManager.Build(Vessel);
            else
                _vesselManager.Update();
            _vesselManager.SetConditions(0, 0, 0);
            _vesselManager.RunFuelFlowSimulation();
            List<FuelStats> segments = _vesselManager.GetFuelFlowResults();
            foreach (FuelStats segment in segments)
                Debug.Log($"{segment}");
        }

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onVesselStandardModification.Add(RebuildOnModification);
            GameEvents.StageManager.OnGUIStageSequenceModified.Add(Rebuild);
        }

        public override void OnDestroy()
        {
            GameEvents.onVesselStandardModification.Remove(RebuildOnModification);
            GameEvents.StageManager.OnGUIStageSequenceModified.Remove(Rebuild);
        }

        private void Rebuild()
        {
            _needsRebuild = true;
        }

        private void RebuildOnModification(Vessel data)
        {
            Rebuild();
        }
    }
}
