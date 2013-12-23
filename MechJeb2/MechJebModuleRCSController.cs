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

        public PIDControllerV pid;

        public double Kp = 0.2, Ki = 0, Kd = 0.02;

        Vector3d lastAct = Vector3d.zero;


        [ToggleInfoItem("Conserve RCS fuel", InfoItem.Category.Thrust)]
        public bool conserveFuel = true;

        [EditableInfoItem("Conserve RCS fuel threshold", InfoItem.Category.Thrust, rightLabel = "m/s")]
        public EditableDouble conserveThreshold = 0.05;

        public MechJebModuleRCSController(MechJebCore core)
            : base(core)
        {
            priority = 600;
            pid = new PIDControllerV(Kp, Ki, Kd, 1, -1);
        }

        public override void OnModuleEnabled()
        {
            pid = new PIDControllerV(Kp, Ki, Kd, 1, -1);
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

                //rcs = pid.Compute(rcs, rcs - lastAct); // Having an Omega would be nice but each test made it worse
                rcs = pid.Compute(rcs);

                //rcs = lastAct + (rcs - lastAct) * (1 / ((0.5 / TimeWarp.fixedDeltaTime) + 1));
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
