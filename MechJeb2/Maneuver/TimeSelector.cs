using System;
using JetBrains.Annotations;
using KSP.Localization;
using UnityEngine;

namespace MuMech
{
    public enum TimeReference
    {
        COMPUTED, X_FROM_NOW, APOAPSIS, PERIAPSIS, ALTITUDE, EQ_ASCENDING, EQ_DESCENDING,
        REL_ASCENDING, REL_DESCENDING, CLOSEST_APPROACH,
        EQ_HIGHEST_AD, EQ_NEAREST_AD, REL_HIGHEST_AD, REL_NEAREST_AD
    }

    public class TimeSelector
    {
        private readonly string[] _timeRefNames;

        private double _universalTime;

        private readonly TimeReference[] allowedTimeRef;

        [Persistent(pass = (int)Pass.GLOBAL)]
        private int _currentTimeRef;

        public TimeReference TimeReference => allowedTimeRef[_currentTimeRef];

        // Input parameters
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableTime LeadTime = 0;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDoubleMult CircularizeAltitude = new EditableDoubleMult(150000, 1000);

        public TimeSelector(TimeReference[] allowedTimeRef)
        {
            this.allowedTimeRef = allowedTimeRef;
            _universalTime      = 0;
            _timeRefNames       = new string[allowedTimeRef.Length];
            for (int i = 0; i < allowedTimeRef.Length; ++i)
            {
                switch (allowedTimeRef[i])
                {
                    case TimeReference.COMPUTED:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect1");
                        break; //at the optimum time
                    case TimeReference.APOAPSIS:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect2");
                        break; //"at the next apoapsis"
                    case TimeReference.CLOSEST_APPROACH:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect3");
                        break; //"at closest approach to target"
                    case TimeReference.EQ_ASCENDING:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect4");
                        break; //"at the equatorial AN"
                    case TimeReference.EQ_DESCENDING:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect5");
                        break; //"at the equatorial DN"
                    case TimeReference.PERIAPSIS:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect6");
                        break; //"at the next periapsis"
                    case TimeReference.REL_ASCENDING:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect7");
                        break; //"at the next AN with the target."
                    case TimeReference.REL_DESCENDING:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect8");
                        break; //"at the next DN with the target."
                    case TimeReference.X_FROM_NOW:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect9");
                        break; //"after a fixed time"
                    case TimeReference.ALTITUDE:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect10");
                        break; //"at an altitude"
                    case TimeReference.EQ_NEAREST_AD:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect11");
                        break; //"at the nearest equatorial AN/DN"
                    case TimeReference.EQ_HIGHEST_AD:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect12");
                        break; //"at the cheapest equatorial AN/DN"
                    case TimeReference.REL_NEAREST_AD:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect13");
                        break; //"at the nearest AN/DN with the target"
                    case TimeReference.REL_HIGHEST_AD:
                        _timeRefNames[i] = Localizer.Format("#MechJeb_Maneu_TimeSelect14");
                        break; //"at the cheapest AN/DN with the target"
                }
            }
        }

        public void DoChooseTimeGUI()
        {
            GUILayout.Label(Localizer.Format("#MechJeb_Maneu_STB")); //Schedule the burn
            GUILayout.BeginHorizontal();
            _currentTimeRef = GuiUtils.ComboBox.Box(_currentTimeRef, _timeRefNames, this);
            switch (TimeReference)
            {
                // No additional parameters required
                case TimeReference.COMPUTED:
                case TimeReference.APOAPSIS:
                case TimeReference.CLOSEST_APPROACH:
                case TimeReference.EQ_ASCENDING:
                case TimeReference.EQ_DESCENDING:
                case TimeReference.PERIAPSIS:
                case TimeReference.REL_ASCENDING:
                case TimeReference.REL_DESCENDING:
                case TimeReference.EQ_NEAREST_AD:
                case TimeReference.EQ_HIGHEST_AD:
                case TimeReference.REL_NEAREST_AD:
                case TimeReference.REL_HIGHEST_AD:
                    break;

                case TimeReference.X_FROM_NOW:
                    GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_of"), LeadTime); //"of"
                    break;

                case TimeReference.ALTITUDE:
                    GuiUtils.SimpleTextBox(Localizer.Format("#MechJeb_of"), CircularizeAltitude, "km"); //"of"
                    break;
            }

            GUILayout.EndHorizontal();
        }

        public double ComputeManeuverTime(Orbit o, double ut, MechJebModuleTargetController target)
        {
            switch (allowedTimeRef[_currentTimeRef])
            {
                case TimeReference.X_FROM_NOW:
                    ut += LeadTime.val;
                    break;

                case TimeReference.APOAPSIS:
                    if (o.eccentricity < 1)
                    {
                        ut = o.NextApoapsisTime(ut);
                    }
                    else
                    {
                        throw new OperationException(
                            Localizer.Format("#MechJeb_Maneu_Exception1")); //"Warning: orbit is hyperbolic, so apoapsis doesn't exist."
                    }

                    break;

                case TimeReference.PERIAPSIS:
                    ut = o.NextPeriapsisTime(ut);
                    break;

                case TimeReference.CLOSEST_APPROACH:
                    if (target.NormalTargetExists)
                    {
                        ut = o.NextClosestApproachTime(target.TargetOrbit, ut);
                    }
                    else
                    {
                        throw new OperationException(Localizer.Format("#MechJeb_Maneu_Exception2")); //"Warning: no target selected."
                    }

                    break;

                case TimeReference.ALTITUDE:
                    if (CircularizeAltitude > o.PeA && (CircularizeAltitude < o.ApA || o.eccentricity >= 1))
                    {
                        ut = o.NextTimeOfRadius(ut, o.referenceBody.Radius + CircularizeAltitude);
                    }
                    else
                    {
                        throw
                            new OperationException(
                                Localizer.Format(
                                    "#MechJeb_Maneu_Exception3")); //"Warning: can't circularize at this altitude, since current orbit does not reach it."
                    }

                    break;

                case TimeReference.EQ_ASCENDING:
                    if (o.AscendingNodeEquatorialExists())
                    {
                        ut = o.TimeOfAscendingNodeEquatorial(ut);
                    }
                    else
                    {
                        throw new OperationException(
                            Localizer.Format("#MechJeb_Maneu_Exception4")); //"Warning: equatorial ascending node doesn't exist."
                    }

                    break;

                case TimeReference.EQ_DESCENDING:
                    if (o.DescendingNodeEquatorialExists())
                    {
                        ut = o.TimeOfDescendingNodeEquatorial(ut);
                    }
                    else
                    {
                        throw new OperationException(
                            Localizer.Format("#MechJeb_Maneu_Exception5")); //"Warning: equatorial descending node doesn't exist."
                    }

                    break;

                case TimeReference.EQ_NEAREST_AD:
                    if (o.AscendingNodeEquatorialExists())
                    {
                        ut = o.DescendingNodeEquatorialExists()
                            ? Math.Min(o.TimeOfAscendingNodeEquatorial(ut), o.TimeOfDescendingNodeEquatorial(ut))
                            : o.TimeOfAscendingNodeEquatorial(ut);
                    }
                    else if (o.DescendingNodeEquatorialExists())
                    {
                        ut = o.TimeOfDescendingNodeEquatorial(ut);
                    }
                    else
                    {
                        throw new OperationException(
                            Localizer.Format("#MechJeb_Maneu_Exception6")); //Warning: neither ascending nor descending node exists.
                    }

                    break;

                case TimeReference.EQ_HIGHEST_AD:
                    if (o.AscendingNodeEquatorialExists())
                    {
                        if (o.DescendingNodeEquatorialExists())
                        {
                            double anTime = o.TimeOfAscendingNodeEquatorial(ut);
                            double dnTime = o.TimeOfDescendingNodeEquatorial(ut);
                            ut = o.getOrbitalVelocityAtUT(anTime).magnitude <= o.getOrbitalVelocityAtUT(dnTime).magnitude
                                ? anTime
                                : dnTime;
                        }
                        else
                        {
                            ut = o.TimeOfAscendingNodeEquatorial(ut);
                        }
                    }
                    else if (o.DescendingNodeEquatorialExists())
                    {
                        ut = o.TimeOfDescendingNodeEquatorial(ut);
                    }
                    else
                    {
                        throw new OperationException(
                            Localizer.Format("#MechJeb_Maneu_Exception7")); //"Warning: neither ascending nor descending node exists."
                    }

                    break;
            }

            _universalTime = ut;
            return _universalTime;
        }
    }
}
