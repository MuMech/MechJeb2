using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleNodeExecutor : ComputerModule
    {
        //public interface:
        public bool autowarp = true;      //whether to auto-warp to nodes
        public double leadTime = 3;       //how many seconds before a burn to end warp (note that we align with the node before warping)
        public double leadFraction = 0.5; //how early to start the burn, given as a fraction of the burn time
        public double precision = 0.1;    //we decide we're finished the burn when the remaining dV falls below this value (in m/s)

        public void ExecuteOneNode(object controller)
        {
            mode = Mode.ONE_NODE;
            users.Add(controller);
            burnTriggered = false;
            alignedForBurn = false;
        }

        public void ExecuteAllNodes(object controller)
        {
            mode = Mode.ALL_NODES;
            users.Add(controller);
            burnTriggered = false;
            alignedForBurn = false;
        }

        public void Abort()
        {
            core.warp.MinimumWarp();
            users.Clear();
        }

        public override void OnModuleEnabled()
        {
            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
        }

        protected enum Mode { ONE_NODE, ALL_NODES };
        protected Mode mode = Mode.ONE_NODE;

        public bool burnTriggered = false;
        public bool alignedForBurn = false;

        public override void OnFixedUpdate()
        {
            if (!vessel.patchedConicSolver.maneuverNodes.Any())
            {
                Abort();
                return;
            }

            //check if we've finished a node:
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double dVLeft = node.GetBurnVector(orbit).magnitude;

            if (dVLeft < precision)
            {
                burnTriggered = false;

                vessel.patchedConicSolver.RemoveManeuverNode(node);

                if (mode == Mode.ONE_NODE)
                {
                    Abort();
                    return;
                }
                else if (mode == Mode.ALL_NODES)
                {
                    if (!vessel.patchedConicSolver.maneuverNodes.Any())
                    {
                        Abort();
                        return;
                    }
                    else
                    {
                        node = vessel.patchedConicSolver.maneuverNodes.First();
                    }
                }
            }

            //aim along the node
            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE, this);

            double burnTime = dVLeft / vesselState.maxThrustAccel;
            if (core.thrust != null && core.thrust.enabled) burnTime /= core.thrust.throttleLimit; //account for throttle limits
            
            double timeToNode = node.UT - vesselState.time;

            if (timeToNode < burnTime * leadFraction)
            {
                burnTriggered = true;
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
            }

            //autowarp, but only if we're already aligned with the node
            if (autowarp && !burnTriggered)
            {
                if (core.attitude.attitudeAngleFromTarget() < 1 || (core.attitude.attitudeAngleFromTarget() < 10 && !MuUtils.PhysicsRunning()))
                {
                    core.warp.WarpToUT(node.UT - burnTime * leadFraction - leadTime);
                }
                else if (!MuUtils.PhysicsRunning() && core.attitude.attitudeAngleFromTarget() > 10)
                {
                    core.warp.MinimumWarp();
                }
            }

            core.thrust.targetThrottle = 0;

            if (burnTriggered)
            {
                if (alignedForBurn)
                {
                    if (core.attitude.attitudeAngleFromTarget() < 90)
                    {
                        double timeConstant = (dVLeft > 10 ? 0.5 : 2);
                        double desiredAcceleration = dVLeft / timeConstant;
                        desiredAcceleration = Math.Max(precision, desiredAcceleration);

                        core.thrust.targetThrottle = Mathf.Clamp01((float)(desiredAcceleration / vesselState.maxThrustAccel));
                    }
                    else
                    {
                        alignedForBurn = false;
                    }
                }
                else
                {
                    if (core.attitude.attitudeAngleFromTarget() < 2)
                    {
                        alignedForBurn = true;
                    }
                }
            }
        }

        public MechJebModuleNodeExecutor(MechJebCore core) : base(core) { }
    }
}
