using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //When enabled, the ascent guidance module makes the purple navball target point
    //along the ascent path. The ascent path can be set via SetPath. The ascent guidance
    //module disables itself if the player selects a different target.
    public class MechJebModuleAscentGuidance : DisplayModule
    {
        public MechJebModuleAscentGuidance(MechJebCore core) : base(core) { }

        protected const string TARGET_NAME = "Ascent Path Guidance";

        public IAscentPath ascentPath = null;

        public EditableDouble desiredInclination = 0;

        public bool launchingToPlane = false;
        public bool launchingToRendezvous = false;
        public bool launchingToInterplanetary = false;
        public double interplanetaryWindowUT;

        MechJebModuleAscentAutopilot autopilot;

        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleAscentAutopilot>();
            if (autopilot != null) desiredInclination = autopilot.desiredInclination;
        }

        public override void OnModuleEnabled()
        {
        }

        public override void OnModuleDisabled()
        {
            if (core.target.NormalTargetExists && (core.target.Name == TARGET_NAME)) core.target.Unset();
            launchingToInterplanetary = false;
            launchingToPlane = false;
            launchingToRendezvous = false;
            MechJebModuleAscentPathEditor editor = core.GetComputerModule<MechJebModuleAscentPathEditor>();
            if (editor != null) editor.enabled = false;
        }

        public override void OnFixedUpdate()
        {
            if (ascentPath == null) return;

            if (core.target.Target != null && core.target.Name == TARGET_NAME)
            {
                double angle = Math.PI / 180 * ascentPath.FlightPathAngle(vesselState.altitudeASL, vesselState.speedSurface);
                double heading = Math.PI / 180 * OrbitalManeuverCalculator.HeadingForInclination(desiredInclination, vesselState.latitude);
                Vector3d horizontalDir = Math.Cos(heading) * vesselState.north + Math.Sin(heading) * vesselState.east;
                Vector3d dir = Math.Cos(angle) * horizontalDir + Math.Sin(angle) * vesselState.up;
                core.target.UpdateDirectionTarget(dir);
            }
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            bool showingGuidance = (core.target.Target != null && core.target.Name == TARGET_NAME);

            if (showingGuidance)
            {
                GUILayout.Label("The purple circle on the navball points along the ascent path.");
                if (GUILayout.Button("Stop showing navball guidance")) core.target.Unset();
            }
            else if (GUILayout.Button("Show navball ascent path guidance"))
            {
                core.target.SetDirectionTarget(TARGET_NAME);
            }

            if (autopilot != null)
            {
                if (autopilot.enabled)
                {
                    if (GUILayout.Button("Disengage autopilot")) autopilot.users.Remove(this);
                }
                else
                {
                    if (GUILayout.Button("Engage autopilot"))
                    {
                        autopilot.users.Add(this);
                    }
                }

                ascentPath = autopilot.ascentPath;

                GuiUtils.SimpleTextBox("Orbit altitude", autopilot.desiredOrbitAltitude, "km");
                autopilot.desiredInclination = desiredInclination;
            }

            GuiUtils.SimpleTextBox("Orbit inclination", desiredInclination, "º");

            core.thrust.LimitToPreventOverheatsInfoItem();
            core.thrust.LimitToTerminalVelocityInfoItem();
            core.thrust.LimitAccelerationInfoItem();
            core.thrust.LimitThrottleInfoItem();
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
            if (autopilot.limitAoA) {
                GUIStyle sl = new GUIStyle(GUI.skin.label);
                if (autopilot.limitingAoA && vesselState.dynamicPressure < autopilot.aoALimitFadeoutPressure)
                    sl.normal.textColor = sl.hover.textColor = Color.green;
                GuiUtils.SimpleTextBox("Dynamic Pressure Fadeout", autopilot.aoALimitFadeoutPressure, "pa", 50, sl);
            }
            GUILayout.EndHorizontal();

            autopilot.correctiveSteering = GUILayout.Toggle(autopilot.correctiveSteering, "Corrective steering");

            autopilot.autostage = GUILayout.Toggle(autopilot.autostage, "Autostage");
            if (autopilot.autostage) core.staging.AutostageSettingsInfoItem();

            autopilot.autodeploySolarPanels = GUILayout.Toggle(autopilot.autodeploySolarPanels, "Auto-deploy solar panels");

            core.node.autowarp = GUILayout.Toggle(core.node.autowarp, "Auto-warp");

            if (autopilot != null && vessel.LandedOrSplashed)
            {
                if (core.target.NormalTargetExists)
                {
                    if (core.node.autowarp)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label("Launch countdown:", GUILayout.ExpandWidth(true));
                        autopilot.warpCountDown.text = GUILayout.TextField(autopilot.warpCountDown.text, GUILayout.Width(60));
                        GUILayout.Label("s", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();
                    }
                    if (!launchingToPlane && !launchingToRendezvous && !launchingToInterplanetary)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Launch to rendezvous:", GUILayout.ExpandWidth(false)))
                        {
                            launchingToRendezvous = true;
                            autopilot.StartCountdown(vesselState.time + LaunchTiming.TimeToPhaseAngle(autopilot.launchPhaseAngle, mainBody, vesselState.longitude, core.target.TargetOrbit));
                        }
                        autopilot.launchPhaseAngle.text = GUILayout.TextField(autopilot.launchPhaseAngle.text, GUILayout.Width(60));
                        GUILayout.Label("º", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button("Launch into plane of target"))
                        {
                            launchingToPlane = true;
                            autopilot.StartCountdown( vesselState.time +
                                LaunchTiming.TimeToPlane(mainBody, vesselState.latitude, vesselState.longitude, core.target.TargetOrbit));
                        }
                        if (core.target.TargetOrbit.referenceBody == orbit.referenceBody.referenceBody)
                        {
                            if (GUILayout.Button("Launch at interplanetary window"))
                            {
                                launchingToInterplanetary = true;
                                //compute the desired launch date
                                OrbitalManeuverCalculator.DeltaVAndTimeForHohmannTransfer(mainBody.orbit, core.target.TargetOrbit, vesselState.time, out interplanetaryWindowUT);
                                double desiredOrbitPeriod = 2 * Math.PI * Math.Sqrt(Math.Pow(mainBody.Radius + autopilot.desiredOrbitAltitude, 3) / mainBody.gravParameter);
                                //launch just before the window, but don't try to launch in the past                                
                                interplanetaryWindowUT -= 3 * desiredOrbitPeriod;
                                interplanetaryWindowUT = Math.Max(vesselState.time + autopilot.warpCountDown, interplanetaryWindowUT);
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
                        desiredInclination = core.target.TargetOrbit.inclination;
                        desiredInclination *= Math.Sign(Vector3d.Dot(core.target.TargetOrbit.SwappedOrbitNormal(), Vector3d.Cross(vesselState.CoM - mainBody.position, mainBody.transform.up)));
                        message = "Launching to target plane";
                    }
                    else if (launchingToRendezvous)
                    {
                        message = "Launching to rendezvous";
                    }

                    if (autopilot.tMinus > 3 * vesselState.deltaT)
                    {
                        message += ": T-" + GuiUtils.TimeToDHMS(autopilot.tMinus, 1);
                    }

                    GUILayout.Label(message);

                    if (GUILayout.Button("Abort")) launchingToInterplanetary = launchingToPlane = launchingToRendezvous = autopilot.timedLaunch = false;
                }
            }

            if (autopilot != null && autopilot.enabled)
            {
                GUILayout.Label("Autopilot status: " + autopilot.status);
            }

            if (!vessel.patchedConicsUnlocked())
            {
                GUILayout.Label("Warning: MechJeb is unable to circularize without an upgraded Tracking Station.");
            }

            MechJebModuleAscentPathEditor editor = core.GetComputerModule<MechJebModuleAscentPathEditor>();
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
