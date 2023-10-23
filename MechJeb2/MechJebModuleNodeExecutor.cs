using System;
using JetBrains.Annotations;
using MechJebLib.Simulations;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleNodeExecutor : ComputerModule
    {
        //public interface:
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool autowarp = true; //whether to auto-warp to nodes

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble leadTime = 3; //how many seconds before a burn to end warp (note that we align with the node before warping)

        [Persistent(pass = (int)Pass.GLOBAL)]
        public EditableDouble tolerance = 0.1; //we decide we're finished the burn when the remaining dV falls below this value (in m/s)

        [ValueInfoItem("#MechJeb_NodeBurnLength", InfoItem.Category.Thrust)] //Node Burn Length
        public string NextNodeBurnTime()
        {
            if (!Vessel.patchedConicsUnlocked())
                return "-";

            double dV;
            if (VesselState.isLoadedPrincipia && DVLeft > 0)
            {
                dV = DVLeft;
            }
            else
            {
                if (Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    return "-";
                ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes[0];
                dV = node.GetBurnVector(Orbit).magnitude;
            }

            double halfBurnTime, spool;
            return GuiUtils.TimeToDHMS(BurnTime(dV, out halfBurnTime, out spool));
        }

        [ValueInfoItem("#MechJeb_NodeBurnCountdown", InfoItem.Category.Thrust)] //Node Burn Countdown
        public string NextNodeCountdown()
        {
            if (!Vessel.patchedConicsUnlocked() || Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return "-";
            ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes[0];
            double dV;
            bool isPrincipia = VesselState.isLoadedPrincipia;
            if (isPrincipia)
            {
                dV = DVLeft;
            }
            else
            {
                dV = node.GetBurnVector(Orbit).magnitude;
            }

            double UT = node.UT;
            double halfBurnTIme, spool;
            BurnTime(dV, out halfBurnTIme, out spool);
            if (isPrincipia)
                UT -= spool;
            else
                UT -= halfBurnTIme; // already takes spoolup into account

            return GuiUtils.TimeToDHMS(UT - VesselState.time);
        }

        public void ExecuteOneNode(object controller)
        {
            mode = Mode.ONE_NODE;
            Users.Add(controller);
            burnTriggered   = false;
            alignedForBurn  = false;
            DVLeft = 0;
        }

        public void ExecuteOnePNode(object controller) //Principia Node
        {
            mode = Mode.ONE_PNODE;
            Users.Add(controller);
            burnTriggered  = false;
            alignedForBurn = false;

            DVLeft = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).magnitude;
        }

        public void ExecuteAllNodes(object controller)
        {
            mode = Mode.ALL_NODES;
            Users.Add(controller);
            burnTriggered   = false;
            alignedForBurn  = false;
            DVLeft = 0;
        }

        public void Abort()
        {
            Core.Warp.MinimumWarp();
            Users.Clear();
            DVLeft = 0;
            burnTriggered   = false;
        }

        protected override void OnModuleEnabled()
        {
            Core.Attitude.Users.Add(this);
            Core.Thrust.Users.Add(this);
        }

        protected override void OnModuleDisabled()
        {
            Core.Attitude.attitudeDeactivate();
            Core.Thrust.ThrustOff();
            Core.Thrust.Users.Remove(this);
            DVLeft = 0;
        }

        protected enum Mode { ONE_NODE, ONE_PNODE, ALL_NODES }

        protected Mode mode = Mode.ONE_NODE;

        public    bool   burnTriggered;
        public    bool   alignedForBurn;
        protected double DVLeft; // for Principia
        protected bool   nearingBurn;

        public override void Drive(FlightCtrlState s)
        {
            if (!burnTriggered && nearingBurn && Core.Thrust.LimitToPreventUnstableIgnition &&
                VesselState.lowestUllage != VesselState.UllageState.VeryStable)
            {
                if (Vessel.hasEnabledRCSModules())
                {
                    if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                    {
                        Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                    }

                    s.Z = -1.0F;
                }
            }
        }

        public override void OnFixedUpdate()
        {
            nearingBurn = false;
            bool hasPrincipia = VesselState.isLoadedPrincipia;
            bool hasNodes = Vessel.patchedConicSolver.maneuverNodes.Count > 0;
            if (!Vessel.patchedConicsUnlocked()
                || (!hasPrincipia && !hasNodes))
            {
                Abort();
                return;
            }

            if (hasPrincipia && mode == Mode.ONE_PNODE)
            {
                if (DVLeft < tolerance)
                {
                    burnTriggered = false;

                    Abort();
                    return;
                }

                //aim along the node
                ManeuverNode node = hasNodes ? Vessel.patchedConicSolver.maneuverNodes[0] : null;
                if (hasNodes)
                {
                    Core.Attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE_COT, this);
                }
                else
                {
                    if (!Core.Attitude.attitudeKILLROT)
                    {
                        Core.Attitude.attitudeTo(Quaternion.LookRotation(Vessel.GetTransform().up, -Vessel.GetTransform().forward),
                            AttitudeReference.INERTIAL, this);
                        Core.Attitude.attitudeKILLROT = true;
                    }
                }

                double halfBurnTime, spool;
                BurnTime(DVLeft, out halfBurnTime, out spool);

                double ignitionUT = hasNodes ? node.UT - spool - leadTime : -1;

                nearingBurn = ignitionUT <= VesselState.time;

                if (ignitionUT < VesselState.time)
                {
                    burnTriggered = true;
                    if (!MuUtils.PhysicsRunning()) Core.Warp.MinimumWarp();
                }

                HandleAutowarp(ignitionUT);

                HandleAligning(DVLeft);

                if ((burnTriggered || nearingBurn) && MuUtils.PhysicsRunning())
                {
                    // decrement remaining dV based on engine and RCS thrust
                    // Since this is Principia, we can't rely on the node's delta V itself updating, we have to do it ourselves.
                    // We also can't just use vesselState.currentThrustAccel because only engines are counted.
                    // NOTE: This *will* include acceleration from decouplers, which is pretty cool.
                    Vector3d dV = (Vessel.acceleration_immediate - Vessel.graviticAcceleration) * TimeWarp.fixedDeltaTime;
                    DVLeft -= Vector3d.Dot(dV, Core.Attitude.targetAttitude());
                }
            }
            else if (hasNodes)
            {
                //check if we've finished a node:
                ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes[0];
                double dVLeft = node.GetBurnVector(Orbit).magnitude;

                if (dVLeft < tolerance && Core.Attitude.attitudeAngleFromTarget() > 5)
                {
                    burnTriggered = false;

                    node.RemoveSelf();

                    if (mode == Mode.ONE_NODE)
                    {
                        Abort();
                        return;
                    }

                    if (mode == Mode.ALL_NODES)
                    {
                        if (Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                        {
                            Abort();
                            return;
                        }

                        node = Vessel.patchedConicSolver.maneuverNodes[0];
                    }
                }

                //aim along the node
                Core.Attitude.attitudeTo(Vector3d.forward, AttitudeReference.MANEUVER_NODE_COT, this);

                double halfBurnTime, spool;
                BurnTime(dVLeft, out halfBurnTime, out spool);

                double ignitionUT = node.UT - halfBurnTime - leadTime;

                nearingBurn = ignitionUT <= VesselState.time;

                //(!double.IsInfinity(num) && num > 0.0 && num2 < num) || num2 <= 0.0
                if (mode == Mode.ONE_NODE || mode == Mode.ALL_NODES)
                {
                    if (ignitionUT < VesselState.time)
                    {
                        burnTriggered = true;
                        if (!MuUtils.PhysicsRunning()) Core.Warp.MinimumWarp();
                    }
                }

                HandleAutowarp(ignitionUT);

                HandleAligning(dVLeft);
            }
        }

        private void HandleAutowarp(double ignitionUT)
        {
            double timeToBurn = ignitionUT - VesselState.time - leadTime;
            //autowarp, but only if we're already aligned with the node
            if (autowarp && !burnTriggered)
            {
                if (timeToBurn > 600 || (MuUtils.PhysicsRunning()
                        ? Core.Attitude.attitudeAngleFromTarget() < 1 && Core.vessel.angularVelocity.magnitude < 0.001
                        : Core.Attitude.attitudeAngleFromTarget() < 10))
                {
                    Core.Warp.WarpToUT(VesselState.time + timeToBurn);
                }
                else
                {
                    //realign
                    Core.Warp.MinimumWarp();
                }
            }
        }

        //private void HandleAligning(double timeToNode, double halfBurnTime, double dVLeft)
        private void HandleAligning(double dVLeft)
        {
            Core.Thrust.TargetThrottle = 0;

            if (burnTriggered)
            {
                if (alignedForBurn)
                {
                    if (Core.Attitude.attitudeAngleFromTarget() < 90)
                    {
                        double timeConstant = dVLeft > 10 || VesselState.minThrustAccel > 0.25 * VesselState.maxThrustAccel ? 0.5 : 2;
                        Core.Thrust.ThrustForDV(dVLeft + tolerance, timeConstant);
                    }
                    else
                    {
                        alignedForBurn = false;
                    }
                }
                else
                {
                    if (Core.Attitude.attitudeAngleFromTarget() < 2)
                    {
                        alignedForBurn = true;
                    }
                }
            }
        }

        private double BurnTime(double dv, out double halfBurnTime, out double spoolupTime)
        {
            double dvLeft = dv;
            double halfDvLeft = dv / 2;

            double  burnTime = 0;
            halfBurnTime = 0;
            spoolupTime  = 0;

            // Old code:
            //      burnTime = dv / vesselState.limitedMaxThrustAccel;

            MechJebModuleStageStats stats = Core.GetComputerModule<MechJebModuleStageStats>();
            stats.RequestUpdate();

            double lastStageBurnTime = 0;
            for (int mjPhase = stats.VacStats.Count - 1; mjPhase >= 0 && dvLeft > 0; mjPhase--)
            {
                FuelStats s = stats.VacStats[mjPhase];
                if (s.DeltaV <= 0 || s.Thrust <= 0)
                {
                    if (Core.Staging.Enabled)
                    {
                        // We staged again before autostagePreDelay is elapsed.
                        // Add the remaining wait time
                        if (burnTime - lastStageBurnTime < Core.Staging.AutostagePreDelay && mjPhase != stats.VacStats.Count - 1)
                            burnTime += Core.Staging.AutostagePreDelay - (burnTime - lastStageBurnTime);
                        burnTime          += Core.Staging.AutostagePreDelay;
                        lastStageBurnTime =  burnTime;
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
                double stageAvgAccel = s.Thrust / ((s.StartMass + stageBurnFinalMass) / 2d);

                // Right now, for simplicity, we're ignoring throttle limits for
                // all but the current stage. This is wrong, but hopefully it's
                // close enough for now.
                // TODO: Be smarter about throttle limits on future stages.
                if (mjPhase == stats.VacStats.Count - 1)
                {
                    stageAvgAccel *= VesselState.throttleFixedLimit;
                }

                halfBurnTime += Math.Min(halfDvLeft, stageBurnDv) / stageAvgAccel;
                halfDvLeft   =  Math.Max(0, halfDvLeft - stageBurnDv);

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
                    spoolupTime  =  burnTime / (spoolupTime * 0.5d);
                    burnTime     += spoolupTime;
                    halfBurnTime += spoolupTime;
                }
                else
                {
                    burnTime     += spoolupTime;
                    halfBurnTime += spoolupTime;
                }
            }

            return burnTime;
        }

        public MechJebModuleNodeExecutor(MechJebCore core) : base(core) { }
    }
}
