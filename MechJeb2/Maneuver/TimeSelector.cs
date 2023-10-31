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

        private readonly TimeReference[] _allowedTimeRef;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public int _currentTimeRef;

        public TimeReference TimeReference => _allowedTimeRef[_currentTimeRef];

        // Input parameters
        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableTime LeadTime = 0;

        [UsedImplicitly]
        [Persistent(pass = (int)Pass.GLOBAL)]
        public readonly EditableDoubleMult CircularizeAltitude = new EditableDoubleMult(150000, 1000);

        //"Warning: orbit is hyperbolic, so apoapsis doesn't exist."
        private static readonly string _maneuverException1 = Localizer.Format("#MechJeb_Maneu_Exception1");

        //"Warning: no target selected."
        private static readonly string _maneuverException2 = Localizer.Format("#MechJeb_Maneu_Exception2");

        //"Warning: can't circularize at this altitude, since current orbit does not reach it."
        private static readonly string _maneuverException3 = Localizer.Format("#MechJeb_Maneu_Exception3");

        //"Warning: equatorial ascending node doesn't exist."
        private static readonly string _maneuverException4 = Localizer.Format("#MechJeb_Maneu_Exception4");

        //"Warning: equatorial descending node doesn't exist."
        private static readonly string _maneuverException5 = Localizer.Format("#MechJeb_Maneu_Exception5");

        //Warning: neither ascending nor descending node exists.
        private static readonly string _maneuverException6 = Localizer.Format("#MechJeb_Maneu_Exception6");

        //"Warning: neither ascending nor descending node exists."
        private static readonly string _maneuverException7 = Localizer.Format("#MechJeb_Maneu_Exception7");

        public TimeSelector(TimeReference[] allowedTimeRef)
        {
            _allowedTimeRef = allowedTimeRef;
            _universalTime  = 0;
            _timeRefNames   = new string[allowedTimeRef.Length];
            for (int i = 0; i < allowedTimeRef.Length; ++i)
            {
                _timeRefNames[i] = allowedTimeRef[i] switch
                {
                    TimeReference.COMPUTED         => Localizer.Format("#MechJeb_Maneu_TimeSelect1"),
                    TimeReference.APOAPSIS         => Localizer.Format("#MechJeb_Maneu_TimeSelect2"),
                    TimeReference.CLOSEST_APPROACH => Localizer.Format("#MechJeb_Maneu_TimeSelect3"),
                    TimeReference.EQ_ASCENDING     => Localizer.Format("#MechJeb_Maneu_TimeSelect4"),
                    TimeReference.EQ_DESCENDING    => Localizer.Format("#MechJeb_Maneu_TimeSelect5"),
                    TimeReference.PERIAPSIS        => Localizer.Format("#MechJeb_Maneu_TimeSelect6"),
                    TimeReference.REL_ASCENDING    => Localizer.Format("#MechJeb_Maneu_TimeSelect7"),
                    TimeReference.REL_DESCENDING   => Localizer.Format("#MechJeb_Maneu_TimeSelect8"),
                    TimeReference.X_FROM_NOW       => Localizer.Format("#MechJeb_Maneu_TimeSelect9"),
                    TimeReference.ALTITUDE         => Localizer.Format("#MechJeb_Maneu_TimeSelect10"),
                    TimeReference.EQ_NEAREST_AD    => Localizer.Format("#MechJeb_Maneu_TimeSelect11"),
                    TimeReference.EQ_HIGHEST_AD    => Localizer.Format("#MechJeb_Maneu_TimeSelect12"),
                    TimeReference.REL_NEAREST_AD   => Localizer.Format("#MechJeb_Maneu_TimeSelect13"),
                    TimeReference.REL_HIGHEST_AD   => Localizer.Format("#MechJeb_Maneu_TimeSelect14"),
                    _                              => _timeRefNames[i]
                };
            }
        }

        public void DoChooseTimeGUI()
        {
            GUILayout.Label(Localizer.Format("#MechJeb_Maneu_STB")); //Schedule the burn
            GUILayout.BeginHorizontal();
            _currentTimeRef = GuiUtils.ComboBox.Box(_currentTimeRef, _timeRefNames, this);
            switch (TimeReference)
            {
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
            switch (_allowedTimeRef[_currentTimeRef])
            {
                case TimeReference.X_FROM_NOW:
                    ut += LeadTime.Val;
                    break;

                case TimeReference.APOAPSIS:
                    if (!(o.eccentricity < 1))
                        throw new OperationException(_maneuverException1);
                    ut = o.NextApoapsisTime(ut);
                    break;

                case TimeReference.PERIAPSIS:
                    ut = o.NextPeriapsisTime(ut);
                    break;

                case TimeReference.CLOSEST_APPROACH:
                    if (!target.NormalTargetExists)
                        throw new OperationException(_maneuverException2);
                    ut = o.NextClosestApproachTime(target.TargetOrbit, ut);
                    break;

                case TimeReference.ALTITUDE:
                    if (!(CircularizeAltitude > o.PeA) || (!(CircularizeAltitude < o.ApA) && !(o.eccentricity >= 1)))
                        throw new OperationException(_maneuverException3);
                    ut = o.NextTimeOfRadius(ut, o.referenceBody.Radius + CircularizeAltitude);
                    break;

                case TimeReference.EQ_ASCENDING:
                    if (!o.AscendingNodeEquatorialExists())
                        throw new OperationException(_maneuverException4);
                    ut = o.TimeOfAscendingNodeEquatorial(ut);
                    break;

                case TimeReference.EQ_DESCENDING:
                    if (!o.DescendingNodeEquatorialExists())
                        throw new OperationException(_maneuverException5);
                    ut = o.TimeOfDescendingNodeEquatorial(ut);
                    break;

                case TimeReference.EQ_NEAREST_AD:
                    if (o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists())
                        ut = Math.Min(o.TimeOfAscendingNodeEquatorial(ut), o.TimeOfDescendingNodeEquatorial(ut));
                    else if (o.AscendingNodeEquatorialExists())
                        ut = o.TimeOfAscendingNodeEquatorial(ut);
                    else if (o.DescendingNodeEquatorialExists())
                        ut = o.TimeOfDescendingNodeEquatorial(ut);
                    else
                        throw new OperationException(_maneuverException6);
                    break;

                case TimeReference.EQ_HIGHEST_AD:
                    if (o.AscendingNodeEquatorialExists() && o.DescendingNodeEquatorialExists())
                    {
                        double anTime = o.TimeOfAscendingNodeEquatorial(ut);
                        double dnTime = o.TimeOfDescendingNodeEquatorial(ut);
                        ut = o.getOrbitalVelocityAtUT(anTime).magnitude <= o.getOrbitalVelocityAtUT(dnTime).magnitude
                            ? anTime
                            : dnTime;
                    }
                    else if (o.AscendingNodeEquatorialExists())
                        ut = o.TimeOfAscendingNodeEquatorial(ut);
                    else if (o.DescendingNodeEquatorialExists())
                        ut = o.TimeOfDescendingNodeEquatorial(ut);
                    else
                        throw new OperationException(_maneuverException7);

                    break;
            }

            _universalTime = ut;
            return _universalTime;
        }
    }
}
