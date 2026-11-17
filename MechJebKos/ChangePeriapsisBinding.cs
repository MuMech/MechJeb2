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
    // ADDONS:MECHJEB:MANEUVERPLANNER:CHANGEPERIAPSIS - change the periapsis with a burn at TIMEREF.
    [KOSNomenclature("MechJebChangePeriapsis")]
    public class ChangePeriapsisBinding : OperationBinding<OperationPeriapsis>
    {
        public ChangePeriapsisBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("PERIAPSIS", new SetSuffix<ScalarValue>(() => Op.NewPeA.Val, value => Op.NewPeA.Val = value,
                "New periapsis altitude in meters."));
        }
    }
}
