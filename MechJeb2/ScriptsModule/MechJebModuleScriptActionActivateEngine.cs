using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionActivateEngine : MechJebModuleScriptAction
	{
		public static String NAME = "ActivateEngine";
		private List<Part> enginePartsList = new List<Part>();
		private List<String> enginePartsNames = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedPartIndex = 0;
		private int old_selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		private bool partHighlighted = false;

		public MechJebModuleScriptActionActivateEngine (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			this.enginePartsList.Clear();
			this.enginePartsNames.Clear();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (Part part in vessel.Parts)
					{
						if (part.IsEngine())
						{
							enginePartsList.Add(part);
							enginePartsNames.Add(part.partInfo.title);
						}
					}
				}
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (enginePartsList[selectedPartIndex] != null)
			{
				if (!enginePartsList[selectedPartIndex].isActiveAndEnabled)
				{
					//enginePartsList[selectedPartIndex].enabled = true;
					enginePartsList[selectedPartIndex].force_activate();
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
			GUILayout.Label("Activate engine");

			if (enginePartsList.Count > 0)
			{
				selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, enginePartsNames.ToArray(), enginePartsNames);
				if (!partHighlighted)
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
					{
						partHighlighted = true;
						enginePartsList[selectedPartIndex].SetHighlight(true, true);
					}
				}
				else
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true)))
					{
						partHighlighted = false;
						enginePartsList[selectedPartIndex].SetHighlight(false, true);
					}
				}
			}
			else
			{
				GUILayout.Label("-- NO DOCK PART --");
			}
			base.postWindowGUI(windowID);
		}

		override public void afterOnFixedUpdate()
		{
			if (selectedPartIndex < enginePartsList.Count && selectedPartIndex != old_selectedPartIndex)
			{
				this.selectedPartFlightID = enginePartsList[selectedPartIndex].flightID;
				this.old_selectedPartIndex = this.selectedPartIndex;
			}
		}

		override public void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in enginePartsList)
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

