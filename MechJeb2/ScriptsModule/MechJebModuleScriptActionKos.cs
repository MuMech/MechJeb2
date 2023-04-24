using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionKos : MechJebModuleScriptAction
    {
        public static    string           NAME          = "kOS";
        private readonly List<Part>       kosParts      = new List<Part>();
        private readonly List<string>     kosPartsNames = new List<string>();
        private readonly List<PartModule> kosModules    = new List<PartModule>();

        [Persistent(pass = (int)Pass.Type)]
        private EditableInt selectedPartIndex = 0;

        [Persistent(pass = (int)Pass.Type)]
        private uint selectedPartFlightID;

        [Persistent(pass = (int)Pass.Type)]
        private string command = "";

        [Persistent(pass = (int)Pass.Type)]
        private bool openTerminal = true;

        [Persistent(pass = (int)Pass.Type)]
        private bool waitFinish = true;

        [Persistent(pass = (int)Pass.Type)]
        private bool closeTerminal = true;

        private bool partHighlighted;

        //Reflected objects cache
        private object sharedObjects;
        private object interpreter;

        public MechJebModuleScriptActionKos(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptActionsList actionsList) : base(
            scriptModule, core, actionsList, NAME)
        {
            kosParts.Clear();
            kosPartsNames.Clear();
            kosModules.Clear();
            foreach (Vessel vessel in FlightGlobals.Vessels)
            {
                if (vessel.state != Vessel.State.DEAD)
                {
                    foreach (Part part in vessel.Parts)
                    {
                        foreach (PartModule module in part.Modules)
                        {
                            if (module.moduleName.Contains("kOSProcessor"))
                            {
                                kosParts.Add(part);
                                kosPartsNames.Add(part.partInfo.title);
                                kosModules.Add(module);
                            }
                        }
                    }
                }
            }
        }

        public override void activateAction()
        {
            base.activateAction();
            if (selectedPartIndex < kosModules.Count)
            {
                if (openTerminal)
                {
                    kosModules[selectedPartIndex].GetType()
                        .InvokeMember("OpenWindow", BindingFlags.InvokeMethod, null, kosModules[selectedPartIndex], null);
                }

                sharedObjects = kosModules[selectedPartIndex].GetType().GetField("shared", BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(kosModules[selectedPartIndex]);
                if (sharedObjects != null)
                {
                    interpreter = sharedObjects.GetType().GetProperty("Interpreter").GetValue(sharedObjects, null);
                    if (interpreter != null)
                    {
                        interpreter.GetType().InvokeMember("ProcessCommand",
                            BindingFlags.InvokeMethod | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, interpreter,
                            new object[] { command });
                        if (!waitFinish)
                        {
                            endAction();
                        }
                    }
                    else
                    {
                        Debug.LogError("---- NO Interpreter OBJECT ----");
                        endAction();
                    }
                }
                else
                {
                    Debug.LogError("---- NO SHARED OBJECT ----");
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
            if (selectedPartIndex < kosModules.Count)
            {
                if (closeTerminal)
                {
                    kosModules[selectedPartIndex].GetType()
                        .InvokeMember("CloseWindow", BindingFlags.InvokeMethod, null, kosModules[selectedPartIndex], null);
                }
            }
        }

        public override void afterOnFixedUpdate()
        {
            //If we are waiting for the sequence to finish, we check the status
            if (!isExecuted() && isStarted())
            {
                if (isCPUActive(kosModules[selectedPartIndex]))
                {
                    endAction();
                }
            }
        }

        public override void WindowGUI(int windowID)
        {
            preWindowGUI(windowID);
            base.WindowGUI(windowID);
            GUILayout.Label("kOS");
            if (kosPartsNames.Count > 0)
            {
                selectedPartIndex = GuiUtils.ComboBox.Box(selectedPartIndex, kosPartsNames.ToArray(), kosPartsNames);
                if (!partHighlighted)
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
                    {
                        partHighlighted = true;
                        kosParts[selectedPartIndex].SetHighlight(true, false);
                    }
                }
                else
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view_a", true), GUILayout.ExpandWidth(false)))
                    {
                        partHighlighted = false;
                        kosParts[selectedPartIndex].SetHighlight(false, false);
                    }
                }

                command       = GUILayout.TextField(command, GUILayout.Width(120), GUILayout.ExpandWidth(true));
                openTerminal  = GUILayout.Toggle(openTerminal, "Open Terminal");
                waitFinish    = GUILayout.Toggle(waitFinish, "Wait Finish");
                closeTerminal = GUILayout.Toggle(closeTerminal, "Close Terminal");
            }
            else
            {
                GUILayout.Label("-- NO kOS module on vessel --");
            }

            if (selectedPartIndex < kosParts.Count)
            {
                selectedPartFlightID = kosParts[selectedPartIndex].flightID;
            }

            postWindowGUI(windowID);
        }

        public override void postLoad(ConfigNode node)
        {
            if (selectedPartFlightID !=
                0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
            {
                int i = 0;
                foreach (Part part in kosParts)
                {
                    if (part.flightID == selectedPartFlightID)
                    {
                        selectedPartIndex = i;
                    }

                    i++;
                }
            }
        }

        public bool isCPUActive(object module)
        {
            if (sharedObjects != null && interpreter != null)
            {
                //We check if the interpreter is waiting to know if our program has been executed
                bool waiting = (bool)interpreter.GetType().InvokeMember("IsWaitingForCommand", BindingFlags.InvokeMethod, null, interpreter, null);
                if (waiting)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
