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
				this.dockingPartsNames.Add (node.part.partInfo.title);
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			dockingPartsList.Clear();
			dockingPartsNames.Clear();
			foreach (ModuleDockingNode node in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleDockingNode>())
			{
				dockingPartsList.Add(node.part);
				this.dockingPartsNames.Add(node.part.partInfo.title);
			}
			if (selectedPartIndex >= dockingPartsList.Count)
			{
				selectedPartIndex = dockingPartsList.Count - 1;
			}
			if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
			{
				if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().isEnabled)
				{
					dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().Decouple();
				}
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

