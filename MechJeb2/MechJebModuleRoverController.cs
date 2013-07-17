using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRoverController : ComputerModule
    {
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
            headingPID = new PIDController(0.05, 0.000001, 0.05);
            speedPID = new PIDController(5, 0.001, 1);
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
        	if (core.target.Target != null && ((core.target.PositionTargetExists && core.target.targetBody == orbit.referenceBody) || core.target.Orbit.referenceBody == orbit.referenceBody)) {
        		var pos = (core.target.PositionTargetExists ? (Vector3)core.target.GetPositionTargetPosition() : core.target.Position);
                if (controlHeading) {
        			heading = Math.Round(HeadingToPos(vessel.transform.position, pos), 1);
                }
                if (controlSpeed) {
        			var curSpeed = vesselState.speedSurface;
                    speed = Math.Round(Math.Min(speed, (core.target.Distance - 100 - (speed * speed * 2)) / 10), 1);
                    if (curSpeed < 0.2 && core.target.Distance < 105) {
                    	controlHeading = controlSpeed = false;
                    	vessel.ActionGroups.SetGroup(KSPActionGroup.Brakes, true);
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

                speedErr = speed - Vector3d.Dot(vesselState.velocityVesselSurface, vesselState.forward);
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
