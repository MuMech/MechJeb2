using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentGTMenu : MechJebModuleAscentMenuBase
    {
        public MechJebModuleAscentGTMenu(MechJebCore core)
            : base(core)
        {
            hidden = true;
        }

        public MechJebModuleAscentGT path { get { return autopilot.ascentPath as MechJebModuleAscentGT; } }
        public MechJebModuleAscentAutopilot autopilot;

        public override void OnStart(PartModule.StartState state)
        {
            autopilot = core.GetComputerModule<MechJebModuleAscentAutopilot>();
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(100) };
        }

        protected override void WindowGUI(int windowID)
        {
            if (path == null)
            {
                GUILayout.Label("Path is null!!!1!!1!1!1111!11eleven");
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            GuiUtils.SimpleTextBox("Turn start altitude:", path.turnStartAltitude, "km");
            GuiUtils.SimpleTextBox("Turn start velocity:", path.turnStartVelocity, "m/s");
            GuiUtils.SimpleTextBox("Turn start pitch:", path.turnStartPitch, "deg");
            GuiUtils.SimpleTextBox("Intermediate altitude:", path.intermediateAltitude, "km");
            GuiUtils.SimpleTextBox("Hold AP Time:", path.holdAPTime, "s");

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override string GetName()
        {
            return "Stock-style GravityTurn™ Pitch Program";
        }
    }
}
