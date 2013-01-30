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
        public double leadFraction = 1.0; //how early to start the burn, given as a fraction of the burn time
        public double precision = 0.1;    //we decide we're finished the burn when the remaining dV falls below this value (in m/s)

        public void ExecuteOneNode()
        {
            mode = Mode.ONE_NODE;
            enabled = true;
            burnTriggered = false;
        }

        public void ExecuteAllNodes()
        {
            mode = Mode.ALL_NODES;
            enabled = true;
            burnTriggered = false;
        }

        public override void OnModuleEnabled()
        {
            core.attitude.users.Add(this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.users.Remove(this);
        }

        protected enum Mode { ONE_NODE, ALL_NODES };
        protected Mode mode = Mode.ONE_NODE;

        protected bool burnTriggered = false;

        public override void OnFixedUpdate()
        {
            if (!vessel.patchedConicSolver.maneuverNodes.Any())
            {
                this.enabled = false;
                return;
            }

            //check if we've finished a node:
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            Vector3d burnVector = node.GetBurnVector(orbit);

            if (burnVector.magnitude < precision)
            {
                burnTriggered = false;

                vessel.patchedConicSolver.RemoveManeuverNode(node);

                if (mode == Mode.ONE_NODE)
                {
                    Debug.Log("Node executor turning off after executing one node");
                    this.enabled = false;
                    vessel.ctrlState.mainThrottle = 0;
                    return;
                }
                else if (mode == Mode.ALL_NODES)
                {
                    if (!vessel.patchedConicSolver.maneuverNodes.Any())
                    {
                        Debug.Log("Node executor turning off after executing all nodes");
                        this.enabled = false;
                        vessel.ctrlState.mainThrottle = 0;
                        return;
                    }
                    else
                    {
                        Debug.Log("Node executor moving on to next node");
                        node = vessel.patchedConicSolver.maneuverNodes.First();
                    }
                }
            }

            //aim along the node
            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE, this);

            Debug.Log("maxThrustAccel = " + vesselState.maxThrustAccel);
            Debug.Log("thrustAvailable = " + vesselState.thrustAvailable);
            double burnTime = burnVector.magnitude / vesselState.maxThrustAccel;
            Debug.Log("burnTime = " + burnTime);

            double timeToNode = node.UT - vesselState.time;
            Debug.Log("time to node = " + timeToNode);

            if (timeToNode < burnTime * leadFraction)
            {
                Debug.Log("triggering burn");
                burnTriggered = true;
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
            }

            //autowarp, but only if we're already aligned with the node
            if (autowarp && !burnTriggered)
            {
                if (core.attitude.attitudeAngleFromTarget() < 1 || 
                    (core.attitude.attitudeAngleFromTarget() < 10 && !MuUtils.PhysicsRunning()))
                {
                    Debug.Log("Warping to node");
                    core.warp.WarpToUT(node.UT - burnTime*leadFraction - leadTime);
                }
                else if (!MuUtils.PhysicsRunning() && core.attitude.attitudeAngleFromTarget() > 10)
                {
                    Debug.Log("Turning off warp");
                    core.warp.MinimumWarp();
                }
            }

            
        }

        public override void Drive(FlightCtrlState s)
        {
            if (burnTriggered && core.attitude.attitudeAngleFromTarget() < 5)
            {
                Debug.Log("burning");
                ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
                double dVLeft = node.GetBurnVector(orbit).magnitude;

                double timeConstant = (dVLeft > 10 ? 0.5 : 2);

                Debug.Log("dVLeft = " + dVLeft);
                Debug.Log("throttle = " + dVLeft / (timeConstant * vesselState.maxThrustAccel));

                s.mainThrottle = Mathf.Clamp((float)(dVLeft / (0.5 * vesselState.maxThrustAccel)), 0.05F, 1);
            }
            else
            {
                s.mainThrottle = 0;
            }
        }


        public MechJebModuleNodeExecutor(MechJebCore core) : base(core) { }



        [ValueInfoItem(name="Node burn time")]
        public string NextManeuverNodeBurnTime()
        {
            if(!vessel.patchedConicSolver.maneuverNodes.Any()) return "N/A";
            
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double time = node.GetBurnVector(node.patch).magnitude / vesselState.maxThrustAccel;
            return MuUtils.ToSI(time, -1) + "s";
        }
    }
}
