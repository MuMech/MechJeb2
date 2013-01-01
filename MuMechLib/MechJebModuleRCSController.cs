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

        public PIDController xPID, yPID, zPID;

        public double Kp = 0.5, Ki = 0, Kd = 0;

        public MechJebModuleRCSController(MechJebCore core) : base(core)
        {
            priority = 600;
            xPID = new PIDController(Kp, Ki, Kd, 1, -1);
            yPID = new PIDController(Kp, Ki, Kd, 1, -1);
            zPID = new PIDController(Kp, Ki, Kd, 1, -1);
        }

        public override void OnModuleEnabled()
        {
            xPID = new PIDController(Kp, Ki, Kd, 1, -1);
            yPID = new PIDController(Kp, Ki, Kd, 1, -1);
            zPID = new PIDController(Kp, Ki, Kd, 1, -1);
            base.OnModuleEnabled();
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

            rcs = new Vector3d(xPID.Compute(rcs.x), yPID.Compute(rcs.y), zPID.Compute(rcs.z));

            s.X = Mathf.Clamp((float)rcs.x, -1, 1);
            s.Y = Mathf.Clamp((float)rcs.z, -1, 1);
            s.Z = Mathf.Clamp((float)rcs.y, -1, 1);

            base.Drive(s);
        }
    }
}
