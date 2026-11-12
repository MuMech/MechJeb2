/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:MANEUVERPLANNER:CHANGELONGITUDE - burn at TIMEREF (an apsis) so that, one orbit later, that apsis is
    // over a chosen surface longitude.
    //
    // The window edits the target controller's longitude for this, which also places a position target; kOS keeps its own
    // (OperationLongitude.LongitudeOverride) and requires it to be set.
    [KOSNomenclature("MechJebChangeLongitude")]
    public class ChangeLongitudeBinding : OperationBinding<OperationLongitude>
    {
        public ChangeLongitudeBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("LONGITUDE", new SetSuffix<ScalarValue>(() => Op.LongitudeOverride ?? double.NaN, value => Op.LongitudeOverride = value,
                "Surface longitude in degrees the burn's apsis is over one orbit later (NaN until set; must be set before planning)."));
        }

        protected override void BeforePlan(Orbit o, double ut, bool replaceLast)
        {
            if (!(Op.LongitudeOverride is double longitude) || double.IsNaN(longitude) || double.IsInfinity(longitude))
                throw new KOSException("Set LONGITUDE (degrees) before planning a surface longitude change.");
        }
    }
}
