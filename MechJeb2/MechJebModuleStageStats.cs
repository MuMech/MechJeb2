extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Primitives;
using MechJebLibBindings;
using MechJebLibBindings.FuelFlowSimulation;
using Unity.Profiling;
using Debug = UnityEngine.Debug;

namespace MuMech
{
    public class MechJebModuleStageStats : ComputerModule
    {
        [ToggleInfoItem("#MechJeb_DVincludecosinelosses", InfoItem.Category.Thrust, showInEditor = true)] //ΔV include cosine losses
        public readonly bool DVLinearThrust = true;

        public CelestialBody EditorBody;
        public bool LiveSLT = true;
        public double AltSLT = 0;
        public double Mach = 0;

        private int _vabRebuildTimer = 1;

        public readonly List<FuelStats> AtmoStats = new List<FuelStats>();
        public readonly List<FuelStats> VacStats = new List<FuelStats>();
        public double AtmoT, VacT;
        public V3 AtmoR, VacR;
        public V3 AtmoV, VacV;
        public V3 AtmoU, VacU;

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
            _vesselModified = true;
        }

        private readonly SimVesselManager _vesselManagerAtmo = new SimVesselManager();
        private readonly SimVesselManager _vesselManagerVac = new SimVesselManager();

        public override void OnFixedUpdate() => GetResults();

        public override void OnUpdate() => GetResults();

        private static ProfilerMarker _newRunSimulationProfile = new ProfilerMarker("RunSimulation");
        private static ProfilerMarker _newBuildProfile = new ProfilerMarker("Build");
        private static ProfilerMarker _newUpdateProfile = new ProfilerMarker("Update");
        private static ProfilerMarker _newVacProfile = new ProfilerMarker("Vac");
        private static ProfilerMarker _newAtmoProfile = new ProfilerMarker("Atmo");

        // the last failure logged: a failing simulation is retried every 100 ms (500 ms in the editor), so it is logged once
        private string _lastFailure;

        private void GetResults()
        {
            var atmo = _vesselManagerAtmo.FuelFlowSimulation;
            var vac = _vesselManagerVac.FuelFlowSimulation;

            // the two runs start together and are published together: the stage table reads both lists with one index
            if (!atmo.IsStopped || !vac.IsStopped)
                return;

            if (atmo.IsCompleted && vac.IsCompleted)
            {
                AtmoStats.Clear();
                foreach (FuelStats item in atmo.Segments)
                    AtmoStats.Add(item);

                AtmoT = _vesselManagerAtmo.T;
                AtmoR = _vesselManagerAtmo.R;
                AtmoV = _vesselManagerAtmo.V;
                AtmoU = _vesselManagerAtmo.U;

                VacStats.Clear();
                foreach (FuelStats item in vac.Segments)
                    VacStats.Add(item);

                VacT = _vesselManagerVac.T;
                VacR = _vesselManagerVac.R;
                VacV = _vesselManagerVac.V;
                VacU = _vesselManagerVac.U;

                _lastFailure = null;
            }
            else
            {
                // the previous stats would look current (after staging or an edit): no stats until a run works again, which
                // the users already handle (no stages yet)
                AtmoStats.Clear();
                VacStats.Clear();

                string failure = (atmo.IsCompleted ? "" : "atmo: " + (atmo.Exception?.ToString() ?? "no exception") + "\n") +
                                 (vac.IsCompleted ? "" : "vac: " + (vac.Exception?.ToString() ?? "no exception"));
                if (failure != _lastFailure)
                {
                    _lastFailure = failure;
                    Debug.Log("[MechJebModuleStageStats] stats failed (logged once until it changes or works):\n" + failure);
                }
            }

            if (!atmo.TryMarkReady())
                throw new Exception("[MechJebModuleStageStats] Tried to mark a running atmo stage stats as ready.");
            if (!vac.TryMarkReady())
                throw new Exception("[MechJebModuleStageStats] Tried to mark a running vac stage stats as ready.");
        }

        private void RunSimulation()
        {
            using ProfilerMarker.AutoScope auto = _newRunSimulationProfile.Auto();

            CelestialBody simBody = HighLogic.LoadedSceneIsEditor ? EditorBody : Vessel.mainBody;

            double staticPressureKpa = HighLogic.LoadedSceneIsEditor || !LiveSLT
                ? simBody.atmosphere ? simBody.GetPressure(AltSLT) : 0
                : Vessel.staticPressurekPa;
            // kg/m³: the engines divide by the sea level density themselves
            double atmDensity = HighLogic.LoadedSceneIsEditor || !LiveSLT
                ? simBody.GetDensity(simBody.GetPressure(AltSLT), simBody.GetTemperature(0))
                : Vessel.atmDensity;
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
                _vesselManagerVac.SetInitial(VesselState.Time, VesselState.OrbitalPosition.WorldToV3Rotated(),
                    VesselState.OrbitalVelocity.WorldToV3Rotated(), VesselState.Forward.WorldToV3Rotated());
                if (!_vesselManagerVac.TryStartFuelFlowSimulationJob())
                    throw new Exception("[MechJebModuleStageStats] could not start vac stats job");
            }

            using (_newAtmoProfile.Auto())
            {
                _vesselManagerAtmo.DVLinearThrust = DVLinearThrust;
                _vesselManagerAtmo.SetConditions(atmDensity, staticPressureKpa * PhysicsGlobals.KpaToAtmospheres, mach);
                _vesselManagerAtmo.SetInitial(VesselState.Time, VesselState.OrbitalPosition.WorldToV3Rotated(),
                    VesselState.OrbitalVelocity.WorldToV3Rotated(), VesselState.Forward.WorldToV3Rotated());
                //_vesselManagerAtmo.PrintVessel();
                if (!_vesselManagerAtmo.TryStartFuelFlowSimulationJob())
                    throw new Exception("[MechJebModuleStageStats] could not start atmo stats job");
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

        private bool SimulationReady() => _vesselManagerAtmo.FuelFlowSimulation.IsReady && _vesselManagerVac.FuelFlowSimulation.IsReady;

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private void TryStartSimulation()
        {
            if (!SimulationReady())
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
            _vesselModified = true;
            _vabRebuildTimer = 2;
        }

        private void OnEditorShipModified(ShipConstruct data)
        {
            _vesselModified = true;
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
