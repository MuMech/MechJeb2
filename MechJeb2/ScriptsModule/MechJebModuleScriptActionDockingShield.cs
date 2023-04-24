using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionDockingShield : MechJebModuleScriptAction
    {
        public static    string       NAME              = "DockingShield";
        private readonly List<Part>   dockingPartsList  = new List<Part>();
        private readonly List<string> dockingPartsNames = new List<string>();

        [Persistent(pass = (int)Pass.Type)]
        private int selectedPartIndex;

        [Persistent(pass = (int)Pass.Type)]
        private int actionType;

        [Persistent(pass = (int)Pass.Type)]
        private uint selectedPartFlightID;

        private          bool         partHighlighted;
        private readonly List<string> actionTypes = new List<string>();

        public MechJebModuleScriptActionDockingShield(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList)
            : base(scriptModule, core, actionsList, NAME)
        {
            actionTypes.Clear();
            actionTypes.Add("Open");
            actionTypes.Add("Close");
            dockingPartsList.Clear();
            dockingPartsNames.Clear();
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel.state != Vessel.State.DEAD)
                {
                    foreach (ModuleDockingNode node in vessel.FindPartModulesImplementing<ModuleDockingNode>())
                    {
                        if (node.deployAnimator != null)
                        {
                            if (node.deployAnimator.actionAvailable)
                            {
                                dockingPartsList.Add(node.part);
                                dockingPartsNames.Add(node.part.partInfo.title);
                            }
                        }
                    }
                }
            }
        }

        public override void activateAction()
        {
            if (dockingPartsList[selectedPartIndex] != null)
            {
                if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
                {
                    if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.actionAvailable)
                    {
                        if (actionType == 0)
                        {
                            if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Progress == 0)
                            {
                                dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Toggle();
                            }
                        }
                        else
                        {
                            if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Progress == 1)
                            {
                                dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Toggle();
                            }
                        }
                    }
                }
            }

            base.activateAction();
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void afterOnFixedUpdate()
        {
            if (started && !executed)
            {
                if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null)
                {
                    if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.status.CompareTo("Locked") == 0)
                    {
                        endAction();
                    }
                }
            }
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("Toggle Shield State");

            if (dockingPartsList.Count > 0)
            {
                selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, dockingPartsNames.ToArray(), dockingPartsNames);
                if (!partHighlighted)
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
                    {
                        partHighlighted = true;
                        dockingPartsList[selectedPartIndex].SetHighlight(true, true);
                    }
                }
                else
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
                    {
                        partHighlighted = false;
                        dockingPartsList[selectedPartIndex].SetHighlight(false, true);
                    }
                }

                actionType = GuiUtils.ComboBox.Box(actionType, actionTypes.ToArray(), actionTypes);

                if (selectedPartIndex < dockingPartsList.Count)
                {
                    selectedPartFlightID = dockingPartsList[selectedPartIndex].flightID;
                }
            }
            else
            {
                GUILayout.Label("-- NO SHIELDED DOCK PART --");
            }

            postWindowGUI(windowID);
        }

        public override void postLoad(ConfigNode node)
        {
            if (selectedPartFlightID !=
                0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
            {
                int i = 0;
                foreach (Part part in dockingPartsList)
                {
                    if (part.flightID == selectedPartFlightID)
                    {
                        selectedPartIndex = i;
                    }

                    i++;
                }
            }
        }
    }
}
