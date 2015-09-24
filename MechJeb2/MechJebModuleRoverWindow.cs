using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

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
			return "Rover Autopilot";
		}

		public override GUILayoutOption[] WindowOptions()
		{
			return new GUILayoutOption[] { GUILayout.Width(200), GUILayout.Height(50) };
		}

		protected override void WindowGUI(int windowID)
		{
			MechJebModuleCustomWindowEditor ed = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
			bool alt = Input.GetKey(KeyCode.LeftAlt);

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
			GUILayout.Label("Target Speed", GUILayout.ExpandWidth(true));
			GUILayout.Label(autopilot.tgtSpeed.ToString("F1"), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Waypoints", GUILayout.ExpandWidth(true));
			GUILayout.Label("Index " + (autopilot.WaypointIndex + 1).ToString() + " of " + autopilot.Waypoints.Count.ToString(), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			
//			GUILayout.Label("Debug1: " + autopilot.debug1.ToString("F3"));
			
			GUILayout.BeginHorizontal();
			if (core.target != null && core.target.Target != null) // && (core.target.targetBody == orbit.referenceBody || (core.target.Orbit != null ? core.target.Orbit.referenceBody == orbit.referenceBody : false))) {
			{
				var vssl = core.target.Target.GetVessel();
				
				if (GUILayout.Button("To Target"))
				{
					core.GetComputerModule<MechJebModuleWaypointWindow>().selIndex = -1;
					autopilot.WaypointIndex = 0;
					autopilot.Waypoints.Clear();
					if (vssl != null) {	autopilot.Waypoints.Add(new MechJebWaypoint(vssl, 25f)); }
					else { autopilot.Waypoints.Add(new MechJebWaypoint(core.target.GetPositionTargetPosition())); }
					autopilot.ControlHeading = autopilot.ControlSpeed = true;
					vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
					autopilot.LoopWaypoints = alt;
				}

				if (GUILayout.Button("Add Target"))
				{
					if (vssl != null) {	autopilot.Waypoints.Add(new MechJebWaypoint(vssl, 25f)); }
					else { autopilot.Waypoints.Add(new MechJebWaypoint(core.target.GetPositionTargetPosition())); }
//					if (autopilot.WaypointIndex < 0) { autopilot.WaypointIndex = autopilot.Waypoints.Count - 1; }
				}
			}
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			if (autopilot.Waypoints.Count > 0) {
				if (!autopilot.ControlHeading || !autopilot.ControlSpeed) {
					if (GUILayout.Button("Follow")) {
						autopilot.WaypointIndex = 0;
						autopilot.ControlHeading = autopilot.ControlSpeed = true;
						autopilot.LoopWaypoints = alt;
					}
				}
				else if (GUILayout.Button("Stop")) {
					// autopilot.WaypointIndex = -1; // more annoying than helpful
					autopilot.ControlHeading = autopilot.ControlSpeed = autopilot.LoopWaypoints = false;
				}
			}
			if (GUILayout.Button("Waypoints")) {
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
