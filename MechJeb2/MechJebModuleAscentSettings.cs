using System;
using System.Collections.Generic;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public enum AscentType { CLASSIC, GRAVITYTURN, PVG }

    public class MechJebModuleAscentSettings : ComputerModule
    {
        public MechJebModuleAscentSettings(MechJebCore core) : base(core)
        {
        }

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble PitchStartVelocity = new EditableDouble(PITCH_START_VELOCITY_DEFAULT);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble PitchRate = new EditableDouble(PITCH_RATE_DEFAULT);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredApoapsis = new EditableDoubleMult(0, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredAttachAlt = new EditableDoubleMult(DESIRED_ATTACH_ALT_DEFAULT, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredFPA = new EditableDoubleMult(DESIRED_FPA_DEFAULT, PI / 180);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DynamicPressureTrigger = new EditableDoubleMult(DYNAMIC_PRESSURE_TRIGGER_DEFAULT, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableInt StagingTrigger = new EditableInt(1);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool AttachAltFlag = ATTACH_ALT_FLAG_DEFAULT;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool StagingTriggerFlag = STAGING_TRIGGER_FLAG_DEFAULT;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnStartAltitude = new EditableDoubleMult(500, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnStartVelocity = new EditableDoubleMult(50);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnStartPitch = new EditableDoubleMult(25);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult IntermediateAltitude = new EditableDoubleMult(45000, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult HoldAPTime = new EditableDoubleMult(1);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
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

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult DesiredOrbitAltitude = new EditableDoubleMult(100000, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble DesiredInclination = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble DesiredLan = new EditableDouble(0.0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool CorrectiveSteering = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble CorrectiveSteeringGain = 0.6; //control gain

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool ForceRoll = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble VerticalRoll = new EditableDouble(0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble TurnRoll = new EditableDouble(0);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool AutodeploySolarPanels = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool AutoDeployAntennas = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool SkipCircularization = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble RollAltitude = new EditableDouble(50);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        private bool _autostage = true;

        public bool Autostage
        {
            get => _autostage;
            set
            {
                bool changed = value != _autostage;
                _autostage = value;
                if (!changed) return;
                if (_autostage && enabled)
                {
                    core.staging.users.Add(this);
                }
                else if (!_autostage)
                {
                    core.staging.users.Remove(this);
                }
            }
        }

        /* classic AoA limter */
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool LimitAoA = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MaxAoA = 5;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult AOALimitFadeoutPressure = new EditableDoubleMult(2500);

        [Persistent(pass = (int)Pass.Type)]
        public bool LimitingAoA = false;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble LimitQa = new EditableDouble(LIMIT_QA_DEFAULT);

        [Persistent(pass = (int)Pass.Type)]
        public bool LimitQaEnabled = LIMIT_QA_ENABLED_DEFAULT;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble LaunchPhaseAngle = LAUNCH_PHASE_ANGLE_DEFAULT;

        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableDouble LaunchLANDifference = LAUNCH_LAN_DIFFERENCE;

        [Persistent(pass = (int)Pass.Global)]
        public readonly EditableInt WarpCountDown = 11;

        /*
         * "Classic" ascent path settings
         */

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnEndAltitude = new EditableDoubleMult(60000, 1000);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble TurnEndAngle = 0;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult TurnShapeExponent = new EditableDoubleMult(0.4, 0.01);

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool AutoPath = true;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public float AutoTurnPerc = 0.05f;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public float AutoTurnSpdFactor = 18.5f;

        public double AutoTurnStartAltitude =>
            vessel.mainBody.atmosphere ? vessel.mainBody.RealMaxAtmosphereAltitude() * AutoTurnPerc : vessel.terrainAltitude + 25;

        public double AutoTurnStartVelocity => vessel.mainBody.atmosphere
            ? AutoTurnSpdFactor * AutoTurnSpdFactor * AutoTurnSpdFactor * 0.015625f
            : double.PositiveInfinity;

        public double AutoTurnEndAltitude =>
            vessel.mainBody.atmosphere
                ? Math.Min(vessel.mainBody.RealMaxAtmosphereAltitude() * 0.85, DesiredOrbitAltitude)
                : Math.Min(30000, DesiredOrbitAltitude * 0.85);

        /*
         * PVG Staging values
         */

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MinDeltaV = MIN_DELTAV_DEFAULT;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MaxCoast = MAX_COAST_DEFAULT;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble MinCoast = MIN_COAST_DEFAULT;

        [Persistent(pass = (int)(Pass.Type))]
        public bool CoastBeforeFlag = false;
        
        [Persistent(pass = (int)Pass.Type)]
        public bool FixedCoast = FIXED_COAST_DEFAULT;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDouble FixedCoastLength = FIXED_COAST_LENGTH_DEFAULT;

        // deliberately not in the UI or in global, edit the ship file with an editor
        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableDouble PreStageTime = PRE_STAGE_TIME_DEFAULT;

        // deliberately not in the UI or in global, edit the ship file with an editor
        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableDouble OptimizerPauseTime = OPTIMIZER_PAUSE_TIME_DEFAULT;

        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableInt LastStage = -1;

        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableInt CoastStageInternal = -1;

        [Persistent(pass = (int)Pass.Type)]
        public bool CoastStageFlag;

        public int CoastStage => CoastStageFlag ? CoastStageInternal.val : -1;

        [Persistent(pass = (int)Pass.Type)]
        public bool OptimizeStageFlag = true;

        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableInt OptimizeStageInternal = -1;

        public int OptimizeStage => OptimizeStageFlag ? OptimizeStageInternal.val : -1;

        [Persistent(pass = (int)Pass.Type)]
        public bool SpinupStageFlag = true;

        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableInt SpinupStageInternal = -1;

        public int SpinupStage => SpinupStageFlag ? SpinupStageInternal.val : -1;

        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableDouble SpinupLeadTime = 50;

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public readonly EditableDoubleMult SpinupAngularVelocity = new EditableDoubleMult(TAU / 6.0, TAU / 60.0);

        [Persistent(pass = (int)Pass.Type)]
        public readonly EditableIntList UnguidedStagesInternal = new EditableIntList();

        [Persistent(pass = (int)Pass.Type)]
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
                    return core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
                case AscentType.GRAVITYTURN:
                    return core.GetComputerModule<MechJebModuleAscentGTAutopilot>();
                case AscentType.PVG:
                    return core.GetComputerModule<MechJebModuleAscentPVGAutopilot>();
                default:
                    return core.GetComputerModule<MechJebModuleAscentClassicAutopilot>();
            }
        }

        private void DisableAscentModules()
        {
            foreach (AscentType type in _values)
                if (type != AscentType)
                    GetAscentModule(type).enabled = false;
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

            if (Math.Abs(DesiredInclination.val) < Math.Abs(vesselState.latitude))
                DesiredInclination.val = Math.Round(vesselState.latitude, 3);

            if (Math.Abs(DesiredInclination.val) > 180 - Math.Abs(vesselState.latitude))
                DesiredInclination.val = 180 - Math.Round(vesselState.latitude, 3);

            core.guidance.UllageLeadTime.val = 20;

            core.settings.rssMode = true;

            /* set the thrust controller to sane RO/RSS defaults */
            core.thrust.limitToPreventUnstableIgnition = false;
            core.thrust.autoRCSUllaging                = true;
            core.thrust.minThrottle.val                = 0.05;
            core.thrust.limiterMinThrottle             = true;
            core.thrust.limitThrottle                  = false;
            core.thrust.limitAcceleration              = false;
            core.thrust.limitToPreventOverheats        = false;
            core.thrust.limitDynamicPressure           = false;
            core.thrust.maxDynamicPressure.val         = 20000;

            /* reset all of the staging controller, and turn on hotstaging and drop solids */
            Autostage                                  = true;
            core.staging.autostagePreDelay.val         = 0.0;
            core.staging.autostagePostDelay.val        = 0.5;
            core.staging.autostageLimit.val            = 0;
            core.staging.fairingMaxDynamicPressure.val = 5000;
            core.staging.fairingMinAltitude.val        = 50000;
            core.staging.clampAutoStageThrustPct.val   = 0.99;
            core.staging.fairingMaxAerothermalFlux.val = 1135;
            core.staging.hotStaging                    = true;
            core.staging.hotStagingLeadTime.val        = 1.0;
            core.staging.dropSolids                    = true;
            core.staging.dropSolidsLeadTime.val        = 1.0;
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
