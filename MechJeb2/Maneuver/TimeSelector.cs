using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MuMech
{
    public enum TimeReference
    {
        COMPUTED, X_FROM_NOW, APOAPSIS, PERIAPSIS, ALTITUDE, EQ_ASCENDING, EQ_DESCENDING,
        REL_ASCENDING, REL_DESCENDING, CLOSEST_APPROACH
    };

    public class TimeSelector
    {
        private string[] timeRefNames;

        public double universalTime;

        private TimeReference[] allowedTimeRef;
        private int currentTimeRef;

        public TimeReference timeReference { get {return allowedTimeRef[currentTimeRef];}}

        // Input parameters
        [Persistent(pass = (int)Pass.Global)]
        public EditableTime leadTime = 0;
        [Persistent(pass = (int)Pass.Global)]
        public EditableDoubleMult circularizeAltitude = new EditableDoubleMult(150000, 1000);

        public TimeSelector(TimeReference[] allowedTimeRef)
        {
            this.allowedTimeRef = allowedTimeRef;
            universalTime = 0;
            timeRefNames = new string[allowedTimeRef.Length];
            for (int i = 0 ; i < allowedTimeRef.Length ; ++i)
            {
              switch (allowedTimeRef[i])
              {
                case TimeReference.APOAPSIS: timeRefNames[i] = "at the next apoapsis"; break;
                case TimeReference.CLOSEST_APPROACH: timeRefNames[i] = "at closest approach to target"; break;
                case TimeReference.EQ_ASCENDING: timeRefNames[i] = "at the equatorial AN"; break;
                case TimeReference.EQ_DESCENDING: timeRefNames[i] = "at the equatorial DN"; break;
                case TimeReference.PERIAPSIS: timeRefNames[i] = "at the next periapsis"; break;
                case TimeReference.REL_ASCENDING: timeRefNames[i] = "at the next AN with the target."; break;
                case TimeReference.REL_DESCENDING: timeRefNames[i] = "at the next DN with the target."; break;

                case TimeReference.X_FROM_NOW: timeRefNames[i] = "after a fixed time"; break;

                case TimeReference.ALTITUDE: timeRefNames[i] = "at an altitude"; break;
              }
            }
        }

        public void DoChooseTimeGUI()
        {
            GUILayout.Label("Schedule the burn");
            GUILayout.BeginHorizontal();
            currentTimeRef = GuiUtils.ComboBox.Box(currentTimeRef, timeRefNames, this);
            switch (timeReference)
            {
              // No additional parameters required
              case TimeReference.APOAPSIS:
              case TimeReference.CLOSEST_APPROACH:
              case TimeReference.EQ_ASCENDING:
              case TimeReference.EQ_DESCENDING:
              case TimeReference.PERIAPSIS:
              case TimeReference.REL_ASCENDING:
              case TimeReference.REL_DESCENDING:
                break;

              case TimeReference.X_FROM_NOW:
                GuiUtils.SimpleTextBox("of", leadTime);
                break;

              case TimeReference.ALTITUDE:
                GuiUtils.SimpleTextBox("of", circularizeAltitude, "km");
                break;
            }
            GUILayout.EndHorizontal();
        }

        public double ComputeManeuverTime(Orbit o, double UT, MechJebModuleTargetController target)
        {
            switch (allowedTimeRef[currentTimeRef])
            {
            case TimeReference.X_FROM_NOW:
                UT += leadTime.val;
                break;

            case TimeReference.APOAPSIS:
                if (o.eccentricity < 1)
                {
                    UT = o.NextApoapsisTime(UT);
                }
                else
                {
                    throw new OperationException("Warning: orbit is hyperbolic, so apoapsis doesn't exist.");
                }
                break;

            case TimeReference.PERIAPSIS:
                UT = o.NextPeriapsisTime(UT);
                break;

            case TimeReference.CLOSEST_APPROACH:
                if (target.NormalTargetExists)
                {
                    UT = o.NextClosestApproachTime(target.TargetOrbit, UT);
                }
                else
                {
                    throw new OperationException("Warning: no target selected.");
                }
                break;

            case TimeReference.ALTITUDE:
                if (circularizeAltitude > o.PeA && (circularizeAltitude < o.ApA || o.eccentricity >= 1))
                {
                    UT = o.NextTimeOfRadius(UT, o.referenceBody.Radius + circularizeAltitude);
                }
                else
                {
                    throw new OperationException("Warning: can't circularize at this altitude, since current orbit does not reach it.");
                }
                break;

            case TimeReference.EQ_ASCENDING:
                if (o.AscendingNodeEquatorialExists())
                {
                    UT = o.TimeOfAscendingNodeEquatorial(UT);
                }
                else
                {
                    throw new OperationException("Warning: equatorial ascending node doesn't exist.");
                }
                break;

            case TimeReference.EQ_DESCENDING:
                if (o.DescendingNodeEquatorialExists())
                {
                    UT = o.TimeOfDescendingNodeEquatorial(UT);
                }
                else
                {
                    throw new OperationException("Warning: equatorial descending node doesn't exist.");
                }
                break;

            }

            universalTime = UT;
            return universalTime;
        }
    }
}

