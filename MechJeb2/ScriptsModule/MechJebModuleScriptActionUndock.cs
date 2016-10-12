using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionUndock : MechJebModuleScriptAction
	{
		public static String NAME = "Undock";

		private List<Part> dockingPartsList = new List<Part>();
		private List<String> dockingPartsNames = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedPartIndex = 0;
		private bool partHighlighted = false;

		public MechJebModuleScriptActionUndock(MechJebModuleScript scriptModule, MechJebCore core) : base(scriptModule, core, NAME)
		{
			//dockingModulesList = currentTargetVessel.FindPartModulesImplementing<ModuleDockingNode>();
			//List<ITargetable> ITargetableList = currentTargetVessel.FindPartModulesImplementing<ITargetable>();
			foreach (ModuleDockingNode node in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleDockingNode>())
			{
				dockingPartsList.Add(node.part);
				dockingPartsNames.Add (node.part.partInfo.title);
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
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
			if (dockingPartsList[selectedPartIndex] != null)
			{
				dockingPartsList[selectedPartIndex].enabled = true;
				dockingPartsList[selectedPartIndex].decouple();
			}
			this.endAction ();
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Decouple");

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
			}
			else
			{
				GUILayout.Label("-- NO DOCK PART --");
			}
			base.postWindowGUI(windowID);
		}
	}
}