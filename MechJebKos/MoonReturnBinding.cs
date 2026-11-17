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
    // ADDONS:MECHJEB:MANEUVERPLANNER:MOONRETURN - return from a moon to the body it orbits, burning at the next return window
    // (MechJeb chooses the time).
    [KOSNomenclature("MechJebMoonReturn")]
    public class MoonReturnBinding : OperationBinding<OperationMoonReturn>
    {
        public MoonReturnBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("PERIAPSIS", new SetSuffix<ScalarValue>(() => Op.Periapsis.Val, value => Op.Periapsis.Val = value,
                "Approximate final periapsis altitude in meters above the body returned to."));
            AddSuffix("INCLINATIONFLAG", new SetSuffix<BooleanValue>(() => Op.InclinationFlag, value => Op.InclinationFlag = value,
                "Also target INCLINATION for the return orbit."));
            AddSuffix("INCLINATION", new SetSuffix<ScalarValue>(() => Op.Inclination.Val, value => Op.Inclination.Val = value,
                "Inclination in degrees of the return orbit, used when INCLINATIONFLAG is true."));
        }
    }
}
