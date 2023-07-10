using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using MechJebLib.Simulations;
using Unity.Profiling;

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

        private bool _vesselModified = true;

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
            // check for done.
            // TryStartSimulation();
        }

        public override void OnUpdate()
        {
            // check for done.
        }

        private double _lastRebuild;

        private static ProfilerMarker _newRunSimulationProfile = new ProfilerMarker("RunSimulation");
        private static ProfilerMarker _newBuildProfile         = new ProfilerMarker("Build");
        private static ProfilerMarker _newUpdateProfile        = new ProfilerMarker("Update");
        private static ProfilerMarker _newVacProfile           = new ProfilerMarker("Vac");
        private static ProfilerMarker _newAtmoProfile          = new ProfilerMarker("Atmo");

        private void RunSimulation()
        {
            using ProfilerMarker.AutoScope auto = _newRunSimulationProfile.Auto();

            CelestialBody simBody = HighLogic.LoadedSceneIsEditor ? EditorBody : Vessel.mainBody;

            double staticPressureKpa = HighLogic.LoadedSceneIsEditor || !LiveSLT
                ? simBody.atmosphere ? simBody.GetPressure(AltSLT) : 0
                : Vessel.staticPressurekPa;
            double atmDensity = (HighLogic.LoadedSceneIsEditor || !LiveSLT
                ? simBody.GetDensity(simBody.GetPressure(AltSLT), simBody.GetTemperature(0))
                : Vessel.atmDensity) / 1.225;
            double mach = HighLogic.LoadedSceneIsEditor ? Mach : Vessel.mach;

            if (_vesselModified || Planetarium.GetUniversalTime() - _lastRebuild > 3)
            {
                using ProfilerMarker.AutoScope auto2 = _newBuildProfile.Auto();

                IShipconstruct v = HighLogic.LoadedSceneIsEditor ? (IShipconstruct)EditorLogic.fetch.ship : Vessel;
                _vesselManagerAtmo.Build(v);
                _vesselManagerVac.Build(v);
                _vesselModified = false;
                _lastRebuild    = Planetarium.GetUniversalTime();
            }
            else
            {
                using ProfilerMarker.AutoScope auto2 = _newUpdateProfile.Auto();

                _vesselManagerAtmo.Update();
                _vesselManagerVac.Update();
            }

            using (_newVacProfile.Auto())
            {
                _vesselManagerVac.DVLinearThrust = DVLinearThrust;
                _vesselManagerVac.SetConditions(0, 0, 0);
                _vesselManagerVac.RunFuelFlowSimulation();
            }

            using (_newAtmoProfile.Auto())
            {
                _vesselManagerAtmo.DVLinearThrust = DVLinearThrust;
                _vesselManagerAtmo.SetConditions(atmDensity, staticPressureKpa * PhysicsGlobals.KpaToAtmospheres, mach);
                _vesselManagerAtmo.RunFuelFlowSimulation();
            }
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
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            GameEvents.onVesselStandardModification.Add(onVesselStandardModification);
            GameEvents.StageManager.OnGUIStageSequenceModified.Add(OnGUIStageSequenceModified);
            if (HighLogic.LoadedSceneIsEditor)
            {
                GameEvents.onEditorShipModified.Add(OnEditorShipModified);
                GameEvents.onPartCrossfeedStateChange.Add(OnPartCrossfeedStateChange);
            }
        }

        public override void OnDestroy()
        {
            GameEvents.onVesselStandardModification.Remove(onVesselStandardModification);
            GameEvents.StageManager.OnGUIStageSequenceModified.Remove(OnGUIStageSequenceModified);
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

        private void OnGUIStageSequenceModified()
        {
            _vesselModified = true;
        }

        private void onVesselStandardModification(Vessel data)
        {
            _vesselModified = true;
        }

        private Stopwatch _timer = new Stopwatch();

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
                    newItem.StartMass   = item.StartMass;
                    newItem.EndMass     = item.EndMass;
                    newItem.DeltaTime   = item.DeltaTime;
                    newItem.DeltaV      = item.DeltaV;
                    newItem.SpoolUpTime = item.SpoolUpTime;
                    newItem.Isp         = item.Isp;
                    newItem.StagedMass  = item.StagedMass;
                    newItem.Thrust      = item.EndThrust;
                    VacStats.Add(newItem);
                }

                foreach (FuelFlowSimulation.FuelStats item in oldstats.atmoStats)
                {
                    var newItem = new FuelStats();
                    newItem.StartMass   = item.StartMass;
                    newItem.EndMass     = item.EndMass;
                    newItem.DeltaTime   = item.DeltaTime;
                    newItem.DeltaV      = item.DeltaV;
                    newItem.SpoolUpTime = item.SpoolUpTime;
                    newItem.Isp         = item.Isp;
                    newItem.StagedMass  = item.StagedMass;
                    newItem.Thrust      = item.EndThrust;
                    AtmoStats.Add(newItem);
                }

                return;
            }

            TryStartSimulation();
        }
    }
}
