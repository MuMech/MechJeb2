using System;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionQuicksave : MechJebModuleScriptAction
	{
		public static string NAME = "Quicksave";
		private int spendTime = 0;
		private readonly int initTime = 5; //Add a 5s timer after the save action
		private float startTime = 0f;
		private bool saved = false;

		public MechJebModuleScriptActionQuicksave (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
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

		public override void afterOnFixedUpdate()
		{
			if (!this.isExecuted() && this.isStarted() && startTime == 0f)
			{
				startTime = Time.time;
			}
			if (!this.isExecuted() && this.isStarted() && startTime > 0)
			{	
				spendTime = initTime - (int)(Math.Round(Time.time - startTime));
				if (spendTime <= 0)
				{
					if (!this.saved)
					{
						//Wait 5s before save so FlightsGlobals are clear to save
						this.scriptModule.setActiveSavepoint(this.actionIndex);
						if (FlightGlobals.ClearToSave() == ClearToSaveStatus.CLEAR)
						{
							QuickSaveLoad.QuickSave();
						}
						this.saved = true;
						startTime = Time.time;
					}
					else {
						//Then wait 5s after save to let the time to save
						this.endAction();
					}
				}
			}
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Quicksave");
			if (this.isStarted() && !this.isExecuted())
			{
				GUILayout.Label(" waiting " + this.spendTime + "s");
			}
			base.postWindowGUI(windowID);
		}
	}
}