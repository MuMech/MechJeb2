using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleRoverController : ComputerModule
	{
		public List<MechJebWaypoint> Waypoints = new List<MechJebWaypoint>();
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

//		private LineRenderer line;
		
		[EditableInfoItem("Safe turnspeed", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Type)]
		public EditableDouble turnSpeed = 3;
		[ToggleInfoItem("Stability Control", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]
		public bool stabilityControl = false;
		[EditableInfoItem("Terrain Look Ahead", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble terrainLookAhead = 1.0;
		[EditableInfoItem("Brake Speed Limit", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Type)]
		public EditableDouble brakeSpeedLimit = 0.7;
		[ToggleInfoItem("Brake on Energy Depletion", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]
		public bool BrakeOnEnergyDepletion = false;

		[EditableInfoItem("Heading PID P", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble hPIDp = 0.5;
		[EditableInfoItem("Heading PID I", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble hPIDi = 0.005;
		[EditableInfoItem("Heading PID D", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble hPIDd = 0.025;
		
		[EditableInfoItem("Speed PID P", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble sPIDp = 2.0;
		[EditableInfoItem("Speed PID I", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble sPIDi = 1.0;
		[EditableInfoItem("Speed PID D", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]
		public EditableDouble sPIDd = 0.025;
		
		[ValueInfoItem("Traction", InfoItem.Category.Rover, format = "F0", units = "%")]
		public float traction = 0;
		[ValueInfoItem("Speed Int Acc", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")]
		public double speedIntAcc = 0;
		
		public List<Part> wheels = new List<Part>();
		public List<WheelCollider> colliders = new List<WheelCollider>();
		public Vector3 norm = Vector3.zero;
		
		public override void OnStart(PartModule.StartState state)
		{
			headingPID = new PIDController(hPIDp, hPIDi, hPIDd);//, 10, -10);
			speedPID = new PIDController(sPIDp, sPIDi, sPIDd);//, 10, -10);
			if (HighLogic.LoadedSceneIsFlight && orbit != null) {
				lastBody = orbit.referenceBody;
			}
//			MechJebRouteRenderer.NewLineRenderer(ref line);
//			line.enabled = false;
			GameEvents.onVesselWasModified.Add(OnVesselModified);
			base.OnStart(state);
		}
		
		public void OnVesselModified(Vessel v) {
			try {
				colliders.Clear();
				vessel.Parts.ForEach(p => colliders.AddRange(p.FindModelComponents<WheelCollider>()));
				wheels.Clear();
				wheels.AddRange(vessel.Parts.FindAll(p => p.FindModelComponent<WheelCollider>() != null));
			}
			catch (Exception ex) {}
		}
		
		[ValueInfoItem("Heading error", InfoItem.Category.Rover, format = "F1", units = "º")]
		public double headingErr;
		[ValueInfoItem("Speed error", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")]
		public double speedErr;
		public MuMech.MovingAverage tgtSpeed = new MovingAverage(150);
		public MuMech.MovingAverage etaSpeed = new MovingAverage(300);

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
			return (float)Math.Max(speed / (Math.Abs(error) / 3 > 1 ? Math.Abs(error) / 3 : 1), turnSpeed);
		}
		
		public void CalculateTraction() {
			if (wheels.Count == 0 && colliders.Count == 0) { OnVesselModified(vessel); }
			RaycastHit hit;
			Physics.Raycast(vessel.CoM + vessel.srf_velocity * terrainLookAhead + vesselState.up * 100, -vesselState.up, out hit, 500, 1 << 15);
			norm = hit.normal;
			traction = 0;
//			foreach (var c in colliders) {
//				//WheelHit hit;
//				//if (c.GetGroundHit(out hit)) { traction += 1; }
//				if (Physics.Raycast(c.transform.position + c.center, -(vesselState.up + norm.normalized) / 2, out hit, c.radius + 1.5f, 1 << 15)) {
//					traction += (1.5f - (hit.distance - c.radius)) * 100;
//				}
//			}
			
			foreach (var w in wheels) {
				if (w.GroundContact) { traction += 100; }
			}
			traction /= colliders.Count;
		}
		
		public override void OnModuleDisabled()
		{
			if (core.attitude.users.Contains(this)) {
//				line.enabled = false;
				core.attitude.attitudeDeactivate();
				core.attitude.users.Remove(this);
			}
			base.OnModuleDisabled();
		}
		
		public override void Drive(FlightCtrlState s) // TODO put the brake in when running out of power to prevent nighttime solar failures on hills, or atleast try to
		{ // TODO make distance calculation for 'reached' determination consider the rover and waypoint on sealevel to prevent height differences from messing it up -- should be done now?
			if (orbit.referenceBody != lastBody) { WaypointIndex = -1; Waypoints.Clear(); }
			MechJebWaypoint wp = (WaypointIndex > -1 && WaypointIndex < Waypoints.Count ? Waypoints[WaypointIndex] : null);
			
			var brake = vessel.ActionGroups[KSPActionGroup.Brakes]; // keep brakes locked if they are
			if (vessel.isActiveVessel) {
				if (GameSettings.BRAKES.GetKeyUp()) {
					brake = false; // release the brakes if the user lets go of them
				}
				if (GameSettings.BRAKES.GetKey()) {
					brake = true; // brake if the user brakes
				}
			}
			
			var curSpeed = Vector3d.Dot(vessel.srf_velocity, vesselState.forward);
			etaSpeed.value = curSpeed;
			
			CalculateTraction();
			speedIntAcc = speedPID.intAccum;
			
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
					var radius = Math.Max(wp.Radius, 10 / 0.8);
					if (distance < radius) {
						if (WaypointIndex + 1 >= Waypoints.Count) { // last waypoint
							newSpeed = new [] { newSpeed, (distance < radius * 0.8 ? 0 : 1) }.Min();
							// ^ limit speed so it'll only go from 1m/s to full stop when braking to prevent accidents on moons
							if (LoopWaypoints) {
								WaypointIndex = 0;
							}
							else {
								newSpeed = 0;
								tgtSpeed.force(newSpeed);
								if (curSpeed < brakeSpeedLimit) {
									if (wp.Quicksave) {
										//if (s.mainThrottle > 0) { s.mainThrottle = 0; }
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
								//if (s.mainThrottle > 0) { s.mainThrottle = 0; }
								newSpeed = 0;
								tgtSpeed.force(newSpeed);
								if (curSpeed < brakeSpeedLimit) {
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
					brake = brake || ((s.wheelThrottle == 0 || !vessel.isActiveVessel) && curSpeed < brakeSpeedLimit && newSpeed < brakeSpeedLimit);
					// ^ brake if needed to prevent rolling, hopefully
					tgtSpeed.value = Math.Round(newSpeed, 1);
				}
			}
			
			if (controlHeading)
			{
				headingPID.intAccum = Mathf.Clamp((float)headingPID.intAccum, -2, 2);

				double instantaneousHeading = vesselState.rotationVesselSurface.eulerAngles.y;
				headingErr = MuUtils.ClampDegrees180(instantaneousHeading - heading);
				if (s.wheelSteer == s.wheelSteerTrim || FlightGlobals.ActiveVessel != vessel)
				{
					float spd = Mathf.Min((float)speed, (float)turnSpeed); // if a slower speed than the turnspeed is used also be more careful with the steering
					float limit = (Mathf.Abs((float)curSpeed) <= turnSpeed ? 1 : Mathf.Clamp((float)(spd / Mathf.Abs((float)curSpeed)), 0.35f, 1f));
					double act = headingPID.Compute(headingErr);
					s.wheelSteer = Mathf.Clamp((float)act, -limit, limit);
				}
			}
			
			// Brake if there is no controler (Pilot eject from seat)
			if (brakeOnEject && vessel.GetReferenceTransformPart() == null)
			{
				s.wheelThrottle = 0;
//				vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
				brake = true;
			}
			else if (controlSpeed)
			{
				speedPID.intAccum = Mathf.Clamp((float)speedPID.intAccum, -10, 10);

				speedErr = (WaypointIndex == -1 ? speed.val : tgtSpeed.value) - Vector3d.Dot(vessel.srf_velocity, vesselState.forward);
				if (s.wheelThrottle == s.wheelThrottleTrim || FlightGlobals.ActiveVessel != vessel)
				{
					double act = speedPID.Compute(speedErr);
					s.wheelThrottle = Mathf.Clamp((float)act, -1, 1);
					if (stabilityControl && traction > 50 && speedErr < -1 && Mathf.Sign(s.wheelThrottle) + Mathf.Sign((float)curSpeed) == 0) {
//						vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
						brake = true;
					}
//					else if (!stabilityControl || traction <= 50 || speedErr > -0.2 || Mathf.Sign(s.wheelThrottle) + Mathf.Sign((float)curSpeed) != 0) {
//						vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (GameSettings.BRAKES.GetKey() && vessel.isActiveVessel));
//					}
				}
			}
			
			if (stabilityControl)
			{
				if (!core.attitude.users.Contains(this))
				{
					core.attitude.users.Add(this);
//					line.enabled = true;
				}
//				float scale = Vector3.Distance(FlightCamera.fetch.mainCamera.transform.position, vessel.CoM) / 900f;
//				line.SetPosition(0, vessel.CoM);
//				line.SetPosition(1, vessel.CoM + hit.normal * 5);
//				line.SetWidth(0, scale + 0.1f);
				var fSpeed = (float)curSpeed;
//				if (Mathf.Abs(fSpeed) >= turnSpeed * 0.75) {
				Vector3 fwd = (Vector3)(traction > 0 ? // V when the speed is low go for the vessels forward, else with a bit of velocity
//				                        ((Mathf.Abs(fSpeed) <= turnSpeed ? vesselState.forward : vessel.srf_velocity / 4) - vessel.transform.right * s.wheelSteer) * Mathf.Sign(fSpeed) :
//				                        // ^ and then add the steering
				                        vesselState.forward * 4 - vessel.transform.right * s.wheelSteer * Mathf.Sign(fSpeed) : // and then add the steering
				                        vessel.srf_velocity); // in the air so follow velocity
				Vector3.OrthoNormalize(ref norm, ref fwd);
				var quat = Quaternion.LookRotation(fwd, norm);
				
//				if (traction > 0 || speed <= turnSpeed) {
//					var u = new Vector3(0, 1, 0);
//
//					var q = FlightGlobals.ship_rotation;
//					var q_s = quat;
//
//					var q_u = new Quaternion(u.x, u.y, u.z, 0);
//					var a = Quaternion.Dot(q, q_s * q_u);
//					var q_qs = Quaternion.Dot(q, q_s);
//					var b = (a == 0) ? Math.Sign(q_qs) : (q_qs / a);
//					var g = b / Mathf.Sqrt((b * b) + 1);
//					var gu = Mathf.Sqrt(1 - (g * g)) * u;
//					var q_d = new Quaternion() { w = g, x = gu.x, y = gu.y, z = gu.z };
//					var n = q_s * q_d;
//
//					quat = n;
//				}
								
				core.attitude.attitudeTo(quat, AttitudeReference.INERTIAL, this);
//				}
			}
			else if (core.attitude.users.Contains(this))
			{
//				line.enabled = false;
				core.attitude.attitudeDeactivate();
				core.attitude.users.Remove(this);
			}
			
			brake = brake && (s.wheelThrottle == 0); // release brake if the user or AP want to drive
			
			if (BrakeOnEnergyDepletion)
			{
				var energyDown = vessel.Parts.FindAll(p => p.Resources.Contains("ElectricCharge") && p.Resources["ElectricCharge"].flowState && p.Resources["ElectricCharge"].amount > 0).Count == 0;
				var openSolars = vessel.mainBody.atmosphere &&
					vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>().FindAll(p => p.isBreakable && p.panelState != ModuleDeployableSolarPanel.panelStates.BROKEN &&
					                                                                         p.panelState != ModuleDeployableSolarPanel.panelStates.RETRACTED).Count > 0;
				
				if (openSolars)
				{
					s.wheelThrottle = 0;
					if (vessel.Parts.FindAll(p => p.Resources.Contains("ElectricCharge") && p.Resources["ElectricCharge"].flowState && 
					                         p.Resources["ElectricCharge"].amount < p.Resources["ElectricCharge"].maxAmount).Count == 0)
					{
						vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>().FindAll(p => p.isBreakable &&
						                                                                         p.panelState == ModuleDeployableSolarPanel.panelStates.EXTENDED).ForEach(p => p.Retract());
		}
				}
		
				brake = brake || openSolars || (curSpeed < 1 && energyDown);
			}
			
			vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, brake);
		}
		
		public override void OnFixedUpdate()
		{
			if (!core.GetComputerModule<MechJebModuleWaypointWindow>().enabled) { // update waypoints unless the waypoint window is (hopefully) doing that already
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
                        Waypoints.Add(new MechJebWaypoint(cn));
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
				foreach (MechJebWaypoint wp in Waypoints) {
					cn.AddNode(wp.ToConfigNode());
				}
			}
			
			core.GetComputerModule<MechJebModuleWaypointWindow>().OnSave(local, type, global); // to save routes if they might not have been saved yet and the window got closed
		}
		
		public MechJebModuleRoverController(MechJebCore core) : base(core) { }
	}
}
