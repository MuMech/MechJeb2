using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionControlFrom : MechJebModuleScriptAction
	{
		public static String NAME = "ControlFrom";
		private List<Part> controlPartsList = new List<Part>();
		private List<String> controlPartsNames = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		private bool partHighlighted = false;

		public MechJebModuleScriptActionControlFrom (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			this.controlPartsList.Clear();
			this.controlPartsNames.Clear();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (Part part in vessel.Parts)
					{
						if (part.HasModule<ModuleCommand>() && !part.name.Contains("mumech"))
						{
							controlPartsList.Add(part);
							controlPartsNames.Add(part.partInfo.title);
						}
					}
				}
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (selectedPartIndex < controlPartsList.Count)
			{
				if (controlPartsList[selectedPartIndex].HasModule<ModuleCommand>())
				{
					controlPartsList[selectedPartIndex].GetModule<ModuleCommand>().MakeReference(); //set "control from here"
				}
			}
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
			GUILayout.Label("Control from");
			if (controlPartsList.Count > 0)
			{
				selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, controlPartsNames.ToArray(), controlPartsNames);
				if (!partHighlighted)
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
					{
						partHighlighted = true;
						controlPartsList[selectedPartIndex].SetHighlight(true, true);
					}
				}
				else
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
					{
						partHighlighted = false;
						controlPartsList[selectedPartIndex].SetHighlight(false, true);
					}
				}

				if (selectedPartIndex < controlPartsList.Count)
				{
					this.selectedPartFlightID = controlPartsList[selectedPartIndex].flightID;
				}
			}
			else
			{
				GUILayout.Label("-- NO PART TO CONTROL --");
			}
			base.postWindowGUI(windowID);
		}

		override public void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0 && selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in controlPartsList)
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

