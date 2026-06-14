/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System;
using kOS.Safe.Encapsulation;
using kOS.Safe.Encapsulation.Suffixes;
using kOS.Safe.Exceptions;
using kOS.Safe.Utilities;

namespace MuMech.MechJebKos
{
    // ADDONS:MECHJEB:NODEEXECUTOR - drives MechJebModuleNodeExecutor.
    [KOSNomenclature("MechJebNodeExecutor")]
    public class NodeExecutorBinding : ComputerModuleBinding<MechJebModuleNodeExecutor>
    {
        public NodeExecutorBinding(Func<MechJebCore?> core) : base(core) { }

        protected override void InitializeSuffixes()
        {
            AddSuffix("ENABLED", new SetSuffix<BooleanValue>(() => Module.Enabled, value => SetEnabled(value),
                "Whether node execution is engaged: set true to execute, false to abort."));
            AddSuffix("STATE", new Suffix<StringValue>(() => Module.State.ToString(),
                "Executor state: WARPALIGN, LEAD, BURN, or IDLE."));
            AddSuffix("MODE", new SetSuffix<StringValue>(() => Module.Mode.ToString(), value => Module.Mode = ParseMode(value),
                "Execution mode: ONE_NODE or ALL_NODES."));
            AddSuffix("AUTOWARP",
                new SetSuffix<BooleanValue>(() => Module.Autowarp, value => Module.Autowarp = value,
                    "Automatically time-warp to the node."));
            AddSuffix("LEADTIME", new SetSuffix<ScalarValue>(() => Module.LeadTime.Val, value => Module.LeadTime.Val = value,
                "Seconds before the burn to begin orienting the vessel."));
            AddSuffix("RCSONLY", new SetSuffix<BooleanValue>(() => Module.RCSOnly, value => Module.RCSOnly = value,
                "Execute the burn using RCS only."));
            AddSuffix("KILLROLLROTATION", new SetSuffix<BooleanValue>(() => Module.KillRollRotation, value => Module.KillRollRotation = value,
                "Kill roll rotation while executing the burn."));
            AddSuffix("EXECUTEONENODE", new NoArgsVoidSuffix(() => Module.ExecuteOneNode(this),
                "Execute the next maneuver node."));
            AddSuffix("EXECUTEALLNODES", new NoArgsVoidSuffix(() => Module.ExecuteAllNodes(this),
                "Execute all maneuver nodes in sequence."));
        }

        // The node executor engages via ExecuteOneNode / Abort rather than the plain Enabled toggle.
        private void SetEnabled(bool enabled)
        {
            if (enabled)
                Module.ExecuteOneNode(this);
            else
                Module.Abort();
        }

        private static MechJebModuleNodeExecutor.Modes ParseMode(StringValue value)
        {
            if (Enum.TryParse(value.ToString(), true, out MechJebModuleNodeExecutor.Modes mode))
                return mode;
            throw new KOSException($"Invalid node executor mode '{value}'. Expected ONE_NODE or ALL_NODES.");
        }
    }
}
