using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleAscentPEGMenu : MechJebModuleAscentMenuBase
    {
        public MechJebModuleAscentPEGMenu(MechJebCore core)
            : base(core)
        {
            hidden = true;
        }

        public MechJebModuleAscentPEG path { get { return autopilot.ascentPath as MechJebModuleAscentPEG; } }
        private MechJebModulePEGController peg { get { return core.GetComputerModule<MechJebModulePEGController>(); } }
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

            GUILayout.Label("Stage Stats");
            if (GUILayout.Button("Reset PEG"))
                peg.Reset();

            /*
            for(int i = peg.stages.Count - 1; i >= 0; i--) {
                GUILayout.Label(String.Format("{0:D}: {1:D} {2:F1} {3:F1}", i, peg.stages[i].kspStage, peg.stages[i].dt, peg.stages[i].Li));
            }
            */

//            GuiUtils.SimpleTextBox("Emergency pitch adj.:", path.pitchBias, "°");


            if (autopilot.enabled)
            {
                GUILayout.Label("Autopilot status: " + autopilot.status);
            }

            if (peg.enabled)
            {
                if (GUILayout.Button("Force Disable PEG"))
                    peg.enabled = false;
            }
            else
            {
                if (GUILayout.Button("Force Enable PEG"))
                    peg.enabled = true;
            }


            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }

        public override string GetName()
        {
            return "Space Shuttle PEG Pitch Program";
        }
    }
}
