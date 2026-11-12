/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:MANEUVERPLANNER - the maneuver planner's operations, one child binding each.
    //
    // Each child keeps its own settings for as long as this kOS processor runs (not shared with the planner window or with other
    // processors). Set the parameters, then MAKENODES() (or CHANGELASTNODE()) places the nodes and returns them:
    //
    //   SET circ TO ADDONS:MECHJEB:MANEUVERPLANNER:CIRCULARIZE.
    //   SET circ:TIMEREF TO "APOAPSIS".
    //   circ:MAKENODES().
    //   ADDONS:MECHJEB:NODEEXECUTOR:EXECUTEONENODE().
    [KOSNomenclature("MechJebManeuverPlanner")]
    public class ManeuverPlannerBinding : Structure
    {
        private readonly CircularizeBinding         _circularize;
        private readonly ChangeApoapsisBinding      _changeApoapsis;
        private readonly ChangePeriapsisBinding     _changePeriapsis;
        private readonly EllipticizeBinding         _ellipticize;
        private readonly ChangeEccentricityBinding  _changeEccentricity;
        private readonly ChangeSemiMajorAxisBinding _changeSemiMajorAxis;
        private readonly ChangeInclinationBinding   _changeInclination;
        private readonly ChangeLanBinding           _changeLan;
        private readonly ChangeLongitudeBinding     _changeLongitude;
        private readonly MatchPlanesBinding         _matchPlanes;
        private readonly MatchVelocitiesBinding     _matchVelocities;
        private readonly CourseCorrectionBinding    _courseCorrection;
        private readonly HohmannBinding             _hohmann;
        private readonly InterceptBinding           _intercept;
        private readonly ResonantOrbitBinding       _resonantOrbit;
        private readonly MoonReturnBinding          _moonReturn;
        private readonly StationaryOrbitBinding     _stationaryOrbit;
        private readonly InterplanetaryBinding      _interplanetary;

        public ManeuverPlannerBinding(Func<MechJebCore?> core, SharedObjects shared)
        {
            _circularize         = new CircularizeBinding(core, shared);
            _changeApoapsis      = new ChangeApoapsisBinding(core, shared);
            _changePeriapsis     = new ChangePeriapsisBinding(core, shared);
            _ellipticize         = new EllipticizeBinding(core, shared);
            _changeEccentricity  = new ChangeEccentricityBinding(core, shared);
            _changeSemiMajorAxis = new ChangeSemiMajorAxisBinding(core, shared);
            _changeInclination   = new ChangeInclinationBinding(core, shared);
            _changeLan           = new ChangeLanBinding(core, shared);
            _changeLongitude     = new ChangeLongitudeBinding(core, shared);
            _matchPlanes         = new MatchPlanesBinding(core, shared);
            _matchVelocities     = new MatchVelocitiesBinding(core, shared);
            _courseCorrection    = new CourseCorrectionBinding(core, shared);
            _hohmann             = new HohmannBinding(core, shared);
            _intercept           = new InterceptBinding(core, shared);
            _resonantOrbit       = new ResonantOrbitBinding(core, shared);
            _moonReturn          = new MoonReturnBinding(core, shared);
            _stationaryOrbit     = new StationaryOrbitBinding(core, shared);
            _interplanetary      = new InterplanetaryBinding(core, shared);
            RegisterInitializer(InitializeSuffixes);
        }

        private void InitializeSuffixes()
        {
            AddSuffix("CIRCULARIZE", new NoArgsSuffix<CircularizeBinding>(() => _circularize,
                "Circularize."));
            AddSuffix("CHANGEAPOAPSIS", new NoArgsSuffix<ChangeApoapsisBinding>(() => _changeApoapsis,
                "Change the apoapsis."));
            AddSuffix("CHANGEPERIAPSIS", new NoArgsSuffix<ChangePeriapsisBinding>(() => _changePeriapsis,
                "Change the periapsis."));
            AddSuffix("ELLIPTICIZE", new NoArgsSuffix<EllipticizeBinding>(() => _ellipticize,
                "Change both the periapsis and the apoapsis."));
            AddSuffix("CHANGEECCENTRICITY", new NoArgsSuffix<ChangeEccentricityBinding>(() => _changeEccentricity,
                "Change the eccentricity."));
            AddSuffix("CHANGESEMIMAJORAXIS", new NoArgsSuffix<ChangeSemiMajorAxisBinding>(() => _changeSemiMajorAxis,
                "Change the semi-major axis."));
            AddSuffix("CHANGEINCLINATION", new NoArgsSuffix<ChangeInclinationBinding>(() => _changeInclination,
                "Change the inclination."));
            AddSuffix("CHANGELAN", new NoArgsSuffix<ChangeLanBinding>(() => _changeLan,
                "Change the longitude of the ascending node."));
            AddSuffix("CHANGELONGITUDE", new NoArgsSuffix<ChangeLongitudeBinding>(() => _changeLongitude,
                "Change the surface longitude of an apsis."));
            AddSuffix("MATCHPLANES", new NoArgsSuffix<MatchPlanesBinding>(() => _matchPlanes,
                "Match planes with the target."));
            AddSuffix("MATCHVELOCITIES", new NoArgsSuffix<MatchVelocitiesBinding>(() => _matchVelocities,
                "Match velocities with the target."));
            AddSuffix("COURSECORRECTION", new NoArgsSuffix<CourseCorrectionBinding>(() => _courseCorrection,
                "Fine tune the closest approach to the target."));
            AddSuffix("HOHMANN", new NoArgsSuffix<HohmannBinding>(() => _hohmann,
                "Bi-impulsive (Hohmann) transfer to the target."));
            AddSuffix("INTERCEPT", new NoArgsSuffix<InterceptBinding>(() => _intercept,
                "Intercept the target at a chosen time."));
            AddSuffix("RESONANTORBIT", new NoArgsSuffix<ResonantOrbitBinding>(() => _resonantOrbit,
                "Change to a resonant orbit."));
            AddSuffix("MOONRETURN", new NoArgsSuffix<MoonReturnBinding>(() => _moonReturn,
                "Return from a moon."));
            AddSuffix("STATIONARYORBIT", new NoArgsSuffix<StationaryOrbitBinding>(() => _stationaryOrbit,
                "Transfer to a stationary (synchronous, equatorial) orbit."));
            AddSuffix("INTERPLANETARY", new NoArgsSuffix<InterplanetaryBinding>(() => _interplanetary,
                "Advanced (porkchop) transfer to another planet."));
        }
    }
}
