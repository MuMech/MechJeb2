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
    // ADDONS:MECHJEB:MANEUVERPLANNER:STATIONARYORBIT - a two-burn transfer into the body's synchronous orbit, over LONGITUDE.
    //
    // The window takes the longitude from (and places) a position target; kOS sets it directly and leaves the target alone.
    [KOSNomenclature("MechJebStationaryOrbit")]
    public class StationaryOrbitBinding : OperationBinding<OperationStationaryOrbit>
    {
        public StationaryOrbitBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("LONGITUDE", new SetSuffix<ScalarValue>(() => Op.targetLongitude, value => Op.targetLongitude = value,
                "Surface longitude in degrees to station over."));
        }
    }
}
