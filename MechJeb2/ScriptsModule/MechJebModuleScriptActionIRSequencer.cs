using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionIRSequencer : MechJebModuleScriptAction
    {
        public static    string             NAME                            = "IRSequencer";
        private readonly List<Part>         irsequencerParts                = new List<Part>();
        private readonly List<string>       irsequencerPartsNames           = new List<string>();
        private readonly List<PartModule>   irsequencerModules              = new List<PartModule>();
        private readonly List<List<string>> irsequencerSequenceNames        = new List<List<string>>();
        private readonly List<List<object>> irsequencerSequences            = new List<List<object>>();
        private readonly List<string>       currentIrsequencerSequenceNames = new List<string>();
        private readonly List<object>       currentIrsequencerSequences     = new List<object>();

        [Persistent(pass = (int)Pass.Type)]
        private EditableInt selectedPartIndex = 0;

        [Persistent(pass = (int)Pass.Type)]
        private uint selectedPartFlightID;

        [Persistent(pass = (int)Pass.Type)]
        private EditableInt selectedSequenceIndex = 0;

        [Persistent(pass = (int)Pass.Type)]
        private Guid selectedSequenceGuid = Guid.Empty;

        [Persistent(pass = (int)Pass.Type)]
        private int actionType;

        [Persistent(pass = (int)Pass.Type)]
        private bool waitFinish = true;

        private readonly List<string> actionTypes = new List<string>();
        private          bool         partHighlighted;
        private          int          old_selectedPartIndex;

        public MechJebModuleScriptActionIRSequencer(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) :
            base(scriptModule, core, actionsList, NAME)
        {
            irsequencerParts.Clear();
            irsequencerPartsNames.Clear();
            irsequencerModules.Clear();
            actionTypes.Clear();
            actionTypes.Add("Start Sequence");
            actionTypes.Add("Pause Sequence");
            actionTypes.Add("Reset Sequence");
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
                                    irsequencerParts.Add(part);
                                    irsequencerPartsNames.Add(part.partInfo.title);
                                    irsequencerModules.Add(module);
                                }
                            }
                        }
                    }
                }
            }

            updateSequencesList();
        }

        private void updateSequencesList()
        {
            irsequencerSequenceNames.Clear();
            irsequencerSequences.Clear();
            foreach (PartModule module in irsequencerModules)
            {
                var sequenceNames = new List<string>();
                var sequenceObjects = new List<object>();
                //List all the sequences on the sequencer
                object sequences = module.GetType().GetField("sequences").GetValue(module);
                if (sequences != null)
                {
                    if (sequences is IEnumerable)
                    {
                        foreach (object sequence in sequences as IEnumerable)
                        {
                            Guid sID = getSequenceGuid(sequence);
                            string name = getSequenceName(sequence);
                            if (sID != Guid.Empty && name != null && name.Length > 0)
                            {
                                sequenceNames.Add(name);
                                sequenceObjects.Add(sequence);
                            }
                        }
                    }
                }

                irsequencerSequenceNames.Add(sequenceNames);
                irsequencerSequences.Add(sequenceObjects);
            }

            populateSequencesList();
        }

        private void populateSequencesList()
        {
            currentIrsequencerSequenceNames.Clear();
            currentIrsequencerSequences.Clear();
            if (selectedPartIndex < irsequencerSequenceNames.Count && selectedPartIndex < irsequencerSequences.Count)
            {
                currentIrsequencerSequenceNames.AddRange(irsequencerSequenceNames[selectedPartIndex]);
                currentIrsequencerSequences.AddRange(irsequencerSequences[selectedPartIndex]);
            }

            old_selectedPartIndex = selectedPartIndex;
        }

        public override void activateAction()
        {
            base.activateAction();
            if (selectedPartIndex < irsequencerModules.Count)
            {
                if (actionType == 0)
                {
                    irsequencerModules[selectedPartIndex].GetType().InvokeMember("StartSequence", BindingFlags.InvokeMethod, null,
                        irsequencerModules[selectedPartIndex], new object[] { selectedSequenceGuid });
                    if (!waitFinish)
                    {
                        endAction();
                    }
                }
                else if (actionType == 1)
                {
                    irsequencerModules[selectedPartIndex].GetType().InvokeMember("PauseSequence", BindingFlags.InvokeMethod, null,
                        irsequencerModules[selectedPartIndex], new object[] { selectedSequenceGuid });
                    endAction();
                }
                else if (actionType == 2)
                {
                    irsequencerModules[selectedPartIndex].GetType().InvokeMember("ResetSequence", BindingFlags.InvokeMethod, null,
                        irsequencerModules[selectedPartIndex], new object[] { selectedSequenceGuid });
                    endAction();
                }
            }
            else
            {
                endAction();
            }
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void afterOnFixedUpdate()
        {
            //If we are waiting for the sequence to finish, we check the status
            if (!isExecuted() && isStarted())
            {
                if (getSequenceIsFinished(currentIrsequencerSequences[selectedSequenceIndex]))
                {
                    endAction();
                }
            }
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("IR");
            if (irsequencerPartsNames.Count > 0)
            {
                actionType        = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);
                selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, irsequencerPartsNames.ToArray(), irsequencerPartsNames);
                if (old_selectedPartIndex != selectedPartIndex)
                {
                    //Change in the selected sequencer part. We change the list of sequences
                    populateSequencesList();
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
                    selectedSequenceIndex = GuiUtils.ComboBox.Box(selectedSequenceIndex, currentIrsequencerSequenceNames.ToArray(),
                        currentIrsequencerSequenceNames);
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
                    updateSequencesList();
                }
            }
            else
            {
                GUILayout.Label("-- NO IR Sequencer on vessel --");
            }

            if (selectedPartIndex < irsequencerParts.Count)
            {
                selectedPartFlightID = irsequencerParts[selectedPartIndex].flightID;
            }

            if (selectedSequenceIndex < currentIrsequencerSequences.Count)
            {
                selectedSequenceGuid = getSequenceGuid(currentIrsequencerSequences[selectedSequenceIndex]);
            }

            postWindowGUI(windowID);
        }

        public override void postLoad(ConfigNode node)
        {
            if (selectedPartFlightID !=
                0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
            {
                int i = 0;
                foreach (Part part in irsequencerParts)
                {
                    if (part.flightID == selectedPartFlightID)
                    {
                        selectedPartIndex = i;
                    }

                    i++;
                }
            }

            //Populate the current list of sequences based on the selected part
            populateSequencesList();
            //then try to find the sequence Guid in the selected part sequences list
            if (selectedSequenceGuid != Guid.Empty)
            {
                int i = 0;
                foreach (object sequence in currentIrsequencerSequences)
                {
                    if (getSequenceGuid(sequence) == selectedSequenceGuid)
                    {
                        selectedSequenceIndex = i;
                    }

                    i++;
                }
            }
        }

        private Guid getSequenceGuid(object sequence)
        {
            return (Guid)sequence.GetType().GetField("sequenceID").GetValue(sequence);
        }

        private string getSequenceName(object sequence)
        {
            return (string)sequence.GetType().GetField("name").GetValue(sequence);
        }

        private bool getSequenceIsFinished(object sequence)
        {
            return (bool)sequence.GetType().GetField("isFinished").GetValue(sequence);
        }
    }
}
