using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionTimer : MechJebModuleScriptAction
	{
		public static String NAME = "Timer";

		[Persistent(pass = (int)Pass.Type)]
		private EditableInt time = 10;
		private int initTime = 10;
		private float startTime;

		public MechJebModuleScriptActionTimer (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			startTime = Time.time;
			initTime = time;
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			if (!this.isStarted())
			{
				GuiUtils.SimpleTextBox ("Wait Time", time, "s", 30);
			}
			if (!this.isExecuted() && this.isStarted())
			{
				time = initTime - (int)(Math.Round (Time.time - startTime));
				if (time.val <= 0)
				{
					this.endAction ();
				}
				GUILayout.Label ("T:-" + time.val+"s");
			}
			base.postWindowGUI(windowID);
		}
	}
}