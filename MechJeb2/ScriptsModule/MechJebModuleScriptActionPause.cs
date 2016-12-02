using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionPause : MechJebModuleScriptAction
	{
		public static String NAME = "Pause";

		public MechJebModuleScriptActionPause (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
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
			GUILayout.Label ("Pause");
			if (this.isStarted())
			{
				if (GUILayout.Button ("GO"))
				{
					this.endAction ();
				}
			}
			base.postWindowGUI(windowID);
		}
	}
}

