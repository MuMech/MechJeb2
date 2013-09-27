using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
	public class MechJebRoverWaypoint {
		public const float defaultRadius = 50;
		public double Latitude;
		public double Longitude;
		public Vector3d Position;
		public float Radius;
		public string Name;
		public Vessel Target;
		public float MinSpeed;
		public float MaxSpeed;
		
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
			Update();
		}
		
		public ConfigNode ToConfigNode() {
			ConfigNode cn = new ConfigNode("Waypoint");
			if (Target != null) {
				cn.AddValue("Target", Target.id);
			}
			cn.AddValue("Latitude", Latitude);
			cn.AddValue("Longitude", Longitude);
			cn.AddValue("Radius", Radius);
			cn.AddValue("MinSpeed", MinSpeed);
			cn.AddValue("MaxSpeed", MaxSpeed);
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
				if (controlHeading || controlSpeed)
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
				if (controlHeading || controlSpeed)
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
		public EditableDouble turnSpeed = 5;

		public override void OnStart(PartModule.StartState state)
		{
			headingPID = new PIDController(0.05, 0.000001, 0.005);
			speedPID = new PIDController(5, 0.001, 0.01);
			lastBody = orbit.referenceBody;
			base.OnStart(state);
		}

		[ValueInfoItem("Heading error", InfoItem.Category.Rover, format = "F1", units = "º")]
		public double headingErr;
		[ValueInfoItem("Speed error", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")]
		public double speedErr;
		public MuMech.MovingAverage tgtSpeed = new MovingAverage(300);
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
			Vector3d myPos  = fromPos - body.transform.position;
			Vector3d north  = body.transform.position + (body.Radius * (Vector3d)body.transform.up) - fromPos;
			Vector3d tgtPos = toPos - fromPos;
			return (diff < 0 ? -1 : 1) * Vector3d.Angle(Vector3d.Exclude(myPos.normalized, north.normalized), Vector3d.Exclude(myPos.normalized, tgtPos.normalized));
		}

		public override void Drive(FlightCtrlState s)
		{
			if (orbit.referenceBody != lastBody) { WaypointIndex = -1; Waypoints.Clear(); }
			MechJebRoverWaypoint wp = (WaypointIndex > -1 ? Waypoints[WaypointIndex] : null);
			
			var curSpeed = vesselState.speedSurface;
			etaSpeed.value = curSpeed;
			
			if (wp != null && wp.Body == orbit.referenceBody) {
				if (controlHeading) {
					heading = Math.Round(HeadingToPos(vessel.CoM, wp.Position), 1);
				}
				if (controlSpeed) {
					var distance = Vector3.Distance(vessel.CoM, wp.Position);
					//var maxSpeed = (wp.MaxSpeed > 0 ? Math.Min((float)speed, wp.MaxSpeed) : speed); // use waypoints maxSpeed if set and smaller than set the speed or just stick with the set speed
					var maxSpeed = (wp.MaxSpeed > 0 ? wp.MaxSpeed : speed); // use waypoints maxSpeed if set or just stick with the set speed
					var minSpeed = (wp.MinSpeed > 0 ? wp.MinSpeed : (WaypointIndex < Waypoints.Count - 1 || LoopWaypoints ? maxSpeed / 2 : 0));
					// ^ use half the set speed or maxSpeed as minSpeed for routing waypoints (all except the last)
					var newSpeed = Math.Min(maxSpeed, (distance - wp.Radius - (curSpeed * curSpeed))); //  * (vesselState.localg / 9.81)
					newSpeed = Math.Max(Math.Max(newSpeed, minSpeed) - Math.Abs(headingErr), new double[] { maxSpeed, turnSpeed, Math.Max(newSpeed, 3) }.Min());
					// ^ limit speed for approaching waypoints and turning but also allow going to 0 when getting very close to the waypoint for following a target
					var radius = Math.Max(wp.Radius, 10 / 0.8); // alternative radius so negative radii can still make it go full speed through waypoints for navigation reasons
					if (distance < radius) {
						if (WaypointIndex + 1 >= Waypoints.Count) { // last waypoint
							newSpeed = new [] { newSpeed, (distance < radius * 0.8 ? 0 : 1) }.Min();
							// ^ limit speed so it'll only go from 1m/s to full stop when braking to prevent accidents on moons
							if (LoopWaypoints) {
								WaypointIndex = 0;
							}
							else {
								controlHeading = false;
								newSpeed = -0.5;
								tgtSpeed.force(newSpeed);
								if (curSpeed < 0.85) {
									WaypointIndex = -1;
									ControlSpeed = false;
								}
							}
						}
						else {
							WaypointIndex++;
						}
					}
					vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (GameSettings.BRAKES.GetKey() && vessel.isActiveVessel) || (s.wheelThrottle == 0 && curSpeed < 0.85 && newSpeed < 0.85));
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

			if (controlSpeed)
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
			if (!core.GetComputerModule<MechJebModuleRoverWaypointWindow>().enabled) {
				Waypoints.ForEach(wp => wp.Update());
			}
		}
		
		public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
		{
			Debug.Log(local);
			if (local != null) {
				ConfigNode wps = local.GetNode("Waypoints");
				if (wps != null && wps.HasNode("Waypoint")) {
					int.TryParse(wps.GetValue("Index"), out WaypointIndex);
					Waypoints.Clear();
					foreach (ConfigNode cn in wps.GetNodes("Waypoint")) {
						Debug.Log(cn);
						Waypoints.Add(new MechJebRoverWaypoint(cn));
						Debug.Log(Waypoints[Waypoints.Count - 1].ToConfigNode());
					}
				}
			}
			Debug.Log(Waypoints.Count);
			base.OnLoad(local, type, global);
		}
		
		public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
		{
			if (local.HasNode("Waypoints")) { local.RemoveNode("Waypoints"); }
			if (Waypoints.Count > 0) {
				ConfigNode cn = local.AddNode("Waypoints");
				cn.AddValue("Index", WaypointIndex);
				foreach (MechJebRoverWaypoint wp in Waypoints) {
					Debug.Log(wp);
					cn.AddNode(wp.ToConfigNode());
				}
			}
			Debug.Log(local);
			base.OnSave(local, type, global);
		}

		public MechJebModuleRoverController(MechJebCore core) : base(core) { }
	}
}
