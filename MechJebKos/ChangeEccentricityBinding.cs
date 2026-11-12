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
    // ADDONS:MECHJEB:MANEUVERPLANNER:CHANGEECCENTRICITY - change the eccentricity with a burn at TIMEREF.
    [KOSNomenclature("MechJebChangeEccentricity")]
    public class ChangeEccentricityBinding : OperationBinding<OperationEccentricity>
    {
        public ChangeEccentricityBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("ECCENTRICITY", new SetSuffix<ScalarValue>(() => Op.NewEcc.Val, value => Op.NewEcc.Val = value,
                "New eccentricity (0 or more)."));
        }
    }
}
