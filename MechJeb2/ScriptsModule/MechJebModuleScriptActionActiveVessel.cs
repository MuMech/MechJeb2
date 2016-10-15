using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionActiveVessel : MechJebModuleScriptAction
	{
		public static String NAME = "ActiveVessel";
		private List<Part> crewableParts;
		private List<String> crewablePartsNames;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndex = 0;
		private int old_selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		bool partHighlighted = false;
		private int spendTime = 0;
		private int initTime = 5; //Add a 5s timer after the action
		private float startTime = 0f;

		public MechJebModuleScriptActionActiveVessel (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			crewableParts = new List<Part>();
			crewablePartsNames = new List<String>();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (Part part in vessel.Parts)
					{
						if (part.CrewCapacity > 0)
						{
							crewableParts.Add(part);
							crewablePartsNames.Add(part.partInfo.title);
						}
					}
				}
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			FlightGlobals.SetActiveVessel(crewableParts[selectedPartIndex].vessel);
			this.scriptModule.setActiveBreakpoint(actionIndex, crewableParts[selectedPartIndex].vessel);
			//crewableParts[selectedPartIndex].vessel.MakeActive();
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void afterOnFixedUpdate()
		{
			if (selectedPartIndex < crewableParts.Count && selectedPartIndex != old_selectedPartIndex)
			{
				this.selectedPartFlightID = crewableParts[selectedPartIndex].flightID;
				this.old_selectedPartIndex = this.selectedPartIndex;
			}

			if (!this.isExecuted() && this.isStarted() && startTime == 0f)
			{
				startTime = Time.time;
			}
			if (!this.isExecuted() && this.isStarted() && startTime > 0)
			{
				spendTime = initTime - (int)(Math.Round(Time.time - startTime));
				if (spendTime <= 0)
				{
					this.endAction();
				}
			}
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Activate Vessel");
			selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, crewablePartsNames.ToArray(), crewablePartsNames);
			if (!partHighlighted)
			{
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
				{
					partHighlighted = true;
					crewableParts[selectedPartIndex].SetHighlight(true, false);
				}
			}
			else
			{
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true)))
				{
					partHighlighted = false;
					crewableParts[selectedPartIndex].SetHighlight(false, false);
				}
			}
			if (this.isStarted() && !this.isExecuted())
			{
				GUILayout.Label(" waiting " + this.spendTime + "s");
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
			this.old_selectedPartIndex = selectedPartIndex;
		}
	}
}

