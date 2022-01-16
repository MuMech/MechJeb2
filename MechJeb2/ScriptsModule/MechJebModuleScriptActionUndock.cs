using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionUndock : MechJebModuleScriptAction
	{
		public static String NAME = "Undock";

		private readonly List<Part> dockingPartsList = new List<Part>();
		private readonly List<String> dockingPartsNames = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		private bool partHighlighted = false;

		public MechJebModuleScriptActionUndock(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) : base(scriptModule, core, actionsList, NAME)
		{
			//dockingModulesList = currentTargetVessel.FindPartModulesImplementing<ModuleDockingNode>();
			//List<ITargetable> ITargetableList = currentTargetVessel.FindPartModulesImplementing<ITargetable>();
			this.dockingPartsList.Clear();
			this.dockingPartsNames.Clear();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (ModuleDockingNode node in vessel.FindPartModulesImplementing<ModuleDockingNode>())
					{
						dockingPartsList.Add(node.part);
						dockingPartsNames.Add(node.part.partInfo.title);
					}
				}
			}
		}

		public override void activateAction()
		{
			base.activateAction();
			//Create again the parts list to manage the changes in the vessel structure
			/*dockingPartsList.Clear();
			dockingPartsNames.Clear();
			foreach (ModuleDockingNode node in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleDockingNode>())
			{
				if (!dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().isEnabled)
				{
					dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().enabled = true;
				}
				dockingPartsList.Add(node.part);
				dockingPartsNames.Add(node.part.partInfo.title);
			}
			if (dockingPartsList.Count > 0)
			{
				if (selectedPartIndex >= dockingPartsList.Count)
				{
					selectedPartIndex = dockingPartsList.Count - 1;
				}
				if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
				{
					dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().Decouple();
				}
			}*/
			//dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().enabled = true;
			/*if (!dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().Events["Undock"].active)
			{
				//Undock action of the selected module not available
				//Check if the attached port is a docking port
				if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().referenceNode.attachedPart.GetModule<ModuleDockingNode>() != null)
				{
					//...and if the attached port can undock
					if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().referenceNode.attachedPart.GetModule<ModuleDockingNode>().Events["Undock"].active)
					{
						dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().referenceNode.attachedPart.GetModule<ModuleDockingNode>().referenceNode.attachedPart = dockingPartsList[selectedPartIndex].parent;
						dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().referenceNode.attachedPart.GetModule<ModuleDockingNode>().Decouple();
					}
				}
			}
			else
			{
				dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().referenceNode.attachedPart = dockingPartsList[selectedPartIndex].parent;
				dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().Decouple();
			}*/
			/*if (dockingPartsList[selectedPartIndex] != null)
			{
				dockingPartsList[selectedPartIndex].enabled = true;
				dockingPartsList[selectedPartIndex].decouple();
			}*/
			if (dockingPartsList[selectedPartIndex] != null)
			{
				//Check if the target dock is a shielded dock: Open the shield
				if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator != null)
				{
					if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.actionAvailable)
					{
						if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Progress == 0)
						{
							dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Toggle();
						}
					}
				}
				//dockingPartsList[selectedPartIndex].enabled = true;
				if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
				{
					dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().Decouple();
				}
				/*if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
				{
					dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().Undock();
				}
				if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
				{
					dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().UndockSameVessel();
				}*/
				//When undocking, this can separate the vessel into 2 new vessels. We take the risk the new one is not the one with the focus. If it's the case, we must transfer the script.
				if (!core.vessel.isActiveVessel)
				{
					this.scriptModule.setActiveBreakpoint(actionIndex, FlightGlobals.ActiveVessel);
					return;//Don't end the action as we do not want to move to the next one
				}
			}
			this.endAction();
		}

		public override  void endAction()
		{
			base.endAction();
		}

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Decouple", GUILayout.ExpandWidth(false));

			if (dockingPartsList.Count > 0)
			{
				selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, dockingPartsNames.ToArray(), dockingPartsNames);
				if (!partHighlighted)
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
					{
						partHighlighted = true;
						dockingPartsList[selectedPartIndex].SetHighlight(true, true);
					}
				}
				else
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
					{
						partHighlighted = false;
						dockingPartsList[selectedPartIndex].SetHighlight(false, true);
					}
				}
				if (selectedPartIndex < dockingPartsList.Count)
				{
					this.selectedPartFlightID = dockingPartsList[selectedPartIndex].flightID;
				}
			}
			else
			{
				GUILayout.Label("-- NO DOCK PART --");
			}
			base.postWindowGUI(windowID);
		}

		public override void postLoad(ConfigNode node)
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
		}
	}
}