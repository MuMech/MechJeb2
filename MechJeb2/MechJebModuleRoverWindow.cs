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
			GUILayout.Label("Target Speed", GUILayout.ExpandWidth(true));
			GUILayout.Label(autopilot.tgtSpeed.value.ToString("F1"), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Waypoints", GUILayout.ExpandWidth(true));
			GUILayout.Label("Index " + (autopilot.WaypointIndex + 1).ToString() + " of " + autopilot.Waypoints.Count.ToString(), GUILayout.ExpandWidth(false));
			GUILayout.EndHorizontal();
			
//			GUILayout.Label("Debug1: " + autopilot.debug1.ToString("F3"));
			
			GUILayout.BeginHorizontal();
			if (core.target != null && core.target.Target != null) {// && (core.target.targetBody == orbit.referenceBody || (core.target.Orbit != null ? core.target.Orbit.referenceBody == orbit.referenceBody : false))) {
				var vssl = core.target.Target.GetVessel();
				
				if (GUILayout.Button("To Target")) {
					autopilot.Waypoints.Clear();
					if (vssl != null) {	autopilot.Waypoints.Add(new MechJebRoverWaypoint(vssl)); }
					else { autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition())); }
					autopilot.WaypointIndex = 0;
					autopilot.ControlHeading = autopilot.ControlSpeed = true;
					vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
					autopilot.loopWaypoints = Input.GetKey(KeyCode.LeftAlt);
					core.GetComputerModule<MechJebModuleRoverWaypointWindow>().selIndex = -1;
					core.GetComputerModule<MechJebModuleRoverWaypointWindow>().tmpRadius = "";
				}

				if (GUILayout.Button("Add Target")) {
					if (vssl != null) {	autopilot.Waypoints.Add(new MechJebRoverWaypoint(vssl)); }
					else { autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition())); }
					if (autopilot.WaypointIndex < 0) { autopilot.WaypointIndex = autopilot.Waypoints.Count - 1; }
				}
			}
			GUILayout.EndHorizontal();
			
//			switch (GUILayout.SelectionGrid(-1, new string[] { (autopilot.WaypointIndex == -1 ? (autopilot.Waypoints.Count > 0 ? "Follow" : "no Waypoints") : "Stop"), "Waypoints" }, 2)) {
//				case 0:
//					if (autopilot.WaypointIndex == -1) {
//						if (autopilot.Waypoints.Count > 0) {
//							autopilot.WaypointIndex = 0;
//						}
//						else {
//						}
//					}
//					else {
//						autopilot.WaypointIndex = -1;
//						autopilot.loopWaypoints = false;
//					}
//					break;
//					
//				case 1:
//					core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled = !core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled;
//					break;
//			}
			
			GUILayout.BeginHorizontal();
			if (autopilot.WaypointIndex == -1) {
				if (autopilot.Waypoints.Count > 0) {
					if (GUILayout.Button("Follow")) {
						autopilot.WaypointIndex = 0;
					}
				}
				else {
//					if (GUILayout.Button("No Waypoints")) {
//					}
				}
			}
			else {
				if (GUILayout.Button("Stop")) {
					autopilot.WaypointIndex = -1;
					autopilot.loopWaypoints = false;
				}
			}
			if (GUILayout.Button("Waypoints")) {
				core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled = !core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled;
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
		}
	}

	public class MechJebModuleRoverWaypointWindow : DisplayModule {
		public MechJebModuleRoverController ap;
		private Vector2 scroll;
		private GUIStyle active;
		private GUIStyle inactive;
		internal int selIndex = -1;
		internal string tmpRadius = "";
		internal string tmpMinSpeed = "";
		internal string tmpMaxSpeed = "";

		public MechJebModuleRoverWaypointWindow(MechJebCore core) : base(core) { }

		public override void OnStart(PartModule.StartState state)
		{
			this.hidden = true;
			ap = core.GetComputerModule<MechJebModuleRoverController>();
			active = new GUIStyle(GuiUtils.skin.button);
			inactive = new GUIStyle(GuiUtils.skin.button);
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
			if (selIndex >= ap.Waypoints.Count) { selIndex = -1; }
			
			scroll = GUILayout.BeginScrollView(scroll);//, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });
			if (ap.Waypoints.Count > 0) {
				GUILayout.BeginVertical();
				double eta = 0;
				for (int i = 0; i < ap.Waypoints.Count; i++) {
					var wp = ap.Waypoints[i];
					if (i >= ap.WaypointIndex && ap.WaypointIndex > -1) {
						eta += GuiUtils.FromToETA((i == ap.WaypointIndex ? (Vector3d)vessel.CoM : ap.Waypoints[i - 1].Position), wp.Position, (i == ap.WaypointIndex ? (float)ap.etaSpeed : wp.MaxSpeed));
					}
					string str = string.Format("[{0}] - {1} - R: {2:F1} m\n       S: {3:F0} ~ {4:F0} - D: {5}m - ETA: {6}", i + 1, wp.GetNameWithCoords(), wp.Radius,
					                           wp.MinSpeed, (wp.MaxSpeed > 0 ? wp.MaxSpeed : ap.speed.val), MuUtils.ToSI(Vector3.Distance(vessel.CoM, wp.Position), -1), GuiUtils.TimeToDHMS(eta));
					GUI.backgroundColor = (i == ap.WaypointIndex ? new Color(0.5f, 1f, 0.5f) : Color.white);
					if (GUILayout.Button(str, (i == selIndex ? active : inactive))) {
						if (selIndex == i) {
							selIndex = -1;
						}
						else {
							selIndex = i;
							tmpRadius = wp.Radius.ToString();
							tmpMinSpeed = wp.MinSpeed.ToString();
							tmpMaxSpeed = wp.MaxSpeed.ToString();
						}
					}
					GUI.backgroundColor = Color.white;
					if (selIndex > -1 && selIndex == i) {
						GUILayout.BeginHorizontal();
						GUILayout.Label("R: ", GUILayout.ExpandWidth(false));
						tmpRadius = GUILayout.TextField(tmpRadius, GUILayout.Width(55));
						float.TryParse(tmpRadius, out ap.Waypoints[selIndex].Radius);
						GUILayout.Label("S: ", GUILayout.ExpandWidth(false));
						tmpMinSpeed = GUILayout.TextField(tmpMinSpeed, GUILayout.Width(45));
						float.TryParse(tmpMinSpeed, out ap.Waypoints[selIndex].MinSpeed);
//						if (ap.Waypoints[selIndex].MinSpeed > ap.Waypoints[selIndex].MaxSpeed) { ap.Waypoints[selIndex].MinSpeed = ap.Waypoints[selIndex].MaxSpeed; }
						tmpMaxSpeed = GUILayout.TextField(tmpMaxSpeed, GUILayout.Width(45));
						float.TryParse(tmpMaxSpeed, out ap.Waypoints[selIndex].MaxSpeed);
//						if (ap.Waypoints[selIndex].MaxSpeed < ap.Waypoints[selIndex].MinSpeed) { ap.Waypoints[selIndex].MaxSpeed = ap.Waypoints[selIndex].MinSpeed; }
						GUILayout.EndHorizontal();
					}
				}
				GUILayout.EndVertical();
			}
			GUILayout.EndScrollView();
			
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add from Map")) {
			}
			if (GUILayout.Button("Remove")) {
				if (Input.GetKey(KeyCode.LeftAlt)) {
					ap.WaypointIndex = -1;
					ap.Waypoints.Clear();
				}
				else {
					ap.Waypoints.RemoveAt(selIndex);
				}
				selIndex = -1;
				tmpRadius = "";
				if (ap.WaypointIndex >= ap.Waypoints.Count) { ap.WaypointIndex = ap.Waypoints.Count - 1; }
			}
			if (GUILayout.Button("Move Up", GUILayout.Width(80))) {
			}
			if (GUILayout.Button("Move Down", GUILayout.Width(80))) {
			}
			if (GUILayout.Button("Settings", GUILayout.ExpandWidth(false))) {
			}
			GUILayout.EndHorizontal();
			
			base.WindowGUI(windowID);
		}
		
		public override void OnFixedUpdate()
		{
			if (!core.GetComputerModule<MechJebModuleRoverWindow>().enabled) { enabled = false; }
		}
	}
}
