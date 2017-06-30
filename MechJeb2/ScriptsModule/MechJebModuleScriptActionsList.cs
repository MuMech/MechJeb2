﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionsList
	{
		private List<MechJebModuleScriptAction> actionsList = new List<MechJebModuleScriptAction>();
		private String[] actionGroups;
		private String[][] actionNames;
		private MechJebModuleScript scriptModule;
		private int selectedGroupIndex = 0;
		private int old_selectedGroupIndex = 0;
		private int selectedActionIndex = 0;
		private bool started = false;
		private bool executed = false;
		private IMechJebModuleScriptActionsListParent parent;
		private int depth = 0;
		private MechJebCore core;
		private int currentIndex;

		public MechJebModuleScriptActionsList(MechJebCore core, MechJebModuleScript scriptModule, IMechJebModuleScriptActionsListParent parent, int depth)
		{
			this.core = core;
			this.scriptModule = scriptModule;
			this.parent = parent;
			this.depth = depth;
			this.populateActionNames();
		}

		public int getDepth()
		{
			return this.depth;
		}

		public void populateActionNames()
		{
			List<String> actionsGroupsList = new List<String>();
			actionsGroupsList.Add("Time");
			actionsGroupsList.Add("Docking");
			actionsGroupsList.Add("Target");
			actionsGroupsList.Add("Control");
			actionsGroupsList.Add("Crew");
			actionsGroupsList.Add("Trajectory");
			actionsGroupsList.Add("Staging/Engines");
			actionsGroupsList.Add("Settings");
			actionsGroupsList.Add("Modules");
			actionsGroupsList.Add("Save/Load/Actions");
			actionsGroupsList.Add("PROGRAM Logic");
			actionsGroupsList.Add("Plugins");
			actionGroups = actionsGroupsList.ToArray();

			actionNames = new String[actionsGroupsList.Count][];

			//Time
			List<String> actionsNamesList = new List<String>();
			actionsNamesList.Add("Timer");
			actionsNamesList.Add("Pause");
			actionsNamesList.Add("Wait for");
			actionsNamesList.Add("Warp");
			actionNames[0] = actionsNamesList.ToArray();

			//Docking
			actionsNamesList = new List<String>();
			actionsNamesList.Add("Decouple");
			actionsNamesList.Add("Dock Shield");
			actionsNamesList.Add("Target Dock");
			actionNames[1] = actionsNamesList.ToArray();

			//Target
			actionsNamesList = new List<String>();
			actionsNamesList.Add("Target Dock");
			actionsNamesList.Add("Target Body");
			actionNames[2] = actionsNamesList.ToArray();

			//Control
			actionsNamesList = new List<String>();
			actionsNamesList.Add("Control From");
			actionsNamesList.Add("RCS");
			actionsNamesList.Add("SAS");
			if (depth == 0)
			{
				actionsNamesList.Add("Switch Vessel"); //Switch Vessel only available at depth 0 because it can change the focus.
			}
			actionNames[3] = actionsNamesList.ToArray();

			//Crew
			actionsNamesList = new List<String>();
			actionsNamesList.Add("Crew Transfer");
			actionNames[4] = actionsNamesList.ToArray();

			//Trajectory
			actionsNamesList = new List<String>();
			actionsNamesList.Add("Maneuver");
			actionsNamesList.Add("Execute node");
			actionNames[5] = actionsNamesList.ToArray();

			//Staging
			actionsNamesList = new List<String>();
			actionsNamesList.Add("Staging");
			actionsNamesList.Add("Activate Engine");
			actionNames[6] = actionsNamesList.ToArray();

			//Settings
			actionsNamesList = new List<String>();
			actionsNamesList.Add("Node tolerance");
			actionNames[7] = actionsNamesList.ToArray();

			//Modules
			actionsNamesList = new List<String>();
            actionsNamesList.Add("MODULE Smart A.S.S.");
            actionsNamesList.Add("MODULE Ascent Autopilot");
			actionsNamesList.Add("MODULE Docking Autopilot");
			actionsNamesList.Add("MODULE Landing");
			actionsNamesList.Add("MODULE Rendezvous");
			actionsNamesList.Add("MODULE Rendezvous Autopilot");
			actionNames[8] = actionsNamesList.ToArray();

			//Save
			actionsNamesList = new List<String>();
			if (depth == 0) //Actions only available at depth 0 because they can change focus
			{
				actionsNamesList.Add("Quicksave");
				actionsNamesList.Add("Load Script");
			}
			actionsNamesList.Add("Action Group");
			actionNames[9] = actionsNamesList.ToArray();

			//PROGRAM Logic
			actionsNamesList = new List<String>();
			if (depth < 4) //Limit program depth to 4 (just to avoid UI mess)
			{
				actionsNamesList.Add("PROGRAM - Repeat");
				actionsNamesList.Add("PROGRAM - If");
				actionsNamesList.Add("PROGRAM - While");
				if (depth < 2) //Limit parallel depth to 2
				{
					actionsNamesList.Add("PROGRAM - Parallel");
				}
			}
			actionsNamesList.Add("Wait for");
			actionNames[10] = actionsNamesList.ToArray();

			//Plugins
			this.refreshActionNamesPlugins();
		}

		public void refreshActionNamesPlugins()
		{
			//Check plugins compatibility
			List<String> actionsNamesList = new List<String>();
			if (scriptModule.hasCompatiblePluginInstalled())
			{
				actionsNamesList = new List<String>();
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
			this.actionsList.Add(action);
		}

		public void removeAction(MechJebModuleScriptAction action)
		{
			this.actionsList.Remove(action);
		}

		public void moveActionUp(MechJebModuleScriptAction action)
		{
			int index = this.actionsList.IndexOf(action);
			this.actionsList.Remove(action);
			if (index > 0)
			{
				this.actionsList.Insert(index - 1, action);
			}
		}

		public void actionsAddWindowGui(int windowID)
		{
			GUIStyle s = new GUIStyle(GUI.skin.label);
			s.normal.textColor = Color.blue;
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.Label("Add action", s, GUILayout.ExpandWidth(false));
			selectedGroupIndex = GuiUtils.ComboBox.Box(selectedGroupIndex, actionGroups, actionGroups);
			if (selectedGroupIndex != old_selectedGroupIndex)
			{
				selectedActionIndex = 0;//Reset action index
				old_selectedGroupIndex = selectedGroupIndex;
			}
			if (selectedGroupIndex == 10 && depth >= 4) //Block more than 4 depth
			{
				GUIStyle s2 = new GUIStyle(GUI.skin.label);
				s2.normal.textColor = Color.red;
				GUILayout.Label("Program depth is limited to 4", s2, GUILayout.ExpandWidth(false));
			}
			else
			{
				selectedActionIndex = GuiUtils.ComboBox.Box(selectedActionIndex, actionNames[selectedGroupIndex], actionNames);
				if (actionNames[selectedGroupIndex][selectedActionIndex].CompareTo("MODULE Ascent Autopilot") == 0 || actionNames[selectedGroupIndex][selectedActionIndex].CompareTo("MODULE Landing") == 0)
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
					{
						if (actionNames[selectedGroupIndex][selectedActionIndex].CompareTo("MODULE Ascent Autopilot") == 0)
						{
							//Open the ascent module GUI
							core.GetComputerModule<MechJebModuleAscentGuidance>().enabled = true;
						}
						if (actionNames[selectedGroupIndex][selectedActionIndex].CompareTo("MODULE Landing") == 0)
						{
							//Open the DockingGuidance module GUI
							core.GetComputerModule<MechJebModuleLandingGuidance>().enabled = true;
						}
					}
				}

				if (GUILayout.Button("+", GUILayout.ExpandWidth(false)))
				{
					String actionName = actionNames[selectedGroupIndex][selectedActionIndex];
					if (actionName.CompareTo("Timer") == 0)
					{
						this.addAction(new MechJebModuleScriptActionTimer(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Decouple") == 0)
					{
						this.addAction(new MechJebModuleScriptActionUndock(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Dock Shield") == 0)
					{
						this.addAction(new MechJebModuleScriptActionDockingShield(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Staging") == 0)
					{
						this.addAction(new MechJebModuleScriptActionStaging(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Target Dock") == 0)
					{
						this.addAction(new MechJebModuleScriptActionTargetDock(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Target Body") == 0)
					{
						this.addAction(new MechJebModuleScriptActionTarget(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Control From") == 0)
					{
						this.addAction(new MechJebModuleScriptActionControlFrom(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Pause") == 0)
					{
						this.addAction(new MechJebModuleScriptActionPause(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Crew Transfer") == 0)
					{
						this.addAction(new MechJebModuleScriptActionCrewTransfer(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Quicksave") == 0)
					{
						this.addAction(new MechJebModuleScriptActionQuicksave(scriptModule, core, this));
					}
					else if (actionName.CompareTo("RCS") == 0)
					{
						this.addAction(new MechJebModuleScriptActionRCS(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Switch Vessel") == 0)
					{
						this.addAction(new MechJebModuleScriptActionActiveVessel(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Activate Engine") == 0)
					{
						this.addAction(new MechJebModuleScriptActionActivateEngine(scriptModule, core, this));
					}
					else if (actionName.CompareTo("SAS") == 0)
					{
						this.addAction(new MechJebModuleScriptActionSAS(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Execute node") == 0)
					{
						this.addAction(new MechJebModuleScriptActionExecuteNode(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Maneuver") == 0)
					{
						this.addAction(new MechJebModuleScriptActionManoeuver(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Node tolerance") == 0)
					{
						this.addAction(new MechJebModuleScriptActionTolerance(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Warp") == 0)
					{
						this.addAction(new MechJebModuleScriptActionWarp(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Wait for") == 0)
					{
						this.addAction(new MechJebModuleScriptActionWaitFor(scriptModule, core, this));
					}
					else if (actionName.CompareTo("PROGRAM - Repeat") == 0)
					{
						if (this.checkMaxDepth())
						{
							this.addAction(new MechJebModuleScriptActionFor(scriptModule, core, this));
						}
					}
					else if (actionName.CompareTo("PROGRAM - If") == 0)
					{
						if (this.checkMaxDepth())
						{
							this.addAction(new MechJebModuleScriptActionIf(scriptModule, core, this));
						}
					}
					else if (actionName.CompareTo("PROGRAM - While") == 0)
					{
						if (this.checkMaxDepth())
						{
							this.addAction(new MechJebModuleScriptActionWhile(scriptModule, core, this));
						}
					}
					else if (actionName.CompareTo("PROGRAM - Parallel") == 0)
					{
						if (this.checkMaxDepth())
						{
							this.addAction(new MechJebModuleScriptActionParallel(scriptModule, core, this));
						}
					}
					else if (actionName.CompareTo("Action Group") == 0)
					{
						this.addAction(new MechJebModuleScriptActionActionGroup(scriptModule, core, this));
					}
					else if (actionName.CompareTo("Load Script") == 0)
					{
						this.addAction(new MechJebModuleScriptActionLoadScript(scriptModule, core, this));
					}
                    else if (actionName.CompareTo("MODULE Smart A.S.S.") == 0)
					{
                        this.addAction(new MechJebModuleScriptActionSmartASS(scriptModule, core, this));
					}
					else if (actionName.CompareTo("MODULE Ascent Autopilot") == 0)
					{
						this.addAction(new MechJebModuleScriptActionAscent(scriptModule, core, this));
					}
					else if (actionName.CompareTo("MODULE Docking Autopilot") == 0)
					{
						this.addAction(new MechJebModuleScriptActionDockingAutopilot(scriptModule, core, this));
					}
					else if (actionName.CompareTo("MODULE Landing") == 0)
					{
						this.addAction(new MechJebModuleScriptActionLanding(scriptModule, core, this));
					}
					else if (actionName.CompareTo("MODULE Rendezvous") == 0)
					{
						this.addAction(new MechJebModuleScriptActionRendezvous(scriptModule, core, this));
					}
					else if (actionName.CompareTo("MODULE Rendezvous Autopilot") == 0)
					{
						this.addAction(new MechJebModuleScriptActionRendezvousAP(scriptModule, core, this));
					}
					else if (actionName.CompareTo("[IR Sequencer] Sequence") == 0)
					{
						this.addAction(new MechJebModuleScriptActionIRSequencer(scriptModule, core, this));
					}
					else if (actionName.CompareTo("[kOS] Command") == 0)
					{
						this.addAction(new MechJebModuleScriptActionKos(scriptModule, core, this));
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
			if (!this.scriptModule.isStarted() && !this.scriptModule.isAddActionDisabled())
			{
				this.actionsAddWindowGui(windowID); //Render Add action
			}
		}

		public void start()
		{
			this.currentIndex = 0;
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
				this.started = true;
				this.currentIndex = start_index;
				actionsList[start_index].activateAction();
			}
			else
			{
				this.setExecuted();
			}

		}

		public void stop()
		{
			this.started = false;
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
			return this.started;
		}

		public bool isExecuted()
		{
			return this.executed;
		}

		public void notifyEndAction()
		{
			this.currentIndex++;
			if (this.currentIndex < actionsList.Count && this.started)
			{
				actionsList[this.currentIndex].activateAction();
			}
			else
			{
				this.setExecuted();
			}
		}

		public void setExecuted()
		{
			this.started = false;
			this.executed = true;
			this.parent.notifyEndActionsList();
		}

		public void clearAll()
		{
			this.stop();
			actionsList.Clear();
		}

		public int getActionsCount()
		{
			return this.actionsList.Count;
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
			this.clearAll();
			//Load custom info scripts, which are stored in our ConfigNode:
			ConfigNode[] scriptNodes = node.GetNodes();
			foreach (ConfigNode scriptNode in scriptNodes)
			{
				MechJebModuleScriptAction obj = null;
                if (scriptNode.name.CompareTo(MechJebModuleScriptActionSmartASS.NAME) == 0)
                {
                    obj = new MechJebModuleScriptActionSmartASS(scriptModule, core, this);
                }
                else if(scriptNode.name.CompareTo(MechJebModuleScriptActionAscent.NAME) == 0)
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
				else {
					Debug.LogError("MechJebModuleScript.LoadConfig : Unknown node " + scriptNode.name);
				}
				if (obj != null)
				{
					ConfigNode.LoadObjectFromConfig(obj, scriptNode);
					obj.postLoad(scriptNode);
					this.addAction(obj);
				}
			}
		}

		public void SaveConfig(ConfigNode node)
		{
			foreach (MechJebModuleScriptAction script in this.actionsList)
			{
				string name = script.getName();
				ConfigNode scriptNode = ConfigNode.CreateConfigFromObject(script, (int)Pass.Type, null);
				script.postSave(scriptNode);
				scriptNode.CopyTo(node.AddNode(name));
			}
		}

		public bool checkMaxDepth()
		{
			if (this.getDepth() >= 4)
			{
				this.scriptModule.setFlashMessage("Program depth is limited to 4", 0);
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
			List<MechJebModuleScriptAction> list = new List<MechJebModuleScriptAction>();
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
			List<MechJebModuleScriptAction> actions = this.getRecursiveActionsList();
			foreach (MechJebModuleScriptAction action in actions)
			{
				action.resetStatus();
			}
		}

		public void recursiveUpdateActionsIndex(int startIndex)
		{
			List<MechJebModuleScriptAction> actions = this.getRecursiveActionsList();
			int index = startIndex;
			foreach (MechJebModuleScriptAction action in actions)
			{
				action.setActionIndex(index);
				index++;
			}
		}
	}
}