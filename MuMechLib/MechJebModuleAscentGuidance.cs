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

        public IAscentPath ascentPath; //other modules can edit this to control what path the guidance uses

        bool launchingToPlane = false;
        bool launchingToRendezvous = false;
        EditableDouble phaseAngle = 0;

        public override void OnModuleEnabled()
        {
            ascentPath = new DefaultAscentPath();
            core.target.SetDirectionTarget(TARGET_NAME);
            core.thrust.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            if (core.target.Name == TARGET_NAME) core.target.Unset();
            core.thrust.users.Remove(this);
            launchingToPlane = false;
            launchingToRendezvous = false;
        }

        public override void OnFixedUpdate()
        {
            /*if (core.target.Name != TARGET_NAME)
            {
                enabled = false;
                return;
            }*/

            if (ascentPath == null) return;

            if (core.target.Name == TARGET_NAME)
            {
                double angle = Math.PI / 180 * ascentPath.FlightPathAngle(vesselState.altitudeASL);
                Vector3d dir = Math.Cos(angle) * vesselState.east + Math.Sin(angle) * vesselState.up;
                core.target.UpdateDirectionTarget(dir);
            }
        }

        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("The purple circle on the navball points along the ascent path.");

            if (GUILayout.Button("Turn off guidance")) enabled = false;

            core.thrust.limitToPreventOverheats = GUILayout.Toggle(core.thrust.limitToPreventOverheats, "Limit throttle to avoid overheats");
            core.thrust.limitToTerminalVelocity = GUILayout.Toggle(core.thrust.limitToTerminalVelocity, "Limit throttle so as not to exceed terminal velocity");
            GUILayout.BeginHorizontal();
            core.thrust.limitAcceleration = GUILayout.Toggle(core.thrust.limitAcceleration, "Limit acceleration to", GUILayout.ExpandWidth(false));
            core.thrust.maxAcceleration.text = GUILayout.TextField(core.thrust.maxAcceleration.text, GUILayout.ExpandWidth(true));
            GUILayout.Label("m/s²", GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
            
            core.staging.enabled = GUILayout.Toggle(core.staging.enabled, "Auto stage");


            MechJebModuleAscentComputer autopilot = core.GetComputerModule<MechJebModuleAscentComputer>();

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
                if(launchingToPlane) tMinus = LaunchTiming.TimeToPlane(mainBody, vesselState.latitude, vesselState.longitude, core.target.Orbit);
                else tMinus = LaunchTiming.TimeToPhaseAngle(phaseAngle, mainBody, vesselState.longitude, core.target.Orbit);

                double launchTime = vesselState.time + tMinus;
                double desiredInclination = core.target.Orbit.inclination;
                desiredInclination *= Math.Sign(Vector3d.Dot(core.target.Orbit.SwappedOrbitNormal(), Vector3d.Cross(vesselState.CoM - mainBody.position, mainBody.transform.up)));
                autopilot.desiredInclination = desiredInclination;

                core.warp.WarpToUT(launchTime);

                GUILayout.Label("Launching to " + (launchingToPlane ? "target plane" : "rendezvous") + ": T-" + MuUtils.ToSI(tMinus, 0) + "s");
                if (tMinus < 3 * vesselState.deltaT)
                {
                    Staging.ActivateNextStage();
                    launchingToPlane = launchingToRendezvous = false;
                }

                if (GUILayout.Button("Abort")) launchingToPlane = launchingToRendezvous = false;
            } 
            

            autopilot.enabled = GUILayout.Toggle(autopilot.enabled, "Autopilot enable");
            if (autopilot.enabled)
            {
                GUILayout.Label("State: " + autopilot.mode.ToString());
            }

            GUILayout.EndVertical();

            base.FlightWindowGUI(windowID);
        }

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[]{ GUILayout.Width(200), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return "Ascent Guidance";
        }
    }
}
