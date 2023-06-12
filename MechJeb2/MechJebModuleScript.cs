using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;
using File = KSP.IO.File;

namespace MuMech
{
    [UsedImplicitly]
    public class MechJebModuleScript : DisplayModule, IMechJebModuleScriptActionsListParent
    {
        private          bool                           started;
        private readonly MechJebModuleScriptActionsList actionsList;
        public           Texture2D                      imageRed   = new Texture2D(20, 20);
        public           Texture2D                      imageGreen = new Texture2D(20, 20);
        public           Texture2D                      imageGray  = new Texture2D(20, 20);

        [Persistent(pass = (int)Pass.Local)]
        private bool minifiedGUI;

        private readonly List<string> scriptsList     = new List<string>();
        private readonly List<string> memorySlotsList = new List<string>();

        [Persistent(pass = (int)Pass.Type)]
        private readonly string[] scriptNames = { "", "", "", "", "", "", "", "" };

        [Persistent(pass = (int)Pass.Type)]
        private readonly string[] globalScriptNames = { "", "", "", "", "", "", "", "" };

        [Persistent(pass = (int)Pass.Local)]
        private int selectedSlot;

        [Persistent(pass = (int)Pass.Local)]
        private int selectedMemorySlotType;

        [Persistent(pass = (int)Pass.Local)]
        public string vesselSaveName;

        [Persistent(pass = (int)Pass.Local)]
        private int activeSavepoint = -1;

        public  int  pendingloadBreakpoint = -1;
        private bool moduleStarted;

        private bool savePointJustSet;

        //Warmup time for restoring after load
        private          bool  warmingUp;
        private          int   spendTime;
        private readonly int   initTime = 5; //Add a 5s warmup time
        private          float startTime;

        private bool deployScriptNameField;

        //Flash message to notify user
        private          string       flashMessage = "";
        private          int          flashMessageType; //0=yellow, 1=red (error)
        private          float        flashMessageStartTime;
        private          bool         waitingDeletionConfirmation;
        private readonly List<string> compatiblePluginsInstalled = new List<string>();
        private          bool         addActionDisabled;
        private          int          old_selectedMemorySlotType;

        public MechJebModuleScript(MechJebCore core) : base(core)
        {
            //Create images
            for (int i = 0; i < 20; i++)
            {
                for (int j = 0; j < 20; j++)
                {
                    if (i < 5 || j < 5 || i > 15 || j > 15)
                    {
                        imageRed.SetPixel(i, j, Color.clear);
                        imageGreen.SetPixel(i, j, Color.clear);
                        imageGray.SetPixel(i, j, Color.clear);
                    }
                    else
                    {
                        imageRed.SetPixel(i, j, Color.red);
                        imageGreen.SetPixel(i, j, Color.green);
                        imageGray.SetPixel(i, j, Color.gray);
                    }
                }
            }

            imageRed.Apply();
            imageGreen.Apply();
            imageGray.Apply();
            memorySlotsList.Clear();
            memorySlotsList.Add("Global Memory");
            memorySlotsList.Add("Vessel Memory");
            //Init main actions list
            actionsList = new MechJebModuleScriptActionsList(core, this, this, 0);
        }

        public void updateScriptsNames()
        {
            scriptsList.Clear();
            for (int i = 0; i < 8; i++)
            {
                if (selectedMemorySlotType == 1)
                {
                    scriptsList.Add("Slot " + (i + 1) + " - " + scriptNames[i]);
                }
                else
                {
                    scriptsList.Add("Slot " + (i + 1) + " - " + globalScriptNames[i]);
                }
            }
        }

        public override void OnStart(PartModule.StartState state)
        {
            //Connection with IRRobotics sequencer
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.FullName.Contains("InfernalRobotics"))
                {
                    compatiblePluginsInstalled.Add("IRSequencer");
                }
                else if (assembly.FullName.Contains("kOS"))
                {
                    compatiblePluginsInstalled.Add("kOS");
                }
            }

            //Populate Actions names. Need to run this after the compatibility check with other plugins
            actionsList.refreshActionNamesPlugins();

            //Don't know why sometimes this value can be "empty" but not null, causing an empty vessel name...
            if (vesselSaveName != null)
            {
                if (vesselSaveName.Length == 0)
                {
                    vesselSaveName = null;
                }
            }

            if (vessel != null)
            {
                if (vesselSaveName == null)
                {
                    //Try to have only one vessel name, whatever the new vessel name. We use the vessel name of the first time the system was instanciated
                    //Can cause problem with load/save...
                    vesselSaveName =
                        vessel != null
                            ? string.Join("_", vessel.vesselName.Split(Path.GetInvalidFileNameChars()))
                            : null; // Strip illegal char from the filename
                }

                //MechJebCore instances
                List<MechJebCore> mechjebCoresList = vessel.FindPartModulesImplementing<MechJebCore>();
                if (mechjebCoresList.Count > 1)
                {
                    foreach (MechJebCore mjCore in mechjebCoresList)
                    {
                        if (mjCore.GetComputerModule<MechJebModuleScript>() != null)
                        {
                            if (mjCore.GetComputerModule<MechJebModuleScript>().vesselSaveName == null)
                            {
                                mjCore.GetComputerModule<MechJebModuleScript>().vesselSaveName = vesselSaveName; //Set the unique vessel name
                            }
                        }
                    }
                }
            }

            LoadScriptModuleConfig();
            moduleStarted = true;
        }

        public override void OnModuleEnabled()
        {
        }

        public override void OnModuleDisabled()
        {
        }

        public override void OnActive()
        {
        }

        public override void OnInactive()
        {
        }

        public override void OnSave(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnSave(local, type, global);
            if (global != null)
                SaveScriptModuleConfig();
        }

        public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
        {
            base.OnLoad(local, type, global);
            LoadScriptModuleConfig();
        }

        public void SaveScriptModuleConfig()
        {
            string slotName = getSaveSlotName(false);
            var node = ConfigNode.CreateConfigFromObject(this, (int)Pass.Type, null);
            node.Save(MuUtils.GetCfgPath("mechjeb_settings_script_" + slotName + "_conf.cfg"));
        }

        public void LoadScriptModuleConfig()
        {
            string slotName = getSaveSlotName(false);
            try
            {
                if (File.Exists<MechJebCore>("mechjeb_settings_script_" + slotName + "_conf.cfg"))
                {
                    var node = ConfigNode.Load(MuUtils.GetCfgPath("mechjeb_settings_script_" + slotName + "_conf.cfg"));
                    if (node == null) return;
                    ConfigNode.LoadObjectFromConfig(this, node);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("MechJebModuleScript.LoadConfig caught an exception trying to load mechjeb_settings_script_" + slotName +
                               "_conf.cfg: " + e);
                return;
            }

            updateScriptsNames();
        }

        protected override void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            if (warmingUp)
            {
                GUILayout.Label(Localizer.Format("#MechJeb_ScriptMod_label1", spendTime)); //"Warming up. Please wait... <<1>> s
            }
            else
            {
                GUILayout.BeginHorizontal();
                var style2 = new GUIStyle(GUI.skin.button);
                if (!started && actionsList.getActionsCount() > 0)
                {
                    style2.normal.textColor = Color.green;
                    if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button1"), style2)) //"▶ START"
                    {
                        start();
                    }

                    style2.normal.textColor = Color.white;
                    if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button2"), style2)) //"☇ Reset"
                    {
                        actionsList.recursiveResetStatus();
                    }

                    addActionDisabled = GUILayout.Toggle(addActionDisabled, Localizer.Format("#MechJeb_ScriptMod_checkbox1")); //"Hide Add Actions"
                }
                else if (started)
                {
                    style2.normal.textColor = Color.red;
                    if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button3"), style2)) //"■ STOP"
                    {
                        stop();
                    }
                }

                if (actionsList.getActionsCount() > 0)
                {
                    if (minifiedGUI)
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button4"))) //"▼ Full GUI"
                        {
                            minifiedGUI = false;
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button5"))) //"△ Compact GUI"
                        {
                            minifiedGUI = true;
                        }
                    }
                }

                GUILayout.EndHorizontal();
                if (!minifiedGUI && !started)
                {
                    GUILayout.BeginHorizontal();
                    style2.normal.textColor = Color.white;
                    if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button6"), style2)) //"Clear All"
                    {
                        actionsList.clearAll();
                    }

                    selectedMemorySlotType = GuiUtils.ComboBox.Box(selectedMemorySlotType, memorySlotsList.ToArray(), memorySlotsList);
                    if (selectedMemorySlotType != old_selectedMemorySlotType)
                    {
                        old_selectedMemorySlotType = selectedMemorySlotType;
                        updateScriptsNames();
                    }

                    selectedSlot = GuiUtils.ComboBox.Box(selectedSlot, scriptsList.ToArray(), scriptsList);
                    if (deployScriptNameField)
                    {
                        if (selectedMemorySlotType == 1)
                        {
                            scriptNames[selectedSlot] =
                                GUILayout.TextField(scriptNames[selectedSlot], GUILayout.Width(120), GUILayout.ExpandWidth(false));
                        }
                        else
                        {
                            globalScriptNames[selectedSlot] = GUILayout.TextField(globalScriptNames[selectedSlot], GUILayout.Width(120),
                                GUILayout.ExpandWidth(false));
                        }

                        if (scriptNames[selectedSlot].Length > 20) //Limit the script name to 20 chars
                        {
                            if (selectedMemorySlotType == 1)
                            {
                                scriptNames[selectedSlot] = scriptNames[selectedSlot].Substring(0, 20);
                            }
                            else
                            {
                                globalScriptNames[selectedSlot] = globalScriptNames[selectedSlot].Substring(0, 20);
                            }
                        }

                        if (GUILayout.Button("<<", GUILayout.ExpandWidth(false)))
                        {
                            deployScriptNameField = false;
                            updateScriptsNames();
                            SaveScriptModuleConfig();
                        }
                    }
                    else
                    {
                        if (GUILayout.Button(">>", GUILayout.ExpandWidth(false)))
                        {
                            deployScriptNameField = true;
                        }
                    }

                    if (GUILayout.Button("✖", GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        if (!waitingDeletionConfirmation)
                        {
                            waitingDeletionConfirmation = true;
                            setFlashMessage(
                                "Warning: To confirm deletion of slot " + (selectedSlot + 1) + " - " + scriptNames[selectedSlot] +
                                ", press again the delete button", 0);
                        }
                        else
                        {
                            DeleteConfig(selectedSlot, true, false);
                            if (selectedMemorySlotType == 1)
                            {
                                scriptNames[selectedSlot] = "";
                            }
                            else
                            {
                                globalScriptNames[selectedSlot] = "";
                            }

                            updateScriptsNames();
                            SaveScriptModuleConfig();
                        }
                    }

                    if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button7"), style2)) //"Save"
                    {
                        SaveConfig(selectedSlot, true, false);
                    }

                    if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button8"), style2)) //"Load"
                    {
                        LoadConfig(selectedSlot, true, false);
                    }

                    GUILayout.EndHorizontal();
                    if (flashMessage.Length > 0)
                    {
                        GUILayout.BeginHorizontal();
                        GUIStyle sFlash = flashMessageType == 1 ? GuiUtils.redLabel : GuiUtils.yellowLabel;
                        GUILayout.Label(flashMessage, sFlash);
                        GUILayout.EndHorizontal();
                    }
                }

                actionsList.actionsWindowGui(windowID); //Render Actions list
            }

            GUILayout.EndVertical();
            base.WindowGUI(windowID);
        }

        public override GUILayoutOption[] WindowOptions()
        {
            return new[] { GUILayout.Width(800), GUILayout.Height(50) };
        }

        public override string GetName()
        {
            return Localizer.Format("#MechJeb_ScriptMod_title"); //"Scripting Module"
        }

        public override string IconName()
        {
            return "Scripting Module";
        }

        public void start()
        {
            if (actionsList.getActionsCount() > 0)
            {
                actionsList.recursiveUpdateActionsIndex(0);
                actionsList.start();
                started = true;
            }
        }

        public void stop()
        {
            started = false;
            //Clean abord the current action
            actionsList.stop();
        }

        public void notifyEndActionsList()
        {
            setActiveSavepoint(-1); //Reset save point to prevent a manual quicksave to open the previous savepoint
            stop();
        }

        public override void OnFixedUpdate()
        {
            //Check if we are restoring after a load
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (activeSavepoint >= 0 && moduleStarted && !savePointJustSet) //We have a pending active savepoint
                {
                    //Warmup time for restoring after load
                    if (startTime > 0)
                    {
                        spendTime = initTime - (int)Math.Round(Time.time - startTime);
                        if (spendTime <= 0)
                        {
                            warmingUp = false;
                            startTime = 0;
                            LoadConfig(selectedSlot, false, false);
                            int asp = activeSavepoint;
                            activeSavepoint = -1;
                            startAfterIndex(asp);
                        }
                    }
                    else
                    {
                        startTime = Time.time;
                        warmingUp = true;
                    }
                }

                if (pendingloadBreakpoint >= 0 && moduleStarted)
                {
                    //Warmup time for restoring after switch
                    if (startTime > 0)
                    {
                        spendTime = initTime - (int)Math.Round(Time.time - startTime);
                        if (spendTime <= 0)
                        {
                            warmingUp = false;
                            startTime = 0;
                            int breakpoint = pendingloadBreakpoint;
                            pendingloadBreakpoint = -1;
                            loadFromBreakpoint(breakpoint);
                        }
                    }
                    else
                    {
                        startTime = Time.time;
                        warmingUp = true;
                    }
                }
            }

            actionsList.OnFixedUpdate(); //List action update

            //Check if we need to close the flashMessage
            if (flashMessageStartTime > 0f)
            {
                float flashSpendTime = (int)Math.Round(Time.time - flashMessageStartTime);
                if (flashSpendTime > 5f)
                {
                    flashMessage                = "";
                    flashMessageStartTime       = 0f;
                    waitingDeletionConfirmation = false;
                }
            }
        }

        public string getSaveSlotName(bool forceSlotName)
        {
            string slotName = vesselSaveName;
            if (selectedMemorySlotType == 0 && !forceSlotName)
            {
                slotName = "G";
            }

            return slotName;
        }

        public void LoadConfig(int slot, bool notify, bool forceSlotName)
        {
            if (vessel == null)
            {
                return;
            }

            if (slot != 9)
            {
                selectedSlot = slot; //Select the slot for the UI. Except slot 9 (temp)
            }

            string slotName = getSaveSlotName(forceSlotName);
            var node = new ConfigNode("MechJebScriptSettings");
            if (File.Exists<MechJebCore>("mechjeb_settings_script_" + slotName + "_" + slot + ".cfg"))
            {
                try
                {
                    node = ConfigNode.Load(MuUtils.GetCfgPath("mechjeb_settings_script_" + slotName + "_" + slot + ".cfg"));
                }
                catch (Exception e)
                {
                    Debug.LogError("MechJebModuleScript.LoadConfig caught an exception trying to load mechjeb_settings_script_" + slotName + "_" +
                                   slot + ".cfg: " + e);
                }
            }
            else if (notify)
            {
                setFlashMessage("ERROR: File not found: mechjeb_settings_script_" + slotName + "_" + slot + ".cfg", 1);
            }

            if (node == null) return;

            actionsList.LoadConfig(node);
        }

        public void SaveConfig(int slot, bool notify, bool forceSlotName)
        {
            var node = new ConfigNode("MechJebScriptSettings");

            actionsList.SaveConfig(node);
            string slotName = getSaveSlotName(forceSlotName);
            if (selectedMemorySlotType == 0)
            {
                slotName = "G";
            }

            node.Save(MuUtils.GetCfgPath("mechjeb_settings_script_" + slotName + "_" + slot + ".cfg"));
            if (notify)
            {
                string message_label = Localizer.Format("#MechJeb_ScriptMod_label2"); //"current vessel"
                if (selectedMemorySlotType == 0 && !forceSlotName)
                {
                    message_label = Localizer.Format("#MechJeb_ScriptMod_label3"); //"global memory"
                }

                setFlashMessage("Script saved in slot " + (slot + 1) + " on " + message_label, 0);
            }
        }

        public void DeleteConfig(int slot, bool notify, bool forceSlotName)
        {
            string slotName = getSaveSlotName(forceSlotName);
            File.Delete<MechJebCore>(MuUtils.GetCfgPath("mechjeb_settings_script_" + slotName + "_" + slot + ".cfg"));
            if (notify)
            {
                setFlashMessage("Script deleted on slot " + (slot + 1), 0);
            }
        }

        public bool isStarted()
        {
            return started;
        }

        //Set the savepoint we reached for further load/save
        public void setActiveSavepoint(int activeSavepoint)
        {
            savePointJustSet     = true;
            this.activeSavepoint = activeSavepoint;
            dirty                = true;
        }

        //Start after the action defined at a specified index (for load restore or vessel switch)
        public void startAfterIndex(int index)
        {
            if (index < actionsList.getRecursiveCount())
            {
                List<MechJebModuleScriptAction> list = actionsList.getRecursiveActionsList();
                for (int i = 0; i <= index; i++)
                {
                    list[i].markActionDone();
                }

                start();
            }
        }

        //Set a breakpoint to be able to recover when we switch vessel
        public void setActiveBreakpoint(int index, Vessel new_vessel)
        {
            SaveConfig(9, false, true); //Slot 9 is used for "temp"
            stop();
            actionsList.clearAll();
            List<MechJebCore> mechjebCoresList = new_vessel.FindPartModulesImplementing<MechJebCore>();
            foreach (MechJebCore mjCore in mechjebCoresList)
            {
                mjCore.GetComputerModule<MechJebModuleScript>().minifiedGUI           = minifiedGUI; //Replicate the UI setting on the other mechjeb
                mjCore.GetComputerModule<MechJebModuleScript>().pendingloadBreakpoint = index;
                return; //We only need to update one mechjeb core. Don't know what happens if there are 2 MJ cores on one vessel?
            }
        }

        public void loadFromBreakpoint(int index)
        {
            LoadConfig(9, false, true);   //Slot 9 is used for "temp"
            DeleteConfig(9, false, true); //Delete the temp config
            startAfterIndex(index);
        }

        public void setFlashMessage(string message, int type)
        {
            flashMessage          = message;
            flashMessageType      = type;
            flashMessageStartTime = Time.time;
        }

        public bool checkCompatiblePluginInstalled(string name)
        {
            foreach (string compatiblePlugin in compatiblePluginsInstalled)
            {
                if (compatiblePlugin.CompareTo(name) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        public bool hasCompatiblePluginInstalled()
        {
            if (compatiblePluginsInstalled.Count > 0)
            {
                return true;
            }

            return false;
        }

        public bool isMinifiedGUI()
        {
            return minifiedGUI;
        }

        public bool isAddActionDisabled()
        {
            return addActionDisabled;
        }

        public void setSelectedMemorySlotType(int selectedMemorySlotType)
        {
            this.selectedMemorySlotType = selectedMemorySlotType;
        }
    }
}
