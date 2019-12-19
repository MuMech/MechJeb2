using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using KSP.Localization;
namespace MuMech
{
	public class MechJebWaypoint {
		public const float defaultRadius = 5;
		public double Latitude;
		public double Longitude;
		public double Altitude;
		public Vector3d Position;
		public float Radius;
		public string Name;
		public Vessel Target;
		public float MinSpeed;
		public float MaxSpeed;
		public bool Quicksave;

		public CelestialBody Body  {
			get { return (Target != null ? Target.mainBody : FlightGlobals.ActiveVessel.mainBody); }
		}

		public MechJebWaypoint(double Latitude, double Longitude, float Radius = defaultRadius, string Name = "", float MinSpeed = 0, float MaxSpeed = 0) { //, CelestialBody Body = null) {
			this.Latitude = Latitude;
			this.Longitude = Longitude;
			this.Radius = Radius;
			this.Name = (Name == null ? "" : Name);
			this.MinSpeed = MinSpeed;
			this.MaxSpeed = MaxSpeed;
			Update();
		}

		public MechJebWaypoint(Vector3d Position, float Radius = defaultRadius, string Name = "", float MinSpeed = 0, float MaxSpeed = 0) { //, CelestialBody Body = null) {
			this.Latitude = Body.GetLatitude(Position);
			this.Longitude = Body.GetLongitude(Position);
			this.Radius = Radius;
			this.Name = (Name == null ? "" : Name);
			this.MinSpeed = MinSpeed;
			this.MaxSpeed = MaxSpeed;
			Update();
		}

		public MechJebWaypoint(Vessel Target, float Radius = defaultRadius, string Name = "", float MinSpeed = 0, float MaxSpeed = 0) {
			this.Target = Target;
			this.Radius = Radius;
			this.Name = (Name == null ? "" : Name);
			this.MinSpeed = MinSpeed;
			this.MaxSpeed = MaxSpeed;
			Update();
		}

		public MechJebWaypoint(ConfigNode Node) {
			if (Node.HasValue("Latitude")) { double.TryParse(Node.GetValue("Latitude"), out this.Latitude); }
			if (Node.HasValue("Longitude")) { double.TryParse(Node.GetValue("Longitude"), out this.Longitude); }
			this.Target = (Node.HasValue("Target") ? FlightGlobals.Vessels.Find(v => v.id.ToString() == Node.GetValue("Target")) : null);
			if (Node.HasValue("Radius")) { float.TryParse(Node.GetValue("Radius"), out this.Radius); } else { this.Radius = defaultRadius; }
			this.Name = (Node.HasValue("Name") ? Node.GetValue("Name") : "");
			if (Node.HasValue("MinSpeed")) { float.TryParse(Node.GetValue("MinSpeed"), out this.MinSpeed); }
			if (Node.HasValue("MaxSpeed")) { float.TryParse(Node.GetValue("MaxSpeed"), out this.MaxSpeed); }
			if (Node.HasValue("Quicksave")) { bool.TryParse(Node.GetValue("Quicksave"), out this.Quicksave); }
			Update();
		}

		public ConfigNode ToConfigNode() {
			ConfigNode cn = new ConfigNode("Waypoint");
			if (Target != null) {
				cn.AddValue("Target", Target.id);
			}
			if (Name != "") { cn.AddValue("Name", Name); }
			cn.AddValue("Latitude", Latitude);
			cn.AddValue("Longitude", Longitude);
			cn.AddValue("Radius", Radius);
			cn.AddValue("MinSpeed", MinSpeed);
			cn.AddValue("MaxSpeed", MaxSpeed);
			cn.AddValue("Quicksave", Quicksave);
			return cn;
		}

		public string GetNameWithCoords() {
			return (Name != "" ? Name : (Target != null ? Target.vesselName : "Waypoint")) + " - " + Coordinates.ToStringDMS(Latitude, Longitude, false);
//				((Latitude >= 0 ? "N " : "S ") + Math.Abs(Math.Round(Latitude, 3)) + ", " + (Longitude >= 0 ? "E " : "W ") + Math.Abs(Math.Round(Longitude, 3)));
		}

		public void Update() {
			if (Target != null) {
				Position = Target.CoM;
				Latitude = Body.GetLatitude(Position);
				Longitude = Body.GetLongitude(Position);
			}
			else {
				Position = Body.GetWorldSurfacePosition(Latitude, Longitude, Body.TerrainAltitude(Latitude, Longitude));
				if (Vector3d.Distance(Position, FlightGlobals.ActiveVessel.CoM) < 200) {
					var dir = (Position - Body.position).normalized;
					var rayPos = Body.position + dir * (Body.Radius + 50000);
					dir = (Vector3d)(Body.position - rayPos).normalized;
					RaycastHit hit;
					var raycast = Physics.Raycast(rayPos, dir, out hit, (float)Body.Radius, 1 << 15);
					if (raycast) {
						dir = (hit.point - Body.position);
						Position = Body.position + dir.normalized * (dir.magnitude + 0.5);
//						Latitude = Body.GetLatitude(Position);
//						Longitude = Body.GetLongitude(Position);
					}
				}
			}
			if (MinSpeed > 0 && MaxSpeed > 0 && MinSpeed > MaxSpeed) { MinSpeed = MaxSpeed; }
			else if (MinSpeed > 0 && MaxSpeed > 0 && MaxSpeed < MinSpeed) { MaxSpeed = MinSpeed; }
		}
	}

	public class MechJebWaypointRoute : List<MechJebWaypoint> {
		public string Name;

		private CelestialBody body;
		public CelestialBody Body {
			get { return body; }
		}

		public string Mode { get; private set; }

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
			cn.AddValue("Mode", Mode);
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

		public MechJebWaypointRoute(string Name = "", CelestialBody Body = null, string Mode = "Rover") {
			this.Name = Name;
			this.body = (Body != null ? Body : FlightGlobals.currentMainBody);
			this.Mode = Mode;
		}

		public MechJebWaypointRoute(ConfigNode Node) { // feed this "Waypoints" nodes, just not the local ones of a ship
			if (Node == null) { return; }
			this.Name = (Node.HasValue("Name") ? Node.GetValue("Name") : "");
			this.body = (Node.HasValue("Body") ? FlightGlobals.Bodies.Find(b => b.bodyName == Node.GetValue("Body")) : null);
			this.Mode = (Node.HasValue("Mode") ? Node.GetValue("Mode") : "Rover");
			if (Node.HasNode("Waypoint")) {
				foreach (ConfigNode cn in Node.GetNodes("Waypoint")) {
					this.Add(new MechJebWaypoint(cn));
				}
			}
		}

//		public new void Add(MechJebWaypoint Waypoint) {
//			this.Add(Waypoint);
//			updateStats();
//		}
//
//		public new void Insert(int Index, MechJebWaypoint Waypoint) {
//			this.Insert(Index, Waypoint);
//			updateStats();
//		}
//
//		public new void Remove(MechJebWaypoint Waypoint) {
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

	public class MechJebModuleWaypointWindow : DisplayModule {
		public enum WaypointMode {
			Rover,
			Plane
		}

		public WaypointMode Mode = WaypointMode.Rover;
		public MechJebModuleRoverController ap;
		public static List<MechJebWaypointRoute> Routes = new List<MechJebWaypointRoute>();
		[EditableInfoItem("#MechJeb_MohoMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Moho Mapdist
		public EditableDouble MohoMapdist = 5000;
		[EditableInfoItem("#MechJeb_EveMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Eve Mapdist
		public EditableDouble EveMapdist = 5000;
		[EditableInfoItem("#MechJeb_GillyMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Gilly Mapdist
		public EditableDouble GillyMapdist = -500;
		[EditableInfoItem("#MechJeb_KerbinMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Kerbin Mapdist
		public EditableDouble KerbinMapdist = 500;
		[EditableInfoItem("#MechJeb_MunMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Mun Mapdist
		public EditableDouble MunMapdist = 4000;
		[EditableInfoItem("#MechJeb_MinmusMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Minmus Mapdist
		public EditableDouble MinmusMapdist = 3500;
		[EditableInfoItem("#MechJeb_DunaMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Duna Mapdist
		public EditableDouble DunaMapdist = 5000;
		[EditableInfoItem("#MechJeb_IkeMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Ike Mapdist
		public EditableDouble IkeMapdist = 4000;
		[EditableInfoItem("#MechJeb_DresMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Dres Mapdist
		public EditableDouble DresMapdist = 1500;
		[EditableInfoItem("#MechJeb_EelooMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Eeloo Mapdist
		public EditableDouble EelooMapdist = 2000;
		[EditableInfoItem("#MechJeb_JoolMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Jool Mapdist
		public EditableDouble JoolMapdist = 30000;
		[EditableInfoItem("#MechJeb_TyloMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Tylo Mapdist
		public EditableDouble TyloMapdist = 5000;
		[EditableInfoItem("#MechJeb_LaytheMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Laythe Mapdist
		public EditableDouble LaytheMapdist = 1000;
		[EditableInfoItem("#MechJeb_PolMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Pol Mapdist
		public EditableDouble PolMapdist = 500;
		[EditableInfoItem("#MechJeb_BopMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Bop Mapdist
		public EditableDouble BopMapdist = 1000;
		[EditableInfoItem("#MechJeb_VallMapdist", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Vall Mapdist
		public EditableDouble VallMapdist = 5000;

		internal int selIndex = -1;
		internal int saveIndex = -1;
		internal string tmpRadius = "";
		internal string tmpMinSpeed = "";
		internal string tmpMaxSpeed = "";
		internal string tmpLat = "";
		internal string tmpLon = "";
	    internal const string coordRegEx = @"^([nsew])?\s*(-?\d+(?:\.\d+)?)(?:[°:\s]+(-?\d+(?:\.\d+)?))?(?:[':\s]+(-?\d+(?:\.\d+)?))?(?:[^nsew]*([nsew])?)?$";

	    private Vector2 scroll;
		private GUIStyle styleActive;
		private GUIStyle styleInactive;
		private GUIStyle styleQuicksave;
		private string titleAdd = "";
		private string saveName = "";
		private bool waitingForPick = false;
		private pages showPage = pages.waypoints;
		private enum pages { waypoints, settings, routes };
		private static MechJebRouteRenderer renderer;
		private Rect[] waypointRects = new Rect[0];
		private int lastIndex = -1;
		private int settingPageIndex = 0;
		private string[] settingPages = new string[] { "Rover", "Waypoints" };
//		private static LineRenderer redLine;
//		private static LineRenderer greenLine;

		public MechJebModuleWaypointWindow(MechJebCore core) : base(core) { }

		public override void OnStart(PartModule.StartState state)
		{
			hidden = true;
			ap = core.GetComputerModule<MechJebModuleRoverController>();
			if (HighLogic.LoadedSceneIsFlight && vessel.isActiveVessel) {
				renderer = MechJebRouteRenderer.AttachToMapView(core);
				renderer.enabled = enabled;
			}

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
//			MechJebRouteRenderer.NewLineRenderer(ref greenLine);
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
			base.OnLoad(local, type, global);

			ConfigNode wps = new ConfigNode("Routes");
			if (KSP.IO.File.Exists<MechJebCore>("mechjeb_routes.cfg"))
			{
				try
				{
					wps = ConfigNode.Load(KSP.IO.IOUtils.GetFilePathFor(core.GetType(), "mechjeb_routes.cfg"));
				}
				catch (Exception e)
				{
					Debug.LogError("MechJebModuleWaypointWindow.OnLoad caught an exception trying to load mechjeb_routes.cfg: " + e);
				}
			}

			if (wps.HasNode("Waypoints"))
			{
				Routes.Clear();
				foreach (ConfigNode cn in wps.GetNodes("Waypoints"))
				{
					Routes.Add(new MechJebWaypointRoute(cn));
				}
				Routes.Sort(SortRoutes);
			}
		}

		public void SaveRoutes()
		{
			var cn = new ConfigNode("Routes");

			if (Routes.Count > 0) {
				Routes.Sort(SortRoutes);
				foreach (MechJebWaypointRoute r in Routes) {
					cn.AddNode(r.ToConfigNode());
				}
			}

			cn.Save(KSP.IO.IOUtils.GetFilePathFor(core.GetType(), "mechjeb_routes.cfg"));
		}

		public override string GetName()
		{
			return Mode.ToString() + " Waypoints" + (titleAdd != "" ? " - " + titleAdd : "");
		}

		public static Coordinates GetMouseFlightCoordinates()
		{
			CelestialBody body = FlightGlobals.currentMainBody;
			Ray mouseRay = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit raycast;
//			mouseRay.origin = ScaledSpace.ScaledToLocalSpace(mouseRay.origin);
			Vector3d relOrigin = mouseRay.origin - body.position;
			Vector3d relSurfacePosition;
			if (Physics.Raycast(mouseRay, out raycast, (float)body.Radius * 4f, 1 << 15))
			{
				return new Coordinates(body.GetLatitude(raycast.point), MuUtils.ClampDegrees180(body.GetLongitude(raycast.point)));
			}
			else
			{
				double curRadius = body.pqsController.radiusMax;
				double lastRadius = 0;
				double error = 0;
				int loops = 0;
				float st = Time.time;
				while (loops < 50)
				{
					if (PQS.LineSphereIntersection(relOrigin, mouseRay.direction, curRadius, out relSurfacePosition))
					{
						Vector3d surfacePoint = body.position + relSurfacePosition;
						double alt = body.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(body.GetLongitude(surfacePoint), Vector3d.down) * QuaternionD.AngleAxis(body.GetLatitude(surfacePoint), Vector3d.forward) * Vector3d.right);
						error = Math.Abs(curRadius - alt);
						if (error < (body.pqsController.radiusMax - body.pqsController.radiusMin) / 100)
						{
							return new Coordinates(body.GetLatitude(surfacePoint), MuUtils.ClampDegrees180(body.GetLongitude(surfacePoint)));
						}
						else
						{
							lastRadius = curRadius;
							curRadius = alt;
							loops++;
						}
					}
					else
					{
						if (loops == 0)
						{
							break;
						}
						else
						{ // Went too low, needs to try higher
							curRadius = (lastRadius * 9 + curRadius) / 10;
							loops++;
						}
					}
				}
			}

			return null;

//			var cam = FlightCamera.fetch.mainCamera;
//			Ray ray = cam.ScreenPointToRay(Input.mousePosition);
////			greenLine.SetPosition(0, ray.origin);
////			greenLine.SetPosition(1, (Vector3d)ray.direction * body.Radius / 2);
////			if (Physics.Raycast(ray, out raycast, (float)body.Radius * 4f, ~(1 << 1))) {
//				Vector3d hit;
//				//body.pqsController.RayIntersection(ray.origin, ray.direction, out hit);
//				PQS.LineSphereIntersection(ray.origin - body.position, ray.direction, body.Radius, out hit);
//				if (hit != Vector3d.zero) {
//					hit = body.position + hit;
//					Vector3d start = ray.origin;
//					Vector3d end = hit;
//					Vector3d point = Vector3d.zero;
//					for (int i = 0; i < 16; i++) {
//						point = (start + end) / 2;
//						//var lat = body.GetLatitude(point);
//						//var lon = body.GetLongitude(point);
//						//var surf = body.GetWorldSurfacePosition(lat, lon, body.TerrainAltitude(lat, lon));
//						var alt = body.GetAltitude(point) - body.TerrainAltitude(point);
//						//Debug.Log(alt);
//						if (alt > 0) {
//							start = point;
//						}
//						else if (alt < 0) {
//							end = point;
//						}
//						else {
//							break;
//						}
//					}
//					hit = point;
////					redLine.SetPosition(0, ray.origin);
////					redLine.SetPosition(1, hit);
//					return new Coordinates(body.GetLatitude(hit), MuUtils.ClampDegrees180(body.GetLongitude(hit)));
//				}
//				else {
//					return null;
//				}
		}

		public static string LatToString(double Lat)
		{
			while (Lat >  90) { Lat -= 180; }
			while (Lat < -90) { Lat += 180; }

			string ns = (Lat >= 0 ? "N" : "S");
			Lat = Math.Abs(Lat);

			int h = (int)Lat;
			Lat -= h; Lat *= 60;

			int m = (int)Lat;
			Lat -= m; Lat *= 60;

			float s = (float)Lat;

			return string.Format("{0} {1}° {2}' {3:F3}\"", ns, h, m, s);
		}

		public static string LonToString(double Lon)
		{
			while (Lon >  180) { Lon -= 360; }
			while (Lon < -180) { Lon += 360; }

			string ew = (Lon >= 0 ? "E" : "W");
			Lon = Math.Abs(Lon);

			int h = (int)Lon;
			Lon -= h; Lon *= 60;

			int m = (int)Lon;
			Lon -= m; Lon *= 60;

			float s = (float)Lon;

			return string.Format("{0} {1}° {2}' {3:F3}\"", ew, h, m, s);
		}

		public static double ParseCoord(string LatLon, bool IsLongitute = false)
		{
			var match = new Regex(coordRegEx, RegexOptions.IgnoreCase).Match(LatLon);
			var range = (IsLongitute ? 180 : 90);

			float nsew = 1;
			if (match.Groups[5] != null)
			{
				if (match.Groups[5].Value.ToUpper() == "N" || match.Groups[5].Value.ToUpper() == "E") { nsew = 1; }
				else if (match.Groups[5].Value.ToUpper() == "S" || match.Groups[5].Value.ToUpper() == "W") { nsew = -1; }
				else if (match.Groups[1] != null) {
					if (match.Groups[1].Value.ToUpper() == "N" || match.Groups[1].Value.ToUpper() == "E") { nsew = 1; }
					else if (match.Groups[1].Value.ToUpper() == "S" || match.Groups[1].Value.ToUpper() == "W") { nsew = -1; }
				}
			}

			float h = 0;
			if (match.Groups[2] != null) { float.TryParse(match.Groups[2].Value, out h); }
			if (h < 0) { nsew *= -1; h *= -1; }

			float m = 0;
			if (match.Groups[3] != null) { float.TryParse(match.Groups[3].Value, out m); }

			float s = 0;
			if (match.Groups[4] != null) { float.TryParse(match.Groups[4].Value, out s); }

			h = (h + (m / 60) + (s / 3600)) * nsew;

			while (h >  range) { h -= range * 2; }
			while (h < -range) { h += range * 2; }

			return h;
		}

		private int SortRoutes(MechJebWaypointRoute a, MechJebWaypointRoute b)
		{
			var bn = string.Compare(a.Body.bodyName, b.Body.bodyName, true);
			if (bn != 0)
			{
				return bn;
			}
			else
			{
				return string.Compare(a.Name, b.Name, true);
			}
		}

		public MechJebWaypoint SelectedWaypoint() {
			return (selIndex > -1 ? ap.Waypoints[selIndex] : null);
		}

		public int SelectedWaypointIndex {
			get { return selIndex; }
			set { selIndex = value; }
		}

		public override GUILayoutOption[] WindowOptions()
		{
			return new GUILayoutOption[] { GUILayout.Width(500), GUILayout.Height(400) };
		}

		public void DrawPageWaypoints()
		{
			bool alt = Input.GetKey(KeyCode.LeftAlt);
			scroll = GUILayout.BeginScrollView(scroll);
			if (ap.Waypoints.Count > 0) {
				waypointRects = new Rect[ap.Waypoints.Count];
				GUILayout.BeginVertical();
				double eta = 0;
				double dist = 0;
				for (int i = 0; i < ap.Waypoints.Count; i++)
				{
					var wp = ap.Waypoints[i];
					var maxSpeed = (wp.MaxSpeed > 0 ? wp.MaxSpeed : ap.speed.val);
					var minSpeed = (wp.MinSpeed > 0 ? wp.MinSpeed : 0);
					if (MapView.MapIsEnabled && i == selIndex)
					{
						MuMech.GLUtils.DrawGroundMarker(mainBody, wp.Latitude, wp.Longitude, Color.red, true, (DateTime.Now.Second + DateTime.Now.Millisecond / 1000f) * 6, mainBody.Radius / 100);
					}
					if (i >= ap.WaypointIndex)
					{
						if (ap.WaypointIndex > -1)
						{
							eta += GuiUtils.FromToETA((i == ap.WaypointIndex ? vessel.CoM : (Vector3)ap.Waypoints[i - 1].Position), (Vector3)wp.Position, (ap.etaSpeed > 0.1 && ap.etaSpeed < maxSpeed ? (float)Math.Round(ap.etaSpeed, 1) : maxSpeed));
						}
						dist += Vector3.Distance((i == ap.WaypointIndex || (ap.WaypointIndex == -1 && i == 0) ? vessel.CoM : (Vector3)ap.Waypoints[i - 1].Position), (Vector3)wp.Position);
					}
					string str = string.Format("[{0}] - {1} - R: {2:F1} m\n       S: {3:F0} ~ {4:F0} - D: {5}m - ETA: {6}", i + 1, wp.GetNameWithCoords(), wp.Radius,
					                           minSpeed, maxSpeed, MuUtils.ToSI(dist, -1), GuiUtils.TimeToDHMS(eta));
					GUI.backgroundColor = (i == ap.WaypointIndex ? new Color(0.5f, 1f, 0.5f) : Color.white);
					if (GUILayout.Button(str, (i == selIndex ? styleActive : (wp.Quicksave ? styleQuicksave : styleInactive))))
					{
						if (alt)
						{
							ap.WaypointIndex = (ap.WaypointIndex == i ? -1 : i);
							// set current waypoint or unset it if it's already the current one
						}
						else
						{
							if (selIndex == i)
							{
								selIndex = -1;
							}
							else
							{
								selIndex = i;
								tmpRadius = wp.Radius.ToString();
								tmpMinSpeed = wp.MinSpeed.ToString();
								tmpMaxSpeed = wp.MaxSpeed.ToString();
								tmpLat = LatToString(wp.Latitude);
								tmpLon = LonToString(wp.Longitude);
							}
						}
					}

					if (Event.current.type == EventType.Repaint)
					{
						waypointRects[i] = GUILayoutUtility.GetLastRect();
						//if (i == ap.WaypointIndex) { Debug.Log(Event.current.type.ToString() + " - " + waypointRects[i].ToString() + " - " + scroll.ToString()); }
					}
					GUI.backgroundColor = Color.white;

					if (selIndex > -1 && selIndex == i)
					{
						GUILayout.BeginHorizontal();

						GUILayout.Label("  Radius: ", GUILayout.ExpandWidth(false));
						tmpRadius = GUILayout.TextField(tmpRadius, GUILayout.Width(50));
						float.TryParse(tmpRadius, out wp.Radius);
						if (GUILayout.Button("A", GUILayout.ExpandWidth(false))) { ap.Waypoints.GetRange(i, ap.Waypoints.Count).ForEach(fewp => fewp.Radius = wp.Radius); }

						GUILayout.Label("- Speed: ", GUILayout.ExpandWidth(false));
						tmpMinSpeed = GUILayout.TextField(tmpMinSpeed, GUILayout.Width(40));
						float.TryParse(tmpMinSpeed, out wp.MinSpeed);
						if (GUILayout.Button("A", GUILayout.ExpandWidth(false))) { ap.Waypoints.GetRange(i, ap.Waypoints.Count).ForEach(fewp => fewp.MinSpeed = wp.MinSpeed); }

						GUILayout.Label(" - ", GUILayout.ExpandWidth(false));
						tmpMaxSpeed = GUILayout.TextField(tmpMaxSpeed, GUILayout.Width(40));
						float.TryParse(tmpMaxSpeed, out wp.MaxSpeed);
						if (GUILayout.Button("A", GUILayout.ExpandWidth(false))) { ap.Waypoints.GetRange(i, ap.Waypoints.Count).ForEach(fewp => fewp.MaxSpeed = wp.MaxSpeed); }

						GUILayout.FlexibleSpace();
						if (GUILayout.Button("QS", (wp.Quicksave ? styleQuicksave : styleInactive), GUILayout.ExpandWidth(false)))
						{
							if (alt)
							{
								ap.Waypoints.GetRange(i, ap.Waypoints.Count).ForEach(fewp => fewp.Quicksave = !fewp.Quicksave);
							}
							else
							{
								wp.Quicksave = !wp.Quicksave;
							}
						}

						GUILayout.EndHorizontal();


						GUILayout.BeginHorizontal();

						GUILayout.Label("Lat ", GUILayout.ExpandWidth(false));
						tmpLat = GUILayout.TextField(tmpLat, GUILayout.Width(125));
						wp.Latitude = ParseCoord(tmpLat);

						GUILayout.Label(" -  Lon ", GUILayout.ExpandWidth(false));
						tmpLon = GUILayout.TextField(tmpLon, GUILayout.Width(125));
						wp.Longitude = ParseCoord(tmpLon, true);

						GUILayout.EndHorizontal();
					}
				}
				titleAdd = "Distance: " + MuUtils.ToSI(dist, -1) + "m - ETA: " + GuiUtils.TimeToDHMS(eta);
				GUILayout.EndVertical();
			}
			else
			{
				titleAdd = "";
			}
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button(alt ? "Reverse" : (!waitingForPick ? "Add Waypoint" : "Abort Adding"), GUILayout.Width(110)))
			{
				if (alt)
				{
					ap.Waypoints.Reverse();
					if (ap.WaypointIndex > -1) { ap.WaypointIndex = ap.Waypoints.Count - 1 - ap.WaypointIndex; }
					if (selIndex > -1) { selIndex = ap.Waypoints.Count - 1 - selIndex; }
				}
				else
				{
					if (!waitingForPick)
					{
						waitingForPick = true;
						if (MapView.MapIsEnabled)
						{
							core.target.Unset();
							core.target.PickPositionTargetOnMap();
						}
					}
					else
					{
						waitingForPick = false;
					}
				}
			}
			if (GUILayout.Button((alt ? "Clear" : "Remove"), GUILayout.Width(65)) && selIndex >= 0 && ap.Waypoints.Count > 0)
			{
				if (alt)
				{
					ap.WaypointIndex = -1;
					ap.Waypoints.Clear();
				}
				else
				{
					ap.Waypoints.RemoveAt(selIndex);
					if (ap.WaypointIndex > selIndex) { ap.WaypointIndex--; }
				}
			    selIndex = -1;
				//if (ap.WaypointIndex >= ap.Waypoints.Count) { ap.WaypointIndex = ap.Waypoints.Count - 1; }
			}
			if (GUILayout.Button((alt ? "Top" : "Up"), GUILayout.Width(57)) && selIndex > 0 && selIndex < ap.Waypoints.Count && ap.Waypoints.Count >= 2)
            {
                if (alt)
                {
                    var t = ap.Waypoints[selIndex];
                    ap.Waypoints.RemoveAt(selIndex);
                    ap.Waypoints.Insert(0, t);
                    selIndex = 0;
                }
                else
                {
                    ap.Waypoints.Swap(selIndex, --selIndex);
                }
            }
            if (GUILayout.Button((alt ? "Bottom" : "Down"), GUILayout.Width(57)) && selIndex >= 0 && selIndex < ap.Waypoints.Count - 1 && ap.Waypoints.Count >= 2)
            {
                if (alt)
                {
                    var t = ap.Waypoints[selIndex];
                    ap.Waypoints.RemoveAt(selIndex);
                    ap.Waypoints.Add(t);
                    selIndex = ap.Waypoints.Count - 1;
                }
                else
                {
                    ap.Waypoints.Swap(selIndex, ++selIndex);
                }
            }
			if (GUILayout.Button("Routes"))
			{
				showPage = pages.routes;
				scroll = Vector2.zero;
			}
			if (GUILayout.Button("Settings"))
			{
				showPage = pages.settings;
				scroll = Vector2.zero;
			}
			GUILayout.EndHorizontal();

			if (selIndex >= ap.Waypoints.Count) { selIndex = -1; }
			if (selIndex == -1 && ap.WaypointIndex > -1 && lastIndex != ap.WaypointIndex && waypointRects.Length > 0)
			{
				scroll.y = waypointRects[ap.WaypointIndex].y - 160;
			}
			lastIndex = ap.WaypointIndex;
		}

		public void DrawPageSettings()
		{
			bool alt = Input.GetKey(KeyCode.LeftAlt);
			titleAdd = "Settings";
			MechJebModuleCustomWindowEditor ed = core.GetComputerModule<MechJebModuleCustomWindowEditor>();
			if (!ap.enabled) { ap.CalculateTraction(); } // keep calculating traction just for displaying it

			scroll = GUILayout.BeginScrollView(scroll);

			settingPageIndex = GUILayout.SelectionGrid(settingPageIndex, settingPages, settingPages.Length);

			switch (settingPageIndex)
			{
				case 0:
					GUILayout.BeginHorizontal();

					GUILayout.BeginVertical();
					ed.registry.Find(i => i.id == "Editable:RoverController.hPIDp").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.hPIDi").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.hPIDd").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.terrainLookAhead").DrawItem();
		//			ed.registry.Find(i => i.id == "Value:RoverController.speedIntAcc").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.tractionLimit").DrawItem();
					ed.registry.Find(i => i.id == "Toggle:RoverController.LimitAcceleration").DrawItem();
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					ed.registry.Find(i => i.id == "Editable:RoverController.sPIDp").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.sPIDi").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.sPIDd").DrawItem();
					ed.registry.Find(i => i.id == "Editable:RoverController.turnSpeed").DrawItem();
					ed.registry.Find(i => i.id == "Value:RoverController.traction").DrawItem();
					GUILayout.EndVertical();

					GUILayout.EndHorizontal();
					break;

				case 1:
					GUILayout.BeginHorizontal();

					GUILayout.BeginVertical();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.MohoMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.EveMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.GillyMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.KerbinMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.MunMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.MinmusMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.DunaMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.IkeMapdist").DrawItem();
					GUILayout.EndVertical();

					GUILayout.BeginVertical();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.DresMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.JoolMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.LaytheMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.VallMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.TyloMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.BopMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.PolMapdist").DrawItem();
					ed.registry.Find(i => i.id == "Editable:WaypointWindow.EelooMapdist").DrawItem();
					GUILayout.EndVertical();

					GUILayout.EndHorizontal();
					break;
			}

			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Waypoints"))
			{
				showPage = pages.waypoints;
				scroll = Vector2.zero;
				lastIndex = -1;
			}
			if (GUILayout.Button("Routes"))
			{
				showPage = pages.routes;
				scroll = Vector2.zero;
			}
			if (GUILayout.Button("Help"))
			{
				core.GetComputerModule<MechJebModuleWaypointHelpWindow>().enabled = !core.GetComputerModule<MechJebModuleWaypointHelpWindow>().enabled;
			}
			GUILayout.EndHorizontal();
		}

		public void DrawPageRoutes()
		{
			bool alt = Input.GetKey(KeyCode.LeftAlt);
			titleAdd = "Routes for " + vessel.mainBody.bodyName;

			scroll = GUILayout.BeginScrollView(scroll);
			var bodyWPs = Routes.FindAll(r => r.Body == vessel.mainBody && r.Mode == Mode.ToString());
			for (int i = 0; i < bodyWPs.Count; i++)
			{
				GUILayout.BeginHorizontal();
				var str = bodyWPs[i].Name + " - " + bodyWPs[i].Stats;
				if (GUILayout.Button(str, (i == saveIndex ? styleActive : styleInactive)))
				{
					saveIndex = (saveIndex == i ? -1 : i);
				}
				if (i == saveIndex)
				{
					if (GUILayout.Button("Delete", GUILayout.Width(70)))
					{
						Routes.RemoveAll(r => r.Name == bodyWPs[i].Name && r.Body == vessel.mainBody && r.Mode == Mode.ToString());
						saveIndex = -1;
					}
				}
				GUILayout.EndHorizontal();
			}
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			saveName = GUILayout.TextField(saveName, GUILayout.Width(150));
			if (GUILayout.Button("Save", GUILayout.Width(50)))
			{
				if (saveName != "" && ap.Waypoints.Count > 0)
				{
					var old = Routes.Find(r => r.Name == saveName && r.Body == vessel.mainBody && r.Mode == Mode.ToString());
					var wps = new MechJebWaypointRoute(saveName, vessel.mainBody);
					ap.Waypoints.ForEach(wp => wps.Add(wp));
					if (old == null)
					{
						Routes.Add(wps);
					}
					else
					{
						Routes[Routes.IndexOf(old)] = wps;
					}
					Routes.Sort(SortRoutes);
					SaveRoutes();
				}
			}
			if (GUILayout.Button((alt ? "Add" : "Load"), GUILayout.Width(50)))
			{
				if (saveIndex > -1)
				{
					if (!alt)
					{
						ap.WaypointIndex = -1;
						ap.Waypoints.Clear();
					}
					Routes[saveIndex].ForEach(wp => ap.Waypoints.Add(wp));
				}
			}
			if (GUILayout.Button("Waypoints"))
			{
				showPage = pages.waypoints;
				scroll = Vector2.zero;
				lastIndex = -1;
			}
			if (GUILayout.Button("Settings"))
			{
				showPage = pages.settings;
				scroll = Vector2.zero;
			}
			GUILayout.EndHorizontal();
		}

		protected override void WindowGUI(int windowID)
		{
			if (GUI.Button(new Rect(windowPos.width - 48, 0, 13, 20), "?", GuiUtils.yellowOnHover))
			{
				var help = core.GetComputerModule<MechJebModuleWaypointHelpWindow>();
				switch (showPage)
				{
						case pages.waypoints: help.selTopic = ((IList)help.topics).IndexOf("Waypoints"); break;
						case pages.settings: help.selTopic = ((IList)help.topics).IndexOf("Settings"); break;
						case pages.routes: help.selTopic = ((IList)help.topics).IndexOf("Routes"); break;
				}
				help.enabled = help.selTopic > -1 || help.enabled;
			}

			if (styleInactive == null)
			{
				styleInactive = new GUIStyle(GuiUtils.skin != null ? GuiUtils.skin.button : GuiUtils.defaultSkin.button);
				styleInactive.alignment = TextAnchor.UpperLeft;
			}
			if (styleActive == null)
			{
				styleActive = new GUIStyle(styleInactive);
				styleActive.active.textColor = styleActive.focused.textColor = styleActive.hover.textColor = styleActive.normal.textColor = Color.green;
			} // for some reason MJ's skin sometimes isn't loaded at OnStart so this has to be done here
			if (styleQuicksave == null)
			{
				styleQuicksave = new GUIStyle(styleActive);
				styleQuicksave.active.textColor = styleQuicksave.focused.textColor = styleQuicksave.hover.textColor = styleQuicksave.normal.textColor = Color.yellow;
			}

			bool alt = Input.GetKey(KeyCode.LeftAlt);

			switch (showPage)
			{
					case pages.waypoints: DrawPageWaypoints(); break;
					case pages.settings: DrawPageSettings(); break;
					case pages.routes: DrawPageRoutes(); break;
			}

			if (waitingForPick && vessel.isActiveVessel && Event.current.type == EventType.Repaint)
			{
				if (MapView.MapIsEnabled)
				{
					if (core.target.pickingPositionTarget == false)
					{
						if (core.target.PositionTargetExists)
						{
							if (selIndex > -1 && selIndex < ap.Waypoints.Count)
							{
								ap.Waypoints.Insert(selIndex, new MechJebWaypoint(core.target.GetPositionTargetPosition()));
								tmpRadius = ap.Waypoints[selIndex].Radius.ToString();
								tmpLat = LatToString(ap.Waypoints[selIndex].Latitude);
								tmpLon = LonToString(ap.Waypoints[selIndex].Longitude);
							}
							else
							{
								ap.Waypoints.Add(new MechJebWaypoint(core.target.GetPositionTargetPosition()));
							}
							core.target.Unset();
							waitingForPick = alt;
						}
						else
						{
							core.target.PickPositionTargetOnMap();
						}
					}
				}
				else
				{
					if (!GuiUtils.MouseIsOverWindow(core))
					{
						Coordinates mouseCoords = GetMouseFlightCoordinates();
						if (mouseCoords != null)
						{
							if (Input.GetMouseButtonDown(0))
							{
								if (selIndex > -1 && selIndex < ap.Waypoints.Count)
								{
									ap.Waypoints.Insert(selIndex, new MechJebWaypoint(mouseCoords.latitude, mouseCoords.longitude));
									tmpRadius = ap.Waypoints[selIndex].Radius.ToString();
									tmpLat = LatToString(ap.Waypoints[selIndex].Latitude);
									tmpLon = LonToString(ap.Waypoints[selIndex].Longitude);
								}
								else
								{
									ap.Waypoints.Add(new MechJebWaypoint(mouseCoords.latitude, mouseCoords.longitude));
								}
								waitingForPick = alt;
							}
						}
					}
				}
			}

			base.WindowGUI(windowID);
		}

		public override void OnFixedUpdate()
		{
			if (vessel.isActiveVessel && (renderer == null || renderer.ap != ap)) { MechJebRouteRenderer.AttachToMapView(core); } //MechJebRouteRenderer.AttachToMapView(core); }
			ap.Waypoints.ForEach(wp => wp.Update());
//			float scale = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, vessel.CoM) / 900f;
//			greenLine.SetPosition(0, vessel.CoM);
//			greenLine.SetPosition(1, vessel.CoM + ap.norm * 5);
//			greenLine.SetWidth(scale + 0.1f, scale + 0.1f);
			base.OnFixedUpdate();
		}
	}

	public class MechJebModuleWaypointHelpWindow : DisplayModule
	{
		public int selTopic = 0;
		public string[] topics = {"Rover Controller", "Waypoints", "Routes", "Settings"};
		string selSubTopic = "";
		GUIStyle btnActive;
		GUIStyle btnInactive;

		void HelpTopic(string title, string text)
		{
			GUILayout.BeginVertical();
			if (GUILayout.Button(title, (selSubTopic == title ? btnActive : btnInactive)))
			{
				selSubTopic = (selSubTopic != title ? title : "");
				windowPos = new Rect(windowPos.x, windowPos.y, windowPos.width, 0);
			}
			if (selSubTopic == title)
			{
				GUILayout.Label(text);
			}
			GUILayout.EndVertical();
		}

		public MechJebModuleWaypointHelpWindow(MechJebCore core) : base(core) { }

		public override string GetName()
		{
            return Localizer.Format("#MechJeb_Waypointhelper_title");//"Waypoint Help"
		}

		public override void OnStart(PartModule.StartState state)
		{
			hidden = true;
			base.OnStart(state);
		}

		protected override void WindowGUI(int windowID)
		{
			if (btnInactive == null)
			{
				btnInactive = new GUIStyle(GuiUtils.skin.button);
				btnInactive.alignment = TextAnchor.MiddleLeft;
			}

			if (btnActive == null)
			{
				btnActive = new GUIStyle(btnInactive);
				btnActive.active.textColor = btnActive.hover.textColor = btnActive.focused.textColor = btnActive.normal.textColor = Color.green;
			}

		 	selTopic = GUILayout.SelectionGrid(selTopic, topics, topics.Length);

		 	switch (topics[selTopic])
		 	{
                case "Rover Controller":
		 			HelpTopic("Holding a set Heading", "To hold a specific heading just tick the box next to 'Heading control' and the autopilot will try to keep going for the entered heading." +
		 			          "\nThis also needs to be enabled when the autopilot is supposed to drive to a waypoint" +
		 			          "'Heading Error' simply shows the error between current heading and target heading.");
		 			HelpTopic("Holding a set Speed", "To hold a specific speed just tick the box next to 'Speed control' and the autopilot will try to keep going at the entered speed." +
		 			          "\nThis also needs to be enabled when the autopilot is supposed to drive to a waypoint" +
		 			          "'Speed Error' simply shows the error between current speed and target speed.");
		 			HelpTopic("More stability while driving and nice landings", "If you turn on 'Stability Control' then the autopilot will use the reaction wheel's torque to keep the rover aligned with the surface." +
		 			          "\nThis means that if you make a jump the autopilot will try to align the rover in the best possible way to land as straight and flat as possible given the available time and torque." +
		 			          "\nBe aware that this doesn't make your rover indestructible, only relatively unlikely to land in a bad way." +
		 			          "\n\n'Stability Control' will also limit the brake to reduce the chances of flipping over from too much braking power." +
		 			          "\nSee 'Settings' -> 'Traction and Braking'. This setting is also saved per vessel.");
		 			HelpTopic("Brake on Pilot Eject", "With this option enabled the rover will stop if the pilot (on manned rovers) should get thrown out of his seat.");
		 			HelpTopic("Target Speed", "Current speed the autopilot tries to achieve.");
		 			HelpTopic("Waypoint Index", "Overview of waypoints and which the autopilot is currently driving to.");
		 			HelpTopic("Button 'Waypoints'", "Opens the waypoint list to set up a route.");
		 			HelpTopic("Button 'Follow' / 'Stop'", "This sets the autopilot to drive along the set route starting at the first waypoint. Only visible when atleast one waypoint is set." +
		 			          "\n\nAlt click will set the autopilot to 'Loop Mode' which will make it go for the first waypoint again after reaching the last." +
		 			          "If the only waypoint happens to be a target it will keep following that instead of only going to it once." +
		 			          "\n\nIf the autopilot is already active the 'Follow' button will turn into the 'Stop' button which will obviously stop it when pressed.");
		 			HelpTopic("Button 'To Target'", "Clears the route, adds the target as only waypoint and starts the autopilot. Only visible with a selected target." +
		 			          "\n\nAlt click will set the autopilot to 'Loop Mode' which will make it continue to follow the target, pausing when near it instead of turning off then.");
		 			HelpTopic("Button 'Add Target'", "Adds the selected target as a waypoint either at the end of the route or before the selected waypoint. Only visible with a selected target.");
		 			break;

		 		case "Waypoints":
		 			HelpTopic("Adding Waypoints", "Adds a new waypoint to the route at the end or before the currently selected waypoint, " +
		 			          "simply click the terrain or somewhere on the body in Mapview." +
		 			          "\n\nAlt clicking will reverse the route for easier going back and holding Alt while clicking the terrain or body in Mapview will allow to add more waypoints without having to click the button again.");
		 			HelpTopic("Removing Waypoints", "Removes the currently selected waypoint." +
		 			          "\n\nAlt clicking will remove all waypoints.");
		 			HelpTopic("Reordering Waypoints", "'Up' and 'Down' will move the selected waypoint up or down in the list, Alt clicking will move it to the top or bottom respectively.");
		 			HelpTopic("Waypoint Radius", "Radius defines the distance to the center of the waypoint after which the waypoint will be considered 'reached'." +
		 			          "\n\nA radius of 5m (default) simply means that when you're 5m from the waypoint away the autopilot will jump to the next or turn off if it was the last." +
		 			          "\n\nThe 'A' button behind the textfield will set the entered radius for all waypoints.");
		 			HelpTopic("Speedlimits", "The two speed textfields represent the minimum and maximum speed for the waypoint." +
		 			          "\n\nThe maximum speed is the speed the autopilot tries to reach to get to the waypoint." +
		 			          "\n\nThe minimum speed was before used to set the speed with which the autopilot will go through the waypoint, but that got reworked now to be based on the next waypoint's max. speed and the turn needed at the waypoint." +
		 			          "\n\nI have no idea what this will currently do if set so better just leave it at 0..." +
		 			          "\n\nThe 'A' buttons set their respective speed for all waypoints.");
		 			HelpTopic("Quicksaving at a Waypoint", "Clicking the 'QS' button will turn on QuickSave for that waypoint." +
		 			          "\n\nThis will make the autopilot stop and try to quicksave at that waypoint and then continue. A QuickSave waypoint has yellow text instead of white." +
		 			          "\n\nSmall sideeffect: leaving the throttle up will prevent the saving from occurring effectively pausing the autopilot at that point until interefered with. (Discovered by Greys)" +
		 			          "\n\nAlt clicking will toggle QS for all waypoints including the clicked one.");
		 			HelpTopic("Changing the current target Waypoint", "Alt clicking a waypoint will mark it as the current target waypoint. The active waypoint has a green tinted background.");
		 			break;

		 		case "Routes":
		 			HelpTopic("Routes Help", "The empty textfield is for saving routes, enter a name there before clicking 'Save'." +
		 			          "\n\nTo load a route simply select one from the list and click 'Load'." +
		 			          "\n\nTo delete a route simply select it and a 'Delete' button will appear right of it.");
		 			break;

		 		case "Settings":
		 			HelpTopic("Heading / Speed PID", "These parameters control the behaviour of the heading's / speed's PID. Saved globally so NO TOUCHING unless you know what you're doing (or atleast know how to write down numbers to restore it if you mess up)");
		 			HelpTopic("Safe Turn Speed", "'Safe Turn Speed' tells the autopilot which speed the rover can usually go full turn through corners without tipping over." +
		 			          "\n\nGiven how differently terrain can be and other influences you can just leave it at 3 m/s but if you're impatient or just want to experiment feel free to test around. Saved per vessel type (same named vessels will share the setting).");
		 			HelpTopic("Traction and Braking", "'Traction' shows in % how many wheels have ground contact." +
		 			          "\n'Traction Brake Limit' defines what traction is atleast needed for the autopilot to still apply the brakes (given 'Stability Control' is active) even if you hold the brake down." +
		 			          "\nThis means the default setting of 75 will make it brake only if atleast 3 wheels have ground contact." +
		 			          "\n'Traction Brake Limit' is saved per vessel type." +
		 			          "\n\nIf you have 'Stability Control' off then it won't take care of your brake and you can flip as much as you want.");
		 			HelpTopic("Changing the route height in Mapview", "These values define offsets for the route height in Mapview. Given how weird it's set up it can be that they are too high or too low so I added these for easier adjusting. Saved globally, I think.");
		 			break;
		 	}

			base.WindowGUI(windowID);
		}
	}

	public class MechJebRouteRenderer : MonoBehaviour
	{
		public static readonly Material material = new Material (Shader.Find ("Legacy Shaders/Particles/Additive"));
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

		public static MechJebRouteRenderer AttachToMapView(MechJebCore Core)
		{
			var renderer = MapView.MapCamera.gameObject.GetComponent<MechJebRouteRenderer>();
			if (!renderer)
			{
				renderer = MapView.MapCamera.gameObject.AddComponent<MechJebRouteRenderer>();
			}
			renderer.ap = Core.GetComputerModule<MechJebModuleRoverController>();
			return renderer;
		}

		public static bool NewLineRenderer(ref LineRenderer line)
		{
			if (line != null) { return false; }
			GameObject obj = new GameObject("LineRenderer");
			line = obj.AddComponent<LineRenderer>();
			line.useWorldSpace = true;
			line.material = material;
		    line.startWidth = 10.0f;
		    line.endWidth = 10.0f;
		    line.positionCount = 2;
			return true;
		}

		public static Vector3 RaisePositionOverTerrain(Vector3 Position, float HeightOffset)
		{
			var body = FlightGlobals.ActiveVessel.mainBody;
			if (MapView.MapIsEnabled)
			{
				var lat = body.GetLatitude(Position);
				var lon = body.GetLongitude(Position);
				return ScaledSpace.LocalToScaledSpace(body.position + (body.Radius + HeightOffset + body.TerrainAltitude(lat, lon)) * body.GetSurfaceNVector(lat, lon));
			}
			else {
				return body.GetWorldSurfacePosition(body.GetLatitude(Position), body.GetLongitude(Position), body.GetAltitude(Position) + HeightOffset);
			}
		}

		public new bool enabled
		{
			get { return base.enabled; }
			set
			{
				base.enabled = value;
				if (pastPath != null) { pastPath.enabled = value; }
				if (currPath != null) { currPath.enabled = value; }
				if (nextPath != null) { nextPath.enabled = value; }
			}
		}

		public void OnPreRender()
		{
			if (NewLineRenderer(ref pastPath)) { pastPath.startColor = pastPathColor; pastPath.endColor=pastPathColor; }
			if (NewLineRenderer(ref currPath)) { currPath.startColor = currPathColor; currPath.endColor=currPathColor; }
			if (NewLineRenderer(ref nextPath)) { nextPath.startColor = nextPathColor; nextPath.endColor=nextPathColor; }
			if (NewLineRenderer(ref selWP))    { selWP.startColor = selWPColor; selWP.endColor=selWPColor; }

			//Debug.Log(ap.vessel.vesselName);
			var window = ap.core.GetComputerModule<MechJebModuleWaypointWindow>();
			switch (ap.vessel.mainBody.bodyName)
			{
					case "Moho" : addHeight = window.MohoMapdist; break;
					case "Eve" : addHeight = window.EveMapdist; break;
					case "Gilly" : addHeight = window.GillyMapdist; break;
					case "Kerbin" : addHeight = window.KerbinMapdist; break;
					case "Mun" : addHeight = window.MunMapdist; break;
					case "Minmus" : addHeight = window.MinmusMapdist; break;
					case "Duna" : addHeight = window.DunaMapdist; break;
					case "Ike" : addHeight = window.IkeMapdist; break;
					case "Dres" : addHeight = window.DresMapdist; break;
					case "Jool" : addHeight = window.JoolMapdist; break;
					case "Laythe" : addHeight = window.LaytheMapdist; break;
					case "Vall" : addHeight = window.VallMapdist; break;
					case "Tylo" : addHeight = window.TyloMapdist; break;
					case "Bop" : addHeight = window.BopMapdist; break;
					case "Pol" : addHeight = window.PolMapdist; break;
					case "Eeloo" : addHeight = window.EelooMapdist; break;
					default: addHeight = window.KerbinMapdist; break;
			}

			if (ap != null && ap.Waypoints.Count > 0 && ap.vessel.isActiveVessel && HighLogic.LoadedSceneIsFlight)
			{
				float targetHeight = (MapView.MapIsEnabled ? (float)addHeight : 3f);
				float scale = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, ap.vessel.CoM) / 700f;
				float width = (MapView.MapIsEnabled ? (float)(0.005 * PlanetariumCamera.fetch.Distance) : scale + 0.1f);
				//float width = (MapView.MapIsEnabled ? (float)mainBody.Radius / 10000 : 1);

				pastPath.startWidth = width;
			    pastPath.endWidth = width;
				currPath.startWidth = width;
			    currPath.endWidth = width;
				nextPath.startWidth = width;
			    nextPath.endWidth = width;
				selWP.gameObject.layer = pastPath.gameObject.layer = currPath.gameObject.layer = nextPath.gameObject.layer = (MapView.MapIsEnabled ? 9 : 0);

				int sel = ap.core.GetComputerModule<MechJebModuleWaypointWindow>().selIndex;
				selWP.enabled = sel > -1 && !MapView.MapIsEnabled;
				if (selWP.enabled)
				{
					float w = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, ap.Waypoints[sel].Position) / 600f + 0.1f;
					selWP.startWidth = 0;
				    selWP.endWidth = w * 10f;
					selWP.SetPosition(0, RaisePositionOverTerrain(ap.Waypoints[sel].Position, targetHeight + 3f));
					selWP.SetPosition(1, RaisePositionOverTerrain(ap.Waypoints[sel].Position, targetHeight + 3f + w * 15f));
				}

				if (ap.WaypointIndex > 0)
				{
//					Debug.Log("drawing pastPath");
					pastPath.enabled = true;
				    pastPath.positionCount = ap.WaypointIndex + 1;
					for (int i = 0; i < ap.WaypointIndex; i++)
					{
//						Debug.Log("vert " + i.ToString());
						pastPath.SetPosition(i, RaisePositionOverTerrain(ap.Waypoints[i].Position, targetHeight));
					}
					pastPath.SetPosition(ap.WaypointIndex, RaisePositionOverTerrain(ap.vessel.CoM, targetHeight));
//					Debug.Log("pastPath drawn");
				}
				else
				{
//					Debug.Log("no pastPath");
					pastPath.enabled = false;
				}

				if (ap.WaypointIndex > -1)
				{
//					Debug.Log("drawing currPath");
					currPath.enabled = true;
					currPath.SetPosition(0, RaisePositionOverTerrain(ap.vessel.CoM, targetHeight));
					currPath.SetPosition(1, RaisePositionOverTerrain(ap.Waypoints[ap.WaypointIndex].Position, targetHeight));
//					Debug.Log("currPath drawn");
				}
				else
				{
//					Debug.Log("no currPath");
					currPath.enabled = false;
				}

				var nextCount = ap.Waypoints.Count - ap.WaypointIndex;
				if (nextCount > 1)
				{
//					Debug.Log("drawing nextPath of " + nextCount + " verts");
					nextPath.enabled = true;
				    nextPath.positionCount = nextCount;
					nextPath.SetPosition(0, RaisePositionOverTerrain((ap.WaypointIndex == -1 ? ap.vessel.CoM : (Vector3)ap.Waypoints[ap.WaypointIndex].Position), targetHeight));
					for (int i = 0; i < nextCount - 1; i++)
					{
//						Debug.Log("vert " + i.ToString() + " (" + (ap.WaypointIndex + 1 + i).ToString() + ")");
						nextPath.SetPosition(i + 1, RaisePositionOverTerrain(ap.Waypoints[ap.WaypointIndex + 1 + i].Position, targetHeight));
					}
//					Debug.Log("nextPath drawn");
				}
				else
				{
//					Debug.Log("no nextPath");
					nextPath.enabled = false;
				}
			}
			else
			{
				//Debug.Log("moo");
				selWP.enabled = pastPath.enabled = currPath.enabled = nextPath.enabled = false;
			}
		}
	}
}
