﻿using UnityEngine;

namespace MuMech
{
    class MechJebModuleRendezvousAutopilotWindow : DisplayModule
    {
        public MechJebModuleRendezvousAutopilotWindow(MechJebCore core) : base(core) { }

        protected override void WindowGUI(int windowID)
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label("Select a target to rendezvous with.");
                base.WindowGUI(windowID);
                return;
            }

            if (core.target.Orbit.referenceBody != orbit.referenceBody)
            {
                GUILayout.Label("Rendezvous target must be in the same sphere of influence.");
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            MechJebModuleRendezvousAutopilot autopilot = core.GetComputerModule<MechJebModuleRendezvousAutopilot>();
            if (autopilot != null)
            {
                GuiUtils.SimpleLabel("Rendezvous target", core.target.Name);
                
                if (!autopilot.enabled)
                {
                    if (GUILayout.Button("Engage autopilot")) autopilot.users.Add(this);
                }
                else
                {
                    if (GUILayout.Button("Disengage autopilot")) autopilot.users.Remove(this);
                }

                GuiUtils.SimpleTextBox("Desired final distance:", autopilot.desiredDistance, "m");

                if (autopilot.enabled) GUILayout.Label("Status: " + autopilot.status);
            }

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(50) };
        }

        public override string GetName()
        {
            return "Rendezvous Autopilot";
        }
    }
}
