using UnityEngine;

namespace MuMech
{
    public class MechJebModuleRendezvousAutopilotWindow : DisplayModule
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

            MechJebModuleRendezvousAutopilot autopilot = core.GetComputerModule<MechJebModuleRendezvousAutopilot>();

            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody)
            {
                GUILayout.Label("Rendezvous target must be in the same sphere of influence.");
                if (autopilot.enabled)
                    autopilot.users.Remove(this);
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();
            
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
                GuiUtils.SimpleTextBox("Max # of phasing orbits:", autopilot.maxPhasingOrbits);

                if (autopilot.maxPhasingOrbits < 5)
                {
                    GUIStyle s = new GUIStyle(GUI.skin.label);
                    s.normal.textColor = Color.yellow;
                    GUILayout.Label("Max # of phasing orbits must be at least 5.", s);
                }

                if (autopilot.enabled) GUILayout.Label("Status: " + autopilot.status);
            }
            
            core.node.autowarp = GUILayout.Toggle(core.node.autowarp, "Auto-warp");

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

        public override bool IsSpaceCenterUpgradeUnlocked()
        {
            return vessel.patchedConicsUnlocked();
        }
    }
}
