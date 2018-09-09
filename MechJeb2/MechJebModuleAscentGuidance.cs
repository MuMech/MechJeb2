using System;
using UnityEngine;

namespace MuMech
{
    //When enabled, the ascent guidance module makes the purple navball target point
    //along the ascent path. The ascent path can be set via SetPath. The ascent guidance
    //module disables itself if the player selects a different target.
    public class MechJebModuleAscentGuidance : DisplayModule
    {
        public MechJebModuleAscentGuidance(MechJebCore core) : base(core) { }

        public EditableDouble desiredInclination = 0;

        public bool launchingToPlane = false;
        public bool launchingToRendezvous = false;
        public bool launchingToInterplanetary = false;
        public double interplanetaryWindowUT;

        public MechJebModuleAscentAutopilot autopilot { get { return core.GetComputerModule<MechJebModuleAscentAutopilot>(); } }
        public MechJebModuleAscentPEG pegascent { get { return core.GetComputerModule<MechJebModuleAscentPEG>(); } }

        public MechJebModuleAscentBase path;
        public MechJebModuleAscentMenuBase editor;

        MechJebModuleAscentNavBall navBall;

        /* XXX: this is all a bit janky, could rub some reflection on it */
        public int ascentPathIdx { get { return autopilot.ascentPathIdx; } set { autopilot.ascentPathIdx = value; } }
        public string[] ascentPathList = { "Classic Ascent Profile", "Stock-style GravityTurn™", "Primer Vector Guidance (RSS/RO)" };

        private void get_path_and_editor(int i, out MechJebModuleAscentBase p, out MechJebModuleAscentMenuBase e)
        {
            if ( i == 0 )
            {
                p = core.GetComputerModule<MechJebModuleAscentClassic>();
                e = core.GetComputerModule<MechJebModuleAscentClassicMenu>();
            }
            else if ( i == 1 )
            {
                p = core.GetComputerModule<MechJebModuleAscentGT>();
                e = null;
            }
            else if ( i == 2 )
            {
                p = pegascent;
                e = null;
            }
            else
            {
                p = null;
                e = null;
            }
        }

        private void disable_path_modules(int otherthan = -1)
        {
            for(int i = 0; i < ascentPathList.Length; i++)
            {
                if ( i == otherthan ) continue;

                MechJebModuleAscentBase p;
                MechJebModuleAscentMenuBase e;

                get_path_and_editor(i, out p, out e);

                Debug.Log("MechJebModuleAscentGuidance disabling " + i + "th path + editor");
                if (p != null) p.enabled = false;
                if (e != null) e.enabled = false;
            }
        }

        private void enable_path_module(int index)
        {
            disable_path_modules(index);

            get_path_and_editor(index, out path, out editor);

            if (path != null) path.enabled = true;

            autopilot.ascentPath = path;
            /* the editor is not necessarily enabled by this, or the window will always pop up */
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (autopilot != null)
            {
                desiredInclination = autopilot.desiredInclination;
            }
            navBall = core.GetComputerModule<MechJebModuleAscentNavBall>();
            enable_path_module(ascentPathIdx);
        }

        public override void OnModuleEnabled()
        {
            enable_path_module(ascentPathIdx);
        }

        public override void OnModuleDisabled()
        {
            launchingToInterplanetary = false;
            launchingToPlane = false;
            launchingToRendezvous = false;
            disable_path_modules();
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            if (autopilot != null)
            {
                if (autopilot.enabled)
                {
                    if (GUILayout.Button("Disengage autopilot"))
                    {
                        autopilot.users.Remove(this);
                    }
                }
                else
                {
                    if (GUILayout.Button("Engage autopilot"))
                    {
                        autopilot.users.Add(this);
                    }
                }
                if (ascentPathIdx == 2)
                {
                    if (GUILayout.Button("Reset Guidance"))
                        core.optimizer.Reset();

                    GUILayout.BeginHorizontal(); // EditorStyles.toolbar);
                    autopilot.showTargeting = GUILayout.Toggle(autopilot.showTargeting, "TARG"); // , EditorStyles.toolbarButton);
                    autopilot.showGuidanceSettings = GUILayout.Toggle(autopilot.showGuidanceSettings, "GUID");
                    autopilot.showSettings = GUILayout.Toggle(autopilot.showSettings, "OPTS");
                    autopilot.showStatus = GUILayout.Toggle(autopilot.showStatus, "STATUS");
                    GUILayout.EndHorizontal();
                }

                if (autopilot.showTargeting || (ascentPathIdx != 2))
                {
                    if (ascentPathIdx == 2)
                    {

                        GuiUtils.SimpleTextBox("Target Periapsis", autopilot.desiredOrbitAltitude, "km");
                        GuiUtils.SimpleTextBox("Target Apoapsis:", pegascent.desiredApoapsis, "km");
                        if ( pegascent.desiredApoapsis >= 0 && pegascent.desiredApoapsis < autopilot.desiredOrbitAltitude )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.yellow;
                            GUILayout.Label("Apoapsis < Periapsis: circularizing orbit at periapsis", s);
                        }
                    }
                    else
                    {
                        GuiUtils.SimpleTextBox("Orbit altitude", autopilot.desiredOrbitAltitude, "km");
                    }

                    GUIStyle si = new GUIStyle(GUI.skin.label);
                    if (!autopilot.enabled && Math.Abs(desiredInclination) < Math.Abs(vesselState.latitude))
                        si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Orange;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Orbit inc.", si, GUILayout.ExpandWidth(true));
                    desiredInclination.text = GUILayout.TextField(desiredInclination.text, GUILayout.ExpandWidth(true), GUILayout.Width(100));
                    GUILayout.Label("º", GUILayout.ExpandWidth(false));
                    if (GUILayout.Button("Current"))
                        desiredInclination.val = vesselState.latitude;
                    GUILayout.EndHorizontal();
                    autopilot.desiredInclination = desiredInclination;
                }

                if (ascentPathIdx == 2)
                {
                    if (autopilot.showGuidanceSettings)
                    {
                        GuiUtils.SimpleTextBox("Booster Pitch start:", pegascent.pitchStartTime, "s");
                        GuiUtils.SimpleTextBox("Booster Pitch rate:", pegascent.pitchRate, "°/s");
                        GUILayout.BeginHorizontal();
                        pegascent.pitchEndToggle = GUILayout.Toggle(pegascent.pitchEndToggle, "Booster Pitch end:");
                        if (pegascent.pitchEndToggle)
                            GuiUtils.SimpleTextBox("", pegascent.pitchEndTime, "s");
                        GUILayout.EndHorizontal();
                        if (pegascent.pitchEndToggle)
                            GUILayout.Label(String.Format("ending pitch: {0:F1}°", 90.0 - (pegascent.pitchEndTime - pegascent.pitchStartTime)*pegascent.pitchRate));
                        GUILayout.BeginHorizontal();
                        pegascent.pegAfterStageToggle = GUILayout.Toggle(pegascent.pegAfterStageToggle, "Start Guidance after KSP Stage #");
                        if (pegascent.pegAfterStageToggle)
                            GuiUtils.SimpleTextBox("", pegascent.pegAfterStage);
                        GUILayout.EndHorizontal();
                        GuiUtils.SimpleTextBox("Guidance Update Interval:", core.optimizer.pegInterval, "s");
                    }
                }

                if (autopilot.showSettings)
                {
                    navBall.NavBallGuidance = GUILayout.Toggle(navBall.NavBallGuidance, "Show ascent navball guidance");
                    if ( ascentPathIdx != 2 )
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
                        GUILayout.Label("FIXME: throttle limiters are down for maintenance");
                    }
                    GUILayout.BeginHorizontal();
                    autopilot.forceRoll = GUILayout.Toggle(autopilot.forceRoll, "Force Roll");
                    if (autopilot.forceRoll)
                    {
                        GuiUtils.SimpleTextBox("climb", autopilot.verticalRoll, "º", 30f);
                        GuiUtils.SimpleTextBox("turn", autopilot.turnRoll, "º", 30f);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    GUIStyle s = new GUIStyle(GUI.skin.toggle);
                    if (autopilot.limitingAoA) s.onHover.textColor = s.onNormal.textColor = Color.green;
                    autopilot.limitAoA = GUILayout.Toggle(autopilot.limitAoA, "Limit AoA to", s, GUILayout.ExpandWidth(true));
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
                        GuiUtils.SimpleTextBox("Dynamic Pressure Fadeout", autopilot.aoALimitFadeoutPressure, "pa", 50, sl);
                    }
                    GUILayout.EndHorizontal();

                    if ( ascentPathIdx == 0 )
                    {
                        // corrective steering only applies to Classic
                        GUILayout.BeginHorizontal();
                        autopilot.correctiveSteering = GUILayout.Toggle(autopilot.correctiveSteering, "Corrective steering", GUILayout.ExpandWidth(false));
                        if (autopilot.correctiveSteering)
                        {
                            GUILayout.Label("Gain", GUILayout.ExpandWidth(false));
                            autopilot.correctiveSteeringGain.text = GUILayout.TextField(autopilot.correctiveSteeringGain.text, GUILayout.Width(40));
                        }
                        GUILayout.EndHorizontal();
                    }

                    autopilot.autostage = GUILayout.Toggle(autopilot.autostage, "Autostage");
                    if (autopilot.autostage) core.staging.AutostageSettingsInfoItem();

                    autopilot.autodeploySolarPanels = GUILayout.Toggle(autopilot.autodeploySolarPanels,
                            "Auto-deploy solar panels");

                    autopilot.autoDeployAntennas = GUILayout.Toggle(autopilot.autoDeployAntennas,
                            "Auto-deploy antennas");

                    GUILayout.BeginHorizontal();
                    core.node.autowarp = GUILayout.Toggle(core.node.autowarp, "Auto-warp");
                    if ( ascentPathIdx != 2 )
                    {
                        autopilot.skipCircularization = GUILayout.Toggle(autopilot.skipCircularization, "Skip Circularization");
                    }
                    else
                    {
                        // skipCircularization is always true for Optimizer
                        autopilot.skipCircularization = true;
                    }
                    GUILayout.EndHorizontal();
                }

                if (ascentPathIdx == 2)
                {
                    if (autopilot.showStatus)
                    {

                        if (core.optimizer.solution != null)
                        {
                            for(int i = 0; i < core.optimizer.solution.num_segments; i++)
                                GUILayout.Label(String.Format("{0}: {1}", i, core.optimizer.solution.ArcString(vesselState.time, i)));
                        }

                        GUILayout.Label(String.Format("vgo: {0:F1}", core.optimizer.vgo));
                        GUILayout.Label(String.Format("tgo: {0:F3}", core.optimizer.tgo));
                        GUILayout.Label(String.Format("heading: {0:F1}", core.optimizer.heading));
                        GUILayout.Label(String.Format("pitch: {0:F1}", core.optimizer.pitch));
                        GUILayout.BeginHorizontal();
                        GUIStyle si = new GUIStyle(GUI.skin.label);
                        if ( core.optimizer.isStable() )
                            si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Green;
                        else if ( core.optimizer.isInitializing() || core.optimizer.status == PegStatus.FINISHED )
                            si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Orange;
                        else
                            si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Red;
                        GUILayout.Label("Guidance Status: " + core.optimizer.status, si);
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("core.optimizer Status: *FIXME*");
                        GUILayout.EndHorizontal();
                    }
                }

                if (vessel.LandedOrSplashed)
                {
                        if (core.target.NormalTargetExists)
                        {
                            if (core.node.autowarp)
                            {
                                GUILayout.BeginHorizontal();
                                GUILayout.Label("Launch countdown:", GUILayout.ExpandWidth(true));
                                autopilot.warpCountDown.text = GUILayout.TextField(autopilot.warpCountDown.text,
                                        GUILayout.Width(60));
                                GUILayout.Label("s", GUILayout.ExpandWidth(false));
                                GUILayout.EndHorizontal();
                            }
                            if (!launchingToPlane && !launchingToRendezvous && !launchingToInterplanetary)
                            {
                                // disable plane/rendezvous/interplanetary for now
                                if ( ascentPathIdx != 2 )
                                {
                                    GUILayout.BeginHorizontal();
                                    if (GUILayout.Button("Launch to rendezvous:", GUILayout.ExpandWidth(false)))
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
                                if (GUILayout.Button("Launch into plane of target", GUILayout.ExpandWidth(false)))
                                {
                                    launchingToPlane = true;

                                    autopilot.StartCountdown(vesselState.time +
                                            LaunchTiming.TimeToPlane(autopilot.launchLANDifference,
                                                mainBody, vesselState.latitude, vesselState.longitude,
                                                core.target.TargetOrbit));
                                }
                                autopilot.launchLANDifference.text = GUILayout.TextField(
                                        autopilot.launchLANDifference.text, GUILayout.Width(60));
                                GUILayout.Label("º", GUILayout.ExpandWidth(false));
                                GUILayout.EndHorizontal();

                                if (core.target.TargetOrbit.referenceBody == orbit.referenceBody.referenceBody)
                                {
                                    if (GUILayout.Button("Launch at interplanetary window"))
                                    {
                                        launchingToInterplanetary = true;
                                        //compute the desired launch date
                                        OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(mainBody.orbit,
                                                core.target.TargetOrbit, vesselState.time, out interplanetaryWindowUT);
                                        double desiredOrbitPeriod = 2 * Math.PI *
                                            Math.Sqrt(
                                                    Math.Pow( mainBody.Radius + autopilot.desiredOrbitAltitude, 3)
                                                    / mainBody.gravParameter);
                                        //launch just before the window, but don't try to launch in the past
                                        interplanetaryWindowUT -= 3*desiredOrbitPeriod;
                                        interplanetaryWindowUT = Math.Max(vesselState.time + autopilot.warpCountDown,
                                                interplanetaryWindowUT);
                                        autopilot.StartCountdown(interplanetaryWindowUT);
                                    }
                                }
                            }
                        }
                        else
                        {
                            launchingToInterplanetary = launchingToPlane = launchingToRendezvous = false;
                            GUILayout.Label("Select a target for a timed launch.");
                        }

                        if (launchingToInterplanetary || launchingToPlane || launchingToRendezvous)
                        {
                            string message = "";
                            if (launchingToInterplanetary)
                            {
                                message = "Launching at interplanetary window";
                            }
                            else if (launchingToPlane)
                            {
                                desiredInclination *=
                                    Math.Sign(Vector3d.Dot(core.target.TargetOrbit.SwappedOrbitNormal(),
                                                Vector3d.Cross(vesselState.CoM - mainBody.position, mainBody.transform.up)));
                                message = "Launching to target plane";
                            }
                            else if (launchingToRendezvous)
                            {
                                message = "Launching to rendezvous";
                            }

                            if (autopilot.tMinus > 3*vesselState.deltaT)
                            {
                                message += ": T-" + GuiUtils.TimeToDHMS(autopilot.tMinus, 1);
                            }

                            GUILayout.Label(message);

                            if (GUILayout.Button("Abort"))
                                launchingToInterplanetary =
                                    launchingToPlane = launchingToRendezvous = autopilot.timedLaunch = false;
                        }
                }

                if (autopilot.enabled)
                {
                    GUILayout.Label("Autopilot status: " + autopilot.status);
                }
            }

            if (!vessel.patchedConicsUnlocked() && ascentPathIdx != 2)
            {
                GUILayout.Label("Warning: MechJeb is unable to circularize without an upgraded Tracking Station.");
            }

            int last_idx = ascentPathIdx;

            GUILayout.BeginHorizontal();
            ascentPathIdx = GuiUtils.ComboBox.Box(ascentPathIdx, ascentPathList, this);
            GUILayout.EndHorizontal();

            if (last_idx != ascentPathIdx) {
                bool last_enabled = editor.enabled;

                enable_path_module(ascentPathIdx);

                editor.enabled = last_enabled;
            }

            if (ascentPathIdx == 0)
                if (editor != null) editor.enabled = GUILayout.Toggle(editor.enabled, "Edit ascent path");

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(240), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return "Ascent Guidance";
        }
    }
}
