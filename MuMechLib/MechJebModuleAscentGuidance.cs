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

        const string TARGET_NAME = "Ascent Path Guidance";

        public IAscentPath ascentPath = new DefaultAscentPath();

        bool launchingToPlane = false;
        bool launchingToRendezvous = false;
        EditableDouble phaseAngle = 0;

        public override void OnModuleEnabled()
        {
        }

        public override void OnModuleDisabled()
        {
            if (core.target.NormalTargetExists && (core.target.Name == TARGET_NAME)) core.target.Unset();
            launchingToPlane = false;
            launchingToRendezvous = false;
            MechJebModuleAscentPathEditor editor = core.GetComputerModule<MechJebModuleAscentPathEditor>();
            if(editor != null) editor.enabled = false;
        }

        public override void OnFixedUpdate()
        {
            if (ascentPath == null) return;

            if (core.target.Target != null && core.target.Name == TARGET_NAME)
            {
                double angle = Math.PI / 180 * ascentPath.FlightPathAngle(vesselState.altitudeASL);
                Vector3d dir = Math.Cos(angle) * vesselState.east + Math.Sin(angle) * vesselState.up;
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

            MechJebModuleAscentAutopilot autopilot = core.GetComputerModule<MechJebModuleAscentAutopilot>();

            if (autopilot != null)
            {
                //autopilot.enabled = GUILayout.Toggle(autopilot.enabled, (autopilot.enabled ? "Disengage autopilot" : "Engage autopilot"), GUI.skin.button);
                if (autopilot.enabled)
                {
                    if (GUILayout.Button("Disengage autopilot")) autopilot.enabled = false;
                }
                else
                {
                    if (GUILayout.Button("Engage autopilot"))
                    {
                        autopilot.enabled = true;
                        autopilot.ascentPath = this.ascentPath;
                    }
                }

                GuiUtils.SimpleTextBox("Orbit altitude", autopilot.desiredOrbitAltitude, "km");
                GuiUtils.SimpleTextBox("Orbit inclination", autopilot.desiredInclination, "º");
            }

            core.thrust.limitToPreventOverheats = GUILayout.Toggle(core.thrust.limitToPreventOverheats, "Prevent overheats");
            core.thrust.limitToTerminalVelocity = GUILayout.Toggle(core.thrust.limitToTerminalVelocity, "Limit to terminal velocity");
            GUILayout.BeginHorizontal();
            core.thrust.limitAcceleration = GUILayout.Toggle(core.thrust.limitAcceleration, "Limit acceleration to", GUILayout.ExpandWidth(false));
            core.thrust.maxAcceleration.text = GUILayout.TextField(core.thrust.maxAcceleration.text, GUILayout.ExpandWidth(true));
            GUILayout.Label("m/s²", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            autopilot.correctiveSteering = GUILayout.Toggle(autopilot.correctiveSteering, "Corrective steering");
            
            //core.staging.enabled = GUILayout.Toggle(core.staging.enabled, "Auto stage");
            core.staging.AutostageInfoItem();

            if (autopilot != null)
            {
                if (core.target.NormalTargetExists && vessel.Landed)
                {
                    if (!launchingToPlane && !launchingToRendezvous && GUILayout.Button("Launch into plane of target"))
                    {
                        launchingToPlane = true;
                    }
                    if (!launchingToPlane && !launchingToRendezvous)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button("Launch at phase angle", GUILayout.ExpandWidth(false)))
                        {
                            launchingToRendezvous = true;
                        }
                        phaseAngle.text = GUILayout.TextField(phaseAngle.text, GUILayout.ExpandWidth(true));
                        GUILayout.Label("º", GUILayout.ExpandWidth(false));
                        GUILayout.EndHorizontal();
                    }
                }
                else
                {
                    launchingToPlane = launchingToRendezvous = false;
                }

                if (launchingToPlane || launchingToRendezvous)
                {
                    double tMinus;
                    if (launchingToPlane) tMinus = LaunchTiming.TimeToPlane(mainBody, vesselState.latitude, vesselState.longitude, core.target.Orbit);
                    else tMinus = LaunchTiming.TimeToPhaseAngle(phaseAngle, mainBody, vesselState.longitude, core.target.Orbit);

                    double launchTime = vesselState.time + tMinus;

                    if (launchingToRendezvous)
                    {
                        double desiredInclination = core.target.Orbit.inclination;
                        desiredInclination *= Math.Sign(Vector3d.Dot(core.target.Orbit.SwappedOrbitNormal(), Vector3d.Cross(vesselState.CoM - mainBody.position, mainBody.transform.up)));
                        autopilot.desiredInclination = desiredInclination;
                    }

                    if (autopilot.enabled) core.warp.WarpToUT(launchTime);

                    GUILayout.Label("Launching to " + (launchingToPlane ? "target plane" : "rendezvous") + ": T-" + MuUtils.ToSI(tMinus, 0) + "s");
                    if (tMinus < 3 * vesselState.deltaT)
                    {
                        if (autopilot.enabled) Staging.ActivateNextStage();
                        launchingToPlane = launchingToRendezvous = false;
                    }

                    if (GUILayout.Button("Abort")) launchingToPlane = launchingToRendezvous = false;
                }

                if (autopilot.enabled)
                {
                    GUILayout.Label("Autopilot status: " + autopilot.status);
                }
            }

            MechJebModuleAscentPathEditor editor = core.GetComputerModule<MechJebModuleAscentPathEditor>();
            if(editor != null) editor.enabled = GUILayout.Toggle(editor.enabled, "Edit ascent path");

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[]{ GUILayout.Width(200), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return "Ascent Guidance";
        }
    }
}
