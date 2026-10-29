extern alias JetBrainsAnnotations;
using System.Collections;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRoverWindow : DisplayModule
    {
        public MechJebModuleRoverController autopilot;

        public MechJebModuleRoverWindow(MechJebCore core) : base(core) { }

        public override void OnStart(PartModule.StartState state) => autopilot = Core.GetComputerModule<MechJebModuleRoverController>();

        public override string GetName() => Localizer.Format("#MechJeb_Rover_title"); // "Rover Autopilot"

        public override string IconName() => "Rover Autopilot";

        protected override GUILayoutOption[] WindowOptions() => new[] { GuiUtils.LayoutWidth(200), GUILayout.Height(50) };

        protected override void WindowGUI(int windowID)
        {
            MechJebModuleCustomWindowEditor ed = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            bool alt = GameSettings.MODIFIER_KEY.GetKey();

            if (GUI.Button(new Rect(WindowPos.width - 48, 0, 13, 20), "?", GuiUtils.YellowOnHover))
            {
                OpenHelp();
            }

            ed.registry.Find(i => i.id == "Toggle:RoverController.ControlHeading").DrawItem();
            GUILayout.BeginHorizontal();
            ed.registry.Find(i => i.id == "Editable:RoverController.heading").DrawItem();
            if (GUILayout.Button("-", GuiUtils.LayoutWidth(18))) { autopilot.heading.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1; }

            if (GUILayout.Button("+", GuiUtils.LayoutWidth(18))) { autopilot.heading.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1; }

            GUILayout.EndHorizontal();
            ed.registry.Find(i => i.id == "Value:RoverController.headingErr").DrawItem();
            ed.registry.Find(i => i.id == "Toggle:RoverController.ControlSpeed").DrawItem();
            GUILayout.BeginHorizontal();
            ed.registry.Find(i => i.id == "Editable:RoverController.speed").DrawItem();
            if (GUILayout.Button("-", GuiUtils.LayoutWidth(18))) { autopilot.speed.Val -= GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1; }

            if (GUILayout.Button("+", GuiUtils.LayoutWidth(18))) { autopilot.speed.Val += GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1; }

            GUILayout.EndHorizontal();
            ed.registry.Find(i => i.id == "Value:RoverController.speedErr").DrawItem();
            ed.registry.Find(i => i.id == "Toggle:RoverController.StabilityControl").DrawItem();

            if (!Core.Settings.HideBrakeOnEject)
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
            GUILayout.Label(Localizer.Format("#MechJeb_Rover_label1"), GuiUtils.LayoutExpandWidth); // "Target Speed"
            GUILayout.Label(autopilot.tgtSpeed.ToString("F1"), GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(Localizer.Format("#MechJeb_Rover_label2"), GuiUtils.LayoutExpandWidth); // "Waypoints"
            GUILayout.Label("Index " + (autopilot.WaypointIndex + 1) + " of " + autopilot.Waypoints.Count, GuiUtils.LayoutNoExpandWidth);
            GUILayout.EndHorizontal();

            //			GUILayout.Label("Debug1: " + autopilot.debug1.ToString("F3"));

            GUILayout.BeginHorizontal();
            if (Core.Target != null && Core.Target.Target != null)
            {
                if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button1"))) // "To Target"
                {
                    ToTarget(alt);
                }

                if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button2"))) // "Add Target"
                {
                    AddTarget();
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
                        Drive(alt); // "Drive"
                    }
                }
                else if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button4")))
                {
                    Stop(); // "Stop"
                }
            }

            if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button5")))
            {
                ToggleWaypointWindow(); // "Waypoints"
            }

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        // ---- actions shared by the IMGUI window above and the uGUI view (MechJeb2.UI) ----

        /// <summary>The "?" title button.</summary>
        internal void OpenHelp()
        {
            MechJebModuleWaypointHelpWindow help = Core.GetComputerModule<MechJebModuleWaypointHelpWindow>();
            help.SelTopic = ((IList)help.Topics).IndexOf("Rover Controller"); // the topic's name (was "Controller": -1, and the help window threw)
            help.Enabled = help.SelTopic > -1 || help.Enabled;
        }

        /// <summary>"To Target": drive to the target, alone; Alt keeps following it.</summary>
        internal void ToTarget(bool alt)
        {
            if (Core.Target == null || Core.Target.Target == null) return;
            Vessel vssl = Core.Target.Target.GetVessel();
            Core.GetComputerModule<MechJebModuleWaypointWindow>().SelIndex = -1;
            autopilot.WaypointIndex = 0;
            autopilot.Waypoints.Clear();
            if (vssl != null) { autopilot.Waypoints.Add(new MechJebWaypoint(vssl, 25f)); }
            else { autopilot.Waypoints.Add(new MechJebWaypoint(Core.Target.GetPositionTargetPosition())); }

            autopilot.ControlHeading = autopilot.ControlSpeed = true;
            Vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
            autopilot.LoopWaypoints = alt;
        }

        /// <summary>"Add Target": append the target to the route.</summary>
        internal void AddTarget()
        {
            if (Core.Target == null || Core.Target.Target == null) return;
            Vessel vssl = Core.Target.Target.GetVessel();
            if (vssl != null) { autopilot.Waypoints.Add(new MechJebWaypoint(vssl, 25f)); }
            else { autopilot.Waypoints.Add(new MechJebWaypoint(Core.Target.GetPositionTargetPosition())); }
        }

        /// <summary>"Drive"; Alt starts from the first waypoint.</summary>
        internal void Drive(bool alt)
        {
            autopilot.WaypointIndex = Mathf.Max(0, alt ? 0 : autopilot.WaypointIndex);
            autopilot.ControlHeading = autopilot.ControlSpeed = true;
            // autopilot.LoopWaypoints = alt;
        }

        /// <summary>"Stop"</summary>
        internal void Stop() =>
            // autopilot.WaypointIndex = -1; // more annoying than helpful
            autopilot.ControlHeading = autopilot.ControlSpeed = autopilot.LoopWaypoints = false;

        /// <summary>"Waypoints": show or hide the waypoint window (in rover mode).</summary>
        internal void ToggleWaypointWindow()
        {
            MechJebModuleWaypointWindow waypoints = Core.GetComputerModule<MechJebModuleWaypointWindow>();
            waypoints.Enabled = !waypoints.Enabled;
            if (waypoints.Enabled)
            {
                waypoints.Mode = MechJebModuleWaypointWindow.WaypointMode.ROVER;
            }
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
            Core.GetComputerModule<MechJebModuleWaypointWindow>().Enabled = false;
            Core.GetComputerModule<MechJebModuleWaypointHelpWindow>().Enabled = false;
            base.OnModuleDisabled();
        }
    }
}
