using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    // load another script and start it automatically
    public class MechJebModuleScriptActionLoadScript : MechJebModuleScriptAction
    {
        public static string NAME = "LoadScript";

        [Persistent(pass = (int)Pass.Local)]
        private int selectedMemorySlotType;

        [Persistent(pass = (int)Pass.Type)]
        private EditableInt scriptSlot = 0;

        [Persistent(pass = (int)Pass.Type)]
        private bool autoStart = true;

        private readonly List<string> scriptsList     = new List<string>();
        private readonly List<string> memorySlotsList = new List<string>();

        public MechJebModuleScriptActionLoadScript(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) :
            base(scriptModule, core, actionsList, NAME)
        {
            for (int i = 1; i <= 4; i++)
            {
                scriptsList.Add("Slot " + i);
            }

            memorySlotsList.Clear();
            memorySlotsList.Add("Global Memory");
            memorySlotsList.Add("Vessel Memory");
        }

        public override void activateAction()
        {
            base.activateAction();
            scriptModule.stop();
            scriptModule.setSelectedMemorySlotType(selectedMemorySlotType);
            scriptModule.LoadConfig(scriptSlot, false, false);
            if (autoStart)
            {
                scriptModule.start();
            }
            //Don't call "End Action" as we already clear the task list and don't want to start the next one.
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("Load");
            selectedMemorySlotType = GuiUtils.ComboBox.Box(selectedMemorySlotType, memorySlotsList.ToArray(), memorySlotsList);
            scriptSlot             = GuiUtils.ComboBox.Box(scriptSlot, scriptsList.ToArray(), this);
            autoStart              = GUILayout.Toggle(autoStart, "Start");
            postWindowGUI(windowID);
        }
    }
}
