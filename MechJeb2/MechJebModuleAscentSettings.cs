extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using JetBrainsAnnotations::JetBrains.Annotations;
using MechJebLibBindings;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public enum AscentType { CLASSIC, PSG }

    public class MechJebModuleAscentSettings : ComputerModule
    {
        public MechJebModuleAscentSettings(MechJebCore core) : base(core)
        {
        }

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool ForceResetROSettings = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PitchStartHeight = new EditableDouble(PITCH_START_HEIGHT_DEFAULT);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble PitchRate = new EditableDouble(PITCH_RATE_DEFAULT);

        [UsedImplicitly]
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public int AscentTypeInteger;

        public AscentType AscentType
        {
            get => (AscentType)AscentTypeInteger;
            set
            {
                AscentTypeInteger = (int)value;
                DisableAscentModules();
            }
        }

        public IAscentAutopilot AscentAutopilot => GetAscentModule(AscentType);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult DesiredOrbitAltitude = new EditableDoubleMult(100000, 1000);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble DesiredInclination = new EditableDouble(0.0);

        // has no effect: nothing reads it. Kept so that kOS scripts setting RELATIVELAN still run.
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool RelativeLAN = false;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool ForceRoll = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble VerticalRoll = new EditableDouble(0);

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble TurnRoll = new EditableDouble(0);

        /// <summary>
        ///     The ascent's own throttle limits: the thrust controller reads them instead of the general ones (Utilities) while an
        ///     ascent autopilot is engaged.
        /// </summary>
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly ThrustLimitSet ThrustLimits = new ThrustLimitSet();

        /// <summary>The ascent's own staging rules: the staging controller reads them while an ascent autopilot is engaged.</summary>
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly StagingRuleSet StagingRules = new StagingRuleSet();

        /// <summary>The ascent's own Auto-warp: the launch countdown, the coast and the Classic circularization burn use it.</summary>
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool Autowarp = true;

        // false until the ascent's own values were first copied from the general ones (InitOwnValuesIfNeeded)
        [UsedImplicitly]
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool OwnValuesInitialized;

        /// <summary>The limits a PSG ascent flies with: the ascent's own, without the throttle cap and the electric limit.</summary>
        public IThrustLimits PsgThrustLimits => _psgThrustLimits ??= new ThrustLimitsWithoutThrottleCap(ThrustLimits);

        private IThrustLimits _psgThrustLimits;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool AutoDeploySolarPanels = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool AutoDeployAntennas = true;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble RollAltitude = new EditableDouble(50);

        [UsedImplicitly]
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool _autostage = true;

        public bool Autostage
        {
            get => _autostage;
            set
            {
                bool changed = value != _autostage;
                _autostage = value;
                if (!changed) return;
                if (_autostage && AscentAutopilot.Enabled)
                {
                    Core.Staging.Users.Add(AscentAutopilot);
                }
                else if (!_autostage)
                {
                    Core.Staging.Users.Remove(AscentAutopilot);
                }
            }
        }

        // whether the ascent autopilot's AoA limiter is currently limiting, for the settings window
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool LimitingAoA = false;

        // the AoA limit the ascent autopilot's limiter last computed, for the settings window
        public double CurrentMaxAoA;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble LaunchLANDifference = LAUNCH_LAN_DIFFERENCE;

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableInt WarpCountDown = 11;

        /*
         * PSG Staging values
         */

        // which stages burn, how, and where the coast and its spin-up go are the PSG plan's (PSGSimulation.LogicalPlan)

        [Persistent(pass = (int)Pass.TYPE)]
        public readonly EditableDouble SpinupLeadTime = 50;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDoubleMult SpinupAngularVelocity = new EditableDoubleMult(TAU / 6.0, TAU / 60.0);

        public bool OptimizeStageFlag => !Core.PSGSimulation.LogicalPlan?.AllFixedBurnTimes() ?? true;

        /*
         * some non-persisted values
         */

        public bool LaunchingToPlane;
        public bool LaunchingToMatchLan;
        public bool LaunchingToLan;
        public bool OverrideWarpToPlane;

        /// <summary>
        ///     An ascent autopilot's engage: its own throttle limits, staging rules and Auto-warp apply instead of the general ones
        ///     until it disengages (ReleaseOwnValues). PSG flies without the throttle cap and the electric limit.
        /// </summary>
        public void ApplyOwnValues(object autopilot, bool psg)
        {
            Core.Thrust.SetLimitsOverride(autopilot, psg ? PsgThrustLimits : ThrustLimits);
            Core.Staging.SetRulesOverride(autopilot, StagingRules);
            Core.Node.SetAutowarpOverride(autopilot, () => Autowarp);
        }

        public void ReleaseOwnValues(object autopilot)
        {
            Core.Thrust.ClearLimitsOverride(autopilot);
            Core.Staging.ClearRulesOverride(autopilot);
            Core.Node.ClearAutowarpOverride(autopilot);
        }

        /// <summary>
        ///     The ascent's own throttle limits, staging rules and Auto-warp start as a copy of the general ones, once: an ascent
        ///     flies as before until the player changes one side. The core calls this after its settings reload and saves at once
        ///     when it returns true.
        /// </summary>
        public bool InitOwnValuesIfNeeded()
        {
            if (OwnValuesInitialized) return false;
            MuMech.ThrustLimits.Copy(Core.Thrust, ThrustLimits);
            MuMech.StagingRules.Copy(Core.Staging, StagingRules);
            Autowarp             = Core.Node.Autowarp;
            OwnValuesInitialized = true;
            return true;
        }

        /*
         * Helpers for dealing with switching between modules
         */

        private readonly AscentType[] _values = (AscentType[])Enum.GetValues(typeof(AscentType));

        private IAscentAutopilot GetAscentModule(AscentType type)
        {
            return type switch
            {
                AscentType.CLASSIC => Core.GetComputerModule<MechJebModuleAscentClassicAutopilot>(),
                AscentType.PSG     => Core.GetComputerModule<MechJebModuleAscentPSGAutopilot>(),
                _                  => Core.GetComputerModule<MechJebModuleAscentClassicAutopilot>()
            };
        }

        private void DisableAscentModules()
        {
            foreach (AscentType type in _values)
                if (type != AscentType)
                    GetAscentModule(type).Users.Clear(); // no user is left to keep it engaged later
        }

        // A load writes AscentTypeInteger and _autostage without their setters: apply what the setters do
        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            int oldAscentType = AscentTypeInteger;

            base.OnLoad(local, type, global);

            if (AscentTypeInteger != oldAscentType)
                DisableAscentModules();

            IAscentAutopilot autopilot = AscentAutopilot;
            if (autopilot == null || !autopilot.Enabled)
                return;

            if (_autostage)
                Core.Staging.Users.Add(autopilot);
            else
                Core.Staging.Users.Remove(autopilot);
        }

        /// <summary>
        ///     Applies the RO defaults once per install with RO. The core calls this after its settings reload, which would
        ///     otherwise put the saved values back over them, and saves at once when it returns true.
        /// </summary>
        public bool ApplyRODefaultsIfNeeded()
        {
            if (!ForceResetROSettings || !ReflectionUtils.IsLoadedRealismOverhaul)
                return false;

            ApplyRODefaults();
            ForceResetROSettings = false;
            return true;
        }

        private void ApplyRODefaults()
        {
            PitchStartHeight.Val = PITCH_START_HEIGHT_DEFAULT;
            PitchRate.Val = PITCH_RATE_DEFAULT;
            LaunchLANDifference.Val = LAUNCH_LAN_DIFFERENCE;

            DesiredOrbitAltitude.Val = 145000;

            Core.Guidance.UllageLeadTime.Val = 20;

            Core.Settings.RssMode = true;

            // set the thrust controller to sane RO/RSS defaults
            Core.Thrust.LimitToPreventUnstableIgnition = false;
            Core.Thrust.AutoRCSUllaging = true;
            Core.Thrust.MinThrottle.Val = 0.05;
            Core.Thrust.LimiterMinThrottle = true;
            Core.Thrust.LimitThrottle = false;
            Core.Thrust.LimitAcceleration = false;
            Core.Thrust.LimitToPreventOverheats = false;
            Core.Thrust.LimitDynamicPressure = false;
            Core.Thrust.MaxDynamicPressure.Val = 20000;

            // reset the staging controller, turn on hot-staging and drop solids
            Autostage = true;
            Core.Staging.AutostagePreDelay.Val = 0.0;
            Core.Staging.AutostagePostDelay.Val = 0.5;
            Core.Staging.AutostageLimit.Val = 0;
            Core.Staging.FairingMaxDynamicPressureEnabled = true;
            Core.Staging.FairingMaxDynamicPressure.Val = 5000;
            Core.Staging.FairingMinAltitudeEnabled = true;
            Core.Staging.FairingMinAltitude.Val = 50000;
            Core.Staging.ClampAutoStageThrustPct.Val = 0.99;
            Core.Staging.FairingMaxAerothermalFluxEnabled = true;
            Core.Staging.FairingMaxAerothermalFlux.Val = 1135;
            Core.Staging.HotStaging = true;
            Core.Staging.HotStagingLeadTime.Val = 2.0;
            Core.Staging.DropSolids = true;
            Core.Staging.DropSolidsTwrPct.Val = 0.50;

            // turn on PSG by default
            AscentType = AscentType.PSG;

            // open the PSG ascent settings windows for new users
            MechJebModuleAscentMenu ascentMenu = Core.GetComputerModule<MechJebModuleAscentMenu>();
            ascentMenu._lastSettingsMenuEnabled = true;

            // long stats
            MechJebModuleInfoItems infoItems = Core.GetComputerModule<MechJebModuleInfoItems>();
            infoItems.StageDisplayState = 1;

            // Set a less restrictive initial limit on maxQ and turn it off by default
            Core.Thrust.MaxDynamicPressure.Val = 50000;
            Core.Thrust.LimitDynamicPressure = false;

            // Disable 'kill roll' toggle
            Core.Node.KillRollRotation = false;

            // the ascent flies with its own values: they get the same defaults
            MuMech.ThrustLimits.Copy(Core.Thrust, ThrustLimits);
            MuMech.StagingRules.Copy(Core.Staging, StagingRules);
            OwnValuesInitialized = true;
        }

        private const double LAUNCH_LAN_DIFFERENCE = 0;
        private const double PITCH_START_HEIGHT_DEFAULT = 100;
        private const double PITCH_RATE_DEFAULT = 5.0;
    }
}
