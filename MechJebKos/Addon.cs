/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using JetBrains.Annotations;
using kOS;
using kOS.AddOns;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // The kOS addon entry point, reached from scripts as ADDONS:MECHJEB.
    //
    // kOS instantiates one Addon per processor (per SharedObjects), so this is inherently
    // local to a single vessel: everything resolves through shared.Vessel and we never reach
    // for FlightGlobals.activeVessel or a global singleton. Cross-vessel coordination is left
    // to kOS itself.
    [kOSAddon("MechJeb")]
    [KOSNomenclature("MechJebAddon")]
    [UsedImplicitly]
    public class Addon : kOS.Suffixed.Addon
    {
        public Addon(SharedObjects shared) : base(shared) => RegisterInitializer(InitializeSuffixes);

        // must never be cached, it can be updated dyanmically
        private MechJebCore? _core => shared.Vessel.GetMasterMechJeb();

        public override BooleanValue Available() => !(_core is null);

        private void InitializeSuffixes()
        {
            var nodeExecutor = new NodeExecutorBinding(() => _core);

            AddSuffix("RUNNING", new NoArgsSuffix<BooleanValue>(() =>  _core?.running ?? false,
                "True if MechJeb is present and running on this vessel."));
            AddSuffix(new[] { "NODE", "NODEEXECUTOR" }, new NoArgsSuffix<NodeExecutorBinding>(() => nodeExecutor,
                "The maneuver node executor."));
        }
    }
}
