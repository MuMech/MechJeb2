using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionActiveVessel : MechJebModuleScriptAction
	{
		public static String NAME = "ActiveVessel";
		private readonly List<Part> commandParts = new List<Part>();
		private readonly List<String> commandPartsNames = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		bool partHighlighted = false;
		private int spendTime = 0;
		private readonly int initTime = 5; //Add a 5s timer after the action
		private float startTime = 0f;

		public MechJebModuleScriptActionActiveVessel (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			this.commandParts.Clear();
			this.commandPartsNames.Clear();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (Part part in vessel.Parts)
					{
						if (part.HasModule<ModuleCommand>() && !part.name.Contains("mumech"))
						{
							commandParts.Add(part);
							commandPartsNames.Add(part.partInfo.title);
						}
					}
				}
			}
		}

		public override void activateAction()
		{
			base.activateAction();
			FlightGlobals.SetActiveVessel(commandParts[selectedPartIndex].vessel);
			this.scriptModule.setActiveBreakpoint(actionIndex, commandParts[selectedPartIndex].vessel);
			//crewableParts[selectedPartIndex].vessel.MakeActive();
		}

		public override  void endAction()
		{
			base.endAction();
		}

		public override void afterOnFixedUpdate()
		{
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

		public override void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("Activate Vessel");
			selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, commandPartsNames.ToArray(), commandPartsNames);
			if (!partHighlighted)
			{
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
				{
					partHighlighted = true;
					commandParts[selectedPartIndex].SetHighlight(true, false);
				}
			}
			else
			{
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
				{
					partHighlighted = false;
					commandParts[selectedPartIndex].SetHighlight(false, false);
				}
			}
			if (this.isStarted() && !this.isExecuted())
			{
				GUILayout.Label(" waiting " + this.spendTime + "s");
			}

			if (selectedPartIndex < commandParts.Count)
			{
				this.selectedPartFlightID = commandParts[selectedPartIndex].flightID;
			}

			base.postWindowGUI(windowID);
		}

		public override void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in commandParts)
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

