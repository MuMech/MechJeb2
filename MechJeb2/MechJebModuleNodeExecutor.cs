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
        public const double leadTime = 3; //how many seconds before a burn to end warp (note that we align with the node before warping)
        [Persistent(pass = (int)Pass.Global)]
        public EditableDouble tolerance = 0.1;    //we decide we're finished the burn when the remaining dV falls below this value (in m/s)


        [ValueInfoItem("Node Burn Length", InfoItem.Category.Thrust)]
        public string NextNodeBurnTime()
        {
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any())
                return "-";
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double dV = node.GetBurnVector(orbit).magnitude;
            double halfBurnTIme;
            return GuiUtils.TimeToDHMS(BurnTime(dV, out halfBurnTIme));
        }

        [ValueInfoItem("Node Burn Countdown", InfoItem.Category.Thrust)]
        public string NextNodeCountdown()
        {
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any())
                return "-";
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double dV = node.GetBurnVector(orbit).magnitude;
            double halfBurnTIme;
            double burnTIme = BurnTime(dV, out halfBurnTIme);
            return GuiUtils.TimeToDHMS(node.UT - halfBurnTIme - vesselState.time);
        }

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
            if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any())
            {
                Abort();
                return;
            }

            //check if we've finished a node:
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes.First();
            double dVLeft = node.GetBurnVector(orbit).magnitude;

            if (dVLeft < tolerance && core.attitude.attitudeAngleFromTarget() > 5)
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

            double halfBurnTime;
            double burnTime = BurnTime(dVLeft, out halfBurnTime);

            double timeToNode = node.UT - vesselState.time;

            if (timeToNode < halfBurnTime)
            {
                burnTriggered = true;
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
            }

            //autowarp, but only if we're already aligned with the node
            if (autowarp && !burnTriggered)
            {
                if (core.attitude.attitudeAngleFromTarget() < 1 || (core.attitude.attitudeAngleFromTarget() < 10 && !MuUtils.PhysicsRunning()))
                {
                    core.warp.WarpToUT(node.UT - halfBurnTime - leadTime);
                }
                else if (!MuUtils.PhysicsRunning() && core.attitude.attitudeAngleFromTarget() > 10 && timeToNode < 600)
                {
                    //realign
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
                        core.thrust.ThrustForDV(dVLeft + tolerance, timeConstant);
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

        private double BurnTime(double dv, out double halfBurnTime)
        {
            double dvLeft = dv;
            double halfDvLeft = dv / 2;

            double burnTime = 0;
            halfBurnTime = 0;

            // Old code:
            //      burnTime = dv / vesselState.limitedMaxThrustAccel;

            var stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this);

            double lastStageBurnTime = 0;
            for (int i = stats.vacStats.Length - 1; i >= 0 && dvLeft > 0; i--)
            {
                var s = stats.vacStats[i];
                if (s.deltaV <= 0 || s.startThrust <= 0)
                {
                    if (core.staging.enabled)
                    {
                        // We staged again before autostagePreDelay is elapsed.
                        // Add the remaining wait time
                        if (burnTime - lastStageBurnTime < core.staging.autostagePreDelay && i != stats.vacStats.Length - 1)
                            burnTime += core.staging.autostagePreDelay - (burnTime - lastStageBurnTime);
                        burnTime += core.staging.autostagePreDelay;
                        lastStageBurnTime = burnTime;
                    }
                    continue;
                }

                double stageBurnDv = Math.Min(s.deltaV, dvLeft);
                dvLeft -= stageBurnDv;

                double stageBurnFraction = stageBurnDv / s.deltaV;

                // Delta-V is proportional to ln(m0 / m1) (where m0 is initial
                // mass and m1 is final mass). We need to know the final mass
                // after this stage burns (m1b):
                //      ln(m0 / m1) * stageBurnFraction = ln(m0 / m1b)
                //      exp(ln(m0 / m1) * stageBurnFraction) = m0 / m1b
                //      m1b = m0 / (exp(ln(m0 / m1) * stageBurnFraction))
                double stageBurnFinalMass = s.startMass / Math.Exp(Math.Log(s.startMass / s.endMass) * stageBurnFraction);
                double stageAvgAccel = s.startThrust / ((s.startMass + stageBurnFinalMass) / 2d);

                // Right now, for simplicity, we're ignoring throttle limits for
                // all but the current stage. This is wrong, but hopefully it's
                // close enough for now.
                // TODO: Be smarter about throttle limits on future stages.
                if (i == stats.vacStats.Length - 1)
                {
                    stageAvgAccel *= vesselState.throttleLimit;
                }

                halfBurnTime += Math.Min(halfDvLeft, stageBurnDv) / stageAvgAccel;
                halfDvLeft = Math.Max(0, halfDvLeft - stageBurnDv);

                burnTime += stageBurnDv / stageAvgAccel;

            }

            return burnTime;
        }

        public MechJebModuleNodeExecutor(MechJebCore core) : base(core) { }
    }
}
