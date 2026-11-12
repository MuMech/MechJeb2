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
    // ADDONS:MECHJEB:MANEUVERPLANNER:RESONANTORBIT - burn at TIMEREF into an orbit whose period is NUMERATOR/DENOMINATOR of the
    // current one (e.g. for spacing satellites).
    [KOSNomenclature("MechJebResonantOrbit")]
    public class ResonantOrbitBinding : OperationBinding<OperationResonantOrbit>
    {
        public ResonantOrbitBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("NUMERATOR", new SetSuffix<ScalarValue>(() => Op.ResonanceNumerator.Val, value => Op.ResonanceNumerator.Val = (int)value,
                "Numerator of the new period as a fraction of the current one (a whole number above zero)."));
            AddSuffix("DENOMINATOR", new SetSuffix<ScalarValue>(() => Op.ResonanceDenominator.Val, value => Op.ResonanceDenominator.Val = (int)value,
                "Denominator of the new period as a fraction of the current one (a whole number above zero)."));
        }
    }
}
