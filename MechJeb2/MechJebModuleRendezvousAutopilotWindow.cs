using UnityEngine;
using KSP.Localization;

namespace MuMech
{
    public class MechJebModuleRendezvousAutopilotWindow : DisplayModule
    {
        public MechJebModuleRendezvousAutopilotWindow(MechJebCore core) : base(core) { }

        protected override void WindowGUI(int windowID)
        {
            if (!core.target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_RZauto_label1"));//"Select a target to rendezvous with."
                base.WindowGUI(windowID);
                return;
            }

            MechJebModuleRendezvousAutopilot autopilot = core.GetComputerModule<MechJebModuleRendezvousAutopilot>();

            if (core.target.TargetOrbit.referenceBody != orbit.referenceBody)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_RZauto_label2"));//"Rendezvous target must be in the same sphere of influence."
                if (autopilot.enabled)
                    autopilot.users.Remove(this);
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();
            
            if (autopilot != null)
            {
                GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RZauto_label3"), core.target.Name);//"Rendezvous target"
                
                if (!autopilot.enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_RZauto_button1"))) autopilot.users.Add(this);//"Engage autopilot"
                }
                else
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_RZauto_button2"))) autopilot.users.Remove(this);//"Disengage autopilot"
                }

                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RZauto_label4"), autopilot.desiredDistance, "m");//"Desired final distance:"
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RZauto_label5"), autopilot.maxPhasingOrbits);//"Max # of phasing orbits:"
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RZauto_label8"), autopilot.maxClosingSpeed, "m/s");//"Max closing velocity:"

                if (autopilot.maxPhasingOrbits < 5)
                {
                    GUIStyle s = new GUIStyle(GUI.skin.label);
                    s.normal.textColor = Color.yellow;
                    GUILayout.Label(Localizer.Format("#MechJeb_RZauto_label6"), s);//"Max # of phasing orbits must be at least 5."
                }

                if (autopilot.enabled) GUILayout.Label( Localizer.Format("#MechJeb_RZauto_label7", autopilot.status));//"Status: <<1>>"
            }
            
            core.node.autowarp = GUILayout.Toggle(core.node.autowarp, Localizer.Format("#MechJeb_RZauto_checkbox1"));//"Auto-warp"

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new GUILayoutOption[] { GUILayout.Width(300), GUILayout.Height(50) };
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_RZauto_title");//"Rendezvous Autopilot"
        }

        public override string IconName()
        {
            return "Rendezvous Autopilot";
        }

        public override bool IsSpaceCenterUpgradeUnlocked()
        {
            return vessel.patchedConicsUnlocked();
        }
    }
}
