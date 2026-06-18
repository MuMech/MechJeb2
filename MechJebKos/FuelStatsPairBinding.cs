/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // The vacuum/atmosphere pair of FuelStats for a single fuel-flow stage index, reached via
    // ADDONS:MECHJEB:STAGESTATS:STAGEDELTAV[i].
    [KOSNomenclature("MechJebFuelStatsPair")]
    public class FuelStatsPairBinding : Structure
    {
        public FuelStatsPairBinding(StageStatsBinding stats, int stage)
        {
            AddSuffix(new[] { "VACUUM", "VAC" }, new NoArgsSuffix<FuelStatsBinding>(
                () => new FuelStatsBinding(() => stats.GetVacFuelStats(stage), stats.VacConditions),
                "Vacuum fuel-flow stats for this stage."));
            AddSuffix(new[] { "ATMOSPHERE", "ATMO" }, new NoArgsSuffix<FuelStatsBinding>(
                () => new FuelStatsBinding(() => stats.GetAtmoFuelStats(stage), stats.AtmoConditions),
                "Atmospheric fuel-flow stats for this stage."));
            AddSuffix("STAGE", new NoArgsSuffix<ScalarValue>(() => stage,
                "The fuel-flow stage index this object refers to."));
        }
    }
}
