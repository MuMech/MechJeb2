using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionActionGroup : MechJebModuleScriptAction
	{
		public static String NAME = "ActionGroup";

		private List<String> actionGroups = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedActionId;

		public MechJebModuleScriptActionActionGroup (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			actionGroups.Add("Abord");
			actionGroups.Add("Brakes");
			actionGroups.Add("Custom01");
			actionGroups.Add("Custom02");
			actionGroups.Add("Custom03");
			actionGroups.Add("Custom04");
			actionGroups.Add("Custom05");
			actionGroups.Add("Custom06");
			actionGroups.Add("Custom07");
			actionGroups.Add("Custom08");
			actionGroups.Add("Custom09");
			actionGroups.Add("Custom10");
			actionGroups.Add("Gear");
			actionGroups.Add("Light");
			actionGroups.Add("None");
			actionGroups.Add("RCS");
			actionGroups.Add("SAS");
			actionGroups.Add("Stage");
		}

		override public void activateAction()
		{
			base.activateAction();
			KSPActionGroup selectedGroup = KSPActionGroup.Abort;
			if (selectedActionId == 0)
			{
				selectedGroup = KSPActionGroup.Abort;
			}
			else if (selectedActionId == 1)
			{
				selectedGroup = KSPActionGroup.Brakes;
			}
			else if (selectedActionId == 2)
			{
				selectedGroup = KSPActionGroup.Custom01;
			}
			else if (selectedActionId == 3)
			{
				selectedGroup = KSPActionGroup.Custom02;
			}
			else if (selectedActionId == 4)
			{
				selectedGroup = KSPActionGroup.Custom03;
			}
			else if (selectedActionId == 5)
			{
				selectedGroup = KSPActionGroup.Custom04;
			}
			else if (selectedActionId == 6)
			{
				selectedGroup = KSPActionGroup.Custom05;
			}
			else if (selectedActionId == 7)
			{
				selectedGroup = KSPActionGroup.Custom06;
			}
			else if (selectedActionId == 8)
			{
				selectedGroup = KSPActionGroup.Custom07;
			}
			else if (selectedActionId == 9)
			{
				selectedGroup = KSPActionGroup.Custom08;
			}
			else if (selectedActionId == 10)
			{
				selectedGroup = KSPActionGroup.Custom09;
			}
			else if (selectedActionId == 11)
			{
				selectedGroup = KSPActionGroup.Custom10;
			}
			else if (selectedActionId == 12)
			{
				selectedGroup = KSPActionGroup.Gear;
			}
			else if (selectedActionId == 13)
			{
				selectedGroup = KSPActionGroup.Light;
			}
			else if (selectedActionId == 14)
			{
				selectedGroup = KSPActionGroup.None;
			}
			else if (selectedActionId == 15)
			{
				selectedGroup = KSPActionGroup.RCS;
			}
			else if (selectedActionId == 16)
			{
				selectedGroup = KSPActionGroup.SAS;
			}
			else if (selectedActionId == 17)
			{
				selectedGroup = KSPActionGroup.Stage;
			}
			FlightGlobals.ActiveVessel.ActionGroups.ToggleGroup(selectedGroup);
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
			GUILayout.Label ("Toggle action group");
			selectedActionId = GuiUtils.ComboBox.Box(selectedActionId, actionGroups.ToArray(), actionGroups);
			base.postWindowGUI(windowID);
		}
	}
}

