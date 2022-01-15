using System;
using UnityEngine;

namespace MuMech
{
	// This template is itended to be used as a model for the new actions.
	// 1. Copy/Paste this class and rename it to your new action's name. Delete this comment in the header :-)
	// 2. Modify the NAME field to make it unique
	// 3. Implement your logic
	// 4. Add the action in the list in MechJebModuleScript (function "OnStart")
	// 5. Add the creation of your module in "GUILayout.Button("Add")" in MechJebModuleScript
	// 6. Add the load settings action in "LoadConfig" in MechJebModuleScript
	public class MechJebModuleScriptActionTemplate : MechJebModuleScriptAction
	{
		public static string NAME = "Template";

		public MechJebModuleScriptActionTemplate (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
		}

		public override void activateAction()
		{
			base.activateAction();
		}

		public override  void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Action Module Template");
			base.postWindowGUI(windowID);
		}
	}
}

