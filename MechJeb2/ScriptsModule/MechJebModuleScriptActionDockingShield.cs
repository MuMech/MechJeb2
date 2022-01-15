using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionDockingShield : MechJebModuleScriptAction
	{
		public static string NAME = "DockingShield";
		private readonly List<Part> dockingPartsList = new List<Part>();
		private readonly List<string> dockingPartsNames = new List<string>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		private bool partHighlighted = false;
		private readonly List<string> actionTypes = new List<string>();

		public MechJebModuleScriptActionDockingShield (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			this.actionTypes.Clear();
			this.actionTypes.Add("Open");
			this.actionTypes.Add("Close");
			this.dockingPartsList.Clear();
			this.dockingPartsNames.Clear();
			foreach (var vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (var node in vessel.FindPartModulesImplementing<ModuleDockingNode>())
					{
                        if (node.deployAnimator?.actionAvailable == true)
                        {
                            dockingPartsList.Add(node.part);
                            dockingPartsNames.Add(node.part.partInfo.title);
                        }
                    }
				}
			}
		}

		public override void activateAction()
		{
            if (dockingPartsList[selectedPartIndex]?.GetModule<ModuleDockingNode>() != null)
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
            base.activateAction();
		}

		public override  void endAction()
		{
			base.endAction();
		}

		public override void afterOnFixedUpdate()
		{
            if (this.started && !this.executed && dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
            {
                if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.status.CompareTo("Locked") == 0)
                {
                    this.endAction();
                }
            }
        }

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label("Toggle Shield State");

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
				actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);

				if (selectedPartIndex < dockingPartsList.Count)
				{
					this.selectedPartFlightID = dockingPartsList[selectedPartIndex].flightID;
				}
			}
			else
			{
				GUILayout.Label("-- NO SHIELDED DOCK PART --");
			}
			base.postWindowGUI(windowID);
		}

		public override void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				var i = 0;
				foreach (var part in dockingPartsList)
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

