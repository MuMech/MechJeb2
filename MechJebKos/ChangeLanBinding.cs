/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:MANEUVERPLANNER:CHANGELAN - change the longitude of the ascending node with a burn at TIMEREF.
    //
    // The window edits the target controller's longitude for this, which also places a position target; kOS keeps its own
    // (OperationLan.LongitudeOverride) and requires it to be set.
    [KOSNomenclature("MechJebChangeLAN")]
    public class ChangeLanBinding : OperationBinding<OperationLan>
    {
        public ChangeLanBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("LAN", new SetSuffix<ScalarValue>(() => Op.LongitudeOverride ?? double.NaN, value => Op.LongitudeOverride = value,
                "New longitude of the ascending node in degrees (NaN until set; must be set before planning)."));
        }

        protected override void BeforePlan(Orbit o, double ut, bool replaceLast)
        {
            if (!(Op.LongitudeOverride is double lan) || double.IsNaN(lan) || double.IsInfinity(lan))
                throw new KOSException("Set LAN (degrees) before planning a LAN change.");
        }
    }
}
