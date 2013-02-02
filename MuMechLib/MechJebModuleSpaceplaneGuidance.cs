using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleSpaceplaneGuidance : DisplayModule
    {
        MechJebModuleSpaceplaneAutopilot autopilot;

        public override GUILayoutOption[] FlightWindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(350), GUILayout.Height(200) };
        }

        protected override void FlightWindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label("Landing", s);

            Runway[] runways = MechJebModuleSpaceplaneAutopilot.runways;
            int runwayIndex = Array.IndexOf(runways, autopilot.runway);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀", GUILayout.ExpandWidth(false))) runwayIndex = (runwayIndex - 1 + runways.Length) % runways.Length;
            GUILayout.Label(autopilot.runway.name);
            if (GUILayout.Button("▶", GUILayout.ExpandWidth(false))) runwayIndex = (runwayIndex + 1) % runways.Length;
            autopilot.runway = runways[runwayIndex];
            GUILayout.EndHorizontal();

            GUILayout.Label("Distance to runway: " + MuUtils.ToSI(Vector3d.Distance(vesselState.CoM, autopilot.runway.Start(vesselState.CoM)), 0) + "m");

            autopilot.showLandingTarget = GUILayout.Toggle(autopilot.showLandingTarget, "Show landing navball guidance");

            if (GUILayout.Button("Autoland")) autopilot.Autoland();
            if (autopilot.enabled && autopilot.mode == MechJebModuleSpaceplaneAutopilot.Mode.AUTOLAND
                && GUILayout.Button("Abort")) autopilot.AutopilotOff();

            GuiUtils.SimpleTextBox("Autoland glideslope:", autopilot.glideslope, "º");

            GUILayout.Label("Hold", s);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Initiate hold:")) autopilot.HoldHeadingAndAltitude();
            GUILayout.Label("Heading:");
            autopilot.targetHeading.text = GUILayout.TextField(autopilot.targetHeading.text, GUILayout.Width(40));
            GUILayout.Label("º Altitude:");
            autopilot.targetAltitude.text = GUILayout.TextField(autopilot.targetAltitude.text, GUILayout.Width(40));
            GUILayout.Label("m");
            GUILayout.EndHorizontal();

            if (autopilot.enabled && autopilot.mode == MechJebModuleSpaceplaneAutopilot.Mode.HOLD
                && GUILayout.Button("Abort")) autopilot.AutopilotOff();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }


        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleSpaceplaneAutopilot>();
            autopilot.enabled = true;
        }

        public MechJebModuleSpaceplaneGuidance(MechJebCore core) : base(core) { }

        public override string GetName()
        {
            return "Spaceplane Guidance";
        }
    }
}
