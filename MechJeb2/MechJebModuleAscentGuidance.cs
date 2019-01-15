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
        public MechJebModuleAscentPVG pvgascent { get { return core.GetComputerModule<MechJebModuleAscentPVG>(); } }
        public MechJebModuleAscentGT gtascent { get { return core.GetComputerModule<MechJebModuleAscentGT>(); } }

        private ascentType ascentPathIdx { get { return autopilot.ascentPathIdxPublic; } }

        MechJebModuleAscentNavBall navBall;

        public override void OnStart(PartModule.StartState state)
        {
            if (autopilot != null)
            {
                desiredInclination = autopilot.desiredInclination;  // FIXME: remove this indirection
            }
            navBall = core.GetComputerModule<MechJebModuleAscentNavBall>();
        }

        public override void OnModuleDisabled()
        {
            launchingToInterplanetary = false;
            launchingToPlane = false;
            launchingToRendezvous = false;
        }

        [GeneralInfoItem("Toggle Ascent Navball Guidance", InfoItem.Category.Misc, showInEditor = false)]
            public void ToggleAscentNavballGuidanceInfoItem()
            {
                if (navBall != null)
                {
                    if (navBall.NavBallGuidance)
                    {
                        if (GUILayout.Button("Hide ascent navball guidance"))
                            navBall.NavBallGuidance = false;
                    }
                    else
                    {
                        if (GUILayout.Button("Show ascent navball guidance"))
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
                if (ascentPathIdx == ascentType.PVG)
                {
                    if (GUILayout.Button("Reset Guidance (DO NOT PRESS)"))
                        core.guidance.Reset();


                    GUILayout.BeginHorizontal(); // EditorStyles.toolbar);

                    if ( GUILayout.Button("TARG", autopilot.showTargeting ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showTargeting = !autopilot.showTargeting;
                    if ( GUILayout.Button("GUID", autopilot.showGuidanceSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showGuidanceSettings = !autopilot.showGuidanceSettings;
                    if ( GUILayout.Button("OPTS", autopilot.showSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showSettings = !autopilot.showSettings;
                    if ( GUILayout.Button("STATUS", autopilot.showStatus ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showStatus = !autopilot.showStatus;
                    GUILayout.EndHorizontal();
                }
                else if (ascentPathIdx == ascentType.GRAVITYTURN)
                {
                    GUILayout.BeginHorizontal(); // EditorStyles.toolbar);
                    if ( GUILayout.Button("TARG", autopilot.showTargeting ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showTargeting = !autopilot.showTargeting;
                    if ( GUILayout.Button("GUID", autopilot.showGuidanceSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showGuidanceSettings = !autopilot.showGuidanceSettings;
                    if ( GUILayout.Button("OPTS", autopilot.showSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showSettings = !autopilot.showSettings;
                    GUILayout.EndHorizontal();
                }
                else if (ascentPathIdx == ascentType.CLASSIC)
                {
                    GUILayout.BeginHorizontal(); // EditorStyles.toolbar);
                    if ( GUILayout.Button("TARG", autopilot.showTargeting ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showTargeting = !autopilot.showTargeting;
                    if ( GUILayout.Button("OPTS", autopilot.showSettings ? btActive : btNormal, GUILayout.ExpandWidth(true)) )
                        autopilot.showSettings = !autopilot.showSettings;
                    GUILayout.EndHorizontal();
                }

                if (autopilot.showTargeting)
                {
                    if (ascentPathIdx == ascentType.PVG)
                    {

                        GuiUtils.SimpleTextBox("Target Periapsis", autopilot.desiredOrbitAltitude, "km");
                        GuiUtils.SimpleTextBox("Target Apoapsis:", pvgascent.desiredApoapsis, "km");
                        if ( pvgascent.desiredApoapsis >= 0 && pvgascent.desiredApoapsis < autopilot.desiredOrbitAltitude )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.yellow;
                            GUILayout.Label("Ap < Pe: circularizing orbit", s);
                        }
                        if ( pvgascent.desiredApoapsis < 0 )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = XKCDColors.Orange;
                            GUILayout.Label("Hyperbolic target orbit (neg Ap)", s);
                        }
                    }
                    else
                    {
                        GuiUtils.SimpleTextBox("Orbit altitude", autopilot.desiredOrbitAltitude, "km");
                    }

                    GUIStyle si = new GUIStyle(GUI.skin.label);
                    if (Math.Abs(desiredInclination) < Math.Abs(vesselState.latitude) - 2.001)
                        si.onHover.textColor = si.onNormal.textColor = si.normal.textColor = XKCDColors.Orange;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Orbit inc.", si, GUILayout.ExpandWidth(true));
                    desiredInclination.text = GUILayout.TextField(desiredInclination.text, GUILayout.ExpandWidth(true), GUILayout.Width(100));
                    GUILayout.Label("º", GUILayout.ExpandWidth(false));
                    if (GUILayout.Button("Current"))
                        desiredInclination.val = vesselState.latitude;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                    if (Math.Abs(desiredInclination) < Math.Abs(vesselState.latitude) - 2.001)
                        GUILayout.Label(String.Format("inc {0:F1}º below current latitude", Math.Abs(vesselState.latitude) - Math.Abs(desiredInclination)), si);
                    GUILayout.EndHorizontal();
                    autopilot.desiredInclination = desiredInclination;
                }

                if (autopilot.showGuidanceSettings)
                {
                    if (ascentPathIdx == ascentType.GRAVITYTURN)
                    {
                        GUILayout.BeginVertical();

                        GuiUtils.SimpleTextBox("Turn start altitude:", gtascent.turnStartAltitude, "km");
                        GuiUtils.SimpleTextBox("Turn start velocity:", gtascent.turnStartVelocity, "m/s");
                        GuiUtils.SimpleTextBox("Turn start pitch:", gtascent.turnStartPitch, "deg");
                        GuiUtils.SimpleTextBox("Intermediate altitude:", gtascent.intermediateAltitude, "km");
                        GuiUtils.SimpleTextBox("Hold AP Time:", gtascent.holdAPTime, "s");

                        GUILayout.EndVertical();
                    }
                    else if (ascentPathIdx == ascentType.PVG)
                    {
                        GUILayout.BeginVertical();
                        GuiUtils.SimpleTextBox("Booster Pitch start:", pvgascent.pitchStartVelocity, "m/s");
                        GuiUtils.SimpleTextBox("Booster Pitch rate:", pvgascent.pitchRate, "°/s");
                        GuiUtils.SimpleTextBox("Guidance Interval:", core.guidance.pvgInterval, "s");
                        if ( core.guidance.pvgInterval < 1 || core.guidance.pvgInterval > 30 )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.yellow;
                            GUILayout.Label("Guidance intervals are limited to between 1s and 30s", s);
                        }
                        GuiUtils.SimpleTextBox("Qα limit", autopilot.limitQa, "Pa-rad");
                        if ( autopilot.limitQa < 100 || autopilot.limitQa > 4000 )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.yellow;

                            if ( autopilot.limitQa < 100 )
                                GUILayout.Label("Qα limit cannot be set to lower than 100 Pa-rad", s);
                            else if ( autopilot.limitQa > 10000 )
                                GUILayout.Label("Qα limit cannot be set to higher than 10000 Pa-rad", s);
                            else
                                GUILayout.Label("Qα limit is recommended to be 1000 to 4000 Pa-rad", s);
                        }
                        pvgascent.omitCoast = GUILayout.Toggle(pvgascent.omitCoast, "Omit Coast");
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
                        //core.thrust.LimitAccelerationInfoItem();
                        //core.thrust.LimitThrottleInfoItem();
                        core.thrust.LimiterMinThrottleInfoItem();
                        //core.thrust.LimitElectricInfoItem();

                        GUILayout.Label("FIXME: g-limiter is down for maintenance");
                        core.thrust.limitAcceleration = false;
                        core.thrust.limitThrottle = false;
                        core.thrust.limitToTerminalVelocity = false;
                        core.thrust.electricThrottle = false;
                    }

                    GUILayout.BeginHorizontal();
                    autopilot.forceRoll = GUILayout.Toggle(autopilot.forceRoll, "Force Roll");
                    if (autopilot.forceRoll)
                    {
                        GuiUtils.SimpleTextBox("climb", autopilot.verticalRoll, "º", 30f);
                        GuiUtils.SimpleTextBox("turn", autopilot.turnRoll, "º", 30f);
                    }
                    GUILayout.EndHorizontal();

                    if (ascentPathIdx != ascentType.PVG)
                    {
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
                            GuiUtils.SimpleTextBox("Dynamic Pressure Fadeout", autopilot.aoALimitFadeoutPressure, "Pa", 50, sl);
                        }
                        GUILayout.EndHorizontal();
                        autopilot.limitQaEnabled = false; // this is only for PVG
                    }

                    if ( ascentPathIdx == ascentType.CLASSIC )
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
                    if ( ascentPathIdx != ascentType.PVG )
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
                        GUILayout.Label("Guidance Status: " + core.guidance.status, si);
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("converges: " + core.guidance.successful_converges, GUILayout.Width(100));
                        GUILayout.Label("status: " + core.guidance.last_lm_status, GUILayout.Width(100));
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("n: " + core.guidance.last_lm_iteration_count + "(" + core.guidance.max_lm_iteration_count + ")", GUILayout.Width(100));
                        GUILayout.Label("staleness: " + GuiUtils.TimeToDHMS(core.guidance.staleness));
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(String.Format("znorm: {0:G5}", core.guidance.last_znorm));
                        GUILayout.EndHorizontal();
                        if ( core.guidance.last_failure_cause != null )
                        {
                            GUIStyle s = new GUIStyle(GUI.skin.label);
                            s.normal.textColor = Color.red;
                            GUILayout.BeginHorizontal();
                            GUILayout.Label("LAST FAILURE: " + core.guidance.last_failure_cause, s);
                            GUILayout.EndHorizontal();
                        }
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
                                if ( ascentPathIdx != ascentType.PVG )
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

            if (!vessel.patchedConicsUnlocked() && ascentPathIdx != ascentType.PVG)
            {
                GUILayout.Label("Warning: MechJeb is unable to circularize without an upgraded Tracking Station.");
            }

            GUILayout.BeginHorizontal();
            autopilot.ascentPathIdxPublic = (ascentType)GuiUtils.ComboBox.Box((int)autopilot.ascentPathIdxPublic, autopilot.ascentPathList, this);
            GUILayout.EndHorizontal();

            if (autopilot.ascentMenu != null) autopilot.ascentMenu.enabled = GUILayout.Toggle(autopilot.ascentMenu.enabled, "Edit ascent path");

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
