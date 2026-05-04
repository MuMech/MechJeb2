#nullable enable

extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using MechJebLib.FuelFlowSimulation;
using MechJebLib.Functions;
using MechJebLib.HoverslamSimulation;
using MechJebLib.Primitives;
using MechJebLibBindings;
using UnityEngine;
using static System.Math;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleHoverslamSimulation : ComputerModule
    {
        // TODO: have the integrator produce this value in the background thread
        // (this isn't as expensive as an ODE integrator but there is multiple kepler solves every tick)
        [ValueInfoItem("#MechJeb_HoverslamImpact", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamImpact_tooltip")] //Impact
        public string Impact()
        {
            if (Orbit.PeA > 0 || Vessel.Landed) return "N/A";

            double impactTime = VesselState.time;
            try
            {
                for (int iter = 0; iter < 10; iter++)
                {
                    Vector3d impactPosition = Orbit.WorldPositionAtUT(impactTime);
                    double   terrainRadius  = MainBody.Radius + MainBody.TerrainAltitude(impactPosition);
                    impactTime = Orbit.NextTimeOfRadius(VesselState.time, terrainRadius);
                }
            }
            catch (ArgumentException)
            {
                return GuiUtils.TimeToDHMS(0, 1);
            }
            catch (ArithmeticException)
            {
                return GuiUtils.TimeToDHMS(0, 1);
            }

            return GuiUtils.TimeToDHMS(impactTime - VesselState.time, 1);
        }

        // TODO: this shows numbers like e.g. -1.2s in flight if the cycles/second is 1.0s -- not sure how to fix
        [ValueInfoItem("#MechJeb_HoverslamIgnition", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamIgnition_tooltip")] //Ignition
        public string Ignition() =>
            Orbit.PeA > 0 || Vessel.Landed ? "N/A" : GuiUtils.TimeToDHMS(IgnitionCountdown, 1);

        [ValueInfoItem("#MechJeb_HoverslamTouchdown", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamTouchdown_tooltip")] //Touchdown
        public string Touchdown() =>
            Orbit.PeA > 0 || Vessel.Landed ? "N/A" : GuiUtils.TimeToDHMS(LandingCountdown, 1);

        // TODO: This counts down even if you don't ignite and i'm not entirely certain what to do about it
        // TODO: this doesn't account for planned vertical descent deltaV
        [ValueInfoItem("#MechJeb_HoverslamDeltaV", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamDeltaV_tooltip")] //Hoverslam Δv
        public string HoverslamDeltaV() =>
            IsFinite(LandingUT) ? DeltaV(LandingCountdown - (IgnitionUT < VesselState.time ? 0 : IgnitionCountdown)).ToSI() + "m/s" : "N/A";

        [ValueInfoItem("#MechJeb_HoverslamCoordinates", InfoItem.Category.Hoverslam, width = 90, tooltip = "#MechJeb_HoverslamCoordinates_tooltip")] //Coordinates
        public string HoverslamCoordinates() => Coordinates.ToStringDMS(Lat, Lng, true);

        [ValueInfoItem("#MechJeb_HoverslamBiome", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamBiome_tooltip")] //Biome
        public string Biome() => IsFinite(LandingUT) ? MainBody.GetExperimentBiomeSafe(Lat, Lng) : "N/A";

        [ValueInfoItem("#MechJeb_HoverslamSlope", InfoItem.Category.Hoverslam, format = "F2", units = "º", tooltip = "#MechJeb_HoverslamSlope_tooltip")] //Slope
        public double Slope;

        [ValueInfoItem("#MechJeb_HoverslamTerrainAltitude", InfoItem.Category.Hoverslam, format = "F1", units = "m", tooltip = "#MechJeb_HoverslamTerrainAltitude_tooltip")] //Terrain altitude
        public double TerrainAltitude;

        [ToggleInfoItem("#MechJeb_HoverslamMapLandingPrediction", InfoItem.Category.Hoverslam, tooltip = "#MechJeb_HoverslamMapLandingPrediction_tooltip")]
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))] //Map landing prediction
        public bool MapLandingPrediction = true;

        [EditableInfoItem("#MechJeb_HoverslamSimRecalcInterval", InfoItem.Category.Hoverslam, width = 50, rightLabel = "s", expandWidth = true, tooltip = "#MechJeb_HoverslamSimRecalcInterval_tooltip")]
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))] //Simulation recalc interval
        public readonly EditableDouble SimRecalcInterval = new EditableDouble(1.0);

        [EditableInfoItem("#MechJeb_HoverslamVerticalAuthority", InfoItem.Category.Hoverslam, width = 50, rightLabel = "%", expandWidth = true, tooltip = "#MechJeb_HoverslamVerticalAuthority_tooltip")]
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))] //Vertical phase authority
        public readonly EditableDoubleMult VerticalAuthority = new EditableDoubleMult(0.5, 0.01);

        [EditableInfoItem("#MechJeb_HoverslamVerticalAltitude", InfoItem.Category.Hoverslam, width = 50, rightLabel = "m", expandWidth = true, tooltip = "#MechJeb_HoverslamVerticalAltitude_tooltip")]
        [Persistent(pass = (int)(Pass.GLOBAL | Pass.TYPE))] //Vertical phase altitude
        public readonly EditableDouble VerticalAltitude = new EditableDouble(100);

        // TODO: VerticalAuthority needs better integration with the prediction.
        // TODO: what does VerticalAuthority mean?  for stock it is just halfway between hover and max.  for a unthrottleable
        //       engine it should just be the same to give 50% duty cycle.  for a throttleable engine, it should probably be
        //       halfway between min and max throttle, even though we can PWM if we have to.
        // TODO: should we be able to disable PWM for throttleable engines to avoid consuming ignitions?
        // TODO: somehow wire up DisengageAutopilot to the ToggleInfoItem
        // TODO: staging delays for multistage fed into simulation
        // TODO: add aero model for landing on Kerbin/Earth
        // TODO: going to need to have spoolup for RO/RF--on a long enough timeline someone will ask about nuclear landers
        // TODO: principia integration and gravitational harmonics
        // TODO: could we check for the presence of the old suicide burn countdown timer in a user's config and then
        //       delete that, and add the new hoverslam info item menu automagically?

        private List<FuelStats>      _vacStats => Core.StageStats.VacStats;
        private HoverslamSimulation? _hoverslam;

        // ReSharper disable MemberCanBePrivate.Global
        public Vector3d LandingPosition;
        public double   IgnitionUT;
        public double   LandingUT;
        public double   FinalThrustAccel;
        public double   Lat;
        public double   Lng;
        public double   IgnitionCountdown => IgnitionUT - VesselState.time;
        public double   LandingCountdown  => LandingUT - VesselState.time;
        public double   FinalDescentSpeed; // should be positive
        public Vector3d IgnitionAttitude;
        // ReSharper restore MemberCanBePrivate.Global

        private double _lastCycleUT;

        public MechJebModuleHoverslamSimulation(MechJebCore core) : base(core)
        {
        }

        public override void OnStart(PartModule.StartState state)
        {
            Enabled = HighLogic.LoadedSceneIsFlight;
            Core.AddToPostDrawQueue(DrawMapViewLanding);
        }

        private void DrawMapViewLanding()
        {
            if (HighLogic.LoadedSceneIsEditor) return;
            if (!MapView.MapIsEnabled) return;
            if (!MapLandingPrediction) return;
            if (!Vessel.isActiveVessel || Vessel.GetMasterMechJeb() != Core) return;
            if (!IsFinite(LandingUT)) return;

            GLUtils.DrawGroundMarker(MainBody, Lat, Lng, Color.magenta, true);
        }

        protected override void OnModuleEnabled() => Reset();

        protected override void OnModuleDisabled() => Reset();

        private void Reset()
        {
            LandingPosition = Vector3d.zero;
            IgnitionUT = double.NaN;
            LandingUT = double.NaN;
            Lat = 0;
            Lng = 0;
            FinalThrustAccel = -1;
            _hoverslam?.Cancel();
            _hoverslam = null;
            _lastCycleUT = 0;
        }

        private double DeltaV(double burnTime)
        {
            Core.StageStats.RequestUpdate();

            int    lastNonZeroIndex = -1;
            double dv               = 0;

            for (int mjPhase = _vacStats.Count - 1; mjPhase >= 0 && burnTime > 0; mjPhase--)
            {
                FuelStats stage = _vacStats[mjPhase];

                if (stage.DeltaV <= 0)
                    continue;

                lastNonZeroIndex = mjPhase;
                double bt = stage.DeltaTime < burnTime ? stage.DeltaTime : burnTime;

                dv += Astro.DeltaVFromMassThrustIspBurntime(stage.StartMass, stage.Thrust, stage.Isp, bt);
                burnTime -= bt;
            }

            if (burnTime > 0 && lastNonZeroIndex > -1) // we ain't gonna make it
            {
                FuelStats stage = _vacStats[lastNonZeroIndex];

                dv += Astro.DeltaVFromMassThrustIspBurntime(stage.StartMass, stage.Thrust, stage.Isp, burnTime);
            }

            return dv;
        }

        // TODO: we don't account for the height of the rocket here
        private double GetGroundRadius() =>
            IsFinite(LandingUT) ? MainBody.Radius + TerrainAltitude : MainBody.Radius;

        public override void OnFixedUpdate()
        {
            if (VesselState.mainBody is null || !Vessel.VesselOffGround() || Orbit.PeA > 0)
            {
                Reset();
                return;
            }

            if (VesselState.time < _lastCycleUT + SimRecalcInterval * TimeWarp.CurrentRate)
                return;

            // make sure our orbit at least dips into the atmosphere, or bodyRadius plus some maximum height of mountains

            Core.StageStats.RequestUpdate();

            if (_vacStats.Count <= 0)
                return;

            if (_hoverslam != null)
            {
                if (_hoverslam.IsRunning)
                    return;

                if (_hoverslam.IsCompleted)
                {
                    LandingPosition = _hoverslam.Rf.V3ToWorldRotated();
                    IgnitionUT = _hoverslam.IgnitionUT;
                    IgnitionAttitude = _hoverslam.IgnitionAttitude.V3ToWorldRotated();
                    LandingUT = _hoverslam.LandingUT;
                    FinalThrustAccel = _hoverslam.FinalThrustAccel;
                    MainBody.GetLatLngAltAtUT(LandingUT, LandingPosition, out Lat, out Lng, out _);
                    TerrainAltitude = MainBody.TerrainAltitude(Lat, Lng, true);
                    Slope = MainBody.GetPQSSlopeDegrees(Lat, Lng);
                }

                if (_hoverslam.IsFaulted && _hoverslam.ExceptionMessage != null)
                    Print($"[MechJebModuleHoverslamSimulation] {_hoverslam.ExceptionMessage}");
            }

            V3 w = 2 * PI / MainBody.rotationPeriod * V3.northpole;

            HoverslamSimulation.HoverslamSimulationBuilder builder = HoverslamSimulation.Builder();

            bool noBurnableStages = true;

            for (int mjPhase = _vacStats.Count - 1; mjPhase >= 0; mjPhase--)
            {
                FuelStats fuelStats = _vacStats[mjPhase];

                if (fuelStats.DeltaV <= 0)
                    continue;

                builder.AddStage(fuelStats.StartMass * 1000, fuelStats.EndMass * 1000, fuelStats.Thrust * 1000, fuelStats.Isp,
                    fuelStats.KSPStage, mjPhase);

                noBurnableStages = false;
            }

            if (noBurnableStages)
                return;

            double r = GetGroundRadius();
            double g = MainBody.gravParameter / (r * r);
            double a = g + Clamp01(VerticalAuthority) * (FinalThrustAccel - g);
            FinalDescentSpeed = FinalThrustAccel < 0 ? 0 : Sqrt(Max(2.0 * (a - g) * VerticalAltitude, 0));

            builder.Initial(Core.StageStats.VacR, Core.StageStats.VacV, Core.StageStats.VacT, MainBody.gravParameter, w);
            builder.TargetConditions(r + VerticalAltitude, FinalDescentSpeed);

            _hoverslam = builder.Build();
            if (!_hoverslam.TryStartJob())
                throw new Exception("[MechJebModuleHoverslamSimulation] could not start job");

            _lastCycleUT = VesselState.time;
        }
    }
}
