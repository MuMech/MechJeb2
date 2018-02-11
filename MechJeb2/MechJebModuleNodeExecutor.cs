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
        public EditableDouble tolerance = 0.1;    //we decide we're finished the burn when the remaining dV falls below this value (in m/s)


        [ValueInfoItem("Node Burn Length", InfoItem.Category.Thrust)]
        public string NextNodeBurnTime()
        {
            if (!vessel.patchedConicsUnlocked() || vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return "-";
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes[0];
            double dV = node.GetBurnVector(orbit).magnitude;
            double halfBurnTIme;
            return GuiUtils.TimeToDHMS(BurnTime(dV, out halfBurnTIme));
        }

        [ValueInfoItem("Node Burn Countdown", InfoItem.Category.Thrust)]
        public string NextNodeCountdown()
        {
            if (!vessel.patchedConicsUnlocked() || vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return "-";
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes[0];
            double dV = node.GetBurnVector(orbit).magnitude;
            double halfBurnTIme;
            BurnTime(dV, out halfBurnTIme);
            return GuiUtils.TimeToDHMS(node.UT - halfBurnTIme - vesselState.time);
        }

        public void ExecuteOneNode(object controller)
        {
            mode = Mode.ONE_NODE;
            enabled = true;
            burnTriggered = false;
            alignedForBurn = false;
        }

        public void ExecuteAllNodes(object controller)
        {
            mode = Mode.ALL_NODES;
            enabled = false;
            burnTriggered = false;
            alignedForBurn = false;
        }

        public void Abort()
        {
            core.warp.MinimumWarp();
            peg.enabled = false;
            users.Clear();
        }

        public override void OnModuleEnabled()
        {
            core.attitude.users.Add(this);
            core.thrust.users.Add(this);
            peg.enabled = true;
            // need to call this at least once
            core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE, this);
        }

        public override void OnModuleDisabled()
        {
            core.attitude.attitudeDeactivate();
            core.thrust.ThrustOff();
            core.thrust.users.Remove(this);
            peg.enabled = false;
        }

        protected enum Mode { ONE_NODE, ALL_NODES };
        protected Mode mode = Mode.ONE_NODE;

        public bool burnTriggered = false;
        public bool alignedForBurn = false;

        private MechJebModulePEGController peg { get { return core.GetComputerModule<MechJebModulePEGController>(); } }

        ManeuverNode node;
        Vector3d burnVector;
        double nodeUT;

        public override void OnFixedUpdate()
        {
            // We do not check if we have a maneuver node here, because once PEG is burning it doesn't need one
            // (and Principia likes to delete them when you're only half done).
            if (burnTriggered)
            {
                burn();
            }
            else
            {
                // now make sure we've got a node and do the preburn
                if (!vessel.patchedConicsUnlocked() || !vessel.patchedConicSolver.maneuverNodes.Any())
                {
                    Abort();
                    return;
                }

                preburn();
            }
        }

        private void burn_finished()
        {
            burnTriggered = false;
            core.thrust.targetThrottle = 0.0F;

            node.RemoveSelf();

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

        private void burn()
        {
            peg.AssertStart(false);
            if (peg.status == PegStatus.FINISHED)
                burn_finished();
        }

        private void preburn()
        {
            node = vessel.patchedConicSolver.maneuverNodes.First();
            double dVLeft = node.GetBurnVector(orbit).magnitude;

            peg.TargetNode(node, true);

            //aim along the node
            double halfBurnTime;
            BurnTime(dVLeft, out halfBurnTime);

            double timeToNode = node.UT - vesselState.time;
            double timeToHalfBT = timeToNode - halfBurnTime;
            double timeToPEGEnable = timeToHalfBT - 10.0; // enable PEG 10 secs before the burntime

            if (timeToPEGEnable <= 0)
            {
                peg.AssertStart(true);
                if (peg.isStable())
                {
                    core.attitude.attitudeTo(peg.iF, AttitudeReference.INERTIAL, this);
                    if (Vector3d.Angle(node.GetBurnVector(orbit), vesselState.forward) < 1 && !burnTriggered)
                    {
                        if (( peg.t_lambda >= node.UT && peg.phi < 40 * UtilMath.Deg2Rad ) || timeToHalfBT <= 0.0 )
                        {
                            core.thrust.targetThrottle = 1.0F;
                            peg.AssertStart(false);
                            burnTriggered = true;
                        }
                    }
                }
                if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
            }
            else
            {
                core.attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE, this);
            }

            //autowarp, but only if we're already aligned with the node
            if (autowarp && timeToPEGEnable > 0)
            {
                if ((Vector3d.Angle(node.GetBurnVector(orbit), vesselState.forward) < 1 && core.vessel.angularVelocity.magnitude < 0.002) || (core.attitude.attitudeAngleFromTarget() < 10 && !MuUtils.PhysicsRunning()))
                {
                    core.warp.WarpToUT(vesselState.time + timeToPEGEnable);
                }
                else
                {
                    if (!MuUtils.PhysicsRunning() && Vector3d.Angle(node.GetBurnVector(orbit), vesselState.forward) > 10 && timeToPEGEnable < 600)
                    {
                        //realign
                        core.warp.MinimumWarp();
                    }
                }
            }

            if (!burnTriggered)
            {
                core.thrust.targetThrottle = 0.0F;
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
            stats.RequestUpdate(this, true);

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
                        stageAvgAccel *= (double)this.vesselState.throttleFixedLimit;
                }

                halfBurnTime += Math.Min(halfDvLeft, stageBurnDv) / stageAvgAccel;
                halfDvLeft = Math.Max(0, halfDvLeft - stageBurnDv);

                burnTime += stageBurnDv / stageAvgAccel;

            }

            /* infinity means acceleration is zero for some reason, which is dangerous nonsense, so use zero instead */
            if (double.IsInfinity(halfBurnTime))
            {
                halfBurnTime = 0.0;
            }

            if (double.IsInfinity(burnTime))
            {
                burnTime = 0.0;
            }

            return burnTime;
        }

        public MechJebModuleNodeExecutor(MechJebCore core) : base(core) { }
    }
}
