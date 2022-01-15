using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionRCS : MechJebModuleScriptAction
	{
		public static string NAME = "RCS";

		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		[Persistent(pass = (int)Pass.Type)]
		private int actionObject;
		private readonly List<string> actionTypes = new List<string>();
		private readonly List<string> actionObjects = new List<string>();
		private string errorMessage = "";

		public MechJebModuleScriptActionRCS (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actionTypes.Add("Enable");
			actionTypes.Add("Disable");
			actionObjects.Add("RCS");
			actionObjects.Add("RCS throttle when engines are offline");
			actionObjects.Add("Use RCS for rotation");
			actionObjects.Add("Zero Rvel");
		}

		public override void activateAction()
		{
			base.activateAction();
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
			else if (actionObject == 3)
			{
				if (actionType == 0)
				{
					if (core.target.Target != null)
					{
						core.rcs.users.Add(this);
						core.rcs.SetTargetRelative(Vector3d.zero);
					}
					else
					{
						this.errorMessage = "ERROR: Target is null";
					}
				}
				else
				{
					core.rcs.users.Remove(this);
				}
			}
			this.endAction();
		}

		public override void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID) {
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			if (errorMessage.Length == 0)
			{
				actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
				actionObject = GuiUtils.ComboBox.Box(actionObject, actionObjects.ToArray(), actionObjects);
			}
			else
			{
				var s = new GUIStyle(GUI.skin.label);
				s.normal.textColor = Color.yellow;
				GUILayout.Label(this.errorMessage, s);			
			}
			base.postWindowGUI(windowID);
		}
	}
}

