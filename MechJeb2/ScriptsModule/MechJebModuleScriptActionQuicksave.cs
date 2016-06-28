using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionQuicksave : MechJebModuleScriptAction
	{
		public static String NAME = "Quicksave";

		public MechJebModuleScriptActionQuicksave (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR)
			{
				QuickSaveLoad.QuickSave();
			}
			this.endAction();
		}
		override public  void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Quicksave");
			base.postWindowGUI(windowID);
		}
	}
}

