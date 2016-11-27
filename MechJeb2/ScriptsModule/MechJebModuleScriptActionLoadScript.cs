using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	// load another script and start it automatically
	public class MechJebModuleScriptActionLoadScript : MechJebModuleScriptAction
	{
		public static String NAME = "LoadScript";
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt scriptSlot = 0;
		[Persistent(pass = (int)Pass.Type)]
		private bool autoStart = true;
		private List<String> scriptsList = new List<String>();

		public MechJebModuleScriptActionLoadScript (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			for (int i = 1; i <= 4; i++)
			{
				scriptsList.Add("Slot " + i);
			}
		}

		override public void activateAction()
		{
			base.activateAction();
			this.scriptModule.stop();
			this.scriptModule.LoadConfig(scriptSlot, false);
			if (this.autoStart)
			{
				this.scriptModule.start();
			}
			//Don't call "End Action" as we already clear the task list and don't want to start the next one.
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Load");
			scriptSlot = GuiUtils.ComboBox.Box(scriptSlot, scriptsList.ToArray(), this);
			autoStart = GUILayout.Toggle(autoStart, "Start");
			base.postWindowGUI(windowID);
		}
	}
}

