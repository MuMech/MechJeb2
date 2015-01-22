using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRCSController : ComputerModule
    {
        public Vector3d targetVelocity = Vector3d.zero;

        public PIDControllerV2 pid;
        Vector3d lastAct = Vector3d.zero;


        [Persistent(pass = (int)(Pass.Global))]
        [ToggleInfoItem("Conserve RCS fuel", InfoItem.Category.Thrust)]
        public bool conserveFuel = false;

        [EditableInfoItem("Conserve RCS fuel threshold", InfoItem.Category.Thrust, rightLabel = "m/s")]
        public EditableDouble conserveThreshold = 0.05;

        [Persistent(pass = (int)(Pass.Local| Pass.Type | Pass.Global))]
        [EditableInfoItem("RCS Tf", InfoItem.Category.Thrust)]
        public EditableDouble Tf = 1;

        [Persistent(pass = (int)(Pass.Global))]
        [ToggleInfoItem("RCS throttle when 0kn thrust", InfoItem.Category.Thrust)]
        public bool rcsThrottle = true;

        public MechJebModuleRCSController(MechJebCore core)
            : base(core)
        {
            priority = 600;
            pid = new PIDControllerV2(0, 0, 0, 1, -1);
        }

        public override void OnModuleEnabled()
        {
            setPIDParameters();
            pid.Reset();
            lastAct = Vector3d.zero;
            base.OnModuleEnabled();
        }


        public void setPIDParameters()
        {
            if (Tf < 2 * TimeWarp.fixedDeltaTime)
                Tf = 2 * TimeWarp.fixedDeltaTime;

            pid.Kd = 0.53 / Tf;
            pid.Kp = pid.Kd / (3 * Math.Sqrt(2) * Tf);
            pid.Ki = pid.Kp / (12 * Math.Sqrt(2) * Tf);
        }


        // When evaluating how fast RCS can accelerate and calculate a speed that avaible thrust should
        // be multipled by that since the PID controller actual lower the used accel
        public double rcsAccelFactor()
        {
            return pid.Kp;
        }

        public override void OnModuleDisabled()
        {
            vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
            base.OnModuleDisabled();
        }

        public void SetTargetWorldVelocity(Vector3d vel)
        {
            targetVelocity = vel;
        }

        public void SetTargetLocalVelocity(Vector3d vel)
        {
            targetVelocity = vessel.GetTransform().rotation * vel;
        }

        public override void Drive(FlightCtrlState s)
        {
            setPIDParameters();

            // Removed the gravity since it also affect the target and we don't know the target pos here.
            // Since the difference is negligable for docking it's removed
            // TODO : add it back once we use the RCS Controler for other use than docking
            Vector3d worldVelocityDelta = vesselState.orbitalVelocity - targetVelocity;
            //worldVelocityDelta += TimeWarp.fixedDeltaTime * vesselState.gravityForce; //account for one frame's worth of gravity
            //worldVelocityDelta -= TimeWarp.fixedDeltaTime * gravityForce = FlightGlobals.getGeeForceAtPosition(  Here be the target position  ); ; //account for one frame's worth of gravity
            
            // We work in local vessel coordinate
            Vector3d velocityDelta = Quaternion.Inverse(vessel.GetTransform().rotation) * worldVelocityDelta;
                        
            if (!conserveFuel || (velocityDelta.magnitude > conserveThreshold))
            {
                if (!vessel.ActionGroups[KSPActionGroup.RCS])
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                }

                Vector3d rcs = new Vector3d();

                foreach (Vector6.Direction dir in Enum.GetValues(typeof(Vector6.Direction)))
                {
                    double dirDv = Vector3d.Dot(velocityDelta, Vector6.directions[dir]);
                    double dirAvail = vesselState.rcsThrustAvailable[dir]; 
                    if (dirAvail  > 0 && Math.Abs(dirDv) > 0.001)
                    {
                        double dirAction = dirDv / (dirAvail * TimeWarp.fixedDeltaTime / vesselState.mass);
                        if (dirAction > 0)
                        {
                            rcs += Vector6.directions[dir] * dirAction;
                        }
                    }
                }
                                
                Vector3d omega = Quaternion.Inverse(vessel.GetTransform().rotation) * (vessel.acceleration - vesselState.gravityForce);

                rcs = pid.Compute(rcs, omega);

                // Disabled the low pass filter for now. Was doing more harm than good
                //rcs = lastAct + (rcs - lastAct) * (1 / ((Tf / TimeWarp.fixedDeltaTime) + 1));
                lastAct = rcs;

                s.X = Mathf.Clamp((float)rcs.x, -1, 1);
                s.Y = Mathf.Clamp((float)rcs.z, -1, 1); //note that z and
                s.Z = Mathf.Clamp((float)rcs.y, -1, 1); //y must be swapped
            }
            else if (conserveFuel)
            {
                if (vessel.ActionGroups[KSPActionGroup.RCS])
                {
                    vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, false);
                }
            }

            base.Drive(s);
        }
    }
}
