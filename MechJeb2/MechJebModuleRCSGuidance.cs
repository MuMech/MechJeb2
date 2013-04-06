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
            core.rcs.runSolver = GUILayout.Toggle(core.rcs.runSolver, "Run solver");
            core.rcs.applyResult = GUILayout.Toggle(core.rcs.applyResult, "Apply result");
            core.rcs.genuineThrottle = GUILayout.Toggle(core.rcs.genuineThrottle, "Genuine throttle");
            core.rcs.forceRecalculate = GUILayout.Toggle(core.rcs.forceRecalculate, "Force recalculation");
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
            GUILayout.Label(String.Format("thrusters used: {0}", core.rcs.thrustersUsed));
            GUILayout.Label(String.Format("efficiency: {0:F2}%", core.rcs.solver.efficiency * 100));
            GUILayout.Label(String.Format("extra torque: {0:F2}", core.rcs.solver.extraTorque.magnitude));

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
