/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:NODE - drives MechJebModuleNodeExecutor.
    [KOSNomenclature("MechJebNodeExecutor")]
    public class NodeExecutorBinding : ComputerModuleBinding<MechJebModuleNodeExecutor>
    {
        public NodeExecutorBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("STATE", new Suffix<StringValue>(() => Module.State.ToString(),
                "Executor state: WARPALIGN, LEAD, BURN, or IDLE."));
            AddSuffix(new[] { "AUTOWARP", "WARP" },
                new SetSuffix<BooleanValue>(() => Module.Autowarp, value => Module.Autowarp = value,
                    "Automatically time-warp to the node."));
        }

        // The node executor engages via ExecuteOneNode / Abort rather than the plain Enabled toggle.
        protected override void SetEnabled(bool enabled)
        {
            if (enabled)
                Module.ExecuteOneNode(this);
            else
                Module.Abort();
        }
    }
}
