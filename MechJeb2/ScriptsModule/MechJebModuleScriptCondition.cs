using System;
using System.Collections.Generic;
using UnityEngine;
using static MechJebLib.Utils.Statics;

namespace MuMech
{
    public class MechJebModuleScriptCondition
    {
        private readonly List<string> conditionsList;
        private readonly List<string> modifiersList;

        [Persistent(pass = (int)Pass.Type)]
        private int selectedCondition;

        [Persistent(pass = (int)Pass.Type)]
        private int selectedModifier;

        [Persistent(pass = (int)Pass.Type)]
        private readonly EditableDouble value0 = new EditableDouble(0);

        [Persistent(pass = (int)Pass.Type)]
        private readonly EditableDouble value1 = new EditableDouble(0);

        [Persistent(pass = (int)Pass.Type)]
        private int value0unit;

        [Persistent(pass = (int)Pass.Type)]
        private int value1unit;

        private readonly MechJebModuleScript       scriptModule;
        private readonly MechJebCore               core;
        private readonly MechJebModuleScriptAction action;
        private readonly MechJebModuleInfoItems    moduleInfoItems;
        private readonly string[]                  units0list               = { "", "k", "M", "G" };
        private readonly string[]                  units1list               = { "", "k", "M", "G" };
        private          double                    valueWhenConditionCheck  = double.NaN;
        private          string                    stringWhenConditionCheck = "N/A";
        private          bool                      conditionVerified;

        public MechJebModuleScriptCondition(MechJebModuleScript scriptModule, MechJebCore core, MechJebModuleScriptAction action)
        {
            this.scriptModule = scriptModule;
            this.core         = core;
            this.action       = action;
            moduleInfoItems   = core.GetComputerModule<MechJebModuleInfoItems>();
            conditionsList    = new List<string>();
            modifiersList     = new List<string>();
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
            selectedModifier  = GuiUtils.ComboBox.Box(selectedModifier, modifiersList.ToArray(), modifiersList);
            if (!action.isStarted())
            {
                GuiUtils.SimpleTextBox("", value0, "");
                value0unit = GuiUtils.ComboBox.Box(value0unit, units0list, units0list);
                if (selectedModifier == 3)
                {
                    GuiUtils.SimpleTextBox("and", value1, "");
                    value1unit = GuiUtils.ComboBox.Box(value1unit, units1list, units1list);
                }
            }
            else
            {
                GUILayout.Label("" + value0.val + " " + units0list[value0unit]);
                if (selectedModifier == 3)
                {
                    GUILayout.Label(" and " + value1.val + " " + units1list[value1unit]);
                }
            }

            if (action.isStarted() || action.isExecuted())
            {
                if (conditionVerified)
                {
                    GUILayout.Label("(Verified " + getStringWhenConditionCheck() + ")", GuiUtils.greenLabel, GUILayout.ExpandWidth(false));
                }
                else
                {
                    GUILayout.Label("(NOT Verified " + getStringWhenConditionCheck() + ")", GuiUtils.redLabel, GUILayout.ExpandWidth(false));
                }
            }
        }

        public bool checkCondition()
        {
            double value0ref = value0.val;
            if (value0unit == 1)
            {
                value0ref *= 1000;
            }
            else if (value0unit == 2)
            {
                value0ref *= 1000000;
            }
            else if (value0unit == 3)
            {
                value0ref *= 1000000000;
            }

            double value1ref = value1.val;
            if (value1unit == 1)
            {
                value1ref *= 1000;
            }
            else if (value1unit == 2)
            {
                value1ref *= 1000000;
            }
            else if (value0unit == 3)
            {
                value1ref *= 1000000000;
            }

            conditionVerified = false;
            double valueToCompare = getValueToCompare();
            valueWhenConditionCheck  = valueToCompare;
            stringWhenConditionCheck = getValueToCompareString();
            if (valueToCompare == double.NaN)
            {
                return false;
            }

            if (valueToCompare < value0ref && selectedModifier == 0)
            {
                conditionVerified = true;
                return true;
            }

            if (valueToCompare == value0ref && selectedModifier == 1)
            {
                conditionVerified = true;
                return true;
            }

            if (valueToCompare > value0ref && selectedModifier == 2)
            {
                conditionVerified = true;
                return true;
            }

            if (valueToCompare > value0ref && valueToCompare < value1ref && selectedModifier == 3)
            {
                conditionVerified = true;
                return true;
            }

            return false;
        }

        public double getValueWhenConditionCheck()
        {
            return valueWhenConditionCheck;
        }

        public string getStringWhenConditionCheck()
        {
            return stringWhenConditionCheck;
        }

        public bool getConditionVerified()
        {
            return conditionVerified;
        }

        public string getValueToCompareString()
        {
            if (selectedCondition == 0) //Altitude
            {
                return core.vesselState.altitudeBottom.ToSI(-1, 0);
            }

            if (selectedCondition == 1) //Speed
            {
                return core.vesselState.speedSurface.value.ToSI(-1, 0);
            }

            if (selectedCondition == 2) //Distance to target
            {
                return moduleInfoItems.TargetDistance();
            }

            if (selectedCondition == 3) //Target Apoapsis
            {
                return moduleInfoItems.TargetApoapsis();
            }

            if (selectedCondition == 4) //Target Periapsis
            {
                return moduleInfoItems.TargetPeriapsis();
            }

            if (selectedCondition == 5) //Time to closest approach
            {
                return moduleInfoItems.TargetTimeToClosestApproach();
            }

            if (selectedCondition == 6) //Distance to closest approach
            {
                return moduleInfoItems.TargetClosestApproachDistance();
            }

            if (selectedCondition == 7) //Target Relative velocity
            {
                return moduleInfoItems.TargetRelativeVelocity();
            }

            if (selectedCondition == 8) //Target inclination
            {
                return moduleInfoItems.TargetInclination();
            }

            if (selectedCondition == 9) //Target orbit period
            {
                return moduleInfoItems.TargetOrbitPeriod();
            }

            if (selectedCondition == 10) //Target orbit speed
            {
                return moduleInfoItems.TargetOrbitSpeed();
            }

            if (selectedCondition == 11) //Target time to Ap
            {
                return moduleInfoItems.TargetOrbitTimeToAp();
            }

            if (selectedCondition == 12) //Target time to Pe
            {
                return moduleInfoItems.TargetOrbitTimeToPe();
            }

            if (selectedCondition == 13) //Target LAN
            {
                return moduleInfoItems.TargetLAN();
            }

            if (selectedCondition == 14) //Target AoP
            {
                return moduleInfoItems.TargetAoP();
            }

            if (selectedCondition == 15) //Target eccentricity
            {
                return moduleInfoItems.TargetEccentricity();
            }

            if (selectedCondition == 16) //Target SMA
            {
                return moduleInfoItems.TargetSMA();
            }

            if (selectedCondition == 17) //Periapsis in SOI
            {
                return moduleInfoItems.PeriapsisInTargetSOI();
            }

            if (selectedCondition == 18) //Phase angle to target
            {
                return moduleInfoItems.PhaseAngle();
            }

            if (selectedCondition == 19) //Target planet phase angle
            {
                return moduleInfoItems.TargetPlanetPhaseAngle();
            }

            if (selectedCondition == 20) //Relative inclination
            {
                return moduleInfoItems.RelativeInclinationToTarget();
            }

            if (selectedCondition == 21) //Time to AN
            {
                return moduleInfoItems.TimeToAscendingNodeWithTarget();
            }

            if (selectedCondition == 22) //Time to DN
            {
                return moduleInfoItems.TimeToDescendingNodeWithTarget();
            }

            if (selectedCondition == 23) //Time to equatorial AN
            {
                return moduleInfoItems.TimeToEquatorialAscendingNode();
            }

            if (selectedCondition == 24) //Time to equatorial DN
            {
                return moduleInfoItems.TimeToEquatorialDescendingNode();
            }

            if (selectedCondition == 25) //Circular orbit speed
            {
                return moduleInfoItems.CircularOrbitSpeed().ToSI();
            }

            if (selectedCondition == 26) //Apoapsis
            {
                return core.vesselState.orbitApA.value.ToSI();
            }

            if (selectedCondition == 27) //Periapsis
            {
                return core.vesselState.orbitPeA.value.ToSI();
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
            else if (selectedCondition == 26) //Apoapsis
            {
                return core.vesselState.orbitApA;
            }
            else if (selectedCondition == 27) //Periapsis
            {
                return core.vesselState.orbitPeA;
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
            if (core.target.Target != null && core.vesselState.altitudeTrue < 1000.0)
            {
                return GuiUtils.FromToETA(core.vessel.CoM, core.target.Transform.position);
            }

            if (!core.target.NormalTargetExists) return double.NaN;
            if (core.target.TargetOrbit.referenceBody != scriptModule.orbit.referenceBody) return double.NaN;
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return double.NaN; }

            if (core.vesselState.altitudeTrue < 1000.0)
            {
                double a = (core.vessel.mainBody.transform.position - core.vessel.transform.position).magnitude;
                double b = (core.vessel.mainBody.transform.position - core.target.Transform.position).magnitude;
                double c = Vector3d.Distance(core.vessel.transform.position, core.target.Position);
                double ang = Math.Acos((a * a + b * b - c * c) / (2f * a * b));
                return ang * core.vessel.mainBody.Radius / core.vesselState.speedSurfaceHorizontal;
            }

            return scriptModule.orbit.NextClosestApproachTime(core.target.TargetOrbit, core.vesselState.time) - core.vesselState.time;
        }

        public double TargetClosestApproachDistance()
        {
            if (!core.target.NormalTargetExists) return double.NaN;
            if (core.target.TargetOrbit.referenceBody != scriptModule.orbit.referenceBody) return double.NaN;
            if (core.vesselState.altitudeTrue < 1000.0) { return double.NaN; }

            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return double.NaN; }

            return scriptModule.orbit.NextClosestApproachDistance(core.target.TargetOrbit, core.vesselState.time);
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
            if (core.target.TargetOrbit.referenceBody != scriptModule.orbit.referenceBody) return double.NaN;
            if (double.IsNaN(core.target.TargetOrbit.semiMajorAxis)) { return double.NaN; }

            return scriptModule.orbit.PhaseAngle(core.target.TargetOrbit, core.vesselState.time);
        }

        public double TargetPlanetPhaseAngle()
        {
            if (!(core.target.Target is CelestialBody)) return double.NaN;
            if (core.target.TargetOrbit.referenceBody != scriptModule.orbit.referenceBody.referenceBody) return double.NaN;

            return scriptModule.mainBody.orbit.PhaseAngle(core.target.TargetOrbit, core.vesselState.time);
        }

        public double RelativeInclinationToTarget()
        {
            if (!core.target.NormalTargetExists) return double.NaN;
            if (core.target.TargetOrbit.referenceBody != scriptModule.orbit.referenceBody) return double.NaN;

            return scriptModule.orbit.RelativeInclination(core.target.TargetOrbit);
        }

        public double TimeToAscendingNodeWithTarget()
        {
            if (!core.target.NormalTargetExists) return double.NaN;
            if (core.target.TargetOrbit.referenceBody != scriptModule.orbit.referenceBody) return double.NaN;
            if (!scriptModule.orbit.AscendingNodeExists(core.target.TargetOrbit)) return double.NaN;

            return scriptModule.orbit.TimeOfAscendingNode(core.target.TargetOrbit, core.vesselState.time) - core.vesselState.time;
        }

        public double TimeToDescendingNodeWithTarget()
        {
            if (!core.target.NormalTargetExists) return double.NaN;
            if (core.target.TargetOrbit.referenceBody != scriptModule.orbit.referenceBody) return double.NaN;
            if (!scriptModule.orbit.DescendingNodeExists(core.target.TargetOrbit)) return double.NaN;

            return scriptModule.orbit.TimeOfDescendingNode(core.target.TargetOrbit, core.vesselState.time) - core.vesselState.time;
        }

        public double TimeToEquatorialAscendingNode()
        {
            if (!scriptModule.orbit.AscendingNodeEquatorialExists()) return double.NaN;

            return scriptModule.orbit.TimeOfAscendingNodeEquatorial(core.vesselState.time) - core.vesselState.time;
        }

        public double TimeToEquatorialDescendingNode()
        {
            if (!scriptModule.orbit.DescendingNodeEquatorialExists()) return double.NaN;

            return scriptModule.orbit.TimeOfDescendingNodeEquatorial(core.vesselState.time) - core.vesselState.time;
        }
    }
}
