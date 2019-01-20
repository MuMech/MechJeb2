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
		[Persistent(pass = (int)Pass.Type)]
		private int value0unit = 0;
		[Persistent(pass = (int)Pass.Type)]
		private int value1unit = 0;
		private MechJebModuleScript scriptModule;
		private MechJebCore core;
		private MechJebModuleScriptAction action;
		private MechJebModuleInfoItems moduleInfoItems;
		private string[] units0list = {"", "k", "M", "G"};
		private string[] units1list = { "", "k", "M", "G" };
		private double valueWhenConditionCheck = double.NaN;
		private string stringWhenConditionCheck = "N/A";
		private bool conditionVerified = false;

		public MechJebModuleScriptCondition(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptAction action)
		{
			this.scriptModule = scriptModule;
			this.core = core;
			this.action = action;
			moduleInfoItems = core.GetComputerModule<MechJebModuleInfoItems>();
			conditionsList = new List<String>();
			modifiersList = new List<String>();
			conditionsList.Add("Altitude");
			conditionsList.Add("Speed");
			conditionsList.Add("Distance to target");
			conditionsList.Add("Target Apoapsis");
			conditionsList.Add("Target Periapsis");
			conditionsList.Add("Target Time to closest approach");
			conditionsList.Add("Target Distance to closest approach");
			conditionsList.Add("Target Relative velocity");
			conditionsList.Add("Target inclination");
			conditionsList.Add("Target orbit period");
			conditionsList.Add("Target orbit speed");
			conditionsList.Add("Target time to Ap");
			conditionsList.Add("Target time to Pe");
			conditionsList.Add("Target LAN");
			conditionsList.Add("Target AoP");
			conditionsList.Add("Target eccentricity");
			conditionsList.Add("Target SMA");
			conditionsList.Add("Periapsis in Target SOI");
			conditionsList.Add("Phase angle to target");
			conditionsList.Add("Target planet phase angle");
			conditionsList.Add("Relative inclination");
			conditionsList.Add("Time to AN");
			conditionsList.Add("Time to DN");
			conditionsList.Add("Time to equatorial AN");
			conditionsList.Add("Time to equatorial DN");
			conditionsList.Add("Circular orbit speed");
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
				GuiUtils.SimpleTextBox("", value0, "", 100);
				value0unit = GuiUtils.ComboBox.Box(value0unit, units0list, units0list);
				if (selectedModifier == 3)
				{
					GuiUtils.SimpleTextBox("and", value1, "", 100);
					value1unit = GuiUtils.ComboBox.Box(value1unit, units1list, units1list);
				}
			}
			else
			{
				GUILayout.Label("" + value0.val+" "+units0list[value0unit]);
				if (selectedModifier == 3)
				{
					GUILayout.Label(" and " + value1.val+ " " + units1list[value1unit]);
				}
			}
			if (action.isStarted() || action.isExecuted())
			{
				GUIStyle s = new GUIStyle(GUI.skin.label);
				if (this.conditionVerified)
				{
					s.normal.textColor = Color.green;
					GUILayout.Label("(Verified " + this.getStringWhenConditionCheck() + ")", s, GUILayout.ExpandWidth(false));
				}
				else
				{
					s.normal.textColor = Color.red;
					GUILayout.Label("(NOT Verified " + this.getStringWhenConditionCheck() + ")", s, GUILayout.ExpandWidth(false));
				}
			}
		}

		public bool checkCondition()
		{
			double value0ref = value0.val;
			if (this.value0unit == 1)
			{
				value0ref *= 1000;
			}
			else if (this.value0unit == 2)
			{
				value0ref *= 1000000;
			}
			else if (this.value0unit == 3)
			{
				value0ref *= 1000000000;
			}
			double value1ref = value1.val;
			if (this.value1unit == 1)
			{
				value1ref *= 1000;
			}
			else if (this.value1unit == 2)
			{
				value1ref *= 1000000;
			}
			else if (this.value0unit == 3)
			{
				value1ref *= 1000000000;
			}

			this.conditionVerified = false;
			double valueToCompare = getValueToCompare();
			this.valueWhenConditionCheck = valueToCompare;
			this.stringWhenConditionCheck = this.getValueToCompareString();
			if (valueToCompare == double.NaN)
			{
				return false;
			}

			if (valueToCompare < value0ref && selectedModifier == 0)
			{
				this.conditionVerified = true;
				return true;
			}
			else if (valueToCompare == value0ref && selectedModifier == 1)
			{
				this.conditionVerified = true;
				return true;
			}
			else if (valueToCompare > value0ref && selectedModifier == 2)
			{
				this.conditionVerified = true;
				return true;
			}
			else if (valueToCompare > value0ref && valueToCompare < value1ref && selectedModifier == 3)
			{
				this.conditionVerified = true;
				return true;
			}

			return false;
		}

		public double getValueWhenConditionCheck()
		{
			return this.valueWhenConditionCheck;
		}

		public String getStringWhenConditionCheck()
		{
			return this.stringWhenConditionCheck;
		}

		public bool getConditionVerified()
		{
			return this.conditionVerified;
		}

		public string getValueToCompareString()
		{
			if (selectedCondition == 0) //Altitude
			{
				return MuUtils.ToSI(core.vesselState.altitudeBottom, -1, 0);
			}
			else if (selectedCondition == 1) //Speed
			{
				return MuUtils.ToSI(core.vesselState.speedSurface.value, -1, 0);
			}
			else if (selectedCondition == 2) //Distance to target
			{
				return moduleInfoItems.TargetDistance();
			}
			else if (selectedCondition == 3) //Target Apoapsis
			{
				return moduleInfoItems.TargetApoapsis();
			}
			else if (selectedCondition == 4) //Target Periapsis
			{
				return moduleInfoItems.TargetPeriapsis();
			}
			else if (selectedCondition == 5) //Time to closest approach
			{
				return moduleInfoItems.TargetTimeToClosestApproach();
			}
			else if (selectedCondition == 6) //Distance to closest approach
			{
				return moduleInfoItems.TargetClosestApproachDistance();
			}
			else if (selectedCondition == 7) //Target Relative velocity
			{
				return moduleInfoItems.TargetRelativeVelocity();
			}
			else if (selectedCondition == 8) //Target inclination
			{
				return moduleInfoItems.TargetInclination();
			}
			else if (selectedCondition == 9) //Target orbit period
			{
				return moduleInfoItems.TargetOrbitPeriod();
			}
			else if (selectedCondition == 10) //Target orbit speed
			{
				return moduleInfoItems.TargetOrbitSpeed();
			}
			else if (selectedCondition == 11) //Target time to Ap
			{
				return moduleInfoItems.TargetOrbitTimeToAp();
			}
			else if (selectedCondition == 12) //Target time to Pe
			{
				return moduleInfoItems.TargetOrbitTimeToPe();
			}
			else if (selectedCondition == 13) //Target LAN
			{
				return moduleInfoItems.TargetLAN();
			}
			else if (selectedCondition == 14) //Target AoP
			{
				return moduleInfoItems.TargetAoP();
			}
			else if (selectedCondition == 15) //Target eccentricity
			{
				return moduleInfoItems.TargetEccentricity();
			}
			else if (selectedCondition == 16) //Target SMA
			{
				return moduleInfoItems.TargetSMA();
			}
			else if (selectedCondition == 17) //Periapsis in SOI
			{
				return moduleInfoItems.PeriapsisInTargetSOI();
			}
			else if (selectedCondition == 18) //Phase angle to target
			{
				return moduleInfoItems.PhaseAngle();
			}
			else if (selectedCondition == 19) //Target planet phase angle
			{
				return moduleInfoItems.TargetPlanetPhaseAngle();
			}
			else if (selectedCondition == 20) //Relative inclination
			{
				return moduleInfoItems.RelativeInclinationToTarget();
			}
			else if (selectedCondition == 21) //Time to AN
			{
				return moduleInfoItems.TimeToAscendingNodeWithTarget();
			}
			else if (selectedCondition == 22) //Time to DN
			{
				return moduleInfoItems.TimeToDescendingNodeWithTarget();
			}
			else if (selectedCondition == 23) //Time to equatorial AN
			{
				return moduleInfoItems.TimeToEquatorialAscendingNode();
			}
			else if (selectedCondition == 24) //Time to equatorial DN
			{
				return moduleInfoItems.TimeToEquatorialDescendingNode();
			}
			else if (selectedCondition == 25) //Circular orbit speed
			{
				return MuUtils.ToSI(moduleInfoItems.CircularOrbitSpeed());
			}
			return "N/A";
		}

		public double getValueToCompare()
		{
			double valueToCompare = 0;
			if (selectedCondition == 0) //Altitude
			{
				valueToCompare = core.vesselState.altitudeBottom;
			}
			else if (selectedCondition == 1) //Speed
			{
				valueToCompare = core.vesselState.speedSurface.value;
			}
			else if (selectedCondition == 2) //Distance to target
			{
				return TargetDistance();
			}
			else if (selectedCondition == 3) //Target Apoapsis
			{
				return TargetApoapsis();
			}
			else if (selectedCondition == 4) //Target Periapsis
			{
				return TargetPeriapsis();
			}
			else if (selectedCondition == 5) //Time to closest approach
			{
				return TargetTimeToClosestApproach();
			}
			else if (selectedCondition == 6) //Distance to closest approach
			{
				return TargetClosestApproachDistance();
			}
			else if (selectedCondition == 7) //Target Relative velocity
			{
				return TargetRelativeVelocity();
			}
			else if (selectedCondition == 8) //Target inclination
			{
				return TargetInclination();
			}
			else if (selectedCondition == 9) //Target orbit period
			{
				return TargetOrbitPeriod();
			}
			else if (selectedCondition == 10) //Target orbit speed
			{
				return TargetOrbitSpeed();
			}
			else if (selectedCondition == 11) //Target time to Ap
			{
				return TargetOrbitTimeToAp();
			}
			else if (selectedCondition == 12) //Target time to Pe
			{
				return TargetOrbitTimeToPe();
			}
			else if (selectedCondition == 13) //Target LAN
			{
				return TargetLAN();
			}
			else if (selectedCondition == 14) //Target AoP
			{
				return TargetAoP();
			}
			else if (selectedCondition == 15) //Target eccentricity
			{
				return TargetEccentricity();
			}
			else if (selectedCondition == 16) //Target SMA
			{
				return TargetSMA();
			}
			else if (selectedCondition == 17) //Periapsis in SOI
			{
				return PeriapsisInTargetSOI();
			}
			else if (selectedCondition == 18) //Phase angle to target
			{
				return PhaseAngle();
			}
			else if (selectedCondition == 19) //Target planet phase angle
			{
				return TargetPlanetPhaseAngle();
			}
			else if (selectedCondition == 20) //Relative inclination
			{
				return RelativeInclinationToTarget();
			}
			else if (selectedCondition == 21) //Time to AN
			{
				return TimeToAscendingNodeWithTarget();
			}
			else if (selectedCondition == 22) //Time to DN
			{
				return TimeToDescendingNodeWithTarget();
			}
			else if (selectedCondition == 23) //Time to equatorial AN
			{
				return TimeToEquatorialAscendingNode();
			}
			else if (selectedCondition == 24) //Time to equatorial DN
			{
				return TimeToEquatorialDescendingNode();
			}
			else if (selectedCondition == 25) //Circular orbit speed
			{
				return moduleInfoItems.CircularOrbitSpeed();
			}
			return valueToCompare;
		}

		public double TargetDistance()
		{
			if (core.target.Target == null) return double.NaN;
			return core.target.Distance;
		}

		public double TargetApoapsis()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.ApA;
		}

		public double TargetPeriapsis()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.PeA;
		}

		public double TargetTimeToClosestApproach()
		{
			if (core.target.Target != null && core.vesselState.altitudeTrue < 1000.0) { return GuiUtils.FromToETA(core.vessel.CoM, core.target.Transform.position); }
			if (!core.target.NormalTargetExists) return double.NaN;
			if (core.target.TargetOrbit.referenceBody != this.scriptModule.orbit.referenceBody) return double.NaN;
			if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return double.NaN; }
			if (core.vesselState.altitudeTrue < 1000.0)
			{
				double a = (core.vessel.mainBody.transform.position - core.vessel.transform.position).magnitude;
				double b = (core.vessel.mainBody.transform.position - core.target.Transform.position).magnitude;
				double c = Vector3d.Distance(core.vessel.transform.position, core.target.Position);
				double ang = Math.Acos(((a * a + b * b) - c * c) / (double)(2f * a * b));
				return ang * core.vessel.mainBody.Radius / core.vesselState.speedSurfaceHorizontal;
			}
			return this.scriptModule.orbit.NextClosestApproachTime(core.target.TargetOrbit, core.vesselState.time) - core.vesselState.time;
		}

		public double TargetClosestApproachDistance()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			if (core.target.TargetOrbit.referenceBody != this.scriptModule.orbit.referenceBody) return double.NaN;
			if (core.vesselState.altitudeTrue < 1000.0) { return double.NaN; }
			if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return double.NaN; }
			return this.scriptModule.orbit.NextClosestApproachDistance(core.target.TargetOrbit, core.vesselState.time);
		}

		public double PeriapsisInTargetSOI()
		{
			if (!core.target.NormalTargetExists) return double.NaN;

			Orbit o = core.vessel.orbit;
			while (o != null && o.referenceBody != (CelestialBody)core.vessel.targetObject)
				o = o.nextPatch;

			if (o == null) return double.NaN;

			return o.PeA;
		}

		public double TargetRelativeVelocity()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.RelativeVelocity.magnitude;
		}

		public double TargetInclination()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.inclination;
		}

		public double TargetOrbitPeriod()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.period;
		}

		public double TargetOrbitSpeed()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.GetVel().magnitude;
		}

		public double TargetOrbitTimeToAp()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.timeToAp;
		}

		public double TargetOrbitTimeToPe()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.timeToPe;
		}

        public double TargetLAN()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.LAN;
		}

		public double TargetAoP()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.argumentOfPeriapsis;
		}

		public double TargetEccentricity()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.eccentricity;
		}

		public double TargetSMA()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			return core.target.TargetOrbit.semiMajorAxis;
		}

		public double PhaseAngle()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			if (core.target.TargetOrbit.referenceBody != this.scriptModule.orbit.referenceBody) return double.NaN;
			if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return double.NaN; }

			return this.scriptModule.orbit.PhaseAngle(core.target.TargetOrbit, core.vesselState.time);
		}

		public double TargetPlanetPhaseAngle()
		{
			if (!(core.target.Target is CelestialBody)) return double.NaN;
			if (core.target.TargetOrbit.referenceBody != this.scriptModule.orbit.referenceBody.referenceBody) return double.NaN;

			return this.scriptModule.mainBody.orbit.PhaseAngle(core.target.TargetOrbit, core.vesselState.time);
		}

		public double RelativeInclinationToTarget()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			if (core.target.TargetOrbit.referenceBody != this.scriptModule.orbit.referenceBody) return double.NaN;

			return this.scriptModule.orbit.RelativeInclination(core.target.TargetOrbit);
		}

		public double TimeToAscendingNodeWithTarget()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			if (core.target.TargetOrbit.referenceBody != this.scriptModule.orbit.referenceBody) return double.NaN;
			if (!this.scriptModule.orbit.AscendingNodeExists(core.target.TargetOrbit)) return double.NaN;

			return this.scriptModule.orbit.TimeOfAscendingNode(core.target.TargetOrbit, core.vesselState.time) - core.vesselState.time;
		}

		public double TimeToDescendingNodeWithTarget()
		{
			if (!core.target.NormalTargetExists) return double.NaN;
			if (core.target.TargetOrbit.referenceBody != this.scriptModule.orbit.referenceBody) return double.NaN;
			if (!this.scriptModule.orbit.DescendingNodeExists(core.target.TargetOrbit)) return double.NaN;

			return this.scriptModule.orbit.TimeOfDescendingNode(core.target.TargetOrbit, core.vesselState.time) - core.vesselState.time;
		}

		public double TimeToEquatorialAscendingNode()
		{
			if (!this.scriptModule.orbit.AscendingNodeEquatorialExists()) return double.NaN;

			return this.scriptModule.orbit.TimeOfAscendingNodeEquatorial(core.vesselState.time) - core.vesselState.time;
		}

		public double TimeToEquatorialDescendingNode()
		{
			if (!this.scriptModule.orbit.DescendingNodeEquatorialExists()) return double.NaN;

			return this.scriptModule.orbit.TimeOfDescendingNodeEquatorial(core.vesselState.time) - core.vesselState.time;
		}
	}
}

