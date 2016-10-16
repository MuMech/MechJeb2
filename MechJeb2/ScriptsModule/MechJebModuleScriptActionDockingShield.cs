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
		private int old_selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		private bool partHighlighted = false;
		private List<String> actionTypes = new List<String>();

		public MechJebModuleScriptActionDockingShield (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			this.actionTypes.Clear();
			this.actionTypes.Add("Open");
			this.actionTypes.Add("Close");
			this.dockingPartsList.Clear();
			this.dockingPartsNames.Clear();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (ModuleDockingNode node in vessel.FindPartModulesImplementing<ModuleDockingNode>())
					{
						if (node.deployAnimator != null)
						{
							if (node.deployAnimator.actionAvailable)
							{
								dockingPartsList.Add(node.part);
								dockingPartsNames.Add(node.part.partInfo.title);
							}
						}
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
			if (selectedPartIndex < dockingPartsList.Count && selectedPartIndex != old_selectedPartIndex)
			{
				this.selectedPartFlightID = dockingPartsList[selectedPartIndex].flightID;
				this.old_selectedPartIndex = this.selectedPartIndex;
			}

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

		override public void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in dockingPartsList)
				{
					if (part.flightID == selectedPartFlightID)
					{
						this.selectedPartIndex = i;
					}
					i++;
				}
			}
			this.old_selectedPartIndex = selectedPartIndex;
		}
	}
}

