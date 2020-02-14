using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleRoverController : ComputerModule
	{
		public List<MechJebWaypoint> Waypoints = new List<MechJebWaypoint>();
		public int WaypointIndex = -1;
		private CelestialBody lastBody = null;
		public bool LoopWaypoints = false;
		
//		protected bool controlHeading;
		[ToggleInfoItem("#MechJeb_ControlHeading", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]//Heading control
		public bool ControlHeading;  // TODO: change things back to properties when ConfigNodes can save and load these
//		{
//			get { return controlHeading; }
//			set
//			{
//				controlHeading = value;
//                if (controlHeading || controlSpeed || brakeOnEject)
//				{
//					users.Add(this);
//				}
//				else
//				{
//					users.Remove(this);
//				}
//			}
//		}

		[EditableInfoItem("#MechJeb_Heading", InfoItem.Category.Rover, width = 40), Persistent(pass = (int)Pass.Local)]//Heading
		public EditableDouble heading = 0;

//		protected bool controlSpeed = false;
		[ToggleInfoItem("#MechJeb_ControlSpeed", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]//Speed control
		public bool ControlSpeed = false;
//		{
//			get { return controlSpeed; }
//			set
//			{
//				controlSpeed = value;
//                if (controlHeading || controlSpeed || brakeOnEject)
//				{
//					users.Add(this);
//				}
//				else
//				{
//					users.Remove(this);
//				}
//			}
//		}

		[EditableInfoItem("#MechJeb_Speed", InfoItem.Category.Rover, width = 40), Persistent(pass = (int)Pass.Local)]//Speed
		public EditableDouble speed = 10;

        [ToggleInfoItem("#MechJeb_BrakeOnEject", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]//Brake on Pilot Eject
        public bool BrakeOnEject = false;

        [ToggleInfoItem("#MechJeb_BrakeOnEnergyDepletion", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]//Brake on Energy Depletion
		public bool BrakeOnEnergyDepletion = false;
		
        [ToggleInfoItem("#MechJeb_WarpToDaylight", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]//Warp until Day if Depleted
		public bool WarpToDaylight = false;
		public bool waitingForDaylight = false;

		[ToggleInfoItem("#MechJeb_StabilityControl", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local)]//Stability Control
		public bool StabilityControl = false;

		[ToggleInfoItem("#MechJeb_LimitAcceleration", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Local | (int)Pass.Type)]//Limit Acceleration
		public bool LimitAcceleration = false;

		public PIDController headingPID;
		public PIDController speedPID;

//		private LineRenderer line;
		
		[EditableInfoItem("#MechJeb_SafeTurnspeed", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Type)]//Safe turnspeed
		public EditableDouble turnSpeed = 3;
		[EditableInfoItem("#MechJeb_TerrainLookAhead", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Terrain Look Ahead
		public EditableDouble terrainLookAhead = 1.0;
		[EditableInfoItem("#MechJeb_BrakeSpeedLimit", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Type)]//Brake Speed Limit
		public EditableDouble brakeSpeedLimit = 0.7;

		[EditableInfoItem("#MechJeb_HeadingPIDP", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Heading PID P
		public EditableDouble hPIDp = 0.03; // 0.01
		[EditableInfoItem("#MechJeb_HeadingPIDI", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Heading PID I
		public EditableDouble hPIDi = 0.002; // 0.001
		[EditableInfoItem("#MechJeb_HeadingPIDD", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Heading PID D
		public EditableDouble hPIDd = 0.005;
		
		[EditableInfoItem("#MechJeb_SpeedPIDP", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Speed PID P
		public EditableDouble sPIDp = 2.0;
		[EditableInfoItem("#MechJeb_SpeedPIDI", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Speed PID I
		public EditableDouble sPIDi = 0.1;
		[EditableInfoItem("#MechJeb_SpeedPIDD", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Global)]//Speed PID D
		public EditableDouble sPIDd = 0.001;
		
		[ValueInfoItem("#MechJeb_SpeedIntAcc", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")]//Speed Int Acc
		public double speedIntAcc = 0;

		[ValueInfoItem("#MechJeb_Traction", InfoItem.Category.Rover, format = "F0", units = "%")]//Traction
		public float traction = 0;
		[EditableInfoItem("#MechJeb_TractionBrakeLimit", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Type)]//Traction Brake Limit
		public EditableDouble tractionLimit = 75;
		
		public List<Part> wheels = new List<Part>();
		public List<WheelCollider> colliders = new List<WheelCollider>();
		public Vector3 norm = Vector3.zero;
		
		public override void OnStart(PartModule.StartState state)
		{
			headingPID = new PIDController(hPIDp, hPIDi, hPIDd);
			speedPID = new PIDController(sPIDp, sPIDi, sPIDd);
			
			if (HighLogic.LoadedSceneIsFlight && orbit != null)
			{
				lastBody = orbit.referenceBody;
			}
			
//			MechJebRouteRenderer.NewLineRenderer(ref line);
//			line.enabled = false;
			
			GameEvents.onVesselWasModified.Add(OnVesselModified);
			
			base.OnStart(state);
		}
		
		public void OnVesselModified(Vessel v)
		{

            //TODO : need to look into the new ModuleWheelSteering ModuleWheelMotor ModuleWheelBrakes ModuleWheelBase ModuleWheelSuspension and see what they could bring

            try
            {
				wheels.Clear();
				wheels.AddRange(vessel.Parts.FindAll(p => p.HasModule<ModuleWheelBase>() /*&& p.FindModelComponent<WheelCollider>() != null*/ && p.GetModule<ModuleWheelBase>().wheelType != WheelType.LEG));
				colliders.Clear();
				wheels.ForEach(p => colliders.AddRange(p.FindModelComponents<WheelCollider>()));
			}
			catch (Exception) {}
		}
		
		[ValueInfoItem("#MechJeb_Headingerror", InfoItem.Category.Rover, format = "F1", units = "º")]//Heading error
		public double headingErr;
		[ValueInfoItem("#MechJeb_Speederror", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")]//Speed error
		public double speedErr;
		public double tgtSpeed;
		public MuMech.MovingAverage etaSpeed = new MovingAverage(50);
		private double lastETA = 0;
		private float lastThrottle = 0;
		double curSpeed;

		public double HeadingToPos(Vector3 fromPos, Vector3 toPos)
		{
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
            return (diff < 0 ? -1 : 1) * Vector3.Angle(Vector3d.Exclude(myPos.normalized, north.normalized), Vector3.ProjectOnPlane(tgtPos.normalized, myPos.normalized));
		}

		public float TurningSpeed(double speed, double error)
		{
			return (float)Math.Max(speed / (Math.Abs(error) / 3 > 1 ? Math.Abs(error) / 3 : 1), turnSpeed);
		}
		
		public void CalculateTraction()
		{
			if (wheels.Count == 0 && colliders.Count == 0) { OnVesselModified(vessel); }
			RaycastHit hit;
			Physics.Raycast(vessel.CoM + vesselState.surfaceVelocity * terrainLookAhead + vesselState.up * 100, -vesselState.up, out hit, 500, 1 << 15);
			norm = hit.normal;
			traction = 0;
//			foreach (var c in colliders) {
//				//WheelHit hit;
//				//if (c.GetGroundHit(out hit)) { traction += 1; }
//				if (Physics.Raycast(c.transform.position + c.center, -(vesselState.up + norm.normalized) / 2, out hit, c.radius + 1.5f, 1 << 15)) {
//					traction += (1.5f - (hit.distance - c.radius)) * 100;
//				}
//			}

		    for (int i = 0; i < wheels.Count; i++)
		    {
		        var w = wheels[i];
		        if (w.GroundContact)
		        {
		            traction += 100;
		        }
		    }

//		    for (int i = 0; i < colliders.Count; i++)
//		    {
//		        var c = colliders[i];
//		        if (c.isGrounded)
//		        {
//		            traction += 100;
//		        }
//		    }

			traction /= wheels.Count;
		}
		
		public override void OnModuleDisabled()
		{
			if (core.attitude.users.Contains(this))
			{
//				line.enabled = false;
				core.attitude.attitudeDeactivate();
				core.attitude.users.Remove(this);
			}

			base.OnModuleDisabled();
		}
		
		private float Square(float number) { return number * number; }
		private double Square(double number) { return number * number; }
		
		public override void Drive(FlightCtrlState s) // TODO put the brake in when running out of power to prevent nighttime solar failures on hills, or atleast try to
		{ // TODO make distance calculation for 'reached' determination consider the rover and waypoint on sealevel to prevent height differences from messing it up -- should be done now?
			if (orbit.referenceBody != lastBody) { WaypointIndex = -1; Waypoints.Clear(); }
			MechJebWaypoint wp = (WaypointIndex > -1 && WaypointIndex < Waypoints.Count ? Waypoints[WaypointIndex] : null);
			
			var brake = vessel.ActionGroups[KSPActionGroup.Brakes]; // keep brakes locked if they are			
			curSpeed = Vector3d.Dot(vesselState.surfaceVelocity, vesselState.forward);
			
			CalculateTraction();
			speedIntAcc = speedPID.intAccum;
			
			if (wp != null && wp.Body == orbit.referenceBody)
			{
				if (ControlHeading)
				{
					heading.val = Math.Round(HeadingToPos(vessel.CoM, wp.Position), 1);
				}
				if (ControlSpeed)
				{
					var nextWP = (WaypointIndex < Waypoints.Count - 1 ? Waypoints[WaypointIndex + 1] : (LoopWaypoints ? Waypoints[0] : null));
					var distance = Vector3.Distance(vessel.CoM, wp.Position);
					if (wp.Target != null) { distance += (float)(wp.Target.srfSpeed * curSpeed) / 2; }
					// var maxSpeed = (wp.MaxSpeed > 0 ? Math.Min((float)speed, wp.MaxSpeed) : speed); // use waypoints maxSpeed if set and smaller than set the speed or just stick with the set speed
					var maxSpeed = (wp.MaxSpeed > 0 ? wp.MaxSpeed : speed); // speed used to go towards the waypoint, using the waypoints maxSpeed if set or just stick with the set speed
					var minSpeed = (wp.MinSpeed > 0 ? wp.MinSpeed :
					               (nextWP != null ? TurningSpeed((nextWP.MaxSpeed > 0 ? nextWP.MaxSpeed : speed), heading - HeadingToPos(wp.Position, nextWP.Position)) :
					               (distance - wp.Radius > 50 ? turnSpeed.val : 1)));
					minSpeed = (wp.Quicksave ? 1 : minSpeed);
					// ^ speed used to go through the waypoint, using half the set speed or maxSpeed as minSpeed for routing waypoints (all except the last)
					var newSpeed = Math.Min(maxSpeed, Math.Max((distance - wp.Radius) / curSpeed, minSpeed)); // brake when getting closer
					newSpeed = (newSpeed > turnSpeed ? TurningSpeed(newSpeed, headingErr) : newSpeed); // reduce speed when turning a lot
//					if (LimitAcceleration) { newSpeed = curSpeed + Mathf.Clamp((float)(newSpeed - curSpeed), -1.5f, 0.5f); }
//					newSpeed = tgtSpeed + Mathf.Clamp((float)(newSpeed - tgtSpeed), -Time.deltaTime * 8f, Time.deltaTime * 2f);
					var radius = Math.Max(wp.Radius, 10);
					if (distance < radius)
					{
						if (WaypointIndex + 1 >= Waypoints.Count) // last waypoint
						{
							newSpeed = new [] { newSpeed, (distance < radius * 0.8 ? 0 : 1) }.Min();
							// ^ limit speed so it'll only go from 1m/s to full stop when braking to prevent accidents on moons
							if (LoopWaypoints)
							{
								WaypointIndex = 0;
							}
							else
							{
								newSpeed = 0;
								brake = true;
//								tgtSpeed.force(newSpeed);
								if (curSpeed < brakeSpeedLimit)
								{
									if (wp.Quicksave)
									{
										//if (s.mainThrottle > 0) { s.mainThrottle = 0; }
										if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR)
										{
											WaypointIndex = -1;
											ControlHeading = ControlSpeed = false;
											QuickSaveLoad.QuickSave();
										}
									}
									else
									{
										WaypointIndex = -1;
										ControlHeading = ControlSpeed = false;
									}
								}
//								else {
//									Debug.Log("Is this even getting called?");
//									WaypointIndex++;
//								}
							}
						}
						else
						{
							if (wp.Quicksave)
							{
								//if (s.mainThrottle > 0) { s.mainThrottle = 0; }
								newSpeed = 0;
//								tgtSpeed.force(newSpeed);
								if (curSpeed < brakeSpeedLimit)
								{
									if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR)
									{
										WaypointIndex++;
										QuickSaveLoad.QuickSave();
									}
								}
							}
							else
							{
								WaypointIndex++;
							}
						}
					}
					brake = brake || ((s.wheelThrottle == 0 || !vessel.isActiveVessel) && curSpeed < brakeSpeedLimit && newSpeed < brakeSpeedLimit);
					// ^ brake if needed to prevent rolling, hopefully
					tgtSpeed = (newSpeed >= 0 ? newSpeed : 0);
				}
			}
			
			if (ControlHeading)
			{
				headingPID.intAccum = Mathf.Clamp((float)headingPID.intAccum, -1, 1);

				double instantaneousHeading = vesselState.rotationVesselSurface.eulerAngles.y;
				headingErr = MuUtils.ClampDegrees180(instantaneousHeading - heading);
				if (s.wheelSteer == s.wheelSteerTrim || FlightGlobals.ActiveVessel != vessel)
				{
					float limit = (Math.Abs(curSpeed) > turnSpeed ? Mathf.Clamp((float)((turnSpeed + 6) / Square(curSpeed)), 0.1f, 1f) : 1f);
					// turnSpeed needs to be higher than curSpeed or it will never steer as much as it could even at 0.2m/s above it
					// double act = headingPID.Compute(headingErr * headingErr / 10 * Math.Sign(headingErr));
					double act = headingPID.Compute(headingErr);
					if (traction >= tractionLimit) {
						s.wheelSteer = Mathf.Clamp((float)act, -limit, limit);
						// prevents it from flying above a waypoint and landing with steering at max while still going fast
					}
				}
			}
			
			// Brake if there is no controler (Pilot eject from seat)
			if (BrakeOnEject && vessel.GetReferenceTransformPart() == null)
			{
				s.wheelThrottle = 0;
//				vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
				brake = true;
			}
			else if (ControlSpeed)
			{
				speedPID.intAccum = Mathf.Clamp((float)speedPID.intAccum, -5, 5);

				speedErr = (WaypointIndex == -1 ? speed.val : tgtSpeed) - Vector3d.Dot(vesselState.surfaceVelocity, vesselState.forward);
				if (s.wheelThrottle == s.wheelThrottleTrim || FlightGlobals.ActiveVessel != vessel)
				{
					float act = (float)speedPID.Compute(speedErr);
					s.wheelThrottle = Mathf.Clamp(act, -1f, 1f);
					// s.wheelThrottle = (!LimitAcceleration ? Mathf.Clamp(act, -1, 1) : // I think I'm using these ( ? : ) a bit too much
						// (traction == 0 ? 0 : (act < 0 ? Mathf.Clamp(act, -1f, 1f) : (lastThrottle + Mathf.Clamp(act - lastThrottle, -0.01f, 0.01f)) * (traction < tractionLimit ? -1 : 1))));
//						(lastThrottle + Mathf.Clamp(act, -0.01f, 0.01f)));
//					Debug.Log(s.wheelThrottle + Mathf.Clamp(act, -0.01f, 0.01f));
					if (curSpeed < 0 & s.wheelThrottle < 0) { s.wheelThrottle = 0; } // don't go backwards
					if (Mathf.Sign(act) + Mathf.Sign(s.wheelThrottle) == 0) { s.wheelThrottle = Mathf.Clamp(act, -1f, 1f); }
					if (speedErr < -1 && StabilityControl && Mathf.Sign(s.wheelThrottle) + Math.Sign(curSpeed) == 0) { // StabilityControl && traction > 50 &&
////						vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
						brake = true;
//						foreach (Part p in wheels) {
//							if (p.GetModule<ModuleWheels.ModuleWheelDamage>().stressPercent >= 0.01) { // #TODO needs adaptive braking
//								brake = false;
//								break;
//							}
//						}
					}
////					else if (!StabilityControl || traction <= 50 || speedErr > -0.2 || Mathf.Sign(s.wheelThrottle) + Mathf.Sign((float)curSpeed) != 0) {
////						vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (GameSettings.BRAKES.GetKey() && vessel.isActiveVessel));
////					}
					lastThrottle = Mathf.Clamp(s.wheelThrottle, -1, 1);
				}
			}
			
			if (StabilityControl)
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
//				                        ((Mathf.Abs(fSpeed) <= turnSpeed ? vesselState.forward : vesselState.surfaceVelocity / 4) - vessel.transform.right * s.wheelSteer) * Mathf.Sign(fSpeed) :
//				                        // ^ and then add the steering
				                        vesselState.forward * 4 - vessel.transform.right * s.wheelSteer * Mathf.Sign(fSpeed) : // and then add the steering
				                        vesselState.surfaceVelocity); // in the air so follow velocity
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
                if (vesselState.torqueAvailable.sqrMagnitude > 0)
				    core.attitude.attitudeTo(quat, AttitudeReference.INERTIAL, this);
//				}
			}
			
			if (BrakeOnEnergyDepletion)
			{
				var batteries = vessel.Parts.FindAll(p => p.Resources.Contains(PartResourceLibrary.ElectricityHashcode) && p.Resources.Get(PartResourceLibrary.ElectricityHashcode).flowState);
				var energyLeft = batteries.Sum(p => p.Resources.Get(PartResourceLibrary.ElectricityHashcode).amount) / batteries.Sum(p => p.Resources.Get(PartResourceLibrary.ElectricityHashcode).maxAmount); 
				var openSolars = vessel.mainBody.atmosphere && // true if in atmosphere and there are breakable solarpanels that aren't broken nor retracted
					vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>().FindAll(p => p.isBreakable && p.deployState != ModuleDeployablePart.DeployState.BROKEN &&
					                                                                         p.deployState != ModuleDeployablePart.DeployState.RETRACTED).Count > 0;
				
				if (openSolars && energyLeft > 0.99)
				{
					vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>().FindAll(p => p.isBreakable &&
					                                                                         p.deployState == ModuleDeployablePart.DeployState.EXTENDED).ForEach(p => p.Retract());
				}
				
				if (energyLeft < 0.05 && Math.Sign(s.wheelThrottle) + Math.Sign(curSpeed) != 0) { s.wheelThrottle = 0; } // save remaining energy by not using it for acceleration
				if (openSolars || energyLeft < 0.03) { tgtSpeed = 0; }
				
				if (curSpeed < brakeSpeedLimit && (energyLeft < 0.05 || openSolars))
				{
					brake = true;
				}

				if (curSpeed < 0.1 && energyLeft < 0.05 && !waitingForDaylight &&
				    vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>().FindAll(p => p.deployState == ModuleDeployablePart.DeployState.EXTENDED).Count > 0)
				{
					waitingForDaylight = true;
				}
			}
			
//			brake = brake && (s.wheelThrottle == 0); // release brake if the user or AP want to drive
			if (s.wheelThrottle != 0 && (Math.Sign(s.wheelThrottle) + Math.Sign(curSpeed) != 0 || curSpeed < 1))
			{
				brake = false; // the AP or user want to drive into the direction of momentum so release the brake
			}
			
			if (vessel.isActiveVessel)
			{
				if (GameSettings.BRAKES.GetKeyUp())
				{
					brake = false; // release the brakes if the user lets go of them
				}
				if (GameSettings.BRAKES.GetKey())
				{
					brake = true; // brake if the user brakes and we aren't about to flip
				}
			}

			tractionLimit = (double)Mathf.Clamp((float)tractionLimit, 0, 100);
			vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, brake && (StabilityControl && (ControlHeading || ControlSpeed) ? traction >= tractionLimit : true));
			// only let go of the brake when losing traction if the AP is driving, otherwise assume the player knows when to let go of it
			// also to not constantly turn off the parking brake from going over a small bump
			if (brake && curSpeed < 0.1) { s.wheelThrottle = 0; }
		}
		
		public override void OnFixedUpdate()
		{
			if (!core.GetComputerModule<MechJebModuleWaypointWindow>().enabled)
			{
				Waypoints.ForEach(wp => wp.Update()); // update waypoints unless the waypoint window is (hopefully) doing that already
			}
			
			if (orbit != null && lastBody != orbit.referenceBody) { lastBody = orbit.referenceBody; }

			headingPID.Kp = hPIDp;
			headingPID.Ki = hPIDi;
			headingPID.Kd = hPIDd;
			speedPID.Kp = sPIDp;
			speedPID.Ki = sPIDi;
			speedPID.Kd = sPIDd;
			
			if (lastETA + 0.2 < DateTime.Now.TimeOfDay.TotalSeconds)
			{
				etaSpeed.value = curSpeed;
				lastETA = DateTime.Now.TimeOfDay.TotalSeconds;
			}
			
			if (!core.GetComputerModule<MechJebModuleRoverWindow>().enabled)
			{
				core.GetComputerModule<MechJebModuleRoverWindow>().OnUpdate(); // update users for Stability Control, Brake on Eject and Brake on Energy Depletion
			}
		}
		
		public override void OnUpdate()
		{
			if (WarpToDaylight && waitingForDaylight && vessel.isActiveVessel)
			{
				var batteries = vessel.Parts.FindAll(p => p.Resources.Contains(PartResourceLibrary.ElectricityHashcode) && p.Resources.Get(PartResourceLibrary.ElectricityHashcode).flowState);
				var energyLeft = batteries.Sum(p => p.Resources.Get(PartResourceLibrary.ElectricityHashcode).amount) / batteries.Sum(p => p.Resources.Get(PartResourceLibrary.ElectricityHashcode).maxAmount);
				
				if (waitingForDaylight)
				{
					if (vessel.FindPartModulesImplementing<ModuleDeployableSolarPanel>().FindAll(p => p.deployState == ModuleDeployablePart.DeployState.EXTENDED).Count == 0)
					{
						waitingForDaylight = false;
					}
					core.warp.WarpRegularAtRate(energyLeft < 0.9 ? 1000 : 50);
					if (energyLeft > 0.99)
					{
						waitingForDaylight = false;
						core.warp.MinimumWarp(false);
					}
				}
			}
			else if (!WarpToDaylight && waitingForDaylight)
			{
				waitingForDaylight = false;
			}

			if (!core.GetComputerModule<MechJebModuleRoverWindow>().enabled)
			{
				core.GetComputerModule<MechJebModuleRoverWindow>().OnUpdate(); // update users for Stability Control, Brake on Eject and Brake on Energy Depletion
			}
			
			if (!StabilityControl && core.attitude.users.Contains(this))
			{
				core.attitude.attitudeDeactivate();
				core.attitude.users.Remove(this);
			}
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
		}
		
		public MechJebModuleRoverController(MechJebCore core) : base(core) { }
	}
}
