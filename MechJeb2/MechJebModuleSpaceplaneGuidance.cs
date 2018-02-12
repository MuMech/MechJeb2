using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleSpaceplaneGuidance : DisplayModule
    {
        MechJebModuleSpaceplaneAutopilot autoland;

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
                autoland.runway = availableRunways[runwayIndex];

                GUILayout.Label("Distance to runway: " + MuUtils.ToSI(Vector3d.Distance(vesselState.CoM, autoland.runway.Start()), 0) + "m");

                showLandingTarget = GUILayout.Toggle(showLandingTarget, "Show landing navball guidance");

                if (GUILayout.Button("Autoland")) autoland.Autoland(this);
                if (autoland.enabled && GUILayout.Button("Abort"))
                    autoland.AutopilotOff();
                
                GuiUtils.SimpleTextBox("Autoland glideslope:", autoland.glideslope);
                GuiUtils.SimpleTextBox("Approach speed:", autoland.approachSpeed);
                GuiUtils.SimpleTextBox("Touchdown speed:", autoland.touchdownSpeed);
                autoland.bEngageReverseIfAvailable = GUILayout.Toggle(autoland.bEngageReverseIfAvailable, "Reverse thrust upon touchdown");
                autoland.bBreakAsSoonAsLanded = GUILayout.Toggle(autoland.bBreakAsSoonAsLanded, "Break As Soon As Landed");

                if (autoland.enabled)
                {
                    GUILayout.Label("State: " + autoland.AutolandApproachStateToHumanReadableDescription());
                    GUILayout.Label(string.Format("Distance to waypoint: {0} m", Math.Round(autoland.GetAutolandLateralDistanceToNextWaypoint(), 0)));
                    GUILayout.Label(string.Format("Target speed: {0} m/s", Math.Round(autoland.Autopilot.SpeedTarget, 1)));
                    GUILayout.Label(string.Format("Target altitude: {0} m", Math.Round(autoland.GetAutolandTargetAltitude(autoland.GetAutolandTargetVector()), 0)));
                    GUILayout.Label(string.Format("Target vertical speed: {0} m/s", Math.Round(autoland.Autopilot.VertSpeedTarget, 1)));
                    GUILayout.Label(string.Format("Target heading: {0}º", Math.Round(autoland.Autopilot.HeadingTarget, 0)));
                }
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(350), GUILayout.Height(200) };
        }

        public override void OnFixedUpdate()
        {
            if (showLandingTarget && autoland != null)
            {
                if (!(core.target.Target is DirectionTarget && core.target.Name == "ILS Guidance")) showLandingTarget = false;
                else
                {
                    core.target.UpdateDirectionTarget(autoland.GetAutolandTargetVector());
                }
            }
        }



        public override void OnStart(PartModule.StartState state)
        {
            autoland = core.GetComputerModule<MechJebModuleSpaceplaneAutopilot>();
        }

        public MechJebModuleSpaceplaneGuidance(MechJebCore core) : base(core) { }

        public override string GetName()
        {
            return "Aircraft Approach & Autoland";
        }
    }
}
