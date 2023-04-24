﻿using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionTargetDock : MechJebModuleScriptAction
    {
        public static string NAME = "TargetDock";

        private readonly List<Part>   dockingPartsList  = new List<Part>();
        private readonly List<string> dockingPartsNames = new List<string>();
        private readonly List<string> controlPartsNames = new List<string>();

        [Persistent(pass = (int)Pass.Type)]
        private int selectedPartIndex;

        [Persistent(pass = (int)Pass.Type)]
        private int controlFromPartIndex;

        [Persistent(pass = (int)Pass.Type)]
        private uint selectedPartFlightID;

        [Persistent(pass = (int)Pass.Type)]
        private uint controlFromPartFlightID;

        private bool partHighlighted;
        private bool partHighlightedControl;

        public MechJebModuleScriptActionTargetDock(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) :
            base(scriptModule, core, actionsList, NAME)
        {
            dockingPartsList.Clear();
            dockingPartsNames.Clear();
            controlPartsNames.Clear();
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel.state != Vessel.State.DEAD)
                {
                    foreach (ModuleDockingNode node in vessel.FindPartModulesImplementing<ModuleDockingNode>())
                    {
                        dockingPartsList.Add(node.part);
                        dockingPartsNames.Add(node.part.partInfo.title);
                        controlPartsNames.Add(node.part.partInfo.title);
                    }
                }
            }
        }

        public override void activateAction()
        {
            base.activateAction();
            if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>() != null &&
                dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>() != null)
            {
                //Check if the target dock is a shielded dock: Open the shield
                if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator != null)
                {
                    if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.actionAvailable)
                    {
                        if (dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Progress == 0)
                        {
                            dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Toggle();
                        }
                    }
                }

                //Check if the "Control" dock is a shielded dock: Open the shield
                if (dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().deployAnimator != null)
                {
                    if (dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().deployAnimator.actionAvailable)
                    {
                        if (dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Progress == 0)
                        {
                            dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().deployAnimator.Toggle();
                        }
                    }
                }

                dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>().SetAsTarget(); //Set target
                core.target.Set(dockingPartsList[selectedPartIndex].GetModule<ModuleDockingNode>());
                dockingPartsList[controlFromPartIndex].GetModule<ModuleDockingNode>().MakeReferenceTransform(); //set "control from here"
            }

            endAction();
        }

        public override void endAction()
        {
            base.endAction();
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("Dock Target");
            if (dockingPartsNames.Count > 0)
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

                GUILayout.Label("C");
                controlFromPartIndex = GuiUtils.ComboBox.Box(controlFromPartIndex, controlPartsNames.ToArray(), controlPartsNames);
                if (!partHighlightedControl)
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
                    {
                        partHighlightedControl = true;
                        dockingPartsList[controlFromPartIndex].SetHighlight(true, true);
                    }
                }
                else
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
                    {
                        partHighlightedControl = false;
                        dockingPartsList[controlFromPartIndex].SetHighlight(false, true);
                    }
                }

                if (selectedPartIndex < dockingPartsList.Count)
                {
                    selectedPartFlightID = dockingPartsList[selectedPartIndex].flightID;
                }

                if (controlFromPartIndex < dockingPartsList.Count)
                {
                    controlFromPartFlightID = dockingPartsList[controlFromPartIndex].flightID;
                }
            }
            else
            {
                GUILayout.Label("-- NO DOCK PART --");
            }

            postWindowGUI(windowID);
        }

        public override void postLoad(ConfigNode node)
        {
            if (selectedPartFlightID != 0 &&
                controlFromPartFlightID !=
                0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
            {
                int i = 0;
                foreach (Part part in dockingPartsList)
                {
                    if (part.flightID == selectedPartFlightID)
                    {
                        selectedPartIndex = i;
                    }

                    if (part.flightID == controlFromPartFlightID)
                    {
                        controlFromPartIndex = i;
                    }

                    i++;
                }
            }
        }
    }
}
