using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;

namespace MuMech
{
	public class MechJebModuleScriptActionStaging : MechJebModuleScriptAction
	{
		public static string NAME = "Staging";

		[Persistent(pass = (int)Pass.Type)]
		private EditableInt stage = 0;
		[Persistent(pass = (int)Pass.Type)]
		private bool nextStage = true;
		private readonly List<string> stagesList = new List<string>();

		public MechJebModuleScriptActionStaging (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			for (var i = 0; i<StageManager.StageCount; i++)
			{
				stagesList.Add (i+"");
			}
		}

		public override void activateAction()
		{
			base.activateAction();
			if (nextStage)
			{
				StageManager.ActivateNextStage();
			}
			else if (stage < StageManager.StageCount)
			{
				StageManager.ActivateStage (stage);
			}
			this.endAction ();
		}

		public override  void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Staging ");
			nextStage = GUILayout.Toggle(nextStage, "Next stage");
			if (!nextStage)
			{
				stage = GuiUtils.ComboBox.Box (stage, stagesList.ToArray(), this);
			}
			base.postWindowGUI(windowID);
		}
	}
}

