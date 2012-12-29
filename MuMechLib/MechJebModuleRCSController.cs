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

        public MechJebModuleRCSController(MechJebCore core) : base(core) { }

        public void SetTargetWorldVelocity(Vector3d vel)
        {
            targetVelocity = vel;
        }

        public void SetTargetLocalVelocity(Vector3d vel)
        {
            targetVelocity = part.vessel.GetTransform().rotation * vel;
        }

        public override void Drive(FlightCtrlState s)
        {
            Vector3d velocityDelta = Quaternion.Inverse(part.vessel.GetTransform().rotation) * (vesselState.velocityVesselOrbit - targetVelocity);
            Vector3d rcs = new Vector3d();

            foreach (Vector6.Direction dir in Enum.GetValues(typeof(Vector6.Direction)))
            {
                if (vesselState.rcsThrustAvailable[dir] > 0)
                {
                    double dV = Vector3d.Dot(velocityDelta, Vector6.directions[dir]) / (vesselState.rcsThrustAvailable[dir] / vesselState.mass);
                    if (dV > 0)
                    {
                        rcs += Vector6.directions[dir] * Math.Min(1, dV);
                    }
                }
            }

            s.X = (float)rcs.x;
            s.Y = (float)rcs.z;
            s.Z = (float)rcs.y;

            base.Drive(s);
        }
    }
}
