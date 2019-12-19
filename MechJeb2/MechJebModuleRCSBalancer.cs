using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleRCSBalancer : ComputerModule
    {
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        [ToggleInfoItem("#MechJeb_smartTranslation", InfoItem.Category.Thrust)]//Smart RCS translation
        public bool smartTranslation = false;

        // Overdrive
        [Persistent(pass = (int)(Pass.Type | Pass.Global))]
        [EditableInfoItem("#MechJeb_RCSBalancerOverdrive", InfoItem.Category.Thrust, rightLabel = "%")]//RCS balancer overdrive
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

        // Variables for RCS solving.
        private RCSSolverThread solverThread = new RCSSolverThread();
        private List<RCSSolver.Thruster> thrusters = null;
        private double[] throttles = null;

        [EditableInfoItem("#MechJeb_RCSBalancerPrecision", InfoItem.Category.Thrust)]//RCS balancer precision
        public EditableInt calcPrecision = 3;

        [GeneralInfoItem("#MechJeb_RCSBalancerInfo", InfoItem.Category.Thrust)]//RCS balancer info
        public void RCSBalancerInfoItem()
        {
            GUILayout.BeginVertical();
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label1"), (solverThread.calculationTime * 1000).ToString("F0") + " ms");//"Calculation time"
            GuiUtils.SimpleLabelInt(Localizer.Format("#MechJeb_RCSBalancerInfo_Label2"), solverThread.taskCount);//"Pending tasks"

            GuiUtils.SimpleLabelInt(Localizer.Format("#MechJeb_RCSBalancerInfo_Label3"),   solverThread.cacheSize);//"Cache size"
            GuiUtils.SimpleLabelInt(Localizer.Format("#MechJeb_RCSBalancerInfo_Label4"),   solverThread.cacheHits);//"Cache hits"
            GuiUtils.SimpleLabelInt(Localizer.Format("#MechJeb_RCSBalancerInfo_Label5"), solverThread.cacheMisses);//"Cache misses"

            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label6"),     MuUtils.ToSI(solverThread.comError) + "m");//"CoM shift"
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label7"),    MuUtils.ToSI(solverThread.comErrorThreshold) + "m");//"CoM recalc"
            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label8"), MuUtils.ToSI(solverThread.maxComError) + "m");//"Max CoM shift"

            GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RCSBalancerInfo_Label9"), solverThread.statusString);//"Status"

            string error = solverThread.errorString;
            if (!string.IsNullOrEmpty(error))
            {
                GUILayout.Label(error, GUILayout.ExpandWidth(true));
            }

            GUILayout.EndVertical();
        }

        [GeneralInfoItem("#MechJeb_RCSThrusterStates", InfoItem.Category.Thrust)]//RCS thruster states
        private void RCSThrusterStateInfoItem()
        {
            GUILayout.BeginVertical();
            GUILayout.Label(Localizer.Format("#MechJeb_RCSThrusterStates_Label1"));//"RCS thrusters states (scaled to 0-9)"

            bool firstRcsModule = true;
            string thrusterStates = "";
            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part p = vessel.parts[index];
                foreach (ModuleRCS pm in p.Modules.OfType<ModuleRCS>())
                {
                    if (!firstRcsModule)
                    {
                        thrusterStates += " ";
                    }
                    firstRcsModule = false;
                    thrusterStates += String.Format("({0:F0}:", pm.thrusterPower * 9);
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

        [GeneralInfoItem("#MechJeb_RCSPartThrottles", InfoItem.Category.Thrust)]//RCS part throttles
        private void RCSPartThrottlesInfoItem()
        {
            GUILayout.BeginVertical();

            bool firstRcsModule = true;
            string thrusterStates = "";

            for (int index = 0; index < vessel.parts.Count; index++)
            {
                Part p = vessel.parts[index];
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

        [GeneralInfoItem("#MechJeb_ControlVector", InfoItem.Category.Thrust)]//Control vector
        private void ControlVectorInfoItem()
        {
            FlightCtrlState s = FlightInputHandler.state;

            string xyz = String.Format("{0:F2} {1:F2} {2:F2}", s.X, s.Y, s.Z);
            string rpy = String.Format("{0:F2} {1:F2} {2:F2}", s.roll, s.pitch, s.yaw);
            GUILayout.BeginVertical();
            GuiUtils.SimpleLabel("X/Y/Z", xyz);
            GuiUtils.SimpleLabel("R/P/Y", rpy);
            GUILayout.EndVertical();
        }
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

        public void GetThrottles(Vector3 direction, out double[] throttles, out List<RCSSolver.Thruster> thrusters)
        {
            solverThread.GetThrottles(vessel, vesselState, direction, out throttles, out thrusters);
        }

        // Throttles RCS thrusters to keep a vessel balanced during translation.
        protected void AdjustRCSThrottles(FlightCtrlState s)
        {
            bool cutThrottles = false;

            if (s.X == 0 && s.Y == 0 && s.Z == 0)
            {
                solverThread.ResetThrusterForces();
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
            
            // RCS balancing on rotation isn't supported.
            //Vector3 rotation = new Vector3(s.pitch, s.roll, s.yaw);

            RCSSolverKey.SetPrecision(calcPrecision);
            GetThrottles(direction, out throttles, out thrusters);

            // If the throttles we got were bad (due to the threaded
            // calculation not having completed yet), cut throttles. It's
            // better to not move at all than move in the wrong direction.
            if (throttles.Length != thrusters.Count)
            {
                throttles = new double[thrusters.Count];
                cutThrottles = true;
            }

            if (cutThrottles)
            {
                for (int i = 0; i < throttles.Length; i++)
                {
                    throttles[i] = 0;
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

        /*
        public override void OnUpdate()
        {
            // Make thruster exhaust onscreen correspond to actual thrust.
            if (smartTranslation && throttles != null)
            {
                for (int i = 0; i < throttles.Length; i++)
                {
                    // 'throttles' and 'thrusters' are guaranteed to be of the
                    // same length.
                    float throttle = (float)throttles[i];
                    var tfx = thrusters[i].partModule.thrusterFX;

                    for (int j = 0; j < tfx.Count; j++)
                    {
                        tfx[j].Power *= throttle;
                    }
                }
            }
            base.OnUpdate();
        }
         */

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
