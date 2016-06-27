using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionRCS : MechJebModuleScriptAction
	{
		public static String NAME = "RCS";

		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		private List<String> actionTypes = new List<String>();

		public MechJebModuleScriptActionRCS (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			actionTypes.Add("Enable");
			actionTypes.Add("Disable");
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (actionType == 0)
			{
				core.rcs.enabled = true;
			}
			else
			{
				core.rcs.enabled = false;
			}
			this.endAction();
		}

		override public void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID) {
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("RCS");
			actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
			base.postWindowGUI(windowID);
		}
	}
}

