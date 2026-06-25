/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;
using kOS.Suffixed;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:VESSELSTATE - read-only access to MechJeb's per-tick VesselState.
    //
    // Everything here is recomputed by MechJeb every physics tick, so all suffixes are read-only
    // outputs. Vectors are raw ship-world vectors (the same frame kOS uses for SHIP:something:VECTOR).
    //
    // TODO: add the Vector6 torque interfaces
    [KOSNomenclature("MechJebVesselState")]
    public class VesselStateBinding : Structure
    {
        private readonly Func<MechJebCore?> _core;

        public VesselStateBinding(Func<MechJebCore?> core)
        {
            _core = core;
            RegisterInitializer(InitializeSuffixes);
        }

        // re-evaluated on every call since the core can update dynamically
        private VesselState _vesselState
        {
            get
            {
                MechJebCore core = _core() ?? throw new KOSException("MechJeb is not available on this vessel.");
                return core.VesselState;
            }
        }

        private void InitializeSuffixes()
        {
            // --- Position / surface ---
            AddSuffix("ALTITUDEASL", new Suffix<ScalarValue>(() => _vesselState.AltitudeASL,
                "Altitude above sea level in meters."));
            AddSuffix("ALTITUDETRUE", new Suffix<ScalarValue>(() => _vesselState.AltitudeTrue,
                "Altitude above terrain (true altitude) in meters."));
            AddSuffix("ALTITUDEBOTTOM", new Suffix<ScalarValue>(() => _vesselState.AltitudeBottom,
                "Altitude of the lowest part of the vessel above terrain in meters."));
            AddSuffix("SURFACEALTITUDEASL", new Suffix<ScalarValue>(() => _vesselState.SurfaceAltitudeASL,
                "Altitude of the terrain (surface) above sea level in meters."));
            AddSuffix("LATITUDE", new Suffix<ScalarValue>(() => _vesselState.Latitude,
                "Latitude in degrees."));
            AddSuffix("LONGITUDE", new Suffix<ScalarValue>(() => _vesselState.Longitude,
                "Longitude in degrees."));
            AddSuffix("HEADING", new Suffix<ScalarValue>(() => _vesselState.Heading,
                "Heading in degrees."));
            AddSuffix("PITCH", new Suffix<ScalarValue>(() => _vesselState.Pitch,
                "Pitch in degrees."));
            AddSuffix("ROLL", new Suffix<ScalarValue>(() => _vesselState.Roll,
                "Roll in degrees."));
            AddSuffix("RADIUS", new Suffix<ScalarValue>(() => _vesselState.Radius,
                "Distance from the center of the main body in meters."));

            // --- Orbit ---
            AddSuffix("APOAPSIS", new Suffix<ScalarValue>(() => _vesselState.OrbitApA,
                "Apoapsis altitude in meters."));
            AddSuffix("PERIAPSIS", new Suffix<ScalarValue>(() => _vesselState.OrbitPeA,
                "Periapsis altitude in meters."));
            AddSuffix("TIMETOAPOAPSIS", new Suffix<ScalarValue>(() => _vesselState.OrbitTimeToAp,
                "Time to apoapsis in seconds."));
            AddSuffix("TIMETOPERIAPSIS", new Suffix<ScalarValue>(() => _vesselState.OrbitTimeToPe,
                "Time to periapsis in seconds."));
            AddSuffix("ECCENTRICITY", new Suffix<ScalarValue>(() => _vesselState.OrbitEccentricity,
                "Orbital eccentricity."));
            AddSuffix("INCLINATION", new Suffix<ScalarValue>(() => _vesselState.OrbitInclination,
                "Orbital inclination in degrees."));
            AddSuffix("LAN", new Suffix<ScalarValue>(() => _vesselState.OrbitLAN,
                "Longitude of the ascending node in degrees."));
            AddSuffix("ARGUMENTOFPERIAPSIS", new Suffix<ScalarValue>(() => _vesselState.OrbitArgumentOfPeriapsis,
                "Argument of periapsis in degrees."));
            AddSuffix("SEMIMAJORAXIS", new Suffix<ScalarValue>(() => _vesselState.OrbitSemiMajorAxis,
                "Semi-major axis in meters."));
            AddSuffix("ORBITALPERIOD", new Suffix<ScalarValue>(() => _vesselState.OrbitPeriod,
                "Orbital period in seconds."));
            AddSuffix("ANGLETOPROGRADE", new Suffix<ScalarValue>(() => _vesselState.AngleToPrograde,
                "Angle to prograde in degrees."));
            AddSuffix("CELESTIALLONGITUDE", new Suffix<ScalarValue>(() => _vesselState.CelestialLongitude,
                "Celestial longitude in degrees."));

            // --- Speeds ---
            AddSuffix("ORBITALSPEED", new Suffix<ScalarValue>(() => _vesselState.SpeedOrbital,
                "Orbital speed in m/s."));
            AddSuffix("ORBITALHORIZONTALSPEED", new Suffix<ScalarValue>(() => _vesselState.SpeedOrbitalHorizontal,
                "Horizontal component of orbital speed in m/s."));
            AddSuffix("SURFACESPEED", new Suffix<ScalarValue>(() => _vesselState.SpeedSurface,
                "Surface-relative speed in m/s."));
            AddSuffix("SURFACEHORIZONTALSPEED", new Suffix<ScalarValue>(() => _vesselState.SpeedSurfaceHorizontal,
                "Horizontal component of surface-relative speed in m/s."));
            AddSuffix("VERTICALSPEED", new Suffix<ScalarValue>(() => _vesselState.SpeedVertical,
                "Vertical speed in m/s."));

            // --- Atmosphere / aerodynamics ---
            AddSuffix("MACH", new Suffix<ScalarValue>(() => _vesselState.Mach,
                "Mach number."));
            AddSuffix("SPEEDOFSOUND", new Suffix<ScalarValue>(() => _vesselState.SpeedOfSound,
                "Local speed of sound in m/s."));
            AddSuffix("DYNAMICPRESSURE", new Suffix<ScalarValue>(() => _vesselState.DynamicPressure,
                "Dynamic pressure in Pa."));
            AddSuffix("MAXDYNAMICPRESSURE", new Suffix<ScalarValue>(() => _vesselState.MaxDynamicPressure,
                "Maximum dynamic pressure seen this flight in Pa."));
            AddSuffix("ATMOSPHERICDENSITY", new Suffix<ScalarValue>(() => _vesselState.AtmosphericDensity,
                "Atmospheric density in kg/m^3."));
            AddSuffix("ATMOSPHERICDENSITYGRAMS", new Suffix<ScalarValue>(() => _vesselState.AtmosphericDensityInGrams,
                "Atmospheric density in g/m^3."));
            AddSuffix("DRAGCOEFFICIENT", new Suffix<ScalarValue>(() => _vesselState.DragCoefficient,
                "Drag coefficient."));
            AddSuffix("AREADRAG", new Suffix<ScalarValue>(() => _vesselState.AreaDrag,
                "Area drag in m^2."));
            AddSuffix("DRAGFORCE", new Suffix<ScalarValue>(() => _vesselState.DragForce,
                "Wind-relative drag force in kN."));
            AddSuffix("DRAGACCELERATION", new Suffix<ScalarValue>(() => _vesselState.DragAcceleration,
                "Wind-relative drag acceleration in m/s^2."));
            AddSuffix("LIFTFORCE", new Suffix<ScalarValue>(() => _vesselState.LiftForce,
                "Wind-relative lift force in kN."));
            AddSuffix("LIFTACCELERATION", new Suffix<ScalarValue>(() => _vesselState.LiftAcceleration,
                "Wind-relative lift acceleration in m/s^2."));
            AddSuffix("PUREDRAG", new Suffix<ScalarValue>(() => _vesselState.PureDrag,
                "Pure drag acceleration magnitude in m/s^2."));
            AddSuffix("PURELIFT", new Suffix<ScalarValue>(() => _vesselState.PureLift,
                "Pure lift acceleration magnitude in m/s^2."));
            AddSuffix("TERMINALVELOCITY", new Suffix<ScalarValue>(() => _vesselState.TerminalVelocity(),
                "Terminal velocity in m/s."));
            AddSuffix("AOA", new Suffix<ScalarValue>(() => _vesselState.AoA,
                "Angle of attack in degrees."));
            AddSuffix("AOS", new Suffix<ScalarValue>(() => _vesselState.AoS,
                "Angle of sideslip in degrees."));
            AddSuffix("AOD", new Suffix<ScalarValue>(() => _vesselState.AoD,
                "Displacement angle in degrees."));
            AddSuffix("AEROTHERMALFLUX", new Suffix<ScalarValue>(() => _vesselState.FreeMolecularAerothermalFlux,
                "Free molecular aerothermal flux in W/m^2."));

            // --- Intake air ---
            AddSuffix("INTAKEAIR", new Suffix<ScalarValue>(() => _vesselState.IntakeAir,
                "Intake air available in kg/s."));
            AddSuffix("INTAKEAIRALLINTAKES", new Suffix<ScalarValue>(() => _vesselState.IntakeAirAllIntakes,
                "Intake air available with all intakes open in kg/s."));
            AddSuffix("INTAKEAIRNEEDED", new Suffix<ScalarValue>(() => _vesselState.IntakeAirNeeded,
                "Intake air needed in kg/s."));
            AddSuffix("INTAKEAIRATMAX", new Suffix<ScalarValue>(() => _vesselState.IntakeAirAtMax,
                "Intake air needed at full throttle in kg/s."));

            // --- Mass / thrust ---
            AddSuffix("MASS", new Suffix<ScalarValue>(() => _vesselState.Mass,
                "Vessel mass in tonnes."));
            AddSuffix("THRUSTAVAILABLE", new Suffix<ScalarValue>(() => _vesselState.ThrustAvailable,
                "Maximum forward thrust in kN."));
            AddSuffix("THRUSTMINIMUM", new Suffix<ScalarValue>(() => _vesselState.ThrustMinimum,
                "Minimum forward thrust in kN."));
            AddSuffix("THRUSTCURRENT", new Suffix<ScalarValue>(() => _vesselState.ThrustCurrent,
                "Forward thrust applied last frame in kN."));
            AddSuffix("MAXTHRUSTACCELERATION", new Suffix<ScalarValue>(() => _vesselState.MaxThrustAcceleration,
                "Forward acceleration at maximum thrust in m/s^2."));
            AddSuffix("MINTHRUSTACCELERATION", new Suffix<ScalarValue>(() => _vesselState.MinThrustAcceleration,
                "Forward acceleration at minimum thrust in m/s^2."));
            AddSuffix("CURRENTTHRUSTACCELERATION", new Suffix<ScalarValue>(() => _vesselState.CurrentThrustAcceleration,
                "Forward acceleration applied last frame in m/s^2."));
            AddSuffix("LIMITEDMAXTHRUSTACCELERATION", new Suffix<ScalarValue>(() => _vesselState.LimitedMaxThrustAcceleration,
                "Forward acceleration at the current fixed throttle limit in m/s^2."));
            AddSuffix("THROTTLELIMIT", new Suffix<ScalarValue>(() => _vesselState.ThrottleLimit,
                "Current throttle limit (0-1), including transient limits."));
            AddSuffix("THROTTLEFIXEDLIMIT", new Suffix<ScalarValue>(() => _vesselState.ThrottleFixedLimit,
                "Current non-transient throttle limit (0-1)."));
            AddSuffix("LOWESTULLAGE", new Suffix<ScalarValue>(() => _vesselState.LowestUllage,
                "Lowest propellant ullage stability across engines (1.0 = stable; always 1.0 without RealFuels)."));

            // --- Misc scalars ---
            AddSuffix("LOCALGRAVITY", new Suffix<ScalarValue>(() => _vesselState.LocalGravity,
                "Local gravitational acceleration in m/s^2."));
            AddSuffix("TIME", new Suffix<ScalarValue>(() => _vesselState.Time,
                "Universal time in seconds."));
            AddSuffix("DELTAT", new Suffix<ScalarValue>(() => _vesselState.DeltaT,
                "Physics timestep in seconds."));
            AddSuffix("MAXENGINERESPONSETIME", new Suffix<ScalarValue>(() => _vesselState.MaxEngineResponseTime,
                "Maximum engine spool-up response time in seconds."));

            // --- Booleans ---
            AddSuffix("PARACHUTEDEPLOYED", new Suffix<BooleanValue>(() => _vesselState.ParachuteDeployed,
                "True if any parachute is deployed."));
            AddSuffix("RCSTHRUST", new Suffix<BooleanValue>(() => _vesselState.RCSThrust,
                "True if RCS is currently thrusting for translation."));

            // --- Vectors (raw ship-world frame) ---
            AddSuffix("COM", new Suffix<Vector>(() => new Vector(_vesselState.CoM),
                "Center of mass position."));
            AddSuffix("COT", new Suffix<Vector>(() => new Vector(_vesselState.CoT),
                "Center of thrust vector."));
            AddSuffix("COL", new Suffix<Vector>(() => new Vector(_vesselState.CoL),
                "Center of lift vector."));
            AddSuffix("DOT", new Suffix<Vector>(() => new Vector(_vesselState.DoT),
                "Direction of thrust vector."));
            AddSuffix("FORWARD", new Suffix<Vector>(() => new Vector(_vesselState.Forward),
                "Unit vector the vessel is pointing along."));
            AddSuffix("UP", new Suffix<Vector>(() => new Vector(_vesselState.Up),
                "Local up (away from the body) unit vector."));
            AddSuffix("NORTH", new Suffix<Vector>(() => new Vector(_vesselState.North),
                "Local north unit vector."));
            AddSuffix("EAST", new Suffix<Vector>(() => new Vector(_vesselState.East),
                "Local east unit vector."));
            AddSuffix("ANGULARMOMENTUM", new Suffix<Vector>(() => new Vector(_vesselState.AngularMomentum),
                "Angular momentum vector."));
            AddSuffix("ANGULARVELOCITY", new Suffix<Vector>(() => new Vector(_vesselState.AngularVelocity),
                "Angular velocity vector."));
            AddSuffix("GRAVITYFORCE", new Suffix<Vector>(() => new Vector(_vesselState.GravityForce),
                "Gravitational acceleration vector in m/s^2."));
            AddSuffix("ORBITALPOSITION", new Suffix<Vector>(() => new Vector(_vesselState.OrbitalPosition),
                "Position relative to the main body."));
            AddSuffix("ORBITALVELOCITY", new Suffix<Vector>(() => new Vector(_vesselState.OrbitalVelocity),
                "Orbital velocity vector."));
            AddSuffix("SURFACEVELOCITY", new Suffix<Vector>(() => new Vector(_vesselState.SurfaceVelocity),
                "Surface-relative velocity vector."));
            AddSuffix("VELOCITYMAINBODYSURFACE", new Suffix<Vector>(() => new Vector(_vesselState.VelocityMainBodySurface),
                "Velocity in the main body's rotating surface frame."));
            AddSuffix("HORIZONTALORBIT", new Suffix<Vector>(() => new Vector(_vesselState.HorizontalOrbit),
                "Unit vector along the horizontal component of orbital velocity."));
            AddSuffix("HORIZONTALSURFACE", new Suffix<Vector>(() => new Vector(_vesselState.HorizontalSurface),
                "Unit vector along the horizontal component of surface velocity."));
            AddSuffix("NORMALPLUS", new Suffix<Vector>(() => new Vector(_vesselState.NormalPlus),
                "Orbit normal (+) unit vector."));
            AddSuffix("NORMALPLUSSURFACE", new Suffix<Vector>(() => new Vector(_vesselState.NormalPlusSurface),
                "Surface-frame normal (+) unit vector."));
            AddSuffix("RADIALPLUS", new Suffix<Vector>(() => new Vector(_vesselState.RadialPlus),
                "Orbit radial (+) unit vector."));
            AddSuffix("RADIALPLUSSURFACE", new Suffix<Vector>(() => new Vector(_vesselState.RadialPlusSurface),
                "Surface-frame radial (+) unit vector."));
            AddSuffix("ROOTPARTPOSITION", new Suffix<Vector>(() => new Vector(_vesselState.RootPartPosition),
                "Position of the root part."));
            AddSuffix("PUREDRAGVECTOR", new Suffix<Vector>(() => new Vector(_vesselState.PureDragVector),
                "Pure drag acceleration vector in m/s^2."));
            AddSuffix("PURELIFTVECTOR", new Suffix<Vector>(() => new Vector(_vesselState.PureLiftVector),
                "Pure lift acceleration vector in m/s^2."));
            AddSuffix("THRUSTFORWARD", new Suffix<Vector>(() => new Vector(_vesselState.ThrustForward),
                "Unit thrust direction (zero if throttle is zero)."));
            AddSuffix("THRUSTVECTORLASTFRAME", new Suffix<Vector>(() => new Vector(_vesselState.ThrustVectorLastFrame),
                "Thrust vector applied last frame in kN."));
            AddSuffix("THRUSTVECTORMAXTHROTTLE", new Suffix<Vector>(() => new Vector(_vesselState.ThrustVectorMaxThrottle),
                "Thrust vector at full throttle in kN."));
            AddSuffix("THRUSTVECTORMINTHROTTLE", new Suffix<Vector>(() => new Vector(_vesselState.ThrustVectorMinThrottle),
                "Thrust vector at zero throttle in kN."));
            AddSuffix("TORQUEAVAILABLE", new Suffix<Vector>(() => new Vector(_vesselState.TorqueAvailable),
                "Total available torque vector."));
            AddSuffix("TORQUEDIFFERENTIALTHROTTLE", new Suffix<Vector>(() => new Vector(_vesselState.TorqueDifferentialThrottle),
                "Torque available from differential throttle."));
            AddSuffix("TORQUERESPONSESPEED", new Suffix<Vector>(() => new Vector(_vesselState.TorqueResponseSpeed),
                "Torque response-speed filter constants."));

            // --- Rotations ---
            AddSuffix("ROTATIONSURFACE", new Suffix<Direction>(() => new Direction(_vesselState.RotationSurface),
                "Rotation of the surface (NED) frame."));
            AddSuffix("ROTATIONVESSELSURFACE", new Suffix<Direction>(() => new Direction(_vesselState.RotationVesselSurface),
                "Rotation of the vessel relative to the surface frame."));
        }
    }
}
