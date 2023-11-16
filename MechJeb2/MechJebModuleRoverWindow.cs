extern alias JetBrainsAnnotations;
using System.Collections;
using JetBrainsAnnotations::JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleRoverWindow : DisplayModule
    {
        public MechJebModuleRoverController autopilot;

        public MechJebModuleRoverWindow(MechJebCore core) : base(core) { }

        public override void OnStart(PartModule.StartState state) => autopilot = Core.GetComputerModule<MechJebModuleRoverController>();

        public override string GetName() => Localizer.Format("#MechJeb_Rover_title"); // "Rover Autopilot"

        public override string IconName() => "Rover Autopilot";

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(200), GUILayout.Height(50) };

        protected override void WindowGUI(int windowID)
        {
            MechJebModuleCustomWindowEditor ed = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            bool alt = GameSettings.MODIFIER_KEY.GetKey();

            if (GUI.Button(new Rect(WindowPos.width - 48, 0, 13, 20), "?", GuiUtils.YellowOnHover))
            {
                MechJebModuleWaypointHelpWindow help = Core.GetComputerModule<MechJebModuleWaypointHelpWindow>();
                help.SelTopic = ((IList)help.Topics).IndexOf("Controller");
                help.Enabled  = help.SelTopic > -1 || help.Enabled;
            }

            ed.registry.Find(i => i.id == "Toggle:RoverController.ControlHeading").DrawItem();
            GUILayout.BeginHorizontal();
            ed.registry.Find(i => i.id == "Editable:RoverController.heading").DrawItem();
            if (GUILayout.Button("-", GUILayout.Width(18))) { autopilot.heading.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1; }

            if (GUILayout.Button("+", GUILayout.Width(18))) { autopilot.heading.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1; }

            GUILayout.EndHorizontal();
            ed.registry.Find(i => i.id == "Value:RoverController.headingErr").DrawItem();
            ed.registry.Find(i => i.id == "Toggle:RoverController.ControlSpeed").DrawItem();
            GUILayout.BeginHorizontal();
            ed.registry.Find(i => i.id == "Editable:RoverController.speed").DrawItem();
            if (GUILayout.Button("-", GUILayout.Width(18))) { autopilot.speed.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1; }

            if (GUILayout.Button("+", GUILayout.Width(18))) { autopilot.speed.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1; }

            GUILayout.EndHorizontal();
            ed.registry.Find(i => i.id == "Value:RoverController.speedErr").DrawItem();
            ed.registry.Find(i => i.id == "Toggle:RoverController.StabilityControl").DrawItem();

            if (!Core.Settings.hideBrakeOnEject)
            {
                ed.registry.Find(i => i.id == "Toggle:RoverController.BrakeOnEject").DrawItem();
            }

            ed.registry.Find(i => i.id == "Toggle:RoverController.BrakeOnEnergyDepletion").DrawItem();
            if (autopilot.BrakeOnEnergyDepletion)
            {
                ed.registry.Find(i => i.id == "Toggle:RoverController.WarpToDaylight").DrawItem();
            }

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_Rover_label1"), GUILayout.ExpandWidth(true)); // "Target Speed"
            GUILayout.Label(autopilot.tgtSpeed.ToString("F1"), GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_Rover_label2"), GUILayout.ExpandWidth(true)); // "Waypoints"
            GUILayout.Label("Index " + (autopilot.WaypointIndex + 1) + " of " + autopilot.Waypoints.Count, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();

//			GUILayout.Label("Debug1: " + autopilot.debug1.ToString("F3"));

            GUILayout.BeginHorizontal();
            if (Core.Target != null && Core.Target.Target != null)
            {
                Vessel vssl = Core.Target.Target.GetVessel();

                if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button1"))) // "To Target"
                {
                    Core.GetComputerModule<MechJebModuleWaypointWindow>().SelIndex = -1;
                    autopilot.WaypointIndex                                        = 0;
                    autopilot.Waypoints.Clear();
                    if (vssl != null) { autopilot.Waypoints.Add(new MechJebWaypoint(vssl, 25f)); }
                    else { autopilot.Waypoints.Add(new MechJebWaypoint(Core.Target.GetPositionTargetPosition())); }

                    autopilot.ControlHeading = autopilot.ControlSpeed = true;
                    Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
                    autopilot.LoopWaypoints = alt;
                }

                if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button2"))) // "Add Target"
                {
                    if (vssl != null) { autopilot.Waypoints.Add(new MechJebWaypoint(vssl, 25f)); }
                    else { autopilot.Waypoints.Add(new MechJebWaypoint(Core.Target.GetPositionTargetPosition())); }
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (autopilot.Waypoints.Count > 0)
            {
                if (!autopilot.ControlHeading || !autopilot.ControlSpeed)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button3")))
                    {
                        // "Drive"
                        autopilot.WaypointIndex  = Mathf.Max(0, alt ? 0 : autopilot.WaypointIndex);
                        autopilot.ControlHeading = autopilot.ControlSpeed = true;
                        // autopilot.LoopWaypoints = alt;
                    }
                }
                else if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button4")))
                {
                    // "Stop"
                    // autopilot.WaypointIndex = -1; // more annoying than helpful
                    autopilot.ControlHeading = autopilot.ControlSpeed = autopilot.LoopWaypoints = false;
                }
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button5")))
            {
                // "Waypoints"
                MechJebModuleWaypointWindow waypoints = Core.GetComputerModule<MechJebModuleWaypointWindow>();
                waypoints.Enabled = !waypoints.Enabled;
                if (waypoints.Enabled)
                {
                    waypoints.Mode = MechJebModuleWaypointWindow.WaypointMode.ROVER;
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override void OnUpdate()
        {
            if (autopilot != null)
            {
                if (autopilot.ControlHeading || autopilot.ControlSpeed || autopilot.StabilityControl || autopilot.BrakeOnEnergyDepletion ||
                    autopilot.BrakeOnEject)
                {
                    autopilot.Users.Add(this);
                }
                else
                {
                    autopilot.Users.Remove(this);
                }
            }
        }

        protected override void OnModuleDisabled()
        {
            Core.GetComputerModule<MechJebModuleWaypointWindow>().Enabled     = false;
            Core.GetComputerModule<MechJebModuleWaypointHelpWindow>().Enabled = false;
            base.OnModuleDisabled();
        }
    }
}
