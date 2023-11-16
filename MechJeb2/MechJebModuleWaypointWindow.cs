using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using KSP.IO;
using KSP.Localization;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebWaypoint
    {
        private const float    DEFAULT_RADIUS = 5;
        public        double   Latitude;
        public        double   Longitude;
        public        double   Altitude;
        public        Vector3d Position;
        public        float    Radius;

        [UsedImplicitly]
        public string Name;

        public readonly Vessel Target;
        public          float  MinSpeed;
        public          float  MaxSpeed;
        public          bool   Quicksave;

        public CelestialBody Body => Target != null ? Target.mainBody : FlightGlobals.ActiveVessel.mainBody;

        public MechJebWaypoint(double latitude, double longitude, float radius = DEFAULT_RADIUS, string name = "", float minSpeed = 0,
            float maxSpeed = 0)
        {
            Latitude  = latitude;
            Longitude = longitude;
            Radius    = radius;
            Name      = name ?? "";
            MinSpeed  = minSpeed;
            MaxSpeed  = maxSpeed;
            Update();
        }

        public MechJebWaypoint(Vector3d position, float radius = DEFAULT_RADIUS, string name = "", float minSpeed = 0, float maxSpeed = 0)
        {
            Latitude  = Body.GetLatitude(position);
            Longitude = Body.GetLongitude(position);
            Radius    = radius;
            Name      = name ?? "";
            MinSpeed  = minSpeed;
            MaxSpeed  = maxSpeed;
            Update();
        }

        public MechJebWaypoint(Vessel target, float radius = DEFAULT_RADIUS, string name = "", float minSpeed = 0, float maxSpeed = 0)
        {
            Target   = target;
            Radius   = radius;
            Name     = name ?? "";
            MinSpeed = minSpeed;
            MaxSpeed = maxSpeed;
            Update();
        }

        public MechJebWaypoint(ConfigNode node)
        {
            if (node.HasValue("Latitude")) { double.TryParse(node.GetValue("Latitude"), out Latitude); }

            if (node.HasValue("Longitude")) { double.TryParse(node.GetValue("Longitude"), out Longitude); }

            Target = node.HasValue("Target") ? FlightGlobals.Vessels.Find(v => v.id.ToString() == node.GetValue("Target")) : null;
            if (node.HasValue("Radius")) { float.TryParse(node.GetValue("Radius"), out Radius); }
            else { Radius = DEFAULT_RADIUS; }

            Name = node.HasValue("Name") ? node.GetValue("Name") : "";
            if (node.HasValue("MinSpeed")) { float.TryParse(node.GetValue("MinSpeed"), out MinSpeed); }

            if (node.HasValue("MaxSpeed")) { float.TryParse(node.GetValue("MaxSpeed"), out MaxSpeed); }

            if (node.HasValue("Quicksave")) { bool.TryParse(node.GetValue("Quicksave"), out Quicksave); }

            Update();
        }

        public ConfigNode ToConfigNode()
        {
            var cn = new ConfigNode("Waypoint");
            if (!(Target is null))
            {
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

        public string GetNameWithCoords() => (Name != "" ? Name : Target is null ? "Waypoint" : Target.vesselName) + " - " +
                                             Coordinates.ToStringDMS(Latitude, Longitude);

        //				((Latitude >= 0 ? "N " : "S ") + Math.Abs(Math.Round(Latitude, 3)) + ", " + (Longitude >= 0 ? "E " : "W ") + Math.Abs(Math.Round(Longitude, 3)));
        public void Update()
        {
            if (Target is null)
            {
                Position = Body.GetWorldSurfacePosition(Latitude, Longitude, Body.TerrainAltitude(Latitude, Longitude));
                if (Vector3d.Distance(Position, FlightGlobals.ActiveVessel.CoM) < 200)
                {
                    Vector3d dir = (Position - Body.position).normalized;
                    Vector3d rayPos = Body.position + dir * (Body.Radius + 50000);
                    dir = (Body.position - rayPos).normalized;
                    bool raycast = Physics.Raycast(rayPos, dir, out RaycastHit hit, (float)Body.Radius, 1 << 15, QueryTriggerInteraction.Ignore);
                    if (raycast)
                    {
                        dir      = hit.point - Body.position;
                        Position = Body.position + dir.normalized * (dir.magnitude + 0.5);
//						Latitude = Body.GetLatitude(Position);
//						Longitude = Body.GetLongitude(Position);
                    }
                }
            }
            else
            {
                Position  = Target.CoM;
                Latitude  = Body.GetLatitude(Position);
                Longitude = Body.GetLongitude(Position);
            }

            if (MinSpeed > 0 && MaxSpeed > 0 && MinSpeed > MaxSpeed)
                MinSpeed = MaxSpeed;
            else if (MinSpeed > 0 && MaxSpeed > 0 && MaxSpeed < MinSpeed)
                MaxSpeed = MinSpeed;
        }
    }

    public class MechJebWaypointRoute : List<MechJebWaypoint>
    {
        public readonly string Name;

        public CelestialBody Body { get; }

        public string Mode { get; }

        private string _stats;

        public string Stats
        {
            get
            {
                UpdateStats(); // recalculation of the stats all the time are fine according to Majiir and Fractal_UK :3
                return _stats;
            }
        }

        public ConfigNode ToConfigNode()
        {
            var cn = new ConfigNode("Waypoints");
            cn.AddValue("Name", Name);
            cn.AddValue("Body", Body.bodyName);
            cn.AddValue("Mode", Mode);
            ForEach(wp => cn.AddNode(wp.ToConfigNode()));
            return cn;
        }

        private void UpdateStats()
        {
            float distance = 0;
            if (Count > 1)
            {
                for (int i = 1; i < Count; i++)
                {
                    distance += Vector3.Distance(this[i - 1].Position, this[i].Position);
                }
            }

            _stats = $"{Count} waypoints over {distance.ToSI(-1)}m";
        }

        public MechJebWaypointRoute(string name = "", CelestialBody body = null, string mode = "Rover")
        {
            Name = name;
            Body = body != null ? body : FlightGlobals.currentMainBody;
            Mode = mode;
        }

        public MechJebWaypointRoute(ConfigNode node)
        {
            // feed this "Waypoints" nodes, just not the local ones of a ship
            if (node == null) { return; }

            Name = node.HasValue("Name") ? node.GetValue("Name") : "";
            Body = node.HasValue("Body") ? FlightGlobals.Bodies.Find(b => b.bodyName == node.GetValue("Body")) : null;
            Mode = node.HasValue("Mode") ? node.GetValue("Mode") : "Rover";
            if (node.HasNode("Waypoint"))
            {
                foreach (ConfigNode cn in node.GetNodes("Waypoint"))
                {
                    Add(new MechJebWaypoint(cn));
                }
            }
        }
    }

    [UsedImplicitly]
    public class MechJebModuleWaypointWindow : DisplayModule
    {
        public enum WaypointMode
        {
            ROVER,
            PLANE
        }

        public                  WaypointMode                 Mode = WaypointMode.ROVER;
        private                 MechJebModuleRoverController _ap;
        private static readonly List<MechJebWaypointRoute>   _routes = new List<MechJebWaypointRoute>();

        [EditableInfoItem("#MechJeb_MohoMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Moho Mapdist
        public readonly EditableDouble MohoMapdist = 5000;

        [EditableInfoItem("#MechJeb_EveMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Eve Mapdist
        public readonly EditableDouble EveMapdist = 5000;

        [EditableInfoItem("#MechJeb_GillyMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Gilly Mapdist
        public readonly EditableDouble GillyMapdist = -500;

        [EditableInfoItem("#MechJeb_KerbinMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Kerbin Mapdist
        public readonly EditableDouble KerbinMapdist = 500;

        [EditableInfoItem("#MechJeb_MunMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Mun Mapdist
        public readonly EditableDouble MunMapdist = 4000;

        [EditableInfoItem("#MechJeb_MinmusMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Minmus Mapdist
        public readonly EditableDouble MinmusMapdist = 3500;

        [EditableInfoItem("#MechJeb_DunaMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Duna Mapdist
        public readonly EditableDouble DunaMapdist = 5000;

        [EditableInfoItem("#MechJeb_IkeMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Ike Mapdist
        public readonly EditableDouble IkeMapdist = 4000;

        [EditableInfoItem("#MechJeb_DresMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Dres Mapdist
        public readonly EditableDouble DresMapdist = 1500;

        [EditableInfoItem("#MechJeb_EelooMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Eeloo Mapdist
        public readonly EditableDouble EelooMapdist = 2000;

        [EditableInfoItem("#MechJeb_JoolMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Jool Mapdist
        public readonly EditableDouble JoolMapdist = 30000;

        [EditableInfoItem("#MechJeb_TyloMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Tylo Mapdist
        public readonly EditableDouble TyloMapdist = 5000;

        [EditableInfoItem("#MechJeb_LaytheMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Laythe Mapdist
        public readonly EditableDouble LaytheMapdist = 1000;

        [EditableInfoItem("#MechJeb_PolMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Pol Mapdist
        public readonly EditableDouble PolMapdist = 500;

        [EditableInfoItem("#MechJeb_BopMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Bop Mapdist
        public readonly EditableDouble BopMapdist = 1000;

        [EditableInfoItem("#MechJeb_VallMapdist", InfoItem.Category.Rover)]
        [Persistent(pass = (int)Pass.GLOBAL)] // Vall Mapdist
        public readonly EditableDouble VallMapdist = 5000;

        internal int    SelIndex     = -1;
        private  int    _saveIndex   = -1;
        private  string _tmpRadius   = "";
        private  string _tmpMinSpeed = "";
        private  string _tmpMaxSpeed = "";
        private  string _tmpLat      = "";
        private  string _tmpLon      = "";

        private const string COORD_REG_EX =
            @"^([nsew])?\s*(-?\d+(?:\.\d+)?)(?:[°:\s]+(-?\d+(?:\.\d+)?))?(?:[':\s]+(-?\d+(?:\.\d+)?))?(?:[^nsew]*([nsew])?)?$";

        private Vector2  _scroll;
        private GUIStyle _styleActive;
        private GUIStyle _styleInactive;
        private GUIStyle _styleQuicksave;
        private string   _titleAdd = "";
        private string   _saveName = "";
        private bool     _waitingForPick;
        private Pages    _showPage = Pages.WAYPOINTS;

        private enum Pages { WAYPOINTS, SETTINGS, ROUTES }

        private static MechJebRouteRenderer _renderer;
        private        Rect[]               _waypointRects = Array.Empty<Rect>();
        private        int                  _lastIndex     = -1;
        private        int                  _settingPageIndex;

        private readonly string[] _settingPages = { "Rover", "Waypoints" };
//		private static LineRenderer redLine;
//		private static LineRenderer greenLine;

        public MechJebModuleWaypointWindow(MechJebCore core) : base(core) { }

        public override void OnStart(PartModule.StartState state)
        {
            Hidden = true;
            _ap    = Core.GetComputerModule<MechJebModuleRoverController>();
            if (HighLogic.LoadedSceneIsFlight && Vessel.isActiveVessel)
            {
                _renderer         = MechJebRouteRenderer.AttachToMapView(Core);
                _renderer.enabled = Enabled;
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

        protected override void OnModuleEnabled()
        {
            if (_renderer != null) { _renderer.enabled = true; }

            base.OnModuleEnabled();
        }

        protected override void OnModuleDisabled()
        {
            if (_renderer != null) { _renderer.enabled = false; }

            base.OnModuleDisabled();
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);

            if (!FlightGlobals.ready) return; // bodies not loaded yet

            var wps = new ConfigNode("Routes");
            if (File.Exists<MechJebCore>("mechjeb_routes.cfg"))
            {
                try
                {
                    wps = ConfigNode.Load(MuUtils.GetCfgPath("mechjeb_routes.cfg"));
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJebModuleWaypointWindow.OnLoad caught an exception trying to load mechjeb_routes.cfg: " + e);
                }
            }

            if (wps.HasNode("Waypoints"))
            {
                _routes.Clear();
                foreach (ConfigNode cn in wps.GetNodes("Waypoints"))
                {
                    _routes.Add(new MechJebWaypointRoute(cn));
                }

                _routes.Sort(SortRoutes);
            }
        }

        private void SaveRoutes()
        {
            var cn = new ConfigNode("Routes");

            if (_routes.Count > 0)
            {
                _routes.Sort(SortRoutes);
                foreach (MechJebWaypointRoute r in _routes)
                {
                    cn.AddNode(r.ToConfigNode());
                }
            }

            cn.Save(MuUtils.GetCfgPath("mechjeb_routes.cfg"));
        }

        public override string GetName() => Mode + " Waypoints" + (_titleAdd != "" ? " - " + _titleAdd : "");

        private static Coordinates GetMouseFlightCoordinates()
        {
            CelestialBody body = FlightGlobals.currentMainBody;
            Ray mouseRay = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
            Vector3d relOrigin = mouseRay.origin - body.position;
            if (Physics.Raycast(mouseRay, out RaycastHit raycast, (float)body.Radius * 4f, 1 << 15, QueryTriggerInteraction.Ignore))
            {
                return new Coordinates(body.GetLatitude(raycast.point), MuUtils.ClampDegrees180(body.GetLongitude(raycast.point)));
            }

            double curRadius = body.pqsController.radiusMax;
            double lastRadius = 0;
            int loops = 0;
            while (loops < 50)
            {
                if (PQS.LineSphereIntersection(relOrigin, mouseRay.direction, curRadius, out Vector3d relSurfacePosition))
                {
                    Vector3d surfacePoint = body.position + relSurfacePosition;
                    double alt = body.pqsController.GetSurfaceHeight(QuaternionD.AngleAxis(body.GetLongitude(surfacePoint), Vector3d.down) *
                                                                     QuaternionD.AngleAxis(body.GetLatitude(surfacePoint), Vector3d.forward) *
                                                                     Vector3d.right);
                    double error = Math.Abs(curRadius - alt);
                    if (error < (body.pqsController.radiusMax - body.pqsController.radiusMin) / 100)
                    {
                        return new Coordinates(body.GetLatitude(surfacePoint), MuUtils.ClampDegrees180(body.GetLongitude(surfacePoint)));
                    }

                    lastRadius = curRadius;
                    curRadius  = alt;
                    loops++;
                }
                else
                {
                    if (loops == 0)
                    {
                        break;
                    } // Went too low, needs to try higher

                    curRadius = (lastRadius * 9 + curRadius) / 10;
                    loops++;
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

        private static string LatToString(double lat)
        {
            while (lat > 90) { lat -= 180; }

            while (lat < -90) { lat += 180; }

            string ns = lat >= 0 ? "N" : "S";
            lat = Math.Abs(lat);

            int h = (int)lat;
            lat -= h;
            lat *= 60;

            int m = (int)lat;
            lat -= m;
            lat *= 60;

            float s = (float)lat;

            return $"{ns} {h}° {m}' {s:F3}\"";
        }

        private static string LonToString(double lon)
        {
            while (lon > 180) { lon -= 360; }

            while (lon < -180) { lon += 360; }

            string ew = lon >= 0 ? "E" : "W";
            lon = Math.Abs(lon);

            int h = (int)lon;
            lon -= h;
            lon *= 60;

            int m = (int)lon;
            lon -= m;
            lon *= 60;

            float s = (float)lon;

            return $"{ew} {h}° {m}' {s:F3}\"";
        }

        private static double ParseCoord(string latLon, bool isLongitute = false)
        {
            Match match = new Regex(COORD_REG_EX, RegexOptions.IgnoreCase).Match(latLon);
            int range = isLongitute ? 180 : 90;

            float nsew = 1;
            if (match.Groups[5] != null)
            {
                if (match.Groups[5].Value.ToUpper() == "N" || match.Groups[5].Value.ToUpper() == "E") { nsew      = 1; }
                else if (match.Groups[5].Value.ToUpper() == "S" || match.Groups[5].Value.ToUpper() == "W") { nsew = -1; }
                else if (match.Groups[1] != null)
                {
                    if (match.Groups[1].Value.ToUpper() == "N" || match.Groups[1].Value.ToUpper() == "E") { nsew      = 1; }
                    else if (match.Groups[1].Value.ToUpper() == "S" || match.Groups[1].Value.ToUpper() == "W") { nsew = -1; }
                }
            }

            float h = 0;
            if (match.Groups[2] != null) { float.TryParse(match.Groups[2].Value, out h); }

            if (h < 0)
            {
                nsew *= -1;
                h    *= -1;
            }

            float m = 0;
            if (match.Groups[3] != null) { float.TryParse(match.Groups[3].Value, out m); }

            float s = 0;
            if (match.Groups[4] != null) { float.TryParse(match.Groups[4].Value, out s); }

            h = (h + m / 60 + s / 3600) * nsew;

            while (h > range) { h -= range * 2; }

            while (h < -range) { h += range * 2; }

            return h;
        }

        private int SortRoutes(MechJebWaypointRoute a, MechJebWaypointRoute b)
        {
            int bn = string.Compare(a.Body.bodyName, b.Body.bodyName, true);
            if (bn != 0)
            {
                return bn;
            }

            return string.Compare(a.Name, b.Name, true);
        }

        public MechJebWaypoint SelectedWaypoint() => SelIndex > -1 ? _ap.Waypoints[SelIndex] : null;

        public int SelectedWaypointIndex
        {
            get => SelIndex;
            set => SelIndex = value;
        }

        protected override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(500), GUILayout.Height(400) };

        private void DrawPageWaypoints()
        {
            bool alt = GameSettings.MODIFIER_KEY.GetKey();
            _scroll = GUILayout.BeginScrollView(_scroll);
            if (_ap.Waypoints.Count > 0)
            {
                _waypointRects = new Rect[_ap.Waypoints.Count];
                GUILayout.BeginVertical();
                double eta = 0;
                double dist = 0;
                for (int i = 0; i < _ap.Waypoints.Count; i++)
                {
                    MechJebWaypoint wp = _ap.Waypoints[i];
                    double maxSpeed = wp.MaxSpeed > 0 ? wp.MaxSpeed : _ap.speed.Val;
                    float minSpeed = wp.MinSpeed > 0 ? wp.MinSpeed : 0;
                    if (MapView.MapIsEnabled && i == SelIndex)
                    {
                        GLUtils.DrawGroundMarker(MainBody, wp.Latitude, wp.Longitude, Color.red, true,
                            (DateTime.Now.Second + DateTime.Now.Millisecond / 1000f) * 6, MainBody.Radius / 100);
                    }

                    if (i >= _ap.WaypointIndex)
                    {
                        if (_ap.WaypointIndex > -1)
                        {
                            eta += GuiUtils.FromToETA(i == _ap.WaypointIndex ? Vessel.CoM : (Vector3)_ap.Waypoints[i - 1].Position, wp.Position,
                                _ap.etaSpeed > 0.1 && _ap.etaSpeed < maxSpeed ? (float)Math.Round(_ap.etaSpeed, 1) : maxSpeed);
                        }

                        dist += Vector3.Distance(
                            i == _ap.WaypointIndex || (_ap.WaypointIndex == -1 && i == 0) ? Vessel.CoM : (Vector3)_ap.Waypoints[i - 1].Position,
                            wp.Position);
                    }

                    string str =
                        $"[{i + 1}] - {wp.GetNameWithCoords()} - R: {wp.Radius:F1} m\n       S: {minSpeed:F0} ~ {maxSpeed:F0} - D: {dist.ToSI(-1)}m - ETA: {GuiUtils.TimeToDHMS(eta)}";
                    GUI.backgroundColor = i == _ap.WaypointIndex ? new Color(0.5f, 1f, 0.5f) : Color.white;
                    if (GUILayout.Button(str, i == SelIndex ? _styleActive : wp.Quicksave ? _styleQuicksave : _styleInactive))
                    {
                        if (alt)
                        {
                            _ap.WaypointIndex = _ap.WaypointIndex == i ? -1 : i;
                            // set current waypoint or unset it if it's already the current one
                        }
                        else
                        {
                            if (SelIndex == i)
                            {
                                SelIndex = -1;
                            }
                            else
                            {
                                SelIndex     = i;
                                _tmpRadius   = wp.Radius.ToString();
                                _tmpMinSpeed = wp.MinSpeed.ToString();
                                _tmpMaxSpeed = wp.MaxSpeed.ToString();
                                _tmpLat      = LatToString(wp.Latitude);
                                _tmpLon      = LonToString(wp.Longitude);
                            }
                        }
                    }

                    if (Event.current.type == EventType.Repaint)
                    {
                        _waypointRects[i] = GUILayoutUtility.GetLastRect();
                        //if (i == ap.WaypointIndex) { Debug.Log(Event.current.type.ToString() + " - " + waypointRects[i].ToString() + " - " + scroll.ToString()); }
                    }

                    GUI.backgroundColor = Color.white;

                    if (SelIndex > -1 && SelIndex == i)
                    {
                        GUILayout.BeginHorizontal();

                        GUILayout.Label("  Radius: ", GUILayout.ExpandWidth(false));
                        _tmpRadius = GUILayout.TextField(_tmpRadius, GUILayout.Width(50));
                        float.TryParse(_tmpRadius, out wp.Radius);
                        if (GUILayout.Button("A", GUILayout.ExpandWidth(false)))
                        {
                            _ap.Waypoints.GetRange(i, _ap.Waypoints.Count - i).ForEach(fewp => fewp.Radius = wp.Radius);
                        }

                        GUILayout.Label("- Speed: ", GUILayout.ExpandWidth(false));
                        _tmpMinSpeed = GUILayout.TextField(_tmpMinSpeed, GUILayout.Width(40));
                        float.TryParse(_tmpMinSpeed, out wp.MinSpeed);
                        if (GUILayout.Button("A", GUILayout.ExpandWidth(false)))
                        {
                            _ap.Waypoints.GetRange(i, _ap.Waypoints.Count - i).ForEach(fewp => fewp.MinSpeed = wp.MinSpeed);
                        }

                        GUILayout.Label(" - ", GUILayout.ExpandWidth(false));
                        _tmpMaxSpeed = GUILayout.TextField(_tmpMaxSpeed, GUILayout.Width(40));
                        float.TryParse(_tmpMaxSpeed, out wp.MaxSpeed);
                        if (GUILayout.Button("A", GUILayout.ExpandWidth(false)))
                        {
                            _ap.Waypoints.GetRange(i, _ap.Waypoints.Count - i).ForEach(fewp => fewp.MaxSpeed = wp.MaxSpeed);
                        }

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("QS", wp.Quicksave ? _styleQuicksave : _styleInactive, GUILayout.ExpandWidth(false)))
                        {
                            if (alt)
                            {
                                _ap.Waypoints.GetRange(i, _ap.Waypoints.Count - i).ForEach(fewp => fewp.Quicksave = !fewp.Quicksave);
                            }
                            else
                            {
                                wp.Quicksave = !wp.Quicksave;
                            }
                        }

                        GUILayout.EndHorizontal();


                        GUILayout.BeginHorizontal();

                        GUILayout.Label("Lat ", GUILayout.ExpandWidth(false));
                        _tmpLat     = GUILayout.TextField(_tmpLat, GUILayout.Width(125));
                        wp.Latitude = ParseCoord(_tmpLat);

                        GUILayout.Label(" -  Lon ", GUILayout.ExpandWidth(false));
                        _tmpLon      = GUILayout.TextField(_tmpLon, GUILayout.Width(125));
                        wp.Longitude = ParseCoord(_tmpLon, true);

                        GUILayout.EndHorizontal();
                    }
                }

                _titleAdd = "Distance: " + dist.ToSI(-1) + "m - ETA: " + GuiUtils.TimeToDHMS(eta);
                GUILayout.EndVertical();
            }
            else
            {
                _titleAdd = "";
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(alt ? "Reverse" : !_waitingForPick ? "Add Waypoint" : "Abort Adding", GUILayout.Width(110)))
            {
                if (alt)
                {
                    _ap.Waypoints.Reverse();
                    if (_ap.WaypointIndex > -1) { _ap.WaypointIndex = _ap.Waypoints.Count - 1 - _ap.WaypointIndex; }

                    if (SelIndex > -1) { SelIndex = _ap.Waypoints.Count - 1 - SelIndex; }
                }
                else
                {
                    if (!_waitingForPick)
                    {
                        _waitingForPick = true;
                        if (MapView.MapIsEnabled)
                        {
                            Core.Target.Unset();
                            Core.Target.PickPositionTargetOnMap();
                        }
                    }
                    else
                    {
                        _waitingForPick = false;
                    }
                }
            }

            if (GUILayout.Button(alt ? "Clear" : "Remove", GUILayout.Width(65)) && _ap.Waypoints.Count > 0)
            {
                if (alt)
                {
                    _ap.WaypointIndex = -1;
                    _ap.Waypoints.Clear();
                }
                else if (SelIndex >= 0)
                {
                    _ap.Waypoints.RemoveAt(SelIndex);
                    if (_ap.WaypointIndex > SelIndex) { _ap.WaypointIndex--; }

                    if (_ap.WaypointIndex >= _ap.Waypoints.Count) { _ap.WaypointIndex = _ap.Waypoints.Count - 1; }
                }

                SelIndex = -1;
            }

            if (GUILayout.Button(alt ? "Top" : "Up", GUILayout.Width(57)) && SelIndex > 0 && SelIndex < _ap.Waypoints.Count &&
                _ap.Waypoints.Count >= 2)
            {
                if (alt)
                {
                    MechJebWaypoint t = _ap.Waypoints[SelIndex];
                    _ap.Waypoints.RemoveAt(SelIndex);
                    _ap.Waypoints.Insert(0, t);
                    SelIndex = 0;
                }
                else
                {
                    _ap.Waypoints.Swap(SelIndex, --SelIndex);
                }
            }

            if (GUILayout.Button(alt ? "Bottom" : "Down", GUILayout.Width(57)) && SelIndex >= 0 && SelIndex < _ap.Waypoints.Count - 1 &&
                _ap.Waypoints.Count >= 2)
            {
                if (alt)
                {
                    MechJebWaypoint t = _ap.Waypoints[SelIndex];
                    _ap.Waypoints.RemoveAt(SelIndex);
                    _ap.Waypoints.Add(t);
                    SelIndex = _ap.Waypoints.Count - 1;
                }
                else
                {
                    _ap.Waypoints.Swap(SelIndex, ++SelIndex);
                }
            }

            if (GUILayout.Button("Routes"))
            {
                _showPage = Pages.ROUTES;
                _scroll   = Vector2.zero;
            }

            if (GUILayout.Button("Settings"))
            {
                _showPage = Pages.SETTINGS;
                _scroll   = Vector2.zero;
            }

            GUILayout.EndHorizontal();

            if (SelIndex >= _ap.Waypoints.Count) { SelIndex = -1; }

            if (SelIndex == -1 && _ap.WaypointIndex > -1 && _lastIndex != _ap.WaypointIndex && _waypointRects.Length > 0)
            {
                _scroll.y = _waypointRects[_ap.WaypointIndex].y - 160;
            }

            _lastIndex = _ap.WaypointIndex;
        }

        private void DrawPageSettings()
        {
            _titleAdd = "Settings";
            MechJebModuleCustomWindowEditor ed = Core.GetComputerModule<MechJebModuleCustomWindowEditor>();
            if (!_ap.Enabled) { _ap.CalculateTraction(); } // keep calculating traction just for displaying it

            _scroll = GUILayout.BeginScrollView(_scroll);

            _settingPageIndex = GUILayout.SelectionGrid(_settingPageIndex, _settingPages, _settingPages.Length);

            switch (_settingPageIndex)
            {
                case 0:
                    GUILayout.BeginHorizontal();

                    GUILayout.BeginVertical();
                    ed.registry.Find(i => i.id == "Editable:RoverController.hPIDp").DrawItem();
                    ed.registry.Find(i => i.id == "Editable:RoverController.hPIDi").DrawItem();
                    ed.registry.Find(i => i.id == "Editable:RoverController.hPIDd").DrawItem();
                    ed.registry.Find(i => i.id == "Editable:RoverController.terrainLookAhead").DrawItem();
//					ed.registry.Find(i => i.id == "Value:RoverController.speedIntAcc").DrawItem();
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
                _showPage  = Pages.WAYPOINTS;
                _scroll    = Vector2.zero;
                _lastIndex = -1;
            }

            if (GUILayout.Button("Routes"))
            {
                _showPage = Pages.ROUTES;
                _scroll   = Vector2.zero;
            }

            if (GUILayout.Button("Help"))
            {
                Core.GetComputerModule<MechJebModuleWaypointHelpWindow>().Enabled =
                    !Core.GetComputerModule<MechJebModuleWaypointHelpWindow>().Enabled;
            }

            GUILayout.EndHorizontal();
        }

        private void DrawPageRoutes()
        {
            bool alt = GameSettings.MODIFIER_KEY.GetKey();
            _titleAdd = "Routes for " + Vessel.mainBody.bodyName;

            _scroll = GUILayout.BeginScrollView(_scroll);
            List<MechJebWaypointRoute> bodyWPs = _routes.FindAll(r => r.Body == Vessel.mainBody && r.Mode == Mode.ToString());
            for (int i = 0; i < bodyWPs.Count; i++)
            {
                GUILayout.BeginHorizontal();
                string str = bodyWPs[i].Name + " - " + bodyWPs[i].Stats;
                if (GUILayout.Button(str, i == _saveIndex ? _styleActive : _styleInactive))
                {
                    _saveIndex = _saveIndex == i ? -1 : i;
                }

                if (i == _saveIndex)
                {
                    if (GUILayout.Button("Delete", GUILayout.Width(70)))
                    {
                        _routes.RemoveAll(r => r.Name == bodyWPs[i].Name && r.Body == Vessel.mainBody && r.Mode == Mode.ToString());
                        SaveRoutes();
                        _saveIndex = -1;
                    }
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            _saveName = GUILayout.TextField(_saveName, GUILayout.Width(150));
            if (GUILayout.Button("Save", GUILayout.Width(50)))
            {
                if (_saveName != "" && _ap.Waypoints.Count > 0)
                {
                    MechJebWaypointRoute old = _routes.Find(r => r.Name == _saveName && r.Body == Vessel.mainBody && r.Mode == Mode.ToString());
                    var wps = new MechJebWaypointRoute(_saveName, Vessel.mainBody);
                    _ap.Waypoints.ForEach(wp => wps.Add(wp));
                    if (old == null)
                    {
                        _routes.Add(wps);
                    }
                    else
                    {
                        _routes[_routes.IndexOf(old)] = wps;
                    }

                    _routes.Sort(SortRoutes);
                    SaveRoutes();
                }
            }

            if (GUILayout.Button(alt ? "Add" : "Load", GUILayout.Width(50)))
            {
                if (_saveIndex > -1)
                {
                    if (!alt)
                    {
                        _ap.WaypointIndex = -1;
                        _ap.Waypoints.Clear();
                    }

                    _routes[_saveIndex].ForEach(wp => _ap.Waypoints.Add(wp));
                }
            }

            if (GUILayout.Button("Waypoints"))
            {
                _showPage  = Pages.WAYPOINTS;
                _scroll    = Vector2.zero;
                _lastIndex = -1;
            }

            if (GUILayout.Button("Settings"))
            {
                _showPage = Pages.SETTINGS;
                _scroll   = Vector2.zero;
            }

            GUILayout.EndHorizontal();
        }

        protected override void WindowGUI(int windowID)
        {
            if (GUI.Button(new Rect(WindowPos.width - 48, 0, 13, 20), "?", GuiUtils.YellowOnHover))
            {
                MechJebModuleWaypointHelpWindow help = Core.GetComputerModule<MechJebModuleWaypointHelpWindow>();
                switch (_showPage)
                {
                    case Pages.WAYPOINTS:
                        help.SelTopic = ((IList)help.Topics).IndexOf("Waypoints");
                        break;
                    case Pages.SETTINGS:
                        help.SelTopic = ((IList)help.Topics).IndexOf("Settings");
                        break;
                    case Pages.ROUTES:
                        help.SelTopic = ((IList)help.Topics).IndexOf("Routes");
                        break;
                }

                help.Enabled = help.SelTopic > -1 || help.Enabled;
            }

            _styleInactive ??= new GUIStyle(GuiUtils.Skin != null ? GuiUtils.Skin.button : GuiUtils.DefaultSkin.button)
            {
                alignment = TextAnchor.UpperLeft
            };

            if (_styleActive == null)
            {
                _styleActive = new GUIStyle(_styleInactive);
                _styleActive.active.textColor =
                    _styleActive.focused.textColor = _styleActive.hover.textColor = _styleActive.normal.textColor = Color.green;
            } // for some reason MJ's skin sometimes isn't loaded at OnStart so this has to be done here

            if (_styleQuicksave == null)
            {
                _styleQuicksave = new GUIStyle(_styleActive);
                _styleQuicksave.active.textColor = _styleQuicksave.focused.textColor =
                    _styleQuicksave.hover.textColor = _styleQuicksave.normal.textColor = Color.yellow;
            }

            bool alt = GameSettings.MODIFIER_KEY.GetKey();

            switch (_showPage)
            {
                case Pages.WAYPOINTS:
                    DrawPageWaypoints();
                    break;
                case Pages.SETTINGS:
                    DrawPageSettings();
                    break;
                case Pages.ROUTES:
                    DrawPageRoutes();
                    break;
            }

            if (_waitingForPick && Vessel.isActiveVessel && Event.current.type == EventType.Repaint)
            {
                if (MapView.MapIsEnabled)
                {
                    if (Core.Target.pickingPositionTarget == false)
                    {
                        if (Core.Target.PositionTargetExists)
                        {
                            if (SelIndex > -1 && SelIndex < _ap.Waypoints.Count)
                            {
                                _ap.Waypoints.Insert(SelIndex, new MechJebWaypoint(Core.Target.GetPositionTargetPosition()));
                                _tmpRadius = _ap.Waypoints[SelIndex].Radius.ToString();
                                _tmpLat    = LatToString(_ap.Waypoints[SelIndex].Latitude);
                                _tmpLon    = LonToString(_ap.Waypoints[SelIndex].Longitude);
                            }
                            else
                            {
                                _ap.Waypoints.Add(new MechJebWaypoint(Core.Target.GetPositionTargetPosition()));
                            }

                            Core.Target.Unset();
                            _waitingForPick = alt;
                        }
                        else
                        {
                            Core.Target.PickPositionTargetOnMap();
                        }
                    }
                }
                else
                {
                    if (!GuiUtils.MouseIsOverWindow(Core))
                    {
                        Coordinates mouseCoords = GetMouseFlightCoordinates();
                        if (mouseCoords != null)
                        {
                            if (Input.GetMouseButtonDown(0))
                            {
                                if (SelIndex > -1 && SelIndex < _ap.Waypoints.Count)
                                {
                                    _ap.Waypoints.Insert(SelIndex, new MechJebWaypoint(mouseCoords.Latitude, mouseCoords.Longitude));
                                    _tmpRadius = _ap.Waypoints[SelIndex].Radius.ToString();
                                    _tmpLat    = LatToString(_ap.Waypoints[SelIndex].Latitude);
                                    _tmpLon    = LonToString(_ap.Waypoints[SelIndex].Longitude);
                                }
                                else
                                {
                                    _ap.Waypoints.Add(new MechJebWaypoint(mouseCoords.Latitude, mouseCoords.Longitude));
                                }

                                _waitingForPick = alt;
                            }
                        }
                    }
                }
            }

            base.WindowGUI(windowID);
        }

        public override void OnFixedUpdate()
        {
            if (Vessel.isActiveVessel && (_renderer == null || _renderer.AP != _ap)) { MechJebRouteRenderer.AttachToMapView(Core); }

            _ap.Waypoints.ForEach(wp => wp.Update());
//			float scale = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, vessel.CoM) / 900f;
//			greenLine.SetPosition(0, vessel.CoM);
//			greenLine.SetPosition(1, vessel.CoM + ap.norm * 5);
//			greenLine.SetWidth(scale + 0.1f, scale + 0.1f);
            base.OnFixedUpdate();
        }
    }

    [UsedImplicitly]
    public class MechJebModuleWaypointHelpWindow : DisplayModule
    {
        public          int      SelTopic;
        public readonly string[] Topics       = { "Rover Controller", "Waypoints", "Routes", "Settings" };
        private         string   _selSubTopic = "";
        private         GUIStyle _btnActive;
        private         GUIStyle _btnInactive;

        private void HelpTopic(string title, string text)
        {
            GUILayout.BeginVertical();
            if (GUILayout.Button(title, _selSubTopic == title ? _btnActive : _btnInactive))
            {
                _selSubTopic = _selSubTopic != title ? title : "";
                WindowPos    = new Rect(WindowPos.x, WindowPos.y, WindowPos.width, 0);
            }

            if (_selSubTopic == title)
            {
                GUILayout.Label(text);
            }

            GUILayout.EndVertical();
        }

        public MechJebModuleWaypointHelpWindow(MechJebCore core) : base(core) { }

        public override string GetName() => Localizer.Format("#MechJeb_Waypointhelper_title"); // "Waypoint Help"

        public override string IconName() => "Waypoint Help";

        public override void OnStart(PartModule.StartState state)
        {
            Hidden = true;
            base.OnStart(state);
        }

        protected override void WindowGUI(int windowID)
        {
            _btnInactive ??= new GUIStyle(GuiUtils.Skin.button) { alignment = TextAnchor.MiddleLeft };

            if (_btnActive == null)
            {
                _btnActive                  = new GUIStyle(_btnInactive);
                _btnActive.active.textColor = _btnActive.hover.textColor = _btnActive.focused.textColor = _btnActive.normal.textColor = Color.green;
            }

            SelTopic = GUILayout.SelectionGrid(SelTopic, Topics, Topics.Length);

            switch (Topics[SelTopic])
            {
                case "Rover Controller":
                    HelpTopic("Holding a set Heading",
                        "To hold a specific heading just tick the box next to 'Heading control' and the autopilot will try to keep going for the entered heading." +
                        "\nThis also needs to be enabled when the autopilot is supposed to drive to a waypoint" +
                        "'Heading Error' simply shows the error between current heading and target heading.");
                    HelpTopic("Holding a set Speed",
                        "To hold a specific speed just tick the box next to 'Speed control' and the autopilot will try to keep going at the entered speed." +
                        "\nThis also needs to be enabled when the autopilot is supposed to drive to a waypoint" +
                        "'Speed Error' simply shows the error between current speed and target speed.");
                    HelpTopic("More stability while driving and nice landings",
                        "If you turn on 'Stability Control' then the autopilot will use the reaction wheel's torque to keep the rover aligned with the surface." +
                        "\nThis means that if you make a jump the autopilot will try to align the rover in the best possible way to land as straight and flat as possible given the available time and torque." +
                        "\nBe aware that this doesn't make your rover indestructible, only relatively unlikely to land in a bad way." +
                        "\n\n'Stability Control' will also limit the brake to reduce the chances of flipping over from too much braking power." +
                        "\nSee 'Settings' -> 'Traction and Braking'. This setting is also saved per vessel.");
                    HelpTopic("Brake on Pilot Eject",
                        "With this option enabled the rover will stop if the pilot (on manned rovers) should get thrown out of his seat.");
                    HelpTopic("Target Speed", "Current speed the autopilot tries to achieve.");
                    HelpTopic("Waypoint Index", "Overview of waypoints and which the autopilot is currently driving to.");
                    HelpTopic("Button 'Waypoints'", "Opens the waypoint list to set up a route.");
                    HelpTopic("Button 'Follow' / 'Stop'",
                        "This sets the autopilot to drive along the set route starting at the first waypoint. Only visible when atleast one waypoint is set." +
                        "\n\nAlt click will set the autopilot to 'Loop Mode' which will make it go for the first waypoint again after reaching the last." +
                        "If the only waypoint happens to be a target it will keep following that instead of only going to it once." +
                        "\n\nIf the autopilot is already active the 'Follow' button will turn into the 'Stop' button which will obviously stop it when pressed.");
                    HelpTopic("Button 'To Target'",
                        "Clears the route, adds the target as only waypoint and starts the autopilot. Only visible with a selected target." +
                        "\n\nAlt click will set the autopilot to 'Loop Mode' which will make it continue to follow the target, pausing when near it instead of turning off then.");
                    HelpTopic("Button 'Add Target'",
                        "Adds the selected target as a waypoint either at the end of the route or before the selected waypoint. Only visible with a selected target.");
                    break;

                case "Waypoints":
                    HelpTopic("Adding Waypoints", "Adds a new waypoint to the route at the end or before the currently selected waypoint, " +
                                                  "simply click the terrain or somewhere on the body in Mapview." +
                                                  "\n\nAlt clicking will reverse the route for easier going back and holding Alt while clicking the terrain or body in Mapview will allow to add more waypoints without having to click the button again.");
                    HelpTopic("Removing Waypoints", "Removes the currently selected waypoint." +
                                                    "\n\nAlt clicking will remove all waypoints.");
                    HelpTopic("Reordering Waypoints",
                        "'Up' and 'Down' will move the selected waypoint up or down in the list, Alt clicking will move it to the top or bottom respectively.");
                    HelpTopic("Waypoint Radius",
                        "Radius defines the distance to the center of the waypoint after which the waypoint will be considered 'reached'." +
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
                    HelpTopic("Changing the current target Waypoint",
                        "Alt clicking a waypoint will mark it as the current target waypoint. The active waypoint has a green tinted background.");
                    break;

                case "Routes":
                    HelpTopic("Routes Help", "The empty textfield is for saving routes, enter a name there before clicking 'Save'." +
                                             "\n\nTo load a route simply select one from the list and click 'Load'." +
                                             "\n\nTo delete a route simply select it and a 'Delete' button will appear right of it.");
                    break;

                case "Settings":
                    HelpTopic("Heading / Speed PID",
                        "These parameters control the behaviour of the heading's / speed's PID. Saved globally so NO TOUCHING unless you know what you're doing (or atleast know how to write down numbers to restore it if you mess up)");
                    HelpTopic("Safe Turn Speed",
                        "'Safe Turn Speed' tells the autopilot which speed the rover can usually go full turn through corners without tipping over." +
                        "\n\nGiven how differently terrain can be and other influences you can just leave it at 3 m/s but if you're impatient or just want to experiment feel free to test around. Saved per vessel type (same named vessels will share the setting).");
                    HelpTopic("Traction and Braking", "'Traction' shows in % how many wheels have ground contact." +
                                                      "\n'Traction Brake Limit' defines what traction is atleast needed for the autopilot to still apply the brakes (given 'Stability Control' is active) even if you hold the brake down." +
                                                      "\nThis means the default setting of 75 will make it brake only if atleast 3 wheels have ground contact." +
                                                      "\n'Traction Brake Limit' is saved per vessel type." +
                                                      "\n\nIf you have 'Stability Control' off then it won't take care of your brake and you can flip as much as you want.");
                    HelpTopic("Changing the route height in Mapview",
                        "These values define offsets for the route height in Mapview. Given how weird it's set up it can be that they are too high or too low so I added these for easier adjusting. Saved globally, I think.");
                    break;
            }

            base.WindowGUI(windowID);
        }
    }

    public class MechJebRouteRenderer : MonoBehaviour
    {
        private static readonly Material                     _material = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));
        public                  MechJebModuleRoverController AP;
        private                 LineRenderer                 _pastPath;
        private                 LineRenderer                 _currPath;
        private                 LineRenderer                 _nextPath;
        private                 LineRenderer                 _selWp;
        private readonly        Color                        _pastPathColor = new Color(0f, 0f, 1f, 0.5f);
        private readonly        Color                        _currPathColor = new Color(0f, 1f, 0f, 0.5f);
        private readonly        Color                        _nextPathColor = new Color(1f, 1f, 0f, 0.5f);
        private readonly        Color                        _selWpColor    = new Color(1f, 0f, 0f, 0.5f);
        private                 double                       _addHeight;

        public static MechJebRouteRenderer AttachToMapView(MechJebCore core)
        {
            MechJebRouteRenderer renderer = MapView.MapCamera.gameObject.GetComponent<MechJebRouteRenderer>();
            if (!renderer)
            {
                renderer = MapView.MapCamera.gameObject.AddComponent<MechJebRouteRenderer>();
            }

            renderer.AP = core.GetComputerModule<MechJebModuleRoverController>();
            return renderer;
        }

        private static bool NewLineRenderer(ref LineRenderer line)
        {
            if (line != null) { return false; }

            var obj = new GameObject("LineRenderer");
            line               = obj.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.material      = _material;
            line.startWidth    = 10.0f;
            line.endWidth      = 10.0f;
            line.positionCount = 2;
            return true;
        }

        private static Vector3 RaisePositionOverTerrain(Vector3 position, float heightOffset)
        {
            CelestialBody body = FlightGlobals.ActiveVessel.mainBody;
            if (!MapView.MapIsEnabled)
                return body.GetWorldSurfacePosition(body.GetLatitude(position), body.GetLongitude(position),
                    body.GetAltitude(position) + heightOffset);
            double lat = body.GetLatitude(position);
            double lon = body.GetLongitude(position);
            return ScaledSpace.LocalToScaledSpace(body.position +
                                                  (body.Radius + heightOffset + body.TerrainAltitude(lat, lon)) *
                                                  body.GetSurfaceNVector(lat, lon));
        }

        public new bool enabled
        {
            get => base.enabled;
            set
            {
                base.enabled = value;
                if (_pastPath != null) { _pastPath.enabled = value; }

                if (_currPath != null) { _currPath.enabled = value; }

                if (_nextPath != null) { _nextPath.enabled = value; }
            }
        }

        public void OnPreRender()
        {
            if (NewLineRenderer(ref _pastPath))
            {
                _pastPath.startColor = _pastPathColor;
                _pastPath.endColor   = _pastPathColor;
            }

            if (NewLineRenderer(ref _currPath))
            {
                _currPath.startColor = _currPathColor;
                _currPath.endColor   = _currPathColor;
            }

            if (NewLineRenderer(ref _nextPath))
            {
                _nextPath.startColor = _nextPathColor;
                _nextPath.endColor   = _nextPathColor;
            }

            if (NewLineRenderer(ref _selWp))
            {
                _selWp.startColor = _selWpColor;
                _selWp.endColor   = _selWpColor;
            }

            //Debug.Log(ap.vessel.vesselName);
            MechJebModuleWaypointWindow window = AP.Core.GetComputerModule<MechJebModuleWaypointWindow>();
            _addHeight = AP.Vessel.mainBody.bodyName switch
            {
                "Moho"   => window.MohoMapdist,
                "Eve"    => window.EveMapdist,
                "Gilly"  => window.GillyMapdist,
                "Kerbin" => window.KerbinMapdist,
                "Mun"    => window.MunMapdist,
                "Minmus" => window.MinmusMapdist,
                "Duna"   => window.DunaMapdist,
                "Ike"    => window.IkeMapdist,
                "Dres"   => window.DresMapdist,
                "Jool"   => window.JoolMapdist,
                "Laythe" => window.LaytheMapdist,
                "Vall"   => window.VallMapdist,
                "Tylo"   => window.TyloMapdist,
                "Bop"    => window.BopMapdist,
                "Pol"    => window.PolMapdist,
                "Eeloo"  => window.EelooMapdist,
                _        => window.KerbinMapdist
            };

            if (AP != null && AP.Waypoints.Count > 0 && AP.Vessel.isActiveVessel && HighLogic.LoadedSceneIsFlight)
            {
                float targetHeight = MapView.MapIsEnabled ? (float)_addHeight : 3f;
                float scale = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, AP.Vessel.CoM) / 700f;
                float width = MapView.MapIsEnabled ? (float)(0.005 * PlanetariumCamera.fetch.Distance) : scale + 0.1f;
                //float width = (MapView.MapIsEnabled ? (float)mainBody.Radius / 10000 : 1);

                _pastPath.startWidth = width;
                _pastPath.endWidth   = width;
                _currPath.startWidth = width;
                _currPath.endWidth   = width;
                _nextPath.startWidth = width;
                _nextPath.endWidth   = width;
                _selWp.gameObject.layer = _pastPath.gameObject.layer =
                    _currPath.gameObject.layer = _nextPath.gameObject.layer = MapView.MapIsEnabled ? 9 : 0;

                int sel = AP.Core.GetComputerModule<MechJebModuleWaypointWindow>().SelIndex;
                _selWp.enabled = sel > -1 && !MapView.MapIsEnabled;
                if (_selWp.enabled)
                {
                    float w = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, AP.Waypoints[sel].Position) / 600f + 0.1f;
                    _selWp.startWidth = 0;
                    _selWp.endWidth   = w * 10f;
                    _selWp.SetPosition(0, RaisePositionOverTerrain(AP.Waypoints[sel].Position, targetHeight + 3f));
                    _selWp.SetPosition(1, RaisePositionOverTerrain(AP.Waypoints[sel].Position, targetHeight + 3f + w * 15f));
                }

                if (AP.WaypointIndex > 0)
                {
//					Debug.Log("drawing pastPath");
                    _pastPath.enabled       = true;
                    _pastPath.positionCount = AP.WaypointIndex + 1;
                    for (int i = 0; i < AP.WaypointIndex; i++)
                    {
//						Debug.Log("vert " + i.ToString());
                        _pastPath.SetPosition(i, RaisePositionOverTerrain(AP.Waypoints[i].Position, targetHeight));
                    }

                    _pastPath.SetPosition(AP.WaypointIndex, RaisePositionOverTerrain(AP.Vessel.CoM, targetHeight));
//					Debug.Log("pastPath drawn");
                }
                else
                {
//					Debug.Log("no pastPath");
                    _pastPath.enabled = false;
                }

                if (AP.WaypointIndex > -1)
                {
//					Debug.Log("drawing currPath");
                    _currPath.enabled = true;
                    _currPath.SetPosition(0, RaisePositionOverTerrain(AP.Vessel.CoM, targetHeight));
                    _currPath.SetPosition(1, RaisePositionOverTerrain(AP.Waypoints[AP.WaypointIndex].Position, targetHeight));
//					Debug.Log("currPath drawn");
                }
                else
                {
//					Debug.Log("no currPath");
                    _currPath.enabled = false;
                }

                int nextCount = AP.Waypoints.Count - AP.WaypointIndex;
                if (nextCount > 1)
                {
//					Debug.Log("drawing nextPath of " + nextCount + " verts");
                    _nextPath.enabled       = true;
                    _nextPath.positionCount = nextCount;
                    _nextPath.SetPosition(0,
                        RaisePositionOverTerrain(AP.WaypointIndex == -1 ? AP.Vessel.CoM : (Vector3)AP.Waypoints[AP.WaypointIndex].Position,
                            targetHeight));
                    for (int i = 0; i < nextCount - 1; i++)
                    {
//						Debug.Log("vert " + i.ToString() + " (" + (ap.WaypointIndex + 1 + i).ToString() + ")");
                        _nextPath.SetPosition(i + 1, RaisePositionOverTerrain(AP.Waypoints[AP.WaypointIndex + 1 + i].Position, targetHeight));
                    }
//					Debug.Log("nextPath drawn");
                }
                else
                {
//					Debug.Log("no nextPath");
                    _nextPath.enabled = false;
                }
            }
            else
            {
                //Debug.Log("moo");
                _selWp.enabled = _pastPath.enabled = _currPath.enabled = _nextPath.enabled = false;
            }
        }
    }
}
