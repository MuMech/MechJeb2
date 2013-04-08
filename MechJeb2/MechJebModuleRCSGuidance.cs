using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    //When enabled, the ascent guidance module makes the purple navball target point
    //along the ascent path. The ascent path can be set via SetPath. The ascent guidance
    //module disables itself if the player selects a different target.
    public class MechJebModuleRCSGuidance : DisplayModule
    {
        public MechJebModuleRCSGuidance(MechJebCore core) : base(core) { }

        const string TARGET_NAME = "RCS Guidance";

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.normal.textColor = Color.red;
            GUILayout.Label("HIGHLY EXPERIMENTAL", s);

            if (GUILayout.Button("Reset thrusters")) core.rcs.ResetThrusters();

            core.rcs.smartTranslation = GUILayout.Toggle(core.rcs.smartTranslation, "Smart translation");
            core.rcs.showThrusterStates = GUILayout.Toggle(core.rcs.showThrusterStates, "Show thruster states");
            if (core.rcs.showThrusterStates)
            {
                core.rcs.onlyWhenMoving = GUILayout.Toggle(core.rcs.onlyWhenMoving, "... only when moving");
            }
            
            if (core.rcs.smartTranslation)
            {
                core.rcs.advancedOptions = GUILayout.Toggle(core.rcs.advancedOptions, "Advanced options");
                if (core.rcs.advancedOptions)
                {
                    // The following options are really only useful for debugging.
                    core.rcs.multithreading = GUILayout.Toggle(core.rcs.multithreading, "Multithreading");

                    core.rcs.thrusterPowerControl = GUILayout.Toggle(core.rcs.thrusterPowerControl, "Power override");

                    if (core.rcs.thrusterPowerControl)
                    {
                        double oldThrusterPower = core.rcs.thrusterPower;
                        GuiUtils.SimpleTextBox("Thruster power", core.rcs.thrusterPower, "%");

                        int sliderPrecision = 3;
                        double sliderThrusterPower = GUILayout.HorizontalSlider((float)core.rcs.thrusterPower, 0.0F, 1.0F);
                        if (Math.Round(Math.Abs(sliderThrusterPower - oldThrusterPower), sliderPrecision) > 0)
                        {
                            core.rcs.thrusterPower = new EditableDoubleMult(Math.Round(sliderThrusterPower, sliderPrecision), 0.01);
                        }
                        core.rcs.globalThrusterPower = GUILayout.Toggle(core.rcs.globalThrusterPower, "Global power");
                    }
                    
                    GuiUtils.SimpleTextBox("Torque factor", core.rcs.tuningParamFactorTorque);
                    GuiUtils.SimpleTextBox("Translate factor", core.rcs.tuningParamFactorTranslate);
                    GuiUtils.SimpleTextBox("Waste factor", core.rcs.tuningParamFactorWaste);
                    GuiUtils.SimpleTextBox("Waste threshold", core.rcs.tuningParamWasteThreshold);

                    core.rcs.solverThread.solver.factorTorque = core.rcs.tuningParamFactorTorque;
                    core.rcs.solverThread.solver.factorTranslate = core.rcs.tuningParamFactorTranslate;
                    core.rcs.solverThread.solver.factorWaste = core.rcs.tuningParamFactorWaste;
                    core.rcs.solverThread.solver.wasteThreshold = core.rcs.tuningParamWasteThreshold;
                }

                core.rcs.debugInfo = GUILayout.Toggle(core.rcs.debugInfo, "Debug info");
                if (core.rcs.debugInfo)
                {
                    GUILayout.Label(String.Format("solver time: {0:F3} s", core.rcs.solverThread.timeSeconds));
                    GUILayout.Label(String.Format("total thrust: {0:F3}", core.rcs.solverThread.solver.totalThrust));
                    GUILayout.Label(String.Format("efficiency: {0:F3}%", core.rcs.solverThread.solver.efficiency * 100));
                    GUILayout.Label(String.Format("extra torque: {0:F3}", core.rcs.solverThread.solver.extraTorque.magnitude));
                    if (core.rcs.solverThread.statusString != null)
                    {
                        GUILayout.Label(String.Format("status: {0}", core.rcs.solverThread.statusString));
                    }
                }
            }

            if (core.rcs.smartTranslation || core.rcs.showThrusterStates)
            {
                core.rcs.users.Add(this);
            }
            else
            {
                core.rcs.users.Remove(this);
            }

            if (core.rcs.showThrusterStates)
            {
                GUILayout.Label(String.Format("control vector: {0}", core.rcs.controlVector));
                GUILayout.Label(String.Format("thruster states:\n{0}", core.rcs.thrusterStates));
                GUILayout.Label(String.Format("status: {0}", core.rcs.status));
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[]{ GUILayout.Width(240), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return TARGET_NAME;
        }
    }
}
