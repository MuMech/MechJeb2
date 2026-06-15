/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;
using static MechJebLib.Utils.Statics;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:HOVERSLAMSIMULATION - reads MechJebModuleHoverslamSimulation.
    //
    // Always-on predictor (enabled whenever in flight), so there is no ENABLED suffix. The read-out
    // values are raw numbers (the GUI's formatted Impact/Ignition/Biome/etc. strings are omitted in
    // favor of these). Time/coordinate outputs are NaN when there is no solution; check HASSOLUTION.
    [KOSNomenclature("MechJebHoverslamSimulation")]
    public class HoverslamSimulationBinding : ComputerModuleBinding<MechJebModuleHoverslamSimulation>
    {
        public HoverslamSimulationBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("HASSOLUTION", new Suffix<BooleanValue>(() => IsFinite(Module.IgnitionUT),
                "True if the simulation currently has a landing solution."));
            AddSuffix("IGNITIONUT", new Suffix<ScalarValue>(() => Module.IgnitionUT,
                "Universal time of suicide-burn ignition (NaN if no solution)."));
            AddSuffix("LANDINGUT", new Suffix<ScalarValue>(() => Module.LandingUT,
                "Universal time of predicted touchdown (NaN if no solution)."));
            AddSuffix("IGNITIONCOUNTDOWN", new Suffix<ScalarValue>(() => Module.IgnitionCountdown,
                "Seconds until ignition (NaN if no solution)."));
            AddSuffix("LANDINGCOUNTDOWN", new Suffix<ScalarValue>(() => Module.LandingCountdown,
                "Seconds until touchdown (NaN if no solution)."));
            AddSuffix("LATITUDE", new Suffix<ScalarValue>(() => Module.Lat,
                "Predicted landing latitude in degrees."));
            AddSuffix("LONGITUDE", new Suffix<ScalarValue>(() => Module.Lng,
                "Predicted landing longitude in degrees."));
            AddSuffix("TERRAINALTITUDE", new Suffix<ScalarValue>(() => Module.TerrainAltitude,
                "Terrain altitude at the predicted landing site in meters."));
            AddSuffix("SLOPE", new Suffix<ScalarValue>(() => Module.Slope,
                "Terrain slope at the predicted landing site in degrees."));
            AddSuffix("FINALDESCENTSPEED", new Suffix<ScalarValue>(() => Module.FinalDescentSpeed,
                "Planned final descent speed in m/s."));
            AddSuffix("FINALTHRUSTACCEL", new Suffix<ScalarValue>(() => Module.FinalThrustAccel,
                "Final thrust acceleration in m/s^2 (-1 if no solution)."));

            AddSuffix("MAPLANDINGPREDICTION", new SetSuffix<BooleanValue>(() => Module.MapLandingPrediction, value => Module.MapLandingPrediction = value,
                "Draw the predicted landing site marker on the map."));
            AddSuffix("SIMRECALCINTERVAL", new SetSuffix<ScalarValue>(() => Module.SimRecalcInterval.Val, value => Module.SimRecalcInterval.Val = value,
                "Seconds between simulation recalculations."));
            AddSuffix("VERTICALAUTHORITY", new SetSuffix<ScalarValue>(() => Module.VerticalAuthority.Val, value => Module.VerticalAuthority.Val = value,
                "Vertical-phase authority as a fraction (0-1)."));
            AddSuffix("VERTICALALTITUDE", new SetSuffix<ScalarValue>(() => Module.VerticalAltitude.Val, value => Module.VerticalAltitude.Val = value,
                "Altitude in meters at which the vertical descent phase begins."));
        }
    }
}
