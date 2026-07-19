/*
 * Copyright Lamont Granquist, Sebastien Gaggini and the MechJeb contributors
 * SPDX-License-Identifier: LicenseRef-PD-hp OR Unlicense OR CC0-1.0 OR 0BSD OR MIT-0 OR MIT OR LGPL-2.1+
 */

using System.Collections.Generic;
using MechJebLib.Utils;
using static System.FormattableString;

namespace MechJebLib.FuelFlowSimulation.PartModules
{
    public class SimModuleDockingNode : SimPartModule
    {
        private static readonly ObjectPool<SimModuleDockingNode> _pool = new ObjectPool<SimModuleDockingNode>(New, Clear);

        public bool Staged = false;
        public SimPart? AttachedPart;

        public override void Dispose() => _pool.Release(this);

        public static SimModuleDockingNode Borrow(SimPart part)
        {
            SimModuleDockingNode decoupler = _pool.Borrow();
            decoupler.Part = part;
            return decoupler;
        }

        private static SimModuleDockingNode New() => new SimModuleDockingNode();

        private static void Clear(SimModuleDockingNode m) => m.AttachedPart = null;

        public override string ToString()
        {
            List<string> fields = CommonFieldList();
            AddField(fields, "Staged", Staged, false);
            if (AttachedPart != null)
                fields.Add(Invariant($"AttachedPart={AttachedPart.Ident}"));
            return ModuleLine("SimModuleDockingNode", fields);
        }
    }
}
