using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleRendezvousAutopilotWindow : DisplayModule
    {
        public MechJebModuleRendezvousAutopilotWindow(MechJebCore core) : base(core) { }

        protected override void WindowGUI(int windowID)
        {
            if (!Core.Target.NormalTargetExists)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_RZauto_label1")); //"Select a target to rendezvous with."
                base.WindowGUI(windowID);
                return;
            }

            MechJebModuleRendezvousAutopilot autopilot = Core.GetComputerModule<MechJebModuleRendezvousAutopilot>();

            if (Core.Target.TargetOrbit.referenceBody != Orbit.referenceBody)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_RZauto_label2")); //"Rendezvous target must be in the same sphere of influence."
                if (autopilot.Enabled)
                    autopilot.Users.Remove(this);
                base.WindowGUI(windowID);
                return;
            }

            GUILayout.BeginVertical();

            if (autopilot != null)
            {
                GuiUtils.SimpleLabel(Localizer.Format("#MechJeb_RZauto_label3"), Core.Target.Name); //"Rendezvous target"

                if (!autopilot.Enabled)
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_RZauto_button1"))) autopilot.Users.Add(this); //"Engage autopilot"
                }
                else
                {
                    if (GUILayout.Button(Localizer.Format("#MechJeb_RZauto_button2"))) autopilot.Users.Remove(this); //"Disengage autopilot"
                }

                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RZauto_label4"), autopilot.desiredDistance, "m");   //"Desired final distance:"
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RZauto_label5"), autopilot.maxPhasingOrbits);       //"Max # of phasing orbits:"
                GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_RZauto_label8"), autopilot.maxClosingSpeed, "m/s"); //"Max closing velocity:"

                if (autopilot.maxPhasingOrbits < 5)
                {
                    GUILayout.Label(Localizer.Format("#MechJeb_RZauto_label6"), GuiUtils.yellowLabel); //"Max # of phasing orbits must be at least 5."
                }

                if (autopilot.Enabled) GUILayout.Label(Localizer.Format("#MechJeb_RZauto_label7", autopilot.status)); //"Status: <<1>>"
            }

            Core.Node.Autowarp = GUILayout.Toggle(Core.Node.Autowarp, Localizer.Format("#MechJeb_RZauto_checkbox1")); //"Auto-warp"

            GUILayout.EndVertical();

            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions() => new[] { GUILayout.Width(300), GUILayout.Height(50) };

        public override string GetName() => Localizer.Format("#MechJeb_RZauto_title"); //"Rendezvous Autopilot"

        public override string IconName() => "Rendezvous Autopilot";

        protected override bool IsSpaceCenterUpgradeUnlocked() => Vessel.patchedConicsUnlocked();
    }
}
