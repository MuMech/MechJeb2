using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO;
using KSP.Localization;

namespace MuMech
{
	public class MechJebModuleScript : DisplayModule, IMechJebModuleScriptActionsListParent
	{
		private bool started = false;
		private MechJebModuleScriptActionsList actionsList;
		public Texture2D imageRed = new Texture2D(20, 20);
		public Texture2D imageGreen = new Texture2D(20, 20);
		public Texture2D imageGray = new Texture2D(20, 20);
		[Persistent(pass = (int)(Pass.Local))]
		private bool minifiedGUI = false;
		private List<String> scriptsList = new List<String>();
		private List<String> memorySlotsList = new List<String>();
		[Persistent(pass = (int)(Pass.Type))]
		private String[] scriptNames = {"","","","","","","",""};
		[Persistent(pass = (int)(Pass.Type))]
		private String[] globalScriptNames = { "", "", "", "", "", "", "", "" };
		[Persistent(pass = (int)(Pass.Local))]
		private int selectedSlot = 0;
		[Persistent(pass = (int)(Pass.Local))]
		private int selectedMemorySlotType = 0;
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
		private bool deployScriptNameField = false;
		//Flash message to notify user
		private String flashMessage = "";
		private int flashMessageType = 0; //0=yellow, 1=red (error)
		private float flashMessageStartTime = 0f;
		private bool waitingDeletionConfirmation = false;
		private List<String> compatiblePluginsInstalled = new List<String>();
		private bool addActionDisabled = false;
		private int old_selectedMemorySlotType;

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
			this.memorySlotsList.Clear();
			this.memorySlotsList.Add("Global Memory");
			this.memorySlotsList.Add("Vessel Memory");
			//Init main actions list
			this.actionsList = new MechJebModuleScriptActionsList(core, this, this, 0);
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
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if (assembly.FullName.Contains("InfernalRobotics"))
				{
					this.compatiblePluginsInstalled.Add("IRSequencer");
				}
				else if (assembly.FullName.Contains("kOS"))
				{
					this.compatiblePluginsInstalled.Add("kOS");
				}
			}
			//Populate Actions names. Need to run this after the compatibility check with other plugins
			this.actionsList.refreshActionNamesPlugins();

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
					vesselSaveName = vessel != null ? string.Join("_", vessel.vesselName.Split(System.IO.Path.GetInvalidFileNameChars())) : null; // Strip illegal char from the filename
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
			this.LoadScriptModuleConfig();
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
            if (global != null)
			    this.SaveScriptModuleConfig();
		}

		public override void OnLoad(ConfigNode local, ConfigNode type, ConfigNode global)
		{
			base.OnLoad(local, type, global);
            this.LoadScriptModuleConfig();
		}

		public void SaveScriptModuleConfig()
		{
			string slotName = this.getSaveSlotName(false);
			ConfigNode node = ConfigNode.CreateConfigFromObject(this, (int)Pass.Type, null);
			node.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + slotName + "_conf.cfg"));
		}

		public void LoadScriptModuleConfig()
		{
			string slotName = this.getSaveSlotName(false);
			if (File.Exists<MechJebCore>("mechjeb_settings_script_" + slotName + "_conf.cfg"))
			{
				ConfigNode node = null;
				try
				{
					node = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + slotName + "_conf.cfg"));
				}
				catch (Exception e)
				{
					Debug.LogError("MechJebModuleScript.LoadConfig caught an exception trying to load mechjeb_settings_script_" + slotName + "_conf.cfg: " + e);
				}
				if (node == null) return;

				ConfigNode.LoadObjectFromConfig(this, node);
			}
			this.updateScriptsNames();
		}

		protected override void WindowGUI(int windowID) {
			GUILayout.BeginVertical();
			if (this.warmingUp)
			{
				GUILayout.Label(Localizer.Format("#MechJeb_ScriptMod_label1", this.spendTime));//"Warming up. Please wait... <<1>> s
			}
			else
			{
				GUILayout.BeginHorizontal();
				GUIStyle style2 = new GUIStyle(GUI.skin.button);
				if (!started && this.actionsList.getActionsCount() > 0)
				{
					style2.normal.textColor = Color.green;
					if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button1"), style2))//"▶ START"
					{
						this.start();
					}
					style2.normal.textColor = Color.white;
					if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button2"), style2))//"☇ Reset"
					{
						this.actionsList.recursiveResetStatus();
					}
					this.addActionDisabled = GUILayout.Toggle(this.addActionDisabled, Localizer.Format("#MechJeb_ScriptMod_checkbox1"));//"Hide Add Actions"
				}
				else if (started)
				{
					style2.normal.textColor = Color.red;
					if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button3"), style2))//"■ STOP"
					{
						this.stop();
					}
				}
				if (this.actionsList.getActionsCount() > 0)
				{
					if (minifiedGUI)
					{
						if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button4")))//"▼ Full GUI"
						{
							this.minifiedGUI = false;
						}
					}
					else {
						if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button5")))//"△ Compact GUI"
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
					if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button6"), style2))//"Clear All"
					{
						this.actionsList.clearAll();
					}
					selectedMemorySlotType = GuiUtils.ComboBox.Box(selectedMemorySlotType, memorySlotsList.ToArray(), memorySlotsList);
					if (selectedMemorySlotType != old_selectedMemorySlotType)
					{
						old_selectedMemorySlotType = selectedMemorySlotType;
						this.updateScriptsNames();
					}
					selectedSlot = GuiUtils.ComboBox.Box(selectedSlot, scriptsList.ToArray(), scriptsList);
					if (deployScriptNameField)
					{
						if (this.selectedMemorySlotType == 1)
						{
							scriptNames[selectedSlot] = GUILayout.TextField(scriptNames[selectedSlot], GUILayout.Width(120), GUILayout.ExpandWidth(false));
						}
						else
						{
							globalScriptNames[selectedSlot] = GUILayout.TextField(globalScriptNames[selectedSlot], GUILayout.Width(120), GUILayout.ExpandWidth(false));
						}
						if (scriptNames[selectedSlot].Length > 20)//Limit the script name to 20 chars
						{
							if (this.selectedMemorySlotType == 1)
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
							this.deployScriptNameField = false;
							this.updateScriptsNames();
							this.SaveScriptModuleConfig();
						}
					}
					else
					{
						if (GUILayout.Button(">>", GUILayout.ExpandWidth(false)))
						{
							this.deployScriptNameField = true;
						}
					}
					if (GUILayout.Button("✖", new GUILayoutOption[] { GUILayout.Width(20), GUILayout.Height(20) }))
					{
						if (!this.waitingDeletionConfirmation)
						{
							this.waitingDeletionConfirmation = true;
							this.setFlashMessage("Warning: To confirm deletion of slot " + (selectedSlot+1) + " - " + scriptNames[selectedSlot] + ", press again the delete button", 0);
						}
						else
						{
							this.DeleteConfig(this.selectedSlot, true, false);
							if (this.selectedMemorySlotType == 1)
							{
								scriptNames[selectedSlot] = "";
							}
							else
							{
								globalScriptNames[selectedSlot] = "";
							}
							this.updateScriptsNames();
							this.SaveScriptModuleConfig();
						}
					}

					if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button7"), style2))//"Save"
					{
						this.SaveConfig(this.selectedSlot, true, false);
					}
					if (GUILayout.Button(Localizer.Format("#MechJeb_ScriptMod_button8"), style2))//"Load"
					{
						this.LoadConfig(this.selectedSlot, true, false);
					}
					GUILayout.EndHorizontal();
					if (this.flashMessage.Length > 0)
					{
						GUILayout.BeginHorizontal();
						GUIStyle sflash = new GUIStyle(GUI.skin.label);
						if (this.flashMessageType == 1)
						{
							sflash.normal.textColor = Color.red;
						}
						else
						{
							sflash.normal.textColor = Color.yellow;
						}
						GUILayout.Label(this.flashMessage, sflash);
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
			return new GUILayoutOption[] { GUILayout.Width(800), GUILayout.Height(50) };
		}

		public override string GetName()
		{
			return Localizer.Format("#MechJeb_ScriptMod_title");//"Scripting Module"
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
				this.started = true;
			}
		}

		public void stop()
		{
			this.started = false;
			//Clean abord the current action
			actionsList.stop();
		}

		public void notifyEndActionsList()
		{
			this.setActiveSavepoint(-1);//Reset save point to prevent a manual quicksave to open the previous savepoint
			this.stop();
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
							this.LoadConfig(this.selectedSlot, false, false);
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

			actionsList.OnFixedUpdate(); //List action update

			//Check if we need to close the flashMessage
			if (this.flashMessageStartTime > 0f)
			{
				float flashSpendTime = (int)(Math.Round(Time.time - this.flashMessageStartTime));
				if (flashSpendTime > 5f)
				{
					this.flashMessage = "";
					this.flashMessageStartTime = 0f;
					this.waitingDeletionConfirmation = false;
				}
			}
		}

		public string getSaveSlotName(bool forceSlotName)
		{
			string slotName = this.vesselSaveName;
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
				this.selectedSlot = slot; //Select the slot for the UI. Except slot 9 (temp)
			}
			string slotName = this.getSaveSlotName(forceSlotName);
			ConfigNode node = new ConfigNode("MechJebScriptSettings");
			if (File.Exists<MechJebCore>("mechjeb_settings_script_" + slotName + "_" + slot + ".cfg"))
			{
				try
				{
					node = ConfigNode.Load(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + slotName + "_" + slot + ".cfg"));
				}
				catch (Exception e)
				{
					Debug.LogError("MechJebModuleScript.LoadConfig caught an exception trying to load mechjeb_settings_script_" + slotName + "_" + slot + ".cfg: " + e);
				}
			}
			else if (notify)
			{
				this.setFlashMessage("ERROR: File not found: mechjeb_settings_script_" + slotName + "_" + slot + ".cfg", 1);
			}
			if (node == null) return;

			actionsList.LoadConfig(node);
		}

		public void SaveConfig(int slot, bool notify, bool forceSlotName)
		{
			ConfigNode node = new ConfigNode("MechJebScriptSettings");

			actionsList.SaveConfig(node);
			string slotName = this.getSaveSlotName(forceSlotName);
			if (selectedMemorySlotType == 0)
			{
				slotName = "G";
			}
			node.Save(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + slotName + "_" + slot + ".cfg"));
			if (notify)
			{
				string message_label = Localizer.Format("#MechJeb_ScriptMod_label2");//"current vessel"
				if (selectedMemorySlotType == 0 && !forceSlotName)
				{
					message_label = Localizer.Format("#MechJeb_ScriptMod_label3");//"global memory"
				}
				this.setFlashMessage("Script saved in slot " + (slot + 1) + " on " + message_label, 0);
			}
		}

		public void DeleteConfig(int slot, bool notify, bool forceSlotName)
		{
			string slotName = this.getSaveSlotName(forceSlotName);
			File.Delete<MechJebCore>(IOUtils.GetFilePathFor(this.GetType(), "mechjeb_settings_script_" + slotName + "_" + slot + ".cfg"));
			if (notify)
			{
				this.setFlashMessage("Script deleted on slot " + (slot+1), 0);
			}
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
            dirty = true;
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
				this.start();
			}
		}

		//Set a breakpoint to be able to recover when we switch vessel
		public void setActiveBreakpoint(int index, Vessel new_vessel)
		{
			this.SaveConfig(9, false, true); //Slot 9 is used for "temp"
			this.stop();
			this.actionsList.clearAll();
			List<MechJebCore> mechjebCoresList = new_vessel.FindPartModulesImplementing<MechJebCore>();
			foreach (MechJebCore mjCore in mechjebCoresList)
			{
				mjCore.GetComputerModule<MechJebModuleScript>().minifiedGUI = this.minifiedGUI; //Replicate the UI setting on the other mechjeb
				mjCore.GetComputerModule<MechJebModuleScript>().pendingloadBreakpoint = index;
				return; //We only need to update one mechjeb core. Don't know what happens if there are 2 MJ cores on one vessel?
			}
		}

		public void loadFromBreakpoint(int index)
		{
			this.LoadConfig(9, false, true); //Slot 9 is used for "temp"
			this.DeleteConfig(9, false, true); //Delete the temp config
			this.startAfterIndex(index);
		}

		public void setFlashMessage(String message, int type)
		{
			this.flashMessage = message;
			this.flashMessageType = type;
			this.flashMessageStartTime = Time.time;
		}

		public bool checkCompatiblePluginInstalled(String name)
		{
			foreach (String compatiblePlugin in this.compatiblePluginsInstalled)
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
			if (this.compatiblePluginsInstalled.Count > 0)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		public bool isMinifiedGUI()
		{
			return this.minifiedGUI;
		}

		public bool isAddActionDisabled()
		{
			return this.addActionDisabled;
		}

		public void setSelectedMemorySlotType(int selectedMemorySlotType)
		{
			this.selectedMemorySlotType = selectedMemorySlotType;
		}
	}
}