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

        public double Kp, Ki, Kd;

        public double Tf = 0.5;         

        public Vector3d lastAct = Vector3d.zero;


        [ToggleInfoItem("Conserve RCS fuel", InfoItem.Category.Thrust)]
        public bool conserveFuel = false;

        [EditableInfoItem("Conserve RCS fuel threshold", InfoItem.Category.Thrust, rightLabel = "m/s")]
        public EditableDouble conserveThreshold = 0.05;

        public MechJebModuleRCSController(MechJebCore core)
            : base(core)
        {
            priority = 600;

            Kd = 0.53 / Tf;
            Kp = Kd / (3 * Math.Sqrt(2) * Tf);
            Ki = Kp / (12 * Math.Sqrt(2) * Tf);
            
            pid = new PIDControllerV2(Kp, Ki, Kd, 1, -1);

        }

        public override void OnModuleEnabled()
        {
            pid = new PIDControllerV2(Kp, Ki, Kd, 1, -1);
            lastAct = Vector3d.zero;
            base.OnModuleEnabled();
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
            Vector3d worldVelocityDelta = vesselState.velocityVesselOrbit - targetVelocity;
            worldVelocityDelta += TimeWarp.fixedDeltaTime * vesselState.gravityForce; //account for one frame's worth of gravity
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
                    if (vesselState.rcsThrustAvailable[dir] > 0)
                    {
                        double dV = Vector3d.Dot(velocityDelta, Vector6.directions[dir]) / (vesselState.rcsThrustAvailable[dir] * TimeWarp.fixedDeltaTime / vesselState.mass);
                        if (dV > 0)
                        {
                            rcs += Vector6.directions[dir] * dV;
                        }
                    }
                }

                rcs = pid.Compute(rcs, (rcs - lastAct) / TimeWarp.fixedDeltaTime);

                // low pass filter,  wf = 1/Tf:
                Vector3d act = lastAct + (rcs - lastAct) * (1 / ((Tf / TimeWarp.fixedDeltaTime) + 1));                      
                lastAct = act;

                s.X = Mathf.Clamp((float)act.x, -1, 1);
                s.Y = Mathf.Clamp((float)act.z, -1, 1); //note that z and
                s.Z = Mathf.Clamp((float)act.y, -1, 1); //y must be swapped
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
