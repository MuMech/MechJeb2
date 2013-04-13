using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRCSBalancer : ComputerModule
    {
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool smartTranslation = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool smartRotation = false;

        // Overdrive
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDoubleMult overdrive = new EditableDoubleMult(1, 0.01);

        // Advanced options
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool advancedOptions = false;

        // Advanced: overdrive scale. While 'overdrive' will range from 0..1,
        // we should reduce it slightly before using it to control the 'waste
        // threshold' tuning parameter, because waste thresholds of 1 or above
        // cause problems by allowing unhelpful thrusters to fire.
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble overdriveScale = 0.9;

        // Advanced: tuning parameters
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorTorque = 1;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorTranslate = 0.005;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public EditableDouble tuningParamFactorWaste = 1;

        // Debug info
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool debugInfo = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool showThrusterStates = false;
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        public bool onlyWhenMoving = true;

        public string status;
        public string thrusterStateStr = "";
        public string throttleCalcStr = "";

        // Variables for RCS solving.
        private RCSSolverThread solverThread = new RCSSolverThread();

        [EditableInfoItem("RCS balancing calculation precision", InfoItem.Category.Thrust)]
        public EditableInt calcPrecision = 3;

        [EditableInfoItem("RCS CoM shift trigger", InfoItem.Category.Thrust)]
        public EditableDouble comShiftTrigger = 0.01;

        public MechJebModuleRCSBalancer(MechJebCore core)
            : base(core)
        {
            priority = 700;
        }

        public override void OnModuleEnabled()
        {
            UpdateTuningParameters();
            solverThread.start();

            base.OnModuleEnabled();
        }

        public override void OnModuleDisabled()
        {
            solverThread.stop();

            base.OnModuleDisabled();
        }

        public void ResetThrusterForces()
        {
            solverThread.ResetThrusterForces();
        }

        public void GetThrottles(Vector3 direction, Vector3 rotation,
            out double[] throttles, out List<RCSSolver.Thruster> thrusters)
        {
            solverThread.GetThrottles(vessel, vesselState, direction, rotation, out throttles, out thrusters);
        }

        // Throttles RCS thrusters to keep a vessel balanced during translation.
        protected void AdjustRCSThrottles(FlightCtrlState s)
        {
            if (s.isNeutral)
            {
                solverThread.ResetThrusterForces();
                return;
            }

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
            Vector3 direction = new Vector3(-s.X, -s.Z, -s.Y);
            Vector3 rotation = new Vector3(s.pitch, s.roll, s.yaw);

            if (!smartRotation) rotation = Vector3.zero;

            // TODO: Update thrusters.

            double[] throttles;
            List<RCSSolver.Thruster> thrusters;
            GetThrottles(direction, rotation, out throttles, out thrusters);
            throttleCalcStr = "";

            // If the throttles we got were bad (due to the threaded calculation
            // not having completed yet), throttle all RCS thrusters to 0. It's
            // better to not move at all than move in the wrong direction.
            if (throttles == null || throttles.Length != thrusters.Count)
            {
                throttles = new double[thrusters.Count];
                for (int i = 0; i < thrusters.Count; i++)
                {
                    throttles[i] = 0;
                }
            }

            throttleCalcStr += "throttles:\n";
            for (int i = 0; i < throttles.Length; i++)
            {
                throttleCalcStr += String.Format("{0:F0}", throttles[i] * 9);
                if (i < throttles.Length - 1)
                {
                    throttleCalcStr += ((i + 1) % 16 == 0) ? "\n" : " ";
                }
            }

            // Apply the calculated throttles to all RCS parts.
            for (int i = 0; i < thrusters.Count; i++)
            {
                thrusters[i].partModule.thrusterPower = (float)throttles[i];
            }
        }

        public void UpdateTuningParameters()
        {
            double wasteThreshold = overdrive * overdriveScale;
            RCSSolverTuningParams tuningParams = new RCSSolverTuningParams();
            tuningParams.wasteThreshold     = wasteThreshold;
            tuningParams.factorTorque       = tuningParamFactorTorque;
            tuningParams.factorTranslate    = tuningParamFactorTranslate;
            tuningParams.factorWaste        = tuningParamFactorWaste;
            solverThread.UpdateTuningParameters(tuningParams);
        }

        public double GetCalculationTime()
        {
            return solverThread.calculationTime;
        }

        private string GetThrusterStates()
        {
            string thrusterStates = "";
            bool firstRcsModule = true;
            foreach (Part p in vessel.parts)
            {
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!firstRcsModule) thrusterStates += " ";
                    firstRcsModule = false;
                    thrusterStates += String.Format("({0:F0}:", pm.thrusterPower * 9);
                    for (int i = 0; i < pm.thrustForces.Count; i++)
                    {
                        if (i != 0) thrusterStates += ",";
                        thrusterStates += (pm.thrustForces[i] * 9).ToString("F0");
                    }
                    thrusterStates += ")";
                }
            }

            if (thrusterStates != "")
            {
                thrusterStates = "thruster states:\n" + thrusterStates;
            }
            return thrusterStates;
        }

        public override void Drive(FlightCtrlState s)
        {
            status = "";

            // Get thruster states -before- we adjust them.
            if (showThrusterStates && !(s.isNeutral && onlyWhenMoving))
            {
                thrusterStateStr = GetThrusterStates();
            }

            if (smartTranslation)
            {
                AdjustRCSThrottles(s);

                if (debugInfo)
                {
                    if (status != "") status += "\n";
                    status += String.Format("control vector: {0:F2} {1:F2} {2:F2} {3:F2} {4:F2} {5:F2}",
                        s.X, s.Y, s.Z, s.roll, s.pitch, s.yaw);
                    status += "\n(thruster/throttle values are 0..9)";
                    if (showThrusterStates && thrusterStateStr != "")
                    {
                        status += "\n" + thrusterStateStr;
                    }
                    if (throttleCalcStr != "")
                    {
                        status += "\n" + throttleCalcStr;
                    }
                }
            }

            base.Drive(s);
        }
    }
}
