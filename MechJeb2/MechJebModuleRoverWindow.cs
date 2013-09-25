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
				}

				if (GUILayout.Button("Add Target")) {
					if (vssl != null) {	autopilot.Waypoints.Add(new MechJebRoverWaypoint(vssl)); }
					else { autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition())); }
//					if (autopilot.WaypointIndex < 0) { autopilot.WaypointIndex = autopilot.Waypoints.Count - 1; }
				}
			}
			GUILayout.EndHorizontal();
						
			GUILayout.BeginHorizontal();
			if (autopilot.WaypointIndex == -1) {
				if (autopilot.Waypoints.Count > 0) {
					if (GUILayout.Button("Follow")) {
						autopilot.WaypointIndex = 0;
						autopilot.ControlHeading = autopilot.ControlSpeed = true;
						autopilot.loopWaypoints = Input.GetKey(KeyCode.LeftAlt);
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
		internal int selIndex = -1;
		internal string tmpRadius = "";
		internal string tmpMinSpeed = "";
		internal string tmpMaxSpeed = "";
		private Vector2 scroll;
		private GUIStyle active;
		private GUIStyle inactive;
		private string titleAdd = "";
		private bool waitingForPick = false;

		public MechJebModuleRoverWaypointWindow(MechJebCore core) : base(core) { }

		public override void OnStart(PartModule.StartState state)
		{
			this.hidden = true;
			ap = core.GetComputerModule<MechJebModuleRoverController>();
			active = new GUIStyle(GuiUtils.skin.button);
			inactive = new GUIStyle(GuiUtils.skin.button);
			active.alignment = inactive.alignment = TextAnchor.UpperLeft;
			active.active.textColor = active.focused.textColor = active.hover.textColor = active.normal.textColor = Color.green;
            if (state != PartModule.StartState.None && state != PartModule.StartState.Editor)
            {
            	RenderingManager.AddToPostDrawQueue(1, DrawWaypoints);
            }
		}

		public override string GetName()
		{
			return "Rover Waypoints" + (ap.Waypoints.Count > 0 && titleAdd != "" ? " - " + titleAdd : "");
		}

		public override GUILayoutOption[] WindowOptions()
		{
			return new GUILayoutOption[] { GUILayout.Width(500), GUILayout.Height(400) };
		}

		protected override void WindowGUI(int windowID)
		{
			if (selIndex >= ap.Waypoints.Count) { selIndex = -1; }
			
			scroll = GUILayout.BeginScrollView(scroll);//, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });
			if (ap.Waypoints.Count > 0) {
				GUILayout.BeginVertical();
				double eta = 0;
				double dist = 0;
				for (int i = 0; i < ap.Waypoints.Count; i++) {
					var wp = ap.Waypoints[i];
					if (ap.WaypointIndex > -1 && i >= ap.WaypointIndex) {
						eta += GuiUtils.FromToETA((i == ap.WaypointIndex ? vessel.CoM : (Vector3)ap.Waypoints[i - 1].Position), (Vector3)wp.Position, (i == ap.WaypointIndex ? (float)ap.etaSpeed : wp.MaxSpeed));
						dist += Vector3.Distance((i == ap.WaypointIndex ? vessel.CoM : (Vector3)ap.Waypoints[i - 1].Position), (Vector3)wp.Position);
					}
					string str = string.Format("[{0}] - {1} - R: {2:F1} m\n       S: {3:F0} ~ {4:F0} - D: {5}m - ETA: {6}", i + 1, wp.GetNameWithCoords(), wp.Radius,
					                           wp.MinSpeed, (wp.MaxSpeed > 0 ? wp.MaxSpeed : ap.speed.val), MuUtils.ToSI(dist, -1), GuiUtils.TimeToDHMS(eta));
					GUI.backgroundColor = (i == ap.WaypointIndex ? new Color(0.5f, 1f, 0.5f) : Color.white);
					if (GUILayout.Button(str, (i == selIndex ? active : inactive))) {
						if (selIndex == i) {
							selIndex = -1;
						}
						else {
							if (Input.GetKey(KeyCode.LeftAlt)) {
								ap.WaypointIndex = i;
							}
							else {
								selIndex = i;
								tmpRadius = wp.Radius.ToString();
								tmpMinSpeed = wp.MinSpeed.ToString();
								tmpMaxSpeed = wp.MaxSpeed.ToString();
							}
						}
					}
					GUI.backgroundColor = Color.white;
					if (selIndex > -1 && selIndex == i) {
						GUILayout.BeginHorizontal();
						GUILayout.Label("  Edit - Radius: ", GUILayout.ExpandWidth(false));
						tmpRadius = GUILayout.TextField(tmpRadius, GUILayout.Width(55));
						float.TryParse(tmpRadius, out ap.Waypoints[selIndex].Radius);
						GUILayout.Label(" - Speed: ", GUILayout.ExpandWidth(false));
						tmpMinSpeed = GUILayout.TextField(tmpMinSpeed, GUILayout.Width(45));
						float.TryParse(tmpMinSpeed, out ap.Waypoints[selIndex].MinSpeed);
						tmpMaxSpeed = GUILayout.TextField(tmpMaxSpeed, GUILayout.Width(45));
						float.TryParse(tmpMaxSpeed, out ap.Waypoints[selIndex].MaxSpeed);
						GUILayout.EndHorizontal();
					}
				}
				titleAdd = "Distance: " + MuUtils.ToSI(dist, -1) + "m - ETA: " + GuiUtils.TimeToDHMS(eta);
				GUILayout.EndVertical();
			}
			GUILayout.EndScrollView();
			
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Add from Map")) {
				core.target.Unset();
				core.target.PickPositionTargetOnMap();
				waitingForPick = true;
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
				if (ap.WaypointIndex >= ap.Waypoints.Count) { ap.WaypointIndex = ap.Waypoints.Count - 1; }
			}
			if (GUILayout.Button("Move Up", GUILayout.Width(80))) {
				if (selIndex > 0) {
					ap.Waypoints.Swap(selIndex, selIndex - 1);
					if (ap.WaypointIndex == selIndex) {
						ap.WaypointIndex--;
					}
					else if (ap.WaypointIndex == selIndex - 1) {
						ap.WaypointIndex++;
					}
					selIndex--;
				}
			}
			if (GUILayout.Button("Move Down", GUILayout.Width(80))) {
				if (selIndex > -1 && selIndex <= ap.Waypoints.Count - 1) {
					ap.Waypoints.Swap(selIndex, selIndex + 1);
					if (ap.WaypointIndex == selIndex) {
						ap.WaypointIndex++;
					}
					else if (ap.WaypointIndex == selIndex + 1) {
						ap.WaypointIndex--;
					}
					selIndex++;
				}
			}
//			if (GUILayout.Button("Settings", GUILayout.ExpandWidth(false))) {
//			}
			GUILayout.EndHorizontal();
			
			base.WindowGUI(windowID);
			
			if (waitingForPick) {
				if (core.target.pickingPositionTarget == false) {
					if (core.target.PositionTargetExists) {
						ap.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition()));
						core.target.Unset();
					}
					waitingForPick = false;
				}
			}
		}
		
		public void DrawWaypoints() {
			if (MapView.MapIsEnabled && this.enabled && ap.Waypoints.Count > 0) {
//				for (int i = 0; i < ap.Waypoints.Count; i++) {
//					var wp = ap.Waypoints[i];
//					var col = (i < ap.WaypointIndex ? Color.blue : (i == ap.WaypointIndex ? Color.green : Color.yellow));
//				}
			}
		}

		public override void OnFixedUpdate()
		{
			if (!core.GetComputerModule<MechJebModuleRoverWindow>().enabled) { enabled = false; }
		}
	}
}
