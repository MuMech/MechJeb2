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
    // ADDONS:MECHJEB:MANEUVERPLANNER:CHANGEINCLINATION - change the inclination with a burn at TIMEREF.
    [KOSNomenclature("MechJebChangeInclination")]
    public class ChangeInclinationBinding : OperationBinding<OperationInclination>
    {
        public ChangeInclinationBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("INCLINATION", new SetSuffix<ScalarValue>(() => Op.NewInc.Val, value => Op.NewInc.Val = value,
                "New inclination in degrees."));
        }
    }
}
