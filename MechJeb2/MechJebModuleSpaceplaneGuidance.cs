using System;
using System.Linq;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleSpaceplaneGuidance : DisplayModule
    {
        private MechJebModuleSpaceplaneAutopilot autoland;

        protected bool _showLandingTarget;

        [Persistent(pass = (int)(Pass.GLOBAL | Pass.LOCAL))]
        public int runwayIndex;

        public bool showLandingTarget
        {
            get => _showLandingTarget;
            set
            {
                if (value && !_showLandingTarget) Core.Target.SetDirectionTarget("ILS Guidance");
                if (!value && Core.Target.Target is DirectionTarget && Core.Target.Name == "ILS Guidance") Core.Target.Unset();
                _showLandingTarget = value;
            }
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            var availableRunways = MechJebModuleSpaceplaneAutopilot.runways.Where(p => p.body == MainBody).ToList();
            if (runwayIndex > availableRunways.Count)
                runwayIndex = 0;

            if (availableRunways.Any())
            {
                GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label1"), GuiUtils.middleCenterLabel); //Landing

                runwayIndex     = GuiUtils.ComboBox.Box(runwayIndex, availableRunways.Select(p => p.name).ToArray(), this);
                autoland.runway = availableRunways[runwayIndex];

                GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label2") +
                                Vector3d.Distance(VesselState.CoM, autoland.runway.Start()).ToSI() + "m"); //Distance to runway:

                showLandingTarget =
                    GUILayout.Toggle(showLandingTarget, Localizer.Format("#MechJeb_ApproAndLand_label3")); //Show landing navball guidance

                if (GUILayout.Button(Localizer.Format("#MechJeb_ApproAndLan_button1"))) //Autoland
                    autoland.Autoland(this);
                if (autoland.Enabled && GUILayout.Button(Localizer.Format("#MechJeb_ApproAndLan_button2"))) //Abort
                    autoland.AutopilotOff();

                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_ApproAndLand_label14"), autoland.glideslope, "°");      //Autoland glideslope:
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_ApproAndLand_label4"), autoland.approachSpeed, "m/s");  //Approach speed:
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_ApproAndLand_label5"), autoland.touchdownSpeed, "m/s"); //Touchdown speed:

                autoland.bEngageReverseIfAvailable =
                    GUILayout.Toggle(autoland.bEngageReverseIfAvailable,
                        Localizer.Format("#MechJeb_ApproAndLand_label6")); //Reverse thrust upon touchdown
                autoland.bBreakAsSoonAsLanded =
                    GUILayout.Toggle(autoland.bBreakAsSoonAsLanded, Localizer.Format("#MechJeb_ApproAndLand_label7")); //Brake as soon as landed

                if (autoland.Enabled)
                {
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label8") +
                                    autoland.AutolandApproachStateToHumanReadableDescription()); //State:
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label9",
                        Math.Round(autoland.GetAutolandLateralDistanceToNextWaypoint(), 0))); //Distance to waypoint: {0}m
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label10",
                        Math.Round(autoland.Autopilot.SpeedTarget, 1))); //Target speed: {0} m/s
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label11",
                        Math.Round(autoland.GetAutolandTargetAltitude(autoland.GetAutolandTargetVector()), 0))); //Target altitude: {0} m
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label12",
                        Math.Round(autoland.Autopilot.VertSpeedTarget, 1))); //Target vertical speed: {0} m/s
                    GUILayout.Label(Localizer.Format("#MechJeb_ApproAndLand_label13",
                        Math.Round(autoland.Autopilot.HeadingTarget, 0))); //Target heading: {0}º
                }
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(350), GUILayout.Height(200) };

        public override void OnFixedUpdate()
        {
            if (showLandingTarget && autoland != null)
            {
                if (!(Core.Target.Target is DirectionTarget && Core.Target.Name == "ILS Guidance")) showLandingTarget = false;
                else
                {
                    Core.Target.UpdateDirectionTarget(autoland.GetAutolandTargetVector());
                }
            }
        }

        public override void OnStart(PartModule.StartState state) => autoland = Core.GetComputerModule<MechJebModuleSpaceplaneAutopilot>();

        public MechJebModuleSpaceplaneGuidance(MechJebCore core) : base(core) { }

        public override string GetName() => Localizer.Format("#MechJeb_ApproAndLand_title"); //Aircraft Approach & Autoland

        public override string IconName() => "Aircraft Approach & Autoland";
    }
}
