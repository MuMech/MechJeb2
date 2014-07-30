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
        }

        public void DoChooseTimeGUI()
        {
            GUILayout.Label("Schedule the burn");
            currentTimeRef = GuiUtils.ArrowSelector(currentTimeRef, allowedTimeRef.Length, () =>
                {
                    switch (allowedTimeRef[currentTimeRef])
                    {
                    case TimeReference.APOAPSIS: GUILayout.Label("at the next apoapsis"); break;
                    case TimeReference.CLOSEST_APPROACH: GUILayout.Label("at closest approach to target"); break;
                    case TimeReference.EQ_ASCENDING: GUILayout.Label("at the equatorial AN"); break;
                    case TimeReference.EQ_DESCENDING: GUILayout.Label("at the equatorial DN"); break;
                    case TimeReference.PERIAPSIS: GUILayout.Label("at the next periapsis"); break;
                    case TimeReference.REL_ASCENDING: GUILayout.Label("at the next AN with the target."); break;
                    case TimeReference.REL_DESCENDING: GUILayout.Label("at the next DN with the target."); break;

                    case TimeReference.X_FROM_NOW:
                        leadTime.text = GUILayout.TextField(leadTime.text, GUILayout.Width(50));
                        GUILayout.Label(" from now");
                        break;

                    case TimeReference.ALTITUDE:
                        GuiUtils.SimpleTextBox("at an altitude of", circularizeAltitude, "km");
                        break;
                    }
                });
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
                    throw new System.Exception("Warning: orbit is hyperbolic, so apoapsis doesn't exist.");
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
                    throw new System.Exception("Warning: no target selected.");
                }
                break;

            case TimeReference.ALTITUDE:
                if (circularizeAltitude > o.PeA && (circularizeAltitude < o.ApA || o.eccentricity >= 1))
                {
                    UT = o.NextTimeOfRadius(UT, o.referenceBody.Radius + circularizeAltitude);
                }
                else
                {
                    throw new System.Exception("Warning: can't circularize at this altitude, since current orbit does not reach it.");
                }
                break;

            case TimeReference.EQ_ASCENDING:
                if (o.AscendingNodeEquatorialExists())
                {
                    UT = o.TimeOfAscendingNodeEquatorial(UT);
                }
                else
                {
                    throw new System.Exception("Warning: equatorial ascending node doesn't exist.");
                }
                break;

            case TimeReference.EQ_DESCENDING:
                if (o.DescendingNodeEquatorialExists())
                {
                    UT = o.TimeOfDescendingNodeEquatorial(UT);
                }
                else
                {
                    throw new System.Exception("Warning: equatorial descending node doesn't exist.");
                }
                break;

            }

            universalTime = UT;
            return universalTime;
        }
    }
}

