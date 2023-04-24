using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionPause : MechJebModuleScriptAction
    {
        public static string NAME = "Pause";

        public MechJebModuleScriptActionPause(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) : base(
            scriptModule, core, actionsList, NAME)
        {
        }

        public override void activateAction()
        {
            base.activateAction();
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("Pause");
            if (isStarted())
            {
                if (GUILayout.Button("GO"))
                {
                    endAction();
                }
            }

            postWindowGUI(windowID);
        }
    }
}
