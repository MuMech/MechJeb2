extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using MechJebLib.Control;
using MechJebLib.Primitives;
using MechJebLibBindings;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleRCSBalancer : ComputerModule
    {
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL)), ToggleInfoItem("#MechJeb_smartTranslation", InfoItem.Category.Thrust)]
        //Smart RCS translation
        public bool smartTranslation;

        // Overdrive
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL)), EditableInfoItem("#MechJeb_RCSBalancerOverdrive", InfoItem.Category.Thrust, rightLabel = "%")]
        //RCS balancer overdrive
        public EditableDoubleMult overdrive = new EditableDoubleMult(1, 0.01);

        // Advanced options
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public bool advancedOptions = false;

        // Advanced: overdrive scale. While 'overdrive' will range from 0..1,
        // we should reduce it slightly before using it to control the 'waste
        // threshold' tuning parameter, because waste thresholds of 1 or above
        // cause problems by allowing unhelpful thrusters to fire.
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble overdriveScale = 0.9;

        // Advanced: tuning parameters
        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble tuningParamFactorTorque = 1;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble tuningParamFactorTranslate = 0.005;

        [Persistent(pass = (int)(Pass.TYPE | Pass.GLOBAL))]
        public readonly EditableDouble tuningParamFactorWaste = 1;

        // ModuleRCS ignores an axis of the translation input under this, its EPSILON.
        private const float MIN_INPUT = 0.05f;

        // Aux class to save/restore original flowMult, the one throttle ModuleRCS
        // has in flight: CalculateThrust multiplies by it and nothing in stock
        // writes it. thrusterPower only counts at load, and flowMult isn't a
        // KSPField, so it can't end up in a save.
        private class ManagedRCSModule
        {
            public readonly ModuleRCS Module;
            private readonly double originalFlowMult;

            // The settings the solver was told about.
            private readonly float thrustPercentage;
            private readonly bool enableX;
            private readonly bool enableY;
            private readonly bool enableZ;

            public ManagedRCSModule(ModuleRCS module)
            {
                Module = module;
                originalFlowMult = module.flowMult;
                thrustPercentage = module.thrustPercentage;
                enableX = module.enableX;
                enableY = module.enableY;
                enableZ = module.enableZ;
            }

            public bool SettingsChanged =>
                Module.thrustPercentage != thrustPercentage || Module.enableX != enableX || Module.enableY != enableY || Module.enableZ != enableZ;

            public void SetThrottle(double throttle) => Module.flowMult = originalFlowMult * throttle;

            public void Restore() => Module.flowMult = originalFlowMult;
        }

        // Variables for RCS solving.
        private readonly RCSSolverThread solverThread = new RCSSolverThread();

        // managedModules[i] and thrusterGeometry[i] describe the same RCS module,
        // and solver throttles are indexed the same way.
        private readonly List<ManagedRCSModule> managedModules = new List<ManagedRCSModule>();
        private RCSSolver.Thruster[] thrusterGeometry = Array.Empty<RCSSolver.Thruster>();

        // Vessel change detection.
        private float lastCheckTime;
        private int lastPartCount;
        private Transform lastReferenceTransform;
        private readonly List<ModuleRCS> lastDisabled = new List<ModuleRCS>();

        // Keeps ship flexing from counting as the CoM moving.
        private readonly CoMShiftTracker comTracker = new CoMShiftTracker();

        public double ComErrorThreshold { get; private set; }
        public double MaxComError       { get; private set; }

        // read by the new UI's version of the RCS balancer info item
        internal RCSSolverThread SolverThread => solverThread;
        internal double ComError => comTracker.Error;

        [EditableInfoItem("#MechJeb_RCSBalancerPrecision", InfoItem.Category.Thrust)] //RCS balancer precision
        public readonly EditableInt calcPrecision = 3;

        [GeneralInfoItem("#MechJeb_RCSBalancerInfo", InfoItem.Category.Thrust)] //RCS balancer info
        public void RCSBalancerInfoItem() { }

        [GeneralInfoItem("#MechJeb_RCSThrusterStates", InfoItem.Category.Thrust)] //RCS thruster states
        private void RCSThrusterStateInfoItem() { }

        [GeneralInfoItem("#MechJeb_RCSPartThrottles", InfoItem.Category.Thrust)] //RCS part throttles
        private void RCSPartThrottlesInfoItem() { }

        [GeneralInfoItem("#MechJeb_ControlVector", InfoItem.Category.Thrust)] //Control vector
        private void ControlVectorInfoItem() { }

        public MechJebModuleRCSBalancer(MechJebCore core)
            : base(core)
        {
            Priority = 700;
        }

        protected override void OnModuleEnabled()
        {
            // Make sure CheckVessel() doesn't try to reuse throttle info from
            // when this module was enabled previously, even if the vessel hasn't
            // changed. Invalidating vessel information on enable lets the UI
            // toggle act as a reset button.
            lastCheckTime = -1;
            lastPartCount = 0;
            MaxComError = 0;

            UpdateTuningParameters();
            solverThread.Start();

            base.OnModuleEnabled();
        }

        protected override void OnModuleDisabled()
        {
            solverThread.Stop();
            ResetThrusterForces();

            base.OnModuleDisabled();
        }

        public override void OnDestroy()
        {
            solverThread.Stop();
            ResetThrusterForces();

            base.OnDestroy();
        }

        public void ResetThrusterForces()
        {
            foreach (ManagedRCSModule m in managedModules)
                m.Restore();
        }

        // What ModuleRCS.FixedUpdate asks of a module before it fires, fuel aside.
        private static bool CanFire(ModuleRCS pm) =>
            pm.isEnabled && pm.moduleIsEnabled && pm.rcsEnabled && !pm.isJustForShow && (!pm.part.ShieldedFromAirstream || pm.shieldedCanThrust);

        private bool ThrustersChanged()
        {
            // Make sure all thrusters can still fire and are set up as they were,
            // because if they're not, our calculations will be wrong.
            foreach (ManagedRCSModule m in managedModules)
            {
                if (!CanFire(m.Module) || m.SettingsChanged)
                {
                    return true;
                }
            }

            // Likewise, make sure any previously-disabled RCS modules are still
            // disabled.
            foreach (ModuleRCS pm in lastDisabled)
            {
                if (CanFire(pm))
                {
                    return true;
                }
            }

            return false;
        }

        // One RCSSolver.Thruster for a module. It's the whole module's throttle we
        // can control, and the game then drives the individual nozzles.
        private RCSSolver.Thruster GetThruster(ModuleRCS pm, Quaternion worldToVessel)
        {
            var positions = new List<V3>();
            var thrustDirs = new List<V3>();

            foreach (Transform t in pm.thrusterTransforms)
            {
                // The nozzles of the other part variants are in the list too.
                if (!t.gameObject.activeInHierarchy)
                {
                    continue;
                }

                // The nozzle's offset from the vessel's center of mass.
                Vector3d pos = worldToVessel * (t.position - VesselState.CoM);
                Vector3d thrustDir = (worldToVessel * (pm.useZaxis ? -t.forward : -t.up)).normalized;

                positions.Add(pos.ToV3());
                thrustDirs.Add(thrustDir.ToV3());
            }

            double power = pm.thrusterPower * pm.thrustPercentage * 0.01 * pm.flowMult;

            // The input's Y and Z are the vessel's Z and Y.
            var axes = new V3(pm.enableX ? 1 : 0, pm.enableZ ? 1 : 0, pm.enableY ? 1 : 0);

            return new RCSSolver.Thruster(positions.ToArray(), thrustDirs.ToArray(), power, axes);
        }

        private void CheckVessel()
        {
            // Once per physics tick, however many callers ask. VesselState alone
            // asks for six directions.
            if (lastCheckTime == Time.fixedTime)
            {
                return;
            }

            lastCheckTime = Time.fixedTime;

            bool changed = false;

            // Rotates world-frame vectors into the vessel's reference frame.
            Transform referenceTransform = Vessel.GetTransform();
            var worldToVessel = Quaternion.Inverse(referenceTransform.rotation);

            if (Vessel.parts.Count != lastPartCount)
            {
                lastPartCount = Vessel.parts.Count;
                changed = true;
            }

            // Everything the solver knows is in this frame, and the cached
            // directions with it.
            if (referenceTransform != lastReferenceTransform)
            {
                lastReferenceTransform = referenceTransform;
                changed = true;
            }

            // Not after a change of parts, when these modules may be gone.
            changed = changed || ThrustersChanged();

            // See if the CoM has moved too much.
            Rigidbody rootPartBody = Vessel.rootPart.rb;
            if (rootPartBody != null)
            {
                // But how much is "too much"? Well, it probably has something to do
                // with the ship's moment of inertia (MoI). Let's say the distance
                // 'd' that the CoM is allowed to shift without a reset is:
                //
                //      d = moi * x + c
                //
                // where 'moi' is the magnitude of the ship's moment of inertia and
                // 'x' and 'c' are tuning parameters to be determined.
                //
                // Using a few actual KSP ships, I burned RCS fuel (or moved fuel
                // from one tank to another) to see how far the CoM could shift
                // before the rotation error on translation became annoying.
                // I came up with roughly:
                //
                //      d         moi
                //      0.005    2.34
                //      0.04    11.90
                //      0.07    19.96
                //
                // I then halved each 'd' value, because we'd like to address this
                // problem -before- it becomes annoying. Least-squares linear
                // regression on the (moi, d/2) pairs gives the following (with
                // adjusted R^2 = 0.999966):
                //
                //      moi = 542.268 d + 1.00654
                //      d = (moi - 1) / 542
                //
                // So the numbers below have some basis in reality. =)

                // Assume MoI magnitude is always >=2.34, since that's all I tested.
                ComErrorThreshold = (Math.Max(VesselState.MoI.magnitude, 2.34) - 1) / 542;

                Vector3d com = worldToVessel * (VesselState.CoM - VesselState.RootPartPosition);

                // Any other change starts over, since it can move the CoM or the
                // frame in one go.
                if (changed)
                {
                    comTracker.Reset(com.ToV3());
                }
                else if (comTracker.Update(com.ToV3(), ComErrorThreshold))
                {
                    changed = true;
                }

                MaxComError = Math.Max(MaxComError, comTracker.Error);
            }

            if (!changed) return;

            // Something about the vessel has changed. We need to reset everything.

            lastDisabled.Clear();

            // Restore the old modules before wrapping them again, since a new
            // ManagedRCSModule takes the module's current flowMult as its original.
            ResetThrusterForces();
            managedModules.Clear();

            // Rebuild the list of thrusters.
            var geometry = new List<RCSSolver.Thruster>();
            foreach (Part p in Vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!CanFire(pm))
                    {
                        // Keep track of this module so we'll know if it can again.
                        lastDisabled.Add(pm);
                    }
                    else
                    {
                        managedModules.Add(new ManagedRCSModule(pm));
                        geometry.Add(GetThruster(pm, worldToVessel));
                    }
                }
            }

            thrusterGeometry = geometry.ToArray();
            solverThread.SetThrusters(thrusterGeometry);
        }

        // The list of throttles is ordered under the assumption that you iterate
        // over the vessel as follows:
        //      foreach part in vessel.parts:
        //          foreach rcsModule in part.Modules.OfType<ModuleRCS>:
        //              ...
        // The throttles will be empty if they haven't been calculated yet.
        public void GetThrottles(Vector3 direction, out double[] throttles, out IReadOnlyList<RCSSolver.Thruster> thrusters)
        {
            // Update vessel info if needed.
            CheckVessel();

            thrusters = thrusterGeometry;
            throttles = solverThread.GetThrottles(direction);
        }

        // Throttles RCS thrusters to keep a vessel balanced during translation.
        protected void AdjustRCSThrottles(FlightCtrlState s)
        {
            // Update vessel info if needed.
            CheckVessel();

            // Note that FlightCtrlState doesn't use the same axes as the
            // vehicle's reference frame. FlightCtrlState coordinates are right-
            // handed, with vessel prograde being -Z. Vessel coordinates
            // are left-handed, with vessel prograde being +Y. Here's how
            // FlightCtrlState relates to various ship directions (and their
            // default keyboard shortcuts):
            //           up (i): y -1
            //         down (k): y +1
            //         left (j): x +1
            //        right (l): x -1
            //      forward (h): z -1
            //     backward (n): z +1
            // To turn this vector into a vessel-relative one, we need to negate
            // each value and also swap the Y and Z values.
            var direction = new Vector3(-InputThatCounts(s.X), -InputThatCounts(s.Z), -InputThatCounts(s.Y));

            // Not translating, so let the game use the thrusters at full power
            // (e.g. for rotation).
            if (direction == Vector3.zero)
            {
                ResetThrusterForces();
                return;
            }

            // RCS balancing on rotation isn't supported.
            //Vector3 rotation = new Vector3(s.pitch, s.roll, s.yaw);

            RCSSolverKey.SetPrecision(calcPrecision);
            double[] throttles = solverThread.GetThrottles(direction);

            // If the threaded calculation hasn't completed yet, leave the thrusters
            // as they are for these few ticks: at the throttles for the last
            // direction, or at full power. Cutting them would drop the translation,
            // and the attitude control that shares them, each time the direction
            // reaches a new bucket.
            if (throttles.Length != managedModules.Count)
            {
                return;
            }

            // Apply the calculated throttles to all RCS parts.
            for (int i = 0; i < managedModules.Count; i++)
            {
                managedModules[i].SetThrottle(throttles[i]);
            }
        }

        private static float InputThatCounts(float input) => Mathf.Abs(input) < MIN_INPUT ? 0 : input;

        public void UpdateTuningParameters()
        {
            double wasteThreshold = overdrive * overdriveScale;
            var tuningParams = new RCSSolverTuningParams(wasteThreshold, tuningParamFactorTorque, tuningParamFactorTranslate,
                tuningParamFactorWaste);
            solverThread.UpdateTuningParameters(tuningParams);
        }

        public double GetCalculationTime() => solverThread.CalculationTime;

        public override void Drive(FlightCtrlState s)
        {
            if (smartTranslation)
            {
                AdjustRCSThrottles(s);
            }
            else
            {
                // The info item can turn smart translation off without disabling us.
                ResetThrusterForces();
            }

            base.Drive(s);
        }
    }
}
