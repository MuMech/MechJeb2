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

        public override void OnModuleEnabled()
        {
            ascentPath = new DefaultAscentPath();
            core.target.SetDirectionTarget(TARGET_NAME);
        }

        public override void OnModuleDisabled()
        {
            if (core.target.Name == TARGET_NAME) core.target.Unset();
        }

        public override void OnFixedUpdate()
        {
            if (!enabled) return;

            if (core.target.Name != TARGET_NAME)
            {
                enabled = false;
                return;
            }

            if (ascentPath == null) return;

            double angle = Math.PI / 180 * ascentPath.FlightPathAngle(vesselState.altitudeASL);
            Vector3d dir = Math.Cos(angle) * vesselState.east + Math.Sin(angle) * vesselState.up;
            core.target.UpdateDirectionTarget(dir);
        }

        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("The purple circle on the navball points along the ascent path.");

            if (GUILayout.Button("Turn off guidance")) enabled = false;

            core.thrust.limitToPreventOverheats = GUILayout.Toggle(core.thrust.limitToPreventOverheats, "Limit throttle to avoid overheats");
            core.thrust.limitToTerminalVelocity = GUILayout.Toggle(core.thrust.limitToTerminalVelocity, "Limit throttle so as not to exceed terminal velocity");
            core.thrust.enabled = (core.thrust.limitToPreventOverheats || core.thrust.limitToTerminalVelocity);

            core.staging.enabled = GUILayout.Toggle(core.staging.enabled, "Auto stage");

            MechJebModuleAscentComputer autopilot = core.GetComputerModule<MechJebModuleAscentComputer>();
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
