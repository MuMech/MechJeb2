﻿using System;
using JetBrains.Annotations;
using KSP.Localization;
using MechJebLib.Maths;
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

        private MechJebModuleAscentBaseAutopilot   _autopilot      => core.ascent;
        private MechJebModuleAscentSettings        _ascentSettings => core.ascentSettings;
        private MechJebModuleAscentClassicPathMenu _classicPathMenu;
        private MechJebModuleAscentPVGSettingsMenu  _pvgSettingsMenu;
        private MechJebModuleAscentSettingsMenu  _settingsMenu;


        public override void OnStart(PartModule.StartState state)
        {
            _pvgSettingsMenu  = core.GetComputerModule<MechJebModuleAscentPVGSettingsMenu>();
            _settingsMenu  = core.GetComputerModule<MechJebModuleAscentSettingsMenu>();
            _classicPathMenu = core.GetComputerModule<MechJebModuleAscentClassicPathMenu>();
        }

        [Persistent(pass = (int)Pass.Global)]
        private bool _lastPVGSettingsEnabled  = false;

        [Persistent(pass = (int)Pass.Global)]
        private bool _lastSettingsMenuEnabled = false;

        public override void OnModuleEnabled()
        {
            _pvgSettingsMenu.enabled = _lastPVGSettingsEnabled;
            _settingsMenu.enabled    = _lastSettingsMenuEnabled;
        }

        public override void OnModuleDisabled()
        {
            _launchingToPlane        = false;
            _launchingToRendezvous   = false;
            _launchingToMatchLan     = false;
            _lastPVGSettingsEnabled  = _pvgSettingsMenu.enabled;
            _lastSettingsMenuEnabled = _settingsMenu.enabled;
            _pvgSettingsMenu.enabled = false;
            _settingsMenu.enabled    = false;
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

            if (_autopilot.enabled && GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button1)) //Disengage autopilot
                _autopilot.users.Remove(this);
            else if (!_autopilot.enabled && GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button2)) //Engage autopilot
                _autopilot.users.Add(this);
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
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label1, _ascentSettings.DesiredOrbitAltitude, "km");                                                                                                     //Target Periapsis
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label2, _ascentSettings.DesiredApoapsis, "km"); //Target Apoapsis:
                }
                else
                {
                    if (!_launchingWithAnyPlaneControl)
                        GuiUtils.SimpleTextBox("Flight Path Angle", _ascentSettings.DesiredFPA, "°");
                    _ascentSettings.AttachAltFlag = true;
                }
                GuiUtils.ToggledTextBox(ref _ascentSettings.AttachAltFlag, CachedLocalizer.Instance.MechJeb_Ascent_attachAlt, _ascentSettings.DesiredAttachAlt, "km");

                if (_ascentSettings.DesiredApoapsis >= 0 && _ascentSettings.DesiredApoapsis < _ascentSettings.DesiredOrbitAltitude)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label3, GuiUtils.yellowLabel); //Ap < Pe: circularizing orbit
                else if (_ascentSettings.AttachAltFlag && _ascentSettings.DesiredAttachAlt > _ascentSettings.DesiredApoapsis)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_warnAttachAltHigh,
                        GuiUtils.orangeLabel); //Attach > Ap: apoapsis insertion
                if (_ascentSettings.DesiredApoapsis < 0)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label4, GuiUtils.orangeLabel); //Hyperbolic target orbit (neg Ap)
                if (_ascentSettings.AttachAltFlag && _ascentSettings.DesiredAttachAlt < _ascentSettings.DesiredOrbitAltitude)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_warnAttachAltLow,
                        GuiUtils.orangeLabel); //Attach < Pe: periapsis insertion
            }
            else
            {
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label5, _ascentSettings.DesiredOrbitAltitude, "km"); //Orbit altitude
            }

            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label6, _ascentSettings.DesiredInclination, "º", 75, GuiUtils.skin.label,
                false);                                                                                          //Orbit inc.
            if (GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button13, GuiUtils.ExpandWidth(false))) //Current
                _ascentSettings.DesiredInclination.val = Math.Round(vesselState.latitude, 3);
            GUILayout.EndHorizontal();

            double delta = Math.Abs(vesselState.latitude) - Math.Abs(_ascentSettings.DesiredInclination);
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

                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label8, _ascentSettings.TurnStartAltitude,
                    "km"); //Turn start altitude:
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label9, _ascentSettings.TurnStartVelocity,
                    "m/s");                                                                                                     //Turn start velocity:
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label10, _ascentSettings.TurnStartPitch, "deg"); //Turn start pitch:
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label11, _ascentSettings.IntermediateAltitude,
                    "km");                                                                                                //Intermediate altitude:
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label12, _ascentSettings.HoldAPTime, "s"); //Hold AP Time:
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

                if (core.guidance.Solution != null)
                {
                    Solution solution = core.guidance.Solution;
                    for (int i = solution.Segments-1; i >= 0; i--)
                        GUILayout.Label($"{PhaseString(solution, vesselState.time, i)}");
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
                if (core.guidance.IsStable())
                    si = GuiUtils.greenLabel;
                else if (core.guidance.IsInitializing() || core.guidance.Status == PVGStatus.FINISHED)
                    si = GuiUtils.orangeLabel;
                else
                    si = GuiUtils.redLabel;
                GUILayout.Label(label26, si); //Guidance Status:
                GUILayout.BeginHorizontal();
                GUILayout.Label(label27, GuiUtils.LayoutWidth(90)); //converges:
                GUILayout.Label(label29); //staleness:

                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(n, GuiUtils.LayoutWidth(90));
                GUILayout.Label(znorm);
                GUILayout.EndHorizontal();
                if (core.glueball.Exception != null)
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
            if (!vessel.LandedOrSplashed) return;
            const int LAN_WIDTH = 60;

            Profiler.BeginSample("MJ.GUIWindow.ShowAutoWarp");
            GUILayout.BeginVertical(GUI.skin.box);

            if (core.node.autowarp)
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label33, _ascentSettings.WarpCountDown, "s", 35); //Launch countdown:

            bool targetExists = core.target.NormalTargetExists;
            if (!_launchingWithAnyPlaneControl && !targetExists)
            {
                _launchingToPlane = _launchingToRendezvous = _launchingToMatchLan = false;
                GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label34); //Select a target for a timed launch.
            }

            if (!_launchingWithAnyPlaneControl)
            {
                // Launch to Rendezvous
                if (targetExists && _ascentSettings.AscentType != AscentType.PVG
                                 && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_button14, _ascentSettings.LaunchPhaseAngle, "º",
                                     width: 40)) //Launch to rendezvous:
                {
                    _launchingToRendezvous = true;
                    _autopilot.StartCountdown(vesselState.time +
                                              TimeToPhaseAngle(_ascentSettings.LaunchPhaseAngle,
                                                  mainBody, vesselState.longitude, core.target.TargetOrbit));
                }

                //Launch into plane of target
                if (targetExists && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_button15, _ascentSettings.LaunchLANDifference, "º",
                        width: LAN_WIDTH)) //Launch into plane of target
                {
                    _launchingToPlane = true;
                    (double timeToPlane, double inclination) = Functions.MinimumTimeToPlane(
                        mainBody.rotationPeriod,
                        vesselState.latitude,
                        vesselState.celestialLongitude,
                        core.target.TargetOrbit.LAN - _ascentSettings.LaunchLANDifference,
                        core.target.TargetOrbit.inclination
                    );
                    _autopilot.StartCountdown(vesselState.time + timeToPlane);
                    _ascentSettings.DesiredInclination.val = inclination;
                }

                //Launch to target LAN
                if (targetExists && _ascentSettings.AscentType == AscentType.PVG
                                 && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_LaunchToTargetLan,
                                     _ascentSettings.LaunchLANDifference,
                                     "º", width: LAN_WIDTH)) //Launch to target LAN
                {
                    _launchingToMatchLan = true;
                    _autopilot.StartCountdown(vesselState.time +
                                              Functions.TimeToPlane(
                                                  mainBody.rotationPeriod,
                                                  vesselState.latitude,
                                                  vesselState.celestialLongitude,
                                                  core.target.TargetOrbit.LAN - _ascentSettings.LaunchLANDifference,
                                                  _ascentSettings.DesiredInclination
                                              )
                    );
                }

                //Launch to LAN
                if (_ascentSettings.AscentType == AscentType.PVG)
                {
                    if (GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_LaunchToLan, _ascentSettings.DesiredLan, "º",
                            width: LAN_WIDTH)) //Launch to LAN
                    {
                        _launchingToLan = true;
                        _autopilot.StartCountdown(vesselState.time +
                                                  Functions.TimeToPlane(
                                                      mainBody.rotationPeriod,
                                                      vesselState.latitude,
                                                      vesselState.celestialLongitude,
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
                if (GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button17)) //Abort
                    _launchingToPlane = _launchingToRendezvous = _launchingToMatchLan = _launchingToLan = _autopilot.TimedLaunch = false;
            }

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        private DateTime _lastRefresh = DateTime.MinValue;
        private string vgo, heading, tgo, pitch, label26, label27, label28, n, label29, znorm, label30, launchTimer, autopilotStatus;
        private TimeSpan _refreshInterval = TimeSpan.FromSeconds(0.1);

        [Persistent(pass = (int)Pass.Global)] private EditableInt _refreshRate = 10;

        private void UpdateStrings()
        {
            DateTime now = DateTime.Now;
            if (now <= _lastRefresh + _refreshInterval) return;

            _lastRefresh = now;
            Profiler.BeginSample("MJ.GUIWindow.UpdateStrings.StringOps");
            vgo     = $"vgo: {core.guidance.VGO:F1}";
            heading = $"heading: {core.guidance.Heading:F1}";
            tgo     = $"tgo: {core.guidance.Tgo:F3}";
            pitch   = $"pitch: {core.guidance.Pitch:F1}";
            label26 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label26}{core.guidance.Status}";
            label27 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label27}{core.glueball.SuccessfulConverges}";
            label28 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label28}{core.glueball.LastLmStatus}";
            n       = $"n: {core.glueball.LastLmIterations}({core.glueball.MaxLmIterations})";
            label29 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label29} {GuiUtils.TimeToDHMS(core.glueball.Staleness)}";
            znorm   = $"znorm: {core.glueball.LastZnorm:G5}";
            if (core.glueball.Exception != null)
               label30 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label30}{core.glueball.Exception.Message}";

            if (_launchingToPlane) launchTimer           = CachedLocalizer.Instance.MechJeb_Ascent_msg2;                 //Launching to target plane
            else if (_launchingToRendezvous) launchTimer = CachedLocalizer.Instance.MechJeb_Ascent_msg3;                 //Launching to rendezvous
            else if (_launchingToMatchLan) launchTimer   = CachedLocalizer.Instance.MechJeb_Ascent_LaunchingToTargetLAN; //Launching to target LAN
            else if (_launchingToLan) launchTimer        = CachedLocalizer.Instance.MechJeb_Ascent_LaunchingToManualLAN; //Launching to manual LAN
            else launchTimer                             = string.Empty;
            if (_autopilot.TMinus > 3 * vesselState.deltaT)
                launchTimer += $": T-{GuiUtils.TimeToDHMS(_autopilot.TMinus, 1)}";

            autopilotStatus = CachedLocalizer.Instance.MechJeb_Ascent_label35 + _autopilot.Status;
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

            if (_ascentSettings.AscentType == AscentType.PVG)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                _pvgSettingsMenu.enabled = GUILayout.Toggle(_pvgSettingsMenu.enabled, "Edit Rocket Staging");
                GUILayout.EndVertical();
            }

            GUILayout.BeginVertical(GUI.skin.box);
            _settingsMenu.enabled = GUILayout.Toggle(_settingsMenu.enabled, "Edit Ascent Settings");
            GUILayout.EndVertical();

            ShowStatusGUIElements();
            ShowAutoWarpGUIElements();

            if (_autopilot.enabled) GUILayout.Label(autopilotStatus); //Autopilot status:
            if (core.DeactivateControl)
                GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label36, GuiUtils.redLabel); //CONTROL DISABLED (AVIONICS)

            if (!vessel.patchedConicsUnlocked() && _ascentSettings.AscentType != AscentType.PVG)
            {
                GUILayout.Label(CachedLocalizer.Instance
                    .MechJeb_Ascent_label37); //"Warning: MechJeb is unable to circularize without an upgraded Tracking Station."
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
                _classicPathMenu.enabled =
                    GUILayout.Toggle(_classicPathMenu.enabled, CachedLocalizer.Instance.MechJeb_Ascent_checkbox10); //Edit ascent path

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

        private string PhaseString(Solution solution, double t, int n)
        {
            if (solution.CoastPhase(n))
                return $"coast: {solution.Tgo(t, n):F1}s";

            double stageDeltaV = 0;

            int kspStage = solution.KSPStage(n);

            if (kspStage < core.stageStats.vacStats.Length)
                 stageDeltaV = core.stageStats.vacStats[kspStage].DeltaV;

            double excessDV = stageDeltaV - solution.DV(t, n);

            // eliminate some of the noise
            if (Math.Abs(excessDV) < 2.5) excessDV = 0;

            return $"burn: {kspStage} {solution.Tgo(t, n):F1}s {solution.DV(t, n):F1}m/s ({excessDV:F1}m/s)";
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(275), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return CachedLocalizer.Instance.MechJeb_Ascent_title; //"Ascent Guidance"
        }

        public override string IconName()
        {
            return "Ascent Guidance";
        }
    }
}
