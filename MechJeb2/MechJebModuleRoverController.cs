using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
	public class MechJebRoverWaypoint {
		public const float defaultRadius = 5;
		public double Latitude;
		public double Longitude;
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
		
		public MechJebRoverWaypoint(double Latitude, double Longitude, float Radius = defaultRadius, string Name = "", float MinSpeed = 0, float MaxSpeed = 0) { //, CelestialBody Body = null) {
			this.Latitude = Latitude;
			this.Longitude = Longitude;
			this.Radius = Radius;
			this.Name = (Name == null ? "" : Name);
			this.MinSpeed = MinSpeed;
			this.MaxSpeed = MaxSpeed;
			Update();
		}
		
		public MechJebRoverWaypoint(Vector3d Position, float Radius = defaultRadius, string Name = "", float MinSpeed = 0, float MaxSpeed = 0) { //, CelestialBody Body = null) {
			this.Latitude = Body.GetLatitude(Position);
			this.Longitude = Body.GetLongitude(Position);
			this.Radius = Radius;
			this.Name = (Name == null ? "" : Name);
			this.MinSpeed = MinSpeed;
			this.MaxSpeed = MaxSpeed;
			Update();
		}
		
		public MechJebRoverWaypoint(Vessel Target, float Radius = defaultRadius, string Name = "", float MinSpeed = 0, float MaxSpeed = 0) {
			this.Target = Target;
			this.Radius = Radius;
			this.Name = (Name == null ? "" : Name);
			this.MinSpeed = MinSpeed;
			this.MaxSpeed = MaxSpeed;
			Update();
		}
		
		public MechJebRoverWaypoint(ConfigNode Node) {
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
	
	public class MechJebModuleRoverController : ComputerModule
	{
		public List<MechJebRoverWaypoint> Waypoints = new List<MechJebRoverWaypoint>();
		public int WaypointIndex = -1;
		private CelestialBody lastBody = null;
		public bool LoopWaypoints = false;
		
		protected bool controlHeading;
		[ToggleInfoItem("Heading control", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]
		public bool ControlHeading
		{
			get { return controlHeading; }
			set
			{
				controlHeading = value;
                if (controlHeading || controlSpeed || brakeOnEject)
				{
					users.Add(this);
				}
				else
				{
					users.Remove(this);
				}
			}
		}

		[EditableInfoItem("Heading", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]
		public EditableDouble heading = 0;

		protected bool controlSpeed = false;
		[ToggleInfoItem("Speed control", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]
		public bool ControlSpeed
		{
			get { return controlSpeed; }
			set
			{
				controlSpeed = value;
                if (controlHeading || controlSpeed || brakeOnEject)
				{
					users.Add(this);
				}
				else
				{
					users.Remove(this);
				}
			}
		}

        protected bool brakeOnEject = false;
        [ToggleInfoItem("Brake on Pilot Eject", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]
        public bool BrakeOnEject
        {
            get { return brakeOnEject; }
            set
            {
                brakeOnEject = value;
                if (controlHeading || controlSpeed || brakeOnEject)
                {
                    users.Add(this);
                }
                else
                {
                    users.Remove(this);
                }
            }
        }

		[EditableInfoItem("Speed", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]
		public EditableDouble speed = 10;

		public PIDController headingPID;
		public PIDController speedPID;
		
		[EditableInfoItem("Safe turnspeed", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Type)]
		public EditableDouble turnSpeed = 3;

		[EditableInfoItem("Heading PID P", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble hPIDp = 0.05;
		[EditableInfoItem("Heading PID I", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble hPIDi = 0.001;
		[EditableInfoItem("Heading PID D", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble hPIDd = 0.001;
		
		[EditableInfoItem("Speed PID P", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble sPIDp = 3;
		[EditableInfoItem("Speed PID I", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble sPIDi = 0.0025;
		[EditableInfoItem("Speed PID D", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble sPIDd = 0.025;
		
		public override void OnStart(PartModule.StartState state)
		{
			headingPID = new PIDController(hPIDp, hPIDi, hPIDd);
			speedPID = new PIDController(sPIDp, sPIDi, sPIDd);
			if (HighLogic.LoadedSceneIsFlight && orbit != null) {
				lastBody = orbit.referenceBody;
			}
			base.OnStart(state);
		}

		[ValueInfoItem("Heading error", InfoItem.Category.Rover, format = "F1", units = "º")]
		public double headingErr;
		[ValueInfoItem("Speed error", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")]
		public double speedErr;
		public MuMech.MovingAverage tgtSpeed = new MovingAverage(150);
		public MuMech.MovingAverage etaSpeed = new MovingAverage(300);

		protected double headingLast, speedLast;

		public double HeadingToPos(Vector3 fromPos, Vector3 toPos) {
			// thanks to Cilph who did most of this since I don't understand anything ~ BR2k
			var body = vessel.mainBody;
			var fromLon = body.GetLongitude(fromPos);
			var toLon = body.GetLongitude(toPos);
			var diff = toLon - fromLon;
			if (diff < -180) { diff += 360; }
			if (diff >  180) { diff -= 360; }
			Vector3 myPos  = fromPos - body.transform.position;
			Vector3 north = body.transform.position + ((float)body.Radius * body.transform.up) - fromPos;
			Vector3 tgtPos = toPos - fromPos;
			return (diff < 0 ? -1 : 1) * Vector3.Angle(Vector3d.Exclude(myPos.normalized, north.normalized), Vector3.Exclude(myPos.normalized, tgtPos.normalized));
		}

		public float TurningSpeed(double speed, double error) {
			return (float)Math.Max(speed / (Math.Abs(error) / 4 > 1 ? Math.Abs(error) / 4 : 1), turnSpeed);
		}
		
		public override void Drive(FlightCtrlState s) // TODO put the brake in when running out of power to prevent nighttime solar failures on hills, or atleast try to
		{ // TODO make distance calculation for 'reached' determination consider the rover and waypoint on sealevel to prevent height differences from messing it up
			if (orbit.referenceBody != lastBody) { WaypointIndex = -1; Waypoints.Clear(); }
			MechJebRoverWaypoint wp = (WaypointIndex > -1 && WaypointIndex < Waypoints.Count ? Waypoints[WaypointIndex] : null);
			
			var curSpeed = vesselState.speedSurface;
			etaSpeed.value = curSpeed;
			
			if (wp != null && wp.Body == orbit.referenceBody) {
				if (controlHeading) {
					heading = Math.Round(HeadingToPos(vessel.CoM, wp.Position), 1);
				}
				if (controlSpeed) {
					var nextWP = (WaypointIndex < Waypoints.Count - 1 ? Waypoints[WaypointIndex + 1] : (LoopWaypoints ? Waypoints[0] : null));
					var distance = Vector3.Distance(vessel.CoM, wp.Position);
					//var maxSpeed = (wp.MaxSpeed > 0 ? Math.Min((float)speed, wp.MaxSpeed) : speed); // use waypoints maxSpeed if set and smaller than set the speed or just stick with the set speed
					var maxSpeed = (wp.MaxSpeed > 0 ? wp.MaxSpeed : speed); // speed used to go towards the waypoint, using the waypoints maxSpeed if set or just stick with the set speed
					var minSpeed = (wp.MinSpeed > 0 ? wp.MinSpeed :
					                (nextWP != null ? TurningSpeed((nextWP.MaxSpeed > 0 ? nextWP.MaxSpeed : speed), heading - HeadingToPos(wp.Position, nextWP.Position)) :
					                 (distance - wp.Radius > 50 ? turnSpeed.val : 1)));
					minSpeed = (wp.Quicksave ? 0 : minSpeed);
					// ^ speed used to go through the waypoint, using half the set speed or maxSpeed as minSpeed for routing waypoints (all except the last)
					var brakeFactor = Math.Max((curSpeed - minSpeed) * 1, 3);
					var newSpeed = Math.Min(maxSpeed, Math.Max((distance - wp.Radius) / brakeFactor, minSpeed)); // brake when getting closer
					newSpeed = (newSpeed > turnSpeed ? TurningSpeed(newSpeed, headingErr) : newSpeed); // reduce speed when turning a lot
					var radius = Math.Max(wp.Radius, 10 / 0.8); // alternative radius so negative radii can still make it go full speed through waypoints for navigation reasons
					if (distance < radius) {
						if (WaypointIndex + 1 >= Waypoints.Count) { // last waypoint
							newSpeed = new [] { newSpeed, (distance < radius * 0.8 ? 0 : 1) }.Min();
							// ^ limit speed so it'll only go from 1m/s to full stop when braking to prevent accidents on moons
							if (LoopWaypoints) {
								WaypointIndex = 0;
							}
							else {
								newSpeed = -0.25;
								tgtSpeed.force(newSpeed);
								if (curSpeed < 0.85) {
									if (wp.Quicksave) {
										if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR) {
											WaypointIndex = -1;
											controlHeading = controlSpeed = false;
											QuickSaveLoad.QuickSave();
										}
									}
									else {
										WaypointIndex = -1;
										controlHeading = controlSpeed = false;
									}
								}
//								else {
//									Debug.Log("Is this even getting called?");
//									WaypointIndex++;
//								}
							}
						}
						else {
							if (wp.Quicksave) {
								newSpeed = -0.25;
								tgtSpeed.force(newSpeed);
								if (curSpeed < 0.85) {
									if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR) {
										WaypointIndex++;
										QuickSaveLoad.QuickSave();
									}
								}
							}
							else {
								WaypointIndex++;
							}
						}
					}
					vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (GameSettings.BRAKES.GetKey() && vessel.isActiveVessel) || ((s.wheelThrottle == 0 || !vessel.isActiveVessel) && curSpeed < 0.85 && newSpeed < 0.85));
					// ^ brake if needed to prevent rolling, hopefully
					tgtSpeed.value = Math.Round(newSpeed, 1);
				}
			}

			if (controlHeading)
			{
				if (heading != headingLast)
				{
					headingPID.Reset();
					headingLast = heading;
				}

				double instantaneousHeading = vesselState.rotationVesselSurface.eulerAngles.y;
				headingErr = MuUtils.ClampDegrees180(instantaneousHeading - heading);
				if (s.wheelSteer == s.wheelSteerTrim || FlightGlobals.ActiveVessel != vessel)
				{
					float spd = Mathf.Min((float)speed, (float)turnSpeed); // if a slower speed than the turnspeed is used also be more careful with the steering
					float limit = (curSpeed <= turnSpeed ? 1 : Mathf.Clamp((float)((spd * spd) / (curSpeed * curSpeed)), 0.2f, 1f));
					double act = headingPID.Compute(headingErr);
					s.wheelSteer = Mathf.Clamp((float)act, -limit, limit);
				}
			}
			// Brake if there is no controler (Pilot eject from seat)
			if (brakeOnEject && vessel.GetReferenceTransformPart() == null)
			{
				s.wheelThrottle = 0;
				vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
			}
			else if (controlSpeed)
			{
				if (speed != speedLast)
				{
					speedPID.Reset();
					speedLast = speed;
				}

				speedErr = (WaypointIndex == -1 ? speed.val : tgtSpeed.value) - Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.forward);
				if (s.wheelThrottle == s.wheelThrottleTrim || FlightGlobals.ActiveVessel != vessel)
				{
					double act = speedPID.Compute(speedErr);
					s.wheelThrottle = Mathf.Clamp((float)act, -1, 1);
				}
			}
		}
		
		public override void OnFixedUpdate()
		{
			if (!core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled) { // update waypoints unless the waypoint window is (hopefully) doing that already
				Waypoints.ForEach(wp => wp.Update());
			}
			if (orbit != null && lastBody != orbit.referenceBody) { lastBody = orbit.referenceBody; }
			headingPID.Kp = hPIDp;
			headingPID.Ki = hPIDi;
			headingPID.Kd = hPIDd;
			speedPID.Kp = sPIDp;
			speedPID.Ki = sPIDi;
			speedPID.Kd = sPIDd;
		}
		
		public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
		{
            base.OnLoad(local, type, global);

            if (local != null)
            {
                var wps = local.GetNode("Waypoints");
                if (wps != null && wps.HasNode("Waypoint"))
                {
                    int.TryParse(wps.GetValue("Index"), out WaypointIndex);
                    Waypoints.Clear();
                    foreach (ConfigNode cn in wps.GetNodes("Waypoint"))
                    {
                        Waypoints.Add(new MechJebRoverWaypoint(cn));
                    }
                }
            }
		}
		
		public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
		{
            base.OnSave(local, type, global);

            if (local == null) return;

			if (local.HasNode("Waypoints")) { local.RemoveNode("Waypoints"); }
			if (Waypoints.Count > 0) {
				ConfigNode cn = local.AddNode("Waypoints");
				cn.AddValue("Index", WaypointIndex);
				foreach (MechJebRoverWaypoint wp in Waypoints) {
					cn.AddNode(wp.ToConfigNode());
				}
			}			
		}
		
		public MechJebModuleRoverController(MechJebCore core) : base(core) { }
	}
}
