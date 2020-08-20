using System.Collections;
using UnityEngine;
using KSP.Localization;
namespace MuMech
{
	public class MechJebModuleRoverWindow : DisplayModule
	{
		public MechJebModuleRoverController autopilot;

		public MechJebModuleRoverWindow(MechJebCore core) : base(core) { }

		public override void OnStart(PartModule.StartState state)
		{
			autopilot = core.GetComputerModule<MechJebModuleRoverController>();
		}

		public override string GetName()
		{
			return Localizer.Format("#MechJeb_Rover_title"); // "Rover Autopilot"
		}

		public override string IconName()
		{
			return "Rover Autopilot";
		}

		public override GUILayoutOption[] WindowOptions()
		{
			return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(50) };
		}

		protected override void WindowGUI(int windowID)
		{
			MechJebModuleCustomWindowEditor ed = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
			bool alt = GameSettings.MODIFIER_KEY.GetKey();

			if (GUI.Button(new Rect(windowPos.width - 48, 0, 13, 20), "?", GuiUtils.yellowOnHover))
			{
				var help = core.GetComputerModule<MechJebModuleWaypointHelpWindow>();
				help.selTopic = ((IList)help.topics).IndexOf("Controller");
				help.enabled = help.selTopic > -1 || help.enabled;
			}

			ed.registry.Find(i => i.id == "Toggle:RoverController.ControlHeading").DrawItem();
			GUILayout.BeginHorizontal();
			ed.registry.Find(i => i.id == "Editable:RoverController.heading").DrawItem();
			if (GUILayout.Button("-", GUILayout.Width(18))) { autopilot.heading.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); }
			if (GUILayout.Button("+", GUILayout.Width(18))) { autopilot.heading.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); }
			GUILayout.EndHorizontal();
			ed.registry.Find(i => i.id == "Value:RoverController.headingErr").DrawItem();
			ed.registry.Find(i => i.id == "Toggle:RoverController.ControlSpeed").DrawItem();
			GUILayout.BeginHorizontal();
			ed.registry.Find(i => i.id == "Editable:RoverController.speed").DrawItem();
			if (GUILayout.Button("-", GUILayout.Width(18))) { autopilot.speed.val -= (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); }
			if (GUILayout.Button("+", GUILayout.Width(18))) { autopilot.speed.val += (GameSettings.MODIFIER_KEY.GetKey() ? 5 : 1); }
			GUILayout.EndHorizontal();
			ed.registry.Find(i => i.id == "Value:RoverController.speedErr").DrawItem();
			ed.registry.Find(i => i.id == "Toggle:RoverController.StabilityControl").DrawItem();

			if (!core.settings.hideBrakeOnEject)
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
			GUILayout.Label("Index " + (autopilot.WaypointIndex + 1).ToString() + " of " + autopilot.Waypoints.Count.ToString(), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

//			GUILayout.Label("Debug1: " + autopilot.debug1.ToString("F3"));

			GUILayout.BeginHorizontal();
			if (core.target != null && core.target.Target != null)
			{
				var vssl = core.target.Target.GetVessel();

				if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button1"))) // "To Target"
				{
					core.GetComputerModule<MechJebModuleWaypointWindow>().selIndex = -1;
					autopilot.WaypointIndex = 0;
					autopilot.Waypoints.Clear();
					if (vssl != null) { autopilot.Waypoints.Add(new MechJebWaypoint(vssl, 25f)); }
					else { autopilot.Waypoints.Add(new MechJebWaypoint(core.target.GetPositionTargetPosition())); }
					autopilot.ControlHeading = autopilot.ControlSpeed = true;
					vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
					autopilot.LoopWaypoints = alt;
				}

				if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button2"))) // "Add Target"
				{
					if (vssl != null) { autopilot.Waypoints.Add(new MechJebWaypoint(vssl, 25f)); }
					else { autopilot.Waypoints.Add(new MechJebWaypoint(core.target.GetPositionTargetPosition())); }
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			if (autopilot.Waypoints.Count > 0) {
				if (!autopilot.ControlHeading || !autopilot.ControlSpeed) {
					if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button3"))) { // "Drive"
						autopilot.WaypointIndex = Mathf.Max(0, (alt ? 0 : autopilot.WaypointIndex));
						autopilot.ControlHeading = autopilot.ControlSpeed = true;
						// autopilot.LoopWaypoints = alt;
					}
				}
				else if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button4"))) { // "Stop"
					// autopilot.WaypointIndex = -1; // more annoying than helpful
					autopilot.ControlHeading = autopilot.ControlSpeed = autopilot.LoopWaypoints = false;
				}
			}
			if (GUILayout.Button(Localizer.Format("#MechJeb_Rover_button5"))) { // "Waypoints"
				var waypoints = core.GetComputerModule<MechJebModuleWaypointWindow>();
				waypoints.enabled = !waypoints.enabled;
				if (waypoints.enabled) {
					waypoints.Mode = MechJebModuleWaypointWindow.WaypointMode.Rover;
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
				if (autopilot.ControlHeading || autopilot.ControlSpeed || autopilot.StabilityControl || autopilot.BrakeOnEnergyDepletion || autopilot.BrakeOnEject)
				{
					autopilot.users.Add(this);
				}
				else
				{
					autopilot.users.Remove(this);
				}
			}
		}

		public override void OnModuleDisabled()
		{
			core.GetComputerModule<MechJebModuleWaypointWindow>().enabled = false;
			core.GetComputerModule<MechJebModuleWaypointHelpWindow>().enabled = false;
			base.OnModuleDisabled();
		}
	}
}
