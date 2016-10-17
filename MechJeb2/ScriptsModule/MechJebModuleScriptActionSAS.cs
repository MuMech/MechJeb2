﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionSAS : MechJebModuleScriptAction
	{
		public static String NAME = "SAS";

		private List<Part> crewableParts = new List<Part>();
		private List<String> crewablePartsNames = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		[Persistent(pass = (int)Pass.Type)]
		private bool onActiveVessel = true;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		bool partHighlighted = false;
		private List<String> actionTypes = new List<String>();

		public MechJebModuleScriptActionSAS (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			this.actionTypes.Clear();
			this.actionTypes.Add("Enable");
			this.actionTypes.Add("Disable");
			this.crewableParts.Clear();
			this.crewablePartsNames.Clear();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (Part part in vessel.Parts)
					{
						if (part.CrewCapacity > 0)
						{
							crewableParts.Add(part);
							crewablePartsNames.Add(part.name);
						}
					}
				}
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			Vessel vessel;
			if (onActiveVessel)
			{
				vessel = FlightGlobals.ActiveVessel;
			}
			else
			{
				vessel = crewableParts[selectedPartIndex].vessel;
			}
			if (actionType == 0)
			{
				vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, true);
			}
			else
			{
				vessel.ActionGroups.SetGroup(KSPActionGroup.SAS, false);
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
			GUILayout.Label ("SAS");
			actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
			onActiveVessel = GUILayout.Toggle(onActiveVessel, "On active Vessel");
			if (!onActiveVessel)
			{
				selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, crewablePartsNames.ToArray(), crewablePartsNames);
				if (crewableParts[selectedPartIndex] != null)
				{
					if (!partHighlighted)
					{
						if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
						{
							partHighlighted = true;
							crewableParts[selectedPartIndex].SetHighlight(true, false);
						}
					}
					else
					{
						if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
						{
							partHighlighted = false;
							crewableParts[selectedPartIndex].SetHighlight(false, false);
						}
					}
				}
			}
			if (selectedPartIndex < crewableParts.Count)
			{
				this.selectedPartFlightID = crewableParts[selectedPartIndex].flightID;
			}
			base.postWindowGUI(windowID);
		}

		override public void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in crewableParts)
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

