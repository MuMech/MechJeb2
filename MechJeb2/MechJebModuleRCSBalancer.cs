extern alias JetBrainsAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using KSP.Localization;
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

        // Aux class to save/restore original thrusterPower
        private class ManagedRCSModule
        {
            public readonly ModuleRCS Module;
            private readonly float originalPower;

            public ManagedRCSModule(ModuleRCS module)
            {
                Module = module;
                originalPower = module.thrusterPower;
            }

            public void SetThrottle(double throttle) => Module.thrusterPower = (float)throttle;

            public void Restore() => Module.thrusterPower = originalPower;
        }

        // Variables for RCS solving.
        private readonly RCSSolverThread solverThread = new RCSSolverThread();

        // managedModules[i] and thrusterGeometry[i] describe the same RCS module,
        // and solver throttles are indexed the same way.
        private readonly List<ManagedRCSModule> managedModules = new List<ManagedRCSModule>();
        private RCSSolver.Thruster[] thrusterGeometry = Array.Empty<RCSSolver.Thruster>();

        // Vessel change detection.
        private int lastPartCount;
        private readonly List<ModuleRCS> lastDisabled = new List<ModuleRCS>();
        private Vector3 lastCoM = Vector3.zero;

        // A moving average reduces measurement error due to ship flexing.
        private readonly MovingAverage comError = new MovingAverage();

        public double ComErrorThreshold { get; private set; }
        public double MaxComError       { get; private set; }

        [EditableInfoItem("#MechJeb_RCSBalancerPrecision", InfoItem.Category.Thrust)] //RCS balancer precision
        public readonly EditableInt calcPrecision = 3;

        [GeneralInfoItem("#MechJeb_RCSBalancerInfo", InfoItem.Category.Thrust)] //RCS balancer info
        public void RCSBalancerInfoItem()
        {
            GUILayout.BeginVertical();
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label1"),
                (solverThread.CalculationTime * 1000).ToString("F0") + " ms"); //"Calculation time"
            GuiUtils.SimpleLabelInt(Localizer.Format("#MechJeb_RCSBalancerInfo_Label2"), solverThread.TaskCount); //"Pending tasks"

            GuiUtils.SimpleLabelInt(Localizer.Format("#MechJeb_RCSBalancerInfo_Label3"), solverThread.CacheSize); //"Cache size"
            GuiUtils.SimpleLabelInt(Localizer.Format("#MechJeb_RCSBalancerInfo_Label4"), solverThread.CacheHits); //"Cache hits"
            GuiUtils.SimpleLabelInt(Localizer.Format("#MechJeb_RCSBalancerInfo_Label5"), solverThread.CacheMisses); //"Cache misses"

            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label6"), comError.Value.ToSI() + "m"); //"CoM shift"
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label7"), ComErrorThreshold.ToSI() + "m"); //"CoM recalc"
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label8"), MaxComError.ToSI() + "m"); //"Max CoM shift"

            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label9"), solverThread.StatusString); //"Status"

            string error = solverThread.ErrorString;
            if (!string.IsNullOrEmpty(error))
            {
                GUILayout.Label(error, GuiUtils.LayoutExpandWidth);
            }

            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_RCSThrusterStates", InfoItem.Category.Thrust)] //RCS thruster states
        private void RCSThrusterStateInfoItem()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_RCSThrusterStates_Label1")); //"RCS thrusters states (scaled to 0-9)"

            bool firstRcsModule = true;
            string thrusterStates = "";
            for (int index = 0; index < Vessel.parts.Count; index++)
            {
                Part p = Vessel.parts[index];
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!firstRcsModule)
                    {
                        thrusterStates += " ";
                    }

                    firstRcsModule = false;
                    thrusterStates += $"({pm.thrusterPower * 9:F0}:";
                    for (int i = 0; i < pm.thrustForces.Length; i++)
                    {
                        if (i != 0)
                        {
                            thrusterStates += ",";
                        }

                        thrusterStates += (pm.thrustForces[i] * 9).ToString("F0");
                    }

                    thrusterStates += ")";
                }
            }

            GUILayout.Label(thrusterStates);
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_RCSPartThrottles", InfoItem.Category.Thrust)] //RCS part throttles
        private void RCSPartThrottlesInfoItem()
        {
            GUILayout.BeginVertical();

            bool firstRcsModule = true;
            string thrusterStates = "";

            for (int index = 0; index < Vessel.parts.Count; index++)
            {
                Part p = Vessel.parts[index];
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!firstRcsModule)
                    {
                        thrusterStates += " ";
                    }

                    firstRcsModule = false;
                    thrusterStates += pm.thrusterPower.ToString("F1");
                }
            }

            GUILayout.Label(thrusterStates);
            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_ControlVector", InfoItem.Category.Thrust)] //Control vector
        private void ControlVectorInfoItem()
        {
            FlightCtrlState s = FlightInputHandler.state;

            string xyz = $"{s.X:F2} {s.Y:F2} {s.Z:F2}";
            string rpy = $"{s.roll:F2} {s.pitch:F2} {s.yaw:F2}";
            GUILayout.BeginVertical();
            GuiUtils.SimpleLabel("X/Y/Z", xyz);
            GuiUtils.SimpleLabel("R/P/Y", rpy);
            GUILayout.EndVertical();
        }

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

        public void ResetThrusterForces()
        {
            foreach (ManagedRCSModule m in managedModules)
                m.Restore();
        }

        private void CheckVessel()
        {
            bool changed = false;

            // Rotates world-frame vectors into the vessel's reference frame.
            Quaternion worldToVessel = Quaternion.Inverse(Vessel.GetTransform().rotation);

            if (Vessel.parts.Count != lastPartCount)
            {
                lastPartCount = Vessel.parts.Count;
                changed = true;
            }

            // Make sure all thrusters are still enabled, because if they're not,
            // our calculations will be wrong.
            foreach (ManagedRCSModule m in managedModules)
            {
                if (!m.Module.isEnabled)
                {
                    changed = true;
                    break;
                }
            }

            // Likewise, make sure any previously-disabled RCS modules are still
            // disabled.
            foreach (ModuleRCS pm in lastDisabled)
            {
                if (pm.isEnabled)
                {
                    changed = true;
                    break;
                }
            }

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

                Vector3 com = worldToVessel * (VesselState.CoM - VesselState.RootPartPosition);
                double thisComErr = (lastCoM - com).magnitude;
                MaxComError = Math.Max(MaxComError, thisComErr);
                comError.Value = thisComErr;
                if (comError > ComErrorThreshold)
                {
                    lastCoM = com;
                    changed = true;
                }
            }

            if (!changed) return;

            // Something about the vessel has changed. We need to reset everything.

            lastDisabled.Clear();

            // Restore the old modules before wrapping them again, since a new
            // ManagedRCSModule takes the module's current power as its original.
            ResetThrusterForces();
            managedModules.Clear();

            // Rebuild the list of thrusters.
            var geometry = new List<RCSSolver.Thruster>();
            foreach (Part p in Vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!pm.isEnabled)
                    {
                        // Keep track of this module so we'll know if it's enabled.
                        lastDisabled.Add(pm);
                    }
                    else if (p.Rigidbody != null && !pm.isJustForShow)
                    {
                        // The part's offset from the vessel's center of mass.
                        Vector3 pos = worldToVessel * (p.Rigidbody.worldCenterOfMass - VesselState.CoM);

                        // Create a single RCSSolver.Thruster for this part. This
                        // requires some assumptions about how the game's RCS code will
                        // drive the individual thrusters (which we can't control).

                        var thrustDirs = new Vector3[pm.thrusterTransforms.Count];
                        for (int i = 0; i < pm.thrusterTransforms.Count; i++)
                        {
                            thrustDirs[i] = (worldToVessel * -pm.thrusterTransforms[i].up).normalized;
                        }

                        managedModules.Add(new ManagedRCSModule(pm));
                        geometry.Add(new RCSSolver.Thruster(pos, thrustDirs));
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
            var direction = new Vector3(-s.X, -s.Z, -s.Y);

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

            // If the throttles we got were bad (due to the threaded
            // calculation not having completed yet), cut throttles. It's
            // better to not move at all than move in the wrong direction.
            if (throttles.Length != managedModules.Count)
            {
                throttles = new double[managedModules.Count];
            }

            // Apply the calculated throttles to all RCS parts.
            for (int i = 0; i < managedModules.Count; i++)
            {
                managedModules[i].SetThrottle(throttles[i]);
            }
        }

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

            base.Drive(s);
        }
    }
}
