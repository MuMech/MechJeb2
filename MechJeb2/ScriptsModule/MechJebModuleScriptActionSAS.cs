using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionSAS : MechJebModuleScriptAction
	{
		public static String NAME = "SAS";

		private List<Part> crewableParts;
		private List<String> crewablePartsNames;
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		[Persistent(pass = (int)Pass.Type)]
		private bool onActiveVessel = true;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndex = 0;
		bool partHighlighted = false;
		private List<String> actionTypes = new List<String>();

		public MechJebModuleScriptActionSAS (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			actionTypes.Add("Enable");
			actionTypes.Add("Disable");
			/*crewableParts = new List<Part>();
			crewablePartsNames = new List<String>();
			Vessel vessel = FlightGlobals.ActiveVessel;
			if (vessel != null)
			{
				foreach (Part part in vessel.Parts)
				{
					if (part.CrewCapacity > 0)
					{
						crewableParts.Add(part);
						crewablePartsNames.Add(part.partInfo.title);
					}
				}
			}*/
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				foreach (Part part in vessel.Parts)
				{
					crewableParts = new List<Part>();
					crewablePartsNames = new List<String>();
					if (part.CrewCapacity > 0)
					{
						crewableParts.Add(part);
						if (part.tag == null || part.tag.Length == 0)
						{
							part.tag = scriptModule.vesselSaveName + " " + part.name;
						}
						crewablePartsNames.Add(part.tag);
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
				if (!partHighlighted)
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
					{
						partHighlighted = true;
						crewableParts[selectedPartIndex].SetHighlight(true, false);
					}
				}
				else {
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true)))
					{
						partHighlighted = false;
						crewableParts[selectedPartIndex].SetHighlight(false, false);
					}
				}
			}
			base.postWindowGUI(windowID);
		}
	}
}

