using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionTimer : MechJebModuleScriptAction
	{
		public static String NAME = "Timer";

		[Persistent(pass = (int)Pass.Type)]
		private EditableInt time = 10;
		private int spendTime = 0;
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

		override public void afterOnFixedUpdate()
		{
			if (!this.isExecuted() && this.isStarted())
			{
				spendTime = initTime - (int)(Math.Round(Time.time - startTime));
				if (spendTime <= 0)
				{
					this.endAction();
				}
			}
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
				GUILayout.Label ("T:-" + spendTime + "s");
			}
			base.postWindowGUI(windowID);
		}
	}
}