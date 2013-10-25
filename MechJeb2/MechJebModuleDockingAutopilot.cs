using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleDockingAutopilot : ComputerModule
    {
        public string status = "";

        public double approachSpeedMult = 0.3; // Approach speed will be approachSpeedMult * maximum safe speed on each axis.

        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        [EditableInfoItem("Docking speed limit", InfoItem.Category.Thrust, rightLabel = "m/s")]
        public EditableDouble speedLimit = 1;
        [Persistent(pass = (int)Pass.Local)]
        public EditableDouble rol = new EditableDouble(0);
        [Persistent(pass = (int)Pass.Local)]
        public Boolean forceRol = false;

        public MechJebModuleDockingAutopilot(MechJebCore core)
            : base(core)
        {
        }

        public override void OnModuleEnabled()
        {
            core.rcs.users.Add(this);
            core.attitude.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.rcs.users.Remove(this);
            core.attitude.attitudeDeactivate();
        }

        private double FixSpeed(double s)
        {
            if (speedLimit != 0)
            {
                if (s >  speedLimit) s =  speedLimit;
                if (s < -speedLimit) s = -speedLimit;
            }
            return s;
        }

        public override void Drive(FlightCtrlState s)
        {
            if (!core.target.NormalTargetExists)
            {
                users.Clear();
                return;
            }

            if (forceRol)
                core.attitude.attitudeTo(Quaternion.LookRotation(Vector3d.back, Vector3d.up) * Quaternion.AngleAxis(-(float)rol, Vector3d.back), AttitudeReference.TARGET_ORIENTATION, this);
            else
                core.attitude.attitudeTo(Vector3d.back, AttitudeReference.TARGET_ORIENTATION, this);

            Vector3d targetVel = core.target.Orbit.GetVel();

            Vector3d separation = core.target.RelativePosition;

            Vector3d zAxis = core.target.DockingAxis;
            double zSep = -Vector3d.Dot(separation, zAxis); //positive if we are in front of the target, negative if behind
            Vector3d lateralSep = Vector3d.Exclude(zAxis, separation);

            // v2 = u2 + 2 * a * s
            // initial speed v - final speed u = 0  - distance s
            // Maximum speed to brake in time = sqrt( 2 * a * s )
            double zApproachSpeed   = FixSpeed( approachSpeedMult * Math.Sqrt( 2 * Math.Abs(zSep) * vesselState.rcsThrustAvailable.GetMagnitude(-zAxis) / vesselState.mass ));
            double latApproachSpeed = FixSpeed( approachSpeedMult * Math.Sqrt( 2 * lateralSep.magnitude * vesselState.rcsThrustAvailable.GetMagnitude(-lateralSep) / vesselState.mass));

            if (zSep < 0)  //we're behind the target
            {
                // TODO : Compare to the vessels size to move around it safely
                if (lateralSep.magnitude < 10) //and we'll hit the target if we back up
                {
                    core.rcs.SetTargetWorldVelocity(targetVel + zApproachSpeed * lateralSep.normalized); //move away from the docking axis
                    status = "Moving away from docking axis at " + zApproachSpeed.ToString("F2") + " m/s to avoid hitting target on backing up";
                }
                else
                {
                    double backUpSpeed = FixSpeed(-zApproachSpeed * Math.Max(1, -zSep / 50));
                    core.rcs.SetTargetWorldVelocity(targetVel + backUpSpeed * zAxis); //back up
                    status = "Backing up at " + backUpSpeed.ToString("F2") + " m/s to get on the correct side of the target to dock.";
                }
            }
            else //we're in front of the target
            {
                //move laterally toward the docking axis
                Vector3d lateralVelocityNeeded = -lateralSep.normalized * latApproachSpeed;

                double zVelocityNeeded = zApproachSpeed;
                
                if (lateralSep.magnitude > 0.2 && lateralSep.magnitude * 10 > zSep)
                {
                    //we're very far off the docking axis
                    if (zSep < lateralSep.magnitude)
                    {
                        //we're far off the docking axis, but our z separation is small. Back up to increase the z separation
                        zVelocityNeeded *= -1;
                        status = "Backing at " + zVelocityNeeded.ToString("F2") + " m/s up and moving toward docking axis.";
                    }
                    else
                    {
                        //we're not extremely close in z, so keep moving in z but slow enough that we are on docking axis before contact
                        zVelocityNeeded = Math.Min(zVelocityNeeded, (Math.Max(zSep, 2) * lateralVelocityNeeded.magnitude) / lateralSep.magnitude);
                        status = "Moving toward the docking axis and dock at " + lateralVelocityNeeded.magnitude.ToString("F2") + " m/s.";
                    }
                }
                else
                {
                    if (zSep > 0.4)
                    {
                        //we're not extremely far off the docking axis. Approach the along z with a speed determined by our z separation
                        //but limited by how far we are off the axis
                        status = "Moving forward to dock at " + zVelocityNeeded.ToString("F2") + " m/s.";
                    }
                    else
                    {
                        // close enough, turn it off and let the magnetic dock work
                        users.Clear();
                        enabled = false;
                        return;
                    }
                }

                Vector3d adjustment = lateralVelocityNeeded + zVelocityNeeded * zAxis.normalized;
                double magnitude = adjustment.magnitude;
                if (magnitude > 0) adjustment *= FixSpeed(magnitude) / magnitude;
                core.rcs.SetTargetWorldVelocity(targetVel + adjustment);
            }
        }
    }
}
