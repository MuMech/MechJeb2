using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionsList
	{
		private List<MechJebModuleScriptAction> actionsList = new List<MechJebModuleScriptAction>();
		private static String[] actionNames;
		private MechJebModuleScript scriptModule;
		private int selectedActionIndex = 0;
		private bool started = false;
		private IMechJebModuleScriptActionsListParent parent;
		private int depth = 0;
		private MechJebCore core;

		public MechJebModuleScriptActionsList(MechJebCore core, MechJebModuleScript scriptModule, IMechJebModuleScriptActionsListParent parent, int depth)
		{
			this.core = core;
			this.scriptModule = scriptModule;
			this.parent = parent;
			this.depth = depth;
		}

		public int getDepth()
		{
			return this.depth;
		}

		public static void populateActionNames(MechJebModuleScript scriptModule)
		{
			List<String> actionsNamesList = new List<String>();
			actionsNamesList.Add("Timer");
			actionsNamesList.Add("Decouple");
			actionsNamesList.Add("Dock Shield");
			actionsNamesList.Add("Staging");
			actionsNamesList.Add("Target Dock");
			actionsNamesList.Add("Target Body");
			actionsNamesList.Add("Control From");
			actionsNamesList.Add("Pause");
			actionsNamesList.Add("Crew Transfer");
			actionsNamesList.Add("Quicksave");
			actionsNamesList.Add("RCS");
			actionsNamesList.Add("Switch Vessel");
			actionsNamesList.Add("Activate Engine");
			actionsNamesList.Add("SAS");
			actionsNamesList.Add("Maneuver");
			actionsNamesList.Add("Execute node");
			actionsNamesList.Add("Action Group");
			actionsNamesList.Add("Node tolerance");
			actionsNamesList.Add("Warp");
			actionsNamesList.Add("Wait for");
			actionsNamesList.Add("CONTROL - Repeat");
			actionsNamesList.Add("CONTROL - If");
			actionsNamesList.Add("CONTROL - While");
			actionsNamesList.Add("Load Script");
			actionsNamesList.Add("MODULE Ascent Autopilot");
			actionsNamesList.Add("MODULE Docking Autopilot");
			actionsNamesList.Add("MODULE Landing");
			actionsNamesList.Add("MODULE Rendezvous");
			actionsNamesList.Add("MODULE Rendezvous Autopilot");
			if (scriptModule.checkCompatiblePluginInstalled("IRSequencer"))
			{
				actionsNamesList.Add("[IR Sequencer] Sequence");
			}
			if (scriptModule.checkCompatiblePluginInstalled("kOS"))
			{
				actionsNamesList.Add("[kOS] Command");
			}
			actionNames = actionsNamesList.ToArray();
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
			GUILayout.Label("Add action", s);
			selectedActionIndex = GuiUtils.ComboBox.Box(selectedActionIndex, actionNames, this);
			if (actionNames[selectedActionIndex].CompareTo("MODULE Ascent Autopilot") == 0 || actionNames[selectedActionIndex].CompareTo("MODULE Landing") == 0)
			{
				if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true), GUILayout.ExpandWidth(false)))
				{
					if (actionNames[selectedActionIndex].CompareTo("MODULE Ascent Autopilot") == 0)
					{
						//Open the ascent module GUI
						core.GetComputerModule<MechJebModuleAscentGuidance>().enabled = true;
					}
					if (actionNames[selectedActionIndex].CompareTo("MODULE Landing") == 0)
					{
						//Open the DockingGuidance module GUI
						core.GetComputerModule<MechJebModuleLandingGuidance>().enabled = true;
					}
				}
			}

			if (GUILayout.Button("Add"))
			{
				if (actionNames[selectedActionIndex].CompareTo("Timer") == 0)
				{
					this.addAction(new MechJebModuleScriptActionTimer(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Decouple") == 0)
				{
					this.addAction(new MechJebModuleScriptActionUndock(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Dock Shield") == 0)
				{
					this.addAction(new MechJebModuleScriptActionDockingShield(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Staging") == 0)
				{
					this.addAction(new MechJebModuleScriptActionStaging(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Target Dock") == 0)
				{
					this.addAction(new MechJebModuleScriptActionTargetDock(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Target Body") == 0)
				{
					this.addAction(new MechJebModuleScriptActionTarget(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Control From") == 0)
				{
					this.addAction(new MechJebModuleScriptActionControlFrom(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Pause") == 0)
				{
					this.addAction(new MechJebModuleScriptActionPause(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Crew Transfer") == 0)
				{
					this.addAction(new MechJebModuleScriptActionCrewTransfer(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Quicksave") == 0)
				{
					this.addAction(new MechJebModuleScriptActionQuicksave(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("RCS") == 0)
				{
					this.addAction(new MechJebModuleScriptActionRCS(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Switch Vessel") == 0)
				{
					this.addAction(new MechJebModuleScriptActionActiveVessel(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Activate Engine") == 0)
				{
					this.addAction(new MechJebModuleScriptActionActivateEngine(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("SAS") == 0)
				{
					this.addAction(new MechJebModuleScriptActionSAS(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Execute node") == 0)
				{
					this.addAction(new MechJebModuleScriptActionExecuteNode(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Maneuver") == 0)
				{
					this.addAction(new MechJebModuleScriptActionManoeuver(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Node tolerance") == 0)
				{
					this.addAction(new MechJebModuleScriptActionTolerance(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Warp") == 0)
				{
					this.addAction(new MechJebModuleScriptActionWarp(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Wait for") == 0)
				{
					this.addAction(new MechJebModuleScriptActionWaitFor(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("CONTROL - Repeat") == 0)
				{
					this.addAction(new MechJebModuleScriptActionFor(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("CONTROL - If") == 0)
				{
					this.addAction(new MechJebModuleScriptActionIf(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("CONTROL - While") == 0)
				{
					this.addAction(new MechJebModuleScriptActionWhile(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Action Group") == 0)
				{
					this.addAction(new MechJebModuleScriptActionActionGroup(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("Load Script") == 0)
				{
					this.addAction(new MechJebModuleScriptActionLoadScript(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("MODULE Ascent Autopilot") == 0)
				{
					this.addAction(new MechJebModuleScriptActionAscent(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("MODULE Docking Autopilot") == 0)
				{
					this.addAction(new MechJebModuleScriptActionDockingAutopilot(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("MODULE Landing") == 0)
				{
					this.addAction(new MechJebModuleScriptActionLanding(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("MODULE Rendezvous") == 0)
				{
					this.addAction(new MechJebModuleScriptActionRendezvous(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("MODULE Rendezvous Autopilot") == 0)
				{
					this.addAction(new MechJebModuleScriptActionRendezvousAP(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("[IR Sequencer] Sequence") == 0)
				{
					this.addAction(new MechJebModuleScriptActionIRSequencer(scriptModule, core, this));
				}
				else if (actionNames[selectedActionIndex].CompareTo("[kOS] Command") == 0)
				{
					this.addAction(new MechJebModuleScriptActionKos(scriptModule, core, this));
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
			if (!this.scriptModule.isStarted())
			{
				this.actionsAddWindowGui(windowID); //Render Add action
			}
		}

		public void start()
		{
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
				actionsList[start_index].activateAction(start_index);
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

		public void notifyEndAction(int index)
		{
			if (actionsList.Count > (index + 1) && this.started)
			{
				actionsList[index + 1].activateAction(index + 1);
			}
			else
			{
				this.parent.notifyEndActionsList();
			}
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
				if (scriptNode.name.CompareTo(MechJebModuleScriptActionAscent.NAME) == 0)
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
	}
}