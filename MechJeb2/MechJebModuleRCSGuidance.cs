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

        const string TARGET_NAME = "RCS Control";

        private void SimpleTextInfo(string left, string right)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(left, GUILayout.ExpandWidth(true));
            GUILayout.Label(right, GUILayout.ExpandWidth(false));
            GUILayout.EndHorizontal();
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            bool wasEnabled = core.rcs.smartTranslation;

            GUILayout.BeginHorizontal();
            core.rcs.smartTranslation = GUILayout.Toggle(core.rcs.smartTranslation, "Smart translation");
            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.normal.textColor = Color.yellow;
            GUILayout.Label("experimental", s);
            GUILayout.EndHorizontal();

            if (wasEnabled && !core.rcs.smartTranslation)
            {
                core.rcs.ResetThrusters();
            }
            
            if (core.rcs.smartTranslation)
            {
                // Overdrive

                double oldOverdrive         = core.rcs.overdrive;
                double oldOverdriveScale    = core.rcs.overdriveScale;
                double oldFactorTorque      = core.rcs.tuningParamFactorTorque;
                double oldFactorTranslate   = core.rcs.tuningParamFactorTranslate;
                double oldFactorWaste       = core.rcs.tuningParamFactorWaste;

                GuiUtils.SimpleTextBox("Overdrive", core.rcs.overdrive, "%");

                double sliderVal = GUILayout.HorizontalSlider((float)core.rcs.overdrive, 0.0F, 1.0F);
                int sliderPrecision = 3;
                if (Math.Round(Math.Abs(sliderVal - oldOverdrive), sliderPrecision) > 0)
                {
                    double rounded = Math.Round(sliderVal, sliderPrecision);
                    core.rcs.overdrive = new EditableDoubleMult(rounded, 0.01);
                }

                GUILayout.Label("Overdrive increases power when possible, at the cost of RCS fuel efficiency.");


                // Advanced options

                core.rcs.advancedOptions = GUILayout.Toggle(core.rcs.advancedOptions, "Advanced options");
                if (core.rcs.advancedOptions)
                {
                    core.rcs.multiAxisFix = GUILayout.Toggle(core.rcs.multiAxisFix, "Attempt multi-axis corrections");
                    if (!core.rcs.multiAxisFix)
                    {
                        core.rcs.perThrusterControl = GUILayout.Toggle(core.rcs.perThrusterControl, "Per-thruster control (broken)");
                    }

                    GuiUtils.SimpleTextBox("Overdrive scale", core.rcs.overdriveScale);
                    GuiUtils.SimpleTextBox("Torque factor", core.rcs.tuningParamFactorTorque);
                    GuiUtils.SimpleTextBox("Translate factor", core.rcs.tuningParamFactorTranslate);
                    GuiUtils.SimpleTextBox("Waste factor", core.rcs.tuningParamFactorWaste);
                }


                // Debug info

                core.rcs.debugInfo = GUILayout.Toggle(core.rcs.debugInfo, "Debug info");
                if (core.rcs.debugInfo)
                {
                    core.rcs.showThrusterStates = GUILayout.Toggle(core.rcs.showThrusterStates, "Show thruster states");
                    if (core.rcs.showThrusterStates)
                    {
                        core.rcs.onlyWhenMoving = GUILayout.Toggle(core.rcs.onlyWhenMoving, "... update only when moving");
                    }
                    SimpleTextInfo("Calculation time", (int)(core.rcs.solverThread.timeSeconds * 1000) + " ms");
                }

                if (core.rcs.status != null && core.rcs.status != "")
                {
                    GUILayout.Label(core.rcs.status);
                }


                // Apply tuning parameters.

                if (oldOverdrive != core.rcs.overdrive
                    || oldOverdriveScale != core.rcs.overdriveScale
                    || oldFactorTorque != core.rcs.tuningParamFactorTorque
                    || oldFactorTranslate != core.rcs.tuningParamFactorTranslate
                    || oldFactorWaste != core.rcs.tuningParamFactorWaste)
                {
                    double wasteThreshold = core.rcs.overdrive * core.rcs.overdriveScale;
                    core.rcs.solverThread.solver.wasteThreshold = wasteThreshold;
                    core.rcs.solverThread.solver.factorTorque = core.rcs.tuningParamFactorTorque;
                    core.rcs.solverThread.solver.factorTranslate = core.rcs.tuningParamFactorTranslate;
                    core.rcs.solverThread.solver.factorWaste = core.rcs.tuningParamFactorWaste;
                    core.rcs.ForceRecalculate();
                }
            }

            if (core.rcs.smartTranslation)
            {
                core.rcs.users.Add(this);
            }
            else
            {
                core.rcs.users.Remove(this);
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
