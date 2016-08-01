using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;

namespace MuMech
{
	public class MechJebModuleScript : DisplayModule
	{
		private List<MechJebModuleScriptAction> actionsList = new List<MechJebModuleScriptAction>();
		private String[] actionNames;
		private bool started = false;
		private int selectedActionIndex = 0;
		public Texture2D imageRed = new Texture2D(20, 20);
		public Texture2D imageGreen = new Texture2D(20, 20);
		public Texture2D imageGray = new Texture2D(20, 20);
		private bool minifiedGUI = false;
		private List<String> scriptsList = new List<String>();
		private int selectedSlot = 0;

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
					else {
						imageRed.SetPixel(i, j, Color.red);
						imageGreen.SetPixel(i, j, Color.green);
						imageGray.SetPixel(i, j, Color.gray);
					}
				}
			}
			imageRed.Apply();
			imageGreen.Apply();
			imageGray.Apply();
			scriptsList.Add("Slot 1");
			scriptsList.Add("Slot 2");
			scriptsList.Add("Slot 3");
			scriptsList.Add("Slot 4");
		}

		public void addAction(MechJebModuleScriptAction action)
		{
			this.actionsList.Add(action);
		}

		public void removeAction(MechJebModuleScriptAction action)
		{
			this.actionsList.Remove (action);
		}

		public void moveActionUp(MechJebModuleScriptAction action)
		{
			int index = this.actionsList.IndexOf (action);
			this.actionsList.Remove (action);
			if (index > 0)
			{
				this.actionsList.Insert (index - 1, action);
			}
		}

		public override void OnStart(PartModule.StartState state)
		{
			List<String> actionsNamesList = new List<String> ();
			actionsNamesList.Add ("Timer");
			actionsNamesList.Add ("Decouple");
			actionsNamesList.Add ("Staging");
			actionsNamesList.Add ("Target Dock");
			actionsNamesList.Add ("Target Body");
			actionsNamesList.Add ("Pause");
			actionsNamesList.Add ("Crew Transfer");
			actionsNamesList.Add ("Quicksave");
			actionsNamesList.Add ("RCS");
			actionsNamesList.Add ("Activate Vessel");
			actionsNamesList.Add ("SAS");
			actionsNamesList.Add ("Execute node");
			actionsNamesList.Add ("Manoeuver");
			actionsNamesList.Add ("Warp");
			actionsNamesList.Add ("MODULE Ascent Autopilot");
			actionsNamesList.Add ("MODULE Docking Autopilot");
			actionsNamesList.Add ("MODULE Landing");

			actionNames = actionsNamesList.ToArray ();
		}

		public override void OnModuleEnabled()
		{
		}

		public override void OnModuleDisabled()
		{
		}

		public void nextAction() {
		}

		protected override void WindowGUI(int windowID) {
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			GUIStyle style2 = new GUIStyle(GUI.skin.button);
			if (!started && this.actionsList.Count > 0)
			{
				style2.normal.textColor = Color.green;
				if (GUILayout.Button("START", style2))
				{
					this.start();
				}
			}
			else if (started)
			{
				style2.normal.textColor = Color.red;
				if (GUILayout.Button ("STOP", style2))
				{
					this.stop ();
				}
			}
			if (this.actionsList.Count > 2)
			{
				if (minifiedGUI)
				{
					if (GUILayout.Button("Full GUI"))
					{
						this.minifiedGUI = false;
					}
				}
				else {
					if (GUILayout.Button("Compact GUI"))
					{
						this.minifiedGUI = true;
					}
				}
			}

			GUILayout.EndHorizontal();
			if (!this.minifiedGUI && !this.started)
			{
				GUILayout.BeginHorizontal();
				style2.normal.textColor = Color.white;
				if (GUILayout.Button("Clear All", style2))
				{
					this.clearAll();
				}
				selectedSlot = GuiUtils.ComboBox.Box(selectedSlot, scriptsList.ToArray(), scriptsList);
				if (GUILayout.Button("Save", style2))
				{
					this.SaveConfig(this.selectedSlot);
				}
				if (GUILayout.Button("Load", style2))
				{
					this.LoadConfig(this.selectedSlot);
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Label("Add action");
				selectedActionIndex = GuiUtils.ComboBox.Box(selectedActionIndex, actionNames, this);
				if (actionNames[selectedActionIndex].CompareTo("MODULE Ascent Autopilot") == 0 || actionNames[selectedActionIndex].CompareTo("MODULE Landing") == 0)
				{
					if (GUILayout.Button(GameDatabase.Instance.GetTexture("MechJeb2/Icons/view", true)))
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
						this.addAction(new MechJebModuleScriptActionTimer(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Decouple") == 0)
					{
						this.addAction(new MechJebModuleScriptActionUndock(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Staging") == 0)
					{
						this.addAction(new MechJebModuleScriptActionStaging(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Target Dock") == 0)
					{
						this.addAction(new MechJebModuleScriptActionTargetDock(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Target Body") == 0)
					{
						this.addAction(new MechJebModuleScriptActionTarget(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Pause") == 0)
					{
						this.addAction(new MechJebModuleScriptActionPause(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Crew Transfer") == 0)
					{
						this.addAction(new MechJebModuleScriptActionCrewTransfer(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Quicksave") == 0)
					{
						this.addAction(new MechJebModuleScriptActionQuicksave(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("RCS") == 0)
					{
						this.addAction(new MechJebModuleScriptActionRCS(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Activate Vessel") == 0)
					{
						this.addAction(new MechJebModuleScriptActionActiveVessel(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("SAS") == 0)
					{
						this.addAction(new MechJebModuleScriptActionSAS(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Execute node") == 0)
					{
						this.addAction(new MechJebModuleScriptActionExecuteNode(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Manoeuver") == 0)
					{
						this.addAction(new MechJebModuleScriptActionManoeuver(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("Warp") == 0)
					{
						this.addAction(new MechJebModuleScriptActionWarp(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("MODULE Ascent Autopilot") == 0)
					{
						this.addAction(new MechJebModuleScriptActionAscent(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("MODULE Docking Autopilot") == 0)
					{
						this.addAction(new MechJebModuleScriptActionDockingAutopilot(this, core));
					}
					else if (actionNames[selectedActionIndex].CompareTo("MODULE Landing") == 0)
					{
						this.addAction(new MechJebModuleScriptActionLanding(this, core));
					}
				}
				GUILayout.EndHorizontal();
			}
			foreach (MechJebModuleScriptAction actionItem in actionsList)
			{
				if (!this.minifiedGUI || actionItem.isStarted())
				{
					actionItem.WindowGUI(windowID);
				}
			}
			GUILayout.EndVertical();
			base.WindowGUI(windowID);
		}

		public override GUILayoutOption[] WindowOptions()
		{
			return new GUILayoutOption[] { GUILayout.Width(450), GUILayout.Height(50) };
		}

		public override string GetName()
		{
			return "Scripting Module (Beta)";
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
				actionsList [start_index].activateAction(start_index);
				this.started = true;
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
			if (actionsList.Count > (index + 1))
			{
				actionsList [index+1].activateAction(index+1);
			}
		}

		public void clearAll()
		{
			this.stop();
			actionsList.Clear ();
		}

		public override void OnFixedUpdate()
		{
			for (int i = 0; i < actionsList.Count; i++)
			{
				if (actionsList[i].isStarted() && !actionsList[i].isExecuted())
				{
					actionsList[i].afterOnFixedUpdate();
				}
			}
		}

		public void LoadConfig(int slot)
		{
			this.clearAll();
			ConfigNode node = new ConfigNode("MechJebScriptSettings");
			string vesselName = vessel != null ? string.Join("_", vessel.vesselName.Split(System.IO.Path.GetInvalidFileNameChars())) : ""; // Strip illegal char from the filename
			if ((vessel != null) && File.Exists<MechJebCore>("mechjeb_settings_script_" + vesselName + "_" + slot + ".cfg"))
			{
				try
				{
					node = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + vesselName + "_" + slot + ".cfg"));
				}
				catch (Exception e)
				{
					Debug.LogError("MechJebModuleScript.LoadConfig caught an exception trying to load mechjeb_settings_script_" + vesselName + ".cfg: " + e);
				}
			}
			if (node == null) return;

			//Load custom info scripts, which are stored in our ConfigNode:
			ConfigNode[] scriptNodes = node.GetNodes();
			foreach (ConfigNode scriptNode in scriptNodes)
			{
				MechJebModuleScriptAction obj = null;
				if (scriptNode.name.CompareTo(MechJebModuleScriptActionAscent.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionAscent(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionTimer.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionTimer(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionCrewTransfer.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionCrewTransfer(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionDockingAutopilot.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionDockingAutopilot(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionPause.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionPause(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionStaging.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionStaging(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionTargetDock.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionTargetDock(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionTarget.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionTarget(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionUndock.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionUndock(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionQuicksave.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionQuicksave(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionRCS.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionRCS(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionActiveVessel.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionActiveVessel(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionSAS.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionSAS(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionThrottle.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionThrottle(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionExecuteNode.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionExecuteNode(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionManoeuver.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionManoeuver(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionLanding.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionLanding(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionWarp.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionWarp(this, core);
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

		public void SaveConfig(int slot)
		{
			ConfigNode node = new ConfigNode("MechJebScriptSettings");
			string vesselName = vessel != null ? string.Join("_", vessel.vesselName.Split(System.IO.Path.GetInvalidFileNameChars())) : ""; // Strip illegal char from the filename

			foreach (MechJebModuleScriptAction script in this.actionsList)
			{
				string name = script.getName();
				ConfigNode scriptNode = ConfigNode.CreateConfigFromObject(script, (int)Pass.Type, null);
				script.postSave(scriptNode);
				scriptNode.CopyTo(node.AddNode(name));
			}

			node.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + vesselName + "_" + slot + ".cfg"));

			//TODO : Find a way to notify the user. The popup appears below the main window...
			//PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Save", "Script saved on the current vessel", "OK", true, HighLogic.UISkin);
		}

		public bool isStarted()
		{
			return this.started;
		}
	}
}

