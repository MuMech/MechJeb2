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
        [Persistent(pass = (int)Pass.Global)]
        public bool autowarp = true;      //whether to auto-warp to nodes
        [Persistent(pass = (int)Pass.Global)]
        public double leadTime = 3;       //how many seconds before a burn to end warp (note that we align with the node before warping)
        [Persistent(pass = (int)Pass.Global)]
        public double leadFraction = 0.5; //how early to start the burn, given as a fraction of the burn time
        [Persistent(pass = (int)Pass.Global)]
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
            core.KillThrottle();
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
                    this.enabled = false;
                    vessel.ctrlState.mainThrottle = 0;
                    return;
                }
                else if (mode == Mode.ALL_NODES)
                {
                    if (!vessel.patchedConicSolver.maneuverNodes.Any())
                    {
                        this.enabled = false;
                        vessel.ctrlState.mainThrottle = 0;
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

            double burnTime = burnVector.magnitude / vesselState.maxThrustAccel;

            double timeToNode = node.UT - vesselState.time;

            if (timeToNode < burnTime * leadFraction)
            {
                burnTriggered = true;
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
            }

            //autowarp, but only if we're already aligned with the node
            if (autowarp && !burnTriggered)
            {
                if (core.attitude.attitudeAngleFromTarget() < 1 || 
                    (core.attitude.attitudeAngleFromTarget() < 10 && !MuUtils.PhysicsRunning()))
                {
                    core.warp.WarpToUT(node.UT - burnTime*leadFraction - leadTime);
                }
                else if (!MuUtils.PhysicsRunning() && core.attitude.attitudeAngleFromTarget() > 10)
                {
                    core.warp.MinimumWarp();
                }
            }

            
        }

        public override void Drive(FlightCtrlState s)
        {
            if (burnTriggered && core.attitude.attitudeAngleFromTarget() < 5)
            {
                ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
                double dVLeft = node.GetBurnVector(orbit).magnitude;

                double timeConstant = (dVLeft > 10 ? 0.5 : 2);
                double desiredAcceleration = dVLeft / timeConstant;
                desiredAcceleration = Math.Max(precision, desiredAcceleration);

                s.mainThrottle = Mathf.Clamp01((float)(desiredAcceleration / vesselState.maxThrustAccel));
            }
            else
            {
                s.mainThrottle = 0;
            }
        }


        public MechJebModuleNodeExecutor(MechJebCore core) : base(core) { }



    }
}
