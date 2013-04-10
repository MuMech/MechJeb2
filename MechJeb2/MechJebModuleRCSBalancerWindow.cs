﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRCSBalancerWindow : DisplayModule
    {
        public MechJebModuleRCSBalancer balancer;

        public override void OnStart(PartModule.StartState state)
        {
            balancer = core.GetComputerModule<MechJebModuleRCSBalancer>();

            base.OnStart(state);
        }

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

            bool wasEnabled = balancer.smartTranslation;

            GUILayout.BeginHorizontal();
            balancer.smartTranslation = GUILayout.Toggle(balancer.smartTranslation, "Smart translation");
            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.normal.textColor = Color.yellow;
            GUILayout.Label("experimental", s);
            GUILayout.EndHorizontal();

            if (wasEnabled != balancer.smartTranslation)
            {
                balancer.ResetThrusters();

                if (balancer.smartTranslation)
                {
                    balancer.users.Add(this);
                }
                else
                {
                    balancer.users.Remove(this);
                }
            }

            if (balancer.smartTranslation)
            {
                // Overdrive
                double oldOverdrive = balancer.overdrive;
                double oldOverdriveScale = balancer.overdriveScale;
                double oldFactorTorque = balancer.tuningParamFactorTorque;
                double oldFactorTranslate = balancer.tuningParamFactorTranslate;
                double oldFactorWaste = balancer.tuningParamFactorWaste;

                GuiUtils.SimpleTextBox("Overdrive", balancer.overdrive, "%");

                double sliderVal = GUILayout.HorizontalSlider((float)balancer.overdrive, 0.0F, 1.0F);
                int sliderPrecision = 3;
                if (Math.Round(Math.Abs(sliderVal - oldOverdrive), sliderPrecision) > 0)
                {
                    double rounded = Math.Round(sliderVal, sliderPrecision);
                    balancer.overdrive = new EditableDoubleMult(rounded, 0.01);
                }

                GUILayout.Label("Overdrive increases power when possible, at the cost of RCS fuel efficiency.");

                // Advanced options
                balancer.advancedOptions = GUILayout.Toggle(balancer.advancedOptions, "Advanced options");
                if (balancer.advancedOptions)
                {
                    GuiUtils.SimpleTextBox("Overdrive scale", balancer.overdriveScale);
                    GuiUtils.SimpleTextBox("Torque factor", balancer.tuningParamFactorTorque);
                    GuiUtils.SimpleTextBox("Translate factor", balancer.tuningParamFactorTranslate);
                    GuiUtils.SimpleTextBox("Waste factor", balancer.tuningParamFactorWaste);
                }

                // Debug info
                balancer.debugInfo = GUILayout.Toggle(balancer.debugInfo, "Debug info");
                if (balancer.debugInfo)
                {
                    balancer.showThrusterStates = GUILayout.Toggle(balancer.showThrusterStates, "Show thruster states");
                    if (balancer.showThrusterStates)
                    {
                        balancer.onlyWhenMoving = GUILayout.Toggle(balancer.onlyWhenMoving, "... update only when moving");
                    }
                    SimpleTextInfo("Calculation time", (int)(balancer.solverThread.timeSeconds * 1000) + " ms");
                }

                if (balancer.status != null && balancer.status != "")
                {
                    GUILayout.Label(balancer.status);
                }

                // Apply tuning parameters.
                if (oldOverdrive != balancer.overdrive
                    || oldOverdriveScale != balancer.overdriveScale
                    || oldFactorTorque != balancer.tuningParamFactorTorque
                    || oldFactorTranslate != balancer.tuningParamFactorTranslate
                    || oldFactorWaste != balancer.tuningParamFactorWaste)
                {
                    balancer.UpdateTuningParameters();
                }
            }

            if (balancer.smartTranslation)
            {
                balancer.users.Add(this);
            }
            else
            {
                balancer.users.Remove(this);
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(240), GUILayout.Height(30) };
        }

        public override string GetName()
        {
            return "RCS Balancer";
        }

        public MechJebModuleRCSBalancerWindow(MechJebCore core) : base(core) { }
    }
}
