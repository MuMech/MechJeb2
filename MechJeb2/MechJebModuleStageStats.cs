using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using MechJebLib.Primitives;
using MechJebLib.Simulations;
using Unity.Profiling;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleStageStats : ComputerModule
    {
        [ToggleInfoItem("#MechJeb_DVincludecosinelosses", InfoItem.Category.Thrust, showInEditor = true)] //ΔV include cosine losses
        public readonly bool DVLinearThrust = true;

        public CelestialBody EditorBody;
        public bool          LiveSLT = true;
        public double        AltSLT  = 0;
        public double        Mach    = 0;

        private int _vabRebuildTimer = 1;

        public readonly List<FuelStats> AtmoStats = new List<FuelStats>();
        public readonly List<FuelStats> VacStats  = new List<FuelStats>();
        public          double          AtmoT, VacT;
        public          V3              AtmoR, VacR;
        public          V3              AtmoV, VacV;
        public          V3              AtmoU, VacU;

        public MechJebModuleStageStats(MechJebCore core) : base(core)
        {
            Enabled = true;
        }

        private bool _vesselModified = true;

        protected override void OnModuleEnabled() => _vesselModified = true;

        protected override void OnModuleDisabled()
        {
            _vesselManagerAtmo.Release();
            _vesselManagerVac.Release();
        }

        private readonly SimVesselManager _vesselManagerAtmo = new SimVesselManager();
        private readonly SimVesselManager _vesselManagerVac  = new SimVesselManager();

        public override void OnFixedUpdate() => GetResults();

        public override void OnUpdate() => GetResults();

        private static ProfilerMarker _newRunSimulationProfile = new ProfilerMarker("RunSimulation");
        private static ProfilerMarker _newBuildProfile         = new ProfilerMarker("Build");
        private static ProfilerMarker _newUpdateProfile        = new ProfilerMarker("Update");
        private static ProfilerMarker _newVacProfile           = new ProfilerMarker("Vac");
        private static ProfilerMarker _newAtmoProfile          = new ProfilerMarker("Atmo");

        private void GetResults()
        {
            if (_vesselManagerAtmo.FuelFlowSimulation.ResultReady)
            {
                AtmoStats.Clear();
                foreach (FuelStats item in _vesselManagerAtmo.FuelFlowSimulation.Segments)
                    AtmoStats.Add(item);
                AtmoT = _vesselManagerAtmo.T;
                AtmoR = _vesselManagerAtmo.R;
                AtmoV = _vesselManagerAtmo.V;
                AtmoU = _vesselManagerAtmo.U;

                _vesselManagerAtmo.FuelFlowSimulation.ResultReady = false;
            }

            if (_vesselManagerVac.FuelFlowSimulation.ResultReady)
            {
                VacStats.Clear();
                foreach (FuelStats item in _vesselManagerVac.FuelFlowSimulation.Segments)
                    VacStats.Add(item);
                VacT = _vesselManagerVac.T;
                VacR = _vesselManagerVac.R;
                VacV = _vesselManagerVac.V;
                VacU = _vesselManagerVac.U;

                _vesselManagerVac.FuelFlowSimulation.ResultReady = false;
            }
        }

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

            // XXX: we do a rebuild every time in the editor because apparently I don't know the right callbacks/magic to
            // make rebuilding only on reconfiguration work.
            if (_vesselModified || HighLogic.LoadedSceneIsEditor)
            {
                using ProfilerMarker.AutoScope auto2 = _newBuildProfile.Auto();

                IShipconstruct v = HighLogic.LoadedSceneIsEditor ? (IShipconstruct)EditorLogic.fetch.ship : Vessel;
                _vesselManagerAtmo.Build(v);
                _vesselManagerVac.Build(v);
                _vesselModified = false;
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
                _vesselManagerVac.SetInitial(VesselState.time, VesselState.orbitalPosition.WorldToV3Rotated(),
                    VesselState.orbitalVelocity.WorldToV3Rotated(), VesselState.forward.WorldToV3Rotated());
                _vesselManagerVac.StartFuelFlowSimulationJob();
            }

            using (_newAtmoProfile.Auto())
            {
                _vesselManagerAtmo.DVLinearThrust = DVLinearThrust;
                _vesselManagerAtmo.SetConditions(atmDensity, staticPressureKpa * PhysicsGlobals.KpaToAtmospheres, mach);
                _vesselManagerAtmo.SetInitial(VesselState.time, VesselState.orbitalPosition.WorldToV3Rotated(),
                    VesselState.orbitalVelocity.WorldToV3Rotated(), VesselState.forward.WorldToV3Rotated());
                _vesselManagerAtmo.StartFuelFlowSimulationJob();
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

        private bool SimulationIsRunning() => _vesselManagerAtmo.FuelFlowSimulation.IsRunning() || _vesselManagerVac.FuelFlowSimulation.IsRunning();

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private void TryStartSimulation()
        {
            if (SimulationIsRunning())
                return;

            if (HighLogic.LoadedSceneIsEditor)
            {
                if (EditorBody is null) return;
            }
            else
            {
                if (Vessel is null) return;
            }

            double refreshInterval = HighLogic.LoadedSceneIsEditor ? 500 : 100;

            if (_stopwatch.IsRunning && _stopwatch.ElapsedMilliseconds < refreshInterval)
                return;

            _stopwatch.Restart();

            StartSimulation();
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
            _vesselModified  = true;
            _vabRebuildTimer = 2;
        }

        private void OnEditorShipModified(ShipConstruct data)
        {
            _vesselModified  = true;
            _vabRebuildTimer = 2;
        }

        private void OnGUIStageSequenceModified() => _vesselModified = true;

        private void onVesselStandardModification(Vessel data) => _vesselModified = true;

        public void RequestUpdate()
        {
            GetResults();

            TryStartSimulation();
        }
    }
}
