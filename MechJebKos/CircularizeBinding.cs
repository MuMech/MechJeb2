/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:MANEUVERPLANNER:CIRCULARIZE - circularize at TIMEREF.
    [KOSNomenclature("MechJebCircularize")]
    public class CircularizeBinding : OperationBinding<OperationCircularize>
    {
        public CircularizeBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes() { }
    }
}
