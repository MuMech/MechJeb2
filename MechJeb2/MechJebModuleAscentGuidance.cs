using System;
using UnityEngine;
using KSP.UI.Screens;
using KSP.Localization;

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

        public MechJebModuleAscentAutopilot autopilot { get { return core.GetComputerModule<MechJebModuleAscentAutopilot>(); } }
        public MechJebModuleAscentPVG pvgascent { get { return core.GetComputerModule<MechJebModuleAscentPVG>(); } }
        public MechJebModuleAscentGT gtascent { get { return core.GetComputerModule<MechJebModuleAscentGT>(); } }
        private MechJebModuleStageStats stats { get { return core.GetComputerModule<MechJebModuleStageStats>(); } }
        private FuelFlowSimulation.Stats[] atmoStats { get { return stats.atmoStats; } }

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
                    if (navBall.NavBallGuidance)
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_NavBallGuidance_btn1")))//Hide ascent navball guidance
                            navBall.NavBallGuidance = false;
                    }
                    else
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_NavBallGuidance_btn2")))//Show ascent navball guidance
                            navBall.NavBallGuidance = true;
                    }
                }
            }

        public static GUIStyle btNormal, btActive;

        protected override void WindowGUI(int windowID)
        {
            if (btNormal == null)
            {
                btNormal = new GUIStyle(GUI.skin.button);
                btNormal.normal.textColor = btNormal.focused.textColor = Color.white;
                btNormal.hover.textColor = btNormal.active.textColor = Color.yellow;
                btNormal.onNormal.textColor = btNormal.onFocused.textColor = btNormal.onHover.textColor = btNormal.onActive.textColor = Color.green;
                btNormal.padding = new RectOffset(8, 8, 8, 8);

                btActive = new GUIStyle(btNormal);
                btActive.active = btActive.onActive;
                btActive.normal = btActive.onNormal;
                btActive.onFocused = btActive.focused;
                btActive.hover = btActive.onHover;
            }

            GUILayout.BeginVertical();

            if (autopilot != null)
            {
                if (autopilot.enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button1")))//Disengage autopilot
                    {
                        autopilot.users.Remove(this);
                    }
                }
                else
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button2")))//Engage autopilot
                    {
                        autopilot.users.Add(this);
                    }
                }
                if (ascentPathIdx == ascentType.PVG)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button3")))//Reset Guidance (DO NOT PRESS)
                        core.guidance.Reset();


                    GUILayout.BeginHorizontal(); // EditorStyles.toolbar);

                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button4"), autopilot.showTargeting ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//"TARG"
                        autopilot.showTargeting = !autopilot.showTargeting;
                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button5"), autopilot.showGuidanceSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//GUID
                        autopilot.showGuidanceSettings = !autopilot.showGuidanceSettings;
                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button6"), autopilot.showSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//OPTS
                        autopilot.showSettings = !autopilot.showSettings;
                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button7"), autopilot.showStatus ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//STATUS
                        autopilot.showStatus = !autopilot.showStatus;
                    GUILayout.EndHorizontal();
                }
                else if (ascentPathIdx == ascentType.GRAVITYTURN)
                {
                    GUILayout.BeginHorizontal(); // EditorStyles.toolbar);
                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button8"), autopilot.showTargeting ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//TARG
                        autopilot.showTargeting = !autopilot.showTargeting;
                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button9"), autopilot.showGuidanceSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//GUID
                        autopilot.showGuidanceSettings = !autopilot.showGuidanceSettings;
                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button10"), autopilot.showSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//OPTS
                        autopilot.showSettings = !autopilot.showSettings;
                    GUILayout.EndHorizontal();
                }
                else if (ascentPathIdx == ascentType.CLASSIC)
                {
                    GUILayout.BeginHorizontal(); // EditorStyles.toolbar);
                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button11"), autopilot.showTargeting ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//TARG
                        autopilot.showTargeting = !autopilot.showTargeting;
                    if ( GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button12"), autopilot.showSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )//OPTS
                        autopilot.showSettings = !autopilot.showSettings;
                    GUILayout.EndHorizontal();
                }

                if (autopilot.showTargeting)
                {
                    if (ascentPathIdx == ascentType.PVG)
                    {

                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label1"), autopilot.desiredOrbitAltitude, "km");//Target Periapsis
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label2"), pvgascent.desiredApoapsis, "km");//Target Apoapsis:
                        if ( pvgascent.desiredApoapsis >= 0 && pvgascent.desiredApoapsis < autopilot.desiredOrbitAltitude )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.yellow;
                            GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label3"), s);//Ap < Pe: circularizing orbit
                        }
                        if ( pvgascent.desiredApoapsis < 0 )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = XKCDColors.Orange;
                            GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label4"), s);//Hyperbolic target orbit (neg Ap)
                        }
                    }
                    else
                    {
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label5"), autopilot.desiredOrbitAltitude, "km");//Orbit altitude
                    }

                    GUIStyle si = new GUIStyle(GUI.skin.label);
                    if (Math.Abs(desiredInclination) < Math.Abs(vesselState.latitude) - 2.001)
                        si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Orange;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label6"), si, GUILayout.ExpandWidth(true));//Orbit inc.
                    desiredInclination.text = GUILayout.TextField(desiredInclination.text, GUILayout.ExpandWidth(true), GUILayout.Width(100));
                    GUILayout.Label("º", GUILayout.ExpandWidth(false));
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button13")))//Current
                        desiredInclination.val = vesselState.latitude;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (Math.Abs(desiredInclination) < Math.Abs(vesselState.latitude) - 2.001)
                        GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label7", Math.Abs(vesselState.latitude) - Math.Abs(desiredInclination)), si);//inc {0:F1}º below current latitude
                    GUILayout.EndHorizontal();
                    autopilot.desiredInclination = desiredInclination;
                }

                if (autopilot.showGuidanceSettings)
                {
                    if (ascentPathIdx == ascentType.GRAVITYTURN)
                    {
                        GUILayout.BeginVertical();

                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label8"), gtascent.turnStartAltitude, "km");//Turn start altitude:
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label9"), gtascent.turnStartVelocity, "m/s");//Turn start velocity:
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label10"), gtascent.turnStartPitch, "deg");//Turn start pitch:
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label11"), gtascent.intermediateAltitude, "km");//Intermediate altitude:
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label12"), gtascent.holdAPTime, "s");//Hold AP Time:

                        GUILayout.EndVertical();
                    }
                    else if (ascentPathIdx == ascentType.PVG)
                    {
                        GUILayout.BeginVertical();
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label13"), pvgascent.pitchStartVelocity, "m/s");//Booster Pitch start:
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label14"), pvgascent.pitchRate, "°/s");//Booster Pitch rate:
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label15"), core.guidance.pvgInterval, "s");//Guidance Interval:
                        if ( core.guidance.pvgInterval < 1 || core.guidance.pvgInterval > 30 )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.yellow;
                            GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label16"), s);//Guidance intervals are limited to between 1s and 30s
                        }
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label17"), autopilot.limitQa, "Pa-rad");//Qα limit
                        if ( autopilot.limitQa < 100 || autopilot.limitQa > 4000 )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.yellow;

                            if ( autopilot.limitQa < 100 )
                                GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label18"), s);//Qα limit cannot be set to lower than 100 Pa-rad
                            else if ( autopilot.limitQa > 10000 )
                                GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label19"), s);//Qα limit cannot be set to higher than 10000 Pa-rad
                            else
                                GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label20"), s);//Qα limit is recommended to be 1000 to 4000 Pa-rad
                        }
                        pvgascent.omitCoast = GUILayout.Toggle(pvgascent.omitCoast, Localizer.Format("#MechJeb_Ascent_checkbox1"));//Omit Coast
                        GUILayout.EndVertical();
                    }
                }

                autopilot.limitQaEnabled = ( ascentPathIdx == ascentType.PVG );  // this is mandatory for PVG

                if (autopilot.showSettings)
                {
                    ToggleAscentNavballGuidanceInfoItem();
                    if ( ascentPathIdx != ascentType.PVG )
                    {
                        core.thrust.LimitToPreventOverheatsInfoItem();
                        //core.thrust.LimitToTerminalVelocityInfoItem();
                        core.thrust.LimitToMaxDynamicPressureInfoItem();
                        core.thrust.LimitAccelerationInfoItem();
                        core.thrust.LimitThrottleInfoItem();
                        core.thrust.LimiterMinThrottleInfoItem();
                        core.thrust.LimitElectricInfoItem();
                    }
                    else
                    {
                        core.thrust.LimitToPreventOverheatsInfoItem();
                        //core.thrust.LimitToTerminalVelocityInfoItem();
                        core.thrust.LimitToMaxDynamicPressureInfoItem();
                        core.thrust.LimitAccelerationInfoItem();
                        //core.thrust.LimitThrottleInfoItem();
                        core.thrust.LimiterMinThrottleInfoItem();
                        //core.thrust.LimitElectricInfoItem();

                        // GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label21")) ;//FIXME: g-limiter is down for maintenance
                        core.thrust.limitThrottle = false;
                        core.thrust.limitToTerminalVelocity = false;
                        core.thrust.electricThrottle = false;
                    }

                    GUILayout.BeginHorizontal();
                    autopilot.forceRoll = GUILayout.Toggle(autopilot.forceRoll, Localizer.Format("#MechJeb_Ascent_checkbox2"));//Force Roll
                    if (autopilot.forceRoll)
                    {
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label22"), autopilot.verticalRoll, "º", 30f);//climb
                        GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label23"), autopilot.turnRoll, "º", 30f);//turn
                    }
                    GUILayout.EndHorizontal();

                    if (ascentPathIdx != ascentType.PVG)
                    {
                        GUILayout.BeginHorizontal();
                        GUIStyle s = new GUIStyle(GUI.skin.toggle);
                        if (autopilot.limitingAoA) s.onHover.textColor = s.onNormal.textColor = Color.green;
                        autopilot.limitAoA = GUILayout.Toggle(autopilot.limitAoA, Localizer.Format("#MechJeb_Ascent_checkbox3"), s, GUILayout.ExpandWidth(true));//Limit AoA to
                        autopilot.maxAoA.text = GUILayout.TextField(autopilot.maxAoA.text, GUILayout.Width(30));
                        GUILayout.Label("º (" + autopilot.currentMaxAoA.ToString("F1") + "°)", GUILayout.ExpandWidth(true));
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        GUILayout.Space(25);
                        if (autopilot.limitAoA)
                        {
                            GUIStyle sl = new GUIStyle(GUI.skin.label);
                            if (autopilot.limitingAoA && vesselState.dynamicPressure < autopilot.aoALimitFadeoutPressure)
                                sl.normal.textColor = sl.hover.textColor = Color.green;
                            GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_Ascent_label24"), autopilot.aoALimitFadeoutPressure, "Pa", 50, sl);//Dynamic Pressure Fadeout
                        }
                        GUILayout.EndHorizontal();
                        autopilot.limitQaEnabled = false; // this is only for PVG
                    }

                    if ( ascentPathIdx == ascentType.CLASSIC )
                    {
                        // corrective steering only applies to Classic
                        GUILayout.BeginHorizontal();
                        autopilot.correctiveSteering = GUILayout.Toggle(autopilot.correctiveSteering, Localizer.Format("#MechJeb_Ascent_checkbox4"), GUILayout.ExpandWidth(false));//Corrective steering
                        if (autopilot.correctiveSteering)
                        {
                            GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label25"), GUILayout.ExpandWidth(false));//Gain
                            autopilot.correctiveSteeringGain.text = GUILayout.TextField(autopilot.correctiveSteeringGain.text, GUILayout.Width(40));
                        }
                        GUILayout.EndHorizontal();
                    }

                    autopilot.autostage = GUILayout.Toggle(autopilot.autostage, Localizer.Format("#MechJeb_Ascent_checkbox5"));//Autostage
                    if (autopilot.autostage) core.staging.AutostageSettingsInfoItem();

                    autopilot.autodeploySolarPanels = GUILayout.Toggle(autopilot.autodeploySolarPanels,
                            Localizer.Format("#MechJeb_Ascent_checkbox6"));//Auto-deploy solar panels

                    autopilot.autoDeployAntennas = GUILayout.Toggle(autopilot.autoDeployAntennas,
                            Localizer.Format("#MechJeb_Ascent_checkbox7"));//Auto-deploy antennas

                    GUILayout.BeginHorizontal();
                    core.node.autowarp = GUILayout.Toggle(core.node.autowarp, Localizer.Format("#MechJeb_Ascent_checkbox8"));//Auto-warp
                    if ( ascentPathIdx != ascentType.PVG )
                    {
                        autopilot.skipCircularization = GUILayout.Toggle(autopilot.skipCircularization, Localizer.Format("#MechJeb_Ascent_checkbox9"));//Skip Circularization
                    }
                    else
                    {
                        // skipCircularization is always true for Optimizer
                        autopilot.skipCircularization = true;
                    }
                    GUILayout.EndHorizontal();
                }

                if (autopilot.showStatus)
                {
                    if (ascentPathIdx == ascentType.PVG)
                    {
                        if (core.guidance.solution != null)
                        {
                            for(int i = core.guidance.solution.num_segments; i > 0; i--)
                                GUILayout.Label(String.Format("{0}: {1}", i, core.guidance.solution.ArcString(vesselState.time, i-1)));
                        }

                        GUILayout.BeginHorizontal();
                        GUILayout.Label(String.Format("vgo: {0:F1}", core.guidance.vgo), GUILayout.Width(100));
                        GUILayout.Label(String.Format("heading: {0:F1}", core.guidance.heading), GUILayout.Width(100));
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(String.Format("tgo: {0:F3}", core.guidance.tgo), GUILayout.Width(100));
                        GUILayout.Label(String.Format("pitch: {0:F1}", core.guidance.pitch), GUILayout.Width(100));
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUIStyle si = new GUIStyle(GUI.skin.label);
                        if ( core.guidance.isStable() )
                            si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Green;
                        else if ( core.guidance.isInitializing() || core.guidance.status == PVGStatus.FINISHED )
                            si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Orange;
                        else
                            si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Red;
                        GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label26") + core.guidance.status, si);//Guidance Status:
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label27") + core.guidance.successful_converges, GUILayout.Width(100));//converges:
                        GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label28") + core.guidance.last_lm_status, GUILayout.Width(100));//status:
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("n: " + core.guidance.last_lm_iteration_count + "(" + core.guidance.max_lm_iteration_count + ")", GUILayout.Width(100));
                        GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label29") + GuiUtils.TimeToDHMS(core.guidance.staleness));//staleness:
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(String.Format("znorm: {0:G5}", core.guidance.last_znorm));
                        GUILayout.EndHorizontal();
                        if ( core.guidance.last_failure_cause != null )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.red;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label30") + core.guidance.last_failure_cause, s);//LAST FAILURE:
                            GUILayout.EndHorizontal();
                        }

                        if ( vessel.situation != Vessel.Situations.LANDED && vessel.situation != Vessel.Situations.PRELAUNCH && vessel.situation != Vessel.Situations.SPLASHED && atmoStats.Length > vessel.currentStage)
                        {
                            double m0 = atmoStats[vessel.currentStage].startMass;
                            double thrust = atmoStats[vessel.currentStage].startThrust;

                            if (Math.Abs(vesselState.mass - m0) / m0 > 0.01)
                            {
                                GUIStyle s = new GUIStyle(GUI.skin.label);
                                s.normal.textColor = Color.yellow;
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(String.Format(Localizer.Format("#MechJeb_Ascent_label31") +"{0:F1}%", (vesselState.mass - m0) / m0 * 100.0 ), s);//MASS IS OFF BY
                                GUILayout.EndHorizontal();
                            }

                            if (Math.Abs(vesselState.thrustCurrent - thrust) / thrust > 0.01)
                            {
                                GUIStyle s = new GUIStyle(GUI.skin.label);
                                s.normal.textColor = Color.yellow;
                                GUILayout.BeginHorizontal();
                                GUILayout.Label(String.Format(Localizer.Format("#MechJeb_Ascent_label32") +"{0:F1}%", (vesselState.thrustCurrent - thrust) / thrust * 100.0 ), s);//THRUST IS OFF BY
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                }

                if (vessel.LandedOrSplashed)
                {
                    if (core.node.autowarp)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label33"), GUILayout.ExpandWidth(true));//Launch countdown:
                        autopilot.warpCountDown.text = GUILayout.TextField(autopilot.warpCountDown.text,
                                GUILayout.Width(60));
                        GUILayout.Label("s", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();
                    }
                    if (core.target.NormalTargetExists)
                    {
                        if (!launchingToPlane && !launchingToRendezvous && !launchingToMatchLAN && !launchingToLAN)
                        {
                            // disable rendezvous in PVG for now
                            if ( ascentPathIdx != ascentType.PVG )
                            {
                                GUILayout.BeginHorizontal();
                                if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button14"), GUILayout.ExpandWidth(false)))//Launch to rendezvous:
                                {
                                    launchingToRendezvous = true;
                                    autopilot.StartCountdown(vesselState.time +
                                            LaunchTiming.TimeToPhaseAngle(autopilot.launchPhaseAngle,
                                                mainBody, vesselState.longitude, core.target.TargetOrbit));
                                }
                                autopilot.launchPhaseAngle.text = GUILayout.TextField(autopilot.launchPhaseAngle.text,
                                        GUILayout.Width(60));
                                GUILayout.Label("º", GUILayout.ExpandWidth(false));
                                GUILayout.EndHorizontal();
                            }

                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button15"), GUILayout.ExpandWidth(false)))//Launch into plane of target
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
                            autopilot.launchLANDifference.text = GUILayout.TextField(
                                    autopilot.launchLANDifference.text, GUILayout.Width(60));
                            GUILayout.Label("º", GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();

                            if ( ascentPathIdx == ascentType.PVG )
                            {
                                GUILayout.BeginHorizontal();
                                if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_LaunchToTargetLan"), GUILayout.ExpandWidth(false)))//Launch to target LAN
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
                                autopilot.launchLANDifference.text = GUILayout.TextField(
                                        autopilot.launchLANDifference.text, GUILayout.Width(60));
                                GUILayout.Label("º", GUILayout.ExpandWidth(false));
                                GUILayout.EndHorizontal();
                            }
                        }
                    }
                    else
                    {
                        launchingToPlane = launchingToRendezvous = launchingToMatchLAN = false;
                        GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label34"));//Select a target for a timed launch.
                    }

                    if ( ascentPathIdx == ascentType.PVG )
                    {
                        if (!launchingToPlane && !launchingToRendezvous && !launchingToMatchLAN && !launchingToLAN)
                        {
                            GUILayout.BeginHorizontal();
                            if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_LaunchToLan"), GUILayout.ExpandWidth(false)))//Launch to LAN
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

                            desiredLAN.text = GUILayout.TextField(desiredLAN.text, GUILayout.Width(60));
                            autopilot.desiredLAN = desiredLAN;
                            GUILayout.Label("º", GUILayout.ExpandWidth(false));
                            GUILayout.EndHorizontal();
                        }
                    }

                    if (launchingToPlane || launchingToRendezvous || launchingToMatchLAN || launchingToLAN)
                    {
                        string message = "";
                        if (launchingToPlane)
                        {
                            desiredInclination = MuUtils.Clamp(core.target.TargetOrbit.inclination, Math.Abs(vesselState.latitude), 180 - Math.Abs(vesselState.latitude));
                            desiredInclination *=
                                Math.Sign(Vector3d.Dot(core.target.TargetOrbit.SwappedOrbitNormal(),
                                            Vector3d.Cross(vesselState.CoM - mainBody.position, mainBody.transform.up)));
                            message = Localizer.Format("#MechJeb_Ascent_msg2");//Launching to target plane
                        }
                        else if (launchingToRendezvous)
                        {
                            message = Localizer.Format("#MechJeb_Ascent_msg3");//Launching to rendezvous
                        }
                        else if (launchingToMatchLAN)
                        {
                            message = Localizer.Format("#MechJeb_Ascent_LaunchingToTargetLAN");//Launching to target LAN
                        }
                        else if (launchingToLAN)
                        {
                            message = Localizer.Format("#MechJeb_Ascent_LaunchingToManualLAN");//Launching to manual LAN
                        }

                        if (autopilot.tMinus > 3*vesselState.deltaT)
                        {
                            message += ": T-" + GuiUtils.TimeToDHMS(autopilot.tMinus, 1);
                        }

                        GUILayout.Label(message);

                        if (GUILayout.Button(Localizer.Format("#MechJeb_Ascent_button17")))//Abort
                            launchingToPlane = launchingToRendezvous = launchingToMatchLAN = launchingToLAN = autopilot.timedLaunch = false;
                    }
                }

                if (autopilot.enabled)
                {
                    GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label35") + autopilot.status);//Autopilot status:
                }
                if (core.DeactivateControl)
                {
                    GUIStyle s = new GUIStyle(GUI.skin.label);
                    s.normal.textColor = Color.red;
                    GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label36"), s);//CONTROL DISABLED (AVIONICS)
                }
            }

            if (!vessel.patchedConicsUnlocked() && ascentPathIdx != ascentType.PVG)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_Ascent_label37"));//"Warning: MechJeb is unable to circularize without an upgraded Tracking Station."
            }

            GUILayout.BeginHorizontal();
            autopilot.ascentPathIdxPublic = (ascentType)GuiUtils.ComboBox.Box((int)autopilot.ascentPathIdxPublic, autopilot.ascentPathList, this);
            GUILayout.EndHorizontal();

            if (autopilot.ascentMenu != null) autopilot.ascentMenu.enabled = GUILayout.Toggle(autopilot.ascentMenu.enabled, Localizer.Format("#MechJeb_Ascent_checkbox10"));//Edit ascent path

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(240), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_Ascent_title");//"Ascent Guidance"
        }

        public override string IconName()
        {
            return "Ascent Guidance";
        }
    }
}
