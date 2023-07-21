using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public enum AscentType { CLASSIC, GRAVITYTURN, PVG }

    [UsedImplicitly]
    public class MechJebModuleAscentSettings : ComputerModule
    {
        public MechJebModuleAscentSettings(MechJebCore core) : base(core)
        {
        }

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PitchStartVelocity = new EditableDouble(PITCH_START_VELOCITY_DEFAULT);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PitchRate = new EditableDouble(PITCH_RATE_DEFAULT);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult DesiredApoapsis = new EditableDoubleMult(0, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult DesiredAttachAlt = new EditableDoubleMult(DESIRED_ATTACH_ALT_DEFAULT, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult DesiredAttachAltFixed = new EditableDoubleMult(DESIRED_ATTACH_ALT_DEFAULT, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult DesiredFPA = new EditableDoubleMult(DESIRED_FPA_DEFAULT, PI / 180);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult DynamicPressureTrigger = new EditableDoubleMult(DYNAMIC_PRESSURE_TRIGGER_DEFAULT, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableInt StagingTrigger = new EditableInt(1);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool AttachAltFlag = ATTACH_ALT_FLAG_DEFAULT;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool StagingTriggerFlag = STAGING_TRIGGER_FLAG_DEFAULT;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult TurnStartAltitude = new EditableDoubleMult(500, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult TurnStartVelocity = new EditableDoubleMult(50);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult TurnStartPitch = new EditableDoubleMult(25);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult IntermediateAltitude = new EditableDoubleMult(45000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult HoldAPTime = new EditableDoubleMult(1);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        private int _ascentTypeInteger;

        public AscentType AscentType
        {
            get => (AscentType)_ascentTypeInteger;
            set
            {
                _ascentTypeInteger = (int)value;
                DisableAscentModules();
            }
        }

        public MechJebModuleAscentBaseAutopilot AscentAutopilot => GetAscentModule(AscentType);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult DesiredOrbitAltitude = new EditableDoubleMult(100000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble DesiredInclination = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble DesiredLan = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool CorrectiveSteering = false;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble CorrectiveSteeringGain = 0.6; //control gain

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool ForceRoll = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VerticalRoll = new EditableDouble(0);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble TurnRoll = new EditableDouble(0);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool AutodeploySolarPanels = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool AutoDeployAntennas = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool SkipCircularization = false;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble RollAltitude = new EditableDouble(50);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        private bool _autostage = true;

        public bool Autostage
        {
            get => _autostage;
            set
            {
                bool changed = value != _autostage;
                _autostage = value;
                if (!changed) return;
                if (_autostage && Enabled)
                {
                    Core.Staging.Users.Add(AscentAutopilot);
                }
                else if (!_autostage)
                {
                    Core.Staging.Users.Remove(AscentAutopilot);
                }
            }
        }

        /* classic AoA limter */
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool LimitAoA = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MaxAoA = 5;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult AOALimitFadeoutPressure = new EditableDoubleMult(2500);

        [Persistent(pass = (int)Pass.TYPE)]
        public bool LimitingAoA = false;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble LimitQa = new EditableDouble(LIMIT_QA_DEFAULT);

        [Persistent(pass = (int)Pass.TYPE)]
        public bool LimitQaEnabled = LIMIT_QA_ENABLED_DEFAULT;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble LaunchPhaseAngle = LAUNCH_PHASE_ANGLE_DEFAULT;

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableDouble LaunchLANDifference = LAUNCH_LAN_DIFFERENCE;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableInt WarpCountDown = 11;

        /*
         * "Classic" ascent path settings
         */

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult TurnEndAltitude = new EditableDoubleMult(60000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble TurnEndAngle = 0;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult TurnShapeExponent = new EditableDoubleMult(0.4, 0.01);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool AutoPath = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public float AutoTurnPerc = 0.05f;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public float AutoTurnSpdFactor = 18.5f;

        public double AutoTurnStartAltitude =>
            Vessel.mainBody.atmosphere ? Vessel.mainBody.RealMaxAtmosphereAltitude() * AutoTurnPerc : Vessel.terrainAltitude + 25;

        public double AutoTurnStartVelocity => Vessel.mainBody.atmosphere
            ? AutoTurnSpdFactor * AutoTurnSpdFactor * AutoTurnSpdFactor * 0.015625f
            : double.PositiveInfinity;

        public double AutoTurnEndAltitude =>
            Vessel.mainBody.atmosphere
                ? Math.Min(Vessel.mainBody.RealMaxAtmosphereAltitude() * 0.85, DesiredOrbitAltitude)
                : Math.Min(30000, DesiredOrbitAltitude * 0.85);

        /*
         * PVG Staging values
         */

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MinDeltaV = MIN_DELTAV_DEFAULT;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MaxCoast = MAX_COAST_DEFAULT;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble MinCoast = MIN_COAST_DEFAULT;

        [Persistent(pass = (int)Pass.TYPE)]
        public bool CoastBeforeFlag = false;

        [Persistent(pass = (int)Pass.TYPE)]
        public bool FixedCoast = FIXED_COAST_DEFAULT;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble FixedCoastLength = FIXED_COAST_LENGTH_DEFAULT;

        // deliberately not in the UI or in global, edit the ship file with an editor
        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableDouble PreStageTime = PRE_STAGE_TIME_DEFAULT;

        // deliberately not in the UI or in global, edit the ship file with an editor
        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableDouble OptimizerPauseTime = OPTIMIZER_PAUSE_TIME_DEFAULT;

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableInt LastStage = -1;

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableInt CoastStageInternal = -1;

        [Persistent(pass = (int)Pass.TYPE)]
        public bool CoastStageFlag;

        public int CoastStage => CoastStageFlag ? CoastStageInternal.val : -1;

        [Persistent(pass = (int)Pass.TYPE)]
        public bool OptimizeStageFlag = true;

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableInt OptimizeStageInternal = -1;

        public int OptimizeStage => OptimizeStageFlag ? OptimizeStageInternal.val : -1;

        [Persistent(pass = (int)Pass.TYPE)]
        public bool SpinupStageFlag = true;

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableInt SpinupStageInternal = -1;

        public int SpinupStage => SpinupStageFlag ? SpinupStageInternal.val : -1;

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableDouble SpinupLeadTime = 50;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult SpinupAngularVelocity = new EditableDoubleMult(TAU / 6.0, TAU / 60.0);

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableIntList UnguidedStagesInternal = new EditableIntList();

        [Persistent(pass = (int)Pass.TYPE)]
        public bool UnguidedStagesFlag;

        private readonly List<int> _emptyList = new List<int>();

        public List<int> UnguidedStages => UnguidedStagesFlag ? UnguidedStagesInternal.val : _emptyList;

        /*
         * some non-persisted values
         */

        public bool LaunchingToPlane;
        public bool LaunchingToRendezvous;
        public bool LaunchingToMatchLan;
        public bool LaunchingToLan;

        /*
         * Helpers for dealing with switching between modules
         */

        private readonly AscentType[] _values = (AscentType[])Enum.GetValues(typeof(AscentType));

        private MechJebModuleAscentBaseAutopilot GetAscentModule(AscentType type)
        {
            switch (type)
            {
                case AscentType.CLASSIC:
                    return Core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
                case AscentType.GRAVITYTURN:
                    return Core.GetComputerModule<MechJebModuleAscentGTAutopilot>();
                case AscentType.PVG:
                    return Core.GetComputerModule<MechJebModuleAscentPVGAutopilot>();
                default:
                    return Core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
            }
        }

        private void DisableAscentModules()
        {
            foreach (AscentType type in _values)
                if (type != AscentType)
                    GetAscentModule(type).Enabled = false;
        }

        public override void OnFixedUpdate()
        {
            DisableAscentModules();
        }

        public void ApplyRODefaults()
        {
            PitchStartVelocity.val     = PITCH_START_VELOCITY_DEFAULT;
            PitchRate.val              = PITCH_RATE_DEFAULT;
            DesiredAttachAlt.val       = DESIRED_ATTACH_ALT_DEFAULT;
            DesiredAttachAltFixed.val  = DESIRED_ATTACH_ALT_DEFAULT;
            DesiredFPA.val             = DESIRED_FPA_DEFAULT;
            DynamicPressureTrigger.val = DYNAMIC_PRESSURE_TRIGGER_DEFAULT;
            AttachAltFlag              = ATTACH_ALT_FLAG_DEFAULT;
            StagingTriggerFlag         = STAGING_TRIGGER_FLAG_DEFAULT;
            LimitQa.val                = LIMIT_QA_DEFAULT;
            LimitQaEnabled             = LIMIT_QA_ENABLED_DEFAULT;
            MinDeltaV.val              = MIN_DELTAV_DEFAULT;
            MaxCoast.val               = MAX_COAST_DEFAULT;
            MinCoast.val               = MIN_COAST_DEFAULT;
            FixedCoast                 = FIXED_COAST_DEFAULT;
            FixedCoastLength.val       = FIXED_COAST_LENGTH_DEFAULT;
            LaunchPhaseAngle.val       = LAUNCH_PHASE_ANGLE_DEFAULT;
            LaunchLANDifference.val    = LAUNCH_LAN_DIFFERENCE;
            PreStageTime.val           = PRE_STAGE_TIME_DEFAULT;
            OptimizerPauseTime.val     = OPTIMIZER_PAUSE_TIME_DEFAULT;

            OptimizeStageInternal.val = -1;
            OptimizeStageFlag         = true;
            SpinupStageFlag           = false;
            SpinupStageInternal.val   = -1;
            CoastStageFlag            = false;
            CoastStageInternal.val    = -1;
            UnguidedStagesFlag        = false;

            if (DesiredOrbitAltitude.val < 145000)
                DesiredOrbitAltitude.val = 145000;

            if (Math.Abs(DesiredInclination.val) < Math.Abs(VesselState.latitude))
                DesiredInclination.val = Math.Round(VesselState.latitude, 3);

            if (Math.Abs(DesiredInclination.val) > 180 - Math.Abs(VesselState.latitude))
                DesiredInclination.val = 180 - Math.Round(VesselState.latitude, 3);

            Core.Guidance.UllageLeadTime.val = 20;

            Core.Settings.rssMode = true;

            /* set the thrust controller to sane RO/RSS defaults */
            Core.Thrust.LimitToPreventUnstableIgnition = false;
            Core.Thrust.AutoRCSUllaging                = true;
            Core.Thrust.MinThrottle.val                = 0.05;
            Core.Thrust.LimiterMinThrottle             = true;
            Core.Thrust.LimitThrottle                  = false;
            Core.Thrust.LimitAcceleration              = false;
            Core.Thrust.LimitToPreventOverheats        = false;
            Core.Thrust.LimitDynamicPressure           = false;
            Core.Thrust.MaxDynamicPressure.val         = 20000;

            /* reset all of the staging controller, and turn on hotstaging and drop solids */
            Autostage                                  = true;
            Core.Staging.AutostagePreDelay.val         = 0.0;
            Core.Staging.AutostagePostDelay.val        = 0.5;
            Core.Staging.AutostageLimit.val            = 0;
            Core.Staging.FairingMaxDynamicPressure.val = 5000;
            Core.Staging.FairingMinAltitude.val        = 50000;
            Core.Staging.ClampAutoStageThrustPct.val   = 0.99;
            Core.Staging.FairingMaxAerothermalFlux.val = 1135;
            Core.Staging.HotStaging                    = true;
            Core.Staging.HotStagingLeadTime.val        = 1.0;
            Core.Staging.DropSolids                    = true;
            Core.Staging.DropSolidsLeadTime.val        = 1.0;
        }

        private const double LAUNCH_LAN_DIFFERENCE            = 0;
        private const double LAUNCH_PHASE_ANGLE_DEFAULT       = 0;
        private const double FIXED_COAST_LENGTH_DEFAULT       = 0;
        private const bool   FIXED_COAST_DEFAULT              = true;
        private const double MIN_COAST_DEFAULT                = 0;
        private const double MAX_COAST_DEFAULT                = 450;
        private const double MIN_DELTAV_DEFAULT               = 40;
        private const double DESIRED_ATTACH_ALT_DEFAULT       = 110000;
        private const double PITCH_START_VELOCITY_DEFAULT     = 50;
        private const double PITCH_RATE_DEFAULT               = 0.50;
        private const double DYNAMIC_PRESSURE_TRIGGER_DEFAULT = 10000;
        private const bool   ATTACH_ALT_FLAG_DEFAULT          = false;
        private const double DESIRED_FPA_DEFAULT              = 0;
        private const bool   STAGING_TRIGGER_FLAG_DEFAULT     = false;
        private const double LIMIT_QA_DEFAULT                 = 2000;
        private const bool   LIMIT_QA_ENABLED_DEFAULT         = true;
        private const double PRE_STAGE_TIME_DEFAULT           = 10;
        private const double OPTIMIZER_PAUSE_TIME_DEFAULT     = 5;
    }
}
