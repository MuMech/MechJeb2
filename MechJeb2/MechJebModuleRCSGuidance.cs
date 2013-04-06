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

        bool selfEnabled = false;

        public override void OnModuleEnabled()
        {
            selfEnabled = core.rcs.users.AmIUser(this);
        }

        public override void OnModuleDisabled()
        {
            core.rcs.users.Remove(this);
        }

        public override void OnFixedUpdate()
        {
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUIStyle s = new GUIStyle(GUI.skin.label);
            s.normal.textColor = Color.red;
            GUILayout.Label("HIGHLY EXPERIMENTAL", s);

            selfEnabled = GUILayout.Toggle(selfEnabled, "Enable");
            if (selfEnabled)
            {
                core.rcs.users.Add(this);
                core.rcs.smartTranslation = GUILayout.Toggle(core.rcs.smartTranslation, "Smart translation");
                core.rcs.runSolver = GUILayout.Toggle(core.rcs.runSolver, "Run solver");
                core.rcs.applyResult = GUILayout.Toggle(core.rcs.applyResult, "Apply result");
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

                    if (oldThrusterPower != core.rcs.thrusterPower)
                    {
                        core.rcs.ApplyThrusterPower();
                    }
                }
                GUILayout.Label(String.Format("disabled thrusters: {0}", core.rcs.thrustersDisabled));
                GUILayout.Label(String.Format("efficiency: {0}%", core.rcs.solver.efficiency * 100));
                GUILayout.Label(String.Format("extra torque: {0}", core.rcs.solver.extraTorque));
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
