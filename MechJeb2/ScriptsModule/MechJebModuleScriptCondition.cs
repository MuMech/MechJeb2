using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuMech
{
	public class MechJebModuleScriptCondition
	{
		private List<String> conditionsList;
		private List<String> modifiersList;
		[Persistent(pass = (int)Pass.Type)]
		private int selectedCondition;
		[Persistent(pass = (int)Pass.Type)]
		private int selectedModifier;
		[Persistent(pass = (int)Pass.Type)]
		private EditableDouble value0 = new EditableDouble(0);
		[Persistent(pass = (int)Pass.Type)]
		private EditableDouble value1 = new EditableDouble(0);
		private MechJebModuleScript scriptModule;
		private MechJebCore core;
		private MechJebModuleScriptAction action;

		public MechJebModuleScriptCondition(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptAction action)
		{
			this.scriptModule = scriptModule;
			this.core = core;
			this.action = action;
			conditionsList = new List<String>();
			modifiersList = new List<String>();
			conditionsList.Add("Altitude");
			conditionsList.Add("Speed");
			conditionsList.Add("Distance to target");
			conditionsList.Add("Apoapsis");
			conditionsList.Add("Periapsis");
			modifiersList.Add("Smaller than");
			modifiersList.Add("Equal to");
			modifiersList.Add("Greater than");
			modifiersList.Add("Between");
		}

		public void WindowGUI(int windowID)
		{
			selectedCondition = GuiUtils.ComboBox.Box(selectedCondition, conditionsList.ToArray(), conditionsList);
			selectedModifier = GuiUtils.ComboBox.Box(selectedModifier, modifiersList.ToArray(), modifiersList);
			if (!action.isStarted())
			{
				GuiUtils.SimpleTextBox("", value0, "", 30);
				if (selectedModifier == 3)
				{
					GuiUtils.SimpleTextBox("and", value1, "", 30);
				}
			}
			else
			{
				GUILayout.Label("" + value0);
				if (selectedModifier == 3)
				{
					GUILayout.Label(" and " + value1);
				}
				GUILayout.Label("Current value: ");
				if (selectedCondition == 0) //Altitude
				{
					GUILayout.Label(FlightGlobals.ActiveVessel.mainBody.GetAltitude(FlightGlobals.ActiveVessel.CoM)+"");
				}
				else if (selectedCondition == 1) //Speed
				{
					GUILayout.Label(core.vesselState.speedSurface.value + "");
				}
				else if (selectedCondition == 2) //Distance to target
				{
					if (core.target.Target == null)
					{
						GUILayout.Label("No Target");
					}
					else
					{
						GUILayout.Label(FlightGlobals.ActiveVessel.mainBody.GetAltitude(FlightGlobals.ActiveVessel.CoM) + "");
					}
				}
			}
		}

		public bool checkCondition()
		{
			if (selectedCondition == 0) //Check Altitude
			{
				double current_altitude = FlightGlobals.ActiveVessel.mainBody.GetAltitude(FlightGlobals.ActiveVessel.CoM);
				if (current_altitude < value0 && selectedModifier == 0)
				{
					return true;
				}
				if (current_altitude == value0 && selectedModifier == 1)
				{
					return true;
				}
				if (current_altitude > value0 && selectedModifier == 2)
				{
					return true;
				}
				if (current_altitude > value0 && current_altitude < value1 && selectedModifier == 3)
				{
					return true;
				}
			}
			if (selectedCondition == 0) //Check speed
			{
				double current_speed = core.vesselState.speedSurface.value;
				if (current_speed < value0 && selectedModifier == 0)
				{
					return true;
				}
				if (current_speed == value0 && selectedModifier == 1)
				{
					return true;
				}
				if (current_speed > value0 && selectedModifier == 2)
				{
					return true;
				}
				if (current_speed > value0 && current_speed < value1 && selectedModifier == 3)
				{
					return true;
				}
			}
			if (selectedCondition == 1) //Check distance to target
			{
				if (core.target.Target == null) return false;
				double current_distance = core.target.Distance;
				if (current_distance < value0 && selectedModifier == 0)
				{
					return true;
				}
				if (current_distance == value0 && selectedModifier == 1)
				{
					return true;
				}
				if (current_distance > value0 && selectedModifier == 2)
				{
					return true;
				}
				if (current_distance > value0 && current_distance < value1 && selectedModifier == 3)
				{
					return true;
				}
			}
			return false;
		}
	}
}

