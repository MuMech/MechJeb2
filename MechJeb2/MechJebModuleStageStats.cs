using System.Collections.Generic;
using JetBrains.Annotations;
using MechJebLib.Simulations;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleStageStats : ComputerModule
    {
        public CelestialBody editorBody;
        public bool          liveSLT = true;
        public double        altSLT  = 0;
        public double        mach    = 0;
        public bool          oldFFS;

        // FIXME: this has to be broken apart if they're getting updated asynchronously
        public List<FuelStats> atmoStats => _vesselManagerAtmo.Segments;
        public List<FuelStats> vacStats  => _vesselManagerVac.Segments;

        public MechJebModuleStageStats(MechJebCore core) : base(core)
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
            _vesselManagerAtmo.Release();
            _vesselManagerVac.Release();
        }

        private readonly SimVesselManager _vesselManagerAtmo = new SimVesselManager();
        private readonly SimVesselManager _vesselManagerVac  = new SimVesselManager();

        public override void OnFixedUpdate()
        {
            if (oldFFS)
            {
                MechJebModuleOldStageStats oldstats = Core.GetComputerModule<MechJebModuleOldStageStats>();
                oldstats.RequestUpdate(this, true);
                atmoStats.Clear();
                vacStats.Clear();
                foreach (FuelFlowSimulation.FuelStats item in oldstats.vacStats)
                {
                    var newItem = new FuelStats();
                    newItem.StartMass       = item.StartMass;
                    newItem.EndMass         = item.EndMass;
                    newItem.DeltaTime       = item.DeltaTime;
                    newItem.DeltaV          = item.DeltaV;
                    newItem.SpoolUpTime     = item.SpoolUpTime;
                    newItem.Isp             = item.Isp;
                    newItem.StagedMass      = item.StagedMass;
                    newItem.Thrust          = item.EndThrust;
                    newItem.ThrustNoCosLoss = item.EndThrust;
                    vacStats.Add(newItem);
                }

                foreach (FuelFlowSimulation.FuelStats item in oldstats.atmoStats)
                {
                    var newItem = new FuelStats();
                    newItem.StartMass       = item.StartMass;
                    newItem.EndMass         = item.EndMass;
                    newItem.DeltaTime       = item.DeltaTime;
                    newItem.DeltaV          = item.DeltaV;
                    newItem.SpoolUpTime     = item.SpoolUpTime;
                    newItem.Isp             = item.Isp;
                    newItem.StagedMass      = item.StagedMass;
                    newItem.Thrust          = item.EndThrust;
                    newItem.ThrustNoCosLoss = item.EndThrust;
                    atmoStats.Add(newItem);
                }
            }
            else
            {
                StartSimulation();
            }
        }

        private void StartSimulation()
        {
            CelestialBody simBody = HighLogic.LoadedSceneIsEditor ? editorBody : Vessel.mainBody;

            double staticPressureKpa = HighLogic.LoadedSceneIsEditor || !liveSLT
                ? simBody.atmosphere ? simBody.GetPressure(altSLT) : 0
                : Vessel.staticPressurekPa;
            double atmDensity = (HighLogic.LoadedSceneIsEditor || !liveSLT
                ? simBody.GetDensity(simBody.GetPressure(altSLT), simBody.GetTemperature(0))
                : Vessel.atmDensity) / 1.225;
            double mach = HighLogic.LoadedSceneIsEditor ? this.mach : Vessel.mach;


            if (_needsRebuild)
            {
                _vesselManagerAtmo.Build(Vessel);
                _vesselManagerVac.Build(Vessel);
            }
            else
            {
                _vesselManagerAtmo.Update();
                _vesselManagerVac.Update();
            }

            _vesselManagerVac.SetConditions(0, 0, 0);
            _vesselManagerVac.RunFuelFlowSimulation();

            _vesselManagerAtmo.SetConditions(atmDensity, staticPressureKpa * PhysicsGlobals.KpaToAtmospheres, mach);
            _vesselManagerAtmo.RunFuelFlowSimulation();
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

        public void RequestUpdate(object controller)
        {
        }
    }
}
