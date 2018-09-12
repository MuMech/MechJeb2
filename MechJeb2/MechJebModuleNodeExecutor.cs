using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleNodeExecutor : ComputerModule
    {
        //public interface:
        [Persistent(pass = (int)Pass.Global)]
        public bool autowarp = true;      //whether to auto-warp to nodes

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble leadTime = 3; //how many seconds before a burn to end warp (note that we align with the node before warping)

        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble tolerance = 0.1;    // FIXME: ignored

        public double timeToNode = 0.0;
        public double nodeBurnTime = 0.0;
        public bool burnTriggered = false;

        [ValueInfoItem("Node Burn Length", InfoItem.Category.Thrust)]
        public string NextNodeBurnTime()
        {
            if (!vessel.patchedConicsUnlocked() || vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return "-";
            // FIXME: wire this up again
            return GuiUtils.TimeToDHMS(nodeBurnTime);
        }

        [ValueInfoItem("Node Burn Countdown", InfoItem.Category.Thrust)]
        public string NextNodeCountdown()
        {
            if (!vessel.patchedConicsUnlocked() || vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return "-";
            // FIXME: wire this up again
            return GuiUtils.TimeToDHMS(timeToNode);
        }

        public void ExecuteOneNode(object controller)
        {
            mode = Mode.ONE_NODE;
            enabled = true;
        }

        public void ExecuteAllNodes(object controller)
        {
            // FIXME: pull all the future nodes and target orbits and stuff them into a List<> here so we can just target the future orbits.
            mode = Mode.ALL_NODES;
            enabled = false;
        }

        public void Abort()
        {
            core.warp.MinimumWarp();
            core.optimizer.enabled = false;
            users.Clear();
        }

        public override void OnModuleEnabled()
        {
            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
            core.optimizer.enabled = true;
            state = NodeState.PREGUIDANCE;
            burnTriggered = false;
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            core.optimizer.enabled = false;
            state = NodeState.PREGUIDANCE;
            burnTriggered = false;
        }

        protected enum Mode { ONE_NODE, ALL_NODES };
        protected Mode mode = Mode.ONE_NODE;
        private enum NodeState { PREGUIDANCE, GUIDANCE, FINISH };
        private NodeState state;

        ManeuverNode node;

        public override void OnFixedUpdate()
        {
            core.optimizer.autowarp = autowarp;

            switch (state)
            {
                case NodeState.PREGUIDANCE:
                    PreGuidance();
                    break;

                case NodeState.GUIDANCE:
                    Guidance();
                    break;
            }

            if (state == NodeState.FINISH)
                Finish();
        }

        // this is coasting / warping until we're a whole estimated burntime before the burn
        // (so precoast in the optimizer is roughly burntime / 2)
        private void PreGuidance()
        {
            if (!vessel.patchedConicsUnlocked() || vessel.patchedConicSolver.maneuverNodes.Count == 0)
            {
                Abort();
                return;
            }

            node = vessel.patchedConicSolver.maneuverNodes[0];
            double dVLeft = node.GetBurnVector(orbit).magnitude;

            double timeToNode = node.UT - vesselState.time;
            double timeToGuidance = timeToNode - Math.Max(BurnTime(dVLeft), 30);   /* leave at least 15 seconds for optimizer convergence */

            if (timeToGuidance <= 0)
            {
                state = NodeState.GUIDANCE;
                return;
            }

            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE, this);

            if (autowarp)
            {
                if ((Vector3d.Angle(node.GetBurnVector(orbit), vesselState.forward) < 1 && core.vessel.angularVelocity.magnitude < 0.002) || (core.attitude.attitudeAngleFromTarget() < 10 && !MuUtils.PhysicsRunning()))
                {
                    core.warp.WarpToUT(vesselState.time + timeToGuidance);
                }
                else
                {
                    if (!MuUtils.PhysicsRunning() && Vector3d.Angle(node.GetBurnVector(orbit), vesselState.forward) > 10 && timeToGuidance < 600)
                    {
                        //realign
                        core.warp.MinimumWarp();
                    }
                }
            }
        }

        private void Finish()
        {
            node.RemoveSelf();

            if (mode == Mode.ONE_NODE)
            {
                Abort();
                return;
            }
            else if (mode == Mode.ALL_NODES)
            {
                if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                {
                    Abort();
                    return;
                }
                else
                {
                    node = vessel.patchedConicSolver.maneuverNodes[0];
                    state = NodeState.PREGUIDANCE;
                }
            }
        }

        private void Guidance()
        {
            double dVLeft = node.GetBurnVector(orbit).magnitude;
            core.optimizer.TargetNode(node, BurnTime(dVLeft));

            core.optimizer.AssertStart();
            if (core.optimizer.isStable())
            {
                core.attitude.attitudeTo(core.optimizer.iF, AttitudeReference.INERTIAL, this);
                if (core.optimizer.status == PegStatus.FINISHED)
                   state = NodeState.FINISH;
            }
            if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
        }

        // this is estimated burntime, it doesn't have to be perfectly accurate, but this needs to be
        // at least within a factor of 2 (if burntime is less than half the burntime coast will be zero)
        private double BurnTime(double dv)
        {
            double burnTime = 0;

            var stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this, true);

            for (int i = stats.vacStats.Length - 1; i >= 0 && dv > 0; i--)
            {
                var s = stats.vacStats[i];

                if (s.deltaV <= 0 || s.startThrust <= 0)
                    continue;

                double stageBurnDv = Math.Min(s.deltaV, dv);
                dv -= stageBurnDv;

                double thrust = s.startThrust;

                // Right now, for simplicity, we're ignoring throttle limits for
                // all but the current stage. This is wrong, but hopefully it's
                // close enough for now.
                // TODO: Be smarter about throttle limits on future stages.
                // XXX: i almost want to delete this?
                // TODO: things like g-limiters should go into the optimizer, and we should ask the optimzer
                if ( i == stats.vacStats.Length - 1)
                    thrust *= vesselState.throttleFixedLimit;

                double a0 = thrust / s.startMass;
                double ve = s.isp * 9.80655;
                double tau = ve / a0;

                burnTime += tau * ( 1.0 - Math.Exp(-stageBurnDv/ve) );
            }

            /* infinity means acceleration is zero for some reason, which is dangerous nonsense, so use zero instead */
            if (double.IsInfinity(burnTime))
                burnTime = 0.0;

            return burnTime;
        }

        public MechJebModuleNodeExecutor(MechJebCore core) : base(core) { }
    }
}
