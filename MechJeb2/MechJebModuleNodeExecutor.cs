using JetBrains.Annotations;
using MechJebLib.Simulations;
using UnityEngine;
using static System.Math;
using static MechJebLib.Statics;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleNodeExecutor : ComputerModule
    {
        //public interface:
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Autowarp = true; //whether to auto-warp to nodes

        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble
            LeadTime = new EditableDouble(3); //how many seconds before a burn to end warp (note that we align with the node before warping)

        [ValueInfoItem("#MechJeb_NodeBurnLength", InfoItem.Category.Thrust)] //Node Burn Length
        public string NextNodeBurnTime()
        {
            if (!Vessel.patchedConicsUnlocked())
                return "-";

            double dV;
            if (VesselState.isLoadedPrincipia && _dvLeft > 0)
            {
                dV = _dvLeft;
            }
            else
            {
                if (Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                    return "-";
                ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes[0];
                dV = node.GetBurnVector(Orbit).magnitude;
            }

            return GuiUtils.TimeToDHMS(BurnTime(dV, out double _, out double _));
        }

        [ValueInfoItem("#MechJeb_NodeBurnCountdown", InfoItem.Category.Thrust)] //Node Burn Countdown
        public string NextNodeCountdown()
        {
            if (!Vessel.patchedConicsUnlocked() || Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                return "-";
            ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes[0];
            double dV = _isLoadedPrincipia ? _dvLeft : node.GetBurnVector(Orbit).magnitude;

            double ut = node.UT;
            BurnTime(dV, out double halfBurnTIme, out double spool);
            if (_isLoadedPrincipia)
                ut -= spool;
            else
                ut -= halfBurnTIme; // already takes spoolup into account

            return GuiUtils.TimeToDHMS(ut - VesselState.time);
        }

        public void ExecuteOneNode(object controller)
        {
            _mode = Mode.ONE_NODE;
            Users.Add(controller);
            BurnTriggered = false;
            _dvLeft       = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).magnitude;
        }

        public void ExecuteAllNodes(object controller)
        {
            _mode = Mode.ALL_NODES;
            Users.Add(controller);
            BurnTriggered = false;
            _dvLeft       = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).magnitude;
        }

        public void Abort()
        {
            Core.Warp.MinimumWarp();
            Users.Clear();
            _dvLeft       = 0;
            BurnTriggered = false;
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
            _dvLeft = 0;
        }

        private enum Mode { ONE_NODE, ALL_NODES }

        private Mode _mode = Mode.ONE_NODE;

        public         bool     BurnTriggered;
        public         bool     IsAligned;
        private        double   _dvLeft;    // for Principia
        private        Vector3d _direction; // de-rotated world vector
        private static bool     _isLoadedPrincipia => VesselState.isLoadedPrincipia;
        private        bool     _hasNodes          => Vessel.patchedConicSolver.maneuverNodes.Count > 0;

        public override void Drive(FlightCtrlState s) => HandleUllage(s);

        private void HandleUllage(FlightCtrlState s)
        {
            // FIXME: can't we rely on the Throttle+Staging controllers to do this right?
            if (BurnTriggered || !Core.Thrust.LimitToPreventUnstableIgnition ||
                VesselState.lowestUllage == VesselState.UllageState.VeryStable) return;

            if (!Vessel.hasEnabledRCSModules()) return;

            if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            s.Z = -1.0F;
        }

        public override void OnFixedUpdate()
        {
            if (!Vessel.patchedConicsUnlocked() || (!_isLoadedPrincipia && !_hasNodes))
            {
                Abort();
                return;
            }

            ManeuverNode node = _hasNodes ? Vessel.patchedConicSolver.maneuverNodes[0] : null;

            if (!_isLoadedPrincipia)
                _dvLeft    = node!.GetBurnVector(Orbit).magnitude;
            else
                DecrementDvLeft();

            _direction = NextDirection();

            if (ShouldTerminate(ref node))
            {
                BurnTriggered = false;
                Abort();
                return;
            }

            Core.Attitude.attitudeTo(Planetarium.fetch.rotation * _direction, AttitudeReference.INERTIAL_COT, this);

            double ignitionUT = CalculateIgnitionUT(node);

            if (VesselState.time >= ignitionUT)
            {
                BurnTriggered = true;
                if (!MuUtils.PhysicsRunning()) Core.Warp.MinimumWarp();
            }

            HandleAutowarp(ignitionUT);

            HandleAligningAndThrust();
        }

        private bool ShouldTerminate(ref ManeuverNode node)
        {
            if (_isLoadedPrincipia)
                return _dvLeft < 0;

            if (BurnTriggered && AngleFromNode() >= 0.5 * PI)
            {
                node.RemoveSelf();

                if (_mode == Mode.ONE_NODE)
                    return true;

                if (_mode == Mode.ALL_NODES)
                {
                    if (Vessel.patchedConicSolver.maneuverNodes.Count == 0)
                        return true;

                    node = Vessel.patchedConicSolver.maneuverNodes[0];
                }
            }

            return false;
        }

        private double AngleFromNode()
        {
            Vector3d fwd = Quaternion.FromToRotation(VesselState.forward, VesselState.thrustForward) * VesselState.forward;
            Vector3d dir = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).normalized;
            return SafeAcos(Vector3d.Dot(fwd, dir));
        }

        private double AngleFromDirection()
        {
            Vector3d fwd = Quaternion.FromToRotation(VesselState.forward, VesselState.thrustForward) * VesselState.forward;
            Vector3d dir = (Planetarium.fetch.rotation * _direction).normalized;
            return SafeAcos(Vector3d.Dot(fwd, dir));
        }

        private Vector3d NextDirection()
        {
            var invRot = QuaternionD.Inverse(Planetarium.fetch.rotation);

            if (_isLoadedPrincipia && !_hasNodes) return _direction;

            if (!_isLoadedPrincipia && _dvLeft < VesselState.minThrustAccel) return _direction;

            return invRot * Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit);
        }

        private double CalculateIgnitionUT(ManeuverNode node)
        {
            BurnTime(_dvLeft, out double halfBurnTime, out double spool);
            if (_isLoadedPrincipia)
                // in principia node.UT is the start of the burn and we need to subtract off the spool time
                return _hasNodes ? node.UT - spool - LeadTime : -1;

            // in stock node.UT is the center of the burn and the halfBurnTime calculation has the spool time
            return node.UT - halfBurnTime - LeadTime;
        }

        private void DecrementDvLeft()
        {
            if (!BurnTriggered || !MuUtils.PhysicsRunning()) return;

            // decrement remaining dV based on engine and RCS thrust
            // Since this is Principia, we can't rely on the node's delta V itself updating, we have to do it ourselves.
            // We also can't just use vesselState.currentThrustAccel because only engines are counted.
            // NOTE: This *will* include acceleration from decouplers, which is pretty cool.
            Vector3d dV = (Vessel.acceleration_immediate - Vessel.graviticAcceleration) * TimeWarp.fixedDeltaTime;
            _dvLeft -= Vector3d.Dot(dV, _direction);
        }

        private void HandleAutowarp(double ignitionUT)
        {
            if (BurnTriggered || !Autowarp) return;

            double timeToBurn = ignitionUT - VesselState.time;

            if (timeToBurn > 600)
            {
                Core.Warp.WarpToUT(ignitionUT - 600);
                return;
            }

            if (MuUtils.PhysicsRunning()
                    ? AngleFromDirection() < Deg2Rad(1) && Core.vessel.angularVelocity.magnitude < 0.001
                    : AngleFromDirection() < Deg2Rad(2))
                Core.Warp.WarpToUT(ignitionUT - 3);
            else
                Core.Warp.MinimumWarp();
        }

        private void HandleAligningAndThrust()
        {
            Core.Thrust.TargetThrottle = 0;

            if (!BurnTriggered) return;

            double timeConstant = _dvLeft > 10 || VesselState.minThrustAccel > 0.25 * VesselState.maxThrustAccel ? 0.5 : 2;
            Core.Thrust.ThrustForDV(_dvLeft, timeConstant);
        }

        private double BurnTime(double dv, out double halfBurnTime, out double spoolupTime)
        {
            double dvLeft = dv;
            double halfDvLeft = dv / 2;

            double burnTime = 0;
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

                double stageBurnDv = Min(s.DeltaV, dvLeft);
                dvLeft -= stageBurnDv;

                double stageBurnFraction = stageBurnDv / s.DeltaV;

                // Delta-V is proportional to ln(m0 / m1) (where m0 is initial
                // mass and m1 is final mass). We need to know the final mass
                // after this stage burns (m1b):
                //      ln(m0 / m1) * stageBurnFraction = ln(m0 / m1b)
                //      exp(ln(m0 / m1) * stageBurnFraction) = m0 / m1b
                //      m1b = m0 / (exp(ln(m0 / m1) * stageBurnFraction))
                double stageBurnFinalMass = s.StartMass / Exp(Log(s.StartMass / s.EndMass) * stageBurnFraction);
                double stageAvgAccel = s.Thrust / ((s.StartMass + stageBurnFinalMass) / 2d);

                // Right now, for simplicity, we're ignoring throttle limits for
                // all but the current stage. This is wrong, but hopefully it's
                // close enough for now.
                // TODO: Be smarter about throttle limits on future stages.
                if (mjPhase == stats.VacStats.Count - 1)
                {
                    stageAvgAccel *= VesselState.throttleFixedLimit;
                }

                halfBurnTime += Min(halfDvLeft, stageBurnDv) / stageAvgAccel;
                halfDvLeft   =  Max(0, halfDvLeft - stageBurnDv);

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
