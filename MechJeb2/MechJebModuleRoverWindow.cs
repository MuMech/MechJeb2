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
					if (vssl != null) {	autopilot.Waypoints.Add(new MechJebRoverWaypoint(vssl)); }
					else { autopilot.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition())); }
					autopilot.ControlHeading = autopilot.ControlSpeed = true;
					vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, false);
					autopilot.LoopWaypoints = alt;
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
		internal int selIndex = -1;
		internal string tmpRadius = "";
		internal string tmpMinSpeed = "";
		internal string tmpMaxSpeed = "";
		private Vector2 scroll;
		private GUIStyle active;
		private GUIStyle inactive;
		private string titleAdd = "";
		private bool waitingForPick = false;
		private bool pickingTerrain = false;
		private static MechJebRoverPathRenderer renderer;
		private Rect[] waypointRects = new Rect[0];
		private int lastIndex = -1;

		public MechJebModuleRoverWaypointWindow(MechJebCore core) : base(core) { }
		
		public override void OnStart(PartModule.StartState state)
		{
			hidden = true;
			ap = core.GetComputerModule<MechJebModuleRoverController>();
			if (HighLogic.LoadedSceneIsFlight && vessel.isActiveVessel) {
				renderer = MechJebRoverPathRenderer.AttachToMapView(core);
				renderer.enabled = enabled;
			}
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
		
		public override string GetName()
		{
			return "Rover Waypoints" + (ap.Waypoints.Count > 0 && titleAdd != "" ? " - " + titleAdd : "");
		}
		
		public static Coordinates GetMouseFlightCoordinates()
		{
			var body = FlightGlobals.currentMainBody;
			Ray mouseRay = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit raycast;
			if (Physics.Raycast(mouseRay, out raycast, (float)body.Radius * 4f, ~(1 << 1))) {
				return new Coordinates(body.GetLatitude(raycast.point), MuUtils.ClampDegrees180(body.GetLongitude(raycast.point)));
			}
			else {
				Vector3d hit;
				//body.pqsController.RayIntersection(mouseRay.origin, mouseRay.direction, out hit);
				PQS.LineSphereIntersection(mouseRay.origin - body.position, mouseRay.direction, body.Radius, out hit);
				Debug.Log(hit);
				if (hit != Vector3d.zero) {
					return new Coordinates(body.GetLatitude(hit), MuUtils.ClampDegrees180(body.GetLongitude(hit)));
				}
				else {
					return null;
				}
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
				//active = new GUIStyle(GuiUtils.skin != null ? GuiUtils.skin.button : GuiUtils.defaultSkin.button);
				//active.alignment = TextAnchor.UpperLeft;
				active = new GUIStyle(inactive);
				active.active.textColor = active.focused.textColor = active.hover.textColor = active.normal.textColor = Color.green;
			} // for some reason MJ's skin sometimes isn't loaded at OnStart so this has to be done here
			
			bool alt = Input.GetKey(KeyCode.LeftAlt);

			scroll = GUILayout.BeginScrollView(scroll);//, new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true) });
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
			if (GUILayout.Button(!waitingForPick && !pickingTerrain ? "Add Waypoint" : "Abort Adding")) {
				if (!waitingForPick && !pickingTerrain) {
					if (MapView.MapIsEnabled) {
						waitingForPick = true;
						core.target.Unset();
						core.target.PickPositionTargetOnMap();
					}
					else {
						pickingTerrain = true;
					}
				}
				else {
					waitingForPick = pickingTerrain = false;
				}
			}
			if (GUILayout.Button("Remove")) {
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
			if (GUILayout.Button("Move Up", GUILayout.Width(85))) {
				do {
					if (selIndex > 0) {
						ap.Waypoints.Swap(selIndex, selIndex - 1);
						/*if (ap.WaypointIndex == selIndex) {
						ap.WaypointIndex--;
					}
					else if (ap.WaypointIndex == selIndex - 1) {
						ap.WaypointIndex++;
					} /**/
						selIndex--;
					}
					else {
						break;
					}
				}
				while (alt);
			}
			if (GUILayout.Button("Move Down", GUILayout.Width(85))) {
				do {
					if (selIndex > -1 && selIndex <= ap.Waypoints.Count - 1) {
						ap.Waypoints.Swap(selIndex, selIndex + 1);
						/*if (ap.WaypointIndex == selIndex) {
						ap.WaypointIndex++;
					}
					else if (ap.WaypointIndex == selIndex + 1) {
						ap.WaypointIndex--;
					} /**/
						selIndex++;
					}
					else {
						break;
					}
				}
				while (alt);
			}
//			if (GUILayout.Button("Settings", GUILayout.ExpandWidth(false))) {
//			}
			GUILayout.EndHorizontal();
			
			if (waitingForPick) {
				if (core.target.pickingPositionTarget == false) {
					if (core.target.PositionTargetExists) {
						ap.Waypoints.Add(new MechJebRoverWaypoint(core.target.GetPositionTargetPosition()));
						core.target.Unset();
					}
					waitingForPick = false;
				}
			}

			if (MapView.MapIsEnabled) pickingTerrain = false; //stop picking on leaving map view
			if (pickingTerrain && vessel.isActiveVessel) {
				if (!GuiUtils.MouseIsOverWindow(core)) {
					Coordinates mouseCoords = GetMouseFlightCoordinates();
					if (mouseCoords != null) {
						if (Input.GetMouseButtonDown(0)) {
							ap.Waypoints.Add(new MechJebRoverWaypoint(mouseCoords.latitude, mouseCoords.longitude));
							pickingTerrain = false;
						}
					}
				}
				else {
					//if (leftWindow && Input.GetMouseButtonDown(0)) { pickingTerrain = false; }
				}
			}
			
			if (selIndex >= ap.Waypoints.Count) { selIndex = -1; }
			if (waypointRects.Length > 0 && ap.WaypointIndex > -1 && lastIndex != ap.WaypointIndex && selIndex == -1) {
				scroll.y = waypointRects[ap.WaypointIndex].y - 160;
			}
			lastIndex = ap.WaypointIndex;
			
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
		public MechJebModuleRoverController ap;
		private LineRenderer pastPath;
		private LineRenderer currPath;
		private LineRenderer nextPath;
		private LineRenderer selWP;
		private Color pastPathColor = new Color(0f, 0f, 1f, 0.5f);
		private Color currPathColor = new Color(0f, 1f, 0f, 0.5f);
		private Color nextPathColor = new Color(1f, 1f, 0f, 0.5f);
		private Color selWPColor = new Color(1f, 0f, 0f, 0.5f);
		private Material material = new Material (Shader.Find ("Particles/Additive"));
		
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
				return ScaledSpace.LocalToScaledSpace(body.position + (body.Radius + HeightOffset) * body.GetSurfaceNVector(body.GetLatitude(Position), body.GetLongitude(Position)));
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
			
			if (ap != null && ap.Waypoints.Count > 0 && ap.vessel.isActiveVessel && HighLogic.LoadedSceneIsFlight) {
				float targetHeight = (MapView.MapIsEnabled ? 300f : 3f);
				float width = (MapView.MapIsEnabled ? (float)(0.005 * PlanetariumCamera.fetch.Distance) : 1);
				float width2 = (MapView.MapIsEnabled ? (float)(0.005 * PlanetariumCamera.fetch.Distance) : 2);
				//float width = (MapView.MapIsEnabled ? (float)mainBody.Radius / 10000 : 1);
				
				pastPath.SetWidth(width, width);
				currPath.SetWidth(width, width2);
				nextPath.SetWidth(width2, width2);
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
