using System;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionTimer : MechJebModuleScriptAction
    {
        public static string NAME = "Timer";

        [Persistent(pass = (int)Pass.Type)]
        private readonly EditableInt time = 10;

        private int   spendTime;
        private int   initTime = 10;
        private float startTime;

        public MechJebModuleScriptActionTimer(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) : base(
            scriptModule, core, actionsList, NAME)
        {
        }

        public override void activateAction()
        {
            base.activateAction();
            startTime = Time.time;
            initTime  = time;
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void afterOnFixedUpdate()
        {
            if (!isExecuted() && isStarted())
            {
                spendTime = initTime - (int)Math.Round(Time.time - startTime);
                if (spendTime <= 0)
                {
                    endAction();
                }
            }
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            if (!isStarted())
            {
                GuiUtils.SimpleTextBox("Wait Time", time, "s", 30);
            }

            if (!isExecuted() && isStarted())
            {
                GUILayout.Label("Wait T:-" + spendTime + "s", GUILayout.ExpandWidth(false));
            }

            postWindowGUI(windowID);
        }
    }
}
