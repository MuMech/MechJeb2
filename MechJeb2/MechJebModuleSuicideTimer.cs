#nullable enable

extern alias JetBrainsAnnotations;
using System;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Primitives;
using MechJebLib.SuicideBurnSimulation;
using MechJebLibBindings;

namespace MuMech
{
    public class MechJebModuleSuicideTimer : ComputerModule
    {
        private Suicide?              _suicide;
        private Suicide.SuicideResult _lastResult;
        public  Vector3d              Rf => _lastResult.Rf.V3ToWorldRotated();

        public MechJebModuleSuicideTimer(MechJebCore core) : base(core)
        {
        }

        protected override void OnModuleEnabled() => Reset();

        protected override void OnModuleDisabled() => Reset();

        private void Reset()
        {
            _lastResult.Rf = V3.zero;
            _lastResult.Tf = VesselState.time;
            _suicide?.Cancel();
            _suicide = null;
        }

        private double GetGroundHeight()
        {
            if (Rf == Vector3d.zero)
                return MainBody.Radius;

            Vector3d latlng = MainBody.GetLatitudeAndLongitude(Rf, true);
            return MainBody.TerrainAltitude(latlng[0], latlng[1]);
        }

        public override void OnFixedUpdate()
        {
            if (!Vessel.VesselOffGround())
                return;

            // make sure our orbit at least dips into the atmosphere, or bodyRadius plus some maximum height of mountains

            Core.StageStats.RequestUpdate();

            if (Core.StageStats.VacStats.Count <= 0)
                return;

            if (_suicide != null)
            {
                if (_suicide.IsRunning)
                    return;

                if (_suicide.IsCompleted)
                    _lastResult = _suicide.Result;
            }

            Suicide.SuicideBuilder suicideBuilder = Suicide.Builder()
                .Initial(VesselState.orbitalPosition.WorldToV3Rotated(), VesselState.orbitalVelocity.WorldToV3Rotated(),
                    VesselState.forward.WorldToV3Rotated(), VesselState.time, MainBody.gravParameter, MainBody.Radius)
                .SetGround(GetGroundHeight());

            for (int mjPhase = Core.StageStats.VacStats.Count - 1; mjPhase >= 0; mjPhase--)
            {
                FuelStats fuelStats = Core.StageStats.VacStats[mjPhase];
                int       kspStage  = Core.StageStats.VacStats[mjPhase].KSPStage;

                suicideBuilder.AddStage(fuelStats.StartMass * 1000, fuelStats.EndMass * 1000, fuelStats.Thrust, fuelStats.Isp,
                    kspStage, mjPhase);
            }

            _suicide = suicideBuilder.Build();
            if (!_suicide.TryStartJob(null))
                throw new Exception("[MechJebModuleSuicideTimer] could not start job");
        }
    }
}
