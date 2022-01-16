using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	// load another script and start it automatically
	public class MechJebModuleScriptActionLoadScript : MechJebModuleScriptAction
	{
		public static String NAME = "LoadScript";
		[Persistent(pass = (int)(Pass.Local))]
		private int selectedMemorySlotType = 0;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt scriptSlot = 0;
		[Persistent(pass = (int)Pass.Type)]
		private bool autoStart = true;
		private readonly List<String> scriptsList = new List<String>();
		private readonly List<String> memorySlotsList = new List<String>();

		public MechJebModuleScriptActionLoadScript (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			for (int i = 1; i <= 4; i++)
			{
				scriptsList.Add("Slot " + i);
			}
			this.memorySlotsList.Clear();
			this.memorySlotsList.Add("Global Memory");
			this.memorySlotsList.Add("Vessel Memory");
		}

		public override void activateAction()
		{
			base.activateAction();
			this.scriptModule.stop();
			this.scriptModule.setSelectedMemorySlotType(selectedMemorySlotType);
			this.scriptModule.LoadConfig(scriptSlot, false, false);
			if (this.autoStart)
			{
				this.scriptModule.start();
			}
			//Don't call "End Action" as we already clear the task list and don't want to start the next one.
		}

		public override  void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Load");
			selectedMemorySlotType = GuiUtils.ComboBox.Box(selectedMemorySlotType, memorySlotsList.ToArray(), memorySlotsList);
			scriptSlot = GuiUtils.ComboBox.Box(scriptSlot, scriptsList.ToArray(), this);
			autoStart = GUILayout.Toggle(autoStart, "Start");
			base.postWindowGUI(windowID);
		}
	}
}

