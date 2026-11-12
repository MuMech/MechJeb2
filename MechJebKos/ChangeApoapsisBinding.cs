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
    // ADDONS:MECHJEB:MANEUVERPLANNER:CHANGEAPOAPSIS - change the apoapsis with a burn at TIMEREF.
    [KOSNomenclature("MechJebChangeApoapsis")]
    public class ChangeApoapsisBinding : OperationBinding<OperationApoapsis>
    {
        public ChangeApoapsisBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("APOAPSIS", new SetSuffix<ScalarValue>(() => Op.NewApA.Val, value => Op.NewApA.Val = value,
                "New apoapsis altitude in meters."));
        }
    }
}
