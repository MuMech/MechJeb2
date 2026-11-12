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
    // ADDONS:MECHJEB:MANEUVERPLANNER:INTERCEPT - intercept the target (same sphere of influence) at a chosen time: a Lambert burn
    // at TIMEREF X_FROM_NOW (LEADTIME), reaching the target INTERCEPTINTERVAL seconds later.
    [KOSNomenclature("MechJebIntercept")]
    public class InterceptBinding : OperationBinding<OperationLambert>
    {
        public InterceptBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("INTERCEPTINTERVAL", new SetSuffix<ScalarValue>(() => Op.InterceptInterval.Val, value => Op.InterceptInterval.Val = value,
                "Seconds from the burn to the intercept."));
        }
    }
}
