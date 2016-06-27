using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionCrewTransfer : MechJebModuleScriptAction
	{
		public static String NAME = "CrewTransfer";
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndexS = 0;
		private List<Part> crewableParts;
		private List<String> crewablePartsNamesS;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndexT = 0;
		private List<String> crewablePartsNamesT;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedKerbal = 0;
		private List<ProtoCrewMember> kerbalsList;
		private List<String> kerbalsNames;
		private bool partHighlightedS = false;
		private bool partHighlightedT = false;

		public MechJebModuleScriptActionCrewTransfer (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
		{
			Vessel vessel = FlightGlobals.ActiveVessel;
			crewableParts = new List<Part> ();
			crewablePartsNamesS = new List<String> ();
			crewablePartsNamesT = new List<String> ();
			if (vessel != null)
			{
				foreach (Part part in vessel.Parts)
				{
					if (part.CrewCapacity > 0)
					{
						crewableParts.Add (part);
						crewablePartsNamesS.Add (part.partInfo.title);
						crewablePartsNamesT.Add (part.partInfo.title);
					}
				}
			}
			kerbalsNames = new List<String> ();
			kerbalsList = new List<ProtoCrewMember> ();
			foreach (ProtoCrewMember kerbal in vessel.GetVesselCrew())
			{
				kerbalsNames.Add(kerbal.name);
				kerbalsList.Add (kerbal);
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
	}
}

