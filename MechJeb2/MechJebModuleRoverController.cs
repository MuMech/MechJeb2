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
		//public CelestialBody Body;
		public MechJebRoverWaypoint(double NS, double EW, string Name = "", float Radius = 100) { //, CelestialBody Body = null) {
			this.NS = NS;
			this.EW = EW;
//			if (Body == null) { Body = FlightGlobals.ActiveVessel.orbit.referenceBody; }
			var Body = FlightGlobals.ActiveVessel.orbit.referenceBody;
			this.Radius = Radius; // radius for considering the waypoint reached in meter
			this.Position = Body.GetWorldSurfacePosition(NS, EW, Body.TerrainAltitude(NS, EW));
			this.Name = Name;
//			this.Body = Body;
		}
		
		public MechJebRoverWaypoint(Vector3d Position, string Name = "", float Radius = 100) { //, CelestialBody Body = null) {
			this.Position = Position;
			this.Radius = Radius;
//			if (Body == null) { Body = FlightGlobals.ActiveVessel.orbit.referenceBody; }
			var Body = FlightGlobals.ActiveVessel.orbit.referenceBody;
			this.NS = Body.GetLatitude(Position);
			this.EW = Body.GetLongitude(Position);
			this.Name = Name;
//			this.Body = Body;
		}
		
		public override string ToString()
		{
			return string.Format("[MechJebRoverWaypoint NS={0:F3}, EW={1:F3}, Position={2}, Radius={3:F3}, Name={4}]", NS, EW, (Vector3)Position, Radius, Name);
		}		
	}
	
	public class MechJebModuleRoverController : ComputerModule
    {
    	public List<MechJebRoverWaypoint> Waypoints = new List<MechJebRoverWaypoint>();
    	public int WaypointIndex = -1;
    	private CelestialBody lastBody = null;
    	
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
        	double tgtSpeed = speed;
        	
        	if (orbit.referenceBody != lastBody) { WaypointIndex = -1; Waypoints.Clear(); }
        	MechJebRoverWaypoint wp = (WaypointIndex > -1 ? Waypoints[WaypointIndex] : null);
        	
        	if (wp != null) { // && wp.Body == orbit.referenceBody) {
        		if (controlHeading) {
        			heading = Math.Round(HeadingToPos(vessel.transform.position, wp.Position), 1);
                }
                if (controlSpeed) {
        			var distance = Vector3.Distance(vessel.transform.position, wp.Position);
        			var curSpeed = vesselState.speedSurface;
        			tgtSpeed = Math.Round(Math.Min(speed, (distance - wp.Radius - (curSpeed * curSpeed * 2)) / 10), 1);
//        			tgtSpeed = (tgtSpeed >= 0 ? tgtSpeed : curSpeed - 1);
        			tgtSpeed = Math.Max(tgtSpeed - Math.Abs(headingErr), 5); // limit speed for approaching waypoints to 5m/s and also limit the speed when doing sharp turns
        			if (distance < (wp.Radius > 0 ? wp.Radius : 25)) {
        				tgtSpeed = (tgtSpeed > 1 ? tgtSpeed : 1); // limit speed so it'll only go from 1m/s to full stop when braking to prevent accidents on moons
        				if (WaypointIndex + 1 >= Waypoints.Count) {
                    		controlHeading = false;
                    		tgtSpeed = -0.5;
                    		if (curSpeed < 1) {
                    			WaypointIndex = -1;
                    			ControlSpeed = false;
                    			vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
                    		}
        				}
        				else {
        					WaypointIndex++;
        				}
                    }
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
                if (s.wheelSteer == s.wheelSteerTrim)
                {
                    double act = headingPID.Compute(headingErr);
                    s.wheelSteer = Mathf.Clamp((float)act, -1, 1);
                }
            }

            if (controlSpeed)
            {
                if (speed != speedLast)
                {
                    speedPID.Reset();
                    speedLast = speed;
                }

                speedErr = tgtSpeed - Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.forward);
                if (s.wheelThrottle == s.wheelThrottleTrim)
                {
                    double act = speedPID.Compute(speedErr);
                    s.wheelThrottle = Mathf.Clamp((float)act, -1, 1);
                }
            }
        }

        public MechJebModuleRoverController(MechJebCore core) : base(core) { }
    }
}
