using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionTargetDock : MechJebModuleScriptAction
	{
		public static String NAME = "TargetDock";

		private List<Part> dockingPartsList = new List<Part>();
		private List<String> dockingPartsNames = new List<String>();
		private List<String> controlPartsNames = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private int controlFromPartIndex = 0;
		private bool partHighlighted = false;
		private bool partHighlightedControl = false;

		public MechJebModuleScriptActionTargetDock (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			//dockingModulesList = currentTargetVessel.FindPartModulesImplementing<ModuleDockingNode>();
			//List<ITargetable> ITargetableList = currentTargetVessel.FindPartModulesImplementing<ITargetable>();
			foreach (ModuleDockingNode node in FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleDockingNode>())
			{
				dockingPartsList.Add(node.part);
				this.dockingPartsNames.Add (node.part.partInfo.title);
				this.controlPartsNames.Add (node.part.partInfo.title);
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null && dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
			{
				dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().SetAsTarget(); //Set target
				this.core.target.Set(dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>());
				dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().MakeReferenceTransform(); //set "control from here"
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
			GUILayout.Label ("Dock Target");
			if (dockingPartsNames.Count > 0)
			{
				selectedPartIndex = GuiUtils.ComboBox.Box (selectedPartIndex, dockingPartsNames.ToArray (), dockingPartsNames);
				if (!partHighlighted)
				{
					if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
					{
						partHighlighted = true;
						dockingPartsList [selectedPartIndex].SetHighlight (true, true);
					}
				}
				else
				{
					if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true)))
					{
						partHighlighted = false;
						dockingPartsList [selectedPartIndex].SetHighlight (false,true);
					}
				}
				GUILayout.Label ("C");
				controlFromPartIndex = GuiUtils.ComboBox.Box (controlFromPartIndex, controlPartsNames.ToArray (), controlPartsNames);
				if (!partHighlightedControl)
				{
					if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
					{
						partHighlightedControl = true;
						dockingPartsList [controlFromPartIndex].SetHighlight (true, true);
					}
				}
				else
				{
					if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true)))
					{
						partHighlightedControl = false;
						dockingPartsList [controlFromPartIndex].SetHighlight (false,true);
					}
				}
			}
			else
			{
				GUILayout.Label ("-- NO DOCK PART --");
			}
			base.postWindowGUI(windowID);
		}
	}
}

