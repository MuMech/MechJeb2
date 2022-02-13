using System;
using UnityEngine;
using KSP.UI.Screens;
using KSP.Localization;
using UnityEngine.Profiling;

namespace MuMech
{
    //When enabled, the ascent guidance module makes the purple navball target point
    //along the ascent path. The ascent path can be set via SetPath. The ascent guidance
    //module disables itself if the player selects a different target.
    public class MechJebModuleAscentGuidance : DisplayModule
    {
        public MechJebModuleAscentGuidance(MechJebCore core) : base(core) { }

        public EditableDouble desiredInclination = 0;
        public EditableDouble desiredLAN = 0;

        public bool launchingToPlane = false;
        public bool launchingToRendezvous = false;
        public bool launchingToMatchLAN = false;
        public bool launchingToLAN = false;
        public bool Launching => launchingToPlane || launchingToRendezvous || launchingToMatchLAN || launchingToLAN;
                
        public MechJebModuleAscentAutopilot autopilot { get { return core.GetComputerModule<MechJebModuleAscentAutopilot>(); } }
        public MechJebModuleAscentPVG pvgascent { get { return core.GetComputerModule<MechJebModuleAscentPVG>(); } }
        public MechJebModuleAscentGT gtascent { get { return core.GetComputerModule<MechJebModuleAscentGT>(); } }
        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.FuelStats[] atmoStats { get { return stats.atmoStats; } }

        private ascentType ascentPathIdx { get { return autopilot.ascentPathIdxPublic; } }

        MechJebModuleAscentNavBall navBall;

        public override void OnStart(PartModule.StartState state)
        {
            if (autopilot != null)
            {
                desiredInclination = autopilot.desiredInclination;  // FIXME: remove this indirection
                desiredLAN = autopilot.desiredLAN;  // FIXME: remove this indirection
            }
            navBall = core.GetComputerModule<MechJebModuleAscentNavBall>();
        }

        public override void OnModuleDisabled()
        {
            launchingToPlane = false;
            launchingToRendezvous = false;
            launchingToMatchLAN = false;
        }

        [GeneralInfoItem("#MechJeb_Toggle_Ascent_Navball_Guidance", InfoItem.Category.Misc, showInEditor = false)]//Toggle Ascent Navball Guidance
        public void ToggleAscentNavballGuidanceInfoItem()
        {
            if (navBall != null)
            {
                //Hide ascent navball guidance    or    Show ascent navball guidance
                string msg = navBall.NavBallGuidance ? CachedLocalizer.Instance.MechJeb_NavBallGuidance_btn1 : CachedLocalizer.Instance.MechJeb_NavBallGuidance_btn2;
                if (GUILayout.Button(msg)) navBall.NavBallGuidance = !navBall.NavBallGuidance;
                //navBall.NavBallGuidance = GUILayout.Toggle(navBall.NavBallGuidance,msg);
            }
        }

        public static GUIStyle btNormal, btActive;

        private bool toggle1, toggle2;
        private string simpleText = "SimpleText";

        private void SetupButtonStyles()
        {
            if (btNormal == null)
            {
                btNormal = new GUIStyle(GUI.skin.button);
                btNormal.normal.textColor = btNormal.focused.textColor = Color.white;
                btNormal.hover.textColor = btNormal.active.textColor = Color.yellow;
                btNormal.onNormal.textColor = btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                btNormal.padding = new RectOffset(8,8,8,8);

                btActive = new GUIStyle(btNormal);
                btActive.active = btActive.onActive;
                btActive.normal = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover = btActive.onHover;
            }
        }

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
            toggle2 = GUILayout.Toggle(toggle2,sToggle2);
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneSimpleTextBox");
            GuiUtils.SimpleTextBox("SimpleTextBox",autopilot.desiredOrbitAltitude,"pfx");//Target Periapsis
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneTextField");
            simpleText = GUILayout.TextField(simpleText);
            Profiler.EndSample();

            Profiler.BeginSample("MJ.GUIWindow.OneSimpleTextField");
            GuiUtils.SimpleTextField(pvgascent.DesiredAttachAlt);
            Profiler.EndSample();
        }

        private void VisibleSectionsGUIElements(out bool showTargeting, out bool showGuidanceSettings, out bool showSettings, out bool showStatus)
        {
            Profiler.BeginSample("MJ.GUIWindow.TopButtons_PVG2buttons4toggles");
            showTargeting = autopilot.showTargeting;
            showGuidanceSettings = autopilot.showGuidanceSettings;
            showSettings = autopilot.showSettings;
            showStatus = autopilot.showStatus;

            if (autopilot.enabled && GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button1))//Disengage autopilot
                autopilot.users.Remove(this);
            else if (!autopilot.enabled && GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button2))//Engage autopilot
                autopilot.users.Add(this);

            if (ascentPathIdx == ascentType.PVG && GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button3))//Reset Guidance (DO NOT PRESS)
                core.guidance.Reset();

            GUILayout.BeginHorizontal(); // EditorStyles.toolbar);
            showTargeting = GUILayout.Toggle(showTargeting,CachedLocalizer.Instance.MechJeb_Ascent_button4, showTargeting ? btActive : btNormal);

            if (ascentPathIdx == ascentType.PVG || ascentPathIdx == ascentType.GRAVITYTURN)
                showGuidanceSettings = GUILayout.Toggle(showGuidanceSettings,CachedLocalizer.Instance.MechJeb_Ascent_button5, showGuidanceSettings ? btActive : btNormal);

            showSettings = GUILayout.Toggle(showSettings,CachedLocalizer.Instance.MechJeb_Ascent_button6,showSettings ? btActive : btNormal);

            if (ascentPathIdx == ascentType.PVG)
                showStatus = GUILayout.Toggle(showStatus,CachedLocalizer.Instance.MechJeb_Ascent_button7,showStatus ? btActive : btNormal);
            GUILayout.EndHorizontal();
            Profiler.EndSample();
        }

        private void ShowTargetingGUIElements()
        {
            if (!autopilot.showTargeting) return;

            Profiler.BeginSample("MJ.GUIWindow.ShowTargeting_PVG_3TextBox_1ToggledTextBox_1Button");

            if (ascentPathIdx == ascentType.PVG)
            {
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label1,autopilot.desiredOrbitAltitude,"km");//Target Periapsis
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label2,pvgascent.DesiredApoapsis,"km");//Target Apoapsis:
                GuiUtils.ToggledTextBox(ref pvgascent.AttachAltFlag,CachedLocalizer.Instance.MechJeb_Ascent_attachAlt,pvgascent.DesiredAttachAlt,"km");

                if (pvgascent.DesiredApoapsis >= 0 && pvgascent.DesiredApoapsis < autopilot.desiredOrbitAltitude)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label3,GuiUtils.yellowLabel);//Ap < Pe: circularizing orbit
                else if (pvgascent.AttachAltFlag && pvgascent.DesiredAttachAlt > pvgascent.DesiredApoapsis)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_warnAttachAltHigh,GuiUtils.orangeLabel);//Attach > Ap: apoapsis insertion
                if (pvgascent.DesiredApoapsis < 0)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label4,GuiUtils.orangeLabel);//Hyperbolic target orbit (neg Ap)
                if (pvgascent.AttachAltFlag && pvgascent.DesiredAttachAlt < autopilot.desiredOrbitAltitude)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_warnAttachAltLow,GuiUtils.orangeLabel);//Attach < Pe: periapsis insertion
            }
            else
            {
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label5,autopilot.desiredOrbitAltitude,"km");//Orbit altitude
            }

            GUIStyle si = (Math.Abs(desiredInclination) < Math.Abs(vesselState.latitude) - 2.001) ? GuiUtils.orangeLabel : GuiUtils.skin.label;
            GUILayout.BeginHorizontal();
            GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label6,desiredInclination,"º",100,si,horizontalFraming: false);//Orbit inc.
            if (GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button13))//Current
                desiredInclination.val = vesselState.latitude;
            GUILayout.EndHorizontal();

            double delta = Math.Abs(vesselState.latitude) - Math.Abs(desiredInclination);
            if (2.001 < delta)
                GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label7",delta),si);//inc {0:F1}º below current latitude

            autopilot.desiredInclination = desiredInclination;

            Profiler.EndSample();
        }

        private void ShowGuidanceSettingsGUIElements()
        {
            Profiler.BeginSample("MJ.GUIWindow.ShowGuidanceSettings_PVG_5TextBox_2ToggledTextBox");

            if (autopilot.showGuidanceSettings)
            {
                if (ascentPathIdx == ascentType.GRAVITYTURN)
                {
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label8,gtascent.turnStartAltitude,"km");//Turn start altitude:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label9,gtascent.turnStartVelocity,"m/s");//Turn start velocity:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label10,gtascent.turnStartPitch,"deg");//Turn start pitch:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label11,gtascent.intermediateAltitude,"km");//Intermediate altitude:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label12,gtascent.holdAPTime,"s");//Hold AP Time:
                }
                else if (ascentPathIdx == ascentType.PVG)
                {
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label13,pvgascent.PitchStartVelocity,"m/s",40);//Booster Pitch start:
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label14,pvgascent.PitchRate,"°/s",40);//Booster Pitch rate:
                    GuiUtils.SimpleTextBox("Q Trigger:",pvgascent.DynamicPressureTrigger,"kPa",40);
                    GuiUtils.ToggledTextBox(ref pvgascent.StagingTriggerFlag,"PVG After Stage:",pvgascent.StagingTrigger,width: 40);
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label15,core.guidance.pvgInterval,"s",40);//Guidance Interval:
                    if (core.guidance.pvgInterval < 1 || core.guidance.pvgInterval > 30)
                    {
                        GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label16,GuiUtils.yellowLabel);//Guidance intervals are limited to between 1s and 30s
                    }
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label17,autopilot.limitQa,"Pa-rad");//Qα limit
                    if (autopilot.limitQa < 100 || autopilot.limitQa > 4000)
                    {
                        if (autopilot.limitQa < 100)
                            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label18,GuiUtils.yellowLabel);//Qα limit cannot be set to lower than 100 Pa-rad
                        else if (autopilot.limitQa > 10000)
                            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label19,GuiUtils.yellowLabel);//Qα limit cannot be set to higher than 10000 Pa-rad
                        else
                            GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label20,GuiUtils.yellowLabel);//Qα limit is recommended to be 1000 to 4000 Pa-rad
                    }
                    Profiler.BeginSample("MJ.GUIWindow.ShowGuidanceSettings.ToggledTextBox");
                    GuiUtils.ToggledTextBox(ref pvgascent.FixedCoast,"Fixed Coast Length:",pvgascent.FixedCoastLength,width: 40);
                    Profiler.EndSample();
                }
            }

            autopilot.limitQaEnabled = (ascentPathIdx == ascentType.PVG);  // this is mandatory for PVG

            Profiler.EndSample();
        }

        private void ShowAscentSettingsGUIElements(out bool forceRoll, out bool correctiveSteering, out bool limitAoA, out bool autostage)
        {
            forceRoll = autopilot.forceRoll;
            correctiveSteering = autopilot.correctiveSteering;
            limitAoA = autopilot.limitAoA;
            autostage = autopilot.autostage;
            if (!autopilot.showSettings) return;

            Profiler.BeginSample("MJ.GUIWindow.AscentItems");

            ToggleAscentNavballGuidanceInfoItem();
            core.thrust.LimitToPreventOverheatsInfoItem();
            core.thrust.LimitToMaxDynamicPressureInfoItem();
            core.thrust.LimitAccelerationInfoItem();
            if (ascentPathIdx != ascentType.PVG) core.thrust.LimitThrottleInfoItem();
            core.thrust.LimiterMinThrottleInfoItem();
            if (ascentPathIdx != ascentType.PVG) core.thrust.LimitElectricInfoItem();

            if (ascentPathIdx == ascentType.PVG)
            {
                // GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label21")) ;//FIXME: g-limiter is down for maintenance
                core.thrust.limitThrottle = false;
                core.thrust.limitToTerminalVelocity = false;
                core.thrust.electricThrottle = false;
            }

            GUILayout.BeginHorizontal();
            forceRoll = GUILayout.Toggle(autopilot.forceRoll,CachedLocalizer.Instance.MechJeb_Ascent_checkbox2);//Force Roll
            if (autopilot.forceRoll)
            {
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label22,autopilot.verticalRoll,"º",30); //climb
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label23,autopilot.turnRoll,"º",30);     //turn
                GuiUtils.SimpleTextBox("Alt",autopilot.rollAltitude,"m",30);
            }
            GUILayout.EndHorizontal();

            if (ascentPathIdx != ascentType.PVG)
            {
                GUIStyle s = autopilot.limitingAoA ? GuiUtils.greenToggle : null;
                GuiUtils.ToggledTextBox(ref limitAoA,CachedLocalizer.Instance.MechJeb_Ascent_checkbox3,autopilot.maxAoA,sCurrentMaxAoA,s,30);//Limit AoA to

                if (autopilot.limitAoA)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(25);
                    GUIStyle sl = (autopilot.limitingAoA && vesselState.dynamicPressure < autopilot.aoALimitFadeoutPressure) ? GuiUtils.greenLabel : GuiUtils.skin.label;
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label24,autopilot.aoALimitFadeoutPressure,"Pa",50,sl);//Dynamic Pressure Fadeout
                    GUILayout.EndHorizontal();
                }
                autopilot.limitQaEnabled = false; // this is only for PVG
            }

            if (ascentPathIdx == ascentType.CLASSIC)
            {
                // corrective steering only applies to Classic
                GUILayout.BeginHorizontal();
                correctiveSteering = GUILayout.Toggle(correctiveSteering,CachedLocalizer.Instance.MechJeb_Ascent_checkbox4,GuiUtils.ExpandWidth(false));//Corrective steering
                if (autopilot.correctiveSteering)
                    GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label25,autopilot.correctiveSteeringGain,width: 40,horizontalFraming: false);    // Gain
                GUILayout.EndHorizontal();
            }

            autostage = GUILayout.Toggle(autostage,CachedLocalizer.Instance.MechJeb_Ascent_checkbox5);//Autostage
            if (autopilot.autostage) core.staging.AutostageSettingsInfoItem();

            autopilot.autodeploySolarPanels = GUILayout.Toggle(autopilot.autodeploySolarPanels,CachedLocalizer.Instance.MechJeb_Ascent_checkbox6);//Auto-deploy solar panels
            autopilot.autoDeployAntennas = GUILayout.Toggle(autopilot.autoDeployAntennas,CachedLocalizer.Instance.MechJeb_Ascent_checkbox7);//Auto-deploy antennas

            GUILayout.BeginHorizontal();
            core.node.autowarp = GUILayout.Toggle(core.node.autowarp,CachedLocalizer.Instance.MechJeb_Ascent_checkbox8);//Auto-warp
            if (ascentPathIdx != ascentType.PVG)
            {
                autopilot.skipCircularization = GUILayout.Toggle(autopilot.skipCircularization,CachedLocalizer.Instance.MechJeb_Ascent_checkbox9);//Skip Circularization
            }
            else
            {
                // skipCircularization is always true for Optimizer
                autopilot.skipCircularization = true;
            }
            GUILayout.EndHorizontal();

            Profiler.EndSample();
        }

        private void ShowStatusGUIElements()
        {
            if (!autopilot.showStatus) return;

            Profiler.BeginSample("MJ.GUIWindow.ShowStatus");

            if (ascentPathIdx == ascentType.PVG)
            {
                if (core.guidance.solution != null)
                {
                    for (int i = core.guidance.solution.num_segments; i > 0; i--)
                        GUILayout.Label($"{i}: {core.guidance.solution.ArcString(vesselState.time,i - 1)}");
                }

                Profiler.BeginSample("MJ.GUIWindow.ShowStatus.Labels");
                GUILayout.BeginHorizontal();
                GUILayout.Label(vgo,GuiUtils.LayoutWidth(100));
                GUILayout.Label(heading,GuiUtils.LayoutWidth(100));
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(tgo,GuiUtils.LayoutWidth(100));
                GUILayout.Label(pitch,GuiUtils.LayoutWidth(100));
                GUILayout.EndHorizontal();
                GUIStyle si;
                if (core.guidance.isStable())
                    si = GuiUtils.greenLabel;
                else if (core.guidance.isInitializing() || core.guidance.status == PVGStatus.FINISHED)
                    si = GuiUtils.orangeLabel;
                else
                    si = GuiUtils.redLabel;
                GUILayout.Label(label26,si);//Guidance Status:
                GUILayout.BeginHorizontal();
                GUILayout.Label(label27,GuiUtils.LayoutWidth(100));//converges:
                GUILayout.Label(label28,GuiUtils.LayoutWidth(100));//status:
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.Label(n,GuiUtils.LayoutWidth(100));
                GUILayout.Label(label29);//staleness:
                GUILayout.EndHorizontal();
                GUILayout.Label(znorm);
                if (core.guidance.last_failure_cause != null)
                {
                    GUIStyle s = core.guidance.staleness < 2 && core.guidance.successful_converges > 0 ? GuiUtils.greenLabel : GuiUtils.redLabel;
                    GUILayout.Label(label30,s);//LAST FAILURE:
                }

                if (vessel.situation != Vessel.Situations.LANDED && vessel.situation != Vessel.Situations.PRELAUNCH && vessel.situation != Vessel.Situations.SPLASHED && atmoStats.Length > vessel.currentStage)
                {
                    double m0 = atmoStats[vessel.currentStage].StartMass;
                    double thrust = atmoStats[vessel.currentStage].EndThrust;

                    if (Math.Abs(vesselState.mass - m0) / m0 > 0.01)
                    {
                        GUILayout.Label($"{CachedLocalizer.Instance.MechJeb_Ascent_label31}{(vesselState.mass - m0) / m0 * 100.0:F1}%",GuiUtils.yellowLabel);//MASS IS OFF BY
                    }

                    double thrustfrac = Math.Abs(vesselState.thrustCurrent - thrust) / thrust;

                    if (thrustfrac > 0.10 && thrustfrac < 0.99)
                    {
                        GUILayout.Label($"{CachedLocalizer.Instance.MechJeb_Ascent_label32}{(vesselState.thrustCurrent - thrust) / thrust * 100.0:F1}%",GuiUtils.yellowLabel);//THRUST IS OFF BY
                    }
                }
                Profiler.EndSample();

            }

            Profiler.EndSample();
        }

        private void ShowAutoWarpGUIElements()
        {
            if (!vessel.LandedOrSplashed) return;
            Profiler.BeginSample("MJ.GUIWindow.ShowAutoWarp");

            if (core.node.autowarp)
                GuiUtils.SimpleTextBox(CachedLocalizer.Instance.MechJeb_Ascent_label33,autopilot.warpCountDown,"s",60);//Launch countdown:

            bool targetExists = core.target.NormalTargetExists;
            if (!Launching && !targetExists)
            {
                launchingToPlane = launchingToRendezvous = launchingToMatchLAN = false;
                GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label34);//Select a target for a timed launch.
            }

            if (!Launching)
            {
                // Launch to Rendezvous
                if (targetExists && ascentPathIdx != ascentType.PVG
                    && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_button14,autopilot.launchPhaseAngle,"º",width: 60)) //Launch to rendezvous:
                {
                    launchingToRendezvous = true;
                    autopilot.StartCountdown(vesselState.time +
                            LaunchTiming.TimeToPhaseAngle(autopilot.launchPhaseAngle,
                                mainBody,vesselState.longitude,core.target.TargetOrbit));
                }

                //Launch into plane of target
                if (targetExists && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_button15,autopilot.launchLANDifference,"º",width: 60)) //Launch into plane of target
                {
                    launchingToPlane = true;
                    autopilot.StartCountdown(vesselState.time +
                            SpaceMath.MinimumTimeToPlane(
                                mainBody.rotationPeriod,
                                vesselState.latitude,
                                vesselState.celestialLongitude,
                                core.target.TargetOrbit.LAN - autopilot.launchLANDifference,
                                core.target.TargetOrbit.inclination
                                )
                            );
                }

                //Launch to target LAN
                if (targetExists && ascentPathIdx == ascentType.PVG
                    && GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_LaunchToTargetLan,autopilot.launchLANDifference,"º",width: 60)) //Launch to target LAN
                {
                    launchingToMatchLAN = true;
                    autopilot.StartCountdown(vesselState.time +
                            SpaceMath.MinimumTimeToPlane(
                                mainBody.rotationPeriod,
                                vesselState.latitude,
                                vesselState.celestialLongitude,
                                core.target.TargetOrbit.LAN - autopilot.launchLANDifference,
                                desiredInclination
                                )
                            );
                }

                //Launch to LAN
                if (ascentPathIdx == ascentType.PVG)
                {
                    if (GuiUtils.ButtonTextBox(CachedLocalizer.Instance.MechJeb_Ascent_LaunchToLan,desiredLAN,"º",width: 60)) //Launch to LAN
                    {
                        launchingToLAN = true;
                        autopilot.StartCountdown(vesselState.time +
                                SpaceMath.MinimumTimeToPlane(
                                    mainBody.rotationPeriod,
                                    vesselState.latitude,
                                    vesselState.celestialLongitude,
                                    desiredLAN,
                                    desiredInclination
                                    )
                                );
                    }

                    autopilot.desiredLAN = desiredLAN;
                }
            }

            if (Launching)
            {
                if (launchingToPlane)
                {
                    desiredInclination = MuUtils.Clamp(core.target.TargetOrbit.inclination,Math.Abs(vesselState.latitude),180 - Math.Abs(vesselState.latitude));
                    desiredInclination *=
                        Math.Sign(Vector3d.Dot(core.target.TargetOrbit.SwappedOrbitNormal(),
                                    Vector3d.Cross(vesselState.CoM - mainBody.position,mainBody.transform.up)));
                }
                GUILayout.Label(launchTimer);
                if (GUILayout.Button(CachedLocalizer.Instance.MechJeb_Ascent_button17))//Abort
                    launchingToPlane = launchingToRendezvous = launchingToMatchLAN = launchingToLAN = autopilot.timedLaunch = false;
            }
            Profiler.EndSample();
        }

        private System.DateTime lastRefresh = System.DateTime.MinValue;
        private string vgo,heading,tgo,pitch,label26,label27,label28,n,label29,znorm,label30,launchTimer,sCurrentMaxAoA,autopilotStatus;
        private System.TimeSpan refreshInterval = System.TimeSpan.FromSeconds(0.1);
        [SerializeField]
        [Persistent(pass = (int)Pass.Global)]
        private EditableInt refreshRate = 10;

        private void UpdateStrings()
        {
            System.DateTime now = System.DateTime.Now;
            if (now > lastRefresh + refreshInterval)
            {
                lastRefresh = System.DateTime.Now;
                Profiler.BeginSample("MJ.GUIWindow.UpdateStrings.StringOps");
                vgo = $"vgo: {core.guidance.vgo:F1}";
                heading = $"heading: {core.guidance.heading:F1}";
                tgo = $"tgo: {core.guidance.tgo:F3}";
                pitch = $"pitch: {core.guidance.pitch:F1}";
                label26 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label26}{core.guidance.status}";
                label27 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label27}{core.guidance.successful_converges}";
                label28 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label28}{core.guidance.last_lm_status}";
                n = $"n: {core.guidance.last_lm_iteration_count}({core.guidance.max_lm_iteration_count})";
                label29 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label29}{GuiUtils.TimeToDHMS(core.guidance.staleness)}";
                znorm = $"znorm: {core.guidance.last_znorm:G5}";
                label30 = string.Empty;
                if (core.guidance.last_failure_cause != null)
                    label30 = $"{CachedLocalizer.Instance.MechJeb_Ascent_label30}{core.guidance.last_failure_cause}";

                if (launchingToPlane) launchTimer = CachedLocalizer.Instance.MechJeb_Ascent_msg2;//Launching to target plane
                else if (launchingToRendezvous) launchTimer = CachedLocalizer.Instance.MechJeb_Ascent_msg3;//Launching to rendezvous
                else if (launchingToMatchLAN) launchTimer = CachedLocalizer.Instance.MechJeb_Ascent_LaunchingToTargetLAN;//Launching to target LAN
                else if (launchingToLAN) launchTimer = CachedLocalizer.Instance.MechJeb_Ascent_LaunchingToManualLAN;//Launching to manual LAN
                else launchTimer = string.Empty;
                if (autopilot.tMinus > 3 * vesselState.deltaT)
                    launchTimer += $": T-{GuiUtils.TimeToDHMS(autopilot.tMinus,1)}";

                sCurrentMaxAoA = $"º ({autopilot.currentMaxAoA:F1}°)";
                autopilotStatus = CachedLocalizer.Instance.MechJeb_Ascent_label35 + autopilot.status;
                Profiler.EndSample();
            }
        }

        private bool advancedSettings = false;
        private void RefreshRateGUI()
        {
            int oldRate = refreshRate;
            if (advancedSettings = GUILayout.Toggle(advancedSettings,"Advanced Window Settings"))
                GuiUtils.SimpleTextBox("Update Interval",refreshRate,"Hz");
            if (oldRate != refreshRate)
            {
                refreshRate = Math.Max(refreshRate,1);
                refreshInterval = System.TimeSpan.FromSeconds(1d / refreshRate);
            }
        }

        protected override void WindowGUI(int windowID)
        {
            SetupButtonStyles();
            GUILayout.BeginVertical();

            if (autopilot != null)
            {
                UpdateStrings();
                //PerformanceTestGUIElements();
                VisibleSectionsGUIElements(out bool showTargeting,out bool showGuidanceSettings,out bool showSettings,out bool showStatus);
                ShowTargetingGUIElements();
                ShowGuidanceSettingsGUIElements();
                ShowAscentSettingsGUIElements(out bool forceRoll,out bool correctiveSteering,out bool limitAoA,out bool autostage);
                ShowStatusGUIElements();
                ShowAutoWarpGUIElements();

                if (autopilot.enabled) GUILayout.Label(autopilotStatus);//Autopilot status:
                if (core.DeactivateControl)
                    GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label36, GuiUtils.redLabel);//CONTROL DISABLED (AVIONICS)

                autopilot.showTargeting = showTargeting;
                autopilot.showGuidanceSettings = showGuidanceSettings;
                autopilot.showSettings = showSettings;
                autopilot.showStatus = showStatus;
                autopilot.forceRoll = forceRoll;
                autopilot.correctiveSteering = correctiveSteering;
                autopilot.limitAoA = limitAoA;
                autopilot.autostage = autostage;
            }

            if (!vessel.patchedConicsUnlocked() && ascentPathIdx != ascentType.PVG)
            {
                GUILayout.Label(CachedLocalizer.Instance.MechJeb_Ascent_label37);//"Warning: MechJeb is unable to circularize without an upgraded Tracking Station."
            }

            GUILayout.BeginHorizontal();
            autopilot.ascentPathIdxPublic = (ascentType)GuiUtils.ComboBox.Box((int)autopilot.ascentPathIdxPublic, autopilot.ascentPathList, this);
            GUILayout.EndHorizontal();

            if (autopilot.ascentMenu != null)
                autopilot.ascentMenu.enabled = GUILayout.Toggle(autopilot.ascentMenu.enabled, CachedLocalizer.Instance.MechJeb_Ascent_checkbox10);//Edit ascent path

            RefreshRateGUI();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(240), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return CachedLocalizer.Instance.MechJeb_Ascent_title;//"Ascent Guidance"
        }

        public override string IconName()
        {
            return "Ascent Guidance";
        }
    }
}
