using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionIRSequencer : MechJebModuleScriptAction
	{
		public static String NAME = "IRSequencer";
		private List<Part> irsequencerParts = new List<Part>();
		private List<String> irsequencerPartsNames = new List<String>();
		private List<PartModule> irsequencerModules = new List<PartModule>();
		private List<List<String>> irsequencerSequenceNames = new List<List<String>>();
		private List<List<object>> irsequencerSequences = new List<List<object>>();
		private List<String> currentIrsequencerSequenceNames = new List<String>();
		private List<object> currentIrsequencerSequences = new List<object>();
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedSequenceIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private Guid selectedSequenceGuid = Guid.Empty;
		[Persistent(pass = (int)Pass.Type)]
		private int actionType;
		[Persistent(pass = (int)Pass.Type)]
		private bool waitFinish = true;
		private List<String> actionTypes = new List<String>();
		private bool partHighlighted = false;
		private int old_selectedPartIndex = 0;

		public MechJebModuleScriptActionIRSequencer (MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList):base(scriptModule, core, actionsList, NAME)
		{
			irsequencerParts.Clear();
			irsequencerPartsNames.Clear();
			irsequencerModules.Clear();
			this.actionTypes.Clear();
			this.actionTypes.Add("Start Sequence");
			this.actionTypes.Add("Pause Sequence");
			this.actionTypes.Add("Reset Sequence");
			foreach (Vessel vessel in FlightGlobals.Vessels)
			{
				if (vessel.state != Vessel.State.DEAD)
				{
					foreach (Part part in vessel.Parts)
					{
						if (part.name.Contains("RoboticsControlUnit"))
						{
							foreach (PartModule module in part.Modules)
							{
								if (module.moduleName.Contains("ModuleSequencer"))
								{
									this.irsequencerParts.Add(part);
									this.irsequencerPartsNames.Add(part.partInfo.title);
									this.irsequencerModules.Add(module);
								}
							}
						}
					}
				}
			}
			this.updateSequencesList();
		}

		private void updateSequencesList()
		{
			irsequencerSequenceNames.Clear();
			irsequencerSequences.Clear();
			foreach (PartModule module in this.irsequencerModules)
			{
				List<String> sequenceNames = new List<String>();
				List<object> sequenceObjects = new List<object>();
				//List all the sequences on the sequencer
				var sequences = module.GetType().GetField("sequences").GetValue(module);
				if (sequences != null)
				{
					if (sequences is IEnumerable)
					{
						foreach (var sequence in sequences as IEnumerable)
						{
							Guid sID = getSequenceGuid(sequence);
							String name = getSequenceName(sequence);
							if (sID != Guid.Empty && name != null && name.Length > 0)
							{
								sequenceNames.Add(name);
								sequenceObjects.Add(sequence);
							}
						}
					}
				}
				this.irsequencerSequenceNames.Add(sequenceNames);
				this.irsequencerSequences.Add(sequenceObjects);
			}
			this.populateSequencesList();
		}

		private void populateSequencesList()
		{
			this.currentIrsequencerSequenceNames.Clear();
			this.currentIrsequencerSequences.Clear();
			if (this.selectedPartIndex < this.irsequencerSequenceNames.Count && this.selectedPartIndex < this.irsequencerSequences.Count)
			{
				this.currentIrsequencerSequenceNames.AddRange(this.irsequencerSequenceNames[this.selectedPartIndex]);
				this.currentIrsequencerSequences.AddRange(this.irsequencerSequences[this.selectedPartIndex]);
			}
			this.old_selectedPartIndex = this.selectedPartIndex;
		}

		override public void activateAction()
		{
			base.activateAction();
			if (this.selectedPartIndex < this.irsequencerModules.Count)
			{
				if (actionType == 0)
				{
					this.irsequencerModules[this.selectedPartIndex].GetType().InvokeMember("StartSequence", System.Reflection.BindingFlags.InvokeMethod, null, this.irsequencerModules[this.selectedPartIndex], new object[] { this.selectedSequenceGuid });
					if (!waitFinish)
					{
						this.endAction();
					}
				}
				else if (actionType == 1)
				{
					this.irsequencerModules[this.selectedPartIndex].GetType().InvokeMember("PauseSequence", System.Reflection.BindingFlags.InvokeMethod, null, this.irsequencerModules[this.selectedPartIndex], new object[] { this.selectedSequenceGuid });
					this.endAction();
				}
				else if (actionType == 2)
				{
					this.irsequencerModules[this.selectedPartIndex].GetType().InvokeMember("ResetSequence", System.Reflection.BindingFlags.InvokeMethod, null, this.irsequencerModules[this.selectedPartIndex], new object[] { this.selectedSequenceGuid });
					this.endAction();
				}
			}
			else
			{
				this.endAction();
			}
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void afterOnFixedUpdate()
		{
			//If we are waiting for the sequence to finish, we check the status
			if (!this.isExecuted() && this.isStarted())
			{
				if (getSequenceIsFinished(currentIrsequencerSequences[selectedSequenceIndex]))
				{
					this.endAction();
				}
			}
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("IR");
			if (irsequencerPartsNames.Count > 0)
			{
				actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
				selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, irsequencerPartsNames.ToArray(), irsequencerPartsNames);
				if (old_selectedPartIndex != selectedPartIndex)
				{
					//Change in the selected sequencer part. We change the list of sequences
					this.populateSequencesList();
				}
				if (!partHighlighted)
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
					{
						partHighlighted = true;
						irsequencerParts[selectedPartIndex].SetHighlight(true, false);
					}
				}
				else
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
					{
						partHighlighted = false;
						irsequencerParts[selectedPartIndex].SetHighlight(false, false);
					}
				}
				if (currentIrsequencerSequenceNames.Count > 0)
				{
					selectedSequenceIndex = GuiUtils.ComboBox.Box(selectedSequenceIndex, currentIrsequencerSequenceNames.ToArray(), currentIrsequencerSequenceNames);
					if (actionType == 0)
					{
						waitFinish = GUILayout.Toggle(waitFinish, "Wait finish");
					}
				}
				else
				{
					GUILayout.Label("-- NO Sequence in this sequencer --");
				}
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/refresh", true), GUILayout.ExpandWidth(false)))
				{
					this.updateSequencesList();
				}
			}
			else
			{
				GUILayout.Label("-- NO IR Sequencer on vessel --");
			}
			if (selectedPartIndex < irsequencerParts.Count)
			{
				this.selectedPartFlightID = irsequencerParts[selectedPartIndex].flightID;
			}
			if (selectedSequenceIndex < currentIrsequencerSequences.Count)
			{
				this.selectedSequenceGuid = this.getSequenceGuid(currentIrsequencerSequences[selectedSequenceIndex]);
			}

			base.postWindowGUI(windowID);
		}

		override public void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in irsequencerParts)
				{
					if (part.flightID == selectedPartFlightID)
					{
						this.selectedPartIndex = i;
					}
					i++;
				}
			}
			//Populate the current list of sequences based on the selected part
			this.populateSequencesList();
			//then try to find the sequence Guid in the selected part sequences list
			if (selectedSequenceGuid != Guid.Empty)
			{
				int i = 0;
				foreach (object sequence in currentIrsequencerSequences)
				{
					if (getSequenceGuid(sequence) == selectedSequenceGuid)
					{
						this.selectedSequenceIndex = i;
					}
					i++;
				}
			}
		}

		private Guid getSequenceGuid(object sequence)
		{
			return (Guid)sequence.GetType().GetField("sequenceID").GetValue(sequence);
		}

		private String getSequenceName(object sequence)
		{
			return (String)sequence.GetType().GetField("name").GetValue(sequence);
		}

		private bool getSequenceIsFinished(object sequence)
		{
			return (bool)sequence.GetType().GetField("isFinished").GetValue(sequence);
		}
	}
}

