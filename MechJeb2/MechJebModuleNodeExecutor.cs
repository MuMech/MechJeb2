using System;

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


        [ValueInfoItem("#MechJeb_NodeBurnLength", InfoItem.Category.Thrust)]//Node Burn Length
        public string NextNodeBurnTime()
        {
            if (!vessel.patchedConicsUnlocked())
                return "-";

            double dV;
            if (VesselState.isLoadedPrincipia && remainingDeltaV > 0)
            {
                dV = remainingDeltaV;
            }
            else
            {
                if (vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    return "-";
                ManeuverNode node = vessel.patchedConicSolver.maneuverNodes[0];
                dV = node.GetBurnVector(orbit).magnitude;
            }
            double halfBurnTime, spool;
            return GuiUtils.TimeToDHMS(BurnTime(dV, out halfBurnTime, out spool));
        }

        [ValueInfoItem("#MechJeb_NodeBurnCountdown", InfoItem.Category.Thrust)]//Node Burn Countdown
        public string NextNodeCountdown()
        {
            if (!vessel.patchedConicsUnlocked() || vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return "-";
            ManeuverNode node = vessel.patchedConicSolver.maneuverNodes[0];
            double dV;
            bool isPrincipia = VesselState.isLoadedPrincipia;
            if (isPrincipia)
            {
                dV = remainingDeltaV;
            }
            else
            {
                dV = node.GetBurnVector(orbit).magnitude;
            }
            double UT = node.UT;
            double halfBurnTIme, spool;
            BurnTime(dV, out halfBurnTIme, out spool);
            if (isPrincipia)
                UT -= spool;
            else
                UT -= halfBurnTIme; // already takes spoolup into account

            return GuiUtils.TimeToDHMS(UT - vesselState.time);
        }

        public void ExecuteOneNode(object controller)
        {
            mode = Mode.ONE_NODE;
            users.Add(controller);
            burnTriggered = false;
            alignedForBurn = false;
            remainingDeltaV = 0;
        }

        public void ExecuteOnePNode(object controller) //Principia Node
        {
            mode = Mode.ONE_PNODE;
            users.Add(controller);
            burnTriggered = false;
            alignedForBurn = false;

            remainingDeltaV = vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(orbit).magnitude;
        }

        public void ExecuteAllNodes(object controller)
        {
            mode = Mode.ALL_NODES;
            users.Add(controller);
            burnTriggered = false;
            alignedForBurn = false;
            remainingDeltaV = 0;
        }

        public void Abort()
        {
            core.warp.MinimumWarp();
            users.Clear();
            remainingDeltaV = 0;
            burnTriggered = false;
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
            remainingDeltaV = 0;
        }

        protected enum Mode { ONE_NODE,ONE_PNODE, ALL_NODES };
        protected Mode mode = Mode.ONE_NODE;

        public bool burnTriggered = false;
        public bool alignedForBurn = false;
        protected double remainingDeltaV = 0; // for Principia

        public override void OnFixedUpdate()
        {
            bool hasPrincipia = VesselState.isLoadedPrincipia;
            if (!vessel.patchedConicsUnlocked()
                || (!hasPrincipia && vessel.patchedConicSolver.maneuverNodes.Count == 0))
            {
                Abort();
                return;
            }

            if (hasPrincipia)
            {
                if (remainingDeltaV < tolerance)
                {
                    burnTriggered = false;

                    Abort();
                    return;
                }
                //aim along the node
                bool hasNode = vessel.patchedConicSolver.maneuverNodes.Count > 0;
                ManeuverNode node = hasNode ? vessel.patchedConicSolver.maneuverNodes[0] : null;
                if (hasNode)
                {
                    core.attitude.attitudeTo(Vector3d.forward,AttitudeReference.MANEUVER_NODE_COT,this);
                }
                else
                {
                    if (!core.attitude.attitudeKILLROT)
                    {
                        core.attitude.attitudeTo(UnityEngine.Quaternion.LookRotation(vessel.GetTransform().up,-vessel.GetTransform().forward),AttitudeReference.INERTIAL,this);
                        core.attitude.attitudeKILLROT = true;
                    }
                }

                double halfBurnTime, spool;
                BurnTime(remainingDeltaV,out halfBurnTime,out spool);

                double timeToNode = hasNode ? node.UT - vesselState.time : -1;
                //print("$$$$$$$ Executor: Node in " + timeToNode + ", spool " + spool.ToString("F3"));
                if ((halfBurnTime > 0 && timeToNode <= spool) || timeToNode < 0)
                {
                    burnTriggered = true;
                    if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
                }

                //autowarp, but only if we're already aligned with the node
                if (autowarp && !burnTriggered)
                {
                    if (timeToNode > 600 || (MuUtils.PhysicsRunning()
                        ? core.attitude.attitudeAngleFromTarget() < 1 && core.vessel.angularVelocity.magnitude < 0.001
                        : core.attitude.attitudeAngleFromTarget() < 10))
                    {
                        core.warp.WarpToUT(timeToNode - spool - leadTime);
                    }
                    else
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
                            double timeConstant = (remainingDeltaV > 10 || vesselState.minThrustAccel > 0.25 * vesselState.maxThrustAccel ? 0.5 : 2);
                            core.thrust.ThrustForDV(remainingDeltaV + tolerance,timeConstant);
                            remainingDeltaV -= vesselState.deltaT * vesselState.currentThrustAccel;
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
            else
            {
                //check if we've finished a node:
                ManeuverNode node = vessel.patchedConicSolver.maneuverNodes[0];
                double dVLeft = node.GetBurnVector(orbit).magnitude;

                if (dVLeft < tolerance && core.attitude.attitudeAngleFromTarget() > 5)
                {
                    burnTriggered = false;

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
                        }
                    }
                }

                //aim along the node
                core.attitude.attitudeTo(Vector3d.forward,AttitudeReference.MANEUVER_NODE_COT,this);

                double halfBurnTime, spool;
                BurnTime(dVLeft, out halfBurnTime, out spool);

                double timeToNode = node.UT - vesselState.time;
                //(!double.IsInfinity(num) && num > 0.0 && num2 < num) || num2 <= 0.0
                if (mode == Mode.ONE_NODE || mode == Mode.ALL_NODES)
                {
                    if (timeToNode < halfBurnTime)
                    {
                        burnTriggered = true;
                        if (!MuUtils.PhysicsRunning()) core.warp.MinimumWarp();
                    }
                }

                //autowarp, but only if we're already aligned with the node
                if (autowarp && !burnTriggered)
                {
                    if ((core.attitude.attitudeAngleFromTarget() < 1 && core.vessel.angularVelocity.magnitude < 0.001) || (core.attitude.attitudeAngleFromTarget() < 10 && !MuUtils.PhysicsRunning()))
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
                            double timeConstant = (dVLeft > 10 || vesselState.minThrustAccel > 0.25 * vesselState.maxThrustAccel ? 0.5 : 2);
                            core.thrust.ThrustForDV(dVLeft + tolerance,timeConstant);
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
        }

        

        private double BurnTime(double dv, out double halfBurnTime, out double spoolupTime)
        {
            double dvLeft = dv;
            double halfDvLeft = dv / 2;

            double burnTime = 0;
            halfBurnTime = 0;
            spoolupTime = 0;

            // Old code:
            //      burnTime = dv / vesselState.limitedMaxThrustAccel;

            var stats = core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate(this, true);

            double lastStageBurnTime = 0;
            for (int i = stats.vacStats.Length - 1; i >= 0 && dvLeft > 0; i--)
            {
                var s = stats.vacStats[i];
                if (s.DeltaV <= 0 || s.StartThrust <= 0)
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

                double stageBurnDv = Math.Min(s.DeltaV, dvLeft);
                dvLeft -= stageBurnDv;

                double stageBurnFraction = stageBurnDv / s.DeltaV;

                // Delta-V is proportional to ln(m0 / m1) (where m0 is initial
                // mass and m1 is final mass). We need to know the final mass
                // after this stage burns (m1b):
                //      ln(m0 / m1) * stageBurnFraction = ln(m0 / m1b)
                //      exp(ln(m0 / m1) * stageBurnFraction) = m0 / m1b
                //      m1b = m0 / (exp(ln(m0 / m1) * stageBurnFraction))
                double stageBurnFinalMass = s.StartMass / Math.Exp(Math.Log(s.StartMass / s.EndMass) * stageBurnFraction);
                double stageAvgAccel = s.StartThrust / ((s.StartMass + stageBurnFinalMass) / 2d);

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

                spoolupTime += s.SpoolUpTime;
                //print("** Execute: For stage " + i + ", found spoolup " + s.SpoolUpTime);
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

            //print("******** Found total spoolup time " + spoolupTime);
            if (spoolupTime > 0 && burnTime > 0)
            {
                if (burnTime < spoolupTime * 0.5d)
                {
                    spoolupTime = burnTime / (spoolupTime * 0.5d);
                    burnTime += spoolupTime;
                    halfBurnTime += spoolupTime;
                }
                else
                {
                    burnTime += spoolupTime;
                    halfBurnTime += spoolupTime;
                }
            }

            return burnTime;
        }

        public MechJebModuleNodeExecutor(MechJebCore core) : base(core) { }
    }
}
