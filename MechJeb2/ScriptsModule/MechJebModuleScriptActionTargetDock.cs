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
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint controlFromPartFlightID = 0;
		private bool partHighlighted = false;
		private bool partHighlightedControl = false;

		public MechJebModuleScriptActionTargetDock (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			this.dockingPartsList.Clear();
			this.dockingPartsNames.Clear();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (ModuleDockingNode node in vessel.FindPartModulesImplementing<ModuleDockingNode>())
					{
						this.dockingPartsList.Add(node.part);
						this.dockingPartsNames.Add(node.part.partInfo.title);
						this.controlPartsNames.Add(node.part.partInfo.title);
					}
				}
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null && dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>() != null)
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
				//Check if the "Control" dock is a shielded dock: Open the shield
				if (dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().deployAnimator != null)
				{
					if (dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().deployAnimator.actionAvailable)
					{
						if (dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Progress == 0)
						{
							dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Toggle();
						}
					}
				}
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
					if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
					{
						partHighlighted = true;
						dockingPartsList [selectedPartIndex].SetHighlight (true, true);
					}
				}
				else
				{
					if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
					{
						partHighlighted = false;
						dockingPartsList [selectedPartIndex].SetHighlight (false,true);
					}
				}
				GUILayout.Label ("C");
				controlFromPartIndex = GuiUtils.ComboBox.Box (controlFromPartIndex, controlPartsNames.ToArray (), controlPartsNames);
				if (!partHighlightedControl)
				{
					if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
					{
						partHighlightedControl = true;
						dockingPartsList [controlFromPartIndex].SetHighlight (true, true);
					}
				}
				else
				{
					if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
					{
						partHighlightedControl = false;
						dockingPartsList [controlFromPartIndex].SetHighlight (false,true);
					}
				}

				if (selectedPartIndex < dockingPartsList.Count)
				{
					this.selectedPartFlightID = dockingPartsList[selectedPartIndex].flightID;
				}
				if (controlFromPartIndex < dockingPartsList.Count)
				{
					this.controlFromPartFlightID = dockingPartsList[controlFromPartIndex].flightID;
				}
			}
			else
			{
				GUILayout.Label ("-- NO DOCK PART --");
			}
			base.postWindowGUI(windowID);
		}

		override public void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0 && controlFromPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in dockingPartsList)
				{
					if (part.flightID == selectedPartFlightID)
					{
						this.selectedPartIndex = i;
					}
					if (part.flightID == controlFromPartFlightID)
					{
						this.controlFromPartIndex = i;
					}
					i++;
				}
			}
		}
	}
}

