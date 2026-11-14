extern alias JetBrainsAnnotations;
using System;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using MechJebLib.PSG;
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

        internal IAscentAutopilot                   _autopilot       => Core.Ascent;
        internal MechJebModuleAscentSettings        _ascentSettings  => Core.AscentSettings;
        internal MechJebModuleAscentClassicPathMenu _classicPathMenu => Core.GetComputerModule<MechJebModuleAscentClassicPathMenu>();
        internal MechJebModuleAscentPSGSettingsMenu _psgSettingsMenu => Core.GetComputerModule<MechJebModuleAscentPSGSettingsMenu>();
        internal MechJebModuleAscentSettingsMenu    _settingsMenu    => Core.GetComputerModule<MechJebModuleAscentSettingsMenu>();

        internal EditableDouble InclinationSetting => Core.LaunchCountdown.InclinationSetting;

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

        // a closed window leaves the launch mode alone: the countdown and the guidance still fly it
        protected override void OnModuleDisabled()
        {
            _lastPSGSettingsEnabled = _psgSettingsMenu.Enabled;
            _lastSettingsMenuEnabled = _settingsMenu.Enabled;
            _psgSettingsMenu.Enabled = false;
            _settingsMenu.Enabled = false;
        }

        // Keeps the PSG LogicalPlan, and the FlightPlan built from it, current while this window is open with PSG selected,
        // whichever UI draws it. Engaging the autopilot enables the PSGSimulation, which freezes both for the flight.
        // Without this window, the PSG ascent autopilot builds them itself when it is engaged (kOS launches).
        public override void OnFixedUpdate()
        {
            if (_ascentSettings.AscentType != AscentType.PSG || !Vessel.isActiveVessel || Core.PSGSimulation.Enabled)
                return;

            Core.StageStats.RequestUpdate();
            Core.PSGSimulation.UpdateLogicalPlan(_ascentSettings);
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

        internal void LaunchToPlaneOfTarget() => Core.LaunchCountdown.StartToPlaneOfTarget();

        internal void LaunchToTargetLan() => Core.LaunchCountdown.StartToTargetLan();

        internal void LaunchToLan() => Core.LaunchCountdown.StartToLan();

        // Abort cancels the launch, not only its timer: an autopilot left engaged would launch at once from a landed vessel, or
        // after T-0 without the plane. Disengaging ends the countdown too.
        internal void AbortTimedLaunch()
        {
            if (Core.Ascent is { Enabled: true } autopilot)
                autopilot.Users.Clear();
            Core.LaunchCountdown.Abort();
        }

        private DateTime _lastRefresh = DateTime.MinValue;
        internal string vgo, heading, tgo, pitch, label26, label27, label28, n, label29, znorm, label30, launchTimer, autopilotStatus;

        // the severity that goes with autopilotStatus, which MechJeb2.UI turns into a colour
        internal AscentStatusLevel autopilotStatusLevel;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableInt _refreshRate = 10;

        // the persisted rate is the one source: a loaded value counts as much as an edited one
        private TimeSpan RefreshInterval => TimeSpan.FromSeconds(1d / Math.Max(_refreshRate, 1));

        internal void UpdateStrings()
        {
            DateTime now = DateTime.Now;
            if (now <= _lastRefresh + RefreshInterval) return;

            _lastRefresh = now;
            Profiler.BeginSample("MJ.GUIWindow.UpdateStrings.StringOps");
            // the guidance rows are only shown for a PSG ascent, so the classic one builds no strings for them
            if (_ascentSettings.AscentType == AscentType.PSG)
            {
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
                // the optimizer only runs while the autopilot (or kOS) uses it: no line before the autopilot is engaged
                optimizerState = Core.PSGSimulation.Enabled
                    ? OptimizerStateText(Core.PSGSimulation.State, Core.PSGSimulation.StateSeconds, Core.Guidance.Solution == null, Core.Guidance.IsCoasting(),
                        Core.PSGSimulation.LastFailureMessage != null ? Core.PSGSimulation.Staleness : 0)
                    : null;
                // the figures belong to the last solve: kept in flight after the ascent ended (they say why), but not on the pad with the
                // autopilot off, where the player may have changed the rocket or the target since
                ReachWarning = Core.PSGSimulation.Enabled || (Vessel != null && !Vessel.LandedOrSplashed) ? PsgReachWarning() : null;
            }
            else
            {
                ReachWarning = null;
            }

            if (_launchingToPlane) launchTimer = CachedLocalizer.Instance.MechJebAscentMsg2; //Launching to target plane
            else if (_launchingToMatchLan) launchTimer = CachedLocalizer.Instance.MechJebAscentLaunchingToTargetLAN; //Launching to target LAN
            else if (_launchingToLan) launchTimer = CachedLocalizer.Instance.MechJebAscentLaunchingToManualLAN; //Launching to manual LAN
            else launchTimer = string.Empty;
            if (Core.LaunchCountdown.TMinus > 3 * VesselState.DeltaT)
                launchTimer += $": T-{GuiUtils.TimeToDHMS(Core.LaunchCountdown.TMinus, 1)}";

            autopilotStatus      = CachedLocalizer.Instance.MechJebAscentLabel35 + _autopilot.Status.Text;
            autopilotStatusLevel = _autopilot.Status.Severity;
            UpdatePhaseLines();
            Profiler.EndSample();
        }

        /// <summary>"Optimizer: …": what the PSG optimizer is doing, or why it is not solving, so that a climbing staleness reads as a pause.</summary>
        internal string optimizerState;

        /// <param name="failingFor">
        ///     the plan's age while the re-solves fail (a failure is kept until the next success), else 0: guidance keeps flying the
        ///     last plan, and its age says how stale it is
        /// </param>
        internal static string OptimizerStateText(OptimizerState state, double seconds, bool noSolution, bool coasting = false, double failingFor = 0)
        {
            switch (state)
            {
                case OptimizerState.SOLVING:
                case OptimizerState.READY:
                    if (noSolution) return failingFor > 0 ? "Optimizer: no plan yet, the solves are failing" : "Optimizer: computing the ascent…";
                    // a re-solve or two can fail and the next succeed: only a plan older than a few seconds is worth a line
                    return failingFor > 3 ? $"Optimizer: re-solves failing, flying the plan from {GuiUtils.TimeToDHMS(failingFor)} ago" : "Optimizer: running";
                case OptimizerState.NO_PLAN:
                    return "Optimizer: no flight plan, no stages to optimize";
                case OptimizerState.STAGE_ENDING:
                    // the freeze comes before the end of each phase: a burn's end or, in a coast, the burn after it
                    return coasting
                        ? $"Optimizer: paused, the burn starts in {Math.Ceiling(seconds):F0} s"
                        : $"Optimizer: paused, the stage ends in {Math.Ceiling(seconds):F0} s";
                case OptimizerState.AFTER_STAGING:
                    return $"Optimizer: paused after staging, {Math.Ceiling(seconds):F0} s";
                case OptimizerState.ON_PAD:
                    return "Optimizer: paused until liftoff";
                case OptimizerState.AFTER_PHASE:
                    return $"Optimizer: paused while the next phase starts, {Math.Ceiling(seconds):F0} s";
                case OptimizerState.COAST_PAUSE:
                    return $"Optimizer: paused for the coast, resumes in {GuiUtils.TimeToDHMS(seconds)}";
                case OptimizerState.TERMINAL:
                    return "Optimizer: paused, terminal guidance";
                case OptimizerState.UNGUIDED_ONLY:
                    return "Optimizer: paused, only unguided stages left";
                case OptimizerState.FINISHED:
                    return "Optimizer: guidance finished";
                case OptimizerState.RETRYING:
                    return "Optimizer: can't plan with these settings, retrying";
                default:
                    return null;
            }
        }

        /// <summary>Why the PSG plan can't reach orbit with this rocket, in words, or null.</summary>
        internal string ReachWarning;

        // the burn lines round a shortfall under 2.5 m/s to zero as noise, so this does too
        private string PsgReachWarning()
        {
            MechJebModulePSGSimulation sim = Core.PSGSimulation;

            if (sim.DeltaVShortfall >= 2.5)
                return $"Not enough Δv: the rocket is {sim.DeltaVShortfall:F0} m/s short of the target orbit.";

            if (!double.IsNaN(sim.SuborbitalPeriapsis))
                return $"Not enough Δv for orbit: the planned orbit's periapsis is {sim.SuborbitalPeriapsis / 1000:F0} km, " +
                    (MainBody.atmosphere ? "inside the atmosphere." : "under the surface.");

            return null;
        }

        // The PSG status lines (one per burn/coast phase, then the terminal conditions, one per line), for the new UI whose rows are fixed
        internal const int MAX_PHASE_LINES = 15;
        internal readonly string[] PhaseLines = new string[MAX_PHASE_LINES];
        internal int PhaseLineCount;

        private void UpdatePhaseLines()
        {
            PhaseLineCount = 0;
            if (_ascentSettings.AscentType != AscentType.PSG || Core.Guidance.Solution == null) return;

            Solution solution = Core.Guidance.Solution;
            string[] terminal = solution.TerminalLines();
            for (int psgPhase = solution.Segments - 1; psgPhase >= 0 && PhaseLineCount < MAX_PHASE_LINES - terminal.Length; psgPhase--)
                PhaseLines[PhaseLineCount++] = PhaseString(solution, VesselState.MET, psgPhase);
            foreach (string line in terminal)
                PhaseLines[PhaseLineCount++] = line;
        }

        internal void RefreshRateChanged(int oldRate)
        {
            if (oldRate != _refreshRate)
                _refreshRate = Math.Max(_refreshRate, 1);
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

            // a coast during a stage splits it into two burns of the same MJ phase, so subtract every burn of this stage up to and
            // including this one to show what is left after it
            double usedDV = 0;
            for (int i = 0; i <= psgPhase; i++)
                if (!solution.CoastPhase(i) && solution.MJPhase(i) == mjPhase)
                    usedDV += solution.DV(t, i);

            double excessDV = stageDeltaV - usedDV;

            // eliminate some of the noise
            if (Math.Abs(excessDV) < 2.5) excessDV = 0;

            return $"burn: {kspStage} {solution.Tgo(t, psgPhase):F1}s {solution.DV(t, psgPhase):F1}m/s ({excessDV:F1}m/s)";
        }

        public override string GetName() => CachedLocalizer.Instance.MechJebAscentTitle; //"Ascent Guidance"

        public override string IconName() => "Ascent Guidance";
    }
}
