﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionControlFrom : MechJebModuleScriptAction
	{
		public static string NAME = "ControlFrom";
		private readonly List<Part> controlPartsList = new List<Part>();
		private readonly List<string> controlPartsNames = new List<string>();
		[Persistent(pass = (int)Pass.Type)]
		private int selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		private bool partHighlighted = false;

		public MechJebModuleScriptActionControlFrom (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			this.controlPartsList.Clear();
			this.controlPartsNames.Clear();
			foreach (var vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (var part in vessel.Parts)
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

		public override void activateAction()
		{
			base.activateAction();
            if (selectedPartIndex < controlPartsList.Count && controlPartsList[selectedPartIndex].HasModule<ModuleCommand>())
            {
                controlPartsList[selectedPartIndex].GetModule<ModuleCommand>().MakeReference(); //set "control from here"
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

		public override void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0 && selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				var i = 0;
				foreach (var part in controlPartsList)
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

