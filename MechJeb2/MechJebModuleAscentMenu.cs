using System;
using KSP.Localization;
using MechJebLib.Maths;
using UnityEngine;
using UnityEngine.Profiling;

namespace MuMech
{
    public class MechJebModuleAscentMenu : DisplayModule
    {


        private readonly string[] _ascentPathList =
        {
            Localizer.Format("#MechJeb_Ascent_ascentPathList1"), Localizer.Format("#MechJeb_Ascent_ascentPathList2"),
            Localizer.Format("#MechJeb_Ascent_ascentPathList3")
        }; // "Classic Ascent Profile", "Stock-style GravityTurn™", "Primer Vector Guidance (RSS/RO)"

        public MechJebModuleAscentMenu(MechJebCore core) : base(core) { }


        public bool launchingToPlane;
        public bool launchingToRendezvous;
        public bool launchingToMatchLAN;
        public bool launchingToLAN;
        public bool Launching => launchingToPlane || launchingToRendezvous || launchingToMatchLAN || launchingToLAN;

        private MechJebModuleAscentBaseAutopilot           _autopilot    => _ascentSettings.AscentAutopilot;
        private MechJebModuleAscentClassicPathMenu    _classicPathMenu => core.GetComputerModule<MechJebModuleAscentClassicPathMenu>();
        private MechJebModuleAscentPVGStagingMenu _pvgStagingMenu;
        private MechJebModuleAscentSettings       _ascentSettings;

        public override void OnStart(PartModule.StartState state)
        {
            _pvgStagingMenu = core.GetComputerModule<MechJebModuleAscentPVGStagingMenu>();
            _ascentSettings = core.GetComputerModule<MechJebModuleAscentSettings>();
        }

        public override void OnModuleDisabled()
        {
            launchingToPlane      = false;
            launchingToRendezvous = false;
            launchingToMatchLAN   = false;
        }

        private static GUIStyle _btNormal, _btActive;

        private bool   toggle1, toggle2;
        private string simpleText = "SimpleText";

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

        // Dev instrumentation: remove when ready to merge the PR.
        private void PerformanceTestGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.StringOps");
            string sToggle1 = $"{toggle1}";
            string sToggle2 = $"Toggle2: {toggle2}";
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.HorizontalGroupPair");
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneButton");
            if (GUILayout.Button("Swap toggle1")) toggle1 = !toggle1;
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneLabel");
            GUILayout.Label(sToggle1);
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneToggle");
            toggle2 = GUILayout.Toggle(toggle2, sToggle2);
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneSimpleTextBox");
            GuiUtils.SimpleTextBox("SimpleTextBox", _ascentSettings.desiredOrbitAltitude, "pfx"); //Target Periapsis
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneTextField");
            simpleText = GUILayout.TextField(simpleText);
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneSimpleTextField");
            GuiUtils.SimpleTextField(_ascentSettings.DesiredAttachAlt);
            Profiler.EndSample();
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

            if (_ascentSettings.ascentType == ascentType.PVG)
            {
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label1, _ascentSettings.desiredOrbitAltitude, "km");  //Target Periapsis
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label2, _ascentSettings.DesiredApoapsis, "km"); //Target Apoapsis:
                GuiUtils.ToggledTextBox(ref _ascentSettings.AttachAltFlag, CachedLocalizer.Instance.MechJeb_Ascent_attachAlt, _ascentSettings.DesiredAttachAlt,
                    "km");

                if (_ascentSettings.DesiredApoapsis >= 0 && _ascentSettings.DesiredApoapsis < _ascentSettings.desiredOrbitAltitude)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label3, GuiUtils.yellowLabel); //Ap < Pe: circularizing orbit
                else if (_ascentSettings.AttachAltFlag && _ascentSettings.DesiredAttachAlt > _ascentSettings.DesiredApoapsis)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_warnAttachAltHigh,
                        GuiUtils.orangeLabel); //Attach > Ap: apoapsis insertion
                if (_ascentSettings.DesiredApoapsis < 0)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label4, GuiUtils.orangeLabel); //Hyperbolic target orbit (neg Ap)
                if (_ascentSettings.AttachAltFlag && _ascentSettings.DesiredAttachAlt < _ascentSettings.desiredOrbitAltitude)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_warnAttachAltLow,
                        GuiUtils.orangeLabel); //Attach < Pe: periapsis insertion
            }
            else
            {
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label5, _ascentSettings.desiredOrbitAltitude, "km"); //Orbit altitude
            }
            
            GUIStyle si = Math.Abs(_ascentSettings.desiredInclination) < Math.Abs(vesselState.latitude) - 2.001 ? GuiUtils.orangeLabel : GuiUtils.skin.label;
            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label6, _ascentSettings.desiredInclination, "º", 75, si, false); //Orbit inc.
            if (GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button13, GuiUtils.ExpandWidth(false)))            //Current
                _ascentSettings.desiredInclination = Math.Round(vesselState.latitude, 2);
            GUILayout.EndHorizontal();

            double delta = Math.Abs(vesselState.latitude) - Math.Abs(_ascentSettings.desiredInclination);
            if (2.001 < delta)
                GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label7", delta), si); //inc {0:F1}º below current latitude
            
            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        private void ShowGuidanceSettingsGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.ShowGuidanceSettings");


                GUILayout.BeginVertical(GUI.skin.box);
                if (_ascentSettings.ascentType == ascentType.GRAVITYTURN)
                {
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label8, _ascentSettings.turnStartAltitude, "km");  //Turn start altitude:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label9, _ascentSettings.turnStartVelocity, "m/s"); //Turn start velocity:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label10, _ascentSettings.turnStartPitch, "deg");   //Turn start pitch:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label11, _ascentSettings.intermediateAltitude,
                        "km");                                                                                                //Intermediate altitude:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label12, _ascentSettings.holdAPTime, "s"); //Hold AP Time:
                }
                else if (_ascentSettings.ascentType == ascentType.PVG)
                {
                    _pvgStagingMenu.enabled = GUILayout.Toggle(_pvgStagingMenu.enabled, "Edit Rocket Staging");
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label13, _ascentSettings.PitchStartVelocity, "m/s", 40);                                                                                       //Booster Pitch start:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label14, _ascentSettings.PitchRate, "°/s", 40); //Booster Pitch rate:
                    GuiUtils.SimpleTextBox("Q Trigger:", _ascentSettings.DynamicPressureTrigger, "kPa", 40);
                    GuiUtils.ToggledTextBox(ref _ascentSettings.StagingTriggerFlag, "PVG After Stage:", _ascentSettings.StagingTrigger, width: 40);
                    //GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label15, core.guidance.pvgInterval, "s", 40); //Guidance Interval:
                    //if (core.guidance.pvgInterval < 1 || core.guidance.pvgInterval > 30)
                    //{
                    //    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label16,
                    //        GuiUtils.yellowLabel); //Guidance intervals are limited to between 1s and 30s
                    //}

                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label17, _ascentSettings.limitQa, "Pa-rad"); //Qα limit
                    if (_ascentSettings.limitQa < 100 || _ascentSettings.limitQa > 4000)
                    {
                        if (_ascentSettings.limitQa < 100)
                            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label18,
                                GuiUtils.yellowLabel); //Qα limit cannot be set to lower than 100 Pa-rad
                        else if (_ascentSettings.limitQa > 10000)
                            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label19,
                                GuiUtils.yellowLabel); //Qα limit cannot be set to higher than 10000 Pa-rad
                        else
                            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label20,
                                GuiUtils.yellowLabel); //Qα limit is recommended to be 1000 to 4000 Pa-rad
                    }
                }

                GUILayout.EndVertical();

            _ascentSettings.limitQaEnabled = _ascentSettings.ascentType == ascentType.PVG; // this is mandatory for PVG

            Profiler.EndSample();
        }

        private readonly string sClimb = $"{CachedLocalizer.Instance.MechJeb_Ascent_label22}: ";
        private readonly string sTurn  = $"{CachedLocalizer.Instance.MechJeb_Ascent_label23}: ";

        private void ShowAscentSettingsGUIElements(out bool forceRoll, out bool correctiveSteering, out bool limitAoA, out bool autostage)
        {
            forceRoll          = _ascentSettings.forceRoll;
            correctiveSteering = _ascentSettings.correctiveSteering;
            limitAoA           = _ascentSettings.limitAoA;
            autostage          = _ascentSettings.autostage;

            Profiler.BeginSample("MJ.GUIWindow.AscentItems");
            GUILayout.BeginVertical(GUI.skin.box);

            core.thrust.LimitToPreventOverheatsInfoItem();
            core.thrust.LimitToMaxDynamicPressureInfoItem();
            core.thrust.LimitAccelerationInfoItem();
            if (_ascentSettings.ascentType != ascentType.PVG) core.thrust.LimitThrottleInfoItem();
            core.thrust.LimiterMinThrottleInfoItem();
            if (_ascentSettings.ascentType != ascentType.PVG) core.thrust.LimitElectricInfoItem();

            if (_ascentSettings.ascentType == ascentType.PVG)
            {
                // GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label21")) ;//FIXME: g-limiter is down for maintenance
                core.thrust.limitThrottle           = false;
                core.thrust.limitToTerminalVelocity = false;
                core.thrust.electricThrottle        = false;
            }

            forceRoll = GUILayout.Toggle(_ascentSettings.forceRoll, CachedLocalizer.Instance.MechJeb_Ascent_checkbox2); //Force Roll
            if (_ascentSettings.forceRoll)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Space(15);
                GuiUtils.SimpleTextBox(sClimb, _ascentSettings.verticalRoll, "º", 30); //climb
                GUILayout.FlexibleSpace();
                GuiUtils.SimpleTextBox(sTurn, _ascentSettings.turnRoll, "º", 30); //turn
                GUILayout.FlexibleSpace();
                GuiUtils.SimpleTextBox("Alt: ", _ascentSettings.rollAltitude, "m", 30);
                GUILayout.EndHorizontal();
            }

            if (_ascentSettings.ascentType != ascentType.PVG)
            {
                GUIStyle s = _ascentSettings.limitingAoA ? GuiUtils.greenToggle : null;
                GuiUtils.ToggledTextBox(ref limitAoA, CachedLocalizer.Instance.MechJeb_Ascent_checkbox3, _ascentSettings.maxAoA, sCurrentMaxAoA, s,
                    30); //Limit AoA to

                if (_ascentSettings.limitAoA)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    GUIStyle sl = _ascentSettings.limitingAoA && vesselState.dynamicPressure < _ascentSettings.aoALimitFadeoutPressure
                        ? GuiUtils.greenLabel
                        : GuiUtils.skin.label;
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label24, _ascentSettings.aoALimitFadeoutPressure, "Pa", 50,
                        sl); //Dynamic Pressure Fadeout
                    GUILayout.EndHorizontal();
                }

                _ascentSettings.limitQaEnabled = false; // this is only for PVG
            }

            if (_ascentSettings.ascentType == ascentType.CLASSIC)
            {
                // corrective steering only applies to Classic
                GUILayout.BeginHorizontal();
                correctiveSteering = GUILayout.Toggle(correctiveSteering, CachedLocalizer.Instance.MechJeb_Ascent_checkbox4,
                    GuiUtils.ExpandWidth(false)); //Corrective steering
                if (_ascentSettings.correctiveSteering)
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label25, _ascentSettings.correctiveSteeringGain, width: 40,
                        horizontalFraming: false); // Gain
                GUILayout.EndHorizontal();
            }

            autostage = GUILayout.Toggle(autostage, CachedLocalizer.Instance.MechJeb_Ascent_checkbox5); //Autostage
            if (_ascentSettings.autostage) core.staging.AutostageSettingsInfoItem();

            _ascentSettings.autodeploySolarPanels =
                GUILayout.Toggle(_ascentSettings.autodeploySolarPanels, CachedLocalizer.Instance.MechJeb_Ascent_checkbox6); //Auto-deploy solar panels
            _ascentSettings.autoDeployAntennas =
                GUILayout.Toggle(_ascentSettings.autoDeployAntennas, CachedLocalizer.Instance.MechJeb_Ascent_checkbox7); //Auto-deploy antennas

            GUILayout.BeginHorizontal();
            core.node.autowarp = GUILayout.Toggle(core.node.autowarp, CachedLocalizer.Instance.MechJeb_Ascent_checkbox8); //Auto-warp
            if (_ascentSettings.ascentType != ascentType.PVG)
            {
                _ascentSettings.skipCircularization =
                    GUILayout.Toggle(_ascentSettings.skipCircularization, CachedLocalizer.Instance.MechJeb_Ascent_checkbox9); //Skip Circularization
            }
            else
            {
                // skipCircularization is always true for Optimizer
                _ascentSettings.skipCircularization = true;
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        private void ShowStatusGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.ShowStatus");
            GUILayout.BeginVertical(GUI.skin.box);

            if (_ascentSettings.ascentType == ascentType.PVG)
            {
                //if (core.guidance.Solution != null)
                //{
                //    for (int i = core.guidance.Solution.num_segments; i > 0; i--)
                //        GUILayout.Label($"{i}: {core.guidance.Solution.ArcString(vesselState.time, i - 1)}");
                //}

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
                GUILayout.Label(label27, GuiUtils.LayoutWidth(100)); //converges:
                GUILayout.Label(label28, GuiUtils.LayoutWidth(100)); //status:
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(n, GuiUtils.LayoutWidth(100));
                GUILayout.Label(label29); //staleness:
                GUILayout.EndHorizontal();
                GUILayout.Label(znorm);
                //if (core.guidance.last_failure_cause != null)
                //{
                //    GUIStyle s = core.guidance.staleness < 2 && core.guidance.successful_converges > 0 ? GuiUtils.greenLabel : GuiUtils.redLabel;
                //    GUILayout.Label(label30, s); //LAST FAILURE:
                //}

                Profiler.EndSample();
            }

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        private void ShowAutoWarpGUIElements()
        {
            if (!vessel.LandedOrSplashed) return;
            const int LAN_width = 60;

            Profiler.BeginSample("MJ.GUIWindow.ShowAutoWarp");
            GUILayout.BeginVertical(GUI.skin.box);

            if (core.node.autowarp)
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label33, _ascentSettings.warpCountDown, "s", 35); //Launch countdown:

            bool targetExists = core.target.NormalTargetExists;
            if (!Launching && !targetExists)
            {
                launchingToPlane = launchingToRendezvous = launchingToMatchLAN = false;
                GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label34); //Select a target for a timed launch.
            }

            if (!Launching)
            {
                // Launch to Rendezvous
                if (targetExists && _ascentSettings.ascentType != ascentType.PVG
                                 && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_button14, _ascentSettings.launchPhaseAngle, "º",
                                     width: 40)) //Launch to rendezvous:
                {
                    launchingToRendezvous = true;
                    _autopilot.StartCountdown(vesselState.time +
                                             TimeToPhaseAngle(_ascentSettings.launchPhaseAngle,
                                                 mainBody, vesselState.longitude, core.target.TargetOrbit));
                }

                //Launch into plane of target
                if (targetExists && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_button15, _ascentSettings.launchLANDifference, "º",
                        width: LAN_width)) //Launch into plane of target
                {
                    launchingToPlane = true;
                    (double timeToPlane, double inclination) = Functions.MinimumTimeToPlane(
                        mainBody.rotationPeriod,
                        vesselState.latitude,
                        vesselState.celestialLongitude,
                        core.target.TargetOrbit.LAN - _ascentSettings.launchLANDifference,
                        core.target.TargetOrbit.inclination
                    );
                    _autopilot.StartCountdown(vesselState.time + timeToPlane);
                    _ascentSettings.desiredInclination = inclination;
                }

                //Launch to target LAN
                if (targetExists && _ascentSettings.ascentType == ascentType.PVG
                                 && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_LaunchToTargetLan, _ascentSettings.launchLANDifference,
                                     "º", width: LAN_width)) //Launch to target LAN
                {
                    launchingToMatchLAN = true;
                    _autopilot.StartCountdown(vesselState.time +
                                             Functions.TimeToPlane(
                                                 mainBody.rotationPeriod,
                                                 vesselState.latitude,
                                                 vesselState.celestialLongitude,
                                                 core.target.TargetOrbit.LAN - _ascentSettings.launchLANDifference,
                                                 _ascentSettings.desiredInclination
                                             )
                    );
                }

                //Launch to LAN
                if (_ascentSettings.ascentType == ascentType.PVG)
                {
                    if (GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_LaunchToLan, _ascentSettings.desiredLAN, "º",
                            width: LAN_width)) //Launch to LAN
                    {
                        launchingToLAN = true;
                        _autopilot.StartCountdown(vesselState.time +
                                                 Functions.TimeToPlane(
                                                     mainBody.rotationPeriod,
                                                     vesselState.latitude,
                                                     vesselState.celestialLongitude,
                                                     _ascentSettings.desiredLAN,
                                                     _ascentSettings.desiredInclination
                                                 )
                        );
                    }
                }
            }

            if (Launching)
            {
                GUILayout.Label(launchTimer);
                if (GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button17)) //Abort
                    launchingToPlane = launchingToRendezvous = launchingToMatchLAN = launchingToLAN = _autopilot.timedLaunch = false;
            }

            GUILayout.EndVertical();
            Profiler.EndSample();
        }

        private DateTime lastRefresh = DateTime.MinValue;
        private string vgo, heading, tgo, pitch, label26, label27, label28, n, label29, znorm, label30, launchTimer, sCurrentMaxAoA, autopilotStatus;
        private TimeSpan refreshInterval = TimeSpan.FromSeconds(0.1);

        [SerializeField] [Persistent(pass = (int)Pass.Global)]
        private EditableInt refreshRate = 10;

        private void UpdateStrings()
        {
            DateTime now = DateTime.Now;
            if (now > lastRefresh + refreshInterval)
            {
                lastRefresh = DateTime.Now;
                Profiler.BeginSample("MJ.GUIWindow.UpdateStrings.StringOps");
                vgo     = $"vgo: {core.guidance.VGO:F1}";
                heading = $"heading: {core.guidance.Heading:F1}";
                tgo     = $"tgo: {core.guidance.Tgo:F3}";
                pitch   = $"pitch: {core.guidance.Pitch:F1}";
                label26 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label26}{core.guidance.Status}";
                label27 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label27}{core.glueball.SuccessfulConverges}";
                label28 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label28}{core.glueball.LastLmStatus}";
                n       = $"n: {core.glueball.LastLmIterations}({core.glueball.MaxLmIterations})";
                label29 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label29}{GuiUtils.TimeToDHMS(core.glueball.Staleness)}";
                znorm   = $"znorm: {core.glueball.LastZnorm:G5}";
                label30 = string.Empty;
                //if (core.guidance.last_failure_cause != null)
                //    label30 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label30}{core.guidance.last_failure_cause}";

                if (launchingToPlane) launchTimer           = CachedLocalizer.Instance.MechJeb_Ascent_msg2; //Launching to target plane
                else if (launchingToRendezvous) launchTimer = CachedLocalizer.Instance.MechJeb_Ascent_msg3; //Launching to rendezvous
                else if (launchingToMatchLAN) launchTimer   = CachedLocalizer.Instance.MechJeb_Ascent_LaunchingToTargetLAN; //Launching to target LAN
                else if (launchingToLAN) launchTimer        = CachedLocalizer.Instance.MechJeb_Ascent_LaunchingToManualLAN; //Launching to manual LAN
                else launchTimer                            = string.Empty;
                if (_autopilot.tMinus > 3 * vesselState.deltaT)
                    launchTimer += $": T-{GuiUtils.TimeToDHMS(_autopilot.tMinus, 1)}";

                sCurrentMaxAoA  = $"º ({_autopilot.currentMaxAoA:F1}°)";
                autopilotStatus = CachedLocalizer.Instance.MechJeb_Ascent_label35 + _autopilot.Status;
                Profiler.EndSample();
            }
        }

        private void RefreshRateGUI()
        {
            int oldRate = refreshRate;
            if (GuiUtils.showAdvancedWindowSettings)
                GuiUtils.SimpleTextBox("Update Interval", refreshRate, "Hz");
            if (oldRate != refreshRate)
            {
                refreshRate     = Math.Max(refreshRate, 1);
                refreshInterval = TimeSpan.FromSeconds(1d / refreshRate);
            }
        }

        protected override void WindowGUI(int windowID)
        {
            SetupButtonStyles();
            GUILayout.BeginVertical();

            if (_autopilot != null)
            {
                UpdateStrings();
                //PerformanceTestGUIElements();
                VisibleSectionsGUIElements();
                ShowTargetingGUIElements();
                ShowGuidanceSettingsGUIElements();
                ShowAscentSettingsGUIElements(out bool forceRoll, out bool correctiveSteering, out bool limitAoA, out bool autostage);
                ShowStatusGUIElements();
                ShowAutoWarpGUIElements();

                if (_autopilot.enabled) GUILayout.Label(autopilotStatus); //Autopilot status:
                if (core.DeactivateControl)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label36, GuiUtils.redLabel); //CONTROL DISABLED (AVIONICS)
                
                _ascentSettings.forceRoll            = forceRoll;
                _ascentSettings.correctiveSteering   = correctiveSteering;
                _ascentSettings.limitAoA             = limitAoA;
                _ascentSettings.autostage            = autostage;
            }

            if (!vessel.patchedConicsUnlocked() && _ascentSettings.ascentType != ascentType.PVG)
            {
                GUILayout.Label(CachedLocalizer.Instance
                    .MechJeb_Ascent_label37); //"Warning: MechJeb is unable to circularize without an upgraded Tracking Station."
            }

            GUILayout.BeginHorizontal();
            _ascentSettings.ascentType = (ascentType)GuiUtils.ComboBox.Box((int)_ascentSettings.ascentType, _ascentPathList, this);
            GUILayout.EndHorizontal();

            if (_ascentSettings.ascentType == ascentType.CLASSIC)
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
        public static double TimeToPhaseAngle(double phaseAngle, CelestialBody launchBody, double launchLongitude, Orbit target)
        {
            double launchpadAngularRate = 360 / launchBody.rotationPeriod;
            double targetAngularRate = 360.0 / target.period;
            if (Vector3d.Dot(-target.GetOrbitNormal().Reorder(132).normalized, launchBody.angularVelocity) < 0)
                targetAngularRate *= -1; //retrograde target

            Vector3d currentLaunchpadDirection = launchBody.GetSurfaceNVector(0, launchLongitude);
            Vector3d currentTargetDirection = target.SwappedRelativePositionAtUT(Planetarium.GetUniversalTime());
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
