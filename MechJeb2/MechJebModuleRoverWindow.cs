<<<<<<< HEAD
﻿using System;
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
            ed.registry.Find(i => i.id == "Toggle:RoverController.BrakeOnEject").DrawItem();

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
					autopilot.LoopWaypoints = Input.GetKey(KeyCode.LeftAlt);
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
						autopilot.LoopWaypoints = Input.GetKey(KeyCode.LeftAlt);
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
					autopilot.LoopWaypoints = false;
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
		
		public override void OnModuleDisabled()
		{
			core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled = false;
			base.OnModuleDisabled();
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
		private MechJebRoverPathRenderer renderer;

		public MechJebModuleRoverWaypointWindow(MechJebCore core) : base(core) { }
		
		public override void OnStart(PartModule.StartState state)
		{
			hidden = true;
			ap = core.GetComputerModule<MechJebModuleRoverController>();
			if (vessel.isActiveVessel) {
				renderer = MechJebRoverPathRenderer.AttachToMapView(core);
				renderer.enabled = enabled; 
			}
		}
		
		public override void OnModuleEnabled()
		{
			if (renderer != null) { renderer.enabled = true; }
			base.OnModuleEnabled();
		}
		
		public override void OnModuleDisabled()
		{
			if (renderer != null) { renderer.enabled = false; }
			base.OnModuleDisabled();
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
			if (inactive == null) {
				inactive = new GUIStyle(GuiUtils.skin != null ? GuiUtils.skin.button : GuiUtils.defaultSkin.button);
				inactive.alignment = TextAnchor.UpperLeft;
			}
			if (active == null) {
				//active = new GUIStyle(GuiUtils.skin != null ? GuiUtils.skin.button : GuiUtils.defaultSkin.button);
				//active.alignment = TextAnchor.UpperLeft;
				active = new GUIStyle(inactive);
				active.active.textColor = active.focused.textColor = active.hover.textColor = active.normal.textColor = Color.green;
			}

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
					var maxSpeed = (wp.MaxSpeed > 0 ? wp.MaxSpeed : ap.speed.val);
					var minSpeed = (wp.MinSpeed > 0 ? wp.MinSpeed : (i < ap.Waypoints.Count - 1 || ap.LoopWaypoints ? maxSpeed / 2 : 0));
					string str = string.Format("[{0}] - {1} - R: {2:F1} m\n       S: {3:F0} ~ {4:F0} - D: {5}m - ETA: {6}", i + 1, wp.GetNameWithCoords(), wp.Radius,
					                           minSpeed, maxSpeed, MuUtils.ToSI(dist, -1), GuiUtils.TimeToDHMS(eta));
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
						GUILayout.Label("  Radius: ", GUILayout.ExpandWidth(false));
						tmpRadius = GUILayout.TextField(tmpRadius, GUILayout.Width(55));
						float.TryParse(tmpRadius, out ap.Waypoints[selIndex].Radius);
						GUILayout.Label("Speed: ", GUILayout.ExpandWidth(false));
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
					if (ap.WaypointIndex > selIndex) { ap.WaypointIndex--; }
				}
				selIndex = -1;
				//if (ap.WaypointIndex >= ap.Waypoints.Count) { ap.WaypointIndex = ap.Waypoints.Count - 1; }
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
	}
	
	public class MechJebRoverPathRenderer : MonoBehaviour {
		private MechJebModuleRoverController ap;
		private LineRenderer pastPath;
		private LineRenderer currPath;
		private LineRenderer nextPath;
		
		public static MechJebRoverPathRenderer AttachToMapView(MechJebCore Core) {
			var renderer = MapView.MapCamera.gameObject.GetComponent<MechJebRoverPathRenderer>();
			if (!renderer) { //Destroy(renderer); }
				renderer = MapView.MapCamera.gameObject.AddComponent<MechJebRoverPathRenderer>();
			}
			renderer.ap = Core.GetComputerModule<MechJebModuleRoverController>();
			//if (NewLineRenderer(ref renderer.pastPath)) { renderer.pastPath.SetColors(Color.blue, Color.blue); }
			//if (NewLineRenderer(ref renderer.currPath)) { renderer.currPath.SetColors(Color.green, Color.green); }
			//if (NewLineRenderer(ref renderer.nextPath)) { renderer.nextPath.SetColors(Color.yellow, Color.yellow); }
			return renderer;
		}
		
		public bool NewLineRenderer(ref LineRenderer Line) {
			if (Line != null) { return false; }
			GameObject obj = new GameObject("LineRenderer");
			Line = obj.AddComponent<LineRenderer>();
			Line.useWorldSpace = true;
			Line.material = new Material (Shader.Find ("Particles/Additive"));
			Line.SetWidth(10.0f, 10.0f);
			Line.SetVertexCount(2);
			Line.SetPosition(0, Vector3.zero);
			Line.SetPosition(1, Vector3.zero);
			Line.SetColors(Color.blue, Color.blue);
			return true;
		}

		public static Vector3 RaisePositionOverTerrain(Vector3 Position, float HeightOffset) {
			var body = FlightGlobals.ActiveVessel.mainBody;
			if (MapView.MapIsEnabled) {
				return ScaledSpace.LocalToScaledSpace(body.position + (body.Radius + HeightOffset) * body.GetSurfaceNVector(body.GetLatitude(Position), body.GetLongitude(Position)));
			}
			else {
				return body.GetWorldSurfacePosition(body.GetLatitude(Position), body.GetLongitude(Position), body.GetAltitude(Position) + HeightOffset);
			}
		}
		
		public new bool enabled {
			get { return base.enabled; }
			set {
				pastPath.enabled = currPath.enabled = nextPath.enabled = base.enabled = value;
			}
		}
		
		public void UpdatePath() {
			if (NewLineRenderer(ref pastPath)) { pastPath.SetColors(Color.blue, Color.blue); }
			if (NewLineRenderer(ref currPath)) { currPath.SetColors(Color.green, Color.green); }
			if (NewLineRenderer(ref nextPath)) { nextPath.SetColors(Color.yellow, Color.yellow); }
			
			//Debug.Log(ap.vessel.vesselName);
			
			if (ap != null && ap.Waypoints.Count > 0) {
				float targetHeight = (MapView.MapIsEnabled ? 100f : 2f);
				float width = (MapView.MapIsEnabled ? (float)(0.005 * PlanetariumCamera.fetch.Distance) : 1);
				//float width = (MapView.MapIsEnabled ? (float)mainBody.Radius / 10000 : 1);
				
				pastPath.SetWidth(width, width);
				currPath.SetWidth(width, width);
				nextPath.SetWidth(width, width);
				pastPath.gameObject.layer = currPath.gameObject.layer = nextPath.gameObject.layer = (MapView.MapIsEnabled ? 9 : 0);
				
				if (ap.WaypointIndex > 0) {
					//Debug.Log("drawing pastPath");
					pastPath.enabled = true;
					pastPath.SetVertexCount(ap.WaypointIndex + 1);
					for (int i = 0; i < ap.WaypointIndex; i++) {
						//Debug.Log("vert " + i.ToString());
						pastPath.SetPosition(i, RaisePositionOverTerrain(ap.Waypoints[i].Position, targetHeight));
					}
					pastPath.SetPosition(ap.WaypointIndex, RaisePositionOverTerrain(ap.vessel.CoM, targetHeight));
					//Debug.Log("pastPath drawn");
				}
				else {
					//Debug.Log("no pastPath");
					pastPath.enabled = false;
				}
				
				if (ap.WaypointIndex > -1) {
					//Debug.Log("drawing currPath");
					currPath.enabled = true;
					currPath.SetPosition(0, RaisePositionOverTerrain(ap.vessel.CoM, targetHeight));
					currPath.SetPosition(1, RaisePositionOverTerrain(ap.Waypoints[ap.WaypointIndex].Position, targetHeight));
					//Debug.Log("currPath drawn");
				}
				else {
					//Debug.Log("no currPath");
					currPath.enabled = false;
				}
				
				var nextCount = ap.Waypoints.Count - ap.WaypointIndex;
				if (nextCount > 1) {
					//Debug.Log("drawing nextPath of " + nextCount + " verts");
					nextPath.enabled = true;
					nextPath.SetVertexCount(nextCount);
					nextPath.SetPosition(0, RaisePositionOverTerrain((ap.WaypointIndex == -1 ? ap.vessel.CoM : (Vector3)ap.Waypoints[ap.WaypointIndex].Position), targetHeight));
					for (int i = 0; i < nextCount - 1; i++) {
						//Debug.Log("vert " + i.ToString() + " (" + (ap.WaypointIndex + 1 + i).ToString() + ")");
						nextPath.SetPosition(i + 1, RaisePositionOverTerrain(ap.Waypoints[ap.WaypointIndex + 1 + i].Position, targetHeight));
					}
					//Debug.Log("nextPath drawn");
				}
				else {
					//Debug.Log("no nextPath");
					nextPath.enabled = false;
				}
			}
			else {
				//Debug.Log("moo");
				pastPath.enabled = currPath.enabled = nextPath.enabled = false;
			}
		}
		
		public void OnPreRender() {
			//if (MapView.MapIsEnabled) {
				UpdatePath();
			//}
		}
		
		public void OnUpdate() {
			//if (!MapView.MapIsEnabled) { UpdatePath(); }
		}
	}
}
=======
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
	public class MechJebRoverRoute : List<MechJebRoverWaypoint> {
		public string Name;
		
		private CelestialBody body;
		public CelestialBody Body {
			get { return body; }
		}
		
		private string stats;
		public string Stats {
			get {
				updateStats(); // recalculation of the stats all the time are fine according to Majiir and Fractal_UK :3
				return stats;
			}
		}
		
		public ConfigNode ToConfigNode() {
			ConfigNode cn = new ConfigNode("Waypoints");
			cn.AddValue("Name", Name);
			cn.AddValue("Body", body.bodyName);
			this.ForEach(wp => cn.AddNode(wp.ToConfigNode()));
			return cn;
		}
		
		private void updateStats() {
			float distance = 0;
			if (Count > 1) {
				for (int i = 1; i < Count; i++) {
					distance += Vector3.Distance(this[i - 1].Position, this[i].Position);
				}
			}
			stats = string.Format("{0} waypoints over {1}m", Count, MuUtils.ToSI(distance, -1));
		}
		
		public MechJebRoverRoute(string Name = "", CelestialBody Body = null) {
			this.Name = Name;
			this.body = (Body != null ? Body : FlightGlobals.currentMainBody);
		}
		
		public MechJebRoverRoute(ConfigNode Node) { // feed this "Waypoints" nodes, just not the local ones of a ship
			if (Node == null) { return; }
			this.Name = (Node.HasValue("Name") ? Node.GetValue("Name") : "");
			this.body = (Node.HasValue("Body") ? FlightGlobals.Bodies.Find(b => b.bodyName == Node.GetValue("Body")) : null);
			if (Node.HasNode("Waypoint")) {
				foreach (ConfigNode cn in Node.GetNodes("Waypoint")) {
					this.Add(new MechJebRoverWaypoint(cn));
				}
			}
		}
		
//		public new void Add(MechJebRoverWaypoint Waypoint) {
//			this.Add(Waypoint);
//			updateStats();
//		}
//
//		public new void Insert(int Index, MechJebRoverWaypoint Waypoint) {
//			this.Insert(Index, Waypoint);
//			updateStats();
//		}
//
//		public new void Remove(MechJebRoverWaypoint Waypoint) {
//			this.Remove(Waypoint);
//			updateStats();
//		}
//
//		public new void RemoveAt(int Index) {
//			this.RemoveAt(Index);
//			updateStats();
//		}
//
//		public new void RemoveRange() {
//
//		}
	}
	
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
					core.GetComputerModule<MechJebModuleRoverWaypointWindow>().selIndex = -1;
					autopilot.WaypointIndex = 0;
					autopilot.Waypoints.Clear();
					if (vssl != null) {	autopilot.Waypoints.Add(new MechJebRoverWaypoint(vssl, 25f)); }
					else { autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition())); }
					autopilot.ControlHeading = autopilot.ControlSpeed = true;
					vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
					autopilot.LoopWaypoints = alt;
				}

				if (GUILayout.Button("Add Target")) {
					if (vssl != null) {	autopilot.Waypoints.Add(new MechJebRoverWaypoint(vssl, 25f)); }
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
						autopilot.LoopWaypoints = alt;
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
					autopilot.ControlHeading = autopilot.ControlSpeed = autopilot.LoopWaypoints = false;
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
			
		public override void OnModuleDisabled()
		{
			core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled = false;
			base.OnModuleDisabled();
		}
	}

	public class MechJebModuleRoverWaypointWindow : DisplayModule {
		public MechJebModuleRoverController ap;
		public static List<MechJebRoverRoute> Routes;
		[EditableInfoItem("Moho Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble MohoMapdist = 5000;
		[EditableInfoItem("Eve Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble EveMapdist = 5000;
		[EditableInfoItem("Gilly Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble GillyMapdist = -500;
		[EditableInfoItem("Kerbin Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble KerbinMapdist = 500;
		[EditableInfoItem("Mun Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble MunMapdist = 4000;
		[EditableInfoItem("Minmus Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble MinmusMapdist = 3500;
		[EditableInfoItem("Duna Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble DunaMapdist = 5000;
		[EditableInfoItem("Ike Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble IkeMapdist = 4000;
		[EditableInfoItem("Dres Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble DresMapdist = 1500;
		[EditableInfoItem("Eeloo Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble EelooMapdist = 2000;
		[EditableInfoItem("Jool Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble JoolMapdist = 30000;
		[EditableInfoItem("Tylo Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble TyloMapdist = 5000;
		[EditableInfoItem("Laythe Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble LaytheMapdist = 1000;
		[EditableInfoItem("Pol Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble PolMapdist = 500;
		[EditableInfoItem("Bop Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble BopMapdist = 1000;
		[EditableInfoItem("Vall Mapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble VallMapdist = 5000;
		internal int selIndex = -1;
		internal int saveIndex = -1;
		internal string tmpRadius = "";
		internal string tmpMinSpeed = "";
		internal string tmpMaxSpeed = "";
		private Vector2 scroll;
		private GUIStyle active;
		private GUIStyle inactive;
		private string titleAdd = "";
		private string saveName = "";
		private bool waitingForPick = false;
		private pages showPage = pages.waypoints;
		private enum pages { waypoints, settings, routes };
		private static MechJebRoverPathRenderer renderer;
		private Rect[] waypointRects = new Rect[0];
		private int lastIndex = -1;
//		private static LineRenderer redLine;
//		private static LineRenderer greenLine;

		public MechJebModuleRoverWaypointWindow(MechJebCore core) : base(core) { }
		
		public override void OnStart(PartModule.StartState state)
		{
			hidden = true;
			ap = core.GetComputerModule<MechJebModuleRoverController>();
			if (HighLogic.LoadedSceneIsFlight && vessel.isActiveVessel) {
				renderer = MechJebRoverPathRenderer.AttachToMapView(core);
				renderer.enabled = enabled;
			}
			if (Routes == null) { Routes = new List<MechJebRoverRoute>(); }
//			GameObject obj = new GameObject("LineRenderer");
//			redLine = obj.AddComponent<LineRenderer>();
//			redLine.useWorldSpace = true;
//			redLine.material = renderer.material;
//			redLine.SetWidth(10.0f, 10.0f);
//			redLine.SetColors(Color.red, Color.red);
//			redLine.SetVertexCount(2);
//			GameObject obj2 = new GameObject("LineRenderer");
//			greenLine = obj2.AddComponent<LineRenderer>();
//			greenLine.useWorldSpace = true;
//			greenLine.material = renderer.material;
//			greenLine.SetWidth(10.0f, 10.0f);
//			greenLine.SetColors(Color.green, Color.green);
//			greenLine.SetVertexCount(2);
			base.OnStart(state);
		}
		
		public override void OnModuleEnabled()
		{
			if (renderer != null) { renderer.enabled = true; }
			base.OnModuleEnabled();
		}
		
		public override void OnModuleDisabled()
		{
			if (renderer != null) { renderer.enabled = false; }
			base.OnModuleDisabled();
		}
		
		public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
		{
			var wps = global.GetNode("Routes");
			if (wps != null) {
				if (wps.HasNode("Waypoints")) {
					Routes.Clear();
					foreach (ConfigNode cn in wps.GetNodes("Waypoints")) {
						Routes.Add(new MechJebRoverRoute(cn));
					}
					Routes.Sort(SortRoutes);
				}
			}
			base.OnLoad(local, type, global);
		}
		
		public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
		{
			if (global.HasNode("Routes")) { global.RemoveNode("Routes"); }
			if (Routes.Count > 0) {
				var cn = global.AddNode(new ConfigNode("Routes"));
				Routes.Sort(SortRoutes);
				foreach (MechJebRoverRoute r in Routes) {
					cn.AddNode(r.ToConfigNode());
				}
			}
			base.OnSave(local, type, global);
		}
		
		public override string GetName()
		{
			return "Rover Waypoints" + (titleAdd != "" ? " - " + titleAdd : "");
		}
		
		public static Coordinates GetMouseFlightCoordinates()
		{
			var body = FlightGlobals.currentMainBody;
			var cam = FlightCamera.fetch.mainCamera;
			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
			RaycastHit raycast;
//			greenLine.SetPosition(0, ray.origin);
//			greenLine.SetPosition(1, (Vector3d)ray.direction * body.Radius / 2);
			if (Physics.Raycast(ray, out raycast, (float)body.Radius * 4f, ~(1 << 1))) {
				return new Coordinates(body.GetLatitude(raycast.point), MuUtils.ClampDegrees180(body.GetLongitude(raycast.point)));
			}
			else {
				Vector3d hit;
				//body.pqsController.RayIntersection(ray.origin, ray.direction, out hit);
				PQS.LineSphereIntersection(ray.origin - body.position, ray.direction, body.Radius, out hit);
				if (hit != Vector3d.zero) {
					hit = body.position + hit;
					Vector3d start = ray.origin;
					Vector3d end = hit;
					Vector3d point = Vector3d.zero;
					for (int i = 0; i < 16; i++) {
						point = (start + end) / 2;
						//var lat = body.GetLatitude(point);
						//var lon = body.GetLongitude(point);
						//var surf = body.GetWorldSurfacePosition(lat, lon, body.TerrainAltitude(lat, lon));
						var alt = body.GetAltitude(point) - body.TerrainAltitude(point);
						//Debug.Log(alt);
						if (alt > 0) {
							start = point;
						}
						else if (alt < 0) {
							end = point;
						}
						else {
							break;
						}
					}
					hit = point;
//					redLine.SetPosition(0, ray.origin);
//					redLine.SetPosition(1, hit);
					return new Coordinates(body.GetLatitude(hit), MuUtils.ClampDegrees180(body.GetLongitude(hit)));
				}
				else {
					return null;
				}
			}
		}
		
		private int SortRoutes(MechJebRoverRoute a, MechJebRoverRoute b) {
			var bn = string.Compare(a.Body.bodyName, b.Body.bodyName, true);
			if (bn != 0) {
				return bn;
			}
			else {
				return string.Compare(a.Name, b.Name, true);
			}
		}

		public override GUILayoutOption[] WindowOptions()
		{
			return new GUILayoutOption[] { GUILayout.Width(500), GUILayout.Height(400) };
		}

		protected override void WindowGUI(int windowID)
		{
			if (inactive == null) {
				inactive = new GUIStyle(GuiUtils.skin != null ? GuiUtils.skin.button : GuiUtils.defaultSkin.button);
				inactive.alignment = TextAnchor.UpperLeft;
			}
			if (active == null) {
				active = new GUIStyle(inactive);
				active.active.textColor = active.focused.textColor = active.hover.textColor = active.normal.textColor = Color.green;
			} // for some reason MJ's skin sometimes isn't loaded at OnStart so this has to be done here
			
			bool alt = Input.GetKey(KeyCode.LeftAlt);
			
			switch (showPage) {
				case pages.waypoints:
					scroll = GUILayout.BeginScrollView(scroll);
					if (ap.Waypoints.Count > 0) {
						waypointRects = new Rect[ap.Waypoints.Count];
						GUILayout.BeginVertical();
						double eta = 0;
						double dist = 0;
						for (int i = 0; i < ap.Waypoints.Count; i++) {
							var wp = ap.Waypoints[i];
							if (i >= ap.WaypointIndex) {
								if (ap.WaypointIndex > -1) {
									eta += GuiUtils.FromToETA((i == ap.WaypointIndex ? vessel.CoM : (Vector3)ap.Waypoints[i - 1].Position), (Vector3)wp.Position, (i == ap.WaypointIndex ? (float)ap.etaSpeed : wp.MaxSpeed));
								}
								dist += Vector3.Distance((i == ap.WaypointIndex || ap.WaypointIndex == -1 ? vessel.CoM : (Vector3)ap.Waypoints[i - 1].Position), (Vector3)wp.Position);
							}
							var maxSpeed = (wp.MaxSpeed > 0 ? wp.MaxSpeed : ap.speed.val);
							var minSpeed = (wp.MinSpeed > 0 ? wp.MinSpeed : (i < ap.Waypoints.Count - 1 || ap.LoopWaypoints ? maxSpeed / 2 : 0));
							string str = string.Format("[{0}] - {1} - R: {2:F1} m\n       S: {3:F0} ~ {4:F0} - D: {5}m - ETA: {6}", i + 1, wp.GetNameWithCoords(), wp.Radius,
							                           minSpeed, maxSpeed, MuUtils.ToSI(dist, -1), GuiUtils.TimeToDHMS(eta));
							GUI.backgroundColor = (i == ap.WaypointIndex ? new Color(0.5f, 1f, 0.5f) : Color.white);
							if (GUILayout.Button(str, (i == selIndex ? active : inactive))) {
								if (alt) {
									ap.WaypointIndex = i;
								}
								else {
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
							}
							if(Event.current.type == EventType.Repaint) {
								waypointRects[i] = GUILayoutUtility.GetLastRect();
								//if (i == ap.WaypointIndex) { Debug.Log(Event.current.type.ToString() + " - " + waypointRects[i].ToString() + " - " + scroll.ToString()); }
							}
							GUI.backgroundColor = Color.white;
							
							if (selIndex > -1 && selIndex == i) {
								GUILayout.BeginHorizontal();
								GUILayout.Label("  Radius: ", GUILayout.ExpandWidth(false));
								tmpRadius = GUILayout.TextField(tmpRadius, GUILayout.Width(50));
								float.TryParse(tmpRadius, out wp.Radius);
								if (GUILayout.Button("A", GUILayout.ExpandWidth(false))) { ap.Waypoints.ForEach(fewp => fewp.Radius = wp.Radius); }
								GUILayout.Label("Speed: ", GUILayout.ExpandWidth(false));
								tmpMinSpeed = GUILayout.TextField(tmpMinSpeed, GUILayout.Width(40));
								float.TryParse(tmpMinSpeed, out wp.MinSpeed);
								if (GUILayout.Button("A", GUILayout.ExpandWidth(false))) { ap.Waypoints.ForEach(fewp => fewp.MinSpeed = wp.MinSpeed); }
								tmpMaxSpeed = GUILayout.TextField(tmpMaxSpeed, GUILayout.Width(40));
								float.TryParse(tmpMaxSpeed, out wp.MaxSpeed);
								if (GUILayout.Button("A", GUILayout.ExpandWidth(false))) { ap.Waypoints.ForEach(fewp => fewp.MaxSpeed = wp.MaxSpeed); }
								GUILayout.EndHorizontal();
							}
						}
						titleAdd = "Distance: " + MuUtils.ToSI(dist, -1) + "m - ETA: " + GuiUtils.TimeToDHMS(eta);
						GUILayout.EndVertical();
					}
					else {
						titleAdd = "";
					}
					GUILayout.EndScrollView();

					GUILayout.BeginHorizontal();
					if (GUILayout.Button(alt ? "Reverse" : (!waitingForPick ? "Add Waypoint" : "Abort Adding"), GUILayout.Width(110))) {
						if (alt) {
							ap.Waypoints.Reverse();
							if (ap.WaypointIndex > -1) { ap.WaypointIndex = ap.Waypoints.Count - 1 - ap.WaypointIndex; }
							if (selIndex > -1) { selIndex = ap.Waypoints.Count - 1 - selIndex; }
						}
						else {
							if (!waitingForPick) {
								waitingForPick = true;
								if (MapView.MapIsEnabled) {
									core.target.Unset();
									core.target.PickPositionTargetOnMap();
								}
							}
							else {
								waitingForPick = false;
							}
						}
					}
					if (GUILayout.Button((alt ? "Clear" : "Remove"), GUILayout.Width(65))) {
						if (alt) {
							ap.WaypointIndex = -1;
							ap.Waypoints.Clear();
						}
						else {
							ap.Waypoints.RemoveAt(selIndex);
							if (ap.WaypointIndex > selIndex) { ap.WaypointIndex--; }
						}
						selIndex = -1;
						//if (ap.WaypointIndex >= ap.Waypoints.Count) { ap.WaypointIndex = ap.Waypoints.Count - 1; }
					}
					if (GUILayout.Button((alt ? "Top" : "Up"), GUILayout.Width(57))) {
						do {
							if (selIndex > 0) {
								ap.Waypoints.Swap(selIndex, selIndex - 1);
								selIndex--;
							}
							else {
								break;
							}
						}
						while (alt);
					}
					if (GUILayout.Button((alt ? "Bottom" : "Down"), GUILayout.Width(57))) {
						do {
							if (selIndex > -1 && selIndex <= ap.Waypoints.Count - 1) {
								ap.Waypoints.Swap(selIndex, selIndex + 1);
								selIndex++;
							}
							else {
								break;
							}
						}
						while (alt);
					}
					if (GUILayout.Button("Routes")) {
						showPage = pages.routes;
						scroll = Vector2.zero;
					}
					if (GUILayout.Button("Settings")) {
						showPage = pages.settings;
						scroll = Vector2.zero;
					}
					GUILayout.EndHorizontal();
					break;
					
					
				case pages.settings:
					titleAdd = "Settings";
					scroll = GUILayout.BeginScrollView(scroll);
					MechJebModuleCustomWindowEditor ed = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
					GUILayout.BeginHorizontal();
					GUILayout.BeginVertical();
					ed.registry.Find(i => i.id == "Editable:RoverController.hPIDp").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.hPIDi").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.hPIDd").DrawItem();
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					ed.registry.Find(i => i.id == "Editable:RoverController.sPIDp").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.sPIDi").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.sPIDd").DrawItem();
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
					ed.registry.Find(i => i.id == "Editable:RoverController.turnSpeed").DrawItem();
					GUILayout.BeginHorizontal();
					GUILayout.BeginVertical();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.MohoMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.EveMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.GillyMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.KerbinMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.MunMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.MinmusMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.DunaMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.IkeMapdist").DrawItem();
					GUILayout.EndVertical();
					GUILayout.BeginVertical();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.DresMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.JoolMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.LaytheMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.VallMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.TyloMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.BopMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.PolMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverWaypointWindow.EelooMapdist").DrawItem();
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
					GUILayout.EndScrollView();

					GUILayout.BeginHorizontal();
					if (GUILayout.Button("Waypoints")) {
						showPage = pages.waypoints;
						scroll = Vector2.zero;
						lastIndex = -1;
					}
					if (GUILayout.Button("Routes")) {
						showPage = pages.routes;
						scroll = Vector2.zero;
					}
					GUILayout.EndHorizontal();
					break;
					
					
				case pages.routes:
					titleAdd = "Routes for " + vessel.mainBody.bodyName;
					
					scroll = GUILayout.BeginScrollView(scroll);
					var bodyWPs = Routes.FindAll(wp => wp.Body == vessel.mainBody);
					for (int i = 0; i < bodyWPs.Count; i++) {
						GUILayout.BeginHorizontal();
						var str = bodyWPs[i].Name + " - " + bodyWPs[i].Stats;
						if (GUILayout.Button(str, (i == saveIndex ? active : inactive))) {
							saveIndex = (saveIndex == i ? -1 : i);
						}
						if (i == saveIndex) {
							if (GUILayout.Button("Delete", GUILayout.Width(70))) {
								Routes.RemoveAll(wp => wp.Name == bodyWPs[i].Name && wp.Body == vessel.mainBody);
								saveIndex = -1;
							}
						}
						GUILayout.EndHorizontal();
					}
					GUILayout.EndScrollView();
					
					GUILayout.BeginHorizontal();
					saveName = GUILayout.TextField(saveName, GUILayout.Width(150));
					if (GUILayout.Button("Save")) {
						if (saveName != "" && ap.Waypoints.Count > 0) {
							var old = Routes.Find(list => list.Name == saveName && list.Body == vessel.mainBody);
							var wps = new MechJebRoverRoute(saveName, vessel.mainBody);
							ap.Waypoints.ForEach(wp => wps.Add(wp));
							if (old == null) {
								Routes.Add(wps);
							}
							else {
								Routes[Routes.IndexOf(old)] = wps;
							}
							Routes.Sort(SortRoutes);
						}
					}
					if (GUILayout.Button((alt ? "Add" : "Load"), GUILayout.Width(50))) {
						if (saveIndex > -1) {
							if (!alt) {
								ap.WaypointIndex = -1;
								ap.Waypoints.Clear();
							}
							Routes[saveIndex].ForEach(wp => ap.Waypoints.Add(wp));
						}
					}
					if (GUILayout.Button("Waypoints")) {
						showPage = pages.waypoints;
						scroll = Vector2.zero;
						lastIndex = -1;
					}
					if (GUILayout.Button("Settings")) {
						showPage = pages.settings;
						scroll = Vector2.zero;
					}
					GUILayout.EndHorizontal();
					break;
			}
			
			if (selIndex >= ap.Waypoints.Count) { selIndex = -1; }
			if (selIndex == -1 && ap.WaypointIndex > -1 && lastIndex != ap.WaypointIndex && waypointRects.Length > 0) {
				scroll.y = waypointRects[ap.WaypointIndex].y - 160;
			}
			lastIndex = ap.WaypointIndex;
			
			if (waitingForPick && vessel.isActiveVessel) {
				if (MapView.MapIsEnabled) {
					if (core.target.pickingPositionTarget == false) {
						if (core.target.PositionTargetExists) {
							if (selIndex > -1 && selIndex < ap.Waypoints.Count) {
								ap.Waypoints.Insert(selIndex, new MechJebRoverWaypoint(core.target.GetPositionTargetPosition()));
							}
							else {
								ap.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition()));
							}
							core.target.Unset();
							waitingForPick = false;
						}
						else {
							core.target.PickPositionTargetOnMap();
						}
					}
				}
				else {
					if (!GuiUtils.MouseIsOverWindow(core)) {
						Coordinates mouseCoords = GetMouseFlightCoordinates();
						if (mouseCoords != null) {
							if (Input.GetMouseButtonDown(0)) {
								if (selIndex > -1 && selIndex < ap.Waypoints.Count) {
									ap.Waypoints.Insert(selIndex, new MechJebRoverWaypoint(mouseCoords.latitude, mouseCoords.longitude));
								}
								else {
									ap.Waypoints.Add(new MechJebRoverWaypoint(mouseCoords.latitude, mouseCoords.longitude));
								}
								waitingForPick = false;
							}
						}
					}
				}
			}
			
			base.WindowGUI(windowID);
		}
		
		public override void OnFixedUpdate()
		{
			if (vessel.isActiveVessel && (renderer == null || renderer.ap != ap)) { MechJebRoverPathRenderer.AttachToMapView(core); } //MechJebRoverPathRenderer.AttachToMapView(core); }
			ap.Waypoints.ForEach(wp => wp.Update());
			base.OnFixedUpdate();
		}
	}

	public class MechJebRoverPathRenderer : MonoBehaviour {
		public readonly Material material = new Material (Shader.Find ("Particles/Additive"));
		public MechJebModuleRoverController ap;
		private LineRenderer pastPath;
		private LineRenderer currPath;
		private LineRenderer nextPath;
		private LineRenderer selWP;
		private Color pastPathColor = new Color(0f, 0f, 1f, 0.5f);
		private Color currPathColor = new Color(0f, 1f, 0f, 0.5f);
		private Color nextPathColor = new Color(1f, 1f, 0f, 0.5f);
		private Color selWPColor = new Color(1f, 0f, 0f, 0.5f);
		private double addHeight;
		
		public static MechJebRoverPathRenderer AttachToMapView(MechJebCore Core) {
			var renderer = MapView.MapCamera.gameObject.GetComponent<MechJebRoverPathRenderer>();
			if (!renderer) { //Destroy(renderer); }
				renderer = MapView.MapCamera.gameObject.AddComponent<MechJebRoverPathRenderer>();
			}
			renderer.ap = Core.GetComputerModule<MechJebModuleRoverController>();
			return renderer;
		}
		
		public bool NewLineRenderer(ref LineRenderer Line) {
			if (Line != null) { return false; }
			GameObject obj = new GameObject("LineRenderer");
			Line = obj.AddComponent<LineRenderer>();
			Line.useWorldSpace = true;
			Line.material = material;
			Line.SetWidth(10.0f, 10.0f);
			Line.SetVertexCount(2);
			return true;
		}

		public static Vector3 RaisePositionOverTerrain(Vector3 Position, float HeightOffset) {
			var body = FlightGlobals.ActiveVessel.mainBody;
			if (MapView.MapIsEnabled) {
				var lat = body.GetLatitude(Position);
				var lon = body.GetLongitude(Position);
				return ScaledSpace.LocalToScaledSpace(body.position + (body.Radius + HeightOffset + body.TerrainAltitude(lat, lon)) * body.GetSurfaceNVector(lat, lon));
			}
			else {
				return body.GetWorldSurfacePosition(body.GetLatitude(Position), body.GetLongitude(Position), body.GetAltitude(Position) + HeightOffset);
			}
		}
		
		public new bool enabled {
			get { return base.enabled; }
			set {
				base.enabled = value;
				if (pastPath != null) { pastPath.enabled = value; }
				if (currPath != null) { currPath.enabled = value; }
				if (nextPath != null) { nextPath.enabled = value; }
			}
		}
		
		public void OnPreRender() {
			if (NewLineRenderer(ref pastPath)) { pastPath.SetColors(pastPathColor, pastPathColor); }
			if (NewLineRenderer(ref currPath)) { currPath.SetColors(currPathColor, currPathColor); }
			if (NewLineRenderer(ref nextPath)) { nextPath.SetColors(nextPathColor, nextPathColor); }
			if (NewLineRenderer(ref selWP)) { selWP.SetColors(selWPColor, selWPColor); }
			
			//Debug.Log(ap.vessel.vesselName);
			var body = ap.core.GetComputerModule<MechJebModuleRoverWaypointWindow>();
			switch (ap.vessel.mainBody.bodyName) {
					case "Moho" : addHeight = body.MohoMapdist; break;
					case "Eve" : addHeight = body.EveMapdist; break;
					case "Gilly" : addHeight = body.GillyMapdist; break;
					case "Kerbin" : addHeight = body.KerbinMapdist; break;
					case "Mun" : addHeight = body.MunMapdist; break;
					case "Minmus" : addHeight = body.MinmusMapdist; break;
					case "Duna" : addHeight = body.DunaMapdist; break;
					case "Ike" : addHeight = body.IkeMapdist; break;
					case "Dres" : addHeight = body.DresMapdist; break;
					case "Jool" : addHeight = body.JoolMapdist; break;
					case "Laythe" : addHeight = body.LaytheMapdist; break;
					case "Vall" : addHeight = body.VallMapdist; break;
					case "Tylo" : addHeight = body.TyloMapdist; break;
					case "Bop" : addHeight = body.BopMapdist; break;
					case "Pol" : addHeight = body.PolMapdist; break;
					case "Eeloo" : addHeight = body.EelooMapdist; break;
			}
			
			if (ap != null && ap.Waypoints.Count > 0 && ap.vessel.isActiveVessel && HighLogic.LoadedSceneIsFlight) {
				float scale = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, ap.vessel.CoM) / 900f;
				float targetHeight = (MapView.MapIsEnabled ? (float)addHeight : 3f);
				float width = (MapView.MapIsEnabled ? (float)(0.005 * PlanetariumCamera.fetch.Distance) : scale + 0.1f);
				//float width = (MapView.MapIsEnabled ? (float)mainBody.Radius / 10000 : 1);
				
				pastPath.SetWidth(width, width);
				currPath.SetWidth(width, width);
				nextPath.SetWidth(width, width);
				selWP.gameObject.layer = pastPath.gameObject.layer = currPath.gameObject.layer = nextPath.gameObject.layer = (MapView.MapIsEnabled ? 9 : 0);
				
				int sel = ap.core.GetComputerModule<MechJebModuleRoverWaypointWindow>().selIndex;
				selWP.enabled = sel > -1;
				if (selWP.enabled) {
					selWP.SetWidth(0f, width * 10f);
					selWP.SetPosition(0, RaisePositionOverTerrain(ap.Waypoints[sel].Position, targetHeight + 3f));
					selWP.SetPosition(1, RaisePositionOverTerrain(ap.Waypoints[sel].Position, targetHeight + width * 15f));
				}
				
				if (ap.WaypointIndex > 0) {
					//Debug.Log("drawing pastPath");
					pastPath.enabled = true;
					pastPath.SetVertexCount(ap.WaypointIndex + 1);
					for (int i = 0; i < ap.WaypointIndex; i++) {
						//Debug.Log("vert " + i.ToString());
						pastPath.SetPosition(i, RaisePositionOverTerrain(ap.Waypoints[i].Position, targetHeight));
					}
					pastPath.SetPosition(ap.WaypointIndex, RaisePositionOverTerrain(ap.vessel.CoM, targetHeight));
					//Debug.Log("pastPath drawn");
				}
				else {
					//Debug.Log("no pastPath");
					pastPath.enabled = false;
				}
				
				if (ap.WaypointIndex > -1) {
					//Debug.Log("drawing currPath");
					currPath.enabled = true;
					currPath.SetPosition(0, RaisePositionOverTerrain(ap.vessel.CoM, targetHeight));
					currPath.SetPosition(1, RaisePositionOverTerrain(ap.Waypoints[ap.WaypointIndex].Position, targetHeight));
					//Debug.Log("currPath drawn");
				}
				else {
					//Debug.Log("no currPath");
					currPath.enabled = false;
				}
				
				var nextCount = ap.Waypoints.Count - ap.WaypointIndex;
				if (nextCount > 1) {
					//Debug.Log("drawing nextPath of " + nextCount + " verts");
					nextPath.enabled = true;
					nextPath.SetVertexCount(nextCount);
					nextPath.SetPosition(0, RaisePositionOverTerrain((ap.WaypointIndex == -1 ? ap.vessel.CoM : (Vector3)ap.Waypoints[ap.WaypointIndex].Position), targetHeight));
					for (int i = 0; i < nextCount - 1; i++) {
						//Debug.Log("vert " + i.ToString() + " (" + (ap.WaypointIndex + 1 + i).ToString() + ")");
						nextPath.SetPosition(i + 1, RaisePositionOverTerrain(ap.Waypoints[ap.WaypointIndex + 1 + i].Position, targetHeight));
					}
					//Debug.Log("nextPath drawn");
				}
				else {
					//Debug.Log("no nextPath");
					nextPath.enabled = false;
				}
			}
			else {
				//Debug.Log("moo");
				selWP.enabled = pastPath.enabled = currPath.enabled = nextPath.enabled = false;
			}
		}
	}
}
>>>>>>> a94aeef... improvements to the driving, waypoint editor capable of editing the radius now
