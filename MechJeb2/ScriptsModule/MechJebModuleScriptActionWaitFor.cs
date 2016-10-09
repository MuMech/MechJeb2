using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionWaitFor : MechJebModuleScriptAction
	{
		public static String NAME = "WaitFor";

		[Persistent(pass = (int)Pass.Type)]
		private int actionType = 0;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt targetAltitude = new EditableInt(1000);
		private List<String> actionTypes = new List<String>();

		public MechJebModuleScriptActionWaitFor (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			actionTypes.Add("an altitude");
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Wait for");
			actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
			if (actionType == 0)
			{
				GUILayout.Label("of");
				targetAltitude.text = GUILayout.TextField(targetAltitude.text, GUILayout.Width(40));
				GUILayout.Label("m");
			}
			base.postWindowGUI(windowID);
		}

		override public void afterOnFixedUpdate()
		{
			if (this.isStarted() && !this.isExecuted())
			{
				if (actionType == 0)
				{
					double current_altitude = FlightGlobals.ActiveVessel.mainBody.GetAltitude(FlightGlobals.ActiveVessel.CoM);
					if (current_altitude >= targetAltitude)
					{
						this.endAction();
					}
				}
			}
		}
	}
}

