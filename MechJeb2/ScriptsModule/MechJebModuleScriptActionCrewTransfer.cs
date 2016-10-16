using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionCrewTransfer : MechJebModuleScriptAction
	{
		public static String NAME = "CrewTransfer";
		private List<Part> crewableParts = new List<Part>();
		private List<String> crewablePartsNamesS = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndexS = 0;
		private int old_selectedPartIndexS = 0;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndexT = 0;
		private int old_selectedPartIndexT = 0;
		private List<String> crewablePartsNamesT = new List<String>();
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedKerbal = 0;
		private int old_selectedKerbal = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartSFlightID = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartTFlightID = 0;
		[Persistent(pass = (int)Pass.Type)]
		private String selectedKerbalName;
		private List<ProtoCrewMember> kerbalsList = new List<ProtoCrewMember>();
		private List<String> kerbalsNames = new List<String>();
		private bool partHighlightedS = false;
		private bool partHighlightedT = false;

		public MechJebModuleScriptActionCrewTransfer (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			this.crewableParts.Clear();
			this.crewablePartsNamesS.Clear();
			this.crewablePartsNamesT.Clear();
			this.kerbalsNames.Clear();
			this.kerbalsList.Clear();
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (Part part in vessel.Parts)
					{
						if (part.CrewCapacity > 0)
						{
							crewableParts.Add(part);
							crewablePartsNamesS.Add(part.partInfo.title);
							crewablePartsNamesT.Add(part.partInfo.title);
						}
					}
					foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
					{
						kerbalsNames.Add(kerbal.name);
						kerbalsList.Add(kerbal);
					}
				}
			}
		}

		private void MoveKerbal(Part source, Part target, ProtoCrewMember kerbal)
		{
			RemoveCrew(kerbal, source, false);

			AddCrew(target, kerbal, false);

			// RemoveCrew works fine alone and AddCrew works fine alone, but if you combine them, it seems you must give KSP a moment to sort it all out,
			// so delay the remaining steps of the transfer process.
			//ManifestBehaviour.BeginDelayedCrewTransfer(source, target, kerbal);
			//CrewTransfer crewTransfer = new CrewTransfer(source, target, kerbal);
		}

		private void AddCrew(Part part, ProtoCrewMember kerbal, bool fireVesselUpdate)
		{
			part.AddCrewmember(kerbal);

			kerbal.rosterStatus = ProtoCrewMember.RosterStatus.Assigned;
			if (kerbal.seat != null)
				kerbal.seat.SpawnCrew();

			GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
		}

		private void RemoveCrew(ProtoCrewMember member, Part part, bool fireVesselUpdate)
		{
			part.RemoveCrewmember(member);
			member.seat = null;
			member.rosterStatus = ProtoCrewMember.RosterStatus.Available;

			GameEvents.onVesselChange.Fire(FlightGlobals.ActiveVessel);
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (crewableParts [selectedPartIndexT].protoModuleCrew.Count < crewableParts [selectedPartIndexT].CrewCapacity)
			{
				MoveKerbal(crewableParts [selectedPartIndexS], crewableParts [selectedPartIndexT], kerbalsList[selectedKerbal]);
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
			GUILayout.Label ("Tra.");
			selectedKerbal = GuiUtils.ComboBox.Box (selectedKerbal, kerbalsNames.ToArray (), kerbalsNames);
			GUILayout.Label ("Fr.");
			selectedPartIndexS = GuiUtils.ComboBox.Box (selectedPartIndexS, crewablePartsNamesS.ToArray (), crewablePartsNamesS);
			if (!partHighlightedS)
			{
				if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
				{
					partHighlightedS = true;
					crewableParts [selectedPartIndexS].SetHighlight (true, false);
				}
			}
			else
			{
				if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true)))
				{
					partHighlightedS = false;
					crewableParts [selectedPartIndexS].SetHighlight (false, false);
				}
			}
			GUILayout.Label ("To");
			selectedPartIndexT = GuiUtils.ComboBox.Box (selectedPartIndexT, crewablePartsNamesT.ToArray (), crewablePartsNamesT);
			if (!partHighlightedT)
			{
				if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
				{
					partHighlightedT = true;
					crewableParts [selectedPartIndexT].SetHighlight (true, false);
				}
			}
			else
			{
				if (GUILayout.Button (GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true)))
				{
					partHighlightedT = false;
					crewableParts [selectedPartIndexT].SetHighlight (false, false);
				}
			}
			base.postWindowGUI(windowID);
		}

		override public void afterOnFixedUpdate()
		{
			if (selectedPartIndexS < crewableParts.Count && selectedPartIndexS != old_selectedPartIndexS)
			{
				this.selectedPartSFlightID = crewableParts[selectedPartIndexS].flightID;
				this.old_selectedPartIndexS = selectedPartIndexS;
			}
			if (selectedPartIndexT < crewableParts.Count && selectedPartIndexT != old_selectedPartIndexT)
			{
				this.selectedPartSFlightID = crewableParts[selectedPartIndexT].flightID;
				this.old_selectedPartIndexT = selectedPartIndexT;
			}
			if (selectedKerbal < kerbalsList.Count && selectedKerbal != old_selectedKerbal)
			{
				this.selectedKerbalName = kerbalsList[selectedKerbal].name;
				this.old_selectedKerbal = selectedKerbal;
			}
		}

		override public void postLoad(ConfigNode node)
		{
			if (selectedPartSFlightID != 0 && selectedPartTFlightID != 0 && selectedKerbalName != null) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in crewableParts)
				{
					if (part.flightID == selectedPartSFlightID)
					{
						this.selectedPartIndexS = i;
					}
					if (part.flightID == selectedPartTFlightID)
					{
						this.selectedPartIndexT = i;
					}
					i++;
				}
				i = 0;
				foreach (String kerbalName in kerbalsNames)
				{
					if (kerbalName.CompareTo(this.selectedKerbalName) == 0)
					{
						this.selectedKerbal = i;
					}
					i++;
				}
			}
			this.old_selectedPartIndexS = this.selectedPartIndexS;
			this.old_selectedPartIndexT = this.selectedPartIndexT;
			this.old_selectedKerbal = this.selectedKerbal;
		}
	}
}

