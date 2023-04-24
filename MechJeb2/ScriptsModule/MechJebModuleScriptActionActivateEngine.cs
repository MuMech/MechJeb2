using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionActivateEngine : MechJebModuleScriptAction
    {
        public static    string       NAME             = "ActivateEngine";
        private readonly List<Part>   enginePartsList  = new List<Part>();
        private readonly List<string> enginePartsNames = new List<string>();

        [Persistent(pass = (int)Pass.Type)]
        private int selectedPartIndex;

        [Persistent(pass = (int)Pass.Type)]
        private uint selectedPartFlightID;

        private bool partHighlighted;

        public MechJebModuleScriptActionActivateEngine(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList)
            : base(scriptModule, core, actionsList, NAME)
        {
            enginePartsList.Clear();
            enginePartsNames.Clear();
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel.state != Vessel.State.DEAD)
                {
                    foreach (Part part in vessel.Parts)
                    {
                        if (part.IsEngine())
                        {
                            enginePartsList.Add(part);
                            enginePartsNames.Add(part.partInfo.title);
                        }
                    }
                }
            }
        }

        public override void activateAction()
        {
            base.activateAction();
            if (enginePartsList[selectedPartIndex] != null)
            {
                enginePartsList[selectedPartIndex].force_activate();
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
            GUILayout.Label("Activate engine");

            if (enginePartsList.Count > 0)
            {
                selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, enginePartsNames.ToArray(), enginePartsNames);
                if (!partHighlighted)
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
                    {
                        partHighlighted = true;
                        enginePartsList[selectedPartIndex].SetHighlight(true, true);
                    }
                }
                else
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
                    {
                        partHighlighted = false;
                        enginePartsList[selectedPartIndex].SetHighlight(false, true);
                    }
                }

                if (selectedPartIndex < enginePartsList.Count)
                {
                    selectedPartFlightID = enginePartsList[selectedPartIndex].flightID;
                }
            }
            else
            {
                GUILayout.Label("-- NO ENGINE PART --");
            }

            postWindowGUI(windowID);
        }

        public override void postLoad(ConfigNode node)
        {
            if (selectedPartFlightID !=
                0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
            {
                int i = 0;
                foreach (Part part in enginePartsList)
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
