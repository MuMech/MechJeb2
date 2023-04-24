using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public class MechJebModuleScriptActionsList
    {
        private readonly List<MechJebModuleScriptAction>       actionsList = new List<MechJebModuleScriptAction>();
        private          string[]                              actionGroups;
        private          string[][]                            actionNames;
        private readonly MechJebModuleScript                   scriptModule;
        private          int                                   selectedGroupIndex;
        private          int                                   old_selectedGroupIndex;
        private          int                                   selectedActionIndex;
        private          bool                                  started;
        private          bool                                  executed;
        private readonly IMechJebModuleScriptActionsListParent parent;
        private readonly int                                   depth;
        private readonly MechJebCore                           core;
        private          int                                   currentIndex;

        public MechJebModuleScriptActionsList(MechJebCore core, MechJebModuleScript scriptModule, IMechJebModuleScriptActionsListParent parent,
            int depth)
        {
            this.core         = core;
            this.scriptModule = scriptModule;
            this.parent       = parent;
            this.depth        = depth;
            populateActionNames();
        }

        public int getDepth()
        {
            return depth;
        }

        public void populateActionNames()
        {
            var actionsGroupsList = new List<string>();
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions1"));  //"Time"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions2"));  //"Docking"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions3"));  //"Target"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions4"));  //"Control"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions5"));  //"Crew"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions6"));  //"Trajectory"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions7"));  //"Staging/Engines"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions8"));  //"Settings"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions9"));  //"Modules"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions10")); //"Save/Load/Actions"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions11")); //"PROGRAM Logic"
            actionsGroupsList.Add(Localizer.Format("#MechJeb_ScriptMod_actions12")); //"Plugins"
            actionGroups = actionsGroupsList.ToArray();

            actionNames = new string[actionsGroupsList.Count][];

            //Time
            var actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions1_1")); //"Timer"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions1_2")); //"Pause"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions1_3")); //"Wait for"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions1_4")); //"Warp"
            actionNames[0] = actionsNamesList.ToArray();

            //Docking
            actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions2_1")); //"Decouple"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions2_2")); //"Dock Shield"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions2_3")); //"Target Dock"
            actionNames[1] = actionsNamesList.ToArray();

            //Target
            actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions3_1")); //"Target Dock"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions3_2")); //"Target Body"
            actionNames[2] = actionsNamesList.ToArray();

            //Control
            actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions4_1")); //"Control From"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions4_2")); //"RCS"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions4_3")); //"SAS"
            if (depth == 0)
            {
                actionsNamesList.Add(
                    Localizer.Format(
                        "#MechJeb_ScriptMod_actions4_4")); //Switch Vessel only available at depth 0 because it can change the focus."Switch Vessel"
            }

            actionNames[3] = actionsNamesList.ToArray();

            //Crew
            actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions5_1")); //"Crew Transfer"
            actionNames[4] = actionsNamesList.ToArray();

            //Trajectory
            actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions6_1")); //"Maneuver"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions6_2")); //"Execute node"
            actionNames[5] = actionsNamesList.ToArray();

            //Staging
            actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions7_1")); //"Staging"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions7_2")); //"Activate Engine"
            actionNames[6] = actionsNamesList.ToArray();

            //Settings
            actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions8_1")); //"Node tolerance"
            actionNames[7] = actionsNamesList.ToArray();

            //Modules
            actionsNamesList = new List<string>();
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions9_1")); //"MODULE Smart A.S.S."
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions9_2")); //"MODULE Ascent Autopilot"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions9_3")); //"MODULE Docking Autopilot"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions9_4")); //"MODULE Landing"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions9_5")); //"MODULE Rendezvous"
            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions9_6")); //"MODULE Rendezvous Autopilot"
            actionNames[8] = actionsNamesList.ToArray();

            //Save
            actionsNamesList = new List<string>();
            if (depth == 0) //Actions only available at depth 0 because they can change focus
            {
                actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions10_1")); //"Quicksave"
                actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions10_2")); //"Load Script"
            }

            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions10_3")); //"Action Group"
            actionNames[9] = actionsNamesList.ToArray();

            //PROGRAM Logic
            actionsNamesList = new List<string>();
            if (depth < 4) //Limit program depth to 4 (just to avoid UI mess)
            {
                actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions11_1")); //"PROGRAM - Repeat"
                actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions11_2")); //"PROGRAM - If"
                actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions11_3")); //"PROGRAM - While"
                if (depth < 2)                                                            //Limit parallel depth to 2
                {
                    actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions11_4")); //"PROGRAM - Parallel"
                }
            }

            actionsNamesList.Add(Localizer.Format("#MechJeb_ScriptMod_actions11_5")); //"Wait for"
            actionNames[10] = actionsNamesList.ToArray();

            //Plugins
            refreshActionNamesPlugins();
        }

        public void refreshActionNamesPlugins()
        {
            //Check plugins compatibility
            var actionsNamesList = new List<string>();
            if (scriptModule.hasCompatiblePluginInstalled())
            {
                actionsNamesList = new List<string>();
                if (scriptModule.checkCompatiblePluginInstalled("IRSequencer"))
                {
                    actionsNamesList.Add("[IR Sequencer] Sequence");
                }

                if (scriptModule.checkCompatiblePluginInstalled("kOS"))
                {
                    actionsNamesList.Add("[kOS] Command");
                }

                actionNames[11] = actionsNamesList.ToArray();
            }
        }

        public void addAction(MechJebModuleScriptAction action)
        {
            actionsList.Add(action);
        }

        public void removeAction(MechJebModuleScriptAction action)
        {
            actionsList.Remove(action);
        }

        public void moveActionUp(MechJebModuleScriptAction action)
        {
            int index = actionsList.IndexOf(action);
            actionsList.Remove(action);
            if (index > 0)
            {
                actionsList.Insert(index - 1, action);
            }
        }

        public void actionsAddWindowGui(int windowID)
        {
            var s = new GUIStyle(GUI.skin.label);
            s.normal.textColor = Color.blue;
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.Label(Localizer.Format("#MechJeb_ScriptMod_label4"), s, GUILayout.ExpandWidth(false)); //"Add action"
            selectedGroupIndex = GuiUtils.ComboBox.Box(selectedGroupIndex, actionGroups, actionGroups);
            if (selectedGroupIndex != old_selectedGroupIndex)
            {
                selectedActionIndex    = 0; //Reset action index
                old_selectedGroupIndex = selectedGroupIndex;
            }

            if (selectedGroupIndex == 10 && depth >= 4) //Block more than 4 depth
            {
                GUILayout.Label(Localizer.Format("#MechJeb_ScriptMod_label5"), GuiUtils.redLabel,
                    GUILayout.ExpandWidth(false)); //"Program depth is limited to 4"
            }
            else
            {
                selectedActionIndex = GuiUtils.ComboBox.Box(selectedActionIndex, actionNames[selectedGroupIndex], actionNames);
                if (actionNames[selectedGroupIndex][selectedActionIndex].CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_2")) == 0 ||
                    actionNames[selectedGroupIndex][selectedActionIndex].CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_4")) ==
                    0) // "MODULE Ascent Autopilot"||"MODULE Landing"
                {
                    if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
                    {
                        if (actionNames[selectedGroupIndex][selectedActionIndex].CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_2")) ==
                            0) // "MODULE Ascent Autopilot"
                        {
                            //Open the ascent module GUI
                            core.GetComputerModule<MechJebModuleAscentMenu>().enabled = true;
                        }

                        if (actionNames[selectedGroupIndex][selectedActionIndex].CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_4")) ==
                            0) // "MODULE Landing"
                        {
                            //Open the DockingGuidance module GUI
                            core.GetComputerModule<MechJebModuleLandingGuidance>().enabled = true;
                        }
                    }
                }

                if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
                {
                    string actionName = actionNames[selectedGroupIndex][selectedActionIndex];
                    if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions1_1")) == 0) //"Timer"
                    {
                        addAction(new MechJebModuleScriptActionTimer(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions2_1")) == 0) //"Decouple"
                    {
                        addAction(new MechJebModuleScriptActionUndock(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions2_2")) == 0) //"Dock Shield"
                    {
                        addAction(new MechJebModuleScriptActionDockingShield(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions7_1")) == 0) //"Staging"
                    {
                        addAction(new MechJebModuleScriptActionStaging(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions3_1")) == 0) //"Target Dock"
                    {
                        addAction(new MechJebModuleScriptActionTargetDock(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions3_2")) == 0) //"Target Body"
                    {
                        addAction(new MechJebModuleScriptActionTarget(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions4_1")) == 0) //"Control From"
                    {
                        addAction(new MechJebModuleScriptActionControlFrom(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions1_2")) == 0) //"Pause"
                    {
                        addAction(new MechJebModuleScriptActionPause(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions5_1")) == 0) //"Crew Transfer"
                    {
                        addAction(new MechJebModuleScriptActionCrewTransfer(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions10_1")) == 0) //"Quicksave"
                    {
                        addAction(new MechJebModuleScriptActionQuicksave(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions4_2")) == 0) //"RCS"
                    {
                        addAction(new MechJebModuleScriptActionRCS(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions4_4")) == 0) //"Switch Vessel"
                    {
                        addAction(new MechJebModuleScriptActionActiveVessel(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions7_2")) == 0) //"Activate Engine"
                    {
                        addAction(new MechJebModuleScriptActionActivateEngine(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions4_3")) == 0) //"SAS"
                    {
                        addAction(new MechJebModuleScriptActionSAS(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions6_2")) == 0) //"Execute node"
                    {
                        addAction(new MechJebModuleScriptActionExecuteNode(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions6_1")) == 0) //"Maneuver"
                    {
                        addAction(new MechJebModuleScriptActionManoeuver(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions8_1")) == 0) //"Node tolerance"
                    {
                        addAction(new MechJebModuleScriptActionTolerance(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions1_4")) == 0) //"Warp"
                    {
                        addAction(new MechJebModuleScriptActionWarp(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions1_3")) == 0) //"Wait for"
                    {
                        addAction(new MechJebModuleScriptActionWaitFor(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions11_1")) == 0) //"PROGRAM - Repeat"
                    {
                        if (checkMaxDepth())
                        {
                            addAction(new MechJebModuleScriptActionFor(scriptModule, core, this));
                        }
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions11_2")) == 0) //"PROGRAM - If"
                    {
                        if (checkMaxDepth())
                        {
                            addAction(new MechJebModuleScriptActionIf(scriptModule, core, this));
                        }
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions11_3")) == 0) //"PROGRAM - While"
                    {
                        if (checkMaxDepth())
                        {
                            addAction(new MechJebModuleScriptActionWhile(scriptModule, core, this));
                        }
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions11_4")) == 0) //"PROGRAM - Parallel"
                    {
                        if (checkMaxDepth())
                        {
                            addAction(new MechJebModuleScriptActionParallel(scriptModule, core, this));
                        }
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions10_3")) == 0) //"Action Group"
                    {
                        addAction(new MechJebModuleScriptActionActionGroup(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions10_2")) == 0) //"Load Script"
                    {
                        addAction(new MechJebModuleScriptActionLoadScript(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_1")) == 0) //"MODULE Smart A.S.S."
                    {
                        addAction(new MechJebModuleScriptActionSmartASS(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_2")) == 0) //"MODULE Ascent Autopilot"
                    {
                        addAction(new MechJebModuleScriptActionAscent(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_3")) == 0) //"MODULE Docking Autopilot"
                    {
                        addAction(new MechJebModuleScriptActionDockingAutopilot(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_4")) == 0) //"MODULE Landing"
                    {
                        addAction(new MechJebModuleScriptActionLanding(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_5")) == 0) //"MODULE Rendezvous"
                    {
                        addAction(new MechJebModuleScriptActionRendezvous(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo(Localizer.Format("#MechJeb_ScriptMod_actions9_6")) == 0) //"MODULE Rendezvous Autopilot"
                    {
                        addAction(new MechJebModuleScriptActionRendezvousAP(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo("[IR Sequencer] Sequence") == 0) //
                    {
                        addAction(new MechJebModuleScriptActionIRSequencer(scriptModule, core, this));
                    }
                    else if (actionName.CompareTo("[kOS] Command") == 0) //
                    {
                        addAction(new MechJebModuleScriptActionKos(scriptModule, core, this));
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        public void actionsWindowGui(int windowID)
        {
            for (int i = 0; i < actionsList.Count; i++) //Don't use "foreach" here to avoid nullpointer exception
            {
                MechJebModuleScriptAction actionItem = actionsList[i];
                if (!scriptModule.isMinifiedGUI() || actionItem.isStarted())
                {
                    actionItem.WindowGUI(windowID);
                }
            }

            if (!scriptModule.isStarted() && !scriptModule.isAddActionDisabled())
            {
                actionsAddWindowGui(windowID); //Render Add action
            }
        }

        public void start()
        {
            currentIndex = 0;
            if (actionsList.Count > 0)
            {
                //Find the first not executed action
                int start_index = 0;
                for (int i = 0; i < actionsList.Count; i++)
                {
                    if (!actionsList[i].isExecuted())
                    {
                        start_index = i;
                        break;
                    }
                }

                started      = true;
                currentIndex = start_index;
                actionsList[start_index].activateAction();
            }
            else
            {
                setExecuted();
            }
        }

        public void stop()
        {
            started = false;
            //Clean abord the current action
            for (int i = 0; i < actionsList.Count; i++)
            {
                if (actionsList[i].isStarted() && !actionsList[i].isExecuted())
                {
                    actionsList[i].onAbord();
                }
            }
        }

        public bool isStarted()
        {
            return started;
        }

        public bool isExecuted()
        {
            return executed;
        }

        public void notifyEndAction()
        {
            currentIndex++;
            if (currentIndex < actionsList.Count && started)
            {
                actionsList[currentIndex].activateAction();
            }
            else
            {
                setExecuted();
            }
        }

        public void setExecuted()
        {
            started  = false;
            executed = true;
            parent.notifyEndActionsList();
        }

        public void clearAll()
        {
            stop();
            actionsList.Clear();
        }

        public int getActionsCount()
        {
            return actionsList.Count;
        }

        public void OnFixedUpdate()
        {
            for (int i = 0; i < actionsList.Count; i++)
            {
                if (actionsList[i].isStarted() && !actionsList[i].isExecuted())
                {
                    actionsList[i].afterOnFixedUpdate();
                }
            }
        }

        public void LoadConfig(ConfigNode node)
        {
            clearAll();
            //Load custom info scripts, which are stored in our ConfigNode:
            ConfigNode[] scriptNodes = node.GetNodes();
            foreach (ConfigNode scriptNode in scriptNodes)
            {
                MechJebModuleScriptAction obj = null;
                if (scriptNode.name.CompareTo(MechJebModuleScriptActionSmartASS.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionSmartASS(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionAscent.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionAscent(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionTimer.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionTimer(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionCrewTransfer.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionCrewTransfer(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionDockingAutopilot.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionDockingAutopilot(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionPause.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionPause(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionStaging.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionStaging(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionTargetDock.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionTargetDock(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionTarget.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionTarget(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionControlFrom.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionControlFrom(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionUndock.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionUndock(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionDockingShield.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionDockingShield(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionQuicksave.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionQuicksave(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionRCS.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionRCS(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionActiveVessel.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionActiveVessel(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionActivateEngine.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionActivateEngine(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionSAS.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionSAS(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionThrottle.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionThrottle(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionExecuteNode.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionExecuteNode(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionManoeuver.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionManoeuver(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionLanding.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionLanding(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionWarp.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionWarp(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionTolerance.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionTolerance(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionWaitFor.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionWaitFor(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionFor.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionFor(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionIf.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionIf(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionWhile.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionWhile(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionParallel.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionParallel(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionActionGroup.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionActionGroup(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionLoadScript.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionLoadScript(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionRendezvous.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionRendezvous(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionRendezvousAP.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionRendezvousAP(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionIRSequencer.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionIRSequencer(scriptModule, core, this);
                }
                else if (scriptNode.name.CompareTo(MechJebModuleScriptActionKos.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionKos(scriptModule, core, this);
                }
                else
                {
                    Debug.LogError("MechJebModuleScript.LoadConfig : Unknown node " + scriptNode.name);
                }

                if (obj != null)
                {
                    ConfigNode.LoadObjectFromConfig(obj, scriptNode);
                    obj.postLoad(scriptNode);
                    addAction(obj);
                }
            }
        }

        public void SaveConfig(ConfigNode node)
        {
            foreach (MechJebModuleScriptAction script in actionsList)
            {
                string name = script.getName();
                var scriptNode = ConfigNode.CreateConfigFromObject(script, (int)Pass.Type, null);
                script.postSave(scriptNode);
                scriptNode.CopyTo(node.AddNode(name));
            }
        }

        public bool checkMaxDepth()
        {
            if (getDepth() >= 4)
            {
                scriptModule.setFlashMessage("Program depth is limited to 4", 0);
                return false;
            }

            return true;
        }

        //Return the list + sub-lists count
        public int getRecursiveCount()
        {
            int count = 0;
            for (int i = 0; i < actionsList.Count; i++)
            {
                count++;
                if (actionsList[i] is IMechJebModuleScriptActionContainer)
                {
                    count += ((IMechJebModuleScriptActionContainer)actionsList[i]).getRecursiveCount();
                }
            }

            return count;
        }

        public List<MechJebModuleScriptAction> getRecursiveActionsList()
        {
            var list = new List<MechJebModuleScriptAction>();
            for (int i = 0; i < actionsList.Count; i++)
            {
                list.Add(actionsList[i]);
                if (actionsList[i] is IMechJebModuleScriptActionContainer)
                {
                    list.AddRange(((IMechJebModuleScriptActionContainer)actionsList[i]).getRecursiveActionsList());
                }
            }

            return list;
        }

        public void recursiveResetStatus()
        {
            List<MechJebModuleScriptAction> actions = getRecursiveActionsList();
            foreach (MechJebModuleScriptAction action in actions)
            {
                action.resetStatus();
            }
        }

        public void recursiveUpdateActionsIndex(int startIndex)
        {
            List<MechJebModuleScriptAction> actions = getRecursiveActionsList();
            int index = startIndex;
            foreach (MechJebModuleScriptAction action in actions)
            {
                action.setActionIndex(index);
                index++;
            }
        }
    }
}
