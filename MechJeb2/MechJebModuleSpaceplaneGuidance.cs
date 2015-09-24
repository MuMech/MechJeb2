using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSpaceplaneGuidance : DisplayModule
    {
        MechJebModuleSpaceplaneAutopilot autopilot;

        protected bool _showLandingTarget = false;

        [Persistent(pass = (int)(Pass.Global | Pass.Local))]
        public int runwayIndex = 0;

        public bool showLandingTarget
        {
            get { return _showLandingTarget; }
            set
            {
                if (value && !_showLandingTarget) core.target.SetDirectionTarget("ILS Guidance");
                if (!value && (core.target.Target is DirectionTarget && core.target.Name == "ILS Guidance")) core.target.Unset();
                _showLandingTarget = value;
            }
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.alignment = TextAnchor.MiddleCenter;

            List<Runway> availableRunways = MechJebModuleSpaceplaneAutopilot.runways.Where(p => p.body == mainBody).ToList();
            if (runwayIndex > availableRunways.Count)
                runwayIndex = 0;

            if (availableRunways.Any())
            {
                GUILayout.Label("Landing", s);

                runwayIndex = GuiUtils.ComboBox.Box(runwayIndex, availableRunways.Select(p => p.name).ToArray(), this);
                autopilot.runway = availableRunways[runwayIndex];

                GUILayout.Label("Distance to runway: " + MuUtils.ToSI(Vector3d.Distance(vesselState.CoM, autopilot.runway.Start(vesselState.CoM)), 0) + "m");

                showLandingTarget = GUILayout.Toggle(showLandingTarget, "Show landing navball guidance");

                if (GUILayout.Button("Autoland")) autopilot.Autoland(this);
                if (autopilot.enabled && autopilot.mode == MechJebModuleSpaceplaneAutopilot.Mode.AUTOLAND
                    && GUILayout.Button("Abort")) autopilot.AutopilotOff();

                GuiUtils.SimpleTextBox("Autoland glideslope:", autopilot.glideslope, "º");
            }

            GUILayout.Label("Hold", s);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Initiate hold:")) autopilot.HoldHeadingAndAltitude(this);
            GUILayout.Label("Heading:");
            autopilot.targetHeading.text = GUILayout.TextField(autopilot.targetHeading.text, GUILayout.Width(40));
            GUILayout.Label("º Altitude:");
            autopilot.targetAltitude.text = GUILayout.TextField(autopilot.targetAltitude.text, GUILayout.Width(40));
            GUILayout.Label("m");
            GUILayout.EndHorizontal();

            if (autopilot.enabled && autopilot.mode == MechJebModuleSpaceplaneAutopilot.Mode.HOLD
                && GUILayout.Button("Abort")) autopilot.AutopilotOff();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(350), GUILayout.Height(200) };
        }

        public override void OnFixedUpdate()
        {
            if (showLandingTarget && autopilot != null)
            {
                if (!(core.target.Target is DirectionTarget && core.target.Name == "ILS Guidance")) showLandingTarget = false;
                else
                {
                    core.target.UpdateDirectionTarget(autopilot.ILSAimDirection());
                }
            }
        }



        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleSpaceplaneAutopilot>();
        }

        public MechJebModuleSpaceplaneGuidance(MechJebCore core) : base(core) { }

        public override string GetName()
        {
            return "Spaceplane Guidance";
        }
    }
}
