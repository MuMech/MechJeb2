using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

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
                GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label1"), s);//Landing

                runwayIndex = GuiUtils.ComboBox.Box(runwayIndex, availableRunways.Select(p => p.name).ToArray(), this);
                autoland.runway = availableRunways[runwayIndex];

                GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label2") + MuUtils.ToSI(Vector3d.Distance(vesselState.CoM, autoland.runway.Start()), 0) + "m");//Distance to runway: 

                showLandingTarget = GUILayout.Toggle(showLandingTarget, Localizer.Format("#MechJeb_ApproAndLand_label3"));//Show landing navball guidance

                if (GUILayout.Button(Localizer.Format("#MechJeb_ApproAndLan_button1")))//Autoland
                    autoland.Autoland(this);
                if (autoland.enabled && GUILayout.Button(Localizer.Format("#MechJeb_ApproAndLan_button2")))//Abort
                    autoland.AutopilotOff();
                
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_ApproAndLand_label14"), autoland.glideslope,"°");//Autoland glideslope:
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_ApproAndLand_label4"), autoland.approachSpeed, "m/s");//Approach speed:
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_ApproAndLand_label5"), autoland.touchdownSpeed, "m/s");//Touchdown speed:
                autoland.bEngageReverseIfAvailable = GUILayout.Toggle(autoland.bEngageReverseIfAvailable, Localizer.Format("#MechJeb_ApproAndLand_label6"));//Reverse thrust upon touchdown
                autoland.bBreakAsSoonAsLanded = GUILayout.Toggle(autoland.bBreakAsSoonAsLanded, Localizer.Format("#MechJeb_ApproAndLand_label7"));//Brake as soon as landed

                if (autoland.enabled)
                {
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label8") + autoland.AutolandApproachStateToHumanReadableDescription());//State:
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label9", Math.Round(autoland.GetAutolandLateralDistanceToNextWaypoint(), 0)));//Distance to waypoint: {0}m
                    //GUILayout.Label(string.Format("Distance to waypoint: {0} m", Math.Round(autoland.GetAutolandLateralDistanceToNextWaypoint(), 0)));
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label10",Math.Round(autoland.Autopilot.SpeedTarget, 1)));//Target speed: {0} m/s
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label11", Math.Round(autoland.GetAutolandTargetAltitude(autoland.GetAutolandTargetVector()), 0)));//Target altitude: {0} m
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label12", Math.Round(autoland.Autopilot.VertSpeedTarget, 1)));//Target vertical speed: {0} m/s
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label13", Math.Round(autoland.Autopilot.HeadingTarget, 0)));//Target heading: {0}º
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
            return Localizer.Format("#MechJeb_ApproAndLand_title");//Aircraft Approach & Autoland
        }

        public override string IconName()
        {
            return "Aircraft Approach & Autoland";
        }
    }
}
