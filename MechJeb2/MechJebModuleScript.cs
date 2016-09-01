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
		private int currentActionIndex = 0;
		public Texture2D imageRed = new Texture2D(20, 20);
		public Texture2D imageGreen = new Texture2D(20, 20);
		public Texture2D imageGray = new Texture2D(20, 20);
		[Persistent(pass = (int)(Pass.Local))]
		private bool minifiedGUI = false;
		private List<String> scriptsList = new List<String>();
		[Persistent(pass = (int)(Pass.Local))]
		private int selectedSlot = 0;
		private List<MechJebCore> mechJebCores = new List<MechJebCore>();
		private List<String> mechJebCoresName = new List<String>();
		private EditableInt selectedMechJebCore = new EditableInt(0);
		[Persistent(pass = (int)(Pass.Local))]
		public String vesselSaveName;
		[Persistent(pass = (int)(Pass.Local))]
		private int activeSavepoint = -1;
		public int pendingloadBreakpoint = -1;
		private bool moduleStarted = false;
		private bool savePointJustSet = false;
		//Warmup time for restoring after load
		private bool warmingUp = false;
		private int spendTime = 0;
		private int initTime = 5; //Add a 5s warmup time
		private float startTime = 0f;

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
			Debug.Log("------ Start core -----");
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
			actionsNamesList.Add ("Manoeuver");
			actionsNamesList.Add ("Execute node");
			actionsNamesList.Add ("Action Group");
			actionsNamesList.Add ("Node tolerance");
			actionsNamesList.Add ("Warp");
			actionsNamesList.Add ("Wait for");
			actionsNamesList.Add ("MODULE Ascent Autopilot");
			actionsNamesList.Add ("MODULE Docking Autopilot");
			actionsNamesList.Add ("MODULE Landing");

			actionNames = actionsNamesList.ToArray ();

			//Don't know why sometimes this value can be "empty" but not null, causing an empty vessel name...
			if (vesselSaveName != null)
			{
				if (vesselSaveName.Length == 0)
				{
					vesselSaveName = null;
				}
			}

			if (vesselSaveName == null)
			{
				//Try to have only one vessel name, whatever the new vessel name. We use the vessel name of the first time the system was instanciated
				//Can cause problem with load/save...
				vesselSaveName = vessel != null ? string.Join("_", vessel.vesselName.Split(System.IO.Path.GetInvalidFileNameChars())) : null; // Strip illegal char from the filename
			}
			else
			{
				Debug.Log("VESSEL SAVE NAME ALREADY SET :" + vesselSaveName);
			}

			//MechJebCore instances
			List<MechJebCore> mechjebCoresList = vessel.FindPartModulesImplementing<MechJebCore>();
			if (mechjebCoresList.Count > 1)
			{
				int position = 0;
				foreach (MechJebCore mjCore in mechjebCoresList)
				{
					mechJebCores.Add(mjCore);
					mechJebCoresName.Add("Core " + position); //TODO: Find a more relevant name...
					position++;
					if (mjCore.GetComputerModule<MechJebModuleScript>().vesselSaveName == null)
					{
						mjCore.GetComputerModule<MechJebModuleScript>().vesselSaveName = vesselSaveName; //Set the unique vessel name
					}
				}
			}
			this.moduleStarted = true;
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
		}

		public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
		{
			base.OnLoad(local, type, global);
		}

		protected override void WindowGUI(int windowID) {
			GUILayout.BeginVertical();
			if (this.warmingUp)
			{
				GUILayout.Label("Warming up. Please wait... " + this.spendTime + "s");
			}
			else
			{
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
					if (GUILayout.Button("STOP", style2))
					{
						this.stop();
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
					//Allow to select core when there are multiple cores
					/*if (mechJebCoresName.Count > 1)
					{
						selectedMechJebCore = GuiUtils.ComboBox.Box(selectedMechJebCore, mechJebCoresName.ToArray(), mechJebCoresName);
					}*/
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
						else if (actionNames[selectedActionIndex].CompareTo("Node tolerance") == 0)
						{
							this.addAction(new MechJebModuleScriptActionTolerance(this, core));
						}
						else if (actionNames[selectedActionIndex].CompareTo("Warp") == 0)
						{
							this.addAction(new MechJebModuleScriptActionWarp(this, core));
						}
						else if (actionNames[selectedActionIndex].CompareTo("Wait for") == 0)
						{
							this.addAction(new MechJebModuleScriptActionWaitFor(this, core));
						}
						else if (actionNames[selectedActionIndex].CompareTo("Action Group") == 0)
						{
							this.addAction(new MechJebModuleScriptActionActionGroup(this, core));
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
				this.started = true;
				actionsList [start_index].activateAction(start_index);
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
				this.currentActionIndex = index + 1;
			}
			else
			{
				this.stop();
			}
		}

		public void clearAll()
		{
			this.stop();
			actionsList.Clear ();
		}

		public override void OnFixedUpdate()
		{
			//Check if we are restoring after a load
			if (HighLogic.LoadedSceneIsFlight)
			{
				if (this.activeSavepoint >= 0 && this.moduleStarted && !this.savePointJustSet) //We have a pending active savepoint
				{
					//Warmup time for restoring after load
					if (startTime > 0)
					{
						spendTime = initTime - (int)(Math.Round(Time.time - startTime));
						if (spendTime <= 0)
						{
							this.warmingUp = false;
							this.startTime = 0;
							this.LoadConfig(this.selectedSlot);
							int asp = this.activeSavepoint;
							this.activeSavepoint = -1;
							this.startAfterIndex(asp);
						}
					}
					else
					{
						startTime = Time.time;
						this.warmingUp = true;
					}
				}
				if (this.pendingloadBreakpoint >= 0 && this.moduleStarted)
				{
					//Warmup time for restoring after switch
					if (startTime > 0)
					{
						spendTime = initTime - (int)(Math.Round(Time.time - startTime));
						if (spendTime <= 0)
						{
							this.warmingUp = false;
							this.startTime = 0;
							int breakpoint = this.pendingloadBreakpoint;
							this.pendingloadBreakpoint = -1;
							this.loadFromBreakpoint(breakpoint);
						}
					}
					else
					{
						startTime = Time.time;
						this.warmingUp = true;
					}
				}
			}

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
			//string vesselName = vessel != null ? string.Join("_", vessel.vesselName.Split(System.IO.Path.GetInvalidFileNameChars())) : ""; // Strip illegal char from the filename
			if ((vessel != null) && File.Exists<MechJebCore>("mechjeb_settings_script_" + vesselSaveName + "_" + slot + ".cfg"))
			{
				try
				{
					node = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + vesselSaveName + "_" + slot + ".cfg"));
				}
				catch (Exception e)
				{
					Debug.LogError("MechJebModuleScript.LoadConfig caught an exception trying to load mechjeb_settings_script_" + vesselSaveName + "_" + slot + ".cfg: " + e);
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
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionTolerance.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionTolerance(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionWaitFor.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionWaitFor(this, core);
				}
				else if (scriptNode.name.CompareTo(MechJebModuleScriptActionActionGroup.NAME) == 0)
				{
					obj = new MechJebModuleScriptActionActionGroup(this, core);
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
			//string vesselName = vessel != null ? string.Join("_", vessel.vesselName.Split(System.IO.Path.GetInvalidFileNameChars())) : ""; // Strip illegal char from the filename

			foreach (MechJebModuleScriptAction script in this.actionsList)
			{
				string name = script.getName();
				ConfigNode scriptNode = ConfigNode.CreateConfigFromObject(script, (int)Pass.Type, null);
				script.postSave(scriptNode);
				scriptNode.CopyTo(node.AddNode(name));
			}

			node.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + vesselSaveName + "_" + slot + ".cfg"));

			//TODO : Find a way to notify the user. The popup appears below the main window...
			//PopupDialog.SpawnPopupDialog(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), "Save", "Script saved on the current vessel", "OK", true, HighLogic.UISkin);
		}

		public void DeleteConfig(int slot)
		{
			File.Delete<MechJebCore>(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + vesselSaveName + "_" + slot + ".cfg"));
		}

		public bool isStarted()
		{
			return this.started;
		}

		//Set the savepoint we reached for further load/save
		public void setActiveSavepoint(int activeSavepoint)
		{
			this.savePointJustSet = true;
			this.activeSavepoint = activeSavepoint;
		}

		//Start after the action defined at a specified index (for load restore or vessel switch)
		public void startAfterIndex(int index)
		{
			Debug.Log("Start after index " + index);
			if (index < actionsList.Count)
			{
				for (int i = 0; i <= index; i++)
				{
					actionsList[i].markActionDone();
				}
				this.start();
			}
		}

		//Set a breakpoint to be able to recover when we switch vessel
		//Before switching vessel, we save the script, then in the OnLoad method of the script module, we check if any other mechjebcore has an active pending breakpoint
		public void setActiveBreakpoint(int index, Vessel new_vessel)
		{
			this.SaveConfig(9); //Slot 9 is used for "temp";
			this.stop();
			List<MechJebCore> mechjebCoresList = new_vessel.FindPartModulesImplementing<MechJebCore>();
			foreach (MechJebCore mjCore in mechjebCoresList)
			{
				mjCore.GetComputerModule<MechJebModuleScript>().pendingloadBreakpoint = index;
				return;
			}
		}

		public void loadFromBreakpoint(int index)
		{
			this.LoadConfig(9); //Slot 9 is used for "temp";
			this.DeleteConfig(9); //Delete the temp config
			this.startAfterIndex(index);
		}
	}
}

