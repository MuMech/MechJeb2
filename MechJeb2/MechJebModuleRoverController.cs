using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
	public class MechJebRoverWaypoint {
		public double NS;
		public double EW;
		public Vector3d Position;
		public float Radius;
		public string Name;
		public Vessel Target;
		//public CelestialBody Body;
		public MechJebRoverWaypoint(double NS, double EW, float Radius = 50, string Name = "") { //, CelestialBody Body = null) {
			this.NS = NS;
			this.EW = EW;
//			if (Body == null) { Body = FlightGlobals.ActiveVessel.orbit.referenceBody; }
			var Body = FlightGlobals.ActiveVessel.mainBody;
			this.Radius = Radius; // radius for considering the waypoint reached in meter
			this.Position = Body.GetWorldSurfacePosition(NS, EW, Body.TerrainAltitude(NS, EW));
			this.Name = Name + (Name != "" ? " - " : "") + ((NS >= 0 ? "N " : "S ") + Math.Abs(Math.Round(NS, 3)) + ", " + (EW >= 0 ? "E " : "W ") + Math.Abs(Math.Round(EW, 3)));
//			this.Body = Body;
		}
		
		public MechJebRoverWaypoint(Vector3d Position, float Radius = 50, string Name = "") { //, CelestialBody Body = null) {
			this.Position = Position;
			this.Radius = Radius;
//			if (Body == null) { Body = FlightGlobals.ActiveVessel.orbit.referenceBody; }
			var Body = FlightGlobals.ActiveVessel.mainBody;
			this.NS = Body.GetLatitude(Position);
			this.EW = Body.GetLongitude(Position);
			this.Name = Name + (Name != "" ? " - " : "") + ((NS >= 0 ? "N " : "S ") + Math.Abs(Math.Round(NS, 3)) + ", " + (EW >= 0 ? "E " : "W ") + Math.Abs(Math.Round(EW, 3)));
//			this.Body = Body;
		}
		
		public MechJebRoverWaypoint(Vessel Target, float Radius = 50) {
			this.Position = Target.CoM;
			this.Radius = Radius;
//			if (Body == null) { Body = FlightGlobals.ActiveVessel.orbit.referenceBody; }
			var Body = Target.mainBody;
			this.NS = Body.GetLatitude(Position);
			this.EW = Body.GetLongitude(Position);
			this.Name = Target.vesselName + " - " + ((NS >= 0 ? "N " : "S ") + Math.Abs(Math.Round(NS, 3)) + ", " + (EW >= 0 ? "E " : "W ") + Math.Abs(Math.Round(EW, 3)));
			this.Target = Target;
//			this.Body = Body;
		}
		
		public void Update() {
			if (Target != null && Target.vesselType != VesselType.Flag) {// && Vector3d.Distance(Target.CoM, FlightGlobals.ActiveVessel.CoM) < 3000) {
				Position = Target.CoM;
				NS = Target.orbit.referenceBody.GetLatitude(Position);
				EW = Target.orbit.referenceBody.GetLongitude(Position);
				Name = Target.vesselName + " - " + ((NS >= 0 ? "N " : "S ") + Math.Abs(Math.Round(NS, 3)) + ", " + (EW >= 0 ? "E " : "W ") + Math.Abs(Math.Round(EW, 3)));
			}
		}
		
		public override string ToString()
		{
			Update();
			return string.Format("[MechJebRoverWaypoint NS={0:F3}, EW={1:F3}, Position={2}, Radius={3:F3}, Name={4}, Target={5}]", NS, EW, (Vector3)Position, Radius, Name, Target);
		}		
	}
	
	public class MechJebModuleRoverController : ComputerModule
    {
    	public List<MechJebRoverWaypoint> Waypoints = new List<MechJebRoverWaypoint>();
    	public int WaypointIndex = -1;
    	private CelestialBody lastBody = null;
        public bool loopWaypoints = false;
    	
        public float debug1;
        
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
        
        [EditableInfoItem("Turn speed", InfoItem.Category.Rover), Persistent(pass = (int)Pass.Type)]
        public EditableDouble turnSpeed = 5;

        public override void OnStart(PartModule.StartState state)
        {
        	headingPID = new PIDController(0.05, 0.000001, 0.005);
            speedPID = new PIDController(5, 0.001, 1);
            lastBody = orbit.referenceBody;
            base.OnStart(state);
        }

        [ValueInfoItem("Heading error", InfoItem.Category.Rover, format = "F1", units = "º")]
        public double headingErr;
        [ValueInfoItem("Speed error", InfoItem.Category.Rover, format = ValueInfoItem.SI, units = "m/s")]
        public double speedErr;
        public MuMech.MovingAverage tgtSpeed = new MovingAverage(100, 0);

        protected double headingLast, speedLast;

        public double HeadingToPos(Vector3d fromPos, Vector3d toPos) {
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
        	
        	if (wp != null) { // && wp.Body == orbit.referenceBody) {
        		wp.Update();
        		if (controlHeading) {
        			heading = Math.Round(HeadingToPos(vessel.CoM, wp.Position), 1);
                }
        		if (controlSpeed) {
        			var distance = Vector3.Distance(vessel.CoM, wp.Position);
        			var newSpeed = Math.Round(Math.Min(speed, (distance - wp.Radius - (curSpeed * curSpeed)) * (vesselState.localg / 9.81)), 1);
        			newSpeed = Math.Max(newSpeed - Math.Abs(headingErr), new double[] { speed, turnSpeed, Math.Abs(newSpeed) }.Min());
        			// ^ limit speed for approaching waypoints and turning but also allow going to 0 when getting very close to the waypoint for following a target
        			if (distance < (wp.Radius > 0 ? wp.Radius : 25)) {
        				newSpeed = (newSpeed > 1 ? (distance < (wp.Radius * 0.75) ? 0 : 1) : newSpeed);
        				// ^ limit speed so it'll only go from 1m/s to full stop when braking to prevent accidents on moons
        				if (WaypointIndex + 1 >= Waypoints.Count) {
        					if (loopWaypoints) {
        						WaypointIndex = 0;
        						vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, (curSpeed < 0.85 && newSpeed < 0.85)); // brake if needed to prevent rolling, hopefully
        					}
        					else {
	        					controlHeading = false;
	        					newSpeed = -1;
	        					if (curSpeed < 0.85) {
	        						WaypointIndex = -1;
	        						ControlSpeed = false;
	        						vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
	        					}
        					}
        				}
        				else {
        					WaypointIndex++;
        				}
        			}
        			tgtSpeed.value = newSpeed;
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
                	float limit = (curSpeed < turnSpeed ? 1 : Mathf.Clamp((float)((spd * spd) / (curSpeed * curSpeed)), 0.1f, 1f));
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

        public MechJebModuleRoverController(MechJebCore core) : base(core) { }
    }
}
