using System;
using JetBrains.Annotations;
using KSP.Localization;
using MechJebLib.Core;
using MechJebLib.PVG;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleAscentMenu : DisplayModule
    {
        private readonly string[] _ascentPathList =
        {
            Localizer.Format("#MechJeb_Ascent_ascentPathList1"), Localizer.Format("#MechJeb_Ascent_ascentPathList2"),
            Localizer.Format("#MechJeb_Ascent_ascentPathList3")
        }; // "Classic Ascent Profile", "Stock-style GravityTurn™", "Primer Vector Guidance (RSS/RO)"

        public MechJebModuleAscentMenu(MechJebCore core) : base(core) { }

        #region Delegators

        private bool _launchingToPlane
        {
            get => _ascentSettings.LaunchingToPlane;
            set => _ascentSettings.LaunchingToPlane = value;
        }

        private bool _launchingToRendezvous
        {
            get => _ascentSettings.LaunchingToRendezvous;
            set => _ascentSettings.LaunchingToRendezvous = value;
        }

        private bool _launchingToMatchLan
        {
            get => _ascentSettings.LaunchingToMatchLan;
            set => _ascentSettings.LaunchingToMatchLan = value;
        }

        private bool _launchingToLan
        {
            get => _ascentSettings.LaunchingToLan;
            set => _ascentSettings.LaunchingToLan = value;
        }

        #endregion

        private bool _launchingWithAnyPlaneControl => _launchingToPlane || _launchingToRendezvous || _launchingToMatchLan || _launchingToLan;

        private MechJebModuleAscentBaseAutopilot   _autopilot      => Core.Ascent;
        private MechJebModuleAscentSettings        _ascentSettings => Core.AscentSettings;
        private MechJebModuleAscentClassicPathMenu _classicPathMenu;
        private MechJebModuleAscentPVGSettingsMenu _pvgSettingsMenu;
        private MechJebModuleAscentSettingsMenu    _settingsMenu;

        public override void OnStart(PartModule.StartState state)
        {
            _pvgSettingsMenu = Core.GetComputerModule<MechJebModuleAscentPVGSettingsMenu>();
            _settingsMenu    = Core.GetComputerModule<MechJebModuleAscentSettingsMenu>();
            _classicPathMenu = Core.GetComputerModule<MechJebModuleAscentClassicPathMenu>();
        }

        [Persistent(pass = (int)Pass.GLOBAL)]
        private bool _lastPVGSettingsEnabled;

        [Persistent(pass = (int)Pass.GLOBAL)]
        private bool _lastSettingsMenuEnabled;

        protected override void OnModuleEnabled()
        {
            _pvgSettingsMenu.Enabled = _lastPVGSettingsEnabled;
            _settingsMenu.Enabled    = _lastSettingsMenuEnabled;
        }

        protected override void OnModuleDisabled()
        {
            _launchingToPlane        = false;
            _launchingToRendezvous   = false;
            _launchingToMatchLan     = false;
            _lastPVGSettingsEnabled  = _pvgSettingsMenu.Enabled;
            _lastSettingsMenuEnabled = _settingsMenu.Enabled;
            _pvgSettingsMenu.Enabled = false;
            _settingsMenu.Enabled    = false;
        }

        private static GUIStyle _btNormal, _btActive;

        private void SetupButtonStyles()
        {
            if (_btNormal == null)
            {
                _btNormal                  = new GUIStyle(GUI.skin.button);
                _btNormal.normal.textColor = _btNormal.focused.textColor = Color.white;
                _btNormal.hover.textColor  = _btNormal.active.textColor  = Color.yellow;
                _btNormal.onNormal.textColor =
                    _btNormal.onFocused.textColor = _btNormal.onHover.textColor = _btNormal.onActive.textColor = Color.green;
                _btNormal.padding = new RectOffset(8, 8, 8, 8);

                _btActive           = new GUIStyle(_btNormal);
                _btActive.active    = _btActive.onActive;
                _btActive.normal    = _btActive.onNormal;
                _btActive.onFocused = _btActive.focused;
                _btActive.hover     = _btActive.onHover;
            }
        }

        private void VisibleSectionsGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.TopButtons");
            GUILayout.BeginVertical(GUI.skin.box);

            if (_autopilot.Enabled && GUILayout.Button(CachedLocalizer.Instance.MechJebAscentButton1)) //Disengage autopilot
                _autopilot.Users.Remove(this);
            else if (!_autopilot.Enabled && GUILayout.Button(CachedLocalizer.Instance.MechJebAscentButton2)) //Engage autopilot
                _autopilot.Users.Add(this);
            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        private void ShowTargetingGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.ShowTargeting");
            GUILayout.BeginVertical(GUI.skin.box);

            if (_ascentSettings.AscentType == AscentType.PVG)
            {
                if (_ascentSettings.OptimizeStage >= 0)
                {
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel1, _ascentSettings.DesiredOrbitAltitude,
                        "km");                                                                                                   //Target Periapsis
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel2, _ascentSettings.DesiredApoapsis, "km"); //Target Apoapsis:
                    GuiUtils.ToggledTextBox(ref _ascentSettings.AttachAltFlag, CachedLocalizer.Instance.MechJebAscentAttachAlt,
                        _ascentSettings.DesiredAttachAlt, "km");
                }
                else
                {
                    if (!_launchingWithAnyPlaneControl)
                        GuiUtils.SimpleTextBox("Flight Path Angle", _ascentSettings.DesiredFPA, "°");
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentAttachAlt,
                        _ascentSettings.DesiredAttachAltFixed, "km");
                }

                if (_ascentSettings.DesiredApoapsis >= 0 && _ascentSettings.DesiredApoapsis < _ascentSettings.DesiredOrbitAltitude)
                    GUILayout.Label(CachedLocalizer.Instance.MechJebAscentLabel3, GuiUtils.yellowLabel); //Ap < Pe: circularizing orbit
                else if (_ascentSettings.AttachAltFlag && _ascentSettings.DesiredAttachAlt > _ascentSettings.DesiredApoapsis)
                    GUILayout.Label(CachedLocalizer.Instance.MechJebAscentWarnAttachAltHigh,
                        GuiUtils.orangeLabel); //Attach > Ap: apoapsis insertion
                if (_ascentSettings.DesiredApoapsis < 0)
                    GUILayout.Label(CachedLocalizer.Instance.MechJebAscentLabel4, GuiUtils.orangeLabel); //Hyperbolic target orbit (neg Ap)
                if (_ascentSettings.AttachAltFlag && _ascentSettings.DesiredAttachAlt < _ascentSettings.DesiredOrbitAltitude)
                    GUILayout.Label(CachedLocalizer.Instance.MechJebAscentWarnAttachAltLow,
                        GuiUtils.orangeLabel); //Attach < Pe: periapsis insertion
            }
            else
            {
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel5, _ascentSettings.DesiredOrbitAltitude, "km"); //Orbit altitude
            }

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel6, _ascentSettings.DesiredInclination, "º", 75, GuiUtils.skin.label,
                false);                                                                                        //Orbit inc.
            if (GUILayout.Button(CachedLocalizer.Instance.MechJebAscentButton13, GuiUtils.ExpandWidth(false))) //Current
                _ascentSettings.DesiredInclination.val = Math.Round(VesselState.latitude, 3);
            GUILayout.EndHorizontal();

            double delta = Math.Abs(VesselState.latitude) - Math.Abs(_ascentSettings.DesiredInclination);
            if (2.001 < delta)
                GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label7", delta), GuiUtils.redLabel); //inc {0:F1}º below current latitude

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        private void ShowGuidanceSettingsGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.ShowGuidanceSettings");


            if (_ascentSettings.AscentType == AscentType.GRAVITYTURN)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel8, _ascentSettings.TurnStartAltitude,
                    "km"); //Turn start altitude:
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel9, _ascentSettings.TurnStartVelocity,
                    "m/s");                                                                                                   //Turn start velocity:
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel10, _ascentSettings.TurnStartPitch, "deg"); //Turn start pitch:
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel11, _ascentSettings.IntermediateAltitude,
                    "km");                                                                                              //Intermediate altitude:
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel12, _ascentSettings.HoldAPTime, "s"); //Hold AP Time:
                GUILayout.EndVertical();
            }

            _ascentSettings.LimitQaEnabled = _ascentSettings.AscentType == AscentType.PVG; // this is mandatory for PVG

            Profiler.EndSample();
        }

        private void ShowStatusGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.ShowStatus");

            if (_ascentSettings.AscentType == AscentType.PVG)
            {
                GUILayout.BeginVertical(GUI.skin.box);

                if (Core.Guidance.Solution != null)
                {
                    Solution solution = Core.Guidance.Solution;
                    for (int pvgPhase = solution.Segments - 1; pvgPhase >= 0; pvgPhase--)
                        GUILayout.Label($"{PhaseString(solution, VesselState.time, pvgPhase)}");
                    GUILayout.Label(solution.TerminalString());
                }

                Profiler.BeginSample("MJ.GUIWindow.ShowStatus.Labels");
                GUILayout.BeginHorizontal();
                GUILayout.Label(vgo, GuiUtils.LayoutWidth(100));
                GUILayout.Label(heading, GuiUtils.LayoutWidth(100));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(tgo, GuiUtils.LayoutWidth(100));
                GUILayout.Label(pitch, GuiUtils.LayoutWidth(100));
                GUILayout.EndHorizontal();
                GUIStyle si;
                if (Core.Guidance.IsStable())
                    si = GuiUtils.greenLabel;
                else if (Core.Guidance.IsInitializing() || Core.Guidance.Status == PVGStatus.FINISHED)
                    si = GuiUtils.orangeLabel;
                else
                    si = GuiUtils.redLabel;
                GUILayout.Label(label26, si); //Guidance Status:
                GUILayout.BeginHorizontal();
                GUILayout.Label(label27, GuiUtils.LayoutWidth(90)); //converges:
                GUILayout.Label(label29);                           //staleness:

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(n, GuiUtils.LayoutWidth(90));
                GUILayout.Label(znorm);
                GUILayout.EndHorizontal();
                if (Core.Glueball.Exception != null)
                {
                    GUILayout.Label(label30, GuiUtils.redLabel); //LAST FAILURE:
                }

                Profiler.EndSample();
                GUILayout.EndVertical();
            }

            Profiler.EndSample();
        }

        private void ShowAutoWarpGUIElements()
        {
            if (!Vessel.LandedOrSplashed) return;
            const int LAN_WIDTH = 60;

            Profiler.BeginSample("MJ.GUIWindow.ShowAutoWarp");
            GUILayout.BeginVertical(GUI.skin.box);

            if (Core.Node.Autowarp)
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJebAscentLabel33, _ascentSettings.WarpCountDown, "s", 35); //Launch countdown:

            bool targetExists = Core.Target.NormalTargetExists;
            if (!_launchingWithAnyPlaneControl && !targetExists)
            {
                _launchingToPlane = _launchingToRendezvous = _launchingToMatchLan = false;
                GUILayout.Label(CachedLocalizer.Instance.MechJebAscentLabel34); //Select a target for a timed launch.
            }

            if (!_launchingWithAnyPlaneControl)
            {
                // Launch to Rendezvous
                if (targetExists && _ascentSettings.AscentType != AscentType.PVG
                                 && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJebAscentButton14, _ascentSettings.LaunchPhaseAngle, "º",
                                     width: 40)) //Launch to rendezvous:
                {
                    _launchingToRendezvous = true;
                    _autopilot.StartCountdown(VesselState.time +
                                              TimeToPhaseAngle(_ascentSettings.LaunchPhaseAngle,
                                                  MainBody, VesselState.longitude, Core.Target.TargetOrbit));
                }

                //Launch into plane of target
                if (targetExists && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJebAscentButton15, _ascentSettings.LaunchLANDifference, "º",
                        width: LAN_WIDTH)) //Launch into plane of target
                {
                    _launchingToPlane = true;
                    (double timeToPlane, double inclination) = Maths.MinimumTimeToPlane(
                        MainBody.rotationPeriod,
                        VesselState.latitude,
                        VesselState.celestialLongitude,
                        Core.Target.TargetOrbit.LAN - _ascentSettings.LaunchLANDifference,
                        Core.Target.TargetOrbit.inclination
                    );
                    _autopilot.StartCountdown(VesselState.time + timeToPlane);
                    _ascentSettings.DesiredInclination.val = inclination;
                }

                //Launch to target LAN
                if (targetExists && _ascentSettings.AscentType == AscentType.PVG
                                 && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJebAscentLaunchToTargetLan,
                                     _ascentSettings.LaunchLANDifference,
                                     "º", width: LAN_WIDTH)) //Launch to target LAN
                {
                    _launchingToMatchLan = true;
                    _autopilot.StartCountdown(VesselState.time +
                                              Maths.TimeToPlane(
                                                  MainBody.rotationPeriod,
                                                  VesselState.latitude,
                                                  VesselState.celestialLongitude,
                                                  Core.Target.TargetOrbit.LAN - _ascentSettings.LaunchLANDifference,
                                                  _ascentSettings.DesiredInclination
                                              )
                    );
                }

                //Launch to LAN
                if (_ascentSettings.AscentType == AscentType.PVG)
                {
                    if (GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJebAscentLaunchToLan, _ascentSettings.DesiredLan, "º",
                            width: LAN_WIDTH)) //Launch to LAN
                    {
                        _launchingToLan = true;
                        _autopilot.StartCountdown(VesselState.time +
                                                  Maths.TimeToPlane(
                                                      MainBody.rotationPeriod,
                                                      VesselState.latitude,
                                                      VesselState.celestialLongitude,
                                                      _ascentSettings.DesiredLan,
                                                      _ascentSettings.DesiredInclination
                                                  )
                        );
                    }
                }
            }

            if (_launchingWithAnyPlaneControl)
            {
                GUILayout.Label(launchTimer);
                if (GUILayout.Button(CachedLocalizer.Instance.MechJebAscentButton17)) //Abort
                    _launchingToPlane = _launchingToRendezvous = _launchingToMatchLan = _launchingToLan = _autopilot.TimedLaunch = false;
            }

            _ascentSettings.OverrideWarpToPlane = GUILayout.Toggle(_ascentSettings.OverrideWarpToPlane, "Override Warp to Plane");

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        private DateTime _lastRefresh = DateTime.MinValue;
        private string   vgo, heading, tgo, pitch, label26, label27, label28, n, label29, znorm, label30, launchTimer, autopilotStatus;
        private TimeSpan _refreshInterval = TimeSpan.FromSeconds(0.1);

        [Persistent(pass = (int)Pass.GLOBAL)]
        private EditableInt _refreshRate = 10;

        private void UpdateStrings()
        {
            DateTime now = DateTime.Now;
            if (now <= _lastRefresh + _refreshInterval) return;

            _lastRefresh = now;
            Profiler.BeginSample("MJ.GUIWindow.UpdateStrings.StringOps");
            vgo     = $"vgo: {Core.Guidance.VGO:F1}";
            heading = $"heading: {Core.Guidance.Heading:F1}";
            tgo     = $"tgo: {Core.Guidance.Tgo:F3}";
            pitch   = $"pitch: {Core.Guidance.Pitch:F1}";
            label26 = $"{CachedLocalizer.Instance.MechJebAscentLabel26}{Core.Guidance.Status}";
            label27 = $"{CachedLocalizer.Instance.MechJebAscentLabel27}{Core.Glueball.SuccessfulConverges}";
            label28 = $"{CachedLocalizer.Instance.MechJebAscentLabel28}{Core.Glueball.LastLmStatus}";
            n       = $"n: {Core.Glueball.LastLmIterations}({Core.Glueball.MaxLmIterations})";
            label29 = $"{CachedLocalizer.Instance.MechJebAscentLabel29} {GuiUtils.TimeToDHMS(Core.Glueball.Staleness)}";
            znorm   = $"znorm: {Core.Glueball.LastZnorm:G5}";
            if (Core.Glueball.Exception != null)
                label30 = $"{CachedLocalizer.Instance.MechJebAscentLabel30}{Core.Glueball.Exception.Message}";

            if (_launchingToPlane) launchTimer           = CachedLocalizer.Instance.MechJebAscentMsg2;                 //Launching to target plane
            else if (_launchingToRendezvous) launchTimer = CachedLocalizer.Instance.MechJebAscentMsg3;                 //Launching to rendezvous
            else if (_launchingToMatchLan) launchTimer   = CachedLocalizer.Instance.MechJebAscentLaunchingToTargetLAN; //Launching to target LAN
            else if (_launchingToLan) launchTimer        = CachedLocalizer.Instance.MechJebAscentLaunchingToManualLAN; //Launching to manual LAN
            else launchTimer                             = string.Empty;
            if (_autopilot.TMinus > 3 * VesselState.deltaT)
                launchTimer += $": T-{GuiUtils.TimeToDHMS(_autopilot.TMinus, 1)}";

            autopilotStatus = CachedLocalizer.Instance.MechJebAscentLabel35 + _autopilot.Status;
            Profiler.EndSample();
        }

        private void RefreshRateGUI()
        {
            int oldRate = _refreshRate;
            if (GuiUtils.showAdvancedWindowSettings)
                GuiUtils.SimpleTextBox("Update Interval", _refreshRate, "Hz");
            if (oldRate != _refreshRate)
            {
                _refreshRate     = Math.Max(_refreshRate, 1);
                _refreshInterval = TimeSpan.FromSeconds(1d / _refreshRate);
            }
        }

        protected override void WindowGUI(int windowID)
        {
            SetupButtonStyles();
            GUILayout.BeginVertical();

            UpdateStrings();
            //PerformanceTestGUIElements();
            VisibleSectionsGUIElements();
            ShowTargetingGUIElements();
            ShowGuidanceSettingsGUIElements();


            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            _settingsMenu.Enabled = GUILayout.Toggle(_settingsMenu.Enabled, "Ascent Settings");

            if (_ascentSettings.AscentType == AscentType.PVG)
            {
                Core.StageStats.RequestUpdate();
                _pvgSettingsMenu.Enabled = GUILayout.Toggle(_pvgSettingsMenu.Enabled, "PVG Settings");
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            ShowStatusGUIElements();
            ShowAutoWarpGUIElements();

            if (_autopilot.Enabled) GUILayout.Label(autopilotStatus); //Autopilot status:
            if (Core.DeactivateControl)
                GUILayout.Label(CachedLocalizer.Instance.MechJebAscentLabel36, GuiUtils.redLabel); //CONTROL DISABLED (AVIONICS)

            if (!Vessel.patchedConicsUnlocked() && _ascentSettings.AscentType != AscentType.PVG)
            {
                GUILayout.Label(CachedLocalizer.Instance
                    .MechJebAscentLabel37); //"Warning: MechJeb is unable to circularize without an upgraded Tracking Station."
            }

            GUILayout.BeginHorizontal();
            if (_ascentSettings.AscentType == AscentType.PVG)
            {
                if (GUILayout.Button("Reset to PVG/RO Defaults"))
                    _ascentSettings.ApplyRODefaults();
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            _ascentSettings.AscentType = (AscentType)GuiUtils.ComboBox.Box((int)_ascentSettings.AscentType, _ascentPathList, this);
            GUILayout.EndHorizontal();

            if (_ascentSettings.AscentType == AscentType.CLASSIC)
                _classicPathMenu.Enabled =
                    GUILayout.Toggle(_classicPathMenu.Enabled, CachedLocalizer.Instance.MechJebAscentCheckbox10); //Edit ascent path

            RefreshRateGUI();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        //Computes the time until the phase angle between the launchpad and the target equals the given angle.
        //The convention used is that phase angle is the angle measured starting at the target and going east until
        //you get to the launchpad.
        //The time returned will not be exactly accurate unless the target is in an exactly circular orbit. However,
        //the time returned will go to exactly zero when the desired phase angle is reached.
        private static double TimeToPhaseAngle(double phaseAngle, CelestialBody launchBody, double launchLongitude, Orbit target)
        {
            double launchpadAngularRate = 360 / launchBody.rotationPeriod;
            double targetAngularRate = 360.0 / target.period;
            if (Vector3d.Dot(-target.GetOrbitNormal().xzy.normalized, launchBody.angularVelocity) < 0)
                targetAngularRate *= -1; //retrograde target

            Vector3d currentLaunchpadDirection = launchBody.GetSurfaceNVector(0, launchLongitude);
            Vector3d currentTargetDirection = target.WorldBCIPositionAtUT(Planetarium.GetUniversalTime());
            currentTargetDirection = Vector3d.Exclude(launchBody.angularVelocity, currentTargetDirection);

            double currentPhaseAngle = Math.Abs(Vector3d.Angle(currentLaunchpadDirection, currentTargetDirection));
            if (Vector3d.Dot(Vector3d.Cross(currentTargetDirection, currentLaunchpadDirection), launchBody.angularVelocity) < 0)
            {
                currentPhaseAngle = 360 - currentPhaseAngle;
            }

            double phaseAngleRate = launchpadAngularRate - targetAngularRate;

            double phaseAngleDifference = MuUtils.ClampDegrees360(phaseAngle - currentPhaseAngle);

            if (phaseAngleRate < 0)
            {
                phaseAngleRate       *= -1;
                phaseAngleDifference =  360 - phaseAngleDifference;
            }


            return phaseAngleDifference / phaseAngleRate;
        }

        private string PhaseString(Solution solution, double t, int pvgPhase)
        {
            int mjPhase = solution.MJPhase(pvgPhase);
            int kspStage = solution.KSPStage(pvgPhase);

            if (solution.CoastPhase(pvgPhase))
                return $"coast: {kspStage} {solution.Tgo(t, pvgPhase):F1}s";

            double stageDeltaV = 0;

            if (mjPhase < Core.StageStats.VacStats.Count)
                stageDeltaV = Core.StageStats.VacStats[mjPhase].DeltaV;

            double excessDV = stageDeltaV - solution.DV(t, pvgPhase);

            // eliminate some of the noise
            if (Math.Abs(excessDV) < 2.5) excessDV = 0;

            return $"burn: {kspStage} {solution.Tgo(t, pvgPhase):F1}s {solution.DV(t, pvgPhase):F1}m/s ({excessDV:F1}m/s)";
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(275), GUILayout.Height(30) };

        public override string GetName() => CachedLocalizer.Instance.MechJebAscentTitle; //"Ascent Guidance"

        public override string IconName() => "Ascent Guidance";
    }
}
