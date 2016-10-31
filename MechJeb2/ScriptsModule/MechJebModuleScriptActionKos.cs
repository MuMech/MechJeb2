using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptActionKos : MechJebModuleScriptAction
	{
		public static String NAME = "kOS";
		private List<Part> kosParts = new List<Part>();
		private List<String> kosPartsNames = new List<String>();
		private List<PartModule> kosModules = new List<PartModule>();
		[Persistent(pass = (int)Pass.Type)]
		private EditableInt selectedPartIndex = 0;
		[Persistent(pass = (int)Pass.Type)]
		private uint selectedPartFlightID = 0;
		[Persistent(pass = (int)Pass.Type)]
		private String command = "";
		[Persistent(pass = (int)Pass.Type)]
		private bool openTerminal = true;
		private bool partHighlighted = false;

		public MechJebModuleScriptActionKos (MechJebModuleScript scriptModule, MechJebCore core):base(scriptModule, core, NAME)
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
								this.kosParts.Add(part);
								this.kosPartsNames.Add(part.partInfo.title);
								this.kosModules.Add(module);
							}
						}
					}
				}
			}
		}

		override public void activateAction(int actionIndex)
		{
			base.activateAction(actionIndex);
			if (this.selectedPartIndex < this.kosModules.Count)
			{
				if (openTerminal)
				{
					this.kosModules[this.selectedPartIndex].GetType().InvokeMember("OpenWindow", System.Reflection.BindingFlags.InvokeMethod, null, this.kosModules[this.selectedPartIndex], null);
				}
				var sharedObjects = this.kosModules[this.selectedPartIndex].GetType().GetField("shared", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(this.kosModules[this.selectedPartIndex]);
				if (sharedObjects != null)
				{
					var interpreter = sharedObjects.GetType().GetProperty("Interpreter").GetValue(sharedObjects, null);
					if (interpreter != null)
					{
						interpreter.GetType().InvokeMember("ProcessCommand", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, interpreter, new object[] { command });
					}
					else
					{
						Debug.LogError("---- NO Interpreter OBJECT ----");
					}
				}
				else
				{
					Debug.LogError("---- NO SHARED OBJECT ----");
				}
			}
			this.endAction();
		}

		override public  void endAction()
		{
			base.endAction();
		}

		override public void afterOnFixedUpdate()
		{
			
		}

		override public void WindowGUI(int windowID)
		{
			base.preWindowGUI(windowID);
			base.WindowGUI(windowID);
			GUILayout.Label ("kOS");
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
				command = GUILayout.TextField(command, GUILayout.Width(120), GUILayout.ExpandWidth(true));
				openTerminal = GUILayout.Toggle(openTerminal, "Open Terminal");
			}
			else
			{
				GUILayout.Label("-- NO kOS module on vessel --");
			}
			if (selectedPartIndex < kosParts.Count)
			{
				this.selectedPartFlightID = kosParts[selectedPartIndex].flightID;
			}

			base.postWindowGUI(windowID);
		}

		override public void postLoad(ConfigNode node)
		{
			if (selectedPartFlightID != 0) //We check if a previous flightID was set on the parts. When switching MechJeb Cores and performing save/load of the script, the port order may change so we try to rely on the flight ID to select the right part.
			{
				int i = 0;
				foreach (Part part in kosParts)
				{
					if (part.flightID == selectedPartFlightID)
					{
						this.selectedPartIndex = i;
					}
					i++;
				}
			}
		}
	}
}

