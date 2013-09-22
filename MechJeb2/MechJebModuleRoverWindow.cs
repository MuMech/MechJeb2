using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

			ed.registry.Find(i => i.id == "Toggle:RoverController.ControlHeading").DrawItem();
			ed.registry.Find(i => i.id == "Editable:RoverController.heading").DrawItem();
			ed.registry.Find(i => i.id == "Value:RoverController.headingErr").DrawItem();
			ed.registry.Find(i => i.id == "Toggle:RoverController.ControlSpeed").DrawItem();
			ed.registry.Find(i => i.id == "Editable:RoverController.speed").DrawItem();
			ed.registry.Find(i => i.id == "Value:RoverController.speedErr").DrawItem();

			GUILayout.BeginVertical();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Target Speed: ", GUILayout.ExpandWidth(true));
			GUILayout.Label(autopilot.tgtSpeed.value.ToString("F1"), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Waypoints: ", GUILayout.ExpandWidth(true));
			GUILayout.Label("Index " + (autopilot.WaypointIndex + 1).ToString() + " of " + autopilot.Waypoints.Count.ToString(), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			
//			GUILayout.Label("Debug1: " + autopilot.debug1.ToString("F3"));
			
			GUILayout.BeginHorizontal();
			if (core.target.Target != null && ((core.target.PositionTargetExists && core.target.targetBody == orbit.referenceBody) || core.target.Orbit.referenceBody == orbit.referenceBody)) {
				if (GUILayout.Button("To Target")) {
					autopilot.Waypoints.Clear();
//					var pos = (core.target.PositionTargetExists ? (Vector3)core.target.GetPositionTargetPosition() : core.target.Position);
//					autopilot.Waypoints.Add(new MechJebRoverWaypoint(pos));
					if (core.target.Target.GetVessel() != null) {
						autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.Target.GetVessel()));
						Debug.Log(autopilot.Waypoints[0].ToString());
					} else {
						autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition()));
					}
					autopilot.WaypointIndex = 0;
					autopilot.ControlHeading = autopilot.ControlSpeed = true;
					vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
					autopilot.loopWaypoints = Input.GetKey(KeyCode.LeftAlt);
				}

				if (GUILayout.Button("Add Target")) {
//					var pos = (core.target.PositionTargetExists ? (Vector3)core.target.GetPositionTargetPosition() : core.target.Position);
//					autopilot.Waypoints.Add(new MechJebRoverWaypoint(pos));
					if (core.target.Target.GetVessel() != null) {
						autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.Target.GetVessel()));
					} else {
						autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition()));
					}
					if (autopilot.WaypointIndex < 0) { autopilot.WaypointIndex = autopilot.Waypoints.Count - 1; }
				}
			}
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Waypoints")) {
				core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled = true;
			}
			GUILayout.EndHorizontal();
			
			GUILayout.EndVertical();
			
			base.WindowGUI(windowID);
		}

		public override void OnUpdate()
		{
			if (autopilot != null)
			{
				if (autopilot.ControlHeading || autopilot.ControlSpeed)
				{
					autopilot.users.Add(this);
				}
				else
				{
					autopilot.users.Remove(this);
				}
			}
			
			if (!enabled) {
				core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled = false;
			}
		}
	}

	public class MechJebModuleRoverWaypointWindow : DisplayModule {
		public MechJebModuleRoverController ap;
		private Vector2 scroll;
		private GUIStyle active = new GUIStyle(GuiUtils.skin.button);
		private GUIStyle inactive = new GUIStyle(GuiUtils.skin.button);
		private int selIndex = -1;

		public MechJebModuleRoverWaypointWindow(MechJebCore core) : base(core) { }

		public override void OnStart(PartModule.StartState state)
		{
			this.hidden = true;
			ap = core.GetComputerModule<MechJebModuleRoverController>();
			active.alignment = inactive.alignment = TextAnchor.UpperLeft;
			active.active.textColor = active.focused.textColor = active.hover.textColor = active.normal.textColor = Color.green;
		}

		public override string GetName()
		{
			return "Rover Waypoints";
		}

		public override GUILayoutOption[] WindowOptions()
		{
			return new GUILayoutOption[] { GUILayout.Width(600), GUILayout.Height(300) };
		}

		protected override void WindowGUI(int windowID)
		{
			if (selIndex > ap.Waypoints.Count) { selIndex = -1; }
			
			scroll = GUILayout.BeginScrollView(scroll);
			
			GUILayout.BeginVertical();
			for (int i = 0; i < ap.Waypoints.Count; i++) {
				var wp = ap.Waypoints[i];
				GUI.backgroundColor = (i == ap.WaypointIndex ? new Color(0.5f, 1f, 0.5f) : Color.white);
				if (GUILayout.Button(string.Format("[{0}] {1} - D: {2:F1} - R: {3:F1}", i + 1, wp.Name, Vector3.Distance(vessel.CoM, wp.Position), wp.Radius), (i == selIndex ? active : inactive))) {
					selIndex = i;
				}
				GUI.backgroundColor = Color.white;
			}
			GUILayout.EndVertical();
			
			GUILayout.EndScrollView();
			
			base.WindowGUI(windowID);
		}
	}
}
