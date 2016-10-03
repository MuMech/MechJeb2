using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionDockingShield : MechJebModuleScriptAction
	{
		public static String NAME = "DockingShield";
		private List<Part> dockingPartsList = new List<Part>();
		private List<String> dockingPartsNames = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		private bool partHighlighted = false;
		private List<String> actionTypes = new List<String>();

		public MechJebModuleScriptActionDockingShield (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			actionTypes.Add("Open");
			actionTypes.Add("Close");
			foreach (ModuleDockingNode node in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleDockingNode>())
			{
				if (node.deployAnimator!=null)
				{
					if (node.deployAnimator.actionAvailable)
					{
						dockingPartsList.Add(node.part);
						dockingPartsNames.Add(node.part.partInfo.title);
					}
				}
			}
		}

		override public void activateAction(int actionIndex)
		{
			if (dockingPartsList[selectedPartIndex] != null)
			{
				if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
				{
					if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.actionAvailable)
					{
						if (actionType == 0)
						{
							if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Progress == 0)
							{
								dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Toggle();
							}
						}
						else
						{
							if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Progress == 1)
							{
								dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Toggle();
							}
						}
					}
				}
			}
			base.activateAction(actionIndex);
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void afterOnFixedUpdate()
		{
			if (this.started && !this.executed)
			{
				if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
				{
					if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.status.CompareTo("Locked") == 0)
					{
						this.endAction();
					}
				}
			}
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label("Toggle Shield State");

			if (dockingPartsList.Count > 0)
			{
				selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, dockingPartsNames.ToArray(), dockingPartsNames);
				if (!partHighlighted)
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
					{
						partHighlighted = true;
						dockingPartsList[selectedPartIndex].SetHighlight(true, true);
					}
				}
				else
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true)))
					{
						partHighlighted = false;
						dockingPartsList[selectedPartIndex].SetHighlight(false, true);
					}
				}
				actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
			}
			else
			{
				GUILayout.Label("-- NO SHIELDED DOCK PART --");
			}
			base.postWindowGUI(windowID);
		}
	}
}

