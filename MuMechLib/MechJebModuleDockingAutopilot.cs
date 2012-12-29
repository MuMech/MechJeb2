using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    class MechJebModuleDockingAutopilot : ComputerModule
    {
        public MechJebModuleDockingAutopilot(MechJebCore core) : base(core) { }

        public string status = "";

        public override void OnModuleEnabled()
        {
            core.attitude.enabled = true;
        }

        public override void OnModuleDisabled()
        {
            core.attitude.enabled = false;
        }


        public override void Drive(FlightCtrlState s)
        {
            if (!Target.Exists()) return;

            core.attitude.attitudeTo(Vector3d.back, AttitudeReference.TARGET_ORIENTATION, this);

            Vector3d targetVel = Target.Orbit().GetVel();

            Vector3d separation = Target.RelativePosition(part.vessel);

            Vector3d zAxis = -Target.Transform().forward; //the docking axis
            double zSep = -Vector3d.Dot(separation, zAxis); //positive if we are in front of the target, negative if behind
            Vector3d lateralSep = Vector3d.Exclude(zAxis, separation);

            Debug.Log("zSep = " + zSep);
            Debug.Log("lateralSep = " + lateralSep.magnitude);

            if (zSep < 0)  //we're behind the target
            {
                if (lateralSep.magnitude < 5) //and we'll hit the target if we back up
                {
                    core.rcs.SetTargetWorldVelocity(targetVel + 1.0 * lateralSep.normalized); //move away from the docking axis
                    status = "Moving away from docking axis at 1 m/s to avoid hitting target on backing up";
                }
                else
                {
                    core.rcs.SetTargetWorldVelocity(targetVel - 1.0 * zAxis); //back up
                    status = "Backing up at 1 m/s to get on the correct side of the target to dock.";
                }
            }
            else //we're in front of the target
            {
                //move laterally toward the docking axis
                Vector3d lateralVelocityNeeded = -lateralSep / 10;
                if (lateralVelocityNeeded.magnitude > 1.0) lateralVelocityNeeded *= (1.0 / lateralVelocityNeeded.magnitude);

                double zVelocityNeeded;

                if (lateralSep.magnitude > 0.2 && lateralSep.magnitude > zSep)
                {
                    //we're very far off the docking axis
                    if (zSep < 10)
                    {
                        //we're far off the docking axis, but our z separation is small. Back up to increase the z separation
                        zVelocityNeeded = -1.0;
                        status = "Backing up and moving toward docking axis.";
                    }
                    else
                    {
                        //we're not extremly close in z, so just stay at this z distance while we fix the lateral separation
                        zVelocityNeeded = 0;
                        status = "Holding still in Z and moving toward the docking axis.";
                    }
                }
                else
                {
                    //we're not extremely far off the docking axis. Approach the along z with a speed determined by our z separation
                    //but limited by how far we are off the axis
                    zVelocityNeeded = 0.2 + 0.02 * zSep;
                    zVelocityNeeded = Math.Min(zVelocityNeeded, (zSep / lateralSep.magnitude) * (0.1 + lateralVelocityNeeded.magnitude));
                    status = "Moving forward to dock.";
                }

                core.rcs.SetTargetWorldVelocity(targetVel + lateralVelocityNeeded + zVelocityNeeded * zAxis);
            }
        }
    }
}
