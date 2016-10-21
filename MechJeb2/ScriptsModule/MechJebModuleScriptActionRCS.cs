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
		[Persistent(pass = (int)Pass.Type)]
		private int actionObject;
		private List<String> actionTypes = new List<String>();
		private List<String> actionObjects = new List<String>();

		public MechJebModuleScriptActionRCS (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			actionTypes.Add("Enable");
			actionTypes.Add("Disable");
			actionObjects.Add("RCS");
			actionObjects.Add("RCS throttle when engines are offline");
			actionObjects.Add("Use RCS for rotation");
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (actionObject == 0)
			{
				if (actionType == 0)
				{
					core.rcs.enabled = true;
				}
				else
				{
					core.rcs.enabled = false;
				}
			}
			else if (actionObject == 1)
			{
				if (actionType == 0)
				{
					core.rcs.rcsThrottle = true;
				}
				else
				{
					core.rcs.rcsThrottle = false;
				}
			}
			else if (actionObject == 2)
			{
				if (actionType == 0)
				{
					core.rcs.rcsForRotation = true;
				}
				else
				{
					core.rcs.rcsForRotation = false;
				}
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
			actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
			actionObject = GuiUtils.ComboBox.Box(actionObject, actionObjects.ToArray(), actionObjects);
			base.postWindowGUI(windowID);
		}
	}
}

