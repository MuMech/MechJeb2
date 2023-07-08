using System.Collections.Generic;
using JetBrains.Annotations;
using MechJebLib.Simulations;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleStageStats : ComputerModule
    {
        [ToggleInfoItem("#MechJeb_DVincludecosinelosses", InfoItem.Category.Thrust, showInEditor = true)] //ΔV include cosine losses
        public bool DVLinearThrust = true;

        public CelestialBody EditorBody;
        public bool          LiveSLT = true;
        public double        AltSLT  = 0;
        public double        Mach    = 0;
        public bool          OldFFS;

        private int _vabRebuildTimer = 1;

        // FIXME: this has to be broken apart if they're getting updated asynchronously
        public List<FuelStats> AtmoStats => _vesselManagerAtmo.Segments;
        public List<FuelStats> VacStats  => _vesselManagerVac.Segments;

        public MechJebModuleStageStats(MechJebCore core) : base(core)
        {
            Enabled = true;
        }

        private bool _vesselModified;

        protected override void OnModuleEnabled()
        {
            _vesselModified = true;
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
            TryStartSimulation();
        }

        public override void OnUpdate()
        {
        }

        private void RunSimulation()
        {
            CelestialBody simBody = HighLogic.LoadedSceneIsEditor ? EditorBody : Vessel.mainBody;

            double staticPressureKpa = HighLogic.LoadedSceneIsEditor || !LiveSLT
                ? simBody.atmosphere ? simBody.GetPressure(AltSLT) : 0
                : Vessel.staticPressurekPa;
            double atmDensity = (HighLogic.LoadedSceneIsEditor || !LiveSLT
                ? simBody.GetDensity(simBody.GetPressure(AltSLT), simBody.GetTemperature(0))
                : Vessel.atmDensity) / 1.225;
            double mach = HighLogic.LoadedSceneIsEditor ? this.Mach : Vessel.mach;


            if (_vesselModified)
            {
                IShipconstruct v = HighLogic.LoadedSceneIsEditor ? (IShipconstruct)EditorLogic.fetch.ship : Vessel;
                _vesselManagerAtmo.Build(v);
                _vesselManagerVac.Build(v);
            }
            else
            {
                _vesselManagerAtmo.Update();
                _vesselManagerVac.Update();
            }

            _vesselManagerVac.DVLinearThrust = DVLinearThrust;
            _vesselManagerVac.SetConditions(0, 0, 0);
            _vesselManagerVac.RunFuelFlowSimulation();

            _vesselManagerAtmo.DVLinearThrust = DVLinearThrust;
            _vesselManagerAtmo.SetConditions(atmDensity, staticPressureKpa * PhysicsGlobals.KpaToAtmospheres, mach);
            _vesselManagerAtmo.RunFuelFlowSimulation();
        }

        private void StartSimulation()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                if (_vabRebuildTimer > 0)
                {
                    PartSet.BuildPartSets(EditorLogic.fetch.ship.parts, null);
                    _vabRebuildTimer--;
                    _vesselModified = true;
                }
            }
            else
                Vessel.UpdateResourceSetsIfDirty();

            RunSimulation();
        }

        private void TryStartSimulation()
        {
            if ((HighLogic.LoadedSceneIsEditor && EditorBody != null) || Vessel != null)
            {
                StartSimulation();
                Users.Clear();
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onVesselStandardModification.Add(VesselModified);
            GameEvents.StageManager.OnGUIStageSequenceModified.Add(VesselModified);
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(OnEditorShipModified);
                GameEvents.onPartCrossfeedStateChange.Add(OnPartCrossfeedStateChange);
            }
        }

        public override void OnDestroy()
        {
            GameEvents.onVesselStandardModification.Remove(VesselModified);
            GameEvents.StageManager.OnGUIStageSequenceModified.Remove(VesselModified);
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
            GameEvents.onPartCrossfeedStateChange.Remove(OnPartCrossfeedStateChange);
        }

        private void OnPartCrossfeedStateChange(Part data)
        {
            _vabRebuildTimer = 2;
        }

        private void OnEditorShipModified(ShipConstruct data)
        {
            _vabRebuildTimer = 2;
        }

        private void VesselModified()
        {
            _vesselModified = true;
        }

        private void VesselModified(Vessel data)
        {
            VesselModified();
        }

        public void RequestUpdate(object controller)
        {
            if (OldFFS)
            {
                MechJebModuleStageStatsOld oldstats = Core.GetComputerModule<MechJebModuleStageStatsOld>();
                oldstats.editorBody = EditorBody;
                oldstats.liveSLT    = LiveSLT;
                oldstats.altSLT     = AltSLT;
                oldstats.mach       = Mach;
                oldstats.RequestUpdate(this, true);
                AtmoStats.Clear();
                VacStats.Clear();
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
                    VacStats.Add(newItem);
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
                    AtmoStats.Add(newItem);
                }

                return;
            }

            Users.Add(controller);

            // In the editor this is our only entry point
            if (HighLogic.LoadedSceneIsEditor)
                TryStartSimulation();
        }
    }
}
