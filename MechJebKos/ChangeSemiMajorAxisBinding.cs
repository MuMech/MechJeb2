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
    // ADDONS:MECHJEB:MANEUVERPLANNER:CHANGESEMIMAJORAXIS - change the semi-major axis with a burn at TIMEREF.
    [KOSNomenclature("MechJebChangeSemiMajorAxis")]
    public class ChangeSemiMajorAxisBinding : OperationBinding<OperationSemiMajor>
    {
        public ChangeSemiMajorAxisBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("SEMIMAJORAXIS", new SetSuffix<ScalarValue>(() => Op.NewSma.Val, value => Op.NewSma.Val = value,
                "New semi-major axis in meters (from the body's center, not an altitude)."));
        }
    }
}
