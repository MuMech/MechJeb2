extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using MechJebLib.Functions;
using MechJebLib.PSG;
using MechJebLibBindings.PSG;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class MechJebModuleAscentMenu : DisplayModule
    {
        internal readonly string[] _ascentPathList = { Localizer.Format("#MechJeb_Ascent_ascentPathList1"), Localizer.Format("#MechJeb_Ascent_ascentPathList3") }; // "Classic Ascent Profile", "PSG (RSS/RO)"

        public MechJebModuleAscentMenu(MechJebCore core) : base(core) { }

        #region Delegators

        internal bool _launchingToPlane
        {
            get => _ascentSettings.LaunchingToPlane;
            set => _ascentSettings.LaunchingToPlane = value;
        }

        internal bool _launchingToMatchLan
        {
            get => _ascentSettings.LaunchingToMatchLan;
            set => _ascentSettings.LaunchingToMatchLan = value;
        }

        internal bool _launchingToLan
        {
            get => _ascentSettings.LaunchingToLan;
            set => _ascentSettings.LaunchingToLan = value;
        }

        #endregion

        internal bool _launchingWithAnyPlaneControl => _launchingToPlane || _launchingToMatchLan || _launchingToLan;

        internal MechJebModuleAscentBaseAutopilot   _autopilot       => Core.Ascent;
        internal MechJebModuleAscentSettings        _ascentSettings  => Core.AscentSettings;
        internal MechJebModuleAscentClassicPathMenu _classicPathMenu => Core.GetComputerModule<MechJebModuleAscentClassicPathMenu>();
        internal MechJebModuleAscentPSGSettingsMenu _psgSettingsMenu => Core.GetComputerModule<MechJebModuleAscentPSGSettingsMenu>();
        internal MechJebModuleAscentSettingsMenu    _settingsMenu    => Core.GetComputerModule<MechJebModuleAscentSettingsMenu>();

        internal EditableDouble InclinationSetting => _ascentSettings.AscentType == AscentType.PSG ? Core.PSGSimulation.Inc : _ascentSettings.DesiredInclination;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool _lastPSGSettingsEnabled;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool _lastSettingsMenuEnabled;

        protected override void OnModuleEnabled()
        {
            _psgSettingsMenu.Enabled = _lastPSGSettingsEnabled;
            _settingsMenu.Enabled = _lastSettingsMenuEnabled;
        }

        protected override void OnModuleDisabled()
        {
            _launchingToPlane = false;
            _launchingToMatchLan = false;
            _lastPSGSettingsEnabled = _psgSettingsMenu.Enabled;
            _lastSettingsMenuEnabled = _settingsMenu.Enabled;
            _psgSettingsMenu.Enabled = false;
            _settingsMenu.Enabled = false;
        }

        // Keeps the PSG LogicalPlan, and the FlightPlan built from it, current while this window is open with PSG selected,
        // whichever UI draws it. Engaging the autopilot enables the PSGSimulation, which freezes both for the flight.
        // BUG: this is the only place the LogicalPlan is created, so kOS can't drive PSG without this window open.
        public override void OnFixedUpdate()
        {
            if (_ascentSettings.AscentType != AscentType.PSG || !Vessel.isActiveVessel || Core.PSGSimulation.Enabled)
                return;

            Core.StageStats.RequestUpdate();

            if (Core.StageStats.VacStats.Count == 0)
                return;

            Core.PSGSimulation.LogicalPlan = new PersistedLogicalPlan(LogicalPlan.GenerateLogicalPlan(Core.StageStats.VacStats,
                _ascentSettings.UnguidedStages, _ascentSettings.FixedStages, _ascentSettings.LastStage, Core.PSGSimulation.MinDeltaV,
                _ascentSettings.CoastStage, _ascentSettings.CoastLocation));

            Core.PSGSimulation.RequestFlightPlanUpdate();
        }

        internal const string InclinationCurrentTooltip =
            "Sets inclination to launch site latitude (for due-east launch). Lower inclinations require a costly plane change.";

        // shared with the new UI
        internal void SetInclinationToLatitude(EditableDouble inclinationSetting) => inclinationSetting.Val = Math.Round(VesselState.Latitude, 3);

        internal double InclinationBelowLatitude(EditableDouble inclinationSetting)
        {
            double inclination = Math.Abs(inclinationSetting);
            return Math.Abs(VesselState.Latitude) - (inclination < 90.0 ? inclination : 180.0 - inclination);
        }

        internal bool AutoWarpTargetExists => Core.Target.NormalTargetExists && Core.Target.TargetOrbit?.referenceBody == MainBody;

        // what ShowAutoWarpGUIElements does before drawing when nothing is being launched and there is no usable target
        internal void ResetLaunchWithoutTarget()
        {
            if (!_launchingWithAnyPlaneControl && !AutoWarpTargetExists)
                _launchingToPlane = _launchingToMatchLan = false;
        }

        internal void LaunchToPlaneOfTarget()
        {
            _launchingToPlane = true;
            (double timeToPlane, double inclination) = Astro.MinimumTimeToPlane(
                MainBody.rotationPeriod,
                VesselState.Latitude,
                VesselState.CelestialLongitude,
                Core.Target.TargetOrbit.LAN - _ascentSettings.LaunchLANDifference,
                Core.Target.TargetOrbit.inclination
            );
            _autopilot.StartCountdown(VesselState.Time + timeToPlane);
            InclinationSetting.Val = inclination;
        }

        internal void LaunchToTargetLan()
        {
            _launchingToMatchLan = true;
            _autopilot.StartCountdown(VesselState.Time +
                Astro.TimeToPlane(
                    MainBody.rotationPeriod,
                    VesselState.Latitude,
                    VesselState.CelestialLongitude,
                    Core.Target.TargetOrbit.LAN - _ascentSettings.LaunchLANDifference,
                    InclinationSetting
                )
            );
        }

        internal void LaunchToLan()
        {
            _launchingToLan = true;
            _autopilot.StartCountdown(VesselState.Time +
                Astro.TimeToPlane(
                    MainBody.rotationPeriod,
                    VesselState.Latitude,
                    VesselState.CelestialLongitude,
                    Core.PSGSimulation.LAN,
                    InclinationSetting
                )
            );
        }

        internal void AbortTimedLaunch() => _launchingToPlane = _launchingToMatchLan = _launchingToLan = _autopilot.TimedLaunch = false;

        private DateTime _lastRefresh = DateTime.MinValue;
        internal string vgo, heading, tgo, pitch, label26, label27, label28, n, label29, znorm, label30, launchTimer, autopilotStatus;

        // the severity that goes with autopilotStatus; WindowGUI turns it into a style (GUI.skin only exists inside OnGUI,
        // and MechJeb2.UI calls UpdateStrings outside it)
        internal MechJebModuleAscentBaseAutopilot.StatusLevel autopilotStatusLevel;
        private TimeSpan _refreshInterval = TimeSpan.FromSeconds(0.1);

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableInt _refreshRate = 10;

        internal void UpdateStrings()
        {
            DateTime now = DateTime.Now;
            if (now <= _lastRefresh + _refreshInterval) return;

            _lastRefresh = now;
            Profiler.BeginSample("MJ.GUIWindow.UpdateStrings.StringOps");
            vgo = $"vgo: {Core.Guidance.Vgo:F1}";
            heading = $"heading: {Core.Guidance.Heading:F1}";
            tgo = $"tgo: {Core.Guidance.Tgo:F3}";
            pitch = $"pitch: {Core.Guidance.Pitch:F1}";
            label26 = $"{CachedLocalizer.Instance.MechJebAscentLabel26}{Core.Guidance.Status}";
            label27 = $"{CachedLocalizer.Instance.MechJebAscentLabel27}{Core.PSGSimulation.SuccessfulConverges}";
            label28 = $"{CachedLocalizer.Instance.MechJebAscentLabel28}{Core.PSGSimulation.LastLmStatus}";
            n = $"n: {Core.PSGSimulation.LastLmIterations}({Core.PSGSimulation.MaxLmIterations})";
            label29 = $"{CachedLocalizer.Instance.MechJebAscentLabel29} {GuiUtils.TimeToDHMS(Core.PSGSimulation.Staleness)}";
            znorm = $"infeasibility: {Core.PSGSimulation.LastInfeasibility:G5}";
            if (Core.PSGSimulation.LastFailureMessage != null)
                label30 = $"{CachedLocalizer.Instance.MechJebAscentLabel30}{Core.PSGSimulation.LastFailureMessage}";

            if (_launchingToPlane) launchTimer = CachedLocalizer.Instance.MechJebAscentMsg2; //Launching to target plane
            else if (_launchingToMatchLan) launchTimer = CachedLocalizer.Instance.MechJebAscentLaunchingToTargetLAN; //Launching to target LAN
            else if (_launchingToLan) launchTimer = CachedLocalizer.Instance.MechJebAscentLaunchingToManualLAN; //Launching to manual LAN
            else launchTimer = string.Empty;
            if (_autopilot.TMinus > 3 * VesselState.DeltaT)
                launchTimer += $": T-{GuiUtils.TimeToDHMS(_autopilot.TMinus, 1)}";

            autopilotStatus      = CachedLocalizer.Instance.MechJebAscentLabel35 + _autopilot.Status;
            autopilotStatusLevel = _autopilot.StatusSeverity;
            UpdatePhaseLines();
            Profiler.EndSample();
        }

        // The PSG status lines (one per burn/coast phase, then the terminal conditions), for the new UI whose rows are fixed
        internal const int MAX_PHASE_LINES = 12;
        internal readonly string[] PhaseLines = new string[MAX_PHASE_LINES];
        internal int PhaseLineCount;

        private void UpdatePhaseLines()
        {
            PhaseLineCount = 0;
            if (_ascentSettings.AscentType != AscentType.PSG || Core.Guidance.Solution == null) return;

            Solution solution = Core.Guidance.Solution;
            for (int psgPhase = solution.Segments - 1; psgPhase >= 0 && PhaseLineCount < MAX_PHASE_LINES - 1; psgPhase--)
                PhaseLines[PhaseLineCount++] = PhaseString(solution, VesselState.Time, psgPhase);
            PhaseLines[PhaseLineCount++] = solution.TerminalString();
        }

        internal void RefreshRateChanged(int oldRate)
        {
            if (oldRate != _refreshRate)
            {
                _refreshRate = Math.Max(_refreshRate, 1);
                _refreshInterval = TimeSpan.FromSeconds(1d / _refreshRate);
            }
        }

        private string PhaseString(Solution solution, double t, int psgPhase)
        {
            int mjPhase = solution.MJPhase(psgPhase);
            int kspStage = solution.KSPStage(psgPhase);

            if (solution.CoastPhase(psgPhase))
                return $"coast: {kspStage} {solution.Tgo(t, psgPhase):F1}s";

            double stageDeltaV = 0;

            if (mjPhase < Core.StageStats.VacStats.Count)
                stageDeltaV = Core.StageStats.VacStats[mjPhase].DeltaV;

            double excessDV = stageDeltaV - solution.DV(t, psgPhase);

            // eliminate some of the noise
            if (Math.Abs(excessDV) < 2.5) excessDV = 0;

            return $"burn: {kspStage} {solution.Tgo(t, psgPhase):F1}s {solution.DV(t, psgPhase):F1}m/s ({excessDV:F1}m/s)";
        }

        public override string GetName() => CachedLocalizer.Instance.MechJebAscentTitle; //"Ascent Guidance"

        public override string IconName() => "Ascent Guidance";
    }
}
