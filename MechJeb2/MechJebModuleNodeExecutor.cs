using System;
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
        // whether to auto-warp to nodes
        [Persistent(pass = (int)Pass.GLOBAL)]
        public bool Autowarp = true;

        // how many seconds before a burn to end warp
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDouble LeadTime = new EditableDouble(3);

        // do burn on RCS engines only
        public bool RCSOnly = false;

        // Node Burn Length
        [ValueInfoItem("#MechJeb_NodeBurnLength", InfoItem.Category.Thrust)]
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
            Users.Add(controller);
            _mode = Mode.ONE_NODE;
            Init();
        }

        public void ExecuteAllNodes(object controller)
        {
            Users.Add(controller);
            _mode = Mode.ALL_NODES;
            Init();
        }

        private void Init()
        {
            State      = States.WARPALIGN;
            _direction = Vector3d.zero;
            _dvLeft    = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).magnitude;
            Core.Thrust.ThrustOff();
        }

        public void Abort()
        {
            Core.Warp.MinimumWarp();
            Core.Thrust.ThrustOff();
            Users.Clear();
            _dvLeft = 0;
            State   = States.IDLE;
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

        public enum States { WARPALIGN, LEAD, BURN, IDLE }

        private Mode   _mode = Mode.ONE_NODE;
        public  States State = States.IDLE;

        private        double   _dvLeft;    // for Principia
        private        Vector3d _direction; // de-rotated world vector
        private        Vector3d _worldDirection => Planetarium.fetch.rotation * _direction;
        private        double   _ignitionUT;
        private static bool     _isLoadedPrincipia => VesselState.isLoadedPrincipia;
        private        bool     _hasNodes          => Vessel.patchedConicSolver.maneuverNodes.Count > 0;

        public override void Drive(FlightCtrlState s) => DoRCS(s);

        private void DoRCS(FlightCtrlState s)
        {
            if (State == States.BURN && RCSOnly)
            {
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);
                s.Z = -1.0F;
                return;
            }

            if (State != States.LEAD || RCSOnly)
                return;

            s.Z = 0.0F;

            if (!Core.Thrust.LimitToPreventUnstableIgnition || VesselState.lowestUllage == VesselState.UllageState.VeryStable)
                return;

            if (!Vessel.hasEnabledRCSModules())
                return;

            if (!Vessel.ActionGroups[KSPActionGroup.RCS])
                Vessel.ActionGroups.SetGroup(KSPActionGroup.RCS, true);

            if (!AlignedAndSettled())
                return;

            s.Z = -1.0F;
        }

        public override void OnFixedUpdate()
        {
            if (!Vessel.patchedConicsUnlocked() || (!_isLoadedPrincipia && !_hasNodes) || State == States.IDLE)
            {
                Abort();
                return;
            }

            _direction = NextDirection();

            // note that in principia after our node disappears this value will change to -1
            _ignitionUT = CalculateIgnitionUT();

            if (VesselState.time >= _ignitionUT - LeadTime && State != States.BURN)
                State = States.LEAD;

            if (VesselState.time >= _ignitionUT && AlignedAndSettled())
                State = States.BURN;

            switch (State)
            {
                case States.WARPALIGN:
                    StateWarpAlign();
                    return;
                case States.LEAD:
                    StateLeadTime();
                    return;
                case States.BURN:
                    StateBurn();
                    return;
            }
        }

        private void StateWarpAlign()
        {
            Core.Thrust.ThrustOff();

            if (!Autowarp)
            {
                SetAttitude();
                return;
            }

            if (MuUtils.PhysicsRunning() ? AlignedAndSettled() : AngleFromDirection() < Deg2Rad(10))
            {
                Core.Warp.WarpToUT(_ignitionUT - LeadTime);
                return;
            }

            double timeToBurn = _ignitionUT - VesselState.time;

            if (timeToBurn > 600)
            {
                Core.Warp.WarpToUT(_ignitionUT - 600);
                return;
            }

            Core.Warp.MinimumWarp();
            SetAttitude();
        }

        private void StateLeadTime()
        {
            Core.Thrust.ThrustOff();

            if (!MuUtils.PhysicsRunning())
            {
                Core.Warp.MinimumWarp();
                return;
            }

            SetAttitude();

            // update _dvLeft here because we're out of warp and might be doing RCS
            if (!_isLoadedPrincipia)
                _dvLeft = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).magnitude;
            else
                DecrementDvLeft();
        }

        private void StateBurn()
        {
            if (!MuUtils.PhysicsRunning())
            {
                Core.Warp.MinimumWarp();
                return;
            }

            SetAttitude();

            if (!_isLoadedPrincipia)
                _dvLeft = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).magnitude;
            else
                DecrementDvLeft();

            if (ShouldTerminate())
                return;

            if (!RCSOnly)
            {
                double timeConstant = _dvLeft > 10 || VesselState.minThrustAccel > 0.25 * VesselState.maxThrustAccel ? 0.5 : 2;
                Core.Thrust.ThrustForDV(_dvLeft, timeConstant);
            }
        }

        private void SetAttitude()
        {
            Core.Attitude.SetAxisControl(true, true, false);
            Core.Attitude.attitudeTo(_worldDirection, AttitudeReference.INERTIAL, this);
        }

        private bool ShouldTerminate()
        {
            if (_isLoadedPrincipia && _dvLeft < 0)
            {
                if (_mode == Mode.ALL_NODES && Vessel.patchedConicSolver.maneuverNodes.Count > 0)
                    Init();
                else
                    Abort();

                return true;
            }

            if (AngleFromNode() >= 0.5 * PI)
            {
                ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes[0];

                node.RemoveSelf();

                if (_mode == Mode.ALL_NODES && Vessel.patchedConicSolver.maneuverNodes.Count > 0)
                    Init();
                else
                    Abort();

                return true;
            }

            return false;
        }

        private bool AlignedAndSettled() => AngleFromDirection() < Deg2Rad(1) && Core.vessel.angularVelocity.magnitude < 0.001;

        // This returns the angle to the node (in radians), note that you probably don't want to use this outside of
        // stock checks for maneuver termination, and probably never in principia (see SafeCurrentPrincipiaNode()).
        private double AngleFromNode()
        {
            //Vector3d fwd = Quaternion.FromToRotation(VesselState.forward, VesselState.thrustForward) * VesselState.forward;
            Vector3d fwd = VesselState.forward;
            Vector3d dir = Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).normalized;
            return SafeAcos(Vector3d.Dot(fwd, dir));
        }

        // This returns the angle to saved inertial direction (in radians).
        private double AngleFromDirection()
        {
            //Vector3d fwd = Quaternion.FromToRotation(VesselState.forward, VesselState.thrustForward) * VesselState.forward;
            Vector3d fwd = VesselState.forward;
            Vector3d dir = _worldDirection.normalized;
            return SafeAcos(Vector3d.Dot(fwd, dir));
        }

        // This handles safely getting the current burning node in Principia without grabbing any subsequent node after
        // principia cleans it up for us.
        private ManeuverNode SafeCurrentPrincipiaNode()
        {
            if (!_hasNodes)
                return null;

            ManeuverNode node = Vessel.patchedConicSolver.maneuverNodes[0];

            // if we're burning the node may disappear in principia, if there's a subsequent node it will
            // appear at a future time, but we don't want to return that (yet anyway).
            if (State == States.BURN && node.UT > VesselState.time)
                return null;

            return node;
        }

        // This manages both initalization and updating of the saved inertial direction.  Note that this is a
        // 'derotated' world vector so that it is consistent across ticks.
        private Vector3d NextDirection()
        {
            var invRot = QuaternionD.Inverse(Planetarium.fetch.rotation);

            if (_direction != Vector3d.zero) // handle initialization
            {
                if (_isLoadedPrincipia && SafeCurrentPrincipiaNode() == null) return _direction;

                // FIXME: need to deal with RCS forward thrust accel here if we're RCSOnly
                if (!_isLoadedPrincipia && _dvLeft < VesselState.minThrustAccel) return _direction;
            }

            return invRot * Vessel.patchedConicSolver.maneuverNodes[0].GetBurnVector(Orbit).normalized;
        }

        private double CalculateIgnitionUT()
        {
            BurnTime(_dvLeft, out double halfBurnTime, out double spool);
            if (_isLoadedPrincipia)
            {
                ManeuverNode node = SafeCurrentPrincipiaNode();
                // in principia node.UT is the start of the burn and we need to subtract off the spool time
                // XXX: maybe we should return VesselState.time here instead of -1 to have it make more sense?
                return node != null ? Vessel.patchedConicSolver.maneuverNodes[0].UT - spool : -1;
            }

            // in stock node.UT is the center of the burn and the halfBurnTime calculation has the spool time
            return Vessel.patchedConicSolver.maneuverNodes[0].UT - halfBurnTime;
        }

        private void DecrementDvLeft()
        {
            if (!MuUtils.PhysicsRunning()) return;

            // decrement remaining dV based on engine and RCS thrust
            // Since this is Principia, we can't rely on the node's delta V itself updating, we have to do it ourselves.
            // We also can't just use vesselState.currentThrustAccel because only engines are counted.
            // NOTE: This *will* include acceleration from decouplers, which is pretty cool.
            Vector3d dV = (Vessel.acceleration_immediate - Vessel.graviticAcceleration) * TimeWarp.fixedDeltaTime;
            _dvLeft -= Vector3d.Dot(dV, _worldDirection);
        }

        // FIXME: this needs to work with RCSOnly
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
