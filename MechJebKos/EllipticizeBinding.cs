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
    // ADDONS:MECHJEB:MANEUVERPLANNER:ELLIPTICIZE - change both periapsis and apoapsis with one burn at TIMEREF.
    [KOSNomenclature("MechJebEllipticize")]
    public class EllipticizeBinding : OperationBinding<OperationEllipticize>
    {
        public EllipticizeBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("PERIAPSIS", new SetSuffix<ScalarValue>(() => Op.NewPeA.Val, value => Op.NewPeA.Val = value,
                "New periapsis altitude in meters."));
            AddSuffix("APOAPSIS", new SetSuffix<ScalarValue>(() => Op.NewApA.Val, value => Op.NewApA.Val = value,
                "New apoapsis altitude in meters."));
        }
    }
}
