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
    // ADDONS:MECHJEB:MANEUVERPLANNER:HOHMANN - bi-impulsive (Hohmann) transfer to the target (same sphere of influence).
    //
    // TIMEREF COMPUTED lets MechJeb choose the departure; any other fixes it.
    [KOSNomenclature("MechJebHohmann")]
    public class HohmannBinding : OperationBinding<OperationGeneric>
    {
        public HohmannBinding(Func<MechJebCore?> core, SharedObjects shared) : base(core, shared) { }

        protected override void InitializeOperationSuffixes()
        {
            AddSuffix("CAPTURE", new SetSuffix<BooleanValue>(() => Op.Capture, value => Op.Capture = value,
                "Two burns, capturing at the target (rendezvous, or MATCHORBIT); false for a single intercept or flyby burn."));
            AddSuffix("PLANCAPTURE", new SetSuffix<BooleanValue>(() => Op.PlanCapture, value => Op.PlanCapture = value,
                "Also make the arrival burn's node, when CAPTURE and the target allows it (a celestial target needs MATCHORBIT or a LAGTIME)."));
            AddSuffix("MATCHORBIT", new SetSuffix<BooleanValue>(() => Op.MatchOrbit, value => Op.MatchOrbit = value,
                "With CAPTURE: match the target's orbit rather than meeting the target itself."));
            AddSuffix("LAGTIME", new SetSuffix<ScalarValue>(() => Op.LagTime.Val, value => Op.LagTime.Val = value,
                "Arrival delay in seconds: arrive this long after the target passes the intercept point (not used with MATCHORBIT)."));
            AddSuffix("COPLANAR", new SetSuffix<BooleanValue>(() => Op.Coplanar, value => Op.Coplanar = value,
                "Plan a simple coplanar transfer, ignoring the difference in inclination."));
        }
    }
}
